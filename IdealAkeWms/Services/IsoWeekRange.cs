using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using IdealAkeWms.Models;

namespace IdealAkeWms.Services;

/// <summary>
/// Loest KW-Zeitraum-Eingaben (Kalenderwoche, HTML-<c>&lt;input type="week"&gt;</c>-Format
/// <c>YYYY-Www</c>) in einen <b>ISO-Wochen-Datumsbereich Montag–Sonntag</b> auf. Genutzt von der
/// Kommissionier-Summiert-Ansicht (Filter auf <see cref="FaHierarchyOrderInfo.KO_Termin"/>) und der
/// Vormontage-Summiert-Ansicht (Filter auf <see cref="FaHierarchyOrderInfo.FE_Termin"/>).
///
/// <para>Das native <c>&lt;input type="week"&gt;</c> ist das etablierte, WCAG-AA-konforme
/// Wochen-Eingabemuster (eigenes Label, Tastaturbedienung, Locale-Formatierung durch den Browser)
/// und passt zum bestehenden „Von/Bis"-Datums-Filtermuster der uebrigen Listen (zwei Felder statt
/// eines Freitextes). Serverseitig wird der Wert deterministisch ueber
/// <see cref="ISOWeek"/> in Montag–Sonntag aufgeloest — unabhaengig vom Browser-Locale.</para>
///
/// Rein funktional/stateless und damit ohne DB unit-testbar.
/// </summary>
public static class IsoWeekRange
{
    /// <summary>
    /// Parst einen <c>YYYY-Www</c>-Wert (z. B. <c>"2026-W33"</c>) in (Jahr, KW). Liefert
    /// <c>null</c> bei leer/ungueltig (inkl. KW, die es im Jahr nicht gibt).
    /// </summary>
    public static (int Year, int Week)? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var v = value.Trim();
        var wPos = v.IndexOf("-W", StringComparison.OrdinalIgnoreCase);
        if (wPos <= 0) return null;

        var yearPart = v.Substring(0, wPos);
        var weekPart = v.Substring(wPos + 2);

        if (!int.TryParse(yearPart, NumberStyles.None, CultureInfo.InvariantCulture, out var year))
            return null;
        if (!int.TryParse(weekPart, NumberStyles.None, CultureInfo.InvariantCulture, out var week))
            return null;
        if (year < 1 || week < 1 || week > ISOWeek.GetWeeksInYear(year))
            return null;

        return (year, week);
    }

    /// <summary>
    /// Loest ein KW-von/KW-bis-Paar (<c>YYYY-Www</c>) in einen halboffenen Datumsbereich
    /// <c>[StartInclusive, EndExclusive)</c> auf: <c>StartInclusive</c> = Montag 00:00 der KW-von,
    /// <c>EndExclusive</c> = Montag 00:00 <b>nach</b> der KW-bis (also Sonntag 23:59:59 der KW-bis
    /// inklusive). Ist nur ein Feld gesetzt, gilt es fuer beide (eine einzelne Woche). Sind beide
    /// leer/ungueltig, liefert die Methode <c>null</c> (kein Filter). Bei vertauschter Reihenfolge
    /// (bis &lt; von) werden die Grenzen getauscht.
    /// </summary>
    public static (DateTime StartInclusive, DateTime EndExclusive)? Resolve(string? von, string? bis)
    {
        var pv = Parse(von);
        var pb = Parse(bis);
        if (pv is null && pb is null) return null;

        pv ??= pb;
        pb ??= pv;

        var vonMonday = ISOWeek.ToDateTime(pv!.Value.Year, pv.Value.Week, DayOfWeek.Monday).Date;
        var bisMonday = ISOWeek.ToDateTime(pb!.Value.Year, pb.Value.Week, DayOfWeek.Monday).Date;
        if (bisMonday < vonMonday)
            (vonMonday, bisMonday) = (bisMonday, vonMonday);

        return (vonMonday, bisMonday.AddDays(7));
    }

    /// <summary>
    /// Menge der <c>HauptFA</c>-Nummern, deren ausgewaehltes Termin-Feld (<paramref name="dateSelector"/>,
    /// z. B. <see cref="FaHierarchyOrderInfo.KO_Termin"/> oder <see cref="FaHierarchyOrderInfo.FE_Termin"/>)
    /// in den Bereich faellt. <b>Fan-out-frei</b>: die 1:n-Kopf-Zeilen werden auf ihre <c>HauptFA</c>
    /// reduziert (ein <c>HauptFA</c> ist enthalten, sobald <b>eine</b> seiner Kopf-Zeilen den Termin
    /// in der KW hat) — es findet kein Join gegen die Positionszeilen statt.
    /// </summary>
    public static HashSet<int> HauptFasInRange(
        IEnumerable<FaHierarchyOrderInfo> infos,
        Func<FaHierarchyOrderInfo, DateTime?> dateSelector,
        (DateTime StartInclusive, DateTime EndExclusive) range)
    {
        ArgumentNullException.ThrowIfNull(infos);
        ArgumentNullException.ThrowIfNull(dateSelector);

        var (start, endEx) = range;
        return infos
            .Where(o =>
            {
                var d = dateSelector(o);
                return d.HasValue && d.Value >= start && d.Value < endEx;
            })
            .Select(o => o.HauptFA)
            .ToHashSet();
    }
}
