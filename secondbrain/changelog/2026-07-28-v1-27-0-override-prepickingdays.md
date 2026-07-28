---
type: changelog
version: 1.27.0
date: 2026-07-28
---
# v1.27.0 — Werkbank-Override „Abweichende Vorkommissioniertage" wird wirksam

- Spec [[2026-07-28-override-prepickingdays]], **Variante A** (integrieren statt entfernen),
  Freigabe durch den Menschen an Schranke 1. Backlog-Ursprung:
  `../backlog/2026-07-28-override-prepickingdays.md`.
- **Prioritaetsregel an genau EINER Codestelle**: neuer `Services/PrePickingDaysResolver.cs`
  (`Resolve(int? workplaceOverride, int globalDays)` + `IsOverrideActive`). Werkbank-Wert gewinnt,
  sobald `HasValue` — **`0` ist ein gueltiger expliziter Override**, kein Synonym fuer „Standard"
  (gleiche Semantik wie `ColumnMap["pre-picking-days"]` in der Werkbank-Liste).
- Genutzt von allen drei Listen, die den Vorkommissioniertermin rechnen:
  `ProductionOrdersController.Index`, `PickingLeitstandController.Index`,
  `FaWorklistController.Index`. Der Wert kommt fuer die ersten beiden aus der erweiterten
  Projection `LeitstandOrderRow.WorkplaceOverridePrePickingDays` (eine Repository-Methode bedient
  beide Controller), fuer FaWorklist aus der bereits `Include`-geladenen Navigation.
- **Kaskade beabsichtigt:** `CoatingDateCalculator` setzt auf dem BG-Termin auf → der
  Beschichtungstermin verschiebt sich automatisch mit, ohne Code-Aenderung dort.
- UI-Rueckmeldung pro Zeile (Freigabe-Antwort 3 + 5): Badge „BG X" + Tooltip an der Werkbank-Zelle
  in **allen drei** Listen; Spaltenkopf-Tooltip „BG-Termin" von der fixen Zahl auf „Standard X,
  je Werkbank abweichend" umgestellt (die fixe Zahl war ab jetzt zeilenweise falsch).
- **Keine Migration, kein SQL-Skript, kein Schema-Diff** — reine Lese-/Verrechnungslogik.
  `SQL/00_FreshInstall.sql` und `SQL/AgentJobs/*` unberuehrt.
- **Deploy-Risiko (fachlich, nicht technisch):** Das Feld war seit
  `20260306081711_AddProductionWorkplaces` wirkungslos. Bestandsdaten-Pruefung des Menschen ergab
  genau **eine** betroffene Werkbank: `Id=1, Name=A1, OverridePrePickingDays=7`. Mit dem Deploy
  springt der BG-Termin aller FAs an `A1` von „Komm. − 1 AT" auf „Komm. − 7 AT" (und der
  Beschichtungstermin entsprechend mit). Vor dem Deploy mit der Fertigung abstimmen — wer die alte
  Wirkung will, leert das Feld an `A1`.
- Tests: `PrePickingDaysResolverTests` (Regel isoliert) + je Controller Override/Standard/`0`
  (Leitstand zusaetzlich die Beschichtungs-Kaskade). Testszenarien **TS-9.6 – TS-9.10** in
  `../../docs/TESTSZENARIEN.md`, Kapitel 9.
- Aufloest den Fallstrick-Eintrag „`OverridePrePickingDays` ist wirkungslos" in [[fallstricke]]
  und Zeile 141 in [[feature-map]].
- Branch `feature/override-prepickingdays` (Worktree `.claude/worktrees/override-prepickingdays`).
  Schranke 2 (manueller Test) am 2026-07-28 genommen, **gemerged nach `main`** mit
  `--no-ff`, Merge-Commit `0548449`. Nachweis **auf dem Merge-Commit** (= Deploy-Stand):
  Build 0 Fehler, Tests 1024 (Web) + 176 (Service) gruen, 0 Fehler, 1 uebersprungen
  (`ProductionOrderEagerCreateAgentJobTests` — braucht echten SQL Server, kein InMemory).
- **Offen nach dem Merge:** `git push` (macht der Mensch), Deploy Web + Service, danach erst
  Worktree/Branch aufraeumen. Vor dem Deploy die Abstimmung mit der Fertigung zu `A1` (siehe
  Deploy-Risiko oben).
