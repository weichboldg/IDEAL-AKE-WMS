---
type: bug
title: "Beschichtungsauftrag: Vorschlag ohne Haken nicht erstellbar, Verpackt-Prüfung erst bei Versendet, Entfernen in Übergeben, Verpackt + Entfernen gleichzeitig"
status: offen
severity: mittel
created: 2026-11-25
affected_code:
  - IdealAkeWms/Services/BeschichtungsauftragWorkflow.cs (AnlegenAsync :112-115, :138; PositionenAenderbar :233-234; BearbeitenAsync :300-311; StatusWechselnAsync :492-503)
  - IdealAkeWms/Views/Beschichtungsauftraege/Details.cshtml (:276, :322, :330-336)
source_backlog: "[[2026-11-25-beschichtungsauftrag-vorschlag-ohne-auswahl-bug]]"
spec: "[[2026-11-25-beschichtungsauftrag-vorschlag-ohne-auswahl-spec]]"
---

> [!note] Name
> Der Bug-Record heißt bewusst anders als die Backlog-Notiz. Die Backlog-Notiz endet schon auf `-bug`, ein gleichnamiger Record
> machte den Wikilink `source_backlog` mehrdeutig.

## Symptom

1. „Aus Vorschlagsliste erstellen“ ohne Haken in der Spalte „Verpackt“ legt nichts an: „Keine Position angehakt — nichts angelegt.“
2. Ob alle Positionen verpackt sind, prüft das System erst bei Übergeben → Versendet, nicht schon bei Entwurf → Übergeben.
3. Im Status Übergeben lassen sich Positionen noch entfernen.
4. Am Auftrag lassen sich „Verpackt“ und „Entfernen“ für dieselbe Position gleichzeitig anhaken. Die Position wird dann ohne Hinweis entfernt.

## Reproduktion

1. Beschichtung → Vorschlagsliste, keinen Haken setzen, „Aus Vorschlagsliste erstellen“ → Warnung, kein Auftrag.
2. Entwurf mit einer unverpackten Position (z. B. über „Positionen hinzufügen“) → „Auf Übergeben setzen“ → gelingt.
3. Auftrag in Übergeben öffnen → Spalte „Entfernen“ ist da, Speichern entfernt die Position.
4. Entwurf: bei einer Position „Verpackt“ und „Entfernen“ anhaken → Speichern → Position weg, keine Meldung.

## Root Cause

Die Festlegungen von v1.99.41 (Spec [[2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck-spec]] A3, A6, A8) sind
so umgesetzt, wie sie damals beschlossen wurden. Sie passen nicht zum Arbeitsablauf:
- A3: eine leere Auswahl ist ein Fehler (`BeschichtungsauftragWorkflow.cs:113-114`).
- A6: Verpackt wird nur für das Ziel Versendet geprüft (`:494-503`).
- A8: `PositionenAenderbar` = Entwurf oder Übergeben, gilt auch für Entfernen (`:233-234`, `:307-311`).
- `BearbeitenAsync` setzt erst den Haken (`:304`) und entfernt dann (`:307-311`), ohne Prüfung auf Widerspruch.

## Fix / Verweis

[[2026-11-25-beschichtungsauftrag-vorschlag-ohne-auswahl-spec]]
