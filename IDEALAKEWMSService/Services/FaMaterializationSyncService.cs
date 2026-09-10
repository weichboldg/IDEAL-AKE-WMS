using IdealAkeWms.Data;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SyncLogger;
using IDEALAKEWMSService.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// Materialisiert die lokale FA-Struktur (<see cref="FaHierarchyNode"/>, Teil 1) nach
/// <see cref="ProductionOrder"/>. Die Struktur ist die Quelle, ProductionOrders ist abgeleitet
/// (Transformation, kein zweiter Import). Läuft NUR bei <c>ProduktionsauftragHierarchisch=true</c>;
/// bei false bleibt der bestehende SageImportService-Pfad (AKE) unangetastet. Die Entscheidung
/// trifft der reine <see cref="FaMaterializationPlanner"/> (unit-getestet), dieser Service ist die
/// DB-/Mail-/Log-Schale.
///
/// Mapping int→string (am ersten echten Datenlauf zu bestätigen, S7-2): OrderNumber=HauptFA,
/// SubOrderNumber=SubFA, ParentSubOrderNumber=VaterFA. Materialisiert werden Nicht-Blatt-Knoten
/// (SubFA != 0). App-verwaltete Felder (IsDone/PickingStatus/BdeStatus/Storno/ExtraInfo)
/// werden bei Updates NIE überschrieben — AUSNAHMEN sind genau zwei (Z1):
/// <see cref="ProductionOrder.ProductionWorkplaceId"/> (Variante B, ADR „Werkbank-Datenhoheit"):
/// Sage liefert je Knoten einen Arbeitsbereich, daraus wird die Werkbank abgeleitet; Sage ist
/// führend, ein abweichender Bestandswert wird überschrieben, weil der Sync nicht unterscheiden
/// kann, ob er von Hand gesetzt oder in Sage umgeplant wurde. Und
/// <see cref="ProductionOrderPickingStatus.HasCoatingParts"/> (Spec F5): aus der Struktur
/// abgeleitet (<see cref="FaMaterializationCoating.Derive"/>), geschrieben über
/// <see cref="IProductionOrderPickingStatusRepository.SetCoatingPartsAsync"/>, die den Reset von
/// <see cref="ProductionOrderPickingStatus.IsCoatingDone"/> bei Wechsel auf false kapselt
/// (Fallstrick #11) — deshalb bewusst NICHT per rohem Feld-Setzen.
/// </summary>
public class FaMaterializationSyncService : IFaMaterializationSyncService
{
    private const string ServiceUser = "IDEALAKEWMSService";

