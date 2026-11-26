---
type: changelog
version: 1.99.83
date: 2026-11-26
---
# v1.99.83 — IDEAL: FA-Struktur je HauptFA eine Zeile (Variante D)

Spec [[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-2-spec]], Lauf-Notiz
[[2026-11-26-fa-liste-hauptfa-fa-struktur-goal-lauf]]. Worktree `feature/2026-08-07-ideal-teile-1-5`, Commits `ce04f887`
(Umsetzung) + `1ae5f420` (QA-Fix F1–F3) + `9cde9c6f` (Doku). **Keine Migration, keine neue Einstellung.** TS-204. Nicht gemergt.

## Kernpunkte

- Struktur-Kopfzeile entfällt: die HauptFA-Zeile trägt Badges, Zähler „n Knoten · m Teile ausgeblendet“ und 15 Kopfspalten
  (Kunde … Bemerkung/Uhrzeit, Keys wie FA-Liste, alle sichtbar). Kombigerät/mehrere Köpfe: Kopfzellen leer, Kopfdaten-Zeile darunter
  (`w-auto` in `.fa-head-wrap`, sticky links — QA F1).
- Teile (SubFA = 0 ohne Arbeitsschritte) standardmäßig ausgeblendet: Regel an EINER Stelle `FaHierarchyTreeBuilder.TeileAusblenden`
  (nach Paging, vor AG-Status); URL `mitTeilen=true` über Knopf „Teile anzeigen“.
- Legende als Popover, Hilfe-Link `#hilfe-fa-struktur`, Klapp-Pfeil 28 px mit `aria-expanded`, Struktur + Matchcode angeheftet.
- `.fa-structure-header` entfernt (toter Stil).

## Deploy-Risiken

- Kein Schema. Alte Zahnrad-Präferenzen (9 Spalten) bleiben gültig, die 15 neuen Spalten erscheinen sichtbar.
- Echtes Blättern mit `mitTeilen` und Kontrast nur Manual-UAT (TS-204).
