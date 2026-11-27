---
type: spec
title: "Wareneingang zu Bestellung: Bestellnummer ohne Jahr, aktive Zeile, Hauptlagerplatz als Vorschlag"
slug: 2026-11-23-wareneingang-zu-bestellung-bedienung-spec
status: Testbereit
created: 2026-11-23
updated: 2026-11-23
source_backlog: "[[2026-11-23-wareneingang-zu-bestellung-bedienung]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: AKE
affected_code:
  - IdealAkeWms/Views/PurchaseOrders/Receive.cshtml
  - IdealAkeWms/Controllers/PurchaseOrdersController.Receive.cs
  - IdealAkeWms/Controllers/PurchaseOrdersController.cs (Ctor, neue Abhaengigkeit IArticleRepository)
  - IdealAkeWms/Data/Repositories/SagePurchaseOrderLineRepository.cs (GetBelIdsByBelegnummerAsync)
  - IdealAkeWms/Data/Repositories/IArticleRepository.cs + ArticleRepository.cs (neue Abfrage Hauptlagerplatz je ArticleId)
  - IdealAkeWms/Models/ViewModels/PurchaseOrderReceiveViewModel.cs (Hinweistext Ziel-Vorschlag)
  - IdealAkeWms/wwwroot/css/site.css (Fokus-Zeile, falls nicht im View)
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (neue ViewConfig PurchaseOrderReceive + GetByViewKey)
  - IdealAkeWms.Tests (PurchaseOrdersReceiveTests, PurchaseOrdersControllerTests, Repository-Test, ViewKeyDriftGuardTests bzw. View-Test Receive)
  - docs/TESTSZENARIEN.md (TS-116 Verweis + neues Kapitel TS-177), docs/anwender (Wareneingang), Views/Help (Wareneingang), Views/Help/Changelog.cshtml, beide AppVersion.cs
open_questions: []   # 1-4 beantwortet (Freigabe-Antworten 2026-11-23), Rework 2026-11-23 eingearbeitet
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald"
freigabe_am: "2026-11-23"
---

# Wareneingang zu Bestellung: Bedienung nachschärfen

Quelle: [[2026-11-23-wareneingang-zu-bestellung-bedienung]]. Vorgänger-Specs: [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]] (v1.84.0/v1.86.0),
[[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]] (Sage-WE-Beleg), [[2026-11-11-wareneingang-teil-3-rahmenabrufe-spec]].

> [!info] Codestand
> Alle Fundstellen beziehen sich auf den Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (AppVersion 1.99.47). Auf `main`
> fehlt der Wareneingang.

## Ziel / Nutzen (das Warum)

Am Wareneingang wird die Seite mit Handscanner und Tastatur im Stehen bedient. Drei Reibungen kosten dort Zeit und erzeugen Fehlgriffe:

1. Die Bestellnummer muss mit dem Jahres-Präfix eingetippt werden (`2027-167578`). Gesprochen und auf Papier ist sie oft nur `167578`.
2. In der breiten Positionstabelle (12 Spalten) sieht man nicht, in welcher Zeile man gerade Menge oder Platz eingibt. Die Zeilen sind nicht
   abwechselnd eingefärbt.
3. Hat die Position keinen buchbaren Sage-Platz, steht „— wählen —“, und der Lagerist sucht den Platz aus einer langen Liste. Der
   Hauptlagerplatz des Artikels ist meistens das richtige Ziel und im WMS schon bekannt.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope** (nur Seite `/PurchaseOrders/Receive`, Standort AKE):
- F1 Bestellnummer ohne Jahr im Feld „Bestellnummer eingeben oder scannen“ (Eingabe und Hardware-Scan, derselbe Weg).
- F2 Positionstabelle: Zeilenstreifen, Hover, Markierung der Zeile mit dem Eingabefokus.
- F2b Positionstabelle: Spaltenanpassung über das Zahnrad (Spalten ein-/ausblenden, Breite ziehen) mit dem vorhandenen
  `column-preferences.js` wie auf „Offene Bestellungen“ (Freigabe-Antwort 1).
- F3 Ziel-Lagerplatz: Ersatzvorschlag Hauptlagerplatz des Artikels, wenn der Sage-Platz der Position nicht auswählbar ist.

**Out-of-Scope:**
- Filter „Bestellung“ der Liste „Offene Bestellungen“ (auch in der Einkaufsansicht). Er sucht schon heute mit „enthält“
  (`PurchaseOrdersController.cs:149`, `Contains(…, OrdinalIgnoreCase)`), `167578` findet dort `2027-167578` bereits. Keine Änderung.
- ~~Zahnrad (Spaltenauswahl) auf der Receive-Seite (siehe Rückfrage 1).~~ *Überholt durch Freigabe-Antwort 1: das Zahnrad ist jetzt In-Scope (F2b).*
- Spalten umsortieren, Standardsortierung, Spaltenfilter und CSV-Export auf der Receive-Seite (kleine Eingabemaske; `SupportsReorder` und
  `SupportsSortDefault` aus wie bei „Offene Bestellungen“, kein `filterable-table`).
- Änderungen am Abgleich `EinkaufBestellungenSyncService`, am Buchen (POST), an der Sage-WE-Platzregel und an der Sage-Übergabe.
- Standort IDEAL: Wareneingang zu Bestellung bleibt dort aus, bis ein SData-Endpunkt da ist (Freigabe-Antwort 7 in
  [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]] Z. 503; an IDEAL ist kein SData-Endpunkt eingetragen, [[sage-sdata-benutzerfelder-lesetest]] Z. 1043).
  Der Code ist gemeinsam, wirkt an IDEAL aber erst mit eingeschaltetem Feature.

## Fachliche Anforderungen

### F1 Bestellnummer ohne Jahr

**Belegt:**
- Die Belegnummer im Cache ist `BelegnummerFormatiert` aus SData, Format `<Belegjahr>-<Belegnummer>` (P-W2 in
  [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]] Z. 305). Geschrieben getrimmt vom Abgleich (`EinkaufBestellungenSyncService.cs:196`).
- **Das Belegjahr ist nicht das Kalenderjahr.** Am 2026-11-11/12 trugen neue Belege am AKE-Test das Jahr 2027 (Bestellung `2027-168715`,
  Sage-WE-Beleg `2027-398675`; [[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]] Z. 687-699, Z. 692: „Bestellnummer 2027-… =
  Geschaeftsjahr ≠ Kalenderjahr“). Wann das Geschäftsjahr wechselt, steht nur in Sage und ist im WMS nicht hinterlegt.
- Heute sucht der Einstieg exakt (`SagePurchaseOrderLineRepository.cs:33-37`: `Belegnummer == key`), `167578` findet also nichts und meldet
  „Bestellung „167578“ nicht gefunden“ (`PurchaseOrdersController.Receive.cs:21-22`).
- Eingabe und Hardware-Scan nutzen dasselbe Feld (`Receive.cshtml:29-30`, `data-hw-scan-ziel`) und denselben GET (`Receive.cs:15-25`).
  Eine Regel im Repository wirkt also für beide Wege.

**Anforderung (Freigabe-Antwort 2: ja):** Das WMS ergänzt **kein** Jahr. Ein reiner Ziffern-Eintrag wird gegen die Endung aller offenen
Bestellungen im Cache gesucht (`Belegnummer` endet auf `-<Eingabe>`). Damit ist die Frage „welches Jahr“ für das WMS gegenstandslos, und es
braucht weder eine Einstellung noch einen Sage-Lesezugriff.
- Eingabe mit `-` (oder sonst nicht nur Ziffern): exakter Vergleich wie bisher.
- Genau eine `BelId` → Bestellung öffnet sich.
- Keine → bisherige Meldung „nicht gefunden …“.
- Mehrere `BelId` (z. B. `2026-167578` und `2027-167578`, oder dieselbe Nummer an zwei Belegen) → Warnmeldung mit **allen** gefundenen
  Belegnummern, z. B. „167578 passt zu 2 Bestellungen: 2026-167578, 2027-167578 – bitte mit Jahr eingeben.“ Nie still die erste oder die
  neueste wählen.
- Feldtext: Label „Bestellnummer (mit oder ohne Jahr) eingeben oder scannen“, Platzhalter „z. B. 168975 oder 2027-168975“.

