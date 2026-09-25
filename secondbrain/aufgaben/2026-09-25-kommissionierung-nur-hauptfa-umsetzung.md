---
typ: notiz
spec: "[[2026-09-25-kommissionierung-nur-hauptfa-spec]]"
status: InUmsetzung
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

- [ ] Plan
- [ ] Sub-FA-Definition (`IsHauptFa`/`IsSubFa`) + Warteschlange + Guards + Tests
- [ ] Rückbau Freigabe-Kaskade + grep-Nachweis
- [ ] Views Leitstand/FA-Liste (HauptFA-Beschränkung)
- [ ] Stückliste „Alle Ziele“ + Druck-Leerzustand
- [ ] Normalisierung Profil/Benutzerverwaltung + Tests
- [ ] Version, Changelog, Hilfe, TS-79, Brain

## Verlauf

- 2026-09-25: InUmsetzung gesetzt (Worktree/Branch eingetragen).
