using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

[RequireMasterDataReadAccess]
public class OrderRecipientsController : Controller
{
    private readonly IOrderRecipientRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public OrderRecipientsController(
        IOrderRecipientRepository repository,
        ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Server-Side-Spaltenfilter: Col-Key (data-col-key der View) -> gerenderter Zell-Text.
    /// Die Getter MUESSEN exakt das liefern, was die View in der Zelle rendert
    /// (Empfaenger-Spalte zeigt nur die Anzahl als Badge).
    /// </summary>
    private static readonly Dictionary<string, Func<OrderRecipientGroup, string?>> ColumnMap = new()
    {
        ["name"] = g => g.Name,
        ["description"] = g => g.Description,
        ["recipients"] = g => g.Recipients.Count.ToString(),
    };

    public async Task<IActionResult> Index(int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = Services.PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = Services.PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        var groups = await _repository.GetAllGroupsAsync();

        // Server-Side-Spaltenfilter: vor der Pagination —
        // Filter muss ueber ALLE Eintraege wirken, nicht nur die aktuelle Seite.
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var list = ColumnFilterHelper.Apply(groups, columnFilters, ColumnMap).ToList();
        ViewBag.Pagination = new Models.ViewModels.PaginationState
        {
            CurrentPage = page,
            PageSize = effectivePageSize,
            PageSizeRaw = rawPageSize,
            TotalCount = list.Count
        };
        return View(list.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList());
    }

    [RequireMasterDataAccess]
    public IActionResult Create()
    {
        var vm = new OrderRecipientGroupViewModel();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> Create(OrderRecipientGroupViewModel vm)
    {
        if (!ModelState.IsValid)
            return View(vm);

        var group = new OrderRecipientGroup
        {
            Name = vm.Name,
            Description = vm.Description,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName()
        };

        await _repository.AddGroupAsync(group);

        TempData["SuccessMessage"] = $"Empfängergruppe \"{group.Name}\" wurde erfolgreich angelegt. Sie können nun Empfänger hinzufügen.";
        return RedirectToAction(nameof(Edit), new { id = group.Id });
    }

    [RequireMasterDataAccess]
    public async Task<IActionResult> Edit(int id)
    {
        var group = await _repository.GetGroupByIdAsync(id);
        if (group == null)
            return NotFound();

        var vm = new OrderRecipientGroupViewModel
        {
            Id = group.Id,
            Name = group.Name,
            Description = group.Description,
            Recipients = group.Recipients.Select(r => new OrderRecipientEditModel
            {
                Id = r.Id,
                Name = r.Name,
                Email = r.Email,
                IsActive = r.IsActive
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> Edit(int id, OrderRecipientGroupViewModel vm)
    {
        if (id != vm.Id)
            return NotFound();

        // Only validate group-level fields, not recipients
        ModelState.Remove("Recipients");
        if (!ModelState.IsValid)
        {
            // Reload recipients for the view
            var existingGroup = await _repository.GetGroupByIdAsync(id);
            if (existingGroup != null)
            {
                vm.Recipients = existingGroup.Recipients.Select(r => new OrderRecipientEditModel
                {
                    Id = r.Id,
                    Name = r.Name,
                    Email = r.Email,
                    IsActive = r.IsActive
                }).ToList();
            }
            return View(vm);
        }

        var group = await _repository.GetGroupByIdAsync(id);
        if (group == null)
            return NotFound();

        group.Name = vm.Name;
        group.Description = vm.Description;
        group.ModifiedAt = DateTime.UtcNow;
        group.ModifiedBy = _currentUserService.GetDisplayName();
        group.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _repository.UpdateGroupAsync(group);

        TempData["SuccessMessage"] = $"Empfängergruppe \"{group.Name}\" wurde erfolgreich gespeichert.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> Delete(int id)
    {
        var group = await _repository.GetGroupByIdAsync(id);
        if (group == null)
            return NotFound();

        if (await _repository.GroupHasOpenRequisitionsAsync(id))
        {
            TempData["WarningMessage"] = $"Die Empfängergruppe \"{group.Name}\" hat noch offene Bestellanforderungen und kann nicht gelöscht werden.";
            return RedirectToAction(nameof(Index));
        }

        var groupName = group.Name;
        var deleted = await _repository.DeleteGroupAsync(id);

        if (deleted)
            TempData["SuccessMessage"] = $"Empfängergruppe \"{groupName}\" wurde erfolgreich gelöscht.";
        else
            TempData["WarningMessage"] = $"Empfängergruppe \"{groupName}\" konnte nicht gelöscht werden.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> AddRecipient(int groupId, OrderRecipientEditModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["WarningMessage"] = "Bitte Name und gültige E-Mail-Adresse eingeben.";
            return RedirectToAction(nameof(Edit), new { id = groupId });
        }

        var group = await _repository.GetGroupByIdAsync(groupId);
        if (group == null)
            return NotFound();

        var recipient = new OrderRecipient
        {
            OrderRecipientGroupId = groupId,
            Name = model.Name,
            Email = model.Email,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.GetDisplayName(),
            CreatedByWindows = _currentUserService.GetWindowsUserName()
        };

        await _repository.AddRecipientAsync(recipient);

        TempData["SuccessMessage"] = $"Empfänger \"{recipient.Name}\" wurde hinzugefügt.";
        return RedirectToAction(nameof(Edit), new { id = groupId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> UpdateRecipient(int groupId, OrderRecipientEditModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["WarningMessage"] = "Bitte Name und gültige E-Mail-Adresse eingeben.";
            return RedirectToAction(nameof(Edit), new { id = groupId });
        }

        var recipient = await _repository.GetRecipientByIdAsync(model.Id);
        if (recipient == null)
            return NotFound();

        recipient.Name = model.Name;
        recipient.Email = model.Email;
        recipient.IsActive = model.IsActive;
        recipient.ModifiedAt = DateTime.UtcNow;
        recipient.ModifiedBy = _currentUserService.GetDisplayName();
        recipient.ModifiedByWindows = _currentUserService.GetWindowsUserName();

        await _repository.UpdateRecipientAsync(recipient);

        TempData["SuccessMessage"] = $"Empfänger \"{recipient.Name}\" wurde aktualisiert.";
        return RedirectToAction(nameof(Edit), new { id = groupId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> DeleteRecipient(int groupId, int recipientId)
    {
        var recipient = await _repository.GetRecipientByIdAsync(recipientId);
        if (recipient == null)
            return NotFound();

        var recipientName = recipient.Name;
        await _repository.DeleteRecipientAsync(recipientId);

        TempData["SuccessMessage"] = $"Empfänger \"{recipientName}\" wurde entfernt.";
        return RedirectToAction(nameof(Edit), new { id = groupId });
    }

    public async Task<IActionResult> ArticleGroupMappings()
    {
        var articleGroups = await _repository.GetDistinctArticleGroupsAsync();
        var allGroups = await _repository.GetAllGroupsAsync();
        var existingMappings = await _repository.GetMappingsAsync();

        var mappings = articleGroups.Select(ag => new ArticleGroupMappingViewModel
        {
            ArticleGroup = ag,
            SelectedGroupIds = existingMappings
                .Where(m => m.ArticleGroup == ag)
                .Select(m => m.OrderRecipientGroupId)
                .ToList()
        }).ToList();

        var vm = new ArticleGroupMappingsPageViewModel
        {
            Mappings = mappings,
            AvailableGroups = allGroups
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterDataAccess]
    public async Task<IActionResult> SaveArticleGroupMappings(ArticleGroupMappingsPageViewModel vm)
    {
        var displayName = _currentUserService.GetDisplayName();
        var windowsUser = _currentUserService.GetWindowsUserName();

        foreach (var mapping in vm.Mappings)
        {
            await _repository.SetMappingsForArticleGroupAsync(
                mapping.ArticleGroup,
                mapping.SelectedGroupIds ?? new List<int>(),
                displayName,
                windowsUser);
        }

        TempData["SuccessMessage"] = "Artikelgruppen-Zuordnungen wurden gespeichert.";
        return RedirectToAction(nameof(ArticleGroupMappings));
    }
}
