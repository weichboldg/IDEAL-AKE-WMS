using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

/// <summary>
/// v1.84.0, Spec 2026-11-08-wareneingang-aus-sage-bestellungen E2/E3: „Offene Bestellungen“ (Vorschau der naechsten X Wochen aus dem Cache
/// offener Sage-Bestellpositionen, nur EBB) und „Wareneingang zu Bestellung“. Lesen = <see cref="RequireStockReadAccessAttribute"/>,
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

    public PurchaseOrdersController(ISagePurchaseOrderLineRepository lines, IStorageLocationRepository locations, IAppSettingRepository settings,
        ICurrentUserService user, IPurchaseOrderReceiptRepository receipts, IStockMovementRepository movements,
        IServiceSettingRepository serviceSettings)
    {
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
        ["liefertermin"] = r => r.Line.Liefertermin?.ToString("dd.MM.yyyy") ?? "ohne Termin",
        ["lieferant"] = r => r.LieferantText,
        ["bestellung"] = r => r.Line.Belegnummer,
        ["artikel"] = r => r.Line.Artikelnummer,
        ["bezeichnung"] = r => r.Line.Bezeichnung1,
        ["bestellt"] = r => r.Line.MengeBestellt.ToString("0.###"),
        ["geliefert"] = r => r.Line.MengeGeliefert.ToString("0.###"),
        ["offen"] = r => r.Line.MengeOffen.ToString("0.###"),
        ["erfasst"] = r => r.ImWmsErfasst.ToString("0.###"),
        ["einheit"] = r => r.Line.MengeBasiseinheit,
        ["lagerplatz"] = r => r.ZielLagerplatz,
        ["hinweis"] = r => Hinweis(r),
    };

    /// <summary>Text der Hinweis-Spalte (Farbe nie allein): Termin, Einheit, Charge/Serie, Lieferant gesperrt.</summary>
    internal static string Hinweis(PurchaseOrderLineRow r) => string.Join(" · ", new[]
    {
        r.Ueberfaellig ? "überfällig" : null,
        r.OhneTermin ? "ohne Termin" : null,
        r.Line.EinheitAbweichend ? "Einheit abweichend" : null,
        r.Line.Chargenpflicht || r.Line.Nachweispflicht ? "Charge/Seriennr. in Sage" : null,
        r.Line.LieferantGesperrt ? "Lieferant gesperrt" : null,
    }.Where(t => t != null));

    public async Task<IActionResult> Index(int? wochen = null, string? lieferant = null, string? bestellung = null, string? artikel = null,
        string? lagerplatz = null, bool nurUeberfaellig = false, int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var standard = await _settings.GetIntValueAsync(AppSettingKeys.OffeneBestellungenVorschauWochen, 2);
        var x = Math.Clamp(wochen ?? standard, 1, 26);
        var heute = DateTime.Today;
        var bis = heute.AddDays(x * 7);

        var alle = await ZeilenAsync(await _lines.GetAllAsync());
        var lieferanten = alle.Where(r => r.Line.LieferantKto != null)
            .GroupBy(r => r.Line.LieferantKto!, StringComparer.OrdinalIgnoreCase)
            .Select(g => (g.Key, g.First().LieferantText ?? g.Key)).OrderBy(l => l.Item2).ToList();

        // F2: Horizont bis heute + X Wochen; Ueberfaellige und Positionen ohne Termin sind immer dabei (markiert, nie still weg).
        IEnumerable<PurchaseOrderLineRow> q = alle.Where(r => r.Line.Liefertermin == null || r.Line.Liefertermin <= bis);
        if (nurUeberfaellig) q = q.Where(r => r.Ueberfaellig);
        if (!string.IsNullOrWhiteSpace(lieferant)) q = q.Where(r => string.Equals(r.Line.LieferantKto, lieferant.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(bestellung)) q = q.Where(r => r.Line.Belegnummer.Contains(bestellung.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(artikel))
            q = q.Where(r => r.Line.Artikelnummer.Contains(artikel.Trim(), StringComparison.OrdinalIgnoreCase)
                          || (r.Line.Bezeichnung1?.Contains(artikel.Trim(), StringComparison.OrdinalIgnoreCase) ?? false));
        if (!string.IsNullOrWhiteSpace(lagerplatz))
            q = q.Where(r => r.ZielLagerplatz?.Contains(lagerplatz.Trim(), StringComparison.OrdinalIgnoreCase) ?? false);

        var sortiert = q.OrderBy(r => r.Line.Liefertermin == null).ThenBy(r => r.Line.Liefertermin).ThenBy(r => r.Line.Belegnummer)
            .ThenBy(r => r.Line.BelPosId).ToList();
        var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
        var gefiltert = ColumnFilterHelper.Apply(sortiert, columnFilters, ColumnMap, ColumnFilterTypes.For("PurchaseOrders/Index")).ToList();

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
            Wochen = x, Lieferant = lieferant, Bestellung = bestellung, Artikel = artikel, Lagerplatz = lagerplatz,
            NurUeberfaellig = nurUeberfaellig, HorizontBis = bis, Stand = await _lines.GetStandAsync(),
            CanReceive = await _user.CanAccessStockAsync(), Lieferanten = lieferanten,
        };
        return View(vm);
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
            ImWmsAbzuziehen = erfasst.GetValueOrDefault(l.BelPosId, ErfasstJeBelPos.Leer).Abzuziehen(l.SyncedAt),
            SageWeText = erfasst.GetValueOrDefault(l.BelPosId, ErfasstJeBelPos.Leer).SageWeText(SageWeStatusText),
            Ueberfaellig = l.Liefertermin != null && l.Liefertermin < heute,
        }).ToList();
    }
}
