# HANDOFF — v1.20.0 Feingranulare Berechtigungen + Bugfixes

**Stand:** 64c76bff7d41763f36acefcfc08e31f16e1595ad docs: Doku-Komplettierung fuer v1.20.0 Bugfix-Welle
**Datum:** 2026-06-10
**Branch:** `bugfix/missingparts-include-pd` (im Worktree `.claude/worktrees/missingparts-include-pd`)
**Tests:** 649 Web + 99 Service alle gruen
**User-Bestaetigung:** alle Bugs manuell getestet und als gefixt bestaetigt

---

## Was steckt im Branch

### Initial-Scope (v1.20.0)
1. **Feingranulare Berechtigungen** — Read/Edit-Split in Stammdaten via neuer Rolle `masterdata_read`
2. **Lager-Worklist nur fuer Lager-Mitarbeiter** — picker raus aus WarehousePicking + MissingPartsLager
3. **Hand-gepflegte Rollen-Uebersicht** — `/Users/RoleOverview`

### Hinzugekommene Aenderungen
4. **Articles + StorageLocations Scope-Gap-Fix** — gleicher Read/Edit-Split wie die anderen 10
5. **Admin-Only-Refactor** — 6 Controller (Users/Roles/Workstations/Settings/SyncLog/BdeShiftCalendar) sind admin-only, nicht mehr masterdata
6. **Layout-Dropdown restrukturiert** — oberer Block fuer masterdata_read, unterer fuer admin
7. **Lagerbestellungen UX-Fixes** — kein Default-Fehlteil bei Ist=0
8. **Filter-Bugs** — Date-Picker dispatched jetzt Event, kein redundantes applyFilters im Server-Mode
9. **Model-Binder int[]-Bug** — leere quantitiesPicked-Inputs als "0" gesendet

### Was bestaetigt funktioniert
- Berechtigungs-System (vom User getestet)
- Filter-Funktionalitaet (vom User getestet)
- Lagerbestellungen-Submit mit ShortageStatus (vom User getestet)

### Was noch zu tun (offen)
- **Merge in main** (nicht autonom — auf User-Bestaetigung warten)
- **Worktree-Cleanup** (nicht autonom — auf User-Bestaetigung warten, Memory-Regel `feedback_worktree_cleanup_ask_first`)
- User testet ggf. weitere Edge-Cases ("ich test noch ein zwei punkte")

---

## Wichtige Pfade

| Pfad | Inhalt |
|---|---|
| `C:/Git/IDEAL-AKE-WMS/` | Main Repo |
| `C:/Git/IDEAL-AKE-WMS/.claude/worktrees/missingparts-include-pd/` | v1.20.0 Worktree |
| `secondbrain/docs/superpowers/specs/2026-06-03-finegrained-permissions-design.md` | Spec |
| `secondbrain/docs/superpowers/plans/2026-06-03-finegrained-permissions.md` | Plan |
| `IdealAkeWms/Filters/Require*Access*.cs` | Filter-Attribute |
| `IdealAkeWms/Models/RoleKeys.cs` | Rollen-Konstanten |
| `IdealAkeWms/Views/Users/RoleOverview.cshtml` | Hand-gepflegte Rollen-Uebersicht |
| `IdealAkeWms/Views/WarehousePicking/Details.cshtml` | Lagerbestellungen Detail-View |
| `IdealAkeWms/wwwroot/js/table-filter.js` | Filter-JS |
| `SQL/67_AddMasterDataReadRole.sql` | DB-Seed |

---

## Code-Pattern (wichtige Fallstricke)

### 1. Model-Binder int[] Empty-String Bug
`<input type="number" name="quantitiesPicked" value="">` mit empty value verschwindet beim Parsen zu `int[]`. Folge: paralleler Mapping-Loop haut Werte auf falsche Items.

**Fix-Pattern (siehe Details.cshtml):**
- `collectProgress()`: empty values als '0' senden statt empty string
- `normalizeEmptyQuantitiesToZero()`: vor jedem form.submit() leere Inputs auf '0' setzen

### 2. Programmatic input.value setzt kein Event ab
`input.value = '...'` loest KEIN `input`-Event aus. Event-Listener (z.B. scheduleServerNavigate) feuert nicht.

**Fix-Pattern (siehe table-filter.js):**
```javascript
input.value = newValue;
input.dispatchEvent(new Event('input', { bubbles: true }));
```

### 3. Server-Filter-Mode: kein client-seitiges applyFilters bei Init
Im Server-Mode hat der Server schon korrekt gefiltert + paginiert. Ein zusaetzliches Client-Filter via applyFilters() ist redundant und kann Datumsspalten verstecken.

**Fix-Pattern (siehe table-filter.js Init):**
```javascript
if (isServerColumnFilter()) {
    restoreFiltersFromUrl();
    // Kein applyFilters() — Server hat schon gefiltert
} else {
    restoreFiltersFromStorage();
    applyFilters();
}
```

