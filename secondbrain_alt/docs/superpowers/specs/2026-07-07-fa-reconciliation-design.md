# FA-Reconciliation (verwaiste Produktionsaufträge stornieren) — Design

**Datum:** 2026-07-07
**Status:** Approved-pending-review
**Branch/Worktree:** `feature/glas-bestellung` (@ `1504573`) → `.claude/worktrees/glas-bestellung`
**Version:** in **v1.25.0** falten (noch nicht released) — kein Versionssprung.

In Sage werden Produktionsaufträge (FAs) teils gelöscht, existieren in der WMS-App aber weiter als offen. Der FA-Sync soll beim Abgleich erkennen, welche in der WMS offenen FAs **nicht mehr in Sage** sind, und sie **stornieren**.

---

## Ziel / Anforderung

1. Beim FA-Abgleich prüfen, ob alle in der WMS **offenen** FAs noch in Sage vorhanden sind.
2. Wenn nein → bei uns auf **storniert** setzen (verschwindet aus offenen Sichten).
3. Taucht eine stornierte FA später wieder in Sage auf → **auto-reaktivieren**.
4. **Sicher:** kein versehentliches Massen-Stornieren bei Sage-Aussetzern.

---

## Datenmodell

Drei additive Felder an `IdealAkeWms/Models/ProductionOrder.cs`:

- `public bool IsCancelled { get; set; }` — default `false`.
- `public DateTime? CancelledAt { get; set; }`
- `public string? CancelledBy { get; set; }` — `nvarchar(256)` NULL (z. B. `"System-Reconcile"`).

**Löschen ist keine Option:** `PickingItem` und `PartRequisition` haben `OnDelete Restrict` auf `ProductionOrder` → ein Delete würde FK-Fehler werfen. Status setzen ist sicher (keine Kaskaden angefasst; PickingItems/PartRequisitions bleiben historisch erhalten).

**Migration 80** `AddProductionOrderCancellation` (additiv, Default `IsCancelled=0`) + idempotentes `SQL/80_AddProductionOrderCancellation.sql` (COL_LENGTH-Guards, History-Insert separater Batch) + `SQL/00_FreshInstall.sql` (Spalten im konsolidierten ProductionOrder-Schema + History-Insert). Alt-FAs bleiben `IsCancelled=0` → unverändert offen.

---

## „Offen" überall erweitern: `!IsDone && !IsCancelled`

Stornierte FAs verhalten sich wie erledigte — raus aus allen offenen Sichten. Jede Query, die heute „offen" über `!IsDone` (bzw. `IsDone || IsDonePicking`) definiert, bekommt zusätzlich `&& !IsCancelled`:

- `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs`:
  - `GetOpenOrdersAsync` (Z. 82–85)
  - `GetOpenOrdersInWindowAsync` (Z. 110–125)
  - `GetForLeitstandAsync` (Z. 41–42, `if (!showDone)`)
  - FA-Liste (`ProductionOrdersController.Index` / `GetAllOrderedAsync`): an derselben Stelle, an der erledigte FAs standardmäßig ausgeblendet werden, auch stornierte ausblenden.
- FA-Vervollständigung / FA-Abarbeitungsliste: `FaCompletionController.Index`, `FaWorklistController.Index` (dieselben Offen-Filter, in denen bereits `IsDone || IsDonePicking` geprüft wird).
- **Raw-SQL-Pfade (Service):**
  - `BomCacheSyncService.ReadOpenOrdersInWindowAsync` — `AND IsCancelled = 0` ergänzen (analog dem bestehenden `IsDone=0 AND NOT EXISTS(... IsDonePicking ...)`).
  - `FaWorkStepDetectionService` — beide Kandidaten-Queries (`matchedFaCount` **und** `candidates`) um `AND IsCancelled = 0` ergänzen.

