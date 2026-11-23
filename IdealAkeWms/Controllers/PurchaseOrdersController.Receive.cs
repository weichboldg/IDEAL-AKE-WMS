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
            if (ids.Count == 1) return RedirectToAction(nameof(Receive), new { belId = ids[0].BelId });
            // v1.99.52 (F1): mehrere Treffer = alle Belegnummern nennen; verschiedene Nummern (Endungssuche) -> mit Jahr eingeben,
            // gleiche Nummer an mehreren Belegen -> ueber die Liste waehlen.
            TempData["WarningMessage"] = ids.Count == 0
                ? $"Bestellung „{belegnummer.Trim()}“ nicht gefunden (nur offene Bestellungen und Rahmenabrufe aus Sage, Stand des letzten Abgleichs)."
                : $"„{belegnummer.Trim()}“ passt zu {ids.Count} Bestellungen: {string.Join(", ", ids.Select(i => i.Belegnummer))} – "
                  + (ids.Select(i => i.Belegnummer).Distinct().Count() > 1 ? "bitte mit Jahr eingeben." : "bitte über „Offene Bestellungen“ wählen.");
            return RedirectToAction(nameof(Receive));
        }
        if (belId == null) return View(new PurchaseOrderReceiveViewModel());

        var vm = await BuildReceiveAsync(belId.Value, null);
        if (vm == null)
        {
            TempData["WarningMessage"] = "Diese Bestellung ist nicht (mehr) offen – Sage hat sie geschlossen oder der Abgleich ist noch nicht gelaufen.";
            return RedirectToAction(nameof(Receive));
        }
        vm.Absendekennung = Guid.NewGuid();   // v1.99.23 (WE-1): Kennung dieses Absendens
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
    public async Task<IActionResult> Receive(int belId, List<PurchaseOrderReceiveInput>? zeilen, string? lieferschein = null, string? barcode = null,
        Guid? absendekennung = null)
    {
        zeilen ??= new();
        var vm = await BuildReceiveAsync(belId, zeilen);
        if (vm == null)
        {
            TempData["WarningMessage"] = "Diese Bestellung ist nicht (mehr) offen – nichts gebucht.";
            return RedirectToAction(nameof(Receive));
        }

        // v1.99.23 (WE-1, ADR 0026): Kennung des Absendens VOR der Zeilenpruefung – ein zweiter POST scheiterte sonst am kleineren Rest
        // („Ueberlieferung bestaetigen“) statt „bereits gebucht“ zu melden. Seite ohne Kennung (vor dem Update geoeffnet): ablehnen (Antwort 5).
        if (absendekennung is not { } kennung || kennung == Guid.Empty)
        {
            TempData["WarningMessage"] = SeiteVeraltetText;
            return RedirectToAction(nameof(Receive), new { belId });
        }
        if (await _receipts.AbsendekennungGebuchtAsync(kennung))
        {
            TempData["WarningMessage"] = BereitsGebuchtText;
            return RedirectToAction(nameof(Receive), new { belId });
        }
        vm.Absendekennung = kennung;

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
        // Umformung (Aufkleber = Dateiname). v1.99.33 Sammellieferschein (A1): ein weiterer Einsatz nur bei gleichem Lieferanten und gleicher,
        // nicht leerer Lieferschein-Nr. wie beim ERSTEN Wareneingang des Barcodes (ohne Gross/Klein); der Dienst nummeriert die CSV (_1, _2 …).
        string? sammelHinweis = null;
        vm.Barcode = !vm.BarcodeAktiv || string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
        if (vm.Barcode != null)
        {
            if (!vm.SageWeAktiv)
                ModelState.AddModelError(string.Empty, vm.AbrufOhneSageWe ? "Barcode nicht möglich: " + SageWareneingang.AbrufOhneSageWeText
                    : "Barcode nicht möglich: Der Sage-Wareneingang ist aus – für diese Buchung entsteht keine CSV-Datei.");
            else if (!WareneingangCsv.IstGueltigerBarcode(vm.Barcode))
                ModelState.AddModelError(string.Empty, WareneingangCsv.BarcodeFehlerText);
            else if (await _receipts.BarcodeVergebenAsync(vm.Barcode) is { } vergeben)
            {
                var grund = SammellieferscheinGrund(vergeben, vm.Zeilen[0].Row.Line.LieferantKto, vm.Lieferschein);
                if (grund == null)
                    sammelHinweis = $" Sammellieferschein – Barcode {vm.Barcode} bereits bei Bestellung {vergeben.Belegnummer} verwendet; die CSV-Datei erhält eine laufende Nummer (_1, _2 …).";
                else
                    ModelState.AddModelError(string.Empty,
                        $"Barcode {vm.Barcode} ist bereits dem Wareneingang zu Bestellung {vergeben.Belegnummer} vom {vergeben.CreatedAt:dd.MM.yyyy HH:mm} zugeordnet – {grund}");
            }
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
                    Absendekennung = kennung,
                    CreatedAt = now, CreatedBy = name, CreatedByWindows = windows,
                },
            };
            try
            {
                await _movements.AddAsync(movement);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (Nummernkreis.IstSqlFehler(ex, 2601, 2627))
            {
                // Gleichstand: ein paralleler POST derselben Seite hat diese Position schon gebucht (UNIQUE Absendekennung+BelPosId, SQL/150).
                // Der Sage-Queue-Decorator reiht erst nach erfolgreichem AddAsync ein – die abgewiesene Zeile erzeugt keinen Queue-Eintrag.
                TempData["WarningMessage"] = BereitsGebuchtText;
                return RedirectToAction(nameof(Receive), new { belId });
            }
        }

        TempData["SuccessMessage"] = $"{buchen.Count} Position(en) eingebucht (davon {teil} Teil-, {ueber} Überlieferung)." + sammelHinweis;
        return RedirectToAction(nameof(Receive), new { belId });
    }

    /// <summary>v1.99.33 (A1, Rueckfrage 3): null = Sammellieferschein erlaubt (gleicher Lieferant, gleiche nicht leere Lieferschein-Nr. wie
    /// beim ersten Wareneingang des Barcodes, getrimmt, ohne Gross/Klein; ein leerer Wert gilt als ungleich), sonst der Grund der Ablehnung.</summary>
    internal static string? SammellieferscheinGrund(PurchaseOrderReceipt erster, string? lieferantKto, string? lieferschein)
    {
        static bool Gleich(string? a, string? b) => !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b)
            && string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
        if (!Gleich(erster.LieferantKto, lieferantKto)) return "anderer Lieferant.";
        if (string.IsNullOrWhiteSpace(erster.Lieferschein)) return "dort ohne Lieferschein-Nr., kein Sammellieferschein möglich.";
        if (!Gleich(erster.Lieferschein, lieferschein))
            return $"für einen Sammellieferschein bitte dieselbe Lieferschein-Nr. eingeben (dort: {erster.Lieferschein.Trim()}).";
        return null;
    }

    internal const string SeiteVeraltetText = "Die Seite ist veraltet – bitte neu laden. Es wurde nichts gebucht.";
    internal const string BereitsGebuchtText = "Diese Eingabe wurde bereits gebucht – nichts doppelt gebucht. Bitte den Stand der Bestellung prüfen.";

    /// <summary>Maske aus dem Cache (Server-Wahrheit) bauen; <paramref name="eingaben"/> = gepostete Werte (bleiben bei Fehlern stehen).</summary>
    private async Task<PurchaseOrderReceiveViewModel?> BuildReceiveAsync(int belId, List<PurchaseOrderReceiveInput>? eingaben)
    {
        var lines = await _lines.GetByBelIdAsync(belId);
        if (lines.Count == 0) return null;
        var rows = await ZeilenAsync(lines);
        var eingabe = (eingaben ?? new()).GroupBy(e => e.BelPosId).ToDictionary(g => g.Key, g => g.First());
        var k = rows[0].Line;
        var schalterAn = SageWareneingang.IstAn(await _serviceSettings.GetValueAsync(SageWareneingang.ToggleKey));
        var (lager, meldungen) = await OffeneBedarfeAsync(rows.Select(r => r.Line.Artikelnummer));
        var lagerplaetze = await _locations.GetActiveOrderedExcludingPickingTransportAsync() ?? new();
        var sageWeAktiv = SageWareneingang.GiltFuer(schalterAn, k);
        var ziele = await ZielVorschlaegeAsync(rows, lagerplaetze, sageWeAktiv);
        return new PurchaseOrderReceiveViewModel
        {
            BelId = belId,
            Belegnummer = k.Belegnummer,
            LieferantText = rows[0].LieferantText,
            LieferantGesperrt = rows.Any(r => r.Line.LieferantGesperrt),
            Liefertermin = rows.Min(r => r.Line.Liefertermin),
            AbNummer = k.AbNummer, Kommission = k.Kommission,   // v1.99.33 Teil B: Kopfwert, an jeder Position gleich
            Lagerplaetze = lagerplaetze,
            // v1.99.15 (Teil 3, F4): alle Zeilen einer Maske haben dieselbe BelId, also dieselbe Belegart - Platzregel, Status beim Buchen
            // und Barcode-Sperre folgen daraus ohne weitere Aenderung.
            SageWeAktiv = sageWeAktiv,
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
                    : new PurchaseOrderReceiveInput { BelPosId = r.Line.BelPosId, StorageLocationId = ziele[r.Line.BelPosId].Platz },
                // v1.99.52 (F3): „Vorschlag“ nur, wenn die Vorbelegung wirklich aus dem Hauptlagerplatz kommt (GET), nie nach einem POST
                ZielVorschlagHauptlagerplatz = eingaben == null && ziele[r.Line.BelPosId].AusHauptlagerplatz,
                ZielHinweis = ziele[r.Line.BelPosId].Hinweis,
                OffeneLagerbestellungen = lager[Schluessel(r.Line.Artikelnummer)].ToList(),
                OffeneBedarfsmeldungen = meldungen[Schluessel(r.Line.Artikelnummer)].ToList(),
            }).ToList(),
        };
    }

    /// <summary>v1.99.52 (Spec 2026-11-23 F3): Vorbelegung des Ziel-Platzes je Position. 1. Sage-Platz der Position, wenn auswaehlbar
    /// (unveraendert); 2. sonst Hauptlagerplatz des Artikels (<c>Article.PrimaryStorageLocationId</c>, OHNE Bestandsbedingung – nicht die
    /// Kommissionier-Regel <c>QuellVorschlag</c>), wenn auswaehlbar und bei Sage-WE ein Sage-Platz mit Sage-Buchung; 3. sonst leer.
    /// Hinweis = warum kein Vorschlag moeglich war (Code des Hauptlagerplatzes) bzw. Position ganz ohne Sage-Platz – melden statt still leer.</summary>
    private async Task<Dictionary<int, (int? Platz, bool AusHauptlagerplatz, string? Hinweis)>> ZielVorschlaegeAsync(
        List<PurchaseOrderLineRow> rows, List<StorageLocation> lagerplaetze, bool sageWeAktiv)
    {
        var waehlbar = lagerplaetze.ToDictionary(p => p.Id);
        var offen = rows.Where(r => r.Line.ArticleId != null && !(r.Line.StorageLocationId is { } sid && waehlbar.ContainsKey(sid))).ToList();
        var haupt = offen.Count == 0 ? new Dictionary<int, int>()
            : await _articles.GetPrimaryStorageLocationIdsAsync(offen.Select(r => r.Line.ArticleId!.Value).Distinct().ToList()) ?? new();
        var codes = haupt.Count == 0 ? new Dictionary<int, string>()
            : (await _locations.GetAllOrderedAsync() ?? new()).ToDictionary(p => p.Id, p => p.Code);
        var ergebnis = rows.ToDictionary(r => r.Line.BelPosId, r => (Platz: r.Line.StorageLocationId, AusHauptlagerplatz: false, Hinweis: (string?)null));
        foreach (var r in offen)
        {
            var keinSagePlatz = r.Line.SagePlatzId == null && r.Line.StorageLocationId == null;
            if (!haupt.TryGetValue(r.Line.ArticleId!.Value, out var hid))
            {
                if (keinSagePlatz) ergebnis[r.Line.BelPosId] = (null, false, "Kein Sage-Platz an der Position – bitte Ziel wählen");
                continue;
            }
            var code = codes.GetValueOrDefault(hid, $"#{hid}");
            ergebnis[r.Line.BelPosId] = !waehlbar.TryGetValue(hid, out var platz)
                    ? (null, false, $"Hauptlagerplatz {code} ist im WMS nicht buchbar – bitte Ziel wählen")
                : sageWeAktiv && !SageWareneingang.IstSageWePlatz(platz)
                    ? (null, false, $"Hauptlagerplatz {code} hat keine Sage-Buchung – bitte Sage-Platz wählen")
                : (hid, true, null);
        }
        return ergebnis;
    }

    private static string Schluessel(string? artikelnummer) => (artikelnummer ?? "").Trim().ToUpperInvariant();

    /// <summary>v1.99.22 (Freigabe-Nachtrag 3, Backlog 2026-11-12 Punkt 2): offene Lagerbestellungen (hinter <c>LagerbestellungAktiv</c>, nur
    /// „false“ sperrt) und offene Bedarfsmeldungen (hinter <c>BestellungenAktiv</c>, nur „true“) zu den Artikeln der Bestellung – dieselben
    /// Abfragen wie die Bestellvorschläge (<see cref="IPurchaseProposalRepository"/>), nur Anzeige.</summary>
    private async Task<(ILookup<string, OffenerBedarf> Lager, ILookup<string, OffenerBedarf> Meldungen)> OffeneBedarfeAsync(IEnumerable<string?> artikel)
    {
        var nummern = artikel.Select(Schluessel).ToHashSet();
        var lagerAn = !string.Equals(await _settings.GetValueAsync(AppSettingKeys.LagerbestellungAktiv), "false", StringComparison.OrdinalIgnoreCase);
        var meldungenAn = string.Equals(await _settings.GetValueAsync(AppSettingKeys.BestellungenAktiv), "true", StringComparison.OrdinalIgnoreCase);
        var lager = lagerAn ? await _bedarfe.GetOffeneLagerbestellungenAsync() ?? new() : new List<OffenerBedarf>();
        var meldungen = meldungenAn ? await _bedarfe.GetOffeneBedarfsmeldungenAsync() ?? new() : new List<OffenerBedarf>();
        return (lager.Where(b => nummern.Contains(Schluessel(b.ArticleNumber))).ToLookup(b => Schluessel(b.ArticleNumber)),
                meldungen.Where(b => nummern.Contains(Schluessel(b.ArticleNumber))).ToLookup(b => Schluessel(b.ArticleNumber)));
    }
}
