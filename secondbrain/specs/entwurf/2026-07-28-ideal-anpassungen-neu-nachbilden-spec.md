---
type: spec
title: IDEAL-Standort-Anpassungen neu nachbilden — FA-Hierarchie-Inversion (Sub-FA), Belegnummer, konfigurierbare Sage-Views, BOM-Artikelmatchcode
slug: 2026-07-28-ideal-anpassungen-neu-nachbilden-spec
status: Entwurf
created: 2026-07-28
updated: 2026-07-28
source_backlog: "[[2026-07-27-ideal-anpassungen-neu-nachbilden]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Models/ProductionOrder.cs
  - IdealAkeWms/Data/ApplicationDbContext.cs
  - IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs
  - IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs
  - IdealAkeWms/Data/Repositories/WorkOperationRepository.cs
  - IdealAkeWms/Data/Repositories/BomRepository.cs
  - IdealAkeWms/Data/Repositories/BomCacheRepository.cs
  - IdealAkeWms/Controllers/ProductionOrdersController.cs
  - IdealAkeWms/Controllers/BdeApiController.cs
  - IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs
  - IdealAkeWms/Models/ViewModels/BomViewModels.cs
  - IdealAkeWms/Models/CachedBomItem.cs
  - IdealAkeWms/Views/ProductionOrders/Index.cshtml
  - IdealAkeWms/wwwroot/css/site.css
  - IDEALAKEWMSService/Services/SageImportService.cs
  - IDEALAKEWMSService/Services/SageProductionOrderSql.cs
  - IDEALAKEWMSService/Services/BomCacheSyncService.cs
  - IDEALAKEWMSService/Services/FaZusatzinfoSyncService.cs (Review, ggf. Anpassung)
  - IDEALAKEWMSService/Services/ProductionOrderReconciler.cs (Review, ggf. Anpassung)
  - SQL/AgentJobs/01_Import_Produktionsauftraege.sql
  - SQL/82_*.sql (neu, FA-Hierarchie)
  - SQL/83_*.sql (neu, BOM Artikelmatchcode)
  - SQL/00_FreshInstall.sql
  - IDEALAKEWMSService/appsettings.json
  - IdealAkeWms/appsettings.json
  - README.md
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "BLOCKIEREND: Reconciliation/FA-Zusatzinfos-Granularitaet der Sage-Views (WaNummer = OrderNumber-Ebene oder SubOrderNumber-Ebene?) ungeklaert — siehe Kollisionsanalyse"
  - "Physischer Ist-Zustand der zweiten Standort-DB (SubOrderNumber evtl. bereits vorhanden) vor Migrationsdesign zu klaeren"
  - "Sync:SageWaListeViewName/Sync:SageStuecklisteViewName: appsettings-only vs. ServiceSettingDefinitions-Katalog"
  - "FA-Reconciliation-SQL (OrderNumber-Update) auf SubOrderNumber umstellen? Fachliche Freigabe noetig"
  - "Auto-Erledigt (FA-Zusatzinfos) je Sub-FA oder nur Hauptauftrag?"
  - "Zielinstallation: welche Datenbank/welcher Standort erhaelt diese Aenderung wirklich (IDEAL only oder beide Linien im selben Codebestand)?"
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Der IdealAkeWms soll an einem **zweiten IDEAL-Standort** produktiv laufen — als eigenständiges
Deployment (eigene DB, eigene Sage-Views, eigene `appsettings.json`), **keine Multi-Tenancy im
Code**. Der IDEAL-Standort bildet Fertigungsaufträge zweistufig ab: eine Haupt-FA-Nummer
(„WA-Nummer", Sage StrukturID) kann mehrere Unteraufträge („Sub-FA", Sage BelID) haben. Das
heutige Schema von `ProductionOrders` kennt nur eine flache, **eindeutige** `OrderNumber` und kann
diese Struktur nicht abbilden — ein Import würde am zweiten Datensatz mit derselben WA-Nummer
scheitern oder (siehe Ist-Zustand) nur notdürftig durchlaufen.

Diese Spec bildet die im alten, nie gemergten Branch `feature/ideal-anpassungen-v1` (Basis
~v1.12.0, seit 2026-07-27 gelöscht) enthaltenen IDEAL-Anpassungen **neu auf dem heutigen main**
nach: FA-Hierarchie invertieren (`OrderNumber` = Haupt-FA, nicht mehr unique;
`SubOrderNumber` = Sub-FA, neu, unique), Belegnummer durchreichen, Sage-Quell-Views je Standort
konfigurierbar machen, BOM-Artikelmatchcode übernehmen, und ein zweistufiges Tree-View für die
FA-Liste analog zur OSEON-Teileverfolgung.

Nutzen: Der zweite Standort bekommt eine korrekte, technisch abgesicherte Abbildung seiner
Sage-Struktur, ohne die bestehende AKE-Linie (main, `vw_AKE_Kommissionierung_*`, `OrderNumber`
unique) zu brechen — beide Linien laufen auf demselben Codebestand mit standortspezifischer
Konfiguration.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**

1. Schema-Erweiterung `ProductionOrders`: `SubOrderNumber` (neu, `NOT NULL` + `UNIQUE`),
   `DocumentNumber` (neu, optional), Unique-Index von `OrderNumber` auf `SubOrderNumber`
   verschoben, zusätzlicher **nicht-eindeutiger** Index auf `OrderNumber`.
2. Sage-Import (`SageImportService.SyncProductionOrdersAsync` **und**
   `SQL/AgentJobs/01_Import_Produktionsauftraege.sql`) auf die konfigurierbare View
   `vw_IDEAL-AKE_Kommissionierung_WAListe` inkl. Sub-FA-/Belegnummer-Mapping.
3. Durchzug von `SubOrderNumber` durch Lese-/Such-/Scan-Pfade, wo eine **eindeutige** FA-Referenz
   gebraucht wird (Regel: eindeutige Lookups → `SubOrderNumber`; Gruppen-Lookups → `OrderNumber`).
4. Zweistufiges, server-gruppiertes Tree-View in der FA-Liste (`ProductionOrders/Index`).
5. Neue Spalte „Belegnr." (`DocumentNumber`), default-hidden.
6. BOM-Erweiterung um `Artikelmatchcode` (Sage-View `vw_IDEAL-AKE_Kommissionierung_StuecklistenDB`,
   konfigurierbarer View-Name, `[ake]`-Präfix-Entfernung in `BomRepository`).
7. Deployment-Konfiguration für die beiden neuen Sage-View-Namen.
8. Migrations-/SQL-Disziplin nach ADR 0004 für beide Schema-Änderungen (Nummern ab `SQL/82`).
9. Adversariale Prüfung der Kollision mit FA-Zusatzinfos v1.26.0 (`OrderNumber` nicht mehr
   eindeutig) und der FA-Reconciliation (v1.25.0).
10. Testszenarien für `docs/TESTSZENARIEN.md` (neues Kapitel).

**Out-of-Scope (bewusst, per Backlog-Entscheidung)**

- **Kein Mandant-Filter** (D6) — die Sage-View hat keine `[Mandant]`-Spalte, es gibt keinen
  Multi-Tenancy-Filter im Code oder in `appsettings.json`.
- **Kein d3.js** oder sonstige neue Frontend-Abhängigkeit für den Tree — Vanilla-JS-Toggle wie
  bei OSEON.
	
- **Keine Änderung an `BomItem.Position`** (bleibt `string?`, D4).
- **Kein Hauptlagerplatz-Import aus der Stückliste** (`Hauptlagerplatz`-Spalte der BOM-View wird
  **nicht** übernommen, D8 — das existierende `Article.PrimaryStorageLocationId`-Feature aus
  v1.25.0 bleibt unberührt und läuft über den separaten Artikel-Sync).
- **Kein Rückzug von `DeliveryDate`** aus dem Schema — die Spalte bleibt, wird vom IDEAL-Sync nur
  nicht mehr beschrieben (D9); die AKE-Linie schreibt sie weiterhin.
- **Keine neue Rolle** — die FA-Liste bleibt unter dem bestehenden
  `RequirePickingOrTrackingOrLeitstandAccess`-Filter (`IdealAkeWms/Controllers/ProductionOrdersController.cs:12`).
- **Keine Änderung an Mandanten-/Multi-Tenant-Architektur** — beide Standorte bleiben getrennte
  Deployments mit getrennter DB.
- **Keine automatische Migration von Bestandsdaten der AKE-Linie** — Migration betrifft nur das
  Schema; die AKE-DB bekommt einen zusätzlichen, aber für sie irrelevanten Satz Spalten/Indizes.

## Ist-Zustand (Code-Referenzen, main HEAD `4db19ef`)

