---
type: changelog
version: 1.71.0
date: 2026-10-30
---
# v1.71.0 — FA-Struktur live aus Sage (SData RealTimeData, nur lesen)

Spec [[2026-10-30-sage-sdata-fa-ansicht-spec]], Umsetzung [[2026-10-30-sage-sdata-fa-ansicht-umsetzung]].
Basis `d7dc3d03` (v1.70.0), Commits `00c368d6`, `8c09ab87`, QA-Fix `0fe5e7c7`. **Keine Migration**, nur Web (Dienst nur Version). TS-103.

## Kernpunkte

- Neue Read-only-Seite „FA-Struktur (Sage live)“ (`SageSdataFaController`), Gate `SageSdataFaAnsichtAktiv` (Default aus), Zugriff wie FA-Struktur.
- Lese-Client `SageSdataReadClient` (nur GET, Contract `RealTimeData`, `where` Pflicht): 5 Aufrufe je Seitenaufbau – BelID→StrukturID,
  Struktur, dann je Quelle ein `or`-Aufruf über alle BelIDs (Blöcke à 50) für Positionen, Rückmeldungen, Fertigmeldungen.
- Baumaufbau `SageSdataFaTreeBuilder`: Wurzel `BelID = StrukturID` (MAN oder VK), Sub-FA-Zuordnung nur eindeutig, sonst Warnung.
- Nach dem ersten Verbindungsfehler übrige Aufrufe „übersprungen“; HTTP-Fehler einer Quelle brechen nicht ab.
- TLS-Schalter je Anfrage über `HttpRequestMessage.Options`, kein DB-Zugriff im Zertifikats-Callback.

## Deploy-Risiken

- **Zugangsdaten:** Web-App braucht `appsettings.Production.json` (Abschnitt `SageLagerbuchung`) im Web-Ordner; nicht im Repo. Ein
  Publish, der den Zielordner leert, löscht sie. Passwortwechsel an zwei Stellen (Dienst + Web).
- Netz/Firewall vom IIS-Host zu `sagetest01.ake.at:5493` ungeprüft. Für IDEAL kein SData-Host → Schalter dort aus.
- TLS-Schalter wirkt erst auf neue Verbindungen (bis 2 min oder App-Pool-Recycle).
