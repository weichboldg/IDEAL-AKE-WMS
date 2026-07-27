# BDE-Settings — Feature-Toggle + Vereinfachter Modus

## Zusammenfassung

Zwei neue AppSettings steuern das BDE-Modul: `BdeAktiv` aktiviert/deaktiviert BDE komplett (Navigation + Zugriff), `BdeNurFaMeldung` schaltet einen vereinfachten Modus frei in dem Operatoren direkt auf Produktionsaufträge statt auf einzelne Arbeitsgänge buchen. Ein drittes Setting `BdeDefaultArbeitsgang` definiert den Arbeitsgang-Namen der im vereinfachten Modus automatisch pro FA angelegt wird.

## Motivation

BDE soll nicht automatisch bei jeder Installation aktiv sein — Kunden die nur Kommissionierung/Lager nutzen, sollen kein BDE-Menü sehen. Zusätzlich brauchen Betriebe ohne detaillierte Arbeitsgangplanung einen vereinfachten Modus: Operator stempelt auf den FA, nicht auf einzelne AGs.

## Neue AppSettings (DB-Tabelle)

| Key | Default | Beschreibung |
|-----|---------|-------------|
| `BdeAktiv` | `false` | BDE-Modul aktiviert (Navigation + Zugriff) |
| `BdeNurFaMeldung` | `false` | Vereinfachter Modus: Buchung auf FA statt AG |
| `BdeDefaultArbeitsgang` | (leer) | AG-Bezeichnung für auto-erstellte Arbeitsgänge (z.B. "PRODUKTION"). Pflicht wenn `BdeNurFaMeldung = true` |

Settings werden auf der bestehenden Settings-Seite als Toggle-Switches verwaltet (analog `LeitstandAktiv`, `TeileverfolgungAktiv`, `BestellungenAktiv`). `BdeDefaultArbeitsgang` als Textfeld, nur sichtbar/editierbar wenn `BdeNurFaMeldung = true`.

**Settings-UI Pattern:** `Views/Settings/Index.cshtml` nutzt gruppierte Tuple-Arrays `(string Title, string[] Keys)`. Neue Gruppe ergänzen: `("BDE", new[] { "BdeAktiv", "BdeNurFaMeldung", "BdeDefaultArbeitsgang" })`. Boolean-Werte werden automatisch als Toggle-Switches gerendert.

## Setting 1: `BdeAktiv` — Feature-Toggle

### Navigation

BDE-Dropdown in `_Layout.cshtml` wird nur angezeigt wenn **beide** Bedingungen erfüllt:
1. `BdeAktiv = true`
2. User hat mindestens eine BDE-Rolle (`bde_user`, `bde_shiftlead`, `bde_admin`) oder `admin`

Bei `BdeAktiv = false`: kein BDE-Menüeintrag, auch nicht für Admins.

### Zugriffssperre

Alle BDE-Controller prüfen `BdeAktiv`:
- `BdeTerminalController`
- `BdeCockpitController`
- `BdeBookingsController`
- `BdeMasterDataController`
- `BdeApiController`

Bei `BdeAktiv = false` → Redirect auf Home mit `TempData["WarningMessage"] = "BDE ist nicht aktiviert."`.

**Implementierung:** Neues Filter-Attribut `[RequireBdeActive]` (analog zu bestehenden Filter-Attributen). Liest das Setting aus der DB via `IAppSettingRepository.GetValueAsync("BdeAktiv")` — identisches Pattern wie `LeitstandAktiv` in `_Layout.cshtml`. Wird auf jedem BDE-Controller als erstes Attribut gesetzt, vor den Rollen-Filtern.

**Bestehendes Pattern in `_Layout.cshtml` (Referenz):**
```csharp
var leitstandAktiv = (await AppSettings.GetValueAsync("LeitstandAktiv"))
    ?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
```
BDE-Navigation analog: `var bdeAktiv = (await AppSettings.GetValueAsync("BdeAktiv"))?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;`

### Settings-UI

Toggle-Switch auf der Settings-Seite im Bereich "Module":
- Label: "BDE (Betriebsdatenerfassung)"
- Untertext: "Terminal-Buchung, Cockpit, Buchungsübersicht"

### Seeding

In `Program.cs` und `SQL/42_AddBde.sql` / `SQL/00_FreshInstall.sql`: `BdeAktiv = false` als Default.

## Setting 2: `BdeNurFaMeldung` — Vereinfachter Modus

### Kernkonzept

