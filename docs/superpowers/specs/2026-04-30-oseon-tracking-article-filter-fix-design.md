# OSEON Tracking Article-Filter Fix — Design Spec

**Datum:** 2026-04-30
**Branch:** `feature/oseon-reporting`
**Versions-Bump:** v1.7.1 → v1.7.2

## 1. Problem

Auf `/Tracking/OseonIndex` blockiert die App, sobald der Benutzer im Artikelnummer-Filter tippt. Ursache: client-seitiger Live-Filter iteriert pro Keystroke über ~150 DOM-Zeilen mit `.classList.add/remove`, ohne Debouncing. Zusätzlich strukturelles UX-Problem: der Filter wirkt nur auf die aktuell geladene Seite (25 Aufträge), nicht auf den Gesamtbestand.

## 2. Ziel

- Performance-Fix: kein DOM-Loop mehr, kein Browser-Freeze.
- UX-Fix: Artikel-Filter sucht im Gesamtbestand mit Pagination.
- Konsistenz: alle Filter (Auftrag, Werkbank, Artikel, ShowFinished, useRelevanceFilter) durchlaufen denselben Form-Submit-Pfad.

## 3. Lösungs-Ansatz

**Filter ins bestehende GET-Form integrieren** (Variante a). Server bekommt neuen Parameter `filterArticle`, Repository filtert mit `Contains` auf `OseonProductionOrder.ArticleNumber`. Live-JS-Filter wird ersatzlos entfernt. QR-Scanner für Artikel triggert nun Form-Submit (analog zu Auftragsnummer).

DB bekommt einen Index auf `ArticleNumber` (klassischer B-Tree-Index — `Contains`-Match nutzt ihn nur bei Prefix-Treffern; trotzdem sinnvoll wegen der häufigen Equality-Vergleiche und als Vorbereitung auf eine spätere Volltextsuche, wenn Bedarf entsteht).

## 4. Architektur & Komponenten

### 4.1 Geänderte Files

- `IdealAkeWms/Controllers/TrackingController.cs` — `OseonIndex` action: neuer Parameter `string? filterArticle`, an Repository durchgereicht.
- `IdealAkeWms/Data/Repositories/IOseonProductionOrderRepository.cs` — `GetPagedAsync` Signatur: neuer Parameter `string? articleNumber`.
- `IdealAkeWms/Data/Repositories/OseonProductionOrderRepository.cs` — Where-Klausel um `ArticleNumber.Contains(...)` erweitern.
- `IdealAkeWms/Views/Tracking/OseonIndex.cshtml`:
  - Filter-Input bekommt `name="filterArticle"` und kommt INS Form.
  - Live-Filter-JS (Zeilen ~459–525) entfernt.
  - QR-Scanner-Callback für Artikel triggert Form-Submit.
  - `data-article-number` Attribute auf Zeilen können bleiben (Anchor für künftige Features), aber sind nicht mehr nötig.
- `IdealAkeWms/Data/ApplicationDbContext.cs` — `HasIndex(o => o.ArticleNumber)` für `OseonProductionOrder`.
- `IdealAkeWms/Migrations/<timestamp>_AddOseonArticleNumberIndex.cs` — EF Migration.
- `SQL/50_AddOseonArticleNumberIndex.sql` — idempotenter SQL-Migration-Script.
- `SQL/00_FreshInstall.sql` — Index-CREATE-Statement aufnehmen.
- `IdealAkeWms.Tests/Repositories/OseonProductionOrderRepositoryArticleFilterTests.cs` — neuer Test-File für den Artikel-Filter.

### 4.2 Versions + Docs

- `IdealAkeWms/AppVersion.cs` + `IDEALAKEWMSService/AppVersion.cs` — v1.7.2, Date 2026-04-30.
- `IdealAkeWms/Views/Help/Changelog.cshtml` — v1.7.2-Eintrag.
- `PROJECT_STATUS.md` — v1.7.2-Eintrag.
- `docs/TESTSZENARIEN.md` — neue Bereich-2-Szenarien für Artikelfilter.

## 5. Datenfluss

```
User tippt Artikelnummer + Submit
  → GET /Tracking/OseonIndex?filterArticle=ABC&filterCustomerOrder=...&...
  → TrackingController.OseonIndex(filterArticle, filterCustomerOrder, ...)
  → repo.GetPagedAsync(searchTerm, workplaceName, articleNumber, showFinished, page, 25, relevantOps)
  → SQL: WHERE
       (CustomerOrderNumber LIKE '%' + @search + '%' OR OseonOrderNumber LIKE '%' + @search + '%')
       AND ArticleNumber LIKE '%' + @article + '%'
       AND WorkplaceName = @workplace
       AND OseonStatus NOT IN (90, 95)  -- wenn !showFinished
  → ORDER BY CustomerOrderNumber, OseonOrderNumber
  → 25 Treffer (mit Pagination)
  → Razor render → Browser
```

## 6. Repository-Änderung

