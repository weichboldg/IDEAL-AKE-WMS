using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using IdealAkeWms.Services.HierarchischeStruktur;
using IdealAkeWms.Services.SageSdata;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using PageSize = IdealAkeWms.Services.PageSize;

namespace IdealAkeWms.Controllers;

/// <summary>
/// v1.99.38 (Spec 2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen): „Arbeitsgänge → Sage“. E0 Vorschau je FA × Kuerzel aus der
/// FA-Struktur (Listen-View-Pattern ADR 0005), Sage-Stand nur auf Knopf „Sage-Stand prüfen“ (nur GET). Nur Admin (Freigabe-Antwort 10),
/// nur bei <c>ProduktionsauftragHierarchisch</c> (IDEAL).
/// </summary>
[RequireAdminAccess]
public class SageFaArbeitsgaengeController : Controller
{
    private readonly IFaHierarchyNodeRepository _nodes;
    private readonly IOperationConfigRepository _configs;
    private readonly IServiceSettingRepository _serviceSettings;
    private readonly ISageSdataReadClient _sage;
    private readonly IConfiguration _configuration;
    private readonly IDataProtectionProvider _schutz;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<SageFaArbeitsgaengeController> _logger;

    public SageFaArbeitsgaengeController(IFaHierarchyNodeRepository nodes, IOperationConfigRepository configs, IServiceSettingRepository serviceSettings,
        ISageSdataReadClient sage, IConfiguration configuration, IDataProtectionProvider schutz, ICurrentUserService currentUser,
        ILogger<SageFaArbeitsgaengeController> logger)
    {
        _nodes = nodes;
        _configs = configs;
        _serviceSettings = serviceSettings;
        _sage = sage;
        _configuration = configuration;
        _schutz = schutz;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>Server-Spaltenfilter (ADR 0005): Getter liefern den gerenderten Text.</summary>
    public static readonly IReadOnlyDictionary<string, Func<SageFaArbeitsgaenge.Zeile, string?>> ColumnMap =
        new Dictionary<string, Func<SageFaArbeitsgaenge.Zeile, string?>>
        {
            ["hauptfa"] = z => z.HauptFa.ToString(),
            ["fa"] = z => z.BelId.ToString(),
            ["ebene"] = z => z.Ebene,
            ["artnr"] = z => z.Artnr,
            ["matchcode"] = z => z.Matchcode,
            ["kuerzel"] = z => z.Kuerzel,
            ["pos"] = z => z.Reihenfolge.ToString(),
            ["sageag"] = z => z.SageAg,
            ["status"] = z => z.Status,
            ["aginsage"] = z => z.AgInSage?.ToString(),
            ["hinweis"] = z => z.Hinweis,
        };

    [HttpGet]
    public async Task<IActionResult> Index(int? hauptFa, int? fa, bool nurAnlegbar = false, bool pruefen = false, int page = 1, int? pageSize = null,
        CancellationToken ct = default)
    {
        if (!string.Equals(await _serviceSettings.GetValueAsync(HierarchischeStrukturKeys.Master), "true", StringComparison.OrdinalIgnoreCase))
        {
            TempData["WarningMessage"] = "„Arbeitsgänge → Sage“ gibt es nur mit hierarchischen Produktionsaufträgen (IDEAL).";
            return RedirectToAction("Index", "Home");
        }
        if (page < 1) page = 1;
        var userDefault = await _currentUser.GetDefaultPageSizeAsync();
        var effectivePageSize = PageSize.Resolve(pageSize, userDefault);
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);

        var knoten = (await _nodes.GetAllAsync())
            .Where(n => n.VerschwundenSeit == null && (hauptFa == null || n.HauptFA == hauptFa) && (fa == null || n.SubFA == fa || n.VaterFA == fa))
            .ToList();
        var configs = await _configs.GetAllAsync();
        var vm = new SageFaArbeitsgaengeViewModel { HauptFa = hauptFa, Fa = fa, NurAnlegbar = nurAnlegbar, Pruefen = pruefen };

        Dictionary<int, SageFaArbeitsgaenge.FaStand>? stand = null;
        var sageFehler = false;
        if (pruefen)
        {
            var ids = knoten.Where(n => n.SubFA != 0 && (fa == null || n.SubFA == fa)).Select(n => n.SubFA).Distinct().ToList();
            if (ids.Count > SageFaArbeitsgaenge.MaxFasJePruefung)
            {
                vm.SageBanner = $"{ids.Count} FAs in der Auswahl – geprüft werden höchstens {SageFaArbeitsgaenge.MaxFasJePruefung}. Bitte nach Haupt-FA filtern.";
                sageFehler = true;
            }
            else
            {
                var web = await SdataZugangsdaten.WebStandAsync(_serviceSettings, _configuration, _schutz);
                if (web.Ziel is not { } ziel)
                {
                    vm.SageBanner = "Sage nicht lesbar – SData nicht konfiguriert: " + string.Join(", ", web.NichtKonfiguriert);
                    sageFehler = true;
                }
                else
                {
                    try
                    {
                        _logger.LogInformation("Sage-AG-Vorschau: Sage-Stand fuer {Anzahl} FA(s) lesen.", ids.Count);
                        stand = await SageFaArbeitsgaenge.LesenAsync(_sage, ziel, ids, ct);
                    }
                    catch (SdataLeseException ex)
                    {
                        _logger.LogWarning(ex, "Sage-AG-Vorschau: Sage-Stand nicht lesbar.");
                        vm.SageBanner = "Sage nicht lesbar: " + ex.Message;
                        sageFehler = true;
                    }
                }
            }
        }

        var ergebnis = SageFaArbeitsgaenge.BuildVorschau(knoten, configs, stand, sageFehler);
        var gefiltert = ColumnFilterHelper.Apply(ergebnis.Zeilen, columnFilters, ColumnMap)
            .Where(z => !nurAnlegbar || z.IstAnlegbar).ToList();
        vm.Blaetter = ergebnis.Blaetter;
        vm.NurInSage = ergebnis.NurInSage;
        vm.Zeilen = gefiltert.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList();
        vm.Pagination = new PaginationState
        {
            CurrentPage = page,
            PageSize = effectivePageSize,
            PageSizeRaw = PageSize.ResolveRaw(pageSize, userDefault),
            TotalCount = gefiltert.Count,
        };
        return View(vm);
    }
}
