# Lagerbestellung aus der Stückliste (BOM) + Modul-Master-Schalter — Implementierungsplan

**For agentic workers:** Execute the tasks in order. Each task is self-contained: it lists the exact files (with line anchors), the failing test to write first (TDD), the implementation, the exact build/test commands with their expected output, and the commit. Do not skip the red→green cycle. All paths are absolute inside the worktree `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\glas-bestellung`. Git runs via the Bash tool: `cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git ...`.

**Spec:** `docs/superpowers/specs/2026-07-09-lagerbestellung-aus-bom-design.md` (verbindlich).
**Branch/Worktree:** `feature/glas-bestellung` → `.claude/worktrees/glas-bestellung` (Basis-HEAD `a2d0c42`).
**Version:** in **v1.25.0** falten — **KEIN** `AppVersion.cs`-Bump, nur Changelog-Card v1.25.0 erweitern.

## Goal

Neben dem bestehenden **Bedarfsmeldung**-Button in der Stückliste bekommt jede Nicht-Baugruppen-Zeile einen zusätzlichen **Lagerbestellung**-Button (Einzel) plus einen **Bulk-Button** (Multi-Select) für markierte Zeilen. Klick legt (typ-abhängig: Lager vs. Glas, automatisch aus der Artikelgruppe) einen offenen Entwurf des Users an bzw. wiederverwendet ihn, fügt die Position(en) hinzu und navigiert zur Bearbeiten-Seite (ein Typ) oder zur Übersicht (gemischt). Zusätzlich schaltet ein neuer AppSetting-Master-Schalter `LagerbestellungAktiv` (Default **`true`**) das komplette Lagerbestellungs-Modul ein/aus.

## Architecture

- **Kein Schema-Change / keine Migration.** Reuse von `CreateDraftAsync`, `AddItemAsync`, `GlasArticleGroupFilter`, `CanOrderLager/GlasAsync`. Nur: neuer AppSetting-Seed (Runtime), neues Filter-Attribut, eine neue API-Action, eine neue Repo-Query, zwei neue ViewBags, View-Erweiterung, Layout-Gating.
- **Master-Schalter `LagerbestellungAktiv`** — Default `true` (bewusst abweichend von den Opt-in-Modulen `BestellungenAktiv`/`LeitstandAktiv`/`FaCompletionAktiv` mit Default `false`): das Lagerbestellungs-Modul ist heute bereits in Betrieb; ein Default `false` würde es beim Deploy abschalten. Der Admin kann es jederzeit ausschalten.
- **Default-true Read-Semantik (kanonisch, an ALLEN Stellen identisch):** `!string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase)` — d. h. `null` (fehlend) → aktiv, `"true"` → aktiv, `"false"` → gesperrt. Bewusst NICHT das `?.Equals("true",…) == true`-Muster der Opt-in-Bools (das defaultet auf false).
- **Filter `RequireLagerbestellungAktivAttribute`** (`TypeFilterAttribute` + `IAsyncActionFilter`, analog `RequirePickingOrStockOrLagerbestellungAccessFilter`): MVC (`context.Controller is Controller`) → `RedirectToActionResult("Index","Home")` + `TempData["WarningMessage"]`; API (`[ApiController]` erbt nur `ControllerBase`) → `NotFoundResult`. Kumuliert mit den bestehenden Rollen-Filtern.
- **Quick-Add-API** `POST /api/warehouserequisitions/quick-add`: EIN Endpunkt für Einzel (Liste mit 1 Item) UND Bulk. Typ automatisch aus `GlasArticleGroupFilter.NormalizeGroup(article.ArticleGroup) ∈ ParseGroups(GlasArtikelgruppen)`; Rechte je Typ; pro Typ genau EIN Draft je Lauf (lazy via `GetOpenDraftForUserAndTypeAsync` → sonst `CreateDraftAsync` mit User-Default-Werkbank); ungültige Items in `skipped` sammeln, nicht abbrechen; alles skipped → `BadRequest`.

## Tech Stack

- ASP.NET Core 10.0 MVC + Repository Pattern + DI, EF Core 10.0 (SQL Server), Bootstrap 5, jQuery/Select2 (bereits in der BOM-View geladen).
- Tests: xUnit + FluentAssertions + Moq + EF InMemory via `TestDbContextFactory.Create()`.
- Build: `dotnet build IdealAkeWms.slnx`. Web-Tests: `dotnet test IdealAkeWms.Tests --nologo`. Kein Service-Test-Impact erwartet.

---

## Task 0: Pre-Flight — Build + Web-Tests grün, HEAD prüfen

**Ziel:** Ausgangslage verifizieren, bevor irgendetwas geändert wird.

- [ ] HEAD prüfen:
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git rev-parse --short HEAD && git status --porcelain
  ```
  Erwartet: `a2d0c42`, danach **keine** Zeilen (sauberer Tree).
- [ ] Build grün:
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet build IdealAkeWms.slnx
  ```
  Erwartet: `Build succeeded.` mit `0 Error(s)`.
- [ ] Web-Tests grün:
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --nologo
  ```
  Erwartet: `Passed!` (0 Failed). Merke die Baseline-Anzahl (`Passed: N`).
- [ ] Kein Commit in diesem Task.

---

## Task 1: AppSetting `LagerbestellungAktiv` (Konstante + Seed + Settings-Toggle)

**Files:**
- `IdealAkeWms/Models/AppSettingKeys.cs:20` (Block „Picking / Leitstand / Warehouse Requisitions")
- `IdealAkeWms/Program.cs:291` (`requisitionSettings`-Array)
- `IdealAkeWms/Views/Settings/Index.cshtml:40` (Gruppe „Bestellungen")

**Steps:**

- [ ] Konstante ergänzen in `AppSettingKeys.cs` direkt nach `BestellungenAktiv` (Zeile 20):
  ```csharp
      public const string BestellungenAktiv = "BestellungenAktiv";
      public const string LagerbestellungAktiv = "LagerbestellungAktiv";
  ```
- [ ] Seed ergänzen in `Program.cs` im `requisitionSettings`-Array (nach der `GemeinsameArtikelgruppen`-Zeile, aktuell Zeile 297). **Default `"true"`** — bewusst abweichend von den false-Default-Modulen:
  ```csharp
      ("GemeinsameArtikelgruppen", "EUZ", "Kommaseparierte Artikelgruppen, die in Lager- UND Glas-Bestellungen verfuegbar sind"),
      (IdealAkeWms.Models.AppSettingKeys.LagerbestellungAktiv, "true", "Lagerbestellungs-Modul aktivieren (Lager+Glas, Meine Fehlteile, Lager-Worklists, BOM-Button). Default true — bestehende Systeme bleiben aktiv."),
  ```
- [ ] Settings-View: den Key in die „Bestellungen"-Gruppe aufnehmen (`Views/Settings/Index.cshtml:40`). Die View erkennt Bool automatisch (Value `"true"`/`"false"` → Toggle), daher genügt der Key im Array:
  ```csharp
      ("Bestellungen", new[] { "BestellungenAktiv", "LagerbestellungAktiv", "DefaultLagerbestellempfaengerId", "DefaultGlasbestellempfaengerId", "GlasArtikelgruppen", "GemeinsameArtikelgruppen" }),
  ```
- [ ] Build:
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet build IdealAkeWms.slnx
  ```
  Erwartet: `Build succeeded.` `0 Error(s)`.
- [ ] Commit:
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add -A && git commit -m "feat(settings): AppSetting LagerbestellungAktiv (Default true) + Seed + Settings-Toggle" -m "Master-Schalter fuer das Lagerbestellungs-Modul (v1.25.0). Default true, damit bestehende Systeme beim Deploy nicht abgeschaltet werden." -m "Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
  ```

---

## Task 2: Filter `RequireLagerbestellungAktivAttribute` + class-level anwenden + Layout-Gating + Tests

**Files (neu):**
- `IdealAkeWms/Filters/RequireLagerbestellungAktivAttribute.cs`
- `IdealAkeWms.Tests/Filters/RequireLagerbestellungAktivFilterTests.cs`

**Files (geändert):**
- `IdealAkeWms/Controllers/WarehouseRequisitionsController.cs:10` (class-level Attribut)
- `IdealAkeWms/Controllers/Api/WarehouseRequisitionsApiController.cs:11` (class-level Attribut)
- `IdealAkeWms/Controllers/MissingPartsController.cs:10`
- `IdealAkeWms/Controllers/MissingPartsLagerController.cs:10`
- `IdealAkeWms/Controllers/WarehousePickingController.cs:10`
- `IdealAkeWms/Views/Shared/_Layout.cshtml:30-49` (neue Variable) + `:129-150` (4 Menü-Einträge gaten)

**Steps:**

- [ ] **RED** — Filter-Test schreiben `IdealAkeWms.Tests/Filters/RequireLagerbestellungAktivFilterTests.cs`:
  ```csharp
  using FluentAssertions;
  using IdealAkeWms.Data.Repositories;
  using IdealAkeWms.Filters;
  using IdealAkeWms.Models;
  using Microsoft.AspNetCore.Http;
  using Microsoft.AspNetCore.Mvc;
  using Microsoft.AspNetCore.Mvc.Abstractions;
  using Microsoft.AspNetCore.Mvc.Filters;
  using Microsoft.AspNetCore.Mvc.ViewFeatures;
  using Microsoft.AspNetCore.Routing;
  using Moq;
  using Xunit;

  namespace IdealAkeWms.Tests.Filters;

  public class RequireLagerbestellungAktivFilterTests
  {
      // Dummy-MVC-Controller (erbt Controller -> hat TempData) fuer den Redirect-Zweig.
      private class DummyMvcController : Controller { }
      // Dummy-API-Controller (erbt nur ControllerBase) fuer den 404-Zweig.
      private class DummyApiController : ControllerBase { }

      private static (ActionExecutingContext ctx, ActionExecutionDelegate next, bool[] called) MakeContext(object controller)
      {
          var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
          var ctx = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(),
              new Dictionary<string, object?>(), controller);
          var called = new[] { false };
          ActionExecutionDelegate next = () =>
          {
              called[0] = true;
              return Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), controller));
          };
          return (ctx, next, called);
      }

      private static RequireLagerbestellungAktivFilter MakeFilter(string? settingValue)
      {
          var settings = new Mock<IAppSettingRepository>();
          settings.Setup(s => s.GetValueAsync(AppSettingKeys.LagerbestellungAktiv)).ReturnsAsync(settingValue);
          return new RequireLagerbestellungAktivFilter(settings.Object);
      }

      [Fact]
      public async Task Aktiv_true_LaesstDurch()
      {
          var filter = MakeFilter("true");
          var (ctx, next, called) = MakeContext(new DummyMvcController());
          await filter.OnActionExecutionAsync(ctx, next);
          called[0].Should().BeTrue();
          ctx.Result.Should().BeNull();
      }

      [Fact]
      public async Task Fehlend_null_DefaultAktiv_LaesstDurch()
      {
          var filter = MakeFilter(null); // Default true
          var (ctx, next, called) = MakeContext(new DummyMvcController());
          await filter.OnActionExecutionAsync(ctx, next);
          called[0].Should().BeTrue();
          ctx.Result.Should().BeNull();
      }

      [Fact]
      public async Task Inaktiv_false_Mvc_RedirectHome()
      {
          var mvc = new DummyMvcController
          {
              TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
          };
          var filter = MakeFilter("false");
          var (ctx, next, called) = MakeContext(mvc);
          await filter.OnActionExecutionAsync(ctx, next);
          called[0].Should().BeFalse();
          ctx.Result.Should().BeOfType<RedirectToActionResult>();
          var r = (RedirectToActionResult)ctx.Result!;
          r.ActionName.Should().Be("Index");
          r.ControllerName.Should().Be("Home");
          mvc.TempData["WarningMessage"].Should().NotBeNull();
      }

      [Fact]
      public async Task Inaktiv_false_Api_NotFound()
      {
          var filter = MakeFilter("false");
          var (ctx, next, called) = MakeContext(new DummyApiController());
          await filter.OnActionExecutionAsync(ctx, next);
          called[0].Should().BeFalse();
          ctx.Result.Should().BeOfType<NotFoundResult>();
      }
  }
  ```
- [ ] Test läuft ROT (Filter existiert noch nicht → Compile-Fehler):
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --filter "FullyQualifiedName~RequireLagerbestellungAktivFilterTests" --nologo
  ```
  Erwartet: Build/Compile-Fehler (Typ `RequireLagerbestellungAktivFilter` nicht gefunden).
