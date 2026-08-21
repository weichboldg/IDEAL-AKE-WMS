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
| B | 5 weitere Views — **3/5**: FaCompletion `b150579`, PickingLeitstand `5b15caf`, Picking `960d5c8` | offen |
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

## Etappe B — eine View pro Lauf (Mensch, 2026-08-20)
Reihenfolge (Tausch 2026-08-20, Mensch): **FaCompletion → PickingLeitstand → Picking → FaWorklist →
Tracking/Index.** Begründung des Tauschs: FaCompletion teilt die Zeilen-Entität (ProductionOrder) mit
ProductionOrders und beweist damit, ob das `_ProductionOrderRow`/MapItem-Muster wirklich wiederverwendbar
ist — diese Erkenntnis brauchen die restlichen vier Views; PickingLeitstand (schwerste View) kann sie nicht
liefern (dort bliebe unklar, ob ein Problem am Muster oder an der View liegt). Nach JEDER View STOPP+Melden
(Sichtprüfung am Testsystem). Vorentscheidungen:
- Alle 5 volle HauptFA-Gruppierung. FA-Nummer-Filter überall Sub-zuerst-dann-Haupt.
- **Views sind heterogen:** PickingLeitstand + FaCompletion = ProductionOrder-Zeilen (nah an der
  Referenz, `_ProductionOrderRow`/MapItem-Muster übernehmbar). FaWorklist (WorkSteps) / Tracking/Index
  (WorkOperations) / Picking (Picking-Status) = **fremde Entitäten je Zeile** → je eigene gruppierte
  Abfrage + eigenes Row-Markup; nur das Rahmen-Muster (Master-Gate, `data-hierarchical`, `fa-liste-group`,
  `fa-liste-gruppierung.js`, ColumnDef `parent-sub-order-number`) ist gleich.
- **Tracking:** Tracking/Index (WorkOperations, `Model.OrderGroups`) ist betroffen; Tracking/OseonIndex
  NICHT (nur QR-Scan am Auftragsnummer-Feld prüfen: trägt er eine FA-Nummer? → melden). Tracking/Index
  gruppiert schon — erst prüfen WONACH; falls OrderNumber, vermischen sich im hierarchischen Modus die
  AGs aller Sub-FAs → **Vorschlag mit Begründung vorlegen, nicht selbst entscheiden.**

### B-View 1: FaCompletion — ERLEDIGT (`b150579`, 2026-08-20)
**Reusability-Befund (der eigentliche Zweck des FaCompletion-first-Tauschs):** Das
`_ProductionOrderRow`/MapItem-Muster ist **NICHT 1:1 übertragbar** — FaCompletion nutzt einen
**anderen Datenpfad**: `GetAllOrderedAsync()` (volle Entities) + In-Memory-Filter/Map/Pagination über
`ColumnFilterHelper`, **nicht** die repo-seitige `GetForLeitstandGroupedAsync`. Deshalb: eigenes Row-Partial
(`_FaCompletionRow.cshtml`) + eigener Gruppier-Code im Controller (GroupBy im Speicher). **Wiederverwendbar ist
das RAHMEN-Muster**, das ich jetzt als bestätigt festhalte und für die nächsten Views als Checkliste nehme:
1. Master-Gate: `IServiceSettingRepository` injizieren + `IsTrue`-Helper + `HierarchischeStrukturKeys.Master`.
2. `data-hierarchical="@(...)"` an der `<table>`; `fa-liste-group`-tbody je HauptFA mit colspan-Kopf +
   `fa-liste-group-toggle`/`fa-liste-chevron` + Sub-FA-Zahl; `fa-liste-gruppierung.js` einbinden.
3. `ColumnDef parent-sub-order-number` (DefaultHidden) in ColumnDefinitions **und** inline column-config.
4. Row: order-number-Zelle = `Hierarchical ? SubOrderNumber : OrderNumber`; parent-Spalte; SageMissingSince-Badge
   (eigene Bedingung, Render-Position wie IsCancelled). Kontext-Wrapper-Klasse (`…RowContext`) statt ViewData.
