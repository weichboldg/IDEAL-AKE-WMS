---
type: spec
title: "IDEAL-Standort Teil 3 — Kommissionierlisten"
slug: 2026-07-29-standort-ideal-teil-3-spec
status: Entwurf
created: 2026-08-06
updated: 2026-08-06
source_backlog: "[[2026-07-29-Standort-IDEAL]]"
depends_on: "[[2026-07-29-standort-ideal-teil-1-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/FaHierarchyKommissionierListenController.cs (neu, Name provisorisch)
  - IdealAkeWms/Services/KommissionierListenService.cs (neu)
  - IdealAkeWms/Models/ViewModels/FaHierarchyKommissionierGruppeViewModel.cs (neu)
  - IdealAkeWms/Filters/RequireFaHierarchyKommissionierlistenAktivAttribute.cs (neu)
  - IdealAkeWms/Views/FaHierarchyKommissionierListen/Index.cshtml (neu)
  - IdealAkeWms/Views/FaHierarchyKommissionierListen/Print.cshtml (neu)
  - IdealAkeWms/Models/AppSettingKeys.cs
  - README.md (AppSettings-Dokumentation, neuer Toggle FaHierarchyKommissionierlistenAktiv)
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Doppelzaehlungs-Regel (SubFA = 0 als alleiniger Blatt-/Kommissionierfilter, Kommissionieren nur als Zusatzfilter) ist ab dieser Fassung der bindende Loesungsentwurf und die AK-Grundlage — Empfehlung, kein Dev-Blocker mehr (strukturell aus dem Anhang hergeleitet, nicht aus Testdaten). Offen bleibt die empirische Bestaetigung an echten IDEAL-Daten: das Testsystem ist leer, das beweist NICHT die Abwesenheit von Zeilen mit SubFA != 0 UND gesetztem Kommissionieren. Vorbedingung fuer Schranke 2 (Manual-UAT). Frage an den Menschen: teilt IDEAL diese Interpretation (kommissioniert wird ausschliesslich auf Blattebene), oder gibt es reale Faelle, in denen auf HauptFA-/Baugruppen-Ebene kommissioniert werden muss (z. B. fremdbezogene statt gefertigte Baugruppen)?"
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

Filterbare Kommissionierlisten je Kommissionier-Ziel (`Kommissionieren`) fuer die IDEAL-Linie —
das druckbare Arbeitsdokument fuer die Lagerentnahme, analog zur AKE-Kommissionierung, aber auf
Basis der IDEAL-Struktur (Teil 1) statt der AKE-BOM-Kette. Kernrisiko dieses Teils ist NICHT die
Anzeige selbst, sondern eine korrekte, nicht-verfaelschende Aggregation ueber eine mehrstufige
Struktur (Haupt-FA → Sub-FA → Blatt) hinweg — eine falsche Ebenen-Wahl wuerde Mengen doppelt oder
gar nicht ausweisen und damit die Lagerentnahme direkt sachlich falsch steuern.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** Liste/Druck aller `FaHierarchyNode`-**Blattpositionen** (`SubFA = 0`) mit gesetztem
`Kommissionieren`, gruppiert nach `HauptFA` (Node-Identitaet, **kein** Fan-out-Join gegen
`FaHierarchyOrderInfo`). Der Kopf einer Gruppe zeigt alle zugehoerigen `FaHierarchyOrderInfo`-Zeilen
(Montage-Abteilungen) als Information — Positionen werden dadurch **nicht** aufgesplittet. Barcode =
`HauptFA`, ein Ausdruck pro `HauptFA`-Gruppe.

**Out-of-Scope:** tatsaechliche Buchung/Transfer (dieser Teil druckt/listet, bucht aber nicht —
eine Buchungsfunktion setzt echte `ProductionOrders` voraus, also fruehestens nach Teil 7/8, falls
ueberhaupt gewuenscht — nicht Teil dieser Spec); `ProductionOrders`/AKE unveraendert; jeglicher
Scan-Workflow (Sub-FA-Scan → Aufloesung auf HauptFA, Werkstattdatenerfassung) — das gehoert zu
Teil 8 (BDE) und bleibt dort offen, siehe Fachliche Anforderungen Punkt 9.

## Fachliche Anforderungen

1. **Filter:** `Kommissionieren IS NOT NULL AND Kommissionieren <> ''`, zusaetzlich optionaler
   Filter auf einen konkreten Ziel-Wert (Dropdown der am Datenbestand vorkommenden Werte).

2. **Ebenen-/Doppelzaehlungsregel (verbindliche Design-Entscheidung — Verifikation an
   Echtdaten siehe Offene Rueckfrage 1).** Kommissioniert werden ausschliesslich Blattpositionen
   `SubFA = 0`. Eine Zeile mit `SubFA != 0` ist ein **Verweis** auf eine Baugruppe, die als eigener
   Sub-FA gefertigt wird — sie liegt nicht im Lager, sondern wird produziert; ihre Bestandteile
   fuehrt der zugehoerige Sub-FA in seinen **eigenen** Zeilen (dort mit `VaterFA` = dieser `SubFA`).
   Ein Blatt steht strukturell in genau einer Stueckliste, daher zaehlt diese Regel strukturell nie
   doppelt und nie null. `Kommissionieren` bleibt der **zusaetzliche** Filter (welches Ziel/welche
   Liste), **NICHT** die Ebenen-Trennung — Reihenfolge: erst `SubFA = 0`, dann `Kommissionieren`.
   `Beschaffungsartikel` wird **nicht** als weiterer Filter verwendet (auch Lagerartikel ohne
   Bestellbezug muessen kommissioniert werden).

