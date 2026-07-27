# BDE Phase 2.3 — Schichtkalender + Auto-Pause + Feiertags-Sync

## Zusammenfassung

Phase 2.3 fügt der WMS-Anwendung einen Schichtkalender hinzu, mit dem ein BDE-Background-Service laufende Buchungen am Schichtende automatisch pausiert. Zusätzlich wird ein zweiter Background-Service eingeführt, der Feiertage automatisch von der Public-API Nager.Date synchronisiert und damit die manuelle jährliche Pflege ablöst.

Hauptkomponenten:
- **`BdeShift`**-Entität (neue Tabelle) mit Default-Plan und Werkbank-Overrides
- **`BdeShiftCalendarService`** (read-only) liefert pro Werkbank+Zeitpunkt das relevante Schichtende
- **`BdeAutoPauseService`** (Worker) pausiert laufende Buchungen am Schichtende
- **`HolidaySyncService`** (Worker) synchronisiert Feiertage von Nager.Date
- Neuer `BdeBookingStatus.AutoPaused = 5` als eigenständiger Status für system-pausierte Buchungen
- Master-Toggle `BdeSchichtkalenderAktiv` (default `false`) — Phase-2.2-Verhalten bleibt bei deaktivierter Phase-2.3 unverändert

## Motivation

Bei Phase 2.2 zeigte sich: Mitarbeiter vergessen häufig sich auszustempeln (Schichtende, Heimfahrt, Wochenende). Buchungen laufen "über Nacht" weiter und produzieren unrealistische Stundenzahlen, die für Reports unbrauchbar sind. Phase 2.3 schließt diese Lücke durch automatisierte Schichtende-Erkennung pro Werkbank.

Zweite Motivation: Die `Holiday`-Tabelle wird heute manuell gepflegt — fehlerträchtig und mühsam. Ein automatischer Sync gegen eine zuverlässige Quelle (Nager.Date) erspart die jährliche Pflege.

## Nicht in Scope

- Kein Pausen-Abzug-Setting (kommt mit Stundenlisten-Reports in Phase 2.5+)
- Keine Nachtschichten über Mitternacht (z.B. 22:00–06:00) — Standard-Workflow von AKE braucht das nicht; Schichten validieren `EndTime > StartTime` strikt
- Keine Stundenlisten-Reports
- Keine Mehr-Standort-Default-Kalender (1 globaler Default reicht)
- Keine Cockpit-Varianten (Phase 2.4)
- Auto-Resume entfällt — Mitarbeiter muss am Folge-Tag manuell scannen + Resume klicken

## Settings

### AppSettings (User-facing)

| Key | Default | Beschreibung |
|-----|---------|-------------|
| `BdeSchichtkalenderAktiv` | `false` | Master-Toggle: Schichtkalender + Auto-Pause aktiv |

### ServiceSettings (Worker-Konfiguration)

| Key | Default | Beschreibung |
|-----|---------|-------------|
| `Sync:BdeAutoPauseIntervalMinutes` | `60` | Tick-Intervall für `BdeAutoPauseService` |
| `Sync:FeiertagSyncEnabled` | `false` | Master-Toggle Feiertags-Sync |
| `Sync:FeiertagCountryCode` | `AT` | ISO-Country-Code für Nager.Date |
| `Sync:FeiertagRegion` | (leer) | Optional Bundesland (`AT-3` Niederösterreich, `AT-6` Steiermark, `AT-4` Oberösterreich, `AT-5` Salzburg, `AT-7` Tirol, `AT-8` Vorarlberg, `AT-9` Wien, `AT-1` Burgenland, `AT-2` Kärnten); leer = nationale Feiertage |
| `Sync:FeiertagJahreVoraus` | `2` | Anzahl zukünftiger Jahre (zusätzlich zum aktuellen Jahr) die mitsyncen |

`HolidaySyncService` tickt täglich (hardcoded 24h, kein eigenes Setting — Feiertage ändern sich höchstens jährlich).

## Schema-Änderungen

### Neue Entity `BdeShift`

```csharp
public class BdeShift : AuditableEntity
{
    [Required]
    public DayOfWeek DayOfWeek { get; set; }

    [Required]
    public TimeSpan StartTime { get; set; }

    [Required]
    public TimeSpan EndTime { get; set; }

    public int? ProductionWorkplaceId { get; set; }
    public ProductionWorkplace? ProductionWorkplace { get; set; }

    [StringLength(50)]
    public string? Name { get; set; }
}
```

