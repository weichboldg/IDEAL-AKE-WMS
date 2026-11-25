using System.Globalization;
using IdealAkeWms.Data;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Services;

/// <summary>v1.99.78: eine Zeile der Tageshistorie (Art = "Buchung" | "Teilmeldung").</summary>
public sealed record BdeTageshistorieZeile(string Art, string Typ, DateTime Start, DateTime? Ende, string Ziel, string? Werkbank,
    int? DauerMinuten, decimal? Gut, decimal? Ausschuss, string Status);

/// <summary>
/// v1.99.78 (Spec 2026-11-25-bde-masken-vereinheitlichung, Etappe 5): Tageshistorie des Werkers = nicht stornierte Buchungen mit
/// Start am Tag (wie die alte Terminal-Karte, api today-history) plus Teilmeldungen des Tages.
/// Fallstrick §69: ALLE Teilmeldungen (teil + fertig, Typ nach IstAbschliessend) – die Historie zeigt jede Handlung.
/// Fallstrick §67: FA- UND Lagerbestell-Meldungen (Lagerbestellung im Ziel gekennzeichnet).
/// Buchungen, die als BdeBookingId an einer gezeigten Teilmeldung haengen (AG-Fertigbuchung der Baugruppe), entfallen: eine Handlung = eine Zeile.
/// </summary>
public static class BdeTageshistorie
{
    private static readonly CultureInfo De = CultureInfo.GetCultureInfo("de-AT");

    public static async Task<List<BdeTageshistorieZeile>> LadenAsync(ApplicationDbContext ctx, int operatorId, DateTime tag)
    {
        var von = tag.Date;
        var bis = von.AddDays(1);
        var jetzt = DateTime.Now;

        var meldungen = await ctx.BdeTeilMeldungen.AsNoTracking()
            .Where(m => m.BdeOperatorId == operatorId && !m.IsCancelled && m.GemeldetAm >= von && m.GemeldetAm < bis)
            .Select(m => new
            {
                m.GemeldetAm, m.IstAbschliessend, m.HauptFA, m.SubFA, m.Position, m.Matchcode, m.GoodQuantity, m.ScrapQuantity,
                m.BdeBookingId, Lager = m.WarehouseRequisitionItemId != null, Werkbank = m.ProductionWorkplace.Name
            })
            .ToListAsync();
        var verknuepft = meldungen.Where(m => m.BdeBookingId != null).Select(m => m.BdeBookingId!.Value).ToHashSet();

        var buchungen = await ctx.BdeBookings.AsNoTracking()
            .Where(b => b.BdeOperatorId == operatorId && !b.IsCancelled && b.StartedAt >= von && b.StartedAt < bis)
            .Select(b => new
            {
                b.Id, b.BookingType, b.Status, b.StartedAt, b.EndedAt,
                Fa = b.WorkOperation != null ? b.WorkOperation.ProductionOrder.OrderNumber : null,
                Ag = b.WorkOperation != null ? b.WorkOperation.OperationNumber : null,
                AgName = b.WorkOperation != null ? b.WorkOperation.Name : null,
                Taetigkeit = b.BdeActivity != null ? b.BdeActivity.Name : null,
                Werkbank = b.ProductionWorkplace.Name,
                Gut = b.Quantities.Sum(q => q.GoodQuantity),
                Ausschuss = b.Quantities.Sum(q => q.ScrapQuantity)
            })
            .ToListAsync();

        var zeilen = buchungen.Where(b => !verknuepft.Contains(b.Id)).Select(b => new BdeTageshistorieZeile(
                "Buchung",
                b.BookingType switch { BdeBookingType.Setup => "Rüsten", BdeBookingType.Production => "Produktion", _ => "Tätigkeit" },
                b.StartedAt, b.EndedAt,
                b.Fa != null ? $"{b.Fa}/{b.Ag?.Trim()} — {b.AgName}" : b.Taetigkeit ?? "–",
                b.Werkbank,
                (int)((b.EndedAt ?? jetzt) - b.StartedAt).TotalMinutes,
                b.Gut, b.Ausschuss,
                b.Status switch
                {
                    BdeBookingStatus.Running => "läuft",
                    BdeBookingStatus.Paused or BdeBookingStatus.AutoPaused => "pausiert",
                    BdeBookingStatus.Resumed => "fortgesetzt",
                    _ => "beendet"
                }))
            .Concat(meldungen.Select(m => new BdeTageshistorieZeile(
                "Teilmeldung",
                m.IstAbschliessend ? "Fertig" : "Teilfertig",
                m.GemeldetAm, null,
                (m.Lager ? "Lagerbestellung · " : "") + $"FA {m.HauptFA} / Sub-FA {m.SubFA}"
                    + (m.Position != null ? $" · Pos. {m.Position}" : "") + (string.IsNullOrEmpty(m.Matchcode) ? "" : $" · {m.Matchcode}"),
                m.Werkbank, null, m.GoodQuantity, m.ScrapQuantity, "gemeldet")))
            .OrderByDescending(z => z.Start)
            .ToList();
        return zeilen;
    }

    private static string? Menge(decimal? v) => v?.ToString("#,##0.###", De);

    /// <summary>Spaltenfilter-Getter = Zelltext der View (Register <c>BdeTeileliste/Tageshistorie</c>).</summary>
    public static readonly IReadOnlyDictionary<string, Func<BdeTageshistorieZeile, string?>> Spalten = new Dictionary<string, Func<BdeTageshistorieZeile, string?>>
    {
        ["zeit"] = z => z.Start.ToString("HH:mm", De) + (z.Ende is { } e ? "–" + e.ToString("HH:mm", De) : ""),
        ["art"] = z => z.Art,
        ["typ"] = z => z.Typ,
        ["ziel"] = z => z.Ziel,
        ["werkbank"] = z => z.Werkbank,
        ["dauer"] = z => z.DauerMinuten?.ToString(De),
        ["gut"] = z => Menge(z.Gut),
        ["ausschuss"] = z => Menge(z.Ausschuss),
        ["status"] = z => z.Status,
    };
}
