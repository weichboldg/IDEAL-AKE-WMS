using System.Globalization;
using System.Text.Json;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

/// <summary>
/// v1.84.0, Spec 2026-11-08-wareneingang-aus-sage-bestellungen E2/E3: „Offene Bestellungen“ (aus dem Cache
/// offener Sage-Bestellpositionen, EBB und seit v1.99.15 Rahmenabrufe EBZ) und „Wareneingang zu Bestellung“. Lesen = <see cref="RequireStockReadAccessAttribute"/>,
/// Buchen = <see cref="RequireStockAccessAttribute"/> (Read/Edit-Split, ADR 0006); Feature-Gate <c>WareneingangBestellungAktiv</c> kumulativ (404).
/// </summary>
[RequireStockReadAccess]
[RequireWareneingangBestellungAktiv]
public partial class PurchaseOrdersController : Controller
{
    private readonly ISagePurchaseOrderLineRepository _lines;
    private readonly IStorageLocationRepository _locations;
    private readonly IAppSettingRepository _settings;
    private readonly ICurrentUserService _user;
    private readonly IPurchaseOrderReceiptRepository _receipts;
    private readonly IStockMovementRepository _movements;
    private readonly IServiceSettingRepository _serviceSettings;
    private readonly IPurchaseProposalRepository _bedarfe; // v1.99.22 offene Bedarfe im Wareneingang (Abfragen der Bestellvorschlaege)
    private readonly IUserViewPreferenceRepository _prefs;  // v1.99.31 F4.5 Checkbox „Überfällige anzeigen“ je Benutzer und Ansicht
    private readonly IArticleRepository _articles;          // v1.99.52 F3 Hauptlagerplatz als Ziel-Vorschlag im Wareneingang

    public PurchaseOrdersController(ISagePurchaseOrderLineRepository lines, IStorageLocationRepository locations, IAppSettingRepository settings,
        ICurrentUserService user, IPurchaseOrderReceiptRepository receipts, IStockMovementRepository movements,
        IServiceSettingRepository serviceSettings, IPurchaseProposalRepository bedarfe, IUserViewPreferenceRepository prefs,
        IArticleRepository articles)
    {
        _articles = articles;
        _prefs = prefs;
        _bedarfe = bedarfe;
        _serviceSettings = serviceSettings;
        _lines = lines;
        _locations = locations;
        _settings = settings;
        _user = user;
        _receipts = receipts;
        _movements = movements;
    }

    // Spaltenfilter (Server-Mode) – Zelltexte wie in der View (ADR 0005, ein Filter-Kern ADR 0017).
    private static readonly Dictionary<string, Func<PurchaseOrderLineRow, string?>> ColumnMap = new()
    {
        ["liefertermin"] = r => LieferterminText(r.Line.Liefertermin),
        ["lieferant"] = r => r.LieferantText,
        ["bestellung"] = r => r.Line.Belegnummer,
        ["ab-nummer"] = r => r.Line.AbNummer,     // v1.99.33 Teil B (Sage-Kopf USER_AB)
        ["kommission"] = r => r.Line.Kommission,  // v1.99.33 Teil B (Sage-Kopf USER_Kom)
        ["artikel"] = r => r.Line.Artikelnummer,
        ["bezeichnung"] = r => r.Line.Bezeichnung1,
        ["bestellt"] = r => r.Line.MengeBestellt.ToString("0.###"),
        ["geliefert"] = r => GeliefertText(r.Line),
        ["offen"] = r => r.Line.MengeOffen.ToString("0.###"),
        ["erfasst"] = r => r.ImWmsErfasst.ToString("0.###"),
        ["einheit"] = r => r.Line.MengeBasiseinheit,
        ["lagerplatz"] = r => r.ZielLagerplatz,
        ["hinweis"] = r => Hinweis(r),
    };

    /// <summary>v1.99.15 (Teil 3, F3): Sage liefert fuer Rahmenabrufe keine gelieferte Menge je Abruf → „–“, nie „0“.</summary>
    internal static string GeliefertText(SagePurchaseOrderLine l) => l.IstRahmenabruf ? "–" : l.MengeGeliefert.ToString("0.###");
    internal const string GeliefertAbrufTitel = "Sage liefert für Rahmenabrufe keine gelieferte Menge je Abruf";

