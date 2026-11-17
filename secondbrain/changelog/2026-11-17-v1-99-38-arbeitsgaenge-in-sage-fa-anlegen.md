---
type: changelog
version: 1.99.38
date: 2026-11-17
---
# v1.99.38 — Arbeitsgänge aus dem WMS in Sage-FAs anlegen (IDEAL): Vorschau und Warteschlange

Bündel `feature/2026-08-07-ideal-teile-1-5`, Commits `825b8f58` (E0) `52a44b71` (E1a) `e5ce82da` (Doku/Version). Spec
[[2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen-spec]] (Epic, E1b Schreibtest blockiert), Fallstrick 49. Goal-Lauf
[[2026-11-17-goal-lauf-sage-ag-anlage]]. Nicht gemergt, qa-agent ausstehend.

## Kernpunkte

- Admin-Seite *Stammdaten → Arbeitsgänge → Sage*: je FA (Haupt + Unter, keine Blätter) und Kürzel aus dem Feld Arbeitsschritte eine Zeile
  mit Sage-AG aus `/OperationConfigs`; Sage-Stand (vorhanden, beendet, begonnen, nur in Sage) nur lesend auf Knopf.
- Schalter `SageFaArbeitsgaengeAnlegenAktiv` (Default **aus**): Knopf „FA … anlegen“ reiht in `SageFaArbeitsgangAnlagen` ein; der
  `SageBookingWorker` liest den FA vor jedem Senden in Sage und legt fehlende AGs per `FertigungsauftragService` ServiceOption 4 an
  (Menge = Fertigungsmenge, Bearbeiter aus `Sync:SageFaArbeitsgaengeBearbeiter`, kein Einlasten). Wiederholung 3×/15 min, unklar nie automatisch.
- Protokoll „Sage-FA-Arbeitsgänge“ (Angelegt/Übersprungen/Fehler), Fehlermail.

## Deploy-Risiken

- **SQL/160** additiv (neue Tabelle, beide Firmen). Web + Service.
- **Schreibt in Sage-FAs, sobald der Schalter an ist** – Feldnamen der Payload sind am IDEAL-Ziel **nicht** belegt (P-1..P-4). Schalter
  erst nach Schreibtest TS-159.x (Freigabe Gerald). Manifest: `Pruefung/sage-fa-ag-sdata-zugang`, `v1.99.38-sage-fa-ag-schalter`,
  `v1.99.38-sage-fa-ag-info`. AKE: Seite nicht sichtbar, Tabelle leer. Tests: TS-159.
