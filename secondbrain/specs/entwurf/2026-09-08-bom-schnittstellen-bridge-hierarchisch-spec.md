---
type: spec
title: "IDEAL: Stueckliste hierarchiefaehig ueber die Repository-Schnittstelle (Bridge statt Cache-Kopie)"
slug: 2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec
status: Entwurf
created: 2026-09-08
updated: 2026-09-08
source_backlog: "[[2026-09-08-bom-schnittstellen-bridge-hierarchisch]]"
supersedes: "[[2026-08-18-ake-view-abhaengigkeiten-hierarchisch-spec]]"
depends_on: "[[2026-08-18-fa-liste-hierarchie-anzeige-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - "IdealAkeWms/Data/Repositories/FaHierarchyBomRepository.cs (NEU) — implementiert IBomRepository UND IBomCacheRepository, liest ueber IFaHierarchyNodeRepository/CachedFaHierarchyNodeRepository (Teil 1, 5-min-Cache-Decorator, Methoden GetByHauptFaAsync(int)/GetAllAsync — verifiziert IFaHierarchyNodeRepository.cs) + IFaHierarchyOrderInfoRepository fuer den Kopf-Artikel der Wurzel (HauptArtnr); KEIN eigener MemoryCache-Decorator. Muss int.TryParse zwischen ProductionOrder.SubOrderNumber (string, Models/ProductionOrder.cs:20) und FaHierarchyNode.HauptFA/VaterFA/SubFA (int, Models/FaHierarchyNode.cs) machen, analog FaMaterializationSyncService (Teil 7)."
  - "IdealAkeWms/Data/Repositories/HierarchicalBomGuardRepository.cs (ENTFERNEN) + IdealAkeWms.Tests/Repositories/HierarchicalBomGuardRepositoryTests.cs (entfernen) — verifiziert: implementiert HEUTE nur IBomRepository, NICHT IBomCacheRepository (Datei gelesen)."
  - "IdealAkeWms/Data/Repositories/IBomRepository.cs (verifiziert, 1 Methode: Task<BomQueryResult> GetBomItemsAsync(string productionOrderArticleNumber)) — Signatur auf BomKey key, BomScope scope = BomScope.DirectChildren umstellen."
  - "IdealAkeWms/Data/Repositories/IBomCacheRepository.cs (verifiziert, 7 Methoden: GetByArticleNumberAsync, GetArticleNumbersWithCoatingPartsAsync, GetHeaderHashesAsync, UpsertBomAsync, DeleteOrphansAsync, GetDeviceArticleNumbersByComponentAsync, GetComponentMengePerDeviceAsync) — nur GetByArticleNumberAsync bekommt BomKey (Aufrufer: BomRepository.cs:25); die vier Sync-/Cache-internen Methoden (GetArticleNumbersWithCoatingPartsAsync/GetHeaderHashesAsync/UpsertBomAsync/DeleteOrphansAsync) werden durch Design H (Klasse-D-Gate direkt in den Service-Aufrufern) ohnehin im hierarchischen Modus nie erreicht — dort KEIN Signaturzwang, nur No-op-Sicherheitsnetz falls doch aufgerufen. GetDeviceArticleNumbersByComponentAsync/GetComponentMengePerDeviceAsync bleiben in der Signatur unveraendert (Aufrufer kennt den BomKey nicht im Voraus, siehe Design G) — FaHierarchyBomRepository implementiert sie mit eigener Reverse-Lookup-Logik gegen FaHierarchyNode."
  - "IdealAkeWms/Data/Repositories/BomRepository.cs (Zeile 22 GetBomItemsAsync, Zeile 25 ruft _bomCacheRepository.GetByArticleNumberAsync auf; Zeile 41 [ake].[dbo].[vw_AKE_Kommissionierung_StuecklistenDB] — Sweep-Fund 1) + CachedBomRepository.cs (Zeile 18/25) — Signatur mechanisch auf BomKey umgestellt, liest intern nur BomKey.ArticleNumber, ignoriert SubOrderNumber + BomScope -> bit-identisch. OSEON-Connection-Konstruktor-Fruehzugriff (F6 der Alt-Spec) bleibt unveraendert, da BomRepository im hierarchischen DI-Zweig gar nicht mehr aufgeloest wird (siehe Design A, Program.cs)."
  - "IdealAkeWms/Models/BomKey.cs (NEU, sealed record BomKey(string ArticleNumber, string? SubOrderNumber = null))"
  - "IdealAkeWms/Models/BomScope.cs (NEU, enum DirectChildren/FullStructure)"
  - "IdealAkeWms/Models/ViewModels/BomViewModels.cs (verifiziert: record BomQueryResult(List<BomItem> Items, string DataSource) — neuer optionaler dritter Parameter MengeIstAuftragsmenge = false, damit AKE-Erzeuger BomRepository.cs:22/CachedBomRepository.cs:18 unveraendert bleiben; BomItem-Klasse Zeile 21-32 unveraendert im Feldbestand, aber siehe Design B fuer die inhaltliche Baugruppe-Befuellung)"
  - "IdealAkeWms/Services/BomQuantityResolver.cs (NEU, statischer Helper: flag ? item.Menge : item.Menge * order.Quantity)"
  - "IdealAkeWms/Controllers/PickingController.cs — 3 IBomRepository-Aufrufer verifiziert: Zeile 300 (Bom-Action) + Zeile 396 Menge = bom.Menge * order.Quantity (EXAKT wie im Backlog zitiert), Zeile 548 (PrintBom, Action beginnt Zeile 539) + Zeile 581 Menge = bom.Menge * order.Quantity (EXAKT wie im Backlog zitiert), Zeile 644 (PrintPicking, Action beginnt Zeile 617 — ruft GetBomItemsAsync NUR zur Bezeichnung1-Anreicherung, KEINE Mengen-Multiplikation dort). Baugruppen-Erkennung Zeile 334-338 (baugruppen-HashSet aus bom.Baugruppe) + Zeile 402 IsBaugruppe — siehe Design B/E fuer die hierarchische Entsprechung. Scope-Entscheidung HauptFA/Sub-FA -> BomScope in der Bom-Action."
  - "IdealAkeWms/Services/ReadOnlyBomBuilder.cs — GENAU 5 Baugruppe-Verwendungen verifiziert (Zeilen 78/79 Aufbau des baugruppen-HashSet, 94 Zuweisung auf BomItemViewModel, 104 IsBaugruppe-Berechnung, 116 Filtertext-Suche); GetBomItemsAsync Zeile 64; Menge = bom.Menge * order.Quantity Zeile 98 (EXAKT wie im Backlog zitiert) -> BomQuantityResolver. Aufrufer FaWorklistController.Bom + FaCompletionController.Bom unveraendert (laufen ueber denselben Builder + dieselbe View)."
  - "IdealAkeWms/Controllers/ArticlesController.cs — Zeile 259 GetDeviceArticleNumbersByComponentAsync + Zeile 285 GetComponentMengePerDeviceAsync EXAKT wie im Backlog zitiert, einzige zwei Aufrufer im gesamten Worktree (verifiziert per Grep). Zeile 304-305: Hierarchical-Flag existiert BEREITS (Etappe-D-Nachtrag der FA-Liste-Hierarchie-Spec) und wird ins ArticleInfoViewModel gereicht — diese Spec baut darauf auf, fuehrt das Flag NICHT neu ein. Zeile 264 GetByArticleNumbersAsync(deviceArticleNumbers) matched auf ProductionOrder.ArticleNumber — fuer den hierarchischen Zweig NICHT wiederverwendbar (siehe Design G, Rueckfrage 6 beantwortet: eigene Reverse-Lookup-Methode noetig, die direkt (HauptFA, SubOrderNumber)-Paare liefert statt Artikelnummern zum Re-Lookup)."
  - "IdealAkeWms/Views/Articles/Info.cshtml — verifiziert Ist-Zustand (Zeile 148): im hierarchischen Modus wird HEUTE order.SubOrderNumber als PRIMAERER fett dargestellter Bezeichner gezeigt (Zeile 160-162: 'N Auftraege · M Sub-FAs offen'-Zusammenfassung existiert schon aus Etappe D). Entscheidung 2 dieser Spec KEHRT DAS UM: HauptFA wird primaere Geraet-Spalte, Sub-FA wird Zusatzspalte — echte Verhaltensaenderung gegenueber dem Ist-Zustand, keine Neueinfuehrung."
  - "IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs — Bom-ViewConfig EXAKT Zeile 219 verifiziert (bestehende Spalten: pick-control/position/assembly-group/resource-number/description1/description2/quantity/procurement/article-group/category/storage-location/source-location/order). 'assembly-group' (Baugruppe) existiert BEREITS — kein neuer Key noetig dafuer. NEU: 'kommissionieren' + 'hauptlagerplatz' (Bonus-Felder ohne AKE-Aequivalent) + bei FullStructure zusaetzlich 'ebene'/'vater-sub-fa', alle nur im hierarchischen Modus im ViewModel befuellt/gerendert."
  - "IdealAkeWms/Views/Picking/Bom.cshtml — Baumlogik verifiziert: parentPos wird rein aus item.Position per String-Split am letzten Punkt berechnet (Zeile 185-188), NICHT aus item.Baugruppe; data-position (Zeile 197), data-parent-pos, data-baugruppe-id (Zeile 199); JS getChildRows/expandBaugruppeStateOnly/collapseBaugruppeStateOnly (Zeile 858-904) lesen ausschliesslich data-parent-pos/data-position, ebenfalls rein positionsbasiert. Design E entsprechend korrigiert: siehe dort."
  - "IdealAkeWms/Models/PickingItem.cs (verifiziert: BomPosition ist [StringLength(50)] string?, Zeile 18-19 — KEINE Migration fuer den zusammengesetzten Schluessel noetig, Feld ist bereits ausreichend gross)"
  - "IDEALAKEWMSService/Services/CoatingDetectionService.cs (verifiziert: Konstruktor hat bereits IConfiguration; Zeile 95 [ProductionDate] IS NOT NULL EXAKT wie im Backlog zitiert — 'zufaelliger' Schutz; Zeile 137-144 Kategorie-Match-SQL [Backlog zitierte 139-155, tatsaechliche Zeilen nach Worktree-Stand 137-144] — harter Code-Gate direkt nach BeginRunAsync einziehen, analog dem bestehenden Skip-Muster bei leerem LackierteilKategorieName [Zeile 52-62]: FinishSuccessAsync mit Null-Counts + return, KEIN throw)"
  - "IDEALAKEWMSService/Services/IFaWorkStepDetectionService.cs + FaWorkStepDetectionService.cs (verifiziert: GENAU 1 Methode DetectAsync; Konstruktor hat HEUTE NUR ApplicationDbContext/ILogger/ISyncLogger, KEIN IConfiguration -> Konstruktor-Signaturaenderung noetig, IConfiguration VOR ISyncLogger einfuegen [Fallstrick: ISyncLogger bleibt letzter Parameter]; Zeile 62-64 Bezeichnung1/2.ToLower().Contains(term) EXAKT wie im Backlog zitiert [62-65])"
  - "IDEALAKEWMSService/Services/IBomCacheSyncService.cs + BomCacheSyncService.cs — verifiziert ZWEI oeffentliche Einstiege (SyncBomCacheAsync UND SyncSpecificArticleNumbersAsync, letztere aufgerufen von SageImportService.cs:194 bei neu importierten Artikelnummern OHNE Window-Filter) — BEIDE brauchen den Klasse-D-Gate, nicht nur SyncBomCacheAsync. SyncBomCacheAsync-Fensterabfrage Zeile 404 hat DENSELBEN zufaelligen po.[ProductionDate] IS NOT NULL-Schutz wie CoatingDetectionService; Zeile 276 [ake].[dbo].[vw_AKE_Kommissionierung_StuecklistenDB] — Sweep-Fund 2."
  - "IdealAkeWms/Program.cs — DI-Registrierung verifiziert Zeile 91 (IBomCacheRepository -> BomCacheRepository, HEUTE UNGUARDED — kein Guard auf diesem Interface!) + Zeile 92-98 (IBomRepository -> HierarchicalBomGuardRepository als Factory um CachedBomRepository via konkretem Typ). Diese Spec ersetzt BEIDE Registrierungen (Zeile 91 UND 92-98) durch eine gemeinsame Weiche: Master true -> FaHierarchyBomRepository (fuer beide Interfaces); Master false -> unveraendert CachedBomRepository/BomCacheRepository."
  - "IdealAkeWms/Views/Shared/_Layout.cshtml — verifiziert: standalone Teil-3-Nav-Item Zeile 112-120 (Kommissionierlisten, eigener top-level <li>) UND separates Kommissionierung-Nav-Item Zeile 177-189 (Picking-Workflow, einfacher <a>-Link mit Badge, KEIN Dropdown). Aenderung: Zeile 177-189 zu einem Dropdown umbauen (wie das Leitstand-Dropdown Zeile 165-176 als Vorbild), zwei Eintraege 'Kommissionierung' (Picking/Index) + 'Kommissionierlisten' (FaHierarchyKommissionierListen/Index); Zeile 112-120 als eigenstaendiges Item entfernen. Gates unveraendert (canPick bzw. canProcessLager && faKommListenAktiv)."
  - "docs/TESTSZENARIEN.md (neues Kapitel, TS-70)"
  - "secondbrain/tests/testszenarien-index.md"
