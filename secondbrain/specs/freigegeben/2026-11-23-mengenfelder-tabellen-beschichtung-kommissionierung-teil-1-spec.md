---
type: spec
title: "Mengenfelder: Pfeil/Spinner zählt ±1, Mausrad ändert keinen Wert (global)"
slug: 2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-1-spec
status: Testbereit
created: 2026-11-23
updated: 2026-11-23
source_backlog: "[[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung]]"
uebersicht: "[[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-uebersicht]]"
depends_on: ""
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
version_vorschlag: "1.99.54 (Dev-Lauf prüft)"
testszenario_vorschlag: "TS-179 (Dev-Lauf prüft)"
affected_code:
  - IdealAkeWms/wwwroot/js/site.js (ein neuer Block, global über _Layout.cshtml:544)
  - docs/TESTSZENARIEN.md (TS-179), Views/Help/Changelog.cshtml, beide AppVersion.cs
  - "Bug-Datensatz: [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-bug]] (Symptom 1)"
open_questions:
  - "Regel bestätigen: Pfeil/Spinner zählt an jedem Zahlenfeld mit Dezimal-Schrittweite um ±1 ab dem aktuellen Wert, Kommastellen bleiben, keine Einheiten-Logik (Stück wird nicht auf ganze Zahlen gerundet)?"
  - "Mausrad: Im fokussierten Zahlenfeld nie den Wert ändern (Feld verliert den Fokus, die Seite scrollt)?"
  - "(Fachbereich) Welches Mengenfeld in der Kommissionierung war gemeint? Die Ist-Menge der Auftragsseite ist seit v1.88.0 ein Textfeld ohne Pfeile; am AKE-Test läuft v1.30.x."
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: "freigegeben"
freigabe_von: "Gerald"
freigabe_am: 2026-11-23
---

# Teil 1: Mengenfelder – Pfeil und Mausrad

Übersicht: [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-uebersicht]] · Backlog-Punkt 1.

## Ziel / Nutzen (das Warum)

Wer im Mengenfeld die Pfeiltaste oder den Spinner nutzt, erwartet „eins mehr“. Heute wird aus 1 eine 1,001, weil der Browser um das
`step`-Attribut zählt. Das kostet am Terminal Zeit und erzeugt falsche Mengen, die man nur beim genauen Hinsehen bemerkt.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** alle `<input type="number">` der Anwendung, deren `step` kleiner als 1 oder `any` ist – serverseitig gerendert und per JS
erzeugt. Eine Stelle: `site.js`.

**Out-of-Scope:**
- Felder mit `step="1"` oder ohne `step` (ganzzahlig, zählen schon heute ±1): BdeTerminal, WarehousePicking, Lagerbestellung,
  Einstellungen, Sortierfelder.
- Textfelder mit `inputmode="decimal"` (z. B. Ist-Menge Kommissionierauftrag `Picking/Bom.cshtml:285`, Mengen Beschichtungsauftrag
  `Beschichtungsauftraege/Details.cshtml:302`) – sie haben keine Pfeile.
- Einheiten-abhängiges Runden (Stück ganzzahlig) – siehe Rückfrage 1. Kein Umbau der `step`-Werte je View.
- Server-Lesen der Dezimalwerte (Kultur) – unverändert, siehe [[fallstricke]] „Die Anwendung hat keine feste Request-Kultur“ (überholt
  seit v1.99.23).

## Fachliche Anforderungen

**Belegt (Bestand im Bündel, Grep `<input[^>]*type="number"` über `IdealAkeWms/`):**

