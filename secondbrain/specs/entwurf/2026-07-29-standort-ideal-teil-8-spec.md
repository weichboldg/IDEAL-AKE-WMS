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
