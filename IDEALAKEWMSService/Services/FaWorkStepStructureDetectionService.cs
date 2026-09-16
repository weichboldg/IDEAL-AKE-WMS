using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SyncLogger;
using IDEALAKEWMSService.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// IDEAL: FA-Arbeitsgang-Erkennung aus der Struktur (v1.41.0). Leitet <c>FaWorkStep</c>-Zeilen
/// explizit aus <c>FaHierarchyNode.Arbeitsschritte</c> ab (leerzeichen-getrennte Token je Position),
/// statt sie wie <see cref="FaWorkStepDetectionService"/> (AKE) aus Bezeichnungen zu raten.
///
/// Bewusst EIGENER Service, kein parametrisierter Bestandsservice (Spec Design A/B): fuenf gemessene
/// Unterschiede zur AKE-Heuristik (Quelle, Match-Mechanismus, Match-Feld, Scope, Zielaufloesung) und
/// kein Eingriff in den live laufenden AKE-Pfad. KEIN interner Master-Gate — der Doppel-Gate
/// (Master + <c>Sync:FaWorkStepStructureDetectionEnabled</c>) sitzt ausschliesslich im
/// <c>SyncWorker</c> (Vorbild <see cref="FaMaterializationSyncService"/>).
///
/// Nur-hinzufuegen-Semantik identisch zum AKE-Vorbild: bestehende <c>FaWorkStep</c>-Zeilen — auch
/// manuell entfernte (<c>IsRemoved=true</c>) — sperren das Re-Add und bleiben unveraendert.
/// Unbekannte Token werden gemeldet, NICHT angelegt (Melden-statt-Anlegen, ADR 0014).
/// </summary>
public class FaWorkStepStructureDetectionService : IFaWorkStepStructureDetectionService
{
    private static readonly char[] TokenSeparators = { ' ' };

    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly IMailService _mail;
    private readonly IUnknownWorkStepTokenState _unknownTokens;
    private readonly ILogger<FaWorkStepStructureDetectionService> _logger;
    private readonly ISyncLogger _syncLogger;

    public FaWorkStepStructureDetectionService(
        ApplicationDbContext db,
        IConfiguration config,
        IMailService mail,
        IUnknownWorkStepTokenState unknownTokens,
        ILogger<FaWorkStepStructureDetectionService> logger,
        ISyncLogger syncLogger) // ISyncLogger als LETZTER Ctor-Parameter (ADR 0010)
    {
        _db = db;
        _config = config;
        _mail = mail;
        _unknownTokens = unknownTokens;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    public async Task<SyncResult> DetectAsync(bool dryRun, CancellationToken ct = default)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.FaWorkStepStructureDetection, ct);
        try
        {
            // 1) Vollstaendiger Struktur-Stand (Full-Refresh-Cache, analog FaMaterializationSyncService).
            var nodes = await _db.FaHierarchyNodes.AsNoTracking().ToListAsync(ct);

            // 2) DirectChildren-Lookup: VaterFA -> Kinder (eine Gruppierung, kein N+1).
            var childrenByParent = nodes
                .Where(n => n.VaterFA.HasValue)
                .GroupBy(n => n.VaterFA!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 3) Katalog: aktive WorkSteps, exakt-Match ueber Code (case-insensitiv, getrimmt).
            //    KEIN SearchString — das ist ausschliesslich das AKE-Heuristik-Feld (Spec AK 6).
            var stepByCode = new Dictionary<string, WorkStep>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in await _db.WorkSteps.Where(w => w.IsActive).ToListAsync(ct))
            {
                var code = s.Code?.Trim();
                if (!string.IsNullOrEmpty(code)) stepByCode[code] = s;
            }

