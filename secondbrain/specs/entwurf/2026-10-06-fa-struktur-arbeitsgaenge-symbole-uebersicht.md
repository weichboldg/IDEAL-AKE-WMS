---
type: uebersicht
title: "FA-Struktur: Arbeitsgänge als Symbole, später BDE-Status in Farbe — Übersicht"
slug: 2026-10-06-fa-struktur-arbeitsgaenge-symbole-uebersicht
status: Entwurf
created: 2026-10-06
updated: 2026-10-06
source_backlog: "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole]]"
anhaenge:
  - "[[sage-fa-struktur-arbeitsschritte.png]]"
open_questions: []
---

> **Umsetzungsort (Hinweis, keine Entscheidung):** nach Schranke 2 des IDEAL-Bündels, aus `main`
> (neuer Worktree). Code-Grundlage aller Teile: Bündel-Stand `.claude/worktrees/2026-08-07-ideal-teile-1-5`
> (v1.47.1). TS-Nummern und Versionen: nächste freie zum Umsetzungszeitpunkt.

Zerlegung von [[2026-10-06-fa-struktur-arbeitsgaenge-symbole]] (`split: true`) in einzeln mergebare Teile,
Reihenfolge wie „Gewünschter Schnitt“ der Notiz. Baut auf
[[2026-09-23-fa-struktur-arbeitsgaenge-anzeige-spec]] (v1.45, Modal) und
[[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]] (v1.44, Arbeitsgänge aus der Struktur) auf.
Standort: IDEAL.

Vorbild (Anhang der Notiz, [[sage-fa-struktur-arbeitsschritte.png]]): Sage „Fertigungsaufträge
bearbeiten“ zeigt je Baumzeile die Arbeitsschritte als Kürzel in **einer** Spalte und die Abteilung je Zeile.
Die WMS-Felder dazu (`FaHierarchyNode.Arbeitsschritte`/`Arbeitsbereich`) sind Kandidaten, die Gleichheit ist
ungeprüft (Prüfschritt P-K1 in Teil 1).

| # | Teil | Zweck | depends_on | Offene Rückfragen |
|---|------|-------|------------|-------------------|
| 1 | [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec]] | Arbeitsgänge als Symbole statt Zeilen, reine Anzeige ohne Status | – | 2 (Nr. 2, 3; Nr. 1 beantwortet) |
| 2 | [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]] | Status-Farben grün/grau/rot bei aktivem BDE, nie Farbe allein | Teil 1 | 6 (Nr. 1-5, neu Nr. 7 Widersprüche Antwort 6 ↔ Code; Nr. 6 beantwortet) |
| 3 | [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-3-spec]] | Vorbereitung BDE-Auftragsanzeige — **Platzhalter**, erst nach Antworten spezifizieren | Teil 1 (ggf. Teil 2) | 5 |

## Querschnitt

- **Entschieden: S** (Teil 1 Antwort 1, Gerald 2026-10-06 — Anzeige aus Sage-Kürzeln, Status aus W; Zuordnung
  in Teil 2 Antwort 6, dort mit vier dokumentierten Widersprüchen zum Erkennungs-Code, Teil-2-Rückfrage 7).
  Ursprünglicher Text: **Rückfrage 1 von Teil 1 (Quelle: Sage-Kürzel S oder erkannte `WorkOperation` W) prägt Teil 2:** Status
  gibt es nur über `WorkOperation`. Bei S braucht Teil 2 eine Zuordnung Kürzel → `WorkOperation` (Blätter und
  Kinder-Kürzel landen per DirectChildren-Scope beim Vater-Sub-FA) und eine Regel für Kürzel ohne
  `WorkOperation` (Teil 2 Rückfrage 6). Bei W entfällt das, die Zeile weicht aber vom Sage-Bild ab.
- **Rückfrage 2 von Teil 1 (Baum vs. Modal) prägt alle Teile:** Sie bestimmt, wo Teil 2 die Status-Abfrage
  ausführt, und ob Teil 3 einen fertigen Symbol-Baustein vorfindet. Variante A hebt die v1.45-Freigabe
  „erst beim Öffnen laden“ auf — das muss die Antwort ausdrücklich sagen.
- **Code-Befund, der Teil 2 betrifft:** Die BDE-Buchung setzt `WorkOperation.IsReported` **nicht**; das
  Terminal erkennt Abschluss über `BdeBookingQuantity.IsFinal`. `IsReported` schreibt nur die manuelle
  Tracking-Rückmeldung. Deshalb ist „erledigt“ in Teil 2 eine eigene Rückfrage.
- **Termine nur je HauptFA, Bedeutung unverifiziert:** Die Beschreibung der Terminfelder stammt aus der
  View-Doku, nicht aus dem Datenfluss — Teil 2 enthält die Prüfabfragen P1/P2 für Sage/PPS.
- Keiner der Teile 1–2 braucht eine Migration oder schreibt Daten; Deploy jeweils nur Web (vorläufig).

## Offene Rückfragen

Keine zur Zerlegung selbst — der Schnitt folgt der Notiz. Die fachlichen Rückfragen stehen in den Teilen.
