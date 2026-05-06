using Microsoft.Data.SqlClient;

namespace IDEALAKEWMSService.Services;

public class SageBestandReader : ISageBestandReader
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SageBestandReader> _logger;

    public SageBestandReader(IConfiguration configuration, ILogger<SageBestandReader> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<SageBestandDto>> GetAllAsync(CancellationToken ct = default)
    {
        var sageConnection = _configuration.GetConnectionString("SageConnection")
            ?? throw new InvalidOperationException("SageConnection nicht konfiguriert.");

        // ANNAHME: KHKLagerplatzbestaende.Bestand ist decimal/numeric.
        // Convert.ToDecimal toleriert auch money/numeric/float.
        // KHKArtikel.Mandant = 1 — analog Artikel-Sync (User-Query-Annahme).
        const string sql = """
            SELECT
                A.Artikelnummer,
                LP.Kurzbezeichnung AS Lagerplatz,
                SUM(LB.Bestand) AS Bestand
            FROM [dbo].[KHKArtikel] AS A
            LEFT JOIN [dbo].[KHKArtikelVarianten] AS AV ON A.Artikelnummer = AV.Artikelnummer
            LEFT JOIN [dbo].[KHKLagerplatzbestaende] AS LB ON A.Artikelnummer = LB.Artikelnummer
            LEFT JOIN [dbo].[KHKLagerplaetze] AS LP ON LB.PlatzID = LP.PlatzID
            WHERE A.Mandant = 1
              AND LB.Bestand IS NOT NULL
            GROUP BY A.Artikelnummer, LP.Kurzbezeichnung, LB.Lagerkennung
            """;

        var result = new List<SageBestandDto>();

        await using var conn = new SqlConnection(sageConnection);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 120 };
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            result.Add(new SageBestandDto(
                Artikelnummer: reader.IsDBNull(0) ? null : reader.GetString(0),
                Lagerplatz:    reader.IsDBNull(1) ? null : reader.GetString(1),
                Bestand:       reader.IsDBNull(2) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(2))
            ));
        }

        _logger.LogInformation("Sage liefert {Count} Bestand-Tupel.", result.Count);
        return result;
    }
}
