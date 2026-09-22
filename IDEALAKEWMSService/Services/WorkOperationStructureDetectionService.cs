using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SyncLogger;
using IDEALAKEWMSService.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// IDEAL: Arbeitsgang-Erkennung aus der Struktur. Leitet <c>WorkOperation</c>-Zeilen explizit aus
/// <c>FaHierarchyNode.Arbeitsschritte</c> ab (leerzeichen-getrennte Token je Position) und matcht sie
/// exakt gegen den Werkbank-Katalog (<c>ProductionWorkplace.ArbeitsschrittCode</c>, Baustein a), statt
/// wie <see cref="FaWorkStepDetectionService"/> (AKE) aus Bezeichnungen zu raten.
///
/// 2c-Umbau des frueheren <c>FaWorkStepStructureDetectionService</c> (v1.41.0, nie deployt): identische
/// Struktur (Full-Refresh-Cache, DirectChildren-Scope, Nur-hinzufuegen-Semantik, Fertig-/Storno-Filter,
/// Melden-statt-Anlegen) — nur Ziel-Tabelle (<c>WorkOperation</c> statt <c>FaWorkStep</c>) und Katalog
/// (<c>ProductionWorkplace</c> statt <c>WorkStep</c>) wechseln. <c>WorkOperation</c> hat KEINEN
/// Katalog-Fremdschluessel (<c>OperationNumber</c>/<c>Name</c> sind Plain-Strings) — der Zwei-Lauf-Ablauf
/// von v1.41.0 entfaellt.
///
/// KEIN interner Master-Gate — der Doppel-Gate (Master +
/// <c>Sync:WorkOperationStructureDetectionEnabled</c>) sitzt im <c>SyncWorker</c>. Drei Sonderfaelle
/// gegenueber v1.41.0: Kuerzel auf der Ausschlussliste (dieselbe Liste wie Baustein a) erzeugen weder
/// Arbeitsgang noch Unbekannt-Meldung (bewusst BEKANNT); ein Kuerzel, das mehrere Werkbaenke trifft, wird
/// als „mehrdeutig" gemeldet (kein Insert); unbekannte Kuerzel ohne Werkbank werden wie bisher gemeldet.
/// </summary>
public class WorkOperationStructureDetectionService : IWorkOperationStructureDetectionService
{
    private const string ServiceUser = "WorkOperationStructureDetection";
    private static readonly char[] TokenSeparators = { ' ' };

    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly IMailService _mail;
    private readonly IUnknownArbeitsschrittTokenState _unknownTokens;
    private readonly ILogger<WorkOperationStructureDetectionService> _logger;
    private readonly ISyncLogger _syncLogger;

    public WorkOperationStructureDetectionService(
        ApplicationDbContext db,
        IConfiguration config,
        IMailService mail,
        IUnknownArbeitsschrittTokenState unknownTokens,
        ILogger<WorkOperationStructureDetectionService> logger,
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
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.WorkOperationStructureDetection, ct);
        try
        {
            // Ausschlussliste (dieselbe Standorteinstellung wie Baustein a). Safe-Variante: ein transienter
            // Settings-DB-Fehler soll den Lauf nicht abwuergen (leere Liste) und macht die Logik testbar.
            var ausschluss = (await ServiceSettings.GetValueSafeAsync(_config, "Sync:ProductionWorkplaceSyncAusschlussliste", ct) ?? string.Empty)
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 1) Vollstaendiger Struktur-Stand (Full-Refresh-Cache, analog FaMaterializationSyncService).
            var nodes = await _db.FaHierarchyNodes.AsNoTracking().ToListAsync(ct);

            // 2) DirectChildren-Lookup: VaterFA -> Kinder (eine Gruppierung, kein N+1).
            var childrenByParent = nodes
                .Where(n => n.VaterFA.HasValue)
                .GroupBy(n => n.VaterFA!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 3) Katalog: ProductionWorkplace je ArbeitsschrittCode (getrimmt, case-insensitiv). Liste je
            //    Kuerzel, damit Mehrdeutigkeit (dasselbe Kuerzel auf mehreren Werkbaenken) erkennbar bleibt.
            var wpByCode = new Dictionary<string, List<ProductionWorkplace>>(StringComparer.OrdinalIgnoreCase);
            foreach (var wp in await _db.ProductionWorkplaces
                         .Where(w => w.ArbeitsschrittCode != null && w.ArbeitsschrittCode != "")
                         .ToListAsync(ct))
            {
                var code = wp.ArbeitsschrittCode!.Trim();
                if (code.Length == 0) continue;
                (wpByCode.TryGetValue(code, out var l) ? l : wpByCode[code] = new List<ProductionWorkplace>()).Add(wp);
            }

