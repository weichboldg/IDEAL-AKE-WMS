---
type: spec
title: "IDEAL-Standort Teil 5 — Vormontage-Listen"
slug: 2026-07-29-standort-ideal-teil-5-spec
status: Entwurf
created: 2026-08-06
updated: 2026-08-06
source_backlog: "[[2026-07-29-Standort-IDEAL]]"
depends_on: "[[2026-07-29-standort-ideal-teil-1-spec]], [[2026-07-29-standort-ideal-teil-3-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/FaHierarchyVormontageController.cs (neu, Name provisorisch)
  - IdealAkeWms/Services/VormontageService.cs (neu)
  - IdealAkeWms/Views/FaHierarchyVormontage/Index.cshtml (neu)
  - IdealAkeWms/Views/FaHierarchyVormontage/Summiert.cshtml (neu)
  - IdealAkeWms/wwwroot/js/ideal-vormontage-export.js (neu, Isolierfraesen-Export)
  - IdealAkeWms/Models/AppSettingKeys.cs
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Isolierfraesen-Software-Import-Format: genaue Spalten-/Trennzeichen-Spezifikation fehlt noch (Anhang referenziert nur 'kompatibel zum Import', kein Format-Dokument)"
  - "Referenz-Screenshot Konzept_Uebertrag_MDE_System.docx (Vormontage-Ansicht mit Reitern pro Arbeitsbereich) — Datei nicht Teil dieser Spec-Runde, vor Feinspezifikation zu beschaffen"
  - "Rollen/Zugriff: bestehende Rolle vorbau wiederverwenden oder neue IDEAL-Rolle?"
  - "Wochenbezug ('kommende Woche') von VMBedarf-Terminen: welches Datumsfeld ist massgeblich (FE_Termin, Neuer_PT_PPS, Verladetermin_Vsl)?"
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

Jeder Vormontage-Arbeitsbereich (Feld `VMBedarf`) bekommt eine Liste seiner in der kommenden
Woche vorzubereitenden Teile — analog zur bestehenden AKE-Vorbau-Abarbeitungsliste, aber auf
IDEAL-Struktur-Basis (Teil 1) und mit eigener Aggregations-/Export-Logik.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** drei Sichten — (1) Einzelne Teile (flache Liste), (2) Summierte Liste (aggregiert
nach Artikel), (3) Export in Zwischenablage im Isolierfraesen-Import-Format.

**Out-of-Scope:** keine Rueckmeldefunktion (Teil 8); `ProductionOrders`/AKE unveraendert; keine
Integration in die bestehende `FaWorklist` (separate Domain laut B5).

## Fachliche Anforderungen

- Filter: `VMBedarf` gefuellt.
- FAListe-Spalten: `HauptFA`, `HauptArtnr`, `Artnr`, `Matchcode`, `Sollmenge`, `Fertigungmenge`,
  `BemerkungPN`, `VMBedarf`.
- FAInfos-Spalten: `FE_Termin`, `MontageAbteilung`, `Prio`, `Neuer_PT_PPS`, `Verladetermin_Vsl`.
- Gruppierung/Reiter je Arbeitsbereich (`VMBedarf`-Wert), analog zur Referenz-Excel-Ansicht.

## Technischer Loesungsentwurf

`VormontageService` liest ueber die Teil-1-Repositories, baut die drei Sichten. Export-Sicht
generiert Zwischenablage-Text im (noch zu klaerenden) Isolierfraesen-Format ueber JS
(`navigator.clipboard`), analog zu bestehenden Clipboard-Mustern im Projekt.

## Migrations-/SQL-Auswirkungen

Keine — reine Lesefunktion.

## Audit-Feld-Auswirkungen

Keine neuen Entitaeten.

## Akzeptanzkriterien

1. Alle drei Sichten (Einzelteile, Summiert, Export) zeigen ausschliesslich Positionen mit
   gesetztem `VMBedarf`.
2. Summierte Sicht aggregiert korrekt nach Artikel (keine Doppelzaehlung ueber mehrere Strukturen
   hinweg).
3. Export liefert eine Zwischenablage-Ausgabe, die sich unveraendert in die
   Isolierfraesen-Software importieren laesst (sobald Format-Spezifikation vorliegt).
