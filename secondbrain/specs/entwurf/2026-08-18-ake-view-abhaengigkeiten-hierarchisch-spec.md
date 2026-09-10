---
type: spec
title: "IDEAL: AKE-View-Abhaengigkeiten hierarchiefaehig machen (Stueckliste/BOM zuerst)"
slug: 2026-08-18-ake-view-abhaengigkeiten-hierarchisch-spec
status: Ueberholt
superseded_by: "[[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]]"
created: 2026-08-18
updated: 2026-09-08
source_backlog: "[[2026-07-29-Standort-IDEAL]]"
depends_on: "[[2026-07-29-standort-ideal-teil-7-spec]]"
task: ""
worktree: ""
branch: ""
epic: false
etappen: []
open_questions: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

> [!warning] UEBERHOLT — KEIN AUFTRAG (Stand 2026-09-08)
> Abgeloest durch **[[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]]**
> (aus der Backlog-Notiz [[2026-09-08-bom-schnittstellen-bridge-hierarchisch]]).
> **Diesen Entwurf nicht mehr freigeben und nicht als Umsetzungsgrundlage verwenden.**
>
> **Was sich in der Nachfolge-Spec aendert — durch Code-Verifikation, die hier fehlte:**
> - **F1/F2 kehren sich um:** Kein Cache-Zugriff bleibt bestehen, aber die Bridge bedient **alle**
>   Schnittstellen-Methoden, nicht nur den BOM-Knopf.
> - **Freigabe-Antwort 2 unten ist FALSCH:** `Ressourcenummer` fehlt nicht — die FAListe-Spalte
>   `Artnr` **ist** die Ressourcenummer; Kopf- und Zeilenschluessel waren verwechselt.
> - **F3** wird zum selbstbeschreibenden Mengen-Flag.
> - **Neuer Scope `FullStructure`** fuer die HauptFA im Picking-Workflow, mit zusammengesetztem
>   Zeilenschluessel (vollstaendiger rekursiver Positionspfad).
> - Ausserdem am Code gefunden und hier nicht bekannt: `IBomCacheRepository` ist ungeschuetzt
>   registriert (der Guard deckt nur `IBomRepository`), und `BomCacheSyncService` hat **zwei**
>   oeffentliche Einstiege.
>
> **Was uebernommen wird:** der **Sweep** (Umfang 1/3).
>
> Als historische Referenz aufbewahrt — sie erklaert, warum der Guard gebaut wurde.

## Ziel / Nutzen (das Warum)

Am 2026-08-18 wurde der Master `ProduktionsauftragHierarchisch` erstmals auf einem IDEAL-System
aktiviert. Struktur-Sync (533 Knoten, 4 OrderInfos) und Materialisierung (130 Auftraege, 0
Umhaeng-Konflikte) liefen fehlerfrei. **Beim ersten Klick auf den BOM-Knopf in der Kommissionierung
folgte HTTP 500:**

```
Microsoft.Data.SqlClient.SqlException: Ungueltiger Objektname
"ake.dbo.vw_AKE_Kommissionierung_StuecklistenDB".  (Error 208)
   at BomRepository.GetBomItemsAsync(...)  BomRepository.cs:44
   at CachedBomRepository.GetBomItemsAsync(...)
   at PickingController.Bom(Int32 id, String filterText)  PickingController.cs:235
```

**Damit ist eine ZWEITE Fehlerklasse aufgedeckt**, die der bisherige Haertungsplan nicht abdeckt:
- **Klasse 1 (Teil 7, Etappe D):** `OrderNumber`-Lookups werden durch die Schema-Inversion
  mehrdeutig — sie liefern *zu viel*.
- **Klasse 2 (diese Spec):** Lesepfade greifen auf **hart verdrahtete AKE-Views bzw. die
  `[ake]`-Datenbank** zu. Auf einem IDEAL-System existieren die nicht — sie liefern *gar nichts*
  und werfen.

Klasse 2 faellt lauter aus (500 statt stiller Falschtreffer), ist aber breiter gestreut: Sie
betrifft jede Stelle, die eine `vw_AKE_*`-View oder `[ake].[dbo]` direkt anspricht. Der BOM-Knopf
ist nur der erste, der angeklickt wurde.

**Ziel:** Alle AKE-View-Abhaengigkeiten im Lesepfad hierarchiefaehig machen — mit dem
BOM-/Stuecklisten-Pfad als erstem und wichtigstem Fall, und einem vollstaendigen Sweep ueber die
uebrigen Fundstellen, **bevor** sie einzeln im Betrieb auffallen.

