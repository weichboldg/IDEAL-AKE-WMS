---
type: adr
id: 0036
title: Lagerplatz-Anlage in Sage per Direkt-SQL hinter austauschbarer Schnittstelle, bis DPS einen SData-Service liefert
status: accepted
date: 2026-11-25
supersedes: ""
superseded_by: ""
---

> Spec [[2026-11-24-lagerplatz-anlage-wms-nach-sage-spec]] (v1.99.71, TS-192, keine Migration), Entscheid Gerald 2026-11-24, Freigabe
> 2026-11-25. Bündel `feature/2026-08-07-ideal-teile-1-5`, Commit `5b6fa696`. Referenz-SQL: Anhang der Spec (Sage-Mitschnitt).

## Kontext und Problem

- Lager und Lagerplätze sollen im WMS erfasst und in Sage angelegt werden. Sage-Standard-SData bietet dafür **keinen** Service (G5 der Spec);
  ein SData-Service von DPS ist angekündigt, aber nicht da.
- Bisher schrieb das WMS an Sage **nur über SData** (Lagerbuchung, Wareneingang, AG-Anlage); [[integrationen]] und Betriebsdoku 09 verboten
  Schreibrechte auf Sage-Datenbanken.
- Die Outbox `SageStammdatenOutbox` (v1.73.0, E9) merkte manuell angelegte Plätze schon vor, aber nichts las sie aus.

## Entscheidung

- **Direkt-SQL hinter `ISageLagerplatzAnlage`** (`IDEALAKEWMSService/Services/ISageLagerplatzAnlage.cs`), eine Implementierung
  `SqlSageLagerplatzAnlage`. Outbox, Status-Automat, Worker-Schritt und Oberfläche hängen nur am Vertrag (Ergebnis Angelegt/Vorhanden/Abgelehnt,
  technische Fehler als Exception). Sender ist der `SageBookingWorker` (Schritt „Lagerplätze → Sage“, `SageLagerplatzAnlageService`).
- **Rechte:** bestehendes Dienstkonto der `SageConnection` (kein neuer Connection String) mit Objekt-GRANTs: `INSERT` `KHKLagerplaetze`,
  `INSERT, UPDATE` `KHKLagerorte`, `EXECUTE` `spSysTan` (KHKTan über Ownership-Chaining), `SELECT` nur falls kein `db_datareader`. Kein
  `db_datawriter`, keine Rechte auf andere Tabellen. Der Schalter `SageLagerplatzAnlageAktiv` (Default aus) bleibt die Sicherung.
- **Lager-Anlage „wenn möglich“:** nur für Lagertechniken der Stufe `SageLagerAnlageLagertechniken` (`Aus`/`Mitschnitt` = LTD0ST, LTD1BL /
  `Alle`), deren Lager-Anlage am Standort durch Mitschnitt bzw. Abnahmevergleich belegt ist; WarenflussKZ fest `SWF`, `HWE`/`HWA` nie;
  Lagertechnik und Bezeichnung bestehender Lager werden nie geändert (vorhanden = nur übernehmen, abweichend = fachlicher Fehler).
- **Transaktion:** je Lager bzw. je Platz **eine** Transaktion inklusive Zähler-UPDATE (`spSysTan` sperrt `KHKTan` bis zum Commit), `XACT_ABORT`,
  kurz; Ergebnis-Zeile erst nach dem `COMMIT` (S5).
- **Idempotenz (H12):** Prüfen und Anlegen in derselben Transaktion unter `UPDLOCK, HOLDLOCK`; eine Wiederholung ist gefahrlos. Deshalb werden
  hängende `Gesendet`-Einträge nach `Sync:SageLagerbuchungStuckMinutes` **erneut** gesendet – ausdrückliche Abweichung vom Hausmuster „hängend nie
  blind neu senden“ (`SageBookingWorker.cs:19`). Restrisiko: gleichzeitiger Sage-Dialog an demselben Platz (F5).
- **Sperren:** `DEADLOCK_PRIORITY LOW` (das WMS ist das Deadlock-Opfer, nicht der Sage-Anwender), typisierte `varchar`-Parameter (Index-Seek unter
  der Sage-Kollation), kein `MAX(PlatzID)`. Indexlage `KHKLagerplaetze` laut P-10 (AKE, Information) in der Spec festgehalten.
- **Fehlerklassen (S3):** technisch = SqlException-Nummern -2, 1205, 1222, 229, 230, 262, 4060, 18456, 233, 10053, 10054, 10060, 53, -1,
  Schweregrad ≥ 20, sowie InvalidOperation/IO/Timeout → Eintrag zurück auf `Offen`, Rest des Takts überspringen, Mail **nur beim Wechsel** des
  Fehlertexts (S7, Merker im Worker), Protokoll-Lauf nur bei neuem Text. Alles andere (THROW 500xx, 515, 207, 208, 2812, 8152, 2627 …) =
  fachlich → Eintrag `Fehler`, Grund an Platz/Outbox, weiter mit dem nächsten, keine Mail. 515/207/208/2812 tragen den Zusatz „Sage-Tabelle oder
  -Prozedur weicht ab – Prüfabfragen P-1 bis P-3“.
- **Updatefestigkeit:** Sage-Versionswechsel können Spalten, Defaults, Trigger und `spSysTan` ändern. Nach jedem Sage-Update vor dem
  Wiedereinschalten P-1 bis P-3 (P-9 bei Lager-Anlage) erneut prüfen, Schalter bis dahin aus. Neue `USER_`-Pflichtfelder ohne Default brechen den
  INSERT ab – sichtbarer fachlicher Fehler 515, kein Teilschreiben.
- **Support DPS:** Direktschreiben ist kein von Sage/DPS unterstützter Weg; DPS wird bewusst nicht informiert (Freigabe-Antwort 6). Abgrenzung im
  Fehlerfall: Outbox und Aktivitäts-Protokoll „Lagerplätze → Sage“ zeigen jeden WMS-Schreibvorgang mit PlatzID, Zeit und Ziel-DB
  (`Sage-Datenbank: <Server>/<DB>` je Lauf).

## Folgen

- Einziger direkter Schreibweg des WMS in eine Sage-Datenbank; alle anderen bleiben SData. Betriebsdoku 05 § 4b / 09 § 2.1 nennen die Ausnahme.
- Voraussetzung: genau **eine** Dienst-Instanz je WMS-DB (wie alle Sage-Schreibwege).
- **Bekannte Folgen (dokumentiert, nicht abgefangen):** Ein hängender Eintrag eines inzwischen deaktivierten Platzes wird nicht mehr gesendet;
  legt Sage ihn doch an, übernimmt ihn der Lagerplatz-Abgleich und **reaktiviert** ihn (H9). Ein neu erfasstes Lager erscheint vor seiner
  Übergabe schon in der Lagerort-Auswahl der Kommissionierbereichs-Regeln (`PickingAreaRepository.cs:36-37`, S10).
- Der Lagerplatz-Abgleich markiert ein Lager mit nicht bestätigter Lager-Outbox nicht als fehlend (`LagerplatzSyncService.SyncStorageAreasAsync`).
- Der SQL-Text ist nur InMemory-frei per Text-Test abgesichert; Transaktion, Sperren und Rechte belegt erst der Abnahmetest TS-192 am Testmandanten.

## Rückbau

Liefert DPS den SData-Service: zweite Implementierung von `ISageLagerplatzAnlage` (SData), DI umstellen, GRANTs entziehen. Outbox, Status,
Worker-Schritt und Oberfläche bleiben unverändert. Diese ADR wird dann superseded.
