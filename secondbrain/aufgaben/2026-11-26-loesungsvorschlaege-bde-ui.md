---
type: aufgabe
status: offen
created: 2026-11-26
version: 1.99.80
source: "[[2026-11-26-besprechung-0830-kritische-pruefung]]"
---
# Lösungsvorschläge BDE/Oberfläche (U-1 … U-14, H-A, H-B, H-4, H-6, F-20 … F-30)

Auftrag Gerald 2026-11-26: „zu allen Punkten Lösungsvorschläge recherchieren, die Fragen gehen wir später step by step durch“. Nur gelesen,
**kein Code geändert**. Basis: Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, HEAD `3c5a97df` (v1.99.80). Pfade ohne Präfix
beziehen sich auf `IdealAkeWms/` im Worktree. Quellen: [[2026-11-25-kommission-lagerplatz-bde-goal-lauf]] (Sammel-Fixes, UAT 2),
[[2026-11-26-besprechung-0830-kritische-pruefung]], [[2026-11-25-bde-masken-vereinheitlichung-spec]],
[[2026-11-20-bde-liste-isolierungsfraese-pfad-material-spec]]. DEV-Abfragen lesend gegen `.`/IDEAL_WMS_DEV und AKE_WMS_DEV.

Leitlinie: Ponytail-Leiter (kleinste tragfähige Lösung, Vorhandenes wiederverwenden). Melden statt still behandeln wird nirgends gekürzt.

## Übersicht

| Punkt | Empfehlung (kurz) | Entscheid nötig? | Schnitt |
|---|---|---|---|
| U-2 FEHLER | Gruppen-Dialog reparieren: Sub-FA/Werkbank aus `BdeKopfBuchungen`, eingegebene Menge übernehmen, leere Felder Pflicht, Unter-Soll-Rückfrage, `modal-xl` | ja (A/B/C) | v1.99.81 |
| B-2 / H-A | Wächter an Pause/Finish/FinishGroup/ReportPartial/CloseOthers: eigenes Terminal + Buchung gehört dem Werker | ja | v1.99.81 |
| H-B / H-5 | fester deutscher Text, Details nur ins Log | nein | v1.99.81 |
| U-1 | klare Meldung mit laufender Buchung + Zeile oben markieren | ja | v1.99.81 (A) |
| U-3 | Hinweis „Sollmenge erreicht – bitte Fertig melden“ auch in der FA-Sicht | nein | v1.99.81 |
| U-4 | Gutmenge Pflicht (`required`) | nein | v1.99.81 |
| U-5 | Werkbank immer zeigen; „Fortsetzen“ nur wo möglich, sonst Grund; Prod-Altbestand vorab zählen | ja | Teil v1.99.81, Rest Backlog |
| U-6 | Sperre erst beim Senden | nein | v1.99.81 |
| U-7 | ganzzahlig lassen, Gruppen-Dialog angleichen | ja (Fachfrage) | v1.99.81 |
| U-8 / H-4 | Buchung zeigen, wenn sie den Tag berührt | nein | v1.99.81 |
| U-9 | Meldebox bei jeder neuen Aktion leeren | nein | v1.99.81 |
| U-10 | Enter = Bestätigen | nein | v1.99.81 |
| U-11 | Aktion in der Karte nach oben (1 CSS-Zeile) + Spaltenwahl je Handy-Konto | ja | v1.99.81 (CSS), Rest ohne Code |
| U-12 | Texte/Zahlenformat glätten | nein | v1.99.81 |
| U-13 | Werker-Hinweis kurz und zuklappbar; Admin-Sicht im Backlog | ja | v1.99.81 (B) |
| U-14 | Hilfetext korrigieren | nein | v1.99.81 |
| H-6 | Vertrauensmodell als ADR festhalten, Bindung an Sitzung später | ja | ADR jetzt, Code Backlog |
| F-20 | so lassen (Spec-Beschluss), Kinder klar kennzeichnen | ja | — |
| F-21 | erledigt seit v1.99.75 (Rest statt Soll) | nein | Status nachziehen |
| F-22 | 0/0-Abschluss der Baugruppe nach Teilmeldungen erlauben | ja | v1.99.81 |
| F-29 | BdeTerminal-GETs jetzt löschen, API nach IIS-Log-Prüfung | ja | teils v1.99.81 |
| F-30 | zwei Fotos löschen | ja (klein) | v1.99.81 |

---

## U-2 (FEHLER) „Alle Produktionsaufträge abschliessen“

**Problem (Root Cause, belegt).** Nachgestellt mit DEV-Daten: Buchungen 44/45 (IDEAL_WMS_DEV) gehören zu **verschiedenen** Sub-FA 1067197 / 1067198
(AG 841/845, beide `EG Entgraten Fläche`, Haupt-FA 1060414), Mengen 42/43 = 0/0 mit `IsFinal = 1`.

1. **Zwei gleiche Zeilen ohne Sub-FA:**
   - Die Gruppen-Antwort liefert nur `OrderNumber`, `OperationNumber` und `Name` (`Controllers/BdeTerminalController.cs:156-164`).
   - Das JS baut daraus `orderNumber + '/' + operationNumber + name` (`wwwroot/js/bde-fa-sicht.js:152`).
   - `ProductionOrder.OrderNumber` ist bei IDEAL der **Haupt-FA**. Die Sub-FA steht in `SubOrderNumber`, wird hier aber nicht abgefragt.
   - Die vorhandene Abfrage `BdeKopfBuchungen.LaufendeAsync` liefert `SubOrderNumber` und `WorkplaceName` schon (`Services/BdeKopfBuchungen.cs:18-38`, F-35).
     Der Gruppen-Zweig hat eine eigene Kopie der Abfrage (`BdeTerminalController.cs:140-149`).
2. **Gutmenge 0 statt der eingegebenen 1:**
   - Der Werker tippt „Fertig“ im Kopf. Dann öffnet zuerst der Mengendialog, Eingabe 1 (`bde-fa-sicht.js:211-213`).
   - Der Server prüft in `Finish` die Gruppenpflicht **vor** jeder Mengenverarbeitung und antwortet `GroupFinishRequired`
     (`BdeTerminalController.cs:138-166`). `goodQty` wird nicht verwendet und nicht zurückgegeben.
   - `gruppe(json)` kennt die Eingabe nicht und belegt jedes Feld fest mit `'0'` (`bde-fa-sicht.js:158`).
   - Der Werker gibt also zweimal Mengen ein, und die erste Eingabe geht verloren.
