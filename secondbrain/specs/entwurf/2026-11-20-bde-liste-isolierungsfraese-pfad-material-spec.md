---
type: spec
title: BDE-Teileliste – Material/Blechdicke/Bemerkung/Fertigungsinfo, Fräsen-Pfad je Artikel, Lagerbestell-Positionen an der Isolierungsfräse
slug: 2026-11-20-bde-liste-isolierungsfraese-pfad-material-spec
status: Entwurf
created: 2026-11-20
updated: 2026-11-20
source_backlog: "[[2026-11-19-bde-liste-isolierungsfraese-pfad-material]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: ""
standort: IDEAL
version_vorschlag: "1.99.49 (Gerald 2026-11-22; 1.99.46 Dashboard-Symbole, 1.99.47 Teil 6 Lagerplatz ausbuchen, 1.99.48 Sage-AG-Bugfix; Dev-Lauf prüft erneut)"
sql_vorschlag: "164 (zuletzt SQL/163_AddBfPositionArbeitsschritte.sql; Dev-Lauf prüft erneut)"
testszenario_vorschlag: "TS-171 (166–170 belegt laut testszenarien-index, 165 reserviert für BDE mobil kompakt; Dev-Lauf prüft erneut)"
affected_code:
  - IdealAkeWms/Views/BdeTeileliste/Index.cshtml
  - IdealAkeWms/Views/BdeTeileliste/Summiert.cshtml
  - IdealAkeWms/wwwroot/js/bde-teileliste.js
  - IdealAkeWms/Services/BdeTeilelisteService.cs
  - IdealAkeWms/Controllers/BdeTeilelisteController.cs
  - IdealAkeWms/Models/ViewModels/BdeTeilelisteViewModel.cs
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs
  - IdealAkeWms/Services/ColumnFilterSyntax.cs (ColumnFilterTypes)
  - IdealAkeWms/Models/BdeTeilMeldung.cs
  - IdealAkeWms/Data/ApplicationDbContext.cs
  - IdealAkeWms/Controllers/BdeBookingsController.cs (Teilmeldungen)
  - IdealAkeWms/Views/BdeBookings/Teilmeldungen.cshtml
  - IdealAkeWms/Models/ProductionWorkplace.cs
  - IdealAkeWms/Controllers/ProductionWorkplacesController.cs
  - IdealAkeWms/Data/Repositories/ProductionWorkplaceRepository.cs (EditableProperties)
  - IdealAkeWms/Views/ProductionWorkplaces/_BdeTeilelisteField.cshtml
  - IdealAkeWms/Controllers/ArticleAttributesController.cs
  - IdealAkeWms/Views/ArticleAttributes/Index.cshtml
  - IdealAkeWms/Controllers/ArticlesController.cs (Edit POST)
  - IdealAkeWms/Views/Articles/Edit.cshtml
  - IdealAkeWms/Data/Repositories/ArticleAttributeRepository.cs (SaveValuesAsync)
  - IdealAkeWms/Models/AppSettingKeys.cs
  - IdealAkeWms/Models/AppSettingDefinitions.cs
  - IdealAkeWms/Models/SettingDefinition.cs (SettingStammdaten)
  - IdealAkeWms/Services/BfArtikelFelder.cs
  - IdealAkeWms/Services/SageSdata/SageUserfelder.cs
  - IdealAkeWms/Migrations/<neu>_AddBdeTeilelisteLagerbestellungen.cs
  - SQL/164_AddBdeTeilelisteLagerbestellungen.sql
  - SQL/00_FreshInstall.sql
  - SQL/Einmalig/2026-11-xx_v1.99.49_FraesenPfad_Erstimport.sql
  - scripts/deploy/nachlauf.psd1
  - IdealAkeWms/Views/Help/Index.cshtml
  - IdealAkeWms/Views/Help/Changelog.cshtml
  - IdealAkeWms/AppVersion.cs
  - IDEALAKEWMSService/AppVersion.cs
  - docs/TESTSZENARIEN.md
open_questions:
  - "(Fachbereich: Benjamin Andl-Braun) Blechdicke: genauer Sage-Feldname (USER_…) am Artikel und Format (z. B. „3,00“ = mm?) – Prüfabfrage P-1. Bis zur Antwort bleibt die Einstellung leer, die Spalte zeigt „–“."
  - "(Fachbereich: Walter Kronberger) Fräsen-Pfad: ist der Wert ein Ordner oder eine Datei (Beispiele ohne Dateiendung)? Wer pflegt ihn im WMS (Artikel bearbeiten braucht die Rolle Stammdaten)?"
  - "(Gerald) Weg „Datei direkt aus dem WMS öffnen“: Empfehlung A – Pfad als Text plus Knopf „Pfad kopieren“, im Explorer mit Strg+L einfügen; B (file-Link per Edge-Gruppenrichtlinie für Intranet-Seiten, mit IT prüfen) höchstens später; C (eigenes Protokoll/Hilfsprogramm je PC) nein."
  - "(Fachbereich: Walter Kronberger) Welche Lagerbestellungen zeigt die Fräse: abgeschickte und teilgelieferte; abgeschlossene und stornierte fallen weg, auch wenn die Fräse sie nicht gemeldet hat. Empfehlung: ja."
  - "(Fachbereich: Walter Kronberger) KW-Filter: Lagerbestell-Positionen haben keinen FE-Termin. Empfehlung: immer sichtbar, solange die Bestellung offen ist (KW-Filter wirkt nur auf Auftragsteile). Alternative: Filter nach Abschickdatum."
  - "(Fachbereich: Walter Kronberger) Summiert: Material und Blechdicke sichtbar, Bemerkung PN, Fertigungsinfo PN und Pfad über das Zahnrad einblendbar; Lagerbestell-Positionen zählen im Summiert mit. Empfehlung: ja."
  - "(Fachbereich: Walter Kronberger) „Material und Blechdicke auch in den anderen BDE-Ansichten“ = Teileliste (Einzeln + Summiert) aller Teileliste-Werkbänke, NICHT die Arbeitsgang-Liste des BDE-Terminals. Empfehlung: ja."
  - "(Gerald) Lagerartikel-Grenze: Arbeitsschritte, Material und Blechdicke am Artikel liest das WMS nur für Sage-Lagerartikel (USER_OSIstLagerartikel ≠ 0) und nur mit Sync:BlechteileFelderNachlesen an. Empfehlung: so bauen, P-2/P-4 vor der Abnahme laufen lassen; fehlen IF-Isolierungen dort, Nachlese als Folgeaufgabe erweitern."
  - "(Gerald) Erstimport der 29 Zeilen aus Walters Liste: Empfehlung Einmal-Skript mit Trockenlauf und eingebetteter Liste (Matchcode → Pfad); dafür Walters Liste als Datei. Mehrdeutige oder unbekannte Matchcodes werden nur gemeldet, nicht geschrieben. Alternative: Handpflege."
  - "(Gerald) Zusatzfund: Der CSV-Export der Teileliste speichert die Seitengröße „Alle“ im Werker-Filter. In dieser Spec mitbeheben? Empfehlung: ja."
  - "(Fachbereich: Walter Kronberger) Soll die Fräsen-Meldung auch in der Lagerbestellung (Lager-Detailseite, Besteller) sichtbar sein? Empfehlung: nicht in diesem Schritt, die Teilmeldungen-Liste reicht."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

# BDE-Teileliste: Material/Blechdicke/Bemerkung/Fertigungsinfo, Fräsen-Pfad, Lagerbestell-Positionen an der Isolierungsfräse

Quelle: [[2026-11-19-bde-liste-isolierungsfraese-pfad-material]] (Walters Antworten 1–5 gelten als Fachbereichs-Vorgabe).
Baut auf [[2026-11-13-bde-vereinfacht-teileliste-blechfertigung-spec]] (v1.99.32, ADR [[0030-teilmeldung-mit-fachlichem-schluessel-neben-workoperation]])
und [[0033-sage-benutzerfelder-als-einstellung]] auf. Alle Code-Belege beziehen sich auf den Bündel-Worktree `2026-08-07-ideal-teile-1-5`,
Stand v1.99.45.

