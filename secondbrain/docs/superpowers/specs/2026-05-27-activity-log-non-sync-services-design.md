# Activity-Log fuer Non-Sync-Services — Design

**Datum:** 2026-05-27
**Status:** Spec, Brainstorming abgeschlossen, wartet auf User-Review vor `writing-plans`
**Scope:** Web + Service (`IdealAkeWms/`, `IDEALAKEWMSService/`)
**Vorgaenger-Spec:** [2026-05-26-synclog-pflicht-alle-syncs-design.md](2026-05-26-synclog-pflicht-alle-syncs-design.md) (v1.15.0)

---

## 1. Problemstellung

Mit v1.15.0 schreiben alle 8 echten Sync-Services ueber `ISyncLogger` ins `SyncLogs`-Audit-Log. Drei wiederkehrende Background-Aktivitaeten sind heute **bewusst** aussen vor geblieben, weil sie keine "Syncs" im engeren Sinne sind:

- `PartRequisitionEmailService` — versendet Bedarfsmeldungen per Mail (Worker-Tick, alle 15 Min, Config-gated)
- `WarehouseRequisitionEmailService` — versendet Lagerbestellungs-Submits + Stornos per Mail (gleicher Worker-Tick)
- `BdeAutoPauseService` — auto-pausiert offene BDE-Buchungen am Schicht-Ende (Worker-Tick, alle 60 Min)

Folgen heute:
- Operations-Team hat keine zentrale Sicht, wann Mails versendet/Bookings pausiert wurden.
- Fehlversand einer einzelnen E-Mail muss in Serilog-Dateilogs gesucht werden.
- Auto-Pause-Aktionen sind unsichtbar fuer Schichtleiter, ausser der einzelnen Booking-Aenderung in der BDE-Cockpit-Liste.

## 2. Ziele

1. **Alle 3 Non-Sync-Services** schreiben einheitlich ins bestehende `SyncLogs`-Schema ueber den vorhandenen `ISyncLogger` — Lifecycle (Start + Ende) + Mid-Run-Events pro Einzel-Aktion (Mail/Booking).
2. **Per-Item-Sichtbarkeit**: jedes Mid-Run-Event referenziert die betroffene Entitaet (FA-Nummer, Requisition-Id, Booking-Id) ueber das vorhandene `Reference`-Feld.
3. **UI-Umbenennung** "Sync-Protokoll" → "Aktivitaets-Protokoll", weil "Sync" semantisch nicht mehr passt sobald E-Mails und BDE-Auto-Pause dort erscheinen.
4. **Keine Schema-Migration, keine neuen Interfaces, keine neuen Controller**. Die `ISyncLogger`/`ISyncRun`-API aus v1.15.0 ist bereits generisch.
5. **Out-of-Scope:** Retention/Cleanup-Job (separate Spec), Notification-Mechanismen (Admin-Mails bei Fehlern), Schema-Refactor `SyncLogs` → `AuditLogs`.

## 3. Scope: Services

| Service | Service-Name (Konstante) | Top-Level-Methode | Trigger | Frequenz |
|---|---|---|---|---|
| `PartRequisitionEmailService` | `PartRequisitionEmail` | `SendPendingEmailsAsync(bool dryRun, ct)` | SyncWorker-Tick, `Sync:PartRequisitionEmailEnabled=true` | 15 Min |
| `WarehouseRequisitionEmailService` | `WarehouseRequisitionEmail` | `SendPendingEmailsAsync(bool dryRun, ct)` | SyncWorker-Tick, `Sync:WarehouseRequisitionEmailEnabled=true` | 15 Min |
| `BdeAutoPauseService` | `BdeAutoPause` | `RunAsync(ct)` | SyncWorker-Tick (eigenes Intervall), `Sync:BdeSchichtkalenderAktiv=true` | 60 Min |

## 4. Schema-Erweiterung — `SyncLogServices`

