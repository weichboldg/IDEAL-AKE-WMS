# BOM-Cache & Lackierteil-Erkennung — Design Spec

## Ziel

1. **BOM-Cache** in der WMS-Datenbank fuer die wichtigsten offenen Auftraege (max. 200, mit Fertigungstermin in den naechsten 8 Wochen). Der Service-Sync befuellt den Cache aus SAGE/OSEON. `BomRepository` liest Cache-First mit Live-Fallback. Performance-Gewinn fuer Stueckliste-Anzeige und ermoeglicht effiziente DB-interne BOM-Auswertungen.

2. **Lackierteil-Erkennung & Beschichtungstermin** auf Basis des BOM-Caches. Pro Auftrag wird gespeichert ob Lackierteile enthalten sind (`HasCoatingParts`). Der Beschichtungstermin und die zugehoerige Spalte werden nur berechnet/angezeigt wenn ein Lackierteil enthalten ist. Eine neue **Lack-T**-Spalte mit klickbarer Checkbox erlaubt User die Lackierteile als erledigt zu markieren (`IsCoatingDone`).

## Architektur

Drei Bausteine:

1. **Persistenter BOM-Cache** — neue Tabellen `CachedBomHeaders` (1 Eintrag pro Geraete-Artikelnummer mit Source/Hash/CachedAt) und `CachedBomItems` (Detail-Items mit FK auf Header). Befuellt durch neuen `BomCacheSyncService` im Windows-Service. Cache-First-Strategie in `BomRepository`: erst DB-Cache pruefen, bei Miss Live-Query SAGE/OSEON. MemoryCache-Decorator (5-Min) bleibt davor.

2. **Lackierteil-Erkennung** auf Basis des BOM-Caches. Sync-Job fragt fuer alle Auftraege im Window: gibt es BOM-Items mit `Articles.ArticleCategory.Name = LackierteilKategorieName`? Setzt `ProductionOrder.HasCoatingParts`. Laeuft **bei neuen Auftraegen** automatisch (Hook in `SageImportService.SyncProductionOrdersAsync`) und **separat fuer alle offenen** ueber neuen Toggle `Sync:CoatingDetectionEnabled`.

3. **UI-Erweiterungen** in der FA-Liste — neue Spalte **Lack-T** zwischen Liefertermin und Glas mit klickbarer Checkbox (nur sichtbar wenn `HasCoatingParts=true`). Die bestehende Beschicht.-Spalte zeigt das Datum nur wenn `HasCoatingParts=true`. Wenn `IsCoatingDone=true`, wird das Datum nicht mehr rot markiert (analog zu `IsDone`).

Der bestehende Picking-Workflow (`PickingItems`-Tabelle, lazy `InitializePickingAsync`) bleibt **unveraendert**. Eine spaetere Picking-Refaktorierung auf den BOM-Cache (FK statt Datenduplikation) kommt in einem separaten Plan.

---

## Datenmodell

### CachedBomHeaders (neu)

Header-Tabelle: ein Eintrag pro Geraete-Artikelnummer (= `ProductionOrder.ArticleNumber`).

| Feld | Typ | Constraint | Beschreibung |
|------|-----|-----------|--------------|
| Id | int | PK, Identity | |
| Artikelnummer | string(100) | Required, Unique | Geraete-Artikelnummer |
| Source | string(20) | Required | "SAGE" oder "OSEON" |
| ItemCount | int | Required | Anzahl Items im Detail |
| ContentHash | string(64) | Required | SHA256 hex der sortierten Item-Liste |
| CachedAt | DateTime | Required | Letztes Sync-Update |

**Indexes:**
- `IX_CachedBomHeaders_Artikelnummer` (Unique)

**Kein AuditableEntity** — reine Cache-Tabelle.

### CachedBomItems (neu)

Detail-Tabelle: BOM-Items pro Header.