## Ziel / Nutzen (das Warum)

Clemens (Isolierungsfräse) filtert heute die Mails der Lagerbestellungen von Hand: Teile, deren Programm unter `…\Gerhard\Listen\…` liegt,
druckt er als Liste für die Fräse, die übrigen schachtelt er. Auftragsbezogene Isolierungen (FA-Struktur, Kürzel IF) sieht er dagegen in der
BDE-Teileliste. Ziel: Lagerteile und Auftragsteile mit Arbeitsschritt IF stehen in **einer** Liste an der Werkbank IF, mit Pfad, Material und
Blechdicke, filterbar und fertig meldbar. Der Pfad wird im WMS je Artikel gepflegt (keine eigene Datenbank). Nebenbei werden Material,
Blechdicke, Bemerkung PN und Fertigungsinfo PN in allen Teileliste-Ansichten verfügbar.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. Teileliste **Einzeln**: neue Spalten Blechdicke und Fräsen-Pfad (mit Knopf „Pfad kopieren“); Material, Bemerkung PN, Fertigungsinfo PN gibt
   es schon.
2. Teileliste **Summiert**: neue Spalten Material, Blechdicke, Bemerkung PN, Fertigungsinfo PN, Fräsen-Pfad (je Rückfrage 6).
3. **Fräsen-Pfad**: manuelles Text-Merkmal am Artikel (Pflege unter Artikel bearbeiten), welches Merkmal es ist, legt eine AppSetting fest,
   Erstimport per Einmal-Skript (Rückfrage 9).
4. **Blechdicke**: neues Sage-Benutzerfeld in der vorhandenen Blechteile-Nachlese, als Sage-Merkmal am Artikel (Name per Einstellung, Rückfrage 1).
5. **Lagerbestell-Positionen** in der Teileliste einer Werkbank mit neuer Option „Lagerbestellungen in der Teileliste“: Positionen offener
   Lagerbestellungen, deren Artikel-Merkmal Arbeitsschritte das Kürzel der Werkbank trägt; fertig meldbar ohne Lagerbuchung, rücknehmbar.
6. BDE-Buchungen › Teilmeldungen zeigt Lagerbestell-Meldungen erkennbar an.
7. Hilfe, Anwender-Changelog, Version, TS-Kapitel, Manifest.

**Out-of-Scope**
- Keine Lagerbuchung, keine Änderung an Status/Menge der Lagerbestellung, nichts an Sage (Walter Antwort 4).
- BDE-Terminal (Arbeitsgang-Liste, `bde-terminal.js`) bleibt unverändert (Rückfrage 7).
- Kein file-Link, kein Hilfsprogramm zum Öffnen (Rückfrage 3, Empfehlung A).
- Kein eigener Excel-Export: der vorhandene CSV-Knopf der Liste reicht (siehe Ponytail).
- Erweiterung der Blechteile-Nachlese auf Nicht-Lagerartikel (Rückfrage 8, ggf. Folgeaufgabe).
- „Frage an JOK, ob es weitere Optimierungen gibt“ (Backlog Z. 19) – nicht Teil dieser Spec.
- Mobile Kartenansicht: eigene Spec [[2026-11-18-bde-vereinfacht-mobil-kompakt-spec]] (zurückgestellt), siehe Abhängigkeiten.

## Fachliche Anforderungen

