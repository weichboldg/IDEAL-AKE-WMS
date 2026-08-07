---
type: spec
title: "IDEAL-Standort Teil 8 — Sub-FA-Rueckmeldung / BDE (Epic)"
slug: 2026-07-29-standort-ideal-teil-8-spec
status: Entwurf
created: 2026-08-06
updated: 2026-08-06
source_backlog: "[[2026-07-29-Standort-IDEAL]]"
depends_on: "[[2026-07-29-standort-ideal-teil-7-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/BdeTerminalController.cs
  - IdealAkeWms/Controllers/BdeApiController.cs
  - IdealAkeWms/Services/BdeBookingService.cs
  - IdealAkeWms/wwwroot/js/barcode-scanner.js
  - IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs
  - IdealAkeWms/Controllers/TrackingController.cs
  - IDEALAKEWMSService/Services/OseonSyncService.cs (Review OrderNumber-Bezug)
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "B2/B-5 (fundamental, muss VOR Etappe 1 entschieden sein): Scan liefert HauptFA=OrderNumber, nach Inversion nicht eindeutig -> identifiziert eine Gruppe, keinen Einzelauftrag. Wird auf Haupt-FA-Ebene rueckgemeldet, oder waehlt der Werker aus den angezeigten Sub-FAs den konkreten SubOrderNumber?"
  - "Setzt diese Etappenfolge tatsaechlich Teil 7 vollstaendig abgeschlossen voraus, oder kann (laut Notiz 'Alternative Reihenfolge') 7/8 vorgezogen werden, wenn Rueckmeldefaehigkeit von Tag eins gebraucht wird? Wirkt sich auf die Startbedingung dieses Epics aus."
  - "Teileverfolgung/OSEON: ist OseonSyncService bereits SubOrderNumber-fest, oder braucht dieser Teil eine eigene Etappe fuer die OSEON-Seite?"
epic: true
etappen:
  - "1: Rueckmelde-Datenmodell (Review/Erweiterung bestehender Satelliten ProductionOrderBdeStatus etc. auf SubOrderNumber-Bezug, keine neue Migration erwartet ausser Bedarf)"
  - "2: Repositories/Services auf SubOrderNumber-Lookups haerten (Fortsetzung des Teil-7-Reviews im BDE-Kontext)"
  - "3: Rueckmelde-Logik (BdeBookingService, ProductionOrdersController) fuer materialisierte Sub-FAs verifizieren/anpassen"
  - "4: BDE-Anbindung inkl. Scan-Aufloesung gemaess Klaerung von B2/B-5 (Gruppen- vs. Einzelauswahl)"
  - "5: Teileverfolgung/OSEON-Seite (falls Etappe-3-Review Anpassungsbedarf zeigt)"
  - "6: Tests (Unit + Test-Szenarien) + Brain-Update"
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Weil die Sub-FAs nach Teil 7 echte `ProductionOrders` sind (mit eindeutiger `SubOrderNumber`),
greifen Arbeitsgaenge, Teileverfolgung und Rueckmeldung **grundsaetzlich unveraendert** — sie
kennen bereits eine `ProductionOrder`-Entitaet und muessen „nur" gegen die neue
Nicht-Eindeutigkeit von `OrderNumber` gehaertet werden, wo sie bislang stillschweigend
Eindeutigkeit annahmen. Dieser Teil macht Sub-FAs am Terminal (BDE) und in der Teileverfolgung
tatsaechlich bedienbar — vorher wurde nur strukturiert und materialisiert.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** BDE-Terminal-Buchung gegen materialisierte Sub-FAs, Scan-Aufloesung (Werker scannt
`HauptFA`, muss aber ggf. einen konkreten `SubOrderNumber` waehlen — siehe B2/B-5), Haertung aller
BDE-/Teileverfolgungs-Lookups auf `SubOrderNumber`, wo Eindeutigkeit gebraucht wird.

**Out-of-Scope:** die Materialisierung selbst (Teil 7, Voraussetzung); keine neue
BDE-Fachlogik jenseits dessen, was die Hierarchie erzwingt (bestehende Buchungsregeln,
Mehrfachbuchungs-Konfiguration etc. bleiben unveraendert).

## Fachliche Anforderungen

- **B2/B-5-Konsequenz (siehe offene Rueckfrage 1, entscheidend fuer die gesamte Etappenfolge):**
  `HauptFA` ist laut Anhang der einzige Produktions-Identifier — auch wenn ein Werker einen
  Sub-FA scannt, muss intern auf `HauptFA` aufgeloest werden. Nach der Inversion identifiziert ein
  Scan damit eine **Gruppe** (alle Sub-FAs einer `OrderNumber`), keinen Einzelauftrag. Die BDE-
  Buchung braucht aber einen eindeutigen `SubOrderNumber`. Es muss entschieden werden, ob (a) auf
  Haupt-FA-Ebene ruckgemeldet wird (dann muesste `BdeBooking` ggf. auf `OrderNumber`-Gruppen statt
  Einzelauftraegen buchen — Modelbruch) oder (b) der Werker nach dem Scan aus den angezeigten
  Sub-FAs den konkreten `SubOrderNumber` waehlt (naeher am bestehenden Modell, aber ein
  zusaetzlicher Bedienschritt am Terminal).
- Alle bestehenden BDE-Fallstricke (Mehrfachbuchungs-Regel, `Paused`/`EndedAt`, BDE-Sperre bei
  verpackt/abgeholt, Auto-Pause-Schichtende) gelten unveraendert je `SubOrderNumber`-Auftrag.

## Technischer Loesungsentwurf

Da `ProductionOrders` nach Teil 7 bereits das materialisierte Sub-FA-Modell traegt, ist dieser
Teil primaer ein **Haertungs- und Anbindungs-Epic**, kein Neubau: bestehende BDE-Services
(`BdeBookingService`, `BdeTerminalController`) werden gegen die konkrete Sub-FA-Auswahl aus der
UI verdrahtet; der Scan-Handler (`barcode-scanner.js`) bekommt — abhaengig vom Ergebnis der
B2/B-5-Klaerung — entweder eine Gruppen-Zwischenansicht oder eine direkte Sub-FA-Aufloesung.

## Migrations-/SQL-Auswirkungen

