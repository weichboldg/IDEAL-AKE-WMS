using IdealAkeWms.Models;

namespace IdealAkeWms.Services.Beschichtung;

/// <summary>Wer <c>ProductionOrderPickingStatus.HasCoatingParts</c> schreibt (Spec 2026-11-16-beschichtungslogik-je-standort A1).
/// Erweiterungspunkt (nicht gebaut, Freigabe-Antwort 17): eine SData-Quelle ueber <c>USER_OSBeschichtet</c> gehoert zu DIESER Achse.</summary>
public enum BeschichtungKennzeichenQuelle
{
    /// <summary>Lackierteil-Erkennung (Dienst): OSEON-Artikelkategorie ueber die WA-Stueckliste (AKE, flach).</summary>
    Artikelkategorie,
    /// <summary>FA-Materialisierung (Dienst): Spalte Beschichtet der Sage-FA-Struktur (IDEAL, hierarchisch).</summary>
    FaStruktur,
}

/// <summary>Woher der Beschichtungstermin kommt (A2).</summary>
public enum BeschichtungTerminQuelle
{
    /// <summary>Vorkommissioniertermin minus <c>BeschichtungTage</c>, auf den letzten Abholtag (<see cref="CoatingDateCalculator"/>).</summary>
    Berechnet,
    /// <summary><c>FaHierarchyOrderInfo.Start_Beschichtung</c> des eindeutigen Kopfes, unveraendert uebernommen (A4).</summary>
    View,
}

/// <summary>Aufgeloeste Quellen. <see cref="Warnungen"/> = ungueltige oder unpassende Rohwerte, die wie „Automatisch“ wirken (A8);
/// der Aufrufer protokolliert sie (Melden statt still behandeln).</summary>
public sealed record BeschichtungQuellen(
    BeschichtungKennzeichenQuelle Kennzeichen,
    BeschichtungTerminQuelle Termin,
    IReadOnlyList<string> Warnungen);

/// <summary>Was <see cref="BeschichtungResolver.Bestimmen"/> je Request braucht (einmal laden, je Zeile anwenden).</summary>
/// <param name="KategorieGesetzt"><c>LackierteilKategorieName</c> nicht leer (Aktiv-Kennung bei Quelle Artikelkategorie, A6).</param>
public sealed record BeschichtungKontext(
    BeschichtungQuellen Quellen,
    bool KategorieGesetzt,
    int BeschichtungTage,
    HashSet<DateTime> Feiertage,
    HashSet<DayOfWeek> Abholtage,
    IBusinessDayService BusinessDays);

/// <summary>Ergebnis je FA (A3). <see cref="Aktiv"/> = Beschichtungs-Spalte/Haken relevant.</summary>
public sealed record BeschichtungInfo(bool Beschichtet, DateTime? Termin, BeschichtungTerminQuelle TerminQuelle, bool Aktiv);

/// <summary>
/// Spec 2026-11-16-beschichtungslogik-je-standort (E1): EINE Stelle fuer Kennzeichen-Quelle, Termin-Quelle, Termin und Aktiv-Kennung.
/// Rein und ohne DB — Web und Dienst (ProjectReference) lesen die Rohwerte selbst und rufen <see cref="ResolveQuellen"/>.
/// „Automatisch“ = heutiges Verhalten je Auftragsmodell (Master <c>ProduktionsauftragHierarchisch</c>).
/// </summary>
public static class BeschichtungResolver
{
    public const string Automatisch = "Automatisch";