Der alte Branch war auf ~v1.12.0-Basis; main ist seither bis v1.26.0 weitergelaufen. Wichtigste
Abweichungen zwischen dem, was der Branch annahm, und dem heutigen main:

**1. `OrderNumber` ist heute eindeutig (Modell UND DB), ohne `SubOrderNumber`.**
`IdealAkeWms/Models/ProductionOrder.cs:10` — `OrderNumber` ist die einzige FA-Referenz, es gibt
kein `SubOrderNumber`- oder `DocumentNumber`-Feld. `ApplicationDbContext.cs:385` —
`entity.HasIndex(e => e.OrderNumber).IsUnique();`. `ApplicationDbContext.cs:373` — MaxLength(100).

**2. Der Sage-Import läuft heute an ZWEI Stellen redundant, beide auf `vw_AKE_Kommissionierung_WAListe`:**
- `IDEALAKEWMSService/Services/SageImportService.cs:33-367`
  (`SyncProductionOrdersAsync`, gated `Sync:ProductionOrdersEnabled`) — läuft im Windows-Service
  und ist der tatsächlich aktive Pfad.
- `SQL/AgentJobs/01_Import_Produktionsauftraege.sql` — historisch als SQL-Agent-Job dokumentiert
  (laut `secondbrain/codebase/integrationen.md`), inhaltlich dasselbe MERGE. Ob dieser Job an
  einem der beiden Standorte tatsächlich noch scharf geschaltet ist, ist ungeklärt (siehe offene
  Rückfrage).