Aktuelle Signatur:
```csharp
Task<OseonPagedResult> GetPagedAsync(
    string? searchTerm,
    string? workplaceName,
    bool showFinished,
    int page,
    int pageSize,
    HashSet<string>? relevantOperationNames = null);
```

Neue Signatur:
```csharp
Task<OseonPagedResult> GetPagedAsync(
    string? searchTerm,
    string? workplaceName,
    string? articleNumber,           // NEU
    bool showFinished,
    int page,
    int pageSize,
    HashSet<string>? relevantOperationNames = null);
```

Where-Klausel-Erweiterung:
```csharp
if (!string.IsNullOrWhiteSpace(articleNumber))
{
    var artTerm = articleNumber.Trim();
    baseQuery = baseQuery.Where(o => o.ArticleNumber != null
        && o.ArticleNumber.Contains(artTerm));
}
```

## 7. View-Änderung

**Vorher (außerhalb Form, Zeile ~25):**
```html
<input id="filterArticle" placeholder="Artikelnummer scannen..." />
<button id="btnScanArticle">QR</button>
```

**Nachher (im Form):**
```html
<input id="filterArticle" name="filterArticle" value="@Model.FilterArticle"
       placeholder="Artikelnummer..." />
<button id="btnScanArticle" type="button">QR</button>
```

QR-Scanner-Callback ändert sich:
```javascript
initTextInputScanner('btnScanArticle', 'filterArticle', 'article', function() {
    var form = document.getElementById('filterArticle').closest('form');
    if (form) form.submit();
});
```

Live-Filter-Code (Zeilen ~459–525) wird **komplett entfernt**, ebenso die zugehörige CSS-Klasse `.article-filter-hidden` falls separat definiert.

## 8. ViewModel-Änderung

Aktuelles ViewModel: `OseonTrackingViewModel` (oder ähnlich — siehe Code). Neues nullable-string-Property `FilterArticle`. Ist value="@Model.FilterArticle" auf dem Input setzbar, damit der Wert nach Submit erhalten bleibt.

## 9. DB-Index

Migration:
```csharp
migrationBuilder.CreateIndex(
    name: "IX_OseonProductionOrders_ArticleNumber",
    table: "OseonProductionOrders",
    column: "ArticleNumber");
```

SQL-Script (idempotent):
```sql
IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = 'IX_OseonProductionOrders_ArticleNumber'
      AND object_id = OBJECT_ID('dbo.OseonProductionOrders'))
BEGIN
    CREATE INDEX IX_OseonProductionOrders_ArticleNumber
        ON dbo.OseonProductionOrders(ArticleNumber);
END
GO
```

`Contains`-Suche ist nicht-SARGable (Index hilft nur begrenzt), aber:
- bei kurzen Tabellen (~10k Zeilen) trotzdem schnell genug
- der Index wird häufig genutzt: alle Equality-Lookups, Sortierungen, künftige StartsWith-Suche
- vorbereitend für späteres Volltext-Catalog (out of scope)

## 10. Tests

**Repository-Tests (3 neue):**
- `Filter_ByArticleNumber_ReturnsMatch` — exakte Übereinstimmung wird gefunden
- `Filter_ByArticleNumber_ContainsMatch` — Substring-Match (z.B. „123" findet „A-1234-X")
- `Filter_ByArticleNumber_CombinedWithSearchTerm` — Artikelfilter UND Auftragsfilter wirken konjunktiv

**Manueller Test (TESTSZENARIEN Bereich 2):**
- TS-2.1: Artikelnummer eingeben + Submit → nur passende Aufträge sichtbar
- TS-2.2: Artikelnummer + Werkbank kombinieren → Schnittmenge
- TS-2.3: QR-Scan auslöst Submit (Filter wird angewendet ohne weiteren Klick)
- TS-2.4: Reset entfernt alle Filter (auch Artikel)
- TS-2.5: Performance-Test: Artikel-Filter mit großem Datenbestand → unter 2 Sekunden

## 11. Out of Scope

- Volltextindex / Lucene
- Live-Filter mit Debouncing (verworfen — Form-Submit ist sauberer)
- Artikelnamen-Suche (zusätzliche Spalte) — nur ArticleNumber filtern
- Filter-Memory pro User
- Phonetische Suche / Fuzzy-Match

## 12. Risiken & Mitigationen

- **Risiko:** EF-Migration „Pending model changes" wenn Index nur per SQL angelegt wird
  - **Mitigation:** Migration mit `dotnet ef migrations add` generieren, Snapshot synchron halten.
- **Risiko:** Tests, die `GetPagedAsync` aufrufen, brechen wegen neuer Signatur
  - **Mitigation:** Default-Wert `articleNumber = null` an passende Stelle in Signatur — wird durch optional-Parameter konsumiert; alle bestehenden Aufrufe bleiben kompilierfähig.
- **Risiko:** `OseonProductionOrder.ArticleNumber` ist nullable → NullRef wenn Index leer
  - **Mitigation:** Where-Klausel checkt `o.ArticleNumber != null` vor `Contains`.
