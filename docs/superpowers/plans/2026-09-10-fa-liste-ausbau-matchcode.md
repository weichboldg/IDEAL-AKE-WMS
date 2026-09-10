# FA-Liste ausbauen: Zeilenwerte, HauptFA-Zeile, Freigabe-Kaskade, Matchcode — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Die FA-Liste zeigt Kopfdaten je Zeile, die HauptFA als normale Zeile, eine neue
Matchcode-Spalte in sechs Listen, und der Leitstand bekommt eine Gruppen-Kaskade „Alle Sub-FAs
freigeben" mit unveraenderter Zuweisungspflicht.

**Architecture:** Vier fachlich unabhaengige Bloecke, die sich aber dieselben Dateien teilen
(`ProductionOrders/Index.cshtml` liegt in Block 1, 2 und 3) — deshalb **ein** Plan, sequenziell, und
nicht vier. Block 1 (Matchcode) ist der einzige mit Schema-Aenderung und laeuft zuerst, damit die
Migration frueh im Zweig liegt. Block 2 (Zeilenwerte) baut die Datenquelle, auf der Block 3
(HauptFA-Zeile, schlanke Kopfzeile) aufsetzt. Block 4 (Kaskade) ist davon unabhaengig und beruehrt
nur den Leitstand.

**Tech Stack:** ASP.NET Core 10 MVC, EF Core 10 (SQL Server), Bootstrap 5, jQuery + eigenes JS,
xUnit/FluentAssertions/Moq/EF-InMemory.

**Spec:** `C:\Git\IDEAL-AKE-WMS\secondbrain\specs\freigegeben\2026-09-10-fa-liste-ausbau-matchcode-spec.md`
(Aufgaben-Notiz: `secondbrain/aufgaben/2026-09-10-fa-liste-ausbau-matchcode.md` — enthaelt die beiden
Vorab-Verifikationen)

## Global Constraints

- **Worktree:** `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch
  `feature/2026-08-07-ideal-teile-1-5`. **Keinen neuen Worktree anlegen, nicht von `main` abzweigen.**
- **Schreibziele:** Anwendungscode, `SQL/`, `docs/`, Tests, Versions-Bump → **in den Worktree**.
  Jede `secondbrain/`-Aenderung → **in den Hauptcheckout** `C:\Git\IDEAL-AKE-WMS\secondbrain\`.
  Der Worktree hat per sparse-checkout kein `secondbrain/`; ist dort eines sichtbar: nicht
  hineinschreiben, melden.
- **Kein Merge, kein `git push`, Worktree nicht loeschen, Status nicht auf `Gemerged` setzen.**
- **AKE-Regression ist die harte Bedingung:** Bei Master `ProduktionsauftragHierarchisch = false`
  verhaelt sich alles **bit-identisch** zum Vorzustand — keine neue Spalte sichtbar ausser dem
  (leeren) Matchcode, keine Kaskaden-Aktion in der Kopfzeile, unveraenderte Filter und Sortierung.
  Jeder neue Pfad ist strukturell nur bei `true` erreichbar, nicht bloss faktisch.
- **Migration:** `SQL/91_AddProductionOrderMatchcode.sql` — Nummer am 2026-09-10 nachgezaehlt
  (Worktree bei 90, `main` bei 88, andere Worktrees bei 83/81). Vor dem Anlegen **erneut** pruefen.
- **Matchcode null-sicher:** Bis die AKE-View liefert, ist das Feld dort leer. Kein Filter, keine
  Sortierung, keine Gruppierung darf darauf ins Leere laufen (F7 — die F1–F8-Nummerierung der
  Materialisierungs-Spec, **nicht** ein `fallstricke.md`-Paragraph).
- **Spaltenregistrierung doppelt pflegen:** Jede neue `<th data-col-key>` MUSS in
  `ColumnDefinitions.cs` **und** im inline `#column-config` der View stehen (Fallstricke **§3**,
  Zeilen 203-212 — nicht §10).
- **Audit-Felder:** Bei jedem Update `ModifiedAt`, `ModifiedBy`, `ModifiedByWindows` setzen.
- **Sprachregel:** Code/Variablen/Routen **englisch**, UI-Texte **deutsch**.
- **Tests:** `dotnet build IdealAkeWms.slnx`, `dotnet test IdealAkeWms.Tests`,
  `dotnet test IDEALAKEWMSService.Tests` — alle gruen als Mindestbedingung.
- **`EF.Functions.Like` vermeiden** (InMemory-Tests), Expression-Tree-Muster wie die uebrigen
  Textspalten nutzen.

## Design-Entscheidungen (Skill `frontend-design`, unter den Leitplanken der Constitution)

Die Constitution hat Vorrang vor dem Eigenstil-Impuls des Skills: **Konsistenz vor
Eigenstaendigkeit** (Bootstrap 5, bestehende Partials und Muster gewinnen) und **Kontrast ist
Funktion** (WCAG AA, Terminals in der Fertigung, teils schlechtes Licht). Keine neue UI-Bibliothek,
kein neues Icon-Set, keine neuen Farben ausserhalb der bestehenden Bootstrap-Palette.

**D1 — Wiederholungswerte unterdruecken: Glyphen unsichtbar, nicht gedimmt.**
Die Zelle behaelt ihren Text im DOM; unterdrueckt wird per eigener Klasse mit
`color: transparent`. **Nicht** per `display:none`, **nicht** per leerem Zellinhalt (sonst
verschwindet der Wert fuer Kopieren und Export), und **nicht** per gedaempftem Grau.
*Warum nicht gedimmt:* Ein absichtlich kontrastarmer Text ist genau das, was die Leitplanke
„Kontrast ist Funktion" verbietet — an einem Terminal bei schlechtem Licht waere er weder lesbar
noch erkennbar abgeschaltet. Unsichtbar ist ehrlicher: Die leere Zelle bedeutet „wie darueber", eine
Tabellenkonvention, die aelter ist als der Bildschirm. Der Wert bleibt fuer Auswahl/Kopieren,
Spaltenfilter und Screenreader vorhanden.

**D2 — HauptFA-Zeile kennzeichnen: Schriftschnitt + Textmarke, nie Farbe allein.**
`fw-semibold` auf der Zeile plus ein Bootstrap-Badge `HauptFA` in der FA-Nummer-Zelle. Farbe darf
nicht alleiniger Bedeutungstraeger sein; der Badge traegt die Bedeutung im Text.

**D3 — Kopfzeile: zwei Informationen bleiben, vier gehen.**
Es bleiben HauptFA-Nummer, Sub-FA-Zahl, **Kunde**, **Fert.-Termin**, Mehrdeutig-Badge. Es gehen
Prio, AB-Nummer, Montage-Abteilung und die uebrigen Termine. Die Dopplung von Kunde und Leittermin
ist Absicht — bei zugeklappter Gruppe ist die Kopfzeile das Einzige, was sichtbar ist.

**D4 — Kaskaden-Modal: exakt das bestehende `cascadeDoneModal` spiegeln.**
Gleiche Struktur, gleiche Knopf-Positionen, gleiche Dialog-Sprache. Bei aktivem
`KommissionierungMitZuweisung` ist die Kommissionierer-Auswahl ein **Pflichtfeld**, und der Dialog
nennt die Massenwirkung im Klartext, bevor sie eintritt.

**Texte (deutsch, aktiv, Aktionsname bleibt durch den Ablauf gleich):**
- Knopf: `Alle Sub-FAs freigeben`
- Dialog-Titel: `Alle Sub-FAs freigeben`
- Zuweisungs-Hinweis: `Der gewählte Kommissionierer wird allen {N} Sub-FAs zugewiesen und kann
  danach je Sub-FA geändert werden.`
- Fehlender Picker: **`Bitte einen Kommissionierer zuweisen.`** — verifizierter Wortlaut aus
  `ToggleRelease` (Z. 348) und `BulkRelease` (Z. 392). **Wortgleich uebernehmen**, nicht neu
  formulieren: Dieselbe Pflicht muss in der Massenaktion dieselbe Meldung zeigen wie in der
  Einzelaktion, sonst liest es sich wie zwei verschiedene Regeln. Mechanik ebenfalls gleich:
  `TempData["WarningMessage"]` + Redirect, **kein** HTTP-Fehler.
- Erfolg: `HauptFA {orderNumber}: {N} Sub-FA(s) freigegeben.` — Satzbau nach dem Vorbild von
  `CascadeDone` (Z. 506); nennt die **tatsaechlich** neu freigegebenen, nicht die Gruppengroesse.