| Feld | Typ | Constraint | Beschreibung |
|------|-----|-----------|--------------|
| Id | int | PK, Identity | |
| CachedBomHeaderId | int | FK → CachedBomHeaders, Cascade | |
| Position | string(50) | Nullable | BOM-Position (z.B. "10.5") |
| Baugruppe | string(200) | Nullable | Baugruppen-Name |
| Ressourcenummer | string(100) | Nullable | Bauteil-Artikelnummer |
| Bezeichnung1 | string(500) | Nullable | |
| Bezeichnung2 | string(500) | Nullable | |
| Menge | decimal(18,3) | Required | |
| Beschaffungsartikel | string(100) | Nullable | |
| Artikelgruppe | string(100) | Nullable | |
| SortOrder | int | Required | Original-Reihenfolge der Items beim Sync (fuer stabile Anzeige) |

**Indexes:**
- `IX_CachedBomItems_CachedBomHeaderId`
- `IX_CachedBomItems_Ressourcenummer` (fuer rueckwaertigen Lookup: welche Stuecklisten enthalten Bauteil X? → Lackierteil-Erkennung)

**OnDelete CachedBomHeader:** Cascade.
**Kein AuditableEntity** — reine Cache-Tabelle.

### ProductionOrder (erweitert)

Zwei neue Felder:

| Feld | Typ | Default | Beschreibung |
|------|-----|---------|--------------|
| HasCoatingParts | bool | false | true wenn BOM mind. 1 Lackierteil enthaelt (sync-berechnet, read-only fuer User) |
| IsCoatingDone | bool | false | User-toggleable: Lackierteile sind erledigt |

### AppSettings (DB-Tabelle)

Neues Setting:

| Key | Default | Beschreibung |
|-----|---------|--------------|
| `LackierteilKategorieName` | (leer) | Name der Artikelkategorie die als Lackierteil gilt. Leer = Feature deaktiviert |

### Service-Konfiguration (`IDEALAKEWMSService/appsettings.json`)

Neue Sync-Toggles und Parameter:

| Key | Default | Beschreibung |
|-----|---------|--------------|
| `Sync:BomCacheEnabled` | `false` | Aktiviert den BOM-Cache-Sync-Job |
| `Sync:BomCacheWeeks` | `8` | Wie viele Wochen Fertigungstermin in die Zukunft cachen |
| `Sync:BomCacheMaxOrders` | `200` | Maximalanzahl Auftraege im Cache (Sicherheitslimit) |
| `Sync:BomCacheMaxAgeHours` | `24` | Sicherheitsnetz: Eintraege aelter als X Stunden werden bei Sync-Lauf zwingend re-validiert |
| `Sync:CoatingDetectionEnabled` | `false` | Aktiviert separaten Lackierteil-Erkennungs-Sync (rueckwirkend fuer alle offenen) |

---

## BOM-Cache-Sync (Service)

### Neuer Service: `BomCacheSyncService`

Datei: `IDEALAKEWMSService/Services/BomCacheSyncService.cs`

**Methode:** `SyncBomCacheAsync(bool dryRun, CancellationToken ct)` — wird vom `SyncWorker` aufgerufen, **nach** dem Sage-Artikel-Import und **vor** dem Lackierteil-Sync.

**Ablauf:**

1. **Window bestimmen:** Aus `IDEAL_AKE_WMS.dbo.ProductionOrders` alle offenen Auftraege selektieren wo
   ```sql
   IsDone = 0
   AND ProductionDate IS NOT NULL
   AND ProductionDate <= DATEADD(week, @BomCacheWeeks, GETDATE())
   ORDER BY ProductionDate ASC
   TOP @BomCacheMaxOrders
   ```
   Auftraege ohne `ProductionDate` werden uebersprungen, mit Warning geloggt.

2. **Distinct Artikelnummern** ermitteln (mehrere Auftraege koennen dieselbe Artikelnummer haben). Das ist die Liste der zu cachenden Stuecklisten.

