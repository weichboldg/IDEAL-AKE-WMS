---
typ: notiz
spec: "[[2026-09-25-kommissionierung-nur-hauptfa-spec]]"
status: Testbereit
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Umsetzung: Kommissionierung nur am HauptFA

Spec [[2026-09-25-kommissionierung-nur-hauptfa-spec]] · **bestehender Bündel-Worktree** (kein neuer, laut
Spec-Hinweis „Umsetzungsort“ und Auftrag).

## Pre-Flight-Verdikt (2026-09-25)

- status Freigegeben, open_questions [], freigabe_entscheidung/_von/_am gesetzt.
- Kritische Prüfung: B1 (Antworten leer) erledigt, B2 durch Antwort 3 beantwortet, B3 durch die Antworten auf
  den zweiten Prüfdurchgang beantwortet. Kein offener Blocker.
- FREIGABE-NACHTRAG gilt vorrangig: `Normalize` liefert `DroppedTargets` (Hinweis nur bei verworfenen
  Einzelzielen); keine eigene Formel in Views (`!item.IsSubFa` bzw. ViewModel-Feld aus `IsSubFa`).
- Worktree-Stand: AppVersion 1.45.0 → dieser Lauf **1.46.0**; TS-78 → **TS-79**; keine Migration.

## Stand

- [x] Plan
- [x] Sub-FA-Definition (`IsHauptFa`/`IsSubFa`) + Warteschlange + Guards + Tests
- [x] Rückbau Freigabe-Kaskade + grep-Nachweis
- [x] Views Leitstand/FA-Liste (HauptFA-Beschränkung)
- [x] Stückliste „Alle Ziele“ + Druck-Leerzustand
- [x] Normalisierung Profil/Benutzerverwaltung + Tests
- [x] Version, Changelog, Hilfe, TS-79, Brain
- [x] QA (Build, Tests, harte Prüfungen, AK-Verifikation) — Testbereit

## Verlauf

- 2026-09-25: InUmsetzung gesetzt (Worktree/Branch eingetragen).
- 2026-09-25: QA abgeschlossen — Build grün, 1684 Tests grün (0 rot), alle harten Prüfungen bestanden
  (grep CascadeRelease/SetReleaseForOrderNumber/CountReleasedByOrderNumber 0 Treffer im Quelltext,
  SetReleaseBatchAsync-/BulkRelease-Fixtures unverändert, eine Sub-FA-Formel, AppVersion 1.46.0
  beidseitig). Eine bekannte, akzeptierte Abweichung dokumentiert (FaWorklist-Link nur mit
  Vorbau-Zugriff, Backlog [[2026-09-25-leitstand-subfa-readonly-stueckliste]]). Status → Testbereit,
  QA-Nachweis + manueller Testplan an der Spec angehängt. Wartet auf Schranke 2 (Mensch: Manual-UAT
  am Testsystem inkl. einmaligem SQL-Lauf, dann Merge mit dem Bündel).
