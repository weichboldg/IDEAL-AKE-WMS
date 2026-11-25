---
type: changelog
version: 1.99.67
date: 2026-11-25
---
# v1.99.67 — Beschichtungsauftrag: VaterFA in Meldungen, kein Übergeben ohne Positionen

Freigabe-Nachtrag zu [[2026-11-25-beschichtungsauftrag-vorschlag-ohne-auswahl-spec]] (QA-Befunde B-1, F1, B-2), Lauf
[[2026-11-25-goal-lauf-beschichtung-sage-buchungen]]. Commit `fcc894c6`, QA `2b4dceaf` (Brain). Kein SQL. TS-162.26–162.29.
Nicht gemergt.

## Kernpunkte

- `BeschichtungsauftragWorkflow.PositionBezeichnung` nennt die VaterFA, z. B. „50002326 (Pos. 1, VaterFA 4711)“ – Artnr + Pos. allein ist an IDEAL
  nicht eindeutig (Meldungen, Übergeben-Fenster, Aria-Labels).
- Übergeben, nach dem 0 Positionen übrig blieben, wird **vor** dem Speichern abgelehnt (`BearbeitenAsync(..., ziel)`, Controller reicht `ziel` durch);
  Weg ist Stornieren. Ist keine Position verpackt, bietet das Fenster „Entwurf stornieren“ statt „entfernen und übergeben“.
- Bricht Browser-Validierung das Übergeben ab, schließt das Fenster und die Knöpfe bleiben bedienbar (B-2).
- Mitbehoben: fehlendes Leerzeichen im Intro der Sage-Buchungen (Gesamtsicht-Befund B2).

## Migration/SQL

Keine.

## Deploy

Web. Dienst nur wegen `AppVersion.cs`. Kein Manifest-Eintrag.

## Risiken

- QA-Befund N-1 (Altbestand): leerer Entwurf ließ sich nicht absenden („ungültiger Wert in .“) → Root Cause und Fix in v1.99.69.
- DEV IDEAL: BA-8 „Start Beschichtung“ versehentlich auf 12.07.2026 gesetzt (Testdaten, siehe [[2026-11-25-folgearbeit-goal-lauf-sage-beschichtung]]).

## Commit / Merge

`fcc894c6` im Bündel. **Nicht gemergt** (wartet Schranke 2).
