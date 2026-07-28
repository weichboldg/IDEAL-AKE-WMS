---
type: changelog
version: 1.24.0
date: 2026-06-30
---
# v1.24.0 — FA-Vorbau 3-Wert-Status, Beschichtungstermin, ENTER-Spaltenfilter

- **FA-Vorbau-Erledigt wird 3-wertig:** `FaWorkStep.IsCompleted` (bool) → `Status`
  (`FaWorkStepStatus` Offen/InBearbeitung/Fertig), als Auswahlfeld in der Abarbeitungsliste **und**
  im Leitstand (VK-VA). Nur `Fertig` blendet eine FA aus. Neue API
  `/api/fa-work-steps/set-status`; `toggle-completed` und die Erledigt-Checkboxen entfallen.
- **Migration 76** ist **daten-konvertierend** und dropt die `IsCompleted`-Spalte; `Down()`
  verliert die Offen/InBearbeitung-Unterscheidung → **DB-Backup vor Deploy**.
- **Beschichtungstermin** als filterbare Spalte in der Abarbeitungsliste; Formel jetzt zentral in
  `CoatingDateCalculator.Compute(...)` (DRY mit dem Leitstand). Backward-Compat bei leerem
  `LackierteilKategorieName` unveraendert.
- **Server-Mode-Spaltenfilter erst bei ENTER** (kein Debounce beim Tippen); Kalender und
  „Filter entfernen" wirken sofort ueber `applyColumnFilterNow()`. Client-Mode bleibt live.
- Android-Fix: `enterkeyhint="search"` auf den Filter-Inputs — ohne das Attribut springt die
  Soft-Tastatur nur ins naechste Feld statt ENTER zu feuern.
