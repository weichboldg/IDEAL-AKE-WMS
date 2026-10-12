---
type: changelog
version: 1.52.0
date: 2026-10-12
---
# v1.52.0 — IDEAL: Arbeitsgänge als Symbole im FA-Struktur-Baum (Teil 1, reine Anzeige)

FA-Struktur Arbeitsgänge als Symbole **Teil 1** ([[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec]], Übersicht
[[2026-10-06-fa-struktur-arbeitsgaenge-symbole-uebersicht]]). **Direkt im Bündel** `feature/2026-08-07-ideal-teile-1-5`,
Start `5764c664` (v1.51.0), Commits `f1a447cb`, `1c86978d`, `8f15833c`, QA-Fix `d6d6beb0`. **Keine Migration.** Umsetzung
[[2026-10-12-fa-struktur-arbeitsgaenge-symbole-teil-1-umsetzung]].

**Warum:** Die Arbeitsgänge eines Auftrags sollen im Baum auf einen Blick sichtbar sein, wie die Spalte
„Arbeitsschritte" in Sage — Grundlage für Status (Teil 2) und BDE-Auftragsanzeige (Teil 3).

## Kernpunkte

- Quelle S: `FaHierarchyNode.Arbeitsschritte` der Zeile selbst, tokenisiert wie die Struktur-Erkennung (Kopie der Regel,
  `FaArbeitsschrittSymbole`), Feld-Reihenfolge, kein Dedupe, Ausschlussliste nicht gefiltert, auch an Blättern.
- Neue Baumspalte `operations` „Arbeitsgänge" rechts neben Arbeitsbereich, neutrale Umriss-Chips (keine Statusfarbe),
  Name per `aria-label`/`title` aus EINER Katalog-Abfrage (nur bei eindeutiger Werkbank). Modal v1.45 unverändert.
- Neuer Drift-Wächter `FaHierarchyStrukturSpaltenTests` für die sechs Spaltenquellen des Baums.
- TS-85; Tests Web 1636 (+17), Service 272.

## Deploy-Risiken

Nur Web, keine Migration, kein Service-Anteil (nur Versions-Bump). Vor der Abnahme Prüfschritt P-K1 der Spec
(Feldgleichheit Sage ↔ WMS). Bekannt: gedimmte Kontextzeilen beim Spaltenfilter (opacity 0.6) liegen für alle Zellen
unter WCAG AA — vorbestehend, QA M1.
