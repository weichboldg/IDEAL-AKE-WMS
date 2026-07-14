# Lagerbestand-Nullsetzen bei verschwundenen Sage-Zeilen — Implementierungsplan

**Spec:** [docs/superpowers/specs/2026-07-09-lagerbestand-nullsetzen-verwaist-design.md](../specs/2026-07-09-lagerbestand-nullsetzen-verwaist-design.md)
**Branch/Worktree:** `feature/glas-bestellung` in `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\glas-bestellung` (HEAD `14bde26`)
**Version:** in v1.25.0 gefaltet — **KEIN AppVersion-Bump**, **KEINE Migration/Schema-Change**.

## For agentic workers

Dies ist ein **Bugfix**, kein Feature. Ziel: der Lagerbestand-Sync soll `(Artikel, Sage-Lagerplatz)`-Paare,
die aus dem Sage-Snapshot **verschwunden** sind (Sage liefert 0-Bestand-Zeilen gar nicht mehr), im WMS
auf **exakt 0** korrigieren — analog zum `WHEN NOT MATCHED BY SOURCE`-Muster der FA-Reconciliation.

Arbeite die Tasks **streng sequentiell** ab (Task 1 → 2 → 3 → 4). Innerhalb jedes Tasks gilt **TDD**:
failing Test schreiben → Rot beweisen → Implementieren → Grün beweisen → committen. **Keine Platzhalter**:
jeder Code-Block ist copy-paste-fertig, jeder Befehl ist wörtlich auszuführen.

