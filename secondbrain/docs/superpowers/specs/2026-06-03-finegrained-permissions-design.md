# Feingranulare Berechtigungen v1.20.0 — Design

**Status:** Spec / Approved
**Datum:** 2026-06-03
**Branch:** `bugfix/missingparts-include-pd` (piggyback auf v1.19.0-Worktree)
**Vorgaenger-Spec:** [2026-05-29-shortage-status-3state-design.md](2026-05-29-shortage-status-3state-design.md)

---

## 1. Ziel

Drei zusammenhaengende Verbesserungen am Berechtigungssystem:

1. **Read/Edit-Split in Stammdaten**: Neue Rolle `masterdata_read` mit Nur-Lesen-Zugriff auf alle 10 heutigen `[RequireMasterDataAccess]`-Controller (Users, Roles, Workstations, Settings, ProductionWorkplaces, OrderRecipients, ArticleCategories, ArticleAttributes, BdeShiftCalendar, SyncLog). Bestehende `masterdata` bleibt = Vollzugriff (Edit impliziert Read).
2. **Lager-Sicht nur fuer Lager-Mitarbeiter**: `picking`-User verlieren Zugriff auf "Lager: Eingehende Listen" (WarehousePicking) und "Lager: Fehlteile" (MissingPartsLager). Bestand + Bewegungshistorie bleiben fuer picker zugaenglich.
3. **Rollen-Uebersicht im Benutzerstamm**: Neue klickbare Uebersichts-Seite "Welche Rolle darf welche Seiten" — hand-gepflegte Tabelle, im Users-Bereich verlinkt, fuer alle Stammdaten-Leser zugaenglich.

## 2. Kontext

### Aktueller Stand (vor v1.20.0)

- 12 Rollen, definiert in `RoleKeys.cs`
- 19 Filter-Attribute (12 Single-Role + 6 Composite + 1 Modul-Toggle `RequireBdeActive`)
- `Role` ↔ `UserRole` (Junction) ↔ `User` — Many-to-Many, Union-Semantik (jeder Match grant access; mehr Rollen = mehr Zugriff)
- Admin-Wildcard ueberspringt alle Pruefungen
- `[RequireMasterDataAccess]` = admin + masterdata, applied an **10 Controller** (CLAUDE.md-Tabelle ist veraltet — listet nur 4): `UsersController`, `RolesController`, `WorkstationsController`, `SettingsController`, `ProductionWorkplacesController`, `OrderRecipientsController`, `ArticleCategoriesController`, `ArticleAttributesController`, `BdeShiftCalendarController`, `SyncLogController` (Class-Level)
- `[RequireStockAccess]` = admin + stock + stock_keyuser + picking, applied auch an `WarehousePickingController` (Lager-Worklist) und `MissingPartsLagerController` (neu im v1.19.0-Worktree)

### Probleme

- **Stammdaten**: kein Read-Only-Modus. Wer was sehen will, muss auch alles aendern duerfen.
- **picker** sieht Lager-Worklist obwohl er an der Werkbank arbeitet — semantische Vermischung.
- **Rollen-Konzept** ist nur in CLAUDE.md dokumentiert, fuer Admin-User im UI nicht sichtbar. Wer einen neuen Benutzer anlegt, weiss nicht, was die Rollen-Checkboxen genau bedeuten.

## 3. Out of Scope

- Werkbank-/Standort-Scoping pro User (Mandant-Trennung) — separate Spec.
- Weitere `xxx_read`-Rollen fuer andere Module (Stock-Read, Tracking-Read, ...) — Pattern wird etabliert, konkrete Anwendung spaeter bei Bedarf.
- Auto-derived Rollen-Uebersicht via Reflection — hand-gepflegt reicht.
- Granular Action-Level Read/Edit per Method-Attribute (Option C aus Brainstorming wurde verworfen).
- Aufsplittung der `picking`-Rolle in Sub-Aspekte — separate Spec.

## 4. Architektur

### 4.1 Neue Rolle

**`masterdata_read`** — "Stammdaten ansehen"
- Wird per Seed-Migration angelegt: `INSERT INTO Roles (Key, Name) WHERE NOT EXISTS`
- Kein User-Backfill noetig (Filter inkludiert `masterdata` implizit)
- Konstante: `RoleKeys.MasterDataRead = "masterdata_read"`