### 4. Read/Edit-Pattern fuer Stammdaten-Controller
- Class-Level `[RequireMasterDataReadAccess]` (Read-Filter, beinhaltet implizit masterdata-Rolle)
- Action-Level `[RequireMasterDataAccess]` an Edit-Actions (Create/Edit/Delete)
- View: `@inject ICurrentUserService _user` + `var canEdit = await _user.HasMasterDataAccessAsync();` → Edit-Buttons in `@if (canEdit)` einwickeln

Aktuell 6 operative Stammdaten-Controller: Articles, StorageLocations, ProductionWorkplaces, OrderRecipients, ArticleCategories, ArticleAttributes.

Die anderen 6 (Users, Roles, Workstations, Settings, SyncLog, BdeShiftCalendar) sind via `[RequireAdminAccess]` admin-only.

---

## Commit-Log seit Plan-Start (Commit 19dfc0d)

```
64c76bf docs: Doku-Komplettierung fuer v1.20.0 Bugfix-Welle
7679384 fix(view): leere quantitiesPicked als "0" senden (Model-Binder-Bug)
b80c85e debug(view): console-logs in Details.cshtml fuer ShortageStatus-Bug
0aaab4d chore(js): Debug-Logs aus table-filter.js entfernen
96cb033 fix(js): Date-Picker-Klicks loesen jetzt Filter-Event aus
92b6c46 debug(js): temporaere console-logs in table-filter.js
e383554 fix(js): kein client-seitiger applyFilters-Aufruf im Server-Filter-Mode
5ee6fcc fix(view): kein Default-Fehlteil mehr bei Ist-Menge=0
aaad9d1 refactor(auth): Admin-Only-Block fuer Benutzer/Rollen/Settings/Logs
83bae79 fix(auth): Articles + StorageLocations Read/Edit-Split (Scope-Gap v1.20.0)
49ca685 docs: PROJECT_STATUS + TESTSZENARIEN Kapitel 34
fa36ed3 docs(claude): Filter-Tabelle korrigiert + Rollen + Fallstricke
914e180 chore: Version v1.20.0 + Changelog
407760f feat(layout): Stammdaten + Lager-Block auf neue Helpers
5d0bded feat(view): Settings-Forms Read-Only-Banner + disabled-Inputs
7fb13df feat(view): Edit-Buttons in 9 Stammdaten-Index-Views ausblenden + RoleOverview-Link
eb937ee feat(view): Users/RoleOverview hand-gepflegte Rollen-Uebersicht
ef8bd56 refactor(auth): Lager-Filter WarehousePicking+MissingPartsLager
bc31542 refactor(auth): BdeShiftCalendar+SyncLog Read/Edit-Split
16e15bf refactor(auth): Workplaces+Recipients+ArticleCats+Attributes Read/Edit-Split
26f4592 refactor(auth): Roles+Workstations+Settings Read/Edit-Split
cd3bf01 refactor(auth): UsersController Read/Edit-Split + RoleOverview-Action
4458abf feat(db): Migration 67 AddMasterDataReadRole
b06c264 test(auth): Filter-Tests fuer MasterDataRead + LagerProcessing
ad9dcc8 feat(auth): MasterDataRead+LagerProcessing Filter + Helper
```

---

## Naechste Schritte bei Wiederaufnahme

### Wenn User "Merge in main" sagt

```bash
cd C:/Git/IDEAL-AKE-WMS
git checkout main
git pull
git merge --no-ff bugfix/missingparts-include-pd -m "Merge branch 'bugfix/missingparts-include-pd' — v1.19.0 + v1.20.0 + Bugfixes"
git push
```

**WICHTIG (Memory-Regel `feedback_worktree_cleanup_ask_first`):** Worktree NICHT autonom raeumen. User muss explizit "Worktree raeumen" sagen.

### Wenn User weitere Aenderungen will

- Auf demselben Branch weiterarbeiten (Worktree ist noch da)
- Bei groesseren Aenderungen: superpowers:brainstorming → writing-plans → subagent-driven-development

### Wenn User explizit Worktree-Cleanup will

```bash
cd C:/Git/IDEAL-AKE-WMS
git worktree remove .claude/worktrees/missingparts-include-pd
git branch -d bugfix/missingparts-include-pd
```

---

## Wichtige Tools/Skills im Projekt

- **superpowers:brainstorming** — Spec schreiben
- **superpowers:writing-plans** — Plan schreiben
- **superpowers:subagent-driven-development** — Plan ausfuehren mit Sub-Agents
- **CLAUDE.md** — Single Source of Truth fuer Projektkonventionen (immer lesen!)
- **secondbrain/HOME.md** — Obsidian-Vault Einstieg

---

## Bekannte Edge-Cases (Vorsicht)

1. **Daten-destruktive Migration v1.19.0**: `ReplaceIsFinalShortageWithShortageStatus` droppt Spalte. Backup vor Deploy.
2. **PartiallyDelivered ist KEIN End-Status**: User kann Bestellungen mit dem Status wieder oeffnen und nachbearbeiten.
3. **Filter-Tests case-sensitive**: ColumnFilterHelper macht `.ToLowerInvariant()` auf Tokens UND values. Filter `"KW24"` → `"kw24"`.

---

**Erstellt:** 2026-06-10 — nach Session zur v1.20.0-Implementierung + Bugfix-Welle.
