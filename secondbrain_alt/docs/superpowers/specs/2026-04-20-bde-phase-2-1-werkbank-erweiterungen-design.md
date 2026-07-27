# BDE Phase 2.1 — Werkbank-Erweiterungen

## Zusammenfassung

Zwei neue Felder auf `ProductionWorkplace`:
- `BdeAktiv` (bool, Default `false`) — gatet die Werkbank für das BDE-Modul
- `BdeDefaultArbeitsgang` (nullable string 200) — überschreibt das globale Setting `BdeDefaultArbeitsgang`, wenn gesetzt

Das globale Setting und der NurFA-Modus bleiben unverändert. Phase 2.1 ist ein reiner additiver Patch — kein Schema-Bruch, keine neuen Rollen, keine neuen Controller.

## Motivation

In Betrieben mit gemischtem Setup (einige Werkbänke mit BDE-Zeitbuchung, andere nur für Kommissionierung/Leitstand) muss BDE pro Werkbank ein-/ausschaltbar sein. Zusätzlich produzieren unterschiedliche Werkbänke unterschiedliche Standard-Tätigkeiten (z.B. "BOHREN", "SCHWEISSEN", "MONTAGE"). Der aktuelle globale Default-AG passt nicht für alle Werkbänke.

## Nicht in Scope

- Keine Änderung an Rollen oder Zugriffsrechten
- Keine Änderung am globalen Setting `BdeDefaultArbeitsgang`-Verhalten selbst
- Kein Schicht-Kalender / Auto-Pause (→ Phase 2.3)
- Keine Mehrfachanmeldung (→ Phase 2.2)
- Keine neuen Cockpit-Varianten (→ Phase 2.4)
- Kein Versions-Bump (bleibt v1.8.2; Release-Cut erfolgt erst wenn mehrere Phase-2-Pakete gebündelt werden)

## Schema-Änderung

### Model

```csharp
public class ProductionWorkplace : AuditableEntity
{
    // ... bestehende Felder ...

    [Display(Name = "BDE aktiv")]
    public bool BdeAktiv { get; set; } = false;

    [StringLength(200)]
    [Display(Name = "Default-Arbeitsgang (BDE)")]
    public string? BdeDefaultArbeitsgang { get; set; }
}
```

### Migration

EF-Migration `AddBdeWerkbankSettings` via `dotnet ef migrations add`.

SQL-Script `SQL/43_AddBdeWerkbankSettings.sql` ist idempotent:
- `IF COL_LENGTH('ProductionWorkplaces', 'BdeAktiv') IS NULL` → `ALTER TABLE ADD BdeAktiv bit NOT NULL CONSTRAINT DF_ProductionWorkplaces_BdeAktiv DEFAULT 0`
- `IF COL_LENGTH('ProductionWorkplaces', 'BdeDefaultArbeitsgang') IS NULL` → `ALTER TABLE ADD BdeDefaultArbeitsgang nvarchar(200) NULL`
- Separater Batch mit `GO` für `__EFMigrationsHistory`-Insert

`SQL/00_FreshInstall.sql`: Spalten-Definitionen im `CREATE TABLE ProductionWorkplaces`-Block ergänzen.

Bestehende Zeilen bekommen automatisch `BdeAktiv = 0` über das `DEFAULT`-Constraint.

### Keine Indizes

Das Feld `BdeAktiv` bekommt keinen separaten Index, da die Tabelle klein ist (Werkbänke sind Stammdaten, typisch < 100 Zeilen).

## Default-AG Resolution

Neues Regel im `BdeDefaultWorkOperationService` (bestehende Klasse erweitern):

```csharp
private async Task<string> ResolveDefaultArbeitsgangAsync(int workplaceId)
{
    var workplace = await _workplaceRepo.GetByIdAsync(workplaceId);
    if (!string.IsNullOrWhiteSpace(workplace?.BdeDefaultArbeitsgang))
        return workplace.BdeDefaultArbeitsgang.Trim();

    var global = await _appSettings.GetValueAsync("BdeDefaultArbeitsgang");
    if (!string.IsNullOrWhiteSpace(global))
        return global.Trim();

    throw new InvalidOperationException(
        "Default-Arbeitsgang ist weder auf der Werkbank noch global konfiguriert.");
}
```

**Wichtig:** `IsNullOrWhiteSpace` (nicht nur `?? null`), damit Leerstring/Whitespace sauber auf Global zurückfällt. `Trim()` entfernt versehentliche Leerzeichen am Rand.

