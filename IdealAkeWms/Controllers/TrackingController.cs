using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[RequireTrackingAccess]
public class TrackingController : Controller
{
    private readonly IWorkOperationRepository _workOperationRepository;
    private readonly IProductionWorkplaceRepository _workplaceRepository;
    private readonly ICurrentUserService _currentUserService;

    public TrackingController(
        IWorkOperationRepository workOperationRepository,
        IProductionWorkplaceRepository workplaceRepository,
        ICurrentUserService currentUserService)
    {
        _workOperationRepository = workOperationRepository;
        _workplaceRepository = workplaceRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IActionResult> Index(string? filterOrderNumber, int? filterWorkplaceId, bool showReported = false)
    {
        var allOperations = await _workOperationRepository.GetAllWithOrderAndWorkplaceAsync();

        // Filter anwenden
        if (!string.IsNullOrWhiteSpace(filterOrderNumber))
            allOperations = allOperations
                .Where(wo => wo.ProductionOrder.OrderNumber.Contains(filterOrderNumber, StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (filterWorkplaceId.HasValue)
            allOperations = allOperations
                .Where(wo => wo.ProductionWorkplaceId == filterWorkplaceId.Value)
                .ToList();

        if (!showReported)
            allOperations = allOperations
                .Where(wo => !wo.IsReported)
                .ToList();

        // Nach Auftrag gruppieren
        var groups = allOperations
            .GroupBy(wo => wo.ProductionOrderId)
            .Select(g =>
            {
                var po = g.First().ProductionOrder;
                var allOpsForOrder = g.ToList();
                return new TrackingOrderGroup
                {
                    ProductionOrderId = po.Id,
                    OrderNumber = po.OrderNumber,
                    ArticleNumber = po.ArticleNumber,
                    Description1 = po.Description1,
                    Customer = po.Customer,
                    ProductionDate = po.ProductionDate,
                    WorkplaceName = po.ProductionWorkplace?.Name,
                    TotalOperations = allOpsForOrder.Count,
                    ReportedOperations = allOpsForOrder.Count(o => o.IsReported),
                    Operations = allOpsForOrder.Select(MapToItem).ToList()
                };
            })
            .OrderBy(g => g.OrderNumber)
            .ToList();

        var vm = new TrackingViewModel
        {
            OrderGroups = groups,
            FilterOrderNumber = filterOrderNumber,
            FilterWorkplaceId = filterWorkplaceId,
            ShowReported = showReported,
            AvailableWorkplaces = await _workplaceRepository.GetAllOrderedAsync(),
            CanReport = await _currentUserService.CanReportOperationsAsync()
        };

        return View(vm);
    }

    public async Task<IActionResult> ByWorkplace(int id, string? filterOrderNumber, bool showReported = false)
    {
        var workplace = await _workplaceRepository.GetByIdAsync(id);
        if (workplace == null)
            return NotFound();

        var operations = showReported
            ? await _workOperationRepository.GetByWorkplaceIdAsync(id)
            : await _workOperationRepository.GetOpenByWorkplaceIdAsync(id);

        if (!string.IsNullOrWhiteSpace(filterOrderNumber))
            operations = operations
                .Where(wo => wo.ProductionOrder.OrderNumber.Contains(filterOrderNumber, StringComparison.OrdinalIgnoreCase))
                .ToList();

        var vm = new TrackingByWorkplaceViewModel
        {
            Workplace = workplace,
            Operations = operations.Select(MapToItem).ToList(),
            CanReport = await _currentUserService.CanReportOperationsAsync(),
            FilterOrderNumber = filterOrderNumber,
            ShowReported = showReported
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Report(int id, string? returnUrl)
    {
        if (!await _currentUserService.CanReportOperationsAsync())
            return RedirectToAction("AccessDenied", "Account");

        var operation = await _workOperationRepository.GetByIdAsync(id);
        if (operation == null)
            return NotFound();

        operation.IsReported = true;
        operation.ReportedAt = DateTime.UtcNow;
        operation.ReportedBy = _currentUserService.GetDisplayName();
        operation.ReportedByWindows = _currentUserService.GetWindowsUserName();
        operation.ModifiedAt = DateTime.UtcNow;
        operation.ModifiedBy = _currentUserService.GetDisplayName();
        operation.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _workOperationRepository.UpdateAsync(operation);

        TempData["SuccessMessage"] = $"Arbeitsgang '{operation.Name}' wurde rückgemeldet.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UndoReport(int id, string? returnUrl)
    {
        if (!await _currentUserService.CanReportOperationsAsync())
            return RedirectToAction("AccessDenied", "Account");

        var operation = await _workOperationRepository.GetByIdAsync(id);
        if (operation == null)
            return NotFound();

        operation.IsReported = false;
        operation.ReportedAt = null;
        operation.ReportedBy = null;
        operation.ReportedByWindows = null;
        operation.ModifiedAt = DateTime.UtcNow;
        operation.ModifiedBy = _currentUserService.GetDisplayName();
        operation.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _workOperationRepository.UpdateAsync(operation);

        TempData["SuccessMessage"] = $"Rückmeldung für '{operation.Name}' wurde zurückgenommen.";

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return RedirectToAction(nameof(Index));
    }

    private static TrackingOperationItem MapToItem(Models.WorkOperation wo)
    {
        return new TrackingOperationItem
        {
            Id = wo.Id,
            OperationNumber = wo.OperationNumber,
            Name = wo.Name,
            Sequence = wo.Sequence,
            WorkplaceName = wo.ProductionWorkplace?.Name,
            OrderNumber = wo.ProductionOrder?.OrderNumber,
            IsReportable = wo.IsReportable,
            IsExternalSystem = wo.IsExternalSystem,
            IsReported = wo.IsReported,
            ReportedAt = wo.ReportedAt,
            ReportedBy = wo.ReportedBy,
            ExternalSource = wo.ExternalSource
        };
    }
}
