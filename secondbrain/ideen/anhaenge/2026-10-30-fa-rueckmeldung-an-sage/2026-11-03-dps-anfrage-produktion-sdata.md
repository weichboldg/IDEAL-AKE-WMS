---
typ: anfrage
datum: 2026-11-03
an: DPS Business Solutions – ERP Professional Service (Ticket#7954779, Michael Wiesner; CC Markus Dobler)
status: beantwortet (DPS-Antwort 2026-11-04)
bezug: "[[2026-10-30-fa-rueckmeldung-an-sage]]"
---
# DPS-Anfrage: Produktion über SData – Grundsatzfragen vor Beauftragung

**Rahmen (Gerald, 2026-11-03):**
- Anfrage als Antwort im bestehenden Ticket#7954779 (dort kam am 11.06.2026 die Postman-Sammlung „AKE / Ideal – CWS Samples“, abgelegt unter
  `backlog/anhaenge/2026-07-29-Postman-Lagerbuchungen/`; nur Stammdaten-Abrufe und Lagerbuchung Zugang/Abgang).
- Nur Grundstruktur, grundsätzliche Fragen und Beispiele; **keine** IDEAL/AKE-spezifischen Details.
- SData-Zugang für weitere Installationen (IDEAL) und für das produktive Sage-System ist **noch nicht gekauft/lizenziert**; Beauftragung erst nach
  Klärung dieser Punkte. LagerbuchungService ist im WMS eingebaut, aber nur am Testsystem getestet.
- Grundlage der Fragen: [[sdata-befund-ake-test-2026-10-30]] §1–§9, [[sdata-2-0-spezifikation]].

**Änderungen gegenüber dem ersten Entwurf:** Frage 3 auf `trackingID` umgestellt (Schema: `syncOrAsync`, §9); alte Frage 9 (Entnahme mit FA-Bezug
über LagerbuchungService) gestrichen – durch das `$template` beantwortet: kein FA-Feld, kein UUID-Feld (§9).

## Text

Betreff: AW: [Ticket#7954779] Postman Collection – Erweiterung um Produktion (Rückmeldungen, FA-Struktur)

Hallo Michael,

danke nochmals für die Postman Collection aus dem Juni. Damit haben wir die Lagerbuchungen aus unserem WMS über
CommonWawiServices/LagerbuchungService am Testsystem erfolgreich getestet.

Als Nächstes möchten wir auch Rückmeldungen aus der Fertigung (BDE) über SData an die Sage-100-Produktion übergeben. Bevor wir den
SData-Zugang für weitere Installationen beauftragen, möchten wir klären, ob und wie das grundsätzlich geht.

Am AKE-Testserver haben wir die Schnittstelle lesend angesehen: CommonWawiServices hat keinen Produktions-Service, der Vertrag MasterData
liefert für jede Ressource HTTP 403, und über RealTimeData können wir FA-Kopf und -Status lesen, aber nicht die Struktur zwischen Haupt- und
Unter-FA.

A) Rückmeldungen schreiben
1. Gibt es einen SData-Weg für PPS-Rückmeldungen (Zeit-RM auf Arbeitsgang, Material-RM mit Lagerplatz, Teil- und Gesamtfertigmeldung,
   „Auftrag beendet“)? Wenn nicht: Könnt ihr einen Partner-Service analog zum LagerbuchungService liefern, der intern über die
   Rückmelde-Engine bucht, also mit allen Folgebuchungen für Lager, MEK, FIBU und Dispo? Mit welchem Aufwand und welcher Lizenz?
2. Alternativ MasterData/strFaBeleg.Sage.PPS mit „Execute“: Welche MethodNames gibt es, und ist eine davon eine Rückmeldung? Die 403 auf
   MasterData kommt mit der Meldung „Die Referenz zwischen dem Datensatz … und dem Element stimmt nicht überein.“ Ist das ein Rechte-,
   Lizenz- oder Konfigurationsthema?
3. Der LagerbuchungService und strFaBeleg … Execute sind im Schema als syncOrAsync deklariert. Erkennt Sage bei Aufrufen mit
   ?trackingID=<uuid> Wiederholungen und bucht sie nicht doppelt? Wenn nicht: Kann eine Rückmeldung eine externe ID mitführen, die wir
   später wieder lesen können? Liefert die Antwort die RueckmeldungID?
4. Wie reagiert der Service, wenn der FA in Sage geöffnet (gesperrt), beendet oder nicht eingelastet ist? Gibt es unterscheidbare Fehlercodes?
5. Erzwingt Sage, dass Unter-FAs vor dem Haupt-FA fertiggemeldet werden?

B) Fertigungsaufträge lesen
6. Die RealTimeData-Quellen dtsFaBelegStruktur, dtsFaVerursacherInfo und dtsFaPositionen liefern über SData immer leer, auch mit
   where-Bedingungen nach SData-2.0-Syntax. Offenbar fehlt ein Kontextparameter des Sage-Clients. Wie lesen wir über SData die Struktur
   Haupt-FA → Unter-FA und die FA-Positionen?

C) Grundsätzliches zum Ablauf
7. Welche Rückmelde-Grundlagen empfehlt ihr, wenn eine externe BDE zurückmeldet, damit Material nicht doppelt entnommen wird?
8. Was passiert mit Unter-FAs, die die Bedarfsprüfung beim Einlasten entfernt: endgültig gelöscht oder archiviert?