Die Methode wird in `FindOrCreateDefaultAsync(int productionOrderId, int workplaceId)` aufgerufen, die den AG-Namen heute aus dem globalen Setting zieht.

## `BdeAktiv`-Filter

### Zentrale Repository-Methode

Neue Methode auf `IProductionWorkplaceRepository` / `ProductionWorkplaceRepository`:

```csharp
Task<List<ProductionWorkplace>> GetBdeActiveAsync();
// Implementation:
// return await _ctx.ProductionWorkplaces.Where(w => w.BdeAktiv).OrderBy(w => w.Name).ToListAsync();
```

Dedizierte Methode (keine IQueryable-Extension), damit die Filter-Absicht im Code-Grep sichtbar ist und Aufrufer nicht versehentlich `GetAllAsync()` verwenden, wo gefiltert werden muss.

### Query-Stellen (betroffen)

| Stelle | Datei | Änderung |
|--------|-------|----------|
| Cockpit-Aktive-Kacheln | `BdeApiController.GetActiveCockpitAsync` | Join-Filter `where w.BdeAktiv` |
| Available-Operations für Terminal | `BdeApiController.GetAvailableOperationsAsync` | Wenn angefragte Werkbank `BdeAktiv = false` → leere Response (`productive: []`, `unplanned: []`); Terminal zeigt "keine Aufträge verfügbar" |
| Terminal-Edit Werkbank-Dropdown | `BdeMasterDataController` / `EditTerminal.cshtml` | `GetBdeActiveAsync()` statt `GetAllAsync()` |
| Buchungsübersicht Werkbank-Filter-Dropdown | `BdeBookingsController.Index` | `GetBdeActiveAsync()` für Dropdown-Optionen |
| Buchungsübersicht Ergebnis-Liste | *keine Änderung* | Historische Buchungen bleiben sichtbar |

### Query-Stellen (nicht betroffen)

Folgende Controller verwenden `ProductionWorkplace` für WMS-Stammdaten und bleiben unverändert:
- `TrackingController`, `ProductionOrdersController`, `ProductionWorkplacesController`
- `OseonProductionOrderRepository`, `ProductionOrderRepository`
- Picking- und Stückliste-Views

### Booking-Validation im Service

`BdeBookingService` muss bei allen drei Start-Methoden prüfen, ob die Ziel-Werkbank BDE-aktiv ist — sonst können Buchungen via direkte API-Calls oder veraltete Terminal-Zuweisungen auf inaktiven Werkbänken landen.

```csharp
// In StartPlannedAsync, StartActivityAsync, ResumeAsync (vor dem eigentlichen Insert):
var workplace = await _ctx.ProductionWorkplaces.FindAsync(workplaceId);
if (workplace == null || !workplace.BdeAktiv)
{
    _logger.LogWarning(
        "BDE-Buchung abgewiesen: Werkbank {WorkplaceId} ist nicht BDE-aktiv (Operator {OperatorId}).",
        workplaceId, operatorId);
    return BdeBookingResult.Invalid("Werkbank ist nicht für BDE aktiviert.");
}
```

`BdeBookingResult.Invalid` existiert bereits; die neue Nutzung folgt dem bestehenden Pattern.

## UI-Änderungen

### Werkbank-Edit (`Views/ProductionWorkplaces/Edit.cshtml`)

Neuer Accordion-Block "BDE-Einstellungen" unterhalb der bestehenden Felder:

```html
<div class="card mt-3">
  <div class="card-header">BDE-Einstellungen</div>
  <div class="card-body">
    <div class="form-check form-switch mb-3">
      <input asp-for="BdeAktiv" class="form-check-input" role="switch" />
      <label asp-for="BdeAktiv" class="form-check-label"></label>
      <div class="form-text">Werkbank erscheint in BDE-Cockpit, Terminal und Buchungsübersicht.</div>
    </div>
    <div class="mb-3">
      <label asp-for="BdeDefaultArbeitsgang" class="form-label"></label>
      <input asp-for="BdeDefaultArbeitsgang" class="form-control" placeholder="z.B. PRODUKTION (leer = globales Setting verwenden)" />
      <div class="form-text">
        Überschreibt das globale Setting. Aktuell global: <strong>@ViewBag.GlobalDefaultArbeitsgang</strong>
      </div>
      <span asp-validation-for="BdeDefaultArbeitsgang" class="text-danger"></span>
    </div>
  </div>
</div>
```

