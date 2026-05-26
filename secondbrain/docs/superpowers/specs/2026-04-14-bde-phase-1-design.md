# BDE — Betriebsdatenerfassung, Phase 1 (Kern)

## Zusammenfassung

BDE erfasst den IST-Zustand der Fertigung in Echtzeit: welcher Werker arbeitet an welchem Arbeitsgang, an welcher Werkbank, mit welchem Ergebnis. Phase 1 liefert den Kern — Statusverwaltung, Mengenmeldung, Terminal-UI und Leitstand-Cockpit. Störungsmeldungen und SAGE-Rückmeldung sind Phase 2.

## Motivation

Heute endet die WMS-Sicht am Lagerplatz: Aufträge, Bestände, Kommissionierung sind transparent — die Fertigung selbst ist eine Black Box. BDE schließt diese Lücke: Rüstzeiten und Produktionszeiten werden erfasst, das Leitstand-Cockpit zeigt live, was an welcher Werkbank passiert, und Gutmenge/Ausschuss werden dokumentiert. Damit entsteht die Datenbasis für spätere SAGE-Rückmeldung und Fertigungs-Reports.

## Scope Phase 1

### In Scope
- Arbeitsgang-Statusverwaltung: Rüsten, Starten, Unterbrechen, Fortsetzen, Beenden
- Mengenmeldung: Gutmenge + Ausschuss, beim Beenden und als Teilfertigmeldung
- Terminal-UI für Werker (Scan-basiert)
- Leitstand-Cockpit (5-Sekunden-Polling)
- Schichtleiter-UI: BDE-Anwender + ungeplante Aktivitäts-Kategorien pflegen, Buchungsliste einsehen
- Admin-UI: Buchungs-Korrektur und -Storno
- Ungeplante Arbeitsgänge via freie Kategorien (Wartung, Reinigung, Schulung …)

### Nicht in Scope (Phase 2 oder später)
- Störungsmeldung + Störungsgrund-Katalog
- SAGE-Rückmeldung (Mengen/Zeiten zurück an SAGE 100)
- Ungeplante AGs mit Bezug zu bestehenden FAs (nur freie Kategorien in Phase 1)
- Personalnummern-Sync aus externen Systemen (manuell)
- Dedizierte BDE-Reports und Analytics
- Feature-Flag `BdeAktiv` (bewusst ausgelassen — Feature wird regulär ausgerollt)
- Reduziertes Kiosk-Layout (normale Navbar)

## Akteure & Rollen

Drei neue Rollen-Keys in `RoleKeys.cs`:

| Rolle | Key | Phase-1-Rechte |
|-------|-----|----------------|
| **BDE-Mitarbeiter** | `bde_user` | Terminal-Buchung: AG scannen, Operator scannen, Status wechseln, Menge melden |
| **BDE-Schichtleiter** | `bde_shiftlead` | Alles von `bde_user` + BDE-Anwender CRUD, Aktivitäts-Kategorien CRUD, Buchungsliste lesen, Cockpit |
| **BDE-Admin** | `bde_admin` | Alles von `bde_shiftlead` + Buchungs-Korrektur, Buchungs-Storno, Terminal-Konfiguration |

`admin`-Wildcard greift wie bestehend — Admins haben volle BDE-Rechte.

### Neue Filter-Attribute
- `[RequireBdeUserAccess]` → `admin`, `bde_user`, `bde_shiftlead`, `bde_admin`
- `[RequireBdeShiftleadAccess]` → `admin`, `bde_shiftlead`, `bde_admin`
- `[RequireBdeAdminAccess]` → `admin`, `bde_admin`
- Cockpit-Zugang → `admin`, `bde_admin`, `bde_shiftlead` (via `[RequireBdeShiftleadAccess]`)

Die existierende `reporting`-Rolle bleibt reserviert für eine zukünftige BDE-Reporting-Phase — in Phase 1 nicht genutzt.

## Kern-Konzepte

### Drei Entitäts-Schichten

