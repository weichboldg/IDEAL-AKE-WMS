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

`9208c3ff` (Plan) … `36911f2f` (Rückbau), `2291c950` (IsHauptFa/Queue), `35a65d36`/`23942f73`/`a3440bc3`/
`8d8bf8a5` (Guards/Views/eine Definition), `7d1cd74e` (Relevanzfilter), `f6911db4` (Normalisierung),
`e2d6e5d9`/`4fffeae9` (Stückliste), `2290c99c`/`1f128c61`/`787c5c90` (Version, Doku). Merge-Commit: folgt mit
dem Bündel.

## Deploy-Risiken

Nur Web-Publish. Keine Migration, kein Service. **Vor dem Test im Testsystem einmalig** freigegebene Sub-FAs
zurücksetzen (SQL in der Spec, `SELECT COUNT(*)` vorab) — nicht produktiv, das Bündel ist nicht gemergt.
Verhaltensänderung für Anwender: Sub-FAs sind im Leitstand nicht mehr freigebbar; Kommissionierer sehen
in der Warteschlange nur HauptFAs (die Zwei-Ebenen-Gruppierung wird damit überflüssig →
[[2026-09-25-picking-warteschlange-gruppierung-rueckbau]]).