`ProductionWorkplacesController.Edit (GET)` lädt den globalen Wert in `ViewBag.GlobalDefaultArbeitsgang` (für den Hinweis-Text).

### Werkbank-Liste (`Views/ProductionWorkplaces/Index.cshtml`)

Neue Spalte "BDE" mit Badge:
- `BdeAktiv = true` → `<span class="badge bg-success">Aktiv</span>`
- `BdeAktiv = false` → `<span class="badge bg-secondary">Inaktiv</span>`

Spalten-Definition:
- `<th data-col-key="BdeAktiv">BDE</th>`
- Registrierung in `ColumnDefinitions.cs` unter der bestehenden Workplace-Gruppe, Signatur entsprechend den bereits dort definierten Spalten (Key `"BdeAktiv"`, Anzeigename `"BDE"`, Default-Sichtbarkeit wie die anderen Spalten dieser Tabelle)

### Werkbank-Create (`Views/ProductionWorkplaces/Create.cshtml`)

Gleiche BDE-Felder wie Edit (Toggle + Textfeld + Hinweis). Default beim Create: `BdeAktiv = false`, leerer Default-AG.

### Settings-Seite

**Keine Änderung.** Nur ein optionaler Hinweis-Text unter dem globalen Feld: "Kann pro Werkbank überschrieben werden (siehe Werkbänke-Stammdaten)".

### BDE-Terminal / Cockpit / Buchungsübersicht

Keine View-Änderungen. Nur die zugrundeliegenden Queries filtern jetzt über `GetBdeActiveAsync()`.

### Fehlermeldung-UX

Wenn ein Terminal einer inaktivierten Werkbank zugewiesen ist und ein Scan eintrifft, gibt der Service die Meldung `"Werkbank ist nicht für BDE aktiviert."` zurück. Das Terminal-JS zeigt diese Meldung im Fehler-Modal unverändert an (bestehende Error-Handling-Kette in `bde-terminal.js`).

## Migration bestehender Daten

Alle Werkbänke starten nach dem Deploy auf `BdeAktiv = false`. Aktive Terminal-Zuweisungen in der Produktionsumgebung werfen daher beim ersten Scan die neue Fehlermeldung "Werkbank ist nicht für BDE aktiviert" — das ist Absicht. Ein Nutzer mit `masterdata`- oder `admin`-Rolle muss explizit jede BDE-Werkbank aktivieren:
1. Werkbänke-Stammdaten öffnen
2. Pro Werkbank: Edit → "BDE aktiv" togglen → Speichern

Diese Migrations-Notwendigkeit ist in den Release-Notes zu dokumentieren, sobald ein Release-Cut erfolgt, der Phase 2.1 enthält.

**Tests:** Die bestehenden BDE-Tests werden in diesem Change selbst angepasst (Test-Werkbänke explizit mit `BdeAktiv = true` anlegen) — sie "brechen" also nicht im Sinne eines Regressions, sondern werden im selben Commit wie die Validation synchron gehalten.

## Tests

### Neue Tests

| Test | Was wird geprüft |
|------|------------------|
| `ProductionWorkplaceTests.BdeAktiv_DefaultsToFalse` | Model-Default ist `false` |
| `ProductionWorkplaceTests.BdeDefaultArbeitsgang_AcceptsNullAndMax200` | Null, Leerstring, 200-Zeichen-String akzeptiert |
| `BdeDefaultWorkOperationServiceTests.Resolve_WerkbankWins` | Werkbank "BOHREN", Global "PRODUKTION" → "BOHREN" |
| `BdeDefaultWorkOperationServiceTests.Resolve_FallsBackToGlobal` | Werkbank null, Global "PRODUKTION" → "PRODUKTION" |
| `BdeDefaultWorkOperationServiceTests.Resolve_WhitespaceFallsBackToGlobal` | Werkbank "   " → Global |
| `BdeDefaultWorkOperationServiceTests.Resolve_BothEmptyThrows` | Beide leer → `InvalidOperationException` |
| `BdeDefaultWorkOperationServiceTests.Resolve_TrimsResult` | Werkbank "  BOHREN  " → "BOHREN" |
| `BdeBookingServiceTests.Start_RejectsInactiveWorkplace` | `BdeAktiv = false` → `BdeBookingResult.Invalid` mit Message |
| `BdeBookingServiceTests.Start_LogsWarningOnInactiveWorkplace` | Log-Warning wird emittiert (Mock-Logger) |
| `ProductionWorkplacesControllerTests.Edit_PersistsBdeFields` | POST mit `BdeAktiv=true` + Default-AG → beide Felder gespeichert, Audit gesetzt |
| `ProductionWorkplacesControllerTests.Edit_ClearsDefaultArbeitsgang` | Leerer Wert wird als `null` gespeichert |
| `BdeApiControllerTests.GetActiveCockpit_OnlyBdeActive` | Kacheln enthalten nur `BdeAktiv=true`-Werkbänke |