3. **Bestehende Header-Hashes laden:** `SELECT Artikelnummer, ContentHash, CachedAt FROM CachedBomHeaders WHERE Artikelnummer IN (...)`. Lookup im RAM.

4. **SAGE-Massen-Query:** Ein einziger Query gegen `[ake].[dbo].[vw_AKE_Kommissionierung_StuecklistenDB]`:
   ```sql
   SELECT Artikelnummer, Position, Baugruppe, Ressourcenummer,
          Bezeichnung1, Bezeichnung2, Menge, Beschaffungsartikel, Artikelgruppe
   FROM [ake].[dbo].[vw_AKE_Kommissionierung_StuecklistenDB]
   WHERE Artikelnummer IN (@Artikelnummer1, @Artikelnummer2, ...)
   ORDER BY Artikelnummer, Position
   ```
   Ergebnisse in `Dictionary<string, List<BomItem>>` per Artikelnummer gruppieren. Source = `"SAGE"`.

5. **OSEON-Fallback:** Fuer alle Artikelnummern die SAGE NICHT geliefert hat (= leere Listen), einzeln per `sp_AKE_Kommissionierung_OseonStuecklistenDB` queryen. Ergebnisse mergen. Source = `"OSEON"`.

6. **Hash berechnen** pro Artikelnummer ueber die sortierte Item-Liste. Algorithmus: `SHA256` ueber String der Form `"Position|Ressourcenummer|Menge|Bezeichnung1|...\n..."` — alle Items joined mit Newline. Hex-encoded.

7. **Vergleich + UPSERT** pro Artikelnummer:
   - Wenn Header existiert UND `ContentHash == newHash` UND `CachedAt > now - MaxAgeHours`: **Skip** (kein Schreibzugriff). Header `CachedAt` nicht updaten — wir wollen wissen wann tatsaechlich was geaendert wurde.
   - Sonst: **Replace**. Im Detail: alte Items (`DELETE FROM CachedBomItems WHERE CachedBomHeaderId = @id`), Header upsert (Update wenn existiert, sonst Insert), neue Items per `SqlBulkCopy` in Temp-Tabelle, dann `INSERT INTO CachedBomItems` aus Temp.

8. **Cleanup verwaister Eintraege:** Am Ende `DELETE FROM CachedBomHeaders WHERE Artikelnummer NOT IN (current window)`. Cascade loescht zugehoerige `CachedBomItems`.

9. **Logging:** Inserted, Updated, Skipped, Errors zaehlen. Logs: `"BOM-Cache-Sync: 12 neu, 5 aktualisiert, 183 unveraendert, 0 Fehler"`.

### Hook bei neuen Auftraegen: `SageImportService.SyncProductionOrdersAsync`

Nach dem `IF EXISTS / ELSE` Branch (ca. Zeile 136), wenn `isInsert == true`, sammeln wir die Artikelnummern (und die `Id` des neu inserteten Auftrags via `SCOPE_IDENTITY()` oder Folgequery) in einer Liste. **Nach** der Sync-Schleife (am Ende der Methode):

- Wenn `Sync:BomCacheEnabled == true` UND `newArticleNumbers.Any()`: rufe `BomCacheSyncService.SyncSpecificArticleNumbersAsync(newArticleNumbers, dryRun, ct)` — eine schmalere Variante die nur fuer diese Artikelnummern syncht (kein Cleanup, kein Window-Filter).
- Wenn zusaetzlich `LackierteilKategorieName` gesetzt UND BOM-Cache lief: rufe `CoatingDetectionService.DetectAndUpdateCoatingFlagsAsync(dryRun, newOrderIds, ct)`.
- Wenn `Sync:BomCacheEnabled == false`: Hook wird **komplett uebersprungen**. Auftraege werden weiterhin angelegt, aber kein BOM-Cache und keine Coating-Erkennung. Der separate Sync-Job (Toggle `Sync:BomCacheEnabled` aktiviert) holt das beim naechsten Lauf nach.

