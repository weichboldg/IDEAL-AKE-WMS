---
type: changelog
version: 1.8.1
date: 2026-04-16
---
# v1.8.1 — BDE-Verbesserungen + enaio-Fix

- **BDE-Terminal:** getrennte Scan-Felder, Operator-Badge, AG-Buttons (produktiv/ungeplant),
  Toast-Bestaetigung, Live-Timer, Tageshistorie.
- **Cockpit:** mehrere Operatoren pro Werkbank werden korrekt angezeigt.
- **Buchungsuebersicht:** Spaltenfilter, KW-Anzeige, Standardfilter auf heute.
- **enaio DMS-Sync: Delta-Filter entfernt** — die `angelegt`-Spalte in enaio ist statisch
  (Bulk-Import 2013), ein Delta haette nach dem ersten Lauf nie wieder etwas gefunden. Jetzt
  Full-Sync mit MERGE gegen Duplikate.
