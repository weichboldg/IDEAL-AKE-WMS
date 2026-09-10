---
type: spec
title: "FA-Liste ausbauen: Zeilenwerte, HauptFA-Zeile, Freigabe-Kaskade, Matchcode"
slug: 2026-09-10-fa-liste-ausbau-matchcode-spec
status: Entwurf
created: 2026-09-10
updated: 2026-09-10
source_backlog: "[[2026-09-10-fa-liste-ausbau-matchcode]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - "IdealAkeWms/Models/ProductionOrder.cs (neue Spalte `Matchcode` (string?, NVARCHAR), analog `ArticleNumber`/`Description1`)"
  - "IDEALAKEWMSService/Services/FaMaterializationSyncService.cs + FaMaterializationPlanner.cs (`Matchcode` aus `FaHierarchyNode.Matchcode` in Anlege- UND Update-Zweig setzen, F1-Regel — gleiches Muster wie `ArticleNumber`/`Description1/2`, kein Sonderfall, keine Z1-Ausnahme noetig, da nicht app-verwaltet)"
  - "IdealAkeWms/Migrations/<neu>_AddProductionOrderMatchcode.cs + SQL/91_AddProductionOrderMatchcode.sql (OBJECT_ID/COL_LENGTH-Guard, additiv) + SQL/00_FreshInstall.sql (Schema-Objekt UND MigrationId)"
  - "IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs (`ProductionOrderListItem`: neue Properties `Matchcode`, `KonstruktionsTermin`, `Prio`, `AbNummer`, `MontageAbteilung` — `Customer`/`ProductionDate`/`DeliveryDate` existieren bereits auf der Zeile, werden aber im hierarchischen Modus bisher NICHT aus den K1-Gruppenwerten befuellt; `ProductionOrderListGroup` ggf. um `IsHauptFaRow`-Kennzeichnung je Item statt eigenem Feld auf der Gruppe)"
  - "IdealAkeWms/Controllers/ProductionOrdersController.cs (`MapItem`: zusaetzliche Parameter fuer K1-Zeilenwerte aus der bereits geladenen `heads`/`group`-Struktur je Zeile setzen statt nur auf Gruppenebene; Kopfzeile-Badges reduzieren [Rueckfrage 2]; HauptFA-Zeile markieren [Rueckfrage 1]; Kunde/Prio/AB-Nummer/Montage-Abteilung/Konstruktions-Termin als C#-Postfilter analog dem bestehenden `FaListDateColumnKeys`/`hasDateFilters`-Zweig behandeln, Kunde-Join-Sonderweg in der Query entfaellt dadurch fuer die FA-Liste)"
  - "IdealAkeWms/Controllers/PickingLeitstandController.cs (dieselbe Kunde-Postfilter-Umstellung, da `BuildLeitstandQuery`/`GetForLeitstandGroupedAsync` gemeinsam mit `ProductionOrdersController` genutzt wird, siehe bereits bestehende Notiz in der Materialisierungs-Spec 'betrifft auch PickingLeitstandController'; NEU: `CascadeReleasePreview`/`CascadeRelease`-Actions analog `CascadeDonePreview`/`CascadeDone`, aber `[RequireLeitstandAccess]` statt `[RequirePickingAccess]` — Freigabe ist ein Leitstand-Recht wie `ToggleRelease`/`BulkRelease`, nicht ein Picking-Recht wie die Fertigmeldungs-Kaskade)"
  - "IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs + IProductionOrderRepository.cs (Kunde-Subquery-Sonderweg in `BuildLeitstandQuery`/`BuildCustomerColumnFilterPredicate` entfernen zugunsten des C#-Postfilters — der `filterCustomer`-Freitextfilter UND der `customer`-Spaltenfilter laufen im hierarchischen Modus dann wie jeder andere on-the-fly berechnete Wert)"
  - "IdealAkeWms/Data/Repositories/IProductionOrderPickingStatusRepository.cs + ProductionOrderPickingStatusRepository.cs (neue `Task<int> SetReleaseForOrderNumberAsync(string orderNumber, string? releasedBy, string modifiedBy, string modifiedByWindows)` — setzt `IsReleasedForPicking=true` NUR fuer aktuell `false`-Zeilen dieser `OrderNumber`-Gruppe, vergibt fortlaufende `PickingPriority` ab `MAX+1` analog `SetReleaseBatchAsync`, EIN `SaveChangesAsync` fuer Atomaritaet [analog `SetIsDoneBdeForOrderNumberAsync`], gibt die Anzahl TATSAECHLICH geaenderter Zeilen zurueck, ruehrt bereits freigegebene Zeilen nicht an [auch nicht `ModifiedAt`])"
  - "IdealAkeWms/Views/PickingLeitstand/Index.cshtml (Gruppen-Kopfzeile: neuer Button 'Alle Sub-FAs freigeben' analog `btn-cascade-done`, eigenes Bestaetigungs-Modal `cascadeReleaseModal` analog `cascadeDoneModal` inkl. Kommissionierer-Auswahl wenn `PickerAssignmentEnabled`; `BulkRelease`-Modal/-Mechanismus bleibt UNVERAENDERT zeilenbasiert)"
  - "IdealAkeWms/Views/ProductionOrders/Index.cshtml (Gruppen-Kopfzeile schlanker: nur noch HauptFA-Nummer, Sub-FA-Zahl, Mehrdeutig-Badge [Rueckfrage 2]; neue Zeilenspalten Kunde/Termine/Prio/AB-Nummer/Montage-Abteilung/Matchcode mit 'nur wo der Wert wechselt'-Darstellung; HauptFA-Zeile visuell/durch eigene Aktionen markiert [Rueckfrage 1])"
  - "IdealAkeWms/Views/ProductionOrders/_ProductionOrderRow.cshtml (neue Zellen fuer die K1-Zeilenwerte + Matchcode, Wechsel-Erkennung zur Vorzeile innerhalb derselben Gruppe)"
  - "IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (neue `ColumnDef('matchcode', 'Matchcode', Locked: false)` — Position neben `article-number`/`description1` — mindestens in `ProductionOrders`, `PickingLeitstand`, `FaCompletion`, `FaWorklist`, `Picking`, `OseonTracking`; je Erhebungsergebnis ggf. weitere Views [Rueckfrage 7]; zusaetzlich `customer-row`/`prio`/`ab-nummer`/`montage-abteilung`/`konstruktions-termin` NUR in `ProductionOrders`, da Kopfdaten-je-Zeile laut Umfangsentscheidung auf die FA-Liste begrenzt ist)"
  - "Views/ProductionOrders/Index.cshtml inline `#column-config` (Fallstricke §10-Regel: JEDE neue `<th data-col-key>` MUSS zusaetzlich zu `ColumnDefinitions.cs` hier eingetragen werden, sonst rutscht sie beim Re-Append vor alle registrierten Spalten)"
  - "IdealAkeWms/Views/PickingLeitstand/_PickingLeitstandRow.cshtml, Views/FaWorklist/_FaWorklistRow.cshtml, Views/FaCompletion/_FaCompletionRow.cshtml, Views/Picking/_PickingRow.cshtml, Views/Tracking/Index.cshtml o.ae. (Matchcode-Zelle je Erhebungsergebnis)"
  - "docs/TESTSZENARIEN.md"
  - "secondbrain/tests/testszenarien-index.md"
  - "secondbrain/specs/entwurf/2026-08-18-fa-liste-hierarchie-anzeige-spec.md (NACHZIEHEN, siehe Abschnitt 'Bezug': Freigabe-Kaskade-Umkehr — Antwort 3/Tabelle Abschnitt 8 'BulkRelease ist Freigabe, nicht Fertigmeldung — keine Kaskade' wird durch die neue eigene Aktion `CascadeRelease` ergaenzt, NICHT durch eine Aenderung an `BulkRelease` selbst)"
