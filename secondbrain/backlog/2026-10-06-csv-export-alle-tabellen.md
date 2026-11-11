---
typ: feature
---
# CSV-Export für alle Tabellen (querschnittlich)

**Aufgenommen 2026-10-06** (Anforderung Gerald, Koordination).

## Anforderung (Gerald)

Funktion zum CSV-Export in allen Tabellen.

## Ausgangslage (am Code belegt, Bündel-Worktree `feature/2026-08-07-ideal-teile-1-5`)

- Es gibt **keinen** CSV-Export: Suche nach `text/csv`, `.csv`, `ExportCsv`, `toCsv` in `IdealAkeWms`
  (`*.cs`, `*.cshtml`, `*.js`, ohne `lib/`) ohne Treffer.
- Listenmuster nach ADR 0005: Server-Pagination, serverseitige Spaltenfilter
  (`ColumnFilterHelper.ReadFromQuery`), Spaltenauswahl je Benutzer (`wwwroot/js/column-preferences.js`,
  `ColumnDefinitions.GetByViewKey`).
- Konvention aus [[2026-09-25-kommissionierliste-summierung-pdf-spec]]: Druck/PDF zeigt die **beim Klick
  sichtbaren** Spalten (`visibleColumns`).

## Für die Spec zu klären (Empfehlungen der Koordination in Klammern)

1. **Welche Zeilen:** alle Zeilen des aktuellen Filters über alle Seiten (Empfehlung) oder nur die
   sichtbare Seite.
2. **Welche Spalten:** die beim Klick sichtbaren, in Bildschirm-Reihenfolge (Empfehlung, gleiche
   Konvention wie Druck/PDF).
3. **Format für Excel in Österreich:** Semikolon, UTF-8 mit BOM, Dezimalkomma, Datum `dd.MM.yyyy`
   (Empfehlung). Dateiname mit Ansicht und Zeitstempel.
4. **Gruppierte Listen** (HauptFA-Gruppen, summierte Ansichten): flach, Gruppenschlüssel als eigene
   Spalte.
5. **Sicherheit:** CSV-Injection entschärfen (Zellen, die mit `=`, `+`, `-`, `@` beginnen). Export nur
   mit dem Leserecht der jeweiligen Ansicht (bestehende `Require*Access`-Filter, Feature-Gates
   kumulativ).
6. **Ein gemeinsamer Baustein** (Helper + Button-Partial) statt Einzellösungen je Ansicht.
7. **Umfang:** Liste aller betroffenen Tabellenansichten ermitteln (Ausgangspunkt: registrierte
   `viewKey`s in `ColumnDefinitions` plus Listen ohne Spaltenauswahl). Bei großem Umfang in Teil-Specs
   nach Bereich schneiden (Baustein + Pilotliste zuerst, dann der Rest).

## Hinweis zur Reihenfolge

Berührt AKE- und IDEAL-Listen. Umsetzung **nach dem Merge des IDEAL-Bündels** empfohlen, sonst
entstehen Doppelarbeit und Konflikte in den Listen-Views.