            // 4) Je materialisiertem Sub-FA (SubFA != 0, identische Grundmenge wie FaMaterialization):
            //    DirectChildren-Scope = eigene Zeile + direkte Kinder; Token gegen Katalog matchen.
            var desiredBySubOrder = new Dictionary<string, Dictionary<int, WorkStep>>(StringComparer.Ordinal);
            var unknownTokenToSubOrders = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var node in nodes)
            {
                if (node.SubFA == 0) continue;
                ct.ThrowIfCancellationRequested();

                var scope = new List<FaHierarchyNode> { node };
                if (childrenByParent.TryGetValue(node.SubFA, out var kids)) scope.AddRange(kids);

                var tokens = scope
                    .Where(n => !string.IsNullOrWhiteSpace(n.Arbeitsschritte))
                    .SelectMany(n => n.Arbeitsschritte!.Split(TokenSeparators,
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (tokens.Count == 0) continue; // leere/null-Arbeitsschritte -> Sub-FA uebersprungen (kein Fehler)

                var subOrder = node.SubFA.ToString();
                foreach (var token in tokens)
                {
                    if (stepByCode.TryGetValue(token, out var step))
                    {
                        if (!desiredBySubOrder.TryGetValue(subOrder, out var map))
                            desiredBySubOrder[subOrder] = map = new Dictionary<int, WorkStep>();
                        map[step.Id] = step;
                    }
                    else
                    {
                        if (!unknownTokenToSubOrders.TryGetValue(token, out var set))
                            unknownTokenToSubOrders[token] = set = new HashSet<string>(StringComparer.Ordinal);
                        set.Add(subOrder);
                    }
                }
            }

            int added = 0, skipped = 0;

            if (desiredBySubOrder.Count > 0)
            {
                var keys = desiredBySubOrder.Keys.ToList();
                // 5) EINE gebuendelte ProductionOrder-Abfrage — gleicher Fertig-/Storno-Filter wie AKE-Vorbild.
                var orders = await _db.ProductionOrders
                    .Where(o => o.SubOrderNumber != null && keys.Contains(o.SubOrderNumber)
                             && !o.IsDone && !o.IsCancelled
                             && !(o.PickingStatus != null && o.PickingStatus.IsDonePicking))
                    .Select(o => new { o.Id, o.OrderNumber, o.SubOrderNumber })
                    .ToListAsync(ct);

                // 6) Bestehende (FA, Step)-Zeilen — aktiv UND IsRemoved — sperren das Re-Add.
                var orderIds = orders.Select(o => o.Id).ToList();
                var existingSet = new HashSet<(int, int)>(
                    (await _db.FaWorkSteps
                        .Where(f => orderIds.Contains(f.ProductionOrderId))
                        .Select(f => new { f.ProductionOrderId, f.WorkStepId })
                        .ToListAsync(ct))
                    .Select(e => (e.ProductionOrderId, e.WorkStepId)));

                foreach (var o in orders)
                {
                    if (!desiredBySubOrder.TryGetValue(o.SubOrderNumber!, out var stepMap)) continue;
                    foreach (var step in stepMap.Values)
                    {
                        if (existingSet.Contains((o.Id, step.Id))) { skipped++; continue; }
                        if (!dryRun)
                        {
                            _db.FaWorkSteps.Add(new FaWorkStep
                            {
                                ProductionOrderId = o.Id,
                                WorkStepId = step.Id,
                                Source = FaWorkStepSources.Struktur,
                                CreatedAt = DateTime.Now,
                                CreatedBy = "FaWorkStepStructureDetection",
                                CreatedByWindows = "FaWorkStepStructureDetection",
                            });
                        }
                        added++;
                        await run.LogInfoAsync(
                            $"FA {o.OrderNumber} → AG {step.Code} {step.Name} erkannt (Struktur)",
                            reference: o.OrderNumber, ct: ct);
                    }
                }

                if (!dryRun) await _db.SaveChangesAsync(ct);
            }

            // 7) Unbekannte Token: JEDER Lauf loggt die Sammelmeldung; Sammelmail nur bei AENDERUNG der
            //    Token-Menge gegenueber dem letzten Lauf (S1). Menge = Token-Namen-Set (nicht die Anzahl,
            //    sonst faellt eine Token-Verschiebung bei gleicher Gesamtzahl nicht auf).
            var unknownChanged = _unknownTokens.HasChanged(unknownTokenToSubOrders.Keys.ToList());
            int unknownTokenCount = unknownTokenToSubOrders.Count;
            int unknownOrderCount = unknownTokenToSubOrders.SelectMany(kv => kv.Value)
                .Distinct(StringComparer.Ordinal).Count();
            if (unknownTokenCount > 0)
            {
                var detail = string.Join(", ", unknownTokenToSubOrders
                    .OrderByDescending(kv => kv.Value.Count).ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(kv => $"{kv.Key} ({kv.Value.Count} Auftraege)"));
                await run.LogWarningAsync(
                    $"Unbekannte Arbeitsschritte ohne WorkStep-Zuordnung ({unknownTokenCount} Tokens, " +
                    $"{unknownOrderCount} Auftraege betroffen): {detail}. Keine Anlage — je Token einen " +
                    "WorkStep mit Code = Token auf /WorkSteps anlegen, der naechste Lauf legt die Arbeitsgaenge an.",
                    ct: ct);
                if (unknownChanged) await SendUnknownTokenDigestAsync(unknownTokenToSubOrders, ct);
            }

            _logger.LogInformation(
                "FA-Arbeitsgang-Erkennung (Struktur) abgeschlossen: {Added} neu, {Skipped} uebersprungen, " +
                "{UnknownTok} unbekannte Token ({UnknownOrd} Auftraege){DryRun}",
                added, skipped, unknownTokenCount, unknownOrderCount, dryRun ? " [DryRun]" : "");

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["neu"] = added,
                ["uebersprungen"] = skipped,
                ["arbeitsschritt_unbekannt"] = unknownTokenCount,
                ["arbeitsschritt_unbekannt_auftraege"] = unknownOrderCount,
            }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);

            return new SyncResult(added, 0, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler bei FA-Arbeitsgang-Erkennung (Struktur).");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }

    /// <summary>Sammelmail (EINE pro Aenderung) — Muster 1:1 aus FaMaterializationSyncService.</summary>
    private async Task SendUnknownTokenDigestAsync(
        Dictionary<string, HashSet<string>> unknown, CancellationToken ct)
    {
        if (unknown.Count == 0) return;
        if (!await ServiceSettings.GetBoolAsync(_config, "ErrorNotification:Enabled", false, ct)) return;
        var recipientsRaw = await ServiceSettings.GetValueAsync(_config, "ErrorNotification:Recipients", ct);
        var recipients = (recipientsRaw ?? string.Empty)
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (recipients.Length == 0) return;

        var orderCount = unknown.SelectMany(kv => kv.Value).Distinct(StringComparer.Ordinal).Count();
        var ordered = unknown.OrderByDescending(kv => kv.Value.Count)
                             .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase).ToList();
        var rows = string.Join("", ordered.Select(kv =>
            $"<li>{System.Net.WebUtility.HtmlEncode(kv.Key)} — {kv.Value.Count} Auftraege</li>"));
        var listText = string.Join(", ", ordered.Select(kv => $"{kv.Key} ({kv.Value.Count} Auftraege)"));
        var subject = $"FA-Arbeitsgang-Erkennung (Struktur): {unknown.Count} unbekannte Arbeitsschritt-Token";
        var html = $"<p>Beim Struktur-Arbeitsgang-Lauf wurden {unknown.Count} Arbeitsschritt-Token " +
                   $"({orderCount} Auftraege) ohne passenden WorkStep-Code gefunden (keine Anlage):</p><ul>{rows}</ul>" +
                   "<p>Bitte je Token einen WorkStep mit <b>Code = Token</b> und Klartext-Name auf /WorkSteps " +
                   "anlegen; der naechste Lauf legt die Arbeitsgaenge dann an.</p>";
        var text = $"FA-Arbeitsgang-Erkennung (Struktur): {unknown.Count} unbekannte Token " +
                   $"({orderCount} Auftraege):\n{listText}";
        await _mail.SendAsync(subject, html, recipients, text, ct);
    }
}
