---
type: spec
title: "IDEAL-Standort Teil 5 — Vormontage-Listen"
slug: 2026-07-29-standort-ideal-teil-5-spec
status: Entwurf
created: 2026-08-06
updated: 2026-08-07
source_backlog: "[[2026-07-29-Standort-IDEAL]]"
depends_on: "[[2026-07-29-standort-ideal-teil-1-spec]], [[2026-07-29-standort-ideal-teil-3-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/FaHierarchyVormontageController.cs (neu, Name provisorisch)
  - IdealAkeWms/Services/VormontageService.cs (neu)
  - IdealAkeWms/Models/ViewModels/FaHierarchyVormontageGruppeViewModel.cs (neu, Sicht 1)
  - IdealAkeWms/Models/ViewModels/FaHierarchyVormontageAggregatViewModel.cs (neu, Sicht 2)
  - IdealAkeWms/Filters/RequireFaHierarchyVormontageAktivAttribute.cs (neu)
  - IdealAkeWms/Views/FaHierarchyVormontage/Index.cshtml (neu, Sicht 1 — Einzelne Teile)
  - IdealAkeWms/Views/FaHierarchyVormontage/Summiert.cshtml (neu, Sicht 2 — Summiert)
  - IdealAkeWms/Models/AppSettingKeys.cs (neuer Key FaHierarchyVormontageAktiv)
  - README.md (AppSettings-Dokumentation, neuer Toggle FaHierarchyVormontageAktiv)
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Wochenbezug `Neuer_PT_PPS` ('kommende Woche'): Datenpfad ist entschieden (eigene Abfrage je HauptFA gegen FaHierarchyOrderInfo, kein Fan-out-Join). Offen bleibt die Filter-Semantik: harter Filter auf die Positionsmenge oder nur Anzeige-/Sortierspalte; Wochengrenze (ISO-KW Montag-Sonntag, rollierendes 7-Tage-Fenster, oder aktuelle vs. naechste Kalenderwoche); welcher Termin bei einem Kombigeraet mit mehreren FaHierarchyOrderInfo-Zeilen zaehlt. Strukturell unabhaengig von der Antwort: in Sicht 2 muss ein etwaiger Wochenfilter VOR der Matchcode-Aggregation auf die Positionen wirken. Ein Loesungsvorschlag des Menschen liegt im Abschnitt 'Entscheidungen zu den Rest-Blockern (2026-08-07)' vor (Filter statt Spalte, ISO-Woche Mo-So, Default naechste KW waehlbar, fruehester Termin bei Kombigeraet + Mehrdeutigkeits-Kennzeichnung, eigener Filterwert 'ohne Termin' fuer Positionen ohne Neuer_PT_PPS) — der zweite Kritische-Pruefungs-Durchgang (T5-2P-B2) bewertet das weiterhin als nicht vollstaendig in eine pruefbare Anforderung/AK uebersetzt, daher hier erneut als offene Rueckfrage gefuehrt, bis der Mensch das ausdruecklich bestaetigt."
  - "Abhaengigkeit vom in Teil 3 angekuendigten 'gemeinsamen Baustein' fuer Sicht 1 (Flag-Filter als Parameter, Kopf-Join ohne Fan-out, Gruppierung/Paging nach HauptFA): Ist dieser Baustein in der Teil-3-Spec tatsaechlich wiederverwendbar geschnitten, oder muss Teil 5 die Mechanik fuer Sicht 1 selbst duplizieren? Laut T5-2P-S3 ist der Baustein in Teil 3 selbst noch nicht verankert (affected_code dort listet nur einen Teil-3-spezifischen KommissionierListenService)."
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

