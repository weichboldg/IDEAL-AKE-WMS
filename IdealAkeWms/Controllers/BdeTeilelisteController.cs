using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using PageSize = IdealAkeWms.Services.PageSize;

namespace IdealAkeWms.Controllers;

/// <summary>
/// v1.99.32 (Spec 2026-11-13-bde-vereinfacht-teileliste-blechfertigung, ADR 0030): BDE-Teileliste der Blechfertigung am Terminal einer
/// Werkbank mit wirksamer Option <see cref="ProductionWorkplace.BdeTeileliste"/>. Der Werker meldet sich per Personalnummer an
/// (BDE-Mitarbeiter, nicht Web-Benutzer); <c>operatorId</c> reist im Query mit und wird je Aufruf gegen aktive BDE-Mitarbeiter geprueft.
/// Terminal = Terminal des angemeldeten Web-Benutzers (nie aus dem Formular). Zugriff wie das Terminal.
/// </summary>
[RequireBdeActive]
[RequireBdeUserAccess]
public class BdeTeilelisteController : Controller
{
    private readonly BdeTeilelisteService _svc;
    private readonly IBdeTerminalRepository _terminals;
    private readonly IProductionWorkplaceRepository _workplaces;
    private readonly IBdeOperatorRepository _operators;
    private readonly IAppSettingRepository _settings;
    private readonly IFaHierarchyOrderInfoRepository _orderInfos;
    private readonly ICurrentUserService _userSvc;

    public BdeTeilelisteController(BdeTeilelisteService svc, IBdeTerminalRepository terminals, IProductionWorkplaceRepository workplaces,
        IBdeOperatorRepository operators, IAppSettingRepository settings, IFaHierarchyOrderInfoRepository orderInfos, ICurrentUserService userSvc)
    {
        _svc = svc; _terminals = terminals; _workplaces = workplaces; _operators = operators; _settings = settings;
        _orderInfos = orderInfos; _userSvc = userSvc;
    }

    private async Task<bool> NurFaAsync() =>
        (await _settings.GetValueAsync(AppSettingKeys.BdeNurFaMeldung))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>Werkbank mit wirksamer Teileliste oder null (dann zurueck ins Terminal, das selbst entscheidet).</summary>
    private async Task<ProductionWorkplace?> WirksameWerkbankAsync(int workplaceId)
    {
        var wp = await _workplaces.GetByIdAsync(workplaceId);
        return wp != null && WerkbankKuerzelFamilie.TeilelisteWirksam(wp, await NurFaAsync()) ? wp : null;
    }

    private async Task<BdeOperator?> AktiverWerkerAsync(int? operatorId) =>
        operatorId is int id && await _operators.GetByIdAsync(id) is { IsActive: true } op ? op : null;

    private async Task<BdeTerminal?> TerminalAsync() =>
        _userSvc.GetCurrentAppUserId() is int uid ? await _terminals.GetByUserIdAsync(uid) : null;

    public async Task<IActionResult> Index(int workplaceId, int? operatorId, string? personalnummer, string? sicht, string? kwVon, string? kwBis,
        bool gemeldete = false, int page = 1, int? pageSize = null)
    {
        if (await TerminalAsync() == null)
        {
            TempData["WarningMessage"] = "Für diesen Benutzer ist kein Terminal konfiguriert. Bitte an den BDE-Admin wenden.";
            return RedirectToAction("Index", "Home");
        }
        var wp = await WirksameWerkbankAsync(workplaceId);
        if (wp == null) return RedirectToAction("Index", "BdeTerminal", new { workplaceId });

        var vm = new BdeTeilelisteViewModel
        {
            Werkbank = wp, Kuerzel = WerkbankKuerzelFamilie.Kuerzel(wp)!, Werkbaenke = await _workplaces.GetBdeActiveAsync(),
        };

        if (!string.IsNullOrWhiteSpace(personalnummer))
        {
            var op = await _operators.GetByPersonnelNumberAsync(personalnummer.Trim());
            if (op is { IsActive: true }) return RedirectToAction(nameof(Index), new { workplaceId, operatorId = op.Id });
            vm.AnmeldeFehler = $"Unbekannte oder inaktive Personalnummer: {personalnummer.Trim()}";
            return View("Anmelden", vm);
        }
        vm.Werker = await AktiverWerkerAsync(operatorId);
        if (vm.Werker == null) return View("Anmelden", vm);

        // Antwort 7: ohne Angaben die Vorgabe aktuelle KW bis +1
        if (!Request.Query.Any(q => q.Key is "sicht" or "kwVon" or "kwBis" or "gemeldete" or "pageSize" || q.Key.StartsWith("colf_", StringComparison.Ordinal)))
            (kwVon, kwBis) = (IsoWoche(DateTime.Today), IsoWoche(DateTime.Today.AddDays(7)));

        vm.Summiert = string.Equals(sicht, "summiert", StringComparison.OrdinalIgnoreCase);
        vm.KwVon = kwVon; vm.KwBis = kwBis; vm.GemeldeteZeigen = gemeldete;
        vm.ReturnUrl = Request.Path + Request.QueryString;

        if (page < 1) page = 1;
        var userDefault = await _userSvc.GetDefaultPageSizeAsync();
        var size = PageSize.Resolve(pageSize, userDefault);
        var colf = ColumnFilterHelper.ReadFromQuery(Request);
        var kw = IsoWeekRange.Resolve(kwVon, kwBis);
        var infos = await _orderInfos.GetAllAsync();
        vm.Kopf = VormontageService.BuildKopfLookup(infos);

        int total;
        if (vm.Summiert)
        {
            // Summiert: Positions-Spalten filtern vor dem Zusammenzaehlen, Matchcode/Summe/Anzahl auf dem Aggregat (wie Vormontage).
            var vorfilter = colf.Where(c => !BdeTeilelisteService.SpaltenSummiert.ContainsKey(c.Key)).ToDictionary(c => c.Key, c => c.Value);
            var daten = await _svc.LadenAsync(vm.Kuerzel, kw, gemeldete, vorfilter, vm.Beschichtet);
            var aggregate = BdeTeilelisteService.Summieren(daten.Zeilen, infos, colf);
            vm.Waisen = daten.Waisen;
            total = aggregate.Count;
            vm.Aggregate = aggregate.Skip((page - 1) * size).Take(size).ToList();
        }
        else
        {
            var daten = await _svc.LadenAsync(vm.Kuerzel, kw, gemeldete, colf, vm.Beschichtet);
            vm.Waisen = daten.Waisen;
            total = daten.Zeilen.Count;
            vm.Zeilen = daten.Zeilen.Skip((page - 1) * size).Take(size).ToList();
        }
        vm.Pagination = new PaginationState
        {
            CurrentPage = page, PageSize = size, PageSizeRaw = PageSize.ResolveRaw(pageSize, userDefault), TotalCount = total,
        };
        return View(vm.Summiert ? "Summiert" : "Index", vm);
    }

    /// <summary>Wert eines &lt;input type="week"&gt; (YYYY-Www) fuer ein Datum.</summary>
    internal static string IsoWoche(DateTime d) => $"{System.Globalization.ISOWeek.GetYear(d):D4}-W{System.Globalization.ISOWeek.GetWeekOfYear(d):D2}";
}
