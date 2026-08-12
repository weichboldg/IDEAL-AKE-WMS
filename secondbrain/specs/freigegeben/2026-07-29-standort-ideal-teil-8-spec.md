---
type: spec
title: "IDEAL-Standort Teil 8 — Sub-FA-Rueckmeldung / BDE (Epic)"
slug: 2026-07-29-standort-ideal-teil-8-spec
status: Entwurf
created: 2026-08-06
updated: 2026-08-07
source_backlog: "[[2026-07-29-Standort-IDEAL]]"
depends_on: "[[2026-07-29-standort-ideal-teil-7-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/BdeTerminalController.cs
  - "IdealAkeWms/Controllers/BdeApiController.cs (GetWorkOperation, GetAvailableOperations)"
  - IdealAkeWms/Services/BdeBookingService.cs
  - "IdealAkeWms/Data/Repositories/WorkOperationRepository.cs (GetByFaAndOperationAsync, GetAllByFaAndOperationAsync aus Teil 7)"
  - "IdealAkeWms/wwwroot/js/bde-terminal.js (scanFaAgInput, NurFA-Button-Matching)"
  - IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs
  - "IdealAkeWms/Controllers/TrackingController.cs (Filter auf OrderNumber, unkritisch — siehe Umfang)"
  - "IDEALAKEWMSService/Services/OseonSyncService.cs (Review OrderNumber-Bezug)"
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Aufloesungslogik je BDE-Modus: Normal-Modus-Lookup (GetByFaAndOperationAsync/GetAllByFaAndOperationAsync) ist auf (OrderNumber + OperationNumber) geschluesselt und liefert WorkOperations; NurFA-Modus hat keine AG-Nummer im Scan (Fallback hart opNumber=01) und bucht per ProductionOrder.Id via StartProductionForOrder. Auf welcher Granularitaet gilt 'genau ein / mehrere' je Modus? Muss in Etappe 2 verbindlich festgelegt werden, bevor Etappe 3 die UI baut."
  - "Scope der Gruppenaufloesung: entscheidet der Server-Lookup (Etappe 2) ueber die volle OrderNumber-Gruppe (auch Sub-FAs an anderen Werkbaenken) oder werkbank-gescopt wie die bestehende GetAvailableOperations-Liste (Etappe 3)? Muss deckungsgleich sein, sonst kann der Server 'mehrere' melden, waehrend die Werkbank-Liste nur einen zeigt (oder umgekehrt)."
epic: true
etappen:
  - "1: Verifikation — Teil-7-Haertung im BDE-Kontext nachvollziehen, Restluecken benennen"
  - "2: Aufloesungslogik serverseitig (Gruppen-Lookup auf OrderNumber, Fallback auf SubOrderNumber, Mehrdeutigkeits-Ergebnis; Granularitaet je Modus + Scope klaeren) inkl. Unit-Tests"
  - "3: Auswahl-UI am Terminal, aufgesetzt auf die bestehende GetAvailableOperations-Liste"
  - "4: NurFA-Button-Matching fixen (kein last-wins ohne break mehr) + Durchzug ueber Teileverfolgung"
  - "5: OSEON-Seite (nur falls Etappe 1 Bedarf zeigt)"
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

