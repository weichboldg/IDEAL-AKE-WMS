---
typ: notiz
spec: "[[2026-10-06-beschichtungsauftrag-ausbau-teil-2-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: Beschichtungsauftrag-Ausbau Teil 2 — Briefkopf / Corporate-Design-Druck

Spec [[2026-10-06-beschichtungsauftrag-ausbau-teil-2-spec]] · **direkt im Bündel-Worktree** (Entscheidung Gerald
2026-10-09, kein eigener Zweig), Start auf `cc86d373` (v1.48.0). Deploy: Web, keine Migration, manuelle Einrichtung
`Branding\` + Einstellung „Briefkopf-Vorlage (Pfad)".

## Stand

- 2026-10-09: Umsetzung läuft.
- 2026-10-09: umgesetzt (Commits `1c560d30`, `1cda91dc`, `742f7855`, QA-Fix `020bf8af`), Build + 1521 Web- / 272
  Service-Tests grün, QA (qa-agent) + Nachlauf ohne Critical/Important, **Testbereit**. Changelog
  [[2026-10-09-v1-49-0-briefkopf-beschichtungsauftrag]].

## Offen

- Schranke 2 mit dem Bündel: Manual-UAT TS-82 (inkl. Einrichtung `Branding\` + Einstellung, Prüfschritt TS-82.1).
- [[2026-10-09-briefkopf-seitenzaehlung]] — „Seite X von Y" bewusst weggelassen.
- [[2026-10-09-briefkopf-csp-und-bereinigung]] — CSP-Gürtel, Grenzen der Regex-Bereinigung (QA M-1, M-11).
