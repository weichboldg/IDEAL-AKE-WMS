# IDEAL-Standort-Anpassungen auf aktuellem main neu nachbilden

> **Herkunft:** Diese Änderungen existierten im (veralteten) Branch `feature/ideal-anpassungen-v1`
> (Worktree `.claude/worktrees/ideal-anpassungen-v1`, Basis ~v1.12.0). Der Branch ist gegenüber dem
> heutigen main zu alt zum Mergen. Statt zu mergen: die **im Worktree enthaltenen** IDEAL-eigenen
> Änderungen auf einem frischen Feature-Branch von main NEU aufsetzen. Original-Spec/Plan als
> Detail-Referenz im alten Worktree: `docs/superpowers/specs/2026-05-15-ideal-anpassungen-v1-design.md`
> + `plans/2026-05-15-ideal-anpassungen-v1.md` + `docs/V1.12.0-VERIFICATION.md`.

## Ziel & Kontext

Der IdealAkeWms soll an einem ZWEITEN IDEAL-Standort laufen — als eigenständiges Deployment,
**KEINE Multi-Tenancy im Code** (jeder Standort eigene DB + eigene Sage-Views + eigene
`appsettings.json`). Kern: Sage-Views auf konfigurierbare `vw_IDEAL-AKE_*` umstellen, 2-Ebenen-
FA-Hierarchie mit **invertiertem** Schema einführen, Belegnummer durchreichen, BOM-Artikelmatchcode
und ein OSEON-analoges Tree-View.

## Design-Entscheidungen (verbindlich)

- **D1 FA-Hierarchie INVERTIEREN:** `OrderNumber` = FA-Nr (Sage StrukturID), **NICHT unique**; neue Spalte
  `SubOrderNumber` = subFA-Nr (Sage BelID), **UNIQUE**. Hauptauftrag ⇔ `OrderNumber == SubOrderNumber`.
- **D2 Belegnummer:** neue optionale Spalte `DocumentNumber` NVARCHAR(100) NULL, in UI default-hidden.
- **D3 Tree-View:** 2-Ebenen-FA-Hierarchie, OSEON-analog (server-gruppiert + Vanilla-JS-Toggle,
  **KEIN d3.js** — „D3" ist die Decision-Nummer, nicht die Library).
- **D4 BOM-Position** bleibt `string?` (keine Schema-Änderung).
- **D5 Sage-Views** einheitlich `vw_IDEAL-AKE_*` (DBAs korrigieren die innere CTE-Referenz; WMS kennt nur
  den äußeren View-Namen).
- **D6 Mandant-Filter: ZURÜCKGENOMMEN** — die View hat KEINE `[Mandant]`-Spalte. KEIN Mandant-Filter im
  Code/appsettings. (Historie: initial eingeführt, nach Live-Test wieder entfernt.)
- **D7 Backfill:** `UPDATE ProductionOrders SET SubOrderNumber = OrderNumber WHERE SubOrderNumber IS NULL`.
- **D8 BOM:** nur `Artikelmatchcode` importieren (`Hauptlagerplatz` NICHT).
- **D9** nur `Fertigungstermin → ProductionDate`; `DeliveryDate` bleibt im Schema, wird vom Sync nicht mehr
  beschrieben.
- **D10 BomRepository:** `[ake]`-Präfix entfernen → `FROM [dbo].[{viewName}]` (DB kommt aus dem
  ConnectionString).

## Umzusetzende Änderungen

**a) FA-Hierarchie-Schema** — Model `ProductionOrder.cs`: `SubOrderNumber` `[Required][StringLength(100)]=""`,
`DocumentNumber` `string? [StringLength(100)]`. `ApplicationDbContext`: `SubOrderNumber` IsRequired +
MaxLength(100), `DocumentNumber` MaxLength(100); UNIQUE-Index von `OrderNumber` → `SubOrderNumber`
verschieben, zusätzlich **nicht-unique** Index auf `OrderNumber` (für Group-By). Neue Migration (siehe
Migrations-Hinweis): Spalten nullable add → Backfill (D7) → ALTER NOT NULL → alten UNIQUE droppen →
UNIQUE-Index `IX_ProductionOrders_SubOrderNumber` + Index `IX_ProductionOrders_OrderNumber`. Alle Schritte
OBJECT_ID/sys.columns/sys.indexes/sys.key_constraints-geguardet (idempotent), FreshInstall + History mitziehen.

**b) Sage-Import** (`SageImportService.cs` + `SQL/AgentJobs/01_Import_Produktionsauftraege.sql`): View
`vw_IDEAL-AKE_Kommissionierung_WAListe`, Name aus Config `Sync:SageWaListeViewName` (Whitelist-Regex
`^[A-Za-z0-9_\-]+$`, sonst `InvalidOperationException`). Mapping: `[FA-Nummer]→OrderNumber`,
`[subFA-Nummer]→SubOrderNumber`, `[Belegnummer]→DocumentNumber`, `[Stückzahl]→Quantity`, `[Kunde]→Customer`,
`[Artikelnummer]→ArticleNumber`, `[Bezeichnung1/2]→Description1/2`, `[Fertigungstermin]→ProductionDate`;
`[Produktionstermin]` und `[Liefertermin]` NICHT mehr. `WHERE [subFA-Nummer] IS NOT NULL`, `SELECT DISTINCT`.
MERGE/Upsert-Key auf `SubOrderNumber`; UPDATE-SET schreibt auch `OrderNumber`+`DocumentNumber` (Sub-FA-
Umhängung); Change-Detection um `OrderNumber`/`DocumentNumber`, `DeliveryDate` raus. **KEIN Mandant-Filter (D6).**