**Nicht belegt, Prüfschritt (Sage, nur SELECT, Mensch):** Ob Sage die Belegnummer je Belegjahr neu beginnt, entscheidet nur, wie oft der Fall
„mehrere Treffer“ vorkommt. Das ändert die Lösung nicht. Prüfabfrage **P-1** im Abschnitt „Prüfabfragen“.

### F2 Positionstabelle: aktive Zeile erkennbar

**Belegt:**
- Die Tabelle ist `table table-sm align-middle` ohne Streifen und Hover (`Receive.cshtml:115`). Fehlerzeilen tragen `table-danger` am `<tr>`
  (`Receive.cshtml:138`).
- Vorbild `PurchaseProposals/Index.cshtml:154`: `table table-sm table-striped`. Auch „Offene Bestellungen“ ist gestreift
  (`PurchaseOrders/Index.cshtml:117`).
- Bootstrap 5.3 färbt Zellen über `box-shadow: inset … var(--bs-table-bg-state, var(--bs-table-bg-type, …))` (`bootstrap.css:1878-1884`);
  Streifen setzen `--bs-table-bg-type` (`:1918-1921`), Hover und `table-active` setzen `--bs-table-bg-state` (`:1928-1935`). `.table-danger`
  definiert eigene Rottöne für Streifen, Hover und aktiv (`:2008-2020`: `#eccccf`, `#e5c7ca`, `#dfc2c4`). **Folge:** Streifen, Hover und eine
  Aktiv-Markierung über `--bs-table-active-bg` bleiben in einer Fehlerzeile rot; `table-danger` wird nicht überdeckt, solange keine eigene
  Hintergrundfarbe gesetzt wird.
- Ein Zahnrad gibt es auf dieser Seite heute nicht: `Receive.cshtml` bindet `column-preferences.js` nicht ein (das Zahnrad entsteht nur dort,
  `column-preferences.js:422-424`). Auf „Offene Bestellungen“ gibt es eines (`PurchaseOrders/Index.cshtml:206`, TS-116.10). Es kommt mit F2b.

**Anforderung:**
- Tabelle bekommt `table-striped table-hover` (Bootstrap nativ, kein eigenes JS).
- Die Zeile, in der ein Eingabeelement den Fokus hat (Ist-Menge, Ziel-Lagerplatz, Überlieferung, Positionsmemo), ist markiert, solange der
  Fokus darin steht. Umsetzung nur mit CSS `:focus-within` (Plattformmittel, Leiter Sprosse 4), kein Klick-Handler. Klick in eine Zelle ohne
  Eingabefeld markiert nichts (kein Fokusziel); das ist gewollt.
- Die Markierung besteht aus **zwei** Teilen, damit Farbe nicht allein trägt und sie bei schlechtem Licht sichtbar ist:
  1. Hintergrund im Aktiv-Ton der Zeile (`--bs-table-active-bg`; in `table-danger`-Zeilen also `#dfc2c4`, sonst Bootstrap-Grau);
  2. ein Balken bzw. Rahmen in der Primärfarbe mit **mindestens 3:1** Kontrast zum Zeilenhintergrund (WCAG 1.4.11), z. B. 4 px am linken
     Rand der Zeile oder ein Umriss um die Zeile. Ein eigenes `box-shadow` an den Zellen ersetzt Bootstraps Hintergrund-Schatten – wer den
     Balken so baut, muss den Hintergrund-Schatten mit angeben, sonst verschwinden Streifen und Rot ([[fallstricke]] Z. 283-295 zu
     Spezifität und Vererbung).
- CSS mit Kindkette auf eine eigene Klasse der Tabelle (z. B. `.we-positionen > tbody > tr:focus-within > *`), nicht global auf alle Tabellen.
- Kontrast nach der Änderung ≥ 4,5:1 für Fließtext in allen Zuständen (normal, Streifen, Hover, aktiv, jeweils mit und ohne `table-danger`),
  einschließlich `text-body-secondary` (Hinweistexte), `text-danger-emphasis` (Fehlertext, Lieferart „Überlieferung“) und Badge
  `text-bg-secondary`. Grobe Rechnung: Fließtext `#212529` auf `#dfc2c4` ≈ 12:1, `text-body-secondary` dort ≈ 5:1 – der Dev misst nach.
- Skill `frontend-design` ist für diesen Teil Pflicht (CLAUDE.md); Bootstrap-Konsistenz geht vor.

### F2b Positionstabelle: Spaltenanpassung (Zahnrad)

Freigabe-Antwort 1 (Gerald 2026-11-23): Die Positionstabelle bekommt das Zahnrad – Spalten ein-/ausblenden und Breite anpassen – über das
vorhandene `column-preferences.js` wie auf „Offene Bestellungen“. Keine neue Bibliothek.

**Belegt – wie das vorhandene Muster arbeitet:**
- Drei Quellen müssen gleich sein: `<th data-col-key>` in der View, der Block `#column-config` (+ `#view-config` mit `viewKey`) in der View und
  der Katalog `ColumnDefinitions` mit Eintrag in `GetByViewKey` (Vorbild `PurchaseOrders/Index.cshtml:114-117, 182-203`,
  `ColumnDefinitions.cs:775-797, 867`). Ohne Katalogeintrag antwortet `/api/user-view-preferences/{viewKey}` mit 400
  (`UserViewPreferencesApiController.cs:29, 45, 67`); `ViewKeyDriftGuardTests.cs:27-37` prüft jede View mit `#view-config` darauf.
- Das Zahnrad hängt sich in den Toolbar-Host vor der Tabelle (`column-preferences.js:422-424`, `site.js:12`); der Host ist ein
  `<div … data-table-toolbar>` direkt vor dem `.table-responsive` (Vorbild `PurchaseOrders/Index.cshtml:115`). Der Knopf ist
  `type="button"` (`column-preferences.js:409-410`) und sendet das umgebende Formular also nicht ab.
- `column-preferences.js` greift auf die **erste** `table[data-view-key]` der Seite (`:899`); die Receive-Seite hat genau eine Tabelle.
- Ausblenden setzt `style.display = 'none'` an `<th>` und Zellen (`:160-180`). **Eingabefelder in ausgeblendeten Zellen bleiben im DOM und
  werden mit dem Formular gesendet** (nur `disabled` schließt aus); ihre Namen `zeilen[i].…` ändern sich nicht. Der Server ordnet die Eingaben
  über `BelPosId` zu (`Receive.cs:205`), nicht über die Spaltenposition.
- `locked` sperrt nur in der Oberfläche (Häkchen `disabled`, kein Kontextmenü „ausblenden“, `:509, 525, 724-730`). Beim Anwenden gespeicherter
  Einstellungen wird `locked` nicht erzwungen (`:113-121, 139-144`); einen Server-Abgleich gibt es nicht. Für den **neuen** View-Key gibt es
  keine Alt-Einstellungen; das Risiko besteht nur bei manuell gebauten API-Aufrufen oder wenn eine Spalte später nachträglich gesperrt wird
  (bekannte Grenze, nicht Teil dieser Spec).
- Umsortieren würde Zellen tauschen; dafür `SupportsReorder: false` (wie „Offene Bestellungen“). Kein `filterable-table` → weder
  `table-filter.js` noch der CSV-Knopf (`csv-export.js:1-2, 256-257` greift nur dort).

**Anforderung:**
- View-Key `PurchaseOrderReceive`, Anzeigename „Wareneingang zu Bestellung – Positionen“ (erscheint so in der Benutzerverwaltung,
  `Views/Users/Edit.cshtml:218`). `SupportsReorder: false`, `SupportsSortDefault: false`.
- Spaltenkatalog (Reihenfolge = heutige Spalten `Receive.cshtml:118-129`):