open_questions:
  - "BomPosition-Zusammensetzung in FullStructure: Code-Befund (Bom.cshtml Zeile 185-188, 858-904) zeigt, dass TreeLevel UND parentPos ausschliesslich aus der Punktzahl bzw. dem letzten Punkt-Segment von item.Position berechnet werden — GENAU wie die AKE-View es fuer eine EINZIGE, bereits vollstaendig rekursive Position liefert (z. B. 15.1.1, siehe [[integrationen]]). Ein flacher Praefix-Schluessel 'VaterSubFA.Position' (EIN zusaetzliches Segment) wuerde parentPos auf VaterSubFA strippen — das ist keine gueltige Position einer anderen Zeile, die Baumlogik braeche. Empfehlung dieser Spec (ersetzt die urspruengliche Idee aus dem Backlog): BomPosition fuer FullStructure als VOLLSTAENDIGEN rekursiven Pfad ab der Wurzel aufbauen (z. B. '3.7.2' — je Ebene die eigene FaHierarchyNode.Position-Ganzzahl anhaengen), dann funktionieren TreeLevel/parentPos/data-parent-pos in Bom.cshtml UNVERAENDERT, exakt wie im AKE-Fall — kein JS-Umbau noetig. Bestaetigung durch den Menschen ausstehend."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
# Flache Schluessel mit Absicht: Obsidians Property-Editor kann verschachtelte
# YAML-Objekte NICHT bearbeiten - und genau diesen Block fuellt der Mensch aus.
---

> **Code-Verifikation dieser Spec:** Alle Datei-/Zeilenangaben sind gegen den Worktree
> `feature/2026-08-07-ideal-teile-1-5` @ Commit `dde9a17` (`C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5`)
> per Grep/Read verifiziert (2026-09-08), nicht nur aus dem Backlog uebernommen. Die Umsetzung
> selbst erfolgt **nach dem Buendel-Merge aus `main`**, in einem **neuen** Worktree — Zeilennummern
> koennen sich bis dahin durch zwischenzeitliche Aenderungen verschoben haben; die Klassen-/Methoden-
> /Feldnamen und die beschriebene Semantik sind der belastbare Teil, Zeilenangaben ein Hinweis auf
> den Stand vom 2026-09-08.

## Ziel / Nutzen (das Warum)

Die gesamte AKE-Maschinerie, die auf der Stueckliste aufsetzt — **Picking-Workflow** (Pick-Status je
Zeile), Vorbau-Stueckliste (`ReadOnlyBomBuilder`), Bedarfsmeldung/Lagerbestellung aus einer Zeile,
Artikelinfo ("in welchen Geraeten kommt Artikel X vor") — soll fuer IDEAL-Auftraege im
hierarchischen Modus **unveraendert nutzbar** sein. Heute endet der BOM-Knopf im hierarchischen
Modus in einem reinen Hinweis (`HierarchicalBomGuardRepository`, Minimal-Guard,
[[2026-08-18-bom-guard-hierarchisch-spec]], TS-68); die Kommissionierung bei IDEAL laeuft bislang
ausschliesslich ueber die Teil-3-Kommissionierliste (`/FaHierarchyKommissionierListen`).

Diese Spec ersetzt den Guard durch eine **echte zweite Implementierung der bestehenden
`IBomRepository`/`IBomCacheRepository`-Schnittstelle**, die aus der bereits lokal vorliegenden
Struktur (`FaHierarchyNode`, Teil 1) liest — **keine** Kopie in den persistenten AKE-BOM-Cache.

**Wichtiger Zusatzbefund aus der Code-Verifikation:** Der heutige Guard
(`HierarchicalBomGuardRepository`) deckt **nur** `IBomRepository` ab. `IBomCacheRepository` ist in
`Program.cs:91` **ungeschuetzt** registriert — Artikelinfo (`ArticlesController.GetDeviceArticleNumbersByComponentAsync`/
`GetComponentMengePerDeviceAsync`) laeuft im hierarchischen Modus heute bereits, liefert aber leere
Ergebnisse, weil der persistente BOM-Cache fuer IDEAL-Auftraege nie befuellt wird (derselbe
zufaellige `ProductionDate IS NOT NULL`-Ausschluss wie bei `CoatingDetectionService`, siehe Design H).
Kein Absturz, aber ein stiller funktionaler Leerlauf — diese Spec schliesst auch diese Luecke.

**Nicht-Ziel — bewusst verworfen: den persistenten BOM-Cache (`CachedBomHeader`/`CachedBomItem`)
aus der Hierarchie zu befuellen.** Das war die urspruengliche Idee der superseded Alt-Spec
([[2026-08-18-ake-view-abhaengigkeiten-hierarchisch-spec]]) und ist nach kritischer Pruefung im
Worktree am 2026-09-08 verworfen worden, aus vier Befunden:

1. **Alle Web-Verbraucher haengen an EINER Schnittstelle.** Picking, Vorbau, Fehlteile-Kontext,
   Bedarfsmeldungen, Lagerbestellungen, Artikelinfo gehen ausnahmslos ueber
   `IBomRepository`/`IBomCacheRepository` mit der einzigen Implementierung `BomCacheRepository`
   (verifiziert: `IBomRepository` hat genau 1 Methode, `IBomCacheRepository` genau 7 — keine
   weiteren Web-Raw-SQL-Zugriffe auf die Cache-Tabellen gefunden). Eine zweite
   Schnittstellen-Implementierung deckt damit alles ab — ein Cache-Umweg ist unnoetig.
2. **Die Cache-Semantik passt nicht zu IDEAL.** Der persistente Cache ist auf **Artikelnummer**
   geschluesselt und fuehrt `Menge` **je Stueck** (Verbraucher rechnen mit der Auftragsmenge hoch,
   verifiziert `PickingController.cs:396`/`:581`, `ReadOnlyBomBuilder.cs:98`). IDEAL-`Sollmenge` ist
   bereits die **Auftragsgesamtmenge**, und Positions-Attribute sind auftragsspezifisch. Ein
   artikel-geschluesselter Cache wuerde doppelt multiplizieren und zwei FAs desselben Artikels
   dieselbe Stueckliste zuweisen — stille Falschdaten.
3. **Die Struktur ist bereits lokal.** Der AKE-Cache existiert nur, weil die Sage-BOM-View ein
   teurer Fremdzugriff ist. `FaHierarchyNode` liegt lokal, indiziert, per Full-Refresh alle 15
   Minuten aktuell (Cache-Decorator `CachedFaHierarchyNodeRepository`, Teil 1). Eine Kopie in den
   Cache waere Duplikation mit Lag, Purge-/Hash-Logik und Vergiftungsrisiko ohne Nutzen.
4. **Zwei Verbraucher-Klassen.** Klasse P (Praesentation/Workflow, z. B. Picking) laesst sich ueber
   die Schnittstelle bedienen. Klasse D (Ableitung: `CoatingDetectionService`,
   `FaWorkStepDetectionService`) sind AKE-Heuristiken (Artikelkategorie-Matching,
   Text-Contains-Suche, verifiziert), die auf IDEAL-Daten Fehldaten erzeugen wuerden — IDEAL liefert
   die entsprechende Wahrheit explizit (`Beschichtet`, `Arbeitsschritte`, FAInfos). Klasse D wird
   fuer hierarchische Auftraege **hart abgeschaltet**, nicht auf die Struktur umgestellt.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**

1. `FaHierarchyBomRepository` als zweite Implementierung von `IBomRepository` **und**
   `IBomCacheRepository`, DI-Weiche nach demselben Master-Resolver-Muster wie in den uebrigen
   IDEAL-Lesepfaden (`ProduktionsauftragHierarchisch`), kein Cache-Decorator im hierarchischen Pfad.
