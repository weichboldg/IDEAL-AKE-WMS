---
type: adr
id: 0009
title: App-Status liegt in Satelliten-Tabellen neben den Sage-Master-Daten des Fertigungsauftrags
status: accepted
date: 2026-05-12
supersedes: ""
superseded_by: ""
---

> **Nachtraeglich erfasst** (2026-07-27). Bestandsdokumentation; eingefuehrt in v1.11.0
> (Specs `production-order-split-phase-1/2/4`), Assembly-Groups-Teil in v1.22.0 durch
> `FaWorkSteps` abgeloest, `ProductionOrderExtraInfo` in v1.26.0 ergaenzt.

## Kontext und Problem

`ProductionOrders` wird von einem SQL-Agent-Job aus der Sage-View
`vw_AKE_Kommissionierung_WAListe` befuellt (MERGE). Gleichzeitig wollte die App eigene Zustaende
am Auftrag fuehren: Kommissionier-Freigabe, Prioritaet, zugewiesener Picker, Glas/Zukauf-Flags,
Beschichtung erledigt, BDE-erledigt. Solange diese Spalten in derselben Tabelle lagen, hat jeder
Sage-Import sie potenziell ueberschrieben — App-Eingaben verschwanden stillschweigend beim
naechsten Sync.

## Betrachtete Optionen

- **App-Spalten in `ProductionOrders` belassen und im MERGE ausklammern** — kein Schema-Umbau,
  aber der Schutz haengt an der Disziplin eines externen SQL-Jobs, der nicht im Repo-Review
  landet.
- **Kompletter eigener App-Auftragsbegriff (Kopie)** — volle Kontrolle, aber Duplikat-Pflege und
  Divergenz zur ERP-Wahrheit.
- **Sage-Master-Tabelle bleibt rein, App-Status in 1:1-/1:N-Satelliten** — der Import kann die
  Master-Tabelle beliebig ueberschreiben, ohne App-Daten zu beruehren.

## Entscheidung

`ProductionOrders` enthaelt **nur Sage-Master-Daten**. App-Status lebt in verbundenen Tabellen:

| Tabelle | Kardinalitaet | Inhalt |
|---|---|---|
| `ProductionOrderPickingStatus` | 1:1 | HasGlass, HasExternalPurchase, HasCoatingParts, IsCoatingDone, IsReleasedForPicking, PickingPriority, AssignedPicker, **IsDonePicking** |
| `ProductionOrderBdeStatus` | 1:1 | IsDoneBde |
| `FaWorkSteps` (+ `FaWorkStepSpecs`) | 1:N | tatsaechlich benoetigte Vorbau-Arbeitsgaenge je FA, deren Erledigt-Status und Spezifikationen |
| `FaAttributeValues` | 1:N | strukturierte Merkmalswerte je FA |
| `ProductionOrderExtraInfo` | 1:1 | read-only Sage-Zusatzinfos (Kaeltemittel, Ventil, Ausfuehrung, Maschine, SageStatus) |

Der AgentJob legt `PickingStatus` und `BdeStatus` ueber Folge-MERGEs eager an. `FaWorkSteps`
entstehen dagegen bedarfsgetrieben (Detection-Sync oder manuell), `ProductionOrderExtraInfo`
ausschliesslich durch den `FaZusatzinfoSyncService`.

Die entscheidende Konsequenzregel: **`ProductionOrder.IsDone` ist Sage-Master und wird von der
App nie beschrieben.** „Kommissionierung fertig" ist `PickingStatus.IsDonePicking`. Deshalb muss
**jede** Lese-/Filterstelle, die „FA erledigt" meint, `IsDone || IsDonePicking` pruefen — und
analog `!IsCancelled` fuehren (v1.25.0, FA-Reconciliation).

## Konsequenzen

**Positiv**
- Der Sage-Import kann die Master-Tabelle frei ueberschreiben; App-Eingaben sind strukturell
  geschuetzt (nicht nur per Konvention im SQL-Job).
- Neue App-Zustaende sind additiv (neuer Satellit oder neue Spalte im Satelliten), ohne den
  Import anzufassen.
- Toggle-APIs sind klar getrennt: `/api/picking-status/toggle`, `/api/fa-work-steps/toggle`,
  `/api/fa-work-steps/set-status`, `/api/bde-status/toggle`.

**Negativ / Risiken**
- **Die Lese-Seite ist die Schwachstelle.** Von v1.11 bis v1.21.0 hat keine Query
  `IsDonePicking` gelesen — der „Abschliessen"-Button war ein Jahr lang wirkungslos. Jede neue
  Offen-Query muss `IsDone || IsDonePicking` und `!IsCancelled` fuehren, in EF **und** in raw SQL.
- `.Include(o => o.PickingStatus)` ist Pflicht, sonst ist der Satellit null und das Flag wird
  nie ausgewertet. In Projektionen (`LeitstandOrderRow`) ist `.Include` wirkungslos — die Felder
  muessen in die Projektion.
- Der AgentJob ist Teil des Deploy-Vertrags: aenderte sich die Satelliten-Struktur, muss das Job-
  Skript im selben Wartungsfenster mitziehen (siehe [[0004-migrations-und-sql-disziplin]]).
- Mehr Joins pro Listenabfrage.
