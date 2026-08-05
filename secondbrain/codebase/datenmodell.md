---
type: codebase-karte
updated: 2026-07-27
---
# Datenmodell

Entitaeten liegen in `../../IdealAkeWms/Models/`, der Kontext in
`../../IdealAkeWms/Data/ApplicationDbContext.cs`.
Geschwister: [[module]] · [[controller]] · [[services]] · [[integrationen]]

## Basis

`AuditableEntity.cs` ist die Basis fachlicher Entitaeten: `Id`, `CreatedAt`, `CreatedBy`,
`CreatedByWindows`, `ModifiedAt?`, `ModifiedBy?`, `ModifiedByWindows?`.
Begruendung und Regeln: [[0003-auditableentity-als-entity-basis]].
**Bewusste Ausnahme:** `AppSetting` (nur `Key`/`Value`/`Description`).

## Fertigungsauftrag und seine Satelliten

Der FA ist der Dreh- und Angelpunkt. `ProductionOrders` enthaelt **nur Sage-Master-Daten**; jeder
App-Zustand liegt in einem Satelliten — Begruendung:
[[0009-app-status-in-satelliten-tabellen-neben-sage-master]].

| Entitaet | Kardinalitaet | Inhalt |
|---|---|---|
| `ProductionOrder` | Master | Sage-Daten; `IsDone` (Sage), `IsCancelled` + `CancelledAt`/`CancelledBy` (Reconciliation, Migration 80) |
| `ProductionOrderPickingStatus` | 1:1 | HasGlass, HasExternalPurchase, HasCoatingParts, IsCoatingDone, IsReleasedForPicking, PickingPriority, AssignedPicker, **IsDonePicking** |
| `ProductionOrderBdeStatus` | 1:1 | IsDoneBde |
| `ProductionOrderExtraInfo` | 1:1 | read-only Sage-Zusatzinfos: Kaeltemittel, Ventil, AusfuehrungEZ, Maschine, SageStatus (Migration 81) |
| `FaWorkStep` (+ `FaWorkStepSpec`) | 1:N | tatsaechlich benoetigte Vorbau-AGs; `IsRemoved`, `Source` (`Sync`/`Manual`), `IsSpecComplete`, `Status` |
| `FaAttributeValue` | 1:N | strukturierte Merkmalswerte je FA (UNIQUE je FA+Definition) |

**Merkregeln** (Details in [[fallstricke]]): „FA erledigt" = `IsDone || IsDonePicking`; jede
Offen-Query fuehrt zusaetzlich `!IsCancelled`; `IsSpecComplete` (Planer) und `Status` (Werker)
sind zwei verschiedene Flags, nur `Status == Fertig` blendet aus.

## Vorbau-Katalog

