using System.Globalization;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.EntityFrameworkCore;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>Zeile aus <c>EinkaufPositionenInfo</c> (Namen exakt wie in der SData-Antwort, Typen laut E0/P-W1 am AKE-Test).</summary>
public sealed record SdataEinkaufPositionZeile(int? BelId, int? BelPosId, string? Belegkennzeichen, string? BelegnummerFormatiert,
    string? A0Empfaenger, string? A0Name1, int? Liefersperre, string? Liefertermin, string? BelegLiefertermin, string? Artikelnummer,
    string? Bezeichnung1, string? Bezeichnung2, int? Chargenpflicht, int? Nachweispflicht, string? Mengeneinheit, string? MengeBasiseinheit,
    string? Lagermengeneinheit, decimal? PositionGGBestellt, decimal? PositionGGGeliefert, int? PlatzID, string? Kurzbezeichnung);

/// <summary>
/// v1.84.0, Spec 2026-11-08-wareneingang-aus-sage-bestellungen E1 (+ Entscheidung E0-Blocker): liest offene Sage-Einkaufsbestellungen aus
/// SData <c>EinkaufPositionenInfo</c> (die Sage-View liefert nur offene Positionen) in den Cache <see cref="SagePurchaseOrderLine"/>.
/// <list type="bullet">
/// <item>Nur <c>EBB</c> (normale Bestellung), per <c>where Belegkennzeichen eq 'EBB'</c> – Rahmenabrufe <c>EBZ</c> teilen sich Vorgangsmengen (B-1).</item>
/// <item>Kopfdaten aus den Positionen (<c>EinkaufBelegekopfInfo</c> liefert mehrere Zeilen je Beleg, B-3); <c>Liefersperre</c> = Lieferant gesperrt.</item>
/// <item>Mengen in der Basiseinheit; <c>Mengeneinheit ≠ Lagermengeneinheit</c> → <see cref="SagePurchaseOrderLine.EinheitAbweichend"/> (B-2).</item>
/// <item>Zuordnung Artikel / Kreditor / Lagerplatz; nicht zuordenbar = Warnung mit Beispielen, Zeile bleibt mit leerem Verweis.</item>
/// <item>Vollersatz je Lauf in einer Transaktion; Empty-Guard: 0 Positionen bei nicht leerem Cache = Warnung, Cache unveraendert.</item>
/// </list>
/// Kein View-Weg, keine Quellen-Weiche (SData-only wie <see cref="ArtikelLieferantSyncService"/>). Nie <c>EinkaufPositionenAllInfo</c>.
/// Gate <c>Sync:EinkaufBestellungenEnabled</c>. Lesefehler: Lauf Fehler + throw (Mail ueber RunResilientAsync). Schreibt nie nach Sage.
/// </summary>
public class EinkaufBestellungenSyncService
{
    public const string Ressource = "EinkaufPositionenInfo";
    public const string Where = "Belegkennzeichen eq 'EBB'";
    public const string Select = "BelId,BelPosId,Belegkennzeichen,BelegnummerFormatiert,A0Empfaenger,A0Name1,Liefersperre,Liefertermin,"
        + "BelegLiefertermin,Artikelnummer,Bezeichnung1,Bezeichnung2,Chargenpflicht,Nachweispflicht,Mengeneinheit,MengeBasiseinheit,"
        + "Lagermengeneinheit,PositionGGBestellt,PositionGGGeliefert,PlatzID,Kurzbezeichnung";
    public const string OrderBy = "BelPosId";
    public const string Belegart = "EBB";
    public const int Seitengroesse = 1000;

    private readonly ApplicationDbContext _ctx;
    private readonly ISageSdataReadClient _client;
    private readonly IConfiguration _config;
    private readonly ILogger<EinkaufBestellungenSyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public EinkaufBestellungenSyncService(ApplicationDbContext ctx, ISageSdataReadClient client, IConfiguration config,
        ILogger<EinkaufBestellungenSyncService> logger, ISyncLogger syncLogger) // ISyncLogger als LETZTER Ctor-Parameter (ADR 0010)
    {
        _ctx = ctx;
        _client = client;
        _config = config;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    public async Task<SyncResult> RunAsync(bool dryRun, CancellationToken ct = default, SdataLeseZiel? ziel = null)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.EinkaufBestellungen, ct);

