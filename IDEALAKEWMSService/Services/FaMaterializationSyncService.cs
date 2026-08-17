using IdealAkeWms.Data;
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
/// (SubFA != 0). App-verwaltete Felder (IsDone/PickingStatus/BdeStatus/Workplace/Storno/ExtraInfo)
/// werden bei Updates NIE überschrieben.
/// </summary>
public class FaMaterializationSyncService : IFaMaterializationSyncService
{
    private const string ServiceUser = "IDEALAKEWMSService";

    private readonly ApplicationDbContext _ctx;
    private readonly IConfiguration _config;
    private readonly IMailService _mail;
    private readonly ILogger<FaMaterializationSyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public FaMaterializationSyncService(
        ApplicationDbContext ctx,
        IConfiguration config,
        IMailService mail,
        ILogger<FaMaterializationSyncService> logger,
        ISyncLogger syncLogger) // ISyncLogger als LETZTER Ctor-Parameter (ADR 0010)
    {
        _ctx = ctx;
        _config = config;
        _mail = mail;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    public async Task<FaMaterializationResult> RunAsync(bool dryRun, CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.FaMaterialization, ct);
        try
        {
            // Quelle: lokale FA-Struktur, Nicht-Blatt-Knoten (SubFA != 0). Mapping int->string.
            var nodes = await _ctx.FaHierarchyNodes.AsNoTracking()
                .Where(n => n.SubFA != 0)
                .ToListAsync(ct);

            var source = nodes
                .Select(n => new MaterializationSourceOrder(
                    n.SubFA.ToString(),
                    n.HauptFA.ToString(),
                    n.VaterFA.HasValue ? n.VaterFA.Value.ToString() : null))
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
                }, messageSuffix: "[DryRun]", ct: ct);
                return new FaMaterializationResult(plan.ToCreate.Count, plan.ToMarkMissing.Count,
                    plan.ToClearMissing.Count, plan.ReparentConflicts.Count, Skipped: false, DryRun: true);
            }

            var now = DateTime.Now;
            var srcByKey = source.ToDictionary(s => s.SubOrderNumber, StringComparer.Ordinal);
            var nodeByKey = new Dictionary<string, FaHierarchyNode>(StringComparer.Ordinal);
            foreach (var n in nodes) nodeByKey.TryAdd(n.SubFA.ToString(), n);
            var conflictKeys = plan.ReparentConflicts.Select(c => c.SubOrderNumber).ToHashSet(StringComparer.Ordinal);
            var toClear = plan.ToClearMissing.ToHashSet(StringComparer.Ordinal);
            var toMark = plan.ToMarkMissing.ToHashSet(StringComparer.Ordinal);

            // (1) Neu anlegen.
            foreach (var key in plan.ToCreate)
            {
                var n = nodeByKey[key];
                _ctx.ProductionOrders.Add(new ProductionOrder
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
                });
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

            // Sammelmail (EINE pro Lauf) für neu-vermisste, nicht-erledigte FAs (S7-2b).
            await SendMissingDigestAsync(plan.MissingToNotify, ct);

            _logger.LogInformation(
                "FA-Materialisierung: {Created} neu, {Marked} vermisst, {Cleared} wieder da, {Conf} Umhäng-Konflikte.",
                plan.ToCreate.Count, plan.ToMarkMissing.Count, plan.ToClearMissing.Count, plan.ReparentConflicts.Count);

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["angelegt"] = plan.ToCreate.Count,
                ["vermisst_neu"] = plan.ToMarkMissing.Count,
                ["wieder_da"] = plan.ToClearMissing.Count,
                ["umhaengung_konflikt"] = plan.ReparentConflicts.Count,
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
}