    /// <summary>Text der Hinweis-Spalte (Farbe nie allein): Rahmenabruf, Termin, Einheit, Charge/Serie, Lieferant gesperrt.</summary>
    internal static string Hinweis(PurchaseOrderLineRow r) => string.Join(" · ", new[]
    {
        r.Line.IstRahmenabruf ? "Rahmenabruf" : null,
        r.Ueberfaellig ? "überfällig" : null,
        r.OhneTermin ? "ohne Termin" : null,
        r.Line.EinheitAbweichend ? "Einheit abweichend" : null,
        r.Line.Chargenpflicht || r.Line.Nachweispflicht ? "Charge/Seriennr. in Sage" : null,
        r.Line.LieferantGesperrt ? "Lieferant gesperrt" : null,
    }.Where(t => t != null));

    /// <summary>v1.99.31 F3.1: Liefertermin mit ISO-KW (wie Fertigungsaufträge) – der Kalender-Klick filtert auf „KWnn“ (Text enthält).</summary>
    internal static string LieferterminText(DateTime? d) => d is { } t ? $"{t:dd.MM.yyyy} KW{ISOWeek.GetWeekOfYear(t)}" : "ohne Termin";

    /// <summary>v1.99.31 F3.3: Vorbelegung des Spaltenfilters Liefertermin, Montag der aktuellen ISO-KW bis Sonntag der N-ten KW (jahresgenau).</summary>
    internal static string LieferterminVorbelegung(DateTime heute, int wochen)
    {
        var mo = ISOWeek.ToDateTime(ISOWeek.GetYear(heute), ISOWeek.GetWeekOfYear(heute), DayOfWeek.Monday);
        var so = mo.AddDays(7 * Math.Clamp(wochen, 1, 26) - 1);
        return $"{mo.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)}...{so.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)}";
    }

    /// <summary>Merker „schon vorbelegt“: überlebt das Leeren des Filters (applyServerFilters entfernt nur colf_*/page), Muster FaWorklist df1.</summary>
    internal const string VorbelegtMerker = "kw";
    internal const string LieferterminKey = "liefertermin";
    internal static string UeberfaelligeViewKey(bool einkauf) => einkauf ? "EinkaufPurchaseOrders.Ueberfaellige" : "PurchaseOrders.Ueberfaellige";

    /// <summary>v1.99.31 F4.2: Sichtbarkeit je Zeile nach allen anderen Filtern. Ohne Termin immer; überfällig nur mit der Checkbox (der
    /// Liefertermin-Filter wirkt auf sie nicht); sonst der Liefertermin-Filter. „nur überfällig“ = nur Überfällige. Reihenfolge bleibt.</summary>
    internal static (List<PurchaseOrderLineRow> Sichtbar, int UeberfaelligAusgeblendet) Sichtbarkeit(IReadOnlyList<PurchaseOrderLineRow> rows,
        string? lieferterminFilter, bool zeigeUeberfaellige, bool nurUeberfaellig)
    {
        if (nurUeberfaellig) return (rows.Where(r => r.Ueberfaellig).ToList(), 0);
        var filter = lieferterminFilter is null ? null : new Dictionary<string, string> { [LieferterminKey] = lieferterminFilter };
        var imBereich = ColumnFilterHelper.Apply(rows.Where(r => !r.OhneTermin && !r.Ueberfaellig), filter, ColumnMap,
            ColumnFilterTypes.For("PurchaseOrders/Index")).ToHashSet();
        var sichtbar = rows.Where(r => r.OhneTermin || (r.Ueberfaellig ? zeigeUeberfaellige : imBereich.Contains(r))).ToList();
        return (sichtbar, zeigeUeberfaellige ? 0 : rows.Count(r => r.Ueberfaellig));
    }

    public Task<IActionResult> Index(string? lieferant = null, string? bestellung = null, string? artikel = null, string? lagerplatz = null,
        bool nurUeberfaellig = false, bool? ueberfaellige = null, bool auchErfassteAbrufe = false, int page = 1, int? pageSize = null)
        => ListeAsync(false, lieferant, bestellung, artikel, lagerplatz, nurUeberfaellig, ueberfaellige, auchErfassteAbrufe, page, pageSize);

