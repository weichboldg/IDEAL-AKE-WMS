---
type: spec
title: Liefertermin offener Bestellungen in Artikelinfo und Lagerbestellung-Details
slug: 2026-11-26-liefertermin-offene-bestellungen-artikelinfo-lagerbestellung-spec
status: Testbereit
created: 2026-11-26
updated: 2026-11-26
source_backlog: "[[2026-11-26-liefertermin-offene-bestellungen-artikelinfo-lagerbestellung]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
affected_code:
  - IdealAkeWms/Data/Repositories/SagePurchaseOrderLineRepository.cs
  - IdealAkeWms/Controllers/ArticlesController.cs
  - IdealAkeWms/Models/ViewModels/ArticleInfoViewModel.cs
  - IdealAkeWms/Views/Articles/Info.cshtml
  - IdealAkeWms/Controllers/WarehousePickingController.cs
  - IdealAkeWms/Models/ViewModels/WarehouseRequisitionDetailViewModel.cs
  - IdealAkeWms/Views/WarehousePicking/Details.cshtml
  - IdealAkeWms/Views/Help/Index.cshtml
  - IdealAkeWms.Tests (ArticlesControllerTests, ArtikelinfoLieferantenTests, FraesenPfadTests, PurchaseProposalsNachbesserungE4Tests, WarehousePickingControllerTests, WarehousePickingVerbrauchsbuchungTests – nur Konstruktor; neue Tests)
open_questions: []
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: "freigegeben"
freigabe_von: "Gerald (Chat)"
freigabe_am: "2026-11-26"
---

## Ziel / Nutzen (das Warum)

Das Lager soll sehen, wann für einen Artikel eine Lieferung kommen sollte, ohne in Sage oder in „Offene Bestellungen“ zu
suchen: in der Artikelinfo vollständig (je offene Bestellposition), in der Lagerbestellung-Detailansicht kompakt (nächster
Termin je Position). Die Daten liegen schon im WMS (Cache `SagePurchaseOrderLines`), es fehlt nur die Anzeige.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. Artikelinfo (`ArticlesController.Info`, `Views/Articles/Info.cshtml`): neue Karte „Offene Bestellungen“.
2. Lager: Eingehende Listen → Details (`WarehousePickingController.Details`, `Views/WarehousePicking/Details.cshtml`, gilt für
   Lager- und Glasbestellungen, dieselbe View): Infospalte „Liefertermin“, über die Spaltenauswahl ein-/ausblendbar,
   Standard ausgeblendet.
3. Eine neue Lesemethode im vorhandenen Repository `ISagePurchaseOrderLineRepository`.

**Out-of-Scope**
- Kein neuer Sync, kein Live-Lesen aus Sage, keine Migration, keine neue Einstellung.
- Druck der Lagerbestellung (`WarehousePicking/Print`) bleibt unverändert (die neue Spalte erscheint dort nicht).
- Partial `_ArtikelSageKarten` bleibt unverändert; die Karte kommt nur in die Artikelinfo, nicht in „Artikel bearbeiten“.
- Kein Tooltip mit allen Terminen in der Details-Zelle (die Artikelinfo zeigt alle; Backlog „Verworfen: alle Termine in der Zelle“).
- Keine Änderung an „Offene Bestellungen“ (Lager/Einkauf), Bestellvorschlägen, Lieferverzug-Mail.

## Fachliche Anforderungen

Belegte Ausgangslage (Code im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`):

- **Datenquelle und Einheit.** `SagePurchaseOrderLines` = offene Sage-Bestellpositionen EBB + EBZ, je Lauf vollständig ersetzt
  (`IDEALAKEWMSService/Services/Sdata/EinkaufBestellungenSyncService.cs:19-33`). `MengeOffen` = bestellt − geliefert **in der
  Basiseinheit**, Einheitstext in `MengeBasiseinheit` (`Models/SagePurchaseOrderLine.cs:9-11,40-47`; Schreibweg
  `EinkaufBestellungenSyncService.cs:199-200`). `EinheitAbweichend` = Bestell- oder Basiseinheit ≠ Lagereinheit (`SagePurchaseOrderLine.cs:49-51`).
  `Liefertermin` = Termin der Position, ersatzweise des Belegs (`SagePurchaseOrderLine.cs:55-56`).
- **Artikel-Zuordnung.** Der Abgleich speichert `Artikelnummer` getrimmt (`EinkaufBestellungenSyncService.cs:162`) und löst
  `ArticleId` über `Articles.ArticleNumber.Trim()` ohne Groß/Klein auf (`:140-142`, `:198`); Positionen ohne WMS-Artikel behalten
  `ArticleId = null`.
- **Standort.** Jede Installation hat ihren eigenen Cache in der eigenen WMS-Datenbank; gefüllt wird er nur, wenn
  `Sync:EinkaufBestellungenEnabled` an ist (Default `false`, `Models/ServiceSettingDefinitions.cs:380`; Gate
  `IDEALAKEWMSService/Workers/SyncWorker.cs:500`). Laut Katalog ist der Schalter für **AKE** gedacht, **IDEAL** „ohne
  SData-Endpunkt aus lassen“ (`ServiceSettingDefinitions.cs:163`, Manifest `scripts/deploy/nachlauf.psd1:470`). Der Code der
  neuen Anzeige ist standortneutral; auf IDEAL zeigt er Daten erst, wenn dort der Abgleich läuft. Ob IDEAL-Prod den Schalter
  inzwischen an hat, steht nicht im Code → Prüfschritt P-1 unten (unverifiziert).
- **Rest-Regel.** Im WMS schon erfasste, in Sage noch nicht enthaltene Wareneingänge zieht die EINE Rest-Regel ab
  (`ErfasstJeBelPos.Rest`, `Data/Repositories/PurchaseOrderReceiptRepository.cs:22-30`; Rahmenabrufe ziehen alles ab).
  Bestellvorschläge und Lieferverzug-Mail rechnen damit (`Controllers/PurchaseProposalsController.cs:102-110,208`).
- **Überfällig** = `Liefertermin < DateTime.Today` (`Controllers/PurchaseOrdersController.cs:230`).

Anforderungen:

**F1 – Karte „Offene Bestellungen“ in der Artikelinfo**
- F1.1 Position: direkt nach den Karten aus `_ArtikelSageKarten` (nach „Lieferanten“), vor „Teil enthalten in folgenden Fertigungsaufträgen“.
- F1.2 Kopf: „Offene Bestellungen“, rechts klein „nur lesend · aus Sage · Stand: dd.MM.yyyy HH:mm“ (Stand = `GetStandAsync()`).
- F1.3 Tabelle je offener Position mit Rest > 0 (Rest-Regel): **Beleg** (`Belegnummer`, bei EBZ dahinter „Rahmenabruf“ als Text),
  **Lieferant** („Name (Konto)“, ohne Name nur das Konto – wie `PurchaseOrdersController.cs:224`), **Offen** (Rest, `0.###`, mit
  `MengeBasiseinheit`; bei `EinheitAbweichend` zusätzlich Text „Einheit abweichend“), **Liefertermin** (Text wie
  `PurchaseOrdersController.LieferterminText`, also „dd.MM.yyyy KWnn“ bzw. „ohne Termin“; überfällig zusätzlich Text „überfällig“,
  Farbe nur zusätzlich).
- F1.4 Sortierung: Liefertermin aufsteigend, ohne Termin zuletzt, dann Belegnummer, dann BelPosId (wie `PurchaseOrdersController.cs:168`).
- F1.5 Positionen mit Rest 0 (im WMS vollständig erfasst, Sage noch nicht nachgezogen) erscheinen nicht; ihre Anzahl steht als
  Fußzeile „n Position(en) im WMS bereits vollständig erfasst – nicht angezeigt“ (melden statt still).
- F1.6 Hinweis statt leerer Karte (Muster Karte „Lieferanten“, `Views/Shared/_ArtikelSageKarten.cshtml:158-163`):
  - Cache leer (`GetStandAsync() == null`): „Offene Bestellungen werden nicht aus Sage gelesen. Sie kommen aus dem Lauf
    „Offene Bestellungen (Sage)“ (Service-Einstellung `Sync:EinkaufBestellungenEnabled`).“
  - Cache gefüllt, keine offene Position mit Rest > 0: „Keine offene Bestellung für diesen Artikel.“ (plus ggf. Fußzeile F1.5).

