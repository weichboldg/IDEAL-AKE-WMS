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

## Antworten auf die Kritische Pruefung (2026-08-06)

**Zu T5-B4 — Gruppierungsachsen: Der Widerspruch geht auf eine zu pauschale Vorgabe von mir
zurueck. Aufloesung:**
Die Uebersichts-Festlegung „Seiteneinheit durchgaengig `HauptFA`" war fuer Listen von
**Positionszeilen** gedacht und traegt fuer eine **Aggregatsicht** nicht — die summiert ja gerade
**ueber** Auftraege hinweg. Verbindlich fuer Teil 5:

| | Reiter/Filter | Gruppierung | Seiteneinheit |
|---|---|---|---|
| Sicht 1 — Einzelne Teile | `VMBedarf` (Arbeitsbereich) | `HauptFA` | **`HauptFA`-Gruppe** |
| Sicht 2 — Summiert | `VMBedarf` (Arbeitsbereich) | Artikel (s. u.) | **aggregierte Zeile** |

- **`VMBedarf` ist eine Filter-/Reiter-Dimension, keine Seiteneinheit.** Der Reiter waehlt den
  Arbeitsbereich; erst *innerhalb* des Reiters greift die Gruppierung.
- **Sicht 1** verhaelt sich damit exakt wie Teil 3/4 — gleicher Baustein, keine Sonderlogik.
- **Sicht 2 ist eine begruendete Ausnahme** von der `HauptFA`-Seiteneinheit: Eine Aggregatzeile
  gehoert per Definition zu mehreren Auftraegen. Paginiert wird ueber die aggregierten Zeilen,
  `TotalCount` zaehlt sie. ADR-0005-Spaltenfilter wirken auf den **aggregierten** Zeilen
  (Matchcode, Summen), nicht auf den Rohpositionen.
