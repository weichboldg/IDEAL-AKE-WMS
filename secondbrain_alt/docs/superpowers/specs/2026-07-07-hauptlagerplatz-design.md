# Hauptlagerplatz (PrimaryStorageLocation) — Design

**Datum:** 2026-07-07
**Status:** Approved-pending-review
**Branch/Worktree:** `feature/glas-bestellung` (@ `1504573`) → `.claude/worktrees/glas-bestellung`
**Version:** in **v1.25.0** falten (noch nicht released) — kein Versionssprung.

Artikel bekommen einen **Hauptlagerplatz**. Wert kommt primär aus Sage; wenn Sage keinen liefert, im APP manuell setzbar. Überall wo Bestand pro Lagerplatz gezeigt wird, steht der Hauptlagerplatz zuerst.

---

## Ziel / Anforderung

1. Stammdaten am Artikel: **Hauptlagerplatz**.
2. Kommt aus Sage (neue Sync-SQL liefert Spalte `PrimaryStorageLocation` = `KHKLagerplaetze.Kurzbezeichnung` via `KHKArtikel.PlatzID`). **Wenn aus Sage → im APP gesperrt** (read-only).
3. Liefert Sage **keinen** Hauptlagerplatz → im APP **manuell einstellbar**.
4. **In den Lagerbeständen (überall wo Bestand pro Lagerplatz angezeigt wird) steht der Hauptlagerplatz immer zuerst.**

Der Hauptlagerplatz **muss** als WMS-`StorageLocation` existieren (Normalfall; wird über den Lagerplatz-Sync gepflegt). Fehlt er ausnahmsweise, darf das Feature nicht crashen — Rohcode wird behalten und der Fehlbestand geloggt.

---

## Datenmodell

Zwei additive Felder an `IdealAkeWms/Models/Article.cs` (erbt `AuditableEntity`):

- `public int? PrimaryStorageLocationId { get; set; }` — FK → `StorageLocations` (`OnDelete SetNull`). Der **aufgelöste** Hauptlagerplatz. Matching gegen Bestände läuft über die StorageLocation-**Id** (nicht über Code-Strings).
- `public StorageLocation? PrimaryStorageLocation { get; set; }` — Navigation.
- `public string? SagePrimaryStorageLocation { get; set; }` — `nvarchar(100)` NULL. Der **Sage-Rohcode** (`Kurzbezeichnung`).

**Lock-Semantik (bewusst ohne separates Source-Flag):**
- `SagePrimaryStorageLocation` **nicht leer** ⇒ Wert stammt aus Sage ⇒ im APP **gesperrt**.
- `SagePrimaryStorageLocation` **leer/null** ⇒ **app-editierbar** (`PrimaryStorageLocationId` frei setzbar).

**EF-Konfiguration** in `ApplicationDbContext.cs` (Article-Config, ~Z. 200–222, analog `ArticleCategoryId`):
```csharp
entity.HasOne(a => a.PrimaryStorageLocation)
      .WithMany()
      .HasForeignKey(a => a.PrimaryStorageLocationId)
      .OnDelete(DeleteBehavior.SetNull);
```

**Migration 79** `AddArticlePrimaryStorageLocation` (additiv): 2 Spalten + Index auf `PrimaryStorageLocationId` + FK. Idempotentes `SQL/79_AddArticlePrimaryStorageLocation.sql` (OBJECT_ID/COL_LENGTH-Guards, History-Insert in separatem Batch). `SQL/00_FreshInstall.sql`: Spalten + Index + FK im konsolidierten Article-Schema **und** History-Insert.

---

## Sync (Windows-Service, primärer Pfad)

`IDEALAKEWMSService/Services/SageImportService.cs` → `SyncArticlesAsync` (Z. 252–408).

**SQL erweitern** (Z. 265–299) auf die vom User gelieferte UNION — beide Zweige joinen `KHKLagerplaetze lp ON a.PlatzID = lp.PlatzID` und selektieren `CAST(lp.Kurzbezeichnung AS nvarchar(100)) AS PrimaryStorageLocation`:

```sql
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
WHERE a.IstBestellartikel = -1 AND Aktiv = -1
```

**Auflösung Code → StorageLocationId:** Einmal pro Sync-Lauf ein Dict `Kurzbezeichnung/Code → StorageLocation.Id` aus der WMS-DB laden (case-insensitive; `Trim`). Der Reader liest die neue Spalte zusätzlich (benannt oder per Index).