            // 4) Je materialisiertem Sub-FA (SubFA != 0): DirectChildren-Scope = eigene Zeile + direkte
            //    Kinder; Token in Erst-Auftauch-Reihenfolge (fuer WorkOperation.Sequence).
            var desiredBySubOrder = new Dictionary<string, Dictionary<string, (ProductionWorkplace Wp, int Seq)>>(StringComparer.Ordinal);
            var unknownTokenToSubOrders = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var ambiguousTokenToSubOrders = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            foreach (var node in nodes)
            {
                if (node.SubFA == 0) continue;
                ct.ThrowIfCancellationRequested();

                var scope = new List<FaHierarchyNode> { node };
                if (childrenByParent.TryGetValue(node.SubFA, out var kids)) scope.AddRange(kids);

                var subOrder = node.SubFA.ToString();
                var seq = 0;
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var token in scope
                             .Where(n => !string.IsNullOrWhiteSpace(n.Arbeitsschritte))
                             .SelectMany(n => n.Arbeitsschritte!.Split(TokenSeparators,
                                 StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
                {
                    if (!seen.Add(token)) continue; // je Sub-FA jedes Kuerzel nur einmal, Reihenfolge = Erst-Auftauchen
                    var position = seq++;

                    if (ausschluss.Contains(token)) continue; // BEKANNT ausgeschlossen: kein Arbeitsgang, keine Meldung

                    if (wpByCode.TryGetValue(token, out var matches))
                    {
                        if (matches.Count > 1)
                        {
                            AddTo(ambiguousTokenToSubOrders, token, subOrder);
                            continue;
                        }
                        if (!desiredBySubOrder.TryGetValue(subOrder, out var map))
                            desiredBySubOrder[subOrder] = map = new Dictionary<string, (ProductionWorkplace, int)>(StringComparer.OrdinalIgnoreCase);
                        map[token] = (matches[0], position);
                    }
                    else
                    {
                        AddTo(unknownTokenToSubOrders, token, subOrder);
                    }
                }
            }

            int added = 0, skipped = 0;

            if (desiredBySubOrder.Count > 0)
            {
                var keys = desiredBySubOrder.Keys.ToList();
                // 5) EINE gebuendelte ProductionOrder-Abfrage — gleicher Fertig-/Storno-Filter wie v1.41.0/AKE.
                var orders = await _db.ProductionOrders
                    .Where(o => o.SubOrderNumber != null && keys.Contains(o.SubOrderNumber)
                             && !o.IsDone && !o.IsCancelled
                             && !(o.PickingStatus != null && o.PickingStatus.IsDonePicking))
                    .Select(o => new { o.Id, o.OrderNumber, o.SubOrderNumber })
                    .ToListAsync(ct);

                // 6) Bestehende (ProductionOrderId, OperationNumber) sperren das Re-Add (Nur-hinzufuegen).
                var orderIds = orders.Select(o => o.Id).ToList();
                var existingSet = new HashSet<(int, string)>(
                    (await _db.WorkOperations
                        .Where(w => orderIds.Contains(w.ProductionOrderId))
                        .Select(w => new { w.ProductionOrderId, w.OperationNumber })
                        .ToListAsync(ct))
                    .Select(e => (e.ProductionOrderId, e.OperationNumber)));

                foreach (var o in orders)
                {
                    if (!desiredBySubOrder.TryGetValue(o.SubOrderNumber!, out var tokenMap)) continue;
                    foreach (var (token, val) in tokenMap)
                    {
                        if (existingSet.Contains((o.Id, token))) { skipped++; continue; }
                        if (!dryRun)
                        {
                            _db.WorkOperations.Add(new WorkOperation
                            {
                                ProductionOrderId = o.Id,
                                OperationNumber = token,
                                Name = val.Wp.Name,
                                ProductionWorkplaceId = val.Wp.Id,
                                Sequence = val.Seq,
                                IsReportable = true,
                                IsExternalSystem = false,
                                IsReported = false,
                                CreatedAt = DateTime.Now,
                                CreatedBy = ServiceUser,
                                CreatedByWindows = ServiceUser,
                            });
                        }
                        added++;
                        await run.LogInfoAsync(
                            $"FA {o.OrderNumber} → AG {token} {val.Wp.Name} erkannt (Struktur, Werkbank {val.Wp.SageArbeitsplatznummer})",
                            reference: o.OrderNumber, ct: ct);
                    }
                }

                if (!dryRun) await _db.SaveChangesAsync(ct);
            }

            // 7) Kuerzel-Mehrdeutigkeit (Absicherung, laut Datenbefund heute nicht vorhanden): JEDER Lauf
            //    loggt sie — kein Insert. Kein eigener S1-Mailzustand (kann derzeit nicht feuern, YAGNI).
            int ambiguousTokenCount = ambiguousTokenToSubOrders.Count;
            if (ambiguousTokenCount > 0)
            {
                var detail = string.Join(", ", ambiguousTokenToSubOrders
                    .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(kv => $"{kv.Key} ({kv.Value.Count} Auftraege)"));
                await run.LogWarningAsync(
                    $"Mehrdeutige Arbeitsschritt-Kuerzel ({ambiguousTokenCount}): {detail}. Je Kuerzel treffen mehrere " +
                    "Werkbaenke (gleicher ArbeitsschrittCode) — kein Arbeitsgang angelegt. Doppelte Kuerzel in den " +
                    "Sage-Arbeitsplaetzen bereinigen.", ct: ct);
            }

            // 8) Unbekannte Token: JEDER Lauf loggt die Sammelmeldung; Sammelmail nur bei AENDERUNG der
            //    Token-Menge (S1). Ausgeschlossene Kuerzel sind hier bewusst NICHT enthalten (bekannt).
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
                    $"Unbekannte Arbeitsschritte ohne Werkbank-Zuordnung ({unknownTokenCount} Tokens, " +
                    $"{unknownOrderCount} Auftraege betroffen): {detail}. Keine Anlage — je Kuerzel den Sage-" +
                    "Arbeitsplatz mit passendem USER_ArbeitsSchritt pflegen; der naechste Lauf legt die Arbeitsgaenge an.",
                    ct: ct);
                if (unknownChanged) await SendUnknownTokenDigestAsync(unknownTokenToSubOrders, ct);
            }