- Die Uebersichts-Aussage ist entsprechend zu qualifizieren („gilt fuer Positionslisten;
  Aggregatsichten paginieren ueber ihre Aggregatzeilen"), sonst erbt jede kuenftige Liste den
  Widerspruch.

**Zu T5-S4 — Aggregation: Schluessel `Matchcode`, Summen ueber `Sollmenge` UND `Fertigungmenge`.**
- **Aggregationsschluessel: `Matchcode`** (nicht `Artnr`) — der Matchcode identifiziert eindeutig,
  was der Werker holt.
- **Summiert werden zwei Mengen getrennt:** `Sollmenge` und `Fertigungmenge`. Beide erscheinen als
  eigene Spalte, keine der beiden ersetzt die andere.
- **Aggregation je Reiter**, also je `VMBedarf`-Wert — nicht ueber alle Arbeitsbereiche hinweg.
  Sonst zeigte die Sicht Mengen an, die ein Bereich gar nicht bearbeitet.
- **Bekannte Folge, bewusst akzeptiert:** Fuehrt derselbe `Artnr` positionsabhaengig verschiedene
  Matchcodes (Anhang-Fallstrick), erscheinen daraus **mehrere** Zeilen. Das ist gewollt — der
  Matchcode ist die Identitaet, nicht die Artikelnummer. In der Spec als Verhalten benennen, damit
  es beim Test nicht als Fehler gemeldet wird.

**Zu T5-B1/B2/B3 und T5-S1/S2/S3 — uebernommen:**
- **B1:** Spec-Rumpf auf **zwei** Sichten ziehen; AK 3, Export-Punkt im Test-Szenario und
  `ideal-vormontage-export.js` aus `affected_code` entfernen. Backlog-Nachtrag existiert
  ([[2026-08-06-vormontage-isolierfraesen-export]]).
- **B2:** Wochenbezug auf `Neuer_PT_PPS` als **Fachliche Anforderung + AK** verankern (bisher nur
  im Antwortblock). Der Termin kommt aus der 1:n-`FaHierarchyOrderInfo` → **eigene Abfrage je
  `HauptFA`**, kein Join gegen die Positionen.
- **B3:** Ebenenregel **`SubFA = 0`** wie in Teil 3 aufnehmen; Anomalie-Diagnose (Zeile mit
  `SubFA != 0` UND gesetztem `VMBedarf` → nicht anzeigen, aber protokollieren). AK 2 auf **beide**
  Doppelzaehlungs-Achsen ausweiten: keine Baugruppen-Verweiszeilen, und keine Mehrfachzaehlung
  durch den Kopf-Join.
- **S1:** Toggle **`FaHierarchyVormontageAktiv`** (Default `false`) +
  `RequireFaHierarchyVormontageAktivAttribute`, kumulativ zum Rollenfilter, Key in
  `AppSettingKeys.cs` + Doku, in `affected_code` und AK verankern.
- **S2:** Access-Filter ist **`RequireVorbauAccessAttribute`** (Class-Level, Read) — die Pruefung
  hat ihn an `FaWorklistController` am Code bestaetigt. Damit entfaellt die Formulierung „der
  Dev-Lauf liest ihn dort aus": Der konkrete Filter steht jetzt in der Spec, wie ADR 0006 es
  verlangt. Keine neue Rolle.
- **S3:** Fan-out-Disziplin wie Teil 3/4 — alle fuenf Kopfspalten (`FE_Termin`,
  `MontageAbteilung`, `Prio`, `Neuer_PT_PPS`, `Verladetermin_Vsl`) kommen aus einer eigenen
  Abfrage je `HauptFA`; `MontageAbteilung` rein informativ, kein Positions-Split; bei mehreren
  OrderInfo-Zeilen alle im Klartext + Mehrdeutigkeits-Kennzeichnung.
- **H1:** Filter praezisieren als `VMBedarf IS NOT NULL AND VMBedarf <> ''` (nvarchar, **kein**
  Sage-Bit — kein `-1`-Vergleich).
- **H3:** Testbarkeits-Vorbedingung und die Reihenfolge-Voraussetzung (Teil 1 produktiv +
  `Sync:HierarchicalFaEnabled`, Teil 3 als Baustein) in den **Deploy-Abschnitt** ziehen, nicht nur
  in den Antwortblock.

## Kritische Pruefung (2026-08-07)

Zweiter Anwalt-des-Teufels-Durchgang, Auftrag: die menschlichen „Antworten auf die Kritische
Pruefung (2026-08-06)" pruefen — (a) vollstaendig/in sich/mit dem Rumpf widerspruchsfrei, (b)
Faktenbehauptungen korrekt, (c) **ist der Rumpf/Frontmatter schon nachgezogen oder noch stale?**
Gegengelesen: diese Teil-5-Spec **komplett**, die Teil-1-Spec (`Neuer_PT_PPS`/`MontageAbteilung`
**nur** auf `FaHierarchyOrderInfo`, 1:n je `HauptFA`; `SubFA = 0` = Blatt; Existenzpruefung statt
Fan-out), die ueberarbeitete Teil-3-Spec inkl. ihres eigenen 2P-Durchgangs (gemeinsamer Baustein,
`SubFA = 0`-Regel, Anomalie-Log, Seiteneinheit `HauptFA`), der Anhang [[sage-views-ideal]]
(`VMBedarf` = nvarchar; `Matchcode` = primaeres Produktions-ID; `Neuer_PT_PPS` auf FAInfos), die
Uebersicht (Querschnitts-Entscheidungen), ADR 0005/0006 sowie **echter main-Code**:
`RequireVorbauAccessAttribute` (existiert, prueft `HasVorbauAccessAsync`) und `FaWorklistController`
(traegt `[RequireVorbauAccess]` **Class-Level**, Zeile 20).

**Was haelt (bewusst bestaetigt, damit klar ist, was NICHT neu aufgerollt wird):**
- **Faktenlage der Antworten stimmt.** `RequireVorbauAccessAttribute` existiert und sitzt
  Class-Level auf `FaWorklistController` — die S2-Behauptung ist am Code bestaetigt (der Filter ist
  ein einzelnes Zugriffs-Gate `HasVorbauAccessAsync`, kein Read/Edit-Split; das Label „Read" ist
  harmlos, weil die Liste ohnehin nur liest). `VMBedarf` = nvarchar (Anhang) → H1 korrekt, kein
  `-1`-Vergleich. `Neuer_PT_PPS` liegt auf der 1:n-`FaHierarchyOrderInfo` (Anhang/Teil 1) → die
  Fan-out-Disziplin aus S3/B2 ist die richtige Konsequenz.
- **Gruppierungs-Aufloesung (B4) ist im Kern tragfaehig:** `VMBedarf` als Reiter-/Filter-Dimension,
  Sicht 1 nach `HauptFA` wie Teil 3/4, Sicht 2 als begruendete Aggregat-Ausnahme. Die Richtung wird
  **nicht** neu aufgerollt — nur ihre Verankerung im Rumpf und zwei Rest-Luecken (siehe unten).

### BLOCKER

**T5-2P-B1 — Die Antworten sind „uebernommen", aber der Rumpf/Frontmatter ist zu 100 % stale:
entschieden, nicht umgesetzt.** Genau der Befund, den der erste Durchgang als T5-B1 markiert hat, ist
**nicht** abgearbeitet — der Antwortblock sagt bei B1/B2/B3/S1/S2/S3/H1/H3 „uebernommen"/„verankern",
aber **kein einziger Absatz oberhalb des Antwortblocks wurde geaendert**. Konkret weiter auf dem
alten Stand (drei Sichten + Export, keine der neuen Regeln):
- **Frontmatter:** `affected_code` fuehrt weiter `wwwroot/js/ideal-vormontage-export.js` (Z. 18) und
  listet **weder** den Toggle-Filter `RequireFaHierarchyVormontageAktivAttribute` **noch** README;
  `open_questions` (Z. 23–26) enthaelt **alle vier** alten Fragen, darunter die bereits
  beantworteten (Datumsfeld = `Neuer_PT_PPS`, Rollen = `RequireVorbauAccess`).
- **Ziel** (Z. 42) „eigener Aggregations-/**Export**-Logik"; **In-Scope** (Z. 46–47) „drei Sichten …
  (3) Export in Zwischenablage"; **Technischer Loesungsentwurf** (Z. 62–64) „Export-Sicht … generiert
  Zwischenablage-Text … `navigator.clipboard`".
- **Fachliche Anforderungen** (Z. 54–58) nennen **nur** „Filter: `VMBedarf` gefuellt" — es fehlen
  `SubFA = 0`-Ebenenregel, Anomalie-Diagnose, Wochenbezug `Neuer_PT_PPS`, Toggle, Access-Filter,
  Fan-out-Disziplin und die Aggregationssemantik komplett.
- **Akzeptanzkriterien** (Z. 76–82): **AK 1** „Alle **drei** Sichten", **AK 3** = Export; es gibt
  **kein** AK fuer Wochenbezug, `SubFA = 0`, Toggle/Access oder Aggregation.
- **Test-Szenarien** (Z. 86–89) und **Offene Rueckfragen** (Z. 99–105) noch Export-/Screenshot-/
  Rollen-/Datumsfeld-orientiert; **Deploy** ohne Testbarkeits-/Reihenfolge-Vorbedingung (H3).
**Verdikt:** „Antworten entschieden, Umsetzung im Rumpf fehlt." Ein Top-down-Dev-Lauf baut weiterhin
das falsche Feature (drei Sichten + Export, ohne Wochenfilter/Ebenenregel). Der gesamte Rumpf +
Frontmatter ist auf **zwei Sichten, kein Export** und auf die neun beschlossenen Punkte zu ziehen,
**bevor** freigegeben wird.

**T5-2P-B2 — Der Wochenbezug bleibt auch im Antwortblock unterspezifiziert — die Kernsemantik des
Features ist nicht entscheidbar.** Ziel/Nutzen ist „in der **kommenden Woche** vorzubereitenden
Teile"; Antwort 4 legt `Neuer_PT_PPS` fest. Antwort B2 sagt aber nur „als Fachliche Anforderung + AK
verankern" und „eigene Abfrage je `HauptFA`, kein Join" — das ist der **Datenpfad**, nicht die
**Semantik**. Drei vom ersten Durchgang gestellte Fragen sind weiterhin **offen**: (a) ist „kommende
Woche" ein **harter Filter** auf die Positionsmenge oder nur eine Anzeige-/Sortierspalte? (b) welche
**Wochengrenze** (ISO-KW Mo–So, rollierende 7 Tage, aktuelle vs. naechste KW)? (c) **Kombigeraet mit
zwei `Neuer_PT_PPS`-Werten** zu einem `HauptFA`: welcher Termin entscheidet die Wochen-Zugehoerigkeit
der Positionen — fruehester, beliebiger Treffer, oder erscheint die Position je passender OrderInfo
(→ neuer Fan-out)? Ohne (a)–(c) ist weder die Fachliche Anforderung noch ein AK formulierbar.
Zusatz: In **Sicht 2** muss der Wochenfilter **vor** der Matchcode-Aggregation auf die Positionen
(ueber ihren `HauptFA`) wirken, sonst summiert die Aggregatzeile Positionen mit unterschiedlicher
Wochen-Eignung zusammen — auch diese Reihenfolge fehlt.

**T5-2P-B3 — Das bestehende AK 2 widerspricht der eigenen Aggregations-Entscheidung (Artikel vs.
Matchcode), und die beschlossene Aggregationssemantik ist als AK nirgends niedergeschrieben.** AK 2
(Z. 78–79) fordert „aggregiert korrekt **nach Artikel**". Antwort S4 beschliesst dagegen
ausdruecklich „Aggregationsschluessel **`Matchcode`** (nicht `Artnr`)". Das ist ein direkter,
sichtbarer Widerspruch im selben Dokument. Zusaetzlich existiert **kein** pruefbares AK fuer die vier
S4-Festlegungen (Schluessel `Matchcode`; zwei getrennte Summen `Sollmenge` **und** `Fertigungmenge`;
Aggregation **je `VMBedarf`-Reiter**; bewusst mehrere Zeilen bei positionsabhaengig abweichendem
Matchcode). Solange das nur im Antwortblock steht, ist die Summierte Sicht **nicht abnahmefaehig**
(Task-Vorgabe „beide Sichten je pruefbar"). AK 2 aufloesen und die S4-Semantik als eigenes AK
niederschreiben.

### SOLLTE

**T5-2P-S1 — Die Anomalie-Behandlung (`SubFA != 0` UND `VMBedarf`) ist erneut nur ein Log —
operator-unsichtbar, und das ist bei einer mengensteuernden Liste ein Betriebsrisiko.** Antwort B3
uebernimmt Teil 3 1:1 („nicht anzeigen, aber protokollieren"). Genau diese Log-only-Loesung hat der
**zweite Durchgang von Teil 3 selbst** als unzureichend markiert (T3-2P-S2: „gar-nicht-ausweisen"
verschwindet still im Serverlog). Fuer die Vormontage — und erst recht fuer die **Summierte Sicht**,
wo eine still verworfene Zeile die angezeigte Menge verfaelscht — gilt dasselbe. **Fix:** Anomalien
zusaetzlich operator-sichtbar machen (Banner/`TempData["WarningMessage"]`: „N Position(en) mit
`VMBedarf` auf Baugruppen-Ebene ausgeschlossen — Datenpflege pruefen"), konsistent zum noch offenen
Teil-3-Fix, nicht nur ins `ILogger`-Log.

**T5-2P-S2 — ADR-0005-Spaltenfilter auf Aggregatzeilen (Sicht 2) ist als Mechanik unterspezifiziert.**
Antwort B4 sagt „Spaltenfilter wirken auf den **aggregierten** Zeilen (Matchcode, Summen), nicht auf
den Rohpositionen" — das ist bewusst **anders** als das Standard-Server-Mode-Muster (das die
Rohspalten filtert). Offen bleibt: eigene `ColumnMap` ueber die Aggregat-Projektion
(Matchcode + zwei Summen); die Reihenfolge Wochenfilter → `SubFA = 0` → `VMBedarf` → aggregieren →
Spaltenfilter → paginieren; und ob `TotalCount` die **nach** Spaltenfilter verbliebenen
Aggregatzeilen zaehlt. Das muss die Spec entweder praezise als Server-Mode-auf-Aggregat spezifizieren
**oder** die Summierte Sicht als begruendete Client-Mode-Ausnahme nach ADR 0005 deklarieren — sonst
baut der Dev-Lauf einen Filter, der ins Leere greift.

**T5-2P-S3 — Teil 5 setzt auf einen „in Teil 3 geschnittenen gemeinsamen Baustein" auf, den es in
der Teil-3-Spec noch nicht gibt.** Der Quer-Absatz und Antwort B4 bauen darauf, Sicht 1 verhalte sich
„exakt wie Teil 3/4 — gleicher Baustein, keine Sonderlogik". Der **zweite Durchgang von Teil 3**
(T3-2P-S3) hat aber festgestellt, dass Teil 3 diese Wiederverwendbarkeit **nirgends** deklariert
(`affected_code` listet einen Teil-3-spezifischen `KommissionierListenService`, keinen gemeinsamen
Baustein) — der Auftrag ist dort **unbehoben**. Damit haengt Teil 5 an einer Zusage, die real noch
nicht existiert. Entweder in Teil 3 den wiederverwendbaren Schnitt tatsaechlich verankern (Reihenfolge
Teil 3 vor Teil 5), oder Teil 5 muss den Baustein selbst schneiden — die Abhaengigkeit darf nicht
stillschweigend vorausgesetzt werden.

### HINWEIS

**T5-2P-H1 — Die Uebersicht widerspricht Sicht 2 und nennt Teil 5 explizit.** Uebersicht Z. 159:
„Seiteneinheit ist **durchgaengig** die Gruppe (`HauptFA`), nicht die Zeile (Teil 2/3/4/5)". Sicht 2
paginiert per Beschluss ueber Aggregatzeilen — direkter Widerspruch. Antwort B4 sieht die
Qualifizierung der Uebersicht vor, aber dieser Edit liegt **ausserhalb dieser Datei** und ist
**nicht** ausgefuehrt (Zeile unveraendert). Beim Nachziehen des Rumpfs die Uebersicht im selben Zug
qualifizieren, sonst erbt jede kuenftige Aggregatliste den Widerspruch.

**T5-2P-H2 — „Sicht 1 verhaelt sich exakt wie Teil 3/4" ist leicht ueberzeichnet.** Die
Gruppierungs-/Paging-Mechanik ist identisch, korrekt — aber Teil 5 setzt **zusaetzlich** eine
Reiter-/Tab-UI je `VMBedarf`-Wert und einen `VMBedarf`-Vorfilter obendrauf, die es in Teil 3/4 nicht
gibt. „Gleicher Baustein plus Reiter-Dimension" ist praezise; „exakt wie Teil 3/4" verdeckt die
Reiter-Ergaenzung.

**T5-2P-H3 — H1-Filter sauber, aber Reiter-Enumeration mitdenken.** `VMBedarf IS NOT NULL AND <> ''`
ist korrekt. Fuer die Reiter-Liste (distinct `VMBedarf`) muss derselbe Ausschluss von NULL/Leerwert
gelten, damit kein leerer „(kein Bereich)"-Reiter entsteht — beim Niederschreiben erwaehnen.

BEREIT ZUR FREIGABE? **NEIN.**

NACHBESSERUNG NOETIG: (1) Rumpf + Frontmatter auf zwei Sichten / kein Export / die neun beschlossenen
Punkte nachziehen — die Antworten sind entschieden, aber im Rumpf **nicht** umgesetzt (T5-2P-B1). (2)
Wochenbezug `Neuer_PT_PPS` semantisch aufloesen: harter Filter vs. Anzeige, Wochengrenze, Kombigeraet-
Mehrdeutigkeit, Reihenfolge vor der Aggregation — dann als Anforderung + AK (T5-2P-B2). (3) AK 2
(Artikel↔Matchcode) aufloesen und die Aggregationssemantik (Matchcode, zwei Summen, je Reiter,
Mehrfachzeilen) als pruefbares AK niederschreiben (T5-2P-B3). Zusaetzlich Anomalie operator-sichtbar
(T5-2P-S1), Aggregat-Spaltenfilter-Mechanik spezifizieren oder Client-Mode-Ausnahme deklarieren
(T5-2P-S2) und die Baustein-Abhaengigkeit zu Teil 3 real absichern (T5-2P-S3). Erst danach freigeben.