F1. **Spalten Einzeln.** Zusätzlich zu den vorhandenen Spalten (`Index.cshtml:28-49`): **Blechdicke** (sichtbar) und **Fräsen-Pfad**
    (standardmäßig ausgeblendet, über das Zahnrad einblendbar; Clemens' Terminal blendet sie einmal ein, die Wahl gilt je Web-Benutzer).
    Bemerkung PN und Fertigungsinfo PN bleiben wie heute ausgeblendet einblendbar (`Index.cshtml:221-222`), Material bleibt sichtbar (`:212`).

F2. **Spalten Summiert** (Rückfrage 6): Material, Blechdicke sichtbar; Bemerkung PN, Fertigungsinfo PN, Fräsen-Pfad ausgeblendet einblendbar.
    Zellwert = die verschiedenen Werte der beitragenden Positionen, sortiert, kommagetrennt (wie Abteilung in Summiert).
    Spaltenfilter auf diesen Spalten filtern die Positionen **vor** dem Zusammenzählen (wie heute Hauptauftrag/Abteilung).

F3. **Fräsen-Pfad pflegen.** Unter Stammdaten › Artikelmerkmale kann ein Merkmal vom Typ **Text** angelegt werden (heute nur Boolean/Dropdown).
    Unter Artikel bearbeiten erscheint ein manuelles Text-Merkmal als Textfeld (max. 1000 Zeichen), Speichern übernimmt den getrimmten Wert,
    leer löscht ihn. Sage-geführte Text-Merkmale bleiben gesperrt wie heute. Welches Merkmal der Fräsen-Pfad ist, legt die AppSetting
    `BdeTeilelistePfadMerkmal` fest (Abschnitt Einstellungen). Die Artikelinfo zeigt das Merkmal ohne Änderung mit (`ArticlesController.cs:266`).

F4. **Pfad öffnen** (Rückfrage 3, Empfehlung A). Die Pfad-Zelle zeigt den Text und einen Knopf „Pfad kopieren“ (≥ 44 px, `bde-touch`).
    Kopieren über die Zwischenablage; geht das nicht (unsicherer Kontext, Browser verweigert), wird der Text markiert und ein Hinweis
    „Text markiert – mit Strg+C kopieren“ gezeigt. Tooltip/Hilfe: „Im Explorer Strg+L drücken, einfügen, Enter.“ Kein `file://`-Link.

F5. **Blechdicke.** Wert aus dem Sage-Benutzerfeld laut Rückfrage 1, gelesen von der Blechteile-Nachlese, gespeichert als Sage-Merkmal am Artikel.
    Zuordnung zur Zeile über die Artikelnummer (FA-Zeile: `FaHierarchyNode.Artnr`, Lagerbestell-Zeile: `WarehouseRequisitionItem.ArticleNumber`).
    Kein Wert / nicht konfiguriert → „–“. Spaltenfilter als **Zahl** (sonst wäre das Komma in „3,00“ ein ODER, ADR 0017).

F6. **Werkbank-Option „Lagerbestellungen in der Teileliste“** (`ProductionWorkplaces.BdeTeilelisteLagerbestellungen`, Default aus), in der
    Werkbank-Maske neben „BDE-Teileliste“; nur speicherbar, wenn „BDE-Teileliste“ an ist (Prüfung wie `ProductionWorkplacesController.cs:261-267`).
    Wirksam nur bei wirksamer Teileliste (`WerkbankKuerzelFamilie.TeilelisteWirksam`, `WerkbankKuerzelFamilie.cs:52-53`) **und**
    `LagerbestellungAktiv`. Kein Kürzel im Code: die Werkbank IF wird über diese Option gewählt (Wächtertest
    `TeilelisteKuerzelKonstantenWaechterTests` bleibt grün).

F7. **Lagerbestell-Zeilen.** Bei wirksamer Option zeigt die Teileliste zusätzlich jede Position einer Lagerbestellung im Status **abgeschickt**
    oder **teilgeliefert** (Rückfrage 4), deren Artikel das Kürzel der Werkbank im Merkmal Arbeitsschritte trägt (Token-Regel wie
    `BdeTeilelisteService.Traegt`, Leerzeichen-Trennung `FaHierarchyTreeViewModel.cs:259-264`, Groß/Klein egal; Walter Antwort 5).
    Zelleninhalt: Matchcode (Artikel), Menge = bestellte Menge, Hauptauftrag leer, Abteilung = **bestellende Werkbank**, Material
    (Merkmal `USER_OSMaterial`), Blechdicke, Pfad, Fertigungsinfo PN (Merkmal `USER_OSFertigungsInfoPN`), Bemerkung PN leer, Bezeichnung =
    Positionstext der Bestellung. Kennzeichnung im Status-Feld: Badge „Lagerbestellung <Nr>“ plus Kopf-Kommentar der Bestellung (klein), damit
    der Werker sie sicher von Auftragsteilen unterscheidet. KW-Filter wirkt auf diese Zeilen nicht (Rückfrage 5), „gemeldete zeigen“ wirkt.

F8. **Fertig melden** einer Lagerbestell-Zeile (Walter Antwort 3/4): wie eine Blechteil-Meldung – Einzelmeldung mit Gut/Ausschuss oder
    Blockmeldung mit voller Menge; Position gilt als gefertigt, **keine Lagerbuchung, keine Statusänderung der Lagerbestellung, kein
    Arbeitsgang, nichts an Sage**. Der Server prüft am Schreibort (ADR 0026): Option wirksam, Werker aktiv, Bestellung noch abgeschickt/
    teilgeliefert, Artikel trägt das Kürzel noch, keine offene Meldung zu Position + Kürzel; Doppelschutz durch gefilterten UNIQUE.
    Sammelmeldung ohne stilles Weglassen (`MeldeErgebnis`).

F9. **Rücknahme** wie bei Teilmeldungen: Werker eigene bis Schichtende bzw. Ende des Meldetags, BDE-Admin mit Grund (`ZuruecknehmenAsync`,
    `BdeTeilelisteService.cs:406-436`, unverändert nutzbar, da über `meldungId`).

F10. **Summiert mit Lagerbestell-Zeilen** (Rückfrage 6): sie zählen je Matchcode mit; „Hauptaufträge“ nennt zusätzlich „LB <Nr>“; Blockmeldung
     „Auswahl fertigmelden“ meldet alle offenen Schlüssel beider Arten.

F11. **Teilmeldungen-Liste** (BDE-Buchungen › Teilmeldungen): neue Spalte „Lagerbestellung“ (Nr./Pos.), Hauptauftrag bei Lagerbestell-Meldungen „–“
     statt „0“.

F12. **CSV** (Rückfrage 10): Der vorhandene CSV-Knopf exportiert alle Zeilen mit den sichtbaren Spalten (also auch Pfad, wenn eingeblendet)
     und verändert den gespeicherten Werker-Filter nicht mehr.

## Technischer Loesungsentwurf

### Befunde (Ist-Stand, belegt)

| # | Befund | Beleg |
|---|---|---|
| B1 | Teileliste-Zeilen kommen nur aus der FA-Struktur: Kandidat = Knoten, dessen `Arbeitsschritte` das Werkbank-Kürzel als Token trägt. | `BdeTeilelisteService.cs:91-92,162` |
| B2 | Bemerkung PN/Fertigungsinfo PN in Einzeln vorhanden, ausgeblendet; Getter (Filter) existieren schon. | `Index.cshtml:44-45,89-90,221-222`; `VormontageService.cs:153-154` |
| B3 | Summiert hat 8 Spalten ohne Material/Bemerkung/Fertigungsinfo; alle Colf-Keys außer matchcode/sollmenge/anzahl filtern schon heute die Positionen vor dem Zusammenzählen (Vorfilter). | `Summiert.cshtml:28-37`; `BdeTeilelisteController.cs:145`; `BdeTeilelisteService.cs:118-124` |
| B4 | `FaHierarchyNode.Material` stammt aus der View-Spalte `Material` bzw. im SData-Modus aus `USER_OSMaterial` – Material der Position, nicht des Artikels. Blechdicke gibt es an der Struktur nicht. | `SageFaStrukturReader.cs:126,156`; `SdataFaStrukturReader.cs:266`; `FaHierarchyNode.cs:35-49` |
| B5 | Artikel-Merkmale `USER_OSArbeitsschritte`, `USER_OSMaterial`, `USER_OSFertigungsInfoPN`, `USER_OSMaterialnummerPN` schreibt die Blechteile-Nachlese – **nur** für Sage-Artikel mit `USER_OSIstLagerartikel ne 0` und nur bei `Sync:BlechteileFelderNachlesen` an (Default aus). Merkmale bleiben unter dem logischen Namen. | `BfArtikelFelder.cs:11-26`; `SdataArtikelReader.cs:336-364`; `BlechteileFelderNachlese.cs:23`; `ServiceSettingDefinitions.cs:106-108` |
| B6 | Sage-Feldnamen kommen aus `Sync:Userfeld:<Abgleich>:<Default>`; Zeile fehlt = Default wird an Sage geschickt, leer = nicht konfiguriert. Katalog hat 27 Einträge (Test). | `SageUserfelder.cs:5-15,41-70,79-92`; `SageUserfelderTests.cs:44` |
| B7 | `ArticleAttributeValue.TextValue` gibt es seit 2026-11-06 – aber nur Sage-Merkmale nutzen es: Anlage Text abgewiesen, Auswahl bietet nur Boolean/Dropdown, Edit-View rendert ein manuelles Text-Merkmal als **Dropdown**, Edit-POST übernimmt `TextValue` nicht, `SaveValuesAsync` wertet nur Boolean/Option als Wert. Ein manuelles Text-Merkmal ginge heute still verloren. | `ArticleAttributeValue.cs:13-18`; `ArticleAttributesController.cs:93-98`; `ArticleAttributes/Index.cshtml:206-207`; `Articles/Edit.cshtml:126-136`; `ArticlesController.cs:199-204`; `ArticleAttributeRepository.cs:162-163` |
| B8 | Fallstrick „Text-Merkmale gibt es nur bei FaAttributes, nie bei ArticleAttributes … `ArticleAttributeValue` hat kein TextValue“ ist **überholt** (B7); das „Warum“ (Wert geht still verloren) gilt für manuelle Merkmale weiter. | `fallstricke.md:185-190` |
| B9 | Der Sage-Merkmal-Writer fasst nur Definitionen mit `SyncSource = "Sage"` an – ein manuelles Merkmal bleibt beim Abgleich unberührt. | `ArtikelBenutzerfelderWriter.cs:40,67,88` |
| B10 | Lagerbestellung: Kopf mit bestellender Werkbank, Status (Draft/Submitted/Closed/Cancelled/PartiallyDelivered), Kommentar; Position mit Artikelnummer (Text), Bezeichnung, Menge – **kein** FA-Bezug, kein Arbeitsgang. | `WarehouseRequisition.cs:8-46`; `WarehouseRequisitionStatus.cs:5-9`; `WarehouseRequisitionItem.cs:11-23` |
| B11 | Positionen werden nur im Entwurf geändert/gelöscht (Guard `CheckOwnershipAndDraft`); Status Draft setzt nur die Anlage. Eine abgeschickte Position verschwindet also nicht unter einer Meldung weg. Im Dienst-Projekt gibt es keinen Schreiber auf Lagerbestellpositionen. | `WarehouseRequisitionsApiController.cs:298-319`; `WarehouseRequisitionRepository.cs:19,170-174`; Grep `WarehouseRequisitionItems` in `IDEALAKEWMSService` ohne Treffer |
| B12 | `BdeTeilMeldung` hat den Schlüssel (HauptFA int NOT NULL, VaterFA, SubFA, Position, Artnr) + Kürzel; gefilterter UNIQUE `UX_BdeTeilMeldungen_Schluessel … WHERE IsCancelled = 0`. Geschrieben nur in `MeldeZeileAsync`; gelesen in `BdeTeilelisteService` und `BdeBookingsController.Teilmeldungen`. | `BdeTeilMeldung.cs:12-54`; `ApplicationDbContext.cs:1570-1590`; `SQL/154_AddBdeTeileliste.sql:69-70`; `BdeTeilelisteService.cs:133-134,346-353`; `BdeBookingsController.cs:277-309` |
| B13 | Formular-Schlüssel `HauptFA|VaterFA|SubFA|Position|Artnr`; alles mit weniger als 5 Teilen ist ungültig → ganze Meldung abgewiesen. | `BdeTeilelisteService.cs:19-30`; `BdeTeilelisteController.cs:183-189` |
| B14 | Werkbank-Pflegefelder sind eine Liste; ein neues Feld, das dort fehlt, wird beim Speichern still nicht geschrieben. | `ProductionWorkplaceRepository.cs:12-20` |
| B15 | CSV-Export ruft im Server-Modus dieselbe URL mit `pageSize=0` ab; der Teileliste-Controller speichert jeden Aufruf mit Filterparametern als Werker-Filter – inkl. `PageSize = 0` („Alle“, gekappt 5000). Danach öffnet die Liste für diesen Werker mit „Alle“. | `csv-export.js:200-206`; `BdeTeilelisteController.cs:115,122-127`; `PageSize.cs:12-30` |
| B16 | Kopieren in die Zwischenablage mit Rückfall „markieren“ gibt es schon als Muster. | `hardware-scanner.js:211-218` |
| B17 | Neue Spalten brauchen den Eintrag in `ColumnDefinitions` (sonst speichert die Prefs-API still nichts) und typisierte Spalten den Eintrag im Register `ColumnFilterTypes` (Drift-Test). | `ColumnDefinitions.cs:454-507,854-855`; `ColumnFilterSyntax.cs:47-48`; `fallstricke.md:233-243` |
| B18 | Artikelnummer der Struktur ↔ WMS-Artikel wird schon heute über `Artnr` getrimmt, Groß/Klein egal, verglichen (Lagerplätze der Kommissionierliste). | `KommissionierListenService.cs:69-76` |

### Lösung

**1. Artikel-Zusatzwerte je Request (eine Abfrage).** Neuer kleiner Lader im `BdeTeilelisteService`: alle `ArticleAttributeValues` der
Definitionen Material, Fertigungsinfo, Arbeitsschritte, Blechdicke (Sage, über `SyncFieldName` = logischer Name) und Fräsen-Pfad
(Id aus `BdeTeilelistePfadMerkmal`) mit `Article.ArticleNumber` – **ohne** Artikelnummer-Liste (keine 2100-Parameter-Grenze; Umfang laut L10
rund 1.263 Lagerartikel, `SdataArtikelReader.cs:335`). Ergebnis: `Dictionary<string, ArtikelZusatz>` (Schlüssel `TrimEnd`, OrdinalIgnoreCase,
B18). Wird wie heute `beschichtet` als Getter in `SpaltenEinzeln` gereicht (`BdeTeilelisteService.cs:98-115`), damit Zelle und Filter
denselben Text liefern (fallstricke „Getter liefert gerenderten Text“). `ColumnFilterTypes`: `blechdicke` = Number in Index und Summiert.

**2. Summiert-Spalten.** `TeilelisteAggregat` bekommt `Materialien`, `Blechdicken`, `BemerkungenPn`, `FertigungsinfosPn`, `Pfade`
(`VormontageService.FormatTexte`, wie `Abteilungen` `BdeTeilelisteService.cs:216`). Keine Änderung an `SpaltenSummiert` nötig: die neuen Keys
sind Vorfilter (B3). Views + `#column-config` + `ColumnDefinitions.BdeTeilelisteSummiert` ergänzen.

**3. Manuelles Text-Merkmal.**
- `ArticleAttributesController.CreateDefinition`: Text zulassen (`:94`), Auswahl in `ArticleAttributes/Index.cshtml:206-207` um „Text (Freitext)“.
- `Articles/Edit.cshtml`: Zweig `AttributeType.Text` (nicht Sage) → `<input type="text" name="Attributes[i].TextValue" maxlength="1000">`.
- `ArticlesController.Edit` POST: `TextValue` mitgeben (`:199-204`); Server kürzt nicht still, sondern lehnt > 1000 Zeichen mit ModelState-Fehler ab.
- `ArticleAttributeRepository.SaveValuesAsync`: `hasValue` = Boolean oder Option **oder** nicht-leerer `TextValue`; `TextValue` beim Update setzen.
  Sage-Definitionen bleiben übersprungen (`:150-158`).
- Fallstrick-Eintrag (B8) im Brain als überholt kennzeichnen und neu fassen (Dev-Lauf, additiv).

**4. Blechdicke.** `BfArtikelFelder`: neue Konstante (logischer Name = Sage-Name laut Rückfrage 1; liegt die Antwort nicht vor:
`USER_OSBlechdicke` als logischer Platzhalter) und in `SageFelder` aufnehmen. `SageUserfelder.Katalog`: Eintrag Blechfertigung „Blechfertigung:
Blechdicke“ (Katalog 27 → 28, Test anpassen). Liegt der Feldname bei Umsetzung **nicht** vor, legt `SQL/164` die ServiceSetting-Zeile mit
leerem Wert an (= nicht konfiguriert; ohne Zeile würde der Platzhalter an Sage geschickt, B6) und der Manifest-Eintrag `Einstellung` trägt den
Namen nach. Sonst Default = echter Name, keine Seed-Zeile.

**5. Lagerbestell-Zeilen.**
- Laden (nur wenn Option wirksam und `LagerbestellungAktiv`): Positionen mit Kopf-Status Submitted/PartiallyDelivered, Join `Articles` über
  Artikelnummer, Filter in C# über das Arbeitsschritte-Merkmal (`Traegt`-Regel auf den Merkmal-Text). `TeilelisteZeile` wird um eine Quelle
  erweitert (`Node` bleibt für FA-Zeilen; Lagerbestell-Zeilen bekommen eine eigene kleine Projektion), Getter je Spalte für beide Arten.
  Sortierung wie heute nach Matchcode.
- Formular-Schlüssel: `LB|<WarehouseRequisitionItemId>`; der Controller trennt vor `TeilSchluessel.Parse` (B13). Ein ungültiger Schlüssel
  bleibt Abweisung der ganzen Anfrage wie heute.
- Melden: eigene `MeldeLagerZeileAsync` neben `MeldeZeileAsync` – applock `BDE-Teilmeldung-LB:<ItemId>|<KÜRZEL>`, Prüfungen F8, App-Guard
  „offene Meldung vorhanden“ (InMemory kennt keinen gefilterten UNIQUE, Memory-Regel), dann Insert; Unique-Verletzung 2601/2627 → „bereits
  gemeldet“ wie `BdeTeilelisteService.cs:358-365`.
- Meldung: `BdeTeilMeldung` mit neuem `WarehouseRequisitionItemId` (int NULL, FK Restrict), `HauptFA = 0`, `VaterFA = NULL`, `SubFA = 0`,
  `Position = NULL`, `Artnr` = Artikelnummer, `Matchcode`-Schnappschuss, `SollmengeBeiMeldung` = bestellte Menge, `BdeBookingId = NULL`.
  **Alle FA-Lesewege filtern ausdrücklich `WarehouseRequisitionItemId IS NULL`** (Laden `:133-134`, Waisen `:166`, `VorhandeneMeldungAsync`
  `:445-449`) – nicht auf „HauptFA 0 kommt nie vor“ verlassen.
- ADR: Ergänzung zu ADR 0030 als neue ADR (nächste freie Nummer, Brain-Stand 0035) „Teilmeldung auch für Lagerbestell-Positionen“ – Warum
  dieselbe Tabelle (Rücknahme, Admin-Storno, Teilmeldungen-Liste, Doppelschutz wiederverwendet) statt einer zweiten.

**6. Teilmeldungen-Liste.** `TeilmeldungSpalten` (`BdeBookingsController.cs:294-309`): `hauptfa` → „–“ bei Lagerbestell-Meldung, neue Spalte
`lagerbestellung` („<Nr> / Pos. <n>“, Include auf Position + Kopf). View + `ColumnDefinitions.BdeTeilmeldungen` ergänzen.

**7. Pfad kopieren.** In `bde-teileliste.js` ein delegierter Klick-Handler auf `[data-pfad-kopieren]` nach Muster B16; kein neues Skript, keine
Änderung an `site.js`.

**8. CSV-Seiteneffekt (Rückfrage 10).** Im `Index`-GET nicht speichern, wenn die Anfrage keine Navigation ist (Header `Sec-Fetch-Mode` ≠
`navigate`; fehlt der Header, wie heute speichern). Kein Eingriff in `csv-export.js` (gemeinsamer Code).

### Umsetzungsreihenfolge (ein Branch, je Schritt ein Commit, jeder Schritt baut und testet grün)

| # | Schritt | hängt an |
|---|---|---|
| 1 | Summiert-Spalten Material/Bemerkung/Fertigungsinfo (F2 ohne Blechdicke/Pfad) | – |
| 2 | Manuelles Text-Merkmal (F3 Pflege) + AppSetting `BdeTeilelistePfadMerkmal` + Spalte Pfad mit Kopieren (F1/F4) | – |
| 3 | Blechdicke (F5) – Code mit leerer Einstellung, wenn Rückfrage 1 offen | Wert: Rückfrage 1 / P-1 |
| 4 | Schema SQL/164 + Migration, Werkbank-Option (F6), Lagerbestell-Zeilen + Melden + Summiert + Teilmeldungen (F7–F11), ADR | – |
| 5 | CSV-Fix (F12), Einmal-Skript Erstimport, Manifest, Hilfe, Changelog, Version, TS-171 | Liste: Rückfrage 9 |

### Ponytail (bewusst nicht gebaut)

- Keine neue Tabelle für den Pfad: vorhandene Artikelmerkmale (Backlog-Vorgabe „keine separate Datenbank“).
- Keine neue Tabelle für Lagerbestell-Meldungen: `BdeTeilMeldungen` mit einer Spalte mehr (Rücknahme, Admin-Storno, Liste geschenkt).
- Kein eigener Excel-Export: `csv-export.js` exportiert die Server-Liste mit sichtbaren Spalten; Excel öffnet die CSV (Semikolon, UTF-8-BOM, `csv-export.js:7-8`).
- Kein „Pfad“-Filter-Bedienelement: der Spaltenfilter (enthält `gerhard\listen`) reicht; die Werkbank IF ist selbst schon der IF-Filter.
- Kein Kürzel „IF“ im Code: Werkbank-Option statt Konstante.
- Keine Pflege des Pfads aus der BDE-Liste heraus: Artikel bearbeiten genügt.
- Keine Änderung am BDE-Terminal.
- *Alternative zur AppSetting:* Merkmal fest über den Namen „Fräsen-Pfad“ finden – kleiner, bricht aber still bei Umbenennung; daher die
  Auswahl-Einstellung nach ADR 0020 (Stammdatenverweis).

### Abhängigkeiten / Regressionsrisiko

- [[2026-11-18-bde-vereinfacht-mobil-kompakt-spec]] ändert dieselben Views (`Index`, `Summiert`, `_Kopf`) – zurückgestellt. Wer später läuft,
  übernimmt die neuen Spalten (`data-label` an jeder neuen Zelle) bzw. die Kartenansicht.
- `ArticleAttributeRepository.SaveValuesAsync` und `Articles/Edit` wirken an **beiden** Standorten: QA prüft an AKE, dass Boolean-/Dropdown-
  Merkmale unverändert speichern und Sage-Merkmale gesperrt bleiben.
- Recreate des UNIQUE `UX_BdeTeilMeldungen_Schluessel` (Filter erweitert): kurzer Index-Neuaufbau auf kleiner Tabelle; Prüfung auf Dubletten
  nicht nötig, da der Filter nur enger wird.

## Migrations-/SQL-Auswirkungen

EF-Migration `AddBdeTeilelisteLagerbestellungen` + **SQL/164_AddBdeTeilelisteLagerbestellungen.sql** (Nummer Dev-Lauf prüfen), idempotent im
Stil von `SQL/154` (COL_LENGTH-/OBJECT_ID-/sys.indexes-Guards, DDL je eigener Batch, Marker in `__EFMigrationsHistory`):
1. `ProductionWorkplaces.BdeTeilelisteLagerbestellungen bit NOT NULL DEFAULT 0` (Guard `COL_LENGTH`).
2. `BdeTeilMeldungen.WarehouseRequisitionItemId int NULL`, FK `FK_BdeTeilMeldungen_WarehouseRequisitionItems_WarehouseRequisitionItemId`
   ON DELETE NO ACTION, Index `IX_BdeTeilMeldungen_WarehouseRequisitionItemId`.
3. `UX_BdeTeilMeldungen_Schluessel` neu mit Filter `[IsCancelled] = 0 AND [WarehouseRequisitionItemId] IS NULL` (Guard: nur droppen/anlegen,
   wenn `sys.indexes.filter_definition` noch den alten Filter trägt).
4. Neu `UX_BdeTeilMeldungen_Lagerbestellung` UNIQUE (`WarehouseRequisitionItemId`, `Kuerzel`) WHERE `[IsCancelled] = 0 AND
   [WarehouseRequisitionItemId] IS NOT NULL`.
5. Nur falls Rückfrage 1 bei Umsetzung offen: `INSERT dbo.ServiceSettings` Key `Sync:Userfeld:Blechfertigung:<Default>` Value `''`
   (IF NOT EXISTS).

`SQL/00_FreshInstall.sql`: Spalten, FK und beide Indizes im Abschnitt BDE-Teileliste. Kein Backfill. `SQL/AgentJobs/*` unberührt.
Einmal-Skript `SQL/Einmalig/2026-11-xx_v1.99.49_FraesenPfad_Erstimport.sql` (ADR 0015/0023, Rückfrage 9): genau ein
`DECLARE @Trockenlauf BIT = 1;`; legt das manuelle Text-Merkmal „Fräsen-Pfad“ an, falls es fehlt, setzt `BdeTeilelistePfadMerkmal`, falls leer,
schreibt je eindeutigem Matchcode-Treffer den Pfad (`CreatedBy = 'Einmal-Skript Fräsen-Pfad'`, `CreatedByWindows = SUSER_SNAME()`,
`CreatedAt = GETDATE()`), meldet mehrdeutige und unbekannte Matchcodes (fallstricke „Matchcode ist nicht eindeutig“, `fallstricke.md:469`);
Trockenlauf-Zweig gibt `TREFFER: n` aus; Wiederholungssperre (Merkmal hat schon Werte → `RAISERROR`) gibt im Trockenlauf `TREFFER: 0` aus.

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `BdeTeilelistePfadMerkmal` (AppSetting, Gruppe BDE) | Stammdatenverweis (`SettingStammdaten.Artikelmerkmal`, neu: Id eines **aktiven manuellen Text-Merkmals**) | Auswahl aus den Artikelmerkmalen Typ Text ohne Sage-Quelle; leer erlaubt; Server prüft beim Speichern Existenz, Typ, Quelle | leer | Welches Artikelmerkmal als „Fräsen-Pfad“ in der Teileliste (Einzeln/Summiert, Spalte + Kopieren) erscheint. Leer = Spalte zeigt „–“. | BDE-Teileliste; Einmal-Skript Erstimport | IDEAL (AKE ohne Wirkung) | – |
| `Sync:Userfeld:Blechfertigung:<Blechdicke-Feld>` (ServiceSetting, ADR 0033) | Text mit Muster `^(USER_[A-Za-z0-9_]{1,100})?$` (`SageUserfelder.cs:21`) | Sage-Feldname am Artikel oder leer | Name laut Rückfrage 1; sonst Seed-Zeile leer (= nicht konfiguriert) | Blechteile-Nachlese fragt das Feld bei Sage an und speichert es als Merkmal Blechdicke; leer = nicht gelesen, Spalte „–“. | Dienst: Artikel-Sync (Blechteile-Felder); Web: Teileliste | IDEAL | „Liest aus Sage (nur lesend). Ein falscher Name lässt die ganze Blechteile-Nachlese mit Warnung ausfallen; Merkmale bleiben dann unverändert.“ |
| `ProductionWorkplaces.BdeTeilelisteLagerbestellungen` (Werkbank-Option, keine Einstellung im Sinn von ADR 0020, der Vollständigkeit halber) | Bool je Werkbank | an/aus; an nur mit „BDE-Teileliste“ | aus | Teileliste zeigt Positionen offener Lagerbestellungen mit dem Kürzel der Werkbank und macht sie meldbar. | Werkbänke › Bearbeiten; Teileliste | IDEAL | – |

Abhängig, nicht geändert: `Sync:BlechteileFelderNachlesen` muss an IDEAL **an** sein (B5), `LagerbestellungAktiv` an (Glossar Default true).

## Audit-Feld-Auswirkungen

- `BdeTeilMeldung` (AuditableEntity): Lagerbestell-Meldungen setzen `CreatedAt/By/ByWindows` wie `BdeTeilelisteService.cs:351`; Rücknahme
  `Modified*` über `SetModified` (`:438-443`). Zeitstempel Lokalzeit (ADR 0025).
- `ArticleAttributeValue` (manuelles Text-Merkmal): `Created*`/`Modified*` aus `SaveValuesAsync` (Benutzer des Formulars); Einmal-Skript wie oben.
- `ArticleAttributeDefinition` „Fräsen-Pfad“: `Created*` aus `CreateDefinition` (`ArticleAttributesController.cs:112-114`) bzw. Einmal-Skript.
- `ProductionWorkplace`: neues Feld fällt unter die vorhandenen `Modified*` des Werkbank-Speicherns; Pflicht: in `EditableProperties` (B14).
- `AppSettings`: wie bestehend über `/Settings`.

**Rollen/Zugriff:** Teileliste unverändert `[RequireBdeActive]` + `[RequireBdeUserAccess]` + Terminal-Pflicht (`BdeTeilelisteController.cs:17-18,87-91`).
Neu sieht das Terminal-Konto der Werkbank mit Option Artikel, Menge, bestellende Werkbank und Kopf-Kommentar fremder Lagerbestellungen –
bewusst, nur lesend, ohne Lagerbestell-Rolle. Merkmal-Pflege: `[RequireMasterDataAccess]` (Rolle `masterdata`, Glossar Z. 93). Admin-Storno
unverändert `[RequireBdeAdminAccess]`. Keine neue Rolle → `controller.md`/`RoleOverview` unberührt.

**Listen-View-Pattern:** keine neue Liste; neue Spalten mit `data-col-key`, `data-filterable`, `#column-config` = `ColumnDefinitions`
(B17), Getter = Zelltext, Typ-Register für `blechdicke`, Pagination und Server-Spaltenfilter vor dem Paging bleiben (`BdeTeilelisteService.cs:190`).

## Akzeptanzkriterien

1. Einzeln: Spalten Blechdicke (sichtbar) und Fräsen-Pfad (ausgeblendet) vorhanden; über das Zahnrad ein-/ausblendbar; die Wahl bleibt nach Reload (Prefs-API 200).
2. Summiert: Material und Blechdicke sichtbar, Bemerkung PN, Fertigungsinfo PN, Fräsen-Pfad einblendbar; Zelle zeigt die verschiedenen Werte der Positionen kommagetrennt.
3. Spaltenfilter Material in Summiert filtert die Positionen vor dem Zusammenzählen (Summe Menge sinkt entsprechend).
4. Spaltenfilter Blechdicke `3,00` trifft Zeilen mit Blechdicke 3,00 (Zahlenfilter, kein ODER).
5. Stammdaten › Artikelmerkmale: Typ „Text“ anlegbar; Artikel bearbeiten zeigt ein Textfeld; Wert speichern, ändern und leeren (= gelöscht) funktioniert; > 1000 Zeichen wird mit Meldung abgewiesen.
6. Boolean-/Dropdown-Merkmale speichern unverändert; Sage-Merkmale bleiben nur lesend und werden beim Speichern nicht geändert (AKE und IDEAL).
7. `/Settings`: `BdeTeilelistePfadMerkmal` bietet nur aktive manuelle Text-Merkmale an; ein anderer Wert (z. B. per Formularmanipulation Id eines Boolean-Merkmals) wird abgelehnt.
8. Pfad-Zelle: „Pfad kopieren“ legt den exakten Text in die Zwischenablage; ohne Zwischenablage-Recht wird der Text markiert und der Hinweis gezeigt. Kein `file://`-Link im Markup.
9. Blechdicke: nach Nachlese-Lauf mit konfiguriertem Feld zeigt die Teileliste den Sage-Wert; mit leerer Einstellung „–“ und im Aktivitäts-Protokoll der Hinweis „nicht konfiguriert“, ohne Abbruch der Nachlese.
10. Werkbank-Option nur speicherbar mit „BDE-Teileliste“ an; Wert bleibt nach Speichern erhalten.
11. Mit Option an: abgeschickte und teilgelieferte Lagerbestell-Positionen, deren Artikel-Arbeitsschritte das Kürzel tragen, erscheinen mit Badge „Lagerbestellung <Nr>“; Entwurf, abgeschlossene und stornierte nicht; Artikel ohne Kürzel nicht; mit Option aus oder `LagerbestellungAktiv` aus keine.
12. KW-Filter blendet Lagerbestell-Zeilen nicht aus (je Rückfrage 5); „gemeldete zeigen“ aus blendet gemeldete aus.
13. Einzelmeldung (Gut/Ausschuss) und Blockmeldung einer Lagerbestell-Zeile legen je eine `BdeTeilMeldung` mit `WarehouseRequisitionItemId` an; keine `StockMovement`, keine `BdeBooking`, Status/Mengen der Lagerbestellung unverändert, keine Sage-Warteschlange.
14. Zweite Meldung derselben Position/Kürzel (zweites Terminal, Doppelklick) → „bereits gemeldet von … um …“, keine zweite Zeile (Manual-UAT für den UNIQUE).
15. Melden einer inzwischen abgeschlossenen oder stornierten Bestellung → abgewiesen mit Grund, Rest der Sammelmeldung läuft.
16. Rücknahme durch den Werker (bis Frist) und Admin-Storno mit Grund funktionieren für Lagerbestell-Meldungen; die Zeile ist danach wieder offen.
17. FA-Zeilen unverändert: Meldung, Baugruppen-AG, Waisen-Zähler, „bereits gemeldet“ verhalten sich wie v1.99.45 (bestehende Tests grün); eine Lagerbestell-Meldung zählt nie als Waise.
18. Teilmeldungen-Liste zeigt Spalte „Lagerbestellung“; Hauptauftrag „–“ bei Lagerbestell-Meldungen; Filter auf beiden Spalten wirkt.
19. CSV-Export enthält alle gefilterten Zeilen mit den sichtbaren Spalten; danach öffnet die Liste für denselben Werker mit der vorher gewählten Seitengröße (je Rückfrage 10).
20. Einmal-Skript: Trockenlauf zeigt `TREFFER: n` und die Liste mehrdeutiger/unbekannter Matchcodes; Scharflauf schreibt n Werte; zweiter Trockenlauf `TREFFER: 0`.
21. Build und alle Tests grün, inkl. `TeilelisteKuerzelKonstantenWaechterTests`, `ColumnFilterTypesDriftTests`, `SageUserfelderTests` (28 Einträge), `NachlaufManifestDriftTests`, `ResponsiveCssTests`.

## Test-Szenarien

Neues Kapitel **TS-171 „BDE-Teileliste: Fräsen-Pfad, Blechdicke, Lagerbestell-Positionen (v1.99.49)“** in `docs/TESTSZENARIEN.md`, danach Zeile in
[[testszenarien-index]]. Standort **DEV IDEAL**, Regression Merkmal-Pflege zusätzlich **DEV AKE**.

Vorbedingungen: Werkbank mit Kürzel IF, BDE aktiv, „BDE-Teileliste“ und neue Option an; Terminal-Benutzer; aktiver Werker; `Sync:BlechteileFelderNachlesen`
an; mindestens ein Artikel mit Arbeitsschritte-Merkmal „… IF …“; zwei abgeschickte Lagerbestellungen (eine teilgeliefert) mit IF-Artikel,
eine im Entwurf, eine abgeschlossen; FA-Struktur mit IF-Zeilen.

- **TS-171.1 Spalten Einzeln/Summiert** – Zahnrad, Werte, Filter Material (Summiert), Filter Blechdicke `3,00` (AK 1–4).
- **TS-171.2 Merkmal Text pflegen** – anlegen, Artikel bearbeiten, ändern, leeren, 1001 Zeichen; Regression Boolean/Dropdown/Sage an AKE und IDEAL (AK 5, 6).
- **TS-171.3 Einstellung Pfad-Merkmal** – Auswahl, ungültiger Wert abgelehnt (AK 7).
- **TS-171.4 Pfad kopieren** – Kopieren, Einfügen im Explorer (Strg+L), Rückfall ohne Zwischenablage (AK 8).
- **TS-171.5 Blechdicke** – Einstellung leer → „–“ + Hinweis; Name gesetzt → Wert nach Dienst-Lauf (AK 9).
- **TS-171.6 Werkbank-Option + Sichtbarkeit Lagerbestellungen** – Option ohne Teileliste abgewiesen; Status-Matrix Entwurf/abgeschickt/teilgeliefert/abgeschlossen/storniert; KW-Filter (AK 10–12).
- **TS-171.7 Melden Lagerbestell-Zeile** – Einzel mit Ausschuss, Block gemischt mit FA-Zeilen, Summiert; DB-Kontrolle: keine StockMovements/BdeBookings, Lagerbestellung unverändert (AK 13, 17).
- **TS-171.8 Doppelschutz und Abweisung** – zwei Terminals gleichzeitig; Bestellung zwischendurch abschließen (AK 14, 15).
- **TS-171.9 Rücknahme/Storno + Teilmeldungen-Liste** (AK 16, 18).
- **TS-171.10 CSV** – Export, danach Liste neu öffnen: Seitengröße unverändert (AK 19).
- **TS-171.11 Einmal-Skript** – Trockenlauf, Scharf, zweiter Trockenlauf (AK 20).

### Prüfabfragen für den Menschen (vor der Abnahme; Ergebnisse in die Spec)

**P-1 (Sage-DB IDEAL, nur lesen; Rückfrage 1) – Feldname Blechdicke.** Tabellen-/Spaltennamen der Sage-Seite sind im WMS-Code nicht belegt
(unverifiziert); alternativ WMS › Sage-Test › Feldliste (Admin, v1.99.14).
```sql
SELECT c.name, t.name AS typ, c.max_length
FROM sys.columns c JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.KHKArtikel') AND c.name LIKE N'USER[_]%'
  AND (c.name LIKE N'%Blech%' OR c.name LIKE N'%Dicke%' OR c.name LIKE N'%Staerke%' OR c.name LIKE N'%Stärke%');
-- danach Stichprobe mit dem gefundenen Feld:
SELECT TOP 20 Artikelnummer, Matchcode, USER_OSMaterialnummerPN, <Feld> FROM dbo.KHKArtikel WHERE Mandant = <Mandant> AND <Feld> IS NOT NULL;
```
**P-2 (Sage-DB IDEAL; Rückfrage 8) – IF-Artikel und Lagerartikel-Kennzeichen.**
```sql
SELECT a.Artikelnummer, a.Matchcode, a.USER_OSIstLagerartikel, a.USER_OSArbeitsschritte
FROM dbo.KHKArtikel a
WHERE a.Mandant = <Mandant> AND N' ' + ISNULL(a.USER_OSArbeitsschritte, N'') + N' ' LIKE N'% IF %'
ORDER BY a.USER_OSIstLagerartikel, a.Matchcode;
-- Zeilen mit USER_OSIstLagerartikel = 0 erkennt das WMS NICHT als Fräsen-Teil (B5).
```
**P-3 (WMS-DB IDEAL) – Werkbank IF und Nachlese-Schalter.**
```sql
SELECT Id, Name, ArbeitsschrittCode, BdeAktiv, BdeTeileliste, SageMissingSince FROM dbo.ProductionWorkplaces WHERE LTRIM(RTRIM(ArbeitsschrittCode)) = N'IF';
SELECT [Key], [Value] FROM dbo.ServiceSettings WHERE [Key] IN (N'Sync:BlechteileFelderNachlesen', N'Sync:Userfeld:Blechfertigung:USER_OSArbeitsschritte');
```
**P-4 (WMS-DB IDEAL; Rückfrage 8) – IF-Zeilen der Struktur ohne Lagerartikel-Merkmal (Blechdicke/Pfad dort leer).**
```sql
SELECT COUNT(*) AS IfZeilen, SUM(CASE WHEN v.Id IS NULL THEN 1 ELSE 0 END) AS OhneLagerartikelMerkmal
FROM dbo.FaHierarchyNodes n
LEFT JOIN dbo.Articles a ON a.ArticleNumber = n.Artnr
LEFT JOIN dbo.ArticleAttributeValues v ON v.ArticleId = a.Id AND v.ArticleAttributeDefinitionId IN
    (SELECT Id FROM dbo.ArticleAttributeDefinitions WHERE SyncSource = N'Sage' AND SyncFieldName = N'USER_OSIstLagerartikel')
WHERE n.VerschwundenSeit IS NULL AND N' ' + ISNULL(n.Arbeitsschritte, N'') + N' ' LIKE N'% IF %';
```

## Etappen (nur bei epic: true)

Entfällt (`epic: false`). Reihenfolge der Commits siehe „Umsetzungsreihenfolge“. Arbeit im Bündel-Worktree `2026-08-07-ideal-teile-1-5`;
Branch vor Beginn mit `scripts/sync-worktree.ps1` aktuell halten.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt | – | – |

## Deploy

*Vorläufig – der QA-Lauf bestätigt gegen den echten Diff.*

- **Web-App:** ja (Teileliste, Merkmal-Pflege, Einstellungen, Werkbänke, Teilmeldungen, Hilfe).
- **Service:** ja (Blechteile-Nachlese liest das zusätzliche Feld über `BfArtikelFelder`/`SageUserfelder`, beide im Web-Projekt, vom Dienst referenziert; dazu `AppVersion.cs`).
- **Migration:** ja (SQL/164 vor dem Web-Start; additiv bis auf den Index-Neuaufbau, Dienst und Web gestoppt wie üblich).
- **Version (vorläufig):** 1.99.49 in beiden `AppVersion.cs` (Dev-Lauf prüft).
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023):

