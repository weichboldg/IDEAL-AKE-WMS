using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

/// <summary>
/// Phase 4 — FA-Vervollstaendigung. Pflegt pro Fertigungsauftrag die 5 Baugruppen
/// VK/VL/VE/VT/VA (siehe <see cref="ProductionOrderAssemblyGroup"/>) inklusive Auspraegungen
/// (<see cref="ProductionOrderAssemblyGroupSpec"/>). IsApplicable wird ueber den bestehenden
/// JSON-Endpoint <c>/api/assembly-groups/toggle-applicable</c> aus der Edit-View getoggelt;
/// IsCompleted hat eine eigene Action mit Audit-Lifecycle.
/// </summary>
[RequireFaCompletionAccess]
public class FaCompletionController : Controller
{
    private static readonly Dictionary<string, string> GroupKeyNames = new()
    {
        ["VK"] = "Kuehlung",
        ["VL"] = "Lueftung",
        ["VE"] = "Elektro",
        ["VT"] = "Tueren",
        ["VA"] = "Aufbau",
    };

    private static readonly string[] GroupKeysOrdered = { "VK", "VL", "VE", "VT", "VA" };

    private readonly IProductionOrderRepository _productionOrderRepository;
    private readonly IProductionOrderAssemblyGroupRepository _assemblyGroupRepository;
    private readonly IProductionOrderAssemblyGroupSpecRepository _specRepository;
    private readonly ICurrentUserService _currentUser;

    public FaCompletionController(
        IProductionOrderRepository productionOrderRepository,
        IProductionOrderAssemblyGroupRepository assemblyGroupRepository,
        IProductionOrderAssemblyGroupSpecRepository specRepository,
        ICurrentUserService currentUser)
    {
        _productionOrderRepository = productionOrderRepository;
        _assemblyGroupRepository = assemblyGroupRepository;
        _specRepository = specRepository;
        _currentUser = currentUser;
    }

