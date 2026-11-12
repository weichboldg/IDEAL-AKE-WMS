using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

/// <summary>
/// v1.99.4 (Spec 2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe E3, F4): Liste „Sage-Wareneingänge“ und „Erneut senden“ von Hand.
/// v1.99.7 (E5): der Dienst wiederholt selbst mit Sage-Prüfung; „Klären“ = kein automatisches Senden, weiter nur „Erneut senden“ oder
/// „Als übergeben markieren“; Q-2-Hinweis bei ausgeschaltetem Sage-Wareneingang. Lesen = Klassen-Filter
/// <see cref="RequireStockReadAccessAttribute"/> + Feature-Gate; „Erneut senden“ = <see cref="RequireStockKeyUserAccessAttribute"/> wie die Sage-Queue.
/// </summary>
public partial class PurchaseOrdersController
{
    public static string SageWeStatusText(SageWareneingangStatus s) => s switch
    {
        SageWareneingangStatus.NichtNoetig => "nicht nötig",
        SageWareneingangStatus.Offen => "offen (wird gesendet)",
        SageWareneingangStatus.Gesendet => "gesendet",
        SageWareneingangStatus.Uebergeben => "an Sage übergeben",
        SageWareneingangStatus.Fehler => "Fehler",
        SageWareneingangStatus.NurInSage => "nur in Sage buchen",
        SageWareneingangStatus.Klaeren => "Klären",
        _ => s.ToString(),
    };

    /// <summary>Korrelation am Sage-Beleg (Memo): WE#&lt;erste Receipt-Id der Gruppe&gt;# (Muster SM#&lt;id&gt;#, fallstricke).</summary>
    public static string SageWeMemo(int ersteReceiptId) => SageWareneingang.Memo(ersteReceiptId);

    // Server-Spaltenfilter: Col-Key -> gerenderter Zelltext (ADR 0005/0017)
    private static readonly Dictionary<string, Func<SageWareneingangZeile, string?>> SageWeColumnMap = new()
    {
        ["gebucht"] = z => z.Receipt.CreatedAt.ToString("dd.MM.yyyy HH:mm"),
        ["bestellung"] = z => z.Receipt.Belegnummer,
        ["pos"] = z => z.Receipt.BelPosId.ToString(),
        ["artikel"] = z => z.Receipt.StockMovement.Article.ArticleNumber,
        ["menge"] = z => z.Receipt.Quantity.ToString("0.###"),
        ["platz"] = z => z.Receipt.StockMovement.StorageLocation.Code,
        ["lieferschein"] = z => z.Receipt.Lieferschein,
        ["memo-position"] = z => z.Receipt.StockMovement.Note,
        ["status"] = z => SageWeStatusText(z.Receipt.SageWeStatus),
        ["versuche"] = z => z.Receipt.SageWeVersuche.ToString(),
        ["sagebeleg"] = z => z.Receipt.SageWeBelegnummer,
        ["memo"] = z => z.Memo,
        ["fehler"] = z => z.Receipt.SageWeFehler,
    };

