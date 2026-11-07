using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[RequireStockOrLagerbestellungAccess]
[RequireLagerbestellungAktiv]
public class MissingPartsController : Controller
{
    private readonly IWarehouseRequisitionRepository _repo;
    private readonly IProductionWorkplaceRepository _workplaces;
    private readonly ICurrentUserService _user;

    public MissingPartsController(
        IWarehouseRequisitionRepository repo,
        IProductionWorkplaceRepository workplaces,
        ICurrentUserService user)
    {
        _repo = repo;
        _workplaces = workplaces;
        _user = user;
    }

    public async Task<IActionResult> Index(
        ShortageStatus tab = ShortageStatus.WillBeRestocked,
        WarehouseRequisitionType? type = null,
        int? workplaceId = null,
        bool mineOnly = true,
        int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;

        // Tab-Normalisierung: None ist ungueltig fuer die Liste -> Default-Tab
        if (tab == ShortageStatus.None) tab = ShortageStatus.WillBeRestocked;

        // Typ-Auflösung (aeussere Lager/Glas-Reiter): nur erlaubte Typen zulassen
        var canOrderLager = await _user.CanOrderLagerAsync();
        var canOrderGlas = await _user.CanOrderGlasAsync();
        var activeType = type ?? (canOrderLager ? WarehouseRequisitionType.Lager : WarehouseRequisitionType.Glas);
        if (!Enum.IsDefined(activeType)) activeType = WarehouseRequisitionType.Lager;
        if (activeType == WarehouseRequisitionType.Glas && !canOrderGlas) activeType = WarehouseRequisitionType.Lager;
        if (activeType == WarehouseRequisitionType.Lager && !canOrderLager && canOrderGlas) activeType = WarehouseRequisitionType.Glas;

        var userDefaultPageSize = await _user.GetDefaultPageSizeAsync();
        var effectivePageSize = IdealAkeWms.Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = IdealAkeWms.Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        // Spec 2026-11-07-fehlteile-nur-meine-werkbaenke-trefferzahl: EINE Werkbank-Menge fuer Liste und beide Tab-Badges, gefiltert im
        // Repository vor Count/Paging (ADR 0005). null = kein Filter; leer = keine Treffer (fremde Werkbank bei „Nur meine Werkbaenke“).
        List<int>? userWorkplaceIds = null;
        IReadOnlyCollection<int>? workplaceIds = workplaceId.HasValue ? new[] { workplaceId.Value } : null;
        if (mineOnly)
        {
            var userId = _user.GetCurrentAppUserId() ?? 0;
            var userWorkplaces = await _workplaces.GetByUserIdAsync(userId);
            userWorkplaceIds = userWorkplaces.Select(w => w.Id).ToList();
            workplaceIds = workplaceId.HasValue
                ? (userWorkplaceIds.Contains(workplaceId.Value) ? new[] { workplaceId.Value } : Array.Empty<int>())
                : userWorkplaceIds;
        }

        bool hasNoWorkplaceMapping = false;
        if (mineOnly && userWorkplaceIds != null && userWorkplaceIds.Count == 0)
            hasNoWorkplaceMapping = true;

        if (hasNoWorkplaceMapping)
        {
            return View(new MissingPartsListViewModel
            {
                Items = new List<MissingPartRow>(),
                AvailableWorkplaces = (await _workplaces.GetByUserIdAsync(_user.GetCurrentAppUserId() ?? 0)).OrderBy(w => w.Name).ToList(),
                WorkplaceFilter = workplaceId,
                MineOnly = mineOnly,
                ActiveTab = tab,
                ActiveType = activeType,
                CanOrderLager = canOrderLager,
                CanOrderGlas = canOrderGlas,
                WaitingTotalCount = 0,
                NoRestockTotalCount = 0,
                HasNoWorkplaceMapping = true,
                Pagination = new PaginationState
                {
                    CurrentPage = page,
                    PageSize = effectivePageSize,
                    PageSizeRaw = rawPageSize,
                    TotalCount = 0
                }
            });
        }

        var columnFilters = IdealAkeWms.Services.ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);

        var (rows, total) = await _repo.GetMissingPartsAsync(
            tab, activeType, workplaceIds, columnFilters, null, null, page, effectivePageSize);

        // Counts fuer beide Tabs (Tab-Header-Badges)
        var waitingResult = await _repo.GetMissingPartsAsync(
            ShortageStatus.WillBeRestocked, activeType, workplaceIds, null, null, null, 1, 1);
        var noRestockResult = await _repo.GetMissingPartsAsync(
            ShortageStatus.NoRestock, activeType, workplaceIds, null, null, null, 1, 1);

        var vm = new MissingPartsListViewModel
        {
            Items = rows.ToList(),
            AvailableWorkplaces = (await _workplaces.GetAllAsync()).OrderBy(w => w.Name).ToList(),
            WorkplaceFilter = workplaceId,
            MineOnly = mineOnly,
            HasNoWorkplaceMapping = hasNoWorkplaceMapping,
            ActiveTab = tab,
            ActiveType = activeType,
            CanOrderLager = canOrderLager,
            CanOrderGlas = canOrderGlas,
            WaitingTotalCount = waitingResult.TotalCount,
            NoRestockTotalCount = noRestockResult.TotalCount,
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = total
            }
        };
        return View(vm);
    }
}