- [ ] **GREEN** — Filter implementieren `IdealAkeWms/Filters/RequireLagerbestellungAktivAttribute.cs`:
  ```csharp
  using IdealAkeWms.Data.Repositories;
  using IdealAkeWms.Models;
  using Microsoft.AspNetCore.Mvc;
  using Microsoft.AspNetCore.Mvc.Filters;

  namespace IdealAkeWms.Filters;

  /// <summary>
  /// Master-Schalter-Gate (v1.25.0): sperrt das komplette Lagerbestellungs-Modul,
  /// wenn AppSetting <c>LagerbestellungAktiv</c> == "false".
  /// Default-Semantik: fehlend/"true" => aktiv, nur "false" => gesperrt (Default true,
  /// damit bestehende Systeme beim Deploy nicht abgeschaltet werden).
  /// MVC-Controller (erbt <see cref="Controller"/>) => Redirect Home + WarningMessage;
  /// API-Controller ([ApiController], erbt nur <see cref="ControllerBase"/>) => 404.
  /// Kumuliert mit den Rollen-Filtern (beide muessen passieren).
  /// </summary>
  public class RequireLagerbestellungAktivAttribute : TypeFilterAttribute
  {
      public RequireLagerbestellungAktivAttribute() : base(typeof(RequireLagerbestellungAktivFilter)) { }
  }

  public class RequireLagerbestellungAktivFilter : IAsyncActionFilter
  {
      private readonly IAppSettingRepository _settings;

      public RequireLagerbestellungAktivFilter(IAppSettingRepository settings)
      {
          _settings = settings;
      }

      public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
      {
          var raw = await _settings.GetValueAsync(AppSettingKeys.LagerbestellungAktiv);
          var aktiv = !string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase);
          if (aktiv)
          {
              await next();
              return;
          }

          if (context.Controller is Controller mvc)
          {
              mvc.TempData["WarningMessage"] = "Das Lagerbestellungs-Modul ist deaktiviert.";
              context.Result = new RedirectToActionResult("Index", "Home", null);
          }
          else
          {
              context.Result = new NotFoundResult();
          }
      }
  }
  ```
- [ ] Class-level Attribut auf die 5 Controller ergänzen (zusätzlich zu den bestehenden Filtern — kumulativ):
  - `WarehouseRequisitionsController.cs:10` — vor die Klasse:
    ```csharp
    [RequirePickingOrStockOrLagerbestellungAccess]
    [RequireLagerbestellungAktiv]
    public class WarehouseRequisitionsController : Controller
    ```
  - `Api/WarehouseRequisitionsApiController.cs:11` — nach `[Route("api/warehouserequisitions")]`:
    ```csharp
    [ApiController]
    [Route("api/warehouserequisitions")]
    [RequirePickingOrStockOrLagerbestellungAccess]
    [RequireLagerbestellungAktiv]
    public class WarehouseRequisitionsApiController : ControllerBase
    ```
  - `MissingPartsController.cs:10`:
    ```csharp
    [RequireStockOrLagerbestellungAccess]
    [RequireLagerbestellungAktiv]
    public class MissingPartsController : Controller
    ```
  - `MissingPartsLagerController.cs:10`:
    ```csharp
    [RequireLagerProcessingAccess]
    [RequireLagerbestellungAktiv]
    public class MissingPartsLagerController : Controller
    ```
  - `WarehousePickingController.cs:10`:
    ```csharp
    [RequireLagerProcessingAccess]
    [RequireLagerbestellungAktiv]
    public class WarehousePickingController : Controller
    ```
- [ ] **Layout-Gating** in `Views/Shared/_Layout.cshtml`. Neue Variable im `@{ }`-Block (nach Zeile 39 `var bestellungenAktiv = …`):
  ```csharp
                              var bestellungenAktiv = (await AppSettings.GetValueAsync("BestellungenAktiv"))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
                              var lagerbestellungAktivRaw = await AppSettings.GetValueAsync(IdealAkeWms.Models.AppSettingKeys.LagerbestellungAktiv);
                              var lagerbestellungAktiv = !string.Equals(lagerbestellungAktivRaw, "false", StringComparison.OrdinalIgnoreCase);
  ```
  Den „Bestellungen"-Dropdown-Block (aktuell Zeile 129-150) so umbauen, dass (a) der Dropdown erscheint wenn EINES der beiden Module aktiv ist, (b) „Bedarfsmeldungen" an `bestellungenAktiv` bleibt, (c) die 4 Lagerbestellungs-Einträge an `lagerbestellungAktiv` hängen. **Grund:** der Dropdown ist heute komplett an `bestellungenAktiv` gekoppelt — mit Default `BestellungenAktiv=false`/`LagerbestellungAktiv=true` würde er sonst ganz verschwinden (Regression). Neuer Block:
  ```html
                          @if ((bestellungenAktiv || lagerbestellungAktiv) && (canPick || canAccessStock || canAccessLagerbestellung || canAccessGlasbestellung))
                          {
                              <li class="nav-item dropdown">
                                  <a class="nav-link dropdown-toggle @((ViewContext.RouteData.Values["controller"]?.ToString() is "PartRequisitions" or "WarehouseRequisitions" or "WarehousePicking" or "MissingParts" or "MissingPartsLager") ? "active" : "")" href="#" role="button" data-bs-toggle="dropdown" aria-expanded="false">
                                      Bestellungen
                                  </a>
                                  <ul class="dropdown-menu">
                                      @if (bestellungenAktiv && (canPick || canAccessStock))
                                      {
                                          <li><a class="dropdown-item" asp-controller="PartRequisitions" asp-action="Index">Bedarfsmeldungen</a></li>
                                      }
                                      @if (lagerbestellungAktiv)
                                      {
                                          <li><a class="dropdown-item" asp-controller="WarehouseRequisitions" asp-action="Index">Lagerbestellungen</a></li>
                                          <li><a class="dropdown-item" asp-controller="MissingParts" asp-action="Index">Meine Fehlteile</a></li>
                                          @if (canProcessLager)
                                          {
                                              <li><hr class="dropdown-divider" style="border-color: rgba(255,255,255,0.2);" /></li>
                                              <li><a class="dropdown-item" asp-controller="WarehousePicking" asp-action="Index">Lager: Eingehende Listen</a></li>
                                              <li><a class="dropdown-item" asp-controller="MissingPartsLager" asp-action="Index">Lager: Fehlteile</a></li>
                                          }
                                      }
                                  </ul>
                              </li>
                          }
  ```
- [ ] **GREEN** — Filter-Tests laufen:
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --filter "FullyQualifiedName~RequireLagerbestellungAktivFilterTests" --nologo
  ```
  Erwartet: `Passed! … Passed: 4`.
- [ ] Voller Build (Layout kompiliert mit):
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet build IdealAkeWms.slnx
  ```
  Erwartet: `Build succeeded.` `0 Error(s)`.
