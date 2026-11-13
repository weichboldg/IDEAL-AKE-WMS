---
type: backlog
created: 2026-11-14
quelle: "[[2026-11-14-wochenend-review-test-anwenderdoku]]"
status: neu
modul: Tabellen-Querschnitt (CSV-Export csv-export.js, Pagination der Fehlteil-Listen)
stand: "Bündel v1.99.22, 90bbbbd7, feature/2026-08-07-ideal-teile-1-5"
---
# Befunde Tabellen-Querschnitt (DEV-Regression B4)

Aus der DEV-Regression [[04-uebrige-module]] (Teil B4 von [[2026-11-14-wochenend-review-test-anwenderdoku]]). Nichts behoben (Schranke 1).
Bezug: [[2026-10-06-csv-export-alle-tabellen-spec]] (TS-86). Pfade relativ zum Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5/`.

## N-B4-2 – CSV: Zahlen ab 1.000 werden nicht als Zahl erkannt, negative als Text mit Apostroph
- **Schwere:** mittel (Excel rechnet mit exportierten Mengen ab 1.000 nicht; TS-86.5/86.7 nicht erfüllt)
- **Fundstelle:** `IdealAkeWms/wwwroot/js/csv-export.js:16` (`PLAIN_NUMBER = /^[+-]?(\d{1,3}(\.\d{3})+|\d+)(,\d+)?$/`), `:21-24`
  (`\s+` → Leerzeichen, danach Injection-Schutz `DANGEROUS_START`); `IdealAkeWms/Program.cs` setzt keine `CultureInfo` → die Server-Kultur
  bestimmt den Tausendertrenner (DEV: geschütztes Leerzeichen U+00A0).
- **Befund:** AKE-DEV `/StockOverview`, Artikel 00000905: Bildschirm `-10 026,000`, CSV `'-10 026,000` (Apostroph = Text). Positive Werte ab 1.000
  stehen als `10 026,000` in der CSV – Excel (deutsch) erkennt Leerzeichen nicht als Tausendertrenner. Kleine Werte (`-6,000`, `56,000`) korrekt.
  Betrifft alle Mengen- und Summenspalten. ![[N-B4-2-csv-tausender-negativ-als-text.jpg]]
- **Prüffrage (Mensch):** Welche Kultur haben akenet01 und idealweb01? FA-Liste/Bestand mit einer Menge ab 1.000 ansehen: Punkt (`1.234,500`) oder
  Leerzeichen (`1 234,500`)? Beim Punkt tritt der Befund dort nicht auf, auf DEV und jedem Server mit `de-AT`-ICU schon.
- **Vorschlag:** In `cleanValue` Leerzeichen/U+00A0 zwischen Zifferngruppen entfernen, bevor `PLAIN_NUMBER` geprüft wird (`-10026,000`), oder
  `PLAIN_NUMBER` um Leerzeichen-Gruppen erweitern und sie beim Export entfernen. Fall in `column-filter-cases`/`Kern_NodeSelbstpruefung`
  ergänzen: `-10 026,000` → `-10026,000` ohne Apostroph.

## N-B4-3 – Fehlteile: Seite außerhalb des Bereichs zeigt „Eintraege 26–15 von 15“
- **Schwere:** Hinweis
- **Fundstelle:** `/MissingParts`, `/MissingPartsLager` (Pagination-Zähler; andere Listen zeigen in dem Fall „Keine … gefunden.“)
- **Befund:** AKE-DEV `/MissingParts?mineOnly=true&page=2` bei 15 Treffern: „Eintraege 26–15 von 15“ und leere Tabelle;
  `/MissingPartsLager?page=99`: „Eintraege 2451–21 von 21“. Nur über veraltete oder handgebaute Links erreichbar.
- **Vorschlag:** Seite auf die letzte vorhandene begrenzen (wie `PaginationState` es für die übrigen Listen vorsieht) oder Zähler „0 von 15“ mit
  Link auf Seite 1.