3. **Keine Unter-Soll-Rückfrage:**
   - `gutOk` lief nur vor dem ersten POST, mit Gut 1 ≥ Soll 1 (`bde-fa-sicht.js:212`, `:116-122`).
   - Der Bestätigungsknopf der Gruppe sendet ohne Prüfung (`bde-fa-sicht.js:170-181`). `parseFloat(...) || 0` macht aus leer 0 (`:176-177`).
   - Der Service bucht jede Zeile mit `HasValue` als Menge, also auch 0/0 mit `IsFinal` (`Services/BdeBookingService.cs:394-399`).
     Eine Pflicht „Gut oder Ausschuss“ gibt es dort nicht. `ReportCompletionCoreAsync:298-299` hat sie.
4. **Tabelle abgeschnitten, Kontrast:**
   - `modal-lg` plus `form-control form-control-lg` je Mengenfeld in voller Spaltenbreite (`Views/Shared/_BdeDialoge.cshtml:24`, `bde-fa-sicht.js:158`).
     Ausschuss und „Abschl.“ laufen rechts aus dem Dialog (Screenshot `bde-alle-produktionsauftraege-abschliessen-ideal.png`).
   - „Abbrechen“ ist `btn-secondary` (`_BdeDialoge.cshtml:47`). Es erscheint hellblau mit weißer Schrift.
     Sonst nutzt die Seite für Nebenaktionen `btn-outline-secondary` (`Views/BdeTeileliste/_KopfBuchungen.cshtml:53`).
5. **Zusatzfund:**
   - Die Gruppe umfasst **alle** laufenden Produktionen des Werkers, ohne Filter auf Werkbank oder Terminal (`BdeTerminalController.cs:143-147`).
   - Ohne Werkbank-Spalte erkennt der Werker eine Buchung von einer anderen Werkbank nicht.

Wirkt nur mit `BdeGleichzeitigerAbschlussBeiMehrfachStart = true` und `BdeMehrfachBuchungProOperator = true`. Beide sind standardmäßig `false`
(`SQL/00_FreshInstall.sql:1095-1097`). Der Prod-Wert ist ungeprüft.

**Optionen**
- **A – reparieren (empfohlen).**
  - Dateien: `BdeTerminalController.cs` (Gruppen-Zweig nutzt `BdeKopfBuchungen.LaufendeAsync` und filtert auf Produktion, liefert Sub-FA und Werkbank;
    die eigene Abfrage entfällt), `bde-fa-sicht.js` `gruppe()`, `_BdeDialoge.cshtml`.
  - Anzeige je Zeile: `Haupt-FA · Sub-FA / AG Name`, Werkbank, Soll.
  - Mengen: Die angeklickte Buchung übernimmt die schon eingegebene Gut- und Ausschussmenge. Die anderen Zeilen sind leer und Pflicht
    (`required`, `reportValidity`).
  - Vor dem Senden eine Sammel-Rückfrage über `window.bdeBestaetigen`, die alle Zeilen mit Gut unter Soll nennt.
  - Darstellung: `modal-xl`, Mengenfelder mit fester Breite, „Abbrechen“ als `btn-outline-secondary`.
  - Aufwand M (ca. 60 Zeilen). Risiko gering: Der Zweig ist nur mit beiden Schaltern aktiv.
  - Test: Unit-Test (Gruppen-Antwort enthält Sub-FA und Werkbank). TS-198.7 erweitern um zwei Sub-FA mit gleichem AG-Namen, Vorbelegung,
    leeres Feld abgewiesen und Unter-Soll-Rückfrage.
- **B – Gruppenpflicht ersatzlos streichen.**
  - Rung 1: Muss das existieren? Standard aus, kein bekannter Nutzer.
  - Dateien: Gruppen-Zweig in `Finish`, `FinishGroup`, `FinishGroupAsync`, Modal, JS `gruppe()`, AppSetting samt Definition und Hilfe
    (`Views/Help/Index.cshtml:2166`).
  - Aufwand M (überwiegend Löschen plus Doku). Risiko: Ist der Schalter auf Prod an, verschwindet eine Funktion.
  - Vorher Prüfabfrage an beiden Prod-DBs (siehe unten).
- **C – Schalter auf Prod aus lassen, Reparatur im Backlog.**
  - Kein Code. Risiko: Jemand schaltet ihn ein, dann entstehen sofort 0/0-Fertigbuchungen.
  - Mindestens ein Warnhinweis in der Einstellung wäre nötig.

**Prüfabfrage für Gerald (Prod AKE und IDEAL):** `SELECT [Key],[Value] FROM dbo.AppSettings WHERE [Key] IN (N'BdeGleichzeitigerAbschlussBeiMehrfachStart', N'BdeMehrfachBuchungProOperator')`

**Empfehlung A.** Die Funktion ist dokumentiert und im Hilfetext beworben. Die Reparatur nutzt die vorhandene Kopf-Abfrage und den vorhandenen
Bestätigungsdialog. Ergibt die Prüfabfrage an beiden Standorten `false` und ist kein Bedarf bekannt, ist B die ehrlichere Wahl: weniger Code
statt reparierter Code.

> **Frage an Gerald:** Der Dialog „Alle Produktionsaufträge abschliessen“ bucht falsche Mengen und zeigt gleich aussehende Zeilen. Er ist nur aktiv,
> wenn „gemeinsamer Abschluss“ eingeschaltet ist. Was tun?
> 1. Reparieren: Sub-FA und Werkbank je Zeile, Menge übernehmen, leere Felder Pflicht, Rückfrage bei Gut unter Soll **(empfohlen)**
> 2. Funktion samt Einstellung entfernen (nur wenn an beiden Prod-Standorten aus)
> 3. Nur ausgeschaltet lassen, Reparatur später

---

## B-2 / H-A Terminal-Wächter auf Pause, Fertig, Teilmenge, Gruppe, Andere beenden