`ProductionWorkplaceId IS NULL` → Default-Kalender. Andernfalls Werkbank-Override.

Index: `(ProductionWorkplaceId, DayOfWeek)` für schnelle Lookups.

Validierung: `EndTime > StartTime` (Nachtschichten out-of-scope).

### Erweiterung `ProductionWorkplace`

Neues Feld:

```csharp
[Display(Name = "Eigener Schichtplan")]
public bool BdeUseCustomShiftPlan { get; set; } = false;
```

**Schalter-Semantik (P3):**
- `BdeUseCustomShiftPlan = true` → ausschließlich werkbank-eigene `BdeShift`-Einträge gelten. Auch leere Liste = 24/7 frei (keine Auto-Pause)
- `BdeUseCustomShiftPlan = false` → Default-Kalender (`ProductionWorkplaceId IS NULL`) gilt

### Erweiterung `Holiday`

Neues Enum + Feld:

```csharp
public enum HolidaySource : byte
{
    Manual = 1,
    NagerSync = 2
}

[Required]
public HolidaySource Source { get; set; } = HolidaySource.Manual;
```

Bestehende Einträge bekommen per Migration-Default `Source = Manual`.

### Erweiterung `BdeBookingStatus`

```csharp
public enum BdeBookingStatus : byte
{
    Running = 1,
    Paused = 2,
    Finished = 3,
    Resumed = 4,
    AutoPaused = 5
}
```

CHECK-Constraint `CK_BdeBookings_StatusEnded` wird erweitert von `Status IN (2,3,4)` auf `Status IN (2,3,4,5)`.

### EF-Migration + SQL

- EF-Migration `AddBdeShiftCalendar` deckt alle 4 Schema-Änderungen in einem Schritt ab
- `SQL/48_AddBdeShiftCalendar.sql` idempotent (OBJECT_ID / COL_LENGTH / sys.check_constraints Guards), inkl. Seeding der neuen AppSetting + ServiceSettings
- `SQL/00_FreshInstall.sql` aktualisiert (Tabelle, Spalten, CHECK)

## BdeShiftCalendarService

Read-only Service der Schicht-Lookup-Logik kapselt. Verwendet von Auto-Pause und ggf. künftigen Reports.

### Interface

```csharp
public interface IBdeShiftCalendarService
{
    Task<DateTime?> GetShiftEndForBookingAsync(int workplaceId, DateTime startedAt);
    Task<IReadOnlyList<BdeShift>> GetShiftsAsync(int workplaceId, DayOfWeek day);
}
```

### Algorithmus `GetShiftEndForBookingAsync`

1. Setting `BdeSchichtkalenderAktiv` lesen → wenn `false`: return `null`
2. Werkbank laden:
   - `BdeUseCustomShiftPlan == true` → `BdeShift WHERE ProductionWorkplaceId == workplaceId`
   - sonst → `BdeShift WHERE ProductionWorkplaceId IS NULL` (Default-Kalender)
3. Filter auf `DayOfWeek == startedAt.DayOfWeek`
4. Wenn `startedAt.Date` Feiertag ist (`Holiday`-Tabelle, `Date == startedAt.Date`) → return `null`
5. Wenn keine Schichten an diesem Tag → return `null`
6. Suche Schicht in der `startedAt` liegt: `shift.StartTime <= startedAt.TimeOfDay <= shift.EndTime`
7. Wenn keine passende Schicht (P2-Edge):
   - Suche **nächstfolgende Schicht** des Tages mit `shift.StartTime > startedAt.TimeOfDay`
   - Falls vorhanden → return `startedAt.Date + nextShift.EndTime`
   - Sonst → return `null` (alle Schichten des Tages sind bereits beendet — Buchung läuft weiter, MA selbst verantwortlich)
8. Passende Schicht gefunden → return `startedAt.Date + shift.EndTime`

### Tests

