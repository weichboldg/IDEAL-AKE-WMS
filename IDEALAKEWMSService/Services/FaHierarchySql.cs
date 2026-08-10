using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// Wird geworfen, wenn ein konfigurierter View-Name die Whitelist-/QUOTENAME-Absicherung nicht
/// besteht. Der FaHierarchySyncService faengt das ab, bricht den Lauf ohne SQL-Ausfuehrung ab
/// (FinishFailedAsync) und schickt zusaetzlich eine Fehlermail (ISyncErrorNotifier).
/// </summary>
public sealed class FaHierarchyViewNameException : Exception
{
    public FaHierarchyViewNameException(string message) : base(message) { }
}

/// <summary>
/// Reiner, unit-testbarer Baustein fuer den FaHierarchy-Sync: View-Namen-Validierung
/// (Whitelist + QUOTENAME) und Aufbau der Roh-SQL-Texte. Objektnamen lassen sich in T-SQL nicht
/// parametrisieren — daher zweistufige Abwehr: (1) ASCII-explizite Whitelist je Namenssegment,
/// (2) QUOTENAME-aequivalentes Klammer-Quoting. Siehe Spec-Anforderung 8.
/// </summary>
public static class FaHierarchySql
{
    // ASCII-explizit (NICHT \w — \w matcht unter Unicode Homoglyphen, genau der relevante
    // Angriff bei extern konfigurierbaren Objektnamen). Bindestrich BEWUSST erlaubt: der reale
    // Default-View-Name traegt einen (vw_IDEAL-AKE_...).
    private static readonly Regex SegmentPattern =
        new("^[A-Za-z0-9_\\-]+$", RegexOptions.CultureInvariant);

    /// <summary>
    /// Validiert einen konfigurierten View-Namen (Format <c>[Schema].[Name]</c>, <c>Schema.Name</c>,
    /// <c>[Name]</c> oder <c>Name</c>) und liefert den vollstaendig geklammerten, direkt im FROM
    /// einsetzbaren Bezeichner zurueck: ein Bare-Name wird zu <c>[Name]</c> (Default-Schema der
    /// Connection = dbo), ein schema-qualifizierter Name zu <c>[Schema].[Name]</c>. Wirft
    /// <see cref="FaHierarchyViewNameException"/> bei jedem Verstoss. Weil die Build-Methoden diesen
    /// Bezeichner unveraendert (ohne eigenes <c>dbo.</c>-Praefix) einsetzen, ist ein
    /// schema-qualifiziert konfigurierter View-Name jetzt gleichwertig moeglich — der frueher
    /// noetige Betriebshinweis „ohne Schema-Praefix konfigurieren" entfaellt.
    /// </summary>
    public static string ValidateAndQuote(string? viewName)
    {
        if (string.IsNullOrWhiteSpace(viewName))
            throw new FaHierarchyViewNameException("View-Name ist leer/nicht konfiguriert.");

        var raw = viewName.Trim();

        // Whitespace an keiner Stelle zulaessig (auch nicht innerhalb).
        if (raw.Any(char.IsWhiteSpace))
            throw new FaHierarchyViewNameException($"View-Name '{viewName}' enthaelt Whitespace.");

        // Segmente an '.' trennen (Trenner zwischen Schema und Name).
        var segments = raw.Split('.');
        if (segments.Length is < 1 or > 2)
            throw new FaHierarchyViewNameException(
                $"View-Name '{viewName}' hat eine unzulaessige Anzahl an Segmenten ({segments.Length}).");

        var quoted = segments.Select(ValidateSegment).ToArray();
        return string.Join(".", quoted);
    }

