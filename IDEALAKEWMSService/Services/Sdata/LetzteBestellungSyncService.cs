using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Services.SyncLogger;
using IDEALAKEWMSService.Common;
using Microsoft.EntityFrameworkCore;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>
/// v1.99.30 (Spec 2026-11-16-bestellvorschlaege-nachbesserung F1, Antwort 5): wann der Lauf „Letzte Bestellung“ faellig ist – reiner Helper
/// ohne DB (Muster <see cref="LieferverzugPlaner.ImZeitfenster"/>).
/// </summary>
public static class LetzteBestellungPlaner
{
    /// <summary>Faellig, wenn der juengste konfigurierte Termin (heute oder gestern, vor <paramref name="jetzt"/>) nach dem letzten Start
    /// liegt. Kein bisheriger Lauf = faellig (verpasster Termin wird nach dem Dienststart genau einmal nachgeholt).</summary>
    public static bool Faellig(DateTime jetzt, DateTime? letzterStart, IEnumerable<int> uhrzeiten)
    {
        var termin = uhrzeiten
            .SelectMany(h => new[] { jetzt.Date.AddHours(h), jetzt.Date.AddDays(-1).AddHours(h) })
            .Where(t => t <= jetzt)
            .DefaultIfEmpty(DateTime.MinValue)
            .Max();
        return termin != DateTime.MinValue && (letzterStart == null || letzterStart < termin);
    }
}

/// <summary>
/// v1.99.30 (Spec 2026-11-16-bestellvorschlaege-nachbesserung F1–F3, E1): juengste Sage-Bestellposition je Artikel + Lieferant
/// (<c>A0Empfaenger</c> = Kto) aus <see cref="RessourceLetzteBestellung"/>, Belegarten EBB und EBZ, Rueckblick
/// <see cref="ArticleSupplier.LetzteBestellungRueckblickJahre"/> Jahre. Vorher Schritt im Artikel-Lieferanten-Abgleich (v1.99.21) und dort
/// rund 5 Minuten je Dienst-Zyklus (QA Q-3) – jetzt eigener Lauf nur zu den Uhrzeiten <c>Sync:LetzteBestellungUhrzeitNacht/-Mittag</c>,
/// Gate <c>Sync:ArtikelLieferantEnabled</c>. SData sortiert nach <c>BelPosId</c>, nicht nach Datum – deshalb alle Seiten lesen und
/// clientseitig nach Belegdatum↓, BelId↓, BelPosId↓ waehlen. Positionen mit <c>MengeBasis &lt;= 0</c> zaehlen nicht (F2).
/// Lesefehler oder EBB leer: Warnung, nichts geaendert. Menge = <c>MengeBasis</c> (Basiseinheit).
/// </summary>
public class LetzteBestellungSyncService
{
    public const string ServiceUser = "LetzteBestellung";
    public const string KeyUhrzeitNacht = "Sync:LetzteBestellungUhrzeitNacht";
    public const string KeyUhrzeitMittag = "Sync:LetzteBestellungUhrzeitMittag";
    public const int DefaultUhrzeitNacht = 3;
    public const int DefaultUhrzeitMittag = 12;

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
    private readonly ILogger<LetzteBestellungSyncService> _logger;
    private readonly ISyncLogger _syncLogger;

    public LetzteBestellungSyncService(ApplicationDbContext ctx, ISageSdataReadClient client, IConfiguration config,
        ILogger<LetzteBestellungSyncService> logger, ISyncLogger syncLogger) // ISyncLogger als LETZTER Ctor-Parameter (ADR 0010)
    {
        _ctx = ctx;
        _client = client;
        _config = config;
        _logger = logger;
        _syncLogger = syncLogger;
    }

    /// <summary>Startet den Lauf nur, wenn <see cref="LetzteBestellungPlaner.Faellig"/>. Marker = juengste Startmeldung dieses Laufs im
    /// Aktivitaetsprotokoll (deckt Dienst-Neustarts ab, keine eigene Tabelle). true = Lauf gestartet.</summary>
    public async Task<bool> RunWennFaelligAsync(bool dryRun, DateTime jetzt, CancellationToken ct = default)
    {
        var nacht = await ServiceSettings.GetIntSafeAsync(_config, KeyUhrzeitNacht, DefaultUhrzeitNacht, ct);
        var mittag = await ServiceSettings.GetIntSafeAsync(_config, KeyUhrzeitMittag, DefaultUhrzeitMittag, ct);
        var letzterStart = await _ctx.SyncLogs.AsNoTracking()
            .Where(l => l.Service == SyncLogServices.LetzteBestellung && l.Message == "Run gestartet")
            .MaxAsync(l => (DateTime?)l.Timestamp, ct);
        if (!LetzteBestellungPlaner.Faellig(jetzt, letzterStart, new[] { nacht, mittag })) return false;
        await RunAsync(dryRun, ct);
        return true;
    }

