---
type: spec
title: "Einbuchung: Standardmenge 1 statt 0"
slug: 2026-08-05-wms-bugs-improvements-teil-5-spec
status: Entwurf
created: 2026-08-05
updated: 2026-08-05
source_backlog: "[[2026-08-05-WmsBugs&Improvements]]"
depends_on: ""
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/StockMovementsController.cs
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Soll die Standardmenge 1 nur beim initialen GET-Aufruf der Einbuchungsseite gelten, oder auch nach jedem erfolgreichen Submit (RedirectToAction(nameof(Inbound)) fuehrt ohnehin zu einem frischen GET mit neuem ViewModel - vermutlich deckungsgleich, zur Sicherheit aber explizit abgefragt)?"
  - "Betrifft die Standardmenge-Aenderung NUR die Einzel-Einbuchung (dieser Teil) oder auch die geplante Mehrfachartikel-Einbuchung aus Teil 2 (dort waere je Zeile eine Standardmenge 1 sinnvoll, ist aber Gegenstand der separaten, dort noch offenen Spec)?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Beim Öffnen der Einbuchungsseite ist das Mengenfeld aktuell mit `0` vorbelegt. Da die meisten
Einbuchungen eine Menge von mindestens 1 betreffen und `0` ohnehin durch die bestehende
Validierung (`[Range(0.001, double.MaxValue)]`) nicht absendbar ist, spart ein Startwert von `1`
dem Anwender einen Tastatur-/Tipp-Schritt bei der häufigsten Eingabe.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Startwert des Mengenfelds auf der Einbuchungsseite (`GET /StockMovements/Inbound`) von `0` auf
  `1` ändern.

**Out-of-Scope**
- Keine Änderung an Ausbuchung oder Umbuchung (Backlog nennt explizit nur die Einbuchung).
- Keine Änderung an der Validierungsregel selbst (`[Range(0.001, double.MaxValue)]` bleibt
  unverändert).
- Keine Änderung am Verhalten bei ungültigen Formular-Rückgaben (`ModelState.IsValid == false`) —
  dort bleibt der vom Anwender zuletzt eingegebene Wert erhalten (Standard-ASP.NET-Model-Binding-
  Verhalten über `asp-for`).

## Fachliche Anforderungen

1. Beim erstmaligen Öffnen von `/StockMovements/Inbound` (GET, kein vorheriger Validierungsfehler)
   zeigt das Mengenfeld den Wert `1` statt `0`.

## Ist-Zustand (Code-Referenzen)

`IdealAkeWms/Controllers/StockMovementsController.cs:86-98` (`Inbound` GET):
```csharp
[RequireStockAccess]
public async Task<IActionResult> Inbound()
{
    var vm = new StockMovementCreateViewModel
    {
        StorageLocations = await _storageLocationRepository.GetActiveOrderedExcludingPickingTransportAsync(),
        Users = await _userRepository.GetActiveUsersAsync()
    };
    ...
    return View(vm);
}
```
`Quantity` wird nicht explizit gesetzt und bleibt beim `decimal`-Standardwert `0`.
`IdealAkeWms/Views/StockMovements/Inbound.cshtml:38-41`:
```html
<input asp-for="Quantity" class="form-control" type="number" step="0.001" min="0.001" />
```
Der `asp-for`-Tag-Helper rendert `value="@Model.Quantity"` — bei `Quantity == 0` erscheint `0` im
Feld.

## Technischer Lösungsentwurf

In `StockMovementsController.Inbound()` (GET) `Quantity = 1` im Objekt-Initializer ergänzen:
```csharp
var vm = new StockMovementCreateViewModel
{
    Quantity = 1,
    StorageLocations = await _storageLocationRepository.GetActiveOrderedExcludingPickingTransportAsync(),
    Users = await _userRepository.GetActiveUsersAsync()
};
```
Der POST-Handler (`Inbound(StockMovementCreateViewModel vm, ...)`) ist nicht betroffen — dort
kommt `Quantity` bereits vom Anwender/Model-Binder.

## Migrations-/SQL-Auswirkungen

Keine.

## Audit-Feld-Auswirkungen

Keine.

## Rollen- und Zugriffsfilter-Auswirkungen

Keine.

## Listen-View-Pattern-Pflichten

Nicht anwendbar (Eingabeformular, keine Liste).

## Akzeptanzkriterien

1. Beim ersten Öffnen von `/StockMovements/Inbound` zeigt das Mengenfeld den Wert `1`.
2. Ein Absenden ohne Änderung der Menge bucht eine `StockMovement` mit `Quantity == 1`.
3. Die Validierung (`Menge muss größer als 0 sein` bei `<= 0`) bleibt unverändert funktionsfähig.

## Test-Szenarien

Ergänzung `docs/TESTSZENARIEN.md` Kapitel 2 (Lager):
- **Schritte:** `/StockMovements/Inbound` öffnen.
- **Erwartetes Verhalten:** Mengenfeld zeigt `1`.
- **Negativfall:** unverändert — Menge auf `0` setzen und absenden → weiterhin
  Validierungsfehler „Menge muss größer als 0 sein".

`secondbrain/tests/testszenarien-index.md` Kapitel 2 entsprechend ergänzen.

## Deploy

- **Web-App:** ja (Ein-Zeilen-Controller-Änderung).
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:**

```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

## Offene Rückfragen

1. Soll die Standardmenge 1 nur beim initialen GET-Aufruf der Einbuchungsseite gelten, oder auch
   nach jedem erfolgreichen Submit (`RedirectToAction(nameof(Inbound))` führt ohnehin zu einem
   frischen GET mit neuem ViewModel — vermutlich deckungsgleich, zur Sicherheit aber explizit
   abgefragt)?
2. Betrifft die Standardmenge-Änderung NUR die Einzel-Einbuchung (dieser Teil) oder auch die
   geplante Mehrfachartikel-Einbuchung aus [[2026-08-05-wms-bugs-improvements-teil-2-spec]] (dort
   wäre je Zeile eine Standardmenge 1 sinnvoll, ist aber Gegenstand der separaten, dort noch
   offenen Spec)?

## Freigabe-Antworten (Mensch füllt aus — Schranke 1)

1. →
2. →