**3. Es existiert bereits ein produktiver Kompatibilitäts-Notbehelf für ein IDEAL-Schema mit
`SubOrderNumber`:** `SageImportService.cs:106-117` prüft per `COL_LENGTH('dbo.ProductionOrders','SubOrderNumber')`,
ob die Zielspalte existiert, und lässt `SageProductionOrderSql.BuildUpsert(hasSubOrderNumber)`
(`IDEALAKEWMSService/Services/SageProductionOrderSql.cs:11-56`) im INSERT-Zweig
`SubOrderNumber = @OrderNumber` mitschreiben, **falls** die Spalte existiert — ohne dass das
EF-Modell diese Spalte kennt oder pflegt. Dokumentiert und mit Testszenario versehen in
`docs/TESTSZENARIEN.md` Kapitel 47 (TS-47.1, „ProductionOrders-515-Fix"). **Das bedeutet: mindestens
eine reale DB hat schon jetzt eine `SubOrderNumber`-Spalte (vermutlich `NOT NULL` + `UNIQUE`, aus
der Zeit vor der Löschung des alten Branches), gegen die der aktuelle main-Code nur defensiv
"nicht abstürzt" — nicht funktional korrekt befüllt.** Siehe Risiken.

**4. FA-Zusatzinfos (v1.26.0) sind bereits auf Mehrfachtreffer je WA-Nummer ausgelegt** —
`IDEALAKEWMSService/Services/FaZusatzinfoSyncService.cs:83-97` gruppiert `ProductionOrders` nach
`OrderNumber` (`ordersByNumber`, `GroupBy(o => o.OrderNumber, ...)`) und iteriert in
`FaZusatzinfoSyncService.cs:116-173` über **alle** Treffer einer WA-Nummer. Der 1:1-Satellit
`ProductionOrderExtraInfo` (`IdealAkeWms/Models/ProductionOrderExtraInfo.cs:13-37`) ist 1:1 zu
`ProductionOrder.Id` (nicht zu `OrderNumber`), es entsteht also pro betroffener `ProductionOrder`-Zeile
eine eigene `ExtraInfo`-Zeile. Der doc-Kommentar in `FaZusatzinfoSyncService.cs:14-15` sagt das
explizit: *„Mehrfach-treffer-faehig (IDEAL-Linie: mehrere ProductionOrders je WA-Nummer → Upsert
je Id)"*. **Diese Schreib-Seite ist strukturell bereits kompatibel** — die offene Frage betrifft
die fachliche Semantik (siehe Kollisionsanalyse).

**5. FA-Reconciliation (v1.25.0) arbeitet ausschließlich auf `OrderNumber`, mit `UPDATE ... WHERE
OrderNumber = @OrderNumber`:** `SageImportService.cs:223` (`sageOrderNumbers`),
`SageImportService.cs:228` (WMS-State-Read `SELECT [OrderNumber], [IsDone], [IsCancelled] ...`),
`SageImportService.cs:279-292` (Reaktivierung) und `SageImportService.cs:313-328` (Storno) — **beide
UPDATE-Statements matchen per `OrderNumber`, nicht per Zeilen-Id oder `SubOrderNumber`.** Das ist
in der heutigen 1:1-Welt korrekt (ein `OrderNumber` = eine Zeile), wird unter der Inversion aber
zur Kollisionsgefahr — siehe Kollisionsanalyse, Punkt 1.

**6. Eindeutige und Gruppen-Lookups auf `OrderNumber`, die die Regel „eindeutig → SubOrderNumber"
betreffen:**
- `IdealAkeWms/Data/Repositories/WorkOperationRepository.cs:78-84`
  (`GetByFaAndOperationAsync(faNumber, operationNumber)`) — **eindeutiger** QR-/BDE-Scan-Lookup,
  aufgerufen aus `IdealAkeWms/Controllers/BdeApiController.cs:53`. Muss unter der Inversion
  `SubOrderNumber` matchen (QR trägt an Index 2 die BelID = künftig `SubOrderNumber`).
- `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs:95-98`
  (`GetByOrderNumberAsync`) — aktuell **ohne Produktions-Aufrufer** (nur Repository-Test
  `IdealAkeWms.Tests/Repositories/ProductionOrderRepositoryTests.cs:45-53`), aber Teil des
  öffentlichen Repository-Interfaces (`IProductionOrderRepository.cs:33`). Bleibt eindeutig,
  sollte künftig `SubOrderNumber` matchen oder umbenannt werden.
- `ProductionOrderRepository.cs:100-116` (`SearchAsync`) — Freitextsuche, muss zusätzlich
  `SubOrderNumber` durchsuchen (Gruppen-artige Suche, mehrere Treffer sind hier bereits vorgesehen).
- `ProductionOrderRepository.cs:155-202` (`ApplyLeitstandColumnFilter`) — Spaltenfilter
  `order-number` filtert nur auf `OrderNumber`; braucht analoges Verhalten für einen neuen
  `sub-order-number`-Filter-Key.
- `IdealAkeWms/Controllers/ProductionOrdersController.cs:56-188` (`Index`) — Liste ist heute
  **flach paginiert** über `GetForLeitstandAsync` (`ProductionOrderRepository.cs:23-88`), **keine**
  Gruppierung. Die geforderte Tree-View (Punkt d) ist eine Neuentwicklung, keine Anpassung eines
  bestehenden Gruppierungsmechanismus.

**7. BOM-Datenquelle** — `IdealAkeWms/Data/Repositories/BomRepository.cs:30-42` (Live-SAGE-Pfad,
`[ake].[dbo].[vw_AKE_Kommissionierung_StuecklistenDB]`, hartcodierter Datenbankname `[ake]`) und
`IDEALAKEWMSService/Services/BomCacheSyncService.cs:273-276` (Cache-Sync-Quelle, dieselbe View,
dasselbe Präfix). Modelle ohne `Artikelmatchcode`: `IdealAkeWms/Models/ViewModels/BomViewModels.cs`
(`BomItem` Z. 5-16, `BomItemViewModel` Z. 26-50, `PrintBomItem` Z. 119-132),
`IdealAkeWms/Models/CachedBomItem.cs:9-41`, Mapping in
`IdealAkeWms/Data/Repositories/BomCacheRepository.cs:33-43` (Cache→BomItem) und
`BomCacheRepository.cs:126-138` (BomItem→CachedBomItem-Insert), Bulk-Copy-ColumnMapping in
`BomCacheSyncService.cs:555-564`.

**8. Bereits existierender Präzedenzfall für einen konfigurierbaren, hartkodiert-benannten
IDEAL-spezifischen Sage-View:** die FA-Zusatzinfos-View heißt `dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen`
(`IDEALAKEWMSService/Services/SageZusatzinfoReader.cs:33,50`, Präfix „IDEAL_AKE_WMS" mit
Unterstrich, **nicht** Bindestrich wie im Backlog-Namensschema `vw_IDEAL-AKE_*`), liegt in der
**Sage-DB** (`SageConnection`), ist aber **nicht konfigurierbar** (Name ist Literal im SQL-String).
Das zeigt: main hat bereits eine IDEAL-spezifische View im Einsatz, aber mit einem anderen
Namensschema und ohne Konfigurierbarkeit — ein Präzedenzfall, der bei der Namenskonvention und der
Frage „appsettings vs. ServiceSettings" mitgedacht werden muss (siehe offene Rückfragen).

**9. Rollenmodell unverändert:** `ProductionOrdersController` unter
`RequirePickingOrTrackingOrLeitstandAccess` (`ProductionOrdersController.cs:12`); BDE-Scan unter
den bestehenden BDE-Rollen; keine der betroffenen Stellen hat heute einen rollenbezogenen Bezug
zu `OrderNumber`/`SubOrderNumber`.

## Fachliche Anforderungen

Übernommen aus den verbindlichen Design-Entscheidungen D1–D10 des Backlogs, an main angepasst:

1. **D1 — FA-Hierarchie invertieren:** `ProductionOrder.OrderNumber` wird die Haupt-FA-Nummer
   (Sage StrukturID) und ist **nicht mehr eindeutig**. Eine neue Spalte `SubOrderNumber`
   (Sage BelID) ist die **eindeutige** Sub-FA-Nummer. Hauptauftrag ⇔ `OrderNumber == SubOrderNumber`.
2. **D2 — Belegnummer:** neue, optionale Spalte `DocumentNumber` (`NVARCHAR(100) NULL`),
   in der FA-Liste default-hidden. 
3. **D3 — Tree-View:** zweistufige, server-gruppierte Baumdarstellung in der FA-Liste, analog zu
   OSEON-Teileverfolgung (`IdealAkeWms/Views/Tracking/Index.cshtml`, Chevron-Toggle,
   `data-bs-toggle` **nicht** verwendet — reines Vanilla-JS wie im Backlog gefordert, kein
   Bootstrap-Collapse).  
4. **D4 — BOM-Position bleibt unverändert** (`string?`, keine Schema-Änderung an `Position`).
5. **D5 — Sage-Views konfigurierbar, einheitlich `vw_IDEAL-AKE_*`:** WA-Liste und Stückliste
   werden über Konfigurationswerte referenziert, nicht mehr hartcodiert.
6. **D6 — Kein Mandant-Filter.**
7. **D7 — Backfill** bestehender Zeilen: `SubOrderNumber = OrderNumber`, wo noch nicht gesetzt.
8. **D8 — BOM:** nur `Artikelmatchcode` übernehmen, `Hauptlagerplatz` explizit **nicht**.
9. **D9 —** Sync schreibt `ProductionDate` weiter, `DeliveryDate` nicht mehr (Spalte bleibt im
   Schema für die AKE-Linie).
10. **D10 — `BomRepository`:** `[ake]`-Datenbank-Präfix entfernen, `FROM [dbo].[{viewName}]` (DB
    kommt aus dem Connection String, nicht aus dem SQL-Text).

## Technischer Lösungsentwurf

### a) FA-Hierarchie-Schema

- `IdealAkeWms/Models/ProductionOrder.cs`: neue Property `SubOrderNumber`
  (`[Required][StringLength(100)]`, Default `""` bis zum Backfill), neue Property
  `DocumentNumber` (`string? [StringLength(100)]`).
- `ApplicationDbContext.cs` (Entity `ProductionOrder`, ab Zeile 369): `SubOrderNumber`
  `IsRequired().HasMaxLength(100)`, `DocumentNumber.HasMaxLength(100)`; bestehenden
  `entity.HasIndex(e => e.OrderNumber).IsUnique();` (Zeile 385) **entfernen**, stattdessen
  `entity.HasIndex(e => e.SubOrderNumber).IsUnique();` **und**
  `entity.HasIndex(e => e.OrderNumber);` (nicht eindeutig, für Group-By in der Tree-View).
- Migrationsreihenfolge in der EF-Migration (ein logischer Change, mehrere Schritte wegen
  NOT-NULL-Backfill): Spalten nullable hinzufügen → Backfill (D7) → `SubOrderNumber` auf
  `NOT NULL` setzen → alten Unique-Index auf `OrderNumber` droppen → neue Indizes anlegen.

### b) Sage-Import

- `SageImportService.SyncProductionOrdersAsync` (`SageImportService.cs:33-367`): Quelle wird
  `vw_IDEAL-AKE_Kommissionierung_WAListe`, View-Name aus Konfiguration
  `Sync:SageWaListeViewName`, gegen eine **Whitelist-Regex** `^[A-Za-z0-9_\-]+$` geprüft — bei
  Verstoß `InvalidOperationException` (verhindert SQL-Injection über einen fehlkonfigurierten
  View-Namen, analog zur bestehenden Praxis strikt parametrisierter Queries).
- Feld-Mapping: `[FA-Nummer]→OrderNumber`, `[subFA-Nummer]→SubOrderNumber`,
  `[Belegnummer]→DocumentNumber`, `[Stückzahl]→Quantity`, `[Kunde]→Customer`,
  `[Artikelnummer]→ArticleNumber`, `[Bezeichnung1/2]→Description1/2`,
  `[Fertigungstermin]→ProductionDate`. `[Liefertermin]` wird **nicht mehr** gelesen (D9).
  `WHERE [subFA-Nummer] IS NOT NULL`, `SELECT DISTINCT`.
- Upsert-Key wird `SubOrderNumber` (statt `OrderNumber`); der bereits vorhandene
  `COL_LENGTH`-Schema-Awareness-Mechanismus (`SageImportService.cs:106-117`,
  `SageProductionOrderSql.cs`) muss überarbeitet werden: heute prüft er nur, **ob** die Spalte
  existiert und befüllt sie im Insert-Zweig mit `OrderNumber` — künftig muss er echte
  `SubOrderNumber`-Werte aus der IDEAL-View schreiben **und** das UPDATE-SET um `OrderNumber` +
  `DocumentNumber` erweitern (Sub-FA-Umhängung: derselbe Beleg kann in Sage einer anderen
  Haupt-FA zugeordnet werden). Change-Detection erweitert um `OrderNumber`/`DocumentNumber`,
  `DeliveryDate` fällt aus der Change-Detection.
- `SQL/AgentJobs/01_Import_Produktionsauftraege.sql`: MERGE `ON p.OrderNumber = src.OrderNumber`
  (Zeile 66) muss auf `ON p.SubOrderNumber = src.SubOrderNumber` umgestellt werden, inkl.
  UPDATE-SET-Erweiterung um `OrderNumber`/`DocumentNumber`. **Vorfrage:** ob dieser Job an einem
  der beiden Standorte überhaupt aktiv ist (siehe offene Rückfragen) — wenn ja, muss er im
  selben Wartungsfenster wie die Migration aktualisiert werden (ADR 0004).
- **Kein Mandant-Filter** in der Query (D6).

### c) SubOrderNumber-Durchzug (eindeutig vs. Gruppe)

Regel: **eindeutige** Lookups (genau ein FA gemeint) gehen über `SubOrderNumber`; **Gruppen**-Lookups
(alle Sub-FAs einer Haupt-FA) bleiben über `OrderNumber`.

- `ProductionOrderListItem` (`IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs:24-54`,
  entspricht dem im Backlog genannten `ProductionOrderViewItem` — der Name im heutigen main ist
  `ProductionOrderListItem`): + `SubOrderNumber`, + `DocumentNumber`,
  `IsMainOrder => string.Equals(OrderNumber, SubOrderNumber, StringComparison.Ordinal)`.
- `LeitstandOrderRow`/`LeitstandOrderPage` (`IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs:5-27`):
  Projektion um `SubOrderNumber`/`DocumentNumber` erweitern (Record-Parameter mit Default, analog
  dem bestehenden Muster der v1.26.0-ExtraInfo-Felder in derselben Datei).
- `ProductionOrdersController.Index` (`ProductionOrdersController.cs:56-188`): Freitextsuche
  (`filterOrderNumber`) zusätzlich gegen `SubOrderNumber` matchen.
- `IProductionOrderRepository`/`ProductionOrderRepository`: neue Methode
  `GetBySubOrderNumberAsync(string subOrderNumber)` (eindeutig, ersetzt/ergänzt
  `GetByOrderNumberAsync` als primären eindeutigen Lookup); `SearchAsync`
  (`ProductionOrderRepository.cs:100-116`) erweitert die `Contains`-Kette um `SubOrderNumber`;
  `GetForLeitstandAsync` (`ProductionOrderRepository.cs:23-88`) bekommt einen neuen Spaltenfilter-Key
  `sub-order-number` in `ApplyLeitstandColumnFilter` (`ProductionOrderRepository.cs:155-202`).
- `WorkOperationRepository.GetByFaAndOperationAsync` (`WorkOperationRepository.cs:78-84`): Match
  von `w.ProductionOrder.OrderNumber == faNumber` auf `w.ProductionOrder.SubOrderNumber == faNumber`
  umstellen — der QR-Code trägt an Index 2 weiterhin die BelID, die künftig `SubOrderNumber` heißt.
  Frontend-Scanner (`wwwroot/js/barcode-scanner.js`) bleibt unverändert, da er nur den rohen
  QR-Wert an den Server weiterreicht.

### d) Tree-View

- `IdealAkeWms/Views/ProductionOrders/Index.cshtml` + neues Partial
  `Views/ProductionOrders/_ProductionOrderRowCells.cshtml` + Ergänzung in
  `wwwroot/css/site.css`: server-seitige Gruppierung nach `OrderNumber`, Pagination auf
  **Gruppen-Ebene** (25 Haupt-FAs/Seite, `Skip`/`Take` auf die Distinct-`OrderNumber`-Menge, nicht
  auf einzelne `ProductionOrder`-Zeilen — das ist eine Abweichung vom heutigen
  `GetForLeitstandAsync`, das zeilenweise paginiert).
- Neuer Projektionstyp `ProductionOrderGroupViewItem { MainOrderNumber, MainOrder?, SubOrders }`.
- Chevron-Toggle-Muster wie in `Views/Tracking/Index.cshtml` (Vanilla-JS, **kein**
  `data-bs-toggle="collapse"`, siehe D3).
- **Phantom-Header**, wenn die Haupt-FA-Zeile selbst nicht im aktuellen Seiten-Slice liegt (z. B.
  Haupt-FA ist bereits erledigt/ausgeblendet, aber eine ihrer Sub-FAs ist offen und passt in die
  Seite).
- **Auto-Expand** bei einem Filtertreffer auf einer Sub-FA-Zeile, plus Expand-All/Collapse-All.
- `ColumnDefinitions.cs` (`IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs:19`, `ProductionOrders`-`ViewConfig`):
  neuer Col-Key `sub-order-number` „Sub-FA" — Pflicht laut Fallstrick „`data-col-key` ist Pflicht
  auf allen `<th>`".
- **Achtung Listen-View-Pattern (ADR 0005):** die Tree-View ist eine begründete Ausnahme vom
  reinen Zeilen-Pagination-Muster, **behält aber** Filterkarte, Server-Spaltenfilter
  (`data-server-column-filter="true"`) und den Pagination-Partial — nur die Zähl-/Slice-Einheit
  wechselt von „Zeile" auf „Hauptauftrag". Das muss im Review explizit gegen ADR 0005 geprüft
  werden (Datumsspalten weiterhin in C# nach Terminberechnung filtern, `TotalCount` = Anzahl
  Hauptaufträge, nicht Zeilen — sonst zeigt die Pagination eine falsche Seitenzahl).

### e) Belegnummer

- `ColumnDefinitions.cs`: neuer Col-Key `document-number` „Belegnr." mit `DefaultHidden: true`
  (Muster aus v1.26.0, siehe `defaultHidden`-Mechanik in `column-preferences.js`) + inline
  `#column-config`-JSON in `Index.cshtml` + `<th>`/`<td>`.

### f) BOM (Artikelmatchcode)

- `IdealAkeWms/Data/Repositories/BomRepository.cs:30-42`: View-Name aus Konfiguration
  `Sync:SageStuecklisteViewName` (dieselbe Whitelist-Regex wie (b)), `[ake]`-Datenbank-Präfix
  entfernen → `FROM [dbo].[{viewName}]` (D10) — die Zieldatenbank ergibt sich aus dem
  `SageConnection`-Connection-String, nicht mehr aus einem hartcodierten `[ake].` im SQL-Text.
- Neue Spalte `Artikelmatchcode` (`string?`, `NVARCHAR(100) NULL`) in der SELECT-Liste zwischen
  `Bezeichnung2` und `Menge` (Reihenfolge nach Backlog-Vorgabe — bei der Migration mit dem
  tatsächlichen View-Schema abgleichen). OSEON-SP-Fallback (`BomRepository.cs:59-90`) liefert
  `Artikelmatchcode = null` (die OSEON-Stored-Procedure kennt das Feld nicht).
- `IdealAkeWms/Models/ViewModels/BomViewModels.cs`: `Artikelmatchcode` in `BomItem` (Z. 5-16),
  `BomItemViewModel` (Z. 26-50), `PrintBomItem` (Z. 119-132) ergänzen.
- `IdealAkeWms/Models/CachedBomItem.cs:9-41`: neue Property `Artikelmatchcode`
  (`[MaxLength(100)]`) — **eigene Migration**, da `CachedBomItem` eine eigenständige Cache-Tabelle
  ist (nicht `AuditableEntity`, siehe `codebase/datenmodell.md`).
- `IdealAkeWms/Data/Repositories/BomCacheRepository.cs:33-43` (Cache→`BomItem`) und
  `BomCacheRepository.cs:126-138` (`BomItem`→`CachedBomItem`): Feld durchreichen.
- `IDEALAKEWMSService/Services/BomCacheSyncService.cs:273-276` (Sage-SELECT),
  `BomCacheSyncService.cs:555-564` (Bulk-Copy-`ColumnMappings`) und die **ContentHash**-Bildung
  (`BomCacheSyncService.cs:~615-625`, `sb.Append(...)`-Kette) müssen `Artikelmatchcode`
  einbeziehen — sonst erkennt der Content-Hash eine reine Artikelmatchcode-Änderung nicht als
  Change und der Cache-Eintrag bleibt veraltet stehen.
- `Hauptlagerplatz` wird **nicht** importiert (D8) — auch wenn die View diese Spalte liefert.

### g) Konfiguration (Deployment, siehe offene Rückfrage zum Mechanismus)

- Laut Backlog-Vorgabe: **appsettings-only**, nicht über die `/ServiceSettings`-Admin-UI.
  `IDEALAKEWMSService/appsettings.json` **und** `IdealAkeWms/appsettings.json` (Web braucht den
  Wert für `BomRepository`, das im Web-Prozess läuft) unter neuem Abschnitt/Keys
  `Sync:SageWaListeViewName` (Default `vw_AKE_Kommissionierung_WAListe`, damit ein
  Upgrade ohne Konfigurationsänderung weiterhin die AKE-Linie bedient) und
  `Sync:SageStuecklisteViewName` (Default `vw_AKE_Kommissionierung_StuecklistenDB`). Gelesen über
  `_configuration["Sync:SageWaListeViewName"]` bzw. `IConfiguration`, **nicht** über
  `ServiceSettings.GetValueSafeAsync` (das wäre DB-first und würde die aktuelle
  `ServiceSettingDefinitions`-Katalogpflicht auslösen — siehe unten, warum das strittig ist).
- `README.md`: neuer Abschnitt „Per-Standort-Konfiguration" mit den beiden Keys.
- `SQL/AgentJobs/`-Hinweistext auf `vw_IDEAL-AKE_*` ergänzen (der AgentJob selbst kennt kein
  `appsettings.json` — der View-Name muss dort direkt im SQL-Skript stehen, mit Kommentarhinweis
  „hier den standortspezifischen View-Namen eintragen").

**Wichtiger Zielkonflikt (siehe offene Rückfrage):** main hat für **jeden** bisherigen
konfigurierbaren `Sync:*`-String-Wert (z. B. `Sync:FeiertagCountryCode`,
`HolidaySyncService.cs:17-24` + `HolidaySyncService.cs:72-74`) durchgängig das
**ServiceSettings-DB-first-Muster** verwendet: `appsettings.json` liefert nur den
IOptions-Fallback-Default, der eigentliche Wert kommt aus der `ServiceSettings`-Tabelle
(`/ServiceSettings`-UI), und jeder gelesene Key muss laut Drift-Guard-Test
(`IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs:65-105`) im Katalog
`ServiceSettingDefinitions.All` stehen. Die Backlog-Vorgabe „appsettings-only" für die beiden
neuen View-Namen würde von diesem etablierten Muster abweichen — mit der nachvollziehbaren
Begründung, dass ein View-Name (wie ein Connection String) reine Infrastruktur ist, die kein
Admin zur Laufzeit verstellen soll. Diese Entscheidung ist **nicht** aus dem Backlog oder dem
Brain eindeutig ableitbar und wird als offene Rückfrage an den Menschen gestellt.

## Migrations-/SQL-Auswirkungen

Verbindlich nach ADR 0004 (fünfstufig), zwei unabhängige Schema-Änderungen, **zwei** neue
Migrationen mit **neuen, aktuellen Timestamps** (nicht die alten `20260515…`-Timestamps aus dem
gelöschten Branch — sonst sortiert EF sie ggf. vor bereits angewandte main-Migrationen ein) und
SQL-Skript-Nummern **ab `SQL/82`** (main ist aktuell bei `SQL/81_AddProductionOrderExtraInfo.sql`;
die im Backlog genannten `SQL/60`/`SQL/61` sind auf main durch `60_ProductionOrderSplit.sql` und
`61_ExtendStorageLocationCodeTo50.sql` belegt).

**Migration 1 — `SQL/82_AddProductionOrderSubOrderNumber.sql` (Name Platzhalter, endgültiger Name
bei Umsetzung per `dotnet ef migrations add`):**

1. Model: `ProductionOrder.cs` um `SubOrderNumber`/`DocumentNumber` erweitern (siehe oben).
2. `dotnet ef migrations add AddProductionOrderSubOrderNumber --project IdealAkeWms`.
3. Idempotentes Skript `SQL/82_AddProductionOrderSubOrderNumber.sql`, Schritte **in dieser
   Reihenfolge, jeweils** mit `IF NOT EXISTS (... sys.columns ...)`/`IF EXISTS (... sys.indexes
   ...)`/`IF EXISTS (... sys.key_constraints ...)`-Guards, DDL in eigenem Batch (`GO`):
   a. `SubOrderNumber NVARCHAR(100) NULL` + `DocumentNumber NVARCHAR(100) NULL` hinzufügen (beide
      zunächst **nullable**, damit der Backfill in Schritt b keine Constraint-Verletzung wirft).
   b. Backfill (D7): `UPDATE [dbo].[ProductionOrders] SET [SubOrderNumber] = [OrderNumber] WHERE
      [SubOrderNumber] IS NULL;` — **idempotent per Konstruktion** (Filter auf `IS NULL`, mehrfach
      ausführbar ohne Wirkung nach dem ersten Lauf).
   c. `ALTER TABLE ... ALTER COLUMN [SubOrderNumber] NVARCHAR(100) NOT NULL` — nur wenn die Spalte
      aktuell nullable ist (`COLUMNPROPERTY(OBJECT_ID(...), 'SubOrderNumber', 'AllowsNull') = 1`).
   d. Alten Unique-Index/Constraint auf `OrderNumber` droppen (Name aus dem generierten EF-Modell
      übernehmen, i. d. R. `IX_ProductionOrders_OrderNumber`), **guarded** über
      `sys.indexes`/`sys.key_constraints`.
   e. Neuen Unique-Index `IX_ProductionOrders_SubOrderNumber` und nicht-eindeutigen Index
      `IX_ProductionOrders_OrderNumber` anlegen, jeweils `IF NOT EXISTS`.
4. `__EFMigrationsHistory`-Insert in separatem Batch.
5. `SQL/00_FreshInstall.sql` an **beiden** Stellen: (a) `ProductionOrders`-Tabellendefinition im
   konsolidierten Schema bekommt `SubOrderNumber NOT NULL UNIQUE` + `DocumentNumber NULL` direkt
   (kein Backfill nötig, Neuinstallation hat keine Bestandsdaten), (b) `MigrationId` im
   History-INSERT-Block.
6. **Daten-Charakter:** additiv, aber mit einer **NOT-NULL-Verschärfung nach Backfill** — bei
   einer produktiven DB mit sehr vielen `ProductionOrders`-Zeilen ist Schritt c/d ein
   Tabellen-Rebuild-Kandidat (abhängig von SQL-Server-Version/Edition); **DB-Backup vor Deploy**
   dokumentieren, analog Migration 65/76 in `codebase/datenmodell.md`.
7. **Kollisionsrisiko mit dem bestehenden Notbehelf** (siehe Ist-Zustand Punkt 3): Falls die
   Ziel-DB bereits eine `SubOrderNumber`-Spalte besitzt (aus der Zeit des alten Branches, ggf.
   ohne EF-Migrationshistorie-Eintrag), **muss** das Skript deren tatsächlichen Zustand
   (Nullability, Unique-Constraint-Name, ggf. bereits vorhandene Daten) vorab per
   `sys.columns`/`sys.indexes` erkennen und **nicht** blind eine zweite, kollidierende Struktur
   erzeugen. Dieser Fall ist ohne Zugriff auf die reale DB nicht abschließend spezifizierbar —
   siehe offene Rückfrage.

**Migration 2 — `SQL/83_AddCachedBomItemArtikelmatchcode.sql` (Name Platzhalter):**

1. Model: `CachedBomItem.cs` um `Artikelmatchcode` (`[MaxLength(100)]`) erweitern.
2. `dotnet ef migrations add AddCachedBomItemArtikelmatchcode --project IdealAkeWms`.
3. Idempotentes Skript, ein Batch: `ALTER TABLE [dbo].[CachedBomItems] ADD [Artikelmatchcode]
   NVARCHAR(100) NULL` mit `IF NOT EXISTS (... sys.columns ...)`-Guard. Rein additiv, kein
   Backfill nötig (Cache wird beim nächsten BOM-Cache-Sync-Lauf ohnehin neu befüllt — Content-Hash
   ändert sich durch das neue Feld in der Hash-Bildung automatisch, siehe Abschnitt f).
4. `__EFMigrationsHistory`-Insert.
5. `SQL/00_FreshInstall.sql` an beiden Stellen (Schema-Objekt + `MigrationId`).
6. **Daten-Charakter:** additiv, kein Backup-Zwang über das ohnehin geltende Standard-Vorgehen
   hinaus.

**`SQL/AgentJobs/`:** `01_Import_Produktionsauftraege.sql` muss im selben Wartungsfenster wie
Migration 1 aktualisiert werden, **falls** der Job produktiv aktiv ist (offene Rückfrage). Die
Reconciliation-Logik in `SageImportService.cs` (kein separates AgentJob-Skript, läuft im Service)
braucht ebenfalls eine Entscheidung vor dem Deploy — siehe Kollisionsanalyse Punkt 1.

**Reihenfolge-Hinweis:** Migration 1 und 2 sind unabhängig voneinander (`ProductionOrders` vs.
`CachedBomItems`) und können in beliebiger Reihenfolge oder in einer gemeinsamen PR umgesetzt
werden; aus Testbarkeitsgründen (kleinere, isolierte Migrationen) wird getrennte Umsetzung
empfohlen.

## Audit-Feld-Auswirkungen

- `ProductionOrder` ist bereits `AuditableEntity` (`ModifiedAt`/`ModifiedBy`/`ModifiedByWindows`
  vorhanden) — jede schreibende Änderung an `SubOrderNumber`/`DocumentNumber` durch den Sage-Sync
  setzt diese Felder wie bisher mit dem Service-Namen (`IDEALAKEWMSService`/`Sage_Schnittstelle`,
  je nach Pfad — siehe Ist-Zustand Punkt 2 zur Redundanz von C#-Sync und AgentJob).
- `CachedBomItem` ist **kein** `AuditableEntity` (reine Cache-Tabelle, siehe
  `codebase/datenmodell.md` Abschnitt „Stueckliste / Cache") — die neue `Artikelmatchcode`-Spalte
  braucht **keine** Audit-Felder.
- Der Backfill-Schritt (D7, Migration 1) ist ein reines SQL-`UPDATE` außerhalb des
  EF-Change-Trackings und schreibt bewusst **keine** `ModifiedAt/ModifiedBy` — es handelt sich um
  eine einmalige Schema-Migration, kein fachlicher Vorgang. Konsistent mit dem Vorgehen bei
  früheren daten-konvertierenden Migrationen (z. B. Migration 76).

## Rollen- und Feature-Toggle-Betrachtung

- **Keine neue Rolle.** Die FA-Liste bleibt unter `RequirePickingOrTrackingOrLeitstandAccess`
  (`ProductionOrdersController.cs:12`); die BDE-Scan-Änderung (`GetByFaAndOperationAsync`) berührt
  keine Rollenprüfung, nur die Lookup-Spalte.
- **Kein neues Feature-Gate/`AppSetting`** — die FA-Hierarchie-Inversion ist eine
  Schema-/Sync-Änderung, kein an-/abschaltbares Modul. Die Tree-View ersetzt die heutige flache
  Darstellung vollständig (kein Toggle „alt/neu"), da beide Standorte auf getrennten DBs laufen
  und die AKE-Linie mit `OrderNumber == SubOrderNumber` für jede Zeile ohnehin wie ein
  Ein-Zeilen-Baum aussieht (Hauptauftrag ohne sichtbare Sub-FAs) — **muss im Review verifiziert
  werden**, dass die Tree-View für die AKE-Linie nicht optisch stört (jede Gruppe hat dort genau
  ein Kind = sich selbst).
- **Zwei neue Deployment-Konfigurationswerte** (`Sync:SageWaListeViewName`,
  `Sync:SageStuecklisteViewName`) — kein Rollenbezug, aber siehe offene Rückfrage zum
  ServiceSettings- vs. appsettings-Mechanismus, der die Drift-Guard-Testliste
  (`ServiceSettingDefinitionsTests.cs:65-105`) betrifft.
- **Listen-View-Pattern-Pflichten (ADR 0005) für die geänderte FA-Liste:** Pagination bleibt
  Pflicht (aber auf Gruppen-Ebene, siehe Abschnitt d), Filterkarte bleibt, **jede** neue Spalte
  (`sub-order-number`, `document-number`) braucht einen Eintrag in `ColumnDefinitions.cs` **und**
  `data-col-key` in der View, Server-Mode-Spaltenfilter bleibt Standard, Datumsspalten weiterhin
  in C# nach Terminberechnung gefiltert. Der Wechsel von Zeilen- auf Gruppen-Pagination ist eine
  **Erweiterung** des Patterns, keine Abkehr davon — muss im Code-Review explizit gegen ADR 0005
  geprüft werden (insbesondere: `TotalCount` in `PaginationState` muss die Anzahl der
  **Hauptaufträge**, nicht der `ProductionOrder`-Zeilen sein, sonst zeigt der Pagination-Partial
  eine falsche Seitenzahl).

## Kollisionsanalyse: FA-Zusatzinfos (v1.26.0) und FA-Reconciliation (v1.25.0)

Das Backlog fordert ausdrücklich eine adversariale Prüfung, weil main seit der alten IDEAL-Basis
zwei Features bekommen hat, die implizit von einem eindeutigen `OrderNumber` ausgehen. Ergebnis
der Prüfung:

**1. FA-Zusatzinfo-Matching (Schreib-Seite) ist bereits mehrfachtreffer-fähig — aber die
Auto-Erledigt-Fold-2-Logik hat eine ungeklärte fachliche Lücke.**
`FaZusatzinfoSyncService.cs:83-97` gruppiert per `OrderNumber` und iteriert
(`FaZusatzinfoSyncService.cs:116-173`) über alle `ProductionOrder`-Zeilen einer WA-Nummer — das
funktioniert strukturell unverändert weiter, weil es bereits auf `OrderNumber` als
**Gruppen**-Schlüssel ausgelegt ist (siehe Ist-Zustand Punkt 4). **Aber:** die
Auto-Erledigt-Regel (`FaZusatzinfoSyncService.cs:165-172`,
`FaZusatzinfoStatus.IstVerpacktOderAbgeholt(row.Status)`) setzt bei einem einzigen Sage-Status
„verpackt/abgeholt" **jede** gematchte `ProductionOrder`-Zeile der WA-Nummer auf
`IsDonePicking = true` — also potenziell **alle Sub-FAs gleichzeitig**, wenn die
FA-Zusatzinfos-View (`dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen`) den Status auf
WA-Nummer-Ebene (nicht Beleg-/Sub-FA-Ebene) führt. Ob das fachlich korrekt ist — „wenn die
Haupt-FA verpackt/abgeholt ist, sind alle ihre Sub-FAs ebenfalls fertig" — oder ob jeder Sub-FA
einen eigenen Status haben kann, der individuell abgeschlossen wird, ist **nicht aus dem Code
oder dem Backlog ableitbar** und muss fachlich geklärt werden, bevor der Sync gegen die
IDEAL-DB mit `Sync:FaZusatzinfoEnabled=true` läuft (der Automatismus ist einweg, siehe
Fallstrick „FA-Zusatzinfos: Auto-Erledigt ist einweg und gecappt").

**2. FA-Reconciliation arbeitet auf `OrderNumber`-Ebene und würde nach der Inversion
Geschwister-Sub-FAs mit-stornieren/mit-reaktivieren.** Wie im Ist-Zustand Punkt 5 dargestellt,
matchen sowohl der Storno- als auch der Reaktivierungs-Zweig
(`SageImportService.cs:279-292`, `SageImportService.cs:313-328`) per
`WHERE [OrderNumber] = @OrderNumber`. Verschwindet unter der Inversion **eine einzelne Sub-FA**
aus der Sage-View (z. B. weil genau dieser Beleg storniert wurde), aber die WA-Nummer als Ganzes
bleibt bestehen (andere Sub-FAs sind weiterhin aktiv), würde der heutige Code **fälschlich alle**
`ProductionOrder`-Zeilen mit dieser `OrderNumber` stornieren — ein Verstoß gegen genau das
Schutzprinzip, das die FA-Reconciliation laut `fallstricke.md` erst eingeführt hat
(„Ein guard-loses ... würde ... alle offenen FAs stornieren"). **Diese Stelle ist im Backlog
nicht erwähnt und stellt das größte unentdeckte Risiko dieser Anpassung dar.** Der technisch
naheliegende Fix wäre, `sageOrderNumbers`/`wmsStates`/die beiden UPDATE-Statements auf
`SubOrderNumber` umzustellen (konsistent mit der Regel „eindeutige Lookups → SubOrderNumber" aus
dem Backlog) — das wird als **vorgeschlagene, aber nicht endgültig freigegebene Lösung**
dokumentiert und muss vor der Umsetzung fachlich bestätigt werden (siehe offene Rückfrage),
weil eine Fehlentscheidung hier zu einem produktiven Massen-Storno führen kann.

**3. `ProductionOrderExtraInfo` selbst braucht keine Schema-Änderung.** Die 1:1-Beziehung
(`ProductionOrderExtraInfo.ProductionOrderId`) hängt an der `Id` der `ProductionOrder`-Zeile, nicht
an `OrderNumber` — bleibt unter der Inversion strukturell korrekt (siehe Ist-Zustand Punkt 4).

**4. `WorkOperationRepository.GetOpenByWorkplaceIdAsync` (BDE-Sperre, Fold 2) ist ebenfalls
Id-basiert** (`wo.ProductionOrder.ExtraInfo`, FK-Navigation über `ProductionOrder.Id`) und daher
**nicht** von der `OrderNumber`-Eindeutigkeit betroffen — keine Änderung nötig.

**5. `EnaioDmsDocumentRepository.GetByOrderNumbersAsync`** (aufgerufen aus
`ProductionOrdersController.cs:163`) arbeitet bereits auf einer Liste von `OrderNumber`s und
liefert eine `Dictionary<string, List<...>>` — das bleibt unter Mehrfachtreffern pro `OrderNumber`
korrekt (dieselben enaio-Dokumente erscheinen dann sinnvollerweise unter jeder Sub-FA-Zeile der
gleichen Haupt-FA), sofern enaio-Dokumente pro Haupt-FA (nicht pro Sub-FA) geführt werden — **nicht
geprüft, da enaio-seitig außerhalb des Codes**, aber strukturell unkritisch.

**6. `IProductionOrderRepository.GetByOrderNumberAsync`** hat keine Produktions-Aufrufer (siehe
Ist-Zustand Punkt 6) — geringes Risiko, aber die Methode sollte nicht stillschweigend
mehrdeutig werden (`FirstOrDefaultAsync` würde nach der Inversion einen von mehreren Sub-FAs
zufällig/nach SQL-interner Reihenfolge liefern). Empfehlung: als eindeutigen Lookup auf
`SubOrderNumber` umstellen oder explizit als „liefert irgendeine Zeile der Gruppe" dokumentieren
und einen neuen `GetBySubOrderNumberAsync` als den eigentlich eindeutigen Zugriffspfad einführen
(wie im Backlog gefordert).

## Risiken und Deploy-Hinweise

1. **Physischer Ist-Zustand einer bereits existierenden `SubOrderNumber`-Spalte unbekannt**
   (siehe Ist-Zustand Punkt 3, TS-47.1). Migration 1 darf **nicht blind** ausgeführt werden, ohne
   vorher `sys.columns`/`sys.indexes`/`sys.key_constraints` der Ziel-DB zu prüfen — sonst droht
   entweder ein Migrationsfehler (Spalte/Index existiert bereits mit abweichender Definition)
   oder ein stiller Datenverlust (falsch angenommene Nullability/Constraint). **DB-Backup vor
   Deploy zwingend.**
2. **FA-Reconciliation-Massen-Storno-Risiko** bei unveränderter `OrderNumber`-Matching-Logik
   (Kollisionsanalyse Punkt 2) — vor scharfschaltung von
   `Sync:ProductionOrderReconcileEnabled=true` an einem IDEAL-Standort **zwingend** klären und
   ggf. fixen, sonst kann ein einzelner verschwundener Sub-FA alle Geschwister-Sub-FAs stornieren.
3. **FA-Zusatzinfos Auto-Erledigt-Granularität** (Kollisionsanalyse Punkt 1) — bei aktivem
   `Sync:FaZusatzinfoEnabled=true` und `autoDoneMaxPerRun` ohne vorherige fachliche Klärung droht
   ein falscher Massen-Abschluss von Sub-FAs, die eigentlich noch offen sind. Erstlauf-Pflicht
   `DryRun` (analog v1.26.0-Erstlauf-Regel) gilt hier umso mehr.
4. **Zwei redundante Sage-Import-Pfade** (C#-Service + AgentJob, Ist-Zustand Punkt 2) — wird nur
   einer der beiden aktualisiert, laufen beide Pfade nach dem Deploy mit unterschiedlicher
   Feldsemantik gegeneinander (einer schreibt noch `OrderNumber`-basiert, der andere
   `SubOrderNumber`-basiert) und der MERGE könnte Duplikate erzeugen oder Daten überschreiben.
   Muss vor Deploy geklärt werden, welcher Pfad an welchem Standort tatsächlich aktiv ist.
5. **Deploy ist Multi-Artefakt** (Model, 2 Migrationen, 2 SQL-Skripte, `00_FreshInstall.sql` an 4
   Stellen, ggf. AgentJob, 2 appsettings.json-Dateien, README) — Reihenfolge im Wartungsfenster:
   DB-Backup → SQL-Skripte einspielen (oder App migrieren lassen) → AgentJob aktualisieren (falls
   aktiv) → Service + Web mit neuer Konfiguration ausrollen → Erstlauf mit `DryRun` für
   FA-Zusatzinfo-Sync (falls aktiv) → Manual-UAT.
6. **AKE-Linie darf nicht regressieren.** Jede Änderung an gemeinsam genutztem Code
   (`ProductionOrderRepository`, `WorkOperationRepository`, `ProductionOrdersController`,
   `BomRepository`) muss gegen die bestehende AKE-Konfiguration (Default-View-Namen,
   `OrderNumber == SubOrderNumber` für jede Zeile) weiter funktionieren — Regressionstests der
   bestehenden `ProductionOrderRepositoryTests`/`WorkOperationRepositoryExtendedTests` müssen grün
   bleiben, plus neue Tests für den IDEAL-Fall.
7. **Freitextsuche/Spaltenfilter-Performance:** `SearchAsync`/`ApplyLeitstandColumnFilter`-Erweiterung
   um `SubOrderNumber` verdoppelt effektiv die `LIKE`-Bedingungen — bei sehr großen `ProductionOrders`-Tabellen
   ggf. Indexnutzung prüfen (der neue `IX_ProductionOrders_SubOrderNumber` deckt exakte Matches,
   nicht `LIKE '%...%'`).

## Akzeptanzkriterien

1. `ProductionOrder.OrderNumber` kann in der DB mehrfach vorkommen (kein Unique-Constraint mehr);
   `ProductionOrder.SubOrderNumber` ist `NOT NULL`, eindeutig (Unique-Index/-Constraint durchsetzt
   auf DB-Ebene), und bei allen Bestandszeilen nach der Migration mit einem nicht-leeren Wert
   befüllt (Backfill greift).
2. Ein Import aus der konfigurierten `vw_IDEAL-AKE_Kommissionierung_WAListe` legt für zwei
   Sage-Zeilen mit identischer `[FA-Nummer]`, aber unterschiedlicher `[subFA-Nummer]`, **zwei**
   getrennte `ProductionOrder`-Zeilen an (kein Duplicate-Key-Fehler, kein gegenseitiges
   Überschreiben).
3. Ändert sich in Sage die `[FA-Nummer]` zu einer bestehenden `[subFA-Nummer]` (Sub-FA-Umhängung),
   aktualisiert der nächste Sync-Lauf `OrderNumber` der bestehenden Zeile, ohne eine neue Zeile
   anzulegen (Upsert-Key bleibt `SubOrderNumber`).
4. Der BDE-Scan (`GetByFaAndOperationAsync`) findet den korrekten `WorkOperation`-Datensatz, wenn
   der QR-Code die `SubOrderNumber` trägt — auch wenn mehrere `ProductionOrder`-Zeilen dieselbe
   `OrderNumber` haben.
5. Die FA-Liste zeigt jede Haupt-FA als aufklappbare Gruppe mit ihren Sub-FAs; ein Hauptauftrag
   ohne weitere Sub-FAs (`OrderNumber == SubOrderNumber`, z. B. jede Zeile in der AKE-Linie) wird
   ohne störendes Leer-Aufklapp-Element dargestellt.
6. Freitextsuche und Spaltenfilter der FA-Liste finden einen FA sowohl über `OrderNumber` als auch
   über `SubOrderNumber`.
7. Die Spalte „Belegnr." (`document-number`) ist standardmäßig ausgeblendet, aber über die
   Spalten-Einstellungen einblendbar und zeigt `DocumentNumber` korrekt an.
8. Die BOM-Anzeige zeigt `Artikelmatchcode` (Sage-Quelle **und** Cache-Quelle liefern denselben
   Wert), aber **nicht** `Hauptlagerplatz`.
9. `BomRepository` fragt die Stückliste ohne hartcodiertes `[ake].`-Datenbank-Präfix ab; ein
   Connection-String auf eine andere Datenbank liefert Daten aus **dieser** Datenbank.
10. Ein falsch konfigurierter View-Name (z. B. mit SQL-Metazeichen) führt zu einer kontrollierten
    `InvalidOperationException` beim Sync-Start, nicht zu einer SQL-Injection-Möglichkeit oder
    einem unkontrollierten Absturz.
11. Die AKE-Linie (Default-Konfiguration, keine Sub-FAs in der Quelle) funktioniert nach dem
    Deploy identisch wie vorher: FA-Liste, Kommissionierung, BDE-Scan, BOM, FA-Zusatzinfos und
    FA-Reconciliation zeigen unverändertes Verhalten (Regressionsschutz).
12. `dotnet build IdealAkeWms.slnx` und beide Testsuiten (`IdealAkeWms.Tests`,
    `IDEALAKEWMSService.Tests`) laufen grün, inklusive neuer Tests für
    `GetBySubOrderNumberAsync`, Mehrfach-Sub-FAs je `OrderNumber`, `IsMainOrder`/Gruppierung und
    Whitelist-Smoke-Tests für beide neuen View-Namen-Konfigurationen.
13. `SQL/00_FreshInstall.sql` erzeugt auf einer leeren DB ein Schema, das exakt dem Endzustand
    nach beiden neuen Migrationen entspricht (Schema-Objekte **und** `__EFMigrationsHistory`).
14. Ein erneutes Einspielen beider neuer `SQL/XX_*.sql`-Skripte gegen eine bereits migrierte DB
    ändert nichts (Idempotenz, `OBJECT_ID`/`sys.columns`/`sys.indexes`-Guards greifen).

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (Kapitel 56, Arbeitstitel „IDEAL-Standort:
FA-Hierarchie-Inversion (Sub-FA), Belegnummer, konfigurierbare Sage-Views, BOM-Artikelmatchcode").
Nachzuziehen in `secondbrain/tests/testszenarien-index.md`. Skizze der Szenarien (Details in der
Umsetzungsphase je nach QA-Feedback zu verfeinern):

**TS-56.1 Sub-FA-Import legt getrennte Zeilen an**
Vorbedingung: Testinstanz gegen eine Sage-Quelle (oder Testview) mit zwei Zeilen, gleiche
`[FA-Nummer]`, unterschiedliche `[subFA-Nummer]`. Schritte: Sync auslösen. Erwartet: zwei
`ProductionOrder`-Zeilen, `OrderNumber` identisch, `SubOrderNumber` unterschiedlich, beide mit
korrektem `DocumentNumber`. Negativ: eine dritte Zeile mit `[subFA-Nummer] IS NULL` wird laut
`WHERE`-Klausel **nicht** importiert.

**TS-56.2 Sub-FA-Umhängung**
Vorbedingung: bestehende Zeile mit `SubOrderNumber = "BEL-1"`, `OrderNumber = "FA-100"`. Schritte:
Quelle liefert für `subFA-Nummer = "BEL-1"` neu `FA-Nummer = "FA-200"`, Sync auslösen. Erwartet:
dieselbe Zeile (per Id) hat jetzt `OrderNumber = "FA-200"`, keine neue Zeile wurde angelegt.

**TS-56.3 BDE-Scan auf Sub-FA-Ebene**
Vorbedingung: zwei `ProductionOrder`-Zeilen mit gleicher `OrderNumber`, unterschiedlicher
`SubOrderNumber`, je eine `WorkOperation`. Schritte: QR-Scan mit der `SubOrderNumber` der zweiten
Zeile am BDE-Terminal. Erwartet: die BDE-Buchung startet auf der **richtigen** `WorkOperation`
(zweite Zeile), nicht auf der ersten. Negativ: Scan mit einer unbekannten `SubOrderNumber` liefert
die bestehende „FA nicht gefunden"-Fehlermeldung.

**TS-56.4 FA-Liste: Tree-View Auf-/Zuklappen + Phantom-Header**
Vorbedingung: eine Haupt-FA mit 3 Sub-FAs, davon eine erledigt (ausgeblendet bei
`showDone=false`). Schritte: FA-Liste öffnen, Gruppe aufklappen, Filter auf eine bestimmte Sub-FA
setzen. Erwartet: Gruppe klappt automatisch auf (Auto-Expand), nicht-passende Sub-FAs bleiben
ausgeblendet; ist die Haupt-FA-Zeile selbst nicht auf der aktuellen Seite, erscheint ein
Phantom-Header mit der `OrderNumber`, aber ohne Haupt-FA-Detaildaten.

**TS-56.5 Belegnummer-Spalte default-hidden**
Schritte: FA-Liste ohne gespeicherte Spalten-Präferenzen öffnen. Erwartet: Spalte „Belegnr."
ist nicht sichtbar. Über Spalten-Einstellungen einblenden → `DocumentNumber` wird korrekt
angezeigt, bleibt nach Neuladen der Seite eingeblendet (persistiert je Benutzer).

**TS-56.6 BOM-Artikelmatchcode (Sage-Live + Cache)**
Vorbedingung: `Sync:SageStuecklisteViewName` zeigt auf eine Testview mit
`Artikelmatchcode`-Spalte. Schritte: a) BOM live (kein Cache-Treffer) öffnen — Artikelmatchcode
korrekt angezeigt. b) BOM-Cache-Sync laufen lassen, danach BOM erneut öffnen (Cache-Treffer) —
identischer Artikelmatchcode-Wert. Negativ: `Hauptlagerplatz`-Spalte der Quellview wird an keiner
Stelle der BOM-Anzeige übernommen.

**TS-56.7 Konfigurierbare View-Namen — Whitelist-Guard**
Schritte: `Sync:SageWaListeViewName` auf einen Wert mit SQL-Metazeichen setzen (z. B.
`"foo; DROP TABLE X --"`). Erwartet: Sync bricht kontrolliert mit
`InvalidOperationException`/Protokoll-Fehlereintrag ab, **kein** SQL wird gegen die Sage-DB
ausgeführt. Gegenprobe: gültiger alternativer View-Name funktioniert.

**TS-56.8 AKE-Regression**
Vorbedingung: Standard-Konfiguration (Default-View-Namen), Sage-Quelle ohne Sub-FA-Konzept
(`[subFA-Nummer]` immer = `[FA-Nummer]`, oder Test gegen die bestehende AKE-View). Schritte: FA-Liste,
Kommissionierung, BDE-Scan, BOM, FA-Zusatzinfo-Sync und FA-Reconciliation vollständig durchspielen.
Erwartet: identisches Verhalten wie vor dieser Änderung — jede FA erscheint als eigene
„Ein-Zeilen-Gruppe", keine Doppel-Anzeige, keine neuen Fehlermeldungen.

**TS-56.9 FA-Reconciliation unter Sub-FA-Granularität** *(abhängig von der offenen
Rückfrage zur Match-Ebene — Szenario final erst nach fachlicher Klärung ausformulierbar)*
Vorbedingung: Haupt-FA mit zwei Sub-FAs, `Sync:ProductionOrderReconcileEnabled=true`. Schritte:
eine der beiden Sub-FAs verschwindet aus der Sage-Quelle (Sub-FA storniert), die andere bleibt
aktiv. Erwartet (**vorbehaltlich Klärung**): nur die verschwundene Sub-FA wird storniert, die
verbleibende bleibt unangetastet offen. Negativ (heutiges Verhalten ohne Fix): **beide** Zeilen
würden storniert — genau dieser Fall ist im QA-Review explizit gegenzuprüfen.

**TS-56.10 FA-Zusatzinfo Auto-Erledigt unter Sub-FA-Granularität** *(ebenfalls abhängig von
Klärung)* — DryRun-Pflicht vor Erstlauf, Recovery-SQL analog Kapitel 55 dokumentieren.

## Offene Rückfragen

1. **BLOCKIEREND — Match-Ebene der Sage-Views für FA-Zusatzinfos und Reconciliation:** Führt die
   View `dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen` (`WaNummer`) den Status auf Haupt-FA-Ebene
   oder auf Sub-FA-Ebene? Analog: soll die FA-Reconciliation nach der Inversion auf `OrderNumber`
   (Gruppen-Storno) oder auf `SubOrderNumber` (Einzel-Storno) matchen? Ohne diese Klärung ist ein
   produktiver Automatismus (Auto-Erledigt, Reconciliation) an einem IDEAL-Standort ein
   Massen-Fehlbuchungs-Risiko. **Muss vor Umsetzungsbeginn der betroffenen Teile geklärt werden.**  => Auto erledigt können wir hier vorerst ausnehmen. 
2. **Physischer Ist-Zustand der/den Ziel-DB(s):** Existiert bereits eine `SubOrderNumber`-Spalte
   (Notbehelf laut `docs/TESTSZENARIEN.md` Kapitel 47 legt das nahe)? Falls ja: exakte
   Definition (Typ, Nullability, Constraint-Namen, ob bereits befüllt) — die Migration muss
   dagegen idempotent und verlustfrei sein. Wer prüft das vor Umsetzungsbeginn (DBA-Zugriff
   nötig)? => auf diesem Standort ist derzeit noch kein System eingeführt. somit starten wir mit einer leeren Datenbank
3. **`Sync:SageWaListeViewName`/`Sync:SageStuecklisteViewName`: appsettings-only (wie im
   Backlog gefordert) oder `ServiceSettingDefinitions`-Katalog (wie alle bisherigen `Sync:*`-Werte,
   inkl. der Drift-Guard-Testliste)?** Die beiden Muster widersprechen sich; das Backlog
   entscheidet sich explizit gegen ServiceSettings, ohne das gegen den bestehenden Präzedenzfall
   (`Sync:FeiertagCountryCode` u. a.) zu begründen. => damals war es in den Appsettings - besser ist Datenbank.
4. **Ist `SQL/AgentJobs/01_Import_Produktionsauftraege.sql` an einem der beiden Standorte
   tatsächlich produktiv scharf geschaltet**, oder läuft ausschließlich der C#-Service-Pfad
   (`SageImportService.SyncProductionOrdersAsync`)? Falls der AgentJob aktiv ist, muss er im
   selben Wartungsfenster aktualisiert werden — falls nicht, kann er ggf. als veraltete
   Dokumentation gekennzeichnet statt gepflegt werden.
   => die SQL Agents gibt es nicht mehr. nur unser Service.
5. **Zielinstallation dieser Spec:** Betrifft die Umsetzung ausschließlich eine neue,
   IDEAL-spezifische Deployment-Konfiguration (mit den AKE-Defaults als Fallback im selben
   Codebestand, wie hier angenommen), oder ist ein separater Codezweig/Fork für den zweiten
   Standort vorgesehen? Diese Spec geht von „ein Codebestand, zwei Konfigurationen" aus (wie im
   Backlog formuliert), das sollte vor Plan-Erstellung noch einmal bestätigt werden.
   => mir wäre ein Codebestand lieber. Dadurch die Standorte doch etwas anders arbeiten, müssen wir einen mechanismus der konfigurationsmöglichkeit offen halten. wie zb. der Toggle für FA Hierarchisch oder Flach
6. **Reihenfolge/Timing:** Soll diese Umsetzung vor oder nach dem noch ausstehenden
   Produktiv-Deploy von v1.25.0/v1.26.0 (siehe `secondbrain/feature-map.md`, Abschnitt „Offen /
   nicht gemerged") erfolgen? Eine gleichzeitige Migration mehrerer ausstehender,
  daten-relevanter Deploys erhöht das Risiko.
  => nachbei, 
7. **Benennung der beiden neuen EF-Migrationen und SQL-Skript-Namen** sind in dieser Spec als
   Platzhalter (`AddProductionOrderSubOrderNumber`, `AddCachedBomItemArtikelmatchcode`) benannt —
   endgültige Namen erst bei tatsächlicher `dotnet ef migrations add`-Ausführung, da EF den Namen
   nicht vorab reserviert und Zwischenzeit-Merges die nächste freie Nummer verschieben können. => ist in Ordnung
8. **`GetByOrderNumberAsync` auf `IProductionOrderRepository`:** beibehalten (mit dokumentierter
   „liefert eine beliebige Zeile der Gruppe"-Semantik) und zusätzlich `GetBySubOrderNumberAsync`
   einführen, oder `GetByOrderNumberAsync` ersatzlos durch `GetBySubOrderNumberAsync` ersetzen
   (Breaking Change am Interface, aber aktuell ohnehin ohne Produktions-Aufrufer)? zusätzlich einführen ist besser oder?