    /// <summary>v1.99.31 (F2/F3/F4): gemeinsame Liste für Lager → Offene Bestellungen und Einkauf → Offene Bestellungen
    /// (<see cref="EinkaufPurchaseOrdersController"/> ruft diese Methode mit <paramref name="einkauf"/> = true; Zugriff prüfen dort dessen
    /// Class-Filter, nicht die dieses Controllers).</summary>
    internal async Task<IActionResult> ListeAsync(bool einkauf, string? lieferant, string? bestellung, string? artikel, string? lagerplatz,
        bool nurUeberfaellig, bool? ueberfaellige, bool auchErfassteAbrufe, int page, int? pageSize)
    {
        var query = HttpContext?.Request?.Query;
        if (query == null || (!query.ContainsKey("colf_" + LieferterminKey) && !query.ContainsKey(VorbelegtMerker)))
        {
            // F3.3: Erstaufruf → Redirect mit sichtbarem, löschbarem Filterwert (nicht unsichtbar serverseitig injiziert)
            var route = new RouteValueDictionary();
            if (query != null) foreach (var q0 in query) route[q0.Key] = q0.Value.ToString();
            var wochen = await _settings.GetIntValueAsync(AppSettingKeys.OffeneBestellungenVorschauWochen, 2);
            route["colf_" + LieferterminKey] = LieferterminVorbelegung(DateTime.Today, wochen);
            route[VorbelegtMerker] = "1";
            return RedirectToAction(nameof(Index), einkauf ? "EinkaufPurchaseOrders" : "PurchaseOrders", route);
        }
        if (page < 1) page = 1;

        // F4.5: Checkbox je Benutzer und Ansicht (UserViewPreferences); null = gespeicherten Wert bzw. Default lesen
        var standardUeberfaellige = einkauf;   // Lager aus, Einkauf ein
        var userId = _user.GetCurrentAppUserId();
        bool? gespeichert = null;
        if (userId is { } uid) gespeichert = UeberfaelligeLesen((await _prefs.GetByUserAndViewAsync(uid, UeberfaelligeViewKey(einkauf)))?.SettingsJson);
        var zeigeUeberfaellige = ueberfaellige ?? gespeichert ?? standardUeberfaellige;
        if (ueberfaellige is { } neu && userId is { } id && neu != (gespeichert ?? standardUeberfaellige))
            await _prefs.SaveAsync(id, UeberfaelligeViewKey(einkauf), JsonSerializer.Serialize(new { an = neu }), _user.GetDisplayName(), _user.GetWindowsUserName());
        if (nurUeberfaellig) zeigeUeberfaellige = true;   // F4.3: schließt „Überfällige anzeigen“ ein

        var alle = await ZeilenAsync(await _lines.GetAllAsync());
        var lieferanten = alle.Where(r => r.Line.LieferantKto != null)
            .GroupBy(r => r.Line.LieferantKto!, StringComparer.OrdinalIgnoreCase)
            .Select(g => (g.Key, g.First().LieferantText ?? g.Key)).OrderBy(l => l.Item2).ToList();

        IEnumerable<PurchaseOrderLineRow> q = alle;
        if (!string.IsNullOrWhiteSpace(lieferant)) q = q.Where(r => string.Equals(r.Line.LieferantKto, lieferant.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(bestellung)) q = q.Where(r => r.Line.Belegnummer.Contains(bestellung.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(artikel))
            q = q.Where(r => r.Line.Artikelnummer.Contains(artikel.Trim(), StringComparison.OrdinalIgnoreCase)
                          || (r.Line.Bezeichnung1?.Contains(artikel.Trim(), StringComparison.OrdinalIgnoreCase) ?? false));
        if (!string.IsNullOrWhiteSpace(lagerplatz))
            q = q.Where(r => r.ZielLagerplatz?.Contains(lagerplatz.Trim(), StringComparison.OrdinalIgnoreCase) ?? false);
        // v1.99.15 (Teil 3, F3, Entscheidung 2): im WMS vollstaendig erfasste Rahmenabrufe (Rest 0) standardmaessig ausblenden - gezaehlt und
        // gemeldet (nie still weg); vor Spaltenfilter und Pagination. EBB mit Rest 0 bleibt sichtbar.
        var ausgeblendeteAbrufe = 0;
        if (!auchErfassteAbrufe)
        {
            var liste = q.ToList();
            q = liste.Where(r => !r.AbrufVollstaendigErfasst);
            ausgeblendeteAbrufe = liste.Count(r => r.AbrufVollstaendigErfasst);
        }

        var sortiert = q.OrderBy(r => r.Line.Liefertermin == null).ThenBy(r => r.Line.Liefertermin).ThenBy(r => r.Line.Belegnummer)
            .ThenBy(r => r.Line.BelPosId).ToList();
        // v1.99.31 F4.2: Liefertermin-Filter getrennt (wirkt nur auf nicht überfällige Zeilen mit Termin), übrige Spaltenfilter wie bisher
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        columnFilters.Remove(LieferterminKey, out var lieferterminFilter);
        var uebrige = ColumnFilterHelper.Apply(sortiert, columnFilters, ColumnMap, ColumnFilterTypes.For("PurchaseOrders/Index")).ToList();
        var (gefiltert, ueberfaelligAusgeblendet) = Sichtbarkeit(uebrige, lieferterminFilter, zeigeUeberfaellige, nurUeberfaellig);

        // F2.3: Knopf „Wareneingang“ nur mit Lager-Schreibrecht und WareneingangBestellungAktiv (im Lager per Class-Gate ohnehin an)
        var canReceive = await _user.CanAccessStockAsync() && (!einkauf || await WareneingangBestellungAnAsync());

        var userDefault = await _user.GetDefaultPageSizeAsync();
        var effectivePageSize = PageSize.Resolve(pageSize, userDefault);
        var vm = new PurchaseOrderListViewModel
        {
            Items = gefiltert.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList(),
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = PageSize.ResolveRaw(pageSize, userDefault),
                TotalCount = gefiltert.Count,
            },
            Lieferant = lieferant, Bestellung = bestellung, Artikel = artikel, Lagerplatz = lagerplatz,
            NurUeberfaellig = nurUeberfaellig, Ueberfaellige = zeigeUeberfaellige, UeberfaelligAusgeblendet = ueberfaelligAusgeblendet,
            AuchErfassteAbrufe = auchErfassteAbrufe, AusgeblendeteAbrufe = ausgeblendeteAbrufe, Stand = await _lines.GetStandAsync(),
            CanReceive = canReceive, Lieferanten = lieferanten, Einkauf = einkauf,
        };
        return View("~/Views/PurchaseOrders/Index.cshtml", vm);
    }

    /// <summary>AppSetting <c>WareneingangBestellungAktiv</c> (nur exakt "true", wie das Feature-Gate).</summary>
    private async Task<bool> WareneingangBestellungAnAsync()
        => string.Equals(await _settings.GetValueAsync(AppSettingKeys.WareneingangBestellungAktiv), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>SettingsJson <c>{"an":true|false}</c>; unlesbar = null (Default gilt).</summary>
    internal static bool? UeberfaelligeLesen(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("an", out var an) && an.ValueKind is JsonValueKind.True or JsonValueKind.False ? an.GetBoolean() : null;
        }
        catch (JsonException) { return null; }
    }

    /// <summary>Cache-Zeilen → Anzeige-Zeilen: WMS-Lagerplatz-Code, Lieferant, „im WMS erfasst“ (eine Abfrage, kein N+1).</summary>
    private async Task<List<PurchaseOrderLineRow>> ZeilenAsync(List<SagePurchaseOrderLine> lines)
    {
        var plaetze = (await _locations.GetAllOrderedAsync() ?? new()).ToDictionary(l => l.Id, l => l.Code);
        var erfasst = await _receipts.GetErfasstJeBelPosAsync(lines.Select(l => l.BelPosId).ToList());
        var heute = DateTime.Today;
        return lines.Select(l => new PurchaseOrderLineRow
        {
            Line = l,
            LieferantText = l.LieferantName != null ? $"{l.LieferantName} ({l.LieferantKto})" : l.LieferantKto,
            ZielLagerplatz = l.StorageLocationId is { } sid && plaetze.TryGetValue(sid, out var code) ? code : l.SagePlatzKurzbezeichnung,
            ZielNichtImWms = l.SagePlatzId != null && l.StorageLocationId == null,
            ImWmsErfasst = erfasst.GetValueOrDefault(l.BelPosId, ErfasstJeBelPos.Leer).Gesamt,
            ImWmsAbzuziehen = erfasst.GetValueOrDefault(l.BelPosId, ErfasstJeBelPos.Leer).Abzuziehen(l),   // v1.99.15: Belegart-abhaengig (EBZ zieht alles ab)
            SageWeText = erfasst.GetValueOrDefault(l.BelPosId, ErfasstJeBelPos.Leer).SageWeText(SageWeStatusText),
            Ueberfaellig = l.Liefertermin != null && l.Liefertermin < heute,
        }).ToList();
    }
}
