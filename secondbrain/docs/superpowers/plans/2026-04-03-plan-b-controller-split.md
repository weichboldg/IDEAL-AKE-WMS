# Plan B: Controller-Split + View-Move — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task.

**Goal:** ProductionOrdersController (765 Zeilen, 15 Actions) aufteilen in ProductionOrdersController (5 Actions), PickingController (8 Actions), PhotoController (3 Actions). Views entsprechend verschieben.

**Architecture:** Actions nach Domäne verschieben. Keine Business-Logik-Änderungen. Views in passende Ordner. Redirect-Stubs für alte URLs. Alle View-Referenzen (asp-controller, Url.Action, hardcoded URLs) aktualisieren.

**Spec:** `docs/superpowers/specs/2026-04-03-navigation-restructuring-design.md` Sektion 3-6

---

## Task 1: PickingController erstellen

Neuen Controller mit den Picking-Actions erstellen. Actions aus ProductionOrdersController KOPIEREN (noch nicht entfernen).

**Actions für PickingController:**
- `Index` (war: Picking, Zeile 63) — Kommissionierliste
- `Bom` (Zeile 359) — Stückliste
- `TogglePicked` (Zeile 498) — Legacy Picking-Toggle
- `TransferPicked` (Zeile 513) — Umbuchen
- `SetPickingStatus` (Zeile 549) — Status setzen
- `PrintBom` (Zeile 573) — Stückliste drucken
- `PrintPicking` (Zeile 647) — Kommissionierliste drucken
- `ToggleDone` (Zeile 337) — Erledigt-Toggle

**Dependencies für PickingController:**
`IProductionOrderRepository`, `ICurrentUserService`, `IAppSettingRepository`, `IHolidayRepository`, `IBusinessDayService`, `IBomRepository`, `IPickingRepository`, `IStockMovementRepository`, `IStorageLocationRepository`, `IArticleRepository`, `IPickingTransferService`, `IUserRepository`, `IEnaioDmsDocumentRepository`, `IPartRequisitionRepository`, `IWebHostEnvironment`

---

## Task 2: PhotoController erstellen (API)

**Actions:**
- `Upload` (war: UploadPhoto, Zeile 684)
- `Get` (war: GetPhotos, Zeile 718)
- `Delete` (war: DeletePhoto, Zeile 745)

Route: `api/photos`, `[RequirePickingAccess]`

**Dependencies:** `IProductionOrderRepository`, `IWebHostEnvironment`

---

## Task 3: Views verschieben

```bash
mkdir -p IdealAkeWms/Views/Picking
mv IdealAkeWms/Views/ProductionOrders/Picking.cshtml IdealAkeWms/Views/Picking/Index.cshtml
mv IdealAkeWms/Views/ProductionOrders/PickingDropdown.cshtml IdealAkeWms/Views/Picking/IndexDropdown.cshtml
mv IdealAkeWms/Views/ProductionOrders/Bom.cshtml IdealAkeWms/Views/Picking/Bom.cshtml
mv IdealAkeWms/Views/ProductionOrders/PrintBom.cshtml IdealAkeWms/Views/Picking/PrintBom.cshtml
mv IdealAkeWms/Views/ProductionOrders/PrintPicking.cshtml IdealAkeWms/Views/Picking/PrintPicking.cshtml
```

PickingController `Index` action: `return View("IndexDropdown")` statt `return View("PickingDropdown")`

---

## Task 4: Referenzen aktualisieren

### Kritische Stellen:
1. `_Select2ProductionOrderPartial.cshtml`: `'/ProductionOrders/Bom/' + id` → `'/Picking/Bom/' + id`
2. `wwwroot/js/photo-upload.js`: 3 URLs → `/api/photos/...`
3. `ProductionOrders/Index.cshtml`: `asp-action="Bom"` → `asp-controller="Picking" asp-action="Bom"`
4. `PartRequisitions/Index.cshtml`: `asp-controller="ProductionOrders" asp-action="Bom"` → `asp-controller="Picking"`
5. `Picking/Bom.cshtml`: `asp-action="Index"` "Zurück zur FA-Liste" → `asp-controller="ProductionOrders" asp-action="Index"`

---

## Task 5: ProductionOrdersController bereinigen + Redirect-Stubs

Actions entfernen die jetzt in PickingController/PhotoController sind. Redirect-Stubs hinzufügen:

```csharp
public IActionResult Bom(int id) => RedirectToActionPermanent("Bom", "Picking", new { id });
public IActionResult Picking() => RedirectToActionPermanent("Index", "Picking");
```

---

## Task 6: Build + Tests + Grep-Verifikation
