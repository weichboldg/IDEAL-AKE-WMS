---
typ: notiz
spec: "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: FA-Struktur Arbeitsgänge als Symbole — Teil 2 (Status)

Spec [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]] · **direkt im Bündel**, Start `d56a340e` (v1.54.0; der
Menü-Lauf lief vorher nicht). Status je Arbeitsgang-Kürzel im FA-Baum bei BDE global + Werkbank aktiv, Sammelhinweis
„nicht konfiguriert“, Kontextzeilen ohne `opacity` (Antwort 11 = C), FA 4a leerer Offset abgewiesen. Version **1.55.0**,
TS-88 (+ TS-87.3). Keine Migration. Deploy: Web.

## Stand

- 2026-10-13: Commits `539be27f` (FA 4a), `e33cdc83` (Repository), `7690d281` (Status-Funktion), `6d85810b`
  (Controller/View/CSS), `99a3b09e` (feature-complete: Version, Changelog, Hilfe, TS-88). Build 0 Fehler, Tests Web
  1826 (+1 übersprungen), Service 272 grün. Web-App nicht lokal gestartet (Auto-Migration gegen AKESQL20).
- Erledigt damit: [[2026-10-12-operationconfig-offset-leer-pruefen]] (FA 4a).
- 2026-10-13: qa-agent → **Testbereit**, keine Critical/Important; Minor M1 (TS-88.12 nachgetragen, `57b762a9`), M4
  (`AppVersion.Date`, `627c992a`) behoben, M2/M3/M5/M6 akzeptiert (siehe Spec). Build 0 Fehler, Web 1826 / Service 272 grün.
- Schranke 2 mit dem Bündel: TS-88 am Testsystem mit `BdeAktiv = true`, Kontrastmessung TS-88.10, Query-Zählung TS-88.11.
