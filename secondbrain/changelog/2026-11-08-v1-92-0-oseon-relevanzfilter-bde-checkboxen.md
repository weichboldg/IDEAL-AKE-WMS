---
type: changelog
version: 1.92.0
date: 2026-11-08
---
# v1.92.0 — Korrekturen: Checkboxen behalten ihren Zustand

Bündel `feature/2026-08-07-ideal-teile-1-5`, Lauf [[2026-11-08-nachbesserungen-vor-buendel-merge-goal-lauf]]. Spec [[2026-11-08-oseon-relevanzfilter-schaltet-sich-ab-spec]].
Commits `cf869e97`, QA-Reparatur `9c9046c4` (B-1: „Final“ verschieben – alte Final-Zeile zuerst aufheben, dann neue setzen, eine Transaktion). Nicht gemergt (Schranke 2).

## Kernpunkte

- Hidden-„false“ hinter der Checkbox (OSEON `useRelevanceFilter`, BDE-Edit `IsFinal`/`Delete`); BDE-Edit lehnt zwei Final-Zeilen ab (UNIQUE-Index `IX_BdeBookingQuantities_Booking_Final`).
- Binder-Nachweis im Projekt: `CheckboxHiddenFeldV192Tests`.

## Deploy-Risiken

Nur Web. Seit dem Fix wirkt „Final“ im BDE-Edit wirklich – Prüfabfrage in der Spec für evtl. früher verlorene Final-Kennzeichen (nur lesen).
