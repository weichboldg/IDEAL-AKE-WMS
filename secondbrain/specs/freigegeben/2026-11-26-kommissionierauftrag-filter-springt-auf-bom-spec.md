---
type: spec
title: "Bugfix: Filtern und „Zurücksetzen“ bleiben auf dem Kommissionierauftrag (auch ohne Umschalter Stückliste/Liste)"
slug: 2026-11-26-kommissionierauftrag-filter-springt-auf-bom-spec
status: Gemerged
created: 2026-11-26
updated: 2026-11-28
source_backlog: "[[2026-11-26-kommissionierauftrag-filter-springt-auf-bom]]"
bug: "[[2026-11-26-kommissionierauftrag-filter-springt-auf-bom-bug]]"
uebersicht: "[[2026-11-26-kommissionierauftrag-nachbesserungen-uebersicht]]"
depends_on: ""
task: ""
jira: ""
worktree: ".claude/worktrees/2026-11-28-buendel-review-kommissionierung"
branch: "feature/2026-11-28-buendel-review-kommissionierung"
standort: beide
affected_code:
  - "[[2026-11-26-kommissionierauftrag-filter-springt-auf-bom-bug]]"
  - IdealAkeWms/Views/Picking/Bom.cshtml (Filterformular Z. 114-115, „Zurücksetzen“ Z. 126 – Bedingung KannListe → Auftrag != null)
  - docs/TESTSZENARIEN.md (TS-209 bzw. KO-Auftrags-Kapitel, Dev-Lauf prüft)
  - IdealAkeWms/Views/Help/Changelog.cshtml, beide AppVersion.cs
open_questions: []
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-11-28
---

## Ziel / Nutzen (das Warum)

Wer auf einem Kommissionierauftrag filtert, soll auf seinem Auftrag bleiben (Haken, Ist-Menge, Umbuchen). Heute springt das
Filterformular ohne Umschalter Stückliste/Liste auf die nur lesbare FA-Stückliste – an der AKE bei jedem Filtern. Bug-Datensatz:
[[2026-11-26-kommissionierauftrag-filter-springt-auf-bom-bug]].

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** Ziel von „Filtern“ (GET-Formular) und „Zurücksetzen“ in `Views/Picking/Bom.cshtml` im Auftragsmodus.

**Out-of-Scope:** `/Picking/Bom/{id}` ohne Auftrag und die anderen Aufrufer derselben View (`FaCompletionController.cs:416`,
`FaWorklistController.cs:387`) – dort bleibt der Zweig `Url.Action("Bom", …)` unverändert. Nebenwirkung von `Bom`
(`ErzeugeOffeneAsync`, `PickingController.cs:239-247`) bleibt, sie wird vom Auftrag aus nur nicht mehr ausgelöst.

## Fachliche Anforderungen

### Ist-Stand (belegt; Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, AppVersion 1.99.89)

- Formular: `action="@(Model.KannListe ? Url.Action("Auftrag", new { id = Model.Auftrag!.Id }) : Url.Action("Bom", new { id = Model.ProductionOrderId }))"`,
  verstecktes Feld `ansicht` nur bei `KannListe` (`Bom.cshtml:114-115`).
- „Zurücksetzen“ (nur bei gesetztem Filtertext): dieselbe Bedingung (`Bom.cshtml:126`).
- `KannListe = true` nur bei `auftrag != null && vm.Hierarchical && KommissionierlistenAktivAsync()` (`PickingController.cs:334-338`).
- Die Seite ist im Auftragsmodus, wenn `Model.Auftrag` gesetzt ist: `Auftrag(...)` → `BomSeiteAsync(..., auftrag, ...)`
  (`PickingController.cs:255-261`, `Auftrag = auftrag` `:326`); `Bom(...)` übergibt `null` (`:248`). Auftragskopf `Bom.cshtml:47`
  (`@if (Model.Auftrag is { } auftrag)`) nutzt dieselbe Unterscheidung.
