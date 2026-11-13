using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using IdealAkeWms.Services;
using Microsoft.AspNetCore.Mvc;

namespace IdealAkeWms.Controllers;

/// <summary>v1.84.0 E3 (Spec 2026-11-08-wareneingang-aus-sage-bestellungen F3): Wareneingang zu einer Sage-Bestellung.</summary>
public partial class PurchaseOrdersController
{
    /// <summary>Einstieg per BelId (aus der Vorschau) oder Belegnummer (Eingabe/Scan, exakt getrimmt). Kein/mehrere Treffer = Meldung.</summary>
    [RequireStockAccess]
    public async Task<IActionResult> Receive(int? belId = null, string? belegnummer = null)
    {
        if (belId == null && !string.IsNullOrWhiteSpace(belegnummer))
        {
            var ids = await _lines.GetBelIdsByBelegnummerAsync(belegnummer);
            if (ids.Count == 1) return RedirectToAction(nameof(Receive), new { belId = ids[0] });
            TempData["WarningMessage"] = ids.Count == 0
                ? $"Bestellung „{belegnummer.Trim()}“ nicht gefunden (nur offene Bestellungen und Rahmenabrufe aus Sage, Stand des letzten Abgleichs)."
                : $"Belegnummer „{belegnummer.Trim()}“ gehört zu {ids.Count} Bestellungen – bitte über „Offene Bestellungen“ wählen.";
            return RedirectToAction(nameof(Receive));
        }
        if (belId == null) return View(new PurchaseOrderReceiveViewModel());

        var vm = await BuildReceiveAsync(belId.Value, null);
        if (vm == null)
        {
            TempData["WarningMessage"] = "Diese Bestellung ist nicht (mehr) offen – Sage hat sie geschlossen oder der Abgleich ist noch nicht gelaufen.";
            return RedirectToAction(nameof(Receive));
        }
        return View(vm);
    }