| Entitaet | Zweck |
|---|---|
| `WorkStep` | Katalog der FA-Vorbau-Arbeitsgaenge (UI-Label „FA-Vorbau-AG", Code bleibt `WorkStep`); Suchbegriffe treiben die Auto-Erkennung. Code `ALLGEMEIN` ist reserviert. |
| `FaWorkStepStatus` | Enum Offen=0 / InBearbeitung=1 / Fertig=2 (Migration 76, **daten-konvertierend**) |
| `FaAttributeDefinition`, `FaAttributeOption`, `FaAttributeWorkStep` | FA-Merkmale und ihre AG-Zuordnung |
| `ProductionWorkplaceWorkStep` | Werkbank↔AG-Mapping — Stammdatum, treibt die Abarbeitungsliste **nicht** |

## Lager

| Entitaet | Zweck |
|---|---|
| `Article` | Artikelstamm; `PrimaryStorageLocationId` (FK) + `SagePrimaryStorageLocation` (Rohcode = Herkunftsnachweis + Lock, Migration 79) |
| `ArticleCategory`, `ArticleAttributeDefinition`, `ArticleAttributeOption`, `ArticleAttributeValue` | Kategorien und Merkmale (Enum `AttributeType` ist mit FA-Merkmalen geteilt — `Text` gilt nur dort) |
| `StorageLocation` | Lagerplatz; `IsActive` (Sage-controlled) vs `IstBuchbar` (user-controlled), `IsPickingTransport` (Kommissionierwagen), `Code` NVARCHAR(50) |
| `StorageLocationSource` | Enum Manual / Sage |
| `StockMovement` | Bewegungszeile (die echte Historie) |
| `MovementType` | Enum — **jede Erweiterung trifft 6 Aggregations-Stellen** |
| `ArticleGroupRecipientMapping` | Artikelgruppe → Empfaengergruppe |

## Bestellwesen

| Entitaet | Zweck |
|---|---|
| `WarehouseRequisition` (+ `WarehouseRequisitionItem`) | Lager-/Glasbestellung |
| `WarehouseRequisitionType` | Enum Lager=1 / Glas=2 (`HasDefaultValue(Lager)`, Migration 77) — steht nur bei der Anlage fest |
| `WarehouseRequisitionStatus` | Draft / Submitted / **PartiallyDelivered** (kein End-Status) / Closed |
| `ShortageStatus` | Enum None=0 / WillBeRestocked=1 / NoRestock=2 (Migration 65, **daten-destruktiv**) |
| `PartRequisition` (+ `PartRequisitionStatus`, `PartRequisitionPriority`) | Bedarfsmeldungen aus der Stueckliste |
| `OrderRecipient`, `OrderRecipientGroup` | Empfaenger und Gruppen |

`WarehouseRequisitionItem.Note` heisst im UI „Notiz Lager"; `NoteEinkauf` heisst in Code **und**
UI so — siehe [[fallstricke]].

## BDE

| Entitaet | Zweck |
|---|---|
| `BdeBooking` (+ `BdeBookingQuantity`) | Buchung; `ParentBookingId` verkettet Fortsetzungen |
| `BdeBookingStatus`, `BdeBookingType` | Status und Art der Buchung (`Paused` hat `EndedAt` gesetzt) |
| `BdeOperator` | Mitarbeiter am Terminal |
| `BdeActivity` | Nicht-Produktions-Taetigkeiten |
| `BdeTerminal` | Terminal-Konfiguration |
| `BdeShift` | Schichten des Schichtkalenders |
| `WorkOperation` | Arbeitsgang (BDE-/OSEON-Kontext — **nicht** `WorkStep`) |

## Stueckliste / Cache

`CachedBomHeader` + `CachedBomItem` — persistenter BOM-Cache, befuellt vom Service in einem
Fenster mit Cap. Grundlage fuer Artikelinfo, Lackierteil- und AG-Erkennung. Siehe
[[0007-bom-quelle-sage-view-mit-oseon-fallback]]. `PickingItem` haelt den Kommissionier-Fortschritt.

## OSEON / enaio

`OseonProductionOrder`, `OseonWorkOperation`, `OseonOperationConfig` (Offset-Tage + Relevanz je AG),
`EnaioDmsDocument` (`DocumentType`: Werkstattauftrag / Werkstattauftrag+Zeichnung / Zeichnung).
Details: [[integrationen]].

## Benutzer, Rollen, Konfiguration

| Entitaet | Zweck |
|---|---|
| `User` | App-Benutzer. AD-User = `WindowsUserName` gesetzt + `PasswordHash` NULL; lokaler User = umgekehrt (Migration 73). Prefs: `DefaultPageSize`, `DefaultWorkStepId` (Migration 70), `DefaultWorkbenches` (komma-separiert, Migration 75 — ersetzte den FK aus Migration 71, **destruktiv**) |
| `Role`, `RoleKeys`, `UserRole` | Rollen als statische Keys + Junction. `Role.AdGroup` in v1.23.0 **gedroppt** → [[0006-rollenkonzept-statische-keys-mit-admin-wildcard]] |
| `Workstation`, `WorkstationUser` | Arbeitsplaetze (Drucker) |
| `ProductionWorkplace`, `ProductionWorkplaceUser` | Werkbaenke und ihre Zuordnung |
| `AppSetting`, `AppSettingKeys` | Fachliche Feature-Toggles der Web-App (`/Settings`). **Key-Liste (35): `../README.md` → „AppSettings"**; Konzept → [[0011-feature-toggles-ueber-appsettings]] |
| `ServiceSetting`, `ServiceSettingDefinition`, `ServiceSettingDefinitions` | Service-Konfiguration (Katalog = Single Source of Truth) → [[0008-servicesettings-db-first-mit-typisiertem-katalog]] |
| `UserViewPreference` | Persistierte Spalten-Einstellungen je View-Key |
| `SyncLog`, `SyncLogLevel` | Aktivitaets-Protokoll (UI-Label ≠ Tabellenname) → [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]] |
| `Holiday`, `HolidaySource` | Feiertage |

