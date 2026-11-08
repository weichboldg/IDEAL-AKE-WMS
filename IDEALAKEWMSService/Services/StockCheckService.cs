using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models.ViewModels;
using Microsoft.Data.SqlClient;

namespace IDEALAKEWMSService.Services;

public class StockCheckService : IStockCheckService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StockCheckService> _logger;
    private readonly IStockMovementRepository _stock;

    public StockCheckService(IConfiguration configuration, ILogger<StockCheckService> logger, IStockMovementRepository stock)
    {
        _configuration = configuration;
        _logger = logger;
        _stock = stock;
    }

    /// <summary>v1.93.0 (Bug 2026-11-08-meldebestand-mail-bestand-falsch-berechnet): Bestand nach der Regel der Web-App
    /// (<see cref="IStockMovementRepository.GetStockByArticleNumbersAsync"/>: Umbuchung an Ziel UND Quelle, Kommissionierwagen auf beiden Seiten
    /// ausgenommen) statt eines eigenen SQL (dort zaehlte eine Umbuchung regulaer→regulaer als Zugang, Artikel ohne Bewegung fehlten).</summary>
    public async Task<List<StockBelowReorderItem>> GetArticlesBelowReorderLevelAsync(CancellationToken ct = default)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection nicht konfiguriert.");

        // Artikelstamm mit Meldebestand – keine Bestandslogik hier.
        const string sql = "SELECT [ArticleNumber], [Description], [Unit], [ReorderLevel] FROM [dbo].[Articles] WHERE [ReorderLevel] > 0";
        var articles = new List<(string ArticleNumber, string? Description, string? Unit, decimal ReorderLevel)>();
        await using (var connection = new SqlConnection(connectionString))
        {
            await connection.OpenAsync(ct);
            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                articles.Add((reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.GetDecimal(3)));
        }

        var stock = articles.Count == 0
            ? new Dictionary<string, List<StockLocationInfo>>()
            : await _stock.GetStockByArticleNumbersAsync(articles.Select(a => a.ArticleNumber).Distinct().ToList());
        var result = BuildBelowReorder(articles, stock);
        _logger.LogInformation("{Count} Artikel mit Bestand auf oder unter Meldebestand gefunden.", result.Count);
        return result;
    }

    /// <summary>Reine Zusammenfuehrung: Bestand = Summe der Plaetze (ohne Bewegung 0), gemeldet bei Bestand ≤ Meldebestand (nur Meldebestand
    /// &gt; 0), Lagerplaetze in der Reihenfolge des Repositories, sortiert nach Artikelnummer.</summary>
    public static List<StockBelowReorderItem> BuildBelowReorder(
        IEnumerable<(string ArticleNumber, string? Description, string? Unit, decimal ReorderLevel)> articles,
        IReadOnlyDictionary<string, List<StockLocationInfo>> stock) =>
        articles
            .Where(a => a.ReorderLevel > 0)
            .Select(a =>
            {
                var plaetze = stock.TryGetValue(a.ArticleNumber, out var l) ? l : new List<StockLocationInfo>();
                return new StockBelowReorderItem(a.ArticleNumber, a.Description, a.Unit, a.ReorderLevel, plaetze.Sum(p => p.Quantity),
                    plaetze.Select(p => p.Code).ToList());
            })
            .Where(i => i.CurrentStock <= i.ReorderLevel)
            .OrderBy(i => i.ArticleNumber, StringComparer.Ordinal)
            .ToList();

    public async Task<List<string>> GetNotificationRecipientsAsync(CancellationToken ct = default)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection nicht konfiguriert.");

        var recipients = new List<string>();

        // 1. Feste Empfänger aus ServiceSettings
        const string settingsSql = "SELECT [Value] FROM [dbo].[ServiceSettings] WHERE [Key] = 'Notifications:Recipients'";
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = new SqlCommand(settingsSql, conn);
        var settingsValue = await cmd.ExecuteScalarAsync(ct) as string;
        if (!string.IsNullOrWhiteSpace(settingsValue))
        {
            recipients.AddRange(settingsValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        // 2. User.Email wo NotifyOnReorderLevel = 1 AND Email IS NOT NULL
        const string userSql = "SELECT [Email] FROM [dbo].[Users] WHERE [NotifyOnReorderLevel] = 1 AND [Email] IS NOT NULL AND [Email] != '' AND [IsActive] = 1";
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
