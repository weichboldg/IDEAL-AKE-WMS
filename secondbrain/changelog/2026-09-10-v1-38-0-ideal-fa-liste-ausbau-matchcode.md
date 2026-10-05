---
type: changelog
version: 1.38.0
date: 2026-09-10
---
# v1.38.0 — IDEAL: FA-Liste ausbauen — Zeilenwerte, HauptFA-Zeile, Freigabe-Kaskade, Matchcode

Umsetzung der freigegebenen Spec [[2026-09-10-fa-liste-ausbau-matchcode-spec]] im
**Buendel-**Worktree `feature/2026-08-07-ideal-teile-1-5`, aufbauend auf
[[2026-09-09-v1-37-0-ideal-materialisierung-fachliche-felder]] (v1.37.0, eigene, bereits
abgenommene Spec — nicht wieder aufgerissen). Umsetzungsnotiz
[[2026-09-10-fa-liste-ausbau-matchcode-umsetzung]], Plan
`docs/superpowers/plans/2026-09-10-fa-liste-ausbau-matchcode.md`.

**Warum:** Kopfdaten (Kunde, Termine, Prio, AB-Nummer, Montage-Abteilung) standen bisher
ausschliesslich in der Gruppen-Kopfzeile — das blockierte Spaltenfilter/Sortierung auf Zeilenebene.
Die HauptFA hatte kein eigenes Zeilen-Dasein. Eine Freigabe am HauptFA kaskadierte nicht auf die
Sub-FAs (anders als die Fertigmeldung). Der fachlich zentrale Matchcode fehlte ueberall ausser in
der FA-Struktur-Ansicht.

## Umgesetzt (vier fachlich unabhaengige Bloecke, ein Worktree)

- **Matchcode** (`461d3e5`, `cbabdd9`, `f4e9a6d`, `48e1c87`, `4a0563a`): neue Spalte
  `ProductionOrder.Matchcode` (`NVARCHAR(200)`), materialisiert im Anlege- UND Update-Pfad
  (F1-Muster, `FaMaterializationSyncService`), angezeigt in **fuenf** FA-Zeilen-Ebene-Listen
  (`ProductionOrders`, `PickingLeitstand`, `FaCompletion`, `FaWorklist`, `Picking`) — sichtbar per
  Default, server-seitiger Filter ohne `EF.Functions.Like` (InMemory-testbar). `OseonTracking`
  bewusst ausgeschlossen (eigener Baum-Mechanismus, kein `table-filter.js`-Pfad) und dokumentiert.
  Bei AKE bleibt die Spalte leer, bis die hausinterne Sage-View-Erweiterung liefert — kein Fehler
  (F7, null-sicherer Lesepfad).
- **Kopfdaten je Zeile** (`9b2a258`, `13693b7`, `6ec37f8`, `cddb5b0`, `a2c7861`, `20686a2`,
  `7b15d8a`, `9c95fb0`): Kunde/Prio/AB-Nummer/Montage-Abteilung/Konstruktions-Termin zusaetzlich
  als Zeilenwerte im Anzeige-Modell (nicht materialisiert, K1-Regel bleibt gewahrt), vier davon
  strukturell bedingt auf `Model.Hierarchical` (AKE bit-identisch). Kunde-Filter (Freitext +
  Spaltenfilter) laeuft hierarchisch jetzt ueber einen C#-Postfilter statt den SQL-Subquery-Join —
  betrifft `ProductionOrdersController` UND `PickingLeitstandController` (teilen `BuildLeitstandQuery`).
  Ein Fix unterwegs: der Postfilter hatte zunaechst den Kunde-Spaltenfilter **im AKE-Flachmodus**
  mitgebrochen und dort die Paginierung abgeschaltet (`cddb5b0`).
- **HauptFA-Zeile + schlankere Kopfzeile** (`0f14850`, eingeschobener Selektor-Sweep `b9bf342`,
  `94463f9`, `9dfce86`, `203e23a`, `a422e44`, `fda7c7a`): Die Wurzelzeile ist jetzt eine normale
  Zeile mit eigenem Badge „HauptFA" + `fw-semibold` (Erkennung `SubOrderNumber == OrderNumber`,
  kein neues ViewModel-Feld), **Schritt-1-Verifikationspflicht der Spec erfuellt** (Test fixiert die
  Modellannahme, Code-Beweiskette in der Aufgaben-Notiz). Gruppen-Kopfzeile zeigt nur noch
  HauptFA-Nummer/Sub-FA-Zahl/Kunde/Fert.-Termin/Mehrdeutig-Badge — Prio/AB-Nr./Montage-Abt./
  uebrige Termine wandern in die Zeilen. „Nur wo der Wert wechselt" ist client-seitig
  (`fa-liste-wiederholung.js`, neu), Wert bleibt im DOM (`color: transparent`, kein `display:none`).
  **Eingeschobener Sweep, nicht Teil der urspruenglichen Spec:** ein zweiter, persistenter
  Anzeigefehler gefunden — Tabellen-Selektoren griffen nicht skopiert in verschachtelte Tabellen
  (Kombigeraete-Varianten). Siehe [[2026-09-10-table-filter-selektoren-nicht-skopiert-bug]], behoben
  durch `:scope >`-Skopierung plus Quelltext-Waechter-Test (`TableScriptScopedSelectorTests`).