| `step` | Fundstellen | Pfeil heute |
|---|---|---|
| `0.001` (9×) | `Articles/Create.cshtml:35`, `Articles/Edit.cshtml:66`, `FaCompletion/Edit.cshtml:304,349`, `StockMovements/Inbound.cshtml:52`, `InboundBulk.cshtml:84,128`, `Outbound.cshtml:39`, `Transfer.cshtml:39` | ±0,001 |
| `0.01` (6× + 2× JS) | `BdeBookings/Edit.cshtml:95,99`, `StockOverview/Index.cshtml:59,63` (Filter), `StorageLocations/Create.cshtml:36`, `Edit.cshtml:70`; **zusätzlich** `wwwroot/js/bde-terminal.js:981,982` (im Backlog nicht gezählt) | ±0,01 |
| `any` (7×) | `BdeTeileliste/Index.cshtml:185,190`, `BfAuftraege/Details.cshtml:115,191`, `Picking/Bom.cshtml:669,1497,1726` (Dialoge Lagerbestellung, Sammel-Lagerbestellung, Bestellung) | ±1 ab aktuellem Wert (Browser-Standard-Schritt 1) |
| `1` (11×) bzw. ohne `step` | u. a. `BdeTerminal/Index.cshtml:119-120`, `WarehousePicking/Details.cshtml:98`, `WarehouseRequisitions/Edit.cshtml:52,170`, `_SettingField.cshtml:55` | ±1, unverändert |

Die Zählung des Backlogs (9/6/11/7) stimmt; es fehlen nur die beiden JS-Felder in `bde-terminal.js`. Alle Seiten mit Zahlenfeldern laufen
über `_Layout.cshtml` (einzige Ausnahmen `Layout = null` sind Login und Druckseiten ohne Zahlenfelder, Grep `Layout\s*=`), `site.js` ist dort
global eingebunden (`_Layout.cshtml:544`). `site.js` hat heute keinen Handler für `focusin`/`keydown`/`wheel` (Grep).

**Anforderung (Empfehlung, Rückfrage 1 und 2):**
1. Pfeil hoch/runter und Spinner an einem Zahlenfeld mit `step < 1` oder `step="any"` ändern den Wert um genau 1, ausgehend vom aktuellen
   Wert. Kommastellen bleiben (1,5 → 2,5). `min`/`max` begrenzen wie bisher.
2. Nach dem Verlassen des Feldes gilt wieder das ursprüngliche `step` – die Browser-Prüfung beim Absenden (z. B. höchstens 3
   Nachkommastellen bei `0.001`) bleibt, wie sie ist.
3. Mausrad über einem fokussierten Zahlenfeld ändert den Wert nie; das Feld verliert den Fokus, die Seite scrollt weiter.
4. Gilt auch für Zahlenfelder, die später per JS entstehen (Dialoge in `Picking/Bom.cshtml`, `bde-terminal.js`, `InboundBulk`-Vorlage).

## Technischer Loesungsentwurf

Leiter: Sprosse 4 (Plattformmittel). Der Browser zählt bei `step="any"` mit dem Standard-Schritt 1 und ohne Raster-Ausrichtung – genau
die gewünschte Regel. Ein delegierter Handler in `site.js` setzt das Attribut nur, solange das Feld bedient wird:

- `focusin` und `pointerdown` (Capture-Phase, damit es vor dem Spinner-Klick wirkt): Ist das Ziel `input[type=number]` mit
  `step="any"` oder `parseFloat(step) < 1`, alten Wert in `data-step-orig` merken und `step = "any"` setzen.
- `focusout`: `step` aus `data-step-orig` zurücksetzen, Attribut entfernen.
- `wheel` (passiv): ist das Ziel das fokussierte Zahlenfeld → `blur()`.

Delegation am `document` deckt dynamisch erzeugte Felder ohne `MutationObserver` ab. Kein neues Skript, keine Änderung an Views.

**Verworfen:** `step` in rund 12 Views auf `any` umschreiben (je View, neue Views fallen zurück); eigene `keydown`-Rechnung (Spinner-Klicks
mit der Maus blieben bei ±0,001).

