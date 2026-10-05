---
typ: feature
---
# IDEAL: Stueckliste hierarchiefaehig ueber die Repository-Schnittstelle (Bridge statt Cache-Kopie)

Nachfolger und **Ersatz** von [[2026-08-18-ake-view-abhaengigkeiten-hierarchisch-spec]] (Entwurf,
wird superseded). Entstanden aus einer Design-Runde am 2026-09-08 (Brainstorming + kritische
Code-Pruefung im Worktree `feature/2026-08-07-ideal-teile-1-5`). Alle Entscheidungen unten sind
vom Menschen getroffen; die Code-Belege (`datei:zeile`) beziehen sich auf den Worktree-Stand.

## Ziel / Nutzen

Die gesamte AKE-Maschinerie, die auf der Stueckliste aufsetzt — **Picking-Workflow**
(Pick-Status je Zeile), Vorbau-Stueckliste, Bedarfsmeldung/Lagerbestellung aus einer Zeile,
Artikelinfo — soll fuer IDEAL-Auftraege im hierarchischen Modus **unveraendert nutzbar** sein.
Heute endet der BOM-Knopf in einer Hinweisseite (Minimal-Guard); die Kommissionierung bei IDEAL
laeuft nur ueber die Teil-3-Kommissionierliste.

**Nicht-Ziel:** den persistenten BOM-Cache (`CachedBomHeaders/Items`) aus der Hierarchie zu
befuellen. Das war die erste Idee und ist nach Pruefung **verworfen** (Begruendung unten).

## Warum Bridge an der Schnittstelle statt Cache-Kopie

**Befund 1 — alle Web-Verbraucher haengen an EINER Schnittstelle.** Picking, Vorbau
(`ReadOnlyBomBuilder`), Fehlteile-Kontext, Bedarfsmeldungen, Lagerbestellungen, Artikelinfo gehen
ausnahmslos ueber `IBomRepository` / `IBomCacheRepository` mit der einzigen Implementierung
`BomCacheRepository`. **Kein** Web-Raw-SQL auf die Cache-Tabellen, **keine** Web-Schreibzugriffe
(`UpsertBomAsync`/`DeleteOrphansAsync` ruft nur der Service-Sync). Eine zweite Implementierung der
Schnittstelle deckt damit alles ab.

