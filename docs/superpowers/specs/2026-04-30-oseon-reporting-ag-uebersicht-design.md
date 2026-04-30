# OSEON Reporting — AG-Übersicht Design Spec

**Datum:** 2026-04-30
**Version:** v1.8.3 (geplant)
**Branch:** `main`

## 1. Ziel & Kontext

Neuer Reporting-Bereich für die Werkstattleitung und Disposition: eine Übersicht aller OSEON-relevanten Arbeitsgänge gruppiert nach Zeit-Slice (Überfällig | Heute | Zukunft). Antwortet die Fragen: "Was ist heute geplant? Was davon erledigt? Welche AGs hängen wir hinterher? Was kommt in den nächsten Tagen?"

Baut auf der existierenden OSEON-Tracking-Infrastruktur auf (lokaler Mirror der OSEON-DB, `OseonProductionOrder`, `OseonWorkOperation`, `OseonOperationConfig`). Liefert keine eigenen Sync-Mechanismen — liest aus dem bestehenden, vom `OseonSyncService` gepflegten Datenbestand.

## 2. Architektur

Eigenständige Reporting-Seite unter `/Reporting/OseonOperations`, geschützt durch `[RequireReportingAccess]` (Filter existiert, Rolle `reporting` ist seit Phase 1 vorbereitet). Single-Action Controller, eine View, ein KPI- + Row-VM-Pärchen, eine zusätzliche Repository-Methode am bestehenden `OseonProductionOrderRepository`.

Querschnittliche Logik (Termin-Berechnung mit Offset + Werktage + optional Feiertage) wird in einen neuen Helper-Service `OseonOperationDueDateCalculator` ausgelagert. Bestehender `TrackingController` bekommt Inline-Logik durch diesen Helper ersetzt (DRY) — Verhalten bleibt identisch (Tracking nutzt den Helper ohne Holiday-Provider).

```
                           ┌─────────────────────────┐
                           │ OseonReportingController│ [RequireReportingAccess]
                           └────────────┬────────────┘
                                        │
                          ┌─────────────┴─────────────┐
                          ▼                           ▼
        IOseonProductionOrderRepository      OseonOperationDueDateCalculator
        .GetRelevantOperationsFor                 (uses IHolidayProvider)
         ReportingAsync(filter, horizon)
                          │
                          ▼
                  OseonReportingViewModel
                  ├─ OseonReportingKpiViewModel
                  └─ List<OseonReportingRowViewModel>
                          │
                          ▼
              OseonReporting/OperationsOverview.cshtml
```

## 3. Komponenten

### 3.1 Neue Files

- **`Controllers/OseonReportingController.cs`** — `[RequireReportingAccess]`. Eine Action `OperationsOverview(workplaceId?, operationNames?, customerOrderNumber?, faNumber?, horizonDays?, slice?)`.
- **`Models/ViewModels/OseonReportingViewModel.cs`** — Hülle: Filter-State, KPI-VM, Row-Liste, gruppierte Tag-Sektionen für Zukunft, "AGs ohne Config"-Counter.
- **`Models/ViewModels/OseonReportingRowViewModel.cs`** — Eine AG-Zeile.
- **`Models/ViewModels/OseonReportingKpiViewModel.cs`** — Vier KPI-Counter.
- **`Services/IHolidayProvider.cs` + `HolidayProvider.cs`** — Lädt einmalig alle Holidays im benötigten Datumsbereich, bietet `IsHoliday(DateTime date)`.
- **`Services/OseonOperationDueDateCalculator.cs`** — Termin-Berechnung. Konstruktor akzeptiert `IHolidayProvider?` (null = nur Wochenenden überspringen).
- **`Views/OseonReporting/OperationsOverview.cshtml`** — KPI-Cards + Tabs + Filter-Bar + Tabelle.

### 3.2 Geänderte Files