**c) SubOrderNumber-Durchzug:** `ProductionOrderViewItem` (+`SubOrderNumber`, +`DocumentNumber`,
`IsMainOrder => OrderNumber==SubOrderNumber` ordinal); `ProductionOrdersController` Index-Mapping +
Freitextsuche auch in `SubOrderNumber`; `IProductionOrderRepository`/`ProductionOrderRepository`:
`GetBySubOrderNumberAsync` + `SearchAsync` sucht auch `SubOrderNumber`; `WorkOperationRepository`
Scan/QR-Lookup von `OrderNumber` → `SubOrderNumber` (QR trägt an Index 2 weiterhin die BelID = jetzt
`SubOrderNumber`; Frontend-Scanner unverändert). **Regel:** eindeutige Lookups → `SubOrderNumber`;
Gruppen-Lookups (alle Sub-FAs einer Haupt-FA) → `OrderNumber`.

**d) Tree-View** (`ProductionOrders/Index.cshtml` + neues Partial `_ProductionOrderRowCells.cshtml` +
`site.css`): server-seitige Gruppierung nach `OrderNumber`, 25 Gruppen/Seite (Skip/Take auf Distinct-
OrderNumber-Keys), `ProductionOrderGroupViewItem{MainOrderNumber, MainOrder?, SubOrders}`, Chevron-Toggle
wie OseonTracking (kein Bootstrap-Collapse), Phantom-Header wenn Haupt-Row nicht in der Page-Slice,
Auto-Expand bei Sub-FA-Filtertreffer, Expand/Collapse-All. `ColumnDefinitions`: `sub-order-number` „Sub-FA".

**e) Belegnummer:** `ColumnDefinitions` `document-number` „Belegnr." (default-hidden) + column-config-JSON + th/td.

**f) BOM** (0226037): View `vw_IDEAL-AKE_Kommissionierung_StuecklistenDB` aus Config
`Sync:SageStuecklisteViewName` (Whitelist), `[ake]`-Präfix entfernen (D10). Neue Spalte `Artikelmatchcode`
`string?`/NVARCHAR(100) NULL in `BomRepository` (SELECT zwischen `Bezeichnung2` und `Menge`; OSEON-SP-Fallback
= null), `BomCacheSyncService` (SELECT + Bulk-Copy-ColumnMapping + **ContentHash**), `BomCacheItem`/
`CachedBomItem` `[MaxLength(100)]`, `BomViewModels` (BomItem/BomItemViewModel/PrintBomItem),
`BomCacheRepository`. Eigene Migration für die `CachedBomItems`-Spalte. `Hauptlagerplatz` NICHT importieren.

**g) Config** (Deployment, **NICHT** ServiceSettings-Admin-UI): `IDEALAKEWMSService/appsettings.json` + Web
`appsettings.json` unter `"Sync"`: `SageWaListeViewName`, `SageStuecklisteViewName`. Über `_configuration[...]`
gelesen. README + PROJECT_STATUS: per-Standort-Block + AgentJobs auf `vw_IDEAL-AKE_*`.

**h) Tests:** `ProductionOrderRepositoryTests` (`GetBySubOrderNumberAsync`, mehrere Sub-FAs je `OrderNumber`
erlaubt), `ProductionOrderViewModelTests` (`IsMainOrder` + Grouping/Phantom-Header), Whitelist-Smoke für
Sage + BOM.

## Migrations-Hinweis (WICHTIG)

Die Original-IDEAL-Migrationen lagen auf `SQL/60` + `SQL/61` mit EF-Timestamps `20260515…` — auf dem heutigen
main sind **60/61 anderweitig belegt** (`60_ProductionOrderSplit.sql`, `61_ExtendStorageLocationCodeTo50.sql`)
und main läuft bis `SQL/81`. Also: die zwei IDEAL-Migrationen auf die **nächsten freien Nummern (ab SQL/82)**
legen und die EF-Migrationen mit **neuen, aktuellen Timestamps** generieren (damit sie hinter dem bestehenden
main-Stand einsortieren — sonst spielt EF „alte" Migrationen gegen eine bereits weiter migrierte DB nach).
`SQL/00_FreshInstall.sql` + `__EFMigrationsHistory`-Inserts entsprechend. Alle SQL idempotent mit
OBJECT_ID/COL_LENGTH/sys.indexes-Guards.

## Achtung: Kollision mit main-Feature FA-Zusatzinfos (v1.26.0)

main hat seit der alten IDEAL-Basis u. a. **FA-Zusatzinfos (Sage)** inkl. Auto-Erledigt + BDE-Sperre bekommen,
deren `[WA Nummer]`-Match auf `ProductionOrders.OrderNumber` (heute unique) beruht. Nach der FA-Hierarchie-
Inversion ist `OrderNumber` **NICHT mehr unique** — `FaZusatzinfoSyncService`, FA-Reconciliation und alle
sonstigen `OrderNumber`-basierten Unique-Lookups im aktuellen main auf `SubOrderNumber`-Kompatibilität prüfen
(die FA-Zusatzinfos-Spec sieht Mehrfach-Treffer je WA bereits vor). **Diese Wechselwirkung adversarial reviewen.**

## Vorgehen

superpowers-Workflow: Spec (aus der Original-IDEAL-Spec + diesem Backlog) → Plan → agentenbasiert mit TDD
(Implementer + Review je Task) → Build `IdealAkeWms.slnx` + beide Testsuiten grün → Manual-Testszenarien
(inkl. V1.12.0-VERIFICATION-Punkte: BOM-Menge-Check, Scan/QR-Lookup auf `SubOrderNumber`, Tree-Expand/Filter).
AppVersion Web+Service + Changelog. Eigener Worktree/Branch von main.
