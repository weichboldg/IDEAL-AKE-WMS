---
type: spec
title: "BOM-Knopf im hierarchischen Modus abfangen statt HTTP 500 (UAT-Blocker, Minimal-Fix)"
slug: 2026-08-18-bom-guard-hierarchisch-spec
status: InUmsetzung
created: 2026-08-18
updated: 2026-08-18
source_backlog: "[[2026-08-18-ideal-nachlese-restarbeiten]]"
depends_on: "[[2026-07-29-standort-ideal-teil-7-spec]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
epic: false
etappen: []
open_questions: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-08-18
---

## Ziel / Nutzen (das Warum)

Nach dem Umlegen des Masters `ProduktionsauftragHierarchisch` am 2026-08-18 stehen 130
materialisierte IDEAL-Auftraege in `ProductionOrders` — und damit auch in FA-Liste und
Kommissionierung. **Ein Klick auf den BOM-Knopf endet dort in HTTP 500:**

```
SqlException: Ungueltiger Objektname "ake.dbo.vw_AKE_Kommissionierung_StuecklistenDB". (208)
   at BomRepository.GetBomItemsAsync(...)  BomRepository.cs:44
   at PickingController.Bom(Int32 id, String filterText)  PickingController.cs:235
```

`BomRepository` fragt bei Cache-Miss live gegen die **AKE-View** — Datenbankname und View fest im
SQL. Auf einem IDEAL-System existiert beides nicht.

**Warum das jetzt gefixt wird, obwohl es in keinem UAT-Punkt steht:** Der Fehler steht in keiner der
30 Checklisten-Positionen (Punkt 11 prueft die AKE-Regression ausdruecklich **mit Toggles auf
`false`**). Er wurde trotzdem gefunden — im normalen Gebrauch, wenige Minuten nach dem Umschalten.
Wer die UAT an echten Strukturen macht, trifft ihn ebenso. Eine Fehlerseite mitten in einer Abnahme
kostet mehr Zeit und Vertrauen, als dieser Fix wert ist.

**Warum NUR ein Minimal-Fix:** Die vollstaendige Loesung
([[2026-08-18-ake-view-abhaengigkeiten-hierarchisch-spec]]) traegt acht Fallstricke, drei davon mit
stillen Falschdaten (Cache-Vergiftung, Cache-First-Reihenfolge, Mengen-Semantik). Das gehoert nicht
in einen Zweig, der mit frischer QA-Evidenz (Web 1174 / Service 221) auf Abnahme wartet. Hier wird
**nur** der Absturz beseitigt — kein neues Repository, kein Cache-Umbau, kein Schluesselwechsel.

## Umfang

**In-Scope**
1. Guard: Im hierarchischen Modus wird der AKE-Stuecklisten-Zugriff **gar nicht erst ausgefuehrt**.
2. Verstaendlicher Hinweis in der Oberflaeche mit Verweis auf `/FaHierarchy`.
3. Sweep der **Aufrufer** von `IBomRepository` — damit nicht ein anderer Einstieg weiterhin wirft.
4. **`supportsSortDefault` der drei IDEAL-Listen auf `true`** (Entscheidung 2026-08-18).
   Sachfremd zum Guard, aber bewusst mitgenommen: Der Epic wird fuer den Guard ohnehin einmal
   geoeffnet und die QA erneut gefahren — diese drei Zeilen kosten dabei nichts. Getrennt haetten
   sie einen eigenen Reopen-Zyklus mit eigener QA erfordert.
   Betroffen: `FaHierarchyKommissionierListen`, `FaHierarchyBeschichtung`,
   `FaHierarchyVormontageEinzeln` (die drei flachen Listen).
   **`FaHierarchyStructure` (Baum) bleibt `false`** — dort ist die Zeilenreihenfolge die Hierarchie;
   eine gespeicherte Spaltensortierung waere semantisch sinnlos.
   Hintergrund: Der Wert stand defensiv auf `false`, weil `table-filter.js` nur das erste `<tbody>`
   sortierte. Der Defekt ist mit Etappe 6 behoben
   ([[2026-08-12-tabellen-sortierung-nur-erste-gruppe-bug]]) — der Grund ist weggefallen.
   **Testszenarien nachziehen:** Die bestehende Vorgabe „bieten *keine* Option Standard-Sortierung"
   (UAT-Punkt 18) kehrt sich fuer diese drei Listen um.