| Key | Überschrift | gesperrt | Grund |
|---|---|---|---|
| `pos` | Pos. | ja | trägt das versteckte `BelPosId`-Feld und die Nummer, auf die sich alle `aria-label` „Position n“ beziehen; erste Zelle für die Fokus-Markierung (F2) |
| `artikel` | Artikel | ja | Artikelnummer, Bezeichnung, Hinweise Charge/Seriennr., Sage-WE-Stand, offener Bedarf |
| `bestellt` | bestellt | nein | |
| `offen` | offen (Sage) | nein | |
| `erfasst` | im WMS erfasst | nein | |
| `rest` | Rest | ja | Grundlage für vollständig/Teil/Überlieferung |
| `einheit` | Einheit | ja | gebucht wird in der Basiseinheit; ohne Einheit droht eine falsche Menge |
| `lagerplatz` | Ziel-Lagerplatz | ja | Pflichtfeld zum Buchen (`Receive.cs:82`) |
| `menge` | Ist-Menge | ja | Pflichtfeld zum Buchen |
| `ueberlieferung` | Überlieferung | ja | Bestätigung, die der Server bei Überlieferung verlangt (`Receive.cs:85`) |
| `memo` | Positionsmemo | nein | optional |
| `status` | Status | ja | zeigt „nicht buchbar“, Lieferart und den Fehlertext der Zeile (`Receive.cshtml:229-242`) – ausgeblendet hieße Fehler still verschweigen |

  Die Nachricht der Koordination nennt „Menge/Lagerplatz/Übernehmen“ als Pflichtspalten; eine Spalte „Übernehmen“ gibt es nicht – gemeint ist
  hier die Spalte „Überlieferung“ (einzige weitere Eingabe-Spalte mit Serverprüfung). Rest, Einheit, Status und Pos. sind zusätzlich gesperrt
  (Gründe in der Tabelle); ausblendbar bleiben bestellt, offen (Sage), im WMS erfasst und Positionsmemo.
- Breite je Spalte ziehbar (vorhandene Griffe `attachResizeHandles`, `:925`), gespeichert je Benutzer.
- Ausgeblendetes Positionsmemo: ein schon eingetragener Wert wird trotzdem gesendet; das Zahnrad-Panel zeigt die Spalte als ausgeblendet.
  Kein Sonderfall nötig.
- Zusammenspiel mit F2: Streifen/Hover/`table-danger` sitzen am `<tr>` und sind von ausgeblendeten Zellen unabhängig; `:focus-within` erreicht
  ausgeblendete Felder nicht (nicht fokussierbar). Der Fokus-Balken sitzt an der gesperrten Spalte Pos. bzw. als Umriss an der Zeile, damit er
  nie mit einer ausgeblendeten Zelle verschwindet.
- Das Zahnrad steht rechts über der Tabelle (Toolbar-Host), unverändert im Aussehen wie auf „Offene Bestellungen“ (kein eigener Knopfstil).

### F3 Hauptlagerplatz als Ziel-Vorschlag

**Belegt – woher der Ziel-Platz heute kommt:**
- Vorbelegung im GET = `SagePurchaseOrderLine.StorageLocationId` (`PurchaseOrdersController.Receive.cs:231`). Der Abgleich löst sie aus der
  `PlatzID` der Sage-Position über `StorageLocations.SageLagerplatzId` auf (`EinkaufBestellungenSyncService.cs:146-148, 174-175, 201-202`).
- Die Auswahlliste enthält nur aktive, buchbare Plätze ohne Kommissionierwagen (`GetActiveOrderedExcludingPickingTransportAsync`,
  `StorageLocationRepository.cs:43-49`: `IsActive && !IsPickingTransport && IstBuchbar`; Aufruf `Receive.cs:217`). Ist die vorbelegte Id
  nicht in der Liste, ist keine Option `selected`, es steht „— wählen —“ (`Receive.cshtml:183-187`).
- „— wählen —“ entsteht damit in drei Fällen: (a) Position ohne Sage-Platz (`SagePlatzId` null; **heute ohne jeden Hinweis**),
  (b) Sage-Platz nicht im WMS (Hinweis `Receive.cshtml:189-191`), (c) zugeordneter WMS-Platz nicht buchbar oder inaktiv (Hinweis
  `Receive.cshtml:193-197`, v1.86.0, Beispiel HR;0;0;0).

**Belegt – woher der Hauptlagerplatz kommt:**
- WMS-Feld `Article.PrimaryStorageLocationId` (Anzeigename „Hauptlagerplatz“, `Article.cs:49-51`), daneben der Sage-Rohcode
  `Article.SagePrimaryStorageLocation` (`Article.cs:53-55`).
- Schreibwege: (1) Artikel-Abgleich des Dienstes. Er liest `KHKArtikel.PlatzID` → `KHKLagerplaetze.Kurzbezeichnung`
  (`SageArticleSql.cs:19, 24, 35, 39`), setzt `PrimaryStorageLocationId` über den **Code** des WMS-Platzes; kein Treffer = FK leer +
  Warnung `hauptlagerplatz_fehlt`; liefert Sage keinen Platz, bleibt der FK unangetastet (`SageImportService.cs:502-529`).
  (2) Manuell in der Artikelpflege, nur wenn Sage keinen Hauptlagerplatz liefert (`ArticlesController.cs:185-189`).
- Leser heute: Bestandsansichten markieren den Hauptlagerplatz (`StockMovementRepository.cs:174-186, 455-485`), die Kommissionierung
  schlägt ihn als **Quelle** vor (`PickingController.cs:670-679` `QuellVorschlag`).
- `QuellVorschlag` ist **nicht** wiederverwendbar: Er verlangt Bestand > 0 auf dem Platz und fällt auf „Platz mit der höchsten Menge“
  zurück – beides ist für ein **Ziel** beim Wareneingang falsch (Artikel ohne Bestand bekäme keinen Vorschlag, und der Rückfall schlüge
  einen beliebigen Bestandsplatz vor). Die Feldwahl `PrimaryStorageLocationId` ist dieselbe, die Regel nicht.
- Die Felder `Hauptlagerplatz` an `FaHierarchyBomItem`/`FaHierarchyNode`/`BfAuftrag` sind Texte aus Stücklisten- bzw. BF-Daten
  (`FaHierarchyBomItem.cs:15`, `BfAuftrag.cs:85`), nicht der Artikel-Stammsatz, und für eine Bestellposition nicht erreichbar. Nicht verwenden.

**Nicht belegt, Prüfschritt:** Ob die `PlatzID` einer Sage-Bestellposition vom Hauptlagerplatz des Artikels abweicht oder nur dessen
Vorbelegung ist, steht nur in Sage. Liefert P-2 „fast immer gleich“, hilft F3 vor allem im Fall (a) und dort, wo die Code-Zuordnung des
Artikel-Abgleichs einen anderen (buchbaren) WMS-Platz trifft als die `SageLagerplatzId`-Zuordnung. P-3 zählt auf AKE-DEV, wie viele offene
Positionen tatsächlich einen Vorschlag bekämen. Beide Ergebnisse ändern den Bau nicht, sie gehören als Befund in die Spec.

**Anforderung (nur GET bzw. erster Aufbau der Maske; im POST bleibt die Eingabe des Benutzers stehen):**
1. Vorrang **Sage-Platz der Position**: ist `Line.StorageLocationId` in `Model.Lagerplaetze`, bleibt er vorbelegt (Entscheidung 5 der
   v1.84-Spec, Z. 501). Unverändert.
2. Sonst **Hauptlagerplatz**: `Article.PrimaryStorageLocationId` des Artikels der Position (`Line.ArticleId`), wenn er in `Model.Lagerplaetze`
   steht – bei `SageWeAktiv` zusätzlich nur, wenn `SageWareneingang.IstSageWePlatz` gilt (`SageWareneingang.cs:39-40`; sonst würde der Vorschlag
   beim Buchen mit `PlatzFehler` abgewiesen, `Receive.cs:84`; Freigabe-Antwort 3). Das gilt auch im Fall (c) – zugeordneter Sage-Platz nicht
   buchbar/inaktiv – mit stehenbleibendem Hinweis (Freigabe-Antwort 4; Ausblick Gerald: künftig werden alle Sage-Plätze im WMS buchbar
   gestellt, der Fall wird selten, der Hinweis bleibt).
3. Sonst leer wie heute.
- Hinweistext unter dem Select, Farbe nie allein (Muster `small text-body-secondary` wie `Receive.cshtml:191`):
  - Vorschlag aus Hauptlagerplatz: „Vorschlag: Hauptlagerplatz des Artikels“ – zusätzlich zu einem bestehenden Hinweis aus (b)/(c), der stehen
    bleibt.
  - Hauptlagerplatz vorhanden, aber nicht auswählbar: „Hauptlagerplatz <Code> ist im WMS nicht buchbar – bitte Ziel wählen“
    bzw. bei Sage-WE „Hauptlagerplatz <Code> hat keine Sage-Buchung – bitte Sage-Platz wählen“.
  - Fall (a) ohne Hauptlagerplatz: „Kein Sage-Platz an der Position – bitte Ziel wählen“ (meldet den bisher stillen Fall).