**Problem.**
- `FremdesTerminalAsync` (`BdeTerminalController.cs:275-281`) steht nur an den Start-, Fortsetzen- und Fertigmelden-Wegen (`:56/75/83/91/106/192`).
- `Pause` (`:97`), `Finish` (`:125`), `ReportPartial` (`:244`) nehmen nur `bookingId`. Es gibt weder eine Terminal- noch eine Werkerprüfung.
- `FinishGroup` (`:221`) prüft nur die vom Client gesendete `operatorId` (`BdeBookingService.cs:380`).
- `CloseOthers` (`:267`) beendet **alle** fremden Buchungen eines beliebigen AG (`BdeBookingService.cs:405-426`).
- Die FA-Sicht sendet bei diesen Aufrufen weder `terminalId` noch `operatorId` (`bde-fa-sicht.js:207/210/213/214/216`). Nur CloseOthers und
  FinishGroup senden `operatorId`.

**Optionen**
- **A – Wächter plus Besitzprüfung (empfohlen).**
  - Ein privater Helfer `FremdeBuchungAsync(bookingId, operatorId, terminalId)` = `FremdesTerminalAsync(terminalId)` plus
    `booking.BdeOperatorId == operatorId`.
  - An `Pause`, `Finish`, `ReportPartial`.
  - `FinishGroup`: zusätzlich `FremdesTerminalAsync`.
  - `CloseOthers`: `FremdesTerminalAsync` plus „der Werker hat auf diesem AG eine beendete Buchung“. Damit kann nur schließen, wer gerade selbst
    fertig gemeldet hat.
  - JS sendet `operatorId` und `terminalId` überall mit.
  - Dateien: `BdeTerminalController.cs`, `bde-fa-sicht.js`, `IdealAkeWms.Tests/Controllers/BdeTerminalControllerTests.cs` (Muster der F-28-Tests).
  - Aufwand S–M. Risiko gering. Buchungen, die an einem anderen Terminal gestartet wurden, bleiben beendbar, weil der Werker zählt und nicht das
    Start-Terminal. Das passt zu „Meine Buchungen“, die werkbank-übergreifend zeigt (`BdeKopfBuchungen.cs:18-26`).
  - Test: Unit-Tests „fremdes Terminal → abgelehnt, nichts geändert“ und „fremde Buchung → abgelehnt“ je Aktion. Live wie F-28 (DEV AKE mit
    gesendetem Terminal 1).
- **B – streng: nur Buchungen des eigenen Terminals** (`booking.BdeTerminalId == eigenes`).
  - Gleiche Dateien, Aufwand S.
  - Risiko: Wer an Terminal X startet und an Terminal Y fertig meldet, wird abgewiesen. Das ist heute ein erlaubter Ablauf.
- **C – nichts bauen, Mailsatz korrigieren** auf „Starten nur vom eigenen Terminal“.
  - Kein Code. Das Restrisiko (fremde Buchungen pausieren oder beenden) bleibt.

**Empfehlung A.** Das ist der kleinste Fix, der die Zusage in der Mail wahr macht. Er nutzt den vorhandenen Wächter und blockiert keinen
erlaubten Ablauf. Die Grenze: `operatorId` selbst bleibt unauthentisiert (siehe H-6).

> **Frage an Gerald:** Sollen Pause, Fertig, Teilmenge, Gruppen-Abschluss und „andere Buchungen beenden“ dieselbe Terminal-Prüfung bekommen wie
> das Starten?
> 1. Ja, eigenes Terminal + Buchung muss dem angemeldeten Werker gehören (Ende an anderem Terminal bleibt möglich) **(empfohlen)**
> 2. Ja, streng: nur Buchungen, die an diesem Terminal gestartet wurden
> 3. Nein, nur den Satz in der Mail korrigieren

---

## H-B / H-5 Technische Fehlertexte (`ex.Message`) beim Werker — kein Entscheid nötig, wird so gebaut

**Problem.**
- `BdeTerminalController.cs:119`, `:182`, `:239` senden „Serverfehler: {Typ} — {ex.Message}“ an den Client.
- `Controllers/PickingLeitstandController.cs:584` zeigt `({ex.Message})` im Altbestand-Hinweis.
- Geloggt wird überall schon (`:114`, `:181`, `:238` bzw. `:583`).

**Lösung.**
- Fester Text, zum Beispiel „Serverfehler – nichts sicher gebucht. Seite neu laden und Buchung prüfen; Details im Protokoll (hh:mm).“ bzw.
  „FA x: Kommissionieraufträge konnten nicht angelegt werden. Der Nachtlauf holt sie nach; Details im Protokoll.“
- Die Meldung bleibt sichtbar, also weiter „melden statt still“. 4 Zeilen, Aufwand S, Risiko keins.
- Test: Unit-Test „Exception → Text ohne Exception-Typ“.

Zusatzfund (nicht BDE, nicht im Schnitt):
- `ex.Message` erscheint auch auf Admin-Masken: `ServiceSettingsController.cs:126/238`, `SageFaArbeitsgaengeController.cs:112`,
  `Api/WarehouseRequisitionsApiController.cs:104/158`, `PickingController.cs:472`.
- Dort lesen Fachadmins. Prüfen, ob das Fachmeldungen (Validierung) oder Interna sind. Nur melden.

---

## U-1 Produktion an zweiter Zeile starten, während eigene Produktion läuft

**Problem.**
- Der Service antwortet `QuantityRequired(production)`, wenn eine eigene Produktion läuft und keine Mehrfachbuchung erlaubt ist
  (`Services/BdeBookingService.cs:99-106`; gleich bei der Tätigkeit `:124-127`).
- Die FA-Sicht zeigt nur „Mengen-Eingabe erforderlich – nichts gebucht.“ (`bde-fa-sicht.js:86`).
- Die betroffene Buchung (`bookingId` in `MapResult`, `BdeTerminalController.cs:283-291`) wird nicht genutzt.

**Optionen**
- **A – klare Meldung + Zeile markieren (empfohlen für v1.99.81).**
  - Text: „Es läuft noch Ihre Produktion <FA / AG>. Erst oben unter „Meine Buchungen“ Pause oder Fertig melden, dann neu starten.“
  - Die Kopfzeile `.bde-kopf-zeile[data-booking-id=…]` wird markiert und angesprungen (Text und Rahmen, nicht nur Farbe). Ihr `data-bez` liefert den
    Namen, also braucht es keine Serveränderung.
  - Datei `bde-fa-sicht.js`. Aufwand S. Risiko keins.
  - Test: TS-Zeile „zweite Zeile starten“, Text und Markierung prüfen.