1. **Terminal** (`BdeTerminal`) — physisches Gerät an einer Werkbank, bindet einen WMS-User (den "Arbeitsplatz-User") an eine Default-Werkbank. Terminal-User meldet sich morgens einmal an, bleibt den ganzen Tag eingeloggt. Werkbank ist in der UI umschaltbar, falls das Gerät zwischen Werkbänken wandert.
2. **Operator** (`BdeOperator`) — Werker, bucht per Personalnummer-Scan an beliebigem Terminal. Separate Identität von WMS-`User`, mit optionaler Verknüpfung falls Werker gleichzeitig WMS-User ist.
3. **Buchung** (`BdeBooking`) — Event-Log: eine Zeile pro kontinuierliche Arbeitsphase eines Operators an einem AG. Pause/Fortsetzung und Operator-Handover erzeugen Folge-Buchungen, verknüpft über `ParentBookingId`.

### Status-Modell

| Status | `EndedAt` | Bedeutung |
|--------|-----------|-----------|
| `Running` | `NULL` | Phase läuft aktuell, im Cockpit sichtbar |
| `Paused` | gesetzt | Phase beendet, Fortsetzung erwartet (Folge-Buchung mit `ParentBookingId`) |
| `Finished` | gesetzt | Phase abgeschlossen, keine Fortsetzung |

Zusätzlich Storno-Flag `IsCancelled` (Admin-Aktion, filtert Buchung aus allen Abfragen).

### Buchungs-Typen (`BookingType`)

| Typ | Gültig für | Mengenmeldung |
|-----|-----------|---------------|
| `Setup` | geplanter AG (`WorkOperationId`) | nein |
| `Production` | geplanter AG (`WorkOperationId`) | ja (Gutmenge + Ausschuss) |
| `Activity` | ungeplant (`BdeActivityId`) | nein |

### State-Machine — erlaubte Übergänge

```
(kein offene Buchung beim Operator)
   ├── "Rüsten starten" (Setup)   → Setup-Running
   ├── "Produktion starten"       → Production-Running
   └── "Aktivität starten"        → Activity-Running

Setup-Running
   ├── "Rüsten beenden"          → Finished
   ├── "Produktion starten"      → Setup wird automatisch beendet, neue Production-Running
   └── "Pause"                   → Setup-Paused (EndedAt gesetzt, wartet auf Fortsetzung)

Production-Running
   ├── "Beenden"                 → Mengen-Dialog → Finished
   ├── "Pause"                   → Mengen-Dialog (optional) → Production-Paused
   ├── "Teilfertigmeldung"       → Mengen-Dialog, Status bleibt Running
   └── "Nächsten AG starten"     → Mengen-Dialog (Pflicht) → Finished, neue Buchung

Activity-Running
   ├── "Beenden"                 → Finished
   └── "Nächste Buchung"         → Activity wird automatisch beendet, neue Buchung

Setup-Paused / Production-Paused (nur als "offene Pause" eines Operators)
   └── "Fortsetzen" (derselbe oder anderer Operator)
       → neue Running-Buchung mit ParentBookingId = pausierte Buchung
```

### Auto-Beendigung bei Operator-Wechsel zwischen AGs

Wenn derselbe Operator einen neuen AG startet, während er noch eine offene Buchung hat, wird die alte Buchung automatisch beendet — mit folgenden Regeln:

- War es `Setup` oder `Activity`: automatisches `Finished`, keine Mengen-Abfrage.
- War es `Production`: UI erzwingt Mengen-Eingabe (Gutmenge + Ausschuss) **bevor** die neue Buchung angelegt wird. Alternative: Operator klickt "Pause" → `Production-Paused`, dann neuer Scan möglich.

### Operator-Handover via Pause-Kette

Operator A rüstet (Setup-Running). Operator A pausiert → Setup-Paused. Operator B scannt AG → System erkennt offene Pause, bietet "Fortsetzen als Production-Running" an. Neue Buchung mit `BdeOperatorId = B`, `ParentBookingId = A's Setup`. Die Gesamt-Historie des AG ergibt sich aus der Kette.

## Datenmodell

### Neue Entitäten

#### `BdeTerminal : AuditableEntity`
Terminal-Konfiguration.

