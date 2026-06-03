# Feingranulare Berechtigungen v1.20.0 — Design

**Status:** Spec / Approved
**Datum:** 2026-06-03
**Branch:** `bugfix/missingparts-include-pd` (piggyback auf v1.19.0-Worktree)
**Vorgaenger-Spec:** [2026-05-29-shortage-status-3state-design.md](2026-05-29-shortage-status-3state-design.md)

---

## 1. Ziel

Drei zusammenhaengende Verbesserungen am Berechtigungssystem:

1. **Read/Edit-Split in Stammdaten**: Neue Rolle `masterdata_read` mit Nur-Lesen-Zugriff auf Users / Roles / Workstations / Settings. Bestehende `masterdata` bleibt = Vollzugriff (Edit impliziert Read).
2. **Lager-Sicht nur fuer Lager-Mitarbeiter**: `picking`-User verlieren Zugriff auf "Lager: Eingehende Listen" (WarehousePicking) und "Lager: Fehlteile" (MissingPartsLager). Bestand + Bewegungshistorie bleiben fuer picker zugaenglich.
3. **Rollen-Uebersicht im Benutzerstamm**: Neue klickbare Uebersichts-Seite "Welche Rolle darf welche Seiten" — hand-gepflegte Tabelle, im Users-Bereich verlinkt, fuer alle Stammdaten-Leser zugaenglich.

## 2. Kontext

### Aktueller Stand (vor v1.20.0)

- 12 Rollen, definiert in `RoleKeys.cs`
- 19 Filter-Attribute (12 Single-Role + 6 Composite + 1 Modul-Toggle `RequireBdeActive`)
- `Role` ↔ `UserRole` (Junction) ↔ `User` — Many-to-Many, Union-Semantik (jeder Match grant access; mehr Rollen = mehr Zugriff)
- Admin-Wildcard ueberspringt alle Pruefungen
- `[RequireMasterDataAccess]` = admin + masterdata, applied an `UsersController`, `RolesController`, `WorkstationsController`, `SettingsController` (Class-Level)
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

**Pflege-Hinweis** oben in der View:
> _"Diese Uebersicht ist hand-gepflegt. Bei Aenderungen an Controller-Filtern bitte `RoleOverview.cshtml` mit-aktualisieren."_

**Link-Stellen** (im Users-Bereich):
- `Users/Index`: Button "Rollen-Uebersicht" im Page-Header
- `Users/Edit`: Link "Was darf welche Rolle?" oberhalb der Rollen-Checkboxen
- `Users/Create`: Link "Was darf welche Rolle?" oberhalb der Rollen-Checkboxen

### 4.5 Button-Verstecken in Read-Only-Mode

Wenn User nur `masterdata_read` hat (NICHT `masterdata`), sollen alle Edit-Buttons in Index-Views ausgeblendet werden:

- "Neu"-Button im Page-Header (`Users/Index`, `Roles/Index`, `Workstations/Index`)
- "Bearbeiten"-Link in Aktion-Spalte pro Zeile
- "Loeschen"-Button in Aktion-Spalte pro Zeile

**Implementation**: Via `@inject ICurrentUserService _user` in der View + `await _user.HasMasterDataAccessAsync()` Check. Beispiel:

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

**Settings**: Analog — Save-Buttons in der Form werden bedingt gerendert. Aber: Inputs koennten weiter editierbar erscheinen — entweder die `disabled`-Attribute setzen oder einen Banner "Read-Only-Modus" zeigen. Praeferenz: **Banner + disabled inputs**.

### 4.6 Layout-Menue-Anpassung

In `Views/Shared/_Layout.cshtml` existiert heute:
```razor
bool canAccessStock = isAdmin || hasRole(stock) || hasRole(stock_keyuser) || hasRole(picking);
@if (canAccessStock)
{
    // "Lager: Eingehende Listen" + "Lager: Fehlteile"
}
```

**Aenderung**:
```razor
bool canAccessLagerProcessing = isAdmin || hasRole(stock) || hasRole(stock_keyuser);
@if (canAccessLagerProcessing)
{
    // "Lager: Eingehende Listen" + "Lager: Fehlteile"
}
```

