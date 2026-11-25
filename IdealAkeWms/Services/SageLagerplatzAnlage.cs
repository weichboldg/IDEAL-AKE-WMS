using System.Globalization;
using System.Text.RegularExpressions;

namespace IdealAkeWms.Services;

/// <summary>
/// v1.99.71 (Spec 2026-11-24-lagerplatz-anlage-wms-nach-sage, ADR 0036): Lager und Lagerplaetze im WMS erfassen, der Dienst legt sie per
/// Direkt-SQL in Sage an. EINE Stelle fuer Web (Formular, Liste) und Dienst (Sende-Lauf): Schluessel, Lagertechniken, Code-Format.
/// </summary>
public static class SageLagerplatzAnlage
{
    /// <summary>Schalter (ServiceSetting, Default aus): Formular + „Erneut senden“ im Web, Senden im Dienst.</summary>
    public const string ToggleKey = "SageLagerplatzAnlageAktiv";
    /// <summary>Stufe der Lager-Anlage (Aus/Mitschnitt/Alle, Default Aus).</summary>
    public const string LagertechnikenKey = "SageLagerAnlageLagertechniken";
    public const string ServiceActor = "system:sage-lagerplatz-anlage";

    public const int MaxPlaetze = 200;
    public const int MaxKoordinate = 9999;

    public const string StufeAus = "Aus";
    public const string StufeMitschnitt = "Mitschnitt";
    public const string StufeAlle = "Alle";

    public static readonly (string Wert, string Text)[] StufenWerte =
    {
        (StufeAus, "Aus – keine Lager-Anlage, nur Plätze in vorhandenen Lagern"),
        (StufeMitschnitt, "Mitschnitt – LTD0ST, LTD1BL (im Sage-Mitschnitt belegt)"),
        (StufeAlle, "Alle – LTD0ST, LTD1BL, LTD1UF, LTD2SF, LTD3HR"),
    };

    public sealed record Technik(string Kennung, string Bezeichnung, string Dimensionen);

    // ponytail: Technikliste fest aus Anhang 4.4/P-5; neue Sage-Technik = Konstante erweitern
    public static readonly Technik[] Techniken =
    {
        new("LTD0ST", "Standardtechnik", ""),
        new("LTD1BL", "Blocklager", "Block"),
        new("LTD1UF", "unstrukturiertes Flächenlager", "Platz"),
        new("LTD2SF", "strukturiertes Flächenlager", "Reihe, Platz"),
        new("LTD3HR", "Hochregal", "Regal, Ebene, Platz"),
    };

    /// <summary>Am Standort fuer die Lager-NEUanlage freigegebene Techniken; unbekannter/leerer Wert = Aus.</summary>
    public static IReadOnlyList<string> TechnikenDerStufe(string? stufe) => stufe?.Trim() switch
    {
        StufeMitschnitt => new[] { "LTD0ST", "LTD1BL" },
        StufeAlle => Techniken.Select(t => t.Kennung).ToArray(),
        _ => Array.Empty<string>(),
    };

    /// <summary>Lagerkennung der Neuanlage: 1-10 Zeichen, nur A-Z/a-z, Ziffern, '-' und '_' (H10; schliesst ';' aus).</summary>
    public static readonly Regex KennungMuster = new("^[A-Za-z0-9_-]{1,10}$", RegexOptions.CultureInvariant);

    /// <summary>WMS-Code = Sage-Kurzbezeichnung <c>&lt;Lagerkennung&gt;;D1;D2;D3</c> (Anhang 5.2).</summary>
    public static string Code(string lagerkennung, int d1, int d2, int d3) =>
        string.Create(CultureInfo.InvariantCulture, $"{lagerkennung};{d1};{d2};{d3}");

    /// <summary>Koordinaten aus dem Code lesen; nur wenn er exakt dem Muster mit DIESER Lagerkennung folgt (ohne Gross/Klein).</summary>
    public static bool TryParseCode(string? code, string lagerkennung, out int d1, out int d2, out int d3)
    {
        d1 = d2 = d3 = 0;
        var teile = code?.Split(';');
        return teile is { Length: 4 }
               && string.Equals(teile[0], lagerkennung, StringComparison.OrdinalIgnoreCase)
               && int.TryParse(teile[1], NumberStyles.None, CultureInfo.InvariantCulture, out d1)
               && int.TryParse(teile[2], NumberStyles.None, CultureInfo.InvariantCulture, out d2)
               && int.TryParse(teile[3], NumberStyles.None, CultureInfo.InvariantCulture, out d3);
    }

    /// <summary>Kartesischer Bereich, D1 aussen, D3 innen (wie Sage, Anhang 5.1).</summary>
    public static IEnumerable<(int D1, int D2, int D3)> Koordinaten(int d1Von, int d1Bis, int d2Von, int d2Bis, int d3Von, int d3Bis)
    {
        for (var a = d1Von; a <= d1Bis; a++)
            for (var b = d2Von; b <= d2Bis; b++)
                for (var c = d3Von; c <= d3Bis; c++)
                    yield return (a, b, c);
    }

    /// <summary>Anzahl Koordinaten des Bereichs (long, damit 9999^3 nicht ueberlaeuft).</summary>
    public static long Anzahl(int d1Von, int d1Bis, int d2Von, int d2Bis, int d3Von, int d3Bis) =>
        (long)(d1Bis - d1Von + 1) * (d2Bis - d2Von + 1) * (d3Bis - d3Von + 1);
}
