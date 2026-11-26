---
type: spec
title: "IDEAL FA-Liste: nur HauptFA-Zeilen, Strukturfelder im Zahnrad, Sprung in die FA-Struktur"
slug: 2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-1-spec
status: Entwurf
created: 2026-11-26
updated: 2026-11-26
source_backlog: "[[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu]]"
uebersicht: "[[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-uebersicht]]"
depends_on: ""
task: ""
jira: ""
worktree: ""
branch: ""
standort: IDEAL
affected_code:
  - "IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs + ProductionOrderRepository.cs (GetForLeitstandGroupedAsync/BuildLeitstandQuery: Parameter nurHauptFa, identisch zur Leitstand-Spec 2026-11-26 ...-teil-2; FA-Nr.-Filter trifft auch Sub-FA-Nummer per Subquery; Zaehlung Auftraege ohne HauptFA-Zeile)"
  - "IdealAkeWms/Data/Repositories/IFaHierarchyNodeRepository.cs + FaHierarchyNodeRepository.cs + CachedFaHierarchyNodeRepository.cs (neu: GetRootsByHauptFaKeysAsync, ein Batch je Seite)"
  - "IdealAkeWms/Controllers/ProductionOrdersController.cs (Index hierarchischer Zweig: nurHauptFa, Zeilen als Items, neue Kopf-/Wurzelfelder in MapItem, neue Postfilter-Keys)"
  - "IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs (ProductionOrderListItem: neue Anzeige-Felder; ViewModel: NurHauptFa, OhneHauptFaZeile)"
  - "IdealAkeWms/Views/ProductionOrders/Index.cshtml (flacher Zweig auch im hierarchischen Modus, neue <th>/#column-config, Zaehlzeile, Banner)"
  - "IdealAkeWms/Views/ProductionOrders/_ProductionOrderRow.cshtml (neue Zellen nur bei Hierarchical, Knopf FA-Struktur in row-actions)"
  - "IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (ProductionOrders: neue Spalten, DefaultHidden)"
  - "IdealAkeWms/Services/ColumnFilterSyntax.cs (Register ProductionOrders/Index: neue Datums-/Zahl-Keys)"
  - "IdealAkeWms.Tests (Repository nurHauptFa, Postfilter neue Keys, ViewKey-Drift-Waechter ProductionOrders)"
  - "docs/TESTSZENARIEN.md, Views/Help/Index.cshtml + Changelog.cshtml, docs/anwender (FA-Liste)"
open_questions:
  - "1. Sub-FA-Zeilen in der FA-Liste ganz weg (Empfehlung) oder Schalter „Sub-FAs anzeigen“ wie in der Leitstand-Spec (L6)? Hebt fuer die FA-Liste die Gruppenansicht aus [[2026-08-18-fa-liste-hierarchie-anzeige-spec]] auf."
  - "2. Welche Strukturfelder aus der Mapping-Tabelle sollen ins Zahnrad (Spalte „Vorschlag“ = ja/nein bestaetigen), und alle neuen per Default ausgeblendet (Empfehlung) oder einzelne sichtbar?"
  - "3. Matchcode der HauptFA-Zeile: bleibt Artikelstamm (Fallstrick „Listen lesen Article.Matchcode“), oder bei leerem Artikelstamm-Treffer Rueckfall auf den Matchcode der Strukturwurzel (wie Stueckliste Variante B)? Fehlende Endgeraete im Artikelstamm sind ein bekannter Mangel."
  - "4. Kopf-Status (FAInfos.Status): eigene Spalte „PPS-Status“ (Empfehlung) oder in die bestehende Spalte „Sage-Status“ (heute aus FAZusatzinformationen) einspielen? Haengt an Pruefabfrage S-1."
  - "5. AKE-Spalten ohne IDEAL-Quelle (Werkbank, Kaeltemittel, Ventil, Ausfuehrung E/Z, Maschine, ggf. Sage-Status): im hierarchischen Modus unveraendert lassen (Empfehlung) oder ausblenden?"
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

