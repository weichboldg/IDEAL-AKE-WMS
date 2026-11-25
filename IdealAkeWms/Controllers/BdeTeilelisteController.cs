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
    private readonly IUserViewPreferenceRepository _prefs;
    private readonly IServiceSettingRepository _serviceSettings;

    public BdeTeilelisteController(BdeTeilelisteService svc, IBdeTerminalRepository terminals, IProductionWorkplaceRepository workplaces,
        IBdeOperatorRepository operators, IAppSettingRepository settings, IFaHierarchyOrderInfoRepository orderInfos, ICurrentUserService userSvc,
        IUserViewPreferenceRepository prefs, IServiceSettingRepository serviceSettings)
    {
        _serviceSettings = serviceSettings;
        _svc = svc; _terminals = terminals; _workplaces = workplaces; _operators = operators; _settings = settings;
        _orderInfos = orderInfos; _userSvc = userSvc; _prefs = prefs;
    }

    /// <summary>E5 (Freigabe-Antwort 4): Filter je Terminal-Web-Benutzer, Werkbank und Werker - ohne Migration in UserViewPreference.</summary>
    internal static string FilterSchluessel(int workplaceId, int operatorId) => $"BdeTL:W{workplaceId}:O{operatorId}";

    /// <summary>Gespeicherter Filter: Wochen relativ zur aktuellen KW, damit "diese + naechste Woche" auch naechste Woche noch stimmt.</summary>
    internal sealed record GespeicherterFilter(string? Sicht, int? KwVonOffset, int? KwBisOffset, bool GemeldeteZeigen, int? PageSize,
        Dictionary<string, string>? Colf);

    internal static int? WochenOffset(string? kw, DateTime heute) =>
        IsoWeekRange.Parse(kw) is { } p
            ? (int)((System.Globalization.ISOWeek.ToDateTime(p.Year, p.Week, DayOfWeek.Monday) - System.Globalization.ISOWeek.ToDateTime(
                System.Globalization.ISOWeek.GetYear(heute), System.Globalization.ISOWeek.GetWeekOfYear(heute), DayOfWeek.Monday)).TotalDays / 7)
            : null;

    /// <summary>Gespeicherten Filter als Query-Werte fuer den Redirect (leere Woche bleibt als leerer Wert erhalten = bewusst kein KW-Filter).</summary>
    internal static Microsoft.AspNetCore.Routing.RouteValueDictionary AlsQuery(int workplaceId, int operatorId, GespeicherterFilter f, DateTime heute)
    {
        var rv = new Microsoft.AspNetCore.Routing.RouteValueDictionary
        {
            ["workplaceId"] = workplaceId, ["operatorId"] = operatorId, ["sicht"] = f.Sicht ?? "einzeln",
            ["kwVon"] = f.KwVonOffset is int v ? IsoWoche(heute.AddDays(7 * v)) : "",
            ["kwBis"] = f.KwBisOffset is int b ? IsoWoche(heute.AddDays(7 * b)) : "",
        };
        if (f.GemeldeteZeigen) rv["gemeldete"] = true;
        if (f.PageSize is int ps) rv["pageSize"] = ps;
        foreach (var (k, wert) in f.Colf ?? new()) rv["colf_" + k] = wert;
        return rv;
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

        // E5: ohne Filter-Angaben den gespeicherten Filter laden (Redirect, damit Spaltenfilter wie gewohnt aus der Query kommen),
        // sonst Antwort 7: Vorgabe aktuelle KW bis +1. Jeder Filter-Aufruf speichert.
        var uid = _userSvc.GetCurrentAppUserId();
        var prefKey = FilterSchluessel(workplaceId, vm.Werker.Id);
        var colf = ColumnFilterHelper.ReadFromQuery(Request);
        if (!Request.Query.Any(q => q.Key is "sicht" or "kwVon" or "kwBis" or "gemeldete" or "pageSize" or "page" || q.Key.StartsWith("colf_", StringComparison.Ordinal)))
        {
            if (uid is int u && await _prefs.GetByUserAndViewAsync(u, prefKey) is { } pref
                && TryLesen(pref.SettingsJson) is { } gespeichert)
                return RedirectToAction(nameof(Index), AlsQuery(workplaceId, vm.Werker.Id, gespeichert, DateTime.Today));
            (kwVon, kwBis) = (IsoWoche(DateTime.Today), IsoWoche(DateTime.Today.AddDays(7)));
        }
        else if (uid is int u && IstNavigation(Request))
        {
            var f = new GespeicherterFilter(sicht, WochenOffset(kwVon, DateTime.Today), WochenOffset(kwBis, DateTime.Today), gemeldete, pageSize,
                colf.Count > 0 ? colf : null);
            await _prefs.SaveAsync(u, prefKey, System.Text.Json.JsonSerializer.Serialize(f), _userSvc.GetDisplayName(), _userSvc.GetWindowsUserName());
        }

        vm.Summiert = string.Equals(sicht, "summiert", StringComparison.OrdinalIgnoreCase);
        vm.KwVon = kwVon; vm.KwBis = kwBis; vm.GemeldeteZeigen = gemeldete;
        vm.ReturnUrl = Request.Path + Request.QueryString;

        if (page < 1) page = 1;
        var userDefault = await _userSvc.GetDefaultPageSizeAsync();
        var size = PageSize.Resolve(pageSize, userDefault);
        var kw = IsoWeekRange.Resolve(kwVon, kwBis);
        vm.Beschichtet = await BeschichtetZelleAsync();
        var infos = await _orderInfos.GetAllAsync();
        vm.Kopf = VormontageService.BuildKopfLookup(infos);
        vm.FraesenPfadAktiv = await RequireFraesenPfadAktivFilter.IstAktivAsync(_settings);
        // v1.99.72 (F6): Lagerbestell-Zeilen nur bei Werkbank-Option (Teileliste ist hier schon wirksam) und LagerbestellungAktiv (nur "false" sperrt)
        var mitLager = wp.BdeTeilelisteLagerbestellungen
                       && !string.Equals(await _settings.GetValueAsync(AppSettingKeys.LagerbestellungAktiv), "false", StringComparison.OrdinalIgnoreCase);

        int total;
        if (vm.Summiert)
        {
            // Summiert: Positions-Spalten filtern vor dem Zusammenzaehlen, Matchcode/Summe/Anzahl auf dem Aggregat (wie Vormontage).
            var vorfilter = colf.Where(c => !BdeTeilelisteService.SpaltenSummiert.ContainsKey(c.Key)).ToDictionary(c => c.Key, c => c.Value);
            var daten = await _svc.LadenAsync(vm.Kuerzel, kw, gemeldete, vorfilter, vm.Beschichtet, vm.FraesenPfadAktiv, mitLager);
            var aggregate = BdeTeilelisteService.Summieren(daten.Zeilen, infos, colf, daten.Zusatz);
            vm.Waisen = daten.Waisen;
            total = aggregate.Count;
            vm.Aggregate = aggregate.Skip((page - 1) * size).Take(size).ToList();
        }
        else
        {
            var daten = await _svc.LadenAsync(vm.Kuerzel, kw, gemeldete, colf, vm.Beschichtet, vm.FraesenPfadAktiv, mitLager);
            vm.Spalten = BdeTeilelisteService.SpaltenZeile(vm.Kopf, vm.Beschichtet, daten.Zusatz);
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

    /// <summary>v1.99.72 (F12): der CSV-Export holt dieselbe URL per fetch (<c>pageSize=0</c>) – das ist keine Navigation und darf den
    /// gespeicherten Werker-Filter nicht ueberschreiben. Fehlt der Header (alte Browser), wird wie bisher gespeichert.</summary>
    internal static bool IstNavigation(HttpRequest request) =>
        !request.Headers.TryGetValue("Sec-Fetch-Mode", out var mode) || string.Equals(mode.ToString(), "navigate", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// E3 (F8–F11): Fertigmelden. Ein Schluessel mit Mengen = Einzelmeldung; sonst je Schluessel volle Sollmenge (Block, Summiert —
    /// ein Summiert-Wert traegt mehrere Schluessel, getrennt durch Zeilenumbruch). Der Client sendet die Absicht (Schluessel), der Server
    /// prueft gegen den aktuellen Stand (ADR 0026). Terminal = Terminal des Web-Benutzers, nie aus dem Formular.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Melden(int workplaceId, int operatorId, string[]? schluessel, string? gut, string? ausschuss, string? returnUrl)
    {
        var terminal = await TerminalAsync();
        if (terminal == null)
        {
            TempData["WarningMessage"] = "Für diesen Benutzer ist kein Terminal konfiguriert. Bitte an den BDE-Admin wenden.";
            return RedirectToAction("Index", "Home");
        }
        var texte = (schluessel ?? Array.Empty<string>()).SelectMany(t => t.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            .Select(t => t.TrimEnd('\r')).Where(t => t.Length > 0).ToList(); // Formular-Versand macht aus LF ein CRLF; Artnr nicht trimmen
        // v1.99.72 (Loesung 5): "LB|<ItemId>" = Lagerbestell-Position, vor TeilSchluessel.Parse getrennt; ungueltig bleibt Abweisung der ganzen Anfrage.
        var keys = texte.Select(t => t.StartsWith(BdeTeilelisteService.LagerPraefix, StringComparison.Ordinal)
                ? (BdeTeilelisteService.ParseLagerSchluessel(t) is int id ? new BdeTeilelisteService.MeldeAuftrag(null, null, null, id) : null)
                : (TeilSchluessel.Parse(t) is { } s ? new BdeTeilelisteService.MeldeAuftrag(s, null, null) : null))
            .ToList();
        var zurueck = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action(nameof(Index), new { workplaceId, operatorId })!;
        if (keys.Count == 0 || keys.Any(k => k == null))
        {
            TempData["WarningMessage"] = keys.Count == 0 ? "Keine Zeile ausgewählt – nichts gemeldet." : "Ungültige Zeilenauswahl – nichts gemeldet. Bitte Seite neu laden.";
            return LocalRedirect(zurueck);
        }

        decimal? g = null, a = null;
        if (keys.Count == 1 && (!string.IsNullOrWhiteSpace(gut) || !string.IsNullOrWhiteSpace(ausschuss)))
        {
            if (!TryMenge(gut, out g) || !TryMenge(ausschuss, out a))
            {
                TempData["WarningMessage"] = "Menge ist keine Zahl – nichts gemeldet.";
                return LocalRedirect(zurueck);
            }
            g ??= 0; a ??= 0;
        }

        var ergebnis = await _svc.MeldenAsync(operatorId, workplaceId, terminal.Id,
            keys.Select(k => k! with { Gut = g, Ausschuss = a }).ToList());
        TempData[ergebnis.AllesGut ? "SuccessMessage" : "WarningMessage"] = ergebnis.Text();
        return LocalRedirect(zurueck);
    }

    /// <summary>E6: Zelltext „Beschichtet“ je Position ueber den Resolver (Quelle aus den AppSettings, Auftragsmodell aus dem Master).
    /// Warnungen zu ungueltigen Quellwerten zeigt /Settings beim Speichern; hier wirkt der Wert wie „Automatisch“.</summary>
    private async Task<Func<FaHierarchyNode, string?>> BeschichtetZelleAsync()
    {
        var master = await _serviceSettings.GetValueAsync(IdealAkeWms.Services.HierarchischeStruktur.HierarchischeStrukturKeys.Master);
        var hierarchisch = master != null && (master.Equals("true", StringComparison.OrdinalIgnoreCase) || master == "1"
            || master.Equals("on", StringComparison.OrdinalIgnoreCase));
        var quellen = IdealAkeWms.Services.Beschichtung.BeschichtungResolver.ResolveQuellen(
            await _settings.GetValueAsync(AppSettingKeys.BeschichtungKennzeichenQuelle),
            await _settings.GetValueAsync(AppSettingKeys.BeschichtungTerminQuelle), hierarchisch);
        return n => BeschichtetText(IdealAkeWms.Services.Beschichtung.BeschichtungResolver.BeschichtetPosition(quellen, n));
    }

    internal static string BeschichtetText(bool? b) => b switch { true => "ja", false => "nein", null => "–" };

    internal static GespeicherterFilter? TryLesen(string json)
    {
        try { return System.Text.Json.JsonSerializer.Deserialize<GespeicherterFilter>(json); }
        catch (System.Text.Json.JsonException) { return null; } // kaputter Eintrag = Vorgabe; der naechste Filter-Aufruf ueberschreibt ihn
    }

    /// <summary>E5: "Filter zuruecksetzen" = gespeicherten Filter loeschen, zurueck auf die Vorgabe (aktuelle KW bis +1).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FilterZuruecksetzen(int workplaceId, int operatorId)
    {
        if (_userSvc.GetCurrentAppUserId() is int u) await _prefs.DeleteByUserAndViewAsync(u, FilterSchluessel(workplaceId, operatorId));
        return RedirectToAction(nameof(Index), new { workplaceId, operatorId });
    }

    /// <summary>E4 (F13): Werker nimmt seine eigene Meldung bis zum Schichtende zurueck; danach nur der BDE-Admin.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Zuruecknehmen(int meldungId, int workplaceId, int operatorId, string? returnUrl)
    {
        var zurueck = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action(nameof(Index), new { workplaceId, operatorId })!;
        if (await AktiverWerkerAsync(operatorId) == null)
        {
            TempData["WarningMessage"] = "Werker nicht angemeldet oder inaktiv – nichts geändert.";
            return LocalRedirect(zurueck);
        }
        var (ok, text) = await _svc.ZuruecknehmenAsync(meldungId, operatorId, null);
        TempData[ok ? "SuccessMessage" : "WarningMessage"] = text;
        return LocalRedirect(zurueck);
    }

    /// <summary>Formularzahl kulturunabhaengig (&lt;input type="number"&gt; sendet Punkt, Hand-Eingabe ggf. Komma). Leer = null.</summary>
    internal static bool TryMenge(string? text, out decimal? wert)
    {
        wert = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        if (!decimal.TryParse(text.Trim().Replace(',', '.'), System.Globalization.NumberStyles.AllowLeadingSign | System.Globalization.NumberStyles.AllowDecimalPoint,
                System.Globalization.CultureInfo.InvariantCulture, out var d)) return false;
        wert = d;
        return true;
    }

    /// <summary>Wert eines &lt;input type="week"&gt; (YYYY-Www) fuer ein Datum.</summary>
    internal static string IsoWoche(DateTime d) => $"{System.Globalization.ISOWeek.GetYear(d):D4}-W{System.Globalization.ISOWeek.GetWeekOfYear(d):D2}";
}
