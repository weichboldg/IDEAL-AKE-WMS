# BDE Phase 2.2 — Mehrfachanmeldung + Zeit-Split

## Zusammenfassung

Zwei neue AppSettings lockern die bisher strikten Ein-Operator-Ein-AG-Constraints:

- `BdeMehrfachBuchungProOperator` — ein Mitarbeiter darf mehrere parallele Buchungen haben (auf verschiedenen Arbeitsgängen)
- `BdeMehrfachBuchungProArbeitsgang` — ein Arbeitsgang darf mehrere parallele Buchungen haben (durch verschiedene Mitarbeiter)

Beide Defaults `false` → Phase-1-Verhalten bleibt nach Deploy unverändert.

Die DB-seitigen Filtered UNIQUE Indizes werden gedroppt und als non-unique Lookup-Indizes neu angelegt. Die Enforcement wandert konditional in den `BdeBookingService`. Ein neuer read-only Service `BdeTimeSplitService` berechnet pro-Segment-Zeit-Splits on-the-fly, weil Roh-Dauern bei Parallel-Buchungen die tatsächliche Arbeitszeit doppelt zählen würden.

## Motivation

Real-world Produktionsumgebungen haben Szenarien, die Phase 1 nicht abdeckt:

1. **Ein MA bedient mehrere Maschinen** (z.B. eine Fräse und eine Bohrmaschine gleichzeitig). Muss sich auf beide FAs einstempeln können.
2. **Mehrere MA arbeiten gemeinsam an einem Auftrag** (z.B. Montage-Team). Müssen sich alle auf denselben AG anmelden können.

Beide Szenarien erfordern:
- Relaxieren der Ein-Buchung-Constraints (konfigurierbar, damit Phase-1-Kunden nicht gezwungen werden)
- Korrekte Zeiterfassung: die Summe aller effektiven Buchungszeiten muss der tatsächlichen MA-Arbeitszeit entsprechen (keine Doppelzählung bei Parallelität)
- UX für die Gesamt-Mengen-Meldung wenn mehrere MA denselben AG bearbeitet haben

## Nicht in Scope

- Keine Stundenlisten-Report pro MA (Phase 2.5+)
- Kein Schicht-Kalender / Auto-Pause (Phase 2.3)
- Keine neuen Cockpit-Varianten (Phase 2.4)
- Kein Versions-Bump (bleibt v1.8.2)
- Keine Rückmeldung an Sage/OSEON mit Effektiv-Zeit (später)
- Keine persistierte Effektiv-Zeit-Spalte (bewusst on-the-fly)

## Settings

### Neue AppSettings

| Key | Default | Beschreibung |
|-----|---------|-------------|
| `BdeMehrfachBuchungProOperator` | `false` | Ein Mitarbeiter darf mehrere parallele Buchungen haben (auf verschiedenen Arbeitsgängen) |
| `BdeMehrfachBuchungProArbeitsgang` | `false` | Ein Arbeitsgang darf mehrere parallele Buchungen haben (durch verschiedene Mitarbeiter) |

Beide als Boolean-Toggle im Settings-UI, unter der bestehenden BDE-Gruppe. Beide Toggles sind ausgegraut wenn `BdeAktiv = false` (UX-Hinweis, kein harter Block).

### Interaktions-Matrix

| ProOperator | ProArbeitsgang | Verhalten |
|:-:|:-:|---|
| `false` | `false` | Phase 1 (1 aktive Buchung pro MA, 1 MA pro AG) |
| `true` | `false` | MA parallel auf mehreren AGs, aber jeder AG exklusiv |
| `false` | `true` | Mehrere MA pro AG, aber jeder MA nur auf 1 AG |
| `true` | `true` | Volle Flexibilität |

### Beziehung zu Phase 1

- Wenn `BdeAktiv = false` sind beide Settings wirkungslos (BDE-Modul komplett aus).
- `BdeNurFaMeldung` (Phase 1) funktioniert unverändert mit beiden neuen Settings — der NurFA-Modus erzeugt pro FA einen Default-AG, und das Multi-Booking-Verhalten gilt dann auf diesem AG wie auf jedem anderen.

