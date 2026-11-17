using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

/// <summary>
/// v1.99.31 (Spec 2026-11-16-offene-bestellungen-sofort-neu-lesen-einkauf-kw-filter F2/F2a): „Einkauf → Offene Bestellungen“ und
/// „Einkauf → Sage-Wareneingänge (offen)“, nur lesend. Rolle <c>einkauf</c> (oder admin) und Gate <c>EinkaufAktiv</c> kumulativ, wie
/// <see cref="PurchaseProposalsController"/>; KEIN Lager-Recht und kein <c>WareneingangBestellungAktiv</c> nötig (Antwort 8).
/// Die Listen baut der <see cref="PurchaseOrdersController"/> (eine Implementierung, zwei Aufrufer); dessen Class-Filter greifen hier NICHT.
/// Wareneingang buchen und die Sage-WE-Aktionen bleiben dort hinter <c>RequireStockAccess</c> bzw. <c>RequireStockKeyUserAccess</c>.
/// </summary>
[RequireEinkaufAccess]
[RequireEinkaufAktiv]
public class EinkaufPurchaseOrdersController : Controller
{
    private readonly PurchaseOrdersController _listen;

    public EinkaufPurchaseOrdersController(PurchaseOrdersController listen) => _listen = listen;

    public Task<IActionResult> Index(string? lieferant = null, string? bestellung = null, string? artikel = null, string? lagerplatz = null,
        bool nurUeberfaellig = false, bool? ueberfaellige = null, bool auchErfassteAbrufe = false, int page = 1, int? pageSize = null)
    {
        _listen.ControllerContext = ControllerContext;
        return _listen.ListeAsync(true, lieferant, bestellung, artikel, lagerplatz, nurUeberfaellig, ueberfaellige, auchErfassteAbrufe, page, pageSize);
    }

    public Task<IActionResult> SageWareneingaenge(SageWareneingangStatus? status, string? bestellung, DateTime? von, DateTime? bis,
        int page = 1, int? pageSize = null)
    {
        _listen.ControllerContext = ControllerContext;
        return _listen.SageWeListeAsync(true, status, bestellung, von, bis, page, pageSize);
    }
}
