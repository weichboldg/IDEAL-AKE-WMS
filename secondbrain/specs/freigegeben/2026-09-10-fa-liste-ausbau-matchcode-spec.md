---
type: spec
title: "FA-Liste ausbauen: Zeilenwerte, HauptFA-Zeile, Freigabe-Kaskade, Matchcode"
slug: 2026-09-10-fa-liste-ausbau-matchcode-spec
status: InUmsetzung
created: 2026-09-10
updated: 2026-09-10
source_backlog: "[[2026-09-10-fa-liste-ausbau-matchcode]]"
task: "[[2026-09-10-fa-liste-ausbau-matchcode]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - "IdealAkeWms/Models/ProductionOrder.cs (neue Spalte `Matchcode` (string?, NVARCHAR), analog `ArticleNumber`/`Description1`)"
  - "IDEALAKEWMSService/Services/FaMaterializationSyncService.cs + FaMaterializationPlanner.cs (`Matchcode` aus `FaHierarchyNode.Matchcode` in Anlege- UND Update-Zweig setzen, F1-Regel — gleiches Muster wie `ArticleNumber`/`Description1/2`, kein Sonderfall, keine Z1-Ausnahme noetig, da nicht app-verwaltet)"
  - "IdealAkeWms/Migrations/<neu>_AddProductionOrderMatchcode.cs + SQL/91_AddProductionOrderMatchcode.sql (OBJECT_ID/COL_LENGTH-Guard, additiv) + SQL/00_FreshInstall.sql (Schema-Objekt UND MigrationId)"
  - "IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs (`ProductionOrderListItem`: neue Properties `Matchcode`, `KonstruktionsTermin`, `Prio`, `AbNummer`, `MontageAbteilung` — `Customer`/`ProductionDate`/`DeliveryDate` existieren bereits auf der Zeile, werden aber im hierarchischen Modus bisher NICHT aus den K1-Gruppenwerten befuellt; `ProductionOrderListGroup` ggf. um `IsHauptFaRow`-Kennzeichnung je Item statt eigenem Feld auf der Gruppe)"
  - "IdealAkeWms/Controllers/ProductionOrdersController.cs (`MapItem`: zusaetzliche Parameter fuer K1-Zeilenwerte aus der bereits geladenen `heads`/`group`-Struktur je Zeile setzen statt nur auf Gruppenebene; Kopfzeile-Badges reduzieren auf HauptFA-Nummer/Sub-FA-Zahl/Kunde/Fert.-Termin/Mehrdeutig-Badge [Antwort 2]; HauptFA-Zeile markieren [Antwort 1, ERST nach dem Code-Nachweis, dass die Wurzel materialisiert wird]; Kunde/Prio/AB-Nummer/Montage-Abteilung/Konstruktions-Termin als C#-Postfilter analog dem bestehenden `FaListDateColumnKeys`/`hasDateFilters`-Zweig behandeln, Kunde-Join-Sonderweg in der Query entfaellt dadurch fuer die FA-Liste)"
  - "IdealAkeWms/Controllers/PickingLeitstandController.cs (dieselbe Kunde-Postfilter-Umstellung, da `BuildLeitstandQuery`/`GetForLeitstandGroupedAsync` gemeinsam mit `ProductionOrdersController` genutzt wird, siehe bereits bestehende Notiz in der Materialisierungs-Spec 'betrifft auch PickingLeitstandController'; NEU: `CascadeReleasePreview`/`CascadeRelease`-Actions analog `CascadeDonePreview`/`CascadeDone`, aber `[RequireLeitstandAccess]` statt `[RequirePickingAccess]` — Freigabe ist ein Leitstand-Recht wie `ToggleRelease`/`BulkRelease`, nicht ein Picking-Recht wie die Fertigmeldungs-Kaskade. `CascadeRelease` uebernimmt die bestehende Zuweisungspflicht UNVERAENDERT: dieselbe harte Pruefung wie `ToggleRelease`/`BulkRelease` in Z. 346-351/391-396 — ist `KommissionierungMitZuweisung` aktiv und kein Kommissionierer uebergeben, wird abgewiesen, nicht durchgelassen)"
  - "IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs + IProductionOrderRepository.cs (Kunde-Subquery-Sonderweg in `BuildLeitstandQuery`/`BuildCustomerColumnFilterPredicate` entfernen zugunsten des C#-Postfilters — der `filterCustomer`-Freitextfilter UND der `customer`-Spaltenfilter laufen im hierarchischen Modus dann wie jeder andere on-the-fly berechnete Wert)"
  - "IdealAkeWms/Data/Repositories/IProductionOrderPickingStatusRepository.cs + ProductionOrderPickingStatusRepository.cs (neue `Task<int> SetReleaseForOrderNumberAsync(string orderNumber, string? releasedBy, string? assignedPicker, string modifiedBy, string modifiedByWindows)` — `assignedPicker` ist Pflicht-Parameter der Signatur, aber nullable, weil er nur bei aktivem `KommissionierungMitZuweisung` gefuellt ist [Antwort zu B2]; setzt `IsReleasedForPicking=true` NUR fuer aktuell `false`-Zeilen dieser `OrderNumber`-Gruppe und setzt denselben `assignedPicker` auf ALLE diese Zeilen, vergibt fortlaufende `PickingPriority` ab `MAX+1` analog `SetReleaseBatchAsync` in der Reihenfolge `SubOrderNumber` AUFSTEIGEND [Antwort 6, Z2 — sonst entscheidet die zufaellige DB-Lesereihenfolge], EIN `SaveChangesAsync` fuer Atomaritaet [analog `SetIsDoneBdeForOrderNumberAsync`], gibt die Anzahl TATSAECHLICH geaenderter Zeilen zurueck, ruehrt bereits freigegebene Zeilen nicht an [auch nicht `ModifiedAt`, auch nicht deren `AssignedPicker`])"
  - "IdealAkeWms/Views/PickingLeitstand/Index.cshtml (Gruppen-Kopfzeile: neuer Button 'Alle Sub-FAs freigeben' analog `btn-cascade-done`, eigenes Bestaetigungs-Modal `cascadeReleaseModal` analog `cascadeDoneModal`; bei aktivem `KommissionierungMitZuweisung` ist die Kommissionierer-Auswahl im Modal PFLICHT — genau wie bei `ToggleRelease`/`BulkRelease` — und das Modal benennt die Massenwirkung im Klartext: 'Der gewaehlte Kommissionierer wird allen N Sub-FAs zugewiesen und kann danach je Sub-FA geaendert werden.'; `BulkRelease`-Modal/-Mechanismus bleibt UNVERAENDERT zeilenbasiert)"
  - "IdealAkeWms/Views/ProductionOrders/Index.cshtml (Gruppen-Kopfzeile schlanker, aber NICHT leer: HauptFA-Nummer, Sub-FA-Zahl, **Kunde**, **ein Leittermin (Fert.-Termin)**, Mehrdeutig-Badge bleiben [Antwort 2/B4 — bei zugeklappter Gruppe ist die Kopfzeile das Einzige, was sichtbar ist]; es wandern nur Prio, AB-Nummer, Montage-Abteilung und die uebrigen Termine in die Zeilen; neue Zeilenspalten Kunde/Termine/Prio/AB-Nummer/Montage-Abteilung/Matchcode mit 'nur wo der Wert wechselt'-Darstellung; HauptFA-Zeile visuell/durch eigene Aktionen markiert [Antwort 1])"
  - "IdealAkeWms/Views/ProductionOrders/_ProductionOrderRow.cshtml (neue Zellen fuer die K1-Zeilenwerte + Matchcode, Wechsel-Erkennung zur Vorzeile innerhalb derselben Gruppe)"
  - "IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (neue `ColumnDef('matchcode', 'Matchcode', Locked: false)` — Position neben `article-number`/`description1` — mindestens in `ProductionOrders`, `PickingLeitstand`, `FaCompletion`, `FaWorklist`, `Picking`, `OseonTracking` — aber NUR soweit die dort angezeigte Zeile eine FA/Sub-FA ist; Scope ist laut Antwort 7 auf die FA-Zeilen-Ebene begrenzt, die Einordnung von `Picking` und `OseonTracking` ist am Code zu verifizieren und bei Komponentenebene auszuschliessen (dann dokumentieren, nicht bauen); zusaetzlich `customer-row`/`prio`/`ab-nummer`/`montage-abteilung`/`konstruktions-termin` NUR in `ProductionOrders`, da Kopfdaten-je-Zeile laut Umfangsentscheidung auf die FA-Liste begrenzt ist)"
  - "Views/ProductionOrders/Index.cshtml inline `#column-config` (Fallstricke **§3** 'Views, Forms, JavaScript', Zeilen 203-212 — NICHT §10, das ist die BOM-Bridge: JEDE neue `<th data-col-key>` MUSS zusaetzlich zu `ColumnDefinitions.cs` hier eingetragen werden, sonst rutscht sie beim Re-Append vor alle registrierten Spalten bzw. liefert die Prefs-API 400)"
  - "IdealAkeWms/Views/PickingLeitstand/_PickingLeitstandRow.cshtml, Views/FaWorklist/_FaWorklistRow.cshtml, Views/FaCompletion/_FaCompletionRow.cshtml, Views/Picking/_PickingRow.cshtml, Views/Tracking/Index.cshtml o.ae. (Matchcode-Zelle je Erhebungsergebnis)"
  - "docs/TESTSZENARIEN.md"
  - "secondbrain/tests/testszenarien-index.md"
  - "secondbrain/specs/freigegeben/2026-08-18-fa-liste-hierarchie-anzeige-spec.md (NACHZIEHEN im HAUPTCHECKOUT — Pfad ist `freigegeben/`, die frueher parallel existierende `entwurf/`-Fassung wurde am 2026-09-10 als Duplikat entfernt: Freigabe-Kaskade-Umkehr — Antwort 3/Tabelle Abschnitt 8 'BulkRelease ist Freigabe, nicht Fertigmeldung — keine Kaskade' wird durch die neue eigene Aktion `CascadeRelease` ergaenzt, NICHT durch eine Aenderung an `BulkRelease` selbst. Ohne dieses Nachziehen widersprechen sich zwei freigegebene Specs)"
