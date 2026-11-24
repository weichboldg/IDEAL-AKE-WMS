---
type: changelog
version: 1.99.62
date: 2026-11-24
---
# v1.99.62 — Alle Tabellen: Streifen, Hover CI-Hellblau, Fokus-Zeile

Spec [[2026-11-23-tabellen-streifen-hover-ci-hellblau-spec]], Lauf [[2026-11-24-tabellen-zeilenfarben-goal-lauf]], ADR [[0035-tabellen-zeilenfarben-global-ueber-bootstrap-zustandsvariablen]].
Commit `7fbdd2b7`. Keine Migration, kein SQL, nur Web (Version in beiden `AppVersion.cs`). TS-187 (TS-184.4 überholt). Nicht gemergt.

## Kernpunkte

- `site.css`: globale Regeln `.table:not(.table-plain) > tbody …` – Streifen, Hover `--ake-light-blue` (auch Touch), Fokus-Zeile `--wms-table-focus-bg` + Balken
  (nur Wertfelder; Kästchen nur Tastatur), Links `--bs-primary-text-emphasis`, `text-*` → `-text-emphasis`, Vault #1C6EA4 / enaio #A35400 voll deckend,
  `@media print` ohne Farbflächen. Alte Regeln `.table tbody tr:hover` und `.we-positionen` entfernt (Klasse auch aus `Receive.cshtml`).
- Ausnahme `table-plain`: `#faTree`, `fa-head-table`, `#trackingTree`, `#oseonTree`, Artikelinfo-Schlüssel-Wert-Tabelle.
- Wächter `TabellenZeilenfarbeTests` (4), `ViewKeyDriftGuardTests.WareneingangPositionen_*` angepasst.

## Deploy-Risiken

- Rein optisch, aber überall: Sichtprüfung TS-187 auf beiden Standorten vor Abnahme. Browser-Cache unkritisch (`asp-append-version`).
- Terminals ohne `:has()`-fähigen Browser: nur keine Fokus-Zeile (Freigabe-Antwort 8: Geräteliste im UAT prüfen).
