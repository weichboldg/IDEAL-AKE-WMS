---
type: spec
title: "Beschichtungsauftrag: Statuswechsel übernimmt ungespeicherte Eingaben"
slug: 2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-3-spec
status: Testbereit
created: 2026-11-23
updated: 2026-11-23
source_backlog: "[[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung]]"
uebersicht: "[[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-uebersicht]]"
depends_on: "[[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-2-spec]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
version_vorschlag: "1.99.56 (Dev-Lauf prüft)"
testszenario_vorschlag: "TS-181 (Dev-Lauf prüft)"
affected_code:
  - IdealAkeWms/Views/Beschichtungsauftraege/Details.cshtml (Status-Knöpfe :100-120)
  - IdealAkeWms/Controllers/BeschichtungsauftraegeController.cs (Bearbeiten :185-216, optionaler Parameter ziel)
  - IdealAkeWms/wwwroot/js/beschichtung-bemerkung.js (nur bei Rückfrage 2 = ja)
  - IdealAkeWms.Tests (Controller-Test Bearbeiten mit ziel)
  - docs/TESTSZENARIEN.md (TS-181, Verweis aus TS-162/163), Views/Help (Beschichtungsauftrag), Views/Help/Changelog.cshtml, beide AppVersion.cs
  - "Bug-Datensatz: [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-bug]] (Symptom 2)"
open_questions:
  - "Variante a, b oder c? (Empfehlung a: Status-Knopf speichert die Eingaben mit)"
  - "Drucken/PDF mit ungespeicherten Positionsänderungen: dieselbe Rückfrage wie heute bei der Bemerkung („nicht gespeichert – trotzdem fortfahren?“)?"
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

# Teil 3: Beschichtungsauftrag – Statuswechsel übernimmt Eingaben

Übersicht: [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-uebersicht]] · Backlog-Punkt 4.
Vorgänger: [[2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck-spec]] (v1.99.41, A8 „EIN Formular, EIN Speichern“, A12 Bemerkung).

## Ziel / Nutzen (das Warum)

Wer im Entwurf Lackierer, Dienstleister oder RAL ändert und dann „Auf Übergeben setzen“ klickt, verliert die Änderung still – oder bekommt
„Dienstleister fehlt“, obwohl er ihn gerade eingetragen hat. Dasselbe gilt für Haken „verpackt“ vor „Auf Versendet setzen“. Heute hilft
nur der Umweg über ENTER bzw. „Speichern“.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** die Status-Knöpfe „Auf … setzen“ auf der Detailseite, solange das Formular `entwurf-form` existiert (Entwurf und Übergeben).
**Out-of-Scope:** Stornieren (bleibt eigenes Formular mit Bestätigung, Speichern vor Storno ist sinnlos), Bemerkung (eigenes Formular,
eigene Warnung A12), Statusregeln im Workflow, Liste/Index.

## Fachliche Anforderungen

**Belegt (Ursache):**
- Alle Eingaben hängen über `form="entwurf-form"` an einem Formular (`Details.cshtml:127-134`, z. B. Lackierer `:147`, Dienstleister `:167`,
  RAL `:175`, Mengen `:302`, Verpackt `:315`, Entfernen `:326`, Kandidaten `:384`); einziger Absende-Knopf „Speichern“ `:339`.
- ENTER in einem Textfeld sendet `entwurf-form` über diesen Knopf ab (Browser-Standard). In einem `<select>` (Lackierer) löst ENTER nichts
  aus – dort hilft nur „Speichern“.
- Die Status-Knöpfe sind eigene Formulare mit nur `ziel` + `rowVersion` (`:103-118`) → `Status` (`Controller :252-257`) lädt den gespeicherten
  Stand. Ungespeicherte Eingaben werden nicht gesendet.
- `StatusWechselnAsync` prüft den **gespeicherten** Stand: Übergeben verlangt `Dienstleister` (`BeschichtungsauftragWorkflow.cs:492-493`),
  Versendet verlangt alle Positionen „verpackt“ (`:494-503`). Der Hinweis „Dienstleister fehlt“ (`Details.cshtml:122-125`) liest ebenfalls
  den gespeicherten Wert.
- Vorhandenes Muster für „ungespeichert“: `beschichtung-bemerkung.js` (Rückfrage an allen `[data-bemerkung-pruefen]`, Capture-Phase);
  Status-Knöpfe, Speichern, Drucken und PDF tragen das Attribut (`Details.cshtml:29, 33, 110, 115, 339`).

**Varianten:**