**Befund 2 — die Cache-Semantik passt nicht zu IDEAL.** Der Cache ist durchgehend auf
**Artikelnummer** geschluesselt (Header, alle fuenf Lookups, Sync-Auswahl, Purge) und fuehrt
`Menge` **je Stueck**: die Verbraucher rechnen `bom.Menge * order.Quantity` hoch
(`PickingController.cs:396`, `:581`, `ReadOnlyBomBuilder.cs:98`). IDEAL-`Sollmenge` ist dagegen
die **Auftragsgesamtmenge** (Anhang [[sage-views-ideal]] Z. 65, Freigabe-Antwort 1 der alten
Spec) und Positions-Attribute sind auftragsspezifisch („Position bevorzugt, Stammdaten als
Fallback"). Ein artikel-geschluesselter Cache wuerde doppelt multiplizieren und zwei FAs desselben
Artikels dieselbe Menge zuweisen — stille Falschdaten (F1/F3 der alten Spec, jetzt konkret).

**Befund 3 — die Struktur ist bereits lokal.** Der Cache existiert auf AKE nur, weil die Quelle
eine langsame Fremd-View ist. `FaHierarchyNode` liegt lokal, indiziert, wird alle 15 Minuten per
Full-Refresh erneuert. Eine Kopie in den Cache waere Duplikation mit Lag, Purge-/Hash-Logik und
Vergiftungsrisiko — ohne Nutzen.

**Befund 4 — zwei Verbraucher-Klassen.**
- **Klasse P (Praesentation/Workflow):** braucht „Komponenten dieses Auftrags mit Mengen" → ueber
  die Schnittstelle bedienbar.
- **Klasse D (Ableitung):** `CoatingDetectionService` (Raw-SQL, Treffer per Artikelkategorie ==
  `LackierteilKategorieName`, `:139-155`) und `FaWorkStepDetectionService`
  (`Bezeichnung1/2.Contains(term)`, `:62-65`) sind **Heuristiken**. IDEAL liefert die Wahrheit
  **explizit**: `Beschichtet` je Position (Anhang Z. 76), `Arbeitsschritte` je Position (Z. 74),
  FAInfos `Start_Beschichtung`/`Dienstleister`/`RAL`. Die Heuristiken auf IDEAL-Daten laufen zu
  lassen erzeugte Fehldaten, keine Funktion. → Klasse D wird fuer hierarchische Auftraege
  **hart abgeschaltet** und ueber die **Materialisierung** (K2, siehe Thema 2) bedient.

## Entscheidungen des Menschen (2026-09-08)

1. **Kommissionieren-Filter:** Picking-Stueckliste zeigt **alle** Zeilen; Spalte `Kommissionieren`
   (Kommissionier-Ziel) mit **Spaltenfilter**. Kein stiller Filter.
2. **Artikelinfo** „in welchen Geraeten kommt Artikel X vor": **HauptFA** als Geraet, **Sub-FA**
   (dessen direktes Kind X ist) als Zusatzspalte.
3. **Arbeitsgaenge aus `Arbeitsschritte`:** eigener Folgeblock, vorgemerkt →
   [[2026-09-08-arbeitsgaenge-aus-arbeitsschritte]].
4. **Koexistenz:** Teil-3-Kommissionierliste (Druck je Ziel) **und** AKE-Picking-Workflow
   (Status-Prozess) sind beide fuer IDEAL nutzbar.
5. **Navigation:** Die Kommissionierliste wandert unter den Menuepunkt **„Kommissionierung"**
   neben den Picking-Workflow (gleiche Toggle-/Rollen-Gates, nur `_Layout`).
6. **Scope im Picking-Workflow:** Auftrag ist **HauptFA** → **komplette Struktur** (alle Ebenen);
   Auftrag ist **Sub-FA** → **nur dessen eigene Stueckliste** (direkte Kinder).

## Design

### A — Schnittstelle und Weiche
- Neue Implementierung `FaHierarchyBomRepository` fuer `IBomRepository` **und**
  `IBomCacheRepository`, liest aus `FaHierarchyNode` (+ `ProductionOrders` fuer den Schluessel).
  Auswahl per Master `ProduktionsauftragHierarchisch` im DI-Resolver — dasselbe Muster wie im
  ganzen Paket. **Kein** MemoryCache-Decorator im hierarchischen Pfad (Resolver liegt aussen).
- Ersetzt den Minimal-Guard `HierarchicalBomGuardRepository` ([[2026-08-18-bom-guard-hierarchisch-spec]])
  vollstaendig; TS-68 wird durch neue Szenarien abgeloest.
- Schluessel: die Schnittstelle nimmt statt `string articleNumber` ein
  `BomKey(ArticleNumber, SubOrderNumber)`. Die flache AKE-Implementierung liest nur
  `ArticleNumber` → **bit-identisch**; die Signaturaenderung an den Aufrufern ist mechanisch.
- Schreibmethoden (`UpsertBomAsync`, `DeleteOrphansAsync`) sind im hierarchischen Pfad
  **No-op mit Log**, nie Exception.
- `Source = "FA-HIERARCHIE"` im `BomQueryResult` sichtbar (ADR [[0007-bom-quelle-sage-view-mit-oseon-fallback]]-Prinzip).

### B — Feld-Mapping (nach Anhang, korrigiert gegenueber der alten Spec)
Die alte Spec hatte `Artnr → Artikelnummer` gemappt und `Ressourcenummer` fuer fehlend erklaert —
sie hat Kopf- und Zeilenschluessel verwechselt. Im AKE-Cache ist `Artikelnummer` der **Kopf**
(wessen Stueckliste) und `Ressourcenummer` die **Komponente je Zeile**. Anhang Z. 62:
FAListe-`Artnr` **ist** `KHKPpsFaBelegePositionen.RessourceNummer`.

| BOM-Feld | ← `FaHierarchyNode` |
|---|---|
| Kopf `Artikelnummer` | Artikel des Elternknotens (`HauptArtnr` bei Wurzel, sonst `Artnr` des Vater-Sub-FA) |
| `Ressourcenummer` | `Artnr` |
| `Position`, `Bezeichnung1/2` | gleichnamig |
| `Baugruppe` | `SubFA <> 0` |
| `Beschaffungsartikel` | bool → `Ja`/`Nein` (AKE-Format) |
| `Artikelgruppe` | gleichnamig — **gleiches Format** `"CODE - Bezeichnung"` wie AKE-Matching (Z. 68/241) |
| `Menge` | `Sollmenge` (Auftragsgesamtmenge, siehe C) |
| Bonus | `Kommissionieren`, `Hauptlagerplatz` (nur hierarchisch gerendert) |

Kinder eines Knotens = Zeilen mit `VaterFA = Knoten.SubFA` (Wurzel: `= HauptFA`), Anhang Z. 82-89.

### C — Mengen: das Ergebnis beschreibt sich selbst
`BomQueryResult.MengeIstAuftragsmenge` (true im hierarchischen Pfad). Die drei
Multiplikationsstellen nutzen **einen** Helper: `flag ? Menge : Menge * Quantity`. **Keine**
globale Master-Abfrage in Verbrauchern — die Daten tragen ihre Semantik, doppelte Multiplikation
ist strukturell unmoeglich.

### D — Zwei Scopes und eine Leitplanke
- `BomScope.DirectChildren` — direkte Kinder eines Sub-FA.
- `BomScope.FullStructure` — alle Nachfahren ueber alle Ebenen, **jeder Knoten einmal**, flach
  (nicht als Baum — den gibt es unter `/FaHierarchy`), inklusive Baugruppen-Zeilen (markiert), mit
  Zusatzspalten `Ebene` und `Vater-Sub-FA`. So bleibt Etappe-8-Wissen erhalten: `Kommissionieren`
  kann auch auf Nicht-Blatt-Zeilen stehen.
- **Picking-Workflow:** `OrderNumber == SubOrderNumber` (HauptFA) → `FullStructure`; sonst
  `DirectChildren` (Entscheidung 6).
- **Leitplanke:** Alle **aggregierenden** Verbraucher (heute: Artikelinfo; kuenftig jedes
  Aggregat) nutzen **immer `DirectChildren`**. Da jeder Knoten direktes Kind genau eines
  materialisierten Sub-FA ist, ist er ueber alle Auftraege **genau einmal** erfasst; mit
  `FullStructure` zaehlte er doppelt (Summierungsfalle aus Teil 3). `FullStructure` ist reiner
  Anzeige-Scope des Picking-Workflows.
- **Kein zweiter Baum-Walker:** `FullStructure` nutzt Zyklus-/Tiefenschutz und Waisen-Behandlung
  aus `FaHierarchyTreeBuilder` wieder (sonst erbt die Vollansicht den Review-Fund M7 aus
  [[2026-09-08-ideal-code-review-nachlese]] in zweiter Ausfuehrung). Blatt-Waisen (VaterFA ohne
  Elternknoten, `SubFA = 0`) sind niemandes direktes Kind → `FullStructure` haengt sie **markiert**
  an die HauptFA; `DirectChildren` kann sie nicht sehen (bewusst, dokumentieren).

### E — HARTE ANFORDERUNG: Zeilenschluessel in der Vollstruktur
Der Pick-Zustand ist persistiert als `PickingItem(ProductionOrderId, BomArticleNumber, BomPosition)`
(`PickingItem.cs:12-19`); die View identifiziert Zeilen und gruppiert Baugruppen-Kinder ueber
`data-position` (`Bom.cshtml:197`, `:868-918`). In `FullStructure` ist `Position` aber **je
Elternknoten** vergeben — „Position 1" existiert unter jeder Baugruppe. Ohne Gegenmassnahme
kollidieren Pick-Zustaende und die Gruppierung greift falsche Kinder: **stille Fehldaten**.
→ `FullStructure`-Zeilen tragen einen **zusammengesetzten Schluessel** `VaterSubFA.Position` in
`BomPosition`; die Baugruppen-Gruppierung nutzt im hierarchischen Modus die **expliziten
Elternbezuege** aus der Struktur statt der Positions-Heuristik. `DirectChildren` ist nicht
betroffen (Position dort eindeutig).
Zur Kenntnis: Pick-Status lebt je **Auftrag**. Ein Teil erscheint in der HauptFA-Vollansicht und
in der Ansicht seines Sub-FA mit **unabhaengigen** Zustaenden. Fachlich akzeptiert (HauptFA ist der
kommunizierte Identifier); ein gemeinsamer Zeilen-Status waere eigener Umfang.

### F — Picking-Ansicht
`Bom.cshtml` ist **Client-Mode** (`filterable-table` + `data-filterable`, kein Server-Filter) →
Spalten `Kommissionieren` + `Hauptlagerplatz` als Client-Spaltenfilter, registriert in
`ColumnDefinitions.Bom` (`ColumnDefinitions.cs:219`, Registrierungspflicht laut fallstricke).
**Nur im hierarchischen Modus gerendert** — kein „immer da, aber leer" (vgl. Review-Fund zur
`parent-sub-order-number`-Spalte). ADR 0005 erlaubt Client-Mode fuer unpaginierte Ansichten.