Datei: [IdealAkeWms/Services/SyncLogger/SyncLogServices.cs](../../../../IdealAkeWms/Services/SyncLogger/SyncLogServices.cs).

Drei neue Konstanten an das Ende der bestehenden 9 anfuegen:

```csharp
public const string PartRequisitionEmail = "PartRequisitionEmail";
public const string WarehouseRequisitionEmail = "WarehouseRequisitionEmail";
public const string BdeAutoPause = "BdeAutoPause";
```

`All`-Property erweitert auf 12 Eintraege (Reihenfolge: erst die 9 Sync, dann die 3 Non-Sync).

Keine weitere Schema-Aenderung. DB-Tabelle, Repository, Controller-Logik bleiben unveraendert. Das `SyncLogController.KnownServices`-Dropdown (bezieht sich auf `SyncLogServices.All`) zeigt automatisch die neuen Namen.

## 5. Service-Integrationen

Identisches Pattern wie v1.15.0 Tasks 10-15. Pro Service: Konstruktor-Erweiterung um `IdealAkeWms.Services.SyncLogger.ISyncLogger syncLogger` (nach `logger`), `await using var run` am Methoden-Anfang, try/catch mit `FinishSuccessAsync`/`FinishFailedAsync`, Mid-Run-Events mit `Reference` pro Einzel-Aktion.

### 5.1 PartRequisitionEmailService

```csharp
public async Task<EmailResult> SendPendingEmailsAsync(bool dryRun, CancellationToken ct)
{
    await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.PartRequisitionEmail, ct);
    int sent = 0, skipped = 0, errors = 0;
    try
    {
        var groups = await _repo.GetPendingGroupedAsync(ct);
        // ...
        foreach (var group in groups)
        {
            try
            {
                if (recipients.Count == 0)
                {
                    await run.LogWarningAsync("Keine aktiven Empfaenger", reference: group.OrderNumber, ct);
                    skipped++;
                    continue;
                }
                // ... Mail-Versand ...
                if (!dryRun) await _mailer.SendAsync(mail, ct);
                await run.LogInfoAsync($"Mail versendet an {string.Join(", ", recipients)}",
                                       reference: group.OrderNumber, ct);
                sent++;
            }
            catch (Exception ex)
            {
                await run.LogWarningAsync($"Mail-Versand fehlgeschlagen: {ex.Message}",
                                          reference: group.OrderNumber, ct);
                errors++;
            }
        }

        await run.FinishSuccessAsync(new Dictionary<string, int>
        {
            ["versendet"] = sent,
            ["ohne_empfaenger"] = skipped,
            ["fehler"] = errors,
        }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
        return new EmailResult(sent, skipped, errors);
    }
    catch (Exception ex)
    {
        await run.LogErrorAsync(ex.Message, ct: ct);
        await run.FinishFailedAsync(ex.Message, ct: ct);
        throw;
    }
}
```

**Reference-Konvention:** FA-Nummer (`group.OrderNumber`).

### 5.2 WarehouseRequisitionEmailService

Hat zwei Queues — Submits und Stornos. Beide laufen unter **einem** Run, Mid-Run-Events differenzieren via Reference-Praefix:

