using IdealAkeWms.Data;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Services;

/// <summary>
/// IDEAL-Standort Teil 8, Etappe 2: serverseitige Aufloesung eines gescannten FA-Segments auf einen
/// buchbaren Sub-FA im Werkbank-Scope. Nach der Schema-Inversion (Teil 7) ist <c>OrderNumber</c>
/// nicht mehr eindeutig; ein Scan kann daher mehrere Sub-FAs derselben Gruppe treffen.
///
/// Verbindliche Entscheidungen (Schranke 1, 2026-08-12):
/// - <b>Granularitaet (KP2-3):</b> Auswahleinheit ist IMMER der Sub-FA (ProductionOrder), in beiden
///   Modi. Im Normal-Modus steht der Arbeitsgang durch den Scan bereits fest; die Mehrdeutigkeit
///   sitzt auf (<c>OrderNumber</c>+<c>OperationNumber</c>), die Auswahl geht ueber die Sub-FAs.
/// - <b>Scope (KP2-6):</b> werkbank-gescopt, deckungsgleich mit
///   <see cref="Controllers.BdeApiController.GetAvailableOperations"/>. Entscheidung „genau ein /
///   mehrere" faellt auf der Werkbank-Menge, nicht auf der vollen Gruppe.
///
/// Aufloesungsreihenfolge (Spec „Fachliche Anforderungen"):
/// 1. <c>OrderNumber</c>-Gruppe, werkbank-gescopt → 1 = direkt (Exact), mehrere = Auswahl (Ambiguous).
/// 2. Kein Treffer im Scope, aber HauptFA existiert global → <see cref="BdeScanResolutionKind.NotInScope"/>.
/// 3. HauptFA existiert gar nicht → Fallback-Lookup gegen <c>SubOrderNumber</c> (heute unerreichbare
///    Vorruestung, AK 3), ebenfalls werkbank-gescopt.
/// 4. Kein Treffer → <see cref="BdeScanResolutionKind.NotFound"/>.
///
/// Hinweis Etappe 3: <c>GetAvailableOperations</c> traegt heute dieselben Scope-Praedikate inline;
/// die Konsolidierung auf diesen Service (damit Server-Entscheidung und Werkbank-Liste per
/// Konstruktion deckungsgleich sind) erfolgt beim UI-Aufsatz in Etappe 3 — abgesichert durch die
/// Tests dieses Service.
/// </summary>
public interface IBdeScanResolver
{
    Task<BdeScanResolution> ResolveAsync(int workplaceId, string scannedFa, string? operationNumber,
        bool nurFaMode, int? operatorId, CancellationToken ct = default);
}

public enum BdeScanResolutionKind
{
    /// <summary>Genau ein buchbares Ziel im Werkbank-Scope → direkt buchen, keine Auswahl.</summary>
    Exact,
    /// <summary>Mehrere Sub-FAs im Werkbank-Scope → Auswahl noetig.</summary>
    Ambiguous,
    /// <summary>Kein offener Treffer an dieser Werkbank, obwohl der HauptFA global existiert.</summary>
    NotInScope,
    /// <summary>Weder <c>OrderNumber</c>- noch <c>SubOrderNumber</c>-Treffer.</summary>
    NotFound
}

public enum BdeBookTarget
{
    /// <summary>Normal-Modus: Buchung per <c>workOperationId</c>.</summary>
    WorkOperation,
    /// <summary>NurFA-Modus: Buchung per <c>productionOrderId</c> (StartProductionForOrder).</summary>
    ProductionOrder
}

/// <summary>Ein buchbarer Auswahlkandidat (immer ein Sub-FA; im Normal-Modus mit fixem Arbeitsgang).</summary>
public record BdeScanCandidate(
    int Id,
    BdeBookTarget Target,
    int ProductionOrderId,
    string OrderNumber,
    string SubOrderNumber,
    string? Description,
    string? ArticleNumber,
    string? OperationNumber,
    string? OperationName);