2. Vollstaendige Ablösung von `HierarchicalBomGuardRepository` (der Minimal-Fix aus
   [[2026-08-18-bom-guard-hierarchisch-spec]]) **und** Schliessung der bisher ungeschuetzten
   `IBomCacheRepository`-Registrierung; TS-68 wird durch neue Szenarien (TS-70) abgeloest.
3. Schluesselwechsel `BomKey(ArticleNumber, SubOrderNumber)` statt `string articleNumber`, plus
   `BomScope` (DirectChildren/FullStructure) — mechanische Anpassung aller Aufrufer (verifizierte
   Liste siehe `affected_code`).
4. Selbstbeschreibendes Mengen-Flag `BomQueryResult.MengeIstAuftragsmenge` + zentraler Helper, EINE
   Implementierung statt verstreuter Multiplikationen (genau 2 Multiplikationsstellen verifiziert:
   `PickingController.cs:396`/`:581`, plus `ReadOnlyBomBuilder.cs:98`).
5. Feld-Mapping AKE-View → `FaHierarchyNode` (korrigiert gegenueber der Alt-Spec **und** gegenueber
   dem urspruenglichen Entwurf dieser Spec, siehe Design B).
6. Picking-Workflow-Scope: HauptFA → `FullStructure` (alle Ebenen, flach, mit zusammengesetztem
   Zeilenschluessel), Sub-FA → `DirectChildren`.
7. `Kommissionieren`-Spaltenfilter + `Hauptlagerplatz` in `Bom.cshtml`, nur hierarchisch gerendert.
8. Artikelinfo-Umbau: HauptFA als Geraet, Sub-FA als Zusatzspalte, Partition IMMER
   `DirectChildren` — **kehrt den heutigen Etappe-D-Zwischenstand um** (aktuell zeigt
   `Views/Articles/Info.cshtml` `SubOrderNumber` als primaeren Bezeichner, verifiziert).
9. Klasse-D-Code-Gates in `CoatingDetectionService`, `FaWorkStepDetectionService` sowie **beiden**
   oeffentlichen Einstiegen von `BomCacheSyncService` (`SyncBomCacheAsync` **und**
   `SyncSpecificArticleNumbersAsync` — Letztere wird von `SageImportService.cs:194` ohne
   Window-Filter aufgerufen und braucht denselben Gate).
10. Navigation: Kommissionierliste (Teil 3) unter den Menuepunkt „Kommissionierung" neben den
    Picking-Workflow (Umbau des heutigen einfachen Links `_Layout.cshtml:177-189` zu einem Dropdown).
11. Sweep aller `vw_AKE_`/`[ake].[dbo]`-Fundstellen (Web + Service) — **verifiziert abgeschlossen**,
    siehe Design I: genau 3 Fundstellen, alle bereits bekannt und bewertet.

**Out-of-Scope (bewusst, eigener Umfang)**

- Arbeitsgaenge (`FaWorkSteps`) aus `Arbeitsschritte` ableiten — eigener Folgeblock
  [[2026-09-08-arbeitsgaenge-aus-arbeitsschritte]], erst nach dieser Spec **und** Thema 2
  spezifizieren.
- `HasCoatingParts`/Beschichtungstermin-Materialisierung (K2) — gehoert zu
  [[2026-08-20-materialisierung-fachliche-felder-spec]]; diese Spec liefert dafuer nur die
  Abschalt-Gates (Design H), nicht die Ersatzquelle.
- Der `OrderNumber`-Sweep aus Teil 7 Etappe D (mehrdeutige Lookups, Klasse 1) — bereits behoben.
- Der `vw_AKE_Kommissionierung_WAListe`-Fund in `SageImportService.cs:57` — bereits als
  Review-Finding H1 separat dokumentiert (config-gesichert, kein Code-Guard); **nicht** Teil dieser
  Spec, siehe Design I.
- Schreibpfade nach Sage (Lagerbuchungen) — unveraendert.
- PDF-Erzeugung, Isolierfraesen-Export, Kombigeraete-Montageabteilung — separate Backlog-Punkte.

## Fachliche Anforderungen

Die folgenden sechs Entscheidungen sind vom Menschen am 2026-09-08 getroffen und **nicht** Teil der
offenen Rueckfragen dieser Spec:

1. **Kommissionieren-Filter:** Picking-Stueckliste zeigt im hierarchischen Modus **alle** Zeilen;
   Spalte `Kommissionieren` (Kommissionier-Ziel) mit Spaltenfilter. Kein stiller Filter.
2. **Artikelinfo:** „in welchen Geraeten kommt Artikel X vor" zeigt im hierarchischen Modus
   **HauptFA** als Geraet und den **Sub-FA** (dessen direktes Kind X ist) als Zusatzspalte — kehrt
   den verifizierten Ist-Zustand um (siehe Umfang Punkt 8).
3. **Arbeitsgaenge aus `Arbeitsschritte`:** eigener Folgeblock, hier nicht spezifiziert.
4. **Koexistenz:** Teil-3-Kommissionierliste (Druck je Ziel) **und** AKE-Picking-Workflow
   (Status-Prozess) bleiben beide fuer IDEAL nutzbar, nebeneinander.
5. **Navigation:** Die Kommissionierliste wandert unter den Menuepunkt „Kommissionierung" neben den
   Picking-Workflow — gleiche Toggle-/Rollen-Gates, nur `_Layout.cshtml`.
6. **Scope im Picking-Workflow:** Auftrag ist HauptFA → komplette Struktur (alle Ebenen); Auftrag
   ist Sub-FA → nur dessen eigene Stueckliste (direkte Kinder).

## Technischer Loesungsentwurf

Referenzmuster: [[0001-repository-pattern-mit-decorator-fuer-caching]] (Repository/Decorator),
[[0007-bom-quelle-sage-view-mit-oseon-fallback]] (Quellen-Kennung im `BomQueryResult`),
[[0005-listen-view-pattern-mit-server-side-spaltenfilter]] (Client-Mode-Ausnahme fuer `Bom.cshtml`,
bereits etabliert). Betroffene Module: [[services]] (Repositories, `IBomRepository`-Familie),
[[datenmodell]] (`PickingItem`), [[controller]] (`PickingController`, `ArticlesController`,
`FaWorklistController`, `FaCompletionController`).

### A — Schnittstelle und Weiche

- Neue Implementierung `IdealAkeWms/Data/Repositories/FaHierarchyBomRepository.cs`, die sowohl
  `IBomRepository` als auch `IBomCacheRepository` bedient und ausschliesslich aus
  `IFaHierarchyNodeRepository`/`CachedFaHierarchyNodeRepository` (Teil 1, verifiziert:
  `GetByHauptFaAsync(int hauptFa)` + `GetAllAsync()`, 5-Minuten-Cache-Decorator bereits vorhanden)
  sowie `IFaHierarchyOrderInfoRepository` (Kopf-Artikel der Wurzel, `HauptArtnr`) liest. **Kein**
  zusaetzlicher `MemoryCache`-Decorator im hierarchischen Pfad.
- **Typkonvertierung:** `ProductionOrder.OrderNumber`/`SubOrderNumber`/`ParentSubOrderNumber` sind
  `string` (verifiziert `Models/ProductionOrder.cs`), `FaHierarchyNode.HauptFA`/`VaterFA`/`SubFA`
  sind `int` (verifiziert `Models/FaHierarchyNode.cs`). `FaHierarchyBomRepository` muss zwischen
  beiden konvertieren (`int.TryParse`), analog dem bereits bestehenden Muster in
  `FaMaterializationSyncService` (Teil 7) — kein neues Konzept, aber ein Implementierungsdetail, das
  nicht vergessen werden darf.
- Auswahl per Master `ProduktionsauftragHierarchisch` im DI-Resolver (`Program.cs`). **Ersetzt ZWEI
  bestehende Registrierungen**, nicht nur eine:
  - `Program.cs:91` `builder.Services.AddScoped<IBomCacheRepository, BomCacheRepository>();` — heute
    **ungeschuetzt**, kein Guard, keine Weiche.
  - `Program.cs:92-98` — `IBomRepository` ueber `HierarchicalBomGuardRepository`-Factory um
    `CachedBomRepository` (konkreter Typ, keine Selbstreferenz).

  Neu: **eine gemeinsame** Weiche fuer beide Interfaces — Master `true` → `FaHierarchyBomRepository`
  fuer `IBomRepository` **und** `IBomCacheRepository`; Master `false` → unveraendert
  `CachedBomRepository`/`BomCacheRepository`. Master-Zustand ueber den bestehenden, bereits an
  mehreren Stellen genutzten Leseweg (`HierarchischeStrukturKeys.Master` via
  `IServiceSettingRepository`) — kein neuer Settings-Key.
- Ersetzt `HierarchicalBomGuardRepository` **vollstaendig** (Datei + zugehoerige Tests entfernen,
  verifiziert: implementiert heute nur `IBomRepository`, kein `IBomCacheRepository`); TS-68 wird
  durch TS-70 abgeloest, im Index vermerken.
- Schluessel: `BomKey(string ArticleNumber, string? SubOrderNumber = null)`. Die flache
  AKE-Implementierung liest nur `ArticleNumber`, ignoriert `SubOrderNumber` und `BomScope`
  vollstaendig → **bit-identisch** zu heute. Die Signaturaenderung an den Aufrufern ist mechanisch —
  vollstaendige, code-verifizierte Aufrufer-Liste:

  | Datei:Zeile | Methode | Anmerkung |
  |---|---|---|
  | `PickingController.cs:300` | `Bom` (Action) | + Scope-Entscheidung HauptFA/Sub-FA |
  | `PickingController.cs:548` | `PrintBom` (Action ab 539) | Mengen-Multiplikation Zeile 581 |
  | `PickingController.cs:644` | `PrintPicking` (Action ab 617) | nur Bezeichnung1-Anreicherung, keine Mengen-Multiplikation |
  | `ReadOnlyBomBuilder.cs:64` | `BuildAsync` | Mengen-Multiplikation Zeile 98; Aufrufer `FaWorklistController.Bom`/`FaCompletionController.Bom` unveraendert |
  | `BomRepository.cs:22` (+`:25` ruft `IBomCacheRepository.GetByArticleNumberAsync`) | `GetBomItemsAsync` | AKE-Implementierung, Signatur mechanisch, Verhalten bit-identisch |
  | `CachedBomRepository.cs:18` (+`:25`) | `GetBomItemsAsync` | AKE-Cache-Decorator, dito |
  | `ArticlesController.cs:259` | `GetDeviceArticleNumbersByComponentAsync` | einziger Aufrufer (verifiziert per Grep) |
  | `ArticlesController.cs:285` | `GetComponentMengePerDeviceAsync` | einziger Aufrufer (verifiziert per Grep) |
  | Tests: `BomRepositoryCacheFirstTests.cs`, `BomCacheRepositoryTests.cs`, `HierarchicalBomGuardRepositoryTests.cs` (entfernen), `ReadOnlyBomBuilderTests.cs`, `FaWorklistControllerTests.cs`, `FaCompletionControllerTests.cs`, `ArticlesControllerTests.cs` | — | alle setzen/verifizieren `GetBomItemsAsync(It.IsAny<string>())` bzw. feste Artikelnummer-Strings — mechanisch auf `BomKey` umzustellen |

  Die vier reinen Sync-/Cache-internen `IBomCacheRepository`-Methoden
  (`GetArticleNumbersWithCoatingPartsAsync`, `GetHeaderHashesAsync`, `UpsertBomAsync`,
  `DeleteOrphansAsync`) haben **keine** Web-Aufrufer (nur `BomCacheSyncService`, Service-seitig, raw
  SQL/EF direkt — nicht ueber die Web-Schnittstelle) und werden durch Design H im hierarchischen
  Modus ohnehin nie erreicht; sie behalten ihre Signatur, bekommen aber die No-op-mit-Log-Sicherung.
