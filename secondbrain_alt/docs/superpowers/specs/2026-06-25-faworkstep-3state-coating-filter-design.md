# FaWorkStep 3-State-Status + Beschichtungstermin + Filter-nach-ENTER — Design

> Status: Entwurf zur Review
> Datum: 2026-06-25
> Branch: `feature/windows-auth-ad-users` (Worktree `.claude/worktrees/missingparts-include-pd`)
> Ziel-Version: v1.23.0 (zusätzliche Changelog-Punkte)

## 1. Kontext & Ziel

Drei Erweiterungen rund um die FA-Vorbau-Abarbeitung:

1. **Erledigt-Checkbox → 3-State-Status.** Der `FaWorkStep`-Erledigt-Status ist heute ein Boolean
   (`IsCompleted`), dargestellt als Checkbox in der FA-Abarbeitungsliste UND als VK-VA-Checkboxen
   im Leitstand. Neu: ein 3-Wert-Status **Offen / in Bearbeitung / Fertig** als Dropdown — in
   beiden Views (Abarbeitungsliste + Leitstand). Die Checkboxen entfallen.
2. **Beschichtungstermin in der Abarbeitungsliste.** Die FA-Abarbeitungsliste zeigt BG-/Komm-/
   Fert.-Termin, aber **nicht** den Beschichtungstermin (anders als FA-Liste/Leitstand). Spalte
   ergänzen.
3. **Filter erst nach ENTER (nur Server-Mode-Tabellen).** Server-Mode-Spaltenfilter navigieren
   heute debounced beim Tippen (500 ms) — das lädt die Seite mitten im Tippen neu und stört. Neu:
   Navigation erst bei **ENTER**. Client-Mode-Tabellen bleiben unverändert (live).

## 2. Nicht-Ziele (YAGNI)