| Test | Szenario |
|------|----------|
| `Sunday_NoShifts_ReturnsNull` | Default-Plan Mo–Fr, So → null |
| `Holiday_ReturnsNull` | Mo Feiertag → null trotz Schicht-Konfiguration |
| `Within_EarlyShift_ReturnsShiftEnd` | Frühschicht 06–14, Buchung 08:30 → Mo 14:00 |
| `BetweenShifts_ReturnsNextShiftEnd` | Frühschicht 06–14 + Spätschicht 14–22, Buchung 14:30 → 22:00 |
| `BeforeFirstShift_ReturnsFirstShiftEnd` | Frühschicht 06–14, Buchung 04:00 → 14:00 |
| `AfterLastShift_ReturnsNull` | Frühschicht 06–14, Buchung 23:00 → null |
| `WorkbenchOverride_PrefersOwnPlan` | Werkbank hat eigenen Plan → Default ignoriert |
| `WorkbenchOverrideEmpty_24x7Free` | Override-Toggle EIN + 0 Einträge → null |
| `MasterToggleOff_ReturnsNull` | `BdeSchichtkalenderAktiv = false` → immer null |

## BdeAutoPauseService (Worker)

Background-Service in `IDEALAKEWMSService/Services/BdeAutoPauseService.cs`. Tickt mit `Sync:BdeAutoPauseIntervalMinutes` (default 60).

### Interface + DTO

```csharp
public interface IBdeAutoPauseService
{
    Task<AutoPauseResult> RunAsync(CancellationToken ct);
}

public record AutoPauseResult(int CheckedCount, int PausedCount, List<string> Errors);
```

### Algorithmus

```
1. BdeSchichtkalenderAktiv lesen → wenn false: return (0,0,[])
2. Lade alle aktiven Buchungen: Status==Running AND EndedAt IS NULL AND !IsCancelled
3. Pro Buchung:
   a. shiftEnd = await ShiftCalendar.GetShiftEndForBookingAsync(b.ProductionWorkplaceId, b.StartedAt)
   b. wenn shiftEnd == null → skip (keine Schicht-Vorgabe)
   c. wenn shiftEnd <= DateTime.Now (Schicht ist vorbei):
      - b.Status = AutoPaused
      - b.EndedAt = shiftEnd  (NICHT DateTime.Now — saubere Buchungszeit)
      - SetAuditModified(b) mit ModifiedBy="BDE-AutoPause"
      - keine BdeBookingQuantity-Zeile (System weiß keine Mengen)
      - SaveChanges(); pausedCount++
   d. Bei Exception: in Errors-Liste loggen, mit nächster Buchung weiter (robust gegen Einzelfehler)
4. Logge checked/paused/errors per Serilog
5. return AutoPauseResult
```

### Edge Cases

- **Mehrschicht-Übergang (Frage 6 → A):** Pause an jeder Schicht-Grenze. MA der Folge-Schicht muss neu scannen + Resume klicken.
- **P8 (Smooth-Start):** Beim ersten Tick nach Aktivierung des Master-Toggles werden alle "schon längst überfälligen" Buchungen rückwirkend pausiert mit ihrem korrekten Schichtende. Kein Sondercode — fällt aus dem Algorithmus.
- **Activity + Setup (Frage 5a → B):** Werden gleichbehandelt wie Production. Auch Activity wird auto-pausiert wenn die Werkbank-Schicht endet.

### Tests

| Test | Szenario |
|------|----------|
| `Run_NoActiveBookings_ReturnsZeroCheck` | leere DB → 0/0 |
| `Run_BookingPastShiftEnd_PausesWithShiftEndTimestamp` | Frühschicht endete um 14:00, Service tickt um 14:30 → Status=AutoPaused, EndedAt=14:00 |
| `Run_BookingWithinShift_NotPaused` | Schichtende noch in der Zukunft → unverändert |
| `Run_MasterToggleOff_NoOps` | Setting off → checked=0 |
| `Run_BookingOutsideAnyShift_NotPaused` | Buchung 23:00 nach Tagesschluss → null aus CalendarService → unverändert |
| `Run_HolidayDay_SkipsAutoPause` | Buchung an Feiertag → null aus CalendarService → unverändert |
| `Run_MultipleBookings_OneFailureDoesntStopOthers` | 3 Buchungen, eine wirft → andere 2 pausiert, Errors enthält 1 Eintrag |
| `Run_PreservesAuditFields` | Pausierte Buchung hat ModifiedBy="BDE-AutoPause" |

## HolidaySyncService (Worker, neuer 2. Service)

Background-Service in `IDEALAKEWMSService/Services/HolidaySyncService.cs`. Tickt täglich (24h hardcoded — Feiertage ändern sich höchstens jährlich, häufigeres Polling überflüssig).

