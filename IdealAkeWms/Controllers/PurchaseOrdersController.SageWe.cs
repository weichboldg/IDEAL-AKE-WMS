using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace IdealAkeWms.Controllers;

/// <summary>
/// v1.99.4 (Spec 2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe E3, F4): Liste „Sage-Wareneingänge“ und „Erneut senden“ von Hand.
/// v1.99.66 (Spec 2026-11-23-sage-buchungen-gesamtsicht): Liste in „Sage-Buchungen“ (<see cref="SageBookingQueueController"/>) aufgegangen, hier nur
/// noch die Umleitung und die Aktionen (Rücksprung über <c>returnUrl</c>).
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

    /// <summary>Korrelation am Sage-Beleg (Memo): Kennung des ersten Receipts der Gruppe, WE#[K#]&lt;Id&gt;# (Muster SM#&lt;id&gt;#, fallstricke).
    /// v1.99.9: dieselbe Regel wie der Dienst (<see cref="SageWareneingang.Kennung"/>) - gesendet = gespeicherte Kennung, nie gesendet = mit dem
    /// aktuellen System-Kuerzel.</summary>
    public static string SageWeMemo(PurchaseOrderReceipt erste, string? kuerzel) => SageWareneingang.Kennung(erste, kuerzel);

    /// <summary>v1.99.66 (Spec 2026-11-23-sage-buchungen-gesamtsicht F10): die Liste ist in „Sage-Buchungen“ aufgegangen - alte Lesezeichen leiten
    /// dauerhaft (301) dorthin um, mit Herkunft Wareneingang; der Status wird über die F2-Zuordnung übersetzt (NichtNoetig/NurInSage fallen weg).
    /// Muster „Abgeloeste Routen“ (ProductionOrdersController). Als GET weiter hinter den Klassen-Filtern dieses Controllers.</summary>
    public IActionResult SageWareneingaenge(SageWareneingangStatus? status, string? bestellung, DateTime? von, DateTime? bis)
        => UmleitungSageBuchungen(status, bestellung, von, bis, nurOffene: false);

    /// <summary>Gemeinsam für Lager und <see cref="EinkaufPurchaseOrdersController"/> (dort mit „nur offene“).</summary>
    internal IActionResult UmleitungSageBuchungen(SageWareneingangStatus? status, string? bestellung, DateTime? von, DateTime? bis, bool nurOffene)
    {
        var ziel = new RouteValueDictionary
        {
            ["herkunft"] = SageBuchungHerkunft.Wareneingang,
            ["status"] = status is { } s ? SageBuchungenGesamtsicht.Anzeige(s) : null,
            ["bestellung"] = bestellung,
            ["von"] = von?.ToString("yyyy-MM-dd"),
            ["bis"] = bis?.ToString("yyyy-MM-dd"),
        };
        if (nurOffene) ziel["nurOffene"] = true;
        return RedirectToActionPermanent("Index", "SageBookingQueue", ziel);
    }

    /// <summary>„Erneut senden“ (Status Fehler oder Klären): die ganze Beleg-Gruppe zurück auf Offen. Der Dienst sucht vor dem Senden die
    /// Memo-Kennung in Sage (v1.99.7), das Sicherungsnetz nicht - der Mensch hat geprüft. Bei ausgeschaltetem Sage-Wareneingang gesperrt (Q-2).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireStockKeyUserAccess]
    public async Task<IActionResult> SageWeErneutSenden(Guid vorgang, string? returnUrl = null)
    {
        if (!SageWareneingang.IstAn(await _serviceSettings.GetValueAsync(SageWareneingang.ToggleKey)))
        {
            TempData["WarningMessage"] = "Nicht eingereiht: Der Sage-Wareneingang ist ausgeschaltet (Service-Einstellung „Wareneingänge als Sage-Wareneingangsbeleg buchen“).";
            return SageBookingQueueController.Zurueck(this, returnUrl, SageBuchungHerkunft.Wareneingang);
        }
        var n = await _receipts.SageWeErneutSendenAsync(vorgang, _user.GetDisplayName(), _user.GetWindowsUserName());
        if (n == 0) TempData["WarningMessage"] = "Nichts erneut eingereiht: Der Wareneingang steht nicht (mehr) auf „Fehler“ oder „Klären“.";
        else TempData["SuccessMessage"] = $"{n} Position(en) erneut zum Senden vorgemerkt. Der Dienst sucht den Beleg vorher in Sage und legt ihn sonst beim nächsten Takt an.";
        return SageBookingQueueController.Zurueck(this, returnUrl, SageBuchungHerkunft.Wareneingang);
    }

    /// <summary>v1.99.7 (E5, „Klären“): Beleg existiert in Sage - von Hand als übergeben markieren (Belegnummer Pflicht, max. 45 Zeichen seit v1.99.22), ohne Senden.
    /// Gilt für Fehler und Klären. Die Rest-Regel zählt die Menge danach als in Sage enthalten (Übergabe-Zeitpunkt = jetzt).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireStockKeyUserAccess]
    public async Task<IActionResult> SageWeAlsUebergeben(Guid vorgang, string? belegnummer, string? returnUrl = null)
    {
        var b = belegnummer?.Trim();
        if (string.IsNullOrEmpty(b) || b.Length > SagePurchaseOrderLine.BelegnummerMaxLaenge)
        {
            TempData["WarningMessage"] = "Nicht markiert: Bitte die Sage-Belegnummer angeben (höchstens 45 Zeichen).";
            return SageBookingQueueController.Zurueck(this, returnUrl, SageBuchungHerkunft.Wareneingang);
        }
        var n = await _receipts.SageWeAlsUebergebenAsync(vorgang, b, _user.GetDisplayName(), _user.GetWindowsUserName());
        if (n == 0) TempData["WarningMessage"] = "Nicht markiert: Der Wareneingang steht nicht (mehr) auf „Fehler“ oder „Klären“.";
        else TempData["SuccessMessage"] = $"{n} Position(en) als an Sage übergeben markiert (Beleg {b}). Es wird nichts gesendet.";
        return SageBookingQueueController.Zurueck(this, returnUrl, SageBuchungHerkunft.Wareneingang);
    }

    /// <summary>v1.99.11 (Wareneingang-CSV F4): Ausgangszeile im Status Fehler zurück auf Offen - der Dienst schreibt beim nächsten Takt neu
    /// (eine vorhandene Datei wird auch dann nicht überschrieben). Recht wie „Erneut senden“.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireStockKeyUserAccess]
    public async Task<IActionResult> CsvErneutSchreiben(Guid vorgang, string? returnUrl = null)
    {
        var n = await _receipts.CsvErneutSchreibenAsync(vorgang, _user.GetDisplayName(), _user.GetWindowsUserName());
        if (n == 0) TempData["WarningMessage"] = "Nichts vorgemerkt: Die CSV-Übergabe steht nicht (mehr) auf „Fehler“.";
        else TempData["SuccessMessage"] = "CSV zum erneuten Schreiben vorgemerkt. Der Dienst schreibt sie beim nächsten Takt (eine vorhandene Datei wird nicht überschrieben).";
        return SageBookingQueueController.Zurueck(this, returnUrl, SageBuchungHerkunft.Wareneingang);
    }
}
