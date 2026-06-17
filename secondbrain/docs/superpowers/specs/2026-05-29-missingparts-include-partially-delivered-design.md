# MissingParts: PartiallyDelivered-Bestellungen mitzaehlen — Hotfix-Design v1.18.1

**Datum:** 2026-05-29
**Status:** Draft
**Branch:** `bugfix/missingparts-include-pd`
**Worktree:** `.claude/worktrees/missingparts-include-pd`
**Version-Ziel:** v1.18.1 (Hotfix)
**Vorgaenger:** v1.18.0 (Lagerbestellungen-Erweiterung)

---

## 1. Bug-Report

User markiert in einer Lagerbestellung ein Item als **Endgueltig Fehlteil** (Checkbox ✓). Die Bestellung bleibt in Status `PartiallyDelivered` weil ein anderes Item noch Restlieferung erwartet. Sowohl `/MissingParts` (Lager-Menue "Fehlteile") als auch die Werkbank-Karte "Meine Fehlteile" zeigen das Item NICHT an, obwohl es als endgueltig markiert ist.

**Erwartung des Users:** Sobald ein Item als endgueltig Fehlteil markiert wird, taucht es sofort in der Fehlteile-Liste auf — unabhaengig vom Bestell-Lifecycle.

**Beispiel:** Bestellung #15 (Status: Teilgeliefert) mit zwei Items:
- Item 1 `EK008285`: Bestellt=2, Ist=1, **IsFinalShortage=true** → soll im /MissingParts erscheinen
- Item 2 `00720623`: Bestellt=1, Ist=0, IsFinalShortage=false (noch Restlieferung erwartet) → soll NICHT erscheinen

## 2. Root Cause

In v1.18.0 wurde der Filter auf `Status == Closed` festgelegt:

[`WarehouseRequisitionRepository.GetMissingPartsAsync`](IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs):
```csharp
.Where(i => i.IsFinalShortage
    && i.WarehouseRequisition.Status == WarehouseRequisitionStatus.Closed);
```

[`WarehouseRequisitionRepository.GetFinalShortagesCountForUserAsync`](IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs):
```csharp
.Where(i => i.IsFinalShortage
    && i.WarehouseRequisition.Status == WarehouseRequisitionStatus.Closed
    && userWorkplaceIds.Contains(i.WarehouseRequisition.ProductionWorkplaceId));
```

Begruendung in v1.18.0-Spec: "PartiallyDelivered = noch in Bearbeitung, Lager kann Flag noch aendern, also nicht endgueltig". Diese Logik passt nicht zur User-Erwartung.

## 3. Fix

`Status == Closed` ersetzen durch `Status IN (Closed, PartiallyDelivered)`. Konkret:

```csharp
// Neuer Filter in beiden Methoden:
.Where(i => i.IsFinalShortage
    && (i.WarehouseRequisition.Status == WarehouseRequisitionStatus.Closed
        || i.WarehouseRequisition.Status == WarehouseRequisitionStatus.PartiallyDelivered));
```

**Cancelled** bleibt ausgeschlossen (storniert ≠ Fehlteil). **Submitted** bleibt ausgeschlossen (Bestellung noch nicht vom Lager bearbeitet).

## 4. Neues mentales Modell

Per-Item-Flag `IsFinalShortage` ist der ALLEINIGE "endgueltig"-Marker. Order-Status spielt fuer die Fehlteil-Sicht keine Rolle mehr:

| Order-Status | Item IsFinalShortage=true | Erscheint in MissingParts? |
|--------------|----------------------------|----------------------------|
| Draft | n/a (Lager hat noch nicht gepickt) | nein |
| Submitted | technisch moeglich, normal nicht | nein |
| **PartiallyDelivered** | **ja** | **ja (NEU)** |
| Closed | ja | ja |
| Cancelled | technisch moeglich | nein |

**Konsequenz fuer den Lager-Workflow:**
- Lager markiert einen Item als endgueltig Fehlteil → erscheint sofort in /MissingParts
- Wenn Lager die Markierung spaeter zuruecknimmt (Checkbox abhaken, Wert aendern) → Item verschwindet wieder
- Erst wenn alle anderen Items entweder vollstaendig geliefert oder ebenfalls als final markiert sind, wechselt der Status auf Closed — aber die Fehlteil-Liste hat das bereits abgebildet