Die IDEAL-FA-Liste zeigt heute je HauptFA eine Gruppe mit allen materialisierten Sub-FAs. Die Liste soll
der schnelle Ueberblick ueber **Auftraege** werden: eine Zeile je HauptFA, alle FA-bezogenen Strukturdaten
per Zahnrad zuschaltbar, und fuer die Sub-FA-Sicht ein Knopf in die FA-Struktur. Die Sub-FA-Sicht lebt
dann an genau einem Ort (FA-Struktur), statt doppelt.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope** (nur hierarchischer Modus, Master `ProduktionsauftragHierarchisch` = true, also IDEAL):

- FA-Liste `/ProductionOrders` zeigt nur HauptFA-Zeilen (`ProductionOrder.IsHauptFa`), Paginierung und
  Zaehler ueber HauptFA-Zeilen.
- Neue, per Default ausgeblendete Spalten aus Auftragskopf (`FaHierarchyOrderInfo`) und Strukturwurzel
  (`FaHierarchyNode`, `SubFA == HauptFA`) gemaess Mapping-Tabelle, je mit Server-Spaltenfilter.
- Knopf je Zeile „FA-Struktur“ → `/FaHierarchy?filterHauptFa=<HauptFA>`.
- Banner fuer Auftraege ohne HauptFA-Zeile (melden statt still verschwinden lassen).

**Out-of-Scope:**

- Flacher Modus (AKE): bit-identisch (Zweig `ProductionOrdersController.cs:398-464` unberuehrt, neue Zellen
  strukturell an `Model.Hierarchical` gebunden wie die K1-Spalten, `ColumnDefinitions.cs:16-21`).
- Leitstand: eigene Spec [[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-2-spec]]
  (paralleler Entwurf, gleicher Repository-Parameter, siehe Uebersicht). Die vier uebrigen Ansichten aus
  [[2026-08-18-fa-liste-hierarchie-anzeige-spec]] (FaCompletion, Picking, FaWorklist, Tracking) bleiben.
- Neue Materialisierung (kein Schreiben nach `ProductionOrders`): die Strukturfelder werden wie die K1-Kopfdaten
  **nur angezeigt** (Drei-Klassen-Trennung K1, [[2026-08-20-materialisierung-fachliche-felder-spec]]).
- Der Durchgang „alle Views korrekt anzeigen“ ([[2026-11-17-ideal-fa-listen-und-views-anzeige-durchgang]],
  ohne Spec) — Ueberlappung: dessen Spalten-Pruefung „leer/falsch/ok“ fuer die FA-Liste wird durch diese Spec
  teilweise vorweggenommen (Mapping-Tabelle unten); der Rest (Kommissionierlisten, Vormontage, Beschichtung,
  BDE-Teileliste, Sage-Plausibilitaet `Start_Beschichtung`) bleibt dort.
- Zusatzfund, nicht mitbauen: der Knopf „OSEON Teileverfolgung“ steht auch an der IDEAL in jeder Zeile
  (`_ProductionOrderRow.cshtml:123-126`), obwohl die IDEAL kein OSEON hat.

## Fachliche Anforderungen

### Ist-Stand (belegt; Buendel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`)

- Hierarchischer Zweig `ProductionOrdersController.Index` `:236-396`: `GetForLeitstandGroupedAsync` (`:246`),
  K1-Kopfdaten aus `GetByHauptFaKeysAsync` (`:263`) — **volle** `FaHierarchyOrderInfo`-Objekte liegen damit
  je Seite schon im Speicher; bei genau einem Kopf werden Kunde, KO, FE, Verladetermin, Prio, AB-Nr.,
  Montage-Abt. uebernommen (`:283-302`), bei Kombigeraet bleiben sie leer + `HeadVariants` (`:303-318`).
- View: ein `<tbody>` je Gruppe mit Kopfzeile und Varianten-Tabelle (`Index.cshtml:134-219`), Zaehlzeile
  „Auftraege · Sub-FAs“ (`:68-75`), `data-hierarchical` steuert `fa-liste-gruppierung.js`/`fa-liste-wiederholung.js` (`:77`, `:279-284`).
