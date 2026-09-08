---
type: aufgabe
title: "IDEAL: BOM-Bridge — Stueckliste ueber die Repository-Schnittstelle (Umsetzung)"
status: InUmsetzung
spec: "[[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
created: 2026-09-08
updated: 2026-09-08
---

# BOM-Bridge hierarchisch (Umsetzung)

Umsetzung der freigegebenen Spec [[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]] im
**bestehenden Buendel-Worktree** (Entscheidung des Menschen 2026-09-08: „im Buendel-Zweig, nicht aus
`main`"; Ausgangs-HEAD `a8d75de`, Buendel-Stand `Testbereit` v1.35.0 → geht fuer diesen Block auf
`InUmsetzung`, Re-QA am Ende fuer das ganze Buendel). Kein Merge, kein Push.

> **STAND 2026-09-08 — gestartet.** Plan wird in `docs/superpowers/plans/2026-09-08-bom-bridge.md`
> (Worktree) geschrieben; Umsetzung subagent-getrieben. Version-Ziel **v1.36.0** (Web + Service,
> Service wegen Klasse-D-Gates), keine Migration.

## Verbindliche Vorgaben aus der Freigabe (Schranke 1)

- **Rueckfrage 1 bestaetigt:** `BomPosition` in `FullStructure` = **vollstaendiger rekursiver Pfad
  ab der Wurzel** (z. B. `3.7.2`), damit `TreeLevel`/`parentPos`/JS in `Bom.cshtml` unveraendert
  laufen. Kein flacher `VaterSubFA.Position`-Praefix.
- **Bedingung (a):** Die urspruengliche Sage-`Position` bleibt sichtbar (eigene Spalte oder
  mindestens Tooltip) — der Pfad ist ein Anzeige-Konstrukt.
- **Bedingung (b):** Pfad-**Kollisionen** (zwei Geschwister mit gleicher Position unter demselben
  FA) beim Aufbau erkennen und protokollieren/kennzeichnen — nie still ueberschreiben.
- Nebenpunkt (nicht loesen): lexikalische Sortierung der Punktnotation wie bei AKE belassen.

## Etappen / Tasks

| # | Task | Status |
|---|---|---|
| 1 | Typen `BomKey`/`BomScope`, `BomQueryResult.MengeIstAuftragsmenge`, `BomQuantityResolver`; Signaturwechsel `IBomRepository`/`IBomCacheRepository.GetByArticleNumberAsync` + alle Aufrufer/Tests (AKE bit-identisch) | offen |
| 2 | `FaHierarchyBomRepository` (beide Interfaces; DirectChildren/FullStructure mit rekursivem Pfad + Kollisionspruefung; Reverse-Lookup Artikelinfo; No-op-Schreibmethoden) + Tests inkl. Property-Test | offen |
| 3 | DI-Weiche `Program.cs` (beide Interfaces), Guard entfernen (+Tests), Scope-Regel in `PickingController.Bom` | offen |
| 4 | Klasse-D-Gates: `CoatingDetectionService`, `FaWorkStepDetectionService` (+`IConfiguration`), `BomCacheSyncService` (beide Einstiege) + Tests | offen |
| 5 | Views: `Bom.cshtml` (Kommissionieren/Hauptlagerplatz/Ebene/Vater-Sub-FA/Sage-Position, nur hierarchisch), `ColumnDefinitions.Bom`, `Articles/Info` (HauptFA primaer, Sub-FA Zusatz), `_Layout` Dropdown „Kommissionierung" | offen |
| 6 | Version v1.36.0, Changelog, TESTSZENARIEN TS-70, Hilfe; qa-agent → Testbereit; Brain (ADR 0013, fallstricke, services, feature-map, changelog, testindex) | offen |

## Entscheidungen im Dev-Lauf (werden hier nachgetragen)

- `IsBaugruppe` hierarchisch: `SubFA != 0` (robuster als Ressourcenummer-Ableitung) — vorgesehen.
- Traversal fuer `FullStructure`: Wiederverwendung des Zyklus-/Tiefenschutzes aus
  `FaHierarchyTreeBuilder` (kein zweiter Walker) — Form wird im Plan festgelegt.