| Feld | Typ | Nullable | Beschreibung |
|------|-----|----------|--------------|
| `UserId` | int (FK `User`) | NOT NULL | Der eingeloggte Terminal-User |
| `DefaultProductionWorkplaceId` | int (FK `ProductionWorkplace`) | NOT NULL | Default-Werkbank |
| `Description` | nvarchar(200) | NULL | z.B. "Tablet Werkbank 3 West" |

**Constraints:**
- `UNIQUE (UserId)` — ein User = ein Terminal

#### `BdeOperator : AuditableEntity`
Werker, die am Terminal buchen.

| Feld | Typ | Nullable | Beschreibung |
|------|-----|----------|--------------|
| `PersonnelNumber` | nvarchar(50) | NOT NULL | Wird gescannt |
| `FirstName` | nvarchar(100) | NOT NULL | |
| `LastName` | nvarchar(100) | NOT NULL | |
| `IsActive` | bit | NOT NULL, default 1 | |
| `UserId` | int (FK `User`) | NULL | Optionale Verknüpfung falls Operator auch WMS-User |

**Constraints:**
- `UNIQUE (PersonnelNumber)`
- `UNIQUE (UserId) WHERE UserId IS NOT NULL` (gefiltert — ein User max. ein Operator)

#### `BdeActivity : AuditableEntity`
Kategorien ungeplanter Arbeiten.

| Feld | Typ | Nullable | Beschreibung |
|------|-----|----------|--------------|
| `Code` | nvarchar(20) | NOT NULL | Kurzcode, z.B. "WART" |
| `Name` | nvarchar(100) | NOT NULL | Anzeige-Name, z.B. "Wartung" |
| `IsActive` | bit | NOT NULL, default 1 | |

**Constraints:**
- `UNIQUE (Code)`

#### `BdeBooking : AuditableEntity`
Zentrale Event-Log-Buchung.

| Feld | Typ | Nullable | Beschreibung |
|------|-----|----------|--------------|
| `BdeOperatorId` | int (FK `BdeOperator`) | NOT NULL | |
| `ProductionWorkplaceId` | int (FK `ProductionWorkplace`) | NOT NULL | |
| `BdeTerminalId` | int (FK `BdeTerminal`) | NOT NULL | Audit: wo wurde gebucht |
| `WorkOperationId` | int (FK `WorkOperation`) | NULL | Bei geplantem AG |
| `BdeActivityId` | int (FK `BdeActivity`) | NULL | Bei ungeplantem AG |
| `BookingType` | tinyint (Enum) | NOT NULL | `1=Setup, 2=Production, 3=Activity` |
| `Status` | tinyint (Enum) | NOT NULL | `1=Running, 2=Paused, 3=Finished` |
| `StartedAt` | datetime2 | NOT NULL | Serverzeit |
| `EndedAt` | datetime2 | NULL | `NULL` = Running, gesetzt = Paused/Finished |
| `IsCancelled` | bit | NOT NULL, default 0 | Storno-Flag |
| `CancellationReason` | nvarchar(500) | NULL | |
| `ParentBookingId` | int (FK `BdeBooking`) | NULL | Vorgänger-Buchung bei Fortsetzung |

**Check-Constraints:**
- `(WorkOperationId IS NOT NULL AND BdeActivityId IS NULL) OR (WorkOperationId IS NULL AND BdeActivityId IS NOT NULL)` — genau einer gesetzt
- `(BookingType = 3 AND BdeActivityId IS NOT NULL) OR (BookingType IN (1,2) AND WorkOperationId IS NOT NULL)` — Typ passt zu Ziel
- `(Status = 1 AND EndedAt IS NULL) OR (Status IN (2,3) AND EndedAt IS NOT NULL)` — Status-Konsistenz

**Indizes:**
- Gefilterter Unique: `UNIQUE (WorkOperationId) WHERE EndedAt IS NULL AND IsCancelled = 0` — max. 1 Running-Buchung pro AG
- Gefilterter Unique: `UNIQUE (BdeOperatorId) WHERE EndedAt IS NULL AND IsCancelled = 0` — max. 1 Running-Buchung pro Operator
- Non-unique: `(ProductionWorkplaceId, EndedAt)` — Cockpit-Query
- Non-unique: `(BdeOperatorId, StartedAt DESC)` — Operator-Historie
- Non-unique: `(StartedAt DESC)` — allgemeine Buchungsliste