- **Freigabe-Kaskade am Leitstand** (`067f1b1`, `aad4b54`, `fef12cf`): neue Aktion
  `CascadeReleasePreview`/`CascadeRelease`, widerruft ausdruecklich die fruehere Festlegung „keine
  Kaskade" aus [[2026-08-18-fa-liste-hierarchie-anzeige-spec]] (dort nachgezogen). `BulkRelease`
  selbst bleibt unveraendert zeilenbasiert. **Harte Kommissionierer-Pflicht uebernommen, nicht
  aufgeweicht:** ist `KommissionierungMitZuweisung` aktiv, verlangt der Kaskaden-Dialog einen
  Picker als Pflichtfeld (genau wie `ToggleRelease`/`BulkRelease`) und benennt die Massenwirkung im
  Klartext, bevor sie eintritt. `[RequireLeitstandAccess]`, fortlaufende `PickingPriority` ab
  `MAX+1` in `SubOrderNumber`-Reihenfolge, ein `SaveChangesAsync`, bereits Freigegebene bleiben
  unberuehrt (auch Audit-Felder).

## Migration / Deploy

- **Migration: ja** — `20260910081155_AddProductionOrderMatchcode` +
  `SQL/91_AddProductionOrderMatchcode.sql` (`COL_LENGTH`-Guard) + `SQL/00_FreshInstall.sql` (beide
  Stellen). Additiv, kein Backfill-Zwang — Bestandszeilen fuellen sich erst beim naechsten
  Materialisierungs-Lauf (F2-Praezedenz).
- **Deploy: `web: true`, `service: true`, `migration: true`.** Keine externe Abhaengigkeit — die
  AKE-View-Erweiterung um den Matchcode ist eine hausinterne, spaeter folgende Aufgabe, kein
  Fremdsystem-Termin, blockiert diesen Deploy nicht.
- **Nach dem Deploy:** einen Materialisierungs-Lauf abwarten (max. 15 Min.), danach Matchcode und
  Kopfdaten-je-Zeile gegenpruefen.
- **Vor dem Merge (Backlog-Vorgabe, nicht Teil dieser Spec):** B-0/Ruling-4-Test am Bildschirm
  (erscheint dasselbe Material auf zwei Kommissionierlisten?).
- **Kontext:** Teil des ungemergten Buendels `feature/2026-08-07-ideal-teile-1-5` — Schranke 2 fuer
  alles zusammen, ein Merge.

## Tests

qa-agent, 2026-09-10, Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` @ `deff907`
(Diff-Basis `be92ade..deff907`, 48 Dateien):

```
dotnet build IdealAkeWms.slnx        -> 0 Fehler, 12 Warnungen (bestehend, keine neuen)
dotnet test IdealAkeWms.Tests        -> Fehler 0, erfolgreich 1322, übersprungen 1, gesamt 1323
dotnet test IDEALAKEWMSService.Tests -> Fehler 0, erfolgreich 265,  übersprungen 0, gesamt 265
```

**Beide Zahlen ausdruecklich hier festgehalten (Auflage des Menschen), nicht nur im verworfenen
SDD-Ledger.** Service-Suite ist **nicht** optional — die Matchcode-Materialisierung aendert
`FaMaterializationSyncService.cs`.

Alle 20 Akzeptanzkriterien gegen den echten Diff abgeglichen (nicht gegen Behauptungen) —
Detail-Stichproben im QA-Nachweis der Spec [[2026-09-10-fa-liste-ausbau-matchcode-spec]]. Harte
Vorgaben bestaetigt: genau eine neue Migration, `SetReleaseForOrderNumberAsync` mit `UtcNow` +
`SubOrderNumber`-Sortierung + Picker fuer alle + `!IsReleasedForPicking`-Filter,
`CascadeRelease`-Picker-Pflicht VOR dem Repository-Aufruf, Matchcode `NVARCHAR(200)` ohne
`EF.Functions.Like`, vier K1-Spalten strukturell bedingt auf `Model.Hierarchical`. TS-73
(20 Szenarien, 1:1 auf AK gemappt) + TS-72 (5 Szenarien, Selektor-Sweep) in
`docs/TESTSZENARIEN.md`. Status auf `Testbereit` gesetzt. Ein nicht-blockierender Befund (Streifen-/
Farbzeilen-Fall der Wiederholungswert-Unterdrueckung nicht explizit in TS-73 benannt) in die
manuelle Checkliste der Spec aufgenommen statt in TESTSZENARIEN.md nachgetragen.

## Merge-Commit

_(offen — Schranke 2, Mensch)_
