---
type: aufgabe
status: offen
created: 2026-11-17
quelle: Gerald, Chat 2026-11-17 (/goal)
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-17 Abend: Nachbesserungen v1.99.39

Auftrag Gerald 2026-11-17: drei Nachbesserungen im Bündel-Worktree (Basis `ab75c336`, v1.99.38), gemeinsam als **v1.99.39** (geprüft: frei;
keine SQL; TS-159 erweitern bzw. TS-160 bei Bedarf).
Grenzen: nichts an Sage, kein Schalter einschalten, keine Einstellungen auf Test/Prod, Publish nur `<Worktree>\publish\paket4-v1.99.39\`
(Paket 1–3 unverändert), kein Merge, kein Push.

1. [[2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen-spec]] QA-Befund F3: Erfolg erst nach Nachlesen der angelegten AG-Position in Sage.
2. Manifest `scripts/deploy/nachlauf.psd1`: Hinweis/Prüfschritt AKE-Prod `Sync:Quelle:Artikel` auf SData – Befund
   [[2026-11-17-ake-4445-vs-prod-sichtpruefung]]; Betriebsdoku.
3. Pagination „Eintraege 26–8 von 8“ (QA-Zusatzfund Lauf [[2026-11-17-goal-lauf-sage-ag-anlage]]): Seite > letzte Seite → letzte Seite.

## Übersicht

| Punkt | Version | Status | Commit |
|---|---|---|---|
| 1 F3 Nachlesen nach Senden | 1.99.39 | Testbereit (QA 2026-11-17, Befund N1 offen) | `69a39a8b` |
| 2 Manifest Sync:Quelle:Artikel AKE | 1.99.39 | Testbereit (QA 2026-11-17) | `ebff14e0` + QA-Fix `2ae5d1da` |
| 3 Pagination klemmen | 1.99.39 | Testbereit (QA 2026-11-17) | `78014b9a` (+ Version/Doku `8d419875`) |

## Protokoll

- 2026-11-17: Lauf gestartet, Nummer 1.99.39 geprüft.
- 2026-11-17: Umsetzung v1.99.39 ([[2026-11-17-v1-99-39-nachbesserung-abend]]). F3: `SageFaArbeitsgangService.NachlesenAsync` – nach 2xx GET über
  `ISageFaStandLookup`, angelegt nur bei Fund der AG-Nummer (`RessourceNummer`), sonst Fehler mit Text, Lesefehler = unklar; `Auswerten` ohne
  Handle-Pflicht. Manifest: `Pruefung/ArtikelQuelleSData` (AKE, Nachher, anzeigen; Key belegt `ServiceSettingDefinitions.cs:141,324`).
  Pagination: Ursache = Controller rechnen Skip/Take aus `page` vor der Trefferzahl, `PaginationState.cs:18-19` (`FirstItemNumber`) ungeklemmt;
  Fix = globaler Result-Filter `Filters/PaginationSeiteKlemmenFilter` (Redirect, nur GET), kein Controller angefasst. Build 0 Fehler;
  Web 3254/1 übersprungen, Dienst 769 grün. Kein Publish, kein Merge, nichts an Sage. Offen: qa-agent.

## QA-Nachweis (qa-agent, 2026-11-17)

Bereich `ab75c336..8d419875` + QA-Fix `2ae5d1da`. Review selbst im Lauf, kein Unteragent.

**1. Build und Tests (Worktree):** `dotnet build IdealAkeWms.slnx` 0 Fehler (29 Warnungen, Altbestand). `dotnet test`: IdealAkeWms.Tests
3254 bestanden / 1 übersprungen / 0 Fehler; IDEALAKEWMSService.Tests 769 / 0 / 0.

**2. Review.**
- Punkt 1 (F3, `69a39a8b`): Nachlesen direkt nach 2xx (`SageFaArbeitsgangService.cs:160`, `NachlesenAsync` :198-212), gleicher GET-Weg wie die
  Prüfung vor dem Senden (`SageFaArbeitsgaenge.LesenAsync`, kein Cache, `SdataFaStandLookup` scoped), Treffer `ArbeitsgangNummer` oder
  `RessourceNummer`; FA nicht gefunden = kein Fund; Lesefehler → `UnklarText`-Präfix → aus `kandidaten` ausgeschlossen (:74-79), Test
  `Nachlesen_Lesefehler_Unklar_NieAutomatischWiederholt` belegt es. **Befund N1 (Mittel, blockiert nur E1b):** „kein Fund nach 2xx“ = `Fehler`
  ohne Unklar-Präfix → automatisch wiederholt (Default 3); liest der Leser eine tatsächlich angelegte AG nicht (P-5), entstehen Dubletten.
  Offen, Entscheidung Gerald (Vorschlag in der Spec, Zeile N1).
- Punkt 2 (`ebff14e0`): SQL korrekt (fehlt/`View`/andere Schreibweise → 1, `SData` exakt → 0; Dienst liest den Wert ebenfalls exakt),
  Firma AKE, Phase Nachher, nur `anzeigen`, keine Umschaltung. **Befund N2 (Niedrig):** Fundstelle in `docs/betrieb/03-…:179` zeigte nur
  `nachlauf.psd1:138-169` → behoben `2ae5d1da` (`138-169,178-181`).
- Punkt 3 (`78014b9a`): nur GET, nur `ViewResult`/`PartialViewResult`; alle 57 `CurrentPage`-Zuweisungen kommen aus dem Parameter `page`
  (bzw. `Math.Max(page,1)` / geklemmtem `pagedResult.Page`) → keine Schleife möglich; alle `PaginationState`-Erzeugungen setzen `TotalCount`;
  Gruppen-Paging zählt Gruppen konsistent; keine AJAX-Aufrufe mit `page` auf View-Actions (CSV-Export setzt `page=1`); JSON unberührt;
  Redirect-Ziel = eigener Pfad (kein Open Redirect). Keine Befunde.
- Punkt 4 (`8d419875`): beide `AppVersion.cs` 1.99.39, Changelog, TS-159-Nachtrag, TS-160, Index/feature-map nachgezogen. Keine Befunde.
- Geänderte Bestandstests: `Auswerten_*` (Verhalten absichtlich geändert, F3) und Fake-Antwort `Anlegen` als Standard bzw. im Wiederhol-Test
  (nötig, weil „angelegt“ jetzt den Fund beim Nachlesen verlangt; F3). Keine „sonstige“ Anpassung.
- Keine SQL/Migration, keine neue Einstellung (ADR 0020 n/a).

**3. Nachweisart:** F3 automatisch (Fake-Client/-Lookup); echtes Sage-Verhalten (TS-159.x6/x7) Manual-UAT am IDEAL-Ziel (E1b). Pagination:
Filter automatisch, echte Listen per Browser/HTTP auf DEV (unten). Manifest: SQL gegen DEV-DB.

**6. DEV.** Zielordner `D:\Dev\{AKE,IDEAL}\{Web,Service}` vor dem Deploy geleert (erhalten: `appsettings.DEV.json*`, `logs`, `App_Data`), dann
`dev-deploy.ps1 -Quelle <Worktree>` je Standort, erhöht: AKE Exit 0, Fußzeile v1.99.39; IDEAL Exit 0, Fußzeile v1.99.39; beide Dienste laufen,
keine ERR/FTL im Dienst-Log. Prüfung `Pruefung/ArtikelQuelleSData` (SQL aus dem Manifest per sqlcmd): AKE_WMS_DEV 0 Treffer (Wert `SData`),
IDEAL_WMS_DEV 0; Gegenproben `sdata`/Key fehlt → 1. `Update-IdealAkeWms.ps1 -NurPruefen` selbst NICHT MÖGLICH auf DEV (Ziele nur Test/Prod,
Verbindung dorthin verboten) – SQL-gleichwertig geprüft.
TS-160 (Browser AKE + HTTP mit Windows-Anmeldung AKE/IDEAL):
- 160.1 OK: AKE `/SyncLog?service=SageWareneingang&colf_Message=e&page=2&pageSize=25` → `page=1`, „Eintraege 1–14 von 14“, alle Parameter erhalten.
- 160.2 OK: AKE `/SyncLog?service=WareneingangCsv&page=9` → `page=2`, „26–44 von 44“.
- 160.3 OK (gleicher Mechanismus, Filter + zu hohe Seite in 160.1).
- 160.4 OK: `colf_Message=zzzqqqxx&page=4` → `page=1`, „Keine Eintraege“.
- 160.5 OK: IDEAL `/Users?page=99` → `page=1` („1–10 von 10“); `/StorageLocations?page=99999` → AKE `page=132` („3276–3294 von 3294“), IDEAL `page=10`;
  `Creditors?page=99` → `page=91`; `Creditors/Details/547?page=9999` (AKE, zweite ViewBag-Liste) → `page=367` („9151–9153 von 9153“);
  IDEAL `/SageFaArbeitsgaenge?page=99` → `page=10` („226–227 von 227“, ein Redirect); ProductionOrders/FaHierarchy/SageBookingQueue `page` hoch → 1.
- 160.6 OK: gültige Seite (`WareneingangCsv&page=2`, AKE) 200 ohne Umleitung.
- 160.7 OK: `page=0`/`page=-3` (SyncLog, Users, StorageLocations) 200, keine Umleitung, Seite 1.
- Hinweis: „Alle“ (`pageSize=0`, Cap 5000) klemmt auf die letzte 5000er-Seite (`Articles?page=99999&pageSize=0` → `page=22`) – konsistent mit
  `PaginationState`, kein Befund.
DEV-Testdaten: keine angelegt/geändert (nur gelesen). Nichts an Sage, kein Schalter.

**7. Publish:** nicht durch QA (Auftrag: Gerald publiziert nach `publish\paket4-v1.99.39\`).

**Offen für den Menschen:** (1) Entscheidung N1; (2) TS-159.x6/x7 am IDEAL-Ziel mit E1b; (3) TS-160 kurz am Test-System; (4) AKE-Prod-Update:
`Pruefung/ArtikelQuelleSData` beachten, `Sync:Quelle:Artikel` von Hand; (5) Publish Paket 4.

