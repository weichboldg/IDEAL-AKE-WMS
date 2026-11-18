using System.Globalization;
using System.Text.Json;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Services.SyncLogger;
using IDEALAKEWMSService.Common;
using Microsoft.EntityFrameworkCore;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>Zeile aus <c>EinkaufPositionenInfo</c> (Namen exakt wie in der SData-Antwort, Typen laut E0/P-W1 am AKE-Test).</summary>
public sealed record SdataEinkaufPositionZeile(int? BelId, int? BelPosId, string? Belegkennzeichen, string? BelegnummerFormatiert,
    string? A0Empfaenger, string? A0Name1, int? Liefersperre, string? Liefertermin, string? BelegLiefertermin, string? Artikelnummer,
    string? Bezeichnung1, string? Bezeichnung2, int? Chargenpflicht, int? Nachweispflicht, string? Mengeneinheit, string? MengeBasiseinheit,
    string? Lagermengeneinheit, decimal? PositionGGBestellt, decimal? PositionGGGeliefert, int? PlatzID, string? Kurzbezeichnung,
    decimal? MengeBasis = null);

/// <summary>
/// v1.84.0, Spec 2026-11-08-wareneingang-aus-sage-bestellungen E1 (+ Entscheidung E0-Blocker): liest offene Sage-Einkaufsbestellungen aus
/// SData <c>EinkaufPositionenInfo</c> (die Sage-View liefert nur offene Positionen) in den Cache <see cref="SagePurchaseOrderLine"/>.
/// <list type="bullet">
/// <item><c>EBB</c> (normale Bestellung) und seit v1.99.15 <c>EBZ</c> (Rahmenabruf), je eine Abfrage <c>where Belegkennzeichen eq '…'</c>
/// (kein <c>or</c>, kein <c>ne</c>). EBZ-Mengen aus <c>MengeBasis</c> des Abrufs, geliefert 0: die <c>PositionGG*</c>-Felder gelten bei EBZ
/// fuer den ganzen Rahmen (Teil 1 B-1; P-W8 2026-11-13: 108 Abrufe, Menge = MengeBasis bei gleicher Einheit).</item>
/// <item>Kopfdaten aus den Positionen (<c>EinkaufBelegekopfInfo</c> liefert mehrere Zeilen je Beleg, B-3); <c>Liefersperre</c> = Lieferant gesperrt.</item>
/// <item>Mengen in der Basiseinheit; <c>Mengeneinheit ≠ Lagermengeneinheit</c> → <see cref="SagePurchaseOrderLine.EinheitAbweichend"/> (B-2).</item>
/// <item>Zuordnung Artikel / Kreditor / Lagerplatz; nicht zuordenbar = Warnung mit Beispielen, Zeile bleibt mit leerem Verweis.</item>
/// <item>Vollersatz je Lauf in einer Transaktion; Empty-Guard je Belegart: liefert eine Abfrage 0 Zeilen, waehrend der Cache Zeilen dieser
/// Belegart hat = Warnung, ganzer Cache unveraendert (ein still leerer Filter loescht sonst die Haelfte).</item>
/// </list>
/// Kein View-Weg, keine Quellen-Weiche (SData-only wie <see cref="ArtikelLieferantSyncService"/>). Nie <c>EinkaufPositionenAllInfo</c>.
/// Gate <c>Sync:EinkaufBestellungenEnabled</c>. Lesefehler: Lauf Fehler + throw (Mail ueber RunResilientAsync). Schreibt nie nach Sage.
/// <para>v1.99.33 (Spec 2026-11-17-wareneingang-sammellieferschein-und-bestellkopf Teil B, Variante B): AB-Nummer/Kommission aus dem
/// Sage-Kopf ueber RealTimeData <see cref="KopfQuelle"/> (nur GET, <c>where</c> nur auf <c>BelID</c> – ein where auf die USER-Aliasse bricht
/// mit SQL-Fehler ab). Ein Fehler dort blockiert den Positions-Abgleich nicht: Werte vom letzten Lauf + Warnung.</para>
/// </summary>
public class EinkaufBestellungenSyncService
{
    public const string Ressource = "EinkaufPositionenInfo";
    public const string Select = "BelId,BelPosId,Belegkennzeichen,BelegnummerFormatiert,A0Empfaenger,A0Name1,Liefersperre,Liefertermin,"
        + "BelegLiefertermin,Artikelnummer,Bezeichnung1,Bezeichnung2,Chargenpflicht,Nachweispflicht,Mengeneinheit,MengeBasiseinheit,"
        + "Lagermengeneinheit,PositionGGBestellt,PositionGGGeliefert,PlatzID,Kurzbezeichnung,MengeBasis";
    public const string OrderBy = "BelPosId";
    public const string Bestellung = "EBB";
    /// <summary>Gelesene Belegarten, je eine Abfrage (Reihenfolge = Abfrage-Reihenfolge).</summary>
    public static readonly IReadOnlyList<string> Belegarten = [Bestellung, SagePurchaseOrderLine.KennzeichenRahmenabruf];
    public static string Where(string belegart) => $"Belegkennzeichen eq '{belegart}'";
    public const int Seitengroesse = 1000;

