---
type: spec
title: Liefertermin offener Bestellungen in Artikelinfo und Lagerbestellung-Details
slug: 2026-11-26-liefertermin-offene-bestellungen-artikelinfo-lagerbestellung-spec
status: Entwurf
created: 2026-11-26
updated: 2026-11-26
source_backlog: "[[2026-11-26-liefertermin-offene-bestellungen-artikelinfo-lagerbestellung]]"
task: ""
jira: ""
worktree: ""
branch: ""
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
open_questions:
  - "Artikelinfo zeigt Beleg, Lieferant, offene Menge und Liefertermin auch Benutzern, die nur masterdata_read haben und „Offene Bestellungen“ (stock_read) nicht oeffnen duerfen. In Ordnung? Vorschlag: ja, keine zusaetzliche Rollenpruefung."
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
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

*Vorläufig – der QA-Lauf bestätigt gegen den echten Diff.*

- **Web-App:** ja (Controller, ViewModels, Views, Repository im Web-Projekt).
- **Service:** nein (Dienst unverändert; Repository liegt im Web-Projekt, der Dienst schreibt den Cache wie bisher).
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): keine. (Kein SQL, keine Einstellung, kein Leitstand-Schritt; die
  Datenvoraussetzung `Sync:EinkaufBestellungenEnabled` hat schon ihren Eintrag `nachlauf.psd1:470`.)
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

1. Bestelldaten in der Artikelinfo auch für Benutzer nur mit masterdata_read (Vorschlag: ja, keine zusätzliche Prüfung) →
