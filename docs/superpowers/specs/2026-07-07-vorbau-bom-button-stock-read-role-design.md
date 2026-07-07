# Vorbau-Stückliste-Button + Rolle `stock_read` — Design

**Datum:** 2026-07-07
**Status:** Approved-pending-review
**Branch/Worktree:** `feature/glas-bestellung` (@ 8fc3686) → `.claude/worktrees/glas-bestellung`
**Version:** fold in **v1.25.0** (noch nicht released) — kein Versionssprung.

Zwei unabhängige, kleine Änderungen in einem Rollout.

---

## Item 1 — Bugfix: Stückliste-Button in FA-Liste für Rolle `vorbau`

### Problem (Root Cause, verifiziert)
In der FA-Liste (`ProductionOrders/Index`) sehen vorbau-User den Stückliste-Button nicht; in der FA-Abarbeitungsliste (`FaWorklist/Index`) öffnen sie die read-only Stückliste problemlos. Drei Fakten:
1. `ProductionOrdersController` trägt `[RequirePickingOrTrackingOrLeitstandAccess]` — **vorbau ist NICHT enthalten**. Betroffene User erreichen die FA-Liste also über eine **Zusatzrolle** (leitstand/tracking); pure-vorbau-User sehen die FA-Liste gar nicht.
2. Der Button in `Views/ProductionOrders/Index.cshtml` ist per `@if (Model.CanPick)` (nur picking) gated → für Nicht-Picker versteckt.
3. Der Button verlinkt auf `PickingController.Bom` = `[RequirePickingAccess]` (nur picking) → selbst bei Sichtbarkeit → AccessDenied. Zudem zeigt die Picking-Stückliste interaktive Kommissionier-Controls (TogglePicked etc.), die vorbau nicht haben soll.

### Lösung (entschieden)
Den Button in der FA-Liste auch für vorbau anzeigen, für vorbau aber auf die **read-only** `FaWorklist/Bom` verlinken (dieselbe Stückliste wie in der Abarbeitungsliste) — **nicht** auf die interaktive `Picking/Bom`. Picker bleiben unverändert. **Keine** Berechtigungs-/Migrations-Änderung, kein Privileg-Leak.

- `ProductionOrderListViewModel`: neues Property `public bool HasVorbauAccess { get; set; }` (`CanPick` existiert bereits).
- `ProductionOrdersController.Index`: `vm.HasVorbauAccess = await _currentUserService.HasVorbauAccessAsync();` (an der Stelle wo `CanPick` gesetzt wird).
- `Views/ProductionOrders/Index.cshtml`:
  - Actions-`<th>` zeigen wenn `Model.CanPick || Model.HasVorbauAccess`.
  - Button-Zelle: wenn `CanPick` → Link `Picking/Bom` (wie bisher); sonst wenn `HasVorbauAccess` → Link `FaWorklist/Bom` (read-only). Gleiches Icon/Title „Stückliste". `asp-route-id="@item.Id"` in beiden Fällen (item.Id = ProductionOrderId, passt zu beiden Bom-Actions).

### Out of scope
Pure-vorbau-Usern **Zugang zur FA-Liste selbst** zu geben (Controller-Filter erweitern) ist NICHT Teil des Wunsches. Die Änderung betrifft nur die Button-Sichtbarkeit+Ziel für User, die die FA-Liste bereits erreichen. `fa_completion` erhält den Button ebenfalls nicht (nicht angefragt, erreicht die FA-Liste ohnehin nur mit Zusatzrolle).

### Tests
- `ProductionOrdersController`-Test: bei `HasVorbauAccessAsync()==true` (Mock) enthält das zurückgegebene VM `HasVorbauAccess==true`; bei false → false. (CanPick-Verhalten unverändert.)
- View-Logik (bedingter Link) ist Razor → Build-verifiziert + Manual-UAT.

---

## Item 2 — Neue Berechtigung: Rolle `stock_read` (Lagerbestand-Ansicht)