5. Z4-Zählzeile „N Aufträge · M Sub-FAs" (nur hierarchisch); FA-Nummer-Filter Sub-zuerst-dann-Haupt
   (Filterkarte **und** Spaltenfilter — hier: eigener hierarchischer Column-Map `order-number => "{Sub} {Haupt}"`).
6. Pagination über GRUPPEN (`Pagination.TotalCount = Gruppenzahl`); Z3b: nach Zeilen filtern, DANN gruppieren
   → leere Gruppen entstehen gar nicht.
7. Test-Ctor: neue `IServiceSettingRepository`-Dep als Mock (null=flach; "true"=hierarchisch).

Konsequenz für die Reihenfolge-Views: **PickingLeitstand** teilt den Datenpfad mit ProductionOrders
(`GetForLeitstandAsync`→`GetForLeitstandGroupedAsync` direkt) — dort ist der Controller-Teil näher an der
Referenz. **FaWorklist/Tracking/Index/Picking** haben je fremde Zeilen-Entitäten → je eigenes Row-Partial +
eigene gruppierte Abfrage, nur das Rahmen-Muster ist gleich (wie schon vermutet, jetzt an FaCompletion belegt).

Umgesetzt in FaCompletion: Controller-Master-Gate + hierarchischer Zweig (GroupBy OrderNumber, Z2-Sortierung
SubOrderNumber, Gruppen-Pagination, Z4), `_FaCompletionRow.cshtml`, View (data-hierarchical/fa-liste-group/
Z4/JS), ColumnDef + inline +parent, 3 Controller-Tests (Gruppierung/Z2/Pagination · Sub-oder-Haupt-Filter ·
Flachmodus-Regression). **Web-Suite 1225 grün** (war 1222 + 3). Master default aus → produktiv unsichtbar.

**Sichtprüfung am Testsystem (FaCompletion, Master an):** Gruppen-Kopf „HauptFA … · N Sub-FAs" + Chevron;
Sub-FA-Zeilen zeigen Sub-Nummer; parent-Spalte per Zahnrad einblendbar; Z4-Zeile; FA-Nummer-Filter matcht
Sub **und** Haupt. Client-Sort-im-Grouped-Modus-Vorbehalt (nur erste tbody) gilt hier wie bei ProductionOrders
— beobachten, Entschärfung sammelt sich für Etappe D.

### B-View 2: PickingLeitstand — ERLEDIGT (`5b15caf`, 2026-08-20)
Datenpfad wie ProductionOrders (`GetForLeitstandAsync`→`GetForLeitstandGroupedAsync` direkt) — daher
Controller-Teil nah an der Referenz: gemeinsame `MapItem`-Anreicherung (Pivot+PickingStatus+Termine) für
flach **und** gruppiert; hierarchischer Zweig mit Gruppen-Pagination + Z4. `_PickingLeitstandRow.cshtml`
(geteilt): order-number=SubOrderNumber, parent-Spalte, SageMissingSince-Badge; Bulk-Select/WorkStep-VK-VA-
Zellen/Freigabe/Prio/Picker unverändert. View: data-hierarchical, fa-liste-group-tbody je HauptFA
(colspan-Kopf+Chevron+Sub-FA-Zahl), dynamischer colCount (28 fix inkl. parent + Conditionals), Z4-Zeile,
JS-Include. ColumnDef + inline column-config +parent. **Web-Suite 1227 grün** (war 1225 + 2 Tests:
Gruppierung/Z4/kein-Flach-Aufruf · Flachmodus-Regression).

**Entscheidung 1 — Bulk-Select über mehrere `<tbody>` (getroffen + umgesetzt + getestet):**
- Die Bulk-JS selektiert `.bulk-row-checkbox` **document-weit** (nicht per tbody) → funktioniert schon über
  alle Gruppen. „Alle sichtbaren auswählen" nutzt `isVisible()` (inline `display!=='none'`); eingeklappte
  Gruppen-Zeilen sind `display:none` → **werden korrekt ausgeschlossen**. BulkRelease postet exakt die
  markierten IDs über alle Gruppen.
