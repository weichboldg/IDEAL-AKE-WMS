using IdealAkeWms.Data;
using IdealAkeWms.Models;
using Microsoft.EntityFrameworkCore;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>
/// Spec 2026-11-06-sage-sdata-stammdaten-sync (E3a/F1/F5): schreibt den Satelliten <c>ArticleSageDetails</c>. Regel: nur Artikel mit
/// <see cref="SageArtikelDto.Details"/> != null (= SData-Weg) werden angefasst; ohne Details (View, Vergleich) bleibt die Zeile, wie sie ist.
/// Die Details spiegeln Sage vollstaendig (auch null-Werte, die Sage so liefert). Audit wie der Artikel-Upsert: Schreiber
/// <c>IDEALAKEWMSService</c>, ModifiedAt/By/ByWindows nur, wenn sich wirklich ein Feld aendert.
/// </summary>
public static class ArticleSageDetailsWriter
{
    public const string Schreiber = "IDEALAKEWMSService"; // wie der Raw-SQL-Upsert (SageImportService)

    /// <returns>(neu, aktualisiert, Details ohne passenden WMS-Artikel)</returns>
    public static async Task<(int Neu, int Aktualisiert, int OhneArtikel)> SchreibenAsync(ApplicationDbContext ctx,
        IEnumerable<SageArtikelDto> artikel, CancellationToken ct)
    {
        var mitDetails = artikel.Where(a => a.Details is not null).ToList();
        if (mitDetails.Count == 0) return (0, 0, 0);

        var idJeNummer = await ArtikelIdsAsync(ctx, ct);
        var vorhanden = await ctx.ArticleSageDetails.ToDictionaryAsync(d => d.ArticleId, ct);

        var jetzt = DateTime.UtcNow;
        var windows = $@"{Environment.UserDomainName}\{Environment.UserName}"; // Dienstkonto, entspricht SYSTEM_USER des Raw-SQL-Upserts
        int neu = 0, geaendert = 0, ohneArtikel = 0;
        foreach (var a in mitDetails)
        {
            if (!idJeNummer.TryGetValue(a.ArticleNumber.TrimEnd(), out var articleId)) { ohneArtikel++; continue; }

            if (!vorhanden.TryGetValue(articleId, out var zeile))
            {
                zeile = new ArticleSageDetail { ArticleId = articleId, CreatedAt = jetzt, CreatedBy = Schreiber, CreatedByWindows = windows };
                ctx.ArticleSageDetails.Add(zeile);
                ctx.Entry(zeile).CurrentValues.SetValues(a.Details!);
                vorhanden[articleId] = zeile;
                neu++;
                continue;
            }

            var entry = ctx.Entry(zeile);
            entry.CurrentValues.SetValues(a.Details!); // nach Namen; markiert nur wirklich geaenderte Felder
            if (!entry.Properties.Any(p => p.IsModified)) continue;
            zeile.ModifiedAt = jetzt;
            zeile.ModifiedBy = Schreiber;
            zeile.ModifiedByWindows = windows;
            geaendert++;
        }

        await ctx.SaveChangesAsync(ct);
        return (neu, geaendert, ohneArtikel);
    }

    /// <summary>Artikelnummer → Id, wie SQL Server verglichen: Gross/Klein und Leerzeichen am Ende egal (auch fuer E3b).</summary>
    internal static async Task<Dictionary<string, int>> ArtikelIdsAsync(ApplicationDbContext ctx, CancellationToken ct)
    {
        var idJeNummer = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in await ctx.Articles.AsNoTracking().Select(a => new { a.Id, a.ArticleNumber }).ToListAsync(ct))
            idJeNummer.TryAdd(a.ArticleNumber.TrimEnd(), a.Id);
        return idJeNummer;
    }
}