### G — Artikelinfo
`GetDeviceArticleNumbersByComponentAsync` / `GetComponentMengePerDeviceAsync` haben **nur**
`ArticlesController.cs:259/285` als Aufrufer → Umgestaltung bleibt begrenzt: HauptFA als Geraet,
Sub-FA als Zusatz, Menge = Sollmenge (so beschriftet), Partition `DirectChildren`.

### H — Klasse D: harte Code-Gates
`CoatingDetectionService`, `FaWorkStepDetectionService` und `BomCacheSyncService` ueberspringen
hierarchische Auftraege **im Code** (nicht nur per Config) — Familie von Fund H1 der Review-Nachlese.
`CoatingDetection` ueberspringt IDEAL heute nur **zufaellig** (`ProductionDate IS NOT NULL`, `:95`,
K1 leer) — nicht auf einen Nebeneffekt bauen. `HasCoatingParts` ← „Sub-FA selbst oder ein
direktes Kind `Beschichtet`" kommt aus der **Materialisierung** = K2 in
[[2026-08-20-materialisierung-fachliche-felder-spec]] (dort Rueckfrage 6). **Beschichtungstermin:**
`CoatingDateCalculator.Compute(vorkommissionierTermin, …)` (`:11-20`) rechnet aus dem
Vorkommissionier-Termin ← `ProductionDate` — bei IDEAL NULL → **kein Termin durch das Flag
allein**. Fuer IDEAL muss der Termin aus FAInfos `Start_Beschichtung` kommen → **Thema 2**
(Rueckfrage 5/6 dort); die beiden Specs weisen sich das gegenseitig zu.

