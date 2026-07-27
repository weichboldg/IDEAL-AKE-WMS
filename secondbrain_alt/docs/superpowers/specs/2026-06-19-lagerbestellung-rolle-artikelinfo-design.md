# Rolle „Lagerbestellung" + Artikelinfo-Kachel für `masterdata_read` — Design

> Status: Entwurf zur Review
> Datum: 2026-06-19
> Branch: `feature/windows-auth-ad-users` (Worktree `.claude/worktrees/missingparts-include-pd`)
> Ziel-Version: v1.23.0 (zusätzliche Changelog-Punkte)

## 1. Kontext & Ziel

Zwei unabhängige, kleine Zugriffs-Erweiterungen:

1. **Neue Rolle „Lagerbestellung"** (`lagerbestellung`): Gibt Zugriff auf **genau**
   `/WarehouseRequisitions` (Meine Lagerbestellungen) und `/MissingParts` (Meine Fehlteile) —
   für Benutzer, die Lagerbestellungen erfassen und ihre Fehlteile verfolgen sollen, ohne
   volle picking/stock-Rechte. Additiv: bestehende Rollen behalten exakt ihren Zugriff.
2. **Artikelinfo-Dashboard-Kachel für `masterdata_read`**: Die Artikelinfo-Seite
   (`Articles/Info`) ist bereits `[RequireMasterDataReadAccess]` — `masterdata_read` darf sie
   aufrufen. Nur die **Dashboard-Kachel** liegt im `@if (canPick)`-Block und ist für reine
   `masterdata_read`-User unsichtbar. Die Kachel soll auch ihnen erscheinen.

## 2. Nicht-Ziele (YAGNI)

- **Keine** Erweiterung der neuen Rolle auf verwandte Bereiche (Bedarfsmeldungen `/PartRequisitions`,
  Empfängergruppen `/OrderRecipientGroups`, Bestand `/StockOverview`, Bewegungen `/StockMovements`,
  Lager-Fehlteile `/MissingPartsLager`). Bewusst eng (User-Entscheid).
- **Keine** Änderung der geteilten Filter `RequirePickingOrStockAccess` / `RequireStockAccess`
  (sonst Scope-Creep auf die anderen Controller).
- **Keine** Änderung am Zugriff von `Articles/Info` selbst (besteht bereits).
- **Keine** Änderung der übrigen Dashboard-Sektionen (nur die Artikelinfo-Kachel).

## 3. Teil A — Rolle „Lagerbestellung"

### 3.1 Rolle
- `RoleKeys.Lagerbestellung = "lagerbestellung"` (in `Models/RoleKeys.cs`).
- System-Rolle (`IsSystem = true`), Name **„Lagerbestellung"**, Beschreibung
  „Lagerbestellungen erfassen + eigene Fehlteile verfolgen". Geseedet via Migration.

### 3.2 Zwei additive Filter (mirror bestehender Composite-Filter)
| Neuer Filter | Erlaubte Rollen | Angewendet auf |
|--------------|-----------------|----------------|
| `RequirePickingOrStockOrLagerbestellungAccessAttribute` | admin, picking, stock, **lagerbestellung** | `WarehouseRequisitionsController`, `WarehouseRequisitionsApiController` (ersetzt `[RequirePickingOrStockAccess]`) |
| `RequireStockOrLagerbestellungAccessAttribute` | admin, stock, stock_keyuser, picking, **lagerbestellung** | `MissingPartsController` (ersetzt `[RequireStockAccess]`) |

- Aufbau exakt nach dem Muster vorhandener Composite-Filter (z. B.
  `RequirePickingOrStockAccessAttribute`): `IAsyncAuthorizationFilter`/`ActionFilter`, prüft
  via `ICurrentUserService.HasAnyRoleAsync(...)`, Redirect/403 wie die bestehenden Filter.
- **Wichtig:** Die alten Filter `RequirePickingOrStockAccess` (bleibt an PartRequisitions,
  OrderRecipientGroups) und `RequireStockAccess` (bleibt an StockOverview, StockMovements)
  werden NICHT verändert.

### 3.3 Menü-Sichtbarkeit
- Im Layout sind „Meine Lagerbestellungen" (`/WarehouseRequisitions`) und „Meine Fehlteile"
  (`/MissingParts`) aktuell hinter einem Rollen-/Can-Check sichtbar. Dieser Check wird um
  `lagerbestellung` erweitert (passende `ICurrentUserService`-Methode bzw. `HasAnyRoleAsync`).
- Konkrete Stelle wird im Plan via grep bestimmt (Layout-Partial + ggf. Helper-Methode).

### 3.4 Seed / Migration
- EF-Migration **74** `AddLagerbestellungRole`: INSERT der Rolle (idempotent: nur wenn Key
  nicht existiert). Muster: `20260608060227_AddMasterDataReadRole`.