> **Fallstrick (dokumentieren):** Die Raw-SQL-Pfade (BomCache, Detection) sind **nicht** InMemory-testbar — nur die EF-Repo-Änderungen sind es. Bei Änderungen an der Offen-Definition immer beide Ebenen anfassen (EF **und** Raw-SQL), sonst zeigt der Cache stornierte FAs weiter.

---

## Reconcile-Logik

**Reiner Helper** `IDEALAKEWMSService/Services/ProductionOrderReconciler.cs` (voll unit-testbar, keine DB):
```csharp
public sealed record ReconcilePlan(
    IReadOnlyList<string> ToCancel,      // OrderNumbers: WMS-offen, nicht in Sage
    IReadOnlyList<string> ToReactivate,  // OrderNumbers: WMS-storniert, wieder in Sage
    bool Skipped, string? SkipReason);

public static ReconcilePlan Plan(
    IReadOnlyCollection<string> sageOrderNumbers,   // aus der View gelesen
    IReadOnlyCollection<WmsOrderState> wmsOrders,   // (OrderNumber, IsDone, IsCancelled)
    int maxCancelPerRun);                            // Sicherheits-Cap
```
Regeln:
- **Guard leer:** `sageOrderNumbers.Count == 0` → `Skipped=true, SkipReason="Sage-Read leer"` (nichts stornieren/reaktivieren).
- **Reaktivieren:** WMS-FA mit `IsCancelled=1`, deren OrderNumber in `sageOrderNumbers` ist → `ToReactivate`.
- **Stornieren:** WMS-FA mit `IsDone=0 && IsCancelled=0`, deren OrderNumber **nicht** in `sageOrderNumbers` ist → `ToCancel`.
- **Cap:** `ToCancel.Count > maxCancelPerRun` → `Skipped=true, SkipReason="Cap überschritten (N > max)"`, `ToCancel` geleert (Reaktivierungen dürfen trotzdem laufen). Schützt vor Teil-Reads der View.

**Verdrahtung** in `IDEALAKEWMSService/Services/SageImportService.cs` → `SyncProductionOrdersAsync` (Z. 30–250), **nach** dem regulären Upsert und **innerhalb** desselben SyncLog-Runs (`SyncLogServices.ProductionOrder`):
- OrderNumber-Set der Sage-View wird beim Import ohnehin gelesen → sammeln.
- WMS-Offen-/Storniert-Zustände laden (`SELECT OrderNumber, IsDone, IsCancelled FROM ProductionOrders WHERE IsDone=0 OR IsCancelled=1`).
- `ProductionOrderReconciler.Plan(...)` aufrufen.
- **Nur wenn Flag aktiv** (`Sync:ProductionOrderReconcileEnabled`, default **false**):
  - `ToReactivate` → `UPDATE ... SET IsCancelled=0, CancelledAt=NULL, CancelledBy=NULL` (Count `reaktiviert`).
  - `ToCancel` → `UPDATE ... SET IsCancelled=1, CancelledAt=@now, CancelledBy='System-Reconcile'` (Count `storniert`).
  - `Skipped` (Guard/Cap) → **kein** Storno; **Warnung** ins Aktivitäts-Protokoll; bei Cap zusätzlich **Fehlermail** über den vorhandenen `SyncErrorNotifier` (aus dem Service-Sync-Fix) mit Detail (Sage-Count, Kandidaten-Count, Cap).
- **DryRun** (`WorkerSettings:SyncDryRun` oder Flag noch aus): Plan berechnen + Counts loggen, **nichts** schreiben. So kann der Admin vor Scharfschalten im Log sehen, was storniert würde.
- Counts-Keys im `FinishSuccessAsync`-Dictionary ergänzen: `storniert`, `reaktiviert` (deutschsprachig, konsistent mit `neu/aktualisiert`).

**Kein Reconcile im AgentJob** `SQL/AgentJobs/01_Import_Produktionsauftraege.sql`: ein guard-loses `WHEN NOT MATCHED BY SOURCE` würde bei leerem/teilweisem View **alle** offenen FAs stornieren. Reconcile bleibt Service-seitig (mit Guard + Cap). Kommentar im AgentJob, der darauf hinweist.