open_questions:
  - "Ist die Wurzelzeile (FaHierarchyNode mit VaterFA=NULL) bereits heute eine materialisierte ProductionOrder-Zeile (SubOrderNumber der Wurzel == OrderNumber)? Voraussetzung fuer Punkt 2 (HauptFA als normale Zeile)."
  - "Welche der bisherigen Kopfzeilen-Badges (Kunde/Prio/Montage-Abteilung/AB-Nummer/Termine) entfallen komplett vs. bleiben zusaetzlich zur Zeilen-Darstellung bestehen?"
  - "Zeilen-Darstellung bei Kombigeraeten (mehrdeutiger HauptFA, IsAmbiguous=true): zeigt jede Sub-FA-Zeile ihre eigene Kopfvariante (welche? woher die Zuordnung Zeile-zu-Variante?) oder bleiben die K1-Zeilenspalten dort grundsaetzlich leer?"
  - "'Nur wo der Wert wechselt': serverseitig in Razor nach Sortierung berechnet, oder client-seitig per JS? Und wie verhaelt sich die Regel in Kombination mit Server-Spaltenfilter/-Sortierung, die die Zeilenreihenfolge veraendert?"
  - "Freigabe-Kaskade: Greift die bestehende Pflicht 'Kommissionierer zuweisen' (KommissionierungMitZuweisung) analog BulkRelease, mit EINEM Picker fuer alle kaskadierten Sub-FAs? Oder laeuft die Kaskade ohne Picker-Zuweisung, die dann je Sub-FA einzeln nachgetragen wird?"
  - "Freigabe-Kaskade: Sollen neu freigegebene Sub-FAs eine fortlaufende PickingPriority bekommen (wie SetReleaseBatchAsync), oder bleibt die Prioritaet bei der Kaskade unbelegt (Werker vergibt sie separat)?"
  - "Matchcode-Scope: gilt diese Spec nur fuer die FA-Zeilen-Ebene (ProductionOrder, 6-8 Standardlisten) oder auch fuer die BOM-Komponentenzeile (Picking/Bom.cshtml, PrintBom, PrintPicking, MissingParts, WarehouseRequisitions)? Die Komponentenzeile haengt an `FaHierarchyBomItem`/`BomItem`, NICHT an `ProductionOrder` — eine andere Materialisierung/Erweiterung waere noetig (siehe Erhebung unten)."
  - "AKE-Quelle Matchcode: liefert `vw_AKE_Kommissionierung_WAListe`/`vw_AKE_..._StuecklistenDB` den Matchcode bereits? Falls nein: Sage-View-Erweiterung ist externe Abhaengigkeit, mit der Sage-Betreuung VOR Umsetzungsbeginn abzustimmen."
  - "Matchcode-Spalte defaultHidden ja/nein je Liste — hausweit einheitlich laut Entscheidung 2026-09-10, aber innerhalb dieser Vorgabe ist der konkrete Default (sichtbar vs. ausgeblendet) noch offen."
  - "[Geparkt aus Backlog, unveraendert] B-0/Ruling 4 — Vollstruktur in der Vorbau-Stueckliste einer HauptFA: erscheint dasselbe Material auf zwei Kommissionierlisten? Am Bildschirm zu testen, nicht zu entscheiden; Ergebnis entscheidet ueber Rueckbau vs. Druck-Whitelist-Erweiterung in PrintBom.cshtml (Ebene + Komm.-Ziel). Vor Merge zu klaeren."
  - "[Geparkt aus Backlog, mit Rechercheergebnis] HasCoatingParts/zweite Z1-Ausnahme: Recherche in diesem Spec-Lauf zeigt, dass F5/F6 aus der Materialisierungs-Spec im Worktree-Stand bereits umgesetzt sind (Klassenkommentar von FaMaterializationSyncService nennt Workplace UND PickingStatus.HasCoatingParts explizit als die zwei Z1-Ausnahmen). Offen bleibt nur die BESTAETIGUNG, dass dieser Stand nach dem noch ausstehenden Merge/Schranke-2 der Materialisierungs-Spec unveraendert gilt."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
# Flache Schluessel mit Absicht: Obsidians Property-Editor kann verschachtelte
# YAML-Objekte NICHT bearbeiten - und genau diesen Block fuellt der Mensch aus.
---

> **Kontext (nicht Vorgabe an den Dev-Lauf):** Diese Spec baut auf
> [[2026-08-20-materialisierung-fachliche-felder-spec]] (Testbereit, v1.37) auf, ist aber
> **bewusst eine eigene Spec**, keine Erweiterung jener — die dortige Spec steht mit frischer QA
> auf `Testbereit`; ein Wiederaufreissen kostet einen vollstaendigen Re-QA-Lauf. Die Umsetzung
> erfolgt im **bestehenden Buendel-Worktree** `.claude/worktrees/2026-08-07-ideal-teile-1-5`
> (Branch `feature/2026-08-07-ideal-teile-1-5`), damit sie mit dem dort laufenden Stand konsistent
> bleibt — das ist Kontext fuer den Dev-Lauf, keine Vorgabe dieses Spec-Agent-Laufs.
>
> **Wichtiger Recherchebefund:** Der komplette IDEAL-Hierarchie-Code (`FaHierarchyNode`,
> `ProductionOrder`-Hierarchiefelder, `FaMaterializationSyncService` usw.) existiert **nur** im
> genannten Worktree — im `main`-Checkout, in dem dieser Spec-Agent-Lauf ausgefuehrt wurde, gibt es
> davon noch **nichts** (unmerged Buendel). Alle Code-Referenzen in dieser Spec beziehen sich auf
> den Worktree-Stand.

## Ziel / Nutzen (das Warum)

Die FA-Liste (`/ProductionOrders`) und der Leitstand (`/PickingLeitstand`) zeigen im hierarchischen
IDEAL-Modus seit [[2026-08-18-fa-liste-hierarchie-anzeige-spec]] und
[[2026-08-20-materialisierung-fachliche-felder-spec]] Kopfdaten (Kunde, Termine, Prio, AB-Nummer,
Montage-Abteilung) **ausschliesslich in der Gruppen-Kopfzeile**. Das genuegt fuer die reine
Anzeige, blockiert aber drei Anschlussbeduerfnisse: Spaltenfilter/Sortierung auf diesen Werten (sie
existieren fuer eine Zeile schlicht nicht), einen spaeteren zeilenbasierten Export, und eine
saubere Trennung zwischen „Auftragskopf" und „HauptFA als eigenstaendiger Fertigungsauftrag mit
eigenen Aktionen". Zusaetzlich fehlt der bei IDEAL fachlich zentrale **Matchcode**
(Typenkurzbezeichnung, `FaHierarchyNode.Matchcode`) ueberall dort, wo heute eine Artikelbezeichnung
steht — er ist bisher nur in der FA-Struktur-Ansicht (`/FaHierarchy`) sichtbar.

Drittens kehrt diese Spec eine fruehere, bewusst vorsichtige Festlegung um: Die
Freigabe-Kaskade am HauptFA (heute: keine) wird eingefuehrt, mit derselben Kaskaden-Mechanik wie
die bereits bestehende Fertigmeldungs-Kaskade, aber als **eigene, neue Aktion** — `BulkRelease`
selbst bleibt unveraendert zeilenbasiert.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:**

1. **Kopfdaten je Zeile (FA-Liste `/ProductionOrders`, nicht die uebrigen fuenf Ansichten — siehe
   „Danach" unten und Rueckfrage-Praezedenz aus der Materialisierungs-Spec „nur FA-Liste").**
   Kunde, Termine (Konstruktions-/Fert.-/Liefertermin), Prio, AB-Nummer, Montage-Abteilung werden
   zusaetzlich zur Gruppen-Kopfzeile als **Zeilenwerte im Anzeige-Modell** verfuegbar (nicht in
   `ProductionOrders` materialisiert — K1-Regel der Materialisierungs-Spec bleibt gewahrt), damit
   Spaltenfilter/Sortierung/ein spaeterer Export darauf zugreifen koennen. Angezeigt wird der Wert
   nur, wo er sich gegenueber der Vorzeile aendert (reine Darstellungsfrage).
