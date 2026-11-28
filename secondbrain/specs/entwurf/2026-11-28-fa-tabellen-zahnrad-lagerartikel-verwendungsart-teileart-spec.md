---
type: spec
title: FA-Tabellen-Zahnrad – Ist Lagerartikel, Verwendungsart, Teileart; FA-Struktur mit erweitertem Zahnrad
slug: 2026-11-28-fa-tabellen-zahnrad-lagerartikel-verwendungsart-teileart-spec
status: Entwurf
created: 2026-11-28
updated: 2026-11-28
source_backlog: "[[2026-11-28-fa-tabellen-zahnrad-lagerartikel-verwendungsart-teileart]]"
depends_on: "[[2026-11-26-bde-zahnrad-artikelfelder-spec]]"
task: ""
jira: "WMS-10"
worktree: ""
branch: ""
standort: beide
affected_code:
  - IdealAkeWms/Models/FaHierarchyNode.cs (neu Verwendungsart, IstLagerartikel)
  - IdealAkeWms/Data/ApplicationDbContext.cs (MaxLength Verwendungsart)
  - IdealAkeWms/Migrations/<neu> (gemeinsam mit BDE-Spec Teil B oder eigene, Rueckfrage 7)
  - IDEALAKEWMSService/Services/FaHierarchySql.cs (BuildNodeSelectSql + COL_LENGTH-Schutz)
  - IDEALAKEWMSService/Services/SageFaStrukturReader.cs (ReadNodesAsync)
  - SQL/172 bzw. SQL/174 (vorlaeufig, siehe Migrationen), SQL/00_FreshInstall.sql
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (FaHierarchyStructure, FaHierarchyKommissionierListen, FaHierarchyVormontageEinzeln, Bom, KommissionierauftragListe, ProductionOrders, PickingLeitstand; ggf. Summiert-Configs)
  - IdealAkeWms/Views/FaHierarchy/Index.cshtml, _FaHierarchyNode.cshtml
  - IdealAkeWms/Services/KommissionierListenService.cs, Views/FaHierarchyKommissionierListen/Index.cshtml (+ Print.cshtml, Summiert nach Rueckfrage 5)
  - IdealAkeWms/Services/VormontageService.cs, Views/FaHierarchyVormontage/Index.cshtml (+ Summiert nach Rueckfrage 5)
  - IdealAkeWms/Models/FaHierarchyBomItem.cs, Data/Repositories/FaHierarchyBomRepository.cs, Models/ViewModels/BomViewModels.cs, Controllers/PickingController.cs, Services/ReadOnlyBomBuilder.cs, Services/KommissionierauftragListe.cs, Views/Picking/Bom.cshtml, Views/Picking/_AuftragListeTabelle.cshtml
  - IdealAkeWms/Controllers/ProductionOrdersController.cs (StrukturFelderSetzen, FaListComputedColumnKeys, MatchFaListComputedFilter), Models/ViewModels/ProductionOrderListViewModel.cs, Views/ProductionOrders/Index.cshtml, _ProductionOrderRow.cshtml
  - IdealAkeWms/Controllers/PickingLeitstandController.cs, Views/PickingLeitstand/Index.cshtml, _PickingLeitstandRow.cshtml (nur nach Rueckfrage 1)
  - IdealAkeWms/Views/Help/Index.cshtml, Views/Help/Changelog.cshtml, docs/anwender/*, docs/TESTSZENARIEN.md
open_questions:
  - "Welche FA-Tabellen bekommen die drei Spalten? Vorschlag: FA-Struktur, Kommissionierlisten (Einzeln), Vormontage (Einzeln), Stückliste und Kommissionierauftrag-Liste; FA-Liste und Leitstand nur, wenn die Werte des Endgeräts (HauptFA-Zeile) dort nützen. (Fachbereich: Benjamin Andl-Braun)"
  - "FA-Struktur „erweiterter Umfang des Zahnrads“: Sind damit alle Positionsfelder gemeint, die das WMS schon hat, aber die FA-Struktur nicht anbietet (Artikelgruppe, Hauptlagerplatz, Bemerkung PN, Fertigungsinfo PN, Arbeitsschritte, Material, VM-Bedarf, Breite/Höhe/Tiefe, Beschaffungsartikel, EK-Bedarf, Position) plus die drei neuen? Oder welcher Bereich ist das Vorbild? (Fachbereich: Benjamin Andl-Braun)"
  - "Verwendungsart: Im Testsystem gibt es nur den Wert „VT“, und zwar genau bei Ist Lagerartikel = Ja. Ist die Verwendungsart fachlich etwas anderes als Ist Lagerartikel, und welche Werte gibt es in der Praxis? (Fachbereich: Benjamin Andl-Braun)"
  - "Teileart = das Sage-Feld Artikeltyp der Stücklistenposition (z. B. BG, FT, EP, ZK)? Die Kommissionierliste hat dafür schon eine ausblendbare Spalte „Artikeltyp“ – soll sie in „Teileart“ umbenannt werden (eine Spalte, kein Duplikat)? (Fachbereich: Benjamin Andl-Braun)"
  - "Summiert-Sichten (Kommissionierliste, Vormontage) und Ausdrucke (Kommissionierliste, Stückliste, Kommissionierauftrag): auch dort die drei Spalten (Summiert: verschiedene Werte kommagetrennt)? Vorschlag: Einzelsichten ja, Summiert und Druck erst bei Bedarf. (Fachbereich: Benjamin Andl-Braun)"
  - "Anzeige: Ist Lagerartikel als Ja/Nein, alle neuen Spalten standardmäßig ausgeblendet (nur über das Zahnrad einblendbar) – einverstanden? (Fachbereich: Benjamin Andl-Braun)"
  - "Verhältnis zur BDE-Spec 2026-11-26: Ist Lagerartikel für ALLE Tabellen (auch BDE) aus der Sage-View an der FA-Position (neues Feld FaHierarchyNode.IstLagerartikel, gemeinsame Migration mit BDE Teil B) statt aus dem Artikelmerkmal? Artikelmerkmal dann nur noch für BDE-Lagerbestell-Zeilen. Das braucht einen Nachtrag in der BDE-Spec."
  - "Schnitt: ein Epic in einem Worktree mit den Etappen E0–E6 (siehe Etappen), gebaut NACH bzw. zusammen mit der BDE-Spec im selben Bündel – einverstanden, oder Split in einzeln mergbare Teile?"
epic: true
etappen:
  - "E0 Schema+Dienst: FaHierarchyNode.Verwendungsart + IstLagerartikel aus der View (nach Rueckfrage 7)"
  - "E1 FA-Struktur: drei Spalten + erweiterter Umfang (nach Rueckfrage 2)"
  - "E2 Kommissionierlisten Einzeln (Summiert/Druck nach Rueckfrage 5)"
  - "E3 Vormontage Einzeln (Summiert nach Rueckfrage 5)"
  - "E4 Stückliste + Kommissionierauftrag-Liste"
  - "E5 FA-Liste (Strukturwurzel, nach Rueckfrage 1)"
  - "E6 Kommissionier-Leitstand (nach Rueckfrage 1)"
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

Benjamin Andl-Braun (IDEAL) will in den Tabellen rund um den Fertigungsauftrag sehen, ob ein Teil ein Lagerartikel ist, welche
Verwendungsart und welche Teileart es hat, ohne in Sage nachzuschlagen (Mail „WMS Anpassungen“, Punkt 5). Gerald erweitert das auf
**alle Tabellen, die den FA betreffen**. Außerdem soll die FA-Struktur im Zahnrad so viele Spalten anbieten wie andere Bereiche
(Punkt 6). Die BDE-Teileliste deckt schon [[2026-11-26-bde-zahnrad-artikelfelder-spec]] ab; diese Spec ist die Erweiterung auf die übrigen
FA-Tabellen und nutzt dieselbe Quelle.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope** (Vorschlag ⭐, endgültig nach Rueckfrage 1/2/5):
- FA-Struktur (`FaHierarchy/Index`): drei Spalten **plus** erweiterter Umfang (Positionsfelder, Rueckfrage 2).
- Kommissionierlisten Einzeln (`FaHierarchyKommissionierListen/Index`).
- Vormontage Einzeln (`FaHierarchyVormontage/Index`).
- Stückliste (`Picking/Bom`) und Maske „Liste“ des Kommissionierauftrags (`Picking/_AuftragListeTabelle`), nur IDEAL (BOM-Bridge).
- FA-Liste (`ProductionOrders/Index`, Werte der Strukturwurzel) und Kommissionier-Leitstand: nur wenn Rueckfrage 1 sie bestätigt.

Je Tabelle drei optionale Spalten mit identischen Col-Keys in allen Tabellen: `teileart` „Teileart“, `ist-lagerartikel`
„Ist Lagerartikel“, `verwendungsart` „Verwendungsart“ (wie die BDE-Spec), jede mit Spaltenfilter im Modus der jeweiligen Tabelle.

**Out-of-Scope**
- BDE-Teileliste (Einzeln/Summiert/FA-Sicht): bleibt in der BDE-Spec.
- AKE: keine sichtbare Änderung. Die Spalten erscheinen nur im hierarchischen Modus (`ProduktionsauftragHierarchisch`), wie die
  v1.99.82-Wurzelfelder der FA-Liste (`ColumnDefinitions.cs:18-21,59-60`). Die AKE-Stückliste (`BomItem` per `SqlQueryRaw` auf die AKE-View)
  bekommt keine neue Eigenschaft, sonst bricht das Mapping (`FaHierarchyBomItem.cs:6-8`).
- Weitere FA-nahe Listen (FA-Abarbeitungsliste `FaWorklist`, FA-Vervollständigung `FaCompletion`, Beschichtung, Kommissionierauftrags-Übersicht
  `Picking/Index`): nicht vorgeschlagen; Rueckfrage 1 kann sie aufnehmen.
- SData-Weg der FA-Struktur (`SdataFaStrukturReader`): unverändert, bekannte Grenze wie in der BDE-Spec.
- Kein Schreiben nach Sage, keine Änderung der Sage-View.

## Fachliche Anforderungen

**Befund je Tabelle (2026-11-28, Hauptcheckout `main` = Bündel v1.99.89, nur gelesen)**

Grundlage aller IDEAL-FA-Tabellen ist der Strukturknoten `FaHierarchyNode`. Er hat `Artikeltyp` (`FaHierarchyNode.cs:47`), aber
**weder** `IstLagerartikel` **noch** `Verwendungsart` (`FaHierarchyNode.cs:15-74`). Der Import selektiert beide View-Spalten nicht
(`FaHierarchySql.cs:139-152`, `SageFaStrukturReader.cs:116-163`); im ganzen Dienst gibt es keinen Treffer auf `Verwendungsart`.
`Ist Lagerartikel` gibt es im WMS nur als Artikelmerkmal `USER_OSIstLagerartikel` (`BfArtikelFelder.cs:13`), gefüllt nur von der
Blechteile-Nachlese und nur für Lagerartikel (Befund 3 der BDE-Spec; P-5 dort: nur Wert `-1`, 1287 Artikel).

| Tabelle | View-Key | Teileart | Ist Lagerartikel | Verwendungsart | Beleg |
|---|---|---|---|---|---|
| FA-Liste | `ProductionOrders` | im Read vorhanden (Wurzelknoten wird geladen), nicht angezeigt | fehlt | fehlt | Wurzeln `ProductionOrdersController.cs:271-273`, Mapping ohne Artikeltyp `:517-542`, Spalten `ColumnDefinitions.cs:61-77` |
| FA-Struktur | `FaHierarchyStructure` | im Modell (`n.Artikeltyp`), nicht angezeigt | fehlt | fehlt | Spalten `ColumnDefinitions.cs:692-726`, Zellen `_FaHierarchyNode.cshtml:156-200` |
| Stückliste | `Bom` | im Knoten, beim Bau von `FaHierarchyBomItem` nicht übernommen | fehlt | fehlt | `FaHierarchyBomRepository.cs:174-194`, `FaHierarchyBomItem.cs:10-34`, Spalten `ColumnDefinitions.cs:260-292` |
| Kommissionierauftrag-Liste | `KommissionierauftragListe` | wie Stückliste (Summenzeile aus der ersten Stücklistenzeile) | fehlt | fehlt | `KommissionierauftragListe.cs:40-47`, `BomViewModels.cs:95-142`, `ColumnDefinitions.cs:299-326` |
| Kommissionierlisten Einzeln | `FaHierarchyKommissionierListen` | **schon da** als Spalte `artikeltyp` „Artikeltyp“, ausgeblendet | fehlt | fehlt | Getter `KommissionierListenService.cs:116`, View `Index.cshtml:83,156,184`, Druck `Print.cshtml:117,136`, `ColumnDefinitions.cs:404` |
| Kommissionierlisten Summiert | `FaHierarchyKommissionierSummiert` | fehlt | fehlt | fehlt | `KommissionierListenService.cs:131-137`, `ColumnDefinitions.cs:417-432` |
| Vormontage Einzeln | `FaHierarchyVormontageEinzeln` | im Knoten, nicht angezeigt | fehlt | fehlt | `VormontageService.cs:80-91,148-154`, `ColumnDefinitions.cs:489-510` |
| Vormontage Summiert | `FaHierarchyVormontageSummiert` | fehlt | fehlt | fehlt | `VormontageService.cs:175-177`, `ColumnDefinitions.cs:658-675` |
| Kommissionier-Leitstand | `PickingLeitstand` | fehlt (lädt keine Strukturknoten, nur Artikel-Matchcodes) | fehlt | fehlt | `PickingLeitstandController.cs:120-137,231,332`, `ColumnDefinitions.cs:91-135` |
| BDE-Teileliste | `BdeTeileliste*` | fehlt (noch nicht gebaut) | fehlt | fehlt | `BdeTeilelisteService.cs:35,313` ohne die Felder; Umsetzung in der BDE-Spec |

„Anscheinend vorhanden“ (Backlog) stimmt damit nur für **Teileart** am Knoten; **Ist Lagerartikel** und **Verwendungsart** liegen in keiner
FA-Tabelle vor.

**Quelle der Werte** (Wiederverwendung der BDE-Spec, deren Prüfergebnisse P-1 bis P-5 vom 2026-11-26):
- Die produktiv konfigurierte View heißt per Default `[vw_IDEAL_AKE_WMS_FAListe]` (`ServiceSettingDefinitions.cs:298`). Sie hat laut P-2
  (IDEAL-Test) die Spalten `Verwendungsart` **und** `IstLagerartikel`.
- P-3: `Verwendungsart = 'VT'` genau dann, wenn `IstLagerartikel = -1` (606/606), sonst NULL (699/699) → Rueckfrage 3.
- P-4: Ist Lagerartikel Position vs. Artikel: 0 Abweichungen; Artikeltyp: 7 von 1305 abweichend, alle mit Position = NULL.
- Folge ⭐: **eine** Quelle für alle Tabellen = FA-Position aus der View, gespeichert am Knoten. Teileart = `FaHierarchyNode.Artikeltyp`
  (schon da). Das weicht für Ist Lagerartikel vom BDE-Entwurf (Teil A: Artikelmerkmal) ab → Rueckfrage 7.

**Anforderungen** (Annahmen nach den Empfehlungen ⭐; die Freigabe kann sie ändern):
1. Jede In-Scope-Tabelle bietet im Zahnrad „Teileart“, „Ist Lagerartikel“, „Verwendungsart“ an, per Default ausgeblendet.
2. Werte je Zeile aus dem zugehörigen Strukturknoten: Teileart = `Artikeltyp`, Ist Lagerartikel = „Ja“/„Nein“ aus `IstLagerartikel`
   (`null` → „–“), Verwendungsart = `Verwendungsart`. Leere Werte zeigen „–“. Getter = Zelle = Filter (fallstricke §26, ADR 0005).
3. Zuordnung Zeile → Knoten: FA-Struktur, Kommissionierliste, Vormontage = die Zeile ist der Knoten; Stückliste/Kommissionierauftrag = Knoten der
   Stücklistenzeile; FA-Liste = Strukturwurzel (wie v1.99.82); Leitstand = Knoten mit `SubFA == SubOrderNumber` (Muster `BdeFaAngebot.cs:116-119`),
   HauptFA-Zeile = Wurzel.
4. FA-Struktur, erweiterter Umfang (Annahme bis Rueckfrage 2): zusätzlich alle Knotenfelder, die schon im Modell stehen und dort fehlen –
   Position, Artikelgruppe, Hauptlagerplatz, Bemerkung PN, Fertigungsinfo PN, Arbeitsschritte, Material, VM-Bedarf, Breite, Höhe, Tiefe,
   Beschaffungsartikel (Ja/Nein), EK-Bedarf (Ja/Nein). Alle ausgeblendet, alle mit Baum-Spaltenfilter. Keys nach Möglichkeit wie in der FA-Liste
   (`artikelgruppe`, `hauptlagerplatz`, `bemerkung`, `fertigungsinfo`, `arbeitsschritte`, `material`, `ColumnDefinitions.cs:70-77`).
5. Kommissionierliste: Die vorhandene Spalte `artikeltyp` ist fachlich die Teileart. Annahme ⭐: Key bleibt `artikeltyp` (gespeicherte
   Spaltenauswahl bleibt gültig), nur das Label wird „Teileart“ (Rueckfrage 4). Keine zweite Spalte.

## Technischer Loesungsentwurf

Ponytail-Leiter: Alle Tabellen hängen schon am Knoten (`FaHierarchyNode`) und an Getter-Dictionaries/`#column-config`. Neu sind nur zwei
Knotenfelder aus der View, die der Abgleich per `SetValues` ohne eigenen Code übernimmt (`FaHierarchySyncService.cs:351-373`, Befund der
BDE-Spec). Keine neue Lesung, kein Artikelmerkmal-Join je Tabelle.

**E0 – Schema + Dienst** (ersetzt bzw. erweitert BDE-Spec Teil B, Rueckfrage 7)
1. `FaHierarchyNode.Verwendungsart` (`string?`, `HasMaxLength(200)` wie `Artikeltyp`, `ApplicationDbContext.cs:1151`) und
   `FaHierarchyNode.IstLagerartikel` (`bool?`; `null` = Spalte fehlt oder Wert NULL).
2. `FaHierarchySql.BuildNodeSelectSql` (`:139-152`) selektiert `f.[Verwendungsart]` und `f.[IstLagerartikel]`. Je Spalte vorher
   `COL_LENGTH(@view, …)`; fehlt sie, `NULL AS [...]` und **eine** Warnung ins Aktivitäts-Protokoll („Sage-View ohne Spalte … – Spalte bleibt leer“).
   Ohne Schutz würde `GetOrdinal` (`SageFaStrukturReader.cs:116-129`) den ganzen Abgleich abbrechen.
3. `SageFaStrukturReader.ReadNodesAsync` liest beide; `IstLagerartikel` über eine nullbare Variante des vorhandenen `Bool`-Helfers
   (`:146,155`), Datentyp der View-Spalte vorher per P-B prüfen.
4. Erster Lauf nach Deploy: viele Knoten „geändert“, `SyncedAt` springt (einmalig, Deploy-Hinweis).

**E1 – FA-Struktur** (`Views/FaHierarchy/Index.cshtml`, `_FaHierarchyNode.cshtml`)
- Filter ist der lokale Baumfilter (`fa-hierarchy-tree.js:258,325`, liest `data-f-<key>` am `<tr>`), **nicht** `table-filter.js`
  (`Index.cshtml:164-166,260-263`). Je neue Spalte: `<th data-col-key>` in Kopf- **und** Filterzeile, `<td data-col-key>`,
  `data-f-<key>` am `<tr>` (`_FaHierarchyNode.cshtml:79-108`), Eintrag in `#column-config` (`Index.cshtml:231-257`) und
  `ColumnDefinitions.FaHierarchyStructure` (`:692-726`).
- `colspan="24"` der Kopf-/Fehlerzeilen (`_FaHierarchyNode.cshtml:207,241`) auf die neue Spaltenzahl anheben (oder aus der Spaltenzahl ableiten).
- Test `FaStrukturVarianteDTests` (Keys/Labels gleich FA-Liste) beachten.

**E2 – Kommissionierlisten**: Getter in `KommissionierListenService` (`:108-119`) um `ist-lagerartikel`, `verwendungsart` ergänzen,
Label `artikeltyp` → „Teileart“ (View `:83,184`, `ColumnDefinitions.cs:404`, Druck `Print.cshtml:117`). Server-Filter läuft schon über
die Getter. Summiert/Druck nur nach Rueckfrage 5 (Summiert: `VormontageService.FormatTexte`, Muster BDE-Spec).

**E3 – Vormontage**: `VormontageService.ColumnMap` (`:80-91`) um `teileart`, `ist-lagerartikel`, `verwendungsart`; View + `ColumnDefinitions` (`:489-510`).

**E4 – Stückliste + Kommissionierauftrag**: `FaHierarchyBomItem` bekommt `Teileart`, `IstLagerartikel`, `Verwendungsart` (nicht `BomItem`!),
gesetzt in `FaHierarchyBomRepository.cs:174-194`; `BomItemViewModel` gleichzieht an allen drei Mapping-Stellen (`PickingController.cs:468-482`,
`:613`, `ReadOnlyBomBuilder.cs:118`); `AuftragSummenzeile` übernimmt sie aus der ersten Zeile wie `Hauptlagerplatz` (`KommissionierauftragListe.cs:40`).
Spalten nur bei `Model.Hierarchical` rendern (Muster `kommissionieren`/`hauptlagerplatz`, `ColumnDefinitions.cs:278-281`). Bom ist Client-Mode
(kein `data-server-column-filter`) – bleibt so.

**E5 – FA-Liste**: `StrukturFelderSetzen` (`ProductionOrdersController.cs:517-542`) setzt die drei Werte aus `root`; Keys in
`FaListComputedColumnKeys` (`:504-511`) und `MatchFaListComputedFilter` (`:576-601`); `ProductionOrderListItem`, Spalten in
`Index.cshtml`/`_ProductionOrderRow.cshtml` nur hierarchisch (AK 20 der FA-Liste: AKE bit-identisch).

**E6 – Leitstand**: Ein Batch `GetRootsByHauptFaKeysAsync` (schon in der FA-Liste genutzt, `:272`) plus Knoten je `(HauptFA, SubFA)` wie
`BdeFaAngebot.cs:116-119`; Postfilter wie FA-Liste. Nur nach Rueckfrage 1.

Allgemein: Spaltenauswahl über `column-preferences.js` (`#column-config`, `:886`) und den Toolbar-Host `window.tableToolbarHost`
(`site.js:12`) – kein neuer Mechanismus. `ViewKeyDriftGuardTests` prüft View ↔ `ColumnDefinitions`. Skill `frontend-design` ist Pflicht
(Views ändern sich), nur Tabellenspalten, kein neuer Stil.

**Bekannte Grenze** (wie BDE-Spec): Der SData-Weg setzt die beiden neuen Felder nicht; bei `Sync:Quelle:FaStruktur = SData` leert `SetValues` sie.
`VergleichsFelder` (`FaStrukturQuelleSwitch.cs:20-51`) bleibt ohne sie. → Folgeaufgabe + Fallstrick.

**Prüfabfragen für den Menschen** (nur lesen; davon abhängige Aussagen sind ungeprüft):
- **P-A View-Name Prod IDEAL** (WMS-DB Prod): `SELECT [Value] FROM ServiceSettings WHERE [Key] = N'Sync:FaHierarchyListeViewName';`
- **P-B Spalten + Datentyp in der Prod-View** (Sage Prod IDEAL, Name aus P-A):
  `SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = N'vw_IDEAL_AKE_WMS_FAListe' AND COLUMN_NAME IN (N'Verwendungsart', N'IstLagerartikel', N'Artikeltyp');`
- **P-C Werte Verwendungsart Prod** (für Rueckfrage 3):
  `SELECT Verwendungsart, IstLagerartikel, COUNT(*) AS Anzahl FROM dbo.[vw_IDEAL_AKE_WMS_FAListe] GROUP BY Verwendungsart, IstLagerartikel ORDER BY Anzahl DESC;`
- **P-D Teileart-Werte** (für Rueckfrage 4): `SELECT Artikeltyp, COUNT(*) FROM dbo.[vw_IDEAL_AKE_WMS_FAListe] GROUP BY Artikeltyp ORDER BY 2 DESC;`

## Migrations-/SQL-Auswirkungen

- E0: EF-Migration, zwei nullable Spalten an `FaHierarchyNodes` (`Verwendungsart nvarchar(200) NULL`, `IstLagerartikel bit NULL`), nicht destruktiv.
  - Variante ⭐ (Rueckfrage 7 = gemeinsam): **eine** Migration mit der BDE-Spec Teil B, `SQL/172` (dort reserviert) um `IstLagerartikel` erweitert.
  - Sonst eigene Migration, Nummer vorläufig `SQL/174` (Stand Hauptcheckout: höchste Datei `169`; `170/171` BDE-nur-FA-Meldung, `172` BDE-Zahnrad,
    `173` ist in zwei Entwürfen doppelt reserviert, `175` Lieferverzug-Spec). Der Dev-Lauf prüft neu.
  - Je Spalte `IF COL_LENGTH('dbo.FaHierarchyNodes', '<Spalte>') IS NULL ALTER TABLE ... ADD ...` im eigenen Batch, danach separater
    `__EFMigrationsHistory`-Insert mit Guard. `SQL/00_FreshInstall.sql` an beiden Stellen (CREATE TABLE + MigrationId).
- `SQL/AgentJobs/`: nicht betroffen (kein Agent-Job schreibt `FaHierarchyNodes`, Befund BDE-Spec).
- E1–E6: keine.

## Einstellungen

keine. Keine neue oder geänderte AppSetting/ServiceSetting. Die View-Spalten hängen an der vorhandenen `Sync:FaHierarchyListeViewName`.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine. `FaHierarchyNode` ist bewusst keine `AuditableEntity` (Cache-Tabelle, Nachvollziehbarkeit über SyncLog + `SyncedAt`,
`FaHierarchyNode.cs:5-12`). Alle übrigen Änderungen sind Lesewege.

**Rollen/Zugriff:** unverändert, keine neue Aktion; die Spalten erscheinen hinter den bestehenden Filtern der jeweiligen Controller.
**Listen-View-Pattern:** keine neue Tabelle; neue Spalten folgen dem Modus der Tabelle (Server-Mode, Client-Mode Bom, Baumfilter FA-Struktur).

## Akzeptanzkriterien

1. E0: Nach einem FA-Struktur-Abgleich (View-Quelle, IDEAL) tragen `FaHierarchyNodes.Verwendungsart` und `IstLagerartikel` die Werte der View
   (Stichprobe gegen P-C).
2. E0 negativ: Fehlt eine der Spalten in der View (Test auf den SQL-Baustein), läuft der Abgleich durch, das Feld bleibt NULL, das
   Aktivitäts-Protokoll hat je fehlender Spalte genau eine Warnung.
3. Jede In-Scope-Tabelle bietet im Zahnrad „Teileart“, „Ist Lagerartikel“, „Verwendungsart“; ohne gespeicherte Auswahl sind sie ausgeblendet.
4. Werte je Zeile = Werte des zugehörigen Knotens nach Anforderung 2/3; Ist Lagerartikel zeigt „Ja“/„Nein“, ohne Wert „–“.
5. Spaltenfilter auf jede neue Spalte liefert genau die Zeilen, deren Zellentext passt (FA-Struktur: Baumfilter mit Vorfahrenpfad;
   Server-Mode-Tabellen: Pagination zählt nach dem Filter).
6. FA-Struktur: Die Felder des erweiterten Umfangs (Anforderung 4, nach Rueckfrage 2) sind wählbar und filterbar; Kopf-/Fehlerzeilen
   (`colspan`) überspannen weiter die ganze Tabelle.
7. Kommissionierliste: Die bisherige Spalte „Artikeltyp“ heißt „Teileart“; eine gespeicherte Spaltenauswahl mit `artikeltyp` bleibt wirksam.
8. Stückliste/Kommissionierauftrag (IDEAL) zeigen die Spalten; an der AKE erscheinen sie nicht, die AKE-Stückliste lädt unverändert.
9. FA-Liste/Leitstand (falls in Scope): Werte der Strukturwurzel bzw. des Sub-FA-Knotens; AKE-FA-Liste bit-identisch zu vorher.
10. Neue Col-Keys sind in allen Tabellen gleich (`teileart`, `ist-lagerartikel`, `verwendungsart`; Ausnahme Kommissionierliste `artikeltyp`).
11. Migration läuft zweimal fehlerfrei; FreshInstall hat beide Spalten und die MigrationId.
12. `dotnet build`/`dotnet test` grün; neue Tests: `BuildNodeSelectSql` mit/ohne Spalten, Getter je Tabelle, `ViewKeyDriftGuardTests`,
    `FaStrukturVarianteDTests`.

## Test-Szenarien

Neues Szenario in `docs/TESTSZENARIEN.md` (nächste freie TS-Nummer, Dev-Lauf prüft), Index `secondbrain/tests/testszenarien-index.md`.
- **Vorbedingungen:** DEV IDEAL, FA-Struktur-Abgleich nach Deploy gelaufen; ein HauptFA mit Baugruppe, Blechteil (Lagerartikel) und Zukaufteil.
- **Schritte:** (1) Aktivitäts-Protokoll des Abgleichs: keine Warnung „ohne Spalte“. (2) Je In-Scope-Tabelle: Zahnrad → drei Spalten einblenden,
  Werte an Lagerartikel/Nicht-Lagerartikel mit P-C/P-D vergleichen. (3) Je Spalte filtern (`Ja`, `Nein`, `VT`, ein Teileart-Wert, `(leer)`).
  (4) FA-Struktur: Felder des erweiterten Umfangs einblenden und filtern; Baum klappt auf den Treffer-Pfad. (5) Neu laden: Auswahl bleibt.
  (6) Kommissionierliste: Spalte heißt „Teileart“, alte Auswahl wirkt.
- **Negativ:** Knoten ohne Werte → „–“; AKE: FA-Liste, Stückliste, Leitstand ohne neue Spalten, Seiten laden fehlerfrei.
- **Regression:** bestehende Spalten/Filter (Material, Arbeitsbereich, Komm.-Ziel, Kopfspalten FA-Struktur) unverändert.

## Etappen (nur bei epic: true)

Epic-Vorschlag (Rueckfrage 8): ein Worktree, je Etappe ein Commit, ein Merge am Ende; Branch während der Arbeit mit
`scripts/sync-worktree.ps1 -Slug <slug>` aktuell halten. Baut auf der BDE-Spec auf (gleiches Bündel, BDE zuerst oder E0 gemeinsam).
Jede Etappe baut und testet grün für sich.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| E0 | Schema+Dienst: `Verwendungsart`, `IstLagerartikel` am Knoten, COL_LENGTH-Schutz, Migration (SQL/172 gemeinsam oder 174) – **wartet auf Rueckfrage 7 und P-B** | offen | |
| E1 | FA-Struktur: drei Spalten + erweiterter Umfang – erweiterter Umfang **wartet auf Rueckfrage 2**; drei Spalten bauen | offen | |
| E2 | Kommissionierlisten Einzeln (+ Summiert/Druck nach Rueckfrage 5), Label Teileart (Rueckfrage 4) | offen | |
| E3 | Vormontage Einzeln (+ Summiert nach Rueckfrage 5) | offen | |
| E4 | Stückliste + Kommissionierauftrag-Liste (nur hierarchisch) | offen | |
| E5 | FA-Liste (Strukturwurzel) – **nur nach Rueckfrage 1** | offen | |
| E6 | Kommissionier-Leitstand – **nur nach Rueckfrage 1** | offen | |

## Deploy

*Vorläufig, die QA bestätigt gegen den echten Diff.*

- **Web-App:** ja (alle Etappen).
- **Service:** ja (E0: `FaHierarchySql`, `SageFaStrukturReader`).
- **Migration:** ja (E0). Schema vor dem Start von Web und Dienst (beide lesen/schreiben `FaHierarchyNodes` über das EF-Modell). Additiv, kein Backup-Sonderfall.
- Hinweis: Der erste FA-Struktur-Lauf nach dem Deploy meldet viele „geändert“ (einmalig).
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023):
  - Id `SQL/172_…` (gemeinsam mit BDE-Spec) bzw. `SQL/174_AddFaHierarchyNodeIstLagerartikelVerwendungsart`, Version = Release-Version, Typ `Schema`,
    Firma `beide`, Phase `Schema`, Ausführung `automatisch`, Text „Spalten FaHierarchyNodes.Verwendungsart/IstLagerartikel (FA-Tabellen-Zahnrad)“.
    Reihenfolge nach `SQL/170`–`173`, falls im selben Release (Deploy-Reihenfolge, nicht Dateisortierung).
  - Id `Info/FaTabellenZahnradView`, Typ `Info`, Firma `IDEAL`, Phase `Nachher`, Text „Nach dem nächsten FA-Struktur-Lauf: Aktivitäts-Protokoll auf
    ‚Sage-View ohne Spalte Verwendungsart/IstLagerartikel‘ prüfen (P-B).“ (Bei gemeinsamer Migration mit dem Info-Eintrag der BDE-Spec zusammenlegen.)
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- Hauspflichten im Dev-Lauf: Version in beiden `AppVersion.cs`, `Views/Help/Changelog.cshtml`, Hilfe (FA-Struktur, Kommissionierliste,
  Vormontage, Stückliste), `docs/anwender`, Brain-Changelog, `feature-map.md`, Fallstrick „Verwendungsart/IstLagerartikel nur über View-Weg“ + Folgeaufgabe SData.

## Offene Rueckfragen

1. **Tabellen-Umfang** (Fachbereich: Benjamin Andl-Braun): Welche FA-Tabellen bekommen die drei Spalten? Vorschlag ⭐: FA-Struktur,
   Kommissionierlisten (Einzeln), Vormontage (Einzeln), Stückliste und Kommissionierauftrag-Liste. FA-Liste und Leitstand zeigen je Zeile den
   ganzen Auftrag (Endgerät); dort wären es die Werte des Endgeräts – nur aufnehmen, wenn das nützt. Fehlt eine Tabelle (z. B. Beschichtung,
   FA-Abarbeitungsliste)?
2. **FA-Struktur „erweiterter Umfang“** (Fachbereich: Benjamin Andl-Braun): Sind alle Positionsfelder gemeint, die das WMS schon hat, die
   FA-Struktur aber nicht anbietet (Position, Artikelgruppe, Hauptlagerplatz, Bemerkung PN, Fertigungsinfo PN, Arbeitsschritte, Material,
   VM-Bedarf, Breite/Höhe/Tiefe, Beschaffungsartikel, EK-Bedarf) plus die drei neuen? Oder welcher Bereich ist das Vorbild?
3. **Verwendungsart** (Fachbereich: Benjamin Andl-Braun): Im Testsystem gibt es nur „VT“, und zwar genau bei Ist Lagerartikel = Ja. Ist die
   Verwendungsart fachlich etwas anderes als Ist Lagerartikel, und welche Werte gibt es in der Praxis?
4. **Teileart** (Fachbereich: Benjamin Andl-Braun): Teileart = Sage-Feld Artikeltyp der Stücklistenposition (z. B. BG, FT, EP, ZK)? Die
   Kommissionierliste hat dafür schon die ausblendbare Spalte „Artikeltyp“ – Vorschlag ⭐: in „Teileart“ umbenennen, keine zweite Spalte.
5. **Summiert und Druck** (Fachbereich: Benjamin Andl-Braun): Auch in den Summiert-Sichten (Kommissionierliste, Vormontage; verschiedene Werte
   kommagetrennt) und in den Ausdrucken (Kommissionierliste, Stückliste, Kommissionierauftrag)? Vorschlag ⭐: erst Einzelsichten, Summiert und
   Druck bei Bedarf.
6. **Anzeige** (Fachbereich: Benjamin Andl-Braun): Ist Lagerartikel als Ja/Nein, alle neuen Spalten standardmäßig ausgeblendet (nur über das
   Zahnrad) – einverstanden?
7. **Verhältnis zur BDE-Spec:** Vorschlag ⭐: Ist Lagerartikel für alle Tabellen (auch BDE) aus der View an der FA-Position (neues Feld
   `FaHierarchyNode.IstLagerartikel`), eine gemeinsame Migration mit BDE Teil B (`SQL/172`); Artikelmerkmal nur noch für BDE-Lagerbestell-Zeilen.
   Grund: eine Quelle, eine Regel; P-4: 0 Abweichungen Position vs. Artikel; das Merkmal fehlt bei nie nachgelesenen Artikeln. Braucht einen
   Nachtrag in [[2026-11-26-bde-zahnrad-artikelfelder-spec]] (Teil A).
8. **Schnitt:** Epic mit E0–E6 in einem Worktree, im selben Bündel nach bzw. mit der BDE-Spec – einverstanden, oder Split in einzeln
   mergbare Teile (Kandidat: E0+E1 als Teil 1, E2–E4 Teil 2, E5/E6 Teil 3)?

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)
<!--
  Von der Koordination eingetragen, wortgetreu, je Antwort:
  <Nr>. <Name>, <yyyy-mm-dd>, [<Jira-Schluessel> Kommentar](<Link>): "<Antwort>"
  Das ist fachlicher Input, keine Freigabe. Entscheidend bleibt der Freigabe-Block darunter;
  uebernimmt der Mensch eine Antwort, genuegt dort "wie Fachbereich".
-->

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Tabellen-Umfang (FA-Struktur, Kommissionierliste, Vormontage, Stückliste/Kommissionierauftrag; FA-Liste/Leitstand?) →
2. FA-Struktur erweiterter Umfang (alle vorhandenen Positionsfelder?) →
3. Verwendungsart (Bedeutung, Werte neben „VT“?) →
4. Teileart = Artikeltyp, Kommissionierliste-Spalte umbenennen? →
5. Summiert-Sichten und Druck mitnehmen? →
6. Anzeige Ja/Nein, Default ausgeblendet? →
7. Ist Lagerartikel aus der View am Knoten, gemeinsame Migration mit BDE Teil B? →
8. Epic E0–E6 im Bündel mit der BDE-Spec oder Split? →
