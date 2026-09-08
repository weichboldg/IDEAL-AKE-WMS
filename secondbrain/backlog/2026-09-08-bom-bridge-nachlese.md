---
typ: feature
---
# BOM-Bridge: Nachlese aus Task-Reviews + Final-Review (2026-09-08)

Gesammelte, bewusst **nicht merge-blockierende** Befunde aus der Umsetzung von
[[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]] (Ledger im Worktree
`.superpowers/sdd/2026-09-08-bom-bridge/progress.md`, Final-Review 2026-09-08). Alle Punkte sind im
Review geparkt worden; die Begruendungen stehen dort. **Einmal gesammelt abarbeiten**, nach dem
Buendel-Merge, aus `main`.

## A — Kleine Code-Haertungen (je < 1 h)

1. **Gemeinsamer `BomItemViewModel`-Mapper.** Die sieben `(bom as FaHierarchyBomItem)?`-Zeilen stehen
   identisch in `PickingController.Bom` und `ReadOnlyBomBuilder.BuildAsync` — klassische Driftstelle,
   sobald ein Feld dazukommt.
2. **Depth-Cap aus AppSetting.** `FaHierarchyBomRepository.BuildFullStructure` nutzt
   `FaHierarchyTreeBuilder.DefaultDepthCap` statt `AppSettingKeys.FaHierarchyMaxDepth` (wie
   `FaHierarchyController`). Nur auf pathologischen Strukturen sichtbar.
3. **Kollisions-Log-Helper.** Identisches Warn-Template in `BuildDirectChildren` und `WalkChildren`;
   ein `LogCollision(parentSubFa, segment)` verhindert Drift.
4. **Gate-Helper im Service.** Vier identische Klasse-D-Gate-Bloecke (Coating/WorkStep/BomCache×2);
   nur Politur, braucht eine gemeinsame Basis.
5. **Reverse-Lookup ohne `GetAllAsync`-Scan.** `GetDeviceArticleNumbersByComponentAsync`/
   `GetComponentMengePerDeviceAsync` scannen je die ganze Tabelle (5-min-Cache faengt es heute ab);
   eine gefilterte Repo-Methode `GetByArtnrAsync` waere der saubere Weg, sobald `FaHierarchyNodes`
   waechst.
6. **`IBomCacheRepository.GetByArticleNumberAsync`-Kontrakt.** Doku sagt „null wenn kein Header";
   `FaHierarchyBomRepository` liefert immer ein Ergebnis. Entweder `null` bei leer oder XML-Doc
   praezisieren.
7. **`FullStructure`-Degradierung am Sub-FA** (faellt still auf `DirectChildren` zurueck) und
   Waisen-Strukturen mit `HasError` — je ein Info-/Warn-Log.
8. **Chevron ohne Kinder** in der Sub-FA-Ansicht: Baugruppen-Zeilen tragen `IsBaugruppe` (Chevron),
   haben in `DirectChildren` aber nie Kinder → Klick oeffnet nichts. Chevron nur rendern, wenn im
   Ergebnis Kinder existieren (oder in `DirectChildren` `IsBaugruppe` fuer die Anzeige unterdruecken).

## B — Tests nachziehen

9. Kind eines kollidierten Knotens (`"5~2.1"`) — die Pfad-Invariante der View haengt daran.
10. Waise mit eigenen Kindern (`W1.3`), `HasError`-Teilausgabe, Partition mit Waisen.
11. `UpsertBomAsync`/`GetArticleNumbersWithCoatingPartsAsync` No-op-Tests; 6 von 8 Forwardern der
    Weiche; `PrintBom`-`IsBaugruppe`-Zweig; Flachmodus-Markup-Snapshot (keine neuen Spalten).
12. `GetBySubOrderNumbersAsync_ReturnsEmpty_ForEmptyInput` unterscheidet Guard und Query nicht;
    `SubOrderNumber != null`-Ausschluss ungetestet.
13. `BomDiResolutionTests`: `ValidateOnBuild = true` wuerde auch den hierarchischen Zweig absichern.

## C — UI / Druck

14. **`PrintBom.cshtml` `ShowCol`-Whitelist** kennt die fuenf hierarchischen Keys nicht → Ausdruck ohne
    Komm.-Ziel/Hauptlagerplatz/Sage-Pos./Ebene/Vater-Sub-FA (in TS-70 als bewusst vermerkt).
15. Badges „Waise"/„Kollision" nur mit `title` — auf Touch-Terminals unerreichbar; `data-bs-toggle="tooltip"`
    wie in `Info.cshtml` waere konsistent (Text traegt die Bedeutung, daher nicht dringend).
16. `NaturalPositionComparer` sortiert `"5~2"` nach `"50"` (Fallback auf Ordinal) — kollidierte Zeile
    rutscht von ihrem Geschwister weg. Kosmetisch, nur auf anomalen Daten.
17. Artikelinfo: jede Zeile traegt denselben HauptFA-Linktext; die Sub-FA-Spalte disambiguiert.

## D — Vorbestehend, ausserhalb der Spec

18. **`warehouse-order`-Drift:** Spalte steht in `Bom.cshtml`-`thead` und `#column-config`, fehlt aber in
    `ColumnDefinitions.Bom` — dieselbe Klasse wie der in Task 5 behobene Fund (siehe
    [[fallstricke]] §10 „zwei Registrierungen").
19. TreeBuilder-Quirk: `VaterFA == 0` ist weder Wurzel noch Waise (`subFaSet` enthaelt die 0 der
    Blaetter) → Knoten faellt aus `FullStructure` ohne Marker. Geerbt aus Teil 2.

## Bezug

[[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]],
[[2026-09-08-bom-schnittstellen-bridge-hierarchisch-umsetzung]],
[[0013-bom-bridge-repository-schnittstelle-statt-cache-kopie]],
[[2026-09-08-ideal-code-review-nachlese]] (Vorlaeufer-Liste des Buendels).

## E — Nachtrag aus dem Re-Review der Fixwelle (2026-09-08)

20. **Rest-Tiebreak:** zwei Geschwister mit gleicher Position, gleichem `SubFA` (z. B. 0) UND gleichem `Artnr` erhalten ihr `~2` weiterhin nach Eingangsreihenfolge; `.ThenBy(n => n.Sollmenge)` wuerde das schliessen, falls IDEAL-Daten den Fall zeigen.
21. **Log-Volumen:** Kollisions- und Zweite-Wurzel-Warnungen feuern je Render (BOM-Ansicht, Druck), nicht einmal je Sync — bei dauerhaft fehlerhaften HauptFAs viele gleiche Zeilen im Serilog. Fallstrick-Kandidat, kein Code-Bug.
22. `PrintPicking` matcht je gepicktem Item per `FirstOrDefault` ueber die Vollstruktur (O(n·m)); bei realen Listengroessen harmlos.
