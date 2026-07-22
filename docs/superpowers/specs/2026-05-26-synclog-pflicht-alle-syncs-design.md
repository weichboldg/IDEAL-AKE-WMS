# SyncLog-Pflicht fuer alle Sync-Services — Design

**Datum:** 2026-05-26
**Status:** Spec, in Brainstorming abgeschlossen, wartet auf User-Review vor `writing-plans`
**Scope:** Web + Service (`IdealAkeWms/`, `IDEALAKEWMSService/`)

---

## 1. Problemstellung

Heute schreiben **2 von 8** echten Sync-Services in die `SyncLogs`-Tabelle ([LagerplatzSyncService](IDEALAKEWMSService/Services/LagerplatzSyncService.cs), [LagerbestandSyncService](IDEALAKEWMSService/Services/LagerbestandSyncService.cs)). Alle anderen Syncs loggen ausschliesslich nach Serilog (Datei-Logs). Die UI [SyncLog/Index](IdealAkeWms/Views/SyncLog/Index.cshtml) ist damit nur ein partielles Audit — fuer 75% der Sync-Aktivitaet komplett blind.

Folgen:
- Operations-Team hat keine zentrale, DB-gestuetzte Sicht "wann lief was, mit welchem Ergebnis".
- Probleme in `BomCacheSyncService`, `OseonSyncService`, `EnaioDmsSyncService`, `HolidaySyncService`, `CoatingDetectionService`, `SageImportService` (Production Orders + Articles) muessen ueber Datei-Logs gesucht werden.
- Wiederkehrende Fragen ("Lief der Sage-Import heute?", "Warum sind die Werkstattauftrags-DMS-Links nicht da?") brauchen Server-Zugriff.

## 2. Ziele

1. **Alle 8 echten Syncs** (= 9 logische Service-Namen, weil `SageImportService` zwei Runs pro Tick fuehrt: `ProductionOrder` + `Article`) schreiben einheitlich ins `SyncLogs`-Schema: Start (Info) + relevante Events (Info/Warning/Error) waehrend des Runs + Ende (Info bei Erfolg / Error bei Failure) mit Counts.
2. **Einheitlicher Aufruf-Mechanismus** (`ISyncLogger` + `ISyncRun`) statt direkten Repo-Aufrufen. Reduziert Boilerplate, zentralisiert Try/Catch + Fallback.
3. **Robustheit:** SyncLog-Schreibfehler duerfen Sync-Runs nie crashen. Diagnose-Logs duerfen nicht in Sync-Transaktionen mitrollen.
4. **Migration der 2 bestehenden Services** auf das neue Pattern — kein gespaltenes Codebase.
5. **Out-of-Scope:** Retention/Cleanup-Job (eigene spaetere Spec), Activity-Log fuer Non-Sync-Aktivitaeten (eigene spaetere Spec), UI-Aenderungen ueber `KnownServices`-Pflege hinaus.

## 3. Scope: Services

| Service | Service-Name (Konstante) | Status heute | Aktion |
|---|---|---|---|
| [LagerplatzSyncService](IDEALAKEWMSService/Services/LagerplatzSyncService.cs) | `Lagerplatz` | nutzt SyncLog (direkter Repo-Call) | **migrieren** auf `ISyncLogger` |
| [LagerbestandSyncService](IDEALAKEWMSService/Services/LagerbestandSyncService.cs) | `Lagerbestand` | nutzt SyncLog (direkter Repo-Call) | **migrieren** auf `ISyncLogger` |
| BomCacheSyncService | `BomCache` | nur Serilog | **integrieren** |
| OseonSyncService | `OseonTracking` | nur Serilog | **integrieren** |
| EnaioDmsSyncService | `EnaioDms` | nur Serilog | **integrieren** |
| HolidaySyncService | `Holiday` | nur Serilog | **integrieren** |
| CoatingDetectionService | `CoatingDetection` | nur Serilog | **integrieren** |
| SageImportService (Production Orders) | `ProductionOrder` | nur Serilog | **integrieren** (Teil 1 von 2 Runs/Tick) |
| SageImportService (Articles) | `Article` | nur Serilog | **integrieren** (Teil 2 von 2 Runs/Tick) |