public record BdeScanResolution(
    BdeScanResolutionKind Kind,
    IReadOnlyList<BdeScanCandidate> Candidates,
    bool ViaSubOrderNumberFallback = false)
{
    /// <summary>Der einzige Kandidat bei <see cref="BdeScanResolutionKind.Exact"/>, sonst null.</summary>
    public BdeScanCandidate? Single => Candidates.Count == 1 ? Candidates[0] : null;

    public static BdeScanResolution NotFound { get; } =
        new(BdeScanResolutionKind.NotFound, Array.Empty<BdeScanCandidate>());
}

public class BdeScanResolver : IBdeScanResolver
{
    private readonly ApplicationDbContext _ctx;

    public BdeScanResolver(ApplicationDbContext ctx) => _ctx = ctx;

    public async Task<BdeScanResolution> ResolveAsync(int workplaceId, string scannedFa, string? operationNumber,
        bool nurFaMode, int? operatorId, CancellationToken ct = default)
    {
        // FA-Segment wie bde-terminal.js:scanFaAgInput / BdeApiController.GetWorkOperation:
        // Split auf ',' / '/' → erstes Segment ist die FA-/OrderNumber.
        var fa = (scannedFa ?? string.Empty).Split(',', '/')[0].Trim();
        if (fa.Length == 0) return BdeScanResolution.NotFound;

        // 1. OrderNumber-Gruppe, werkbank-gescopt.
        var byOrder = await BuildCandidatesAsync(workplaceId, fa, bySubOrder: false, operationNumber, nurFaMode, operatorId, ct);
        if (byOrder.Count > 0)
            return new BdeScanResolution(
                byOrder.Count == 1 ? BdeScanResolutionKind.Exact : BdeScanResolutionKind.Ambiguous,
                byOrder);

        // 2. Kein Treffer im Scope, aber HauptFA existiert global → eigene Meldung (KP2-6 Randfall a).
        var hauptFaExists = await _ctx.ProductionOrders.AsNoTracking().AnyAsync(po => po.OrderNumber == fa, ct);
        if (hauptFaExists)
            return new BdeScanResolution(BdeScanResolutionKind.NotInScope, Array.Empty<BdeScanCandidate>());

        // 3. Fallback SubOrderNumber (heute unerreichbare Vorruestung, AK 3), ebenfalls werkbank-gescopt.
        var bySub = await BuildCandidatesAsync(workplaceId, fa, bySubOrder: true, operationNumber, nurFaMode, operatorId, ct);
        if (bySub.Count > 0)
            return new BdeScanResolution(
                bySub.Count == 1 ? BdeScanResolutionKind.Exact : BdeScanResolutionKind.Ambiguous,
                bySub, ViaSubOrderNumberFallback: true);

        // 4. Kein Treffer → bestehende Fehlerbehandlung.
        return BdeScanResolution.NotFound;
    }

    private Task<List<BdeScanCandidate>> BuildCandidatesAsync(int workplaceId, string value, bool bySubOrder,
        string? operationNumber, bool nurFaMode, int? operatorId, CancellationToken ct)
        => nurFaMode
            ? BuildNurFaCandidatesAsync(workplaceId, value, bySubOrder, operatorId, ct)
            : BuildNormalCandidatesAsync(workplaceId, value, bySubOrder, operationNumber, operatorId, ct);