### Sonderregeln

- **Setup (Rüsten) bleibt IMMER single-operator-single-AG**, unabhängig von beiden Settings. Grund: Arbeitsschutz und fachlich-logischer Vorgang (Rüsten ist einmalig vor Produktionsbeginn).
- **Activity (ungeplante Tätigkeit) bleibt IMMER single-active pro Operator**, unabhängig von `BdeMehrfachBuchungProOperator`. Grund: Aktivitäten wie Meeting/Wartung/Schulung sind semantisch mit paralleler Produktion inkompatibel.

## Schema-Änderungen

### Gedroppte Indizes

```sql
DROP INDEX [IX_BdeBookings_BdeOperatorId_Active] ON [dbo].[BdeBookings];
DROP INDEX [IX_BdeBookings_WorkOperationId_Active] ON [dbo].[BdeBookings];
```

### Ersatz-Indizes (nicht unique)

Beide Filter-Expressions und Index-Namen bleiben identisch — nur `UNIQUE` entfernt:

```sql
CREATE INDEX [IX_BdeBookings_BdeOperatorId_Active]
    ON [dbo].[BdeBookings] (BdeOperatorId)
    WHERE EndedAt IS NULL AND IsCancelled = 0;

CREATE INDEX [IX_BdeBookings_WorkOperationId_Active]
    ON [dbo].[BdeBookings] (WorkOperationId)
    WHERE EndedAt IS NULL AND IsCancelled = 0;
```

Lookup-Queries (Collision-Check für Setup, Parallel-Booking-Lookup) nutzen sie weiter.

### Keine Entity-Änderungen

`BdeBooking`, `BdeBookingQuantity`, `WorkOperation` und `ProductionOrder` bleiben strukturell unverändert. Kein neues Feld für Effektiv-Zeit.

### Migration

EF-Migration `RelaxBdeBookingConstraints`: DROP + CREATE der beiden Indizes.

SQL-Script `SQL/45_RelaxBdeBookingConstraints.sql`: idempotent:
- `IF EXISTS (... AND is_unique = 1)` → DROP + CREATE als non-unique
- `__EFMigrationsHistory`-Insert mit `IF NOT EXISTS` Guard

`SQL/00_FreshInstall.sql`: bestehende CREATE-Statements anpassen (`UNIQUE` entfernen).

## BdeBookingService — konditionale Enforcement

Die Constraints wandern aus den Indizes in den Service. Alle Service-Methoden, die neue Buchungen erzeugen (`StartPlannedAsync`, `StartActivityAsync`, `ResumeAsync`), prüfen die Settings am Start der Transaktion und skip-pen Checks konditional.

### Ableitung der Enforcement-Regeln

Die beiden Settings wirken **ausschließlich auf Production-Buchungen**:

- `BdeMehrfachBuchungProArbeitsgang` steuert, ob eine laufende Production-Buchung eines **anderen** Operators auf derselben WorkOperation als Collision gilt.
- `BdeMehrfachBuchungProOperator` steuert, ob eine laufende Production-Buchung des **eigenen** Operators `QuantityRequired` erzwingt.

Setup- und Activity-Starts folgen unverändert der Phase-1-Logik (mit einer Ausnahme: der Setup-Collision-Check ignoriert `BdeMehrfachBuchungProArbeitsgang` — Setup bleibt 1-Operator-pro-AG).

### `StartPlannedAsync` — Setup (type = Setup)

Unverändert gegenüber Phase 1, mit einer Präzisierung:

- **Collision-Check** (anderer Operator hat laufende Buchung auf dieser WorkOperation): bei Kollision → `BdeBookingResult.Collision(existing)`. **Unabhängig von `BdeMehrfachBuchungProArbeitsgang`** — Setup ist immer exklusiv pro AG.
- **Auto-Close der eigenen Buchung**: Phase-1-Logik. Insbesondere: wenn eigener Operator aktive Production(s) hat → `QuantityRequired` (Setup nicht erlaubt, solange Production läuft).

### `StartPlannedAsync` — Production (type = Production)