Jeder Vormontage-Arbeitsbereich (Feld `VMBedarf`) bekommt zwei Sichten auf seine vorzubereitenden
Teile: eine Einzelteile-Liste (analog zur Kommissionierliste aus Teil 3) und eine nach `Matchcode`
summierte Mengensicht — auf Basis der IDEAL-Struktur (Teil 1) und mit derselben Ebenen-/
Doppelzaehlungsdisziplin wie Teil 3/4 (`SubFA = 0`). Der urspruenglich vorgesehene Export in die
Isolierfraesen-Software ist **nicht** Teil dieses Spec-Standes (siehe Umfang) — Format-Spezifikation
und Referenz-Screenshot lagen bei der Schranke-1-Runde nicht vor.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** zwei Sichten — (1) **Einzelne Teile**: flache, nach `HauptFA` gruppierte
Positionsliste, mechanisch wie Teil 3 (Blattfilter `SubFA = 0`, Kopf aus `FaHierarchyOrderInfo` je
`HauptFA`, kein Fan-out-Join); (2) **Summiert**: nach `Matchcode` aggregierte Mengensicht je
`VMBedarf`-Reiter. Beide Sichten filtern auf `VMBedarf IS NOT NULL AND VMBedarf <> ''` und bieten
Reiter je vorkommendem `VMBedarf`-Wert.

**Out-of-Scope:** Export in die Zwischenablage/Isolierfraesen-Software — herausgenommen laut
Freigabe-Antwort Runde 1 (Format-Spezifikation und Referenz-Screenshot
`Konzept_Uebertrag_MDE_System.docx` fehlten), Folgearbeit erfasst in
[[2026-08-06-vormontage-isolierfraesen-export]]; keine Rueckmeldefunktion (Teil 8);
`ProductionOrders`/AKE unveraendert; keine Integration in die bestehende `FaWorklist` (separate
Domain laut Uebersicht-Entscheidung B5).

## Fachliche Anforderungen

1. **Filter:** `VMBedarf IS NOT NULL AND VMBedarf <> ''` (nvarchar-Bereichsbezeichnung, kein
   Sage-Bit — kein `-1`-Vergleich). Die Reiter-Liste (distinct `VMBedarf`-Werte) unterliegt demselben
   Ausschluss, damit kein leerer „(kein Bereich)"-Reiter entsteht.

2. **Ebenen-/Doppelzaehlungsregel (verbindlich, analog Teil 3 Fachliche Anforderung 2):**
   Positionen werden ausschliesslich auf Blattebene (`SubFA = 0`) gefuehrt. Eine Zeile mit
   `SubFA != 0` ist ein Verweis auf eine als eigener Sub-FA gefertigte Baugruppe — ihre Bestandteile
   fuehrt der Sub-FA in seinen eigenen Zeilen. Reihenfolge: erst `SubFA = 0`, dann `VMBedarf`.