    /// <summary>
    /// Buchen (F3): alle Zeilen zuerst pruefen (keine Teilbuchung), dann je Zeile eine Einbuchung NUR ueber
    /// <see cref="IStockMovementRepository.AddAsync"/> (Sage-Queue-Decorator), der Wareneingang haengt als Navigation an der Bewegung
    /// (selbes SaveChanges).
    /// </summary>
    [RequireStockAccess]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Receive(int belId, List<PurchaseOrderReceiveInput>? zeilen, string? lieferschein = null, string? barcode = null)
    {
        zeilen ??= new();
        var vm = await BuildReceiveAsync(belId, zeilen);
        if (vm == null)
        {
            TempData["WarningMessage"] = "Diese Bestellung ist nicht (mehr) offen – nichts gebucht.";
            return RedirectToAction(nameof(Receive));
        }

        var erlaubtePlaetze = vm.Lagerplaetze.Select(l => l.Id).ToHashSet();
        var plaetze = vm.Lagerplaetze.ToDictionary(l => l.Id);
        var buchen = new List<(PurchaseOrderReceiveRow Zeile, decimal Menge)>();
        foreach (var z in vm.Zeilen.Where(z => !string.IsNullOrWhiteSpace(z.Eingabe.Menge)))
        {
            var e = z.Eingabe;
            var lesbar = BeschichtungsauftraegeController.TryMenge(e.Menge, out var menge);
            z.Fehler = z.NichtBuchbar
                ?? (!lesbar ? $"„{e.Menge}“ ist keine Zahl." : null)
                ?? (menge <= 0 ? "Menge muss größer als 0 sein." : null)
                ?? (decimal.Round(menge, 3) != menge ? "Höchstens 3 Nachkommastellen (Lagerbewegung speichert 3)." : null)
                ?? (e.StorageLocationId is not { } sid || !erlaubtePlaetze.Contains(sid) ? "Bitte einen Ziel-Lagerplatz wählen." : null)
                // v1.99.4 (F2, Freigabe-Antwort 3): bei Sage-WE nur Sage-Plaetze mit Sage-Buchung, auch fuer NurInSage-Zeilen
                ?? (vm.SageWeAktiv && !SageWareneingang.IstSageWePlatz(plaetze[e.StorageLocationId!.Value]) ? SageWareneingang.PlatzFehler(plaetze[e.StorageLocationId.Value].Code) : null)
                ?? (menge > z.Rest && !e.UeberlieferungBestaetigt ? $"Überlieferung ({menge:0.###} > Rest {z.Rest:0.###}) bitte bestätigen." : null)
                ?? (e.Memo?.Trim() is { Length: > PurchaseOrderReceiveViewModel.MemoMaxLaenge } ? "Positionsmemo höchstens 200 Zeichen." : null);
            buchen.Add((z, menge));
        }

        // v1.99.4 (F6): Lieferschein-Nr. des Lieferanten, getrimmt, hoechstens 20 Zeichen (Sage EKLieferschein); Pflicht nur per AppSetting
        vm.Lieferschein = string.IsNullOrWhiteSpace(lieferschein) ? null : lieferschein.Trim();
        if (vm.Lieferschein is { Length: > PurchaseOrderReceiveViewModel.LieferscheinMaxLaenge })
            ModelState.AddModelError(string.Empty, "Lieferschein-Nr. höchstens 20 Zeichen (Sage-Feld EKLieferschein).");
        if (vm.Lieferschein == null && vm.LieferscheinPflicht)
            ModelState.AddModelError(string.Empty, "Bitte die Lieferschein-Nummer des Lieferanten eingeben.");

        // v1.99.11 (Wareneingang-CSV F1): Barcode des Lieferschein-Aufklebers, nur bei eingeschaltetem Feld (sonst ignoriert). Getrimmt, keine
        // Umformung (Aufkleber = Dateiname); im WMS nur an einem Absenden (ohne Gross/Klein, Windows-Dateinamen).
        vm.Barcode = !vm.BarcodeAktiv || string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
        if (vm.Barcode != null)
        {
            if (!vm.SageWeAktiv)
                ModelState.AddModelError(string.Empty, vm.AbrufOhneSageWe ? "Barcode nicht möglich: " + SageWareneingang.AbrufOhneSageWeText
                    : "Barcode nicht möglich: Der Sage-Wareneingang ist aus – für diese Buchung entsteht keine CSV-Datei.");
            else if (!WareneingangCsv.IstGueltigerBarcode(vm.Barcode))
                ModelState.AddModelError(string.Empty, WareneingangCsv.BarcodeFehlerText);
            else if (await _receipts.BarcodeVergebenAsync(vm.Barcode) is { } vergeben)
                ModelState.AddModelError(string.Empty,
                    $"Barcode {vm.Barcode} ist bereits dem Wareneingang zu Bestellung {vergeben.Belegnummer} vom {vergeben.CreatedAt:dd.MM.yyyy HH:mm} zugeordnet.");
        }

        if (buchen.Count == 0)
            ModelState.AddModelError(string.Empty, "Keine Menge eingegeben – nichts gebucht.");
        if (buchen.Any(b => b.Zeile.Fehler != null))
            ModelState.AddModelError(string.Empty, "Nichts gebucht: bitte die markierten Zeilen korrigieren.");
        if (!ModelState.IsValid) return View(vm);

        var now = DateTime.Now;
        var userId = _user.GetCurrentAppUserId();
        var name = _user.GetDisplayName();
        var windows = _user.GetWindowsUserName();
        int teil = 0, ueber = 0;
        // v1.99.4 (F2): Entscheidung beim Buchen - EIN Vorgang (= ein ELL-Beleg) je Absenden; spaeteres Umschalten aendert nichts mehr.
        var vorgang = vm.SageWeAktiv ? Guid.NewGuid() : (Guid?)null;
        foreach (var (z, menge) in buchen)
        {
            var l = z.Row.Line;
            var art = menge == z.Rest ? PurchaseOrderReceiptLieferart.Vollstaendig
                : menge < z.Rest ? PurchaseOrderReceiptLieferart.Teillieferung : PurchaseOrderReceiptLieferart.Ueberlieferung;
            if (art == PurchaseOrderReceiptLieferart.Teillieferung) teil++;
            if (art == PurchaseOrderReceiptLieferart.Ueberlieferung) ueber++;
            var movement = new StockMovement
            {
                ArticleId = l.ArticleId!.Value,
                Quantity = menge,
                StorageLocationId = z.Eingabe.StorageLocationId!.Value,
                MovementType = MovementType.Einbuchung,
                Timestamp = now,
                UserId = userId,
                WindowsUser = windows,
                CreatedAt = now,
                CreatedBy = name,
                CreatedByWindows = windows,
                Note = string.IsNullOrWhiteSpace(z.Eingabe.Memo) ? null : z.Eingabe.Memo.Trim(),   // v1.99.6 Positionsmemo
                PurchaseOrderReceipt = new PurchaseOrderReceipt
                {
                    BelId = l.BelId, BelPosId = l.BelPosId, Belegnummer = l.Belegnummer, LieferantKto = l.LieferantKto, Quantity = menge,
                    OffenLautSageBeimBuchen = l.MengeOffen, SagePlatzId = l.SagePlatzId, Lieferart = art,
                    Lieferschein = vm.Lieferschein,
                    Barcode = vm.Barcode, LieferantName = l.LieferantName,   // v1.99.11 Wareneingang-CSV
                    SageWeStatus = !vm.SageWeAktiv ? SageWareneingangStatus.NichtNoetig
                        : l.Chargenpflicht || l.Nachweispflicht ? SageWareneingangStatus.NurInSage : SageWareneingangStatus.Offen,
                    SageWeVorgang = vm.SageWeAktiv && !(l.Chargenpflicht || l.Nachweispflicht) ? vorgang : null,
                    CreatedAt = now, CreatedBy = name, CreatedByWindows = windows,
                },
            };
            await _movements.AddAsync(movement);
        }

        TempData["SuccessMessage"] = $"{buchen.Count} Position(en) eingebucht (davon {teil} Teil-, {ueber} Überlieferung).";
        return RedirectToAction(nameof(Receive), new { belId });
    }