**Prüfpunkt Dev/QA (nicht belegt, Browser-Verhalten):** Dass Chromium (Edge/Chrome an den Terminals) bei `step="any"` von 1,5 auf 2,5
zählt und bei Spinner-Klick zuerst `pointerdown` feuert, ist im DEV-Test nachzuweisen (AK 1–3). Firefox-Verhalten beim Mausrad ist
browserabhängig; der `blur` schadet dort nicht.

## Migrations-/SQL-Auswirkungen

Keine.

## Einstellungen

keine

## Audit-Feld-Auswirkungen

Keine (reine Oberfläche).

## Akzeptanzkriterien

1. Einbuchung (`/StockMovements/Inbound`): Menge 1, Pfeil hoch → 2, Pfeil runter → 1; nie 1,001.
2. Dasselbe mit dem Spinner per Maus.
3. Menge 2,5 eingetippt, Pfeil hoch → 3,5.
4. Nach Verlassen des Feldes ist das `step`-Attribut wieder `0.001` (DevTools); Absenden mit 1,0005 meldet der Browser wie bisher.
5. Mausrad über dem fokussierten Feld: Wert bleibt, Seite scrollt.
6. Ein per JS erzeugtes Feld (Sammel-Lagerbestellung in der Stückliste, `Picking/Bom.cshtml:1497`; Gruppen-Meldung `bde-terminal.js:981`)
   verhält sich wie AK 1 und 5.
7. Ganzzahl-Felder (`WarehousePicking/Details`, `BdeTerminal`) verhalten sich unverändert.
8. Kein Konsolenfehler auf Seiten ohne Zahlenfeld.

## Test-Szenarien

Neues Kapitel **TS-179 „Mengenfelder: Pfeil ±1, Mausrad ohne Wirkung (v1.99.54)“** in `docs/TESTSZENARIEN.md`, danach Zeile in
`secondbrain/tests/testszenarien-index.md`.
- 179.1 Einbuchung Tastatur und Spinner (AK 1–3) · 179.2 Absende-Prüfung unverändert (AK 4) · 179.3 Mausrad (AK 5)
- 179.4 Dialog-Felder Stückliste und BDE-Terminal-Gruppe (AK 6) · 179.5 Negativ: Ganzzahl-Felder, Seite ohne Zahlenfeld (AK 7, 8)
- Je an DEV AKE und DEV IDEAL, Edge.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt (kein Epic) | – | – |

## Deploy

