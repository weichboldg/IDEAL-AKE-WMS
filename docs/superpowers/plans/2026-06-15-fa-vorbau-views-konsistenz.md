# FA-Vorbau-Views Konsistenz + Text-Merkmal + Werkbank-Default Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development. Steps use checkbox (`- [ ]`).

**Goal:** Fuenf Erweiterungen rund um die FA-Vorbau-Views: (1) Gear-Spaltenkonfig in allen FA-Views, (2) einheitliche BOM/enaio/Vault-Buttons in FA-Vervollstaendigen + FA-Abarbeitung, (3) per-User Werkbank-Default-Filter in der Abarbeitung, (4) Text-Merkmal-Typ, (5) Umbenennung "Arbeitsgaenge" → "FA-Vorbau-AG".

**Architecture:** Views-Konsistenz ueber ein gemeinsames Partial fuer die Dokument-Buttons. Zwei kleine Datenmodell-Erweiterungen: `User.DefaultWorkplaceId` (Migration 71) und `FaAttributeValue.TextValue` + `AttributeType.Text` (Migration 72). Rest sind UI-/Label-Aenderungen.

**Tech Stack:** ASP.NET Core 10 MVC + EF Core 10 (SQL Server), xUnit + FluentAssertions + Moq + EF InMemory.

**Rahmen:** Worktree `.claude/worktrees/missingparts-include-pd`, Branch `bugfix/missingparts-include-pd`, HEAD `3bb1bff`. Faltet in v1.22.0 — kein Versions-Bump. Migrationen **71, 72**. Baseline: 709 Web (1 skipped) + 104 Service gruen. Strikt sequentiell (Datei-Ueberschneidungen auf FaCompletion/FaWorklist-Views).

**Bekannte Bausteine:**
- enaio-Badges: shared Partial `Views/Shared/_EnaioDmsBadges.cshtml` (`EnaioDmsBadgesViewModel`).
- Vault-Link (Muster ProductionOrders/Index): `<a href="http://akevault24.ake.at/AutodeskTC/AKE-VAULT01/explore?search=@ArticleNumber&searchContext=0" target="_blank" class="vault-link" title="Zeichnung in Vault oeffnen"><svg.../></a>`.
- read-only BOM: `FaCompletion/Bom/{id}` + `FaWorklist/Bom/{id}`.
- Gear-Spaltenkonfig: Inline-`#view-config`-Block (Format aus `ProductionOrders/Index.cshtml` bzw. `PickingLeitstand/Index.cshtml` ablesen) + `filterable-table data-view-key="..."`.

---

### Task 0: Pre-Flight

- [ ] `git log --oneline -1` → `3bb1bff` (oder neuer), clean. `dotnet build IdealAkeWms.slnx` + `dotnet test IdealAkeWms.slnx --no-build` → 709 Web (1 skipped) + 104 Service. Zahlen notieren.

---

### Task 1: FA-Views-Konsistenz — Gear-Spaltenkonfig + BOM/enaio/Vault-Buttons (A1 + A2)

**Files:**
- Create: `IdealAkeWms/Views/Shared/_FaDocumentLinks.cshtml` (+ ggf. ViewModel)
- Modify: `IdealAkeWms/Views/FaCompletion/Index.cshtml`, `FaCompletion/Edit.cshtml`, `FaWorklist/Index.cshtml`

- [ ] **Step 1: Gemeinsames Dokument-Link-Partial.** `_FaDocumentLinks.cshtml` nimmt entgegen: `string? ArticleNumber`, `string OrderNumber`, `int ProductionOrderId`, `string BomController` ("FaCompletion" bzw. "FaWorklist"), `List<EnaioDmsDocumentLink>? EnaioLinks`. Rendert in einheitlicher Reihenfolge/Optik:
  1. **BOM-Button** → `<a asp-controller="@BomController" asp-action="Bom" asp-route-id="@ProductionOrderId" title="Stueckliste">` (Icon).
  2. **enaio-Badges** → `@await Html.PartialAsync("_EnaioDmsBadges", new EnaioDmsBadgesViewModel(EnaioLinks))` (nur wenn vorhanden).
  3. **Vault-Link** (nur wenn ArticleNumber) → Vault-Markup oben.
  Kleines ViewModel `FaDocumentLinksViewModel` mit diesen Feldern (in `Models/ViewModels/`).
