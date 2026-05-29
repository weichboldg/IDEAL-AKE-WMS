using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[RequirePickingOrStockAccess]
public class WarehouseRequisitionsController : Controller
{
    private readonly IWarehouseRequisitionRepository _repo;
    private readonly IProductionWorkplaceRepository _workplaces;
    private readonly IOrderRecipientRepository _groups;
    private readonly ICurrentUserService _user;
    private readonly IAppSettingRepository _settings;

    public WarehouseRequisitionsController(
        IWarehouseRequisitionRepository repo,
        IProductionWorkplaceRepository workplaces,
        IOrderRecipientRepository groups,
        ICurrentUserService user,
        IAppSettingRepository settings)
    {
        _repo = repo; _workplaces = workplaces; _groups = groups; _user = user; _settings = settings;
    }

    public async Task<IActionResult> Index(int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var userDefaultPageSize = await _user.GetDefaultPageSizeAsync();
        var effectivePageSize = IdealAkeWms.Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = IdealAkeWms.Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var userId = _user.GetCurrentAppUserId() ?? 0;
        var displayName = _user.GetDisplayName();
        var all = await _repo.GetForUserAsync(userId);
        // Stabiler Filter via CreatedByUserId; Fallback auf CreatedBy fuer Altdaten ohne UserId.
        var ownOnly = all.Where(r => r.CreatedByUserId == userId
            || (r.CreatedByUserId == null && r.CreatedBy == displayName)).ToList();

        var totalCount = ownOnly.Count;
        var paged = ownOnly.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList();

        var (missingItemCount, missingReqCount) = await _repo.GetFinalShortagesCountForUserAsync(userId);

        var vm = new WarehouseRequisitionListViewModel
        {
            Items = paged.Select(r => new WarehouseRequisitionListItemViewModel(
                r.Id,
                r.ProductionWorkplace?.Name ?? "",
                r.CreatedBy,
                r.CreatedAt,
                r.SubmittedAt,
                r.Items.Count,
                r.Status)).ToList(),
            AvailableWorkplaces = await _workplaces.GetByUserIdAsync(userId),
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = totalCount
            },
            MissingPartsItemCount = missingItemCount,
            MissingPartsRequisitionCount = missingReqCount,
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDraft(int? workplaceId)
    {
        var userId = _user.GetCurrentAppUserId() ?? 0;
        var workplaces = await _workplaces.GetByUserIdAsync(userId);

        if (workplaces.Count == 0)
        {
            TempData["WarningMessage"] = "Bitte Werkbank-Zuordnung in Stammdaten pflegen.";
            return RedirectToAction(nameof(Index));
        }

        int chosenWp;
        if (workplaceId.HasValue && workplaces.Any(w => w.Id == workplaceId.Value))
        {
            chosenWp = workplaceId.Value;
        }
        else if (workplaces.Count == 1)
        {
            chosenWp = workplaces[0].Id;
        }
        else
        {
            TempData["WarningMessage"] = "Bitte Werkbank waehlen.";
            return RedirectToAction(nameof(Index));
        }

        var newId = await _repo.CreateDraftAsync(chosenWp, userId, _user.GetDisplayName(), _user.GetWindowsUserName());
        return RedirectToAction(nameof(Edit), new { id = newId });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var r = await _repo.GetByIdAsync(id);
        if (r == null) return NotFound();
        var userId = _user.GetCurrentAppUserId() ?? 0;
        var displayName = _user.GetDisplayName();
        // Wenn CreatedByUserId gesetzt ist, primaer per Id pruefen; sonst Fallback auf Display-Name.
        var ownsRequisition = r.CreatedByUserId != null
            ? r.CreatedByUserId == userId
            : r.CreatedBy == displayName;
        if (!ownsRequisition)
            return Forbid();

        var vm = new WarehouseRequisitionEditViewModel
        {
            Id = r.Id,
            WorkplaceName = r.ProductionWorkplace?.Name ?? "",
            Status = r.Status,
            CreatedAt = r.CreatedAt,
            RowVersion = r.RowVersion,
            Items = r.Items.OrderBy(i => i.Position).Select(i =>
                new WarehouseRequisitionEditItemViewModel(i.Id, i.Position, i.ArticleNumber, i.ArticleDescription, i.Unit, i.QuantityRequested)).ToList()
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(int id)
    {
        var r = await _repo.GetByIdAsync(id);
        if (r == null) return NotFound();
        if (r.Status != WarehouseRequisitionStatus.Draft)
        {
            TempData["WarningMessage"] = "Nur Entwurfe koennen abgeschickt werden.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        if (r.Items.Count == 0)
        {
            TempData["WarningMessage"] = "Bitte mindestens einen Artikel hinzufuegen.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        var groupId = await _settings.GetIntValueAsync("DefaultLagerbestellempfaengerId", 0);
        if (groupId <= 0)
        {
            TempData["WarningMessage"] = "Default-Lagerbestellempfaenger nicht konfiguriert (Einstellungen).";
            return RedirectToAction(nameof(Edit), new { id });
        }
        var grp = await _groups.GetGroupByIdAsync(groupId);
        if (grp == null)
        {
            TempData["WarningMessage"] = "Konfigurierte Empfaenger-Gruppe nicht gefunden.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        try
        {
            await _repo.SubmitAsync(id, groupId, _user.GetCurrentAppUserId() ?? 0,
                _user.GetDisplayName(), _user.GetWindowsUserName(), r.RowVersion);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            TempData["WarningMessage"] = "Bestellung wurde inzwischen geaendert — bitte Liste neu laden.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        TempData["SuccessMessage"] = $"Liste #{id} abgeschickt — wird per E-Mail gesendet (max. 15 Min).";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason)
    {
        var r = await _repo.GetByIdAsync(id);
        if (r == null) return NotFound();
        if (r.Status != WarehouseRequisitionStatus.Draft && r.Status != WarehouseRequisitionStatus.Submitted)
        {
            TempData["WarningMessage"] = "Liste kann in diesem Status nicht storniert werden.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        try
        {
            await _repo.CancelAsync(id, reason, _user.GetCurrentAppUserId() ?? 0,
                _user.GetDisplayName(), _user.GetWindowsUserName(), r.RowVersion);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            TempData["WarningMessage"] = "Bestellung wurde inzwischen geaendert — bitte Liste neu laden.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        TempData["SuccessMessage"] = $"Liste #{id} storniert.";
        return RedirectToAction(nameof(Index));
    }
}