*Vorläufig, QA bestätigt am echten Diff.*
- **Web-App:** ja (nur `site.js`)
- **Service:** nein
- **Migration:** nein
- **Manifest-Einträge:** keine
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` (im Worktree)

## Offene Rueckfragen

1. Regel bestätigen: Pfeil/Spinner zählt an jedem Zahlenfeld mit Dezimal-Schrittweite um ±1 ab dem aktuellen Wert, Kommastellen bleiben,
   keine Einheiten-Logik (Stück wird nicht auf ganze Zahlen gerundet)? Empfehlung: ja. Einheiten-Logik bräuchte die Einheit an jedem Feld
   (je View) und widerspricht „eine Stelle“.
2. Mausrad: Im fokussierten Zahlenfeld nie den Wert ändern (Feld verliert den Fokus, Seite scrollt)? Empfehlung: ja.
3. (Fachbereich) Welches Mengenfeld in der Kommissionierung war gemeint? Die Ist-Menge der Auftragsseite ist seit v1.88.0 ein Textfeld ohne
   Pfeile (`Picking/Bom.cshtml:285`), am AKE-Test läuft v1.30.x ([[2026-11-12-deploy-fahrplan-buendel-v1-99]] Z. 69). Ändert die Lösung
   nicht, nur den Test.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. → ja (Empfehlung): ±1 ab aktuellem Wert, Kommastellen bleiben, keine Einheiten-Logik.
2. → ja (Empfehlung): Mausrad ändert im fokussierten Zahlenfeld nie den Wert.
3. → Gemeint ist die Kommissionierung; die Mengeneingabe kommt als eigene Spalte „Menge IST“ (siehe Teil 5, Antwort 3). Die Pfeil-Regel gilt dort mit.

## QA-Nachweis 2026-11-23 (qa-agent)

**Gemeinsamer Nachweis (QA-Lauf 2026-11-23 abends, qa-agent, Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Diff `1cfb92d7..b79a4b1a`):**
- **Build:** `dotnet build IdealAkeWms.slnx` 0 Fehler (30 Warnungen, vorbestehend).
- **Tests:** `IdealAkeWms.Tests` 3480 bestanden / 1 übersprungen / 0 Fehler; `IDEALAKEWMSService.Tests` 790 / 0 / 0. Nach dem QA-Fix `b79a4b1a` Web erneut 3480/1/0.
- **DEV-Deploy** (`D:\Dev\scripts\dev-deploy.ps1`, Quelle Worktree, Web + Dienst, erhöhte Shell): AKE und IDEAL je zweimal (vor und nach QA-Fix), Exit 0,
  „HTTP 200, Fusszeile v1.99.58“, „Fertig: AKE v1.99.58 auf http://localhost:8081/“ bzw. „Fertig: IDEAL v1.99.58 auf http://localhost:8082/“. Footer = AppVersion 1.99.58.
- **Publish:** `<worktree>\publish\IDEALAKEWMSWeb` + `IDEALAKEWMSWebService` (Ordner vorher geleert, beide Exit 0), `SQL\` → `publish\SQL`, `scripts\deploy\` → `publish\deploy`,
  `publish\publish-info.txt` Version 1.99.58, Commit `b79a4b1a0fef42d220265ad5d81983816dce50aa`. Paket-ZIP/Shares: Sache des Orchestrators (DoD).
- **QA-Fix `b79a4b1a`** (`qa(...teil-5)`): Leerzeilen-`colspan` der Stückliste zählt die neue Spalte „Menge IST“ mit (`Picking/Bom.cshtml`, `emptyColspan` + 1 bei `Model.Auftrag != null`).
- **Geänderte bestehende Tests:** `WarehousePickingVerbrauchsbuchungTests` (Zeile 227: `vm.EntnahmePlaetze` → `vm.Items[0].EntnahmeOptionen`, Typ-/Ortswechsel durch Nachbesserung `b7e2d5a4`, Aussage gleich);
  `ViewKeyDriftGuardTests.ViewKey_Registriert` (verschärft: `supportsReorder` Gleichlauf, Teil 2 AK 6); `ViewKeyDriftGuardTests.WareneingangPositionen_*` `(false,false)`→`(true,false)` (Verhalten absichtlich geändert, Teil 2 Antwort 1). Kein „Sonstiges“.
- **Checkliste CLAUDE.md:** keine Migration/kein SQL/kein FreshInstall/keine Einstellung (ADR 0020 entfällt), keine Audit-Änderung; Version 1.99.54–1.99.58 in beiden `AppVersion.cs` (Dienst nur Nummer), Anwender-Changelog je Version vorhanden;
  TS-178.12, TS-179–183 in `docs/TESTSZENARIEN.md` und im Testindex. **Offen beim Orchestrator:** Brain-Changelog `secondbrain/changelog/` und `feature-map.md` für 1.99.54–1.99.58 fehlen noch; Doku-Nachzug `docs/anwender`/`docs/betrieb` (DoD). Kein Manifest-Eintrag nötig (kein Deploy-Schritt).

**Code-Review (qa-agent, selbst, Diff `1cfb92d7..HEAD`, 24 Dateien):** Befunde gesamt: (B1) `Bom.cshtml` Leerzeilen-`colspan` ohne neue Spalte → behoben `b79a4b1a`.
(H1, Hinweis, nicht behoben) `site.js` Teil 1: Bekommt ein Zahlenfeld `pointerdown`, aber nie den Fokus (z. B. `disabled`), bleibt `step="any"` bis zum nächsten Fokus/Blur – nur Browser-Schrittprüfung betroffen, Server prüft ohnehin; kein Handlungsbedarf.
(H2, Hinweis) Nachbesserung `EntnahmeOptionen`: Rückfall „volle Liste“ greift nur, wenn weder Bestand noch NAN wählbar – dann bleibt die frühere Laufzeit (seltener Fall, bewusst).
Geprüfte Fehlerklassen: kein `@Html.Raw` in `<script>` (neue `#column-config`-Zeile ist statisches JSON in `<text>`); kein `hidden` mit `d-*`; keine neuen stillen Fallbacks (`r.Auftrag?.RowVersion ?? rv` führt höchstens zu „veraltet“-Warnung, nicht still);
Parallel-Arrays: Receive nutzt indizierte Namen `zeilen[i].*`, Umsortieren der Spalten ändert keine Bindung; Spaltenzugriffe in `Bom.cshtml`-JS über `bomColKeyToIndex` (dynamisch), nicht über feste Indizes; `column-preferences.js` ignoriert gespeicherte Schlüssel ohne DOM-Spalte (Stückliste ohne Auftrag);
Serverregel Statuswechsel bleibt in `BeschichtungsauftragWorkflow.StatusWechselnAsync` (eine Stelle), Ist-Menge-Prüfung weiter nur serverseitig in `PickingTransferService`.