### Interface + DTO

```csharp
public interface IHolidaySyncService
{
    Task<HolidaySyncResult> RunAsync(CancellationToken ct);
}

public record HolidaySyncResult(int FetchedCount, int InsertedCount, List<string> Errors);
```

### Algorithmus

```
1. Settings lesen: Sync:FeiertagSyncEnabled, Sync:FeiertagCountryCode, Sync:FeiertagRegion, Sync:FeiertagJahreVoraus
2. Wenn Sync nicht aktiv → return (0,0,[])
3. Jahres-Range: [aktuelles Jahr .. aktuelles Jahr + JahreVoraus]
4. Pro Jahr:
   a. HTTP GET https://date.nager.at/api/v3/PublicHolidays/{year}/{country}
   b. Wenn HTTP-Fehler/Timeout: in Errors loggen, mit nächstem Jahr weiter
   c. Filter:
      - Wenn Region gesetzt: holiday.counties contains region OR holiday.counties is null (national)
      - Wenn Region leer: nur Einträge mit counties == null (national)
   d. fetchedCount += matchedCount
5. Pro gefiltertem Holiday:
   a. SELECT 1 FROM Holidays WHERE Date == holiday.Date → wenn vorhanden: skip (additive only — manuelle und sync-Eintraege werden NICHT überschrieben)
   b. INSERT mit Source=NagerSync, Description=localName, Audit-Felder mit CreatedBy="HolidaySync"
   c. insertedCount++
6. SaveChanges
7. Bei DryRun (WorkerSettings:SyncDryRun): nur loggen, nichts inserten
8. return HolidaySyncResult
```

### HttpClient-Konfiguration

```csharp
services.AddHttpClient<IHolidaySyncService, HolidaySyncService>(client =>
{
    client.BaseAddress = new Uri("https://date.nager.at/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
```

### Stammdaten-UI Holiday-Verwaltung

Bestehende Holiday-Verwaltungsseite (falls vorhanden) bekommt:
- Neue Spalte "Quelle" (Manual/Sync)
- Sync-Einträge sind read-only (kein Edit), löschen erlaubt (nächster Sync legt sie wieder an)
- Manual-Einträge bleiben voll editierbar

### Tests

| Test | Szenario |
|------|----------|
| `Run_SyncDisabled_NoOps` | Setting off → 0 API-Calls, 0 DB-Writes |
| `Run_FetchesCurrentAndForwardYears` | JahreVoraus=2 → 3 API-Calls (current + 2) |
| `Run_InsertsOnlyMissingDates` | DB hat 01.01., API liefert 01.01.+06.01. → nur 06.01. eingefügt |
| `Run_PreservesManualEntries` | Manual-Eintrag mit gleichem Datum → wird nicht überschrieben |
| `Run_WithRegion_FiltersCounties` | Region="AT-3" → nur Einträge mit counties enthält "AT-3" oder counties==null |
| `Run_ApiTimeout_LogsError` | Mock HttpClient timeout → Errors-Liste enthält Eintrag, kein Crash |
| `Run_DryRun_NoInserts` | DryRun=true → 0 Inserts |
| `Run_SetsSourceNagerSync` | inserted Holiday hat Source=NagerSync |

## UI

### Default-Kalender — neue Page `/BdeShiftCalendar` (BDE-Untermenü)

Controller `BdeShiftCalendarController`, Filter `[RequireMasterDataAccess]`. Layout: 7 Wochentag-Karten in Grid (Mo–So). Pro Karte Schicht-Liste mit Edit/Delete-Buttons + "Hinzufügen". Modal-Editor für Name (optional), StartTime, EndTime. Validierung serverseitig: `EndTime > StartTime`.

### Werkbank-Override im `Views/ProductionWorkplaces/Edit.cshtml`

Neuer Card-Abschnitt "Schichtplan (BDE)" unter den existierenden BDE-Settings. Toggle `BdeUseCustomShiftPlan`. Wenn aktiv: identischer 7-Tage-Editor wie Default-Kalender, eingebettet im Edit-Formular. JS toggelt Editor-Sichtbarkeit live ohne Reload. Wenn aktiv aber 0 Einträge: UI-Hinweis "Keine Schichten konfiguriert — keine automatische Pausierung."