- `Auftrag(int id, string? filterText, string? ansicht, …)` nimmt `filterText` an (`PickingController.cs:255`) – der Zielweg existiert.
- Vorher (main) immer `asp-action="Bom" asp-route-id="@Model.ProductionOrderId"` (`IdealAkeWms/Views/Picking/Bom.cshtml:39`, `:50` im
  Hauptcheckout) – der Fehler ist älter als v1.99.89 und dort nur teilweise behoben.

### Soll

| ID | Anforderung |
|---|---|
| F1 | Im Auftragsmodus (`Model.Auftrag != null`) gehen „Filtern“ und „Zurücksetzen“ immer auf `/Picking/Auftrag/{Auftrag.Id}`, unabhängig von `KannListe`. |
| F2 | Mit Umschalter (`KannListe`) bleibt die gewählte Maske erhalten (`ansicht=liste|stueckliste` wie heute); ohne Umschalter wird kein `ansicht` mitgegeben. |
| F3 | Ohne Auftrag (FA-Stückliste, FA-Vervollständigung, Werkbank) unverändert `Url.Action("Bom", new { id = Model.ProductionOrderId })`. |

## Technischer Loesungsentwurf

Ponytail: eine Bedingung an zwei Stellen tauschen, kein Controller-Code. In `Bom.cshtml:114` und `:126` `Model.KannListe ?` durch
`Model.Auftrag != null ?` ersetzen; in `:126` `ansicht = Model.KannListe ? (Model.IstListe ? "liste" : "stueckliste") : null`
(Route-Wert `null` wird von `Url.Action` weggelassen). `:115` (verstecktes `ansicht` nur bei `KannListe`) bleibt.
Kein Unit-Test möglich (Razor) – Nachweis per Manual-UAT auf beiden DEV-Standorten. Frontend-Skill: keine sichtbare Änderung, nur Ziel-URL.

## Migrations-/SQL-Auswirkungen

Keine. Kein Schema, keine Migration, `SQL/00_FreshInstall.sql` und `SQL/AgentJobs/` unberührt.

## Einstellungen

keine

## Audit-Feld-Auswirkungen

Keine – kein Schreibweg geändert. Indirekt entfällt der ungewollte Aufruf von `Bom` (mit `LeitstandAktiv = false` → `ErzeugeOffeneAsync`,
das Aufträge anlegt und auditiert) beim Filtern auf dem Auftrag.

## Akzeptanzkriterien

1. AKE: `/Picking/Auftrag/{id}` → Filtertext „x“, „Filtern“ → URL `/Picking/Auftrag/{id}?filterText=x`, Auftragskopf sichtbar, Zeilen gefiltert, Haken/Umbuchung bedienbar.
2. AKE: auf der gefilterten Auftragsseite „Zurücksetzen“ → `/Picking/Auftrag/{id}` ohne Filter.
3. IDEAL mit `FaHierarchyKommissionierlistenAktiv = false`: wie 1 und 2.
4. IDEAL mit Kommissionierlisten an: Filtern/Zurücksetzen in „Liste“ und in „Stückliste“ bleiben auf dem Auftrag **und** in der gewählten Maske (Verhalten v1.99.89 unverändert).
5. `/Picking/Bom/{FA-Id}` (ohne Auftrag): Filtern/Zurücksetzen bleiben auf `/Picking/Bom/{FA-Id}`; Stichprobe FA-Vervollständigung bzw. Werkbank-Stückliste unverändert.
6. Build und alle Tests grün.

## Test-Szenarien

Kapitel in `docs/TESTSZENARIEN.md`: Ergänzung im Kapitel Kommissionierauftrag-Liste (TS-209, neuer Schritt) oder eigenes Kapitel
(Vorschlag TS-210; der Dev-Lauf prüft die nächste freie Nummer, Stand Index: 209), danach `secondbrain/tests/testszenarien-index.md`.

