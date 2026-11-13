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

/// <summary>v1.99.21 E3: Zeile aus <c>EinkaufPositionenAllInfo</c> (Namen laut Vertrags-$schema, E0 P-L1 a; Belegdatum dateTime als Text).</summary>
public sealed record SdataLetzteBestellungZeile(int? BelId, int? BelPosId, string? Belegkennzeichen, string? Belegdatum,
    string? BelegnummerFormatiert, string? A0Empfaenger, string? Artikelnummer, decimal? MengeBasis);

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

    // v1.99.21 E3: Feldnamen aus dem Vertrags-$schema (E0 P-L1 a, 2026-11-13); nur mit where (SageSdataReadClient.NurMitWhere).
    public const string RessourceLetzteBestellung = "EinkaufPositionenAllInfo";
    public const string SelectLetzteBestellung = "BelId,BelPosId,Belegkennzeichen,Belegdatum,BelegnummerFormatiert,A0Empfaenger,Artikelnummer,MengeBasis";
    public const string OrderByLetzteBestellung = "BelPosId";
    /// <summary>Datum nur als <c>@yyyy-MM-dd@</c> (fallstricke §31: in Anfuehrungszeichen still 0 Treffer).</summary>
    public static string WhereLetzteBestellung(string belegart, DateTime ab) =>
        $"Belegkennzeichen eq '{belegart}' and Belegdatum ge @{ab.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)}@";

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

            // v1.99.21 (E3): zweiter Leseschritt im selben Lauf; Lesefehler = Warnung, der Lauf bleibt erfolgreich.
            var letzte = await LetzteBestellungAsync(run, ziel!, dryRun, artikel, kreditoren, now, ct);

            var uebersprungen = ohneSchluessel + doppelt + kreditorFehltZeilen;
            await run.FinishSuccessAsync(new Dictionary<string, int>(letzte)
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

    /// <summary>
    /// v1.99.21 (Spec 2026-11-08-einkauf-bestellvorschlaege E3, Freigabe-Nachtrag 2): juengste Sage-Bestellposition je Artikel + Lieferant
    /// (<c>A0Empfaenger</c> = Kto) aus <see cref="RessourceLetzteBestellung"/>, Belegarten EBB und EBZ (Antwort 12: der juengste Beleg gilt),
    /// Rueckblick <see cref="ArticleSupplier.LetzteBestellungRueckblickJahre"/> Jahre. SData sortiert nach <c>BelPosId</c>, nicht nach Datum
    /// (E0) – deshalb alle Seiten lesen und clientseitig nach Belegdatum↓, BelId↓, BelPosId↓ waehlen. Abgelegt nur an vorhandenen
    /// <see cref="ArticleSupplier"/>-Zeilen; ohne Treffer im Rueckblick werden gespeicherte Werte geleert. Lesefehler oder EBB leer:
    /// Warnung, nichts geaendert. Menge = <c>MengeBasis</c> (Basiseinheit, P-L2 c); die Einheiten-Sperre Basis ≠ Lager prueft die Liste.
    /// </summary>
    private async Task<Dictionary<string, int>> LetzteBestellungAsync(ISyncRun run, SdataLeseZiel ziel, bool dryRun,
        IReadOnlyDictionary<string, int> artikel, IReadOnlyDictionary<string, int> kreditoren, DateTime now, CancellationToken ct)
    {
        var ab = now.Date.AddYears(-ArticleSupplier.LetzteBestellungRueckblickJahre);
        var zeilen = new List<SdataLetzteBestellungZeile>();
        var jeArt = new Dictionary<string, int>();
        try
        {
            foreach (var art in EinkaufBestellungenSyncService.Belegarten)
            {
                var seiten = await _client.QueryAllAsync<SdataLetzteBestellungZeile>(ziel, SageSdataReadClient.VertragResources,
                    RessourceLetzteBestellung, WhereLetzteBestellung(art, ab), SelectLetzteBestellung, OrderByLetzteBestellung, ct,
                    EinkaufBestellungenSyncService.Seitengroesse);
                zeilen.AddRange(seiten.Zeilen);
                jeArt[art] = seiten.Zeilen.Count;
                await run.LogInfoAsync($"Letzte Bestellung {art}: {seiten.Zusammenfassung}", ct: ct);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Letzte Bestellung aus Sage (SData) nicht lesbar.");
            await run.LogWarningAsync(SdataSchluessel.Kappen($"Letzte Bestellung: {RessourceLetzteBestellung} nicht lesbar – gespeicherte Werte " +
                $"bleiben unveraendert: {ex.Message}"), ct: ct);
            return new() { ["letzte_bestellung_lesefehler"] = 1 };
        }

        var ebb = jeArt[EinkaufBestellungenSyncService.Bestellung];
        if (ebb == 0)
        {
            // Bestellungen der letzten Jahre sind nie leer (AKE-Test: 36.288 Positionen in 3 Jahren) – 0 = Filter/Feld kaputt.
            await run.LogWarningAsync($"Letzte Bestellung: {RessourceLetzteBestellung} liefert 0 Bestellpositionen " +
                $"({WhereLetzteBestellung(EinkaufBestellungenSyncService.Bestellung, ab)}) – gespeicherte Werte bleiben unveraendert.", ct: ct);
            return new() { ["letzte_bestellung_positionen"] = zeilen.Count };
        }

        int ohneDaten = 0, artikelFehlt = 0;
        var juengste = new Dictionary<(int ArticleId, string Kto), (DateTime Am, int BelId, int BelPosId, decimal Menge, string? Beleg, string Nummer)>();
        foreach (var z in zeilen)
        {
            var nummer = Text(z.Artikelnummer);
            var kto = Text(z.A0Empfaenger)?.ToUpperInvariant();
            var am = EinkaufBestellungenSyncService.Datum(z.Belegdatum);
            if (nummer == null || kto == null || am == null || z.MengeBasis is not { } menge || z.BelId is not { } belId) { ohneDaten++; continue; }
            if (!artikel.TryGetValue(nummer, out var articleId)) { artikelFehlt++; continue; }
            var beleg = Text(z.BelegnummerFormatiert);
            var neu = (Am: am.Value, BelId: belId, BelPosId: z.BelPosId ?? 0, Menge: Math.Round(menge, 3),
                Beleg: beleg is { Length: > 45 } ? null : beleg, Nummer: nummer); // Spalte nvarchar(45) = Sage string(45)
            if (!juengste.TryGetValue((articleId, kto), out var bisher)
                || (neu.Am, neu.BelId, neu.BelPosId).CompareTo((bisher.Am, bisher.BelId, bisher.BelPosId)) > 0)
                juengste[(articleId, kto)] = neu;
        }

        var lieferanten = _ctx.ArticleSuppliers.Local.GroupBy(s => (s.ArticleId, s.CreditorId)).ToDictionary(g => g.Key, g => g.First());
        var getroffen = new HashSet<ArticleSupplier>();
        var ohneZuordnung = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        int ohneZuordnungZahl = 0, aktualisiert = 0, geleert = 0;
        void Audit(ArticleSupplier s) { s.ModifiedAt = now; s.ModifiedBy = ServiceUser; s.ModifiedByWindows = ServiceUser; }
        foreach (var ((articleId, kto), w) in juengste)
        {
            if (!kreditoren.TryGetValue(kto, out var creditorId) || !lieferanten.TryGetValue((articleId, creditorId), out var s))
            {
                ohneZuordnungZahl++;
                ohneZuordnung.Add($"{w.Nummer}/{kto}");
                continue;
            }
            getroffen.Add(s);
            if (Setzen(s, w.Menge, w.Am, w.Beleg)) { aktualisiert++; Audit(s); }
        }
        foreach (var s in lieferanten.Values.Where(s => !getroffen.Contains(s)))
            if (Setzen(s, null, null, null)) { geleert++; Audit(s); }

        if (ohneDaten > 0)
            await run.LogWarningAsync($"Letzte Bestellung: {ohneDaten} Position(en) ohne Artikelnummer, Lieferant, Belegdatum, BelId oder MengeBasis uebersprungen.", ct: ct);
        if (artikelFehlt > 0)
            await run.LogInfoAsync($"Letzte Bestellung: {artikelFehlt} Position(en) zu Artikeln, die es im WMS nicht gibt — nicht uebernommen.", ct: ct);
        if (ohneZuordnungZahl > 0)
            await run.LogWarningAsync(SdataSchluessel.Kappen($"Letzte Bestellung: {ohneZuordnungZahl} Artikel/Lieferant ohne Artikel-Lieferant-Zeile " +
                $"im WMS – nicht abgelegt: {string.Join(", ", ohneZuordnung.Take(20))}"), ct: ct);
        if (!dryRun) await _ctx.SaveChangesAsync(ct);

        return new()
        {
            ["letzte_bestellung_positionen"] = zeilen.Count,
            ["letzte_bestellung_positionen_ebz"] = jeArt[SagePurchaseOrderLine.KennzeichenRahmenabruf],
            ["letzte_bestellung_gefunden"] = juengste.Count,
            ["letzte_bestellung_aktualisiert"] = aktualisiert,
            ["letzte_bestellung_geleert"] = geleert,
            ["letzte_bestellung_ohne_zuordnung"] = ohneZuordnungZahl,
            ["letzte_bestellung_uebersprungen"] = ohneDaten,
        };
    }

    private static bool Setzen(ArticleSupplier s, decimal? menge, DateTime? am, string? beleg)
    {
        if (s.LetzteBestellmenge == menge && s.LetzteBestellungAm == am && s.LetzteBestellungBeleg == beleg) return false;
        s.LetzteBestellmenge = menge;
        s.LetzteBestellungAm = am;
        s.LetzteBestellungBeleg = beleg;
        return true;
    }
}