3. **Anomalie-Diagnose statt stillem Verwerfen.** Trifft die Liste eine Zeile mit `SubFA != 0` UND
   gesetztem `Kommissionieren`, wird sie **nicht** kommissioniert, aber als Warnung protokolliert
   (`ILogger`/Serilog, `HauptFA` + `Position` + `Artnr`) — das macht eine Datenpflege-Abweichung
   sichtbar statt sie verschwinden zu lassen. Dies ist eine Web-seitige Lesefunktion (kein
   Hintergrund-Sync), daher `ILogger`-Warnung statt `SyncLog`-Eintrag (ADR 0010 gilt fuer
   Hintergrund-Services).

4. **Gruppierung ausschliesslich nach `HauptFA`** (Node-Identitaet). `MontageAbteilung` liegt
   gemaess der gemeinsamen Kern-Entscheidung (Teil 1) **nur** auf `FaHierarchyOrderInfo` und wird
   als **Kopf-Metadatum** je Gruppe angezeigt — bei mehreren `FaHierarchyOrderInfo`-Zeilen je
   `HauptFA` (Kombigeraete) werden **alle** im Kopf ausgewiesen, die Positionen werden **nicht**
   kuenstlich pro Montage-Abteilung gesplittet (das Modell liefert dafuer keinen Schluessel auf
   Positionsebene). Kopfdaten werden in einer **eigenen** Abfrage je Seiten-`HauptFA`-Menge geholt
   und im ViewModel zusammengefuehrt — **kein** `INNER JOIN ... ON HauptFA` gegen die
   Positionszeilen (das wuerde bei mehreren OrderInfo-Zeilen je `HauptFA` einen Fan-out erzeugen und
   jede Menge vervielfachen).

5. **Bekannte Einschraenkung (dokumentieren, auch im Druck):** Zwei Kombigeraete mit demselben
   `HauptFA` teilen sich denselben Barcode. Da pro `HauptFA`-Gruppe genau **ein** Ausdruck erzeugt
   wird (nicht einer je Montage-Abteilung, siehe Technischer Loesungsentwurf), gibt es **keine**
   Kollision zwischen zwei physischen Ausdrucken mit identischem Barcode — die Montage-Abteilung im
   Klartext-Kopf ist das einzige Unterscheidungsmerkmal fuer den Menschen. Fachliche Behandlung von
   Kombinationsgeraeten (echte Trennung) bleibt Backlog-Nachtrag, siehe Teil 1 (Fund B-1).

6. **Kopf aus `FaHierarchyOrderInfo`:** `ABNr`, `HauptFA`, `Kunde`, `MontageAbteilung`, Termine je
   nach Layout-Bedarf.

7. **Barcode = `HauptFA`** (einziger Produktions-Identifier laut Anhang; `SubFA` wird nie als
   Barcode verwendet).

8. **Zugriff:** `RequireLagerProcessingAccessAttribute` (Read, Class-Level, identisch zum
   bestehenden Kommissionier-Bereich `WarehousePickingController`) + neues Feature-Toggle
   `FaHierarchyKommissionierlistenAktiv` (`AppSettingKeys`, Default `false`) via neuen
   `RequireFaHierarchyKommissionierlistenAktivAttribute` (Muster 1:1 wie
   `RequireLagerbestellungAktivAttribute`, aber **invertierte** Default-Semantik: fehlend/nicht
   `"true"` ⇒ inaktiv — Default-aus-Konvention fuer neue Features, im Unterschied zu
   `LagerbestellungAktiv`, dessen Default-ein aus Bestandsschutz kommt). Keine neue Rolle.

9. **Geschwister-Materialfluss (Offene Frage 3 der Notiz — geklaert, Freigabe-Antwort 2, Runde 1):**
   Das Kennzeichen fuer hausintern gefertigte vs. zugekaufte Positionen ist das bereits vorhandene
   Feld `Artikeltyp` (Zukaufteil/Baugruppe/…) — kein zusaetzliches Artikelnummer-Matching noetig.
   Eine Reihenfolge-/Verfuegbarkeitslogik erzwingt das WMS **nicht**: die Reihenfolge ergibt sich
   aus der `SubFA`-Nummer bzw. aus den PPS-Informationen — reine Information, das PPS steuert.

