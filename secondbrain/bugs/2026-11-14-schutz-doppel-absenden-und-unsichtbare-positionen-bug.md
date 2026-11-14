---
type: bug
title: "Doppel-Absenden bucht doppelt / storniert, Unterpositionen im KO-Auftrag unsichtbar, CSV-Zahlen abhängig von der Server-Kultur"
status: offen
severity: hoch
created: 2026-11-14
standort: beide
affected_code:
  - "IdealAkeWms/Services/Nummernkreis.cs:15 (IstDeadlock nur eine Ebene), :28"
  - "IdealAkeWms/Services/PickingOrderWorkflow.cs:162-168 (Statusprüfung nur vor dem Versuch), :170-174 (Neulesen ohne Prüfung)"
  - "IdealAkeWms/Controllers/PickingLeitstandController.cs:395 (newReleased = !ps.IsReleasedForPicking), :417-420"
  - "IdealAkeWms/Controllers/PurchaseOrdersController.Receive.cs:42-146 (POST Receive ohne Vorgangs-Kennung), :91 (Barcode-Prüfung ohne Sperre), :108"
  - "IdealAkeWms/Services/BdeBookingService.cs:290-292 (Lesen-dann-Schreiben ohne Sperre), :571-581"
  - "IdealAkeWms/Views/Picking/Bom.cshtml:223-238 (Vater-Position aus dem Positionstext, display:none bei TreeLevel > 0), :953-954"
  - "IdealAkeWms/wwwroot/js/csv-export.js:16, :20-25"
  - "IdealAkeWms/Program.cs (keine feste Kultur)"
source_backlog: "[[2026-11-14-review-befunde-kommissionierung]]"
weitere_quellen:
  - "[[2026-11-14-review-befunde-wareneingang]]"
  - "[[2026-11-14-review-befunde-bde]]"
  - "[[2026-11-14-review-befunde-tabellen]]"
  - "[[01-kommissionierung]]"
  - "[[02-wareneingang-einkauf]]"
  - "[[03-ideal-hierarchie-beschichtung-bde]]"
  - "[[04-uebrige-module]]"
spec: "[[2026-11-14-schutz-doppel-absenden-und-unsichtbare-positionen-spec]]"
---

# Doppel-Absenden und unsichtbare Positionen (Sammel-Bug)

Sammel-Datensatz für sieben Befunde mit gemeinsamer Ursache „Schreib-POST prüft den Ausgangszustand nicht unter Sperre /
Client sperrt nicht" plus zwei Anzeige-Befunde. Gemessen in der DEV-Regression vom 2026-11-13 (Bündel v1.99.22, 90bbbbd7).

## Symptom

| ID | Symptom | Schwere |
|---|---|---|
| K-1 + N-2 | Paralleler Abschluss „Teilweise“: einer HTTP 500 (Deadlock), wird der Deadlock wiederholt, entsteht ein zweiter Folgeauftrag mit demselben Rest | hoch |
| N-2 | Parallele Freigabe: HTTP 500, FA freigegeben **ohne** Kommissionierauftrag | mittel |
| K-6 | Zweiter Klick auf „Freigeben“ bzw. veraltete Leitstand-Seite **storniert** die gerade angelegten Aufträge und löscht den Kommissionierer | mittel |
| WE-1 / WE-4 | Zwei gleiche POSTs „Einbuchen“ → zwei Wareneingänge, zwei Bewegungen; mit Sage-WE zwei ELL-Belege; gleicher Barcode zweimal | hoch |
| BDE-1 | Zwei gleichzeitige AG-Fertigmeldungen → zwei `Finished`-Buchungen mit `IsFinal`-Menge, Gutmenge doppelt | mittel |
| N-1 | KO-Auftrag: Unterpositionen, deren Vater-Zeile nicht im Auftrag ist, sind dauerhaft unsichtbar (IDEAL 73 von 79 Zeilen) | hoch |
| N-B4-2 | CSV: `-10 026,000` wird als Text `'-10 026,000` exportiert, Zahlen ab 1.000 sind in Excel keine Zahl | mittel |