- Die HauptFA-Zeile existiert als materialisierte Wurzel (`SubOrderNumber == OrderNumber`):
  `FaMaterializationSyncService.cs:149-153` legt `SubOrderNumber = n.SubFA` an; Nachweis am Testsystem in
  [[2026-09-10-fa-liste-ausbau-matchcode-spec]] Freigabe-Antwort 1. HauptFA-Definition: `ProductionOrder.cs:29-30`.
- Materialisiert werden nur `OrderNumber`, `SubOrderNumber`, `ParentSubOrderNumber`, `Quantity` (= Sollmenge),
  `ArticleNumber` (= Artnr), `Description1/2` (= Bezeichnung1/2), `SageMissingSince`
  (`FaMaterializationSyncService.cs:149-181`) plus Lack-Flag (`:216-228`). Alles Weitere ist nicht am Auftrag.
- Matchcode der Liste kommt aus dem Artikelstamm (`MatchcodeLookup.BuildAsync`, `ProductionOrdersController.cs:254`),
  bewusst (Fallstrick „FA-Struktur-Baum liest FaHierarchyNode.Matchcode, die Listen Article.Matchcode“).
  Gefertigte Endgeraete koennen im Artikelstamm fehlen (Fallstrick „Articles ist eine gefilterte Projektion“) —
  genau das trifft HauptFA-Zeilen.
- Kaeltemittel/Ventil/Ausfuehrung/Maschine/Sage-Status kommen aus `ProductionOrderExtraInfo`, geschrieben von
  `FaZusatzinfoSyncService` fuer jede `ProductionOrder` mit `OrderNumber == WaNummer` (`FaZusatzinfoSyncService.cs:83-97`,
  `:127-139`); ob die View `vw_IDEAL_AKE_WMS_FAZusatzinformationen` in der IDEAL-Sage existiert, ist **unbelegt** (S-1).
- Zugriff: `ProductionOrdersController` und `FaHierarchyController` haben denselben Klassenfilter
  `[RequirePickingOrTrackingOrLeitstandAccess]` (`ProductionOrdersController.cs:13`, `FaHierarchyController.cs:24`);
  FaHierarchy zusaetzlich `[RequireHierarchischeStrukturAktiv]` (`:25`). Wer die FA-Liste sieht, darf in die Struktur.
- Sprung-Muster existiert: `_FaLink.cshtml:14` baut im hierarchischen Modus `Url.Action("Index", "FaHierarchy", new { filterHauptFa = fa.OrderNumber })`.
  Die FA-Struktur filtert `filterHauptFa` per `Contains` (`FaHierarchyController.cs:204-205`).

### Soll

| ID | Anforderung |
|---|---|
| F1 | Hierarchischer Modus: je HauptFA genau eine Zeile (`IsHauptFa`). Keine Gruppen-Kopfzeile, keine Varianten-Tabelle. Paginierung und Zaehlzeile „n Auftraege“ ueber HauptFA-Zeilen. |
| F2 | Filterkarte, Spaltenfilter und C#-Postfilter wirken auf die HauptFA-Zeile. Ausnahme FA-Nr. (Filterkarte und Spalte `order-number`): eine **Sub-FA-Nummer** findet ihren HauptFA (Links von aussen, z. B. `StockApiController.cs:85`, uebergeben auch Sub-FA-Nummern). |
| F3 | Neue Spalten laut Mapping-Tabelle (nach Rueckfrage 2), Default ausgeblendet, je `data-col-key` + Server-Spaltenfilter. Werte nur angezeigt, nie materialisiert. Kombigeraet (mehrere Koepfe): Kopf-Spalten leer, wie heute bei K1 (kein stilles Auswaehlen). |
| F4 | Kombigeraet-Zeile traegt in der FA-Nr.-Zelle das Badge „mehrdeutig (Kombigeraet)“ (heute in der Gruppen-Kopfzeile, `Index.cshtml:167`); die Kopfvarianten zeigt die FA-Struktur (Kopfdaten-Tabelle, `FaHierarchy/Index.cshtml:219-247`). |
| F5 | Knopf „FA-Struktur“ in `row-actions` jeder Zeile, nur `Model.Hierarchical`; Ziel `/FaHierarchy?filterHauptFa=<OrderNumber>`, gleicher Tab. Beschriftung per `title`/`aria-label`, Trefferflaeche wie die Nachbarknoepfe (`btn-sm`). |
| F6 | Banner `alert-warning` mit Text, wenn offene Auftraege ohne HauptFA-Zeile existieren: „n Auftraege ohne HauptFA-Zeile werden in der FA-Liste nicht angezeigt – Struktur unter FA-Struktur.“ Gezaehlt mit demselben `showDone`-Filter. |
| F7 | Spalte „Uebergeordnete Sub-FA“ (`parent-sub-order-number`) entfaellt im hierarchischen Modus (an der HauptFA-Zeile immer leer). |
| F8 | AKE (Master false): Liste, Spalten, Zahnrad-Inhalt und gespeicherte Praeferenzen unveraendert. |