#### `BdeBookingQuantity : AuditableEntity`
Teilmengen und Abschluss-Mengen einer Production-Buchung.

| Feld | Typ | Nullable | Beschreibung |
|------|-----|----------|--------------|
| `BdeBookingId` | int (FK `BdeBooking`) | NOT NULL | |
| `BdeOperatorId` | int (FK `BdeOperator`) | NOT NULL | Wer meldet |
| `GoodQuantity` | decimal(18,4) | NOT NULL, default 0 | |
| `ScrapQuantity` | decimal(18,4) | NOT NULL, default 0 | |
| `IsFinal` | bit | NOT NULL | `true` bei Abschluss-Meldung, `false` bei Teilmeldung |
| `ReportedAt` | datetime2 | NOT NULL | |

**Indizes:**
- `(BdeBookingId, ReportedAt)` — Aggregation pro Buchung
- Gefilterter Unique: `UNIQUE (BdeBookingId) WHERE IsFinal = 1` — max. 1 finale Meldung pro Buchung

### Bestehende Entitäten — unverändert
`WorkOperation`, `ProductionOrder`, `ProductionWorkplace`, `User` bleiben wie sie sind. Insbesondere `WorkOperation.IsReported`/`ReportedAt`/`ReportedBy` bleiben Phase-2-Reserve für SAGE-Rückmeldung und werden in Phase 1 nicht gesetzt.

### Aggregation der Mengen
Gesamtmengen einer Buchung werden **nicht materialisiert**:

```sql
SELECT SUM(GoodQuantity) AS TotalGood, SUM(ScrapQuantity) AS TotalScrap
FROM BdeBookingQuantities
WHERE BdeBookingId = @id
```

Grund: Single Source of Truth, keine Redundanz, Admin-Korrekturen bleiben konsistent.

## UI / Controller-Struktur

### Controller

| Controller | Zweck | Filter |
|------------|-------|--------|
| `BdeTerminalController` | Terminal-UI (Scan, Buchen, Mengen) | `[RequireBdeUserAccess]` |
| `BdeCockpitController` | Leitstand-Cockpit (Kachel-Ansicht) | `[RequireBdeShiftleadAccess]` |
| `BdeBookingsController` | Buchungsliste + Korrektur + Storno | `[RequireBdeShiftleadAccess]` (Liste), `[RequireBdeAdminAccess]` (Edit/Storno) |
| `BdeMasterDataController` | Tabs: Operatoren + Activities + Terminals | `[RequireBdeShiftleadAccess]` (Operatoren, Activities), `[RequireBdeAdminAccess]` (Terminals) |
| `BdeApiController` | JSON-Endpoints für Cockpit-Polling und Terminal-AJAX | `[RequireBdeUserAccess]` bzw. `[RequireBdeShiftleadAccess]` |

Kein separater `BdeActivitiesController` — als Tab in `BdeMasterDataController` integriert, um Controller-Sprawl zu vermeiden.

### Terminal-UI Flow

1. Terminal-User öffnet `/BdeTerminal` → Seite zeigt Werkbank-Header (aus `BdeTerminal.DefaultProductionWorkplaceId`, umschaltbar).
2. Hauptbereich zeigt **aktuelle aktive Buchung** (falls vorhanden) an dieser Werkbank oder **Scan-Prompt**.
3. Scan-Input (barcode-scanner.js) akzeptiert:
   - **Personalnummer** → Operator identifiziert
   - **FA-AG-QR** → WorkOperation identifiziert
   - Reihenfolge: Operator zuerst scannen, dann AG. Oder umgekehrt — System hält Context bis beides da ist.
4. Wenn Operator + AG identifiziert → Aktions-Buttons je nach Status:
   - Keine aktive Buchung: "Rüsten starten" / "Produktion starten"
   - Laufende Setup: "Rüsten beenden", "Produktion starten", "Pause"
   - Laufende Production: "Beenden (mit Mengen)", "Teilfertigmeldung", "Pause"
   - Pausiert: "Fortsetzen (Rüsten)", "Fortsetzen (Produktion)"
