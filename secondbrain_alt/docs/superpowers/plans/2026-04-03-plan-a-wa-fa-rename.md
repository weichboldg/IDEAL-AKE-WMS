# Plan A: WA→FA Terminologie-Rename — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Alle UI-Labels, Platzhalter, Hilfetexte und Dokumentation von "Werkstattauftrag/WA" auf "Fertigungsauftrag/FA" umbenennen. Keine DB-, Property- oder URL-Änderungen.

**Architecture:** Reine Text-Ersetzungen in Views (.cshtml), Model Display-Attributes, Controller TempData-Meldungen, Service Log-Messages, E-Mail-Templates und Docs. Externe Systeme (enaio, Sage) und DB-Werte bleiben unberührt.

**Tech Stack:** ASP.NET Core 10.0 Razor Views, C# String-Literale

**Spec:** `docs/superpowers/specs/2026-04-03-navigation-restructuring-design.md` Sektion 1

---

## NICHT umbenennen (externe Systeme!)

- `EnaioDmsSyncService.cs`: SQL-Query `WHERE feld1 IN ('Werkstattauftrag', 'Zeichnung')` + Vergleich `typ == "Werkstattauftrag"` — enaio-DB-Wert
- `SageImportService.cs`: SQL-Spalte `[WA Nummer]` — Sage-View-Spaltenname
- `EnaioDmsDocument.cs` Zeile 10: XML-Comment `"Werkstattauftrag" oder "Zeichnung"` — beschreibt DB-Wert, kann bleiben
- C#-Properties: `OrderNumber`, `CurrentWaNumbers`, `NewWaNumber`
- CSS-Klasse `.wa-number` in PrintBom.cshtml
- Test-Daten in IdealAkeWms.Tests/

---

## Task 1: Model + Controller — Display-Attribute, ErrorMessage, TempData, Logs

**Files:**
- Modify: `IdealAkeWms/Models/ProductionOrder.cs:7,9`
- Modify: `IdealAkeWms/Controllers/ProductionOrdersController.cs:115,313,368,560,698`
- Modify: `IdealAkeWms/Controllers/StockMovementsController.cs:342`
- Modify: `IdealAkeWms/Services/PickingTransferService.cs:69,249`
- Modify: `IdealAkeWms/Models/ViewModels/ProductionOrderViewModel.cs:17`
- Modify: `IdealAkeWms/Models/EnaioDmsDocument.cs:15`

- [ ] **Step 1: ProductionOrder.cs — Display + ErrorMessage**

Zeile 7: `"WA Nummer ist erforderlich"` → `"FA Nummer ist erforderlich"`
Zeile 9: `[Display(Name = "WA Nummer")]` → `[Display(Name = "FA Nummer")]`

- [ ] **Step 2: ProductionOrdersController.cs — TempData + Kommentar**

Zeile 115: `$"WA {order.OrderNumber} kann nicht freigegeben werden"` → `$"FA {order.OrderNumber} kann nicht freigegeben werden"`
Zeile 313: Kommentar `// enaio DMS-Links laden (Bulk-Lookup fuer alle WA-Nummern)` → `FA-Nummern`
Zeile 368: `"Dieser Werkstattauftrag hat keine Artikelnummer."` → `"Dieser Fertigungsauftrag hat keine Artikelnummer."`
Zeile 560: Kommentar `// Kommissionierung abgeschlossen → WA automatisch erledigt setzen` → `FA automatisch erledigt setzen`
Zeile 698: Kommentar `// Bestehende Fotos fuer diesen WA zaehlen` → `FA zaehlen`

- [ ] **Step 3: PickingTransferService.cs — Logs + Kommentar**

Zeile 69: Kommentar `// Bei forceTransfer: WA-Nummern zusammenführen` → `FA-Nummern`
Zeile 249: `"TransferPicked fehlgeschlagen für WA-Id {OrderId}"` → `"TransferPicked fehlgeschlagen für FA-Id {OrderId}"`

- [ ] **Step 4: ProductionOrderViewModel.cs — XML-Comment**

Zeile 17: `/// <summary>enaio DMS-Links pro WA-Nummer` → `FA-Nummer`

- [ ] **Step 5: StockMovementsController.cs — Kommentar**

Zeile 342: Kommentar `// Bei Kommissionierwagen: neueste WA-Nummer automatisch ermitteln` → `FA-Nummer`

- [ ] **Step 6: EnaioDmsDocument.cs — XML-Comment**

Zeile 15: `/// <summary>WA-Nummer: feld44 (WA) oder left(feld43,7) (Zeichnung)</summary>` → `FA-Nummer: feld44 (FA) oder left(feld43,7) (Zeichnung)`

- [ ] **Step 7: E-Mail-Template — PartRequisitionEmailService.cs**

