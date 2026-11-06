---
typ: notiz
spec: "[[2026-11-06-lagerplatz-etikett-rolle-und-vormontage-felder-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: Lagerplatz-Etikett 93 × 55 mm und Vormontage-Felder

Spec [[2026-11-06-lagerplatz-etikett-rolle-und-vormontage-felder-spec]], direkt im Bündel. Start `6969620a` (v1.71.1), Ziel **v1.72.0**.
Keine Migration. Deploy: nur Web.

## Plan (kompakt)

**Teil 1 (eigener Commit):**
1. `AppSettingKeys.LagerplatzEtikettRolle93x55` (Default aus, nur exakt "true"), Katalogfeld Bool in neuer Gruppe „Druck“ + `GroupOrder`,
   Key in `/Settings` Gruppe „Lager“.
2. `PrintLabels.cshtml` liest den Wert per `@inject IAppSettingRepository`; A4-Block unverändert, Rolle = Klasse `labels-rolle`
   (`@page 93mm 55mm`, ein Etikett je Seite über `.label + .label { break-before: page }`, ohne Rahmen, Zone schwarz) + Treiber-Hinweis
   in der Bildschirmleiste.
3. Katalog-Test, Hilfe-Satz.

**Teil 2:**
4. `VormontageService`: Formatierer Kopfwerte (Kunde, FE-Termin), Sicht-1-Map je Request (`BuildEinzelnColumnMap(lookup)`),
   Sicht 2 Positions-Vorfilter vor Aggregation + Anzeige-Listen im ViewModel.
5. Register `fe-termin` = Datum für beide Views, Views (th/td/column-config/colCount), `ColumnDefinitions`, Untertitel „VM-Bedarfs“,
   Kommentare „Arbeitsbereich“ → „VM-Bedarf“.
6. Tests (Service, Drift-Wächter A/B/C + ServiceColumnMap Sicht 1), TS-Kapitel, Changelog, v1.72.0 ×2, Hilfe.
7. qa-agent.

Stand: Testbereit (QA 2026-11-06, Worktree `48349786`, `43a3988f`, QA-Fix `d91b1d1d` README). Offen: Manual-UAT TS-104 am Rollendrucker, TS-105 am IDEAL-Testsystem.