D) Bitte um Beispiele
Am meisten würde uns eine Erweiterung der Postman Collection helfen, gerne im selben Stil wie die Lagerbuchungs-Samples:
- je eine Zeit-Rückmeldung, Material-Rückmeldung und Fertigmeldung zu einem Unter-FA sowie „Auftrag beendet“,
- ein Abruf der FA-Struktur mit Haupt- und Unter-FAs,
- je eine Fehlerantwort (FA gesperrt, FA beendet, Menge zu hoch).

Wenn diese Punkte geklärt sind, können wir die Beauftragung angehen. Gerne auch in einem kurzen Termin.

Viele Grüße
Gerald

## Antwort DPS (von Gerald weitergegeben 2026-11-04)

Hinweis: DPS hat die Fassung mit **9 Fragen** beantwortet (Frage 3 ohne `trackingID`, Frage 9 LagerbuchungService noch enthalten), nicht die
Endfassung oben.

| # | Frage (kurz) | Antwort DPS (sinngemäß) |
|---|---|---|
| 1 | SData-Weg für PPS-Rückmeldungen / Partner-Service | **Ja, gibt es.** Preis klärt DPS. |
| 2 | `strFaBeleg … Execute`, 403 auf MasterData | Die von Sage gelieferten Elemente sind so nicht nutzbar und **von Sage nicht zur Nutzung freigegeben**. |
| 3 | Externe ID / RueckmeldungID | Antwort liefert die **RückmeldeID**. Eine externe ID kann **nicht** mitgegeben werden – machbar nur über **Anpassungsprogrammierung**. |
| 4 | FA gesperrt / beendet / nicht eingelastet | „Dem Service erstmal egal“ – Rückmeldungen sind unabhängig davon, ob ein FA offen ist. |
| 5 | Unter-FA vor Haupt-FA erzwungen? | **Nein.** |
| 6 | Struktur über RealTimeData | RealTimeData stammt von Sage und ist **für Endanwender nicht zu verwenden**. |
| 7 | Empfohlene Rückmelde-Grundlagen | Die Grundlagen können **im Service je Aufruf mitgegeben** werden und gelten dann unabhängig von der Sage-Einstellung. |
| 8 | Bedarfsprüfung: Unter-FA gelöscht/archiviert | PPS-Frage, nicht API → **über Consulting** klären. |
| 9 | LagerbuchungService mit FA-Bezug | LagerbuchungService und Rückmeldungen sind „zwei Paar Stiefel“. |
| D | Postman-Beispiele | Gibt es nicht; müsste erstellt werden (Aufwand). „Wenn gewünscht, tun wir das gerne.“ |

## Bewertung Koordination (2026-11-04)

1. **Schreibweg geklärt:** Weg A (Partner-Service über SData) existiert. Die SageBridge (Weg C) und `strFaBeleg` (Weg B) entfallen.
2. **RealTimeData und MasterData sind nicht freigegeben.** Folgen:
   - Entscheidung E-3 („Lesen wo möglich über SData“) ist so nicht haltbar → Lesen bleibt bei den SQL-Views; SData nur zum Schreiben.
   - Die SData-FA-Ansicht v1.71.0 liest `dtsFaBelegsuche` aus RealTimeData ([[2026-10-30-sage-sdata-fa-ansicht-spec]]) – nutzt also eine nicht
     freigegebene Quelle. Einordnung offen (Gerald).
3. **Idempotenz liegt beim WMS:** Ohne externe ID lässt sich nach einem Timeout nicht sicher feststellen, ob gebucht wurde. Optionen: Anpassung bei
   DPS beauftragen (externe ID) **oder** kein automatischer Retry ohne Antwort + Lesen der Rückmeldungen per SQL-View als Nachweis (Heuristik).
4. **Fachliche Prüfungen liegen beim WMS:** Der Service prüft weder FA-Status (eingelastet/beendet) noch die Reihenfolge Unter-FA vor Haupt-FA.
   Das WMS muss beides selbst sicherstellen, sonst bucht es auf beendete FAs oder in falscher Reihenfolge.
5. **Rückmelde-Grundlagen je Aufruf:** Das WMS legt selbst fest, wie entnommen/fertiggemeldet wird. Die Werte sind eine Entscheidung im WMS
   (Doppel-Entnahme-Risiko R19/K-D) und gehören in die Spec.
6. **Materialverbrauch zum FA = Material-Rückmeldung,** nicht LagerbuchungService. Bestätigt R19: Die heutige freie Entnahme ohne FA-Bezug muss
   für FA-Material durch die Material-RM ersetzt werden.

## Offen nach der Antwort

- **2026-11-06:** DPS hat am AKE-Test 11 neue Services in `CommonWawiServices` installiert (Zeit-, Material-, Fertigmeldung u. a.) –
  Aufbau in [[sdata-befund-ake-test-2026-10-30]] §10. Damit beantwortet: Umfang und Grundlagen je Aufruf. Weiter offen: FA beenden, Sperre, Preis,
  Bedeutung `BacklogPrimaryKeys` (Idempotenz?), `IstEnde`, `CreateRueckmeldungenSubAuto`.

- Preis Service (DPS), Preis Beispiele, Preis Anpassung „externe ID“.
- Name/Vertrag des Service, Umfang (Zeit-RM, Material-RM, Teil-/Gesamtfertigmeldung, **Auftrag beenden**), welche Grundlagen je Aufruf.
- Verhalten bei in Sage geöffnetem (gesperrtem) FA – in Antwort 4 nicht beantwortet.
- Frage 8 → DPS-Consulting.
- Lizenz je Installation (AKE-Test, IDEAL, produktiv).
