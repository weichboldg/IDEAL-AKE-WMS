using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using PageSize = IdealAkeWms.Services.PageSize;

namespace IdealAkeWms.Controllers;

/// <summary>
/// IDEAL Teil 2 — rekursive FA-Struktur-/Baumanzeige (read-only) auf den in Teil 1 importierten
/// <see cref="FaHierarchyNode"/>/<see cref="FaHierarchyOrderInfo"/>-Tabellen.
/// Beruehrt <b>weder</b> <c>ProductionOrders</c> <b>noch</b> den Master
/// <c>ProduktionsauftragHierarchisch</c> — unabhaengig von Teil 7, vor Teil 7 lieferbar.
/// Zugriff: derselbe Class-Level-Read-Filter wie <see cref="ProductionOrdersController"/>
/// (ADR 0006, B-2). Reine Read-View ⇒ kein Edit-Split.
/// </summary>
[RequirePickingOrTrackingOrLeitstandAccess]
public class FaHierarchyController : Controller
{
    private readonly IFaHierarchyNodeRepository _nodeRepository;
    private readonly IFaHierarchyOrderInfoRepository _orderInfoRepository;
    private readonly FaHierarchyTreeBuilder _treeBuilder;
    private readonly IAppSettingRepository _settingRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUserRepository _userRepository;
    private readonly IWorkOperationRepository _workOperationRepository;

    public FaHierarchyController(
        IFaHierarchyNodeRepository nodeRepository,
        IFaHierarchyOrderInfoRepository orderInfoRepository,
        FaHierarchyTreeBuilder treeBuilder,
        IAppSettingRepository settingRepository,
        ICurrentUserService currentUserService,
        IUserRepository userRepository,
        IWorkOperationRepository workOperationRepository)
    {
        _nodeRepository = nodeRepository;
        _orderInfoRepository = orderInfoRepository;
        _treeBuilder = treeBuilder;
        _settingRepository = settingRepository;
        _currentUserService = currentUserService;
        _userRepository = userRepository;
        _workOperationRepository = workOperationRepository;
    }

    public async Task<IActionResult> Index(
        string? filterHauptFa,
        string? filterCustomer,
        string? filterText,
        int page = 1,
        int? pageSize = null)
    {
        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = PageSize.Resolve(pageSize, userDefaultPageSize);
        var rawPageSize = PageSize.ResolveRaw(pageSize, userDefaultPageSize);

        // Q5: Tiefen-Cap aus AppSettings (ADR 0011), Default 500 ohne DB-Zeile (kein Seed).
        var depthCap = await _settingRepository.GetIntValueAsync(
            AppSettingKeys.FaHierarchyMaxDepth, FaHierarchyTreeBuilder.DefaultDepthCap);

        var allNodes = await _nodeRepository.GetAllAsync();
        var allOrderInfos = await _orderInfoRepository.GetAllAsync();

        // Server-seitiger Filter auf STRUKTUR-Ebene (Kopf ODER irgendein Knoten der Gruppe).
        // Entscheidet WELCHE Strukturen erscheinen — nie, welche Knoten innerhalb einer Struktur
        // sichtbar sind (das wuerde Kinder verwaisen; Baum-Integritaet, ADR-0005-Baum-Ausnahme).
        var qualifyingHauptFas = FilterHauptFas(allNodes, allOrderInfos, filterHauptFa, filterCustomer, filterText);
        var filteredNodes = qualifyingHauptFas == null
            ? allNodes
            : allNodes.Where(n => qualifyingHauptFas.Contains(n.HauptFA)).ToList();
        var filteredOrderInfos = qualifyingHauptFas == null
            ? allOrderInfos
            : allOrderInfos.Where(o => qualifyingHauptFas.Contains(o.HauptFA)).ToList();

        var allStructures = _treeBuilder.Build(filteredNodes, filteredOrderInfos, depthCap);

        // Seiteneinheit = Struktur: TotalCount zaehlt Strukturen (Wurzeln + Waisen-Pseudowurzeln);
        // eine Struktur wird nie ueber Seiten getrennt.
        var totalCount = allStructures.Count;
        var pageStructures = allStructures
            .Skip((page - 1) * effectivePageSize)
            .Take(effectivePageSize)
            .ToList();

        // AG-Knopf: EIN schlanker Existenz-Check fuer die sichtbaren Strukturen (nur Schluessel, keine Details).
        var withOperations = await _workOperationRepository.GetOrderNumbersWithWorkOperationsAsync(
            pageStructures.Select(s => s.HauptFA.ToString()).Distinct().ToList());
        foreach (var s in pageStructures)
            s.HasWorkOperations = withOperations.Contains(s.HauptFA.ToString());

        // Sichtbares UI-Signal (zusaetzlich zur ILogger-Warnung im Builder) — keine Mail, kein SyncLog.
        if (pageStructures.Any(s => s.HasError))
        {
            TempData["WarningMessage"] =
                "Mindestens eine Struktur konnte wegen eines Tiefen-Cap- oder Zyklenfehlers nicht "
                + "vollstaendig aufgebaut werden (siehe Markierung an der Struktur). Details im Serverprotokoll.";
        }

        // Q4: Auto-Expand gekoppelt an das bestehende User-Setting RecursiveFilterSearch (wie BOM-Tree).
        var recursiveFilterSearch = false;
        var currentUserId = _currentUserService.GetCurrentAppUserId();
        if (currentUserId.HasValue)
        {
            var currentUser = await _userRepository.GetByIdAsync(currentUserId.Value);
            if (currentUser != null)
                recursiveFilterSearch = currentUser.RecursiveFilterSearch;
        }

        var vm = new FaHierarchyTreeViewModel
        {
            Structures = pageStructures,
            FilterHauptFa = filterHauptFa,
            FilterCustomer = filterCustomer,
            FilterText = filterText,
            RecursiveFilterSearch = recursiveFilterSearch,
            DepthCap = depthCap,
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = rawPageSize,
                TotalCount = totalCount
            }
        };