- **B – geführter Wechsel.**
  - Dialog „Laufende Produktion X pausieren und Y starten?“ → Mengendialog → Pause → Start.
  - Datei `bde-fa-sicht.js` (Verkettung, Fehlerfall nach der Pause beachten). Aufwand M.
  - Risiko: Scheitert der Start nach erfolgreicher Pause, ist der Zustand halb gewechselt. Das muss sichtbar gemeldet werden.

**Empfehlung A jetzt, B als Backlog, falls der Morgenfall häufig ist.** A beseitigt die Ratlosigkeit ohne neue Abläufe.

> **Frage an Gerald:** Wenn ein Werker einen zweiten Arbeitsgang startet, während seine Produktion noch läuft – was soll passieren?
> 1. Klare Meldung „erst Pause oder Fertig melden“ und die laufende Buchung oben markieren **(empfohlen)**
> 2. Gleich anbieten: laufende Produktion pausieren (mit Mengen) und den neuen Arbeitsgang starten
> 3. So lassen

---

## U-3 Teilfertig über der Sollmenge ohne Hinweis — kein Entscheid nötig, wird so gebaut

**Problem.**
- `ReportPartialQuantityAsync` speichert ohne Blick auf das Soll (`BdeBookingService.cs:353-364`).
- Die Teileliste meldet für denselben Fall „Sollmenge erreicht – bitte Fertig melden“ (`Services/BdeTeilelisteService.cs:658`).

**Lösung.**
- In `ReportPartialQuantityAsync` die Summe Gut aller nicht stornierten Mengen des AG plus die neue Menge mit `ProductionOrder.Quantity` vergleichen.
  Ist sie ≥ Soll, als Ergebnis-Nachricht denselben Text zurückgeben.
- `bde-fa-sicht.js` hängt `json.message` an „Teilmenge gemeldet.“ an.
- Speichern bleibt erlaubt, wie in der Teileliste (Antwort 18 der Spec). Aufwand S. Risiko gering.
- Test: Unit-Test Soll 1, Teil 5 → Erfolg + Hinweistext.

---

## U-4 Leere Gutmenge wird still 0 — kein Entscheid nötig, wird so gebaut

**Problem.**
- `inputGood` hat kein `required` (`Views/Shared/_BdeDialoge.cshtml:8`).
- `parseFloat(gut.value || '0')` (`bde-fa-sicht.js:105`) macht aus leer 0.
- Pause bucht so 0/0 (DEV: BdeBookingQuantities 33 an Buchung 36).

**Lösung.** `required` am Feld. `reportValidity()` steht schon in `:104`. Eine Zeile, Muster wie der Teileliste-Dialog. Wer nichts geschafft hat,
tippt 0. Test: TS-Zeile „Pause mit leerer Gutmenge → Pflichtfeld“.

---

## U-5 „Meine Buchungen“ zeigt Pausierte, die nie fortgesetzt werden können (AKE)

**Problem.**
- `PausierteAsync` zeigt jede pausierte oder automatisch pausierte Buchung des Werkers ohne Fortsetzung, werkbank-übergreifend und ohne Alter
  (`Services/BdeKopfBuchungen.cs:23-26`).
- Die Werkbank steht nur im zugeklappten Detail (`Views/BdeTeileliste/_KopfBuchungen.cshtml:70-71`, sichtbar nur mit „Zeiten und Details“).
- „Fortsetzen“ scheitert dann an der Familien- bzw. Abgeholt-Prüfung (`BdeBookingService.cs:196-202`).
- DEV AKE: 5 Buchungen Status 5 ohne Kind, älteste vom 03.07.2026. DEV IDEAL: 2 (Abfrage dieser Recherche).

**Optionen**
- **A – sichtbar machen (empfohlen für v1.99.81).**
  - Werkbank und Datum (nicht nur Uhrzeit) in der Pausiert-Zeile immer zeigen.
  - Meldetexte vereinheitlichen: „moeglich“ → „möglich“, doppelten Strich entfernen (`BdeBookingService.cs:572` und
    `ergebnis()` hängen beide „ – nichts gebucht“ an).
  - Dateien: `_KopfBuchungen.cshtml`, Text in `BdeBookingService`. Aufwand S.
- **B – zusätzlich „Abschließen“ statt „Fortsetzen“.**
  - Wenn der FA abgeholt ist oder die Werkbank nicht passt: Knopf „Abschließen“. Er setzt die Pausiert-Buchung auf beendet, ohne Mengen; die Zeit ist
    schon gebucht.
  - Dateien: `BdeKopfBuchungen` (Flag „fortsetzbar“), `_KopfBuchungen.cshtml`, neue kleine Aktion mit Wächter (siehe H-A).
  - Aufwand M. Risiko: Statuswechsel an Altbuchungen, Auswertung prüfen.
- **C – Altbestand einmalig bereinigen** (Admin unter BDE-Buchungen, kein Code).
  - Vorher an Prod zählen: `SELECT BdeOperatorId, COUNT(*), MIN(StartedAt) FROM dbo.BdeBookings b WHERE Status IN (2,5) AND IsCancelled = 0 AND NOT EXISTS (SELECT 1 FROM dbo.BdeBookings c WHERE c.ParentBookingId = b.Id) GROUP BY BdeOperatorId`

**Empfehlung A jetzt + Prüfabfrage C vor Prod; B ins Backlog**, falls die Zahl an Prod nennenswert ist.

> **Frage an Gerald:** Alte, automatisch pausierte Buchungen (FA längst abgeholt, andere Werkbank) stehen beim Werker mit „Fortsetzen“, das nie
> klappt. Was tun?
> 1. Jetzt Werkbank und Datum sichtbar machen, Altbestand an Prod zählen, „Abschließen“-Knopf später je nach Menge **(empfohlen)**
> 2. Gleich „Abschließen“ statt „Fortsetzen“ bauen, wenn Fortsetzen nicht möglich ist
> 3. Nur Altbestand per Admin bereinigen, Oberfläche so lassen

---

## U-6 „wird gesendet …“ schon bei offenem Mengendialog — kein Entscheid nötig, wird so gebaut

