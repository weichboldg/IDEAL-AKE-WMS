# AKEBDELight - Analyse, Features & Verbesserungsvorschläge

## 1. Feature-Übersicht

### 1.1 Lagerverwaltung (Stock Management)
| Feature | Beschreibung | Dateien |
|---------|-------------|---------|
| Einbuchung | Ware ins Lager buchen (Artikel + Lagerplatz + Menge) | StockMovementsController, Inbound.cshtml |
| Ausbuchung | Ware aus dem Lager entnehmen | StockMovementsController, Outbound.cshtml |
| Umbuchung | Ware zwischen Lagerplätzen verschieben | StockMovementsController, Transfer.cshtml |
| Bestandsübersicht | Aktueller Bestand pro Artikel/Lagerplatz, mit Warn-/Kritisch-Schwellen | StockOverviewController, Index.cshtml |
| Bewegungshistorie | Alle Bewegungen mit Filtern (Datum, Artikel, Typ, User, WA) | StockMovementsController, Index.cshtml |

### 1.2 Werkstattaufträge (Production Orders)
| Feature | Beschreibung | Dateien |
|---------|-------------|---------|
| WA-Übersicht | Liste aller Aufträge, Filter, Terminberechnung (Geschäftstage) | ProductionOrdersController.Index, Index.cshtml |
| Terminberechnung | Automatische Berechnung: Kommissionier-, Baugruppen-, Beschichtungstermin | BusinessDayService, HolidayRepository |
| Status-Verwaltung | WA als "Erledigt" markieren | ProductionOrdersController.ToggleDone |

### 1.3 Kommissionierung (Picking/BOM)
| Feature | Beschreibung | Dateien |
|---------|-------------|---------|
| Stückliste (BOM) | Anzeige der Stückliste aus externer AKE-DB VIEW | BomRepository, Bom.cshtml |
| Baumstruktur | Mehrstufiger klappbarer Baum (Position-basiert) | Bom.cshtml (JS), NaturalPositionComparer |
| Picking | Checkbox zum Markieren kommissionierter Teile | PickingRepository, Bom.cshtml |
| Baugruppen-Picking | Baugruppe als ganzes pickbar (Teile darunter ignoriert) | PickingRepository.TogglePickedAsync |
| Auto-Suggest Lagerplatz | Automatischer Vorschlag: Lagerplatz mit höchstem Bestand oder "NAN" | ProductionOrdersController.Bom |
| Umbuchung gepickter Teile | Gepickte Teile in Ziel-Lagerplatz umbuchen | ProductionOrdersController.TransferPicked |
| Picking-Status | Status nach Umbuchung: "teilkommissioniert" oder "abgeschlossen" | ProductionOrdersController.SetPickingStatus |
| Foto-Upload | Fotos zur Kommissionierung hochladen (JPEG-Kompression) | photo-upload.js, UploadPhoto/GetPhotos/DeletePhoto |
| Stückliste drucken | Vollständige Stückliste als Print-View | PrintBom.cshtml |
| Kommissionierliste drucken | Nur gepickte Artikel als Print-View | PrintPicking.cshtml |

### 1.4 Barcode/QR-Scanner
| Feature | Beschreibung | Dateien |
|---------|-------------|---------|
| Kamera-Scanner | HTML5-basierter QR/Barcode-Scan per Kamera | barcode-scanner.js, html5-qrcode |
| Bild-Upload Fallback | QR-Code aus Foto scannen (wenn keine Kamera) | barcode-scanner.js |
| Artikel-Scan | QR-Code → Select2-Dropdown füllen | barcode-scanner.js |
| Lagerplatz-Scan | Barcode → Lagerplatz-Dropdown auswählen | barcode-scanner.js |

### 1.5 Stammdaten
| Feature | Beschreibung | Dateien |
|---------|-------------|---------|
| Artikel | CRUD, paginiert, Suche, Select2-AJAX-Suche | ArticlesController, ArticlesApiController |
| Lagerplätze | CRUD, Barcode-Etiketten drucken | StorageLocationsController, PrintLabels.cshtml |
| Benutzer | CRUD, optionales Passwort (PBKDF2-SHA256) | UsersController, PasswordService |
| Arbeitsplätze | CRUD, Benutzer-Zuordnung, Default-Drucker | WorkstationsController |
| Feiertage | Manuell + automatischer Import (Nager API, AT) | SettingsController, HolidayImportService |
| App-Settings | Key-Value Konfiguration (Schwellwerte, Tage) | SettingsController, AppSettingRepository |