**F2 – Infospalte „Liefertermin“ in Lager: Eingehende Listen → Details**
- F2.1 Spalte `liefertermin`, Kopf „Liefertermin“, steht nach „Lagerplatz“; in `column-config` mit `"defaultHidden": true`
  (vorhandenes Muster der Spalte „Fräsen-Pfad“, `Views/WarehousePicking/Details.cshtml:252-256`; `column-preferences.js:68,117`).
- F2.2 Die Spalte gibt es nur, wenn der Cache einen Stand hat (wie `FraesenPfade == null` = keine Spalte,
  `WarehouseRequisitionDetailViewModel.cs:43-44`). Kopf-`title`: „Nächster Liefertermin offener Sage-Bestellungen (Stand dd.MM.yyyy HH:mm)“.
- F2.3 Zelltext je Position aus den offenen Positionen des Artikels mit Rest > 0:
  - keine → „—“;
  - sonst der früheste Termin (überfällige zählen mit, Backlog-Vorschlag) als „dd.MM.yyyy“, überfällig mit Zusatz „ überfällig“,
    nur Positionen ohne Termin → „ohne Termin“; gibt es weitere offene Positionen, Zusatz „ (+N)“ (N = Anzahl − 1).
    Beispiele: „12.12.2026“, „12.12.2026 (+1)“, „05.11.2026 überfällig (+2)“, „ohne Termin“.
  - Artikel `DUMMY` (`Article.DummyArticleNumber`) → immer „—“.
- F2.4 Ist der Zelltext nicht „—“ und hat der Benutzer Stammdaten-Lesezugriff (`ICurrentUserService.HasMasterDataReadAccessAsync`,
  wie `PurchaseProposalsController.cs:160`), ist er ein Link auf `Articles/Info?articleNumber=…` mit `target="_blank"`
  (neuer Tab: Ist-Mengen werden auf der Seite nicht automatisch gespeichert, `Details.cshtml:303-336`; ein Wechsel im selben Tab
  verlöre sie). Kein `returnUrl` (der Rücksprung-Knopf der Artikelinfo ist fest „Zurück zu Bestellvorschlägen“, `Info.cshtml:14-22`).
  Ohne Stammdaten-Lesezugriff nur Text.
- F2.5 Zelle mit `data-filter-text`/`data-csv-value` = Zelltext (Client-Spaltenfilter und CSV wie die übrigen Spalten); `<th>` mit
  `data-col-key="liefertermin" data-filterable`.

**Entscheidungen aus Code/Brain (keine Rückfrage nötig)**
- E1 Einheit: angezeigt wird die Cache-Menge in **ihrer** Basiseinheit mit Einheitstext; die Lagerbestellung zeigt in der Spalte
  keine Menge, daher entsteht dort kein Einheitenvergleich.
- E2 Offene Menge = Rest nach der EINEN Rest-Regel (`ErfasstJeBelPos.Rest`), nicht `MengeOffen` roh – sonst stünde eine im WMS
  schon gebuchte Lieferung weiter als „kommt noch“ da. Rest 0 wird ausgeblendet und gezählt (F1.5).
- E3 Zuordnung über die Artikelnummer (eine Methode für beide Seiten; die Details-Positionen tragen nur `ArticleNumber`,
  `Models/WarehouseRequisitionItem.cs:12`, keine `ArticleId`). Gleichwertig zur `ArticleId`, weil der Abgleich die Id genau über
  diese Nummer auflöst (`EinkaufBestellungenSyncService.cs:140-142`); trifft zusätzlich Positionen ohne WMS-Artikel nicht falsch,
  da die Artikelinfo nur WMS-Artikel aufruft.
- E4 Kein Feature-Gate: `WareneingangBestellungAktiv` steuert die Lager-Menüpunkte (`Models/AppSettingDefinitions.cs:120`), nicht
  die Daten. Leerer Cache wird sichtbar gemeldet (F1.6) bzw. die Spalte entfällt (F2.2).
- E5 Zugriff Details unverändert `[RequireLagerProcessingAccess]` (admin, stock, stock_keyuser; `codebase/controller.md:65`); die
  Spalte zeigt nur ein Datum, keinen Lieferanten.

## Technischer Loesungsentwurf

Kleinster Weg, keine neue Abstraktion (ponytail): eine Repository-Methode, Rest-Regel wiederverwenden, Spalten-Muster
„Fräsen-Pfad“ kopieren.

1. **Repository** `ISagePurchaseOrderLineRepository` (`Data/Repositories/SagePurchaseOrderLineRepository.cs:11-21`) – es gibt
   heute keine Methode je Artikel (nur `GetAllAsync`, `GetByBelIdAsync`, `GetBelIdsByBelegnummerAsync`, `GetStandAsync`).
   Neu: `Task<List<SagePurchaseOrderLine>> GetByArtikelnummernAsync(IReadOnlyCollection<string> artikelnummern)` –
   `AsNoTracking().Where(l => nummern.Contains(l.Artikelnummer))`, Nummern vorher getrimmt, leere Liste → leere Liste ohne Abfrage.
   Groß/Klein macht die CI-Kollation der DB (fallstricke §26 / Prüfschritt P-2 der DB); InMemory-Tests verwenden gleiche Schreibweise
   (fallstricke, Abschnitt „Groß/Klein in SQL macht die Kollation“). Kein Caching-Decorator (der Cache ist die Tabelle,
   Repository-Kommentar `:6-9`). DI ist schon registriert (`Program.cs:95`).
2. **ArticlesController.Info** (`Controllers/ArticlesController.cs:267-365`): Konstruktor um `ISagePurchaseOrderLineRepository` und
   `IPurchaseOrderReceiptRepository` erweitern. Nach dem Artikel-Treffer: `stand = GetStandAsync()`; wenn `stand != null`:
   Zeilen = `GetByArtikelnummernAsync([article.ArticleNumber])`, `erfasst = GetErfasstJeBelPosAsync(BelPosIds)`, je Zeile
   `Rest = erfasst.GetValueOrDefault(l.BelPosId, ErfasstJeBelPos.Leer).Rest(l)`; Rest > 0 → Liste (sortiert F1.4), Rest 0 → Zähler.
   ViewModel `ArticleInfoViewModel` erhält `OffeneBestellungenStand` (`DateTime?`), `OffeneBestellungen`
   (`List<(SagePurchaseOrderLine Line, decimal Rest)>`), `OffeneBestellungenErfasst` (`int`).
3. **Info.cshtml**: Karte nach Zeile 195 (`<partial name="_ArtikelSageKarten" …>`), Bootstrap-Karte `card mb-3` mit
   `table table-sm table-striped mb-0` wie die Lieferanten-Karte; Liefertermin-Text über
   `PurchaseOrdersController.LieferterminText` (internal static, `PurchaseOrdersController.cs:84`). Kleine, unpaginierte,
   vorgefilterte Tabelle → kein Listen-View-Pattern nötig (siehe unten).
4. **WarehousePickingController.Details** (`Controllers/WarehousePickingController.cs:205-257`): Konstruktor um dieselben zwei
   Repositories erweitern. Einmal je Aufruf (nicht in der Positionsschleife): `stand`; wenn `stand != null`, Zeilen aller
   Positions-Artikelnummern (ohne `DUMMY`) in **einer** Abfrage, `erfasst` in **einer** Abfrage, dann
   `Liefertermine = Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)` Artikelnummer → Zelltext.
   Zelltext-Regel als `internal static string LieferterminZelle(IEnumerable<(DateTime? Liefertermin, decimal Rest)> offen, DateTime heute)`
   im Controller (testbar ohne View, Muster `VorschlagHinweis`). `ArtikelinfoLink = HasMasterDataReadAccessAsync()`.
   ViewModel `WarehouseRequisitionDetailViewModel`: `Dictionary<string,string>? Liefertermine` (null = Cache leer → keine Spalte),
   `DateTime? LieferterminStand`, `bool ArtikelinfoLink`. Der Positions-Record `WarehouseRequisitionDetailItemViewModel` bleibt unverändert.
