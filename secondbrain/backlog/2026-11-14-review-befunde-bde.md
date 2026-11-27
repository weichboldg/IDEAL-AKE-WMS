---
type: backlog
created: 2026-11-14
quelle: "[[2026-11-14-wochenend-review-test-anwenderdoku]]"
status: neu
modul: BDE im Bündel (v1.63.0 Stufe 1 Kürzel-Familie/nur AG-Fertigmeldung, v1.64 Terminal-Liste, Hardware-Scan am BDE)
stand: "Bündel v1.99.22, 90bbbbd7, feature/2026-08-07-ideal-teile-1-5"
---
# Review-Befunde BDE (Teil A3, lesend)

Gegenstand: `git diff main...HEAD` im Bündel-Worktree, nur BDE-Anteile. Nichts behoben (Schranke 1). Bezug:
[[2026-10-19-bde-stufe-1-werkbaenke-ag-fertigmeldung-spec]].

## BDE-1 – Doppelte AG-Fertigmeldung bei Doppeltipp möglich
- **Schwere:** mittel
- **Fundstelle:** `IdealAkeWms/Services/BdeBookingService.cs:268` (`ReportCompletionAsync`), `:290` (Prüfung `AnyAsync(q => q.IsFinal …)`, dann
  Insert), `:568-580` (`InTransactionAsync` = Standard-Isolation ReadCommitted, mit RCSI Snapshot-Lesen); `IdealAkeWms/wwwroot/js/bde-terminal.js:466-472`
  (`btnQtyOk.onclick` ohne Sperre, Modal blendet nur aus)
- **Befund:** Die Sperre „Arbeitsgang ist bereits fertig gemeldet“ ist ein Lesen-dann-Schreiben ohne Sperre; der UNIQUE-Index `IX_BdeBookingQuantities_Booking_Final` (`SQL/00_FreshInstall.sql:2585`) gilt nur je Buchung, nicht je Arbeitsgang, und jede Anfrage legt eine neue Buchung an. Zwei gleichzeitige
  Anfragen (Doppeltipp auf „OK“ während der Ausblend-Animation am Touch-Terminal, oder zwei Terminals) sehen beide keine End-Menge und legen
  beide eine `Finished`-Buchung mit `IsFinal`-Menge an.
- **Szenario:** Werkbank mit „nur AG-Fertigmeldung“, Werker tippt zweimal schnell auf OK → zwei Fertigmeldungen, Gutmenge doppelt in
  Tagesverlauf und Auswertung.
- **Vorschlag:** OK-Knopf bis zur Antwort sperren (`disabled`) **und** serverseitig sperren (z. B. `sp_getapplock` je WorkOperationId oder
  `UPDLOCK, HOLDLOCK` auf die Prüfabfrage bzw. gefilterter UNIQUE-Index auf IsFinal je Arbeitsgang). Nicht InMemory-testbar → Manual-UAT.
- **DEV-Nachmessung 2026-11-14:** **reproduziert (serverseitig), am Desktop-Doppelklick nicht.** IDEAL-DEV, Werkbank 65 „Entgraten
  Fläche“ kurz auf „Nur AG-Fertigmeldung“, Test-Terminal 2. Echter Doppelklick auf „Bestätigen“ (Chrome, Maus): **eine** Buchung (Id 7,
  WO 540) – das Modal schließt vor dem zweiten Klick. Zwei gleichzeitige `POST /BdeTerminal/ReportCompletion` (Netz-Wiederholung, zwei
  Terminals, Touch-Doppeltipp): in **4 von 6** Läufen **zwei** `Finished`-Buchungen mit `IsFinal`-Menge für denselben Arbeitsgang
  (WO 526: Id 9/10, 529: 11/12, 532: 13/14, 538: 16/17; Abstand 0,04–0,08 ms; WO 524 und 536 je einmal „bereits fertig gemeldet“).
  Folge: Gutmenge 2 statt 1. Die Liste BDE-Buchungen zeigt die Doppel nur als gleichlautende Zeilen „1059463/EG“ (Sub-FA fehlt, siehe
  Nachtrag). Bild: ![[BDE-1-doppelte-ag-fertigmeldung.png]] (Ids 15–17: WO 536 einmal, WO 538 doppelt). Alle Testbuchungen 7–17
  storniert („QA Wochenend-Regression B3 Testbuchung“). Bericht [[03-ideal-hierarchie-beschichtung-bde]].

## BDE-2 – Unmaskierte Server-Texte in innerHTML (Altbestand, nicht im Bündel geändert)
- **Schwere:** Hinweis
- **Fundstelle:** `IdealAkeWms/wwwroot/js/bde-terminal.js:498` (`i.target`), `:532,534,547` (`op.label`, `a.label`), `:745` (`i.operationName`),
  `:934` (`o.operatorName`), `:973` (`b.operationName`)
- **Befund:** Diese Zeilen sind nicht Teil des Diffs; der neue Bündel-Code im selben File maskiert durchgängig (`escapeHtml`). Werte stammen aus
  Sage/Stammdaten (FA-Beschreibung, AG-Name, Bedienername) – ein `<` im Text zerstört die Darstellung, ein bösartiger Stammdatentext liefe als HTML.
- **Vorschlag:** Bei nächster Berührung `escapeHtml` nachziehen (eigene kleine Aufgabe).
- **DEV-Nachmessung 2026-11-14:** **bestätigt (statisch).** Die auf IDEAL-DEV ausgelieferte `D:\Dev\IDEAL\Web\wwwroot\js\bde-terminal.js`
  (11.11.2026) enthält die genannten Stellen unverändert unmaskiert (`:498` `i.target`, `:532/:534` `op.label`, `:547` `a.label`, `:934`
  `o.operatorName`, `:973` `b.operationName`). Kein Live-Versuch mit `<`-Stammdatentext (hätte Stammdaten auf DEV verändert).

## Geprüft ohne Befund
- Zugriffsschutz: `BdeTerminalController` (`RequireBdeActive` + `RequireBdeUserAccess`), `BdeBookingsController`/`BdeMasterDataController`
  (Schichtleiter lesen, Admin schreiben), `BdeApiController`; alle POSTs mit `ValidateAntiForgeryToken`.
- Audit: neue Buchungen über `SetAudit`, Statuswechsel über `SetAuditModified`, `DateTime.Now`.
- Hardware-Scanner am BDE: Passwortfelder ausgenommen (`hardware-scanner.js:228-231,411`), synthetische Ereignisse zählen nicht.

## Nachtrag aus der DEV-Regression (B3, 2026-11-14)
- **N-B3-5 (Hinweis):** Liste BDE-Buchungen, Spalte „Ziel“ zeigt bei IDEAL nur `HauptFA/AG` (z. B. `1059463/EG`), nicht die Sub-FA.
  Buchungen verschiedener Sub-FAs sehen gleich aus; eine doppelte Fertigmeldung (BDE-1) ist in der Oberfläche nicht erkennbar.
  Vorschlag: bei Master an `HauptFA / Sub-FA / AG` anzeigen. Beleg und Bild: [[03-ideal-hierarchie-beschichtung-bde]].