### 1.6 Authentifizierung
| Feature | Beschreibung | Dateien |
|---------|-------------|---------|
| Windows Auth | Negotiate/NTLM Authentifizierung | Program.cs |
| App-Login | Benutzer-Dropdown + optionales Passwort | AccountController, Login.cshtml |
| Session | 8h idle timeout, Session-basierte Autorisierung | Program.cs (Middleware) |

---

## 2. Architektur-Bewertung

### 2.1 Stärken
- **Saubere Schichtentrennung**: Controller → Repository → DbContext
- **Dependency Injection**: Alle Services und Repositories per DI registriert
- **Audit-Trail**: Alle Entitäten haben Created/Modified Felder
- **Parametrisierte Queries**: Kein SQL-Injection-Risiko (SqlQueryRaw mit Parametern)
- **Windows Auth**: Sicheres Single-Sign-On im Intranet

### 2.2 Schwächen & Risiken

---

## 3. Verbesserungsvorschläge

### 3.1 KRITISCH - Datenintegrität & Stabilität

#### 3.1.1 Fehlende Transaktionen bei mehrstufigen Operationen
**Problem**: `TransferPicked` erstellt mehrere StockMovements in einer Schleife. Wenn einer fehlschlägt, sind vorherige bereits gespeichert → inkonsistenter Bestand.
**Lösung**: `IDbContextTransaction` verwenden.
```csharp
// IST:
foreach (var item in pickedItems) {
    await _stockMovementRepository.AddAsync(movement); // jedes einzeln SaveChanges!
}

// SOLL:
using var transaction = await _context.Database.BeginTransactionAsync();
try {
    foreach (var item in pickedItems) {
        _context.StockMovements.Add(movement);
    }
    await _context.SaveChangesAsync();
    await _pickingRepository.MarkAsTransferredAsync(...);
    await transaction.CommitAsync();
} catch {
    await transaction.RollbackAsync();
    throw;
}
```

#### 3.1.2 Repository.AddAsync ruft SaveChanges pro Entity auf
**Problem**: `Repository<T>.AddAsync()` ruft `SaveChangesAsync()` nach JEDEM Add auf. Bei Batch-Operationen (z.B. `InitializePickingAsync`) wird korrekt erst am Ende gespeichert, aber bei `TransferPicked` wird pro Movement gespeichert.
**Lösung**: Batch-fähige Methoden im Repository, oder ein Unit-of-Work Pattern.

#### 3.1.3 Keine Concurrency-Behandlung
**Problem**: Zwei Benutzer können gleichzeitig denselben Picking-Item toggeln oder denselben Bestand umbuchen → Race Conditions.
**Lösung**: Optimistic Concurrency mit `[ConcurrencyCheck]` oder `RowVersion` auf kritischen Entitäten (StockMovement, PickingItem).

#### 3.1.4 Keine negative Bestands-Prüfung
**Problem**: Bei Ausbuchung/Umbuchung wird nicht geprüft, ob genug Bestand vorhanden ist → negativer Bestand möglich.
**Lösung**: Vor jeder Ausbuchung/Umbuchung aktuellen Bestand prüfen und ablehnen wenn nicht ausreichend.

### 3.2 HOCH - Performance

#### 3.2.1 Bestandsberechnung ist teuer (N+1 ähnlich)
**Problem**: `GetCurrentStockAsync` und `GetStockByArticleNumbersAsync` laufen über ALLE StockMovements und aggregieren in-memory (GroupBy nach Include). Bei wachsender Datenmenge wird dies langsam.
**Lösung**:
- **Option A**: Materialisierte Bestandstabelle (`CurrentStock`) die bei jeder Bewegung aktualisiert wird
- **Option B**: SQL-View für den aktuellen Bestand auf der DB
- **Option C**: Caching mit kurzer TTL (30s) für Bestandsabfragen

#### 3.2.2 BOM-Abfrage ohne Caching
**Problem**: Jeder Aufruf von `Bom(id)` holt die BOM aus der externen DB. Bei häufiger Nutzung hohe Last auf die AKE-Datenbank.
**Lösung**: In-Memory Cache mit `IMemoryCache` (5-10 Min TTL), da sich BOMs selten ändern.

#### 3.2.3 Bewegungshistorie ohne Paginierung
**Problem**: `GetMovementHistoryAsync` lädt ALLE Bewegungen die den Filtern entsprechen. Bei langem Betrieb können das Tausende Einträge sein.
**Lösung**: Server-seitige Paginierung einführen (Take/Skip).

### 3.3 MITTEL - Robustheit & Fehlerbehandlung

#### 3.3.1 Keine globale Fehlerbehandlung in AJAX-Calls
**Problem**: JavaScript-Seitig werden Fehler nur mit `alert()` angezeigt. Kein strukturiertes Error-Handling.
**Lösung**: Toast-Notifications (Bootstrap Toast), zentrale AJAX-Error-Handler-Funktion.