Voraussichtlich **keine grosse Migration** — die Satelliten (`ProductionOrderBdeStatus` etc.)
haengen bereits per FK an `ProductionOrder.Id`, nicht an `OrderNumber`, und funktionieren daher
strukturell unveraendert. Falls im Zuge einer Etappe doch ein Datenmodell-Zusatz noetig wird
(z. B. ein UI-Statusfeld fuer die Gruppen-/Einzelauswahl), erhaelt er eine eigene idempotente
`SQL/XX_*.sql`-Datei nach ADR 0004 — Nummer zum jeweiligen Etappen-Zeitpunkt neu zu ermitteln
(voraussichtlich ab `SQL/88`, abhaengig vom Stand von Teil 1/7 zum Umsetzungszeitpunkt).

## Audit-Feld-Auswirkungen

Keine Aenderung an bestehenden Audit-Feldern; `BdeBooking` bleibt wie heute protokolliert.

## Akzeptanzkriterien

1. Ein Werker kann am Terminal einen materialisierten Sub-FA eindeutig buchen (kein
   Ambiguitaets-Fehler, keine falsche Zuordnung bei gleicher `OrderNumber`).
2. Scan-Aufloesung verhaelt sich gemaess der getroffenen B2/B-5-Entscheidung konsistent ueber alle
   Scan-Einstiegspunkte (Terminal, Teileverfolgung, Kommissionierung — soweit betroffen).
3. Bestehende BDE-Regeln (Mehrfachbuchung, Pause, Sperre bei verpackt/abgeholt) funktionieren
   unveraendert je Sub-FA.
4. AKE-Verhalten (Master aus) unveraendert.
5. Jede Etappe endet in einem eigenstaendigen, buildbaren Commit (kein Zwischenzustand, der
   `dotnet build`/`dotnet test` bricht).

## Test-Szenarien

Neues Kapitel „IDEAL Teil 8 — Sub-FA-BDE": Terminal-Buchung auf zwei Sub-FAs derselben
`OrderNumber` nacheinander — beide korrekt getrennt erfasst; Scan-Aufloesung je nach
B2/B-5-Entscheidung; Regressionslauf der bestehenden BDE-Testszenarien (Mehrfachbuchung, Pause,
Sperre) gegen materialisierte Sub-FAs.

## Etappen (epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| 1 | Rueckmelde-Datenmodell: Review/ggf. Erweiterung bestehender Satelliten auf Sub-FA-Bezug | offen | |
| 2 | Repositories/Services auf `SubOrderNumber`-Lookups haerten | offen | |
| 3 | Rueckmelde-Logik (`BdeBookingService`, `ProductionOrdersController`) fuer materialisierte Sub-FAs verifizieren/anpassen | offen | |
| 4 | BDE-Anbindung inkl. Scan-Aufloesung gemaess B2/B-5-Entscheidung | offen | |
| 5 | Teileverfolgung/OSEON-Seite (nur falls Etappe 2/3 Anpassungsbedarf zeigen) | offen | |
| 6 | Tests (Unit + Test-Szenarien) + Brain-Update | offen | |

Ein langlebiger Worktree traegt alle Etappen; **kein** Zwischen-Merge. Waehrend der Arbeit den
Branch regelmaessig mit `scripts/sync-worktree.ps1 -Slug <slug>` auf `main`-Stand halten. QA und
Merge (Schranke 2) erst, wenn **alle** Etappen abgeschlossen sind.

## Deploy