- Schreibmethoden (`UpsertBomAsync`, `DeleteOrphansAsync`) auf `IBomCacheRepository` sind im
  hierarchischen Pfad **No-op mit Log**, nie Exception — als Verteidigungslinie, falls trotz Design H
  doch ein Aufruf durchrutscht.
- `BomQueryResult.DataSource = "FA-HIERARCHIE"` (Analogie zu `"SAGE"`/`"CACHE"` aus ADR 0007) macht
  die Quelle in Anzeige und Log erkennbar. Verifiziert: `BomQueryResult` ist ein `record` mit exakt
  zwei Positionsargumenten (`Items`, `DataSource`) — `MengeIstAuftragsmenge` wird als **dritter,
  optionaler** Parameter mit Default `false` ergaenzt, damit die bestehenden AKE-Erzeugerstellen
  (`BomRepository.cs:22`, `CachedBomRepository.cs:18`) unveraendert kompilieren.

### B — Feld-Mapping (nach Anhang [[sage-views-ideal]], **zweifach korrigiert**)

Die superseded Alt-Spec hatte `Artnr → Artikelnummer` gemappt und `Ressourcenummer` als fehlend
erklaert — sie hat Kopf- und Zeilenschluessel verwechselt. Im AKE-Cache ist `Artikelnummer` der
**Kopf** (wessen Stueckliste das ist) und `Ressourcenummer` die **Komponente je Zeile**. Anhang Z. 62:
FAListe-`Artnr` **ist** `KHKPpsFaBelegePositionen.RessourceNummer`.