- **Keine** neue View/Spalte in der eigentlichen Kommissionierungs-/Picking-Worklist (User-Entscheid:
  „Kommissionierung" = der Leitstand, der die VK-VA-Spalten bereits zeigt).
- **Keine** Änderung an `IsSpecComplete` (FA-Vervollstaendigung „Vollständig definiert") — das ist
  ein anderes Flag und bleibt unberührt.
- **Keine** Änderung am Client-Mode-Filterverhalten (Change 3 nur Server-Mode).
- **Keine** Änderung an den Filter-Card-Inputs (`workStepId`/`workbenches`/`showDone` mit
  `onchange="this.form.submit()"`) — die feuern erst beim Verlassen des Feldes, haben das
  Tipp-Problem nicht.

## 3. Change 1 — FaWorkStep: 3-State-Status

### 3.1 Datenmodell (Migration 76)
- Neuer Enum `FaWorkStepStatus { Offen = 0, InBearbeitung = 1, Fertig = 2 }` (in `Models/`).
- `FaWorkStep.IsCompleted` (bool) **ersatzlos ersetzt** durch `Status` (`FaWorkStepStatus`, als int
  gespeichert; EF-Default-Enum-Mapping, kein `HasConversion` nötig). `CompletedAt`/`CompletedBy`
  bleiben (gesetzt wenn → `Fertig`, sonst `null`).
- **Migration 76** `ReplaceFaWorkStepIsCompletedWithStatus`: neue Spalte `Status` INT NOT NULL
  DEFAULT 0; Daten-Konvertierung `UPDATE FaWorkSteps SET Status = CASE WHEN IsCompleted = 1 THEN 2
  ELSE 0 END`; danach `IsCompleted`-Spalte droppen. `Down()` reversibel (Spalte `IsCompleted`
  wieder anlegen, `IsCompleted = CASE WHEN Status = 2 THEN 1 ELSE 0 END`, `Status` droppen — die
  Unterscheidung Offen/InBearbeitung geht im Down() verloren, dokumentiert). + `SQL/76_*.sql`
  (idempotent) + `SQL/00_FreshInstall.sql` (Spalte + History). Muster: ShortageStatus-Migration
  (v1.19.0). **DB-Backup vor Produktions-Deploy empfohlen** (daten-konvertierend).

### 3.2 Repository / API
- `IFaWorkStepRepository.SetIsCompletedAsync(id, bool, …)` → `SetStatusAsync(id, FaWorkStepStatus, …)`
  (setzt `Status`; `CompletedAt/By` bei `Fertig`, sonst null; Audit wie bisher).
- `FaWorkStepPivotCell(int FaWorkStepId, bool IsCompleted)` → `FaWorkStepPivotCell(int FaWorkStepId,
  FaWorkStepStatus Status)`. `GetWorkStepDetailPivotAsync` projiziert `f.Status` statt
  `f.IsCompleted`.
- API `FaWorkStepsApiController`: `[HttpPost("toggle-completed")]` (`{ faWorkStepId, value:bool }`)
  → `[HttpPost("set-status")]` (`{ faWorkStepId, status:int }`). Filter
  `[RequireVorbauOrPickingOrLeitstandAccess]` **unverändert**. Request-DTO `SetStatusRequest`
  (faWorkStepId:int, status:int → in `FaWorkStepStatus` casten, ungültig → `BadRequest`).

### 3.3 Ausblende-Logik
`FaWorklistController.Index`: `!showDone && selectedStep.IsCompleted` →
`!showDone && selectedStep.Status == FaWorkStepStatus.Fertig`. „in Bearbeitung" bleibt sichtbar.

### 3.4 Views
- **FaWorklist/Index.cshtml** (Erledigt-Spalte): Checkbox → `<select class="form-select form-select-sm worklist-status" data-fa-work-step-id="…">` mit Optionen Offen/in Bearbeitung/Fertig, `selected` nach `WorkStepCell.Status`. JS: `change`-Delegation → POST `/api/fa-work-steps/set-status` `{ faWorkStepId, status }`; Fehlerfall: alten Wert zurücksetzen (Pattern wie heute). `FaWorklistCell.IsCompleted` → `Status` (`FaWorkStepStatus`).
- **PickingLeitstand/Index.cshtml** (VK-VA): die 5 Checkboxen → 5 `<select>` (gleiche Optionen + Endpoint), `@(!Model.CanPick ? "disabled" : "")` bleibt. Fehlender Code = AG nicht anwendbar = leere Zelle (wie heute). Die VK-VA-**Spaltenfilter** (C#-Text nach Pivot): „offen"/„in bearbeitung"/„fertig"/"" statt „erledigt"/„offen".
- Gemeinsamer Render-Helper für die 3 Optionen (Razor-Partial oder statische Liste), damit Abarbeitungsliste + Leitstand identisch sind.

### 3.5 Tests
- Repo `SetStatusAsync`: setzt Status korrekt; `CompletedAt/By` bei Fertig gesetzt, bei Offen/InBearbeitung null.
- `GetWorkStepDetailPivotAsync`: Cell trägt `Status`.
- `FaWorklistController`: `Status==Fertig` blendet aus (ohne showDone); `InBearbeitung` + `Offen` sichtbar; `showDone` zeigt alle.
- API `set-status`: gültiger Status → Ok + Repo-Aufruf; ungültiger int → BadRequest.

## 4. Change 2 — Beschichtungstermin in der Abarbeitungsliste

- `FaWorklistRow.BeschichtungsTermin` (DateTime?).
- `FaWorklistController.Index` lädt zusätzlich `BeschichtungTage` (Default 10), `BeschichtungAbholtage`
  (Default „Dienstag,Donnerstag" → `ParsePickupDays`), `LackierteilKategorieName`. Berechnung **identisch
  zum Leitstand**: `raw = SubtractBusinessDays(VorkommissionierTermin, BeschichtungTage, holidays)`;
  `BeschichtungsTermin = FindPreviousPickupDay(raw, pickupDays)`; nur wenn `LackierteilKategorieName`
  leer **oder** `PickingStatus.HasCoatingParts` true (Backward-Compat). `GetAllOrderedAsync` lädt
  `PickingStatus` bereits (`HasCoatingParts` verfügbar).
- **DRY:** Die Coating-Berechnung (heute inline in `PickingLeitstandController`) in einen kleinen
  Helper extrahieren (z. B. `CoatingDateCalculator.Compute(vorkomm, beschichtungTage, holidays,
  pickupDays, hasCoatingParts, featureActive, businessDayService)` → `DateTime?`), den beide
  Controller nutzen. Unit-testbar.
- **View** FaWorklist/Index.cshtml: neue Spalte „Beschicht." (col-key `coating-date`), **vor**
  `bg-date` (konsistent mit FA-Liste/Leitstand-Reihenfolge), filterbar (Datum, KW-Format), th + td
  (`FormatDateWithKw`) + inline `#column-config`-Eintrag. `columnCount` von `9 + attrs + 1` auf
  `10 + attrs + 1`.
- `BuildColumnMap`: `["coating-date"] = r => FormatDateForFilter(r.BeschichtungsTermin)`.

### 4.1 Tests
- `CoatingDateCalculator`: feature inaktiv (LackierteilKategorieName leer) → Termin für alle; aktiv +
  HasCoatingParts=false → null; aktiv + true → berechneter Termin (SubtractBusinessDays +
  FindPreviousPickupDay). (Pure Unit-Tests; `IBusinessDayService` real oder Fake.)
- `FaWorklistController`: Row trägt `BeschichtungsTermin`; `colf_coating-date` filtert.

## 5. Change 3 — Filter erst nach ENTER (nur Server-Mode)

`wwwroot/js/table-filter.js`:
- **Server-Mode** (`isServerColumnFilter()`): Der `input`-Listener navigiert **nicht** mehr (kein
  Debounce-Reload beim Tippen). Stattdessen ein `keydown`-Listener je Filter-Input: bei `Enter`
  → `applyServerFilters()` (sofort, ohne 500-ms-Timer; baut die `?colf_*`-URL + `page`-Reset wie
  heute `scheduleServerNavigate`, nur ohne `setTimeout`).
- **Programmatische Setzungen** (Kalender-KW/Tag-Klick, „Filter entfernen"-Button,
  `window.setColumnFilter`): setzen weiterhin `input.value`, aber statt ein `input`-Event zu
  dispatchen rufen sie eine einheitliche `applyColumnFilter()`-Funktion auf → Server-Mode:
  `applyServerFilters()` (sofort navigieren); Client-Mode: `applyFilters()` (live). So wirken
  Kalender-Auswahl + Filter-entfernen weiterhin **sofort**, nur reines Tippen wartet auf ENTER.
- **Client-Mode** (`filterable-table` ohne `data-server-column-filter`): unverändert (`input` →
  `applyFilters`, live).
- Kein Backend, keine ViewModels, keine Migration. Rein `table-filter.js`.

### 5.1 Tests
Manuell (TESTSZENARIEN): Server-Mode-Tabelle (z. B. FA-Liste) — Tippen löst KEINEN Reload aus; ENTER
filtert; Kalender-KW-Klick filtert sofort; „Filter entfernen" wirkt sofort. Client-Mode (z. B.
Tracking/ByWorkplace) — unverändert live. (Reines JS → kein xUnit.)

## 6. Doku & Versionierung
- `Changelog.cshtml` v1.23.0-Karte: 3 Bullets.
- `CLAUDE.md`: Fallstrick `FaWorkStep`-Completion (IsCompleted→Status-Enum, 3 States, set-status-API,
  Ausblende = Fertig), Pagination/Filter-Abschnitt (Server-Mode-Filter erst bei ENTER), Migration-76-Hinweis.
- `docs/TESTSZENARIEN.md`: neues Kapitel (Status-Dropdown Abarbeitungsliste+Leitstand,
  Beschichtungstermin, Filter-ENTER).
- `Views/Help/Index.cshtml`: kurzer Hinweis (Status-Dropdown + Filter-ENTER).
- `PROJECT_STATUS.md`: Eintrag.

## 7. Risiken / offene Punkte
- **Migration 76 ist daten-konvertierend + droppt `IsCompleted`** — wie ShortageStatus (v1.19.0).
  `Down()` verliert die Offen/InBearbeitung-Unterscheidung. DB-Backup vor Deploy empfohlen.
- **`IsCompleted`-Vollständigkeit:** alle 8 Lesestellen (FaWorklistController, FaWorklist-View,
  PickingLeitstand-View, FaWorkStepRepository 2×, IFaWorkStepRepository-Record, ApiController) auf
  `Status` umstellen — Build deckt Compile-Lücken; Plan listet jede Stelle.
- **Change 3 ist global** (jede Server-Mode-Tabelle). Risiko: Kalender/Clear-Pfade müssen weiter
  sofort wirken (sonst „Filter klickt nicht"). Der Plan testet diese Pfade explizit (TESTSZENARIEN).
- Die VK-VA-Spalten im Leitstand zeigen bewusst nur die 5 statischen Codes (YAGNI, unverändert).