### Terminal: AutoPaused-Markierung im Paused-Hint

Phase-2.2 zeigt pausierte Buchungen nach Operator-Scan. Bisher nur `Status == Paused`. Erweiterung: `Status == Paused OR Status == AutoPaused` (im `PausedBookings`-Endpoint anpassen). Frontend zeigt unterschiedliche Texte:
- `Paused` → "pausiert seit HH:MM"
- `AutoPaused` → "auto-pausiert seit HH:MM" + Hinweis "(Schichtende)"

Resume-Verhalten ist identisch (P7): `ResumeAsync` akzeptiert sowohl `Paused` als auch `AutoPaused`.

### Settings-UI

`Views/Settings/Index.cshtml`: BDE-Gruppe um `BdeSchichtkalenderAktiv` erweitern. Kein eigenes UI für Service-Settings (das passiert im Service-Settings-Bereich oder per appsettings.json).

## Service-Code-Anpassungen

### `BdeBookingService.ResumeAsync` (P7)

Bestehende Prüfung `parent.Status != BdeBookingStatus.Paused` erweitern auf:

```csharp
if (parent.Status != BdeBookingStatus.Paused && parent.Status != BdeBookingStatus.AutoPaused)
    return BdeBookingResult.Invalid("Ziel-Buchung ist nicht pausiert.");
```

`PausedBookings`-Endpoint (Terminal) ähnlich: `Status == Paused || Status == AutoPaused`.

## Tests-Übersicht

| Bereich | Anzahl |
|---------|--------|
| `BdeShiftCalendarServiceTests` | 9 |
| `BdeAutoPauseServiceTests` | 8 |
| `HolidaySyncServiceTests` | 8 |
| `BdeShiftCalendarControllerTests` | 6 |
| `ProductionWorkplacesControllerTests` Erweiterung | 3 |
| `BdeBookingServiceTests` Erweiterung (Resume akzeptiert AutoPaused) | 2 |
| `BdeTerminalControllerTests` Erweiterung (PausedBookings inkl. AutoPaused) | 2 |
| **Summe** | **~38 neue Tests** |

## Migration + Doku

- EF-Migration `AddBdeShiftCalendar` (1 neue Tabelle + 2 neue Spalten + CHECK-Update)
- `SQL/48_AddBdeShiftCalendar.sql` idempotent inkl. Settings-Seeds
- `SQL/00_FreshInstall.sql` aktualisiert
- `CLAUDE.md` AppSettings + Service-Konfig + Status-Enum aktualisiert
- `Views/Help/Index.cshtml`: 3 neue BDE-Unterabschnitte (Schichtkalender, Auto-Pause, Feiertags-Sync)
- `Views/Help/Changelog.cshtml`: v1.8.2 Phase-2.3-Block (kein Versions-Bump)
- `PROJECT_STATUS.md`: Phase 2.3 als abgeschlossen
- `IdealAkeWms/AppVersion.cs` + `IDEALAKEWMSService/AppVersion.cs`: Date auf Tag der Phase-2.3-Fertigstellung
- `docs/TESTSZENARIEN.md`: neuer Bereich "BDE Phase 2.3 — Schichtkalender + Auto-Pause" mit ~10–12 manuellen Testszenarien

## Manueller UI-Smoke-Test

1. Default-Kalender Mo–Fr 06–14, 14–22 anlegen → Schichten erscheinen, Edit/Delete funktioniert
2. Werkbank A1 Override-Toggle EIN, eigene Schichten anlegen → speichern → Werkbank-View zeigt eigenen Plan
3. Werkbank A2 Override AUS → Default gilt
4. Master-Toggle aktivieren, MA stempelt um 13:50 in Frühschicht → Service tickt nach 14:00 → Buchung Status=AutoPaused, EndedAt=14:00
5. MA scannt am Folge-Tag → Paused-Hint zeigt "auto-pausiert seit gestern 14:00", Klick Fortsetzen → neue Buchung läuft
6. Feiertag (z.B. 26.10.) auto-importiert via HolidaySync → Buchung an dem Tag wird NICHT auto-pausiert
7. Service-Settings Feiertags-Sync aktivieren, Region AT-3 wählen → nach erstem Sync sind die NÖ-Feiertage in der Holiday-Tabelle mit Source=NagerSync

## Offene Fragen

Keine — alle Design-Fragen wurden im Brainstorming geklärt.