**Deploy (QA-bestätigt am echten Diff):** Web ja (`IdealAkeWms/` geändert), Dienst: nur Versionsnummer (`IDEALAKEWMSService/AppVersion.cs`) – fachlich unverändert, mitliefern für gleiche Version; Migration nein; Manifest nein.
Paket: `<worktree>\publish` v1.99.58 (Web + Dienst). Ablauf Mensch: Paket auf Testsystem → testen → Merge. Nach dem Merge nur dann aus `main` neu publizieren, wenn der Merge geprüfte Dateien mit parallelen main-Änderungen zusammengeführt hat:
`dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` und `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`.

**Beweisart je AK:** alle AK Manual-UAT (Browser-JavaScript, nicht automatisiert testbar).

**DEV-Testlauf TS-179 (DEV AKE :8081, Chrome per claude-in-chrome, `/StockMovements/Inbound`):**
| Nr | gemacht | Ergebnis | Urteil |
|---|---|---|---|
| 179.1 | Menge `1`, Pfeil hoch, Pfeil runter; `2,5` + Pfeil hoch; Spinner-Pfeil oben per Maus | 1 → 2 → 1; 2,5 → 3,5; Spinner 3,5 → 4,5 (nie x,001) | OK |
| 179.2 | `step`-Attribut nach Verlassen / Absenden 1,0005 | Attribut ohne DevTools/JS-Werkzeug nicht ablesbar; Code `site.js` focusout setzt `data-step-orig` zurück | NICHT MOEGLICH (Mensch, DevTools) |
| 179.3 | Feld fokussiert, Mausrad über dem Feld | Wert bleibt 4,5, Feld verliert den Fokus | OK |
| 179.4 | Dialogfelder Stückliste / BDE-Gruppe | nicht ausgeführt | NICHT MOEGLICH (Zeit; Mensch) |
| 179.5 | Konsole auf der Einbuchung | keine Konsolenfehler | OK (Teil); Ganzzahlfelder offen |

**Manuelle Checkliste (offen für den Menschen):**
1. TS-179.2 in Edge mit DevTools: `step` nach Blur wieder `0.001`, Absenden mit 1,0005 meldet der Browser.
2. TS-179.4 Sammel-Lagerbestellung (Stückliste) und BDE-Gruppen-Meldung: Pfeil ±1, Mausrad.
3. TS-179.5 Ganzzahlfelder (`WarehousePicking/Details`, BDE) unverändert.
4. Durchlauf am Testsystem (Edge) nach Paket-Deploy.
