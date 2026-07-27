# Design: Rollenkonzept — Rollenbasierte Zugriffskontrolle

**Datum**: 2026-03-20
**Status**: Entwurf
**Kontext**: Erweiterung 1 von 3 (Rollenkonzept → Lagerzuweisung → Kontaktverwaltung/Push)

---

## Zusammenfassung

Umstellung des bestehenden feldbasierten Berechtigungssystems (Boolean-Flags auf User-Model) auf ein rollenbasiertes System mit eigener `Role`-Tabelle und `UserRole`-Junction-Tabelle. Rollen können direkt zugewiesen oder über AD-Gruppen automatisch vergeben werden. Architektur ist so angelegt, dass später Permissions pro Rolle über die UI konfigurierbar sind (Phase B).

---

## 1. Datenmodell

### 1.1 Neue Entity: `Role` (erbt von AuditableEntity)

| Feld          | Typ                          | Beschreibung                                                                 |
|---------------|------------------------------|------------------------------------------------------------------------------|
| `Key`         | string, 50, required, unique | Technischer Identifier (stabil, für Code-Referenzen)                         |
| `Name`        | string, 100, required        | Anzeigename (editierbar über UI)                                             |
| `Description` | string, 500, nullable        | Beschreibung der Rolle                                                       |
| `AdGroup`     | string, 200, nullable        | SAMAccountName der AD-Gruppe (nur Gruppenname, kein DN). Mitglieder erhalten die Rolle automatisch. |
| `IsSystem`    | bool, default false          | Systemrolle = nicht löschbar über UI (Seed-Daten setzen explizit `true`)     |
| `SortOrder`   | int                          | Anzeigereihenfolge                                                           |

### 1.2 Neue Entity: `UserRole` (erbt von AuditableEntity)

| Feld     | Typ         | Beschreibung          |
|----------|-------------|-----------------------|
| `UserId` | int, FK → User | Benutzer-Referenz   |
| `RoleId` | int, FK → Role | Rollen-Referenz     |

- Unique Constraint auf `(UserId, RoleId)`
- Navigation Properties: `User`, `Role`

### 1.3 User-Model Änderungen

**Entfernte Felder:**
- `IsAdmin`
- `HasMasterDataAccess`
- `CanPick`
- `CanViewTracking`
- `CanReportOperations`

**Neue Navigation Properties:**
- `UserRoles` → Collection von `UserRole`

**Bleiben unverändert:**
- `NotifyOnReorderLevel` (Benachrichtigungs-Setting, keine Rolle)
- `Email` (Kontaktdaten)

### 1.4 Seed-Daten (7 Standardrollen)

| Key             | Name                  | SortOrder | IsSystem |
|-----------------|-----------------------|-----------|----------|
| `admin`         | Administrator         | 0         | true     |
| `masterdata`    | Stammdaten            | 10        | true     |
| `picking`       | Kommissionierer       | 20        | true     |
| `stock`         | Lager                 | 30        | true     |
| `stock_keyuser` | Lager Keyuser         | 40        | true     |
| `tracking`      | Teileverfolgung       | 50        | true     |
| `reporting`     | Betriebsdaten (BDE)   | 60        | true     |

### 1.5 Statische Referenz: `RoleKeys`

```csharp
public static class RoleKeys
{
    public const string Admin = "admin";
    public const string MasterData = "masterdata";
    public const string Picking = "picking";
    public const string Stock = "stock";
    public const string StockKeyUser = "stock_keyuser";
    public const string Tracking = "tracking";
    public const string Reporting = "reporting";
}
```

---

## 2. ICurrentUserService Umbau

### 2.1 Interface-Änderungen

```csharp
public interface ICurrentUserService
{
    // Bestehend (unverändert):
    string GetWindowsUserName();
    string GetDisplayName();
    int? GetCurrentAppUserId();
    string? GetCurrentAppUserName();
    bool IsLoggedIn();

    // Neu (rollenbasiert):
    Task<bool> HasRoleAsync(string roleKey);
    Task<bool> HasAnyRoleAsync(params string[] roleKeys);

    // Bestehend, intern auf Rollen umgestellt:
    Task<bool> IsAdminAsync();              // → HasRoleAsync("admin")
    Task<bool> HasMasterDataAccessAsync();  // → HasAnyRoleAsync("admin", "masterdata") — AD-Gruppe wird beim Rollen-Laden aufgelöst (s. 2.2)
    Task<bool> CanPickAsync();              // → HasAnyRoleAsync("admin", "picking")
    Task<bool> CanViewTrackingAsync();      // → HasAnyRoleAsync("admin", "tracking")
    Task<bool> CanReportOperationsAsync();  // → HasAnyRoleAsync("admin", "reporting")

    // Neu für Lager:
    Task<bool> CanAccessStockAsync();       // → HasAnyRoleAsync("admin", "stock", "stock_keyuser")
    Task<bool> CanTransferStockAsync();     // → HasAnyRoleAsync("admin", "stock_keyuser")
}
```