4. AKE-Verhalten unveraendert.

## Test-Szenarien

Neues Kapitel „IDEAL Teil 5 — Vormontage-Listen": Testfall mit mehreren Strukturen und
uebereinstimmenden Artikeln in der Summierten Sicht; Export-Zwischenablage gegen die
Isolierfraesen-Software abnehmen (sobald Format vorliegt); Reiter-Wechsel zwischen
Arbeitsbereichen.

## Deploy

- **Web-App:** ja.
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  (provisorisch).

## Offene Rueckfragen

1. Isolierfraesen-Import-Format (Spalten/Trennzeichen) fehlt.
2. Referenz-Screenshot `Konzept_Uebertrag_MDE_System.docx` nicht Teil dieser Spec-Runde — vor
   Feinspezifikation zu beschaffen.
3. Rollen/Zugriff fuer diese Listen.
4. Massgebliches Datumsfeld fuer „kommende Woche".

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. →bitte für später vormerken, habe ich jetzt nicht
2. →bitte für später vormerken, habe ich jetzt nicht
3. →wie gehabt - bei unseren abarbeitungslisten
4. →produktionstermin

### Ausformuliert (2026-08-06)

**Zu 1 und 2 — Konsequenz: Der Isolierfraesen-Export wird aus Teil 5 HERAUSGENOMMEN.**
Weder das Import-Format (Spalten/Trennzeichen) noch der Referenz-Screenshot liegen vor.
"Fuer spaeter vormerken" darf nicht heissen, dass der Export im Umfang bleibt und AK 3 ein leeres
Versprechen wird — gebaut werden kann er ohne Formatvorgabe nicht.
Daher verbindlich:
- **In-Scope von Teil 5 sind zwei Sichten:** (1) Einzelne Teile (flache Liste), (2) Summierte
  Liste (aggregiert nach Artikel).
- **Sicht (3) Export in die Zwischenablage entfaellt** aus diesem Teil, ebenso **AK 3** und der
  zugehoerige Test-Szenario-Punkt. `wwwroot/js/ideal-vormontage-export.js` faellt aus
  `affected_code`.
- **Backlog-Nachtrag anlegen:** "Vormontage — Isolierfraesen-Export", mit den beiden Vorbedingungen
  (Format-Spezifikation, Referenz-Screenshot `Konzept_Uebertrag_MDE_System.docx`). Wird
  nachgezogen, sobald beides vorliegt.

**Zu 3 — Berechtigung 1:1 aus den bestehenden Abarbeitungslisten.**
Keine neue Rolle, kein neuer Filter. Der neue Controller bekommt **denselben**
Class-Level-Read-Filter, den die bestehende **Vorbau-/Abarbeitungsliste** traegt; der Dev-Lauf
liest ihn dort aus dem Class-Level-Attribut und setzt ihn identisch (Read-Ebene genuegt — Liste
ohne Bearbeitung). Steht dort kein Class-Level-Filter, ist das ein Fund und zurueckzumelden, nicht
stillschweigend zu uebergehen (ADR 0006).

**Zu 4 — Massgebliches Datumsfeld: `Neuer_PT_PPS`. [BESTAETIGT 2026-08-06]**
"Produktionstermin" ist der **PT-Termin aus der PPS-View**, also `Neuer_PT_PPS` — nicht `FE_Termin`
(Fertigstellung) und nicht `Verladetermin_Vsl` (Verladung). Der Wochenbezug ("kommende Woche")
wird auf diesem Feld gebildet. Der Termin wird **konsumiert, nicht gerechnet** (vgl. die
paketweite Entscheidung, dass bei IDEAL die PPS-Termine die Settings-Terminlogik ersetzen).

**Quer — Teil 5 setzt auf Teil 3 auf.** Die Listen-/Druck-Mechanik (Flag-Filter, Kopf-Abfrage je
`HauptFA`, Gruppierung, Paginierung ueber Gruppen, ADR-0005-Spaltenfilter) kommt aus dem in Teil 3
geschnittenen gemeinsamen Baustein. Teil 5 **erweitert** ihn um die Vormontage-Sichten, statt ihn
zu duplizieren. `depends_on` entsprechend ergaenzt.