            _logger.LogInformation(
                "WorkOperation-Struktur-Erkennung abgeschlossen: {Added} neu, {Skipped} uebersprungen, " +
                "{Ambig} mehrdeutig, {UnknownTok} unbekannte Token ({UnknownOrd} Auftraege){DryRun}",
                added, skipped, ambiguousTokenCount, unknownTokenCount, unknownOrderCount, dryRun ? " [DryRun]" : "");

            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["neu"] = added,
                ["uebersprungen"] = skipped,
                ["arbeitsschritt_mehrdeutig"] = ambiguousTokenCount,
                ["arbeitsschritt_unbekannt"] = unknownTokenCount,
                ["arbeitsschritt_unbekannt_auftraege"] = unknownOrderCount,
            }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);

            return new SyncResult(added, 0, 0);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Fehler bei WorkOperation-Struktur-Erkennung.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }

    private static void AddTo(Dictionary<string, HashSet<string>> map, string token, string subOrder)
    {
        if (!map.TryGetValue(token, out var set))
            map[token] = set = new HashSet<string>(StringComparer.Ordinal);
        set.Add(subOrder);
    }

    /// <summary>Sammelmail (EINE pro Aenderung) — Muster 1:1 aus dem v1.41.0-Vorbild.</summary>
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
        var subject = $"WorkOperation-Struktur-Erkennung: {unknown.Count} unbekannte Arbeitsschritt-Kuerzel";
        var html = $"<p>Beim Struktur-Arbeitsgang-Lauf wurden {unknown.Count} Arbeitsschritt-Kuerzel " +
                   $"({orderCount} Auftraege) ohne passende Werkbank gefunden (keine Anlage):</p><ul>{rows}</ul>" +
                   "<p>Bitte je Kuerzel den Sage-Arbeitsplatz mit passendem <b>USER_ArbeitsSchritt</b> pflegen; " +
                   "der naechste Lauf legt die Arbeitsgaenge dann an.</p>";
        var text = $"WorkOperation-Struktur-Erkennung: {unknown.Count} unbekannte Kuerzel " +
                   $"({orderCount} Auftraege):\n{listText}";
        await _mail.SendAsync(subject, html, recipients, text, ct);
    }
}
