---
type: aufgabe
status: erledigt
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
| [[2026-11-16-offene-bestellungen-sofort-neu-lesen-einkauf-kw-filter-spec]] | Spec | – | 1.99.31 | **Testbereit** (qa-agent) | `8b7d8252`, `2f84a012`, `9c623d4c`, `a7bc62d4`, QA-Fix `d3a00101` | Rückfragen 9–11 nicht blockierend |
| [[2026-11-16-bestellvorschlaege-nachbesserung-spec]] (Q2 = b) | Nachbesserung | – | 1.99.30 | **Testbereit** (qa-agent) | `5b123c11` | Q2-B1 als Backlog [[2026-11-17-artikelinfo-bestand-teiltextsuche-bug]] |

## Protokoll

- 2026-11-17: Lauf gestartet; Q2-Fix und Rumpf-Nachzug parallel (Brain bzw. Worktree, keine Überschneidung).
- 2026-11-17: Q2 = b umgesetzt (`5b123c11`), QA Testbereit (00730607: Liste = Artikelinfo 45171), DEV AKE+IDEAL aus Scratch-Worktree `qa-5b123c11` (Aufräumen nach Freigabe). Zusatzfund Q2-B1/B2 als Backlog.
- 2026-11-17: Offene Bestellungen umgesetzt: `8b7d8252` (Dienst F1: Anstoß, Sperre, Rückgabe Belegzahl), `2f84a012` (Web F2/F2a/F3/F4),
  `9c623d4c` (wip feature-complete: v1.99.31, Doku, TS-155, Manifest). Build 0 Fehler; Tests Web 3013 grün (1 übersprungen), Dienst 682 grün.
  Abweichung: Anlass zählt Belege statt Positionen. Brain: [[2026-11-17-v1-99-31-offene-bestellungen-sofort-neu-lesen]], feature-map,
  testszenarien-index (155), services.md, controller.md, fallstricke §42, Spec-Deploy-Abschnitt gegen den Diff geprüft. Nächster Schritt:
  qa-agent (nicht in diesem Lauf).
- 2026-11-17: Offene Bestellungen QA **Testbereit** (Web 3013/0/1, Dienst 682/0/0), QA-Fix `d3a00101` (Guard-Test Einkauf-Controller). DEV AKE+IDEAL v1.99.31 (enthält Q2).

## Abschluss (2026-11-17)

Beide Teile **Testbereit**. Worktree-HEAD `d3a00101`, v1.99.31, keine neue SQL. Publish `<Worktree>\publish\` (publish-info: 1.99.31, `d3a00101`,
07:24); alte `publish\deploy.zip` nicht verwenden. DEV AKE (:8081) und IDEAL (:8082) auf 1.99.31. Kein Merge, kein Push, nichts an Sage.

Offen für Gerald: Rückfragen 9–11 Offene Bestellungen (9 Keyuser-Filter lässt Kommissionierung durch; 10 „CSV erneut schreiben“ in Liste nur
offener WEs nie sichtbar; 11 Rücksprung nach Aktion in die Lager-Liste); TS-155.1–155.4 (echter Sage-WE-Anstoß) auf dem Testsystem;
Q2-B1 [[2026-11-17-artikelinfo-bestand-teiltextsuche-bug]]; QA-Hinweis B-3 (Warnung doppelt auf Lager → Sage-Wareneingänge, Altbestand);
Scratch-Worktree `qa-5b123c11` aufräumen (Freigabe); manuelle Checklisten in beiden Specs.