### 4.2 Neue Filter-Attribute

**`RequireMasterDataReadAccessAttribute`**
```csharp
// Allowed: admin OR masterdata_read OR masterdata
new[] { RoleKeys.Admin, RoleKeys.MasterDataRead, RoleKeys.MasterData }
```

**`RequireLagerProcessingAccessAttribute`**
```csharp
// Allowed: admin OR stock OR stock_keyuser
// Bewusst NICHT picking — picker arbeitet an der Werkbank, nicht im Lager
new[] { RoleKeys.Admin, RoleKeys.Stock, RoleKeys.StockKeyUser }
```

Beide Attribute folgen dem bestehenden Pattern (siehe `RequireMasterDataAccessAttribute` als Vorlage).

### 4.3 Filter-Anwendung (Pattern: Class-Level Read + Action-Level Edit-Overrides)

ASP.NET-Filter kumulieren: wenn Class `[RequireRead]` und Action `[RequireEdit]` hat, muss User BEIDE bestehen. Da Read den Edit-Role implizit erlaubt, ist das fuer Vollzugriffs-User transparent.

**Globales Pattern** fuer alle 10 Stammdaten-Controller:
1. Class-Attribute auf `[RequireMasterDataReadAccess]` umstellen (gewaehrt minimum Read)
2. Jede schreibende Action (POST/PUT/DELETE) und jede GET-Form-Rendering-Action (Create-/Edit-Form) bekommt ein zusaetzliches `[RequireMasterDataAccess]`
3. Reine GET-List/Details/Read-Only-Actions: Class-Filter reicht (keine zusaetzlichen Attribute)
4. Bestehende `[RequireMasterDataAccess]` an Class-Level entfernen

#### UsersController
| Action | Filter |
|---|---|
| **(Class)** | `[RequireMasterDataReadAccess]` |
| GET Index | (Class genug) |
| GET Create | `[RequireMasterDataAccess]` |
| POST Create | `[RequireMasterDataAccess]` |
| GET Edit | `[RequireMasterDataAccess]` |
| POST Edit | `[RequireMasterDataAccess]` |
| POST ResetViewPreferences | `[RequireMasterDataAccess]` |
| **NEU: GET RoleOverview** | (Class genug, read-only) |

#### RolesController
| Action | Filter |
|---|---|
| **(Class)** | `[RequireMasterDataReadAccess]` |
| GET Index | (Class genug) |
| GET Create | `[RequireMasterDataAccess]` |
| POST Create | `[RequireMasterDataAccess]` |
| GET Edit | `[RequireMasterDataAccess]` |
| POST Edit | `[RequireMasterDataAccess]` |
| POST Delete | `[RequireMasterDataAccess]` |

#### WorkstationsController
| Action | Filter |
|---|---|
| **(Class)** | `[RequireMasterDataReadAccess]` |
| GET Index | (Class genug) |
| GET Create | `[RequireMasterDataAccess]` |
| POST Create | `[RequireMasterDataAccess]` |
| GET Edit | `[RequireMasterDataAccess]` |
| POST Edit | `[RequireMasterDataAccess]` |

#### SettingsController
| Action | Filter |
|---|---|
| **(Class)** | `[RequireMasterDataReadAccess]` |
| GET Index | (Class genug) |
| GET OperationConfig | (Class genug, ist auch nur Liste) |
| POST SaveSettings | `[RequireMasterDataAccess]` |
| POST AddHoliday | `[RequireMasterDataAccess]` |
| POST DeleteHoliday | `[RequireMasterDataAccess]` |
| POST SyncHolidays | `[RequireMasterDataAccess]` |
| POST AddOperationConfig | `[RequireMasterDataAccess]` |
| POST UpdateOperationConfig | `[RequireMasterDataAccess]` |
| POST DeleteOperationConfig | `[RequireMasterDataAccess]` |

#### ProductionWorkplacesController (Werkbaenke)
| Action | Filter |
|---|---|
| **(Class)** | `[RequireMasterDataReadAccess]` |
| GET Index | (Class genug) |
| GET Create | `[RequireMasterDataAccess]` |
| POST Create | `[RequireMasterDataAccess]` |
| GET Edit | `[RequireMasterDataAccess]` |
| POST Edit | `[RequireMasterDataAccess]` |

