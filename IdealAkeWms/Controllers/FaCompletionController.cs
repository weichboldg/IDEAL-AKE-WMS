using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;

namespace IdealAkeWms.Controllers;

/// <summary>
/// FA-Vervollstaendigung (seit v1.22.0 auf FaWorkSteps + Merkmale + Werkbank).
/// Pflegt pro Fertigungsauftrag die aktiven Arbeitsgaenge (<see cref="FaWorkStep"/>)
/// inklusive Auspraegungen (<see cref="FaWorkStepSpec"/>), Merkmalswerte
/// (<see cref="FaAttributeValue"/>) und die Werkbank-Zuweisung. Der AJAX-Toggle
/// <c>/api/fa-work-steps/toggle</c> bleibt fuer Leitstand + Edit-View;
/// IsSpecComplete ("vollstaendig definiert") hat eine eigene Action mit
/// Audit-Lifecycle. Arbeit-erledigt (IsCompleted) wird ausschliesslich in der
/// FA-Abarbeitungsliste gesetzt — NICHT hier.
/// </summary>
[RequireFaCompletionAccess]
public class FaCompletionController : Controller
{
    private readonly IProductionOrderRepository _productionOrderRepository;
    private readonly IFaWorkStepRepository _faWorkStepRepository;
    private readonly IWorkStepRepository _workStepRepository;
    private readonly IFaAttributeRepository _faAttributeRepository;
    private readonly IProductionWorkplaceRepository _productionWorkplaceRepository;
    private readonly IEnaioDmsDocumentRepository _enaioDmsDocumentRepository;
    private readonly ReadOnlyBomBuilder _readOnlyBomBuilder;
    private readonly ICurrentUserService _currentUser;

    public FaCompletionController(
        IProductionOrderRepository productionOrderRepository,
        IFaWorkStepRepository faWorkStepRepository,
        IWorkStepRepository workStepRepository,
        IFaAttributeRepository faAttributeRepository,
        IProductionWorkplaceRepository productionWorkplaceRepository,
        IEnaioDmsDocumentRepository enaioDmsDocumentRepository,
        ReadOnlyBomBuilder readOnlyBomBuilder,
        ICurrentUserService currentUser)
    {
        _productionOrderRepository = productionOrderRepository;
        _faWorkStepRepository = faWorkStepRepository;
        _workStepRepository = workStepRepository;
        _faAttributeRepository = faAttributeRepository;
        _productionWorkplaceRepository = productionWorkplaceRepository;
        _enaioDmsDocumentRepository = enaioDmsDocumentRepository;
        _readOnlyBomBuilder = readOnlyBomBuilder;
        _currentUser = currentUser;
    }

    // GET /FaCompletion
    public async Task<IActionResult> Index(
        string? filterOrderNumber,
        string? filterArticleNumber,
        string? filterCustomer,
        bool showDone = false,
        int page = 1,
        int? pageSize = null)
    {
        var userDefaultPageSize = await _currentUser.GetDefaultPageSizeAsync();
        var effectivePageSize = PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = PageSize.ResolveRaw(pageSize, userDefaultPageSize);

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
            // "Erledigt" = Sage-IsDone ODER App-Komm-erledigt (IsDonePicking) — konsistent zur FA-Liste.
            orders = orders
                .Where(o => !o.IsDone && !(o.PickingStatus != null && o.PickingStatus.IsDonePicking))
                .ToList();
        }

        var orderIds = orders.Select(o => o.Id).ToList();
        var counts = await _faWorkStepRepository.GetCountsByProductionOrderIdsAsync(orderIds);