#### 3.3.2 Controller-Aktionen ohne try-catch
**Problem**: POST-Aktionen wie `TogglePicked`, `TransferPicked` haben kein try-catch. DB-Fehler resultieren in 500er-Responses.
**Lösung**: Globaler Exception-Filter oder try-catch in kritischen Aktionen mit strukturiertem Fehler-Response.

#### 3.3.3 Foto-Löschung ohne Bestätigungs-Schutz
**Problem**: `DeletePhoto` prüft nur auf Path-Traversal, aber nicht ob das Foto zum aktuellen WA gehört.
**Lösung**: Prüfung ob Dateiname mit der WA-Nummer beginnt.

#### 3.3.4 EnsureCreated() statt Migrations
**Problem**: `db.Database.EnsureCreated()` erstellt die DB nur wenn sie nicht existiert. Schema-Änderungen (neue Spalten) werden NICHT angewendet → SQL-Skripte nötig.
**Lösung**: Auf EF Core Migrations umsteigen (`dotnet ef migrations`). Dann werden Schema-Änderungen automatisch angewendet.

### 3.4 NIEDRIG - Code-Qualität & Wartbarkeit

#### 3.4.1 Duplizierter Code in StockMovementRepository
**Problem**: Die Bestandsberechnungs-Logik (Einbuchung + Umbuchung-Ziel vs. Umbuchung-Quelle) ist in `GetCurrentStockAsync` und `GetStockByArticleNumbersAsync` dupliziert.
**Lösung**: Gemeinsame private Methode extrahieren.

#### 3.4.2 ProductionOrdersController ist zu groß (~450 Zeilen)
**Problem**: Der Controller enthält Picking, Fotos, Drucken, BOM-Logik - alles zusammen.
**Lösung**: Aufteilen in separate Controller oder die Business-Logik in Services verschieben:
- `PickingService` für Picking-Logik
- `PhotoService` für Foto-Verwaltung

#### 3.4.3 Magic Strings
**Problem**: Session-Keys ("AppUserId", "AppUserName"), Status-Werte ("teilkommissioniert", "abgeschlossen"), Lagerplatz-Codes ("NAN") sind hardcoded.
**Lösung**: Konstanten-Klasse (`SessionKeys`, `PickingStatusValues`).

#### 3.4.4 DateTime.Now statt UTC
**Problem**: Überall wird `DateTime.Now` verwendet. Bei Serverwechsel oder Zeitzone-Änderung inkonsistent.
**Lösung**: `DateTime.UtcNow` oder `DateTimeOffset.UtcNow` verwenden und in der Anzeige lokalisieren. (Geringes Risiko da Intranet-Anwendung in einer Zeitzone)

---

## 4. Test-Strategie

### 4.1 Projekt-Setup

Neues xUnit-Testprojekt: `AKEBDELight.Tests`
```
AKEBDELight.Tests/
├── Unit/
│   ├── Services/
│   │   ├── BusinessDayServiceTests.cs
│   │   ├── NaturalPositionComparerTests.cs
│   │   └── PasswordServiceTests.cs
│   ├── Repositories/
│   │   ├── StockMovementRepositoryTests.cs
│   │   ├── PickingRepositoryTests.cs
│   │   └── ArticleRepositoryTests.cs
│   └── Controllers/
│       ├── ProductionOrdersControllerTests.cs
│       └── StockMovementsControllerTests.cs
├── Integration/
│   ├── DatabaseFixture.cs
│   ├── StockMovementIntegrationTests.cs
│   ├── PickingWorkflowTests.cs
│   └── BomRepositoryTests.cs
└── AKEBDELight.Tests.csproj
```

### 4.2 Unit Tests - Priorität nach Risiko

#### Priorität 1: Geschäftslogik (kein DB-Zugriff nötig)

**NaturalPositionComparerTests** - Sortierung
```
- Einfache Nummern: 1, 2, 10, 11 (nicht 1, 10, 11, 2)
- Mehrstufig: 1, 1.1, 1.1.1, 1.2, 2, 10, 10.1
- Null-Handling: null vs "1", null vs null
- Leere Strings: "" vs "1"
- Gleiche Werte: "5" vs "5"
```

**BusinessDayServiceTests** - Terminberechnung
```
- Normaler Fall: 4 Arbeitstage zurück
- Wochenende überspringen: Freitag - 1 = Donnerstag, Montag - 1 = Freitag
- Feiertag überspringen: Tag vor Feiertag
- Kombination: Wochenende + Feiertag
- 0 Tage zurück = selber Tag
- Negativer Fall: Was passiert bei negativen Tagen?
```

