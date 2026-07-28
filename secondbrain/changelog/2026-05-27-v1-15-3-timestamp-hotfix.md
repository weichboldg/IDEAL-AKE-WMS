---
type: changelog
version: 1.15.3
date: 2026-05-27
---
# v1.15.3 — Hotfix: Protokoll-Timestamp in Lokalzeit statt UTC

- Symptom (User-Report): „das Aktivitaets-Protokoll schreibt nicht mehr".
- Ursache: `SyncRun.WriteEntryAsync` setzte `DateTime.UtcNow`, der Model-Default ist aber
  `DateTime.Now`, und die UI zeigt ohne UTC-Konversion an. Neue Eintraege erschienen dadurch in der
  DESC-Sortierung zwei Stunden „frueher" und verschwanden unter aelteren.
- Fix: das Timestamp-Assignment wurde entfernt — der Model-Default greift (`a2a3275`).
- Bestehende UTC-Timestamps zwischen v1.15.0 und v1.15.3 bleiben in der DB.
- Im selben Zug: Worktree-Konvention „groessere Aenderungen in eigenem Worktree" verankert und die
  ctor-Position von `ISyncLogger` in allen 11 Services vereinheitlicht.
- Dauerregel dazu: [[fallstricke]] und
  [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]].