Im normalen Modus bucht der Operator auf einen **Arbeitsgang** (WorkOperation). Im vereinfachten Modus bucht er auf einen **Produktionsauftrag** (ProductionOrder). Technisch: das System erzeugt automatisch eine WorkOperation pro FA (mit dem konfigurierten Default-Namen) und bucht darauf. Der Anwender sieht davon nichts.

### Kein Schema-Bruch

Das bestehende `BdeBooking`-Datenmodell bleibt **unverändert**:
- `BdeBooking.WorkOperationId` zeigt auf die auto-erstellte WorkOperation
- Check-Constraints bleiben wie sie sind
- Keine neue Spalte, keine Migration

### Auto-Create Default-Arbeitsgang

Wenn `BdeNurFaMeldung = true` und ein Operator einen FA wählt:

1. System sucht bestehende WorkOperation mit `ProductionOrderId = FA` und `Name = BdeDefaultArbeitsgang`
2. Falls nicht vorhanden: neue WorkOperation wird angelegt:
   - `ProductionOrderId = gewählter FA`
   - `OperationNumber = "01"`
   - `Name = BdeDefaultArbeitsgang` (z.B. "PRODUKTION")
   - `ProductionWorkplaceId = aktuelle Werkbank des Terminals`
   - `Sequence = 1`
   - `IsReportable = true`
   - `CreatedBy = "BDE-AutoCreate"`, `CreatedByWindows = "BDE-AutoCreate"`
3. Buchung wird gegen diese WorkOperation erstellt (normale `BdeBookingService`-Logik)

**Ort der Logik:** Neuer Service `BdeDefaultWorkOperationService` mit Methode `FindOrCreateDefaultAsync(int productionOrderId, int workplaceId)`. Nicht im `BdeBookingService` (der bleibt WorkOperation-agnostisch).

**Concurrent-Auto-Create-Schutz:** Da kein Unique-Constraint auf `(ProductionOrderId, OperationNumber)` in WorkOperations existiert, muss die Find-Or-Create-Logik in einer Transaktion laufen: `BeginTransaction` → `FirstOrDefault(wo => wo.ProductionOrderId == faId && wo.Name == defaultName)` → falls null: `Add + SaveChanges` → `Commit`. Bei Race-Condition (zwei Terminals gleichzeitig) fängt die Transaktion das ab; im Worst-Case entsteht ein Duplikat — harmlos da die Buchung nur gegen das erste Ergebnis läuft.

**FA/AG-Scan im NurFA-Modus:** Wenn der Scan-Input ein FA+AG-Format enthält (z.B. `FA-12345,10`), wird der AG-Teil **still ignoriert** — nur die FA-Nummer wird verwendet.

### Terminal-Verhalten im NurFA-Modus

| Aspekt | Normaler Modus | NurFA-Modus |
|--------|---------------|-------------|
| **AG-Buttons** | WorkOperations der Werkbank (gruppiert produktiv/ungeplant) | ProductionOrders der Werkbank (nur produktiv) |
| **Ungeplante Tätigkeiten** | Sichtbar als eigene Gruppe | **Ausgeblendet** |
| **Rüsten** | "Rüsten starten" / "Produktion starten" | Nur "**Starten**" (direkt Production, kein Setup) |
| **FA/AG-Scan** | Akzeptiert `FA-Nr,AG-Nr` | Akzeptiert nur `FA-Nr` (ohne AG-Teil) |
| **Button-Text** | `FA-12345 / 10 — Fräsen` | `FA-12345 — Artikelbezeichnung` |
| **Mengen** | Gutmenge + Ausschuss | Gutmenge + Ausschuss (unverändert) |
| **Teilfertigmeldung** | Ja | Ja |
| **Pause** | Ja | Ja |

### API-Änderung

`GET /api/bde/available-operations/{workplaceId}` prüft `BdeNurFaMeldung`:

**Wenn `false` (normal):** wie bisher — offene WorkOperations + Aktivitäten.

**Wenn `true` (NurFA):** liefert offene ProductionOrders an dieser Werkbank (aus `ProductionOrder.ProductionWorkplaceId` oder aus zugewiesenen WorkOperations). Kein `unplanned`-Array. Response-Shape:

```json
{
  "productive": [
    { "id": 123, "label": "FA-12345 — Seitenteil links", "type": "fa" }
  ],
  "unplanned": []
}
```

Die `id` ist hier die **ProductionOrder.Id** (nicht WorkOperation.Id). Der Terminal-JS erkennt `type: "fa"` und ruft einen neuen Endpoint auf:

`POST /BdeTerminal/StartProductionForOrder` — nimmt `productionOrderId` statt `workOperationId`, findet/erstellt den Default-AG intern.

### Terminal-Controller: Neuer Action

```csharp
[HttpPost, ValidateAntiForgeryToken]
public async Task<IActionResult> StartProductionForOrder(int operatorId, int productionOrderId, int workplaceId, int terminalId)
{
    // 1. Default-AG finden oder erstellen
    var workOperationId = await _defaultWoService.FindOrCreateDefaultAsync(productionOrderId, workplaceId);
    // 2. Normale Buchung starten (immer Production, nie Setup)
    var result = await _bookingSvc.StartProductionAsync(operatorId, workOperationId, workplaceId, terminalId);
    return Json(MapResult(result));
}
```

### Cockpit + Buchungsliste

Keine Logik-Änderung nötig. Die Anzeige ergibt sich aus den Daten:
- Cockpit zeigt `WorkOperation.ProductionOrder.OrderNumber` (FA-Nummer) — der unsichtbare AG "PRODUKTION" erscheint nicht prominent
- Buchungsliste zeigt in der "Ziel"-Spalte `FA-12345/01` — akzeptabel, da der AG-Teil (`/01`) harmlos ist

Optional (nice-to-have): im NurFA-Modus die AG-Nummer in der Buchungsliste ausblenden und nur FA-Nummer zeigen. Kann später ergänzt werden.

### Validierung

Beim Speichern der Settings:
- `BdeNurFaMeldung = true` UND `BdeDefaultArbeitsgang` leer → Fehler: "Bei aktivierter FA-Meldung muss ein Default-Arbeitsgang definiert werden."
- `BdeNurFaMeldung = true` UND `BdeAktiv = false` → Warnung (optional): "BDE ist nicht aktiv — FA-Meldung hat keine Auswirkung."

### Settings-UI

Unterhalb des `BdeAktiv`-Toggles:
- Toggle: "Nur FA-Meldung (vereinfacht)"
- Textfeld (bedingt sichtbar): "Default-Arbeitsgang" mit Placeholder "z.B. PRODUKTION"
- Nur editierbar wenn `BdeAktiv = true`

## Betroffene Dateien (Überblick)

| Bereich | Dateien |
|---------|---------|
| Settings-Tabelle | `SQL/42_AddBde.sql`, `SQL/00_FreshInstall.sql`, `Program.cs` (Seeding) |
| Feature-Toggle Filter | Neues `Filters/RequireBdeActiveAttribute.cs` |
| Navigation | `Views/Shared/_Layout.cshtml` |
| BDE-Controller | Alle 5 BDE-Controller (+ `[RequireBdeActive]`) |
| Terminal-UI | `bde-terminal.js`, `Views/BdeTerminal/Index.cshtml` |
| Terminal-Controller | `BdeTerminalController.cs` (neue Action) |
| API | `BdeApiController.cs` (available-operations Logik) |
| Auto-Create Service | Neues `Services/BdeDefaultWorkOperationService.cs` |
| Settings-UI | `Views/Settings/Index.cshtml` (3 neue Felder) |
| Docs | `CLAUDE.md`, `Views/Help/Index.cshtml`, `Views/Help/Changelog.cshtml` |

## Bekannte Einschränkungen / Edge-Cases

- **SAGE-Import erzeugt echte WorkOperations:** Wenn SAGE Arbeitsgänge für einen FA importiert UND `BdeNurFaMeldung = true`, existieren sowohl der auto-erstellte Default-AG als auch die importierten AGs. Kein Konflikt — der Default-AG wird per Name (`BdeDefaultArbeitsgang`) identifiziert, nicht per OperationNumber. Die importierten AGs sind im Terminal nicht sichtbar (NurFA zeigt nur FAs).
- **Wechsel NurFA → Normal:** Bestehende Buchungen auf Default-AGs bleiben gültig. Im normalen Modus sieht man sie als reguläre AG-Buchungen. Kein Datenverlust.
- **Wechsel Normal → NurFA:** Laufende Buchungen auf spezifische AGs laufen weiter. Neue Buchungen gehen über Default-AGs.

## Nicht in Scope

- SAGE-Rückmeldung (Phase 2 später)
- Störungsmeldung (Phase 2 später)
- Änderung des BdeBooking-Schemas (bleibt unverändert)
- Rüsten im NurFA-Modus (bewusst ausgelassen — nur Produktion)