- [ ] Commit:
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add -A && git commit -m "feat(filter): RequireLagerbestellungAktiv gate + class-level auf 5 Controller + Layout-Gating" -m "MVC -> Redirect Home + WarningMessage, API -> 404. 4 Bestellungen-Menueeintraege hinter LagerbestellungAktiv; Dropdown erscheint wenn Bedarfsmeldungen ODER Lagerbestellung aktiv. Filter-Tests (aktiv/fehlend/false-MVC/false-API)." -m "Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
  ```

---

## Task 3: Repo `GetOpenDraftForUserAndTypeAsync(userId, type)` + InMemory-Test

**Files:**
- `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs:9` (Interface)
- `IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs:29` (Impl, nach `CreateDraftAsync`)
- `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs` (neue Tests)

**Steps:**

- [ ] **RED** — Test in `WarehouseRequisitionRepositoryTests.cs` ergänzen (Muster: `TestDbContextFactory.Create()`). Falls die Klasse einen `Setup()`/Helper hat, diesen nutzen; ansonsten inline:
  ```csharp
      [Fact]
      public async Task GetOpenDraftForUserAndType_FindetOffenenDraftJeTyp()
      {
          var ctx = TestDbContextFactory.Create();
          var repo = new WarehouseRequisitionRepository(ctx);
          var wp = new ProductionWorkplace { Name = "WB1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
          ctx.ProductionWorkplaces.Add(wp);
          ctx.SaveChanges();

          var lagerId = await repo.CreateDraftAsync(wp.Id, WarehouseRequisitionType.Lager, 7, "u", "w");
          var glasId  = await repo.CreateDraftAsync(wp.Id, WarehouseRequisitionType.Glas, 7, "u", "w");

          var foundLager = await repo.GetOpenDraftForUserAndTypeAsync(7, WarehouseRequisitionType.Lager);
          var foundGlas  = await repo.GetOpenDraftForUserAndTypeAsync(7, WarehouseRequisitionType.Glas);

          foundLager!.Id.Should().Be(lagerId);
          foundGlas!.Id.Should().Be(glasId);
      }

      [Fact]
      public async Task GetOpenDraftForUserAndType_KeinDraft_Null()
      {
          var ctx = TestDbContextFactory.Create();
          var repo = new WarehouseRequisitionRepository(ctx);
          var found = await repo.GetOpenDraftForUserAndTypeAsync(7, WarehouseRequisitionType.Lager);
          found.Should().BeNull();
      }

      [Fact]
      public async Task GetOpenDraftForUserAndType_IgnoriertFremdeUndNichtDraft()
      {
          var ctx = TestDbContextFactory.Create();
          var repo = new WarehouseRequisitionRepository(ctx);
          var wp = new ProductionWorkplace { Name = "WB1", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t" };
          ctx.ProductionWorkplaces.Add(wp);
          ctx.SaveChanges();

          // fremder Draft (anderer User)
          await repo.CreateDraftAsync(wp.Id, WarehouseRequisitionType.Lager, 999, "u", "w");
          // eigener, aber abgeschickt
          var submitted = new WarehouseRequisition
          {
              ProductionWorkplaceId = wp.Id, Type = WarehouseRequisitionType.Lager,
              Status = WarehouseRequisitionStatus.Submitted, CreatedByUserId = 7,
              CreatedAt = DateTime.Now, CreatedBy = "u", CreatedByWindows = "w"
          };
          ctx.WarehouseRequisitions.Add(submitted);
          ctx.SaveChanges();

          var found = await repo.GetOpenDraftForUserAndTypeAsync(7, WarehouseRequisitionType.Lager);
          found.Should().BeNull("weder fremde noch abgeschickte Bestellungen zaehlen");
      }
  ```
  (Falls `using FluentAssertions;`, `using IdealAkeWms.Models;`, `using IdealAkeWms.Data.Repositories;`, `using IdealAkeWms.Tests.Helpers;` in der Datei fehlen: ergänzen.)
- [ ] ROT:
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --filter "FullyQualifiedName~WarehouseRequisitionRepositoryTests" --nologo
  ```
  Erwartet: Compile-Fehler (`GetOpenDraftForUserAndTypeAsync` nicht in `IWarehouseRequisitionRepository`).
- [ ] **GREEN** — Interface-Methode ergänzen (`IWarehouseRequisitionRepository.cs`, nach `CreateDraftAsync`, Zeile 8/9):
  ```csharp
      Task<int> CreateDraftAsync(int productionWorkplaceId, WarehouseRequisitionType type, int currentUserId, string currentUserName, string windowsUserName);

      /// <summary>
      /// Liefert den offenen (Status==Draft) Entwurf des Users fuer den gegebenen Typ
      /// (neuester zuerst) oder null. Fuer die BOM-Quick-Add-Wiederverwendung (v1.25.0).
      /// </summary>
      Task<WarehouseRequisition?> GetOpenDraftForUserAndTypeAsync(int userId, WarehouseRequisitionType type);
  ```
- [ ] Impl ergänzen (`WarehouseRequisitionRepository.cs`, direkt nach `CreateDraftAsync`, Zeile 28):
  ```csharp
      public async Task<WarehouseRequisition?> GetOpenDraftForUserAndTypeAsync(int userId, WarehouseRequisitionType type)
      {
          return await _context.WarehouseRequisitions
              .Where(r => r.Status == WarehouseRequisitionStatus.Draft
                  && r.Type == type
                  && r.CreatedByUserId == userId)
              .OrderByDescending(r => r.CreatedAt)
              .FirstOrDefaultAsync();
      }
  ```
- [ ] GRÜN:
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --filter "FullyQualifiedName~WarehouseRequisitionRepositoryTests" --nologo
  ```
  Erwartet: `Passed!` (die 3 neuen Tests + bestehende grün).
- [ ] Commit:
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add -A && git commit -m "feat(repo): GetOpenDraftForUserAndTypeAsync fuer BOM-Quick-Add-Draft-Wiederverwendung" -m "Findet offenen Draft je User+Typ (neuester zuerst); ignoriert fremde/abgeschickte. InMemory-Tests." -m "Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
  ```

---

## Task 4: Quick-Add-API `POST /api/warehouserequisitions/quick-add` (TDD)

**Files:**
- `IdealAkeWms/Controllers/Api/WarehouseRequisitionsApiController.cs` (DI erweitern + neue Action + Records)
- `IdealAkeWms.Tests/Controllers/WarehouseRequisitionsApiControllerTests.cs` (Setup erweitern + Quick-Add-Tests)

**DI-Änderung:** Der Controller braucht `IProductionWorkplaceRepository` (Werkbank-Auflösung). Konstruktor + Feld ergänzen.

**Workplace-Auflösung (Design-Entscheid, dokumentiert):** Quick-Add hat keinen Dialog zur Werkbank-Wahl (Spec „Werkbank = User-Default, FA-unabhängig"). Es gibt seit v1.23.0 keinen Single-FK-Default mehr (`DefaultWorkplaceId` → `DefaultWorkbenches` komma-String). Deshalb: **erste zugeordnete Werkbank** des Users via `IProductionWorkplaceRepository.GetByUserIdAsync(userId)` (ist nach `Name` sortiert). Keine Werkbank (Count 0) → gesamter Request `BadRequest("Bitte Standard-Werkbank im Profil hinterlegen.")` (Spec-Wortlaut). Auflösung erfolgt **lazy** — nur wenn tatsächlich ein neuer Draft angelegt werden muss (existierender offener Draft braucht keine Werkbank).

**Steps:**

- [ ] **RED** — Setup() im Test-File erweitern (neuer Konstruktor-Param) + Werkbank-Seed-Helper + Quick-Add-Tests. `WarehouseRequisitionsApiControllerTests.cs`:
  - `Setup()` (Zeile 16-36) austauschen, sodass die Werkbank-Repo injiziert + der User-Rechte-Mock (Lager+Glas true als Default) gesetzt wird:
    ```csharp
      private static (WarehouseRequisitionsApiController ctrl, ApplicationDbContext ctx, Mock<ICurrentUserService> user) Setup(
          bool canOrderLager = true, bool canOrderGlas = true)
      {
          var ctx = TestDbContextFactory.Create();

          var currentUser = new Mock<ICurrentUserService>();
          currentUser.Setup(s => s.GetCurrentAppUserId()).Returns(1);
          currentUser.Setup(s => s.GetDisplayName()).Returns("tester");
          currentUser.Setup(s => s.GetWindowsUserName()).Returns("DOMAIN\\tester");
          currentUser.Setup(s => s.CanOrderLagerAsync()).ReturnsAsync(canOrderLager);
          currentUser.Setup(s => s.CanOrderGlasAsync()).ReturnsAsync(canOrderGlas);

          var settings = new Mock<IAppSettingRepository>();
          settings.Setup(s => s.GetValueAsync(AppSettingKeys.GlasArtikelgruppen)).ReturnsAsync("GLAS");
          settings.Setup(s => s.GetValueAsync(AppSettingKeys.GemeinsameArtikelgruppen)).ReturnsAsync("EUZ");

          var repo = new WarehouseRequisitionRepository(ctx);
          var articles = new ArticleRepository(ctx);
          var stock = new Mock<IStockMovementRepository>();
          var workplaces = new ProductionWorkplaceRepository(ctx);

          var ctrl = new WarehouseRequisitionsApiController(
              repo, articles, stock.Object, currentUser.Object, settings.Object, workplaces);
          return (ctrl, ctx, currentUser);
      }
    ```
  - **Wichtig:** die bestehenden AddItem/UpdateItem/RemoveItem-Tests rufen `Setup()` als 2-Tupel-Destrukturierung auf (`var (ctrl, ctx) = Setup();`). Da `Setup()` jetzt ein 3-Tupel liefert, diese Aufrufe auf `var (ctrl, ctx, _) = Setup();` umstellen (alle bestehenden Setup()-Aufrufe im File). Alternativ eine 2-Tupel-Overload `Setup()` behalten, die das 3-Tupel weiterreicht — sauberer ist die `_`-Destrukturierung.
  - Werkbank-Seed-Helper ergänzen (User 1 an eine Werkbank hängen, sonst blockt die Werkbank-Auflösung):
    ```csharp
      private static void SeedUserWorkplace(ApplicationDbContext ctx, int userId = SetupUserId)
      {
          var wp = new ProductionWorkplace
          {
              Name = "WB-Default", CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
          };
          ctx.ProductionWorkplaces.Add(wp);
          ctx.SaveChanges();
          ctx.ProductionWorkplaceUsers.Add(new ProductionWorkplaceUser
          {
              ProductionWorkplaceId = wp.Id, UserId = userId,
              CreatedAt = DateTime.Now, CreatedBy = "t", CreatedByWindows = "t"
          });
          ctx.SaveChanges();
      }
    ```
  - Quick-Add-Tests:
    ```csharp
      [Fact]
      public async Task QuickAdd_EinLagerArtikel_NeuerLagerDraftPlusItem()
      {
          var (ctrl, ctx, _) = Setup();
          SeedArticles(ctx);
          SeedUserWorkplace(ctx);

          var result = await ctrl.QuickAdd(new WarehouseRequisitionsApiController.QuickAddRequest(
              new List<WarehouseRequisitionsApiController.QuickAddItem>
              {
                  new("ART-940", 3)
              }));

          var ok = result.Should().BeOfType<OkObjectResult>().Subject;
          var resp = (WarehouseRequisitionsApiController.QuickAddResponse)ok.Value!;
          resp.AddedLager.Should().Be(1);
          resp.AddedGlas.Should().Be(0);
          resp.LagerRequisitionId.Should().NotBeNull();
          resp.GlasRequisitionId.Should().BeNull();
          ctx.WarehouseRequisitions.Should().ContainSingle(r => r.Type == WarehouseRequisitionType.Lager && r.Status == WarehouseRequisitionStatus.Draft);
          ctx.WarehouseRequisitionItems.Should().ContainSingle(i => i.ArticleNumber == "ART-940" && i.QuantityRequested == 3);
      }

      [Fact]
      public async Task QuickAdd_EinGlasArtikel_NeuerGlasDraft()
      {
          var (ctrl, ctx, _) = Setup();
          SeedArticles(ctx);
          SeedUserWorkplace(ctx);

          var result = await ctrl.QuickAdd(new WarehouseRequisitionsApiController.QuickAddRequest(
              new List<WarehouseRequisitionsApiController.QuickAddItem> { new("ART-GLAS", 2) }));

          var resp = (WarehouseRequisitionsApiController.QuickAddResponse)result.Should().BeOfType<OkObjectResult>().Subject.Value!;
          resp.AddedGlas.Should().Be(1);
          resp.GlasRequisitionId.Should().NotBeNull();
          resp.LagerRequisitionId.Should().BeNull();
          ctx.WarehouseRequisitions.Should().ContainSingle(r => r.Type == WarehouseRequisitionType.Glas);
      }

      [Fact]
      public async Task QuickAdd_ZweiLagerArtikel_SelberDraft()
      {
          var (ctrl, ctx, _) = Setup();
          SeedArticles(ctx);
          SeedUserWorkplace(ctx);

          var result = await ctrl.QuickAdd(new WarehouseRequisitionsApiController.QuickAddRequest(
              new List<WarehouseRequisitionsApiController.QuickAddItem> { new("ART-940", 1), new("ART-EUZ", 2) }));

          var resp = (WarehouseRequisitionsApiController.QuickAddResponse)result.Should().BeOfType<OkObjectResult>().Subject.Value!;
          resp.AddedLager.Should().Be(2);
          ctx.WarehouseRequisitions.Count(r => r.Type == WarehouseRequisitionType.Lager).Should().Be(1, "beide Lager-Items in EINEN Draft");
          ctx.WarehouseRequisitionItems.Count().Should().Be(2);
      }

      [Fact]
      public async Task QuickAdd_VorhandenerOffenerDraft_Wiederverwendet()
      {
          var (ctrl, ctx, _) = Setup();
          SeedArticles(ctx);
          SeedUserWorkplace(ctx);
          var existing = SeedDraft(ctx, WarehouseRequisitionType.Lager);

          var result = await ctrl.QuickAdd(new WarehouseRequisitionsApiController.QuickAddRequest(
              new List<WarehouseRequisitionsApiController.QuickAddItem> { new("ART-940", 1) }));

          var resp = (WarehouseRequisitionsApiController.QuickAddResponse)result.Should().BeOfType<OkObjectResult>().Subject.Value!;
          resp.LagerRequisitionId.Should().Be(existing, "offener Draft wird wiederverwendet, kein neuer");
          ctx.WarehouseRequisitions.Count(r => r.Type == WarehouseRequisitionType.Lager).Should().Be(1);
      }

      [Fact]
      public async Task QuickAdd_GemischteListe_ZweiDrafts()
      {
          var (ctrl, ctx, _) = Setup();
          SeedArticles(ctx);
          SeedUserWorkplace(ctx);

          var result = await ctrl.QuickAdd(new WarehouseRequisitionsApiController.QuickAddRequest(
              new List<WarehouseRequisitionsApiController.QuickAddItem> { new("ART-940", 1), new("ART-GLAS", 1) }));

          var resp = (WarehouseRequisitionsApiController.QuickAddResponse)result.Should().BeOfType<OkObjectResult>().Subject.Value!;
          resp.AddedLager.Should().Be(1);
          resp.AddedGlas.Should().Be(1);
          resp.LagerRequisitionId.Should().NotBeNull();
          resp.GlasRequisitionId.Should().NotBeNull();
          ctx.WarehouseRequisitions.Count(r => r.Type == WarehouseRequisitionType.Lager).Should().Be(1);
          ctx.WarehouseRequisitions.Count(r => r.Type == WarehouseRequisitionType.Glas).Should().Be(1);
      }

      [Fact]
      public async Task QuickAdd_FehlendesGlasRecht_Skipped()
      {
          var (ctrl, ctx, _) = Setup(canOrderLager: true, canOrderGlas: false);
          SeedArticles(ctx);
          SeedUserWorkplace(ctx);

          var result = await ctrl.QuickAdd(new WarehouseRequisitionsApiController.QuickAddRequest(
              new List<WarehouseRequisitionsApiController.QuickAddItem> { new("ART-GLAS", 1) }));

          // alles skipped -> BadRequest
          var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
          var resp = (WarehouseRequisitionsApiController.QuickAddResponse)bad.Value!;
          resp.Skipped.Should().ContainSingle(s => s.ArticleNumber == "ART-GLAS");
          ctx.WarehouseRequisitions.Should().BeEmpty();
      }

      [Fact]
      public async Task QuickAdd_ArtikelNichtGefunden_Skipped()
      {
          var (ctrl, ctx, _) = Setup();
          SeedArticles(ctx);
          SeedUserWorkplace(ctx);

          var result = await ctrl.QuickAdd(new WarehouseRequisitionsApiController.QuickAddRequest(
              new List<WarehouseRequisitionsApiController.QuickAddItem> { new("ART-940", 1), new("UNBEKANNT", 1) }));

          var resp = (WarehouseRequisitionsApiController.QuickAddResponse)result.Should().BeOfType<OkObjectResult>().Subject.Value!;
          resp.AddedLager.Should().Be(1);
          resp.Skipped.Should().ContainSingle(s => s.ArticleNumber == "UNBEKANNT");
      }

      [Fact]
      public async Task QuickAdd_MengeNullOderNegativ_Skipped()
      {
          var (ctrl, ctx, _) = Setup();
          SeedArticles(ctx);
          SeedUserWorkplace(ctx);

          var result = await ctrl.QuickAdd(new WarehouseRequisitionsApiController.QuickAddRequest(
              new List<WarehouseRequisitionsApiController.QuickAddItem> { new("ART-940", 0) }));

          var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
          var resp = (WarehouseRequisitionsApiController.QuickAddResponse)bad.Value!;
          resp.Skipped.Should().ContainSingle(s => s.ArticleNumber == "ART-940");
          ctx.WarehouseRequisitionItems.Should().BeEmpty();
      }

      [Fact]
      public async Task QuickAdd_KeineWerkbank_BadRequest()
      {
          var (ctrl, ctx, _) = Setup();
          SeedArticles(ctx);
          // KEIN SeedUserWorkplace -> Werkbank nicht aufloesbar

          var result = await ctrl.QuickAdd(new WarehouseRequisitionsApiController.QuickAddRequest(
              new List<WarehouseRequisitionsApiController.QuickAddItem> { new("ART-940", 1) }));

          result.Should().BeOfType<BadRequestObjectResult>();
          ctx.WarehouseRequisitions.Should().BeEmpty();
      }
    ```
- [ ] ROT:
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --filter "FullyQualifiedName~WarehouseRequisitionsApiControllerTests" --nologo
  ```
  Erwartet: Compile-Fehler (Konstruktor-Arität, `QuickAdd`/`QuickAddRequest`/`QuickAddResponse` fehlen).
- [ ] **GREEN** — Controller-Änderung `Api/WarehouseRequisitionsApiController.cs`:
  - Feld + Konstruktor erweitern (nach `_settings`):
    ```csharp
      private readonly IWarehouseRequisitionRepository _repo;
      private readonly IArticleRepository _articles;
      private readonly IStockMovementRepository _stock;
      private readonly ICurrentUserService _user;
      private readonly IAppSettingRepository _settings;
      private readonly IProductionWorkplaceRepository _workplaces;

      public WarehouseRequisitionsApiController(
          IWarehouseRequisitionRepository repo, IArticleRepository articles,
          IStockMovementRepository stock, ICurrentUserService user,
          IAppSettingRepository settings, IProductionWorkplaceRepository workplaces)
      {
          _repo = repo;
          _articles = articles;
          _stock = stock;
          _user = user;
          _settings = settings;
          _workplaces = workplaces;
      }
    ```
  - Records (bei den anderen `record`-Deklarationen, nach `UpdateItemRequest`):
    ```csharp
      public record QuickAddItem(string ArticleNumber, decimal Quantity);
      public record QuickAddRequest(List<QuickAddItem> Items);
      public record QuickAddSkipped(string ArticleNumber, string Reason);
      public record QuickAddResponse(int? LagerRequisitionId, int? GlasRequisitionId,
          int AddedLager, int AddedGlas, List<QuickAddSkipped> Skipped);
    ```
  - Neue Action (z. B. nach `AddItem`, vor `UpdateItem`):
    ```csharp
      /// <summary>
      /// BOM-Quick-Add (v1.25.0): EIN Endpunkt fuer Einzel (1 Item) UND Bulk.
      /// Typ automatisch aus der Artikelgruppe (Glas-Gruppe -> Glas, sonst -> Lager;
      /// gemeinsame/EUZ -> Lager). Rechte je Typ. Pro Typ genau EIN Draft je Lauf
      /// (offener Draft wiederverwendet, sonst neu mit User-Default-Werkbank).
      /// Ungueltige Items landen in skipped (kein Abbruch); alles skipped -> BadRequest.
      /// </summary>
      [HttpPost("quick-add")]
      public async Task<IActionResult> QuickAdd([FromBody] QuickAddRequest body)
      {
          var items = body?.Items ?? new List<QuickAddItem>();
          if (items.Count == 0)
              return BadRequest(new { error = "Keine Positionen uebergeben." });

          var userId = _user.GetCurrentAppUserId() ?? 0;
          var displayName = _user.GetDisplayName();
          var winName = _user.GetWindowsUserName();

          var glasGroups = GlasArticleGroupFilter.ParseGroups(
              await _settings.GetValueAsync(AppSettingKeys.GlasArtikelgruppen));

          var canOrderLager = await _user.CanOrderLagerAsync();
          var canOrderGlas = await _user.CanOrderGlasAsync();

          int? lagerReqId = null, glasReqId = null;
          int addedLager = 0, addedGlas = 0;
          var skipped = new List<QuickAddSkipped>();

          // Werkbank (erste zugeordnete) nur bei Bedarf und nur einmal aufloesen.
          int? resolvedWorkplaceId = null;
          bool workplaceResolved = false;
          async Task<int?> ResolveWorkplaceAsync()
          {
              if (workplaceResolved) return resolvedWorkplaceId;
              workplaceResolved = true;
              var wps = await _workplaces.GetByUserIdAsync(userId);
              resolvedWorkplaceId = wps.Count > 0 ? wps[0].Id : (int?)null;
              return resolvedWorkplaceId;
          }

          foreach (var item in items)
          {
              var articleNumber = item.ArticleNumber?.Trim() ?? string.Empty;

              if (item.Quantity <= 0)
              {
                  skipped.Add(new QuickAddSkipped(articleNumber, "Menge muss groesser 0 sein."));
                  continue;
              }

              var article = await _articles.GetByArticleNumberAsync(articleNumber);
              if (article == null)
              {
                  skipped.Add(new QuickAddSkipped(articleNumber, "Artikel nicht gefunden."));
                  continue;
              }

              // Typ automatisch: reine Glas-Gruppe -> Glas, sonst Lager (gemeinsame/EUZ -> Lager).
              var norm = GlasArticleGroupFilter.NormalizeGroup(article.ArticleGroup);
              var type = glasGroups.Contains(norm)
                  ? WarehouseRequisitionType.Glas
                  : WarehouseRequisitionType.Lager;

              if (type == WarehouseRequisitionType.Glas && !canOrderGlas)
              {
                  skipped.Add(new QuickAddSkipped(articleNumber, "Keine Glasbestell-Berechtigung."));
                  continue;
              }
              if (type == WarehouseRequisitionType.Lager && !canOrderLager)
              {
                  skipped.Add(new QuickAddSkipped(articleNumber, "Keine Lagerbestell-Berechtigung."));
                  continue;
              }

              // Draft je Typ lazy.
              int reqId;
              if (type == WarehouseRequisitionType.Glas)
              {
                  if (glasReqId == null)
                  {
                      var existing = await _repo.GetOpenDraftForUserAndTypeAsync(userId, type);
                      if (existing != null)
                      {
                          glasReqId = existing.Id;
                      }
                      else
                      {
                          var wpId = await ResolveWorkplaceAsync();
                          if (wpId == null)
                              return BadRequest(new { error = "Bitte Standard-Werkbank im Profil hinterlegen." });
                          glasReqId = await _repo.CreateDraftAsync(wpId.Value, type, userId, displayName, winName);
                      }
                  }
                  reqId = glasReqId.Value;
              }
              else
              {
                  if (lagerReqId == null)
                  {
                      var existing = await _repo.GetOpenDraftForUserAndTypeAsync(userId, type);
                      if (existing != null)
                      {
                          lagerReqId = existing.Id;
                      }
                      else
                      {
                          var wpId = await ResolveWorkplaceAsync();
                          if (wpId == null)
                              return BadRequest(new { error = "Bitte Standard-Werkbank im Profil hinterlegen." });
                          lagerReqId = await _repo.CreateDraftAsync(wpId.Value, type, userId, displayName, winName);
                      }
                  }
                  reqId = lagerReqId.Value;
              }

              try
              {
                  await _repo.AddItemAsync(reqId, articleNumber, article.Description ?? string.Empty,
                      article.Unit, item.Quantity, displayName, winName);
                  if (type == WarehouseRequisitionType.Glas) addedGlas++; else addedLager++;
              }
              catch (InvalidOperationException ex)
              {
                  // z. B. Artikel bereits in dieser Bestellung.
                  skipped.Add(new QuickAddSkipped(articleNumber, ex.Message));
              }
          }

          var response = new QuickAddResponse(lagerReqId, glasReqId, addedLager, addedGlas, skipped);
          if (addedLager == 0 && addedGlas == 0)
              return BadRequest(response);
          return Ok(response);
      }
    ```
  - **Hinweis:** `GlasArticleGroupFilter` liegt im Namespace `IdealAkeWms.Services` (bereits via `using IdealAkeWms.Services;` im File vorhanden). `IProductionWorkplaceRepository`, `ProductionWorkplaceUser` liegen in `IdealAkeWms.Data.Repositories` / `IdealAkeWms.Models` (Usings bereits vorhanden).
- [ ] GRÜN:
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --filter "FullyQualifiedName~WarehouseRequisitionsApiControllerTests" --nologo
  ```
  Erwartet: `Passed!` — alle bestehenden AddItem/UpdateItem/RemoveItem-Tests + die 9 neuen Quick-Add-Tests grün.
- [ ] Voller Build + volle Web-Tests (DI-Änderung wirkt sich auf `Program.cs`-Registrierung aus — `IProductionWorkplaceRepository` ist bereits registriert; nur prüfen):
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet build IdealAkeWms.slnx && dotnet test IdealAkeWms.Tests --nologo
  ```
  Erwartet: `Build succeeded.` + `Passed!`.
- [ ] Commit:
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add -A && git commit -m "feat(api): POST /api/warehouserequisitions/quick-add (Einzel+Bulk, Auto-Typ, Draft-Wiederverwendung)" -m "Typ automatisch aus Artikelgruppe; Rechte je Typ; pro Typ genau EIN Draft je Lauf; skipped-Sammlung; alles skipped -> BadRequest. Werkbank = erste User-Werkbank (Count 0 -> BadRequest). 9 Controller-Tests." -m "Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
  ```

---

## Task 5: ViewBags `LagerbestellungAktiv` + `CanOrderWarehouse` in beiden Bom-Controllern

**Files:**
- `IdealAkeWms/Controllers/PickingController.cs:373-381` (Ende der `Bom`-Action, nach `ViewBag.OpenRequisitions`)
- `IdealAkeWms/Controllers/FaWorklistController.cs:273-277` (Ende der `Bom`-Action, vor `return View(...)`)

**Steps:**

- [ ] `PickingController.Bom` — nach `ViewBag.OpenRequisitions = openRequisitions;` (Zeile 379) und vor `return View(vm);`:
  ```csharp
          ViewBag.OpenRequisitions = openRequisitions;

          // Lagerbestellung-Button (v1.25.0): Master-Schalter (Default true: nur "false" sperrt)
          // + Order-Recht (Lager ODER Glas). Unabhaengig von Bedarfsmeldungen (BestellungenAktiv).
          var lagerbestellungAktivRaw = await _settingRepository.GetValueAsync(AppSettingKeys.LagerbestellungAktiv);
          ViewBag.LagerbestellungAktiv = !string.Equals(lagerbestellungAktivRaw, "false", StringComparison.OrdinalIgnoreCase);
          ViewBag.CanOrderWarehouse = await _currentUserService.CanOrderLagerAsync()
              || await _currentUserService.CanOrderGlasAsync();

          return View(vm);
  ```
- [ ] `FaWorklistController.Bom` — nach dem `vm`-Build und vor `return View("~/Views/Picking/Bom.cshtml", vm);` (Zeile 273-277):
  ```csharp
          var vm = await _readOnlyBomBuilder.BuildAsync(id, filterText, _currentUser.GetCurrentAppUserId());
          if (vm == null)
              return NotFound();

          // Lagerbestellung-Button auch im Read-only-Vorbau (v1.25.0), unabhaengig von ReadOnly.
          var lagerbestellungAktivRaw = await _settingRepository.GetValueAsync(AppSettingKeys.LagerbestellungAktiv);
          ViewBag.LagerbestellungAktiv = !string.Equals(lagerbestellungAktivRaw, "false", StringComparison.OrdinalIgnoreCase);
          ViewBag.CanOrderWarehouse = await _currentUser.CanOrderLagerAsync()
              || await _currentUser.CanOrderGlasAsync();

          return View("~/Views/Picking/Bom.cshtml", vm);
  ```
- [ ] Build:
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet build IdealAkeWms.slnx
  ```
  Erwartet: `Build succeeded.` `0 Error(s)`.
- [ ] Commit:
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add -A && git commit -m "feat(bom): ViewBag.LagerbestellungAktiv + CanOrderWarehouse in Picking.Bom + FaWorklist.Bom" -m "CanOrderWarehouse = CanOrderLagerAsync || CanOrderGlasAsync. Default-true-Read (nur false sperrt). Vorbereitung fuer den BOM-Button." -m "Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
  ```

---

## Task 6: `Views/Picking/Bom.cshtml` — Einzel-Button + Bulk + Checkboxen + 2 Modals + JS

**File:** `IdealAkeWms/Views/Picking/Bom.cshtml`

**Sichtbarkeit:** Alle neuen Elemente hinter `@if (ViewBag.LagerbestellungAktiv == true && ViewBag.CanOrderWarehouse == true)` — **unabhängig von `Model.ReadOnly`** (also auch im Vorbau-Read-only). Die bestehende Bedarfsmeldung-Spalte/-Modal (`BestellungenAktiv && !ReadOnly`) bleibt unberührt.

**Wichtige Muster-Entscheide (dokumentiert):**
- **Eigene Auswahl-Checkboxen** `.warehouse-select` (nicht `.picking-checkbox` wiederverwenden): die Picking-Checkboxen existieren nur in `!ReadOnly` UND nur für pickbare Items — der Warehouse-Bulk muss aber in BEIDEN Modi und pro Nicht-Baugruppen-Zeile funktionieren. Daher eine dedizierte Checkbox in der neuen Spalte. Kein `name`-Attribut (kein Hidden+Checkbox-Konflikt — reine JS-Auswahl).
- **Neue Spalte** `warehouse-order` (nicht in die bestehende `order`-Spalte mischen — die ist an `BestellungenAktiv && !ReadOnly` gekoppelt).
- **`data-quantity` invariant** (`@item.Menge.ToString(System.Globalization.CultureInfo.InvariantCulture)`), damit `parseFloat` im JS zuverlässig ist (Culture-Fallstrick).
- **JS läuft vor `if (readOnly) return;`** (Zeile 838), damit es auch im Read-only-Modus greift. `token` (Zeile 609) + `@Html.AntiForgeryToken()` (Zeile 81) sind dort verfügbar.

**Steps (alle im selben File, dann EIN Build):**

- [ ] **Header-`<th>`** — nach dem `order`-Header-Block (aktuell Zeile 105-108) einfügen:
  ```html
                  @if (ViewBag.BestellungenAktiv == true && !Model.ReadOnly)
                  {
                      <th style="width: 80px;" data-col-key="order">Bestellen</th>
                  }
                  @if (ViewBag.LagerbestellungAktiv == true && ViewBag.CanOrderWarehouse == true)
                  {
                      <th style="width: 90px;" data-col-key="warehouse-order">Lagerbestellung</th>
                  }
  ```
- [ ] **Zeilen-`<td>`** — nach dem `order`-`<td>`-Block (aktuell Zeile 212-243) einfügen. Für Baugruppen-Zeilen bleibt die Zelle leer:
  ```html
                      @if (ViewBag.LagerbestellungAktiv == true && ViewBag.CanOrderWarehouse == true)
                      {
                          <td class="text-center">
                              @if (!item.IsBaugruppe)
                              {
                                  <input type="checkbox" class="form-check-input warehouse-select me-1"
                                         data-article-number="@item.Ressourcenummer"
                                         data-description="@item.Bezeichnung1"
                                         data-quantity="@item.Menge.ToString(System.Globalization.CultureInfo.InvariantCulture)" />
                                  <button type="button" class="btn btn-sm btn-outline-primary order-warehouse-btn" title="Lagerbestellung"
                                          data-article-number="@item.Ressourcenummer"
                                          data-description="@item.Bezeichnung1"
                                          data-quantity="@item.Menge.ToString(System.Globalization.CultureInfo.InvariantCulture)">
                                      <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" viewBox="0 0 16 16">
                                          <path d="M0 2.5A.5.5 0 0 1 .5 2H2a.5.5 0 0 1 .485.379L2.89 4H14.5a.5.5 0 0 1 .485.621l-1.5 6A.5.5 0 0 1 13 11H4a.5.5 0 0 1-.485-.379L1.61 3H.5a.5.5 0 0 1-.5-.5M3.14 5l.5 2H5V5zM6 5v2h2V5zm3 0v2h2V5zm3 0v2h1.36l.5-2zM5 8H3.89l.5 2H5zm1 2h2V8H6zm3 0h2V8H9zm3 0h1.11l.5-2H12zm-6.5 3a1 1 0 1 0 0 2 1 1 0 0 0 0-2m7 0a1 1 0 1 0 0 2 1 1 0 0 0 0-2"/>
                                      </svg>
                                  </button>
                              }
                          </td>
                      }
  ```
- [ ] **Empty-State-Colspan** (aktuell Zeile 248-250) erweitern:
  ```csharp
                  // Spaltenzahl: 12 Basis (inkl. pick-control + source-location); ReadOnly -2;
                  // Bestellen-Spalte +1; Lagerbestellung-Spalte +1.
                  var emptyColspan = (Model.ReadOnly ? 10 : 12)
                      + (ViewBag.BestellungenAktiv == true && !Model.ReadOnly ? 1 : 0)
                      + (ViewBag.LagerbestellungAktiv == true && ViewBag.CanOrderWarehouse == true ? 1 : 0);
  ```
- [ ] **Bulk-Button** in der Filter-Card-Toolbar — nach dem `btnCollapseAll`-Button (aktuell Zeile 71-75, innerhalb `<div class="col-auto ms-auto d-flex gap-2 align-items-center">`, direkt vor dem schließenden `</div>` bei Zeile 76):
  ```html
                  @if (ViewBag.LagerbestellungAktiv == true && ViewBag.CanOrderWarehouse == true)
                  {
                      <button type="button" id="btnBulkWarehouseOrder" class="btn btn-sm btn-primary" style="display:none;" title="Ausgewaehlte als Lagerbestellung">
                          <svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" viewBox="0 0 16 16" class="me-1">
                              <path d="M0 2.5A.5.5 0 0 1 .5 2H2a.5.5 0 0 1 .485.379L2.89 4H14.5a.5.5 0 0 1 .485.621l-1.5 6A.5.5 0 0 1 13 11H4a.5.5 0 0 1-.485-.379L1.61 3H.5a.5.5 0 0 1-.5-.5M5.5 13a1 1 0 1 0 0 2 1 1 0 0 0 0-2m7 0a1 1 0 1 0 0 2 1 1 0 0 0 0-2"/>
                          </svg>
                          Lagerbestellung (Auswahl)
                      </button>
                  }
  ```
- [ ] **column-config JSON** — nach dem `order`-Append (aktuell Zeile 512) einen Append für die neue Spalte:
  ```csharp
      @if (!Model.ReadOnly) { <text>,{ "key": "source-location", "label": "Quell-Lagerplatz", "locked": false, "defaultWidth": null }</text> }
      @if (ViewBag.BestellungenAktiv == true && !Model.ReadOnly) { <text>,{ "key": "order", "label": "Bestellen", "locked": false, "defaultWidth": 80 }</text> }
      @if (ViewBag.LagerbestellungAktiv == true && ViewBag.CanOrderWarehouse == true) { <text>,{ "key": "warehouse-order", "label": "Lagerbestellung", "locked": false, "defaultWidth": 90 }</text> }
  ```
- [ ] **Zwei Modals** — als eigener Block, außerhalb des `!Model.ReadOnly`-Blocks (damit auch im Read-only sichtbar). Direkt VOR `<div class="mt-3">` (aktuell Zeile 484, der Zurück-Button-Block) einfügen:
  ```html
  @if (ViewBag.LagerbestellungAktiv == true && ViewBag.CanOrderWarehouse == true)
  {
      <!-- Lagerbestellung Einzel-Modal (v1.25.0) -->
      <div class="modal fade" id="warehouseOrderModal" tabindex="-1">
          <div class="modal-dialog modal-dialog-centered">
              <div class="modal-content">
                  <div class="modal-header" style="background-color: var(--ake-secondary); color: white;">
                      <h5 class="modal-title">Lagerbestellung erstellen</h5>
                      <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                  </div>
                  <div class="modal-body">
                      <p class="mb-1"><strong id="whSingleArticle"></strong></p>
                      <p class="text-muted small mb-3" id="whSingleDesc"></p>
                      <label class="form-label fw-bold" for="whSingleQty">Menge</label>
                      <input type="number" id="whSingleQty" class="form-control" min="0.001" step="any" />
                      <input type="hidden" id="whSingleArticleNumber" />
                  </div>
                  <div class="modal-footer">
                      <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Abbrechen</button>
                      <button type="button" class="btn btn-primary" id="btnWarehouseSingleSubmit">Zur Bestellung hinzufuegen</button>
                  </div>
              </div>
          </div>
      </div>

      <!-- Lagerbestellung Bulk-Modal (v1.25.0) -->
      <div class="modal fade" id="warehouseOrderBulkModal" tabindex="-1">
          <div class="modal-dialog modal-lg modal-dialog-centered">
              <div class="modal-content">
                  <div class="modal-header" style="background-color: var(--ake-secondary); color: white;">
                      <h5 class="modal-title">Lagerbestellung (Auswahl)</h5>
                      <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                  </div>
                  <div class="modal-body">
                      <div class="table-responsive mb-0">
                          <table class="table table-sm table-striped mb-0">
                              <thead>
                                  <tr>
                                      <th>Artikelnummer</th>
                                      <th>Bezeichnung</th>
                                      <th class="text-end" style="width: 120px;">Menge</th>
                                  </tr>
                              </thead>
                              <tbody id="warehouseBulkBody"></tbody>
                          </table>
                      </div>
                  </div>
                  <div class="modal-footer">
                      <button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">Abbrechen</button>
                      <button type="button" class="btn btn-primary" id="btnWarehouseBulkSubmit">Zur Bestellung hinzufuegen</button>
                  </div>
              </div>
          </div>
      </div>
  }
  ```
- [ ] **JS** — in `@section Scripts`, im DOMContentLoaded-Handler, direkt NACH dem Print-Handler-Block (endet bei `});` Zeile 834) und VOR dem Kommentar `// Ab hier ausschliesslich Picking-Funktionalitaet …` / `if (readOnly) return;` (Zeile 836-838) einfügen:
  ```javascript
              // ========== Lagerbestellung aus Stueckliste (v1.25.0) ==========
              // Laeuft in BEIDEN Modi (auch Read-only/Vorbau) — daher VOR `if (readOnly) return;`.
              @if (ViewBag.LagerbestellungAktiv == true && ViewBag.CanOrderWarehouse == true)
              {
                  <text>
              (function () {
                  var whModalEl = document.getElementById('warehouseOrderModal');
                  var whBulkModalEl = document.getElementById('warehouseOrderBulkModal');
                  if (!whModalEl || !whBulkModalEl) return;
                  var whModal = null, whBulkModal = null;

                  function whEscape(t) { var d = document.createElement('div'); d.textContent = t || ''; return d.innerHTML; }

                  function handleQuickAddResult(data) {
                      var skippedMsg = (data.skipped && data.skipped.length)
                          ? '\nUebersprungen:\n' + data.skipped.map(function (s) { return s.articleNumber + ': ' + s.reason; }).join('\n')
                          : '';
                      if (data.lagerRequisitionId && data.glasRequisitionId) {
                          alert('Zu Lager: ' + data.addedLager + ', zu Glas: ' + data.addedGlas + ' hinzugefuegt.' + skippedMsg);
                          window.location = '/WarehouseRequisitions';
                      } else if (data.lagerRequisitionId) {
                          if (skippedMsg) alert('Hinzugefuegt.' + skippedMsg);
                          window.location = '/WarehouseRequisitions/Edit/' + data.lagerRequisitionId;
                      } else if (data.glasRequisitionId) {
                          if (skippedMsg) alert('Hinzugefuegt.' + skippedMsg);
                          window.location = '/WarehouseRequisitions/Edit/' + data.glasRequisitionId;
                      }
                  }

                  function postQuickAdd(items, submitBtn) {
                      if (!items.length) return;
                      submitBtn.disabled = true;
                      $.ajax({
                          url: '/api/warehouserequisitions/quick-add',
                          type: 'POST',
                          contentType: 'application/json',
                          data: JSON.stringify({ items: items }),
                          headers: { 'RequestVerificationToken': token },
                          success: function (data) { handleQuickAddResult(data); },
                          error: function (xhr) {
                              submitBtn.disabled = false;
                              var r = xhr.responseJSON;
                              if (r && r.skipped && r.skipped.length) {
                                  alert('Nichts hinzugefuegt.\n' + r.skipped.map(function (s) { return s.articleNumber + ': ' + s.reason; }).join('\n'));
                              } else {
                                  alert((r && r.error) ? r.error : 'Fehler beim Erstellen der Lagerbestellung.');
                              }
                          }
                      });
                  }

                  // --- Einzel ---
                  document.querySelectorAll('.order-warehouse-btn').forEach(function (btn) {
                      btn.addEventListener('click', function (e) {
                          e.stopPropagation();
                          document.getElementById('whSingleArticle').textContent = this.getAttribute('data-article-number');
                          document.getElementById('whSingleDesc').textContent = this.getAttribute('data-description') || '';
                          document.getElementById('whSingleArticleNumber').value = this.getAttribute('data-article-number');
                          document.getElementById('whSingleQty').value = this.getAttribute('data-quantity');
                          if (!whModal) whModal = new bootstrap.Modal(whModalEl);
                          whModal.show();
                      });
                  });
                  document.getElementById('btnWarehouseSingleSubmit').addEventListener('click', function () {
                      var articleNumber = document.getElementById('whSingleArticleNumber').value;
                      var qty = parseFloat(document.getElementById('whSingleQty').value) || 0;
                      postQuickAdd([{ articleNumber: articleNumber, quantity: qty }], this);
                  });

                  // --- Bulk ---
                  var whBulkBtn = document.getElementById('btnBulkWarehouseOrder');
                  function updateWhBulkBtn() {
                      if (!whBulkBtn) return;
                      var n = document.querySelectorAll('.warehouse-select:checked').length;
                      whBulkBtn.style.display = n > 0 ? '' : 'none';
                  }
                  document.querySelectorAll('.warehouse-select').forEach(function (cb) {
                      cb.addEventListener('change', updateWhBulkBtn);
                  });
                  if (whBulkBtn) {
                      whBulkBtn.addEventListener('click', function () {
                          var body = document.getElementById('warehouseBulkBody');
                          body.innerHTML = '';
                          document.querySelectorAll('.warehouse-select:checked').forEach(function (cb) {
                              var an = cb.getAttribute('data-article-number');
                              var desc = cb.getAttribute('data-description') || '';
                              var qty = cb.getAttribute('data-quantity');
                              var tr = document.createElement('tr');
                              tr.innerHTML = '<td>' + whEscape(an) + '</td><td>' + whEscape(desc) + '</td>' +
                                  '<td class="text-end"><input type="number" class="form-control form-control-sm text-end wh-bulk-qty" ' +
                                  'data-article-number="' + whEscape(an) + '" value="' + whEscape(qty) + '" min="0.001" step="any" ' +
                                  'style="width: 100px; display:inline-block;" /></td>';
                              body.appendChild(tr);
                          });
                          if (!whBulkModal) whBulkModal = new bootstrap.Modal(whBulkModalEl);
                          whBulkModal.show();
                      });
                  }
                  document.getElementById('btnWarehouseBulkSubmit').addEventListener('click', function () {
                      var items = [];
                      document.querySelectorAll('#warehouseBulkBody .wh-bulk-qty').forEach(function (inp) {
                          items.push({
                              articleNumber: inp.getAttribute('data-article-number'),
                              quantity: parseFloat(inp.value) || 0
                          });
                      });
                      postQuickAdd(items, this);
                  });
              })();
                  </text>
              }
  ```
- [ ] Build (Razor kompiliert):
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet build IdealAkeWms.slnx
  ```
  Erwartet: `Build succeeded.` `0 Error(s)`. (Razor-Syntax-Fehler würden hier als Build-Error auftauchen.)
- [ ] Volle Web-Tests (Regression):
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet test IdealAkeWms.Tests --nologo
  ```
  Erwartet: `Passed!`.
- [ ] Commit:
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add -A && git commit -m "feat(bom): Lagerbestellung Einzel-Button + Bulk (Multi-Select) + 2 Modals + JS" -m "Neue Spalte warehouse-order mit .warehouse-select-Checkbox + .order-warehouse-btn je Nicht-Baugruppen-Zeile; Bulk-Button in der Toolbar; Modals + quick-add-fetch. Sichtbar bei LagerbestellungAktiv && CanOrderWarehouse, unabhaengig von ReadOnly. Navigation: ein Typ -> Edit, gemischt -> Uebersicht; skipped angezeigt." -m "Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
  ```

---

## Task 7: Doku + Final-Check

**Files:**
- `IdealAkeWms/Views/Help/Changelog.cshtml:16-67` (v1.25.0-Card, neuer `<li>`)
- `CLAUDE.md` (Zugriffsschutz-Tabelle + AppSettings-Tabelle + Fallstrick)
- `docs/TESTSZENARIEN.md` (neues Kapitel)
- `PROJECT_STATUS.md` (Feature-Eintrag)

**Steps:**

- [ ] **Changelog** — neuen `<li>` in die v1.25.0-Card-`<ul>` (vor `</ul>` bei Zeile 67):
  ```html
                      <li><strong>Lagerbestellung direkt aus der Stückliste:</strong> In der Stückliste
                          (Kommissionierung und Vorbau-Abarbeitung) gibt es neben &bdquo;Bedarfsmeldung&ldquo;
                          jetzt einen <strong>Lagerbestellung</strong>-Button je Zeile und einen Bulk-Button
                          für markierte Zeilen. Der Bestelltyp (Lager vs. Glas) wird automatisch aus der
                          Artikelgruppe abgeleitet; der Artikel landet im offenen Entwurf des passenden Typs
                          (neu angelegt oder wiederverwendet). Das gesamte Lagerbestellungs-Modul lässt sich
                          über den neuen Schalter <code>LagerbestellungAktiv</code> (Default aktiv) ein- und
                          ausschalten.</li>
  ```
- [ ] **CLAUDE.md — Zugriffsschutz-Tabelle:** neue Zeile (nach der `RequirePickingOrStockOrLagerbestellungAccess`-Zeile) für den Master-Gate-Filter. Da es KEIN Rollen-Filter ist, als eigene Zeile mit klarer Semantik:
  ```markdown
  | `[RequireLagerbestellungAktiv]` | *(kein Rollen-Filter — AppSetting-Gate)* | WarehouseRequisitionsController, WarehouseRequisitionsApiController, MissingPartsController, MissingPartsLagerController, WarehousePickingController (class-level, KUMULATIV zum Rollen-Filter). Master-Schalter `LagerbestellungAktiv` (Default true, nur `"false"` sperrt): MVC → Redirect Home + WarningMessage, API → 404 (v1.25.0) |
  ```
- [ ] **CLAUDE.md — AppSettings-Tabelle:** neue Zeile bei den Bestell-Settings:
  ```markdown
  | `LagerbestellungAktiv` | `true` | Master-Schalter Lagerbestellungs-Modul (Lager+Glas, Meine Fehlteile, Lager-Worklists, BOM-Button). Default true (bewusst abweichend von Opt-in-Modulen — bestehende Systeme bleiben aktiv). Nur `"false"` sperrt (v1.25.0) |
  ```
- [ ] **CLAUDE.md — neuer Fallstrick** (im Block „Bekannte Fallstricke"):
  ```markdown
  - **Lagerbestellung-aus-BOM + Master-Schalter (v1.25.0)**: Der Master-Schalter `LagerbestellungAktiv` (Default **`true`**, abweichend von den false-Default-Opt-in-Modulen) gated das komplette Modul (`WarehouseRequisitions`/`MissingParts`/`MissingPartsLager`/`WarehousePicking` MVC+API + BOM-Button). **Default-true-Read-Semantik überall identisch:** `!string.Equals(raw, "false", OrdinalIgnoreCase)` (null/`"true"` → aktiv, nur `"false"` → gesperrt) — NICHT das `?.Equals("true",…)==true`-Muster (das defaultet false). Filter `RequireLagerbestellungAktivAttribute` unterscheidet MVC (`context.Controller is Controller` → Redirect Home + WarningMessage) vs. API (`ControllerBase` → 404). Der BOM-Button (`Views/Picking/Bom.cshtml`) nutzt EIGENE `.warehouse-select`-Checkboxen (nicht die `.picking-checkbox` der Bedarfsmeldung — die existieren nur in `!ReadOnly`) und läuft im JS VOR `if (readOnly) return;`, damit er auch im Vorbau-Read-only greift. Quick-Add-API (`POST /api/warehouserequisitions/quick-add`) leitet den Typ aus der Artikelgruppe ab (Glas-Gruppe → Glas, sonst Lager; gemeinsame/EUZ → Lager), legt je Typ genau EINEN Draft je Lauf an (offener Draft via `GetOpenDraftForUserAndTypeAsync` wiederverwendet), Werkbank = **erste** zugeordnete User-Werkbank (`GetByUserIdAsync`[0]; keine → BadRequest). Ungültige Items → `skipped` (kein Abbruch); alles skipped → BadRequest. Kein Schema-Change/keine Migration.
  ```
- [ ] **CLAUDE.md — Rollenkonzept/Layout-Notiz (optional, im passenden Fallstrick):** Hinweis ergänzen, dass der „Bestellungen"-Dropdown seit v1.25.0 erscheint, wenn `BestellungenAktiv` ODER `LagerbestellungAktiv` aktiv ist (Bedarfsmeldungen hängen an `BestellungenAktiv`, die 4 Lagerbestellungs-Einträge an `LagerbestellungAktiv`).
- [ ] **TESTSZENARIEN.md — neues Kapitel** anhängen (nächste freie Kapitelnummer verwenden — im File die letzte `## Kapitel NN`-Nummer prüfen und +1). Inhalt:
  ```markdown
  ## Kapitel NN: Lagerbestellung aus der Stückliste + Master-Schalter (v1.25.0)

  ### Vorbedingungen
  - `LagerbestellungAktiv` = true (Einstellungen → Bestellungen).
  - `GlasArtikelgruppen` (z. B. `GLAS`), `GemeinsameArtikelgruppen` (`EUZ`), `DefaultLagerbestellempfaengerId`, `DefaultGlasbestellempfaengerId` konfiguriert.
  - Test-User A: Rolle `picking` (darf Lager UND Glas ordern) + mindestens eine Werkbank zugeordnet.
  - Test-User B: nur Rolle `vorbau`, KEIN Bestell-Recht.
  - Test-User C: nur Rolle `lagerbestellung` (Lager, nicht Glas).
  - Eine offene FA mit Stückliste, darunter ein Lager-Artikel (z. B. Gruppe 940), ein Glas-Artikel (Gruppe GLAS), ein EUZ-Artikel.

  ### Szenario 1 — Button-Sichtbarkeit je Rolle
  1. Als User A: Kommissionierung → FA → Stückliste öffnen.
     - Erwartet: Spalte „Lagerbestellung" mit Checkbox + Button je Nicht-Baugruppen-Zeile; Baugruppen-Zeilen leer.
  2. Als User B (nur vorbau): FA-Abarbeitungsliste → Stückliste (read-only) öffnen.
     - Erwartet: KEIN Lagerbestellung-Button (kein Bestell-Recht), obwohl read-only-BOM.
  3. Als User A: dieselbe read-only-BOM aus der Abarbeitungsliste (falls Rolle vorhanden) — Button sichtbar (unabhängig von ReadOnly).

  ### Szenario 2 — Einzelbestellung Lager (neuer Draft)
  1. Als User A ohne offenen Lager-Draft: Lager-Artikel-Button klicken → Modal zeigt Artikel + editierbare Menge (BOM-Menge vorbelegt).
  2. Menge bestätigen → „Zur Bestellung hinzufuegen".
     - Erwartet: Weiterleitung auf `/WarehouseRequisitions/Edit/{id}`, neuer Lager-Draft mit der Position.

  ### Szenario 3 — Zweite Position → selber Draft
  1. Zweiten Lager-Artikel per Einzel-Button hinzufügen.
     - Erwartet: KEIN neuer Draft — dieselbe offene Bestellung, zweite Position.

  ### Szenario 4 — Glas-Ableitung
  1. Glas-Artikel (Gruppe GLAS) per Button bestellen.
     - Erwartet: landet in einem GLAS-Draft (eigener Typ), Weiterleitung auf dessen Edit.
  2. EUZ-Artikel bestellen → landet im Lager-Draft (gemeinsame Gruppe → Lager).

  ### Szenario 5 — Bulk (gemischt)
  1. Lager-Artikel + Glas-Artikel markieren (Checkboxen) → „Lagerbestellung (Auswahl)" erscheint.
  2. Bulk-Button → Modal listet beide mit Mengen → bestätigen.
     - Erwartet: EIN Lager-Draft + EIN Glas-Draft, Positionen korrekt verteilt; Weiterleitung auf `/WarehouseRequisitions` (Übersicht) + Meldung „Zu Lager: 1, zu Glas: 1 hinzugefügt".

  ### Szenario 6 — Rechte-Fehler / Übersprungen
  1. Als User C (nur lagerbestellung, kein Glas-Recht): Glas-Artikel per Button bestellen.
     - Erwartet: Fehlermeldung „Nichts hinzugefuegt … Keine Glasbestell-Berechtigung", nichts angelegt.
  2. Bulk mit Lager-Artikel + Glas-Artikel als User C.
     - Erwartet: Lager-Artikel hinzugefügt, Glas-Artikel in „Uebersprungen".

  ### Szenario 7 — Master-Schalter aus
  1. Einstellungen → `LagerbestellungAktiv` = false → speichern.
  2. Menü prüfen: „Lagerbestellungen", „Meine Fehlteile", „Lager: Eingehende Listen", „Lager: Fehlteile" sind weg; „Bedarfsmeldungen" nur sichtbar wenn `BestellungenAktiv` an.
  3. Direkter Aufruf `/WarehouseRequisitions` → Redirect Home + Warnhinweis.
  4. Stückliste öffnen → KEIN Lagerbestellung-Button/-Spalte.
  5. `LagerbestellungAktiv` wieder auf true → alles wieder sichtbar.
  ```
- [ ] **PROJECT_STATUS.md** — kurzen Feature-Eintrag ergänzen (Muster der bestehenden Einträge): „Lagerbestellung aus BOM (Einzel + Bulk) + Master-Schalter `LagerbestellungAktiv` (v1.25.0)".
- [ ] **Final-Check — Build 0 Errors + volle Web-Tests grün + kein AppVersion-Bump:**
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && dotnet build IdealAkeWms.slnx && dotnet test IdealAkeWms.Tests --nologo
  ```
  Erwartet: `Build succeeded.` `0 Error(s)` + `Passed!` (Baseline aus Task 0 + neue Tests, 0 Failed).
- [ ] Bestätigen, dass `AppVersion.cs` (Web + Service) UNVERÄNDERT ist:
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git diff --name-only a2d0c42 -- "**/AppVersion.cs"
  ```
  Erwartet: **leere** Ausgabe (keine AppVersion-Datei geändert).
- [ ] Commit:
  ```bash
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add -A && git commit -m "docs: Lagerbestellung-aus-BOM (v1.25.0) — Changelog, CLAUDE.md, TESTSZENARIEN, PROJECT_STATUS" -m "Kein AppVersion-Bump (in v1.25.0 gefaltet)." -m "Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
  ```

---

## Datei-Struktur-Übersicht

**Neu:**
- `IdealAkeWms/Filters/RequireLagerbestellungAktivAttribute.cs` — Master-Gate-Filter (Attribut + `IAsyncActionFilter`).
- `IdealAkeWms.Tests/Filters/RequireLagerbestellungAktivFilterTests.cs` — 4 Filter-Tests (aktiv/fehlend/false-MVC/false-API).

**Geändert (Produktion):**
- `IdealAkeWms/Models/AppSettingKeys.cs` — Konstante `LagerbestellungAktiv`.
- `IdealAkeWms/Program.cs` — Seed `LagerbestellungAktiv="true"` (im `requisitionSettings`-Block).
- `IdealAkeWms/Views/Settings/Index.cshtml` — Key in Gruppe „Bestellungen".
- `IdealAkeWms/Controllers/WarehouseRequisitionsController.cs` — class-level `[RequireLagerbestellungAktiv]`.
- `IdealAkeWms/Controllers/Api/WarehouseRequisitionsApiController.cs` — class-level Filter + DI (`IProductionWorkplaceRepository`) + `QuickAdd`-Action + Records.
- `IdealAkeWms/Controllers/MissingPartsController.cs` / `MissingPartsLagerController.cs` / `WarehousePickingController.cs` — class-level `[RequireLagerbestellungAktiv]`.
- `IdealAkeWms/Controllers/PickingController.cs` — 2 ViewBags in `Bom`.
- `IdealAkeWms/Controllers/FaWorklistController.cs` — 2 ViewBags in `Bom`.
- `IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs` + `WarehouseRequisitionRepository.cs` — `GetOpenDraftForUserAndTypeAsync`.
- `IdealAkeWms/Views/Picking/Bom.cshtml` — Spalte `warehouse-order` (Checkbox + Einzel-Button), Bulk-Button, 2 Modals, JS, column-config, empty-colspan.
- `IdealAkeWms/Views/Shared/_Layout.cshtml` — `lagerbestellungAktiv`-Variable + Gating der 4 Menü-Einträge (+ Dropdown erscheint bei `BestellungenAktiv || LagerbestellungAktiv`).

**Geändert (Tests):**
- `IdealAkeWms.Tests/Controllers/WarehouseRequisitionsApiControllerTests.cs` — `Setup()` erweitert (Werkbank-Repo + Rechte-Mocks), `SeedUserWorkplace`-Helper, 9 Quick-Add-Tests.
- `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs` — 3 Tests für `GetOpenDraftForUserAndTypeAsync`.

**Geändert (Doku):**
- `IdealAkeWms/Views/Help/Changelog.cshtml`, `CLAUDE.md`, `docs/TESTSZENARIEN.md`, `PROJECT_STATUS.md`.

---

## Offene Annahmen / Entscheide

1. **Werkbank-Auflösung Quick-Add:** Es gibt keinen Single-FK-Default mehr (`DefaultWorkplaceId` → `DefaultWorkbenches` komma-String, v1.23.0). Der Plan nutzt die **erste** zugeordnete Werkbank (`GetByUserIdAsync(userId)[0]`, nach Name sortiert); Count 0 → `BadRequest("Bitte Standard-Werkbank im Profil hinterlegen.")`. Das ist pragmatischer als die CreateDraft-„count==1"-Enge (die einen Multi-Werkbank-User blockieren würde) und deckt den Normalfall ab. Falls stattdessen strikte Einzel-Werkbank-Semantik gewünscht ist (Count≠1 → BadRequest), nur `ResolveWorkplaceAsync` anpassen.
2. **`data-unit`:** Die Spec listet ein `data-unit`-Attribut; `BomItemViewModel` hat aber KEINE Unit-Property. Der Plan lässt es weg — der Server leitet die Einheit ohnehin aus `Article.Unit` ab (Quick-Add-Request trägt nur `ArticleNumber` + `Quantity`).
3. **Layout-Kopplung:** Der „Bestellungen"-Dropdown war komplett an `bestellungenAktiv` gekoppelt. Der Plan entkoppelt (`bestellungenAktiv || lagerbestellungAktiv`), sonst würde die Standardkonfiguration (`BestellungenAktiv=false`, `LagerbestellungAktiv=true`) das ganze Lagerbestellungs-Menü verstecken.
