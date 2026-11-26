---
type: spec
title: "Bugfix: Filtern und „Zurücksetzen“ bleiben auf dem Kommissionierauftrag (auch ohne Umschalter Stückliste/Liste)"
slug: 2026-11-26-kommissionierauftrag-filter-springt-auf-bom-spec
status: Entwurf
created: 2026-11-26
updated: 2026-11-26
source_backlog: "[[2026-11-26-kommissionierauftrag-filter-springt-auf-bom]]"
bug: "[[2026-11-26-kommissionierauftrag-filter-springt-auf-bom-bug]]"
uebersicht: "[[2026-11-26-kommissionierauftrag-nachbesserungen-uebersicht]]"
depends_on: ""
task: ""
jira: ""
worktree: ""
branch: ""
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
freigabe_von: ""
freigabe_am: ""
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

*Vorläufig – der QA-Lauf bestätigt gegen den echten Diff.*

- **Web-App:** ja (nur `Views/Picking/Bom.cshtml`)
- **Service:** nur Versionsnummer (`IDEALAKEWMSService/AppVersion.cs`), keine Logik
- **Migration:** nein
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): keine (kein SQL, keine Einstellung, kein Leitstand-Schritt; reiner Bugfix ohne Anwender-Info).
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` (im Worktree; im Bündel ggf. beide Komponenten).

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