- Gesperrte Zeilen („Artikel nicht im WMS“, „Einheit abweichend“) bleiben unverändert ohne Select.
- Der Server prüft beim Buchen wie bisher (`Receive.cs:82, 84`); der Vorschlag ist nur Vorbelegung, keine neue Regel.

## Technischer Loesungsentwurf

Muster: Repository-Interfaces (ADR 0001), MVC-Controller mit `TempData["WarningMessage"]`, Spaltenauswahl über `column-preferences.js` +
`ColumnDefinitions` + `tableToolbarHost`; kein neues Partial, kein neues JS, keine neue Bibliothek.

**F1** – `ISagePurchaseOrderLineRepository.GetBelIdsByBelegnummerAsync` (einziger Aufrufer `Receive.cs:19`, Grep im Worktree) liefert künftig
`(BelId, Belegnummer)` je Treffer (Distinct über `BelId`), damit die Meldung die Belegnummern nennen kann:
- Schlüssel getrimmt; besteht er nur aus Ziffern: `Where(l => l.Belegnummer.EndsWith("-" + key))`, sonst `Belegnummer == key` wie bisher.
  `-` ist in `LIKE` kein Platzhalter; der Cache hat rund 1.000 bis 1.200 Zeilen (P-W1), ein Index ist nicht nötig.
- Methodenname darf bleiben oder sprechend werden (`FindBestellungenAsync`); Dev entscheidet, Moq-Setups in den Tests ziehen mit.
- Meldung bei mehreren Treffern im Controller aus den gelieferten Belegnummern (sortiert).
- Label/Platzhalter in `Receive.cshtml:28-29`.

**F2** – nur View + CSS:
- `Receive.cshtml:115`: `class="table table-sm table-striped table-hover align-middle we-positionen"`.
- Eine CSS-Regel für `:focus-within` (Hintergrund über `--bs-table-bg-state: var(--bs-table-active-bg)` und `--bs-table-color-state:
  var(--bs-table-active-color)` an den Zellen) plus der Balken/Umriss. Ort: `wwwroot/css/site.css` (lädt nach Bootstrap) mit Kommentar und
  Versionsvermerk.
- Sichtprüfung am Bildschirm Pflicht (Zustände normal/Streifen/Hover/aktiv × mit/ohne `table-danger`).

**F2b** – Muster „Offene Bestellungen“ (v1.95.0) 1:1:
- `ColumnDefinitions.cs`: neue `ViewConfig PurchaseOrderReceive` (Katalog aus F2b, `Locked` wie dort) und Zeile in `GetByViewKey`.
- `Receive.cshtml`: Toolbar-Host `<div class="d-flex justify-content-end mb-1" data-table-toolbar></div>` direkt vor `.table-responsive`
  (`:114`); an der Tabelle `data-view-key="PurchaseOrderReceive"`; jedes `<th>` mit `data-col-key`; Blöcke `#view-config`
  (`supportsReorder: false`, `supportsSortDefault: false`) und `#column-config` innerhalb von `@if (Model.BelId != null)`; `column-preferences.js`
  im Abschnitt `Scripts`. Keine Klasse `filterable-table`.
- Formular-Indizes `zeilen[i]`, `id="ueber_@i"` und das Rest-Skript (`Receive.cshtml:269-280`, sucht je `tr[data-rest]`) bleiben unverändert.
- Test analog `ViewKeyDriftGuardTests.OffeneBestellungen_DreiQuellenGleich_UndToolbarHost` (`:39-48`): `<th data-col-key>` = `#column-config` =
  Katalog, Toolbar-Host vorhanden, gesperrte Keys genau wie in F2b.

**F3** – Controller + kleine Repository-Abfrage:
- Neue Methode `IArticleRepository.GetPrimaryStorageLocationIdsAsync(IReadOnlyCollection<int> articleIds)` →
  `Dictionary<int,int>` (nur Artikel mit FK). Eine Abfrage für alle Positionen, kein N+1. `IArticleRepository` hat keinen Caching-Decorator
  (`Program.cs:74` registriert direkt `ArticleRepository`).
- `PurchaseOrdersController` bekommt `IArticleRepository` in den Ctor (Tests mit Ctor-Aufruf ziehen mit, als „bewusst geändert“ melden).
- In `BuildReceiveAsync` (`Receive.cs:200-236`) nur der Default-Zweig `eingabe == null` (`:231`): Vorbelegung nach Regel 1–3 oben. Die
  Herkunft des Vorschlags und ggf. der Code eines nicht auswählbaren Hauptlagerplatzes kommen als zwei Eigenschaften an
  `PurchaseOrderReceiveRow` (z. B. `ZielVorschlagHauptlagerplatz`, `HauptlagerplatzNichtWaehlbar`), Texte im View. Den Code eines nicht
  auswählbaren Hauptlagerplatzes aus der schon geladenen Platzliste `GetAllOrderedAsync` (`PurchaseOrdersController.cs:215`) bzw. einem
  zweiten Aufruf davon – keine weitere Tabelle.
- Kein Eingriff in `QuellVorschlag`, in den Abgleich oder in den POST.

## Migrations-/SQL-Auswirkungen

Keine. Keine Migration, kein `SQL/XX_*.sql`, `SQL/00_FreshInstall.sql` und `SQL/AgentJobs/` unberührt. Nur Lesezugriffe auf bestehende
Spalten (`SagePurchaseOrderLines.Belegnummer`, `Articles.PrimaryStorageLocationId`).

## Einstellungen

keine. (Bewusst keine Einstellung „Belegjahr“: F1 sucht ohne Jahr; Freigabe-Antwort 2. Die Spaltenauswahl ist eine persönliche
Ansichtseinstellung je Benutzer über die bestehende API, keine AppSetting/ServiceSetting nach ADR 0020.)

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine. Es wird nichts neu geschrieben; die Buchung (`StockMovement` + `PurchaseOrderReceipt`, Audit `Receive.cs:140-164`) bleibt unverändert.
Der vorgeschlagene Platz landet wie jeder gewählte in `StockMovement.StorageLocationId`; Abweichung vom Sage-Platz bleibt über
`PurchaseOrderReceipt.SagePlatzId` nachvollziehbar (`Receive.cs:156`). Die Spalteneinstellungen speichert die bestehende
`/api/user-view-preferences`-Schnittstelle wie für alle anderen Ansichten; daran ändert sich nichts.

## Rollen und Zugriff

Unverändert: Klasse `[RequireStockReadAccess]` + Feature-Gate `[RequireWareneingangBestellungAktiv]` (`PurchaseOrdersController.cs:17-18`),
Receive GET/POST `[RequireStockAccess]` (`Receive.cs:14, 43`). Keine neue Rolle, kein neuer Filter.

## Listen-View-Pattern

Nicht anwendbar: Die Positionstabelle ist eine kleine, auf eine Bestellung vorgefilterte Eingabemaske ohne Pagination (Kommentar
`Receive.cshtml:5-6`, ADR 0005 Ausnahme). F2 ändert nur das Aussehen, keine neue Tabellenansicht. F2b ergänzt nur das Zahnrad
(Spaltenauswahl), keinen Spaltenfilter und keine Pagination.

## Prüfabfragen

**P-1 (Sage AKE-Test bzw. produktiv, nur SELECT, Mensch) – beginnt die Belegnummer je Belegjahr neu?** Spaltennamen aus den SData-Beschreibungen
`dtsEKBelegMst`/`eptEKBeleg` ([[sage-sdata-realtimedata]] Z. 102, [[sage-sdata-api-vertraege]] Z. 47) abgeleitet, vor dem Lauf in
`KHKEKBelege` gegenprüfen:
```sql
SELECT Belegjahr, Belegkennzeichen, COUNT(*) AS Belege, MIN(Belegnummer) AS VonNr, MAX(Belegnummer) AS BisNr
FROM dbo.KHKEKBelege
WHERE Mandant = 1 AND Belegkennzeichen IN ('EBB','EBZ') AND Belegjahr >= 2024
GROUP BY Belegjahr, Belegkennzeichen
ORDER BY Belegjahr, Belegkennzeichen;
```
Überlappende Bereiche zwischen zwei Jahren = gleiche Nummer in zwei Jahren möglich → Fall „mehrere Treffer“ kommt vor (Lösung bleibt gleich).