---

## Konfiguration (Service — appsettings.json / ServiceSettings)

| Key | Default | Beschreibung |
|-----|---------|-------------|
| `Sync:ProductionOrderReconcileEnabled` | `false` | Verwaiste FAs (in Sage gelöscht) automatisch stornieren. Opt-in — Admin schaltet nach DryRun-Kontrolle scharf. |
| `Sync:ReconcileMaxCancelPerRun` | `100` | Sicherheits-Cap: mehr Stornokandidaten je Lauf → kein Storno + Fehlermail (Schutz vor Sage-Teil-Reads). |

`SyncErrorNotifier` (bereits vorhanden) wird für die Cap-Fehlermail wiederverwendet; die `ErrorNotification`-Settings (`Enabled`, `Recipients`) gelten.

---

## UI

Stornierte FAs sind wie erledigte standardmäßig aus offenen Sichten ausgeblendet. Minimal:
- Wo erledigte FAs sichtbar gemacht werden (`ProductionOrders/Index` bzw. Leitstand mit „showDone"): stornierte mit Badge **„In Sage gelöscht"** (rot/grau) statt „erledigt" kennzeichnen, damit der Unterschied im Reporting sichtbar ist.
- Kein neues Menü, keine neue Rolle, keine Bulk-Aktion in dieser Iteration (manuelles Reaktivieren = später, falls gewünscht).

---

## Tests

- **`ProductionOrderReconciler.Plan`** (Service-Unit-Tests, umfassend):
  - leerer Sage-Read → `Skipped` (Guard), nichts.
  - verwaiste offene FA → `ToCancel`.
  - erledigte (IsDone) FA nicht in Sage → **nicht** storniert (nur offene).
  - bereits stornierte, wieder in Sage → `ToReactivate`.
  - stornierte, weiter nicht in Sage → bleibt (kein Doppel-Storno).
  - `ToCancel > Cap` → `Skipped` (Cap), Reaktivierungen bleiben erlaubt.
- **Offen-Query-Änderungen** (`ProductionOrderRepository`, InMemory): storniert wird aus GetOpen/GetForLeitstand/GetAllOrdered ausgeblendet.
- **Verdrahtung + Raw-SQL (BomCache/Detection) + SMTP** = **Manual-UAT** (nicht InMemory-testbar). TESTSZENARIEN mit DryRun-Kontroll-Lauf.

---

## Deploy

- Migration 80 additiv (kein Datenverlust, Alt-FAs `IsCancelled=0`). FreshInstall + History synchron.
- **Reconcile ist per Default AUS.** Ablauf für den Admin: Deploy → einen Sync mit `SyncDryRun` (oder Flag noch aus) laufen lassen → Aktivitäts-Protokoll prüfen, wie viele FAs storniert würden → bei Plausibilität `Sync:ProductionOrderReconcileEnabled=true` setzen.
- DB-Backup vor dem ersten scharfen Lauf empfohlen.
- Voraussetzung/Annahme (bestätigt): die Sage-View `vw_AKE_Kommissionierung_WAListe` liefert **alle** offenen FAs (nicht zeitlich gefenstert) → jede WMS-offene FA, die nicht darin ist, ist wirklich in Sage weg. Guard + Cap fangen View-Ausfälle ab.

## Out of scope / Annahmen

- FA-Schlüssel für den Abgleich ist `OrderNumber` (derselbe Key, den der Import zum Matchen nutzt). Die IDEAL-Sonderspalte `SubOrderNumber` (separater Branch) ist auf diesem Branch nicht Teil des Schlüssels — bei einer späteren Zusammenführung mit dem IDEAL-Schema ist der Reconcile-Key erneut zu prüfen.
- Kein manuelles Reaktivieren-UI, keine Storno-Historie-Ansicht in dieser Iteration.
- Reconcile nur im Service, nicht im AgentJob.
