# OverridePrePickingDays wirksam machen — Implementation Plan (Variante A)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `ProductionWorkplace.OverridePrePickingDays` wird in der Terminberechnung des
Vorkommissioniertermins wirksam (Werkbank-Override schlaegt globales `VorkommissionierTage`),
konsistent in allen drei Listen, mit Pro-Zeile-Rueckmeldung in der UI.

**Architecture:** Kein neues Muster — Anwendung des bestehenden
„Werkbank-Override-gewinnt-wenn-gesetzt"-Musters (`BdeDefaultArbeitsgang`). Die Prioritaetsregel
liegt an **einer** Codestelle (`PrePickingDaysResolver`, Freigabe-Antwort 4) und wird von den drei
Controllern aufgerufen. Der Override-Wert kommt fuer Leitstand/FA-Liste ueber ein neues Feld in der
bestehenden `LeitstandOrderRow`-Projection, fuer die FA-Abarbeitungsliste je AG direkt aus der
bereits `Include`-geladenen Navigation. Keine Schema-Aenderung, keine Migration.

**Tech Stack:** ASP.NET Core 10 MVC, EF Core 10, xUnit + FluentAssertions + Moq + EF-InMemory,
Bootstrap 5.

## Global Constraints

- Freigabe-Entscheidung: **Variante A** (integrieren), Spec
  `secondbrain/specs/freigegeben/2026-07-28-override-prepickingdays.md`.
- Prioritaetsregel: `OverridePrePickingDays.HasValue` (inkl. `0`) gewinnt; sonst globales
  `VorkommissionierTage` (Default `1`). `0` ist ein gueltiger expliziter Override.
