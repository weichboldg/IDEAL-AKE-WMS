# Artikelkategorien & Merkmale — Design Spec

## Ziel

Artikelkategorien (aus OSEON synchronisiert oder manuell gepflegt) und frei definierbare Merkmale (Boolean/Dropdown) pro Artikel einfuehren. Merkmale und Kategorien sollen in der Artikel-Uebersicht als filterbare Spalten und in der Stueckliste (BOM) sichtbar sein.

## Architektur

Zwei unabhaengige Subsysteme:

1. **Artikelkategorien** — Eigene Tabelle `ArticleCategories`, referenziert per FK vom Artikel. Kategorien werden per OSEON-Sync automatisch angelegt und zugeordnet. Zusaetzlich manuell wartbar unter Stammdaten.

2. **Artikelmerkmale (EAV-Pattern)** — Drei Tabellen: Merkmal-Definitionen, Dropdown-Vorgabewerte, Artikel-Merkmal-Werte. Admin definiert Merkmale, Werte werden pro Artikel in der Edit-Seite gepflegt. Datenstruktur ist fuer kuenftigen Sync aus OSEON/Sage vorbereitet.

---

## Datenmodell

### ArticleCategory (neu)

| Feld | Typ | Constraint | Beschreibung |
|------|-----|-----------|--------------|
| Id | int | PK, Identity | |
| Name | string(200) | Required, Unique | z.B. "Blechtafel_AKE", "Lackierteile" |
| Description | string(500) | Nullable | Bemerkung (aus OSEON oder manuell) |
| OseonTyp | int? | Nullable | OSEON ArtikelKategorie.Typ (1,2,4,6) |
| Source | string(50) | Nullable | "OSEON" fuer sync-erstellte, null fuer manuelle |
| Audit-Felder | | AuditableEntity | CreatedAt/By, ModifiedAt/By |

### Article (erweitert)

| Feld | Typ | Constraint | Beschreibung |
|------|-----|-----------|--------------|
| ArticleCategoryId | int? | FK → ArticleCategories, Nullable | Zugeordnete Kategorie |
| Navigation: ArticleCategory | | | |

OnDelete: `SetNull` — beim Loeschen einer Kategorie wird die Zuordnung entfernt, Artikel bleibt erhalten.

### ArticleAttributeDefinition (neu)

| Feld | Typ | Constraint | Beschreibung |
|------|-----|-----------|--------------|
| Id | int | PK, Identity | |
| Name | string(200) | Required, Unique | z.B. "Laserschnitt", "Material" |
| AttributeType | int (Enum) | Required | 0=Boolean, 1=Dropdown |
| SortOrder | int | Default 0 | Reihenfolge in UI (Index-Spalten, Edit-Felder) |
| IsActive | bool | Default true | Deaktiviert: unsichtbar in UI, Werte bleiben in DB |
| SyncSource | string(50) | Nullable | Vorbereitung: "OSEON", "SAGE", null=manuell |
| SyncFieldName | string(200) | Nullable | Vorbereitung: Quellfeld-Name fuer kuenftigen Sync |
| Audit-Felder | | AuditableEntity | |

Enum `AttributeType`:
```csharp
public enum AttributeType
{
    Boolean = 0,
    Dropdown = 1
}
```

### ArticleAttributeOption (neu)

| Feld | Typ | Constraint | Beschreibung |
|------|-----|-----------|--------------|
| Id | int | PK, Identity | |
| ArticleAttributeDefinitionId | int | FK, Required | Gehoert zu welchem Merkmal |
| Value | string(200) | Required | z.B. "Alu", "Stahl", "Edelstahl" |
| SortOrder | int | Default 0 | Reihenfolge im Dropdown |

OnDelete: `Cascade` — beim Loeschen der Definition werden auch Optionen geloescht.
Loeschen einer einzelnen Option: Nur wenn keine Artikel diese Option verwenden (UI-Sperre).

### ArticleAttributeValue (neu)

| Feld | Typ | Constraint | Beschreibung |
|------|-----|-----------|--------------|
| Id | int | PK, Identity | |
| ArticleId | int | FK → Articles, Required | |
| ArticleAttributeDefinitionId | int | FK → ArticleAttributeDefinitions, Required | |
| BooleanValue | bool? | Nullable | Wert fuer Boolean-Typ |
| SelectedOptionId | int? | FK → ArticleAttributeOptions, Nullable | Wert fuer Dropdown-Typ |
| Audit-Felder | | AuditableEntity | |