```csharp
public async Task<EmailResult> SendPendingEmailsAsync(bool dryRun, CancellationToken ct)
{
    await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.WarehouseRequisitionEmail, ct);
    int submitSent = 0, stornoSent = 0, errors = 0;
    try
    {
        // Submit-Queue
        foreach (var req in await _repo.GetPendingSubmitsAsync(ct))
        {
            try
            {
                // ...Mail-Versand...
                await run.LogInfoAsync($"Submit-Mail versendet an {recipients}",
                                       reference: $"submit/{req.Id}", ct);
                submitSent++;
            }
            catch (Exception ex)
            {
                await run.LogWarningAsync($"Submit-Mail fehlgeschlagen: {ex.Message}",
                                          reference: $"submit/{req.Id}", ct);
                errors++;
            }
        }

        // Storno-Queue (gleicher Run)
        foreach (var req in await _repo.GetPendingCancelsAsync(ct))
        {
            try
            {
                // ...Mail-Versand...
                await run.LogInfoAsync($"Storno-Mail versendet an {recipients}",
                                       reference: $"storno/{req.Id}", ct);
                stornoSent++;
            }
            catch (Exception ex)
            {
                await run.LogWarningAsync($"Storno-Mail fehlgeschlagen: {ex.Message}",
                                          reference: $"storno/{req.Id}", ct);
                errors++;
            }
        }

        await run.FinishSuccessAsync(new Dictionary<string, int>
        {
            ["submit_versendet"] = submitSent,
            ["storno_versendet"] = stornoSent,
            ["fehler"] = errors,
        }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
        return new EmailResult(submitSent, stornoSent, errors);
    }
    catch (Exception ex)
    {
        await run.LogErrorAsync(ex.Message, ct: ct);
        await run.FinishFailedAsync(ex.Message, ct: ct);
        throw;
    }
}
```

**Reference-Konvention:** `"submit/{requisitionId}"` oder `"storno/{requisitionId}"` — damit Filter im UI nach `submit/` oder `storno/` filtern kann.

### 5.3 BdeAutoPauseService

```csharp
public async Task<AutoPauseResult> RunAsync(CancellationToken ct)
{
    await using var run = await _syncLogger.BeginRunAsync(SyncLogServices.BdeAutoPause, ct);
    int checkedCount = 0, pausedCount = 0, errors = 0;
    try
    {
        var openBookings = await _repo.GetOpenBookingsAsync(ct);
        checkedCount = openBookings.Count;

        foreach (var booking in openBookings)
        {
            try
            {
                var shiftEnd = _shiftCalendar.GetShiftEnd(booking);
                if (DateTime.Now < shiftEnd) continue;

                booking.Status = BdeBookingStatus.AutoPaused;
                booking.EndedAt = shiftEnd;
                await _repo.UpdateAsync(booking, ct);

                await run.LogInfoAsync($"Booking auto-paused (Schichtende {shiftEnd:HH:mm})",
                                       reference: $"booking/{booking.Id}", ct);
                pausedCount++;
            }
            catch (Exception ex)
            {
                await run.LogWarningAsync($"Auto-Pause fehlgeschlagen: {ex.Message}",
                                          reference: $"booking/{booking.Id}", ct);
                errors++;
            }
        }

        await run.FinishSuccessAsync(new Dictionary<string, int>
        {
            ["geprueft"] = checkedCount,
            ["pausiert"] = pausedCount,
            ["fehler"] = errors,
        }, ct: ct);
        return new AutoPauseResult(checkedCount, pausedCount, errors);
    }
    catch (Exception ex)
    {
        await run.LogErrorAsync(ex.Message, ct: ct);
        await run.FinishFailedAsync(ex.Message, ct: ct);
        throw;
    }
}
```

**Reference-Konvention:** `"booking/{bookingId}"`.

### 5.4 Konstruktor-Position-Konvention

Alle 3 Services bekommen `ISyncLogger syncLogger` als zusaetzlichen Parameter **nach `ILogger<T> logger`** — konsistent mit den BomCache/Oseon/EnaioDms/CoatingDetection-Integrationen aus v1.15.0.

## 6. UI-Umbenennung

Asymmetrisch: User-facing Labels werden auf "Aktivitaets-Protokoll" umbenannt, interner Code (Klassen, Tabellen, Routes) bleibt stabil.

### 6.1 Was umbenannt wird