        var allItems = orders.Select(o =>
        {
            var c = counts.GetValueOrDefault(o.Id);
            return new FaCompletionListItem
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Quantity = o.Quantity,
                Customer = o.Customer,
                ArticleNumber = o.ArticleNumber,
                Description1 = o.Description1,
                ProductionDate = o.ProductionDate,
                IsDone = o.IsDone,
                ApplicableCount = c?.ActiveCount ?? 0,
                CompletedCount = c?.SpecCompleteCount ?? 0,
                SpecCount = c?.SpecCount ?? 0,
                HasNoWorkplace = o.ProductionWorkplaceId == null,
                WorkplaceName = o.ProductionWorkplace?.Name,
            };
        }).ToList();

        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var filteredItems = ColumnFilterHelper.Apply(allItems, columnFilters, FaCompletionColumnMap).ToList();

        var totalCount = filteredItems.Count;
        var pagedItems = filteredItems
            .Skip((page - 1) * effectivePageSize)
            .Take(effectivePageSize)
            .ToList();

        // enaio DMS-Links fuer die einheitlichen FA-Vorbau-Buttons (Bulk-Lookup, nur Seite).
        var orderNumbers = pagedItems.Select(i => i.OrderNumber).Distinct().ToList();
        var dmsLinks = await _enaioDmsDocumentRepository.GetByOrderNumbersAsync(orderNumbers);

        var vm = new FaCompletionListViewModel
        {
            Items = pagedItems,
            FilterOrderNumber = filterOrderNumber,
            FilterArticleNumber = filterArticleNumber,
            FilterCustomer = filterCustomer,
            ShowDone = showDone,
            EnaioDmsLinks = dmsLinks,
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = totalCount,
            },
        };

        return View(vm);
    }

    private static readonly Dictionary<string, Func<FaCompletionListItem, string?>> FaCompletionColumnMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["order-number"] = i => i.OrderNumber,
            ["quantity"] = i => i.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["customer"] = i => i.Customer,
            ["article-number"] = i => i.ArticleNumber,
            ["description1"] = i => i.Description1,
            ["production-date"] = i => i.ProductionDate.HasValue
                ? $"{i.ProductionDate.Value:dd.MM.yyyy} KW{System.Globalization.ISOWeek.GetWeekOfYear(i.ProductionDate.Value)}"
                : string.Empty,
            ["workbench"] = i => i.HasNoWorkplace ? "Keine Werkbank" : i.WorkplaceName,
            ["applicable"] = i => i.ApplicableCount.ToString(),
            ["completed"] = i => i.CompletedCount.ToString(),
            ["spec-count"] = i => i.SpecCount.ToString(),
        };

    // GET /FaCompletion/Edit/{id}
    public async Task<IActionResult> Edit(int id, string? tab = null)
    {
        var order = await _productionOrderRepository.GetByIdAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        // Aktive FA-Arbeitsgaenge (inkl. WorkStep + Specs), SortOrder-sortiert
        var faWorkSteps = await _faWorkStepRepository.GetByProductionOrderIdAsync(id);

        // Katalog fuer "AG hinzufuegen": aktive WorkSteps, die am FA noch nicht aktiv sind
        var allActiveWorkSteps = await _workStepRepository.GetActiveAsync();
        var activeWorkStepIds = faWorkSteps.Select(f => f.WorkStepId).ToHashSet();
        var availableWorkSteps = allActiveWorkSteps
            .Where(w => !activeWorkStepIds.Contains(w.Id))
            .ToList();

        // Merkmale der aktiven Arbeitsgaenge + aktuelle Werte des FA
        var attributeDefs = faWorkSteps.Count == 0
            ? new List<FaAttributeDefinition>()
            : await _faAttributeRepository.GetActiveForWorkStepsAsync(
                faWorkSteps.Select(f => f.WorkStepId).ToList());
        var attributeValues = await _faAttributeRepository.GetValuesByProductionOrderIdAsync(id);
        var valuesByDefinition = attributeValues.ToDictionary(v => v.FaAttributeDefinitionId);

        var tabs = faWorkSteps.Select(f => new FaWorkStepTabViewModel
        {
            FaWorkStepId = f.Id,
            WorkStepId = f.WorkStepId,
            Code = f.WorkStep.Code,
            Name = f.WorkStep.Name,
            IsSpecComplete = f.IsSpecComplete,
            SpecCompletedAt = f.SpecCompletedAt,
            SpecCompletedBy = f.SpecCompletedBy,
            Attributes = attributeDefs
                .Where(d => d.WorkSteps.Any(j => j.WorkStepId == f.WorkStepId))
                .Select(d =>
                {
                    var value = valuesByDefinition.GetValueOrDefault(d.Id);
                    return new FaAttributeFieldViewModel
                    {
                        DefinitionId = d.Id,
                        Name = d.Name,
                        AttributeType = d.AttributeType,
                        Options = d.Options
                            .OrderBy(o => o.SortOrder).ThenBy(o => o.Value)
                            .Select(o => new FaAttributeOptionViewModel
                            {
                                Id = o.Id,
                                Value = o.Value,
                                IsActive = o.IsActive,
                            }).ToList(),
                        SelectedOptionId = value?.SelectedOptionId,
                        BooleanValue = value?.BooleanValue,
                        TextValue = value?.TextValue,
                    };
                }).ToList(),
            Specs = f.Specs
                .OrderBy(s => s.SortOrder).ThenBy(s => s.Id)
                .Select(s => new FaWorkStepSpecFormModel
                {
                    Id = s.Id,
                    FaWorkStepId = s.FaWorkStepId,
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
        }).ToList();

        var activeTab = !string.IsNullOrWhiteSpace(tab) && tabs.Any(t => t.Code == tab)
            ? tab!
            : tabs.FirstOrDefault()?.Code ?? string.Empty;

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
            ProductionWorkplaceId = order.ProductionWorkplaceId,
            AvailableWorkplaces = await _productionWorkplaceRepository.GetAllOrderedAsync(),
            AvailableWorkSteps = availableWorkSteps,
            ActiveTab = activeTab,
            Tabs = tabs,
            EnaioDmsLinks = await _enaioDmsDocumentRepository.GetByOrderNumbersAsync(
                new List<string> { order.OrderNumber }),
        };

        return View(vm);
    }

    // GET /FaCompletion/Bom/{id} — read-only Stueckliste fuer die FA-Vervollstaendigung.
    // Gemeinsamer ReadOnlyBomBuilder (DRY mit FaWorklist.Bom); Modul-Gate FaCompletionAktiv
    // greift bereits ueber den Class-Level-Filter [RequireFaCompletionAccess].
    public async Task<IActionResult> Bom(int id, string? filterText)
    {
        var vm = await _readOnlyBomBuilder.BuildAsync(id, filterText, _currentUser.GetCurrentAppUserId());
        if (vm == null)
        {
            // FA fehlt oder hat keine Artikelnummer -> zurueck zur Edit-Ansicht mit Hinweis.
            var order = await _productionOrderRepository.GetByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            TempData["WarningMessage"] = "Dieser Fertigungsauftrag hat keine Artikelnummer.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        // Zurueck-Link auf die FA-Vervollstaendigung statt Default (FaWorklist),
        // damit fa_completion-User nicht auf die fuer sie gesperrte Abarbeitungsliste landen.
        vm.BackController = "FaCompletion";
        vm.BackText = "Zurück zur FA-Vervollständigung";

        return View("~/Views/Picking/Bom.cshtml", vm);
    }

    // POST /FaCompletion/SetWorkplace
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetWorkplace(int id, int? workplaceId)
    {
        var order = await _productionOrderRepository.GetByIdAsync(id);
        if (order == null)
        {
            return NotFound();
        }

        order.ProductionWorkplaceId = workplaceId;
        order.ModifiedAt = DateTime.Now;
        order.ModifiedBy = _currentUser.GetDisplayName();
        order.ModifiedByWindows = _currentUser.GetWindowsUserName();

        await _productionOrderRepository.UpdateAsync(order);

        TempData["SuccessMessage"] = workplaceId.HasValue
            ? "Werkbank zugewiesen."
            : "Werkbank-Zuweisung entfernt.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    // POST /FaCompletion/SaveAttributeValue
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveAttributeValue(int id, int definitionId, int? optionId, bool? boolValue, string? textValue = null, string? tab = null)
    {
        await _faAttributeRepository.UpsertValueAsync(
            id,
            definitionId,
            optionId,
            boolValue,
            textValue,
            _currentUser.GetDisplayName(),
            _currentUser.GetWindowsUserName());

        return RedirectToAction(nameof(Edit), new { id, tab });
    }

    // POST /FaCompletion/AddWorkStep
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddWorkStep(int id, int workStepId)
    {
        var step = await _workStepRepository.GetByIdAsync(workStepId);
        if (step == null)
        {
            return NotFound();
        }

        await _faWorkStepRepository.SetActiveAsync(
            id, workStepId, active: true,
            _currentUser.GetDisplayName(), _currentUser.GetWindowsUserName());

        TempData["SuccessMessage"] = $"Arbeitsgang {step.Code} hinzugefuegt.";
        return RedirectToAction(nameof(Edit), new { id, tab = step.Code });
    }

    // POST /FaCompletion/RemoveWorkStep
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveWorkStep(int id, int workStepId)
    {
        var step = await _workStepRepository.GetByIdAsync(workStepId);
        if (step == null)
        {
            return NotFound();
        }

        await _faWorkStepRepository.SetActiveAsync(
            id, workStepId, active: false,
            _currentUser.GetDisplayName(), _currentUser.GetWindowsUserName());

        TempData["SuccessMessage"] = $"Arbeitsgang {step.Code} entfernt.";
        return RedirectToAction(nameof(Edit), new { id });
    }

    // POST /FaCompletion/AddSpec
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSpec(FaWorkStepSpecFormModel form)
    {
        var row = await _faWorkStepRepository.GetByIdAsync(form.FaWorkStepId);
        if (row == null)
        {
            return NotFound("FaWorkStep fehlt.");
        }

        if (string.IsNullOrWhiteSpace(form.Description))
        {
            TempData["WarningMessage"] = "Beschreibung ist erforderlich.";
            return RedirectToAction(nameof(Edit), new { id = row.ProductionOrderId, tab = row.WorkStep.Code });
        }

        var spec = new FaWorkStepSpec
        {
            FaWorkStepId = form.FaWorkStepId,
            ArticleId = form.ArticleId,
            Description = form.Description.Trim(),
            Quantity = form.Quantity,
            Notes = form.Notes,
            SortOrder = form.SortOrder,
            CreatedAt = DateTime.Now,
            CreatedBy = _currentUser.GetDisplayName(),
            CreatedByWindows = _currentUser.GetWindowsUserName(),
        };

        await _faWorkStepRepository.AddSpecAsync(spec);

        TempData["SuccessMessage"] = "Auspraegung hinzugefuegt.";
        return RedirectToAction(nameof(Edit), new { id = row.ProductionOrderId, tab = row.WorkStep.Code });
    }

    // POST /FaCompletion/EditSpec
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSpec(FaWorkStepSpecFormModel form)
    {
        var existing = await _faWorkStepRepository.GetSpecByIdAsync(form.Id);
        if (existing == null)
        {
            return NotFound();
        }

        var row = await _faWorkStepRepository.GetByIdAsync(existing.FaWorkStepId);
        if (row == null)
        {
            return NotFound("FaWorkStep fehlt.");
        }

        if (string.IsNullOrWhiteSpace(form.Description))
        {
            TempData["WarningMessage"] = "Beschreibung ist erforderlich.";
            return RedirectToAction(nameof(Edit), new { id = row.ProductionOrderId, tab = row.WorkStep.Code });
        }

        existing.ArticleId = form.ArticleId;
        existing.Description = form.Description.Trim();
        existing.Quantity = form.Quantity;
        existing.Notes = form.Notes;
        existing.SortOrder = form.SortOrder;
        existing.ModifiedAt = DateTime.Now;
        existing.ModifiedBy = _currentUser.GetDisplayName();
        existing.ModifiedByWindows = _currentUser.GetWindowsUserName();

        await _faWorkStepRepository.UpdateSpecAsync(existing);

        TempData["SuccessMessage"] = "Auspraegung aktualisiert.";
        return RedirectToAction(nameof(Edit), new { id = row.ProductionOrderId, tab = row.WorkStep.Code });
    }

    // POST /FaCompletion/DeleteSpec
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSpec(int id)
    {
        var existing = await _faWorkStepRepository.GetSpecByIdAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        var row = await _faWorkStepRepository.GetByIdAsync(existing.FaWorkStepId);

        await _faWorkStepRepository.DeleteSpecAsync(id);

        TempData["SuccessMessage"] = "Auspraegung geloescht.";
        return RedirectToAction(nameof(Edit), new
        {
            id = row?.ProductionOrderId ?? existing.FaWorkStep.ProductionOrderId,
            tab = row?.WorkStep.Code,
        });
    }

    // POST /FaCompletion/ToggleSpecComplete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSpecComplete(int faWorkStepId)
    {
        var row = await _faWorkStepRepository.GetByIdAsync(faWorkStepId);
        if (row == null)
        {
            return NotFound();
        }

        var newValue = !row.IsSpecComplete;

        await _faWorkStepRepository.SetIsSpecCompleteAsync(
            faWorkStepId,
            newValue,
            _currentUser.GetDisplayName(),
            _currentUser.GetWindowsUserName());

        TempData["SuccessMessage"] = newValue
            ? "Arbeitsgang als vollstaendig definiert markiert."
            : "Definition zurueckgesetzt.";

        return RedirectToAction(nameof(Edit), new { id = row.ProductionOrderId, tab = row.WorkStep.Code });
    }
}