Unique Constraint: `(ArticleId, ArticleAttributeDefinitionId)` — max. 1 Wert pro Artikel pro Merkmal.
OnDelete ArticleAttributeDefinition: `Cascade` — Merkmal geloescht → alle Werte weg.
OnDelete Article: `Cascade` — Artikel geloescht → alle Merkmal-Werte weg.
OnDelete SelectedOption: `SetNull` — Option geloescht → Wert wird null (nur theoretisch, UI verhindert Loeschen wenn in Verwendung).

### Indexes

```
ArticleCategories: IX_ArticleCategories_Name (Unique)
Articles: IX_Articles_ArticleCategoryId
ArticleAttributeDefinitions: IX_ArticleAttributeDefinitions_Name (Unique)
ArticleAttributeOptions: IX_ArticleAttributeOptions_DefinitionId
ArticleAttributeValues: IX_ArticleAttributeValues_ArticleId_DefinitionId (Unique, Composite)
ArticleAttributeValues: IX_ArticleAttributeValues_ArticleId
```

---

## UI

### Artikel-Index (erweitert)

Die bestehende Artikel-Uebersicht erhaelt:

- **Neue Spalte "Kategorie"** (nach Artikelgruppe) — zeigt `ArticleCategory.Name`. Spaltenfilter via `table-filter.js` (`data-filterable`, `data-col`).
- **Dynamische Spalten pro aktivem Merkmal** — gerendert per Razor-Loop ueber aktive `ArticleAttributeDefinitions` (sortiert nach `SortOrder`). Boolean als "Ja"/"Nein", Dropdown als Klartext. Alle mit `data-filterable`.
- **Tabelle benoetigt `filterable-table` CSS-Klasse** — aktuell fehlt diese auf der Artikel-Tabelle.

Performance: Alle Merkmal-Werte fuer die angezeigten Artikel werden per Batch-Query geladen (ein Query fuer alle Artikel-IDs × alle aktiven Definitionen), nicht pro Artikel.

### Artikel-Edit (erweitert)

Neues `ArticleEditViewModel`:
```csharp
public class ArticleEditViewModel
{
    public Article Article { get; set; }
    public List<ArticleCategory> Categories { get; set; }  // Dropdown-Auswahl
    public List<AttributeEditItem> Attributes { get; set; } // Dynamische Felder
}

public class AttributeEditItem
{
    public int DefinitionId { get; set; }
    public string Name { get; set; }
    public AttributeType AttributeType { get; set; }
    public bool? BooleanValue { get; set; }
    public int? SelectedOptionId { get; set; }
    public List<ArticleAttributeOption> Options { get; set; } // Fuer Dropdown
}
```

In der Edit-View:
- **Kategorie-Dropdown** (nach Artikelgruppe, vor Meldebestand) — alle Kategorien, leer = keine
- **Merkmal-Sektion** (nach den Stamm-Feldern, eigene Card) — dynamisch gerendert:
  - Boolean → Checkbox
  - Dropdown → `<select>` mit Vorgabewerten + leere Option

Controller-POST: Liest Kategorie-ID aus Form, iteriert ueber Merkmal-Felder (`Attributes[0].BooleanValue`, `Attributes[0].SelectedOptionId`), erstellt/aktualisiert/loescht `ArticleAttributeValue`-Eintraege.

### Artikel-Info (erweitert)

Zeigt Kategorie und Merkmal-Werte als Read-Only `<dl>` unter den Stammdaten an.

### Stueckliste / BOM (erweitert)

In der BOM-Ansicht (`Views/Picking/Bom.cshtml`):
- Neue Spalte **"Kategorie"** neben Artikelgruppe — zeigt die Kategorie des BOM-Artikels (Ressourcenummer → Article → ArticleCategory.Name).
- Merkmal-Spalten in BOM: **Nicht eingeplant** (zu viele Spalten in der ohnehin breiten Tabelle). Kann spaeter ergaenzt werden.
- Daten werden per Batch-Lookup geladen (analog zu `GetStockByArticleNumbersAsync`): Alle Artikelnummern der BOM → ein Query fuer Kategorien.