    public async Task<IActionResult> SageWareneingaenge(SageWareneingangStatus? status, string? bestellung, DateTime? von, DateTime? bis,
        int page = 1, int? pageSize = null)
    {
        if (page < 1) page = 1;
        var userDefault = await _user.GetDefaultPageSizeAsync();
        var effectivePageSize = PageSize.Resolve(pageSize, userDefault);

        var receipts = await _receipts.GetSageWareneingaengeAsync(status, bestellung, von, bis);
        var memoJeVorgang = receipts.Where(r => r.SageWeVorgang != null).GroupBy(r => r.SageWeVorgang!.Value)
            .ToDictionary(g => g.Key, g => SageWeMemo(g.Min(r => r.Id)));
        var zeilen = receipts.Select(r => new SageWareneingangZeile
        {
            Receipt = r,
            Memo = r.SageWeVorgang is { } v && memoJeVorgang.TryGetValue(v, out var m) ? m : null,
        });
        var gefiltert = ColumnFilterHelper.Apply(zeilen.AsQueryable(), ColumnFilterHelper.ReadFromQuery(HttpContext?.Request), SageWeColumnMap,
            ColumnFilterTypes.For("PurchaseOrders/SageWareneingaenge")).ToList();

        return View(new SageWareneingaengeViewModel
        {
            Zeilen = gefiltert.Skip((page - 1) * effectivePageSize).Take(effectivePageSize).ToList(),
            Status = status, Bestellung = bestellung, Von = von, Bis = bis,
            CanErneutSenden = await _user.CanTransferStockAsync(),
            AusHinweis = SageWareneingang.AusHinweis(SageWareneingang.IstAn(await _serviceSettings.GetValueAsync(SageWareneingang.ToggleKey)),
                await _receipts.SageWeNichtUebergebenAsync()),
            Pagination = new PaginationState
            {
                CurrentPage = page, PageSize = effectivePageSize, PageSizeRaw = PageSize.ResolveRaw(pageSize, userDefault), TotalCount = gefiltert.Count,
            },
        });
    }

    /// <summary>„Erneut senden“ (Status Fehler oder Klären): die ganze Beleg-Gruppe zurück auf Offen. Der Dienst sucht vor dem Senden die
    /// Memo-Kennung in Sage (v1.99.7), das Sicherungsnetz nicht - der Mensch hat geprüft. Bei ausgeschaltetem Sage-Wareneingang gesperrt (Q-2).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireStockKeyUserAccess]
    public async Task<IActionResult> SageWeErneutSenden(Guid vorgang)
    {
        if (!SageWareneingang.IstAn(await _serviceSettings.GetValueAsync(SageWareneingang.ToggleKey)))
        {
            TempData["WarningMessage"] = "Nicht eingereiht: Der Sage-Wareneingang ist ausgeschaltet (Service-Einstellung „Wareneingänge als Sage-Wareneingangsbeleg buchen“).";
            return RedirectToAction(nameof(SageWareneingaenge));
        }
        var n = await _receipts.SageWeErneutSendenAsync(vorgang, _user.GetDisplayName(), _user.GetWindowsUserName());
        if (n == 0) TempData["WarningMessage"] = "Nichts erneut eingereiht: Der Wareneingang steht nicht (mehr) auf „Fehler“ oder „Klären“.";
        else TempData["SuccessMessage"] = $"{n} Position(en) erneut zum Senden vorgemerkt. Der Dienst sucht den Beleg vorher in Sage und legt ihn sonst beim nächsten Takt an.";
        return RedirectToAction(nameof(SageWareneingaenge));
    }

    /// <summary>v1.99.7 (E5, „Klären“): Beleg existiert in Sage - von Hand als übergeben markieren (Belegnummer Pflicht, max. 30 Zeichen), ohne Senden.
    /// Gilt für Fehler und Klären. Die Rest-Regel zählt die Menge danach als in Sage enthalten (Übergabe-Zeitpunkt = jetzt).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireStockKeyUserAccess]
    public async Task<IActionResult> SageWeAlsUebergeben(Guid vorgang, string? belegnummer)
    {
        var b = belegnummer?.Trim();
        if (string.IsNullOrEmpty(b) || b.Length > 30)
        {
            TempData["WarningMessage"] = "Nicht markiert: Bitte die Sage-Belegnummer angeben (höchstens 30 Zeichen).";
            return RedirectToAction(nameof(SageWareneingaenge));
        }
        var n = await _receipts.SageWeAlsUebergebenAsync(vorgang, b, _user.GetDisplayName(), _user.GetWindowsUserName());
        if (n == 0) TempData["WarningMessage"] = "Nicht markiert: Der Wareneingang steht nicht (mehr) auf „Fehler“ oder „Klären“.";
        else TempData["SuccessMessage"] = $"{n} Position(en) als an Sage übergeben markiert (Beleg {b}). Es wird nichts gesendet.";
        return RedirectToAction(nameof(SageWareneingaenge));
    }
}
