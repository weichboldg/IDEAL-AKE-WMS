using Microsoft.Data.SqlClient;

namespace IDEALAKEWMSService.Services;

public class SageArbeitsplatzReader : ISageArbeitsplatzReader
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SageArbeitsplatzReader> _logger;

    public SageArbeitsplatzReader(IConfiguration configuration, ILogger<SageArbeitsplatzReader> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<SageArbeitsplatzDto>> GetActiveAsync(int mandant, CancellationToken ct = default)
    {
        var sageConnection = _configuration.GetConnectionString("SageConnection")
            ?? throw new InvalidOperationException("SageConnection nicht konfiguriert.");

        // Baustein a: Arbeitsplaetze je Mandant, aktiv (Sage-Konvention Aktiv = -1).
        // Arbeitsplatznummer bewusst typunabhaengig gelesen (GetValue().ToString()) — Sage fuehrt sie
        // laut Spec als varchar(31); der defensive Read traegt auch, falls die Spalte doch int ist.
        const string sql = """
            SELECT Arbeitsplatznummer, Bezeichnung1, USER_ArbeitsSchritt
            FROM KHKPpsArbeitsplaetze
            WHERE Mandant = @mandant
              AND Aktiv = -1
            """;

        var result = new List<SageArbeitsplatzDto>();

        await using var conn = new SqlConnection(sageConnection);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
        cmd.Parameters.AddWithValue("@mandant", mandant);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            result.Add(new SageArbeitsplatzDto(
                Arbeitsplatznummer: reader.IsDBNull(0) ? null : reader.GetValue(0)?.ToString(),
                Bezeichnung1: reader.IsDBNull(1) ? null : reader.GetString(1),
                Arbeitsschritt: reader.IsDBNull(2) ? null : reader.GetString(2)
            ));
        }

        _logger.LogInformation("Sage liefert {Count} aktive Arbeitsplaetze (Mandant {Mandant}).", result.Count, mandant);
        return result;
    }
}
