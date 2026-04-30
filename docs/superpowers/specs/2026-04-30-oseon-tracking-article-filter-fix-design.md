# OSEON Tracking Article-Filter Fix — Design Spec

**Datum:** 2026-04-30
**Branch:** `feature/oseon-reporting`
**Versions-Bump:** v1.7.1 → v1.7.2
**Branch-Baseline:** AppVersion ist auf `feature/oseon-reporting` bereits 1.7.1 (von Task 8 der OSEON-Reporting-Phase). main steht bei 1.7.0.

## 1. Problem

Auf `/Tracking/OseonIndex` blockiert die App, sobald der Benutzer im Artikelnummer-Filter tippt. Ursache: client-seitiger Live-Filter iteriert pro Keystroke über ~150 DOM-Zeilen mit `.classList.add/remove`, ohne Debouncing. Zusätzlich strukturelles UX-Problem: der Filter wirkt nur auf die aktuell geladene Seite (25 Aufträge), nicht auf den Gesamtbestand.

## 2. Ziel

- Performance-Fix: kein DOM-Loop mehr, kein Browser-Freeze.
- UX-Fix: Artikel-Filter sucht im Gesamtbestand mit Pagination.
- Konsistenz: alle Filter (Auftrag, Werkbank, Artikel, ShowFinished, useRelevanceFilter) durchlaufen denselben Form-Submit-Pfad.

## 3. Lösungs-Ansatz

**Filter ins bestehende GET-Form integrieren.** Server bekommt neuen Parameter `filterArticle`, Repository filtert mit `Contains` auf `OseonProductionOrder.ArticleNumber`. Live-JS-Filter wird ersatzlos entfernt. QR-Scanner für Artikel triggert Form-Submit (analog Auftragsnummer).

DB bekommt einen Index auf `ArticleNumber`. Klassischer B-Tree, hilft `Contains` (nicht-SARGable) nur eingeschränkt — primärer Wert: Vorbereitung für künftige Equality-/StartsWith-Lookups, niedriges Schreib-Overhead bei seltenem Article-Update via Sync.

## 4. AG-Level-Filter-Verhalten (explizit)

Der Filter wirkt auf **Order-Ebene**. Wenn `OseonProductionOrder.ArticleNumber` matcht, wird der Auftrag inkl. **aller** zugehörigen `OseonWorkOperation` angezeigt. Wenn nicht, verschwindet der Auftrag komplett (samt seinen AGs) — kein AG-Level-Filter, kein partielles Anzeigen einzelner AGs unter einem nicht-passenden Order-Header.

## 5. Architektur & Komponenten

### 5.1 Geänderte Files

- `IdealAkeWms/Controllers/TrackingController.cs:185` — `OseonIndex` action: neuer Parameter `string? filterArticle = null`, an Repository durchgereicht, an ViewModel zurückgegeben.
- `IdealAkeWms/Data/Repositories/IOseonProductionOrderRepository.cs:15` — `GetPagedAsync` Signatur: neuer optionaler Parameter `string? articleNumber = null` **am Ende** (nach `relevantOperationNames`).
- `IdealAkeWms/Data/Repositories/OseonProductionOrderRepository.cs:36` — Where-Klausel um `ArticleNumber.Contains(...)` mit Null-Check erweitern.
- `IdealAkeWms/Models/ViewModels/OseonTrackingViewModel.cs` — neue Property `string? FilterArticle { get; set; }`. Property-Name konsistent zu `MovementHistoryViewModel.FilterArticle` und `StockOverviewViewModel.FilterArticle`.
- `IdealAkeWms/Views/Tracking/OseonIndex.cshtml`:
  - Filter-Input bekommt `name="filterArticle"` und kommt INS `<form method="get">`.
  - `value="@Model.FilterArticle"` damit Wert nach Submit erhalten bleibt.
  - Inline-`<style>`-Block mit `.article-filter-hidden` (Zeilen ~348–350) entfernen.
  - Live-Filter-JS (Zeilen ~459–525, inkl. `applyArticleFilter()` und Keyboard-Handler) ersatzlos entfernen.
  - QR-Scanner-Callback für Artikel triggert Form-Submit (analog `btnScanCustomerOrder`).
  - `data-article-number` Attribute auf `<tr>`-Zeilen entfernen (toter Anchor nach JS-Cleanup).
