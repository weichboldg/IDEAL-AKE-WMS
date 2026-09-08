---
type: aufgabe
title: "BOM-Knopf im hierarchischen Modus abfangen (UAT-Blocker Minimal-Fix)"
status: Testbereit
spec: "[[2026-08-18-bom-guard-hierarchisch-spec]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
created: 2026-08-18
updated: 2026-09-08
---

# BOM-Guard hierarchisch (Minimal-Fix, UAT-Blocker)

Umsetzung der freigegebenen Spec [[2026-08-18-bom-guard-hierarchisch-spec]] (kein Epic) im **Bündel-**
Worktree `feature/2026-08-07-ideal-teile-1-5`. **VORGEZOGEN vor dem Merge** (Freigabe-Antwort 1): das
Bündel geht von `Testbereit` zurück auf `InUmsetzung`, danach QA erneut (Build + BEIDE Suiten).
Kein Zwischen-Merge, kein Bump (Version bleibt v1.31–v1.34).

## Befund
Nach Master-Flip stehen 130 materialisierte IDEAL-FAs in `ProductionOrders` → BOM-Knopf →
**HTTP 500** (`BomRepository.GetBomItemsAsync` fragt live gegen fest verdrahtete AKE-View
`[ake].[dbo].[vw_AKE_Kommissionierung_StuecklistenDB]`, die auf IDEAL nicht existiert).

## Umsetzung
1. **Guard-Decorator `HierarchicalBomGuardRepository`** (äußerster `IBomRepository`, vor
   `CachedBomRepository`→`BomRepository`): keyt am Master-Toggle
   (`IServiceSettingRepository.GetValueAsync(HierarchischeStrukturKeys.Master)`, wiederverwendeter
   Web-Leseweg), **kein try/catch** (Fallstrick 1). Master `true` → leeres `BomQueryResult` mit
   `DataSource = BomDataSources.HierarchicalUnavailable` ("NICHT_VERFUEGBAR_HIERARCHISCH"), **kein**
   DB-Zugriff. Master `false` → unverändert an den Cache/Live-Pfad delegiert (AK 1). Fängt **alle**
   Aufrufer ab (PickingController.Bom/PrintBom/×2, ReadOnlyBomBuilder → AK 5).
2. **Marker-Konstante** `BomDataSources.HierarchicalUnavailable` (Models/ViewModels) — Unterscheidbar
   von "KEINE_DATEN" (AK 4).
3. **Hinweis in `Views/Picking/Bom.cshtml`** (frontend-design, Bootstrap `alert-info`): „Die Stückliste
   wird im hierarchischen Modus über die FA-Struktur angezeigt" + Link `/FaHierarchy`, als
   **Zwischenstand** erkennbar (AK 6). Filter-Card + Tabelle im hierarchischen Modus ausgeblendet;
   Aktions-Karte entfällt ohnehin (`Model.Items.Any()`-Bedingung).
4. **`supportsSortDefault=true`** für die 3 flachen IDEAL-Listen (Umfang Punkt 4):
   `FaHierarchyKommissionierListen`, `FaHierarchyBeschichtung`, `FaHierarchyVormontageEinzeln`.
   Baum `FaHierarchyStructure` bleibt `false`. Grund weggefallen ([[2026-08-12-tabellen-sortierung-nur-erste-gruppe-bug]] Etappe-6-Fix).

## Out-of-Scope (Folge-Spec)
Hierarchische BOM-Quelle / `FaHierarchyBomRepository` / Cache-Umbau / Sweep aller `vw_AKE_`-Fundstellen
→ [[2026-08-18-ake-view-abhaengigkeiten-hierarchisch-spec]] (erster Block nächster Zyklus, aus `main`).

## Fortschritt
- Setup — **erledigt**.
- Umsetzung — **feature-complete** (Worktree `1173cf1`): Guard-Decorator + DI, `BomDataSources`-Marker,
  Bom.cshtml-Hinweis + Ausblenden + Badge, 3× `supportsSortDefault=true`, 5 Guard-Tests, TS-68 +
  TS-59.19/60.17/61.25 umgekehrt. Kein Versions-Bump. Build grün, Web-Suite **1219 grün** (+5).
- **QA (2026-08-18) — Testbereit.** Build grün, Web-Suite 1219/1 skip/0 Fehler, Service-Suite
  231/0 Fehler. Code-Review ohne Findings (Guard hängt strikt am Master, Marker unterscheidbar,
  alle 4 Aufrufer über den gemeinsamen `Views/Picking/Bom.cshtml`-Guard abgedeckt, DI-Factory ohne
  Selbstreferenz). Deploy-Abschnitt der Spec finalisiert (Web ja / Service nein / Migration nein,
  Publish-Befehl aus dem Worktree). Manuelle Test-Checkliste an die Spec angehängt. Wartet auf
  Schranke 2 (Mensch: Manual-UAT, dann Merge).
- **Re-QA Nachtrag UAT-Lauf 1 (2026-09-08) — weiterhin Testbereit.** Worktree-HEAD jetzt `a8d75de`
  (Befund U1: JS-Frühabbruch auf der Guard-Hinweisseite + Sub-FA-Kopfzeile). Build grün, Web-Suite
  **1245 grün +1 skip** (+2 ggü. `dde9a17`), Service-Suite **232 grün** unverändert. Diff-Review
  ohne Befund (AKE-Flachmodus bit-identisch, JS-Guard trifft nur den Hinweiszweig, `KEINE_DATEN`
  behält `#bomTable`). Details siehe Spec-Abschnitt „Re-QA Nachtrag UAT-Lauf 1 (2026-09-08)".
