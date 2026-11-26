---
type: bug
title: "Leitstand: Kaskaden-Dialog „Alle Sub-FAs fertigmelden“ zählt die HauptFA-Zeile (und stornierte Zeilen) als Sub-FA"
status: offen
severity: niedrig
created: 2026-11-26
affected_code:
  - IdealAkeWms/Controllers/PickingLeitstandController.cs:713-722 (CascadeDonePreview, totalSubFas = CountByOrderNumberAsync)
  - IdealAkeWms/Controllers/PickingLeitstandController.cs:731-753 (CascadeDone, Log und Erfolgsmeldung „n Sub-FA(s)“ = alle geschriebenen Zeilen)
  - IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs:250-253 (CountByOrderNumberAsync, alle Zeilen der OrderNumber inkl. HauptFA und stornierter)
  - IdealAkeWms/Data/Repositories/ProductionOrderBdeStatusRepository.cs:32-61 (SetIsDoneBdeForOrderNumberAsync, schreibt alle Zeilen der OrderNumber inkl. HauptFA und stornierter)
  - IdealAkeWms/Views/PickingLeitstand/Index.cshtml:250 (Gruppen-Badge „n Sub-FAs“ = group.Items.Count inkl. HauptFA-Zeile) und :153 (Zählzeile „y Sub-FAs“ = HierarchicalRowCount inkl. HauptFA-Zeilen)
source_backlog: "[[2026-11-26-leitstand-kaskaden-zaehler-ohne-hauptfa]]"
spec: "[[2026-11-26-leitstand-kaskaden-zaehler-ohne-hauptfa-spec]]"
---

## Symptom

DEV IDEAL, HauptFA 1064052, Leitstand ohne „Sub-FAs anzeigen“: Die Zeile zeigt „+ 38 Sub-FAs“, der Bestätigungsdialog von „Alle Sub-FAs
fertigmelden“ nennt „39 Sub-FA(s)“ (QA-Befund F-1 der Spec [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-2-spec]],
Abnahmezeile TS-208.4). Vorbestehend seit der Kaskade (v1.35.0, Etappe C `8806016`), sichtbar erst seit v1.99.88, weil „+ n“ jetzt daneben steht.

## Reproduktion

1. DEV IDEAL, `/PickingLeitstand`, Schalter „Sub-FAs anzeigen“ aus.
2. Zeile eines HauptFA mit Sub-FAs suchen (z. B. 1064052), Zahl „+ n Sub-FAs“ in der FA-Nr.-Zelle notieren.
3. „Alle Sub-FAs fertigmelden“ in den Zeilen-Aktionen klicken → Dialog zeigt n + 1 (bei stornierten Sub-FAs entsprechend mehr). Abbrechen.
4. Mit „Sub-FAs anzeigen“: Gruppen-Badge „n Sub-FAs“ zählt die HauptFA-Zeile ebenfalls mit.

## Root Cause

Drei Zählungen mit verschiedener Basis (belegt im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, v1.99.89):

| Stelle | Basis |
|---|---|
| „+ n Sub-FAs“ (`_PickingLeitstandRow.cshtml:52-56`, `LeitstandHauptFaZusammenfassung.cs:26`) | nicht stornierte Zeilen ohne die HauptFA-Zeile (`ProductionOrderRepository.cs:197-208` `!IsCancelled`, Controller `:250-251` `z.Id != r.Id`) |
| Dialog „n Sub-FA(s)“ (`PickingLeitstandController.cs:718`) | `CountByOrderNumberAsync` = **alle** Zeilen der `OrderNumber` (`ProductionOrderRepository.cs:250-253`), also HauptFA-Zeile + stornierte |
| Erfolgsmeldung/Log „n Sub-FA(s)“ (`:744-750`) | Anzahl geschriebener `ProductionOrderBdeStatus`-Zeilen aller Zeilen der `OrderNumber` (`ProductionOrderBdeStatusRepository.cs:37-46`, `:60`) |

Die Kaskade **setzt die HauptFA-Zeile selbst mit** (`IsDoneBde = true`) und ebenso stornierte Zeilen: der Filter ist nur
`o.OrderNumber == orderNumber` (`ProductionOrderBdeStatusRepository.cs:37-40`). Die Repository-Tests legen nur Zeilen ohne `SubOrderNumber` an
(`ProductionOrderBdeStatusRepositoryTests.cs:49-52`, `TestDataHelper.cs:42-49` setzt keine), die nach `ProductionOrder.IsHauptFa`
(`ProductionOrder.cs:29-30`) alle als HauptFA gelten; die Unterscheidung HauptFA/Sub-FA prüft kein Test.

Folge heute: nur eine falsche Zahl. `IsDoneBde` hat im Code **keinen Leser** außer Tests: Suche `IsDoneBde` in `*.cs` außerhalb `Migrations/`
findet nur Schreiber (`BdeStatusApiController.cs:41`, `ProductionOrderBdeStatusRepository.cs:25`/`:52`), Anlage (`SageImportService.cs:212`) und
Tests; in `*.cshtml`/`*.js` nur Kommentare und den Toggle-Endpunkt (`Index.cshtml:570`). Kein Datenschaden.

## Fix / Verweis

Spec [[2026-11-26-leitstand-kaskaden-zaehler-ohne-hauptfa-spec]] (Entwurf, Rückfragen zur Basis: HauptFA-Zeile mitmelden, stornierte Zeilen).