### 2.2 Rollen-Caching

1. Beim ersten Aufruf von `HasRoleAsync`: **alle Rollen-Keys des Users** laden (direkte + AD-basierte)
2. Direkte Rollen: `SELECT r.Key FROM Roles r JOIN UserRoles ur ON ... WHERE ur.UserId = @userId`
3. AD-Rollen: `SELECT r.Key, r.AdGroup FROM Roles r WHERE r.AdGroup IS NOT NULL` → für jede: `WindowsPrincipal.IsInRole(r.AdGroup)` prüfen
4. Vereinigung in `HashSet<string>` cachen → alle weiteren Prüfungen im selben Request ohne DB-Zugriff
5. AD-Gruppen-Ergebnis zusätzlich in MemoryCache (konfigurierbar über `AdGroupCacheMinutes`)

### 2.3 AD-Gruppen-Prüfung (generisch)

Bisherige Sonderbehandlung `StammdatenADGruppe` in `HasMasterDataAccessAsync()` wird ersetzt durch generisches AD-Mapping auf `Role.AdGroup`. Funktioniert für alle Rollen gleich.

---

## 3. Filter & Controller-Zuordnung

### 3.1 Bestehende Filter — Umbau

Alle Filter bleiben als `TypeFilterAttribute` mit `IAsyncActionFilter`. Intern delegieren sie an `ICurrentUserService`-Rollen-Methoden. Bei fehlender Berechtigung: Redirect auf `Account/AccessDenied`.

| Filter                             | Prüft Rollen                           | Angewendet auf                                                              |
|------------------------------------|----------------------------------------|-----------------------------------------------------------------------------|
| `[RequireAdminAccess]`             | `admin`                                | (wie bisher)                                                                |
| `[RequireMasterDataAccess]`        | `admin`, `masterdata`                  | UsersController, WorkstationsController, SettingsController, RolesController |
| `[RequirePickingAccess]`           | `admin`, `picking`                     | ProductionOrdersApiController, einzelne Actions                             |
| `[RequireTrackingAccess]`          | `admin`, `tracking`                    | TrackingController                                                          |
| `[RequirePickingOrTrackingAccess]` | `admin`, `picking`, `tracking`         | ProductionOrdersController.Index                                            |
| `[RequireReportingAccess]` (neu)   | `admin`, `reporting`                   | (für spätere BDE-Controller)                                                |

### 3.2 Neue Filter für Lager

| Filter                            | Prüft Rollen                            | Angewendet auf                       |
|-----------------------------------|-----------------------------------------|--------------------------------------|
| `[RequireStockAccess]` (neu)      | `admin`, `stock`, `stock_keyuser`, `picking` | Einbuchung, Bestandsübersicht   |
| `[RequireStockKeyUserAccess]` (neu) | `admin`, `stock_keyuser`, `picking`    | Lagerplatz-Ausbuchung, Lagerplatz-Umbuchung |

### 3.3 StockMovementsController — Rechte pro Action

| Action                        | Rollen                                        |
|-------------------------------|-----------------------------------------------|
| Einbuchung                    | `stock`, `stock_keyuser`, `picking`, `admin`   |
| Ausbuchung (Artikel)          | `stock`, `stock_keyuser`, `picking`, `admin`   |
| Ausbuchung (Lagerplatz kompl.)| `stock_keyuser`, `picking`, `admin`            |
| Umbuchung                     | `stock`, `stock_keyuser`, `picking`, `admin`   |
| Umbuchung (Lagerplatz kompl.) | `stock_keyuser`, `picking`, `admin`            |
| Bestandsübersicht             | `stock`, `stock_keyuser`, `picking`, `admin`   |

### 3.4 Admin-Wildcard — Bewusste Verhaltensänderung

