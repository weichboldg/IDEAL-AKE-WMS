---
type: aufgabe
title: "IDEAL-Standort Teil 8 — Sub-FA-Rueckmeldung / BDE (Epic-Umsetzung)"
status: InUmsetzung
spec: "[[2026-07-29-standort-ideal-teil-8-spec]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
created: 2026-08-18
updated: 2026-08-18
---

# Teil 8 — Sub-FA-BDE (Epic)

Umsetzung der Spec [[2026-07-29-standort-ideal-teil-8-spec]] im **selben** Worktree wie Teile 1–5
und Teil 7 (`.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`).
Kein Zwischen-Merge — Teil 7 ist vollstaendig fertig (A–E, Testbereit), Startvoraussetzung laut
NACHTRAG 2026-08-12 erfuellt. Schranke 1 genommen (KP2-3/KP2-6 am 2026-08-12 entschieden).

## Etappen-Fortschritt

| # | Etappe | Status |
|---|--------|--------|
| 1 | Verifikation (Teil-7-Haertung im BDE-Kontext, OSEON-Urteil) | **erledigt** |
| 2 | Aufloesungslogik serverseitig + Unit-Tests | **erledigt** (`f86a886`) |
| 3 | Auswahl-UI am Terminal | offen |
| 4 | NurFA-Fix + Durchzug Teileverfolgung | offen |
| 5 | OSEON-Seite | **entfaellt** (Etappe-1-Urteil) |
| 6 | Tests + Brain-Update | offen |

## Etappe 1 — Verifikations-Urteil (2026-08-18)

Am realen Worktree-Code verifiziert: `WorkOperationRepository`/`IWorkOperationRepository`,
`BdeApiController.GetWorkOperation`/`GetAvailableOperations`, `BdeTerminalController.StartProductionForOrder`,
`bde-terminal.js` (`scanFaAgInput` + NurFA-Matching + Button-Handler), `OseonSyncService`,
`TrackingController`.

### 1. Teil-7-Naht vorhanden (KP2-2 aufgeloest)
- `IWorkOperationRepository.GetAllByFaAndOperationAsync(faNumber, operationNumber)` + Impl vorhanden
  (`WorkOperationRepository.cs:101` — mengenwertig, `OrderNumber == faNumber && OperationNumber ==
  operationNumber`, **kein** `Take`).
- `GetByFaAndOperationAsync` (`:84`) protokolliert Mehrfachtreffer (`Take(2)` + `LogWarning`), gibt
  weiterhin die erste Zeile zurueck → Terminal-Verhalten unveraendert. **Genau der in der
  Grenzformulierung zugesagte Liefergegenstand.** Teil 8 stellt die Aufrufer um.

### 2. Der EINE eindeutigkeitsannehmende BDE-Scan-Seam (Normal-Modus)
- `BdeApiController.GetWorkOperation(faNumber, opNumber)` (`:48`) → `_workOps.GetByFaAndOperationAsync`
  → **eine** WO. Split `faNumber.Split(',')[0]` steht **serverseitig** hier (nicht nur im JS).
  → **Etappe 2/3: auf `GetAllByFaAndOperationAsync` + Disambiguierung umstellen.**
- Granularitaet (KP2-3-Entscheidung, verbindlich): Auswahl **ueber Sub-FAs (ProductionOrder)**, nicht
  ueber WorkOperations. Bei mehreren WOs mit gleichem (`OrderNumber`,`OperationNumber`) → Auswahl der
  zugehoerigen Sub-FAs; der Arbeitsgang steht durch den Scan bereits fest.

### 3. NurFA-Modus (KP-5 / KP2-3) — der Fehlbucher
- `bde-terminal.js:scanFaAgInput` NurFA-Zweig (`:168-170`):
  `querySelectorAll('.bde-op-btn[data-type="fa"]').forEach(... if indexOf(faNumber) !== -1 faBtn = btn)`
  — **last-wins ohne `break`**. Button-Label ist `"{OrderNumber} — {Description1}"`
  (`BdeApiController:200`); mehrere Sub-FAs derselben `OrderNumber` → still greift der letzte.
- Zusatz-Vektor: `indexOf` ist ein **Substring**-Match auf dem ganzen `textContent` (inkl.
  Description1) → FA "100" matcht Button "1002 — …". Beim Fix mitbedenken.