`canAccessStock` bleibt fuer Bestand/Bewegungs-Menue-Eintraege erhalten (picker darf weiter sehen).

## 5. Datenbank-Migration

### 5.1 EF-Migration `AddMasterDataReadRole`

```csharp
migrationBuilder.Sql(@"
    IF NOT EXISTS (SELECT 1 FROM Roles WHERE [Key] = 'masterdata_read')
    BEGIN
        INSERT INTO Roles ([Key], [Name], [Description], [CreatedAt], [CreatedBy], [CreatedByWindows])
        VALUES ('masterdata_read', 'Stammdaten ansehen',
                'Nur-Lesen-Zugriff auf Benutzer, Rollen, Arbeitsplaetze und Einstellungen.',
                SYSDATETIME(), 'system', 'system')
    END
");
```

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

Falls Tests explizit Read-Only-Szenarien dazubekommen: separates Setup mit nur `masterdata_read`-Rolle.

### 6.3 Smoke-Tests fuer neue Action

**`UsersControllerTests.RoleOverview_RendersView`** — ViewResult, Model = vorhandener oder leerer Wert (View braucht eigentlich kein Model — Static Content)

### 6.4 Manuelle Tests (TESTSZENARIEN.md, neues Kapitel)

1. **Read-Only-User**: Anlegen mit nur `masterdata_read` → Login → Users/Index sichtbar, "Neu"-Button NICHT vorhanden, /Users/Create URL liefert 403
2. **Picker-Sichtsperre**: Anlegen mit nur `picking` → Menue zeigt KEINE "Lager: Eingehende Listen" + KEINE "Lager: Fehlteile". URL-Direktaufruf `/WarehousePicking/Index` liefert 403
3. **Multi-Rollen-User**: Anlegen mit `picking` + `masterdata_read` → Werkbank-Sichten OK, Stammdaten-Read OK, kein Edit-Zugriff
4. **RoleOverview-Klick**: Aus `/Users/Index` → Button "Rollen-Uebersicht" → Tabelle mit 13 Rollen + Seiten gerendert
5. **Migration-Smoke**: Nach Update steht in `Roles`-Tabelle die neue Zeile `masterdata_read`

## 7. Dokumentation

- **`CLAUDE.md`**:
  - Rolle-Tabelle: neue Zeile `masterdata_read`
  - Filter-Tabelle: neue Zeilen `RequireMasterDataReadAccess` + `RequireLagerProcessingAccess`
  - Filter-Tabelle: WarehousePicking + MissingPartsLager unter `RequireLagerProcessingAccess` statt `RequireStockAccess`
  - Fallstrick: "Class-Level Read + Action-Level Edit ist das kanonische Stammdaten-Pattern fuer weitere xxx_read-Rollen"
- **`Views/Help/Changelog.cshtml`** v1.20.0-Karte: 3 Bullets
- **`PROJECT_STATUS.md`**: kurzer Fortschritts-Hinweis
- **`docs/TESTSZENARIEN.md`**: neues Kapitel "Feingranulare Berechtigungen"
- **`AppVersion.cs`** (Web + Service): v1.19.0 → v1.20.0

## 8. Abhaengigkeiten

- **v1.19.0 muss zuerst stabil sein**: `MissingPartsLagerController` (im Worktree) ist Voraussetzung fuer Aenderung 2 (Lager-Filter). Da Piggyback auf demselben Branch — kein Issue, aber Reihenfolge der Commits sauber: v1.19.0-Aenderungen zuerst, dann v1.20.0 obendrauf.

## 9. Architecture Decision Record (Kurz)

**Entscheidung**: Zwei Rollen pro Stammdatum-Modul (Read + Edit), Edit impliziert Read im Filter.

**Verworfen**: HTTP-Method-basierte Pruefung (Edge-Cases bei GET-Edit-Forms). Action-Level-Attribute pro Method (zu verbose). Auto-derived RoleOverview via Reflection (Komplexitaet ueber Nutzen).

**Konsequenz**: Spaeter `stock_read`, `tracking_read` etc. lassen sich 1:1 nach demselben Pattern einfuegen. RoleOverview muss bei Aenderungen hand-gepflegt werden — Pflege-Hinweis ist in der View selbst sichtbar.