- [ ] **Step 2: Einbinden.** In den 3 Views die FA-Nr-/Artikel-Zelle bzw. den Kopf so anpassen, dass die drei Buttons konsistent erscheinen:
  - `FaWorklist/Index.cshtml`: FA-Nr-Zelle nutzt das Partial (BomController="FaWorklist"); enaio-Links aus `Model.EnaioDmsLinks`.
  - `FaCompletion/Index.cshtml`: FA-Nr-Zelle nutzt das Partial (BomController="FaCompletion"); enaio-Links muessen pro Zeile vorhanden sein — falls `FaCompletionListViewModel` keine `EnaioDmsLinks` hat, im Controller `Index` per `GetByOrderNumbersAsync(orderNumbers)` laden + ins ViewModel (Muster ProductionOrders/Index). (Die bestehende FA-Nr→Bom-Verlinkung wird durch den BOM-Button im Partial abgeloest.)
  - `FaCompletion/Edit.cshtml`: Kopfbereich (hat schon enaio + Stueckliste-Button) auf das Partial vereinheitlichen + Vault-Link ergaenzen. Optik = wie Abarbeitungsliste.
- [ ] **Step 3: Gear-Spaltenkonfig.** In `FaCompletion/Index.cshtml` + `FaWorklist/Index.cshtml` den Inline-`#view-config`-Block ergaenzen (Format 1:1 aus `ProductionOrders/Index.cshtml` ablesen: JSON-Liste der Spalten mit key/label/locked/defaultWidth). Jede Tabellen-Spalte mit ihrem `data-col-key` eintragen, damit das Zahnrad-Icon (Spalten ein-/ausblenden + Reihenfolge) funktioniert. `data-view-key` ist bereits gesetzt ("FaCompletion"/"FaWorklist"). Sicherstellen dass `column-preferences.js` VOR `table-filter.js` geladen wird (sollte schon).
- [ ] **Step 4:** Build + Vollsuite (PowerShell). Smoke: beide Views rendern, Gear zeigt Spaltenliste.
- [ ] **Step 5: Commit** `feat(fa-views): einheitliche BOM/enaio/Vault-Buttons + Gear-Spaltenkonfig in FA-Vervollstaendigen/Abarbeitung`

---

### Task 2: Per-User Werkbank-Default-Filter in der Abarbeitung (A3)

**Files:**
- Modify: `IdealAkeWms/Models/User.cs` + `ApplicationDbContext.cs` (FK) + Migration `AddUserDefaultWorkplace` (71) + `SQL/71_*.sql` + `SQL/00_FreshInstall.sql`
- Modify: `IdealAkeWms/Models/ViewModels/ProfileViewModel.cs` + `UserEditViewModel.cs` + `FaWorklistViewModel.cs`
- Modify: `IdealAkeWms/Controllers/AccountController.cs` + `UsersController.cs` + `FaWorklistController.cs`
- Modify: `IdealAkeWms/Views/Account/Profile.cshtml` + `Users/Edit.cshtml` + `FaWorklist/Index.cshtml`
- Test: `AccountControllerTests` / `UsersControllerTests` (+1) + `FaWorklistControllerTests` (+1)