- **Footgun gefunden + geschlossen:** Zeilen markieren, dann Gruppe **einklappen** ließ sie markiert-aber-
  versteckt (Filter-Pfad hakt sie ab, Collapse meldete nichts). Fix: `fa-liste-gruppierung.js` feuert jetzt
  `fa-liste-group-toggled` (generisch, keine Bulk-Kenntnis); die PickingLeitstand-Bulk-JS ruft darauf
  `bulkSyncSelection()` → versteckte markierte Zeilen werden abgewählt. **BulkRelease gibt keine unsichtbaren
  Aufträge einer eingeklappten Gruppe frei.**
- **Am Testsystem prüfen:** (a) „Alle sichtbaren" markiert nur aufgeklappte Gruppen; (b) Gruppe mit
  markierten Zeilen einklappen → Zähler/Markierung fällt auf die sichtbaren zurück; (c) BulkRelease/
  -zurücknehmen wirkt genau auf die markierten IDs.

**Entscheidung 2 — Memory-Filter (Datum, VK-VA) wirken auf ZEILEN, nicht Gruppen (umgesetzt):** je Gruppe
gefiltert, dann leere Gruppen entfernt (Z3b); Gruppe erscheint, solange ≥1 Zeile passt. Pagination über
die verbleibenden Gruppen.

**Client-Sort-im-Grouped-Modus-Vorbehalt** (nur erste tbody) gilt hier wie bei ProductionOrders/FaCompletion —
sammelt sich für Etappe D.

### B-View 3: Picking (Kommissionier-Liste) — ERLEDIGT (`960d5c8`, 2026-08-21)
**Klassifikations-Korrektur:** Die Picking-**Index-Liste** ist entgegen der ersten Heterogenitäts-Vermutung
NICHT „fremde Entität je Zeile", sondern ProductionOrder-abgeleitet (freigegebene FAs → `PickingListItem`,
In-Memory-Filter/Gruppierung) — **gleiche Form wie FaCompletion**. (Die „Picking-Status je Zeile"-Notiz bezog
sich auf einen anderen Aspekt; der Datenpfad ist FaCompletion-nah.) `_ProductionOrderRow`/MapItem trotzdem
nicht 1:1 (eigene schlanke Zeile), aber Rahmen-Muster + In-Memory-GroupBy wie FaCompletion.

**BESONDERHEIT — Prioritäts-Warteschlange (getroffene + zu bestätigende Entscheidung):** Die Kommissionier-
Liste ist die **Arbeits-Warteschlange des Kommissionierers**, sortiert nach `PickingPriority` (asc, null zuletzt,
dann Termin; `GetReleasedForPickingAsync`). Gruppierung nach HauptFA **reorganisiert diese Queue**. Gewählt
(least-disruptive): die Eingabe ist schon Prio-sortiert, **stabiles GroupBy** liefert dadurch **Gruppen-
Reihenfolge = dringlichste (kleinste Prio) zuerst** und **innerhalb der Gruppe die Prio-Reihenfolge** —
**bewusst NICHT SubOrderNumber (generisches Z2)**, weil hier die Dringlichkeit führt. Kein Zwischen-Merge →
am Testsystem bestätigen oder auf „within-group SubOrderNumber" umstellen (dann verliert die Gruppe die
Prio-Ordnung). **Prominent im STOP-Report geflaggt.**

Umgesetzt: Controller-Master-Gate + hierarchischer Zweig (GroupBy OrderNumber, Gruppen-Pagination, Z4),
FA-Nummer-Spaltenfilter Sub-zuerst-dann-Haupt (eigener hierarchischer Column-Map); `_PickingRow.cshtml`
(geteilt, `clickable-row` erhalten, order-number=SubOrderNumber, parent-Spalte, SageMissingSince-Badge);
View (data-hierarchical, fa-liste-group-tbody, Z4-Zeile, JS-Include, `hasRows`-Leerprüfung für beide Modi);
ColumnDef + inline column-config +parent. **Web-Suite 1230 grün** (war 1227 + 3 Tests: Gruppierung/
dringlichste-zuerst/Z4 · Sub-oder-Haupt-Filter · Flachmodus-Regression).

