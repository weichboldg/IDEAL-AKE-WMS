---
type: changelog
version: 1.99.65
date: 2026-11-25
---
# v1.99.65 — Beschichtungsauftrag: ohne Auswahl anlegen, Rückfrage beim Übergeben, Verpackt oder Entfernen

Spec [[2026-11-25-beschichtungsauftrag-vorschlag-ohne-auswahl-spec]], Lauf [[2026-11-25-goal-lauf-beschichtung-sage-buchungen]].
Commit `8ffa4c4a`, QA `186e63dd` (Brain). Kein SQL, keine Einstellung. TS-162.3/162.7/162.8 geändert, neu 162.21–162.25.
Nicht gemergt.

## Kernpunkte

- „Aus Vorschlagsliste erstellen“ ohne Haken legt den Entwurf mit **allen freien** beschichteten Positionen des HauptFA an, alle **offen**
  (`BeschichtungsauftragWorkflow.AnlegenAsync`: `auswahl` null oder leer = alle, `Verpackt = !alle`). Mit Haken wie bisher (nur angehakte, verpackt).
- „Auf Übergeben setzen“ mit nicht verpackten Positionen: Fenster mit „Positionen ohne Verpackt-Kennzeichen entfernen und übergeben“ oder
  „Nicht verpackt beibehalten und übergeben“ (`wwwroot/js/beschichtung-bemerkung.js`). Versendet bleibt nur bei allem verpackt.
- Eine Regel `BeschichtungsauftragWorkflow.Entfernbar(status, verpackt)`: im Entwurf immer, in Übergeben nur mit **gespeichertem** Haken „offen“;
  Server und Detailseite lesen sie.
- „Verpackt“ + „Entfernen“ an derselben Position: Server lehnt mit Positionsnamen ab (melden statt still entscheiden), Oberfläche sperrt „Verpackt“.

## Migration/SQL

Keine.

## Deploy

Web (`BeschichtungsauftragWorkflow`, `BeschichtungsauftraegeController`, `Details.cshtml`, `_VorschlagAuftraege.cshtml`, JS, Hilfe).
Dienst nur wegen `AppVersion.cs`. Kein Manifest-Eintrag.

## Risiken

- Verhaltensänderung für das Lager: Erstellen ohne Haken war bisher eine Warnung, jetzt entsteht ein Entwurf mit allen freien Positionen.
- QA-Befunde B-1 (Positionsbezeichnung an IDEAL nicht eindeutig) und F1 (Übergeben mit 0 Positionen) → in v1.99.67 behoben;
  B-2 (Fensterknopf nach Browser-Validierung gesperrt) → ebenfalls v1.99.67.

## Commit / Merge

`8ffa4c4a` im Bündel `feature/2026-08-07-ideal-teile-1-5`. **Nicht gemergt** (wartet Schranke 2).