10. **Sub-FA-Scan (Freigabe-Antwort 1, Runde 1: „wenn HauptFA → ganze Gruppe, wenn SubFA → nur
    Sub-FA") ist fuer Teil 3 gegenstandslos.** Teil 3 liefert nur Liste/Druck, keinen Scan-Workflow
    (Out-of-Scope). Die Antwort betrifft einen zukuenftigen BDE-Scan-Workflow und wandert
    unveraendert als Vorgabe zu **Teil 8** — sie ist dort weiterhin durch den Anhang-Fallstrick
    („`HauptFA` ist der einzige Produktions-Identifier, `SubFA` wird in Barcodes nicht verwendet")
    zu pruefen, bevor sie umgesetzt wird.

## Technischer Loesungsentwurf

`KommissionierListenService` liest ueber `IFaHierarchyNodeRepository`/`IFaHierarchyOrderInfoRepository`
(Teil 1) und baut die Liste in folgenden Schritten auf (GUI und Druck teilen sich dieselbe Pipeline
bis auf den letzten Schritt):

1. **Positionen laden und dedupliziert filtern:** alle `FaHierarchyNode`-Zeilen mit `SubFA = 0` UND
   `Kommissionieren <> ''` (optional zusaetzlich auf den gewaehlten Ziel-Wert eingeschraenkt). Zeilen
   mit `SubFA != 0` UND gesetztem `Kommissionieren` werden gesondert gesammelt und **nur** geloggt
   (Anforderung 3), nie in die Liste aufgenommen.
2. **Server-Side-Spaltenfilter** (ADR 0005) via `ColumnFilterHelper.ReadFromQuery` +
   `ColumnFilterHelper.Apply` auf den Positionszeilen; `ColumnMap` (Col-Key → gerenderter Zelltext)
   analog `WarehousePickingController.ColumnMap` fuer die Spalten `HauptFA`, `Artnr`, `Matchcode`,
   `Sollmenge`, `Hauptlagerplatz`, `Kommissionieren`, `Arbeitsbereich`, `Artikeltyp`, `Beschichtet`,
   `Material`. Filter wirken auf die Positionszeile, **vor** der Gruppierung.
3. **Gruppieren nach `HauptFA`.** Faellt eine Gruppe durch den Spaltenfilter auf 0 Positionen,
   erscheint sie **nicht** — kein Kopf ohne Zeilen.
4. **Pagination auf Gruppen-Ebene (ADR 0005, geschaerft fuer den Gruppen-Fall):** `PageSize.Resolve`
   + `PaginationState` + `_Pagination`-Partial wie ueberall, aber die **Seiteneinheit ist die Gruppe
   (`HauptFA`), nicht die Zeile** — konsistent zu Teil 2. Eine Gruppe wird **nie** ueber zwei Seiten
   getrennt; `TotalCount` zaehlt Gruppen, nicht Positionszeilen. Das ist der laut Ideen-Notiz
   riskanteste Paging-Fall dieser Liste — explizit so umzusetzen, nicht dem Dev-Lauf zu ueberlassen.
5. **Kopfdaten NACHTRAEGLICH je Seiten-`HauptFA`-Menge holen** (ein `GetByHauptFaListAsync`-Aufruf
   des `IFaHierarchyOrderInfoRepository` fuer genau die `HauptFA`-Werte der aktuellen Seite) und im
   ViewModel den passenden Gruppen zuordnen — **kein** Join gegen die Positionszeilen. Bei mehreren
   OrderInfo-Zeilen je `HauptFA` landen alle im Kopf-ViewModel dieser Gruppe.
6. **Filterkarte** (`<div class="card filter-card mb-3">`) mit dem Kommissionier-Ziel-Dropdown ueber
   dem Tabellenblock.
7. **Druck (`Print`-Action, ohne `id`-Parameter — bewusst anders als
   `WarehousePickingController.Print(int id)`, dessen Einzel-`id`-Muster hier nicht passt):**
   uebernimmt dieselben Query-Parameter wie die Bildschirmliste (inkl. `colf_*` und
   Ziel-Wert-Filter), fuehrt Schritte 1–3 **ohne** Pagination auf der **gesamten** gefilterten Menge
   aus und rendert **einen Ausdruck je `HauptFA`-Gruppe** mit CSS-Seitenumbruch
   (`page-break-after: always`) zwischen den Gruppen. Barcode = `HauptFA` je Gruppe, Kopf listet
   alle zugehoerigen `FaHierarchyOrderInfo`-Zeilen. Ist die gefilterte Menge leer, zeigt der Druck
   einen Hinweistext statt eines leeren Blatts (ebenso die Bildschirmliste bei 0 Treffern).

Kein zusaetzliches DTO-/Domain-Objekt jenseits eines schlanken Gruppen-ViewModels
(`FaHierarchyKommissionierGruppeViewModel`: `HauptFA`, `List<FaHierarchyOrderInfo>` Kopf,
`List<FaHierarchyNode>` gefilterte Positionen) noetig — die Repositories liefern bereits flache
EF-Entitaeten (Teil-1-Entscheidung).

## Migrations-/SQL-Auswirkungen

Keine — reine Lesefunktion. Der neue AppSetting-Key `FaHierarchyKommissionierlistenAktiv` braucht
keine Migration (generische Key-Value-Tabelle `AppSettings`, fehlender Key wird per Code-Default
`false` behandelt).

## Audit-Feld-Auswirkungen

Keine neuen Entitaeten. Falls eine spaetere Ausbaustufe eine Buchungsfunktion ergaenzt, ist das
gegen echte `ProductionOrders`/`StockMovement` zu bauen (Teil 7/8), nicht gegen die
Struktur-Cache-Tabellen.

## Akzeptanzkriterien

1. Liste zeigt ausschliesslich Blattpositionen (`SubFA = 0`) mit gesetztem `Kommissionieren`.
   Zeilen mit `SubFA != 0` erscheinen **nie** — code-pruefbar unabhaengig vom Testdatenbestand
   (synthetische Fixture: `HauptFA` mit einer Verweiszeile `SubFA != 0` **und** einem Sub-FA, der
   eigene Blattzeilen fuehrt).
2. **Keine Doppel- oder Nullzaehlung von Mengen** (unbedingtes Akzeptanzkriterium, nicht mehr
   konditional formuliert): jede kommissionierbare Position erscheint in **genau einer**
   Kommissionierliste.
3. **Anomalie-Diagnose:** Positionen mit `SubFA != 0` UND gesetztem `Kommissionieren` werden als
   Warnung protokolliert (`HauptFA` + `Position`), aber nicht kommissioniert.
4. Gruppierung erfolgt ausschliesslich nach `HauptFA`. Der Kopf einer Gruppe zeigt alle zugehoerigen
   `FaHierarchyOrderInfo`-Zeilen (Montage-Abteilungen); Positionen werden dadurch **nicht**
   gesplittet, kein Fan-out-Join, keine Mengenvervielfachung bei mehreren OrderInfo-Zeilen je
   `HauptFA`.
5. Die Liste erfuellt ADR 0005 vollstaendig: Pagination via `PageSize.Resolve`/`PaginationState`/
   `_Pagination`, Filterkarte, Server-Side-Spaltenfilter auf allen Tabellenspalten
   (`data-server-column-filter="true"`, `data-col-key` je `<th>`, `ColumnFilterHelper.ReadFromQuery`).
   **Seiteneinheit ist die Gruppe** (`HauptFA`); `TotalCount` zaehlt Gruppen; keine Gruppe wird ueber
   Seiten getrennt.
6. Spaltenfilter wirken auf Positionszeilen; faellt eine Gruppe dadurch auf 0 Positionen, verschwindet
   der Kopf vollstaendig (kein Kopf ohne Zeilen).
7. Druck spiegelt exakt die aktiv gefilterte Bildschirmliste (inkl. `colf_*`), mit Seitenumbruch je
   `HauptFA`-Gruppe — ein physischer Ausdruck pro `HauptFA`, Barcode = `HauptFA`, Kopf listet alle
   Montage-Abteilungs-Zeilen dieses `HauptFA` (verhindert die Barcode-Kollision zweier Kombigeraete
   mit gleichem `HauptFA`, siehe Fachliche Anforderungen Punkt 5).
8. Leere gefilterte Liste zeigt einen Hinweis (Bildschirm **und** Druck), kein leeres Blatt.
9. Zugriff nur mit `RequireLagerProcessingAccessAttribute` **und** aktivem Toggle
   `FaHierarchyKommissionierlistenAktiv` (Default `false`); ohne Toggle Redirect + `WarningMessage`,
   ohne Rolle Redirect auf `AccessDenied`.
10. AKE-Verhalten unveraendert (bestehende Controller/Views unberuehrt).

## Test-Szenarien

Neues Kapitel „IDEAL Teil 3 — Kommissionierlisten" in `docs/TESTSZENARIEN.md`, zweigeteilt:

**Automatisiert/InMemory-Fixture (unabhaengig vom IDEAL-Testsystem pruefbar):**
- `SubFA = 0`-Filter dedupliziert korrekt: synthetisches `HauptFA` mit einer `SubFA != 0`-Verweiszeile
  und einem Sub-FA mit eigenen Blattzeilen → Verweiszeile erscheint nicht, Sub-FA-Blaetter erscheinen
  je einmal (AK1/AK2).
- Anomalie-Zeile (`SubFA != 0` **und** `Kommissionieren` gesetzt) wird geloggt und ausgeschlossen (AK3).
- Gruppierung ohne Fan-out bei zwei `FaHierarchyOrderInfo`-Zeilen je `HauptFA`: Kopf zeigt beide,
  Positionsanzahl bleibt 1:1 zur Quellzeilenzahl (AK4).
- Pagination zaehlt Gruppen, keine Gruppe wird ueber zwei Seiten getrennt (AK5).
- Ein Spaltenfilter, der eine Gruppe komplett leert, laesst den Kopf verschwinden (AK6).
- Druck bei leerer gefilterter Menge zeigt Hinweistext, kein leeres Blatt (AK8).
- Zugriff ohne Rolle bzw. bei deaktiviertem Toggle → Redirect (AK9).

**Manuell am IDEAL-Testsystem (Vorbedingung: produktivnahe Daten, siehe Deploy-Abschnitt —
mit dem aktuell leeren Testsystem nicht durchfuehrbar):**
- Filter auf ein konkretes Kommissionier-Ziel liefert die erwartete Teilmenge.
- Ein bekanntes Kombinationsgeraet (zwei `MontageAbteilung`-Zeilen, gleicher `HauptFA`) erzeugt
  **einen** Ausdruck mit beiden Montage-Abteilungen im Kopf, keinen doppelten Barcode.
- **Mengenabgleich Struktur vs. Liste:** manuelle Gegenrechnung an einer bekannten mehrstufigen
  Struktur bestaetigt (oder widerlegt) die `SubFA = 0`-Regel empirisch — das ist die Verifikation
  aus Offene Rueckfrage 1, kein reiner Regressionstest.
- Druckvergleich Bildschirm vs. Papier bei aktiven Spaltenfiltern.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen.

## Deploy

- **Web-App:** ja.
- **Service:** nein.
- **Migration:** nein.
- **Voraussetzung/Reihenfolge:** Teil 1 (`FaHierarchyNode`/`FaHierarchyOrderInfo` + Sync) muss bereits
  gemergt und mit `Sync:HierarchicalFaEnabled = true` produktiv laufen, sonst ist die Liste dauerhaft
  leer (kein Fehler, aber fachlich nutzlos). Der neue Toggle `FaHierarchyKommissionierlistenAktiv`
  bleibt nach dem Deploy default **aus**.
- **Schranke-2-Vorbedingung (wichtig, siehe T3-H1 in der Kritischen Pruefung):** Manual-UAT der
  IDEAL-spezifischen Szenarien (Filter-Ziel, Kombigeraet-Kopf, Mengenabgleich, Druckvergleich) kann
  **erst** gruen werden, wenn produktivnahe IDEAL-Daten im Testsystem liegen — das aktuell leere
  Testsystem reicht nicht. Diese Abhaengigkeit ist dem qa-agent explizit als Vorbedingung zu melden,
  nicht erst beim Testversuch zu entdecken.
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  (provisorisch).

## Offene Rueckfragen

1. Doppelzaehlungs-Regel (`SubFA = 0` als alleiniger Blatt-/Kommissionierfilter, `Kommissionieren`
   nur als Zusatzfilter) ist ab dieser Fassung der bindende Loesungsentwurf und die AK-Grundlage
   (Fachliche Anforderungen Punkt 2/3, AK 1–3) — **Empfehlung**, kein Dev-Blocker mehr, weil
   strukturell aus dem Anhang hergeleitet und nicht von Testdaten abhaengig. Offen bleibt die
   **empirische Bestaetigung an echten IDEAL-Daten**: das Testsystem ist leer, das beweist **nicht**
   die Abwesenheit von Zeilen mit `SubFA != 0` UND gesetztem `Kommissionieren`. Vorbedingung fuer
   Schranke 2 (Manual-UAT, siehe Deploy-Abschnitt). Frage an den Menschen: Teilt IDEAL diese
   Interpretation (kommissioniert wird ausschliesslich auf Blattebene), oder gibt es reale Faelle,
   in denen auf HauptFA-/Baugruppen-Ebene kommissioniert werden muss (z. B. fremdbezogene statt
   gefertigte Baugruppen)?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. →

## Freigabe-Antworten — Runde 1 (2026-08-06, beantwortet, aus der Ideen-Notiz)

> Historischer Erst-Durchlauf vor der Kritischen Pruefung. Die Antworten sind in die Fachlichen
> Anforderungen (Punkte 8–10) eingearbeitet; Antwort 3/4 wurden durch die strukturelle
> `SubFA = 0`-Regel ersetzt (siehe Kritische Pruefung/Nachbesserung unten) — als unzureichend fuer
> einen Blocker-Abschluss erkannt („ich **denke**", „Testsystem ist leer, also nein" beweist keine
> Abwesenheit).

1. →wenn hauptfa -> ganze gruppe, wenn subfa nur sub-fa
2. →ist in der view | `Artikeltyp` | nvarchar | Position bevorzugt, Beleg als Fallback | Zukaufteil / Baugruppe / ... | -> reihenfolge ergibt sich aus SubFA nummer bzw. durch die PPS Informationen 
3. →ich denke pro SubFA, anders macht es keinen sinn.
4. →tesstsystem ist leer. also nein
5. →rollen wie gehabt

## Kritische Pruefung (2026-08-06)

Anwalt-des-Teufels-Durchsicht VOR Freigabe. Gegengelesen: diese Teil-3-Spec, die Teil-1-Spec
(Datenmodell `FaHierarchyNode`/`FaHierarchyOrderInfo`), die Ideen-Notiz inkl. „Kritische Pruefung"
(Offene Frage 4 / B2), der Anhang [[sage-views-ideal]], ADR 0005/0006 sowie der echte Referenzcode
(`ProductionOrdersController.Index`, `WarehousePickingController`/`WarehousePickingPrintLayout`).
Die Spec ist kompakt und gut disponiert — aber sie hat die Schranke-1-Antworten **nicht in Text,
AK und Loesungsentwurf eingearbeitet**, und dadurch stehen mehrere Kern-Entscheidungen weiter offen
bzw. widerspruechlich. Mehrere AK sind nicht implementierbar oder nicht pruefbar.

### BLOCKER

**T3-B1 — Offene Frage 4 (Doppelzaehlung) ist NICHT geloest, nur „beantwortet". Der teuerste Fehler
bleibt offen.**
Antwort 3 lautet „ich **denke** pro SubFA, anders macht es keinen Sinn" — eine Vermutung, kein
Beschluss, und **nirgends in eine konkrete Filter-/Aggregationsregel uebersetzt**. Der
„Technischer Loesungsentwurf" sagt weiter nur „wendet Filter/Gruppierung an", AK4 bleibt woertlich
konditional („sobald offene Rueckfrage 3 geklaert ist — bis dahin Blocker fuer den Dev-Lauf"). Damit
ginge ein Dev-Lauf mit **ungeloester** Ebenen-Frage los. Konkret ungeklaert: Der Haupt-FA fuehrt in
seiner Stueckliste die **Baugruppen** seiner Sub-FAs (Zeilen mit `SubFA != 0`), jeder Sub-FA fuehrt
**dieselben Teile** als seine Bestandteile. „Gruppiert nach HauptFA" + Filter `Kommissionieren` ueber
`FaHierarchyNode` zieht **beide Ebenen** in eine Liste → Aufsummieren zaehlt doppelt; filtert man zu
hart, zaehlt gar nicht. Es fehlt die harte Regel (z. B. „nur `SubFA = 0`-Blaetter kommissionieren"
oder „nur Positionen des jeweiligen Sub-FA, HauptFA-Verweiszeilen ausschliessen") — genau die, die
AK4 pruefbar machen wuerde. **Erst als konkrete Filterregel niederschreiben, dann AK4 unbedingt (nicht
konditional) formulieren.**

**T3-B2 — Vorpruefung „SubFA != 0 UND Kommissionieren?" wurde mit einem Nicht-Beweis abgetan.**
Antwort 4: „Testsystem ist leer. also nein." Ein **leeres** Testsystem beweist NICHT, dass es keine
Zeilen mit `SubFA != 0` UND gesetztem `Kommissionieren` gibt — es beweist nur, dass die Pruefung
**nicht durchgefuehrt werden konnte**. Genau diese Vorpruefung sollte entscheiden, ob `Kommissionieren`
allein die saubere Trennlinie ist (dann T3-B1 entschaerft) oder ob zusaetzlich ueber `SubFA = 0` /
`Beschaffungsartikel` gefiltert werden muss. Ergebnis: Die Design-Entscheidung aus T3-B1 ist
**empirisch nicht abgesichert**. Solange kein produktivnahes Datenset vorliegt, muss der
Doppelzaehl-Schutz **defensiv** ausgelegt werden (explizite Blatt-/Ebenen-Regel, nicht „ist eh leer").

**T3-B3 — AK2 „Gruppierung trennt Kombinationsgeraete nach Montage-Abteilung" ist mit dem Teil-1-
Datenmodell NICHT implementierbar — und erzeugt selbst eine zweite Doppelzaehlung.**
Teil-1 (Antwort A1) fuehrt `MontageAbteilung` **ausschliesslich auf `FaHierarchyOrderInfo`**
(auftragsbezogen) und **nicht** auf `FaHierarchyNode` (positionsbezogen) — bewusst, als reines
Info-Feld, kein Struktur-Schluessel. Die Kommissionier-Positionen kommen aber aus `FaHierarchyNode`.
Um sie „nach Montage-Abteilung zu trennen", muesste jede Node-Zeile einer der (bei Kombigeraeten
**mehreren**) FAInfos-Zeilen desselben `HauptFA` zugeordnet werden — **genau das kann die FAListe laut
Anhang (B3) nicht** („FAListe kann sie nicht unterscheiden"). Schlimmer: der vom Anhang empfohlene
`FaHierarchyNode INNER JOIN FaHierarchyOrderInfo ON HauptFA` **faechert** bei zwei FAInfos-Zeilen je
`HauptFA` **jede** Positionszeile auf 2 auf (kartesisches Produkt) → jede Menge doppelt. AK2 verlangt
also etwas, das das Modell nicht hergibt, und der naive Join produziert stille Mengen-Verdopplung.
**Vor Freigabe entscheiden:** entweder AK2 streichen/abschwaechen (Montage-Abteilung nur als Info-Kopf,
keine echte Trennung der Positionen), oder Teil 1 muss den Trennschluessel auf Positionsebene liefern
(was Teil 1 explizit auf Teil 7 verschoben hat — Konflikt).

**T3-B4 — Listen-View-Pattern (ADR 0005) fehlt vollstaendig — Verstoss gegen harte CLAUDE.md-Regel.**
Die Spec fordert an **keiner** Stelle Pagination (`PageSize.Resolve` + `PaginationState` +
`_Pagination`-Partial), Filterkarte oder **Server-Side-Spaltenfilter** (`data-server-column-filter`,
`data-col-key` je `<th>`, `ColumnFilterHelper.ReadFromQuery`, `ColumnMap` Col-Key→gerenderter Text).
CLAUDE.md: „Spaltenfilter fuer JEDE Tabelle", ADR 0005: „Pflicht fuer alle Tabellen-Views".
Referenz ist `ProductionOrdersController.Index`. Ohne explizite Forderung baut der Dev-Lauf eine
nicht-konforme Liste. **Zusatz-Fallstrick fuer diese Liste:** Sie ist **gruppiert** (nach HauptFA) —
Pagination „ueber Gruppen statt Zeilen" ist laut Ideen-Notiz der riskanteste Paging-Fall; das muss die
Spec adressieren (paginiert man Gruppen oder Zeilen? wie zaehlt `TotalCount`?), nicht offenlassen.

### SOLLTE

**T3-S1 — Print-Modell passt nicht 1:1 zum zitierten Muster.** `WarehousePickingPrintLayout` /
`WarehousePickingController.Print(int id)` druckt **genau eine** Anforderung per `id`. Der Anhang
verlangt „**je ein Ausdruck pro Auftrag** mit Barcode HauptFA" ueber die **gefilterte Menge** —
also einen Mehr-Gruppen-Druck, kein Einzel-`id`-Druck. Die Formulierung „analog
`WarehousePickingPrintLayout`-Muster" verdeckt diesen strukturellen Unterschied. Klaeren: druckt
Print die ganze gefilterte Liste (Seitenumbruch je HauptFA) oder ein HauptFA je Aufruf? Und wie
spiegelt der Druck die `colf_*`-Server-Spaltenfilter der Bildschirmliste (AK3)?

**T3-S2 — Access-Filter/Feature-Toggle nicht benannt.** Antwort 5 „rollen wie gehabt" ist zu vage.
Es existiert `RequireLagerProcessingAccessAttribute` (+ Read/Edit-Split) und das Toggle-Muster
`RequireLagerbestellungAktivAttribute`. Der Anhang schlaegt ein eigenes Feature-Toggle vor
(`FaHierarchyKommissionierlistenAktiv`, Default false). Die Spec listet `AppSettingKeys.cs` in
`affected_code`, benennt aber **weder** den konkreten Access-Filter **noch** den Toggle-Key **noch**
die Read/Edit-Ebene. Per ADR 0006 + CLAUDE.md (neue Rolle/Filter → 3 Stellen; neuer Toggle →
README/Settings) muss das konkret stehen.

**T3-S3 — Randfaelle unbehandelt.** (a) HauptFA-Gruppe, deren Positionen nach dem
`Kommissionieren`-Filter alle wegfallen → leere Gruppe darf nicht als Kopf ohne Zeilen erscheinen.
(b) Druck bei komplett leerer (gefilterter) Liste → definiertes Verhalten (Hinweis statt leeres
Blatt). (c) **Barcode-Kollision:** Barcode = `HauptFA`; zwei Kombigeraete teilen sich denselben
`HauptFA` → derselbe Barcode auf zwei fachlich verschiedenen Ausdrucken. Das ist die B2/B3-Kollision
im Druck; mindestens als bekannte Einschraenkung dokumentieren (Montage-Abteilung im Klartext-Kopf),
solange keine echte Trennung moeglich ist.

**T3-S4 — Antwort 1 widerspricht dem Anhang.** Antwort 1 „wenn subfa → nur sub-fa" setzt einen
**Sub-FA-Barcode/Scan** voraus. Der Anhang ist explizit: „`HauptFA` ist der EINZIGE
Produktions-Identifier; `SubFA` wird in Barcodes und Kommunikation **nicht** verwendet." Der
Sub-FA-Scan ist laut Ideen-Notiz (B2) eine **unbestaetigte Annahme** („wird beim ersten Test
geprueft") — und das Testsystem ist leer. Fuer Teil 3 (nur Liste/Druck, keine Buchung/Scan-Workflow
laut Out-of-Scope) ist die Frage ohnehin weitgehend gegenstandslos; dann sollte Rueckfrage 1 als
„fuer Teil 3 nicht relevant, gehoert zu Teil 8 (BDE)" markiert und nicht als geloest gefuehrt werden.

### HINWEIS

**T3-H1 — Testbarkeit/UAT blockiert.** Antwort 4 („Testsystem ist leer") bedeutet: **keine** der
IDEAL-spezifischen Test-Szenarien (Filter auf ein Ziel, Kombigeraet-Trennung, Mengenabgleich,
Druckvergleich) laesst sich manuell abnehmen. Damit kann Schranke 2 (Manual-UAT) faktisch nicht
gruen werden, bevor produktivnahe IDEAL-Daten vorliegen. Diese Vorbedingung (befuelltes Testsystem)
gehoert explizit in den Deploy-/Test-Abschnitt, sonst blockiert sie den qa-agent spaeter unbemerkt.

**T3-H2 — Abhaengigkeit.** `depends_on` Teil-1-Spec ist selbst noch `Entwurf` (nicht gemergt); ohne
den Teil-1-Sync gibt es keine Daten. Reihenfolge/Voraussetzung im Deploy-Abschnitt festhalten.

**T3-H3 — Spec-Hygiene.** Die Schranke-1-Antworten stehen unverarbeitet am Dateiende; „Offene
Rueckfragen" und AK4 tun weiter so, als sei nichts beantwortet. Nach Aufloesung von T3-B1..B4 die
Antworten in Loesungsentwurf + AK **einarbeiten** und die Rueckfragen-Liste auf den tatsaechlichen
Reststand kuerzen.

NACHBESSERUNG NOETIG: (1) Doppelzaehl-Schutz als konkrete Filter-/Ebenen-Regel niederschreiben und
AK4 unbedingt machen (T3-B1/T3-B2), (2) AK2 gegen das Teil-1-Modell aufloesen — Montage-Abteilung
trennt Positionen nicht, INNER-JOIN-Fan-out vermeiden (T3-B3), (3) ADR-0005-Listen-Pattern inkl.
Gruppen-Pagination und Server-Spaltenfilter explizit fordern (T3-B4), (4) Access-Filter + Feature-
Toggle konkret benennen (T3-S2). Erst danach freigeben.

### Nachbesserung (2026-08-06)

Status je Befund — Details in den Fachlichen Anforderungen, im Loesungsentwurf und in den
Akzeptanzkriterien oben, hier nur die Kurzfassung mit Verweis:

- **T3-B1 (Doppelzaehlung — keine konkrete Regel) — TEILWEISE BEHOBEN, Rest bleibt Rueckfrage.**
  Konkrete, unbedingte Regel niedergeschrieben: `SubFA = 0` ist der alleinige
  Kommissionier-Blattfilter, `Kommissionieren` nur Zusatzfilter (Fachliche Anforderungen Punkt 2).
  AK1/AK2 sind jetzt unbedingt formuliert und code-pruefbar (synthetische Fixture, keine echten
  IDEAL-Daten noetig). **Bleibt Rueckfrage:** die empirische Bestaetigung, dass diese Interpretation
  der realen IDEAL-Fertigungslogik entspricht — siehe Offene Rueckfrage 1. Das ist kein
  Design-Blocker mehr (die Regel ist strukturell, nicht aus „ist eh leer" abgeleitet), aber eine
  echte fachliche Unsicherheit, die nicht geraten werden darf.
- **T3-B2 (leeres Testsystem kein Beweis) — BEHOBEN.** Die Design-Entscheidung haengt nicht mehr an
  einer unbewiesenen Abwesenheit ab, sondern an der Struktur selbst (`SubFA`). Die
  Anomalie-Diagnose (Fachliche Anforderungen Punkt 3, AK3) faengt den Fall ab, dass die Annahme doch
  nicht zutrifft — die Abweichung wird sichtbar geloggt statt still falsch gezaehlt. Die empirische
  Pruefung wandert als Datenqualitaets-Kontrolle in die manuelle Test-Checkliste (Test-Szenarien,
  Abschnitt „Manuell am IDEAL-Testsystem").
- **T3-B3 (AK2 nicht implementierbar, Fan-out-Risiko) — BEHOBEN.** AK2 in der bisherigen Form
  („Gruppierung trennt Kombinationsgeraete nach Montage-Abteilung") ist gestrichen. Neu: Gruppierung
  ausschliesslich nach `HauptFA`, Montage-Abteilung nur als Kopf-Metadatum aus einer **eigenen**
  Abfrage je `HauptFA`-Menge (kein `INNER JOIN ... ON HauptFA` gegen Positionszeilen) — siehe
  Fachliche Anforderungen Punkt 4/5, Loesungsentwurf Schritt 5, AK4. Bekannte Einschraenkung
  (Barcode-Kollision bei zwei Kombigeraeten mit gleichem `HauptFA`) ist dokumentiert und durch das
  „ein Ausdruck je `HauptFA`-Gruppe"-Druckmodell strukturell entschaerft (kein doppelter Barcode auf
  zwei Papieren).
- **T3-B4 (ADR 0005 fehlt vollstaendig) — BEHOBEN.** Pagination, Filterkarte und Server-Spaltenfilter
  sind jetzt explizit gefordert (Fachliche Anforderungen Punkt 8 i. V. m. Loesungsentwurf
  Schritt 2/4/6, AK5/AK6). Die Gruppen-Pagination ist konkret spezifiziert: Seiteneinheit = Gruppe
  (`HauptFA`), `TotalCount` zaehlt Gruppen, keine Gruppe wird ueber Seiten getrennt, ein
  Spaltenfilter kann eine Gruppe vollstaendig zum Verschwinden bringen (kein Kopf ohne Zeilen).
- **T3-S1 (Print-Muster passt nicht) — BEHOBEN.** Klargestellt: kein Einzel-`id`-Druck wie
  `WarehousePickingController.Print(int id)`, sondern Druck der gesamten aktuell gefilterten Menge
  mit Seitenumbruch je `HauptFA`-Gruppe, dieselben `colf_*`-Parameter wie die Bildschirmliste
  (Loesungsentwurf Schritt 7, AK7).
- **T3-S2 (Access-Filter/Toggle nicht benannt) — BEHOBEN.** Konkret benannt:
  `RequireLagerProcessingAccessAttribute` (Read, Class-Level, aus `WarehousePickingController`
  uebernommen) + neues `RequireFaHierarchyKommissionierlistenAktivAttribute`
  (`FaHierarchyKommissionierlistenAktiv`, Default `false`) — Fachliche Anforderungen Punkt 8, AK9,
  `affected_code`.
- **T3-S3 (Randfaelle) — BEHOBEN.** (a) Leere Gruppe nach Spaltenfilter verschwindet vollstaendig
  (Loesungsentwurf Schritt 3, AK6). (b) Leere gefilterte Liste zeigt Hinweistext, kein leeres Blatt
  (Loesungsentwurf Schritt 7, AK8). (c) Barcode-Kollision durch „ein Ausdruck je `HauptFA`-Gruppe"
  strukturell vermieden und als bekannte Einschraenkung dokumentiert (Fachliche Anforderungen
  Punkt 5).
- **T3-S4 (Antwort 1 widerspricht Anhang) — BEHOBEN.** Rueckfrage 1 (Sub-FA-Scan) ist als fuer
  Teil 3 gegenstandslos markiert und nicht mehr als geloest gefuehrt; sie wandert unveraendert als
  Vorgabe zu Teil 8 (Fachliche Anforderungen Punkt 10).
- **T3-H1 (Testbarkeit/UAT blockiert) — BEHOBEN (als Vorbedingung dokumentiert, nicht aufgeloest).**
  Explizit im Deploy-Abschnitt als Schranke-2-Vorbedingung vermerkt; die automatisierbaren
  Struktur-Regeln sind im Test-Szenarien-Abschnitt von den manuellen Echtdaten-Szenarien getrennt,
  damit der qa-agent nicht unbemerkt daran haengen bleibt.
- **T3-H2 (Abhaengigkeit Teil 1) — BEHOBEN (dokumentiert, nicht aufloesbar durch diese Spec).**
  Bereits ueber `depends_on` im Frontmatter abgebildet, zusaetzlich im Deploy-Abschnitt als
  Reihenfolge-Voraussetzung benannt (Teil 1 muss produktiv laufen und Daten liefern).
- **T3-H3 (Spec-Hygiene) — BEHOBEN.** Die Runde-1-Antworten sind in Fachliche Anforderungen,
  Loesungsentwurf und Akzeptanzkriterien eingearbeitet (mit Verweis auf die jeweilige Antwort);
  „Offene Rueckfragen" ist auf den tatsaechlichen Reststand (1 Punkt) gekuerzt; die Runde-1-Antworten
  bleiben als historischer Beleg in einem eigenen Abschnitt erhalten, statt geloescht zu werden.
