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

## BDE-2 – Unmaskierte Server-Texte in innerHTML (Altbestand, nicht im Bündel geändert)
- **Schwere:** Hinweis
- **Fundstelle:** `IdealAkeWms/wwwroot/js/bde-terminal.js:498` (`i.target`), `:532,534,547` (`op.label`, `a.label`), `:745` (`i.operationName`),
  `:934` (`o.operatorName`), `:973` (`b.operationName`)
- **Befund:** Diese Zeilen sind nicht Teil des Diffs; der neue Bündel-Code im selben File maskiert durchgängig (`escapeHtml`). Werte stammen aus
  Sage/Stammdaten (FA-Beschreibung, AG-Name, Bedienername) – ein `<` im Text zerstört die Darstellung, ein bösartiger Stammdatentext liefe als HTML.
- **Vorschlag:** Bei nächster Berührung `escapeHtml` nachziehen (eigene kleine Aufgabe).

## Geprüft ohne Befund
- Zugriffsschutz: `BdeTerminalController` (`RequireBdeActive` + `RequireBdeUserAccess`), `BdeBookingsController`/`BdeMasterDataController`
  (Schichtleiter lesen, Admin schreiben), `BdeApiController`; alle POSTs mit `ValidateAntiForgeryToken`.
- Audit: neue Buchungen über `SetAudit`, Statuswechsel über `SetAuditModified`, `DateTime.Now`.
- Hardware-Scanner am BDE: Passwortfelder ausgenommen (`hardware-scanner.js:228-231,411`), synthetische Ereignisse zählen nicht.