## Warum eine eigene Spec und nicht Teil 7, Etappe D

- Teil 7 ist umgesetzt; der Dev-Agent arbeitet parallel an Teil 8 im selben Worktree. Eine
  Erweiterung von Teil 7 wuerde einen abgeschlossenen Epic wieder aufreissen und mit dem laufenden
  Teil-8-Lauf um dieselben Dateien konkurrieren.
- Es ist fachlich eine andere Klasse (fehlende Datenquelle statt mehrdeutiger Schluessel) mit
  eigener Loesungsform (Repository-Weiche statt Lookup-Umstellung).
- Sie ist unabhaengig testbar und einzeln mergebar.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. **Sweep:** vollstaendige Erhebung aller Fundstellen mit `vw_AKE_` bzw. `[ake].[dbo]` (und
   sinngleichen Schreibweisen) in Web **und** Service. Ergebnis als Liste mit Urteil je Fundstelle.
2. **BOM/Stueckliste hierarchiefaehig** — der verifizierte Fall, vollstaendig umgesetzt.
3. Fuer die uebrigen Fundstellen je Fall: umsetzen, oder mit Begruendung als „nicht betroffen"
   bzw. „eigener Umfang" dokumentieren. **Kein Fundort bleibt unbewertet.**

**Out-of-Scope**
- Der `OrderNumber`-Sweep aus Teil 7 Etappe D (Klasse 1) — andere Ursache, dort behandelt.
- Schreibpfade nach Sage (Lagerbuchungen) — unveraendert.
- BDE/Rueckmeldung (Teil 8, laeuft parallel).

## Der verifizierte Fall: BOM/Stueckliste

### Ist-Zustand

`BomRepository.GetBomItemsAsync(string productionOrderArticleNumber)`:
1. **Cache-First** ueber `IBomCacheRepository.GetByArticleNumberAsync(artikelnummer)`.
2. Bei Miss: **Live-Query** gegen `[ake].[dbo].[vw_AKE_Kommissionierung_StuecklistenDB]`,
   gefiltert auf `Artikelnummer`.

Dekoriert von `CachedBomRepository`, aufgerufen aus `PickingController.Bom(int id, string filterText)`.

### Warum das im hierarchischen Modus nicht nur „eine andere View" ist

Die IDEAL-Stueckliste ist **strukturell etwas anderes** — das ist der Kern dieser Spec:

| | AKE heute | IDEAL |
|---|---|---|
| Quelle | Sage-View, live ueber Verbindung zur `[ake]`-DB | `FaHierarchyNode`, lokale Tabelle |
| Schluessel | **Artikelnummer** | **der Fertigungsauftrag** (`SubOrderNumber`/`SubFA`) |
| Gueltigkeit | je Artikel, auftragsuebergreifend gleich | je Auftrag, kann pro Auftrag abweichen |
| Cachebarkeit | sinnvoll (teurer Fremdzugriff) | ueberfluessig (schon lokal) |

Das deckt sich mit Befund B4 der Ideen-Notiz: **`vw_IDEAL-AKE_Kommissionierung_FAListe` IST die
Stueckliste** — eine Zeile = eine Position. Die Daten liegen nach dem Struktur-Sync bereits lokal
(im Beispiel-Lauf: 533 Knoten).

**Konsequenz:** Es genuegt nicht, den View-Namen auszutauschen. Schluessel, Quelle und
Cache-Verhalten aendern sich gemeinsam — deshalb eine eigene `IBomRepository`-Implementierung,
nicht ein `if` im bestehenden Repository.

## FALLSTRICKE (vor der Umsetzung lesen)

Acht Punkte, sortiert nach Schadenshoehe. Die ersten drei erzeugen **stille Falschdaten** —
gefaehrlicher als der 500er, der diese Spec ausgeloest hat.