**Hinweis SageImportService:** ein Service-Tick = **zwei** logische Sync-Runs (`ProductionOrder` + `Article`), je mit eigenem `BeginRunAsync`/`FinishXxxAsync`-Paar.

**Explizit ausgeschlossen** (separate Activity-Log-Spec spaeter):
- `PartRequisitionEmailService`
- `WarehouseRequisitionEmailService`
- `BdeAutoPauseService`

## 4. API

### 4.1 Interfaces

Datei: `IdealAkeWms/Services/SyncLogger/ISyncLogger.cs` und `ISyncRun.cs`.

```csharp
namespace IdealAkeWms.Services.SyncLogger;

public interface ISyncLogger
{
    Task<ISyncRun> BeginRunAsync(string serviceName, CancellationToken ct = default);
}

public interface ISyncRun : IAsyncDisposable
{
    Task LogInfoAsync(string message, string? reference = null, CancellationToken ct = default);
    Task LogWarningAsync(string message, string? reference = null, CancellationToken ct = default);
    Task LogErrorAsync(string message, string? reference = null, CancellationToken ct = default);

    /// <summary>
    /// Erwarteter Abschluss bei sauberem Durchlauf. counts werden als deutschsprachige Key-Value-Pairs
    /// (z.B. "neu" => 12, "aktualisiert" => 5) in die End-Message gerendert.
    /// Erwartung: genau einer der beiden Finish-Aufrufe pro Run. Ein zweiter Aufruf wird idempotent
    /// ignoriert (kein Throw, kein zusaetzlicher Eintrag) — siehe Error-Handling §7.
    /// </summary>
    Task FinishSuccessAsync(IReadOnlyDictionary<string, int>? counts = null,
                            string? messageSuffix = null,
                            CancellationToken ct = default);

    /// <summary>
    /// Erwarteter Abschluss bei Fehler. errorMessage geht in den End-Eintrag,
    /// counts (soweit vorhanden) werden mit demselben Formatter wie bei Success gerendert.
    /// Idempotent (siehe FinishSuccessAsync).
    /// </summary>
    Task FinishFailedAsync(string errorMessage,
                           IReadOnlyDictionary<string, int>? counts = null,
                           CancellationToken ct = default);
}

// Hinweis zur Interface-Signatur:
// IAsyncDisposable.DisposeAsync() akzeptiert per Standard keinen CancellationToken.
// Die SyncRun-Implementierung verwendet CancellationToken.None fuer den "unexpected termination"-
// Eintrag — Dispose soll nicht abgebrochen werden, weil ihre einzige Funktion das Loggen ist.
```

### 4.2 Verwendungs-Pattern (Standard)

```csharp
await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.BomCache, ct);
try
{
    var inserted = 0; var updated = 0; var skipped = 0;
    foreach (var item in items)
    {
        if (IsDuplicate(item))
        {
            await run.LogWarningAsync("Duplikat ignoriert", reference: item.Code, ct);
            skipped++;
            continue;
        }
        // ... arbeit ...
    }

    await run.FinishSuccessAsync(new Dictionary<string, int>
    {
        ["neu"] = inserted,
        ["aktualisiert"] = updated,
        ["uebersprungen"] = skipped,
    }, ct: ct);
}
catch (Exception ex)
{
    await run.LogErrorAsync(ex.Message, ct: ct);
    await run.FinishFailedAsync(ex.Message, ct: ct);
    throw;
}
```

### 4.3 Verwendungs-Pattern (SageImportService — 2 Runs)

```csharp
// Innerhalb eines Service-Ticks
await ImportProductionOrdersAsync(ct);  // ruft intern BeginRunAsync(SyncLogServices.ProductionOrder)
await ImportArticlesAsync(ct);          // ruft intern BeginRunAsync(SyncLogServices.Article)
```

