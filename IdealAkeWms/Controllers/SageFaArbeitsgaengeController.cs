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
    private readonly ISageFaArbeitsgangAnlageRepository _queue;
    private readonly ILogger<SageFaArbeitsgaengeController> _logger;

    public SageFaArbeitsgaengeController(IFaHierarchyNodeRepository nodes, IOperationConfigRepository configs, IServiceSettingRepository serviceSettings,
        ISageSdataReadClient sage, IConfiguration configuration, IDataProtectionProvider schutz, ICurrentUserService currentUser,
        ISageFaArbeitsgangAnlageRepository queue, ILogger<SageFaArbeitsgaengeController> logger)
    {
        _queue = queue;
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
        if (!await IstAnAsync(HierarchischeStrukturKeys.Master))
        {
            TempData["WarningMessage"] = NurHierarchisch;
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
            // QA F1: alle FAs der angezeigten Knoten (beim FA-Filter auch die Unter-FAs), sonst stuenden diese ohne Lesefehler auf „Sage nicht lesbar“
            var ids = knoten.Where(n => n.SubFA != 0).Select(n => n.SubFA).Distinct().ToList();
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

        var faIds = knoten.Where(n => n.SubFA != 0).Select(n => n.SubFA).Distinct().ToList();
        var queue = SageFaArbeitsgaenge.QueueStatus(await _queue.GetByBelIdsAsync(faIds));
        vm.SchalterAn = await IstAnAsync(SageFaArbeitsgaenge.ToggleKey);
        vm.BearbeiterFehlt = vm.SchalterAn && string.IsNullOrWhiteSpace(await _serviceSettings.GetValueAsync(SageFaArbeitsgaenge.BearbeiterKey));
        var ergebnis = SageFaArbeitsgaenge.BuildVorschau(knoten, configs, stand, sageFehler, queue);
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
        // Knopf je FA der Seite mit einreihbaren Zeilen (alle Zeilen des FA, nicht nur die der Seite)
        var seitenFas = vm.Zeilen.Select(z => z.BelId).ToHashSet();
        vm.Einreihbar = ergebnis.Zeilen.Where(z => seitenFas.Contains(z.BelId) && SageFaArbeitsgaenge.IstEinreihbar(z))
            .GroupBy(z => z.BelId).Select(g => new SageFaEinreihbar(g.Key, g.First().Artnr, g.First().Matchcode,
                g.DistinctBy(z => z.SageAg, StringComparer.OrdinalIgnoreCase).Select(z => z.SageAg!).ToList()))
            .ToList();
        return View(vm);
    }

    public const string NurHierarchisch = "„Arbeitsgänge → Sage“ gibt es nur mit hierarchischen Produktionsaufträgen (IDEAL).";
    public const string StandGeaendert = "Stand hat sich geändert – Vorschau neu laden und erneut prüfen. Nichts eingereiht.";

    /// <summary>E1a: reiht die bestaetigten AGs eines FA fuer den Dienst ein (Freigabe-Antwort 9). Schalter und Ziel-Stand prueft der Server
    /// am Schreibort erneut (ADR 0026): jede erwartete Sage-AG muss zum FA gehoeren (Kuerzel im Feld, wirksame Sage-AG), sonst nichts. Sage
    /// liest erst der Dienst unmittelbar vor dem Senden (Duplikat-, Beendet- und Begonnen-Schutz).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Anlegen(int belId, string[]? erwarteteAgNummern, string? zurueck)
    {
        IActionResult Zurueck() => Url.IsLocalUrl(zurueck) ? Redirect(zurueck!) : RedirectToAction(nameof(Index), new { fa = belId });
        if (!await IstAnAsync(HierarchischeStrukturKeys.Master))
        {
            TempData["WarningMessage"] = NurHierarchisch;
            return RedirectToAction("Index", "Home");
        }
        if (!await IstAnAsync(SageFaArbeitsgaenge.ToggleKey))
        {
            TempData["WarningMessage"] = SageFaArbeitsgaenge.AusText;
            return Zurueck();
        }
        var erwartet = (erwarteteAgNummern ?? Array.Empty<string>()).Select(a => a?.Trim()).Where(a => !string.IsNullOrEmpty(a))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var zeilen = SageFaArbeitsgaenge.BuildVorschau((await _nodes.GetAllAsync()).Where(n => n.SubFA == belId), await _configs.GetAllAsync(), null)
            .Zeilen.Where(z => z.BelId == belId && z.SageAg != null).ToList();
        if (belId <= 0 || erwartet.Count == 0 || erwartet.Any(a => !zeilen.Any(z => string.Equals(z.SageAg, a, StringComparison.OrdinalIgnoreCase))))
        {
            TempData["WarningMessage"] = StandGeaendert;
            return Zurueck();
        }
        var neu = erwartet.Select(a => zeilen.First(z => string.Equals(z.SageAg, a, StringComparison.OrdinalIgnoreCase)))
            .Select(z => new IdealAkeWms.Models.SageFaArbeitsgangAnlage
            {
                BelId = belId, HauptFa = z.HauptFa, Kuerzel = z.Kuerzel, SageArbeitsgangnummer = z.SageAg!, Reihenfolge = z.Reihenfolge,
            }).ToList();
        var anzahl = await _queue.EinreihenAsync(neu, _currentUser.GetDisplayName(), _currentUser.GetWindowsUserName());
        _logger.LogInformation("Sage-AG-Anlage: FA {BelId}, {Anzahl} von {Erwartet} AG(s) eingereiht ({Ags}) durch {Benutzer}.",
            belId, anzahl, erwartet.Count, string.Join(",", erwartet), _currentUser.GetDisplayName());
        if (anzahl > 0)
            TempData["SuccessMessage"] = $"FA {belId}: {anzahl} Arbeitsgang/Arbeitsgänge eingereiht. Der Dienst prüft Sage und legt sie an; Status hier nach „Sage-Stand prüfen“ bzw. neu laden.";
        else
            TempData["WarningMessage"] = $"FA {belId}: nichts eingereiht – die Arbeitsgänge sind schon eingereiht, gesendet oder angelegt.";
        return Zurueck();
    }

    private async Task<bool> IstAnAsync(string key) =>
        string.Equals((await _serviceSettings.GetValueAsync(key))?.Trim(), "true", StringComparison.OrdinalIgnoreCase);
}
