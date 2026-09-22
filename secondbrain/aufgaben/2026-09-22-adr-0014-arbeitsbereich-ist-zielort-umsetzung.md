---
typ: notiz
spec: "[[2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: Rückbau Werkbank-aus-Arbeitsbereich-Ableitung (ADR 0014 auf falsches Feld)

Spec [[2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec]] · **bestehender Bündel-Worktree**.
Version **1.43.0** (Worktree bei 1.42.0, BDE-Spec beansprucht keine feste Nummer). **Keine Migration.**
Reihenfolge: **VOR** der BDE-Spec ([[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]]).

Reiner **Rückbau**: die im August auf falscher Annahme („Werkbank = Arbeitsbereich") gebaute
Werkbank-Ableitung aus `FaHierarchyNode.Arbeitsbereich` (v1.37, Testbereit, ungemergt) entfernen.
Arbeitsbereich = **Zielort**, keine Werkbank. Werkbank lebt je Arbeitsgang (BDE-Spec).

## Pre-Flight-Verdikt (2026-09-22)

- status Freigegeben, open_questions [], freigabe_entscheidung/_von/_am gesetzt.
- Kritische Prüfung (2026-09-22) B1/B2/S1 **geklärt + in den Rumpf gezogen**: Freigabe-Antworten 1–3
  gefüllt, FA 5 + AK 9-Korrektur + **AK 10** (manuelle Werkbank überlebt Sync) ergänzt. Kein Blocker.
- Datei lag in `entwurf/` mit status Freigegeben → nach `freigegeben/` gezogen (git mv, Basename bleibt).
- Keine View-Änderung → frontend-design nicht nötig.

## Freigabe-Entscheidung (verbindlich)

Werkbank vorerst **nur je Arbeitsgang**, keine automatische Werkbank am Auftrag (Nachrüstung offen);
manueller Weg `FaCompletion.SetWorkplace` **bleibt und wird nach dem Rückbau nicht mehr überschrieben**
(gewollte Verhaltensänderung, AK 10); Zielort-Spalte = eigener Umfang; ADR 0014 = **additiver Nachtrag**.

## Auftrag (Rückbau, Rumpf = maßgeblich)

**Code (Worktree):**
- `FaMaterializationSyncService.cs`: Klassenkommentar 2→1 Z1-Ausnahme; Ctor-Param `IUnknownWorkplaceState`;
  Workplace-Lookup-Block; beide `ApplyWorkplace`-Aufrufe (Anlege-/Update-Pfad); Methode `ApplyWorkplace` +
  Record `WorkplaceRef`; `SendUnknownWorkplaceDigestAsync`; Unbekannt-/Mehrdeutig-Meldungen; die 4
  Werkbank-Counts-Keys an allen 3 `FinishSuccessAsync` + Log-Statement; Counter-Variablen — alles entfernen.
  `ServiceSettings`/`IMailService` (SendMissingDigest) bleiben.
- `FaMaterializationPlanner.cs`: `MaterializationSourceOrder.Arbeitsbereich` entfernen; Erzeugungsaufruf kürzen.
- Löschen: `Common/IUnknownWorkplaceState.cs`, `Tests/.../FaMaterializationWorkplaceTests.cs`.
- `Program.cs`: DI `IUnknownWorkplaceState` entfernen (NICHT `IUnknownWorkStepTokenState`!).
- Tests `FaMaterializationSyncServiceTests`/`PlannerTests` gegen geänderte `MaterializationSourceOrder`-Signatur.
- Version 1.43.0 ×2, Anwender-Changelog (als Korrektur/Rückbau).

**Brain (Hauptcheckout):** ADR 0014 **additiver Nachtrag**, fallstricke-Eintrag, feature-map (v1.37-Zeile),
v1.37-Spec additiver Hinweis (AK 9/10 dort überholt), codebase/services bereinigen, TESTSZENARIEN +
testszenarien-index, Brain-Changelog.

## Arbeitsstand

- 2026-09-22: Spec Freigegeben → InUmsetzung, nach freigegeben/ gezogen. Vollständige Service-Datei gelesen. Rückbau startet.
- 2026-09-22: **feature-complete.** Werkbank-Ableitungsblock aus `FaMaterializationSyncService` komplett
  entfernt (14 präzise Edits), `MaterializationSourceOrder.Arbeitsbereich` raus, `IUnknownWorkplaceState.cs`
  + `FaMaterializationWorkplaceTests.cs` + `UnknownWorkplaceStateTests.cs` gelöscht, `Program.cs`-DI raus.
  **Über die Spec hinaus gefunden (am Code):** `FaMaterializationCoatingWriteTests`/`SyncServiceTests`
  konstruieren den Service mit dem entfernten Ctor-Param → Mock-Arg entfernt; `FaMaterializationPlannerTests`-
  Helper `Src` baut den Record → `null`-Arg entfernt; `IUnknownWorkStepTokenState`-cref auf gelöschten Typ
  entschärft; `UnknownWorkplaceStateTests` = „der eigene Test" (Spec) mitgelöscht.
  Build 0 Fehler, **Web 1391 +1 skip, Service 263** grün (−14 gelöschte Workplace-Tests; Coating/Planner/
  Sync regressionsgrün). Version **1.43.0** ×2, Anwender-Changelog (als Korrektur).
  Brain: **ADR-0014-Nachtrag** (additiv), [[fallstricke]] §15, Brain-Changelog, feature-map (v1.37-Hinweis +
  v1.43-Zeile), v1.37-Spec additiver Warn-Callout (AK 9/10 überholt), codebase/services, testszenarien-index,
  TESTSZENARIEN TS-71 (71.6–71.9 markiert, 71.14/71.15 neu).
- **Offen:** qa-agent (setzt `Testbereit`), dann Schranke 2 mit dem ganzen Bündel. Kein Merge/Push.
- 2026-09-22: **qa-agent — Testbereit.** Build 0 Fehler; Tests Web 1391+1 skip / Service 263 grün
  (bestätigt gegen Diff `ab10b57..0aceee8`). AK 1–8 statisch/testbar nachgewiesen (Grep + Diff-Lesung
  + Testlauf), AK 9/10 als Manual-UAT bestätigt über TS-71.14/71.15. Code-Review ohne Findings.
  Deploy-Flags (web/service=true, migration=false) gegen den echten Diff bestätigt, unverändert zum
  provisorischen Stand. Spec-Frontmatter → `Testbereit`, QA-Nachweis + Manual-Checkliste angehängt.
  `feature-map.md` v1.43.0-Zeile → `Testbereit`. Wartet auf Schranke 2 (Mensch).