Beide Importe sind logisch eigenstaendig — jeweils eigenes `BeginRunAsync`/`FinishXxxAsync`-Paar.

### 4.4 Service-Namen-Konstanten

Datei: `IdealAkeWms/Services/SyncLogger/SyncLogServices.cs`.

```csharp
public static class SyncLogServices
{
    public const string Lagerplatz = "Lagerplatz";
    public const string Lagerbestand = "Lagerbestand";
    public const string BomCache = "BomCache";
    public const string OseonTracking = "OseonTracking";
    public const string EnaioDms = "EnaioDms";
    public const string Holiday = "Holiday";
    public const string CoatingDetection = "CoatingDetection";
    public const string ProductionOrder = "ProductionOrder";  // SageImport-Teil 1
    public const string Article = "Article";                  // SageImport-Teil 2

    public static IReadOnlyList<string> All => new[]
    {
        Lagerplatz, Lagerbestand, BomCache, OseonTracking, EnaioDms,
        Holiday, CoatingDetection, ProductionOrder, Article,
    };
}
```

Die [`SyncLogController.KnownServices`](IdealAkeWms/Controllers/SyncLogController.cs)-Liste wird auf `SyncLogServices.All` umgestellt — Single Source of Truth.

## 5. Implementierung

### 5.1 `SyncLogger` (Singleton-Service)

Datei: `IdealAkeWms/Services/SyncLogger/SyncLogger.cs`.

- **Konstruktor:** `IDbContextFactory<ApplicationDbContext>`, `ILogger<SyncLogger>` (Serilog-Fallback).
- **DI-Lebenszyklus:** Singleton. Verwendet `IDbContextFactory.CreateDbContextAsync()` pro Insert — entkoppelt von Scope-Lifecycle und Sync-Transaktionen.
- `BeginRunAsync(serviceName)`:
  1. Schreibt einen **Start-Eintrag** (Level Info, Message `"Run gestartet"`, kein Reference) via eigenem DbContext.
  2. Liefert eine neue `SyncRun`-Instanz (siehe 5.2) zurueck.

### 5.2 `SyncRun` (interne Implementierung von `ISyncRun`)

Datei: `IdealAkeWms/Services/SyncLogger/SyncRun.cs`.

- Felder: `_loggerFactory` (zum Erzeugen neuer DbContexts), `_serviceName`, `_serilog`, `_finishedAt` (nullable DateTime — null = noch nicht finished), `_finishMode` (None/Success/Failed).
- `LogInfoAsync/LogWarningAsync/LogErrorAsync(msg, reference?)`: holt frischen DbContext, schreibt einen Eintrag, dispose Context. Try/Catch um den DB-Write — bei Fehler Serilog-Warning, **kein Re-throw**.
- `FinishSuccessAsync(counts?, suffix?)`: schreibt End-Eintrag Level Info mit Message `"Run erfolgreich beendet — neu=12, aktualisiert=5, uebersprungen=0"`. Setzt `_finishMode=Success`.
- `FinishFailedAsync(errorMessage, counts?)`: schreibt End-Eintrag Level Error mit Message `"Run fehlgeschlagen: {errorMessage} — neu=2, ..."`. Setzt `_finishMode=Failed`.
- `DisposeAsync()`:
  - Wenn `_finishMode == None` → schreibt einen Warning-Eintrag `"Run wurde unerwartet beendet (kein FinishXxx-Aufruf)"`. Damit verlorene Finish-Pfade auffallen.
  - Wenn `_finishMode != None` → nichts tun.

### 5.3 Counts-Rendering

Helper-Funktion intern in `SyncRun`, **gemeinsam genutzt** von `FinishSuccessAsync` und `FinishFailedAsync`:

```csharp
private static string FormatCounts(IReadOnlyDictionary<string, int>? counts)
{
    if (counts == null || counts.Count == 0) return string.Empty;
    return string.Join(", ", counts.Select(kv => $"{kv.Key}={kv.Value}"));
}
```

