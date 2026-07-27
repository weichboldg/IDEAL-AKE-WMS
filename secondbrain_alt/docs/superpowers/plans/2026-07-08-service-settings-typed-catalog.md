# Typisierte, vollständige ServiceSettings-Verwaltung — Implementierungsplan

**For agentic workers.** Jeder Task ist bite-sized, TDD-first, mit echtem Code + exakten Befehlen. Arbeite die Tasks der Reihe nach ab. Führe die genannten Test-/Build-Befehle aus; committe nach jedem grünen Task. KEIN AppVersion-Bump (v1.25.0 wird gefaltet), KEIN Schema-Change, KEINE Migration, KEIN `dotnet ef migrations add`.

**Goal:** Die WMS-Seite `/ServiceSettings` wird von einem Freitext-Key/Value-Editor zu einem **typisierten, vollständigen** Editor. Bool → Aktiv/Inaktiv-Toggle, Int → Zahlenfeld, String → Text-/Textarea. Ein **typisierter Katalog** (`ServiceSettingDefinitions.All`) ist Single Source of Truth für Typ/Default/Kategorie/Beschreibung und treibt sowohl das Seeding (`Program.cs`) als auch die UI. Die 8 heute nur via `IConfiguration` gelesenen Service-Keys werden DB-first (mit resilientem Fallback). Ausnahmen: `ConnectionStrings:*` + `MailSettings:*` bleiben appsettings-only.

**Architecture:** Katalog liegt im **Web-Projekt** `IdealAkeWms/Models/` (der Service referenziert das Web-Projekt via `ProjectReference` → beide Seiten nutzen den Katalog). Seeding-Loop in `Program.cs` idempotent über `ServiceSettingDefinitions.All`. UI spiegelt das etablierte Toggle-Muster aus `SettingsController.SaveSettings(Dictionary<string,string>)` + `Views/Settings/Index.cshtml` (`.bool-toggle`/`.bool-hidden` + JS-Label-Sync). Lesestellen-Umstellung nutzt bestehende `ServiceSettings.GetBoolAsync/GetIntAsync/GetValueAsync` (Service `Common/ServiceSettings.cs`) plus neue Safe-Wrapper.

**Tech Stack:** ASP.NET Core 10.0 MVC + EF Core 10.0 (SQL Server), xUnit + FluentAssertions + Moq + EF InMemory (`TestDbContextFactory.Create()`). Worktree `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\glas-bestellung` (Branch `feature/glas-bestellung`, HEAD `d3f786d`).

**Konventionen:**
- **Build:** `dotnet build IdealAkeWms.slnx` (`.slnx`, nicht `.sln`).
- **Web-Tests:** `dotnet test IdealAkeWms.Tests --nologo`.
- **Service-Tests:** `dotnet test IDEALAKEWMSService.Tests --nologo`.
- **Git (git-bash, im Worktree):** `cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git ...`. Commit je Task via `git commit -F <heredoc>` oder mehrere `-m`. KEIN literales `@'...'@` (das ist PowerShell). Footer: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.
- **Fallstrick (CLAUDE.md):** Hidden+Checkbox mit gleichem `name` funktioniert NICHT — der Hidden-Input trägt `name="settings[Key]"`, die Checkbox hat KEIN `name` und synchronisiert per JS/`onchange` den Hidden-Wert.
- **TempData:** Nur `SuccessMessage`/`WarningMessage`. Fehler via `ModelState`.

---

## Task 0: Pre-Flight — Baseline grün, HEAD bestätigen

**Files:** keine Änderung.

- [ ] HEAD + Branch bestätigen:
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git rev-parse HEAD && git branch --show-current
```
Erwartet: `d3f786d732b58d7f9d595be0ad9a5d874b43307d` und `feature/glas-bestellung`.

- [ ] Build grün:
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet build IdealAkeWms.slnx --nologo 2>&1 | tail -3
```
Erwartet: `0 Fehler` (Warnungen NU1902/CS8602 sind vorbestehend, ignorieren).

- [ ] Web-Tests grün:
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --nologo 2>&1 | tail -4
```
Erwartet: `Fehler: 0` / `Failed: 0` — alle Tests bestehen.

- [ ] Service-Tests grün:
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IDEALAKEWMSService.Tests --nologo 2>&1 | tail -4
```
Erwartet: `Fehler: 0` / `Failed: 0`.

Kein Commit (nur Verifikation).

---

## Task 1: Typisierter Katalog + Konsistenz-Tests (TDD)

**Files:**
- Create `IdealAkeWms/Models/ServiceSettingDefinition.cs`
- Create `IdealAkeWms/Models/ServiceSettingDefinitions.cs`
- Create (Test) `IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs`

### 1a. Enum + Record + Katalog schreiben (Implementierung zuerst, weil die Tests dagegen kompilieren)

- [ ] Erstelle `IdealAkeWms/Models/ServiceSettingDefinition.cs`:
```csharp
namespace IdealAkeWms.Models;

/// <summary>Typ einer Service-Einstellung — steuert die UI-Darstellung.</summary>
public enum ServiceSettingType
{
    Bool,
    Int,
    String
}

/// <summary>
/// Definiert eine Service-Einstellung typisiert: Single Source of Truth fuer
/// Typ, Default, Kategorie und Beschreibung. Treibt sowohl das Seeding
/// (<c>Program.cs</c>) als auch die typisierte UI (<c>/ServiceSettings</c>).
/// </summary>
/// <param name="Key">DB-Key (z.B. "Sync:BomCacheEnabled").</param>
/// <param name="Type">Typ fuer UI + Validierung.</param>
/// <param name="DefaultValue">
/// Default IMMER als String im DB-Format: bool -&gt; "true"/"false",
/// int -&gt; "60", string -&gt; Rohwert.
/// </param>
/// <param name="Category">UI-Gruppierung, z.B. "Sync", "BOM-Cache".</param>
/// <param name="Description">Anzeige unter dem Feld.</param>
/// <param name="Multiline">String-Listen (z.B. Empfaenger) als Textarea rendern.</param>
public sealed record ServiceSettingDefinition(
    string Key,
    ServiceSettingType Type,
    string DefaultValue,
    string Category,
    string Description,
    bool Multiline = false);
```

