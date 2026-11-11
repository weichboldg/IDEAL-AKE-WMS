---
typ: notiz
spec: "[[2026-10-06-beschichtungsauftrag-ausbau-teil-1-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: Beschichtungsauftrag-Ausbau Teil 1 — Summierte Ansicht (v1.48.0)

Spec [[2026-10-06-beschichtungsauftrag-ausbau-teil-1-spec]] · eigener Worktree, **abgezweigt vom Bündel**
`feature/2026-08-07-ideal-teile-1-5` (Stand v1.47.5, `f102fbf5`) — die Beschichtungsansicht existiert nur im
Bündel. Merge erst nach Schranke 2 des Bündels. Deploy: Web, keine Migration.

## Stand

- 2026-10-09: Worktree angelegt, Umsetzung läuft.
- 2026-10-09: umgesetzt (Commits `850258e3`, `578aec6b`, `c226a465`, `8d9cefd2`, `cc86d373`), Build + 1474 Web- /
  272 Service-Tests gruen, QA (qa-agent) ohne Critical/Important, **Testbereit**. Changelog
  [[2026-10-09-v1-48-0-beschichtungsauftrag-summiert]].
- 2026-10-09 per Fast-Forward ins Buendel uebernommen (`cc86d373`); Buendel-HEAD jetzt `cc86d373` (v1.48.0), Build + 1474 Web- / 272 Service-Tests gruen.
  Zweig `feature/2026-10-06-beschichtungsauftrag-ausbau-teil-1` + Worktree bleiben bis zur Freigabe bestehen.

## Offen

- Schranke 2: Manual-UAT TS-81 (inkl. Pruefschritt Mengeneinheit TS-81.1 auf der Sage-DB IDEAL) — mit dem Buendel
  (Teil 1 ist seit 2026-10-09 im Buendel enthalten).
- Nachlese beider Summierungen (Kommissionierliste + Beschichtung): leere Artnr wird je HauptFA zusammengefasst,
  Artnr-Gruppierung case-sensitiv, Locked-Spalten bei explizitem `visibleColumns` nicht erzwungen (QA M2/M4).
- Doppelte Spec-Datei: `specs/entwurf/` und `specs/freigegeben/` enthalten beide
  `2026-10-06-beschichtungsauftrag-ausbau-teil-1-spec.md` (beim Freigeben kopiert statt verschoben) — die Entwurfs-Kopie
  entfernen (Mensch), sonst loest der Wikilink mehrdeutig auf.
