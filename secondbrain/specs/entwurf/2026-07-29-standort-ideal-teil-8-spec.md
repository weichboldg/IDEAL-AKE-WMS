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

1. →
2. →
3. →