    // GET /FaCompletion
    public async Task<IActionResult> Index(
        string? filterOrderNumber,
        string? filterArticleNumber,
        string? filterCustomer,
        bool showDone = false)
    {
        var orders = await _productionOrderRepository.GetAllOrderedAsync();

        if (!string.IsNullOrWhiteSpace(filterOrderNumber))
        {
            orders = orders
                .Where(o => o.OrderNumber.Contains(filterOrderNumber, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(filterArticleNumber))
        {
            orders = orders
                .Where(o => o.ArticleNumber != null
                            && o.ArticleNumber.Contains(filterArticleNumber, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(filterCustomer))
        {
            orders = orders
                .Where(o => o.Customer != null
                            && o.Customer.Contains(filterCustomer, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (!showDone)
        {
            orders = orders.Where(o => !o.IsDone).ToList();
        }

        var orderIds = orders.Select(o => o.Id).ToList();
        var pivot = await _assemblyGroupRepository.GetIsApplicablePivotAsync(orderIds);
        var groupRows = orderIds.Count == 0
            ? new List<ProductionOrderAssemblyGroup>()
            : await _assemblyGroupRepository.GetByProductionOrderIdsAsync(orderIds);
        var groupIds = groupRows.Select(g => g.Id).ToList();
        var specsByGroup = await _specRepository.GetByAssemblyGroupIdsAsync(groupIds);

        var completedByOrder = groupRows
            .GroupBy(g => g.ProductionOrderId)
            .ToDictionary(g => g.Key, g => g.Count(x => x.IsCompleted));

        var specCountByOrder = groupRows
            .GroupBy(g => g.ProductionOrderId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(x => specsByGroup.TryGetValue(x.Id, out var list) ? list.Count : 0));

        var items = orders.Select(o => new FaCompletionListItem
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            Quantity = o.Quantity,
            Customer = o.Customer,
            ArticleNumber = o.ArticleNumber,
            Description1 = o.Description1,
            ProductionDate = o.ProductionDate,
            IsDone = o.IsDone,
            ApplicableCount = pivot.TryGetValue(o.Id, out var pivotRow)
                ? pivotRow.Count(kv => kv.Value)
                : 0,
            CompletedCount = completedByOrder.GetValueOrDefault(o.Id),
            SpecCount = specCountByOrder.GetValueOrDefault(o.Id),
        }).ToList();

        var vm = new FaCompletionListViewModel
        {
            Items = items,
            FilterOrderNumber = filterOrderNumber,
            FilterArticleNumber = filterArticleNumber,
            FilterCustomer = filterCustomer,
            ShowDone = showDone,
        };

        return View(vm);
    }

    // GET /FaCompletion/Edit/{id}
    public async Task<IActionResult> Edit(int id, string? tab = null)
    {
        var order = await _productionOrderRepository.GetByIdAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        var groups = await _assemblyGroupRepository.GetByProductionOrderIdAsync(id);
        if (groups.Count == 0)
        {
            return NotFound("AssemblyGroups fehlen (sollte durch Phase 1 eager-created sein).");
        }

        var specsByGroup = await _specRepository.GetByAssemblyGroupIdsAsync(groups.Select(g => g.Id));

        var activeTab = !string.IsNullOrWhiteSpace(tab) && GroupKeyNames.ContainsKey(tab) ? tab! : "VK";

        var tabs = GroupKeysOrdered.Select(key =>
        {
            var grp = groups.FirstOrDefault(g => g.GroupKey == key)
                      ?? throw new InvalidOperationException(
                          $"AssemblyGroup {key} fehlt fuer FA {id} (Phase-1-Eager-Create haette sie anlegen muessen).");
            var specs = specsByGroup.GetValueOrDefault(grp.Id) ?? new List<ProductionOrderAssemblyGroupSpec>();

            return new AssemblyGroupTabViewModel
            {
                AssemblyGroupId = grp.Id,
                GroupKey = grp.GroupKey,
                GroupName = GroupKeyNames[grp.GroupKey],
                IsApplicable = grp.IsApplicable,
                IsCompleted = grp.IsCompleted,
                CompletedAt = grp.CompletedAt,
                CompletedBy = grp.CompletedBy,
                Specs = specs.Select(s => new AssemblyGroupSpecFormModel
                {
                    Id = s.Id,
                    AssemblyGroupId = s.AssemblyGroupId,
                    ArticleId = s.ArticleId,
                    ArticleText = s.Article != null
                        ? s.Article.ArticleNumber
                          + (s.Article.Description != null ? " - " + s.Article.Description : string.Empty)
                        : null,
                    Description = s.Description,
                    Quantity = s.Quantity,
                    Notes = s.Notes,
                    SortOrder = s.SortOrder,
                }).ToList(),
            };
        }).ToList();

        var vm = new FaCompletionEditViewModel
        {
            ProductionOrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Quantity = order.Quantity,
            Customer = order.Customer,
            ArticleNumber = order.ArticleNumber,
            Description1 = order.Description1,
            Description2 = order.Description2,
            ProductionDate = order.ProductionDate,
            DeliveryDate = order.DeliveryDate,
            IsDone = order.IsDone,
            ActiveTab = activeTab,
            Tabs = tabs,
        };

        return View(vm);
    }

    // POST /FaCompletion/AddSpec
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSpec(AssemblyGroupSpecFormModel form)
    {
        var grp = await _assemblyGroupRepository.GetByIdAsync(form.AssemblyGroupId);
        if (grp == null)
        {
            return NotFound("AssemblyGroup fehlt.");
        }

        if (string.IsNullOrWhiteSpace(form.Description))
        {
            TempData["WarningMessage"] = "Beschreibung ist erforderlich.";
            return RedirectToAction(nameof(Edit), new { id = grp.ProductionOrderId, tab = grp.GroupKey });
        }

        var spec = new ProductionOrderAssemblyGroupSpec
        {
            AssemblyGroupId = form.AssemblyGroupId,
            ArticleId = form.ArticleId,
            Description = form.Description.Trim(),
            Quantity = form.Quantity,
            Notes = form.Notes,
            SortOrder = form.SortOrder,
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUser.GetDisplayName(),
            CreatedByWindows = _currentUser.GetWindowsUserName(),
        };

        await _specRepository.AddAsync(spec);

        TempData["SuccessMessage"] = "Auspraegung hinzugefuegt.";
        return RedirectToAction(nameof(Edit), new { id = grp.ProductionOrderId, tab = grp.GroupKey });
    }

    // POST /FaCompletion/EditSpec
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSpec(AssemblyGroupSpecFormModel form)
    {
        var existing = await _specRepository.GetByIdAsync(form.Id);
        if (existing == null)
        {
            return NotFound();
        }

        var grp = existing.AssemblyGroup;

        if (string.IsNullOrWhiteSpace(form.Description))
        {
            TempData["WarningMessage"] = "Beschreibung ist erforderlich.";
            return RedirectToAction(nameof(Edit), new { id = grp.ProductionOrderId, tab = grp.GroupKey });
        }

        existing.ArticleId = form.ArticleId;
        existing.Description = form.Description.Trim();
        existing.Quantity = form.Quantity;
        existing.Notes = form.Notes;
        existing.SortOrder = form.SortOrder;
        existing.ModifiedAt = DateTime.Now;
        existing.ModifiedBy = _currentUser.GetDisplayName();
        existing.ModifiedByWindows = _currentUser.GetWindowsUserName();

        await _specRepository.UpdateAsync(existing);

        TempData["SuccessMessage"] = "Auspraegung aktualisiert.";
        return RedirectToAction(nameof(Edit), new { id = grp.ProductionOrderId, tab = grp.GroupKey });
    }

    // POST /FaCompletion/DeleteSpec
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSpec(int id)
    {
        var existing = await _specRepository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        var grp = existing.AssemblyGroup;

        await _specRepository.DeleteAsync(id);

        TempData["SuccessMessage"] = "Auspraegung geloescht.";
        return RedirectToAction(nameof(Edit), new { id = grp.ProductionOrderId, tab = grp.GroupKey });
    }

    // POST /FaCompletion/ToggleIsCompleted
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleIsCompleted(int assemblyGroupId)
    {
        var grp = await _assemblyGroupRepository.GetByIdAsync(assemblyGroupId);
        if (grp == null)
        {
            return NotFound();
        }

        var newValue = !grp.IsCompleted;
        var displayName = _currentUser.GetDisplayName();
        var windowsName = _currentUser.GetWindowsUserName();

        await _assemblyGroupRepository.SetIsCompletedAsync(
            assemblyGroupId,
            newValue,
            displayName,
            displayName,
            windowsName);

        TempData["SuccessMessage"] = newValue
            ? "Baugruppe als vervollstaendigt markiert."
            : "Vervollstaendigung zurueckgesetzt.";

        return RedirectToAction(nameof(Edit), new { id = grp.ProductionOrderId, tab = grp.GroupKey });
    }
}
