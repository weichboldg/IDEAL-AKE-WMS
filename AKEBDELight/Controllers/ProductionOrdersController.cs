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
    private readonly IAppSettingRepository _settingRepository;
    private readonly IHolidayRepository _holidayRepository;
    private readonly IBusinessDayService _businessDayService;

    public ProductionOrdersController(
        IProductionOrderRepository productionOrderRepository,
        ICurrentUserService currentUserService,
        IAppSettingRepository settingRepository,
        IHolidayRepository holidayRepository,
        IBusinessDayService businessDayService)
    {
        _productionOrderRepository = productionOrderRepository;
        _currentUserService = currentUserService;
        _settingRepository = settingRepository;
        _holidayRepository = holidayRepository;
        _businessDayService = businessDayService;
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

        // Load settings for calculated dates
        var kommissionierTage = await _settingRepository.GetIntValueAsync("KommissionierTage", 4);
        var vorkommissionierTage = await _settingRepository.GetIntValueAsync("VorkommissionierTage", 1);
        var beschichtungTage = await _settingRepository.GetIntValueAsync("BeschichtungTage", 10);
        var holidays = await _holidayRepository.GetHolidayDatesAsync();

        // Map to view items with calculated dates
        var viewItems = orders.Select(o =>
        {
            var item = new ProductionOrderViewItem
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Quantity = o.Quantity,
                Customer = o.Customer,
                ArticleNumber = o.ArticleNumber,
                Description1 = o.Description1,
                Description2 = o.Description2,
                ProductionDate = o.ProductionDate,
                DeliveryDate = o.DeliveryDate,
                IsDone = o.IsDone
            };

            if (o.ProductionDate.HasValue)
            {
                item.KommissionierTermin = _businessDayService.SubtractBusinessDays(
                    o.ProductionDate.Value, kommissionierTage, holidays);
                item.VorkommissionierTermin = _businessDayService.SubtractBusinessDays(
                    item.KommissionierTermin.Value, vorkommissionierTage, holidays);
                item.BeschichtungTermin = _businessDayService.SubtractBusinessDays(
                    item.KommissionierTermin.Value, beschichtungTage, holidays);
            }

            return item;
        }).ToList();

        var vm = new ProductionOrderViewModel
        {
            Items = viewItems,
            FilterOrderNumber = filterOrderNumber,
            FilterArticleNumber = filterArticleNumber,
            FilterCustomer = filterCustomer,
            ShowDone = showDone,
            KommissionierTage = kommissionierTage,
            VorkommissionierTage = vorkommissionierTage,
            BeschichtungTage = beschichtungTage
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
