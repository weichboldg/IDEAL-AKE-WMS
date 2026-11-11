---
type: bug
title: Checkbox mit verstecktem false-Feld VOR der Checkbox bindet immer false (OSEON-Relevanzfilter, BDE-Buchung bearbeiten)
status: offen
severity: mittel
created: 2026-11-08
affected_code:
  - "IdealAkeWms/Views/Tracking/OseonIndex.cshtml:52-54"
  - "IdealAkeWms/Views/BdeBookings/Edit.cshtml:102-103 (IsFinal)"
  - "IdealAkeWms/Views/BdeBookings/Edit.cshtml:107-108 (Delete)"
source_backlog: "[[2026-11-08-oseon-relevanzfilter-schaltet-sich-ab]]"
spec: "[[2026-11-08-oseon-relevanzfilter-schaltet-sich-ab-spec]]"
---

## Symptom

- **OSEON-Teileverfolgung** (`/Tracking/OseonIndex`): Jedes Absenden der Filterkarte, auch ein Artikel-Scan, schaltet „Fremdsystem-Relevanz“
  aus. Danach ist der Haken leer, die Liste zeigt alle Arbeitsgänge.
- **BDE-Buchung bearbeiten** (`/BdeBookings/Edit/{id}`, Rolle BDE-Admin), aus dem Code abgeleitet, auf DEV noch nicht nachgestellt: Ein
  angehaktes „Final“ wird beim Speichern zu `false`, ein angehaktes „Löschen“ löscht die Mengenzeile nicht.

## Reproduktion

OSEON (im Browser belegt, AKE-DEV v1.79.0, [[2026-11-08-oseon-teileverfolgung-doppelter-klickhandler-spec]] Zeile 260-262):
`/Tracking/OseonIndex` öffnen (Haken gesetzt, Default) → Artikel eingeben oder scannen → „Filtern“. Die URL enthält
`useRelevanceFilter=false&useRelevanceFilter=true`, der Haken ist aus.

BDE: Buchung mit einer Mengenzeile öffnen → „Final“ anhaken → Speichern → erneut öffnen: „Final“ ist leer.

## Root Cause

Beide Formulare stellen `<input type="hidden" name="X" value="false" />` **vor** die Checkbox mit gleichem `name`. Angehakt sendet der Browser
`X=false&X=true`. Der Binder für einfache Typen nimmt den ersten Wert (`false`). Abgewählt kommt nur `false`. Ergebnis: Der Wert ist immer
`false`. Belege und Binder-Nachweis stehen in der Spec.

## Fix / Verweis

[[2026-11-08-oseon-relevanzfilter-schaltet-sich-ab-spec]]: Das versteckte Feld kommt **hinter** die Checkbox, nach dem Muster v1.81.0
`MissingParts/Index.cshtml:85-89`.
