---
type: changelog
version: 1.12.0
date: 2026-05-12
---
# v1.12.0 — Leitstand-Kommissionierliste extrahiert (Split Phase 2)

Zwei spezialisierte Sichten statt einer Mehrzweck-Liste:

- **Fertigungsauftraege** — schlanke FA-Uebersicht fuer Picker, Tracker und Leitstand: nur
  Sage-Master-Daten, Werkbank und FA-Abschluss-Status, **ohne** Komm-Status-Spalten.
- **Leitstand** (neu) — die reiche Liste mit allen Status-Spalten (Glas/Zukauf/VK-VA),
  Bulk-Freigabe und Picker-Zuweisung. Sichtbar bei `LeitstandAktiv` fuer die Rollen `picking`
  oder `leitstand`.
- **Performance:** der schlanke Index laedt schneller, weil das Status-Pivot fuer User entfaellt,
  die es nicht brauchen.
- **Backward-Compat:** die alten Routen `/ProductionOrders/ToggleRelease|BulkRelease|SetPriority|ChangeAssignedPicker`
  antworten mit **301-Redirect** auf die neuen `/PickingLeitstand/...`-Endpoints — Bookmarks und
  alte Browser-Tabs laufen weiter.