5. **Details.cshtml**: `<th data-col-key="liefertermin" data-filterable title="…">Liefertermin</th>` nach `storage` (nur wenn
   `Model.Liefertermine != null`), Zelle entsprechend; in `column-config` nach `storage` konditional
   `@:{ "key": "liefertermin", "label": "Liefertermin", "locked": false, "defaultWidth": null, "defaultHidden": true },`
   (Komma unkritisch, `note-lager` folgt immer).
6. **Tests** (xUnit/FluentAssertions/InMemory): Konstruktoren in den sechs Testdateien nachziehen (4× `new ArticlesController(`,
   4× `new WarehousePickingController(` laut Suche). Neue Tests siehe Test-Szenarien.
7. **Frontend:** View-Änderung → Skill `frontend-design` explizit aufrufen; Kontrast WCAG AA, „überfällig“ als Text.

## Migrations-/SQL-Auswirkungen

Keine. Keine Model-/Schemaänderung, keine EF-Migration, kein `SQL/XX_*.sql`, `SQL/00_FreshInstall.sql` unverändert, `SQL/AgentJobs/`
nicht betroffen. Abfrage nutzt die vorhandene Tabelle; ein Index auf `Artikelnummer` fehlt (`SQL/112_AddSagePurchaseOrderLines.sql:58-70`
hat BelPosId, BelId, Belegnummer, ArticleId, Liefertermin) – bei ca. 1.000 Zeilen (Repository-Kommentar `:13`) unkritisch;
ponytail: Index erst, wenn eine Messung es verlangt.

## Einstellungen

keine (neu oder geändert). Gelesen wird nur der bestehende Zustand: Cache-Stand aus `Sync:EinkaufBestellungenEnabled` (unverändert).

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| keine | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine. Reine Leseanzeige; `SagePurchaseOrderLine` ist bewusst kein `AuditableEntity` (`SagePurchaseOrderLine.cs:7-8`), es wird
nichts geschrieben.

## Akzeptanzkriterien

1. Artikelinfo eines Artikels mit offenen Bestellpositionen (AKE-DEV, Abgleich an) zeigt die Karte „Offene Bestellungen“ mit Beleg,
   Lieferant, Offen (Rest + Basiseinheit) und Liefertermin je Position, sortiert nach Termin, ohne Termin zuletzt.
2. Eine Position mit Liefertermin vor heute trägt in der Termin-Zelle den Text „überfällig“.
3. Ein Rahmenabruf (EBZ) ist am Text „Rahmenabruf“ erkennbar; eine Position mit `EinheitAbweichend` am Text „Einheit abweichend“.
4. Die angezeigte offene Menge entspricht `ErfasstJeBelPos.Rest` (nach einem WMS-Wareneingang, der noch nicht in Sage ist,
   kleiner als `MengeOffen`); eine Position mit Rest 0 fehlt in der Tabelle und wird in der Fußzeile gezählt.
5. Artikel ohne offene Bestellung (Cache gefüllt): Karte mit Hinweis „Keine offene Bestellung für diesen Artikel.“
6. Cache leer (z. B. IDEAL-DEV mit Schalter aus und leerer Tabelle): Karte mit Hinweis auf `Sync:EinkaufBestellungenEnabled`, keine leere Tabelle.
7. Kopf der Karte zeigt „Stand: …“ = jüngstes `SyncedAt`.
8. Details einer Lagerbestellung: Spalte „Liefertermin“ ist beim ersten Aufruf ausgeblendet und über das Zahnrad einblendbar; die
   Einstellung bleibt nach Neuladen erhalten (Spaltenpräferenz `WarehousePickingDetails`).
9. Eingeblendet zeigt die Zelle den frühesten Termin, „ (+N)“ bei weiteren offenen Positionen, „ überfällig“ bei Termin < heute,
   „ohne Termin“ wenn nur Positionen ohne Termin, „—“ ohne offene Position und für `DUMMY`.
10. Mit Stammdaten-Lesezugriff ist der Termin ein Link, der die Artikelinfo des Artikels in einem neuen Tab öffnet; ohne
    Stammdaten-Lesezugriff (z. B. nur `stock`) reiner Text, kein Link.
11. Cache leer: Details zeigt keine Spalte „Liefertermin“ (auch nicht im Zahnrad); die Seite funktioniert sonst unverändert.
12. Die Details-Seite lädt die Liefertermine mit einer festen Zahl Abfragen (Stand, Bestellpositionen, Erfasst; keine Abfrage
    je Position) – unabhängig von der Positionszahl.
13. Ist-Mengen, Notizen, Fehlteil-Status, Drucken und Abschließen verhalten sich unverändert (Regression).
14. `dotnet build` und `dotnet test` grün.

## Test-Szenarien

Neues Kapitel **TS-206 „Liefertermin offener Bestellungen (Artikelinfo, Lagerbestellung-Details)“** in `docs/TESTSZENARIEN.md`
(nächste freie Nummer: höchste vergebene TS-204 in `docs/TESTSZENARIEN.md` und `secondbrain/tests/testszenarien-index.md:178`,
TS-205 reserviert von [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-uebersicht]]; der Dev-Lauf
prüft erneut). Danach `secondbrain/tests/testszenarien-index.md` nachziehen.

Vorbedingungen: AKE-DEV mit `Sync:EinkaufBestellungenEnabled = true`, Lauf „Offene Bestellungen (Sage)“ gelaufen; Rollen-Benutzer
A (admin oder masterdata_read + stock) und B (nur stock). Testartikel per SQL suchen (nur lesen, WMS-DB):
```sql
SELECT TOP 20 Artikelnummer, COUNT(*) AS Positionen, MIN(Liefertermin) AS Naechster,
       SUM(CASE WHEN Liefertermin < CAST(GETDATE() AS date) THEN 1 ELSE 0 END) AS Ueberfaellig,
       SUM(CASE WHEN Belegkennzeichen = 'EBZ' THEN 1 ELSE 0 END) AS Abrufe
FROM dbo.SagePurchaseOrderLines GROUP BY Artikelnummer HAVING COUNT(*) > 1 ORDER BY COUNT(*) DESC;
```

- **TS-206.1 Artikelinfo mit mehreren Bestellungen:** Artikel aus der Abfrage aufrufen → Karte, Sortierung, Stand, Lieferant, Einheit (AK 1, 7).
- **TS-206.2 Überfällig / Rahmenabruf / Einheit abweichend:** passenden Artikel (Spalte Ueberfaellig/Abrufe > 0) → Texte sichtbar (AK 2, 3).
- **TS-206.3 Rest-Regel:** Wareneingang zu einer Bestellung im WMS teilweise erfassen (noch nicht in Sage übergeben) → Artikelinfo
  zeigt reduzierten Rest; vollständig erfassen → Position weg, Fußzeile zählt sie (AK 4).
- **TS-206.4 Ohne Bestellung:** Artikel ohne Zeile im Cache → Hinweistext (AK 5).
- **TS-206.5 Cache leer:** IDEAL-DEV (Schalter aus, Tabelle leer; vorher `SELECT COUNT(*) FROM dbo.SagePurchaseOrderLines` = 0
  prüfen) → Artikelinfo-Hinweis, Details ohne Spalte (AK 6, 11).
- **TS-206.6 Details Spalte:** Lagerbestellung mit Positionen der Testartikel öffnen → Spalte ausgeblendet; einblenden; Texte
  „(+N)“, „überfällig“, „—“, „ohne Termin“ prüfen; Neuladen → bleibt eingeblendet (AK 8, 9).
- **TS-206.7 Link/Rollen:** Benutzer A → Link öffnet Artikelinfo im neuen Tab, eingetragene Ist-Menge im Ursprungs-Tab bleibt
  stehen; Benutzer B → kein Link (AK 10).