- Regel steht an genau **einer** Codestelle (Freigabe-Antwort 4: „gleich wie ueberall").
- Pro-Zeile-Rueckmeldung (Tooltip/Badge an der Werkbank-Zelle) in **allen drei** Listen inkl.
  `FaWorklist/Index` (Freigabe-Antwort 3 + 5).
- Kein Schema-Diff: keine Migration, kein neues SQL-Skript, `SQL/00_FreshInstall.sql` unveraendert.
- Sprachregel: Code englisch, UI-Texte deutsch.
- Bestandsdaten laut Freigabe-Antwort 2: genau eine Werkbank betroffen (`Id=1`, `Name=A1`,
  `OverridePrePickingDays=7`) — Blast Radius = FAs an Werkbank `A1`, deren BG-Termin sich von
  `Komm. - 1 AT` auf `Komm. - 7 AT` verschiebt (inkl. Kaskade auf den Beschichtungstermin).

## File Structure

| Datei | Verantwortung |
|---|---|
| `IdealAkeWms/Services/PrePickingDaysResolver.cs` (neu) | Prioritaetsregel, einzige Codestelle |
| `IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs` | `LeitstandOrderRow` + Override-Feld |
| `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs` | Projection mappt Override |
| `IdealAkeWms/Controllers/{ProductionOrders,PickingLeitstand,FaWorklist}Controller.cs` | Resolver-Aufruf |
| `IdealAkeWms/Models/ViewModels/{ProductionOrderList,PickingLeitstand,FaWorklist}ViewModel.cs` | `PrePickingDaysOverride` je Zeile (+ `VorkommissionierTage` in `FaWorklistViewModel`) |
| `IdealAkeWms/Views/{ProductionOrders,PickingLeitstand,FaWorklist}/Index.cshtml` | Badge + Tooltip an der Werkbank-Zelle, generischer Spaltenkopf-Tooltip |
| `IdealAkeWms.Tests/Services/PrePickingDaysResolverTests.cs` (neu) | Regel isoliert |
| `IdealAkeWms.Tests/Controllers/*` | Regel je Controller |

---

### Task 1: `PrePickingDaysResolver` (Prioritaetsregel an einer Stelle)

**Files:**
- Create: `IdealAkeWms/Services/PrePickingDaysResolver.cs`
- Test: `IdealAkeWms.Tests/Services/PrePickingDaysResolverTests.cs`

**Interfaces:**
- Consumes: nichts.
- Produces: `static int PrePickingDaysResolver.Resolve(int? workplaceOverride, int globalDays)`,
  `static bool PrePickingDaysResolver.IsOverrideActive(int? workplaceOverride)`.

- [ ] **Step 1: Failing Test schreiben** (`PrePickingDaysResolverTests`): Override gewinnt,
      `0` gewinnt, `null` faellt auf global zurueck, `IsOverrideActive` unterscheidet strikt nach
      `HasValue`.
- [ ] **Step 2:** `dotnet test IdealAkeWms.Tests --filter PrePickingDaysResolver` → FAIL (Typ fehlt).
- [ ] **Step 3:** Resolver implementieren (`workplaceOverride ?? globalDays`).
- [ ] **Step 4:** Test erneut → PASS.

### Task 2: Override in die `LeitstandOrderRow`-Projection

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs` (record `LeitstandOrderRow`)
- Modify: `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs` (`GetForLeitstandAsync`)

**Interfaces:**
- Produces: `LeitstandOrderRow.WorkplaceOverridePrePickingDays` (`int?`, **letzter** Parameter mit
  Default `null`, damit alle bestehenden positionalen Konstruktor-Aufrufe kompilieren).

- [ ] **Step 1:** Feld am Record-Ende ergaenzen.
- [ ] **Step 2:** In der `.Select(...)`-Projection
      `o.ProductionWorkplace != null ? o.ProductionWorkplace.OverridePrePickingDays : null` mappen.
- [ ] **Step 3:** `dotnet build` → gruen.

### Task 3: `ProductionOrdersController.Index` (FA-Liste)

**Files:**
- Modify: `IdealAkeWms/Controllers/ProductionOrdersController.cs:128-142`
- Modify: `IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs` (`ProductionOrderListItem`)
- Modify: `IdealAkeWms/Views/ProductionOrders/Index.cshtml:87,151`
- Test: `IdealAkeWms.Tests/Controllers/ProductionOrdersControllerSlimTests.cs`

**Interfaces:**
- Consumes: Task 1 (`PrePickingDaysResolver`), Task 2 (`WorkplaceOverridePrePickingDays`).
- Produces: `ProductionOrderListItem.PrePickingDaysOverride` (`int?`, nur gesetzt wenn ein Override
  tatsaechlich griff).

- [ ] **Step 1:** Test: Row mit Override `7`, global `1` → `VorkommissionierTermin` = Komm. - 7 AT,
      `PrePickingDaysOverride == 7`; Row ohne Override → global, `PrePickingDaysOverride == null`;
      Override `0` → `VorkommissionierTermin == KommissionierTermin`.
- [ ] **Step 2:** Test laufen → FAIL.
- [ ] **Step 3:** Controller: `PrePickingDaysResolver.Resolve(...)` statt `vorkommissionierTage`;
      `PrePickingDaysOverride` setzen wenn `IsOverrideActive`.
- [ ] **Step 4:** View: Badge + `title` an der Werkbank-Zelle; Spaltenkopf-Tooltip „BG-Termin"
      generisch.
- [ ] **Step 5:** Test → PASS.

### Task 4: `PickingLeitstandController.Index` (Leitstand)

**Files:**
- Modify: `IdealAkeWms/Controllers/PickingLeitstandController.cs:133-144`
- Modify: `IdealAkeWms/Models/ViewModels/PickingLeitstandViewModel.cs` (`PickingLeitstandItem`)
- Modify: `IdealAkeWms/Views/PickingLeitstand/Index.cshtml:119,195`
- Test: `IdealAkeWms.Tests/Controllers/PickingLeitstandControllerTests.cs`

**Interfaces:** wie Task 3, Property heisst identisch `PrePickingDaysOverride`.

- [ ] **Step 1-5:** analog Task 3 (Beschichtungstermin laeuft weiter ueber
      `CoatingDateCalculator.Compute` und erbt die Verschiebung automatisch).

### Task 5: `FaWorklistController.Index` (FA-Abarbeitungsliste je AG)

**Files:**
- Modify: `IdealAkeWms/Controllers/FaWorklistController.cs:209-219`
- Modify: `IdealAkeWms/Models/ViewModels/FaWorklistViewModel.cs` (`FaWorklistRow` +
  `FaWorklistViewModel.VorkommissionierTage` fuer den „Standard: Y"-Teil des Tooltips)
- Modify: `IdealAkeWms/Views/FaWorklist/Index.cshtml:133`
- Test: `IdealAkeWms.Tests/Controllers/FaWorklistControllerTests.cs` (echtes Repo + InMemory)

**Interfaces:** `order.ProductionWorkplace?.OverridePrePickingDays` ist bereits per `Include`
geladen (`GetAllOrderedAsync`) — kein Repository-Eingriff noetig.

- [ ] **Step 1:** Test: Werkbank mit `OverridePrePickingDays = 3` vs. Werkbank ohne → beide Zeilen
      pruefen (`BusinessDayService` ist hier echt, also echte Arbeitstags-Arithmetik).
- [ ] **Step 2:** Test → FAIL.
- [ ] **Step 3:** Controller + ViewModel anpassen.
- [ ] **Step 4:** View: Badge + Tooltip an der Werkbank-Zelle (Freigabe-Antwort 5: ja).
- [ ] **Step 5:** Test → PASS.

### Task 6: Doku, Version, Brain

**Files:**
- Modify: `docs/TESTSZENARIEN.md` (Kapitel 9, TS-9.6 – TS-9.10 + Inhaltsverzeichnis-Zeile)
- Modify: `secondbrain/tests/testszenarien-index.md` (Kapitel-9-Zeile auf TS-9.1 – 9.10)
- Modify: `IdealAkeWms/AppVersion.cs`, `IDEALAKEWMSService/AppVersion.cs` → `1.27.0`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml` (Anwender-Changelog v1.27.0)
- Create: `secondbrain/changelog/2026-07-28-v1-27-0-override-prepickingdays.md`
- Modify: `secondbrain/feature-map.md:141` (offen → umgesetzt, Verweis auf Spec)
- Modify: `secondbrain/architektur/fallstricke.md:596` (Eintrag „ist wirkungslos" additiv durch die
  jetzt geltende Prioritaetsregel ersetzen)
- Modify: `secondbrain/specs/freigegeben/2026-07-28-override-prepickingdays.md` (Frontmatter:
  `freigabe.entscheidung: A`, `status`, `worktree`, `branch`)

- [ ] **Step 1:** Testszenarien TS-9.6 – TS-9.10 vollstaendig ausformulieren
      (Vorbedingungen/Schritte/Erwartung/Negativfall).
- [ ] **Step 2:** Index, Version, beide Changelogs, feature-map, fallstricke nachziehen.
- [ ] **Step 3:** `dotnet build` + `dotnet test` → Beweis in die Spec (macht der `qa-agent`).

## Self-Review

- **Spec-Deckung:** Anforderung 3 → Task 1; 4 → Tasks 3-5; 5 → automatisch via
  `CoatingDateCalculator` (Task 4/5, keine Code-Aenderung noetig); 6 → Views in Tasks 3-5;
  7 → Task 3/4 Step 4; 8 → Tasks 1/3/4/5 Tests; Gemeinsame Anforderung 2 → Task 6.
  Akzeptanzkriterien 1-8 sind abgedeckt (8 = kein Schema-Diff: bewusst keine Migration).
- **Platzhalter:** keine.
- **Typkonsistenz:** `PrePickingDaysOverride` (`int?`) in allen drei Zeilen-ViewModels gleich
  benannt; `WorkplaceOverridePrePickingDays` nur im Repository-Record.
