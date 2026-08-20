---
type: aufgabe
title: "FA-Liste und verwandte Ansichten hierarchiefaehig darstellen (Epic, Nachtrag Teil 7/8)"
status: InUmsetzung
spec: "[[2026-08-18-fa-liste-hierarchie-anzeige-spec]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
created: 2026-08-20
updated: 2026-08-20
---

# FA-Liste hierarchiefaehig (Epic)

Umsetzung der freigegebenen Spec [[2026-08-18-fa-liste-hierarchie-anzeige-spec]]. Am 2026-08-20 (Mensch)
zu einem **Epic** umstrukturiert (Umfang: 6 Views + Gruppen-Pagination + JS-Modul + Kaskade + Z4-Sweep +
Z1/Z3-Sync). Buendel-Worktree, kein Zwischen-Merge, QA erst Etappe E. Gates geprueft: Freigabe-Antworten
1–8 + Z1–Z5 beantwortet, BLOCKER BL1 in „ANTWORTEN auf die Kritische Pruefung (2026-08-20)" ausgeraeumt
(Kaskade-Wirt = PickingLeitstand-Gruppenkopfzeile).

## Etappen
| # | Etappe | Status |
|---|--------|--------|
| A | Anzeige-Fundament + ProductionOrders-Referenz-View + Z4-ERHEBUNG. **STOPP nach A.** | **erledigt** (`30b7a8c`/`9315186`/`abd2620`) |
| B | 5 weitere Views (Anzeige-Teil) | offen |
| C | Kaskade Leitstand-Kopfzeile | offen |
| D | Z4-Sweep + Z3-Sync-Meldung + Z1-Regressionstest | offen |
| E | Testszenarien + Brain + qa-agent → Testbereit | offen |

## Verbindliche Entscheidungen (aus der Spec)
- **Wirksam nur bei Master `ProduktionsauftragHierarchisch == true`** (Lese-Muster wie
  `StandortEinstellungenController`: `IServiceSettingRepository.GetValueAsync(HierarchischeStrukturKeys.Master)`
  + IsTrue). Bei Master `false` **bit-identisch** zu heute (AKE-Regression, AK 10).
- **ALLE Nachfahren** je Gruppe als flache Zeilen (nicht nur direkte Kinder). Gruppen-Kopfzeile =
  `OrderNumber`, Zeilen-`order-number` = `SubOrderNumber`. Muster = `tbody`-je-HauptFA +
  colspan-Kopfzeile (wie `FaHierarchyKommissionierListen`) **plus** Chevron-Collapse (neu, aus OSEON).
- Neue Spalte `parent-sub-order-number` (`defaultHidden`), 6 viewKeys, additiv in ColumnDefinitions,
  keine Migration.
- Suche: erst `SubOrderNumber`, dann `OrderNumber`; **Auto-Expand bei Treffer in zugeklappter Gruppe**
  (F2, gefaehrlichster stiller Fehler) im neuen JS-Modul, KEIN Eingriff in table-filter.js.
- Pagination ueber **Gruppen** (TotalCount = Anzahl OrderNumber-Gruppen), Gruppe nie ueber Seiten getrennt.
- `SageMissingSince`-Badge: eigene Bedingung/Text, gleiche Render-Position wie IsCancelled-Badge,
  KEIN Feld-Mapping (Z5).
- Z2: Sortierung je Gruppe `SubOrderNumber` aufsteigend (Vorbelegung).
- Z4: „N Auftraege · M Sub-FAs" ueberall wo Auftraege gezaehlt werden; im Flachmodus eine Zahl.
- Kaskade (Etappe C): nur Leitstand-Gruppenkopfzeile, `IsDoneBde=true` alle Nachfahren, atomar,
  Dialog mit Offene-Buchung-Zahl (jede nicht beendete/nicht stornierte Buchung), ILogger; keine
  Gruppen-Ruecknahme; Zeilen-Toggle unveraendert. Kaskade schreibt an `ProductionOrderBdeStatus`.
- Z1: Sync fasst WMS-Zustaende nicht an (bereits erfuellt, nur Regressionstest in D).
- BDE-Cockpit ausgeklammert (Backlog); Kommissionierung kaskadiert NICHT.

