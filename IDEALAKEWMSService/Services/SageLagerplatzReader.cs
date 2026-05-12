using Microsoft.Data.SqlClient;

namespace IDEALAKEWMSService.Services;

public class SageLagerplatzReader : ISageLagerplatzReader
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SageLagerplatzReader> _logger;

    public SageLagerplatzReader(IConfiguration configuration, ILogger<SageLagerplatzReader> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<SageLagerplatzDto>> GetAllActiveAsync(CancellationToken ct = default)
    {
        var sageConnection = _configuration.GetConnectionString("SageConnection")
            ?? throw new InvalidOperationException("SageConnection nicht konfiguriert.");

        // ANNAHME: KHKLagerorte hat eine Mandant-Spalte (analog Artikel-Sync).
        // Falls Sage-Schema das nicht hat, "AND lo.Mandant = 1" entfernen.
        const string sql = """
            SELECT lo.Lagerkennung, lp.Kurzbezeichnung, lp.Platzbezeichnung
            FROM KHKLagerorte lo
            LEFT JOIN KHKLagerplaetze lp ON lo.Lagerkennung = lp.Lagerkennung
            WHERE lo.Mandant = 1
              AND lo.Aktiv = -1
            """;

        var result = new List<SageLagerplatzDto>();

        await using var conn = new SqlConnection(sageConnection);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            result.Add(new SageLagerplatzDto(
                Lagerkennung: reader.IsDBNull(0) ? null : reader.GetString(0),
                Kurzbezeichnung: reader.IsDBNull(1) ? null : reader.GetString(1),
                Platzbezeichnung: reader.IsDBNull(2) ? null : reader.GetString(2)
            ));
        }

        _logger.LogInformation("Sage liefert {Count} aktive Lagerplaetze.", result.Count);
        return result;
    }
}
