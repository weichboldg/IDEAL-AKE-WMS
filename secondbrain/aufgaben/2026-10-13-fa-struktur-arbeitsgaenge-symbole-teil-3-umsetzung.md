---
typ: notiz
spec: "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-3-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: FA-Struktur Arbeitsgänge — Teil 3 (BDE-Terminal)

Spec [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-3-spec]] · **direkt im Bündel**, Start `d3746373` (v1.56.0).
BDE-Terminal „Arbeitsgang wählen“: Blöcke je Auftrag, Sortierung nach Fertigungstermin, Status aus Teil 2, fremd gebuchte
Arbeitsgänge sichtbar als „läuft“. Version **1.57.0**, TS-90. Keine Migration. Deploy: Web.

## Stand

- 2026-10-13: Commits `4c5d4e66`, `d4f326ef` (feature-complete). Build 0 Fehler, Web 1845 / Service 272 grün. Web-App nicht
  lokal gestartet.
- 2026-10-13: qa-agent → **Testbereit**, keine Critical/Important, kein Fix nötig; Minor M1 (AKE-Prüfabfrage
  `FaHierarchyOrderInfos` = 0 in der Checkliste), M2 (`label` unescaped, im Normalmodus nicht mehr angezeigt), M3 (AK 10 nur per
  Review → TS-90.10), M4 (CS8601 im Test, kosmetisch). Build 0 Fehler, Web 1845 / Service 272 grün.
- Schranke 2 mit dem Bündel: Vorab P-T1 (inkl. Kollision) + P-T2, dann TS-90 am Testsystem mit `BdeAktiv = true`, IDEAL und AKE.
