---
type: bug
title: "Stückliste: Zelle Lagerplatz listet alle Bestandsplätze in einer Zeile — Tabelle wird sehr breit, Scrollbar erst am Tabellenende"
status: behoben
fixed_in: "v1.62.0"
severity: mittel
created: 2026-10-16
affected_code:
  - "IdealAkeWms/Views/Picking/Bom.cshtml:267-276 (Zelle Lagerplatz, string.Join aller StockLocations)"
  - "IdealAkeWms/wwwroot/css/site.css:250-253 (white-space: nowrap !important fuer alle Zellen in .table-responsive)"
  - "IdealAkeWms/Data/Repositories/StockMovementRepository.cs:385-473 (GetStockByArticleNumbersAsync, Reihenfolge Hauptlagerplatz, dann Code)"
source_backlog: "[[2026-10-06-stueckliste-zahnrad-bug-matchcode]]"
spec: "[[2026-10-16-stueckliste-matchcode-lagerplatz-limit-spec]]"
---

## Symptom

Gerald, 2026-10-16, Screenshot IDEAL-Test, `Picking/Bom/131` (FA 1059463, 263 Positionen): Hat eine Komponente
viele Bestandsplätze, wird die Spalte „Lagerplatz“ sehr breit, und damit die ganze Tabelle. Die horizontale
Scrollbar sitzt am Tabellenende, also unter 263 Zeilen. Wer nach rechts will, muss erst ganz nach unten scrollen.
Beide Standorte sind betroffen: die Zelle ist dieselbe View, AKE und IDEAL.

Der zweite Fehler aus der Quell-Notiz („Zahnrad unsichtbar“) ist **behoben** (v1.47.3), siehe unten.

## Reproduktion

1. IDEAL-Test, Stückliste eines FA öffnen, in dem mindestens eine Komponente auf vielen Lagerplätzen Bestand hat
   (z. B. FA 1059463, `Picking/Bom/131`).
2. Spalte „Lagerplatz“ ansehen: alle Plätze stehen als `Code (Menge), Code (Menge), …` in **einer** Zeile.
3. Die Tabelle ist breiter als der Bildschirm. Die Scrollbar erscheint erst nach dem Scrollen ans Tabellenende.

## Root Cause

- `Bom.cshtml:270` rendert `string.Join(", ", item.StockLocations.Select(sl => $"{sl.Code} ({sl.Quantity:N3})"))`
  ohne Obergrenze.
- `site.css:250-253` verbietet für **alle** Zellen in `.table-responsive` den Umbruch (`white-space: nowrap !important`),
  `site.css:244-247` setzt die Tabelle auf `width: max-content`. Eine lange Zelle macht so die ganze Tabelle breit.
- Das ist kein Regress, sondern ein Grenzfall, der seit Anbeginn besteht und an IDEAL mit großen Strukturen sichtbar
  wird.

## Fix / Verweis

Entscheidung Gerald 2026-10-16: höchstens N Plätze in der Zelle, der Rest per „+n weitere“ mit Popover.
Ausgestaltung in [[2026-10-16-stueckliste-matchcode-lagerplatz-limit-spec]].

Zahnrad-Teil der Quell-Notiz: **behoben in v1.47.3**, im Bündel-Worktree belegt: `Views/Picking/Bom.cshtml:129-133`
(Badges/Leerzustand über dem Host), `:142` leerer Host, `:143` `.table-responsive`; Wächter
`IdealAkeWms.Tests/Views/BomZahnradHostTests.cs:31-36`; [[2026-10-06-v1-47-3-stueckliste-zahnrad-sichtbar]].
