---
type: spec
title: Alle Tabellen – Streifen, Hover und Fokus-Zeile in CI-Blau
slug: 2026-11-23-tabellen-streifen-hover-ci-hellblau-spec
status: Entwurf
created: 2026-11-23
updated: 2026-11-23
source_backlog: "[[2026-11-23-tabellen-streifen-hover-ci-hellblau]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: beide
affected_code:
  - IdealAkeWms/wwwroot/css/site.css (Regel .table tbody tr:hover Z. 198-200 ersetzen; .we-positionen Z. 1505-1530 in globale Regeln überführen; neue globale Regeln + @media print)
  - IdealAkeWms/Views/FaHierarchy/Index.cshtml:112 (#faTree, Ausnahmeklasse)
  - IdealAkeWms/Views/Tracking/Index.cshtml:72 (#trackingTree, Ausnahmeklasse)
  - IdealAkeWms/Views/Tracking/OseonIndex.cshtml:103 (#oseonTree, Ausnahmeklasse)
  - IdealAkeWms/Views/Articles/Info.cshtml:151 (Schlüssel-Wert-Tabelle, Ausnahmeklasse – nur falls Rückfrage 2 so entschieden)
  - IdealAkeWms/Views/PurchaseOrders/Receive.cshtml:118 (Klasse we-positionen entfällt oder bleibt ohne CSS)
  - IdealAkeWms.Tests/Views/ViewKeyDriftGuardTests.cs:70-93 (CSS-Teil des WE-Wächters anpassen)
  - IdealAkeWms.Tests/Views/TabellenZeilenfarbeTests.cs (neu, zentraler Wächter)
  - IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs (1.99.62)
  - IdealAkeWms/Views/Help/Changelog.cshtml
  - docs/TESTSZENARIEN.md (TS-187 neu, TS-184.4 als überholt markieren)
open_questions:
  - "1. Variante A (global auf .table, Ausnahmeklasse table-plain) oder B (Opt-in-Klasse je View)? Empfehlung A."
  - "2. Ausnahmen genau: die drei Baumtabellen (#faTree, #trackingTree, #oseonTree) plus die randlose Schlüssel-Wert-Tabelle in der Artikelinfo (Articles/Info.cshtml:151)? Weitere?"
  - "3. Streifen auch in kleinen Detail-, Modal- und Hilfetabellen (z. B. Arbeitsgänge-Modal, BDE-Gruppenmeldung, Positionsauswahl, Hilfe)? Empfehlung ja, einheitlich."
  - "4. Streifenfarbe: Bootstrap-Grau wie im Wareneingang (#F2F2F2) behalten oder CI-Grau --ake-light-gray (#F5F5F5)? Empfehlung Bootstrap-Grau (kein CI-Blau, sonst Verwechslung mit Fokus-Zeile)."
  - "5. Hover nur bei Geräten mit Maus (@media (hover: hover)), damit auf reinen Touch-Scannern nach dem Antippen keine Zeile hellblau „kleben“ bleibt? Empfehlung ja."
  - "6. Farbige Textklassen in Tabellenzeilen (text-danger/-success/-primary/-info/-warning) auf die dunklen -emphasis-Töne umstellen? Sie liegen schon heute auf Streifen bei ca. 4,0:1 und würden auf Hellblau ca. 3,0:1 erreichen (unter AA). Empfehlung ja, nur in tbody von .table."
  - "7. Fokus-Zeile: Auslöser nur Wertfelder (Text, Zahl, Auswahl, Memo) per Maus und Tastatur, Kästchen/Optionsfelder nur per Tastatur, Links und Knöpfe nie. Im Wareneingang markiert damit ein fokussierter Link die Zeile nicht mehr (bisher :focus-within). Einverstanden?"
  - "8. (IT/Fachbereich) Laufen alle Terminals und Scanner auf Edge/Chrome ab 105 bzw. Firefox ab 121? Die Fokus-Zeile braucht :has(); ohne Unterstützung fällt nur die Fokus-Zeile weg, Streifen/Hover/Links bleiben."
  - "9. Vault-/enaio-Symbol-Links in Tabellen (#43A6E2/#EF7D00 mit opacity 0,7, schon auf Weiß unter 3:1) hier mitnehmen oder als eigene Backlog-Notiz? Empfehlung eigene Notiz."
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: "A"
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Im Wareneingang zu Bestellung sind seit v1.99.52/v1.99.59 Zeilen gestreift, die Zeile unter der Maus CI-hellblau und die Zeile, in der man
tippt, sehr hellblau mit dunkelblauem Balken. Gerald will das in **allen** Tabellen: Bootstrap-Grau als Hover war am Terminal kaum zu sehen
(Rückmeldung zu TS-177, [[2026-11-23-v1-99-59-wareneingang-hover-hellblau]]). Einheitliche Zeilenführung senkt Verrutschen in der Zeile
beim Ablesen und Eintippen an Fertigungs- und Lagerterminals.

Nebenbefund, der die Sache stützt: Die App hat **schon** eine globale Hover-Regel `.table tbody tr:hover { background-color: var(--ake-very-light-blue); }`
(`site.css:198-200`), und der Kommentar `site.css:730-733` nennt „Hover … mit dem AKE-Hellblau (gleich wie FA-Liste / Bestand / alle anderen
Listen)“ als Absicht. Die Regel ist aber wirkungslos: Bootstrap 5.3 malt jede Zelle deckend (`background-color: var(--bs-table-bg)`,
`bootstrap.css:1881`, `--bs-table-bg: var(--bs-body-bg)` = Weiß, `bootstrap.css:1864`), der Zeilenhintergrund am `<tr>` liegt darunter. Das deckt
sich mit TS-184.4 („andere Tabelle mit Hover … unverändert (grau)“) – grau ist dort nur, wer `table-hover` trägt. Die Absicht existiert also,
die Umsetzung fehlt. (Sichtprüfung im Dev-Lauf bestätigt das, bevor die Regel entfernt wird.)

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Globale Zeilenfarben für jede `<table class="table …">` in Bildschirmansichten: Streifen, Hover CI-Hellblau, Fokus-Zeile mit Balken (nur bei Wertfeldern),
  Links dunkelblau, keine Farbflächen im Druck.
- Eine Ausnahmeklasse für Tabellen mit eigener Zeilenfärbung (Bäume).
- Überführen der `.we-positionen`-Regeln (`site.css:1505-1530`) in die globalen Regeln; Entfernen der wirkungslosen Regel `site.css:198-200`.
- Ein zentraler Wächter-Test; WE-Wächter angepasst.
- Je nach Rückfrage 6: farbige Textklassen in Tabellenzeilen auf `-emphasis`.

**Out-of-Scope**
- Druckansichten mit `Layout = null` (Inventur unten): laden weder Bootstrap noch `site.css` – unberührt.
- Kopfzeile (`thead`, dunkelblau `site.css:189-196`), Spaltenfilter-Zeile, Fußzeilen (`tfoot`) – Regeln greifen nur in `tbody`.
- Neue Zeilenzustände (Auswahl/Markierung) – gibt es heute nicht (siehe Inventur).
- Vault-/enaio-Symbolkontrast (Rückfrage 9), OSEON-Ebenenfarben (`site.css:734-749`, vermutlich ebenfalls wirkungslos, siehe Zusatzfunde).
- Bootstrap-Upgrade, neue Bibliothek, Schalter.

## Fachliche Anforderungen

1. Jede Bildschirm-Tabelle mit Klasse `table` zeigt abwechselnd hinterlegte Zeilen (Bootstrap-Streifen), auch ohne `table-striped` im Markup.
2. Die Zeile unter der Maus ist CI-Hellblau `--ake-light-blue` (#A1D9F7), auch ohne `table-hover` im Markup.
3. Zeilen mit eigener Bedeutungsfarbe (`table-danger`, `table-warning`, `table-success`, `table-secondary`, `table-info`, `table-primary`, `table-light`, `table-dark`)
   behalten ihren Ton in Ruhe, beim Überfahren und mit Fokus (Fallstrick 61). Gruppen-Kopfzeilen mit `table-active` behalten ihren Grauton in Ruhe.
4. Fokus-Zeile: Steht der Eingabefokus in einem Wertfeld einer Zeile, ist die Zeile `--ake-very-light-blue` (#D9F4FF) mit 4-px-Balken links in
   `--bs-primary-text-emphasis` (#052C65). Kästchen/Optionsfelder lösen das nur bei Tastaturfokus aus (Rückfrage 7). Fokus auf Link/Knopf: keine Fokus-Zeile.
   Fokus-Zeile hat Vorrang vor Hover.
5. Links in Tabellenzeilen sind dunkelblau (#052C65), auch beim Überfahren; Knöpfe (`.btn`), Badge-Links (`.badge`) und Vault/enaio-Symbole (`.vault-link`, `.dms-link`) behalten ihre Farbe.
6. Druck/PDF aus normalen Bildschirmseiten (Strg+P): keine Streifen, kein Hover, keine Fokus-Zeile – auch mit eingeschalteten Hintergrundgrafiken.
7. Kontrast WCAG AA in allen Zuständen (Tabelle unten); Farbe nie alleiniger Bedeutungsträger: Fokus-Zeile hat den Balken plus Fokusring des Felds, Bedeutungszeilen
   behalten ihren Text/Badge.
8. Tabellen mit Ausnahmeklasse (Bäume) bleiben exakt wie heute.

## Technischer Loesungsentwurf

### Inventur `<table` in `IdealAkeWms/Views/**` (Stand Worktree v1.99.59)

112 Treffer `<table` (Grep über `Views/**/*.cshtml`). Dazu 2 per JS erzeugte `.table` (`wwwroot/js/bde-terminal.js:510` mit `table-striped`,
`wwwroot/js/column-filter-syntax.js:188` Hilfe-Popover) und 1 Nicht-Bootstrap-Raster (`table-filter.js:493` `.date-filter-grid`, nicht betroffen).

| Gruppe | Anzahl | Belege | Wirkung der globalen Regel |
|---|---|---|---|
| Druck-Tabellen ohne `.table` | 13 | `Beschichtungsauftraege/Print.cshtml:54,71`, `FaHierarchyBeschichtung/Print.cshtml:75`, `PrintSummiert.cshtml:80`, `_BeschichtungKopfDruck.cshtml:16,38`, `FaHierarchyKommissionierListen/Print.cshtml:79,105`, `PrintSummiert.cshtml:71`, `Picking/PrintBom.cshtml:215`, `Picking/PrintPicking.cshtml:174`, `Shared/_PrintBriefkopf.cshtml:27`, `WarehousePicking/Print.cshtml:44` | keine – alle mit `Layout = null` (Grep `Layout\s*=`: nur Print-Views, `Login`, `_OseonGroupDetails`), kein `site.css`, keine Klasse `table` |
| `.table` gesamt | 99 | 98 Markup + 1 JS-String `Tracking/OseonIndex.cshtml:358` | betroffen |
| davon schon `table-striped` | 73 (in 67 Dateien) | Grep `<table[^>]*table-striped` | nur Hover-Farbe ändert sich |
| davon `table-hover` | 8 | `Articles/Info.cshtml:189,236`, `BdeBookings/Index.cshtml:83`, `Beschichtungsauftraege/Index.cshtml:44`, `BfAuftraege/Details.cshtml:88`, `BfAuftraege/Index.cshtml:66`, `Picking/Index.cshtml:65`, `PurchaseOrders/Receive.cshtml:118` | Hover grau → hellblau |
| `.table` ohne Streifen (neu gestreift) | 26 | `Articles/Info.cshtml:151,189,236`; `BdeBookings/Edit.cshtml:73`; `BdeTerminal/Index.cshtml:158` (Modal); `Beschichtungsauftraege/Details.cshtml:373`; `BfAuftraege/Details.cshtml:88`; `FaHierarchy/Index.cshtml:112,222`; `FaHierarchy/_FaWorkOperationsModal.cshtml:20`; `Help/Index.cshtml:367,2640`; `OseonReporting/_OseonReportingTable.cshtml:4`; `ProductionOrders/Index.cshtml:181` (Varianten, geschachtelt); `ProductionWorkplaces/Edit.cshtml:165`; `SageSdataFa/Index.cshtml:105`; `Settings/Index.cshtml:91`; `Shared/_BeschichtungFreiePositionen.cshtml:5`; `StockMovements/InboundBulk.cshtml:58`; `Tracking/Index.cshtml:72,242`; `Tracking/OseonIndex.cshtml:103`; `WarehousePicking/Details.cshtml:67`; `WarehousePicking/Index.cshtml:60`; `WarehouseRequisitions/Edit.cshtml:35`; `WarehouseRequisitions/Index.cshtml:78` | Streifen + Hover neu |
| Baumtabellen mit eigener Zeilenfärbung | 3 | `#faTree` `FaHierarchy/Index.cshtml:112` (Zellfarben `site.css:1403-1407`: Hover Grau, `.fa-hl` Hellblau, `.fa-filter-hit` Amber, per `background-color` am `td`); `#trackingTree` `Tracking/Index.cshtml:72`; `#oseonTree` `Tracking/OseonIndex.cshtml:103` (Ebenenfarben am `tr`, `site.css:734-749`; Zeilen per `display:none` auf-/zugeklappt `Tracking/Index.cshtml:152`, `_OseonGroupDetails.cshtml:73`) | **Ausnahme nötig**: Bootstraps Zellschatten liegt *über* `background-color`; ein Streifen/Hover-Schatten verdeckt `.fa-hl`/`.fa-filter-hit` (die Treffer-Markierung wäre in jeder zweiten Zeile weg) und zugeklappte Zeilen zerreißen das Streifenmuster |
| Zeilen mit Kontextklasse | – | `table-danger`: `ProductionOrders/_ProductionOrderRow.cshtml:20`, `PurchaseOrders/Index.cshtml:142`, `PurchaseOrders/Receive.cshtml:141`, `StockMovements/InboundBulk.cshtml:70`; `table-secondary` (erledigt): `FaCompletion/_FaCompletionRow.cshtml:6`, `PickingLeitstand/_PickingLeitstandRow.cshtml:14`, `Picking/Bom.cshtml:230`, `_ProductionOrderRow.cshtml:20`; `table-success`: `Bom.cshtml:230`, `Tracking/ByWorkplace.cshtml:70`, `Tracking/Index.cshtml:152,257`; `table-warning` per JS (Blinken/Treffer): `Bom.cshtml:848`, `InboundBulk.cshtml:253`, `OutboundAll.cshtml:212`, statisch `Users/RoleOverview.cshtml:35`; `table-success` per JS `Bom.cshtml:1572`; `table-light`: `ArticleAttributes/Index.cshtml:127`, `FaAttributes/Index.cshtml:102`, `FaCompletion/Edit.cshtml:331`, `Tracking/Index.cshtml:92,216`; `table-dark` nur in `tfoot` (`LocationTransfer.cshtml:72`, `OutboundAll.cshtml:99`) | behalten ihren Ton: Kontextklassen setzen `--bs-table-hover-bg`/`-active-bg`/`-striped-bg` selbst am `tr` (`bootstrap.css:1938-2050`), das schlägt die geerbte Tabellen-Variable |
| Gruppen-Kopfzeilen `tr.table-active` | 5 | `FaHierarchyBeschichtung/Index.cshtml:92`, `Summiert.cshtml:92`, `FaHierarchyKommissionierListen/Index.cshtml:104`, `Summiert.cshtml:103`, `FaHierarchyVormontage/Index.cshtml:107` | Grund: `--bs-table-active-bg`. **Darf nicht global umgestellt werden** – das WE-Muster (`site.css:1524` setzt `--bs-table-active-bg` auf Hellblau) würde diese Kopfzeilen hellblau färben. Die globale Fokus-Zeile bekommt deshalb eine eigene Variable |
| Kontextklasse am `<table>` selbst | 0 | Grep über alle `<table`-Zeilen | Wächter sichert das ab: säße dort `table-danger`, würde die spätere `.table`-Variable aus `site.css` (gleiche Spezifität 0,1,0) Bootstraps Wert überschreiben |
| Inline-Styles an Zeilen | 6 | `Picking/Index.cshtml:88` (`cursor:pointer`, `.clickable-row`), `Tracking/Index.cshtml:92,121,152`, `Tracking/OseonIndex.cshtml:138`, `_OseonGroupDetails.cshtml:73` (nur `cursor`/`display`) | keine Farbe; Baumzeilen über Ausnahme |
| Zellen mit eigenem Grund | – | `.fa-liste-group-head > td.fa-liste-group-header` (`site.css:1304-1309`, Hellgrau), `.fa-structure-subhead > td` (`site.css:1351-1354`, Weiß, nur im Baum), `BdeTeileliste/Index.cshtml:145` `td.bg-body-tertiary` (Detailzeile) | Streifen (5 % Schwarz) legen sich halbtransparent darüber – wie heute in diesen schon gestreiften Listen; Hover deckt sie während des Überfahrens hellblau. Gruppenkopf bleibt durch Fettschrift + Chevron erkennbar |
| Wertfelder in Zeilen (Fokus-Zeile) | 15 Dateien | Grep `<td…>\s*(<div|span|label…>)?<input|select|textarea` (multiline): u. a. `PurchaseOrders/Receive.cshtml:185,219,228,237`, `StockMovements/InboundBulk.cshtml`, `FaCompletion/Edit.cshtml`, `BdeBookings/Edit.cshtml`, `OrderRecipients/Edit.cshtml`, `OperationConfigs/Index.cshtml`, `BfAuftraege/Details.cshtml`, `Beschichtungsauftraege/Details.cshtml`, `Picking/Bom.cshtml` (Menge IST), `PickingLeitstand/_PickingLeitstandRow.cshtml` (Sammel-Kästchen); JS-Zeilen: `WarehouseRequisitions/Edit.cshtml:35`, `BdeTerminal/Index.cshtml:168` | globale Fokus-Regel ohne Markup-Änderung |
| Markierte/ausgewählte Zeilen | 0 | kein `selected`/`table-active`-Umschalten in JS (Grep `classList.(add|toggle)` in Views und `wwwroot/js`); Sammelauswahl Leitstand nur per Kästchen (`site.css:1266-1275`) | kein Konflikt; deshalb Kästchen-Fokus nur per Tastatur (Rückfrage 7) |
| Sticky Header | 0 | Grep `sticky-top|position: sticky` in Views: keine; `site.css`: nur `.bulk-action-bar` (1251) und `.table-sticky-scrollbar` (1033) | kein Konflikt |
| Spaltenfilter-Zeile | – | `table-filter.js:157-158,236` hängt `tr.filter-row` in den `thead` | Regeln nur in `tbody` – unberührt |
| DataTables o. ä. | 0 | Grep `DataTable` in js/cshtml/css: keine; eigene Skripte `table-filter.js`, `column-preferences.js` (blendet Zellen per `style.display` aus, `column-preferences.js:163-178`) | siehe Fallen |

### Variantenvergleich

**Variante A – global auf `.table`, Ausnahmeklasse `table-plain` (Empfehlung)**
Einige Regeln in `site.css`, Markup bleibt; nur die Ausnahmetabellen bekommen `table-plain` (3, ggf. 4). Neue Views bekommen das Verhalten automatisch.
Die Absicht „Hover einheitlich hellblau in allen Listen“ steht schon im Code (`site.css:198`, `:730-733`), A setzt sie nur wirksam um.
Risiko: unbekannte Sonderfälle bekommen Streifen – abgefangen durch Inventur, Sichtprüfung TS-187 und Wächter, der die Ausnahmeliste festnagelt.

**Variante B – Opt-in-Klasse je View**
~95 Markup-Änderungen, jede neue View muss daran denken; der Wächter müsste ohnehin je `<table class="table` die Klasse oder eine Ausnahme verlangen.
Mehr Diff, gleiches Ergebnis, mehr Drift. Nur sinnvoll, wenn Gerald viele Tabellen bewusst ohne Streifen will (Rückfrage 2/3).

Empfehlung **A**. `freigabe_entscheidung` bleibt leer (Schranke 1).

### CSS-Skizze (Variante A, ersetzt `site.css:198-200` und `:1505-1530`)

Muster nach Fallstrick 61: nur Bootstraps Zustandsvariablen setzen, nie `background-color` am `td`; Balken als zweiter Schatten zusammen mit dem Bootstrap-Schatten.

```css
.table {
    --bs-table-hover-bg: var(--ake-light-blue);          /* #A1D9F7 */
    --bs-table-hover-color: var(--bs-body-color);        /* wie WE v1.99.59 */
    --wms-table-focus-bg: var(--ake-very-light-blue);    /* #D9F4FF – eigene Variable, --bs-table-active-bg bleibt (tr.table-active) */
}
/* Kontextzeilen behalten im Fokus ihren eigenen Aktiv-Ton (table-danger #dfc2c4) */
:is(.table-primary, .table-secondary, .table-success, .table-info, .table-warning, .table-danger, .table-light, .table-dark) {
    --wms-table-focus-bg: var(--bs-table-active-bg);
}
.table:not(.table-plain) > tbody > tr:nth-of-type(odd) > * {
    --bs-table-color-type: var(--bs-table-striped-color);
    --bs-table-bg-type: var(--bs-table-striped-bg);
}
@media (hover: hover) {                                  /* Rückfrage 5 */
    .table:not(.table-plain) > tbody > tr:hover > * {
        --bs-table-color-state: var(--bs-table-hover-color);
        --bs-table-bg-state: var(--bs-table-hover-bg);
    }
}
/* Fokus-Zeile nach Hover (gewinnt bei Gleichstand). Wertfelder immer, Kästchen nur Tastatur (Rückfrage 7) */
.table:not(.table-plain) > tbody > tr:has(:is(input:not([type=hidden], [type=checkbox], [type=radio], [type=button], [type=submit]), select, textarea):focus,
                                         :is(input[type=checkbox], input[type=radio]):focus-visible) > * {
    --bs-table-bg-state: var(--wms-table-focus-bg);
}
.table:not(.table-plain) > tbody > tr:has(/* gleicher Auslöser */) > :first-child {
    box-shadow: inset 4px 0 0 var(--bs-primary-text-emphasis),
                inset 0 0 0 9999px var(--bs-table-bg-state, var(--bs-table-bg-type, var(--bs-table-accent-bg)));
}
.table > tbody a:not(.btn, .badge, .vault-link, .dms-link) { color: var(--bs-primary-text-emphasis); }
/* Rückfrage 6, falls ja: */
.table > tbody .text-danger  { color: var(--bs-danger-text-emphasis)  !important; }   /* analog success/primary/info/warning */
@media print {
    .table > tbody > tr > * { --bs-table-bg-type: initial; --bs-table-bg-state: initial; box-shadow: none; }
}
```

Hinweise zur Skizze:
- `table-striped`/`table-hover` im Markup bleiben stehen (kein 73-Dateien-Diff); sie sind danach redundant, aber harmlos.
- Bootstrap 5.3.3 (`bootstrap.css:3`). Streifen-Grau `rgba(0,0,0,.05)` → #F2F2F2 (`bootstrap.css:1868`), Hover-Grau bisher #EDEDED (`:1872`).
- Spezifität: Streifen (0,3,2) > Bootstrap-Streifen (0,2,2); Hover (0,3,2); Fokus durch `:has(:is(…):focus)` höher und später → Fokus gewinnt über Hover wie heute im WE.
- Kontextklassen am `tr` definieren `--bs-table-hover-bg`/`-striped-bg`/`-active-bg` selbst (`bootstrap.css:2008-2020` für danger) → Hover/Streifen/Fokus bleiben dort im Kontextton.
- Geschachtelte Tabellen (`ProductionOrders/Index.cshtml:181`, `Tracking/Index.cshtml:242`): Bootstrap setzt `--bs-table-bg-state: initial` an jeder `.table` (`bootstrap.css:1862`), der äußere Hover vererbt sich nicht nach innen. `:has()` der äußeren Zeile trifft aber auch Felder in der inneren Tabelle – heute haben die geschachtelten Tabellen keine Wertfelder.
- Der Druck-Block ist Absicherung: die eigenen Print-Views (`Layout = null`) sind ohnehin unberührt.
- Variablenname `--wms-table-focus-bg` englisch (Sprachregel); Ausnahmeklasse `table-plain`.
- `.we-positionen`: CSS-Regeln entfallen (global abgedeckt). Die Klasse im Markup `Receive.cshtml:118` darf bleiben oder entfallen; der WE-Wächter wird angepasst.

### Kontrast (WCAG AA, berechnet nach WCAG-2-Relativluminanz)

| Vordergrund | Hintergrund | Verhältnis | Ziel | OK |
|---|---|---|---|---|
| Zelltext #000 (`--bs-table-color` = emphasis) | Streifen #F2F2F2 | 18,8:1 | 4,5 | ja |
| Hover-Text #212529 (`--bs-body-color`) | Hover #A1D9F7 | 10,1:1 | 4,5 | ja |
| Text #212529 | Fokus #D9F4FF | 13,5:1 | 4,5 | ja |
| `text-muted` (rgba(33,37,41,.75), Bootstrap 5.3 `--bs-secondary-color`) | Streifen #F2F2F2 / Hover #A1D9F7 / Fokus #D9F4FF | 6,4 / 5,3 / 6,3:1 | 4,5 | ja |
| Link #052C65 (`--bs-primary-text-emphasis`) | Weiß / #F2F2F2 / #A1D9F7 / #D9F4FF / danger-Hover #E5C7CA | 13,5 / 12,1 / 8,9 / 11,8 / 8,6:1 | 4,5 | ja |
| Link Bootstrap-Blau #0D6EFD (heute) | Weiß / #F2F2F2 / #A1D9F7 / #D9F4FF | 4,5 / **4,0** / **2,95** / **3,9**:1 | 4,5 | nur Weiß – Streifen fallen schon heute durch |
| Link-Hover Bootstrap #0A58CA (heute) | #A1D9F7 | **4,2**:1 | 4,5 | nein → Regel gilt auch im Hover |
| Balken #052C65 | Fokus #D9F4FF / danger-aktiv #DFC2C4 | 11,8 / ca. 8:1 (`site.css:1508`) | 3 | ja |
| `text-danger` #DC3545 | Weiß / #F2F2F2 / #A1D9F7 | 4,5 / **4,0** / **3,0**:1 | 4,5 | Rückfrage 6 |
| `text-success` #198754 | Weiß / #F2F2F2 / #A1D9F7 | 4,5 / **4,1** / **3,0**:1 | 4,5 | Rückfrage 6 |
| `--bs-danger-text-emphasis` #58151C / `--bs-success-text-emphasis` #0A3622 | #A1D9F7 | 9,0 / 8,8:1 | 4,5 | ja |
| Badges (`bg-*`, `text-bg-*`) | eigener Grund | unverändert | – | nicht betroffen |
| Hover #A1D9F7 vs. Fokus #D9F4FF | – | 1,33:1 | – | Unterscheidung trägt der Balken (11,8:1), nicht die Farbe |

### Wächter (ein zentraler Test statt je View)

Neue Klasse `IdealAkeWms.Tests/Views/TabellenZeilenfarbeTests.cs` (Muster `ViewKeyDriftGuardTests`/`ResponsiveCssTests`: Dateien lesen, Regex):
1. `site.css` enthält genau die globalen Regeln (Streifen, Hover in `@media (hover: hover)`, Fokus mit `--wms-table-focus-bg` und Balken `inset 4px 0 0` + Bootstrap-Fallback-Kette, Links mit `:not(.btn, .badge, .vault-link, .dms-link)`, `@media print`-Rücksetzung) und **keine** dieser Regeln setzt `background` (Fallstrick 61).
2. `--bs-table-active-bg` wird in `site.css` nirgends umgesetzt (schützt die `tr.table-active`-Gruppenköpfe).
3. Die wirkungslose Regel `.table tbody tr:hover` und die `.we-positionen`-Regeln sind weg.
4. Über alle `Views/**/*.cshtml` und `wwwroot/js/*.js`: kein `<table` trägt selbst eine Kontextklasse `table-(primary|secondary|success|info|warning|danger|light|dark)`.
5. Die Menge der Tabellen mit `table-plain` ist exakt die freigegebene Ausnahmeliste (neue Ausnahme = bewusste Teständerung).
`ViewKeyDriftGuardTests.WareneingangPositionen_DreiQuellenGleich_Gesperrt_FokusZeile` (`:70-93`): CSS-Teil (`:84-92`, „genau vier `.we-positionen`-Regeln“) entfällt
bzw. verweist auf den neuen Wächter; Markup-Assert `:82` an die Klasse anpassen.

### Fallen

- **`frontend-design`-Skill ist bei der Umsetzung Pflicht** (CLAUDE.md, Frontend-Arbeit). Leitplanke **Konsistenz vor Eigenstil**: Bootstrap-Variablen, `--ake-*`-Palette, kein neuer Farbwert.
- **Wirkungslose Regel ist grün** (Fallstrick „Spezifität … Vererbung“, `fallstricke.md:283-295`): Build und Tests sehen kein Pixel. Sichtprüfung je Zustand ist Pflicht (TS-187).
- **Nie `background-color` am `td`** – überdeckt `table-danger` (Fallstrick 61). Nur Variablen.
- **`--bs-table-active-bg` nicht global ändern** – färbt die 5 Gruppen-Kopfzeilen `tr.table-active` (Inventur).
- **Bäume:** `.fa-node-row` färbt per `background-color` am `td` (`site.css:1403-1407`); ein Bootstrap-Schatten darüber verdeckt `.fa-hl`/`.fa-filter-hit`. Deshalb `table-plain`.
- **Ausgeblendete Zeilen zerreißen Streifen:** `:nth-of-type` zählt auch per `display:none` versteckte Zeilen (Client-Filter `table-filter.js`, zugeklappte Gruppen). Neu gestreift und client-gefiltert z. B. `WarehousePicking/Details.cshtml:67`. Bootstrap-Standardverhalten, bei schon gestreiften Listen heute genauso – hinnehmen.
- **Mehrere `tbody`** (Gruppen der IDEAL-Listen): Streifen beginnen je Gruppe neu – wie heute.
- **Ausgeblendete erste Spalte:** `column-preferences.js:163-178` blendet per `style.display` aus; ist die erste Zelle versteckt, fehlt der Balken, Hintergrund und Fokusring des Felds bleiben. Im WE ist Pos. gesperrt (`ViewKeyDriftGuardTests.cs:78-79`).
- **Spaltenfilter-Zeile/Kopf** liegen im `thead` (`table-filter.js:236`) – Regeln strikt auf `> tbody` begrenzen.
- **Touch-Terminals:** `:hover` bleibt nach Antippen hängen → `@media (hover: hover)` (Rückfrage 5); `site.css:1204` nutzt `@media (hover: none)` bereits.
- **`:has()`-Unterstützung** (Rückfrage 8): ohne Unterstützung nur keine Fokus-Zeile; die Regel steht allein, damit ein unbekannter Selektor keine Nachbarregel mitreißt.
- **Link-Regel nicht zu breit:** `Picking/Bom.cshtml:325` ist ein `<a class="badge text-bg-secondary">` – dunkelblau auf Grau wäre unlesbar, daher `:not(.badge)`.
- **Kontextklasse am `<table>`** würde von der späteren `.table`-Variable überschrieben – Wächter Punkt 4.

### Zusatzfunde (melden, nicht mitbauen)

- `site.css:734-749`: OSEON-Ebenenfarben und -Hover sitzen am `tr` (mit `!important`), die deckenden Zellen liegen darüber – vermutlich ebenso wirkungslos wie `site.css:198`. Sichtprüfung; ggf. eigene Notiz.
- `.vault-link`/`.dms-link` (`site.css:451-471`): #43A6E2 bzw. #EF7D00 mit `opacity: .7` liegen schon auf Weiß unter 3:1, `.vault-link:hover` wird weiß (auf Hellblau unsichtbar). Rückfrage 9.
- Bootstrap-Linkblau auf den bestehenden Streifen nur 4,0:1 – mit der Link-Regel dieser Spec behoben.

## Migrations-/SQL-Auswirkungen

Keine. Keine Entität, keine Migration, kein `SQL/`-Skript, `00_FreshInstall.sql` und `SQL/AgentJobs/` unberührt.

## Einstellungen

keine. Begründung: Zeilenfarben sind reine Darstellung ohne fachliche Wirkung und ohne Standortunterschied; ein Schalter wäre Konfiguration für einen Wert,
der sich nicht ändert (ponytail Sprosse 1). Tabellen, die keine Streifen wollen, nutzen die Ausnahmeklasse.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine – keine Datenänderung.

## Akzeptanzkriterien

Rollen/Zugriff: unverändert (reine Darstellung, keine Route, kein Filter). Listen-View-Pattern: keine neue Liste; bestehende Pagination, Filterkarte,
Spaltenfilter, Zahnrad, CSV und Sortierung funktionieren unverändert.

1. Eine bisher ungestreifte Liste (z. B. `/WarehousePicking`, `/WarehouseRequisitions`) zeigt abwechselnd hinterlegte Zeilen.
2. In jeder `.table` außerhalb der Ausnahmen ist die Zeile unter der Maus #A1D9F7, auch in Listen ohne `table-hover` und in den 8 Tabellen mit `table-hover` (vorher grau).
3. Zeilen mit `table-danger`/`-secondary`/`-success`/`-warning` behalten in Ruhe, Hover und Fokus ihren Kontextton (z. B. stornierte FA rot, erledigte grau, kommissionierte grün, überfällige Bestellung rot).
4. Gruppen-Kopfzeilen `tr.table-active` der IDEAL-Listen (Kommissionierlisten, Beschichtung, Vormontage) sind in Ruhe grau wie vorher.
5. Fokus in einem Text-/Zahlen-/Auswahl-/Memofeld einer Zeile: Zeile #D9F4FF und 4-px-Balken #052C65 links; Fokus wandert mit Tab; Fokus außerhalb: keine Markierung.
6. Mausklick in ein Kästchen (z. B. Sammelauswahl Leitstand) markiert die Zeile nicht; Tab auf ein Kästchen markiert sie (gemäß Rückfrage 7).
7. Fokus auf Link oder Knopf in einer Zeile markiert die Zeile nicht.
8. Zeile mit Fokus und Maus darüber: Fokus-Darstellung hat Vorrang.
9. Links in Tabellenzeilen sind #052C65, auch beim Überfahren; `.btn`, Badge-Links (`Bom.cshtml:325`) und Vault/enaio-Symbole unverändert.
10. Die drei Baumtabellen (FA-Struktur, Teileverfolgung, OSEON-Teileverfolgung) sehen aus wie vorher: keine Streifen, FA-Struktur-Hover hellgrau, „Hervorheben“ hellblau und Filter-Treffer amber in **jeder** Zeile sichtbar.
11. Strg+P einer Liste mit Hintergrundgrafiken an: keine Streifen, kein Hellblau. Druckansichten (Kommissionierliste, Stückliste, Beschichtungsauftrag, Lagerbestellung) und PDF unverändert.
12. Kopfzeile, Spaltenfilter-Zeile und Fußzeilen unverändert.
13. Kontrastwerte der Tabelle oben stichprobenhaft mit DevTools bestätigt (Text ≥ 4,5:1, Balken ≥ 3:1) in normal/Streifen/Hover/Fokus, mit und ohne `table-danger`.
14. Wareneingang zu Bestellung verhält sich wie TS-177.5/TS-184, mit Ausnahme von Rückfrage 7 (Link-Fokus markiert nicht mehr).
15. (Falls Rückfrage 6 ja) `text-danger`/`text-success` in Tabellenzeilen sind dunkel (#58151C/#0A3622) und auf allen Zeilengründen ≥ 4,5:1.
16. (Falls Rückfrage 5 ja) Auf einem reinen Touch-Gerät bleibt nach Antippen keine Zeile hellblau.
17. `TabellenZeilenfarbeTests` (5 Prüfungen) und der angepasste WE-Wächter sind grün; volle Testsuite grün.

## Test-Szenarien

Neues Kapitel **TS-187 — Alle Tabellen: Streifen, Hover und Fokus-Zeile in CI-Blau (v1.99.62)** in `docs/TESTSZENARIEN.md` (Worktree), danach
`secondbrain/tests/testszenarien-index.md`. **TS-184.4** („andere Tabelle … Hover unverändert grau“) als überholt durch TS-187 kennzeichnen, nicht löschen.
Standort DEV AKE und DEV IDEAL, Edge; nur Web; nur ansehen, nichts buchen (außer Fokus-Tests ohne Absenden).

| Nr | Schritte | Erwartung |
|---|---|---|
| 187.1 | AKE: `/WarehousePicking`, `/WarehouseRequisitions`, `/StockMovements/InboundBulk`; IDEAL: Kommissionierlisten, Vormontage, BDE-Teileliste | Streifen; Hover hellblau; Gruppenköpfe grau, beim Überfahren hellblau (AK 1, 2, 4) |
| 187.2 | Beschichtungsaufträge, Kommissionierung (`/Picking`), BDE-Buchungen (vorher `table-hover`) | Hover hellblau statt grau (AK 2) |
| 187.3 | FA-Liste mit storniertem und erledigtem FA; Offene Bestellungen mit überfälliger; Stückliste mit kommissionierter Zeile; Teileverfolgung je Werkbank mit gemeldetem AG | Rot/Grau/Grün bleiben in Ruhe und beim Überfahren (AK 3) |
| 187.4 | Mehrfach-Einbuchung, Lagerbestellung bearbeiten, FA-Abschluss bearbeiten, Wareneingang zu Bestellung: mit Tab durch Mengen-/Auswahl-/Memofelder; Klick außerhalb; in einer roten Zeile tippen | Fokus-Zeile sehr hellblau + Balken, wandert mit; außerhalb keine; rote Zeile bleibt rot mit Balken (AK 5, 8, 14) |
| 187.5 | Leitstand: Sammel-Kästchen mit der Maus anklicken, dann per Tab ansteuern; Link „Bearbeiten“/Knopf in einer Liste fokussieren | Maus: keine Fokus-Zeile; Tab: Fokus-Zeile; Link/Knopf: keine (AK 6, 7) |
| 187.6 | Links in Zeilen (FA-Nummer, „Lagerbestellung Nr. …“, Artikel) normal und beim Überfahren; Stückliste „+n weitere“-Badge; Vault/enaio-Symbole | Links dunkelblau, Badge und Symbole unverändert (AK 9) |
| 187.7 | IDEAL: FA-Struktur mit „Hervorheben“ und Spaltenfilter; AKE: Teileverfolgung und OSEON-Teileverfolgung aufklappen | wie vorher, keine Streifen, Markierungen in jeder Zeile (AK 10) |
| 187.8 | Eine Liste mit Strg+P (Hintergrundgrafiken an) und als PDF; Kommissionierliste drucken; Stückliste drucken | keine Farbflächen; Druckansichten unverändert (AK 11) |
| 187.9 | Spaltenfilter setzen, Zahnrad: erste Spalte ausblenden, Spalte verschieben, sortieren, CSV | Filterzeile/Kopf unverändert; Fokus-Zeile ohne Balken bei ausgeblendeter erster Spalte, Hintergrund sichtbar (AK 12) |
| 187.10 | Modals: BDE-Terminal „Alle fertig melden“, FA-Struktur Arbeitsgänge; Artikelinfo; Einstellungen; Hilfe | gemäß Rückfrage 2/3 gestreift oder Ausnahme (AK 1, 10) |
| 187.11 | Kontrast mit DevTools: Text, `text-muted`, Link, Balken je Zustand normal/Streifen/Hover/Fokus, mit `table-danger` | Werte ≥ Tabelle der Spec (AK 13, 15) |
| 187.12 | Negativ: Android-Scanner (Touch) – Zeile antippen, wegtippen | keine hängende Hover-Farbe (AK 16) |
| 187.13 | Negativ: Seite ohne Tabelle, Konsole offen | keine Fehler, kein Layoutsprung |

**Automatisch:** `TabellenZeilenfarbeTests` (5 Prüfungen, s. Wächter), `ViewKeyDriftGuardTests.WareneingangPositionen_*` angepasst, volle Suite.

## Etappen (nur bei epic: true)

Nicht zutreffend (`epic: false`) – ein Commit.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

*Vorläufig – der QA-Lauf bestätigt gegen den echten Diff.*

- **Web-App:** ja (`site.css`, wenige Views, Changelog, Version **1.99.62** – 1.99.60 (Menü FA-Struktur, `bcb7a357`) und 1.99.61 (FA-Struktur-Titel/Gate) sind vergeben; Dev-Lauf prüft erneut).
- **Service:** nein (nur `AppVersion.cs` mitzählen, kein Service-Deploy nötig, sofern der Dev-Lauf am Service nichts ändert).
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): keine – kein SQL, keine Einstellung, kein Leitstand-Schritt, kein Ordner. Optional eine
  `Info`-Zeile („Tabellen zeigen Streifen und hellblauen Hover“) nur, wenn Gerald eine Anwender-Info beim Update will – sonst keine.
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` (im Worktree). Browser-Cache:
  `site.css` hängt mit `asp-append-version` (`_Layout.cshtml:12`) – kein manuelles Leeren nötig.

**Brain im Umsetzungslauf:** neuer ADR (nächste freie Nummer, laut Ordner `secondbrain/architektur/adr/` am 2026-11-23 erneut geprüft: höchste 0034, also 0035) „Tabellen-Zeilenfarben global über Bootstrap-Zustandsvariablen mit
Ausnahmeklasse“; `fallstricke.md` ergänzen (Abschnitt 61 um „`--bs-table-active-bg` nie global“, „tr-Hintergrund in Bootstrap 5.3 unsichtbar“);
Changelog, feature-map, Testindex.

## Offene Rueckfragen

1. Variante A (global auf `.table`, Ausnahmeklasse `table-plain`) oder B (Opt-in-Klasse je View)? Empfehlung A (Begründung im Variantenvergleich).
2. Ausnahmen genau: die drei Baumtabellen (`#faTree`, `#trackingTree`, `#oseonTree`) plus die randlose Schlüssel-Wert-Tabelle der Artikelinfo (`Articles/Info.cshtml:151`)? Weitere (z. B. Kopf-Tabelle `fa-head-table` im FA-Baum, `FaHierarchy/Index.cshtml:222`)?
3. Streifen auch in kleinen Detail-, Modal- und Hilfetabellen (Arbeitsgänge-Modal, BDE-Gruppenmeldung, Positionsauswahl, Hilfe)? Empfehlung ja, einheitlich.
4. Streifenfarbe: Bootstrap-Grau wie im Wareneingang (#F2F2F2) oder CI-Grau `--ake-light-gray` (#F5F5F5)? Empfehlung Bootstrap-Grau; kein Blau, sonst Verwechslung mit der Fokus-Zeile.
5. Hover nur bei Geräten mit Maus (`@media (hover: hover)`), damit auf reinen Touch-Scannern nach dem Antippen keine Zeile hellblau hängen bleibt? Empfehlung ja.
6. Farbige Textklassen in Tabellenzeilen (`text-danger/-success/-primary/-info/-warning`) auf die dunklen `-emphasis`-Töne umstellen? Heute schon auf Streifen ca. 4,0:1, auf Hellblau ca. 3,0:1. Empfehlung ja, nur in `tbody` von `.table`.
7. Fokus-Zeile: Auslöser nur Wertfelder (Maus und Tastatur), Kästchen/Optionsfelder nur Tastatur, Links/Knöpfe nie. Im Wareneingang markiert ein fokussierter Link die Zeile dann nicht mehr. Einverstanden?
8. (IT/Fachbereich) Laufen alle Terminals und Scanner auf Edge/Chrome ab 105 bzw. Firefox ab 121 (`:has()`)? Ohne Unterstützung entfällt nur die Fokus-Zeile.
9. Vault-/enaio-Symbol-Links in Tabellen (schon auf Weiß unter 3:1) hier mitnehmen oder eigene Backlog-Notiz? Empfehlung eigene Notiz.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

<!-- kein Jira-Vorgang -->

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Variante A oder B → **A – global auf `.table`, Ausnahmeklasse `table-plain`.**
2. Ausnahmen (Bäume + Artikelinfo-Schlüssel-Wert-Tabelle, weitere?) → **Die drei Bäume (`#faTree`, `#trackingTree`, `#oseonTree`), Artikelinfo-Schlüssel-Wert-Tabelle (`Articles/Info.cshtml:151`) und die FA-Kopf-Tabelle `fa-head-table`.**
3. Streifen auch in Detail-/Modal-/Hilfetabellen → **Ja, überall.**
4. Streifenfarbe Bootstrap-Grau oder CI-Grau → **Bootstrap-Grau #F2F2F2.**
5. Hover nur bei Maus (`hover: hover`) → **Nein – Hover überall, ohne `@media (hover: hover)`.**
6. Farbige Textklassen in Tabellen auf `-emphasis` → **Ja, dunkler.**
7. Auslöser der Fokus-Zeile (Wertfelder; Kästchen nur Tastatur; WE-Link-Fokus entfällt) → **Ja, wie empfohlen: nur Eingabefelder; Kästchen nur Tastatur; Links/Knöpfe nie.**
8. Browser der Terminals/Scanner (`:has()`) → **Unklar – trotzdem `:has()`; Geräteliste im UAT prüfen, auf alten Geräten fehlt nur die Fokus-Zeile.**
9. Vault-/enaio-Symbol-Kontrast hier oder eigene Notiz → **Hier mitnehmen (in dieser Spec umsetzen).**
