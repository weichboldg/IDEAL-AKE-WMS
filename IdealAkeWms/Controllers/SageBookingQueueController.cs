using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

/// <summary>
/// Seite „Sage-Buchungen“ (bis v1.99.65 „Sage-Lagerbuchungen“): ausgehende Sage-Lagerbuchungen (Queue) und seit v1.99.66
/// (Spec 2026-11-23-sage-buchungen-gesamtsicht) auch die Sage-Wareneingänge in einer Liste. Liest nur; repariert wird über die bestehenden
/// POST-Actions (<see cref="Requeue"/> hier, die WE-Aktionen am <see cref="PurchaseOrdersController"/>), alle mit Rücksprung auf diese Seite.
/// Lesen: <see cref="RequireStockReadOrEinkaufAccessAttribute"/> (Einkauf ohne Lager-Recht sieht nur Sage-Wareneingänge, keine Knöpfe,
/// Rückfrage 12a); Requeue: <see cref="RequireStockReadAccessAttribute"/> + <see cref="RequireStockKeyUserAccessAttribute"/>.
/// </summary>
public class SageBookingQueueController : Controller
{
    private readonly ISageBookingQueueRepository _queue;
    private readonly ICurrentUserService _currentUserService;
    private readonly IServiceSettingRepository _serviceSettings;
    private readonly IPurchaseOrderReceiptRepository _receipts;
    private readonly IAppSettingRepository _appSettings;

    public SageBookingQueueController(
        ISageBookingQueueRepository queue,
        ICurrentUserService currentUserService,
        IServiceSettingRepository serviceSettings,
        IPurchaseOrderReceiptRepository receipts,
        IAppSettingRepository appSettings)
    {
        _queue = queue;
        _currentUserService = currentUserService;
        _serviceSettings = serviceSettings;
        _receipts = receipts;
        _appSettings = appSettings;
    }

    public static string StatusText(SageBookingQueueStatus s) => s switch
    {
        SageBookingQueueStatus.Offen => "Offen",
        SageBookingQueueStatus.Gesendet => "Gesendet",
        SageBookingQueueStatus.Bestaetigt => "Bestätigt",
        SageBookingQueueStatus.Fehler => "Fehler",
        _ => s.ToString()
    };

    [RequireStockReadOrEinkaufAccess]
    public async Task<IActionResult> Index(SageBuchungHerkunft? herkunft, SageBuchungAnzeige? status, bool nurOffene, string? bestellung,
        DateTime? von, DateTime? bis, int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var userDefaultPageSize = await _currentUserService.GetDefaultPageSizeAsync();
        var effectivePageSize = PageSize.Resolve(pageSize, userDefaultPageSize);
        var ab = (von ?? DateTime.Today.AddDays(-SageBuchungenGesamtsicht.StandardTage)).Date;

        // F7: Lagerbuchungen = stock_read-Kreis; WE-Zeilen = Modul an UND (stock_read oder Einkauf) — v1.99.69 B3: Modul aus = keine WE-Zeilen, auch fuer den Einkauf
        var stockRead = await _currentUserService.CanAccessStockReadAsync();
        var weModul = string.Equals(await _appSettings.GetValueAsync(AppSettingKeys.WareneingangBestellungAktiv), "true", StringComparison.OrdinalIgnoreCase);
        var zeigtWe = weModul && (stockRead || await RequireStockReadOrEinkaufAccessFilter.EinkaufAsync(_currentUserService, _appSettings));
        var canTransfer = stockRead && await _currentUserService.CanTransferStockAsync();
        if (!zeigtWe)
        {
            // ohne WE-Recht gibt es weder WE-Filter noch WE-Zeilen
            bestellung = null;
            if (herkunft == SageBuchungHerkunft.Wareneingang) herkunft = null;
        }

        // Nicht sichtbare bzw. ausgefilterte Quellen werden nicht gelesen
        var queue = stockRead && herkunft != SageBuchungHerkunft.Wareneingang && string.IsNullOrWhiteSpace(bestellung)
            ? await _queue.GetForMonitoringAsync(ab, bis) : [];
        var weGewuenscht = zeigtWe && (herkunft is null or SageBuchungHerkunft.Wareneingang);
        var receipts = weGewuenscht ? await _receipts.GetSageWareneingaengeAsync(bestellung, ab, bis) : [];

        var sageWeAn = SageWareneingang.IstAn(await _serviceSettings.GetValueAsync(SageWareneingang.ToggleKey));
        var csvAn = SageWareneingang.IstAn(await _serviceSettings.GetValueAsync(WareneingangCsv.AktivKey));
        var csvPfad = await _serviceSettings.GetValueAsync(WareneingangCsv.PfadKey);
        var zeilen = SageBuchungenGesamtsicht.Build(queue, await WeZeilenAsync(receipts, sageWeAn, csvAn, csvPfad))
            .Where(z => herkunft == null || z.Herkunft == herkunft)
            .Where(z => status == null || z.Anzeige == status)
            .Where(z => !nurOffene || SageBuchungenGesamtsicht.IstOffen(z.Anzeige));

        // Server-Side-Spaltenfilter ueber die ganze Union, vor der Pagination (ADR 0005)
        var gefiltert = ColumnFilterHelper.Apply(zeilen.AsQueryable(), ColumnFilterHelper.ReadFromQuery(HttpContext?.Request),
            SageBuchungenGesamtsicht.Spalten, ColumnFilterTypes.For("SageBookingQueue/Index")).ToList();
        var seite = gefiltert.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList();
        SageBuchungenGesamtsicht.MarkiereErsteJeVorgang(seite);

        var sslRaw = await _serviceSettings.GetValueAsync(SageTlsPolicy.SettingKey);
        var vm = new SageBookingQueueViewModel
        {
            Zeilen = seite,
            Herkunft = herkunft, Status = status, NurOffene = nurOffene, Bestellung = bestellung, Von = ab, Bis = bis,
            OffeneAusserhalb = gefiltert.Count(z => SageBuchungenGesamtsicht.IstOffen(z.Anzeige) && !SageBuchungenGesamtsicht.ImZeitraum(z.CreatedAt, ab, bis)),
            ZeigtLagerbuchungen = stockRead,
            ZeigtWareneingaenge = zeigtWe,
            CanRequeue = canTransfer,
            // die WE-Actions liegen hinter dem Klassen-Gate des PurchaseOrdersController (WareneingangBestellungAktiv, sonst 404)
            CanWeAktionen = canTransfer && weModul,
            CanReceive = zeigtWe && weModul && await _currentUserService.CanAccessStockAsync(),
            SslCheckDisabled = stockRead && !SageTlsPolicy.ShouldVerifyCertificate(sslRaw),
            AusHinweis = zeigtWe ? SageWareneingang.AusHinweis(sageWeAn, await _receipts.SageWeNichtUebergebenAsync()) : null,
            CsvHinweise = zeigtWe && csvAn ? WareneingangCsv.Hinweise(csvAn, sageWeAn, csvPfad, await _receipts.CsvWartendAsync()) : new(),
            Pagination = new PaginationState
            {
                CurrentPage = page,
                PageSize = effectivePageSize,
                PageSizeRaw = PageSize.ResolveRaw(pageSize, userDefaultPageSize),
                TotalCount = gefiltert.Count
            }
        };
        return View(vm);
    }

