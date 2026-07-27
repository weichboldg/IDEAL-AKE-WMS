# Cleanup-Jobs im Service — Aktivitätsprotokoll-Bereinigung — Design

**Status:** Approved (Design), wartet auf Plan
**Version:** in v1.25.0 gefaltet (kein AppVersion-Bump), Service-seitig, im Worktree `glas-bestellung`
**Scope:** Neuer `CleanupWorker` (BackgroundService) + erster Cleaner „Aktivitätsprotokoll"

## Problem

Die `SyncLogs`-Tabelle (UI-Label „Aktivitäts-Protokoll") wächst unbegrenzt — jeder
Sync-/Service-Lauf schreibt Start-/Finish- und Detail-Zeilen. Es gibt keine
automatische Bereinigung; die Tabelle läuft langfristig voll.

## Ziel

Ein **eigener, erweiterbarer „Cleanup"-Abschnitt** im Windows-Service, der als
ersten Job das Aktivitätsprotokoll nach einer in den Server-Settings definierten
Aufbewahrungsperiode bereinigt. Weitere Cleaner werden step-by-step nach demselben
Muster ergänzt.

Beispiel: `Cleanup:AktivitaetsprotokollAufbewahrungTage = 180` → alles älter als
180 Tage wird gelöscht.

## Nicht-Ziel / bewusst ausgeschlossen

- **Kein generisches `ICleanupJob`-Framework** (YAGNI bei aktuell einem Job) — die
  Erweiterbarkeit kommt aus dem klar strukturierten `CleanupWorker` + je Cleaner ein
  eigener Service + eigenes Setting.
- **Keine konfigurierbare Batch-Größe** und **kein konfigurierbarer Takt** (fest:
  Batch 5000, Takt 24 h). Nur die Aufbewahrung ist pro Cleaner einstellbar.
- **Kein Web-UI-Bereinigungs-Button** — reiner Hintergrund-Job.

## Design

### 1. `CleanupWorker` (neuer BackgroundService) — der „eigene Abschnitt"

Neue Datei `IDEALAKEWMSService/Workers/CleanupWorker.cs`, registriert in der
Service-`Program.cs` neben `SyncWorker`/`NotificationWorker`.

- **Loop:** führt alle Cleaner in jedem Durchlauf aus, danach `Task.Delay(24 h)`
  (`private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24)`). Nach
  Service-Start läuft der Cleaner einmal sofort, dann täglich. (Kein `_lastRun`-
  Tracking nötig, solange alle Cleaner denselben 24-h-Takt haben — jeder Cleaner ist
  ohnehin idempotent; ein Neustart lässt ihn erneut laufen, das schadet nicht.)
- **DryRun-bewusst:** liest `WorkerSettings:SyncDryRun` und reicht das Flag an jeden
  Cleaner durch (zählt statt zu löschen).
- **Resilient:** jeder Cleaner in try/catch → Fehler killt den Loop NICHT
  (`_logger.LogError` + `ISyncErrorNotifier.NotifyAsync(stepName, ex, ct)`), analog
  zum `RunResilientAsync`/`NotifyErrorAsync`-Muster im `SyncWorker`.
- **Scope:** pro Lauf ein `_scopeFactory.CreateScope()`, daraus die (scoped) Cleaner-
  Services auflösen.

### 2. Erster Cleaner: Aktivitätsprotokoll-Bereinigung

**Setting** `Cleanup:AktivitaetsprotokollAufbewahrungTage`
- Typ `Int`, Default `"180"`, Kategorie **„Bereinigung"**.
- Semantik: **N > 0** → lösche `SyncLog`-Zeilen mit `Timestamp < (DateTime.Now − N Tage)`.
  **N ≤ 0 (oder leer/nicht parsebar) = deaktiviert** (nie löschen).
- Gelesen via `ServiceSettings.GetIntSafeAsync(_config, key, 180, ct)` (DB-first,
  resilient — DB-Hickup → Default, Worker-Loop läuft weiter).

**Reine Logik** `ActivityLogCleanupPlanner.ComputeCutoff(DateTime now, int retentionDays)`
→ `DateTime?`:
- `retentionDays <= 0` → `null` (deaktiviert).
- sonst → `now.AddDays(-retentionDays)`.
- Statischer, seiteneffektfreier Helper → voll unit-testbar.

**Testbarer Service** `IActivityLogCleanupService` / `ActivityLogCleanupService`:
```
Task<ActivityLogCleanupResult> RunAsync(bool dryRun, CancellationToken ct);
public record ActivityLogCleanupResult(int Deleted, bool Skipped, string? SkipReason);
```
Ablauf:
1. `retentionDays = await ServiceSettings.GetIntSafeAsync(_config, "Cleanup:AktivitaetsprotokollAufbewahrungTage", 180, ct)`.
2. `cutoff = ActivityLogCleanupPlanner.ComputeCutoff(DateTime.Now, retentionDays)`.
3. `cutoff == null` (deaktiviert) → `return new(0, Skipped: true, "deaktiviert (Aufbewahrung <= 0)")`.
   **Kein** Protokoll-Eintrag (Serilog-Debug reicht → deaktiviert = still).
4. sonst: eigenen Protokoll-Lauf öffnen und löschen (siehe unten).

**Löschen** — neue Repo-Methode (Web-Projekt, vom Service via ProjectReference nutzbar):
```
Task<int> DeleteOlderThanAsync(DateTime cutoff, int batchSize, bool dryRun, CancellationToken ct);
```
- `dryRun == true` → nur zählen (`Where(x => x.Timestamp < cutoff).CountAsync`), NICHT löschen.
- sonst gebatcht bis leer:
  ```
  while (true) {
      var batch = await _context.SyncLogs
          .Where(x => x.Timestamp < cutoff).Take(batchSize).ToListAsync(ct);
      if (batch.Count == 0) break;
      _context.SyncLogs.RemoveRange(batch);
      await _context.SaveChangesAsync(ct);
      total += batch.Count;
      if (batch.Count < batchSize) break;
  }
  return total;
  ```
- `batchSize` als Konstante `5000` im Service übergeben (kein Setting).
- Load-Batch + `RemoveRange` (statt `ExecuteDeleteAsync`) bewusst, damit InMemory-testbar
  UND gegen lange Sperren gebatcht; SyncLog-Zeilen sind klein.

**Eigener Protokoll-Lauf** (nur wenn aktiviert):
- Neue Konstante `SyncLogServices.CleanupActivityLog = "CleanupAktivitaetsprotokoll"`
  (+ in `SyncLogServices.All` aufnehmen → Dropdown im Protokoll-Filter kennt den Namen).
- `await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.CleanupActivityLog, ct);`
- **Erst löschen, dann `FinishSuccessAsync`** — der eigene Lauf-Eintrag (`Timestamp = jetzt`)
  ist neuer als der Stichtag, wird also vom aktuellen Lauf NICHT mitgelöscht. Alte
  Cleanup-Lauf-Zeilen werden von künftigen Läufen selbst mitbereinigt (self-cleaning).
- `FinishSuccessAsync(counts: { ["geloescht"] = deleted }, messageSuffix:
  $"Aufbewahrung {retentionDays} Tage, Stichtag {cutoff:dd.MM.yyyy}" + (dryRun ? " (DryRun — nichts geloescht)" : ""), ct)`.
- Bei Exception: `run.FinishFailedAsync(ex.Message, ct)` + **re-throw** (der Worker
  fängt es, loggt + `NotifyAsync`). Pro aktiviertem Lauf entsteht ein Protokoll-Eintrag
  (auch bei `geloescht=0`) — konsistent mit allen anderen Jobs, self-cleaning.

### 3. Katalog + Seed + Drift-Guard

- Neuer Eintrag in `ServiceSettingDefinitions.All`:
  `new("Cleanup:AktivitaetsprotokollAufbewahrungTage", ServiceSettingType.Int, "180", "Bereinigung", "Aktivitaets-Protokoll: Eintraege aelter als X Tage werden taeglich geloescht (0 = nie loeschen)")`.
- `/ServiceSettings`-Editor + Program.cs-Seed erben den Key automatisch (beide getrieben von `All`).
- `InlineData("Cleanup:AktivitaetsprotokollAufbewahrungTage")` im Drift-Guard-Test
  `ServiceSettingDefinitionsTests.All_ContainsDocumentedServiceReadKey` ergänzen (sonst rot).

### 4. DI

- `CleanupWorker` als HostedService in der Service-`Program.cs` registrieren.
- `IActivityLogCleanupService` → `ActivityLogCleanupService` (scoped) registrieren.
- `ISyncLogRepository` muss im Service-DI auflösbar sein — **im Plan-Pre-flight prüfen**
  und ggf. registrieren (Web registriert es; der Service hat eigene Registrierungen).
- `ISyncLogger` + `ISyncErrorNotifier` + `IConfiguration` sind im Service bereits registriert.

### 5. Erweiterbarkeit (Muster für weitere Cleaner)

Ein künftiger Cleaner (z.B. „alte BomCache-Einträge", „alte BDE-Buchungen") =:
1. neues `Cleanup:XxxAufbewahrungTage`-Setting in `ServiceSettingDefinitions.All`
   (+ `InlineData` im Drift-Guard),
2. `SyncLogServices.CleanupXxx`-Konstante (+ `All`),
3. `IXxxCleanupService.RunAsync(dryRun, ct)` + zugehörige Repo-Delete-Methode + Planner,
4. ein weiterer gegateter Block im `CleanupWorker`,
5. Doku.

## Betroffene Dateien

- `IDEALAKEWMSService/Workers/CleanupWorker.cs` (**neu**)
- `IDEALAKEWMSService/Services/ActivityLogCleanupService.cs` (**neu**, + `IActivityLogCleanupService`)
- `IDEALAKEWMSService/Services/ActivityLogCleanupPlanner.cs` (**neu**, reine Logik + Result-Record)
  *(oder Planner/Result im selben File wie der Service — Plan entscheidet)*
- `IdealAkeWms/Data/Repositories/SyncLogRepository.cs` + `ISyncLogRepository` (Methode `DeleteOlderThanAsync`)
- `IdealAkeWms/Services/SyncLogger/SyncLogServices.cs` (Konstante `CleanupActivityLog` + `All`)
- `IdealAkeWms/Models/ServiceSettingDefinitions.cs` (neuer Key, Kategorie „Bereinigung")
- `IDEALAKEWMSService/Program.cs` (DI: Worker + Service; ggf. `ISyncLogRepository`)
- Tests: `ActivityLogCleanupPlannerTests`, `ActivityLogCleanupServiceTests`,
  `SyncLogRepositoryTests` (DeleteOlderThan), `ServiceSettingDefinitionsTests` (InlineData)
- Doku: Changelog (v1.25.0-Karte), CLAUDE.md (Service-Config-Tabelle + Fallstrick),
  `docs/TESTSZENARIEN.md` (neues Kapitel), PROJECT_STATUS.md
- **Kein** Schema-Change / **keine** Migration / **kein** AppVersion-Bump

## Tests (TDD)

- **Planner (rein, xUnit):** (a) `retentionDays=180`, `now=fix` → `now.AddDays(-180)`;
  (b) `retentionDays=0` → `null`; (c) `retentionDays=-5` → `null`.
- **Repo `DeleteOlderThanAsync` (InMemory):** seed alte (`< cutoff`) + neue (`>= cutoff`)
  Zeilen → (d) `dryRun=false` löscht nur die alten, Rückgabe = Anzahl gelöschter;
  (e) `dryRun=true` löscht NICHTS, Rückgabe = Anzahl der Kandidaten; (f) Batch-Grenze:
  mehr Kandidaten als `batchSize` → alle gelöscht (Schleife terminiert).
- **Service (InMemory + fake ISyncLogger/ISyncLogRepository oder echtes Repo):**
  (g) deaktiviert (`retention=0` in DB) → `Skipped=true`, kein Delete, **kein**
  Protokoll-Lauf; (h) aktiv → Delete + genau ein Protokoll-Lauf mit `geloescht`-Count;
  (i) DryRun → kein Delete, Lauf-Suffix „(DryRun …)".
  *(Setting-Read via `ServiceSettings.GetIntSafeAsync` liest aus der DB, nicht aus
  IConfiguration → in InMemory-Tests fällt der Wert auf Default 180; für den
  „deaktiviert"-Test daher entweder den Service so bauen, dass `retentionDays` injizierbar/
  test-übersteuerbar ist, ODER den Test über den Planner + eine dünnere Service-Signatur
  führen. Plan klärt die genaue Teststrategie — analog zum Lagerbestand-Cap-Fallstrick.)*
- **CleanupWorker-Loop:** Manual-UAT (BackgroundService-Loop, wie die anderen Worker).

## Fallstricke (verifiziert / zu beachten)

1. **Stichtag aus `DateTime.Now` (Lokalzeit), NICHT `UtcNow`** — `SyncLog.Timestamp`
   wird per Model-Default als Lokalzeit gespeichert (dokumentierter Fallstrick). Ein
   UtcNow-Stichtag würde je nach Offset zu früh/spät schneiden. Der Planner bekommt `now`
   als Parameter; der Service übergibt `DateTime.Now`.
2. **Deaktiviert = still** — bei `retention <= 0` KEIN Protokoll-Eintrag (sonst schreibt
   ein „deaktivierter" Job täglich ins Log, das er nicht bereinigt).
3. **Selbst-Bereinigung** — der Cleaner löscht aus der Tabelle, in die er selbst
   schreibt. Erst löschen, dann `FinishSuccess`; der eigene Lauf-Eintrag ist neuer als der
   Stichtag → überlebt. Alte Cleanup-Läufe werden von künftigen Läufen mitbereinigt.
4. **`ServiceSettings.GetIntSafeAsync` liest die DB, nicht IConfiguration** — in
   InMemory-Tests greift immer der Default (180). Teststrategie im Plan entsprechend
   (Planner separat testen; Service-„deaktiviert"-Fall über injizierbaren Retention-Wert).
5. **Gebatchtes Löschen ohne `ExecuteDeleteAsync`** — bewusst Load-Batch + `RemoveRange`
   (InMemory-testbar + gegen lange Sperren gebatcht). `ExecuteDeleteAsync` wäre effizienter,
   ist aber nicht InMemory-testbar.
6. **Drift-Guard** — neuer Katalog-Key MUSS als `InlineData` in
   `ServiceSettingDefinitionsTests.All_ContainsDocumentedServiceReadKey` (sonst schlägt der
   Test fehl).
7. **`ISyncLogRepository`-Registrierung im Service** — Pre-flight prüfen; ggf. ergänzen.
