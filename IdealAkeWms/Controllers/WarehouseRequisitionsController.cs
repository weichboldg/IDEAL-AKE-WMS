using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

[RequirePickingOrStockOrLagerbestellungAccess]
[RequireLagerbestellungAktiv]
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

    /// <summary>
    /// Server-Side-Spaltenfilter: Col-Key (data-col-key der View) -> gerenderter Zell-Text.
    /// Die Getter MUESSEN exakt das liefern, was die View in der Zelle rendert
    /// (deutsche Status-Badge-Texte, Datum im View-Format, "—" fuer leer).
    /// </summary>
    private static readonly Dictionary<string, Func<WarehouseRequisitionListItemViewModel, string?>> ColumnMap = new()
    {
        ["id"] = r => r.Id.ToString(),
        ["workplace"] = r => r.WorkplaceName,
        ["items"] = r => r.ItemCount.ToString(),
        ["status"] = r => r.Status switch
        {
            WarehouseRequisitionStatus.Draft => "Entwurf",
            WarehouseRequisitionStatus.Submitted => "Abgeschickt",
            WarehouseRequisitionStatus.PartiallyDelivered => "Teilgeliefert",
            WarehouseRequisitionStatus.Closed => "Erledigt",
            WarehouseRequisitionStatus.Cancelled => "Storniert",
            _ => r.Status.ToString()
        },
        ["created"] = r => r.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
        ["submitted"] = r => r.SubmittedAt?.ToString("dd.MM.yyyy HH:mm") ?? "—",
    };

    public async Task<IActionResult> Index(WarehouseRequisitionType? type = null, int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var userDefaultPageSize = await _user.GetDefaultPageSizeAsync();
        var effectivePageSize = IdealAkeWms.Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = IdealAkeWms.Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var canOrderLager = await _user.CanOrderLagerAsync();
        var canOrderGlas = await _user.CanOrderGlasAsync();
        var activeType = type ?? (canOrderLager ? WarehouseRequisitionType.Lager : WarehouseRequisitionType.Glas);
        if (activeType == WarehouseRequisitionType.Glas && !canOrderGlas) activeType = WarehouseRequisitionType.Lager;
        if (activeType == WarehouseRequisitionType.Lager && !canOrderLager && canOrderGlas) activeType = WarehouseRequisitionType.Glas;

        var userId = _user.GetCurrentAppUserId() ?? 0;
        var displayName = _user.GetDisplayName();
        var all = await _repo.GetForUserAsync(userId);
        // Stabiler Filter via CreatedByUserId; Fallback auf CreatedBy fuer Altdaten ohne UserId.
        var ownOnly = all.Where(r => r.CreatedByUserId == userId
            || (r.CreatedByUserId == null && r.CreatedBy == displayName))
            .Where(r => r.Type == activeType).ToList();

        // Server-Side-Spaltenfilter: ALLE Rows -> ViewModel -> filtern -> zaehlen -> paginieren.
        // (Filter muss ueber alle Eintraege wirken, nicht nur die aktuelle Seite.)
        var allItems = ownOnly.Select(r => new WarehouseRequisitionListItemViewModel(
            r.Id,
            r.ProductionWorkplace?.Name ?? "",
            r.CreatedBy,
            r.CreatedAt,
            r.SubmittedAt,
            r.Items.Count,
            r.Status,
            r.Comment)).ToList();

        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var filtered = ColumnFilterHelper.Apply(allItems, columnFilters, ColumnMap).ToList();
        var totalCount = filtered.Count;
        var paged = filtered.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList();

        var (missingItemCount, missingReqCount, missingNoRestockItemCount, missingNoRestockReqCount) =
            await _repo.GetShortageCountsForUserAsync(userId);

        var vm = new WarehouseRequisitionListViewModel
        {
            Items = paged,
            AvailableWorkplaces = await _workplaces.GetByUserIdAsync(userId),
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = totalCount
            },
            MissingPartsWaitingItemCount = missingItemCount,
            MissingPartsWaitingRequisitionCount = missingReqCount,
            MissingPartsNoRestockItemCount = missingNoRestockItemCount,
            MissingPartsNoRestockRequisitionCount = missingNoRestockReqCount,
            ActiveType = activeType,
            CanOrderLager = canOrderLager,
            CanOrderGlas = canOrderGlas,
        };
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDraft(int? workplaceId, WarehouseRequisitionType type = WarehouseRequisitionType.Lager)
    {
        if (!Enum.IsDefined(type))
        {
            TempData["WarningMessage"] = "Ungueltiger Bestelltyp.";
            return RedirectToAction(nameof(Index));
        }

        var allowed = type == WarehouseRequisitionType.Glas
            ? await _user.CanOrderGlasAsync()
            : await _user.CanOrderLagerAsync();
        if (!allowed)
        {
            TempData["WarningMessage"] = "Keine Berechtigung fuer diesen Bestelltyp.";
            return RedirectToAction(nameof(Index));
        }

        var userId = _user.GetCurrentAppUserId() ?? 0;
        var workplaces = await _workplaces.GetByUserIdAsync(userId);

        if (workplaces.Count == 0)
        {
            TempData["WarningMessage"] = "Bitte Werkbank-Zuordnung in Stammdaten pflegen.";
            return RedirectToAction(nameof(Index), new { type });
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
            return RedirectToAction(nameof(Index), new { type });
        }

        var newId = await _repo.CreateDraftAsync(chosenWp, type, userId, _user.GetDisplayName(), _user.GetWindowsUserName());
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
            Type = r.Type,
            CreatedAt = r.CreatedAt,
            Comment = r.Comment,
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
        var userId = _user.GetCurrentAppUserId() ?? 0;
        var displayName = _user.GetDisplayName();
        // Wenn CreatedByUserId gesetzt ist, primaer per Id pruefen; sonst Fallback auf Display-Name.
        var ownsRequisition = r.CreatedByUserId != null
            ? r.CreatedByUserId == userId
            : r.CreatedBy == displayName;
        if (!ownsRequisition)
            return Forbid();
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
        var settingKey = r.Type == WarehouseRequisitionType.Glas
            ? AppSettingKeys.DefaultGlasbestellempfaengerId
            : AppSettingKeys.DefaultLagerbestellempfaengerId;
        var groupId = await _settings.GetIntValueAsync(settingKey, 0);
        if (groupId <= 0)
        {
            TempData["WarningMessage"] = r.Type == WarehouseRequisitionType.Glas
                ? "Default-Glasbestellempfaenger nicht konfiguriert (Einstellungen)."
                : "Default-Lagerbestellempfaenger nicht konfiguriert (Einstellungen).";
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
            await _repo.SubmitAsync(id, groupId, userId,
                displayName, _user.GetWindowsUserName(), r.RowVersion);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            TempData["WarningMessage"] = "Bestellung wurde inzwischen geaendert — bitte Liste neu laden.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        TempData["SuccessMessage"] = $"Liste #{id} abgeschickt — wird per E-Mail gesendet (max. 15 Min).";
        return RedirectToAction(nameof(Index), new { type = r.Type });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason)
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
        if (r.Status != WarehouseRequisitionStatus.Draft && r.Status != WarehouseRequisitionStatus.Submitted)
        {
            TempData["WarningMessage"] = "Liste kann in diesem Status nicht storniert werden.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        try
        {
            await _repo.CancelAsync(id, reason, userId,
                displayName, _user.GetWindowsUserName(), r.RowVersion);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
        {
            TempData["WarningMessage"] = "Bestellung wurde inzwischen geaendert — bitte Liste neu laden.";
            return RedirectToAction(nameof(Edit), new { id });
        }
        TempData["SuccessMessage"] = $"Liste #{id} storniert.";
        return RedirectToAction(nameof(Index), new { type = r.Type });
    }
}
