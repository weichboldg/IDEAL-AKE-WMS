using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Controllers;

[RequireBdeShiftleadAccess]
public class BdeBookingsController : Controller
{
    private readonly IBdeBookingRepository _repo;
    private readonly IBdeOperatorRepository _ops;
    private readonly IProductionWorkplaceRepository _workplaces;

    public BdeBookingsController(
        IBdeBookingRepository repo,
        IBdeOperatorRepository ops,
        IProductionWorkplaceRepository workplaces)
    {
        _repo = repo;
        _ops = ops;
        _workplaces = workplaces;
    }

    public async Task<IActionResult> Index(int skip = 0, int take = 50, int? operatorId = null, int? workplaceId = null, DateTime? from = null, DateTime? to = null)
    {
        var list = await _repo.GetHistoryAsync(skip, take, operatorId, workplaceId, from, to);
        var vms = list.Select(b => new BdeBookingListViewModel
        {
            Id = b.Id,
            OperatorName = b.BdeOperator.DisplayName,
            WorkplaceName = b.ProductionWorkplace.Name,
            BookingType = b.BookingType.ToString(),
            Status = b.Status.ToString(),
            StartedAt = b.StartedAt,
            EndedAt = b.EndedAt,
            Target = b.WorkOperation != null
                ? $"{b.WorkOperation.ProductionOrder.OrderNumber}/{b.WorkOperation.OperationNumber}"
                : b.BdeActivity?.Name,
            TotalGood = b.Quantities.Sum(q => q.GoodQuantity),
            TotalScrap = b.Quantities.Sum(q => q.ScrapQuantity),
            IsCancelled = b.IsCancelled
        }).ToList();

        ViewBag.Operators = await _ops.GetAllActiveAsync();
        ViewBag.Workplaces = await _workplaces.GetAllOrderedAsync();
        ViewBag.Filter = new { skip, take, operatorId, workplaceId, from, to };

        return View(vms);
    }
}
