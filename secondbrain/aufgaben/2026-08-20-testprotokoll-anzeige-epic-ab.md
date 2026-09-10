---
typ: notiz
---
# Testprotokoll: IDEAL Anzeige-Epic, Etappen A + B

**Testsystem:** http://idealweb01.ideal.ideal-ake.at:88/
**Umfang:** ausschliesslich die Etappen **A und B** des Anzeige-Epics
[[2026-08-18-fa-liste-hierarchie-anzeige-spec]] — also **nur die Darstellung**.
**Datum:** ____________  **Pruefer:** ____________

## AUSSERHALB dieses Protokolls (noch nicht gebaut)

Nicht pruefen, nicht als Fehler melden — diese Teile existieren noch nicht:
- **Kaskade** „Alle Sub-FAs fertigmelden" auf der Leitstand-Gruppenkopfzeile (Etappe C)
- **Zaehl-Sweep** ausserhalb der Listen — Startseite, Kacheln, `Articles/Info` zeigen weiterhin
  die alten Zahlen (Etappe D)
- **Z3-Sync-Meldung** bei neuem Sub-FA unter fertigem Haupt-FA (Etappe D)
- **Leere Spalten** (Kunde, Werkbank, Beschicht., Termine) — bekannt und dokumentiert in
  [[2026-08-20-materialisierung-fachliche-felder]]. **Kein Fehler dieses Epics.**

## SCHRITT 0 — Zuerst: Laeuft dort ueberhaupt der aktuelle Stand?

**Ohne diesen Punkt ist das ganze Protokoll wertlos.** Etappe B ist frisch fertig; ist auf
`idealweb01` ein aelterer Build publiziert, testest du die Vorversion.

| # | Pruefung | Erwartet | Beobachtet | OK |
|---|---|---|---|---|
| 0.1 | Versionsanzeige der Anwendung | traegt den Stand aus dem Worktree (nach v1.34.0 + Nachtraege) | | |
| 0.2 | FA-Struktur-Ansicht `/FaHierarchy` oeffnen | vorhanden, Tabelle mit Spalten (Tree-Table), nicht der alte div-Baum | | |
| 0.3 | FA-Liste `/ProductionOrders` oeffnen, Master an | Gruppen-Kopfzeilen „HauptFA … · N Sub-FAs" sichtbar | | |

**Wenn 0.2 oder 0.3 fehlschlaegt: abbrechen und zuerst Web publizieren.**

## SCHRITT 1 — Flachmodus: AKE muss unveraendert sein

Das ist die harte Bedingung des gesamten Pakets. **Master auf `false` schalten.**

| # | Pruefung | Erwartet | Beobachtet | OK |
|---|---|---|---|---|
| 1.1 | FA-Liste | keine Gruppen-Kopfzeilen, flache Liste wie frueher | | |
| 1.2 | Spalten | `parent-sub-order-number` nicht sichtbar | | |
| 1.3 | Zaehlzeile | **eine** Zahl wie bisher, nicht „N Auftraege · M Sub-FAs" | | |
| 1.4 | Paginierung | zaehlt Zeilen wie bisher | | |
| 1.5 | Suche nach FA-Nummer | Verhalten unveraendert | | |
| 1.6 | Leitstand: Bulk-Select + Freigabe | unveraendert, **kein** neuer Knopf in der Kopfzeile | | |
| 1.7 | Tracking/Index | zweistufig wie frueher, Bootstrap-Aufklappen | | |
| 1.8 | Picking, FaWorklist, FaCompletion | unveraendert | | |

**Danach Master wieder auf `true` fuer Schritt 2 und 3.**

## SCHRITT 2 — Je View: Gruppierung und Darstellung (Master an)

Fuer **jede** der sechs Views dieselben fuenf Pruefungen. `PickingLeitstand` und `Tracking/Index`
haben zusaetzlich eigene Punkte weiter unten.

| # | Pruefung | Erwartet |
|---|---|---|
| a | Gruppen-Kopfzeile | „HauptFA …" + Anzahl Sub-FAs, aufklappbar (Chevron) |
| b | **Spaltenausrichtung** | Spalten fluchten von der ERSTEN bis zur LETZTEN Gruppe |
| c | Zeilen-FA-Nummer | zeigt die **Sub-FA**-Nummer, nicht mehrfach dieselbe Haupt-Nummer |
| d | Eltern-Spalte | ueber das Zahnrad einblendbar, zeigt den Elternzeiger |
| e | Zaehlzeile | „N Auftraege · M Sub-FAs", Paginierung ueber **Gruppen** |

| View | a | b | c | d | e | Bemerkung |
|---|---|---|---|---|---|---|
| `/ProductionOrders` (FA-Liste) | | | | | | Referenz aus Etappe A |
| `/FaCompletion` | | | | | | |
| `/PickingLeitstand` | | | | | | s. Schritt 4 |
| `/Picking` | | | | | | |
| `/FaWorklist` | | | | | | |
| `/Tracking` (Index) | | | | | | dreistufig, s. Schritt 5 |

