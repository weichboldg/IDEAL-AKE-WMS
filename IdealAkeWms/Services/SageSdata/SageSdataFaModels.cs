using System.Text.Json.Serialization;

namespace IdealAkeWms.Services.SageSdata;

// Spec 2026-10-30-sage-sdata-fa-ansicht (v1.71.0): schlanke Lese-Modelle fuer SData RealTimeData — genau die angezeigten
// Felder, Namen exakt wie in der Sage-Antwort (Istmenge klein). Sage-Booleans bleiben roh als int? (-1 = wahr, 0 = falsch).

/// <summary>Ein FA-Beleg aus <c>dtsFaBelegsuche.Sage.PPS</c>.</summary>
public sealed record SageFaKopf(
    int BelID, int StrukturID, string? Artikelnummer, string? Matchcode,
    decimal? Sollmenge, decimal? Fertigungmenge, decimal? Istmenge,
    int? IstEingelastet, int? IstBegonnen, int? IstBeendet, int? IstGesperrt,
    string? VerursacherTyp, DateTimeOffset? Sollstarttermin, DateTimeOffset? Sollendtermin, DateTimeOffset? GeaendertAm);

/// <summary>Eine Position aus <c>dtsTeileverwendungsnachweis.Sage.PPS</c> (Herkunft 2 = FA-Beleg).</summary>
public sealed record SageFaPosition(
    string? Ressourcetyp, string? Ressourcenummer, string? Matchcode, decimal? Menge,
    string? Arbeitsgangnummer, string? Arbeitsplatznummer, string? Materialteileart,
    int? Herkunft, int? BelID, int? IstBeendet);

/// <summary>Zeile aus <c>dtsRueckKorrektur</c> bzw. <c>dtsFertigmeldungen</c> — nur die gezaehlten Felder.</summary>
public sealed record SageRueckmeldung(int? BelID, decimal? MengeGut);

/// <summary>Huelle einer SData-Feed-Antwort: <c>{"$resources":[…]}</c>. Weitere Felder ($url, $updated …) ignoriert STJ.</summary>
public sealed class SdataFeed<T>
{
    [JsonPropertyName("$resources")]
    public List<T>? Resources { get; set; }

    // Spec 2026-11-06-sage-sdata-stammdaten-sync (E0): Paging-Felder der CommonWawiServicesResources-Antwort.
    // AllowReadingFromString, damit ein als String gelieferter Wert die bestehende RealTimeData-Ansicht nicht bricht.
    [JsonPropertyName("$totalResults")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int? TotalResults { get; set; }

    [JsonPropertyName("$itemsPerPage")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public int? ItemsPerPage { get; set; }
}

/// <summary>Diagnose eines SData-Aufrufs. Status 0 = Verbindungsfehler (nicht erreichbar, Timeout, TLS).</summary>
public sealed record SdataCall(
    string Quelle, string? Url, int Status, long DauerMs, int Zeilen, string? Fehler, string? Rohdaten, bool Uebersprungen = false)
{
    // QA Q-1: 2xx mit unlesbarem Body (z. B. HTML-Loginseite) ist KEIN Erfolg — sonst wuerde daraus still
    // "nicht gefunden" bzw. "Keine Positionen".
    public bool Erfolgreich => !Uebersprungen && Status is >= 200 and < 300 && Fehler is null;
    public bool Verbindungsfehler => !Uebersprungen && Status == 0;

    // QA Q-2: neutral formuliert — uebersprungen wird nach Verbindungsfehler UND nach HTTP-Fehler bei Suche/Struktur.
    public static SdataCall Uebersprungene(string quelle) =>
        new(quelle, null, 0, 0, 0, "übersprungen (vorheriger Aufruf fehlgeschlagen)", null, true);
}

/// <summary>Ergebnis eines Lese-Aufrufs: Zeilen (bei Fehler leer) + Diagnose.</summary>
public sealed record SdataErgebnis<T>(IReadOnlyList<T> Zeilen, SdataCall Call);

/// <summary>Ziel und Zugang eines Lese-Aufrufs; vom Controller je Seitenaufbau aus Settings + Konfiguration gebaut.</summary>
public sealed record SdataLeseZiel(
    string BaseUrl, string Application, string Dataset, string Username, string Password, bool ZertifikatPruefen)
{
    // Zugangsdaten nie in Logs/Diagnose: ToString des Records wuerde sie sonst ausgeben.
    public override string ToString() => $"SdataLeseZiel {{ BaseUrl = {BaseUrl}, Dataset = {Dataset} }}";
}

public static class SageBool
{
    /// <summary>Sage liefert Booleans als -1 (wahr) / 0 (falsch).</summary>
    public static bool IsTrue(int? value) => value is not null and not 0;
}

/// <summary>Namen der gelesenen RealTimeData-Quellen.</summary>
public static class SageSdataQuellen
{
    public const string FaBelegsuche = "dtsFaBelegsuche.Sage.PPS";
    public const string Teileverwendungsnachweis = "dtsTeileverwendungsnachweis.Sage.PPS";
    public const string RueckKorrektur = "dtsRueckKorrektur.Sage.PPS";
    public const string Fertigmeldungen = "dtsFertigmeldungen.Sage.PPS";

    public const string FaSelect = "BelID,StrukturID,Artikelnummer,Matchcode,Sollmenge,Fertigungmenge,Istmenge,IstEingelastet,"
        + "IstBegonnen,IstBeendet,IstGesperrt,VerursacherTyp,Sollstarttermin,Sollendtermin,GeaendertAm";
    public const string PositionSelect = "BelID,Herkunft,Ressourcetyp,Ressourcenummer,Matchcode,Menge,Arbeitsgangnummer,"
        + "Arbeitsplatznummer,Materialteileart,IstBeendet";
    public const string RueckmeldungSelect = "BelID,MengeGut";

    /// <summary>Hoechstens so viele BelIDs je or-verknuepftem where (Freigabe-Nachtrag 1; Laenge der Request-Zeile).</summary>
    public const int MaxBelIdsJeAufruf = 50;

    /// <summary>
    /// <c>BelID eq 1 or BelID eq 2 …</c> in Bloecken zu hoechstens <paramref name="max"/>. Werte nur aus int — Freitext gelangt
    /// nie in die Abfrage (Vertrauensgrenze), daher ohne Quotes.
    /// </summary>
    public static IReadOnlyList<string> OrBloecke(string feld, IEnumerable<int> ids, int max = MaxBelIdsJeAufruf) =>
        ids.Distinct().Chunk(max).Select(b => string.Join(" or ", b.Select(id => $"{feld} eq {id}"))).ToList();
}
