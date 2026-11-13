using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Services.SyncLogger;
using Microsoft.EntityFrameworkCore;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>Zeile aus <c>ArtikelLieferantInfo</c> (Namen exakt wie in der SData-Antwort, Typen P-S1).</summary>
public sealed record SdataArtikelLieferantZeile(string? Lieferant, string? Artikelnummer, string? Bestellnummer, string? Bezeichnung1,
    string? Bezeichnung2, decimal? Mindestbestellmenge, string? Einkaufsmengeneinheit, decimal? UmrechnungsfaktorEK, int? Wiederbeschaffungszeit,
    decimal? Einzelpreis, int? DezimalstellenEK = null, decimal? UmrechnungsfaktorVPEK = null);

/// <summary>Zeile aus <c>ArtikelVariantenExtended</c> — nur der Hauptlieferant (Kto-Format, z. B. K31013).</summary>
public sealed record SdataHauptlieferantZeile(string? Artikelnummer, string? Hauptlieferant);

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E8c/F10): liest die Artikel-Lieferanten aus SData (<c>ArtikelLieferantInfo</c>) in
/// <see cref="ArticleSupplier"/> — nur fuer Artikel, die im WMS existieren, und Lieferanten, die als <see cref="Creditor"/> mit gleicher
/// <c>SageKto</c> vorhanden sind (sonst Zeile uebersprungen und gezaehlt; fehlende Kreditoren als Warnung). <c>IstHauptlieferant</c> aus
/// <c>ArtikelVariantenExtended.Hauptlieferant</c>. Die Tabelle traegt nur Sage-Daten (keine WMS-Felder): Zuordnungen, die Sage nicht mehr
/// liefert, werden entfernt (Empty-Guard: ohne zugeordnete Zeile wird nichts entfernt). Gate <c>Sync:ArtikelLieferantEnabled</c>, im
/// SyncWorker nach Artikel- und Kreditoren-Abgleich. Lesefehler: Lauf Fehler + throw (Mail ueber RunResilientAsync).
/// </summary>
public class ArtikelLieferantSyncService
{
    public const string ServiceUser = "ArtikelLieferantSync";
    public const string Ressource = "ArtikelLieferantInfo";
    public const string Select = "Lieferant,Artikelnummer,Bestellnummer,Bezeichnung1,Bezeichnung2,Mindestbestellmenge,Einkaufsmengeneinheit,"
        + "UmrechnungsfaktorEK,Wiederbeschaffungszeit,Einzelpreis,DezimalstellenEK,UmrechnungsfaktorVPEK"; // v1.85.0 (belegt 2026-11-08; VPEK gibt es nicht)
    public const string OrderBy = "Artikelnummer,Lieferant";
    public const string RessourceVarianten = "ArtikelVariantenExtended";
    public const string SelectVarianten = "Artikelnummer,Hauptlieferant";
    // where, damit die 100.000-Zeilen-Grenze ohne where nicht greift; nur Varianten mit Hauptlieferant sind interessant.
    // NICHT "ne ''"/"gt ''": Sage-SData liefert dafuer auf Textfeldern still 0 Zeilen (AKE-Test 2026-11-07: ne '' → 0, like '_%' → 23.509 von
    // 279.529, fallstricke „Sage-SData-Lesen“). like '_%' = mindestens ein Zeichen, ohne Annahme ueber das Kontonummern-Praefix.
    public const string WhereVarianten = "Hauptlieferant like '_%'";
    public const string OrderByVarianten = "Artikelnummer,AuspraegungID"; // AuspraegungID wie SdataArtikelReader (stabile Sortierung)
    // ponytail: 34.802 Zeilen am AKE-Test in grossen Seiten (wie SdataArtikelReader.SeitengroesseRessourcen).
    public const int Seitengroesse = 5000;

    private readonly ApplicationDbContext _ctx;
    private readonly ISageSdataReadClient _client;
    private readonly IConfiguration _config;
    private readonly ILogger<ArtikelLieferantSyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public ArtikelLieferantSyncService(ApplicationDbContext ctx, ISageSdataReadClient client, IConfiguration config,
        ILogger<ArtikelLieferantSyncService> logger, ISyncLogger syncLogger) // ISyncLogger als LETZTER Ctor-Parameter (ADR 0010)
    {
        _ctx = ctx;
        _client = client;
        _config = config;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    public async Task<SyncResult> RunAsync(bool dryRun, CancellationToken ct = default, SdataLeseZiel? ziel = null)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.ArtikelLieferant, ct);

