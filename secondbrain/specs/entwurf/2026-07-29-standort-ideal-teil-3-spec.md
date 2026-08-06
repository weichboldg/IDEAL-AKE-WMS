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
  - IdealAkeWms/Views/FaHierarchyKommissionierListen/Index.cshtml (neu)
  - IdealAkeWms/Views/FaHierarchyKommissionierListen/Print.cshtml (neu)
  - IdealAkeWms/Models/AppSettingKeys.cs
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "B2/B-5: Scan liefert HauptFA (Gruppe, nicht eindeutig) — was passiert nach dem Scan in der Kommissionierliste? Ganze Gruppe anzeigen oder Werker waehlt Sub-FA?"
  - "Offene Frage 3 (Notiz): Geschwister-Materialfluss — gibt es ein Kennzeichen fuer hausintern gefertigte Positionen, oder muss ueber Artikelnummer gegen Geschwister-FAs gematcht werden? Muss das WMS eine Reihenfolge/Verfuegbarkeit erzwingen, oder ist das reine Information (PPS steuert)?"
  - "Offene Frage 4 (Notiz, harter Blocker) — Kommissionier-Doppelzaehlung: Haupt-FA fuehrt Baugruppen seiner Sub-FAs UND jeder Sub-FA fuehrt eigene Bestandteile. Auf welcher Ebene wird tatsaechlich kommissioniert (Haupt-FA, je Sub-FA, gemischt Zukauf/Fertigungsmaterial)? Vorbedingung fuer korrekte Mengen."
  - "Vorpruefung vor Feinspezifikation: gibt es Zeilen mit SubFA != 0 UND gesetztem Kommissionieren? Falls nein, ist Kommissionieren allein die Trennlinie Lagerentnahme/Eigenfertigung und es kann nichts doppelt gezaehlt werden."
  - "Rollen/Zugriff: bestehende Rolle (picking) wiederverwenden oder neue IDEAL-Rolle?"
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
Basis der IDEAL-Struktur (Teil 1) statt der AKE-BOM-Kette.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** Liste/Druck aller `FaHierarchyNode`-Positionen mit gesetztem `Kommissionieren`,
gruppiert nach `HauptFA` (+ Montage-Abteilung), Kopf aus `FaHierarchyOrderInfo`, Barcode `HauptFA`.

**Out-of-Scope:** tatsaechliche Buchung/Transfer (dieser Teil druckt/listet, bucht aber nicht —
eine Buchungsfunktion setzt echte `ProductionOrders` voraus, also fruehestens nach Teil 7/8, falls
ueberhaupt gewuenscht — nicht Teil dieser Spec); `ProductionOrders`/AKE unveraendert.

## Fachliche Anforderungen

- Filter: `Kommissionieren IS NOT NULL AND Kommissionieren <> ''`, zusaetzlich Filter nach
  konkretem Ziel-Wert.
- Gruppierung nach `HauptFA` (+ `MontageAbteilung` aus `FaHierarchyOrderInfo` bei Kombinationsgeraeten).
- Kopf aus `FaHierarchyOrderInfo` (`ABNr`, `HauptFA`, Kunde/Termine je nach Layout-Bedarf).
- Barcode = `HauptFA` (einziger Produktions-Identifier laut Anhang).
- **Vor der Feinspezifizierung zwingend zu pruefen** (Notiz-Vorgabe): existieren Zeilen mit
  `SubFA != 0` UND gesetztem `Kommissionieren`? Wenn nein, trennt `Kommissionieren` selbst
  Lagerentnahme von Eigenfertigung sauber. Wenn ja, zusaetzlich ueber `SubFA = 0` bzw.
  `Beschaffungsartikel` filtern, um keine Baugruppen-internen Positionen mitzuziehen.

## Technischer Loesungsentwurf

`KommissionierListenService` liest ueber `IFaHierarchyNodeRepository`/`IFaHierarchyOrderInfoRepository`
(Teil 1), wendet Filter/Gruppierung an, liefert ein Druck-ViewModel analog zum bestehenden
`WarehousePickingPrintLayout`-Muster (GUI-Spiegelung: gleiche Filter/Sortierung im Druck wie in
der Liste).

## Migrations-/SQL-Auswirkungen

Keine — reine Lesefunktion.

## Audit-Feld-Auswirkungen

Keine neuen Entitaeten. Falls eine spaetere Ausbaustufe eine Buchungsfunktion ergaenzt, ist das
gegen echte `ProductionOrders`/`StockMovement` zu bauen (Teil 7/8), nicht gegen die
Struktur-Cache-Tabellen.

## Akzeptanzkriterien

1. Liste zeigt ausschliesslich Positionen mit gesetztem `Kommissionieren`.
2. Gruppierung trennt Kombinationsgeraete korrekt nach Montage-Abteilung.
3. Druck spiegelt exakt Filter/Sortierung der Bildschirmliste (analog Lagerbestellungs-Druck-Muster).
4. **Keine Doppel- oder Nullzaehlung** von Mengen (Akzeptanzkriterium, sobald offene Rueckfrage 3
   geklaert ist — bis dahin Blocker fuer den Dev-Lauf).
5. AKE-Verhalten unveraendert.

## Test-Szenarien

Neues Kapitel „IDEAL Teil 3 — Kommissionierlisten": Filter auf ein konkretes Kommissionier-Ziel;
Kombinationsgeraet mit zwei Montage-Abteilungen korrekt getrennt; Mengenabgleich Struktur vs.
Liste (kein Doppelzaehlen); Druck-Vergleich Bildschirm vs. Papier.

## Deploy

- **Web-App:** ja.
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  (provisorisch).

## Offene Rueckfragen

1. B2/B-5 — Scan = Gruppe: Was passiert nach dem Scan (ganze Gruppe zeigen vs. Sub-FA-Auswahl)?
2. Offene Frage 3 der Notiz — Geschwister-Materialfluss: Kennzeichen fuer Eigenfertigung
   vorhanden, oder Artikelnummer-Matching noetig? Braucht das WMS eine Reihenfolge-/
   Verfuegbarkeitslogik?
3. Offene Frage 4 der Notiz (harter Blocker) — auf welcher Ebene wird kommissioniert, um
   Doppelzaehlung zu vermeiden?
4. Existieren Zeilen mit `SubFA != 0` UND gesetztem `Kommissionieren` am Testsystem?
5. Rollen/Zugriff fuer diese Liste.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

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