- `IdealAkeWms/Data/ApplicationDbContext.cs:483-513` — `HasIndex(o => o.ArticleNumber).HasDatabaseName("IX_OseonProductionOrders_ArticleNumber")` für `OseonProductionOrder`.
- `IdealAkeWms/Migrations/<timestamp>_AddOseonArticleNumberIndex.cs` — EF-generierte Migration.
- `SQL/50_AddOseonArticleNumberIndex.sql` — idempotenter SQL-Script (Reihen-Nummer 50 schließt direkt an 49 = `AddOseonReportingHorizonSetting` aus diesem Branch an).
- `SQL/00_FreshInstall.sql:375-380` — neues `CREATE INDEX IX_OseonProductionOrders_ArticleNumber` direkt nach den anderen `OseonProductionOrders`-Indexes.
- `IdealAkeWms.Tests/Repositories/OseonProductionOrderRepositoryArticleFilterTests.cs` — neue Test-Datei mit 5 Tests (siehe §10).
- `IdealAkeWms/AppVersion.cs` + `IDEALAKEWMSService/AppVersion.cs` — Version `1.7.2`, Date `2026-04-30`.
- `IdealAkeWms/Views/Help/Index.cshtml:262-272` — Filter-Liste für OSEON-Tracking um `<dt>Artikelnummer</dt><dd>...</dd>` erweitern.
- `IdealAkeWms/Views/Help/Changelog.cshtml` — v1.7.2-Block einfügen (über v1.7.1).
- `PROJECT_STATUS.md` — v1.7.2-Eintrag hinzufügen.
- `docs/TESTSZENARIEN.md` — neuer Bereich 2 mit TS-2.1..TS-2.4.

### 5.2 Files NICHT angefasst

- `CLAUDE.md` — keine Änderung nötig (Filter-Set wird dort nicht aufgelistet, AppSettings-Tabelle unverändert, keine neuen Fallstricke).

## 6. Repository-Signatur (Backward-compatibility)

**Aktuell** (auf branch):
```csharp
Task<OseonPagedResult> GetPagedAsync(
    string? searchTerm,
    string? workplaceName,
    bool showFinished,
    int page,
    int pageSize,
    HashSet<string>? relevantOperationNames = null);
```

**Neu** — `articleNumber` **am Ende** mit Default `null`:
```csharp
Task<OseonPagedResult> GetPagedAsync(
    string? searchTerm,
    string? workplaceName,
    bool showFinished,
    int page,
    int pageSize,
    HashSet<string>? relevantOperationNames = null,
    string? articleNumber = null);
```

**Begründung:** Diese Position bricht KEINEN der 7 bestehenden positionellen Test-Aufrufe (`OseonProductionOrderRepositoryTests.cs:148/153/177/200/223/247/273`), weil sie alle vor dem neuen Parameter aufhören. Der Controller-Call wird mit `articleNumber: filterArticle` als named argument erweitert. Trade-off: semantische Gruppierung „searchTerm/workplaceName/articleNumber zusammen" wird nicht eingehalten — akzeptiert wegen minimalem Blast-Radius.

**Where-Klausel-Erweiterung** in der Implementation:
```csharp
if (!string.IsNullOrWhiteSpace(articleNumber))
{
    var artTerm = articleNumber.Trim();
    baseQuery = baseQuery.Where(o => o.ArticleNumber != null
        && o.ArticleNumber.Contains(artTerm));
}
```

## 7. View-Änderung (konkret)