### Ziel
Neue **additive Lese-Rolle** `stock_read` (Anzeigename „Lagerbestand-Ansicht") mit ausschließlich **lesendem** Zugriff auf:
- **Lager → Bestände** (`StockOverviewController`, komplett read-only)
- **Lager → Bewegungshistorie** (`StockMovementsController.Index`, read-only)

KEINE Buchungen (Ein-/Aus-/Umbuchung, Lagerplatz-Sonderaktionen). Bestehende Rollen (admin/stock/stock_keyuser/picking) behalten vollen Zugriff.

### Muster
Analog `masterdata_read` (v1.20.0): eigener Read-Filter + additive `CanAccess…ReadAsync`-Methode.

### Änderungen
- `Models/RoleKeys.cs`: `public const string StockRead = "stock_read";`
- `Services/ICurrentUserService.cs` + `CurrentUserService.cs`:
  ```csharp
  public async Task<bool> CanAccessStockReadAsync()
      => await HasAnyRoleAsync(RoleKeys.Admin, RoleKeys.Stock, RoleKeys.StockKeyUser, RoleKeys.Picking, RoleKeys.StockRead);
  ```
  (Additive Menge: alle bisherigen Stock-Zugriffsrollen PLUS `stock_read`.)
- NEU `Filters/RequireStockReadAccessAttribute.cs` (Muster = `RequireMasterDataReadAccessAttribute`): prüft `CanAccessStockReadAsync()`.
- `Controllers/StockOverviewController.cs`: Class-Level `[RequireStockAccess]` → `[RequireStockReadAccess]` (der ganze Controller ist read-only).
- `Controllers/StockMovementsController.cs`: **nur** die `Index`-Action (Bewegungshistorie) `[RequireStockAccess]` → `[RequireStockReadAccess]`. ALLE Schreib-Actions (`Inbound`/`Outbound`/`Transfer`/`OutboundAll`/`LocationTransfer`) bleiben `[RequireStockAccess]` bzw. `[RequireStockKeyUserAccess]` unverändert → stock_read erreicht nur die Liste.
- Rollen-Seed:
  - `Program.cs` `defaultRoles`-Array: `(RoleKeys.StockRead, "Lagerbestand-Ansicht", "Nur-Lesen-Zugriff auf Bestände und Bewegungshistorie", 35)` (SortOrder 35 frei).
  - **Migration** `<ts>_AddStockReadRole` (Muster = `AddLagerbestellungRole`, reiner Rollen-Insert, IF NOT EXISTS) + `SQL/78_AddStockReadRole.sql` (idempotent) + `SQL/00_FreshInstall.sql` (Rollen-Insert-Block nach `glasbestellung` + History-Insert). Nächste Migration nach 77 → SQL/78.
- `Views/Shared/_Layout.cshtml`:
  - Neue Variable `var canAccessStockRead = await CurrentUserService.CanAccessStockReadAsync();`.
  - Lager-Dropdown-Gate: `@if (canAccessStock || canPick || canAccessStockRead)`.
  - Buchungs-Einträge (Einbuchung/Ausbuchung/Umbuchung + Lagerplatz-Sonderaktionen) hinter `@if (canAccessStock || canPick)` schachteln, damit pure-stock_read NUR **Bestände** + **Bewegungshistorie** sieht. (Die Lagerplatz-Sonderaktionen bleiben zusätzlich hinter `CanTransferStockAsync()`.)
- `Views/Users/RoleOverview.cshtml`: neue Zeile `stock_read` (nach `stock`), read-only-Beschreibung.
- Doku: `CLAUDE.md` (Rollen-Tabelle + Zugriffsschutz-Tabelle: neuer Filter `RequireStockReadAccess` → StockOverview + StockMovements.Index), Changelog-Bullet (v1.25.0-Card), `docs/TESTSZENARIEN.md` (neues Kapitel: pure-stock_read sieht Bestände+Bewegungen read-only, KEINE Buchungs-Menüs/Actions; admin/stock unverändert).

### Tests
- `CurrentUserServiceRoleTests` (bestehende Datei): `CanAccessStockReadAsync` — true für admin/stock/stock_keyuser/picking/stock_read; false für z. B. nur-tracking / keine Rolle.
- Optional Controller-Access-Test: stock_read-User bekommt bei einer Schreib-Action (z. B. `StockMovements.Inbound` GET) ein Redirect auf AccessDenied — nur falls mit dem bestehenden Test-Setup einfach machbar; sonst Manual-UAT.

### Out of scope
- Kein Zugriff für stock_read auf „Lager: Eingehende Listen", „Lager: Fehlteile", Bedarfsmeldungen o. Ä. — ausschließlich Bestände + Bewegungshistorie.
- Kein neues Datenmodell/keine Tabellenänderung (Migration 78 = reiner Rollen-Insert).

---

## Doku-/Abschluss-Checkliste (beide Items)
- [ ] Build 0 Fehler + Web-Tests grün (aktuell 839+ / 1 Skip) + Service 123
- [ ] `dotnet ef migrations has-pending-model-changes` leer (Item 2 Migration ist reiner SQL-Insert → kein Model-Change; trotzdem prüfen)
- [ ] Migration 78 + SQL/78 + FreshInstall (Rolle + History) synchron
- [ ] CLAUDE.md, Changelog, TESTSZENARIEN, RoleOverview
- [ ] Reviews (Spec + Quality) je Task
