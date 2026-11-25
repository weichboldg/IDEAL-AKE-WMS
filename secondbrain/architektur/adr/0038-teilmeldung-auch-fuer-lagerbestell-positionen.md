---
type: adr
id: 0038
title: Teilmeldung auch für Lagerbestell-Positionen (dieselbe Tabelle BdeTeilMeldungen)
status: accepted
date: 2026-11-25
supersedes: ""
superseded_by: ""
---

> Spec [[2026-11-20-bde-liste-isolierungsfraese-pfad-material-spec]] (v1.99.72, TS-193, SQL/165), Freigabe Gerald 2026-11-25. Ergänzt [[0030-teilmeldung-mit-fachlichem-schluessel-neben-workoperation]].
> Bündel `feature/2026-08-07-ideal-teile-1-5`, Commit `d49a3f39`.

## Kontext und Problem

- Die Isolierungsfräse (IF) fräst neben FA-Teilen auch Isolierungen, die über Lagerbestellungen angefordert werden. Die Werker sollen sie in
  derselben Teileliste sehen und fertig melden – ohne Buchung, ohne Sage.
- Teilmeldungen kennen bisher nur den fachlichen FA-Schlüssel (HauptFA, VaterFA, SubFA, Position, Artnr, Kürzel), ADR 0030.

## Entscheidung

- **Dieselbe Tabelle `BdeTeilMeldungen`** mit neuer Spalte `WarehouseRequisitionItemId` (int NULL, FK Restrict). Lagerbestell-Meldung:
  `HauptFA = 0`, `VaterFA = NULL`, `SubFA = 0`, `Position = NULL`, `BdeBookingId = NULL`.
- Doppelschutz über **zwei gefilterte UNIQUE-Indizes**: FA-Schlüssel `WHERE [IsCancelled] = 0 AND [WarehouseRequisitionItemId] IS NULL`,
  Lagerbestellung (`WarehouseRequisitionItemId`, `Kuerzel`) `WHERE [IsCancelled] = 0 AND [WarehouseRequisitionItemId] IS NOT NULL`; dazu App-Guard
  (InMemory) und applock je Position+Kürzel.
- **Alle FA-Lesewege filtern ausdrücklich `WarehouseRequisitionItemId IS NULL`** (Laden, Waisen, vorhandene Meldung) – nicht auf „HauptFA 0 kommt
  nie vor“ verlassen.

## Begründung

- Rücknahme, Admin-Storno, Teilmeldungen-Liste, Doppelschutz und Audit werden wiederverwendet; eine zweite Tabelle hätte jeden dieser Wege verdoppelt.
- Nachteil: die FA-Spalten sind bei Lagerbestell-Meldungen Platzhalter (0/NULL). Dafür der ausdrückliche Filter in jedem FA-Leseweg (Fallstrick §67).

## Folgen

- Neue Lesewege auf `BdeTeilMeldungen` müssen sich entscheiden: FA (`IS NULL`), Lagerbestellung (`IS NOT NULL`) oder beide.
- Eine Meldung ändert die Lagerbestellung nicht; das Lager schließt sie wie bisher ab.