- **TS-206.8 Regression Details:** Ist-Mengen, Fehlteil, Drucken, Drucken und Abschließen, Speichern + Abschließen wie bisher (AK 13).
- **Negativfälle:** Position `DUMMY` → „—“; Glasbestellung (Typ Glas) → Spalte gleich; Spaltenfilter „Liefertermin“ auf „überfällig“ filtert die Zeilen.

Unit-Tests (neu): `SagePurchaseOrderLineRepository.GetByArtikelnummernAsync` (Treffer, getrimmte Eingabe, leere Liste);
`WarehousePickingController.LieferterminZelle` (keine/eine/mehrere Positionen, überfällig, ohne Termin, nur ohne Termin);
`ArticlesController.Info` (Cache leer → `OffeneBestellungenStand == null`; Rest-0-Position gezählt, nicht gelistet; Sortierung);
`WarehousePickingController.Details` (Cache leer → `Liefertermine == null`; `DUMMY` → „—“); View-Text-Test
`Details.cshtml` enthält `"key": "liefertermin"` mit `"defaultHidden": true` (Muster `PurchaseOrdersControllerTests.cs:292`).

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt (kein Epic) | – | – |

## Deploy

*Vom QA-Lauf 2026-11-26 gegen den echten Diff `1f11fa48..b3a3809f` bestätigt (siehe QA-Nachweis unten).*

- **Web-App:** ja (Controller, ViewModels, Views, Repository im Web-Projekt).
- **Service:** nein (Dienst unverändert; Repository liegt im Web-Projekt, der Dienst schreibt den Cache wie bisher).
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): keine. (Kein SQL, keine Einstellung, kein Leitstand-Schritt; die
  Datenvoraussetzung `Sync:EinkaufBestellungenEnabled` hat schon ihren Eintrag `nachlauf.psd1:470`.)
- **QA-Bestätigung aus dem Diff:** nur Dateien unter `IdealAkeWms/` (+ Tests, docs) → **deploy.web = true**; unter `IDEALAKEWMSService/`
  nur `AppVersion.cs` (Versionsgleichstand, keine Logik) → **deploy.service = false** (fachlich nicht nötig; das Bündel-Paket enthält ihn
  trotzdem, Version 1.99.85); keine Datei unter `*/Migrations/`, kein `SQL/` → **deploy.migration = false**. Manifest: kein Eintrag nötig (bestätigt).
- **Publish (erledigt im QA-Lauf):** `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` – IDEALAKEWMSWeb +
  IDEALAKEWMSWebService + SQL + deploy, `publish-info.txt` Version 1.99.85, Commit `f6d4fcb26ec5fd39216c5f6332c017f108d786b8`
  (Nach-QA 3 2026-11-26 19:48, ersetzt `fa54b418` und enthält B-2/B-4; siehe „QA-Nachtrag 3“ unten).
  Ablauf Mensch: publish aus dem Worktree → Testsystem → testen → Merge. Nach dem Merge nur dann erneut aus main publishen, wenn der
  Merge getestete Dateien mit parallelen main-Änderungen zusammengeführt hat:
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` und
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`.
  Keine Migration, kein Einmal-Skript → keine besondere Reihenfolge (Web austauschen genügt).
- **Publish-Befehle:** im Worktree `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`.
- Version in beiden `AppVersion.cs` hochzählen (nächste freie Version im Bündel, Dev-Lauf prüft), Anwender-Changelog
  `Views/Help/Changelog.cshtml`, Hilfeseite `Views/Help/Index.cshtml` (Abschnitte Artikelinfo und „Lager: Eingehende Listen“ mit der
  neuen Karte/Spalte, Hilfe-Detail-Regel), `docs/anwender/` betroffene Kapitel (Artikelinfo, Lagerbestellungen) mit Foto gegen DEV.

**Prüfschritt P-1 (Mensch, vor der Abnahme auf IDEAL, unverifiziert):** Ist der Abgleich auf IDEAL-Prod an und der Cache gefüllt?
Auf der IDEAL-WMS-Datenbank (`IDEAL_AKE_WMS`):
```sql
SELECT [Key], [Value] FROM dbo.ServiceSettings WHERE [Key] = 'Sync:EinkaufBestellungenEnabled';
SELECT COUNT(*) AS Zeilen, MAX(SyncedAt) AS Stand FROM dbo.SagePurchaseOrderLines;
```
Schalter aus / 0 Zeilen → auf IDEAL erscheinen nur die Hinweise (AK 6, 11); das ist erwartetes Verhalten, kein Fehler.

## Offene Rueckfragen

1. Die Artikelinfo (Klassenfilter `RequireMasterDataReadAccess`: admin, masterdata_read, masterdata; `codebase/controller.md:54`)
   zeigt mit der neuen Karte Beleg, Lieferant, offene Menge und Liefertermin. „Offene Bestellungen“ selbst braucht
   `RequireStockReadAccess` (admin, stock, stock_keyuser, picking, stock_read; `codebase/controller.md:64`). Ein Benutzer nur mit
   `masterdata_read` sähe damit Bestelldaten, die er in „Offene Bestellungen“ nicht öffnen darf. Lieferanten sieht er in der
   Artikelinfo schon heute (Karte „Lieferanten“); Preise werden nicht gezeigt. **Vorschlag:** in Ordnung, keine zusätzliche Rollenprüfung.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

(kein Jira-Vorgang)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Bestelldaten in der Artikelinfo auch für Benutzer nur mit masterdata_read (Vorschlag: ja, keine zusätzliche Prüfung) → **ok (Gerald 2026-11-26)**
2. Welche Positionen anzeigen? → **nur offene oder teilgelieferte (Rest > 0); im WMS vollständig erfasste (Rest 0) nicht anzeigen – wie F1.5 (Gerald 2026-11-26)**

## Umsetzungsnachweis (Dev-Lauf 2026-11-26)
- Commit Worktree: `1f11fa48` (wip feature-complete), Version 1.99.85.
- `dotnet build IdealAkeWms.slnx`: 0 Fehler (28 Warnungen, keine neue in den geänderten Dateien).
- `dotnet test`: IdealAkeWms.Tests **Bestanden** 3782 erfolgreich, 1 übersprungen, 0 Fehler; IDEALAKEWMSService.Tests **Bestanden** 848/848.
- Neue Tests `LieferterminOffeneBestellungenTests` (7). Bestandstest `ViewKeyDriftGuardTests.FraesenPfad_DreiQuellenGleich_StandardAus`
  verlangte die Spalte auch im Katalog → `ColumnDefinitions.WarehousePickingDetails` um `liefertermin` (DefaultHidden) ergänzt,
  Druck nimmt sie aus (`WarehousePickingPrintLayout.ResolveColumns`, wie Fräsen-Pfad).
- Befund Druck-Filter (Liefertermin/Fräsen-Pfad gegen leeren Text) auf Geralds Zuruf behoben: Print filtert mit demselben Zelltext
  (gemeinsamer Helfer `LieferterminTexteAsync`), Test `Print_SpaltenfilterLiefertermin_FiltertWieDerBildschirm_NichtGegenLeerenText`;
  Web-Tests danach 3783 ok/1 übersprungen.

## QA-Nachweis (qa-agent, 2026-11-26)