## Fortschritt
- Setup (Epic-Umbau zu Etappen A–E, Aufgabe) — **erledigt** (Brain `220272d`).
- Etappe A — **begonnen, Referenz-View-Build steht aus (Checkpoint 2026-08-20).**
  - **Z4-ERHEBUNG (Katalog, vorläufig):** Zählstellen für „Aufträge" gefunden: (1) Listen-Kopf/
    `_Pagination`-TotalCount je Liste (ProductionOrders u.a.), (2) `Views/Home/Index.cshtml` +
    Dashboard-Kacheln (`_ArtikelinfoTile` etc.), (3) `Views/Articles/Info.cshtml:159`
    „@Model.UsedInOrders.Count offene Auftraege" (BOM-Cache-basiert). **Vollständiger Sweep +
    Umsetzung = Etappe D** (Format „N Aufträge · M Sub-FAs"). Keine Überraschung bisher — Zählstellen
    sind überschaubar; im Sweep genauer verifizieren.
  - **Referenz-View-Kontext gesichtet:** `ProductionOrdersController.Index` nutzt
    `IProductionOrderRepository.GetForLeitstandAsync` (zeilenweise Projektion + Server-Spaltenfilter +
    Datumsfilter, Pagination `PageSize.Resolve`/`PaginationState`); View 297 Z., `data-view-key=
    "ProductionOrders"`, `data-server-column-filter="true"`, IsCancelled-Badge Z. 126-128.
    Repo `ProductionOrderRepository` hat noch **keine** gruppierte Abfrage.
  - **Etappe A Build-Fortschritt:**
    - (a) **erledigt** (`30b7a8c`): Repo `GetForLeitstandGroupedAsync` (Gruppe=OrderNumber,
      TotalGroupCount/TotalRowCount, order-number matcht SubOrderNumber ODER OrderNumber, Z2-Sortierung,
      Z3b-showDone) + gemeinsamer `BuildLeitstandQuery`/`ProjectLeitstandRows` + 3 Unit-Tests grün.
      `LeitstandOrderRow` um SubOrderNumber/ParentSubOrderNumber/SageMissingSince erweitert.
    - (b) **erledigt** (`9315186`): `ColumnDefinitions.ProductionOrders` +`parent-sub-order-number`
      (DefaultHidden); `wwwroot/js/fa-liste-gruppierung.js` (Chevron-Collapse je Gruppe, Auto-Expand F2,
      Alle-auf/zu; aktiv nur bei `data-hierarchical="true"`; kein Eingriff in table-filter.js).
    - (c) **OFFEN — Controller-Master-Gate + View-Umbau (der delikate Referenz-Teil):**
      Befund: `ProductionOrders/Index.cshtml` Zeilen-Markup ~137 Z. (reich), `item`-VM reicher als
      `LeitstandOrderRow` (Controller reichert Termine/PrePickingOverride/Coating an). Plan:
      1. Zeilen-`<tr>` (View Z. 98–231) in Partial `_ProductionOrderRow.cshtml` ziehen (Model=item-VM,
         `Model.CanPick`/`HasVorbauAccess`/`EnaioDmsLinks`/`VorkommissionierTage` via ViewData/Wrapper).
      2. item-VM um SubOrderNumber/ParentSubOrderNumber/SageMissingSince; Controller-Mapping (flach +
         gruppiert) füllt sie aus `LeitstandOrderRow`.
      3. Controller: Master lesen (`IServiceSettingRepository.GetValueAsync(HierarchischeStrukturKeys.Master)`
         + IsTrue); bei true `GetForLeitstandGroupedAsync` + Gruppen-Mapping, VM-Flag `Hierarchical`,
         `Groups`, Gruppen-Pagination + Z4-Zählung.
      4. View: `@if (Hierarchical)` → `<table data-hierarchical="true">`, `<tbody class="fa-liste-group">`
         je Gruppe mit colspan-Kopfzeile (`fa-liste-group-head` + `fa-liste-group-toggle`/`fa-liste-chevron`,
         OrderNumber + Knotenzahl) + Sub-FA-Zeilen (`fa-liste-node-row`, order-number-Zelle=SubOrderNumber,
         parent-Spalte, SageMissingSince-Badge); sonst flach wie heute. `fa-liste-gruppierung.js` einbinden.
         Zählformat „N Aufträge · M Sub-FAs" (Z4, nur hier; Sweep = D).
      **Grund für den Checkpoint:** Referenz-View, die B 5× repliziert — bewusst mit Fokus statt gehetzt.
    - (c) **ERLEDIGT** (`abd2620`): Controller-Master-Gate + gemeinsame `MapItem`-Anreicherung (flach+
      gruppiert) + hierarchischer Zweig (Gruppen-Query, Gruppen-Mapping, Gruppen-Pagination, Datumsfilter-
      je-Gruppe/Z3b, Z4-Zahl). `_ProductionOrderRow.cshtml`-Partial (geteilt); im hierarchischen Modus
      order-number=SubOrderNumber, parent-Spalte, SageMissingSince-Badge. View: `data-hierarchical`,
      `<tbody class="fa-liste-group">` je HauptFA (colspan-Kopf+Chevron+Sub-FA-Zahl), Z4-Zählzeile,
      JS-Include. site.css Gruppen-Kopf (WCAG AA). Test-Ctor-Fix. **Web-Suite 1222 grün.**
- **Etappe A KOMPLETT.** Master default aus → produktiv unsichtbar bis Umschaltung.

## Review-Punkte für die Sichtprüfung am Testsystem (ProductionOrders, Master an, 130 Aufträge)
- **Client-Sort im Grouped-Modus:** `table-filter.js` sortiert nur das erste `<tbody>` (Schwester-Spec-
  Fallstrick). Der Server sortiert Z2 (SubOrderNumber je Gruppe). Falls Klick auf einen Sortier-Header
  im hierarchischen Modus nur die erste Gruppe sortiert → in Etappe B/D entschärfen (Sortier-Header im
  hierarchischen Modus deaktivieren o. ä.). **Bitte am Testsystem beobachten.**
- **Spaltenausrichtung** über alle Gruppen (parent-Spalte defaultHidden, per Zahnrad einblendbar).
- **Auto-Expand F2:** ProductionOrders ist server-gefiltert → nach Filter/Reload sind Gruppen offen
  (F2 dort inhärent erfüllt); der Client-Auto-Expand greift, wo ein Client-Hervorheben-Filter existiert
  (Etappe B je View prüfen).
- **Z4-Zählzeile** „N Aufträge · M Sub-FAs" — vollständiger Zähl-Sweep (Home/Kacheln etc.) ist Etappe D.

## Eingang Etappe B
Muster aus ProductionOrders (Repo-Gruppenabfrage-Analogon je Controller ODER generisch, Partial-Row,
`data-hierarchical`, `fa-liste-group`-tbody, JS-Include, ColumnDef `parent-sub-order-number` je viewKey)
auf FaWorklist, FaCompletion, Tracking, Picking, PickingLeitstand replizieren (nur Anzeige-Teil).