    /// <summary>Loest die beiden AppSettings auf. Fehlender/leerer Wert und „Automatisch“ = nach Auftragsmodell; ein unbekannter
    /// oder zum Auftragsmodell unpassender Wert (A8) wirkt wie „Automatisch“ und landet in <see cref="BeschichtungQuellen.Warnungen"/>.</summary>
    public static BeschichtungQuellen ResolveQuellen(string? kennzeichenRaw, string? terminRaw, bool hierarchisch)
    {
        var warnungen = new List<string>();
        var autoK = hierarchisch ? BeschichtungKennzeichenQuelle.FaStruktur : BeschichtungKennzeichenQuelle.Artikelkategorie;
        var autoT = hierarchisch ? BeschichtungTerminQuelle.View : BeschichtungTerminQuelle.Berechnet;
        // Kennzeichen: die Datenbasis gibt es nur im passenden Auftragsmodell (BomCache nur flach, FA-Struktur nur hierarchisch).
        var k = Waehle(AppSettingKeys.BeschichtungKennzeichenQuelle, kennzeichenRaw, autoK, q => q == autoK, hierarchisch, warnungen);
        // Termin: Berechnet geht immer; View nur hierarchisch (an der AKE keine Datumsspalte, bis P-A5 eine belegt — Freigabe-Antwort 15).
        var t = Waehle(AppSettingKeys.BeschichtungTerminQuelle, terminRaw, autoT,
            q => q == BeschichtungTerminQuelle.Berechnet || hierarchisch, hierarchisch, warnungen);
        return new BeschichtungQuellen(k, t, warnungen);
    }

    /// <summary>Kennzeichen, Termin und Aktiv-Kennung je FA. <c>Beschichtet</c> ist immer <paramref name="hasCoatingParts"/> (die eine
    /// materialisierte Wahrheit; die Quelle bestimmt nur, WER sie schreibt). View: <c>Start_Beschichtung</c> 1:1, nie ein Ersatztermin,
    /// kein <see cref="CoatingDateCalculator"/> (A4). Berechnet: Calculator ab dem Vorkommissioniertermin (A5).</summary>
    /// <param name="vorkommissionierTermin">Aus dem FA-Datum (hierarchisch: Kopf-<c>FE_Termin</c>) inkl. Werkbank-Override — wie heute je Zeile berechnet.</param>
    /// <param name="eindeutigerKopf">Der EINE Auftragskopf; null bei Kombigeraet oder ohne Kopf.</param>
    public static BeschichtungInfo Bestimmen(BeschichtungKontext k, bool hasCoatingParts, DateTime? vorkommissionierTermin,
        FaHierarchyOrderInfo? eindeutigerKopf)
    {
        var aktiv = k.Quellen.Kennzeichen == BeschichtungKennzeichenQuelle.FaStruktur || k.KategorieGesetzt;
        var termin = k.Quellen.Termin == BeschichtungTerminQuelle.View
            ? (hasCoatingParts ? eindeutigerKopf?.Start_Beschichtung : null)
            : CoatingDateCalculator.Compute(vorkommissionierTermin, k.BeschichtungTage, k.Feiertage, k.Abholtage, hasCoatingParts, aktiv, k.BusinessDays);
        return new BeschichtungInfo(hasCoatingParts, termin, k.Quellen.Termin, aktiv);
    }

    private static T Waehle<T>(string key, string? roh, T auto, Func<T, bool> passt, bool hierarchisch, List<string> warnungen)
        where T : struct, Enum
    {
        var t = roh?.Trim();
        if (string.IsNullOrEmpty(t) || t.Equals(Automatisch, StringComparison.OrdinalIgnoreCase)) return auto;
        var name = Enum.GetNames<T>().FirstOrDefault(n => n.Equals(t, StringComparison.OrdinalIgnoreCase));
        if (name == null)
        {
            warnungen.Add($"{Anzeige(key)}: „{roh}“ ist kein gültiger Wert – wirkt wie „Automatisch“ ({auto}).");
            return auto;
        }
        var q = Enum.Parse<T>(name);
        if (!passt(q))
        {
            warnungen.Add($"{Anzeige(key)}: „{q}“ passt nicht zum Auftragsmodell "
                + (hierarchisch ? "(hierarchisch, ProduktionsauftragHierarchisch an)" : "(flach, ProduktionsauftragHierarchisch aus)")
                + $" – wirkt wie „Automatisch“ ({auto}).");
            return auto;
        }
        return q;
    }

    private static string Anzeige(string key) =>
        AppSettingDefinitions.TryGet(key, out var d) && d.Anzeigename != null ? $"„{d.Anzeigename}“" : key;
}
