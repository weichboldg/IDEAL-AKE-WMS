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
/// werden bei Updates NIE überschrieben — AUSNAHME ist genau eine (Z1):
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
    private readonly IProductionOrderPickingStatusRepository _pickingStatus;
    private readonly ILogger<FaMaterializationSyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public FaMaterializationSyncService(
        ApplicationDbContext ctx,
        IConfiguration config,
        IMailService mail,
        IProductionOrderPickingStatusRepository pickingStatus,
        ILogger<FaMaterializationSyncService> logger,
        ISyncLogger syncLogger) // ISyncLogger als LETZTER Ctor-Parameter (ADR 0010)
    {
        _ctx = ctx;
        _config = config;
        _mail = mail;
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
                    Description1 = n.Bezeichnung1,
                    Description2 = n.Bezeichnung2,
                    IsDone = false,
                    SageMissingSince = null,
                    CreatedAt = now, CreatedBy = ServiceUser, CreatedByWindows = ServiceUser,
                };
                _ctx.ProductionOrders.Add(newOrder);
            }

            // (2)/(3) Bestehende: Quell-Felder aktualisieren (App-Felder erhalten), missing setzen/klären.
            foreach (var o in orders)
            {
                var touched = false;
                if (toMark.Contains(o.SubOrderNumber)) { o.SageMissingSince = now; touched = true; }
                else if (toClear.Contains(o.SubOrderNumber)) { o.SageMissingSince = null; touched = true; }

                if (nodeByKey.TryGetValue(o.SubOrderNumber, out var sn) && srcByKey.ContainsKey(o.SubOrderNumber))
                {
                    o.Quantity = sn.Sollmenge;
                    o.ArticleNumber = sn.Artnr;
                    o.Description1 = sn.Bezeichnung1;
                    o.Description2 = sn.Bezeichnung2;
                    // ParentSubOrderNumber NUR ohne Konflikt übernehmen (Regel 3: Umhängung nicht still).
                    if (!conflictKeys.Contains(o.SubOrderNumber))
                        o.ParentSubOrderNumber = sn.VaterFA?.ToString();
                    touched = true;
                }
                if (touched)
                {
                    o.ModifiedAt = now; o.ModifiedBy = ServiceUser; o.ModifiedByWindows = ServiceUser;
                }
            }

            await _ctx.SaveChangesAsync(ct);

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
                "{Status} Statuszeilen ergänzt, {Coating} Lack-Flags gesetzt.",
                plan.ToCreate.Count, plan.ToMarkMissing.Count, plan.ToClearMissing.Count, plan.ReparentConflicts.Count,
                statusRowsCreated, coatingFlags);

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["angelegt"] = plan.ToCreate.Count,
                ["vermisst_neu"] = plan.ToMarkMissing.Count,
                ["wieder_da"] = plan.ToClearMissing.Count,
                ["umhaengung_konflikt"] = plan.ReparentConflicts.Count,
                ["statuszeilen_ergaenzt"] = statusRowsCreated,
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