- `SQL/74_AddLagerbestellungRole.sql` (idempotenter INSERT-Guard + History-Insert).
- `SQL/00_FreshInstall.sql`: Role-Seed-INSERT um die Zeile ergänzen + History-Insert.

## 4. Teil B — Artikelinfo-Kachel für `masterdata_read`

### 4.1 HomeController
- Zusätzlich `ViewBag.HasMasterDataReadAccess = await _currentUserService.HasMasterDataReadAccessAsync()`
  setzen (die *Read*-Variante: admin/masterdata_read/masterdata). Die bestehende
  `ViewBag.HasMasterDataAccess` (Edit-Variante) bleibt unverändert.

### 4.2 Home/Index.cshtml
- Die Artikelinfo-Kachel (aktuell im `@if (canPick)`-Block) in ein kleines **Partial**
  `Views/Home/_ArtikelinfoTile.cshtml` auslagern (reines Kachel-Markup, kein State).
- Verwendung:
  - In der bestehenden Kommissionier-Sektion (innerhalb `@if (canPick)`) — **unverändert**
    für picking-User (kein optisches Regress).
  - Zusätzlich in einem neuen kleinen Block `@if (!canPick && hasMasterDataReadAccess)`
    (eigene Mini-Sektion „Artikel"), sodass reine `masterdata_read`-User **nur** die
    Artikelinfo-Kachel sehen (nicht die Lager-/Kommissionier-Kacheln, die sie nicht öffnen
    dürfen).
- `bool hasMasterDataReadAccess = ViewBag.HasMasterDataReadAccess ?? false;` im View-Header.

## 5. Doku
- `RoleOverview.cshtml` (hand-gepflegt): neue Rolle `lagerbestellung` + ihr Zugriff eintragen.
- `CLAUDE.md`: Zugriffsschutz-Tabelle (zwei neue Filter), Rollenkonzept-Tabelle (neue Rolle),
  ggf. Layout-Hinweis. Artikelinfo-Kachel-Sichtbarkeit notieren.
- `Views/Help/Changelog.cshtml`: v1.23.0-Karte um zwei Bullets ergänzen (neue Rolle;
  Artikelinfo-Kachel für Stammdaten-ansehen).
- `docs/TESTSZENARIEN.md`: Szenarien (siehe §7).
- `PROJECT_STATUS.md`: kurzer Eintrag.

## 6. Tests
- **Filter-Tests** (Muster bestehender Filter-Attribut-Tests, falls vorhanden; sonst Controller-
  Smoke):
  - `lagerbestellung`-User: Zugriff auf WarehouseRequisitions + MissingParts erlaubt;
    auf PartRequisitions/StockOverview/StockMovements/MissingPartsLager **verweigert**.
  - Bestehende Rollen (picking/stock/stock_keyuser) behalten Zugriff auf die zwei Controller.
- **RoleKeys**: `Lagerbestellung`-Konstante vorhanden; falls es einen „alle System-Rollen"-Seed-
  Test gibt, dort ergänzen.
- **HomeController**: `HasMasterDataReadAccess` wird im ViewBag gesetzt (Smoke, falls
  HomeController-Tests existieren).
- Die genaue Testbarkeit der Filter (Attribut-Unit-Test vs. Integration) wird im Plan anhand
  des bestehenden Musters festgelegt.

## 7. TESTSZENARIEN (manuell)
- TS: `lagerbestellung`-User sieht/öffnet „Meine Lagerbestellungen" + „Meine Fehlteile",
  aber NICHT Bestand/Bewegungen/Bedarfsmeldungen (Menü + direkte URL → AccessDenied).
- TS: picking/stock-User unverändert (Regression).
- TS: `masterdata_read`-User sieht die Artikelinfo-Kachel am Dashboard und kann die
  Artikelinfo öffnen; sieht aber KEINE Lager-/Kommissionier-Kacheln.
- TS: picking-User-Dashboard zeigt die Artikelinfo-Kachel wie bisher (kein Regress).

## 8. Migration / Deploy
- Migration 74 + `SQL/74` + FreshInstall + History.
- Kein Datenmodell-Risiko (reiner Role-Seed + Filter-/View-Änderungen).
- Rolle nach Deploy einem Benutzer zuweisen (Benutzerstamm) zum Testen.

## 9. Offene Punkte / Risiken
- Geringfügig: die exakte Menü-Sichtbarkeits-Stelle + ob eine dedizierte
  `ICurrentUserService.CanAccessLagerbestellungAsync()`-Methode sinnvoll ist (für DRY zwischen
  Layout und ggf. Controller-Checks) — wird im Plan entschieden (Default: kleine Can-Methode,
  konsistent zu `CanProcessLagerAsync()` etc.).
- Die zwei neuen Filter müssen in `Program.cs`/Filter-Registry nichts Zusätzliches brauchen
  (Attribute werden per Reflection gefunden) — im Plan verifizieren.
