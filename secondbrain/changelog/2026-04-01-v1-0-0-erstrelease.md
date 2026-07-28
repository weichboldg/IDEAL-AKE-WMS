---
type: changelog
version: 1.0.0
date: 2026-04-01
---
# v1.0.0 — Erstrelease mit Versionierung

Erste versionierte Fassung. Der Funktionsbestand davor (Lagerbewegungen, Stueckliste,
Kommissionierung, Stammdaten, OSEON-Teileverfolgung, RBAC, Windows-Service) ist in
`../../docs/PROJECT_STATUS-backup-2026-07.md` unter „Aenderungen (16.02.–30.03.2026)" dokumentiert.

- **Versionierung eingefuehrt:** `AppVersion.cs` in Web **und** Service, Version im Footer,
  Changelog-View unter `/Help/Changelog` — seither die anwendersichtbare Release-Wahrheit.
- **KW-Filter** in allen 5 Datumsspalten der FA-Liste (ISO 8601), mit Kalender-Popup: Klick auf KW
  oder Tag filtert.
- **enaio DMS-Integration:** Entity + Sync-Service + orange Link-Icons neben den FA-Nummern,
  Connection String `EnaioDmsConnection`.
- Bestandsuebersicht: FA-Filter (Netto-Bestand je Artikel+Lagerplatz) und QR-Scan fuer Artikel + FA.
- **Responsive Design:** Mobile-First-CSS, Touch-Targets 44 px, Sticky-Scrollbar, Navbar-User im
  Hamburger-Menue, `flex-wrap` auf Page-Headern.
- **Kommissionierwagen-Filterung** eingefuehrt: Wagen zaehlen nicht als Bestandsort — nicht in
  Stuecklisten-Bestand, Dropdowns oder Meldebestand-Farbcodierung.
- Bugfixes enaio: `object1.id` ist `int` (→ `Convert.ToInt64`), fehlende Audit-Felder im MERGE,
  Whitespace-Unterstrich bei mehreren Dokumenten.
