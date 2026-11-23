---
type: changelog
version: 1.99.61
date: 2026-11-23
---
# v1.99.61 — FA-Struktur: Titel ohne Standort, nur bei hierarchischer Struktur

Wunsch Gerald 2026-11-23 („FA-Struktur (IDEAL) nur auf FA-Struktur; wenn das Feature deaktiviert ist, soll es auch nicht angezeigt werden“), ohne eigene Spec.
Lauf [[2026-11-23-goal-lauf-mengenfelder-tabellen-beschichtung]]. Commits `22c118ff` + `cf3968bd` (Controller-Gate nach QA B-1). Keine Migration, nur Web. TS-186.

## Kernpunkte

- Titel/Überschrift, Spaltenauswahl-Name (`ColumnDefinitions`) und Hilfe: „FA-Struktur“.
- „Feature“ = Master `ProduktionsauftragHierarchisch` (ServiceSetting) – Entscheidung Gerald 2026-11-23 auf QA-Rückfrage H-1 („Nur Master“), passend zu `_FaLink`/`StockApiController`, die schon am Master hängen. Nicht der Import-Schalter `Sync:HierarchicalFaEnabled`.
- `NavigationSicht`: `"FaStruktur" => faLesen && hierarchisch` (Menü + Kachel); Master-Vergleich mit `Trim()` wie `FaBuchungsText.IstHierarchisch`.
- Neuer Filter `RequireHierarchischeStrukturAktiv` am `FaHierarchyController` (ADR 0011, kumulativ zum Rollen-Filter): aus → Startseite mit WarningMessage, API 404.
- ServiceSettings ohne Cache-Decorator: Umschalten wirkt ab dem nächsten Seitenaufruf.

## Deploy

Nur Web. Auf AKE (Master aus) verschwindet FA-Struktur aus Menü/Startseite – war dort ohnehin leer.