**Out-of-Scope** (bewusst, gehoert in die Folge-Spec)
- Eine hierarchische BOM-Quelle, `FaHierarchyBomRepository`, Cache-Umgehung, Schluesselwechsel.
- Der Sweep ueber alle uebrigen `vw_AKE_`/`[ake].[dbo]`-Fundstellen.

**Fachlicher Hintergrund, warum ein Hinweis hier vertretbar ist:** Die FA-Struktur-Ansicht **ist**
bereits die Stueckliste (Befund B4: die IDEAL-`FAListe`-View liefert Struktur UND Stueckliste in
einem). Ein IDEAL-Anwender verliert also nichts — er sieht dieselben Daten unter `/FaHierarchy`,
sogar vollstaendiger.

## Technischer Loesungsentwurf

### Wo der Guard sitzt: im Repository, nicht im Controller

**Entscheidung:** Der Guard sitzt in der `IBomRepository`-Kette (Decorator oder
`BomRepository.GetBomItemsAsync` ganz vorn), **nicht** nur in `PickingController.Bom`.
Begruendung: Ein Controller-Guard schuetzt genau einen Einstieg. Ruft ein anderer Aufrufer
(BDE-Terminal, Fehlteile, eine API) dieselbe Methode, wirft es dort weiter — und der naechste 500er
faellt an einer Stelle auf, an der niemand ihn erwartet.

### Verhalten

- **Master `false` (AKE):** unveraendert. Cache-First, dann Live gegen die AKE-View. **Kein**
  zusaetzlicher Code im Pfad, keine Verhaltensaenderung.
- **Master `true` (hierarchisch):** kein DB-Zugriff. Rueckgabe eines leeren `BomQueryResult` mit
  einer eigenen Quellen-Kennung (z. B. `NICHT_VERFUEGBAR_HIERARCHISCH`), damit die Oberflaeche den
  Zustand von „Stueckliste ist leer" unterscheiden kann.
- **Anzeige:** Statt einer leeren Tabelle ein Hinweis: *„Die Stueckliste wird im hierarchischen
  Modus ueber die FA-Struktur angezeigt"* mit Link auf `/FaHierarchy`. Kein Fehler-Look — es ist
  kein Fehler, sondern ein anderer Weg.
- **Optional, wenn ohne Suchaufwand moeglich:** den BOM-Knopf im hierarchischen Modus ausblenden.
  **Der Guard bleibt trotzdem Pflicht** — er faengt Direktaufrufe der URL und alle uebrigen
  Aufrufer ab.

### Master-Zustand lesen — bestehenden Weg wiederverwenden

Teil 7 hat fuer die `HierarchieUmstellung`-Seite bereits einen web-seitigen Zugriff auf den
Master-Zustand samt gecachtem Sperrstatus gebaut. **Diesen wiederverwenden** — kein zweiter Leseweg,
kein neuer Settings-Key, keine eigene Caching-Logik.

## Fallstricke

1. **Kein `try/catch` um das SQL.** Verlockend, aber falsch: Ein Fangnetz um die Abfrage wuerde
   **jeden** SQL-Fehler verschlucken — auch echte (Timeout, Rechte, Tippfehler im View-Namen) — und
   sie als „Stueckliste nicht verfuegbar" ausgeben. Der Guard muss **explizit am Master-Schalter**
   haengen, nicht am Scheitern.
2. **Leeres Ergebnis darf nicht wie „keine Positionen" aussehen.** Ohne eigene Quellen-Kennung
   waere ein Auftrag ohne Stueckliste nicht von „im hierarchischen Modus nicht verfuegbar" zu
   unterscheiden — der Anwender haelte die Stueckliste fuer leer.
3. **Andere Aufrufer nicht vergessen.** Vor der Umsetzung die Aufrufer von `GetBomItemsAsync` /
   `IBomRepository` erheben. Der BOM-Knopf war der erste, der angeklickt wurde — nicht zwingend der
   einzige.
4. **Nicht zum Feature ausbauen.** Sobald jemand anfaengt, „dann koennten wir doch gleich aus
   `FaHierarchyNode` lesen", ist es die Folge-Spec mit ihren acht Fallstricken. **Hier nicht.**
   Der Hinweistext muss erkennbar als Zwischenstand formuliert sein, damit niemand BOM fuer IDEAL
   als „erledigt" verbucht.
5. **`OseonConnection` im Konstruktor.** `BomRepository` holt sie im Ctor und wirft bei Fehlen. Auf
   diesem System ist sie gesetzt (der Fehler kam aus dem SQL, nicht aus dem Ctor) — aber falls der
   Guard so frueh sitzt, dass der Ctor gar nicht mehr laufen muss, ist das ein Nebengewinn. Nicht
   erzwingen, nur nicht verschlechtern.

