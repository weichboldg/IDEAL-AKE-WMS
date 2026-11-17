using IdealAkeWms.Data;
using IdealAkeWms.Models;
using IdealAkeWms.Services;
using IdealAkeWms.Services.SageSdata;
using IdealAkeWms.Services.SyncLogger;
using IDEALAKEWMSService.Common;
using Microsoft.EntityFrameworkCore;

namespace IDEALAKEWMSService.Services.Sdata;

/// <summary>
/// Spec 2026-11-13-blechfertigung-bf-auftraege-wicam-csv, V-B: schmaler SData-Nachlese-Schritt im Artikel-Abgleich, solange die Artikel an
/// IDEAL ueber die View kommen (P-R1). Liest die Blechteile-Felder + Bestellmenge (<see cref="SdataArtikelReader.BlechteileFelderLesenAsync"/>,
/// nur GET) und schreibt sie ueber den vorhandenen <see cref="ArtikelBenutzerfelderWriter"/>. Artikel, die das Merkmal IstLagerartikel tragen,
/// aber nicht mehr geliefert werden, bekommen leere Werte (Writer entfernt sie). Ein Fehler hier ist nur eine Warnung: der View-Abgleich
/// bleibt gueltig. Faellt P-R1, Schalter <see cref="BfArtikelFelder.NachlesenKey"/> aus.
/// </summary>
public static class BlechteileFelderNachlese
{
    public static async Task LaufAsync(SdataArtikelReader reader, ApplicationDbContext ctx, IConfiguration configuration, ISyncRun run,
        CancellationToken ct)
    {
        if (!await ServiceSettings.GetBoolSafeAsync(configuration, BfArtikelFelder.NachlesenKey, false, ct)) return;
        await SchrittAsync(reader, ctx, run, () => SdataLeseZielLeser.LesenAsync(configuration, ct), ct);
    }

    /// <summary>Lesen + Schreiben mit eigenem try (testbar ohne DB-Schalter).</summary>
    internal static async Task SchrittAsync(SdataArtikelReader reader, ApplicationDbContext ctx, ISyncRun run, Func<Task<SdataLeseZiel>> ziel,
        CancellationToken ct)
    {
        var hinweise = new List<SdataHinweis>();
        try
        {
            var artikel = await reader.BlechteileFelderLesenAsync(await ziel(), hinweise, ct);
            foreach (var h in hinweise)
                if (h.Warnung) await run.LogWarningAsync(h.Text, ct: ct); else await run.LogInfoAsync(h.Text, ct: ct);
            if (artikel is null) return;

            var bf = await ArtikelBenutzerfelderWriter.SchreibenAsync(ctx, artikel.Concat(await NichtMehrGeliefertAsync(ctx, artikel, ct)), ct);
            foreach (var w in bf.Warnungen) await run.LogWarningAsync(w, ct: ct);
            await run.LogInfoAsync($"Blechteile-Felder (SData-Nachlese): {artikel.Count} Lagerartikel gelesen, {bf.DefinitionenNeu} Merkmal(e) angelegt, "
                + $"{bf.Neu} Wert(e) neu, {bf.Aktualisiert} aktualisiert, {bf.Entfernt} entfernt"
                + (bf.OhneArtikel > 0 ? $", {bf.OhneArtikel} ohne WMS-Artikel." : "."), ct: ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ctx.ChangeTracker.Clear(); // halb geschriebene Merkmale verwerfen; der View-Abgleich ist schon gespeichert
            await run.LogWarningAsync($"Blechteile-Felder (SData-Nachlese) fehlgeschlagen: {ex.Message} – Artikel-Abgleich (View) bleibt gültig, Merkmale unverändert.", ct: ct);
        }
    }

    /// <summary>WMS-Artikel mit Merkmal IstLagerartikel, die Sage nicht mehr als Lagerartikel liefert: alle Felder leer → Merkmale weg.</summary>
    internal static async Task<List<SageArtikelDto>> NichtMehrGeliefertAsync(ApplicationDbContext ctx, List<SageArtikelDto> geliefert, CancellationToken ct)
    {
        var da = geliefert.Select(a => a.ArticleNumber.TrimEnd()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var mitMerkmal = await ctx.ArticleAttributeValues
            .Where(v => v.ArticleAttributeDefinition.SyncSource == ArticleAttributeDefinition.SyncSourceSage
                        && v.ArticleAttributeDefinition.SyncFieldName == BfArtikelFelder.IstLagerartikel)
            .Select(v => v.Article.ArticleNumber).ToListAsync(ct);
        var leer = BfArtikelFelder.SageFelder.Append(BfArtikelFelder.Bestellmenge).ToDictionary(f => f, _ => (string?)null);
        return mitMerkmal.Where(n => !da.Contains(n.TrimEnd()))
            .Select(n => new SageArtikelDto(n, null, null, null, null, null, null) { Benutzerfelder = leer }).ToList();
    }
}