### Mapping-Tabelle Strukturfeld → FA-Listen-Spalte

Quelle K = Auftragskopf `FaHierarchyOrderInfo` (`Models/FaHierarchyOrderInfo.cs:21-39`, je Seite schon geladen),
W = Strukturwurzel `FaHierarchyNode` mit `SubFA == HauptFA` (`Models/FaHierarchyNode.cs:35-64`, neuer Batch-Read).

**Bereits abgebildet (keine Aenderung):**

| Strukturfeld | FA-Liste `data-col-key` | Beleg |
|---|---|---|
| K `Kunde` | `customer` | `ProductionOrdersController.cs:286`, `:225` |
| K `Prio`, `ABNr`, `MontageAbteilung`, `KO_Termin` | `prio`, `ab-nummer`, `montage-abteilung`, `konstruktions-termin` | `:290-292`, `:287` |
| K `FE_Termin` | `production-date` (+ abgeleitet `picking-date`, `bg-date`) | `:288`, `:201-211` |
| K `Verladetermin_Vsl` | `delivery-date` | `:289`, `:203` |
| K `Start_Beschichtung` | `coating-date` (nur Termin-Quelle View, nur mit Lack-Flag) | `BeschichtungResolver.Bestimmen`, `:216-217` |
| W `Artnr`, `Bezeichnung1/2`, `Sollmenge` | `article-number`, `description1/2`, `quantity` | materialisiert, `FaMaterializationSyncService.cs:154-157` |
| W `Beschichtet` (selbst oder direktes Kind) | `coating-part` (Lack-T) | `FaMaterializationCoating.Derive`, Lack-Flag `:216-228` |

**Neu (Vorschlag, Rueckfrage 2):**

| Strukturfeld | neue Spalte (`data-col-key`, Label) | Filtertyp | Vorschlag |
|---|---|---|---|
| K `Status` | `pps-status`, „PPS-Status“ | Text | ja (Rueckfrage 4) |
| K `Pos` | `ab-position`, „AB-Pos.“ | Text | ja |
| K `Dienstleister` | `dienstleister`, „Beschichter“ | Text | ja |
| K `RAL` | `ral`, „RAL“ | Text | ja |
| K `Beschichten_Retour` | `beschichtung-retour`, „Beschicht. retour“ | Datum | ja |
| K `Neuer_PT_PPS` | `neuer-pt-pps`, „Neuer PT (PPS)“ | Datum | ja |
| K `Montagestunden` | `montagestunden`, „Montage-Std.“ | Zahl | ja |
| K `Bemerkung_Uhrzeit` | `bemerkung-uhrzeit`, „Bemerkung/Uhrzeit“ | Text | ja |
| W `Fertigungmenge` | `fertigungsmenge`, „Fert.-Menge“ | Zahl | ja |
| W `Artikelgruppe` | `artikelgruppe`, „Artikelgruppe“ | Text | ja |
| W `Arbeitsbereich` | `arbeitsbereich`, „Arbeitsbereich“ | Text | ja — **nicht** in `workbench` (ADR 0014 Rueckbau v1.43.0: Arbeitsbereich = Zielort, keine Werkbank) |
| W `Kommissionieren` | `komm-ziel`, „Komm.-Ziel“ | Text | ja |
| W `Hauptlagerplatz` | `hauptlagerplatz`, „Hauptlagerplatz“ | Text | ja |
| W `BemerkungPN` | `bemerkung`, „Bemerkung“ | Text | ja |
| W `FertigungsInfoPN` | `fertigungsinfo`, „Fertigungsinfo“ | Text | ja |
| W `Arbeitsschritte` | `arbeitsschritte`, „Arbeitsschritte“ (Rohtext, keine Status-Symbole) | Text | ja |
| W `Material` | `material`, „Material“ | Text | offen |
| W `Artikeltyp` | `artikeltyp`, „Artikeltyp“ | Text | offen |
| W `Breite`/`Hoehe`/`Tiefe` | `abmessungen`, „B × H × T“ (eine Zelle) | Text | offen |
| W `VMBedarf`, `EKBedarf`, `Beschaffungsartikel` | — | — | nein (Stuecklisten-Merkmale der Position, an der Wurzel ohne Auftragsbedeutung; unbelegt, deshalb nur Vorschlag) |
| W `HauptArtnr`, `Matchcode` | — | — | nein (`HauptArtnr` an der Wurzel = Artikel des Geraets, unbelegt; Matchcode siehe Rueckfrage 3) |