**Zusaetzliche Korrektur aus der Code-Verifikation dieser Spec (beantwortet Rueckfrage 2 der
urspruenglichen Fassung):** `BomItem.Baugruppe` ist **kein** boolescher Baugruppen-Indikator — es ist
ein **String, der die Ressourcenummer/Artikelnummer der uebergeordneten Baugruppe traegt**
(bestaetigt durch `[integrationen]`: „Baugruppe | Artikelnummer der uebergeordneten Baugruppe").
Verifiziert an **allen 5** Verwendungsstellen in `ReadOnlyBomBuilder.cs` (Zeilen 78/79/94/104/116,
identisches Muster in `PickingController.cs:334-338`/`402`):

- Zeilen 78-79: `baugruppen`-`HashSet` wird aus **allen** `Baugruppe`-Werten des aktuellen
  Ergebnisses gebildet (Menge aller Eltern-Referenzen im Result-Set).
- Zeile 94: `Baugruppe` wird 1:1 auf `BomItemViewModel.Baugruppe` durchgereicht (reine Anzeige/Filter,
  siehe unten).
- Zeile 104: `IsBaugruppe = baugruppen.Contains(bom.Ressourcenummer)` — eine Zeile gilt als Baugruppe,
  wenn **irgendeine andere Zeile im selben Ergebnis** sie als ihre `Baugruppe` referenziert.
  `IsBaugruppe` ist **kein Feld von `BomItem`**, sondern eine im Controller/Builder abgeleitete
  Eigenschaft von `BomItemViewModel`.
- Zeile 116: `Baugruppe` fliesst zusaetzlich in die Freitextsuche (`filterText`).

**Wichtig — die Baumdarstellung selbst nutzt `Baugruppe` NICHT.** Verifiziert in `Bom.cshtml`: die
Eltern-Kind-Beziehung fuer Einklappen/Ausklappen wird **ausschliesslich** aus `item.Position`
berechnet (`parentPos = Position.Substring(0, Position.LastIndexOf('.'))`, Zeile 185-188), nicht aus
`Baugruppe`. `data-baugruppe-id="@item.Ressourcenummer"` markiert nur, DASS eine Zeile eine Baugruppe
IST (fuer das Chevron-Icon), nicht WESSEN Kind sie ist.

**Konsequenz fuer das Mapping:**

| BOM-Feld (AKE-View) | ← `FaHierarchyNode` | Anmerkung |
|---|---|---|
| Kopf `Artikelnummer` | Artikel des Elternknotens (`HauptArtnr` bei der Wurzel, sonst `Artnr` des Vater-Sub-FA) | |
| `Ressourcenummer` | `Artnr` | |
| `Position` | **fuer `DirectChildren`**: `Position` (int → string), eindeutig innerhalb der Abfrage. **Fuer `FullStructure`**: vollstaendiger rekursiver Pfad ab der Wurzel (siehe Design E, Offene Rueckfrage 1) — NICHT der rohe `FaHierarchyNode.Position`-Wert allein, sonst kollidieren gleiche Positionsnummern unter verschiedenen Eltern. |
| `Bezeichnung1`/`Bezeichnung2` | gleichnamig | |
| `Baugruppe` | Ressourcenummer/Artnr des **unmittelbaren** Elternknotens (fuer `DirectChildren`: der Artnr des aktuell abgefragten Kopf-Sub-FA, fuer alle Zeilen identisch; fuer `FullStructure`: je Zeile der Artnr des Knotens mit `SubFA == diese Zeile.VaterFA`, aufgeloest innerhalb des Ergebnisses) | `IsBaugruppe` bleibt eine abgeleitete Groesse in der aufrufenden Schicht wie bei AKE, ODER wird — da `FaHierarchyNode.SubFA` das direkt und zuverlaessig hergibt — vereinfacht als `SubFA != 0` berechnet (Dev-Lauf-Entscheidung; beide Wege sind fachlich aequivalent, `SubFA != 0` ist robuster, weil es nicht von Ressourcenummer-Eindeutigkeit im Result-Set abhaengt) |
| `Beschaffungsartikel` | bool → `Ja`/`Nein` (AKE-Format uebernehmen) | |
| `Artikelgruppe` | gleichnamig — **gleiches Format** `"CODE - Bezeichnung"` wie das bestehende AKE-Matching (Anhang Z. 68/241, Fallstrick „Artikelgruppe BOM vs Articles") | |
| `Menge` | `Sollmenge` (Auftragsgesamtmenge, siehe Design C) | |
| Bonus (kein AKE-Aequivalent, zusaetzlich, nur hierarchisch gerendert) | `Kommissionieren`, `Hauptlagerplatz` | bereits eigene `ColumnDef`-Keys `assembly-group` (Baugruppe existiert schon), neu `kommissionieren`/`hauptlagerplatz` |

Kinder eines Knotens = Zeilen mit `VaterFA = Knoten.SubFA` (bei der Wurzel: `VaterFA = HauptFA`,
faktisch derselbe Wert, da bei der Wurzel `OrderNumber == SubOrderNumber`, Anhang Z. 82-89).

### C — Mengen: das Ergebnis beschreibt sich selbst

`BomQueryResult.MengeIstAuftragsmenge` (bool, `true` im hierarchischen Pfad, `false`/Default im
AKE-Pfad, dritter optionaler Record-Parameter). Zentraler Helper
`BomQuantityResolver.Resolve(result, item, order)` → `flag ? item.Menge : item.Menge * order.Quantity`.
Genau **zwei** verifizierte Multiplikationsstellen werden auf diesen Helper umgestellt:
`PickingController.cs:396`/`:581`, `ReadOnlyBomBuilder.cs:98` — **keine** globale Master-Abfrage in
den Verbrauchern, die Daten tragen ihre Semantik selbst, doppelte Multiplikation ist damit
strukturell unmoeglich statt nur „vom Entwickler bedacht".

### D — Zwei Scopes und eine Leitplanke

- `BomScope.DirectChildren` — direkte Kinder eines Sub-FA (Standardfall, AKE-Aequivalent).
- `BomScope.FullStructure` — alle Nachfahren ueber alle Ebenen, **jeder Knoten genau einmal**,
  flach (nicht als Baum — den gibt es bereits unter `/FaHierarchy`), inklusive
  Baugruppen-Zeilen (markiert), mit Zusatzspalten `Ebene` und `Vater-Sub-FA`. Damit bleibt
  Etappe-8-Wissen erhalten: `Kommissionieren` kann auch auf Nicht-Blatt-Zeilen stehen.
- **Picking-Workflow:** `order.OrderNumber == order.SubOrderNumber` (HauptFA) → `FullStructure`;
  sonst `DirectChildren` (Fachliche Anforderung 6).
- **Leitplanke — Aggregate immer `DirectChildren`.** Alle **aggregierenden** Verbraucher (heute:
  Artikelinfo; kuenftig jedes Aggregat) nutzen **immer** `DirectChildren`. Da jeder Knoten direktes
  Kind genau eines materialisierten Sub-FA ist, ist er ueber alle Auftraege **genau einmal** erfasst;
  mit `FullStructure` zaehlte er doppelt (dieselbe Summierungsfalle wie in Teil 3, jetzt bewusst
  vermieden). `FullStructure` ist reiner Anzeige-Scope des Picking-Workflows, niemals Aggregat-Scope.
- **Kein zweiter Baum-Walker.** `FullStructure` nutzt Zyklus-/Tiefenschutz und Waisen-Behandlung
  aus dem bestehenden `FaHierarchyTreeBuilder` (Teil 2) wieder — entweder durch direkte
  Wiederverwendung oder durch Extraktion eines gemeinsamen Traversal-Helpers (Dev-Lauf-Entscheidung,
  keine zweite Implementierung derselben Logik). Grund: Der Code-Review-Befund M7
  ([[2026-09-08-ideal-code-review-nachlese]]: ein vollstaendig abgekoppelter Ring ohne Wurzel/Waise
  wird von `FaHierarchyTreeBuilder` heute still verworfen) darf sich in einem zweiten,
  eigenstaendigen Walker nicht wiederholen — die Behebung von M7 selbst ist NICHT Teil dieser Spec,
  aber ein zweiter Walker mit derselben Schwaeche waere eine unnoetige zweite Angriffsflaeche.
- **Blatt-Waisen (`VaterFA` ohne Elternknoten, `SubFA = 0`)** sind niemandes direktes Kind →
  `FullStructure` haengt sie **markiert** an die HauptFA an; `DirectChildren` kann sie strukturell
  nicht sehen (bewusst, in der View dokumentieren, kein Fehlerzustand).

### E — HARTE ANFORDERUNG: Zeilenschluessel in der Vollstruktur (korrigiert gegenueber dem urspruenglichen Entwurf)

Der Pick-Zustand ist persistiert als `PickingItem(ProductionOrderId, BomArticleNumber, BomPosition)`
(verifiziert `Models/PickingItem.cs:12-19`: `BomArticleNumber` `[StringLength(100)]`, `BomPosition`
`[StringLength(50)] string?`); die View identifiziert Zeilen und gruppiert Baugruppen-Kinder ueber
`data-position`/`data-parent-pos` (verifiziert `Bom.cshtml:194-200`, JS `Bom.cshtml:858-923`).

**Code-Befund, der den urspruenglichen Backlog-Vorschlag korrigiert:** `TreeLevel` (Anzahl Punkte in
`Position`) **und** `parentPos` (alles vor dem letzten Punkt) werden **ausschliesslich** aus
`item.Position` berechnet — sowohl serverseitig (`PickingController.cs:352`,
`ReadOnlyBomBuilder.cs:88`) als auch clientseitig (`Bom.cshtml`). Die AKE-View liefert dafuer bereits
einen **vollstaendig rekursiven, mehrstufigen** Positionswert (`15`, `15.1`, `15.1.1` — Integrations-
karte [[integrationen]]) fuer EINE Abfrage `GetBomItemsAsync(order.ArticleNumber)`, die intern schon
den kompletten Baum dieses Geraets liefert. Ein flacher Praefix-Schluessel `VaterSubFA.Position`
(genau EIN zusaetzliches Segment, wie im urspruenglichen Backlog-Entwurf vorgeschlagen) wuerde
`parentPos` auf `VaterSubFA` strippen — das ist **keine gueltige Position irgendeiner anderen Zeile**
im Ergebnis, die Baum-/Kollisionslogik der View bricht.

**Empfehlung dieser Spec (ersetzt den urspruenglichen Vorschlag, Offene Rueckfrage 1 bittet um
Bestaetigung):** `BomPosition` fuer `FullStructure`-Zeilen als **vollstaendigen rekursiven Pfad ab
der Wurzel** aufbauen — je Ebene die eigene `FaHierarchyNode.Position`-Ganzzahl an den Pfad des
Elternknotens anhaengen (z. B. Wurzel-Kind an Position 3 → `"3"`; dessen Kind an Position 7 →
`"3.7"`; ein Enkel an Position 2 → `"3.7.2"`). Damit funktionieren `TreeLevel`, `parentPos` und die
gesamte JS-Baumlogik in `Bom.cshtml` **unveraendert**, exakt wie im AKE-Fall — **kein** JS-Umbau
noetig, nur die serverseitige Konstruktion des `Position`-Strings aendert sich. Die Baugruppen-
Gruppierung braucht dann **keine** gesonderte „explizite Elternbezuege statt Positions-Heuristik"-
Sonderbehandlung mehr, wie der urspruengliche Entwurf annahm — die bestehende Positions-Heuristik
funktioniert korrekt, **wenn** `Position` korrekt (rekursiv) konstruiert ist.

`DirectChildren` ist von alldem nicht betroffen: dort ist `Position` innerhalb der einen Abfrage
eindeutig (kein Pfad-Aufbau noetig, einfacher `FaHierarchyNode.Position`-Wert genuegt).

Zur Kenntnis: Pick-Status lebt je **Auftrag** (`ProductionOrderId`). Ein Teil erscheint in der
HauptFA-Vollansicht und in der Ansicht seines eigenen Sub-FA mit **unabhaengigen** Zustaenden.
Fachlich akzeptiert (HauptFA ist der kommunizierte Identifier, Anhang „Bekannte Fallstricke"); ein
gemeinsamer Zeilen-Status waere eigener Umfang.

**Migrations-Frage geklaert:** `PickingItem.BomPosition` ist bereits `NVARCHAR(50)` — **keine
Migration noetig**, ein vollstaendiger rekursiver Pfad (z. B. `"3.7.2"`) passt bequem in 50 Zeichen
fuer jede realistische Strukturtiefe.

### F — Picking-Ansicht

`Bom.cshtml` ist **Client-Mode** (`filterable-table` + `data-filterable`, kein Server-Filter) → neue
Spalten `Kommissionieren` + `Hauptlagerplatz` als Client-Spaltenfilter, registriert in
`ColumnDefinitions.Bom` (verifiziert `ColumnDefinitions.cs:219`, bestehende Spalten:
`pick-control`/`position`/`assembly-group`/`resource-number`/`description1`/`description2`/
`quantity`/`procurement`/`article-group`/`category`/`storage-location`/`source-location`/`order`).
Neue Keys: `kommissionieren`, `hauptlagerplatz` (und bei `FullStructure` zusaetzlich `ebene`,
`vater-sub-fa`). **Nur im hierarchischen Modus gerendert** — kein „immer da, aber leer" (Lehre aus
dem Review-Fund zur `parent-sub-order-number`-Spalte in der FA-Liste-Hierarchie-Spec). ADR 0005
erlaubt Client-Mode ausdruecklich fuer unpaginierte, vorgefilterte Ansichten wie diese.

### G — Artikelinfo (korrigiert gegenueber dem urspruenglichen Entwurf, Rueckfrage 6 beantwortet)

**Ist-Zustand verifiziert (`ArticlesController.cs`, `Views/Articles/Info.cshtml`):**
`GetDeviceArticleNumbersByComponentAsync`/`GetComponentMengePerDeviceAsync` haben **exakt zwei**
Aufrufer, beide in `ArticlesController.Info` (Zeile 259/285) — bestaetigt den Backlog-Befund. Das
Ergebnis (eine Liste von „Geraete-Artikelnummern") wird anschliessend gegen
`_productionOrderRepository.GetByArticleNumbersAsync(deviceArticleNumbers)` aufgeloest (Zeile 264) —
diese Methode filtert `ProductionOrder.ArticleNumber IN (...)`. Der bereits existierende
`Hierarchical`-Flag (Zeile 304-305, aus dem Etappe-D-Nachtrag der FA-Liste-Hierarchie-Spec) zeigt im
hierarchischen Modus heute `order.SubOrderNumber` als primaeren Bezeichner (`Info.cshtml:148`) mit
einer Zusammenfassung „N Auftraege · M Sub-FAs offen" (Zeile 160-162).

**Rueckfrage 6 beantwortet — NICHT 1:1 wiederverwendbar:** Der bestehende Zwei-Schritt-Weg
(Geraete-Artikelnummer ermitteln → `ProductionOrder` per `ArticleNumber` nachschlagen) passt nicht
zur hierarchischen Semantik dieser Spec: ein HauptFA ist **nicht** eindeutig durch eine Artikelnummer
identifiziert (mehrere Struktur-Instanzen desselben Geraetetyps koennen denselben `HauptArtnr`
tragen), und `ProductionOrder.ArticleNumber` einer materialisierten Sub-FA-Zeile ist die Artikel-
nummer **dieser Baugruppe selbst**, nicht des HauptFA. `FaHierarchyBomRepository` implementiert
`GetDeviceArticleNumbersByComponentAsync`/`GetComponentMengePerDeviceAsync` daher **nicht** als
einfache Weiterleitung, sondern mit einer eigenen Reverse-Lookup-Logik direkt gegen
`FaHierarchyNode`: alle Zeilen mit `Artnr == componentArticleNumber` finden, je Treffer den
unmittelbaren Elternknoten (`VaterFA`) UND die Wurzel (`HauptFA`) auf loesen, und **(HauptFA,
SubOrderNumber-des-Elternknotens)-Paare** statt Artikelnummern liefern. `ArticlesController.Info`
braucht dafuer im hierarchischen Zweig einen eigenen, direkteren Pfad, der diese Paare **nicht**
zusaetzlich ueber `GetByArticleNumbersAsync` re-aufloest, sondern direkt gegen
`SubOrderNumber`/`OrderNumber` matcht (`IProductionOrderRepository` hat dafuer bereits
`GetByOrderOrSubOrderNumberAsync`-aehnliche Lookups aus Teil 7/der FA-Liste-Hierarchie-Spec — exakte
Methode Dev-Lauf-Entscheidung). Partition **immer** `DirectChildren` (Leitplanke Design D), Menge =
Sollmenge (entsprechend beschriftet).

**Konkrete Verhaltensaenderung:** Entscheidung 2 (HauptFA als Geraet, Sub-FA als Zusatzspalte) kehrt
den heutigen Etappe-D-Zwischenstand (`SubOrderNumber` primaer) um.

### H — Klasse D: harte Code-Gates (Details verifiziert)

`CoatingDetectionService`, `FaWorkStepDetectionService` und **beide** oeffentlichen Einstiege von
`BomCacheSyncService` ueberspringen hierarchische Auftraege **im Code** (nicht nur per Config) —
Familie des Review-Fundes H1 ([[2026-09-08-ideal-code-review-nachlese]]).

**Verifizierter Ist-Zustand:**
- `CoatingDetectionService.DetectAndUpdateCoatingFlagsAsync` (`IConfiguration` bereits im
  Konstruktor) schliesst IDEAL heute nur **zufaellig** aus: `[ProductionDate] IS NOT NULL`
  (`CoatingDetectionService.cs:95`) — bei IDEAL ist `ProductionDate` NULL (K1-Regel, Materialisierungs-
  Spec). Kategorie-Match-SQL gegen `CachedBomHeaders`/`CachedBomItems`: Zeilen ~137-144.
- `FaWorkStepDetectionService.DetectAsync` hat **keinen** `IConfiguration`-Parameter (nur
  `ApplicationDbContext`, `ILogger`, `ISyncLogger`) — **Konstruktor-Signaturaenderung noetig**, um
  den Master lesen zu koennen (`IConfiguration` **vor** `ISyncLogger` einfuegen, Fallstrick
  „`ISyncLogger` ist der letzte Konstruktor-Parameter" beachten). Text-Match-Query gegen
  `CachedBomItems.Bezeichnung1/2`: Zeilen 62-64.
- `BomCacheSyncService` hat **zwei** oeffentliche Methoden (`SyncBomCacheAsync`,
  `SyncSpecificArticleNumbersAsync`) — **beide** brauchen den Gate. `SyncSpecificArticleNumbersAsync`
  wird von `SageImportService.cs:194` bei neu importierten Artikelnummern **ohne** den
  `ProductionDate`-Window-Filter aufgerufen und ist damit ein **eigener Umgehungspfad** des
  zufaelligen Schutzes — genau das Risiko, das ein harter Code-Gate statt eines Konfigurations-
  Zufalls beheben soll. `SyncBomCacheAsync`s Fensterabfrage hat denselben zufaelligen
  `ProductionDate IS NOT NULL`-Schutz (Zeile 404) wie `CoatingDetectionService`.

**Gate-Muster (an allen vier Einstiegen identisch):** direkt nach `BeginRunAsync`,
`ServiceSettings.GetBoolSafeAsync(_configuration, "ProduktionsauftragHierarchisch", false, ct)`
lesen (bereits etabliertes Muster, verifiziert `SyncWorker.cs:357`) — bei `true`: **kein**
Datenbankzugriff, `run.FinishSuccessAsync(...)` mit Null-Counts (analog dem bestehenden
Skip-Verhalten bei leerem `LackierteilKategorieName`, `CoatingDetectionService.cs:52-62`), Log-Info
„uebersprungen: hierarchischer Modus", `return` — **kein** `throw`.

`HasCoatingParts` (aus „Sub-FA selbst oder ein direktes Kind `Beschichtet`") kommt aus der
**Materialisierung** = K2 in [[2026-08-20-materialisierung-fachliche-felder-spec]] (dort deren
Rueckfrage 6) — **nicht** aus dieser Spec.

**Beschichtungstermin:** `CoatingDateCalculator.Compute` (verifiziert Signatur, `.cs:11-18`) rechnet
aus dem Vorkommissionier-Termin (`vorkommissionierTermin`-Parameter); bei IDEAL ist der zugrunde
liegende `ProductionDate` `NULL` → **kein** Termin durch das Flag allein. Fuer IDEAL muss der Termin
aus FAInfos `Start_Beschichtung` kommen — das ist Thema 2 (Materialisierungs-Spec, deren
Rueckfragen 5/6). Diese Spec liefert nur die **Gates** (Abschaltung der AKE-Heuristik), nicht die
Ersatzquelle.

**`HasGlass` — Rueckfrage 3 beantwortet, KEIN Klasse-D-Kandidat.** Verifiziert:
`ProductionOrderPickingStatus.HasGlass` ist ein rein **manuell gesetztes** Feld (`PickingStatusApiController.cs`,
`AllowedFields` inkl. `"HasGlass"`, `POST /api/picking-status/toggle`), initial `0` beim
Eager-Create (`SageImportService.cs:165` — `MERGE ... WHEN NOT MATCHED BY TARGET`, nur beim Anlegen,
nie ueberschrieben). Es gibt **keine** BOM-Ableitung, **keinen** Sync-Schreibzugriff auf `HasGlass` —
es funktioniert fuer materialisierte IDEAL-Sub-FAs identisch wie fuer AKE-Auftraege (dieselbe
`ProductionOrderPickingStatus`-Zeile, dasselbe Eager-Create, dieselbe API). **Kein Gate, kein
Sweep-Fund, keine Aenderung dieser Spec noetig.**

### I — Sweep (Rueckfrage-Umfang **verifiziert und abgeschlossen**, nicht mehr offen)

Vollstaendige Erhebung aller `vw_AKE_`/`[ake].[dbo]`/`ake.dbo`/`[ake]`-Fundstellen in
`IdealAkeWms/` und `IDEALAKEWMSService/` per Grep — **genau drei Treffer, keine weiteren**:

| Datei:Zeile | Kontext | Urteil |
|---|---|---|
| `IdealAkeWms/Data/Repositories/BomRepository.cs:41` | `FROM [ake].[dbo].[vw_AKE_Kommissionierung_StuecklistenDB]` — Live-BOM-Query bei Cache-Miss | **betroffen — Gegenstand dieser Spec.** Bleibt fuer AKE (Master `false`) unveraendert; im hierarchischen Modus wird `BomRepository` durch die Design-A-Weiche gar nicht mehr aufgeloest. |
| `IDEALAKEWMSService/Services/BomCacheSyncService.cs:276` | dieselbe View, raw SQL innerhalb des BOM-Cache-Syncs | **betroffen — Gegenstand dieser Spec**, Design H (Klasse-D-Gate deckt beide `BomCacheSyncService`-Einstiege ab, der Gate liegt VOR diesem SQL). |
| `IDEALAKEWMSService/Services/SageImportService.cs:57` | `FROM [dbo].[vw_AKE_Kommissionierung_WAListe]` — Produktionsauftrags-Import (`SyncProductionOrdersAsync`) | **nicht betroffen HIER** — bereits als eigener Review-Finding H1 dokumentiert ([[2026-09-08-ideal-code-review-nachlese]]): config-gesichert (`Sync:ProductionOrdersEnabled=false` auf IDEAL-Systemen als Deploy-Vorbedingung), **kein Code-Guard** vorhanden. Fix „vor Produktiv-Deploy" ist ein **eigener** Punkt, nicht Teil dieser Spec (siehe Umfang, Out-of-Scope). |

`HasGlass` ist wie in Design H beschrieben **kein** BOM-Ableitungsfund und taucht daher folgerichtig
nicht in dieser Liste auf (es gibt keine `vw_AKE_`/`[ake]`-Textstelle, die `HasGlass` beruehrt).

Die Sweep-Liste der alten Spec ging noch von einer unbekannten Anzahl Fundstellen aus („die Zahl ist
nicht vorab bekannt") — das ist mit dieser Verifikation **abgeschlossen**: drei Fundstellen, alle
bewertet.

## Migrations-/SQL-Auswirkungen

**Keine Migration.** Verifiziert: Alle neuen Typen (`BomKey`, `BomScope`,
`BomQueryResult.MengeIstAuftragsmenge`) sind reine C#-Werttypen/Properties ohne DB-Bezug.
`FaHierarchyBomRepository` liest ausschliesslich aus bereits existierenden Tabellen
(`FaHierarchyNodes`, `FaHierarchyOrderInfos`, aus Teil 7 `SQL/89`). Der zusammengesetzte
Zeilenschluessel fuer `FullStructure` (Design E) passt in das bereits vorhandene
`PickingItem.BomPosition`-Feld (`NVARCHAR(50)`, verifiziert `Models/PickingItem.cs:19`) — **keine
Schema-Aenderung**. `FaWorkStepDetectionService` bekommt einen neuen Konstruktor-Parameter
(`IConfiguration`), das ist eine reine C#-Signaturaenderung, keine DB-Auswirkung.

## Audit-Feld-Auswirkungen

Keine neuen `AuditableEntity`-Entitaeten. `FaHierarchyBomRepository` ist ein reiner Lesepfad (bis auf
die No-op-Schreibmethoden aus Design A, die nichts persistieren). `PickingItem` bleibt in seiner
bestehenden Audit-Semantik unveraendert — der zusammengesetzte Schluessel aendert nur den **Inhalt**
von `BomPosition`, nicht die Audit-Felder-Pflicht bei Picking-Updates.

## Betroffene Rollen / Zugriffsfilter

Keine neue Rolle, kein neuer Zugriffsfilter. Der Picking-Workflow behaelt seine bestehenden
`RequirePickingAccess`/`RequireStockReadAccess`-Filter (Rollen `picking`/`stock_read`)
unveraendert — diese Spec liefert nur eine andere Datenquelle hinter derselben Zugriffskontrolle.
Die Navigations-Verschiebung (Design, Fachliche Anforderung 5) aendert **nur Markup** in
`_Layout.cshtml` (verifiziert: Umbau des bestehenden einfachen `<a>`-Links Zeile 177-189 zu einem
Dropdown nach dem Vorbild des Leitstand-Dropdowns Zeile 165-176, Entfernen des eigenstaendigen
Teil-3-Nav-Items Zeile 112-120); die Kommissionierliste behaelt ihren bestehenden Gate
`RequireLagerProcessingAccess` + Toggle `FaHierarchyKommissionierlistenAktiv` (`canProcessLager &&
faKommListenAktiv`) unveraendert, der Picking-Workflow-Eintrag behaelt `canPick` unveraendert.

## Listen-View-Pattern-Pflichten (ADR 0005)

`Bom.cshtml` bleibt Client-Mode (begruendete ADR-0005-Ausnahme, unpaginierte, vorgefilterte
Ansicht). Die zwei neuen Spalten (`kommissionieren`, `hauptlagerplatz`) sowie im `FullStructure`-Fall
`ebene`/`vater-sub-fa` bekommen trotzdem **je** einen `data-col-key`-Eintrag in
`ColumnDefinitions.Bom` (Pflicht laut Fallstrick „data-col-key ist Pflicht auf allen `<th>` in
filterable-tables") — Client-Filter, nicht Server-Filter, aber ohne den Key ist die Spalte weder
filterbar noch in den Spalten-Preferences konfigurierbar. `assembly-group` (Baugruppe) existiert
bereits, kein neuer Key noetig. Kein neuer `viewKey` (bleibt `Bom`, bereits registriert
`ColumnDefinitions.cs:425`) — daher **kein** Risiko des 400er-Fallstricks „neuer viewKey ohne
`GetByViewKey`-Registrierung".

## Akzeptanzkriterien

1. **AKE unveraendert (F-AKE):** Bei Master `false` verhaelt sich der BOM-Knopf **bit-identisch**
   zu heute — Cache-First, dann Live gegen die AKE-View, keine der neuen Spalten sichtbar, kein
   `MengeIstAuftragsmenge`-Flag gesetzt. Nachweis: bestehende AKE-BOM-Tests unveraendert gruen +
   ein neuer Regressionstest, der `BomKey`/`BomScope` an `BomRepository` uebergibt und identisches
   Ergebnis zum Vor-Zustand erhaelt.
2. **Kein Cache-Zugriff im hierarchischen Pfad (F-Cache):** Weder lesend noch schreibend — inklusive
   der bislang ungeschuetzten `IBomCacheRepository`-Registrierung (`Program.cs:91`). Ein gefuellter
   persistenter BOM-Cache veraendert das Ergebnis im hierarchischen Modus **nicht** nachweisbar.
3. **Keine doppelte Mengen-Multiplikation (F-Menge):** An einem FA mit Auftragsmenge > 1 zeigt der
   hierarchische Pfad die Menge aus `Sollmenge` unveraendert (kein `* Quantity`); der AKE-Pfad
   multipliziert weiterhin wie bisher. Beide der zwei verifizierten Multiplikationsstellen laufen
   ueber `BomQuantityResolver`.
4. **Aggregate nur `DirectChildren` (F-Partition):** Property-Test „Vereinigung aller
   `DirectChildren` ueber alle materialisierten Sub-FAs einer HauptFA = alle Nicht-Wurzel-Knoten,
   ohne Doppelung (ausser markierten Blatt-Waisen)" ist gruen. Artikelinfo nutzt nachweisbar
   ausschliesslich `DirectChildren`.
5. **Zeilenschluessel eindeutig in `FullStructure` (F-Zeilenschluessel):** `Position` wird als
   vollstaendiger rekursiver Pfad konstruiert; zwei Positionen mit identischer letzter Ziffer unter
   verschiedenen Vater-Sub-FAs erhalten unterschiedliche `BomPosition`-Werte; die bestehende
   `Bom.cshtml`-Baumlogik (Positions-basiert) gruppiert korrekt **ohne** JS-Aenderung.
6. **Signaturwechsel vollstaendig und mechanisch (F-Signatur):** Jeder in der Aufrufer-Tabelle
   (Design A) gelistete Aufrufer ist auf `BomKey`/`BomScope` umgestellt; Build + alle bestehenden
   Tests gruen nach der Umstellung.
7. **Scope-Regel korrekt (Fachliche Anforderung 6):** Aufruf am HauptFA liefert `FullStructure`
   (alle Ebenen, jeder Knoten einmal, Baugruppen-Zeilen markiert); Aufruf an einem Sub-FA liefert
   ausschliesslich `DirectChildren`.
8. **Kommissionieren-Filter ohne stillen Filter:** Alle Zeilen erscheinen in der Picking-Stueckliste;
   `Kommissionieren` ist als Client-Spaltenfilter bedienbar, kein Zeilen wird vorab ausgeblendet.
9. **Artikelinfo HauptFA/Sub-FA:** „In welchen Geraeten kommt Artikel X vor" zeigt im hierarchischen
   Modus HauptFA als Geraet-Spalte und den direkten Sub-FA-Elternknoten als Zusatzspalte (kehrt den
   heutigen Etappe-D-Zwischenstand um), Menge = Sollmenge, entsprechend beschriftet.
10. **Klasse-D-Gates aktiv an allen vier Einstiegen:** Bei Master `true` fuehren
    `CoatingDetectionService.DetectAndUpdateCoatingFlagsAsync`,
    `FaWorkStepDetectionService.DetectAsync`, `BomCacheSyncService.SyncBomCacheAsync` **und**
    `BomCacheSyncService.SyncSpecificArticleNumbersAsync` **keinen** Durchlauf aus (geloggt als
    bewusster Skip, nicht als Fehler) — unabhaengig vom Zustand von `ProductionDate` und unabhaengig
    davon, ob der Aufruf ueber das Sync-Fenster oder ueber den `SageImportService`-Umgehungspfad
    erfolgt.
11. **Navigation:** Kommissionierliste erscheint im Menue unter „Kommissionierung" neben dem
    Picking-Workflow-Eintrag (gemeinsames Dropdown), mit unveraenderten Toggle-/Rollen-Gates.
12. **Koexistenz:** Ein und derselbe HauptFA ist sowohl ueber die Teil-3-Kommissionierliste als auch
    ueber den Picking-Workflow bedienbar, ohne dass eine Aktion die andere blockiert oder
    widerspruechliche Zustaende erzeugt.
13. **Guard vollstaendig ersetzt:** `HierarchicalBomGuardRepository` existiert nicht mehr im Code;
    kein verbliebener Aufrufpfad zeigt noch den alten Hinweistext „Stueckliste wird im
    hierarchischen Modus ueber die FA-Struktur angezeigt".
14. **Sweep vollstaendig:** Alle drei verifizierten Fundstellen sind bewertet (zwei davon Gegenstand
    dieser Spec, eine als eigener Punkt — Review-Finding H1 — ausdruecklich ausgeklammert); kein
    weiterer Fund taucht bei erneutem Grep in `IdealAkeWms/`/`IDEALAKEWMSService/` auf.
15. **`HasGlass` unveraendert:** Regressionstest bestaetigt, dass `HasGlass` (manuell, per API) fuer
    materialisierte IDEAL-Sub-FAs identisch funktioniert wie fuer AKE-Auftraege — keine Beruehrung
    durch diese Spec.
16. **Feld-Mapping vollstaendig dokumentiert und nachvollzogen:** Jede AKE-View-Spalte hat eine
    dokumentierte Entsprechung oder eine dokumentierte, bewusste Luecke (kein stiller Leerwert bei
    aktivem Filter/aktiver Sortierung); `Baugruppe`/`IsBaugruppe` folgen der in Design B verifizierten
    Semantik (Eltern-Referenz-String bzw. abgeleiteter/`SubFA != 0`-Bool), nicht der urspruenglich
    vermuteten `SubFA <> 0`-direkten-Gleichsetzung mit `Baugruppe` selbst.

## Test-Szenarien

**Automatisiert (Unit/InMemory):**

- Je Scope (`DirectChildren`/`FullStructure`) ein FA-natives Test-Set gegen `FaHierarchyNode`
  (InMemory-Kontext, Teil-1-Muster).
- **Property-Test „jeder Knoten genau einmal":** Vereinigung aller `DirectChildren`-Ergebnisse ueber
  alle materialisierten Sub-FAs einer HauptFA-Gruppe = alle Nicht-Wurzel-Knoten dieser Gruppe, ohne
  Doppelung (ausser markierten Blatt-Waisen).
- Mengen-Flag: Test je Multiplikationsstelle (`PickingController.cs:396`/`:581`,
  `ReadOnlyBomBuilder.cs:98`) mit `MengeIstAuftragsmenge = true` vs. `false`.
- Feld-Mapping: Test, der jede Tabellenzeile aus Design B einzeln prueft (Wert kommt aus dem
  richtigen `FaHierarchyNode`-Feld an der richtigen `BomItem`-Property an), inkl. `Baugruppe`
  (Eltern-Referenz) und `IsBaugruppe`.
- Zeilenschluessel-Eindeutigkeit: Testdatensatz mit zwei gleich positionierten Zeilen unter
  unterschiedlichen Vater-Sub-FAs → unterschiedliche `BomPosition`-Werte (rekursiver Pfad), korrekte
  Gruppierung ohne JS-Aenderung nachgewiesen (Snapshot-Test der gerenderten `data-parent-pos`-Werte).
- Klasse-D-Gates: Je der vier verifizierten Einstiege ein Test „bei Master `true` kein
  Datenbankzugriff/keine Verarbeitung, Log-Eintrag vorhanden", inkl. explizitem Test fuer
  `SyncSpecificArticleNumbersAsync` (nicht nur `SyncBomCacheAsync`).
- Artikelinfo-Reverse-Lookup: Test, der ein Bauteil in zwei verschiedenen Sub-FAs derselben und
  unterschiedlicher HauptFAs platziert und die korrekten (HauptFA, SubOrderNumber)-Paare erwartet.
- AKE-Regression: bestehende BOM-/Picking-Tests unveraendert gruen; Cache-Treffer und -Miss je
  einmal gegen die umgestellte `BomKey`-Signatur.
- `HasGlass`-Regression: bestehender Toggle-Test unveraendert gruen bei Master `true`.

**Manual-UAT — neues Kapitel „IDEAL — BOM-Bridge (Stueckliste ueber die Schnittstelle)" in
`docs/TESTSZENARIEN.md`, TS-70 (naechste freie Nummer nach TS-69):**

- **TS-70.1 HauptFA-Vollansicht (AK 7, AK 5).**
  Vorbedingung: Master `ProduktionsauftragHierarchisch = true`, materialisierter HauptFA mit
  mehrstufiger Struktur (mind. 2 Ebenen, mind. eine Baugruppe mit eigenen Kindern) im
  IDEAL-Testsystem.
  Schritte: BOM-Knopf am HauptFA oeffnen; Zeilen jeder Ebene identifizieren; eine Position unter
  Baugruppe A und eine mit identischer `Position`-Nummer unter Baugruppe B pruefen; je eine Zeile
  aus zwei verschiedenen Baugruppen anhaken (Pick-Status).
  Erwartung: alle Ebenen sichtbar, jeder Knoten genau einmal; die beiden gleich positionierten
  Zeilen bleiben unabhaengig anhakbar (kein gemeinsamer/verwechselter Pick-Zustand); Baugruppen-Auf-
  /Zuklappen zeigt die korrekten eigenen Kinder (ohne dass am Frontend etwas geaendert wurde).
  Negativfall: eine der beiden Zeilen anhaken, Seite neu laden → nur die angehakte Zeile bleibt
  markiert, die andere nicht.

- **TS-70.2 Sub-FA-Ansicht (AK 7).**
  Vorbedingung: derselbe HauptFA, ein Sub-FA mit eigenen direkten Kindern.
  Schritte: BOM-Knopf direkt am Sub-FA oeffnen.
  Erwartung: ausschliesslich die direkten Kinder dieses Sub-FA erscheinen, keine Enkel, keine
  Geschwister-Zweige.

- **TS-70.3 Kommissionieren-Filter (AK 8).**
  Schritte: BOM-Knopf am HauptFA oeffnen; Spaltenfilter `Kommissionieren` auf einen konkreten
  Zielwert setzen.
  Erwartung: nur Zeilen mit passendem Kommissionier-Ziel sichtbar; Filter zuruecksetzen zeigt wieder
  **alle** Zeilen (kein vorab stumm gefilterter Zustand beim ersten Aufruf).
  Negativfall: Zielwert waehlen, fuer den es keine Zeile gibt → leere, aber erkennbar gefilterte
  Ansicht, kein Fehler.

- **TS-70.4 Artikelinfo HauptFA/Sub-FA (AK 9).**
  Vorbedingung: ein Artikel, der als Komponente in mind. zwei verschiedenen Sub-FAs derselben oder
  unterschiedlicher HauptFAs vorkommt.
  Schritte: Artikelinfo des Artikels oeffnen.
  Erwartung: je Fundstelle HauptFA (Geraet, fett) + der jeweils direkte Sub-FA-Elternknoten als
  Zusatzspalte (**nicht mehr** Sub-FA fett/primaer wie im heutigen Etappe-D-Zwischenstand); Menge =
  Sollmenge, entsprechend beschriftet.

- **TS-70.5 Menue „Kommissionierung" mit beiden Einstiegen (AK 11, AK 12).**
  Schritte: Menuepunkt „Kommissionierung" oeffnen.
  Erwartung: ein Dropdown mit sowohl dem Picking-Workflow-Eintrag als auch der Kommissionierliste
  (Teil 3); beide funktionieren unabhaengig voneinander am selben HauptFA (z. B. in der
  Kommissionierliste drucken, im Picking-Workflow parallel Zeilen anhaken — keine gegenseitige
  Blockade).

- **TS-70.6 Flachmodus bit-identisch (AK 1).**
  Schritte: Master auf `false` stellen (AKE-Testsystem oder AKE-Auftrag); BOM-Knopf an bekanntem
  AKE-FA oeffnen, einmal mit gefuelltem, einmal mit geleertem BOM-Cache.
  Erwartung: Verhalten identisch zum Stand vor dieser Spec — keine neuen Spalten, keine
  Kommissionieren-Filterkarte, `Menge` weiterhin hochgerechnet mit der Auftragsmenge.

- **TS-70.7 Klasse-D-Gate sichtbar im Aktivitaets-Protokoll (AK 10).**
  Vorbedingung: `Sync:CoatingDetectionEnabled`/`Sync:FaWorkStepDetectionEnabled`/`Sync:BomCacheEnabled`
  aktiv, Master `true`.
  Schritte: einen Sync-Zyklus abwarten oder manuell ausloesen; Aktivitaets-Protokoll pruefen; falls
  moeglich zusaetzlich einen neuen IDEAL-Auftrag importieren lassen (um den
  `SyncSpecificArticleNumbersAsync`-Pfad ueber `SageImportService` auszuloesen).
  Erwartung: alle vier Laeufe (inkl. des `SageImportService`-ausgeloesten) erscheinen als bewusst
  uebersprungen (nicht als Fehler), keine Coating-/WorkStep-/Cache-Erkennung findet fuer
  hierarchische Auftraege statt.

- **TS-70.8 Guard vollstaendig ersetzt (AK 13).**
  Schritte: `/Picking/Bom/<id>` an einem hierarchischen Auftrag direkt aufrufen.
  Erwartung: die echte Stueckliste erscheint (nicht mehr der Hinweistext „Stueckliste wird im
  hierarchischen Modus ueber die FA-Struktur angezeigt" aus TS-68).

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen (TS-68-Zeile auf „abgeloest
durch TS-70" ergaenzen, TS-70-Zeile neu anlegen).

## Deploy

**Provisorisch (Dev-Lauf bestaetigt gegen den echten Diff):**

- **Web-App:** ja — neue/geaenderte Repositories, Controller, ViewModel, Views (`Bom.cshtml`,
  `_Layout.cshtml`, `Views/Articles/Info.cshtml`), `ColumnDefinitions.cs`, `Program.cs`.
- **Service:** ja — Klasse-D-Gates in `CoatingDetectionService`, `FaWorkStepDetectionService`
  (inkl. Konstruktor-Signaturaenderung), `BomCacheSyncService` (`IDEALAKEWMSService/Services/`).
- **Migration:** nein (verifiziert, siehe Migrations-Abschnitt).

**Publish-Befehle (im Worktree, nach dem gruenen QA-Nachweis, vor dem Merge):**
```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
```

## Reihenfolge / Einordnung

- **Nach dem Buendel-Merge, aus `main`.** Diese Spec braucht kein Artefakt, das nicht bereits in
  `main` liegt, sobald das laufende Buendel (Teile 1–8, BOM-Guard, FA-Struktur-Darstellung,
  Listen-Spaltenauswahl, FA-Liste-Hierarchie-Epic — alle `Testbereit`, siehe [[feature-map]]) gemergt
  ist. Kein Zwischen-Merge fuer diese Spec allein.
- **Eigener, frisch aus `main` gezogener Worktree** — nicht der alte Buendel-Worktree
  `feature/2026-08-07-ideal-teile-1-5` (der wird nach dem Merge erst nach expliziter Freigabe
  aufgeraeumt).
- **Supersedes [[2026-08-18-ake-view-abhaengigkeiten-hierarchisch-spec]].** Als **offene Aktion**,
  NICHT von diesem Spec-Lauf selbst ausgefuehrt: Die Alt-Spec-Datei bekommt `status: Superseded` im
  Frontmatter, sobald diese Spec freigegeben wird (Mensch bzw. ein spaeterer, dafuer beauftragter
  Lauf) — der Spec-Agent aendert gemaess seiner Regeln keine fremden Spec-Dateien.
- **Ersetzt den umgesetzten Minimal-Guard** [[2026-08-18-bom-guard-hierarchisch-spec]]
  (`status: Testbereit`, TS-68) im **Code**; die Guard-Spec-Datei selbst bleibt als historischer
  Beleg unveraendert (nicht von diesem Lauf zu aendern), TS-68 wird im Testindex als „abgeloest
  durch TS-70" vermerkt (Aktion des Dev-/QA-Laufs, nicht dieses Spec-Laufs).
- **Verzahnt mit** [[2026-08-20-materialisierung-fachliche-felder-spec]]: K2 `HasCoatingParts` und
  der Beschichtungstermin aus FAInfos entstehen **dort**; diese Spec liefert nur die
  Abschalt-Gates (Design H). Reihenfolge zwischen beiden Specs ist nicht zwingend — sie koennen
  unabhaengig freigegeben werden, sollten aber vor der gemeinsamen Umsetzung aufeinander abgestimmt
  sein (beide beruehren `CoatingDateCalculator`-Umfeld).
- **Folgeblock** [[2026-09-08-arbeitsgaenge-aus-arbeitsschritte]] — erst NACH dieser Spec **und**
  der Materialisierungs-Spec spezifizieren (Klasse-D-Abschaltung von `FaWorkStepDetectionService`
  ist Voraussetzung).

## Brain-Pflichten bei Umsetzung (Merkliste fuer den Dev-Lauf)

- Version-Bump in **beiden** `AppVersion.cs` (Web + Service) — naechste freie Nummer nach dem
  Buendel-Merge neu bestimmen (Stand dieser Spec: nach v1.35.0, vermutlich v1.36.0, im Dev-Lauf zu
  bestaetigen).
- Anwender-Changelog `Views/Help/Changelog.cshtml` + Brain-Changelog
  `secondbrain/changelog/YYYY-MM-DD-vX-Y-Z-bom-bridge.md`.
- `secondbrain/feature-map.md` — neue Zeile im Abschnitt „IDEAL-Standort", BOM-Bridge als
  abgeschlossenen Baustein nach der FA-Liste-Hierarchie-Zeile eintragen; die
  „Buendel-Nachlese — BOM-Guard hierarchisch"-Zeile auf „abgeloest durch BOM-Bridge" aktualisieren.
- **ADR-Kandidat:** `secondbrain/architektur/adr/0013-bom-bridge-repository-schnittstelle-statt-cache-kopie.md`
  (naechste freie ADR-Nummer, verifiziert: 0001–0012 vergeben, 0013 frei) — dokumentiert die
  Entscheidung „Bridge an der bestehenden Repository-Schnittstelle statt Kopie in den persistenten
  BOM-Cache", mit den vier Befunden aus „Ziel/Nutzen" als Begruendung, unter Bezug auf
  [[0001-repository-pattern-mit-decorator-fuer-caching]] und
  [[0007-bom-quelle-sage-view-mit-oseon-fallback]].
- `secondbrain/architektur/fallstricke.md` — neuer Eintrag zur `Baugruppe`/`IsBaugruppe`-Semantik
  (String-Eltern-Referenz vs. abgeleiteter Bool, Design B) und zum Zeilenschluessel/Positions-
  Konstruktions-Fallstrick der Vollstruktur (Design E: rekursiver Pfad statt flacher Praefix).
- `secondbrain/codebase/services.md` — `FaHierarchyBomRepository` in die Repository-Tabelle
  aufnehmen, `HierarchicalBomGuardRepository`-Zeile entfernen/ersetzen.
- `secondbrain/glossar/glossar.md` — ggf. `BomKey`/`BomScope` als technische Begriffe ergaenzen,
  falls sie im UI/Fachgespraech eine eigene Bezeichnung bekommen.
- Hilfeseite (`Views/Help/`) — konkrete Details zu den neuen Filtern/Spalten ergaenzen
  (`feedback_help_details`-Regel).
- `secondbrain/tests/testszenarien-index.md` — TS-70-Zeile, TS-68-Zeile auf „abgeloest" setzen.

## Offene Rueckfragen

1. **`BomPosition`-Konstruktion in `FullStructure`:** Code-verifiziert (Design E) ist ein
   **vollstaendiger rekursiver Positions-Pfad ab der Wurzel** (z. B. `"3.7.2"`) die einzige
   Konstruktion, die die bestehende `TreeLevel`/`parentPos`-Logik in `Bom.cshtml` unveraendert
   weiterlaufen laesst — der urspruengliche Backlog-Vorschlag eines flachen `VaterSubFA.Position`-
   Praefixes wuerde diese Logik brechen. Bitte bestaetigen, dass der rekursive Pfad die gewuenschte
   Loesung ist (Alternative waere ein JS-Umbau auf explizite Elternbezuege, mit entsprechend groesserem
   Aufwand).

*(Die urspruenglichen Rueckfragen 2 „Baugruppe-Semantik" und 3 „HasGlass" sind durch die
Code-Verifikation dieser Fassung beantwortet — siehe Design B bzw. Design H. Die zusaetzlichen
Rueckfragen 4 „Worktree fehlt" und 6 „Artikelinfo-Methodenform" der Vorfassung sind ebenfalls
erledigt: Rueckfrage 4 durch die Verifikation gegen den tatsaechlichen Worktree, Rueckfrage 6
inhaltlich beantwortet in Design G. Rueckfrage 5 „`BomPosition`-Datentyp" ist beantwortet: bereits
`NVARCHAR(50)`, keine Migration.)*

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. →