**Problem.** `window.wmsSperren(btn)` sperrt vor dem Dialog (`bde-fa-sicht.js:201`). Das betrifft Pause, Teilfertig und Fertig (`:205-213`).

**Lösung.** Bei den drei Dialog-Aktionen erst nach `mengen()` sperren. Der offene Bootstrap-Dialog verhindert den Doppelklick ohnehin. Die anderen
Aktionen sperren weiter sofort. Aufwand S. Test: UAT-Zeile 4 wiederholen, Zeile 10 (Doppelklick, genau 1 POST) muss grün bleiben.

---

## U-7 Mengen in der FA-Sicht nur ganzzahlig

**Problem.**
- `step="1"` (`_BdeDialoge.cshtml:8-9`), der Gruppen-Dialog aber `step="0.01"` (`bde-fa-sicht.js:158`).
- Die Teileliste nimmt `step="any"`.
- Beleg: DEV AKE 3 048 und IDEAL 194 Fertigungsaufträge, **keiner** mit nicht ganzzahliger Menge. Keine BdeBookingQuantities mit Nachkommastellen.

**Optionen**
- **A – ganzzahlig lassen, Gruppen-Dialog auf `step="1"` angleichen (empfohlen).** Eine Zeile. Die Teileliste bleibt `any` (Stücklistenmengen in
  m/kg).
- **B – überall `step="any"`.** Zwei Attribute. Risiko: Tippfehler wie 0,5 Stück werden angenommen.

> **Frage an Gerald:** Melden Werker an einem Arbeitsgang (FA-Sicht) jemals Teilmengen wie 2,5 (m, kg)?
> 1. Nein – FA-Sicht bleibt ganzzahlig, der Gruppen-Dialog wird angeglichen **(empfohlen)**
> 2. Ja – Dezimalmengen in allen BDE-Mengendialogen erlauben

---

## U-8 / H-4 Tageshistorie und Buchungen über Mitternacht — kein Entscheid nötig, wird so gebaut

**Problem.**
- Buchungen werden nur nach Start am Tag gefiltert (`Services/BdeTageshistorie.cs:40`).
- Buchung 36 (Start 25.11. 22:46, Ende 26.11. 06:20) fehlt deshalb am 26.11.

**Lösung.**
- Filter auf „berührt den Tag“ ändern: `b.StartedAt < bis && (b.EndedAt ?? jetzt) >= von`. Eine Zeile.
- In der Zeit-Spalte das Datum zeigen, wenn Start oder Ende nicht am Tag liegen (`Spalten["zeit"]`, `:92`).
- Für Betriebe ohne Nachtschicht ändert sich nichts. Mit Nachtschicht ist die Buchung an beiden Tagen sichtbar, an denen gearbeitet wurde.
- Aufwand S. Test: Unit-Test (InMemory) mit Buchung 22:00–06:00 erscheint an beiden Tagen. UAT-Zeile 13 wiederholen.

Zusatzfund: Die Spalte „Auftrag“ der Tageshistorie zeigt bei Buchungen nur `OrderNumber` ohne Sub-FA (`BdeTageshistorie.cs:44`, `:58`). Das ist
dieselbe Fehlerklasse wie U-2: An IDEAL sehen alle Sub-FA eines Haupt-FA gleich aus. Vorschlag: im selben Zug `SubOrderNumber` ergänzen (Muster
F-35, `_KopfBuchungen.cshtml:13`).

---

## U-9 Alte Meldungen bleiben stehen — kein Entscheid nötig, wird so gebaut

**Problem.** `melden()` ersetzt die Box (`bde-fa-sicht.js:18-28`). Ein Scan-Treffer (`markieren`, `:293-309`), ein Auswahldialog und ein neuer
Dialogstart leeren sie aber nicht. Die Erfolgsmeldung aus `sessionStorage` (`:52-55`) bleibt neben dem nächsten Dialog stehen.

**Lösung.** `box.replaceChildren()` am Anfang des Scan-Submit (`:317`) und der beiden Klick-Handler (`:196`, `:237`). Drei Zeilen, Aufwand S.

---

## U-10 Enter im Mengendialog — kein Entscheid nötig, wird so gebaut

**Problem.** Der Dialog hat kein `<form>`. Beide Knöpfe sind `type="button"` (`_BdeDialoge.cshtml:11`). Enter bewirkt nichts.

**Lösung.** In `mengen()` ein `keydown`-Handler am Modal: Enter → `btnQtyOk.click()` (der Klick läuft durch `reportValidity`). Aufwand S.
Test: Tastatur 0 + Enter → Rückfrage bzw. Buchung. Hinweis: Ein Hardware-Scanner mit Enter-Suffix bestätigt dann ebenfalls. Das ist gewollt, weil
es sich wie in der Teileliste verhält.

---

## U-11 Handy 390 px: lange Karten, Aktion ganz unten

**Problem.**
- Kartenmodus aus `wwwroot/css/bde.css:141-163`. Die Aktionszelle hat `order: 99` (`:160`), steht also am Kartenende.
- Jede Karte zeigt alle eingeblendeten Spalten.

**Optionen**
- **A – Aktion nach oben + Spaltenwahl je Handy-Konto (empfohlen).**
  - `order: 99` → `order: 0` (direkt nach dem Auswahl-Haken, `order: -1`). Eine CSS-Zeile.
  - Weniger Felder ohne Code: Die Zahnrad-Spaltenwahl blendet Zellen per Inline-`display:none` aus (`wwwroot/js/column-preferences.js:161-178`).
    Inline schlägt die Karten-Regel `display:flex`.
  - Sie wird je Web-Konto gespeichert. Für die Handy-Konten einmal FA, Sub-FA, AG, Matchcode und Status wählen.
  - Aufwand S. Test: 390 px, erste Karte zeigt die Knöpfe oben. Mit reduzierter Spaltenwahl Kartenhöhe messen.
- **B – feste Handy-Feldliste per CSS** (≤ 576 px nur bestimmte `data-col-key`).
  - Aufwand S–M. Risiko: Werker können ausgeblendete Felder nicht selbst zurückholen. Das widerspricht dem Zahnrad.