### Performance-Annahmen

- **Window-Query** auf `ProductionOrders`: schneller Index-Seek, ~10ms bei < 10'000 Auftraegen
- **SAGE-Massen-Query** mit `IN` (bis zu 200 Werte): SAGE-View ist nach `Artikelnummer` indiziert, ~1-2 Sekunden fuer alle BOMs (~5'000-10'000 Items)
- **Hash-Berechnung:** in C# auf bereits geladenen Daten, vernachlaessigbar
- **BulkCopy:** ~500ms fuer 10'000 Items
- **Gesamt-Sync:** ~3-5 Sekunden bei einem vollen Sync, ~100ms wenn alles unveraendert (nur Hash-Vergleiche)

---

## Cache-First Read im `BomRepository`

### Neuer Repository-Layer: `IBomCacheRepository`

Datei: `IdealAkeWms/Data/Repositories/BomCacheRepository.cs`

**Interface:**
```csharp
public interface IBomCacheRepository
{
    Task<BomQueryResult?> GetByArticleNumberAsync(string articleNumber);
    Task<List<string>> GetArticleNumbersWithCoatingPartsAsync(string lackierteilCategoryName, List<string> articleNumbers);
}
```

`GetByArticleNumberAsync` liest aus `CachedBomHeaders` + `CachedBomItems` (JOIN). Wenn Header vorhanden → mappt zu `BomQueryResult(Items, Source)`. Wenn nicht vorhanden → `null`.

`GetArticleNumbersWithCoatingPartsAsync` ist die Lackierteil-Erkennung: gibt aus der Liste der Artikelnummern jene zurueck deren BOM mind. ein Item mit `Ressourcenummer` enthaelt das in `Articles` einer `ArticleCategory` mit Name = `lackierteilCategoryName` zugeordnet ist. Ein einziger SQL-Query mit JOINs:

```sql
SELECT DISTINCT h.Artikelnummer
FROM CachedBomHeaders h
INNER JOIN CachedBomItems i ON i.CachedBomHeaderId = h.Id
INNER JOIN Articles a ON a.ArticleNumber = i.Ressourcenummer
INNER JOIN ArticleCategories c ON c.Id = a.ArticleCategoryId
WHERE h.Artikelnummer IN (...)
  AND c.Name = @LackierteilName
```

### `BomRepository` orchestriert Cache-First

Datei: `IdealAkeWms/Data/Repositories/BomRepository.cs`

Aenderung in `GetBomItemsAsync(string articleNumber)`:

```csharp
public async Task<BomQueryResult> GetBomItemsAsync(string articleNumber)
{
    // 1. Cache-First
    var cached = await _bomCacheRepository.GetByArticleNumberAsync(articleNumber);
    if (cached != null && cached.Items.Any())
        return cached;  // Source bleibt SAGE oder OSEON aus Header

    // 2. Live-Query SAGE (bestehender Code)
    var sageItems = await QuerySageAsync(articleNumber);
    if (sageItems.Any())
        return new BomQueryResult(sageItems, "SAGE");

    // 3. Live-Query OSEON (bestehender Code)
    var oseonItems = await QueryOseonAsync(articleNumber);
    if (oseonItems.Any())
        return new BomQueryResult(oseonItems, "OSEON");

    // 4. Keine Daten
    return new BomQueryResult(new List<BomItem>(), "KEINE_DATEN");
}
```

`CachedBomRepository` (5-Min MemoryCache-Decorator) bleibt davor unveraendert.

### DI-Registrierung

```csharp
builder.Services.AddScoped<IBomCacheRepository, BomCacheRepository>();
```

---

## Lackierteil-Erkennung

### Neuer Service-Job: Lackierteil-Erkennung

Datei: `IDEALAKEWMSService/Services/CoatingDetectionService.cs`

**Methode:** `DetectAndUpdateCoatingFlagsAsync(bool dryRun, List<int>? specificOrderIds, CancellationToken ct)`