**Upsert-Logik** je Artikel (Z. 342–371 erweitern):
- **Sage liefert Wert** (nicht leer/whitespace):
  - `SagePrimaryStorageLocation = trimmedCode`.
  - `PrimaryStorageLocationId = lookup[code]` falls vorhanden.
  - **Kein Match** → `PrimaryStorageLocationId = null` (kein WMS-Lagerplatz → keine Sortier-Priorisierung möglich, ehrlicher als ein stale FK); Rohcode trotzdem gesetzt (bleibt sichtbar + gesperrt); **Warnung** ins Aktivitäts-Protokoll (Count `hauptlagerplatz_fehlt`, mit ArticleNumber + Code im Log-Text). Der Admin sieht so den fehlenden Lagerplatz und kann ihn anlegen; beim nächsten Sync matcht der FK dann.
- **Sage liefert leer**:
  - `SagePrimaryStorageLocation = null` (⇒ wieder app-editierbar).
  - `PrimaryStorageLocationId` **nicht anfassen** (bewahrt manuelle App-Wahl).

Die Reconcile-/Full-Update-Semantik von `SyncArticlesAsync` bleibt sonst unverändert. `SageImportHelpers.cs`: optional ein kleiner Lookup-/Normalisierungs-Helper (`NormalizeLocationCode`) für Unit-Tests.

**AgentJob** `SQL/AgentJobs/02_Import_Artikel.sql` bleibt **deprecated** (seit v1.17.0) — nur Kommentar-Hinweis, dass PrimaryStorageLocation NICHT unterstützt wird (der Service ist der Produktivpfad).

---

## UI — Artikel bearbeiten

`IdealAkeWms/Controllers/ArticlesController.cs` (Edit-GET Z. 97–129, Edit-POST Z. 131–171) + `Models/ViewModels/ArticleEditViewModel.cs` + `Views/Articles/Edit.cshtml`.

- ViewModel: `List<StorageLocation> StorageLocations` (aktive, ohne Kommissionierwagen — via `GetActiveOrderedExcludingPickingTransportAsync`) + `bool IsPrimarySageControlled` (`= !string.IsNullOrWhiteSpace(article.SagePrimaryStorageLocation)`).
- View: neues Feld **Hauptlagerplatz** nach ReorderLevel.
  - `IsPrimarySageControlled == true` → Dropdown **disabled** + Hidden-Input mit aktueller Id + Alert „Hauptlagerplatz aus Sage übernommen (gesperrt)", Sage-Code angezeigt (Muster wie StorageLocation-Sage-Lock).
  - sonst → editierbares `<select asp-for="Article.PrimaryStorageLocationId">` mit Leer-Option „—".
- POST-Guard: bei `IsPrimarySageControlled` eingehende `PrimaryStorageLocationId` **ignorieren** (Server-seitig gegen Tampering; Sage-Wert bleibt). Audit-Felder (`ModifiedAt/By/ByWindows`) setzen.

`[RequireMasterDataAccess]` (Edit) bleibt; Read-User (`masterdata_read`) sehen das Feld read-only wie den Rest.

---

## Sortierung „Hauptlagerplatz zuerst"

**Zentraler Ansatz:** die zwei Repo-Methoden, durch die praktisch alle Bestand-je-Lagerplatz-Anzeigen laufen, sortieren künftig **je Artikel** den Hauptlagerplatz nach oben. Damit erben ~9 Stellen das Verhalten automatisch.

`IdealAkeWms/Data/Repositories/StockMovementRepository.cs`:
- `GetCurrentStockAsync` (Z. 13–170): statt `OrderBy(ArticleNumber).ThenBy(StorageLocationCode)` →
  `OrderBy(ArticleNumber).ThenBy(g => g.StorageLocationId == primaryOf[g.ArticleNumber] ? 0 : 1).ThenBy(StorageLocationCode)`.
- `GetStockByArticleNumbersAsync` (Z. 318–392): pro Artikel-Gruppe `OrderBy(loc.StorageLocationId == primaryId ? 0 : 1).ThenBy(Code)` statt nur `OrderBy(Code)`.
- Beide Methoden laden dazu ein Dict `ArticleNumber → PrimaryStorageLocationId` für die abgefragten Artikel (ein zusätzlicher, gefilterter Article-Query).
- `StockOverviewItem` (`Models/ViewModels/StockOverviewViewModel.cs` Z. 17–31) + `StockLocationInfo`: neues `bool IsPrimaryStorageLocation` (gesetzt beim Mapping: `StorageLocationId == primaryId`).

