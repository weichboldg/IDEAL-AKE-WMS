---
type: aufgabe
status: offen
created: 2026-11-17
quelle: Gerald, Chat 2026-11-17 (/goal + Dev-Lauf-Auftrag)
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# /goal-Lauf 2026-11-17: Offene Bestellungen sofort neu lesen + Bestellvorschläge Q2 = b

Auftrag Gerald 2026-11-17: zwei Teile im Bündel-Worktree (Stand v1.99.30, `54f04740`). Grenzen: nichts an Sage schreiben außer der bestehenden
Sage-WE-Logik, keine Einstellungen auf Test/Prod, kein Merge, kein Push. QA je Teil, Publish am Ende, DEV AKE+IDEAL einspielen.

1. [[2026-11-16-offene-bestellungen-sofort-neu-lesen-einkauf-kw-filter-spec]] – liegt unter `specs/entwurf/` mit `status: Freigegeben`, laut
   Auftrag freigegeben. Vorher Freigabe-Antworten 1–8 (Gerald 2026-11-17 06:41/06:43) in den Rumpf ziehen.
2. [[2026-11-16-bestellvorschlaege-nachbesserung-spec]] – QA-Befund Q2, Entscheidung **Variante b**: die Liste zählt den Wagenbestand wie die Artikelinfo.

Nummern: Version 1.99.30 belegt (Bestellvorschläge, Q2 bleibt in 1.99.30) → Offene Bestellungen **1.99.31** (Spec nannte 1.99.32; 1.99.27
reserviert Mapping); TS-155; keine SQL.

## Übersicht

| Spec | Typ | SQL | Version | Status | Commits | Blocker |
|---|---|---|---|---|---|---|
| [[2026-11-16-offene-bestellungen-sofort-neu-lesen-einkauf-kw-filter-spec]] | Spec | – | 1.99.31 | InUmsetzung (Rumpf nachgezogen) | – | Rückfragen 9–11 nicht blockierend |
| [[2026-11-16-bestellvorschlaege-nachbesserung-spec]] (Q2 = b) | Nachbesserung | – | 1.99.30 | InUmsetzung | – | – |

## Protokoll

- 2026-11-17: Lauf gestartet; Q2-Fix und Rumpf-Nachzug parallel (Brain bzw. Worktree, keine Überschneidung).