**P-2 (Sage, nur SELECT, Mensch) – Positionsplatz vs. Hauptlagerplatz des Artikels** (View-Name aus P-W1 der v1.84-Spec Z. 295-296; ob die
View eine Spalte `Mandant` hat, ist unbelegt – daher fest `a.Mandant = 1`):
```sql
SELECT COUNT(*) AS Positionen,
       SUM(CASE WHEN ISNULL(p.PlatzID, 0) = 0 THEN 1 ELSE 0 END)                                   AS OhnePositionsplatz,
       SUM(CASE WHEN ISNULL(p.PlatzID, 0) = 0 AND ISNULL(a.PlatzID, 0) > 0 THEN 1 ELSE 0 END)       AS OhnePositionsplatz_MitHauptplatz,
       SUM(CASE WHEN p.PlatzID > 0 AND p.PlatzID = a.PlatzID THEN 1 ELSE 0 END)                    AS GleichHauptplatz,
       SUM(CASE WHEN p.PlatzID > 0 AND a.PlatzID > 0 AND p.PlatzID <> a.PlatzID THEN 1 ELSE 0 END) AS AbweichendVomHauptplatz
FROM dbo.vwDPSBS_CWS_Mde_EinkaufpositionenInfo p
LEFT JOIN dbo.KHKArtikel a ON a.Artikelnummer = p.Artikelnummer AND a.Mandant = 1
WHERE p.Belegkennzeichen IN ('EBB','EBZ');
```

**P-3 (WMS-DB AKE-DEV, nur SELECT, Dev in E0 oder Mensch) – wie viele offene Positionen bekämen einen Vorschlag?**
```sql
SELECT COUNT(*) AS Positionen,
       SUM(CASE WHEN z.Id IS NULL OR z.IsActive = 0 OR z.IstBuchbar = 0 OR z.IsPickingTransport = 1 THEN 1 ELSE 0 END) AS HeuteOhneVorbelegung,
       SUM(CASE WHEN (z.Id IS NULL OR z.IsActive = 0 OR z.IstBuchbar = 0 OR z.IsPickingTransport = 1)
                 AND h.Id IS NOT NULL AND h.IsActive = 1 AND h.IstBuchbar = 1 AND h.IsPickingTransport = 0 THEN 1 ELSE 0 END) AS KuenftigMitHauptplatz,
       SUM(CASE WHEN l.SagePlatzId IS NULL THEN 1 ELSE 0 END) AS OhneSagePlatz
FROM dbo.SagePurchaseOrderLines l
LEFT JOIN dbo.StorageLocations z ON z.Id = l.StorageLocationId
LEFT JOIN dbo.Articles a ON a.Id = l.ArticleId
LEFT JOIN dbo.StorageLocations h ON h.Id = a.PrimaryStorageLocationId
WHERE l.ArticleId IS NOT NULL AND l.EinheitAbweichend = 0;
```
(Ohne Sage-WE-Regel; mit `SageWareneingangBelegAktiv` zusätzlich `h.SageBuchungErlaubt = 1 AND h.SageLagerplatzId IS NOT NULL AND
h.SageLagerkennung IS NOT NULL` – Spalten nach `SageWareneingang.cs:39-40`.)

## Akzeptanzkriterien

1. Eingabe `167578` (nur Ziffern) öffnet die offene Bestellung `2027-167578`, wenn genau eine Bestellung im Cache auf `-167578` endet.
2. Derselbe Ablauf per Hardware-Scanner (Scan `167578` ins Feld) verhält sich wie AK 1.
3. Eingabe `2027-167578` öffnet wie bisher exakt; `27-167578` oder `x167578` werden exakt gesucht und melden „nicht gefunden“.
4. Endet keine Bestellung auf `-<Eingabe>`: Meldung „Bestellung „…“ nicht gefunden …“ wie bisher, Seite leer.
5. Enden zwei Bestellungen (verschiedene `BelId`) auf `-<Eingabe>`: Warnmeldung nennt beide Belegnummern; es öffnet sich keine Bestellung.
6. `1675` öffnet **nicht** `2027-167578` (Endungsvergleich nur hinter dem Bindestrich).
7. Label und Platzhalter des Feldes nennen „mit oder ohne Jahr“ bzw. „z. B. 168975 oder 2027-168975“.
8. Die Positionstabelle ist gestreift und hebt die Zeile unter dem Mauszeiger hervor.
9. Steht der Fokus in Ist-Menge, Ziel-Lagerplatz, Überlieferung oder Positionsmemo einer Zeile, ist genau diese Zeile mit Hintergrund **und**
   Balken/Umriss markiert; Tab in die nächste Zeile verschiebt die Markierung; Fokus außerhalb der Tabelle = keine Markierung.
10. Eine Zeile mit `table-danger` (nach fehlgeschlagenem Buchen) bleibt in allen Zuständen (Streifen, Hover, aktiv) erkennbar rot; der
    Fehlertext steht weiterhin darin.
11. Kontrast: Fließ- und Hinweistexte ≥ 4,5:1, Balken/Umriss ≥ 3:1 gegen den Zeilenhintergrund, gemessen in allen Zuständen aus AK 8-10.
12. Position mit buchbarem Sage-Platz: dieser bleibt vorbelegt, kein Hauptlagerplatz-Hinweis.
13. Position ohne Sage-Platz, Artikel mit buchbarem Hauptlagerplatz: Hauptlagerplatz vorbelegt, Hinweis „Vorschlag: Hauptlagerplatz des Artikels“.
14. Position, deren Sage-Platz nicht im WMS ist, Artikel mit buchbarem Hauptlagerplatz: Hauptlagerplatz vorbelegt, beide Hinweise sichtbar.
15. Fall (c) (Sage-Platz zugeordnet, aber im WMS nicht buchbar/inaktiv), Artikel mit auswählbarem Hauptlagerplatz: Hauptlagerplatz vorbelegt;
    der Hinweis „Sage-Platz … ist im WMS nicht buchbar“ bleibt zusätzlich zu „Vorschlag: Hauptlagerplatz des Artikels“ sichtbar.
16. Hauptlagerplatz vorhanden, aber nicht buchbar/inaktiv: „— wählen —“ und Hinweis „Hauptlagerplatz <Code> ist im WMS nicht buchbar – bitte
    Ziel wählen“.
17. `SageWareneingangBelegAktiv` an und Hauptlagerplatz ohne Sage-Buchung: kein Vorschlag, Hinweis „… hat keine Sage-Buchung – bitte Sage-Platz
    wählen“.
18. Position ohne Sage-Platz und ohne Hauptlagerplatz: „— wählen —“ mit Hinweis „Kein Sage-Platz an der Position – bitte Ziel wählen“.
19. Nach einem fehlgeschlagenen POST steht der vom Benutzer gewählte Platz weiter im Select (kein Überschreiben durch den Vorschlag).
20. Buchen mit vorgeschlagenem Hauptlagerplatz erzeugt eine Einbuchung auf genau diesen Platz; `PurchaseOrderReceipt.SagePlatzId` = Sage-Platz
    der Position (oder leer).
21. Filter „Bestellung“ der Liste „Offene Bestellungen“ findet mit `167578` weiterhin `2027-167578` (Regression, unverändert).
22. Über der Positionstabelle steht rechts das Zahnrad „Spalten konfigurieren“; es öffnet das bekannte Spalten-Panel. Kein CSV-Knopf, keine
    Filterzeile, kein Umsortieren.
23. Im Panel lassen sich bestellt, offen (Sage), im WMS erfasst und Positionsmemo ausblenden; Pos., Artikel, Rest, Einheit, Ziel-Lagerplatz,
    Ist-Menge, Überlieferung und Status sind gesperrt (Häkchen nicht abwählbar, Kontextmenü ohne „ausblenden“).
24. Spaltenbreiten lassen sich ziehen; Sichtbarkeit und Breiten bleiben nach Neuladen und auf einer anderen Bestellung erhalten (je Benutzer);
    „Zurücksetzen“ stellt alle Spalten wieder her.
25. Mit ausgeblendeten Spalten (auch Positionsmemo mit eingetragenem Wert) bucht „Buchen“ jede Position mit ihrer eigenen Menge, ihrem Platz und
    ihrem Memo – keine Verschiebung zwischen Zeilen; Fehlerzeilen (`table-danger`, Fehlertext in Status) erscheinen weiterhin.