## SCHRITT 3 — Querschnitt (an der FA-Liste pruefen, dann stichprobenartig anderswo)

| # | Pruefung | Erwartet | Beobachtet | OK |
|---|---|---|---|---|
| 3.1 | **Suche in ZUGEKLAPPTER Gruppe** — Gruppe zuklappen, Sub-FA-Nummer daraus suchen | Gruppe klappt automatisch auf, Treffer hervorgehoben. **Der wichtigste Punkt des Protokolls** — schlaegt er fehl, faellt es sonst niemandem auf | | |
| 3.2 | Suche nach einer **Haupt**-FA-Nummer | findet die Gruppe | | |
| 3.3 | **Sortieren** per Klick auf einen Spaltenkopf | **alle** Gruppen sortieren sich, nicht nur die erste. Bekannter Vorbehalt — Ergebnis notieren, es entscheidet einen offenen Punkt in Etappe D | | |
| 3.4 | Sortierung: bleiben Zeilen in ihrer Gruppe? | keine Zeile wandert ueber eine Gruppengrenze | | |
| 3.5 | Spaltenfilter (Filterzeile) | schraenkt Zeilen ein; Gruppe bleibt, solange eine Zeile passt | | |
| 3.6 | Spaltenauswahl (Zahnrad) | Spalten aus-/einblenden wirkt in **jeder** Gruppe, nicht nur der ersten | | |
| 3.7 | Blaettern | Seitenwechsel schneidet **keine** Gruppe auseinander | | |
| 3.8 | „Alle auf / alle zu" | wirkt ueber alle Gruppen | | |
| 3.9 | Lesbarkeit Gruppen-Kopfzeile | Text klar lesbar (Kontrast), auch am Terminal | | |

## SCHRITT 4 — PickingLeitstand (heikelste View)

| # | Pruefung | Erwartet | Beobachtet | OK |
|---|---|---|---|---|
| 4.1 | **„Alle auswaehlen" im Tabellenkopf** | erfasst Zeilen in **allen** Gruppen — nicht nur der ersten | | |
| 4.2 | „Alle auswaehlen" bei **zugeklappten** Gruppen | Verhalten notieren: werden unsichtbare Zeilen mitausgewaehlt? **Beides vertretbar, aber es muss erkennbar sein** — `BulkRelease` gibt Auftraege frei | | |
| 4.3 | Auswahl-Zaehler | stimmt mit der tatsaechlichen Auswahl ueberein | | |
| 4.4 | Datumsfilter | wirkt auf Zeilen; Gruppe bleibt, solange eine Zeile passt | | |
| 4.5 | VK-VA-Arbeitsschritt-Filter | dito | | |
| 4.6 | `IsDoneBde`-Zeilen-Toggle | unveraendert, wirkt **nur** auf seine Zeile | | |

## SCHRITT 5 — Tracking/Index (dreistufig)

| # | Pruefung | Erwartet | Beobachtet | OK |
|---|---|---|---|---|
| 5.1 | Ebene 1: HauptFA-Kopf | Fortschritt „fertige / alle Sub-FAs" + Balken | | |
| 5.2 | Ebene 2: Sub-FA-Zeile | Fortschritt „rueckgemeldete / alle AGs" | | |
| 5.3 | Ebene 3: Arbeitsgaenge | aufklappbar unter der Sub-FA | | |
| 5.4 | Chevron auf **beiden** Ebenen | funktioniert unabhaengig | | |
| 5.5 | „Alle auf / alle zu" | wirkt auf beide Ebenen | | |
| 5.6 | **Rueckmelden je Arbeitsgang** | funktioniert wie bisher; Fortschritt aktualisiert sich | | |
| 5.7 | Zuruecknehmen je Arbeitsgang | funktioniert | | |
| 5.8 | FA-Filter im Kopf | findet Sub-FA **und** Haupt-FA | | |
| 5.9 | **Ladezeit** | Seite baut sich zuegig auf — die dritte Ebene wird bewusst eager geladen. Bei spuerbarer Verzoegerung notieren (Lazy-Entscheidung waere zu revidieren) | | |

## Auswertung

**Abbruchkriterien** — bei einem dieser Punkte nicht weitertesten, sondern melden:
- Schritt 0 fehlgeschlagen (falscher Build)
- Irgendein Punkt aus Schritt 1 (AKE-Regression) fehlgeschlagen

**Befunde:**

### Durchgang 1 — 20.08.2026, ueber Browser-Fernsteuerung (Claude)

Geprueft wurden ausschliesslich **lesende** Punkte an `/ProductionOrders` und `/FaHierarchy`.
Zustandsaendernde Schritte (Rueckmelden, Bulk-Select, Master umschalten) wurden **bewusst nicht**
ausgefuehrt.