**Ablauf:**

1. **Setting laden:** `LackierteilKategorieName` aus `AppSettings`. Wenn leer → return mit Warning "Feature inaktiv".

2. **Auftraege bestimmen:**
   - Wenn `specificOrderIds != null`: nur diese (= Hook bei neuen Auftraegen)
   - Sonst: alle offenen Auftraege im BomCache-Window (= rueckwirkender Sync)

3. **Distinct Artikelnummern** der Auftraege ermitteln.

4. **Lackierteil-Lookup:** `IBomCacheRepository.GetArticleNumbersWithCoatingPartsAsync(lackierteilName, artikelnummern)` → Set der Artikelnummern mit Lackierteilen.

5. **`HasCoatingParts` updaten** pro Auftrag: `UPDATE ProductionOrders SET HasCoatingParts = @flag WHERE Id IN (...)`. Bulk-Update via 2 Queries (1x mit `flag=1`, 1x mit `flag=0`).

6. **Wichtig:** `IsCoatingDone` wird **NICHT** vom Sync veraendert — das ist ein User-Feld.

### Integration

**Hook bei neuen Auftraegen:**
- In `SageImportService.SyncProductionOrdersAsync`: nach BomCacheSync der neuen Artikelnummern, rufen wir `CoatingDetectionService.DetectAndUpdateCoatingFlagsAsync(dryRun, newOrderIds, ct)`. Nur wenn Lackierteil-Setting gesetzt UND `Sync:BomCacheEnabled`.

**Separater Sync-Job:**
- Toggle `Sync:CoatingDetectionEnabled` (default false).
- Im `SyncWorker` als eigener Block, **nach** BomCacheSync.
- Ruft `CoatingDetectionService.DetectAndUpdateCoatingFlagsAsync(dryRun, null, ct)` (= alle Auftraege im Window).

### SyncWorker-Reihenfolge

```
1. SageImportService.SyncProductionOrdersAsync()
   ├─ Inserts/Updates ProductionOrders
   └─ HOOK: BomCacheSync fuer neue ArticleNumbers
       └─ HOOK: CoatingDetection fuer neue OrderIds
2. SageImportService.SyncArticlesAsync()
3. OseonSyncService.SyncArticleCategoriesToWmsAsync() (bestehend)
4. BomCacheSyncService.SyncBomCacheAsync() (full window, wenn Sync:BomCacheEnabled)
5. CoatingDetectionService.DetectAndUpdateCoatingFlagsAsync(null) (full window, wenn Sync:CoatingDetectionEnabled)
6. OseonSyncService.SyncOseonProductionOrdersAsync() (bestehend)
7. ... (weitere bestehende Sync-Schritte)
```

---

## UI-Aenderungen

### Settings-Seite (`Views/Settings/Index.cshtml`)

Neuer Eintrag in der "Kommissionierung" Sektion:

```
Lackierteil-Kategorie: [Textfeld] [Speichern]
Hilfe: Name der Artikelkategorie die als Lackierteil gilt. Leer = Feature deaktiviert.
```

Liest/schreibt `LackierteilKategorieName` aus AppSettings.

### Production Orders Index (`Views/ProductionOrders/Index.cshtml`)

**Neue Spalte "Lack-T"** zwischen Liefertermin und Glas:

```html
<th class="text-nowrap text-center" style="width: 55px;">Lack-T</th>
```

Im Body:
```html
<td class="text-center">
    @if (item.HasCoatingParts)
    {
        <form asp-action="ToggleCoatingDone" method="post" class="d-inline">
            @Html.AntiForgeryToken()
            <input type="hidden" name="id" value="@item.Id" />
            <input type="hidden" name="returnUrl" value="@Context.Request.Path@Context.Request.QueryString" />
            <button type="submit" class="btn btn-link p-0 border-0">
                @if (item.IsCoatingDone)
                {
                    <span title="Lackierteile erledigt">✅</span>
                }
                else
                {
                    <span title="Lackierteile offen">⬜</span>
                }
            </button>
        </form>
    }
</td>
```

