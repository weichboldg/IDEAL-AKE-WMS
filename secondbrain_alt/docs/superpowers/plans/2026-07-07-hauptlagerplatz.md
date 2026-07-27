# Hauptlagerplatz (PrimaryStorageLocation) — Implementierungsplan

> **For agentic workers:** Execute tasks in order. Each task is bite-sized and ends with a green build + a commit. Follow the TDD loop: write the failing test, run it and see it fail, implement, run it and see it pass, then commit. Do NOT skip the "see it fail" step. All paths are absolute. All commands run from the worktree root unless stated otherwise. Do NOT bump `AppVersion.cs` — this feature is folded into the unreleased v1.25.0.

**Goal:** Artikel bekommen einen **Hauptlagerplatz**. Der Wert kommt primär aus Sage (`KHKLagerplaetze.Kurzbezeichnung` via `KHKArtikel.PlatzID`); liefert Sage keinen, ist er in der App manuell setzbar. Überall wo Bestand pro Lagerplatz angezeigt wird, steht der Hauptlagerplatz je Artikel zuerst; in der Bestandsübersicht trägt er ein ⭐-Badge.

**Architecture:** ASP.NET Core 10.0 MVC + Repository Pattern + EF Core 10.0 (SQL Server). Windows-Service (`IDEALAKEWMSService`) macht den Sage-Sync über raw ADO.NET. Das Feature ist rein additiv: zwei nullable Spalten an `Articles` (`PrimaryStorageLocationId` FK → `StorageLocations` SetNull, `SagePrimaryStorageLocation` nvarchar(100)). Die Sortier-Priorisierung sitzt zentral in zwei Repo-Methoden (`GetCurrentStockAsync`, `GetStockByArticleNumbersAsync`), sodass ~9 Anzeige-Stellen sie automatisch erben; drei Stellen, die danach nach Menge umsortieren, ziehen "primary first" als oberste Ordnung ein.

**Tech Stack:** C# / .NET 10, EF Core 10, xUnit + FluentAssertions + Moq + EF InMemory, Bootstrap 5, Razor.

**Spec:** `docs/superpowers/specs/2026-07-07-hauptlagerplatz-design.md` (verbindlich).

**Worktree:** `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\glas-bestellung` (Branch `feature/glas-bestellung`, HEAD @ `44d1f21` oder neuer).

**Konventionen für alle Commands:**
- Build: `dotnet build IdealAkeWms.slnx` (die Solution-Datei ist `.slnx`, NICHT `.sln`).
- Web-Tests: `dotnet test IdealAkeWms.Tests --nologo`
- Service-Tests: `dotnet test IDEALAKEWMSService.Tests --nologo`
- Git im Worktree via Bash: `cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git ...`
- Jeder Commit endet mit einer Leerzeile + `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

---

## Task 0 — Pre-Flight: Baseline grün

**Files:**
- (nur lesen/ausführen, keine Änderungen)

**Steps:**

- [ ] 1. HEAD und Branch bestätigen:
  ```
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git log --oneline -1 && git rev-parse --abbrev-ref HEAD
  ```
  Erwartete Ausgabe (SHA darf neuer sein):
  ```
  44d1f21 docs(spec): Hauptlagerplatz + FA-Reconciliation (v1.25.0)
  feature/glas-bestellung
  ```

- [ ] 2. Build grün:
  ```
  dotnet build IdealAkeWms.slnx
  ```
  Erwartete Ausgabe endet mit `Build succeeded` und `0 Error(s)` (Warnings sind ok).

- [ ] 3. Web-Tests grün:
  ```
  dotnet test IdealAkeWms.Tests --nologo
  ```
  Erwartete Ausgabe endet mit `Passed!  - Failed:     0` (Anzahl bestandener Tests egal, muss ~839 sein).

- [ ] 4. Service-Tests grün:
  ```
  dotnet test IDEALAKEWMSService.Tests --nologo
  ```
  Erwartete Ausgabe endet mit `Passed!  - Failed:     0` (~115 Tests).

- [ ] 5. Kein Commit (nichts geändert). Wenn ein Schritt rot ist: STOPP und melde das an den User, bevor du weitermachst.

---

## Task 1 — Model + EF-Config: PrimaryStorageLocation-Felder

**Files:**
- Modify: `IdealAkeWms/Models/Article.cs` (nach Z. 29, `ArticleCategory`-Nav)
- Modify: `IdealAkeWms/Data/ApplicationDbContext.cs` (Article-Config, Z. 200–223)

**Steps:**

- [ ] 1. In `IdealAkeWms/Models/Article.cs` nach der `ArticleCategory`-Navigation (aktuell Z. 29, direkt vor der `StockMovements`-Collection) die drei Felder ergänzen:
  ```csharp
      [Display(Name = "Kategorie")]
      public int? ArticleCategoryId { get; set; }
      public ArticleCategory? ArticleCategory { get; set; }

      [Display(Name = "Hauptlagerplatz")]
      public int? PrimaryStorageLocationId { get; set; }
      public StorageLocation? PrimaryStorageLocation { get; set; }

      /// <summary>Sage-Rohcode (KHKLagerplaetze.Kurzbezeichnung). Nicht leer ⇒ aus Sage ⇒ in der App gesperrt.</summary>
      [StringLength(100)]
      public string? SagePrimaryStorageLocation { get; set; }

      public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
  ```
  (Der `ICollection<StockMovement>`-Block existiert bereits — nur die drei neuen Felder werden davor eingefügt; die vorhandene Collection nicht duplizieren.)

- [ ] 2. In `IdealAkeWms/Data/ApplicationDbContext.cs` im `modelBuilder.Entity<Article>`-Block (Z. 201–223) direkt nach der bestehenden `entity.HasOne(e => e.ArticleCategory)…`-Konfiguration (endet Z. 222 mit `.OnDelete(DeleteBehavior.SetNull);`) ergänzen:
  ```csharp
              entity.HasOne(e => e.ArticleCategory)
                  .WithMany(c => c.Articles)
                  .HasForeignKey(e => e.ArticleCategoryId)
                  .OnDelete(DeleteBehavior.SetNull);

              entity.Property(e => e.SagePrimaryStorageLocation).HasMaxLength(100);

              entity.HasIndex(e => e.PrimaryStorageLocationId);

              entity.HasOne(e => e.PrimaryStorageLocation)
                  .WithMany()
                  .HasForeignKey(e => e.PrimaryStorageLocationId)
                  .OnDelete(DeleteBehavior.SetNull);
          });
  ```
  (Die schließende `});` ersetzt die bestehende Z. 223 — nicht doppeln.)

- [ ] 3. Build grün:
  ```
  dotnet build IdealAkeWms.slnx
  ```
  Erwartete Ausgabe: `Build succeeded`, `0 Error(s)`.

- [ ] 4. Committen:
  ```
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IdealAkeWms/Models/Article.cs IdealAkeWms/Data/ApplicationDbContext.cs && git commit -m "$(cat <<'EOF'
feat(article): PrimaryStorageLocation-Felder + EF-Config (Hauptlagerplatz)

