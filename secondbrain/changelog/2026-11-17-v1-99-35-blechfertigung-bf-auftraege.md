---
type: changelog
version: 1.99.35
date: 2026-11-17
---
# v1.99.35 — Blechfertigung: BF-Aufträge (Warenkorb, Schachtler-Freigabe, WiCAM-CSV, Fertig, Storno)

Bündel `feature/2026-08-07-ideal-teile-1-5`, Commits `09063984`, `ea21bddd`, `37e83d85`, `89ce487a`, `9f2a0802` (A Datenquelle V-B, B Modell/Web, C CSV-Dienst, D Fertig/Storno/Doku).
Spec [[2026-11-13-blechfertigung-bf-auftraege-wicam-csv-spec]]. Nicht gemergt, qa-agent ausstehend. Lauf-Notiz [[2026-11-17-goal-lauf-bf-auftraege]].

## Kernpunkte

- **Datenquelle V-B:** `IDEALAKEWMSService/Services/Sdata/BlechteileFelderNachlese.cs` liest im View-Modus des Artikel-Abgleichs die
  `USER_OS*`-Felder + Bestellmenge der Grundvariante per SData (nur GET) und schreibt sie über `ArtikelBenutzerfelderWriter` als Merkmale.
  Schalter `Sync:BlechteileFelderNachlesen` (aus). Feldnamen an einer Stelle: `IdealAkeWms/Services/BfArtikelFelder.cs`. 0 Zeilen/Fehler =
  Warnung ohne Schreiben.
- **Modell:** `BfAuftrag` (Status Entwurf/Bestellt/Freigegeben/Fertig/Storniert, Nummer `BF` + 8 Ziffern, Termine, CSV-Status/Fehler/Versuche,
  RowVersion) + `BfAuftragPosition` (Menge, Status, Hauptlagerplatz, Materialnummer, Bemerkung, `Gestrichen`, `Uebergeben`).
- **Web:** `BfAuftraegeController` (Warenkorb, Liste, Details, Freigabe, Fertig einzeln/Liste, Storno, CSV erneut), API
  `GET api/bf-auftraege/artikel`, Logik in `Services/BfAuftragWorkflow.cs`. Gate `RequireBlechfertigungAktiv`, Rolle `schachtler` mit
  `RequireSchachtlerAccess`; Bestellen = `CanBfBestellenAsync` (Lagerbestellung-Rollen oder schachtler).
- **Freigabe:** Termine ab Freigabetag über `BusinessDayService` + Feiertage; Bemerkung `[Status - ][Fertigungsinfo - ]TT.MM[ - Pos bei BG]`;
  Sperren (Mengenregel, `;`/`"`/Zeilenumbruch, nicht Windows-1252, fehlender Matchcode/Lagerplatz, keine übergebbare Position). Ohne
  Materialnummer nur Hinweis, Position nicht übergeben (Freigabe-Antwort 2).
- **CSV (Dienst):** `BfCsvService` im `SageBookingWorker` (eigener try, unabhängig vom Sage-WE): `.tmp` → `File.Move(overwrite:false)`,
  Windows-1252 mit `ExceptionFallback`, CRLF; Kollision = Fehler ohne Auto-Wiederholung; Wiederholung nach Abstand bis Maximum. Inhalt in
  `IdealAkeWms/Services/BfCsv.cs`. **Layout Platzhalter bis P-1.**
- **Mail:** `BfAuftragEmailService` im `SyncWorker`, Schalter `Sync:BfAuftragEmailEnabled`, nur beim Abschicken.
- **Fertig/Storno:** Fertig nur Freigegeben + CSV geschrieben, nur Status. Storno: Besteller eigener Auftrag bis Freigabe, Schachtler bis Fertig
  (Pool-Hinweis), Grund optional ≤ 500.

## Migration / SQL

- **SQL/158** `158_AddBfAuftraege.sql`, MigrationId `20261117154645_AddBfAuftraege`: Tabellen `BfAuftraege`, `BfAuftragPositionen`, additiv,
  FreshInstall an beiden Stellen. AgentJobs nicht betroffen. Rolle `schachtler` per Manifest-Reparatur.
- Manifest `scripts/deploy/nachlauf.psd1`: SQL/158, Prüfungen Rolle/Empfängergruppe/CSV-Pfad/Feiertage, `v1.99.35-BfPoolRecht`,
  `-BlechfertigungAktiv`, `-BfCsv`, `-BlechteileFelder`.

## Deploy-Risiken

- Gering bei Default aus. **Vor `BfCsvAktiv` an:** P-1 (Musterdatei aus dem WiCAM-Pool) abgleichen, P-7 Schreibrecht Dienstkonto.
- Warenkorb-Test braucht View-Block 3 (Lagerartikel in der Sage-View, Gerald).
- Test: TS-157.

## Nachtrag B-1 und Test-CSV (Commit `0b31a17d`, Goal-Lauf [[2026-11-17-goal-lauf-sammel-abend]])

- B-1 Variante (c), **vorläufig, wie Excel, durch P-1 zu bestätigen:** `BfCsv.Feld` fasst Felder mit `;` oder `"` in doppelte
  Anführungszeichen (enthaltene `"` verdoppelt); `;` sperrt die Freigabe nicht mehr (Sage-Lagerplatz `S-03;0;0;0`).
- `BfCsv.KopfAsync` liest die CSV-Kopfwerte für Dienst und Web gemeinsam.
- „Test-CSV herunterladen“ (schachtler/admin, POST `BfAuftraege/TestCsv`): derselbe Erzeuger, Download `<BF-Nummer>.csv`, kein Statuswechsel,
  nichts im Pool, Eintrag im Aktivitäts-Protokoll `BfAuftragCsv`. Kein Register „WiCAM-Test“. TS-157.21–157.23.