## Reproduktion

- K-1/N-2: AKE-DEV KO-2026-00021, zwei gleichzeitige `POST /Picking/AuftragAbschliessen vollstaendig=false` → 1× 200, 1× 500
  (Log `idealakewms-20261113_004.log` 19:58:28). Freigabe: TS-120.10, 3 von 4 Läufen ein 500er ([[01-kommissionierung]]).
- K-6: AKE-DEV FA 2629001, zweimal `POST /PickingLeitstand/ToggleRelease` → „3 … angelegt“, dann „3 … storniert“.
- WE-1: AKE-DEV Bestellung 2027-168321 Pos. 402142, zwei gleichzeitige `POST /PurchaseOrders/Receive` mit 3 Stk → Receipts 14/15,
  Bewegungen 19534/19535 ([[02-wareneingang-einkauf]]). Echter Doppelklick im Desktop-Chrome: nur ein POST.
- BDE-1: IDEAL-DEV Werkbank 65, zwei gleichzeitige `POST /BdeTerminal/ReportCompletion` → in 4 von 6 Läufen zwei Fertigmeldungen
  ([[03-ideal-hierarchie-beschichtung-bde]]).
- N-1: IDEAL-DEV KO-2026-00005 (79 Positionen, 6 sichtbar), AKE-DEV KO-2026-00048 (2 von 2 unsichtbar), KO-2026-00022 (8 von 12).
- N-B4-2: AKE-DEV `/StockOverview`, Artikel 00000905, CSV-Export ([[04-uebrige-module]]).

## Root Cause

- **N-2:** `Nummernkreis.IstDeadlock` prüft nur `ex` und `ex.InnerException` (`Nummernkreis.cs:15`); EF verpackt den Deadlock als
  `InvalidOperationException` → `DbUpdateException` → `SqlException 1205`, der Catch-Filter `:28` greift nicht.
- **K-1:** `AbschliessenAsync` prüft den Status nur vor `MitVersuchenAsync` (`PickingOrderWorkflow.cs:164`), der Wiederholversuch liest
  neu (`:174`), prüft aber nicht.
- **K-6:** `ToggleRelease` kehrt den gespeicherten Zustand um (`PickingLeitstandController.cs:395`) statt die Absicht des Klicks auszuführen.
- **WE-1/WE-4:** Der POST hat keine Kennung des Absendens; jeder Aufruf bucht neu (`PurchaseOrdersController.Receive.cs:108-141`),
  die Barcode-Prüfung `:91` ist Lesen-dann-Schreiben.
- **BDE-1:** `AnyAsync(IsFinal …)` dann Insert in einer ReadCommitted-Transaktion ohne Sperre (`BdeBookingService.cs:290-323`, `:577`);
  der UNIQUE-Index `IX_BdeBookingQuantities_Booking_Final` gilt je Buchung, nicht je Arbeitsgang.
- **Alle Doppel-Fälle clientseitig:** kein Submit-Schutz in `site.js` (Grep `submit` ohne Treffer), `.set-status-btn`
  (`Bom.cshtml:1668-1686`) und `btnQtyOk` (`bde-terminal.js:466-472`) sperren nicht.
- **N-1:** Die Vater-Position wird aus dem Positionstext abgeleitet (`Bom.cshtml:224-227`), die Zeile startet versteckt (`:238`) und wird nur
  sichtbar, wenn die Vater-Zeile aufgeklappt ist (`:954`). Im Auftragsmodus fehlen Vater-Zeilen (Filter `PickingController.cs:291-293`).
- **N-B4-2:** `Program.cs` setzt keine Kultur (Grep `DefaultThreadCurrent|RequestLocalization`: kein Treffer); unter ICU-`de-AT` ist der
  Tausendertrenner U+00A0, `PLAIN_NUMBER` (`csv-export.js:16`) kennt nur den Punkt.

## Fix / Verweis

[[2026-11-14-schutz-doppel-absenden-und-unsichtbare-positionen-spec]]