**Quer — Testbarkeit.** Wie in Teil 3/4: Schranke 2 kann erst gruen werden, wenn produktivnahe
IDEAL-Daten mit gesetztem `VMBedarf` im Testsystem liegen. Als Vorbedingung in den Deploy-/
Test-Abschnitt.

**Hinweis:** Fuer Teil 5 liegt bisher **keine kritische Pruefung** vor. Vor der Freigabe
`/review secondbrain/specs/entwurf/2026-07-29-standort-ideal-teil-5-spec.md` laufen lassen —
besonders die Aggregation der Summierten Sicht (AK 2, "keine Doppelzaehlung ueber mehrere
Strukturen") verdient dieselbe Ebenen-Pruefung wie Teil 3: Auch hier gilt, dass nur Blaetter
(`SubFA = 0`) Material sind.

## Kritische Pruefung (2026-08-06)

Anwalt-des-Teufels-Durchsicht VOR der Freigabe. Gegengelesen: diese Teil-5-Spec inkl. der
ausformulierten Freigabe-Antworten, die Teil-1-Spec (Datenmodell `FaHierarchyNode`/
`FaHierarchyOrderInfo`, `MontageAbteilung`/PPS-Termine **nur** auf OrderInfo, Existenzpruefung statt
Fan-out-Join), die ueberarbeiteten Teil-3- und Teil-4-Specs (gemeinsamer Listen-/Druck-Baustein,
`SubFA = 0`-Ebenenregel, kein `INNER JOIN ... ON HauptFA`), die Uebersicht (Querschnitts-
Entscheidungen), die Ideen-Notiz inkl. „Kritische Pruefung", der Anhang [[sage-views-ideal]]
(`VMBedarf` = **nvarchar** Vormontage-Bereich, kein Bit; FAInfos-Granularitaet 1:n je `HauptFA`),
ADR 0005/0006 sowie echter Code (`FaWorklistController` mit `[RequireVorbauAccess]`,
`RequireVorbauAccessAttribute`, `ProductionOrdersController.Index`, Teil-3/4-Toggle-Muster). Die
Kern-Idee ist tragfaehig und die Feld-→Tabelle-Zuordnung stimmt — aber der **Spec-Rumpf ist nicht
auf die ausformulierten Freigabe-Antworten nachgezogen**, und mehrere Kern-Regeln aus Teil 1/3/4
fehlen komplett. In der jetzigen Fassung wuerde ein Dev-Lauf, der von oben nach unten liest, das
falsche Feature bauen.

### BLOCKER — vor der Freigabe zu klaeren

**T5-B1 — Der Spec-Rumpf widerspricht der Export-Streichung; ein Top-down-Dev-Lauf baut den bereits
verworfenen Isolierfraesen-Export.** Die „Ausformuliert (2026-08-06)"-Antwort nimmt den Export
**verbindlich heraus** (zwei Sichten, AK 3 entfaellt, `ideal-vormontage-export.js` faellt aus
`affected_code`). Der uebrige Spec-Text ist aber unveraendert auf **drei Sichten + Export**:
In-Scope (Z. 46–47 „drei Sichten … (3) Export in Zwischenablage"), Fachliche Anforderungen (der
Export bleibt implizit), Technischer Loesungsentwurf (Z. 63–64 „Export-Sicht generiert
Zwischenablage-Text … `navigator.clipboard`"), Akzeptanzkriterium **AK 1** („Alle drei Sichten …")
und **AK 3** (Export), das Test-Szenario („Export-Zwischenablage … abnehmen") sowie **Offene
Rueckfragen 1 und 2**. Im **Frontmatter** stehen weiterhin `ideal-vormontage-export.js` in
`affected_code` und beide Export-Punkte in `open_questions`. Der Zielabsatz spricht noch von
„eigener Aggregations-/**Export**-Logik". Nur der Ausformuliert-Block ist korrekt — der Rest ist der
alte Stand. **Fix:** Den gesamten Rumpf auf **zwei Sichten, kein Export** ziehen (In-Scope,
Fachliche Anforderungen, Technischer Loesungsentwurf, AK 1/AK 3, Test-Szenarien, Offene Rueckfragen
1/2, `affected_code`, `open_questions`). Der Backlog-Nachtrag existiert bereits
(`secondbrain/backlog/2026-08-06-vormontage-isolierfraesen-export.md`) — nur der Spec-Rumpf ist noch
inkonsistent.

**T5-B2 — Der Wochenbezug („kommende Woche", `Neuer_PT_PPS`) — der eigentliche Sinn des Features und
Gegenstand von Freigabe-Antwort 4 — fehlt in Fachlichen Anforderungen UND Akzeptanzkriterien und
kollidiert mit der Fan-out-Regel.** Ziel/Nutzen verspricht „in der **kommenden Woche**
vorzubereitenden Teile", und Antwort 4 legt `Neuer_PT_PPS` als massgebliches Datumsfeld fest. Die
Fachlichen Anforderungen sagen aber nur „Filter: `VMBedarf` gefuellt", und AK 1 fordert nur
„Positionen mit gesetztem `VMBedarf`" — die **Wochen-Einschraenkung ist nirgends als Anforderung
oder AK verankert**. Zusaetzlich sitzt `Neuer_PT_PPS` laut Teil 1 **auf `FaHierarchyOrderInfo`
(auftragsbezogen, 1:n je `HauptFA`)**, nicht auf den Positionszeilen (`FaHierarchyNode`). Eine
Positionsmenge nach einem Datum der 1:n-OrderInfo-Tabelle zu filtern, reisst **genau den
Fan-out-Join wieder auf**, den Teil 1/3/4 verbieten — und bei einem Kombigeraet mit zwei
OrderInfo-Zeilen ist unklar, **welcher** `Neuer_PT_PPS` gilt. **Fix vor Freigabe klaeren:** (a) ist
„kommende Woche" ein **harter Filter** oder nur eine anzeigbare/sortierbare Spalte; (b) welche
Wochengrenze (ISO-KW Mo–So, rollierende 7 Tage, aktuelle vs. naechste KW); (c) wie der auf
`FaHierarchyOrderInfo` liegende Termin **ohne Fan-out** an die Positionen gebunden wird (eigene
Abfrage je `HauptFA` wie in Teil 3/4, Termin als Kopf-Metadatum) und was bei mehrdeutigem
`Neuer_PT_PPS` (Kombigeraet) passiert. Danach als Fachliche Anforderung **und** AK niederschreiben.

**T5-B3 — Die Ebenen-/Doppelzaehlungsregel (`SubFA = 0`) fehlt vollstaendig — genau die Pruefung,
die der Mensch selbst verlangt.** Die Fachlichen Anforderungen haben **keinen** Blattfilter und
**keine** Anomalie-Diagnose (anders als Teil 3, Fachliche Anforderungen Punkt 2/3). `VMBedarf` sitzt
auf den `FAListe`-Positionen; ob es **nur** auf Blaettern (`SubFA = 0`) oder auch auf
Baugruppen-Verweiszeilen (`SubFA != 0`) gesetzt ist, ist unbekannt (Testsystem leer). Ohne die
`SubFA = 0`-Regel + Anomalie-Logging zaehlt die **Einzelteil-Liste** und erst recht die **Summierte
Sicht** Material doppelt (Haupt-FA fuehrt die Baugruppen seiner Sub-FAs, jeder Sub-FA seine
Bestandteile). Achtung — **AK 2 loest das NICHT:** „keine Doppelzaehlung ueber mehrere Strukturen
hinweg" beschreibt das **beabsichtigte** Aufsummieren gleicher Artikel ueber verschiedene Strukturen
(dort ist Summieren korrekt); die **gefaehrliche** Doppelzaehlung ist die **innerhalb** einer
Struktur ueber die Ebenen — genau der Teil-3-Fall, den der Schluss-Hinweis dieser Spec ausdruecklich
anmahnt („nur Blaetter `SubFA = 0` sind Material"). **Fix:** `SubFA = 0` als alleinige
Blatt-/Bedarfsregel und `VMBedarf` als Zusatzfilter niederschreiben (Reihenfolge: erst `SubFA = 0`,
dann `VMBedarf`), Anomalie-Diagnose (`SubFA != 0` UND `VMBedarf` gesetzt → geloggt, nicht gezaehlt)
analog Teil 3 Punkt 3 ergaenzen, und AK 2 auf **beide** Doppelzaehlungs-Achsen ausweiten (Ebenen
**und** Struktur-Aggregation).

**T5-B4 — ADR 0005 (Listen-View-Pattern) wird nirgends gefordert, und das Gruppierungs-/Paging-
Modell widerspricht sich.** Die Spec nennt an **keiner** Stelle Pagination (`PageSize.Resolve` +
`PaginationState` + `_Pagination`), Filterkarte oder Server-Side-Spaltenfilter
(`data-server-column-filter`, `data-col-key` je `<th>`, `ColumnFilterHelper.ReadFromQuery`) — harte
CLAUDE.md-/ADR-0005-Pflicht fuer jede Tabellen-View, in Teil 3/4 explizit gefordert, hier fehlend.
**Zusaetzlich widersprechen sich die Gruppierungsachsen:** Die Fachliche Anforderung sagt
„Gruppierung/Reiter je **Arbeitsbereich** (`VMBedarf`-Wert)", die Uebersicht schreibt dagegen
„Seiteneinheit durchgaengig die Gruppe (**`HauptFA`**)" fest, und der wiederverwendete
Teil-3-Baustein gruppiert/paginiert nach `HauptFA`. Was ist die Gruppen-/Reiter-/Seiteneinheit —
`VMBedarf` oder `HauptFA`? Und wie fuegt sich die **Summierte Sicht** (aggregiert nach Artikel, also
gerade **nicht** nach `HauptFA`-Gruppe) in ein `HauptFA`-Paging und in ADR-0005-Spaltenfilter ein?
**Fix:** Pattern explizit fordern und das Gruppen-/Paging-Modell fuer **beide** Sichten
widerspruchsfrei festlegen (inkl. wie „Reiter je `VMBedarf`" mit der HauptFA-Seiteneinheit
zusammengeht — oder dass die Summierte Sicht eine begruendete Client-Mode-Ausnahme ist).

### SOLLTE — macht den Dev-Lauf sicher

**T5-S1 — Feature-Toggle fehlt komplett.** Teil 3 hat `FaHierarchyKommissionierlistenAktiv`, Teil 4
`FaHierarchyBeschichtungAktiv`; die Uebersicht-Freigabe-Antwort 2 nennt ausdruecklich „die
Feature-Toggles der Listen (**Teil 3/4/5**)" als AppSettings, der Anhang schlug `IdealVormontageAktiv`
vor. Teil 5 benennt **weder** einen Toggle-Key **noch** ein Gate-Attribut (`affected_code` listet nur
`AppSettingKeys.cs`, ohne Key). **Fix:** `FaHierarchyVormontageAktiv` (Default `false`) +
`RequireFaHierarchyVormontageAktivAttribute` (Muster analog `RequireLagerbestellungAktivAttribute`,
invertierte Default-aus-Semantik wie Teil 3/4), kumulativ zum Rollen-Filter, plus README/Settings-
Doku — in `affected_code` und AK verankern.

**T5-S2 — Access-Filter nicht konkret benannt.** Freigabe-Antwort 3 („wie gehabt — bei unseren
Abarbeitungslisten") ist aufloesbar: die Abarbeitungsliste ist `FaWorklistController`, sie traegt
das Class-Level-Attribut **`[RequireVorbauAccess]`** (Read-Ebene). Die Spec verschiebt das aber auf
„der Dev-Lauf liest ihn dort aus", statt es zu nennen. Per ADR 0006 gehoert der konkrete Filter in
die Spec: **`RequireVorbauAccessAttribute`**, Read, Class-Level, keine neue Rolle. (Bestaetigt am
Code — der Fund „steht dort kein Class-Level-Filter" tritt nicht ein.)

**T5-S3 — Fan-out-Disziplin fuer die FAInfos-Kopfspalten unbenannt.** Die Spec will
`FE_Termin`, `MontageAbteilung`, `Prio`, `Neuer_PT_PPS`, `Verladetermin_Vsl` zeigen — **alle fuenf
auf der 1:n-Tabelle `FaHierarchyOrderInfo`**. Wie in Teil 3/4 muss stehen: **kein
`INNER JOIN ... ON HauptFA`** gegen die Positionen, Kopf-/Termindaten in **eigener Abfrage je
`HauptFA`**, `MontageAbteilung` nur informativ (kein Positions-Split), bei mehreren OrderInfo-Zeilen
alle im Klartext + Mehrdeutigkeits-Kennzeichnung. Ohne diese Ansage erbt Teil 5 die
Mengen-Verdopplung aus dem Anhang-Beispiel.

**T5-S4 — Aggregationssemantik der Summierten Sicht unterspezifiziert (nicht je pruefbar).** AK 2
sagt nur „aggregiert nach Artikel". Offen: Aggregationsschluessel `Artnr` allein oder
`Artnr`+`Matchcode` (laut Anhang-Fallstrick kann derselbe Artikel **positionsabhaengig
unterschiedliche Matchcodes** fuehren)? Welche Mengen werden summiert (`Sollmenge`,
`Fertigungmenge`)? Aggregation **je `VMBedarf`-Reiter** oder ueber alle Bereiche? Beide Sichten
muessen einzeln, testbar spezifiziert sein (Task-Punkt „drei Sichten je pruefbar").

### HINWEIS

**T5-H1 — `VMBedarf`-Semantik ist korrekt (kein `Beschichtet=-1`-Fehler).** Anders als der invertierte
`-1`-Fehler in Teil 4 ist `VMBedarf` laut Anhang eine **nvarchar**-Bereichsbezeichnung, **kein**
Sage-Bit. „Filter: `VMBedarf` gefuellt" ist damit richtig — praezisieren als
`VMBedarf IS NOT NULL AND VMBedarf <> ''` (analog `Kommissionieren` in Teil 3), und die
Reiter-Gruppierung erfolgt **nach dem `VMBedarf`-Wert**. Keine Boolean-Konvertierung noetig.

**T5-H2 — Backlog-Nachtrag existiert.** `secondbrain/backlog/2026-08-06-vormontage-isolierfraesen-export.md`
ist angelegt — die Export-Folgearbeit (Voraussetzungen: Import-Format-Spezifikation +
Referenz-Screenshot `Konzept_Uebertrag_MDE_System.docx`) ist erfasst. „Isolierfraesen" = Import in
die Software der nachgelagerten Isolierfraes-Maschine; fachlich fuer Teil 5 selbst jetzt
gegenstandslos.

**T5-H3 — Testbarkeits-Vorbedingung nur im Ausformuliert-Block.** Die (korrekte) Aussage „Schranke 2
erst gruen mit produktivnahen IDEAL-Daten mit gesetztem `VMBedarf`" steht nur im Quer-Absatz, nicht
im **Deploy**-Abschnitt selbst — dorthin (und in die Test-Szenarien) explizit als Vorbedingung
ziehen, analog Teil 3/4, damit der qa-agent nicht unbemerkt daran haengenbleibt. Ebenso im
Deploy-Abschnitt die Reihenfolge-Voraussetzung (Teil 1 produktiv + `Sync:HierarchicalFaEnabled`,
Teil 3 als Baustein) benennen — `depends_on` hat sie, der Deploy-Text nicht.

BEREIT ZUR FREIGABE? **NEIN.**

NACHBESSERUNG NOETIG: (1) Spec-Rumpf auf zwei Sichten / kein Export nachziehen (T5-B1); (2)
Wochenbezug `Neuer_PT_PPS` als Anforderung + AK verankern und den Fan-out an die 1:n-OrderInfo
aufloesen (T5-B2); (3) `SubFA = 0`-Ebenenregel + Anomalie-Diagnose ergaenzen und AK 2 auf beide
Doppelzaehlungs-Achsen ausweiten (T5-B3); (4) ADR-0005-Pattern fordern und das Gruppen-/Paging-Modell
(`VMBedarf` vs. `HauptFA`, Summierte Sicht) widerspruchsfrei festlegen (T5-B4). Zusaetzlich Toggle
und Access-Filter konkret benennen (T5-S1/S2) sowie Fan-out-Disziplin und Aggregationssemantik
schaerfen (T5-S3/S4). Erst danach freigeben.