End-Message-Format:
- **Success:** `"Run erfolgreich beendet{ ' — ' + counts }{ ' — ' + suffix }"`
- **Failed:**  `"Run fehlgeschlagen: {errorMessage}{ ' — ' + counts }"`

Bindestrich-Trenner werden nur eingefuegt, wenn der jeweilige Teil nicht leer ist.

### 5.4 DI-Registrierung

**Web-Projekt** [Program.cs](IdealAkeWms/Program.cs):
```csharp
builder.Services.AddDbContextFactory<ApplicationDbContext>(/* gleiche Connection */);
builder.Services.AddSingleton<ISyncLogger, SyncLogger>();
```

**Service-Projekt** [Program.cs](IDEALAKEWMSService/Program.cs):
```csharp
builder.Services.AddDbContextFactory<ApplicationDbContext>(/* gleiche Connection */);
builder.Services.AddSingleton<ISyncLogger, SyncLogger>();
```

**Konflikt-Check:** Wenn bereits ein normales `AddDbContext` (Scoped) registriert ist, kann `AddDbContextFactory` parallel registriert werden (`optionsLifetime: ServiceLifetime.Singleton`). Plan-Phase wird das verifizieren und ggf. anpassen.

### 5.5 Migration der bestehenden 2 Services

`LagerplatzSyncService` und `LagerbestandSyncService` werden umgestellt:
- Injizierten `ISyncLogRepository _syncLogs` ersetzt durch `ISyncLogger _syncLogger`.
- Direkte `_syncLogs.AddAsync(...)`-Aufrufe ersetzt durch `run.LogXxxAsync(...)`.
- Lokale `Service`-Konstanten ersetzt durch `SyncLogServices.Lagerplatz` / `Lagerbestand`.
- Bestehende Tests anpassen ([LagerplatzSyncServiceTests](IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs)) — entweder FakeSyncLogger oder Moq.

## 6. Datenfluss & Schema

Keine Schema-Aenderungen. Das `SyncLogs`-Schema bleibt 1:1 wie heute (Migration `SQL/56_AddSyncLog.sql`).

Pro Sync-Run entstehen typisch 2-N Zeilen:
- 1 Start (Info, Message `"Run gestartet"`, Reference null).
- 0..N Mid-Run-Events (Info/Warning/Error, Message + Reference je nach Sync).
- 1 End (Info bei Success / Error bei Failure, Message inkl. Counts und ggf. Errormessage).

`Reference` bleibt ein freitext-String, typischerweise der naturalsprachliche Schluessel des betroffenen Datensatzes (Lagerplatz-Code, Artikelnummer, FA-Nummer, etc.).

## 7. Error-Handling

| Szenario | Verhalten |
|---|---|
| SyncLog-DB-Schreibfehler (Connection-Timeout, DB-Lock) | Try/Catch in `SyncRun` faengt ab → Serilog `_logger.LogWarning(ex, "SyncLog write failed for {Service}", _serviceName)` → Methode kehrt normal zurueck. Kein Throw. |
| Sync wirft Exception nach `BeginRunAsync` ohne `FinishXxxAsync` | Caller fangs in seinem catch und ruft `FinishFailedAsync` (Standard-Pattern, siehe 4.2). Wenn er das vergisst: `DisposeAsync` schreibt Warning "Run unerwartet beendet". |
| `IDbContextFactory` selbst wirft (DB unerreichbar) | Try/Catch faengt — Serilog-Warning. Sync laeuft trotzdem weiter. |
| User ruft `FinishSuccessAsync` **und** `FinishFailedAsync` | Zweiter Aufruf wird ignoriert (`if (_finishMode != None) return;`). |
| Sync-Run wird per `CancellationToken` abgebrochen | Sync-Code fanget OperationCanceledException, ruft `FinishFailedAsync("cancelled")`. Logger-eigene Operationen werden mit ct durchgereicht. |

## 8. Testing

### 8.1 Unit-Tests fuer `SyncLogger` / `SyncRun`

