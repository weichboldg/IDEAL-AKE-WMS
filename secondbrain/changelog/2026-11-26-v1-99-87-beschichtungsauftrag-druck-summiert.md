---
type: changelog
version: 1.99.87
date: 2026-11-26
---
# v1.99.87 — Beschichtungsauftrag: Druck und PDF immer summiert je Artikel

Spec [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-1-spec]] (Bündel 1 Teil 1), Lauf-Notiz
[[2026-11-26-buendel-1-lagerbestellung-goal-lauf]]. Worktree `feature/2026-08-07-ideal-teile-1-5`, Commit `e67e9f6f`.
**Keine Migration, keine neue Einstellung.** TS-205 (TS-162.14 überholt). Nicht gemergt.

## Kernpunkte

- Eine reine Funktion `BeschichtungsauftraegeDruck.Summiert`: gruppiert je (Artnr getrimmt, ohne FA), Pos. 1..n, Positionen
  ohne Artnr je eigene Zeile, freie Position zuletzt; Druck und PDF teilen das Modell.
- Verpackt ☑/☐/◩ „n von m verpackt“, „Maße uneinheitlich“ als zweite Zeile der immer sichtbaren Verpackt-Zelle, Summenzeile
  „Summe Teile im Auftrag x (m Positionen)“ im `tfoot` (nur am Ende). Kehrt Antwort 11 aus
  [[2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck-spec]] um.

## Deploy-Risiken

- Nur Web. ◩ im PDF hängt an Server-Schriften (DEV: Segoe UI Symbol eingebettet) — TS-205.7 auf dem Testsystem prüfen.
- QA-Testdaten „QA TS-205“ (CreatedBy QA): DEV AKE BA-5/6, DEV IDEAL BA-12/13.
