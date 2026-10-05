---
type: changelog
version: 1.36.0
date: 2026-09-08
---
# v1.36.0 — IDEAL: BOM-Bridge — Stückliste über die Repository-Schnittstelle

Umsetzung der freigegebenen Spec [[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]] im
**Bündel-**Worktree `feature/2026-08-07-ideal-teile-1-5` (Entscheidung des Menschen 2026-09-08: im
Bündel-Zweig, nicht aus `main`; kein Zwischen-Merge, Schranke 2 fürs ganze Bündel). Umsetzungsnotiz
[[2026-09-08-bom-schnittstellen-bridge-hierarchisch-umsetzung]], Entscheidung
[[0013-bom-bridge-repository-schnittstelle-statt-cache-kopie]], Plan `docs/superpowers/plans/2026-09-08-bom-bridge.md`.

**Warum:** Der Minimal-Guard ([[2026-08-18-bom-guard-hierarchisch-spec]]) hatte den BOM-Knopf im
hierarchischen Modus nur abgefangen (Hinweisseite). Damit die AKE-Maschinerie — Picking-Workflow mit
Pick-Status je Zeile, Vorbau-/Vervollständigungs-Stückliste, Bedarfsmeldung/Lagerbestellung aus einer
Zeile, Artikelinfo — für IDEAL läuft, liefert jetzt eine **zweite Implementierung der bestehenden
Schnittstelle** die Stückliste direkt aus der lokalen FA-Struktur. **Nicht** gebaut: eine Kopie in den
persistenten AKE-BOM-Cache (verworfen — Artikel-Schlüssel und Menge-je-Stück passen nicht zu
auftragsbezogenen Sollmengen; Details im ADR). **Nur bei Master `ProduktionsauftragHierarchisch =
true`; bei `false` bit-identisch zu AKE (eigene Klasse).**

## Umgesetzt (Tasks 1–6)

- **1 — Typen + Signaturwechsel** (`f89659f`): `BomKey(ArticleNumber, SubOrderNumber, OrderNumber)`
  statt `string` an `IBomRepository.GetBomItemsAsync` und `IBomCacheRepository.GetByArticleNumberAsync`;
  `BomScope` (`DirectChildren`/`FullStructure`, `BomScopes.ForOrder`: HauptFA → FullStructure);
  `BomQueryResult.MengeIstAuftragsmenge` + `BomQuantityResolver` als **einzige** Multiplikationsstelle
  (Picking.Bom, PrintBom, ReadOnlyBomBuilder). Flache Implementierungen lesen nur `ArticleNumber`.
- **2 — `FaHierarchyBomRepository`** (`e99bcf3`, Review-Fix `3784e4b`): beide Interfaces aus
  `FaHierarchyNode` via `IFaHierarchyNodeRepository` (5-min-Cache aus Teil 1), Traversal über den
  bestehenden `FaHierarchyTreeBuilder` (kein zweiter Walker). Mapping nach Anhang: Kopf = Artikel des
  Elternknotens (Wurzel: `HauptArtnr`), `Ressourcenummer = Artnr`, `Baugruppe` = Eltern-Artnr,
  `IsBaugruppe = SubFA != 0`, `Beschaffungsartikel` Ja/Nein, `Menge = Sollmenge`. `FullStructure`:
  rekursiver Positions-Pfad ab der Wurzel (`3.7.2`), Sage-Position separat (`SagePosition`),
  Geschwister-Kollisionen `~n` + Flag + Log (Freigabe-Bedingung b), Blatt-Waisen `W<n>` markiert.
  Reverse-Lookup für Artikelinfo liefert Eltern-Sub-FA-Nummern + Σ Sollmenge. Schreibmethoden No-op.
- **3 — Master-Weiche, Guard raus** (`611475d`): `BomRepositoryMasterSwitch` bedient **beide**
  Interfaces, entscheidet pro Aufruf, Ziele lazy per Delegate (sonst DI-Zyklus über
  `BomRepository → IBomCacheRepository`); `Program.cs` beide Interfaces → dieselbe scoped Instanz.
  `HierarchicalBomGuardRepository` + Tests + `BomDataSources.HierarchicalUnavailable` + Hinweis-Zweig in
  `Bom.cshtml` entfernt; Picking/ReadOnlyBomBuilder mappen `FaHierarchyBomItem`-Felder,
  `BomViewModel.Hierarchical/FullStructure`.