26. Streifen, Hover und Fokus-Markierung (AK 8-11) funktionieren unverändert mit ausgeblendeten Spalten; der Fokus-Balken bleibt sichtbar.
27. Der Hinweis „Rest/Teil/Überlieferung“ unter der Ist-Menge (Skript `Receive.cshtml:269-280`) reagiert mit ausgeblendeten Spalten wie zuvor.
28. Die Ansicht „Wareneingang zu Bestellung – Positionen“ erscheint in der Benutzerverwaltung unter den gespeicherten Ansichten, sobald ein
    Benutzer sie angepasst hat.

## Test-Szenarien

Automatisiert (xUnit, EF-InMemory/Moq):
- Repository: Ziffern-Eingabe → Endungstreffer; mit `-` → exakt; `1675` trifft `…-167578` nicht; zwei `BelId` → zwei Treffer mit Belegnummern.
- `PurchaseOrdersReceiveTests`: Redirect bei einem Treffer; Warnmeldung mit beiden Belegnummern bei zwei; Vorbelegung Regeln 1-3 inkl.
  Sage-WE-Fall und POST-Erhalt der Eingabe (AK 12-20).
- View-/Quelltext-Guard: Tabelle trägt `table-striped table-hover` und die Fokus-Klasse; keine Hintergrundfarbe überschreibt `table-danger`.
- Spaltenauswahl: `ViewKeyDriftGuardTests` deckt `Receive.cshtml` automatisch ab (Theorie über alle Views mit `#view-config`); dazu ein Fact
  analog `OffeneBestellungen_DreiQuellenGleich_UndToolbarHost` (`<th data-col-key>` = `#column-config` = `ColumnDefinitions.PurchaseOrderReceive`,
  `data-table-toolbar` vorhanden, kein `filterable-table`) und ein Test, dass genau die acht Keys aus F2b `Locked` sind.

Manuell – `docs/TESTSZENARIEN.md`: neues Kapitel **TS-177** „Wareneingang zu Bestellung: Bestellnummer ohne Jahr, aktive Zeile,
Spaltenanpassung, Hauptlagerplatz-Vorschlag“ (nächste freie Nummer; TS-175 ist in [[2026-11-23-bugfix-dashboard-symbole-nachschaerfen-spec]], TS-176 in
[[2026-11-23-dashboard-favoriten-reiter-spec]] reserviert – der Dev prüft beim Bau neu) plus Verweiszeile im Kopf von TS-116 („Seit v1.99.x: … siehe TS-177“). Skizze:

| Nr | Schritte | Erwartung |
|---|---|---|
| 177.1 | Benutzer S (`stock`), AKE-Test: Bestellnummer einer offenen Bestellung ohne Jahr eintippen, Öffnen | Bestellung öffnet sich (AK 1). |
| 177.2 | Dieselbe Nummer per Hardware-Scanner ins Feld scannen | wie 177.1 (AK 2). |
| 177.3 | Negativ: `2027-<Nr>` (exakt), unbekannte Ziffernfolge, Teilnummer (`1675`) | exakt öffnet; sonst „nicht gefunden“ (AK 3, 4, 6). |
| 177.4 | Negativ: Ziffernfolge, die zu zwei offenen Bestellungen passt (P-1 bzw. Testdatum im DEV-Cache) | Meldung nennt beide Belegnummern, nichts geöffnet (AK 5). |
| 177.5 | Bestellung mit ≥ 4 Positionen; Maus über die Zeilen; Tab durch Ist-Menge/Platz/Memo mehrerer Zeilen; Sichtprüfung am Terminal | Streifen, Hover, Markierung wandert mit dem Fokus, Balken sichtbar (AK 8, 9). |
| 177.6 | Ungültige Menge in einer Zeile buchen (Fehlerzeile), dann in diese Zeile tabben und mit der Maus darüber | Zeile bleibt rot, Fehlertext lesbar, Markierung zusätzlich sichtbar (AK 10, 11). |
| 177.7 | Position ohne Sage-Platz mit buchbarem Hauptlagerplatz; Position mit Sage-Platz „nicht im WMS“; Position mit nicht buchbarem Hauptlagerplatz | Vorbelegung und Hinweise nach AK 13, 14, 16, 18. |
| 177.8 | `SageWareneingangBelegAktiv` an (nur Anzeige, nicht buchen): Position mit Hauptlagerplatz ohne Sage-Buchung | kein Vorschlag, Hinweis nach AK 17. |
| 177.9 | Mit vorgeschlagenem Hauptlagerplatz eine Teilmenge buchen (Sage-WE **aus**) | Einbuchung auf den Hauptlagerplatz, Bewegungshistorie; Receipt mit `SagePlatzId` der Position (AK 20). |
| 177.10 | Offene Bestellungen, Filter „Bestellung“ = Nummer ohne Jahr | Treffer wie bisher (AK 21). |
| 177.11 | Bestellung öffnen, Zahnrad rechts über der Tabelle anklicken; im Panel alle Häkchen probieren, Rechtsklick auf Überschriften | Panel öffnet; vier Spalten ausblendbar, acht gesperrt; kein CSV-Knopf, keine Filterzeile (AK 22, 23). |
| 177.12 | bestellt + Positionsmemo ausblenden, Breite von Artikel ziehen, Seite neu laden, andere Bestellung öffnen, dann „Zurücksetzen“ | Einstellung bleibt erhalten, gilt für jede Bestellung; Zurücksetzen zeigt alle Spalten (AK 24). |
| 177.13 | Mit ausgeblendeten Spalten: in zwei Zeilen unterschiedliche Mengen/Plätze eingeben (vorher Memo eintragen, dann Memo ausblenden), eine Zeile mit ungültiger Menge; Buchen. Danach erneut, nur gültige Zeilen | Erst: Fehlerzeile rot mit Text, Eingaben der anderen Zeile stehen unverändert. Dann: jede Position mit eigener Menge/Platz/Memo gebucht (Bewegungshistorie) (AK 25). |
| 177.14 | Mit ausgeblendeten Spalten durch die Zeilen tabben, Maus darüber; Menge über/unter Rest eingeben | Streifen/Hover/Fokus-Balken wie 177.5; Teil-/Überlieferungs-Hinweis wie bisher (AK 26, 27). |
| 177.15 | Admin: Benutzerverwaltung des Benutzers aus 177.12 öffnen | Gespeicherte Ansicht „Wareneingang zu Bestellung – Positionen“ gelistet (AK 28). |

`secondbrain/tests/testszenarien-index.md` nachziehen.

## Etappen (nur bei epic: true)

Nicht anwendbar (epic: false). Empfohlene Commit-Reihenfolge innerhalb einer Spec: F1, F3, F2b, F2 (F2 zuletzt wegen Sichtprüfung; der
Fokus-Balken wird dann schon mit Zahnrad und ausgeblendeten Spalten geprüft).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

> [!note] Vom QA-Lauf 2026-11-23 bestätigt – siehe „QA-Nachweis → Deploy (final)“ am Ende (Dienst nur Versionsnummer, Paket = Bündel v1.99.53).

- **Web-App:** ja (View, Controller, Repository, `ColumnDefinitions`, CSS, Hilfe/Changelog, AppVersion).
- **Hilfe/Doku:** Hilfeseite zum Wareneingang (`Views/Help`) und `docs/anwender` Kapitel Wareneingang: Eingabe ohne Jahr und Mehrdeutigkeits-
  Meldung, Fokus-Markierung, Zahnrad mit den vier ausblendbaren Spalten und dem Hinweis, dass Pflichtspalten gesperrt sind, Hauptlagerplatz-
  Vorschlag mit seinen Hinweistexten; Fotos per Playwright gegen DEV AKE (nur ansehen, nicht buchen). Anwender-Changelog
  `Views/Help/Changelog.cshtml`.
- **Service:** nein (Abgleich unverändert; nur Versionsgleichstand in `IDEALAKEWMSService/AppVersion.cs` nach Hausregel).
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023): keine.
- **Version:** vorgesehen **1.99.52** (Stand 2026-11-23: Worktree 1.99.47; 1.99.48–1.99.50 in anderen Entwürfen, 1.99.51 an
  [[2026-11-23-dashboard-favoriten-reiter-spec]] vergeben); der Dev prüft beim Bau neu.
- **Nummern:** keine SQL-Nummer, kein ADR, kein neuer Fallstrick-Eintrag nötig (falls der Bau doch einen braucht: Fallstrick Nr. 61, Nr. 60 ist
  von der Favoriten-Spec vorgesehen).
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
- **Standort:** DEV AKE (IDEAL: Feature aus, nur Regressions-Blick, dass die Seite dort weiter 404 liefert).

