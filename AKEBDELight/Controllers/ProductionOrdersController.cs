using Microsoft.AspNetCore.Mvc;
using AKEBDELight.Data.Repositories;
using AKEBDELight.Models;
using AKEBDELight.Models.ViewModels;
using AKEBDELight.Services;

namespace AKEBDELight.Controllers;

public class ProductionOrdersController : Controller
{
    private readonly IProductionOrderRepository _productionOrderRepository;
    private readonly ICurrentUserService _currentUserService;

    public ProductionOrdersController(
        IProductionOrderRepository productionOrderRepository,
        ICurrentUserService currentUserService)
    {
        _productionOrderRepository = productionOrderRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index(
        string? filterOrderNumber,
        string? filterArticleNumber,
        string? filterCustomer,
        bool showDone = false)
    {
        var orders = await _productionOrderRepository.GetAllOrderedAsync();

        // Filter anwenden
        if (!string.IsNullOrWhiteSpace(filterOrderNumber))
        {
            orders = orders.Where(o => o.OrderNumber.Contains(filterOrderNumber, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(filterArticleNumber))
        {
            orders = orders.Where(o => o.ArticleNumber != null && o.ArticleNumber.Contains(filterArticleNumber, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(filterCustomer))
        {
            orders = orders.Where(o => o.Customer != null && o.Customer.Contains(filterCustomer, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!showDone)
        {
            orders = orders.Where(o => !o.IsDone).ToList();
        }

        var vm = new ProductionOrderViewModel
        {
            Items = orders,
            FilterOrderNumber = filterOrderNumber,
            FilterArticleNumber = filterArticleNumber,
            FilterCustomer = filterCustomer,
            ShowDone = showDone
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleDone(int id, string? returnUrl)
    {
        var order = await _productionOrderRepository.GetByIdAsync(id);
        if (order == null)
            return NotFound();

        order.IsDone = !order.IsDone;
        order.ModifiedAt = DateTime.Now;
        order.ModifiedBy = _currentUserService.GetDisplayName();
        order.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _productionOrderRepository.UpdateAsync(order);

        if (!string.IsNullOrEmpty(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }
}