- **`Data/Repositories/IOseonProductionOrderRepository.cs` + `OseonProductionOrderRepository.cs`** — neue Methode `GetRelevantOperationsForReportingAsync(filter, horizonDays, today, ct)`.
- **`Controllers/TrackingController.cs`** — Inline-Termin-Berechnung durch `OseonOperationDueDateCalculator.Calculate(...)` ersetzen (ohne HolidayProvider, Verhalten unverändert).
- **`Views/Shared/_Layout.cshtml`** — neuer Top-Level-Dropdown "Reporting" mit aktuell einem Eintrag "OSEON AG-Übersicht", sichtbar via `await CurrentUserService.HasReportingAccessAsync()` (oder bestehende Methode wiederverwenden, je nach Service-API).
- **`Models/AppSettingKeys.cs`** — neue Konstante `OseonReportingHorizonDays`.
- **`Program.cs`** — AppSetting-Seed `(OseonReportingHorizonDays, "10", "Reporting: Tage in die Zukunft")`.
- **`IdealAkeWms/AppVersion.cs` + `IDEALAKEWMSService/AppVersion.cs`** — Version `1.8.3`, Date `2026-04-30`.
- **`SQL/00_FreshInstall.sql`** — Seed des neuen AppSetting + neuer Migration-Eintrag (falls schema-relevant — hier nicht: keine Schema-Änderung, nur AppSetting-Seed).
- **`SQL/49_AddOseonReportingHorizonSetting.sql`** — idempotenter Seed des neuen AppSetting.
- **`CLAUDE.md`** — AppSettings-Tabelle ergänzen, neuer Filter `[RequireReportingAccess]` in Zugriffsschutz-Tabelle als "tatsächlich verwendet" dokumentieren.
- **`Views/Help/Index.cshtml` + `Views/Help/Changelog.cshtml`** — Dokumentation + Changelog-Eintrag.
- **`PROJECT_STATUS.md`** — neuer v1.8.3-Eintrag.
- **`docs/TESTSZENARIEN.md`** — neuer Bereich 16 mit TS-16.1..16.6.

## 4. Datenmodell & VMs

```csharp
public enum OseonReportingSlice : byte { Overdue = 1, Today = 2, Future = 3 }

public record OseonReportingKpiViewModel(
    int OverdueCount,
    int TodayPlannedCount,
    int TodayDoneCount,
    int FutureCount);

public record OseonReportingRowViewModel(
    string CustomerOrderNumber,
    string FaNumber,
    int Position,
    string OperationName,
    string? WorkplaceName,
    DateTime CalculatedDueDate,
    byte OseonStatus,
    string StatusText,
    string StatusBadgeClass,
    OseonReportingSlice Slice,
    bool IsDoneToday);

public class OseonReportingViewModel
{
    public OseonReportingKpiViewModel Kpis { get; set; } = default!;
    public List<OseonReportingRowViewModel> Rows { get; set; } = new();
    public List<OseonReportingDayGroup> FutureDayGroups { get; set; } = new();
    public int OperationsWithoutConfigCount { get; set; }
    public DateTime? DataAsOf { get; set; }            // Max(LastChangedInOseon)
    public OseonReportingFilter Filter { get; set; } = new();
    public List<string> AvailableOperationNames { get; set; } = new();
    public List<ProductionWorkplace> AvailableWorkplaces { get; set; } = new();
    public int HorizonDaysEffective { get; set; }
}

public record OseonReportingDayGroup(DateTime Date, int Count, List<OseonReportingRowViewModel> Rows);

public class OseonReportingFilter
{
    public int? WorkplaceId { get; set; }
    public List<string> OperationNames { get; set; } = new();
    public string? CustomerOrderNumber { get; set; }
    public string? FaNumber { get; set; }
    public int? HorizonDaysOverride { get; set; }
    public OseonReportingSlice? Slice { get; set; }     // Tab-Auswahl, Default = Today
}
```

## 5. Query-Logik

```sql
SELECT wo, po, cfg
FROM OseonWorkOperation wo
INNER JOIN OseonProductionOrder po ON wo.OrderOseonId = po.OseonId
LEFT JOIN OseonOperationConfig cfg ON wo.Name = cfg.OperationName
WHERE
  cfg.IsOseonRelevant = 1                                  -- LEFT JOIN: NULL fällt raus
  AND po.OseonStatus IN (20, 30, 60, 90)                   -- aktive Aufträge
  AND wo.OseonStatus != 95                                 -- nicht stornierte AGs
  AND po.DueDate IS NOT NULL                               -- Termin vorhanden
  AND (@workplaceId IS NULL OR po.ProductionWorkplaceId = @workplaceId)
  AND (@operationNames IS NULL OR wo.Name IN (@operationNames))
  AND (@customerOrderNumber IS NULL OR po.CustomerOrderNumber LIKE @customerOrderNumber + '%')
  AND (@faNumber IS NULL OR po.OseonOrderNumber LIKE @faNumber + '%')
ORDER BY po.DueDate, po.OseonOrderNumber, wo.PositionNumber
```

Materialisierung mit `ToListAsync()`. Anschließend in C#:

