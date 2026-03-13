using Microsoft.Data.SqlClient;

namespace IDEALAKEWMSService.Services;

public class StockCheckService : IStockCheckService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StockCheckService> _logger;

    public StockCheckService(IConfiguration configuration, ILogger<StockCheckService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<StockBelowReorderItem>> GetArticlesBelowReorderLevelAsync(CancellationToken ct = default)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection nicht konfiguriert.");

        // MovementType ist int-Enum: 0=Einbuchung, 1=Ausbuchung, 2=Umbuchung
        // Kommissionierwagen (IsPickingTransport = 1) werden NICHT zum Lagerbestand gezählt:
        //   - sl_t = Ziellagerplatz (StorageLocationId), sl_s = Quelllagerplatz (SourceStorageLocationId)
        //   - Einbuchung (0) auf regulären Platz:          +Qty
        //   - Ausbuchung (1) von regulärem Platz:          -Qty
        //   - Umbuchung (2) Ziel regulär:                  +Qty  (Ware kommt auf regulären Platz)
        //   - Umbuchung (2) Quelle regulär:                -Qty  (Ware verlässt regulären Platz)
        //   - Ausbuchung (1) von Kommissionierwagen:        0    (war bereits via Umbuchung abgezogen)
        // STRING_AGG(DISTINCT ...) erst ab SQL Server 2022 — Distinct via Subquery für Kompatibilität.
        const string sql = """
            SELECT
                a.ArticleNumber,
                a.Description,
                a.Unit,
                a.ReorderLevel,
                SUM(CASE
                    WHEN sm.MovementType = 0 AND (sl_t.IsPickingTransport = 0 OR sl_t.Id IS NULL) THEN  sm.Quantity
                    WHEN sm.MovementType = 1 AND (sl_t.IsPickingTransport = 0 OR sl_t.Id IS NULL) THEN -sm.Quantity
                    WHEN sm.MovementType = 2 AND (sl_t.IsPickingTransport = 0 OR sl_t.Id IS NULL) THEN  sm.Quantity
                    WHEN sm.MovementType = 2 AND (sl_s.IsPickingTransport = 0 OR sl_s.Id IS NULL) THEN -sm.Quantity
                    ELSE 0
                END) AS CurrentStock,
                (
                    SELECT STRING_AGG(Code, ', ')
                    FROM (
                        SELECT DISTINCT sl2.Code
                        FROM StorageLocations sl2
                        INNER JOIN StockMovements sm2 ON sm2.StorageLocationId = sl2.Id
                        WHERE sm2.ArticleId = a.Id
                          AND sl2.IsPickingTransport = 0
                    ) AS dl
                ) AS StorageLocations
            FROM Articles a
            INNER JOIN StockMovements sm ON sm.ArticleId = a.Id
            LEFT JOIN StorageLocations sl_t ON sl_t.Id = sm.StorageLocationId
            LEFT JOIN StorageLocations sl_s ON sl_s.Id = sm.SourceStorageLocationId
            WHERE a.ReorderLevel IS NOT NULL AND a.ReorderLevel > 0
            GROUP BY a.Id, a.ArticleNumber, a.Description, a.Unit, a.ReorderLevel
            HAVING SUM(CASE
                WHEN sm.MovementType = 0 AND (sl_t.IsPickingTransport = 0 OR sl_t.Id IS NULL) THEN  sm.Quantity
                WHEN sm.MovementType = 1 AND (sl_t.IsPickingTransport = 0 OR sl_t.Id IS NULL) THEN -sm.Quantity
                WHEN sm.MovementType = 2 AND (sl_t.IsPickingTransport = 0 OR sl_t.Id IS NULL) THEN  sm.Quantity
                WHEN sm.MovementType = 2 AND (sl_s.IsPickingTransport = 0 OR sl_s.Id IS NULL) THEN -sm.Quantity
                ELSE 0
            END) <= a.ReorderLevel
            ORDER BY a.ArticleNumber
            """;

        var result = new List<StockBelowReorderItem>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            var locations = reader.IsDBNull(5) ? new List<string>() :
                reader.GetString(5).Split(", ", StringSplitOptions.RemoveEmptyEntries).ToList();

            result.Add(new StockBelowReorderItem(
                ArticleNumber: reader.GetString(0),
                Description: reader.IsDBNull(1) ? null : reader.GetString(1),
                Unit: reader.IsDBNull(2) ? null : reader.GetString(2),
                ReorderLevel: reader.GetDecimal(3),
                CurrentStock: reader.GetDecimal(4),
                StorageLocations: locations
            ));
        }

        _logger.LogInformation("{Count} Artikel mit Bestand auf oder unter Meldebestand gefunden.", result.Count);
        return result;
    }

    public async Task<List<string>> GetNotificationRecipientsAsync(CancellationToken ct = default)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection nicht konfiguriert.");

        var recipients = new List<string>();

        // 1. Feste Empfänger aus ServiceSettings
        const string settingsSql = "SELECT [Value] FROM [ServiceSettings] WHERE [Key] = 'Notifications:Recipients'";
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(settingsSql, conn);
        var settingsValue = await cmd.ExecuteScalarAsync(ct) as string;
        if (!string.IsNullOrWhiteSpace(settingsValue))
        {
            recipients.AddRange(settingsValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        // 2. User.Email wo NotifyOnReorderLevel = 1 AND Email IS NOT NULL
        const string userSql = "SELECT [Email] FROM [Users] WHERE [NotifyOnReorderLevel] = 1 AND [Email] IS NOT NULL AND [Email] != '' AND [IsActive] = 1";
        await using var cmd2 = new SqlCommand(userSql, conn);
        await using var reader = await cmd2.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var email = reader.GetString(0);
            if (!string.IsNullOrWhiteSpace(email) && !recipients.Contains(email, StringComparer.OrdinalIgnoreCase))
                recipients.Add(email);
        }

        return recipients;
    }
}