    private readonly ApplicationDbContext _ctx;
    private readonly IConfiguration _config;
    private readonly IMailService _mail;
    private readonly IUnknownWorkplaceState _unknownWorkplaces;
    private readonly IProductionOrderPickingStatusRepository _pickingStatus;
    private readonly ILogger<FaMaterializationSyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public FaMaterializationSyncService(
        ApplicationDbContext ctx,
        IConfiguration config,
        IMailService mail,
        IUnknownWorkplaceState unknownWorkplaces,
        IProductionOrderPickingStatusRepository pickingStatus,
        ILogger<FaMaterializationSyncService> logger,
        ISyncLogger syncLogger) // ISyncLogger als LETZTER Ctor-Parameter (ADR 0010)
    {
        _ctx = ctx;
        _config = config;
        _mail = mail;
        _unknownWorkplaces = unknownWorkplaces;
        _pickingStatus = pickingStatus;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    public async Task<FaMaterializationResult> RunAsync(bool dryRun, CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.FaMaterialization, ct);
        try
        {
            // Quelle: lokale FA-Struktur, ALLE Knoten inkl. Blaetter (SubFA == 0) — die
            // Lack-Ableitung (K2) braucht die Blattebene, um "direktes Kind beschichtet" zu
            // erkennen. Materialisiert werden weiterhin nur Nicht-Blatt-Knoten, gefiltert erst
            // beim Aufbau von "source". Mapping int->string.
            var nodes = await _ctx.FaHierarchyNodes.AsNoTracking()
                .ToListAsync(ct);

            var coating = FaMaterializationCoating.Derive(nodes);

            var source = nodes
                .Where(n => n.SubFA != 0)
                .Select(n => new MaterializationSourceOrder(
                    n.SubFA.ToString(),
                    n.HauptFA.ToString(),
                    n.VaterFA.HasValue ? n.VaterFA.Value.ToString() : null,
                    string.IsNullOrWhiteSpace(n.Arbeitsbereich) ? null : n.Arbeitsbereich.Trim(),
                    coating.GetValueOrDefault(n.SubFA.ToString())))
                .ToList();

            var orders = await _ctx.ProductionOrders
                .Include(o => o.PickingStatus)
                .ToListAsync(ct);

            var existing = orders
                .Select(o => new MaterializationExistingOrder(
                    o.SubOrderNumber, o.OrderNumber, o.ParentSubOrderNumber,
                    IsMissingMarked: o.SageMissingSince.HasValue,
                    IsKnownDone: o.IsDone || (o.PickingStatus?.IsDonePicking ?? false)))
                .ToList();

            var plan = FaMaterializationPlanner.Plan(source, existing);

            if (plan.Skipped)
            {
                _logger.LogWarning("FA-Materialisierung übersprungen: {Reason}", plan.SkipReason);
                await run.LogWarningAsync($"Übersprungen: {plan.SkipReason}", ct: ct);
                await run.FinishSuccessAsync(new Dictionary<string, int>
                {
                    ["angelegt"] = 0, ["vermisst_neu"] = 0, ["wieder_da"] = 0, ["umhaengung_konflikt"] = 0,
                    ["statuszeilen_ergaenzt"] = 0,
                    ["werkbank_gesetzt"] = 0, ["werkbank_abweichend"] = 0,
                    ["arbeitsbereich_unbekannt"] = 0, ["arbeitsbereich_mehrdeutig"] = 0,
                    ["lackflag_gesetzt"] = 0,
                }, messageSuffix: $"[übersprungen: {plan.SkipReason}]", ct: ct);
                return new FaMaterializationResult(0, 0, 0, 0, Skipped: true, dryRun);
            }

            // Umhängungen: nie still übernehmen — protokollieren (Regel 3).
            foreach (var c in plan.ReparentConflicts)
                await run.LogWarningAsync(
                    $"Umhängung erkannt (NICHT übernommen): Sub-FA {c.SubOrderNumber} Vater {c.OldParent ?? "—"} -> {c.NewParent ?? "—"} (zur Klärung)",
                    reference: c.SubOrderNumber, ct: ct);

            if (dryRun)
            {
                await run.FinishSuccessAsync(new Dictionary<string, int>
                {
                    ["angelegt"] = plan.ToCreate.Count,
                    ["vermisst_neu"] = plan.ToMarkMissing.Count,
                    ["wieder_da"] = plan.ToClearMissing.Count,
                    ["umhaengung_konflikt"] = plan.ReparentConflicts.Count,
                    ["statuszeilen_ergaenzt"] = 0,
                    ["werkbank_gesetzt"] = 0, ["werkbank_abweichend"] = 0,
                    ["arbeitsbereich_unbekannt"] = 0, ["arbeitsbereich_mehrdeutig"] = 0,
                    ["lackflag_gesetzt"] = 0,
                }, messageSuffix: "[DryRun]", ct: ct);
                return new FaMaterializationResult(plan.ToCreate.Count, plan.ToMarkMissing.Count,
                    plan.ToClearMissing.Count, plan.ReparentConflicts.Count, Skipped: false, DryRun: true);
            }

            var now = DateTime.Now;
            var srcByKey = source.ToDictionary(s => s.SubOrderNumber, StringComparer.Ordinal);
            // Wie "source": nur Nicht-Blatt-Knoten (SubFA != 0). Blaetter tragen alle denselben
            // Schluessel "0" — ohne diesen Filter wuerde TryAdd einen beliebigen davon gewinnen
            // lassen. Heute folgenlos (Zugriffe zusaetzlich ueber srcByKey abgesichert), aber eine
            // Falle fuer spaeter (Review-Nachzug Task 2). Die Lack-Ableitung selbst braucht weiterhin
            // ALLE Knoten inkl. Blaetter (siehe FaMaterializationCoating.Derive oben).
            var nodeByKey = new Dictionary<string, FaHierarchyNode>(StringComparer.Ordinal);
            foreach (var n in nodes.Where(n => n.SubFA != 0)) nodeByKey.TryAdd(n.SubFA.ToString(), n);
            var conflictKeys = plan.ReparentConflicts.Select(c => c.SubOrderNumber).ToHashSet(StringComparer.Ordinal);
            var toClear = plan.ToClearMissing.ToHashSet(StringComparer.Ordinal);
            var toMark = plan.ToMarkMissing.ToHashSet(StringComparer.Ordinal);

            // K2 / Variante B: EINE Werkbank-Abfrage je Lauf (kein N+1). S2: Name-Match
            // case-insensitiv + getrimmt. ProductionWorkplace.Name hat KEINEN Unique-Index —
            // mehrdeutige Namen sind moeglich und werden gemeldet, nicht geraten.
            var workplaceGroups = (await _ctx.ProductionWorkplaces.AsNoTracking()
                    .Select(w => new WorkplaceRef(w.Id, w.Name))
                    .ToListAsync(ct))
                .GroupBy(w => (w.Name ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            var unknownWorkplaceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ambiguousWorkplaceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var workplaceDeviations = new List<string>();
            var workplaceChanges = 0;

            // (1) Neu anlegen.
            foreach (var key in plan.ToCreate)
            {
                var n = nodeByKey[key];
                var newOrder = new ProductionOrder
                {
                    OrderNumber = n.HauptFA.ToString(),
                    SubOrderNumber = n.SubFA.ToString(),
                    ParentSubOrderNumber = n.VaterFA?.ToString(),
                    Quantity = n.Sollmenge,
                    ArticleNumber = n.Artnr,
                    Matchcode = n.Matchcode,
                    Description1 = n.Bezeichnung1,
                    Description2 = n.Bezeichnung2,
                    IsDone = false,
                    SageMissingSince = null,
                    CreatedAt = now, CreatedBy = ServiceUser, CreatedByWindows = ServiceUser,
                };
                if (ApplyWorkplace(newOrder, srcByKey[key].Arbeitsbereich, workplaceGroups,
                        unknownWorkplaceNames, ambiguousWorkplaceNames, workplaceDeviations))
                    workplaceChanges++;
                _ctx.ProductionOrders.Add(newOrder);
            }

            // (2)/(3) Bestehende: Quell-Felder aktualisieren (App-Felder erhalten), missing setzen/klären.
            foreach (var o in orders)
            {
                var touched = false;
                if (toMark.Contains(o.SubOrderNumber)) { o.SageMissingSince = now; touched = true; }
                else if (toClear.Contains(o.SubOrderNumber)) { o.SageMissingSince = null; touched = true; }

                if (nodeByKey.TryGetValue(o.SubOrderNumber, out var sn) && srcByKey.TryGetValue(o.SubOrderNumber, out var src))
                {
                    o.Quantity = sn.Sollmenge;
                    o.ArticleNumber = sn.Artnr;
                    o.Matchcode = sn.Matchcode;
                    o.Description1 = sn.Bezeichnung1;
                    o.Description2 = sn.Bezeichnung2;
                    // ParentSubOrderNumber NUR ohne Konflikt übernehmen (Regel 3: Umhängung nicht still).
                    if (!conflictKeys.Contains(o.SubOrderNumber))
                        o.ParentSubOrderNumber = sn.VaterFA?.ToString();
                    // F1: dieselbe Zuweisungsmethode wie im Anlege-Pfad — der Bestandsunterschied
                    // zwischen den beiden Zweigen ist die Ursache des UAT-Befunds und darf sich
                    // nicht wiederholen.
                    if (ApplyWorkplace(o, src.Arbeitsbereich, workplaceGroups,
                            unknownWorkplaceNames, ambiguousWorkplaceNames, workplaceDeviations))
                        workplaceChanges++;
                    touched = true;
                }
                if (touched)
                {
                    o.ModifiedAt = now; o.ModifiedBy = ServiceUser; o.ModifiedByWindows = ServiceUser;
                }
            }

            await _ctx.SaveChangesAsync(ct);

            // Meldungen zur Werkbank-Zuweisung (S1/S2/B4): Log bei JEDEM Lauf, Sammelmail nur bei
            // Aenderung der unbekannten Menge gegenueber dem letzten Lauf (sonst 96 identische
            // Mails/Tag bei 15-Minuten-Takt).
            // Zustand IMMER fortschreiben — auch bei leerer Menge. Sonst bleibt der letzte
            // gemeldete Stand stehen, nachdem jemand die Werkbaenke angelegt hat, und ein
            // spaeteres Wiederauftreten derselben Namen gilt faelschlich als "unveraendert"
            // und wird nie gemeldet.
            var unknownWorkplacesChanged = _unknownWorkplaces.HasChanged(unknownWorkplaceNames.ToList());
            if (unknownWorkplaceNames.Count > 0)
            {
                var list = string.Join(", ", unknownWorkplaceNames.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
                await run.LogWarningAsync(
                    $"Unbekannte Arbeitsbereiche ohne Werkbank-Stammsatz ({unknownWorkplaceNames.Count}): {list}. " +
                    "Keine Zuweisung — Werkbank einmalig anlegen, der nächste Lauf füllt sie.", ct: ct);
                if (unknownWorkplacesChanged)
                    await SendUnknownWorkplaceDigestAsync(unknownWorkplaceNames, ct);
            }
            if (ambiguousWorkplaceNames.Count > 0)
                await run.LogWarningAsync($"Arbeitsbereich-Name mehrfach in den Werkbank-Stammdaten " +
                    $"({ambiguousWorkplaceNames.Count}): {string.Join(", ", ambiguousWorkplaceNames)}. Keine Zuweisung.", ct: ct);
            foreach (var d in workplaceDeviations)
                await run.LogWarningAsync(d, ct: ct);

            var werkbankGesetzt = workplaceChanges - workplaceDeviations.Count;
            var werkbankAbweichend = workplaceDeviations.Count;

            // F6 / UAT-Befund H0: Die beiden 1:1-Satelliten-Zeilen existieren fuer materialisierte
            // Sub-FAs sonst NIE. Der AKE-Sync legt sie eager an (SageImportService); die
            // Materialisierung tat das bisher nicht — Folge: Freigabe im Leitstand scheitert mit
            // "PickingStatus-Zeile fehlt", BDE-Rueckmeldung und Kaskade laufen ins Leere.
            // Anlege- UND Update-Pfad, damit auch der Bestand nachgezogen wird (F6).
            var statusRowsCreated = 0;
            if (!dryRun)
                statusRowsCreated = await EnsureStatusRowsAsync(srcByKey.Keys, now, ct);

            // F5: zweite Z1-Ausnahme. Geschrieben wird ueber die etablierte Repository-Methode,
            // nicht per rohem Feld-Setzen — sie kapselt den IsCoatingDone-Reset (Fallstrick #11).
            // MUSS nach EnsureStatusRowsAsync laufen: SetCoatingPartsAsync aktualisiert nur
            // BESTEHENDE Zeilen.
            var coatingFlags = 0;
            if (!dryRun)
            {
                var matKeys = srcByKey.Keys.ToList();          // dieselbe Menge wie in EnsureStatusRowsAsync
                var orderIds = await _ctx.ProductionOrders
                    .Where(o => o.SubOrderNumber != null && matKeys.Contains(o.SubOrderNumber))
                    .Select(o => new { o.Id, o.SubOrderNumber })
                    .ToListAsync(ct);

                var coatingMap = orderIds.ToDictionary(
                    x => x.Id,
                    x => coating.GetValueOrDefault(x.SubOrderNumber!));
                await _pickingStatus.SetCoatingPartsAsync(coatingMap);
                coatingFlags = coatingMap.Count(kv => kv.Value);
            }

            // Sammelmail (EINE pro Lauf) für neu-vermisste, nicht-erledigte FAs (S7-2b).
            await SendMissingDigestAsync(plan.MissingToNotify, ct);

            _logger.LogInformation(
                "FA-Materialisierung: {Created} neu, {Marked} vermisst, {Cleared} wieder da, {Conf} Umhäng-Konflikte, " +
                "{Status} Statuszeilen ergänzt, {WpSet} Werkbank gesetzt, {WpDev} abweichend, " +
                "{WpUnknown} unbekannte Arbeitsbereiche, {WpAmbig} mehrdeutige Namen, {Coating} Lack-Flags gesetzt.",
                plan.ToCreate.Count, plan.ToMarkMissing.Count, plan.ToClearMissing.Count, plan.ReparentConflicts.Count,
                statusRowsCreated, werkbankGesetzt, werkbankAbweichend, unknownWorkplaceNames.Count, ambiguousWorkplaceNames.Count,
                coatingFlags);

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["angelegt"] = plan.ToCreate.Count,
                ["vermisst_neu"] = plan.ToMarkMissing.Count,
                ["wieder_da"] = plan.ToClearMissing.Count,
                ["umhaengung_konflikt"] = plan.ReparentConflicts.Count,
                ["statuszeilen_ergaenzt"] = statusRowsCreated,
                ["werkbank_gesetzt"] = werkbankGesetzt,
                ["werkbank_abweichend"] = werkbankAbweichend,
                ["arbeitsbereich_unbekannt"] = unknownWorkplaceNames.Count,
                ["arbeitsbereich_mehrdeutig"] = ambiguousWorkplaceNames.Count,
                ["lackflag_gesetzt"] = coatingFlags,
            }, ct: ct);

            return new FaMaterializationResult(plan.ToCreate.Count, plan.ToMarkMissing.Count,
                plan.ToClearMissing.Count, plan.ReparentConflicts.Count, Skipped: false, DryRun: false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "FA-Materialisierung fehlgeschlagen.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw; // Fehlermail über RunResilientAsync im SyncWorker
        }
    }

    private async Task SendMissingDigestAsync(IReadOnlyList<string> missing, CancellationToken ct)
    {
        if (missing.Count == 0) return;
        if (!await ServiceSettings.GetBoolAsync(_config, "ErrorNotification:Enabled", false, ct)) return;
        var recipientsRaw = await ServiceSettings.GetValueAsync(_config, "ErrorNotification:Recipients", ct);
        var recipients = (recipientsRaw ?? string.Empty)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (recipients.Length == 0) return;

        var list = string.Join(", ", missing);
        var subject = $"FA-Materialisierung: {missing.Count} FA nicht mehr in Sage";
        var html = $"<p>Beim FA-Materialisierungs-Lauf sind folgende {missing.Count} Aufträge nicht mehr " +
                   $"in der Sage-Struktur (Status: vermisst, nicht gelöscht):</p><p>{list}</p>";
        var text = $"FA-Materialisierung: {missing.Count} FA nicht mehr in Sage:\n{list}";
        await _mail.SendAsync(subject, html, recipients, text, ct);
    }

    private async Task SendUnknownWorkplaceDigestAsync(IReadOnlyCollection<string> unknown, CancellationToken ct)
    {
        if (unknown.Count == 0) return;
        if (!await ServiceSettings.GetBoolAsync(_config, "ErrorNotification:Enabled", false, ct)) return;
        var recipientsRaw = await ServiceSettings.GetValueAsync(_config, "ErrorNotification:Recipients", ct);
        var recipients = (recipientsRaw ?? string.Empty)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (recipients.Length == 0) return;

        var list = string.Join(", ", unknown.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
        var subject = $"FA-Materialisierung: {unknown.Count} unbekannte Arbeitsbereiche ohne Werkbank-Stammsatz";
        var html = $"<p>Beim FA-Materialisierungs-Lauf wurden folgende {unknown.Count} Arbeitsbereiche " +
                   $"ohne passenden Werkbank-Stammsatz gefunden (keine Zuweisung, keine Neuanlage):</p><p>{list}</p>";
        var text = $"FA-Materialisierung: {unknown.Count} unbekannte Arbeitsbereiche ohne Werkbank-Stammsatz:\n{list}";
        await _mail.SendAsync(subject, html, recipients, text, ct);
    }

    /// <summary>Werkbank-Kurzform fuer den Name-Match (anonyme Typen sind nicht uebergabefaehig).</summary>
    private sealed record WorkplaceRef(int Id, string Name);

    /// <summary>
    /// Weist die Werkbank aus dem Arbeitsbereich zu (Variante B). Genutzt von Anlege- UND
    /// Update-Pfad — derselbe Code, damit sich der Bestandsunterschied zwischen beiden Zweigen
    /// (Ursache des UAT-Befunds) nicht wiederholt. Sage ist führend: ein abweichender
    /// Bestandswert wird überschrieben (B4 — die Meldung bleibt bewusst unscharf, der Sync kann
    /// nicht wissen, ob der Wert von Hand gesetzt oder in Sage umgeplant wurde). Unbekannter
    /// oder mehrdeutiger Name: melden, NICHT zuweisen, Bestandswert bleibt unangetastet.
    /// </summary>
    /// <returns>true, wenn der Wert geändert wurde.</returns>
    private bool ApplyWorkplace(ProductionOrder order, string? arbeitsbereich,
        IReadOnlyDictionary<string, List<WorkplaceRef>> groups,
        ISet<string> unknown, ISet<string> ambiguous, IList<string> deviations)
    {
        if (string.IsNullOrWhiteSpace(arbeitsbereich)) return false;
        var name = arbeitsbereich.Trim();

        if (!groups.TryGetValue(name, out var matches)) { unknown.Add(name); return false; }
        if (matches.Count > 1) { ambiguous.Add(name); return false; }

        var targetId = matches[0].Id;
        if (order.ProductionWorkplaceId == targetId) return false;

        // B4: Der Sync kann NICHT unterscheiden, ob der Bestandswert von Hand gesetzt oder in
        // Sage umgeplant wurde. Die Meldung bleibt deshalb bewusst unscharf.
        if (order.ProductionWorkplaceId.HasValue)
            deviations.Add($"Sub-FA {order.SubOrderNumber}: Werkbank weicht vom Quellwert ab " +
                           $"(bisher Id {order.ProductionWorkplaceId}, Quelle '{name}')");

        order.ProductionWorkplaceId = targetId;
        return true;
    }

    /// <summary>
    /// Stellt fuer alle materialisierten Sub-FAs die beiden 1:1-Statuszeilen sicher
    /// (<see cref="ProductionOrderPickingStatus"/>, <see cref="ProductionOrderBdeStatus"/>).
    /// Idempotent — legt nur an, was fehlt; bestehende Werte werden nie angefasst
    /// (Muster: WHEN NOT MATCHED BY TARGET im AKE-Eager-Create).
    /// </summary>
    private async Task<int> EnsureStatusRowsAsync(
        IEnumerable<string> subOrderNumbers, DateTime now, CancellationToken ct)
    {
        var keys = subOrderNumbers.ToList();
        if (keys.Count == 0) return 0;

        var ids = await _ctx.ProductionOrders
            .Where(o => o.SubOrderNumber != null && keys.Contains(o.SubOrderNumber))
            .Select(o => o.Id)
            .ToListAsync(ct);
        if (ids.Count == 0) return 0;

        var withPicking = await _ctx.ProductionOrderPickingStatuses
            .Where(s => ids.Contains(s.ProductionOrderId))
            .Select(s => s.ProductionOrderId).ToListAsync(ct);
        var withBde = await _ctx.ProductionOrderBdeStatuses
            .Where(s => ids.Contains(s.ProductionOrderId))
            .Select(s => s.ProductionOrderId).ToListAsync(ct);

        var created = 0;
        foreach (var id in ids.Except(withPicking))
        {
            _ctx.ProductionOrderPickingStatuses.Add(new ProductionOrderPickingStatus
            {
                ProductionOrderId = id,
                CreatedAt = now, CreatedBy = ServiceUser, CreatedByWindows = ServiceUser
            });
            created++;
        }
        foreach (var id in ids.Except(withBde))
        {
            _ctx.ProductionOrderBdeStatuses.Add(new ProductionOrderBdeStatus
            {
                ProductionOrderId = id,
                CreatedAt = now, CreatedBy = ServiceUser, CreatedByWindows = ServiceUser
            });
            created++;
        }

        if (created > 0) await _ctx.SaveChangesAsync(ct);
        return created;
    }
}