| Id | Version | Typ | Firma | Phase | Ausführung | Text |
|---|---|---|---|---|---|---|
| `SQL/164` | 1.99.49 | Schema | beide | Schema | automatisch | `164_AddBdeTeilelisteLagerbestellungen.sql`, MigrationId der neuen Migration; Voraussetzung „additiv: Werkbank-Option (Default 0), BdeTeilMeldungen.WarehouseRequisitionItemId NULL + FK, UNIQUE-Filter erweitert, zweiter gefilterter UNIQUE“ |
| `Einmalig/2026-11-xx_v1.99.49_FraesenPfad_Erstimport` | 1.99.49 | Einmalig | IDEAL | Nachher | Trockenlauf | „Legt das Merkmal Fräsen-Pfad an (falls fehlt), setzt BdeTeilelistePfadMerkmal (falls leer) und übernimmt Walters Liste je eindeutigem Matchcode. Mehrdeutige/unbekannte Matchcodes stehen in der Ausgabe – von Hand pflegen.“ Braucht `SQL/164`; Voraussetzung „Web ≥ 1.99.49, DB-Backup“ |
| `Einstellung/v1.99.49-BlechdickeFeld` | 1.99.49 | Einstellung | IDEAL | Nachher | – | „/ServiceSettings: Sage-Benutzerfelder: Blechfertigung › Blechdicke = <Feldname aus P-1/Rückfrage 1> eintragen. Entfällt, wenn der Name bei Umsetzung schon als Default im Code steht.“ |
| `Pruefung/BlechteileNachleseAus` | 1.99.49 | Pruefung | IDEAL | Nachher | anzeigen | Prüfung: `SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N'Sync:BlechteileFelderNachlesen' AND LOWER([Value]) IN (N'true', N'1')) THEN 0 ELSE 1 END`; Text „Blechteile-Nachlese aus: Arbeitsschritte/Material/Blechdicke am Artikel fehlen, Lagerbestell-Positionen erscheinen nicht an der Fräse. /ServiceSettings einschalten (liest nur aus Sage).“ |
| `Pruefung/TeilelisteLagerbestellungenOhneWerkbank` | 1.99.49 | Pruefung | IDEAL | Nachher | anzeigen | Prüfung: `SELECT CASE WHEN COL_LENGTH(N'dbo.ProductionWorkplaces', N'BdeTeilelisteLagerbestellungen') IS NULL THEN 0 WHEN EXISTS (SELECT 1 FROM dbo.ProductionWorkplaces WHERE BdeTeilelisteLagerbestellungen = 1 AND BdeTeileliste = 1 AND BdeAktiv = 1) THEN 0 ELSE 1 END`; Text „Keine Werkbank zeigt Lagerbestellungen in der Teileliste: an der Werkbank der Isolierungsfräse (Kürzel IF) unter Werkbänke › Bearbeiten „BDE-Teileliste“ und „Lagerbestellungen in der Teileliste“ einschalten.“ |
| `v1.99.49-FraesePfadInfo` | 1.99.49 | Info | IDEAL | Nachher | anzeigen | „Clemens/Walter informieren: Teileliste der Isolierungsfräse zeigt bestellte Isolierungen (Badge Lagerbestellung), Spalte Fräsen-Pfad über das Zahnrad einblenden, Pfad kopieren → Explorer Strg+L. Pfad pflegen unter Artikel bearbeiten (Merkmal Fräsen-Pfad). Browser-Cache am Terminal ggf. leeren.“ |

  Kein Leitstand- oder Ordner-Eintrag. Reihenfolge: Schema vor Einmalig; Einstellung und Prüfungen nach dem Start.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

