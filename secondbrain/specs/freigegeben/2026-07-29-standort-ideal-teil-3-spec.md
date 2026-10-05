---
type: spec
title: "IDEAL-Standort Teil 3 — Kommissionierlisten"
slug: 2026-07-29-standort-ideal-teil-3-spec
status: Freigegeben
created: 2026-08-06
updated: 2026-08-13
source_backlog: "[[2026-07-29-Standort-IDEAL]]"
depends_on: "[[2026-07-29-standort-ideal-teil-1-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/FaHierarchyKommissionierListenController.cs (neu, Name provisorisch — zugleich Referenzimplementierung des gemeinsamen FaHierarchy-Listen-/Druck-Bausteins, den Teil 4/5 erweitern statt duplizieren)
  - IdealAkeWms/Services/KommissionierListenService.cs (neu — Filter-Flag, Header-Join ohne Fan-out, Gruppierung/Paging nach HauptFA und Druck-Scaffold bewusst als wiederverwendbarer Baustein geschnitten)
  - IdealAkeWms/Models/ViewModels/FaHierarchyKommissionierGruppeViewModel.cs (neu, inkl. Anomalie-Anzahl/-HauptFA-Liste fuer das Warnbanner)
  - IdealAkeWms/Filters/RequireFaHierarchyKommissionierlistenAktivAttribute.cs (neu)
  - IdealAkeWms/Views/FaHierarchyKommissionierListen/Index.cshtml (neu, inkl. Anomalie-Warnbanner)
  - IdealAkeWms/Views/FaHierarchyKommissionierListen/Print.cshtml (neu, inkl. Anomalie-Warnbanner im Ausdruck)
  - IdealAkeWms/Models/AppSettingKeys.cs
  - README.md (AppSettings-Dokumentation, neuer Toggle FaHierarchyKommissionierlistenAktiv)
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
  - IdealAkeWms/Services/KommissionierListenService.cs — Nachtrag 2026-08-13: `BuildAsync`/`GetTargetValuesAsync` ohne `SubFA`-Einschraenkung (`FaHierarchyListBuilder.Build` mit `leafOnly:false, anomalyOnNonLeaf:false`, wie Teil 4/Beschichtung); neue `BuildSummiertAsync`-Methode (Aggregation HauptFA/Artnr/Kommissionieren-Ziel, Sollmenge-Summe, KW-Filter auf `KO_Termin`)
  - IdealAkeWms/Controllers/FaHierarchyKommissionierListenController.cs — Nachtrag 2026-08-13: neue Action `Summiert(kwVon, kwBis, ...)`; Anomalie-Banner-Aufbau in `Index`/`Print` entfaellt
  - IdealAkeWms/Models/ViewModels/FaHierarchyKommissionierGruppeViewModel.cs — Nachtrag 2026-08-13: Anomalie-Anzahl/-HauptFA-Liste entfaellt (kein Banner mehr)
  - IdealAkeWms/Models/ViewModels/FaHierarchyKommissionierSummiertViewModel.cs (neu, Nachtrag 2026-08-13 — analog `FaHierarchyVormontageSummiertViewModel`, zusaetzlich KW-Filterfelder)
  - IdealAkeWms/Views/FaHierarchyKommissionierListen/Summiert.cshtml (neu, Nachtrag 2026-08-13)
  - IdealAkeWms/Views/FaHierarchyKommissionierListen/Index.cshtml — Nachtrag 2026-08-13: Anomalie-Warnbanner entfernen
  - IdealAkeWms/Views/FaHierarchyKommissionierListen/Print.cshtml — Nachtrag 2026-08-13: Anomalie-Warnbanner entfernen
open_questions:
  - "Arbeitsannahme (strukturell aus dem Datenmodell hergeleitet, kein dokumentierter Fakt): kommissioniert wird ausschliesslich auf Blattebene (SubFA = 0) — eine Zeile mit SubFA != 0 verweist auf eine Baugruppe, die in einem eigenen Sub-FA gefertigt wird und daher nicht aus dem Lager geholt wird. Verbleibende, einzige Rueckfrage: empirische Bestaetigung am IDEAL-Testsystem, ob wirklich ausschliesslich auf Blattebene kommissioniert wird (das aktuell leere Testsystem beweist das nicht) — Vorbedingung fuer Schranke 2. Absicherung bereits eingebaut: solange die Annahme nicht bestaetigt ist, wird eine SubFA != 0-Zeile mit gesetztem Kommissionieren nicht nur geloggt, sondern als operator-sichtbares Banner in Bildschirmliste UND Druck ausgewiesen (Fachliche Anforderungen Punkt 3, AK3). NACHTRAG 2026-08-13: durch den ersten Datenlauf (2026-08-12) und die UAT vom 2026-08-13 empirisch widerlegt und geschlossen — siehe Abschnitt „Nachtrag / Aenderung aus UAT (2026-08-13)\" am Dateiende."
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-08-07
---

## Ziel / Nutzen (das Warum)

Filterbare Kommissionierlisten je Kommissionier-Ziel (`Kommissionieren`) fuer die IDEAL-Linie —
das druckbare Arbeitsdokument fuer die Lagerentnahme, analog zur AKE-Kommissionierung, aber auf
Basis der IDEAL-Struktur (Teil 1) statt der AKE-BOM-Kette. Kernrisiko dieses Teils ist NICHT die
Anzeige selbst, sondern eine korrekte, nicht-verfaelschende Aggregation ueber eine mehrstufige
Struktur (Haupt-FA → Sub-FA → Blatt) hinweg — eine falsche Ebenen-Wahl wuerde Mengen doppelt oder
gar nicht ausweisen und damit die Lagerentnahme direkt sachlich falsch steuern.