## Offene Rueckfragen

> [!note] Rework 2026-11-23
> Alle vier Fragen sind in den Freigabe-Antworten beantwortet und im Body eingearbeitet. Die *Empfehlung* zu Frage 1 („kein Zahnrad
> nachrüsten“) ist **überholt** – Gerald hat abweichend entschieden (Zahnrad, F2b). Fragen und Empfehlungen bleiben als Nachweis stehen.

1. **Was meint „das Zahnrad“?** Auf der Seite „Wareneingang zu Bestellung“ gibt es kein Zahnrad (`Receive.cshtml` bindet `column-preferences.js`
   nicht ein); auf „Offene Bestellungen“ schon (TS-116.10). Gemeint ist vermutlich die **Positionstabelle der Wareneingangs-Seite**.
   *Empfehlung:* F2 gilt für die Positionstabelle der Receive-Seite; dort **kein** Zahnrad nachrüsten (kleine Eingabemaske, alle Spalten nötig).
   Falls die Liste „Offene Bestellungen“ gemeint ist: dort fehlt nur `table-hover` (gestreift ist sie schon) – eine Zeile, ohne Fokus-Markierung.
2. **Bestellnummer ohne Jahr – Suche statt Jahr ergänzen?** Das Belegjahr ist Sages Geschäftsjahr (2027 im November 2026), das WMS kennt den
   Wechsel nicht. *Empfehlung:* kein Jahr ergänzen, sondern reine Ziffern gegen die Endung „-<Nummer>“ aller offenen Bestellungen suchen;
   mehrere Treffer = Meldung mit allen Belegnummern, nie automatisch die neueste. Alternative (nicht empfohlen): Jahr aus einer neuen
   Einstellung „aktuelles Belegjahr“ – braucht jährliche Pflege und findet ältere offene Bestellungen nicht.
3. **Hauptlagerplatz-Vorschlag bei eingeschaltetem Sage-Wareneingang:** nur vorschlagen, wenn der Hauptlagerplatz ein Sage-Platz mit
   Sage-Buchung ist? *Empfehlung:* ja – sonst wird der Vorschlag beim Buchen abgewiesen (`Receive.cs:84`); stattdessen Hinweis
   „Hauptlagerplatz … hat keine Sage-Buchung – bitte Sage-Platz wählen“.
4. **Fall „Sage-Platz zugeordnet, aber im WMS nicht buchbar“ (z. B. HR;0;0;0 vor der Stammdatenänderung):** auch dann den Hauptlagerplatz
   vorschlagen? *Empfehlung:* ja; der Hinweis „Sage-Platz … ist im WMS nicht buchbar“ bleibt stehen, damit die Abweichung sichtbar ist. Der
   Backlog formuliert „wenn kein Sage-Platz zur Position passt“ – ein nicht wählbarer Platz passt nicht.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

<!--
  Von der Koordination eingetragen, wortgetreu, je Antwort:
  <Nr>. <Name>, <yyyy-mm-dd>, [<Jira-Schluessel> Kommentar](<Link>): "<Antwort>"
-->

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Was meint „das Zahnrad“ – Positionstabelle der Receive-Seite, kein Zahnrad nachrüsten? → **nein – Abweichung: Positionstabelle der Receive-Seite bekommt die Spaltenanpassung (Zahnrad): Spalten ein-/ausblenden und Breite anpassen, über das bestehende column-preferences.js (Gerald 2026-11-23)**
2. Bestellnummer ohne Jahr per Endungssuche, mehrere Treffer = Meldung mit Liste? → **ja**
3. Hauptlagerplatz bei Sage-WE nur, wenn Sage-Platz mit Sage-Buchung? → **ja**
4. Hauptlagerplatz auch bei zugeordnetem, aber nicht buchbarem Sage-Platz? → **ja, mit Hinweis. Ausblick Gerald: künftig werden alle Sage-Plätze im WMS auf buchbar gestellt – der Fall wird dann selten, Hinweis bleibt trotzdem**

> [!note] Bestätigung Gerald 2026-11-23: „im Wareneingang immer den Hauptplatz vorschlagen“ – Hauptlagerplatz ohne Bestandsbedingung (nicht die Kommissionier-Regel); Vorrang Sage-Platz der Position bleibt wie in F3.

## QA-Nachweis (qa-agent, 2026-11-23)

Bündel-QA im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, Bereich `0b6fff36..a93bd910`
(vier Specs v1.99.50–v1.99.53 aus [[2026-11-23-goal-lauf-dashboard-we-lagerbestellung]]). Die QA prüft je Spec den eigenen Commit-Bereich, Build und Tests decken den ganzen Worktree ab.

### 1. Build und Tests

- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 30 Warnungen (alle bekannt, u. a. CS8620 in `BfAuftragWorkflowTests`, NU1903 Pkcs).
- `dotnet test` vor dem QA-Fix: IdealAkeWms.Tests 3472 bestanden / 1 übersprungen / 0 Fehler; IDEALAKEWMSService.Tests 790 / 0 / 0.
- Nach dem QA-Fix `a93bd910`: IdealAkeWms.Tests **3473 bestanden / 1 übersprungen / 0 Fehler**, IDEALAKEWMSService.Tests **790 / 0 / 0**.

### 6. DEV-Deploy

- `powershell -ExecutionPolicy Bypass -File D:\Dev\scripts\dev-deploy.ps1 -Standort AKE|IDEAL -Quelle <worktree>` in einer erhöhten Shell, Web und Dienst,
  zweimal (nach Build-Grün `f7b26ebc`, nach dem QA-Fix `a93bd910`). Ausgabe jeweils „HTTP 200, Fusszeile v1.99.53 … Fertig: AKE v1.99.53“ bzw. „IDEAL v1.99.53“, Exit 0.
- AKE http://localhost:8081 und IDEAL http://localhost:8082 zeigen in der Fußzeile **v1.99.53** = `AppVersion` des Branches.
- Beide DEV-DBs: letzte Migration `20261123124036_AddStockMovementWarehouseRequisitionItem`, Spalte `StockMovements.WarehouseRequisitionItemId` vorhanden.
- DEV-Sicherheit geprüft: ServiceSetting `SageLagerbuchungAktiv` = false auf AKE_WMS_DEV und IDEAL_WMS_DEV, kein `Sync:*`, keine Mail eingeschaltet.
  `SData:BaseUrl` zeigt auf AKE DEV auf sagetest01, auf IDEAL DEV auf idealsage2. Deshalb wurde keine Sage-Übergabe ausgelöst.

### 7. Publish