- **4 — Klasse-D-Gates** (`56e8faa`): `IHierarchicalModeReader` (DB-first) im Service;
  `CoatingDetectionService`, `FaWorkStepDetectionService`, `BomCacheSyncService` (**beide** Einstiege,
  inkl. `SyncSpecificArticleNumbersAsync` aus `SageImportService`) überspringen bei Master `true`
  ohne DB-Zugriff, geloggt als Skip (`uebersprungen_hierarchisch`).
- **5 — Views** (`cd34b08`, `86371ee`, Review-Fix `a3f125e`): `Bom.cshtml` Spalten Komm.-Ziel,
  Hauptlagerplatz, Sage-Pos. (default ausgeblendet), bei Vollstruktur Ebene/Vater-Sub-FA; Badges
  „Waise"/„Kollision"; Tooltip Sage-Position; Registrierung in `ColumnDefinitions.Bom` **und** im
  Inline-`#column-config` (Review-Fund: der Client liest nur Letzteres). Artikelinfo: HauptFA als Gerät
  + Sub-FA-Spalte, `GetBySubOrderNumbersAsync`, Verbrauch = Σ Sollmenge ohne × Stückzahl. Navigation:
  Dropdown „Kommissionierung" (Picking-Workflow + Kommissionierlisten) **nur** wenn beide verfügbar,
  sonst bisheriger Einzel-Link (AKE unverändert).
- **6 — Version/Doku** (`8d9468d`): 1.35.0 → **1.36.0** (Web+Service), Anwender-Changelog, Hilfe,
  TS-70 (70.1–70.11) in `docs/TESTSZENARIEN.md`, **TS-68 als abgelöst markiert**, DI-Auflösungstest
  `BomDiResolutionTests` (schützt die lazy Weiche gegen Rekursion).
- **Final-Review-Fixwelle** (`22d31ae`): `PrintPicking` auf `BomScopes.ForOrder` (sonst leere
  Bezeichnungen tieferer Ebenen im Kommissionierschein); deterministischer Tiebreak
  (`ThenBy SubFA, Artnr`) für Kollisions-Suffixe (sonst wandernder Pick-Zustand nach Cache-Ablauf);
  Warnung bei zweiter echter Wurzel; TS-70.7 (drei Protokoll-Läufe, spezifischer BOM-Cache-Pfad nur
  Serilog) + TS-70.11 (Druck ohne hierarchische Spalten, bewusst); Hilfe/Changelog: Sage-Pos. per
  Zahnrad einblenden.

## Migration / Deploy

- **Migration: keine.** `PickingItem.BomPosition` (`NVARCHAR(50)`) fasst den rekursiven Pfad.
- **Deploy: `web: true`, `service: true` (Gates + Konstruktor-Signaturen), `migration: false`.**
- **Kontext:** Teil des ungemergten Bündels (Teile 1–8 + FA-Liste-Hierarchie + diese Spec) — Schranke 2
  für alles zusammen, ein Merge. **Vorher:** `publish.zip`-Blob (Commit `549c5db`) aus der History.

## Tests

qa-agent-Lauf 2026-09-08 (Worktree-HEAD `22d31ae`, nach der Final-Review-Fixwelle): `dotnet build
IdealAkeWms.slnx` → 0 Fehler. `dotnet test IdealAkeWms.Tests` → **1266 erfolgreich, 1 übersprungen
(vorbestehend), 0 Fehler, gesamt 1267.** `dotnet test IDEALAKEWMSService.Tests` → **236 erfolgreich,
0 Fehler.** AK 1–16 gegen den echten Diff `25399be..22d31ae` abgeglichen, Guard-Grep
(`HierarchicalUnavailable`/`HierarchicalBomGuardRepository`) in Quelldateien leer, `publish.zip` in
keinem Commit dieser Spec. Neue Tests: `FaHierarchyBomRepositoryTests` (13),
`BomRepositoryMasterSwitchTests` (4), `BomQuantityResolverTests` (2), `HierarchicalModeGateTests` (4),
`BomDiResolutionTests` (2), Controller-/Builder-/Artikelinfo-/Repository-Tests (je 1–2). Status:
**Testbereit** — der manuelle Rest ist TS-70 am IDEAL-Testsystem (Vollansicht, Kollision, Menü,
Klasse-D-Skip im Protokoll, Druck) und die AKE-Regression auf einer AKE-Instanz.

## Merge-Commit

_(offen — Schranke 2, Mensch; ganzes Bündel in einem Merge)_
