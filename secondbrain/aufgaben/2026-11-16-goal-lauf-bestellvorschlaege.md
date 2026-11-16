---
type: aufgabe
status: offen
created: 2026-11-16
quelle: Gerald, Chat 2026-11-16 (/goal + Dev-Lauf-Auftrag)
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-16: Bestellvorschläge Nachbesserung

Auftrag Gerald 2026-11-16: [[2026-11-16-bestellvorschlaege-nachbesserung-spec]] im Bündel-Worktree (Stand v1.99.29). Spec liegt unter
`specs/entwurf/` mit `status: Freigegeben` – laut Auftrag gilt sie als freigegeben. Rumpf vorher nachgezogen (Antworten 1–8, Nachtrag 19:49;
`5099cce1`). Grenzen: nichts an Sage, keine Einstellungen auf Test/Prod, kein Merge, kein Push. Publish am Ende.

Nummern beim Start: Version 1.99.29 → **1.99.30** (1.99.27 reserviert Mapping); SQL: letzte 152, 151/153/154 reserviert → **SQL/155**;
TS: letzte 153 → **TS-154**. Parallel läuft im selben Worktree der Lauf [[2026-11-16-goal-lauf-beschichtung-fa-sdata-arbeitsgaenge]]
(FA-Struktur R1/R2) – nur explizite Pfade stagen, Versions-/Changelog-Dateien vor dem Bump frisch lesen.

Restfragen aus dem Rumpf-Nachzug: R1 (Artikelinfo-Link nur mit `stammdaten_lesen`; einkauf hat das Recht nicht – Gerald entscheidet:
/Roles oder Filter erweitern), R2 (Bestellungen ohne Liefertermin zählen nicht im Fenster – mit Till bestätigen), R3 (Info).

## Übersicht

| Spec | Typ | SQL | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-16-bestellvorschlaege-nachbesserung-spec]] | Epic | 155 | 1.99.30 | InUmsetzung | – | E7/E8 nur Ersatzlösung bis P-N1..P-N9 |

## Protokoll

- 2026-11-16: Lauf gestartet, Rumpf nachgezogen (`5099cce1`).
