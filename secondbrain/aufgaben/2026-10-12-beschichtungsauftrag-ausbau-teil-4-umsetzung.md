---
typ: notiz
spec: "[[2026-10-06-beschichtungsauftrag-ausbau-teil-4-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: Beschichtungsauftrag-Ausbau Teil 4 — Oberfläche (Vorschlagsliste, Reklamation, Druck)

Spec [[2026-10-06-beschichtungsauftrag-ausbau-teil-4-spec]] · **direkt im Bündel** (Entscheidung Gerald 2026-10-09),
Start `a5b27550` (v1.50.0). Oberfläche auf dem Teil-3-Workflow ([[2026-10-06-beschichtungsauftrag-ausbau-teil-3-spec]]),
Briefkopf aus Teil 2. Deploy: Web, keine Migration; neue Schreibrolle zuweisen.

## Stand

- 2026-10-12: Umsetzung läuft. Web-App nicht lokal starten (Auto-Migration gegen AKESQL20).
- 2026-10-12: Umsetzung fertig, Commits `3b5149af`, `8d60cb10`, `17bbf3be`, `5af265f4`, `91c77105`, QA-Fix `5764c664`. QA ohne Critical/Important (M1/M4 behoben, M2/M3/M6 akzeptiert) → **Testbereit**. Build + 1619 Web- / 272 Service-Tests grün. Changelog [[2026-10-12-v1-51-0-beschichtungsauftraege-oberflaeche]].
- Schranke 2 mit dem Bündel: TS-84 (inkl. Abschluss TS-83.6); Deploy-Schritt Rolle `beschichtungsauftrag_schreiben`.
- Offen für den Menschen: Zusatzfunde Z1 (Zahnrad-Host Einzelliste) und Z2 (Server-Kultur bei `decimal`-Formularen) der Spec — eigene Entscheidung, nicht mitgebaut.
