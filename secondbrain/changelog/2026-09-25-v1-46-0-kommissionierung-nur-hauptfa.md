---
type: changelog
version: 1.46.0
date: 2026-09-25
---
# v1.46.0 — Kommissionierung nur am HauptFA: „Alle Ziele“, Rückbau Freigabe-Kaskade

Umsetzung von [[2026-09-25-kommissionierung-nur-hauptfa-spec]] im **Bündel-**Worktree
`feature/2026-08-07-ideal-teile-1-5`, Umsetzungsnotiz [[2026-09-25-kommissionierung-nur-hauptfa-umsetzung]].
Anlass [[2026-09-13-kommissionierung-nur-hauptfa]]. **Keine Migration.**

**Warum:** Kommissioniert wird ausschließlich am HauptFA (komplette Stücklisten-Vollstruktur). Damit verlor
die in v1.38.0 gebaute Freigabe-Kaskade für Sub-FAs ihre Bedeutung, und Freigabe-/Kommissionier-Elemente an
Sub-FA-Zeilen wurden zu Bedienelementen ohne Wirkung. Gleichzeitig brauchte die lange Vollstruktur einen
Filter „nur was zu holen ist“.

## Kernpunkte

- **Eine Sub-FA-Definition:** `ProductionOrder.IsHauptFa` (EF-übersetzbar); `IsSubFa`/`IsSubFaOf` daraus
  kompiliert. Ersetzt fünf abweichende Fassungen (inkl. `BomScopes.ForOrder`, `HierarchicalDataExistsAsync`,
  zwei Views). `""` gilt überall als HauptFA ([[fallstricke]] §17).
- **Warteschlange nur HauptFA:** vier Queue-Abfragen `.Where(IsHauptFa)`; Count/Max auf `ProductionOrders`
  als Wurzel (1:1). Alte, freigegebene Sub-FAs tauchen nicht mehr auf.
- **Sperren:** `Picking/Bom` und `ToggleRelease` weisen Sub-FAs mit Hinweis ab (beide Richtungen);
  `BulkRelease` überspringt Sub-FAs und meldet sie mit ihrer Sub-FA-Nummer (ein gemeinsamer Hinweis mit
  „keine Artikelnummer“). Leitstand/FA-Liste blenden Freigabe, Bulk-Checkbox und interaktive Stückliste auf
  Sub-FA-Zeilen aus; die schreibgeschützte Stückliste in der FA-Liste nur mit Vorbau-Recht.
- **Rückbau Freigabe-Kaskade** (v1.38.0): Actions, Repository-Methoden, Button/Modal/JS, Tests entfernt;
  Anwender-Changelog und TS-73 als zurückgenommen markiert.
- **„Alle Ziele“ (`!(leer)`)** als exklusive erste Option im Komm.-Ziel-Filter der Stückliste — Bedeutungswert,
  neue Ziele automatisch enthalten; Profil-Freitext wird beim Speichern normalisiert (Hinweis nur, wenn
  Einzelziele verworfen wurden). Drucken ohne sichtbare Positionen öffnet keinen Ausdruck, sondern zeigt
  einen Hinweis.
- `KommissionierRelevanzFilter.IsRelevant` als Ort der Kommissionierlisten-Regel extrahiert (Verhalten gleich).

## Commits

`d3daf7ed` (Plan) … `ee583ed7` (Rückbau), `a30e7647` (IsHauptFa/Queue), `cba1be97`/`ffb1572e`/`a5bc8367`/
`b9991917` (Guards/Views/eine Definition), `855f7310` (Relevanzfilter), `f40053d3` (Normalisierung),
`6a6adee6`/`4467b894` (Stückliste), `50582f81`/`c61cb796`/`30c9c20e` (Version, Doku). Merge-Commit: folgt mit
dem Bündel.

## Deploy-Risiken

Nur Web-Publish. Keine Migration, kein Service. **Vor dem Test im Testsystem einmalig** freigegebene Sub-FAs
zurücksetzen (SQL in der Spec, `SELECT COUNT(*)` vorab) — nicht produktiv, das Bündel ist nicht gemergt.
Verhaltensänderung für Anwender: Sub-FAs sind im Leitstand nicht mehr freigebbar; Kommissionierer sehen
in der Warteschlange nur HauptFAs (die Zwei-Ebenen-Gruppierung wird damit überflüssig →
[[2026-09-25-picking-warteschlange-gruppierung-rueckbau]]).
