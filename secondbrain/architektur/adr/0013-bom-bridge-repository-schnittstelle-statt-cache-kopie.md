---
type: adr
id: 0013
title: Stueckliste im hierarchischen Modus ueber eine zweite Implementierung der Repository-Schnittstelle, nicht ueber eine Kopie in den BOM-Cache
status: accepted
date: 2026-09-08
supersedes: ""
superseded_by: ""
---

## Kontext und Problem

Am Standort IDEAL (Master `ProduktionsauftragHierarchisch = true`) liegt die Stueckliste eines
Fertigungsauftrags nicht in der AKE-Sage-View `vw_AKE_Kommissionierung_StuecklistenDB`, sondern in der
lokalen Struktur-Tabelle `FaHierarchyNodes` (Teil 1, alle 15 Minuten Full-Refresh). Der gesamte
AKE-Werkzeugkasten, der auf der Stueckliste aufsetzt — Picking-Workflow mit Pick-Status je Zeile,
Vorbau-/Vervollstaendigungs-Stueckliste (`ReadOnlyBomBuilder`), Bedarfsmeldung/Lagerbestellung aus
einer Zeile, Artikelinfo („in welchen Geraeten kommt Artikel X vor") — soll fuer IDEAL unveraendert
nutzbar sein. Bis dahin endete der BOM-Knopf im hierarchischen Modus in einem Minimal-Guard
(Hinweisseite, [[2026-08-18-bom-guard-hierarchisch-spec]]).

Die naheliegende Idee war, den persistenten AKE-BOM-Cache (`CachedBomHeaders`/`CachedBomItems`) aus der
Hierarchie zu **befuellen**, damit „alles wie bei AKE" laeuft. Vier Befunde aus der Code-Pruefung
(2026-09-08) sprachen dagegen:

1. **Alle Web-Verbraucher haengen an einer Schnittstelle.** `IBomRepository` (1 Methode) und
   `IBomCacheRepository` (7 Methoden) mit der einzigen Implementierung `BomCacheRepository`; kein
   Web-Raw-SQL auf die Cache-Tabellen, keine Web-Schreibzugriffe. Eine zweite Implementierung deckt
   alles ab.
2. **Die Cache-Semantik passt nicht zu IDEAL.** Der Cache ist auf **Artikelnummer** geschluesselt und
   fuehrt `Menge` **je Stueck** (Verbraucher rechnen `Menge × Quantity`). IDEAL-`Sollmenge` ist bereits
   die Auftragsgesamtmenge, und Positions-Attribute sind auftragsspezifisch. Ein Artikel-Cache wuerde
   doppelt multiplizieren und zwei FAs desselben Artikels dieselbe Stueckliste zuweisen — still.
3. **Die Struktur ist bereits lokal.** Der Cache existiert nur, weil die Sage-View ein teurer
   Fremdzugriff ist. Eine Kopie waere Duplikation mit Lag, Purge-/Hash-Logik und Vergiftungsrisiko.
4. **Zwei Verbraucher-Klassen.** Klasse P (Praesentation/Workflow) laesst sich ueber die Schnittstelle
   bedienen. Klasse D (`CoatingDetectionService`, `FaWorkStepDetectionService`, `BomCacheSyncService`)
   sind AKE-**Heuristiken** (Artikelkategorie-Match, Text-Contains), die auf IDEAL-Daten Fehldaten
   erzeugen — IDEAL liefert die Wahrheit explizit (`Beschichtet`, `Arbeitsschritte`, FAInfos).

## Betrachtete Optionen

- **Cache speisen (FA-geschluesselt):** neuer Sync-Zweig, Migration fuer Schluesselspalte, Purge nach
  `Source`, Hash-Logik; Klasse D liefe und riete. Korrekt nur mit grossem Umbau, dauerhaft zwei
  Wahrheiten.
- **Per-Stueck-Normalisierung in den Artikel-Cache:** null Verbraucher-Aenderung, aber verlustbehaftete
  Division und stille Falschtreffer bei auftragsspezifischen Abweichungen.
- **Guard behalten, eigene Stuecklisten-Seite:** nur der BOM-Knopf; Picking-Workflow, Bedarfe,
  Artikelinfo blieben fuer IDEAL blind.
- **Bridge an der Schnittstelle (gewaehlt):** `FaHierarchyBomRepository` implementiert beide
  Interfaces aus `FaHierarchyNode`; eine Weiche waehlt pro Aufruf am Master.

## Entscheidung

- `FaHierarchyBomRepository : IBomRepository, IBomCacheRepository` liest ausschliesslich aus
  `IFaHierarchyNodeRepository` (5-min-Cache-Decorator aus Teil 1), traversiert per
  `FaHierarchyTreeBuilder` (kein zweiter Baum-Walker), kein MemoryCache-Decorator, kein Zugriff auf den
  AKE-Cache; Schreibmethoden sind No-op mit Log.
- `BomRepositoryMasterSwitch` bedient **beide** Interfaces und entscheidet **pro Aufruf** am Master
  (`HierarchischeStrukturKeys.Master` ueber `IServiceSettingRepository`). Ziele werden **lazy** per
  Delegate aufgeloest, weil `BomRepository` selbst `IBomCacheRepository` (= die Weiche) injiziert —
  eager waere ein DI-Zyklus. Master `false` → unveraendert `CachedBomRepository`/`BomCacheRepository`.
- Schluessel `BomKey(ArticleNumber, SubOrderNumber, OrderNumber)` statt `string`; die flache
  Implementierung liest nur `ArticleNumber` (bit-identisch). Scope `BomScope`: HauptFA →
  `FullStructure` (alle Ebenen, flach, rekursiver Positions-Pfad `3.7.2`), Sub-FA → `DirectChildren`.
  **Aggregate nutzen immer `DirectChildren`** (jeder Knoten genau einmal).
- Das Ergebnis beschreibt seine Mengen-Semantik selbst (`BomQueryResult.MengeIstAuftragsmenge`);
  `BomQuantityResolver` ist die einzige Multiplikationsstelle — keine Master-Abfrage in Verbrauchern.
- Klasse D wird im hierarchischen Modus **hart im Code** abgeschaltet (`IHierarchicalModeReader`,
  DB-first), nicht auf die Struktur umgestellt. Ersatzquellen (K2 `HasCoatingParts`, Arbeitsgaenge
  aus `Arbeitsschritte`) sind eigene Specs.

## Konsequenzen

**Positiv**
- Eine Wahrheit (die Struktur), kein Lag, keine Cache-Vergiftung, keine Migration.
- AKE-Pfad ist eine eigene Klasse — bit-identisch beweisbar; die Signaturaenderung ist mechanisch.
- Doppelte Multiplikation ist strukturell unmoeglich (Flag am Ergebnis), nicht nur „bedacht".
- Der rekursive Positions-Pfad laesst die bestehende Baum-Logik in `Bom.cshtml` (`TreeLevel` =
  Punkte, `parentPos` = Praefix) unveraendert laufen.

**Negativ / Risiken**
- Zwei Settings-Reads je BOM-Seite im Flachmodus (pro-Aufruf-Weiche) — bewusst, Settings-Read ist
  billig.
- Die Weiche ist nur statisch/unit-getestet; der Lazy-Graph in `Program.cs` braucht einen
  DI-Aufloesungstest und den App-Start als Beweis (siehe [[fallstricke]] §10).
- Artikelinfo-Methoden liefern im hierarchischen Modus Eltern-Sub-FA-Nummern statt
  Geraete-Artikelnummern — der Aufrufer (`ArticlesController`) muss das kennen (eigener Zweig).
- Pick-Status lebt je Auftrag: dasselbe Teil hat in der HauptFA-Vollansicht und in der Sub-FA-Ansicht
  unabhaengige Zustaende (fachlich akzeptiert).

Bezug: [[0001-repository-pattern-mit-decorator-fuer-caching]],
[[0007-bom-quelle-sage-view-mit-oseon-fallback]], [[0012-fa-hierarchie-einweg-migrationstor]],
Spec [[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]].