**Collision-Check** (anderer Operator hat laufende Buchung auf derselben WorkOperation):
- `BdeMehrfachBuchungProArbeitsgang = false` → Collision abweisen
- `BdeMehrfachBuchungProArbeitsgang = true` → überspringen

**Eigener-Operator-Check** — unterteilt in **terminal** (beendet die Logik) und **kumulativ** (können mehrfach feuern):

**Terminal-Regeln (schließen den Flow):**
1. (T) Wenn Operator aktive Setup-Buchung auf **demselben AG** hat → Setup schließen, `ParentBookingId` setzen, Production starten. Fertig. (Setup→Production-Transition, unabhängig von Settings.)

**Kumulative Regeln (werden beide bei Bedarf durchlaufen, BEVOR Regel 4 greift):**
2. (K) Wenn Operator aktive Setup-Buchung auf **anderem AG** hat → diese Setup-Buchung schließen. Weiter zu Regel 3.
3. (K) Wenn Operator aktive Activity-Buchung hat → Activity schließen. Weiter zu Regel 4.

**Terminal-Regel (entscheidet das Ergebnis):**
4. (T) Wenn Operator nach Regel 2+3 noch aktive Production-Buchung(en) hat:
   - `BdeMehrfachBuchungProOperator = false` → `QuantityRequired` (eine der bestehenden Productions muss zuerst mit Mengen beendet werden)
   - `BdeMehrfachBuchungProOperator = true` → keine Auto-Close, die neue Production wird parallel angelegt
   
   Wenn Operator keine Production hat (nach Regel 2+3 nur noch leer) → neue Production wird angelegt.

**Hinweis zur Reihenfolge:** Regel 1 wird als Erstes geprüft, weil Setup→Production-Transition auf demselben AG fachlich anders ist als Auto-Close. Falls Regel 1 nicht zutrifft, werden Regel 2 und 3 **beide** durchlaufen (ein Operator kann vor Rule 4 eine Setup auf AG-X und eine Activity gleichzeitig haben — das sollte im Normalfall durch Phase-1-Enforcement nicht entstehen, könnte aber bei Setting-Aktivierung während laufender Buchungen vorkommen). Erst dann greift Regel 4.

### `StartActivityAsync`

Activity bleibt **single-active pro Operator** unabhängig von Settings:
- Wenn Operator aktive Production hat → `QuantityRequired`
- Wenn Operator aktive Setup hat → Setup schließen
- Wenn Operator aktive Activity hat → diese Activity schließen, neue starten
- Sonst → neue Activity starten

### `ResumeAsync`

Resume legt eine neue Buchung an, die Felder (WorkOperationId, BdeActivityId) vom pausierten Parent übernimmt und `ParentBookingId` setzt. Die Enforcement richtet sich nach dem Buchungstyp der neuen Buchung:
- **Production-Resume**: Collision-Check + Eigener-Operator-Check wie in `StartPlannedAsync — Production`
- **Setup-Resume**: Collision-Check streng, Eigener-Operator-Check wie in `StartPlannedAsync — Setup` (Phase-1-Logik)
- **Activity-Resume**: Eigener-Operator-Check wie in `StartActivityAsync` (Phase-1-Logik, single-active)

### Neue Methode `CloseOtherBookingsOnWorkOperationAsync`

```csharp
public async Task<CloseOthersResult> CloseOtherBookingsOnWorkOperationAsync(
    int workOperationId,
    int exceptOperatorId)

public record CloseOthersResult(int ClosedCount);
```

- Findet alle aktiven, nicht-stornierten Buchungen auf `workOperationId` mit `BdeOperatorId != exceptOperatorId`
- Beendet jede mit `EndedAt = DateTime.Now`, `Status = Finished`
- Erzeugt **keine** `BdeBookingQuantity`-Zeilen (die Gesamt-Gutmenge wurde bereits vom reporting-MA erfasst)
- Transaktional, setzt Audit-Felder (`ModifiedAt/By/ByWindows`)
- Return: `CloseOthersResult(ClosedCount)` — wie viele Buchungen wurden tatsächlich geschlossen