#### OrderRecipientsController (Bestell-Empfaengergruppen)
| Action | Filter |
|---|---|
| **(Class)** | `[RequireMasterDataReadAccess]` |
| GET Index | (Class genug) |
| GET ArticleGroupMappings | (Class genug, ist Listing) |
| GET Create | `[RequireMasterDataAccess]` |
| POST Create | `[RequireMasterDataAccess]` |
| GET Edit | `[RequireMasterDataAccess]` |
| POST Edit | `[RequireMasterDataAccess]` |
| POST Delete | `[RequireMasterDataAccess]` |
| POST AddRecipient | `[RequireMasterDataAccess]` |
| POST UpdateRecipient | `[RequireMasterDataAccess]` |
| POST DeleteRecipient | `[RequireMasterDataAccess]` |
| POST SaveArticleGroupMappings | `[RequireMasterDataAccess]` |

#### ArticleCategoriesController + ArticleAttributesController
Pattern wie oben: Class auf `[RequireMasterDataReadAccess]`, Edit-Actions zusaetzlich `[RequireMasterDataAccess]`. Genaue Action-Liste wird im Plan ausgearbeitet.

#### BdeShiftCalendarController (Schichtkalender)
Pattern wie oben. Spezialfall: `BdeShiftCalendar` ist eine spezielle View — POST/Save/Delete fuer Schichtmuster + Feiertags-Overrides bekommen Edit-Filter, View bleibt Read.

#### SyncLogController (Aktivitaets-Protokoll)
Pattern minimal: nur GET Index (Listing) — keine Edits. **Sonderfall**: Da es nur Read-Actions gibt, ist der Class-Filter `[RequireMasterDataReadAccess]` ausreichend. Keine zusaetzlichen Action-Filter.

#### WarehousePickingController + MissingPartsLagerController
| Aenderung |
|---|
| Class-Level: `[RequireStockAccess]` → **`[RequireLagerProcessingAccess]`** |
| Action-Level: kein Override noetig |

**Effekt**: picker verliert WarehousePicking (Index, Details, Close, SaveProgress, PrintAndClose) und MissingPartsLager komplett. Behaelt Werkbank-Bestellungen (`WarehouseRequisitions`, `PartRequisitions`), `MissingParts` (eigene Werkbaenke), Bestand + Bewegungshistorie.

### 4.4 Rollen-Uebersicht-Seite

**Route**: `/Users/RoleOverview`
**Datei**: `Views/Users/RoleOverview.cshtml`
**Filter**: erbt Class-Level `[RequireMasterDataReadAccess]` aus UsersController — Read-User darf, picker darf nicht (verhindert Berechtigungs-Wissens-Leak)

**Inhalt** (hand-gepflegte Razor-Tabelle):

| Spalte | Inhalt |
|---|---|
| Rolle-Key | z.B. `masterdata` |
| Klartext-Name | z.B. "Stammdaten verwalten" |
| Beschreibung | 1-Satz-Erklaerung |
| Sichtbare Seiten/Aktionen | Bullet-Liste, je 5-10 Eintraege |

**Initiale Rollen-Liste** (13 Zeilen):
- `admin`, `masterdata`, `masterdata_read` (NEU), `picking`, `stock`, `stock_keyuser`, `tracking`, `reporting`, `leitstand`, `fa_completion`, `bde_user`, `bde_shiftlead`, `bde_admin`

**Erklaer-Header** oben (deckt den Hilfe-Detail-Zweck mit ab):
> _"Die Berechtigungen sind als Many-to-Many-Verknuepfung zwischen Benutzern und Rollen abgebildet. Wer mehrere Rollen hat, hat die **Vereinigung** der Rechte (mehr Rollen = mehr Zugriff). Die Rolle `admin` ist ein Wildcard und ueberspringt alle Pruefungen. Rollen koennen optional mit AD-Gruppen verknuepft werden — Login uebernimmt dann die Rollen automatisch."_

**Pflege-Hinweis** oben in der View:
> _"Diese Uebersicht ist hand-gepflegt. Bei Aenderungen an Controller-Filtern bitte `RoleOverview.cshtml` mit-aktualisieren."_