Die Bedeutung der neuen Kopf-/Wurzelfelder ist in dieser Spec **nur aus den Namen** abgeleitet (Sage-View-Felder,
nicht gegen die View-Definition geprueft); `Dienstleister` wird z. B. im Beschichtungsauftrag gelesen
(`BeschichtungsauftraegeController.cs:132-139`). Die Labels sind deshalb Vorschlaege fuer die Bestaetigung in
Rueckfrage 2 und gelten bis dahin als **unverifiziert**.

## Technischer Loesungsentwurf

Ponytail: vorhandene Gruppen-Abfrage mit einem Parameter, vorhandener flacher View-Zweig, vorhandene
Kopfdaten-Abfrage, ein neuer Batch-Read, vorhandenes Postfilter-Muster.

1. **Repository (`ProductionOrderRepository`)**: `BuildLeitstandQuery`/`GetForLeitstandGroupedAsync` bekommen
   `bool nurHauptFa = false` — **derselbe** Parameter, den die Leitstand-Spec (Soll L1, Loesung 1) einfuehrt.
   Wer zuerst baut, fuehrt ihn ein; der zweite Lauf verwendet ihn. Bei `true`: `q.Where(ProductionOrder.IsHauptFa)`
   (die eine Sub-FA-Definition, Fallstrick „Es gibt genau EINE Sub-FA-Definition“); FA-Nr.-Filter (`:107-111` und
   `order-number` in `ApplyLeitstandColumnFilter`) zusaetzlich per Subquery
   `_dbSet.Any(s => s.OrderNumber == o.OrderNumber && s.SubOrderNumber.Contains(f))` (Muster `:129-133`).
   Zaehlung „ohne HauptFA-Zeile“: dieselbe Methode wie in der Leitstand-Spec (`CountGruppenOhneHauptFaAsync(showDone)`), nicht zweimal bauen.
2. **Strukturwurzel**: `IFaHierarchyNodeRepository.GetRootsByHauptFaKeysAsync(IReadOnlyCollection<int>)`
   (`Where(keys.Contains(n.HauptFA) && n.SubFA == n.HauptFA)`, `AsNoTracking`), Muster
   `FaHierarchyOrderInfoRepository.GetByHauptFaKeysAsync` (`:39-47`). Decorator `CachedFaHierarchyNodeRepository`
   reicht ohne Cache durch (Schluesselmenge je Seite verschieden). Mehr als eine Wurzel je HauptFA: erste nehmen
   **und** `LogWarning` einmal je Seitenaufbau (melden statt still); Pruefabfrage W-1 klaert die Datenlage vorab.