### Artikelkategorien-Seite (neu, Admin)

Eigener Menuepunkt unter **Stammdaten → Artikelkategorien**. Zugriff: `[RequireMasterDataAccess]` (admin, masterdata).

Layout aehnlich OseonOperationConfig:
- **Tabelle** (links, col-lg-8): Name, Beschreibung, Quelle (Badge: "OSEON" blau, "Manuell" grau), Anzahl zugeordneter Artikel, Bearbeiten/Loeschen
- **Formular** (rechts, col-lg-4): Name + Beschreibung eingeben, Erstellen-Button
- **Inline-Edit**: Name + Beschreibung aenderbar, Speichern-Button
- **Loeschen**: Nur wenn keine Artikel zugeordnet. Sonst Hinweis "X Artikel verwenden diese Kategorie".
- OSEON-Kategorien: Bearbeitbar (Name/Beschreibung), aber Loeschen zeigt Warnung "Wird beim naechsten Sync ggf. neu erstellt".

### Artikelmerkmale-Seite (neu, Admin)

Eigener Menuepunkt unter **Stammdaten → Artikelmerkmale**. Zugriff: `[RequireMasterDataAccess]` (admin, masterdata).

Layout:
- **Tabelle**: Name, Typ (Badge: "Boolean"/"Dropdown"), Reihenfolge, Aktiv (Ja/Nein), Anzahl Werte, Aktionen
- **Formular**: Name, Typ-Dropdown (Boolean/Dropdown), Reihenfolge (auto MAX+1), Erstellen-Button
- **Inline-Edit**: Name, Reihenfolge, Aktiv-Toggle
- **Dropdown-Optionen**: Aufklappbarer Bereich pro Merkmal (Chevron). Optionen-Liste mit Drag-Sort oder Reihenfolge-Nummer + Hinzufuegen/Loeschen.
  - Option loeschen: Nur wenn keine Artikel diesen Wert verwenden (UI-Sperre mit Hinweis).
- **Deaktivieren** statt Loeschen (wenn Werte existieren): Merkmal verschwindet aus Artikel-Index/Edit, Werte bleiben in DB.
- **Physisches Loeschen**: Nur wenn keine Werte existieren. Sonst: "Merkmal wird verwendet — bitte zuerst deaktivieren."

---

## OSEON-Sync: Artikelkategorien

### Neuer Toggle

`Sync:OseonArticleCategoryEnabled` (default `false`) in `IDEALAKEWMSService/appsettings.json`.

### Sync-Logik (neue Methode in OseonSyncService)

`SyncArticleCategoriesToWmsAsync()`:

1. **Kategorien synchronisieren:**
   - Query: `SELECT Name, Bemerkung, Typ FROM ArtikelKategorie` auf OSEON-DB
   - Fuer jede Kategorie: Wenn nicht in `ArticleCategories` vorhanden → INSERT mit `Source="OSEON"`
   - Wenn vorhanden: Update `Description` und `OseonTyp` (Name ist Unique Key)

2. **Artikel-Zuordnung synchronisieren:**
   - Query: `SELECT Name AS Artikelnummer, Kategorie AS Artikelkategorie FROM Artikel WHERE Kategorie != ''` auf OSEON-DB
   - Nur Artikel die in unserer `Articles`-Tabelle existieren (INNER JOIN auf `ArticleNumber`)
   - Lookup `ArticleCategory` per Name → setze `ArticleCategoryId` am Artikel
   - Bulk-Update via MERGE oder UPDATE...FROM

3. **Kein Loeschen**: Manuell erstellte Kategorien (Source != "OSEON") werden nicht angefasst. OSEON-Kategorien die nicht mehr in OSEON existieren bleiben erhalten (koennen manuell geloescht werden).

### Integration in SyncWorker

Nach dem bestehenden OSEON-Tracking-Block:
```
[Sync:OseonArticleCategoryEnabled] → OseonSyncService.SyncArticleCategoriesToWmsAsync()
```

### SQL-Migration

- Neue Tabellen: `ArticleCategories`, `ArticleAttributeDefinitions`, `ArticleAttributeOptions`, `ArticleAttributeValues`
- Neues Feld: `Articles.ArticleCategoryId` (nullable FK)
- Script: `SQL/39_AddArticleCategoriesAndAttributes.sql`
- `SQL/00_FreshInstall.sql` aktualisieren