open_questions: []
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: "Freigabe-Kaskade folgt der bestehenden Zuweisungspflicht (ein Pflicht-Picker fuer alle, wenn KommissionierungMitZuweisung aktiv); Matchcode hausweit sichtbar, AKE-View-Erweiterung folgt spaeter"
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-09-10
# Flache Schluessel mit Absicht: Obsidians Property-Editor kann verschachtelte
# YAML-Objekte NICHT bearbeiten - und genau diesen Block fuellt der Mensch aus.
---

> [!check] Fliesstext auf den beantworteten Stand nachgezogen (2026-09-10, vor Umsetzungsbeginn)
> Die Freigabe-Antworten lagen vor, waren aber **nicht in den Rumpf gezogen** — der Rumpf
> widersprach ihnen an fuenf Stellen und haette den Dev-Lauf in die falsche Richtung geschickt.
> Bereinigt, jeweils zugunsten der Antwort:
> 1. **Kopfzeile behaelt Kunde und einen Leittermin** (Fert.-Termin) — nicht „nur noch Nummer,
>    Sub-FA-Zahl, Badge". Bei zugeklappter Gruppe ist die Kopfzeile das Einzige, was sichtbar ist.
> 2. **„Nur wo der Wert wechselt" ist CLIENT-seitig**, nach jedem Reorder, Wert bleibt im DOM —
>    nicht serverseitig. Das dagegen angefuehrte Fallstricke-Zitat betrifft `applyFilters()`, also
>    das Filtern, nicht das Sortieren; Zitat `§10` → **`§3`** korrigiert.
> 3. **Freigabe-Kaskade NICHT ohne Zuweisung:** `KommissionierungMitZuweisung` blockiert hart
>    (`PickingLeitstandController` Z. 346-351/391-396). Der Dialog verlangt EINEN Kommissionierer
>    fuer alle und benennt die Massenwirkung. Die bestehende Regel wird nicht aufgeweicht.
> 4. **AKE-Matchcode ist keine externe Sage-Abhaengigkeit**, sondern eine eigene, planbare Aufgabe
>    („folgt spaeter") — nachgezogen in Fachlichen Anforderungen, Out-of-Scope und Deploy.
> 5. **AK 5 auf eine normale Gruppe umgeschrieben** (ein Kombigeraet kann den Fall nicht
>    illustrieren, dort bleiben die Zellen leer); Kombigeraete haben jetzt ein **eigenes** AK.
>
> Dazu: `open_questions` geleert, die SOLLTE-/HINWEIS-Punkte der Pruefung eingearbeitet
> (Picker-Parameter und Prioritaets-Reihenfolge in der Repository-Signatur, `F7` als
> Materialisierungs-Spec-Nummerierung statt `fallstricke.md`-Paragraph kenntlich gemacht,
> Verifikationsvorbehalt aus Antwort 1 als **Schritt 1** in den Technischen Loesungsentwurf gezogen).
> Die Abschnitte „Freigabe-Antworten", „ANTWORTEN auf die Kritische Pruefung" und „Kritische
> Pruefung" bleiben unveraendert als Protokoll stehen.

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
2. **HauptFA als normale Zeile (FA-Liste).** Die Gruppen-Kopfzeile wird **schlanker, nicht leer**
   [Antwort 2]: Es bleiben HauptFA-Nummer, Sub-FA-Zahl, **Kunde**, **ein Leittermin (Fert.-Termin)**
   und das Mehrdeutig-Badge. In die Zeilen wandern **Prio, AB-Nummer, Montage-Abteilung und die
   uebrigen Termine**. Die Dopplung von Kunde und Leittermin ist Absicht: Bei **zugeklappter** Gruppe
   ist die Kopfzeile das Einzige, was sichtbar ist — faellt dort alles weg, zeigt eine zugeklappte
   Liste nur noch Nummern und die Zwei-Ebenen-Ansicht verliert genau den Ueberblick, fuer den sie
   gebaut wurde. Die HauptFA-eigene Zeile traegt ihre Aktionen selbst.
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
   ueber das Zahnrad steuerbar wie jede andere Spalte, **sichtbar per Default (kein `defaultHidden`,
   kein Modus-Sonderfall)** [Antwort 9]. Fuer IDEAL sofort befuellt; fuer AKE leer, bis die
   hausintern geplante View-Erweiterung liefert — Anzeige/Filter/Sortierung duerfen daran nicht
   scheitern (**F7-Regel**; „F7" ist die Kurzregel-Nummerierung F1–F8 **der
   Materialisierungs-Spec**, nicht ein Paragraph in `fallstricke.md` — dessen §7 behandelt „Lager,
   Bestand, Bestellwesen").
5. **Erhebung** (Teil des Umfangs, siehe Abschnitt „Rechercheergebnis: Erhebung Artikelbezeichnung"
   unten): welche Listen zeigen heute eine Artikelbezeichnung. Die **Scope-Frage ist entschieden**
   [Antwort 7]: **nur die FA-Zeilen-Ebene**. Offen bleibt damit allein die Einordnung **je View** —
   ob die dort angezeigte Zeile eine FA/Sub-FA oder eine BOM-Komponente ist. Das ist am Code zu
   verifizieren (betrifft `Picking` und `OseonTracking`, siehe Tabelle); faellt eine View auf die
   Komponentenebene, gehoert sie zur Folge-Arbeit und ist als solche zu dokumentieren, nicht
   stillschweigend zu bauen.
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
  WarehouseRequisitions) — **entschieden out of scope** [Antwort 7]. Fachlich ist sie gewollt
  („ueberall, wo eine Artikelbezeichnung steht"), technisch aber ein anderer Pfad
  (`FaHierarchyBomItem`/`BomItem` statt `ProductionOrder`), und die **BOM-Bridge steht frisch auf
  `Testbereit`** — sie sofort wieder aufzureissen kostet einen vollstaendigen Re-QA-Lauf fuer einen
  Zusatz, der nicht draengt. **Als eigene Backlog-Notiz anzulegen** (Teil dieses Dev-Laufs), mit dem
  Hinweis: Fuer **IDEAL** fuehrt die Quelle den Matchcode bereits (die Bridge liest
  `FaHierarchyNode`), fuer **AKE** erst nach der View-Erweiterung.
- **Kombinationsgeraete als Gruppen-Schluessel-Erweiterung** — weiterhin paketweit out of scope
  (unveraendert aus Teil 7/Materialisierungs-Spec).
- **AKE-View-Erweiterung um den Matchcode selbst** — **keine externe Abhaengigkeit, sondern eine
  eigene, planbare Aufgabe, die spaeter folgt** [Antwort 8/B3]. Die Erweiterung liegt bei uns
  selbst, nicht bei einem Dritten; es ist ein Termin, kein Risiko. Diese Spec **blockiert
  ausdruecklich nicht** darauf: Spalte, Materialisierung und Anzeige werden **jetzt** gebaut und fuer
  **IDEAL sofort** befuellt; fuer AKE bleibt die Spalte leer und fuellt sich danach **ohne weitere
  Code-Aenderung**. Zwei Dinge sichern das ab: der **null-sichere Lesepfad** (F7) und der
  **UAT-Vermerk** „Matchcode ist bei AKE bis zur View-Erweiterung leer — kein Fehler" (sonst wird sie
  beim ersten AKE-Regressionstest als Mangel gemeldet). Das Nachholen (Mapping im AKE-Lesepfad
  ergaenzen, einmal pruefen, dass die Spalte sich fuellt) ist als **eigener kleiner Backlog-Punkt** zu
  fuehren — sonst geht es zwischen den groesseren Themen unter.

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
| `Picking` | `Views/Picking/Index.cshtml` (Kommissionierliste) | `description` | **am Code zu verifizieren** — je Kommissionierposition, evtl. BOM-Ebene. Ist es die Komponentenebene, faellt die View laut Antwort 7 aus dem Scope (Folge-Arbeit) |
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
`ProductionOrder.Matchcode`. **Antwort 7 hat diese Ebene entschieden ausgeschlossen** — sie ist
Folge-Arbeit und bekommt eine eigene Backlog-Notiz. Die dort noetige Machbarkeitsfrage
(`FaHierarchyBomItem` ist eine gefahrlos erweiterbare Ableitung, siehe Fallstricke §10 „`BomItem`
darf KEINE neue Property bekommen" — die Ausnahme ist die abgeleitete Klasse) gehoert damit in jene
Folge-Aufgabe, **nicht** in diesen Dev-Lauf.

**Fazit fuer den gesicherten Kern dieser Spec:** **Vier** Listen sind gesichert FA-Zeilen-Ebene
(`ProductionOrders`, `PickingLeitstand`, `FaCompletion`, `FaWorklist`) — sie koennen den neuen
`ProductionOrder.Matchcode` direkt zeigen, sobald er materialisiert ist, ohne weitere
Datenquellen-Aenderung. **Zwei** (`Picking`, `OseonTracking`) sind **am Code zu verifizieren**: Zeigt
die Zeile dort eine FA/Sub-FA, kommen sie dazu; zeigt sie eine Komponente, fallen sie nach Antwort 7
in die Folge-Arbeit — das Ergebnis ist zu dokumentieren, damit spaeter niemand eine vergessene Liste
vermutet. Die BOM-Komponentenebene (`Bom`, `WarehousePickingDetails`, Druckdokumente, Fehlteile,
Bestellungen) ist entschieden **out of scope**.

## Fachliche Anforderungen

### 1 — Kopfdaten je Zeile (FA-Liste)

- Zeilenwerte fuer Kunde, Konstruktions-/Fert.-/Liefertermin, Prio, AB-Nummer, Montage-Abteilung
  werden im hierarchischen Modus aus denselben `FaHierarchyOrderInfo`-Kopfdaten befuellt, die heute
  bereits fuer die Gruppen-Kopfzeile geladen werden (`GetByHauptFaKeysAsync`, **kein** zusaetzlicher
  Fan-out-Join). `MapItem` erhaelt dafuer dieselben Gruppenwerte, die es heute schon fuer
  `ProductionDate`/`DeliveryDate`/`BeschichtungTermin` nutzt (Termin-Kaskade-Mechanik der
  Materialisierungs-Spec), zusaetzlich fuer `Customer`/`Prio`/`AbNummer`/`MontageAbteilung`.
- **Anzeige „nur wo der Wert wechselt" — client-seitig, nach JEDEM Reorder** [Antwort 4/B1]:
  reine Darstellungsregel, keine Datenregel.
  - Der Wert steht **in jeder Zeile im DOM**, damit Spaltenfilter, Sortierung und ein spaeterer
    Export ihn sehen. Nur die **Darstellung** wird unterdrueckt bzw. gedimmt.
  - **Nicht per `display:none`, nicht per leerem Zellinhalt** — sonst verschwindet der Wert fuers
    Kopieren und fuer einen Export mit.
  - Die Unterdrueckung ist eine kleine Funktion, die nach **Init, Sortierung und Filter-Reload**
    erneut laeuft, aufgerufen aus denselben Stellen wie die Sortierung.
  - **Warum nicht serverseitig:** Die Sortierung laeuft nachweislich client-seitig
    (`table-filter.js`, die URL bleibt unveraendert). Eine serverseitig berechnete „erste
    Vorkommnis"-Markierung waere nach dem ersten Sortierklick **falsch** — dann stuende der Kunde
    bei einer beliebigen Zeile in der Mitte.
- **Kombigeraet-Fall: bei `IsAmbiguous=true` bleiben die K1-Zeilenspalten LEER** [Antwort 3].
  Keine Zuordnung erfinden: Es gibt **keinen Trennschluessel auf Positionsebene** — genau das ist der
  Kern des Kombigeraete-Problems ([[2026-08-06-kombinationsgeraete-montageabteilung]]). Eine Zeile
  einer Kopfvariante zuzuordnen waere geraten, und ein falscher Kunde oder Termin an einer Zeile ist
  schlimmer als ein leeres Feld. Die Kopfzeile zeigt weiterhin **alle** Varianten im Klartext plus
  Badge — unveraendert wie heute.
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
  Zeilenspalten wie jede andere Zeile. **Die Wurzel IST bereits materialisiert** [Antwort 1]: Die
  Materialisierung nimmt laut Teil-7-Spec ausdruecklich „die Zeilen mit `SubFA != 0` **plus die
  Wurzel**", und am Testsystem ist in der Gruppe „HauptFA 1035235" die **erste Zeile** `1035235`
  selbst. Es braucht damit **keine** zusaetzliche Materialisierungs-Anpassung.
  **Verifikationspflicht des Dev-Laufs:** Diese Aussage stammt aus einer Bildschirm-Beobachtung —
  und Bildschirm-Beobachtungen haben in dieser Runde schon einen falschen Befund getragen (B-1/H-1).
  **Erster Schritt der Umsetzung ist deshalb der Gegenbeweis am Code**, nicht das Bauen darauf.
- Sie bekommt ihre eigenen Aktionen in der Zeile statt eines separaten Kopf-Widgets.
- **Kopfzeile wird schlanker, aber NICHT leer** [Antwort 2/B4]: Es bleiben HauptFA-Nummer,
  Sub-FA-Zahl-Badge, **Kunde**, **ein Leittermin (Fert.-Termin)** und das Mehrdeutig-Badge. Nur
  Prio, AB-Nummer, Montage-Abteilung und die uebrigen Termine entfallen dort
  (`Views/ProductionOrders/Index.cshtml` Zeilen ~131-167) und erscheinen stattdessen in den Zeilen.
  Begruendung siehe Umfang Punkt 2: bei zugeklappter Gruppe ist die Kopfzeile das Einzige, was
  sichtbar ist.
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
- **Kommissionierer-Zuweisung: die bestehende Pflicht gilt unveraendert** [Antwort zu B2,
  ENTSCHEIDUNG]. Am Code geprueft: `KommissionierungMitZuweisung` **blockiert hart**
  (`PickingLeitstandController` Z. 346-351 / 391-396) — es belegt die Oberflaeche nicht bloss vor.
  Damit greift die in Antwort 5 selbst gesetzte Pruefbedingung („blockiert es hart, dann melden statt
  die bestehende Regel aufzuweichen"), und sie wird zugunsten der Regel aufgeloest:
  - Ist `KommissionierungMitZuweisung` **aktiv**, verlangt der Kaskaden-Dialog **einen
    Kommissionierer** — genau wie `ToggleRelease` und `BulkRelease`. Er wird auf **alle**
    kaskadierten Sub-FAs gesetzt.
  - Der Dialog **benennt die Massenwirkung ausdruecklich**, bevor sie eintritt: „Der gewaehlte
    Kommissionierer wird allen N Sub-FAs zugewiesen und kann danach je Sub-FA geaendert werden."
  - Ist die Einstellung **inaktiv**, laeuft die Kaskade ohne Zuweisung — wie die Einzelfreigabe auch.
  - **Die bestehende Regel wird NICHT aufgeweicht.** Eine Massenaktion, die eine Pflicht umgeht, die
    fuer die Einzelaktion gilt, ist eine Inkonsistenz, die frueher oder spaeter jemand ausnutzt oder
    als Fehler meldet. Das fachliche Bedenken bleibt richtig — die Sub-FAs haengen an verschiedenen
    Arbeitsbereichen (`K-02`, `S-01`, `H4-04` …), ein Picker fuer alle ist oft nicht der
    endgueltige. **Aber das ist ein Argument gegen die REGEL, nicht fuer ihre Umgehung.** Zeigt die
    Praxis, dass die Vorbelegung staendig nachkorrigiert wird, ist das der Anlass, die
    Zuweisungspflicht selbst zu ueberdenken — dann bewusst und fuer alle Freigabewege. Dasselbe
    Muster wie beim Werkbank-Umschaltpunkt B→C: beobachten, dann entscheiden, nicht vorab ausweichen.
- **Fortlaufende `PickingPriority`, alle in EINEM Batch** [Antwort 6], Muster
  `SetReleaseBatchAsync`. Begruendung: Die Sub-FAs eines Geraets sollen in der
  Kommissionier-Reihenfolge **beieinander bleiben** — ohne Prioritaet verteilen sie sich beliebig
  zwischen fremden Auftraegen, und dann holt jemand Teile fuer ein Geraet ueber den halben Tag
  verteilt. **Reihenfolge innerhalb des Batches: `SubOrderNumber` aufsteigend** (Z2, wie ueberall) —
  explizit zu setzen, sonst entscheidet die zufaellige DB-Lesereihenfolge.

### 4 — Matchcode

- **Neue Spalte `ProductionOrder.Matchcode`** (string?, analog `ArticleNumber`), materialisiert aus
  `FaHierarchyNode.Matchcode` in `FaMaterializationSyncService` — Anlege- UND Update-Pfad in
  DERSELBEN Aenderung (F1-Regel der Materialisierungs-Spec, dasselbe Muster wie
  `ArticleNumber`/`Description1/2`). **Kein** Sonderfall, **keine** Z1-Ausnahme — `Matchcode` ist
  kein app-verwaltetes Feld, sondern eine reine Sage-Quell-Abbildung wie `ArticleNumber`.
- **Anzeige** in den sechs gesicherten FA-Zeilen-Ebene-Listen (siehe Erhebung oben), Position
  „eigene Spalte neben Artikelnummer/Bezeichnung 1", `Locked: false`, ueber das Zahnrad
  steuerbar — **beide** Stellen pflegen: `ColumnDefinitions.cs` UND das inline `#column-config`
  der jeweiligen View (Fallstricke **§3** „Views, Forms, JavaScript", Zeilen 203-212 — **nicht §10**,
  das ist die BOM-Bridge; Regel „neue `<th data-col-key>` immer in BEIDEN Stellen").
- **Hausweit sichtbar per Default — kein `defaultHidden`, kein Modus-Sonderfall** [Antwort 9]: Der
  Matchcode ist fuer IDEAL essentiell; ihn ausgeblendet auszuliefern hiesse, dass ihn jeder Anwender
  erst suchen muss. Die voruebergehend leere AKE-Spalte ist der Preis — und er ist gering, weil die
  Spaltenauswahl seit Etappe 6 **je Benutzer** funktioniert: Wen sie stoert, blendet sie mit einem
  Klick aus. Ein modusabhaengiger Default waere genau der Sonderfall, den die Entscheidung „hausweit"
  beseitigt hat.
- **F7-Regel (leeres Feld darf Filter/Sortierung nicht ins Leere laufen lassen)** — „F7" ist die
  Kurzregel-Nummerierung F1–F8 **der Materialisierungs-Spec**, nicht ein `fallstricke.md`-Paragraph
  (dessen §7 ist „Lager, Bestand, Bestellwesen"): solange AKE die
  Spalte nicht befuellt, muss ein Spaltenfilter auf „Matchcode" fuer AKE-Zeilen entweder alle
  Zeilen als „kein Treffer" behandeln (korrekt, weil leer) oder die Spalte fuer AKE ausblenden —
  **keine** Sonderbehandlung, die stillschweigend eine leere Liste erzeugt ohne erkennbaren Grund.
  Da die Spalte laut Entscheidung hausweit gleich behandelt wird, gilt: leer bleibt leer, Filter
  liefert dafuer korrekt keine Treffer, keine Ausnahme im Filter-Code noetig.
- **AKE-Quelle: GEKLAERT — eigene, planbare Aufgabe, die spaeter folgt** [Antwort 8/B3]. Die
  AKE-View wird **hausintern** um den Matchcode erweitert. Damit ist das **keine offene
  Abhaengigkeit** mehr, sondern ein Termin: Die Erweiterung liegt bei uns selbst, nicht bei einem
  Dritten. **Kein Abstimmungsschritt vor Umsetzungsbeginn, keine Blockade.** Spalte,
  Materialisierung und Anzeige werden **jetzt** gebaut und fuer **IDEAL sofort** befuellt; fuer AKE
  bleibt die Spalte leer und fuellt sich danach **ohne weitere Code-Aenderung**.
  Zwei Absicherungen sind Pflicht dieses Dev-Laufs:
  1. **Null-sicherer Lesepfad** — kein Filter, keine Sortierung, keine Gruppierung laeuft auf dem
     leeren Feld ins Leere (F7).
  2. **UAT-Vermerk** „Matchcode ist bei AKE bis zur View-Erweiterung leer — **kein Fehler**", in
     Spec UND manuelle Test-Checkliste. Ohne ihn wird die leere Spalte beim ersten
     AKE-Regressionstest als Mangel gemeldet.
  Das Nachholen nach der View-Erweiterung (Mapping im AKE-Lesepfad ergaenzen, einmal pruefen, dass
  die Spalte sich fuellt) ist als **eigener kleiner Backlog-Punkt** zu fuehren.

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

> **Schritt 1 der Umsetzung ist eine Verifikation, kein Bau** [Vorbehalt aus Antwort 1]: Am **Code**
> gegenpruefen, dass die Wurzelzeile (`VaterFA = NULL`) tatsaechlich materialisiert wird — also dass
> `FaMaterializationPlanner`/`FaMaterializationSyncService` die Wurzel mitnehmen und nicht nur
> `SubFA != 0`. Die Ja-Antwort stammt aus einer Bildschirm-Beobachtung, und Bildschirm-Beobachtungen
> haben in dieser Runde bereits einen falschen Befund getragen (B-1/H-1 im
> [[2026-08-20-fehlerprotokoll-anzeige-epic-ab]]). Ergibt die Pruefung **nein**, ist Punkt 2
> (HauptFA als normale Zeile) blockiert — dann **melden**, nicht eine Materialisierungs-Anpassung
> dazuerfinden: Die Materialisierung gehoert einer anderen, frisch abgenommenen Spec.

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
- **„Nur wo der Wert wechselt": CLIENT-seitig** [Antwort 4, bestaetigt in B1]. Der Razor-/Controller-
  Weg ist ausdruecklich **verworfen**:
  - Die Zelle wird **immer mit Wert gerendert** (Server liefert jede Zeile vollstaendig).
  - Eine kleine JS-Funktion markiert je Gruppen-`<tbody>` Wiederholungen zur Vorzeile und
    unterdrueckt **nur die Darstellung** (z. B. per Klasse, die den Text transparent/gedimmt
    schaltet) — **nicht** `display:none`, **nicht** leerer Zellinhalt, damit Kopieren, Spaltenfilter
    und ein spaeterer Export den Wert weiterhin sehen.
  - Sie laeuft nach **Init, nach jeder Sortierung und nach jedem Filter-Reload** — aufgerufen aus
    denselben Stellen wie die Sortierung (`table-filter.js` reordert die Zeilen je `<tbody>`; nach
    jedem Reorder ist die „erste Vorkommnis"-Markierung neu zu bestimmen).
  - **Warum nicht serverseitig:** Die Sortierung laeuft client-seitig (URL bleibt unveraendert). Eine
    serverseitig gesetzte Markierung waere nach dem ersten Sortierklick falsch.
  - **Korrektur des frueheren Arguments:** Der hier vorher zitierte Fallstrick („Server-Filter-Mode:
    kein clientseitiges `applyFilters()` beim Init") betrifft das **Filtern**, nicht das
    **Sortieren** — sachlich richtig, aber auf den falschen Vorgang angewandt. Er spricht nicht
    gegen clientseitige Darstellungslogik nach einem Reorder.
- **Kunde-Postfilter:** `ProductionOrdersController.Index` erweitert die bestehende
  `FaListDateColumnKeys`/`hasDateFilters`-Verzweigung um `customer`/`prio`/`ab-nummer`/
  `montage-abteilung` als weitere „nach C#-Berechnung filtern"-Keys (analog den Datumsspalten);
  `ProductionOrderRepository.BuildLeitstandQuery`/`BuildCustomerColumnFilterPredicate` verlieren
  dadurch ihren hierarchischen Sonderfall und werden fuer den `customer`-Filter im hierarchischen
  Modus nicht mehr aufgerufen (das SQL-Praedikat bleibt nur fuer den flachen AKE-Fall bestehen,
  F4-Praezedenz). Gleiche Umstellung in `PickingLeitstandController.Index`, weil beide Controller
  dieselbe Repository-Methode teilen.
- **Freigabe-Kaskade:** neue `IProductionOrderPickingStatusRepository.
  SetReleaseForOrderNumberAsync(orderNumber, releasedBy, assignedPicker, modifiedBy,
  modifiedByWindows)` — Zwei-Schritt-Query (IDs lokal, dann `Contains`, InMemory-testbar, analog
  `SetIsDoneBdeForOrderNumberAsync`), filtert zusaetzlich auf `IsReleasedForPicking == false`,
  setzt `assignedPicker` (nullable — nur bei aktivem `KommissionierungMitZuweisung` gefuellt) auf
  **alle** betroffenen Zeilen, vergibt `PickingPriority` fortlaufend ab `MAX+1` (wiederverwendet die
  Logik aus `GetMaxPickingPriorityAsync`) **in der Reihenfolge `SubOrderNumber` aufsteigend** —
  explizit sortieren, nicht der DB-Lesereihenfolge ueberlassen —, EIN `SaveChangesAsync`.
  `PickingLeitstandController` bekommt `CascadeReleasePreview`(GET)/`CascadeRelease`(POST),
  Route/Namensgebung/Bestaetigungsdialog-Markup 1:1 nach dem Vorbild
  `CascadeDonePreview`/`CascadeDone` (`Views/PickingLeitstand/Index.cshtml` Zeilen ~344-383), aber
  mit `[RequireLeitstandAccess]` statt `[RequirePickingAccess]` und dem Button sichtbar nur bei
  `Model.CanManagePickingRelease` (dieselbe Sichtbarkeitsbedingung wie der bestehende
  `BulkRelease`-Mechanismus), nicht bei `Model.CanPick`.
  **`CascadeRelease` uebernimmt die Zuweisungspflicht wortgleich** aus `ToggleRelease`/`BulkRelease`
  (`PickingLeitstandController` Z. 346-351 / 391-396): Ist `KommissionierungMitZuweisung` aktiv und
  kein Kommissionierer uebergeben, wird **abgewiesen** — kein Sonderweg, keine Lockerung fuer die
  Massenaktion. Der Preview-Dialog fuehrt in diesem Fall die Kommissionierer-Auswahl als Pflichtfeld
  und nennt die Massenwirkung im Klartext (siehe Fachliche Anforderungen 3).
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

**Nummer am 2026-09-10 im Worktree nachgezaehlt, nicht angenommen:** Der Buendel-Worktree steht bei
`90_InvertProductionOrderHierarchy.sql`, `main` bei `88_...`, die beiden anderen offenen Worktrees bei
`83`/`81`. **`SQL/91_` ist damit die naechste freie Nummer.** Sollte vor dem Merge dieses Buendels ein
anderer Branch zuerst landen, ist die Nummer erneut zu pruefen (uebliches Restrisiko jedes offenen
Buendels, keine Besonderheit dieser Spec).

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
  `ColumnDefinitions.cs`-Eintrag UND inline `#column-config` (Fallstricke **§3** „Views, Forms,
  JavaScript", Zeilen 203-212 — **nicht §10**; beide Stellen sind Pflicht, sonst 400 bei den
  Spaltenpraeferenzen bzw. stille Fehlplatzierung beim Re-Append).
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
2. **Matchcode-Spalte sichtbar und filterbar** in den **vier gesicherten** FA-Zeilen-Ebene-Listen
   (`ProductionOrders`, `PickingLeitstand`, `FaCompletion`, `FaWorklist`) — **sichtbar per Default**,
   ueber das Zahnrad ein-/ausblendbar, Position neben Artikelnummer/Bezeichnung 1. Fuer `Picking` und
   `OseonTracking` gilt das **zusaetzlich, sofern** die Code-Verifikation die Zeile als FA/Sub-FA
   bestaetigt; andernfalls ist der Ausschluss dokumentiert (Antwort 7).
3. **AKE-Regression:** bei Master `false` bzw. auf einer AKE-Instanz ist `Matchcode` leer (kein
   Materialisierungsversuch, kein Fehler); Spaltenfilter auf einer durchgehend leeren
   Matchcode-Spalte liefert korrekt „kein Treffer" statt eines Fehlers oder einer stillen leeren
   Liste ohne erkennbaren Grund (F7-Nachweis).
4. **Kopfdaten je Zeile vorhanden.** Jede Sub-FA-Zeile der FA-Liste traegt im Anzeige-Modell Kunde,
   Termine, Prio, AB-Nummer, Montage-Abteilung als eigene Werte (nicht nur die Gruppe) — nachweisbar
   z. B. durch einen Spaltenfilter auf „Kunde", der Treffer auf Zeilenebene liefert, ohne dass
   `ProductionOrder.Customer` in der DB befuellt ist.
5. **Anzeige „nur wo der Wert wechselt" korrekt — an einer normalen, eindeutigen Gruppe.** In einer
   Gruppe mit `IsAmbiguous=false` zeigen zwei aufeinanderfolgende Zeilen mit identischem Kunden den
   Kunden nur in der **ersten** der beiden; tritt weiter unten in derselben Gruppe ein **anderer**
   Wert auf (z. B. eine andere Montage-Abteilung), erscheint er dort wieder.
   **Zusaetzlich nachzuweisen** (folgt aus Antwort 4): Der unterdrueckte Wert ist weiterhin **im DOM
   vorhanden** (kein `display:none`, kein leerer Zellinhalt) und ein Spaltenfilter auf dieser Spalte
   findet die Zeile trotz unterdrueckter Darstellung.
6. **Markierung haelt nach Sortierung.** Nach einem Klick auf einen Spaltenkopf (Reorder) stimmt die
   „nur wo der Wert wechselt"-Darstellung wieder zur **neuen** Zeilenfolge — der Wert steht bei der
   jeweils ersten Zeile jeder Wiederholungskette, nicht bei einer beliebigen Zeile in der Mitte.
   Das ist der Nachweis, dass die Markierung client-seitig nach jedem Reorder neu laeuft und nicht
   serverseitig eingebrannt ist.
7. **Kombigeraete: K1-Zeilenspalten bleiben leer.** In einer Gruppe mit `IsAmbiguous=true` sind die
   K1-Zeilenspalten (Kunde, Termine, Prio, AB-Nummer, Montage-Abteilung) auf **allen** Zeilen leer —
   keine Zeile wird einer Kopfvariante zugeordnet. Die Gruppen-Kopfzeile zeigt dort weiterhin **alle**
   Varianten im Klartext plus Mehrdeutig-Badge.
8. **Kunde-Filter funktioniert weiterhin ohne Sonderweg-Join.** Freitext- und Spaltenfilter auf
   „Kunde" liefern im hierarchischen Modus weiterhin korrekte Treffer, obwohl
   `ProductionOrder.Customer` je Zeile leer bleibt — nachweisbar durch denselben Testfall wie in der
   Materialisierungs-Spec (TS-71-Referenz), jetzt aber ueber den C#-Postfilter statt den
   SQL-Subquery-Join.
9. **HauptFA-Zeile vorhanden und erkennbar.** Die HauptFA-eigene Zeile (Wurzel) ist in der FA-Liste
   als normale Zeile sichtbar und traegt ihre Aktionen selbst. **Vorgeschaltet ist der
   Code-Nachweis**, dass die Wurzel materialisiert wird (siehe Technischer Loesungsentwurf,
   Schritt 1) — faellt er negativ aus, ist dieses AK nicht erfuellbar und der Befund zu melden.
10. **Kopfzeile schlanker, aber nicht leer.** Die Gruppen-Kopfzeile zeigt HauptFA-Nummer,
    Sub-FA-Zahl-Badge, **Kunde**, **Fert.-Termin** und das Mehrdeutig-Badge. **Entfernt** sind dort
    Prio, AB-Nummer, Montage-Abteilung und die uebrigen Termine — diese erscheinen nur noch in den
    Zeilen. Gegenprobe: Eine **zugeklappte** Gruppe zeigt weiterhin Kunde und Fert.-Termin.
11. **Freigabe-Kaskade kaskadiert korrekt.** „Alle Sub-FAs freigeben" am HauptFA setzt
   `IsReleasedForPicking=true` fuer ALLE Nachfahren-Sub-FAs dieser `OrderNumber` (nicht nur direkte
   Kinder), atomar (ein `SaveChangesAsync`), mit Bestaetigungsdialog, der die Gesamtzahl UND die
   Anzahl tatsaechlich neu freizugebender Sub-FAs zeigt.
12. **Freigabe-Kaskade ruehrt bereits Freigegebene nicht an.** Eine Sub-FA, die vor der Kaskade
    bereits `IsReleasedForPicking=true` war, bleibt danach unveraendert (Wert, Audit-Felder **und**
    bereits gesetzter Kommissionierer).
13. **Freigabe-Kaskade meldet die korrekte Anzahl.** Die Erfolgsmeldung nennt die Anzahl
    TATSAECHLICH neu freigegebener Sub-FAs, nicht die Gesamtzahl der Gruppe.
14. **Zuweisungspflicht gilt auch fuer die Kaskade.** Ist `KommissionierungMitZuweisung` **aktiv**,
    wird ein `CascadeRelease` **ohne** Kommissionierer abgewiesen — genau wie `ToggleRelease` und
    `BulkRelease`; der Dialog fuehrt die Auswahl als Pflichtfeld und benennt im Klartext, dass der
    gewaehlte Kommissionierer **allen N** Sub-FAs zugewiesen wird und danach je Sub-FA aenderbar ist.
    Ist die Einstellung **inaktiv**, laeuft die Kaskade ohne Zuweisung durch. **Negativtest ist
    Pflicht** — dass die Massenaktion die Pflicht nicht umgeht, ist der Kern der Entscheidung zu B2.
15. **Prioritaet fortlaufend und in definierter Reihenfolge.** Die neu freigegebenen Sub-FAs erhalten
    fortlaufende `PickingPriority`-Werte ab `MAX+1`, vergeben nach **`SubOrderNumber` aufsteigend** —
    nachweisbar daran, dass die Gruppe in der Kommissionier-Reihenfolge zusammenhaengend und in
    Sub-FA-Ordnung steht, nicht in zufaelliger DB-Lesereihenfolge.
16. **Keine Gruppen-Ruecknahme.** Es gibt keine Aktion, die die Freigabe fuer eine ganze HauptFA-Gruppe
    zurueckzieht; `ToggleRelease` bleibt die einzige Ruecknahme-Aktion, zeilenbasiert.
17. **`BulkRelease` unveraendert.** Die bestehende Mehrfachauswahl-Freigabe verhaelt sich nach dieser
    Spec bit-identisch zum Vorzustand — kein kaskadierendes Verhalten, keine neue Option.
18. **Zugriffsschutz korrekt.** `CascadeReleasePreview`/`CascadeRelease` sind ohne Leitstand-Recht
    nicht aufrufbar (403/Redirect wie bei den anderen `RequireLeitstandAccess`-Actions); der
    Freigeben-Button erscheint in der Oberflaeche nur bei `CanManagePickingRelease`.
19. **[[2026-08-18-fa-liste-hierarchie-anzeige-spec]] nachgezogen.** Die dortige Festlegung „`BulkRelease`
    ist Freigabe, nicht Fertigmeldung — keine Kaskade" ist um die neue Aktion `CascadeRelease`
    ergaenzt/widerrufen dokumentiert (Statusabschnitt/Tabelle Abschnitt 8), sodass sich die beiden
    Specs nicht mehr widersprechen. **Nachzuziehen im Hauptcheckout**, Pfad
    `secondbrain/specs/freigegeben/`.
20. **AKE-Regression gesamt.** Bei Master `false` verhaelt sich die FA-Liste bit-identisch zum
    Vorzustand: **keine** neue Spalte sichtbar ausser dem (leeren) Matchcode, **keine**
    Kaskaden-Aktion in der Kopfzeile, unveraenderte Filter und unveraenderte Sortierung. Der leere
    Matchcode bei AKE ist **kein Fehler** (siehe Fachliche Anforderungen 4) und als solcher in der
    manuellen Checkliste vermerkt.

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
- **Deploy-Vorbedingung: keine.** Es gibt **keine externe Abhaengigkeit** — weder fuer IDEAL noch
  fuer AKE [Antwort 8/B3]. Die AKE-View-Erweiterung um den Matchcode ist eine **hausinterne, planbare
  Aufgabe, die spaeter folgt**, kein Fremdsystem-Termin und kein Abstimmungsschritt mit Dritten. Sie
  blockiert diesen Deploy nicht: Fuer AKE bleibt die Spalte bis dahin leer (F7, null-sicher) und
  fuellt sich danach **ohne weitere Code-Aenderung**.
- **In die Deploy-/Abnahmenotiz aufnehmen:** „Matchcode ist bei AKE bis zur View-Erweiterung leer —
  kein Fehler." Ohne diesen Vermerk wird die leere Spalte beim ersten AKE-Regressionstest als Mangel
  gemeldet.
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

> [!success] ALLE ELF BEANTWORTET — Stand 2026-09-10, keine offene Rueckfrage mehr
> Verbindlich ist der Abschnitt **„Freigabe-Antworten"** und, wo die kritische Pruefung eine Antwort
> praezisiert hat, der Abschnitt **„ANTWORTEN auf die Kritische Pruefung"**. Der Fliesstext dieser
> Spec wurde am 2026-09-10 auf diesen Stand nachgezogen (fuenf Widersprueche bereinigt: Kopfzeile
> behaelt Kunde + Leittermin · Wechsel-Darstellung client-seitig · Kaskade mit Pflicht-Picker ·
> AKE-View als eigene Aufgabe statt externer Abhaengigkeit · AK 5 auf eine normale Gruppe, eigenes AK
> fuer Kombigeraete). Die Liste unten bleibt als **Fragenprotokoll** stehen — sie dokumentiert, was
> gefragt wurde, nicht offene Arbeit.
>
> **Zwei Punkte sind bewusst keine Rueckfragen, sondern Termine:** Nr. 10 (B-0/Ruling 4) wird **am
> Bildschirm getestet, vor dem Merge** — nicht entschieden. Nr. 11 braucht nach Schranke 2 der
> Materialisierungs-Spec **einen Blick in den Klassenkommentar**, kein Arbeitspaket.

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

1. → **JA — die Wurzelzeile ist bereits materialisiert.** Am Testsystem sichtbar: In der Gruppe
   „HauptFA 1035235" ist die **erste Zeile** `1035235` selbst, gefolgt von `1043421`, `1043422` …
   Die Materialisierung nimmt laut Teil-7-Spec ausdruecklich „die Zeilen mit `SubFA != 0` **plus die
   Wurzel**". Punkt 2 (HauptFA als normale Zeile) braucht damit **keine** zusaetzliche
   Materialisierungs-Anpassung.
   *Trotzdem am Code gegenpruefen, nicht auf diese Beobachtung bauen* — sie stammt aus einem
   Bildschirmfoto, und Bildschirmfotos haben in dieser Runde schon einmal einen falschen Befund
   getragen.

2. → **Die Kopfzeile bleibt — aber schlank.**
   **Grund, der leicht uebersehen wird:** Bei **zugeklappter** Gruppe ist die Kopfzeile das
   Einzige, was man sieht. Faellt dort alles weg, zeigt eine zugeklappte Liste nur noch Nummern —
   und die Zwei-Ebenen-Ansicht verliert genau den Ueberblick, fuer den sie gebaut wurde.
   - **Bleiben in der Kopfzeile:** HauptFA-Nummer, Sub-FA-Zahl, **Kunde**, **ein** Leittermin
     (Fert.-Termin), Mehrdeutig-Badge.
   - **Wandern in die Zeilen:** Prio, AB-Nummer, Montage-Abteilung, die uebrigen Termine.
   Die Dopplung von Kunde und Leittermin ist Absicht, kein Versehen — sie kostet eine Zeile und
   erhaelt die Bedienbarkeit im zugeklappten Zustand.

3. → **Bei `IsAmbiguous=true` bleiben die K1-Zeilenspalten LEER.** Keine Zuordnung erfinden.
   Es gibt **keinen Trennschluessel auf Positionsebene** — genau das ist der Kern des
   Kombigeraete-Problems ([[2026-08-06-kombinationsgeraete-montageabteilung]]). Eine Zeile einer
   Kopfvariante zuzuordnen waere geraten, und ein falscher Kunde oder Termin an einer Zeile ist
   schlimmer als ein leeres Feld.
   Die Kopfzeile zeigt weiterhin **alle** Varianten im Klartext plus Badge — unveraendert wie heute.

4. → **Client-seitig, ausgeloest nach JEDEM Reorder — und der Wert bleibt im DOM.**
   **Die Falle:** Die Sortierung laeuft client-seitig (`table-filter.js`, URL bleibt unveraendert).
   Eine server-seitig berechnete „erste Vorkommnis"-Markierung waere nach dem ersten Sortierklick
   **falsch** — dann stuende der Kunde bei einer beliebigen Zeile in der Mitte.
   **Vorgabe:**
   - Der Wert steht **in jeder Zeile im DOM** — damit Spaltenfilter, Sortierung und ein spaeterer
     Export ihn sehen. Nur die **Darstellung** wird unterdrueckt bzw. gedimmt.
   - Die Unterdrueckung ist eine kleine Funktion, die nach **Init, Sortierung und Filter-Reload**
     erneut laeuft — aufgerufen aus denselben Stellen wie die Sortierung.
   - **Nicht per `display:none` oder leerem Zellinhalt**, sonst verschwindet der Wert fuer
     Kopieren/Export mit.

5. → **Kaskade ohne Pflicht-Zuweisung; ein OPTIONALER Picker im Dialog, der auf alle wirkt.**
   Begruendung: Die Sub-FAs eines Geraets haengen an **verschiedenen Arbeitsbereichen**
   (`K-02`, `S-01`, `H4-04` …). Ein Picker fuer alle waere in der Mehrzahl der Faelle der falsche.
   **ZU PRUEFEN vor der Umsetzung:** Blockiert `KommissionierungMitZuweisung` die Freigabe ohne
   Picker, oder belegt es nur die Oberflaeche vor? Blockiert es hart, ist die Kaskade ohne
   Zuweisung nicht moeglich — **dann melden statt die bestehende Regel aufzuweichen.** Eine
   Pflicht, die fuer Einzelfreigaben gilt, darf nicht stillschweigend durch eine Massenaktion
   umgangen werden.

6. → **JA — fortlaufende `PickingPriority`, alle in EINEM Batch** (Muster `SetReleaseBatchAsync`).
   Begruendung: Die Sub-FAs eines Geraets sollen in der Kommissionier-Reihenfolge **beieinander
   bleiben**. Ohne Prioritaet verteilen sie sich beliebig zwischen fremden Auftraegen — und dann
   holt jemand Teile fuer ein Geraet ueber den halben Tag verteilt.
   Reihenfolge innerhalb des Batches: `SubOrderNumber` aufsteigend (Z2, wie ueberall).

7. → **NUR die FA-Zeilen-Ebene. Die BOM-Komponentenebene ist Folge-Arbeit.**
   Fachlich ist sie gewollt („ueberall, wo eine Artikelbezeichnung steht"), technisch aber ein
   anderer Pfad: `FaHierarchyBomItem`/`BomItem` statt `ProductionOrder`. Und die **BOM-Bridge steht
   frisch auf `Testbereit`** — sie sofort wieder aufzureissen kostet einen vollstaendigen Re-QA-Lauf
   fuer einen Zusatz, der nicht draengt.
   **Als eigene Backlog-Notiz** anlegen, mit dem Hinweis: Fuer **IDEAL** fuehrt die Quelle den
   Matchcode bereits (die Bridge liest `FaHierarchyNode`), fuer **AKE** erst nach der
   View-Erweiterung aus Nr. 8.

8. → **GEKLAERT: Die AKE-View wird hausintern um den Matchcode erweitert — folgt spaeter.**
   Damit ist es **keine offene Abhaengigkeit** mehr, sondern ein Termin. Die Erweiterung liegt bei
   uns selbst, nicht bei einem Dritten.
   **Vorgehen unveraendert und ausdruecklich nicht blockierend:** Spalte, Materialisierung und
   Anzeige werden **jetzt** gebaut und fuer **IDEAL sofort** befuellt. Fuer AKE bleibt sie leer, bis
   die View liefert — dann fuellt sie sich **ohne weitere Code-Aenderung**.
   **Zwei Dinge, die das absichert:**
   - Der Lesepfad muss den Matchcode **null-sicher** behandeln; keine Sortierung, kein Filter und
     keine Gruppierung darf auf einem leeren Feld ins Leere laufen (F7).
   - **Als bekannte Zwischeneinschraenkung in die Spec und in die UAT-Checkliste**: „Matchcode-Spalte
     ist bei AKE bis zur View-Erweiterung leer — kein Fehler." Sonst wird sie beim ersten
     AKE-Regressionstest als Mangel gemeldet.
   **Nach der View-Erweiterung nachzuholen:** das Mapping im AKE-Lesepfad ergaenzen und einmal
   pruefen, dass die Spalte sich fuellt. Als eigener kleiner Punkt im Backlog fuehren, sonst geht es
   zwischen den groesseren Themen unter.

9. → **Sichtbar per Default, hausweit einheitlich — kein `defaultHidden`, kein Modus-Sonderfall.**
   Der Matchcode ist fuer IDEAL essentiell; ihn ausgeblendet auszuliefern hiesse, dass ihn jeder
   Anwender erst suchen muss.
   Die voruebergehend leere AKE-Spalte ist der Preis — und er ist gering, weil die Spaltenauswahl
   seit Etappe 6 **je Benutzer** funktioniert: Wen sie stoert, blendet sie mit einem Klick aus. Ein
   modusabhaengiger Default waere genau der Sonderfall, den die Entscheidung „hausweit" beseitigt
   hat.

10. → **Unveraendert geparkt — wird getestet, nicht entschieden.** Pruefrage bleibt: *Erscheint
    dasselbe Material auf ZWEI Kommissionierlisten?* Am **Bildschirm** pruefen (dem Ausdruck fehlen
    genau die Spalten, die man dafuer braucht). Ergebnis entscheidet zwischen Rueckbau (Einzeiler
    in der Scope-Regel) und Druck-Whitelist-Erweiterung in `PrintBom.cshtml`. **Vor dem Merge.**

11. → **BESTAETIGT — nichts zu tun.** Der Rechercheergebnis stimmt: F5/F6 sind im Worktree-Stand
    umgesetzt, der Klassenkommentar nennt beide Z1-Ausnahmen (`Workplace` und `PickingStatus`).
    Damit ist mein urspruenglicher B1-Ausschluss ueberholt — der Grund dafuer (keine
    `PickingStatus`-Zeile fuer IDEAL) ist durch den F6-Fix weggefallen.
    **Einzige Restaufgabe:** Nach Schranke 2 der Materialisierungs-Spec gegenpruefen, dass es so
    geblieben ist — ein Blick in den Klassenkommentar, kein eigener Arbeitspunkt.

## ANTWORTEN auf die Kritische Pruefung (2026-09-10)

### Zu B1 — **Antwort 4 gilt, der Technik-Abschnitt ist zu korrigieren.**

Der Reviewer hat es am Code nachgewiesen: Der zitierte Fallstrick handelt von `applyFilters()`,
also vom **Filtern**, nicht vom **Sortieren**. Die Begruendung im Loesungsentwurf ist sachlich
richtig, aber auf den falschen Vorgang angewandt.

**Verbindlich bleibt Antwort 4:** client-seitig, ausgeloest nach jedem Reorder, Wert bleibt im DOM.
Grund unveraendert: Die Sortierung laeuft nachweislich client-seitig — eine server-seitig gesetzte
„erste Vorkommnis"-Markierung waere nach dem ersten Sortierklick falsch.
Im Rumpf entsprechend umschreiben; das falsche Fallstricke-Zitat (§10 statt §3) gleich mit
korrigieren.

### Zu B2 — **Die Kaskade folgt der bestehenden Regel: EIN Pflicht-Picker fuer alle.** [ENTSCHEIDUNG]

Meine Antwort 5 hat die Pruefbedingung selbst gesetzt — *„Blockiert es hart, dann melden statt die
bestehende Regel aufzuweichen."* Der Reviewer hat geprueft: **Es blockiert hart**
(`PickingLeitstandController` Z. 346-351 / 391-396). Damit greift die Bedingung, und die Antwort
ist entsprechend aufzuloesen — **nicht die Regel.**

**Verbindlich:**
- Ist `KommissionierungMitZuweisung` aktiv, verlangt der Kaskaden-Dialog **einen Kommissionierer**,
  genau wie `ToggleRelease` und `BulkRelease`. Er wird auf **alle** kaskadierten Sub-FAs gesetzt.
- Der Dialog **benennt das ausdruecklich**: „Der gewaehlte Kommissionierer wird allen N Sub-FAs
  zugewiesen und kann danach je Sub-FA geaendert werden." Damit ist die Massenwirkung sichtbar,
  bevor sie eintritt.
- Ist die Einstellung inaktiv, laeuft die Kaskade ohne Zuweisung — wie die Einzelfreigabe auch.

**Warum nicht die Ausnahme:** Eine Massenaktion, die eine Pflicht umgeht, die fuer die Einzelaktion
gilt, ist eine Inkonsistenz, die frueher oder spaeter jemand ausnutzt oder als Fehler meldet. Mein
fachliches Bedenken bleibt richtig — die Sub-FAs haengen an verschiedenen Arbeitsbereichen, ein
Picker fuer alle ist oft nicht der endgueltige. **Aber das ist ein Argument gegen die REGEL, nicht
fuer ihre Umgehung.** Zeigt die Praxis, dass die Vorbelegung staendig nachkorrigiert wird, ist das
der Anlass, die Zuweisungspflicht selbst zu ueberdenken — dann bewusst und fuer alle Freigabewege.
Dasselbe Muster wie beim Werkbank-Umschaltpunkt B→C: beobachten, dann entscheiden, nicht vorab
ausweichen.

### Zu B3 — uebernommen: Risikoklasse an drei Stellen im Rumpf nachziehen.

Die AKE-View-Erweiterung ist **hausintern und planbar**, keine externe Abhaengigkeit. Fachliche
Anforderungen, Out-of-Scope und Deploy-Abschnitt sind entsprechend umzuschreiben — aus
„Sage-Betreuung, vor Umsetzungsbeginn abzustimmen" wird „eigene Aufgabe, folgt spaeter; Spalte
bleibt bis dahin fuer AKE leer".
Die beiden Absicherungen aus Antwort 8 gehen mit: **null-sicherer Lesepfad** (kein Filter, keine
Sortierung laeuft auf dem leeren Feld ins Leere) und der **UAT-Vermerk** „Matchcode bei AKE leer —
kein Fehler".

### Zu B4 — bestaetigt, Kopfzeile behaelt Kunde und Leittermin.

Wie in Antwort 2: `affected_code` und Loesungsentwurf sagen „nur noch HauptFA-Nummer, Sub-FA-Zahl,
Mehrdeutig-Badge" — das ist zu korrigieren. **Kunde und ein Leittermin (Fert.-Termin) bleiben**,
weil die Kopfzeile bei zugeklappter Gruppe das Einzige ist, was sichtbar bleibt.

### Zu B5 — uebernommen: AK 5 braucht ein anderes Beispiel.

Guter Fund. Ein Kombigeraet kann den „Wert wechselt"-Fall nicht illustrieren, weil die
K1-Zeilenspalten dort laut Antwort 3 **leer bleiben** — es gibt keinen Wert, der wechseln koennte.
AK 5 ist auf eine **normale, eindeutige Gruppe** umzuschreiben (zwei aufeinanderfolgende Zeilen
mit gleichem Kunden, davon zeigt nur die erste den Wert).
Fuer Kombigeraete gehoert stattdessen ein **eigenes** AK: K1-Zeilenspalten bleiben leer, die
Kopfzeile zeigt alle Varianten plus Badge.

### Zu den SOLLTE-/HINWEIS-Punkten

Uebernommen, ohne Einzelvorbehalt — inkl. der Zitatkorrektur §10 → §3. Sie gehen mit der
Nachbesserung mit.

**Nach der Nachbesserung ist die Spec freigabereif** — dann Status, Freigabe-Felder und Ordner
gemeinsam umstellen.

## Kritische Pruefung (2026-09-10)

Alle 11 Rueckfragen sind beantwortet, aber die Antworten wurden **nicht in den Fliesstext
zurueckgezogen** — an mehreren Stellen widerspricht der bereits geschriebene Spec-Rumpf (Umfang,
Fachliche Anforderungen, Technischer Loesungsentwurf, Akzeptanzkriterien, Test-Szenarien,
affected_code) den beantworteten Rueckfragen. Der eine vom Menschen bereits gefundene Widerspruch
(Kopfzeile) ist nicht der einzige — vier weitere Abweichungen wurden gefunden, zwei davon am
Code verifiziert.

### BLOCKER

1. **Kopfzeile-Widerspruch (vom Menschen bereits gefunden, hier bestaetigt und lokalisiert).**
   `affected_code` (Eintrag zu `Views/ProductionOrders/Index.cshtml`) und **Fachliche Anforderungen
   Punkt 2** sagen als Tatsachenbehauptung „nur noch HauptFA-Nummer, Sub-FA-Zahl-Badge und
   Mehrdeutig-Badge **bleiben sicher bestehen**" — Antwort 2 legt fest, dass **zusaetzlich** Kunde
   und der Fert.-Termin bewusst in der Kopfzeile bleiben (Begruendung: zugeklappte Gruppe zeigt
   sonst nur Nummern). Akzeptanzkriterium 8 und das Test-Szenario „Kopfzeile schlanker" sind mit
   „mindestens" bzw. „gemaess Rueckfrage 2" so vage formuliert, dass sie den Widerspruch nicht
   selbst auffangen — ein Dev-Lauf, der nur AK 8 liest, koennte Kunde/Termin trotzdem entfernen.
   **Frage an den Menschen:** keine neue Entscheidung noetig, nur Bestaetigung, dass die
   Nacharbeit (Fliesstext auf Antwort 2 ziehen, AK 8 und Testszenario konkretisieren:
   „HauptFA-Nummer, Sub-FA-Zahl, Kunde, EIN Fert.-Termin, Mehrdeutig-Badge — genau diese fuenf,
   nicht mehr/weniger") vor Freigabe erledigt wird.

2. **„Nur wo der Wert wechselt" — Technischer Loesungsentwurf widerspricht Antwort 4 fundamental,
   und die dort gegebene Begruendung ist sachlich falsch angewandt.** Der Abschnitt „Technischer
   Loesungsentwurf" schreibt: *„Berechnung NACH Sortierung, serverseitig ... **kein clientseitiges
   Verstecken per JS**, weil das mit Server-Spaltenfiltern/-Sortierung (ADR 0005 Server-Mode)
   kollidieren wuerde (bereits dokumentierter Fallstrick 'Server-Filter-Mode: kein clientseitiges
   `applyFilters()` beim Init')."* Antwort 4 legt das **exakte Gegenteil** fest: „Client-seitig,
   ausgeloest nach JEDEM Reorder — und der Wert bleibt im DOM", mit der ausdruecklichen Begruendung,
   dass die Sortierung selbst bereits client-seitig laeuft (`table-filter.js`, URL bleibt
   unveraendert).
   **Verifiziert am Code:** `Views/ProductionOrders/Index.cshtml` traegt zwar
   `data-server-column-filter="true"`, aber `table-filter.js` sortiert nachweislich client-seitig
   (`sortTable()`, DOM-Reorder ohne Page-Reload) — Antwort 4 trifft technisch zu. Der zitierte
   Fallstrick (`secondbrain/architektur/fallstricke.md` Zeile 317-320, „Server-Filter-Mode: kein
   clientseitiges `applyFilters()` beim Init") bezieht sich auf `applyFilters()` — das **Filtern**
   per DOM-Zellentext-Matching, das Datumsspalten falsch interpretiert — **nicht** auf ein
   client-seitiges Dimmen/Verstecken doppelter Zeilenwerte nach einem Resort. Die Begruendung im
   Technik-Abschnitt verwechselt zwei verschiedene Mechanismen.
   **Konsequenz:** Der komplette Absatz „`Nur wo der Wert wechselt`" im Technischen
   Loesungsentwurf muss neu geschrieben werden: client-seitige Funktion, aufgerufen nach
   Init/Sortierung/Filter-Reload (dieselben Aufrufstellen wie die Sortierung selbst), Dimmen statt
   `display:none`/leerer Zellinhalt (Wert bleibt fuer Copy/Export im DOM), **kein** serverseitiger
   Ansatz.

3. **AKE-Matchcode-Abhaengigkeit: Antwort 8 aendert die Risikoklasse, der Fliesstext zieht das
   nicht nach.** Antwort 8 stellt fest: „Die AKE-View wird **hausintern** um den Matchcode
   erweitert ... Die Erweiterung liegt **bei uns selbst, nicht bei einem Dritten**." Drei Stellen
   im Rumpf behandeln das weiterhin als **externe** Abhaengigkeit mit **Sage-Betreuung**:
   - Fachliche Anforderungen Punkt 4: „AKE-Quelle ungeklaert, moegliche externe Abhaengigkeit ...
     ist die Sage-View-Erweiterung ein Fremd-DB-Objekt ... nicht durch einen Dev-Lauf loesbar,
     sondern mit der Sage-Betreuung abzustimmen."
   - Umfang/Out-of-Scope: „AKE-Sage-View-Erweiterung selbst ... das ist eine externe Abhaengigkeit
     mit der Sage-Betreuung, nicht durch einen Dev-Lauf loesbar."
   - Deploy/Deploy-Vorbedingung: „Falls Rueckfrage 8 ergibt, dass die AKE-Sage-View erweitert
     werden muss, ist DAS eine separate, extern abzustimmende Aenderung mit eigenem Zeitplan."
   Das ist kein Formulierungsdetail — es aendert, ob diese Spec auf einen externen Dritten wartet
   oder auf eine eigene, planbare Folgearbeit. **Frage an den Menschen:** Wer genau pflegt
   `vw_AKE_Kommissionierung_WAListe`/`_StuecklistenDB` „hausintern" — ein internes IDEAL-AKE-Team,
   das nicht die Sage-Betreuung ist? Und: Antwort 7 verlangt fuer die AKE-View-Erweiterung „eine
   eigene Backlog-Notiz" — ist die bereits angelegt oder Teil der Nacharbeit vor Freigabe?

4. **Freigabe-Kaskade „ohne Pflicht-Zuweisung" widerspricht verifiziertem Code-Verhalten — und
   Antwort 5 hat diesen Fall selbst als Ausschlusskriterium benannt.** Antwort 5 sagt: „Kaskade
   ohne Pflicht-Zuweisung; ein OPTIONALER Picker im Dialog", aber auch: „**ZU PRUEFEN vor der
   Umsetzung:** Blockiert `KommissionierungMitZuweisung` die Freigabe ohne Picker ... ? Blockiert
   es hart, ist die Kaskade ohne Zuweisung **nicht moeglich** — dann melden statt die bestehende
   Regel aufzuweichen."
   **Verifiziert am Code** (`PickingLeitstandController.cs`, Worktree
   `2026-08-07-ideal-teile-1-5`): `ToggleRelease` (Zeile 346-351) und `BulkRelease` (Zeile 391-396)
   blocken **hart** — bei aktivem `KommissionierungMitZuweisung` und fehlendem `assignedPickerId`
   wird die Freigabe abgelehnt (`TempData["WarningMessage"] = "Bitte einen Kommissionierer
   zuweisen."`, Redirect ohne Freigabe). Die in Antwort 5 selbst gestellte Pruefbedingung trifft
   also zu.
   **Frage an den Menschen:** Damit ist „Kaskade ohne Pflicht-Zuweisung" in der jetzigen Form nicht
   umsetzbar, ohne entweder (a) die bestehende Einzelfreigabe-Regel aufzuweichen — von Antwort 5
   ausdruecklich abgelehnt — oder (b) bei aktivem `KommissionierungMitZuweisung` auch die Kaskade
   zu blockieren bzw. dort eine Pflicht-Zuweisung (fuer alle kaskadierten Sub-FAs gemeinsam?)
   einzufuehren. Welche Variante gilt?

5. **Akzeptanzkriterium 5 widerspricht Antwort 3.** AK 5 nennt als Beispiel fuer einen Wertwechsel:
   „wechselt der Wert (z. B. bei einem Kombigeraet mit unterschiedlichen Montage-Abteilungen je
   Variante), erscheint er erneut." Antwort 3 legt aber fest, dass die K1-Zeilenspalten bei
   `IsAmbiguous=true` **grundsaetzlich leer bleiben** — „keine Zuordnung erfinden ... kein
   Trennschluessel auf Positionsebene". Es gibt bei einem Kombigeraet also keinen „gewechselten
   Wert" zu zeigen, sondern durchgehend leere Zellen. Das AK-Beispiel muss ersetzt werden (z. B.
   durch zwei aufeinanderfolgende, **nicht mehrdeutige** HauptFA-Gruppen mit unterschiedlicher
   Montage-Abteilung).

### SOLLTE

- **Konditionale Formulierungen im Fliesstext nicht auf den beantworteten Stand gezogen.** Mehrere
  Stellen sprechen noch von „ist Rueckfrage X", „zu verifizieren, Rueckfrage 1" (Fachliche
  Anforderungen Punkt 2 zu Antwort 1; Umfang/Fachliche Anforderungen Punkt 1 „ist Rueckfrage 3/4";
  Akzeptanzkriterium 7 „Sofern Rueckfrage 1 bestaetigt ..."), obwohl die Antworten vorliegen. Ein
  Dev-Lauf, der den Fliesstext statt nur den Rueckfragen-Block liest, sieht zwei sich
  widersprechende Versionen im selben Dokument. Empfehlung: vor Freigabe den gesamten Fliesstext
  auf den beantworteten Stand nachziehen, nicht nur den Rueckfragen-Block ergaenzen. Antwort 1
  enthaelt zudem einen expliziten Vorbehalt („am Code gegenpruefen, nicht auf diese Beobachtung
  bauen") — der sollte als Verifikationsschritt im Dev-Lauf (z. B. erster Satz unter „Technischer
  Loesungsentwurf" zu Punkt 2) stehen, nicht nur als Fussnote in der Antwort.
- **Repository-Methode fuer die Kaskade hat keinen Picker-Parameter.** Antwort 5 fordert „ein
  OPTIONALER Picker im Dialog, der auf alle wirkt". Die im Technischen Loesungsentwurf skizzierte
  `SetReleaseForOrderNumberAsync(orderNumber, releasedBy, modifiedBy, modifiedByWindows)` hat dafuer
  keinen Parameter und keine Beschreibung, wie/ob `SetAssignedPickerAsync` fuer alle betroffenen
  Zeilen mit aufgerufen wird. Sollte ergaenzt werden, sobald BLOCKER 4 geklaert ist.
- **Reihenfolge der Prioritaetsvergabe fehlt im Technik-Abschnitt.** Antwort 6 legt „Reihenfolge
  innerhalb des Batches: `SubOrderNumber` aufsteigend" fest; der Technische Loesungsentwurf nennt
  nur `GetMaxPickingPriorityAsync`, aber nicht die Sortierreihenfolge beim Vergeben der
  fortlaufenden Prioritaet — sollte explizit ergaenzt werden, sonst ueberlaesst der Dev-Lauf das
  der zufaelligen DB-Lesereihenfolge.
- **Fallstricke-Zitat „§10" fuer die Spaltenkonfigurations-Pflicht ist falsch adressiert.**
  Verifiziert: `secondbrain/architektur/fallstricke.md` §10 behandelt „IDEAL — BOM-Bridge"
  (`BomItem`-Erweiterung, `FullStructure`-Positionen etc.) — nicht Spaltenkonfiguration. Die Regel
  „`data-col-key` ist Pflicht auf allen `<th>`" bzw. „Neuer `viewKey` ohne
  `ColumnDefinitions.GetByViewKey`-Registrierung → Prefs-API 400" steht in **§3 „Views, Forms,
  JavaScript"** (Zeilen 203-212). Der Fehler stammt bereits aus der Backlog-Notiz (dort ebenfalls
  „Fallstricke §10") und wurde unveraendert uebernommen — vor der Umsetzung richtigstellen, sonst
  schlaegt ein Dev-Lauf im falschen Abschnitt nach.

### HINWEIS

- **„F7-Regel" vs. „Fallstricke §7" vermengt vermutlich zwei verschiedene Nummerierungen.** Die
  Spec zitiert mehrfach „F7-Regel (leeres Feld darf Filter/Sortierung nicht ins Leere laufen
  lassen)" und daneben „vgl. Fallstricke §7-Referenz". `secondbrain/architektur/fallstricke.md`
  §7 ist tatsaechlich „Lager, Bestand, Bestellwesen" — ein anderes Thema. „F7" stammt vermutlich
  aus den F1-F8-Kurzregeln der Materialisierungs-Spec (dort projektintern durchnummeriert), nicht
  aus den Fallstricke-Paragraphen. Rein eine Verwechslungsgefahr beim Nachschlagen, keine
  inhaltliche Luecke — vor Freigabe kurz klarstellen, welche der beiden Nummerierungen gemeint ist.
- **SQL-Skriptnummer 91 kollisionsfrei, Stand heute.** Geprueft: Worktree
  `2026-08-07-ideal-teile-1-5` steht bei `90_InvertProductionOrderHierarchy.sql`, `main` bei
  `88_AllowMultipleDummyRequisitionItems.sql`, die beiden anderen offenen Worktrees
  (`2026-07-29-sage-lagerbuchungen`, `override-prepickingdays`) bei `83`/`81`. `SQL/91_...` ist
  damit aktuell kollisionsfrei — das uebliche Restrisiko einer Nummernverschiebung beim Merge
  eines anderen Branches zuerst besteht wie bei jedem offenen Buendel, ist aber keine neue Luecke
  dieser Spec.
- **Groesse.** 14 `affected_code`-Eintraege ueber Controller/Repository/Views/Migration/Testdoku,
  vier fachlich unabhaengige Themenbloecke (Zeilenwerte, HauptFA-Zeile, Freigabe-Kaskade,
  Matchcode). Das ist gross fuer einen einzelnen Dev-Lauf, liegt aber bewusst im bestehenden
  Buendel-Worktree, der bereits mehrere Etappen traegt — kein `split`/`epic`-Kandidat im
  Sinn einer neuen Struktur, sondern konsistent mit der dort bereits etablierten Praxis.
- **AK 3 verifiziert.** Der Akzeptanzkriterien-Nachweis „F1/F2-Regression (Matchcode)" ist
  plausibel und deckungsgleich mit dem bereits abgenommenen Muster der Materialisierungs-Spec —
  kein Befund.

**BEREIT ZUR FREIGABE:** NACHBESSERUNG NOETIG — fuenf Blocker, davon zwei sachlich verifizierte
Widersprueche zum bestehenden Code (Freigabe-Kaskade/Picker-Pflicht, Anzeige-Mechanik „nur wo der
Wert wechselt"), die den Dev-Lauf in eine andere Richtung schicken wuerden als von Antwort 4/5
verlangt. Der Fliesstext muss auf den beantworteten Stand gezogen werden, bevor die Spec nach
`specs/freigegeben/` verschoben wird.