### `FinishAsync` und `PauseAsync`

Keine Änderung. Mengen-Reporting und Pause-Semantik sind schon heute pro Buchung unabhängig.

### NurFA-Modus — `StartProductionForOrder`

Der in Phase 1 eingeführte `BdeTerminalController.StartProductionForOrder`-Endpoint (NurFA-Modus) ruft intern `BdeDefaultWorkOperationService.FindOrCreateDefaultAsync` (um die Default-WorkOperation zu ermitteln) und anschließend `BdeBookingService.StartProductionAsync`. Die Enforcement-Regeln aus diesem Abschnitt gelten für den NurFA-Modus automatisch — keine eigene Logik nötig.

## BdeTimeSplitService (neu)

Read-only Service, berechnet pro-Segment-Split on-the-fly.

### Interface

```csharp
public interface IBdeTimeSplitService
{
    Task<IReadOnlyList<BookingSplit>> ComputeForOperatorDayAsync(int operatorId, DateTime day);
    Task<TimeSpan> ComputeEffectiveDurationAsync(int bookingId);
}

public record BookingSplit(int BookingId, TimeSpan EffectiveDuration);
```

### Algorithmus

**Eingabe:** Operator + Kalendertag.

**Schritt 1 — Buchungen laden:** Alle Buchungen des Operators, deren Zeit-Intervall `[StartedAt, EndedAt ?? DateTime.Now]` mit dem Tagesfenster `[day 00:00, day+1 00:00)` überlappt. Storniert-Flags ausgenommen.

**Schritt 2 — Zeitpunkte sammeln:** Alle `StartedAt` und `EndedAt` (bzw. für laufende `DateTime.Now`) dieser Buchungen werden zusammen mit Tag-Start und Tag-Ende als sortierte, distinct Zeitpunkt-Liste gesammelt.

**Schritt 3 — Segmente bilden:** Zwischen je zwei aufeinanderfolgenden Zeitpunkten entsteht ein Segment. Für jedes Segment wird die Liste der Buchungen ermittelt, deren Intervall das Segment enthält.

**Schritt 4 — Segment splitten:** Segment-Dauer auf die aktiven Buchungen verteilen:
- **1 aktive Buchung** → ganze Segment-Dauer an diese Buchung
- **N > 1 aktive Buchungen** → nach Gewichtung:
  - **Primär-Gewicht:** Summe der gemeldeten Gutmengen auf der Buchung (`BdeBookingQuantity.GoodQuantity`, alle Zeilen inkl. partial + final)
  - **Fallback 1 (wenn alle parallelen Buchungen Gesamt-Gutmenge 0 haben):** Sollmenge der FA (`ProductionOrder.Quantity` der WorkOperation → ProductionOrder-Referenz)
  - **Fallback 2 (wenn keine Sollmenge verfügbar):** gleichmäßig 1/N

**Schritt 5 — Aggregieren:** Pro Buchung werden die Segment-Anteile aufsummiert und als `BookingSplit` zurückgegeben.

### `ComputeEffectiveDurationAsync(bookingId)`

Convenience-Wrapper für Einzel-Buchungen. Die Buchung wird geladen; ihr Zeitintervall `[StartedAt, EndedAt ?? DateTime.Now]` kann mehrere Kalendertage spannen. Der Wrapper iteriert über alle Tage in diesem Span, ruft `ComputeForOperatorDayAsync` pro Tag auf und summiert die `BookingSplit`-Anteile derselben `bookingId` auf. Rückgabe: die Gesamt-Effektiv-Dauer über alle Tage.

### Pausierte Buchungen

Eine pausierte Buchung hat bereits einen `EndedAt`-Wert gesetzt (Phase-1-Konvention). Sie zählt damit als "beendetes Intervall" und ist in Segmenten nach dem Pausenzeitpunkt nicht mehr aktiv. Das Resume legt eine neue Buchung mit eigenem `StartedAt` an — die neue Buchung wird in der Split-Berechnung separat behandelt.

### Cross-Day-Buchungen