| | Variante | Wirkung | Aufwand / Risiko |
|---|---|---|---|
| **a** | Status-Knopf sendet `entwurf-form` mit `ziel`; Server speichert, dann Statuswechsel | Ein Klick, nichts geht verloren, Prüfungen sehen den neuen Stand | klein: View-Knöpfe + ein Parameter; zwei Schritte (siehe unten) |
| b | Automatisch speichern bei `change` | kein „Speichern“ mehr nötig | groß: Seite lädt nach jedem Feld neu oder AJAX mit `rowVersion`-Fortschreibung; Kandidaten/Select2-Zeilen halb ausgefüllt → Fehlermeldungen beim Tippen; widerspricht A8 |
| c | Warnung „ungespeicherte Änderungen“ vor Statuswechsel | Verlust wird verhindert, Benutzer muss trotzdem erst speichern | sehr klein (vorhandenes JS erweitern); löst die Meldung nicht, nur das stille Verlieren |

**Empfehlung: a.** Sie beseitigt die Ursache mit dem kleinsten Eingriff und nutzt den vorhandenen Speicherweg. Die Varianten-Entscheidung
trifft Gerald (`freigabe_entscheidung`).

**Anforderung bei Variante a:**
1. Solange `entwurf-form` existiert, sind die Status-Knöpfe (außer Stornieren) `type="submit" form="entwurf-form" name="ziel" value="<Ziel>"`.
   Ohne `entwurf-form` (nicht bearbeitbar) bleiben sie wie heute eigene Formulare.
2. `Bearbeiten` bekommt `BeschichtungsauftragStatus? ziel = null`. Ablauf: wie heute speichern; scheitert das, Meldung wie heute, **kein**
   Statuswechsel. Gelingt es und `ziel` ist gesetzt: `StatusWechselnAsync(id, ziel, <RowVersion nach dem Speichern>)`.
3. Scheitert der Statuswechsel nach erfolgreichem Speichern (z. B. Dienstleister leer, nicht alles verpackt): `SuccessMessage` „Gespeichert.“
   **und** `WarningMessage` mit dem Fehlertext des Workflows – nie still.
4. Gelingt beides: eine Meldung „Gespeichert, Status auf „…“ gesetzt.“ (plus „n Position(en) hinzugefügt“ wie heute).
5. Die Rückfrage der Bemerkung (`data-bemerkung-pruefen`) bleibt an den Knöpfen.

## Technischer Loesungsentwurf

Leiter Sprosse 2: vorhandener POST `Bearbeiten` und vorhandener `StatusWechselnAsync`, kein neuer Endpunkt. HTML-`form`/`name`/`value` am
Knopf (Plattformmittel) statt JS. Zwei Speichervorgänge nacheinander (eigene `SaveChanges`) – bewusst: der Statuswechsel prüft den
gespeicherten Stand, Speichern ohne Statuswechsel ist ein gültiges Ergebnis.
**Prüfpunkt Dev:** `BeschichtungsauftragErgebnis.Auftrag` (`BeschichtungsauftragWorkflow.cs:15-18`) muss nach `BearbeitenAsync` die neue
`RowVersion` tragen (EF schreibt den Concurrency-Token nach `SaveChanges` zurück); sonst nachladen. Test deckt das ab.

Bei Rückfrage 2 = ja: `beschichtung-bemerkung.js` um „`entwurf-form` geändert“ erweitern (Vergleich der serialisierten Formulardaten beim
Laden vs. beim Klick), aber nur für Drucken/PDF (`data-print-link`); Status und Speichern nehmen die Eingaben ja jetzt mit.

## Migrations-/SQL-Auswirkungen

Keine.

## Einstellungen

keine

## Audit-Feld-Auswirkungen

Unverändert: Speichern und Statuswechsel setzen die Felder über die vorhandenen Workflow-Methoden (`Geaendert(a, jetzt)`, `:515`).
Bei Variante a entstehen zwei Änderungen kurz nacheinander (ModifiedAt zweimal).

## Akzeptanzkriterien

1. Entwurf ohne Dienstleister: Dienstleister eintippen (kein ENTER), „Auf Übergeben setzen“ → Status Übergeben, Dienstleister gespeichert.
2. Lackierer im Auswahlfeld ändern, direkt Status-Knopf → Kreditor gespeichert, Status gewechselt.
3. RAL ändern + Status-Knopf → RAL gespeichert.
4. Übergeben: alle Haken „verpackt“ setzen, ohne Speichern „Auf Versendet setzen“ → Versendet.
5. Übergeben: einen Haken offen lassen, „Auf Versendet setzen“ → „Gespeichert.“ + Warnung mit der offenen Position; übrige Haken gespeichert,
   Status bleibt Übergeben.
6. Ungültige Menge („abc“) + Status-Knopf → Warnung „Eingabe nicht gespeichert …“, kein Statuswechsel.
7. Stornieren verhält sich wie heute (Bestätigung, eigene Anfrage).
8. Zwei Fenster: im zweiten speichern, im ersten Status-Knopf → „veralteter Stand“ wie heute, nichts geändert.
9. Bemerkung geändert, Status-Knopf → Rückfrage wie bisher.
10. Nur bei Rückfrage 2 = ja: Menge geändert, „Drucken“ → Rückfrage „nicht gespeichert“.

## Test-Szenarien