Datei: `IdealAkeWms.Tests/Services/SyncLoggerTests.cs`.

- `BeginRunAsync_writes_start_entry`
- `LogInfoAsync_writes_info_entry_with_reference`
- `LogWarningAsync/ErrorAsync_writes_correct_level`
- `FinishSuccessAsync_writes_end_entry_with_formatted_counts`
- `FinishFailedAsync_writes_error_level_end_entry`
- `Dispose_without_finish_writes_unexpected_termination_warning`
- `Dispose_after_finish_writes_nothing`
- `DoubleFinish_is_ignored`
- `WriteFailure_falls_back_to_serilog_and_does_not_throw`

Setup: InMemory-DbContext via `IDbContextFactory<ApplicationDbContext>`-Test-Implementierung. Bekanntes Pattern aus [TestDbContextFactory](IdealAkeWms.Tests/Helpers/TestDbContextFactory.cs).

### 8.2 Migrierte Service-Tests

- [`LagerplatzSyncServiceTests`](IDEALAKEWMSService.Tests/Services/LagerplatzSyncServiceTests.cs): Mock-`ISyncLogger` mit Mock-`ISyncRun`. Verifizieren, dass BeginRunAsync mit `"Lagerplatz"` aufgerufen wird, dass LogWarningAsync bei Konflikten gerufen wird, dass FinishSuccessAsync mit korrekten Counts gerufen wird.
- Analog fuer `LagerbestandSyncService` (bestehende Tests bleiben grundlegend gleich, Assert-Targets wechseln vom Repo zum Logger).

### 8.3 Tests fuer die 6 neu zu integrierenden Services

- Pro Service mindestens **1 Happy-Path-Test** (Begin + Finish wird gerufen) und **1 Error-Test** (FinishFailedAsync wird gerufen wenn Sync wirft).
- Mock-`ISyncLogger`. Detail-Coverage steigt schrittweise — diese Spec erzwingt keinen 100%-Coverage-Anstieg.

### 8.4 Test-Helper

Datei: `IdealAkeWms.Tests/Helpers/FakeSyncLogger.cs` — In-Memory-Recorder, der alle Aufrufe (BeginRun, LogXxx, FinishXxx, DisposeAsync) in einer Liste sammelt, damit Tests pro Service einfach `fakeLogger.Calls.Should().ContainXxx(...)` machen koennen. Reduziert Moq-Boilerplate.

## 9. Verwendung in der UI

[SyncLog/Index](IdealAkeWms/Views/SyncLog/Index.cshtml) bleibt funktional unveraendert — nutzt die `Service`/`Level`/`Reference`-Filter und Pagination, die bereits da sind. Einzige Anpassung: `SyncLogController.KnownServices` wird auf `SyncLogServices.All` umgestellt, damit das Service-Dropdown automatisch alle 9 Service-Namen kennt.

## 10. Versionierung & Doku

- **AppVersion-Bump** auf `1.15.0` (Web + Service) — neuer einheitlicher Sync-Audit ist Major-Feature.
- **Changelog** Web ([Views/Help/Changelog.cshtml](IdealAkeWms/Views/Help/Changelog.cshtml)) — neuer v1.15.0-Block.
- **Hilfeseite** ([Views/Help/Index.cshtml](IdealAkeWms/Views/Help/Index.cshtml)) — "Sync-Protokoll"-Abschnitt erweitern: "Alle Background-Sync-Jobs werden hier protokolliert (Start, Warnings, Fehler, Ende-Summary)."
- **TESTSZENARIEN** ([docs/TESTSZENARIEN.md](docs/TESTSZENARIEN.md)) — 1 Szenario pro Service (insgesamt ~8): Service ausloesen, im Sync-Protokoll Start- + End-Eintrag pruefen.
- **PROJECT_STATUS.md** — neuer Eintrag in "Hauptfunktionen" und Roadmap-Zeile fuer v1.15.0.
- **CLAUDE.md** — Stichwort "SyncLogger" im "Bekannte Fallstricke"-Block (Hinweis: Counts-Keys deutschsprachig, IDbContextFactory wegen Transaktions-Isolation).