- [ ] **Step 1: User-Feld + Migration.** `User.cs` nach `DefaultWorkStepId`:
```csharp
    /// <summary>Vorausgewaehlte Werkbank-Zusatzfilter in der FA-Abarbeitungsliste (NULL = alle).</summary>
    [Display(Name = "Standard-Werkbank (FA-Abarbeitungsliste)")]
    public int? DefaultWorkplaceId { get; set; }
    public ProductionWorkplace? DefaultWorkplace { get; set; }
```
DbContext User-Block: `HasOne(DefaultWorkplace).WithMany().HasForeignKey(DefaultWorkplaceId).OnDelete(SetNull)`. Migration `dotnet ef migrations add AddUserDefaultWorkplace`; `has-pending-model-changes` leer. `SQL/71_AddUserDefaultWorkplace.sql` idempotent (Spalte INT NULL + FK auf ProductionWorkplaces + Index + History; Muster `SQL/70`). FreshInstall: Spalte in Users-CREATE + FK NACH ProductionWorkplaces-CREATE + History. **Beide Stellen.**
- [ ] **Step 2: Profil/Benutzerstamm-UI** (Muster `DefaultWorkStepId`, das in Profile + Users/Edit schon existiert): `int? DefaultWorkplaceId` + `List<ProductionWorkplace> AvailableWorkplaces` in beiden ViewModels; Controller laden (`IProductionWorkplaceRepository.GetAllOrderedAsync()`) + speichern; Views Dropdown "(alle)" + Werkbaenke. +1 Test (Profile_Post_SavesDefaultWorkplace).
- [ ] **Step 3: FaWorklist Werkbank-Filter.** `FaWorklistViewModel`: `int? SelectedWorkplaceId` + `List<ProductionWorkplace> AvailableWorkplaces`. Controller `Index(int? workStepId, int? workplaceId, bool showDone, ...)`:
  - `AvailableWorkplaces` laden.
  - Default `workplaceId`: wenn null → User.DefaultWorkplaceId (analog DefaultWorkStepId).
  - In der FA-Auswahl (nach dem WorkStep-Filter) zusaetzlich auf `o.ProductionWorkplaceId == workplaceId` filtern, WENN workplaceId gesetzt (sonst alle Werkbaenke). Kombiniert mit dem Arbeitsgang-Filter (UND).
  - Test `Index_FiltersByWorkplace_WhenSet` (2 FAs gleicher AG, verschiedene Werkbaenke → workplaceId-Filter liefert 1) + `Index_UsesUserDefaultWorkplace_WhenNoParam`.
- [ ] **Step 4: View.** `FaWorklist/Index.cshtml` Filter-Card: zusaetzliches Werkbank-Dropdown ("(alle)" + Werkbaenke, `SelectedWorkplaceId`, onchange-Submit) neben dem Arbeitsgang-Dropdown.
- [ ] **Step 5:** Build + Vollsuite + Commit `feat(worklist): per-User Werkbank-Default-Filter in der FA-Abarbeitung (Migration 71)`

---

### Task 3: Text-Merkmal-Typ (B1)

**Files:**
- Modify: `IdealAkeWms/Models/ArticleAttributeDefinition.cs` (Enum `AttributeType` + `Text`) — Enum ist shared.
- Modify: `IdealAkeWms/Models/FaAttributeValue.cs` (+ `string? TextValue`) + `ApplicationDbContext.cs` + Migration `AddFaAttributeTextValue` (72) + `SQL/72_*.sql` + FreshInstall
- Modify: `IdealAkeWms/Data/Repositories/FaAttributeRepository.cs` (`UpsertValueAsync` um textValue) + Interface
- Modify: `IdealAkeWms/Controllers/FaAttributesController.cs` (Typ-Dropdown bietet Text) + `FaCompletionController.cs` (SaveAttributeValue nimmt textValue)
- Modify: `IdealAkeWms/Views/FaAttributes/*` (Typ-Option Text) + `FaCompletion/Edit.cshtml` (Text-Input) + `FaWorklistController.FormatAttributeValue` (Text-Anzeige)
- Test: `FaAttributeRepositoryTests` (+1) + ggf. Controller

- [ ] **Step 1: Enum + Wert-Feld + Migration.** `AttributeType` um `Text = 2` ergaenzen (Datei `ArticleAttributeDefinition.cs`). `FaAttributeValue.cs` + `string? TextValue`. DbContext FaAttributeValue-Block: `Property(TextValue).HasMaxLength(1000)`. Migration `AddFaAttributeTextValue` (nur FaAttributeValues-Spalte; der Enum-Wert ist kein Schema). `SQL/72` idempotent + FreshInstall (Spalte `[TextValue] NVARCHAR(1000) NULL` im FaAttributeValues-CREATE + History). **WICHTIG:** ArticleAttributes-UI darf `Text` NICHT anbieten (ArticleAttributeValue hat kein TextValue) — nur FaAttributes bietet Text an. Kommentar am Enum dazu.
- [ ] **Step 2: Repo.** `UpsertValueAsync(poId, defId, optionId?, boolValue?, textValue?, by, byWin)` — Signatur um `string? textValue` erweitern; "leer" (alle null/leer) loescht die Zeile. Test `UpsertValue_StoresTextValue`. Aufrufer (FaCompletionController.SaveAttributeValue) anpassen.
- [ ] **Step 3: FaAttributes Stammdaten.** Typ-Dropdown um Option "Text" erweitern (in der FaAttributes-Definition-Form). Bei Typ=Text keine Optionen noetig (Optionen-Block ausblenden/ignorieren).
- [ ] **Step 4: FaCompletion Edit.** Im Merkmal-Block fuer `AttributeType.Text` ein `<input type="text">` (statt Dropdown/JA-NEIN), Wert = aktueller TextValue, `onchange`/blur-Submit auf `SaveAttributeValue` mit `textValue`-Feld (+ hidden tab wie bei den anderen). Das Tab-Models/Attribut-VM braucht ggf. ein `TextValue`-Feld.
- [ ] **Step 5: Worklist-Anzeige.** `FaWorklistController.FormatAttributeValue` (+ FaCompletion-Tab-Anzeige): `AttributeType.Text` → `value.TextValue ?? ""`.
- [ ] **Step 6:** Build + Vollsuite + Commit `feat(fa-merkmale): Text-Merkmal-Typ (Migration 72)`