- **Vorbedingung:** AKE-DEV und IDEAL-DEV, offener Kommissionierauftrag mit ≥ 3 Zeilen, Benutzer mit Picking-Recht.
- **x.1 (AK 1/2)** AKE: Auftrag öffnen, nach einer Artikelnummer filtern → bleibt auf dem Auftrag; Zurücksetzen → Auftrag ungefiltert.
- **x.2 (AK 3)** IDEAL, Kommissionierlisten aus: wie x.1.
- **x.3 (AK 4)** IDEAL, Kommissionierlisten an: in „Liste“ filtern → Liste bleibt; umschalten auf „Stückliste“, filtern → Stückliste bleibt; Zurücksetzen je Maske.
- **x.4 (AK 5) Regression:** `/Picking/Bom/{FA-Id}` filtern/zurücksetzen → bleibt auf der FA-Stückliste.
- **Negativ:** Auftrag im Status Abgeschlossen → Filtern bleibt auf dem (schreibgeschützten) Auftrag.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | kein Epic | – | – |

## Deploy

*Final (QA 2026-11-28, aus dem echten Diff `96cf2f8d..48a2ece3`).*

- **Geänderte Dateien:** `IdealAkeWms/Views/Picking/Bom.cshtml`, `IdealAkeWms/Views/Help/Changelog.cshtml`, beide `AppVersion.cs`, `docs/TESTSZENARIEN.md`.
- **Web-App:** ja (`deploy.web: true`) – Razor-View `Bom.cshtml` (Formular-Ziel + „Zurücksetzen“).
- **Service:** nur Versionsnummer 1.99.90 (`deploy.service: true` nur für einen einheitlichen Versionsstand, keine Logik).
- **Migration:** nein (`deploy.migration: false`), kein SQL, keine Einmal-Skripte.
- **Manifest** `scripts/deploy/nachlauf.psd1`: keine Einträge nötig (kein SQL, keine Einstellung, kein Leitstand-Schritt) – geprüft, nichts fehlt.
- **Publish (erledigt):** `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-11-28-buendel-review-kommissionierung\publish` mit
  `IDEALAKEWMSWeb\`, `IDEALAKEWMSWebService\`, `SQL\`, `deploy\`, `publish-info.txt` (Version 1.99.90, Commit `48a2ece3829cac7394f51b96cfa6022e4627feba`).
  Im Bündel wird das Paket am Laufende mit den Folge-Specs neu gebaut – maßgeblich ist dann das letzte Publish.
- **Reihenfolge:** Web + Service ersetzen (Update-Skript `deploy\Update-IdealAkeWms.ps1`), kein DB-Schritt.
- **Ablauf Mensch:** Publish aus dem Worktree → Testsystem → Test → Merge. Nach dem Merge nur dann neu aus `main` publishen, wenn der Merge
  getestete Dateien mit parallelen `main`-Änderungen zusammengeführt hat:
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

## Offene Rueckfragen

Keine – Ursache und Ziel sind im Code eindeutig (Ist-Stand oben), das erwartete Verhalten steht im Backlog.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

Keine offenen Rückfragen – Freigabe = Verschieben nach `specs/freigegeben/` + `status: Freigegeben`.

## QA-Nachweis (qa-agent, 2026-11-28)

Worktree `.claude/worktrees/2026-11-28-buendel-review-kommissionierung`, Branch `feature/2026-11-28-buendel-review-kommissionierung`,
geprüfter Commit-Bereich `96cf2f8d..48a2ece3` (Spec 1 des Bündels). Ergebnis: **Testbereit**.

### 1. Build und Tests
- `dotnet build IdealAkeWms.slnx`: **0 Fehler**, 37 Warnungen (bestehend, keine neue aus dem Diff).
- `dotnet test`: `IdealAkeWms.Tests` **3853 bestanden / 1 übersprungen / 0 Fehler** (3854); `IDEALAKEWMSService.Tests` **848 / 0 / 0** (848).

### 2. Code-Review (selbst, in diesem Lauf)
Geprüft: kompletter Diff (5 Dateien), `Bom.cshtml` Z. 40–130 (Auftragskopf, Umschalter Z. 62, Filterkarte Z. 112–130),
`PickingController.Bom` (Z. ~225–249, `ErzeugeOffeneAsync` nur bei `LeitstandAktiv=false`), `Auftrag` (Z. 255–261), `BomSeiteAsync`
(`Auftrag = auftrag` Z. 326, `KannListe` Z. 333–338, `KommissionierlistenAktivAsync` Z. 370), Fremdaufrufer der View
`FaCompletionController.cs:416` und `FaWorklistController.cs:387` (setzen `Auftrag` nicht → `null` → Zweig `Bom` unverändert, F3).
- F1 erfüllt: beide Ziele hängen an `Model.Auftrag != null`. F2: verstecktes `ansicht` nur bei `KannListe` (Z. 116 unverändert);
  „Zurücksetzen“ gibt `ansicht = null` ohne Umschalter → `Url.Action` lässt den Wert weg (auf DEV belegt, s. u.). F3: Else-Zweig unverändert.
- Die `!`-Unterdrückung in Z. 115 ist nach der neuen Bedingung harmlos (redundant), Z. 127 ohne `!` – kein Befund.
- Defektklassen: keine `<script>`-Einbettung, kein `hidden`+`d-*`, kein stiller Fallback, keine Parallel-Arrays, keine nur-Client-Validierung,
  keine Regel-Duplikate (die Auftrag/Bom-Unterscheidung nutzt dieselbe Bedingung wie der Auftragskopf Z. 47).
- **Code-Befunde: keine.**
- **B-1 (Brain, nicht blockierend):** Brain-Changelog `secondbrain/changelog/2026-11-28-v1-99-90-*.md` und `feature-map.md` fehlen noch –
  Checklistenpunkt für den Lauf-Abschluss (Hauptagent), kein Code-Defekt.

### 3. Nachweisart je Akzeptanzkriterium
| AK | Nachweis |
|---|---|
| 1 AKE Filtern auf Auftrag | Manual-UAT (Razor-Ziel-URL) – auf AKE-DEV durchgeführt, OK |
| 2 AKE Zurücksetzen | Manual-UAT – AKE-DEV OK |
| 3 IDEAL Kommissionierlisten aus | Manual-UAT – IDEAL-DEV OK |
| 4 IDEAL Kommissionierlisten an, beide Masken | Manual-UAT – IDEAL-DEV OK |
| 5 `/Picking/Bom/{id}` unverändert | Manual-UAT – AKE-DEV OK; FA-Vervollständigung/Werkbank per Code belegt, Browser-Stichprobe offen |
| 6 Build + Tests grün | automatisiert – grün (s. 1) |

Kein AK ist durch einen automatisierten Test bewiesen außer AK 6; die Manual-UAT-AKs zählen nur durch den DEV-Lauf unten.

### 4. Geänderte bestehende Tests
Keine – der Diff ändert keine Testdatei.

### 5. Checkliste CLAUDE.md
Migration/SQL/FreshInstall/AgentJobs: entfällt (kein Schema). Audit-Felder: entfällt (kein Schreibweg). Version 1.99.90 in **beiden**
`AppVersion.cs` ✓. Anwender-Changelog `Views/Help/Changelog.cshtml` ✓. `docs/TESTSZENARIEN.md` TS-210.1–210.5 ✓ + Stand-Zeile ✓.
Testindex Zeile 210 ✓. Brain-Changelog + feature-map: offen (B-1). 5a ADR 0020: keine neue/geänderte Einstellung.

### 6. DEV-Deploy und Testlauf
Standort laut Frontmatter: **beide**. Shell erhöht (Administrator = True).
- `dev-deploy.ps1 -Standort AKE -Quelle <worktree>`: Exit 0, „HTTP 200, Fusszeile v1.99.90“, „Fertig: AKE v1.99.90 auf http://localhost:8081/“.
- `dev-deploy.ps1 -Standort IDEAL -Quelle <worktree>`: Exit 0, „HTTP 200, Fusszeile v1.99.90“, „Fertig: IDEAL v1.99.90 auf http://localhost:8082/“.

Testdaten: AKE-DEV KO-Auftrag Id 41 (InArbeit, 185 Zeilen, FA-Id 3044), Id 111 (Abgeschlossen); IDEAL-DEV Id 3 (Offen, 90 Zeilen).
Nur gefiltert/zurückgesetzt, nichts gebucht.

| Schritt | Durchgeführt | Ergebnis | Status |
|---|---|---|---|
| TS-210.1 AKE Filtern | `/Picking/Auftrag/41`, Filter `87046554`, „Filtern“ | URL `/Picking/Auftrag/41?filterText=87046554`, Kopf KO-2026-00008 sichtbar, „3 Positionen“, Umbuchen/Abschließen-Knöpfe vorhanden | OK |
| TS-210.1 AKE Zurücksetzen | „Zurücksetzen“ | URL `/Picking/Auftrag/41`, „185 Positionen“, Kopf sichtbar | OK |
| TS-210.2 IDEAL Listen aus | `FaHierarchyKommissionierlistenAktiv` auf DEV kurz `false` (+ App-Pool-Recycle wegen Cache), `/Picking/Auftrag/3`, kein Umschalter; Filter `50002134` | URL `/Picking/Auftrag/3?filterText=50002134` (kein `ansicht`), 1 Position; „Zurücksetzen“ → `/Picking/Auftrag/3`, 90 Positionen. Einstellung danach wieder `true` + Recycle | OK |
| TS-210.3 IDEAL Liste | Maske „Liste“, Filter, „Filtern“ | `?ansicht=liste&filterText=50002134`, „1 Zeilen (1 Positionen)“, Liste aktiv; „Zurücksetzen“ → `?ansicht=liste`, 46 Zeilen (90 Positionen) | OK |
| TS-210.3 IDEAL Stückliste | Umschalten „Stückliste“, Filter, „Filtern“ | `?ansicht=stueckliste&filterText=50002134`, 1 Position, Stückliste aktiv; „Zurücksetzen“ → `?ansicht=stueckliste`, 90 Positionen | OK |
| TS-210.4 Regression ohne Auftrag | `/Picking/Bom/3044` (AKE, `LeitstandAktiv=true` → kein Anlegen), Filter, „Filtern“, „Zurücksetzen“ | `/Picking/Bom/3044?filterText=87046554` (3 Positionen) → `/Picking/Bom/3044` (187 Positionen), Karte „Kommissionieraufträge dieses FA“ | OK |
| TS-210.4 FA-Vervollständigung / Werkbank | nicht im Browser | Code: beide Controller setzen kein `Auftrag` → unveränderter `Bom`-Zweig | NICHT MOEGLICH (zeitlich nicht im Browser gestichprobt; offen für den Menschen) |
| TS-210.5 Negativ abgeschlossen | `/Picking/Auftrag/111?filterText=87`, „Filtern“, „Zurücksetzen“ | bleibt auf `/Picking/Auftrag/111` (Status Abgeschlossen, „nur lesbar“, keine Umbuchen-Knöpfe); Reset → 98 Positionen | OK |

Randnotiz (kein Befund dieser Spec): Auf AKE-DEV blendet ein Benutzer-Standard-Spaltenfilter „Bezeichnung 1 = Verdampfer“ Zeilen client-seitig aus;
Positionszähler zählt serverseitig. Vorbestehend, unabhängig vom Diff.

### 7. Publish
`C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-11-28-buendel-review-kommissionierung\publish\` – Web + Service (beide Exit 0, Zielordner vorher geleert),
`SQL\`, `deploy\`, `publish-info.txt` (Version 1.99.90, Commit `48a2ece3829cac7394f51b96cfa6022e4627feba`, Erstellt 2026-11-28 11:35). `publish/` ist gitignored.

### Manuelle Testcheckliste (Mensch, Schranke 2)
1. TS-210.4 Stichprobe: FA-Vervollständigung und Werkbank-Stückliste filtern/zurücksetzen → bleiben auf ihrer Seite (im DEV nicht geprüft).
2. TS-210.1 auf dem Testsystem AKE mit echtem Picking-Benutzer: Filtern, Haken setzen und „Gepickte Artikel umbuchen“ auf der gefilterten Seite.
3. TS-210.2/210.3 auf dem Testsystem IDEAL (Kommissionierlisten so, wie dort eingestellt) – Liste und Stückliste je Filtern/Zurücksetzen.
4. TS-210.5 abgeschlossener Auftrag auf dem Testsystem.
5. Danach Merge (Schranke 2).