Eine Buchung die von 22:00 Tag-N bis 02:00 Tag-N+1 läuft wird bei der Split-Berechnung pro Tag separat behandelt. Der Teil vor Mitternacht zählt zum Tag N, der Teil danach zum Tag N+1. Jeweils gegen das Tagesfenster geclippt.

## Terminal-UI — Multi-MA-Abschluss-Dialog

### Flow

```
MA scannt "Fertig" → Mengen-Dialog (Gut, Ausschuss, ☐ Abschluss-Meldung)
    ↓
POST /BdeTerminal/Finish (mit IsFinal=true)
    ↓
Service beendet Buchung
    ↓
Controller prüft: BdeMehrfachBuchungProArbeitsgang aktiv + IsFinal=true + andere Buchungen aktiv auf dieser WO?
    ↓
 ┌─ NEIN → Erfolgs-Toast, fertig
 │
 └─ JA → Response enthält "otherActiveBookings": [{operatorId, operatorName, startedAt}, ...]
         → JS zeigt sekundären Dialog: "Auch beenden?"
              [Ja, alle beenden]  [Nein, nur meine]
         ↓
      JA → POST /BdeTerminal/CloseOthers { workOperationId }
           → Service.CloseOtherBookingsOnWorkOperationAsync(woId, currentOperatorId)
           → Erfolgs-Toast mit Count
      NEIN → Erfolgs-Toast für eigene Finish, andere laufen weiter
```

### Controller

**`BdeTerminalController.Finish`** (bestehend, erweitern):
- Nach erfolgreicher Service-Antwort: wenn beide Bedingungen erfüllt (`BdeMehrfachBuchungProArbeitsgang = true` + `IsFinal = true`), Query auf andere aktive Buchungen der selben WorkOperation mit anderem Operator.
- Response-DTO erhält neues Feld `OtherActiveBookings: [{OperatorId, OperatorName, StartedAt}]`.
- **Feld ist immer vorhanden im Response** (leeres Array wenn keine anderen aktiven Buchungen oder Bedingungen nicht erfüllt). Das vereinfacht das JS-Handling — `response.otherActiveBookings.length > 0` als einzige Prüfung.