- **Web-App:** ja.
- **Service:** ja (falls Etappe 5 OSEON-Anpassungen bringt).
- **Migration:** wahrscheinlich, Umfang haengt von den Etappen ab (siehe „Migrations-/SQL-
  Auswirkungen").
- **Publish-Befehle:** wie Teil 7, vom Dev-Lauf am Ende aller Etappen gegen den tatsaechlichen
  Gesamt-Diff zu bestaetigen.

## Offene Rueckfragen

1. B2/B-5 — Scan-Aufloesung: Haupt-FA-Ebene oder Werker-Auswahl aus den Sub-FAs? Muss **vor**
   Etappe 4 (idealerweise vor Etappe 1) entschieden sein, weil es das Datenmodell fuer die
   BDE-Anbindung praegt.
2. Bleibt die Voraussetzung „Teil 7 vollstaendig abgeschlossen" bestehen, oder wird laut der in
   der Notiz genannten „Alternativen Reihenfolge" vorgezogen?
3. Ist die OSEON-Seite (`OseonSyncService`) bereits `SubOrderNumber`-fest, oder braucht es dafuer
   eine eigene Etappe?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. → **Variante (b): Der Werker waehlt nach dem Scan den konkreten Sub-FA. Aber
   code-getrieben, nicht annahmegetrieben.**

   **Variante (a) — Rueckmeldung auf Haupt-FA-Ebene — wird abgelehnt**, aus zwei Gruenden:
   `BdeBooking` haengt per FK an `ProductionOrder.Id`; eine Buchung "auf eine Gruppe" hat schlicht
   kein Ziel — das waere ein Modellbruch mitten im Kern. Und fachlich: Wenn nur auf Haupt-FA-Ebene
   rueckgemeldet wird, ist die ganze Materialisierung der Sub-FAs (Teil 7) zwecklos. Man wuerde das
   Datenmodell umbauen und die Information dann wegwerfen.

   **Verbindliche Aufloesungs-Reihenfolge nach einem Scan:**
   1. Der gescannte Code wird zuerst gegen **`SubOrderNumber`** aufgeloest. Trifft er → direkt
      buchen, **keine Auswahl**. (Deckt den Fall ab, dass es tatsaechlich Sub-FA-Barcodes gibt —
      siehe Annahme B2, beim ersten Test zu pruefen.)
   2. Trifft er nicht, wird gegen **`OrderNumber`** aufgeloest:
      - **genau ein** zugehoeriger Auftrag → direkt buchen, keine Auswahl (haelt den flachen bzw.
        einstufigen Fall reibungslos)
      - **mehrere** → Auswahlliste der Sub-FAs dieses `HauptFA`
   3. Kein Treffer → bestehende Fehlerbehandlung, unveraendert.

   **Die Auswahlliste muss unterscheidbar sein**, sonst ist sie am Terminal wertlos: mindestens
   Sub-FA-Nummer, Matchcode/Bezeichnung und der Arbeitsbereich; sinnvoll ist, die Sub-FAs mit einem
   offenen Arbeitsgang im Bereich des Werkers **vorzusortieren oder hervorzuheben**. Reine Nummern
   nebeneinander sind am Terminal nicht bedienbar.

   **Warum diese Reihenfolge und nicht eine feste Annahme:** Ob IDEAL Sub-FA-Barcodes hat, ist bis
   heute unbestaetigt (Testsystem leer). Die code-getriebene Aufloesung funktioniert in **beiden**
   Welten — mit Sub-FA-Barcodes ohne Zusatzschritt, ohne sie mit Auswahl. Damit haengt die
   Umsetzung nicht mehr an einer offenen Annahme, und Etappe 1 kann starten.

2. → **Ja, Teil 7 bleibt vollstaendige Voraussetzung.** `depends_on` unveraendert. Die "alternative
   Reihenfolge" der Ideen-Notiz galt fuer den Fall, dass Rueckmeldefaehigkeit von Tag eins gebraucht
   wird — die risiko-aufsteigende Reihenfolge ist inzwischen entschieden (Uebersichts-Spec), also
   greift sie nicht. Vor Teil 7 gibt es keine materialisierten Sub-FAs, an denen dieses Epic
   arbeiten koennte.

3. → **Offen lassen — aber als Ergebnis von Etappe 2, nicht als Vorbedingung.**
   Ob `OseonSyncService` bereits `SubOrderNumber`-fest ist, laesst sich ohne den Code-Review nicht
   beantworten und soll die Freigabe nicht blockieren. Verbindlich:
   - Die `OrderNumber`-Bezuege in `OseonSyncService` sind Teil des Haertungs-Reviews in **Etappe 2**.
   - **Etappe 2 liefert ein ausdrueckliches Urteil**: OSEON ist bereits fest (dann entfaellt Etappe 5)
     oder nicht (dann wird Etappe 5 mit konkretem Umfang gefuellt).
   - Etappe 5 bleibt bis dahin als **bedingte** Etappe in der Tabelle stehen — nicht streichen,
     nicht blind einplanen.

## Kritische Pruefung (2026-08-06)

Anwalt-des-Teufels-Durchsicht vor Schranke 1. Gegengelesen: diese Spec inkl. aller drei
Freigabe-Antworten, die Teil-7-Spec (Schema-Inversion, `SubOrderNumber` unique, Regel „eindeutige
Lookups → SubOrderNumber"), die Uebersichts-Spec, die Ideen-Notiz (B2/B3) und der Anhang
[[sage-views-ideal]] — **plus der reale main-Code** des BDE-/Scan-/Rueckmelde-Pfads
(`BdeApiController`, `BdeBookingService`, `WorkOperationRepository`, `bde-terminal.js`,
`barcode-scanner.js`, `ProductionOrder`/`BdeBooking`-Modelle, `ApplicationDbContext`).

**Vorab bestaetigt (Staerken):** Die B2-Kernfrage (Scan = Gruppe, nie Einzel-Sub-FA) ist durch
Freigabe-Antwort 1 **sauber geloest** — code-getriebene Aufloesungsreihenfolge (SubOrderNumber →
OrderNumber-Gruppe → Auswahlliste) statt fester Annahme, ablehnung von Variante (a) mit korrekter
Begruendung (BdeBooking haengt per Id, „Buchung auf eine Gruppe" hat kein Ziel). Die drei
Freigabe-Antworten sind untereinander **widerspruchsfrei und vollstaendig** (decken die drei
Frontmatter-`open_questions` 1:1 ab). Der Id-basierte Kern haelt: `BdeBooking → WorkOperation →
ProductionOrder` laeuft ausschliesslich ueber `Id`; alle `OrderNumber`-Vorkommen in
`BdeBookingService`/`BdeApiController`/`BdeTerminalController` sind Anzeige-/Meldungsprojektionen,
keine Lookups. Insofern greift „grundsaetzlich unveraendert" fuer den FK-Pfad tatsaechlich.

### BLOCKER — vor der Freigabe zu klaeren

**KP-1 — `affected_code` zeigt am eigentlichen Scan-Aufloesungspunkt vorbei.**
Der reale, einzige eindeutigkeitsannehmende Scan-Lookup im BDE-Pfad ist
`WorkOperationRepository.GetByFaAndOperationAsync(faNumber, operationNumber)` —
`FirstOrDefaultAsync(w => w.ProductionOrder.OrderNumber == faNumber && w.OperationNumber ==
operationNumber)`, aufgerufen aus `BdeApiController.GetWorkOperation` (Route
`/api/bde/workoperation`), angestossen vom **Terminal-eigenen** Scan-Handler
`bde-terminal.js:scanFaAgInput`. **Genau diese drei Stellen stehen NICHT in `affected_code`.**
Stattdessen listet die Spec `barcode-scanner.js` — das ist der Artikel-/Lagerplatz-Scanner
(`initScanner('btnScanArticle'...)`), **nicht** der BDE-Terminal-Pfad. `WorkOperationRepository`
fehlt komplett. Ein Epic ueber Scan-Aufloesung, das die Datei mit der Scan-Aufloesung nicht nennt,
leitet den Dev an den falschen Naht. `affected_code` vor Freigabe korrigieren: mindestens
`WorkOperationRepository.GetByFaAndOperationAsync`, `BdeApiController.GetWorkOperation`,
`bde-terminal.js` aufnehmen; `barcode-scanner.js` nur, falls dessen `productionOrder`-Pfad wirklich
betroffen ist (heute nicht).

**KP-2 — Aufloesungs-Schritt 1 (Treffer auf `SubOrderNumber`) hat keine Eingabequelle.**
Freigabe-Antwort 1 Schritt 1 sagt: „gescannter Code wird zuerst gegen `SubOrderNumber` aufgeloest".
Der reale Scan liefert aber nur **`HauptFA` = OrderNumber**: `bde-terminal.js` splittet auf `,`/`/`
in `[fa, op]`; `barcode-scanner.js` nimmt bei `productionOrder` **Index 2 = FA-Nummer** (Codekommentar
woertlich „FA-Nummer", als OrderNumber-Text behandelt, **kein BelID-Begriff im Client**). Die
Teil-7-Behauptung „QR traegt an Index 2 die BelID = SubOrderNumber" ist damit **durch den Code
widerlegt** — Index 2 ist die HauptFA, nicht die BelID. Folge: Schritt 1 feuert nie ueber den
QR-Pfad, solange das QR-Format die BelID nicht zusaetzlich traegt (und der Anhang B2 sagt
ausdruecklich, `SubFA` werde in Barcodes **nie** verwendet). Die Spec muss festlegen, **welchen
String** die Aufloesung konsumiert und ob Schritt 1 real erreichbar ist — sonst ist die
Aufloesungsreihenfolge nur auf dem Papier code-getrieben. (Dieser Punkt gehoert mit Teil 7
abgestimmt, dessen Index-2-Behauptung falsch ist.)

### SOLLTE — sichert die spaeteren Dev-/Etappen-Laeufe

**KP-3 — Etappen 1–3 ueberlappen die Teil-7-Pflichthaertung; die Grenze ist undefiniert.**
Teil 7 hat in-scope „Durchzug von `SubOrderNumber` durch Repositories/Controller/Scan-Lookups" und
bewertet als AK 8 **jede** der 14 `OrderNumber`-Fundstellen — darunter `WorkOperationRepository`
(= der Scan-Break-Punkt aus KP-1). Teil-8-Etappe 2 heisst „Repositories/Services auf
`SubOrderNumber` haerten (Fortsetzung des Teil-7-Reviews im BDE-Kontext)". Wenn Teil 7 seine
Pflicht erfuellt, ist die Haertung dieser Lookups schon dort erledigt — was bleibt fuer Etappe 2/3
konkret uebrig ausser der neuen **Auswahl-UI** (Etappe 4)? Die Grenze „Teil 7 flaggt/haertet die
generischen Lookups, Teil 8 baut die BDE-Disambiguierungs-UI" muss **explizit** stehen, sonst faellt
`GetByFaAndOperationAsync` zwischen beide Teile oder wird doppelt angefasst. Zudem: Teil 7 ist
**weder implementiert noch freigegeben** (Pflicht-`/review` + 14-Fundstellen-Bewertung stehen aus,
`SubOrderNumber` existiert im WMS-Modell noch nicht) — die Etappen 1–3 koennen ihre AK erst
konkretisieren, wenn Teil 7 gemergt ist. Der Epic-Schnitt ist damit front-lastig „review/verify"
und duenn genau dort, wo das echte Risiko sitzt (Auswahl-UI). Erwaegen: Etappen 1–3 zu einem
Verifikations-Schritt zusammenziehen, Etappe 4 (Auswahl-UI + Durchzug ueber Terminal/Teileverfolgung)
feiner schneiden.

**KP-4 — Kombinationsgeraete kommen in Teil 8 nicht vor, brechen aber die Auswahlliste.**
Paketweit (Uebersicht + Teil-7-Antwort 1) ist entschieden: **keine `MontageAbteilung` in
`ProductionOrders`**, Kombinationsgeraete out of scope. Genau dann teilen sich aber zwei logische
Auftraege dieselbe `OrderNumber`, und die Auswahlliste nach dem Scan (Antwort 1: „Sub-FA-Nummer,
Matchcode, Arbeitsbereich") zieht **nur** aus `ProductionOrders` — ohne `MontageAbteilung`. Der
Werker sieht die Sub-FAs beider Kombi-Auftraege vermischt und kann sie nicht auftragsweise trennen.
Teil 8 muss die Paket-Entscheidung „Kombinationsgeraete out of scope" **explizit uebernehmen** und
die Grenze der Auswahlliste benennen (bewusst positionsweise Auswahl, keine Auftragstrennung) —
sonst baut der Dev eine UI, die diesen Fall still falsch bedient.

**KP-5 — NurFA-Button-Matching „last wins, no break" wird mit nicht-eindeutiger OrderNumber zum Fehlbucher.**
`bde-terminal.js` (NurFA-Modus) matcht den gescannten FA gegen die Bildschirm-Buttons per
Substring **ohne `break`** — bei mehreren Treffern gewinnt still der letzte. Heute harmlos (Unique
Index auf `OrderNumber`), nach der Inversion ein stiller Fehlgriff auf den falschen Sub-FA. Das
gehoert als **explizites AK** und Fix in Teil 8 (kein „last wins" mehr, sondern Auswahl gemaess
Antwort 1).

**KP-6 — Test-Datenabhaengigkeit fehlt im Deploy/Test-Abschnitt.**
Die Uebersicht nennt das **leere IDEAL-Testsystem** als kritischste Vorbedingung des ganzen Pakets;
fuer BDE gilt das verschaerft. AK 1 (zwei Sub-FAs derselben `OrderNumber` getrennt buchen) UND die
Verifikation von Aufloesungs-Schritt 1 (existieren Sub-FA-Barcodes? — Anhang B2 offen) sind ohne
produktivnahe hierarchische Rueckmeldedaten **nicht gruen zu bekommen**. Der Deploy-/Test-Abschnitt
sollte diese Abhaengigkeit als Schranke-2-Vorbedingung ausweisen (analog Teil 1–5).

### HINWEIS

**KP-7 — Das Terminal hat bereits eine Id-basierte, werkbank-gescopte FA-Liste.**
`BdeApiController.GetAvailableOperations` (NurFA-Modus) liefert offene FAs/Arbeitsgaenge der Werkbank
mit `id = po.Id` bzw. `wo.Id`; der Klick bucht per Id (`StartProductionForOrder`). Die von Antwort 1
gewuenschte „Auswahl nach Scan" ueberlappt dieses bestehende Muster — Etappe 4 sollte darauf
aufsetzen (Filter der bestehenden Liste), nicht eine zweite Auswahl-Mechanik erfinden. Macht Etappe 4
kleiner als der Text suggeriert.

**KP-8 — Audit ok, aber Etappe-1-Vorbehalt benennen.** `BdeBooking` setzt heute korrekt
`ModifiedAt/By/ByWindows` (`SetAudit`). Solange Etappe 1 kein neues persistiertes Modell einfuehrt,
bleibt ADR 0003 unberuehrt — **falls doch** (z. B. ein UI-Status fuer die Auswahl), muss die neue
Entitaet `AuditableEntity` erben; das im Datenmodell-Abschnitt festhalten.

**KP-9 — Startbedingung korrekt formuliert, aber scharf halten.** `depends_on` Teil 7 + Antwort 2
sind richtig; Teil 8 darf **nicht gestartet** werden, bevor Teil 7 gemergt ist (sonst arbeiten die
Etappen an nicht existierendem `SubOrderNumber`). Das ist in der Spec angelegt — bei der Freigabe
bewusst als harte Reihenfolge behandeln.

**Verdikt:** NACHBESSERUNG NOETIG: `affected_code` benennt den echten Scan-Aufloesungspunkt nicht
(KP-1), Aufloesungs-Schritt 1 hat keine belegte Eingabequelle und widerspricht dem Code (KP-2), und
die Etappen-1–3-Grenze zu Teil 7 plus die Kombinationsgeraete-/NurFA-Luecken (KP-3/4/5) wuerden den
Dev fehlleiten. Konzept (Variante b, code-getriebene Aufloesung) und Id-Kern sind solide — die
Nachbesserung ist Praezisierung des Dateiscopes, der Aufloesungs-Eingabe und der Teil-7-Abgrenzung,
keine Neukonzeption.

## Antworten auf die Kritische Pruefung (2026-08-06)

**Zu KP-2 — Befund akzeptiert, und die Aufloesungsreihenfolge aus Freigabe-Antwort 1 war
SCHLIMMER als „nur unerreichbar": sie haette falsch gebucht. [KORREKTUR]**

Die Pruefung zeigt am Code, dass der Scan nur die **HauptFA** liefert (Index 2 = FA-Nummer, keine
BelID) — die gegenteilige Behauptung in Teil 7 ist damit widerlegt. Aber der eigentliche Fehler
liegt tiefer: **Beim Hauptauftrag gilt `OrderNumber == SubOrderNumber`.** Ein `SubOrderNumber`-
Lookup mit einer gescannten HauptFA trifft daher **die Zeile des Hauptauftrags** — Schritt 1 haette
nicht „nie gefeuert", sondern **immer sofort auf den Hauptauftrag gebucht** und die Sub-FA-Auswahl
komplett uebersprungen. Genau das Gegenteil der Absicht.

**Korrigierte, verbindliche Aufloesungsreihenfolge:**
1. Lookup gegen **`OrderNumber`** (die Gruppe).
   - **genau ein** Auftrag → direkt buchen, keine Auswahl (flacher/AKE-Fall unveraendert)
   - **mehrere** → **Auswahlliste** der Auftraege dieser `OrderNumber` (Hauptauftrag **und**
     Sub-FAs — der Hauptauftrag ist selbst ein buchbarer FA)
2. **Kein Treffer** → Fallback-Lookup gegen **`SubOrderNumber`** → direkt buchen. Dieser Zweig ist
   heute unerreichbar und **bewusst als Vorruestung** enthalten: Er greift automatisch, falls
   spaeter Sub-FA-Barcodes eingefuehrt werden (Annahme B2, unbestaetigt). Er kann nicht falsch
   treffen, weil eine echte Sub-FA-Nummer nie eine `OrderNumber` ist.
3. Kein Treffer → bestehende Fehlerbehandlung, unveraendert.

**Konsumierter String — festgelegt:** das `fa`-Segment aus `bde-terminal.js:scanFaAgInput`
(Split auf `,`/`/` → `[fa, op]`). Der QR-Pfad in `barcode-scanner.js` (Index 2) liefert denselben
Wert. **Kein** BelID-Begriff im Client, keine Formataenderung noetig.

**Folge fuer Teil 7:** Die dortige Behauptung „QR traegt an Index 2 die BelID = SubOrderNumber" ist
falsch und muss in der Teil-7-Spec **gestrichen** werden — sie ist am Code widerlegt.

**Zu KP-1 — `affected_code` wird auf die echten Nahtstellen korrigiert.** Aufnehmen:
`Data/Repositories/WorkOperationRepository.cs` (`GetByFaAndOperationAsync`),
`Controllers/BdeApiController.cs` (`GetWorkOperation`, `GetAvailableOperations`),
`wwwroot/js/bde-terminal.js`. **Entfernen:** `wwwroot/js/barcode-scanner.js` — das ist der
Artikel-/Lagerplatz-Scanner, nicht der BDE-Terminal-Pfad. (Auch in Teil 7 entfernen, dort steht er
ebenfalls faelschlich.)

**Zu KP-3 — Grenze zu Teil 7, verbindlich formuliert:**
> **Teil 7 macht die Datenschicht mehrdeutigkeitsfaehig, ohne das Terminal-Verhalten zu aendern.
> Teil 8 baut die Disambiguierungs-UI und schaltet das Terminal darauf um.**

Konkret an `GetByFaAndOperationAsync`: **Teil 7** ergaenzt eine mengenwertige Variante
(`GetAllByFaAndOperationAsync`) und protokolliert im bestehenden Einzel-Lookup, wenn er mehr als
eine Zeile faende — aendert aber **nicht**, was das Terminal tut. Damit bleibt Teil 7 fuer sich
mergebar, ohne das Terminal zu brechen. **Teil 8** stellt den Aufruf auf die mengenwertige Variante
um und haengt die Auswahl daran.

**Etappen-Schnitt entsprechend neu** (KP-3-Empfehlung uebernommen — vorn war zu dick, hinten zu
duenn):
| # | Etappe |
|---|---|
| 1 | Verifikation: Teil-7-Haertung im BDE-Kontext nachvollziehen, Restluecken benennen (fasst die bisherigen 1–3 zusammen) |
| 2 | Aufloesungslogik serverseitig (Gruppen-Lookup, Fallback, Mehrdeutigkeits-Ergebnis) inkl. Unit-Tests |
| 3 | Auswahl-UI am Terminal, aufgesetzt auf die bestehende `GetAvailableOperations`-Liste (KP-7) |
| 4 | NurFA-Button-Matching fixen (KP-5) + Durchzug ueber Teileverfolgung |
| 5 | OSEON-Seite (nur falls Etappe 1 Bedarf zeigt) |
| 6 | Tests + Brain-Update |

**Zu KP-4 — Kombinationsgeraete: Paket-Entscheidung wird ausdruecklich uebernommen.**
Keine `MontageAbteilung` in `ProductionOrders`, also **kann die Auswahlliste zwei Kombi-Auftraege
nicht auftragsweise trennen** — der Werker sieht die Sub-FAs beider vermischt. Das ist die bewusst
akzeptierte Grenze, kein Umsetzungsfehler. In der Spec als **bekannte Einschraenkung** benennen
(damit der Dev keine Trennung erfindet und der Test sie nicht als Fehler meldet) und mit
[[2026-08-06-kombinationsgeraete-montageabteilung]] verlinken. Die Auswahl erfolgt bewusst
**positionsweise** (Matchcode/Arbeitsbereich unterscheiden), nicht auftragsweise.

**Zu KP-5 — NurFA-„last wins, no break" ist ein Fehlbucher und wird gefixt.**
Heute harmlos (Unique Index), nach der Inversion ein stiller Griff auf den falschen Sub-FA. Als
**eigenes AK** aufnehmen: Kein Substring-„last wins" mehr — bei mehreren Treffern greift dieselbe
Auswahl wie nach dem Scan. Gehoert in Etappe 4.

**Zu KP-6 — uebernommen.** Testdaten-Abhaengigkeit als Schranke-2-Vorbedingung in den Deploy-/
Test-Abschnitt (analog Teil 1–5): AK 1 und die Verifikation des Fallback-Zweigs sind ohne
produktivnahe hierarchische Rueckmeldedaten nicht gruen zu bekommen.

**Zu KP-7 — uebernommen, macht Etappe 3 kleiner.** Die Auswahl setzt auf die bestehende
`GetAvailableOperations`-Liste auf (Id-basiert, werkbank-gescopt) und **filtert** sie auf die
gescannte `OrderNumber` — keine zweite Auswahl-Mechanik. Der Klick bucht weiterhin per `Id`
(`StartProductionForOrder`), also ueber den bereits eindeutigen Schluessel.

**Zu KP-8 — vermerkt.** Solange Etappe 1/2 kein neues persistiertes Modell einfuehren, bleibt ADR
0003 unberuehrt. Entsteht doch eine neue Entitaet (z. B. UI-Status der Auswahl), erbt sie
`AuditableEntity` — im Datenmodell-Abschnitt festhalten.

**Zu KP-9 — bestaetigt als harte Reihenfolge.** Teil 8 wird **nicht gestartet**, bevor Teil 7
gemergt ist. `depends_on` bleibt.

## Kritische Pruefung (2026-08-07)

Zweiter Anwalt-des-Teufels-Durchgang, **nach** dem Antwortblock (2026-08-06). Gegengelesen: diese
Spec komplett (Frontmatter, Rumpf, erster Kritik-Block, Antwortblock), die Teil-7-Spec inkl. ihrer
Antworten, die Ideen-Notiz (B2/B5), der Anhang [[sage-views-ideal]], die Kombinationsgeraete-Notiz
[[2026-08-06-kombinationsgeraete-montageabteilung]] — **plus verifiziert am realen main-Code**:
`WorkOperationRepository.GetByFaAndOperationAsync`, `BdeApiController.GetWorkOperation`/
`GetAvailableOperations`, `bde-terminal.js` (`scanFaAgInput` + NurFA-Matching + Button-Rendering),
`barcode-scanner.js`, `BdeBookingService` (Audit), `TrackingController`.

**Vorab bestaetigt (Staerken):** Die KP-2-**Korrektur** ist am Code belegt und scharf: Weil beim
Hauptauftrag `OrderNumber == SubOrderNumber` gilt (Teil-7-Spec Zeile 82, Ideen-Notiz Zeile 125),
haette die urspruengliche „SubOrderNumber zuerst"-Reihenfolge tatsaechlich **immer sofort auf den
Hauptauftrag gebucht** und die Auswahl uebersprungen — die Umkehrung (OrderNumber-Gruppe zuerst,
SubOrderNumber als Vorruest-Fallback) ist richtig. Der Fallback-Zweig kann nicht fehltreffen: er
wird nur erreicht, wenn **kein** OrderNumber-Match vorliegt, und `SubOrderNumber` ist unique — die
Begruendung im Antwortblock haelt. Der AKE-Fall bleibt unveraendert (flach → genau ein Treffer →
Direktbuchung). Audit ist sauber (`BdeBookingService.SetAudit`/`SetAuditModified` ziehen
`ModifiedBy`/`ModifiedByWindows` aus `_userSvc`, KP-8 korrekt). NurFA-„last wins" (KP-5) am Code
verifiziert (`bde-terminal.js` Zeilen 168-170: `forEach` mit `indexOf`, **kein** `break`).

### BLOCKER — vor der Freigabe zu klaeren

**KP2-1 — Der RUMPF und das FRONTMATTER wurden NICHT auf die Antworten nachgezogen; die
Korrekturen leben ausschliesslich im Antwortblock. Die Spec widerspricht sich selbst.**
Genau der Defekt, den der Teil-7-Durchgang als B7-3/H7-2 blockiert hat — hier ist er unbehoben, und
der Antwortblock verspricht nicht einmal, ihn zu beheben. Konkret liest ein Dev heute
widerspruechliche Anweisungen, je nachdem, welchen Teil der Datei er oeffnet:
- **`affected_code` (Frontmatter Zeilen 13-21)** listet weiterhin `wwwroot/js/barcode-scanner.js`
  (Zeile 17) und enthaelt **weder** `Data/Repositories/WorkOperationRepository.cs` **noch**
  `wwwroot/js/bde-terminal.js` — also exakt die Falsch-Zuordnung, die KP-1 korrigiert. Der
  Antwortblock (Zu KP-1) beschreibt die Korrektur nur, **fuehrt sie im Frontmatter nicht aus**.
- **`open_questions` (Zeilen 23-26)** sind vollstaendig durch die Freigabe-Antworten beantwortet,
  stehen aber noch — das HOME-Dashboard zeigt Teil 8 damit faelschlich als offen (dieselbe Wirkung,
  die Teil 7 als H7-2 ausdruecklich vermeidet und leert).
- **`etappen` (Frontmatter Zeilen 28-34) UND die Etappen-Tabelle im Rumpf (Zeilen 117-126)** tragen
  noch den **alten** 6-Etappen-Schnitt (1 Rueckmelde-Datenmodell, 2 Repositories haerten …). Der
  Antwortblock hat einen **neuen** Schnitt (Zeilen 363-370: 1 Verifikation, 2 Aufloesungslogik, 3
  Auswahl-UI, 4 NurFA-Fix+Teileverfolgung …). Zwei Etappen-Tabellen in einer Datei, die einander
  widersprechen.
- **„Fachliche Anforderungen" B2/B-5 (Zeilen 65-73)** stellt die Entscheidung weiterhin als **offen**
  dar („Es muss entschieden werden, ob (a) … oder (b) …") — obwohl Variante (b) entschieden **und**
  die Aufloesungsreihenfolge zweimal korrigiert wurde.
- **„Technischer Loesungsentwurf" (Zeilen 78-83)** nennt `barcode-scanner.js` als Scan-Handler
  („bekommt … entweder eine Gruppen-Zwischenansicht oder eine direkte Sub-FA-Aufloesung") — falsche
  Datei (KP-1) **und** als „entweder/oder" unentschieden formuliert.
- **Akzeptanzkriterien (Zeilen 98-108)** enthalten **kein** AK fuer die korrigierte
  Aufloesungsreihenfolge, **kein** AK fuer den NurFA-Fix (der Antwortblock sagt zu KP-5 ausdruecklich
  „als eigenes AK aufnehmen"), **kein** AK fuer die Kombi-Einschraenkung und **keine** Nennung des
  Fallback-Zweigs. Die Deploy-Sektion (Zeilen 132-139) fuehrt die KP-6-Testdaten-Vorbedingung nicht,
  obwohl der Antwortblock sie „uebernommen" nennt.

  Solange der Rumpf nicht nachgezogen ist, ist die Spec **nicht umsetzungsreif**: Der Dev muesste den
  Antwortblock als heimliche Wahrheit gegen den widersprechenden Rumpf durchsetzen. Vor Freigabe:
  Frontmatter (`affected_code`, `open_questions` leeren, `etappen` auf den neuen Schnitt), die
  Etappen-Tabelle im Rumpf, den B2/B-5-Abschnitt, den Loesungsentwurf und die AK **auf den
  Antwortblock ziehen** — genauso, wie Teil 7 es in „Zu B7-3" fuer sich zugesagt hat.

**KP2-2 — Die Etappen-1/2-Grenze stuetzt sich auf einen Teil-7-Liefergegenstand, den Teil 7 gar
nicht zusagt.** Der Antwortblock (Zu KP-3) formuliert die Grenze verbindlich: „**Teil 7** ergaenzt
eine mengenwertige Variante (`GetAllByFaAndOperationAsync`) und protokolliert im bestehenden
Einzel-Lookup, wenn er mehr als eine Zeile faende … **Teil 8** stellt den Aufruf auf die
mengenwertige Variante um." Gegengelesen mit der **Teil-7-Spec**: deren In-Scope nennt nur „Durchzug
von `SubOrderNumber` durch Repositories/Controller/Scan-Lookups", und **AK 8** verlangt je Fundstelle
eine **binaere** Einordnung — „unkritisch (Gruppen-Lookup, bleibt `OrderNumber`)" **oder** „kritisch,
auf `SubOrderNumber` umgestellt". Eine **dritte** Zusage — „mengenwertige Zusatzvariante anlegen +
Einzel-Lookup nur protokollieren, Terminal unveraendert lassen" — steht in Teil 7 **nirgends**.
`WorkOperationRepository` ist dort zwar unter den 14 Fundstellen gelistet, aber unter der
Binaer-Regel faellt der BDE-Scan-Lookup entweder in „bleibt OrderNumber" (dann fehlt die
Disambiguierung ganz) oder „auf SubOrderNumber umgestellt" (dann bricht — wie KP-2 zeigt — die
Gruppenaufloesung). Die von Teil 8 gebrauchte „mengenwertige Variante + Logging, Terminal
unangetastet" ist ein **eigener** Liefergegenstand. Folge: Entweder die **Teil-7-Spec** wird vor
deren Freigabe explizit um diesen Punkt ergaenzt (AK + `affected_code`), **oder** Teil 8 uebernimmt
die Erstellung von `GetAllByFaAndOperationAsync` + Logging selbst in Etappe 1/2 — dann ist die
Grenzformulierung „Teil 7 liefert, Teil 8 stellt um" falsch und muss umgeschrieben werden. So wie es
steht, arbeiten beide Specs an einer Naht, die keine von beiden verbindlich baut.

### SOLLTE — sichert die Dev-/Etappen-Laeufe

**KP2-3 — Die korrigierte Aufloesungsreihenfolge ist nur auf Auftrags-Granularitaet formuliert und
ignoriert, dass der reale Normal-Modus-Lookup auf (`OrderNumber` + `OperationNumber`) keyed ist.**
Der einzige eindeutigkeitsannehmende Normal-Modus-Seam ist `GetByFaAndOperationAsync(faNumber,
operationNumber)` = `FirstOrDefaultAsync(w => w.ProductionOrder.OrderNumber == faNumber &&
w.OperationNumber == operationNumber)` — er filtert **zusaetzlich** auf die AG-Nummer und liefert
**eine WorkOperation**, keinen „Auftrag". Der Antwortblock beschreibt Schritt 1 aber als
„Lookup gegen `OrderNumber` (die Gruppe) → genau ein **Auftrag** → direkt buchen". Bei zwei Sub-FAs
derselben `OrderNumber`, die **beide** einen AG „01" haben, liefert (`OrderNumber`, „01") **zwei**
WorkOperations — die Mehrdeutigkeit sitzt also auf (`OrderNumber`+`OperationNumber`)-Ebene, nicht auf
reiner `OrderNumber`-Ebene. Verschaerfend: der **NurFA-Modus** hat gar keine AG im Scan (das Terminal
setzt im Fallback hart `opNumber=01`, `bde-terminal.js` Zeile 176) und bucht per `ProductionOrder.Id`
ueber `StartProductionForOrder` — **andere** Granularitaet und **anderer** Buchungspfad als der
Normal-Modus (per `workOperationId`). Etappe 2 („Aufloesungslogik serverseitig") muss die
Reihenfolge fuer **beide** Modi getrennt festlegen (Normal: Menge der WorkOperations je
`OrderNumber`+`OperationNumber`; NurFA: Menge der ProductionOrders je `OrderNumber`), sonst rät der
Dev, welche Ebene „genau ein / mehrere" meint.

**KP2-4 — `affected_code`-Korrektur (Antwortblock) ist gegen den neuen Etappen-4-Umfang
unvollstaendig.** Etappe 4 (neuer Schnitt) enthaelt ausdruecklich „Durchzug ueber Teileverfolgung".
Der reale Teileverfolgungs-Scan laeuft ueber `barcode-scanner.js` (`scanType 'productionOrder'`,
Index 2 = FA-Nummer) und fuellt einen Filter, den `TrackingController` per
`OrderNumber.Contains(filterOrderNumber)` auswertet — ein **Filter**, kein eindeutigkeitsannehmender
Lookup, also unkritisch, aber **betroffen**. Der Antwortblock streicht `barcode-scanner.js` pauschal
und benennt fuer Etappe 4 **keinen** Teileverfolgungs-Code (`TrackingController` steht zwar noch im
Rumpf-`affected_code`, wird in der KP-1-Antwort aber nicht bestaetigt). Beim Nachziehen von
`affected_code` (KP2-1) den Teileverfolgungs-Pfad bewusst behandeln: entweder als „nur Filter,
unkritisch" dokumentieren oder aufnehmen — nicht stillschweigend fallen lassen.

**KP2-5 — Der Satz „Der QR-Pfad in `barcode-scanner.js` (Index 2) liefert denselben Wert" ist
irrefuehrend.** Das BDE-Terminal ruft `barcode-scanner.js` **nicht** auf — `scanFaAgInput`
verarbeitet ein eigenes Text-Input (`scanFaAg`, Split auf `,`/`/`). `barcode-scanner.js` ist ein
**separater** Kamera-/Bild-Scanner fuer andere Masken (Artikel/Lagerplatz/Teileverfolgung). Fuer die
Festlegung des konsumierten Strings ist das harmlos (das `fa`-Segment aus `bde-terminal.js` ist die
alleinige Quelle), aber die Behauptung suggeriert eine Kopplung der beiden Scanner, die es nicht
gibt. Beim Rumpf-Nachzug (KP2-1) den Satz auf „das BDE-Terminal nutzt einen eigenen
Text-Input-Handler, nicht `barcode-scanner.js`" schaerfen.

**KP2-6 — Server-seitige Gruppenaufloesung (Etappe 2) und werkbank-gescopte Auswahl-UI (Etappe 3)
koennen divergieren.** `GetAvailableOperations` liefert nur FAs/AGs **dieser Werkbank** (KP-7,
verifiziert: `ProductionWorkplaceId == workplaceId`). Ein server-seitiger Gruppen-Lookup auf
`OrderNumber` liefert dagegen **alle** Sub-FAs der Gruppe, auch die an anderen Werkbaenken. Wird die
UI-Auswahl (Etappe 3) auf die Werkbank-Liste gefiltert, aber die Mehrdeutigkeitspruefung (Etappe 2)
auf die volle Gruppe gestellt, kann Etappe 2 „mehrere" melden, waehrend die Werkbank-Liste nur einen
zeigt (oder umgekehrt). Festlegen, auf welcher Menge „genau ein / mehrere" entschieden wird — sinnvoll
die werkbank-gescopte, damit UI und Entscheidung deckungsgleich sind.

### HINWEIS

**KP2-7 — `epic: true` korrekt gesetzt** (Frontmatter Zeile 27); der neue Etappen-Schnitt aus dem
Antwortblock ist in der Reihenfolge schluessig (Verifikation → Server-Logik → UI → Fix+Durchzug →
OSEON bedingt → Tests) und mit „ein langlebiger Worktree, kein Zwischen-Merge" (Rumpf Zeile 128)
vertraeglich — die Etappen brauchen nur **buildbare** Commits (AK 5), keine je-Etappe-Mergebarkeit.
Einzig die Doppelung „Unit-Tests in Etappe 2" vs. „Tests in Etappe 6" klarstellen (Etappe 6 =
Integrations-/Testszenarien + Brain).

**KP2-8 — Regressionsgarantie AKE haelt, aber unverankert.** Der „mehrere → Auswahl"-Zweig bricht
den AKE-Fall nicht (dort `OrderNumber == SubOrderNumber`, immer genau ein Treffer → Direktbuchung).
Das ist korrekt — gehoert aber als expliziter AK-Satz in den nachgezogenen Rumpf (heute nur AK 4
„AKE-Verhalten unveraendert", ohne Bezug auf den neuen Auswahl-Zweig).

**KP2-9 — Teil-7-Rumpf traegt noch die widerlegte „Index 2 = BelID"-Behauptung.** Der Antwortblock
(Zu KP-2) sagt korrekt, diese Aussage sei am Code widerlegt und muesse in der **Teil-7-Spec**
gestrichen werden — dort steht sie aber weiterhin (`affected_code` Zeile 27 „Index 2 = BelID" und
Rumpf Zeile 165 „QR traegt an Index 2 die BelID = kuenftig `SubOrderNumber`"). Das ist Teil-7-Pflege
(nicht in dieser Datei zu aendern), aber die Grenzformulierung von Teil 8 haengt daran: Solange Teil
7 die falsche Behauptung traegt, ist die Abstimmung „mit Teil 7 abgestimmt" (Antwortblock zu KP-2)
faktisch offen. Bei der Teil-7-Nachbesserung mitziehen.

**Verdikt:** Der fachliche Kern ist jetzt **richtig** — die KP-2-Korrektur ist am Code belegt, die
Aufloesungsreihenfolge (OrderNumber-Gruppe → Fallback SubOrderNumber) ist konsistent und
AKE-regressionssicher, Audit und Id-Kern halten. **Aber** der Rumpf/das Frontmatter wurden nicht auf
den Antwortblock nachgezogen: `affected_code`, `open_questions`, beide Etappen-Tabellen, der
B2/B-5-Abschnitt, der Loesungsentwurf und die AK widersprechen den Antworten (KP2-1) — dieselbe
Luecke, die Teil 7 als B7-3 blockiert hat. Zusaetzlich stuetzt sich die Etappen-Grenze auf einen
Teil-7-Liefergegenstand, den Teil 7 nicht zusagt (KP2-2), und die Aufloesungslogik ist fuer die zwei
BDE-Modi unterspezifiziert (KP2-3). Das sind Praezisierungen und ein Nachzug, keine Neukonzeption.

NACHBESSERUNG NOETIG: Rumpf + Frontmatter auf den Antwortblock nachziehen (KP2-1), die
Teil-7-Grenze verbindlich absichern (KP2-2), die Aufloesungslogik je BDE-Modus auf die reale
(`OrderNumber`+`OperationNumber`)-/PO-Id-Granularitaet festlegen (KP2-3).