**Link-Stellen** (im Users-Bereich):
- `Users/Index`: Button "Rollen-Uebersicht" im Page-Header
- `Users/Edit`: Link "Was darf welche Rolle?" oberhalb der Rollen-Checkboxen
- `Users/Create`: Link "Was darf welche Rolle?" oberhalb der Rollen-Checkboxen

### 4.5 Button-Verstecken in Read-Only-Mode

Wenn User nur `masterdata_read` hat (NICHT `masterdata`), sollen alle Edit-Buttons in Index/Listing-Views ausgeblendet werden. **Anwendung auf alle 10 Stammdaten-Index-Views** plus die OperationConfig-Sub-View und SettingsController-Form.

Pro View:
- "Neu"-Button im Page-Header
- "Bearbeiten"-Link in Aktion-Spalte pro Zeile
- "Loeschen"-Button in Aktion-Spalte pro Zeile

**Implementation**: Via `@inject ICurrentUserService _user` in der View + neue Helper-Methode (siehe 4.7):

```razor
@inject IdealAkeWms.Services.ICurrentUserService _user
@{
    var canEdit = await _user.HasMasterDataAccessAsync();
}
@if (canEdit)
{
    <a asp-action="Create" class="btn btn-primary">Neu</a>
}
```

**Settings-Form (Settings/Index)**: Inline-Edit-Pattern — Save-Buttons werden bedingt gerendert, ALLE Inputs bekommen `disabled` Attribut wenn `!canEdit`, und oben ein Info-Banner: *"Sie haben Nur-Lesen-Zugriff auf die Stammdaten. Aenderungen koennen nicht gespeichert werden."*

**OperationConfig-Form / Andere Inline-Forms** (BdeShiftCalendar, etc.): gleiche Banner+disabled-Strategie.

**Liste der 10 Index-Views** die angepasst werden:
`Users`, `Roles`, `Workstations`, `Settings`, `ProductionWorkplaces`, `OrderRecipients`, `ArticleCategories`, `ArticleAttributes`, `BdeShiftCalendar`, `SyncLog`. SyncLog hat keine Edit-Buttons → keine Aenderung dort.

### 4.6 Layout-Menue-Anpassung (Stammdaten-Block + Lager-Block)

In `Views/Shared/_Layout.cshtml` Zeilen 154+180 existiert heute der Stammdaten-Block hinter:
```razor
@if (await CurrentUserService.HasMasterDataAccessAsync())
{
    // <hr/> + Eintraege: Benutzer, Rollen, Arbeitsplaetze, Einstellungen, ...
}
```

**Aenderung Stammdaten-Block:**
```razor
@if (await CurrentUserService.HasMasterDataReadAccessAsync())  // NEU: Read reicht
{
    // <hr/> + Eintraege: Benutzer, Rollen, Arbeitsplaetze, Einstellungen, ... (alle 10 Bereiche werden sichtbar)
}
```

Das stellt sicher, dass `masterdata_read`-User die Menue-Eintraege ueberhaupt sehen — Index-Views darunter sind ja lesbar.

**Aenderung Lager-Block** (Zeilen ~118 — heute `canAccessStock`):
```razor
@if (await CurrentUserService.CanProcessLagerAsync())  // NEU: ohne picking
{
    // "Lager: Eingehende Listen" + "Lager: Fehlteile"
}
```

`canAccessStock` bleibt fuer Bestand/Bewegungs-Menue-Eintraege erhalten (picker darf weiter sehen).

### 4.7 Neue Helper-Methoden in CurrentUserService

Konvention (siehe `CanAccessStockAsync`, `CanPickAsync`, `HasMasterDataAccessAsync` etc.): jeder Filter hat eine View-Helper-Method.

**Neu hinzu:**

```csharp
public async Task<bool> HasMasterDataReadAccessAsync()
    => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.MasterDataRead, RoleKeys.MasterData);

public async Task<bool> CanProcessLagerAsync()
    => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.Stock, RoleKeys.StockKeyUser);
```

Bestehende `HasMasterDataAccessAsync()` und `CanAccessStockAsync()` bleiben unveraendert (Edit-Sicht bzw. inkl. picker).

### 4.8 API-Endpoints (Out-of-Scope)