- Fallback-API-Aufruf (`:176`) nutzt `opNumber=01` hart + `GetByFaAndOperationAsync` (Einzel) →
  gleicher Umstellungsbedarf.
- Button-Click-Handler (`:404-412`) bucht per `productionOrderId = btn.dataset.faId` via
  `StartProductionForOrder` → bereits **Id-basiert/eindeutig**. Die Auswahl-UI (Etappe 3) setzt hier auf.
- **Etappe 4: last-wins-Substring durch denselben Auswahlweg wie im Normal-Modus ersetzen.**

### 4. Auswahl-UI-Fundament vorhanden (KP-7 / KP2-6)
- `BdeApiController.GetAvailableOperations(workplaceId, operatorId)` (`:158`) liefert
  **werkbank-gescopte, Id-basierte** Liste — NurFA: offene ProductionOrders (`id=po.Id`,
  `ProductionWorkplaceId == workplaceId`); Normal: offene WorkOperations (`id=wo.Id`).
- KP2-6-Scope-Entscheidung (werkbank-gescopt) ist damit **deckungsgleich** mit dieser Liste. Etappe 3
  **filtert** diese Liste auf die gescannte `OrderNumber` — keine zweite Auswahl-Mechanik.

### 5. OSEON-Urteil → Etappe 5 ENTFAELLT
- Haupt-Sync `SyncOseonProductionOrdersAsync` arbeitet auf separater Spiegeltabelle
  `OseonProductionOrders` (Key `OseonOrderNumber`/`CustomerOrderNumber`) — **nicht** auf
  WMS-`ProductionOrders.OrderNumber`. Von der Inversion unberuehrt.
- Einziger WMS-`ProductionOrders`-Schreibpfad `SyncWorkplacesToProductionOrdersAsync` (`:506`) ist ein
  **set-based UPDATE-JOIN** `po.OrderNumber = oseon.CustomerOrderNumber` (`GROUP BY CustomerOrderNumber`,
  `WHERE po.ProductionWorkplaceId IS NULL`) — **keine Eindeutigkeitsannahme**, faechert korrekt auf
  alle Gruppenzeilen auf. Deckungsgleich mit Teil-7-AK-8-Einstufung „bleibt bewusst OrderNumber (Gruppen)".
- **Doku-Vorbehalt (kein Fehler):** Fehlt mehreren Sub-FAs derselben `OrderNumber` die Werkbank,
  erhielten alle dieselbe Gruppen-Werkbank aus OSEON. Da die Materialisierung (Teil 7 Etappe C) die
  Werkbank je Sub-FA setzt, ist das ein selten greifender Grob-Fallback, kein Regressionspfad.
- **Folge:** Etappe 5 als **entfallend** markiert (bedingte Etappe, Bedingung nicht eingetreten). Kein
  Service-Deploy allein aus Teil 8 noetig — `deploy.service` bleibt wg. Gesamtbuendel/Materialisierung.

### 6. Teileverfolgung (Etappe 4, Durchzug)
- `TrackingController` (`:63`, `:157`) nutzt `wo.ProductionOrder.OrderNumber.Contains(filter)` — reiner
  **Filter**, kein eindeutigkeitsannehmender Lookup. Nach der Inversion matcht ein OrderNumber-Filter
  alle Sub-FAs der Gruppe → genau das gewuenschte Gruppierungsverhalten. **Kein Codeaenderungsbedarf,
  nur Bestaetigung** im Etappe-4-Durchzug.

## Konkrete Restluecken-Liste (Eingang Etappe 2–4)
- **Etappe 2 (Server):** `GetWorkOperation` auf mengenwertigen Lookup umstellen; Aufloesungsergebnis =
  Menge der Sub-FA-Kandidaten, **werkbank-gescopt** (KP2-6). Zwei Randfaelle spezifizieren: (a) 0
  Treffer im Scope trotz existierendem HauptFA → eigene Meldung „Auftrag hat an dieser Werkbank keinen
  offenen Arbeitsgang"; (b) genau 1 Treffer im Scope trotz global mehrerer → Direktbuchung. Fallback
  `SubOrderNumber` als unerreichbare Vorruestung. Unit-Tests.
