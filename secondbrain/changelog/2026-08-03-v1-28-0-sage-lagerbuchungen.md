---
type: changelog
version: 1.28.0
date: 2026-08-03
---
# v1.28.0 — Sage-Lagerbuchungen (ausgehend: WMS → Sage via SData)

- Spec [[2026-07-29-sage-lagerbuchungen-spec]], Freigabe an Schranke 1 (2026-08-03). Backlog-Ursprung
  `../backlog/2026-07-29-Postman-Lagerbuchungen.md`. **Step 1 bewusst klein:** nur manuelle
  `Einbuchung`/`Ausbuchung`, kein BDE-Kanal, keine Serien/Chargen, **kein Umbuchung** (Schranke-1-B1).
- **Gegenrichtung** zu `LagerbestandSyncService` (Sage→WMS, bleibt unberuehrt): manuelle Buchungen
  werden asynchron ueber eine Queue an Sage gemeldet.
- **Enqueue (Web):** Decorator `SageBookingEnqueueingStockMovementRepository` (**Subclassing** von
  `StockMovementRepository`, nur `override AddAsync` — S6), registriert in `IdealAkeWms/Program.cs`.
  Reine Entscheidung `SageBookingEnqueueDecision.ShouldEnqueue` = nur Ein-/Ausbuchung **UND** globaler
  Toggle **UND** Lagerplatz-Flag. Enqueue-Fehler werden gefangen + protokolliert, **nie geworfen** (B4),
  damit die bereits gespeicherte WMS-Buchung nicht scheitert/verloren geht.
- **Sende-Pfad (Service):** neuer `SageBookingWorker` (eigener BackgroundService, Kurztakt
  `Sync:SageLagerbuchungIntervalSeconds`, Default 20s). Je Tick: Reconciliation-Sweep (B4, 15-Min-
  Rueckblick) → Recovery haengender `Gesendet` (S3) → offene senden (`Gesendet` VOR dem HTTP-Call, AK10)
  → Fehler-Cap-Mail. `ISageLagerbuchungClient` (typed HttpClient, Basic-Auth; URL siehe „SData-URL"
  unten), `SageLagerbuchungPayloadBuilder` (rein, testbar).
- **Idempotenz (B2):** Korrelation `SM#<id>#` im Sage-`Memo`; vor jedem **erneuten** Senden (Requeue /
  haengender Gesendet) Read-Lookup gegen `KHKLagerplatzbuchungen` (`SageBuchungLookupReader`, Raw-SQL
  ueber `SageConnection`) — nie blindes Resend. **Dev-Lauf/UAT:** exakte Korrelationsspalte
  (`Memo` vs. `Referenz`) am Testsystem bestaetigen (eine Zeile in `SageBuchungLookupReader`).
- **Payload-Kennung (B3):** volle Kurzbezeichnung = `StorageLocation.Code` (z. B. `LL;1;4;0`), **kein**
  `;0;0;0`. Neue `StorageLocation`-Felder `SageBuchungErlaubt` (Opt-in, Default false, kumulativ zum
  globalen Toggle), `SageLagerkennung` (= Code), `SageLagerplatzId` (= `KHKLagerplaetze.PlatzID`),
  befuellt vom erweiterten `LagerplatzSyncService`/`SageLagerplatzReader` (`lp.PlatzID`).
- **Verbindungstest (2026-08-04):** Button „Sage-Verbindung testen" auf `/ServiceSettings`
  (`ServiceSettingsController.TestSageConnection`, admin-only) — nebenwirkungsfreier **GET** (nie eine
  Buchung) gegen eine waehlbare SData-Resource (Default `$schema`) mit den aktuellen Formularwerten,
  Basic-Auth (im Dialog eingegeben, nicht gespeichert) und dem konfigurierten TLS-Verhalten; zeigt
  Status, **Response-Header** und Body im Modal. Geteilter URL-Builder `IdealAkeWms/Services/SdataUrlBuilder.cs`
  (Web-Test + Service-Client identisch); `SageLagerbuchungClient.BuildServiceUrl` delegiert dorthin.
- **Monitoring-UI (S1):** `/SageBookingQueue` (Listen-View-Pattern, `[RequireStockReadAccess]`) mit
  Requeue-Aktion fuer Fehler-Eintraege (`[RequireStockKeyUserAccess]` = `CanTransferStockAsync`).
  Menuepunkt unter Lager → Sage-Lagerbuchungen. **Keine neue Rolle.**