Neues Kapitel **TS-181 „Beschichtungsauftrag: Statuswechsel übernimmt Eingaben (v1.99.56)“**, Verweis aus TS-162.
- 181.1 Kopf Entwurf (AK 1–3) · 181.2 Verpackt/Versendet (AK 4, 5) · 181.3 Negativ: ungültige Menge, veralteter Stand, Storno (AK 6–8)
- 181.4 Bemerkung/Druck (AK 9, 10) · 181.5 Automatisch: Controller-Test `Bearbeiten` mit `ziel` (Erfolg, Speichern ok + Status abgelehnt, Speichern abgelehnt)
- Standort: Seite ist an beiden aktiv; Test auf DEV AKE (Meldung) und Stichprobe DEV IDEAL.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt (kein Epic) | – | – |

## Deploy

*Vorläufig, QA bestätigt am echten Diff.*
- **Web-App:** ja
- **Service:** nein
- **Migration:** nein
- **Manifest-Einträge:** keine
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Offene Rueckfragen

1. Variante a, b oder c? Empfehlung **a** (Status-Knopf speichert die Eingaben mit; Begründung in der Variantentabelle).
2. Drucken/PDF mit ungespeicherten Positionsänderungen: dieselbe Rückfrage wie heute bei der Bemerkung („nicht gespeichert – trotzdem
   fortfahren?“)? Empfehlung: ja, Wiederverwendung von `beschichtung-bemerkung.js`; sonst druckt man den alten Stand.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. → a (Empfehlung): Status-Knopf speichert die Eingaben mit.
2. → ja (Empfehlung): dieselbe Rückfrage wie bei der Bemerkung.

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

**Beweisart je AK:** AK 1, 5, 6 und Teilerfolg automatisiert (`BeschichtungsauftraegeControllerTests.Bearbeiten_MitZiel_*`, `Bearbeiten_Uebergeben_HakenMitZielVersendet`, InMemory).
Prüfpunkt RowVersion nach erstem Speichern auf SQL Server: **auf DEV belegt** (siehe 181.1/181.2). AK 7, 9, 10 Manual-UAT (Browser-Dialog).

**DEV-Testlauf TS-181 (DEV AKE :8081, SQL Server `AKE_WMS_DEV`):**
| Nr | gemacht | Ergebnis | Urteil |
|---|---|---|---|
| 181.1 | BA-2026-00002 (Entwurf, ohne Dienstleister): Dienstleister „QA Lack TS181“ + RAL „RAL 9016“ getippt, ohne ENTER „Auf „Übergeben“ setzen“ | Grün „Gespeichert, Status auf „Übergeben“ gesetzt.“; DB: Status 30, Dienstleister/RAL gespeichert, `ModifiedAt` = `UebergebenAt` 17:58:21 → Speichern und Wechsel in einer Anfrage, zweiter Schritt mit neuer RowVersion ohne Konflikt | OK |
| 181.1 (Lackierer-Auswahl) | nicht einzeln | – | offen (Mensch) |
| 181.2 | Übergeben: Haken 87013744 entfernt, „Auf „Versendet“ setzen“; dann Haken gesetzt, erneut | erst Gelb „Versendet nur, wenn alle Positionen verpackt sind — nicht verpackt (1): 87013744 (ohne FA)…“ + Grün „Gespeichert.“, DB Verpackt 1/0, Status 30; dann „Gespeichert, Status auf „Versendet“ gesetzt.“ | OK |
| 181.3 | BA-2026-00001: Dienstleister + Menge `abc`, Status-Knopf; zwei Tabs: Tab 2 „Speichern“, Tab 1 Status-Knopf | „Eingabe nicht gespeichert: Menge „abc“ ist keine Zahl.“, DB unverändert; „BA-2026-00001 wurde inzwischen von jemand anderem geändert — bitte neu laden …“, DB unverändert (Status 10, Dienstleister NULL) | OK |
| 181.3 Storno | nicht geklickt (Bestätigungsdialog) | – | NICHT MOEGLICH (Dialog) |
| 181.4 | Rückfragen Bemerkung/Druck | nicht ausgeführt (Browser-confirm) | NICHT MOEGLICH (Dialog) |
| 181.5 | BA-2026-00002 Versendet (nicht bearbeitbar): „Auf „Erledigt“ setzen“ (eigenes Formular mit hidden `ziel`/`rowVersion`) | „Status auf „Erledigt“ gesetzt.“ | OK |
Testdaten danach: BA-2026-00002 steht auf Erledigt (DEV).

**Manuelle Checkliste (offen):**
1. TS-181.1 Lackierer im Auswahlfeld wählen + Status-Knopf → Kreditor gespeichert.
2. TS-181.3 Stornieren mit Bestätigung wie bisher.
3. TS-181.4 Bemerkung geändert → Rückfrage; Menge geändert → „Drucken“/„PDF“ fragt „Änderungen an den Positionen sind nicht gespeichert …“, Abbrechen druckt nicht; nach Speichern keine Rückfrage.
4. Stichprobe DEV IDEAL / Testsystem.