### Bestehende Tests anpassen

Alle `BdeBookingServiceTests` die eine Test-Werkbank anlegen, müssen `BdeAktiv = true` explizit setzen — sonst kippen sie durch das neue Validation-Gate. Betroffen sind alle Tests in `IdealAkeWms.Tests/Services/BdeBookingServiceTests.cs`. Gleiches gilt für alle Tests in `BdeApiControllerTests`, `BdeTerminalControllerTests`.

Test-Factory-Helfer (falls vorhanden oder neu erstellen): `TestDataHelper.CreateBdeActiveWorkplace(ctx, name)` — setzt `BdeAktiv = true` standardmäßig.

### Manueller UI-Test

Nach der Implementierung:
1. Werkbank anlegen, `BdeAktiv = false` lassen → sollte nicht im Cockpit/Terminal erscheinen
2. Werkbank aktivieren → erscheint im Cockpit, Terminal-Dropdowns, Buchungsübersicht-Filter
3. Werkbank-Default-AG setzen (im NurFA-Modus) → auto-erstellter AG hat diesen Namen
4. Default-AG leeren → fällt auf globales Setting zurück
5. Werkbank deaktivieren mit zugewiesenem Terminal → Terminal wirft bei Scan Fehler "Werkbank ist nicht für BDE aktiviert"

## Dokumentation

### PROJECT_STATUS.md

Phase 2.1 als abgeschlossen eintragen, sobald Implementierung steht.

### Views/Help/Index.cshtml

Neuer Unterabschnitt unter "BDE" → "Werkbank für BDE konfigurieren":
1. Werkbank öffnen (Menü Stammdaten → Werkbänke)
2. "BDE aktiv" einschalten
3. Optional: Default-Arbeitsgang eintragen, wenn diese Werkbank einen abweichenden Standard-AG hat
4. Speichern

Plus Troubleshooting-Eintrag: *"Fehler 'Werkbank ist nicht für BDE aktiviert' beim Scan → Werkbank-Stammdaten öffnen und 'BDE aktiv' einschalten."*

### CLAUDE.md

Keine Aktualisierung zwingend nötig (kein neues Rollenkonzept, keine neue Middleware). Optional: unter "Bekannte Fallstricke" Eintrag ergänzen:
- **Werkbank BdeAktiv-Gate**: BdeBookingService verweigert Buchungen auf inaktiven Werkbänken. Tests müssen Test-Werkbänke explizit mit `BdeAktiv = true` anlegen.

## Betroffene Dateien (Überblick)

| Bereich | Dateien |
|---------|---------|
| Model | `Models/ProductionWorkplace.cs` |
| Migration | EF-Migration + `SQL/43_AddBdeWerkbankSettings.sql` + `SQL/00_FreshInstall.sql` |
| Repository | `Data/Repositories/IProductionWorkplaceRepository.cs`, `ProductionWorkplaceRepository.cs` |
| Services | `Services/BdeDefaultWorkOperationService.cs` (Resolution-Logik), `Services/BdeBookingService.cs` (Validation) |
| Controllers | `Controllers/BdeApiController.cs`, `Controllers/BdeBookingsController.cs`, `Controllers/BdeMasterDataController.cs`, `Controllers/ProductionWorkplacesController.cs` |
| ViewModels | `Models/ViewModels/ProductionWorkplaceEditViewModel.cs` |
| Views | `Views/ProductionWorkplaces/Edit.cshtml`, `Create.cshtml`, `Index.cshtml` |
| Column-Config | `Models/ViewModels/ColumnDefinitions.cs` |
| Hilfe | `Views/Help/Index.cshtml` |
| Tests | Neue Tests in `ProductionWorkplaceTests`, `BdeDefaultWorkOperationServiceTests`, `BdeBookingServiceTests`, `ProductionWorkplacesControllerTests`, `BdeApiControllerTests`; Anpassung aller bestehenden BDE-Service-Tests |

## Offene Fragen

Keine — alle Design-Fragen wurden im Brainstorming geklärt.