Brain-Pflichten des Dev-Laufs: Hilfe BDE-Teileliste (`Views/Help/Index.cshtml`, Abschnitt ab ca. Z. 1881: Lagerbestell-Zeilen, Pfad kopieren,
Blechdicke) und Artikelmerkmale (Typ Text); Anwender- und Brain-Changelog; [[feature-map]]; [[testszenarien-index]]; Glossar „Teilmeldung“
um Lagerbestell-Positionen ergänzen; neue ADR (Abschnitt Lösung 5); Fallstrick `fallstricke.md:185-190` als überholt kennzeichnen und neu fassen.

## Offene Rueckfragen

1. **(Fachbereich: Benjamin Andl-Braun) Blechdicke – Sage-Feld:** Wie heißt das Benutzerfeld „Blechdicke“ am Sage-Artikel genau (USER_…), und
   in welchem Format steht der Wert (z. B. „3,00“ = mm)? Prüfabfrage **P-1**. Bis zur Antwort baut der Dev-Lauf mit leerer Einstellung; die Spalte
   zeigt „–“, der Name wird beim Deploy eingetragen.
2. **(Fachbereich: Walter Kronberger) Fräsen-Pfad – Ordner oder Datei, wer pflegt?** Die Beispiele (`Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU`)
   haben keine Dateiendung – ist es ein Ordner mit den Programmen oder eine Datei? Wer pflegt den Pfad im WMS? (Artikel bearbeiten braucht die
   Rolle Stammdaten.)