## 5. Auswirkungen

**Tests (`WarehouseRequisitionRepositoryTests`):**
- `GetMissingPartsAsync_OnlyIncludesItemsWithIsFinalShortageTrue` — bisher: PartiallyDelivered-Bestellung → 0 Treffer. NEU: derselbe Setup gibt 1 Treffer (das Item mit IsFinalShortage=true). Test umflippen.
- Neuer Test: `GetMissingPartsAsync_IncludesPartiallyDeliveredFinalShortages` — PartiallyDelivered-Bestellung mit IsFinalShortage=true erscheint.
- Neuer Test: `GetFinalShortagesCountForUserAsync_IncludesPartiallyDelivered` — Counts inkludieren PartiallyDelivered.
- `GetMissingPartsAsync_ReturnsOnlyClosedRequisitions_WithFinalShortages` — Test-Namen + Erwartung anpassen: NEU: zeigt Closed+PartiallyDelivered, NICHT Cancelled/Submitted.

**Doku:**
- `CLAUDE.md` — Fallstrick "MissingParts zeigt nur ... Status=Closed (PartiallyDelivered-IsFinalShortages zaehlen noch nicht)" entfernen + neuen Fallstrick "MissingParts zeigt IsFinalShortage=true aus Closed UND PartiallyDelivered" hinzufuegen.
- `docs/TESTSZENARIEN.md` — Szenario 32.3 anpassen (vorher: PartiallyDelivered → Item nicht in Liste; neu: PartiallyDelivered + Item mit final-Flag → Item IST in Liste). Neues Szenario 32.11: Hotfix-Verifikation.
- `Views/Help/Changelog.cshtml` — neue v1.18.1-Card.
- `PROJECT_STATUS.md` — Hotfix-Notiz oben.
- `AppVersion.cs` (Web + Service) — `1.18.1` / `2026-05-29`.

**Vorhandener v1.18.0-Spec / Plan im secondbrain:** bleiben unveraendert (historisches Dokument). Hotfix-Spec referenziert sie aber.

## 6. Out-of-Scope

- UI-Aenderungen (Status-Badges, Buttons, Detail-View) — bleiben wie sie sind
- Datenmodell-Aenderungen (Enum, Spalten) — bleiben
- Sage-Sync / andere Module — irrelevant
- Server-Restart / Connection-Strings — irrelevant

## 7. Reihenfolge der Tasks (grob, Detail in writing-plans)

1. Pre-Flight Baseline auf neuem Branch
2. 2 LINQ-Filter aendern in `WarehouseRequisitionRepository.cs`
3. Tests aktualisieren / hinzufuegen (TDD)
4. CLAUDE.md, TESTSZENARIEN.md anpassen
5. AppVersion v1.18.1 + Changelog-Card + PROJECT_STATUS Hotfix-Notiz
6. Final-Check (Build + Tests)
7. Merge in main (NACH User-Bestaetigung)
8. Worktree-Cleanup (NACH User-Bestaetigung — siehe Memory-Feedback)

## 8. Risiken

- **Cascade Effect**: bisherige Werkbank-User die ihre eigene `/MissingParts?mineOnly=true` aufgerufen haben sehen jetzt evtl. mehr Eintraege als zuvor. Erwartet — das ist das gewollte Verhalten.
- **Race Condition bei wechselndem Flag**: Lager kann den Flag in PartiallyDelivered-Bestellung beliebig hin- und herwechseln. Each query reflektiert den aktuellen State. Akzeptiert.
- **Datums-Sortierung**: `OrderByDescending(i => i.WarehouseRequisition.ClosedAt ?? DateTime.MinValue)` sortiert PartiallyDelivered (kein ClosedAt) ans Ende. Nicht schoen, aber funktional ok. Alternative: ueber `CreatedAt` als Fallback sortieren — optional als Verbesserung (siehe Plan).