Article bekommt PrimaryStorageLocationId (FK -> StorageLocations, SetNull),
Navigation PrimaryStorageLocation und SagePrimaryStorageLocation (nvarchar(100),
Sage-Rohcode). Additiv, nullable.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
  ```

---

## Task 2 — Migration 79 + SQL/79 + FreshInstall

**Files:**
- Create: `IdealAkeWms/Migrations/<timestamp>_AddArticlePrimaryStorageLocation.cs` (via `dotnet ef`)
- Create: `SQL/79_AddArticlePrimaryStorageLocation.sql`
- Modify: `SQL/00_FreshInstall.sql` (Article-Schema-Block + History-Insert-Block)

**Steps:**

- [ ] 1. Migration generieren:
  ```
  dotnet ef migrations add AddArticlePrimaryStorageLocation --project IdealAkeWms
  ```
  Erwartete Ausgabe: `Build started...` / `Build succeeded.` / `Done. To undo this action, use 'ef migrations remove'`. Notiere den generierten Timestamp-Prefix (Format `20260707HHMMSS_AddArticlePrimaryStorageLocation`). Merke ihn dir als `<MIGID>` für die folgenden Schritte.

- [ ] 2. Prüfen, dass die Migration die zwei Spalten + Index + FK anlegt (Sichtprüfung):
  ```
  cat IdealAkeWms/Migrations/*_AddArticlePrimaryStorageLocation.cs
  ```
  Erwartet: `AddColumn<int>(name: "PrimaryStorageLocationId", …)`, `AddColumn<string>(name: "SagePrimaryStorageLocation", … maxLength: 100 …)`, `CreateIndex(name: "IX_Articles_PrimaryStorageLocationId", …)`, `AddForeignKey(… "FK_Articles_StorageLocations_PrimaryStorageLocationId" … onDelete: ReferentialAction.SetNull)`. Falls die genauen Namen abweichen, in Schritt 4/5 die tatsächlichen Namen aus dieser Datei übernehmen.

- [ ] 3. `has-pending` muss leer sein:
  ```
  dotnet ef migrations has-pending-model-changes --project IdealAkeWms
  ```
  Erwartete Ausgabe: `No changes have been made to the model since the last migration.`

- [ ] 4. Idempotentes SQL-Skript `SQL/79_AddArticlePrimaryStorageLocation.sql` anlegen (ersetze `<MIGID>` durch den echten Timestamp aus Schritt 1):
  ```sql
  -- SQL/79_AddArticlePrimaryStorageLocation.sql
  -- Migration 79: AddArticlePrimaryStorageLocation (v1.25.0)
  -- Hauptlagerplatz am Artikel: PrimaryStorageLocationId (FK -> StorageLocations, SetNull)
  -- + SagePrimaryStorageLocation (Sage-Rohcode). Additiv, idempotent.
  SET NOCOUNT ON;
  GO

  IF COL_LENGTH('dbo.Articles', 'PrimaryStorageLocationId') IS NULL
  BEGIN
      ALTER TABLE dbo.Articles ADD PrimaryStorageLocationId INT NULL;
      PRINT 'Spalte [Articles].[PrimaryStorageLocationId] erstellt.';
  END
  GO

  IF COL_LENGTH('dbo.Articles', 'SagePrimaryStorageLocation') IS NULL
  BEGIN
      ALTER TABLE dbo.Articles ADD SagePrimaryStorageLocation NVARCHAR(100) NULL;
      PRINT 'Spalte [Articles].[SagePrimaryStorageLocation] erstellt.';
  END
  GO

  IF NOT EXISTS (SELECT 1 FROM sys.indexes
      WHERE name = 'IX_Articles_PrimaryStorageLocationId'
        AND object_id = OBJECT_ID('dbo.Articles'))
  BEGIN
      CREATE INDEX IX_Articles_PrimaryStorageLocationId
          ON dbo.Articles(PrimaryStorageLocationId);
      PRINT 'Index IX_Articles_PrimaryStorageLocationId erstellt.';
  END
  GO

  IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys
      WHERE name = 'FK_Articles_StorageLocations_PrimaryStorageLocationId'
        AND parent_object_id = OBJECT_ID('dbo.Articles'))
  BEGIN
      ALTER TABLE dbo.Articles
          ADD CONSTRAINT FK_Articles_StorageLocations_PrimaryStorageLocationId
          FOREIGN KEY (PrimaryStorageLocationId)
          REFERENCES dbo.StorageLocations(Id) ON DELETE SET NULL;
      PRINT 'FK FK_Articles_StorageLocations_PrimaryStorageLocationId erstellt.';
  END
  GO

  IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
      WHERE MigrationId = '<MIGID>_AddArticlePrimaryStorageLocation')
  BEGIN
      INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
      VALUES ('<MIGID>_AddArticlePrimaryStorageLocation', '10.0.2');
  END
  GO
  ```

- [ ] 5. `SQL/00_FreshInstall.sql` an ZWEI Stellen erweitern.

  **(a) Article-Schema.** Der `CREATE TABLE [dbo].[Articles]`-Block liegt bei Z. 137–157. Ergänze die zwei Spalten in der Spaltenliste (vor der `CONSTRAINT [PK_Articles]`-Zeile, aktuell Z. 152):
  ```sql
          [Description]       NVARCHAR(500)     NULL,
          [Unit]              NVARCHAR(20)      NULL,
          [ReorderLevel]      DECIMAL(18,3)     NULL,
          [ArticleGroup]      NVARCHAR(100)     NULL,
          [PrimaryStorageLocationId]   INT           NULL,
          [SagePrimaryStorageLocation] NVARCHAR(100) NULL,
          [CreatedAt]         DATETIME2         NOT NULL DEFAULT GETDATE(),
  ```
  Dann NACH dem `ArticleCategoryId`-Guard-Block (aktuell Z. 1181–1191, endet mit `END` / `GO`) einen neuen idempotenten Guard-Block für Index + FK einfügen (die Spalten sind im `CREATE TABLE` schon enthalten; der Guard fängt bestehende DBs ab, bei denen die Spalten noch fehlen könnten und legt Index+FK an):
  ```sql
  IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Articles') AND name = 'PrimaryStorageLocationId')
  BEGIN
      ALTER TABLE [dbo].[Articles] ADD [PrimaryStorageLocationId] INT NULL;
      ALTER TABLE [dbo].[Articles] ADD [SagePrimaryStorageLocation] NVARCHAR(100) NULL;
      PRINT 'Spalten [Articles].[PrimaryStorageLocationId]/[SagePrimaryStorageLocation] erstellt.';
  END
  GO

  IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Articles_PrimaryStorageLocationId' AND object_id = OBJECT_ID('dbo.Articles'))
  BEGIN
      CREATE NONCLUSTERED INDEX [IX_Articles_PrimaryStorageLocationId] ON [dbo].[Articles] ([PrimaryStorageLocationId]);
  END
  GO

  IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Articles_StorageLocations_PrimaryStorageLocationId' AND parent_object_id = OBJECT_ID('dbo.Articles'))
  BEGIN
      ALTER TABLE [dbo].[Articles] ADD CONSTRAINT [FK_Articles_StorageLocations_PrimaryStorageLocationId]
          FOREIGN KEY ([PrimaryStorageLocationId]) REFERENCES [dbo].[StorageLocations]([Id]) ON DELETE SET NULL;
      PRINT 'FK [FK_Articles_StorageLocations_PrimaryStorageLocationId] erstellt.';
  END
  GO
  ```

  **(b) History-Insert.** Im `__EFMigrationsHistory`-Block am Dateiende (nach der `20260707113155_AddStockReadRole`-Zeile, aktuell Z. 2080–2082, vor dem `GO` / `PRINT 'EF Migrations History initialisiert.'`) einfügen (ersetze `<MIGID>`):
  ```sql
  IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '<MIGID>_AddArticlePrimaryStorageLocation')
      INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('<MIGID>_AddArticlePrimaryStorageLocation', '10.0.2');
  ```

- [ ] 6. Build grün + `has-pending` erneut leer:
  ```
  dotnet build IdealAkeWms.slnx && dotnet ef migrations has-pending-model-changes --project IdealAkeWms
  ```
  Erwartet: `Build succeeded`, `0 Error(s)` und `No changes have been made to the model since the last migration.`

- [ ] 7. Committen:
  ```
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IdealAkeWms/Migrations SQL/79_AddArticlePrimaryStorageLocation.sql SQL/00_FreshInstall.sql && git commit -m "$(cat <<'EOF'
feat(db): Migration 79 AddArticlePrimaryStorageLocation + FreshInstall

Zwei nullable Spalten an Articles (PrimaryStorageLocationId FK SetNull,
SagePrimaryStorageLocation nvarchar(100)) + Index + FK. Idempotentes SQL/79
mit COL_LENGTH/OBJECT_ID-Guards, History-Insert im separaten Batch.
FreshInstall an beiden Stellen synchron (Schema + History).

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
  ```

---

## Task 3 — SageImportService: Sync des Hauptlagerplatzes (TDD Helper)

**Files:**
- Modify: `IDEALAKEWMSService/Services/SageImportHelpers.cs` (neuer Helper `NormalizeLocationCode`)
- Modify: `IDEALAKEWMSService.Tests/Services/SageImportServiceTests.cs` (neue Theory)
- Modify: `IDEALAKEWMSService/Services/SageImportService.cs` (`SyncArticlesAsync`, SQL Z. 234–268, Reader Z. 270–289, Upsert Z. 311–357)

**Steps:**

- [ ] 1. **Failing test zuerst.** In `IDEALAKEWMSService.Tests/Services/SageImportServiceTests.cs` nach der `ParseReorderLevel_handles_edge_cases`-Theory (endet Z. 75) eine neue Theory für den Normalisierungs-Helper einfügen:
  ```csharp
      [Theory]
      [InlineData(null,          null)]
      [InlineData("",            null)]
      [InlineData("   ",         null)]
      [InlineData("A-01",        "A-01")]
      [InlineData("  A-01  ",    "A-01")]
      public void NormalizeLocationCode_trims_and_nulls_empty(string? raw, string? expected)
      {
          SageImportHelpers.NormalizeLocationCode(raw).Should().Be(expected);
      }
  ```

- [ ] 2. Test läuft rot (Helper existiert noch nicht → Compile-Fehler ist ein akzeptables "fail"):
  ```
  dotnet test IDEALAKEWMSService.Tests --nologo
  ```
  Erwartet: Build-Fehler `'SageImportHelpers' does not contain a definition for 'NormalizeLocationCode'` ODER (wenn du zuerst nur die Signatur anlegst) `Failed`. Wichtig: er darf NICHT grün sein.

- [ ] 3. Helper in `IDEALAKEWMSService/Services/SageImportHelpers.cs` implementieren (nach `ParseReorderLevel`, vor der schließenden `}` der Klasse):
  ```csharp
      /// <summary>
      /// Normalisiert einen Sage-Lagerplatz-Rohcode (Kurzbezeichnung): Trim,
      /// leer/whitespace -> null. Case bleibt erhalten (Lookup ist separat case-insensitiv).
      /// </summary>
      internal static string? NormalizeLocationCode(string? raw)
      {
          if (string.IsNullOrWhiteSpace(raw)) return null;
          return raw.Trim();
      }
  ```

- [ ] 4. Test läuft grün:
  ```
  dotnet test IDEALAKEWMSService.Tests --nologo
  ```
  Erwartet: `Passed!  - Failed:     0`.

- [ ] 5. **SQL in `SyncArticlesAsync` erweitern.** In `IDEALAKEWMSService/Services/SageImportService.cs` den `const string sageSql`-Block (Z. 234–268) so ersetzen, dass beide UNION-Zweige `KHKLagerplaetze` joinen und `PrimaryStorageLocation` selektieren, und die äußere `GROUP BY`-Projektion die neue Spalte durchreicht:
  ```csharp
              const string sageSql = """
                  WITH RawArticles AS (
                      SELECT DISTINCT
                          CAST(r.Ressourcenummer AS nvarchar(100))   AS ArticleNumber,
                          CAST(a.Bezeichnung1 AS nvarchar(500))      AS Description,
                          CAST(a.Lagermengeneinheit AS nvarchar(20)) AS Unit,
                          CAST(a.Artikelgruppe AS nvarchar(100))     AS ArticleGroup,
                          CAST(v.Meldebestand AS nvarchar(20))       AS ReorderLevel,
                          CAST(lp.Kurzbezeichnung AS nvarchar(100))  AS PrimaryStorageLocation
                      FROM [dbo].[KHKPpsRessourcenPositionen] r
                      LEFT JOIN [dbo].[KHKArtikel] a ON a.Artikelnummer = r.Ressourcenummer
                      LEFT JOIN [dbo].[KHKArtikelvarianten] v ON a.Artikelnummer = v.Artikelnummer
                      LEFT JOIN [dbo].[KHKLagerplaetze] lp ON a.PlatzID = lp.PlatzID
                      WHERE r.Ressourcenummer IS NOT NULL AND r.Ressourcenummer != ''

                      UNION

                      SELECT
                          CAST(a.Artikelnummer AS nvarchar(100))     AS ArticleNumber,
                          CAST(a.Bezeichnung1 AS nvarchar(500))      AS Description,
                          CAST(a.Lagermengeneinheit AS nvarchar(20)) AS Unit,
                          CAST(a.Artikelgruppe AS nvarchar(100))     AS ArticleGroup,
                          CAST(v.Meldebestand AS nvarchar(20))       AS ReorderLevel,
                          CAST(lp.Kurzbezeichnung AS nvarchar(100))  AS PrimaryStorageLocation
                      FROM [dbo].[KHKArtikel] a
                      LEFT JOIN [dbo].[KHKArtikelvarianten] v ON a.Artikelnummer = v.Artikelnummer
                      LEFT JOIN [dbo].[KHKLagerplaetze] lp ON a.PlatzID = lp.PlatzID
                      WHERE a.IstBestellartikel = -1 AND a.Aktiv = -1
                  )
                  SELECT
                      ArticleNumber,
                      MAX(Description)  AS Description,
                      MAX(Unit)         AS Unit,
                      MAX(ArticleGroup) AS ArticleGroup,
                      MAX(ReorderLevel) AS ReorderLevel,
                      MAX(PrimaryStorageLocation) AS PrimaryStorageLocation
                  FROM RawArticles
                  WHERE ArticleNumber IS NOT NULL AND ArticleNumber != ''
                  GROUP BY ArticleNumber
                  """;
  ```

- [ ] 6. **Reader-Tupel + Lese-Logik erweitern.** Das Tupel-Typ (Z. 270) und den Reader-Block (Z. 277–289) ersetzen:
  ```csharp
              var sageArticles = new List<(string ArticleNumber, string? Description, string? Unit, string? ArticleGroup, decimal? ReorderLevel, string? PrimaryStorageLocation)>();

              await using (var sageConn = new SqlConnection(sageConnection))
              {
                  await sageConn.OpenAsync(ct);
                  await using var cmd = new SqlCommand(sageSql, sageConn);
                  cmd.CommandTimeout = 120;
                  await using var reader = await cmd.ExecuteReaderAsync(ct);
                  while (await reader.ReadAsync(ct))
                  {
                      string? reorderRaw = reader.IsDBNull(4) ? null : reader.GetString(4);
                      string? primaryRaw = reader.IsDBNull(5) ? null : reader.GetString(5);
                      sageArticles.Add((
                          ArticleNumber: reader.GetString(0),
                          Description:   reader.IsDBNull(1) ? null : reader.GetString(1),
                          Unit:          reader.IsDBNull(2) ? null : reader.GetString(2),
                          ArticleGroup:  reader.IsDBNull(3) ? null : reader.GetString(3),
                          ReorderLevel:  SageImportHelpers.ParseReorderLevel(reorderRaw),
                          PrimaryStorageLocation: SageImportHelpers.NormalizeLocationCode(primaryRaw)
                      ));
                  }
              }
  ```

- [ ] 7. **Code→StorageLocationId-Lookup einmal pro Lauf laden.** Direkt NACH dem `await wmsConn.OpenAsync(ct);` (aktuell Z. 307) und VOR der `foreach (var article in sageArticles)`-Schleife (Z. 309) einfügen. `int missingPrimaryLocation = 0;` als Counter deklarieren:
  ```csharp
              await using var wmsConn = new SqlConnection(wmsConnection);
              await wmsConn.OpenAsync(ct);

              // Code -> StorageLocationId einmal pro Lauf laden (case-insensitiv, getrimmt).
              var locationIdByCode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
              await using (var locCmd = new SqlCommand(
                  "SELECT [Id], [Code] FROM [dbo].[StorageLocations] WHERE [Code] IS NOT NULL", wmsConn))
              {
                  await using var locReader = await locCmd.ExecuteReaderAsync(ct);
                  while (await locReader.ReadAsync(ct))
                  {
                      var code = locReader.GetString(1).Trim();
                      if (code.Length > 0)
                          locationIdByCode[code] = locReader.GetInt32(0);
                  }
              }

              int inserted = 0, updated = 0, missingPrimaryLocation = 0;
  ```
  **Wichtig:** Die vorhandene Zeile `int inserted = 0, updated = 0;` (aktuell Z. 304) ENTFERNEN — sie wird durch die obige `int inserted = 0, updated = 0, missingPrimaryLocation = 0;` ersetzt. Die neue Deklaration steht NACH dem Lookup-Load, aber die `foreach`-Schleife nutzt sie danach — Reihenfolge passt, weil beide vor der Schleife stehen.

- [ ] 8. **Upsert-SQL + Parameter erweitern.** Im `foreach`-Body (Z. 311–357) den `upsertSql` um die zwei Spalten in UPDATE + INSERT ergänzen und den Change-Detection-`WHERE` um `PrimaryStorageLocationId`/`SagePrimaryStorageLocation` erweitern. Der Upsert setzt beide Spalten immer (der Aufrufer liefert die korrekten Werte je Fall). Ersetze den kompletten `const string upsertSql = """ … """;`-Block durch:
  ```csharp
                  const string upsertSql = """
                      IF EXISTS (SELECT 1 FROM [dbo].[Articles] WHERE [ArticleNumber] = @ArticleNumber)
                      BEGIN
                          UPDATE [dbo].[Articles] SET
                              [Description]                = @Description,
                              [Unit]                       = @Unit,
                              [ArticleGroup]               = @ArticleGroup,
                              [ReorderLevel]               = @ReorderLevel,
                              [SagePrimaryStorageLocation] = @SagePrimaryStorageLocation,
                              [PrimaryStorageLocationId]   = CASE WHEN @UpdatePrimaryId = 1 THEN @PrimaryStorageLocationId ELSE [PrimaryStorageLocationId] END,
                              [ModifiedAt]        = GETUTCDATE(),
                              [ModifiedBy]        = 'IDEALAKEWMSService',
                              [ModifiedByWindows] = SYSTEM_USER
                          WHERE [ArticleNumber] = @ArticleNumber
                            AND (
                                ISNULL([Description],'')   != ISNULL(@Description,'')   OR
                                ISNULL([Unit],'')          != ISNULL(@Unit,'')          OR
                                ISNULL([ArticleGroup],'')  != ISNULL(@ArticleGroup,'')  OR
                                ISNULL([ReorderLevel],-1)  != ISNULL(@ReorderLevel,-1)  OR
                                ISNULL([SagePrimaryStorageLocation],'') != ISNULL(@SagePrimaryStorageLocation,'') OR
                                (@UpdatePrimaryId = 1 AND ISNULL([PrimaryStorageLocationId],-1) != ISNULL(@PrimaryStorageLocationId,-1))
                            )
                          SELECT 0 AS IsInsert, @@ROWCOUNT AS Affected
                      END
                      ELSE
                      BEGIN
                          INSERT INTO [dbo].[Articles]
                              ([ArticleNumber],[Description],[Unit],[ArticleGroup],[ReorderLevel],
                               [PrimaryStorageLocationId],[SagePrimaryStorageLocation],
                               [CreatedAt],[CreatedBy],[CreatedByWindows])
                          VALUES (@ArticleNumber, @Description, @Unit, @ArticleGroup, @ReorderLevel,
                                  @PrimaryStorageLocationId, @SagePrimaryStorageLocation,
                                  GETUTCDATE(), 'IDEALAKEWMSService', SYSTEM_USER)
                          SELECT 1 AS IsInsert, 1 AS Affected
                      END
                      """;

                  // Upsert-Regeln (Spec §Upsert-Logik):
                  //  - Sage liefert Wert: SagePrimaryStorageLocation = code; PrimaryStorageLocationId = lookup[code]
                  //    oder null (kein Match -> Warn-Count). Immer aktualisieren (@UpdatePrimaryId = 1).
                  //  - Sage leer: SagePrimaryStorageLocation = null; PrimaryStorageLocationId NICHT anfassen
                  //    (bewahrt manuelle App-Wahl) -> @UpdatePrimaryId = 0.
                  string? sageCode = article.PrimaryStorageLocation;
                  int? primaryId = null;
                  int updatePrimaryId = 0;
                  if (sageCode is not null)
                  {
                      updatePrimaryId = 1;
                      if (locationIdByCode.TryGetValue(sageCode, out var foundId))
                      {
                          primaryId = foundId;
                      }
                      else
                      {
                          missingPrimaryLocation++;
                          _logger.LogWarning(
                              "Hauptlagerplatz '{Code}' fuer Artikel {ArticleNumber} nicht als WMS-Lagerplatz gefunden — FK bleibt leer.",
                              sageCode, article.ArticleNumber);
                      }
                  }

                  await using var cmd = new SqlCommand(upsertSql, wmsConn);
                  cmd.Parameters.AddWithValue("@ArticleNumber", article.ArticleNumber);
                  cmd.Parameters.AddWithValue("@Description",  (object?)article.Description  ?? DBNull.Value);
                  cmd.Parameters.AddWithValue("@Unit",         (object?)article.Unit         ?? DBNull.Value);
                  cmd.Parameters.AddWithValue("@ArticleGroup", (object?)article.ArticleGroup ?? DBNull.Value);
                  cmd.Parameters.AddWithValue("@ReorderLevel", (object?)article.ReorderLevel ?? DBNull.Value);
                  cmd.Parameters.AddWithValue("@SagePrimaryStorageLocation", (object?)sageCode ?? DBNull.Value);
                  cmd.Parameters.AddWithValue("@PrimaryStorageLocationId", (object?)primaryId ?? DBNull.Value);
                  cmd.Parameters.AddWithValue("@UpdatePrimaryId", updatePrimaryId);

                  await using var reader = await cmd.ExecuteReaderAsync(ct);
                  if (await reader.ReadAsync(ct))
                  {
                      var isInsert = reader.GetInt32(0) == 1;
                      var affected = reader.GetInt32(1);
                      if (isInsert) inserted++;
                      else if (affected > 0) updated++;
                  }
  ```
  (Das ersetzt den bisherigen `upsertSql`-Block UND den `cmd`/Parameter/Reader-Block ab Z. 311 bis Z. 356 — die alten `cmd.Parameters.AddWithValue`-Zeilen für Description/Unit/ArticleGroup/ReorderLevel entfallen, sie sind oben mit drin.)

- [ ] 9. **`hauptlagerplatz_fehlt`-Count ins Aktivitäts-Protokoll.** Den `run.FinishSuccessAsync`-Aufruf (Z. 361–366) um den Count erweitern:
  ```csharp
              _logger.LogInformation("Artikel-Sync abgeschlossen: {Inserted} neu, {Updated} aktualisiert, {Missing} ohne WMS-Hauptlagerplatz.", inserted, updated, missingPrimaryLocation);

              await run.FinishSuccessAsync(new Dictionary<string, int>
              {
                  ["gelesen"]              = sageArticles.Count,
                  ["neu"]                  = inserted,
                  ["aktualisiert"]         = updated,
                  ["hauptlagerplatz_fehlt"] = missingPrimaryLocation,
              }, ct: ct);
  ```

- [ ] 10. Build + Service-Tests grün (die vorhandenen `SyncArticlesAsync_writes_lifecycle_via_failure_path`- und `ParseReorderLevel`-Tests müssen weiter passen; die raw-SQL-Strecke ist Manual-UAT):
  ```
  dotnet build IdealAkeWms.slnx && dotnet test IDEALAKEWMSService.Tests --nologo
  ```
  Erwartet: `Build succeeded`, `0 Error(s)` und `Passed!  - Failed:     0`.

- [ ] 11. Committen:
  ```
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IDEALAKEWMSService/Services/SageImportService.cs IDEALAKEWMSService/Services/SageImportHelpers.cs IDEALAKEWMSService.Tests/Services/SageImportServiceTests.cs && git commit -m "$(cat <<'EOF'
feat(sync): Hauptlagerplatz im Artikel-Sync (Sage -> PrimaryStorageLocation)

SyncArticlesAsync joint KHKLagerplaetze und liest Kurzbezeichnung als
PrimaryStorageLocation. Code->StorageLocationId-Lookup einmal pro Lauf.
Sage-Wert -> Rohcode + FK-Lookup (kein Match: FK null + Warn-Log, Count
hauptlagerplatz_fehlt). Sage leer -> Rohcode null, FK unberuehrt (bewahrt
manuelle App-Wahl). Neuer pure Helper NormalizeLocationCode + Unit-Tests.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
  ```

---

## Task 4 — StockMovementRepository: "Hauptlagerplatz zuerst" + IsPrimary-Flag (TDD)

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/StockOverviewViewModel.cs` (`StockOverviewItem`, Z. 17–31)
- Modify: `IdealAkeWms/Models/ViewModels/BomViewModels.cs` (`StockLocationInfo`, Z. 18–23)
- Modify: `IdealAkeWms/Data/Repositories/StockMovementRepository.cs` (`GetCurrentStockAsync` Z. 13–170, `GetStockByArticleNumbersAsync` Z. 318–392)
- Modify: `IdealAkeWms.Tests/Repositories/StockMovementRepositoryTests.cs` (neue Tests)

**Steps:**

- [ ] 1. **ViewModels erweitern.** In `IdealAkeWms/Models/ViewModels/StockOverviewViewModel.cs` in `StockOverviewItem` (nach Z. 30 `StorageLocationIstBuchbar`) ergänzen:
  ```csharp
      public bool StorageLocationIstBuchbar { get; set; } = true;
      public bool IsPrimaryStorageLocation { get; set; }
  ```
  In `IdealAkeWms/Models/ViewModels/BomViewModels.cs` in `StockLocationInfo` (nach Z. 22 `StorageLocationId`) ergänzen:
  ```csharp
      public int StorageLocationId { get; set; }
      public bool IsPrimaryStorageLocation { get; set; }
  ```

- [ ] 2. **Failing tests zuerst.** In `IdealAkeWms.Tests/Repositories/StockMovementRepositoryTests.cs` einen Helper zum Setzen des Hauptlagerplatzes und drei Tests ergänzen. Zuerst — direkt nach `SeedBaseData` (Z. 10–40) — einen kleinen Helper hinzufügen:
  ```csharp
      private static void SetPrimaryLocation(Data.ApplicationDbContext ctx, Article article, StorageLocation loc)
      {
          article.PrimaryStorageLocationId = loc.Id;
          ctx.SaveChanges();
      }
  ```
  Dann am Dateiende (vor der schließenden `}` der Klasse) einfügen:
  ```csharp
      [Fact]
      public async Task GetCurrentStock_PrimaryLocation_SortedFirstPerArticle()
      {
          using var ctx = TestDbContextFactory.Create();
          var (article, loc1, loc2) = SeedBaseData(ctx);
          var repo = new StockMovementRepository(ctx);

          // L01 kommt alphabetisch vor L02; wir machen L02 zum Hauptlagerplatz.
          await repo.AddAsync(CreateMovement(article, loc1, 5, MovementType.Einbuchung));
          await repo.AddAsync(CreateMovement(article, loc2, 3, MovementType.Einbuchung));
          SetPrimaryLocation(ctx, article, loc2);

          var stock = await repo.GetCurrentStockAsync();

          stock.Should().HaveCount(2);
          stock[0].StorageLocationCode.Should().Be("L02");
          stock[0].IsPrimaryStorageLocation.Should().BeTrue();
          stock[1].StorageLocationCode.Should().Be("L01");
          stock[1].IsPrimaryStorageLocation.Should().BeFalse();
      }

      [Fact]
      public async Task GetCurrentStock_NoPrimaryLocation_SortedByCode()
      {
          using var ctx = TestDbContextFactory.Create();
          var (article, loc1, loc2) = SeedBaseData(ctx);
          var repo = new StockMovementRepository(ctx);

          await repo.AddAsync(CreateMovement(article, loc2, 3, MovementType.Einbuchung));
          await repo.AddAsync(CreateMovement(article, loc1, 5, MovementType.Einbuchung));

          var stock = await repo.GetCurrentStockAsync();

          stock.Should().HaveCount(2);
          stock[0].StorageLocationCode.Should().Be("L01");
          stock[1].StorageLocationCode.Should().Be("L02");
          stock.All(s => !s.IsPrimaryStorageLocation).Should().BeTrue();
      }

      [Fact]
      public async Task GetStockByArticleNumbers_PrimaryLocation_SortedFirstWithFlag()
      {
          using var ctx = TestDbContextFactory.Create();
          var (article, loc1, loc2) = SeedBaseData(ctx);
          var repo = new StockMovementRepository(ctx);

          await repo.AddAsync(CreateMovement(article, loc1, 5, MovementType.Einbuchung));
          await repo.AddAsync(CreateMovement(article, loc2, 3, MovementType.Einbuchung));
          SetPrimaryLocation(ctx, article, loc2);

          var result = await repo.GetStockByArticleNumbersAsync(new List<string> { "ART-001" });

          result.Should().ContainKey("ART-001");
          var locs = result["ART-001"];
          locs.Should().HaveCount(2);
          locs[0].Code.Should().Be("L02");
          locs[0].IsPrimaryStorageLocation.Should().BeTrue();
          locs[1].Code.Should().Be("L01");
          locs[1].IsPrimaryStorageLocation.Should().BeFalse();
      }
  ```

- [ ] 3. Tests laufen rot:
  ```
  dotnet test IdealAkeWms.Tests --nologo
  ```
  Erwartet: die drei neuen Tests `Failed` (Sortierung/Flag noch nicht implementiert). Übrige Tests grün.

- [ ] 4. **`GetCurrentStockAsync` implementieren.** In `IdealAkeWms/Data/Repositories/StockMovementRepository.cs` am Ende der Methode (statt der `return merged.OrderBy(...).ThenBy(...).ToList();`-Zeile Z. 169) die Primary-Auflösung + Flag + Sort einbauen. Ersetze die aktuelle `return`-Zeile (Z. 169) durch:
  ```csharp
          // Hauptlagerplatz je Artikel laden (nur die abgefragten Artikel).
          var articleIds = merged.Select(m => m.ArticleId).Distinct().ToList();
          var primaryByArticleId = await _context.Set<Article>()
              .Where(a => articleIds.Contains(a.Id) && a.PrimaryStorageLocationId != null)
              .Select(a => new { a.Id, a.PrimaryStorageLocationId })
              .ToDictionaryAsync(a => a.Id, a => a.PrimaryStorageLocationId!.Value);

          foreach (var item in merged)
          {
              item.IsPrimaryStorageLocation =
                  primaryByArticleId.TryGetValue(item.ArticleId, out var primaryLocId)
                  && item.StorageLocationId == primaryLocId;
          }

          return merged
              .OrderBy(g => g.ArticleNumber)
              .ThenBy(g => g.IsPrimaryStorageLocation ? 0 : 1)
              .ThenBy(g => g.StorageLocationCode)
              .ToList();
  ```
  (`_context` ist im Basis-`Repository<T>` als `protected` verfügbar; `Article` ist über `using IdealAkeWms.Models;` schon importiert.)

- [ ] 5. **`GetStockByArticleNumbersAsync` implementieren.** Der bisherige `return`-Block (Z. 374–391) mappt und sortiert per `OrderBy(i => i.Code)`. Da `StockLocationInfo` jetzt ein `IsPrimaryStorageLocation` trägt und die Sortierung pro Artikel den Hauptlagerplatz zuerst braucht, den `return`-Block ersetzen. Zuerst — vor dem `return destItems.Concat(srcItems)…` (Z. 374) — den Primary-Lookup laden:
  ```csharp
          // Hauptlagerplatz je Artikelnummer laden (nur abgefragte Artikel).
          var primaryByArticleNumber = await _dbSet
              .Where(sm => articleNumbers.Contains(sm.Article.ArticleNumber)
                        && sm.Article.PrimaryStorageLocationId != null)
              .Select(sm => new { sm.Article.ArticleNumber, PrimaryId = sm.Article.PrimaryStorageLocationId!.Value })
              .Distinct()
              .ToDictionaryAsync(x => x.ArticleNumber, x => x.PrimaryId);

          return destItems.Concat(srcItems)
              .GroupBy(x => new { x.ArticleNumber, x.StorageLocationId, x.StorageLocationCode })
              .Select(g => new
              {
                  g.Key.ArticleNumber,
                  Info = new StockLocationInfo
                  {
                      StorageLocationId = g.Key.StorageLocationId,
                      Code = g.Key.StorageLocationCode,
                      Quantity = g.Sum(x => x.Quantity),
                      IsPrimaryStorageLocation =
                          primaryByArticleNumber.TryGetValue(g.Key.ArticleNumber, out var primaryId)
                          && g.Key.StorageLocationId == primaryId
                  }
              })
              .Where(x => x.Info.Quantity != 0)
              .GroupBy(x => x.ArticleNumber)
              .ToDictionary(
                  g => g.Key,
                  g => g.Select(x => x.Info)
                        .OrderBy(i => i.IsPrimaryStorageLocation ? 0 : 1)
                        .ThenBy(i => i.Code)
                        .ToList()
              );
  ```
  (Ersetzt den kompletten bisherigen `return`-Block Z. 374–391.)

- [ ] 6. Tests grün:
  ```
  dotnet test IdealAkeWms.Tests --nologo
  ```
  Erwartet: `Passed!  - Failed:     0`.

- [ ] 7. Committen:
  ```
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IdealAkeWms/Data/Repositories/StockMovementRepository.cs IdealAkeWms/Models/ViewModels/StockOverviewViewModel.cs IdealAkeWms/Models/ViewModels/BomViewModels.cs IdealAkeWms.Tests/Repositories/StockMovementRepositoryTests.cs && git commit -m "$(cat <<'EOF'
feat(stock): Hauptlagerplatz je Artikel zuerst sortieren + IsPrimary-Flag

GetCurrentStockAsync und GetStockByArticleNumbersAsync laden pro Artikel den
PrimaryStorageLocationId und sortieren die Hauptlagerplatz-Zeile nach oben
(OrderBy primary-first vor Code). StockOverviewItem + StockLocationInfo tragen
IsPrimaryStorageLocation. Ohne Hauptlagerplatz unveraendert (Code-Sort).

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
  ```

---

## Task 5 — PickingApi + MissingPartsLager: primary-first vor Menge (TDD)

**Files:**
- Modify: `IdealAkeWms/Controllers/Api/PickingApiController.cs` (`SearchSourceLocations`, Sort Z. 51–64)
- Modify: `IdealAkeWms/Controllers/MissingPartsLagerController.cs` (Enrichment Z. 55–66)
- Modify: `IdealAkeWms.Tests/Controllers/MissingPartsLagerControllerTests.cs` (Sort-Assert)

**Steps:**

- [ ] 1. **PickingApiController — Stock + Primary-Ids laden.** Ersetze den bestehenden `stockByLoc`-Block (Z. 33–43, der `var stockByLoc = new Dictionary<int, decimal>();` … `}`-Block) durch eine kombinierte Variante, die im selben Durchlauf die Hauptlagerplatz-Ids einsammelt (kein zweiter Query):
  ```csharp
          // Stock + Hauptlagerplatz-Ids pro Article einmal laden
          var stockByLoc = new Dictionary<int, decimal>();
          var primaryLocIds = new HashSet<int>();
          if (!string.IsNullOrWhiteSpace(articleNumber))
          {
              var stockDict = await _stockMovements.GetStockByArticleNumbersAsync(new List<string> { articleNumber });
              if (stockDict.TryGetValue(articleNumber, out var stockList))
              {
                  foreach (var s in stockList)
                  {
                      stockByLoc[s.StorageLocationId] = s.Quantity;
                      if (s.IsPrimaryStorageLocation) primaryLocIds.Add(s.StorageLocationId);
                  }
              }
          }
  ```

- [ ] 2. **PickingApiController — Sortierung.** Die `ranked`-Projektion (Z. 51–64) um `isPrimary` erweitern und primary als oberste Ordnung setzen. Ersetze den `var ranked = filtered … .ToList();`-Block durch:
  ```csharp
          var ranked = filtered
              .Select(l =>
              {
                  stockByLoc.TryGetValue(l.Id, out var qty);
                  var hasStock = qty > 0;
                  var isPrimary = primaryLocIds.Contains(l.Id);
                  var label = hasStock ? $"{l.Code} ({qty:N3})" : l.Code;
                  return new { id = l.Id, text = label, qty, hasStock, isPrimary };
              })
              .OrderByDescending(x => x.isPrimary)
              .ThenByDescending(x => x.hasStock)
              .ThenByDescending(x => x.qty)
              .ThenBy(x => x.text)
              .Take(limit)
              .Select(x => new { id = x.id, text = x.text })
              .ToList();
  ```

- [ ] 3. **MissingPartsLagerController — Failing test zuerst.** In `IdealAkeWms.Tests/Controllers/MissingPartsLagerControllerTests.cs` einen Test ergänzen (oder einen bestehenden Reihenfolge-Assert erweitern), der die Anreicherungsreihenfolge prüft: zwei Lagerplätze mit Bestand seeden (der Hauptlagerplatz mit **kleinerer** Menge), `Article.PrimaryStorageLocationId` auf den kleineren setzen, `Index(...)` aufrufen und assertieren, dass der `StorageLocations`-String des Items mit dem Hauptlagerplatz-Code **beginnt**. Orientiere dich strukturell am vorhandenen Setup dieser Testdatei (gleiche Fake-/Repo-Konstruktion; `_stock.GetStockByArticleNumbersAsync` liefert die `StockLocationInfo`-Liste mit gesetztem `IsPrimaryStorageLocation`). Test laufen lassen:
  ```
  dotnet test IdealAkeWms.Tests --nologo
  ```
  Erwartet: der neue Test `Failed` (Enrichment sortiert noch nach Menge, nicht primary-first).

- [ ] 4. **MissingPartsLagerController implementieren.** Im `enrichedRows`-Select (Z. 55–66) die `nonZero`-Sortierung um primary-first ergänzen. Ersetze den `var nonZero = …`-Block durch:
  ```csharp
              var nonZero = locs.Where(l => l.Quantity > 0)
                                .OrderByDescending(l => l.IsPrimaryStorageLocation)
                                .ThenByDescending(l => l.Quantity)
                                .ToList();
  ```

- [ ] 5. Build + Web-Tests grün:
  ```
  dotnet build IdealAkeWms.slnx && dotnet test IdealAkeWms.Tests --nologo
  ```
  Erwartet: `Build succeeded`, `0 Error(s)`, `Passed!  - Failed:     0`.

- [ ] 6. Committen:
  ```
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IdealAkeWms/Controllers/Api/PickingApiController.cs IdealAkeWms/Controllers/MissingPartsLagerController.cs IdealAkeWms.Tests/Controllers/MissingPartsLagerControllerTests.cs && git commit -m "$(cat <<'EOF'
feat(picking): Hauptlagerplatz als oberste Ordnung vor Menge

SearchSourceLocations (Quell-Dropdown) und MissingPartsLager-Enrichment ziehen
OrderByDescending(IsPrimaryStorageLocation) vor die Mengen-Sortierung — der
Hauptlagerplatz steht auch bei geringerer Menge oben.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
  ```

---

## Task 6 — PickingController.Bom: Hauptlagerplatz als Quellvorschlag

**Files:**
- Modify: `IdealAkeWms/Controllers/PickingController.cs` (`Bom`, Auto-Suggest Z. 291–302)

**Steps:**

- [ ] 1. Den Auto-Suggest-Block (Z. 291–302) so ändern, dass zuerst der Hauptlagerplatz gewählt wird, WENN er buchbar ist und dort Bestand > 0 liegt; sonst der bisherige Fallback (höchste Menge), sonst NAN. Ersetze:
  ```csharp
              int? suggestedLocationId = null;
              var buchbarStock = locations
                  .Where(sl => sl.Quantity > 0 && buchbarLocationIds.Contains(sl.StorageLocationId))
                  .ToList();
              // 1. Hauptlagerplatz bevorzugen, wenn dort buchbarer Bestand > 0 liegt.
              var primaryBuchbar = buchbarStock
                  .FirstOrDefault(sl => sl.IsPrimaryStorageLocation);
              if (primaryBuchbar != null)
              {
                  suggestedLocationId = primaryBuchbar.StorageLocationId;
              }
              else if (buchbarStock.Count > 0)
              {
                  suggestedLocationId = buchbarStock.OrderByDescending(sl => sl.Quantity).First().StorageLocationId;
              }
              else if (nanLocationId.HasValue)
              {
                  suggestedLocationId = nanLocationId;
              }
  ```
  (`locations` ist die `List<StockLocationInfo>` aus `stockByArticle` — sie trägt seit Task 4 `IsPrimaryStorageLocation`.)

- [ ] 2. Build grün:
  ```
  dotnet build IdealAkeWms.slnx
  ```
  Erwartet: `Build succeeded`, `0 Error(s)`.

- [ ] 3. Committen:
  ```
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IdealAkeWms/Controllers/PickingController.cs && git commit -m "$(cat <<'EOF'
feat(picking): Bom-Quellvorschlag bevorzugt Hauptlagerplatz

Der Auto-Suggest-Quellplatz in PickingController.Bom waehlt den Hauptlagerplatz,
wenn dort buchbarer Bestand > 0 liegt; sonst Fallback hoechste Menge, sonst NAN.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
  ```

---

## Task 7 — Articles-Edit UI: Hauptlagerplatz-Feld mit Sage-Lock

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/ArticleEditViewModel.cs` (Felder)
- Modify: `IdealAkeWms/Controllers/ArticlesController.cs` (Edit-GET Z. 96–129, Edit-POST Z. 131–189)
- Modify: `IdealAkeWms/Views/Articles/Edit.cshtml` (neues Feld nach ReorderLevel)

**Steps:**

- [ ] 1. **ViewModel.** In `IdealAkeWms/Models/ViewModels/ArticleEditViewModel.cs` in `ArticleEditViewModel` (nach Z. 7 `Attributes`) ergänzen:
  ```csharp
      public List<AttributeEditItem> Attributes { get; set; } = new();
      public List<StorageLocation> StorageLocations { get; set; } = new();
      public bool IsPrimarySageControlled { get; set; }
  ```
  (Der Namespace importiert `IdealAkeWms.Models` bereits implicit über die Article/AttributeType-Nutzung — falls der Compiler `StorageLocation` nicht findet, oben `using IdealAkeWms.Models;` ergänzen.)

- [ ] 2. **Edit-GET.** In `ArticlesController.Edit(int id)` (Z. 96–129) nach dem Laden der `categories` (Z. 103) die aktiven Lagerplätze laden und im ViewModel setzen. Dazu braucht der Controller den `IStorageLocationRepository`. Ergänze das Feld + Konstruktor-Parameter:
  - Feld (nach Z. 19 `_productionOrderRepository`):
    ```csharp
      private readonly IProductionOrderRepository _productionOrderRepository;
      private readonly IStorageLocationRepository _storageLocationRepository;
    ```
  - Konstruktor-Signatur (Z. 21–37) um `IStorageLocationRepository storageLocationRepository` erweitern und `_storageLocationRepository = storageLocationRepository;` zuweisen.
  - Im GET, das `vm` (Z. 121–126) erweitern:
    ```csharp
          var storageLocations = await _storageLocationRepository.GetActiveOrderedExcludingPickingTransportAsync();

          var vm = new ArticleEditViewModel
          {
              Article = article,
              Categories = categories,
              Attributes = attributeItems,
              StorageLocations = storageLocations,
              IsPrimarySageControlled = !string.IsNullOrWhiteSpace(article.SagePrimaryStorageLocation)
          };
    ```

- [ ] 3. **Edit-POST.** Zwei Änderungen in `Edit(int id, ArticleEditViewModel vm)` (Z. 131–189):
  - Im ModelState-Fehlerpfad (Z. 140–156) die `StorageLocations`/`IsPrimarySageControlled` erneut befüllen, damit das Dropdown beim Re-Render nicht leer ist. `existing` ist an dieser Stelle noch NICHT geladen (das passiert erst Z. 158), daher den Sage-Zustand mit einem frischen Lookup ermitteln. Nach `vm.Categories = await _categoryRepository.GetAllOrderedAsync();` (Z. 142) ergänzen:
    ```csharp
              vm.Categories = await _categoryRepository.GetAllOrderedAsync();
              vm.StorageLocations = await _storageLocationRepository.GetActiveOrderedExcludingPickingTransportAsync();
              var sageCheck = await _articleRepository.GetByIdAsync(id);
              vm.IsPrimarySageControlled = !string.IsNullOrWhiteSpace(sageCheck?.SagePrimaryStorageLocation);
    ```
  - Im Erfolgspfad (nach Z. 166 `existing.ArticleCategoryId = vm.Article.ArticleCategoryId;`) den POST-Guard einbauen: bei Sage-Kontrolle die eingehende Id ignorieren, sonst übernehmen. Audit-Felder sind schon gesetzt (Z. 167–169). Ergänze:
    ```csharp
          existing.ArticleCategoryId = vm.Article.ArticleCategoryId;

          // POST-Guard: Sage-kontrollierter Hauptlagerplatz ist read-only — eingehende Id ignorieren.
          var isSagePrimary = !string.IsNullOrWhiteSpace(existing.SagePrimaryStorageLocation);
          if (!isSagePrimary)
          {
              existing.PrimaryStorageLocationId = vm.Article.PrimaryStorageLocationId;
          }

          existing.ModifiedAt = DateTime.Now;
    ```

- [ ] 4. **View.** In `IdealAkeWms/Views/Articles/Edit.cshtml` direkt NACH dem ReorderLevel-Block (Z. 54–59, endet mit `</div>`) das Hauptlagerplatz-Feld einfügen:
  ```html
                  <div class="mb-3">
                      <label asp-for="Article.PrimaryStorageLocationId" class="form-label">Hauptlagerplatz</label>
                      @if (Model.IsPrimarySageControlled)
                      {
                          <input type="hidden" asp-for="Article.PrimaryStorageLocationId" />
                          <select class="form-select bg-light" disabled>
                              <option>@Model.Article.SagePrimaryStorageLocation</option>
                          </select>
                          <div class="alert alert-info mt-2 mb-0 py-2">
                              Hauptlagerplatz aus Sage &uuml;bernommen (gesperrt):
                              <strong>@Model.Article.SagePrimaryStorageLocation</strong>
                          </div>
                      }
                      else
                      {
                          <select asp-for="Article.PrimaryStorageLocationId" class="form-select">
                              <option value="">&mdash;</option>
                              @foreach (var sl in Model.StorageLocations)
                              {
                                  <option value="@sl.Id">@sl.Code@(string.IsNullOrEmpty(sl.Description) ? "" : $" — {sl.Description}")</option>
                              }
                          </select>
                          <small class="text-muted">Bestimmt die Sortierung &bdquo;Hauptlagerplatz zuerst&ldquo; in den Best&auml;nden.</small>
                      }
                  </div>
  ```
  (`asp-for` mit `disabled`-Select würde den Wert nicht posten — deshalb der zusätzliche Hidden-Input im Sage-Zweig, der die aktuelle Id mitschickt. Der POST-Guard ignoriert ihn ohnehin serverseitig; der Hidden hält nur das Modell konsistent, falls Validierung re-rendered.)

- [ ] 5. Build grün:
  ```
  dotnet build IdealAkeWms.slnx
  ```
  Erwartet: `Build succeeded`, `0 Error(s)`.

- [ ] 6. **`ArticlesControllerTests` an die neue Konstruktor-Arity anpassen.** Der `Build()`-Helper in `IdealAkeWms.Tests/Controllers/ArticlesControllerTests.cs` (Z. 22–43) instanziiert den Controller direkt mit 7 Args. Da `IStorageLocationRepository` als **letzter** Konstruktor-Parameter hinzukommt (nach `productionOrderRepository`), einen Mock ergänzen und übergeben:
  - Nach `var orders = new Mock<IProductionOrderRepository>();` (Z. 29) einfügen:
    ```csharp
          var orders = new Mock<IProductionOrderRepository>();
          var storageLocations = new Mock<IStorageLocationRepository>();
          storageLocations.Setup(s => s.GetActiveOrderedExcludingPickingTransportAsync())
              .ReturnsAsync(new List<StorageLocation>());
    ```
  - Den `new ArticlesController(...)`-Call (Z. 36–38) auf den neuen Parameter erweitern:
    ```csharp
          var ctrl = new ArticlesController(
              article.Object, stock.Object, user.Object, attr.Object,
              category.Object, bom.Object, orders.Object, storageLocations.Object);
    ```
    (Falls andere Tests in dieser Datei den Controller separat bauen: gleiche Ergänzung dort.)
  ```
  dotnet test IdealAkeWms.Tests --nologo
  ```
  Erwartet: `Passed!  - Failed:     0`.

- [ ] 7. Committen:
  ```
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IdealAkeWms/Models/ViewModels/ArticleEditViewModel.cs IdealAkeWms/Controllers/ArticlesController.cs IdealAkeWms/Views/Articles/Edit.cshtml IdealAkeWms.Tests/Controllers/ArticlesControllerTests.cs && git commit -m "$(cat <<'EOF'
feat(article-ui): Hauptlagerplatz im Artikel bearbeiten (Sage-Lock)

ArticleEditViewModel traegt StorageLocations + IsPrimarySageControlled.
Edit-GET laedt aktive Lagerplaetze; Edit-POST ignoriert die eingehende Id
serverseitig, wenn SagePrimaryStorageLocation gesetzt ist (Tampering-Schutz).
View: Dropdown mit Leer-Option, im Sage-Fall disabled + Hidden-Id + Alert.

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
  ```

---

## Task 8 — StockOverview/Index: ⭐-Badge am Hauptlagerplatz

**Files:**
- Modify: `IdealAkeWms/Views/StockOverview/Index.cshtml` (Lagerplatz-Zelle Z. 121–135)

**Steps:**

- [ ] 1. In der Lagerplatz-Zelle (`<td>` mit `@item.StorageLocationCode`, Z. 121–135) direkt nach dem `<strong>@item.StorageLocationCode</strong>` (Z. 122) das Badge einfügen — nur wenn `IsPrimaryStorageLocation`:
  ```html
                      <td>
                          <strong>@item.StorageLocationCode</strong>
                          @if (item.IsPrimaryStorageLocation)
                          {
                              <span class="badge bg-warning text-dark ms-1" title="Hauptlagerplatz dieses Artikels">&#9733; Haupt</span>
                          }
                          @if (!item.StorageLocationIsActive && item.CurrentQuantity > 0)
  ```
  (Der Rest der Zelle bleibt unverändert; das `@if (!item.StorageLocationIsActive …)` ist die bestehende Zeile 123.)

- [ ] 2. Build grün:
  ```
  dotnet build IdealAkeWms.slnx
  ```
  Erwartet: `Build succeeded`, `0 Error(s)`.

- [ ] 3. Committen:
  ```
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IdealAkeWms/Views/StockOverview/Index.cshtml && git commit -m "$(cat <<'EOF'
feat(stock-ui): Stern-Badge "Haupt" am Hauptlagerplatz in der Bestandsuebersicht

Nur in StockOverview/Index — an der Hauptlagerplatz-Zeile via
IsPrimaryStorageLocation. Andere Anzeige-Stellen bleiben Badge-frei
(bewusste Scope-Begrenzung, User-Entscheid).

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
  ```

---

## Task 9 — Dokumentation: Changelog + CLAUDE.md + TESTSZENARIEN + PROJECT_STATUS

**Files:**
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml` (v1.25.0-Card, Z. 16–43)
- Modify: `CLAUDE.md` (neuer Fallstrick-Absatz)
- Modify: `docs/TESTSZENARIEN.md` (neues Kapitel 49)
- Modify: `PROJECT_STATUS.md` (v1.25.0-Abschnitt)

**Steps:**

- [ ] 1. **Changelog.** In `IdealAkeWms/Views/Help/Changelog.cshtml` in der v1.25.0-`<ul>` (Z. 16–43) vor `</ul>` (Z. 43) einen Bullet ergänzen:
  ```html
                      <li><strong>Hauptlagerplatz am Artikel:</strong> Artikel haben jetzt einen
                          <strong>Hauptlagerplatz</strong>. Er kommt aus Sage (dann gesperrt) oder ist,
                          wenn Sage keinen liefert, in &bdquo;Artikel bearbeiten&ldquo; manuell w&auml;hlbar.
                          &Uuml;berall wo Bestand pro Lagerplatz angezeigt wird, steht der Hauptlagerplatz
                          zuerst; in der Bestands&uuml;bersicht tr&auml;gt er ein &#9733;-Badge. Fehlt der
                          Lagerplatz ausnahmsweise im WMS, bleibt der Sage-Code sichtbar und der Fall wird
                          im Aktivit&auml;ts-Protokoll gez&auml;hlt (<code>hauptlagerplatz_fehlt</code>).</li>
  ```

- [ ] 2. **CLAUDE.md.** Am Ende des `## Bekannte Fallstricke`-Blocks (nach dem letzten `- **SyncWorker-Resilienz…**`-Absatz) einen neuen Fallstrick-Absatz ergänzen:
  ```markdown
  - **Hauptlagerplatz am Artikel (v1.25.0)**: `Article.PrimaryStorageLocationId` (FK → `StorageLocations`, SetNull, **Migration 79** `AddArticlePrimaryStorageLocation`, additiv) + `Article.SagePrimaryStorageLocation` (nvarchar(100), Sage-Rohcode). **Lock-Semantik ohne Source-Flag:** `SagePrimaryStorageLocation` nicht leer ⇒ Wert stammt aus Sage ⇒ in der App gesperrt (Edit-POST ignoriert die eingehende `PrimaryStorageLocationId` serverseitig); leer ⇒ app-editierbar. **Sync-Regeln** (`SageImportService.SyncArticlesAsync`, raw-SQL, Manual-UAT): Sage liefert Wert → Rohcode + FK-Lookup (`locationIdByCode`, case-insensitiv, einmal/Lauf); kein Match → FK null + Warn-Log + Count `hauptlagerplatz_fehlt`; Sage leer → Rohcode null, FK **unberührt** (bewahrt manuelle App-Wahl). **Sortierung „Hauptlagerplatz zuerst":** zentral in `StockMovementRepository.GetCurrentStockAsync` + `GetStockByArticleNumbersAsync` (`OrderBy(ArticleNumber).ThenBy(IsPrimary ? 0 : 1).ThenBy(Code)`) — ~9 Anzeige-Stellen erben das. Drei Stellen ziehen primary als oberste Ordnung VOR die Menge ein: `PickingApiController.SearchSourceLocations`, `MissingPartsLagerController`-Enrichment, `PickingController.Bom`-Quellvorschlag (Hauptlagerplatz bevorzugt bei buchbarem Bestand > 0). Badge ⭐ „Haupt" NUR in `StockOverview/Index` (via `IsPrimaryStorageLocation`), sonst nur Sortierung. Generische Buchungs-Dropdowns bleiben unverändert (nicht artikelspezifisch).
  ```

- [ ] 3. **TESTSZENARIEN.** In `docs/TESTSZENARIEN.md` ans Dateiende ein neues Kapitel 49 anhängen (nächste freie Nummer nach 48):
  ```markdown

  ## Kapitel 49: Hauptlagerplatz am Artikel (v1.25.0)

  **Feature:** Artikel haben einen Hauptlagerplatz. Wert primär aus Sage (dann gesperrt), sonst in der App setzbar. In allen Bestand-je-Lagerplatz-Anzeigen steht der Hauptlagerplatz zuerst; Bestandsübersicht zeigt ein ⭐-Badge.

  ### 49.1 Manuellen Hauptlagerplatz setzen (Sage liefert keinen)
  **Vorbedingung:** Rolle admin oder masterdata. Ein Artikel ohne Sage-Hauptlagerplatz (`SagePrimaryStorageLocation` leer). Mindestens zwei aktive, nicht-Wagen-Lagerplätze existieren.
  1. Stammdaten → Artikel → Artikel bearbeiten öffnen.
  2. Feld **Hauptlagerplatz** ist ein editierbares Dropdown mit Leer-Option „—".
  3. Einen Lagerplatz wählen, Speichern.
  **Erwartet:** Erfolgsmeldung „Artikel gespeichert."; nach erneutem Öffnen ist der gewählte Lagerplatz vorausgewählt.

  ### 49.2 Sage-Lock (Hauptlagerplatz aus Sage gesperrt)
  **Vorbedingung:** Ein Artikel, bei dem `SagePrimaryStorageLocation` gesetzt ist (nach einem Artikel-Sync mit gepflegtem `KHKArtikel.PlatzID`).
  1. Artikel bearbeiten öffnen.
  **Erwartet:** Das Hauptlagerplatz-Dropdown ist **disabled**; darunter eine blaue Info-Box „Hauptlagerplatz aus Sage übernommen (gesperrt): <Code>". Ein Änderungsversuch (z. B. per Browser-DevTools den Hidden-Wert ändern und posten) darf den Wert NICHT ändern — der Server ignoriert die eingehende Id.

  ### 49.3 Sortierung „Hauptlagerplatz zuerst" in der Bestandsübersicht
  **Vorbedingung:** Ein Artikel mit Bestand auf mindestens zwei Lagerplätzen; Hauptlagerplatz = derjenige mit der ALPHABETISCH späteren/kleineren Menge (bewusst nicht der „natürliche" erste).
  1. Bestand → Artikelbestände öffnen, nach dem Artikel filtern.
  **Erwartet:** Die Zeile des Hauptlagerplatzes steht je Artikel ganz oben, unabhängig von Code-Alphabet und Menge; sie trägt ein gelbes Badge „★ Haupt". Die übrigen Lagerplatz-Zeilen folgen nach Code sortiert (kein Badge).

  ### 49.4 Sortierung im Kommissionier-Quellvorschlag
  **Vorbedingung:** Ein FA mit einem Bauteil, das Bestand auf Hauptlagerplatz (buchbar, Menge > 0) und einem weiteren buchbaren Lagerplatz mit HÖHERER Menge hat.
  1. Kommissionierung → Stückliste des FA öffnen.
  **Erwartet:** Der vorgeschlagene Quell-Lagerplatz für dieses Bauteil ist der **Hauptlagerplatz** (nicht der mit der höchsten Menge). Im Quell-Dropdown (Suche) steht der Hauptlagerplatz an erster Stelle. Hat der Hauptlagerplatz keinen buchbaren Bestand, greift der bisherige Fallback (höchste Menge), sonst NAN.

  ### 49.5 Sortierung in „Lager: Fehlteile"
  **Vorbedingung:** Ein Fehlteil-Item, dessen Artikel Bestand auf Hauptlagerplatz (kleinere Menge) + weiterem Lagerplatz (größere Menge) hat.
  1. Lager → Lager: Fehlteile öffnen.
  **Erwartet:** In der Lagerplatz-Spalte des Items steht der Hauptlagerplatz zuerst, auch bei geringerer Menge.

  ### 49.6 Fehlender WMS-Lagerplatz (Negativfall)
  **Vorbedingung:** Sage liefert einen Hauptlagerplatz-Code (`Kurzbezeichnung`), zu dem KEIN WMS-`StorageLocation` mit passendem `Code` existiert.
  1. Artikel-Sync laufen lassen (Windows-Service).
  **Erwartet:** Der Artikel behält den Sage-Rohcode sichtbar + gesperrt (49.2), `PrimaryStorageLocationId` bleibt leer, keine Sortier-Priorisierung. Im Aktivitäts-Protokoll (Service „Article") ist der Count `hauptlagerplatz_fehlt` > 0; im Service-Log steht eine Warnung mit Artikelnummer + Code. Nach Anlegen/Sync des fehlenden Lagerplatzes matcht der FK beim nächsten Lauf.
  ```

- [ ] 4. **PROJECT_STATUS.** In `PROJECT_STATUS.md` im v1.25.0-Abschnitt (ab Z. 30 `### v1.25.0 (2026-07-03)…`) einen kurzen Zusatzpunkt ergänzen, dass der Hauptlagerplatz Teil von v1.25.0 ist (eine Zeile, z. B. als Bullet: „Hauptlagerplatz am Artikel (Sage-Sync + manueller Fallback, Sortierung „Haupt zuerst", ⭐-Badge; Migration 79)").

- [ ] 5. Build grün (Changelog ist Razor):
  ```
  dotnet build IdealAkeWms.slnx
  ```
  Erwartet: `Build succeeded`, `0 Error(s)`.

- [ ] 6. Committen:
  ```
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git add IdealAkeWms/Views/Help/Changelog.cshtml CLAUDE.md docs/TESTSZENARIEN.md PROJECT_STATUS.md && git commit -m "$(cat <<'EOF'
docs(hauptlagerplatz): Changelog-Bullet + CLAUDE.md-Fallstrick + TS Kap.49 + PROJECT_STATUS

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>
EOF
)"
  ```

---

## Task 10 — Final-Check

**Files:**
- (nur ausführen/verifizieren)

**Steps:**

- [ ] 1. Build 0 Fehler:
  ```
  dotnet build IdealAkeWms.slnx
  ```
  Erwartet: `Build succeeded`, `0 Error(s)`.

- [ ] 2. `has-pending` leer:
  ```
  dotnet ef migrations has-pending-model-changes --project IdealAkeWms
  ```
  Erwartet: `No changes have been made to the model since the last migration.`

- [ ] 3. Web-Tests grün:
  ```
  dotnet test IdealAkeWms.Tests --nologo
  ```
  Erwartet: `Passed!  - Failed:     0`.

- [ ] 4. Service-Tests grün:
  ```
  dotnet test IDEALAKEWMSService.Tests --nologo
  ```
  Erwartet: `Passed!  - Failed:     0`.

- [ ] 5. Grep-Beweise, dass Feld/Migration/FreshInstall zusammenpassen:
  ```
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && grep -rl "PrimaryStorageLocation" IdealAkeWms/Models/Article.cs IdealAkeWms/Migrations SQL/79_AddArticlePrimaryStorageLocation.sql && grep -c "PrimaryStorageLocation" SQL/00_FreshInstall.sql
  ```
  Erwartet: `Article.cs`, ein Migrations-File und das SQL/79 werden gelistet; `grep -c` auf FreshInstall liefert eine Zahl ≥ 4 (Schema-Spalten + Index + FK + History erwähnen `PrimaryStorageLocation`).

- [ ] 6. History-Insert-Id stimmt mit dem Migrations-Timestamp überein:
  ```
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && ls IdealAkeWms/Migrations/*_AddArticlePrimaryStorageLocation.cs && grep "AddArticlePrimaryStorageLocation" SQL/00_FreshInstall.sql SQL/79_AddArticlePrimaryStorageLocation.sql
  ```
  Erwartet: Der Dateiname-Timestamp und die `MigrationId` in beiden SQL-Dateien sind identisch (`<MIGID>_AddArticlePrimaryStorageLocation`).

- [ ] 7. Git-Status sauber (alles committed):
  ```
  cd "C:/Git/IDEAL-AKE-WMS/.claude/worktrees/glas-bestellung" && git status --short
  ```
  Erwartet: leere Ausgabe. Falls noch etwas offen ist, mit passender Message committen (Co-Authored-By-Footer nicht vergessen).

---

## File-Struktur-Übersicht (was wird angefasst, warum)

| Datei | Task | Warum |
|-------|------|-------|
| `IdealAkeWms/Models/Article.cs` | 1 | Zwei neue Felder + Navigation (PrimaryStorageLocationId, PrimaryStorageLocation, SagePrimaryStorageLocation) |
| `IdealAkeWms/Data/ApplicationDbContext.cs` | 1 | EF-Config: FK SetNull + Index + nvarchar(100) |
| `IdealAkeWms/Migrations/<MIGID>_AddArticlePrimaryStorageLocation.cs` | 2 | EF-Migration 79 (generiert) |
| `SQL/79_AddArticlePrimaryStorageLocation.sql` | 2 | Idempotentes Deploy-Skript (COL_LENGTH/OBJECT_ID-Guards, History-Insert) |
| `SQL/00_FreshInstall.sql` | 2 | Konsolidiertes Schema (Article-Spalten + Index + FK) + History-Insert synchron |
| `IDEALAKEWMSService/Services/SageImportHelpers.cs` | 3 | Pure Helper `NormalizeLocationCode` (unit-testbar) |
| `IDEALAKEWMSService/Services/SageImportService.cs` | 3 | Sync-SQL um KHKLagerplaetze-Join erweitert; Code→Id-Lookup; Upsert-Regeln; Warn-Count |
| `IDEALAKEWMSService.Tests/Services/SageImportServiceTests.cs` | 3 | Theory für `NormalizeLocationCode` |
| `IdealAkeWms/Models/ViewModels/StockOverviewViewModel.cs` | 4 | `IsPrimaryStorageLocation` auf `StockOverviewItem` |
| `IdealAkeWms/Models/ViewModels/BomViewModels.cs` | 4 | `IsPrimaryStorageLocation` auf `StockLocationInfo` |
| `IdealAkeWms/Data/Repositories/StockMovementRepository.cs` | 4 | Zentrale „Haupt zuerst"-Sortierung + Flag in beiden Methoden |
| `IdealAkeWms.Tests/Repositories/StockMovementRepositoryTests.cs` | 4 | Sort-/Flag-Tests (mit + ohne Hauptlagerplatz) |
| `IdealAkeWms/Controllers/Api/PickingApiController.cs` | 5 | primary-first vor Menge im Quell-Dropdown |
| `IdealAkeWms/Controllers/MissingPartsLagerController.cs` | 5 | primary-first vor Menge im Lager-Fehlteile-Enrichment |
| `IdealAkeWms.Tests/Controllers/MissingPartsLagerControllerTests.cs` | 5 | Sort-Assert |
| `IdealAkeWms/Controllers/PickingController.cs` | 6 | Bom-Quellvorschlag bevorzugt Hauptlagerplatz |
| `IdealAkeWms/Models/ViewModels/ArticleEditViewModel.cs` | 7 | `StorageLocations` + `IsPrimarySageControlled` |
| `IdealAkeWms/Controllers/ArticlesController.cs` | 7 | GET lädt Lagerplätze; POST-Guard + Audit |
| `IdealAkeWms/Views/Articles/Edit.cshtml` | 7 | Dropdown / Sage-Lock-Darstellung |
| `IdealAkeWms.Tests/Controllers/ArticlesControllerTests.cs` | 7 | Konstruktor-Arity (neuer Repo-Mock) |
| `IdealAkeWms/Views/StockOverview/Index.cshtml` | 8 | ⭐-Badge „Haupt" |
| `IdealAkeWms/Views/Help/Changelog.cshtml` | 9 | v1.25.0-Bullet |
| `CLAUDE.md` | 9 | Fallstrick-Absatz |
| `docs/TESTSZENARIEN.md` | 9 | Kapitel 49 |
| `PROJECT_STATUS.md` | 9 | v1.25.0-Zusatzpunkt |