3. **(Gerald) „Datei direkt aus dem WMS öffnen“:** Browser öffnen `Y:\…`/`file://`-Links aus einer Web-Seite in der Regel nicht.
   *Empfehlung A:* Pfad als Text + Knopf „Pfad kopieren“, im Explorer Strg+L, einfügen, Enter – geht an jedem PC ohne Einrichtung.
   *B:* `file://`-Link nur, wenn die IT für die WMS-Adresse eine Edge-Gruppenrichtlinie für Datei-Links aus dem Intranet setzt (Name/Verhalten mit
   IT prüfen, hier nicht verifiziert) – höchstens als spätere Ergänzung. *C:* eigenes URL-Protokoll/Hilfsprogramm je PC – nein (Installation je Gerät).
   Zusätzlich: Ist `Y:` an den Fräsen-PCs gleich verbunden, oder soll der Pfad als UNC-Pfad gepflegt werden?
4. **(Fachbereich: Walter Kronberger) Welche Lagerbestellungen?** Gezeigt werden Positionen abgeschickter und teilgelieferter Bestellungen;
   abgeschlossene und stornierte fallen weg, auch wenn die Fräse sie nicht gemeldet hat. *Empfehlung: ja.*
5. **(Fachbereich: Walter Kronberger) KW-Filter:** Lagerbestell-Positionen haben keinen FE-Termin. *Empfehlung:* immer sichtbar, solange die
   Bestellung offen ist; der KW-Filter wirkt nur auf Auftragsteile. Alternative: nach Abschickdatum filtern.