---

### Task 4: Umbenennung "Arbeitsgaenge" → "FA-Vorbau-AG" (B2)

**Files:** UI-Labels in `_Layout.cshtml` (Menue), `Views/WorkSteps/*`, `Views/FaAttributes/*` (AG-Zuordnung-Label), `Views/FaCompletion/*`, `Views/FaWorklist/*`, `Views/Users/RoleOverview.cshtml`, Hilfe — NUR die FA-Vorbau-bezogenen Stellen.

- [ ] **Step 1:** `grep -rn "Arbeitsgang\|Arbeitsgaenge\|Arbeitsgänge"` ueber `IdealAkeWms/Views` + Layout. Pro Treffer entscheiden: gehoert er zum **FA-Vorbau** (WorkSteps-Stammdaten, FaWorkStep-Kontext, FA-Vervollstaendigen/Abarbeitung, FaAttributes-AG-Zuordnung, Leitstand-VK-VA-Tooltips) → umbenennen auf **"FA-Vorbau-AG"** bzw. **"FA-Vorbau-Arbeitsgang"** (Singular/Plural sinnvoll). NICHT anfassen: BDE-Arbeitsgaenge, OSEON-Arbeitsgaenge/Teileverfolgung (anderer Kontext).
- [ ] **Step 2:** Stammdaten-Menue-Eintrag "Arbeitsgaenge" → "FA-Vorbau-AG"; WorkSteps Index/Create/Edit Page-Header + Labels; FaAttributes "Zugeordnete Arbeitsgaenge" → "Zugeordnete FA-Vorbau-AG"; FaWorklist Arbeitsgang-Dropdown-Label → "FA-Vorbau-AG"; FaCompletion-Tab-/Hinweistexte. Routen/Controller-Namen (`WorkStepsController`) + Code bleiben unveraendert (nur Anzeige-Labels).
- [ ] **Step 3:** Build + Vollsuite + Commit `refactor(ui): 'Arbeitsgaenge' → 'FA-Vorbau-AG' (nur FA-Vorbau-Labels)`

---

### Task 5: Doku + Final-Check + Review

- [ ] **Step 1: Doku.** Changelog (v1.22.0-Card) Bullets: einheitliche FA-View-Buttons + Gear-Spaltenkonfig; Werkbank-Default-Filter; Text-Merkmal; Umbenennung. `CLAUDE.md`: AppSettings/Service unveraendert; Hinweis Text-Merkmal nur FaAttributes (nicht ArticleAttributes); `User.DefaultWorkplaceId`. `docs/TESTSZENARIEN.md` Kapitel 38: Faelle (Gear-Spalten, Werkbank-Default, Text-Merkmal). Hilfe Index: Labels "FA-Vorbau-AG".
- [ ] **Step 2: Final-Check.** Build 0 Fehler; Tests gruen; `has-pending-model-changes` leer; SQL/71+72 + FreshInstall (MigrationIds + Spalten); kein "Arbeitsgang"-Restlabel im FA-Vorbau-Kontext.
- [ ] **Step 3: Final-Review-Subagent** (read-only) ueber die Range.

### Task 6: PAUSE — User-Test (NICHT autonom mergen)