3. **Controller `Index`, hierarchischer Zweig**: `nurHauptFa: true` an `:246`. Ergebnis-Zeilen wie heute mappen;
   `MapItem` bekommt Kopf und Wurzel durchgereicht und setzt die neuen Felder nur bei eindeutigem Kopf (Kopf-Felder)
   bzw. vorhandener Wurzel. `vm.Items = listGroups.SelectMany(g => g.Items)`, `vm.Hierarchical = true` bleibt
   (FA-Nr.-Zelle, Badges), neu `vm.NurHauptFa = true`, `vm.OhneHauptFaZeile = n`, `IsAmbiguous` je Item fuer F4.
   Neue Text-Keys in `FaListComputedColumnKeys` + `MatchFaListComputedFilter` (`:484-533`), neue Datums-Keys in
   `FaListDateColumnKeys` + `MatchFaListDateFilter` (`:474-514`), Zahl-/Datums-Typen im Register
   `ColumnFilterSyntax.cs:69`. Gruppen-Postfilter-Schleife bleibt (je Gruppe eine Zeile).
4. **View `Index.cshtml`**: Zweigwahl `Model.Hierarchical && !Model.NurHauptFa` → Gruppen (dann nur noch fuer den
   Fall Rueckfrage 1 = Schalter), sonst flacher Zweig. `data-hierarchical` nur bei Gruppenansicht `true`, damit
   `fa-liste-gruppierung.js`/`fa-liste-wiederholung.js` nichts tun. Neue `<th data-filterable data-col-key>` und
   `#column-config`-Eintraege mit `"defaultHidden": true`, alle hinter `@if (Model.Hierarchical)` (Muster `:89-95`,
   `:249-252`); `colCount` entsprechend. `parent-sub-order-number` bei Hierarchical weglassen (F7). Zaehlzeile, Banner F6.
5. **Row-Partial**: neue Zellen hinter `@if (Model.Hierarchical)` in derselben Reihenfolge wie `<th>`; Knopf F5 in
   `row-actions` (`:122-146`), Badge F4 in der FA-Nr.-Zelle (`:46-64`).
6. **ColumnDefinitions.ProductionOrders** (`ColumnDefinitions.cs:23-60`): neue `ColumnDef(..., DefaultHidden: true)`,
   deckungsgleich mit `#column-config` (Drift-Waechter).
7. Frontend: Skill `frontend-design` ausdruecklich aufrufen; Bootstrap-Knopf wie Nachbarn, Kontrast AA, Badge mit Text.

## Migrations-/SQL-Auswirkungen

Keine Migration, kein Schema, kein `SQL/XX`, `00_FreshInstall.sql` und `SQL/AgentJobs/` unberuehrt (nur LINQ-Lesen).

Pruefabfragen vor dem Dev-Lauf (nur lesen):

```sql
-- W-1 (WMS-DB IDEAL-Test): genau eine Wurzel je HauptFA?
SELECT HauptFA, COUNT(*) AS Wurzeln
FROM dbo.FaHierarchyNodes
WHERE SubFA = HauptFA AND VerschwundenSeit IS NULL
GROUP BY HauptFA HAVING COUNT(*) <> 1;

-- W-2 (WMS-DB IDEAL-Test): Auftraege ohne HauptFA-Zeile (Banner F6)
SELECT COUNT(DISTINCT po.OrderNumber) AS OhneHauptFa
FROM dbo.ProductionOrders po
WHERE po.IsDone = 0 AND po.IsCancelled = 0
  AND NOT EXISTS (SELECT 1 FROM dbo.ProductionOrders h
                  WHERE h.OrderNumber = po.OrderNumber
                    AND (h.SubOrderNumber = N'' OR h.SubOrderNumber = h.OrderNumber));

-- W-3 (WMS-DB IDEAL-Test): Fuellgrad der vorgeschlagenen Kopf-/Wurzelfelder (Entscheidungshilfe Rueckfrage 2)
SELECT COUNT(*) AS Koepfe,
       SUM(CASE WHEN NULLIF(LTRIM(Status),'') IS NULL THEN 0 ELSE 1 END) AS mit_Status,
       SUM(CASE WHEN NULLIF(LTRIM(RAL),'') IS NULL THEN 0 ELSE 1 END) AS mit_RAL,
       SUM(CASE WHEN Montagestunden IS NULL THEN 0 ELSE 1 END) AS mit_Montagestd,
       SUM(CASE WHEN Neuer_PT_PPS IS NULL THEN 0 ELSE 1 END) AS mit_NeuerPT
FROM dbo.FaHierarchyOrderInfos;

-- S-1 (Sage IDEAL Test, nur SELECT): gibt es die Zusatzinfo-View an der IDEAL? (Rueckfragen 4/5)
SELECT OBJECT_ID(N'dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen', N'V') AS ViewId;
```