- **Etappe 3 (UI):** Auswahlliste auf `GetAvailableOperations` aufsetzen, auf gescannte `OrderNumber`
  filtern; Zeile = Sub-FA-Nummer + Matchcode/Bezeichnung + Arbeitsbereich; Klick bucht per `Id`.
- **Etappe 4:** NurFA `scanFaAgInput` von last-wins-Substring auf denselben Auswahlweg umstellen
  (inkl. Substring-Vektor); Teileverfolgung nur bestaetigen (kein Codeaenderungsbedarf).
- **Etappe 5:** entfaellt (s. o.).
- **Etappe 6:** Unit + Testszenarien (Kapitel „IDEAL Teil 8 — Sub-FA-BDE") + Brain.

## Etappe 2 — serverseitige Aufloesungslogik (2026-08-18, Worktree `f86a886`)

Neuer Service `IdealAkeWms/Services/BdeScanResolver.cs` (`IBdeScanResolver`), DI-registriert
(`Program.cs`). Reine serverseitige Aufloesung, **kein** UI, **kein** Eingriff in
`GetAvailableOperations`/`GetWorkOperation` (Etappe 3).

**API der Auflösung** — `ResolveAsync(workplaceId, scannedFa, operationNumber?, nurFaMode, operatorId?)`
→ `BdeScanResolution { Kind, Candidates[], ViaSubOrderNumberFallback }`:
- **Kind** = `Exact` (1 im Scope) / `Ambiguous` (mehrere) / `NotInScope` (0 im Scope, HauptFA global
  vorhanden) / `NotFound` (kein Order- und kein SubOrder-Treffer).
- **Candidate** = immer ein Sub-FA (KP2-3): `Target` = `WorkOperation` (Normal, Buchung per
  `workOperationId`) bzw. `ProductionOrder` (NurFA, per `productionOrderId`); traegt `OrderNumber`,
  `SubOrderNumber`, `Description`, `ArticleNumber`, `OperationNumber`/`OperationName` (nur Normal).
- Aufloesungsreihenfolge: OrderNumber-Gruppe (werkbank-gescopt) → NotInScope → SubOrderNumber-Fallback
  (AK 3, unerreichbare Vorruestung) → NotFound. FA-Segment-Split (`,`/`/`) serverseitig.
- **Scope (KP2-6):** werkbank-gescopt, deckungsgleich mit `GetAvailableOperations` (offen an der
  Werkbank, nicht verpackt/abgeholt, nicht aktiv gebucht, nicht final gemeldet). Predikate heute
  im Resolver dupliziert; **Etappe 3 konsolidiert `GetAvailableOperations` auf den Resolver** (durch
  die Tests abgesichert) — dann Deckungsgleichheit per Konstruktion.

**Tests:** `IdealAkeWms.Tests/Services/BdeScanResolverTests.cs`, 14 Faelle — Normal Exact/Ambiguous,
NurFA Exact/Ambiguous, beide KP2-6-Randfaelle (Exact-in-Scope-trotz-global-mehrerer; NotInScope-trotz-
HauptFA), Aktiv-/Verpackt-/Done-Ausschluss, Komma-Split, AKE-Flachfall, SubOrder-Fallback, NotFound,
leerer Scan. Web-Suite **1200 gruen** (+14).

**Eingang Etappe 3 (UI):** `GetAvailableOperations` auf `BdeScanResolver` umstellen; JS `scanFaAgInput`
(Normal) auf einen `resolve-scan`-Endpoint statt `/workoperation` legen; bei `Ambiguous` Auswahlliste
(Sub-FA-Nr + Matchcode + AG), bei `NotInScope` die eigene Meldung, bei `Exact` Direktbuchung. Endpoint
+ Controller-Verdrahtung gehoeren in Etappe 3 (BdeApiController-Ctor-Aenderung dort).

## Verbleibende Schranke-2-/UAT-Vorbedingung
Testdaten: AK 2/6 (zwei Sub-FAs derselben `OrderNumber` getrennt buchen) und AK 3 (Fallback
`SubOrderNumber`) sind ohne produktivnahe hierarchische Rueckmeldedaten im (heute leeren)
IDEAL-Testsystem nicht gruen zu bekommen — vor Schranke 2 sicherstellen.