> **Frage an Gerald:** Auf dem Handy ist jede Arbeitsgang-Karte lang, die Knöpfe stehen ganz unten. Wie kürzen?
> 1. Knöpfe nach oben; die Felder je Handy-Konto über das vorhandene Zahnrad reduzieren **(empfohlen)**
> 2. Knöpfe nach oben und auf dem Handy fest nur FA/Sub-FA/AG/Matchcode/Status zeigen
> 3. So lassen

---

## U-12 Texte und Zahlenformat — kein Entscheid nötig, wird so gebaut

**Fundstellen.**
- „1.0000“: `data-rest` wird mit `InvariantCulture` aus `decimal(…,4)` gerendert (`Views/BdeTeileliste/Index.cshtml:163`), das JS übernimmt es
  roh (`wwwroot/js/bde-teileliste.js:36`). Lösung: `z.Rest.ToString("0.####", InvariantCulture)` → „1“.
- „Gemeldet: 1.“ ohne Matchcode: `BdeTeilelisteService.cs:538`. Lösung: bei genau einer Zeile Bezeichnung anhängen (`bezeichnung` liegt in
  `MeldeZeileAsync` vor).
- „1 Zeilen“: `bde-teileliste.js:97`, `:116`. „1 weitere Buchungen“: `bde-fa-sicht.js:139`. „Meldung(en)“: `Views/BdeTeileliste/_Kopf.cshtml:143`.
  „Kommissionierauftrag/-aufträge“: `PickingLeitstandController.cs:621`. Lösung: Singular/Plural per Ternär (je eine Zeile).
- „Eintraege“: `Views/Shared/_Pagination.cshtml:10/30/34` → „Einträge“.
  Achtung: Das Partial ist global. Nur Text, keine Logik („gemeinsames JS/Partial nur einmal anfassen“).

Aufwand S. Test: Sichtprüfung der Erfolgsmeldungen (UAT-Zeilen 17, 21, 23).

---

## U-13 Admin-Hinweis „Meldung(en) ohne passende Strukturzeile“ beim Werker

**Problem.**
- Der Hinweis erscheint bei `Waisen > 0` für jeden (`_Kopf.cshtml:140-146`).
- Das Web-Konto am Terminal ist das Terminal-Konto, nicht der BDE-Admin.
- Die Admin-Seite `BdeBookings/Teilmeldungen` kennzeichnet Waisen **nicht**: Grep auf „Waise“ in `Views/BdeBookings/` und
  `Controllers/BdeBookingsController.cs` findet nichts.
- Ein reines Ausblenden beim Werker würde das einzige Signal entfernen. Das verbietet die Hausregel „melden statt still“.

**Optionen**
- **A – Werker-Text kurz und zuklappbar.**
  - Text: „n Meldung(en) passen nicht mehr zur FA-Struktur – BDE-Admin informieren.“ als `<details>`, Link nur für
    `CanManageBdeAdminAsync()` (`Services/CurrentUserService.cs:161-162`).
  - Datei `_Kopf.cshtml` (+ ViewModel-Flag). Aufwand S.
- **B – Hinweis nur für BDE-Admin, dafür Waisen-Filter auf der Admin-Seite.**
  - Dateien: `_Kopf.cshtml`, `BdeBookingsController` (Teilmeldungen), Filter/Badge.
  - Aufwand M. Erst mit B ist das Signal beim Admin, ohne den Werker zu stören.

**Empfehlung A jetzt, B im Backlog.**

> **Frage an Gerald:** Der Hinweis auf „Meldungen ohne Strukturzeile“ richtet sich an den BDE-Admin, steht aber beim Werker. Wie lösen?
> 1. Jetzt kurz und zuklappbar beim Werker („BDE-Admin informieren“); später eine Waisen-Anzeige auf der Admin-Seite, dann nur noch dort **(empfohlen)**
> 2. Sofort nur noch für BDE-Admin anzeigen und die Admin-Seite im selben Lauf erweitern
> 3. So lassen

---

## U-14 Hilfetext Fräsen-Pfad — kein Entscheid nötig, wird so gebaut