- [ ] Erstelle `IdealAkeWms/Models/ServiceSettingDefinitions.cs`. **Vollständiger Katalog** — jeder Key stammt aus einem realen Service-Read ODER dem bestehenden `Program.cs`-Seed (per Grep verifiziert). Defaults = die im jeweiligen Aufruf/Seed übergebenen Werte:
```csharp
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace IdealAkeWms.Models;

/// <summary>
/// Vollständiger, typisierter Katalog aller Service-Einstellungen.
/// Quelle: jeder Eintrag entspricht einem realen ServiceSettings-Read im
/// Service (Common/ServiceSettings.Get*Async oder direkter ServiceSettings-
/// Tabellen-Read) bzw. dem bisherigen Program.cs-Seed-Block.
/// Ausgenommen (bewusst NICHT im Katalog): ConnectionStrings:* + MailSettings:*.
/// </summary>
public static class ServiceSettingDefinitions
{
    public static IReadOnlyList<ServiceSettingDefinition> All { get; } = new List<ServiceSettingDefinition>
    {
        // ----- Sync (Master-Schalter der einzelnen Sync-Bloecke) -----
        new("Sync:ProductionOrdersEnabled",          ServiceSettingType.Bool, "true",  "Sync", "Produktionsauftraege-Sync aus SAGE aktiv"),
        new("Sync:ArticlesEnabled",                  ServiceSettingType.Bool, "true",  "Sync", "Artikel-Sync aus SAGE aktiv"),
        new("Sync:OseonArticleCategoryEnabled",      ServiceSettingType.Bool, "false", "Sync", "OSEON-Artikelkategorie-Sync aktiv (laeuft nach Artikel-Import)"),
        new("Sync:OseonTrackingEnabled",             ServiceSettingType.Bool, "false", "Sync", "OSEON-Tracking-Sync + Werkbank-Sync aktiv"),
        new("Sync:EnaioDmsEnabled",                  ServiceSettingType.Bool, "false", "Sync", "enaio DMS-Sync aktiv"),
        new("Sync:PartRequisitionEmailEnabled",      ServiceSettingType.Bool, "false", "Sync", "Bedarfsmeldungs-E-Mail-Versand aktiv"),
        new("Sync:WarehouseRequisitionEmailEnabled", ServiceSettingType.Bool, "false", "Sync", "E-Mail-Versand fuer Lagerbestellungen aktiv"),
        new("Sync:LagerplaetzeEnabled",              ServiceSettingType.Bool, "false", "Sync", "Sage-Lagerplatz-Stammdaten-Sync aktiv"),
        new("Sync:LagerbestandEnabled",              ServiceSettingType.Bool, "false", "Sync", "Sage-Lagerbestand-Sync (Bestand-Korrektur) aktiv"),
        new("Sync:LagerbestandIntervalMinutes",      ServiceSettingType.Int,  "0",     "Sync", "Eigenes Intervall (Minuten) fuer Lagerbestand-Sync (0 = Worker-Standard)"),
        new("Sync:ProductionOrderReconcileEnabled",  ServiceSettingType.Bool, "false", "Sync", "Verwaiste (in Sage geloeschte) offene FAs automatisch stornieren (Opt-in)"),
        new("Sync:ReconcileMaxCancelPerRun",         ServiceSettingType.Int,  "100",   "Sync", "Sicherheits-Cap: mehr Storno-Kandidaten je Lauf -> kein Storno + Fehlermail"),

        // ----- BOM-Cache -----
        new("Sync:BomCacheEnabled",                  ServiceSettingType.Bool, "false", "BOM-Cache", "BOM-Cache-Sync aktiv (Top-N offene Auftraege werden gecacht)"),
        new("Sync:BomCacheWeeks",                    ServiceSettingType.Int,  "8",     "BOM-Cache", "Wieviele Wochen Fertigungstermin in die Zukunft cachen"),
        new("Sync:BomCacheMaxOrders",                ServiceSettingType.Int,  "200",   "BOM-Cache", "Maximalanzahl Auftraege im BOM-Cache"),
        new("Sync:BomCacheMaxAgeHours",              ServiceSettingType.Int,  "24",    "BOM-Cache", "Sicherheitsnetz: Re-Sync wenn Cache-Eintrag aelter als X Stunden"),

        // ----- FA-Vervollstaendigung -----
        new("Sync:FaWorkStepDetectionEnabled",       ServiceSettingType.Bool, "false", "FA-Vervollstaendigung", "Automatische FA-Arbeitsgang-Erkennung aus dem BOM-Cache (laeuft nach BomCache-Sync)"),

        // ----- Lackierteile -----
        new("Sync:CoatingDetectionEnabled",          ServiceSettingType.Bool, "false", "Lackierteile", "Lackierteil-Erkennung als separater Sync-Job aktiv"),

        // ----- BDE / Feiertage -----
        new("Sync:BdeAutoPauseIntervalMinutes",      ServiceSettingType.Int,  "60",    "BDE", "Intervall (Minuten) fuer BDE-Auto-Pause am Schichtende"),
        new("Sync:FeiertagSyncEnabled",              ServiceSettingType.Bool, "false", "Feiertage", "Feiertags-Sync aus Nager.Date aktiv"),
        new("Sync:FeiertagCountryCode",              ServiceSettingType.String, "AT",  "Feiertage", "Laendercode fuer Feiertags-Sync (ISO-3166 alpha-2, z.B. AT, DE)"),
        new("Sync:FeiertagRegion",                   ServiceSettingType.String, "",    "Feiertage", "Optionale Region fuer Feiertags-Sync (z.B. AT-3 fuer Niederoesterreich)"),
        new("Sync:FeiertagJahreVoraus",              ServiceSettingType.Int,  "2",     "Feiertage", "Anzahl Folgejahre, die Feiertage vorausgesynct werden"),

        // ----- Worker -----
        new("WorkerSettings:SyncIntervalMinutes",    ServiceSettingType.Int,  "15",    "Worker", "Sync-Intervall (Minuten) fuer den SyncWorker"),
        new("WorkerSettings:NotificationCheckIntervalMinutes", ServiceSettingType.Int, "60", "Worker", "Intervall (Minuten) fuer die Meldebestand-Pruefung (NotificationWorker)"),
        new("WorkerSettings:SyncDryRun",             ServiceSettingType.Bool, "false", "Worker", "DryRun-Modus: kein Schreiben, nur Simulation"),

        // ----- Fehlermail -----
        new("ErrorNotification:Enabled",             ServiceSettingType.Bool, "false", "Fehlermail", "Fehlermail-Versand bei Sync-Fehlern aktiv"),
        new("ErrorNotification:Recipients",          ServiceSettingType.String, "",    "Fehlermail", "Empfaenger-Liste (kommagetrennt) fuer Sync-Fehlermails", Multiline: true),

        // ----- Benachrichtigungen (Meldebestand-Mail) -----
        new("Notifications:MeldebestandEnabled",     ServiceSettingType.Bool, "true",  "Benachrichtigungen", "Meldebestand-Mail aktiv"),
        new("Notifications:MeldebestandSubject",     ServiceSettingType.String, "Meldebestand unterschritten — IDEAL AKE WMS", "Benachrichtigungen", "Betreff der Meldebestand-Mail"),
        new("Notifications:Recipients",              ServiceSettingType.String, "",    "Benachrichtigungen", "Feste Empfaenger fuer Meldebestand-Mail (kommagetrennt, z.B. lager@ake.at,leitung@ake.at)", Multiline: true),
        new("Notifications:AppBaseUrl",              ServiceSettingType.String, "",    "Benachrichtigungen", "Basis-URL der App fuer Links in Mails (z.B. https://wms.ake.at)"),
    };

    /// <summary>Findet eine Definition per Key (case-sensitiv, wie DB-Key).</summary>
    public static bool TryGet(string key, [NotNullWhen(true)] out ServiceSettingDefinition? def)
    {
        def = All.FirstOrDefault(d => d.Key == key);
        return def is not null;
    }
}
```

### 1b. Konsistenz-Tests schreiben (jetzt kompilierbar) — Red erwartet? Nein: der Katalog ist bereits korrekt, die Tests müssen SOFORT grün sein. Sie sind der Drift-Guard.

- [ ] Erstelle `IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs`:
```csharp
using System.Linq;
using FluentAssertions;
using IdealAkeWms.Models;

namespace IdealAkeWms.Tests.Models;

public class ServiceSettingDefinitionsTests
{
    [Fact]
    public void All_HasNoDuplicateKeys()
    {
        var keys = ServiceSettingDefinitions.All.Select(d => d.Key).ToList();
        keys.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void All_BoolDefaults_AreTrueOrFalse()
    {
        foreach (var def in ServiceSettingDefinitions.All.Where(d => d.Type == ServiceSettingType.Bool))
        {
            def.DefaultValue.Should().BeOneOf("true", "false",
                because: $"{def.Key} ist Bool und muss 'true'/'false' als Default haben");
        }
    }

    [Fact]
    public void All_IntDefaults_ParseAsInt()
    {
        foreach (var def in ServiceSettingDefinitions.All.Where(d => d.Type == ServiceSettingType.Int))
        {
            int.TryParse(def.DefaultValue, out _).Should().BeTrue(
                because: $"{def.Key} ist Int und muss einen int-parsebaren Default haben (war '{def.DefaultValue}')");
        }
    }

    [Fact]
    public void All_CategoryAndDescription_AreNotEmpty()
    {
        foreach (var def in ServiceSettingDefinitions.All)
        {
            def.Category.Should().NotBeNullOrWhiteSpace(because: $"{def.Key} braucht eine Kategorie");
            def.Description.Should().NotBeNullOrWhiteSpace(because: $"{def.Key} braucht eine Beschreibung");
        }
    }

    [Fact]
    public void TryGet_ExistingKey_ReturnsDefinition()
    {
        ServiceSettingDefinitions.TryGet("Sync:BomCacheEnabled", out var def).Should().BeTrue();
        def!.Type.Should().Be(ServiceSettingType.Bool);
        def.DefaultValue.Should().Be("false");
    }

    [Fact]
    public void TryGet_UnknownKey_ReturnsFalse()
    {
        ServiceSettingDefinitions.TryGet("Does:NotExist", out var def).Should().BeFalse();
        def.Should().BeNull();
    }

    // Drift-Guard: JEDER dokumentierte, service-gelesene Key MUSS im Katalog sein.
    // Diese feste Liste stammt aus dem Grep aller ServiceSettings-Reads im Service
    // (Common/ServiceSettings.Get*Async + direkte [ServiceSettings]-Tabellen-Reads)
    // sowie den IConfiguration-Reads, die dieser Umbau DB-first stellt.
    [Theory]
    [InlineData("Sync:ProductionOrdersEnabled")]
    [InlineData("Sync:ArticlesEnabled")]
    [InlineData("Sync:OseonArticleCategoryEnabled")]
    [InlineData("Sync:OseonTrackingEnabled")]
    [InlineData("Sync:EnaioDmsEnabled")]
    [InlineData("Sync:PartRequisitionEmailEnabled")]
    [InlineData("Sync:WarehouseRequisitionEmailEnabled")]
    [InlineData("Sync:LagerplaetzeEnabled")]
    [InlineData("Sync:LagerbestandEnabled")]
    [InlineData("Sync:LagerbestandIntervalMinutes")]
    [InlineData("Sync:ProductionOrderReconcileEnabled")]
    [InlineData("Sync:ReconcileMaxCancelPerRun")]
    [InlineData("Sync:BomCacheEnabled")]
    [InlineData("Sync:BomCacheWeeks")]
    [InlineData("Sync:BomCacheMaxOrders")]
    [InlineData("Sync:BomCacheMaxAgeHours")]
    [InlineData("Sync:FaWorkStepDetectionEnabled")]
    [InlineData("Sync:CoatingDetectionEnabled")]
    [InlineData("Sync:BdeAutoPauseIntervalMinutes")]
    [InlineData("Sync:FeiertagSyncEnabled")]
    [InlineData("Sync:FeiertagCountryCode")]
    [InlineData("Sync:FeiertagRegion")]
    [InlineData("Sync:FeiertagJahreVoraus")]
    [InlineData("WorkerSettings:SyncIntervalMinutes")]
    [InlineData("WorkerSettings:NotificationCheckIntervalMinutes")]
    [InlineData("WorkerSettings:SyncDryRun")]
    [InlineData("ErrorNotification:Enabled")]
    [InlineData("ErrorNotification:Recipients")]
    [InlineData("Notifications:MeldebestandEnabled")]
    [InlineData("Notifications:MeldebestandSubject")]
    [InlineData("Notifications:Recipients")]
    [InlineData("Notifications:AppBaseUrl")]
    public void All_ContainsDocumentedServiceReadKey(string key)
    {
        ServiceSettingDefinitions.All.Select(d => d.Key).Should().Contain(key);
    }
}
```