**Vorher (außerhalb Form, ~Zeile 25):**
```html
<input id="filterArticle" placeholder="Artikelnummer scannen..." />
<button id="btnScanArticle" type="button">QR</button>
```

**Nachher (im Form, neben filterCustomerOrder):**
```html
<input id="filterArticle" name="filterArticle"
       value="@Model.FilterArticle"
       class="form-control form-control-sm"
       placeholder="Artikelnummer..." />
<button id="btnScanArticle" type="button" class="btn btn-sm btn-outline-secondary">QR</button>
```

**QR-Scanner-Callback** (analog zum bestehenden Customer-Order-Pattern, ~Zeile 528):
```javascript
initTextInputScanner('btnScanArticle', 'filterArticle', 'article', function() {
    var form = document.getElementById('filterArticle').closest('form');
    if (form) form.submit();
});
```

**Zu entfernen:**
- Inline-`<style>`-Block mit `.article-filter-hidden` (Zeilen ~348–350)
- `applyArticleFilter()`-Funktion + Event-Listeners (Zeilen ~459–525)
- `data-article-number`-Attribute auf den `<tr>`-Render-Zeilen
- Falls vorhanden: Keyboard-Handler an `#filterArticle`, der `applyArticleFilter()` aufruft

## 8. UX-Trade-off (explizit)

QR-Scanner triggert nun Form-Submit → vollständiger Page-Reload mit 25-Zeilen-Pagination des gefilterten Ergebnisses. Vorher: instant-Hide einzelner Zeilen ohne Server-Roundtrip.

Der Trade-off lohnt sich, weil:
- Live-Filter blockierte den Browser bis zum Freeze (das Problem)
- Live-Filter sah nur die aktuelle Seite (UX-Mangel)
- Form-Submit ist bei <500ms Server-Latenz schneller als der gefrorene Browser

## 9. DB-Index

Migration:
```csharp
migrationBuilder.CreateIndex(
    name: "IX_OseonProductionOrders_ArticleNumber",
    table: "OseonProductionOrders",
    column: "ArticleNumber");
```

`SQL/50_AddOseonArticleNumberIndex.sql` (idempotent):
```sql
-- Phase: OSEON Tracking Article Filter Fix v1.7.2
-- Idempotent: Index auf ArticleNumber für Filter-Performance.

IF NOT EXISTS (SELECT 1 FROM sys.indexes
    WHERE name = 'IX_OseonProductionOrders_ArticleNumber'
      AND object_id = OBJECT_ID('dbo.OseonProductionOrders'))
BEGIN
    CREATE INDEX IX_OseonProductionOrders_ArticleNumber
        ON dbo.OseonProductionOrders(ArticleNumber);
END
GO
```

`SQL/00_FreshInstall.sql:375-380` — direkt nach den anderen `OseonProductionOrders`-Indizes (nach `IX_OseonProductionOrders_WorkplaceName`):
```sql
CREATE INDEX [IX_OseonProductionOrders_ArticleNumber] ON [OseonProductionOrders]([ArticleNumber]);
```

**Index-Honesty:** `Contains`-Suche ist nicht-SARGable; SQL Server kann den Index meist nur als Index-Scan nutzen (statt Table-Scan). Bei der erwarteten Tabellengröße (~10k Zeilen) bringt das marginale Performance-Vorteile. Der Index ist primär eine Vorbereitung — falls künftig Volltextsuche, StartsWith oder Equality-Lookups dazukommen, ist die Infrastruktur bereit. Schreib-Overhead vernachlässigbar (OSEON-Sync schreibt Aufträge selten).

## 10. Tests

**Repository-Tests (5 neue, in `OseonProductionOrderRepositoryArticleFilterTests.cs`):**

