---
type: changelog
version: 1.19.0
date: 2026-05-29
---
# v1.19.0 — Lagerbestellungen: ShortageStatus 3-State + 2-Tab-Fehlteile

Anlass: v1.18.0 hatte ein einzelnes Bool `IsFinalShortage`. Beim Test zeigte sich, dass der
Lagermitarbeiter zwischen „wird nachgeliefert" und „wird nicht nachgeliefert" unterscheiden muss.

- `IsFinalShortage` (bool) → `ShortageStatus`-Enum (None / WillBeRestocked / NoRestock), 2 Radios
  je Position, 2 Tabs in der Fehlteile-Liste.
- **Migration 65** ist **daten-destruktiv**: konvertiert und dropt die Spalte; `Down()` verliert die
  Unterscheidung None/WillBeRestocked → **DB-Backup vor Deploy**.
- Status-Ableitung: Order wird `PartiallyDelivered`, wenn eine Position `WillBeRestocked` ist,
  sonst `Closed`.
- Zweite Notiz `NoteEinkauf` („Notiz EK") neben `Note` („Notiz Lager").
- `MissingPartsController` filtert per Default auf **eigene** Werkbaenke (`mineOnly=true`); neue
  Lager-Sicht als eigener `MissingPartsLagerController`. User ohne Werkbank-Zuordnung sehen eine
  leere Liste mit Info-Banner — bewusst **kein** Fallback auf alle Fehlteile.
- Radio-3-State (Doppelklick → None) ist selbstgebaut; Bootstrap kann das nicht.