        return View(vm);
    }

    /// <summary>
    /// Modal-Inhalt "Arbeitsgaenge" EINER Struktur (per fetch beim Oeffnen). Laedt nur die Knoten dieser
    /// HauptFA (nie GetAllAsync) und die Arbeitsgaenge dieser HauptFA. Erbt den Class-Level-Read-Filter.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> WorkOperations(int hauptFa)
    {
        var nodes = await _nodeRepository.GetByHauptFaAsync(hauptFa);
        if (nodes.Count == 0)
            return NotFound();

        var depthCap = await _settingRepository.GetIntValueAsync(
            AppSettingKeys.FaHierarchyMaxDepth, FaHierarchyTreeBuilder.DefaultDepthCap);
        var structures = _treeBuilder.Build(nodes, Array.Empty<FaHierarchyOrderInfo>(), depthCap);

        // Gleicher Schluessel wie der Existenz-Check im Index (OrderNumber == HauptFA): Knopf und Modal
        // sehen dieselben Arbeitsgaenge; was keinem Knoten zuzuordnen ist, meldet das Modal separat.
        var operations = await _workOperationRepository.GetByOrderNumberWithWorkplaceAsync(hauptFa.ToString());

        return PartialView("_FaWorkOperationsModal", FaWorkOperationsViewModel.Create(hauptFa, structures, operations));
    }

    /// <summary>
    /// Ermittelt die HauptFA-Werte, deren Struktur der Filterkarte genuegt (Kopf ODER Knoten passt).
    /// Gibt <c>null</c> zurueck, wenn kein Filter gesetzt ist (alle Strukturen qualifizieren).
    /// Semantik zwischen den Feldern: UND (analog <see cref="ProductionOrdersController"/>).
    /// </summary>
    private static HashSet<int>? FilterHauptFas(
        List<FaHierarchyNode> nodes,
        List<FaHierarchyOrderInfo> orderInfos,
        string? filterHauptFa,
        string? filterCustomer,
        string? filterText)
    {
        var hasHauptFa = !string.IsNullOrWhiteSpace(filterHauptFa);
        var hasCustomer = !string.IsNullOrWhiteSpace(filterCustomer);
        var hasText = !string.IsNullOrWhiteSpace(filterText);
        if (!hasHauptFa && !hasCustomer && !hasText)
            return null;

        var hauptFaText = filterHauptFa?.Trim();
        var customerText = filterCustomer?.Trim();
        var freeText = filterText?.Trim();

        var infosByHauptFa = orderInfos.GroupBy(o => o.HauptFA).ToDictionary(g => g.Key, g => g.ToList());
        var qualifying = new HashSet<int>();

        foreach (var group in nodes.GroupBy(n => n.HauptFA))
        {
            var groupNodes = group.ToList();
            var infos = infosByHauptFa.TryGetValue(group.Key, out var oi) ? oi : new List<FaHierarchyOrderInfo>();

            var matchHauptFa = !hasHauptFa
                || group.Key.ToString().Contains(hauptFaText!, StringComparison.OrdinalIgnoreCase);
            var matchCustomer = !hasCustomer
                || infos.Any(i => ContainsCi(i.Kunde, customerText));
            var matchText = !hasText
                || MatchesFreeText(groupNodes, infos, freeText!);

            if (matchHauptFa && matchCustomer && matchText)
                qualifying.Add(group.Key);
        }

        return qualifying;
    }

    /// <summary>Freitext-Treffer auf Kopfdaten (Kunde/Status) ODER irgendeinen Knoten der Gruppe.</summary>
    private static bool MatchesFreeText(List<FaHierarchyNode> groupNodes, List<FaHierarchyOrderInfo> infos, string text)
    {
        var headMatch = infos.Any(i =>
            ContainsCi(i.Kunde, text) ||
            ContainsCi(i.Status, text) ||
            ContainsCi(i.ABNr, text));
        if (headMatch)
            return true;

        return groupNodes.Any(n =>
            ContainsCi(n.HauptArtnr, text) ||
            ContainsCi(n.Artnr, text) ||
            ContainsCi(n.Matchcode, text) ||
            ContainsCi(n.Bezeichnung1, text) ||
            ContainsCi(n.Bezeichnung2, text) ||
            ContainsCi(n.Artikelgruppe, text));
    }

    private static bool ContainsCi(string? haystack, string? needle) =>
        !string.IsNullOrEmpty(haystack) && !string.IsNullOrEmpty(needle)
        && haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