**`BdeTerminalController.CloseOthers`** (neu):
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> CloseOthers(int workOperationId, int operatorId)
```
- `operatorId` = der Operator, der gerade die Abschluss-Meldung gemacht hat (dessen Buchung NICHT geschlossen werden soll)
- Ruft `_bookingService.CloseOtherBookingsOnWorkOperationAsync(workOperationId, exceptOperatorId: operatorId)`
- Rückgabe JSON `{ closedCount: N }`

Das Terminal-JS kennt `operatorId` aus dem laufenden Operator-Scan-Kontext und sendet ihn mit dem Request.

### Terminal-JS (`bde-terminal.js`)

Erweiterung des Finish-Response-Handlers:
- Wenn `response.otherActiveBookings?.length > 0`, Bootstrap-Modal öffnen mit Liste der anderen MAs
- Button "Ja, alle beenden" ruft AJAX-POST auf `/BdeTerminal/CloseOthers` mit der WorkOperationId
- Button "Nein, nur meine" schließt das Modal ohne Aktion

### Keine Änderung an

- Bestehender Mengen-Dialog (Gut/Ausschuss + IsFinal-Checkbox) bleibt identisch
- Pause-Flow, Operator-Scan-Flow, Cockpit-Polling

## Buchungsübersicht — Spalte "Effektive Zeit"

### Views

- **`BdeBookings/Index.cshtml`** — neue Spalte `<th data-col-key="effective-duration">Effektive Zeit</th>` zwischen "Ende" und "Operator". Anzeige als `HH:MM`. Bei laufenden Buchungen Suffix `*` + Tooltip "Wird nach Beenden angepasst".
- **`BdeBookings/Edit.cshtml`** — Effektiv-Zeit als read-only Display (nicht editierbar).

### ColumnDefinitions

```csharp
public static readonly ViewConfig BdeBookings = new(...)
{
    Columns =
    [
        new ColumnDef("started-at",          "Start"),
        new ColumnDef("ended-at",            "Ende"),
        new ColumnDef("effective-duration",  "Effektive Zeit"),  // NEU
        new ColumnDef("operator",            "Operator"),
        // ... rest unchanged
    ]
};
```

Default-Sichtbarkeit `true`, User kann via column-preferences ausblenden.

### ViewModel + Controller

`BdeBookingListViewModel` erhält ein neues Feld:

```csharp
public Dictionary<int, TimeSpan> EffectiveDurations { get; set; } = new();
```

`BdeBookingsController.Index` nach dem Laden der Buchungen:
- Pro Operator die Union aller Tage sammeln, die irgendeine seiner sichtbaren Buchungen spannt (inkl. Cross-Day-Buchungen: `StartedAt.Date` bis `(EndedAt ?? Now).Date`)
- Pro `(OperatorId, Tag)`-Kombination einmal `BdeTimeSplitService.ComputeForOperatorDayAsync` aufrufen
- Alle zurückgegebenen `BookingSplit`-Einträge pro `bookingId` **aufsummieren** (eine Buchung kann in mehreren Tagen einen Anteil haben) und das aggregierte Dictionary ins ViewModel einhängen

View nutzt `Model.EffectiveDurations.GetValueOrDefault(booking.Id, TimeSpan.Zero)` und formatiert als `hh\:mm`.

### Performance

- Typische Seitenansicht zeigt 25-100 Buchungen nach Filter
- Gruppen (Operator, Tag) typischerweise 5-20 pro Seite
- Kein Caching in Phase 2.2 — falls spürbar langsam nach Deploy, MemoryCache-Wrapper 5 Min ergänzen

## Tests

### Neu: `BdeTimeSplitServiceTests`

| Test | Szenario |
|------|----------|
| `SingleBooking_NoOverlap_ReturnsFullDuration` | 1 Buchung solo → Effektiv = Roh |
| `TwoBookings_FullOverlap_EqualQty_SplitsHalf` | 2 Buchungen 100% überlappt, je 5 Stk → 50/50 |
| `TwoBookings_FullOverlap_UnequalQty_SplitsByQty` | 2 Buchungen überlappt, 3 vs 2 Stk → 60/40 |
| `ThreeSegments_SoloParallelSolo_CorrectSums` | solo + parallel + solo → erwartete Summen stimmen |
| `RunningBooking_SplitsUntilNow` | Buchung ohne `EndedAt` → Split bis `DateTime.Now` |
| `FallbackToSollmenge_WhenNoGutmenge` | Beide Buchungen 0 Gutmenge → Split nach FA-Sollmenge-Anteil (`ProductionOrder.Quantity`) |
| `FallbackToEqual_WhenNoSollmenge` | Keine Gutmenge + keine Sollmenge → gleichmäßig |
| `PausedBookingIgnored` | Pausierte Buchung (EndedAt gesetzt) zählt nur bis zum Pausen-Zeitpunkt |
| `CrossDayBooking_ClippedToDay` | 22:00–02:00 → Tag N clipped bei 24:00, Tag N+1 ab 00:00 |
| `ComputeEffectiveDurationAsync_SumsAcrossDays` | Cross-Day-Buchung 22:00-02:00 → Wrapper summiert Anteile aus Tag N + Tag N+1 |
| `NoBookings_ReturnsEmpty` | Keine Buchungen am Tag → leere Liste |
| `CancelledBookingsIgnored` | Stornierte Buchungen werden nicht einbezogen |

### Erweiterung: `BdeBookingServiceTests`

| Test | Szenario |
|------|----------|
| `StartProduction_MultiMaDisabled_RejectsCollision` | Setting off, 2 Operatoren auf 1 AG → Collision wie Phase 1 |
| `StartProduction_MultiMaEnabled_AllowsSecondOperator` | Setting on → zweite Buchung wird angelegt |
| `StartProduction_MultiOperatorDisabled_RequiresQuantity` | Setting off, MA hat aktive Production → QuantityRequired |
| `StartProduction_MultiOperatorEnabled_AllowsParallel` | Setting on → parallele Production-Buchung |
| `StartSetup_AlwaysExclusive_EvenInMultiMaMode` | ProArbeitsgang=true + 2 MAs versuchen Setup → Collision |
| `StartSetup_AlwaysExclusive_PerOperator` | ProOperator=true + MA hat Setup + startet neues Setup → altes schließt |
| `SetupToProduction_SameAG_AlwaysTransitions` | Auch in Multi-Mode: Setup→Production auf selbem AG schließt Setup, setzt ParentBookingId |
| `StartActivity_AlwaysExclusive_PerOperator` | Activity bleibt single-active auch mit ProOperator=true |
| `CloseOtherBookings_FindsOtherOperatorsOnSameWo` | MA1+MA2+MA3 aktiv auf WO → Close(wo, MA1) → MA2+MA3 beendet, Count=2 |
| `CloseOtherBookings_SkipsCancelled` | Stornierte werden nicht berührt |
| `CloseOtherBookings_NoOthers_ReturnsZero` | Nur 1 aktiv → Count=0, kein Fehler |
| `CloseOtherBookings_SetsAuditFields` | Geschlossene Buchungen haben ModifiedAt/By/ByWindows |

### Erweiterung: `BdeTerminalControllerTests`

| Test | Szenario |
|------|----------|
| `Finish_MultiMaEnabled_ReturnsOtherActiveBookings` | Setting on + andere MA aktiv + IsFinal=true → Response-Feld gesetzt |
| `Finish_MultiMaDisabled_OmitsOtherActiveBookings` | Setting off → leer/weggelassen |
| `Finish_NotFinal_OmitsOtherActiveBookings` | IsFinal=false → weggelassen |
| `CloseOthers_ClosesSiblingBookings` | POST → delegiert an Service, Response-Count korrekt |

### Erweiterung: `BdeBookingsControllerTests`

| Test | Szenario |
|------|----------|
| `Index_PopulatesEffectiveDurations` | Controller gruppiert nach (Operator, Tag), ruft Service, füllt Dict |
| `Index_CrossDayBooking_AggregatesBothDays` | Cross-Day-Buchung → Dict-Wert = Summe aus beiden Tages-Aufrufen |
| `Index_EmptyBookings_EmptyDictionary` | Leere Liste → leeres Dict (kein NullRef) |

### Bestehende Tests anpassen

- `BdeBookingTestSeed` bleibt wie er ist (liefert BdeAktiv=true aus Phase 2.1)
- Alle bestehenden `BdeBookingServiceTests` laufen mit `ProOperator=false, ProArbeitsgang=false` (Phase-1-Verhalten) weiter — müssen nicht angepasst werden
- Neue Tests setzen explizit die Settings via Mock oder AppSettings-Seed

### Manueller UI-Test

1. Beide Settings aus → Phase-1-Verhalten unverändert
2. `...ProOperator=true` → MA startet FA-A, dann FA-B — beide laufen parallel
3. `...ProArbeitsgang=true` → MA1 bucht FA-A, MA2 bucht ebenfalls FA-A
4. Abschluss-Meldung auf Multi-MA-AG → Dialog "andere auch beenden?"
5. Dialog "Ja" → andere Buchungen enden, keine Gutmenge-Zeile erzeugt
6. Dialog "Nein" → nur eigene Buchung endet
7. Buchungsübersicht → Spalte "Effektive Zeit" zeigt sinnvolle Split-Zeiten
8. Pro-Segment-Split mit 3-Segment-Szenario (solo + parallel + solo) visuell verifizieren
9. Setup-Kollision bleibt hart auch bei `...ProArbeitsgang=true`
10. Activity bleibt single-active auch bei `...ProOperator=true`

## Migration + Seeding + Dokumentation

### EF-Migration

`dotnet ef migrations add RelaxBdeBookingConstraints` — DropIndex + CreateIndex Operationen für beide Indizes.

### SQL-Script

`SQL/45_RelaxBdeBookingConstraints.sql`:
- `IF EXISTS (... AND is_unique = 1)` → DROP + CREATE (non-unique)
- `IF NOT EXISTS` → nur CREATE (falls Ersatz schon da)
- `__EFMigrationsHistory`-Insert mit Guard

`SQL/00_FreshInstall.sql`: UNIQUE aus den beiden CREATE-Statements entfernen.

### AppSettings-Seeding

`Program.cs` — neue Defaults einfügen:

```csharp
new AppSetting {
    Key = "BdeMehrfachBuchungProOperator",
    Value = "false",
    Description = "Ein Mitarbeiter darf mehrere parallele Buchungen haben"
},
new AppSetting {
    Key = "BdeMehrfachBuchungProArbeitsgang",
    Value = "false",
    Description = "Ein Arbeitsgang darf mehrere parallele Buchungen haben"
},
```

Gleicher Block im SQL-Seed in `SQL/00_FreshInstall.sql`.

### Settings-UI

`Views/Settings/Index.cshtml`:

```csharp
("BDE", new[] {
    "BdeAktiv",
    "BdeNurFaMeldung",
    "BdeDefaultArbeitsgang",
    "BdeMehrfachBuchungProOperator",
    "BdeMehrfachBuchungProArbeitsgang"
})
```

Beide neuen Toggles sind UI-seitig ausgegraut wenn `BdeAktiv = false`.

### Dokumentation

- **`CLAUDE.md`:**
  - AppSettings-Tabelle um die 2 neuen Keys ergänzen
  - "Bekannte Fallstricke" → Eintrag über gelockerte Indizes und Service-Enforcement
  - Bestehender Eintrag "BDE-Buchung Ein-Operator-Regel" wird überarbeitet auf konditionale Enforcement
- **`Views/Help/Index.cshtml`:**
  - Neuer Unterabschnitt "Mehrfach-Anmeldung konfigurieren" (Schritt-für-Schritt)
  - Neuer Unterabschnitt "Zeit-Split erklärt" (Beispiel-Szenario mit Zahlen)
  - Troubleshooting-Eintrag "Warum unterscheiden sich Roh- und Effektiv-Zeit?"
- **`Views/Help/Changelog.cshtml`:** neuer Block innerhalb v1.8.2 für Phase 2.2
- **`IdealAkeWms/AppVersion.cs` + `IDEALAKEWMSService/AppVersion.cs`:** `Date` auf 2026-04-21 setzen
- **`PROJECT_STATUS.md`:** Phase 2.2 als abgeschlossen eintragen

### Kein Versions-Bump

Bleibt bei v1.8.2. Release-Cut erfolgt erst wenn Phase 2.3 + 2.4 ebenfalls durch sind.

## Betroffene Dateien (Überblick)

| Bereich | Dateien |
|---------|---------|
| Schema | EF-Migration `RelaxBdeBookingConstraints`, `SQL/45_RelaxBdeBookingConstraints.sql`, `SQL/00_FreshInstall.sql` |
| Seeding | `Program.cs`, `SQL/00_FreshInstall.sql` |
| Services | `Services/BdeBookingService.cs`, neues `Services/BdeTimeSplitService.cs` + `IBdeTimeSplitService` |
| Controller | `Controllers/BdeBookingsController.cs`, `Controllers/BdeTerminalController.cs` |
| ViewModels | `Models/ViewModels/BdeBookingListViewModel.cs` |
| Views | `Views/BdeBookings/Index.cshtml`, `Views/BdeBookings/Edit.cshtml`, `Views/Settings/Index.cshtml` |
| Column-Config | `Models/ViewModels/ColumnDefinitions.cs` |
| JS | `wwwroot/js/bde-terminal.js` |
| Hilfe/Doku | `Views/Help/Index.cshtml`, `Views/Help/Changelog.cshtml`, `CLAUDE.md`, `PROJECT_STATUS.md`, `AppVersion.cs` (Web + Service) |
| Tests | Neu: `BdeTimeSplitServiceTests`; erweitert: `BdeBookingServiceTests`, `BdeTerminalControllerTests`, `BdeBookingsControllerTests` |

## Offene Fragen

Keine — alle Design-Fragen wurden im Brainstorming geklärt.
