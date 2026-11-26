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
    private readonly BdeFaAngebot _faAngebot;
    private readonly IBdeBookingService _booking;

    public BdeTeilelisteController(BdeTeilelisteService svc, IBdeTerminalRepository terminals, IProductionWorkplaceRepository workplaces,
        IBdeOperatorRepository operators, IAppSettingRepository settings, IFaHierarchyOrderInfoRepository orderInfos, ICurrentUserService userSvc,
        IUserViewPreferenceRepository prefs, IServiceSettingRepository serviceSettings, BdeFaAngebot faAngebot, IBdeBookingService booking)
    {
        _serviceSettings = serviceSettings; _faAngebot = faAngebot; _booking = booking;
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

    /// <summary>v1.99.76 (E4-7): Master <c>ProduktionsauftragHierarchisch</c> ("true"/"1"/"on"), EINE Lesung fuer Reiter, Melden und Beschichtet.</summary>
    private async Task<bool> HierarchischAsync()
    {
        var master = await _serviceSettings.GetValueAsync(IdealAkeWms.Services.HierarchischeStruktur.HierarchischeStrukturKeys.Master);
        return master != null && (master.Equals("true", StringComparison.OrdinalIgnoreCase) || master == "1"
            || master.Equals("on", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>v1.99.76: BDE-aktive Werkbank (<see cref="WerkbankKuerzelFamilie.SeiteVerfuegbar"/>) oder null. Die Seite liest
    /// <c>BdeNurFaMeldung</c> nicht (G5) und leitet nie zurueck aufs Terminal (ab Etappe 5 waere das ein Kreis).</summary>
    private async Task<ProductionWorkplace?> BdeWerkbankAsync(int workplaceId) =>
        await _workplaces.GetByIdAsync(workplaceId) is { } wp && WerkbankKuerzelFamilie.SeiteVerfuegbar(wp) ? wp : null;

    /// <summary>v1.99.76 (E4-7, AK 29): angefragte Sicht, ohne Angabe die Standardsicht der Werkbank; Teile-Reiter ohne Teile-Sicht → FA.</summary>
    internal static string SichtAufloesen(string? sicht, ProductionWorkplace wp, bool hierarchisch)
    {
        var s = sicht?.Trim().ToLowerInvariant();
        if (s is not ("einzeln" or "summiert" or "fa")) s = WerkbankKuerzelFamilie.TeileStandard(wp, hierarchisch) ? "einzeln" : "fa";
        return s != "fa" && !WerkbankKuerzelFamilie.TeileSichtVerfuegbar(wp, hierarchisch) ? "fa" : s;
    }

    private async Task<BdeOperator?> AktiverWerkerAsync(int? operatorId) =>
        operatorId is int id && await _operators.GetByIdAsync(id) is { IsActive: true } op ? op : null;

    private async Task<BdeTerminal?> TerminalAsync() =>
        _userSvc.GetCurrentAppUserId() is int uid ? await _terminals.GetByUserIdAsync(uid) : null;

    public async Task<IActionResult> Index(int workplaceId, int? operatorId, string? personalnummer, string? sicht, string? kwVon, string? kwBis,
        bool gemeldete = false, int page = 1, int? pageSize = null, int? markiert = null, string? ziel = null)
    {
        var terminal = await TerminalAsync();
        if (terminal == null)
        {
            TempData["WarningMessage"] = "Für diesen Benutzer ist kein Terminal konfiguriert. Bitte an den BDE-Admin wenden.";
            return RedirectToAction("Index", "Home");
        }
        var werkbaenke = await _workplaces.GetBdeActiveAsync();
        var wp = await BdeWerkbankAsync(workplaceId);
        if (wp == null) return View("NichtAktiv", werkbaenke); // AK 32d: Hinweis + Werkbank-Auswahl, kein Redirect

        var hierarchisch = await HierarchischAsync();
        var vm = new BdeTeilelisteViewModel
        {
            Werkbank = wp, Kuerzel = WerkbankKuerzelFamilie.Kuerzel(wp), Werkbaenke = werkbaenke,
            Hierarchisch = hierarchisch, TeileSicht = WerkbankKuerzelFamilie.TeileSichtVerfuegbar(wp, hierarchisch),
        };

        if (!string.IsNullOrWhiteSpace(personalnummer))
        {
            var op = await _operators.GetByPersonnelNumberAsync(personalnummer.Trim());
            var zurHistorie = ziel == "historie"; // v1.99.79 F-31
            if (op is { IsActive: true })
                return RedirectToAction(zurHistorie ? nameof(Tageshistorie) : nameof(Index), new { workplaceId, operatorId = op.Id });
            if (zurHistorie) vm.Sicht = "historie";
            vm.AnmeldeFehler = $"Unbekannte oder inaktive Personalnummer: {personalnummer.Trim()}";
            return View("Anmelden", vm);
        }
        vm.Werker = await AktiverWerkerAsync(operatorId);
        if (vm.Werker == null) return View("Anmelden", vm);
        vm.BdeAdmin = await _userSvc.CanManageBdeAdminAsync();

        // E5: ohne Filter-Angaben den gespeicherten Filter laden (Redirect, damit Spaltenfilter wie gewohnt aus der Query kommen),
        // sonst Antwort 7: Vorgabe aktuelle KW bis +1. Jeder Filter-Aufruf speichert.
        var uid = _userSvc.GetCurrentAppUserId();
        var prefKey = FilterSchluessel(workplaceId, vm.Werker.Id);
        var colf = ColumnFilterHelper.ReadFromQuery(Request);
        var ohneAngaben = !Request.Query.Any(q => q.Key is "sicht" or "kwVon" or "kwBis" or "gemeldete" or "pageSize" or "page" || q.Key.StartsWith("colf_", StringComparison.Ordinal));
        vm.Sicht = SichtAufloesen(ohneAngaben ? null : sicht, wp, hierarchisch);
        if (ohneAngaben)
        {
            if (uid is int u && await _prefs.GetByUserAndViewAsync(u, prefKey) is { } pref
                && TryLesen(pref.SettingsJson) is { } gespeichert)
                return RedirectToAction(nameof(Index), AlsQuery(workplaceId, vm.Werker.Id, gespeichert, DateTime.Today));
            (kwVon, kwBis) = (IsoWoche(DateTime.Today), IsoWoche(DateTime.Today.AddDays(7)));
        }
        else if (uid is int u && IstNavigation(Request))
        {
            var f = new GespeicherterFilter(vm.Sicht, WochenOffset(kwVon, DateTime.Today), WochenOffset(kwBis, DateTime.Today), gemeldete, pageSize,
                colf.Count > 0 ? colf : null);
            await _prefs.SaveAsync(u, prefKey, System.Text.Json.JsonSerializer.Serialize(f), _userSvc.GetDisplayName(), _userSvc.GetWindowsUserName());
        }

        vm.KwVon = kwVon; vm.KwBis = kwBis; vm.GemeldeteZeigen = gemeldete;
        vm.ReturnUrl = Request.Path + Request.QueryString;

        if (page < 1) page = 1;
        var userDefault = await _userSvc.GetDefaultPageSizeAsync();
        var size = PageSize.Resolve(pageSize, userDefault);

        if (vm.IstFa)
        {
            // E4-1/E4-2: Zeilenquelle wie available-operations (BdeFaAngebot), Spaltenfilter vor Pagination (ADR 0005), Pagination statt MaxHauptFa.
            var zeilen = ColumnFilterHelper.Apply(await _faAngebot.FaSichtAsync(wp, vm.Werker.Id), colf, BdeFaAngebot.Spalten,
                ColumnFilterTypes.For("BdeTeileliste/Fa")).ToList();
            vm.NurAgFertigmeldung = WerkbankKuerzelFamilie.NurAgFertigmeldungWirksam(wp, nurFaGlobal: false);
            vm.TerminalId = terminal.Id;
            (vm.KopfLaufend, vm.KopfPausiert) = await _faAngebot.KopfBuchungenAsync(vm.Werker.Id);
            foreach (var p in vm.KopfPausiert)
                if (p.WorkOperationId is int pwo && await _booking.FortsetzenSperreAsync(pwo, wp.Id) is { } grund)
                    vm.PausiertGesperrt[p.BookingId] = grund;
            // E4-5: Scan-Treffer ausserhalb der aktuellen Seite → Seite mit der Zeile; ausgefiltert = sichtbarer Hinweis statt still nichts.
            if (markiert is int m)
            {
                vm.Markiert = m;
                if (SeiteMitZeile(zeilen.Select(z => z.Op.Id).ToList(), m, size) is int seite) page = seite;
                else TempData["WarningMessage"] = "Der gescannte Arbeitsgang ist mit den aktuellen Spaltenfiltern nicht sichtbar. Spaltenfilter zurücksetzen.";
            }
            vm.FaZeilen = zeilen.Skip((page - 1) * size).Take(size).ToList();
            vm.Pagination = new PaginationState
            {
                CurrentPage = page, PageSize = size, PageSizeRaw = PageSize.ResolveRaw(pageSize, userDefault), TotalCount = zeilen.Count,
            };
            return View("Fa", vm);
        }
        var kuerzel = vm.Kuerzel!; // Teile-Sicht verfuegbar => Kuerzel gesetzt
        var kw = IsoWeekRange.Resolve(kwVon, kwBis);
        vm.Beschichtet = await BeschichtetZelleAsync(hierarchisch);
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
            var daten = await _svc.LadenAsync(kuerzel, kw, gemeldete, vorfilter, vm.Beschichtet, vm.FraesenPfadAktiv, mitLager);
            var aggregate = BdeTeilelisteService.Summieren(daten.Zeilen, infos, colf, daten.Zusatz);
            vm.Spalten = BdeTeilelisteService.SpaltenZeile(vm.Kopf, vm.Beschichtet, daten.Zusatz); // v1.99.74: Zellen der Einzelauftraege
            vm.Waisen = daten.Waisen;
            total = aggregate.Count;
            vm.Aggregate = aggregate.Skip((page - 1) * size).Take(size).ToList();
        }
        else
        {
            var daten = await _svc.LadenAsync(kuerzel, kw, gemeldete, colf, vm.Beschichtet, vm.FraesenPfadAktiv, mitLager);
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

    /// <summary>
    /// v1.99.78 (Spec 2026-11-25-bde-masken-vereinheitlichung, Etappe 5): Tageshistorie des Werkers (Buchungen + Teilmeldungen von heute,
    /// Quelle <see cref="BdeTageshistorie"/>). Gate-Folge wie <see cref="Index"/>; ohne Werkbank die Terminal-Vorgabe (Menuepunkt ohne Parameter).
    /// Spaltenfilter vor Pagination (ADR 0005), in C# – die Datenmenge eines Werker-Tages ist klein.
    /// </summary>
    public async Task<IActionResult> Tageshistorie([FromServices] IdealAkeWms.Data.ApplicationDbContext ctx, int? workplaceId, int? operatorId,
        int page = 1, int? pageSize = null)
    {
        var terminal = await TerminalAsync();
        if (terminal == null)
        {
            TempData["WarningMessage"] = "Für diesen Benutzer ist kein Terminal konfiguriert. Bitte an den BDE-Admin wenden.";
            return RedirectToAction("Index", "Home");
        }
        var werkbaenke = await _workplaces.GetBdeActiveAsync();
        var wp = await BdeWerkbankAsync(workplaceId ?? terminal.DefaultProductionWorkplaceId);
        if (wp == null) return View("NichtAktiv", werkbaenke);
        var hierarchisch = await HierarchischAsync();
        var vm = new BdeTeilelisteViewModel
        {
            Werkbank = wp, Kuerzel = WerkbankKuerzelFamilie.Kuerzel(wp), Werkbaenke = werkbaenke, Sicht = "historie",
            Hierarchisch = hierarchisch, TeileSicht = WerkbankKuerzelFamilie.TeileSichtVerfuegbar(wp, hierarchisch),
            Werker = await AktiverWerkerAsync(operatorId),
        };
        if (vm.Werker == null) return View("Anmelden", vm);

        if (page < 1) page = 1;
        var userDefault = await _userSvc.GetDefaultPageSizeAsync();
        var size = PageSize.Resolve(pageSize, userDefault);
        var zeilen = ColumnFilterHelper.Apply(await BdeTageshistorie.LadenAsync(ctx, vm.Werker.Id, DateTime.Today), ColumnFilterHelper.ReadFromQuery(Request),
            BdeTageshistorie.Spalten, ColumnFilterTypes.For("BdeTeileliste/Tageshistorie")).ToList();
        vm.Historie = zeilen.Skip((page - 1) * size).Take(size).ToList();
        vm.Pagination = new PaginationState
        {
            CurrentPage = page, PageSize = size, PageSizeRaw = PageSize.ResolveRaw(pageSize, userDefault), TotalCount = zeilen.Count,
        };
        return View(vm);
    }

    /// <summary>v1.99.72 (F12): der CSV-Export holt dieselbe URL per fetch (<c>pageSize=0</c>) – das ist keine Navigation und darf den
    /// gespeicherten Werker-Filter nicht ueberschreiben. Fehlt der Header (alte Browser), wird wie bisher gespeichert.</summary>
    /// <summary>v1.99.77 (E4-5): Seite (1-basiert), auf der der Arbeitsgang in der gefilterten Liste steht; null = nicht in der Liste.</summary>
    internal static int? SeiteMitZeile(IList<int> woIds, int woId, int size)
    {
        var i = woIds.IndexOf(woId);
        return i < 0 ? null : i / size + 1;
    }

    internal static bool IstNavigation(HttpRequest request) =>
        !request.Headers.TryGetValue("Sec-Fetch-Mode", out var mode) || string.Equals(mode.ToString(), "navigate", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// E3 (F8–F11): Fertigmelden. Ein Schluessel mit Mengen = Einzelmeldung; sonst je Schluessel volle Sollmenge (Block, Summiert —
    /// ein Summiert-Wert traegt mehrere Schluessel, getrennt durch Zeilenumbruch). Der Client sendet die Absicht (Schluessel), der Server
    /// prueft gegen den aktuellen Stand (ADR 0026). Terminal = Terminal des Web-Benutzers, nie aus dem Formular.
    /// v1.99.75 (E3, ADR 0037): <paramref name="art"/> "teil" = Teilfertig (nur Einzelzeile, Gut Pflicht, Zeile bleibt offen), sonst Fertig
    /// (abschliessend; ohne Menge = Restmenge je Zeile).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Melden(int workplaceId, int operatorId, string[]? schluessel, string? gut, string? ausschuss, string? returnUrl,
        string? art = null)
    {
        var terminal = await TerminalAsync();
        if (terminal == null)
        {
            TempData["WarningMessage"] = "Für diesen Benutzer ist kein Terminal konfiguriert. Bitte an den BDE-Admin wenden.";
            return RedirectToAction("Index", "Home");
        }
        var zurueckVorab = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action(nameof(Index), new { workplaceId, operatorId })!;
        // v1.99.76 (AK 31): Teilmeldung nur mit verfuegbarer Teile-Sicht (Master + BdeTeileliste + Kuerzel); der Service prueft TeilelisteWirksam.
        if (await _workplaces.GetByIdAsync(workplaceId) is not { } mwp || !WerkbankKuerzelFamilie.TeileSichtVerfuegbar(mwp, await HierarchischAsync()))
        {
            TempData["WarningMessage"] = "Diese Werkbank hat keine BDE-Teileliste – nichts gemeldet.";
            return LocalRedirect(zurueckVorab);
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

        var teil = string.Equals(art, "teil", StringComparison.OrdinalIgnoreCase);
        if (teil && keys.Count != 1)
        {
            TempData["WarningMessage"] = "Teilfertig geht nur für eine einzelne Zeile – nichts gemeldet.";
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
            keys.Select(k => k! with { Gut = g, Ausschuss = a, Abschliessend = !teil }).ToList());
        TempData[ergebnis.AllesGut ? "SuccessMessage" : "WarningMessage"] = ergebnis.Text();
        return LocalRedirect(zurueck);
    }

    /// <summary>E6: Zelltext „Beschichtet“ je Position ueber den Resolver (Quelle aus den AppSettings, Auftragsmodell aus dem Master).
    /// Warnungen zu ungueltigen Quellwerten zeigt /Settings beim Speichern; hier wirkt der Wert wie „Automatisch“.</summary>
    private async Task<Func<FaHierarchyNode, string?>> BeschichtetZelleAsync(bool hierarchisch)
    {
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

    /// <summary>v1.99.76 (E4-3, Etappe 4a): Anstempeln je FA-Zeile = Ruesten bzw. Produktion starten ueber den bestehenden
    /// <see cref="IBdeBookingService"/> (Startpruefungen dort). Terminal = Terminal des Web-Benutzers. Ergebnisse ohne eigenen Dialog
    /// (Kollision, InvalidState) erscheinen als sichtbare Meldung nach dem Neuladen - nichts still verschluckt; die Dialoge folgen mit 4b.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Anstempeln(int workplaceId, int operatorId, int workOperationId, string? art, string? returnUrl)
    {
        var zurueck = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action(nameof(Index), new { workplaceId, operatorId, sicht = "fa" })!;
        var terminal = await TerminalAsync();
        if (terminal == null)
        {
            TempData["WarningMessage"] = "Für diesen Benutzer ist kein Terminal konfiguriert. Bitte an den BDE-Admin wenden.";
            return RedirectToAction("Index", "Home");
        }
        if (await AktiverWerkerAsync(operatorId) == null || await BdeWerkbankAsync(workplaceId) == null)
        {
            TempData["WarningMessage"] = "Werker nicht angemeldet oder Werkbank nicht BDE-aktiv – nichts gebucht.";
            return LocalRedirect(zurueck);
        }
        var ruesten = string.Equals(art, "ruesten", StringComparison.OrdinalIgnoreCase);
        var r = ruesten
            ? await _booking.StartSetupAsync(operatorId, workOperationId, workplaceId, terminal.Id)
            : await _booking.StartProductionAsync(operatorId, workOperationId, workplaceId, terminal.Id);
        TempData[r.Outcome == BdeBookingOutcome.Success ? "SuccessMessage" : "WarningMessage"] = AnstempelnText(r, ruesten);
        return LocalRedirect(zurueck);
    }

    /// <summary>Meldetext je Ergebnis (Texte wie frueher <c>bde-terminal.js</c> post(), geloescht v1.99.78).</summary>
    internal static string AnstempelnText(BdeBookingResult r, bool ruesten) => r.Outcome switch
    {
        BdeBookingOutcome.Success => ruesten ? "Rüsten gestartet." : "Produktion gestartet.",
        BdeBookingOutcome.CollisionOtherOperator =>
            $"AG ist bereits in Arbeit durch {r.CollidingBooking?.BdeOperator?.DisplayName ?? "einen anderen Werker"} an "
            + $"{r.CollidingBooking?.ProductionWorkplace?.Name ?? "einer anderen Werkbank"} seit {r.CollidingBooking?.StartedAt:HH:mm} – nichts gebucht.",
        BdeBookingOutcome.NotFound => "Arbeitsgang nicht gefunden – nichts gebucht.",
        _ => (r.Message ?? "Aktion nicht möglich").TrimEnd('.', ' ') + ". Nichts gebucht.", // v1.99.81 (U-5): wie bde-fa-sicht.js ergebnis()
    };

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
