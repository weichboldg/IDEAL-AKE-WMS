# Matchcode im Artikelstamm (Article.Matchcode) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Der Matchcode wird ein Feld von `Article` (nicht `ProductionOrder`), damit er in JEDER artikelbezogenen Suche sichtbar ist; die in v1.38.0 gebaute (nie gemergte) `ProductionOrder.Matchcode`-Spalte wird sauber zurückgebaut, die fünf FA-Listen behalten Spalte+Filter aus `Article` per Join über `ArticleNumber`.

**Architecture:** Neue Spalte `Article.Matchcode` (NVARCHAR(200)). Die fünf FA-Zeilen-Listen lesen den Matchcode künftig über einen Equi-Join auf `Article.ArticleNumber` (Batch-Dictionary nach der Pagination für Anzeige + AK-14-Zählung; Spaltenfilter der FA-Liste wird zum C#-Postfilter). Alle artikelbezogenen Suchstellen (Artikelstamm, Bestand, Fehlteile, Bedarfsmeldungen) bekommen Matchcode als drittes OR-Glied. Sage-Sync füllt `Article.Matchcode` aus `KHKArtikel.Matchcode`.

**Tech Stack:** ASP.NET Core 10 MVC + EF Core 10, SQL Server, Bootstrap 5 + Select2, xUnit/FluentAssertions/Moq/EF-InMemory.

**Spec:** `C:\git\IDEAL-AKE-WMS\secondbrain\specs\freigegeben\2026-09-13-matchcode-artikelstamm-spec.md` (Hauptcheckout; der Worktree hat kein `secondbrain/`).

**Worktree:** `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`. Alle Pfade unten relativ dazu.

## Global Constraints

- **Auftrag = `affected_code`, „Technischer Lösungsentwurf", AK 1–16.** Die Abschnitte „Freigabe-Antworten"/„ANTWORTEN"/„Kritische Prüfung" sind **Begründung, nicht Auftrag** (Nutzer-Vorgabe).
- **Migrationsreihenfolge verbindlich: ZUERST `remove` (ProductionOrder.Matchcode), DANACH `add` (AddArticleMatchcode).** `ef migrations remove` trägt nur, solange `20260910081155_AddProductionOrderMatchcode` die letzte Migration ist (verifiziert). Passiert in Task 3 — kein anderer Task legt eine Migration an.
- **AK 16 — Feldlänge 200, beide Zahlen identisch:** `Article.Matchcode` `[StringLength(200)]` → `NVARCHAR(200)`; der Sage-`CAST` ist `nvarchar(200)`. Das `nvarchar(500)` im Freigabe-Antwort-Snippet ist Begründung/überholt — **200 gilt**. Ein CAST größer als die Zielspalte ist unzulässig.
- **Null-sicher überall (F7):** leerer Matchcode = „kein Treffer auf dieser Spalte", nie Exception, nie unerwartet leere Gesamtliste.
- **AK 14 — melden statt still:** jeder erweiterte Listenaufbau zählt Zeilen ohne Artikelstamm-Treffer und protokolliert **eine** Zahl je Aufbau (nicht je Zeile).
- **Audit-Felder** bei jedem `Article`-Schreiben (Sync + `/Articles/Edit`): `ModifiedAt`/`ModifiedBy`/`ModifiedByWindows` — unverändertes Muster, nur ein Feld mehr.
- **Anzeige/Filter der fünf FA-Listen bleiben** — nur die Datenquelle wechselt von `ProductionOrder.Matchcode` auf `Article` (Equi-Join `ArticleNumber`). `ColumnDefinitions.cs`, `#column-config`, Row-Partials bleiben unberührt.
- Code Englisch, UI Deutsch. Kein Merge, kein Push. Keine Schreibzugriffe auf `secondbrain/`. **Web-App NICHT lokal starten** (`Program.cs` ruft `Database.Migrate()` gegen AKESQL20). `dotnet build`/`dotnet test` nur.
- Commit-Trailer: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>` und `Claude-Session: https://claude.ai/code/session_01HqgAuqzkKHLpYraKhCi797`.

## Code-verifizierte Korrekturen an der Spec (A-Befunde, 2026-09-15 am Worktree gemessen)

- **A1 — `BuildExtraInfoOrContains` ist NICHT für die Article-Subquery verwendbar.** Der Helper (`ProductionOrderRepository.cs:415`) castet den Selektor-Body zu `MemberExpression` und zieht `.Expression` als Null-Guard; eine korrelierte Subquery ist ein `MethodCallExpression`. Spec-Zeile 318-321 („Helper akzeptiert beliebigen Selektor") ist falsch. **Lösung:** Der Matchcode-**Spaltenfilter der FA-Liste** (`ProductionOrdersController`) wandert in die bestehende C#-Postfilter-Maschinerie: `"matchcode"` in `FaListComputedColumnKeys` aufnehmen (`ProductionOrdersController.cs:449`), Server-Filter-Case `"matchcode"` (`ProductionOrderRepository.cs:308`) entfernen. Der Postfilter filtert auf `item.Matchcode` (bereits aus dem Article-Join gefüllt). Die Client-Filter-Dicts (`i => i.Matchcode`) in FaCompletion/FaWorklist/Picking bleiben unverändert.
- **A2 — Fünf Projektions-Quellstellen, nicht „ein Ort":** `ProductionOrdersController.cs:151`, `ProductionOrderRepository.ProjectLeitstandRows:178` (→ PickingLeitstand), `FaCompletionController.cs:125`, `FaWorklistController.cs:225`, `PickingController.cs:159`. Erste zwei sind IQueryable-Projektionen (korrelierte Subquery möglich); Picking ist eine In-Memory-Projektion über eine materialisierte `List<ProductionOrder>` (braucht Batch-Dict). FaCompletion/FaWorklist am Diff prüfen (IQueryable vs. in-memory).
- **A3 — AK 15 gilt (feuert je Tastendruck):** Artikel-Typeahead über Select2 mit `minimumInputLength: 0` in `_Select2ArticlePartial.cshtml`, `StockMovements/InboundBulk.cshtml`; `WarehouseRequisitions/Edit.cshtml` per custom fetch. `_Select2ProductionOrderPartial` ist Auftrags-, keine Artikelsuche → unberührt.
- **A4 — `FaMaterializationPlanner.MaterializationSourceOrder` führt Matchcode NICHT separat** (Grep leer) → in Task 3 nichts dort zu kürzen.
- **A5 — Article-Sync-Query ist ein `WITH RawArticles AS (…UNION…) SELECT … GROUP BY ArticleNumber`** (`SageImportService.cs:382-421`), beide Zweige joinen `KHKArtikel a`. Matchcode in beide Zweige als `CAST(a.Matchcode AS nvarchar(200)) AS Matchcode`, außen `MAX(Matchcode) AS Matchcode`.

## Design-Entscheidungen

- **D1 — Article-Matchcode-Lookup als eine Repository-Methode:** `IArticleRepository.GetMatchcodesByArticleNumbersAsync(IReadOnlyCollection<string> articleNumbers) → Task<Dictionary<string, string?>>` (case-insensitive Keys via `StringComparer.OrdinalIgnoreCase`). Deckt In-Memory-Projektion (Picking) UND AK-14-Zählung (fehlend = ArticleNumber nicht im Dict) für ALLE fünf Listen ab. Für die IQueryable-Projektionen (ProductionOrders, ProjectLeitstandRows) wird nach der Pagination derselbe Dict-Ansatz genutzt: Seite materialisieren → Article-Nummern sammeln → ein Batch-Query → `item.Matchcode` setzen + Fehltreffer zählen. Kein korreliertes SQL-Subquery, kein InMemory-Risiko, ein Muster für alle fünf.
- **D2 — Matchcode-Spaltenfilter FA-Liste = Postfilter** (siehe A1). Nur `ProductionOrdersController` nutzt `FaListComputedColumnKeys`; die anderen vier filtern client-seitig auf `item.Matchcode`. Damit filtert überall der berechnete Wert.
- **D3 — Version 1.40.0, Testkapitel TS-75.**
- **D4 — ArticlesController.Info Exakt-Matchcode-Fallback** (affected_code:30 + Freigabe-Antwort 2, jetzt entschieden): schlägt der exakte `GetByArticleNumberAsync` fehl, EXAKT (kein Teilstring) über Matchcode nachschlagen. Neue Repo-Methode `GetByMatchcodeExactAsync(string matchcode)`. Klein, eindeutig, testbar.

---

### Task 1: `Article.Matchcode` + Batch-Lookup-Repository-Methode (additiv)

**Files:**
- Modify: `IdealAkeWms/Models/Article.cs` (Property nach `Description`)
- Modify: `IdealAkeWms/Data/Repositories/IArticleRepository.cs` + `ArticleRepository.cs` (zwei Methoden)
- Test: `IdealAkeWms.Tests/Repositories/ArticleRepositoryTests.cs` (neu oder vorhanden)

**Interfaces:**
- Produces: `Article.Matchcode` (`string?`, `[StringLength(200)]`, `[Display(Name="Matchcode")]`)
- Produces: `Task<Dictionary<string,string?>> IArticleRepository.GetMatchcodesByArticleNumbersAsync(IReadOnlyCollection<string> articleNumbers)`
- Produces: `Task<Article?> IArticleRepository.GetByMatchcodeExactAsync(string matchcode)`

- [ ] **Step 1: Failing tests** — `GetMatchcodesByArticleNumbersAsync` liefert Dict nur für existierende Artikel (fehlende Nummern fehlen im Dict; leerer Matchcode kommt als `null`-Value zurück; leere Eingabe → leeres Dict); `GetByMatchcodeExactAsync` liefert exakten Treffer, `null` bei Teilstring/kein Treffer. InMemory-DB (`TestDbContextFactory`).
- [ ] **Step 2: Run** `dotnet test IdealAkeWms.Tests --filter ArticleRepositoryTests` → FAIL.
- [ ] **Step 3: Implementation**

`Article.cs` nach der `Description`-Property:
```csharp
    [StringLength(200)]
    [Display(Name = "Matchcode")]
    public string? Matchcode { get; set; }
```

`ArticleRepository.cs`:
```csharp
    /// <summary>Matchcode je Artikelnummer für einen Stapel (Anzeige + AK-14-Zählung der FA-Listen):
    /// eine Abfrage statt N. Fehlende Artikelnummer ⇒ NICHT im Dict (⇒ Fehltreffer); vorhandener,
    /// aber leerer Matchcode ⇒ Value null. Keys case-insensitiv (ArticleNumber ist UNIQUE, aber
    /// Groß/Klein soll nicht am Treffer entscheiden).</summary>
    public async Task<Dictionary<string, string?>> GetMatchcodesByArticleNumbersAsync(IReadOnlyCollection<string> articleNumbers)
    {
        var wanted = articleNumbers.Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();
        if (wanted.Count == 0) return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var rows = await _dbSet.Where(a => wanted.Contains(a.ArticleNumber))
                               .Select(a => new { a.ArticleNumber, a.Matchcode })
                               .ToListAsync();
        return rows.ToDictionary(r => r.ArticleNumber, r => r.Matchcode, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<Article?> GetByMatchcodeExactAsync(string matchcode)
        => await _dbSet.FirstOrDefaultAsync(a => a.Matchcode == matchcode);
```
(Interface um beide Signaturen ergänzen.)

- [ ] **Step 4: Run** → PASS; `dotnet build IdealAkeWms.slnx` 0 Fehler (eine `PendingModelChangesWarning` kann build-seitig nicht auftreten — Snapshot wird erst in Task 3 gezogen; falls ein Test `Migrate()` auslöst: er nutzt InMemory, kein Snapshot-Check).
- [ ] **Step 5: Commit** `feat(matchcode): Article.Matchcode + Batch-Lookup GetMatchcodesByArticleNumbersAsync`

---

### Task 2: Fünf FA-Listen auf Article-Matchcode umstellen + AK-14-Zähler + Matchcode-Postfilter

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs` (`ProjectLeitstandRows:178`, `ApplyLeitstandColumnFilter:308`, ggf. Hilfsmethode; `LeitstandOrderRow` behält das Feld, Quelle wechselt — siehe unten)
- Modify: `IdealAkeWms/Controllers/ProductionOrdersController.cs` (Projektion :151, `FaListComputedColumnKeys:449` um `"matchcode"`, AK-14-Zähler)
- Modify: `IdealAkeWms/Controllers/PickingLeitstandController.cs` (Matchcode aus dem Article-Join in die Zeilen füllen + AK-14-Zähler; nutzt `ProjectLeitstandRows`)
- Modify: `IdealAkeWms/Controllers/FaCompletionController.cs` (:125 + AK-14), `FaWorklistController.cs` (:225 + AK-14), `PickingController.cs` (:159, in-memory Dict + AK-14)
- Test: `IdealAkeWms.Tests/Repositories/ProductionOrderRepositoryTests.cs` (bestehende `ColumnFilter_Matchcode_*` auf die neue Quelle/den Postfilter umstellen), ggf. Controller-Tests

**Interfaces:**
- Consumes: `IArticleRepository.GetMatchcodesByArticleNumbersAsync` (Task 1)

**Vorgehen (Root-Cause-Muster, D1/D2):** Jede der fünf Listen materialisiert ihre (paginierte) Zeilen wie bisher, aber **ohne** `o.Matchcode`. Danach: `var nums = rows.Select(r => r.ArticleNumber).Where(n => !string.IsNullOrEmpty(n)).ToList();` → `var mc = await _articleRepository.GetMatchcodesByArticleNumbersAsync(nums);` → je Zeile `row.Matchcode = mc.GetValueOrDefault(row.ArticleNumber ?? "")` (null wenn Artikel fehlt ODER Matchcode leer). AK-14: `var fehlend = nums.Count(n => !mc.ContainsKey(n)); if (fehlend > 0) _logger.LogInformation("Matchcode-Join: {Fehlend} von {Gesamt} FA-Zeilen ohne Artikelstamm-Treffer ({Liste})", fehlend, nums.Count, "<ListenName>");` — genau **eine** Zahl je Aufbau.

- [ ] **Step 1: Failing test** — `ProductionOrderRepositoryTests`: ein Test, der beweist, dass eine FA-Zeile ihren Matchcode aus `Article` (per `ArticleNumber`) zieht und nicht aus `ProductionOrder` (das Feld ist nach Task 3 weg; hier als Postfilter/Join-Verhalten testen). Der Matchcode-**Spaltenfilter** wird jetzt über den ProductionOrders-Postfilter getestet (Controller-Ebene) statt `ApplyLeitstandColumnFilter` — bestehende `ColumnFilter_Matchcode_*` entsprechend anpassen/ersetzen.
- [ ] **Step 2: Run** → FAIL.
- [ ] **Step 3: Implementation**
  - `ProductionOrderRepository.ProjectLeitstandRows`: das `o.Matchcode`-Argument (:178) durch **kein** Feld ersetzen — `LeitstandOrderRow.Matchcode` wird nachträglich im Controller gefüllt. Zwei saubere Optionen (Implementer wählt die kleinere): (a) `LeitstandOrderRow` behält das Feld, Projektion setzt `null`, Controller füllt nach; (b) Konstruktorargument entfernen und Feld als settable Property. Bevorzugt (a) — kleinster Diff, `record`-Positional bleibt.
  - `ApplyLeitstandColumnFilter`: Case `"matchcode"` (:301-308) **entfernen**.
  - `ProductionOrdersController`: `"matchcode"` in `FaListComputedColumnKeys` (:449) aufnehmen; Projektion :151 nicht mehr `o.Matchcode`; nach Materialisierung Batch-Dict füllen + AK-14 loggen; der bestehende Postfilter-Loop (`computedFilters` auf `item.Matchcode`) filtert dann automatisch.
  - PickingLeitstand/FaCompletion/FaWorklist/Picking: Projektion ohne `o.Matchcode`, Batch-Dict-Nachfüllung + AK-14. Client-Filter-Dicts (`i => i.Matchcode`) bleiben.
  - `IProductionOrderRepository`/Controller ggf. `IArticleRepository` injizieren, falls noch nicht vorhanden.
- [ ] **Step 4: Run** `dotnet build` + `dotnet test IdealAkeWms.Tests` → grün. **AK 2 Vorher/Nachher:** im Test sicherstellen, dass eine Zeile mit vorhandenem Article denselben Matchcode zeigt wie zuvor, und eine Zeile ohne Article `null` (⇒ AK-14-Zähler > 0 im Log).
- [ ] **Step 5: Commit** `feat(matchcode): fünf FA-Listen lesen Matchcode aus Article-Join + AK-14-Fehltreffer-Zähler`

> Hinweis: `ProductionOrder.Matchcode` existiert nach Task 2 noch (Property), wird aber nirgends mehr gelesen. Der Build bleibt grün. Der physische Rückbau ist Task 3.

---

### Task 3: Rückbau `ProductionOrder.Matchcode` + Migrationen (remove → add) + SQL + FreshInstall

**Files:**
- Modify: `IdealAkeWms/Models/ProductionOrder.cs` (Zeilen 43-45 entfernen)
- Modify: `IDEALAKEWMSService/Services/FaMaterializationSyncService.cs` (Zeilen ~176 + ~200 `Matchcode =` entfernen)
- Remove: `IdealAkeWms/Migrations/20260910081155_AddProductionOrderMatchcode.cs` + `.Designer.cs` (via `dotnet ef migrations remove`)
- Create: `IdealAkeWms/Migrations/<neu>_AddArticleMatchcode.cs` (+ Designer) via `dotnet ef migrations add`
- Remove: `SQL/91_AddProductionOrderMatchcode.sql`; Create: `SQL/91_AddArticleMatchcode.sql`
- Modify: `SQL/00_FreshInstall.sql` (vier Änderungen, siehe unten)

- [ ] **Step 1: Verify + remove** `dotnet ef migrations list --project IdealAkeWms` → bestätigen, dass `AddProductionOrderMatchcode` die letzte ist. Property `ProductionOrder.Matchcode` (43-45) und die zwei `FaMaterializationSyncService`-Zeilen entfernen. Dann `dotnet ef migrations remove --project IdealAkeWms` (liest nur `__EFMigrationsHistory`, ein harmloser Read gegen AKESQL20; die Migration ist dort NICHT angewendet → sauberes Entfernen. Meldet EF Verbindungsprobleme, ist die Nicht-Anwendung bereits verifiziert → `--force` ist dann sicher). Entfernt Migration+Designer und revertiert den Snapshot.
- [ ] **Step 2: add** `dotnet ef migrations add AddArticleMatchcode --project IdealAkeWms` → additive `AddColumn Matchcode NVARCHAR(200) NULL` auf `Articles`. Prüfen, dass die Migration NUR Article.Matchcode enthält (kein ProductionOrder-Rest).
- [ ] **Step 3: SQL** `SQL/91_AddProductionOrderMatchcode.sql` löschen. `SQL/91_AddArticleMatchcode.sql` neu (Muster 1:1 aus der gelöschten Datei, Tabelle → `dbo.Articles`, Guard `IF COL_LENGTH('dbo.Articles','Matchcode') IS NULL … ALTER TABLE … ADD [Matchcode] NVARCHAR(200) NULL;` DDL in eigenem Batch, `__EFMigrationsHistory`-Insert für die neue MigrationId in separatem Batch). Nummer 91 ist frei (die ProductionOrder-91 war nie deployt); falls ein anderer Branch inzwischen 91 belegt, auf 92 zählen.
- [ ] **Step 4: FreshInstall** `SQL/00_FreshInstall.sql`: (1) `[Matchcode] NVARCHAR(200) NULL` aus dem `ProductionOrders`-CREATE-Block entfernen; (2) `[Matchcode] NVARCHAR(200) NULL` in den `Articles`-CREATE-Block ergänzen; (3) History-Insert-Zeile `20260910081155_AddProductionOrderMatchcode` entfernen; (4) neue History-Insert-Zeile für `<neu>_AddArticleMatchcode`.
- [ ] **Step 5: Run** `dotnet build IdealAkeWms.slnx` 0 Fehler; `dotnet test` (beide Projekte) grün. Bestätigen: kein Code referenziert `ProductionOrder.Matchcode` mehr (`grep -rn "\.Matchcode" IdealAkeWms IDEALAKEWMSService` prüfen — nur `Article`/`FaHierarchyNode`/`item.Matchcode`(ViewModel) dürfen bleiben).
- [ ] **Step 6: Commit** `refactor(matchcode): ProductionOrder.Matchcode zurückgebaut; Migration AddProductionOrderMatchcode entfernt, AddArticleMatchcode neu`

---

### Task 4: Sage-Artikel-Sync um Matchcode erweitern

**Files:**
- Modify: `IDEALAKEWMSService/Services/SageImportService.cs` (`SyncArticlesAsync`: Query :382-421, Reader-Tupel :423ff., Upsert-SQL :480-515 inkl. Änderungserkennung)
- Test: `IDEALAKEWMSService.Tests/Services/…` (soweit ohne echte Sage testbar — Upsert-Change-Detection/Feldzuweisung; der SQL-Read gegen Sage bleibt Manual-UAT)

- [ ] **Step 1:** In beide `UNION`-Zweige der `sageSql` `CAST(a.Matchcode AS nvarchar(200)) AS Matchcode` ergänzen; im äußeren `SELECT` `MAX(Matchcode) AS Matchcode` (Artikel-Matchcode ist je Artikel identisch, `MAX` wählt nichts aus). Reader-Tupel + Zuweisung um `Matchcode` erweitern (`reader.IsDBNull(n) ? null : reader.GetString(n)`).
- [ ] **Step 2:** Upsert-SQL: `[Matchcode]` in INSERT-Spaltenliste + VALUES, in die UPDATE-SET-Liste, und in die `ISNULL(...) != ISNULL(...)`-Änderungserkennungskette (sonst wird eine reine Matchcode-Änderung nicht erkannt). Audit-Felder wie gehabt (kein neues Muster).
- [ ] **Step 3:** Test für die Change-Detection/Zuweisung soweit möglich; `dotnet build` + `dotnet test` grün.
- [ ] **Step 4: Commit** `feat(matchcode): Article-Sync liest KHKArtikel.Matchcode (CAST nvarchar(200), MAX je Artikel)`

---

### Task 5: Artikelstammliste — Suche, Spaltenfilter, Views, Artikelinfo-Fallback

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/ArticleRepository.cs` (`SearchAsync:22-23`, `GetPaginatedAsync:40-41`, `ApplyArticleColumnFilter:62-85` neuer `"matchcode"`-Case)
- Modify: `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` (Artikelstamm-`ViewConfig` — viewKey am `data-view-key` in `Views/Articles/Index.cshtml` verifizieren — neue `ColumnDef("matchcode","Matchcode", Locked:false)` neben `article-number`/`description`)
- Modify: `IdealAkeWms/Views/Articles/Index.cshtml` (Spalte + `#column-config`-Eintrag + `<th data-col-key="matchcode">` + `<td>` an identischer Position — Fallstricke §3), `Info.cshtml` (Matchcode-Anzeige, leer/„—" wenn null), `Edit.cshtml` (Matchcode-Feld analog `Description`)
- Modify: `IdealAkeWms/Controllers/ArticlesController.cs` (`Info:218-228` — Exakt-Matchcode-Fallback, D4; `Edit`-POST bindet Matchcode + Audit)
- Test: `IdealAkeWms.Tests/Repositories/ArticleRepositoryTests.cs` (Suche/Filter über Matchcode), ggf. Controller-Test für den Info-Fallback

- [ ] **Step 1: Failing tests** — Freitextsuche findet über Matchcode-Teilstring; Spaltenfilter-Case `"matchcode"` (analog `"description"`, null-sicher, `EF.Functions.Like` wie die Nachbarn); Info-Fallback: exakter Matchcode-Treffer wenn ArticleNumber nicht trifft, kein Teilstring.
- [ ] **Step 2: Run** → FAIL.
- [ ] **Step 3: Implementation**
  - `SearchAsync`/`GetPaginatedAsync`: `|| (a.Matchcode != null && a.Matchcode.Contains(query/search))` ergänzen.
  - `ApplyArticleColumnFilter`: neuer Case analog `"description"` (mit `EF.Functions.Like`, wie die anderen Cases dort — Konsistenz vor F7 an dieser Stelle; die Article-Filter nutzen bereits durchgängig `EF.Functions.Like`).
  - `ArticlesController.Info`: nach dem `GetByArticleNumberAsync`-Miss (vor dem NotFound) `GetByMatchcodeExactAsync(articleNumber.Trim())` versuchen; Treffer → normal weiter. `Edit`-POST: `article.Matchcode = model.Matchcode` in die bestehende Zuweisung, Audit unverändert.
  - Views: Index Spalte an gleicher Position in `<thead>`/`#column-config`/`<td>` (§3); Info Zeile „Matchcode"; Edit Eingabefeld.
- [ ] **Step 4: Run** `dotnet build` + `dotnet test IdealAkeWms.Tests` grün.
- [ ] **Step 5: Commit** `feat(matchcode): Artikelstamm — Suche, Spaltenfilter, Views, Info-Exakt-Fallback`

---

### Task 6: Übrige Suchstellen — Bestand, Fehlteile, Bedarfsmeldungen, FA-Fertigmeldung-Freitext

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/StockMovementRepository.cs` (Freitext :29-30, :89-90, :322-323 + Spaltenfilter :558-561: `sm.Article.Matchcode` ergänzen — `Article` ist per Navigation schon geladen)
- Modify: `IdealAkeWms/Data/Repositories/PartRequisitionRepository.cs` (:80-81: Snapshot-Felder `ArticleNumber`/`ArticleDescription` — Matchcode-Suche über zusätzlichen Join/Subquery auf `Article.ArticleNumber`, KEIN neues Snapshot-Feld)
- Modify: `IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs` (`ApplyMissingPartsTextFilter:400-462`: gleiches Muster wie PartRequisition — Join/Subquery auf `Article`)
- Modify: `IdealAkeWms/Controllers/FaCompletionController.cs` (Freitextfilter ~88-92: den `ProductionOrder.ArticleNumber`-Freitext um den Article-Matchcode als ODER-Bedingung erweitern — über denselben Article-Join wie Task 2)
- Test: je Repository ein Suchtest (InMemory), soweit die Struktur es zulässt

- [ ] **Step 1: Failing tests** — je Fundstelle: Matchcode-Teilstring findet die erwartete Zeile; leerer Matchcode = kein Treffer, keine Exception; Nicht-Treffer-String → leere Liste.
- [ ] **Step 2: Run** → FAIL.
- [ ] **Step 3: Implementation** — StockMovement: triviales OR-Glied (`sm.Article` geladen). PartRequisition/WarehouseRequisition: Matchcode über `_context.Set<Article>().Any(a => a.ArticleNumber == r.ArticleNumber && a.Matchcode != null && a.Matchcode.Contains(term))` als zusätzliche ODER-Bedingung neben den Snapshot-Feldern (InMemory-tauglich: `.Any(...)` mit Contains, kein `EF.Functions.Like`, kein nested `tokens.Any`-Lambda — Token-Schleife wie im bestehenden Code). FaCompletion: Article-Join-Bedingung analog.
- [ ] **Step 4: Run** grün.
- [ ] **Step 5: Commit** `feat(matchcode): Suche über Matchcode in Bestand, Fehlteilen, Bedarfsmeldungen, FA-Fertigmeldung`

---

### Task 7: AK 15 — Artikel-Typeahead erst ab 3 Zeichen + Entprellung

**Files:**
- Modify: `IdealAkeWms/Views/Shared/_Select2ArticlePartial.cshtml` (`minimumInputLength: 0` → `3`; `delay` ~300 belassen)
- Modify: `IdealAkeWms/Views/StockMovements/InboundBulk.cshtml` (Article-Select2: `minimumInputLength: 3`)
- Modify: `IdealAkeWms/Views/WarehouseRequisitions/Edit.cshtml` (custom fetch bei `#article-search`: erst ab `q.trim().length >= 3` feuern, ~300 ms `setTimeout`-Entprellung falls nicht vorhanden)
- Test: Quelltext-Wächter (kein JS-Harness) — `IdealAkeWms.Tests/Helpers/ArticleSearchMinLengthSourceTests.cs`: die drei Dateien enthalten die Article-`/api/articles/search`-Suche NUR mit `minimumInputLength: 3` bzw. der Längen-Guard-Bedingung; kein `minimumInputLength: 0` an einem Article-Search-Widget.

- [ ] **Step 1:** Verifikation im Brief bestätigt (A3): die drei Widgets feuern je Tastendruck. Failing Quelltext-Wächter schreiben.
- [ ] **Step 2: Run** → FAIL.
- [ ] **Step 3:** `minimumInputLength: 3` setzen; WarehouseRequisitions/Edit `if (q.length < 3) { results.innerHTML=''; return; }` + Entprellung. `_Select2ProductionOrderPartial` und `Picking/Bom.cshtml` (BOM-/Auftragssuche) NICHT anfassen — nicht Artikel-Matchcode-Scope.
- [ ] **Step 4: Run** grün.
- [ ] **Step 5: Commit** `feat(matchcode): Artikel-Typeahead erst ab 3 Zeichen (AK 15, 108k-Zeilen-Scan pro Buchstabe vermeiden)`

---

### Task 8: Version 1.40.0, Anwender-Changelog, README, Testszenarien TS-75, Test-Konsolidierung

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs` + `IDEALAKEWMSService/AppVersion.cs` → `"1.40.0"` (Datum 2026-09-15)
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml` (Karte v1.40.0 VOR v1.39.0, identisches Markup)
- Modify: `README.md` (falls AppSettings/Betrieb betroffen — hier v. a. Hinweis, dass der Matchcode bei AKE erst nach einem Artikel-Sync-Lauf erscheint)
- Modify: `docs/TESTSZENARIEN.md` (Kapitel `## TS-75 — …`, Muster TS-74; die 9 Spec-Szenarien voll ausformulieren: Vorbedingung/Schritte/Erwartung/Negativfall + Manual-UAT-Marker)
- Verify: die in Task 3 entfernten/umgestellten Tests (`ProductionOrderRepositoryTests.ColumnFilter_Matchcode_*`, `FaMaterializationSyncServiceTests` Matchcode) sind konsistent grün

- [ ] **Step 1:** Versionen + Changelog-Karte (Anwendersprache): Matchcode hausweit suchbar (Artikelstamm, Bestand, Fehlteile, Bedarfsmeldungen, FA-Listen); bei AKE erst nach Sync-Lauf befüllt (kein Fehler wenn leer); Artikelinfo findet auch per Matchcode (exakt).
- [ ] **Step 2:** TS-75.1–75.9 aus der Spec-Skizze voll ausformulieren, inkl. AK-2-Vorher/Nachher-Regression der fünf Listen, AK-9/10 leerer Matchcode, AK-8 Migrations-Idempotenz, AK-15 Typeahead-Mindestlänge, AK-14 Fehltreffer-Log (Manual-UAT: Log prüfen). `secondbrain/tests/testszenarien-index.md` zieht der Controller im Hauptcheckout nach — hier nur `docs/TESTSZENARIEN.md`.
- [ ] **Step 3: Run** `dotnet build IdealAkeWms.slnx` 0 Fehler; `dotnet test IdealAkeWms.Tests` und `dotnet test IDEALAKEWMSService.Tests` grün — beide Zahlen in den Report.
- [ ] **Step 4: Commit** `chore: v1.40.0 — Anwender-Changelog, README, Testszenarien TS-75`

---

## Self-Review (Spec-Abdeckung)

| AK | Task |
|---|---|
| 1 (Article.Matchcode additiv, idempotent) | 1, 3 |
| 2 (Vorher/Nachher, keine Zeile kippt) | 2 (Test) |
| 3 (FA-Listen-Spaltenfilter unverändert, neue Quelle) | 2 |
| 4 (Artikelstammliste Spalte+Filter+Freitext) | 5 |
| 5 (Artikelinfo zeigt Matchcode) | 5 |
| 6 (Bestand/Bewegung) | 6 |
| 7 (Fehlteile/Bestellung) | 6 |
| 8 (Bedarfsmeldungen) | 6 |
| 9,10 (leerer Matchcode kein Fehler/AKE-Regression) | 1,5,6 (null-sicher) |
| 11 (Sync inkl. Audit) | 4 |
| 12 (OSEON/BOM unverändert kein Matchcode) | out of scope, im UAT-Vermerk |
| 13 (build+test grün) | alle |
| 14 (Fehltreffer-Zähler im Log) | 2 |
| 15 (Typeahead ab 3 Zeichen + Entprellung) | 7 |
| 16 (Feldlänge 200 = CAST 200) | 3, 4 |

Brain (Hauptcheckout, durch den Controller): Spec-Status/Deploy/QA, Umsetzungsnotiz, `codebase/*`, `feature-map.md`, `tests/testszenarien-index.md`, Changelog v1.40.0, `fallstricke.md` (A1 BuildExtraInfoOrContains-Grenze; Baum↔Listen-Inkonsistenz H-2), Backlog-Kandidaten (OSEON-Matchcode, FA-Baum auf Artikelstamm umstellen, Artikel-Sync um gefertigte Artikel erweitern falls AK-14-Zähler > 0).