Tabellen-/Spaltennamen belegt: `FaHierarchyNodes` (`SQL/00_FreshInstall.sql:1948-1950`), Spalten aus den Modellen;
`VerschwundenSeit` seit SQL/159. Aussagen, die von S-1 abhaengen (Sage-Status an der IDEAL befuellt oder nicht),
sind bis dahin **unverifiziert**.

## Einstellungen

keine. „Nur HauptFA“ ist im hierarchischen Modus fest (kein Toggle), Master `ProduktionsauftragHierarchisch` unveraendert.
Falls Rueckfrage 1 = Schalter: URL-Parameter der Seite (`mitSubFa`), keine Einstellung.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine — reine Lese-Aenderung, keine Entitaet wird geschrieben. Spalten-Praeferenzen laufen ueber die bestehende
`UserViewPreferences`-API (unveraendert).

## Akzeptanzkriterien

1. IDEAL (Master true): `/ProductionOrders` zeigt je HauptFA genau eine Zeile (die Zeile mit `SubOrderNumber == OrderNumber`), keine Sub-FA-Zeile, keine Gruppen-Kopfzeile.
2. Zaehlzeile „n Auftraege“; `Pagination.TotalCount` = Anzahl HauptFA-Zeilen nach Filtern; Seitengroesse wirkt auf HauptFA-Zeilen.
3. FA-Nr.-Filter (Karte und Spalte) mit einer Sub-FA-Nummer liefert die zugehoerige HauptFA-Zeile; mit der HauptFA-Nummer ebenfalls.
4. Artikelnummer-, Kunde-, Datums- und Matchcode-Filter wirken auf die Werte der HauptFA-Zeile.
5. Jede in Rueckfrage 2 bestaetigte neue Spalte ist im Zahnrad vorhanden, per Default ausgeblendet, zeigt den Wert aus Kopf bzw. Wurzel und hat einen funktionierenden Spaltenfilter (Text: enthaelt/NOT; Datum: KW/Bereich; Zahl: Vergleich).
6. Kombigeraet: Kopf-Spalten (alt und neu) leer, Badge „mehrdeutig (Kombigeraet)“ in der FA-Nr.-Zelle; Wurzel-Spalten befuellt.
7. Knopf „FA-Struktur“ je Zeile oeffnet `/FaHierarchy?filterHauptFa=<HauptFA>` und zeigt dort diese Struktur.
8. Existieren offene Auftraege ohne HauptFA-Zeile, zeigt die Liste das Banner F6 mit der Anzahl (gleich Pruefabfrage W-2); sonst kein Banner.
9. Spalte „Uebergeordnete Sub-FA“ erscheint im hierarchischen Modus weder in der Tabelle noch im Zahnrad.
10. AKE (Master false): FA-Liste unveraendert — gleiche Spalten, gleiche Zeilen, kein FA-Struktur-Knopf, keine neuen Zahnrad-Eintraege.
11. `ColumnDefinitions.ProductionOrders` und `#column-config` sind deckungsgleich (Drift-Waechter-Test gruen).
12. Kein Schreibzugriff: Diff enthaelt keine Migration, keine SQL-Datei, keine Aenderung im Service-Projekt.
13. Build + alle Tests gruen; neue Tests: Repository `nurHauptFa` (Sub-FA-Nummer findet HauptFA, Sub-FA-Zeilen ausgeschlossen), Postfilter je neuem Key-Typ.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (naechste freie Nummer, Stand 2026-11-26 ab TS-203; Dev-Lauf prueft),
Index `secondbrain/tests/testszenarien-index.md`. TS-69/TS-73 (Gruppenansicht FA-Liste) als „fuer die FA-Liste
abgeloest“ kennzeichnen, nicht loeschen.