6. **(Fachbereich: Walter Kronberger) Summiert:** Material und Blechdicke sichtbar, Bemerkung PN, Fertigungsinfo PN und Pfad über das Zahnrad
   einblendbar; Lagerbestell-Positionen zählen im Summiert je Matchcode mit. *Empfehlung: ja, ausblendbar.*
7. **(Fachbereich: Walter Kronberger) „Auch in den anderen BDE-Ansichten“:** gemeint als Teileliste (Einzeln + Summiert) aller Teileliste-Werkbänke,
   nicht die Arbeitsgang-Liste des BDE-Terminals? *Empfehlung: ja* – das Terminal zeigt Arbeitsgänge, keine Teile.
8. **(Gerald) Lagerartikel-Grenze:** Arbeitsschritte, Material und Blechdicke am Artikel liest das WMS nur für Sage-Lagerartikel
   (`USER_OSIstLagerartikel ≠ 0`) und nur mit `Sync:BlechteileFelderNachlesen` an (B5). Isolierungen ohne dieses Kennzeichen erscheinen nicht als
   Lagerbestell-Zeile, auftragsbezogene Teile ohne Kennzeichen zeigen Blechdicke/Pfad trotzdem (Pfad ist manuell), Blechdicke aber „–“.
   *Empfehlung:* so bauen, **P-2** und **P-4** vor der Abnahme laufen lassen; fehlen IF-Isolierungen, die Nachlese als Folgeaufgabe erweitern.
