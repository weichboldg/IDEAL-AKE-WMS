---
version: 1.99.64
datum: 2026-11-24
branch: feature/2026-08-07-ideal-teile-1-5
commits: [3929f146, 595dc46c, c505688f, f420e23e, c476a628, 1c1ba9bf]
paket: paket18-v1.99.64
---
# v1.99.64 – Hotfix AKE-Prod: SData GROUP BY, AKE-Test-Pfade, Set-Startkennwort

Anlass: Update AKE-Prod auf v1.99.63 (2026-11-24). Nicht gemergt.

## Kernpunkte
- **SData GROUP BY** (`f420e23e`): Sage SData auf AKE-Prod (`akesage100`, `ake;1`) gruppiert nach den `select`-Feldern; ein
  `orderBy`-Feld ausserhalb der Liste ergibt HTTP 500. Betroffen Artikel-Sync (`PpsRessourcenPositionenInfo`, `BelPosID`) und
  Artikel-Lieferant (`ArtikelVariantenExtended`, `AuspraegungID`), SyncLogs 4883760/4883802. Fix zentral:
  `SageSdataReadClient.MitSortfeldern` in `BuildQueryUrl`. Einzel-Fix `c505688f` per `c476a628` zurueckgenommen.
  Live am 2026-11-24 im Browser gegen akesage100 belegt: alte Abfrage reproduziert den Fehler, beide neuen liefern Daten.
  Fallstrick 63. RealTimeData-Weg (`BuildUrl`) nicht abgedeckt (heute unkritisch, QA R-1).
- **ziele.psd1**: AKE-Test `C:\inetpub\wwwroot\IdealAkeWMS-TEST` / `C:\AKE\IDEALAKEWMSService-Test`.
- **deploy\Set-Startkennwort.ps1**: gemeinsames Startkennwort + Pflichtwechsel fuer lokale Benutzer ohne Kennwort
  (`Pruefung/BenutzerOhneKennwort`, AKE-Prod 17 Benutzer). Vorher/Nachher-Schritt.

QA: zwei qa-agent-Laeufe, Befunde in [[2026-11-24-hotfix-deploy-ake-test-startkennwort]]. Build 0 Fehler, Tests Web 3497 / Service 790 gruen.

## Deploy
Keine Migration. Web + Dienst. Nach dem Deploy auf AKE-Prod pruefen: Artikel-Sync und Artikel-Lieferant im Aktivitaets-Protokoll erfolgreich.

## Paket
`publish\paket18-v1.99.64.zip` (Publish aus `1c1ba9bf`; 346 Web, 93 Dienst, 151 SQL, 6 deploy, publish-info.txt): 237 052 539 Bytes,
SHA-256 `D7A7F7D79F21FC487EE752C7B6F8659451C227BD28038AE581BB02B7EFCBE037`; auf `\\AKENET01.ake.at\Installsourcen` und
`\\IDEALWEB01.ideal.ideal-ake.at\InstallSourcen` Groesse + SHA-256 gleich (vorher nicht vorhanden).