```
foreach (var (wo, po, cfg) in raw):
    calcDate = OseonOperationDueDateCalculator.Calculate(po.DueDate.Value, cfg.DueDateOffsetDays, holidayProvider)
    slice = calcDate < today AND wo.OseonStatus != 90  → Overdue
            calcDate == today                          → Today
            calcDate > today AND ≤ today + horizon     → Future
            sonst                                       → ausgeschlossen
    isDoneToday = wo.OseonStatus == 90
                  AND wo.LastStatusReportInOseon?.Date == today

KPIs:
  OverdueCount       = count where slice == Overdue
  TodayPlannedCount  = count where slice == Today
  TodayDoneCount     = count where isDoneToday == true                  // unabhängig vom slice
  FutureCount        = count where slice == Future

OperationsWithoutConfigCount =
  count from raw query where cfg is null AND po.OseonStatus IN (20,30,60,90)
                       AND wo.OseonStatus != 95
                       (separate Query)
```

Werktag-/Feiertags-Berechnung in C#, weil EF die Logik nicht in SQL übersetzt.

## 6. Helper: `OseonOperationDueDateCalculator`

```csharp
public class OseonOperationDueDateCalculator
{
    private readonly IHolidayProvider? _holidays;

    public OseonOperationDueDateCalculator(IHolidayProvider? holidays = null)
    {
        _holidays = holidays;
    }

    public DateTime Calculate(DateTime baseDate, int offsetDays)
    {
        if (offsetDays == 0) return baseDate.Date;

        var sign = Math.Sign(offsetDays);
        var remaining = Math.Abs(offsetDays);
        var current = baseDate.Date;

        while (remaining > 0)
        {
            current = current.AddDays(sign);
            if (IsWorkday(current)) remaining--;
        }
        return current;
    }

    private bool IsWorkday(DateTime d)
    {
        if (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday) return false;
        if (_holidays?.IsHoliday(d) == true) return false;
        return true;
    }
}
```

## 7. UI-Layout

**Header-Zeile:**
- `<h2>OSEON AG-Übersicht</h2>` (links) + Untertitel "Geplante vs. erledigte Arbeitsgänge"
- Daten-Stand-Badge (rechts): "Daten Stand: 30.04.2026 14:32" oder "noch nie synchronisiert"

**Filter-Bar (immer sichtbar, `flex-wrap gap-2`):**
- Werkbank-Dropdown (Single-Select)
- AG-Name-Multiselect (Select2, Quelle: Configs mit `IsOseonRelevant=true`)
- Kunden-/Auftragsnummer Suchfeld + QR-Scan-Button
- FA-Nummer Suchfeld
- Horizont-Override (Number, Default = AppSetting)
- "Filter zurücksetzen"-Button

**Banner (conditional):**
- Wenn `OperationsWithoutConfigCount > 0`: gelbe Box "X AGs ohne Config-Eintrag — bitte unter Stammdaten → OSEON-Arbeitsgänge pflegen"

**KPI-Cards (4 Cards, anzeigend, nicht klickbar):**
| Card | Farbe | Wert |
|---|---|---|
| Überfällig | rot | `OverdueCount` |
| Heute geplant | neutral | `TodayPlannedCount` |
| Heute erledigt | grün | `TodayDoneCount` |
| Zukunft (X Tage) | blau | `FutureCount` |

**Tabs/Pills (Slice-Wechsel):** `Heute (Default) | Überfällig | Zukunft | Alle`. Klick → Page-Reload mit `?slice=...` (oder JS-Filter — pragmatisch erstmal Reload, simpel).

**AG-Liste:**
- `.filterable-table` mit `data-col-key` pro Spalte (column-preferences-Pattern)
- Spalten: Kunde | Auftrag (Link → Tracking) | FA | Pos | AG | Werkbank | Soll-Termin (calc.) | Status | Slice
- Default-Sort: `CalculatedDueDate ASC`, dann `OseonStatus ASC`, dann `PositionNumber ASC`
- Status-Badge via bestehende `OseonStatusHelper.GetBadgeClass()`
- Slice-Badge: rot/neutral/blau

**Zukunfts-Tab Spezial:**
- Tabelle nach `CalculatedDueDate` gruppiert
- Tages-Header: "Mi 06.05.2026 — 23 AGs"
- Pro Tag bis Tag 14 ab heute, danach in Wochen-Buckets ("KW 22 — 47 AGs")

**Empty State:**
- Kein Eintrag: `<div class="alert alert-info">Keine OSEON-relevanten Arbeitsgänge im gewählten Zeitraum.</div>`

**Responsive:**
- KPI-Cards 4×col-md, gestapelt auf Mobile (col-12)
- Tabelle in `.table-responsive` Wrapper
- Filter-Bar `flex-wrap gap-2`