**Achtung**: Im bisherigen System hat ein Admin-User NICHT automatisch alle Rechte (z.B. `CanPick` musste separat gesetzt sein). Mit dem neuen Rollensystem erhält die `admin`-Rolle explizit Zugriff auf alle Bereiche. Dies ist eine **bewusste Erweiterung**, nicht nur ein Refactoring. Alle bestehenden Admin-User erhalten dadurch automatisch vollen Zugriff.

`IsAdminAsync()` → `HasRoleAsync("admin")`. Alle Filter prüfen Admin als erste Rolle — Admin überspringt alle Berechtigungsprüfungen.

### 3.5 StockMovementsController — Umbau auf Action-Level

Der bestehende Class-Level-Filter `[RequirePickingAccess]` auf `StockMovementsController` wird **entfernt**. Stattdessen erhalten die einzelnen Actions die in Sektion 3.3 definierten Filter. Ebenso wird `[RequirePickingAccess]` auf `StockOverviewController` durch `[RequireStockAccess]` ersetzt.

### 3.6 Picking-Rolle inkludiert Stock-Zugriff — Designentscheidung

Die `picking`-Rolle ist bewusst in `[RequireStockAccess]` und `[RequireStockKeyUserAccess]` enthalten. Kommissionierer brauchen vollen Lagerzugriff für ihre Arbeit. Dies ist eine Berechtigung-Erweiterung gegenüber dem alten System, wo Picking und Lager nicht getrennt waren.

### 3.7 Generische Filter-Basis (Vorbereitung Phase B)

Alle benannten Filter delegieren intern an eine gemeinsame Methode. Wenn später Phase B (Permissions per GUI) kommt, kann ein generischer `[RequireRole("stock", "admin")]`-Filter die Einzelfilter ersetzen.

### 3.8 Vollständige Filter-Zuordnung (alle Controller)

Zusätzlich zu den oben genannten:
- `ProductionWorkplacesController` — bleibt `[RequireMasterDataAccess]`
- `StockOverviewController` — wechselt von `[RequirePickingAccess]` zu `[RequireStockAccess]`

---

## 4. Datenmigration

### 4.1 Migrations-Reihenfolge (Zwei-Phasen-Ansatz)

**Migration 1: Tabellen anlegen + Daten migrieren** (alte Spalten bleiben noch)

1. `Roles`-Tabelle anlegen + 7 Standardrollen seeden
2. `UserRoles`-Tabelle anlegen
3. Bestehende Boolean-Felder → Rollen-Zuweisungen migrieren:

| Altes Feld                | → Rolle       |
|---------------------------|---------------|
| `IsAdmin = 1`             | → `admin`     |
| `HasMasterDataAccess = 1` | → `masterdata`|
| `CanPick = 1`             | → `picking`   |
| `CanViewTracking = 1`     | → `tracking`  |
| `CanReportOperations = 1` | → `reporting` |

4. AppSetting `StammdatenADGruppe` → Wert in `Role.AdGroup` der Rolle "masterdata" übertragen, AppSetting-Zeile löschen

**Kein automatisches Mapping** zu `stock` / `stock_keyuser` — neue Rollen werden manuell zugewiesen.

**Migration 2: Alte Spalten entfernen** (nach erfolgreicher Verifikation)

5. Alte Boolean-Felder vom User-Model entfernen (`IsAdmin`, `HasMasterDataAccess`, `CanPick`, `CanViewTracking`, `CanReportOperations`)

Dieser Zwei-Phasen-Ansatz erlaubt Verifikation zwischen den Migrationen. Falls Probleme auftreten, sind die alten Daten noch vorhanden.

### 4.2 SQL-Scripts

- Neues `SQL/XX_AddRoles.sql` mit `OBJECT_ID`-Guard (idempotent)
- `SQL/00_FreshInstall.sql` aktualisieren (neue Tabellen, Seed-Daten, alte Spalten entfernt)
- `SQL/AgentJobs/` — nicht betroffen (importieren keine User-Berechtigungs-Felder)

### 4.3 EF Migration

- `dotnet ef migrations add AddRolesAndUserRoles`
- Model-Änderungen: User ohne alte Booleans, neue DbSets `Roles`, `UserRoles`
- `__EFMigrationsHistory` in SQL-Script markieren

---

## 5. UI-Änderungen

### 5.1 Rollen-Verwaltung (neu)

**Menüpunkt**: Stammdaten → "Rollen" (sichtbar für `masterdata`, `admin`)

**Index-View** — Tabelle:

| Spalte          | Beschreibung                               |
|-----------------|--------------------------------------------|
| Name            | Anzeigename                                |
| Key             | Technischer Schlüssel                      |
| AD-Gruppe       | Falls gesetzt                              |
| Benutzer-Anzahl | Anzahl direkt zugewiesener User            |
| Aktionen        | Bearbeiten                                 |

**Edit-View**:
- **Name** (Textfeld, required)
- **Description** (Textfeld, optional)
- **AD-Gruppe** (Textfeld, optional)
  - Placeholder: `z.B. WMS_Lager`
  - Hilfetext: "SAMAccountName der AD-Gruppe (nur Gruppenname, kein DN). Mitglieder dieser Gruppe erhalten die Rolle automatisch."
- **Key** (readonly bei Systemrollen, editierbar bei eigenen)
- **SortOrder** (readonly bei Systemrollen, editierbar bei eigenen)
- **Löschen**: Nur bei `IsSystem = false`

### 5.2 User-Verwaltung (Umbau)

**Edit-View** — alte Checkboxen ersetzen durch Rollen-Checkbox-Gruppe:

```
Rollen:
☑ Administrator
☐ Stammdaten
☑ Kommissionierer
☐ Lager
☐ Lager Keyuser
☑ Teileverfolgung
☐ Betriebsdaten (BDE)
```

- Sortiert nach `Role.SortOrder`
- Direkte Zuweisung = Checkbox an/aus
- AD-basierte Rollen: Info-Badge `"(via AD-Gruppe WMS_Lager)"` — nicht abwählbar, nicht als Checkbox

### 5.3 Navbar-Anpassung

Menü-Sichtbarkeit basiert auf Rollen:

| Menüpunkt              | Sichtbar für Rollen                        |
|------------------------|--------------------------------------------|
| Werkstattaufträge      | `picking`, `tracking`, `admin`             |
| Lagerbewegungen        | `stock`, `stock_keyuser`, `picking`, `admin`|
| Bestände               | `stock`, `stock_keyuser`, `picking`, `admin`|
| Kommissionierung       | `picking`, `admin`                         |
| Teileverfolgung        | `tracking`, `admin`                        |
| Stammdaten (Dropdown)  | `masterdata`, `admin`                      |
| → Rollen               | `masterdata`, `admin`                      |

---

## 6. Konfiguration über Settings

### 6.1 AppSetting (DB)

Kein `RollenSystemAktiv`-Schalter nötig — der Zwei-Phasen-Migrationsansatz (Sektion 4.1) erlaubt sicheres Rollback, solange Migration 2 nicht ausgeführt wurde. Ein Fallback-Schalter ohne die alten Spalten wäre wirkungslos.

### 6.2 appsettings.json

| Key                            | Default | Beschreibung                                                                |
|--------------------------------|---------|-----------------------------------------------------------------------------|
| `Security:AdGroupCacheMinutes` | `5`     | Cache-Dauer für AD-Gruppen-Mitgliedschaften (MemoryCache, über Requests hinweg) |

Liegt in `appsettings.json` (nicht DB), konsistent mit dem bestehenden Pattern für Service-Level-Config (z.B. `Sync:OseonTrackingEnabled`).

### 6.3 Entferntes AppSetting

| Key                   | Ersetzt durch                          |
|-----------------------|----------------------------------------|
| `StammdatenADGruppe`  | `Role.AdGroup` auf Rolle "masterdata"  |

---

## 7. Betroffene Dateien (geschätzt)

### Neue Dateien
- `Models/Role.cs` — Entity
- `Models/UserRole.cs` — Junction-Entity
- `Models/RoleKeys.cs` — Statische Konstanten
- `Models/ViewModels/RoleEditViewModel.cs` — ViewModel für Rollen-CRUD
- `Controllers/RolesController.cs` — CRUD für Rollen
- `Views/Roles/Index.cshtml` — Rollen-Liste
- `Views/Roles/Edit.cshtml` — Rollen-Bearbeitung
- `Views/Roles/Create.cshtml` — Rollen-Anlage (oder shared mit Edit)
- `Filters/RequireStockAccessAttribute.cs` — Neuer Filter
- `Filters/RequireStockKeyUserAccessAttribute.cs` — Neuer Filter
- `Filters/RequireReportingAccessAttribute.cs` — Neuer Filter
- `SQL/XX_AddRoles.sql` — Migrations-Script (Phase 1: Tabellen + Datenmigration)
- `SQL/XX+1_RemoveOldPermissionColumns.sql` — Migrations-Script (Phase 2: Alte Spalten entfernen)
- `Migrations/YYYYMMDD_AddRolesAndUserRoles.cs` — EF Migration Phase 1
- `Migrations/YYYYMMDD_RemoveOldPermissionColumns.cs` — EF Migration Phase 2