5. Mengen-Dialog: modales Bootstrap-Modal mit Gutmenge + Ausschuss (numerische Felder, Touch-freundlich).
6. Kollisions-Fall (AG bereits woanders in Arbeit): Modal mit Info "AG ist in Arbeit durch *Max Mustermann* an *Werkbank 5* seit 14:23".
7. Ungeplante Aktivitäten: separater Button "Ungeplante Tätigkeit" → Dropdown mit aktiven `BdeActivity`-Einträgen.

### Cockpit-UI Flow

`/BdeCockpit`:
- Kachel-Grid, eine Kachel pro aktiver `ProductionWorkplace`
- Kachel-Inhalt: Werkbank-Name, Operator-Name (aktuell aktiv), AG-Info (FA-Nummer + AG-Nr + Bezeichnung), BookingType-Badge (Rüsten/Produktion/Aktivität), Laufzeit (seit `StartedAt`), Status-Farbe
- Farb-Schema: grün = Production-Running, orange = Setup-Running, blau = Activity-Running, gelb = Paused, grau = frei
- JavaScript-Polling: `GET /api/bde/cockpit` alle 5 Sekunden, re-rendert Kacheln

### API: Cockpit-Endpoint

**`GET /api/bde/cockpit`** → JSON:
```json
{
  "workplaces": [
    {
      "workplaceId": 3,
      "workplaceName": "Werkbank 3",
      "status": "Running",
      "bookingType": "Production",
      "operatorName": "Max Mustermann",
      "orderNumber": "FA-12345",
      "operationNumber": "10",
      "operationName": "Fräsen Seitenteil",
      "startedAt": "2026-04-14T14:23:00",
      "runtimeSeconds": 4732
    },
    {
      "workplaceId": 5,
      "workplaceName": "Werkbank 5",
      "status": "Idle",
      "bookingType": null,
      "operatorName": null
    }
  ],
  "serverTime": "2026-04-14T15:41:52"
}
```

Alle aktiven Werkbänke (konfigurierbar wie bisher), plus freie als `"Idle"`.

## Session & Terminal-Login

- Terminal-User nutzt reguläre WMS-Session (`AppUserId`, 8h Timeout, Cookie `IdealAkeWms.Session`).
- Für Terminal-User wird **Session Sliding Expiration** sichergestellt (Check im Code, bei Bedarf in `Program.cs` konfigurieren) — jeder AJAX-Call verlängert die Session.
- Terminal-UI zeigt im Header diskret den angemeldeten User + Werkbank + Session-Ablauf. Werker wissen bei Session-Ende, dass sie den Admin rufen müssen.
- **Kein Auto-Login**, kein "Remember-Me" — Terminal-User muss morgens manuell anmelden (Sicherheits-Trade-off, akzeptiert).

## Admin-Korrektur & Storno

### Korrektur einer Buchung
Admin öffnet `/BdeBookings/Edit/{id}`. Editierbar:
- `StartedAt`, `EndedAt` (Zeitkorrektur bei vergessenem Ende)
- `BdeOperatorId` (falsche Person gebucht)
- `ProductionWorkplaceId` (falsche Werkbank)
- Teilmengen (`BdeBookingQuantity`-Einträge): editieren / löschen / hinzufügen
- `Status` darf nur zu konsistenten Werten geändert werden (Check-Constraints).

Änderungen setzen `ModifiedAt`/`ModifiedBy`/`ModifiedByWindows` über `ICurrentUserService`.

### Storno
Admin klickt "Stornieren" → Dialog fordert `CancellationReason` → `IsCancelled = true`. Buchung bleibt in DB, wird aus allen produktiven Queries (Cockpit, Reporting, Mengen-Aggregation) ausgeschlossen. Bewusst kein Hard-Delete.

### Vergessene offene Buchungen
Admin kann jede Buchung mit `EndedAt = NULL` per "Manuell beenden"-Action schließen. Dabei `EndedAt = (frei wählbar, Default: letzte Aktivität des Operators)`, Status → `Finished`. Dient dem Aufräumen des Cockpits am Tagesbeginn.

## Migrationen & SQL

