---
type: uebersicht
title: "FA-Struktur: Arbeitsgänge als Symbole, später BDE-Status in Farbe — Übersicht"
slug: 2026-10-06-fa-struktur-arbeitsgaenge-symbole-uebersicht
status: Entwurf
created: 2026-10-06
updated: 2026-10-07
source_backlog: "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole]]"
anhaenge:
  - "[[sage-fa-struktur-arbeitsschritte.png]]"
open_questions: []
---

> **Umsetzungsort (Hinweis, keine Entscheidung):** nach Schranke 2 des IDEAL-Bündels, aus `main`
> (neuer Worktree). Code-Grundlage aller Teile: Bündel-Stand `.claude/worktrees/2026-08-07-ideal-teile-1-5`
> (v1.47.1; seit Rework 2026-10-07 v1.47.4). TS-Nummern und Versionen: nächste freie zum Umsetzungszeitpunkt.

Zerlegung von [[2026-10-06-fa-struktur-arbeitsgaenge-symbole]] (`split: true`) in einzeln mergebare Teile,
Reihenfolge wie „Gewünschter Schnitt“ der Notiz. Baut auf
[[2026-09-23-fa-struktur-arbeitsgaenge-anzeige-spec]] (v1.45, Modal) und
[[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]] (v1.44, Arbeitsgänge aus der Struktur) auf.
Standort: Teile 1-2 **IDEAL** (FA-Struktur), Teil 3 **AKE und IDEAL** (BDE-Terminal, Teil-3-Antwort 5).
Vorbedingung für Teil 2/3: Scope-Korrektur der Arbeitsgang-Erkennung **v1.47.4** im Bündel
([[2026-10-07-v1-47-4-arbeitsgang-erkennung-kind-sub-fa]], aus Teil-2-Antwort 7 W-1).

Vorbild (Anhang der Notiz, [[sage-fa-struktur-arbeitsschritte.png]]): Sage „Fertigungsaufträge
bearbeiten“ zeigt je Baumzeile die Arbeitsschritte als Kürzel in **einer** Spalte und die Abteilung je Zeile.
Die WMS-Felder dazu (`FaHierarchyNode.Arbeitsschritte`/`Arbeitsbereich`) sind Kandidaten, die Gleichheit ist
ungeprüft (Prüfschritt P-K1 in Teil 1).

| # | Teil | Zweck | depends_on | Offene Rückfragen |
|---|------|-------|------------|-------------------|
| 1 | [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec]] | Sage-Kürzel als Symbole direkt in den Baumzeilen der FA-Struktur, reine Anzeige ohne Status (Quelle S, Ort A) | – | 1 (Nr. 4 Modal bestätigen; Nr. 1-3 beantwortet) |
| 2 | [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]] | Status erledigt/läuft/überfällig/offen an den Symbolen bei BDE global + Werkbank aktiv; überfällig = `FE_Termin` + Offset-Werktage (`OseonDueDateCalculator`); Farbe + Zeichen + `aria-label` | Teil 1 (+ Bündel v1.47.4) | 4 (Nr. 7 Rest W-2..W-4, neu Nr. 8 Offset-Pflege, Nr. 9 Kombigeräte, Nr. 10 Vorrang läuft/überfällig; Nr. 1-6 und W-1 beantwortet) |
| 3 | [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-3-spec]] | BDE-Terminal „Arbeitsgang wählen“: offene Aufträge je Werkbank gruppiert, nach Fertigungstermin sortiert, Arbeitsgänge mit Status aus Teil 2 — AKE und IDEAL, bestehende BDE-Rollen (ausspezifiziert, kein Platzhalter mehr) | Teil 2 (vormals Teil 1) | 3 (neu Nr. 6 „läuft“ vs. heutige Auswahl, Nr. 7 AKE-Termin, Nr. 8 NurFA-Modus; Nr. 1-5 beantwortet) |

## Querschnitt

- **Entschieden: S** (Teil 1 Antwort 1, Gerald 2026-10-06 — Anzeige aus Sage-Kürzeln, Status aus W; Zuordnung
  in Teil 2 Antwort 6, dort mit vier dokumentierten Widersprüchen zum Erkennungs-Code, Teil-2-Rückfrage 7;
  W-1 seit 2026-10-07 entschieden, W-2 bis W-4 offen).
  Ursprünglicher Text: **Rückfrage 1 von Teil 1 (Quelle: Sage-Kürzel S oder erkannte `WorkOperation` W) prägt Teil 2:** Status
  gibt es nur über `WorkOperation`. Bei S braucht Teil 2 eine Zuordnung Kürzel → `WorkOperation` (Blätter und
  Kinder-Kürzel landen per DirectChildren-Scope beim Vater-Sub-FA) und eine Regel für Kürzel ohne
  `WorkOperation` (Teil 2 Rückfrage 6). Bei W entfällt das, die Zeile weicht aber vom Sage-Bild ab.
- **Entschieden: Ort A** (Teil-1-Antwort 2, Keyuser 2026-10-07): Symbole direkt in den Baumzeilen; die
  v1.45-Freigabe „erst beim Öffnen laden“ gilt für die Symbole nicht mehr, Teil 2 fragt den Status im
  Seitenaufbau ab. Ob das Modal unverändert bleibt, bestätigt Teil-1-Rückfrage 4. Ursprünglicher Text:
  „Rückfrage 2 von Teil 1 (Baum vs. Modal) prägt alle Teile … das muss die Antwort ausdrücklich sagen.“
- **Entschieden: „erledigt“ = `IsFinal`** (Teil-2-Antwort 3). Code-Befund dazu bleibt: Die BDE-Buchung setzt
  `WorkOperation.IsReported` nicht; `IsReported` schreibt nur die manuelle Tracking-Rückmeldung.
- **Entschieden: „überfällig“** = `FE_Termin` + Offset-Werktage über den vorhandenen `OseonDueDateCalculator`
  (Teil-2-Antwort 2 + Nachtrag). Offen: wo die IDEAL-Offsets gepflegt werden (Teil-2-Rückfrage 8; Variante B
  brächte eine Migration) und die Kombigeräte-Regel (Teil-2-Rückfrage 9). Termine liegen nur je HauptFA;
  ihre Bedeutung bleibt unverifiziert (Teil-2-Prüfschritte P1/P2).
- **Entschieden: W-1** (Teil-2-Antwort 7) → Bugfix v1.47.4 im Bündel, nicht in diesen Teilen.
- **Teil 3 ist ausspezifiziert** (Antworten 1-5) und hängt jetzt an Teil 2 (Status). Er gilt für AKE **und**
  IDEAL; die AKE-Seite (Termin, Arbeitsgänge, NurFA) hängt an Teil-3-Rückfragen 7/8 und Prüfschritt P-T1.
- Migration: Teil 1 und 3 keine; Teil 2 nur bei Rückfrage 8 Variante B. Keiner der Teile schreibt
  Bewegungsdaten; Deploy jeweils nur Web (vorläufig).
- **Reihenfolge der Umsetzung:** Bündel-Merge (inkl. v1.47.4) → Teil 1 → Teil 2 → Teil 3.

## Offene Rückfragen

Keine zur Zerlegung selbst — der Schnitt folgt der Notiz. Die fachlichen Rückfragen stehen in den Teilen.
