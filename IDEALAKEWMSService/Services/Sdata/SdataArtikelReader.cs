using System.Text.Json;
using System.Text.RegularExpressions;
using IdealAkeWms.Models;
using IdealAkeWms.Services.SageSdata;
using IDEALAKEWMSService.Common;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>Zeile aus <c>Artikel</c> (Namen exakt wie in der SData-Antwort, Typen laut P-S1-$schema).</summary>
public sealed record SdataArtikelZeile(
    string? Artikelnummer, string? Bezeichnung1, string? Bezeichnung2, string? Matchcode, string? Artikelgruppe,
    string? Hauptartikelgruppe, string? Vaterartikelgruppe, string? Ersatzartikelnummer, string? Hersteller, string? HArtikelnummer,
    string? Lagermengeneinheit, int? DezimalstellenLager, bool? Lagerfuehrung, int? PlatzID, string? Entnahmeverfahren, int? Inventurmethode,
    int? Chargenpflicht, int? Nachweispflicht, bool? IstBestellartikel, bool? IstFertigungsartikel, bool? IstUnterbaugruppe, bool? IstErsatzteil,
    bool? IstVerschleissteil, bool? Aktiv, int? Stuecklistentyp, string? Schwundart, decimal? Schwund, decimal? MinimaleLosgroesse,
    decimal? MaximaleLosgroesse, string? Zeichnungsnummer, decimal? Laenge, decimal? Breite, decimal? Hoehe, string? Gewichtseinheit, string? Langtext,
    string? Basismengeneinheit = null, int? DezimalstellenBasis = null, decimal? UmrechnungsFaktorLME = null, string? Verkaufsmengeneinheit = null,
    int? DezimalstellenVK = null, decimal? UmrechnungsfaktorVK = null, string? VerpackungseinheitVK = null, decimal? UmrechnungsfaktorVPVK = null);

/// <summary>Zeile aus <c>ArtikelVariantenExtended</c> (Namen exakt wie in der SData-Antwort).</summary>
public sealed record SdataArtikelVarianteZeile(
    string? Artikelnummer, int? AuspraegungID, string? EANNummer, string? ABCKlasse, decimal? Lagerbestand, decimal? LagerbestandOhneDispo,
    decimal? LagerbestandVerfuegbar, decimal? Gewicht, decimal? MittlererEK, decimal? Bestellmenge, decimal? Meldebestand, int? Bestellzeitpunkt,
    string? Hauptlieferant, string? LetzterLieferant, int? Wiederbeschaffungszeit, int? USER_LagerplatzID, decimal? USER_Mindestbestand);