`IDEALAKEWMSService/Services/PartRequisitionEmailService.cs`:
Zeile 89: `$"{prefix}Bedarfsmeldung — WA {orderNumber}"` → `$"{prefix}Bedarfsmeldung — FA {orderNumber}"`
Zeile 116: `Werkstattauftrag {first.OrderNumber}` → `Fertigungsauftrag {first.OrderNumber}`

- [ ] **Step 8: Build prüfen**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj && dotnet build IDEALAKEWMSService/IDEALAKEWMSService.csproj`

- [ ] **Step 9: Commit**

```bash
git add IdealAkeWms/Models/ IdealAkeWms/Controllers/ IdealAkeWms/Services/ IdealAkeWms/Models/ViewModels/ IDEALAKEWMSService/Services/
git commit -m "refactor: rename WA→FA in model attributes, TempData, logs, email template"
```

---

## Task 2: Views — Produktionsaufträge, Picking, Bom, Print

**Files:**
- Modify: `IdealAkeWms/Views/ProductionOrders/Index.cshtml:4,25,26,60`
- Modify: `IdealAkeWms/Views/ProductionOrders/Picking.cshtml:22`
- Modify: `IdealAkeWms/Views/ProductionOrders/Bom.cshtml:3,8,312,359,456`
- Modify: `IdealAkeWms/Views/ProductionOrders/PrintBom.cshtml:10,183`
- Modify: `IdealAkeWms/Views/ProductionOrders/PrintPicking.cshtml:10,112`
- Modify: `IdealAkeWms/Views/ProductionOrders/PickingDropdown.cshtml:11,14,16`

- [ ] **Step 1: Index.cshtml**

Zeile 4: `"Werkstattaufträge"` → `"Fertigungsaufträge"`
Zeile 25: `>WA Nummer</label>` → `>FA Nummer</label>`
Zeile 26: `placeholder="WA Nummer"` → `placeholder="FA Nummer"`
Zeile 60: `>WA Nr.</th>` → `>FA Nr.</th>`

- [ ] **Step 2: Picking.cshtml**

Zeile 22: `>WA Nr.</th>` → `>FA Nr.</th>`

- [ ] **Step 3: Bom.cshtml**

Zeile 3: `$"Stückliste - WA {Model.OrderNumber}"` → `$"Stückliste - FA {Model.OrderNumber}"`
Zeile 8: `Stückliste - WA @Model.OrderNumber` → `Stückliste - FA @Model.OrderNumber`
Zeile 312: `Neuer Werkstattauftrag:` → `Neuer Fertigungsauftrag:`
Zeile 359: `für diesen Werkstattauftrag?` → `für diesen Fertigungsauftrag?`
Zeile 456: `Zurück zur WA-Liste` → `Zurück zur FA-Liste`

- [ ] **Step 4: PrintBom.cshtml**

Zeile 10: `Stückliste - WA @Model.OrderNumber` → `Stückliste - FA @Model.OrderNumber`
Zeile 183: `WA @Model.OrderNumber` → `FA @Model.OrderNumber`

- [ ] **Step 5: PrintPicking.cshtml**

Zeile 10: `Kommissionierliste - WA @Model.OrderNumber` → `Kommissionierliste - FA @Model.OrderNumber`
Zeile 112: `<strong>Werkstattauftrag:</strong>` → `<strong>Fertigungsauftrag:</strong>`

- [ ] **Step 6: PickingDropdown.cshtml**

Zeile 11: `Werkstattauftrag auswählen` → `Fertigungsauftrag auswählen`
Zeile 14: `Werkstattauftrag suchen und Stückliste` → `Fertigungsauftrag suchen und Stückliste`
Zeile 16: `<label class="form-label">Werkstattauftrag</label>` → `Fertigungsauftrag`

- [ ] **Step 7: Commit**

```bash
git add IdealAkeWms/Views/ProductionOrders/
git commit -m "refactor: rename WA→FA in ProductionOrders views"
```

---

## Task 3: Views — Tracking, Bestellungen, Lager, StockOverview

**Files:**
- Modify: `IdealAkeWms/Views/Tracking/Index.cshtml:16,17,61`
- Modify: `IdealAkeWms/Views/Tracking/ByWorkplace.cshtml:23,24,58`
- Modify: `IdealAkeWms/Views/PartRequisitions/Index.cshtml:17,65`
- Modify: `IdealAkeWms/Views/StockOverview/Index.cshtml:45`
- Modify: `IdealAkeWms/Views/StockMovements/Inbound.cshtml:75`
- Modify: `IdealAkeWms/Views/StockMovements/OutboundAll.cshtml:51,95`
- Modify: `IdealAkeWms/Views/StorageLocations/Create.cshtml:43`
- Modify: `IdealAkeWms/Views/StorageLocations/Edit.cshtml:47`

- [ ] **Step 1: Tracking Views**

`Tracking/Index.cshtml`:
Zeile 16: `WA-Nummer` → `FA-Nummer`
Zeile 17: `placeholder="z.B. WA-001"` → `placeholder="z.B. FA-001"`
Zeile 61: `>WA-Nr.</th>` → `>FA-Nr.</th>`

`Tracking/ByWorkplace.cshtml`:
Zeile 23: `WA-Nummer` → `FA-Nummer`
Zeile 24: `placeholder="z.B. WA-001"` → `placeholder="z.B. FA-001"`
Zeile 58: `>WA-Nr.</th>` → `>FA-Nr.</th>`

- [ ] **Step 2: PartRequisitions + Stock Views**

`PartRequisitions/Index.cshtml`:
Zeile 17: `placeholder="WA-Nr., Kunde,` → `placeholder="FA-Nr., Kunde,`
Zeile 65: `>WA-Nr.</th>` → `>FA-Nr.</th>`

`StockOverview/Index.cshtml`:
Zeile 45: `placeholder="WA-Nr."` → `placeholder="FA-Nr."`
Zeile 46: `title="WA-QR-Code scannen"` → `title="FA-QR-Code scannen"`

`StockMovements/Inbound.cshtml`:
Zeile 75: `>WA-Nr</th>` → `>FA-Nr.</th>`

`StockMovements/OutboundAll.cshtml`:
Zeile 51: `WA-Nummer wurde automatisch` → `FA-Nummer wurde automatisch`
Zeile 95: `placeholder="WA-Nummer"` → `placeholder="FA-Nummer"`

- [ ] **Step 3: StorageLocations**

`StorageLocations/Create.cshtml` Zeile 43: `verschiedene WA auf gleichem Wagen` → `verschiedene FA auf gleichem Wagen`
`StorageLocations/Edit.cshtml` Zeile 47: gleiche Änderung

- [ ] **Step 4: Commit**

```bash
git add IdealAkeWms/Views/Tracking/ IdealAkeWms/Views/PartRequisitions/ IdealAkeWms/Views/StockOverview/ IdealAkeWms/Views/StockMovements/ IdealAkeWms/Views/StorageLocations/
git commit -m "refactor: rename WA→FA in Tracking, Bestellungen, Lager views"
```

---

## Task 4: Layout, Dashboard, Hilfe, Select2-Partial

**Files:**
- Modify: `IdealAkeWms/Views/Shared/_Layout.cshtml:77`
- Modify: `IdealAkeWms/Views/Shared/_Select2ProductionOrderPartial.cshtml:27`
- Modify: `IdealAkeWms/Views/Home/Index.cshtml:81,89`
- Modify: `IdealAkeWms/Views/Help/Index.cshtml:54,57,74,76,89,173,228`
- Modify: `IdealAkeWms/Views/Help/Changelog.cshtml:45,46,120,126,158,159`

- [ ] **Step 1: _Layout.cshtml**

Zeile 77: `>Werkstattaufträge</a>` → `>Fertigungsaufträge</a>`

- [ ] **Step 2: _Select2ProductionOrderPartial.cshtml**

Zeile 27: `'-- Werkstattauftrag suchen --'` → `'-- Fertigungsauftrag suchen --'`

- [ ] **Step 3: Home/Index.cshtml (Dashboard)**

Zeile 81: `Werkstattaufträge` → `Fertigungsaufträge`
Zeile 89: `Werkstattaufträge verwalten` → `Fertigungsaufträge verwalten`

- [ ] **Step 4: Help/Index.cshtml**

Zeile 54: `Werkstattaufträge` → `Fertigungsaufträge`
Zeile 57: `Werkstattauftraege` → `Fertigungsauftraege`
Zeile 74: `WA-Nummer erscheinen... Werkstattauftrag` → `FA-Nummer erscheinen... Fertigungsauftrag`
Zeile 76: `Werkstattauftrag-Dokument` → `Fertigungsauftrag-Dokument`
Zeile 89: `WA-Nummer` → `FA-Nummer`
Zeile 173: `Werkstattauftrag waehlen` → `Fertigungsauftrag waehlen`
Zeile 228: `und Werkstattauftrag angezeigt` → `und Fertigungsauftrag angezeigt`

- [ ] **Step 5: Help/Changelog.cshtml** (nur v1.2.0+ Einträge)

Zeile 45: `Werkstattauftraege` → `Fertigungsauftraege`
Zeile 46: `WA-Liste` → `FA-Liste`
Zeile 120: `in Werkstattauftraegen` → `in Fertigungsauftraegen`
Zeile 126: `WA-Nummer` → `FA-Nummer`
Zeile 158: `Werkstattauftraege: Spalten` → `Fertigungsauftraege: Spalten`
Zeile 159: `WA-Nummer` → `FA-Nummer` (nur wenn in v1.0.0 Eintrag — historisch, optional)

**Hinweis**: Ältere Changelog-Einträge (v1.0.0, v1.1.0, v1.1.1) können als historisch belassen werden oder konsistent umbenannt werden. Empfehlung: Alles umbenennen für Konsistenz.

- [ ] **Step 6: Build + Tests**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj && dotnet test IdealAkeWms.Tests`