## Migrationen

`../../IdealAkeWms/Migrations/` + idempotente Skripte `../../SQL/XX_*.sql` (aktuell bis 81) +
konsolidiertes `../../SQL/00_FreshInstall.sql`. Der Workflow ist verbindlich —
[[0004-migrations-und-sql-disziplin]].

**Migrationen mit Deploy-Risiko** (DB-Backup vorher):

| Nr | Name | Risiko |
|---|---|---|
| 65 | `ReplaceIsFinalShortageWithShortageStatus` | daten-destruktiv; `Down()` verliert None/WillBeRestocked |
| 69 | `SplitFaWorkStepCompletion` | verschiebt `IsCompleted` → `IsSpecComplete` |
| 75 | `ReplaceUserDefaultWorkplaceWithWorkbenches` | destruktiv, alte FK-Werte verloren |
| 76 | `ReplaceFaWorkStepIsCompletedWithStatus` | daten-konvertierend, dropt Spalte; `Down()` verliert Detail-Status |
| 80 | `AddProductionOrderCancellation` | additiv, aber schaltet FA-Reconciliation frei |
| 81 | `AddProductionOrderExtraInfo` | additiv; der zugehoerige Sync setzt FAs automatisch auf erledigt (vorher DryRun) |

## Standard-Daten (Neuinstallation)

| Typ | Wert | Ort |
|---|---|---|
| Benutzer | `admin`, Passwort leer | Seeding in `Program.cs` |
| Lagerplatz | `NAN` (Fallback fuer negative Buchungen) | Seeding in `Program.cs` |
| ServiceSettings | alle Katalog-Keys mit Default | Seeding aus `ServiceSettingDefinitions.All` |

## Sage-Lagerbuchungen (v1.28.0)

- **`StorageLocation`** (weiterhin `AuditableEntity`) — drei neue Spalten (Migration 82):
  `SageBuchungErlaubt` (`BIT NOT NULL DEFAULT 0`, user-controlled Opt-in), `SageLagerkennung`
  (`NVARCHAR(50) NULL` = volle Kurzbezeichnung = `Code`, vom Lagerplatz-Sync befuellt),
  `SageLagerplatzId` (`INT NULL` = `KHKLagerplaetze.PlatzID`).
- **`SageBookingQueueItem`** (neu, `AuditableEntity`, Migration 83, Tabelle `SageBookingQueueItems`):
  FK `StockMovementId` → `StockMovements` (Restrict), `Status`-Enum
  (`Offen=0`/`Gesendet=1`/`Bestaetigt=2`/`Fehler=3`, Index auf `Status`), `AttemptCount`,
  `LastAttemptAt`, `LastError` (nvarchar 2000), `SageResponseRaw` (nvarchar max), `SentAt`,
  `ConfirmedAt`. Enqueue durch den Web-Decorator, Statuswechsel durch den `SageBookingWorker`
  (`ModifiedBy` = `system:sage-booking`). Details [[2026-07-29-sage-lagerbuchungen-spec]].