**In-Scope:** BDE-Terminal-Buchung gegen materialisierte Sub-FAs; Aufloesung des gescannten
`fa`-Segments (aus `bde-terminal.js:scanFaAgInput`) ueber die verbindliche Reihenfolge
`OrderNumber`-Gruppe → Fallback `SubOrderNumber` → bestehende Fehlerbehandlung (siehe „Fachliche
Anforderungen"); Auswahl-UI bei mehrdeutiger `OrderNumber`-Gruppe, aufgesetzt auf die bestehende
werkbank-gescopte `GetAvailableOperations`-Liste; Fix des NurFA-Substring-Matchings
(last-wins-ohne-`break`, siehe unten); Durchzug der Aufloesung ueber die Teileverfolgung (dort nur
Filter auf `OrderNumber`, unkritisch, siehe `TrackingController`).

**Out-of-Scope:** die Materialisierung selbst (Teil 7, Voraussetzung — liefert
`GetAllByFaAndOperationAsync` + Logging, siehe „Technischer Loesungsentwurf"); Kombinationsgeraete/
`MontageAbteilung` (Paket-Entscheidung, siehe
[[2026-08-06-kombinationsgeraete-montageabteilung]] — die Auswahlliste kann Kombi-Auftraege nicht
auftragsweise trennen, das ist bewusst akzeptiert); keine neue BDE-Fachlogik jenseits dessen, was
die Hierarchie erzwingt (bestehende Buchungsregeln, Mehrfachbuchungs-Konfiguration etc. bleiben
unveraendert).

## Fachliche Anforderungen

- **Scan-Aufloesungsreihenfolge (entschieden — Variante b, Werker waehlt nach dem Scan; siehe
  „Bereits entschieden" unten):** Konsumierter String ist das `fa`-Segment aus
  `bde-terminal.js:scanFaAgInput` (Split auf `,`/`/` → `[fa, op]`; derselbe String kommt auch aus
  dem QR-Pfad an Index 2 — das BDE-Terminal nutzt dafuer einen eigenen Text-Input-Handler, nicht
  `barcode-scanner.js`). Verbindliche Reihenfolge:
  1. Lookup gegen **`OrderNumber`** (die Gruppe).
     - **genau ein** zugehoeriger Auftrag → direkt buchen, keine Auswahl (flacher/AKE-Fall
       unveraendert, dort gilt `OrderNumber == SubOrderNumber`)
     - **mehrere** → Auswahlliste der Auftraege dieser `OrderNumber` (Hauptauftrag **und**
       Sub-FAs — der Hauptauftrag ist selbst ein buchbarer FA); die Liste zeigt mindestens
       Sub-FA-Nummer, Matchcode/Bezeichnung und Arbeitsbereich, vorsortiert/hervorgehoben nach
       offenem Arbeitsgang im Bereich des Werkers.
  2. **Kein Treffer** → Fallback-Lookup gegen **`SubOrderNumber`** → direkt buchen. Dieser Zweig
     ist heute unerreichbar (Anhang B2: Sub-FA wird in Barcodes bislang nicht verwendet) und
     bewusst als Vorruestung fuer kuenftige Sub-FA-Barcodes enthalten; er kann nicht fehltreffen,
     weil eine echte `SubOrderNumber` nie eine `OrderNumber` ist.
  3. **Kein Treffer** → bestehende Fehlerbehandlung, unveraendert.
- **NurFA-Modus-Fix:** Der bestehende Substring-Vergleich in `bde-terminal.js` (Button-Matching
  ohne `break`, „last wins" bei mehreren Treffern) wird durch dieselbe Auswahl wie nach dem Scan
  ersetzt — kein stiller Fehlgriff auf den falschen Sub-FA mehr.
- **Kombinationsgeraete (bekannte Grenze, kein Umsetzungsfehler):** Da `ProductionOrders` kein
  `MontageAbteilung`-Feld traegt (Paket-Entscheidung, siehe
  [[2026-08-06-kombinationsgeraete-montageabteilung]]), kann die Auswahlliste zwei
  Kombinationsgeraete-Auftraege derselben `OrderNumber` nicht auftragsweise trennen — der Werker
  unterscheidet positionsweise (Matchcode/Arbeitsbereich). Dev und Test duerfen das nicht als
  Fehler werten.
- **Randfall gleichzeitige Rueckmeldung:** Zwei Werker melden zeitgleich unterschiedliche Sub-FAs
  derselben `OrderNumber` zurueck — unproblematisch, weil jede Buchung nach der Aufloesung ueber
  die eindeutige `WorkOperation`- bzw. `ProductionOrder.Id` laeuft (Normal-Modus:
  `workOperationId`; NurFA-Modus: `StartProductionForOrder` per `ProductionOrder.Id`). Kein
  gemeinsamer Lock noetig.
- Alle bestehenden BDE-Fallstricke (Mehrfachbuchungs-Regel, `Paused`/`EndedAt`, BDE-Sperre bei
  verpackt/abgeholt, Auto-Pause-Schichtende) gelten unveraendert je `SubOrderNumber`-Auftrag.

## Technischer Loesungsentwurf

Da `ProductionOrders` nach Teil 7 bereits das materialisierte Sub-FA-Modell traegt, ist dieser
Teil primaer ein **Haertungs- und Anbindungs-Epic**, kein Neubau.

**Verbindliche Teil-7/Teil-8-Grenze:** Teil 7 macht die Datenschicht mehrdeutigkeitsfaehig, ohne
das Terminal-Verhalten zu aendern — konkret ergaenzt Teil 7 an `WorkOperationRepository` eine
mengenwertige Variante `GetAllByFaAndOperationAsync` neben dem bestehenden
`GetByFaAndOperationAsync` und protokolliert dort, wenn der Einzel-Lookup mehr als eine Zeile
faende. Teil 8 stellt den Aufruf im BDE-Pfad auf die mengenwertige Variante um und baut die
Disambiguierungs-UI. Damit bleibt Teil 7 fuer sich mergebar, ohne das Terminal zu brechen. (Diese
Grenze ist als vorgegeben zu behandeln; die entsprechende Ergaenzung von AK/`affected_code` in der
Teil-7-Spec ist dort zu pflegen, nicht in dieser Datei.)

`BdeApiController.GetWorkOperation` (Normal-Modus) und `GetAvailableOperations`/das
NurFA-Button-Matching in `bde-terminal.js` werden auf die neue Aufloesungsreihenfolge (siehe
„Fachliche Anforderungen") umgestellt. Die Auswahl-UI setzt auf die bestehende, werkbank-gescopte
`GetAvailableOperations`-Liste auf (Id-basiert, Klick bucht per `Id` via `StartProductionForOrder`)
und filtert sie auf die gescannte `OrderNumber` — keine zweite Auswahl-Mechanik.
`barcode-scanner.js` ist **nicht** Teil des BDE-Terminal-Pfads (das ist der
Artikel-/Lagerplatz-/Teileverfolgungs-Kamera-Scanner) und daher nicht betroffen; die
Teileverfolgung (`TrackingController`) wertet `OrderNumber` nur als **Filter** aus (`Contains`),
kein eindeutigkeitsannehmender Lookup — unkritisch, aber im `affected_code` mitgefuehrt.

Offen bleibt die genaue Granularitaet je BDE-Modus und der Scope der Gruppenaufloesung (siehe
„Offene Rueckfragen") — Etappe 2 legt beides verbindlich fest, bevor Etappe 3 die UI baut.

## Migrations-/SQL-Auswirkungen

Voraussichtlich **keine grosse Migration** — die Satelliten (`ProductionOrderBdeStatus` etc.)
haengen bereits per FK an `ProductionOrder.Id`, nicht an `OrderNumber`, und funktionieren daher
strukturell unveraendert. Falls im Zuge einer Etappe doch ein Datenmodell-Zusatz noetig wird
(z. B. ein UI-Statusfeld fuer die Gruppen-/Einzelauswahl), erhaelt er eine eigene idempotente
`SQL/XX_*.sql`-Datei nach ADR 0004 — Nummer zum jeweiligen Etappen-Zeitpunkt neu zu ermitteln
(voraussichtlich ab `SQL/88`, abhaengig vom Stand von Teil 1/7 zum Umsetzungszeitpunkt).

## Audit-Feld-Auswirkungen

`BdeBooking` setzt heute korrekt `ModifiedAt`/`ModifiedBy`/`ModifiedByWindows` ueber
`BdeBookingService.SetAudit`/`SetAuditModified` aus `ICurrentUserService` — daran aendert sich
nichts, solange keine neue persistierte Entitaet entsteht. Fuehrt eine Etappe (z. B. ein
UI-Statusfeld fuer die Auswahl) doch eine neue Entitaet ein, muss sie `AuditableEntity` erben
(ADR 0003) und bei jedem Update die drei Felder setzen — siehe Akzeptanzkriterium dazu.

## Akzeptanzkriterien

1. Scan/Eingabe des `fa`-Segments trifft genau einen Auftrag ueber `OrderNumber` → direkte
   Buchung ohne Auswahl (deckt den AKE-/flachen Fall sowie eindeutige IDEAL-FAs ab).
2. Scan/Eingabe trifft mehrere Auftraege derselben `OrderNumber` → Auswahlliste mit
   Sub-FA-Nummer, Matchcode/Bezeichnung und Arbeitsbereich (inkl. Hauptauftrag als Option);
   Werker waehlt, danach Buchung per eindeutiger `Id`.
3. Kein Treffer auf `OrderNumber`, aber Treffer auf `SubOrderNumber` → direkte Buchung
   (Fallback-Zweig; ohne Sub-FA-Barcode-Testdaten heute nicht pruefbar, siehe
   Testdaten-Vorbedingung unter „Deploy").
4. Kein Treffer auf beiden → bestehende Fehlerbehandlung, unveraendert.
5. NurFA-Modus: bei mehreren Substring-Treffern erscheint dieselbe Auswahl wie im Normal-Modus —
   kein stilles „last wins" mehr.
6. Zwei Sub-FAs derselben `OrderNumber` werden am Terminal nacheinander korrekt getrennt gebucht
   (keine Vermischung von Buchungen).
7. Kombinationsgeraete: Auswahlliste zeigt Sub-FAs mehrerer Kombi-Auftraege positionsweise
   unterscheidbar, keine automatische Auftragstrennung — als bekannte Grenze dokumentiert, kein
   Testfehler.
8. Bestehende BDE-Regeln (Mehrfachbuchung, Pause, Sperre bei verpackt/abgeholt) funktionieren
   unveraendert je Sub-FA.
9. AKE-Verhalten (Master aus, `OrderNumber == SubOrderNumber`) unveraendert: immer genau ein
   Treffer, immer Direktbuchung ohne Auswahl.
10. Fuehrt eine Etappe eine neue persistierte Entitaet ein (z. B. UI-Status der Auswahl), erbt sie
    `AuditableEntity` und setzt `ModifiedAt`/`ModifiedBy`/`ModifiedByWindows` bei jedem Update
    (ADR 0003).
11. Jede Etappe endet in einem eigenstaendigen, buildbaren Commit (kein Zwischenzustand, der
    `dotnet build`/`dotnet test` bricht).

## Test-Szenarien

Neues Kapitel „IDEAL Teil 8 — Sub-FA-BDE":
- Terminal-Buchung auf zwei Sub-FAs derselben `OrderNumber` nacheinander (Normal- und
  NurFA-Modus) — beide korrekt getrennt erfasst, Auswahlliste zeigt beide unterscheidbar.
- Scan/Eingabe einer eindeutigen `OrderNumber` (flacher/AKE-Fall) — Direktbuchung ohne Auswahl.
- NurFA-Modus mit mehreren Substring-Treffern — Auswahl statt „last wins".
- Fallback-Zweig `SubOrderNumber` (sobald Testdaten mit Sub-FA-Barcodes verfuegbar sind, siehe
  Testdaten-Vorbedingung unter „Deploy").
- Kombinationsgeraete-Auswahlliste (positionsweise, keine Auftragstrennung) — als erwartetes
  Verhalten protokolliert, nicht als Fehler.
- Regressionslauf der bestehenden BDE-Testszenarien (Mehrfachbuchung, Pause, Sperre) gegen
  materialisierte Sub-FAs.

## Etappen (epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| 1 | Verifikation: Teil-7-Haertung im BDE-Kontext nachvollziehen, Restluecken benennen | offen | |
| 2 | Aufloesungslogik serverseitig (Gruppen-Lookup auf `OrderNumber`, Fallback auf `SubOrderNumber`, Mehrdeutigkeits-Ergebnis; Granularitaet je Modus + Scope klaeren, siehe „Offene Rueckfragen") inkl. Unit-Tests | offen | |
| 3 | Auswahl-UI am Terminal, aufgesetzt auf die bestehende `GetAvailableOperations`-Liste | offen | |
| 4 | NurFA-Button-Matching fixen (kein last-wins ohne `break` mehr) + Durchzug ueber Teileverfolgung | offen | |
| 5 | OSEON-Seite (nur falls Etappe 1 Bedarf zeigt) | offen | |
| 6 | Tests (Unit + Test-Szenarien) + Brain-Update | offen | |

Ein langlebiger Worktree traegt alle Etappen; **kein** Zwischen-Merge. Waehrend der Arbeit den
Branch regelmaessig mit `scripts/sync-worktree.ps1 -Slug <slug>` auf `main`-Stand halten. QA und
Merge (Schranke 2) erst, wenn **alle** Etappen abgeschlossen sind.

## Deploy

- **Web-App:** ja.
- **Service:** ja (falls Etappe 5 OSEON-Anpassungen bringt).
- **Migration:** wahrscheinlich keine, Umfang haengt vom Ergebnis von Etappe 1 ab (siehe
  „Migrations-/SQL-Auswirkungen").
- **Testdaten-Vorbedingung (Schranke-2-Vorbedingung, analog Teil 1–5):** AK 2/6 (mehrere Sub-FAs
  derselben `OrderNumber` getrennt buchen) und AK 3 (Fallback-Zweig `SubOrderNumber`) sind ohne
  produktivnahe hierarchische Rueckmeldedaten im IDEAL-Testsystem nicht gruen zu bekommen — das
  Testsystem ist heute leer (siehe Uebersichts-Spec). Vor Schranke 2 sicherstellen, dass
  entsprechende Testdaten existieren.
- **Publish-Befehle:** wie Teil 7, vom Dev-Lauf am Ende aller Etappen gegen den tatsaechlichen
  Gesamt-Diff zu bestaetigen.

## Offene Rueckfragen

1. Aufloesungslogik je BDE-Modus: Normal-Modus-Lookup (`GetByFaAndOperationAsync`/
   `GetAllByFaAndOperationAsync`) ist auf (`OrderNumber` + `OperationNumber`) geschluesselt und
   liefert `WorkOperation`s; NurFA-Modus hat keine AG-Nummer im Scan (Fallback hart
   `opNumber=01`) und bucht per `ProductionOrder.Id` via `StartProductionForOrder`. Auf welcher
   Granularitaet gilt „genau ein / mehrere" je Modus? Muss in Etappe 2 verbindlich festgelegt
   werden, bevor Etappe 3 die UI baut.
2. Scope der Gruppenaufloesung: entscheidet der Server-Lookup (Etappe 2) ueber die volle
   `OrderNumber`-Gruppe (auch Sub-FAs an anderen Werkbaenken) oder werkbank-gescopt wie die
   bestehende `GetAvailableOperations`-Liste (Etappe 3)? Muss deckungsgleich sein, sonst kann der
   Server „mehrere" melden, waehrend die Werkbank-Liste nur einen zeigt (oder umgekehrt).

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. →
2. →

## Bereits entschieden (Schranke 1, erster Durchgang — 2026-08-06)

Die folgenden drei Rueckfragen aus dem ersten Entwurf sind beantwortet und werden hier archiviert
statt weiter als offene Rueckfrage gefuehrt (`open_questions` oben ist auf das tatsaechlich noch
Offene getrimmt).

**1. B2/B-5 — Scan-Aufloesung: Haupt-FA-Ebene oder Werker-Auswahl aus den Sub-FAs?**

→ **Variante (b): Der Werker waehlt nach dem Scan den konkreten Sub-FA. Aber
code-getrieben, nicht annahmegetrieben.**

**Variante (a) — Rueckmeldung auf Haupt-FA-Ebene — wird abgelehnt**, aus zwei Gruenden:
`BdeBooking` haengt per FK an `ProductionOrder.Id`; eine Buchung "auf eine Gruppe" hat schlicht
kein Ziel — das waere ein Modellbruch mitten im Kern. Und fachlich: Wenn nur auf Haupt-FA-Ebene
rueckgemeldet wird, ist die ganze Materialisierung der Sub-FAs (Teil 7) zwecklos. Man wuerde das
Datenmodell umbauen und die Information dann wegwerfen.

**Hinweis (Nachbesserung):** Die in dieser Antwort urspruenglich beschriebene konkrete
Aufloesungsreihenfolge (`SubOrderNumber` zuerst) wurde durch die KP-2-Korrektur ersetzt (siehe
„Antworten auf die Kritische Pruefung (2026-08-06)", Zu KP-2, weiter unten): Weil beim
Hauptauftrag `OrderNumber == SubOrderNumber` gilt, haette ein `SubOrderNumber`-Lookup zuerst
**immer sofort auf den Hauptauftrag gebucht** und die Auswahl uebersprungen. Verbindlich ist die
Reihenfolge in „Fachliche Anforderungen" oben: **`OrderNumber`-Gruppe zuerst, `SubOrderNumber` als
unerreichbarer Vorruest-Fallback danach.** Die grundsaetzliche Entscheidung — Variante (b),
code-getrieben statt annahmegetrieben, Ablehnung von Variante (a) — bleibt unveraendert gueltig.

**2. Bleibt die Voraussetzung „Teil 7 vollstaendig abgeschlossen" bestehen, oder wird laut der in
der Notiz genannten „Alternativen Reihenfolge" vorgezogen?**

→ **Ja, Teil 7 bleibt vollstaendige Voraussetzung.** `depends_on` unveraendert. Die "alternative
Reihenfolge" der Ideen-Notiz galt fuer den Fall, dass Rueckmeldefaehigkeit von Tag eins gebraucht
wird — die risiko-aufsteigende Reihenfolge ist inzwischen entschieden (Uebersichts-Spec), also
greift sie nicht. Vor Teil 7 gibt es keine materialisierten Sub-FAs, an denen dieses Epic
arbeiten koennte.

**3. Ist die OSEON-Seite (`OseonSyncService`) bereits `SubOrderNumber`-fest, oder braucht es
dafuer eine eigene Etappe?**

→ **Offen lassen — aber als Ergebnis von Etappe 1 (vormals Etappe 2), nicht als Vorbedingung.**
Ob `OseonSyncService` bereits `SubOrderNumber`-fest ist, laesst sich ohne den Code-Review nicht
beantworten und soll die Freigabe nicht blockieren. Verbindlich:
- Die `OrderNumber`-Bezuege in `OseonSyncService` sind Teil des Haertungs-Reviews in **Etappe 1**
  (Verifikation, neuer Schnitt).
- **Etappe 1 liefert ein ausdrueckliches Urteil**: OSEON ist bereits fest (dann entfaellt
  Etappe 5) oder nicht (dann wird Etappe 5 mit konkretem Umfang gefuellt).
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

### Nachbesserung 2 (2026-08-07)

Rumpf und Frontmatter wurden auf den Antwortblock (2026-08-06) nachgezogen — behebt KP2-1:

- `affected_code` (Frontmatter) korrigiert: `barcode-scanner.js` entfernt, `WorkOperationRepository.cs`
  (`GetByFaAndOperationAsync`/`GetAllByFaAndOperationAsync`), `bde-terminal.js` und die konkreten
  `BdeApiController`-Methoden ergaenzt; `TrackingController.cs` bleibt mit dem Hinweis „Filter,
  unkritisch" (KP2-4).
- `open_questions`/„Offene Rueckfragen" getrimmt: die drei urspruenglichen Fragen sind beantwortet
  und nach „Bereits entschieden (Schranke 1, erster Durchgang — 2026-08-06)" archiviert; neu
  aufgenommen sind die beiden von der zweiten Pruefung offen gelassenen Fragen KP2-3 (Granularitaet
  je BDE-Modus) und KP2-6 (Scope der Gruppenaufloesung, werkbank- vs. gruppenweit) — dafuer die
  „Freigabe-Antworten" neu und leer prefillt.
- Beide Etappen-Tabellen (Frontmatter `etappen` und Rumpf) auf den einen, im Antwortblock (Zu KP-3)
  festgelegten 6er-Schnitt reduziert (Verifikation → Aufloesungslogik → Auswahl-UI → NurFA-Fix +
  Teileverfolgung → OSEON bedingt → Tests).
- „Fachliche Anforderungen" stellt B2/B-5 nicht mehr als offen dar, sondern traegt die korrigierte,
  code-getriebene Aufloesungsreihenfolge (`OrderNumber`-Gruppe zuerst, `SubOrderNumber`-Fallback
  danach) direkt als Anforderung, inkl. NurFA-Fix, Kombigeraete-Grenze
  ([[2026-08-06-kombinationsgeraete-montageabteilung]]) und dem Randfall gleichzeitiger
  Rueckmeldung zweier Sub-FAs derselben `OrderNumber`.
- „Technischer Loesungsentwurf" benennt jetzt `bde-terminal.js` statt `barcode-scanner.js` als
  Scan-Handler, stellt die Teil-7/Teil-8-Grenze (`GetAllByFaAndOperationAsync` + Logging in Teil 7,
  Umstellung + UI in Teil 8) verbindlich dar (Aufloesung KP2-2 laut Vorgabe) und verweist auf die
  zwei verbleibenden offenen Fragen.
- Akzeptanzkriterien um die korrigierte Aufloesungsreihenfolge, den NurFA-Fix, die Kombigeraete-
  Grenze und einen Audit-AK (ADR 0003, fuer den Fall einer neuen persistierten Entitaet) erweitert.
- Deploy-Abschnitt um die KP-6-Testdaten-Vorbedingung (Schranke-2-Vorbedingung, leeres
  IDEAL-Testsystem) ergaenzt.

**Offen fuer die naechste Runde:** KP2-2 (Teil-7/8-Grenze) ist laut Vorgabe verbindlich aufgeloest
und daher **nicht** mehr als offene Rueckfrage gefuehrt — die Teil-7-Spec muss den Liefergegenstand
`GetAllByFaAndOperationAsync` + Logging aber noch selbst tragen (dortige Pflege, nicht Teil dieser
Datei). KP2-3 (Granularitaet je BDE-Modus) und KP2-6 (Scope der Gruppenaufloesung) sind **echte,
ungeloeste** Fragen und stehen jetzt in `open_questions`/„Offene Rueckfragen" — sie muessen vor
Schranke 1 beantwortet werden, bevor Etappe 2 startet. KP2-5 (irrefuehrender Satz zu
`barcode-scanner.js`) ist mit der Neuformulierung im Loesungsentwurf erledigt. KP2-9 (Teil-7-Rumpf
traegt noch die widerlegte „Index 2 = BelID"-Behauptung) bleibt Teil-7-Pflege und ist hier nicht
behoben.