`Views/Articles/_FraesenPfadPflegen.cshtml:11`: „UNC-Pfad, Y: = \\idealfs1\Daten$\Produktion\CAM; leer = kein Pfad“ →
„Vollständigen UNC-Pfad eintragen, z. B. \\idealfs1\Daten$\Produktion\CAM\… (nicht Y:\…); leer = kein Pfad“. Eine Zeile.
Abhängigkeit: Die Abbildung Y: → `\\idealfs1\Daten$\Produktion\CAM\` ist noch Rückfrage 15 (F-14). Den Text erst final machen, wenn die Antwort da ist.

---

## H-6 operatorId ist nicht authentisiert

**Problem.**
- Der Werker wird per Personalnummer gewählt. `operatorId` reist im Query bzw. POST mit.
- `BdeTeilelisteController` prüft nur „aktiver BDE-Mitarbeiter“ (`Controllers/BdeTeilelisteController.cs:13-14`, `:91-92`).
- `BdeTerminalController` prüft nicht einmal das: `Pause`, `Finish` usw. ohne `operatorId`, `FinishGroup` und `CloseOthers` übernehmen sie ungeprüft.
- Authentisiert ist nur das Web-Konto (Terminal, Windows-Anmeldung).

**Optionen**
- **A – als Vertrauensmodell festhalten (empfohlen).**
  - ADR „BDE-Werker: Personalnummer identifiziert, authentisiert nicht. Schutzgrenze ist das Terminal-Konto“.
  - Dazu H-A (Besitzprüfung) und eine Aktivprüfung `AktiverWerkerAsync` auch im `BdeTerminalController` (Muster vorhanden).
  - Aufwand S (ADR + wenige Zeilen in H-A).
- **B – Werker an die Sitzung binden.** Nach der Personalnummer-Anmeldung liegt `operatorId` serverseitig (Session/Cookie je Terminal) und nicht mehr
  im Query.
  - Dateien: alle BDE-Aktionen, `bde-fa-sicht.js`, Teileliste-Formulare, Werker-wechseln-Ablauf.
  - Aufwand L. Risiko: Zwei Tabs mit zwei Werkern am selben Terminal (heute erlaubt, UAT-Zeile 11) gehen dann nicht mehr.
- **C – PIN je Werker.** Aufwand L, dazu fachliche Klärung (Handschuhe, Scanner) nötig.

> **Frage an Gerald:** Die Personalnummer am Terminal ist heute nur Auswahl, kein Login. Soll das so bleiben?
> 1. Ja, bewusst so (ADR); Schutz über Terminal-Konto + Prüfung „Buchung gehört dem Werker“ (H-A) **(empfohlen)**
> 2. Nein, Werker serverseitig an die Sitzung binden (größerer Umbau, eigene Spec)
> 3. Nein, zusätzlich PIN je Werker (eigene Spec)

---

## F-20 CSV-Export der Teileliste enthält die Teile-Zeilen

**Problem.**
- Server-Mode exportiert jede `tr` ohne `td[colspan]` (`wwwroot/js/csv-export.js:105-113`, Server-Pfad ohne Include `:225`).
- Das betrifft auch die zugeklappten Kinder `tr.bde-tl-kind` (`Views/BdeTeileliste/Index.cshtml:73`).
- Die Spec hat das **bewusst so beschlossen**: „CSV: Kinder erscheinen als eigene Zeilen; die Spalte Vater-FA ordnet sie zu“
  ([[2026-11-25-bde-masken-vereinheitlichung-spec]], Abschnitt Markup).
- Die Matchcode-Zelle der Kinder beginnt im Export mit „↳Teil der Baugruppe:“ (Pfeil + `visually-hidden`, `Index.cshtml:104`, `cellValue` liest
  `textContent`, `csv-export.js:97`).

**Optionen**
- **A – so lassen (Spec-Beschluss), nur Kennzeichnung sauber (empfohlen).**
  - `data-csv-value="@n.Matchcode"` an der Kind-Matchcode-Zelle, damit Excel-Abgleiche nicht am Präfix scheitern. Die Zuordnung leistet Vater-FA.
  - Eine Zeile je View (Einzeln, Summiert). In der Anwenderdoku einen Satz ergänzen.
- **B – Kinder aus dem Export nehmen.**
  - `dataRows` überspringt `tr[data-csv-export="false"]` (eine Zeile im gemeinsamen `csv-export.js`, gleiche Opt-out-Benennung wie Tabelle und
    Spalte), dazu das Attribut an `tr.bde-tl-kind`.
  - Aufwand S. Risiko: Das gemeinsame Skript wirkt für alle Tabellen. Es ist aber nur Opt-in je Zeile, daher gering. Der Export ist dann ärmer als die
    Ansicht im aufgeklappten Zustand.

> **Frage an Gerald:** Der CSV-Export der Teileliste enthält auch die Teile unter jeder Baugruppe (eigene Zeilen, zugeordnet über Vater-FA) – so
> war es in der Spec beschlossen. Bleibt es dabei?
> 1. Ja, Kinder bleiben drin; Matchcode ohne „Teil der Baugruppe:“-Präfix exportieren **(empfohlen)**
> 2. Nein, nur Baugruppen-/Hauptzeilen exportieren

---

## F-21 Summen-Haken meldet volle Sollmenge — kein Entscheid nötig, bereits erledigt

**Beleg.**
- Seit v1.99.75 (E3-3) meldet der Summen-Haken abschließend mit **Rest**:
  - `data-soll` am Summen-Haken = `SummeSollmengeOffen` (`Views/BdeTeileliste/Summiert.cshtml:63`) = Summe `Rest`
    (`BdeTeilelisteService.cs:483`), am Einzel-Haken `z.Rest` (`Summiert.cshtml:107`, `Index.cshtml:97`).
  - Serverseitig gilt: Gut `null` bei Fertig = `Math.Max(0, soll − bisher Gut)` (`BdeTeilelisteService.cs:650`).
- UAT 2 Zeile 23 bestätigt den Text „… mit ihrer Restmenge fertigmelden“.
- **Aktion:** In der Lauf-Notiz F-21 auf „erledigt (v1.99.75 E3-3)“ setzen. Die Empfehlung der Besprechung („Rest statt Zielmenge“) ist damit
  umgesetzt.

---

## F-22 Baugruppe: „Fertig“ mit 0/0 nach Teilmeldungen wird für den Arbeitsgang abgewiesen

**Problem.**
- Die Teilmeldung erlaubt den Abschluss mit 0/0, wenn schon Meldungen da sind (`BdeTeilelisteService.cs:657`, `nullErlaubt: a.Abschliessend &&
  bisher.Count > 0`). Sie wird gespeichert (`:760-761`).
- Die Baugruppe ruft danach `ReportCompletionCoreAsync` (`:778`). Der weist 0/0 immer ab (`BdeBookingService.cs:298-299`).
- Folge: Hinweis „nur die Teilmeldung gespeichert“ (`BdeTeilelisteService.cs:785-786`). Der Sub-FA-Arbeitsgang bekommt **keine** Fertig-Buchung
  (`IsFinal`) und bleibt im BDE offen.
- Das ist kein Schönheitsfehler. Der AG-Status ist danach falsch.

**Optionen**
- **A – gleiche Regel im Kern (empfohlen).**
  - `ReportCompletionCoreAsync` erlaubt 0/0, wenn `istFinal` und am AG schon nicht stornierte Teil-Buchungen (`IsFinal = false`) liegen. Das ist
    dieselbe Bedingung wie in der Teileliste.
  - Eine Bedingung an `:298`. Aufwand S. Risiko gering: Die FA-Sicht-Fertigmeldung (`ReportCompletion`) erlaubt dann 0/0 ebenfalls nur nach
    Teilmengen, mit Unter-Soll-Rückfrage im JS.
  - Test: Unit-Test „Teil 5, dann Fertig 0/0 → IsFinal-Buchung“ und „0/0 ohne Vorgeschichte → abgewiesen“.
- **B – 0/0 auch in der Teileliste verbieten** (wie Baugruppe heute).
  - Risiko: Nach einer Übermeldung kann der Werker die Zeile nur mit einer erfundenen Menge schließen (Spec Antwort 18 sieht 0/0 genau dafür vor).

> **Frage an Gerald:** Nach Teilmeldungen darf ein Blechteil mit „Fertig 0/0“ abgeschlossen werden, eine Baugruppe nicht – ihr Arbeitsgang bleibt
> dann offen. Angleichen?
> 1. Ja, 0/0-Abschluss überall erlauben, aber nur wenn vorher schon Teilmengen gemeldet wurden **(empfohlen)**
> 2. Nein, 0/0 überall verbieten (Abschluss nur mit Menge)

---

## F-29 Verwaiste Endpunkte

**Beleg (Grep über `IdealAkeWms`, Views, JS, Tests).**
- Keine View und kein JS ruft `BdeTerminal/ActiveBookings`, `PausedBookings` (`BdeTerminalController.cs:251-264`) oder `StartProductionForOrder`
  (`:53-70`) auf. Auch nicht `api/bde/operator/{nr}`, `operator/{id}/active-booking`, `operator/{id}/today-history` und
  `available-operations/{id}` (`Controllers/BdeApiController.cs:55`, `:123`, `:151`, `:208`).
- Aufrufer sind nur Tests (`IdealAkeWms.Tests/Controllers/BdeTerminalControllerTests.cs:50-160`, `BdeApiControllerTests.cs`).
- `docs/TESTSZENARIEN.md` TS-90.11 nennt `available-operations`.
- Externe Aufrufer (Skripte, Geräte) sind aus dem Code **nicht** auszuschließen.
- Die Daten-GETs `ActiveBookings`/`PausedBookings` nehmen `operatorId` vom Client und haben keinen Terminal-Wächter.

**Optionen**
- **A – jetzt die zwei BdeTerminal-GETs löschen, API-Endpunkte nach IIS-Log-Prüfung (empfohlen).**
  - Die GETs liegen im Controller, den H-A ohnehin anfasst. Tests mitlöschen. Aufwand S.
  - API: Prüfabfrage für Gerald – IIS-Logs beider Prod-Sites der letzten 30 Tage nach `/api/bde/operator` und `/api/bde/available-operations`
    durchsuchen. Bei 0 Treffern im Backlog löschen, TS-90.11 anpassen.
  - `StartProductionForOrder` geht mit [[2026-11-25-bde-nur-fa-meldung-entfernen]].
- **B – alles in v1.99.81 löschen.** Aufwand M (Tests + TS). Risiko: ein unbekannter externer Aufrufer bricht ohne Vorwarnung.
- **C – alles stehen lassen.** Toter Code ohne Wächter bleibt.

> **Frage an Gerald:** Mehrere alte BDE-Schnittstellen werden von der Oberfläche nicht mehr benutzt. Löschen?
> 1. Die zwei Terminal-Abfragen jetzt; die `/api/bde`-Endpunkte erst, wenn die Prod-IIS-Logs 30 Tage lang keinen Aufruf zeigen **(empfohlen)**
> 2. Alles sofort löschen
> 3. Alles stehen lassen

---

## F-30 Unreferenzierte Fotos 11-02 / 11-03

**Beleg.** `docs/anwender/bilder/11-02-liste-haupt-fa-gewaehlt.jpg` und `11-03-scan-und-knoepfe.jpg` (Stand 13.11., zeigen das gelöschte Terminal).
`git grep "bilder/11-0" -- docs` trifft nur 11-01, 11-04 bis 11-07 (`docs/anwender/11-bde-terminal.md:40-104`).

**Optionen.**
- **A – löschen** (`git rm`, zwei Dateien). Die Nummern bleiben frei, damit keine Umbenennung der anderen Fotos nötig ist.
- **B – belassen.**

> **Frage an Gerald:** Zwei alte Anwender-Fotos des abgelösten Terminals werden nirgends mehr gezeigt. Löschen?
> 1. Ja, löschen (Nummern 11-02/11-03 bleiben frei) **(empfohlen)**
> 2. Nein, behalten

---

## Vorschlag Schnitt

### Fix-Lauf v1.99.81 (vor Prod Pflicht)
Pflicht, weil Datenfehler oder Zusage in der Mail:
- **U-2** (Variante je Entscheid; bei B/C mindestens Prüfabfrage + Warnhinweis)
- **H-A / B-2** (oder Mailsatz korrigieren, falls Variante 3)
- **H-B / H-5** (Interna nicht an Werker)
- **F-22** (AG bleibt sonst falsch offen)
- **U-4** (stille 0/0-Mengen)

Billig mitnehmen, gleiche Dateien (`bde-fa-sicht.js`, `_BdeDialoge.cshtml`, `BdeTerminalController.cs`, `_KopfBuchungen.cshtml`), je S:
- U-1 (A), U-3, U-5 (A), U-6, U-7 (A), U-8 + Sub-FA in der Tageshistorie, U-9, U-10, U-11 (CSS-Zeile), U-12, U-13 (A), U-14 (falls
  Rückfrage 15 beantwortet), F-20 (A, `data-csv-value`), F-29 (BdeTerminal-GETs), F-30.
- Nicht vergessen: Frontend-Skill (CLAUDE.md), Testszenarien TS-198.x/TS-201.x nachziehen, `qa-agent`, Version beider `AppVersion.cs`,
  Changelog. Keine Migration nötig (alle Punkte ohne Schema).

### Später (Backlog)
- U-1 B (geführter AG-Wechsel), U-5 B („Abschließen“ für nicht fortsetzbare Pausen), U-13 B (Waisen-Anzeige auf der Admin-Seite), H-6 B/C (Sitzung/PIN,
  eigene Spec), F-29 API-Endpunkte (nach IIS-Log-Prüfung), `StartProductionForOrder` mit [[2026-11-25-bde-nur-fa-meldung-entfernen]].
- `ex.Message` auf Admin-Masken (Zusatzfund H-B) sichten.
- Zusatzfund U-2: `FinishGroupAsync` und `FinishAsync` prüfen nicht, ob der AG schon eine `IsFinal`-Menge hat, im Gegensatz zu
  `ReportCompletionCoreAsync:301-303`. Doppelte Fertigmeldung desselben AG bleibt möglich. Melden, nicht mitbauen.

### Prüfabfragen vor Prod (Mensch)
1. `BdeGleichzeitigerAbschlussBeiMehrfachStart` / `BdeMehrfachBuchungProOperator` an AKE und IDEAL (U-2).
2. Pausierte Buchungen ohne Fortsetzung je Werker (U-5, Abfrage oben).
3. IIS-Logs `/api/bde/operator*`, `/api/bde/available-operations*` (F-29).