### Geänderte Dateien
- `Models/User.cs` — Felder entfernen, Navigation Property hinzufügen
- `Data/ApplicationDbContext.cs` — Neue DbSets, OnModelCreating (inkl. Unique Index auf Role.Key)
- `Services/ICurrentUserService.cs` — Neue Methoden
- `Services/CurrentUserService.cs` — Rollenbasierte Implementierung
- `Program.cs` — Admin-Seeding: UserRole statt `HasMasterDataAccess = true`
- `Controllers/UsersController.cs` — Rollen-Checkboxen statt Booleans
- `Controllers/StockMovementsController.cs` — Class-Level-Filter entfernen, Action-Level Filter
- `Controllers/StockOverviewController.cs` — `[RequirePickingAccess]` → `[RequireStockAccess]`
- `Views/Users/Edit.cshtml` — Checkbox-Gruppe für Rollen
- `Views/Users/Create.cshtml` — Checkbox-Gruppe für Rollen (analog Edit)
- `Views/Users/Index.cshtml` — Rollen-Badges statt IsAdmin-Badge
- `Views/Shared/_Layout.cshtml` — Navbar rollenbasiert
- `Filters/RequireMasterDataAccessAttribute.cs` — Intern auf Rollen
- `Filters/RequirePickingAccessAttribute.cs` — Intern auf Rollen
- `Filters/RequireTrackingAccessAttribute.cs` — Intern auf Rollen
- `Filters/RequirePickingOrTrackingAccessAttribute.cs` — Intern auf Rollen
- `Filters/RequireAdminAccessAttribute.cs` — Intern auf Rollen
- `appsettings.json` — `Security:AdGroupCacheMinutes` hinzufügen
- `SQL/00_FreshInstall.sql` — Aktualisieren
- `PROJECT_STATUS.md` — Aktualisieren
- `CLAUDE.md` — Rollen-Dokumentation ergänzen
- `README.md` — Feature dokumentieren

---

## 8. Testing

### 8.1 Unit Tests — CurrentUserService

- `HasRoleAsync` gibt `true` zurück wenn User die Rolle direkt hat
- `HasRoleAsync` gibt `true` zurück wenn User die Rolle via AD-Gruppe hat
- `HasAnyRoleAsync` gibt `true` zurück wenn mindestens eine Rolle zutrifft
- `IsAdminAsync` → Admin hat Zugriff auf alles
- Rollen-Caching: Nur ein DB-Aufruf pro Request (auch bei mehreren `HasRoleAsync`-Aufrufen)

### 8.2 Unit Tests — Filter

- Jeder Filter: Zugriff gewährt wenn passende Rolle vorhanden
- Jeder Filter: Redirect auf `AccessDenied` wenn keine passende Rolle
- Admin-Wildcard: Jeder Filter gewährt Zugriff für Admin-Rolle

### 8.3 Integration Tests — RolesController

- CRUD-Operationen auf Rollen
- Systemrollen nicht löschbar
- Unique Constraint auf `(UserId, RoleId)` — keine doppelten Zuweisungen

### 8.4 Integration Tests — User-Rollen-Zuweisung

- Rollen-Checkboxen im User-Edit: Zuweisung erstellt `UserRole`-Einträge
- Abwählen einer Rolle entfernt den `UserRole`-Eintrag
- AD-basierte Rollen sind nicht manuell abwählbar

### 8.5 Migration Tests

- Nach Migration 1: Alle bestehenden Boolean-Flags korrekt in Rollen überführt
- Kein User verliert Berechtigungen durch die Migration

### 8.6 Test-Infrastruktur

- `TestApplicationDbContext` bleibt (InMemory-Kompatibilität)
- Neue Test-Helpers: `CreateUserWithRoles(params string[] roleKeys)` für Setup

---

## 9. Zukunft: Phase B (Permissions per GUI)

Die gewählte Architektur ermöglicht später:
1. Neue Entity `Permission` (Key, Name, Description)
2. Junction `RolePermission` (RoleId, PermissionId)
3. Filter prüft dann `HasPermissionAsync("stock.transfer")` statt `HasRoleAsync("stock_keyuser")`
4. Rollen + Permissions komplett über UI verwaltbar
5. Kein Umbau der Grundstruktur nötig — nur Erweiterung
