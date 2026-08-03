---
type: aufgabe
title: Umsetzung Sage-Lagerbuchungen (SData-API, Queue + Worker)
status: InArbeit
created: 2026-08-03
spec: "[[2026-07-29-sage-lagerbuchungen-spec]]"
worktree: ".claude/worktrees/2026-07-29-sage-lagerbuchungen"
branch: "feature/2026-07-29-sage-lagerbuchungen"
---

# Umsetzung Sage-Lagerbuchungen

Ausfuehrungsplan zur freigegebenen Spec [[2026-07-29-sage-lagerbuchungen-spec]].
Massgeblich: der Abschnitt **„Schranke-1-Antworten — konsolidiert und geprueft"** am Spec-Ende.

## Verbindliche Entscheidungen (Schranke 1)
- **Scope:** nur `Einbuchung`/`Ausbuchung` (B1: Umbuchung RAUS).
- **Flag:** `SageBuchungErlaubt`, positives Opt-in, Default `false`, kumulativ zum globalen
  Toggle `SageLagerbuchungAktiv` (UND).
- **Payload-Kennung (B3):** volle Kurzbezeichnung = `StorageLocation.Code` (z. B. `LL;1;4;0`),
  **kein** `;0;0;0`. `SageLagerplatzId` = `KHKLagerplaetze.PlatzID`.
- **Idempotenz (B2):** `Memo`/Korrelation (`StockMovement.Id`) + Read-Lookup gegen Sage-Tabelle
  `KHKLagerplatzbuchungen` vor Requeue/Recovery — nie blindes Resend.
- **B4:** Enqueue-Fehler im Decorator fangen + protokollieren, nie werfen; Reconciliation-Sweep.
- **S1:** Monitoring-Liste `/SageBookingQueue` inkl. Requeue ist In-Scope.
- **S3:** haengende `Gesendet` ueber B2-Lookup aufloesen.
- **S6:** Decorator als **Subclassing** (`: StockMovementRepository`, nur `override AddAsync`).
- **S7:** hartes Regressions-Kriterium „bei Toggle aus bit-identisch".
- **S5:** Standorteinstellungs-Maske ist **out-of-scope** → eigene Backlog-Notiz.

## Phasen (jede endet mit gruenem `dotnet build`, wip-Commit)

### Phase A — Fundament (Modelle + Schema)
- [ ] `StorageLocation`: `SageBuchungErlaubt` (bool), `SageLagerkennung` (nvarchar50?), `SageLagerplatzId` (int?)
- [ ] `SageBookingQueueStatus` enum (Offen/Gesendet/Bestaetigt/Fehler)
- [ ] `SageBookingQueueItem : AuditableEntity`
- [ ] `ApplicationDbContext`: DbSet + Entity-Config (FK StockMovement, Index Status)
- [ ] `ServiceSettingDefinitions.All`: SageLagerbuchungAktiv, SData:BaseUrl, SData:Dataset,
      Sync:SageLagerbuchungIntervalSeconds, Sync:SageLagerbuchungMaxRetries, Cap
- [ ] `SyncLogServices`: Konstante `SageLagerbuchung`
- [ ] Migration 82 (StorageLocation-Felder) + SQL/82 + FreshInstall (2 Stellen)
- [ ] Migration 83 (SageBookingQueueItems) + SQL/83 + FreshInstall (2 Stellen)

### Phase B — Web: Enqueue-Pfad
- [ ] `ISageBookingQueueRepository` + `SageBookingQueueRepository`
- [ ] `SageBookingEnqueueDecision` (reiner Helper, testbar)
- [ ] `SageBookingEnqueueingStockMovementRepository : StockMovementRepository` (override AddAsync, try/catch)
- [ ] `Program.cs` (Web): Decorator-Registrierung + Repo + Decision-DI
- [ ] `StorageLocationsController`: Create/Edit `SageBuchungErlaubt` + ColumnMap „sage-buchung"
- [ ] Views StorageLocations: Index (Spalte), Create/Edit (Feld)

### Phase C — Service: Sende-Pfad
- [ ] `SageLagerbuchungPayloadBuilder` (rein, testbar) + Payload-DTOs
- [ ] `ISageLagerbuchungClient` + `SageLagerbuchungClient` (typed HttpClient, Basic-Auth, POST)
- [ ] `ISageBuchungLookupReader` + Impl (Raw-SQL gegen `KHKLagerplatzbuchungen`, B2)
- [ ] `SageLagerplatzReader` + `SageLagerplatzDto`: `PlatzID` selektieren
- [ ] `LagerplatzSyncService`: `SageLagerkennung`(=Code) + `SageLagerplatzId` schreiben
- [ ] `SageBookingWorker : BackgroundService` (Poll, ISyncLogger, Send, Reconcile-Sweep, Fehlermail)
- [ ] `Program.cs` (Service): AddHttpClient + Worker + Reader-DI
- [ ] appsettings(.Development).json: Block `SageLagerbuchung:Username/Password`

### Phase D — Monitoring-UI (S1)
- [ ] `SageBookingQueueController` (`[RequireStockReadAccess]`, Requeue `[RequireStockKeyUserAccess]`)
- [ ] ViewModel + `Views/SageBookingQueue/Index.cshtml` (Listen-View-Pattern)
- [ ] Requeue-Action mit B2-Lookup

### Phase E — Tests + Doku + Version + Brain
- [ ] `SageLagerbuchungPayloadBuilderTests` (Zugang/Entnahme spiegelbildlich)
- [ ] `SageBookingEnqueueDecisionTests` (Toggle/Flag/Sage-Typ-Ausschluss)
- [ ] `SageBookingWorkerTests` (Invariante ohne DB, soweit machbar)
- [ ] `docs/TESTSZENARIEN.md` neues Kapitel + `secondbrain/tests/testszenarien-index.md`
- [ ] `AppVersion.cs` (Web + Service) + `Views/Help/Changelog.cshtml`
- [ ] Brain: changelog, feature-map, codebase/{integrationen,services,datenmodell}, fallstricke
- [ ] Backlog-Notiz „Standorteinstellungs-Maske" (S5)

### Phase F — QA (Schranke vor Testbereit)
- [ ] `dotnet build` + `dotnet test` gruen (Beweis in Spec)
- [ ] qa-agent: Deploy-Abschnitt finalisieren, status Testbereit + manuelle Checkliste

## Offene Dev-Lauf-Verifikationen am Sage-Testsystem (Manual-UAT, nicht im Code loesbar)
- B2: exakte Korrelationsspalte (`Memo` vs. `Referenz`) + reales Antwortformat `LagerbuchungService`.
- Frage 7: `KHKLagerplaetze.PlatzID` als Quellspalte bestaetigen.
- Frage 4: `Article.ArticleNumber == Sage-Artikelnummer`.