**PasswordServiceTests** - Passwort-Hashing
```
- Hash erzeugen und verifizieren (Roundtrip)
- Falsches Passwort wird abgelehnt
- Verschiedene Passwörter erzeugen verschiedene Hashes (Salt)
- Leeres Passwort
```

#### Priorität 2: Repository-Tests (In-Memory DB)

**StockMovementRepositoryTests** - Bestandsberechnung
```
- Einbuchung erhöht Bestand
- Ausbuchung verringert Bestand
- Umbuchung: Quelle verringert, Ziel erhöht
- Mehrere Bewegungen: korrekter Saldo
- Filter nach Artikel
- Filter nach Lagerplatz
- Filter nach Mindest-/Maximal-Menge
- Leere DB: leere Ergebnisse
- GetStockByArticleNumbers: korrekte Gruppierung
```

**PickingRepositoryTests** - Kommissionierung
```
- InitializePickingAsync: erstellt Items aus BOM
- InitializePickingAsync: ignoriert wenn schon Items existieren
- TogglePickedAsync: Picked → Unpicked (und zurück)
- TogglePickedAsync: setzt Lagerplatz, Zeitstempel, User
- GetPickedNotTransferredAsync: nur gepickte, nicht umgebuchte
- MarkAsTransferredAsync: setzt Flag + Zeitstempel
```

#### Priorität 3: Controller-Tests (Mocked Dependencies)

**ProductionOrdersControllerTests**
```
- Bom: Lädt BOM + Picking + Stock korrekt zusammen
- Bom: TreeLevel wird korrekt aus Position berechnet
- Bom: IsBaugruppe wird korrekt erkannt
- Bom: Filter filtert nach Ressourcenummer, Bezeichnung, Baugruppe
- TransferPicked: Erstellt StockMovements für alle gepickten Items
- TransferPicked: Gibt Fehler wenn keine gepickten Items
- TransferPicked: Überspringt Items ohne Quell-Lagerplatz
- TogglePicked: Ruft Repository korrekt auf
- PrintBom: Alle BOM-Items, nicht nur gepickte
- PrintPicking: Nur gepickte Items
```

### 4.3 Integrations-Tests

**StockMovementIntegrationTests** (SQL Server InMemory oder TestContainers)
```
- Vollständiger Workflow: Einbuchung → Bestand prüfen → Ausbuchung → Bestand prüfen
- Umbuchung: Quell-Bestand sinkt, Ziel-Bestand steigt
- Concurrent Access: Zwei gleichzeitige Umbuchungen
```

**PickingWorkflowTests**
```
- Vollständiger Workflow: BOM laden → Picking initialisieren → Items picken → Umbuchen
- Picking-Status wird korrekt gesetzt
- Umgebuchte Items können nicht erneut umgebucht werden
```

### 4.4 Empfohlene NuGet-Pakete für Tests
```xml
<PackageReference Include="xunit" Version="2.*" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
<PackageReference Include="Moq" Version="4.*" />
<PackageReference Include="FluentAssertions" Version="7.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.*" />
```

### 4.5 CI/CD Integration
```yaml
# Beispiel GitHub Actions
- dotnet test AKEBDELight.Tests --logger "trx" --results-directory TestResults
- Testberichte als Artefakte hochladen
- Build-Gate: Tests müssen bestehen vor Deployment
```

---

## 5. Priorisierte Umsetzungsreihenfolge

| # | Maßnahme | Aufwand | Risiko-Reduktion |
|---|----------|---------|-----------------|
| 1 | **Test-Projekt aufsetzen** + NaturalPositionComparer + BusinessDayService Tests | Klein | Grundlage für alles |
| 2 | **Transaktionen in TransferPicked** | Klein | Verhindert inkonsistente Bestände |
| 3 | **Repository-Tests** (StockMovement, Picking) mit InMemory-DB | Mittel | Deckt Kernlogik ab |
| 4 | **Concurrency-Check** auf PickingItem + StockMovement | Klein | Verhindert Race Conditions |
| 5 | **Negative-Bestands-Prüfung** bei Ausbuchung/Umbuchung | Klein | Verhindert Dateninkonsistenz |
| 6 | **Controller-Tests** mit Mocks | Mittel | Sichert Geschäftslogik |
| 7 | **BOM-Caching** mit IMemoryCache | Klein | Reduziert DB-Last |
| 8 | **Paginierung** für Bewegungshistorie | Mittel | Performance bei großem Datenvolumen |
| 9 | **Bestandstabelle materialisieren** | Groß | Langfristige Performance |
| 10 | **Controller aufteilen** (PickingService, PhotoService) | Mittel | Wartbarkeit |