    public async Task RunAsync(bool dryRun, CancellationToken ct = default, SdataLeseZiel? ziel = null)
    {
        await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.LetzteBestellung, ct);
        try
        {
            ziel ??= await SdataLeseZielLeser.LesenAsync(_config, ct);
            if (ziel.Warnung is { } zugangsWarnung) await run.LogWarningAsync(zugangsWarnung, ct: ct);
            var counts = await AbgleichenAsync(run, ziel, dryRun, DateTime.Now, ct);
            await run.FinishSuccessAsync(counts, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Letzte Bestellung (Einkauf) fehlgeschlagen.");
            await run.LogErrorAsync(ex.Message, ct: ct);
            await run.FinishFailedAsync(ex.Message, ct: ct);
            throw;
        }
    }

    private async Task<Dictionary<string, int>> AbgleichenAsync(ISyncRun run, SdataLeseZiel ziel, bool dryRun, DateTime now, CancellationToken ct)
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
            await run.LogWarningAsync(SdataSchluessel.Kappen($"{RessourceLetzteBestellung} nicht lesbar – gespeicherte Werte " +
                $"bleiben unveraendert: {ex.Message}"), ct: ct);
            return new() { ["lesefehler"] = 1 };
        }

        var ebb = jeArt[EinkaufBestellungenSyncService.Bestellung];
        if (ebb == 0)
        {
            // Bestellungen der letzten Jahre sind nie leer (AKE-Test: 36.288 Positionen in 3 Jahren) – 0 = Filter/Feld kaputt.
            await run.LogWarningAsync($"{RessourceLetzteBestellung} liefert 0 Bestellpositionen " +
                $"({WhereLetzteBestellung(EinkaufBestellungenSyncService.Bestellung, ab)}) – gespeicherte Werte bleiben unveraendert.", ct: ct);
            return new() { ["positionen"] = zeilen.Count };
        }

        var artikel = (await _ctx.Articles.AsNoTracking().Select(a => new { a.Id, a.ArticleNumber }).ToListAsync(ct))
            .GroupBy(a => a.ArticleNumber.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);
        var kreditoren = (await _ctx.Creditors.AsNoTracking().Where(c => c.SageKto != null).Select(c => new { c.Id, c.SageKto }).ToListAsync(ct))
            .GroupBy(c => c.SageKto!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        int ohneDaten = 0, artikelFehlt = 0, mengeNull = 0;
        var juengste = new Dictionary<(int ArticleId, string Kto), (DateTime Am, int BelId, int BelPosId, decimal Menge, string? Beleg, string Nummer)>();
        foreach (var z in zeilen)
        {
            var nummer = Text(z.Artikelnummer);
            var kto = Text(z.A0Empfaenger)?.ToUpperInvariant();
            var am = EinkaufBestellungenSyncService.Datum(z.Belegdatum);
            if (nummer == null || kto == null || am == null || z.MengeBasis is not { } menge || z.BelId is not { } belId) { ohneDaten++; continue; }
            if (menge <= 0m) { mengeNull++; continue; } // F2: Basismenge 0 ist keine Bestellung, die naechstjuengere gilt (Q-4, EK023626)
            if (!artikel.TryGetValue(nummer, out var articleId)) { artikelFehlt++; continue; }
            var beleg = Text(z.BelegnummerFormatiert);
            var neu = (Am: am.Value, BelId: belId, BelPosId: z.BelPosId ?? 0, Menge: Math.Round(menge, 3),
                Beleg: beleg is { Length: > SagePurchaseOrderLine.BelegnummerMaxLaenge } ? null : beleg, Nummer: nummer); // Spalte = Sage string(45)
            if (!juengste.TryGetValue((articleId, kto), out var bisher)
                || (neu.Am, neu.BelId, neu.BelPosId).CompareTo((bisher.Am, bisher.BelId, bisher.BelPosId)) > 0)
                juengste[(articleId, kto)] = neu;
        }

        var lieferanten = (await _ctx.ArticleSuppliers.ToListAsync(ct)).GroupBy(s => (s.ArticleId, s.CreditorId)).ToDictionary(g => g.Key, g => g.First());
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
            await run.LogWarningAsync($"{ohneDaten} Position(en) ohne Artikelnummer, Lieferant, Belegdatum, BelId oder MengeBasis uebersprungen.", ct: ct);
        if (mengeNull > 0)
            await run.LogInfoAsync($"{mengeNull} Position(en) mit MengeBasis 0 zaehlen nicht als letzte Bestellung.", ct: ct);
        if (artikelFehlt > 0)
            await run.LogInfoAsync($"{artikelFehlt} Position(en) zu Artikeln, die es im WMS nicht gibt — nicht uebernommen.", ct: ct);
        if (ohneZuordnungZahl > 0)
            await run.LogWarningAsync(SdataSchluessel.Kappen($"{ohneZuordnungZahl} Artikel/Lieferant ohne Artikel-Lieferant-Zeile " +
                $"im WMS – nicht abgelegt: {string.Join(", ", ohneZuordnung.Take(20))}"), ct: ct);
        if (!dryRun) await _ctx.SaveChangesAsync(ct);

        return new()
        {
            ["positionen"] = zeilen.Count,
            ["positionen_ebz"] = jeArt[SagePurchaseOrderLine.KennzeichenRahmenabruf],
            ["menge_null"] = mengeNull,
            ["gefunden"] = juengste.Count,
            ["aktualisiert"] = aktualisiert,
            ["geleert"] = geleert,
            ["ohne_zuordnung"] = ohneZuordnungZahl,
            ["uebersprungen"] = ohneDaten,
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

    private static string? Text(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