3. **Anomalie-Diagnose, operator-sichtbar (geschaerft gegenueber Teil 3 — siehe Uebersicht,
   Querschnitts-Regel „Das Web verschickt keine Mails", und T5-2P-S1):** Eine Zeile mit
   `SubFA != 0` UND gesetztem `VMBedarf` wird **nicht** angezeigt, aber (a) als `ILogger`-Warnung
   protokolliert (`HauptFA` + `Position` + `Artnr`) **und** (b) als Banner/
   `TempData["WarningMessage"]` auf der Liste ausgewiesen („N Position(en) mit `VMBedarf` auf
   Baugruppen-Ebene ausgeschlossen — Datenpflege pruefen"). Anders als in der urspruenglichen
   Teil-3-Fassung reicht ein reines Server-Log hier nicht: Eine Vormontage-Liste steuert
   Materialbereitstellung, eine still im Log verschwindende Zeile ist ein Betriebsrisiko.

4. **Gruppierung Sicht 1 ausschliesslich nach `HauptFA`** (Node-Identitaet), kein Fan-out-Join gegen
   `FaHierarchyOrderInfo`.

5. **Fan-out-Disziplin fuer die Kopfspalten:** `FE_Termin`, `MontageAbteilung`, `Prio`,
   `Neuer_PT_PPS`, `Verladetermin_Vsl` liegen alle auf der 1:n-Tabelle `FaHierarchyOrderInfo` und
   werden in einer **eigenen** Abfrage je Seiten-`HauptFA`-Menge geholt (kein
   `INNER JOIN ... ON HauptFA` gegen die Positionen). `MontageAbteilung` ist rein informativ, kein
   Positions-Split. Bei mehreren `FaHierarchyOrderInfo`-Zeilen je `HauptFA` (Kombigeraet) werden alle
   im Klartext aufgefuehrt und die Gruppe als mehrdeutig gekennzeichnet.

6. **Wochenbezug `Neuer_PT_PPS` — Datenpfad entschieden, Filter-Semantik offen (siehe Offene
   Rueckfrage 1).** Der Termin wird — sobald die Semantik feststeht — ueber dieselbe Kopf-Abfrage wie
   Punkt 5 gebunden, niemals ueber einen Join gegen die Positionen. Diese Spec-Fassung setzt den
   Wochenfilter **nicht** um; Fachliche Anforderung und AK folgen, sobald die Rueckfrage beantwortet
   ist.

7. **Aggregation Sicht 2 (Summiert):**
   - Schluessel ist **`Matchcode`**, nicht `Artnr` — der Matchcode identifiziert eindeutig, was der
     Werker holt.
   - Summiert werden **zwei getrennte Mengen**: `Sollmenge` und `Fertigungmenge`, je eigene Spalte.
   - Aggregiert wird **je `VMBedarf`-Reiter**, nicht ueber alle Arbeitsbereiche hinweg.
   - **Bewusst akzeptierte Folge:** Fuehrt derselbe `Artnr` positionsabhaengig unterschiedliche
     `Matchcode`-Werte, entstehen daraus mehrere Aggregatzeilen — das ist gewollt, kein Fehler, und
     im Test nicht als solcher zu melden.

8. **Paging/ADR 0005 fuer beide Sichten:**
   - **Sicht 1** verhaelt sich wie Teil 3: Seiteneinheit ist die `HauptFA`-Gruppe, `TotalCount` zaehlt
     Gruppen, Server-Side-Spaltenfilter (`data-server-column-filter`, `data-col-key` je `<th>`,
     `ColumnFilterHelper.ReadFromQuery`) wirken auf den Positionszeilen vor der Gruppierung.
   - **Sicht 2 ist eine begruendete Ausnahme:** Eine Aggregatzeile gehoert per Definition zu mehreren
     Auftraegen und laesst sich nicht sinnvoll nach `HauptFA` paginieren. Seiteneinheit ist die
     **Aggregatzeile**, `TotalCount` zaehlt Aggregatzeilen, Server-Side-Spaltenfilter wirken auf der
     Aggregat-Projektion (`Matchcode`, `Sollmenge`-Summe, `Fertigungmenge`-Summe) statt auf den
     Rohpositionen.
   - **Bekannte Doku-Nacharbeit (nicht Teil dieser Datei):** Die Uebersicht formuliert „Seiteneinheit
     durchgaengig `HauptFA` (Teil 2/3/4/5)" ohne Einschraenkung — das gilt fuer Positionslisten, nicht
     fuer Sicht 2. Beim naechsten Uebersicht-Update entsprechend qualifizieren (T5-2P-H1).

9. **Zugriff:** `RequireVorbauAccessAttribute` (Class-Level, Read — identisch zur bestehenden
   Vorbau-/Abarbeitungsliste `FaWorklistController`, am Code bestaetigt) **plus** neues Feature-Toggle
   `FaHierarchyVormontageAktiv` (`AppSettingKeys`, Default `false`) via neuen
   `RequireFaHierarchyVormontageAktivAttribute` (Muster 1:1 wie
   `RequireFaHierarchyKommissionierlistenAktivAttribute` aus Teil 3, invertierte Default-aus-
   Semantik). Keine neue Rolle.

## Technischer Loesungsentwurf

`VormontageService` liest ueber `IFaHierarchyNodeRepository`/`IFaHierarchyOrderInfoRepository`
(Teil 1) — nach Moeglichkeit ueber denselben wiederverwendbaren Listen-/Druck-Baustein, den Teil 3
fuer seine Mechanik schneidet (siehe Offene Rueckfrage 2 — dieser Baustein ist in der aktuellen
Teil-3-Fassung noch nicht real verankert; bis dahin ist die folgende Beschreibung eigenstaendig
lauffaehig).

**Sicht 1 — Einzelne Teile** (Schritte wie Teil 3, Fachliche Anforderungen 2–5):
1. Positionen laden: `FaHierarchyNode` mit `SubFA = 0` UND `VMBedarf IS NOT NULL AND VMBedarf <> ''`,
   optional zusaetzlich auf den gewaehlten Reiter-Wert eingeschraenkt. Zeilen mit `SubFA != 0` UND
   gesetztem `VMBedarf` werden gesondert gesammelt, geloggt und in die Banner-Zaehlung aufgenommen
   (Fachliche Anforderung 3), nie in die Liste aufgenommen.
2. Server-Side-Spaltenfilter (ADR 0005) auf den Positionszeilen, vor der Gruppierung.
3. Gruppieren nach `HauptFA`; eine Gruppe, die durch den Spaltenfilter auf 0 Positionen faellt,
   erscheint nicht.
4. Pagination auf Gruppen-Ebene (`PageSize.Resolve` + `PaginationState` + `_Pagination`-Partial),
   `TotalCount` zaehlt Gruppen.
5. Kopfdaten (Fachliche Anforderung 5) nachtraeglich je Seiten-`HauptFA`-Menge holen und im
   ViewModel zuordnen — kein Join gegen die Positionen.
6. Filterkarte mit `VMBedarf`-Reiterwahl und Spaltenfiltern ueber dem Tabellenblock.

**Sicht 2 — Summiert:**
1. Dieselbe Blattfilter-Basis wie Sicht 1 (`SubFA = 0`, `VMBedarf` gesetzt, aktueller Reiter).
2. Sobald der Wochenbezug entschieden ist (Offene Rueckfrage 1): Wochenfilter auf dieser
   Positionsmenge anwenden, **bevor** aggregiert wird.
3. Gruppieren nach `Matchcode`, `Sollmenge` und `Fertigungmenge` je Gruppe summieren.
4. Server-Side-Spaltenfilter auf der Aggregat-Projektion (`Matchcode` + zwei Summenspalten).
5. Pagination auf Aggregatzeilen-Ebene, `TotalCount` zaehlt Aggregatzeilen.

Kein zusaetzliches DTO jenseits zweier schlanker ViewModels
(`FaHierarchyVormontageGruppeViewModel` fuer Sicht 1, analog Teil-3-Gruppen-ViewModel;
`FaHierarchyVormontageAggregatViewModel` fuer Sicht 2: `Matchcode`, `SummeSollmenge`,
`SummeFertigungmenge`, `VMBedarf`).

## Migrations-/SQL-Auswirkungen

Keine — reine Lesefunktion. Der neue AppSetting-Key `FaHierarchyVormontageAktiv` braucht keine
Migration (generische Key-Value-Tabelle `AppSettings`, fehlender Key wird per Code-Default `false`
behandelt).

## Audit-Feld-Auswirkungen

Keine neuen Entitaeten.

## Akzeptanzkriterien

1. Beide Sichten zeigen ausschliesslich Blattpositionen (`SubFA = 0`) mit gesetztem `VMBedarf`
   (`IS NOT NULL AND <> ''`); die Reiter-Liste enthaelt keinen leeren „(kein Bereich)"-Eintrag.
2. **Keine Doppelzaehlung auf beiden Achsen** (Ebenen und Struktur): (a) Zeilen mit `SubFA != 0`
   erscheinen nie — code-pruefbar per synthetischer Fixture (`HauptFA` mit Verweiszeile `SubFA != 0`
   und einem Sub-FA mit eigenen Blattzeilen), analog Teil 3 AK1; (b) bei mehreren
   `FaHierarchyOrderInfo`-Zeilen je `HauptFA` bleibt die Positionsanzahl 1:1 zur Quellzeilenzahl
   (kein Fan-out-Join, keine Mengenvervielfachung).
3. Eine Anomalie-Zeile (`SubFA != 0` UND `VMBedarf` gesetzt) wird nicht angezeigt, aber (a) als
   `ILogger`-Warnung protokolliert **und** (b) als Banner/`WarningMessage` mit Anzahl auf der Liste
   ausgewiesen.
4. Sicht 2 aggregiert nach **`Matchcode`** (nicht `Artnr`) je `VMBedarf`-Reiter, mit zwei getrennten
   Summenspalten `Sollmenge` und `Fertigungmenge`. Fuehrt derselbe `Artnr` positionsabhaengig
   unterschiedliche Matchcodes, entstehen bewusst mehrere Zeilen — im Test nicht als Fehler zu werten.
5. Sicht 1 erfuellt ADR 0005 wie Teil 3 (Seiteneinheit `HauptFA`-Gruppe, `TotalCount` zaehlt Gruppen,
   Server-Spaltenfilter auf Positionszeilen). Sicht 2 erfuellt ADR 0005 als begruendete Ausnahme:
   Seiteneinheit Aggregatzeile, `TotalCount` zaehlt Aggregatzeilen, Spaltenfilter wirken auf
   `Matchcode` + Summenspalten.
6. Zugriff nur mit `RequireVorbauAccessAttribute` **und** aktivem Toggle `FaHierarchyVormontageAktiv`
   (Default `false`); ohne Toggle Redirect + `WarningMessage`, ohne Zugriff Redirect auf
   `AccessDenied`.
7. AKE-Verhalten unveraendert (bestehende Controller/Views unberuehrt).
8. **Nicht Teil dieser Freigabe-Runde:** ein AK zum Wochenbezug-Filter (`Neuer_PT_PPS`) folgt, sobald
   Offene Rueckfrage 1 beantwortet ist.

## Test-Szenarien

Neues Kapitel „IDEAL Teil 5 — Vormontage-Listen" in `docs/TESTSZENARIEN.md`, zweigeteilt analog
Teil 3:

**Automatisiert/InMemory-Fixture:**
- `SubFA = 0`-Filter dedupliziert korrekt (synthetische Fixture wie Teil 3) — AK1/AK2a.
- Kopf-Join ohne Fan-out bei zwei `FaHierarchyOrderInfo`-Zeilen je `HauptFA` — AK2b.
- Anomalie-Zeile wird geloggt, ausgeschlossen **und** im Banner ausgewiesen — AK3.
- Sicht-2-Aggregation nach `Matchcode`: zwei Positionen mit gleichem `Artnr`, aber
  unterschiedlichem `Matchcode`, ergeben zwei Aggregatzeilen; zwei Positionen mit gleichem
  `Matchcode` werden zu einer Zeile mit summierten `Sollmenge`/`Fertigungmenge` — AK4.
- Pagination Sicht 1 zaehlt Gruppen, Pagination Sicht 2 zaehlt Aggregatzeilen — AK5.
- Zugriff ohne Rolle bzw. bei deaktiviertem Toggle → Redirect — AK6.

**Manuell am IDEAL-Testsystem (Vorbedingung: produktivnahe Daten, siehe Deploy-Abschnitt — mit dem
aktuell leeren Testsystem nicht durchfuehrbar):**
- Reiter-Wechsel zwischen `VMBedarf`-Arbeitsbereichen liefert die erwartete Teilmenge in beiden
  Sichten.
- Mengenabgleich Struktur vs. Sicht 2 an einer bekannten mehrstufigen Struktur.
- (Nach Beantwortung von Offene Rueckfrage 1:) Wochenfilter-Verhalten inkl. Kombigeraet-Fall.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen.

## Deploy

- **Web-App:** ja.
- **Service:** nein.
- **Migration:** nein.
- **Voraussetzung/Reihenfolge:** Teil 1 (`FaHierarchyNode`/`FaHierarchyOrderInfo` + Sync) muss
  bereits gemergt sein und mit `Sync:HierarchicalFaEnabled = true` produktiv laufen, sonst bleiben
  beide Sichten dauerhaft leer. Sicht 1 setzt zusaetzlich auf den in Teil 3 vorgesehenen
  wiederverwendbaren Listen-/Druck-Baustein auf — dieser ist laut Offener Rueckfrage 2 in der
  aktuellen Teil-3-Fassung noch nicht real geschnitten; Reihenfolge Teil 3 vor Teil 5 empfohlen. Der
  neue Toggle `FaHierarchyVormontageAktiv` bleibt nach dem Deploy default **aus**.
- **Schranke-2-Vorbedingung:** Manual-UAT der IDEAL-spezifischen Szenarien (Reiter-Wechsel,
  Mengenabgleich) kann erst gruen werden, wenn produktivnahe IDEAL-Daten mit gesetztem `VMBedarf`
  im Testsystem liegen — das aktuell leere Testsystem reicht nicht. Dem qa-agent explizit als
  Vorbedingung zu melden.
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o
  .\publish\IDEALAKEWMSWeb` (provisorisch).

## Offene Rueckfragen

1. Wochenbezug `Neuer_PT_PPS` ("kommende Woche"): Datenpfad ist entschieden (eigene Abfrage je
   `HauptFA` gegen `FaHierarchyOrderInfo`, kein Fan-out-Join). Offen bleibt die Filter-Semantik:
   harter Filter auf die Positionsmenge oder nur Anzeige-/Sortierspalte; Wochengrenze (ISO-KW
   Montag–Sonntag, rollierendes 7-Tage-Fenster, oder aktuelle vs. naechste Kalenderwoche); welcher
   Termin bei einem Kombigeraet mit mehreren `FaHierarchyOrderInfo`-Zeilen zaehlt. Strukturell
   unabhaengig von der Antwort: In Sicht 2 muss ein etwaiger Wochenfilter **vor** der
   Matchcode-Aggregation auf die Positionen wirken. Ein Loesungsvorschlag liegt bereits im Abschnitt
   „Entscheidungen zu den Rest-Blockern (2026-08-07)" vor (Filter statt Spalte, ISO-Woche Mo–So,
   Default naechste KW waehlbar, fruehester Termin bei Kombigeraet + Mehrdeutigkeits-Kennzeichnung,
   eigener Filterwert „ohne Termin") — der zweite Kritische-Pruefungs-Durchgang (T5-2P-B2) haelt das
   weiterhin fuer nicht vollstaendig in eine pruefbare Anforderung/AK uebersetzt, daher hier erneut
   als offene Rueckfrage gefuehrt, bis der Mensch das ausdruecklich bestaetigt.
2. Abhaengigkeit vom in Teil 3 angekuendigten „gemeinsamen Baustein" fuer Sicht 1 (Flag-Filter als
   Parameter, Kopf-Join ohne Fan-out, Gruppierung/Paging nach `HauptFA`): Ist dieser Baustein in der
   Teil-3-Spec tatsaechlich wiederverwendbar geschnitten, oder muss Teil 5 die Mechanik fuer Sicht 1
   selbst duplizieren? Laut T5-2P-S3 ist der Baustein in Teil 3 selbst noch nicht verankert
   (`affected_code` dort listet nur einen Teil-3-spezifischen `KommissionierListenService`).

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. →
2. →

## Freigabe-Antworten — Runde 1 (2026-08-06, beantwortet)

> Historischer Erst-Durchlauf vor der Kritischen Pruefung. Die Antworten sind in Umfang, Fachliche
> Anforderungen, Loesungsentwurf und Akzeptanzkriterien oben eingearbeitet.

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

## Entscheidungen zu den Rest-Blockern (2026-08-07)

**AK 2 wird korrigiert: Aggregationsschluessel ist `Matchcode`, nicht „Artikel". [KORREKTUR]**
AK 2 sagt heute „aggregiert nach Artikel" und widerspricht damit der S4-Antwort. Massgeblich ist
der **Matchcode** — AK 2 entsprechend umschreiben. Konsequenz explizit mitschreiben: Fuehrt derselbe
`Artnr` positionsabhaengig verschiedene Matchcodes, entstehen daraus **mehrere** Zeilen. Das ist
gewollt und darf im Test nicht als Fehler gemeldet werden.

**Wochenbezug `Neuer_PT_PPS`: Filter, nicht Spalte — und waehlbar. [ENTSCHEIDUNG]**
- **Filter, nicht Anzeige-Spalte.** „Kommende Woche" schraenkt die Menge ein. Das Datum darf
  zusaetzlich als Spalte erscheinen, aber der Wochenbezug ist ein Filter.
- **Wochengrenze: ISO-Woche, Montag bis Sonntag** (`ISO 8601`, wie sonst im Haus). Keine
  rollierenden „naechste 7 Tage" — die Vormontage plant in Kalenderwochen, nicht in Zeitfenstern.
- **Waehlbar statt fest.** Vorbelegt ist die **naechste** ISO-Woche relativ zu heute; die Woche ist
  aber ueber die Filterkarte verstellbar (Wochenauswahl bzw. vor/zurueck). Begruendung: Eine
  Vormontage-Liste wird vorausgeplant — spaetestens in der zweiten Woche will jemand „KW+2" sehen,
  und ein hartkodierter Filter zwingt ihn dann zu einer Nachforderung.
- **Positionen ohne `Neuer_PT_PPS`** (NULL/leer) erscheinen **nicht** in einer Wochensicht — sie
  haben keinen Termin, also keine Woche. Sie gehen nicht verloren: Ein eigener Filterwert „ohne
  Termin" macht sie sichtbar, damit ungeplante Positionen auffallen statt zu verschwinden.
- **Kombigeraet / mehrere OrderInfo-Zeilen zu einem `HauptFA`:** Es gilt der **frueheste**
  `Neuer_PT_PPS` (die Arbeit muss zum fruehesten Termin fertig sein). Weichen die Termine der
  Kopfzeilen voneinander ab, wird die Gruppe wie ueberall im Paket als **mehrdeutig gekennzeichnet**
  und protokolliert — nicht stillschweigend auf einen Wert reduziert.

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

### Nachbesserung 2 (2026-08-07)

Status je Befund aus dem zweiten Kritische-Pruefung-Durchgang — Details in Umfang, Fachlichen
Anforderungen, Loesungsentwurf, Akzeptanzkriterien und Test-Szenarien oben, hier nur die Kurzfassung:

- **T5-2P-B1 (Rumpf/Frontmatter 100 % stale) — BEHOBEN.** Rumpf auf zwei Sichten gezogen: Ziel,
  Umfang, Fachliche Anforderungen, Loesungsentwurf, Akzeptanzkriterien, Test-Szenarien und
  `affected_code` sind auf „zwei Sichten, kein Export" umgestellt; `ideal-vormontage-export.js` ist
  aus `affected_code` entfernt; alle neun in der Antwort/Entscheidung beschlossenen Punkte
  (`SubFA = 0`-Regel + operator-sichtbares Banner, Toggle, Access-Filter, Fan-out-Disziplin,
  Matchcode-Aggregation, Gruppen-/Aggregat-Paging) sind niedergeschrieben. `open_questions` ist auf
  die zwei tatsaechlich verbleibenden Punkte gekuerzt — die Fragen zu Rollen und massgeblichem
  Datumsfeld waren beantwortet und entfallen.
- **T5-2P-B2 (Wochenbezug bleibt unterspezifiziert) — BEWUSST NICHT AUFGELOEST, als Offene
  Rueckfrage 1 gefuehrt.** Der Datenpfad ist in Fachliche Anforderung 6 fixiert (eigene Abfrage je
  `HauptFA`, kein Join); die Filter-Semantik selbst (hart/Anzeige, Wochengrenze, Kombigeraet,
  Reihenfolge vor der Aggregation in Sicht 2) bleibt ausdruecklich offen, obwohl im Abschnitt
  „Entscheidungen zu den Rest-Blockern (2026-08-07)" bereits ein Loesungsvorschlag vorliegt — der
  zweite Review haelt diesen Vorschlag fuer nicht vollstaendig in eine pruefbare Anforderung/AK
  uebersetzt. Kein AK behauptet daher ein Wochenfilter-Verhalten (AK 8 markiert das explizit als
  nachgereicht, sobald die Rueckfrage beantwortet ist).
- **T5-2P-B3 (AK 2 widerspricht Aggregations-Entscheidung) — BEHOBEN.** Das Aggregations-AK (jetzt
  Nummer 4) fordert „aggregiert nach `Matchcode`" statt „nach Artikel", inklusive der vier
  S4-Festlegungen (Schluessel, zwei getrennte Summenspalten, Aggregation je Reiter, bewusste
  Mehrfachzeilen bei abweichendem Matchcode) als pruefbarer Text.
- **T5-2P-S1 (Anomalie nur Log) — BEHOBEN.** Die Anomalie-Behandlung (Fachliche Anforderung 3,
  AK 3) ist jetzt zweigleisig: `ILogger`-Warnung **und** Banner/`TempData["WarningMessage"]` —
  konsistent zur Uebersichts-Querschnittsregel „Das Web verschickt keine Mails, aber ein sichtbares
  Signal".
- **T5-2P-S2 (Aggregat-Spaltenfilter unterspezifiziert) — BEHOBEN.** Sicht 2 ist explizit als
  Server-Mode-auf-Aggregat spezifiziert (Fachliche Anforderung 8, Loesungsentwurf Sicht 2): eigene
  Projektion `Matchcode` + zwei Summenspalten, `TotalCount` zaehlt Aggregatzeilen, Reihenfolge
  Blattfilter → `VMBedarf` → (Wochenfilter, sobald entschieden) → aggregieren → Spaltenfilter →
  paginieren.
- **T5-2P-S3 (Baustein-Abhaengigkeit zu Teil 3 nicht real) — NICHT AUFLOESBAR durch diese Spec, als
  Offene Rueckfrage 2 dokumentiert.** Teil 5 kann den Baustein nicht selbst in Teil 3 verankern; die
  Abhaengigkeit ist im Deploy-Abschnitt als Reihenfolge-Empfehlung (Teil 3 vor Teil 5) und als offene
  Rueckfrage vermerkt, statt stillschweigend vorausgesetzt zu werden.
- **T5-2P-H1 (Uebersicht widerspricht Sicht 2) — als Doku-Nacharbeit vermerkt, nicht in dieser Datei
  behoben.** Die Uebersicht liegt ausserhalb des Schreibauftrags dieser Runde (separate Datei);
  Fachliche Anforderung 8 haelt fest, dass die Uebersichts-Aussage „Seiteneinheit durchgaengig
  `HauptFA`" beim naechsten Uebersicht-Update fuer Aggregatsichten zu qualifizieren ist.
- **T5-2P-H2 (Sicht 1 „exakt wie Teil 3/4" ueberzeichnet) — praezisiert.** Fachliche Anforderungen
  benennen jetzt zusaetzlich zur `HauptFA`-Mechanik die `VMBedarf`-Reiter-/Vorfilter-Dimension, die
  Teil 3/4 nicht kennen.
- **T5-2P-H3 (Reiter-Enumeration NULL/Leer) — BEHOBEN.** Fachliche Anforderung 1 verlangt denselben
  Ausschluss fuer die Reiter-Liste, AK 1 prueft das explizit.

BEREIT ZUR FREIGABE? **NEIN.** Offene Rueckfrage 1 (Wochenbezug-Semantik) und Offene Rueckfrage 2
(Baustein-Abhaengigkeit zu Teil 3) sind vom Menschen zu beantworten, bevor Schranke 1 passiert werden
kann. Rumpf und Frontmatter sind ab diesem Stand nicht mehr stale — ein Dev-Lauf, der von oben nach
unten liest, baut jetzt die zwei tatsaechlich beschlossenen Sichten korrekt (bis auf den bewusst
zurueckgestellten Wochenfilter).