    /// <summary>Memo-Kennung je Vorgang und CSV-Stand (unverändert aus der bisherigen Liste „Sage-Wareneingänge“, v1.99.4/v1.99.9/v1.99.11).</summary>
    private async Task<List<SageWareneingangZeile>> WeZeilenAsync(List<PurchaseOrderReceipt> receipts, bool sageWeAn, bool csvAn, string? csvPfad)
    {
        if (receipts.Count == 0) return [];
        var kuerzel = (await _serviceSettings.GetValueAsync(SageSystemKuerzel.Key))?.Trim();
        var memoJeVorgang = receipts.Where(r => r.SageWeVorgang != null).GroupBy(r => r.SageWeVorgang!.Value)
            .ToDictionary(g => g.Key, g => PurchaseOrdersController.SageWeMemo(g.MinBy(r => r.Id)!, kuerzel));
        var exporte = await _receipts.GetCsvExporteAsync(receipts.Where(r => r.Barcode != null && r.SageWeVorgang != null)
            .Select(r => r.SageWeVorgang!.Value).Distinct().ToList());
        return receipts.Select(r =>
        {
            var export = r.SageWeVorgang is { } ev && exporte.TryGetValue(ev, out var x) ? x : null;
            return new SageWareneingangZeile
            {
                Receipt = r,
                Memo = r.SageWeVorgang is { } v && memoJeVorgang.TryGetValue(v, out var m) ? m : null,
                Csv = WareneingangCsv.StatusText(r, export, csvAn, sageWeAn, csvPfad),
                CsvFehler = export?.Status == WareneingangCsvStatus.Fehler,
            };
        }).ToList();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireStockReadAccess]
    [RequireStockKeyUserAccess]
    public async Task<IActionResult> Requeue(int id, string? returnUrl = null)
    {
        var item = await _queue.GetByIdWithMovementAsync(id);
        if (item == null)
            return NotFound();

        if (item.Status != SageBookingQueueStatus.Fehler)
        {
            TempData["WarningMessage"] = $"Eintrag #{id} ist nicht im Status Fehler und wird nicht erneut eingereiht.";
            return Zurueck(this, returnUrl, null);
        }

        // Zuruecksetzen auf Offen. Der Worker prueft VOR dem erneuten Senden ueber den Sage-Memo-Lookup,
        // ob die Buchung dort bereits existiert (Doppelbuchungs-Schutz, B2) — kein blindes Resend.
        await _queue.RequeueAsync(id, _currentUserService.GetDisplayName());
        TempData["SuccessMessage"] = $"Eintrag #{id} wurde erneut eingereiht. Der Dienst prüft vor dem Senden, ob die Buchung in Sage bereits existiert.";
        return Zurueck(this, returnUrl, null);
    }

    /// <summary>v1.99.66 (F5): Rücksprung auf die gefilterte Seite (nur lokale URL, Muster BdeTeilelisteController), sonst Sage-Buchungen
    /// (WE-Actions mit Herkunft Wareneingang).</summary>
    internal static IActionResult Zurueck(Controller c, string? returnUrl, SageBuchungHerkunft? herkunft)
        => !string.IsNullOrEmpty(returnUrl) && c.Url.IsLocalUrl(returnUrl)
            ? c.LocalRedirect(returnUrl)
            : c.RedirectToAction(nameof(Index), "SageBookingQueue", herkunft == null ? null : new { herkunft });
}
