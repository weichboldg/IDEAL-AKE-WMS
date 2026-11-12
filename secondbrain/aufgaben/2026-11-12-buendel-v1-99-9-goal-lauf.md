---
type: aufgabe
status: offen
created: 2026-11-12
---
# /goal-Lauf 2026-11-12 (2): System-Kürzel Sage-Kennung, Lieferverzug-Mail, Barcode + CSV

Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, Start v1.99.8 `bb788f0e`.
Auftrag Gerald 2026-11-12 (Chat, /goal-run). Reihenfolge laut Auftrag; Versionen, SQL-Nummern und TS-Kapitel in Bau-Reihenfolge
(löst die Kollision v1.99.9/SQL 144/TS-128 der Entwürfe auf). Grenzen: keine Mail an externe Adressen (DEV nur Testbetrieb bzw.
Datei/Testpostfach), kein POST nach Sage, kein Schreiben auf echte UNC-Pfade außer einem DEV-Ordner, kein Lauf gegen Zielsysteme,
kein Push, kein Merge.

| Spec | Typ | SQL-Block | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]] (nur Freigabe-Nachtrag 14:20 System-Kürzel) | Epic (Nachtrag) | SQL/144 | 1.99.9 | **Testbereit** (QA 2026-11-12) | `115e8a09`, `c47f0136`, QA `4c1b5523`; Brain `443ee5fc`, QA `0aa7891c` | – |
| [[2026-11-12-offene-bestellungen-mahnmail-lieferverzug-spec]] | Spec | offen | 1.99.10 | InUmsetzung | – | – |
| [[2026-11-12-wareneingang-barcode-lieferschein-csv-export-spec]] | Spec | offen | 1.99.11 | InUmsetzung | – | – |

- Vorprüfung 2026-11-12: Teil 2 stand auf Testbereit, der Auftrag nennt den Nachtrag ausdrücklich (ersetzt die Freigabe). Mahnmail und
  Barcode/CSV: Freigegeben, alle Freigabe-Antworten ausgefüllt, kein offener BLOCKER, Freigabe-Nachtrag Mahnmail (Antwort 7/12) gilt vor dem Rumpf.
- v1.99.9: SM# hat dasselbe Problem → gleiches Kürzel (`SM#<K>#<id>#`), Kennung je Queue-Eintrag gespeichert. Kürzel muss mit Buchstaben beginnen (enger als Nachtrag, begründet). DEV AKE/IDEAL deployt, `Sync:SageSystemKuerzel = DEV`, 5 Sync-Schalter true. Publish v1.99.9 `4c1b5523`.