- Nichts zu tun: `HauptFA {orderNumber}: alle Sub-FAs waren bereits freigegeben — nichts geändert.`
  (Vorbild `CascadeDone`: „… keine Sub-FAs gefunden — nichts geändert.")

**D7 — Zeitbasis: `DateTime.UtcNow`, wie die uebrige Freigabe-Familie.**
Verifiziert und bewusst entschieden: `SetReleaseAsync`/`SetReleaseBatchAsync` stempeln
`DateTime.UtcNow`, `SetAssignedPickerAsync` und `SetIsDoneBdeForOrderNumberAsync` dagegen
`DateTime.Now`. Die neue Methode gehoert zur **Freigabe**-Familie und nimmt daher `UtcNow`.
> **Befund, NICHT in dieser Spec zu beheben:** In derselben Tabelle
> (`ProductionOrderPickingStatus`) landen ueber verschiedene Methoden **zwei verschiedene
> Zeitbasen** in `ModifiedAt` — in Oesterreich ein bis zwei Stunden Unterschied. Wer
> Audit-Zeitstempel vergleicht, vergleicht Aepfel mit Birnen. Gehoert als eigener Punkt ins Brain,
> nicht in diesen Lauf.

**CSS-Ort:** `IdealAkeWms/wwwroot/css/site.css`, im Block direkt hinter `.fa-liste-chevron`
(Z. ~1250). Die FA-Listen nutzen keine eigene View-CSS; `bde.css` ist dem BDE-Terminal vorbehalten.

**D5 — Teilnehmende Zellen markieren sich selbst, statt per Spaltenindex gefunden zu werden.**
Verifiziert: Die Zellen in `_ProductionOrderRow.cshtml` tragen **kein** `data-col-key` — die
Zuordnung zum `<thead>` laeuft ausschliesslich ueber die **Positionsreihenfolge**. Ein JS, das die
Wiederholungsspalten per Index sucht, bricht daher beim naechsten Spalten-Einschub still.
Deshalb: Jede teilnehmende Zelle bekommt im Partial `data-repeat-key="<spaltenschluessel>"`. Das ist
additiv, stoert die Positionszuordnung nicht und macht das JS gegen Spaltenverschiebungen immun.

**D6 — Der Sortier-Hook ist eine Aenderung an gemeinsam genutztem JS — und wird gebuendelt.**
Verifiziert: `table-filter.js` dispatcht `table-filter-applied` **nur** aus `applyFilters()`; nach dem
**Sortieren** dispatcht es **nichts**. D1 braucht also einen neuen Hook. Vorgabe:
- In `sortTable()` am Ende **ein** `CustomEvent('table-sorted', { detail: { viewKey, colKey, dir } })`
  auf `document` — eine Stelle, die sowohl den `<th>`-Klick als auch `window.triggerSort`
  (Default-Sort beim Init) abdeckt. Rein additiv: Auf `table-sorted` hoert heute niemand, die
  AKE-Listen aendern ihr Verhalten nicht.
- **Im selben Griff** den geparkten Befund **B-2** beheben (siehe
  [[2026-08-20-fehlerprotokoll-anzeige-epic-ab]] und Punkt 23 der
  [[2026-09-08-bom-bridge-nachlese]]): `sortTable` kehrt bei `getPhysicalIndex(colKey) < 0` und
  `window.triggerSort` bei unbekanntem `th` **stillschweigend** zurueck — ohne Ausnahme, ohne
  Meldung, ohne Wirkung. Beide Stellen bekommen ein `console.warn` mit `viewKey` und `colKey`.
  *Warum hier:* `table-filter.js` haengt an rund 30 Listen; jeder Eingriff braucht denselben
  Regressionsnachweis (Ein-`tbody`-Listen gegenpruefen). Zwei kleine Eingriffe kosten zwei volle
  Nachweise — deshalb **ein** Griff. Die Regressionsliste wird **je Tabelle** erhoben, nicht je
  View-Datei (`BdeMasterData/Index.cshtml` zeigt drei `<tbody>`, ist aber unkritisch: drei getrennte
  Tabellen mit je einem).

## Abweichungen von der Spec, am Code belegt (vor Umsetzungsbeginn)

Drei Punkte, bei denen die Spec-Annahme dem Code nicht standhaelt. **Der Code gewinnt**, die Spec
wird nachgezogen.

**A1 — `OseonTracking` faellt aus dem Matchcode-Umfang. Fuenf Listen, nicht sechs.**
Die Spec nennt sechs „FA-Zeilen-Ebene-Listen". `OseonTracking` ist zwar FA-Ebene, speist sich aber
**nicht aus `ProductionOrder`**: `OseonGroupViewModelBuilder.BuildAsync` nimmt
`IEnumerable<OseonProductionOrder>` (Zeile 29), und `OseonProductionOrder` hat `ArticleNumber`, aber
**keinen** Matchcode. `ProductionOrder.Matchcode` ist dort nur ueber einen zusaetzlichen Join
erreichbar — also genau der Fall, den **Antwort 7** in die Folge-Arbeit verweist. Dazu kommt: Die
View ist ein lazy ladender Baum mit eigenem Sortierpfad, und **IDEAL hat gar kein OSEON** (Befund T7
des UAT-Lauf 1) — die Spalte waere dort fuer IDEAL dauerhaft leer.
⇒ **In-Scope: `ProductionOrders`, `PickingLeitstand`, `FaCompletion`, `FaWorklist`, `Picking`.**
`OseonTracking` wird als Folge-Arbeit dokumentiert, nicht gebaut.

**A2 — `MaterializationSourceOrder` wird NICHT erweitert.**
Die Spec verlangt, den Record um `Matchcode` zu erweitern. Unnoetig: Anlege- und Update-Zweig lesen
den Knoten **direkt** (`nodeByKey[key]` bzw. `sn`), nicht den Record — genau dort, wo `Artnr` und
`Bezeichnung1/2` gesetzt werden. Ein Feld in den Record aufzunehmen, das niemand liest, waere ein
zweiter, abweichender Wahrheitsort (derselbe Befund wie Punkt 16 der Materialisierungs-Nachlese:
`MaterializationSourceOrder.HasCoatingParts` wird befuellt, aber nirgends gelesen).

**A3 — Spaltenlaenge ist entschieden: `NVARCHAR(200)`, nicht „100 oder groesszuegiger".**
Die Quelle `FaHierarchyNodes.Matchcode` ist `NVARCHAR(200)` (`SQL/89_AddFaHierarchy.sql:29`,
`SQL/00_FreshInstall.sql:1761`). Die Zielspalte spiegelt das **exakt** → `[StringLength(200)]`.
*Warum das wichtig ist:* Bei 100 wuerde die Materialisierung laengere Matchcodes abschneiden oder
werfen — stiller Datenverlust bzw. ein Sync-Abbruch, beides erst im Betrieb sichtbar.

---

# Block 1 — Matchcode (Schema, Materialisierung, fuenf Listen)

### Task 1: Spalte `ProductionOrder.Matchcode` + Migration

**Files:**
- Modify: `IdealAkeWms/Models/ProductionOrder.cs:43` (vor `ArticleNumber`)
- Create: `IdealAkeWms/Migrations/<timestamp>_AddProductionOrderMatchcode.cs` (von `dotnet ef` erzeugt)
- Create: `SQL/91_AddProductionOrderMatchcode.sql`
- Modify: `SQL/00_FreshInstall.sql:268` (Schema) **und** `:2298` (MigrationId)

**Interfaces:**
- Produces: `ProductionOrder.Matchcode` (`string?`, max 200) — genutzt von Task 2, 3, 4.

- [ ] **Step 1: Nummer gegenpruefen, nicht annehmen**

```bash
ls SQL/*.sql | sort -t_ -k1 -n | tail -3
```
Erwartet: hoechste Nummer ist `90_InvertProductionOrderHierarchy.sql` → `91` ist frei. Ist dort
inzwischen eine 91, die naechste freie Nummer nehmen und alle Dateinamen unten entsprechend anpassen.

- [ ] **Step 2: Property ergaenzen**

In `IdealAkeWms/Models/ProductionOrder.cs` direkt **vor** `ArticleNumber` (Zeile 43):

```csharp
    [StringLength(200)]
    [Display(Name = "Matchcode")]
    public string? Matchcode { get; set; }
```

Laenge 200 spiegelt die Quelle `FaHierarchyNodes.Matchcode` — nicht aendern.

- [ ] **Step 3: Migration erzeugen**

```bash
dotnet ef migrations add AddProductionOrderMatchcode --project IdealAkeWms
```

- [ ] **Step 4: Erzeugtes Skript offline gegenpruefen**

```bash
dotnet ef migrations script --project IdealAkeWms --idempotent -o /tmp/mig91.sql
```
Erwartet: **nur** ein `ALTER TABLE [ProductionOrders] ADD [Matchcode] nvarchar(200) NULL;`.
**Kein** `DROP INDEX`, **kein** `ALTER COLUMN` auf Bestandsspalten. Findet sich dort mehr:
anhalten und melden — Fallstricke §9 (auch `AlterColumn` zieht ungeschuetzte Auto-Index-DDL).

- [ ] **Step 5: `SQL/91_AddProductionOrderMatchcode.sql` anlegen**

Muster exakt nach `SQL/85_AddWarehouseRequisitionComment.sql`. `<timestamp>` durch den echten
Migrations-Zeitstempel aus Step 3 ersetzen:

```sql
-- SQL/91_AddProductionOrderMatchcode.sql
-- Migration 91: AddProductionOrderMatchcode
-- Rein additiv, idempotent: neue Spalte ProductionOrders.Matchcode (NVARCHAR(200), NULL).
-- Laenge spiegelt die Quelle FaHierarchyNodes.Matchcode — keine Kuerzung, kein Datenverlust.
-- Bestandszeilen bleiben NULL und fuellen sich beim naechsten Materialisierungs-Lauf.
-- DDL in eigenem Batch, __EFMigrationsHistory-Insert in separatem Batch.
SET NOCOUNT ON;
GO

IF COL_LENGTH('dbo.ProductionOrders', 'Matchcode') IS NULL
BEGIN
    ALTER TABLE [dbo].[ProductionOrders] ADD [Matchcode] NVARCHAR(200) NULL;
    PRINT 'Spalte ProductionOrders.Matchcode hinzugefuegt.';
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = '<timestamp>_AddProductionOrderMatchcode')
BEGIN
    INSERT INTO dbo.__EFMigrationsHistory (MigrationId, ProductVersion)
    VALUES ('<timestamp>_AddProductionOrderMatchcode', '10.0.2');
END
GO
```

- [ ] **Step 6: `SQL/00_FreshInstall.sql` an BEIDEN Stellen nachziehen**

(a) Schema — hinter Zeile 268 (`[Description2] NVARCHAR(500) NULL,`) einfuegen:
```sql
        [Matchcode]               NVARCHAR(200)     NULL,
```
(b) MigrationId — hinter Zeile 2298 anhaengen:
```sql
IF NOT EXISTS (SELECT * FROM [dbo].[__EFMigrationsHistory] WHERE [MigrationId] = '<timestamp>_AddProductionOrderMatchcode')
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES ('<timestamp>_AddProductionOrderMatchcode', '10.0.2');
```

- [ ] **Step 7: Build + Commit**

```bash
dotnet build IdealAkeWms.slnx
git add IdealAkeWms/Models/ProductionOrder.cs IdealAkeWms/Migrations SQL/91_AddProductionOrderMatchcode.sql SQL/00_FreshInstall.sql
git commit -m "feat: Spalte ProductionOrder.Matchcode (NVARCHAR(200), additiv)"
```

---

### Task 2: Matchcode materialisieren (Anlege- UND Update-Pfad, F1)

**Files:**
- Modify: `IDEALAKEWMSService/Services/FaMaterializationSyncService.cs:177` (Anlegen) und `:200` (Update)
- Test: `IDEALAKEWMSService.Tests/Services/FaMaterializationSyncServiceTests.cs`

**Interfaces:**
- Consumes: `ProductionOrder.Matchcode` aus Task 1.
- Produces: befuellter `Matchcode` auf materialisierten Zeilen — genutzt von Task 3/4.

- [ ] **Step 1: Fehlschlagenden Test schreiben**

In `FaMaterializationSyncServiceTests.cs`. Zuerst den Fixture-Helfer (`:36-41`) um `Matchcode`
erweitern — **beide** Zweige brauchen ihn:

```csharp
    private static FaHierarchyNode Node(int haupt, int sub, int? vater) => new()
    {
        HauptFA = haupt, SubFA = sub, VaterFA = vater,
        Sollmenge = 1m, Artnr = "ART", Bezeichnung1 = "Bez1", Bezeichnung2 = "Bez2",
        Matchcode = "MC-" + sub,
        SyncedAt = DateTime.Now
    };
```

Dann zwei Tests — einer je Pfad, weil die F1-Regel genau hier schon einmal gebrochen war:

```csharp
    [Fact]
    public async Task RunAsync_NewSubFa_SetsMatchcodeFromNode()
    {
        using var ctx = NewContext();
        ctx.FaHierarchyNodes.Add(Node(100, 101, 100));
        await ctx.SaveChangesAsync();

        await NewService(ctx).RunAsync(dryRun: false, CancellationToken.None);

        var order = await ctx.ProductionOrders.SingleAsync(o => o.SubOrderNumber == "101");
        order.Matchcode.Should().Be("MC-101");
    }

    [Fact]
    public async Task RunAsync_ExistingSubFa_UpdatesMatchcode_WhenSourceChanged()
    {
        using var ctx = NewContext();
        ctx.FaHierarchyNodes.Add(Node(100, 101, 100));
        ctx.ProductionOrders.Add(new ProductionOrder
        {
            OrderNumber = "100", SubOrderNumber = "101", Matchcode = "ALT", Quantity = 1m
        });
        await ctx.SaveChangesAsync();

        await NewService(ctx).RunAsync(dryRun: false, CancellationToken.None);

        var order = await ctx.ProductionOrders.SingleAsync(o => o.SubOrderNumber == "101");
        order.Matchcode.Should().Be("MC-101");
    }
```

*Hinweis fuer den Umsetzer:* `NewContext()`/`NewService(ctx)` sind die in dieser Testdatei bereits
vorhandenen Helfer — ihre genauen Namen aus den bestehenden Tests (`:44`, `:102`) uebernehmen, nicht
erfinden.

- [ ] **Step 2: Tests laufen lassen, Fehlschlag bestaetigen**

```bash
dotnet test IDEALAKEWMSService.Tests --filter Matchcode
```
Erwartet: FAIL — `Matchcode` ist `null` statt `"MC-101"`.

- [ ] **Step 3: Anlege-Pfad ergaenzen**

`FaMaterializationSyncService.cs`, im `newOrder`-Initialisierer direkt **hinter** `ArticleNumber = n.Artnr,`:

```csharp
                    Matchcode = n.Matchcode,
```

- [ ] **Step 4: Update-Pfad ergaenzen**

Im Update-Block direkt **hinter** `o.ArticleNumber = sn.Artnr;`:

```csharp
                    o.Matchcode = sn.Matchcode;
```

**Beides in derselben Aenderung** (F1-Regel). `MaterializationSourceOrder` bleibt unberuehrt (A2).

- [ ] **Step 5: Tests laufen lassen**

```bash
dotnet test IDEALAKEWMSService.Tests
```
Erwartet: alle gruen, inkl. der beiden neuen.

- [ ] **Step 6: Commit**

```bash
git add IDEALAKEWMSService/Services/FaMaterializationSyncService.cs IDEALAKEWMSService.Tests/Services/FaMaterializationSyncServiceTests.cs
git commit -m "feat: Matchcode materialisieren (Anlege- und Update-Pfad, F1)"
```

---

### Task 3: Matchcode durch die Leseschicht bis in fuenf Listen

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs:5` (`LeitstandOrderRow`)
- Modify: `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs` (`ProjectLeitstandRows`)
- Modify: `IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs` (`ProductionOrderListItem`)
- Modify: `IdealAkeWms/Controllers/ProductionOrdersController.cs` (`MapItem`)
- Modify: `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` (fuenf ViewConfigs)
- Modify: je Liste `<thead>`, inline `#column-config` und Row-Partial (Tabelle unten)

**Interfaces:**
- Consumes: `ProductionOrder.Matchcode` (Task 1), materialisiert (Task 2).
- Produces: `ProductionOrderListItem.Matchcode` (`string?`), `LeitstandOrderRow.Matchcode` (`string?`),
  Spaltenschluessel `"matchcode"` — genutzt von Task 4.

- [ ] **Step 1: `LeitstandOrderRow` erweitern**

`Matchcode` als **weiteren defaulteten Parameter am Ende** anhaengen (nicht in die Mitte — der Record
ist positionell, jede Einfuegung in der Mitte verschiebt alle Aufrufer still):

```csharp
    string? Matchcode = null
```

Dann in `ProjectLeitstandRows` die Projektion um `o.Matchcode` ergaenzen, an der Position, die dem
neuen Parameter entspricht (benannt uebergeben, falls die bestehende Projektion benannte Argumente
nutzt).

- [ ] **Step 2: ViewModel + `MapItem`**

In `ProductionOrderListItem`: `public string? Matchcode { get; set; }`.
In `MapItem` (Controller Zeile ~107-174) neben `ArticleNumber`: `Matchcode = o.Matchcode,` bzw.
`item.Matchcode = o.Matchcode;` — je nachdem, wie die Nachbarfelder dort gesetzt werden.

- [ ] **Step 3: Spalte in `ColumnDefinitions.cs` registrieren — fuenf ViewConfigs**

Je ViewConfig **direkt vor** `description1` (also hinter `article-number`):

```csharp
            new ColumnDef("matchcode", "Matchcode"),
```

`Locked: false`, **kein** `DefaultHidden` (Antwort 9: sichtbar per Default). Betroffen:
`ProductionOrders` (Z. 19-50), `PickingLeitstand` (Z. 60), `FaCompletion` (Z. 107),
`FaWorklist` (Z. 136), `Picking` (Z. 170). **`OseonTracking` NICHT** (A1).

> **Achtung, Reihenfolge ist bindend:** Die Zellen in den Row-Partials tragen **kein**
> `data-col-key`; die Zuordnung laeuft ausschliesslich ueber die **Position**. `<th>`,
> `#column-config`, `ColumnDefinitions` und `<td>` muessen daher **dieselbe** Reihenfolge haben.
> Wer nur drei der vier Stellen pflegt, verschiebt die ganze Tabelle um eine Spalte — und es faellt
> erst am Bildschirm auf.

- [ ] **Step 4: Je Liste vier Stellen pflegen**

| viewKey | `<th>` einfuegen hinter | inline `#column-config` | Row-Partial: `<td>` hinter |
|---|---|---|---|
| `ProductionOrders` | `Index.cshtml` `article-number` (Z. 81) | Z. 242-266 | `_ProductionOrderRow.cshtml:61-64` |
| `PickingLeitstand` | `Index.cshtml:117` | `:395` | `_PickingLeitstandRow.cshtml:57-61` |
| `FaCompletion` | `Index.cshtml:69` | `:153` | `_FaCompletionRow.cshtml:41` |
| `FaWorklist` | `Index.cshtml:104` | `:187` | `_FaWorklistRow.cshtml:54` |
| `Picking` | `Index.cshtml:53` | `:122` | `_PickingRow.cshtml:29` |

`<th>` je Liste (Datumsspalten-Attribute entfallen, es ist Text):
```html
                    <th data-filterable data-col-key="matchcode">Matchcode</th>
```
`#column-config`-Eintrag:
```json
    { "key": "matchcode", "label": "Matchcode", "locked": false, "defaultWidth": null },
```
Zelle im Row-Partial:
```html
                        <td>@item.Matchcode</td>
```
(Im `_OseonGroupDetails` **nichts** aendern — A1.)

- [ ] **Step 5: Build + alle Tests**

```bash
dotnet build IdealAkeWms.slnx && dotnet test IdealAkeWms.Tests
```

- [ ] **Step 6: Commit**

```bash
git add -A IdealAkeWms
git commit -m "feat: Matchcode-Spalte in fuenf FA-Listen (ColumnDefinitions + column-config + th + td)"
```

---

### Task 4: Server-Spaltenfilter auf Matchcode, null-sicher (F7)

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs` (Spaltenfilter-Schalter)
- Test: `IdealAkeWms.Tests/Repositories/ProductionOrderRepositoryTests.cs`

**Interfaces:**
- Consumes: Spaltenschluessel `"matchcode"` aus Task 3.

- [ ] **Step 1: Fehlschlagenden Test schreiben**

```csharp
    [Fact]
    public async Task ColumnFilter_Matchcode_FindsRow_AndIgnoresNullRows()
    {
        using var ctx = NewContext();
        ctx.ProductionOrders.AddRange(
            new ProductionOrder { OrderNumber = "1", ArticleNumber = "A", Matchcode = "KESSEL-7" },
            new ProductionOrder { OrderNumber = "2", ArticleNumber = "B", Matchcode = null });
        await ctx.SaveChangesAsync();

        var repo = new ProductionOrderRepository(ctx);
        var page = await repo.GetForLeitstandAsync(null, null, null, showDone: true, page: 1,
            pageSize: 50, columnFilters: new Dictionary<string, string> { ["matchcode"] = "kessel" });

        page.Rows.Should().HaveCount(1);
        page.Rows[0].OrderNumber.Should().Be("1");
    }
```

Zweiter Test — **der eigentliche F7-Nachweis**: Eine Liste, in der **alle** Matchcodes `null` sind
(der AKE-Fall bis zur View-Erweiterung), liefert bei gesetztem Filter **null Treffer ohne Fehler**:

```csharp
    [Fact]
    public async Task ColumnFilter_Matchcode_AllNull_ReturnsNoHits_WithoutThrowing()
    {
        using var ctx = NewContext();
        ctx.ProductionOrders.AddRange(
            new ProductionOrder { OrderNumber = "1", Matchcode = null },
            new ProductionOrder { OrderNumber = "2", Matchcode = null });
        await ctx.SaveChangesAsync();

        var repo = new ProductionOrderRepository(ctx);
        var page = await repo.GetForLeitstandAsync(null, null, null, showDone: true, page: 1,
            pageSize: 50, columnFilters: new Dictionary<string, string> { ["matchcode"] = "x" });

        page.Rows.Should().BeEmpty();
        page.TotalCount.Should().Be(0);
    }
```

*Signaturen aus den bestehenden Tests derselben Datei uebernehmen*, nicht erfinden.

- [ ] **Step 2: Fehlschlag bestaetigen**

```bash
dotnet test IdealAkeWms.Tests --filter Matchcode
```
Erwartet: FAIL — der Filterschluessel wird noch nicht ausgewertet, beide Zeilen kommen zurueck.

- [ ] **Step 3: Filter ergaenzen**

Im Spaltenfilter-`switch`/`if`-Block von `BuildLeitstandQuery` neben `"article-number"` und
`"description1"` einen Zweig fuer `"matchcode"` ergaenzen — **genau dasselbe Muster wie die
Nachbar-Textspalten**, ohne `EF.Functions.Like` (InMemory-Tests). Null-Sicherheit entsteht dabei von
selbst: Das Praedikat prueft wie bei den Nachbarn zuerst `o.Matchcode != null`, eine `null`-Zeile
matcht nie. **Keine Sonderbehandlung, kein Ausblenden der Spalte, keine stille leere Liste.**

- [ ] **Step 4: Tests gruen**

```bash
dotnet test IdealAkeWms.Tests
```

- [ ] **Step 5: Commit**

```bash
git add IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs IdealAkeWms.Tests
git commit -m "feat: Server-Spaltenfilter auf Matchcode (null-sicher, F7)"
```

---

# Block 2 — Kopfdaten je Zeile (K1 im Anzeige-Modell)

### Task 5: Gruppenwerte bis auf die Zeile durchreichen

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs` (`ProductionOrderListItem`)
- Modify: `IdealAkeWms/Controllers/ProductionOrdersController.cs:107` (`MapItem`), `:247` (hier. Aufruf)
- Test: `IdealAkeWms.Tests/Controllers/ProductionOrdersControllerTests.cs`

**Interfaces:**
- Produces: `ProductionOrderListItem.KonstruktionsTermin` (`DateTime?`), `.Prio` (`int?`),
  `.AbNummer` (`string?`), `.MontageAbteilung` (`string?`) — und `.Customer` wird im hierarchischen
  Modus erstmals aus dem Gruppenkopf befuellt. Genutzt von Task 6, 7, 8, 10.

- [ ] **Step 1: Fehlschlagenden Test schreiben**

Muster: der bestehende `Index_Hierarchical_UsesGroupProductionDateForCascade_NotRowValue`
(`PickingLeitstandControllerTests.cs:186`) zeigt, wie ein Gruppenwert gegen einen Zeilenwert geprueft
wird. Analog:

```csharp
    [Fact]
    public async Task Index_Hierarchical_FillsRowCustomerAndHeadFields_FromGroupHead()
    {
        // Arrange: eine HauptFA-Gruppe mit EINEM Kopf (nicht mehrdeutig), Sub-FA-Zeile mit
        // leerem ProductionOrder.Customer — die K1-Regel laesst die DB-Spalte leer.
        // FaHierarchyOrderInfo traegt Kunde/Prio/ABNr/MontageAbteilung/KO_Termin.

        var result = await controller.Index(/* bestehende Parameterliste der Nachbartests */);

        var vm = result.Model as ProductionOrderListViewModel;
        var row = vm!.Groups.Single().Items.Single();
        row.Customer.Should().Be("Mustermann GmbH");
        row.Prio.Should().Be(3);
        row.AbNummer.Should().Be("AB-42");
        row.MontageAbteilung.Should().Be("Montage 1");
        row.KonstruktionsTermin.Should().Be(new DateTime(2026, 9, 1));
    }
```

Und der **AKE-Regressionstest** dazu, der wichtiger ist als der erste:

```csharp
    [Fact]
    public async Task Index_Flat_LeavesNewRowFieldsNull_BitIdentical()
    {
        // Master false → flacher Zweig, MapItem ohne Gruppenargumente
        var row = /* … */;
        row.Prio.Should().BeNull();
        row.AbNummer.Should().BeNull();
        row.MontageAbteilung.Should().BeNull();
        row.KonstruktionsTermin.Should().BeNull();
        row.Customer.Should().Be("AUS DER DB");  // unveraendert aus ProductionOrder.Customer
    }
```

- [ ] **Step 2: Fehlschlag bestaetigen**

```bash
dotnet test IdealAkeWms.Tests --filter FillsRowCustomerAndHeadFields
```
Erwartet: FAIL — Properties existieren nicht / sind null.

- [ ] **Step 3: ViewModel erweitern**

In `ProductionOrderListItem`:

```csharp
    public DateTime? KonstruktionsTermin { get; set; }
    public int? Prio { get; set; }
    public string? AbNummer { get; set; }
    public string? MontageAbteilung { get; set; }
```

- [ ] **Step 4: `MapItem` um Gruppenparameter erweitern**

Signatur (neue Parameter **am Ende**, alle mit Default `null`, damit der flache Aufruf in Zeile 312
unveraendert bleibt — das ist der AKE-Regressionsschutz aus F4):

```csharp
ProductionOrderListItem MapItem(LeitstandOrderRow o, bool hasCoatingParts, bool isCoatingDone,
    DateTime? groupProductionDate = null, DateTime? groupDeliveryDate = null,
    DateTime? groupCoatingStart = null, bool hierarchical = false,
    string? groupCustomer = null, int? groupPrio = null, string? groupAbNummer = null,
    string? groupMontageAbteilung = null, DateTime? groupKonstruktionsTermin = null)
```

Im Rumpf, **nur** im hierarchischen Fall (sonst bleibt AKE bit-identisch):

```csharp
            if (hierarchical)
            {
                item.Customer = groupCustomer ?? item.Customer;
                item.Prio = groupPrio;
                item.AbNummer = groupAbNummer;
                item.MontageAbteilung = groupMontageAbteilung;
                item.KonstruktionsTermin = groupKonstruktionsTermin;
            }
```

> **Kombigeraete fallen damit automatisch richtig aus:** Bei `heads.Count > 1` bleiben die Locals im
> Controller `null` (verifiziert, Zeile 193-242) — die Zeilenfelder werden dann `null`, also **leer**.
> Genau Antwort 3. **Keine** zusaetzliche `IsAmbiguous`-Abfrage in `MapItem` einbauen; die waere eine
> zweite Wahrheit neben den Locals.

- [ ] **Step 5: Hierarchischen Aufruf erweitern**

Im hierarchischen Zweig (Zeile ~247) die fuenf neuen Argumente aus denselben Locals durchreichen,
die heute schon `group.Customer` usw. setzen — **kein** zusaetzlicher Datenbankzugriff,
`GetByHauptFaKeysAsync` liefert die Koepfe bereits.

- [ ] **Step 6: Tests gruen + Commit**

```bash
dotnet test IdealAkeWms.Tests
git add IdealAkeWms
git commit -m "feat: K1-Kopfdaten je Zeile im Anzeige-Modell (hierarchisch), AKE unveraendert"
```

---

### Task 6: Kunde/Prio/AB-Nummer/Montage-Abteilung/KO-Termin als C#-Postfilter

**Files:**
- Modify: `IdealAkeWms/Controllers/ProductionOrdersController.cs:81-92` (Filter-Aufteilung),
  `:257-270` (Anwendung), `:359-362` (`FaListDateColumnKeys`), `:372` (`MatchFaListDateFilter`)
- Test: `IdealAkeWms.Tests/Controllers/ProductionOrdersControllerTests.cs`

**Interfaces:**
- Consumes: die Zeilenfelder aus Task 5.
- Produces: die Schluesselmenge `FaListComputedColumnKeys` — genutzt von Task 7.

- [ ] **Step 1: Fehlschlagenden Test schreiben**

```csharp
    [Fact]
    public async Task Index_Hierarchical_ColumnFilter_Prio_FiltersOnComputedRowValue()
    {
        // Zwei Gruppen, Prio 3 und Prio 9, ProductionOrder.Prio existiert NICHT in der DB.
        // Spaltenfilter "prio" = "3" → nur die erste Gruppe bleibt, Z3b: leere Gruppen fallen weg.
        vm.Groups.Should().HaveCount(1);
        vm.Groups.Single().Items.Should().OnlyContain(i => i.Prio == 3);
    }
```

- [ ] **Step 2: Fehlschlag bestaetigen** — der Filter landet heute als SQL-Praedikat und findet nichts
  (bzw. wirft, weil die Spalte nicht existiert).

- [ ] **Step 3: Zweite Schluesselmenge einfuehren**

Neben `FaListDateColumnKeys` (Zeile 359):

```csharp
    private static readonly HashSet<string> FaListComputedColumnKeys =
        new(StringComparer.OrdinalIgnoreCase)
        { "customer", "prio", "ab-nummer", "montage-abteilung", "konstruktions-termin" };
```

`konstruktions-termin` ist ein **Datums**wert und gehoert zusaetzlich in die Datums-Behandlung —
entweder in `FaListDateColumnKeys` aufnehmen **oder** in `MatchFaListDateFilter` mitbehandeln. Eine
der beiden Stellen, nicht beide.

- [ ] **Step 4: Aufteilung erweitern**

Die bestehende Mechanik bleibt, nur die Bedingung wird breiter: Wo heute
`FaListDateColumnKeys.Contains(kv.Key)` steht (Zeile 83, 86), kommt
`|| FaListComputedColumnKeys.Contains(kv.Key)` dazu. Und `hasDateFilters` wird zu
`hasPostFilters`. **Wichtig:** Das gilt **nur** im hierarchischen Zweig; der flache Zweig bleibt
unberuehrt (F4).

- [ ] **Step 5: Anwendung erweitern**

Im Block Zeile 257-270 neben `MatchFaListDateFilter` eine Schwesterfunktion:

```csharp
    private static bool MatchFaListComputedFilter(ProductionOrderListItem item, string key,
        List<string> tokens, bool negate)
    {
        var text = key switch
        {
            "customer"           => item.Customer,
            "prio"               => item.Prio?.ToString(),
            "ab-nummer"          => item.AbNummer,
            "montage-abteilung"  => item.MontageAbteilung,
            _                    => null
        };
        text = (text ?? string.Empty).ToLowerInvariant();
        var hit = tokens.Any(t => text.Contains(t));
        return negate ? !hit : hit;
    }
```

> **Leerer Wert matcht nie** (`string.Empty.Contains("x") == false`) — damit verhaelt sich ein Filter
> auf einer bei Kombigeraeten leeren Spalte korrekt: keine Treffer, kein Fehler, keine stille leere
> Liste ohne Grund. Dieselbe Regel wie F7 beim Matchcode.

- [ ] **Step 6: Tests gruen + Commit**

```bash
dotnet test IdealAkeWms.Tests
git add IdealAkeWms
git commit -m "feat: K1-Spaltenfilter als C#-Postfilter nach der Berechnung (ADR 0005)"
```

---

### Task 7: Kunde-Subquery-Sonderweg im hierarchischen Modus entfernen

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs:112-136` (Freitext),
  `:286-290` (Spaltenfilter), `:344` + `:363` (Praedikat-Builder)
- Modify: `IdealAkeWms/Controllers/PickingLeitstandController.cs` (gleiche Umstellung)
- Test: bestehender `ToQueryString()`-Test in `ProductionOrderRepositoryTests`

**Interfaces:**
- Consumes: `FaListComputedColumnKeys` aus Task 6.

> **Vorsicht, das ist der riskanteste Task des Blocks.** `BuildLeitstandQuery` wird von **vier**
> Stellen genutzt (`GetForLeitstandAsync`, `GetForLeitstandGroupedAsync`, und darueber
> `ProductionOrdersController` **und** `PickingLeitstandController`). Der Leitstand bekommt die
> K1-Zeilenwerte laut Spec **nicht** (Out-of-Scope, Entscheidung B5) — wer dort den SQL-Filter
> entfernt, ohne einen Postfilter zu ergaenzen, **verliert die Kunde-Suche auf dem Leitstand
> vollstaendig**. Das waere eine stille Regression in einer Produktivansicht.

- [ ] **Step 1: Entscheidung festhalten und pruefen**

Der Sonderweg wird **nur fuer die FA-Liste** abgeloest. Auf dem Leitstand bleibt der
Subquery-Sonderweg **bestehen**, bis dort die Zeilenwerte nachgezogen werden (Folge-Arbeit).
Technisch: `BuildLeitstandQuery` bekommt einen zusaetzlichen Parameter
`bool skipComputedColumnFilters = false`; nur `ProductionOrdersController` setzt ihn im
hierarchischen Zweig auf `true`.

- [ ] **Step 2: Regressionstest fuer den Leitstand zuerst**

```csharp
    [Fact]
    public async Task Leitstand_Hierarchical_CustomerFilter_StillUsesSubquery()
    {
        // Der Leitstand MUSS weiterhin ueber FaHierarchyOrderInfo finden.
        var repo = new ProductionOrderRepository(ctx);
        var page = await repo.GetForLeitstandGroupedAsync(null, null, "Mustermann",
            showDone: true, page: 1, pageSize: 50);
        page.Groups.Should().NotBeEmpty();
    }
```

- [ ] **Step 3: Parameter einziehen, FA-Liste umstellen, Leitstand unveraendert lassen**

- [ ] **Step 4: Beide Tests gruen + Commit**

```bash
dotnet test IdealAkeWms.Tests
git add IdealAkeWms
git commit -m "feat: Kunde-Filter der FA-Liste ueber C#-Postfilter, Leitstand-Subquery unveraendert"
```

---

### Task 8: Die fuenf neuen Spalten in der FA-Liste anzeigen

**Files:**
- Modify: `IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs` (nur `ProductionOrders`)
- Modify: `IdealAkeWms/Views/ProductionOrders/Index.cshtml` (`<thead>`, `#column-config`)
- Modify: `IdealAkeWms/Views/ProductionOrders/_ProductionOrderRow.cshtml` (Zellen)

- [ ] **Step 1: Fuenf `ColumnDef` ergaenzen — nur in `ProductionOrders`**

```csharp
            new ColumnDef("prio",                "Prio",                DefaultWidth: 55),
            new ColumnDef("ab-nummer",           "AB-Nr.",              DefaultWidth: 80),
            new ColumnDef("montage-abteilung",   "Montage-Abt."),
            new ColumnDef("konstruktions-termin","KO-Termin"),
```

`customer` existiert bereits als Spalte — **nicht** doppelt anlegen. Position: die vier neuen hinter
`customer`.

- [ ] **Step 2: `<thead>` + `#column-config` an derselben Position pflegen** (vier Stellen-Regel aus
  Task 3 gilt unveraendert)

```html
                    <th data-filterable data-col-key="prio">Prio</th>
                    <th data-filterable data-col-key="ab-nummer">AB-Nr.</th>
                    <th data-filterable data-col-key="montage-abteilung">Montage-Abt.</th>
                    <th data-filterable data-col-key="konstruktions-termin" data-date-filter>KO-Termin</th>
```

`colCount` in `Index.cshtml:105-106` **mitzaehlen** (heute 20/21 → 25/26 nach Matchcode + vier).
Wird das vergessen, ist die Gruppen-Kopfzeile zu kurz und die Tabelle verrutscht optisch.

- [ ] **Step 3: Zellen im Row-Partial, mit `data-repeat-key` (D5)**

```html
                        <td data-repeat-key="customer">@item.Customer</td>
                        <td data-repeat-key="prio">@item.Prio</td>
                        <td data-repeat-key="ab-nummer">@item.AbNummer</td>
                        <td data-repeat-key="montage-abteilung">@item.MontageAbteilung</td>
                        <td data-repeat-key="konstruktions-termin">@FormatDateWithKw(item.KonstruktionsTermin)</td>
```

Die bestehende `customer`-Zelle (`:59`) wird dabei **ersetzt**, nicht ergaenzt.
`data-repeat-key` **nur** auf diesen Zellen — sie sind die Wiederholungskandidaten.

- [ ] **Step 4: Build + Commit**

```bash
dotnet build IdealAkeWms.slnx
git add IdealAkeWms
git commit -m "feat: Prio/AB-Nr./Montage-Abt./KO-Termin als Zeilenspalten der FA-Liste"
```

---

# Block 3 — Wechsel-Darstellung, HauptFA-Zeile, schlanke Kopfzeile

### Task 9: Sortier-Hook in `table-filter.js` + B-2 im selben Griff

**Files:**
- Modify: `IdealAkeWms/wwwroot/js/table-filter.js:314-365` (`sortTable`), `:567` (`triggerSort`)

**Interfaces:**
- Produces: `document`-Event `table-sorted` mit `detail: { viewKey, colKey, dir }` — genutzt von Task 10.

> **Gemeinsam genutztes JS — EIN Griff, EIN Regressionsnachweis.** Diese Datei haengt an rund 30
> Listen. Deshalb kommen hier **beide** Aenderungen zusammen: der neue Hook (fuer D1) und der
> geparkte Befund **B-2**. Zwei getrennte Eingriffe wuerden zwei vollstaendige Regressionsnachweise
> kosten.

- [ ] **Step 1: Event am Ende von `sortTable` dispatchen**

Nach der `forEach`-Schleife ueber die `<tbody>`-Elemente (hinter Zeile 364):

```javascript
        document.dispatchEvent(new CustomEvent('table-sorted', {
            detail: { viewKey: getViewKey(), colKey: colKey, dir: dir }
        }));
```

Eine Stelle deckt **beide** Aufrufwege ab: den `<th>`-Klick (Zeile 244) und `window.triggerSort`
(Default-Sort beim Init, Zeile 252/583). Rein additiv — auf `table-sorted` hoert heute niemand.

- [ ] **Step 2: B-2 beheben — die beiden stillen Abbrueche melden**

In `sortTable`, Zeile 317:

```javascript
        if (colIndex < 0) {
            console.warn('[table-filter] Sortierung uebersprungen: Spalte "' + colKey +
                '" ist in dieser Tabelle nicht aufloesbar (viewKey: ' + getViewKey() + ').');
            return;
        }
```

In `window.triggerSort`, Zeile 573:

```javascript
        if (!th) {
            console.warn('[table-filter] triggerSort ohne Wirkung: kein filterbarer Spaltenkopf "' +
                colKey + '" (viewKey: ' + getViewKey() + ').');
            return;
        }
```

- [ ] **Step 3: Regressionsnachweis — JE TABELLE, nicht je Datei**

Pruefliste (Ein-`tbody`-Tabellen muessen sich **identisch** verhalten):
- `/Articles`, `/StockOverview`, `/Users` — je eine Tabelle, ein `<tbody>`: Sortierklick sortiert wie
  vorher.
- `/BdeMasterData` — **drei getrennte Tabellen mit je einem `<tbody>`**: jede sortiert unabhaengig.
  (Eine Zaehlung der `<tbody>` je *Datei* ist hier irrefuehrend — deshalb je Tabelle pruefen.)
- `/FaHierarchyKommissionierListen` — mehrere `<tbody>` in **einer** Tabelle: jede Gruppe sortiert in
  sich, keine Zeile wandert ueber eine Gruppengrenze.
- Browser-Konsole: bei normaler Nutzung **keine** neuen Warnungen.

- [ ] **Step 4: Build + Commit**

```bash
dotnet build IdealAkeWms.slnx
git add IdealAkeWms/wwwroot/js/table-filter.js
git commit -m "feat: table-sorted-Event + Befund B-2 (stiller Sortier-Abbruch meldet sich jetzt)"
```

---

### Task 10: Wiederholungswerte unterdruecken (client-seitig, nach jedem Reorder)

**Files:**
- Create: `IdealAkeWms/wwwroot/js/fa-liste-wiederholung.js`
- Modify: `IdealAkeWms/wwwroot/css/site.css` (hinter `.fa-liste-chevron`, Z. ~1250)
- Modify: `IdealAkeWms/Views/ProductionOrders/Index.cshtml` (Script-Tag im `@section Scripts`)

**Interfaces:**
- Consumes: `data-repeat-key`-Zellen (Task 8), Event `table-sorted` (Task 9),
  Event `table-filter-applied` (bestehend).

- [ ] **Step 1: CSS-Klasse**

In `site.css`, direkt hinter `.fa-liste-chevron`:

```css
/* Wiederholungswert in der FA-Liste: Glyphen unsichtbar, Wert bleibt im DOM.
   Bewusst NICHT display:none und NICHT leerer Zellinhalt — sonst verschwindet der Wert
   fuer Auswahl, Kopieren und Export. Bewusst auch NICHT gedimmt: absichtlich kontrastarmer
   Text widerspricht der Kontrastregel (Terminals in der Fertigung, teils schlechtes Licht);
   eine leere Zelle bedeutet "wie darueber" und ist eine alte Tabellenkonvention. */
.fa-liste-repeat-hidden {
    color: transparent;
}
```

- [ ] **Step 2: Modul anlegen**

```javascript
// FA-Liste: Wiederholungswerte je HauptFA-Gruppe nur an der ersten Zeile zeigen.
// Client-seitig, weil die Sortierung client-seitig laeuft (table-filter.js) — eine
// serverseitig gesetzte Markierung waere nach dem ersten Sortierklick falsch.
// Der Wert bleibt im DOM; unterdrueckt wird ausschliesslich die Darstellung.
(function () {
    'use strict';

    var CLASS = 'fa-liste-repeat-hidden';

    function apply(table) {
        table.querySelectorAll('tbody.fa-liste-group').forEach(function (tbody) {
            var last = {};
            tbody.querySelectorAll('tr.fa-liste-node-row').forEach(function (row) {
                if (row.style.display === 'none') return;          // gefilterte Zeile zaehlt nicht
                row.querySelectorAll('[data-repeat-key]').forEach(function (cell) {
                    var key = cell.getAttribute('data-repeat-key');
                    var val = (cell.textContent || '').trim();
                    if (val !== '' && last[key] === val) {
                        cell.classList.add(CLASS);
                    } else {
                        cell.classList.remove(CLASS);
                        last[key] = val;
                    }
                });
            });
        });
    }

    function applyAll() {
        document.querySelectorAll('table[data-hierarchical="true"]').forEach(apply);
    }

    applyAll();
    document.addEventListener('table-sorted', applyAll);
    document.addEventListener('table-filter-applied', applyAll);
    document.addEventListener('fa-liste-group-toggled', applyAll);
})();
```

> Drei Eigenschaften, die bewusst so sind: (1) `data-hierarchical="true"` gated das Modul — bei
> Master `false` laeuft es **nie**, AKE bleibt unberuehrt. (2) Ein **leerer** Wert wird nie
> unterdrueckt, sonst koennte eine leere Zelle eine gefuellte maskieren. (3) Zeilen mit
> `display: none` werden uebersprungen, damit nach einem Filter die erste **sichtbare** Zeile den
> Wert zeigt — nicht eine ausgeblendete.

- [ ] **Step 3: Einbinden — nach `table-filter.js`**

In `Views/ProductionOrders/Index.cshtml`, `@section Scripts`, **hinter** `fa-liste-gruppierung.js`:

```html
    <script src="~/js/fa-liste-wiederholung.js" asp-append-version="true"></script>
```

- [ ] **Step 4: Am Bildschirm pruefen** (Bedingung aus der Constitution: ohne gesehene View keine
  Erfolgsmeldung)

Testsystem, Master an, `/ProductionOrders`: Zwei aufeinanderfolgende Zeilen mit gleichem Kunden →
nur die erste zeigt ihn. Dann Sortierklick → Markierung sitzt wieder an der ersten Zeile der **neuen**
Reihenfolge. Dann Zelle markieren und kopieren → der Wert ist im Clipboard.

- [ ] **Step 5: Commit**

```bash
git add IdealAkeWms/wwwroot/js/fa-liste-wiederholung.js IdealAkeWms/wwwroot/css/site.css IdealAkeWms/Views/ProductionOrders/Index.cshtml
git commit -m "feat: Wiederholungswerte der FA-Liste nur an der ersten Zeile zeigen (client-seitig)"
```

---

### Task 11: HauptFA-Zeile kennzeichnen + Kopfzeile verschlanken

**Files:**
- Modify: `IdealAkeWms/Views/ProductionOrders/_ProductionOrderRow.cshtml:13-22`
- Modify: `IdealAkeWms/Views/ProductionOrders/Index.cshtml:133-167` (Badges)

- [ ] **Step 1: HauptFA-Zeile erkennen und markieren (D2)**

Im Kopfblock des Partials, neben `faCellNumber` (Zeile 13):

```csharp
    var isHauptFaRow = Model.Hierarchical
        && !string.IsNullOrEmpty(item.SubOrderNumber)
        && item.SubOrderNumber == item.OrderNumber;
```

Die Erkennung braucht **kein** neues ViewModel-Feld: Die Wurzel ist genau die Zeile, deren Sub-FA
gleich der HauptFA ist (bestaetigt durch die Test-Fixture `Node(100, 100, null)` und die Messung am
Testsystem). Dann in `rowClass` (Zeile 20):

```csharp
    if (isHauptFaRow) rowClass = (rowClass + " fw-semibold").Trim();
```

Und in der FA-Nummer-Zelle ein Badge **mit Text**, nicht nur Farbe:

```html
                            @if (isHauptFaRow)
                            {
                                <span class="badge rounded-pill bg-secondary ms-1">HauptFA</span>
                            }
```

- [ ] **Step 2: Kopfzeile verschlanken (D3)**

In `Index.cshtml` im `@if (!group.IsAmbiguous)`-Block (Z. 133-167) **entfernen**: die Badges fuer
`Prio`, `MontageAbteilung`, `AbNummer`, `KonstruktionsTermin` und `DeliveryDate`.
**Bleiben**: `Customer` (`bg-primary`) und `ProductionDate` („FE …").
Der `else`-Zweig mit „mehrdeutig (Kombigerät)" (Z. 164-167) bleibt **unveraendert**, ebenso der
`HeadVariants`-Block (Z. 170-205) — einschliesslich der Klasse `fa-liste-node-row` auf dessen `<tr>`,
**ohne die bleibt die Varianten-Tabelle beim Zuklappen stehen** (das war der fuenfte stille Fehler
des letzten Laufs, Commit `be92ade`).

- [ ] **Step 3: Am Bildschirm pruefen**

Gruppe **zuklappen** → Kopfzeile zeigt weiterhin HauptFA-Nummer, Sub-FA-Zahl, Kunde, FE-Termin.
HauptFA-Zeile ist halbfett und traegt das Badge. Kombigeraet-Gruppe: Varianten-Tabelle klappt mit zu.

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Views/ProductionOrders
git commit -m "feat: HauptFA-Zeile gekennzeichnet, Gruppen-Kopfzeile auf Kunde + Leittermin verschlankt"
```

---

# Block 4 — Freigabe-Kaskade am Leitstand

### Task 12: `SetReleaseForOrderNumberAsync` im Repository

**Files:**
- Modify: `IdealAkeWms/Data/Repositories/IProductionOrderPickingStatusRepository.cs`
- Modify: `IdealAkeWms/Data/Repositories/ProductionOrderPickingStatusRepository.cs` (neben `SetReleaseBatchAsync`, Z. 218)
- Test: `IdealAkeWms.Tests/Repositories/ProductionOrderPickingStatusRepositoryTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  Task<int> SetReleaseForOrderNumberAsync(string orderNumber, int? assignedPickerId,
      string? assignedPickerName, string? releasedBy, string modifiedBy, string modifiedByWindows);
  ```
  Rueckgabe = Anzahl **tatsaechlich** geaenderter Zeilen. Genutzt von Task 13.

- [ ] **Step 1: Fehlschlagende Tests schreiben** (Muster:
  `ProductionOrderBdeStatusRepositoryTests.cs:45-84`)

```csharp
    [Fact]
    public async Task SetReleaseForOrderNumber_ReleasesOnlyUnreleasedRows_OfThatHauptFa()
    {
        using var ctx = NewContext();
        // HauptFA "100": zwei Sub-FAs offen, eine bereits freigegeben; HauptFA "200" als Fremdgruppe
        var affected = await repo.SetReleaseForOrderNumberAsync("100", null, null, "Tester",
            "TestUser", "DOMAIN\\testuser");

        affected.Should().Be(2);
        // Fremdgruppe unberuehrt
        (await ctx.ProductionOrderPickingStatuses.SingleAsync(s => s.ProductionOrderId == foreignId))
            .IsReleasedForPicking.Should().BeFalse();
    }

    [Fact]
    public async Task SetReleaseForOrderNumber_LeavesAlreadyReleasedRowsUntouched_IncludingAudit()
    {
        // Die bereits freigegebene Zeile behaelt ModifiedAt, ModifiedBy UND AssignedPickerName.
        var before = existing.ModifiedAt;
        await repo.SetReleaseForOrderNumberAsync("100", 7, "Picker A", "Tester", "TestUser", "DOMAIN\\testuser");
        existing.ModifiedAt.Should().Be(before);
        existing.AssignedPickerName.Should().Be("Bestandspicker");
    }

    [Fact]
    public async Task SetReleaseForOrderNumber_AssignsPickerToAllNewlyReleasedRows()
    {
        var affected = await repo.SetReleaseForOrderNumberAsync("100", 7, "Picker A", "Tester", "TestUser", "DOMAIN\\testuser");
        affected.Should().Be(2);
        ctx.ProductionOrderPickingStatuses.Where(s => s.IsReleasedForPicking && s.ProductionOrderId != existingId)
            .Should().OnlyContain(s => s.AssignedPickerId == 7 && s.AssignedPickerName == "Picker A");
    }

    [Fact]
    public async Task SetReleaseForOrderNumber_AssignsPriority_InSubOrderNumberOrder()
    {
        // Sub-FAs absichtlich in umgekehrter Reihenfolge anlegen: "103" vor "102".
        await repo.SetReleaseForOrderNumberAsync("100", null, null, "Tester", "TestUser", "DOMAIN\\testuser");
        var byKey = await ctx.ProductionOrderPickingStatuses
            .Include(s => s.ProductionOrder).ToListAsync();
        var prio102 = byKey.Single(s => s.ProductionOrder!.SubOrderNumber == "102").PickingPriority;
        var prio103 = byKey.Single(s => s.ProductionOrder!.SubOrderNumber == "103").PickingPriority;
        prio102.Should().BeLessThan(prio103!.Value);   // Z2: SubOrderNumber aufsteigend
    }

    [Fact]
    public async Task SetReleaseForOrderNumber_UnknownOrderNumber_ReturnsZero()
    {
        (await repo.SetReleaseForOrderNumberAsync("999", null, null, "T", "T", "T")).Should().Be(0);
    }
```

- [ ] **Step 2: Fehlschlag bestaetigen**

```bash
dotnet test IdealAkeWms.Tests --filter SetReleaseForOrderNumber
```
Erwartet: Compile-Fehler (Methode fehlt).

- [ ] **Step 3: Implementieren**

Muster: Zwei-Schritt-Query wie `SetIsDoneBdeForOrderNumberAsync` (IDs lokal, dann `Contains` —
InMemory-testbar), Prioritaetsvergabe wie `SetReleaseBatchAsync` (`maxPrio` **einmal** vorab, dann
`++maxPrio`), **ein** `SaveChangesAsync`, gemeinsamer Zeitstempel `DateTime.UtcNow` (D7).

```csharp
    public async Task<int> SetReleaseForOrderNumberAsync(string orderNumber, int? assignedPickerId,
        string? assignedPickerName, string? releasedBy, string modifiedBy, string modifiedByWindows)
    {
        if (string.IsNullOrWhiteSpace(orderNumber)) return 0;

        // Schritt 1: Zielzeilen lokal bestimmen — in SubOrderNumber-Reihenfolge (Z2),
        // damit die Prioritaet nicht von der DB-Lesereihenfolge abhaengt.
        var targets = await _context.ProductionOrderPickingStatuses
            .Include(s => s.ProductionOrder)
            .Where(s => s.ProductionOrder != null
                        && s.ProductionOrder.OrderNumber == orderNumber
                        && !s.IsReleasedForPicking)
            .ToListAsync();

        targets = targets
            .OrderBy(s => s.ProductionOrder!.SubOrderNumber, StringComparer.Ordinal)
            .ToList();

        if (targets.Count == 0) return 0;

        var maxPrio = await GetMaxPickingPriorityAsync();
        var now = DateTime.UtcNow;

        foreach (var row in targets)
        {
            row.IsReleasedForPicking = true;
            row.ReleasedAt = now;
            row.ReleasedBy = releasedBy;
            if (assignedPickerId.HasValue)
            {
                row.AssignedPickerId = assignedPickerId;
                row.AssignedPickerName = assignedPickerName;
            }
            if (!row.PickingPriority.HasValue) row.PickingPriority = ++maxPrio;
            row.ModifiedAt = now;
            row.ModifiedBy = modifiedBy;
            row.ModifiedByWindows = modifiedByWindows;
        }

        await _context.SaveChangesAsync();
        return targets.Count;
    }
```

> Zwei bewusste Entscheidungen: `!s.IsReleasedForPicking` filtert **in der Abfrage**, damit bereits
> freigegebene Zeilen gar nicht geladen und nicht versehentlich gestempelt werden. Und der Picker
> wird nur gesetzt, **wenn** einer uebergeben ist — bei inaktiver Zuweisungspflicht bleibt das Feld
> leer, wie bei der Einzelfreigabe.

- [ ] **Step 4: Tests gruen + Commit**

```bash
dotnet test IdealAkeWms.Tests
git add IdealAkeWms/Data/Repositories IdealAkeWms.Tests
git commit -m "feat: SetReleaseForOrderNumberAsync (atomar, Prio nach SubOrderNumber, Picker fuer alle)"
```

---

### Task 13: Controller-Actions mit unveraenderter Zuweisungspflicht

**Files:**
- Modify: `IdealAkeWms/Controllers/PickingLeitstandController.cs` (neben `CascadeDone`, Z. 469-511)
- Test: `IdealAkeWms.Tests/Controllers/PickingLeitstandControllerTests.cs` (Muster Z. 697-743)

**Interfaces:**
- Consumes: `SetReleaseForOrderNumberAsync` (Task 12).
- Produces: Routen `CascadeReleasePreview` (GET), `CascadeRelease` (POST).

- [ ] **Step 1: Fehlschlagende Tests schreiben — der Negativtest ist der wichtigste**

```csharp
    [Fact]
    public async Task CascadeRelease_Rejects_WhenPickerAssignmentEnabled_AndNoPickerGiven()
    {
        SetSetting(AppSettingKeys.KommissionierungMitZuweisung, "true");

        var result = await controller.CascadeRelease("100", assignedPickerId: null, returnUrl: null);

        controller.TempData["WarningMessage"].Should().Be("Bitte einen Kommissionierer zuweisen.");
        _pickingStatusRepo.Verify(r => r.SetReleaseForOrderNumberAsync(
            It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CascadeRelease_Releases_WhenPickerAssignmentDisabled_WithoutPicker()
    {
        SetSetting(AppSettingKeys.KommissionierungMitZuweisung, "false");
        _pickingStatusRepo.Setup(r => r.SetReleaseForOrderNumberAsync("100", null, null,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(3);

        await controller.CascadeRelease("100", null, null);

        controller.TempData["SuccessMessage"].Should().Be("HauptFA 100: 3 Sub-FA(s) freigegeben.");
    }

    [Fact]
    public async Task CascadeReleasePreview_ReturnsTotalAlreadyReleasedAndToRelease() { /* … */ }

    [Fact]
    public async Task CascadeRelease_DoesNothing_WhenOrderNumberMissing() { /* … */ }
```

`SetSetting(...)` ist der in dieser Testdatei bereits vorhandene Helfer fuer
`_settingRepository`-Moqs — Namen aus den Nachbartests uebernehmen.

- [ ] **Step 2: Fehlschlag bestaetigen**

- [ ] **Step 3: Actions implementieren**

```csharp
    [HttpGet]
    [RequireLeitstandAccess]
    public async Task<IActionResult> CascadeReleasePreview(string? orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber)) return BadRequest("orderNumber fehlt.");
        var totalSubFas = await _productionOrderRepository.CountByOrderNumberAsync(orderNumber);
        var alreadyReleased = await _pickingStatusRepository.CountReleasedByOrderNumberAsync(orderNumber);
        return Ok(new { orderNumber, totalSubFas, alreadyReleased, toRelease = totalSubFas - alreadyReleased });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireLeitstandAccess]
    public async Task<IActionResult> CascadeRelease(string? orderNumber, int? assignedPickerId, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            TempData["WarningMessage"] = "Keine HauptFA angegeben — nichts freigegeben.";
            return RedirectBackOrIndex(returnUrl);
        }

        // Dieselbe harte Pflicht wie ToggleRelease (Z. 346-351) und BulkRelease (Z. 391-396).
        // Eine Massenaktion darf eine Pflicht der Einzelaktion nicht umgehen.
        var pickerAssignmentEnabled = (await _settingRepository.GetValueAsync(
            AppSettingKeys.KommissionierungMitZuweisung))
            ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        if (pickerAssignmentEnabled && !assignedPickerId.HasValue)
        {
            TempData["WarningMessage"] = "Bitte einen Kommissionierer zuweisen.";
            return RedirectBackOrIndex(returnUrl);
        }

        string? pickerName = null;
        if (assignedPickerId.HasValue)
            pickerName = (await _userRepository.GetByIdAsync(assignedPickerId.Value))?.Name;

        var affected = await _pickingStatusRepository.SetReleaseForOrderNumberAsync(
            orderNumber, assignedPickerId, pickerName,
            _currentUserService.GetDisplayName(),
            _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName());

        _logger.LogInformation(
            "IDEAL-Kaskade Freigabe (IsReleasedForPicking): HauptFA {OrderNumber} → {Count} Sub-FA(s) " +
            "freigegeben durch {User} ({Windows}), Kommissionierer {Picker}.",
            orderNumber, affected, _currentUserService.GetDisplayName(),
            _currentUserService.GetWindowsUserName(), pickerName ?? "(keiner)");

        TempData["SuccessMessage"] = affected > 0
            ? $"HauptFA {orderNumber}: {affected} Sub-FA(s) freigegeben."
            : $"HauptFA {orderNumber}: alle Sub-FAs waren bereits freigegeben — nichts geändert.";
        return RedirectBackOrIndex(returnUrl);
    }
```

`CountReleasedByOrderNumberAsync` ist neu und klein — im selben Task zum Repository hinzufuegen
(Signatur: `Task<int> CountReleasedByOrderNumberAsync(string orderNumber)`).

- [ ] **Step 4: Tests gruen + Commit**

```bash
dotnet test IdealAkeWms.Tests
git add IdealAkeWms IdealAkeWms.Tests
git commit -m "feat: CascadeRelease/-Preview mit unveraenderter Kommissionierer-Pflicht"
```

---

### Task 14: Knopf und Dialog in der Gruppen-Kopfzeile

**Files:**
- Modify: `IdealAkeWms/Views/PickingLeitstand/Index.cshtml` (Knopf bei Z. ~194, Modal bei Z. ~346, JS bei Z. ~433)

- [ ] **Step 1: Knopf neben den bestehenden**

```html
                        @if (Model.CanManagePickingRelease)
                        {
                            <button type="button" class="btn btn-sm btn-outline-primary ms-2 btn-cascade-release"
                                    data-order-number="@group.OrderNumber">
                                Alle Sub-FAs freigeben
                            </button>
                        }
```

> **Bewusst `CanManagePickingRelease`, nicht `CanPick`** — Freigabe ist ein Leitstand-Recht. Damit
> haben die beiden Knoepfe in derselben Kopfzeile **unterschiedliche** Sichtbarkeitsbedingungen. Das
> ist fachlich richtig und sieht beim Testen nach einem Fehler aus; es gehoert deshalb in die
> manuelle Checkliste.

- [ ] **Step 2: Modal — `cascadeDoneModal` spiegeln (D4)**

Gerendert unter `@if (Model.Hierarchical && Model.CanManagePickingRelease)`. Form auf
`CascadeRelease`, `@Html.AntiForgeryToken()`, Hidden `orderNumber` + `returnUrl`. Body: Gesamtzahl,
davon bereits freigegeben, davon neu freizugeben. **Bei aktiver Zuweisungspflicht** das
Pflicht-Select analog `bulkReleaseModal` (Z. 322-331) — Quelle `ViewBag.ActivePickers as List<User>`:

```html
                    @if (Model.PickerAssignmentEnabled)
                    {
                        <div class="mb-2">
                            <label for="cascadeAssignedPickerIdSelect" class="form-label">Kommissionierer</label>
                            <select name="assignedPickerId" id="cascadeAssignedPickerIdSelect"
                                    class="form-select" required>
                                <option value="">— Bitte auswaehlen —</option>
                                @foreach (var p in activePickers)
                                {
                                    <option value="@p.Id">@p.Name</option>
                                }
                            </select>
                            <div class="form-text">
                                Der gewählte Kommissionierer wird allen
                                <span id="cascadeReleaseCountInline">—</span> Sub-FAs zugewiesen und
                                kann danach je Sub-FA geändert werden.
                            </div>
                        </div>
                    }
```

- [ ] **Step 3: JS — `btn-cascade-done`-Block spiegeln**

`e.stopPropagation()` (sonst klappt die Gruppe zu), `bootstrap.Modal.getOrCreateInstance`,
`fetch` auf `CascadeReleasePreview`, Zahlen einsetzen, Submit erst nach Antwort aktiv. Im `catch`
**trotzdem** freigeben — „der Server ist die Wahrheit", wie beim bestehenden Dialog.

- [ ] **Step 4: Am Bildschirm pruefen**

Mit aktiver Zuweisungspflicht: Absenden ohne Auswahl ist nicht moeglich (`required`), und falls doch
(z. B. per direktem POST) erscheint „Bitte einen Kommissionierer zuweisen.". Mit inaktiver Pflicht:
kein Select, Kaskade laeuft durch. Gruppe klappt beim Knopfdruck **nicht** zu.

- [ ] **Step 5: Commit**

```bash
git add IdealAkeWms/Views/PickingLeitstand/Index.cshtml
git commit -m "feat: Gruppen-Knopf und Dialog fuer die Freigabe-Kaskade"
```

---

# Block 5 — Abschluss

### Task 15: Version, Changelog, Testszenarien

**Files:**
- Modify: `IdealAkeWms/AppVersion.cs` **und** `IDEALAKEWMSService/AppVersion.cs` → `1.38.0`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml`
- Modify: `docs/TESTSZENARIEN.md` (neues TS-72)

- [ ] **Step 1: Beide `AppVersion.cs` auf `1.38.0`** (eine vergessen = stiller Versionsdrift)
- [ ] **Step 2: Anwender-Changelog** — in der Sprache der Anwender, nicht der Entwickler:
  Matchcode-Spalte, Kopfdaten je Zeile, HauptFA als Zeile, „Alle Sub-FAs freigeben".
  **Mit** dem Hinweis, dass der Matchcode am Standort AKE bis zur View-Erweiterung leer bleibt.
- [ ] **Step 3: `docs/TESTSZENARIEN.md`** — TS-72 mit je einem Szenario pro AK (20 AKs), inkl.
  Negativfaellen: Kaskade ohne Picker bei aktiver Pflicht, Filter auf durchgehend leerer
  Matchcode-Spalte, Flachmodus-Regression, Kombigeraet-Gruppe mit leeren K1-Spalten.
- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/AppVersion.cs IDEALAKEWMSService/AppVersion.cs IdealAkeWms/Views/Help/Changelog.cshtml docs/TESTSZENARIEN.md
git commit -m "chore: v1.38.0, Anwender-Changelog, TS-72"
```

---

### Task 16: QA (nicht vom Umsetzer)

- [ ] `superpowers:verification-before-completion` + Code-Review
- [ ] Subagent `qa-agent` — er allein darf `status: Testbereit` setzen, nur mit gruenem Beweis
- [ ] **Brain-Updates gehen in den HAUPTCHECKOUT**, nicht in den Worktree: Spec-Status + QA-Nachweis,
      Aufgaben-Notiz, `feature-map`, `changelog/2026-09-10-v1-38-0-*.md`, `testszenarien-index`,
      und die drei Nebenbefunde N1-N3 aus der Aufgaben-Notiz.

---

## Self-Review gegen die Spec

**Spec-Deckung, AK fuer AK:** AK 1 → Task 2 · AK 2 → Task 3 (fuenf statt sechs Listen, A1
dokumentiert) · AK 3 → Task 4 · AK 4 → Task 5 · AK 5 → Task 10 · AK 6 → Task 9+10 (Markierung haelt
nach Reorder) · AK 7 → Task 5 (Kombigeraet: Locals bleiben null) · AK 8 → Task 6+7 · AK 9 → Task 11 ·
AK 10 → Task 11 · AK 11 → Task 12+13 · AK 12 → Task 12 · AK 13 → Task 13 · AK 14 → Task 13
(Negativtest) · AK 15 → Task 12 · AK 16 → keine Aktion (es wird keine Ruecknahme gebaut; durch Task
13 belegt, weil nur `true` gesetzt wird) · AK 17 → Task 12/13 (`BulkRelease` unberuehrt) · AK 18 →
Task 13 · AK 19 → bereits erledigt (Anzeige-Spec nachgezogen, Commit `6ebe058`) · AK 20 → Task 5/9/10
(AKE-Pfade strukturell unerreichbar) + Regressionsliste in Task 9.

**Offen und bewusst nicht im Plan:** Die geparkte Frage B-0/Ruling 4 (Vollstruktur in der
Vorbau-Stueckliste) ist **ein Test am Bildschirm vor dem Merge**, keine Umsetzungsaufgabe — sie
gehoert in die manuelle Checkliste, nicht in einen Task.