- [ ] Tests laufen lassen (müssen grün sein — Katalog + Tests wurden konsistent geschrieben):
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~ServiceSettingDefinitionsTests" 2>&1 | tail -6
```
Erwartet: alle 38 (6 Facts + 32 Theory-Faelle) grün, `Fehler: 0`.
> Falls rot: die Fehlermeldung nennt Key + erwarteten Wert (z.B. Bool-Default nicht "true"/"false", oder fehlender Katalog-Eintrag) — Katalog fixen, nicht den Test aufweichen.

- [ ] Commit:
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IdealAkeWms/Models/ServiceSettingDefinition.cs IdealAkeWms/Models/ServiceSettingDefinitions.cs IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs && git commit -F - <<'EOF'
feat(service-settings): typisierter ServiceSettings-Katalog + Konsistenz-Tests

ServiceSettingDefinition (record + ServiceSettingType enum + Multiline) und
ServiceSettingDefinitions.All als Single Source of Truth (32 Keys: 17 bool,
9 int, 6 string). Drift-Guard-Tests: keine Doppel-Keys, Bool-Defaults in
{true,false}, Int-Defaults int-parsebar, Category/Description gefuellt, jeder
dokumentierte service-gelesene Key ist im Katalog.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
```

---

## Task 2: Program.cs — Seed-Loop über den Katalog

**Files:**
- Modify `IdealAkeWms/Program.cs:379-414` (Block `serviceSettingSeed` … `db.SaveChanges();`)

Der bestehende partielle Seed (22 Keys, hartcodiertes Tupel-Array) wird durch eine idempotente Schleife über `ServiceSettingDefinitions.All` (32 Keys) ersetzt. Bestehende DB-Zeilen bleiben unberührt (User-Werte gewinnen).

- [ ] Ersetze in `IdealAkeWms/Program.cs` den Block von `// Standard Service-Settings` (Zeile 379) bis `db.SaveChanges();` (Zeile 414) durch:
```csharp
    // Standard Service-Settings — vollstaendig aus dem typisierten Katalog geseedet.
    // Idempotent: bestehende Zeilen (User-Werte) bleiben unberuehrt.
    foreach (var def in IdealAkeWms.Models.ServiceSettingDefinitions.All)
    {
        if (!db.ServiceSettings.Any(s => s.Key == def.Key))
        {
            db.ServiceSettings.Add(new IdealAkeWms.Models.ServiceSetting
            {
                Key = def.Key,
                Value = def.DefaultValue,
                Category = def.Category,
                Description = def.Description
            });
        }
    }
    db.SaveChanges();
```
> Hinweis: Das alte `var serviceSettingSeed = new (...)[] { ... };` Array + die `foreach (var (key, value, category, description) in serviceSettingSeed)`-Schleife komplett entfernen. Der neue Block ist der einzige ServiceSettings-Seed.