        IReadOnlyList<SdataArtikelLieferantZeile> zeilen;
        IReadOnlyList<SdataHauptlieferantZeile> haupt;
        try
        {
            ziel ??= await SdataLeseZielLeser.LesenAsync(_config, ct);
            if (ziel.Warnung is { } zugangsWarnung) await run.LogWarningAsync(zugangsWarnung, ct: ct); // v1.99.14 F6: Rueckfall appsettings
            var seiten = await _client.QueryAllAsync<SdataArtikelLieferantZeile>(ziel, SageSdataReadClient.VertragResources, Ressource,
                null, Select, OrderBy, ct, Seitengroesse);
            zeilen = seiten.Zeilen;
            await run.LogInfoAsync(seiten.Zusammenfassung, ct: ct);
            var varianten = await _client.QueryAllAsync<SdataHauptlieferantZeile>(ziel, SageSdataReadClient.VertragResources, RessourceVarianten,
                WhereVarianten, SelectVarianten, OrderByVarianten, ct, Seitengroesse);
            haupt = varianten.Zeilen;
            await run.LogInfoAsync(varianten.Zusammenfassung, ct: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Artikel-Lieferanten aus Sage (SData) nicht lesbar.");
            await run.LogErrorAsync($"Sage (SData) nicht lesbar: {ex.Message}", ct: ct);
            await run.FinishFailedAsync($"Sage (SData) nicht lesbar: {ex.Message}", ct: ct);
            throw;
        }

        try
        {
            var artikel = (await _ctx.Articles.AsNoTracking().Select(a => new { a.Id, a.ArticleNumber }).ToListAsync(ct))
                .GroupBy(a => a.ArticleNumber.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);
            var kreditoren = (await _ctx.Creditors.AsNoTracking().Where(c => c.SageKto != null).Select(c => new { c.Id, c.SageKto }).ToListAsync(ct))
                .GroupBy(c => c.SageKto!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);
            // Eine Quelle, die nie leer ist (AKE-Test 23.509 Varianten mit Hauptlieferant), liefert 0 Zeilen = Filter/Feld kaputt:
            // melden und die Hauptlieferant-Kennzeichen unveraendert lassen statt alle auf false zu setzen.
            var hauptGelesen = haupt.Count > 0;
            if (!hauptGelesen && zeilen.Count > 0)
                await run.LogWarningAsync($"{RessourceVarianten} liefert 0 Zeilen mit Hauptlieferant ({WhereVarianten}) — Filter pruefen; " +
                    "Kennzeichen Hauptlieferant bleiben unveraendert.", ct: ct);
            var hauptlieferant = haupt.Where(h => !string.IsNullOrWhiteSpace(h.Artikelnummer) && !string.IsNullOrWhiteSpace(h.Hauptlieferant))
                .Select(h => (h.Artikelnummer!.Trim().ToUpperInvariant(), h.Hauptlieferant!.Trim().ToUpperInvariant()))
                .ToHashSet();

            int ohneSchluessel = 0, doppelt = 0, artikelFehlt = 0, neu = 0, aktualisiert = 0, unveraendert = 0, istHaupt = 0;
            var kreditorFehlt = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            int kreditorFehltZeilen = 0;
            var bestand = await _ctx.ArticleSuppliers.ToListAsync(ct);
            var byPaar = bestand.GroupBy(s => (s.ArticleId, s.CreditorId)).ToDictionary(g => g.Key, g => g.First());
            var gesehen = new HashSet<(int, int)>();
            var now = DateTime.Now;

            foreach (var z in zeilen)
            {
                var nummer = z.Artikelnummer?.Trim();
                var kto = z.Lieferant?.Trim();
                if (string.IsNullOrEmpty(nummer) || string.IsNullOrEmpty(kto)) { ohneSchluessel++; continue; }
                if (!artikel.TryGetValue(nummer, out var articleId)) { artikelFehlt++; continue; }
                if (!kreditoren.TryGetValue(kto, out var creditorId)) { kreditorFehlt.Add(kto); kreditorFehltZeilen++; continue; }
                if (!gesehen.Add((articleId, creditorId))) { doppelt++; continue; } // UNIQUE (ArticleId, CreditorId): erste Zeile gewinnt

                var bisher = byPaar.TryGetValue((articleId, creditorId), out var vorhanden) && vorhanden.IstHauptlieferant;
                var h = hauptGelesen ? hauptlieferant.Contains((nummer.ToUpperInvariant(), kto.ToUpperInvariant())) : bisher;
                if (h) istHaupt++;
                if (!byPaar.TryGetValue((articleId, creditorId), out var s))
                {
                    neu++;
                    if (dryRun) continue;
                    s = new ArticleSupplier
                    {
                        ArticleId = articleId, CreditorId = creditorId, Artikelnummer = nummer,
                        CreatedAt = now, CreatedBy = ServiceUser, CreatedByWindows = ServiceUser,
                    };
                    Uebernehmen(s, z, h);
                    _ctx.ArticleSuppliers.Add(s);
                    continue;
                }
                if (!Uebernehmen(s, z, h)) { unveraendert++; continue; }
                aktualisiert++;
                s.ModifiedAt = now; s.ModifiedBy = ServiceUser; s.ModifiedByWindows = ServiceUser;
            }

            if (ohneSchluessel > 0)
                await run.LogWarningAsync($"{ohneSchluessel} Zeile(n) ohne Artikelnummer oder Lieferant uebersprungen.", ct: ct);
            if (doppelt > 0)
                await run.LogWarningAsync($"{doppelt} Zeile(n) mit bereits gelesenem Artikel + Lieferant uebersprungen (erste Zeile gilt).", ct: ct);
            if (kreditorFehlt.Count > 0)
                await run.LogWarningAsync(SdataSchluessel.Kappen($"{kreditorFehltZeilen} Zeile(n) uebersprungen, {kreditorFehlt.Count} Lieferant(en) " +
                    $"ohne Kreditor im WMS (Kreditoren-Abgleich Sync:KreditorenEnabled aktiv?): {string.Join(", ", kreditorFehlt.Take(20))}"), ct: ct);
            if (artikelFehlt > 0)
                await run.LogInfoAsync($"{artikelFehlt} Zeile(n) zu Artikeln, die es im WMS nicht gibt — nicht uebernommen.", ct: ct);

            var entfernt = new List<ArticleSupplier>();
            if (gesehen.Count == 0)
                await run.LogWarningAsync("Keine Artikel-Lieferant-Zeile zuordenbar — nichts entfernt (Empty-Guard).", ct: ct);
            else
            {
                entfernt.AddRange(bestand.Where(s => !gesehen.Contains((s.ArticleId, s.CreditorId))));
                if (entfernt.Count > 0)
                {
                    await run.LogInfoAsync(SdataSchluessel.Kappen($"{entfernt.Count} Zuordnung(en) nicht mehr in Sage, entfernt: " +
                        string.Join(", ", entfernt.Take(20).Select(s => $"{s.Artikelnummer}/{s.CreditorId}"))), ct: ct);
                    if (!dryRun) _ctx.ArticleSuppliers.RemoveRange(entfernt);
                }
            }

            if (!dryRun) await _ctx.SaveChangesAsync(ct);

            var uebersprungen = ohneSchluessel + doppelt + kreditorFehltZeilen;
            await run.FinishSuccessAsync(new Dictionary<string, int>
            {
                ["gelesen"] = zeilen.Count,
                ["neu"] = neu,
                ["aktualisiert"] = aktualisiert,
                ["unveraendert"] = unveraendert,
                ["entfernt"] = entfernt.Count,
                ["hauptlieferant"] = istHaupt,
                ["artikel_fehlt"] = artikelFehlt,
                ["kreditor_fehlt"] = kreditorFehltZeilen,
                ["uebersprungen"] = uebersprungen,
            }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
            return new SyncResult(neu, aktualisiert, uebersprungen);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Artikel-Lieferant-Abgleich fehlgeschlagen.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }

    /// <summary>Sage-Werte uebernehmen (F1: null = nicht schreiben); true = etwas geaendert.
    /// TODO(P-S1): Nachkommastellen unbelegt — gerundet auf die Spalten aus Spec F10, damit nicht jeder Lauf eine Aenderung sieht.</summary>
    private static bool Uebernehmen(ArticleSupplier s, SdataArtikelLieferantZeile z, bool istHaupt)
    {
        var anders = false;
        void Set<TV>(TV? neu, TV? alt, Action<TV> schreiben)
        {
            if (neu == null || EqualityComparer<TV?>.Default.Equals(neu, alt)) return;
            schreiben(neu);
            anders = true;
        }
        Set(Text(z.Bestellnummer), s.SageBestellnummer, v => s.SageBestellnummer = v);
        Set(Text(z.Bezeichnung1), s.Bezeichnung1, v => s.Bezeichnung1 = v);
        Set(Text(z.Bezeichnung2), s.Bezeichnung2, v => s.Bezeichnung2 = v);
        Set(Text(z.Einkaufsmengeneinheit), s.Einkaufsmengeneinheit, v => s.Einkaufsmengeneinheit = v);
        Set(z.Mindestbestellmenge is { } m ? Math.Round(m, 3) : (decimal?)null, s.Mindestbestellmenge, v => s.Mindestbestellmenge = v);
        Set(z.UmrechnungsfaktorEK is { } u ? Math.Round(u, 6) : (decimal?)null, s.UmrechnungsfaktorEK, v => s.UmrechnungsfaktorEK = v);
        Set(z.Einzelpreis is { } p ? Math.Round(p, 4) : (decimal?)null, s.Einzelpreis, v => s.Einzelpreis = v);
        Set(z.Wiederbeschaffungszeit, s.Wiederbeschaffungszeit, v => s.Wiederbeschaffungszeit = v);
        Set(z.DezimalstellenEK, s.DezimalstellenEK, v => s.DezimalstellenEK = v);
        Set(z.UmrechnungsfaktorVPEK is { } vp ? Math.Round(vp, 6) : (decimal?)null, s.UmrechnungsfaktorVPEK, v => s.UmrechnungsfaktorVPEK = v);
        if (s.IstHauptlieferant != istHaupt) { s.IstHauptlieferant = istHaupt; anders = true; }
        return anders;
    }

    private static string? Text(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