    /// <summary>v1.99.33 Teil B: RealTimeData-Quelle der EK-Belegkoepfe; Benutzerfelder mit Tabellen-Praefix (fallstricke §41, P-1 2026-11-17).</summary>
    public const string KopfQuelle = "dtsEKBelegMst.Sage.Wawi";
    public const string FeldAbNummer = "KHKEKBelege_USER_AB";
    public const string FeldKommission = "KHKEKBelege_USER_Kom";
    public const string KopfSelect = "BelID," + FeldAbNummer + "," + FeldKommission; // AKE-Default
    /// <summary>v1.99.40 (ADR 0033): Praefix bleibt Code, der Feldname kommt aus Sync:Userfeld:Bestellungen:USER_AB / USER_Kom.</summary>
    public const string PraefixKopf = "KHKEKBelege_";

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

    // v1.99.31 (Spec 2026-11-16-offene-bestellungen-sofort-neu-lesen F1.7): regulaerer Lauf (SyncWorker) und Anstoss nach Sage-Wareneingang
    // (SageBookingWorker) nie gleichzeitig - der Vollersatz loescht und fuegt in einer Transaktion ein, ein zweiter Lauf scheitert am UNIQUE BelPosId.
    // Statisch, weil der Service Scoped ist; beide Worker laufen im selben Prozess.
    // ponytail: prozessweite Sperre; reicht, solange nur EIN Dienst je DB laeuft (Ist-Zustand). Mehrere Dienste je DB -> sp_getapplock.
    private static readonly SemaphoreSlim Sperre = new(1, 1);

    /// <summary>Regulaerer Lauf: wartet, bis ein laufender (angestossener) Lauf fertig ist.</summary>
    public async Task<SyncResult> RunAsync(bool dryRun, CancellationToken ct = default, SdataLeseZiel? ziel = null, string? anlass = null)
    {
        await Sperre.WaitAsync(ct);
        try { return await RunCoreAsync(dryRun, ct, ziel, anlass); }
        finally { Sperre.Release(); }
    }

    /// <summary>v1.99.31 F1: Anstoss nach Sage-Wareneingang. Laeuft gerade ein Lauf, kommt sofort <c>false</c> zurueck (nichts gelesen,
    /// der Aufrufer holt im naechsten Takt nach). Fehler werfen wie bei <see cref="RunAsync"/>.</summary>
    public async Task<bool> TryRunExclusiveAsync(bool dryRun, string anlass, CancellationToken ct = default, SdataLeseZiel? ziel = null)
    {
        if (!await Sperre.WaitAsync(0, ct)) return false;
        try { await RunCoreAsync(dryRun, ct, ziel, anlass); return true; }
        finally { Sperre.Release(); }
    }