---

## Repositories

### IArticleCategoryRepository

```csharp
Task<List<ArticleCategory>> GetAllOrderedAsync();
Task<ArticleCategory?> GetByIdAsync(int id);
Task<ArticleCategory?> GetByNameAsync(string name);
Task<Dictionary<string, int>> GetCategoryNameToIdMapAsync(); // Fuer Sync-Lookup
Task<Dictionary<int, int>> GetArticleCountByCategoryAsync(); // Fuer Uebersicht
Task AddAsync(ArticleCategory category);
Task UpdateAsync(ArticleCategory category);
Task DeleteAsync(int id);
```

### IArticleAttributeRepository

```csharp
// Definitionen
Task<List<ArticleAttributeDefinition>> GetAllDefinitionsAsync();
Task<List<ArticleAttributeDefinition>> GetActiveDefinitionsOrderedAsync();
Task<ArticleAttributeDefinition?> GetDefinitionByIdAsync(int id);
Task AddDefinitionAsync(ArticleAttributeDefinition definition);
Task UpdateDefinitionAsync(ArticleAttributeDefinition definition);
Task DeleteDefinitionAsync(int id);
Task<bool> DefinitionHasValuesAsync(int definitionId);

// Optionen
Task<List<ArticleAttributeOption>> GetOptionsByDefinitionIdAsync(int definitionId);
Task AddOptionAsync(ArticleAttributeOption option);
Task DeleteOptionAsync(int id);
Task<bool> OptionIsInUseAsync(int optionId);

// Werte
Task<List<ArticleAttributeValue>> GetValuesByArticleIdAsync(int articleId);
Task<Dictionary<int, List<ArticleAttributeValue>>> GetValuesByArticleIdsAsync(List<int> articleIds);
Task SaveValuesAsync(int articleId, List<ArticleAttributeValue> values, string userName, string windowsUser);

// Batch fuer BOM
Task<Dictionary<string, string?>> GetCategoryNamesByArticleNumbersAsync(List<string> articleNumbers);
```

### ArticleRepository (erweitert)

`GetPaginatedAsync` erhaelt `.Include(a => a.ArticleCategory)` fuer Kategorie-Spalte.
Merkmal-Werte werden separat per `GetValuesByArticleIdsAsync` geladen (kein N+1).

---

## Zugriffskontrolle

| Bereich | Filter | Rollen |
|---------|--------|--------|
| Artikelkategorien CRUD | `[RequireMasterDataAccess]` | admin, masterdata |
| Artikelmerkmale CRUD | `[RequireMasterDataAccess]` | admin, masterdata |
| Artikel Edit (inkl. Kategorie + Merkmale) | Bestehendes Verhalten (kein Filter) | Alle angemeldeten Benutzer |
| Artikel Index (inkl. neue Spalten) | Bestehendes Verhalten | Alle angemeldeten Benutzer |

---

## Fallstricke & Designentscheidungen

1. **Performance Artikel-Index**: Merkmal-Werte per Batch-Query laden (1 Query fuer alle sichtbaren Artikel-IDs), nicht per Artikel. Dictionary `articleId → List<AttributeValue>` im ViewModel.

2. **table-filter.js**: Dynamische Spalten sind kein Problem — sie werden server-seitig via Razor gerendert und sind beim DOMContentLoaded bereits im HTML. `data-filterable` + `data-col` reichen.

3. **Edit-Form Model Binding**: Dynamische Merkmale als `List<AttributeEditItem>` im ViewModel. Form-Felder mit Index: `Attributes[0].BooleanValue`, `Attributes[1].SelectedOptionId`. ASP.NET Core bindet Listen korrekt, solange die Indices lueckenlos sind.

4. **Kategorie-Loeschen mit zugeordneten Artikeln**: `OnDelete.SetNull` in DB, UI verhindert Loeschen mit Hinweis. Admin muss erst Artikel umzuordnen oder Zuordnung bewusst aufheben.

5. **Merkmal-Typ nachtraeglich aendern**: Nicht erlaubt. Wenn ein Boolean-Merkmal zum Dropdown werden soll → altes deaktivieren, neues erstellen. Vereinfacht die Logik erheblich.