## 8. Berechtigung & Menü

- **Filter:** `[RequireReportingAccess]` (existiert, `Filters/RequireReportingAccessAttribute.cs`)
- **Rolle:** `reporting` (CLAUDE.md Rollenkonzept)
- **Menü:** Neuer Top-Level-Dropdown "Reporting" in `_Layout.cshtml`, sichtbar via `await CurrentUserService.HasReportingAccessAsync()`. Aktuell ein Eintrag: "OSEON AG-Übersicht". Reservation für künftige Reports.
- **`ICurrentUserService` Methode:** Existing `CanReportOperationsAsync()` falls vorhanden — sonst `HasReportingAccessAsync()` ergänzen (klein, ein-zeilige Methode analog zu anderen `Has*Access`-Methoden).

## 9. AppSettings

| Key | Default | Beschreibung |
|---|---|---|
| `OseonReportingHorizonDays` | `10` | Reporting: Tage in die Zukunft |

Min/Max in Controller-Logik geclampt: `Math.Clamp(parsed, 1, 60)`.

## 10. Error Handling & Edge Cases

- Leere Holidays-Tabelle: HolidayProvider liefert leeres HashSet → Calculator nutzt nur Wochenenden. Kein Crash.
- Keine OSEON-relevanten AGs konfiguriert: Banner + leere KPIs (alle 0).
- Sync noch nie gelaufen: `DataAsOf == null` → Badge "noch nie synchronisiert", Empty-State-Hinweis.
- Ungültiger AppSetting: `int.TryParse` mit Default 10, dann clamp.
- Unbekannte `workplaceId`: Filter greift, leere Liste.
- `po.DueDate` null: AG aus Query bereits ausgeschlossen.
- `wo.LastStatusReportInOseon` null: AG zählt **nicht** als "heute erledigt" (auch wenn Status=90).
- Keine Berechtigung: Standard-Filter-Block.

## 11. Testing

### Unit Tests

**`OseonOperationDueDateCalculatorTests` (~6 Tests):**
- Wochenende übersprungen
- Feiertag übersprungen (mit HolidayProvider)
- Negativer Offset (Termin vor DueDate)
- Offset = 0 (Termin = DueDate)
- Ohne HolidayProvider: nur Wochenenden
- Mehrere aufeinanderfolgende Feiertage

### Repository Tests (`OseonProductionOrderRepositoryTests` erweitern, ~5 Tests)

- AGs ohne `OseonOperationConfig` ausgeschlossen
- Aufträge mit Status NOT IN (20,30,60,90) ausgeschlossen
- Stornierte AGs (95) ausgeschlossen
- Filter `workplaceId` greift
- Filter `operationNames` greift (Multi-Select)

### Controller Tests (`OseonReportingControllerTests`, ~4 Tests)

- KPI-Counts für gemischtes Szenario (1 überfällig, 2 heute geplant, 1 davon heute erledigt, 3 zukunft)
- "Heute erledigt"-Logik: `Status=90 AND LastStatusReportInOseon.Date == today` zählt unabhängig vom Slice
- Banner für `OperationsWithoutConfigCount > 0` rendert
- Default-Tab/Slice ist `Today` wenn kein Query-Parameter

### Manuelle Testszenarien (TESTSZENARIEN.md, neuer Bereich 16, 6 Tests)

- TS-16.1 KPI-Cards zeigen korrekte Counts
- TS-16.2 Tab-Wechsel filtert Tabelle
- TS-16.3 Filter Werkbank/AG-Name funktioniert
- TS-16.4 Auftragsnummer-Link öffnet Tracking
- TS-16.5 Banner für ungepflegte Configs
- TS-16.6 Berechtigungs-Block für Nicht-Reporter

## 12. Versions-Bump

- Web + Service: `1.8.2` → `1.8.3`
- Date: `2026-04-30`
- Changelog-Eintrag mit Phase-spezifischem Text

## 13. Out of Scope (YAGNI)

- Excel-/CSV-Export
- Druck-Layout
- User-Preferences-Speichern (Filter-State)
- Realtime-Auto-Refresh
- Server-Side Pagination (alles auf einer Seite, max ~2000 Zeilen erwartet; falls real überschritten → Phase 2)
- Manueller "Sync now"-Button
- Eigenes Sync-Metadaten-Modell
- Edit-Action auf AGs (read-only)
- Sub-Charts / Trend-Grafiken
- E-Mail-Benachrichtigung bei Überfälligkeit