- [ ] **Step 7: Commit**

```bash
git add IdealAkeWms/Views/Shared/ IdealAkeWms/Views/Home/ IdealAkeWms/Views/Help/
git commit -m "refactor: rename WA→FA in Layout, Dashboard, Hilfe, Select2-Partial"
```

---

## Task 5: Dokumentation — CLAUDE.md, README.md, PROJECT_STATUS.md

**Files:**
- Modify: `CLAUDE.md` (~20 Stellen mit "WA" oder "Werkstattauftrag")
- Modify: `README.md` (~10 Stellen)
- Modify: `PROJECT_STATUS.md` (~15 Stellen)

- [ ] **Step 1: Globale Suche+Ersetze in Docs**

Führe in CLAUDE.md, README.md, PROJECT_STATUS.md folgende Ersetzungen durch:

| Suche | Ersetze |
|-------|---------|
| `Werkstattaufträge` | `Fertigungsaufträge` |
| `Werkstattauftraege` | `Fertigungsauftraege` |
| `Werkstattauftrag` | `Fertigungsauftrag` |
| `WA-Liste` | `FA-Liste` |
| `WA-Nummer` | `FA-Nummer` |
| `WA-Nr` | `FA-Nr` |
| `WA Nummer` | `FA Nummer` |
| `WA ` (vor Zahlen/Nummern) | `FA ` |