    // NurFA-Modus: offene ProductionOrders an dieser Werkbank (deckungsgleich mit
    // GetAvailableOperations, NurFA-Zweig): nicht erledigt/storniert, nicht Sage-verpackt/-abgeholt,
    // nicht final gemeldet, und (falls Operator bekannt) nicht bereits vom Operator aktiv gebucht.
    private Task<List<BdeScanCandidate>> BuildNurFaCandidatesAsync(int workplaceId, string value,
        bool bySubOrder, int? operatorId, CancellationToken ct)
    {
        var q = _ctx.ProductionOrders.AsNoTracking()
            .Where(po => po.ProductionWorkplaceId == workplaceId && !po.IsDone && !po.IsCancelled
                && (po.ExtraInfo == null || po.ExtraInfo.SageStatus == null
                    || (po.ExtraInfo.SageStatus.Trim().ToLower() != FaZusatzinfoStatus.Verpackt
                        && po.ExtraInfo.SageStatus.Trim().ToLower() != FaZusatzinfoStatus.Abgeholt))
                && !_ctx.BdeBookingQuantities.Any(bq =>
                    bq.IsFinal
                    && bq.BdeBooking.WorkOperation!.ProductionOrderId == po.Id
                    && !bq.BdeBooking.IsCancelled));

        q = bySubOrder ? q.Where(po => po.SubOrderNumber == value) : q.Where(po => po.OrderNumber == value);

        if (operatorId.HasValue)
        {
            q = q.Where(po => !_ctx.BdeBookings.Any(b =>
                b.WorkOperation!.ProductionOrderId == po.Id
                && b.BdeOperatorId == operatorId.Value
                && b.EndedAt == null
                && !b.IsCancelled));
        }

        return q
            .OrderBy(po => po.SubOrderNumber)
            .Select(po => new BdeScanCandidate(
                po.Id, BdeBookTarget.ProductionOrder, po.Id,
                po.OrderNumber, po.SubOrderNumber, po.Description1, po.ArticleNumber, null, null))
            .ToListAsync(ct);
    }

    // Normal-Modus: offene WorkOperations an dieser Werkbank (deckungsgleich mit
    // GetOpenByWorkplaceIdAsync(excludePacked:true) minus aktiv/final gemeldet), gefiltert auf
    // OrderNumber (+ OperationNumber aus dem Scan). Der Arbeitsgang steht fest — die Auswahl geht
    // ueber die Sub-FAs (KP2-3).
    private Task<List<BdeScanCandidate>> BuildNormalCandidatesAsync(int workplaceId, string value,
        bool bySubOrder, string? operationNumber, int? operatorId, CancellationToken ct)
    {
        var q = _ctx.WorkOperations.AsNoTracking()
            .Include(w => w.ProductionOrder)
            .Where(w => w.ProductionWorkplaceId == workplaceId && !w.IsReported
                && (w.ProductionOrder.ExtraInfo == null || w.ProductionOrder.ExtraInfo.SageStatus == null
                    || (w.ProductionOrder.ExtraInfo.SageStatus.Trim().ToLower() != FaZusatzinfoStatus.Verpackt
                        && w.ProductionOrder.ExtraInfo.SageStatus.Trim().ToLower() != FaZusatzinfoStatus.Abgeholt)));

        q = bySubOrder
            ? q.Where(w => w.ProductionOrder.SubOrderNumber == value)
            : q.Where(w => w.ProductionOrder.OrderNumber == value);

        if (!string.IsNullOrEmpty(operationNumber))
            q = q.Where(w => w.OperationNumber == operationNumber);

        // Aktiv gebuchte (irgendwer, EndedAt==null) und final gemeldete AGs raus. Der system-weite
        // Aktiv-Ausschluss umfasst den operator-eigenen (myActive ⊆ active), daher genuegen die zwei.
        q = q.Where(w =>
            !_ctx.BdeBookings.Any(b => b.WorkOperationId == w.Id && b.EndedAt == null && !b.IsCancelled)
            && !_ctx.BdeBookingQuantities.Any(bq =>
                bq.IsFinal && !bq.BdeBooking.IsCancelled && bq.BdeBooking.WorkOperationId == w.Id));

        return q
            .OrderBy(w => w.ProductionOrder.SubOrderNumber).ThenBy(w => w.Sequence)
            .Select(w => new BdeScanCandidate(
                w.Id, BdeBookTarget.WorkOperation, w.ProductionOrderId,
                w.ProductionOrder.OrderNumber, w.ProductionOrder.SubOrderNumber,
                w.ProductionOrder.Description1, w.ProductionOrder.ArticleNumber,
                w.OperationNumber, w.Name))
            .ToListAsync(ct);
    }
}