    /// <summary>Maske aus dem Cache (Server-Wahrheit) bauen; <paramref name="eingaben"/> = gepostete Werte (bleiben bei Fehlern stehen).</summary>
    private async Task<PurchaseOrderReceiveViewModel?> BuildReceiveAsync(int belId, List<PurchaseOrderReceiveInput>? eingaben)
    {
        var lines = await _lines.GetByBelIdAsync(belId);
        if (lines.Count == 0) return null;
        var rows = await ZeilenAsync(lines);
        var eingabe = (eingaben ?? new()).GroupBy(e => e.BelPosId).ToDictionary(g => g.Key, g => g.First());
        var k = rows[0].Line;
        var schalterAn = SageWareneingang.IstAn(await _serviceSettings.GetValueAsync(SageWareneingang.ToggleKey));
        return new PurchaseOrderReceiveViewModel
        {
            BelId = belId,
            Belegnummer = k.Belegnummer,
            LieferantText = rows[0].LieferantText,
            LieferantGesperrt = rows.Any(r => r.Line.LieferantGesperrt),
            Liefertermin = rows.Min(r => r.Line.Liefertermin),
            Lagerplaetze = await _locations.GetActiveOrderedExcludingPickingTransportAsync() ?? new(),
            // v1.99.15 (Teil 3, F4): alle Zeilen einer Maske haben dieselbe BelId, also dieselbe Belegart - Platzregel, Status beim Buchen
            // und Barcode-Sperre folgen daraus ohne weitere Aenderung.
            SageWeAktiv = SageWareneingang.GiltFuer(schalterAn, k),
            SageWeSchalterAn = schalterAn,
            Rahmenabruf = k.IstRahmenabruf,
            LieferscheinPflicht = string.Equals((await _settings.GetValueAsync(AppSettingKeys.WareneingangLieferscheinPflicht))?.Trim(), "true",
                StringComparison.OrdinalIgnoreCase),
            BarcodeAktiv = string.Equals((await _settings.GetValueAsync(AppSettingKeys.WareneingangBarcodeAktiv))?.Trim(), "true",
                StringComparison.OrdinalIgnoreCase),
            Zeilen = rows.Select(r => new PurchaseOrderReceiveRow
            {
                Row = r,
                Eingabe = eingabe.TryGetValue(r.Line.BelPosId, out var e) ? e
                    : new PurchaseOrderReceiveInput { BelPosId = r.Line.BelPosId, StorageLocationId = r.Line.StorageLocationId },
            }).ToList(),
        };
    }
}
