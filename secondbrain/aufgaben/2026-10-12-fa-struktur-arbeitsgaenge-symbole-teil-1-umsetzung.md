---
typ: notiz
spec: "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: FA-Struktur Arbeitsgänge als Symbole — Teil 1 (reine Anzeige)

Spec [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec]] · **direkt im Bündel** (Entscheidung Gerald
2026-10-09), Start `5764c664` (Endstand Beschichtungsauftrag Teil 4, v1.51.0). Sage-Kürzel aus
`FaHierarchyNode.Arbeitsschritte` als Symbole in den Baumzeilen von `/FaHierarchy`; kein Status, Modal unverändert.
Deploy: Web, keine Migration.

## Stand

- 2026-10-12: Umsetzung läuft. Web-App nicht lokal starten (Auto-Migration gegen AKESQL20). Prüfschritt P-K1 der Spec
  (Feldgleichheit Sage ↔ WMS) liegt nicht vor — ändert den Bau nicht, bleibt Teil der Abnahme (TS S1a).
- 2026-10-12: Umsetzung fertig, Commits `f1a447cb`, `1c86978d`, `8f15833c`, QA-Fix `d6d6beb0`. QA ohne Critical/Important (M2/M3 behoben, M1 vorbestehend: gedimmte Kontextzeilen beim Spaltenfilter unter WCAG AA für alle Zellen) → **Testbereit**. Build + 1636 Web- / 272 Service-Tests grün. Changelog [[2026-10-12-v1-52-0-fa-struktur-arbeitsgaenge-symbole]].
- Schranke 2 mit dem Bündel: TS-85, vorher P-K1 (Sage ↔ WMS) ausführen.