| # | Punkt | Ergebnis |
|---|---|---|
| 0.1 | Version | **OK** — Fusszeile `v1.35.0`, neuer als v1.34.0 |
| 0.2 | `/FaHierarchy` | **OK** — Tree-Table mit Spalten (Struktur, Matchcode, Arbeitsbereich, Komm.-Ziel, Soll/Fert., SubFA, Status), Spaltenfilter-Zeile, „Alle auf/zu", Legende, Kopfdaten je Gruppe (Montage-Abt., Status, KO-/FE-Termin, AB-Nr.), Knotentyp-Icons. 533 Knoten, 4 Strukturen |
| 0.3 | `/ProductionOrders` gruppiert | **OK** — „4 Auftraege · 130 Sub-FAs (Gruppierung nach HauptFA; Paginierung ueber Gruppen)", Gruppenkopf „HauptFA 1035235 · 39 Sub-FAs" |
| 2c | Zeilen-FA-Nummer | **OK** — zeigt die Sub-FA-Nummer, nicht mehrfach dieselbe Haupt-Nummer |
| 2e | Zaehlzeile | **OK** — Z4-Format wie spezifiziert |
| 3.1 | **Suche in ZUGEKLAPPTER Gruppe** | **OK** — Gruppe 1035235 zugeklappt, Sub-FA `1043425` gesucht: Zeile gefunden, Gruppe offen, Zaehlung „1 Auftraege · 1 Sub-FAs". *Mechanik anders als erwartet:* Der Spaltenfilter ist **server-seitig** (`?colf_order-number=…`), die Seite laedt neu und rendert die Trefferguppe offen — kein clientseitiges Auto-Expand noetig |
| 3.2 | Suche nach HauptFA | **OK** — `1037045` liefert die ganze Gruppe (26 Sub-FAs). Sub-zuerst-dann-Haupt bestaetigt |
| 3.3 | **Sortieren per Spaltenkopf** | **FEHLGESCHLAGEN — siehe B-1** |
| — | Leere Spalten (Kunde, Werkbank, Termine) | erwartungsgemaess leer, **kein Fehler dieses Epics** ([[2026-08-20-materialisierung-fachliche-felder]]) |

### B-1 — Sortierklick blockiert die Seite ueber 30 Sekunden (REPRODUZIERBAR)

**Schwere: hoch.** Ein Klick auf `FA Nr.` laesst den Renderer **zweimal in Folge ueber 30 Sekunden**
nicht reagieren (beide Male CDP-Timeout beim Screenshot); danach ist die Seite wieder bedienbar und
die Sortierung angewandt („FA Nr. ▲").

Datenmenge dabei: **130 Zeilen in 4 Gruppen** — also klein. Die Sortierung laeuft **client-seitig**
(URL bleibt unveraendert, kein Sort-Parameter).

**Vermutliche Ursache — der im OSEON-Code dokumentierte Fallstrick:** verschachtelte
`querySelectorAll`-Aufrufe je Zeile ueber die gesamte Tabelle (dort 14 Gruppen x 99 Sub-Auftraege =
1386 Voll-Scans, „sekundenlanger Hang", geloest mit vorberechneten Maps). Hier vermutlich in
`table-filter.js`, das im gruppierten Modus ueber mehrere `<tbody>` arbeitet.

**Warum das ernst ist:** Am Fertigungsterminal sieht ein halbminuetig eingefrorenes Fenster nach
einem Absturz aus — der Werker klickt erneut, schliesst die Seite oder meldet einen Fehler. Und die
Datenmenge waechst; 130 Sub-FAs sind der heutige Testbestand.

**Gehoert nach Etappe D**, wo der Sortier-Vorbehalt ohnehin als Sammelpunkt steht. Der
OSEON-Loesungsweg (ein Scan, vorberechnete Maps) ist die naheliegende Vorlage.

### Nebenbeobachtung

`Artikelnummer` traegt kein Sortier-Zeichen und reagiert nicht auf Klick — sortierbar sind offenbar
nur einzelne Spalten (`FA Nr.`, `Komm.`). Ob das so gewollt ist, waere zu bestaetigen.

### NICHT geprueft (braucht einen Menschen am Testsystem)

Flachmodus-Regression (Schritt 1, erfordert Umschalten des Masters), Bulk-Select am Leitstand,
Rueckmelden in Tracking, Spaltenausrichtung ueber mehrere Gruppen im Detail, Ladezeit-Empfinden,
Lesbarkeit am Terminal.

**Gesamturteil Durchgang 1:** [x] mit Einschraenkungen — Anzeige, Gruppierung, Zaehlung und Suche
funktionieren wie spezifiziert; **die Sortierung ist unbrauchbar (B-1)**.

## Bezug

[[2026-08-18-fa-liste-hierarchie-anzeige-spec]] · [[2026-08-20-wiedereinstieg-ideal]] ·
[[2026-08-20-materialisierung-fachliche-felder]]