(Symbol-Variante mit echten Bootstrap-Icons im Implementation Plan ausarbeiten.)

**Beschicht.-Spalte (bestehend, Aenderung):**

Datum nur anzeigen wenn `HasCoatingParts == true`. Rote Markierung NUR wenn `HasCoatingParts && !IsCoatingDone && !IsDone && termin < heute`.

Im Code:
```csharp
if (!order.HasCoatingParts)
{
    item.BeschichtungTermin = null;  // Nicht berechnen, nicht anzeigen
}
else
{
    // bestehende Berechnung
}
```

In der View:
```html
<td class="@(!item.IsDone && !item.IsCoatingDone && item.HasCoatingParts && item.BeschichtungTermin.HasValue && item.BeschichtungTermin.Value < today ? "text-danger fw-bold" : "")">
    @(item.HasCoatingParts ? FormatDateWithKw(item.BeschichtungTermin) : "")
</td>
```

### Controller-Action: `ToggleCoatingDone`

Datei: `Controllers/ProductionOrdersController.cs`

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ToggleCoatingDone(int id, string? returnUrl)
{
    var order = await _productionOrderRepository.GetByIdAsync(id);
    if (order == null) return NotFound();

    order.IsCoatingDone = !order.IsCoatingDone;
    order.ModifiedAt = DateTime.Now;
    order.ModifiedBy = _currentUserService.GetDisplayName();
    order.ModifiedByWindows = _currentUserService.GetWindowsUserName();
    await _productionOrderRepository.UpdateAsync(order);

    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        return Redirect(returnUrl);
    return RedirectToAction(nameof(Index));
}
```

**Kein neuer Filter** — gleiche Berechtigung wie der bestehende `ToggleHasGlass` / `ToggleHasExternalPurchase`-Workflow.

---

## Repositories

### IBomCacheRepository (neu)

```csharp
public interface IBomCacheRepository
{
    Task<BomQueryResult?> GetByArticleNumberAsync(string articleNumber);
    Task<HashSet<string>> GetArticleNumbersWithCoatingPartsAsync(string lackierteilCategoryName, List<string> articleNumbers);
    Task<Dictionary<string, (string Hash, DateTime CachedAt)>> GetHeaderHashesAsync(List<string> articleNumbers);
    Task UpsertBomAsync(string articleNumber, string source, string contentHash, List<BomItem> items);
    Task DeleteOrphansAsync(List<string> currentArticleNumbers);
}
```

### IProductionOrderRepository (erweitert)

Neue Methode:
```csharp
Task<List<ProductionOrder>> GetOpenOrdersInWindowAsync(int weeksAhead, int maxCount);
Task SetCoatingFlagsAsync(Dictionary<int, bool> orderIdToHasCoatingParts);
```

Erste Methode liefert die Window-Auftraege fuer den Sync. Zweite Methode macht den Bulk-Update.

---

## Migration & Daten

### SQL-Migration

Datei: `SQL/40_AddBomCacheAndCoatingDetection.sql`

Inhalt (vereinfachte Skizze):
- `CREATE TABLE CachedBomHeaders` mit Indexes
- `CREATE TABLE CachedBomItems` mit Indexes + FK
- `ALTER TABLE ProductionOrders ADD HasCoatingParts BIT NOT NULL DEFAULT 0`
- `ALTER TABLE ProductionOrders ADD IsCoatingDone BIT NOT NULL DEFAULT 0`
- INSERT in `__EFMigrationsHistory`
- Alles mit `OBJECT_ID`-Guards.

`SQL/00_FreshInstall.sql` analog ergaenzen.

### EF Migration

`dotnet ef migrations add AddBomCacheAndCoatingDetection` — generiert Migration. Timestamps in beiden SQL-Files alignen.

### Initial-Befuellung

Beim ersten Start des Service mit `Sync:BomCacheEnabled = true` wird der Cache initial befuellt. Erwartete Dauer: 5-10 Sekunden fuer 200 Auftraege. Logs informieren ueber Fortschritt.

---

## Zugriffskontrolle

| Bereich | Filter | Rollen |
|---------|--------|--------|
| `ToggleCoatingDone` | (kein expliziter Filter) | Wie bestehende Toggle-Actions (alle eingeloggten User) |
| `BomRepository.GetBomItemsAsync` | (read-only, transparent) | Wie bisher |
| Cache-Tabellen direkt | — | Nur Service + Repository, kein User-Zugriff |

---

## Fallstricke und Designentscheidungen

1. **Cache per Artikelnummer (nicht per Auftrag):** Mehrere Auftraege koennen denselben `ArticleNumber` haben (gleiche Stueckliste). Wir cachen einmal pro Artikelnummer, nicht pro Auftrag.

2. **Hash-basierte Aenderungserkennung:** Da SAGE-View kein `LastChanged` hat, hashen wir den BOM-Inhalt. Bei unveraendertem Hash kein Schreibzugriff. Sicherheitsnetz: `MaxAgeHours` erzwingt periodisches Re-Sync auch bei gleichem Hash (fuer den Fall dass das Hashing einen Fehler hat oder die Items im selben Sekundenbereich kommen).

3. **Filterkriterium ProductionDate:** Auftraege ohne `ProductionDate` werden uebersprungen (mit Warning geloggt). Sollte selten vorkommen.

4. **OSEON-Fallback im Service:** Service ruft erst SAGE-Massen-Query, dann fuer leere Artikelnummern OSEON-SP einzeln. So sind beide Quellen im Cache abgedeckt.

5. **Source pro Header:** `Source` ist pro Stueckliste eindeutig (entweder SAGE oder OSEON). Header-Tabelle ist die natuerliche Stelle dafuer. Beim Lesen kommt die Originalquelle korrekt zurueck.

6. **Lackierteil-Erkennung haengt von `Articles`-Tabelle ab:** Bauteile muessen in unserer `Articles`-Tabelle existieren UND einer Kategorie zugeordnet sein. Bauteile die nur in OSEON existieren werden nicht erkannt. Mitigation: OSEON-Kategorie-Sync (Feature 1) befuellt die Zuordnung automatisch.

7. **Picking-Workflow unveraendert:** Der bestehende `PickingItems`-Mechanismus mit lazy `InitializePickingAsync` aendert sich nicht. Eine spaetere Refaktorierung (PickingItem nur bei tatsaechlichem Pick + FK auf CachedBomItems) ist ein eigenstaendiger Plan.

8. **Performance-Annahme bei Hashing:** Wir hashen ueber sortierte Item-Liste. Wenn SAGE die Items in unterschiedlicher Reihenfolge liefert, koennten Hash-Mismatches entstehen. Loesung: vor Hashing **immer** `ORDER BY Position, Ressourcenummer` in C#.

9. **Cleanup-Kollision:** Wenn Auftrag X aus dem Window faellt aber Auftrag Y mit derselben Artikelnummer noch im Window ist, darf der Cache-Eintrag nicht geloescht werden. Cleanup-Query verwendet daher `WHERE Artikelnummer NOT IN (currentWindow)` mit den **distinct** Artikelnummern aller Window-Auftraege.

10. **Beschichtungsdatum-Logik:** Aktuell wird Beschichtungstermin fuer alle Auftraege berechnet. Mit dem Feature wird er nur berechnet wenn `HasCoatingParts == true`. **Wichtig:** Wenn das Feature deaktiviert ist (`LackierteilKategorieName` leer), **alle** Auftraege haben `HasCoatingParts == false` → Beschichtungstermin wird nirgends mehr angezeigt. **Mitigation:** Wenn `LackierteilKategorieName` leer ist, behandle alle Auftraege als "potentiell Lackierteil" — d.h. Beschichtungstermin wird **immer** berechnet (Verhalten wie heute). Nur wenn Setting gesetzt ist, wird gefiltert. So bleibt das System rueckwaerts-kompatibel.

11. **`IsCoatingDone` zuruecksetzen?** Wenn ein Auftrag im Sync `HasCoatingParts` von `true` auf `false` wechselt (BOM hat sich geaendert, Lackierteile nicht mehr drin), sollte `IsCoatingDone` auch zurueckgesetzt werden? **Entscheidung:** Ja — wenn `HasCoatingParts=false`, dann auch `IsCoatingDone=false` (kein Status zu erledigen). Sync setzt beide Felder atomic.

12. **MemoryCache 5-Min vs. DB-Cache:** Beide Cache-Schichten bleiben. MemoryCache verhindert wiederholte DB-Reads innerhalb von 5 Minuten. DB-Cache ueberlebt App-Restarts und ist die Quelle fuer den Lackierteil-Lookup.

---

## Aenderungen an bestehenden Dateien

| Datei | Aenderung |
|-------|-----------|
| `Models/ProductionOrder.cs` | + `HasCoatingParts`, `IsCoatingDone` |
| `Data/ApplicationDbContext.cs` | + DbSets fuer 2 neue Tabellen + Configs + 2 Spalten auf ProductionOrders |
| `Data/Repositories/BomRepository.cs` | + Cache-First-Logik via `IBomCacheRepository` |
| `Data/Repositories/ProductionOrderRepository.cs` | + `GetOpenOrdersInWindowAsync`, `SetCoatingFlagsAsync` |
| `Controllers/ProductionOrdersController.cs` | + `ToggleCoatingDone` Action, Beschichtungstermin-Berechnung bedingt |
| `Views/ProductionOrders/Index.cshtml` | + Lack-T-Spalte, Beschicht.-Spalte bedingt |
| `Views/Settings/Index.cshtml` | + Eingabefeld `LackierteilKategorieName` |
| `Controllers/SettingsController.cs` | + Save-Action fuer neues Setting |
| `Program.cs` | + DI-Registrierung `IBomCacheRepository` |
| `IDEALAKEWMSService/Services/SageImportService.cs` | + Hook fuer BOM-Cache + Coating-Detection bei neuen Auftraegen |
| `IDEALAKEWMSService/Workers/SyncWorker.cs` | + Aufrufe `BomCacheSyncService` und `CoatingDetectionService` |
| `IDEALAKEWMSService/appsettings.json` | + 5 neue Sync-Settings |
| `SQL/40_AddBomCacheAndCoatingDetection.sql` | Neue Tabellen + Spalten |
| `SQL/00_FreshInstall.sql` | Aktualisieren |
| `CLAUDE.md` | Neue Entities, Repos, Sync-Toggles, Fallstricke |
| `Views/Help/Index.cshtml` | Hilfe fuer Lack-T-Spalte und Lackierteil-Setting |
| `Views/Help/Changelog.cshtml` | v1.5.0-Eintrag |
| `AppVersion.cs` (Web + Service) | 1.5.0 |

---

## Neue Dateien

| Datei | Beschreibung |
|-------|-------------|
| `Models/CachedBomHeader.cs` | Entity |
| `Models/CachedBomItem.cs` | Entity |
| `Data/Repositories/IBomCacheRepository.cs` | Interface |
| `Data/Repositories/BomCacheRepository.cs` | Implementation |
| `IDEALAKEWMSService/Services/IBomCacheSyncService.cs` | Interface |
| `IDEALAKEWMSService/Services/BomCacheSyncService.cs` | Sync-Job |
| `IDEALAKEWMSService/Services/ICoatingDetectionService.cs` | Interface |
| `IDEALAKEWMSService/Services/CoatingDetectionService.cs` | Sync-Job |
| `SQL/40_AddBomCacheAndCoatingDetection.sql` | DB-Migration |