9. **(Gerald) Erstimport Walters Liste (29 Zeilen):** *Empfehlung:* Einmal-Skript mit Trockenlauf und eingebetteter Liste (Matchcode → Pfad), läuft
   gleich auf DEV, Test und Produktion; dafür Walters Liste als Datei (die Mail im Backlog hat nur vier Beispiele). Mehrdeutige oder unbekannte
   Matchcodes werden nur gemeldet. Alternative: Handpflege je System.
10. **(Gerald) Zusatzfund CSV:** Der CSV-Export der Teileliste ruft die Liste mit `pageSize=0` ab, der Controller speichert das als Werker-Filter
    (B15) – danach öffnet die Liste mit „Alle“. Mitbeheben (Speichern nur bei Navigation)? *Empfehlung: ja*, Clemens wird die CSV nutzen.
11. **(Fachbereich: Walter Kronberger) Fräsen-Meldung in der Lagerbestellung sichtbar?** Soll das Lager bzw. der Besteller in der Lagerbestellung
    sehen, dass die Fräse eine Position gemeldet hat? *Empfehlung:* nicht in diesem Schritt; die Teilmeldungen-Liste reicht, Nachrüsten ist eine Spalte.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Blechdicke: Sage-Feldname und Format (P-1)? →
2. Fräsen-Pfad: Ordner oder Datei, wer pflegt? →
3. „Datei direkt öffnen“: A Kopieren / B file-Link mit Richtlinie / C nein; Y: oder UNC? →
4. Lagerbestellungen: abgeschickt + teilgeliefert, abgeschlossene/stornierte weg? →
5. KW-Filter für Lagerbestell-Positionen: immer sichtbar oder nach Abschickdatum? →
6. Summiert-Spalten wie vorgeschlagen, Lagerbestell-Positionen zählen mit? →
7. „Andere BDE-Ansichten“ = Teileliste, nicht BDE-Terminal? →
8. Lagerartikel-Grenze akzeptiert, P-2/P-4 vor Abnahme? →
9. Erstimport per Einmal-Skript (Liste als Datei) oder Handpflege? →
10. CSV-Zusatzfund mitbeheben? →
11. Fräsen-Meldung in der Lagerbestellung sichtbar? →
