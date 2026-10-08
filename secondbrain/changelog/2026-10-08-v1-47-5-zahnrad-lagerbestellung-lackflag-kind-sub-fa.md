---
type: changelog
version: 1.47.5
date: 2026-10-08
---
# v1.47.5 — Zahnrad Lagerbestellung-Details + Lack-Flag: Kind-Sub-FA zählt nur für sich

Zwei Bugfixes im **Bündel-**Worktree `feature/2026-08-07-ideal-teile-1-5`, Commit `7e78f9a6`. **Keine Migration,
kein Einmal-Skript.**

## Fix 1 — Zahnrad auf der Lagerbestellung-Detailseite

- Befund: [[2026-10-06-lagerbestellung-zahnrad-unsichtbar]] — `#incomplete-alert` (`d-none`) stand direkt vor
  `.table-responsive`; das Zahnrad hing darin (unsichtbar) und wurde beim Einblenden per `textContent` gelöscht.
- Fix in `Views/WarehousePicking/Details.cshtml`: leerer Toolbar-Host als direkter Vorgänger der Tabelle, Hinweis
  darüber. Wächter `WarehousePickingDetailsZahnradHostTests` (vorher rot), TS-18.10 ergänzt, [[fallstricke]] §19.
- Betrifft auch **AKE-Produktion** (`main`, seit Hotfix v1.30.1). Dieser Fix kommt erst mit dem Bündel-Merge
  nach AKE; ein eigener Hotfix auf `main` ist nicht gemacht (Entscheidung offen im Backlog-Eintrag).

## Fix 2 — Lack-Flag: Kind-Sub-FA markiert den Vater nicht mehr

- Entscheidung Gerald 2026-10-07: zum Lackierer gehen nur die Teile, nicht der Korpus. FREIGABE-NACHTRAG in
  [[2026-08-20-materialisierung-fachliche-felder-spec]]; gleiche Regel wie
  [[2026-10-07-v1-47-4-arbeitsgang-erkennung-kind-sub-fa]].
- `FaMaterializationCoating.Derive`: nur direkte **Blatt**-Kinder (`SubFA == 0`) markieren den Vater-Sub-FA.
- Wirkung: `HasCoatingParts` → Beschichtungs-Kennzeichen, Beschichtungstermin und „Beschichtung erledigt“ in
  FA-Liste, Kommissionier-Leitstand, FA-Abarbeitungsliste. Beschichtungsauftrag (liest `Beschichtet` am Knoten)
  unberührt. TS-71.10a, [[fallstricke]] §20.

## Deploy-Risiken

Web **und** Service (Fix 2 liegt im Service). Nach dem Service-Deploy kippt beim nächsten Materialisierungslauf
das Flag an betroffenen Vater-Sub-FAs auf `false`; ein dort manuell gesetztes „Beschichtung erledigt“ wird dabei
zurückgesetzt (gewollt, Fallstrick #11). Vorab zählen: Fundabfrage in TS-71.10a.