| Datei | Aenderung |
|---|---|
| [IdealAkeWms/Views/Shared/_Layout.cshtml](../../../../IdealAkeWms/Views/Shared/_Layout.cshtml) | Nav-Menue-Label "Sync-Protokoll" → "Aktivitaets-Protokoll" |
| [IdealAkeWms/Views/SyncLog/Index.cshtml](../../../../IdealAkeWms/Views/SyncLog/Index.cshtml) | `ViewData["Title"]`, `<h2>` Page-Header, Spalten-Header `<th>Service</th>` → `<th>Aktivitaet</th>`, Filter-Dropdown `<label>Service</label>` → `<label>Aktivitaet</label>` |
| [IdealAkeWms/Views/Help/Index.cshtml](../../../../IdealAkeWms/Views/Help/Index.cshtml) | Sync-Protokoll-Abschnitt umbenennen + Service-Liste aktualisieren auf 12 Namen |
| [docs/TESTSZENARIEN.md](../../../../docs/TESTSZENARIEN.md) | Kapitel 27 Referenzen "Sync-Protokoll" → "Aktivitaets-Protokoll" |

### 6.2 Was NICHT umbenannt wird

- DB-Tabelle `SyncLogs` (keine Migration)
- Controller `SyncLogController`, Route `/SyncLog/Index`
- Models `SyncLog`, `SyncLogLevel`
- Interfaces `ISyncLogger`, `ISyncRun`, Klasse `SyncLogger`, Konstanten `SyncLogServices`
- Repository `SyncLogRepository`
- `data-col-key="Service"`-Attribut (nur das Display-Label `<th>` wird umbenannt — der col-key bleibt damit bestehende `?colf_Service=...`-Filter-URLs funktionieren)

### 6.3 CLAUDE.md-Dokumentation der Asymmetrie

Neuer Punkt im "Bekannte Fallstricke"-Block:

> **UI 'Aktivitaets-Protokoll' vs. Tabelle 'SyncLogs' (seit v1.15.1)**: Das UI-Label und der Menue-Eintrag heissen "Aktivitaets-Protokoll" (umbenannt v1.15.1), die DB-Tabelle, Klassen und Interfaces behalten den historischen Namen `SyncLog`/`SyncLogger`. Bewusste Asymmetrie — interner Refactor war es nicht wert.

## 7. Tests

### 7.1 PartRequisitionEmailService

- Bestehende Tests auf neuen Konstruktor mit `FakeSyncLogger` umstellen.
- Neuer Lifecycle-Test:
  ```csharp
  [Fact]
  public async Task Send_writes_lifecycle_to_synclogger()
  {
      var fakeLogger = new FakeSyncLogger();
      var service = new PartRequisitionEmailService(/* ... */, fakeLogger, /* logger */);
      await service.SendPendingEmailsAsync(dryRun: false, ct: CancellationToken.None);
      fakeLogger.Runs.Should().ContainSingle();
      fakeLogger.Runs[0].ServiceName.Should().Be("PartRequisitionEmail");
      fakeLogger.Runs[0].FinishedSuccess.Should().BeTrue();
  }
  ```
- Neuer Reference-Test: bei einer Test-Gruppe mit OrderNumber "FA-001" und erfolgreichem Versand muss ein `Info`-Event mit `reference = "FA-001"` aufgezeichnet werden.

### 7.2 WarehouseRequisitionEmailService

- Bestehende Tests auf neuen Konstruktor umstellen.
- Lifecycle-Test (siehe oben).
- Reference-Differenzierungs-Test: bei einem Submit und einem Storno gleichzeitig, Events haben `reference.StartsWith("submit/")` bzw. `reference.StartsWith("storno/")`.

### 7.3 BdeAutoPauseService

- Bestehende Tests auf neuen Konstruktor umstellen.
- Lifecycle-Test.
- Reference-Test: bei einem auto-paused Booking-Id 42 muss ein `Info`-Event mit `reference = "booking/42"` aufgezeichnet werden.

Keine neuen `SyncLoggerTests` noetig — die 9 bestehenden decken die generische Infrastruktur ab.

## 8. Versionierung & Doku

