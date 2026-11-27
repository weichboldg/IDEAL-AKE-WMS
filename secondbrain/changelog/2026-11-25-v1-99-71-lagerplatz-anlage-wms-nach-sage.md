---
type: changelog
version: 1.99.71
date: 2026-11-25
---
# v1.99.71 — Lager und Lagerplätze im WMS erfassen, Anlage in Sage per Direkt-SQL

Spec [[2026-11-24-lagerplatz-anlage-wms-nach-sage-spec]]. Commit `5b6fa696` (Worktree `feature/2026-08-07-ideal-teile-1-5`).
**Kein SQL, keine Migration**, Web **und** Dienst. TS-192. ADR [[0036-lagerplatz-anlage-in-sage-per-direkt-sql-hinter-austauschbarer-schnittstelle]], Fallstrick §66. Nicht gemergt.

## Kernpunkte

- Web: *Stammdaten → Lagerplätze → Lagerplätze in Sage anlegen* (`StorageLocationsController.CreateInSage`, Rolle masterdata): Lagerort +
  Koordinatenbereich (≤ 200 Plätze, Koordinaten ≤ 9999), Code `Lager;D1;D2;D3`, vorhandene Codes übersprungen; „Neues Lager erfassen …“
  nur für Techniken der Stufe. Plätze und Outbox in einer Transaktion. Liste: Spalte „Übergabe-Fehler“, „Erneut senden“ je Platz und je Lager,
  Lager-Hinweise; Edit sperrt Code/Bezeichnung/Zone.
- Dienst: Schritt „Lagerplätze → Sage“ im `SageBookingWorker`: Lager vor Plätzen, Status vor dem Aufruf `Gesendet`, hängend erneut
  (idempotent), Übernahme mit Sage-Sperr-/Maßwerten, Fehlerklassen fachlich/technisch, Mail nur beim Textwechsel, Info `Sage-Datenbank:` je Lauf.
- Lagerplatz-Abgleich: Lager mit offener Lager-Outbox nicht als fehlend markieren.
- Neue ServiceSettings `SageLagerplatzAnlageAktiv` (Bool, false) und `SageLagerAnlageLagertechniken` (Choice Aus/Mitschnitt/Alle, Aus);
  SyncLog-Service `SageLagerplatzAnlage`; Outbox-Typ `StorageArea`.
- Manifest: `v1.99.71-SageLagerplatzRechte`, `v1.99.71-SageLagerplatzAnlageAktiv`, `v1.99.71-SageLagerAnlageLagertechniken`,
  `Pruefung/LagerorteFuerSageAnlage` (braucht SQL/98).

## Deploy-Risiken

- Beide Schalter bleiben aus. Einschalten je Standort erst nach GRANT auf der Sage-Mandanten-DB, Prüfabfragen (IDEAL P-1 bis P-9 Pflicht),
  V3-Beleg der Ziel-DB und Abnahmetest TS-192 am Testmandanten; produktiv zusätzlich Sage-DB-Backup (Betrieb 05 § 4b).
- Einziger direkter Schreibweg in Sage; nur eine Dienst-Instanz je WMS-DB. DEV-AKE-Dienst nur mit `appsettings.DEV.json` → `ake_TEST2026`.
