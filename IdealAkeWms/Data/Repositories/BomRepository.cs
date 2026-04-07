using System.Data;
using IdealAkeWms.Models.ViewModels;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace IdealAkeWms.Data.Repositories;

public class BomRepository : IBomRepository
{
    private readonly ApplicationDbContext _context;
    private readonly string _oseonConnectionString;
    private readonly IBomCacheRepository _bomCacheRepository;

    public BomRepository(ApplicationDbContext context, IConfiguration configuration, IBomCacheRepository bomCacheRepository)
    {
        _context = context;
        _oseonConnectionString = configuration.GetConnectionString("OseonConnection")
            ?? throw new InvalidOperationException("OseonConnection connection string is missing.");
        _bomCacheRepository = bomCacheRepository;
    }

    public async Task<BomQueryResult> GetBomItemsAsync(string productionOrderArticleNumber)
    {
        // 1. Cache-First
        var cached = await _bomCacheRepository.GetByArticleNumberAsync(productionOrderArticleNumber);
        if (cached != null && cached.Items.Count > 0)
            return cached;

        // 2. Live SAGE query
        var sql = @"
            SELECT
                [Artikelnummer],
                [Position],
                [Baugruppe],
                [Ressourcenummer],
                [Bezeichnung1],
                [Bezeichnung2],
                [Menge],
                [Beschaffungsartikel],
                [Artikelgruppe]
            FROM [ake].[dbo].[vw_AKE_Kommissionierung_StuecklistenDB]
            WHERE [Artikelnummer] = {0}";

        var items = await _context.Database
            .SqlQueryRaw<BomItem>(sql, productionOrderArticleNumber)
            .ToListAsync();

        if (items.Count > 0)
            return new BomQueryResult(items, "SAGE");

        // Fallback: Oseon (TRUMPF) Stored Procedure
        var oseonItems = await GetBomItemsFromOseonAsync(productionOrderArticleNumber);
        if (oseonItems.Count > 0)
            return new BomQueryResult(oseonItems, "OSEON");

        return new BomQueryResult(oseonItems, "KEINE_DATEN");
    }

    private async Task<List<BomItem>> GetBomItemsFromOseonAsync(string productionOrderArticleNumber)
    {
        var items = new List<BomItem>();

        await using var conn = new SqlConnection(_oseonConnectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand("sp_AKE_Kommissionierung_OseonStuecklistenDB", conn)
        {
            CommandType = CommandType.StoredProcedure
        };
        cmd.Parameters.AddWithValue("@artikelnummer", productionOrderArticleNumber);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new BomItem
            {
                Artikelnummer       = reader["Artikelnummer"] as string ?? string.Empty,
                Position            = reader["Position"] as string,
                Baugruppe           = reader["Baugruppe"] as string,
                Ressourcenummer     = reader["Ressourcenummer"] as string,
                Bezeichnung1        = reader["Bezeichnung1"] as string,
                Bezeichnung2        = reader["Bezeichnung2"] as string,
                Menge               = reader["Menge"] is decimal m ? m : 0m,
                Beschaffungsartikel = reader["Beschaffungsartikel"] as string,
                Artikelgruppe       = reader["Artikelgruppe"] as string,
            });
        }

        return items;
    }
}
