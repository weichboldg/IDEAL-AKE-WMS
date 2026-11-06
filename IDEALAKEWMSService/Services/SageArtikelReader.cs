using IDEALAKEWMSService.Common;
using Microsoft.Data.SqlClient;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// View-/SQL-Weg des Artikel-Abgleichs (A3). Der Lese-Code ist UNVERAENDERT aus <c>SageImportService.SyncArticlesAsync</c>
/// herausgezogen (Spec 2026-11-06-sage-sdata-stammdaten-sync, E3a/F5): <see cref="SageArticleSql.Select"/> (AKE-Legacy-Select bzw.
/// IDEAL-View aus <c>Sync:ArticlesViewName</c>), Existenzpruefung der View, Parsen von Meldebestand und Hauptlagerplatz.
/// </summary>
public sealed class SageArtikelReader : ISageArtikelReader
{
    private readonly IConfiguration _configuration;

    public SageArtikelReader(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<SageArtikelLesung> GetAllAsync(CancellationToken ct = default)
    {
        var sageConnection = _configuration.GetConnectionString("SageConnection")
            ?? throw new InvalidOperationException("SageConnection nicht konfiguriert.");

        // Quelle (Spec 2026-10-14-ideal-sage-views-umstellung): leer = bisheriger Select zeichengleich (AKE + IDEAL bis
        // zur Umstellung), gesetzt = View. Ungueltiger Name -> catch im Abgleich (Fehler + Mail, kein SQL).
        var viewRaw = await ServiceSettings.GetValueAsync(_configuration, "Sync:ArticlesViewName", ct);
        var quotedView = string.IsNullOrWhiteSpace(viewRaw) ? null : FaHierarchySql.ValidateAndQuote(viewRaw);
        var sageSql = SageArticleSql.Select(quotedView);

        var sageArticles = new List<SageArtikelDto>();

        await using var sageConn = new SqlConnection(sageConnection);
        await sageConn.OpenAsync(ct);

        // Fehlende View: Warnung + Skip, KEIN stiller Rueckfall auf den alten Select (der holte genau die
        // Daten zurueck, die die View abloesen soll), keine Mail, keine Mutation (Rueckfrage 6).
        if (quotedView != null && !await FaHierarchySql.SourceExistsAsync(sageConn, quotedView, ct))
            return new SageArtikelLesung(sageArticles, FaHierarchySql.SourceMissingMessage(quotedView));

        await using var cmd = new SqlCommand(sageSql, sageConn);
        cmd.CommandTimeout = 120;
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            string? reorderRaw = reader.IsDBNull(4) ? null : reader.GetString(4);
            string? primaryRaw = reader.IsDBNull(5) ? null : reader.GetString(5);
            sageArticles.Add(new SageArtikelDto(
                ArticleNumber: reader.GetString(0),
                Description:   reader.IsDBNull(1) ? null : reader.GetString(1),
                Unit:          reader.IsDBNull(2) ? null : reader.GetString(2),
                ArticleGroup:  reader.IsDBNull(3) ? null : reader.GetString(3),
                ReorderLevel:  SageImportHelpers.ParseReorderLevel(reorderRaw),
                PrimaryStorageLocation: SageImportHelpers.NormalizeLocationCode(primaryRaw),
                Matchcode:     reader.IsDBNull(6) ? null : reader.GetString(6)
            ));
        }

        return new SageArtikelLesung(sageArticles);
    }
}
