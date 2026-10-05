---
typ: notiz
spec: "[[2026-09-28-ake-hotfix-stueckliste-druck-lagerbestellung-ist-spec]]"
status: erledigt
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
---
# Vorwaerts-Merge main → Buendel: AKE-Hotfix 1.30.1 (v1.47.1) — Merge-Protokoll

Pflicht-Deliverable laut Spec-Abschnitt „Uebertrag ins Buendel": tatsaechliche Konfliktdateien gegen die
Vorhersage — erste Probe, wie weit `main` und das Buendel seit August auseinanderliegen.
Merge-Commit **`6c253c60`** (Buendel), Quelle `main` mit `f89c0124`. Changelog
[[2026-10-05-v1-47-1-buendel-ake-hotfix-uebernommen]].

## Vorpruefung

- Kein `/dev`-Lauf aktiv (keine Spec `InUmsetzung`; drei alte Aufgaben-Notizen tragen noch `InUmsetzung`
  ohne laufenden Lauf: pdf-erzeugung, matchcode, ideal-bde — Status-Leichen, separat aufraeumen).
- `main` enthaelt `f89c0124` ✓.
- Worktree **nicht** sauber: lokale Aenderung an `CLAUDE.md` (Abschnitt „Skill ponytail") — gemeldet, auf
  Entscheidung des Menschen (Variante a) verworfen; der Abschnitt ist in `main` identisch enthalten und kam
  mit dem Merge zurueck.

## Konflikte: Vorhersage vs. tatsaechlich

| Datei | Vorhersage (Spec, merge-tree 2026-09-28) | Tatsaechlich | Aufloesung |
|---|---|---|---|
| `IdealAkeWms/AppVersion.cs` | Konflikt | **Konflikt** | `1.47.1`, Date `2026-10-05` |
| `IDEALAKEWMSService/AppVersion.cs` | Konflikt | **Konflikt** | `1.47.1`, Date `2026-10-05` |
| `Views/Help/Changelog.cshtml` | Konflikt (beide oben) | **Konflikt** | neuer v1.47.1 oben, dann Buendel v1.47.0…v1.31.0, dann v1.30.1, v1.30.0 (Versionsfolge) |
| `Views/Picking/Bom.cshtml` | Konflikt erwartet | **auto-gemergt** | Leerzustand-Sperre `:1232-1239` steht vor dem POST-Formular `:1274`; kein `window.open(url` mehr |
| `Controllers/PickingController.cs` | Konflikt moeglich | **auto-gemergt** | `RenderPrintBomAsync` (`:585`) = Buendel-Rumpf, byte-gleich bis auf `View("PrintBom", vm)` (skriptgestuetzt verglichen) |
| `Views/Picking/PrintBom.cshtml` | evtl. | unberuehrt | — |
| `docs/TESTSZENARIEN.md` | Konflikt (Ende-Zeile) | **auto-gemergt** | TS-5.11 / TS-18.10 + Index-Tabelle vorhanden |
| WarehousePicking-Dateien | konfliktfrei | konfliktfrei | — |
| `secondbrain/` | 0 | 0 (Brain-Commits von main reisen im Merge mit; Worktree blendet den Ordner aus) | — |

**Befund:** 3 Konflikte statt der befuerchteten 5-6; alle rein „beide Seiten haben dieselbe Zeile
fortgeschrieben" (Version, Changelog-Kopf). Die inhaltlich riskanten Dateien (`Bom.cshtml`,
`PickingController.cs`) hat Git sauber zusammengefuehrt — trotzdem einzeln belegt.

## Pflicht-Nachzug + Signatur-Suche

- `IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs` Z.1031/1056/1091/1125: vier
  Buendel-Tests `GetMissingPartsAsync_ArticleNumberColumnFilter_*` riefen `CloseAsync` mit
  `Dictionary<int, decimal>` → `decimal?` (sonst nichts geaendert).
- Suche im ganzen Buendel: keine weiteren `CloseAsync`-Aufrufer ausser Controller/Repository; keine
  Moq-Setups mit `IReadOnlyDictionary<int, decimal>>`; alle `Close`/`PrintAndClose`-Testaufrufe mit
  `int?[]`; kein GET-Link auf `PrintBom` mit `visiblePositions` (die Summierungs-Spec nutzt fuer die
  Kommissionierliste nur `visibleColumns` per GET — Spaltenliste, unkritisch).

## Nachweis

Build gruen (8 vorbestehende Warnungen). Tests vorher Web 1435/1436, Service 268/268 → nachher Web
**1449/1450** (+14 = Hotfix-Tests: Bindung 4, Routing 2, Controller 8), Service **268/268**.
Kein Push, kein Merge nach main, kein Publish; Einmal-Skript nicht ausgefuehrt.
