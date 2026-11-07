using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[Route("api/stock")]
[ApiController]
[RequireStockOrPickingOrTrackingAccess]
public class StockApiController : ControllerBase
{
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IProductionOrderRepository _productionOrders;
    private readonly ICurrentUserService _currentUser;
    private readonly IServiceSettingRepository _serviceSettings;

    public StockApiController(IStockMovementRepository stockMovementRepository, IProductionOrderRepository productionOrders,
        ICurrentUserService currentUser, IServiceSettingRepository serviceSettings)
    {
        _stockMovementRepository = stockMovementRepository;
        _productionOrders = productionOrders;
        _currentUser = currentUser;
        _serviceSettings = serviceSettings;
    }

    /// <summary>v1.87.0: Pruefkarte zum FA-Feld (Regel <see cref="IProductionOrderRepository.FindForBookingAsync"/>). <c>link</c> nur mit
    /// FA-Leserecht; Ziel FA-Struktur bei Master „hierarchisch“, sonst FA-Liste.</summary>
    [HttpGet("fa-lookup")]
    public async Task<IActionResult> FaLookup(string? nr)
    {
        var fa = await _productionOrders.FindForBookingAsync(nr);
        var o = fa.Order;
        string? link = null;
        if (o != null && await _currentUser.CanViewProductionOrdersAsync())
            link = await FaLinkAsync(o.OrderNumber);
        return Ok(new
        {
            status = fa.Status.ToString().ToLowerInvariant(),
            id = o?.Id,
            orderNumber = o?.OrderNumber,
            subOrderNumber = o?.SubOrderNumber,
            anzeigeNummer = o == null ? null : o.IsSubFa ? $"{o.SubOrderNumber} (Haupt-FA {o.OrderNumber})" : o.OrderNumber,
            description = o?.Description1,
            articleNumber = o?.ArticleNumber,
            statusText = o == null ? null : FaBuchungsText.Status(o),
            warnStatus = o != null && FaBuchungsText.IstWarnStatus(o),
            hinweis = fa.Status switch
            {
                FaAufloesungStatus.Unbekannt => $"FA {nr?.Trim()} ist im WMS unbekannt.",
                FaAufloesungStatus.Mehrdeutig => $"FA {nr?.Trim()} ist im WMS mehrdeutig.",
                FaAufloesungStatus.Liste => "Im Feld stehen mehrere FAs.",
                _ => null,
            },
            link,
        });
    }

    /// <summary>v1.87.0: Vorschlaege offener Haupt-FAs (bestehende Suche, max. 20).</summary>
    [HttpGet("fa-search")]
    public async Task<IActionResult> FaSearch(string? q)
    {
        if (string.IsNullOrWhiteSpace(q)) return Ok(Array.Empty<object>());
        var treffer = await _productionOrders.SearchAsync(q.Trim(), 20);
        return Ok(treffer.Select(o => new
        {
            orderNumber = o.OrderNumber,
            text = string.Join(" – ", new[] { o.OrderNumber, o.ArticleNumber, o.Description1, o.Customer }.Where(s => !string.IsNullOrWhiteSpace(s))),
        }));
    }

    private async Task<string?> FaLinkAsync(string orderNumber)
    {
        var hierarchisch = string.Equals((await _serviceSettings.GetValueAsync(
            Services.HierarchischeStruktur.HierarchischeStrukturKeys.Master))?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        return hierarchisch
            ? Url.Action("Index", "FaHierarchy", new { filterHauptFa = orderNumber })
            : Url.Action("Index", "ProductionOrders", new { filterOrderNumber = orderNumber });
    }

    [HttpGet("by-order/{orderNumber}")]
    public async Task<IActionResult> GetStockByOrder(string orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            return BadRequest("orderNumber is required.");

        var items = await _stockMovementRepository.GetStockByProductionOrderAsync(orderNumber);

        return Ok(items.Select(i => new
        {
            articleNumber = i.ArticleNumber,
            description = i.ArticleDescription ?? "",
            storageLocation = i.StorageLocationCode,
            quantity = i.CurrentQuantity,
            unit = i.Unit ?? ""
        }));
    }
}
