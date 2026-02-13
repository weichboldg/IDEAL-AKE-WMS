using AKEBDELight.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AKEBDELight.Data.Repositories;

public class BomRepository : IBomRepository
{
    private readonly ApplicationDbContext _context;

    public BomRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BomItem>> GetBomItemsAsync(string productionOrderArticleNumber)
    {
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

        return await _context.Database
            .SqlQueryRaw<BomItem>(sql, productionOrderArticleNumber)
            .ToListAsync();
    }
}