> **Die acht Fallstricke im Detail stehen am Ende dieser Spec** (Abschnitt „Fallstricke im Detail").
> Sie sind vor der Umsetzung zu lesen — mehrere Akzeptanzkriterien verweisen darauf.

## Technischer Loesungsentwurf

### Weiche

Neue Implementierung `FaHierarchyBomRepository : IBomRepository`, die aus `FaHierarchyNode` liest.
Auswahl ueber den Master-Schalter `ProduktionsauftragHierarchisch` — **dasselbe Resolver-Muster wie
in den uebrigen Lesepfaden**, umgesetzt bei der DI-Registrierung bzw. ueber einen schlanken
Resolver, **nicht** als Verzweigung innerhalb von `BomRepository`.

Reihenfolge der Dekoration (wichtig wegen F1/F2):
```
hierarchisch:  Aufrufer -> FaHierarchyBomRepository            (KEIN Cache-Decorator)
flach (AKE):   Aufrufer -> CachedBomRepository -> BomRepository (unveraendert)
```

### Lesepfad hierarchisch

- Quelle: `FaHierarchyNode`, gefiltert auf die **direkten Kinder** des FA (F4).
- Schluessel: der FA aus dem Aufrufer (F5).
- Kein Zugriff auf `IBomCacheRepository`, keine OSEON-Verbindung (F1, F6).
- Ergebnis als `BomQueryResult` mit eigener Quellen-Kennung (z. B. `"FA-HIERARCHIE"` statt
  `"SAGE"`/`"CACHE"`) — damit in Anzeige und Log erkennbar ist, woher die Daten stammen. Das ist
  bei einem Umschaltverhalten die billigste Diagnosehilfe.

### Sweep (Teil 1 des Umfangs)

Suchbegriffe ueber Web **und** Service, inkl. `.cshtml` und `.sql`:
`vw_AKE_`, `[ake].[dbo]`, `ake.dbo`, `[ake]`

Je Fundstelle ein schriftliches Urteil:
- **betroffen** → hierarchische Entsprechung noetig (umsetzen oder als eigener Umfang benennen)
- **nicht betroffen** → mit Begruendung (z. B. laeuft nur bei Master=false, oder Sage-Standardtabelle
  statt AKE-spezifischer View)

Die Zahl der Fundstellen ist **nicht** vorab bekannt — die Liste entsteht im Sweep und ist keine
abzuhakende Menge.

## Akzeptanzkriterien

1. **AKE unveraendert:** Bei `ProduktionsauftragHierarchisch = false` verhaelt sich der
   BOM-Knopf **bit-identisch** zu heute — Cache-First, dann Live gegen die AKE-View.
2. **IDEAL funktioniert:** Bei Master `true` liefert der BOM-Knopf die Positionen aus
   `FaHierarchyNode`, ohne 500 und ohne Zugriff auf `[ake].[dbo]`.
3. **Kein Cache im hierarchischen Pfad** — weder lesend noch schreibend (F1/F2). Nachweisbar:
   Ein gefuellter BOM-Cache veraendert das Ergebnis im hierarchischen Modus **nicht**.
4. **Ebenen-Regel:** Es erscheinen ausschliesslich die **direkten Kinder** des FA; Enkel und tiefere
   Ebenen nicht (F4).
5. **Mengen-Semantik geklaert und dokumentiert** (F3): Ergebnis der Datenpruefung steht in der
   Spec, und die angezeigte Menge stimmt an einem FA mit Auftragsmenge > 1 nachweislich.
6. **Keine OSEON-Voraussetzung** im hierarchischen Pfad (F6).
7. **Feld-Mapping dokumentiert**; nicht befuellbare Spalten sind bewusst ausgeblendet oder leer,
   und kein Filter/keine Sortierung laeuft still ins Leere (F7).
8. **Sweep vollstaendig:** Jede gefundene `vw_AKE_`/`[ake]`-Fundstelle traegt ein schriftliches
   Urteil. Kein Fundort bleibt unbewertet.
9. **Quellen-Kennung** im `BomQueryResult` unterscheidet hierarchisch/AKE.

## Test-Szenarien

- **AKE-Regression:** Bei Master=false BOM-Knopf an einem bekannten Auftrag — Ergebnis identisch
  zum Stand vor der Aenderung (Cache-Treffer und Cache-Miss je einmal).
- **IDEAL-Grundfall:** Master=true, BOM-Knopf am materialisierten FA — Positionen erscheinen,
  keine Exception, Quelle als hierarchisch gekennzeichnet.
- **Cache-Immunitaet (F1/F2):** BOM-Cache mit Eintraegen zum selben Artikel befuellen, im
  hierarchischen Modus oeffnen — die Cache-Daten duerfen **nicht** erscheinen.
- **Zwei Auftraege, ein Artikel (F1):** Zwei FAs mit demselben Artikel, aber unterschiedlichen
  Positionen — beide zeigen ihre **eigene** Stueckliste.
- **Ebenen (F4):** FA mit Enkelknoten — nur die direkten Kinder erscheinen.
- **Menge (F3):** FA mit Auftragsmenge > 1 — angezeigte Menge gegen die Erwartung aus der
  Datenpruefung.
- **Leerfall:** FA ohne Kindknoten — leere Liste mit Hinweis, kein Fehler.

Vorbedingung: produktivnahe IDEAL-Daten im Testsystem (wie bei Teil 1–5). Der AKE-Regressionsteil
ist unabhaengig davon pruefbar.

## Offene Rueckfragen

1. **Mengen-Semantik (F3):** Ist `Sollmenge` in `FaHierarchyNode` bereits die Auftragsgesamtmenge,
   oder eine Menge je Stueck? — an echten Daten zu pruefen, bevor gebaut wird.
2. **Feld-Mapping (F7):** Welche der AKE-Spalten sind im hierarchischen Modus entbehrlich, welche
   muessen ersetzt werden (z. B. `Ressourcenummer`, `Artikelgruppe`)?
3. **Reihenfolge zu Teil 8 (F8):** danach umsetzen, oder abgestimmt parallel?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. → **`Sollmenge` ist die Auftragsgesamtmenge. [GEKLAERT — F3 entschaerft]**
   Damit ist im hierarchischen Pfad **keine** Hochrechnung mit der Auftragsmenge vorzunehmen; die
   Menge wird uebernommen, wie sie in `FaHierarchyNode` steht.
   **Bleibt zu pruefen (der eigentliche Fallstrick, jetzt praeziser):** Rechnet der bestehende
   Aufrufer (`PickingController.Bom` / die BOM-Ansicht) die AKE-`Menge` heute mit der Auftrags-
   menge hoch? Wenn ja, muss diese Multiplikation im hierarchischen Pfad **unterbleiben** — sonst
   wird die bereits vollstaendige Menge ein zweites Mal multipliziert. Als AK verankern und am
   FA mit Auftragsmenge > 1 nachweisen.

2. → **Fast alle Felder sind vorhanden — es fehlt genau eines: `Ressourcenummer`.**
   Am Modell `FaHierarchyNode` geprueft (2026-08-18):

   | AKE-View-Spalte | Entsprechung in `FaHierarchyNode` |
   |---|---|
   | `Artikelnummer` | `Artnr` |
   | `Position` | `Position` |
   | `Baugruppe` | ableitbar: `SubFA != 0` (Baugruppe mit eigenem FA) |
   | `Bezeichnung1` / `Bezeichnung2` | gleichnamig |
   | `Menge` | `Sollmenge` (zusaetzlich `Fertigungmenge` verfuegbar) |
   | `Beschaffungsartikel` | gleichnamig (bool) |
   | `Artikelgruppe` | gleichnamig |
   | **`Ressourcenummer`** | **fehlt** |

   **Vorgabe fuer die fehlende `Ressourcenummer`:** Im hierarchischen Modus bleibt die Spalte
   **leer** — sie wird **nicht** aus `Artnr` erzeugt. Der Anhang benennt Artikelnummer und
   Ressourcenummer ausdruecklich als **verschiedene** Schluessel; sie gleichzusetzen erzeugte einen
   stillen Falschtreffer statt einer sichtbaren Luecke.
   Zusaetzlich zu pruefen und in der Spec festzuhalten: **Wird `Ressourcenummer` in der BOM-Ansicht
   ueberhaupt angezeigt oder ausgewertet** (Sortierung, Filter, Verknuepfung)? Falls ja, wird die
   Spalte im hierarchischen Modus ausgeblendet — eine sichtbar leere Spalte, auf die ein Filter
   wirkt, liefert sonst eine leere Liste ohne erkennbaren Grund (F7).
   **Bonus, nicht Pflicht:** `FaHierarchyNode` fuehrt Felder, die die AKE-View nicht hat
   (`Matchcode`, `Hauptlagerplatz`, `Arbeitsbereich`, `Artikeltyp`, `Material`, `Beschichtet`,
   `EKBedarf`, Masse). Ob davon etwas in der BOM-Ansicht sinnvoll ist, ist eine eigene Frage —
   **nicht** in diesem Umfang, um die Regressionsgarantie fuer AKE einfach zu halten.

3. → **Teil 8 ist fertig; diese Spec kann jetzt laufen. [F8 entschaerft]**
   Stand 2026-08-18: Teile 1-5 (v1.31.0), Teil 7 (v1.32.0) und Teil 8 (v1.33.0) stehen alle auf
   `Testbereit` im Zweig `feature/2026-08-07-ideal-teile-1-5`; der qa-agent hat Teil 8 mit gruenem
   Beweis passiert (Web 1205, Service 231). Es laeuft kein paralleler Dev-Lauf mehr.

   **Aber eine Reihenfolge-Entscheidung ist noetig — und sie ist nicht trivial:**
   Das Buendel ist **abnahmereif und wartet auf Schranke 2** (Manual-UAT + Merge). Diese Spec jetzt
   in denselben Zweig zu legen wuerde den Zustand `Testbereit` wieder aufreissen und die QA-Evidenz
   entwerten — dieselbe Abwaegung wie bei den beiden Darstellungs-Specs, nur diesmal mit einem
   Buendel aus drei Versionen dahinter.
   **Empfehlung: erst mergen, dann diese Spec von `main` aus bauen.** Sie braucht kein Artefakt
   aus dem Zweig, das nicht nach dem Merge in `main` liegt — `FaHierarchyNode` und der
   Master-Schalter sind dann dort. Damit bleibt die Abnahme des grossen Buendels sauber geschnitten,
   und der BOM-Fix wird ein kleiner, eigener Durchlauf mit ueberschaubarem Testumfang.
   **Gegenargument, das dagegen spricht:** Der BOM-Knopf wirft im hierarchischen Modus einen 500er.
   Wer nach dem Merge auf dem Testsystem arbeitet, laeuft hinein. Falls das die UAT behindert, kann
   der Fix vorgezogen werden — dann aber bewusst mit erneuter QA fuer das gesamte Buendel.
   **Zu entscheiden: vorher oder nachher? — ENTSCHIEDEN (2026-08-18): NACHHER.**
   Der Guard (Minimal-Fix, [[2026-08-18-bom-guard-hierarchisch-spec]]) wird **vorgezogen** und
   verhindert die Fehlerseite waehrend der UAT. **Diese Spec bleibt draussen** und ist als **erster
   Block des naechsten Entwicklungszyklus** gesetzt — nach dem Merge, in einem eigenen Worktree von
   `main`. Begruendung: acht Fallstricke, drei davon mit stillen Falschdaten — das gehoert nicht in
   einen Zweig, der mit frischer QA-Evidenz auf Abnahme wartet.
   `worktree`/`branch` bleiben daher **leer**, bis der Merge durch ist.

## Fallstricke im Detail

### F1 — Cache-Vergiftung (hoechstes Risiko)

`BomCacheRepository` ist auf **Artikelnummer** geschluesselt. Die IDEAL-Stueckliste ist
**auftragsspezifisch**. Wuerde der hierarchische Pfad in denselben Cache schreiben, erhielten zwei
Auftraege desselben Artikels dieselbe Stueckliste — **ohne Fehler, ohne Meldung**. Der Werker
kommissioniert dann nach der Stueckliste eines fremden Auftrags.

**Vorgabe:** Der hierarchische Pfad benutzt den BOM-Cache **weder lesend noch schreibend**.

### F2 — Cache-First muss VOLLSTAENDIG uebersprungen werden, nicht nur der Live-Zweig

Der heutige Ablauf ist Cache-First, dann Live. Ersetzt man nur den Live-Zweig, liefert ein
zufaellig gefuellter Cache (Testreste, ein einmal gelaufener `BomCacheSyncService`) weiterhin
AKE-Daten — und zwar **erfolgreich**. Aus einem lauten 500er wuerde ein stiller Falschtreffer.
Dass der Fehler ueberhaupt auftrat, beweist nur, dass der Cache gerade leer war.

**Vorgabe:** Die Weiche greift **vor** dem Cache-Zugriff, nicht darin.

### F3 — Mengen-Semantik: Menge je Stueck vs. Menge je Auftrag

Die AKE-View liefert `Menge`; `FaHierarchyNode` fuehrt `Sollmenge`/`Fertigungmenge`. **Es ist nicht
belegt, dass das dieselbe Bezugsgroesse ist.** Ist die AKE-`Menge` „pro 1 Stueck Endprodukt" und
rechnet der Aufrufer sie mit der Auftragsmenge hoch, waehrend die Struktur bereits
auftragsbezogene Mengen fuehrt, wird **doppelt multipliziert** — falsche Kommissioniermengen, ohne
jede Fehlermeldung.

**Vorgabe:** Vor der Umsetzung an echten Daten pruefen: Ein FA mit Auftragsmenge > 1 auswaehlen und
vergleichen, ob `Sollmenge` bereits die Gesamtmenge ist. Ergebnis in der Spec festhalten. Falls
noetig, im Aufrufer keine zweite Multiplikation.

### F4 — Ebenen-Frage: direkte Kinder oder alle Nachfahren?

Die AKE-Stueckliste ist **einstufig** (die Positionen des Artikels). Die IDEAL-Struktur ist
mehrstufig. „Die Stueckliste dieses FA" kann daher zweierlei heissen.

**Entscheidung: direkte Kinder** — die Positionen, die an *diesem* FA verbaut werden
(`ParentSubOrderNumber`/`VaterFA` == der FA). Begruendung: Das entspricht der AKE-Semantik, und der
Werker an dieser Werkbank holt genau dieses Material. Eine Gesamtaufloesung ueber alle Ebenen waere
eine andere Funktion (und wuerde Baugruppen doppelt mit ihren Bestandteilen zeigen — vgl. die
Summierungs-Falle aus Teil 3).

### F5 — Schluesselwechsel: Signatur traegt den Artikel, gebraucht wird der FA

`GetBomItemsAsync(string productionOrderArticleNumber)` traegt die **Artikelnummer**. Der
hierarchische Pfad braucht den **FA**. Die Artikelnummer als Schluessel zu behalten und intern auf
einen FA zu mappen ist **nicht zulaessig**: Zu einem Artikel koennen mehrere FAs gehoeren — die
Abbildung waere mehrdeutig und traefe im Zweifel den falschen.

**Vorgabe:** Der Aufrufer (`PickingController.Bom(int id)`) hat die `ProductionOrder`-Id und damit
Zugriff auf `SubOrderNumber`. Die Schnittstelle wird so erweitert, dass der hierarchische Pfad den
FA erhaelt — nicht nachtraeglich zurueckgerechnet.

### F6 — `OseonConnection` im Konstruktor: potenzieller Totalausfall

`BomRepository` holt im **Konstruktor** `configuration.GetConnectionString("OseonConnection")` und
wirft bei Fehlen eine `InvalidOperationException`. Auf einer IDEAL-Instanz **ohne OSEON** wuerde
damit **jede** Anfrage sterben, die `IBomRepository` aufloest — nicht nur der BOM-Knopf.

Am 2026-08-18 trat das nicht auf (der Fehler kam aus dem SQL, nicht aus dem Konstruktor), die
Verbindung ist also gesetzt. **Zu pruefen bleibt, worauf sie zeigt** — vermutlich auf die
AKE-OSEON-Instanz. Der hierarchische Pfad darf **keine** OSEON-Verbindung voraussetzen.

### F7 — Feld-Mapping: was die Struktur nicht hat

Die AKE-View liefert `Artikelnummer, Position, Baugruppe, Ressourcenummer, Bezeichnung1,
Bezeichnung2, Menge, Beschaffungsartikel, Artikelgruppe`. `FaHierarchyNode` fuehrt nicht alle
davon. Fehlende Felder duerfen **leer** bleiben — aber die View/Anzeige darf nicht stillschweigend
darauf bauen (Sortierung, Gruppierung, Filter auf einem leeren Feld ergibt eine leere Liste ohne
erkennbaren Grund).

**Vorgabe:** Feld-fuer-Feld-Mapping in der Spec dokumentieren; fuer jedes nicht befuellbare Feld
entscheiden, ob die Spalte im hierarchischen Modus ausgeblendet oder leer angezeigt wird.

### F8 — Parallelarbeit an Teil 8

Der Dev-Agent arbeitet zeitgleich im selben Worktree an Teil 8 (BDE). Beruehrungspunkte sind
gering (BOM vs. Rueckmeldung), aber vorhanden: `ProductionOrderRepository` steht in beiden
`affected_code`-Listen.

**Vorgabe:** Diese Spec wird **nicht parallel** zu Teil 8 umgesetzt, sondern danach — oder
abgestimmt in einer eigenen Etappe. Vor dem Start pruefen, welche Dateien der Teil-8-Lauf offen
hat.
