---
type: backlog
typ: bugfix
status: in-spec
spec: "[[2026-11-25-beschichtungsauftrag-vorschlag-ohne-auswahl-spec]]"
bug: "[[2026-11-25-beschichtungsauftrag-ohne-auswahl-entfernen-uebergeben-bug]]"
created: 2026-11-25
quelle: Gerald, Chat 2026-11-25 („planen wir einen bugfix kleinen change …“)
modul: Beschichtungsauftrag (Vorschlagsliste, Details)
---
# Beschichtungsauftrag: Vorschlag ohne Auswahl erstellbar, Prüfung erst bei Übergeben, Entfernen nur im Entwurf

Wortlaut: *Die Vorschlagsliste im Beschichtungsauftrag kann nicht erstellt werden, wenn der Artikel nicht verpackt ausgewählt
wurde. „Keine Position angehakt — nichts angelegt.“ Der Vorschlag soll ohne Auswahl erstellbar sein. Erst von Entwurf auf
Übergeben sollte da geprüft werden. Löschen sollte ab Übergeben auch nicht mehr möglich sein. Es ist auch möglich, dass ich
verpackt und entfernen gleichzeitig auswähle. Sollte entweder oder sein.*

## Befund 2026-11-25 (Bündel-Worktree `2026-08-07-ideal-teile-1-5`)

1. **Leere Auswahl wird abgelehnt:** `BeschichtungsauftragWorkflow.AnlegenAsync` (`Services/BeschichtungsauftragWorkflow.cs:112-115`)
   liefert bei `auswahl is { IstLeer: true }` die Meldung „Keine Position angehakt — nichts angelegt.“. Aufruf aus
   „Aus Vorschlagsliste erstellen“ (`Controllers/BeschichtungsauftraegeController.cs:145-171`). Der Pfad `auswahl == null`
   (= alle freien Positionen, `AlleFreien`, `:144`) existiert schon. Alles aus der Auswahl wird `Verpackt = true` (`:138`).
   Test dazu: `BeschichtungsauftraegeControllerTests.cs:151`; TS-162.3; Hilfe `Views/Help/Index.cshtml:1537`;
   `docs/anwender/15-beschichtung-beschichtungsauftrag.md:56`.
2. **Verpackt-Prüfung heute erst bei Versendet:** `StatusWechselnAsync` (`:484-500`) prüft „alle Positionen verpackt“ nur für
   Ziel `Versendet` (A6); Entwurf → Übergeben prüft nur den Dienstleister (`:492`).
3. **Entfernen auch in Übergeben möglich:** `PositionenAenderbar` (`:233-234`) = Entwurf **oder** Übergeben; gilt für Haken,
   Menge, Entfernen und Hinzufügen (A8, v1.99.41). Entfernen-Spalte in `Views/Beschichtungsauftraege/Details.cshtml:276, 329-333`.
4. **Verpackt und Entfernen gleichzeitig:** zwei unabhängige Checkboxen (`Details.cshtml:322` `verpackt`, `:331` `entfernen`);
   `BearbeitenAsync` (`:300-311`) setzt erst den Haken, dann wird die Position entfernt — kein Hinweis, kein Fehler.

## Für die Spec klären
- „Ohne Auswahl erstellbar“: alle freien Positionen übernehmen (offen, nicht verpackt) oder leerer Auftrag?
- Prüfung bei Übergeben: alle Positionen verpackt (A6 von Versendet nach Übergeben vorziehen oder an beiden Stellen)?
- Ab Übergeben: nur Entfernen sperren, oder auch Hinzufügen/Menge/Haken?
- Entweder-oder: im UI gegenseitig sperren **und** serverseitig ablehnen (Fehler nennen, nicht still entscheiden).