**Stellen, die nach dem Zentral-Sort erneut nach Menge sortieren → „primary first" als oberste Ordnung einziehen:**
- `IdealAkeWms/Api/PickingApiController.cs` `SearchSourceLocations` (Z. 28–67, Sort Z. 59–61): `OrderByDescending(isPrimary).ThenByDescending(hasStock).ThenByDescending(qty).ThenBy(text)`.
- `IdealAkeWms/Controllers/MissingPartsLagerController.cs` (Enrichment Z. 57–59): `OrderByDescending(isPrimary).ThenByDescending(Quantity)`.

**Picking-Quellvorschlag** `IdealAkeWms/Controllers/PickingController.cs` `Bom` (Z. 297): Default-Quelle = Hauptlagerplatz **wenn dort buchbarer Bestand > 0**, sonst Fallback auf höchste Menge (bisher `buchbarStock.OrderByDescending(Quantity).First()`).

**Badge/Marker:** ⭐ „Haupt" **nur** in der Bestandsübersicht (`Views/StockOverview/Index.cshtml`) an der Hauptlagerplatz-Zeile (via `IsPrimaryStorageLocation`). Alle anderen Stellen: nur Sortierung, **kein** Badge (bewusste Scope-Begrenzung, User-Entscheid).

**Generische Buchungs-Dropdowns** (Ein-/Aus-/Umbuchung, OutboundAll/LocationTransfer) bleiben **unverändert** — sie sind nicht artikelspezifisch, daher gibt es keinen eindeutigen „Haupt". (Nur der artikelspezifische Picking-Quellvorschlag wird angepasst.)

### Betroffene Anzeige-Stellen (Referenz, Ergebnis der Inventur)
Erben Zentral-Sort automatisch: StockOverview, Picking/Bom, WarehousePicking/Details, WarehouseRequisitions-Bestand (API), Picking/PrintBom, ReadOnlyBomBuilder (VB-/Kompl.-Liste), Articles/Info. Explizit angepasst: PickingApi/SearchSourceLocations, MissingPartsLager, PickingController-Quellvorschlag. Nicht relevant: StockMovements/Index (Historie), generische Buchungs-Dropdowns.

---

## Tests

- **SageImportHelpers** (Service-Tests, InternalsVisibleTo vorhanden): Code-Normalisierung/Lookup (Trim, case-insensitive, kein Match → null).
- **StockMovementRepository** (InMemory-Repo-Tests): bei gesetztem `Article.PrimaryStorageLocationId` steht die Hauptlagerplatz-Zeile je Artikel zuerst; `IsPrimaryStorageLocation` korrekt gesetzt; ohne Hauptlagerplatz unverändert (Code-Sort).
- **Sort-vor-Menge** (PickingApi/MissingPartsLager): Unit-/Controller-Test, dass Hauptlagerplatz auch bei geringerer Menge oben steht.
- **Sync** (SyncArticlesAsync, Raw-SQL) + **UI** (Edit-Lock) = **Manual-UAT** (Raw-SQL nicht InMemory-testbar).

---

## Deploy

- Migration 79 additiv (nullable Spalten, kein Datenverlust). FreshInstall + History synchron.
- Erst nach dem ersten Artikel-Sync mit der neuen SQL sind Hauptlagerplätze befüllt. Voraussetzung: Lagerplatz-Stammdaten (StorageLocations) vorhanden — sonst `hauptlagerplatz_fehlt`-Warnungen im Aktivitäts-Protokoll (Hinweis für Admin, fehlende Lagerplätze anzulegen/zu syncen).
- Kein AppSetting/Feature-Flag nötig (rein additives Stammdaten-Feld).

## Out of scope

- Kein Auto-Anlegen fehlender `StorageLocation`s aus dem Artikel-Sync (bleibt Aufgabe des Lagerplatz-Syncs); stattdessen Warn-Log.
- Keine Badge in Picking/Bom o. a. (nur Bestandsübersicht) — User-Entscheid.
- Generische Buchungs-Dropdowns unverändert.