        IReadOnlyList<SdataEinkaufPositionZeile> zeilen;
        // v1.99.4 (Wareneingang Teil 2, F5): Stand VOR dem Lesen - Rest-Regel vergleicht SageWeAm <= SyncedAt
        var now = DateTime.Now;
        try
        {
            ziel ??= await SdataLeseZielLeser.LesenAsync(_config, ct);
            var seiten = await _client.QueryAllAsync<SdataEinkaufPositionZeile>(ziel, SageSdataReadClient.VertragResources, Ressource,
                Where, Select, OrderBy, ct, Seitengroesse);
            zeilen = seiten.Zeilen;
            await run.LogInfoAsync(seiten.Zusammenfassung, ct: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Offene Bestellungen aus Sage (SData) nicht lesbar.");
            await run.LogErrorAsync($"Sage (SData) nicht lesbar: {ex.Message}", ct: ct);
            await run.FinishFailedAsync($"Sage (SData) nicht lesbar: {ex.Message}", ct: ct);
            throw;
        }

        try
        {
            var bestand = await _ctx.SagePurchaseOrderLines.ToListAsync(ct);
            if (zeilen.Count == 0 && bestand.Count > 0)
            {
                await run.LogWarningAsync($"{Ressource} liefert 0 offene Positionen ({Where}), Cache hat {bestand.Count} – Cache bleibt unveraendert (Empty-Guard).", ct: ct);
                await run.FinishSuccessAsync(new Dictionary<string, int> { ["positionen"] = 0, ["uebernommen"] = 0 }, ct: ct);
                return new SyncResult(0, 0, 0);
            }

            var artikel = (await _ctx.Articles.AsNoTracking().Select(a => new { a.Id, a.ArticleNumber }).ToListAsync(ct))
                .GroupBy(a => a.ArticleNumber.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);
            var kreditoren = (await _ctx.Creditors.AsNoTracking().Where(c => c.SageKto != null).Select(c => new { c.Id, c.SageKto }).ToListAsync(ct))
                .GroupBy(c => c.SageKto!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);
            var plaetze = (await _ctx.StorageLocations.AsNoTracking().Where(l => l.SageLagerplatzId != null)
                    .Select(l => new { l.Id, l.SageLagerplatzId }).ToListAsync(ct))
                .GroupBy(l => l.SageLagerplatzId!.Value).ToDictionary(g => g.Key, g => g.First().Id);

            var neu = new List<SagePurchaseOrderLine>();
            var gesehen = new HashSet<int>();
            int ohneSchluessel = 0, doppelt = 0, belegartAus = 0, terminKaputt = 0, einheitAbweichend = 0, gesperrt = 0, mengeFehlt = 0;
            var artikelFehlt = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            var kreditorFehlt = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            var platzFehlt = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            int artikelFehltZeilen = 0, kreditorFehltZeilen = 0, platzFehltZeilen = 0;

            foreach (var z in zeilen)
            {
                var nummer = z.Artikelnummer?.Trim();
                if (z.BelPosId is not { } belPosId || z.BelId is not { } belId || string.IsNullOrEmpty(nummer)
                    || string.IsNullOrWhiteSpace(z.BelegnummerFormatiert)) { ohneSchluessel++; continue; }
                // Doppelte Absicherung zum where: nur EBB (Rahmenabrufe teilen sich Vorgangsmengen, E0 B-1).
                if (!string.Equals(z.Belegkennzeichen?.Trim(), Belegart, StringComparison.OrdinalIgnoreCase)) { belegartAus++; continue; }
                if (!gesehen.Add(belPosId)) { doppelt++; continue; }

                var kto = Text(z.A0Empfaenger);
                int? articleId = artikel.TryGetValue(nummer, out var aId) ? aId : null;
                int? creditorId = kto != null && kreditoren.TryGetValue(kto, out var cId) ? cId : null;
                int? platzId = z.PlatzID is { } pid && pid > 0 ? pid : null;
                int? storageLocationId = platzId is { } p2 && plaetze.TryGetValue(p2, out var sId) ? sId : null;
                if (articleId == null) { artikelFehlt.Add(nummer); artikelFehltZeilen++; }
                if (creditorId == null) { kreditorFehlt.Add(kto ?? "(leer)"); kreditorFehltZeilen++; }
                if (platzId != null && storageLocationId == null) { platzFehlt.Add($"{Text(z.Kurzbezeichnung) ?? "?"} (ID {platzId})"); platzFehltZeilen++; }

                var termin = Datum(z.Liefertermin) ?? Datum(z.BelegLiefertermin);
                if (termin == null && (!string.IsNullOrWhiteSpace(z.Liefertermin) || !string.IsNullOrWhiteSpace(z.BelegLiefertermin))) terminKaputt++;
                if (z.PositionGGBestellt == null || z.PositionGGGeliefert == null) mengeFehlt++; // QA Q-6: melden statt still 0
                var bestellt = z.PositionGGBestellt ?? 0m;
                var geliefert = z.PositionGGGeliefert ?? 0m;
                // E0 B-2 + Nachbesserung v1.86.0: gebucht wird die Menge in der BASISeinheit auf einen Artikel mit Lagereinheit -> gesperrt, wenn
                // Bestell- ODER Basiseinheit von der Lagereinheit abweicht (EK003604 am AKE-Test: Bestellung PKG, Basis Stk, Lager PKG).
                var abweichend = !string.Equals(Text(z.Mengeneinheit), Text(z.Lagermengeneinheit), StringComparison.OrdinalIgnoreCase)
                              || !string.Equals(Text(z.MengeBasiseinheit), Text(z.Lagermengeneinheit), StringComparison.OrdinalIgnoreCase);
                if (abweichend) einheitAbweichend++;
                var sperre = (z.Liefersperre ?? 0) != 0;
                if (sperre) gesperrt++;

                neu.Add(new SagePurchaseOrderLine
                {
                    BelPosId = belPosId, BelId = belId, Belegkennzeichen = Belegart, Belegnummer = z.BelegnummerFormatiert!.Trim(),
                    LieferantKto = kto, LieferantName = Text(z.A0Name1), LieferantGesperrt = sperre, CreditorId = creditorId,
                    Artikelnummer = nummer, ArticleId = articleId, Bezeichnung1 = Text(z.Bezeichnung1), Bezeichnung2 = Text(z.Bezeichnung2),
                    MengeBestellt = bestellt, MengeGeliefert = geliefert, MengeOffen = bestellt - geliefert,
                    Mengeneinheit = Text(z.Mengeneinheit), MengeBasiseinheit = Text(z.MengeBasiseinheit), Lagermengeneinheit = Text(z.Lagermengeneinheit),
                    EinheitAbweichend = abweichend, SagePlatzId = platzId, SagePlatzKurzbezeichnung = Text(z.Kurzbezeichnung),
                    StorageLocationId = storageLocationId, Liefertermin = termin, Chargenpflicht = (z.Chargenpflicht ?? 0) != 0,
                    Nachweispflicht = (z.Nachweispflicht ?? 0) != 0, SyncedAt = now,
                });
            }

            if (ohneSchluessel > 0) await run.LogWarningAsync($"{ohneSchluessel} Position(en) ohne BelId/BelPosId/Artikelnummer/Belegnummer uebersprungen.", ct: ct);
            if (doppelt > 0) await run.LogWarningAsync($"{doppelt} Position(en) mit bereits gelesener BelPosId uebersprungen (erste Zeile gilt).", ct: ct);
            if (belegartAus > 0) await run.LogInfoAsync($"{belegartAus} Position(en) anderer Belegart als {Belegart} nicht uebernommen.", ct: ct);
            if (artikelFehltZeilen > 0)
                await run.LogWarningAsync(SdataSchluessel.Kappen($"{artikelFehltZeilen} Position(en) zu Artikeln, die es im WMS nicht gibt (nicht buchbar): " +
                    string.Join(", ", artikelFehlt.Take(20))), ct: ct);
            if (kreditorFehltZeilen > 0)
                await run.LogWarningAsync(SdataSchluessel.Kappen($"{kreditorFehltZeilen} Position(en), Lieferant ohne Kreditor im WMS (Kreditoren-Abgleich " +
                    $"Sync:KreditorenEnabled aktiv?): {string.Join(", ", kreditorFehlt.Take(20))}"), ct: ct);
            if (platzFehltZeilen > 0)
                await run.LogWarningAsync(SdataSchluessel.Kappen($"{platzFehltZeilen} Position(en), Sage-Lagerplatz nicht im WMS (Lagerplatz-Abgleich): " +
                    string.Join(", ", platzFehlt.Take(20))), ct: ct);
            if (einheitAbweichend > 0)
                await run.LogInfoAsync($"{einheitAbweichend} Position(en) mit Bestell- oder Basiseinheit ≠ Lagermengeneinheit – im WMS markiert und nicht buchbar.", ct: ct);
            if (mengeFehlt > 0)
                await run.LogWarningAsync($"{mengeFehlt} Position(en) ohne PositionGGBestellt/PositionGGGeliefert – fehlender Wert als 0 gefuehrt, bitte in Sage pruefen.", ct: ct);
            if (terminKaputt > 0) await run.LogWarningAsync($"{terminKaputt} Position(en) mit unlesbarem Liefertermin – als „ohne Termin“ gefuehrt.", ct: ct);

            if (!dryRun)
            {
                await using var tx = _ctx.Database.IsRelational() ? await _ctx.Database.BeginTransactionAsync(ct) : null;
                _ctx.SagePurchaseOrderLines.RemoveRange(bestand);
                _ctx.SagePurchaseOrderLines.AddRange(neu);
                await _ctx.SaveChangesAsync(ct);
                if (tx != null) await tx.CommitAsync(ct);
            }

            var uebersprungen = ohneSchluessel + doppelt + belegartAus;
            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["positionen"] = zeilen.Count,
                ["koepfe"] = neu.Select(n => n.BelId).Distinct().Count(),
                ["uebernommen"] = neu.Count,
                ["belegart_ausgeschlossen"] = belegartAus,
                ["artikel_fehlt"] = artikelFehltZeilen,
                ["kreditor_fehlt"] = kreditorFehltZeilen,
                ["lagerplatz_fehlt"] = platzFehltZeilen,
                ["einheit_abweichend"] = einheitAbweichend,
                ["lieferant_gesperrt"] = gesperrt,
                ["menge_fehlt"] = mengeFehlt,
                ["uebersprungen"] = uebersprungen,
            }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
            return new SyncResult(neu.Count, 0, uebersprungen);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Abgleich offene Bestellungen fehlgeschlagen.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }

    private static string? Text(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private static DateTime? Datum(string? s) =>
        !string.IsNullOrWhiteSpace(s) && DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var t)
            ? t.Date : null;
}
