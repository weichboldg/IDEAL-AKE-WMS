---
type: aufgabe
title: "IDEAL: BOM-Bridge — Stueckliste ueber die Repository-Schnittstelle (Umsetzung)"
status: Testbereit
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

> **STAND 2026-09-08 — QA durch → Testbereit.** Plan
> `docs/superpowers/plans/2026-09-08-bom-bridge.md` (Worktree, Commit `25399be`); Umsetzung
> subagent-getrieben (6 Tasks, je Task-Review; Fixrunden: Task 2 ×1, Task 5 ×1); Final-Review-Fixwelle
> `22d31ae` (PrintPicking-Scope, deterministischer Kollisions-Tiebreak, Wurzel-Warnung,
> TS-70.7/Druck-Doku, Hilfe). Version **v1.36.0** (Web + Service), keine Migration. qa-agent-Lauf
> 2026-09-08: Build 0 Fehler, Web **1266 grün + 1 skip**, Service **236 grün**, AK 1–16 abgeglichen,
> Guard-Grep leer, `publish.zip` in keinem Commit. Worktree-HEAD `22d31ae`. Ledger
> `.superpowers/sdd/2026-09-08-bom-bridge/progress.md`. Wartet jetzt mit dem ganzen Buendel auf
> Schranke 2 (Mensch: Manual-UAT + ein Merge).

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
| 1 | Typen `BomKey`/`BomScope`, `BomQueryResult.MengeIstAuftragsmenge`, `BomQuantityResolver`; Signaturwechsel `IBomRepository`/`IBomCacheRepository.GetByArticleNumberAsync` + alle Aufrufer/Tests (AKE bit-identisch) | **erledigt** `f89659f` (Review clean) |
| 2 | `FaHierarchyBomRepository` (beide Interfaces; DirectChildren/FullStructure mit rekursivem Pfad + Kollisionspruefung; Reverse-Lookup Artikelinfo; No-op-Schreibmethoden) + Tests inkl. Property-Test | **erledigt** `e99bcf3` + Fix `3784e4b` (Wurzel-Kopf `HauptArtnr`, Kollisions-Log DirectChildren) |
| 3 | DI-Weiche `Program.cs` (beide Interfaces), Guard entfernen (+Tests), Scope-Regel in `PickingController.Bom` | **erledigt** `611475d` (Review clean; `BomRepositoryMasterSwitch`, lazy Delegates) |
| 4 | Klasse-D-Gates: `CoatingDetectionService`, `FaWorkStepDetectionService`, `BomCacheSyncService` (beide Einstiege) + Tests | **erledigt** `56e8faa` (ueber `IHierarchicalModeReader`, Review clean) |
| 5 | Views: `Bom.cshtml` (Kommissionieren/Hauptlagerplatz/Ebene/Vater-Sub-FA/Sage-Position, nur hierarchisch), `ColumnDefinitions.Bom`, `Articles/Info` (HauptFA primaer, Sub-FA Zusatz), `_Layout` Dropdown „Kommissionierung" | **erledigt** `cd34b08` + `86371ee` (Nav nur bei zwei Eintraegen) + Fix `a3f125e` (`#column-config`) |
| 6 | Version v1.36.0, Changelog, TESTSZENARIEN TS-70 (TS-68 abgeloest), Hilfe, DI-Aufloesungstest; qa-agent → Testbereit; Brain (ADR 0013, fallstricke §10, services, feature-map, changelog, testindex) | **erledigt** `8d9468d` + Fixwelle `22d31ae`; qa-agent 2026-09-08: Build 0 Fehler, Web 1266 grün + 1 skip, Service 236 grün → **Testbereit** |

## Entscheidungen im Dev-Lauf (Rulings, vollstaendig im Ledger)

- `BomKey` traegt zusaetzlich `OrderNumber` (HauptFA) fuer den gecachten Gruppen-Lookup
  `GetByHauptFaAsync` — Spec nannte nur (ArticleNumber, SubOrderNumber); flache Impl ignoriert es.
- Master-Read im Service ueber `IHierarchicalModeReader` (DB-first) statt direkt — Gates ohne SQL
  Server testbar.
- `FullStructure` an einem Sub-FA faellt auf `DirectChildren` zurueck (Fachliche Anforderung 6
  definiert FullStructure nur an der HauptFA).
- Scope-Regel (HauptFA → FullStructure) gilt auch fuer `PrintBom` und `ReadOnlyBomBuilder`
  (Vorbau/FaCompletion) — „Stueckliste des aktuellen FA" ist ueberall dieselbe.
- Blatt-Waisen: Pfad-Praefix `W<n>` (ohne Punkt = Top-Level), `IsWaise`.
- `IsBaugruppe` hierarchisch: `SubFA != 0`; `Baugruppe` = Eltern-Artnr (String-Referenz wie AKE).
- Navigation: Dropdown „Kommissionierung" NUR bei beiden Eintraegen; Lager-only-Nutzer behalten den
  Kommissionierlisten-Link (jetzt im Kommissionierung-Slot).
- Razor-Tooltip als Attribut*wert* `title="@(… : null)"` (Brief-Vorlage war kaputt); „Geplanter
  Verbrauch"-Label existiert nicht → Tooltip an „Verfuegbarer Bestand".
- TS-68 wird in TESTSZENARIEN als abgeloest markiert (Plan-Defekt: nur TS-70 anfuegen haette die
  Abnahme in die Irre gefuehrt); DI-Aufloesungstest `BomDiResolutionTests` ergaenzt.
- Task-2-Reverse-Lookup-Finding (Eltern-SubFAs statt Artikelnummern) ist plan-mandated und wurde in
  Task 5 (`ArticlesController`) aufgeloest.

## Review-Befunde, bewusst offen (deferred, fuer den naechsten Zyklus)

Vollstaendige Liste im Ledger (`minor (deferred)`), Auswahl: identische Mapping-Bloecke in
`PickingController`/`ReadOnlyBomBuilder` (Mapper-Helper), vier identische Gate-Bloecke, zwei
Settings-Reads je BOM-Seite flach, Depth-Cap hardcoded statt AppSetting, `PrintBom.cshtml`-Whitelist
ignoriert hierarchische Spalten (Ausdruck ohne Komm.-Ziel/Ebene), Badges nur `title` (kein
bs-tooltip), `warehouse-order` fehlt in `ColumnDefinitions.Bom` (vorbestehend), TreeBuilder-Quirk
`VaterFA==0`.
