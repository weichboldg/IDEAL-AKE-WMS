using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers.Api;

[ApiController]
[Route("api/picking")]
[RequirePickingAccess]
public class PickingApiController : ControllerBase
{
    private readonly IStorageLocationRepository _storageLocations;
    private readonly IStockMovementRepository _stockMovements;

    public PickingApiController(
        IStorageLocationRepository storageLocations,
        IStockMovementRepository stockMovements)
    {
        _storageLocations = storageLocations;
        _stockMovements = stockMovements;
    }

    /// <summary>
    /// Liefert Source-Location-Auswahl fuer Bom-Picking-Dropdown.
    /// Aktive, buchbare, nicht-Picking-Transport-Lagerplaetze.
    /// Sortiert: Bestand absteigend, dann Code.
    /// </summary>
    [HttpGet("source-locations")]
    public async Task<IActionResult> SearchSourceLocations(string? articleNumber, string? q, int limit = 50)
    {
        var locations = await _storageLocations.GetActiveOrderedExcludingPickingTransportAsync();

        // Stock pro Article einmal laden
        var stockByLoc = new Dictionary<int, decimal>();
        if (!string.IsNullOrWhiteSpace(articleNumber))
        {
            var stockDict = await _stockMovements.GetStockByArticleNumbersAsync(new List<string> { articleNumber });
            if (stockDict.TryGetValue(articleNumber, out var stockList))
            {
                foreach (var s in stockList)
                    stockByLoc[s.StorageLocationId] = s.Quantity;
            }
        }

        var filtered = locations.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            filtered = filtered.Where(l => l.Code.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        var ranked = filtered
            .Select(l =>
            {
                stockByLoc.TryGetValue(l.Id, out var qty);
                var hasStock = qty > 0;
                var label = hasStock ? $"{l.Code} ({qty:N3})" : l.Code;
                return new { id = l.Id, text = label, qty, hasStock };
            })
            .OrderByDescending(x => x.hasStock)
            .ThenByDescending(x => x.qty)
            .ThenBy(x => x.text)
            .Take(limit)
            .Select(x => new { id = x.id, text = x.text })
            .ToList();

        return Ok(ranked);
    }
}