Prüfbereich: Commits `1f11fa48..b3a3809f` (Umsetzung + Druck-Filter-Fix) im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`,
Branch `feature/2026-08-07-ideal-teile-1-5`. Build und Tests über den ganzen Worktree.

### 1. Build und Tests (HEAD `b3a3809f`)
- `dotnet build IdealAkeWms.slnx`: **0 Fehler**, 33 Warnungen (NuGet-Advisories, CS8321/CS8604 in nicht berührten Dateien).
- `dotnet test --no-build`: IdealAkeWms.Tests **Bestanden** 3783 erfolgreich / 1 übersprungen / 0 Fehler (3784);
  IDEALAKEWMSService.Tests **Bestanden** 848 / 0 / 0.

### 2. Code-Review (selbst, in diesem Lauf)
Geprüft: kompletter Diff beider Commits; dazu `ErfasstJeBelPos` (`PurchaseOrderReceiptRepository.cs:12-30`), `PurchaseOrdersController`
(Sortierung `:168`, Überfällig `:230`, `LieferterminText` `:84`), `WarehousePickingPrintLayout` (ResolveColumns, CellText, ColumnMap, SortItems),
`ColumnFilterHelper.Apply`, `table-filter.js` (Datums-Sortierung `:345-357`), Details-Formular (parallele Arrays).

Bewusst geprüfte Defektklassen:
- Werte in `<script>`: `column-config` bekommt nur einen festen `@:`-Literalblock, keine Daten; `title`/`data-filter-text` laufen über Razor-Encoding. OK.
- `hidden` + `d-*`: nicht vorhanden. OK.
- Stille Fallbacks: `?? "—"` (kein Cache-Eintrag = keine offene Bestellung, fachlich gewollt F2.3), `ErfasstJeBelPos.Leer` (bestehendes Muster). OK.
- Positionsbindung paralleler Arrays: die neue Zelle enthält kein Eingabefeld; `quantitiesPicked`/`itemIds` unverändert. OK.
- Nur-Client-Validierung: keine (Leseanzeige). OK.
- Eine Regel an mehreren Orten: Rest-Regel nur über `ErfasstJeBelPos.Rest` (Info, Details, Druck-Filter über den gemeinsamen Helfer
  `LieferterminTexteAsync`, `WarehousePickingController.cs:272`). „Überfällig“ steht dreimal: `PurchaseOrdersController.cs:230`,
  `Info.cshtml:238`, `WarehousePickingController.cs:293` – alle `Termin < heute` (einmal mit `.Date`); `Liefertermin` ist ein Datum, kein Unterschied. Hingenommen.

Befunde:
- **B-1 (behoben vor QA-Ende, Commit `b3a3809f`):** Mit dem Katalog-Eintrag `liefertermin` landete der Key in `WarehousePickingPrintLayout.ColumnMap`,
  `CellText` lieferte dafür `""` → ein Bildschirm-Spaltenfilter auf „Liefertermin“ hätte den Druck leer gefiltert. Fix: Print setzt für
  `liefertermin`/`fraesen-pfad` den Bildschirm-Zelltext ein (`WarehousePickingController.cs:605-611`). Auf DEV verifiziert (siehe 6).
- **B-2 (niedrig, offen, nicht gebaut):** Sortiert der Benutzer am Bildschirm nach „Liefertermin“ (oder setzt sie als Standard-Sortierung) und
  druckt, sortiert `WarehousePickingPrintLayout.SortItems` über `CellText` → `""` für alle Zeilen → Druck in Pos-Reihenfolge statt nach Termin
  (`WarehousePickingPrintLayout.cs:126-130`; gleiche Lage beim Fräsen-Pfad). Kein Datenverlust; der Druck zeigt die Spalte ohnehin nicht (Out-of-Scope
  „Druck unverändert“). Entscheidung Mensch, ob nachgezogen wird.
- **B-3 (Checkliste, Brain):** Brain-Changelog `secondbrain/changelog/2026-11-26-v1-99-85-*.md` und Eintrag in `secondbrain/feature-map.md`
  fehlen noch (v1.99.84 hat beides). Kein Code-Befund; nachzuziehen vom Umsetzungslauf.
- Nebenbei: `Info.cshtml` Zeile 1 hat das UTF-8-BOM verloren – harmlos (Datei bleibt UTF-8, DEV zeigt Umlaute korrekt).

### 3. Beweisart je AK
| AK | Beweis |
|---|---|
| 1 Karte, Spalten, Sortierung | Test `Info_RestRegel_…_AK1_AK4` (Sortierung, InMemory) + DEV AKE OK (EK015744, EK000376) |
| 2 „überfällig“ | DEV AKE OK; View-Text-Test. Darstellung = Manual-UAT |
| 3 Rahmenabruf / Einheit abweichend | DEV AKE OK (EK015744 „Rahmenabruf“, EK000376 „Einheit abweichend“) – Manual-UAT, kein Test |
| 4 Rest-Regel, Rest 0 in Fußzeile | Test (Mock-Erfasst) + DEV AKE OK mit echten WE-Daten (EK009055: 30 − 9,5 = 20,5 Stk; EK015744: 1 Position Rest 0 → Fußzeile) |
| 5 Hinweis ohne Bestellung | DEV AKE OK (EK000015); View-Text-Test |
| 6 Cache leer, Hinweis | Test `Info_CacheLeer_…_AK6` + DEV IDEAL OK (Cache temporär geleert, danach 1059 Zeilen zurück) |
| 7 Stand im Kopf | DEV AKE OK („Stand: 17.11.2026 11:51“ = `MAX(SyncedAt)`) |
| 8 Spalte Standard aus, Zahnrad, Persistenz | View-Text-Test + Drift-Wächter; DEV AKE OK (Browser) – JS = Manual-UAT |
| 9 Zelltext | Tests `LieferterminZelle_Faelle_AK9`, `Details_…_AK9_AK10`; DEV AKE OK für „(+4)“, „(+8)“, „überfällig“, „—“ (ohne Bestellung, DUMMY). „ohne Termin“ nur per Test |
| 10 Link nach Rolle | Test (Flag `ArtikelinfoLink`); DEV: Link `href=/Articles/Info?articleNumber=…`, `target=_blank` (admin). Ohne masterdata_read: **Manual-UAT** |
| 11 Cache leer → keine Spalte | Test `Details_CacheLeer_KeineSpalte_AK11` + DEV IDEAL OK (keine Spalte, kein Zahnrad-Eintrag; Glasbestellung #13) |
| 12 feste Zahl Abfragen | Code-Review (3 Abfragen außerhalb der Schleife, `WarehousePickingController.cs:265,272-284`); kein Test (InMemory zählt keine SQL) |
| 13 Regression Details | Bestandstests grün; DEV: Druck mit/ohne Filter OK. Ist-Mengen/Abschließen nicht ausgeführt → **Manual-UAT** |
| 14 build/test grün | erfüllt (oben) |

Groß/Klein-Abgleich der Artikelnummer in SQL (CI-Kollation) ist nicht InMemory-beweisbar → Manual-UAT (auf DEV gleich geschrieben, OK).

### 4. Geänderte Bestandstests
- `ArticlesControllerTests`, `FraesenPfadTests`, `PurchaseProposalsNachbesserungE4Tests`, `WarehousePickingControllerTests` (3×),
  `WarehousePickingVerbrauchsbuchungTests`: **nur Konstruktor** (zwei neue Repos) – Compile-Fix.
- `ArtikelinfoLieferantenTests.Build`: `private` → `internal`, optionale Parameter `poLines`/`receipts` (Default Mock) – Compile-Fix +
  Wiederverwendung durch die neuen Tests; Verhalten der Bestandstests unverändert.
- Keine Fixture-Anpassung „bis grün“. `ViewKeyDriftGuardTests` unverändert; er erzwang den Katalog-Eintrag (beabsichtigt).

### 5. Checkliste / 5a
- Migration/SQL/FreshInstall/AgentJobs: nicht betroffen (bestätigt). Audit-Felder: nicht betroffen (nur lesen).
- Version 1.99.85 in beiden `AppVersion.cs`, Anwender-Changelog und Hilfe (Artikelinfo + Lager-Details) ergänzt.
- `docs/TESTSZENARIEN.md` TS-206 vorhanden; Index `tests/testszenarien-index.md:179` vorhanden. `docs/anwender` Kap. 03 + README ergänzt.
- Brain-Changelog + feature-map: **fehlen** (B-3).
- ADR 0020: keine neue/geänderte Einstellung (bestätigt) – entfällt.
- `nachlauf.psd1`: kein Deploy-Schritt nötig (bestätigt).

### 6. DEV-Deploy und Testlauf
- Standort laut Frontmatter `beide`. Shell elevated.
- `dev-deploy.ps1 -Standort AKE -Quelle <worktree>`: Exit 0, „HTTP 200, Fusszeile v1.99.85“, „Fertig: AKE v1.99.85 auf http://localhost:8081/“.
- `dev-deploy.ps1 -Standort IDEAL -Quelle <worktree>`: Exit 0, „HTTP 200, Fusszeile v1.99.85“, „Fertig: IDEAL v1.99.85 auf http://localhost:8082/“.
- Ausgangslage DEV: `Sync:EinkaufBestellungenEnabled = false` auf beiden (nicht angefasst); Cache AKE 1160 Zeilen (Stand 17.11.2026 11:51), IDEAL 1059 (07.11.2026).
- Testdaten: AKE_WMS_DEV Lagerbestellung **#73** (Kommentar „QA TS-206 Liefertermin“, Abgeschickt, Positionen EK015744, EK010938,
  EK009055, EK000015, DUMMY) angelegt, bleibt für den Menschen stehen. Spaltenpräferenz `WarehousePickingDetails` (Liefertermin an) für den admin-Benutzer auf AKE-DEV gespeichert.

| Schritt | Ergebnis | Status |
|---|---|---|
| TS-206.1 Artikelinfo EK015744 (AKE) | Karte nach „Lieferanten“, 5 Zeilen nach Termin aufsteigend, „Calorflex SRL (K30249)“, Stk, Kopf „Stand: 17.11.2026 11:51“ | OK |
| TS-206.2 Überfällig / Rahmenabruf / Einheit abweichend | EK015744 „Rahmenabruf“ + „überfällig“; EK000376 3× „Einheit abweichend“ | OK |
| TS-206.3 Rest-Regel | EK009055 Offen 20,5 Stk (Sage 30, WMS 9,5); EK015744 Fußzeile „1 Position(en) im WMS bereits vollständig erfasst“ (BelPosId 359499, 33/33). Eigene WE-Erfassung nicht ausgeführt (vorhandene Daten genügten) | OK |
| TS-206.4 Ohne Bestellung | EK000015 „Keine offene Bestellung für diesen Artikel.“ | OK |
| TS-206.5 Cache leer (IDEAL) | Cache gesichert + geleert → Artikelinfo 50002070 Hinweis mit `Sync:EinkaufBestellungenEnabled`; Glasbestellung #13 ohne Spalte, Zahnrad ohne „Liefertermin“; Cache zurückgespielt (1059 Zeilen, Stand gleich), Sicherungstabelle gelöscht | OK |
| TS-206.6 Details Spalte (AKE #73) | Erstaufruf ohne Spalte; Zahnrad → einblenden; Neuladen → bleibt. Zellen „22.08.2025 überfällig (+4)“, „12.06.2026 überfällig (+8)“, „16.06.2026 überfällig“, „—“ (EK000015), „—“ (DUMMY) – deckt sich mit SQL | OK |
| TS-206.6 „ohne Termin“ | keine Cache-Position ohne Termin auf DEV | NICHT MOEGLICH (nur Unit-Test) |
| TS-206.7 Link Benutzer A (admin) | Link `/Articles/Info?articleNumber=EK010938`, `target="_blank"` | OK |
| TS-206.7 Ist-Menge bleibt / Benutzer B nur stock | DEV-Browser nur als admin angemeldet | NICHT MOEGLICH |
| TS-206.8 Regression | Druck ohne Filter unverändert (Spalte nicht im Druck); Ist-Mengen speichern, Fehlteil, Drucken und Abschließen, Speichern + Abschließen nicht ausgeführt | Druck OK / Rest NICHT MOEGLICH (offen für den Menschen) |
| Negativ: Spaltenfilter | Bildschirm „(+8)“ → nur Pos 2; Druck `?colf_liefertermin=überfällig` → Pos 1–3, `?colf_liefertermin=(+8)` → nur Pos 2; Spalte nicht im Druck | OK |
| Negativ: Glasbestellung | IDEAL #13 (Glas) bei leerem Cache ohne Spalte; Glas mit gefülltem Cache nicht eigens geprüft (dieselbe View) | OK (teilweise) |

### 7. Publish
`C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` neu befüllt (Zielordner vorher geleert): IDEALAKEWMSWeb (Exit 0),
IDEALAKEWMSWebService (Exit 0), `SQL\`, `deploy\`; `publish-info.txt`: Version 1.99.85, Commit `b3a3809fd0df1a64dbb12660f8e4505cc821c30a`,
Branch `feature/2026-08-07-ideal-teile-1-5`, Erstellt 2026-11-26 19:07, Komponenten IDEALAKEWMSWeb + IDEALAKEWMSWebService.

Ergebnis: **Testbereit** (B-1 behoben; B-2 niedrig, offen zur Entscheidung; B-3 Brain-Nachzug offen).

### Manuelle Test-Checkliste (Mensch)
1. AKE-DEV, Benutzer **nur stock** (ohne masterdata_read): Lagerbestellung #73 → Spalte „Liefertermin“ als Text, **kein** Link (AK 10).
2. AKE-DEV #73 als admin: Ist-Menge eintragen (nicht speichern), Termin-Link klicken → Artikelinfo im neuen Tab; Ursprungs-Tab behält die Ist-Menge (TS-206.7).
3. AKE-DEV #73: Ist-Mengen speichern, Fehlteil setzen, „Drucken“, „Drucken und Abschließen“ bzw. „Speichern + Abschließen“ wie bisher (TS-206.8, AK 13).
4. B-2: nach „Liefertermin“ sortieren und drucken → Druck in Pos-Reihenfolge; entscheiden, ob das so bleiben darf.
5. „ohne Termin“ (nur Unit-Test): auf dem Testsystem `SELECT TOP 5 Artikelnummer FROM dbo.SagePurchaseOrderLines WHERE Liefertermin IS NULL`
   → Karte und Zelle prüfen.
6. Prüfschritt P-1 (IDEAL-Prod: Schalter/Cache) vor der Abnahme auf IDEAL.
7. Testsystem-Lauf mit dem Paket aus `publish\` (fachlich genügt Web), TS-206.1–206.8 dort einmal durchgehen; dann Merge (Schranke 2).


## QA-Nachtrag (qa-agent, 2026-11-26, Commit `fa54b418`)

Anlass: Doku-Foto AKE-DEV, Artikelinfo EK010938 – Karte „Offene Bestellungen“ rollte quer, „überfällig“ abgeschnitten
(globale Regel `.table-responsive td { white-space: nowrap !important }`, `site.css:238`). Geprüfter Bereich: nur `b3a3809f..fa54b418`
(2 Dateien, 3 Zeilen); Build/Tests über den ganzen Worktree.

**1. Build und Tests (HEAD `fa54b418`):** `dotnet build IdealAkeWms.slnx` 0 Fehler (33 Warnungen, Bestand). `dotnet test`:
IdealAkeWms.Tests 3783 bestanden / 1 übersprungen / 0 Fehler; IDEALAKEWMSService.Tests 848 / 0 / 0.

**2. Code-Review des Deltas:** `Info.cshtml:238-241` – `<span> überfällig</span>` → `<div class="small text-danger fw-semibold">überfällig</div>`
unter dem Termin (Muster „Einheit abweichend“). Bedingung `line.Liefertermin < heuteOb` unverändert, keine Daten in `<script>`, kein `hidden`/`d-*`,
kein Fallback, keine Eingabe. `text-danger` in Tabellen wird global auf `--bs-danger-text-emphasis` gesetzt (`site.css:1521`). UTF-8-BOM in Zeile 1
wieder da (Kleinbefund der ersten QA damit erledigt). **Keine Befunde.**

**4. Geänderte Bestandstests:** `LieferterminOffeneBestellungenTests.cs:196` – Marker `" überfällig"` → `">überfällig</div>"`:
Verhaltensänderung durch diesen Fix (Darstellung), kein Fixture-Biegen. OK.

**6. DEV-Deploy:** Shell erhöht. `dev-deploy.ps1 -Standort AKE` Exit 0, „HTTP 200, Fusszeile v1.99.85“; `-Standort IDEAL` Exit 0, „HTTP 200,
Fusszeile v1.99.85“. Quelle: Bündel-Worktree, Web + Dienst.

| Schritt | Ergebnis | Status |
|---|---|---|
| AKE-DEV `/Articles/Info?articleNumber=EK010938`, Karte „Offene Bestellungen“ (9 Positionen, 6 überfällig) | Tabelle ohne horizontale Bildlaufleiste, Spalten Beleg/Lieferant/Offen/Liefertermin vollständig; „überfällig“ ganz lesbar als zweite Zeile unter „TT.MM.JJJJ KWnn“ | OK |
| Breite 1440 px | Browserfenster ließ sich nicht umstellen, gemessen bei Viewport 1523 px (gleicher Bootstrap-Breakpoint xxl ≥1400). Karte ≈727 px breit, Tabelleninhalt endet ≈60 px vor dem Kartenrand; bei 1440 px wird die Karte ≈40 px schmaler → passt rechnerisch, nicht direkt gemessen | OK (rechnerisch) – Mensch bestätigt bei 1440 px |
| Kontrast | Farbe in Tabellen `#58151c` (`--bs-danger-text-emphasis`). Auf Streifen `#F2F2F2` ≈ 12:1, auf Weiß ≈ 13:1, auf Hover `#A1D9F7` ≈ 9:1 → WCAG AA (4,5:1) erfüllt; Bedeutung zusätzlich als Wort, nicht nur Farbe | OK |
| Lieferanten-Karte darüber | rollt weiterhin quer (Spalte „Wiederbeschaffung…“), Bestand, nicht Teil dieser Spec | Hinweis, kein Befund |