## Akzeptanzkriterien

1. **AKE unveraendert:** Bei Master `false` verhaelt sich der BOM-Knopf bit-identisch zu vorher —
   Cache-Treffer und Cache-Miss je einmal nachgewiesen.
2. **Kein 500 mehr:** Bei Master `true` liefert `/Picking/Bom/<id>` eine normale Seite mit Hinweis,
   keine Exception, **kein** SQL-Zugriff auf `[ake].[dbo]` (im Log nachweisbar).
3. **Guard haengt am Schalter, nicht am Fehler** — kein `try/catch` um die AKE-Abfrage.
4. **Unterscheidbarkeit:** Der Zustand „hierarchisch, nicht verfuegbar" ist im `BomQueryResult`
   maschinell von „leere Stueckliste" unterscheidbar.
5. **Alle Aufrufer abgedeckt:** Jeder erhobene Aufrufer von `IBomRepository` laeuft im
   hierarchischen Modus ohne Exception.
6. **Hinweis verweist auf `/FaHierarchy`** und ist als Zwischenstand erkennbar formuliert.

## Test-Szenarien

- **AKE-Regression:** Master `false`, BOM-Knopf an bekanntem Auftrag — Positionen wie bisher
  (einmal mit gefuelltem, einmal mit leerem Cache).
- **IDEAL-Grundfall:** Master `true`, BOM-Knopf an einem materialisierten FA — Hinweisseite, kein
  Fehler, Link auf `/FaHierarchy` funktioniert.
- **Direktaufruf:** `/Picking/Bom/<id>` per URL im hierarchischen Modus — gleicher Hinweis (Beleg,
  dass der Guard nicht nur an einem ausgeblendeten Knopf haengt).
- **Log-Nachweis:** Im hierarchischen Modus erscheint **keine** SQL-Abfrage gegen
  `vw_AKE_Kommissionierung_StuecklistenDB`.
- **Weitere Aufrufer:** jeder erhobene Einstieg einmal im hierarchischen Modus aufrufen.

## Deploy

Reine Web-Aenderung: `deploy.web = true`, `service = false`, `migration = false`. Geht mit dem
bestehenden Web-Publish des Buendels mit — **kein** zusaetzlicher Deploy-Schritt.

**Folgen fuer das Buendel:** Der Epic geht von `Testbereit` auf `InUmsetzung` zurueck; nach der
Umsetzung ist die **QA erneut zu fahren** (Build + beide Test-Suiten), bevor Schranke 2 ansteht. Das
ist der bewusst getragene Preis dafuer, die UAT nicht in einen 500er laufen zu lassen. Version bleibt
unveraendert (v1.31.0/v1.32.0/v1.33.0 wie gehabt) — kein eigener Bump fuer einen Guard.

**Ergaenzung der UAT-Checkliste:** ein Punkt „BOM-Knopf im hierarchischen Modus zeigt den Hinweis
statt einer Fehlerseite" — damit der Fix in der Abnahme auch bestaetigt wird.

## Offene Rueckfragen

Keine. Umfang, Verhalten und Abgrenzung sind entschieden; die vollstaendige Loesung ist als eigene
Spec ausgelagert.

## Freigabe-Antworten (2026-08-18)

1. **Guard wird VORGEZOGEN** — vor dem Merge, im bestehenden Epic-Worktree. Der Epic geht dafuer
   von `Testbereit` auf `InUmsetzung` zurueck; die QA ist danach erneut zu fahren (Build + beide
   Suiten), bevor Schranke 2 ansteht. Bewusst getragener Preis: Die UAT soll nicht in eine
   Fehlerseite laufen.
2. **`supportsSortDefault` auf `true`** fuer die drei flachen IDEAL-Listen (Umfang Punkt 4);
   der Baum bleibt `false`.
3. **Die Vollloesung** ([[2026-08-18-ake-view-abhaengigkeiten-hierarchisch-spec]]) bleibt
   **ausserhalb** dieses Laufs und ist als **erster Block des naechsten Entwicklungszyklus**
   gesetzt — nach dem Merge, aus `main`. Kein Widerspruch zum Vorziehen des Guards: Der Guard
   verhindert das Betreten eines Datenpfads, die Vollloesung baut einen.

Damit ist die Spec startklar — Status setzen und nach `specs/freigegeben/` verschieben.