6. **OSEON-Sync Kollation**: `COLLATE Latin1_General_CI_AS` beim Vergleich von Artikelnummern, analog zum bestehenden Sage-Import.

7. **BOM Kategorie-Spalte**: Batch-Lookup per `GetCategoryNamesByArticleNumbersAsync(artikelnummern)` — ein Query, kein N+1. Nur Kategorie-Name, keine Merkmale in BOM (zu breit).

8. **SortOrder bei Neuanlage**: Automatisch `MAX(SortOrder) + 1` oder 0 wenn keine existieren. Verhindert, dass Admin sich um Sortierung kuemmern muss.

9. **SyncSource/SyncFieldName**: Nur Datenstruktur vorbereitet, kein Sync-Code fuer Merkmale implementiert. Kann spaeter ergaenzt werden ohne Schema-Aenderung.

---

## Aenderungen an bestehenden Dateien

| Datei | Aenderung |
|-------|-----------|
| `Models/Article.cs` | + `ArticleCategoryId`, Navigation `ArticleCategory` |
| `Data/ApplicationDbContext.cs` | + Entity-Configs fuer 4 neue Tabellen + FK auf Article |
| `Controllers/ArticlesController.cs` | Edit GET/POST erweitert (ViewModel, Merkmale speichern) |
| `Views/Articles/Index.cshtml` | + Kategorie-Spalte, + dynamische Merkmal-Spalten, `filterable-table` |
| `Views/Articles/Edit.cshtml` | + Kategorie-Dropdown, + Merkmal-Sektion |
| `Views/Articles/Info.cshtml` | + Kategorie + Merkmale read-only |
| `Views/Picking/Bom.cshtml` | + Kategorie-Spalte |
| `Controllers/PickingController.cs` | Bom-Action: Kategorie-Daten laden |
| `Views/Shared/_Layout.cshtml` | + Menuepunkte "Artikelkategorien", "Artikelmerkmale" |
| `Program.cs` | + Repository-Registrierungen |
| `IDEALAKEWMSService/Services/OseonSyncService.cs` | + `SyncArticleCategoriesToWmsAsync()` |
| `IDEALAKEWMSService/Workers/SyncWorker.cs` | + Aufruf Kategorie-Sync |
| `IDEALAKEWMSService/appsettings.json` | + `Sync:OseonArticleCategoryEnabled` |
| `SQL/39_AddArticleCategoriesAndAttributes.sql` | Neue Tabellen + Indexes |
| `SQL/00_FreshInstall.sql` | Aktualisieren |
| `CLAUDE.md` | Neue Entities, Repositories, Fallstricke dokumentieren |
| `Views/Help/Index.cshtml` | Hilfe fuer Kategorien + Merkmale |
| `Views/Help/Changelog.cshtml` | Changelog v1.4.0 |
| `AppVersion.cs` (Web + Service) | Version 1.4.0 |

---

## Neue Dateien

| Datei | Beschreibung |
|-------|-------------|
| `Models/ArticleCategory.cs` | Entity |
| `Models/ArticleAttributeDefinition.cs` | Entity + Enum `AttributeType` |
| `Models/ArticleAttributeOption.cs` | Entity |
| `Models/ArticleAttributeValue.cs` | Entity |
| `Models/ViewModels/ArticleEditViewModel.cs` | ViewModel fuer Edit mit Merkmalen |
| `Models/ViewModels/ArticleIndexViewModel.cs` | Erweitert um Merkmal-Daten |
| `Data/Repositories/IArticleCategoryRepository.cs` | Interface |
| `Data/Repositories/ArticleCategoryRepository.cs` | Implementierung |
| `Data/Repositories/IArticleAttributeRepository.cs` | Interface |
| `Data/Repositories/ArticleAttributeRepository.cs` | Implementierung |
| `Controllers/ArticleCategoriesController.cs` | CRUD Stammdaten |
| `Controllers/ArticleAttributesController.cs` | CRUD Stammdaten + Optionen |
| `Views/ArticleCategories/Index.cshtml` | Kategorien-Uebersicht |
| `Views/ArticleAttributes/Index.cshtml` | Merkmale-Uebersicht |
| `SQL/39_AddArticleCategoriesAndAttributes.sql` | DB-Migration |
| `Filters/RequireAdminAccessAttribute.cs` | Nur Admin-Rolle (falls gewuenscht) |