**Sichtprüfung am Testsystem (Picking, Master an):** (1) **Reihenfolge-Bestätigung:** dringlichste HauptFA-
Gruppe oben, innerhalb der Gruppe Prio-Reihenfolge — passt das zum Kommissionier-Workflow, oder soll innerhalb
SubOrderNumber sortiert werden? (2) Klick auf eine Sub-FA-Zeile öffnet deren Stückliste; (3) Chevron auf/zu;
(4) FA-Nummer-Filter matcht Sub und Haupt; (5) Z4-Zeile. Client-Sort-im-Grouped-Vorbehalt wie überall (Etappe D).

### B-View 2 (Original-Untersuchung): PickingLeitstand — Checkpoint-Notiz (erledigt, s. oben)
**Zusatzpunkte für den Bauplan (Mensch, 2026-08-20) — vor dem Build explizit entscheiden + testen:**
- **Bulk-Select über mehrere `<tbody>`:** Greift „alle auswählen" über ALLE Gruppen oder nur die erste?
  Folgen sind real — BulkRelease gibt Aufträge frei. Und: werden Zeilen in ZUGEKLAPPTEN Gruppen
  mitausgewählt? Beides explizit entscheiden und testen (nicht implizit lassen).
- **Memory-Filter (Datum, VK-VA):** wirken auf ZEILEN, nicht auf Gruppen — eine Gruppe erscheint, solange
  ≥1 Zeile passt (Z3b), leere Gruppen fallen weg. (Analog zum FaCompletion-GroupBy-nach-Filter-Muster.)
Befund: **komplexeste View** — `Views/PickingLeitstand/Index.cshtml` 830 Z., Zeilen-`<tr>` ~246 Z.
(Bulk-Select-Checkbox, WorkStep-VK-VA-Zellen mit 3-Wert-Status, Freigabe/Priorität/Picker-Zuweisung,
IsDoneBde-Toggle [= Kaskade-Host Etappe C], DMS-Badges). Controller nutzt `GetForLeitstandAsync`
(→ `GetForLeitstandGroupedAsync` direkt nutzbar) + `PickingLeitstandItem`-VM (WorkStep-Pivot) +
**Memory-Filter** (`LeitstandDateColumnKeys` + `LeitstandWorkStepColumnKeys`). `IServiceSettingRepository`
**noch nicht injiziert**.
**Bauplan (analog ProductionOrders):**
1. `PickingLeitstandController`: `IServiceSettingRepository` injizieren; Zeilen-Select (Z. 96–153) in
   lokale `MapItem`-Funktion; Master lesen; hierarchisch → `GetForLeitstandGroupedAsync` + Gruppen-Mapping
   + Gruppen-Pagination + Memory-Filter-je-Gruppe (Datum + VK-VA) + Z4-Zahl; VM-Flag Hierarchical/Groups/
   HierarchicalRowCount + `PickingLeitstandGroup`-Klasse; item +SubOrderNumber/ParentSubOrderNumber/SageMissingSince.
2. Zeilen-`<tr>` (Z. 146–392) in `_PickingLeitstandRow.cshtml` (Kontext-Wrapper wie ProductionOrderRowContext:
   CanPick/CanManagePickingRelease/LeitstandAktiv/PickerAssignmentEnabled/EnaioDmsLinks/ViewBag.ActivePickers/
   Hierarchical). order-number-Zelle=SubOrderNumber, parent-Spalte, SageMissingSince-Badge.
3. View: `data-hierarchical`, `fa-liste-group`-tbody je HauptFA (colspan-Kopf+Chevron+Sub-FA-Zahl) vs. flach;
   thead+#column-config +parent-sub-order-number; Z4-Zählzeile; `fa-liste-gruppierung.js` einbinden.
   **Bulk-Select im Grouped-Modus prüfen** (Header-Checkbox/„alle sichtbaren" über mehrere tbody).
4. `ColumnDefinitions.PickingLeitstand` +`parent-sub-order-number`. Test-Ctor-Fixes (neue Dep).
**Warum Checkpoint:** schwerste View, 246-Zeilen-Row-Extraktion + Memory-Filter/Bulk-Select-im-Grouped-Modus
— mit frischem Fokus statt am Ende der Marathon-Session. Kaskade (IsDoneBde) bleibt Etappe C.