- **Vorbedingung:** IDEAL-Testsystem, Master an, mind. ein normaler HauptFA, ein Kombigeraet, ein HauptFA mit Lackierteilen; Benutzer mit Picking-Recht und einer nur mit Tracking-Recht.
- **Positiv:** nur HauptFA-Zeilen (AK 1/2); Sub-FA-Nummer suchen (AK 3); neue Spalte einblenden, Wert gegen FA-Struktur-Kopf/-Wurzel pruefen, filtern (AK 5); Knopf FA-Struktur (AK 7); Reload behaelt Zahnrad-Auswahl.
- **Kombigeraet:** AK 6.
- **Negativ:** Auftrag ohne HauptFA-Zeile → Banner (AK 8); Filter ohne Treffer → Leerzustand mit korrektem colspan; AKE-Testsystem unveraendert (AK 10); Benutzer ohne FA-Listen-Recht → kein Zugriff wie bisher.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | (kein Epic) | – | – |

## Deploy

*Vorlaeufig — der qa-agent bestaetigt gegen den echten Diff.*

- **Web-App:** ja (Controller, Repository, Views, ColumnDefinitions)
- **Service:** nein
- **Migration:** nein
- **Manifest-Eintraege** (`scripts/deploy/nachlauf.psd1`):
  - Id `fa-liste-nur-hauptfa-info`, Version = Version des Laufs, Typ `Info`, Firma `IDEAL`, Phase `Nachher`, Ausfuehrung `anzeigen`,
    Text „FA-Liste zeigt nur noch HauptFA-Zeilen; Sub-FAs ueber den Knopf FA-Struktur. Weitere Strukturfelder im Zahnrad.“
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` (im Worktree)

## Offene Rueckfragen

1. Sub-FA-Zeilen in der FA-Liste **ganz weg** (Empfehlung: die Sub-FA-Sicht lebt dann nur in der FA-Struktur, ein Knopf entfernt) oder ein Schalter „Sub-FAs anzeigen“ wie in der Leitstand-Spec (L6), der die heutige Gruppenansicht zurueckholt? Beides hebt fuer die FA-Liste die Standard-Gruppenansicht aus [[2026-08-18-fa-liste-hierarchie-anzeige-spec]] auf; „ganz weg“ macht dort den FA-Listen-Teil (Gruppen-JS, Wiederholungswerte) fuer diese Seite tot.
2. Mapping-Tabelle „Neu“: je Zeile ja/nein (Spalte „Vorschlag“, „offen“ = bitte entscheiden), Labels ok? Alle neuen Spalten per Default ausgeblendet (Empfehlung) oder einzelne sichtbar?
3. Matchcode der HauptFA-Zeile: bleibt Artikelstamm (dokumentierte Festlegung fuer Listen), oder bei fehlendem Artikelstamm-Treffer Rueckfall auf den Matchcode der Strukturwurzel (wie Stueckliste Variante B seit v1.62.0)? Gefertigte Endgeraete fehlen evtl. im Artikelstamm, dann bleibt die Spalte an genau diesen Zeilen leer.
4. Kopf-Status (`FAInfos.Status`): eigene Spalte „PPS-Status“ (Empfehlung) oder im hierarchischen Modus in die bestehende Spalte „Sage-Status“ (heute `ExtraInfo.SageStatus` aus FAZusatzinformationen)? Ergebnis von S-1 bitte dazunehmen.
5. AKE-Spalten ohne bekannte IDEAL-Quelle (Werkbank, Kaeltemittel, Ventil, Ausfuehrung E/Z, Maschine, ggf. Sage-Status) im hierarchischen Modus unveraendert lassen (Empfehlung, kleinster Eingriff) oder ausblenden?

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. →
2. →
3. →
4. →
5. →