1. `Filter_ByArticleNumber_ReturnsExactMatch` — `articleNumber: "ART-100"` findet Auftrag mit `ArticleNumber = "ART-100"`.
2. `Filter_ByArticleNumber_ReturnsContainsMatch` — `articleNumber: "100"` findet Aufträge mit `ArticleNumber = "ART-100"` und `ART-1001`.
3. `Filter_ByArticleNumber_IgnoresOrdersWithNullArticleNumber` — Aufträge mit `ArticleNumber = null` werden ausgeschlossen, andere Match-Aufträge bleiben sichtbar (kein NRE).
4. `Filter_ByArticleNumber_WhitespaceOnly_TreatedAsNoFilter` — `articleNumber: "   "` wird wie kein Filter behandelt (alle Aufträge werden geliefert).
5. `Filter_ByArticleNumber_CombinedWithSearchTermAndWorkplace_AllConjunctive` — Kombinierter Filter (Artikel + Auftragsnummer + Werkbank) wirkt konjunktiv.

**Hinweis zur EF-InMemory-Collation:** SQL Server `Contains` ist case-insensitive (default Collation), EF-InMemory ist case-sensitive. Tests verwenden konsistente Klein-/Großschreibung im Match-String und in den Seed-Daten, um den Unterschied zu vermeiden.

**Manuelle Testszenarien (TESTSZENARIEN.md, Bereich 2):**

- TS-2.1: Artikelnummer eingeben + Submit → nur passende Aufträge sichtbar; Filter-Wert bleibt im Input erhalten.
- TS-2.2: Artikel + Werkbank + Auftragsnummer kombinieren → Schnittmenge.
- TS-2.3: QR-Scan eines Artikel-Codes löst automatisch Form-Submit aus, Filter wird angewendet.
- TS-2.4: Reset-Link entfernt alle Filter (auch Artikel) und liefert die volle Liste zurück.

(Ein expliziter Performance-Test entfällt — Acceptance-Kriterium ist „kein Browser-Freeze beim Tippen".)

## 11. Out of Scope

- Volltextindex / Lucene
- Live-Filter mit Debouncing (verworfen — Form-Submit ist sauberer)
- Artikel-Beschreibungs-Suche (zusätzliche Spalte)
- Filter-Memory pro User
- Phonetische Suche / Fuzzy-Match
- Server-Side StartsWith-Modus statt Contains (Y-A-G-N-I)

## 12. Risiken & Mitigationen

- **Risiko:** EF-Migration „Pending model changes" wenn nur SQL-Script angelegt wird
  - **Mitigation:** EF-Migration UND SQL-Script erzeugen, Snapshot synchron halten.
- **Risiko:** Bestehende `GetPagedAsync`-Aufrufe brechen
  - **Mitigation:** `articleNumber` als letzter Parameter mit Default `null` — alle 7 Test-Calls bleiben kompilierfähig.
- **Risiko:** `OseonProductionOrder.ArticleNumber = null` → NullRef
  - **Mitigation:** Null-Check vor `Contains` in der LINQ-Where-Klausel.
- **Risiko:** SQL-Server vs. EF-InMemory Case-Sensitivity divergiert
  - **Mitigation:** Tests verwenden konsistente Schreibweise; im UAT explizit Mixed-Case prüfen.
- **Risiko:** QR-Scan-Page-Reload wirkt langsamer als Live-Filter (UX-Wahrnehmung)
  - **Mitigation:** Akzeptiert; Vorgänger-Verhalten war nicht funktional (Freeze). Server-Roundtrip <500ms vs. Browser-Freeze ist eindeutige Verbesserung.

## 13. Tasks-Dekomposition (für Plan)

5 Tasks erwartet:
1. Repository: Signatur erweitern + Where-Klausel + 5 Tests
2. Controller + ViewModel: `filterArticle` Parameter durchschleifen, ViewModel-Property `FilterArticle`
3. View + JS-Cleanup: Form-Integration, Live-Filter weg, Inline-CSS weg, QR-Submit, dead Attributes weg
4. EF-Migration + SQL/50 + FreshInstall + ApplicationDbContext
5. AppVersion + Help/Index + Changelog + PROJECT_STATUS + TESTSZENARIEN