- **Migrationen:** `82_AddStorageLocationSageLagerbuchung` (3 Spalten) + `83_AddSageBookingQueue`
  (Tabelle `SageBookingQueueItems`, FK→`StockMovements`, Index `Status`). Beide additiv, idempotent,
  `SQL/00_FreshInstall.sql` an beiden Stellen nachgezogen. `SQL/AgentJobs/*` **nicht** betroffen.
- **ServiceSettings** (DB-first, Katalog): `SageLagerbuchungAktiv`, `SData:BaseUrl`,
  `SData:Application`, `SData:ServiceContract`, `SData:Dataset`,
  `Sync:SageLagerbuchungIntervalSeconds/BatchSize/MaxRetries/MaxErrorsPerRun/StuckMinutes`,
  `SageLagerbuchungSslZertifikatPruefen`. **appsettings-only** (Geheimnis, ADR 0008):
  `SageLagerbuchung:Username/Password` (nur Service). Neuer `SyncLogServices.SageLagerbuchung`.
- **SData-URL + Kodierung (2026-08-03):** Ziel-URL =
  `{SData:BaseUrl}/sdata/{SData:Application}/{SData:ServiceContract}/{SData:Dataset}/$service/LagerbuchungService`
  (reiner Builder `SageLagerbuchungClient.BuildServiceUrl`). **Neu konfigurierbar:** `SData:Application`
  (Default `ol`), `SData:ServiceContract` (Default `CommonWawiServices`) — nicht mehr hart verdrahtet.
  **Kodierung:** `;` im Dataset (`ake_TEST2026;1`) und `$` der Resource bleiben **literal** (kein
  `Uri.EscapeDataString`; RFC-3986-sub-delims; .NET-`Uri` laesst sie im Pfad unangetastet). Test
  `SageLagerbuchungClientUrlTests` prueft die exakte Discovery-URL inkl. literalem `;`/`$`.
- **TLS-Schalter (2026-08-03):** `SageLagerbuchungSslZertifikatPruefen` (Bool, Default `true`,
  **fail-safe:** fehlend/unparsebar → geprueft; `IdealAkeWms/Services/SageTlsPolicy.cs`) wirkt **nur**
  auf den `ISageLagerbuchungClient` via `ConfigurePrimaryHttpMessageHandler`. Der
  `ServerCertificateCustomValidationCallback` liest den Wert **zur Laufzeit** aus ServiceSettings
  (nicht bei DI-Registrierung) → Aenderung greift **ohne Dienst-Neustart**. Bei `false`: Warnung im
  Worker-Start-Log + Warnhinweis in `/ServiceSettings` und `/SageBookingQueue`. Grund: Testserver
  `sagetest01.ake.at` hat `PartialChain` (interne PKI unfertig).
- **Deploy-Risiken:** kein daten-destruktiver Schritt. **Server-Handgriffe:** appsettings-Credentials
  am Service ergaenzen; `/ServiceSettings` → `SData:BaseUrl`/`SData:Dataset` + Toggle + je Lagerplatz
  `SageBuchungErlaubt` setzen. **Ein-Instanz-Voraussetzung** (Idempotenz schuetzt nur bei genau einem
  laufenden Worker). Zuerst mit einem Testartikel/Testlagerplatz am Sage-Testsystem beginnen.
- **Tests:** `SageLagerbuchungPayloadBuilderTests`, `SageBookingEnqueueDecisionTests`,
  `SageBookingEnqueueingStockMovementRepositoryTests` (InMemory-Decorator, S7/AK6),
  `SageBookingCorrelationTests`. Build 0 Fehler; Web 1042 + Service 186 gruen (1 uebersprungen).
  Manual-UAT-Kapitel **56** in `../../docs/TESTSZENARIEN.md` (Sende-/SData-Pfad nicht InMemory-testbar).
- Branch `feature/2026-07-29-sage-lagerbuchungen` (Worktree
  `.claude/worktrees/2026-07-29-sage-lagerbuchungen`). **Schranke 2 (manueller Test + Merge) steht aus.**
  Folgearbeit siehe [[2026-08-03-deploy-v1-28-0-sage-lagerbuchungen]].