API-Controller (`Controllers/Api/*`) werden in dieser Version NICHT angepasst. Falls in Zukunft API-Endpoints fuer Stammdaten dazukommen, gilt das gleiche Pattern: GET = Read-Filter, POST/PUT/DELETE = Edit-Filter. `UserViewPreferencesApiController` bleibt filterlos (Login-Check reicht).

## 5. Datenbank-Migration

### 5.1 EF-Migration `AddMasterDataReadRole`

`Role`-Entity hat Spalten: `Key`, `Name`, `Description`, `AdGroup`, `IsSystem`, `SortOrder` plus AuditableEntity-Felder.

```csharp
migrationBuilder.Sql(@"
    IF NOT EXISTS (SELECT 1 FROM Roles WHERE [Key] = 'masterdata_read')
    BEGIN
        INSERT INTO Roles ([Key], [Name], [Description], [AdGroup], [IsSystem], [SortOrder],
                           [CreatedAt], [CreatedBy], [CreatedByWindows])
        VALUES ('masterdata_read', 'Stammdaten ansehen',
                'Nur-Lesen-Zugriff auf alle Stammdaten-Sichten (Benutzer, Rollen, Arbeitsplaetze, Einstellungen, Werkbaenke, Empfaenger, Artikelkategorien/-attribute, Schichtkalender, Aktivitaets-Protokoll).',
                NULL, 1, 5,
                SYSDATETIME(), 'system', 'system')
    END
");
```

Felder im Detail:
- `IsSystem = 1`: Verhindert versehentliches Loeschen via Roles/Index
- `SortOrder = 5`: Platziert die Rolle direkt nach `masterdata` (Sort 4) im Roles-Listing
- `AdGroup = NULL`: Admin kann spaeter per Roles/Edit eine AD-Gruppe zuweisen

### 5.2 SQL/67_AddMasterDataReadRole.sql

Identisch zur EF-Migration (idempotenter IF NOT EXISTS-Guard). Wird vom FreshInstall referenziert.

### 5.3 SQL/00_FreshInstall.sql

- INSERT der `masterdata_read`-Rolle im Roles-Seeding-Block ergaenzen
- `__EFMigrationsHistory`-Block: neuer Eintrag fuer `AddMasterDataReadRole`. Die exakte MigrationId wird beim `dotnet ef migrations add AddMasterDataReadRole`-Lauf generiert (Format `YYYYMMDDHHMMSS_AddMasterDataReadRole`); danach 1:1 in FreshInstall.sql kopieren.

## 6. Tests

### 6.1 Filter-Unit-Tests (neue Dateien)

**`RequireMasterDataReadAccessAttributeTests.cs`** (5 Tests):
- `Allows_Admin`
- `Allows_MasterData` (Edit-Rolle inkludiert)
- `Allows_MasterDataRead`
- `Denies_PickingOnly`
- `Denies_EmptyRoles`

**`RequireLagerProcessingAccessAttributeTests.cs`** (5 Tests):
- `Allows_Admin`
- `Allows_Stock`
- `Allows_StockKeyUser`
- `Denies_PickingOnly` (kritischer Test — verifiziert die User-Anforderung)
- `Denies_EmptyRoles`

### 6.2 Bestehende Test-Anpassungen

Pro Controller pruefen ob Test-Setup eine Edit-Rolle liefert. Erwartung: alle Tests benutzen `masterdata` → bleiben gruen, weil `RequireMasterDataReadAccess` `masterdata` akzeptiert.

Betroffene Test-Klassen (alle 10 Stammdaten-Controller + WarehousePicking + MissingPartsLager):
- `UsersControllerTests`, `RolesControllerTests`, `WorkstationsControllerTests`, `SettingsControllerTests`
- `ProductionWorkplacesControllerTests`, `OrderRecipientsControllerTests`, `ArticleCategoriesControllerTests`, `ArticleAttributesControllerTests`, `BdeShiftCalendarControllerTests`, `SyncLogControllerTests`
- `WarehousePickingControllerTests`, `MissingPartsLagerControllerTests` (picker-Tests muessen 403 erwarten)

Falls Tests explizit Read-Only-Szenarien dazubekommen: separates Setup mit nur `masterdata_read`-Rolle.

### 6.5 CurrentUserService-Helper-Tests