2. **HauptFA als normale Zeile (FA-Liste).** Die Gruppen-Kopfzeile wird schlanker (HauptFA-Nummer,
   Sub-FA-Zahl, Mehrdeutig-Badge); die bisher dort gezeigten Kopfdaten-Badges entfallen ganz oder
   teilweise (Rueckfrage 2), weil sie durch Punkt 1 ohnehin in der ersten Zeile erscheinen. Die
   HauptFA-eigene Zeile traegt ihre Aktionen selbst.
3. **Freigabe-Kaskade (`/PickingLeitstand`).** Eine neue Aktion „Alle Sub-FAs freigeben" in der
   Leitstand-Gruppen-Kopfzeile, analog zur bestehenden Fertigmeldungs-Kaskade (`CascadeDone`):
   kaskadiert ueber ALLE Nachfahren (= alle Zeilen mit derselben `OrderNumber`, nicht nur direkte
   Kinder), atomar, mit Bestaetigungsdialog und Anzahl, laesst bereits freigegebene Sub-FAs
   unveraendert, meldet die Anzahl TATSAECHLICH neu freigegebener, kennt keine Gruppen-Ruecknahme.
   `BulkRelease` (Mehrfachauswahl) bleibt **unveraendert zeilenbasiert**.
4. **Matchcode als Querschnittsspalte (hausweit, IDEAL UND AKE).** Neue Spalte
   `ProductionOrder.Matchcode`, materialisiert aus `FaHierarchyNode.Matchcode` (Anlege- und
   Update-Pfad, F1-Regel), additive Migration. Anzeige in allen ADR-0005-Listen, die heute eine
   Artikelbezeichnung zeigen (Erhebung siehe unten), Position neben Artikelnummer/Bezeichnung 1,
   ueber das Zahnrad steuerbar wie jede andere Spalte. Fuer IDEAL sofort befuellt; fuer AKE leer,
   bis die Sage-Quelle den Matchcode liefert — Anzeige/Filter/Sortierung duerfen daran nicht
   scheitern (F7-Regel, siehe Fallstricke §7-Referenz „leere Spalte wird ausgeblendet/liefert kein
   Falsch-Leer").
5. **Erhebung** (Teil des Umfangs, siehe Abschnitt „Rechercheergebnis: Erhebung Artikelbezeichnung"
   unten): welche Listen zeigen heute eine Artikelbezeichnung, und gehoert der Matchcode dort auf
   FA-Zeilen-Ebene oder auf BOM-Komponentenebene (zwei verschiedene Datenquellen, siehe Rueckfrage
   7).
6. Testszenarien fuer alle vier Themenbloecke inkl. AKE-Regression (Matchcode bleibt leer, sonst
   bit-identisch).

**Out-of-Scope:**

- **Replikation der Kopfdaten-je-Zeile auf Leitstand, Tracking, Picking, FaWorklist, FaCompletion**
  — explizit als „Danach" im Backlog benannt, bewusst NICHT Teil dieser Spec. Diese Spec liefert
  mit Punkt 1 die Zeilenquelle, die eine spaetere Replikation deutlich vereinfacht (dieselbe
  Zeilenquelle statt einer Kopfzeilen-Sonderloesung je View), fuehrt sie aber nicht selbst durch.
- **B-0/Ruling-4-Entscheidung selbst** (Vollstruktur in der Vorbau-Stueckliste) — wird getestet,
  nicht in dieser Spec entschieden (geparkte Frage 1, siehe unten). Muss laut Backlog **vor** dem
  Merge geklaert sein, ist aber kein Bestandteil des hier beschriebenen Funktionsumfangs.
- **Matchcode auf BOM-Komponentenebene** (Picking/Bom, PrintBom, PrintPicking, MissingParts,
  WarehouseRequisitions), falls die Klaerung in Rueckfrage 7 ergibt, dass das eine andere
  Datenquelle (`FaHierarchyBomItem`) braucht — dann ist das Folgearbeit, kein Bestandteil dieser
  Spec (siehe Erhebungsergebnis).
- **Kombinationsgeraete als Gruppen-Schluessel-Erweiterung** — weiterhin paketweit out of scope
  (unveraendert aus Teil 7/Materialisierungs-Spec).
- **AKE-Sage-View-Erweiterung selbst** (falls Rueckfrage 8 ergibt, dass sie noetig ist) — das ist
  eine externe Abhaengigkeit mit der Sage-Betreuung, nicht durch einen Dev-Lauf loesbar. Diese
  Spec baut die App-seitige Spalte/Materialisierung/Anzeige so, dass sie sich fuellt, sobald die
  Quelle liefert — ohne selbst die Quelle zu aendern.

## Rechercheergebnis: Erhebung „wo steht heute eine Artikelbezeichnung"

Verifiziert am Code (`grep` auf `Description1`/`Bezeichnung`/`ArticleDescription` in `Views/` und
auf `description`-`ColumnDef`-Eintraege in `ColumnDefinitions.cs`), Stand Worktree
`2026-08-07-ideal-teile-1-5`:

| ColumnDefinitions-`viewKey` | View | Zeigt Bezeichnung (Spalte) | Matchcode-Quelle waere |
|---|---|---|---|
| `ProductionOrders` | `Views/ProductionOrders/Index.cshtml` | `description1`/`description2` | `ProductionOrder.Matchcode` (FA-Zeile) |
| `PickingLeitstand` | `Views/PickingLeitstand/Index.cshtml` | `description1`/`description2` | `ProductionOrder.Matchcode` (FA-Zeile) |
| `FaCompletion` | `Views/FaCompletion/Index.cshtml` | `description1`/`description2` | `ProductionOrder.Matchcode` (FA-Zeile) |
| `FaWorklist` | `Views/FaWorklist/Index.cshtml` | `description1` | `ProductionOrder.Matchcode` (FA-Zeile) |
| `Picking` | `Views/Picking/Index.cshtml` (Kommissionierliste) | `description` | zu klaeren (Rueckfrage 7) — je Kommissionierposition, evtl. BOM-Ebene |
| `OseonTracking` | `Views/Tracking/*.cshtml` | `description` | vermutlich `ProductionOrder.Matchcode` (FA-Zeile), zu verifizieren |
| `Bom` | `Views/Picking/Bom.cshtml` | `description1`/`description2` | **BOM-Komponentenebene** (`FaHierarchyBomItem`), NICHT `ProductionOrder` |
| `WarehousePickingDetails` | `Views/WarehousePicking/Details.cshtml` | `description` | **BOM-/Requisitions-Komponentenebene**, NICHT `ProductionOrder` |
| `FaHierarchyStructure` | `Views/FaHierarchy/Index.cshtml` | `description` | **bereits vorhanden** — Matchcode-Spalte existiert dort schon (Locked, `FaHierarchyNode.Matchcode` direkt) |

Zusaetzlich als Artikelbezeichnung-Anzeigen identifiziert, aber **nicht** ueber
`ColumnDefinitions`/ADR-0005-Pattern (Druckdokumente/Fehlteile/Bestellungen aus dem Backlog-Text):
`Views/Picking/PrintBom.cshtml`, `Views/Picking/PrintPicking.cshtml` (eigene Druck-Whitelists, kein
Zahnrad), `Views/MissingParts/Index.cshtml`, `Views/MissingPartsLager/Index.cshtml`,
`Views/WarehouseRequisitions/Edit.cshtml`, `Views/Articles/Index.cshtml` + `Info.cshtml` +
`Edit.cshtml`, `Views/StockOverview/Index.cshtml`, `Views/StockMovements/*.cshtml`. Alle diese
zeigen die Bezeichnung einer **Komponente/eines Artikels**, nicht eines FA/Sub-FA — ihre
Datenquelle ist `Article`/`BomItem`/`FaHierarchyBomItem`, strukturell verschieden von
`ProductionOrder.Matchcode`. Diese Liste ist die Grundlage fuer Rueckfrage 7, **nicht** bereits eine
Entscheidung — die genaue Machbarkeit (`FaHierarchyBomItem` ist eine gefahrlos erweiterbare
Ableitung, siehe Fallstricke §10 „`BomItem` darf KEINE neue Property bekommen" — die Ausnahme ist
die abgeleitete Klasse) ist Dev-Lauf-Aufgabe, sobald Rueckfrage 7 den Umfang klaert.

**Fazit fuer den gesicherten Kern dieser Spec:** Sechs FA-Zeilen-Ebene-Listen
(`ProductionOrders`, `PickingLeitstand`, `FaCompletion`, `FaWorklist`, `Picking`, `OseonTracking`)
koennen den neuen `ProductionOrder.Matchcode` direkt zeigen, sobald er materialisiert ist — ohne
weitere Datenquellen-Aenderung. Die BOM-Komponentenebene (`Bom`, `WarehousePickingDetails`,
Druckdokumente, Fehlteile, Bestellungen) braucht eine andere, hier NICHT gesicherte Erweiterung.

## Fachliche Anforderungen

### 1 — Kopfdaten je Zeile (FA-Liste)

- Zeilenwerte fuer Kunde, Konstruktions-/Fert.-/Liefertermin, Prio, AB-Nummer, Montage-Abteilung
  werden im hierarchischen Modus aus denselben `FaHierarchyOrderInfo`-Kopfdaten befuellt, die heute
  bereits fuer die Gruppen-Kopfzeile geladen werden (`GetByHauptFaKeysAsync`, **kein** zusaetzlicher
  Fan-out-Join). `MapItem` erhaelt dafuer dieselben Gruppenwerte, die es heute schon fuer
  `ProductionDate`/`DeliveryDate`/`BeschichtungTermin` nutzt (Termin-Kaskade-Mechanik der
  Materialisierungs-Spec), zusaetzlich fuer `Customer`/`Prio`/`AbNummer`/`MontageAbteilung`.
- **Anzeige „nur wo der Wert wechselt":** reine Darstellungsregel, keine Datenregel — die Werte
  existieren auf JEDER Zeile, nur die Darstellung blendet Wiederholungen zur Vorzeile innerhalb
  derselben Gruppe aus. Exakte Umsetzung (serverseitig nach Sortierung vs. clientseitig) ist
  Rueckfrage 4.
- **Kombigeraet-Fall:** bei `IsAmbiguous=true` gibt es keinen eindeutigen Kopf — wie sich das auf
  die Zeilenwerte auswirkt (leer lassen vs. Zeilen einer Variante zuordnen), ist Rueckfrage 3.
- **Kunde-Filter/-Spaltenfilter entfallen als Sonderweg:** der heutige Subquery-Join gegen
  `FaHierarchyOrderInfo` in `ProductionOrderRepository.BuildLeitstandQuery` wird ersetzt durch
  denselben Mechanismus, der heute schon fuer Datumsspalten gilt (`FaListDateColumnKeys`,
  „Datumsspalten in C# NACH der Termin-Berechnung filtern", ADR 0005): Kunde/Prio/AB-Nummer/
  Montage-Abteilung sind im hierarchischen Modus berechnete Zeilenwerte, kein SQL-Praedikat mehr —
  sie werden nach `MapItem` in C# gefiltert. Das betrifft `ProductionOrdersController` UND
  `PickingLeitstandController` (beide nutzen `BuildLeitstandQuery`).
- **Export:** diese Spec liefert nur die Datenbasis (Zeilenwerte im Anzeige-Modell); ein
  tatsaechlicher Export-Button ist nicht Teil des Umfangs.

### 2 — HauptFA wird eine normale Zeile

- Die Wurzelzeile der Struktur (Sub-FA mit `VaterFA = NULL`) traegt ihre Werte in den regulaeren
  Zeilenspalten wie jede andere Zeile — **sofern sie bereits heute eine materialisierte
  `ProductionOrder`-Zeile ist** (zu verifizieren, Rueckfrage 1; die Materialisierung filtert auf
  `SubFA != 0`, und die Wurzel hat vermutlich `SubFA` = ihre eigene BelID `!= 0`, ist also
  vermutlich bereits eine Zeile — aber unbewiesen, siehe Fallstricke §10 „Wurzel-Kopf ist
  `HauptArtnr`" als indirekter Beleg fuer die Existenz einer Wurzelzeile in `FaHierarchyNode`).
- Sie bekommt ihre eigenen Aktionen in der Zeile (welche genau — z. B. die heute in der
  Gruppen-Kopfzeile sitzenden Buttons — ist Teil von Rueckfrage 2) statt eines separaten
  Kopf-Widgets.
- **Kopfzeile wird schlanker:** nur noch HauptFA-Nummer, Sub-FA-Zahl-Badge, Mehrdeutig-Badge
  bleiben sicher bestehen. Ob die heutigen Kopfdaten-Badges (Kunde/Prio/Montage-Abt/AB-Nr/Termine,
  `Views/ProductionOrders/Index.cshtml` Zeilen ~131-167) ganz entfallen oder in reduzierter Form
  bleiben, ist Rueckfrage 2 — durch Punkt 1 erscheinen dieselben Werte ohnehin in der ersten
  sichtbaren Zeile.
- Der Kombigeraet-Sonderblock (`HeadVariants`-Tabelle, Zeilen ~170-205) bleibt unabhaengig von
  dieser Entscheidung bestehen, solange `IsAmbiguous` weiterhin „kein eindeutiger Kopf" bedeutet.

### 3 — Freigabe-Kaskade (Umkehr der Entscheidung in der Anzeige-Spec)

**Diese Spec widerruft ausdruecklich** die Festlegung in
[[2026-08-18-fa-liste-hierarchie-anzeige-spec]] (Abschnitt 8, „Praezisierte Antwort 3":
„`BulkRelease` (Freigabe) — **keine Kaskade**, bleibt zeilenbasiert und unveraendert"). Begruendung
(aus dem Backlog uebernommen): Eine Freigabe behauptet keine geleistete Arbeit, sondern erlaubt nur
den Beginn — ist das Geraet freigegeben, sind es seine Baugruppen fachlich zwingend auch. Das ist
dieselbe Logik, die die Fertigmeldungs-Kaskade bereits trägt, nur mit einer schwaecheren Aussage.

**Was NICHT widerrufen wird:** `BulkRelease` selbst (Mehrfachauswahl-Aktion) bleibt **unveraendert
zeilenbasiert** — die Kaskade ist eine **komplett neue, eigene Aktion** (`CascadeRelease`), kein
geaendertes Verhalten der bestehenden Aktion.

Konkrete Regeln (identisch zur Fertigmeldungs-Kaskade, wo sinnvoll uebertragen):

- **Traeger:** `ProductionOrderPickingStatus.IsReleasedForPicking`.
- **Kaskade ueber ALLE Nachfahren**, nicht nur direkte Kinder — technisch identisch zur
  Fertigmeldungs-Kaskade: alle `ProductionOrder`-Zeilen mit derselben `OrderNumber` (= HauptFA),
  weil die materialisierte Struktur bereits flach ueber `OrderNumber` gruppiert ALLE Nachfahren
  enthaelt (kein rekursiver Baum-Walk noetig, siehe `SetIsDoneBdeForOrderNumberAsync`-Praezedenz).
- **Atomar:** ein `SaveChangesAsync` fuer alle betroffenen Zeilen.
- **Bestaetigungsdialog mit Anzahl** vor der Ausfuehrung (`CascadeReleasePreview`, analog
  `CascadeDonePreview`): Gesamtzahl Sub-FAs, davon bereits freigegeben, davon neu freizugebende.
- **Bereits freigegebene Sub-FAs bleiben unveraendert** (kein Reset, keine beruehrten Audit-Felder)
  — nur `IsReleasedForPicking=false`-Zeilen werden gesetzt.
- **Bestaetigung nennt die Anzahl TATSAECHLICH neu freigegebener**, nicht die Gesamtzahl der Gruppe.
- **Keine Gruppen-Ruecknahme:** Zuruecknehmen bleibt ausschliesslich zeilenbasiert
  (`ToggleRelease`), analog zur Fertigmeldungs-Asymmetrie.
- **Zugriff:** `[RequireLeitstandAccess]` (wie `ToggleRelease`/`BulkRelease`/`SetPriority`), NICHT
  `[RequirePickingAccess]` (das ist das Fertigmeldungs-Recht) — Freigabe ist fachlich ein
  Leitstand-Vorgang.
- **Offene Detailfragen** (Picker-Zuweisungspflicht, Prioritaetsvergabe): Rueckfragen 5/6.

### 4 — Matchcode

- **Neue Spalte `ProductionOrder.Matchcode`** (string?, analog `ArticleNumber`), materialisiert aus
  `FaHierarchyNode.Matchcode` in `FaMaterializationSyncService` — Anlege- UND Update-Pfad in
  DERSELBEN Aenderung (F1-Regel der Materialisierungs-Spec, dasselbe Muster wie
  `ArticleNumber`/`Description1/2`). **Kein** Sonderfall, **keine** Z1-Ausnahme — `Matchcode` ist
  kein app-verwaltetes Feld, sondern eine reine Sage-Quell-Abbildung wie `ArticleNumber`.
- **Anzeige** in den sechs gesicherten FA-Zeilen-Ebene-Listen (siehe Erhebung oben), Position
  „eigene Spalte neben Artikelnummer/Bezeichnung 1", `Locked: false`, ueber das Zahnrad
  steuerbar — **beide** Stellen pflegen: `ColumnDefinitions.cs` UND das inline `#column-config`
  der jeweiligen View (Fallstricke §10, Regel „neue `<th data-col-key>` immer in BEIDEN Stellen").
- **Hausweit, kein `defaultHidden`-Unterschied nach Standort** [ENTSCHIEDEN 2026-09-10, aus dem
  Backlog uebernommen]: Matchcode ist eine normale Spalte in beiden Modi. Fuer AKE bleibt sie
  vorerst leer (Rueckfrage 8 klaert, ob/wie sie befuellt wird), aber sie ist **sichtbar
  vorhanden**, nicht versteckt.
- **F7-Regel (leeres Feld darf Filter/Sortierung nicht ins Leere laufen lassen):** solange AKE die
  Spalte nicht befuellt, muss ein Spaltenfilter auf „Matchcode" fuer AKE-Zeilen entweder alle
  Zeilen als „kein Treffer" behandeln (korrekt, weil leer) oder die Spalte fuer AKE ausblenden —
  **keine** Sonderbehandlung, die stillschweigend eine leere Liste erzeugt ohne erkennbaren Grund.
  Da die Spalte laut Entscheidung hausweit gleich behandelt wird, gilt: leer bleibt leer, Filter
  liefert dafuer korrekt keine Treffer, keine Ausnahme im Filter-Code noetig.
- **AKE-Quelle ungeklaert, moegliche externe Abhaengigkeit** (Rueckfrage 8): Vor Umsetzungsbeginn
  zu pruefen, ob `vw_AKE_Kommissionierung_WAListe`/`_StuecklistenDB` den Matchcode bereits liefern.
  Falls nicht, ist die Sage-View-Erweiterung ein Fremd-DB-Objekt und damit dieselbe Abhaengigkeitsklasse
  wie die IDEAL-Views — nicht durch einen Dev-Lauf loesbar, sondern mit der Sage-Betreuung
  abzustimmen. Diese Spec blockiert NICHT darauf: Spalte/Materialisierung/Anzeige werden gebaut
  und fuer IDEAL sofort befuellt, AKE bleibt bekannte Zwischeneinschraenkung bis die Quelle steht.

## Geparkte Fragen (aus dem Backlog uebernommen, hier nur festgehalten — nicht entschieden)

1. **B-0/Ruling 4 — Vollstruktur in der Vorbau-Stueckliste einer HauptFA.** Konkrete Pruefrage:
   erscheint dasselbe Material dadurch auf ZWEI Kommissionierlisten (Sub-FA-Vorbauliste UND
   HauptFA-Vollstruktur)? Am Bildschirm zu testen (nicht am Ausdruck, dem fehlen die relevanten
   Spalten), Ergebnis entscheidet ueber Rueckbau (Einzeiler in der Scope-Regel) oder
   Druck-Whitelist-Erweiterung in `PrintBom.cshtml` um Ebene und Komm.-Ziel. **Muss laut Backlog vor
   dem Merge geklaert sein** — Bezug: [[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]].
2. **`HasCoatingParts`/zweite Z1-Ausnahme.** Backlog-Frage: faellt `PickingStatus` durch den
   F6-Fix aus der app-verwalteten Z1-Liste des `FaMaterializationSyncService`? **Rechercheergebnis
   dieses Spec-Laufs:** Im Worktree-Stand ist das bereits umgesetzt — der Klassenkommentar von
   `FaMaterializationSyncService` (Zeilen 22-32) nennt explizit zwei Ausnahmen:
   `ProductionOrder.ProductionWorkplaceId` (Werkbank) und
   `ProductionOrderPickingStatus.HasCoatingParts` (F5/F6), inkl. Begruendung und Verweis auf
   `SetCoatingPartsAsync`. Die offene Restfrage ist nur noch die **Bestaetigung**, dass dieser
   Stand nach Abschluss von Schranke 2 der Materialisierungs-Spec (noch nicht gemergt) unveraendert
   bleibt — kein neuer Handlungsbedarf für diese Spec erkennbar, aber vom Menschen zu bestaetigen.

## Danach: Folgearbeit (nicht Teil dieser Spec)

Replikation von Kunde/Termine je Zeile auf Leitstand, Tracking, Picking, FaWorklist, FaCompletion —
bewusst NACH dieser Spec. Mit der Zeilenquelle aus Punkt 1 dieser Spec wird die Replikation
einfacher (dieselbe Zeilenquelle statt einer Kopfzeilen-Sonderloesung je View). Als eigene Aufgabe
in `secondbrain/aufgaben/` festzuhalten, sobald diese Spec umgesetzt und beobachtet ist.

## Technischer Loesungsentwurf

**Muster:** Repository-Pattern (ADR 0001), Listen-View-Pattern mit Server-Spaltenfilter (ADR 0005),
F1-Regel (Anlege-/Update-Pfad gemeinsam, aus der Materialisierungs-Spec uebernommen).

- **Matchcode-Materialisierung:** `FaMaterializationPlanner.MaterializationSourceOrder` um
  `Matchcode` (string?) erweitern (reiner Record, kein DB-Zugriff); `FaMaterializationSyncService.
  RunAsync` setzt `newOrder.Matchcode = n.Matchcode` im Anlege-Block UND `o.Matchcode = sn.Matchcode`
  im Update-Block — exakt dasselbe Muster wie `ArticleNumber`/`Description1/2` direkt daneben,
  keine neue Sonderlogik.
- **Kopfdaten je Zeile:** `MapItem` in `ProductionOrdersController.Index` bekommt zusaetzliche
  optionale Parameter (`groupCustomer`, `groupPrio`, `groupAbNummer`, `groupMontageAbteilung`,
  `groupKonstruktionsTermin`), analog den bestehenden `groupProductionDate`/`groupDeliveryDate`/
  `groupCoatingStart`-Parametern; der Aufrufer im hierarchischen Zweig (Zeile ~244-249) reicht sie
  aus derselben bereits geladenen `heads`/`group`-Struktur durch (kein zusaetzlicher Datenbankzugriff,
  kein Fan-out). AKE-Aufrufstelle (flacher Zweig) uebergibt weiterhin nichts → bit-identisches
  Verhalten (F4-Praezedenz).
- **„Nur wo der Wert wechselt":** Berechnung NACH Sortierung, serverseitig in
  `Views/ProductionOrders/_ProductionOrderRow.cshtml`/im Controller (Vergleich zur Vorzeile
  innerhalb derselben Gruppe) — Rueckfrage 4 klaert die genaue Stelle; **kein** clientseitiges
  Verstecken per JS, weil das mit Server-Spaltenfiltern/-Sortierung (ADR 0005 Server-Mode)
  kollidieren wuerde (bereits dokumentierter Fallstrick „Server-Filter-Mode: kein clientseitiges
  `applyFilters()` beim Init").
- **Kunde-Postfilter:** `ProductionOrdersController.Index` erweitert die bestehende
  `FaListDateColumnKeys`/`hasDateFilters`-Verzweigung um `customer`/`prio`/`ab-nummer`/
  `montage-abteilung` als weitere „nach C#-Berechnung filtern"-Keys (analog den Datumsspalten);
  `ProductionOrderRepository.BuildLeitstandQuery`/`BuildCustomerColumnFilterPredicate` verlieren
  dadurch ihren hierarchischen Sonderfall und werden fuer den `customer`-Filter im hierarchischen
  Modus nicht mehr aufgerufen (das SQL-Praedikat bleibt nur fuer den flachen AKE-Fall bestehen,
  F4-Praezedenz). Gleiche Umstellung in `PickingLeitstandController.Index`, weil beide Controller
  dieselbe Repository-Methode teilen.
- **Freigabe-Kaskade:** neue `IProductionOrderPickingStatusRepository.
  SetReleaseForOrderNumberAsync(orderNumber, releasedBy, modifiedBy, modifiedByWindows)` —
  Zwei-Schritt-Query (IDs lokal, dann `Contains`, InMemory-testbar, analog
  `SetIsDoneBdeForOrderNumberAsync`), filtert zusaetzlich auf `IsReleasedForPicking == false`,
  vergibt `PickingPriority` fortlaufend ab `MAX+1` (wiederverwendet die Logik aus
  `GetMaxPickingPriorityAsync`), EIN `SaveChangesAsync`. `PickingLeitstandController` bekommt
  `CascadeReleasePreview`(GET)/`CascadeRelease`(POST), Route/Namensgebung/Bestaetigungsdialog-Markup
  1:1 nach dem Vorbild `CascadeDonePreview`/`CascadeDone` (`Views/PickingLeitstand/Index.cshtml`
  Zeilen ~344-383), aber mit `[RequireLeitstandAccess]` statt `[RequirePickingAccess]` und dem
  Button sichtbar nur bei `Model.CanManagePickingRelease` (dieselbe Sichtbarkeitsbedingung wie der
  bestehende `BulkRelease`-Mechanismus), nicht bei `Model.CanPick`.
- **Matchcode-Anzeige:** `ColumnDefinitions.cs` bekommt je betroffenem `viewKey` einen neuen
  `ColumnDef("matchcode", "Matchcode", Locked: false)`, platziert direkt nach `article-number`
  bzw. vor `description1`; jede betroffene View bekommt dieselbe Spalte im inline
  `#column-config`-Block UND ein `<th data-col-key="matchcode">` UND die Zellen-Befuellung in der
  jeweiligen Row-Partial.

## Migrations-/SQL-Auswirkungen

**Migration kommt zurueck** (`deploy.migration: true`, im Unterschied zur Materialisierungs-Spec,
die final `false` war): neue Spalte `ProductionOrder.Matchcode` (NVARCHAR, nullable, keine
Laengenbegrenzung ueber `[StringLength]` hinaus noetig — analog `ArticleNumber` `[StringLength(100)]`
oder grosszuegiger, da Matchcodes laut FA-Struktur-Ansicht teils laenger sein koennen; exakte Laenge
am ersten echten Datenlauf zu pruefen).

Ablauf gemaess CLAUDE.md/ADR 0004: Model (`ProductionOrder.Matchcode`) → `dotnet ef migrations add
AddProductionOrderMatchcode --project IdealAkeWms` → idempotentes `SQL/91_AddProductionOrderMatchcode.sql`
mit `OBJECT_ID`/`COL_LENGTH`-Guard (DDL in eigenem Batch) → `__EFMigrationsHistory`-Insert in
separatem Batch → `SQL/00_FreshInstall.sql` an **beiden** Stellen (Schema-Objekt in der
`ProductionOrder`-Tabelle UND `MigrationId` im Insert-Block am Ende). Additiv, kein Datenumbau, kein
Backfill-Risiko fuer Bestandszeilen (die fuellen sich wie jedes andere K2-Feld erst durch den
naechsten Materialisierungs-Lauf — F2-Praezedenz der Materialisierungs-Spec: nach dem Deploy einen
Lauf abwarten, dann pruefen).

Keine weiteren Schema-Aenderungen: Kopfdaten-je-Zeile (Punkt 1) bleiben nach der K1-Regel
unmaterialisiert (reine Anzeige-Modell-Anreicherung wie `ProductionDate`/`DeliveryDate` heute
schon), Freigabe-Kaskade (Punkt 3) nutzt ausschliesslich die bestehende
`ProductionOrderPickingStatus`-Tabelle.

## Audit-Feld-Auswirkungen

`ProductionOrder` bleibt `AuditableEntity`; jede vom Materialisierungs-Sync geschriebene Zeile
(Anlegen und Update, inkl. des neuen `Matchcode`-Felds) setzt weiterhin `ModifiedAt`/`ModifiedBy`/
`ModifiedByWindows` auf den Service-Namen — unveraendert durch diese Spec, nur ein zusaetzliches
Feld in der bestehenden Zuweisung.

Die Freigabe-Kaskade schreibt `ProductionOrderPickingStatus`-Zeilen (kein `AuditableEntity`, hat
aber eigene `ModifiedAt`/`ModifiedBy`/`ModifiedByWindows`-Felder analog dem bestehenden
`SetReleaseBatchAsync`-Muster) — **nur fuer tatsaechlich geaenderte Zeilen**, bereits freigegebene
Zeilen werden nicht beruehrt (auch ihre Audit-Felder nicht), das ist Teil der Fachanforderung
„bereits freigegeben bleibt unveraendert".

## Betroffene Rollen / Zugriffsfilter

- **Neue Aktionen `CascadeReleasePreview`/`CascadeRelease`:** `[RequireLeitstandAccess]` — dieselbe
  Rolle wie `ToggleRelease`/`BulkRelease`/`SetPriority`/`ChangeAssignedPicker`. **Keine** neue Rolle,
  **kein** neuer Filter — nur eine weitere Action unter einem bestehenden Attribut. Damit entfaellt
  der sonst faellige Dreifach-Eintrag (Attribut/`controller.md`/`RoleOverview.cshtml`) fuer eine
  komplett neue Rolle; zu ergaenzen ist lediglich die neue Action in
  `secondbrain/codebase/controller.md` (bestehende Rolle, neue Route).
- **Kopfdaten-je-Zeile/HauptFA-Zeile/Matchcode-Anzeige:** keine Aenderung an bestehenden
  Zugriffsfiltern — reine Datenanreicherung in bereits geschuetzten Ansichten
  (`RequirePickingOrTrackingOrLeitstandAccess` auf `ProductionOrdersController`, unveraendert).

## Listen-View-Pattern-Pflichten (ADR 0005)

- **Matchcode-Spalte:** volle ADR-0005-Pflicht in jeder betroffenen Liste — eigener `data-col-key`,
  `ColumnDefinitions.cs`-Eintrag UND inline `#column-config` (Fallstricke §10, beide Stellen sind
  Pflicht, sonst 400 bei den Spaltenpraeferenzen bzw. stille Fehlplatzierung beim Re-Append).
  Server-seitiger Spaltenfilter auf Matchcode ist ein normaler String-Filter (`EF.Functions.Like`
  vermeiden wegen InMemory-Tests, Expression-Tree-Pattern wie die uebrigen Textspalten).
- **Kopfdaten-je-Zeile-Spalten (nur `ProductionOrders`):** ebenfalls volle ADR-0005-Pflicht, aber
  mit der Besonderheit, dass Kunde/Prio/AB-Nummer/Montage-Abteilung/Konstruktions-Termin im
  hierarchischen Modus **berechnete** Werte sind (siehe Technischer Loesungsentwurf) — Filter auf
  diesen Spalten laufen wie die bestehenden Datumsspalten NACH der Berechnung in C#, nicht als
  SQL-Praedikat. Das ist eine bereits etablierte, ADR-0005-konforme Ausnahme (Server-Mode bleibt
  gewahrt, nur die Filteranwendung verschiebt sich serverseitig nach C#).
- **Freigabe-Kaskade:** keine neue Tabellenspalte, kein Listen-View-Pattern-Bezug — reine
  Massenaktion analog `CascadeDone`.

## Akzeptanzkriterien

1. **Matchcode materialisiert.** Nach einem Materialisierungs-Lauf zeigt `ProductionOrder.Matchcode`
   fuer IDEAL-Sub-FAs den Wert aus `FaHierarchyNode.Matchcode` — sowohl fuer neu angelegte als auch
   fuer zuvor bereits materialisierte Bestandszeilen (F1/F2-Nachweis wie in der
   Materialisierungs-Spec).
2. **Matchcode-Spalte sichtbar und filterbar** in allen sechs FA-Zeilen-Ebene-Listen aus der
   Erhebung, ueber das Zahnrad ein-/ausblendbar, Position neben Artikelnummer/Bezeichnung 1.
3. **AKE-Regression:** bei Master `false` bzw. auf einer AKE-Instanz ist `Matchcode` leer (kein
   Materialisierungsversuch, kein Fehler); Spaltenfilter auf einer durchgehend leeren
   Matchcode-Spalte liefert korrekt „kein Treffer" statt eines Fehlers oder einer stillen leeren
   Liste ohne erkennbaren Grund (F7-Nachweis).
4. **Kopfdaten je Zeile vorhanden.** Jede Sub-FA-Zeile der FA-Liste traegt im Anzeige-Modell Kunde,
   Termine, Prio, AB-Nummer, Montage-Abteilung als eigene Werte (nicht nur die Gruppe) — nachweisbar
   z. B. durch einen Spaltenfilter auf „Kunde", der Treffer auf Zeilenebene liefert, ohne dass
   `ProductionOrder.Customer` in der DB befuellt ist.
5. **Anzeige „nur wo der Wert wechselt" korrekt.** Zwei aufeinanderfolgende Zeilen derselben Gruppe
   mit identischem Kunden zeigen den Kunden nur in der ersten der beiden Zeilen; wechselt der Wert
   (z. B. bei einem Kombigeraet mit unterschiedlichen Montage-Abteilungen je Variante), erscheint er
   erneut.
6. **Kunde-Filter funktioniert weiterhin ohne Sonderweg-Join.** Freitext- und Spaltenfilter auf
   „Kunde" liefern im hierarchischen Modus weiterhin korrekte Treffer, obwohl
   `ProductionOrder.Customer` je Zeile leer bleibt — nachweisbar durch denselben Testfall wie in der
   Materialisierungs-Spec (TS-71-Referenz), jetzt aber ueber den C#-Postfilter statt den
   SQL-Subquery-Join.
7. **HauptFA-Zeile vorhanden und erkennbar.** Sofern Rueckfrage 1 bestaetigt, dass die Wurzelzeile
   bereits materialisiert ist: die HauptFA-eigene Zeile ist in der FA-Liste sichtbar und traegt die
   gemaess Rueckfrage 2 festgelegten Aktionen.
8. **Kopfzeile schlanker.** Die Gruppen-Kopfzeile zeigt nach dieser Aenderung mindestens
   HauptFA-Nummer, Sub-FA-Zahl-Badge und Mehrdeutig-Badge; die uebrigen Kopfdaten-Badges sind gemaess
   Rueckfrage 2 entfernt oder reduziert.
9. **Freigabe-Kaskade kaskadiert korrekt.** „Alle Sub-FAs freigeben" am HauptFA setzt
   `IsReleasedForPicking=true` fuer ALLE Nachfahren-Sub-FAs dieser `OrderNumber` (nicht nur direkte
   Kinder), atomar (ein `SaveChangesAsync`), mit Bestaetigungsdialog, der die Gesamtzahl UND die
   Anzahl tatsaechlich neu freizugebender Sub-FAs zeigt.
10. **Freigabe-Kaskade ruehrt bereits Freigegebene nicht an.** Eine Sub-FA, die vor der Kaskade
    bereits `IsReleasedForPicking=true` war, bleibt danach unveraendert (Wert UND Audit-Felder).
11. **Freigabe-Kaskade meldet die korrekte Anzahl.** Die Erfolgsmeldung nennt die Anzahl
    TATSAECHLICH neu freigegebener Sub-FAs, nicht die Gesamtzahl der Gruppe.
12. **Keine Gruppen-Ruecknahme.** Es gibt keine Aktion, die die Freigabe fuer eine ganze HauptFA-Gruppe
    zurueckzieht; `ToggleRelease` bleibt die einzige Ruecknahme-Aktion, zeilenbasiert.
13. **`BulkRelease` unveraendert.** Die bestehende Mehrfachauswahl-Freigabe verhaelt sich nach dieser
    Spec bit-identisch zum Vorzustand — kein kaskadierendes Verhalten, keine neue Option.
14. **Zugriffsschutz korrekt.** `CascadeReleasePreview`/`CascadeRelease` sind ohne Leitstand-Recht
    nicht aufrufbar (403/Redirect wie bei den anderen `RequireLeitstandAccess`-Actions); der
    Freigeben-Button erscheint in der Oberflaeche nur bei `CanManagePickingRelease`.
15. **[[2026-08-18-fa-liste-hierarchie-anzeige-spec]] nachgezogen.** Die dortige Festlegung „`BulkRelease`
    ist Freigabe, nicht Fertigmeldung — keine Kaskade" ist um die neue Aktion `CascadeRelease`
    ergaenzt/widerrufen dokumentiert (Statusabschnitt/Tabelle Abschnitt 8), sodass sich die beiden
    Specs nicht mehr widersprechen.

## Test-Szenarien

Neues Kapitel „IDEAL — FA-Liste-Ausbau: Zeilenwerte, HauptFA-Zeile, Freigabe-Kaskade, Matchcode"
in `docs/TESTSZENARIEN.md` (voraussichtlich TS-72, Nummer final durch den qa-agent):

- **Matchcode-Materialisierung:** Materialisierungs-Lauf gegen eine IDEAL-Sub-FA mit bekanntem
  `FaHierarchyNode.Matchcode` → `ProductionOrder.Matchcode` befuellt, sowohl fuer eine neu
  angelegte als auch eine bereits bestehende Zeile (Update-Pfad).
- **Matchcode-Anzeige/-Filter:** Spalte ueber das Zahnrad einblenden in mindestens zwei der sechs
  betroffenen Listen → Wert sichtbar, Server-Spaltenfilter auf einen bekannten Matchcode liefert
  korrekte Treffer.
- **AKE-Regression Matchcode:** Master `false` bzw. AKE-Instanz → Spalte vorhanden (kein
  `defaultHidden`-Unterschied), aber durchgehend leer; Filter auf der leeren Spalte liefert „kein
  Treffer", kein Fehler, keine kaputte Sortierung.
- **Kopfdaten je Zeile:** HauptFA mit mehreren Sub-FAs oeffnen → Kunde/Termine/Prio/AB-Nummer/
  Montage-Abteilung sind auf jeder Zeile im Anzeige-Modell vorhanden (z. B. per Spaltenfilter
  nachweisbar), aber nur bei Wertwechsel sichtbar dargestellt.
- **Kunde-Filter-Regression (Postfilter statt Join):** Freitext- und Spaltenfilter „Kunde" im
  hierarchischen Modus → weiterhin Treffer, identisch zum TS-71-Testfall der Materialisierungs-Spec,
  jetzt ueber den C#-Postfilter.
- **HauptFA-Zeile:** FA-Liste im hierarchischen Modus oeffnen → die Wurzelzeile ist als eigene Zeile
  sichtbar und von den Sub-FA-Zeilen unterscheidbar; ihre Aktionen sind direkt in der Zeile bedienbar.
- **Kopfzeile schlanker:** Gruppen-Kopfzeile zeigt nach der Aenderung nur noch die gemaess Rueckfrage
  2 festgelegten Elemente.
- **Freigabe-Kaskade — Normalfall:** HauptFA mit z. B. 10 noch nicht freigegebenen Sub-FAs → „Alle
  Sub-FAs freigeben" → Bestaetigungsdialog zeigt 10 → nach Bestaetigung: alle 10
  `IsReleasedForPicking=true`, Erfolgsmeldung nennt 10.
- **Freigabe-Kaskade — teilweise bereits freigegeben:** HauptFA mit 10 Sub-FAs, davon 3 bereits
  freigegeben → Dialog zeigt Gesamt 10, davon 3 bereits freigegeben, 7 werden neu freigegeben →
  nach Ausfuehrung: Erfolgsmeldung nennt 7, die 3 bereits freigegebenen sind unveraendert (Audit-Felder
  gegenpruefen).
- **Freigabe-Kaskade — keine Gruppen-Ruecknahme:** nach einer Kaskade gibt es keine Bedienoberflaeche,
  die die ganze Gruppe zurueckzieht; eine einzelne Zeile lässt sich weiterhin per `ToggleRelease`
  zurueckziehen.
- **`BulkRelease`-Regression:** Mehrfachauswahl-Freigabe verhaelt sich unveraendert zeilenbasiert,
  keine Kaskade ausgeloest.
- **Zugriffsschutz:** Ein Benutzer ohne Leitstand-Recht sieht den „Alle Sub-FAs freigeben"-Button
  nicht bzw. ein direkter POST auf `CascadeRelease` wird abgewiesen.
- **F1/F2-Regression (Matchcode):** wie bei den uebrigen K2-Feldern — Materialisierungs-Lauf gegen
  Bestandsdaten befuellt Matchcode ohne manuellen Eingriff.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen.

## Deploy

**Provisorisch vom Spec-Agent geschaetzt — der qa-agent bestaetigt/korrigiert das gegen den echten
Diff, analog dem Vorgehen bei der Materialisierungs-Spec.**

- **Web-App:** ja (`deploy.web: true`) — `ProductionOrdersController`, `PickingLeitstandController`,
  `ProductionOrderRepository`/`IProductionOrderRepository`, `ProductionOrderPickingStatusRepository`/
  `IProductionOrderPickingStatusRepository`, `ProductionOrderListViewModel`, `ColumnDefinitions.cs`,
  diverse Views (`ProductionOrders/Index.cshtml` + `_ProductionOrderRow.cshtml`,
  `PickingLeitstand/Index.cshtml`, ggf. weitere Row-Partials je Erhebungsergebnis),
  `Views/Help/Changelog.cshtml` + `Index.cshtml`, `AppVersion.cs` (voraussichtlich 1.38.0).
- **Service:** ja (`deploy.service: true`) — `FaMaterializationSyncService`,
  `FaMaterializationPlanner`, `AppVersion.cs` (1.38.0).
- **Migration:** ja (`deploy.migration: true`) — `Migrations/<neu>_AddProductionOrderMatchcode.cs`,
  `SQL/91_AddProductionOrderMatchcode.sql`, `SQL/00_FreshInstall.sql` (Schema + MigrationId).
  Additiv, kein Backup-Zwang ueber die uebliche Deploy-Vorsicht hinaus (keine daten-destruktive
  Aenderung).
- **Deploy-Vorbedingung:** keine neue externe Abhaengigkeit fuer IDEAL. Falls Rueckfrage 8 ergibt,
  dass die AKE-Sage-View erweitert werden muss, ist DAS eine separate, extern abzustimmende
  Aenderung mit eigenem Zeitplan — blockiert NICHT den Deploy dieser Spec (AKE bleibt leer, siehe
  F7-Regel).
- **Nach dem Deploy:** einen Materialisierungs-Lauf abwarten (max. 15 Minuten), danach Matchcode-
  Spalte und Kopfdaten-je-Zeile gegenpruefen — nicht annehmen, dass Bestandszeilen sofort befuellt
  sind (F2-Praezedenz).
- **Ablauf (Mensch):** Worktree publizieren → Testsystem → manueller Test (inkl. B-0/Ruling-4-Test,
  geparkte Frage 1, VOR dem Merge) → dann Merge. Teil des ungemergten Buendel-Worktrees
  `.claude/worktrees/2026-08-07-ideal-teile-1-5` — Schranke 2 gilt fuer das gesamte Buendel, ein
  Merge.
- **Publish-Befehle (im Worktree):**
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```

## Offene Rueckfragen

1. Ist die Wurzelzeile (`FaHierarchyNode` mit `VaterFA = NULL`) bereits heute eine materialisierte
   `ProductionOrder`-Zeile (`SubOrderNumber` der Wurzel == `OrderNumber`)? Das ist Voraussetzung
   fuer Punkt 2 (HauptFA als normale Zeile). Falls nein: braucht es eine zusaetzliche
   Materialisierungs-Anpassung, die dann VOR dieser Spec bzw. als deren Teil zu klaeren waere.
2. Welche der heutigen Kopfzeilen-Badges (Kunde/Prio/Montage-Abteilung/AB-Nummer/Termine) entfallen
   komplett, welche bleiben zusaetzlich zur neuen Zeilen-Darstellung bestehen?
3. Zeilen-Darstellung bei Kombigeraeten (`IsAmbiguous=true`): zeigt jede Sub-FA-Zeile eine eigene
   Kopfvariante (und woher kaeme die Zuordnung Zeile-zu-Variante), oder bleiben die K1-Zeilenspalten
   dort grundsaetzlich leer wie bisher die Gruppen-Kopfzeile?
4. „Nur wo der Wert wechselt": serverseitig in Razor/Controller nach Sortierung berechnet oder
   clientseitig per JS? Wie verhaelt sich die Regel in Kombination mit Server-Spaltenfilter/
   -Sortierung, die die Zeilenreihenfolge veraendert?
5. Freigabe-Kaskade: greift die bestehende Pflicht „Kommissionierer zuweisen"
   (`KommissionierungMitZuweisung`) analog `BulkRelease`, mit EINEM Picker fuer alle kaskadierten
   Sub-FAs? Oder laeuft die Kaskade ohne Picker-Zuweisung, die dann je Sub-FA separat nachgetragen
   wird?
6. Freigabe-Kaskade: sollen neu freigegebene Sub-FAs eine fortlaufende `PickingPriority` bekommen
   (wie `SetReleaseBatchAsync`), oder bleibt die Prioritaet bei der Kaskade unbelegt?
7. Matchcode-Scope: gilt diese Spec nur fuer die FA-Zeilen-Ebene (`ProductionOrder`, sechs
   Standardlisten) oder auch fuer die BOM-Komponentenzeile (Picking/Bom, PrintBom, PrintPicking,
   MissingParts, WarehouseRequisitions)? Letztere haengt an `FaHierarchyBomItem`/`BomItem`, nicht an
   `ProductionOrder` — eine andere, hier nicht gesicherte Erweiterung waere noetig.
8. AKE-Quelle Matchcode: liefert `vw_AKE_Kommissionierung_WAListe`/`_StuecklistenDB` den Matchcode
   bereits? Falls nein: die Sage-View-Erweiterung ist eine externe Abhaengigkeit, VOR
   Umsetzungsbeginn mit der Sage-Betreuung abzustimmen, nicht anzunehmen.
9. Matchcode-Spalte `defaultHidden` ja/nein je Liste — hausweit einheitlich laut Entscheidung
   2026-09-10, aber der konkrete Default (sichtbar vs. ausgeblendet) innerhalb dieser Vorgabe ist
   noch offen.
10. [Geparkt aus dem Backlog, unveraendert] B-0/Ruling 4 — Vollstruktur in der Vorbau-Stueckliste
    einer HauptFA: erscheint dasselbe Material auf zwei Kommissionierlisten? Am Bildschirm zu
    testen, Ergebnis entscheidet ueber Rueckbau vs. Druck-Whitelist-Erweiterung in `PrintBom.cshtml`.
    Muss vor dem Merge geklaert sein.
11. [Geparkt aus dem Backlog, mit Rechercheergebnis] `HasCoatingParts`/zweite Z1-Ausnahme: dieser
    Spec-Lauf hat verifiziert, dass F5/F6 im Worktree-Stand bereits umgesetzt sind (Klassenkommentar
    nennt beide Ausnahmen). Offen ist nur die Bestaetigung, dass dieser Stand nach Abschluss der
    (noch ausstehenden) Schranke 2 der Materialisierungs-Spec unveraendert bleibt.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. →
2. →
3. →
4. →
5. →
6. →
7. →
8. →
9. →
10. →
11. →