- **AppVersion-Bump** auf `1.15.1` (Patch — kein API-Break, nur Erweiterung der Logging-Coverage + UI-Umbenennung). Web + Service.
- **Changelog** ([Views/Help/Changelog.cshtml](../../../../IdealAkeWms/Views/Help/Changelog.cshtml)) — v1.15.1-Eintrag.
- **Hilfeseite** — Sync-Protokoll-Abschnitt umbenennen, Service-Liste auf 12 Namen erweitern.
- **TESTSZENARIEN** — neues Kapitel 28 mit 3 Szenarien (1 pro Service).
- **PROJECT_STATUS.md** — neue Sub-Task-Sektion + neue Hauptfunktionen-Zeile + Roadmap-Eintrag fuer v1.15.1.
- **CLAUDE.md** — Asymmetrie-Hinweis (siehe 6.3).

## 9. Risiken & Mitigation

| Risiko | Mitigation |
|---|---|
| User-Verwirrung durch Asymmetrie UI vs. DB-Tabelle | CLAUDE.md-Doku, Hilfeseite erklaert die Umbenennung |
| Bookmarks/URLs auf `/SyncLog/Index` koennen weiterhin funktionieren | Routes/Controller bleiben unveraendert |
| `?colf_Service=...`-Filter-URLs muessen weiter funktionieren | `data-col-key="Service"` bleibt, nur das Display-Label wird umbenannt |
| Volumen-Zunahme im SyncLog | Per-Mail-Events bei E-Mail-Services erhoehen Zeilenanzahl um ~5-25/Tag pro Service. Retention-Job bleibt eigene spaetere Spec. |
| Bestehende Tests (PartRequisition/Warehouse/BdeAutoPause) brechen durch Konstruktor-Aenderung | TDD: erst Tests mit FakeSyncLogger gruen ziehen, dann Service-Code anfassen |

## 10. Abgrenzung — NICHT in dieser Spec

- Keine Retention/Cleanup-Logik fuer `SyncLogs` (eigene Spec)
- Keine Notification-Mechanismen (z.B. Admin-Mail bei Errors)
- Keine Performance-Optimierung (kein Batching mehrerer Events in einer SaveChanges-Operation)
- Keine UI-Erweiterung wie Dashboard-Charts oder Trend-Analyse
- Keine Umbenennung interner Klassen (kein Refactor `SyncLog*` → `Activity*`)
- Keine Erweiterung des `SyncLog`-Schemas (z.B. um `DurationMs` oder strukturierte Tags)

## 11. Offen / In Plan-Phase zu klaeren

- **Genaue Position des `ISyncLogger`-Parameters in den 3 Service-Konstruktoren** — vorgeschlagen "nach `logger`", final in Plan-Phase nach Lesen der echten Signaturen.
- **Counter-Variable-Mapping** — die in Sektion 5 vorgeschlagenen lokalen Variablennamen (`sent`, `skipped`, `errors`, `submitSent`, `stornoSent`, `checkedCount`, `pausedCount`) muessen mit den **echten** Variablen-Namen aus dem bestehenden Code abgeglichen werden.
- **Bestehende Test-Setups** — pro Service einzeln verifizieren, ob `FakeSyncLogger`-Injection bei jedem `new PartRequisitionEmailService(...)`/`new WarehouseRequisitionEmailService(...)`/`new BdeAutoPauseService(...)`-Call-Site noetig ist.
- **Reference-Format pro Mail-Gruppe** — bei PartRequisitionEmail wird derzeit nach (Email-Empfaenger + FA-Nummer) gruppiert. Reference koennte sein: nur die FA-Nummer (einfacher) oder `"fa/{order}|to/{recipients}"` (eindeutig). Plan-Phase entscheidet.

---

**Naechster Schritt:** User reviewt die Spec. Bei Freigabe → `superpowers:writing-plans` → Implementierungsplan.