## 11. Risiken & Mitigation

| Risiko | Mitigation |
|---|---|
| `IDbContextFactory<ApplicationDbContext>` konflikt mit bestehender `AddDbContext`-Registrierung | Plan-Phase verifiziert Programm.cs-Setup. Falls Konflikt: `optionsLifetime: ServiceLifetime.Singleton` setzen oder bestehende Registrierung umstellen. |
| Massiver Anstieg an SyncLog-Zeilen (alle Syncs schreiben jetzt 2+ Zeilen pro Tick, jeden 15min-Tick) | Bei 9 Service-Namen x 96 Ticks/Tag x 2 Min-Zeilen = ~1730 Zeilen/Tag (Lifecycle only) + Warnings/Errors. Akzeptabel ohne Cleanup fuer Monate. Retention-Spec wird separat folgen. |
| Migrationspass fuer Lagerplatz/Lagerbestand verliert vorhandene Warnings versehentlich | Test-Coverage MUSS vor Migration die heutigen Warning-Pfade abdecken. Plan-Phase: erst Tests fuer aktuelles Verhalten gruen ziehen, dann migrieren, dann nochmal gruen. |
| Worker im Service-Projekt sind Singleton, `ApplicationDbContext` ist Scoped — Risiko eines Lifecycle-Mismatches | `SyncLogger` ist selbst Singleton und verwendet `IDbContextFactory` (Singleton-vertraeglich). Keine direkte Scope-Resolution noetig. |
| Test-Doppelarbeit (jeder der 8 Services bekommt aehnliche Mock-Setups) | `FakeSyncLogger` als Test-Helper (8.4) reduziert Boilerplate. |
| Existierende Logs-Filtering im Code referenziert evtl. `"Lagerplatz"`-String direkt | grep nach den 9 Service-Strings im gesamten Repo — Plan-Phase mappt etwaige Treffer auf Konstanten. |

## 12. Abgrenzung — was diese Spec NICHT macht

- Keine Retention/Cleanup (alte SyncLog-Eintraege loeschen) — bleibt heutiger Stand. Eigene Spec spaeter.
- Keine Activity-Log fuer Non-Sync-Aktivitaeten (`PartRequisitionEmailService`, `WarehouseRequisitionEmailService`, `BdeAutoPauseService`) — eigene Spec.
- Keine UI-Aenderung ueber `KnownServices`-Pflege hinaus. Kein Dashboard, kein Trend-Chart.
- Keine Schema-Erweiterung von `SyncLogs` (z.B. um `RunId`, `DurationMs`). Aktuelle Felder reichen fuer Lifecycle-Audit.

## 13. Offen / In Plan-Phase zu klaeren

- **`IDbContextFactory`-Registrierung im Service-Projekt** — exakte Form (`AddDbContextFactory` allein oder `AddDbContext` + `AddDbContextFactory` kombiniert) in Plan-Phase verifizieren.
- **Reihenfolge der Service-Migrationen** — vorgeschlagen: 1) Logger + Tests bauen, 2) Lagerplatz/Lagerbestand migrieren (Regression-Testbed), 3) die 6 neuen Integrationen, 4) UI-/Doku-Touch. Plan-Phase entscheidet final.
- **Commit-Granularitaet** — vorgeschlagen 1 Commit pro Service-Integration. Plan-Phase entscheidet final.
- **Repo-weite Suche nach hartcodierten Service-Strings** — `grep` auf die 9 Service-Namen (z.B. `"Lagerplatz"`, `"BomCache"`) ist in der Plan-Phase Task 1, damit existierende String-Referenzen (Tests, Filter, KnownServices) gegen die neuen `SyncLogServices.*`-Konstanten gemappt werden.

---

**Naechster Schritt:** User reviewt diese Spec. Bei Freigabe → `superpowers:writing-plans` → Implementierungsplan.
