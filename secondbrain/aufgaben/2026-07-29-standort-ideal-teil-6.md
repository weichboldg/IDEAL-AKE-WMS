---
type: aufgabe
title: "IDEAL-Standort Teil 6 — Standorteinstellungen-Maske (Umsetzung)"
status: InUmsetzung
spec: "[[2026-07-29-standort-ideal-teil-6-spec]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
created: 2026-08-18
updated: 2026-08-18
---

# Teil 6 — Standorteinstellungen-Maske

Umsetzung der freigegebenen Spec [[2026-07-29-standort-ideal-teil-6-spec]] (kein Epic) im **Bündel-**
Worktree `feature/2026-08-07-ideal-teile-1-5` (gemeinsam mit Teilen 1–5, 7, 8). Grund: Teil 6
`depends_on` Teil 7 (Master-Status/Umschaltseite) + Teil 3 (Firmendaten-Fallback) — die existieren nur
im Bündel-Worktree, nicht auf `main`. Der /dev-worktree-Leer-Gate greift daher hier bewusst nicht;
Schranke 1 ist genommen (freigabe_von Gerald Weichbold, 2026-08-12), worktree/branch waren als Teil der
Freigabe auf den Bündel gesetzt. Kein Zwischen-Merge.

## Kuratierte Maske (Gruppen + Keys)
- **Firmendaten (NEU, AppSettings):** `Firmenname`, `Firmenanschrift` — Keys entstehen hier
  (`AppSettingKeys.cs`, Fallback-Regel: Teil 3 hat sie nicht angelegt). Kein Seed/Migration.
- **Mandant / Views (ServiceSettings):** `SData:Dataset`, `Sync:FaHierarchyListeViewName`,
  `Sync:FaHierarchyInfosViewName` (View-Namen roh gespeichert; Validierung sitzt im Sync — UX-Hinweis
  in der Maske, H-1).
- **Feature-Toggles (AppSettings):** `FaHierarchyMaxTiefe` (Int), `FaHierarchyKommissionierlistenAktiv`,
  `FaHierarchyBeschichtungAktiv`, `FaHierarchyVormontageAktiv` (Bool, invertierte Default-aus-Semantik).
- **Struktur-Import (ServiceSettings):** `Sync:HierarchicalFaEnabled` (Bool).
- **Master (READ-ONLY):** `ProduktionsauftragHierarchisch` — Anzeige an/aus (Key-Wert) + gesperrt ja/nein
  (`HierarchischeStrukturStatus.HierarchicalDataExists`) + Stand (`LastRefreshedUtc`) + Link zu
  `/HierarchieUmstellung`. **Nie** Teil des Sammel-POST, ruft `HierarchischeStrukturGuard` nicht auf.
- **Nicht in der Maske (kein Key vorhanden):** PPS „berechnend vs. View" — `Neuer_PT_PPS` wurde bewusst
  nie gebaut (Teil 5), daher kein Bedienelement. In der Spec als Beispiel genannt, aber kein Key da.

## Transaktions-Design (B-1 / Z2-S2)
- Neuer schmaler Baustein `IStandortSettingsWriter` (`StandortSettingsWriter`) — der einzige Ort mit
  direktem `ApplicationDbContext`-Zugriff (spec-erlaubter dünner UoW statt Context im Controller).
- `SaveAtomicAsync(app kv, service kv)`: `BeginTransactionAsync` → App- + Service-Settings auf dem
  geteilten Context stagen → **ein** `SaveChangesAsync` → `CommitAsync`. Bei Fehler `RollbackAsync` +
  ChangeTracker zurücksetzen (Fallstrick: geteilte Instanz driftet sonst).
- **Cache-Invalidierung erst NACH Commit** (Fallstrick `CachedSettingRepository` entfernt sonst zu
  früh); `CachedSettingRepository.CachePrefix` wird dafür `public`.
- **Int-Validierung im Controller VOR dem Write** (kein Partial-Save; bei Fehler View mit
  eingegebenen Werten zurück, nichts geschrieben) — spiegelt `ServiceSettingsController.SaveSettings`
  (Checkbox→"true"/"false", `int.TryParse` invariant).

## Dateien (Worktree)
- `Models/AppSettingKeys.cs` — Firmenname/Firmenanschrift.
- `Services/Standort/StandortSettingsWriter.cs` (+ `IStandortSettingsWriter`), DI in `Program.cs`.
- `Data/Repositories/CachedSettingRepository.cs` — `CachePrefix` public.
- `Controllers/StandortEinstellungenController.cs` (neu, `[RequireAdminAccess]` class-level).
- `Models/ViewModels/...` StandortEinstellungenViewModel (App+Service gemeinsam) + Master-Status.
- `Views/StandortEinstellungen/Index.cshtml` (neu, frontend-design PFLICHT: Bootstrap-Konsistenz mit
  `/ServiceSettings`, WCAG AA).
- Tests: Controller (GET rendert Gruppen, POST atomar, Master nicht schreibbar, Int-Fehler kein
  Partial-Save, Admin-only) + StandortSettingsWriter (atomar/Rollback).
- `docs/TESTSZENARIEN.md` (Kapitel TS-67), Versions-Bump.

## Brain (HAUPTCHECKOUT)
codebase/controller.md (neuer Controller, kein neuer Filter/Rolle), testszenarien-index TS-67,
changelog, feature-map Teil 6.

## Fortschritt
- Setup (Spec InUmsetzung, Aufgabe) — **erledigt**.
- Umsetzung — offen.