    private static string ValidateSegment(string segment)
    {
        if (segment.Length == 0)
            throw new FaHierarchyViewNameException("View-Name enthaelt ein leeres Segment.");

        // Optionale, ausgeglichene aeussere Klammern entfernen; jede andere Klammer-Konstellation
        // (unausgeglichen, mitten im Segment) ist verboten.
        string inner;
        if (segment[0] == '[')
        {
            if (segment[^1] != ']')
                throw new FaHierarchyViewNameException(
                    $"View-Segment '{segment}' hat unausgeglichene eckige Klammern.");
            inner = segment[1..^1];
        }
        else
        {
            inner = segment;
        }

        if (inner.Length == 0)
            throw new FaHierarchyViewNameException("View-Name enthaelt ein leeres Segment (nur Klammern).");

        if (inner.Contains('[') || inner.Contains(']'))
            throw new FaHierarchyViewNameException(
                $"View-Segment '{segment}' enthaelt eckige Klammern an unzulaessiger Stelle.");

        // Zweizeichenfolge '--' (SQL-Kommentar-Einleiter) explizit ablehnen — sie besteht aus
        // erlaubten Einzelzeichen, kann aber in keinem echten Objektnamen vorkommen (Defense-in-Depth).
        if (inner.Contains("--", StringComparison.Ordinal))
            throw new FaHierarchyViewNameException(
                $"View-Segment '{segment}' enthaelt die verbotene Zeichenfolge '--'.");

        // Alles jenseits ASCII ablehnen (Homoglyphen-Schutz).
        if (inner.Any(c => c > 0x7F))
            throw new FaHierarchyViewNameException(
                $"View-Segment '{segment}' enthaelt Nicht-ASCII-Zeichen.");

        if (!SegmentPattern.IsMatch(inner))
            throw new FaHierarchyViewNameException(
                $"View-Segment '{segment}' entspricht nicht dem erlaubten Zeichensatz [A-Za-z0-9_-].");

        // QUOTENAME-Aequivalent: in eckige Klammern, enthaltene ']' verdoppeln (kann durch die
        // Whitelist nicht auftreten, aber strukturell korrekt).
        return "[" + inner.Replace("]", "]]") + "]";
    }

    /// <summary>COUNT(*) auf einen bereits validiert+geklammerten View-Bezeichner — Empty-Guard auf
    /// der ungefilterten Roh-Zeilenzahl. Der Bezeichner ist vollstaendig (ggf. schema-qualifiziert),
    /// daher KEIN zusaetzliches <c>dbo.</c>-Praefix (sonst entstuende <c>dbo.[dbo].[…]</c>).</summary>
    public static string BuildCountSql(string quotedView) =>
        $"SELECT COUNT_BIG(*) FROM {quotedView}";

    /// <summary>
    /// Node-Import: Existenzpruefung statt Zeilen-Join. Jede FAListe-Position landet GENAU EINMAL,
    /// unabhaengig von der Zahl der FAInfos-Zeilen (Montage-Abteilungen) zum selben HauptFA.
    /// </summary>
    public static string BuildNodeSelectSql(string quotedListeView, string quotedInfosView) =>
        $"""
        SELECT
            f.[HauptFA], f.[VaterFA], f.[SubFA], f.[Position],
            f.[HauptArtnr], f.[Artnr], f.[Matchcode], f.[Bezeichnung1], f.[Bezeichnung2],
            f.[Sollmenge], f.[Fertigungmenge], f.[Beschaffungsartikel], f.[Artikelgruppe],
            f.[Hauptlagerplatz], f.[BemerkungPN], f.[FertigungsInfoPN], f.[Kommissionieren],
            f.[Arbeitsbereich], f.[Arbeitsschritte], f.[Artikeltyp], f.[Beschichtet], f.[Material],
            f.[Breite], f.[Hoehe], f.[Tiefe], f.[EKBedarf], f.[VMBedarf]
        FROM {quotedListeView} f
        WHERE EXISTS (
            SELECT 1 FROM {quotedInfosView} i WHERE i.[HauptFA] = f.[HauptFA]
        )
        """;

    /// <summary>OrderInfo-Import: separat und vollstaendig (alle Zeilen, eine je Montage-Abteilung).</summary>
    public static string BuildInfosSelectSql(string quotedInfosView) =>
        $"""
        SELECT
            i.[HauptFA], i.[Montage-Abteilung] AS [MontageAbteilung], i.[ABNr], i.[Pos], i.[Kunde],
            i.[Status], i.[Dienstleister], i.[RAL], i.[Bemerkung_Uhrzeit],
            i.[KO_Termin], i.[FE_Termin], i.[Start_Beschichtung], i.[Beschichten_Retour],
            i.[Neuer_PT_PPS], i.[Verladetermin_Vsl], i.[Montagestunden], i.[Prio]
        FROM {quotedInfosView} i
        """;
}