### EF Core Migration
`dotnet ef migrations add AddBde` → erzeugt Tabellen für alle fünf neuen Entities + FK-Constraints.

### SQL-Script `SQL/XX_AddBde.sql`
Idempotent via `OBJECT_ID`-Guards:
- `CREATE TABLE BdeTerminals` (guarded)
- `CREATE TABLE BdeOperators` (guarded)
- `CREATE TABLE BdeActivities` (guarded)
- `CREATE TABLE BdeBookings` (guarded) mit Check-Constraints und gefilterten Unique-Indizes
- `CREATE TABLE BdeBookingQuantities` (guarded)
- Tabellen-Erstellung in separatem Batch (GO) vor Index-/Constraint-Erstellung
- Rollen-Inserts: `bde_user`, `bde_shiftlead`, `bde_admin` in `Roles` (guarded via `NOT EXISTS`)
- `__EFMigrationsHistory`-Insert in separatem Batch

### `SQL/00_FreshInstall.sql` Update
Neue Tabellen, Rollen und Indizes eintragen.

### `SQL/AgentJobs/` Anpassungen
Keine — BDE betrifft keine Import-Scripts.

## Rollen-Seeding

In `Program.cs` beim Admin-Seed-Block: neue Rollen `bde_user`, `bde_shiftlead`, `bde_admin` als Seed-Einträge in `Roles` anlegen (falls nicht vorhanden), analog zu `RoleKeys.Admin` etc.

## Version & Dokumentation

- `AppVersion.cs` (Web + Service) hochzählen (ein Minor-Sprung, weil großes Feature)
- `Views/Help/Changelog.cshtml`: neue Version + BDE-Einträge ergänzen
- `Views/Help/Index.cshtml`: neuer BDE-Abschnitt mit Rollen-Erklärung, Terminal-Flow, Cockpit-Nutzung
- `PROJECT_STATUS.md`: BDE-Phase-1 als neues Kapitel
- `README.md`: Feature-Liste erweitern
- `CLAUDE.md`: neue Rollen, Controller-Filter-Tabelle, BDE-Fallstricke ergänzen

## Tests

Analog bestehender Test-Infrastruktur (xUnit + FluentAssertions + Moq + EF InMemory, `TestApplicationDbContext`):

- **Repository-Tests**: `BdeBookingRepository` — Create, Pause, Resume, Finish, Cancel, State-Machine-Violations, Race Condition (gleichzeitige Running-Buchungen auf selben AG)
- **State-Machine-Tests**: alle erlaubten und verbotenen Übergänge
- **Aggregations-Tests**: `BdeBookingQuantity` Summen, mit und ohne Teilmengen, mit stornierten Buchungen
- **Controller-Tests**: `BdeTerminalController` Happy-Path (Scan → Start → Pause → Resume → End mit Mengen), `BdeBookingsController` Korrektur/Storno mit Audit-Feldern
- **Cockpit-API-Test**: JSON-Shape, Idle-Werkbänke, Running-Werkbänke
- **Integration**: InMemory DB kennt keine Check-Constraints und Filter-Indizes — diese werden in separaten SQL-Tests oder manueller QA gegen echte SQL-DB validiert.

## Offene Punkte / bewusst verschoben

- **QR-Code-Format für Personalnummer**: muss mit existierendem Scanner-Code abgestimmt werden (CLAUDE.md-Hinweis: Komma-Suffix, FA-Nummer an 3. Stelle). Prefix-Regel (z.B. `OP-12345` für Operator, `FA-12345,10` für FA-AG) im Implementierungsplan konkretisieren.
- **Cockpit-Rendering-Details** (Responsive-Breakpoints, Touch-Optimierung, Dark-Mode): einheitlich mit `--ake-primary`/`--ake-secondary`, sonst freie Gestaltung im Implementierungsplan.
- **Multi-Operator-Parallelbetrieb** pro AG: bewusst nicht unterstützt, dokumentiert. Falls in der Praxis häufig, Phase-2-Erweiterung möglich.
- **Teilmengen-Korrektur via Terminal** (nicht nur Admin): Phase 1 nur Admin-seitig korrigierbar. Werker kann Teilmeldung hinzufügen, nicht ändern. Verhindert Manipulation.
