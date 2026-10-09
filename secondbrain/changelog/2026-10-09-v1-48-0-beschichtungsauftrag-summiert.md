---
type: changelog
version: 1.48.0
date: 2026-10-09
---
# v1.48.0 — IDEAL: Beschichtungsauftrag summiert als Standard

Beschichtungsauftrag-Ausbau **Teil 1** ([[2026-10-06-beschichtungsauftrag-ausbau-teil-1-spec]], Übersicht
[[2026-10-06-beschichtungsauftrag-ausbau-uebersicht]]). Eigener Worktree
`feature/2026-10-06-beschichtungsauftrag-ausbau-teil-1`, **abgezweigt vom Bündel** `feature/2026-08-07-ideal-teile-1-5`
@ `f102fbf5` (v1.47.5) — die Beschichtungsansicht existiert nur im Bündel. Commits `850258e3`, `578aec6b`, `c226a465`,
`8d9cefd2`. **Keine Migration.** Umsetzung [[2026-10-09-beschichtungsauftrag-ausbau-teil-1-umsetzung]].

**Warum:** Der Beschichtungsauftrag listete jede beschichtete Strukturzeile einzeln; der Beschichter braucht die
Stückzahl je gleichem Teil. Muster der Kommissionierliste v1.47.0, ohne neue Mechanik.

## Kernpunkte

- Summierschlüssel **HauptFA + Artnr** (Antwort 1). Maße nur Anzeige; weichen sie ab: Werte der ersten Position
  (`Position`, dann `SubFA`), sichtbarer Hinweis „Maße uneinheitlich" in der gesperrten Matchcode-Spalte (Bildschirm und
  Druck) und `ILogger`-Warnung mit HauptFA + Artnr. `NULL` = `0` (Sync-Mapping).
- **Summiert ist Standard** (Antwort 2): Navigation auf `Summiert`, Umschalter Liste/Summiert in beiden Sichten
  (geteilte Spaltenfilter, ohne `colf_sollmenge`). Einzelliste, Druck, PDF unverändert.
- Druck/PDF der summierten Sicht nach Kommissionier-Muster (sichtbare Spalten, eingebettetes CSS, Barcode als Data-URI),
  Dateiname `Beschichtungsauftrag_<HauptFA>_<yyyyMMdd-HHmm>.pdf`, Fehler → zurück auf `Summiert`.
- Neuer ViewKey `FaHierarchyBeschichtungSummiert` (9 Spalten inkl. „Positionen") in `ColumnDefinitions` + Switch.
- Gemeinsamer Helfer `PrintVisibleColumns` (vorher privat im Kommissionier-Controller), Kopf-Partials
  `_BeschichtungKopf`/`_BeschichtungKopfDruck` (Index/Print nutzen sie mit).
- Wächter: Zahnrad-Host ([[fallstricke]] §19), Vier-Quellen-Drift-Guard um Beschichtung erweitert. TS-81.

## Deploy-Risiken

Nur Web-Publish (Service-Version mitgezogen, keine Service-Änderung). Merge erst nach Schranke 2 des Bündels, dann diesen
Zweig auf den Bündel-Stand bzw. `main` nachziehen. Vor der Abnahme Prüfschritt Mengeneinheit (TS-81.1, wie TS-80.1).
Zusatzfund: Positionen ohne Artnr werden je HauptFA zusammengefasst (wie bei der Kommissionierliste) — Prüfabfrage in der Spec.