`CurrentUserServiceTests` um 2 neue Methoden ergaenzen:
- `HasMasterDataReadAccessAsync_AdminTrue` / `_MasterDataTrue` / `_MasterDataReadTrue` / `_OthersFalse`
- `CanProcessLagerAsync_AdminTrue` / `_StockTrue` / `_StockKeyUserTrue` / `_PickingFalse` / `_OthersFalse`

### 6.3 Smoke-Tests fuer neue Action

**`UsersControllerTests.RoleOverview_RendersView`** — ViewResult, Model = vorhandener oder leerer Wert (View braucht eigentlich kein Model — Static Content)

### 6.4 Manuelle Tests (TESTSZENARIEN.md, neues Kapitel)

1. **Read-Only-User**: Anlegen mit nur `masterdata_read` → Login → Users/Index sichtbar, "Neu"-Button NICHT vorhanden, /Users/Create URL liefert 403
2. **Picker-Sichtsperre**: Anlegen mit nur `picking` → Menue zeigt KEINE "Lager: Eingehende Listen" + KEINE "Lager: Fehlteile". URL-Direktaufruf `/WarehousePicking/Index` liefert 403
3. **Multi-Rollen-User**: Anlegen mit `picking` + `masterdata_read` → Werkbank-Sichten OK, Stammdaten-Read OK, kein Edit-Zugriff
4. **RoleOverview-Klick**: Aus `/Users/Index` → Button "Rollen-Uebersicht" → Tabelle mit 13 Rollen + Seiten gerendert
5. **Migration-Smoke**: Nach Update steht in `Roles`-Tabelle die neue Zeile `masterdata_read`

## 7. Dokumentation

- **`CLAUDE.md`** — **substantielle Korrektur noetig** (heutige Filter-Tabelle ist veraltet):
  - Rolle-Tabelle: neue Zeile `masterdata_read`
  - Filter-Tabelle: neue Zeilen `RequireMasterDataReadAccess` + `RequireLagerProcessingAccess`
  - Filter-Tabelle: `RequireMasterDataAccess`-Zeile **vollstaendige Controller-Liste eintragen** (alle 10, nicht nur 4)
  - Filter-Tabelle: WarehousePicking + MissingPartsLager unter `RequireLagerProcessingAccess` statt `RequireStockAccess`
  - Fallstrick: "Class-Level Read + Action-Level Edit ist das kanonische Stammdaten-Pattern fuer weitere xxx_read-Rollen"
  - Fallstrick: "Rollen-Uebersicht (`/Users/RoleOverview`) ist hand-gepflegt — bei Filter-Aenderungen mit-updaten"
- **`Views/Help/Changelog.cshtml`** v1.20.0-Karte: 3 Bullets
- **`PROJECT_STATUS.md`**: kurzer Fortschritts-Hinweis
- **`docs/TESTSZENARIEN.md`**: neues Kapitel "Feingranulare Berechtigungen"
- **`AppVersion.cs`** (Web + Service): v1.19.0 → v1.20.0
- **Keine separate Hilfe-Detail-Seite** (`Views/Help/Berechtigungen.cshtml`) — bewusste Entscheidung. Die Rollen-Uebersicht im Users-Bereich erfuellt den Doku-Zweck. Hinweis dort: Pflegehinweis-Header in der View selbst, und der Pflegehinweis in CLAUDE.md.

## 8. Abhaengigkeiten

- **v1.19.0 muss zuerst stabil sein**: `MissingPartsLagerController` (im Worktree) ist Voraussetzung fuer Aenderung 2 (Lager-Filter). Da Piggyback auf demselben Branch — kein Issue, aber Reihenfolge der Commits sauber: v1.19.0-Aenderungen zuerst, dann v1.20.0 obendrauf.

## 9. Architecture Decision Record (Kurz)

**Entscheidung**: Zwei Rollen pro Stammdatum-Modul (Read + Edit), Edit impliziert Read im Filter.

**Verworfen**: HTTP-Method-basierte Pruefung (Edge-Cases bei GET-Edit-Forms). Action-Level-Attribute pro Method (zu verbose). Auto-derived RoleOverview via Reflection (Komplexitaet ueber Nutzen).

**Konsequenz**: Spaeter `stock_read`, `tracking_read` etc. lassen sich 1:1 nach demselben Pattern einfuegen. RoleOverview muss bei Aenderungen hand-gepflegt werden — Pflege-Hinweis ist in der View selbst sichtbar.