    private async Task<SyncResult> RunCoreAsync(bool dryRun, CancellationToken ct, SdataLeseZiel? ziel, string? anlass)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.EinkaufBestellungen, ct);
        if (anlass is not null) await run.LogInfoAsync($"Anlass: {anlass}", ct: ct);   // F1.6: erkennbar gegen den regulaeren Lauf

        var zeilen = new List<SdataEinkaufPositionZeile>();
        var jeArt = new Dictionary<string, int>();
        // v1.99.4 (Wareneingang Teil 2, F5): Stand VOR dem Lesen - Rest-Regel vergleicht SageWeAm <= SyncedAt
        var now = DateTime.Now;
        try
        {
            ziel ??= await SdataLeseZielLeser.LesenAsync(_config, ct);
            if (ziel.Warnung is { } zugangsWarnung) await run.LogWarningAsync(zugangsWarnung, ct: ct); // v1.99.14 F6: Rueckfall appsettings
            foreach (var art in Belegarten)
            {
                var seiten = await _client.QueryAllAsync<SdataEinkaufPositionZeile>(ziel, SageSdataReadClient.VertragResources, Ressource,
                    Where(art), Select, OrderBy, ct, Seitengroesse);
                zeilen.AddRange(seiten.Zeilen);
                jeArt[art] = seiten.Zeilen.Count;
                await run.LogInfoAsync($"{art}: {seiten.Zusammenfassung}", ct: ct);
            }
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
            foreach (var art in Belegarten)
            {
                var imCache = bestand.Count(b => string.Equals(b.Belegkennzeichen, art, StringComparison.OrdinalIgnoreCase));
                if (jeArt[art] > 0 || imCache == 0) continue;
                await run.LogWarningAsync($"{Ressource} liefert 0 offene Positionen ({Where(art)}), Cache hat {imCache} – Cache bleibt unveraendert (Empty-Guard).", ct: ct);
                await run.FinishSuccessAsync(new Dictionary<string, int> { ["positionen"] = zeilen.Count, ["uebernommen"] = 0 }, ct: ct);
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

            var koepfe = await KoepfeAsync(ziel!, zeilen.Where(z => z.BelId != null).Select(z => z.BelId!.Value).Distinct().ToList(), bestand, run, ct);

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
                // Doppelte Absicherung zum where: nur EBB und EBZ.
                var belegart = z.Belegkennzeichen?.Trim().ToUpperInvariant();
                if (belegart == null || !Belegarten.Contains(belegart)) { belegartAus++; continue; }
                var abruf = belegart == SagePurchaseOrderLine.KennzeichenRahmenabruf;
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
                // EBZ (F1): Abrufmenge = MengeBasis, geliefert 0 - PositionGG* gelten fuer den ganzen Rahmen und wirken hier nie.
                if (abruf ? z.MengeBasis == null : z.PositionGGBestellt == null || z.PositionGGGeliefert == null) mengeFehlt++; // QA Q-6: melden statt still 0
                var bestellt = (abruf ? z.MengeBasis : z.PositionGGBestellt) ?? 0m;
                var geliefert = abruf ? 0m : z.PositionGGGeliefert ?? 0m;
                // E0 B-2 + Nachbesserung v1.86.0: gebucht wird die Menge in der BASISeinheit auf einen Artikel mit Lagereinheit -> gesperrt, wenn
                // Bestell- ODER Basiseinheit von der Lagereinheit abweicht (EK003604 am AKE-Test: Bestellung PKG, Basis Stk, Lager PKG).
                var abweichend = !string.Equals(Text(z.Mengeneinheit), Text(z.Lagermengeneinheit), StringComparison.OrdinalIgnoreCase)
                              || !string.Equals(Text(z.MengeBasiseinheit), Text(z.Lagermengeneinheit), StringComparison.OrdinalIgnoreCase);
                if (abweichend) einheitAbweichend++;
                var sperre = (z.Liefersperre ?? 0) != 0;
                if (sperre) gesperrt++;

                neu.Add(new SagePurchaseOrderLine
                {
                    BelPosId = belPosId, BelId = belId, Belegkennzeichen = belegart, Belegnummer = z.BelegnummerFormatiert!.Trim(),
                    LieferantKto = kto, LieferantName = Text(z.A0Name1), LieferantGesperrt = sperre, CreditorId = creditorId,
                    Artikelnummer = nummer, ArticleId = articleId, Bezeichnung1 = Text(z.Bezeichnung1), Bezeichnung2 = Text(z.Bezeichnung2),
                    MengeBestellt = bestellt, MengeGeliefert = geliefert, MengeOffen = bestellt - geliefert,
                    Mengeneinheit = Text(z.Mengeneinheit), MengeBasiseinheit = Text(z.MengeBasiseinheit), Lagermengeneinheit = Text(z.Lagermengeneinheit),
                    EinheitAbweichend = abweichend, SagePlatzId = platzId, SagePlatzKurzbezeichnung = Text(z.Kurzbezeichnung),
                    StorageLocationId = storageLocationId, Liefertermin = termin, Chargenpflicht = (z.Chargenpflicht ?? 0) != 0,
                    Nachweispflicht = (z.Nachweispflicht ?? 0) != 0, SyncedAt = now,
                    AbNummer = koepfe.Werte.GetValueOrDefault(belId).Ab, Kommission = koepfe.Werte.GetValueOrDefault(belId).Kom,
                });
            }

            if (ohneSchluessel > 0) await run.LogWarningAsync($"{ohneSchluessel} Position(en) ohne BelId/BelPosId/Artikelnummer/Belegnummer uebersprungen.", ct: ct);
            if (doppelt > 0) await run.LogWarningAsync($"{doppelt} Position(en) mit bereits gelesener BelPosId uebersprungen (erste Zeile gilt).", ct: ct);
            if (belegartAus > 0) await run.LogInfoAsync($"{belegartAus} Position(en) anderer Belegart als {string.Join("/", Belegarten)} nicht uebernommen.", ct: ct);
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
                await run.LogWarningAsync($"{mengeFehlt} Position(en) ohne Menge (EBB PositionGGBestellt/PositionGGGeliefert, EBZ MengeBasis) – fehlender Wert als 0 gefuehrt, bitte in Sage pruefen.", ct: ct);
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
                ["positionen_ebb"] = jeArt[Bestellung],
                ["positionen_ebz"] = jeArt[SagePurchaseOrderLine.KennzeichenRahmenabruf],
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
                ["ab_nummer"] = neu.Where(n => n.AbNummer != null).Select(n => n.BelId).Distinct().Count(),
                ["kommission"] = neu.Where(n => n.Kommission != null).Select(n => n.BelId).Distinct().Count(),
                ["kopf_ohne_treffer"] = koepfe.OhneTreffer,
                ["kopf_zu_lang"] = koepfe.ZuLang,
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

    /// <summary>v1.99.33 Teil B (B4): Kopfwerte je BelID aus <see cref="KopfQuelle"/> in Bloecken zu 50. Melden statt still: Feld fehlt in jeder
    /// Zeile (Name/Praefix falsch – RealTimeData liefert unbekannte select-Namen still nicht), BelID ohne Kopf, Wert zu lang (nicht schreiben,
    /// nie kuerzen). Lesefehler: Werte je BelID aus dem bisherigen Cache + Warnung, der Positions-Abgleich laeuft weiter.</summary>
    private async Task<(Dictionary<int, (string? Ab, string? Kom)> Werte, int OhneTreffer, int ZuLang)> KoepfeAsync(SdataLeseZiel ziel,
        IReadOnlyList<int> belIds, IReadOnlyList<SagePurchaseOrderLine> bestand, ISyncRun run, CancellationToken ct)
    {
        var zeilen = new List<Dictionary<string, JsonElement>>();
        var felder = await ServiceSettings.UserfelderSafeAsync(_config, ct);
        foreach (var h in felder.Hinweise) await run.LogWarningAsync(h, ct: ct);
        string? Kopffeld(string standard) => felder.Feld(SageUserfelder.Bestellungen, standard) is { } n ? PraefixKopf + n : null;
        var feldAb = Kopffeld("USER_AB");
        var feldKom = Kopffeld("USER_Kom");
        foreach (var (f, name) in new[] { (feldAb, "AB-Nummer"), (feldKom, "Kommission") })
            if (f is null) await run.LogInfoAsync($"Sage-Benutzerfeld „{name}“ nicht konfiguriert (Service-Einstellungen) – nicht gelesen, bleibt leer.", ct: ct);
        var kopfSelect = string.Join(",", new[] { "BelID", feldAb, feldKom }.Where(s => s is not null));
        try
        {
            foreach (var block in SageSdataQuellen.OrBloecke("BelID", belIds))
            {
                var r = await _client.GetAsync<Dictionary<string, JsonElement>>(ziel, KopfQuelle, block, kopfSelect, "BelID", ct);
                if (!r.Call.Erfolgreich) throw new SdataLeseException(KopfQuelle, r.Call.Status, r.Call.Fehler ?? "unbekannter Fehler");
                zeilen.AddRange(r.Zeilen.Select(z => new Dictionary<string, JsonElement>(z, StringComparer.OrdinalIgnoreCase)));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Kopfdaten (AB-Nummer/Kommission) aus {Quelle} nicht lesbar.", KopfQuelle);
            await run.LogWarningAsync(SdataSchluessel.Kappen($"Kopfdaten (AB-Nummer/Kommission) nicht lesbar: {felder.Erklaeren(ex)} – Werte vom letzten Lauf."), ct: ct);
            var alt = bestand.GroupBy(b => b.BelId).ToDictionary(g => g.Key, g => (g.First().AbNummer, g.First().Kommission));
            return (alt, 0, 0);
        }
        if (belIds.Count > 0) await run.LogInfoAsync($"Kopfdaten: Quelle SData (RealTimeData) {KopfQuelle}, {zeilen.Count} Zeile(n) zu {belIds.Count} Beleg(en).", ct: ct);

        foreach (var (feld, name) in new[] { (feldAb, "AB-Nummer"), (feldKom, "Kommission") })
            if (feld is not null && zeilen.Count > 0 && !zeilen.Any(z => z.ContainsKey(feld)))
                await run.LogWarningAsync($"Sage liefert {feld} nicht (Feldname/Präfix prüfen) – {name} bleibt leer.", ct: ct);

        var werte = new Dictionary<int, (string? Ab, string? Kom)>();
        var zuLang = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        string? Wert(Dictionary<string, JsonElement> z, string? feld, int max, int belId)
        {
            var t = feld is not null && z.TryGetValue(feld, out var w) ? SdataArtikelReader.AlsText(w) : null;
            if (t is not { Length: > 0 } || t.Length <= max) return t;
            zuLang.Add($"BelID {belId} {feld} ({t.Length} > {max})");
            return null;
        }
        foreach (var z in zeilen)
            if (z.TryGetValue("BelID", out var id) && int.TryParse(SdataArtikelReader.AlsText(id), NumberStyles.Integer, CultureInfo.InvariantCulture, out var belId)
                && !werte.ContainsKey(belId))
                werte[belId] = (Wert(z, feldAb, SagePurchaseOrderLine.AbNummerMaxLaenge, belId),
                    Wert(z, feldKom, SagePurchaseOrderLine.KommissionMaxLaenge, belId));

        var ohneTreffer = belIds.Count(b => !werte.ContainsKey(b));
        if (ohneTreffer > 0)
            await run.LogWarningAsync(SdataSchluessel.Kappen($"{ohneTreffer} Beleg(e) ohne Kopf in {KopfQuelle} – AB-Nummer/Kommission leer: " +
                string.Join(", ", belIds.Where(b => !werte.ContainsKey(b)).Take(20))), ct: ct);
        if (zuLang.Count > 0)
            await run.LogWarningAsync(SdataSchluessel.Kappen($"{zuLang.Count} Kopfwert(e) länger als die WMS-Spalte – nicht übernommen (nie gekürzt): " +
                string.Join(", ", zuLang.Take(20))), ct: ct);
        return (werte, ohneTreffer, zuLang.Count);
    }

    private static string? Text(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    internal static DateTime? Datum(string? s) =>
        !string.IsNullOrWhiteSpace(s) && DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var t)
            ? t.Date : null;
}
