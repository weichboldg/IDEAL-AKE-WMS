using IdealAkeWms.Models;
using IdealAkeWms.Services;

namespace IDEALAKEWMSService.Services;

/// <summary>Zustand eines FA in Sage (<c>KHKPpsFaBelege</c>), gelesen nur fuer verschwundene FAs. Nicht im Ergebnis = in Sage nicht gefunden.</summary>
public enum FaSageStatus { Offen, Beendet, Inaktiv }

/// <summary>Ergebnis von <see cref="FaStrukturAbgleich.Planen{T,K}"/>: neue Zeilen, Paare (Bestand, Read), fehlende Bestandszeilen.</summary>
public sealed record AbgleichPlan<T>(List<T> Neu, List<(T Alt, T Neu)> Paare, List<T> Fehlend);

/// <summary>
/// v1.99.37 (Spec 2026-11-17-fa-struktur-abgleich-statt-full-replace, ADR 0032): reiner, DB-freier Planer des FA-Struktur-Abgleichs (Muster
/// Materialisierung, ADR 0012). Schluessel folgen der Spalten-Kollation (Fallstrick 47): Text am Ende getrimmt, gross/klein egal, leer = null.
/// </summary>
public static class FaStrukturAbgleich
{
    /// <summary>Text wie die Spalten-Kollation ihn vergleicht: nachlaufende Leerzeichen und Gross/Klein egal, leer = null.</summary>
    public static string? Norm(string? s) => s?.TrimEnd() is { Length: > 0 } t ? t.ToUpperInvariant() : null;

    /// <summary>Leer bzw. nur Leerzeichen → null (so speichert der Abgleich, wie SQL/159 den Bestand umstellt).</summary>
    public static string? LeerAlsNull(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    /// <summary>Knoten: <c>(HauptFA, VaterFA, SubFA, Position, Artnr)</c> wie <c>UX_FaHierarchyNodes_Schluessel</c> (Ladder: <see cref="TeilSchluessel"/>).</summary>
    public static TeilSchluessel Schluessel(FaHierarchyNode n) => new(n.HauptFA, n.VaterFA, n.SubFA, n.Position, Norm(n.Artnr));

    /// <summary>OrderInfos: <c>(HauptFA, ABNr, Pos)</c> wie <c>UX_FaHierarchyOrderInfos_Schluessel</c>.</summary>
    public static (int, string?, string?) Schluessel(FaHierarchyOrderInfo i) => (i.HauptFA, Norm(i.ABNr), Norm(i.Pos));

    /// <summary>FA, in dessen Stueckliste die Zeile steht (Anforderung 2).</summary>
    public static int Owner(FaHierarchyNode n) => n.VaterFA ?? n.HauptFA;

    /// <summary>Dubletten-Waechter (EF InMemory erzwingt keinen UNIQUE-Index): Gruppen (HauptFA) mit doppeltem Schluessel im Read + die Schluessel.</summary>
    public static (HashSet<int> Gruppen, List<string> Schluessel) Doppelt<T, K>(IEnumerable<T> read, Func<T, K> schluessel, Func<T, int> gruppe)
        where K : notnull
    {
        var doppelt = read.GroupBy(schluessel).Where(g => g.Count() > 1).ToList();
        return (doppelt.Select(g => gruppe(g.First())).ToHashSet(), doppelt.Select(g => g.Key.ToString()!).ToList());
    }

    /// <summary>Neu / Paare / Fehlend je Schluessel; Gruppen in <paramref name="gesperrt"/> werden weder angelegt noch abgeglichen noch vermisst.</summary>
    public static AbgleichPlan<T> Planen<T, K>(IEnumerable<T> bestand, IEnumerable<T> read, Func<T, K> schluessel, Func<T, int> gruppe,
        IReadOnlySet<int> gesperrt) where K : notnull
    {
        var lesung = read.Where(r => !gesperrt.Contains(gruppe(r))).GroupBy(schluessel).ToDictionary(g => g.Key, g => g.First());

        var paare = new List<(T, T)>();
        var fehlend = new List<T>();
        var gesehen = new HashSet<K>();
        foreach (var alt in bestand)
        {
            if (gesperrt.Contains(gruppe(alt))) continue;
            var k = schluessel(alt);
            if (lesung.TryGetValue(k, out var neu) && gesehen.Add(k)) paare.Add((alt, neu));
            else fehlend.Add(alt);
        }
        var neue = lesung.Where(kv => !gesehen.Contains(kv.Key)).Select(kv => kv.Value).ToList();
        return new(neue, paare, fehlend);
    }

    /// <summary>Entscheidung je fehlender Zeile (Anforderung 3). null = entfernen, sonst Grund zum Behalten.</summary>
    /// <param name="subFa">Eigener Sub-FA eines FA-Knotens, sonst 0.</param>
    /// <param name="owner">FA, in dessen Stueckliste die Zeile steht.</param>
    /// <param name="gelesen">FAs, die dieser Lauf gelesen hat.</param>
    /// <param name="status">Sage-Zustand der abgefragten FAs (nicht enthalten = nicht gefunden).</param>
    public static string? Entscheiden(int subFa, int owner, IReadOnlySet<int> gelesen, IReadOnlyDictionary<int, FaSageStatus> status)
    {
        if (subFa != 0 && status.TryGetValue(subFa, out var sub) && sub == FaSageStatus.Beendet)
            return FaStrukturGrund.Beendet;
        if (gelesen.Contains(owner)) return null; // Position in Sage entfernt
        if (!status.TryGetValue(owner, out var s)) return null; // FA in Sage nicht gefunden
        return s switch
        {
            FaSageStatus.Beendet => FaStrukturGrund.Beendet,
            FaSageStatus.Inaktiv => FaStrukturGrund.Inaktiv, // bis Rueckfrage 1 behalten (Freigabe-Antwort 1)
            _ => FaStrukturGrund.Unklar,
        };
    }
}
