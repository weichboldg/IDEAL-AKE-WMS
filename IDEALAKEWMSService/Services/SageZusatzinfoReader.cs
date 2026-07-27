using Microsoft.Data.SqlClient;

namespace IDEALAKEWMSService.Services;

/// <summary>
/// Raw-SQL-Read der FA-Zusatzinfos aus Sage (SageConnection, immer dbo.-Praefix).
/// Defensive CASTs = hartes Abschneiden an der Quelle statt Truncation-Fehler beim
/// Write (Muster der bestehenden Sage-Reads). View-Existenz-Guard VOR dem Read:
/// fehlt die View, liefert der Reader ViewExists=false — der Sync endet dann
/// regulaer (kein Mail-Spam alle 15 min auf Systemen ohne View).
/// </summary>
public class SageZusatzinfoReader : ISageZusatzinfoReader
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SageZusatzinfoReader> _logger;

    public SageZusatzinfoReader(IConfiguration configuration, ILogger<SageZusatzinfoReader> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SageZusatzinfoReadResult> ReadAsync(CancellationToken ct = default)
    {
        var sageConnection = _configuration.GetConnectionString("SageConnection")
            ?? throw new InvalidOperationException("SageConnection nicht konfiguriert.");

        await using var conn = new SqlConnection(sageConnection);
        await conn.OpenAsync(ct);

        // View-Existenz-Guard (Spec §2, Pflicht).
        await using (var checkCmd = new SqlCommand(
            "SELECT OBJECT_ID('dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen', 'V')", conn))
        {
            var objId = await checkCmd.ExecuteScalarAsync(ct);
            if (objId == null || objId == DBNull.Value)
            {
                _logger.LogWarning("Sage-View vw_IDEAL_AKE_WMS_FAZusatzinformationen nicht vorhanden — Sync wird uebersprungen.");
                return new SageZusatzinfoReadResult(false, new List<SageZusatzinfoRow>());
            }
        }

        const string sql = """
            SELECT CAST([WA Nummer] AS nvarchar(100))       AS WaNummer,
                   CAST(Kaeltemittel AS nvarchar(200))      AS Kaeltemittel,
                   CAST(Ventil AS nvarchar(200))            AS Ventil,
                   CAST([Ausfuehrung E/Z] AS nvarchar(200)) AS AusfuehrungEZ,
                   CAST(Maschine AS nvarchar(200))          AS Maschine,
                   CAST(Status AS nvarchar(200))            AS Status
            FROM dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen
            """;

        var rows = new List<SageZusatzinfoRow>();
        await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 120 };
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            rows.Add(new SageZusatzinfoRow(
                WaNummer:      reader.IsDBNull(0) ? null : reader.GetString(0),
                Kaeltemittel:  reader.IsDBNull(1) ? null : reader.GetString(1),
                Ventil:        reader.IsDBNull(2) ? null : reader.GetString(2),
                AusfuehrungEZ: reader.IsDBNull(3) ? null : reader.GetString(3),
                Maschine:      reader.IsDBNull(4) ? null : reader.GetString(4),
                Status:        reader.IsDBNull(5) ? null : reader.GetString(5)));
        }

        _logger.LogInformation("Sage liefert {Count} FA-Zusatzinfo-Zeilen.", rows.Count);
        return new SageZusatzinfoReadResult(true, rows);
    }
}