/// <summary>Zeile aus <c>PpsRessourcenPositionenInfo</c> (nur die Ressourcenummer).</summary>
public sealed record SdataRessourceZeile(string? Ressourcenummer);

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E3a/F5): Artikel aus SData (<c>Artikel</c> + <c>ArtikelVariantenExtended</c>,
/// Hauptlagerplatz-Code ueber <c>LagerplaetzeInfo</c>). Liefert dieselben Felder wie <see cref="SageArtikelReader"/> und dazu die
/// Zusatzfelder fuer den Satelliten (<see cref="SageArtikelDto.Details"/>). Wirft bei jedem Lesefehler und bei unlesbaren Zeilen.
/// <para>
/// <b>Menge (G16 neu, 2026-11-07):</b> Der Reader bildet den am Code belegten AKE-Filter nach (<see cref="SageArticleSql.LegacySql"/>):
/// alle Ressourcenummern aus <c>PpsRessourcenPositionenInfo</c> (LEFT JOIN: auch ohne Artikelstamm) ∪ <c>IstBestellartikel = -1 AND
/// Aktiv = -1</c>. An AKE ist SData damit frei; TODO(P-R1): Der IDEAL-Filter steckt im Viewtext von <c>Sync:ArticlesViewName</c> und ist
/// nicht belegt — ist die View gesetzt, sperrt <see cref="ArtikelQuelleSwitch"/> SData (Regel <c>SageQuelleSperre</c>).
/// </para>
/// </summary>
public sealed class SdataArtikelReader : ISageArtikelReader
{
    public const string RessourceArtikel = "Artikel";
    public const string RessourceVarianten = "ArtikelVariantenExtended";
    public const string RessourceRessourcen = "PpsRessourcenPositionenInfo";
    public const int Blockgroesse = 200; // Artikelnummern je "in (…)"-Abfrage (URL-Laenge)

    public const string SelectArtikel = "Artikelnummer,Bezeichnung1,Bezeichnung2,Matchcode,Artikelgruppe,Hauptartikelgruppe,Vaterartikelgruppe,"
        + "Ersatzartikelnummer,Hersteller,HArtikelnummer,Lagermengeneinheit,DezimalstellenLager,Lagerfuehrung,PlatzID,Entnahmeverfahren,"
        + "Inventurmethode,Chargenpflicht,Nachweispflicht,IstBestellartikel,IstFertigungsartikel,IstUnterbaugruppe,IstErsatzteil,"
        + "IstVerschleissteil,Aktiv,Stuecklistentyp,Schwundart,Schwund,MinimaleLosgroesse,MaximaleLosgroesse,Zeichnungsnummer,Laenge,Breite,"
        + "Hoehe,Gewichtseinheit,Langtext,"
        // v1.85.0: Mengeneinheiten (Feldnamen am AKE-Test 2026-11-08 belegt, Spec artikeldetails Nachbesserung 5)
        + "Basismengeneinheit,DezimalstellenBasis,UmrechnungsFaktorLME,Verkaufsmengeneinheit,DezimalstellenVK,UmrechnungsfaktorVK,"
        + "VerpackungseinheitVK,UmrechnungsfaktorVPVK";
    // AuspraegungID nur fuer die stabile Sortierung; Feldname aus dem Schema (sage-sdata-artikel-felder), nicht in der P-S1-Typliste.
    public const string SelectVarianten = "Artikelnummer,AuspraegungID,EANNummer,ABCKlasse,Lagerbestand,LagerbestandOhneDispo,LagerbestandVerfuegbar,"
        + "Gewicht,MittlererEK,Bestellmenge,Meldebestand,Bestellzeitpunkt,Hauptlieferant,LetzterLieferant,Wiederbeschaffungszeit,"
        + "USER_LagerplatzID,USER_Mindestbestand";
    public const string SelectPlaetze = "PlatzID,Kurzbezeichnung";

    // TODO(P-R1): AKE-Filter angenommen (s. Klassen-Doku). Schreibweise -1 wie in der Sage-Tabelle (SData setzt where in SQL um,
    // Fehler kaemen als HTTP 500 mit SQL zurueck, nie als stilles Leer).
    public const string WhereBestellartikel = "IstBestellartikel eq -1 and Aktiv eq -1";
    // TODO(P-S1): PpsRessourcenPositionenInfo steht nicht in der P-S1-Typliste; Feldnamen Ressourcenummer/BelPosID aus dem
    // Schnittstellenkatalog (sage-sdata-commonwawiservicesresources). Ohne where wuerde die 100.000-Zeilen-Grenze greifen (240.552 am AKE-Test).
    // NICHT "ne ''": liefert auf Textfeldern still 0 Zeilen (AKE-Test 2026-11-07: ne '' → 0, like '_%' → 240.552 = alle).
    public const string WhereRessourcen = "Ressourcenummer like '_%'";
    public const string OrderByRessourcen = "Ressourcenummer,BelPosID";
    // ponytail: 240k Positionen in grossen Seiten (der Client kommt mit kleineren Server-Seiten zurecht); erst lesen, wenn G16 die Menge bestaetigt.
    public const int SeitengroesseRessourcen = 5000;

    private readonly ISageSdataReadClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SdataArtikelReader> _logger;

    public SdataArtikelReader(ISageSdataReadClient client, IConfiguration configuration, ILogger<SdataArtikelReader> logger)
    {
        _client = client;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>Hinweise des letzten Aufrufs (Ressourcen/Zeilen/Dauer je Abfrage).</summary>
    public IReadOnlyList<SdataHinweis> Hinweise { get; private set; } = Array.Empty<SdataHinweis>();

    public async Task<SageArtikelLesung> GetAllAsync(CancellationToken ct = default)
    {
        var ziel = await SdataLeseZielLeser.LesenAsync(_configuration, ct);
        // Zeile fehlt (null) = Katalog-Default; explizit leer = keine Benutzerfelder.
        var roh = await ServiceSettings.GetValueSafeAsync(_configuration, BenutzerfelderKey, ct)
            ?? ServiceSettingDefinitions.All.First(d => d.Key == BenutzerfelderKey).DefaultValue;
        return new(await LesenAsync(ziel, ct, BenutzerfelderParsen(roh)));
    }

    // ----- Benutzerfelder (E3b) -----

    public const string BenutzerfelderKey = "Sync:ArtikelBenutzerfelder";
    private static readonly Regex GueltigesBenutzerfeld = new("^USER_[A-Za-z0-9_]{1,100}$", RegexOptions.CultureInvariant);

    /// <summary>
    /// Kommagetrennte Liste aus <c>Sync:ArtikelBenutzerfelder</c>: gueltig = <c>USER_</c> + Buchstaben/Ziffern/Unterstrich (der Name landet
    /// im <c>select</c> der URL), doppelte (Gross/Klein egal) einmal. Ungueltige kommen als Warnung zurueck, nie in die Abfrage.
    /// </summary>
    public static (List<string> Gueltig, List<string> Ungueltig) BenutzerfelderParsen(string? roh)
    {
        var gueltig = new List<string>();
        var ungueltig = new List<string>();
        foreach (var teil in (roh ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!GueltigesBenutzerfeld.IsMatch(teil)) ungueltig.Add(teil);
            else if (!gueltig.Contains(teil, StringComparer.OrdinalIgnoreCase)) gueltig.Add(teil);
        }
        return (gueltig, ungueltig);
    }

    /// <summary>
    /// Liest die Benutzerfelder je Artikelnummer (eigene Abfrage, damit ein falscher Feldname nie den Artikel-Abgleich bricht).
    /// Antwortet Sage mit HTTP 500, wird jedes Feld einzeln an einem Artikel geprueft: Felder mit eigenem 500 = unbekannt (Warnung),
    /// die uebrigen werden erneut gelesen. Faellt kein Feld als Ursache auf, war es ein echter Fehler → werfen.
    /// TODO(P-S1): Annahme „unbekanntes Feld im select = HTTP 500“ (Etabliertes Muster E1); liefert Sage stattdessen nichts, greift die
    /// Pruefung „in keiner Zeile geliefert“ unten (Warnung).
    /// </summary>
    private async Task<Dictionary<string, Dictionary<string, string?>>> BenutzerfelderLesenAsync(SdataLeseZiel ziel, List<string> felder,
        IReadOnlyList<string> nummern, List<SdataHinweis> hinweise, CancellationToken ct)
    {
        var ergebnis = new Dictionary<string, Dictionary<string, string?>>(StringComparer.OrdinalIgnoreCase);
        if (felder.Count == 0 || nummern.Count == 0) return ergebnis;

        List<Dictionary<string, JsonElement>> zeilen;
        try
        {
            zeilen = await InBloeckenAsync<Dictionary<string, JsonElement>>(ziel, RessourceArtikel, "Artikelnummer," + string.Join(",", felder),
                "Artikelnummer", nummern, hinweise, ct);
        }
        catch (SdataLeseException ex) when (ex.Status == 500)
        {
            var unbekannt = new List<string>();
            foreach (var feld in felder)
            {
                try
                {
                    await _client.QueryAllAsync<Dictionary<string, JsonElement>>(ziel, SageSdataReadClient.VertragResources, RessourceArtikel,
                        InFilter(new[] { nummern[0] }), "Artikelnummer," + feld, "Artikelnummer", ct, 1);
                }
                catch (SdataLeseException probe) when (probe.Status == 500) { unbekannt.Add(feld); }
            }
            if (unbekannt.Count == 0) throw;
            foreach (var feld in unbekannt)
                hinweise.Add(new($"Benutzerfeld {feld} ist in Sage (Artikel) unbekannt – übersprungen. {BenutzerfelderKey} prüfen.", Warnung: true));
            felder = felder.Except(unbekannt).ToList();
            if (felder.Count == 0) return ergebnis;
            zeilen = await InBloeckenAsync<Dictionary<string, JsonElement>>(ziel, RessourceArtikel, "Artikelnummer," + string.Join(",", felder),
                "Artikelnummer", nummern, hinweise, ct);
        }

        var geliefert = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var roh in zeilen)
        {
            var zeile = new Dictionary<string, JsonElement>(roh, StringComparer.OrdinalIgnoreCase);
            if (!zeile.TryGetValue("Artikelnummer", out var nr) || nr.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(nr.GetString()))
                throw new SdataLeseException(RessourceArtikel, 0, "Benutzerfelder: Zeile ohne Artikelnummer – Feldnamen prüfen (P-S1).");
            var werte = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var feld in felder)
            {
                if (zeile.TryGetValue(feld, out var wert)) geliefert.Add(feld);
                werte[feld] = AlsText(wert);
            }
            ergebnis.TryAdd(Schluessel(nr.GetString()), werte);
        }
        foreach (var feld in felder.Where(f => zeilen.Count > 0 && !geliefert.Contains(f)))
            hinweise.Add(new($"Benutzerfeld {feld} wurde von Sage in keiner Zeile geliefert (unbekannt oder überall leer).", Warnung: true));
        return ergebnis;
    }

    /// <summary>JSON-Wert als Text fuer <c>ArticleAttributeValues.TextValue</c>; null/leer = null.</summary>
    // TODO(P-S1): Typen der USER-Felder unbelegt; Zahlen/Booleans als Rohtext (Sage-Bool z. B. "-1"), bis das $schema die Typen zeigt.
    internal static string? AlsText(JsonElement wert) => wert.ValueKind switch
    {
        JsonValueKind.String => string.IsNullOrWhiteSpace(wert.GetString()) ? null : wert.GetString()!.Trim(),
        JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => wert.GetRawText(),
        _ => null,
    };

    public async Task<List<SageArtikelDto>> LesenAsync(SdataLeseZiel ziel, CancellationToken ct,
        (List<string> Gueltig, List<string> Ungueltig) benutzerfelder = default)
    {
        var hinweise = new List<SdataHinweis>();
        Hinweise = hinweise;

        var bestell = await _client.QueryAllAsync<SdataArtikelZeile>(ziel, SageSdataReadClient.VertragResources, RessourceArtikel,
            WhereBestellartikel, SelectArtikel, "Artikelnummer", ct);
        hinweise.Add(new(bestell.Zusammenfassung));
        var ressourcen = await _client.QueryAllAsync<SdataRessourceZeile>(ziel, SageSdataReadClient.VertragResources, RessourceRessourcen,
            WhereRessourcen, "Ressourcenummer", OrderByRessourcen, ct, SeitengroesseRessourcen);
        hinweise.Add(new(ressourcen.Zusammenfassung));
        // Die PPS-Ressourcenpositionen sind nie leer (AKE-Test 240.552): 0 Zeilen = Filter/Feld kaputt. Werfen statt still nur die
        // Bestellartikel zu liefern — sonst fehlten im SData-Modus alle Nur-Ressource-Artikel (fail-safe, Vergleich meldet es als Fehler).
        if (ressourcen.Zeilen.Count == 0)
            throw new SdataLeseException(RessourceRessourcen, 0, $"0 Zeilen ({WhereRessourcen}) – Quelle kann nicht leer sein, Filter prüfen.");
        if (ressourcen.Zeilen.Count > 0 && ressourcen.Zeilen.All(r => string.IsNullOrWhiteSpace(r.Ressourcenummer)))
            throw new SdataLeseException(RessourceRessourcen, 0, $"{ressourcen.Zeilen.Count} Zeilen, aber keine mit Ressourcenummer – Feldname prüfen (P-S1).");

        var artikel = new List<SdataArtikelZeile>(bestell.Zeilen);
        var bekannt = new HashSet<string>(bestell.Zeilen.Select(a => Schluessel(a.Artikelnummer)), StringComparer.OrdinalIgnoreCase);
        var nurRessource = ressourcen.Zeilen.Select(r => r.Ressourcenummer).Where(n => !string.IsNullOrWhiteSpace(n) && !bekannt.Contains(Schluessel(n)))
            .Select(n => n!).DistinctBy(Schluessel, StringComparer.OrdinalIgnoreCase).ToList();
        artikel.AddRange(await InBloeckenAsync<SdataArtikelZeile>(ziel, RessourceArtikel, SelectArtikel, "Artikelnummer", nurRessource, hinweise, ct));

        var nummern = artikel.Select(a => a.Artikelnummer).Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n!).ToList();
        var varianten = await InBloeckenAsync<SdataArtikelVarianteZeile>(ziel, RessourceVarianten, SelectVarianten, "Artikelnummer,AuspraegungID",
            nummern, hinweise, ct);
        var plaetze = await _client.QueryAllAsync<SdataLagerplatzZeile>(ziel, SageSdataReadClient.VertragResources,
            SdataLagerplatzReader.RessourceLagerplaetze, null, SelectPlaetze, "PlatzID", ct);
        hinweise.Add(new(plaetze.Zusammenfassung));

        var result = Abbilden(artikel, nurRessource, varianten, plaetze.Zeilen);

        // E3b: Benutzerfelder nur fuer Artikel mit Stamm (Ressourcenummern ohne Artikelstamm haben keine). Fehlt eine Nummer in der
        // Antwort, bleibt ihr Benutzerfelder null (= Merkmale unberuehrt).
        foreach (var f in benutzerfelder.Ungueltig ?? new())
            hinweise.Add(new($"Benutzerfeld „{f}“ ist kein gültiger Feldname (erwartet USER_…) – übersprungen. {BenutzerfelderKey} prüfen.", Warnung: true));
        var stamm = artikel.Select(a => a.Artikelnummer!).DistinctBy(Schluessel, StringComparer.OrdinalIgnoreCase).ToList();
        var userWerte = await BenutzerfelderLesenAsync(ziel, benutzerfelder.Gueltig ?? new(), stamm, hinweise, ct);
        if (userWerte.Count > 0)
            result = result.Select(d => userWerte.TryGetValue(Schluessel(d.ArticleNumber), out var w) ? d with { Benutzerfelder = w } : d).ToList();
        _logger.LogInformation("Sage (SData) liefert {Count} Artikel. {Hinweise}", result.Count, string.Join(" | ", hinweise.Select(h => h.Text)));
        return result;
    }

    /// <summary>
    /// Wie der SQL-Select: je Artikelnummer (Gross/Klein, Leerzeichen am Ende egal) eine Zeile; Ressourcenummern ohne Artikelstamm nur mit
    /// Nummer (LEFT JOIN). Meldebestand 0 = kein Meldebestand, Hauptlagerplatz = Kurzbezeichnung der PlatzID (getrimmt, leer = null).
    /// </summary>
    internal static List<SageArtikelDto> Abbilden(IReadOnlyList<SdataArtikelZeile> artikel, IReadOnlyList<string> ressourcenummern,
        IReadOnlyList<SdataArtikelVarianteZeile> varianten, IReadOnlyList<SdataLagerplatzZeile> plaetze)
    {
        // Pflichtfelder: unlesbare Zeile = Feldname/Schema passt nicht -> werfen statt still weglassen.
        var kaputt = artikel.Count(a => string.IsNullOrWhiteSpace(a.Artikelnummer));
        if (kaputt > 0)
            throw new SdataLeseException(RessourceArtikel, 0, $"{kaputt} von {artikel.Count} Zeilen ohne Artikelnummer – Feldnamen prüfen (P-S1).");
        kaputt = varianten.Count(v => string.IsNullOrWhiteSpace(v.Artikelnummer));
        if (kaputt > 0)
            throw new SdataLeseException(RessourceVarianten, 0, $"{kaputt} von {varianten.Count} Zeilen ohne Artikelnummer – Feldnamen prüfen (P-S1).");

        // ponytail: mehrere Auspraegungen je Artikel -> die mit der kleinsten AuspraegungID (SQL nimmt MAX je Feld ueber alle Varianten);
        // eine Abweichung zeigt der Vergleich als Meldebestand-Differenz.
        var varianteJe = varianten.GroupBy(v => Schluessel(v.Artikelnummer), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.OrderBy(v => v.AuspraegungID ?? int.MaxValue).First(), StringComparer.OrdinalIgnoreCase);
        var platzJe = plaetze.Where(p => p.PlatzID is not null).GroupBy(p => p.PlatzID!.Value).ToDictionary(g => g.Key, g => g.First().Kurzbezeichnung);

        var result = new List<SageArtikelDto>();
        var gesehen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in artikel)
        {
            if (!gesehen.Add(Schluessel(a.Artikelnummer))) continue;
            var v = varianteJe.GetValueOrDefault(Schluessel(a.Artikelnummer));
            var platz = a.PlatzID is { } id ? platzJe.GetValueOrDefault(id) : null;
            result.Add(new SageArtikelDto(
                a.Artikelnummer!, a.Bezeichnung1, a.Lagermengeneinheit, a.Artikelgruppe,
                v?.Meldebestand is { } m && m != 0m ? m : null, // wie SageImportHelpers.ParseReorderLevel
                SageImportHelpers.NormalizeLocationCode(platz), a.Matchcode,
                Details(a, v)));
        }
        foreach (var n in ressourcenummern)
            if (gesehen.Add(Schluessel(n)))
                result.Add(new SageArtikelDto(n, null, null, null, null, null, null));
        return result;
    }

    private static SageArtikelDetails Details(SdataArtikelZeile a, SdataArtikelVarianteZeile? v) => new()
    {
        Bezeichnung2 = a.Bezeichnung2, Hauptartikelgruppe = a.Hauptartikelgruppe, Vaterartikelgruppe = a.Vaterartikelgruppe,
        Hersteller = a.Hersteller, HArtikelnummer = a.HArtikelnummer, Zeichnungsnummer = a.Zeichnungsnummer,
        Ersatzartikelnummer = a.Ersatzartikelnummer, Langtext = a.Langtext, Gewichtseinheit = a.Gewichtseinheit,
        Basismengeneinheit = (a.Basismengeneinheit), DezimalstellenBasis = a.DezimalstellenBasis, Lagermengeneinheit = (a.Lagermengeneinheit),
        UmrechnungsFaktorLME = Runde(a.UmrechnungsFaktorLME, 6), Verkaufsmengeneinheit = (a.Verkaufsmengeneinheit), DezimalstellenVK = a.DezimalstellenVK,
        UmrechnungsfaktorVK = Runde(a.UmrechnungsfaktorVK, 6), VerpackungseinheitVK = (a.VerpackungseinheitVK),
        UmrechnungsfaktorVPVK = Runde(a.UmrechnungsfaktorVPVK, 6),
        DezimalstellenLager = a.DezimalstellenLager, Lagerfuehrung = a.Lagerfuehrung, SagePlatzId = a.PlatzID,
        Entnahmeverfahren = a.Entnahmeverfahren, Inventurmethode = a.Inventurmethode, Chargenpflicht = a.Chargenpflicht,
        Nachweispflicht = a.Nachweispflicht, IstBestellartikel = a.IstBestellartikel, IstFertigungsartikel = a.IstFertigungsartikel,
        IstUnterbaugruppe = a.IstUnterbaugruppe, IstErsatzteil = a.IstErsatzteil, IstVerschleissteil = a.IstVerschleissteil, Aktiv = a.Aktiv,
        Stuecklistentyp = a.Stuecklistentyp, Schwundart = a.Schwundart, Schwund = R3(a.Schwund),
        MinimaleLosgroesse = R3(a.MinimaleLosgroesse), MaximaleLosgroesse = R3(a.MaximaleLosgroesse),
        Laenge = R3(a.Laenge), Breite = R3(a.Breite), Hoehe = R3(a.Hoehe),
        EANNummer = v?.EANNummer, ABCKlasse = v?.ABCKlasse, Gewicht = R3(v?.Gewicht), Lagerbestand = R3(v?.Lagerbestand),
        LagerbestandVerfuegbar = R3(v?.LagerbestandVerfuegbar), LagerbestandOhneDispo = R3(v?.LagerbestandOhneDispo),
        Bestellmenge = R3(v?.Bestellmenge), MittlererEK = Runde(v?.MittlererEK, 4), Bestellzeitpunkt = v?.Bestellzeitpunkt,
        Hauptlieferant = v?.Hauptlieferant, LetzterLieferant = v?.LetzterLieferant, Wiederbeschaffungszeit = v?.Wiederbeschaffungszeit,
        UserLagerplatzId = v?.USER_LagerplatzID, UserMindestbestand = R3(v?.USER_Mindestbestand),
    };

    /// <summary>Artikelnummern blockweise per <c>Artikelnummer in ('…',…)</c> lesen; eine Hinweis-Zeile fuer alle Bloecke.</summary>
    private async Task<List<T>> InBloeckenAsync<T>(SdataLeseZiel ziel, string ressource, string select, string orderBy,
        IReadOnlyList<string> nummern, List<SdataHinweis> hinweise, CancellationToken ct)
    {
        var zeilen = new List<T>();
        int bloecke = 0, seiten = 0;
        long ms = 0;
        foreach (var block in nummern.Chunk(Blockgroesse))
        {
            var r = await _client.QueryAllAsync<T>(ziel, SageSdataReadClient.VertragResources, ressource, InFilter(block), select, orderBy, ct);
            zeilen.AddRange(r.Zeilen);
            bloecke++;
            seiten += r.Seiten;
            ms += r.DauerMs;
        }
        if (bloecke > 0)
            hinweise.Add(new($"Quelle SData: {ressource}, {bloecke} Block/Blöcke à {Blockgroesse} Artikelnummern, {seiten} Seite(n), {zeilen.Count} Zeile(n), {ms} ms"));
        return zeilen;
    }

    internal static string InFilter(IEnumerable<string> nummern) =>
        "Artikelnummer in (" + string.Join(",", nummern.Select(n => "'" + n.Replace("'", "''") + "'")) + ")";

    private static string Schluessel(string? nummer) => nummer?.TrimEnd() ?? "";

    // TODO(P-S1): Nachkommastellen unbelegt; Spalten decimal(18,3) bzw. MittlererEK (18,4). Runden, damit der Satellit nicht bei jedem
    // Lauf eine Abweichung zur gespeicherten (gerundeten) Zahl sieht.
    private static decimal? R3(decimal? wert) => Runde(wert, 3);
    private static decimal? Runde(decimal? wert, int stellen) => wert is null ? null : Math.Round(wert.Value, stellen, MidpointRounding.AwayFromZero);
}