`<worktree>\publish\` geleert und neu erstellt: `IDEALAKEWMSWeb` + `IDEALAKEWMSWebService` (`dotnet publish -c Release`, beide Exit 0), `SQL\` → `publish\SQL\`
(inkl. `166_…`), `scripts\deploy\` → `publish\deploy\` (inkl. `Update-IdealAkeWms.ps1`), `publish-info.txt`: Version 1.99.53, Commit
`a93bd91039e22bae194422e11e253d1cd6cbab78`, Branch, vier Spec-Slugs, Erstellt 2026-11-23 14:12, Komponenten IDEALAKEWMSWeb + IDEALAKEWMSWebService.
Paket-ZIP/Shares macht der Koordinator.

### Bündel-weite geänderte Bestandstests (Punkt 4)

| Test | Einordnung |
|---|---|
| `PickingControllerTests` | nur Typ: Quell-Vorschlag nach `QuellLagerplatzVorschlag.Waehle` verschoben (Lagerbestellung) |
| `PurchaseOrdersControllerTests`, `SageWareneingaengeListeTests` | nur Typ: Ctor um `IArticleRepository` (WE) |
| `PurchaseOrdersReceiveTests` | Ctor + erwartete Meldung bewusst geändert (WE F1/AK 5, Mehrdeutigkeit) |
| `WarehousePickingControllerTests` | Ctor + `GetByIdAsync`-Setup wegen Status-Guard F4 (Lagerbestellung AK 16, bewusst) |
| `NachlaufManifestDriftTests` | + SQL/166 (Lagerbestellung) |
| `CsvExportCoverageTests` | bewusst: Sortiertext `data-filter-text` (Lagerbestellung Abweichung 6) |
| `AppSettingDefinitionsTests` | Anzahl 67 → 68 (neue Einstellung Lagerbestellung) |

Keine Änderung der Klasse „sonstiges“: Keine Fixture wurde angepasst, nur damit ein Test grün wird.

### Abweichungen aus der Umsetzung (Lauf-Notiz, von QA geprüft und akzeptiert)

1. Tragen zwei Belege genau dieselbe Belegnummer, nennt die Meldung beide Nummern und verweist auf „über Offene Bestellungen wählen“. Das ist strenger als AK 5 und richtig.
2. Der Fokus-Balken nutzt `--bs-primary-text-emphasis` statt `--bs-primary`, weil `--bs-primary` auf aktivem `table-danger` nur etwa 2,7:1 erreicht (AK 11).
3. Die Hinweise „nicht buchbar“ und „ohne Sage-Buchung“ erscheinen auch nach einem POST-Fehler (AK 19: die Auswahl bleibt, der Hinweis bleibt sichtbar).
4. Fall (a) bedeutet `SagePlatzId` null **und** `StorageLocationId` null.

### 2. Code-Review (Commits `e51e80c9`, `9937d357`, selbst durchgeführt)

Geprüft: `PurchaseOrdersController.Receive.cs` (Endungssuche nur bei reinen Ziffern über `EndsWith("-"+key)`, sonst exakt; Mehrdeutigkeit über
`GetBelIdsByBelegnummerAsync` → Liste (BelId, Belegnummer); `ZielVorschlaegeAsync` mit Sage-Platz-Vorrang, bei Sage-WE nur `IstSageWePlatz`; ein POST-Fehler
übernimmt die Benutzerauswahl), `ArticleRepository.GetPrimaryStorageLocationIdsAsync` (ein Batch-Read), `Receive.cshtml` (`data-col-key` an allen `<th>`,
8 gesperrte Spalten in `ColumnDefinitions.PurchaseOrderReceive`; das Ausblenden geschieht per CSS, die Eingabefelder bleiben im DOM. Damit gibt es keine
Positionsverschiebung beim Binden, AK 25), `site.css` (`:focus-within` mit `--bs-table-bg-state` und Inset-Box-Shadow, `table-danger` bleibt erhalten).
Kein `hidden` mit `d-*`, keine Werte in `<script>`. Die Pflichtprüfung beim Buchen liegt weiter serverseitig.
**Befunde: keine.** Hinweis: Die Übersetzung von `EndsWith` in SQL (LIKE `%-167578`) ist nur auf echtem SQL Server belegbar. Auf DEV gegen SQL Server
funktioniert sie (siehe 6c).

### 3. Nachweisart je AK

| AK | Nachweis |
|---|---|
| 1, 3, 4, 5, 6 | automatisch (`PurchaseOrdersReceiveBedienungTests`, `PurchaseOrdersReceiveTests`) **und** DEV AKE für 1, 4/6 und 5 (SQL-Übersetzung) |
| 2 | Manual-UAT (Hardware-Scanner) |
| 7 | DEV-Sicht (Label „mit oder ohne Jahr“, Platzhalter „z. B. 168975 oder 2027-168975“) |
| 8, 9, 10, 11, 26 | Manual-UAT (Sichtprüfung). Kontrast berechnet: Balken `#052c65` auf rot-aktiv etwa 8,1:1, Text `#212529` etwa 9,3:1, Sekundärtext etwa 5,1:1 |
| 12–19 | automatisch (`PurchaseOrdersReceiveBedienungTests`); DEV AKE für 13, 15/16 (siehe 6c) |
| 20 | automatisch (InMemory); Buchung auf DEV nicht ausgeführt → Manual-UAT |
| 21 | unverändert (Bestandscode), Manual-UAT |
| 22 | DEV: Knopf „Spalten konfigurieren“ vorhanden; `ViewKeyDriftGuardTests.WareneingangPositionen_*` automatisch |
| 23, 24, 25, 27, 28 | Manual-UAT (Browser-JS, echtes Model-Binding) |

### 5. Checkliste

Migration/SQL: entfällt. Version 1.99.52 in beiden `AppVersion.cs`, Changelog v1.99.52, TS-177 in `docs/TESTSZENARIEN.md`, Testindex Zeile 177 vorhanden,
Manifest: keine Einträge, richtig so. Einstellungen (ADR 0020): keine. Brain-Nachzug (fallstricke §61, controller.md, feature-map, Changelog) macht der Koordinator.

### 6c. Testlauf auf DEV AKE (8081)

| Schritt | Ergebnis | Status |
|---|---|---|
| `168949` (nur Ziffern) | öffnet `2027-168949` (`?belId=363165`) | OK |
| `1689` | „Bestellung "1689" nicht gefunden …“, Seite leer (keine Teilübereinstimmung) | OK |
| Mehrdeutigkeit: Testzeile `2026-168955` (BelId 990001) per SQL in `SagePurchaseOrderLines`, Eingabe `168955` | Meldung „"168955" passt zu 2 Bestellungen: 2026-168955, 2027-168955 …“, es öffnet sich keine Bestellung; Testzeile danach gelöscht | OK |
| `2027-168955` Pos 1/2/4 (Sage-Platz und Hauptlagerplatz im WMS nicht buchbar) | „— wählen —“ + „Sage-Platz … ist im WMS nicht buchbar“ + „Hauptlagerplatz … ist im WMS nicht buchbar“ | OK |
| Pos 3 (ohne Sage-Platz), Hauptlagerplatz vorübergehend auf buchbaren Platz `1;1;1` gesetzt | `1;1;1` vorbelegt, „Vorschlag: Hauptlagerplatz des Artikels“; danach `PrimaryStorageLocationId` wieder NULL | OK |
| Positionen mit abweichender Einheit (2027-168949) | „nicht buchbar, Einheit abweichend …“ wie bisher | OK |
| Zahnrad sichtbar | Knopf „Spalten konfigurieren“ über der Tabelle | OK |
| Fokus-Zeile, Streifen/Hover, Spalten aus-/einblenden, Breiten, Buchen | Chrome-Screenshot im Lauf eingefroren; nicht gebucht (Hinweis der Spec: nur ansehen) | NICHT MOEGLICH, Manual-UAT |
| IDEAL-Regression „404“ | IDEAL DEV zeigt die Seite, weil dort `WareneingangBestellungAktiv=true` gesetzt ist; der Gate-Code ist unverändert (`[RequireWareneingangBestellungAktiv]`) | OK, kein Befund |

### Deploy (final, aus dem echten Diff)

- Web: ja (Controller, Repository, ColumnDefinitions, ViewModel, View, CSS, Hilfe/Changelog). Dienst: ja, **nur Versionsnummer** (`e51e80c9` hebt
  `IDEALAKEWMSService/AppVersion.cs`). Migration: nein. Manifest: nichts.
- Paket: `<worktree>\publish` mit **v1.99.53** (Bündel), Commit `a93bd910`. Ablauf: Publish aus dem Worktree → Testsystem → testen → Merge.
  Nach dem Merge nur dann neu aus main publishen, wenn der Merge getestete Dateien mit parallelen main-Änderungen zusammengeführt hat.

### Manuelle Testcheckliste (offen für den Menschen)

1. TS-177 auf dem Testsystem AKE: Bestellnummer ohne Jahr eintippen und per Hardware-Scanner scannen (AK 1/2).
2. Fokus-Zeile mit Tab durch alle Eingabefelder, auch auf einer roten Fehlerzeile. Balken und Text gut lesbar, auch bei schlechtem Licht am Terminal.
3. Zahnrad: vier Spalten ausblenden, Breiten ziehen, neu laden, andere Bestellung öffnen, „Zurücksetzen“. Gesperrte Spalten lassen sich nicht abwählen.
4. Mit ausgeblendetem Positionsmemo (Wert eingetragen) und ausgeblendeten Mengen-Spalten buchen: Jede Zeile bucht ihre eigene Menge, ihren Platz und ihr Memo (AK 25).
5. Buchen mit dem vorgeschlagenen Hauptlagerplatz: Einbuchung auf genau diesen Platz, `PurchaseOrderReceipt.SagePlatzId` passt (AK 20). Mit `SageWareneingangBelegAktiv`
   nur auf dem Testsystem.
6. Prüfabfragen P-1–P-3 der Spec auf der Sage-DB.
7. Liste „Offene Bestellungen“, Filter `167578` (Regression AK 21).