Teil 3 wird zusaetzlich als **Referenzimplementierung eines wiederverwendbaren FaHierarchy-Listen-/
Druck-Bausteins** geschnitten: Teil 4 (Beschichtungsauftrag) und Teil 5 (Vormontage-Listen) machen
strukturell dasselbe (Flag-Filter auf `FaHierarchyNode`, Header-Join gegen `FaHierarchyOrderInfo`
ohne Fan-out, Gruppierung/Paging nach `HauptFA`, Druck-Scaffold) und erweitern laut Uebersicht
(„Ergaenzende Querschnitts-Entscheidungen") diesen Baustein, statt ihn zu duplizieren — `depends_on`
beider Teile zeigt bereits auf diese Spec. Diese Spec liefert damit nicht nur die Kommissionierliste
selbst, sondern den gemeinsamen Bauplan fuer alle drei Teile.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** Liste/Druck aller `FaHierarchyNode`-**Blattpositionen** (`SubFA = 0`) mit gesetztem
`Kommissionieren`, gruppiert nach `HauptFA` (Node-Identitaet, **kein** Fan-out-Join gegen
`FaHierarchyOrderInfo`). Der Kopf einer Gruppe zeigt alle zugehoerigen `FaHierarchyOrderInfo`-Zeilen
(Montage-Abteilungen) als Information — Positionen werden dadurch **nicht** aufgesplittet. Barcode =
`HauptFA`, ein Ausdruck pro `HauptFA`-Gruppe. Der Service ist bewusst so geschnitten, dass
Filter-Flag (`Kommissionieren`), Header-Join (`HauptFA` → `FaHierarchyOrderInfo`, kein Fan-out),
Gruppierung/Paging (Seiteneinheit = Gruppe `HauptFA`) und Druck-Scaffold (ein Ausdruck je
`HauptFA`-Gruppe, Seitenumbruch, Leerfall-Hinweis) als wiederverwendbarer Baustein fungieren, den
Teil 4 (`Beschichtet`) und Teil 5 (`VMBedarf`) mit ihrem jeweiligen Flag erweitern, statt die
Mechanik erneut zu bauen.

**Out-of-Scope:** tatsaechliche Buchung/Transfer (dieser Teil druckt/listet, bucht aber nicht —
eine Buchungsfunktion setzt echte `ProductionOrders` voraus, also fruehestens nach Teil 7/8, falls
ueberhaupt gewuenscht — nicht Teil dieser Spec); `ProductionOrders`/AKE unveraendert; jeglicher
Scan-Workflow (Sub-FA-Scan → Aufloesung auf HauptFA, Werkstattdatenerfassung) — das gehoert zu
Teil 8 (BDE) und bleibt dort offen, siehe Fachliche Anforderungen Punkt 9.

## Fachliche Anforderungen

1. **Filter:** `Kommissionieren IS NOT NULL AND Kommissionieren <> ''`, zusaetzlich optionaler
   Filter auf einen konkreten Ziel-Wert (Dropdown der am Datenbestand vorkommenden Werte).

2. **[UEBERHOLT seit Nachtrag 2026-08-13 — siehe „Nachtrag / Aenderung aus UAT (2026-08-13)" am
   Dateiende: der `SubFA = 0`-Filter ist ersatzlos entfernt. Dieser Punkt bleibt als historischer
   Beleg der urspruenglichen Herleitung stehen, ist aber nicht mehr verbindlich.]**
   **Ebenen-/Doppelzaehlungsregel — Arbeitsannahme, strukturell aus dem Datenmodell hergeleitet,
   kein dokumentierter Fakt (verbleibende Verifikation siehe Offene Rueckfrage 1).** Kommissioniert
   werden ausschliesslich Blattpositionen `SubFA = 0`. Eine Zeile mit `SubFA != 0` ist ein
   **Verweis** auf eine Baugruppe, die in einem **eigenen** Sub-FA gefertigt wird — sie liegt nicht
   im Lager, sondern wird produziert, und wird deshalb nicht kommissioniert; ihre Bestandteile fuehrt
   der zugehoerige Sub-FA in seinen **eigenen** Zeilen (dort mit `VaterFA` = dieser `SubFA`). Ein
   Blatt steht strukturell in genau einer Stueckliste, daher zaehlt diese Regel strukturell nie
   doppelt und nie null — **unter der Voraussetzung, dass die Annahme zutrifft**. `Kommissionieren`
   bleibt der **zusaetzliche** Filter (welches Ziel/welche Liste), **NICHT** die Ebenen-Trennung —
   Reihenfolge: erst `SubFA = 0`, dann `Kommissionieren`. `Beschaffungsartikel` wird **nicht** als
   weiterer Filter verwendet (auch Lagerartikel ohne Bestellbezug muessen kommissioniert werden).

3. **[UEBERHOLT seit Nachtrag 2026-08-13 — siehe „Nachtrag / Aenderung aus UAT (2026-08-13)" am
   Dateiende: das Anomalie-Banner entfaellt vollstaendig, es gibt keinen Ausnahmefall mehr. Dieser
   Punkt bleibt als historischer Beleg stehen (er erklaert, warum das Banner ueberhaupt gebaut
   wurde, und hat sich in der Praxis bewaehrt), ist aber nicht mehr verbindlich.]**
   **Anomalie-Diagnose — geloggt UND operator-sichtbar (PFLICHT, solange Punkt 2 Arbeitsannahme
   bleibt).** Trifft die Liste eine Zeile mit `SubFA != 0` UND gesetztem `Kommissionieren`, wird sie
   **nicht** kommissioniert — die Abweichung darf aber nicht nur im Serverlog verschwinden:
   1. **Server-Log:** `ILogger`/Serilog-Warnung mit `HauptFA` + `Position` + `Artnr` (Web-seitige
      Lesefunktion, kein Hintergrund-Sync, daher `ILogger` statt `SyncLog` — ADR 0010 gilt fuer
      Hintergrund-Services), je Lauf dedupliziert/gedrosselt (eine Warnung je `HauptFA` und
      Request statt einer Zeile je betroffener Position — sonst Log-Rauschen bei wiederholtem
      Blaettern/Filtern derselben Anomalien).
   2. **Operator-sichtbares Banner in Bildschirmliste UND Druck** (zwingend, nicht optional): Anzahl
      und `HauptFA`-Liste der ausgeschlossenen Positionen, z. B. „Warnung: N Position(en) mit
      gesetztem `Kommissionieren` auf Baugruppen-Ebene (`SubFA != 0`) ausgeschlossen — Datenpflege
      pruefen". Solange die Blattebene-Annahme aus Punkt 2 nicht empirisch bestaetigt ist, darf eine
      betroffene, eigentlich kommissionierbare Position nicht spurlos aus der operativen Sicht des
      Kommissionierers verschwinden — genau das waere der in „Ziel/Nutzen" benannte teuerste Fehler
      („Mengen ... gar nicht ausweisen"). Kein Banner ohne Anomalien; bei 0 Anomalien bleibt es
      unsichtbar.

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
   Klartext-Kopf ist das einzige Unterscheidungsmerkmal fuer den Menschen. Der Anhang verlangt
   woertlich „je ein Ausdruck **pro Auftrag** mit Barcode `HauptFA`"; diese Spec weicht davon bewusst
   ab („ein Ausdruck je `HauptFA`-**Gruppe**", also ggf. mehrere Auftraege/Montage-Abteilungen auf
   einem Blatt) — konsistent zur Entscheidung „Kombinationsgeraete sind out of scope, keine Trennung
   nach `MontageAbteilung`" (Teil 1, Uebersicht). Fachliche Behandlung von Kombinationsgeraeten
   (echte Trennung) bleibt Backlog-Nachtrag [[2026-08-06-kombinationsgeraete-montageabteilung]].

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

11. **Referenz-/Baustein-Rolle fuer Teil 4/5 (Uebersicht, „Ergaenzende Querschnitts-
    Entscheidungen").** `KommissionierListenService`/-Controller sind bewusst als wiederverwendbarer
    Schnitt zu bauen: Flag-Filter (`Kommissionieren`) als Parameter statt hart codiert, gemeinsamer
    Header-Join ohne Fan-out, gemeinsame Gruppierung/Paging-Logik nach `HauptFA`, gemeinsames
    Druck-Scaffold (ein Ausdruck je `HauptFA`-Gruppe, Seitenumbruch, Leerfall-Hinweis, Anomalie-
    Banner-Slot). Teil 4 (`Beschichtet`) und Teil 5 (`VMBedarf`) erweitern diesen Baustein, statt ihn
    zu duplizieren (`depends_on` beider Teile zeigt bereits auf diese Spec). Diese Spec liefert damit
    den Bauplan, nicht nur die Kommissionierliste selbst.

## Technischer Loesungsentwurf

`KommissionierListenService` liest ueber `IFaHierarchyNodeRepository`/`IFaHierarchyOrderInfoRepository`
(Teil 1) und baut die Liste in folgenden Schritten auf (GUI und Druck teilen sich dieselbe Pipeline
bis auf den letzten Schritt). Der Service ist bewusst generisch geschnitten (Flag-Spalte, ColumnMap,
Druck-Scaffold als Parameter/Vorlage statt hart codiert), damit Teil 4 und Teil 5 ihn mit ihrem
jeweiligen Flag erweitern koennen, ohne die Mechanik zu duplizieren (Fachliche Anforderungen
Punkt 11):

1. **Positionen laden und dedupliziert filtern:** alle `FaHierarchyNode`-Zeilen mit `SubFA = 0` UND
   `Kommissionieren <> ''` (optional zusaetzlich auf den gewaehlten Ziel-Wert eingeschraenkt). Zeilen
   mit `SubFA != 0` UND gesetztem `Kommissionieren` werden gesondert gesammelt: **ILogger-Warnung**
   (Anforderung 3.1, je `HauptFA`/Request dedupliziert) **und** eine kleine Anomalie-Liste
   (`HauptFA` + Anzahl), die GUI und Druck fuer das Banner (Anforderung 3.2) verwenden — nie in die
   Kommissionierliste selbst aufgenommen.
2. **Server-Side-Spaltenfilter** (ADR 0005) via `ColumnFilterHelper.ReadFromQuery` +
   `ColumnFilterHelper.Apply` auf den Positionszeilen; `ColumnMap` (Col-Key → gerenderter Zelltext)
   analog `WarehousePickingController.ColumnMap` fuer die Spalten `HauptFA`, `HauptArtnr`, `Artnr`,
   `Matchcode`, `Sollmenge`, `Hauptlagerplatz`, `Kommissionieren`, `Arbeitsbereich`, `Artikeltyp`,
   `Beschichtet`, `Material` (`HauptArtnr` gemaess Anhang-Spaltenliste ergaenzt, konsistent zur
   Positionstabelle in Teil 4). Filter wirken auf die Positionszeile, **vor** der Gruppierung.
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
   dem Tabellenblock. **Direkt darunter** erscheint bei vorhandenen Anomalien das Warnbanner aus
   Anforderung 3.2 (kein Banner bei 0 Anomalien).
7. **Druck (`Print`-Action, ohne `id`-Parameter — bewusst anders als
   `WarehousePickingController.Print(int id)`, dessen Einzel-`id`-Muster hier nicht passt):**
   uebernimmt dieselben Query-Parameter wie die Bildschirmliste (inkl. `colf_*` und
   Ziel-Wert-Filter), fuehrt Schritte 1–3 **ohne** Pagination auf der **gesamten** gefilterten Menge
   aus und rendert **einen Ausdruck je `HauptFA`-Gruppe** mit CSS-Seitenumbruch
   (`page-break-after: always`) zwischen den Gruppen. Barcode = `HauptFA` je Gruppe, Kopf listet
   alle zugehoerigen `FaHierarchyOrderInfo`-Zeilen. Enthaelt die gedruckte Menge Anomalie-Zeilen
   (Anforderung 3.2), erscheint das Warnbanner auch **auf dem Ausdruck** (z. B. als Kopfzeile vor der
   ersten Gruppe), nicht nur am Bildschirm. Ist die gefilterte Menge leer, zeigt der Druck einen
   Hinweistext statt eines leeren Blatts (ebenso die Bildschirmliste bei 0 Treffern).

Kein zusaetzliches DTO-/Domain-Objekt jenseits eines schlanken Gruppen-ViewModels
(`FaHierarchyKommissionierGruppeViewModel`: `HauptFA`, `List<FaHierarchyOrderInfo>` Kopf,
`List<FaHierarchyNode>` gefilterte Positionen) noetig — die Repositories liefern bereits flache
EF-Entitaeten (Teil-1-Entscheidung). Zusaetzlich ein schlankes View-seitiges Aggregat fuer die
Anomalie-Anzahl/-`HauptFA`-Liste (kein neues Domain-Objekt) — GUI und Druck lesen daraus dieselbe
Information fuer das Banner.

> **Hinweis (Nachtrag 2026-08-13):** Schritte 1, 6 und 7 dieses Loesungsentwurfs beschreiben den
> `SubFA = 0`-Filter und das Anomalie-Banner — beides ist seit dem Nachtrag am Dateiende ersatzlos
> entfernt (`FaHierarchyListBuilder.Build` wird mit `leafOnly:false, anomalyOnNonLeaf:false`
> aufgerufen, wie bereits bei Teil 4/Beschichtung). Der Abschnitt bleibt als historischer Beleg der
> urspruenglichen Baustein-Nutzung stehen; verbindlich ist der Nachtrag.

## Migrations-/SQL-Auswirkungen

Keine — reine Lesefunktion. Der neue AppSetting-Key `FaHierarchyKommissionierlistenAktiv` braucht
keine Migration (generische Key-Value-Tabelle `AppSettings`, fehlender Key wird per Code-Default
`false` behandelt). Die Summiert-Ansicht (Nachtrag 2026-08-13) aggregiert rein in C# ueber bereits
geladene `FaHierarchyNode`/`FaHierarchyOrderInfo`-Daten — ebenfalls keine Migration.

## Audit-Feld-Auswirkungen

Keine neuen Entitaeten. Falls eine spaetere Ausbaustufe eine Buchungsfunktion ergaenzt, ist das
gegen echte `ProductionOrders`/`StockMovement` zu bauen (Teil 7/8), nicht gegen die
Struktur-Cache-Tabellen.

## Akzeptanzkriterien

1. Liste zeigt ausschliesslich Blattpositionen (`SubFA = 0`) mit gesetztem `Kommissionieren`.
   Zeilen mit `SubFA != 0` erscheinen **nie** — code-pruefbar unabhaengig vom Testdatenbestand
   (synthetische Fixture: `HauptFA` mit einer Verweiszeile `SubFA != 0` **und** einem Sub-FA, der
   eigene Blattzeilen fuehrt).
2. **Keine Doppel- oder Nullzaehlung von Mengen — gekoppelt an die Arbeitsannahme aus Fachlicher
   Anforderung 2, mit eingebauter Absicherung.** Unter der Annahme, dass `Kommissionieren`
   ausschliesslich auf Blattpositionen (`SubFA = 0`) gesetzt wird, erscheint jede kommissionierbare
   Position in **genau einer** Kommissionierliste (code-pruefbar per synthetischer Fixture, siehe
   AK1). Trifft die Annahme in der Praxis nicht zu (`SubFA != 0` UND `Kommissionieren` gesetzt), wird
   diese Position in **keiner** Liste gefuehrt — dieser Fall ist dann aber NICHT still: er wird
   zwingend geloggt und als Banner ausgewiesen (AK3). Das Risiko „still nicht gezaehlt" ist damit
   durch Sichtbarkeit abgesichert, nicht durch die (noch unbestaetigte) Annahme selbst.
3. **Anomalie-Diagnose ist ein unbedingtes, zweikanaliges Akzeptanzkriterium.** Positionen mit
   `SubFA != 0` UND gesetztem `Kommissionieren` werden (a) als `ILogger`-Warnung protokolliert
   (`HauptFA` + `Position` + `Artnr`) **und** (b) als sichtbares Banner sowohl in der Bildschirmliste
   als auch im Druck angezeigt (Anzahl + betroffene `HauptFA`), sobald mindestens eine Anomalie in
   der aktuell gefilterten Menge vorkommt — code-pruefbar ueber dieselbe synthetische Fixture wie
   AK1 (Banner erscheint bei vorhandener Anomalie-Zeile, bleibt bei 0 Anomalien unsichtbar).
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
   mit gleichem `HauptFA`, siehe Fachliche Anforderungen Punkt 5). Enthaelt die gedruckte Menge
   Anomalie-Zeilen (AK3), erscheint das Warnbanner auch auf dem Ausdruck, nicht nur am Bildschirm.
8. Leere gefilterte Liste zeigt einen Hinweis (Bildschirm **und** Druck), kein leeres Blatt.
9. Zugriff nur mit `RequireLagerProcessingAccessAttribute` **und** aktivem Toggle
   `FaHierarchyKommissionierlistenAktiv` (Default `false`); ohne Toggle Redirect + `WarningMessage`,
   ohne Rolle Redirect auf `AccessDenied`.
10. AKE-Verhalten unveraendert (bestehende Controller/Views unberuehrt).
11. Filter-Flag, Header-Join, Gruppierung/Paging und Druck-Scaffold sind als Parameter/
    wiederverwendbare Methoden geschnitten (nicht hart auf `Kommissionieren` verdrahtet) —
    nachweisbar durch die Service-/Controller-Signatur (Flag als Parameter statt Konstante) und
    durch tatsaechliche Wiederverwendung, sobald Teil 4/5 umgesetzt werden.

> **Hinweis (Nachtrag 2026-08-13):** AK1, AK2 (Teil „genau eine Liste") und AK3 (Anomalie-Banner)
> gelten mit dem `SubFA = 0`-Filter bzw. dem Banner, die beide entfernt wurden — sie sind durch die
> neuen AK im Nachtrag am Dateiende ersetzt. AK4–AK11 bleiben unveraendert gueltig.

## Test-Szenarien

Neues Kapitel „IDEAL Teil 3 — Kommissionierlisten" in `docs/TESTSZENARIEN.md`, zweigeteilt:

**Automatisiert/InMemory-Fixture (unabhaengig vom IDEAL-Testsystem pruefbar):**
- `SubFA = 0`-Filter dedupliziert korrekt: synthetisches `HauptFA` mit einer `SubFA != 0`-Verweiszeile
  und einem Sub-FA mit eigenen Blattzeilen → Verweiszeile erscheint nicht, Sub-FA-Blaetter erscheinen
  je einmal (AK1/AK2).
- Anomalie-Zeile (`SubFA != 0` **und** `Kommissionieren` gesetzt) wird geloggt, aus der Liste
  ausgeschlossen und per Banner (Bildschirm **und** Druck) sichtbar gemacht (AK3).
- Anomalie-Banner erscheint in Bildschirmliste UND Druck, sobald die Fixture mindestens eine
  Anomalie-Zeile enthaelt; bleibt bei 0 Anomalien unsichtbar (AK3).
- Gruppierung ohne Fan-out bei zwei `FaHierarchyOrderInfo`-Zeilen je `HauptFA`: Kopf zeigt beide,
  Positionsanzahl bleibt 1:1 zur Quellzeilenzahl (AK4).
- Pagination zaehlt Gruppen, keine Gruppe wird ueber zwei Seiten getrennt (AK5).
- Ein Spaltenfilter, der eine Gruppe komplett leert, laesst den Kopf verschwinden (AK6).
- Druck bei leerer gefilterter Menge zeigt Hinweistext, kein leeres Blatt (AK8).
- Zugriff ohne Rolle bzw. bei deaktiviertem Toggle → Redirect (AK9).

> **Hinweis (Nachtrag 2026-08-13):** Die ersten drei Punkte (SubFA-Dedup, Anomalie-Log, Anomalie-
> Banner) sind durch den Nachtrag hinfaellig und werden durch die dortigen Testfaelle ersetzt.

**Manuell am IDEAL-Testsystem (Vorbedingung: produktivnahe Daten, siehe Deploy-Abschnitt —
mit dem aktuell leeren Testsystem nicht durchfuehrbar):**
- Filter auf ein konkretes Kommissionier-Ziel liefert die erwartete Teilmenge.
- Ein bekanntes Kombinationsgeraet (zwei `MontageAbteilung`-Zeilen, gleicher `HauptFA`) erzeugt
  **einen** Ausdruck mit beiden Montage-Abteilungen im Kopf, keinen doppelten Barcode.
- **Mengenabgleich Struktur vs. Liste:** manuelle Gegenrechnung an einer bekannten mehrstufigen
  Struktur bestaetigt (oder widerlegt) die `SubFA = 0`-Arbeitsannahme empirisch — das ist die
  Verifikation aus Offener Rueckfrage 1, kein reiner Regressionstest.
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

1. **Arbeitsannahme (nicht dokumentierter Fakt, strukturell aus dem Datenmodell hergeleitet):**
   Kommissioniert wird ausschliesslich auf Blattebene (`SubFA = 0`). Begruendung: eine Zeile mit
   `SubFA != 0` verweist auf eine Baugruppe, die in einem eigenen Sub-FA gefertigt wird — die holt
   man nicht aus dem Lager, sondern produziert sie. Der Anhang (Abschnitt C) stuetzt diese Annahme
   nicht explizit (er filtert dort nur nach `Kommissionieren`, ohne `SubFA` zu erwaehnen) — sie ist
   eine Herleitung dieser Spec-Runde, kein dokumentierter Fakt. **Verbleibende Rueckfrage:**
   empirische Bestaetigung am IDEAL-Testsystem, ob wirklich ausschliesslich auf Blattebene
   kommissioniert wird, oder ob reale Faelle existieren, in denen auf Baugruppen-/HauptFA-Ebene
   kommissioniert werden muss (z. B. fremdbezogene statt gefertigte Baugruppen). Das aktuell leere
   Testsystem beweist die Abwesenheit solcher Faelle **nicht** — Vorbedingung fuer Schranke 2
   (Manual-UAT, siehe Deploy-Abschnitt). **Absicherung bereits in dieser Fassung eingebaut:** Solange
   diese Annahme nicht bestaetigt ist, wird eine `SubFA != 0`-Zeile mit gesetztem `Kommissionieren`
   nicht nur geloggt, sondern als operator-sichtbares Banner in Bildschirmliste UND Druck ausgewiesen
   (Fachliche Anforderungen Punkt 3, AK3) — falls die Annahme falsch ist, faellt das auf, statt still
   Mengen verschwinden zu lassen.
   **BEANTWORTET UND GESCHLOSSEN (2026-08-12/2026-08-13):** siehe „ENTSCHEIDUNG NACH ERSTEM
   DATENLAUF (2026-08-12)" und „Nachtrag / Aenderung aus UAT (2026-08-13)" am Dateiende — die
   Annahme war falsch, das Pflicht-Banner hat den Fall wie vorgesehen sichtbar gemacht.

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

## Kritische Pruefung (2026-08-07)

Zweiter Anwalt-des-Teufels-Durchgang, ausdruecklich mit dem Auftrag, die **seit der Nachbesserung
erweiterten menschlichen Antworten** zu pruefen. Gegengelesen: diese Teil-3-Spec **komplett**, die
Teil-1-Spec (`FaHierarchyNode` 1:1 Positionen, `SubFA = 0` = Blatt / `SubFA <> 0` = eigener FA,
Wurzel `SubFA = HauptFA`, `MontageAbteilung` **nur** auf `FaHierarchyOrderInfo`, Existenzpruefung
statt Fan-out-Join), die ueberarbeiteten Teil-4- und Teil-5-Specs (gemeinsamer Listen-/Druck-Baustein,
`SubFA = 0`-Regel, Kombigeraete out of scope), die Uebersicht (Querschnitts-Entscheidungen:
„Teil 3 = Referenzimplementierung", „Seiteneinheit durchgaengig `HauptFA`", Listen-Toggles in
AppSettings), der Anhang [[sage-views-ideal]] (Barcode = `HauptFA`, `SubFA = 0` = Blatt,
Kommissionierlisten-Spaltenliste, „ein Ausdruck **pro Auftrag**"), ADR 0005/0006 sowie **echter
main-Code**: `RequireLagerProcessingAccessAttribute` (existiert, prueft `CanProcessLagerAsync`),
`WarehousePickingController` (traegt `[RequireLagerProcessingAccess]` Class-Level — die Spec-Angabe
„identisch zum bestehenden Kommissionier-Bereich" stimmt), `RequireLagerbestellungAktivAttribute`
(existiert, Default-**ein**-Semantik `!= "false"` — die Teil-3-Vorgabe „invertierte Default-aus"
ist korrekt beschrieben).

**Was haelt (bewusst bestaetigt, damit klar ist, was NICHT neu aufgerollt wird):**
- **Doppelzaehlung (ehem. Offene Frage 4) ist als ENTSCHIEDENE, code-pruefbare Regel geloest.**
  `SubFA = 0` ist der alleinige Blatt-/Kommissionierfilter, `Kommissionieren` nur Zusatzfilter
  (Fachliche Anforderung 2, verbindliche Design-Entscheidung). AK1/AK2 sind unbedingt formuliert und
  ueber eine **synthetische Fixture unabhaengig vom Testdatenbestand** pruefbar (Test-Szenarien,
  Abschnitt „Automatisiert/InMemory"). Da die Node-/OrderInfo-Tabellen lokale EF-Tabellen sind
  (Teil 1), ist der `SubFA = 0`-Filter als LINQ EF-InMemory-testbar — die Fixture ist realisierbar.
  Der frueherer BLOCKER wird daher **nicht** neu aufgerollt.
- **Gruppierung nur nach `HauptFA`, kein Fan-out-Join, `MontageAbteilung` nur Kopf** — konsistent zu
  Teil 1 und zur Uebersicht. Barcode-Kollision durch „ein Ausdruck je `HauptFA`-Gruppe" strukturell
  entschaerft. ADR 0005 vollstaendig (Pagination/Filterkarte/Server-Spaltenfilter + Gruppen-Paging,
  Seiteneinheit `HauptFA`, `TotalCount` zaehlt Gruppen).

### BLOCKER

**T3-2P-B1 — Die im Auftrag angenommenen „erweiterten Antworten" existieren NICHT; die einzige
fachliche Kernfrage (Offene Rueckfrage 1) ist unbeantwortet.**
Die Sektion „## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)" enthaelt weiterhin nur
`1. →` (leer); `freigabe_entscheidung`/`freigabe_von`/`freigabe_am` im Frontmatter sind leer.
`git log`/`git diff` belegen: die Datei ist seit dem Nachbesserungs-Commit (`ddcae5f`)
**unveraendert** — der Mensch hat seit Runde 1 **keine** neuen Antworten ergaenzt. Damit ist die
Praemisse dieses Durchgangs („pruefe die neuen Antworten") nicht erfuellbar: es gibt keine.
Inhaltlich offen bleibt genau die Frage, die die Nachbesserung selbst als Rest markiert hat:
**Kommissioniert IDEAL wirklich ausschliesslich auf Blattebene (`SubFA = 0`), oder gibt es reale
Faelle, in denen auf Baugruppen-/HauptFA-Ebene kommissioniert werden muss (z. B. fremdbezogene
statt gefertigte Baugruppen)?** Die `SubFA = 0`-Regel ist strukturell entschieden und fuer den
Dev-Lauf tragfaehig — aber ihre **fachliche Richtigkeit** kann nur der Mensch bestaetigen, und
diese Bestaetigung fehlt. Der Anhang (Abschnitt C, „Kommissionierlisten") stuetzt die Blatt-nur-Regel
NICHT: er listet `SubFA` als Anzeigespalte und nennt als Filter allein `Kommissionieren` — die
`SubFA = 0`-Einschraenkung ist eine reine Spec-Interpretation, nicht aus der Quelle abgeleitet. Vor
der Freigabe (Schranke 1) muss der Mensch Rueckfrage 1 explizit beantworten; solange ist die
Freigabe unvollstaendig.

### SOLLTE

**T3-2P-S1 — AK2 ist als „unbedingtes Akzeptanzkriterium" ueberformuliert; es ist in Wahrheit
bedingt.**
AK2 („jede kommissionierbare Position erscheint in **genau einer** Kommissionierliste",
ausdruecklich „nicht mehr konditional formuliert") gilt nur UNTER der noch unbestaetigten Annahme
aus Rueckfrage 1 (`Kommissionieren` wird nur auf Blaettern gesetzt). Trifft die Annahme nicht zu —
also im von Rueckfrage 1 selbst benannten Fall „auf Baugruppen-Ebene kommissionieren" — erscheint
eine kommissionierbare `SubFA <> 0`-Position in **KEINER** Liste (sie wird nur geloggt, Anforderung
3). Dann ist AK2 verletzt („in keiner Liste" statt „in genau einer"). Die Unbedingtheit von AK2 ist
also an dieselbe unbewiesene Praemisse gekoppelt wie B1. **Fix:** AK2 ehrlich an die Annahme koppeln
(„unter der Voraussetzung, dass `Kommissionieren` nur auf Blaettern gesetzt ist — Anomalien siehe
AK3") **oder** — besser — die Anomalie operator-sichtbar machen (S2), damit „null gezaehlt" nie
still passiert.

**T3-2P-S2 — Die Anomalie-Diagnose ist NUR ein Server-Log (`ILogger`) — fuer den Kommissionierer
unsichtbar, und das realisiert genau das erklaerte Kernrisiko.**
Ziel/Nutzen benennt das Kernrisiko selbst: „Mengen doppelt **oder gar nicht** ausweisen und damit
die Lagerentnahme direkt sachlich falsch steuern". Anforderung 3/AK3 fangen den Fall
`SubFA <> 0` UND `Kommissionieren` gesetzt zwar ab — aber nur als `ILogger`/Serilog-Warnung. Der
Kommissionierer, der die Liste/den Druck abarbeitet, sieht **nichts**: eine kommissionierbare
Position, die auf einer Baugruppen-Zeile haengt, verschwindet still aus seiner Liste, und der Hinweis
liegt nur im Serverlog. Damit tritt genau der „gar nicht ausweisen"-Fall ein, den die Spec als
teuersten Fehler bezeichnet. Der Kanal `ILogger` (statt `SyncLog`) ist fuer eine Web-Lesefunktion
korrekt begruendet — es fehlt die **Sichtbarkeit**. **Fix:** Anomalien zusaetzlich im UI und im Druck
sichtbar machen (z. B. `TempData["WarningMessage"]`/Banner „N Position(en) mit gesetztem
`Kommissionieren` auf Baugruppen-Ebene ausgeschlossen — Datenpflege pruefen"), nicht nur ins
Serverlog schreiben. Bei einer mengensteuernden Liste ist eine still fehlende Position ein
Betriebsrisiko, kein reines Diagnose-Detail.

**T3-2P-S3 — Teil 3 ist laut Uebersicht und Teil-4/5-`depends_on` die REFERENZIMPLEMENTIERUNG des
gemeinsamen Bausteins — die Teil-3-Spec selbst sagt das nirgends.**
Die Uebersicht („Ergaenzende Querschnitts-Entscheidungen") und die Nachbesserungen von Teil 4/Teil 5
legen fest: Teil 3 „schneidet den Baustein bewusst wiederverwendbar" (Filter als Parameter,
gemeinsamer Header-Join ohne Fan-out, Gruppierung/Paging nach `HauptFA`, gemeinsames Druck-Scaffold),
Teil 4/5 „erweitern ihn, statt ihn zu duplizieren". Die Teil-3-Spec erwaehnt diese Verpflichtung an
KEINER Stelle — weder in Umfang, Loesungsentwurf noch `affected_code` (dort steht ein konkreter,
Teil-3-spezifischer `KommissionierListenService`, kein wiederverwendbarer Baustein). Ein Dev-Lauf,
der nur Teil 3 liest, baut die Mechanik Teil-3-eng; Teil 4/5 muessten dann refaktorieren — genau die
Divergenz, die die S-3-Entscheidung der Runde 1 vermeiden wollte. **Fix:** Den Baustein-Auftrag
explizit in Teil 3 aufnehmen (wiederverwendbarer Schnitt, Filter parametriert, gemeinsames
Header-/Gruppierungs-/Druck-Geruest) und in `affected_code` sichtbar machen.

### HINWEIS

**T3-2P-H1 — ColumnMap laesst `HauptArtnr` gegenueber der Anhang-Spaltenliste aus.** Der Anhang
(Abschnitt C) fuehrt fuer Kommissionierlisten `HauptArtnr` unter den FAListe-Spalten; die ColumnMap
im Loesungsentwurf (Schritt 2) listet sie nicht (`HauptFA, Artnr, Matchcode, Sollmenge,
Hauptlagerplatz, Kommissionieren, Arbeitsbereich, Artikeltyp, Beschichtet, Material`). `SubFA` fehlt
korrekt (durch `SubFA = 0` konstant, als Filter sinnlos). Fuer `HauptArtnr` klaeren, ob sie
Positions-Spalte oder Kopf-Metadatum ist — nicht wortlos weglassen.

**T3-2P-H2 — Bewusste, aber unzitierte Abweichung vom Anhang-Druckformat.** Der Anhang (Abschnitt C,
„Format") verlangt „je ein Ausdruck **pro Auftrag** mit Barcode `HauptFA`"; Teil 3 druckt „ein
Ausdruck je **`HauptFA`**-Gruppe" (bei Kombigeraeten mehrere Auftraege/Montage-Abteilungen auf einem
Blatt). Das ist konsistent mit „Kombigeraete out of scope" + „Barcode = `HauptFA`" und daher
richtig — aber die Spec zitiert vom Anhang nur den Identifier-Fallstrick, nicht die
„pro Auftrag"-Vorgabe. Ein Satz, der die Abweichung als bewusst kennzeichnet, macht die Spec
gegen den Anhang wasserdicht.

**T3-2P-H3 — Anomalie-Logging-Rauschen.** Die Anomalie-Warnung (Anforderung 3) feuert bei JEDEM
Listen-/Filter-/Paging-Aufruf erneut und im Druck ueber die GESAMTE gefilterte Menge (Web-Lesefunktion,
kein einmaliger Sync-Lauf). Bei vorhandenen Anomalien entsteht Log-Rauschen; ggf. je Lauf
deduplizieren oder drosseln.

BEREIT ZUR FREIGABE? **NEIN.**

NACHBESSERUNG NOETIG: (1) Offene Rueckfrage 1 (kommissioniert IDEAL nur auf Blattebene?) ist die
einzige fachliche Kernfrage und **unbeantwortet** — die im Auftrag angenommenen „erweiterten
Antworten" existieren nicht (Datei seit `ddcae5f` unveraendert); ohne die menschliche Bestaetigung
ist die Freigabe unvollstaendig (T3-2P-B1). (2) AK2 als bedingt kennzeichnen bzw. — besser — die
Anomalie operator-sichtbar machen (T3-2P-S1/S2), damit der von der Spec selbst benannte
„gar-nicht-ausweisen"-Fall nicht still im Serverlog verschwindet. (3) Den Referenz-/Baustein-Auftrag
fuer Teil 4/5 in Teil 3 verankern (T3-2P-S3). Die Doppelzaehlungs-Regel selbst ist geloest und wird
nicht neu aufgerollt.

### Nachbesserung 2 (2026-08-07)

Status je Befund aus der Kritischen Pruefung (2026-08-07) — Details in Fachlichen Anforderungen
2/3/11, Loesungsentwurf, Akzeptanzkriterien und Offene Rueckfragen oben, hier nur die Kurzfassung mit
Verweis:

- **T3-2P-B1 (Offene Rueckfrage 1 unbeantwortet) — TEILWEISE BEHOBEN: bewusste Arbeitsannahme statt
  Blockade, nicht geraten.** Der Mensch hat entschieden, mit einer **Arbeitsannahme** in den Dev-Lauf
  zu gehen statt auf eine vollstaendige Klaerung zu warten: Kommissioniert wird ausschliesslich auf
  Blattebene (`SubFA = 0`), strukturell hergeleitet (eine `SubFA != 0`-Zeile verweist auf eine im
  eigenen Sub-FA gefertigte Baugruppe, die nicht aus dem Lager geholt wird) — explizit als
  **Herleitung, kein dokumentierter Fakt** gekennzeichnet (Fachliche Anforderungen Punkt 2, Offene
  Rueckfrage 1). Die einzige verbleibende Rueckfrage ist auf genau diesen einen Punkt gekuerzt: die
  empirische Bestaetigung am IDEAL-Testsystem. Das ist **kein** vollstaendiger Abschluss von B1 (die
  fachliche Richtigkeit ist weiterhin unbestaetigt und kann nur der Mensch/IDEAL bestaetigen), aber
  ein bewusster, dokumentierter Uebergang von „unbeantwortet" zu „Arbeitsannahme mit
  Pflicht-Absicherung" (naechster Punkt) — die Freigabe (Schranke 1) haengt weiterhin an genau dieser
  einen Rueckfrage.
- **T3-2P-S1/S2 (AK2 ueberformuliert / Anomalie nur im Serverlog) — BEHOBEN durch Pflicht-Banner.**
  Auf ausdrueckliche menschliche Vorgabe ist die Anomalie-Diagnose jetzt **zweikanalig**: `ILogger`-
  Warnung **und** ein operator-sichtbares Banner in der Bildschirmliste **und** im Druck (Fachliche
  Anforderungen Punkt 3, jetzt unbedingtes AK3). AK2 ist ehrlich an die Arbeitsannahme gekoppelt und
  benennt den Fall „Annahme trifft nicht zu" explizit, verweist aber auf die Banner-Absicherung: eine
  betroffene Position verschwindet dann zwar aus der Liste, aber nicht mehr unbemerkt aus der
  operativen Sicht — genau der von T3-2P-S2 verlangte Fix. Zusaetzlich (H3) wird die Server-Log-Warnung
  je `HauptFA`/Request dedupliziert, um Log-Rauschen bei wiederholtem Filtern/Blaettern zu vermeiden.
- **T3-2P-S3 (Referenz-/Baustein-Rolle nicht verankert) — BEHOBEN.** Teil 3 benennt jetzt explizit
  seine Rolle als Referenzimplementierung des gemeinsamen FaHierarchy-Listen-/Druck-Bausteins, den
  Teil 4 und Teil 5 erweitern (Ziel/Nutzen, Umfang, Fachliche Anforderungen Punkt 11, neue AK 11,
  Loesungsentwurf-Einleitung, `affected_code`-Kommentare). Konsistent zur Uebersicht („Ergaenzende
  Querschnitts-Entscheidungen") und zu den bereits entsprechend formulierten Teil-4/5-Specs.
- **T3-2P-H1 (ColumnMap ohne `HauptArtnr`) — BEHOBEN.** `HauptArtnr` ist in der ColumnMap
  (Loesungsentwurf Schritt 2) als Positionsspalte ergaenzt, konsistent zur Anhang-Spaltenliste und
  zur Positionstabelle in Teil 4.
- **T3-2P-H2 (unzitierte Abweichung vom Anhang-Druckformat) — BEHOBEN.** Fachliche Anforderungen
  Punkt 5 zitiert jetzt woertlich die Anhang-Vorgabe „je ein Ausdruck pro Auftrag mit Barcode
  `HauptFA`" und kennzeichnet die Abweichung („ein Ausdruck je `HauptFA`-**Gruppe**") explizit als
  bewusst, konsistent zur Kombigeraete-out-of-scope-Entscheidung.
- **T3-2P-H3 (Anomalie-Logging-Rauschen) — BEHOBEN.** Fachliche Anforderungen Punkt 3.1 und
  Loesungsentwurf Schritt 1 legen fest, dass die `ILogger`-Warnung je `HauptFA` und Request
  dedupliziert wird statt je betroffener Position erneut zu feuern.

## ENTSCHEIDUNG NACH ERSTEM DATENLAUF (2026-08-12) — ueberschreibt die Arbeitsannahme

**Die Arbeitsannahme `SubFA = 0` ist am realen Datenbestand WIDERLEGT.** Das Pflicht-Banner hat
genau seine Aufgabe erfuellt: *„9 Position(en) mit gesetztem `Kommissionieren` auf Baugruppen-Ebene
(`SubFA != 0`) ausgeschlossen — Datenpflege pruefen. Betroffene HauptFA: 1043153."* Es ist kein
Pflegefehler, sondern der fachliche Normalfall.

**Der `SubFA = 0`-Filter wird ERSATZLOS ENTFERNT.** Kommissioniert wird ausschliesslich nach
`Kommissionieren IS NOT NULL AND <> ''` — auf **allen** Ebenen. Offene Rueckfrage 1 ist damit
beantwortet und geschlossen.

**Warum das keine Doppelzaehlung ist — und was daraus folgt:**
Eine Baugruppe (`SubFA != 0`) und ihre Bestandteile sind **zwei verschiedene Kommissioniervorgaenge
zu verschiedenen Zeitpunkten an verschiedenen Arbeitsplaetzen**:
- Die **Bestandteile** werden geholt, um die Baugruppe zu **bauen** (im Beispiel `S-01`).
- Die **fertige Baugruppe** wird geholt, um sie zu **verbauen** (im Beispiel `H1-03`).

Daraus folgt zwingend: **Der Arbeitsbereich wird zur tragenden Dimension der Liste**, nicht nur zu
einer Spalte. Ohne ihn mischt eine Liste zwei verschiedene Kommissionier-Auftraege, und der Werker
holt Teile, die er nicht braucht. Konkret:
- **Arbeitsbereich als Filter mit Auswahlliste** (nicht Freitext) prominent in der Filterkarte.
- Sinnvoll als **Gruppierungsebene** innerhalb des `HauptFA` bzw. als eigener Reiter — beim ersten
  echten Test mit dem Anwender pruefen, welche der beiden Formen bedienbar ist.

**Anzupassen:**
1. **Filter:** `SubFA = 0` raus, nur noch `Kommissionieren` gesetzt.
2. **AK 2** neu: „Eine Position erscheint genau einmal je `HauptFA`-Gruppe. Positionen auf
   Baugruppen-Ebene erscheinen ebenso wie Blattpositionen." Die Kopplung an die Arbeitsannahme
   entfaellt.
3. **AK 3 / Banner / Anomalie-Diagnose:** entfaellt ersatzlos — es gibt keine Anomalie mehr. Auch
   die `ILogger`-Warnung samt Deduplizierung (H3) faellt weg.
4. **Fachliche Anforderungen 2 und 3** entsprechend umschreiben; die Herleitung als **historisch**
   kennzeichnen, nicht loeschen — sie erklaert, warum das Banner ueberhaupt gebaut wurde.
5. **Teil 5 mitziehen:** Dort steht dieselbe Ebenenregel (aus Teil 3 uebernommen). Sie ist damit
   ebenfalls hinfaellig — `VMBedarf` filtert ohne `SubFA`-Einschraenkung.

**Das Banner hat sich bewaehrt.** Ohne die Pflicht-Vorgabe waeren die neun Positionen still
verschwunden und der Fehler erst beim Anwender aufgefallen — vermutlich als fehlendes Material an
der Endmontage. Das Muster „Annahme + sichtbare Anomalie-Meldung statt stillem Filter" gehoert
deshalb in `secondbrain/architektur/muster/` als wiederverwendbares Vorgehen.

## ERWEITERUNG (2026-08-12): Summierte Sicht auch fuer Kommissionierlisten

Die Kommissionierliste bekommt — wie Teil 5 — **zwei Sichten**: Einzelpositionen (bisher) und
**Summiert**. Das Muster existiert bereits in [[2026-07-29-standort-ideal-teil-5-spec]]:
Aggregation nach **`Matchcode`**, Summen ueber `Sollmenge` **und** `Fertigungmenge` getrennt.

**In den gemeinsamen Baustein, nicht in Teil 3 allein.** Teil 3 ist die Referenzimplementierung —
die summierte Sicht gehoert daher in den geteilten Listen-Baustein, damit Teil 4 und Teil 5 sie
**erben** statt sie ein zweites und drittes Mal zu bauen. Teil 5 braucht dann keine eigene
Aggregationsmechanik mehr, nur noch die Konfiguration (`VMBedarf` statt `Kommissionieren`).

**ACHTUNG — Falle, die durch das Entfernen des `SubFA = 0`-Filters neu entsteht:**
Solange nur Blattpositionen in der Liste waren, war eine Summe unproblematisch. Jetzt enthaelt die
Menge **Baugruppen UND deren Bestandteile**. Eine Summe ueber die gesamte Struktur addierte damit
die Baugruppe **und** die Teile, aus denen sie besteht — fachlich sinnlos („eine Tuer plus die vier
Bleche, aus denen die Tuer ist").

**Verbindlich:** Die Summierung ist **immer auf einen Arbeitsbereich eingegrenzt** — nie ueber alle
Ebenen und Bereiche hinweg. Innerhalb eines Arbeitsbereichs gilt genau ein Vorgang (dort bauen ODER
dort verbauen), damit ist die Summe die Menge, die der Werker an diesen Platz holen muss — und genau
das ist der Zweck der Sicht.
Eine unbeschraenkte Gesamtsumme wird **nicht** angeboten. Wuerde sie angeboten, waere sie die Zahl,
nach der jemand irgendwann disponiert.

**Anzupassen:** In-Scope-Abschnitt (zwei Sichten), eigenes AK fuer die Aggregation (Schluessel
`Matchcode`, getrennte Summen, Pflicht-Eingrenzung auf Arbeitsbereich), Baustein-Rolle (AK 11) um
die summierte Sicht erweitern; `depends_on`-Hinweis in Teil 5, dass die Mechanik aus Teil 3 kommt.

> **Hinweis (Nachtrag 2026-08-13):** Diese Erweiterung skizzierte den generischen Rahmen einer
> Summiert-Sicht (uebernommen aus Teil 5: `Matchcode`, `Sollmenge`+`Fertigungmenge`, Arbeitsbereichs-
> Eingrenzung). Fuer die Kommissionierliste selbst hat der Mensch in der UAT vom 2026-08-13 davon
> abweichende, konkrete Regeln festgelegt (Aggregation nach `Artnr` statt `Matchcode`, nur
> `Sollmenge`, Trennung nach `Kommissionieren`-Ziel statt/zusaetzlich zu Arbeitsbereich, KW-Filter
> auf `KO_Termin` statt Arbeitsbereichs-Reiter) — siehe Aenderung 2 im Nachtrag am Dateiende. Diese
> Erweiterung bleibt als Beleg des urspruenglichen, generischen Plans stehen; fuer Teil 3 gilt die
> konkretere Fassung im Nachtrag.

## ERGAENZUNG (2026-08-12): Spaltenauswahl anschliessen — in DIESEM Epic

**Befund:** Die neuen IDEAL-Listen nutzen die bestehende Spalten-Mechanik der Anwendung nicht. Die
Tabelle in `Views/FaHierarchyKommissionierListen/Index.cshtml` ist bereits richtig ausgezeichnet
(`data-view-key`, `data-col-key` an jedem `<th>`, `filterable-table`), aber
`column-preferences.js` wird nicht eingebunden — also kein Zahnrad, keine Spaltenauswahl, keine
Breiten, keine gespeicherte Sortierung. Der Rest der Anwendung hat das ueberall
(`ProductionOrders` u. a.).

**Vorgabe: konsistent zum Bestehenden, gleiche Logik — anschliessen, nicht nachbauen.** Server-Seite
(`UserViewPreferencesApiController`, `UserViewPreferenceRepository`, Persistenz je Benutzer) steht
bereits und ist view-key-agnostisch. Es fehlen je View **drei Bloecke**, Vorlage
`Views/ProductionOrders/Index.cshtml` (Dateiende): `<script type="application/json" id="view-config">`,
`<script type="application/json" id="column-config">` und die Einbindung von
`~/js/column-preferences.js` **vor** `table-filter.js`.

**Gehoert in den gemeinsamen Baustein**, nicht in die Einzelview: Teil 3 schneidet ihn gerade —
nimmt er die Bloecke auf, erben Teil 4 und Teil 5 sie kostenlos. Nachtraeglich sind es drei Views
einzeln.

**Konfiguration:** `locked: true` fuer die identifizierenden Spalten (`hauptfa`, `matchcode`);
selten gebrauchte (`artikeltyp`, `material`, `beschichtet`) mit `defaultHidden: true`, weil die
Liste mit elf Spalten breit ist. **Kein C#, keine Migration.** Details:
[[2026-08-12-listen-spaltenauswahl]].

**Ausgenommen: die FA-Struktur (Teil 2).** Sie ist ein div-Baum ohne ausgerichtete Spalten;
`column-preferences.js` arbeitet ueber Spaltenindizes einer echten Tabelle. Dort ist zuerst die
Tree-Table-Umstellung noetig — siehe [[2026-08-12-fa-struktur-darstellung]], bleibt ausserhalb
dieses Epics.

### Ursachenbehebung: ADR 0005 um das vollstaendige Listen-Paket ergaenzen

**Warum es ueberhaupt gefehlt hat:** Die Spec hat den ADR-0005-Listenteil verlangt (Pagination,
Filterkarte, Server-Side-Spaltenfilter) — die **Spaltenpraeferenzen aber nie erwaehnt**. Der
Dev-Lauf hat exakt gebaut, was dastand. Das ist kein Einzelversehen, sondern ein Muster: Es
wiederholt sich bei jeder neuen Liste, solange die Vollstaendigkeit nur im Gedaechtnis einzelner
Menschen existiert und nicht im Muster selbst.

**Deshalb wird ADR 0005 ergaenzt** — Spaltenpraeferenzen als **vierter, verbindlicher Bestandteil**:

> Eine neue Liste ist erst vollstaendig, wenn sie **alle vier** Bestandteile hat:
> (1) Pagination (`PageSize.Resolve` + `PaginationState` + `_Pagination`),
> (2) Filterkarte,
> (3) Server-Side-Spaltenfilter (`data-server-column-filter`, `data-col-key` je `<th>`),
> (4) **Spaltenpraeferenzen** (`view-config` + `column-config` + `column-preferences.js`).

Konkret in ADR 0005: den vierten Punkt in die Checkliste, `column-preferences.js` in die
Dateiliste, und `Views/ProductionOrders/Index.cshtml` als Referenzimplementierung benennen.

**Wirkung:** Jede kuenftige Spec, die „nach ADR 0005" sagt, verlangt damit automatisch auch die
Spaltenauswahl — ohne dass es jemand erinnern muss. Das ist der eigentliche Fix; die drei Bloecke
in den IDEAL-Views sind nur das Symptom.

## Nachtrag / Aenderung aus UAT (2026-08-13)

Zwei vom Menschen in der manuellen UAT (Schranke-2-Test) getroffene, verbindliche Aenderungen. Sie
konkretisieren/ersetzen fuer Teil 3 die vorherigen Abschnitte „ENTSCHEIDUNG NACH ERSTEM DATENLAUF
(2026-08-12)" und „ERWEITERUNG (2026-08-12): Summierte Sicht" — jene bleiben als historischer Beleg
stehen, verbindlich ist ab hier dieser Nachtrag. Ebenso **ueberholt** sind Fachliche Anforderungen
Punkt 2 (Blattregel `SubFA = 0`) und Punkt 3 (Anomalie-Diagnose/Banner) sowie die darauf gestuetzten
Akzeptanzkriterien 1–3 — beide Punkte bleiben im Text stehen (mit Markierung), weil sie erklaeren,
warum die Blatt-Annahme und das Banner ueberhaupt gebaut wurden und wie das Banner seine Aufgabe
erfuellt hat (siehe „ENTSCHEIDUNG NACH ERSTEM DATENLAUF").

### Aenderung 1 — SubFA=0-Blattannahme aufgehoben (aus UAT widerlegt)

**Verbindliche Anforderung:** Es werden **alle** `FaHierarchyNode`-Positionen mit gesetztem
`Kommissionieren` (`IS NOT NULL AND <> ''`) gelistet und gezaehlt, **unabhaengig von `SubFA`** —
sowohl Blattpositionen (`SubFA = 0`) als auch Baugruppen-Zeilen (`SubFA != 0`). Das Anomalie-Banner
(bisherige Fachliche Anforderung 3) **entfaellt vollstaendig**, ebenso die zugehoerige
`ILogger`-Warnung samt Deduplizierung: es gibt keinen Ausnahmefall mehr, den es zu melden gaebe. Das
`Kommissionieren`-Flag aus Sage ist die alleinige, massgebliche Quelle dafuer, ob eine Position
kommissioniert wird — nicht die `SubFA`-Ebene.

**Umsetzung ueber den gemeinsamen Baustein:** `KommissionierListenService.BuildAsync` ruft
`FaHierarchyListBuilder.Build(...)` mit `leafOnly: false, anomalyOnNonLeaf: false` auf — exakt die
Parametrisierung, die Teil 4 (Beschichtung) fuer denselben Baustein bereits verwendet. Der Baustein
selbst (`FaHierarchyListBuilder`) unterstuetzt das bereits (Opt-in-Naht `leafOnly`/`anomalyOnNonLeaf`,
Default `true`/`true` fuer Teil 3 bisher) — es ist eine Parameteraenderung am Aufrufer, keine
Aenderung am Baustein. `GetTargetValuesAsync` (Quelle des Ziel-Dropdowns) ist konsistent mitzuziehen:
die bisherige `n.SubFA == 0`-Einschraenkung dort entfaellt ebenfalls, sonst fehlen Ziel-Werte, die
nur auf Baugruppen-Ebene vorkommen, weiterhin im Dropdown.

**Caveat (zu dokumentieren, keine Design-Entscheidung mehr offen):** Traegt eine Baugruppe
(`SubFA != 0`) **und** ihre Bestandteile (deren eigener Sub-FA) beide das `Kommissionieren`-Flag,
erscheinen beide als eigene Zeilen/Gruppen — das ist **beabsichtigt** (zwei verschiedene
Kommissioniervorgaenge an verschiedenen Arbeitsplaetzen, siehe „ENTSCHEIDUNG NACH ERSTEM
DATENLAUF"), **keine** Doppelzaehlung im Sinne eines Bugs. Die Mengen-Richtigkeit einer **Summe**
ueber mehrere Ebenen hinweg liegt damit in der Sage-Datenpflege (korrekte `Kommissionieren`-Zuweisung
je Ebene) — bei der UAT sind Summen gegen die Realitaet zu pruefen (siehe Test-Szenarien unten).

**Akzeptanzkriterien:**
- N1a. Eine synthetische Fixture mit zwei Zeilen desselben `HauptFA` — einer Baugruppen-Zeile
  (`SubFA != 0`, `Kommissionieren` gesetzt) und einer Blattzeile (`SubFA = 0`, `Kommissionieren`
  gesetzt) — zeigt **beide** Zeilen in der Kommissionierliste (Bildschirm und Druck).
- N1b. Bei derselben Fixture erscheint **kein** Anomalie-Banner (weder Bildschirm noch Druck) und es
  wird **keine** `ILogger`-Warnung erzeugt.
- N1c. `FaHierarchyKommissionierGruppeViewModel`/`FaHierarchyKommissionierPrintViewModel` fuehren
  kein Anomalie-Feld mehr (bzw. es bleibt strukturell leer/ungenutzt und wird nicht mehr gerendert).
- N1d. Das Ziel-Dropdown (`GetTargetValuesAsync`) enthaelt auch `Kommissionieren`-Werte, die nur auf
  `SubFA != 0`-Zeilen vorkommen.
- N1e. `FaHierarchyListBuilder.Build` wird im `KommissionierListenService` nachweisbar mit
  `leafOnly: false, anomalyOnNonLeaf: false` aufgerufen (Code-Review/Unit-Test der Aufrufstelle).

### Aenderung 2 — neue „Summiert"-Ansicht fuer die Kommissionierliste

**Verbindliche Anforderung:** Zweite Ansicht/Action `Summiert` am
`FaHierarchyKommissionierListenController` (analog zur Vormontage-Sicht 2
`FaHierarchyVormontageController.Summiert`, aber mit eigenen, fuer die Kommissionierliste
spezifischen Regeln — **nicht** identisch zu Teil 5 uebernehmen):
- **Aggregation:** je `HauptFA`, gruppiert nach **Artikelnummer (`Artnr`)**, summiert **nur
  `Sollmenge`** (kein `Fertigungmenge`-Pendant wie in Teil 5), **je Kommissionierziel
  (`Kommissionieren`-Wert) getrennt** — eine Aggregatzeile ist damit eindeutig durch
  `(HauptFA, Artnr, Kommissionieren)` bestimmt.
- **KW-Zeitraum-Eingabe:** ein Eingabefeld-Paar (Kalenderwoche von/bis, ISO-Kalenderwoche Mo–So),
  analog zu den Datumsfeldern der uebrigen Listen (Filterkarte). Die KW waehlt die einzubeziehenden
  `HauptFA` aus: alle `HauptFA`, deren zugehoerige(s) `FaHierarchyOrderInfo.KO_Termin` in die
  gewaehlte Kalenderwoche(n) faellt. Danach erfolgt die Summenbildung je `HauptFA`/`Artnr`/Ziel
  ausschliesslich innerhalb dieser vorgefilterten `HauptFA`-Menge.
- **`KO_Termin`-Definition bleibt unveraendert** (weiterhin der Konstruktions-Termin aus Teil 1/
  Anhang) — der Mensch hat am 2026-08-13 ausdruecklich bestaetigt, dass fuer den KW-Filter genau
  dieses Feld verwendet wird, **keine** Aenderung an dessen Berechnung/Herkunft.
- **ADR 0005 vollstaendig:** Pagination (Seiteneinheit = Aggregatzeile, analog Teil 5 Sicht 2),
  Filterkarte (inkl. KW-von/KW-bis und Ziel-Dropdown), Server-Side-Spaltenfilter auf der
  Aggregat-Projektion, Spaltenpraeferenzen (`view-config`/`column-config`/`column-preferences.js`,
  siehe ADR-0005-Ergaenzung oben).
- **Zugriffsschutz/Toggle identisch zur bestehenden Kommissionier-Ansicht:**
  `RequireLagerProcessingAccessAttribute` + `RequireFaHierarchyKommissionierlistenAktivAttribute`
  (kein eigener Toggle, keine neue Rolle).

**Akzeptanzkriterien:**
- N2a. Eine synthetische Fixture mit zwei Positionszeilen desselben `HauptFA`, derselben `Artnr` und
  demselben Kommissionierziel liefert **eine** Aggregatzeile mit der **Summe** der `Sollmenge`
  beider Zeilen.
- N2b. Zwei Zeilen mit derselben `Artnr`, aber **unterschiedlichem** Kommissionierziel, liefern
  **zwei getrennte** Aggregatzeilen (keine Vermischung ueber Ziele hinweg).
- N2c. Ein `HauptFA`, dessen `FaHierarchyOrderInfo.KO_Termin` **ausserhalb** der gewaehlten
  KW-Zeitraum-Eingabe liegt, erscheint in der Summiert-Ansicht **nicht** (auch wenn er passende
  `Kommissionieren`-Positionen haette); ein `HauptFA` **innerhalb** der KW erscheint vollstaendig.
- N2d. Ohne KW-Eingabe zeigt die Ansicht ein definiertes Verhalten (z. B. Hinweistext „Kalenderwoche
  eingeben" statt einer ungefilterten Gesamtsumme) — es gibt **keine** implizite Gesamtsumme ueber
  alle `HauptFA` hinweg.
  > [!warning] **Überholt (2026-09-28):** N2d gilt nicht mehr. Seit [[2026-09-25-kommissionierliste-summierung-pdf-spec]]
  > (Antwort 4, v1.47.0) zeigt die Summiert-Ansicht **ohne** KW alle HauptFA (Summe je HauptFA, keine
  > auftragsübergreifende Gesamtsumme); die KW ist nur noch optionaler Zusatzfilter.
- N2e. Zugriff nur mit `RequireLagerProcessingAccessAttribute` **und** aktivem Toggle
  `FaHierarchyKommissionierlistenAktiv`; identisches Verhalten wie bei `Index`/`Print` bei fehlender
  Rolle/inaktivem Toggle.
- N2f. Die Ansicht erfuellt ADR 0005 vollstaendig (Pagination, Filterkarte, Server-Spaltenfilter,
  Spaltenpraeferenzen) — code-pruefbar analog zu AK5 der bestehenden Liste.

### Bezug zu den ueberholten Anforderungen

Fachliche Anforderungen 2 und 3 sowie Akzeptanzkriterien 1–3 (weiter oben im Dokument, jeweils mit
`[UEBERHOLT ...]`-Markierung versehen) beschreiben die urspruengliche, durch die UAT widerlegte
`SubFA = 0`-Blattregel samt Pflicht-Anomalie-Banner. Sie werden **nicht geloescht** — sie bleiben als
historischer Beleg dafuer stehen, dass die Blattregel bewusst als Arbeitsannahme mit eingebauter
Absicherung (Banner) gebaut wurde, und dass genau diese Absicherung in der Praxis (2026-08-12,
„9 Position(en) ... ausgeschlossen") ihre Aufgabe erfuellt hat, bevor sie mit diesem Nachtrag
ersatzlos entfaellt. Verbindlich fuer die weitere Umsetzung sind ab diesem Nachtrag ausschliesslich
Aenderung 1 und Aenderung 2 oben.

### Test-Szenarien (Ergaenzung)

Neue Faelle fuer `docs/TESTSZENARIEN.md`, Kapitel „IDEAL Teil 3 — Kommissionierlisten":
- Baugruppen-Zeile (`SubFA != 0`) mit gesetztem `Kommissionieren` erscheint in Liste und Druck, kein
  Anomalie-Banner, kein Log-Eintrag (N1a/N1b).
- Ziel-Dropdown enthaelt einen Wert, der nur auf einer `SubFA != 0`-Zeile vorkommt (N1d).
- Summiert-Ansicht: zwei Zeilen gleiche `Artnr`/gleiches Ziel → eine Summenzeile; gleiche
  `Artnr`/unterschiedliches Ziel → zwei Zeilen (N2a/N2b).
- Summiert-Ansicht: `HauptFA` ausserhalb der gewaehlten KW (`KO_Termin`) fehlt vollstaendig; `HauptFA`
  innerhalb der KW ist vollstaendig enthalten (N2c).
- Summiert-Ansicht ohne KW-Eingabe zeigt Hinweistext statt Gesamtsumme (N2d).
  *(Überholt 2026-09-28 durch [[2026-09-25-kommissionierliste-summierung-pdf-spec]]: ohne KW alle HauptFA.)*
- **Manuell am IDEAL-Testsystem:** Mengenabgleich der Summiert-Ansicht gegen eine bekannte
  Baugruppe/Bestandteile-Konstellation (Caveat aus Aenderung 1) — bestaetigt, dass Doppel-Erfassung
  in Sage (Baugruppe UND Bestandteile mit `Kommissionieren`) korrekt beide Vorgaenge abbildet und
  nicht faelschlich als ein Vorgang summiert wird.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen.