**Verifizierte Ausgangslage (nicht erneut recherchieren — bereits am echten Code geprüft, siehe Abschnitt
„Fallstricke (verifiziert)" am Ende):**
- `SageBestandDto` = `record SageBestandDto(string? Artikelnummer, string? Lagerplatz, decimal? Bestand)`.
- `StorageLocationSource` ist eine **static class mit String-Konstanten** (`Manual`/`Sage`), `StorageLocation.Source` ist ein `string`. NAN wird in `IdealAkeWms/Program.cs` OHNE `Source` geseedet → default `Manual`.
- Es gibt **bereits** `IDEALAKEWMSService.Tests/Services/LagerbestandSyncServiceTests.cs` (14 Tests) — die Spec-Annahme „keine Tests, neue Datei nötig" ist FALSCH. Diese Datei wird **erweitert**.
- `ServiceSettings.GetIntSafeAsync` liest aus der **DB-Tabelle** `[ServiceSettings]` (über eine echte `SqlConnection`), **nicht** aus IConfiguration-Keys. In InMemory-Tests fällt der Read immer auf den mitgegebenen **Default (100)** zurück (kein Connection-String → `try/catch` → Default). Konsequenz: der Service-Cap-Test muss **101 verwaiste Paare** seeden.
- DI: `AddScoped<ILagerbestandSyncService, LagerbestandSyncService>()` (Typ-Registrierung) — beide neuen ctor-Deps (`IConfiguration`, `ISyncErrorNotifier`) sind im Host bereits registriert → **keine Änderung an `Program.cs` nötig**.

## Goal

Nach der bestehenden Korrektur-Schleife in `LagerbestandSyncService.RunAsync` für jedes `(Artikel, Lagerplatz)`
mit WMS-Bestand **≠ 0** auf einem **Sage-Quelle + aktiven** Lagerplatz, dessen Paar **nicht** im aktuellen
Sage-Snapshot vorkommt, eine `StockMovement`-Korrektur auf **exakt 0** buchen — geschützt durch Leer-Guard
(leerer Sage-Read → nichts anfassen) und Cap (zu viele Kandidaten → kein Nullsetzen + Fehlermail).

## Architecture

- **Reiner Planer-Helfer** `LagerbestandZeroingPlanner.Plan(...)` (statisch, keine DB, voll unit-testbar) — Vorbild ist `ProductionOrderReconciler.Plan(...)`. Kapselt die drei Guards (Leer-Read/Cap/Kandidaten) und liefert ein `LagerbestandZeroingPlan`-Record.
- **Service-Verdrahtung** in `LagerbestandSyncService.RunAsync`: baut `sagePresentKeys` (aus den **roh** gelesenen Zeilen, vor Dedup) + `managedStock` (gefilterter `wmsStock`), ruft den Planer, bucht Nullkorrekturen bzw. loggt Skip + `NotifyAsync` bei Cap. Alles VOR dem bestehenden `SaveChangesAsync`.
- **Konfiguration** über den typisierten Katalog `ServiceSettingDefinitions.All` (Single Source of Truth) + `appsettings.json` (Default-Referenz). Gelesen via `ServiceSettings.GetIntSafeAsync`.

## Tech Stack

- .NET 10, ASP.NET Core 10 (Web) + Worker-Service (`IDEALAKEWMSService`), EF Core 10 + SQL Server, Serilog.
- Tests: xUnit + FluentAssertions + Moq + EF InMemory (`TestDbContextFactory.Create()`, `FakeSyncLogger`, `FakeSageBestandReader`).
- Build: `dotnet build IdealAkeWms.slnx`. Service-Tests: `dotnet test IDEALAKEWMSService.Tests --nologo`. Web-Tests: `dotnet test IdealAkeWms.Tests --nologo`.
- Git im Worktree via Bash: `cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git ...`. Commit je Task, Message endet mit der Co-Authored-By-Zeile. Bei `fatal error - add_item` (git-bash Heredoc) den `git commit` **1× wiederholen**.

---

## Task 0: Pre-Flight (Baseline grün)

**Ziel:** Beweisen, dass Build + beide Test-Suiten VOR jeder Änderung grün sind und HEAD stimmt.

**Files:** (keine Änderung)

- [ ] HEAD prüfen:
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git rev-parse --short HEAD && git status --porcelain
  ```
  Erwartet: `14bde26` (oder neuer, falls Task-Commits schon liefen) und ein sauberer/erwarteter Arbeitsbaum.
- [ ] Build:
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet build IdealAkeWms.slnx
  ```
  Erwartet: `Build succeeded.` mit `0 Error(s)`.
- [ ] Service-Tests + Web-Tests:
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IDEALAKEWMSService.Tests --nologo && dotnet test IdealAkeWms.Tests --nologo
  ```
  Erwartet: beide `Passed!  - Failed: 0, ...`. (Service-Baseline enthält die 14 bestehenden `LagerbestandSyncServiceTests`.)
- [ ] KEIN Commit (reine Verifikation).

---

## Task 1: Reiner Planer-Helfer `LagerbestandZeroingPlanner` (TDD)

**Ziel:** Reine Entscheidungslogik + Records anlegen, zuerst über 5 Unit-Tests getrieben.

**Files:**
- `IDEALAKEWMSService.Tests/Services/LagerbestandZeroingPlannerTests.cs` (**neu**)
- `IDEALAKEWMSService/Services/LagerbestandZeroingPlanner.cs` (**neu**)

### Signatur (verbindlich)

```csharp
public sealed record LagerbestandZeroingPlan(
    IReadOnlyList<(int ArticleId, int StorageLocationId, decimal WmsBestand)> ToZero,
    bool Skipped,
    string? SkipReason);

public static LagerbestandZeroingPlan Plan(
    int sageRowCountRaw,
    IReadOnlySet<(int ArticleId, int StorageLocationId)> sagePresentKeys,
    IReadOnlyDictionary<(int ArticleId, int StorageLocationId), decimal> managedStock,
    int maxPerRun)
```

### Steps

- [ ] **Failing test schreiben** — Datei `IDEALAKEWMSService.Tests/Services/LagerbestandZeroingPlannerTests.cs`:
  ```csharp
  using System.Collections.Generic;
  using FluentAssertions;
  using IDEALAKEWMSService.Services;
  using Xunit;

  namespace IDEALAKEWMSService.Tests.Services;

  public class LagerbestandZeroingPlannerTests
  {
      // (a) Leerer Sage-Read -> Skip "leer", selbst wenn managedStock verwaiste Paare hat.
      [Fact]
      public void Plan_EmptySageRead_Skips_EvenWithOrphans()
      {
          var present = new HashSet<(int, int)>();
          var managed = new Dictionary<(int, int), decimal> { [(1, 1)] = 5m };

          var plan = LagerbestandZeroingPlanner.Plan(
              sageRowCountRaw: 0, present, managed, maxPerRun: 100);

          plan.Skipped.Should().BeTrue();
          plan.SkipReason.Should().Contain("leer");
          plan.ToZero.Should().BeEmpty();
      }

      // (b) Verwaistes Paar (in managedStock, nicht in sagePresentKeys) -> ToZero (mit Menge).
      [Fact]
      public void Plan_OrphanPair_GoesToZero_WithQuantity()
      {
          var present = new HashSet<(int, int)> { (1, 1) };
          var managed = new Dictionary<(int, int), decimal> { [(1, 1)] = 5m, [(2, 2)] = 3m };

          var plan = LagerbestandZeroingPlanner.Plan(
              sageRowCountRaw: 1, present, managed, maxPerRun: 100);

          plan.Skipped.Should().BeFalse();
          plan.ToZero.Should().ContainSingle();
          plan.ToZero[0].Should().Be((2, 2, 3m));
      }

      // (c) Paar in sagePresentKeys -> NICHT in ToZero.
      [Fact]
      public void Plan_PairPresentInSage_NotZeroed()
      {
          var present = new HashSet<(int, int)> { (1, 1) };
          var managed = new Dictionary<(int, int), decimal> { [(1, 1)] = 5m };

          var plan = LagerbestandZeroingPlanner.Plan(
              sageRowCountRaw: 1, present, managed, maxPerRun: 100);

          plan.Skipped.Should().BeFalse();
          plan.ToZero.Should().BeEmpty();
      }

      // (d) Kandidaten > Cap -> Skip "Cap".
      [Fact]
      public void Plan_CandidatesExceedCap_Skips()
      {
          var present = new HashSet<(int, int)>();
          var managed = new Dictionary<(int, int), decimal>
          {
              [(1, 1)] = 1m, [(2, 2)] = 1m, [(3, 3)] = 1m
          };

          var plan = LagerbestandZeroingPlanner.Plan(
              sageRowCountRaw: 5, present, managed, maxPerRun: 2);

          plan.Skipped.Should().BeTrue();
          plan.SkipReason.Should().Contain("Cap");
          plan.ToZero.Should().BeEmpty();
      }

      // (e) Kandidaten == Cap -> NICHT skipped.
      [Fact]
      public void Plan_CandidatesEqualCap_NotSkipped()
      {
          var present = new HashSet<(int, int)>();
          var managed = new Dictionary<(int, int), decimal> { [(1, 1)] = 1m, [(2, 2)] = 1m };

          var plan = LagerbestandZeroingPlanner.Plan(
              sageRowCountRaw: 5, present, managed, maxPerRun: 2);

          plan.Skipped.Should().BeFalse();
          plan.ToZero.Should().HaveCount(2);
      }
  }
  ```
- [ ] **Rot beweisen** (Helfer existiert noch nicht → Kompilierfehler zählt als „rot"):
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IDEALAKEWMSService.Tests --nologo --filter "FullyQualifiedName~LagerbestandZeroingPlannerTests"
  ```
  Erwartet: **Build-/Testfehler** (`LagerbestandZeroingPlanner` nicht gefunden).
- [ ] **Implementieren** — Datei `IDEALAKEWMSService/Services/LagerbestandZeroingPlanner.cs`:
  ```csharp
  using System.Collections.Generic;
  using System.Linq;

  namespace IDEALAKEWMSService.Services;

  /// <summary>
  /// Ergebnis des Nullsetz-Abgleichs. <see cref="ToZero"/> traegt je Eintrag den zu
  /// nullenden WMS-Bestand mit (fuer die Korrekturbuchung). Bei <see cref="Skipped"/>=true
  /// wird KEINE Nullkorrektur geschrieben (Guard/Cap).
  /// </summary>
  public sealed record LagerbestandZeroingPlan(
      IReadOnlyList<(int ArticleId, int StorageLocationId, decimal WmsBestand)> ToZero,
      bool Skipped,
      string? SkipReason);

  /// <summary>
  /// Reine Nullsetz-Logik (voll unit-testbar, keine DB). Findet WMS-verwaltete Bestaende
  /// (Sage-Quelle + aktiv + Menge != 0), deren (Artikel, Lagerplatz)-Paar nicht mehr im
  /// aktuellen Sage-Snapshot vorkommt, und liefert die auf 0 zu korrigierenden Paare.
  /// Leer-Guard (leerer Sage-Read) + Cap schuetzen vor Massen-Nullsetzen bei Sage-Teil-/Ausfaellen.
  /// </summary>
  public static class LagerbestandZeroingPlanner
  {
      public static LagerbestandZeroingPlan Plan(
          int sageRowCountRaw,
          IReadOnlySet<(int ArticleId, int StorageLocationId)> sagePresentKeys,
          IReadOnlyDictionary<(int ArticleId, int StorageLocationId), decimal> managedStock,
          int maxPerRun)
      {
          // Guard: leerer Sage-Read -> nichts anfassen (Sage-Ausfall/Teil-Read).
          if (sageRowCountRaw == 0)
          {
              return new LagerbestandZeroingPlan(
                  Array.Empty<(int, int, decimal)>(),
                  Skipped: true,
                  SkipReason: "Sage-Read leer (0 Zeilen)");
          }

          // Kandidaten: managedStock-Keys, die NICHT im Sage-Snapshot vorkommen.
          var candidates = managedStock
              .Where(kv => !sagePresentKeys.Contains(kv.Key))
              .Select(kv => (kv.Key.ArticleId, kv.Key.StorageLocationId, kv.Value))
              .ToList();

          // Cap: zu viele Kandidaten -> kein Nullsetzen (Schutz vor Sage-Teil-Read).
          if (candidates.Count > maxPerRun)
          {
              return new LagerbestandZeroingPlan(
                  Array.Empty<(int, int, decimal)>(),
                  Skipped: true,
                  SkipReason: $"Cap ueberschritten: {candidates.Count} > {maxPerRun} — kein Nullsetzen");
          }

          return new LagerbestandZeroingPlan(candidates, Skipped: false, SkipReason: null);
      }
  }
  ```
- [ ] **Grün beweisen:**
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IDEALAKEWMSService.Tests --nologo --filter "FullyQualifiedName~LagerbestandZeroingPlannerTests"
  ```
  Erwartet: `Passed!  - Failed: 0, Passed: 5`.
- [ ] **Commit:**
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IDEALAKEWMSService/Services/LagerbestandZeroingPlanner.cs IDEALAKEWMSService.Tests/Services/LagerbestandZeroingPlannerTests.cs && git commit -F - <<'EOF'
  test(service): LagerbestandZeroingPlanner + 5 Unit-Tests (Leer/Verwaist/Present/Cap)

  Reiner Planer-Helfer (Vorbild ProductionOrderReconciler) mit Leer-Guard,
  Cap und Kandidaten-Auswahl. Noch nicht im Service verdrahtet.

  Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
  EOF
  ```
  (Bei `fatal error - add_item` den `git commit` 1× wiederholen.)

---

## Task 2: Katalog-Key `Sync:LagerbestandNullsetzenMaxPerRun` (TDD über Drift-Guard)

**Ziel:** Neuen Int-Setting-Key (Default `100`) im typisierten Katalog + `appsettings.json` (Service) + Drift-Guard-InlineData.

**Files:**
- `IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs:98` (neue `[InlineData]`-Zeile)
- `IdealAkeWms/Models/ServiceSettingDefinitions.cs:28` (neue Katalog-Zeile)
- `IDEALAKEWMSService/appsettings.json:31` (neuer Sync-Default)

**Hinweis (verifiziert):** Der Drift-Guard-Test `All_ContainsDocumentedServiceReadKey` ist eine `[Theory]`
mit `[InlineData]` je **dokumentiertem, service-gelesenem** Key; er prüft `All.Should().Contain(key)`.
Es gibt **keinen** Umkehr-Test (Katalog ⊆ Dokumentiert). Ein neuer Katalog-Key ohne InlineData bräche
den Test NICHT — aber weil der Service den Key real liest, gehört er in die dokumentierte Liste. Wir nutzen
das für einen sauberen TDD-Mikrozyklus: **erst InlineData (Rot: Katalog hat den Key noch nicht), dann Katalog (Grün)**.

### Steps

- [ ] **Failing test schreiben** — in `IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs` direkt nach Zeile 75 (`[InlineData("Sync:LagerbestandIntervalMinutes")]`) einfügen:
  ```csharp
      [InlineData("Sync:LagerbestandNullsetzenMaxPerRun")]
  ```
- [ ] **Rot beweisen:**
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~ServiceSettingDefinitionsTests"
  ```
  Erwartet: **1 fehlgeschlagener** `All_ContainsDocumentedServiceReadKey`-Fall (`Expected ... to contain "Sync:LagerbestandNullsetzenMaxPerRun"`).
- [ ] **Implementieren (Katalog)** — in `IdealAkeWms/Models/ServiceSettingDefinitions.cs` direkt nach Zeile 28 (`new("Sync:LagerbestandIntervalMinutes", ...)`) einfügen:
  ```csharp
          new("Sync:LagerbestandNullsetzenMaxPerRun", ServiceSettingType.Int, "100",   "Sync", "Sicherheits-Cap: mehr in Sage verschwundene Bestand-Paare je Lauf -> kein Nullsetzen + Fehlermail"),
  ```
- [ ] **Implementieren (appsettings.json)** — in `IDEALAKEWMSService/appsettings.json` im `"Sync"`-Block nach der Zeile `"LagerbestandIntervalMinutes": 0,` einfügen:
  ```json
      "LagerbestandNullsetzenMaxPerRun": 100,
  ```
- [ ] **Grün beweisen** (Katalog-Konsistenz + Drift-Guard):
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~ServiceSettingDefinitionsTests"
  ```
  Erwartet: `Passed!  - Failed: 0, ...` (alle Fälle inkl. `All_HasNoDuplicateKeys`, `All_IntDefaults_ParseAsInt`).
- [ ] **Commit:**
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IdealAkeWms/Models/ServiceSettingDefinitions.cs IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs IDEALAKEWMSService/appsettings.json && git commit -F - <<'EOF'
  feat(config): Sync:LagerbestandNullsetzenMaxPerRun (Int, Default 100)

  Neuer typisierter Katalog-Key + appsettings-Default + Drift-Guard-InlineData
  fuer den Nullsetz-Cap des Lagerbestand-Syncs.

  Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
  EOF
  ```

---

## Task 3: `LagerbestandSyncService` — Zeroing-Block verdrahten (TDD)

**Ziel:** ctor um `IConfiguration` + `ISyncErrorNotifier` erweitern (ISyncLogger bleibt **letzter** Parameter),
Zeroing-Block nach der Korrektur-Schleife einfügen, `Build()`-Helper anpassen, 6 neue Service-Tests (f–k).

**Files:**
- `IDEALAKEWMSService.Tests/Services/LagerbestandSyncServiceTests.cs` (Helper-Update + 6 neue Tests)
- `IDEALAKEWMSService/Services/LagerbestandSyncService.cs` (ctor + Zeroing-Block + `nullgesetzt`-Count)

### 3a. Test-Helper anpassen + neue Tests schreiben (Rot)

- [ ] **Usings ergänzen** — oben in `LagerbestandSyncServiceTests.cs` zu den bestehenden `using`s hinzufügen:
  ```csharp
  using Microsoft.Extensions.Configuration;
  using Moq;
  ```
- [ ] **`Build()`-Helper anpassen + `BuildWithNotifier()` ergänzen** — den bestehenden `Build()`-Block (Zeilen 15–25) ersetzen durch:
  ```csharp
      private static (LagerbestandSyncService service, FakeSageBestandReader reader,
                      IdealAkeWms.Data.ApplicationDbContext ctx, FakeSyncLogger fakeLogger)
          Build()
      {
          var (service, reader, ctx, fakeLogger, _) = BuildWithNotifier();
          return (service, reader, ctx, fakeLogger);
      }

      private static (LagerbestandSyncService service, FakeSageBestandReader reader,
                      IdealAkeWms.Data.ApplicationDbContext ctx, FakeSyncLogger fakeLogger,
                      Mock<ISyncErrorNotifier> notifier)
          BuildWithNotifier()
      {
          var ctx = TestDbContextFactory.Create();
          var reader = new FakeSageBestandReader();
          var fakeLogger = new FakeSyncLogger();
          var stockRepo = new StockMovementRepository(ctx);
          var config = new ConfigurationBuilder().Build();   // kein Connection-String -> Cap faellt auf Default 100
          var notifier = new Mock<ISyncErrorNotifier>();
          var service = new LagerbestandSyncService(
              ctx, reader, stockRepo, config, notifier.Object,
              NullLogger<LagerbestandSyncService>.Instance, fakeLogger);
          return (service, reader, ctx, fakeLogger, notifier);
      }
  ```
  (Die 14 bestehenden Tests nutzen `Build()` weiter — Signatur/Return unverändert.)
- [ ] **6 neue Tests** ans Ende der Klasse (vor der schließenden `}`) einfügen:
  ```csharp
      // (f) Verwaistes Paar mit WMS > 0 auf Sage-aktivem Platz -> genau eine SageAusbuchung auf 0.
      [Fact]
      public async Task Run_OrphanPairWithStock_BooksSageAusbuchungToZero()
      {
          var (svc, reader, ctx, fakeLogger) = Build();
          SeedArticle(ctx, id: 1, number: "A-1");
          SeedSageLocation(ctx, id: 1, code: "L-1");   // verwaist (nicht in Sage-Read)
          SeedSageLocation(ctx, id: 2, code: "L-P");   // in Sage-Read vorhanden
          ctx.StockMovements.Add(new StockMovement
          {
              ArticleId = 1, StorageLocationId = 1, Quantity = 5m,
              MovementType = MovementType.Einbuchung, Timestamp = DateTime.Now,
              WindowsUser = "tester", CreatedAt = DateTime.Now,
              CreatedBy = "tester", CreatedByWindows = "tester"
          });
          await ctx.SaveChangesAsync();
          // Sage-Snapshot: nur (A-1, L-P), Bestand 0 (present, noChange). (A-1, L-1) fehlt -> verwaist.
          reader.Records = new() { new("A-1", "L-P", 0m) };

          var result = await svc.RunAsync(dryRun: false);

          var zeroing = ctx.StockMovements
              .Where(m => m.MovementType == MovementType.SageAusbuchung).ToList();
          zeroing.Should().ContainSingle();
          zeroing[0].StorageLocationId.Should().Be(1);
          zeroing[0].Quantity.Should().Be(5m);
          zeroing[0].WindowsUser.Should().Be(SyncUser);
          zeroing[0].Note.Should().Contain("auf 0 gesetzt");
          fakeLogger.Runs[0].FinalCounts!["nullgesetzt"].Should().Be(1);
          result.CorrectionsMinus.Should().Be(0); // Zeroing zaehlt NICHT als Korrektur
      }

      // (g) Leerer Sage-Read -> keine Nullbuchung, Warn "leer", kein NotifyAsync.
      [Fact]
      public async Task Run_EmptySageRead_NoZeroing_LogsLeerWarning()
      {
          var (svc, reader, ctx, fakeLogger, notifier) = BuildWithNotifier();
          SeedArticle(ctx, id: 1, number: "A-1");
          SeedSageLocation(ctx, id: 1, code: "L-1");
          ctx.StockMovements.Add(new StockMovement
          {
              ArticleId = 1, StorageLocationId = 1, Quantity = 5m,
              MovementType = MovementType.Einbuchung, Timestamp = DateTime.Now,
              WindowsUser = "tester", CreatedAt = DateTime.Now,
              CreatedBy = "tester", CreatedByWindows = "tester"
          });
          await ctx.SaveChangesAsync();
          reader.Records = new();   // leerer Read

          await svc.RunAsync(dryRun: false);

          ctx.StockMovements.Where(m => m.MovementType >= MovementType.SageEinbuchung)
              .Should().BeEmpty();
          fakeLogger.Runs[0].FinalCounts!["nullgesetzt"].Should().Be(0);
          fakeLogger.Runs[0].Events.Should().Contain(e =>
              e.Level == "Warning" && e.Message.Contains("leer"));
          notifier.Verify(n => n.NotifyAsync(
              It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Never);
      }

      // (h) Cap ueberschritten -> keine Nullbuchung + NotifyAsync aufgerufen.
      // Hinweis: ServiceSettings liest aus der DB, in Tests faellt der Cap immer auf Default 100 zurueck
      // -> 101 verwaiste Paare noetig, um den Cap zu ueberschreiten.
      [Fact]
      public async Task Run_CapExceeded_NoZeroing_CallsNotify()
      {
          var (svc, reader, ctx, fakeLogger, notifier) = BuildWithNotifier();
          SeedArticle(ctx, id: 1, number: "A-1");
          for (int i = 1; i <= 101; i++)
          {
              SeedSageLocation(ctx, id: i, code: $"L-{i}");
              ctx.StockMovements.Add(new StockMovement
              {
                  ArticleId = 1, StorageLocationId = i, Quantity = 1m,
                  MovementType = MovementType.Einbuchung, Timestamp = DateTime.Now,
                  WindowsUser = "tester", CreatedAt = DateTime.Now,
                  CreatedBy = "tester", CreatedByWindows = "tester"
              });
          }
          SeedSageLocation(ctx, id: 200, code: "L-P");   // present, damit Read nicht leer
          await ctx.SaveChangesAsync();
          reader.Records = new() { new("A-1", "L-P", 0m) };   // 101 Paare (L-1..L-101) verwaist

          await svc.RunAsync(dryRun: false);

          ctx.StockMovements.Where(m => m.MovementType >= MovementType.SageEinbuchung)
              .Should().BeEmpty();
          fakeLogger.Runs[0].FinalCounts!["nullgesetzt"].Should().Be(0);
          fakeLogger.Runs[0].Events.Should().Contain(e =>
              e.Level == "Warning" && e.Message.Contains("Cap"));
          notifier.Verify(n => n.NotifyAsync(
              It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Once);
      }

      // (i) Verwaistes Paar auf manuellem/inaktivem Platz -> NICHT genullt.
      [Fact]
      public async Task Run_OrphanOnManualOrInactiveLocation_NotZeroed()
      {
          var (svc, reader, ctx, _) = Build();
          SeedArticle(ctx, id: 1, number: "A-1");
          ctx.StorageLocations.Add(new StorageLocation
          {
              Id = 1, Code = "MAN-1", BarcodeValue = "MAN-1",
              Source = StorageLocationSource.Manual, IsActive = true,
              IsPickingTransport = false, CreatedBy = "t", CreatedByWindows = "t"
          });
          SeedSageLocation(ctx, id: 2, code: "INACT-1", isActive: false);
          SeedSageLocation(ctx, id: 3, code: "L-P");
          ctx.StockMovements.AddRange(
              new StockMovement { ArticleId = 1, StorageLocationId = 1, Quantity = 5m, MovementType = MovementType.Einbuchung, Timestamp = DateTime.Now, WindowsUser = "t", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" },
              new StockMovement { ArticleId = 1, StorageLocationId = 2, Quantity = 5m, MovementType = MovementType.Einbuchung, Timestamp = DateTime.Now, WindowsUser = "t", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" }
          );
          await ctx.SaveChangesAsync();
          reader.Records = new() { new("A-1", "L-P", 0m) };

          await svc.RunAsync(dryRun: false);

          ctx.StockMovements.Where(m => m.MovementType >= MovementType.SageEinbuchung)
              .Should().BeEmpty();
      }

      // (j) Duplikat-Key in Sage -> NICHT genullt (Dups sind in Sage vorhanden, nur mehrdeutig).
      [Fact]
      public async Task Run_DuplicateSageKey_NotZeroed()
      {
          var (svc, reader, ctx, fakeLogger) = Build();
          SeedArticle(ctx, id: 1, number: "A-1");
          SeedSageLocation(ctx, id: 1, code: "L-1");
          ctx.StockMovements.Add(new StockMovement
          {
              ArticleId = 1, StorageLocationId = 1, Quantity = 5m,
              MovementType = MovementType.Einbuchung, Timestamp = DateTime.Now,
              WindowsUser = "t", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
          });
          await ctx.SaveChangesAsync();
          // (A-1, L-1) doppelt -> Korrektur-Schleife ueberspringt (Warn "mehrfach"),
          // aber sagePresentKeys (aus Roh-Zeilen) enthaelt (A-1, L-1) -> KEIN Nullsetzen.
          reader.Records = new() { new("A-1", "L-1", 5m), new("A-1", "L-1", 7m) };

          await svc.RunAsync(dryRun: false);

          ctx.StockMovements.Where(m => m.MovementType >= MovementType.SageEinbuchung)
              .Should().BeEmpty();
          fakeLogger.Runs[0].FinalCounts!["nullgesetzt"].Should().Be(0);
          fakeLogger.Runs[0].Events.Should().Contain(e =>
              e.Level == "Warning" && e.Message.Contains("mehrfach"));
      }

      // (k) DryRun -> keine Writes, aber Count + Log.
      [Fact]
      public async Task Run_DryRun_NoZeroingWrites_ButCounts()
      {
          var (svc, reader, ctx, fakeLogger) = Build();
          SeedArticle(ctx, id: 1, number: "A-1");
          SeedSageLocation(ctx, id: 1, code: "L-1");   // verwaist
          SeedSageLocation(ctx, id: 2, code: "L-P");
          ctx.StockMovements.Add(new StockMovement
          {
              ArticleId = 1, StorageLocationId = 1, Quantity = 5m,
              MovementType = MovementType.Einbuchung, Timestamp = DateTime.Now,
              WindowsUser = "t", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
          });
          await ctx.SaveChangesAsync();
          reader.Records = new() { new("A-1", "L-P", 0m) };

          await svc.RunAsync(dryRun: true);

          ctx.StockMovements.Where(m => m.MovementType == MovementType.SageAusbuchung)
              .Should().BeEmpty();   // KEIN Write
          fakeLogger.Runs[0].FinalCounts!["nullgesetzt"].Should().Be(1);   // Count trotzdem
      }
  ```
- [ ] **Rot beweisen** (ctor-Signatur passt noch nicht + Zeroing fehlt → Kompilier-/Testfehler):
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IDEALAKEWMSService.Tests --nologo --filter "FullyQualifiedName~LagerbestandSyncServiceTests"
  ```
  Erwartet: **Build-Fehler** (ctor erwartet 5 Argumente) — das ist das erwartete Rot.

### 3b. Service implementieren (Grün)

- [ ] **Usings ergänzen** — oben in `IDEALAKEWMSService/Services/LagerbestandSyncService.cs` hinzufügen:
  ```csharp
  using IDEALAKEWMSService.Common;
  using Microsoft.Extensions.Configuration;
  ```
- [ ] **Felder + ctor** — den Feld-Block (Zeilen 14–18) und den ctor (Zeilen 20–32) ersetzen durch:
  ```csharp
      private readonly ApplicationDbContext _ctx;
      private readonly ISageBestandReader _reader;
      private readonly IStockMovementRepository _stockRepo;
      private readonly IConfiguration _config;
      private readonly ISyncErrorNotifier _errorNotifier;
      private readonly ILogger<LagerbestandSyncService> _logger;
      private readonly ISyncLogger _syncLogger;

      public LagerbestandSyncService(
          ApplicationDbContext ctx,
          ISageBestandReader reader,
          IStockMovementRepository stockRepo,
          IConfiguration config,
          ISyncErrorNotifier errorNotifier,
          ILogger<LagerbestandSyncService> logger,
          ISyncLogger syncLogger)
      {
          _ctx = ctx;
          _reader = reader;
          _stockRepo = stockRepo;
          _config = config;
          _errorNotifier = errorNotifier;
          _syncLogger = syncLogger;
          _logger = logger;
      }
  ```
- [ ] **Roh-Zeilen erhalten** — direkt nach dem Read-`try/catch` (nach der schließenden `}` der Zeile 59, also unmittelbar vor dem Kommentar `// Sage-Duplikate erkennen:` in Zeile 61) einfügen:
  ```csharp
              // Roh-Zeilen (VOR Dedup) fuer den Nullsetz-Abgleich festhalten: sagePresentKeys
              // muss auch Duplikat-Keys enthalten (die sind in Sage vorhanden, nur mehrdeutig).
              var rawSageRows = sageRows;
              var sageRowCountRaw = rawSageRows.Count;
  ```
  (Zeile 77–80 weist `sageRows` einer neuen, deduplizierten Liste zu — `rawSageRows` behält per Referenz die Roh-Liste.)
- [ ] **sagePresentKeys bauen** — direkt nach dem `wmsStock`-Load (nach Zeile 90 `var wmsStock = await _stockRepo.GetCurrentStockByArticleAndLocationAsync();`) einfügen:
  ```csharp
              // "In-Sage-vorhanden"-Set aus den ROH-Zeilen (inkl. Duplikat-Keys + inaktive/manuelle
              // Plaetze — schaden nicht, sind ohnehin keine Nullsetz-Kandidaten).
              var sagePresentKeys = new HashSet<(int ArticleId, int StorageLocationId)>();
              foreach (var raw in rawSageRows)
              {
                  if (string.IsNullOrWhiteSpace(raw.Artikelnummer) || string.IsNullOrWhiteSpace(raw.Lagerplatz))
                      continue;
                  if (!articleByNumber.TryGetValue(raw.Artikelnummer, out var presentArticleId))
                      continue;
                  if (!locationByCode.TryGetValue(raw.Lagerplatz, out var presentLoc))
                      continue;
                  sagePresentKeys.Add((presentArticleId, presentLoc.Id));
              }
  ```
- [ ] **Zeroing-Block** — direkt nach dem Ende der Korrektur-Schleife (nach der schließenden `}` von `foreach (var dto in sageRows)`, Zeile 163) und VOR `if (!dryRun) await _ctx.SaveChangesAsync(ct);` (Zeile 165) einfügen:
  ```csharp
              // ---- Nullsetzen verwaister Bestaende (in Sage verschwundene Paare) ----------
              // managedStock = wmsStock gefiltert auf Sage-Quelle + aktiv + Menge != 0.
              // (GetCurrentStockByArticleAndLocationAsync enthaelt auch Netto-0-Paare UND
              //  Umbuchungs-Quellseiten-Keys -> beides MUSS raus.)
              var locationInfoById = locationByCode.Values
                  .ToDictionary(v => v.Id, v => (v.Source, v.IsActive));
              var managedStock = wmsStock
                  .Where(kv => kv.Value != 0m
                            && locationInfoById.TryGetValue(kv.Key.StorageLocationId, out var li)
                            && li.Source == StorageLocationSource.Sage
                            && li.IsActive)
                  .ToDictionary(kv => kv.Key, kv => kv.Value);

              var maxPerRun = await ServiceSettings.GetIntSafeAsync(
                  _config, "Sync:LagerbestandNullsetzenMaxPerRun", 100, ct);

              var zeroPlan = LagerbestandZeroingPlanner.Plan(
                  sageRowCountRaw, sagePresentKeys, managedStock, maxPerRun);

              int nullgesetzt = 0;
              if (zeroPlan.Skipped)
              {
                  var reason = zeroPlan.SkipReason ?? "unbekannt";
                  await run.LogWarningAsync($"Nullsetzen uebersprungen: {reason}", ct: ct);
                  _logger.LogWarning("Lagerbestand-Nullsetzen uebersprungen: {Reason}", reason);

                  // Cap-Skip zusaetzlich per Fehlermail (Sage-Teil-Read-Verdacht). Guard (leer) NICHT.
                  if (reason.StartsWith("Cap", StringComparison.OrdinalIgnoreCase))
                  {
                      await _errorNotifier.NotifyAsync(
                          "Lagerbestand-Nullsetzen: Cap ueberschritten",
                          new InvalidOperationException(
                              $"Lagerbestand-Nullsetzen Cap ueberschritten: {reason}. " +
                              $"Sage lieferte {sageRowCountRaw} Zeilen, Cap={maxPerRun}. " +
                              $"Kein Nullsetzen geschrieben — moeglicher Sage-Teil-Read."),
                          ct);
                  }
              }
              else
              {
                  // Reverse-Lookups fuer lesbare Detailzeilen (Artikel-Nummer / Lagerplatz-Code).
                  var articleNumberById = articleByNumber.ToDictionary(kv => kv.Value, kv => kv.Key);
                  var codeById = locationByCode.ToDictionary(kv => kv.Value.Id, kv => kv.Key);
                  int infoLines = 0;

                  foreach (var (articleId, locId, wmsBestand) in zeroPlan.ToZero)
                  {
                      if (!dryRun)
                      {
                          _ctx.StockMovements.Add(new StockMovement
                          {
                              ArticleId = articleId,
                              StorageLocationId = locId,
                              Quantity = Math.Abs(wmsBestand),
                              MovementType = wmsBestand > 0 ? MovementType.SageAusbuchung : MovementType.SageEinbuchung,
                              Note = $"Sage-Korrektur: in Sage nicht mehr vorhanden -> auf 0 gesetzt (WMS war {wmsBestand})",
                              Timestamp = DateTime.Now,
                              UserId = null,
                              WindowsUser = SyncUser,
                              CreatedAt = DateTime.Now,
                              CreatedBy = SyncUser,
                              CreatedByWindows = Environment.MachineName
                          });
                      }

                      nullgesetzt++;
                      if (infoLines < 100)
                      {
                          var articleNumber = articleNumberById.GetValueOrDefault(articleId, articleId.ToString());
                          var code = codeById.GetValueOrDefault(locId, locId.ToString());
                          await run.LogInfoAsync(
                              $"Bestand auf 0 gesetzt: {articleNumber} @ {code} (WMS war {wmsBestand})",
                              reference: code, ct: ct);
                          infoLines++;
                      }
                  }

                  if (nullgesetzt > 0)
                      _logger.LogInformation("Lagerbestand-Nullsetzen: {Count} Paar(e) auf 0 gesetzt.", nullgesetzt);
              }
              // ---- Ende Nullsetzen ---------------------------------------------------------
  ```
- [ ] **Counts-Dict erweitern** — im `FinishSuccessAsync`-Aufruf (Zeilen 167–173) das Counts-Dict um `nullgesetzt` ergänzen:
  ```csharp
              await run.FinishSuccessAsync(new Dictionary<string, int>
              {
                  ["einbuchungen"] = plus,
                  ["ausbuchungen"] = minus,
                  ["nullgesetzt"] = nullgesetzt,
                  ["uebersprungen"] = skipped,
                  ["fehler"] = errors,
              }, messageSuffix: dryRun ? "[DryRun]" : null, ct: ct);
  ```
- [ ] **Grün beweisen** (neue + 14 bestehende Service-Tests):
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IDEALAKEWMSService.Tests --nologo --filter "FullyQualifiedName~LagerbestandSyncServiceTests"
  ```
  Erwartet: `Passed!  - Failed: 0, Passed: 20` (14 bestehende + 6 neue).
- [ ] **DI-Registrierung prüfen (kein Code-Change)** — verifizieren, dass `Program.cs:57` unverändert korrekt ist (Typ-Registrierung löst `IConfiguration` + `ISyncErrorNotifier` automatisch auf). Voller Service-Build:
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj
  ```
  Erwartet: `Build succeeded.` `0 Error(s)`. (Kein Edit an `Program.cs` — beide Deps sind bereits registriert.)
- [ ] **Commit:**
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IDEALAKEWMSService/Services/LagerbestandSyncService.cs IDEALAKEWMSService.Tests/Services/LagerbestandSyncServiceTests.cs && git commit -F - <<'EOF'
  fix(service): Lagerbestand-Sync nullt in Sage verschwundene Bestaende

  Nach der Korrektur-Schleife werden (Artikel, Sage-aktiver Lagerplatz)-Paare
  mit WMS-Bestand != 0, deren Paar nicht mehr im Sage-Snapshot vorkommt, auf 0
  korrigiert (WHEN NOT MATCHED BY SOURCE). Leer-Guard + Cap (Fehlermail) via
  LagerbestandZeroingPlanner. sagePresentKeys aus Roh-Zeilen (inkl. Dups),
  managedStock filtert Netto-0 + Umbuchungs-Quellseiten raus. ctor um
  IConfiguration + ISyncErrorNotifier erweitert (ISyncLogger bleibt letzter).

  Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
  EOF
  ```

---

## Task 4: Dokumentation + Final-Check

**Ziel:** Changelog (v1.25.0-Bullet), CLAUDE.md-Fallstrick, TESTSZENARIEN (neues Kapitel 53), PROJECT_STATUS
aktualisieren; Gesamt-Build + beide Test-Suiten grün; kein AppVersion-Bump / keine Migration bestätigen.

**Files:**
- `IdealAkeWms/Views/Help/Changelog.cshtml` (v1.25.0-`<ul>`, nach der letzten `<li>` des v1.25.0-Blocks)
- `CLAUDE.md` (neuer Fallstrick-Bullet)
- `docs/TESTSZENARIEN.md` (neues `## Kapitel 53`)
- `PROJECT_STATUS.md` (kurzer Vermerk)

### Steps

- [ ] **Changelog** — in `IdealAkeWms/Views/Help/Changelog.cshtml` im **v1.25.0**-`<ul>` (beginnt Zeile 16) eine weitere `<li>` ergänzen (z. B. direkt nach der `hauptlagerplatz_fehlt`-`</li>`):
  ```html
                      <li><strong>Lagerbestand-Korrektur vollst&auml;ndiger:</strong> F&auml;llt der
                          Sage-Bestand eines Artikels auf einem Lagerplatz auf 0 (Sage liefert die Zeile
                          dann gar nicht mehr), setzt der Lagerbestand-Sync den WMS-Bestand jetzt
                          automatisch ebenfalls auf 0 &mdash; nur f&uuml;r Sage-Lagerpl&auml;tze
                          (aktiv), gesch&uuml;tzt durch einen Sicherheits-Cap
                          (<code>Sync:LagerbestandNullsetzenMaxPerRun</code>, Default 100) mit Fehlermail
                          bei Verdacht auf einen Sage-Teil-Read.</li>
  ```
- [ ] **CLAUDE.md** — im Abschnitt „Bekannte Fallstricke" (nach dem `MovementType-Aggregation`-Bullet oder am Ende der Liste) einen Bullet ergänzen:
  ```markdown
  - **Lagerbestand-Nullsetzen verwaister Paare (v1.25.0)**: `LagerbestandSyncService` nullt nach der Korrektur-Schleife `(Artikel, Sage-aktiver Lagerplatz)`-Paare, die aus dem Sage-Snapshot verschwunden sind (Sage liefert 0-Bestand-Zeilen nicht mehr). Reine Entscheidung im unit-getesteten `LagerbestandZeroingPlanner.Plan` (Leer-Guard: `sageRowCountRaw==0` → Skip; Cap: `> Sync:LagerbestandNullsetzenMaxPerRun` (Default 100) → Skip + `ISyncErrorNotifier.NotifyAsync`). **Fallstricke:** (1) `sagePresentKeys` MUSS aus den **roh** gelesenen Sage-Zeilen (VOR Dedup) gebaut werden — sonst würden mehrdeutige (Duplikat-)Paare, die in Sage vorhanden sind, fälschlich genullt. (2) `managedStock` MUSS `GetCurrentStockByArticleAndLocationAsync` auf `qty != 0` UND `Source==Sage` UND `IsActive` filtern (die Methode enthält Netto-0-Paare und Umbuchungs-Quellseiten-Keys). (3) NAN + Kommissionierwagen sind `Source=Manual` → automatisch ausgeschlossen. (4) Kein neuer `MovementType` — `SageAusbuchung`/`SageEinbuchung` wiederverwendet (Vorzeichen: `delta = -wmsBestand`, bei negativem Bestand `SageEinbuchung`). Counts-Key `nullgesetzt` im `Lagerbestand`-Aktivitäts-Protokoll-Lauf (kein eigener Service-Name). **Test-Fallstrick:** `ServiceSettings.GetIntSafeAsync` liest aus der DB, nicht aus IConfiguration → in InMemory-Tests fällt der Cap immer auf Default 100 → der Cap-Test seedet 101 verwaiste Paare.
  ```
- [ ] **TESTSZENARIEN** — in `docs/TESTSZENARIEN.md` am Dateiende (nach Kapitel 52) neues Kapitel anhängen:
  ```markdown

  ## Kapitel 53: Lagerbestand-Nullsetzen verwaister Paare (v1.25.0)

  **Vorbedingungen:**
  - Service läuft, `Sync:LagerbestandEnabled = true` (in `/ServiceSettings`), `WorkerSettings:SyncDryRun = false`.
  - Mindestens ein Artikel mit WMS-Bestand > 0 auf einem **Sage**-Lagerplatz (`Source=Sage`, `IsActive=true`), dessen `(Artikel, Lagerplatz)`-Paar in der Sage-Bestand-Quelle NICHT (mehr) vorkommt.

  > **Hinweis Aktivitäts-Protokoll:** Das Nullsetzen läuft im bestehenden `Lagerbestand`-Sync (kein eigener Protokoll-Eintrag). Der Counts-Schlüssel `nullgesetzt` zählt die auf 0 gesetzten Paare.

  ### 1. Verwaistes Paar wird auf 0 gesetzt
  1. Bestand für ein Sage-aktives Paar im WMS aufbauen (z. B. +5 Einbuchung), das in Sage 0 ist (Sage liefert die Zeile nicht).
  2. Sync-Zyklus auslösen.
  3. **Erwartet:** Eine `SageAusbuchung` über 5 wird gebucht (WMS-Bestand danach 0), Note „Sage-Korrektur: in Sage nicht mehr vorhanden -> auf 0 gesetzt (WMS war 5)". Der `Lagerbestand`-Eintrag zeigt Count `nullgesetzt=1` und eine Info-Detailzeile „Bestand auf 0 gesetzt: <ArtNr> @ <Code> (WMS war 5)".

  ### 2. Guard — leerer Sage-Read nullt NICHTS
  1. Sage-Bestand-Quelle liefert (simuliert) 0 Zeilen.
  2. Sync-Zyklus auslösen.
  3. **Erwartet:** KEINE Nullbuchung. Warn-Detailzeile „Nullsetzen uebersprungen: Sage-Read leer (0 Zeilen)". Keine Fehlermail (Guard ist kein Cap).

  ### 3. Cap — zu viele Kandidaten → kein Nullsetzen + Fehlermail
  1. `Sync:LagerbestandNullsetzenMaxPerRun` niedrig setzen (z. B. 1), mehr als 1 verwaistes Paar vorhanden. `ErrorNotification:Enabled=true` + Empfänger gesetzt.
  2. Sync-Zyklus auslösen.
  3. **Erwartet:** KEINE Nullbuchung. Warn-Detailzeile „Nullsetzen uebersprungen: Cap ueberschritten: N > 1 — kein Nullsetzen". Fehlermail an die Empfänger.

  ### 4. Manueller/inaktiver Lagerplatz bleibt unberührt
  1. Verwaistes Paar mit WMS-Bestand > 0 auf einem `Source=Manual`-Platz ODER einem inaktiven Sage-Platz.
  2. Sync-Zyklus auslösen.
  3. **Erwartet:** KEINE Nullbuchung für dieses Paar (nur Sage-aktive Paare werden genullt). NAN und Kommissionierwagen (Manual) sind ebenfalls ausgeschlossen.

  ### 5. Duplikat-Paar in Sage bleibt unberührt
  1. Sage liefert dasselbe `(Artikel, Lagerplatz)`-Paar mehrfach (mehrdeutig).
  2. Sync-Zyklus auslösen.
  3. **Erwartet:** Das Paar wird als Duplikat übersprungen (Warn „mehrfach"), aber NICHT genullt (es ist in Sage vorhanden, nur mehrdeutig).

  ### 6. DryRun schreibt nicht
  1. `WorkerSettings:SyncDryRun = true`, sonst wie Szenario 1.
  2. Sync-Zyklus auslösen.
  3. **Erwartet:** KEINE Nullbuchung in der DB, aber `nullgesetzt=1` im Protokoll (Simulation) + `[DryRun]`-Suffix.

  **Negativ/Regression:**
  - Bestehende Bestandskorrekturen (Delta ≠ 0 für in Sage vorhandene Paare) funktionieren unverändert.
  - Netto-0-Paare (z. B. +5/−5) werden NICHT als verwaist genullt (managedStock filtert `qty != 0`).

  ---
  ```
- [ ] **PROJECT_STATUS.md** — kurzen Vermerk im v1.25.0-/aktuellen Abschnitt ergänzen (Bugfix, service-seitig, kein Bump):
  ```markdown
  - **Bugfix Lagerbestand-Nullsetzen (v1.25.0-Fold):** Lagerbestand-Sync setzt in Sage verschwundene Bestand-Paare (Sage-aktive Lagerplätze) auf 0. Leer-Guard + Cap (`Sync:LagerbestandNullsetzenMaxPerRun`, Default 100) + Fehlermail. Kein Schema-Change, kein AppVersion-Bump.
  ```
- [ ] **Kein-Bump / Keine-Migration bestätigen:**
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && grep -n 'Version = ' IdealAkeWms/AppVersion.cs && git status --porcelain -- IdealAkeWms/Migrations IDEALAKEWMSService/Migrations SQL 2>/dev/null
  ```
  Erwartet: `Version = "1.25.0"` (unverändert) und **keine** geänderten/neuen Migrations-/SQL-Dateien.
- [ ] **Final-Check (Gesamt-Build + beide Suiten):**
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet build IdealAkeWms.slnx && dotnet test IDEALAKEWMSService.Tests --nologo && dotnet test IdealAkeWms.Tests --nologo
  ```
  Erwartet: `Build succeeded.` `0 Error(s)`; beide `Passed!  - Failed: 0, ...`.
- [ ] **Commit:**
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IdealAkeWms/Views/Help/Changelog.cshtml CLAUDE.md docs/TESTSZENARIEN.md PROJECT_STATUS.md && git commit -F - <<'EOF'
  docs: Lagerbestand-Nullsetzen (Changelog/CLAUDE/TESTSZENARIEN/PROJECT_STATUS)

  Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
  EOF
  ```

---

## Datei-Struktur (Übersicht der Änderungen)

```
IDEALAKEWMSService/
  Services/
    LagerbestandZeroingPlanner.cs          (NEU  — Task 1: reiner Planer + Record)
    LagerbestandSyncService.cs             (EDIT — Task 3: ctor + Zeroing-Block + nullgesetzt)
  appsettings.json                         (EDIT — Task 2: Sync:LagerbestandNullsetzenMaxPerRun = 100)
  Program.cs                               (UNVERÄNDERT — DI löst neue ctor-Deps automatisch auf)

IDEALAKEWMSService.Tests/
  Services/
    LagerbestandZeroingPlannerTests.cs     (NEU  — Task 1: 5 Unit-Tests a–e)
    LagerbestandSyncServiceTests.cs        (EDIT — Task 3: Build()-Update + 6 Tests f–k)

IdealAkeWms/
  Models/ServiceSettingDefinitions.cs      (EDIT — Task 2: Katalog-Zeile)
  Views/Help/Changelog.cshtml              (EDIT — Task 4: v1.25.0-Bullet)

IdealAkeWms.Tests/
  Models/ServiceSettingDefinitionsTests.cs (EDIT — Task 2: InlineData)

CLAUDE.md                                  (EDIT — Task 4: Fallstrick)
docs/TESTSZENARIEN.md                      (EDIT — Task 4: Kapitel 53)
PROJECT_STATUS.md                          (EDIT — Task 4: Vermerk)
```

---

## Fallstricke (verifiziert am echten Code)

Jeder Spec-Fallstrick ist hier gegen den realen Code bestätigt (oder korrigiert), mit Datei:Zeile-Beleg.

1. **Netto-0-Paare + Umbuchungs-Quellseiten-Keys** — **BESTÄTIGT.**
   `StockMovementRepository.GetCurrentStockByArticleAndLocationAsync` ([StockMovementRepository.cs:472-504](../../../IdealAkeWms/Data/Repositories/StockMovementRepository.cs)) summiert ALLE Bewegungen (Netto-0 bleibt als `0m`-Eintrag im Dict, Zeile 493) und addiert die Umbuchungs-Quellseite (`srcKey = (ArticleId, SourceStorageLocationId)`, Zeile 496-500). → `managedStock` filtert `kv.Value != 0m` UND `Source==Sage` UND `IsActive`. Getestet durch Service-Test (i).

2. **Duplikat-Keys müssen ins `sagePresentKeys`** — **BESTÄTIGT.**
   Die Dedup-Logik entfernt Duplikat-Tupel aus `sageRows` ([LagerbestandSyncService.cs:76-80](../../../IDEALAKEWMSService/Services/LagerbestandSyncService.cs)). Deshalb `sagePresentKeys` aus `rawSageRows` (Referenz VOR der Reassign in Zeile 77) bauen. Getestet durch Service-Test (j).

3. **Leer-Guard auf Roh-Read-Count** — **BESTÄTIGT.**
   `sageRowCountRaw = rawSageRows.Count` (vor Dedup). Der Read hat keinen Early-Return bei leer (Zeilen 41-59 fangen nur Exceptions), das Nullsetzen wird also auch bei leerem Read erreicht → Guard greift. Getestet durch Planner-Test (a) + Service-Test (g).

4. **Snapshot-Konsistenz / kein Doppel-Buchen** — **BESTÄTIGT.**
   `wmsStock` wird einmal geladen (Zeile 90) und von Korrektur-Schleife + Nullsetzen geteilt. Ein korrektur-gebuchtes Paar ist per Definition in `sagePresentKeys` (sein Roh-Row resolvet Artikel+Lagerplatz identisch) → nie Nullsetz-Kandidat.

5. **Kein neuer `MovementType`** — **BESTÄTIGT.**
   `MovementType.SageEinbuchung = 3`, `SageAusbuchung = 4` existieren ([MovementType.cs:8-9](../../../IdealAkeWms/Models/MovementType.cs)) und sind in der Aggregation berücksichtigt (Zeilen 485/488). Keine Aggregations-Änderung nötig.

6. **ctor-Erweiterung + DI-Registrierung** — **KORRIGIERT (kein Code-Change nötig).**
   Registrierung ist `AddScoped<ILagerbestandSyncService, LagerbestandSyncService>()` ([Program.cs:57](../../../IDEALAKEWMSService/Program.cs)) — reine Typ-Registrierung. `IConfiguration` ist im Host immer registriert; `ISyncErrorNotifier` ist an [Program.cs:64](../../../IDEALAKEWMSService/Program.cs) registriert. DI löst beide neuen ctor-Args automatisch auf → **`Program.cs` bleibt unverändert** (die Spec-Formulierung „DI-Registrierung nachziehen" ist hier gegenstandslos; nur der `new LagerbestandSyncService(...)`-Aufruf im **Test-Build()** muss angepasst werden).

7. **Drift-Guard-Test** — **KORRIGIERT/präzisiert.**
   `ServiceSettingDefinitionsTests.All_ContainsDocumentedServiceReadKey` ([ServiceSettingDefinitionsTests.cs:65-101](../../../IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs)) ist eine `[Theory]` mit `[InlineData]` je dokumentiertem Key; sie prüft `All.Should().Contain(key)`. „Documented" = die hand-gepflegte InlineData-Liste (aus dem Grep aller Service-Reads). Es gibt **keinen** Umkehr-Test (Katalog ⊆ Dokumentiert) — ein neuer Katalog-Key OHNE InlineData bräche den Test also nicht. Da der Service den Key real liest, wird die InlineData ergänzt (hält den Guard ehrlich) und liefert einen sauberen Rot→Grün-Mikrozyklus (InlineData zuerst → Rot, Katalog danach → Grün).

8. **`ISyncErrorNotifier.NotifyAsync(string, Exception, ct)`** — **BESTÄTIGT.**
   Signatur ([ISyncErrorNotifier.cs:6](../../../IDEALAKEWMSService/Services/ISyncErrorNotifier.cs)): `Task NotifyAsync(string stepName, Exception ex, CancellationToken ct = default)` — „wirft NIE". Cap-Fall übergibt stepName + synthetische `InvalidOperationException`; **kein throw** im Service (Korrekturen bleiben, `FinishSuccess`). Vorbild: [SageImportService.cs:303-309](../../../IDEALAKEWMSService/Services/SageImportService.cs). Getestet durch Service-Test (h).

9. **NAN + Kommissionierwagen = `Source=Manual`** — **BESTÄTIGT.**
   `StorageLocationSource` ist eine **static class mit String-Konstanten** (`Manual`/`Sage`), NICHT ein `enum` ([StorageLocationSource.cs](../../../IdealAkeWms/Models/StorageLocationSource.cs)); `StorageLocation.Source` ist ein `string` mit Default `StorageLocationSource.Manual` ([StorageLocation.cs:38](../../../IdealAkeWms/Models/StorageLocation.cs)). Das NAN-Seed in [IdealAkeWms/Program.cs:140-152](../../../IdealAkeWms/Program.cs) setzt `Source` NICHT → default `Manual`. → NAN + Wagen sind durch den `Source==Sage`-Filter automatisch ausgeschlossen. (Der Filter ist ein String-Vergleich, kein Enum-Vergleich.)

10. **Vorzeichen (negativer WMS-Bestand)** — **BESTÄTIGT.**
    `Quantity = Math.Abs(wmsBestand)`, `MovementType = wmsBestand > 0 ? SageAusbuchung : SageEinbuchung`. Bei negativem Bestand bringt `delta = -wmsBestand` (positiv) exakt auf 0 via `SageEinbuchung` — konsistent mit der bestehenden Korrektur-Schleife ([LagerbestandSyncService.cs:151](../../../IDEALAKEWMSService/Services/LagerbestandSyncService.cs)).

### Zusätzliche Korrekturen gegenüber der Task-Vorgabe

- **`SageBestandDto`-Feldnamen** — **BESTÄTIGT:** `record SageBestandDto(string? Artikelnummer, string? Lagerplatz, decimal? Bestand)` ([ISageBestandReader.cs:4](../../../IDEALAKEWMSService/Services/ISageBestandReader.cs)). `Bestand` ist `decimal?` (NULL → als 0 behandelt).
- **Bestehende Service-Tests** — **KORREKTUR:** Die Datei `IDEALAKEWMSService.Tests/Services/LagerbestandSyncServiceTests.cs` **existiert bereits** mit 14 Tests + `Build()`-Helper + `FakeSageBestandReader` ([Helpers/FakeSageBestandReader.cs](../../../IDEALAKEWMSService.Tests/Helpers/FakeSageBestandReader.cs)) + `FakeSyncLogger` ([IdealAkeWms.Tests/Helpers/FakeSyncLogger.cs](../../../IdealAkeWms.Tests/Helpers/FakeSyncLogger.cs)). → **erweitern**, nicht neu anlegen. Die Spec-Annahme „KEINE Tests, neue Datei nötig" ist überholt.
- **ctor-Reihenfolge** — die bestehende Reihenfolge ist `ctx, reader, stockRepo, ILogger, ISyncLogger` (ISyncLogger letzter, [LagerbestandSyncService.cs:20-25](../../../IDEALAKEWMSService/Services/LagerbestandSyncService.cs)). Neu: `ctx, reader, stockRepo, IConfiguration, ISyncErrorNotifier, ILogger, ISyncLogger` — die zwei neuen Deps VOR `ILogger`, `ISyncLogger` bleibt **letzter** (Projekt-Konvention eingehalten).
- **Cap-Read in Tests nicht injizierbar** — **KORREKTUR:** `ServiceSettings.GetIntSafeAsync` liest über `ConnectionStrings.Wms(config)` aus der DB-Tabelle ([ServiceSettings.cs:19-29,61-66](../../../IDEALAKEWMSService/Common/ServiceSettings.cs)), nicht aus IConfiguration-Keys. Fehlt der Connection-String (Test), wirft `ConnectionStrings.Wms` → `GetIntSafeAsync` fängt ab → Default 100. Daher seedet der Service-Cap-Test (h) **101** verwaiste Paare.
- **Kein eigener `SyncLogServices`-Name** — das Nullsetzen läuft im bestehenden `Lagerbestand`-Lauf (`SyncLogServices.Lagerbestand` existiert, [SyncLogServices.cs:11](../../../IdealAkeWms/Services/SyncLogger/SyncLogServices.cs)). Kein neuer Service-Konstanten-Eintrag nötig (anders als beim Reconcile).

## Offene Annahmen

- **Bestehender Test #10 `Run_AggregatesMultiplePreMovements_BeforeComputingDelta` bleibt grün — inzidenteller Zeroing-Effekt (verifiziert):** Dieser Test erzeugt eine Umbuchung mit `SourceStorageLocationId = 2` (L-2, Sage/aktiv), sodass `GetCurrentStockByArticleAndLocationAsync` einen Quellseiten-Key `(1,2) = -2` liefert; L-2 ist nicht im Sage-Read. Mit dem neuen Zeroing wird `(A-1,L-2)` daher zum Kandidaten und erhält eine `SageEinbuchung` (+2 → auf 0) — das ist **spec-korrektes** Verhalten (verwaistes Sage-aktives Paar mit Bestand ≠ 0). Die Assertions von #10 (`CorrectionsMinus == 1`, genau **eine** `SageAusbuchung` mit Menge 2) bleiben trotzdem erfüllt, weil die Zeroing-Buchung eine `SageEinbuchung` (nicht `SageAusbuchung`) ist und den Korrektur-Zähler `minus` nicht berührt. **Falls ein Reviewer die Isolation sauberer will**, den Sage-Read von #10 um `new("A-1", "L-2", 8m)` (bzw. den erwarteten L-2-Bestand) ergänzen — nicht erforderlich für Grün. Alle übrigen 13 bestehenden Tests lösen kein Zeroing aus (kein Sage-aktiver Managed-Bestand außerhalb des Sage-Reads).
- Der Info-Detailzeilen-Cap ist auf **100 Zeilen/Lauf** gesetzt (`infoLines < 100`), der `nullgesetzt`-Count zählt ALLE (analog zum `hauptlagerplatz_fehlt`-Muster). Falls ein anderer Cap gewünscht ist, nur die Konstante anpassen.
- Der `NotifyAsync`-`stepName` folgt der Spec wörtlich (`"Lagerbestand-Nullsetzen: Cap ueberschritten"`); die FA-Reconciliation nutzt stattdessen den Service-Konstanten-Namen. Beide sind zulässig — der Test (h) prüft nur, dass `NotifyAsync` einmal aufgerufen wird.