- [ ] Build grün (Razor + C# kompilieren, keine ungenutzte Variable):
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet build IdealAkeWms.slnx --nologo 2>&1 | tail -3
```
Erwartet: `0 Fehler`.

- [ ] Web-Tests grün (kein Regress):
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --nologo 2>&1 | tail -4
```
Erwartet: `Fehler: 0`.

- [ ] Commit:
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IdealAkeWms/Program.cs && git commit -F - <<'EOF'
feat(service-settings): Program.cs seedet ServiceSettings aus dem Katalog

Der partielle 22-Key-Seed-Block wird durch eine idempotente Schleife ueber
ServiceSettingDefinitions.All (32 Keys) ersetzt. Beim naechsten Start existiert
jede Katalog-Zeile in der DB (inkl. Reconcile/Worker/ErrorNotification/Feiertag);
bestehende Zeilen bleiben unveraendert.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
```

---

## Task 3: ViewModel + Controller (Index-Merge + SaveSettings) — TDD

**Files:**
- Create `IdealAkeWms/Models/ViewModels/ServiceSettingsViewModel.cs`
- Modify `IdealAkeWms/Controllers/ServiceSettingsController.cs`
- Create (Test) `IdealAkeWms.Tests/Controllers/ServiceSettingsControllerTests.cs`

### 3a. ViewModel

- [ ] Erstelle `IdealAkeWms/Models/ViewModels/ServiceSettingsViewModel.cs`:
```csharp
using System.Collections.Generic;
using IdealAkeWms.Models;

namespace IdealAkeWms.Models.ViewModels;

/// <summary>Ein typisiertes Feld auf der ServiceSettings-Seite.</summary>
public class ServiceSettingItem
{
    public string Key { get; set; } = string.Empty;
    public ServiceSettingType Type { get; set; }
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Multiline { get; set; }
}

/// <summary>Eine UI-Kategorie mit ihren Feldern.</summary>
public class ServiceSettingGroup
{
    public string Category { get; set; } = string.Empty;
    public List<ServiceSettingItem> Items { get; set; } = new();
}

/// <summary>
/// ViewModel fuer /ServiceSettings: Katalog∪DB gemergt und gruppiert, plus
/// Orphan-Zeilen (DB-Keys ohne Katalog-Eintrag) fuer den Freitext-Fallback.
/// </summary>
public class ServiceSettingsViewModel
{
    public List<ServiceSettingGroup> Groups { get; set; } = new();
    public List<ServiceSetting> OrphanEntries { get; set; } = new();
}
```

### 3b. Controller-Tests schreiben (Red)

- [ ] Erstelle `IdealAkeWms.Tests/Controllers/ServiceSettingsControllerTests.cs`. Setup mit `Mock<IServiceSettingRepository>`, `DefaultHttpContext`, `TempDataDictionary` (Muster aus `ArticlesControllerTests`):
```csharp
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using IdealAkeWms.Controllers;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;

namespace IdealAkeWms.Tests.Controllers;

public class ServiceSettingsControllerTests
{
    private static ServiceSettingsController Build(Mock<IServiceSettingRepository> repo)
    {
        var ctrl = new ServiceSettingsController(repo.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        ctrl.TempData = new TempDataDictionary(ctrl.HttpContext, Mock.Of<ITempDataProvider>());
        return ctrl;
    }

    [Fact]
    public async Task Index_MergesCatalogAndDb_TypedAndGrouped()
    {
        var repo = new Mock<IServiceSettingRepository>();
        // DB: ein bekannter Key mit User-Override + ein Orphan (nicht im Katalog).
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ServiceSetting>
        {
            new() { Key = "Sync:BomCacheEnabled", Value = "true",  Category = "BOM-Cache", Description = "alt" },
            new() { Key = "Legacy:Frei",          Value = "xyz",   Category = "Sonstiges", Description = "orphan" }
        });

        var ctrl = Build(repo);
        var result = await ctrl.Index() as ViewResult;
        var vm = result!.Model as ServiceSettingsViewModel;

        vm.Should().NotBeNull();
        // Bekannter Katalog-Key: Wert = DB-Override "true", Typ = Bool aus Katalog.
        var allItems = vm!.Groups.SelectMany(g => g.Items).ToList();
        var bom = allItems.Single(i => i.Key == "Sync:BomCacheEnabled");
        bom.Type.Should().Be(ServiceSettingType.Bool);
        bom.Value.Should().Be("true");
        // Katalog-Key OHNE DB-Zeile faellt auf Default zurueck (z.B. Sync:BomCacheWeeks = "8").
        var weeks = allItems.Single(i => i.Key == "Sync:BomCacheWeeks");
        weeks.Type.Should().Be(ServiceSettingType.Int);
        weeks.Value.Should().Be("8");
        // Ein String-Multiline-Key existiert.
        allItems.Single(i => i.Key == "ErrorNotification:Recipients").Multiline.Should().BeTrue();
        // Orphan separat, NICHT in den Gruppen.
        vm.OrphanEntries.Should().ContainSingle(o => o.Key == "Legacy:Frei");
        allItems.Should().NotContain(i => i.Key == "Legacy:Frei");
    }

    [Fact]
    public async Task Index_ShowsAllCatalogKeys_EvenWithEmptyDb()
    {
        var repo = new Mock<IServiceSettingRepository>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<ServiceSetting>());

        var ctrl = Build(repo);
        var result = await ctrl.Index() as ViewResult;
        var vm = result!.Model as ServiceSettingsViewModel;

        var keys = vm!.Groups.SelectMany(g => g.Items).Select(i => i.Key).ToList();
        keys.Should().HaveCount(ServiceSettingDefinitions.All.Count);
        keys.Should().Contain("WorkerSettings:SyncIntervalMinutes");
        vm.OrphanEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveSettings_BoolNormalized_And_Upserted()
    {
        var repo = new Mock<IServiceSettingRepository>();
        var ctrl = Build(repo);

        var settings = new Dictionary<string, string>
        {
            ["Sync:BomCacheEnabled"] = "true",
            ["Sync:CoatingDetectionEnabled"] = "false"
        };

        var result = await ctrl.SaveSettings(settings);

        result.Should().BeOfType<RedirectToActionResult>();
        // Bool-Keys werden mit Kategorie/Beschreibung aus dem Katalog geschrieben.
        repo.Verify(r => r.UpsertAsync("Sync:BomCacheEnabled", "true", "BOM-Cache", It.IsAny<string>()), Times.Once);
        repo.Verify(r => r.UpsertAsync("Sync:CoatingDetectionEnabled", "false", "Lackierteile", It.IsAny<string>()), Times.Once);
        ctrl.TempData["SuccessMessage"].Should().NotBeNull();
    }

    [Fact]
    public async Task SaveSettings_BoolCheckboxValueOn_NormalizedToTrue()
    {
        var repo = new Mock<IServiceSettingRepository>();
        var ctrl = Build(repo);

        // Falls je ein Rohwert != "true"/"false" ankommt (z.B. "on"): normalisieren.
        var settings = new Dictionary<string, string> { ["Sync:BomCacheEnabled"] = "on" };
        await ctrl.SaveSettings(settings);

        repo.Verify(r => r.UpsertAsync("Sync:BomCacheEnabled", "true", "BOM-Cache", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SaveSettings_IntParseError_AddsModelStateError_AndSkipsUpsertForThatKey()
    {
        var repo = new Mock<IServiceSettingRepository>();
        var ctrl = Build(repo);

        var settings = new Dictionary<string, string>
        {
            ["Sync:BomCacheWeeks"] = "abc",   // ungueltig
            ["Sync:BomCacheMaxOrders"] = "300" // gueltig
        };

        var result = await ctrl.SaveSettings(settings);

        // Ungueltiger Int-Key wird NICHT gespeichert, aber gueltiger schon.
        repo.Verify(r => r.UpsertAsync("Sync:BomCacheWeeks", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        repo.Verify(r => r.UpsertAsync("Sync:BomCacheMaxOrders", "300", "BOM-Cache", It.IsAny<string>()), Times.Once);
        ctrl.ModelState.IsValid.Should().BeFalse();
        // Bei Fehler: View zurueck (nicht Redirect), damit der Fehler sichtbar ist.
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task SaveSettings_UnknownKey_IgnoredSilently()
    {
        var repo = new Mock<IServiceSettingRepository>();
        var ctrl = Build(repo);

        var settings = new Dictionary<string, string> { ["Not:InCatalog"] = "whatever" };
        await ctrl.SaveSettings(settings);

        repo.Verify(r => r.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
```

- [ ] Tests laufen (Red — `Index` gibt noch `List<ServiceSetting>` zurück, `SaveSettings` existiert nicht):
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~ServiceSettingsControllerTests" 2>&1 | tail -8
```
Erwartet: Kompilierfehler ODER Test-Failures (Methode `SaveSettings` fehlt, `Index`-Model-Cast schlägt fehl). Das ist das erwartete Rot.

### 3c. Controller implementieren (Green)

- [ ] Ersetze den Inhalt von `IdealAkeWms/Controllers/ServiceSettingsController.cs`. `Index` merged Katalog∪DB; neue `SaveSettings`-POST; `Create`/`Edit`/`Delete` bleiben für Orphans erhalten:
```csharp
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using IdealAkeWms.Data.Repositories;
using IdealAkeWms.Filters;
using IdealAkeWms.Models;
using IdealAkeWms.Models.ViewModels;

namespace IdealAkeWms.Controllers;

[RequireAdminAccess]
public class ServiceSettingsController : Controller
{
    private readonly IServiceSettingRepository _repository;

    public ServiceSettingsController(IServiceSettingRepository repository)
    {
        _repository = repository;
    }

    public async Task<IActionResult> Index()
    {
        var vm = await BuildViewModelAsync();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveSettings(Dictionary<string, string> settings)
    {
        settings ??= new Dictionary<string, string>();

        foreach (var (key, rawValue) in settings)
        {
            if (!ServiceSettingDefinitions.TryGet(key, out var def))
                continue; // unbekannte Keys ignorieren (Orphans laufen ueber Edit/Delete)

            var value = rawValue ?? string.Empty;

            switch (def.Type)
            {
                case ServiceSettingType.Bool:
                    // Checkbox/Hidden liefert "true"/"false"; jeder truthy-Wert -> "true".
                    var isTrue = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                              || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase)
                              || value == "1";
                    await _repository.UpsertAsync(key, isTrue ? "true" : "false", def.Category, def.Description);
                    break;

                case ServiceSettingType.Int:
                    if (!int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                    {
                        ModelState.AddModelError(key, $"'{def.Key}' erwartet eine ganze Zahl (war: '{value}').");
                        continue; // diesen Key NICHT speichern
                    }
                    await _repository.UpsertAsync(key, i.ToString(CultureInfo.InvariantCulture), def.Category, def.Description);
                    break;

                default: // String
                    await _repository.UpsertAsync(key, value, def.Category, def.Description);
                    break;
            }
        }

        if (!ModelState.IsValid)
        {
            // Fehler sichtbar machen: View mit gemergtem Stand zurueckgeben.
            var vm = await BuildViewModelAsync();
            return View(nameof(Index), vm);
        }

        TempData["SuccessMessage"] = "Einstellungen gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ServiceSettingsViewModel> BuildViewModelAsync()
    {
        var dbRows = await _repository.GetAllAsync();
        var dbByKey = dbRows.ToDictionary(s => s.Key, StringComparer.Ordinal);

        var vm = new ServiceSettingsViewModel();

        // Kategorie-Reihenfolge = Reihenfolge des ersten Vorkommens im Katalog (stabil).
        foreach (var def in ServiceSettingDefinitions.All)
        {
            var value = dbByKey.TryGetValue(def.Key, out var row) && row.Value != null
                ? row.Value
                : def.DefaultValue;

            var group = vm.Groups.FirstOrDefault(g => g.Category == def.Category);
            if (group == null)
            {
                group = new ServiceSettingGroup { Category = def.Category };
                vm.Groups.Add(group);
            }
            group.Items.Add(new ServiceSettingItem
            {
                Key = def.Key,
                Type = def.Type,
                Value = value,
                Description = def.Description,
                Multiline = def.Multiline
            });
        }

        // Orphans = DB-Keys ohne Katalog-Eintrag.
        var catalogKeys = ServiceSettingDefinitions.All.Select(d => d.Key).ToHashSet(StringComparer.Ordinal);
        vm.OrphanEntries = dbRows.Where(r => !catalogKeys.Contains(r.Key)).ToList();

        return vm;
    }

    // --- Freitext-Fallback fuer Orphan-/Ad-hoc-Keys (nicht mehr Hauptpfad) ---

    public IActionResult Create()
    {
        return View(new ServiceSetting());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceSetting setting)
    {
        if (!ModelState.IsValid)
            return View(setting);

        await _repository.UpsertAsync(setting.Key, setting.Value, setting.Category, setting.Description);
        TempData["SuccessMessage"] = "Einstellung gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var settings = await _repository.GetAllAsync();
        var item = settings.FirstOrDefault(s => s.Key == id);
        if (item == null)
            return NotFound();

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, ServiceSetting setting)
    {
        if (id != setting.Key)
            return NotFound();

        if (!ModelState.IsValid)
            return View(setting);

        await _repository.UpsertAsync(setting.Key, setting.Value, setting.Category, setting.Description);
        TempData["SuccessMessage"] = "Einstellung gespeichert.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        await _repository.DeleteAsync(id);
        TempData["SuccessMessage"] = "Einstellung gelöscht.";
        return RedirectToAction(nameof(Index));
    }
}
```
> Der `SaveSettings`-Fehlerpfad gibt `View(nameof(Index), vm)` zurück. Weil Task 4 die View neu schreibt (Model = `ServiceSettingsViewModel`), ist der Cast in `Index.cshtml` konsistent. Bis Task 4 kompiliert die alte View noch gegen `List<ServiceSetting>` — deshalb Build/Test erst NACH Task 4 der View grün für die *View*; die Controller-**Tests** (die die View nicht rendern) sind schon hier grün.

- [ ] Controller-Tests laufen (Green — Tests rendern keine View, nur Model/Redirect prüfen):
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --nologo --filter "FullyQualifiedName~ServiceSettingsControllerTests" 2>&1 | tail -6
```
Erwartet: 6 Tests grün, `Fehler: 0`.
> Falls der Build hier wegen der noch alten `Index.cshtml` (Model-Mismatch) fehlschlägt: Razor-Views werden bei `dotnet test` normalerweise NICHT streng typgeprüft (Runtime-Compilation). Falls doch ein Build-Fehler an `Views/ServiceSettings/Index.cshtml` auftritt, ziehe Task 4 (View-Rewrite) VOR und committe beide zusammen. Andernfalls hier committen.

- [ ] Vollständige Web-Tests grün (kein Regress an anderen Controllern):
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --nologo 2>&1 | tail -4
```
Erwartet: `Fehler: 0`.

- [ ] Commit:
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IdealAkeWms/Models/ViewModels/ServiceSettingsViewModel.cs IdealAkeWms/Controllers/ServiceSettingsController.cs IdealAkeWms.Tests/Controllers/ServiceSettingsControllerTests.cs && git commit -F - <<'EOF'
feat(service-settings): typisiertes ViewModel + Index-Merge + SaveSettings

ServiceSettingsViewModel (Groups je Kategorie + typisierte Items + Orphans).
Index merged Katalog∪DB (DB-Wert gewinnt, Default als Fallback), Orphans separat.
Neue SaveSettings(Dictionary<string,string>): Upsert je Key mit Kategorie/
Beschreibung aus Katalog, Bool -> "true"/"false" normalisiert, Int via
int.TryParse (Parsefehler -> ModelState + View zurueck, Key uebersprungen),
unbekannte Keys ignoriert. Create/Edit/Delete bleiben fuer Orphans.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
```

---

## Task 4: View `Views/ServiceSettings/Index.cshtml` neu — typisiert + Toggle

**Files:**
- Modify `IdealAkeWms/Views/ServiceSettings/Index.cshtml` (vollständig ersetzen)

Neue View mit Model `ServiceSettingsViewModel`, gruppiert nach Kategorie, ein `<form asp-action="SaveSettings">`. Bool → `form-check form-switch` + Hidden (`.bool-toggle`/`.bool-hidden`, Fallstrick beachtet); Int → `type=number`; String → text/textarea(Multiline). Orphans in eingeklappter „Erweitert"-Sektion mit Edit/Delete + „Neu". JS spiegelt `Views/Settings/Index.cshtml`.

- [ ] Ersetze `IdealAkeWms/Views/ServiceSettings/Index.cshtml` vollständig:
```cshtml
@model IdealAkeWms.Models.ViewModels.ServiceSettingsViewModel
@using IdealAkeWms.Models
@{
    ViewData["Title"] = "Service-Einstellungen";
}

<div class="d-flex justify-content-between align-items-center flex-wrap gap-2 mb-3">
    <h2 class="page-header mb-0">Service-Einstellungen</h2>
</div>

@if (TempData["SuccessMessage"] != null)
{
    <div class="alert alert-success">@TempData["SuccessMessage"]</div>
}
@if (TempData["WarningMessage"] != null)
{
    <div class="alert alert-warning">@TempData["WarningMessage"]</div>
}
@if (!ViewData.ModelState.IsValid)
{
    <div class="alert alert-warning">
        <strong>Nicht gespeichert:</strong>
        <ul class="mb-0">
            @foreach (var err in ViewData.ModelState.Values.SelectMany(v => v.Errors))
            {
                <li>@err.ErrorMessage</li>
            }
        </ul>
    </div>
}

<div class="alert alert-info mb-3">
    <strong>Hinweis:</strong> Diese Werte werden vom Windows-Dienst gelesen (DB gewinnt).
    Datenbankverbindungen (<code>ConnectionStrings</code>) und SMTP (<code>MailSettings</code>)
    bleiben in <code>appsettings.json</code> des Dienstes.
</div>

<form asp-action="SaveSettings" method="post">
    @Html.AntiForgeryToken()
    @foreach (var group in Model.Groups)
    {
        <div class="card mb-3">
            <div class="card-header fw-semibold">@group.Category</div>
            <div class="card-body">
                @foreach (var item in group.Items)
                {
                    <div class="mb-3" data-setting-row="@item.Key">
                        <label class="form-label fw-bold">@item.Key</label>
                        @if (!string.IsNullOrEmpty(item.Description))
                        {
                            <br /><small class="text-muted">@item.Description</small>
                        }
                        @if (item.Type == ServiceSettingType.Bool)
                        {
                            var isChecked = string.Equals(item.Value, "true", StringComparison.OrdinalIgnoreCase);
                            <input type="hidden" name="settings[@item.Key]" value="@(isChecked ? "true" : "false")" class="bool-hidden" />
                            <div class="form-check form-switch mt-1">
                                <input type="checkbox" class="form-check-input bool-toggle" id="setting_@item.Key" @(isChecked ? "checked" : "") />
                                <label class="form-check-label" for="setting_@item.Key">@(isChecked ? "Aktiviert" : "Deaktiviert")</label>
                            </div>
                        }
                        else if (item.Type == ServiceSettingType.Int)
                        {
                            <input type="number" step="1" name="settings[@item.Key]" value="@item.Value" class="form-control" />
                        }
                        else if (item.Multiline)
                        {
                            <textarea name="settings[@item.Key]" rows="2" class="form-control">@item.Value</textarea>
                        }
                        else
                        {
                            <input type="text" name="settings[@item.Key]" value="@item.Value" class="form-control" />
                        }
                    </div>
                }
            </div>
        </div>
    }

    <button type="submit" class="btn btn-primary mb-4">Einstellungen speichern</button>
</form>

<div class="card mb-3">
    <div class="card-header">
        <button class="btn btn-link p-0 text-decoration-none fw-semibold" type="button"
                data-bs-toggle="collapse" data-bs-target="#orphanSection" aria-expanded="false">
            Sonstige / Erweitert (@Model.OrphanEntries.Count)
        </button>
        <a asp-action="Create" class="btn btn-sm btn-outline-primary float-end">Neu</a>
    </div>
    <div id="orphanSection" class="collapse">
        <div class="card-body">
            @if (!Model.OrphanEntries.Any())
            {
                <p class="text-muted mb-0">Keine zusaetzlichen (nicht-katalogisierten) Eintraege.</p>
            }
            else
            {
                <div class="table-responsive">
                    <table class="table table-striped mb-0">
                        <thead>
                            <tr>
                                <th style="width: 30%">Schlüssel</th>
                                <th style="width: 30%">Wert</th>
                                <th>Beschreibung</th>
                                <th style="width: 130px;"></th>
                            </tr>
                        </thead>
                        <tbody>
                            @foreach (var item in Model.OrphanEntries)
                            {
                                <tr>
                                    <td><code>@item.Key</code></td>
                                    <td>@item.Value</td>
                                    <td class="text-muted small">@item.Description</td>
                                    <td>
                                        <div class="d-flex gap-1">
                                            <a asp-action="Edit" asp-route-id="@item.Key" class="btn btn-sm btn-secondary">Bearbeiten</a>
                                            <form asp-action="Delete" asp-route-id="@item.Key" method="post" class="m-0"
                                                  onsubmit="return confirm('Einstellung \'@item.Key\' löschen?');">
                                                @Html.AntiForgeryToken()
                                                <button type="submit" class="btn btn-sm btn-outline-danger">Löschen</button>
                                            </form>
                                        </div>
                                    </td>
                                </tr>
                            }
                        </tbody>
                    </table>
                </div>
            }
        </div>
    </div>
</div>

@section Scripts {
    <script>
        // Bool-Toggle: synchronisiert den Hidden-Wert + Label (Fallstrick: Hidden+Checkbox
        // gleicher name funktioniert NICHT — Checkbox hat KEIN name, Hidden traegt den name).
        document.querySelectorAll('.bool-toggle').forEach(function (cb) {
            cb.addEventListener('change', function () {
                var hidden = this.closest('[data-setting-row]').querySelector('.bool-hidden');
                if (hidden) hidden.value = this.checked ? 'true' : 'false';
                var label = this.parentElement.querySelector('.form-check-label');
                if (label) label.textContent = this.checked ? 'Aktiviert' : 'Deaktiviert';
            });
        });
    </script>
}
```

- [ ] Build grün (Razor kompiliert gegen das neue Model):
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet build IdealAkeWms.slnx --nologo 2>&1 | tail -3
```
Erwartet: `0 Fehler`.

- [ ] Web-Tests grün:
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --nologo 2>&1 | tail -4
```
Erwartet: `Fehler: 0`.

- [ ] Commit:
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IdealAkeWms/Views/ServiceSettings/Index.cshtml && git commit -F - <<'EOF'
feat(service-settings): typisierte /ServiceSettings-View mit Toggles

Nach Kategorie gruppiertes SaveSettings-Formular: Bool -> form-switch + Hidden
(bool-toggle/bool-hidden, JS-Label-Sync), Int -> type=number, String ->
text/textarea(Multiline), Beschreibung als form-text. Eingeklappte
"Sonstige/Erweitert"-Sektion fuer Orphans (Edit/Delete + Neu). ModelState-Fehler
werden oben als Warn-Alert gerendert.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
```

---

## Task 5: Service-Lesestellen DB-first umstellen (die 8 Keys) + Safe-Reader

**Files:**
- Modify `IDEALAKEWMSService/Common/ServiceSettings.cs` (neue `GetIntSafeAsync`/`GetBoolSafeAsync`)
- Modify `IDEALAKEWMSService/Workers/SyncWorker.cs:31-32` (SyncIntervalMinutes/SyncDryRun) + `:355` (Feiertag-Gate bleibt, aber Options → DB, siehe unten)
- Modify `IDEALAKEWMSService/Workers/NotificationWorker.cs:25` (NotificationCheckIntervalMinutes)
- Modify `IDEALAKEWMSService/Services/SyncErrorNotifier.cs:31-32` (ErrorNotification:Enabled/Recipients)
- Modify `IDEALAKEWMSService/Services/HolidaySyncService.cs` (Feiertag-Options DB-first überschreiben) — siehe Hinweis

> **Umfang-Klärung (aus Grep):** Die 8 Keys der Spec-Tabelle sind: `WorkerSettings:SyncIntervalMinutes`, `WorkerSettings:SyncDryRun`, `WorkerSettings:NotificationCheckIntervalMinutes`, `ErrorNotification:Enabled`, `ErrorNotification:Recipients`, `Sync:FeiertagCountryCode`, `Sync:FeiertagRegion`, `Sync:FeiertagJahreVoraus`. Die drei `Feiertag*`-Werte werden im Code NICHT direkt via `_configuration.GetValue` gelesen, sondern über `HolidaySyncOptions` (`IOptions<HolidaySyncOptions>`, gebunden in Program.cs an die `Sync`-Section). Der Gate `Sync:FeiertagSyncEnabled` (SyncWorker:355 + HolidaySyncOptions.Enabled) bleibt IConfiguration-getrieben (bewusst — der Worker-Gate und der Service-interne Gate lesen dieselbe Quelle; DB-Umstellung des Enable-Flags ist NICHT Teil dieser 8). MailSettings + ConnectionStrings bleiben unangetastet.

### 5a. Safe-Reader-Helper (resilient gegen transiente DB-Fehler)

- [ ] Ergänze in `IDEALAKEWMSService/Common/ServiceSettings.cs` (nach `GetIntAsync`, vor der schließenden `}`):
```csharp
    /// <summary>
    /// Wie <see cref="GetBoolAsync"/>, faengt aber transiente DB-Fehler ab und
    /// liefert dann den Default (statt zu werfen). Fuer Takt-relevante Reads,
    /// die den Worker-Loop nicht abwuergen duerfen.
    /// </summary>
    public static async Task<bool> GetBoolSafeAsync(IConfiguration config, string key, bool defaultValue, CancellationToken ct = default)
    {
        try { return await GetBoolAsync(config, key, defaultValue, ct); }
        catch (OperationCanceledException) { throw; }
        catch { return defaultValue; }
    }

    /// <summary>
    /// Wie <see cref="GetIntAsync"/>, faengt aber transiente DB-Fehler ab und
    /// liefert dann den Default (statt zu werfen).
    /// </summary>
    public static async Task<int> GetIntSafeAsync(IConfiguration config, string key, int defaultValue, CancellationToken ct = default)
    {
        try { return await GetIntAsync(config, key, defaultValue, ct); }
        catch (OperationCanceledException) { throw; }
        catch { return defaultValue; }
    }

    /// <summary>
    /// Wie <see cref="GetValueAsync"/>, faengt aber transiente DB-Fehler ab und
    /// liefert dann null (statt zu werfen).
    /// </summary>
    public static async Task<string?> GetValueSafeAsync(IConfiguration config, string key, CancellationToken ct = default)
    {
        try { return await GetValueAsync(config, key, ct); }
        catch (OperationCanceledException) { throw; }
        catch { return null; }
    }
```

### 5b. SyncWorker — Interval + DryRun DB-first (Takt-relevant → Safe-Reader)

- [ ] In `IDEALAKEWMSService/Workers/SyncWorker.cs`, ersetze Zeilen 31-32:
```csharp
            var intervalMinutes = _configuration.GetValue<int>("WorkerSettings:SyncIntervalMinutes", 15);
            var dryRun = _configuration.GetValue<bool>("WorkerSettings:SyncDryRun", false);
```
durch:
```csharp
            var intervalMinutes = await ServiceSettings.GetIntSafeAsync(_configuration, "WorkerSettings:SyncIntervalMinutes", 15, stoppingToken);
            var dryRun = await ServiceSettings.GetBoolSafeAsync(_configuration, "WorkerSettings:SyncDryRun", false, stoppingToken);
```
> `ExecuteAsync` ist bereits `async` und `stoppingToken` in Scope — `await` ist zulässig.

### 5c. NotificationWorker — CheckInterval DB-first (Takt-relevant → Safe-Reader)

- [ ] In `IDEALAKEWMSService/Workers/NotificationWorker.cs`, ersetze Zeile 25:
```csharp
            var intervalMinutes = _configuration.GetValue<int>("WorkerSettings:NotificationCheckIntervalMinutes", 60);
```
durch:
```csharp
            var intervalMinutes = await IDEALAKEWMSService.Common.ServiceSettings.GetIntSafeAsync(_configuration, "WorkerSettings:NotificationCheckIntervalMinutes", 60, stoppingToken);
```
> `NotificationWorker` hat kein `using IDEALAKEWMSService.Common;` — voll qualifiziert (oder das using ergänzen). `ExecuteAsync` ist `async`, `stoppingToken` in Scope.

### 5d. SyncErrorNotifier — Enabled + Recipients DB-first (Komma-String splitten)

- [ ] In `IDEALAKEWMSService/Services/SyncErrorNotifier.cs`, ersetze Zeilen 31-32:
```csharp
            var enabled = _config.GetValue<bool>("ErrorNotification:Enabled", false);
            var recipients = _config.GetSection("ErrorNotification:Recipients").Get<string[]>() ?? Array.Empty<string>();
```
durch:
```csharp
            var enabled = await IDEALAKEWMSService.Common.ServiceSettings.GetBoolSafeAsync(_config, "ErrorNotification:Enabled", false, ct);
            var recipientsRaw = await IDEALAKEWMSService.Common.ServiceSettings.GetValueSafeAsync(_config, "ErrorNotification:Recipients", ct);
            var recipients = (recipientsRaw ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
```
> `NotifyAsync` ist bereits `async Task` mit `ct` in Scope. Der bestehende `try/catch` in `NotifyAsync` (Notifier wirft NIE) fängt DB-Fehler ohnehin ab — die Safe-Reader sind zusätzlicher Gürtel. `System` (für `Array`/`StringSplitOptions`) ist implizit via `<ImplicitUsings>` verfügbar; `StringSplitOptions` braucht ggf. `using System;` (bereits vorhanden über global usings).

### 5e. HolidaySyncService — Feiertag Country/Region/JahreVoraus DB-first überschreiben

Die drei String/Int-Werte kommen aus `IOptions<HolidaySyncOptions>`. Statt die Options-Bindung umzubauen, werden in `RunAsync` die drei Werte NACH dem `_options.Value`-Zugriff aus der DB überschrieben (DB gewinnt), mit Fallback auf den Options-Wert.

- [ ] In `IDEALAKEWMSService/Services/HolidaySyncService.cs`, direkt nach `var opts = _options.Value;` (Zeile 56) und VOR dem `if (!opts.Enabled)`-Block, füge ein:
```csharp
            // DB-first (v1.25.0): Country/Region/JahreVoraus aus ServiceSettings ueberschreiben
            // (DB gewinnt). Enable-Flag bleibt aus IConfiguration/Options (Gate-Konsistenz).
            var dbCountry = await IDEALAKEWMSService.Common.ServiceSettings.GetValueSafeAsync(
                _config, "Sync:FeiertagCountryCode", ct);
            if (!string.IsNullOrWhiteSpace(dbCountry)) opts.CountryCode = dbCountry.Trim();

            var dbRegion = await IDEALAKEWMSService.Common.ServiceSettings.GetValueSafeAsync(
                _config, "Sync:FeiertagRegion", ct);
            if (dbRegion != null) opts.Region = dbRegion.Trim(); // leer erlaubt (= keine Region)

            opts.JahreVoraus = await IDEALAKEWMSService.Common.ServiceSettings.GetIntSafeAsync(
                _config, "Sync:FeiertagJahreVoraus", opts.JahreVoraus, ct);
```
> **Voraussetzung:** `HolidaySyncService` braucht `IConfiguration _config`. Prüfe den Konstruktor: aktuell injiziert er `ApplicationDbContext ctx, HttpClient http, IOptions<HolidaySyncOptions> options, ILogger logger, ISyncLogger syncLogger` — **kein** `IConfiguration`. Ergänze `IConfiguration config` als Konstruktor-Parameter (nach `http`, vor `options`) + Feld `private readonly IConfiguration _config;` + Zuweisung. DI liefert `IConfiguration` automatisch. `using Microsoft.Extensions.Configuration;` ist bereits vorhanden (Zeile 5).
> Konkret: Konstruktor-Signatur wird
> `public HolidaySyncService(ApplicationDbContext ctx, HttpClient http, IConfiguration config, IOptions<HolidaySyncOptions> options, ILogger<HolidaySyncService> logger, ISyncLogger syncLogger)` mit `_config = config;`.

- [ ] Build grün:
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet build IdealAkeWms.slnx --nologo 2>&1 | tail -3
```
Erwartet: `0 Fehler`.

- [ ] Service-Tests grün (kein Regress; `HolidaySyncService`-Tests müssen den neuen Ctor-Param bekommen):
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IDEALAKEWMSService.Tests --nologo 2>&1 | tail -6
```
Erwartet: `Fehler: 0`.
> **Falls rot** wegen `HolidaySyncService`-Konstruktor in bestehenden Tests: die Tests neu-verdrahten (zusätzliches `Mock.Of<IConfiguration>()` bzw. eine In-Memory-`ConfigurationBuilder().Build()` als `config`-Arg übergeben). Suche vorher:
> ```bash
> cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && grep -rln "new HolidaySyncService(" IDEALAKEWMSService.Tests/
> ```
> und passe jeden Fundort an: `new HolidaySyncService(ctx, http, new ConfigurationBuilder().Build(), options, logger, syncLogger)`.

- [ ] Web-Tests grün (Katalog/Controller unberührt, aber Vollpass):
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --nologo 2>&1 | tail -4
```
Erwartet: `Fehler: 0`.

- [ ] Commit:
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IDEALAKEWMSService/Common/ServiceSettings.cs IDEALAKEWMSService/Workers/SyncWorker.cs IDEALAKEWMSService/Workers/NotificationWorker.cs IDEALAKEWMSService/Services/SyncErrorNotifier.cs IDEALAKEWMSService/Services/HolidaySyncService.cs && git commit -F - <<'EOF'
feat(service-settings): 8 Service-Lesestellen DB-first (resilient)

GetBoolSafeAsync/GetIntSafeAsync/GetValueSafeAsync (try/catch -> Default) fuer
Takt-relevante Reads. WorkerSettings:SyncIntervalMinutes/SyncDryRun (SyncWorker),
WorkerSettings:NotificationCheckIntervalMinutes (NotificationWorker),
ErrorNotification:Enabled/Recipients (SyncErrorNotifier, Recipients jetzt
Komma-String -> Split) und Sync:FeiertagCountryCode/Region/JahreVoraus
(HolidaySyncService, DB ueberschreibt Options) lesen jetzt DB-first.
MailSettings + ConnectionStrings bleiben appsettings-only.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
```

---

## Task 6: Dokumentation — Changelog + CLAUDE.md + TESTSZENARIEN + PROJECT_STATUS

**Files:**
- Modify `IdealAkeWms/Views/Help/Changelog.cshtml` (v1.25.0-Card erweitern, KEIN neuer Versions-Card)
- Modify `CLAUDE.md` (Fallstrick + Service-Konfig-Tabellen-Hinweis)
- Modify `docs/TESTSZENARIEN.md` (neues Kapitel 51)
- Modify `PROJECT_STATUS.md` (v1.25.0-Abschnitt ergänzen)

### 6a. Changelog — Bullet in den bestehenden v1.25.0-Card

- [ ] In `IdealAkeWms/Views/Help/Changelog.cshtml`, füge in die v1.25.0-`<ul>` (endet bei Zeile 56 `</ul>`) vor `</ul>` einen neuen `<li>` ein:
```html
                    <li><strong>Service-Einstellungen typisiert &amp; vollstaendig:</strong> Die Seite
                        &bdquo;Service-Einstellungen&ldquo; zeigt jetzt jede Einstellung typgerecht &ndash;
                        Ja/Nein-Schalter, Zahlenfelder, Textfelder &ndash; und ist vollstaendig (auch die
                        frueher nur in <code>appsettings.json</code> liegenden Sync-/Worker-/Fehlermail-/
                        Feiertags-Werte). Die Datenbank ist ab jetzt die einzige Steuerungsquelle
                        (DB gewinnt); nur Datenbankverbindungen und SMTP bleiben in der Dienst-Konfiguration.</li>
```

### 6b. CLAUDE.md — Fallstrick + Tabellen-Hinweis

- [ ] Ergänze in `CLAUDE.md` einen neuen Fallstrick-Bullet im Abschnitt „## Bekannte Fallstricke" (ans Ende der Liste):
```markdown
- **ServiceSettings katalog-getrieben + typisiert (v1.25.0)**: Der `/ServiceSettings`-Editor ist jetzt typisiert (Bool-Toggle/Int/String) und **vollstaendig** — getrieben vom Katalog `IdealAkeWms/Models/ServiceSettingDefinitions.All` (Single Source of Truth fuer Typ/Default/Kategorie/Beschreibung, liegt im Web-Projekt, Service nutzt ihn via ProjectReference). `Program.cs` seedet idempotent ueber `ServiceSettingDefinitions.All`; `ServiceSettingsController.SaveSettings(Dictionary<string,string>)` upsertet je Key mit Kategorie/Beschreibung aus dem Katalog (Bool → "true"/"false" normalisiert, Int via `int.TryParse` → ModelState-Fehler + View bei Parsefehler, unbekannte Keys ignoriert). **DB gewinnt:** appsettings.json `Sync:`/`WorkerSettings:`/`ErrorNotification:`/`Feiertag`-Werte werden vom Service NICHT mehr gelesen (nur noch Default-Referenz) — Steuerung ausschliesslich ueber `/ServiceSettings`. **Ausnahme:** `MailSettings:*` (SMTP) + `ConnectionStrings:*` bleiben appsettings-only. Neuer Service-Key → IMMER in den Katalog aufnehmen (Drift-Guard-Test `ServiceSettingDefinitionsTests.All_ContainsDocumentedServiceReadKey` schlaegt sonst fehl). Takt-relevante Reads (Worker-Intervall/DryRun) nutzen `ServiceSettings.GetIntSafeAsync`/`GetBoolSafeAsync` (try/catch → Default), damit ein transienter DB-Fehler den Worker-Loop nicht abwuergt. **Fallstrick Hidden+Checkbox:** in der View traegt der Hidden-Input `name="settings[Key]"`, die Checkbox hat KEIN name und synct per JS.
```

- [ ] Im Abschnitt „## Service-Konfiguration (appsettings.json / ServiceSettings DB)" einen Kopfzeilen-Hinweis ergänzen (direkt unter der Ueberschrift, vor der Tabelle):
```markdown
> **Seit v1.25.0:** Alle hier gelisteten Keys (ausser `Security:*`) sind **DB-getrieben** — sie werden vom Service aus der `[ServiceSettings]`-Tabelle gelesen (DB gewinnt) und in `/ServiceSettings` typisiert gepflegt. Die appsettings.json-Werte sind nur noch Default-Referenz. Katalog: `IdealAkeWms/Models/ServiceSettingDefinitions.All`.
```

### 6c. TESTSZENARIEN — neues Kapitel 51

- [ ] Hänge ans Ende von `docs/TESTSZENARIEN.md` an:
```markdown

## Kapitel 51: Typisierte, vollständige Service-Einstellungen (v1.25.0)

**Vorbedingung:** Als `admin` eingeloggt. Windows-Dienst läuft (für die Wirkungs-Checks).

### TS-51.1 — Vollständigkeit + Typisierung
1. `/ServiceSettings` öffnen.
2. **Erwartet:** Jede Einstellung erscheint typgerecht: Bool-Keys (z.B. `Sync:BomCacheEnabled`, `WorkerSettings:SyncDryRun`, `ErrorNotification:Enabled`) als **Aktiv/Inaktiv**-Schalter; Int-Keys (z.B. `Sync:BomCacheWeeks`, `WorkerSettings:SyncIntervalMinutes`) als **Zahlenfeld**; String-Keys (z.B. `Sync:FeiertagCountryCode`, `ErrorNotification:Recipients`) als **Textfeld/Textarea**. Alle 32 Katalog-Keys sind sichtbar, nach Kategorie gruppiert (Sync, BOM-Cache, FA-Vervollstaendigung, Lackierteile, BDE, Feiertage, Worker, Fehlermail, Benachrichtigungen).

### TS-51.2 — Bool-Toggle speichern + Wirkung
1. `Sync:ProductionOrderReconcileEnabled` von Inaktiv auf **Aktiv** schalten → „Einstellungen speichern".
2. **Erwartet:** Erfolgs-Alert; nach Reload steht der Toggle auf Aktiv. In DB `[ServiceSettings]` Key = `Sync:ProductionOrderReconcileEnabled`, Value = `true`.
3. Nächsten Sync-Zyklus abwarten → im Aktivitäts-Protokoll erscheint der Reconcile-Lauf scharf (nicht mehr „Deaktiviert").

### TS-51.3 — Int-Validierung (Negativfall)
1. In `Sync:BomCacheWeeks` `abc` eintippen (Zahlenfeld erlaubt das ggf. nur per Paste/DevTools — alternativ ein anderes Int-Feld leeren und Buchstaben einfügen) → speichern.
2. **Erwartet:** KEIN Erfolgs-Redirect; oben ein Warn-Alert „'Sync:BomCacheWeeks' erwartet eine ganze Zahl". Der Wert in der DB bleibt unverändert; gültige Felder im selben Submit wurden gespeichert.

### TS-51.4 — String/Textarea (Empfänger)
1. `ErrorNotification:Recipients` = `a@ake.at, b@ake.at` speichern.
2. **Erwartet:** Nach Reload steht der Komma-String im Feld. Bei einem provozierten Sync-Fehler (und `ErrorNotification:Enabled` = Aktiv) geht eine Fehlermail an beide Adressen (Split auf Komma).

### TS-51.5 — DB gewinnt über appsettings
1. In `appsettings.json` des Dienstes `WorkerSettings:SyncIntervalMinutes` = `99` setzen, aber in `/ServiceSettings` `15` lassen.
2. **Erwartet:** Der Dienst taktet mit **15** (DB-Wert), nicht 99 — die appsettings-Zahl wird ignoriert.

### TS-51.6 — Orphan-Fallback
1. Über „Neu" einen Ad-hoc-Key `Test:Orphan` = `x` anlegen.
2. **Erwartet:** Er erscheint NICHT in den typisierten Gruppen, sondern in der eingeklappten „Sonstige/Erweitert"-Sektion mit Bearbeiten/Löschen.
```

### 6d. PROJECT_STATUS — Bullet im v1.25.0-Abschnitt

- [ ] Ergänze im v1.25.0-Abschnitt von `PROJECT_STATUS.md` (nach dem FA-Reconciliation-Bullet, ~Zeile 57) einen Bullet:
```markdown
- **ServiceSettings typisiert + vollstaendig (v1.25.0):** Katalog `ServiceSettingDefinitions.All` (32 Keys) treibt Seeding (`Program.cs`) + typisierte `/ServiceSettings`-UI (Bool-Toggle/Int/String). 8 Service-Lesestellen (Worker-Intervall/DryRun, NotificationInterval, ErrorNotification, Feiertag-Country/Region/JahreVoraus) DB-first mit resilientem Fallback. DB gewinnt; nur MailSettings/ConnectionStrings bleiben appsettings. Tests: `ServiceSettingDefinitionsTests` (Drift-Guard) + `ServiceSettingsControllerTests` (Merge + Validierung). Kein Schema-Change. TESTSZENARIEN Kap. 51.
```

- [ ] Build grün (Changelog-Razor kompiliert):
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet build IdealAkeWms.slnx --nologo 2>&1 | tail -3
```
Erwartet: `0 Fehler`.

- [ ] Commit:
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IdealAkeWms/Views/Help/Changelog.cshtml CLAUDE.md docs/TESTSZENARIEN.md PROJECT_STATUS.md && git commit -F - <<'EOF'
docs(service-settings): Changelog/CLAUDE.md/TESTSZENARIEN/PROJECT_STATUS

v1.25.0-Changelog-Bullet (typisierte, vollstaendige Service-Einstellungen),
CLAUDE.md-Fallstrick (katalog-getrieben, DB gewinnt, MailSettings/
ConnectionStrings bleiben appsettings) + Tabellen-Hinweis, TESTSZENARIEN
Kapitel 51 (jeder Typ speichern + Wirkung, int-Validierung, DB-gewinnt,
Orphan-Fallback), PROJECT_STATUS-Bullet.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
```

---

## Task 7: Final-Check — Build + Tests + Grep-Beweise

**Files:** keine Änderung (ggf. Fix-Commit).

- [ ] Build 0 Fehler:
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet build IdealAkeWms.slnx --nologo 2>&1 | tail -3
```
Erwartet: `0 Fehler`.

- [ ] Web-Tests + Service-Tests grün:
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --nologo 2>&1 | tail -4 && dotnet test IDEALAKEWMSService.Tests --nologo 2>&1 | tail -4
```
Erwartet: beide `Fehler: 0`.

- [ ] Grep-Beweis 1 — jeder service-gelesene Key-String ist im Katalog (kein Read ohne Katalog-Eintrag). Liste alle `"Prefix:Suffix"`-Key-Literale im Service + prüfe manuell gegen `ServiceSettingDefinitions.All`:
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && rg -oN '"[A-Za-z]+:[A-Za-z]+"' IDEALAKEWMSService/Workers IDEALAKEWMSService/Services IDEALAKEWMSService/Common | sort -u
```
Erwartet: jeder ausgegebene Key (z.B. `"Sync:BomCacheEnabled"`, `"WorkerSettings:SyncIntervalMinutes"`, `"ErrorNotification:Enabled"`, `"Notifications:MeldebestandEnabled"` etc.) ist im Katalog. Der Drift-Guard-Test `All_ContainsDocumentedServiceReadKey` deckt die dokumentierten Keys bereits ab. (Ignoriere Nicht-Setting-Literale wie `"DefaultConnection"` bzw. Enum-/URL-Fragmente — relevant sind nur `Sync:`/`WorkerSettings:`/`ErrorNotification:`/`Notifications:`-Keys.)

- [ ] Grep-Beweis 2 — kein `_configuration.GetValue`/`_config.GetValue`/`GetSection(...).Get<string[]>` mehr für die 8 umgestellten Keys (ausser MailSettings/ConnectionStrings/`Sync:FeiertagSyncEnabled`-Gate/`Sync:LagerbestandEnabled`-Gate/`Sync:*Enabled`-Sync-Gates, die bewusst IConfiguration bleiben):
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && rg -n "GetValue<|GetSection\(" IDEALAKEWMSService/Workers/SyncWorker.cs IDEALAKEWMSService/Workers/NotificationWorker.cs IDEALAKEWMSService/Services/SyncErrorNotifier.cs
```
Erwartet: **KEIN** Treffer mehr für `WorkerSettings:SyncIntervalMinutes`, `WorkerSettings:SyncDryRun`, `WorkerSettings:NotificationCheckIntervalMinutes`, `ErrorNotification:Enabled`, `ErrorNotification:Recipients`. Verbliebene `GetValue`-Treffer in SyncWorker sind nur die **Sync-Block-Enable-Gates** (`Sync:ProductionOrdersEnabled`, `Sync:ArticlesEnabled`, `Sync:OseonTrackingEnabled`, `Sync:EnaioDmsEnabled`, `Sync:OseonArticleCategoryEnabled`, `Sync:PartRequisitionEmailEnabled`, `Sync:WarehouseRequisitionEmailEnabled`, `Sync:LagerplaetzeEnabled`, `Sync:LagerbestandEnabled`, `Sync:LagerbestandIntervalMinutes`, `Sync:FeiertagSyncEnabled`) — diese sind NICHT Teil der 8 und bleiben bewusst IConfiguration (siehe Task-5-Umfangsklärung; die Katalog-Zeilen existieren trotzdem fürs Seeding, aber die Enable-Gate-Umstellung ist YAGNI-außerhalb-Scope). Wenn ein späterer Task auch diese umstellen will → separater Plan.
> **Wichtige Nuance für den Ausführenden:** Der Scope dieses Plans sind exakt die **8** Keys der Spec-Tabelle. Die vielen `Sync:*Enabled`-Gate-Reads via `_configuration.GetValue` bleiben unverändert — sie zu ändern ist NICHT gefordert und würde den Diff aufblähen. Der Katalog listet sie dennoch (Seeding + UI-Sichtbarkeit), was korrekt ist: die UI schreibt sie in die DB, aber der Worker-Gate liest sie (noch) aus IConfiguration. Das ist bewusst und in Task 6 dokumentiert („DB gewinnt" gilt vollständig erst nach optionaler Folge-Umstellung der Gates — für die 8 Keys gilt es sofort).

- [ ] Falls Beweise/Tests einen Rest aufdecken: minimal fixen + committen:
```bash
cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add -A && git commit -F - <<'EOF'
fix(service-settings): Final-Check-Nacharbeit

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
```
> Wenn nichts zu fixen ist, entfällt dieser Commit.

---

## File-Struktur-Übersicht

```
IdealAkeWms/
  Models/
    ServiceSettingDefinition.cs         (NEU — enum ServiceSettingType + record)
    ServiceSettingDefinitions.cs        (NEU — All-Katalog, 32 Keys, TryGet)
    ViewModels/
      ServiceSettingsViewModel.cs       (NEU — Groups/Items/Orphans)
  Controllers/
    ServiceSettingsController.cs        (GEÄNDERT — Index-Merge + SaveSettings, Orphan-CRUD bleibt)
  Views/
    ServiceSettings/
      Index.cshtml                      (GEÄNDERT — typisierte gruppierte Form + Toggle-JS)
    Help/
      Changelog.cshtml                  (GEÄNDERT — v1.25.0-Bullet, KEIN neuer Card)
  Program.cs                            (GEÄNDERT — Seed-Loop über Katalog, Zeilen ~379-414)

IdealAkeWms.Tests/
  Models/
    ServiceSettingDefinitionsTests.cs   (NEU — Drift-Guard + Konsistenz)
  Controllers/
    ServiceSettingsControllerTests.cs   (NEU — Index-Merge + SaveSettings-Validierung)

IDEALAKEWMSService/
  Common/
    ServiceSettings.cs                  (GEÄNDERT — GetBoolSafeAsync/GetIntSafeAsync/GetValueSafeAsync)
  Workers/
    SyncWorker.cs                       (GEÄNDERT — SyncIntervalMinutes/SyncDryRun DB-first)
    NotificationWorker.cs               (GEÄNDERT — NotificationCheckIntervalMinutes DB-first)
  Services/
    SyncErrorNotifier.cs                (GEÄNDERT — ErrorNotification:Enabled/Recipients DB-first)
    HolidaySyncService.cs               (GEÄNDERT — Feiertag Country/Region/JahreVoraus DB-first + IConfiguration-Ctor-Param)

CLAUDE.md                               (GEÄNDERT — Fallstrick + Service-Konfig-Tabellen-Hinweis)
docs/TESTSZENARIEN.md                   (GEÄNDERT — Kapitel 51)
PROJECT_STATUS.md                       (GEÄNDERT — v1.25.0-Bullet)
```

**Katalog-Bilanz:** 32 Keys = **17 bool** + **9 int** + **6 string** (davon 2 Multiline: `ErrorNotification:Recipients`, `Notifications:Recipients`).

**Nicht im Scope:** AppSettings-Seite (`/Settings`), MailSettings/SMTP + ConnectionStrings (bleiben appsettings), Sync-Block-Enable-Gates (`Sync:*Enabled`-Reads via IConfiguration bleiben — nur die 8 Spec-Keys werden umgestellt), Caching, Schema-Change/Migration.