### I — Sweep (aus der alten Spec uebernommen)
Vollstaendige Erhebung aller `vw_AKE_` / `[ake].[dbo]`-Fundstellen in Web und Service, je
Fundstelle ein schriftliches Urteil. Zusaetzlich zu klassifizieren: **`HasGlass`** (Glas-Bestellung
v1.25) — vermutlich ebenfalls BOM-Ableitung → Klasse-D-Kandidat.

## Fallstricke (fuer die Spec als AK verankern)
- **F-Cache:** hierarchischer Pfad benutzt den BOM-Cache weder lesend noch schreibend; ein
  gefuellter Cache veraendert das Ergebnis nicht (Erbe F1/F2 der alten Spec).
- **F-Menge:** keine zweite Multiplikation — ueber das Flag, nicht ueber Master-Abfragen.
- **F-Partition:** Aggregate nie ueber `FullStructure` (Property-Test „jeder Knoten genau einmal").
- **F-Zeilenschluessel:** Kollisionen in `FullStructure` (Abschnitt E).
- **F-AKE:** flache Implementierung ist eine **eigene Klasse**; bit-identisch beweisbar.
- **F-Signatur:** `BomKey` beruehrt AKE-Aufrufer mechanisch — jede Stelle einzeln, Tests gruen.

## Test-Anforderungen
- FA-native Methoden je Scope (Unit, InMemory ueber `FaHierarchyNode`).
- **Property-Test:** Vereinigung aller `DirectChildren` ueber alle materialisierten Sub-FAs einer
  HauptFA = alle Nicht-Wurzel-Knoten, ohne Doppelung (ausser Blatt-Waisen, explizit).
- Mengen-Flag an den drei Stellen; Mapping Feld fuer Feld; Zeilenschluessel-Eindeutigkeit in
  `FullStructure`; Klasse-D-Gates.
- AKE-Regression: bestehende BOM-/Picking-Tests unveraendert gruen; Cache-Treffer und -Miss.
- Manual-UAT: HauptFA-Vollansicht (Pick-Zustand je Zeile, Baugruppen-Gruppierung), Sub-FA-Ansicht,
  `Kommissionieren`-Filter, Artikelinfo HauptFA/Sub-FA, Menue „Kommissionierung" mit beiden
  Einstiegen, Flachmodus bit-identisch.

## Reihenfolge / Einordnung
- **Nach dem Buendel-Merge, aus `main`** (kein Artefakt noetig, das nicht in `main` liegt).
- Supersedes [[2026-08-18-ake-view-abhaengigkeiten-hierarchisch-spec]] (F1/F2 kehren sich um,
  Freigabe-Antwort 2 korrigiert, F3 wird zum Mengen-Flag, Sweep uebernommen).
- Verzahnt mit Thema 2 [[2026-08-20-materialisierung-fachliche-felder-spec]]: K2 `HasCoatingParts`
  dort, Beschichtungstermin aus FAInfos dort; hier nur die Gates.
- Folgeblock [[2026-09-08-arbeitsgaenge-aus-arbeitsschritte]].

## Offene Fragen (Schranke 1)
1. Zusammensetzung des `BomPosition`-Schluessels in `FullStructure` bestaetigen
   (`VaterSubFA.Position`) — oder Knoten-Id?
2. `Baugruppe`-Semantik der AKE-View (Gruppierungswert) gegen `SubFA <> 0` am `ReadOnlyBomBuilder`
   (5 Verwendungen) verifizieren — Mapping-Detail, keine Richtungsfrage.
3. `HasGlass`: BOM-Ableitung? Wenn ja, IDEAL-Quelle benennen (Artikeltyp/Artikelgruppe?).
