---
type: backlog
typ: feature
status: offen
created: 2026-11-28
standort: beide
quelle: "Mail „WMS Anpassungen“ (Benjamin Andl-Braun, IDEAL) Punkt 2 + Ergänzung Gerald im Chat 2026-11-28 („Sage-Info-Felder wie z. B. OS_-Felder sollen filterbar sein“)"
anhaenge:
  - anhaenge/2026-11-28-mail-wms-anpassungen-2.png
---
# Stammdaten Artikel: Merkmal-Spalten (Sage-Infofelder, OS_-Felder) filterbar

## Wunsch
„Bei den Stammdaten-Artikel soll die Leiste filterbar sein“; Gerald ergänzt: die Sage-Infofelder (z. B. `USER_OS…`) sollen filterbar sein.

## Befund (2026-11-28, nur gelesen, Worktree v1.99.89)
- `Views/Articles/Index.cshtml:77-78`: Merkmal-Spalten `attr-{DefinitionId}` (u. a. Sage-Merkmale wie `USER_OSIstLagerartikel`,
  `USER_OSArtikeltyp`) sind im Zahnrad wählbar, haben aber **bewusst kein Filterfeld**: „ApplyArticleColumnFilter mappt attr-Spalten nicht –
  Filter-Input wäre tot“. Die übrigen Spalten filtern serverseitig (ADR 0005).
- Merkmale sind typisiert (`AttributeType`: Text, Boolean, Auswahl …, `Edit.cshtml:102-130`) – Filter je Typ (ja/nein, Auswahlwert, Text).

## Rückfragen (Vorschlag)
- Alle Merkmale filterbar oder nur Sage-Merkmale?
- „Leiste“ = die Spaltenfilter-Zeile der Artikelliste (Annahme) – oder eine andere Leiste (z. B. Filterkarte oben)?