**ACHTUNG**: In CLAUDE.md NICHT umbenennen:
- `EnaioDmsSyncService` Beschreibung wo `"Werkstattauftrag"` als DB-Wert referenziert wird
- `SageImportService`/Agent-Job SQL-Spalte `[WA Nummer]` (externer Spaltenname)
- Abschnitt über `DocumentType ("Werkstattauftrag"/"Zeichnung")` — DB-Wert

- [ ] **Step 2: Commit**

```bash
git add CLAUDE.md README.md PROJECT_STATUS.md
git commit -m "refactor: rename WA→FA in CLAUDE.md, README, PROJECT_STATUS documentation"
```

---

## Task 6: Verifikation + Version

- [ ] **Step 1: Grep-Verifikation**

```bash
# Alle müssen 0 relevante Treffer ergeben:
grep -rn "WA Nummer" --include="*.cshtml" IdealAkeWms/Views/
grep -rn "WA Nr" --include="*.cshtml" IdealAkeWms/Views/
grep -rn "WA-Nummer" --include="*.cshtml" IdealAkeWms/Views/
grep -rn "Werkstattauftrag" --include="*.cshtml" IdealAkeWms/Views/ | grep -v "DocumentType"
grep -rn "\"WA " --include="*.cs" IdealAkeWms/Controllers/ IdealAkeWms/Services/ IdealAkeWms/Models/
```

Erlaubte Ausnahmen:
- `EnaioDmsSyncService.cs`: `'Werkstattauftrag'` in SQL-Query
- `EnaioDmsDocument.cs`: XML-Comment über DB-Wert
- CSS-Klasse `.wa-number`
- Test-Dateien

- [ ] **Step 2: Build + alle Tests**

Run: `dotnet build IdealAkeWms/IdealAkeWms.csproj && dotnet test IdealAkeWms.Tests`
Expected: 0 errors, 211+ tests pass

- [ ] **Step 3: Version auf 1.3.0 hochzählen**

Beide `AppVersion.cs`: `Version = "1.3.0"`

- [ ] **Step 4: Changelog v1.3.0 Eintrag**

Neuer Block in `Help/Changelog.cshtml`:
- "Terminologie: Werkstattauftrag → Fertigungsauftrag (WA → FA) durchgängig in der gesamten Oberfläche"

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "docs: version 1.3.0, WA→FA rename complete"
```

---

## Zusammenfassung

| Task | Dateien | Scope |
|------|---------|-------|
| 1 | 6 .cs Dateien | Model, Controller, Service, E-Mail |
| 2 | 6 Views | ProductionOrders Views |
| 3 | 8 Views | Tracking, Bestellungen, Lager |
| 4 | 6 Views | Layout, Dashboard, Hilfe, Select2 |
| 5 | 3 Docs | CLAUDE.md, README, PROJECT_STATUS |
| 6 | Verifikation | Grep + Build + Version |