**7. Publish:** `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` – IDEALAKEWMSWeb, IDEALAKEWMSWebService, `SQL\`, `deploy\`
vorher gelöscht und neu erzeugt (beide `dotnet publish` Exit 0). `publish-info.txt`: Version 1.99.85, Commit `fa54b4189c38144a3239a2ca8ac8cd9b35527a9c`,
Branch `feature/2026-08-07-ideal-teile-1-5`, Erstellt 2026-11-26 19:20, Komponenten IDEALAKEWMSWeb + IDEALAKEWMSWebService. Publizierte `IdealAkeWms.dll`
enthält das neue Markup `überfällig</div>` (Stichprobe). Hinweis: `publish\paket24-v1.99.85.zip` (19:10) stammt noch vom Stand `b3a3809f` und
ist **veraltet**; ZIP/Shares macht der Mensch neu.

Ergebnis: **Testbereit bleibt.** Deploy-Abschnitt unverändert gültig (nur Web fachlich nötig, keine Migration), Commit dort nachgezogen.

**Ergänzung zur manuellen Checkliste:** 8. Artikelinfo EK010938 bei genau 1440 px (und am Werkstatt-Terminal) ansehen: Karte „Offene Bestellungen“
ohne Querrollen, „überfällig“ unter dem Termin.

## Nachtrag B-2 (2026-11-26, Gerald „Ja“ im Chat)
- B-2 behoben in `5fb5ca8a`: `WarehousePickingPrintLayout.SortItems` nimmt die im Print ergänzte Spaltenzuordnung (Liefertermin,
  Fräsen-Pfad), geladen nur wenn Filter oder (Default-)Sortierung sie braucht. Test `Print_SortierungNachLiefertermin_WirktImDruck_B2`.
  Web-Tests 3784 ok/1 übersprungen. Nach-QA durch qa-agent folgt.
- Gerald: „IDEAL lesen wir dann auch“ → am Standort IDEAL soll `Sync:EinkaufBestellungenEnabled` eingeschaltet werden (Betriebsschritt,
  P-1); bis dahin zeigt IDEAL den Hinweis statt der Daten.


## QA-Nachtrag 2 (qa-agent, 2026-11-26, Commits `fa54b418..5fb5ca8a`) – NICHT bestanden

Geprüft: `a6e71944` (nur Doku/Fotos, keine Befunde) und `5fb5ca8a` (Fix B-2). Dazu gelesen: `WarehousePickingPrintLayout.cs` komplett
(CellText, ColumnMap, SortItems, CompareLikeGui), `WarehousePickingController.Print` + `LieferterminTexteAsync` (`:272-296`, `:600-631`),
`table-filter.js` `compareValues`/`sortTable` (`:347-422`), `Details.cshtml` (`buildPrintUrl` `:391-408`, Spaltenkopf `:77-88`).

**1. Build und Tests (HEAD `5fb5ca8a`):** `dotnet build IdealAkeWms.slnx` 0 Fehler (33 Warnungen, Bestand). `dotnet test`:
IdealAkeWms.Tests 3784 bestanden / 1 übersprungen / 0 Fehler; IDEALAKEWMSService.Tests 848 / 0 / 0.

**2. Code-Review des Deltas:**
- Default-Map: `SortItems` ohne `colMap` nimmt `ColumnMap`; `text(a) ?? ""` ist für die Katalog-Keys identisch zu `CellText(a, col)` (liefert nie null),
  unbekannter Key fällt wie bisher auf `pos asc`. Bestehende `WarehousePickingPrintLayoutTests` unverändert und grün. OK.
- Leerer Cache: `LieferterminTexteAsync` → `(null, null)`, `colMap["liefertermin"]` bleibt der Katalog-Eintrag (`CellText` → `""`), alle gleich, stabile
  Sortierung → Pos-Reihenfolge in beide Richtungen. Bildschirm: Spalte fehlt (`Details.cshtml:77`), `sortTable` bricht mit Warnung ab → ebenfalls Pos-Reihenfolge. Konsistent. Gleich beim Fräsen-Pfad bei aus geschaltetem Schalter. OK.
- `Braucht`: Sortierschlüssel wie in `SortItems` (live vor Default), Ordinal wie das Dictionary. OK.
- **B-4 (FEHLER, offen): Druck sortiert Liefertermin als Text, der Bildschirm als Datum.** `CompareLikeGui` (`WarehousePickingPrintLayout.cs:142-147`)
  repliziert `compareValues` nur zum Teil: der Datumszweig (`table-filter.js:348-359`, Präfix `dd.MM.yyyy` chronologisch, Nicht-Datum asc ans Ende) fehlt.
  Bis v1.99.84 hatte keine Druck-Spalte ein Datum, deshalb fiel das nicht auf; mit `liefertermin` greift es. Zelltexte wie `22.08.2025 überfällig (+4)`
  parsen nicht als Zahl → de-Stringvergleich ab dem Tag („12.06.2026“ < „16.06.2026“ < „22.08.2025“), „—“ landet vorn. Auf DEV belegt (Schritt 6).
  Vorschlag Fix (eine Stelle): in `CompareLikeGui` vor dem Zahlenzweig den Datumszweig wie in JS nachbilden (`^(\d{2})\.(\d{2})\.(\d{4})`, beide Datum →
  `DateTime`-Vergleich; nur eines Datum → Datum zuerst, `OrderByDescending` dreht das wie JS um). Test-Lücke: `Print_SortierungNachLiefertermin_WirktImDruck_B2`
  prüft nur „asc ≠ desc“ und erkennt die falsche Reihenfolge nicht; er sollte die erwartete Reihenfolge mit mindestens zwei Terminen über Monats-/Jahresgrenze
  und einer „—“-Zeile prüfen.
- Klein (kein Befund): beide geänderten `.cs`-Dateien haben in Zeile 1 ein UTF-8-BOM bekommen – harmlos.

**3. Beweisart:** Druck-Sortierung nach Liefertermin = Manual-UAT (Bildschirm-Reihenfolge entsteht in Browser-JS); der Unit-Test beweist nur, dass überhaupt sortiert wird.

**4. Geänderte Bestandstests:** keine (nur ein neuer Test).

**6. DEV-Deploy:** Shell erhöht. `dev-deploy.ps1 -Standort AKE` Exit 0, „HTTP 200, Fusszeile v1.99.85“; `-Standort IDEAL` Exit 0, „HTTP 200, Fusszeile v1.99.85“.
Quelle: Bündel-Worktree HEAD `5fb5ca8a`, Web + Dienst.

| Schritt | Ergebnis | Status |
|---|---|---|
| AKE-DEV `/WarehousePicking/Details/73`, Liefertermin-Zellen | Pos1 `22.08.2025 überfällig (+4)`, Pos2 `12.06.2026 überfällig (+8)`, Pos3 `16.06.2026 überfällig`, Pos4/5 `—` | OK |
| Bildschirm sortiert Liefertermin auf | 1, 2, 3, 4, 5 (chronologisch, „—“ am Ende) | OK |
| Drucken (URL `Print/73?sortCol=liefertermin&sortDir=asc`) | 4, 5, 2, 3, 1 | **FEHLER** (B-4) |
| Bildschirm sortiert Liefertermin ab | 4, 5, 3, 2, 1 | OK |
| Drucken (`sortDir=desc`) | 1, 3, 2, 4, 5 | **FEHLER** (B-4) |

**7. Publish:** nicht neu erzeugt (QA nicht grün). `publish\publish-info.txt` steht unverändert auf Version 1.99.85, Commit
`fa54b4189c38144a3239a2ca8ac8cd9b35527a9c`, Erstellt 2026-11-26 19:20 – also ohne den B-2-Fix. Neu publishen nach behobenem B-4 und erneuter Nach-QA.

Ergebnis: **status zurück auf InUmsetzung.** Erster Fehler: Druck `sortDir=asc` liefert 4,5,2,3,1 statt 1,2,3,4,5. Vermutete Ursache: fehlender Datumszweig in
`CompareLikeGui`. Fehlversuch 1 von 2 für B-2. Deploy-Abschnitt bleibt gültig (nur Web, keine Migration).

## QA-Nachtrag 3 (qa-agent, 2026-11-26, Commits `5fb5ca8a..f6d4fcb2`) – bestanden

Geprüft: `f6d4fcb2` (Fix B-4, 2 Dateien). Dazu gelesen: `WarehousePickingPrintLayout.cs` `SortItems`/`CompareLikeGui`/`LeadingDate`/`TryNum`
(`:102-163`), `CellText` (`:69-89`), einziger Aufrufer `WarehousePickingController.Print` (`:607-617`, Map inkl. `liefertermin`/`fraesen-pfad`),
`table-filter.js` `compareValues` (`:347-371`).

**1. Build und Tests (HEAD `f6d4fcb2`):** `dotnet build IdealAkeWms.slnx` 0 Fehler (33 Warnungen, Bestand). `dotnet test`:
IdealAkeWms.Tests 3785 bestanden / 1 übersprungen / 0 Fehler; IDEALAKEWMSService.Tests 848 / 0 / 0.

**2. Code-Review des Deltas:**
- Parität zu JS: beide Datum → chronologisch; nur eines Datum → Datum vorn (asc), `OrderByDescending` dreht wie JS `dir==='desc' ? 1 : -1`;
  danach Zahl, dann de-String unverändert. Gleichstände: `OrderBy`/`OrderByDescending` stabil, JS `Array.sort` stabil → gleiche Pos-Folge. OK.
- Andere Druckspalten mit möglichem Datumsanfang: `note-lager`, `note-ek` (Freitext, z. B. „01.12.2026 bestellt“) sortieren jetzt wie der Bildschirm
  chronologisch – vorher abweichend, also eine behobene Abweichung. `pos`/`requested`/`picked` sind Ganzzahlen (≤ 10 Zeichen ohne Punkte an Stelle 3/6),
  `unit`/`shortage`/`description` ohne Datumsmuster. `article-number`/`storage`: DEV-Abfrage auf beiden DEV-DBs, 0 Artikelnummern/Lagerplätze/Positionen
  mit Muster `NN.NN.NNNN%` (`Articles`, `StorageLocations.Code`, `WarehouseRequisitionItems.ArticleNumber/Note`).
- Restabweichung (Hinweis, kein Befund): JS erkennt jedes `^\d{2}\.\d{2}\.\d{4}` als Datum, auch ungültige wie „31.02.2026“ oder „10.20.3040“
  (`new Date` rollt über); C# `TryParseExact` nur gültige Daten. Betrifft nur Zelltexte mit ungültigem Datumsanfang – in den DEV-Daten nicht vorhanden.
- Kein `<script>`, kein `hidden`/`d-*`, kein stiller Fallback neu, keine Eingabe, keine Einstellung.
- **B-5 (klein, Doku, offen für den Umsetzer):** `docs/TESTSZENARIEN.md` TS-206 hat keinen Schritt für „Druck sortiert nach Liefertermin wie der
  Bildschirm“ (B-2/B-4); TS-206.8 nennt nur „Drucken (ohne Spalte Liefertermin)“. Testszenarien-Pflicht für Bugfixes → Schritt TS-206.9 ergänzen
  (Bildschirm nach Liefertermin auf-/absteigend, Drucken, gleiche Reihenfolge, „—“ asc am Ende). Blockiert Testbereit nicht; steht als Punkt 9 in der Checkliste.

**3. Beweisart:** Vergleichsregel C# automatisiert (`CompareLikeGui_DatumChronologisch_OhneDatumAmEnde_WieTableFilterJs_B4`,
`Print_SortierungNachLiefertermin_ReihenfolgeWieBildschirm_B2`). Gleichheit mit der Bildschirm-Reihenfolge (Browser-JS) = Manual-UAT, auf DEV belegt (Schritt 6).

**4. Geänderte Tests:** `Print_SortierungNachLiefertermin_WirktImDruck_B2` (aus `5fb5ca8a`, im selben Zweig, nicht in main) ersetzt durch
`…_ReihenfolgeWieBildschirm_B2` mit exakter Reihenfolge – verschärft nach Befund der QA 2, kein Fixture-Biegen. Bestehende `WarehousePickingPrintLayoutTests` unverändert grün.

**6. DEV-Deploy:** Shell erhöht. `dev-deploy.ps1 -Standort AKE` Exit 0, „HTTP 200, Fusszeile v1.99.85“; `-Standort IDEAL` Exit 0, „HTTP 200, Fusszeile v1.99.85“.
Quelle: Bündel-Worktree HEAD `f6d4fcb2`, Web + Dienst.

| Schritt | Ergebnis | Status |
|---|---|---|
| AKE-DEV `/WarehousePicking/Details/73`, Bildschirm Liefertermin aufsteigend | 1, 2, 3, 4, 5 (22.08.2025, 12.06.2026, 16.06.2026, —, —) | OK |
| Druck `/WarehousePicking/Print/73?sortCol=liefertermin&sortDir=asc` (nur geöffnet, kein Druckdialog) | 1, 2, 3, 4, 5 | OK (vorher 4,5,2,3,1) |
| Bildschirm Liefertermin absteigend | 4, 5, 3, 2, 1 | OK |
| Druck `sortDir=desc` | 4, 5, 3, 2, 1 | OK (vorher 1,3,2,4,5) |

**7. Publish:** `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` – IDEALAKEWMSWeb, IDEALAKEWMSWebService, `SQL\`, `deploy\`
vorher gelöscht und neu erzeugt (beide `dotnet publish` Exit 0). `publish-info.txt`: Version 1.99.85, Commit `f6d4fcb26ec5fd39216c5f6332c017f108d786b8`,
Branch `feature/2026-08-07-ideal-teile-1-5`, Spec dieser Slug, Erstellt 2026-11-26 19:48, Komponenten IDEALAKEWMSWeb + IDEALAKEWMSWebService.
Die ZIPs `paket24-v1.99.85.zip` (19:10) und `paket25-v1.99.85.zip` (19:22) im Ordner sind **älter** als dieser Stand (ohne B-2/B-4) – neues ZIP/Shares macht der Mensch.

Ergebnis: **status Testbereit.** Deploy-Abschnitt gültig (fachlich nur Web, keine Migration, kein Manifest-Eintrag), Commit dort nachgezogen.

**Ergänzung zur manuellen Checkliste:** 9. Lagerbestellung mit Liefertermin-Spalte: nach Liefertermin auf- und absteigend sortieren, jeweils „Drucken“ →
gleiche Reihenfolge wie am Bildschirm, „—“/„ohne Termin“ aufsteigend am Ende (B-2/B-4; Szenario TS-206.9 nachtragen, B-5).
