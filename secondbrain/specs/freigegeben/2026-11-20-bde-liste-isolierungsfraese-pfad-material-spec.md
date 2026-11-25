---
type: spec
title: BDE-Teileliste – Material/Blechdicke/Bemerkung/Fertigungsinfo, Fräsen-Pfad je Artikel, Lagerbestell-Positionen an der Isolierungsfräse
slug: 2026-11-20-bde-liste-isolierungsfraese-pfad-material-spec
status: InUmsetzung
created: 2026-11-20
updated: 2026-11-25
source_backlog: "[[2026-11-19-bde-liste-isolierungsfraese-pfad-material]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: IDEAL
version_vorschlag: "nächste freie nach 1.99.69 (Stand Bündel-Worktree 2026-11-25, AppVersion.cs:5; der frühere Vorschlag 1.99.49 ist vergeben; Dev-Lauf prüft erneut)"
sql_vorschlag: "164 (Articles.CamPath) und 165 (Lagerbestell-Zeilen) – beide für diese Spec reserviert (bf-auftrag-baugruppen-Spec Z. 62, bde-masken-Spec Z. 440); belegt bis 167 im Bündel-Worktree, 168 an kommissionierauftraege-nachtrag vergeben; Dev-Lauf prüft erneut"
testszenario_vorschlag: "TS-171 (weiter für diese Spec reserviert; testszenarien-index führt TS-170 und TS-173 bis TS-190; Dev-Lauf prüft erneut)"
affected_code:
  - IdealAkeWms/Models/Article.cs (neue Spalte CamPath)
  - IdealAkeWms/Helpers/FraesenPfad.cs (neu – Prüfen/Normalisieren, UNC → file-URI)
  - IdealAkeWms/Views/Shared/_FraesenPfad.cshtml (neu – Link + Pfad kopieren)
  - IdealAkeWms/wwwroot/js/site.js (ein delegierter Handler „Pfad kopieren“)
  - IdealAkeWms/Models/RoleKeys.cs
  - IdealAkeWms/Services/ICurrentUserService.cs
  - IdealAkeWms/Services/CurrentUserService.cs
  - IdealAkeWms/Filters/RequireFraesenPfadPflegenAccessAttribute.cs (neu)
  - IdealAkeWms/Filters/RequireFraesenPfadAktivAttribute.cs (neu)
  - IdealAkeWms/Views/Users/RoleOverview.cshtml
  - IdealAkeWms/Controllers/ArticlesController.cs (Index, Info, Edit GET nur Flags; neue POST-Aktion FraesenPfad; Edit POST unverändert)
  - IdealAkeWms/Views/Articles/Index.cshtml
  - IdealAkeWms/Views/Articles/Info.cshtml
  - IdealAkeWms/Views/Articles/Edit.cshtml
  - IdealAkeWms/Views/Articles/_FraesenPfadPflegen.cshtml (neu – kleines Pflegeformular, auf Info und Edit)
  - IdealAkeWms/Models/ViewModels/ArticleInfoViewModel (CamPath)
  - IdealAkeWms/Data/Repositories/ArticleRepository.cs + IArticleRepository.cs (Spaltenfilter fraesen-pfad, GetFraesenPfadeAsync)
  - IdealAkeWms/Controllers/StockOverviewController.cs
  - IdealAkeWms/Views/StockOverview/Index.cshtml
  - IdealAkeWms/Views/WarehouseRequisitions/Edit.cshtml (neu Spaltenauswahl WarehouseRequisitionItems, Spalte Fräsen-Pfad, tr data-article, JS loadStock)
  - IdealAkeWms/Controllers/WarehouseRequisitionsController.cs (Edit GET – Pfad je Position, Schalter-Flag)
  - IdealAkeWms/Models/ViewModels/WarehouseRequisitionEditViewModel (Item um CamPath, Flag FraesenPfadAktiv)
  - IdealAkeWms/Views/WarehousePicking/Details.cshtml (+ WarehousePickingController)
  - IdealAkeWms/Views/BdeTeileliste/Index.cshtml
  - IdealAkeWms/Views/BdeTeileliste/Summiert.cshtml
  - IdealAkeWms/Services/BdeTeilelisteService.cs
  - IdealAkeWms/Controllers/BdeTeilelisteController.cs (Pfad nur lesend; kein POST FraesenPfad mehr, Antwort 22)
  - IdealAkeWms/Models/ViewModels/BdeTeilelisteViewModel.cs
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (Articles, StockOverview neu, WarehouseRequisitionItems neu, WarehousePickingDetails, BdeTeileliste, BdeTeilelisteSummiert, BdeTeilmeldungen; GetByViewKey)
  - IdealAkeWms.Tests/Views/ViewKeyDriftGuardTests.cs (neuer Fact Besteller-Seite, Muster OffeneBestellungen/WareneingangPositionen)
  - IdealAkeWms/Services/ColumnFilterSyntax.cs (ColumnFilterTypes)
  - IdealAkeWms/Models/BdeTeilMeldung.cs
  - IdealAkeWms/Data/ApplicationDbContext.cs
  - IdealAkeWms/Controllers/BdeBookingsController.cs (Teilmeldungen)
  - IdealAkeWms/Views/BdeBookings/Teilmeldungen.cshtml
  - IdealAkeWms/Models/ProductionWorkplace.cs
  - IdealAkeWms/Controllers/ProductionWorkplacesController.cs
  - IdealAkeWms/Data/Repositories/ProductionWorkplaceRepository.cs (EditableProperties)
  - IdealAkeWms/Views/ProductionWorkplaces/_BdeTeilelisteField.cshtml
  - IdealAkeWms/Models/AppSettingKeys.cs
  - IdealAkeWms/Models/AppSettingDefinitions.cs
  - IdealAkeWms/Services/BfArtikelFelder.cs
  - IdealAkeWms/Services/SageSdata/SageUserfelder.cs
  - IdealAkeWms/Migrations/<neu>_AddArticleCamPath.cs
  - IdealAkeWms/Migrations/<neu>_AddBdeTeilelisteLagerbestellungen.cs
  - SQL/164_AddArticleCamPath.sql
  - SQL/165_AddBdeTeilelisteLagerbestellungen.sql
  - SQL/00_FreshInstall.sql
  - SQL/Einmalig/<Datum>_v<Version>_FraesenPfad_Erstimport.sql
  - scripts/deploy/nachlauf.psd1
  - IdealAkeWms/Views/Help/Index.cshtml
  - IdealAkeWms/Views/Help/Changelog.cshtml
  - IdealAkeWms/AppVersion.cs
  - IDEALAKEWMSService/AppVersion.cs
  - docs/TESTSZENARIEN.md
open_questions:
  - "(1, Fachbereich: Benjamin Andl-Braun) Blechdicke: Feldname USER_OSBlechdicke entschieden (Gerald 2026-11-24); Format/Einheit (z. B. „3,00“ = mm?) offen – P-1 mit diesem Feld."
  - "(2, Fachbereich: Walter Kronberger) Fräsen-Pfad Ordner oder Datei / wer pflegt – Freigabezeile leer; inhaltlich von Walters Antwort, Antwort 12 und Rückfrage 15 überlagert. Bestätigen."
  - "(4, Fachbereich: Andi über Walter) Welche Lagerbestellungen: abgeschickt + teilgeliefert. Rumpf baut die Empfehlung."
  - "(6, Gerald) Summiert-Spalten wie vorgeschlagen (Walter: „passt so“) – Freigabezeile leer."
  - "(7 und 14, Fachbereich: Walter Kronberger) „Andere BDE-Ansichten“ = Teileliste; Kanten/Schweißen reicht Teileliste einschalten? Rumpf baut die Empfehlung."
  - "(8, Gerald) Lagerartikel-Grenze akzeptiert, P-2/P-4 vor Abnahme? Rumpf baut die Empfehlung."
  - "(9, Gerald) Erstimport per Einmal-Skript – Liste liegt vor (179 Zeilen); Rumpf baut das Einmal-Skript."
  - "(10, Gerald) CSV-Zusatzfund mitbeheben? Rumpf baut die Empfehlung (ja)."
  - "(11, Fachbereich: Andi über Walter) Fräsen-Meldung in der Lagerbestellung sichtbar? Rumpf baut die Empfehlung (nein)."
  - "(15, Fachbereich: Walter Kronberger) Pfad-Form: wie Spalte E übernehmen, „ist enthalten in …“ → Pfad des genannten Artikels oder leer? Rumpf: wie Spalte E, „ist enthalten in …“ nur melden."
  - "(16, Fachbereich: Walter Kronberger) Belegestation/Twin Kundenseite pflegen oder leer; Zeile 161 nicht importieren? Rumpf: Zeile 161 nur melden, Belegestation in eigenem Block importieren (bei „leer“ Block streichen)."
  - "(17, Fachbereich: Walter Kronberger) Spalten C/D/G: Bedeutung; Zuschnittmaß im WMS? Rumpf: nicht übernommen."
  - "(26, Gerald, nicht blockierend) Artikelinfo und „Artikel bearbeiten“ sind Detailseiten ohne Spaltenauswahl: Pfad dort bei Schalter an immer sichtbar (Rumpf), oder auch dort erst auf Klick (z. B. eingeklappt)?"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: "Rumpf (alle offenen Punkte wie Vorschlag)"
freigabe_von: Gerald
freigabe_am: 2026-11-25
---

# BDE-Teileliste: Material/Blechdicke/Bemerkung/Fertigungsinfo, Fräsen-Pfad, Lagerbestell-Positionen an der Isolierungsfräse

Quelle: [[2026-11-19-bde-liste-isolierungsfraese-pfad-material]] (Walters Antworten 1–5 gelten als Fachbereichs-Vorgabe).
Baut auf [[2026-11-13-bde-vereinfacht-teileliste-blechfertigung-spec]] (v1.99.32, ADR [[0030-teilmeldung-mit-fachlichem-schluessel-neben-workoperation]])
und [[0033-sage-benutzerfelder-als-einstellung]] auf. Alle Code-Belege beziehen sich auf den Bündel-Worktree `2026-08-07-ideal-teile-1-5`
(B1–B18 Stand v1.99.45, B19–B26 und Nachprüfung Stand v1.99.69 am 2026-11-25). `main` enthält BDE-Teileliste, `BfArtikelFelder` und `SQL/1xx`
nicht – Belege gegen `main` sind für diese Spec nicht möglich. Frühere, durch Antworten überholte Fassungen sind im Rumpf mit
*(Fassung vor 2026-11-25 … überholt, Antwort N)* gekennzeichnet.

## Ziel / Nutzen (das Warum)

Clemens (Isolierungsfräse) filtert heute die Mails der Lagerbestellungen von Hand: Teile, deren Programm unter `…\Gerhard\Listen\…` liegt,
druckt er als Liste für die Fräse, die übrigen schachtelt er. Auftragsbezogene Isolierungen (FA-Struktur, Kürzel IF) sieht er dagegen in der
BDE-Teileliste. Ziel: Lagerteile und Auftragsteile mit Arbeitsschritt IF stehen in **einer** Liste an der Werkbank IF, mit Pfad, Material und
Blechdicke, filterbar und fertig meldbar. Der Pfad ist ein reines WMS-Feld im Artikelstamm (Spalte `Articles.CamPath`, Antwort 19), als
`file://`-Link klickbar (Antwort 21) und je Standort schaltbar (Antwort 20). Nebenbei werden Material, Blechdicke, Bemerkung PN und
Fertigungsinfo PN in allen Teileliste-Ansichten verfügbar.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. Teileliste **Einzeln**: neue Spalten Blechdicke und Fräsen-Pfad (Link + „Pfad kopieren“, nur lesend); Material, Bemerkung PN,
   Fertigungsinfo PN gibt es schon. *(Fassung vor dem zweiten Nachzug 2026-11-25: „Stift zum Pflegen“ – überholt, Antwort 22.)*
2. Teileliste **Summiert**: neue Spalten Material, Blechdicke, Bemerkung PN, Fertigungsinfo PN, Fräsen-Pfad (je Rückfrage 6).
3. **Fräsen-Pfad** als eigene Spalte im Artikelstamm (`Articles.CamPath`, Anzeige „Fräsen-Pfad“), vom Sage-Abgleich nie geschrieben (B19);
   Erstimport aus `WICAM-Pfade.xlsx` per Einmal-Skript. *(Fassung vor 2026-11-25: manuelles Text-Merkmal + AppSetting `BdeTeilelistePfadMerkmal`
   – überholt, Antwort 19.)*
4. **Feature-Schalter** `FraesenPfadAktiv` (AppSetting, Default aus, Antwort 20) und **neue Rolle** `fraesenpfad_pflegen` (Antwort 12).
5. **Gemeinsamer Baustein** Pfad-Anzeige (UNC → `file://`-Link, „Pfad kopieren“ als Rückfall) in den Masken der Stufe 1: Artikel-Liste,
   Artikelinfo, Artikel bearbeiten, BDE-Teileliste (Einzeln + Summiert), Lagerbestellung, Bestandsübersicht (Antwort 21).
6. **Blechdicke**: Sage-Feld `USER_OSBlechdicke` (Antwort 1) in der vorhandenen Blechteile-Nachlese, als Sage-Merkmal am Artikel; fester Name
   im Katalog wie die übrigen `USER_OS…`-Felder (B23), kein Deploy-Eintrag.
7. **Lagerbestell-Positionen** in der Teileliste einer Werkbank mit neuer Option „Lagerbestellungen in der Teileliste“: Positionen offener
   Lagerbestellungen, deren Artikel-Merkmal Arbeitsschritte das Kürzel der Werkbank trägt; Termin = Abschicken + N Arbeitstage (Antwort 13);
   fertig meldbar ohne Lagerbuchung, rücknehmbar.
8. BDE-Buchungen › Teilmeldungen zeigt Lagerbestell-Meldungen erkennbar an.
9. Hilfe, Anwender-Changelog, Version, TS-Kapitel, Manifest.

**Out-of-Scope**
- Keine Lagerbuchung, keine Änderung an Status/Menge der Lagerbestellung, nichts an Sage (Walter Antwort 4).
- BDE-Terminal (Arbeitsgang-Liste, `bde-terminal.js`) bleibt unverändert (Rückfrage 7).
- ~~Kein file-Link, kein Hilfsprogramm zum Öffnen~~ *(überholt, Antworten 18/21: `file://`-Link wird gebaut; ein Hilfsprogramm/eigenes
  Protokoll bleibt ausgeschlossen).*
- **Übrige Listen mit Artikelbezug** (Artikelnummer steht laut Antwort 21 in 28 Views, viele mit Sage-Zeilen ohne Artikel-Entity): Folge-Spec;
  der Dev-Lauf legt dafür eine Aufgabe in `secondbrain/aufgaben/` an. Der Baustein (`_FraesenPfad` + `GetFraesenPfadeAsync`) ist dafür wiederverwendbar.
- Zuschnittmaß (Spalte G der Pfad-Liste), Spalten C/D (Rückfrage 17 offen) – nicht übernommen.
- Automatisches Umschreiben `Y:` → UNC bei der Pflege: nein, die Maske verlangt UNC und nennt die Freigabe im Hinweis (nur das Einmal-Skript ersetzt `Y:`).
- Kein eigener Excel-Export: der vorhandene CSV-Knopf der Liste reicht (siehe Ponytail).
- Erweiterung der Blechteile-Nachlese auf Nicht-Lagerartikel (Rückfrage 8, ggf. Folgeaufgabe).
- „Frage an JOK, ob es weitere Optimierungen gibt“ (Backlog Z. 19) – nicht Teil dieser Spec.
- Mobile Kartenansicht: eigene Spec [[2026-11-18-bde-vereinfacht-mobil-kompakt-spec]] (zurückgestellt), siehe Abhängigkeiten.

## Fachliche Anforderungen

F1. **Spalten Einzeln.** Zusätzlich zu den vorhandenen Spalten (`Index.cshtml:28-49`): **Blechdicke** (sichtbar) und – nur bei
    `FraesenPfadAktiv` – **Fräsen-Pfad** (standardmäßig ausgeblendet, über das Zahnrad einblendbar; Clemens' Terminal blendet sie einmal ein,
    die Wahl gilt je Web-Benutzer). Bemerkung PN und Fertigungsinfo PN bleiben wie heute ausgeblendet einblendbar (`Index.cshtml:221-222`),
    Material bleibt sichtbar (`:212`).

F2. **Spalten Summiert** (Rückfrage 6): Material, Blechdicke sichtbar; Bemerkung PN, Fertigungsinfo PN, Fräsen-Pfad (nur bei Schalter an)
    ausgeblendet einblendbar. Zellwert = die verschiedenen Werte der beitragenden Positionen, sortiert, kommagetrennt (wie Abteilung in Summiert);
    bei Fräsen-Pfad je Wert der Baustein aus F4. Spaltenfilter auf diesen Spalten filtern die Positionen **vor** dem Zusammenzählen (wie heute
    Hauptauftrag/Abteilung).

F3. **Fräsen-Pfad im Artikelstamm** (Antwort 19). Neue Spalte `Articles.CamPath` (nvarchar(500) NULL), Anzeige „Fräsen-Pfad“, reines WMS-Feld:
    der Sage-Artikelabgleich und der AgentJob schreiben nur feste Spaltenlisten und fassen sie nie an (B19). Gültiger Wert: leer (= kein Pfad)
    oder ein UNC-Pfad – getrimmt, beginnt mit `\\`, keine Steuerzeichen, höchstens 500 Zeichen. Alles andere (z. B. `Y:\…`) wird mit Meldung
    abgewiesen: „Bitte als UNC-Pfad eintragen (\\server\freigabe\…). Laufwerk Y: an der Fräse = \\idealfs1\Daten$\Produktion\CAM“ (Antwort 18).
    Eine Prüfregel an **einem** Ort (`Helpers/FraesenPfad.Pruefen`) für den einen Schreibweg der Anwendung (F14). *(Fassung vor dem zweiten
    Nachzug: „für beide Schreibwege“ – überholt, Antworten 22/23.)*
    *(Fassung vor 2026-11-25: Text-Merkmal-Typ unter Stammdaten › Artikelmerkmale, Änderungen an `ArticleAttributesController`,
    `ArticleAttributes/Index`, `ArticleAttributeRepository.SaveValuesAsync`, AppSetting `BdeTeilelistePfadMerkmal` – überholt, Antwort 19.)*

F4. **Pfad öffnen** (Antworten 3, 18, 21). Gemeinsamer Baustein `_FraesenPfad` (Partial) + `FraesenPfad.FileUri` (Helper):
    - UNC-Wert → klickbarer Link `file://<server>/<freigabe>/…` (Backslash → `/`, Leerzeichen/Umlaute/`#`/`%` URL-kodiert als UTF-8, `$` bleibt);
      Linktext = der UNC-Pfad. Beispiel `\\idealfs1\Daten$\Produktion\CAM\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU` →
      `file://idealfs1/Daten$/Produktion/CAM/Gerhard/Listen/EKP/EKP_System_20mm_03_2022_NEU`.
    - Daneben Knopf „Pfad kopieren“ (≥ 44 px in der Teileliste, `bde-touch`) – Rückfall, wenn der Link am PC nicht öffnet (GPO aktiv,
      Test 2026-11-25 nur von einer lokalen Testseite – echte WMS-Adresse am Fräsen-PC im Manual-UAT prüfen, Antwort 18). Kopiert wird der UNC-Text; geht die Zwischenablage nicht, wird der Text markiert und der Hinweis
      „Text markiert – mit Strg+C kopieren“ gezeigt (Muster B26). Tooltip/Hilfe: „Öffnet der Link nicht: Pfad kopieren, im Explorer Strg+L, einfügen, Enter.“
    - Leer → „–“. Ein Wert, der (z. B. aus Altbestand) kein UNC ist, erscheint nur als Text + Kopieren, ohne Link.
    *(Fassung vor 2026-11-25: „Kein `file://`-Link“, Empfehlung A – überholt, Antworten 18/21.)*

F5. **Blechdicke.** Wert aus dem Sage-Feld **`USER_OSBlechdicke`** (Antwort 1), gelesen von der Blechteile-Nachlese, gespeichert als Sage-Merkmal
    am Artikel (Definition legt der Writer selbst an, B23). Fester Name wie die übrigen `USER_OS…`-Felder: Konstante in `BfArtikelFelder` +
    Katalogeintrag mit Default `USER_OSBlechdicke` – ohne Einstellungszeile liest der Dienst sofort (B23); die Einstellung bleibt nach ADR 0033 nur
    zum Umbenennen/Abschalten. Zuordnung zur Zeile über die Artikelnummer (FA-Zeile: `FaHierarchyNode.Artnr`, Lagerbestell-Zeile:
    `WarehouseRequisitionItem.ArticleNumber`). Kein Wert / nicht konfiguriert → „–“. Spaltenfilter als **Zahl** (sonst wäre das Komma in „3,00“
    ein ODER, ADR 0017). Format/Einheit des Sage-Werts ist offen (Rückfrage 1, P-1): angezeigt wird der Merkmal-Text, wie ihn die Nachlese
    schreibt; ergibt P-1 ein anderes Format als „3,00“, passt der Dev-Lauf nur die Anzeige an.
    *(Fassung vor 2026-11-25: Name per leerer Einstellung, beim Deploy einzutragen – überholt, Antwort 1.)*

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
    der Werker sie sicher von Auftragsteilen unterscheidet. **Termin** (Spalte FE-Termin) = `AddBusinessDays(SubmittedAt.Date, N, Feiertage)`
    mit N aus `BdeTeilelisteLagerbestellungArbeitstage` (Default 10; Antwort 5-Nachtrag + 13; Bausteine B25). Der KW-Filter wirkt auf diesen
    Termin wie auf den FE-Termin der Auftragsteile (Zeile sichtbar, wenn der Termin in der gewählten KW-Spanne liegt); fehlt `SubmittedAt`
    (nicht erwartet, einziger Schreiber setzt es beim Abschicken, B25), hat die Zeile keinen Termin und fällt bei gesetztem KW-Filter weg – wie ein
    Auftragsteil ohne FE-Termin. „gemeldete zeigen“ wirkt. *(Fassung vor 2026-11-25: „KW-Filter wirkt auf diese Zeilen nicht“ – überholt, Antwort 5-Nachtrag.)*

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

F13. **Masken Stufe 1** (Antwort 21), alle nur bei `FraesenPfadAktiv`, Anzeige immer über den Baustein F4:

| Maske | Darstellung | Spaltenfilter / Spaltenauswahl |
|---|---|---|
| Artikel-Liste (`Articles/Index`) | Spalte „Fräsen-Pfad“, Standard ausgeblendet | Server-Filter in `ArticleRepository.ApplyArticleColumnFilter` (Key `fraesen-pfad` → `a.CamPath`); Eintrag in `ColumnDefinitions.Articles` (DefaultHidden) und bedingt im `#column-config` der View (Muster der `attr-`-Spalten, `Index.cshtml:72-73,156`); `colspan` (`totalColCount`) mitzählen |
| Artikelinfo (`Articles/Info`) | Zeile „Fräsen-Pfad“ in den Stammdaten (Baustein); darunter für Pflegeberechtigte das Pflegeformular `_FraesenPfadPflegen` (F14) | – (Detailseite) |
| Artikel bearbeiten (`Articles/Edit`) | eigene Karte „Fräsen-Pfad“ **unterhalb** des Stammdaten-Formulars (`Edit.cshtml:17-145`), außerhalb des `<form asp-action="Edit">`: Baustein + `_FraesenPfadPflegen` mit eigenem Knopf „Fräsen-Pfad speichern“ (F14). Das Edit-Formular selbst bekommt **kein** Feld. *(Fassung vor dem zweiten Nachzug: Textfeld im Edit-Formular – überholt, Antwort 23/Lösung 3.)* | – |
| BDE-Teileliste Einzeln/Summiert | Spalte (F1/F2), **nur lesend** *(Stift – überholt, Antwort 22)* | Getter = Zelltext (UNC-Text), Server-Filter vor dem Paging wie die übrigen Spalten; `ColumnDefinitions.BdeTeileliste`/`…Summiert` |
| Lagerbestellung – Lager-Detailseite (`WarehousePicking/Details`) | Spalte „Fräsen-Pfad“, Standard ausgeblendet | Tabelle hat Spaltenauswahl und Client-Filter (`Details.cshtml:67,229-245`): `<th data-filterable data-col-key="fraesen-pfad">` als letzte Spalte (nach „Fehlteil-Status“); Eintrag in `ColumnDefinitions.WarehousePickingDetails` (`DefaultHidden: true`) **und** im handgeschriebenen `#column-config` mit `"defaultHidden": true`, beides bedingt wie `th`/`td` (B29) |
| Lagerbestellung – Besteller-Seite (`WarehouseRequisitions/Edit`) | Spalte „Fräsen-Pfad“, Standard ausgeblendet (Antwort 24) | Tabelle bekommt eine **Spaltenauswahl** (heute keine, `Edit.cshtml:34-40`) nach Muster Wareneingang-Positionen (`Receive.cshtml:116,268,292`; ohne Filter/CSV): Toolbar-Host `data-table-toolbar`, `data-view-key="WarehouseRequisitionItems"`, `#view-config` `{ "viewKey": "WarehouseRequisitionItems", "supportsReorder": false, "supportsSortDefault": false }`, `#column-config`, `column-preferences.js`; neue `ColumnDefinitions.WarehouseRequisitionItems` (`pos` + `article-number` Locked, `description`, `quantity`, `unit`, `storage`, `fraesen-pfad` DefaultHidden) + Eintrag in `GetByViewKey` (`ColumnDefinitions.cs:902`, sonst 400 der Prefs-API, `ViewKeyDriftGuardTests.cs:36-37`). Pfad-Spalte nach „Lagerplatz“, vor der Knopf-Spalte (ohne Key). **JS-Absicherung:** `loadStock` liest die Artikelnummer heute über `row.cells[1]` (`Edit.cshtml:139`) – umgestellt auf `row.dataset.article` mit `data-article="@i.ArticleNumber"` am `<tr>` (Muster `Details.cshtml:89`); damit ist die Spaltenreihenfolge für das JS egal. *(Fassung vor dem dritten Nachzug: „keine neue Spalte, Pfad unter der Bezeichnung“ – überholt, Antwort 24.)* |
| Bestandsübersicht (`StockOverview/Index`) | Spalte „Fräsen-Pfad“, Standard ausgeblendet | Server-Filter in-memory über `_stockOverviewColMap` (`StockOverviewController.cs:21-29,73`) mit Getter aus `GetFraesenPfadeAsync`; die Liste hat **heute keine Spaltenauswahl** (`Index.cshtml:100`, kein `data-view-key`/`#column-config`) – sie bekommt eine: neue `ColumnDefinitions.StockOverview` mit den 6 vorhandenen Spalten + `fraesen-pfad` (DefaultHidden), `data-view-key="StockOverview"`, `#column-config` (Drift-Guard `ViewKeyDriftGuardTests`) |

Alle übrigen Listen mit Artikelbezug: Folge-Spec (Out-of-Scope).

**Regel „Standard ausgeblendet“ (Antwort 24):** In **jeder** Tabelle der Stufe 1 (Artikel-Liste, BDE-Teileliste Einzeln und Summiert,
Lager-Detailseite, Besteller-Seite, Bestandsübersicht) ist „Fräsen-Pfad“ nur über die Spaltenauswahl (Zahnrad) einblendbar und standardmäßig
ausgeblendet. Wirksam ist allein das inline `#column-config` (`column-preferences.js:69,117`, gelesen `:885-886`); `ColumnDef.DefaultHidden`
im Katalog muss gleich stehen. Pflicht je Tabelle also drei Quellen gleich: `<th data-col-key="fraesen-pfad">` + Zelle, JSON-Eintrag mit
`"defaultHidden": true`, `ColumnDef("fraesen-pfad", "Fräsen-Pfad", DefaultHidden: true)` – `th`, `td` und JSON-Eintrag nur bei `FraesenPfadAktiv`
(Muster bedingter Eintrag `Bom.cshtml` „ist-menge“, `ViewKeyDriftGuardTests.cs:44-54`). Ein Benutzer, der die Spalte einmal einblendet, behält
seine Wahl (Prefs-API). Artikelinfo und „Artikel bearbeiten“ sind Detailseiten ohne Spaltenauswahl und zeigen den Pfad bei Schalter an als
Zeile/Karte (Rückfrage 26, nicht blockierend).

F14. **Fräsen-Pfad pflegen** (Antworten 12/19/22/23). **Ein** Schreibweg: neue Aktion `ArticlesController.FraesenPfad` (POST).
     - Pfleger: ein **persönliches Konto** mit Rolle `fraesenpfad_pflegen` (Antwort 22) – das Terminal-Konto der Fräse bekommt sie nicht.
       Gepflegt wird **im Artikelstamm**: auf der Artikelinfo und auf „Artikel bearbeiten“ (F13). In der BDE-Teileliste wird nicht gepflegt.
     - Formular `_FraesenPfadPflegen` (Model: Artikelnummer, aktueller `CamPath`, ReturnUrl): Textfeld „Fräsen-Pfad“ (maxlength 500, vorbelegt),
       Hinweis „UNC-Pfad, Y: = \\idealfs1\Daten$\Produktion\CAM; leer = kein Pfad“, Knopf „Fräsen-Pfad speichern“, Antiforgery-Token. Wird nur
       gerendert, wenn `FraesenPfadAktiv` an **und** `CanPflegeFraesenPfadAsync()` wahr ist; sonst zeigen beide Seiten nur den Baustein (F4).
     - POST `Articles/FraesenPfad(string articleNumber, string? camPath, string? returnUrl)`, `[HttpPost]`, `[ValidateAntiForgeryToken]`,
       Action-Level `[RequireFraesenPfadAktiv]` + `[RequireFraesenPfadPflegenAccess]` (kumulativ zum Klassenfilter, ADR 0006/0011). Ablauf:
       Artikel über `GetByArticleNumberAsync(articleNumber.Trim())` laden (getrackt, B28) – fehlt er → `TempData["WarningMessage"]` „Artikel <Nr>
       nicht gefunden – Pfad nicht gespeichert“; `FraesenPfad.Pruefen(camPath)` – Fehler → `TempData["WarningMessage"]` mit der Meldung aus F3,
       nichts gespeichert; sonst **nur** `CamPath` (leer → NULL) sowie `ModifiedAt = DateTime.Now`, `ModifiedBy = GetDisplayName()`,
       `ModifiedByWindows = GetWindowsUserName()` setzen (Muster `ArticlesController.cs:192-194`), `UpdateAsync`, `TempData["SuccessMessage"]`
       „Fräsen-Pfad gespeichert.“ Redirect auf `returnUrl`, wenn `Url.IsLocalUrl`, sonst `Info?articleNumber=<Nr>`. Unverändert gleicher Wert →
       trotzdem speichern (keine Sonderlogik).
     - **Edit-POST bleibt unverändert** (`ArticlesController.cs:145-214`): er kopiert `CamPath` nicht, das Edit-Formular hat kein Pfad-Feld. Damit
       kann „Artikel bearbeiten“ einen vorhandenen Pfad weder bei Schalter aus noch an noch löschen (B20-Falle entfällt statt bewacht zu werden).
     - Wer pflegen darf: `CanPflegeFraesenPfadAsync()` = `admin`, `masterdata` oder neue Rolle **`fraesenpfad_pflegen`** (Anzeige „Fräsen-Pfad
       pflegen“). Die Rolle erlaubt nur diese eine Aktion; alle übrigen Artikelfelder bleiben `[RequireMasterDataAccess]` (Antwort 23).
     - Zugang: Artikelinfo und „Artikel bearbeiten“ liegen im `ArticlesController` mit Klassenfilter `[RequireMasterDataReadAccess]`
       (`ArticlesController.cs:10`, B27). Ein Konto **nur** mit `fraesenpfad_pflegen` landet dort auf „Zugriff verweigert“. Festlegung (Antwort 25,
       Variante a): der Pfleger bekommt **zusätzlich `masterdata_read`** (Hand-Schritt unter /Roles); kein Code am Klassenfilter. Er erreicht so
       Stammdaten › Artikel / Artikelinfo (`NavigationKatalog.cs:255`, `_Layout.cshtml:345-358`) und pflegt dort über die Artikelinfo; „Artikel
       bearbeiten“ bleibt ihm verschlossen (`[RequireMasterDataAccess]`, `ArticlesController.cs:100,144`). Fehlt `masterdata_read`, meldet das die
       Manifest-Prüfung `Pruefung/FraesenpfadOhneStammdatenLesen` (Deploy) statt still.
     *(Fassung vor dem zweiten Nachzug 2026-11-25, überholt durch Antworten 22/23 – „Zwei Schreibwege“:)*
     - *(überholt)* **Artikel bearbeiten** (Rolle Stammdaten wie bisher, `ArticlesController.cs:100,144`): Feld im Formular; der POST übernimmt `CamPath`
       **nur, wenn `FraesenPfadAktiv` an ist** – sonst steht das Feld nicht im Formular und ein ungeprüftes `existing.CamPath = vm.Article.CamPath`
       würde vorhandene Pfade still löschen (B20). Ungültiger Wert → ModelState-Fehler am Feld, nichts gespeichert.
     - *(überholt)* **Stift in der BDE-Teileliste** an der Pfad-Zelle (Einzeln und Summiert): kleines Modal mit Textfeld (vorbelegt), Speichern = POST
       `BdeTeileliste/FraesenPfad` (Artikelnummer, Pfad, ReturnUrl) → schreibt **nur** `CamPath` + Modified-Felder des Artikels mit dieser
       Artikelnummer (Vergleich getrimmt, Groß/Klein egal wie B18), danach `TempData["SuccessMessage"]`/`["WarningMessage"]` und Redirect auf die
       lokale ReturnUrl. Artikel nicht im WMS-Artikelstamm → Warnung „Artikel <Nr> ist nicht im Artikelstamm – Pfad nicht gespeichert“.
       Filter: Klasse wie heute (`[RequireBdeActive]`, `[RequireBdeUserAccess]`) **plus** Action-Level `[RequireFraesenPfadPflegenAccess]` und
       `[RequireFraesenPfadAktiv]` (ADR 0006/0011, kumulativ). Der Stift erscheint nur, wenn der Benutzer pflegen darf.
     - *(überholt)* Wer pflegen darf: `CanPflegeFraesenPfadAsync()` = `admin`, `masterdata` oder neue Rolle **`fraesenpfad_pflegen`** (Anzeige „Fräsen-Pfad
       pflegen“). Die Rolle erlaubt nichts anderes. Wer sie bekommt (Terminal-Konto der Fräse oder persönliche Konten), ist Rückfrage 22;
       ob sie auch „Artikel bearbeiten“ öffnen darf, Rückfrage 23 (Rumpf: nein).

F15. **Schalter** `FraesenPfadAktiv` (Antwort 20): aus (Default, AKE) = Spalte, Link, Kopieren und Pflegeformular erscheinen in **keiner** Maske,
     die Spaltenauswahl listet „Fräsen-Pfad“ nicht, der POST `Articles/FraesenPfad` leitet mit Warnung um (Filter); gespeicherte Pfade bleiben in
     der DB unverändert (der Edit-POST fasst `CamPath` nie an, F14). Kumulativ zu Rollen (ADR 0011). *(Fassung vor dem zweiten Nachzug: „Feld …
     und Stift“, „POST aus F14“ = Teileliste – überholt, Antworten 22/23.)*

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
| B7 | `ArticleAttributeValue.TextValue` gibt es seit 2026-11-06 – aber nur Sage-Merkmale nutzen es: Anlage Text abgewiesen, Auswahl bietet nur Boolean/Dropdown, Edit-View rendert ein manuelles Text-Merkmal als **Dropdown**, Edit-POST übernimmt `TextValue` nicht, `SaveValuesAsync` wertet nur Boolean/Option als Wert. Ein manuelles Text-Merkmal ginge heute still verloren. *(Seit Antwort 19 ohne Folge für diese Spec – kein Text-Merkmal mehr.)* | `ArticleAttributeValue.cs:13-18`; `ArticleAttributesController.cs:93-98`; `ArticleAttributes/Index.cshtml:206-207`; `Articles/Edit.cshtml:126-136`; `ArticlesController.cs:199-204`; `ArticleAttributeRepository.cs:162-163` |
| B8 | Fallstrick „Text-Merkmale gibt es nur bei FaAttributes, nie bei ArticleAttributes … `ArticleAttributeValue` hat kein TextValue“ ist **überholt** (B7); das „Warum“ (Wert geht still verloren) gilt für manuelle Merkmale weiter. *(Umbau des Fallstricks entfällt in dieser Spec, Antwort 19; als Zusatzfund melden.)* | `fallstricke.md:185-190` |
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
| B19 | **Schreibwege `Articles` (alle):** Sage-Artikelabgleich UPDATE nur Description/Unit/ArticleGroup/ReorderLevel/SagePrimaryStorageLocation/PrimaryStorageLocationId/Matchcode/Modified*, INSERT mit fester Spaltenliste; AgentJob nur INSERT mit Spaltenliste; Web nur `ArticlesController.Create/Edit`; kein Löschweg (`Articles.Remove`/`DELETE FROM Articles` ohne Treffer in `*.cs`). Eine neue NULL-Spalte bleibt beim Abgleich unberührt. | `SageImportService.cs:463-500`; `SQL/AgentJobs/02_Import_Artikel.sql:26-27`; `ArticlesController.cs:87-96,174-196` |
| B20 | `ArticlesController.Edit` POST lädt den Artikel und kopiert Felder **einzeln** (`existing.X = vm.Article.X`); ein Feld, das nicht im Formular steht, käme als `null` an. *(Seit dem zweiten Nachzug: der Edit-POST bekommt keine `CamPath`-Zeile – die Falle entsteht gar nicht, F14.)* | `ArticlesController.cs:174-196` |
| B21 | Zugriff: Artikel-Controller Klasse `[RequireMasterDataReadAccess]`, Create/Edit `[RequireMasterDataAccess]`; `ArticlesApiController` ohne Klassenfilter; Teileliste `[RequireBdeActive]`+`[RequireBdeUserAccess]` und nur mit Terminal des angemeldeten Web-Benutzers. | `ArticlesController.cs:10,78,86,100,144`; `ArticlesApiController.cs:8-10`; `BdeTeilelisteController.cs:17-18,81-91` |
| B22 | Neue Rollen der letzten Versionen: Key in `RoleKeys`, Prüfmethode `HasAnyRoleAsync(Admin, …)`, Filter als `TypeFilterAttribute`, Anlage der Rollen-Zeile **nicht** per Start-Seed (der enthält nur die Altrollen), sondern per Manifest-`Pruefung` mit `Reparatur`. Feature-Gate-Filter: Muster `RequireBlechfertigungAktivAttribute` (MVC: WarningMessage + Startseite, API: 404). | `RoleKeys.cs:30-32`; `CurrentUserService.cs:121-122`; `RequireSchachtlerAccessAttribute.cs:9-29`; `Program.cs:237-271`; `nachlauf.psd1:263-270`; `RequireBlechfertigungAktivAttribute.cs:10-37` |
| B23 | **Muster Sage-Benutzerfelder der Nachlese ist fest:** jedes `USER_OS…`-Feld = Konstante in `BfArtikelFelder` + Katalogeintrag mit Default = Feldname; Zeile fehlt = Default wird gelesen. Die Nachlese fragt alle konfigurierten `SageFelder` generisch ab (Dictionary, kein typisiertes DTO); der Writer legt die Merkmal-Definition selbst an. → Blechdicke braucht keinen Deploy-Eintrag. | `BfArtikelFelder.cs:13-26`; `SageUserfelder.cs:50-56,85`; `SdataArtikelReader.cs:359-364`; `ArtikelBenutzerfelderWriter.cs:40,50-53` |
| B24 | Masken Stufe 1: Artikel-Liste Server-Modus mit `data-view-key="Articles"` und bedingten `#column-config`-Einträgen, Filter-SQL zentral; Bestandsübersicht Server-Filter in-memory, **ohne** Spaltenauswahl; Lagerbestellung Besteller-Seite ohne Spaltenauswahl/Filter, JS greift auf `row.cells[1]` (Nachprüfung v1.99.69: nur `loadStock` – Artikelnummer für die Lagerplatz-Anzeige, `Edit.cshtml:138-148`; das Hinzufügen von Artikeln liest `btn.dataset.art`, `:176,219`); Lager-Detailseite mit Spaltenauswahl (Client-Filter). | `Articles/Index.cshtml:59,72-73,144-159`; `ArticleRepository.cs:169-190`; `ColumnDefinitions.cs:680-697`; `StockOverviewController.cs:21-29,73`; `StockOverview/Index.cshtml:100-108`; `WarehouseRequisitions/Edit.cshtml:35-48,139`; `WarehousePicking/Details.cshtml:67,89-91,232` |
| B25 | Termin-Bausteine: `AddBusinessDays` (Mo–Fr ohne Feiertage), Feiertage über `_holidays.GetHolidayDatesAsync()` wie bei den BF-Terminen; `SubmittedAt` wird nur beim Abschicken gesetzt; KW-Filter der Teileliste über den FE-Termin des Haupt-FA. | `BusinessDayService.cs:29-50`; `BfAuftragWorkflow.cs:217-221`; `WarehouseRequisitionRepository.cs:185`; `BdeTeilelisteService.cs:182-186` |
| B26 | „Kopieren mit Rückfall markieren“ gibt es nur in `hardware-scanner.js`; ein Baustein für sechs Masken braucht den Handler an einer gemeinsamen Stelle. | `hardware-scanner.js:211-218` (`navigator.clipboard` `:216-217`) |
| B27 | **Zugang Artikelseiten (Stand v1.99.69):** Klassenfilter `[RequireMasterDataReadAccess]` → `HasMasterDataReadAccessAsync()` = `admin`, `masterdata_read`, `masterdata`, sonst Redirect „Zugriff verweigert“; Rollen nur direkt am Benutzer (`UserRoles`), keine Vererbung. Artikelinfo hängt im Menü an Stammdaten-Lesen. Derselbe Klassenfilter sitzt auf 11 Controllern (u. a. Kreditoren, Debitoren, Werkbänke, Lagerplätze) – `masterdata_read` öffnet alle lesend. Ein Konto nur mit `fraesenpfad_pflegen` erreicht die Artikelseiten **nicht**. | `ArticlesController.cs:10`; `RequireMasterDataReadAccessAttribute.cs:26-34`; `CurrentUserService.cs:76-89,177-193`; `NavigationKatalog.cs:255`; `_Layout.cshtml:345-358`; `Help/Index.cshtml:546`; Grep `^\[RequireMasterDataReadAccess\]` in `Controllers/` = 11 Dateien |
| B28 | **Schreibbausteine Artikel:** `GetByArticleNumberAsync` liest getrackt (`FirstOrDefaultAsync` ohne `AsNoTracking`), `UpdateAsync` = `_dbSet.Update` + `SaveChangesAsync`; kein Caching-Decorator für `IArticleRepository`. Info löst den Artikel über Artikelnummer bzw. exakten Matchcode auf und kennt im ViewModel nur die Artikelnummer (kein Id). | `ArticleRepository.cs:75-78`; `Repository.cs:40-44`; `Program.cs:74`; `ArticlesController.cs:222-240,290-306` |
| B29 | **Spaltenauswahl (Stand v1.99.69):** `column-preferences.js` liest Spalten und `defaultHidden` **nur** aus dem inline `#column-config`/`#view-config` (`:69,117,885-886`); `ColumnDef.DefaultHidden` im Katalog wirkt allein nicht (handgeschriebenes JSON z. B. `Details.cshtml:232-245`, `Articles/Index.cshtml:152-153`). Ausblenden = `style.display = 'none'` je Zelle, die Zelle bleibt im DOM (`:160-179`); physisches Umordnen der Zellen nur bei `supportsReorder` (`:134-135,220-247`). Ein `cells[n]`-Zugriff bricht also nur durch Umordnen oder eine davor eingefügte Spalte. Neue viewKeys brauchen `GetByViewKey` (`ColumnDefinitions.cs:902`), sonst lehnt die Prefs-API ab (`ViewKeyDriftGuardTests.cs:9-11,36-41`). Vorbild Positionstabelle mit Zahnrad ohne Filter/CSV: `Receive.cshtml:116,268,292`, Test `ViewKeyDriftGuardTests.cs:67-85`. Besteller-Seite: `Edit` GET baut die Positionen ohne Pfad (`WarehouseRequisitionsController.cs:162-188`), Controller `[RequirePickingOrStockOrLagerbestellungAccess]` + `[RequireLagerbestellungAktiv]` (`:10-11`). | siehe Zelle |

### Lösung

**1. Artikel-Zusatzwerte je Request (eine Abfrage je Quelle).** Neuer kleiner Lader im `BdeTeilelisteService`: alle `ArticleAttributeValues`
der Definitionen Material, Fertigungsinfo, Arbeitsschritte, Blechdicke (Sage, über `SyncFieldName` = logischer Name) mit `Article.ArticleNumber`
– **ohne** Artikelnummer-Liste (keine 2100-Parameter-Grenze; Umfang laut L10 rund 1.263 Lagerartikel, `SdataArtikelReader.cs:335`) – und,
nur bei `FraesenPfadAktiv`, die Pfade aus `IArticleRepository.GetFraesenPfadeAsync()` (alle Artikel mit `CamPath IS NOT NULL`, Lösung 3).
Ergebnis: `Dictionary<string, ArtikelZusatz>` (Schlüssel `TrimEnd`, OrdinalIgnoreCase, B18). *(Fassung vor 2026-11-25: Pfad als Merkmal mit Id
aus `BdeTeilelistePfadMerkmal` – überholt, Antwort 19.)* Wird wie heute `beschichtet` als Getter in `SpaltenEinzeln` gereicht (`BdeTeilelisteService.cs:98-115`), damit Zelle und Filter
denselben Text liefern (fallstricke „Getter liefert gerenderten Text“). `ColumnFilterTypes`: `blechdicke` = Number in Index und Summiert.

**2. Summiert-Spalten.** `TeilelisteAggregat` bekommt `Materialien`, `Blechdicken`, `BemerkungenPn`, `FertigungsinfosPn`, `Pfade`
(`VormontageService.FormatTexte`, wie `Abteilungen` `BdeTeilelisteService.cs:216`). Keine Änderung an `SpaltenSummiert` nötig: die neuen Keys
sind Vorfilter (B3). Views + `#column-config` + `ColumnDefinitions.BdeTeilelisteSummiert` ergänzen.

**3. Fräsen-Pfad im Artikelstamm** (F3, F4, F13–F15).
- `Article.CamPath` (`[StringLength(500)]`, `[Display(Name = "Fräsen-Pfad")]`), Migration + `SQL/164` (Abschnitt Migrationen).
- `Helpers/FraesenPfad.cs` (static, eine Stelle): `Pruefen(string?) → (string? Wert, string? Fehler)` (trimmen, leer → null, UNC-Regel F3) und
  `FileUri(string unc) → string?` (null, wenn kein UNC). Für die Kodierung zuerst Standardmittel (`Uri.EscapeDataString` je Segment, danach
  `%24` → `$`); ein kleiner Unit-Test mit den Beispielen aus F4 plus Leerzeichen/Umlaut/`#` legt das Ergebnis fest.
- `IArticleRepository.GetFraesenPfadeAsync()` → `Dictionary<string, string>` (ArticleNumber getrimmt → CamPath, OrdinalIgnoreCase) aller Artikel
  mit Pfad; **eine** Abfrage ohne Parameterliste, genutzt von Teileliste, Bestandsübersicht und Lagerbestellung (Ergebnismenge = gepflegte Pfade,
  Größenordnung der Pfad-Liste: 179 Zeilen).
- `Views/Shared/_FraesenPfad.cshtml` (Model `string?`): „–“ / Link + Kopieren-Knopf (`data-pfad-kopieren="<UNC>"`). Ein delegierter
  Klick-Handler in `site.js` nach Muster B26 – einmal für alle Masken (gemeinsames JS nur einmal anfassen). Rendert nichts Eigenes zum Schalter:
  die Aufrufer binden das Partial nur bei `FraesenPfadAktiv` ein.
- Schalter lesen: je Controller-Aufruf einmal `GetValueAsync(AppSettingKeys.FraesenPfadAktiv)` = „true“ (Muster `RequireBlechfertigungAktivFilter`,
  B22) und als Flag ins ViewModel bzw. `ViewBag`; POST-Gate über neuen `RequireFraesenPfadAktivAttribute`.
- Rolle: `RoleKeys.FraesenpfadPflegen = "fraesenpfad_pflegen"`, `ICurrentUserService.CanPflegeFraesenPfadAsync()` = `HasAnyRoleAsync(Admin,
  MasterData, FraesenpfadPflegen)`, Filter `RequireFraesenPfadPflegenAccessAttribute` (Muster `RequireSchachtlerAccessAttribute`); drei Stellen:
  Attribut, `secondbrain/codebase/controller.md`, `Views/Users/RoleOverview.cshtml` (dort Hinweis „braucht zusätzlich Stammdaten lesen“, B27);
  Rollen-Zeile per Manifest-Prüfung (Deploy).
- Pflege (F14, Antworten 22/23): **eine** neue Aktion `ArticlesController.FraesenPfad` (POST) + Partial `Views/Articles/_FraesenPfadPflegen.cshtml`,
  eingebunden in `Info.cshtml` (Stammdaten-Karte, `Info.cshtml:77-90`) und in `Edit.cshtml` als eigene Karte **nach** dem `</form>`
  (`Edit.cshtml:145`). `Info` und `Edit` GET setzen nur zwei Flags (`FraesenPfadAktiv`, `CanPflegeFraesenPfadAsync()`) und reichen `CamPath`
  durch (`ArticleInfoViewModel.CamPath`; Edit hat `Article.CamPath`). **Edit-POST und `Create` bleiben unverändert.**
  Warum so (ponytail-Leiter): (1) Eine Rollenweiche im großen Edit-POST bräuchte eine zweite Prüfung je Feld plus die B20-Wache – die schmale
  Aktion schreibt nachweislich nur `CamPath` + `Modified*`. (2) Ein Schreibweg statt zwei: dieselbe Aktion bedient Stammdaten-Benutzer auf „Artikel
  bearbeiten“ und Pfleger auf der Artikelinfo; Prüfregel und Audit an einer Stelle. (3) Der Edit-POST fasst `CamPath` nie an – die B20-Falle
  (Schalter aus löscht Pfade) kann nicht entstehen, statt bewacht werden zu müssen. (4) Zugang über die vorhandene Rolle `masterdata_read`
  statt eines neuen Lese-Gates am Klassenfilter (B27, Antwort 25 Variante a).
  *(Fassung vor dem zweiten Nachzug: `ArticlesController.Edit` GET/POST mit B20-Wache plus `BdeTeilelisteController.FraesenPfad` – überholt,
  Antworten 22/23.)*
- Masken nach F13; Artikel-Liste filtert per SQL (`"fraesen-pfad" => ColumnFilterSql.Predicate<Article>(f, a => a.CamPath)`), Bestandsübersicht
  und Teileliste per Getter, Lager-Detailseite per Client-Filter. Besteller-Seite (Antwort 24): `Edit` GET holt bei Schalter an einmal
  `GetFraesenPfadeAsync()` und gibt je Position den Pfad ins Item-ViewModel (Lookup Artikelnummer getrimmt, OrdinalIgnoreCase; kein Treffer, z. B.
  DUMMY-Position → „–“); View: Spaltenauswahl nach F13 + `data-article` am `<tr>`, `loadStock` liest `row.dataset.article` (eine Zeile). Kein
  Filter, kein CSV auf dieser Seite (kleine Positionstabelle, Muster Wareneingang-Positionen). Alle Stufe-1-Tabellen: „Standard ausgeblendet“
  nach der Regel unter F13 (drei Quellen gleich).
*(Fassung vor 2026-11-25 „3. Manuelles Text-Merkmal“ – `ArticleAttributesController.CreateDefinition`, Text-Zweig in `Articles/Edit`, `TextValue`
im Edit-POST, `SaveValuesAsync`, Fallstrick-Umbau B8 – überholt, Antwort 19.)*

**4. Blechdicke.** `BfArtikelFelder`: neue Konstante `Blechdicke = "USER_OSBlechdicke"` (Antwort 1) und in `SageFelder` aufnehmen.
`SageUserfelder.Katalog`: Eintrag `new(Blechfertigung, "USER_OSBlechdicke", "Blechfertigung: Blechdicke", BfWirkung, "IDEAL")` (Katalog 27 → 28,
`SageUserfelderTests` anpassen). Keine Seed-Zeile, kein Manifest-Eintrag: Zeile fehlt = Default wird gelesen (B23).
*(Fassung vor 2026-11-25: leere Seed-Zeile in SQL/164 + Manifest-`Einstellung` zum Nachtragen – überholt, Antwort 1.)*

**5. Lagerbestell-Zeilen.**
- Laden (nur wenn Option wirksam und `LagerbestellungAktiv`): Positionen mit Kopf-Status Submitted/PartiallyDelivered, Join `Articles` über
  Artikelnummer, Filter in C# über das Arbeitsschritte-Merkmal (`Traegt`-Regel auf den Merkmal-Text). `TeilelisteZeile` wird um eine Quelle
  erweitert (`Node` bleibt für FA-Zeilen; Lagerbestell-Zeilen bekommen eine eigene kleine Projektion), Getter je Spalte für beide Arten.
  Sortierung wie heute nach Matchcode.
- Termin (F7): beim Laden je Zeile `IBusinessDayService.AddBusinessDays(SubmittedAt.Value.Date, N, feiertage)` mit `feiertage` aus
  `IHolidayRepository.GetHolidayDatesAsync()` und N = `GetIntValueAsync(BdeTeilelisteLagerbestellungArbeitstage, 10)` (Muster B25). Der Getter
  `fe-termin` liefert ihn im Format der Auftragsteile; der KW-Filter in `Bauen` (`BdeTeilelisteService.cs:182-186`) bekommt für
  Lagerbestell-Zeilen den Vergleich auf diesen Termin statt auf die Haupt-FA-Menge.
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

**7. Pfad kopieren / Link.** Siehe Lösung 3 (Baustein `_FraesenPfad` + ein Handler in `site.js`). *(Fassung vor 2026-11-25: Handler nur in
`bde-teileliste.js`, keine Änderung an `site.js` – überholt, Antwort 21: der Baustein läuft in sechs Masken.)* `bde-teileliste.js` bleibt
unverändert (kein Stift-Modal mehr, Antwort 22; das JS liest keine Formular-Schlüssel, Grep `split` in `bde-teileliste.js` ohne Treffer, Stand v1.99.69).
*(Fassung vor dem zweiten Nachzug: „`bde-teileliste.js` bekommt nur das Stift-Modal“ – überholt.)*

**8. CSV-Seiteneffekt (Rückfrage 10).** Im `Index`-GET nicht speichern, wenn die Anfrage keine Navigation ist (Header `Sec-Fetch-Mode` ≠
`navigate`; fehlt der Header, wie heute speichern). Kein Eingriff in `csv-export.js` (gemeinsamer Code).

### Umsetzungsreihenfolge (ein Branch, je Schritt ein Commit, jeder Schritt baut und testet grün)

| # | Schritt | hängt an |
|---|---|---|
| 1 | Summiert-Spalten Material/Bemerkung/Fertigungsinfo (F2 ohne Blechdicke/Pfad) | – |
| 2 | `Articles.CamPath` (Model, Migration, SQL/164, FreshInstall), `FraesenPfad`-Helper + Test, Schalter `FraesenPfadAktiv`, Rolle + Filter, Baustein `_FraesenPfad` + `site.js`-Handler, Aktion `Articles/FraesenPfad` + `_FraesenPfadPflegen` auf Artikelinfo/Artikel bearbeiten, Artikel-Liste (F3, F4, F13 Artikel-Masken, F14, F15) | – (Zugang Pfleger entschieden, Antwort 25: nur Rollenvergabe) |
| 3 | Teileliste: Spalte Pfad Einzeln/Summiert, nur lesend (F1, F2) *(Stift/POST – überholt, Antwort 22)* | Schritt 2 |
| 4 | Bestandsübersicht (neue Spaltenauswahl) + Lagerbestellung Lager-Detailseite (Spalte) und Besteller-Seite (neue Spaltenauswahl + `data-article`) (F13 Rest) | Schritt 2 *(„Umfang Lagerbestellung: Rückfrage 24“ – erledigt, Antwort 24: beide Seiten)* |
| 5 | Blechdicke (F5) – fester Name `USER_OSBlechdicke` | Format: Rückfrage 1 / P-1 (nur Anzeige) |
| 6 | Schema SQL/165 + Migration, Werkbank-Option (F6), Lagerbestell-Zeilen inkl. Termin + Melden + Summiert + Teilmeldungen (F7–F11), Einstellung Arbeitstage, ADR | – |
| 7 | CSV-Fix (F12), Einmal-Skript Erstimport, Manifest, Hilfe, Changelog, Version, TS-171, Aufgabe Folge-Spec übrige Listen | Liste liegt vor; Inhalt der Liste: Rückfragen 15–17 |

### Ponytail (bewusst nicht gebaut)

- Keine neue Tabelle für den Pfad: eine Spalte am Artikel (Antwort 19; Backlog-Vorgabe „keine separate Datenbank“ bleibt erfüllt).
- Keine neue Tabelle für Lagerbestell-Meldungen: `BdeTeilMeldungen` mit einer Spalte mehr (Rücknahme, Admin-Storno, Liste geschenkt).
- Kein eigener Excel-Export: `csv-export.js` exportiert die Server-Liste mit sichtbaren Spalten; Excel öffnet die CSV (Semikolon, UTF-8-BOM, `csv-export.js:7-8`).
- Kein „Pfad“-Filter-Bedienelement: der Spaltenfilter (enthält `gerhard\listen`) reicht; die Werkbank IF ist selbst schon der IF-Filter.
- Kein Kürzel „IF“ im Code: Werkbank-Option statt Konstante.
- Keine Einstellung für die Freigabe hinter `Y:`: die Pflege verlangt UNC, nur das Einmal-Skript ersetzt das Präfix einmalig.
- Keine Einstellung für den Blechdicke-Feldnamen außer dem ohnehin vorhandenen Katalog-Mechanismus (ADR 0033, B23).
- Kein Index auf `CamPath`: gefiltert wird mit „enthält“ (LIKE `%x%`), die Teileliste lädt `CamPath IS NOT NULL` auf einer Spalte ohne Selektivitätsgewinn.
- Keine Änderung am BDE-Terminal.
- ~~Keine Pflege des Pfads aus der BDE-Liste heraus~~ und ~~Alternative zur AppSetting `BdeTeilelistePfadMerkmal`~~ *(überholt, Antworten 12/19:
  Stift in der Teileliste; keine AppSetting für das Merkmal, weil es kein Merkmal mehr gibt).*
- Keine Pflege aus der BDE-Teileliste (Antwort 22: wieder gestrichen) – `bde-teileliste.js` und `BdeTeilelisteController` bekommen keinen Pflegeweg.
- Keine Rollenweiche im Edit-POST, kein Pfad-Feld im Edit-Formular: eine schmale Aktion `Articles/FraesenPfad` für alle Pflegeberechtigten (F14).
- Kein eigenes Lese-Gate für Pfleger am Artikel-Klassenfilter: vorhandene Rolle `masterdata_read` (B27, Antwort 25); fehlt sie, meldet es eine Manifest-Prüfung.
- Kein Pflege-Stift in der Artikel-Liste: Pflege auf Artikelinfo (Menü Stammdaten › Artikelinfo) und „Artikel bearbeiten“ reicht.

### Abhängigkeiten / Regressionsrisiko

- [[2026-11-18-bde-vereinfacht-mobil-kompakt-spec]] ändert dieselben Views (`Index`, `Summiert`, `_Kopf`) – zurückgestellt. Wer später läuft,
  übernimmt die neuen Spalten (`data-label` an jeder neuen Zelle) bzw. die Kartenansicht.
- `Articles/Edit`, `Articles/Index`, `ArticlesController`, `StockOverview` und die Lagerbestell-Views laufen an **beiden** Standorten: QA prüft an
  AKE (Schalter aus), dass Artikel bearbeiten alle Felder und Merkmale unverändert speichert (Edit-POST unverändert, F14), kein Fräsen-Pfad sichtbar ist und die
  Bestandsübersicht mit neuer Spaltenauswahl wie bisher filtert; die Besteller-Seite der Lagerbestellung (auch Glasbestellung, gleiche View
  `Edit.cshtml:3`) hat an beiden Standorten das neue Zahnrad (ohne Pfad-Eintrag bei Schalter aus) und füllt die Lagerplatz-Spalte wie bisher. *(Fassung vor 2026-11-25: Regression `SaveValuesAsync` – überholt, Antwort 19.)*
- `site.js` ist global: der neue Handler reagiert nur auf `[data-pfad-kopieren]` (delegiert), sonst nichts.
- Recreate des UNIQUE `UX_BdeTeilMeldungen_Schluessel` (Filter erweitert): kurzer Index-Neuaufbau auf kleiner Tabelle; Prüfung auf Dubletten
  nicht nötig, da der Filter nur enger wird. **Konflikt** mit [[2026-11-25-bde-masken-vereinheitlichung-spec]] (SQL/168 baut denselben Index):
  wer als zweiter baut, setzt den vereinigten Filter `[IsCancelled]=0 AND [IstAbschliessend]=1 AND [WarehouseRequisitionItemId] IS NULL` und
  prüft im Guard beide Teile (dort Antwort 12, entschieden).

## Migrations-/SQL-Auswirkungen

Reihenfolge je Schema-Änderung (CLAUDE.md, ADR 0004): Model → `dotnet ef migrations add` → idempotentes `SQL/XX` (DDL je eigener Batch,
Guard) → `__EFMigrationsHistory`-Insert in **separatem** Batch → `SQL/00_FreshInstall.sql` an **zwei** Stellen (Schema-Objekt + `MigrationId`).
Nummern 164/165 sind für diese Spec reserviert (Frontmatter); der Dev-Lauf prüft vor dem Anlegen erneut.

**SQL/164_AddArticleCamPath.sql** + EF-Migration `AddArticleCamPath` (Schritt 2):
1. `IF COL_LENGTH(N'dbo.Articles', N'CamPath') IS NULL ALTER TABLE dbo.Articles ADD CamPath nvarchar(500) NULL;` – kein Default, kein Index.
2. Eigener Batch: `IF NOT EXISTS (… __EFMigrationsHistory WHERE MigrationId = N'<Id>') INSERT …`.
- FreshInstall: Spalte in `CREATE TABLE dbo.Articles` und `MigrationId`.
- Additiv, nicht daten-destruktiv. `SQL/AgentJobs/02_Import_Artikel.sql` braucht keine Änderung (INSERT mit fester Spaltenliste, Spalte ist NULL-fähig, B19).

**SQL/165_AddBdeTeilelisteLagerbestellungen.sql** + EF-Migration `AddBdeTeilelisteLagerbestellungen` (Schritt 6), Stil von `SQL/154`
(COL_LENGTH-/OBJECT_ID-/sys.indexes-Guards):
1. `ProductionWorkplaces.BdeTeilelisteLagerbestellungen bit NOT NULL DEFAULT 0` (Guard `COL_LENGTH`).
2. `BdeTeilMeldungen.WarehouseRequisitionItemId int NULL`, FK `FK_BdeTeilMeldungen_WarehouseRequisitionItems_WarehouseRequisitionItemId`
   ON DELETE NO ACTION, Index `IX_BdeTeilMeldungen_WarehouseRequisitionItemId`.
3. `UX_BdeTeilMeldungen_Schluessel` neu mit Filter `[IsCancelled] = 0 AND [WarehouseRequisitionItemId] IS NULL` (Guard: nur droppen/anlegen,
   wenn `sys.indexes.filter_definition` noch den alten Filter trägt; Konflikt mit SQL/168 siehe Abhängigkeiten).
4. Neu `UX_BdeTeilMeldungen_Lagerbestellung` UNIQUE (`WarehouseRequisitionItemId`, `Kuerzel`) WHERE `[IsCancelled] = 0 AND
   [WarehouseRequisitionItemId] IS NOT NULL`.
5. Eigener Batch: `__EFMigrationsHistory`.
- FreshInstall: Spalten, FK und beide Indizes im Abschnitt BDE-Teileliste + `MigrationId`. Kein Backfill. `SQL/AgentJobs/*` unberührt.
- *(Fassung vor 2026-11-25: Punkt 5 „Seed-Zeile Blechdicke leer“ – überholt, Antwort 1.)*

**Einmal-Skript `SQL/Einmalig/<Datum>_v<Version>_FraesenPfad_Erstimport.sql`** (ADR 0015/0023; Datenquelle
`backlog/anhaenge/2026-11-19-bde-liste-isolierungsfraese-pfad-material/WICAM-Pfade.xlsx`, Befunde im Abschnitt „Pfad-Liste, 2026-11-24“):
- Der Dev-Lauf bettet die 179 Zeilen als `VALUES (Zeile, Matchcode, PfadRoh)` ein (Spalten A und E, Excel-Zeilennummer für die Meldeliste).
  Die Belegestation-Zeilen (`G-BL-…`, 51 Zeilen) stehen in einem eigenen, kommentierten Block, damit sie bei Antwort „leer lassen“ auf
  Rückfrage 16 mit einer Änderung herausfallen. Spalten B, C, D, F, G werden nicht übernommen (Rückfrage 17).
- **Annahme (Rückfrage 15 offen, Vorschlag):** Pfad **wie in Spalte E**, kein Anhängen von `\<Matchcode>.tcn`.
- Umrechnung: getrimmt; beginnt mit `Y:\` (Groß/Klein egal) → `\\idealfs1\Daten$\Produktion\CAM\` + Rest (Antwort 18); beginnt mit `\\` → unverändert;
  sonst **nicht schreiben, nur melden** – das trifft „ist enthalten in …“ (Zeilen 155, 157, 164–166) und Zeile 161 (`Y\` ohne Doppelpunkt).
  Ergebnis > 500 Zeichen → melden.
- Zuordnung: `Articles.Matchcode` getrimmt, exakt (fallstricke „Matchcode ist nicht eindeutig“, `fallstricke.md:469`): 0 Treffer → „unbekannt“,
  > 1 → „mehrdeutig“, Matchcode mehrfach in der Liste mit verschiedenen Pfaden → „doppelt in der Liste“; alle nur melden.
- Geschrieben wird nur, wo `CamPath IS NULL` (Handpflege wird nie überschrieben; abweichender vorhandener Wert → „schon gepflegt“ melden).
  Dadurch ist das Skript ohne Sperre wiederholbar: der zweite Trockenlauf findet `TREFFER: 0`.
- Genau ein `DECLARE @Trockenlauf BIT = 1;`; der Trockenlauf-Zweig gibt `TREFFER: n` (= zu schreibende Zeilen) und die Meldeliste aus; der
  scharfe Zweig setzt `CamPath`, `ModifiedAt = GETDATE()`, `ModifiedBy = N'Einmal-Skript Fräsen-Pfad'`, `ModifiedByWindows = LEFT(SUSER_SNAME(), 200)`.
  Kein `ROLLBACK`, keine sqlcmd-Direktiven (Ausführung Trockenlauf, ADR 0023). Guard: Spalte fehlt (`COL_LENGTH … IS NULL`) → Abbruch mit Meldung.
- *(Fassung vor 2026-11-25: legt Text-Merkmal an, setzt `BdeTeilelistePfadMerkmal`, schreibt `ArticleAttributeValues`, Wiederholungssperre per
  `RAISERROR` – überholt, Antwort 19.)*

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `FraesenPfadAktiv` (AppSetting, Gruppe „IDEAL (AKE bleibt aus)“, Helper `B(...)` wie `BlechfertigungAktiv`, `AppSettingDefinitions.cs:191-194`) – Anzeigename „Fräsen-Pfad anzeigen und pflegen“ | Bool | an/aus (Schalter); nur exakt `true` = an | aus (kein Seed) | An: Fräsen-Pfad erscheint in Artikel-Liste, Artikelinfo und Artikel bearbeiten (je mit Pflegeformular für Berechtigte), BDE-Teileliste (Einzeln/Summiert, nur lesend), Lagerbestellung und Bestandsübersicht (F13). Aus: nirgends sichtbar, Pflege gesperrt; gespeicherte Pfade bleiben erhalten. Kumulativ zu den Rollen (ADR 0011). | Web: Masken F13, POST `Articles/FraesenPfad` (F14) | IDEAL (AKE aus) | – (nur Anzeige im WMS) |
| `BdeTeilelisteLagerbestellungArbeitstage` (AppSetting, Gruppe BDE, Helper `I(...)` wie `BfFeTerminArbeitstage`, `AppSettingDefinitions.cs:206-208`) – Anzeigename „Teileliste: Termin Lagerbestellung (Arbeitstage nach Abschicken)“ | Int | 0–60 (Server prüft) | 10 (Antwort 13; kein Seed, Lesen mit Default) | Termin der Lagerbestell-Zeilen in der Teileliste = Abschickdatum + N Arbeitstage (Mo–Fr, ohne Feiertage); der KW-Filter wirkt auf diesen Termin. | Web: BDE-Teileliste | IDEAL (ohne Wirkung, solange keine Werkbank die Option hat) | – |
| `Sync:Userfeld:Blechfertigung:USER_OSBlechdicke` (ServiceSetting, ADR 0033, Katalogeintrag „Blechfertigung: Blechdicke“) | Text mit Muster `^(USER_[A-Za-z0-9_]{1,100})?$` (`SageUserfelder.cs:21`) | Sage-Feldname am Artikel oder leer | `USER_OSBlechdicke` (Antwort 1; Zeile fehlt = Default, B23 – kein Seed) | Blechteile-Nachlese fragt das Feld bei Sage an und speichert es als Merkmal Blechdicke; leer = nicht gelesen, Spalte „–“. | Dienst: Artikel-Sync (Blechteile-Felder); Web: Teileliste | IDEAL | „Liest aus Sage (nur lesend). Ein falscher Name lässt die ganze Blechteile-Nachlese mit Warnung ausfallen; Merkmale bleiben dann unverändert.“ (gleicher Text wie die übrigen Blechfertigung-Felder) |
| ~~`BdeTeilelistePfadMerkmal`~~ | – | – | – | *Überholt, Antwort 19: der Pfad ist kein Merkmal mehr; die Einstellung wird nicht gebaut.* | – | – | – |
| `ProductionWorkplaces.BdeTeilelisteLagerbestellungen` (Werkbank-Option, keine Einstellung im Sinn von ADR 0020, der Vollständigkeit halber) | Bool je Werkbank | an/aus; an nur mit „BDE-Teileliste“ | aus | Teileliste zeigt Positionen offener Lagerbestellungen mit dem Kürzel der Werkbank und macht sie meldbar. | Werkbänke › Bearbeiten; Teileliste | IDEAL | – |

Abhängig, nicht geändert: `Sync:BlechteileFelderNachlesen` muss an IDEAL **an** sein (B5), `LagerbestellungAktiv` an (Glossar Default true).

## Audit-Feld-Auswirkungen

- `BdeTeilMeldung` (AuditableEntity): Lagerbestell-Meldungen setzen `CreatedAt/By/ByWindows` wie `BdeTeilelisteService.cs:351`; Rücknahme
  `Modified*` über `SetModified` (`:438-443`). Zeitstempel Lokalzeit (ADR 0025).
- `Article` (AuditableEntity): der eine Pflegeweg `Articles/FraesenPfad` (F14) setzt `ModifiedAt = DateTime.Now`, `ModifiedBy`, `ModifiedByWindows`
  aus `ICurrentUserService` (Muster `ArticlesController.cs:192-194`) – mit dem persönlichen Konto des Pflegers (Antwort 22), nicht dem
  Terminal-Konto *(Fassung vor dem zweiten Nachzug: „beide Pflegewege (Artikel bearbeiten, Stift der Teileliste)“ – überholt)*; Einmal-Skript `ModifiedBy = 'Einmal-Skript Fräsen-Pfad'`,
  `ModifiedByWindows = SUSER_SNAME()`. Der Sage-Abgleich überschreibt `Modified*` bei jedem Lauf (B19) – wer den Pfad zuletzt geändert hat, ist
  danach nicht mehr ablesbar; bewusst ohne eigene Historie (keine Anforderung).
- *(Fassung vor 2026-11-25: `ArticleAttributeValue`/`ArticleAttributeDefinition` „Fräsen-Pfad“ – überholt, Antwort 19.)*
- `ProductionWorkplace`: neues Feld fällt unter die vorhandenen `Modified*` des Werkbank-Speicherns; Pflicht: in `EditableProperties` (B14).
- `AppSettings`: wie bestehend über `/Settings`.

**Rollen/Zugriff:** Teileliste unverändert `[RequireBdeActive]` + `[RequireBdeUserAccess]` + Terminal-Pflicht (`BdeTeilelisteController.cs:17-18,87-91`).
Neu sieht das Terminal-Konto der Werkbank mit Option Artikel, Menge, bestellende Werkbank und Kopf-Kommentar fremder Lagerbestellungen –
bewusst, nur lesend, ohne Lagerbestell-Rolle. Admin-Storno unverändert `[RequireBdeAdminAccess]`.
**Neue Rolle `fraesenpfad_pflegen`** (Antworten 12/22/23; Anzeige „Fräsen-Pfad pflegen“): darf **nur** die Aktion `Articles/FraesenPfad`
(Pflegeformular auf Artikelinfo/Artikel bearbeiten), sonst nichts; vergeben an ein persönliches Konto, nicht an das Terminal-Konto. Pflegen dürfen
`admin`, `masterdata`, `fraesenpfad_pflegen` (`CanPflegeFraesenPfadAsync`). Artikel bearbeiten (GET/POST) bleibt `[RequireMasterDataAccess]`
(B21) – alle übrigen Artikelfelder ändert nur Stammdaten-Schreiben. Zugang zu den Artikelseiten: Pfleger bekommt zusätzlich `masterdata_read`
(Klassenfilter, B27; Antwort 25 Variante a, kein Code am Filter), Manifest-Prüfung meldet Pfleger ohne Lesezugang. *(Fassung vor dem zweiten Nachzug: „über den Stift der
Teileliste“ – überholt, Antwort 22.)* Lesen des Pfads: jeder, der die jeweilige Maske sehen darf (keine eigene Leserolle). Alles zusätzlich hinter
`FraesenPfadAktiv` (ADR 0011). Drei Stellen Pflicht: Attribut/Filter, `secondbrain/codebase/controller.md`, `Views/Users/RoleOverview.cshtml`;
Rollen-Zeile in `dbo.Roles` per Manifest-Prüfung (B22). *(Fassung vor 2026-11-25: „Keine neue Rolle“ – überholt, Antwort 12.)*

**Listen-View-Pattern:** keine neue Liste. Neue Spalten mit `data-col-key`, `data-filterable`, `#column-config` = `ColumnDefinitions` (B17),
Getter = Zelltext, Typ-Register für `blechdicke`, Pagination und Server-Spaltenfilter vor dem Paging bleiben (`BdeTeilelisteService.cs:190`).
Fräsen-Pfad in den Listen nach F13 (Artikel-Liste SQL-Filter, Bestandsübersicht neue Spaltenauswahl, Lager-Detailseite Client-Filter wie die
übrigen Spalten der Tabelle, Besteller-Seite neue Spaltenauswahl ohne Filter/CSV – kleine, unpaginierte Positionstabelle einer Bestellung wie
Wareneingang-Positionen); Spalte überall Standard ausgeblendet nach der Regel unter F13 (drei Quellen gleich, `"defaultHidden": true` im JSON);
`ColumnFilterTypesDriftTests`/`ViewKeyDriftGuardTests` grün.

## Akzeptanzkriterien

1. Einzeln: Spalten Blechdicke (sichtbar) und – bei `FraesenPfadAktiv` – Fräsen-Pfad (ausgeblendet) vorhanden; über das Zahnrad ein-/ausblendbar; die Wahl bleibt nach Reload (Prefs-API 200).
2. Summiert: Material und Blechdicke sichtbar, Bemerkung PN, Fertigungsinfo PN, Fräsen-Pfad einblendbar; Zelle zeigt die verschiedenen Werte der Positionen kommagetrennt.
3. Spaltenfilter Material in Summiert filtert die Positionen vor dem Zusammenzählen (Summe Menge sinkt entsprechend).
4. Spaltenfilter Blechdicke `3,00` trifft Zeilen mit Blechdicke 3,00 (Zahlenfilter, kein ODER).
5. Pflegeformular (Schalter an) auf Artikelinfo und auf „Artikel bearbeiten“ (Karte unter dem Stammdaten-Formular): „Fräsen-Pfad speichern“ speichert einen UNC-Wert getrimmt, ändern und leeren (= NULL) funktioniert; `Y:\…`, Text ohne `\\` oder > 500 Zeichen → Warnung mit der Meldung aus F3, nichts gespeichert; `ModifiedAt/By/ByWindows` = persönliches Konto des Speichernden (DB-Kontrolle); alle übrigen Artikelspalten unverändert; Rückkehr auf die Seite, von der gespeichert wurde.
6. „Artikel bearbeiten“ › Speichern (Stammdaten-Formular) lässt einen vorhandenen `CamPath` bei Schalter **an und aus** unverändert (DB-Kontrolle) und speichert alle übrigen Felder und Merkmale wie bisher. Schalter aus: in keiner Maske erscheint Spalte, Pflegeformular, Link oder Kopieren; die Spaltenauswahlen listen „Fräsen-Pfad“ nicht; direkter POST `Articles/FraesenPfad` leitet mit Warnung um, nichts gespeichert (AKE und IDEAL).
7. Rollen (Schalter an): Konto mit `fraesenpfad_pflegen` + `masterdata_read` sieht das Pflegeformular auf der Artikelinfo und speichert; „Artikel bearbeiten“ bleibt ihm „Zugriff verweigert“. Konto nur mit `masterdata_read` sieht den Pfad, aber kein Formular; direkter POST → „Zugriff verweigert“, nichts gespeichert. Konto nur mit `fraesenpfad_pflegen` (ohne Lesezugang) → Artikelinfo „Zugriff verweigert“, und die Manifest-Prüfung `Pruefung/FraesenpfadOhneStammdatenLesen` zählt es. `masterdata`/`admin` pflegen auf beiden Seiten. Die BDE-Teileliste zeigt den Pfad nur lesend, ohne Stift. Artikelnummer unbekannt (manipulierter POST) → Warnung, nichts gespeichert. Die Rolle steht in der Rollenübersicht mit Hinweis „braucht zusätzlich Stammdaten lesen“. *(Fassung vor dem zweiten Nachzug: „Stift in der Teileliste …“ – überholt, Antwort 22.)*
8. Baustein (alle Masken F13): UNC-Wert erscheint als Link `file://server/freigabe/…` (Leerzeichen/Umlaute kodiert, `$` erhalten – Unit-Test mit den Beispielen aus F4); Klick am Fräsen-PC öffnet den Ordner im Explorer (Manual-UAT); „Pfad kopieren“ legt den UNC-Text in die Zwischenablage, ohne Zwischenablage-Recht wird markiert und der Hinweis gezeigt; leer → „–“.
9. Blechdicke: nach Nachlese-Lauf ohne Einstellungszeile wird `USER_OSBlechdicke` gelesen und die Teileliste zeigt den Sage-Wert; mit leerer Einstellungszeile „–“ und im Aktivitäts-Protokoll der Hinweis „nicht konfiguriert“, ohne Abbruch der Nachlese.
9a. Masken Stufe 1 (Schalter an): Artikel-Liste, Bestandsübersicht und Lager-Detailseite haben die Spalte „Fräsen-Pfad“ (Standard aus, einblendbar, Filter `gerhard\listen` trifft); Artikelinfo zeigt den Pfad; die Besteller-Seite hat ein Zahnrad, „Fräsen-Pfad“ ist dort Standard aus und einblendbar (Wahl bleibt nach Reload), Pos und Artikel-Nr lassen sich nicht ausblenden; die Lagerplatz-Spalte der Besteller-Seite füllt sich weiterhin für jede Position (JS über `data-article`), auch mit eingeblendetem Pfad; Artikel hinzufügen, Menge ändern, Entfernen laufen wie vorher; die Bestandsübersicht filtert und blättert wie vorher. *(Fassung vor dem dritten Nachzug: „Besteller-Seite zeigt den Pfad … fügt Artikel per Nummer ein (JS `cells[1]` unverändert)“ – überholt, Antwort 24; `cells[1]` diente nur der Lagerplatz-Anzeige, B24.)*
9b. „Standard ausgeblendet“ überall: Ein Benutzer ohne gespeicherte Spaltenwahl sieht „Fräsen-Pfad“ in keiner der sechs Tabellen (Artikel-Liste, Teileliste Einzeln, Summiert, Lager-Detailseite, Besteller-Seite, Bestandsübersicht), findet sie aber in jedem Zahnrad. Schalter aus: kein Zahnrad listet sie.
10. Werkbank-Option nur speicherbar mit „BDE-Teileliste“ an; Wert bleibt nach Speichern erhalten.
11. Mit Option an: abgeschickte und teilgelieferte Lagerbestell-Positionen, deren Artikel-Arbeitsschritte das Kürzel tragen, erscheinen mit Badge „Lagerbestellung <Nr>“; Entwurf, abgeschlossene und stornierte nicht; Artikel ohne Kürzel nicht; mit Option aus oder `LagerbestellungAktiv` aus keine.
12. Lagerbestell-Zeile zeigt Termin = Abschickdatum + 10 Arbeitstage (ohne Wochenende/Feiertage; mit Einstellung 5 entsprechend); KW-Filter zeigt sie nur, wenn dieser Termin in der KW-Spanne liegt; „gemeldete zeigen“ aus blendet gemeldete aus. *(Fassung vor 2026-11-25: „KW-Filter blendet nicht aus“ – überholt, Antwort 5-Nachtrag/13.)*
13. Einzelmeldung (Gut/Ausschuss) und Blockmeldung einer Lagerbestell-Zeile legen je eine `BdeTeilMeldung` mit `WarehouseRequisitionItemId` an; keine `StockMovement`, keine `BdeBooking`, Status/Mengen der Lagerbestellung unverändert, keine Sage-Warteschlange.
14. Zweite Meldung derselben Position/Kürzel (zweites Terminal, Doppelklick) → „bereits gemeldet von … um …“, keine zweite Zeile (Manual-UAT für den UNIQUE).
15. Melden einer inzwischen abgeschlossenen oder stornierten Bestellung → abgewiesen mit Grund, Rest der Sammelmeldung läuft.
16. Rücknahme durch den Werker (bis Frist) und Admin-Storno mit Grund funktionieren für Lagerbestell-Meldungen; die Zeile ist danach wieder offen.
17. FA-Zeilen unverändert: Meldung, Baugruppen-AG, Waisen-Zähler, „bereits gemeldet“ verhalten sich wie v1.99.45 (bestehende Tests grün); eine Lagerbestell-Meldung zählt nie als Waise.
18. Teilmeldungen-Liste zeigt Spalte „Lagerbestellung“; Hauptauftrag „–“ bei Lagerbestell-Meldungen; Filter auf beiden Spalten wirkt.
19. CSV-Export enthält alle gefilterten Zeilen mit den sichtbaren Spalten; danach öffnet die Liste für denselben Werker mit der vorher gewählten Seitengröße (je Rückfrage 10).
20. Einmal-Skript: Trockenlauf zeigt `TREFFER: n` und die Meldeliste (unbekannt, mehrdeutig, doppelt in der Liste, ohne gültigen Pfad – mind. Zeilen 155, 157, 161, 164–166 –, schon gepflegt); `Y:\…` wird als `\\idealfs1\Daten$\Produktion\CAM\…` geschrieben; Scharflauf schreibt n Werte; ein vorher von Hand gepflegter Pfad bleibt; zweiter Trockenlauf `TREFFER: 0`.
21. Build und alle Tests grün, inkl. `TeilelisteKuerzelKonstantenWaechterTests`, `ColumnFilterTypesDriftTests`, `ViewKeyDriftGuardTests`, `SageUserfelderTests` (28 Einträge), `NachlaufManifestDriftTests`, `ResponsiveCssTests`, neuer Unit-Test `FraesenPfad` (Prüfen + FileUri), neuer Controller-Test `ArticlesController.FraesenPfad` (InMemory: gültig →
    nur `CamPath` + `Modified*` geändert, übrige Spalten gleich; ungültig → nichts geändert, WarningMessage; leer → NULL); neuer View-Test
    Besteller-Seite in `ViewKeyDriftGuardTests` (Muster `:56-85`): `th`-Keys = `#column-config`-Keys = `ColumnDefinitions.WarehouseRequisitionItems`,
    `fraesen-pfad` mit `"defaultHidden": true`, `supportsReorder` false, Toolbar-Host vorhanden, kein `row.cells[` mehr im View; je Stufe-1-Tabelle
    steht `fraesen-pfad` im Katalog mit `DefaultHidden: true` und im View-JSON mit `"defaultHidden": true`.

## Test-Szenarien

Neues Kapitel **TS-171 „Fräsen-Pfad, Blechdicke, Lagerbestell-Positionen in der BDE-Teileliste (v<Version>)“** in `docs/TESTSZENARIEN.md`, danach Zeile in
[[testszenarien-index]]. Standort **DEV IDEAL**, Regression Schalter aus / Artikel bearbeiten / Bestandsübersicht zusätzlich **DEV AKE**.

Vorbedingungen: Werkbank mit Kürzel IF, BDE aktiv, „BDE-Teileliste“ und neue Option an; Terminal-Benutzer; aktiver Werker; `Sync:BlechteileFelderNachlesen`
an; `FraesenPfadAktiv` an (IDEAL); persönliche Konten: A `fraesenpfad_pflegen` + `masterdata_read`, B nur `masterdata_read`, C nur
`fraesenpfad_pflegen`, D `masterdata` *(Fassung vor dem zweiten Nachzug: „nur mit `bde_user` + `fraesenpfad_pflegen` …“ – überholt, Antwort 22)*;
mindestens ein Artikel mit Arbeitsschritte-Merkmal „… IF …“; zwei abgeschickte Lagerbestellungen (eine teilgeliefert) mit IF-Artikel,
eine im Entwurf, eine abgeschlossen; FA-Struktur mit IF-Zeilen; Einmal-Skript gelaufen (TS-171.11) oder Pfade von Hand gepflegt.

- **TS-171.1 Spalten Einzeln/Summiert** – Zahnrad, Werte, Filter Material (Summiert), Filter Blechdicke `3,00` (AK 1–4).
- **TS-171.2 Fräsen-Pfad pflegen (Artikelinfo und Artikel bearbeiten)** – mit Konto A auf der Artikelinfo, mit Konto D auf „Artikel bearbeiten“ (Karte unter dem Formular): UNC speichern, ändern, leeren; `Y:\…` und 501 Zeichen abgewiesen; DB-Kontrolle: nur `CamPath` + `Modified*` geändert, `ModifiedBy` = persönliches Konto; danach mit Konto D das Stammdaten-Formular speichern → `CamPath` unverändert (AK 5, 6).
- **TS-171.3 Schalter aus (DEV AKE und IDEAL)** – kein Pfad, kein Formular in keiner Maske; Artikel bearbeiten speichern lässt `CamPath` unverändert (DB); POST `Articles/FraesenPfad` direkt (z. B. Formular aus Schalter-an-Sitzung erneut absenden) → Warnung, nichts gespeichert (AK 6).
- **TS-171.4 Link und Pfad kopieren** – Link am Fräsen-PC öffnet den Ordner; Pfad mit Leerzeichen/Umlaut; Kopieren → Explorer Strg+L; Rückfall ohne Zwischenablage (AK 8).
- **TS-171.4a Rolle und Zugang** – Konten A–D: Formular sichtbar ja/nein, Speichern, „Artikel bearbeiten“ für A verweigert, direkter POST mit B abgewiesen, C kommt nicht auf die Artikelinfo und erscheint in der Manifest-Prüfung `Pruefung/FraesenpfadOhneStammdatenLesen`; Teileliste am Terminal zeigt den Pfad ohne Stift; Rollenübersicht mit Hinweis (AK 7). *(Fassung vor dem zweiten Nachzug: „Stift + Rolle“ – überholt, Antwort 22.)*
- **TS-171.4b Masken Stufe 1** – mit einem Benutzer ohne gespeicherte Spaltenwahl: Artikel-Liste, Bestandsübersicht (neue Spaltenauswahl, Filter, Blättern), Lager-Detailseite, Besteller-Seite (neues Zahnrad; Pfad einblenden, Reload; Lagerplatz-Spalte gefüllt; Artikel hinzufügen/Menge/Entfernen im Entwurf), Teileliste Einzeln/Summiert – überall „Fräsen-Pfad“ zuerst ausgeblendet, im Zahnrad vorhanden; Artikelinfo zeigt den Pfad (AK 9a, 9b). *(Fassung vor dem dritten Nachzug: „Besteller-Seite (Artikel per Nummer hinzufügen läuft weiter)“ – überholt, Antwort 24.)*
- **TS-171.5 Blechdicke** – ohne Einstellungszeile → Wert aus `USER_OSBlechdicke` nach Dienst-Lauf; Zeile leer → „–“ + Hinweis (AK 9).
- **TS-171.6 Werkbank-Option + Sichtbarkeit Lagerbestellungen** – Option ohne Teileliste abgewiesen; Status-Matrix Entwurf/abgeschickt/teilgeliefert/abgeschlossen/storniert; Termin = Abschicken + 10 AT über einen Feiertag; KW-Filter mit und ohne Treffer (AK 10–12).
- **TS-171.7 Melden Lagerbestell-Zeile** – Einzel mit Ausschuss, Block gemischt mit FA-Zeilen, Summiert; DB-Kontrolle: keine StockMovements/BdeBookings, Lagerbestellung unverändert (AK 13, 17).
- **TS-171.8 Doppelschutz und Abweisung** – zwei Terminals gleichzeitig; Bestellung zwischendurch abschließen (AK 14, 15).
- **TS-171.9 Rücknahme/Storno + Teilmeldungen-Liste** (AK 16, 18).
- **TS-171.10 CSV** – Export, danach Liste neu öffnen: Seitengröße unverändert (AK 19).
- **TS-171.11 Einmal-Skript** – vorher einen Artikel der Liste von Hand mit anderem Pfad pflegen; Trockenlauf (Meldeliste prüfen), Scharf, zweiter Trockenlauf (AK 20).

### Prüfabfragen für den Menschen (vor der Abnahme; Ergebnisse in die Spec)

**P-1 (Sage-DB IDEAL, nur lesen; Rückfrage 1) – Format/Einheit von `USER_OSBlechdicke`.** Feldname entschieden (Antwort 1); Typ und Format
sind im WMS-Code nicht belegt (unverifiziert); alternativ WMS › Sage-Test › Feldliste (Admin, v1.99.14).
```sql
SELECT c.name, t.name AS typ, c.max_length, c.precision, c.scale
FROM sys.columns c JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.KHKArtikel') AND c.name = N'USER_OSBlechdicke';
SELECT TOP 20 Artikelnummer, Matchcode, USER_OSMaterialnummerPN, USER_OSBlechdicke
FROM dbo.KHKArtikel WHERE Mandant = <Mandant> AND USER_OSBlechdicke IS NOT NULL;
-- Ergebnis: leere Treffermenge bei der ersten Abfrage = Feld heißt anders -> Rückfrage 1 erneut an Benjamin.
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

- **Web-App:** ja (Teileliste, Artikel-Masken, Bestandsübersicht, Lagerbestellung, Rolle, Einstellungen, Werkbänke, Teilmeldungen, Hilfe).
- **Service:** ja (Blechteile-Nachlese liest das zusätzliche Feld über `BfArtikelFelder`/`SageUserfelder`, beide im Web-Projekt, vom Dienst referenziert; dazu `AppVersion.cs`).
- **Migration:** ja (SQL/164 und SQL/165 vor dem Web-Start; additiv bis auf den Index-Neuaufbau in SQL/165, Dienst und Web gestoppt wie üblich; nicht daten-destruktiv).
- **Version (vorläufig):** nächste freie nach 1.99.69 in beiden `AppVersion.cs` (Dev-Lauf prüft); `<V>` unten = diese Version.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023):

| Id | Version | Typ | Firma | Phase | Ausführung | Text |
|---|---|---|---|---|---|---|
| `SQL/164` | `<V>` | Schema | beide | Schema | automatisch | `164_AddArticleCamPath.sql`, MigrationId der Migration `AddArticleCamPath`; Voraussetzung „additiv: Articles.CamPath nvarchar(500) NULL“ |
| `SQL/165` | `<V>` | Schema | beide | Schema | automatisch | `165_AddBdeTeilelisteLagerbestellungen.sql`, MigrationId der neuen Migration; Voraussetzung „additiv: Werkbank-Option (Default 0), BdeTeilMeldungen.WarehouseRequisitionItemId NULL + FK, UNIQUE-Filter erweitert, zweiter gefilterter UNIQUE“ |
| `Pruefung/RolleFraesenpfadPflegen` | `<V>` | Pruefung | beide | Haltepunkt | Reparatur | Prüfung und Reparatur 1:1 nach `Pruefung/RolleSchachtler` (`nachlauf.psd1:263-270`: `COUNT(*)` fehlender Keys; `INSERT INTO dbo.Roles ([Key], [Name], [Description], [IsSystem], [SortOrder], [Created*]) … WHERE NOT EXISTS`) mit `fraesenpfad_pflegen` / „Fräsen-Pfad pflegen“ / „Fräsen-Pfad am Artikel ändern (Artikelinfo, Artikel bearbeiten); braucht zusätzlich Stammdaten lesen; sonst keine Rechte.“; Text „Rolle fraesenpfad_pflegen fehlt (kein Seed). Zuweisen an das persönliche Konto des Pflegers (zusammen mit Stammdaten lesen) bleibt Hand-Schritt unter /Roles.“ *(Fassung vor dem zweiten Nachzug: Beschreibung „Stift in der BDE-Teileliste“ – überholt, Antwort 22.)* |
| `Pruefung/FraesenpfadOhneStammdatenLesen` | `<V>` | Pruefung | IDEAL | Nachher | anzeigen | Prüfung: `SELECT COUNT(*) FROM dbo.UserRoles ur JOIN dbo.Roles r ON r.Id = ur.RoleId WHERE r.[Key] = N'fraesenpfad_pflegen' AND NOT EXISTS (SELECT 1 FROM dbo.UserRoles u2 JOIN dbo.Roles r2 ON r2.Id = u2.RoleId WHERE u2.UserId = ur.UserId AND r2.[Key] IN (N'admin', N'masterdata', N'masterdata_read'))` (Tabellen `00_FreshInstall.sql:1006-1037`, Keys `RoleKeys.cs:5-7`); Text „Benutzer mit Fräsen-Pfad pflegen ohne Stammdaten lesen erreichen die Artikelinfo nicht – unter /Roles zusätzlich Stammdaten lesen zuweisen.“ Läuft vor der Rollenvergabe mit 0 durch; gilt bei jedem späteren Update weiter. |
| `Einmalig/<Datum>_v<V>_FraesenPfad_Erstimport` | `<V>` | Einmalig | IDEAL | Nachher | Trockenlauf | „Übernimmt die Fräsen-Pfade aus WICAM-Pfade.xlsx je eindeutigem Matchcode in Articles.CamPath (nur leere; Y:\ → \\idealfs1\Daten$\Produktion\CAM). Unbekannte, mehrdeutige, doppelte Matchcodes und Zeilen ohne gültigen Pfad stehen in der Ausgabe – von Hand pflegen.“ Braucht `SQL/164`; Spur `SELECT COUNT(*) FROM dbo.Articles WHERE ModifiedBy = N'Einmal-Skript Fräsen-Pfad'`; Voraussetzung „Web ≥ `<V>`, DB-Backup; vor SCHARF Antworten auf Rückfragen 15/16 prüfen“ |
| `Einstellung/<V>-FraesenPfadAktiv` | `<V>` | Einstellung | IDEAL | Nachher | – | „/Settings › IDEAL: Fräsen-Pfad anzeigen und pflegen einschalten (nach dem Erstimport). AKE bleibt aus.“ |
| ~~`Einstellung/v1.99.49-BlechdickeFeld`~~ | – | – | – | – | – | *Entfällt (Antwort 1: fester Default `USER_OSBlechdicke`, B23).* |
| `Pruefung/BlechteileNachleseAus` | `<V>` | Pruefung | IDEAL | Nachher | anzeigen | Prüfung: `SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N'Sync:BlechteileFelderNachlesen' AND LOWER([Value]) IN (N'true', N'1')) THEN 0 ELSE 1 END`; Text „Blechteile-Nachlese aus: Arbeitsschritte/Material/Blechdicke am Artikel fehlen, Lagerbestell-Positionen erscheinen nicht an der Fräse. /ServiceSettings einschalten (liest nur aus Sage).“ |
| `Pruefung/TeilelisteLagerbestellungenOhneWerkbank` | `<V>` | Pruefung | IDEAL | Nachher | anzeigen | Prüfung: `SELECT CASE WHEN COL_LENGTH(N'dbo.ProductionWorkplaces', N'BdeTeilelisteLagerbestellungen') IS NULL THEN 0 WHEN EXISTS (SELECT 1 FROM dbo.ProductionWorkplaces WHERE BdeTeilelisteLagerbestellungen = 1 AND BdeTeileliste = 1 AND BdeAktiv = 1) THEN 0 ELSE 1 END`; Text „Keine Werkbank zeigt Lagerbestellungen in der Teileliste: an der Werkbank der Isolierungsfräse (Kürzel IF) unter Werkbänke › Bearbeiten „BDE-Teileliste“ und „Lagerbestellungen in der Teileliste“ einschalten.“ |
| `v<V>-FraesePfadInfo` | `<V>` | Info | IDEAL | Nachher | anzeigen | „Clemens/Walter informieren: Teileliste der Isolierungsfräse zeigt bestellte Isolierungen (Badge Lagerbestellung, Termin = Abschicken + 10 Arbeitstage), Spalte Fräsen-Pfad über das Zahnrad einblenden, Link öffnet den Ordner (sonst Pfad kopieren → Explorer Strg+L). Pfad pflegen: der zuständige Pfleger mit eigenem Konto (Rollen Fräsen-Pfad pflegen + Stammdaten lesen) über Stammdaten › Artikelinfo, Stammdaten-Benutzer auch unter Artikel bearbeiten; in der Teileliste nur Anzeige; nur UNC-Pfade. Meldeliste des Erstimports nachpflegen. Browser-Cache am Terminal ggf. leeren.“ |

  Kein Leitstand- oder Ordner-Eintrag. Reihenfolge: Schema (164 vor 165) vor Rollen-Prüfung vor Einmalig; Einstellung `FraesenPfadAktiv` nach
  dem Erstimport; übrige Prüfungen nach dem Start. Die Arbeitstage-Einstellung braucht keinen Eintrag (Default 10 ohne Seed).
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

Brain-Pflichten des Dev-Laufs: Hilfe BDE-Teileliste (`Views/Help/Index.cshtml`, Abschnitt ab ca. Z. 1881: Lagerbestell-Zeilen inkl. Termin,
Fräsen-Pfad Link/Kopieren – nur Anzeige, Blechdicke) und Artikelstamm/Artikelinfo (Pflegeformular Fräsen-Pfad, UNC-Regel, Rollen Fräsen-Pfad
pflegen + Stammdaten lesen; Abschnitt um `Help/Index.cshtml:546`); Anwender- und Brain-Changelog; [[feature-map]];
[[testszenarien-index]]; Glossar „Teilmeldung“ um Lagerbestell-Positionen und neuer Begriff „Fräsen-Pfad“ (WMS-Feld, nicht aus Sage);
Rolle `fraesenpfad_pflegen` in `secondbrain/codebase/controller.md` + `datenmodell.md` (`Articles.CamPath`); neue ADR (Abschnitt Lösung 5);
Aufgabe „Fräsen-Pfad in den übrigen Artikel-Listen (Folge-Spec)“ in `secondbrain/aufgaben/`. *(Fassung vor 2026-11-25: Hilfe Artikelmerkmale Typ
Text und Fallstrick-Umbau `fallstricke.md:185-190` – überholt, Antwort 19.)*

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

### Walter Kronberger, 2026-11-23 (per Mail, über Gerald)

| Frage | Antwort Walter (sinngemäß, Kernsätze wörtlich) | Folge für die Spec (Vorschlag, Gerald entscheidet bei Schranke 1) |
|---|---|---|
| 2 Ordner/Datei, wer pflegt | „Der Pfad im WMS wird auf alle Fälle von den Zuständigen der Fräsmaschine gepflegt.“ Derzeit ein **Ordner**, darin je Teil ein Fräsprogramm mit dem Matchcode als Namen; kompletter Pfad z. B. `Y:\Gerhard\Listen\EKP\EKP_System_20mm_03_2022_NEU\<Matchcode>.tcn`. | Gepflegt wird der **Ordner** je Artikel (Merkmal Fräsen-Pfad). Zelle/„Pfad kopieren“ liefern `<Ordner>\<Matchcode>.tcn` (ein `\` zwischen Ordner und Name, kein doppelter). F3/F4/AK 8 anpassen. Pfleger brauchen heute die Rolle Stammdaten (`[RequireMasterDataAccess]`) – siehe neue Rückfrage 12. |
| 3 Datei öffnen, Y: | „File Explorer öffnen über Websiten ist immer etwas schwierig – versuchen können wir es.“ Nur **eine** Fräsmaschine, `Y:` ist deren Laufwerk. | Kein Bedarf an UNC/Mehrfach-PC. Ob zusätzlich zu A ein Öffnen-Versuch (B, `file://` + Edge-Richtlinie der IT) gebaut wird, bleibt Geralds Entscheid (Freigabe 3). Da es nur einen PC gibt, ist B einmalig einzurichten; Kopieren bleibt als Rückfall Pflicht. |
| 4 Welche Lagerbestellungen | „Das müsste Andi beantworten.“ | **offen** – an Andi weitergeben (Name/Funktion nicht im Brain belegt; Gerald ergänzt). Bis dahin Empfehlung unverändert. |
| 5 KW-Filter | „Lagerbestellungen haben schon einen Fertigungstermin … **Bestelldatum + 10 Werkstage (inkl. Feiertage berücksichtigen)**.“ | Empfehlung „immer sichtbar“ **verworfen**. Lagerbestell-Zeile bekommt FE-Termin = `AddBusinessDays(Bestelldatum, 10, Feiertage)` (vorhanden: `IBusinessDayService.AddBusinessDays`, `BusinessDayService.cs:29`; Feiertage aus `IHolidayRepository`). KW-Filter wirkt auf diese Zeilen wie auf Auftragsteile; F7/AK 12 + TS-171.6 anpassen. „Bestelldatum“ = `WarehouseRequisition.SubmittedAt` (Abschicken, `WarehouseRequisitionRepository.cs:185`) – siehe Rückfrage 13. Die 10 Tage: feste Zahl oder Einstellung, siehe Rückfrage 13. |
| 6 Summiert | „Ich denke das passt so.“ | Wie vorgeschlagen. |
| 7 Andere BDE-Ansichten | „Warum soll die Arbeitsgang-Liste keine Teile zeigen? War ja geplant für Kanten, Schweißen, usw.?“ | Vermutlich Missverständnis: Die Teileliste ist nicht an IF gebunden – jede Werkbank mit Kürzel kann „BDE-Teileliste“ einschalten (`WerkbankKuerzelFamilie.TeilelisteWirksam`, `WerkbankKuerzelFamilie.cs:52-53`), also auch Kanten/Schweißen; die neuen Spalten wirken dort automatisch. Die Arbeitsgang-Liste des Terminals zeigt Arbeitsgänge je FA, keine Teilezeilen. Rückfrage 14 an Walter. |
| 9 Liste | „Bekommst du, wenn Clemens die Liste überarbeitet hat.“ | Einmal-Skript wartet auf die Datei; Liste muss nach Antwort 2 **Matchcode → Ordner** enthalten (nicht Datei). Spec-Umsetzung kann vorher laufen (Schritt 5 hängt an der Liste). |
| 11 Meldung in Lagerbestellung | „Das müsste Andi beantworten.“ | **offen** – an Andi. Bis dahin Empfehlung „nicht in diesem Schritt“. |

### Pfad-Liste, 2026-11-24 (Mail über Gerald, Anhang `WICAM-Pfade.xlsx`)

Datei: `backlog/anhaenge/2026-11-19-bde-liste-isolierungsfraese-pfad-material/WICAM-Pfade.xlsx` (ein Blatt „Tabelle1“, keine
Kopfzeile, **179 Zeilen** statt der angekündigten 29). Spalten laut Inhalt und Mail: A Matchcode, B Material (Jackodur 20/30/40),
C unklar (Werte wie `K-02`, `H2-01`; vermutlich Lagerplatz – **ungeprüft**), D Menge (laut Mail „Menge 40 → 20“; Bedeutung
Losgröße/Mindestmenge **ungeprüft**), E Pfad, F Bearbeitung („Maschine“ rot / „Nesting“ blau), G Zuschnittmaß (neu, nur teilweise).
Mail sinngemäß: Maß von `G-KT-ISO-Blende-MH+SW-ZS-1100-676-V01` auf 1100 geändert, Menge von `G-KT-ISO-Boden-Auflage-Hinten-ZS-1000-617-V01`
von 40 auf 20 geändert; rote Pfade auf sortenrein geschachtelte Nesting-Programme umgestellt, direkt an der Fräse zu bearbeiten. Laut
Spalte F geht alles von der Maschine aus, **Ausnahmen**: Belegestation (`G-BL-…`, 51 Zeilen, „Nesting“; Abstimmung mit Martin wegen
Auftragsbezug offen) und `S-WKS2-ISO-Twin-Kundenseite-670-170-V01` (auftragsbezogen aus der Vormontageliste).

Befunde gegen die Spec (Koordination 2026-11-24, aus dem Dateiinhalt):
- **Regel `<Ordner>\<Matchcode>.tcn` (Antwort 2) trägt nicht überall:** Die roten Nesting-Pfade (`Y:\1_NESTCAD\…`, z. B. Zeilen 119–121,
  143, 156, 158, 162, 163) enden auf einen Programmnamen, der nicht der Matchcode ist; Zeilen 176–179 enden auf eine Datei
  (`…\Schiebedeckel_NEU_1mm_Kleiner_ab_13.11.2024.xmlst`); Zeile 52 enden auf einen Ordner mit Matchcode-Namen.
- **Keine Pfade:** Zeilen 155, 157, 164–166 enthalten „ist enthalten in &lt;anderer Matchcode&gt;“.
- **Zeile 161** (Twin Kundenseite): `Y\Nestcad\WKS\2026\11.2026\23.11.2026_40mm` – Doppelpunkt fehlt, Ordner ist datumsbezogen (Ausnahme).
- **Alle Pfade mit `Y:`**, Freigabe 3 verlangt UNC → Zuordnung `Y:` → `\\server\freigabe` von der IT nötig, bevor der Erstimport läuft.
- **Spalte G (Zuschnittmaß)** ist in der Spec nicht vorgesehen.

**Neue Rückfragen aus den Antworten**

12. **(Gerald) Pflege-Recht Fräsen-Pfad:** Die Zuständigen der Fräse pflegen den Ordner. Bekommen sie die Rolle Stammdaten (darf dann alle
    Artikel-/Stammdaten ändern), oder braucht es eine schmale Pflegemöglichkeit nur für dieses Merkmal (z. B. Bearbeiten direkt aus der Teileliste
    mit BDE-Recht)? *Empfehlung:* zuerst Rolle Stammdaten für 1–2 Personen; eine eigene Pflegemaske erst, wenn das nicht tragbar ist.
13. **(Gerald/Walter) Termin Lagerbestellung:** Ist „Bestelldatum“ das Abschicken (`SubmittedAt`) – *Empfehlung: ja*, der Entwurf ist noch keine
    Bestellung. Und sind die 10 Werktage fest oder eine Einstellung (AppSetting, Zahl, Default 10, IDEAL)? *Empfehlung:* Einstellung, nach ADR 0020.
14. **(Fachbereich: Walter Kronberger) Kanten/Schweißen:** Reicht es, an den Werkbänken Kanten/Schweißen die BDE-Teileliste einzuschalten (dann sehen sie
    ihre Teile mit Material/Blechdicke/Pfad), oder soll zusätzlich die Arbeitsgang-Liste am BDE-Terminal Teile zeigen? Letzteres wäre eine eigene Spec.

**Neue Rückfragen aus der Pfad-Liste (2026-11-24)**

15. **(Fachbereich: Walter Kronberger) Pfad-Form:** Die Liste mischt Ordner, Nesting-Programme ohne Matchcode-Namen und Dateien (`.xmlst`).
    *Vorschlag:* Pfad wird **so übernommen, wie er in Spalte E steht** (kein automatisches Anhängen von `\<Matchcode>.tcn`); Antwort 2 gilt
    damit nicht mehr als Regel. Zeilen mit „ist enthalten in …“: Pfad des genannten Artikels übernehmen oder leer lassen?
16. **(Fachbereich: Walter Kronberger) Ausnahmen Belegestation/Twin Kundenseite:** Pfad trotzdem pflegen oder leer lassen, solange die
    Abstimmung mit Martin läuft? Zeile 161 hat keinen Doppelpunkt und einen Tagesordner – nicht importieren (*Vorschlag*).
17. **(Fachbereich: Walter Kronberger) Spalten C, D, G:** Was bedeuten C (`K-02`, `H2-01` …) und D (Menge)? Soll das Zuschnittmaß (G)
    im WMS angezeigt werden (dann neues Merkmal, eigene Rückfrage an Gerald), oder ist es nur Info für uns?
18. **(Gerald/IT) UNC statt `Y:`:** Auf welche Freigabe zeigt `Y:` an der Fräse? Ohne diese Zuordnung kann der Erstimport die Pfade nicht als UNC schreiben.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Blechdicke: Sage-Feldname und Format (P-1)? → **(Gerald 2026-11-24) Feld heißt `USER_OSBlechdicke`.** Format (Typ, Einheit, z. B. „3,00“ = mm) noch offen – P-1 mit diesem Feldnamen laufen lassen.
2. Fräsen-Pfad: Ordner oder Datei, wer pflegt? → **(Gerald 2026-11-25) Freigabe mit Rumpf-Vorschlag; Antwort Fachbereich bzw. Bestätigung ausstehend, nicht blockierend – Abweichung = Nachbesserung.**
3. „Datei direkt öffnen“: A Kopieren / B file-Link mit Richtlinie / C nein; Y: oder UNC? → **(Gerald 2026-11-23) Pfad als UNC eintragen (nicht Y:), damit direkt öffnen; Kopieren bleibt als Rückfall.** Folge: B mit UNC (`file://server/freigabe/…`), Edge-Richtlinie für Datei-Links mit der IT klären (unverifiziert); Einmal-Skript/Liste mit UNC-Ordnern.
4. Lagerbestellungen: abgeschickt + teilgeliefert, abgeschlossene/stornierte weg? → **(Gerald 2026-11-25) Freigabe mit Rumpf-Vorschlag; Antwort Fachbereich bzw. Bestätigung ausstehend, nicht blockierend – Abweichung = Nachbesserung.**
5. KW-Filter für Lagerbestell-Positionen: immer sichtbar oder nach Abschickdatum? → **(Gerald 2026-11-25) Freigabe mit Rumpf-Vorschlag; Antwort Fachbereich bzw. Bestätigung ausstehend, nicht blockierend – Abweichung = Nachbesserung.**
6. Summiert-Spalten wie vorgeschlagen, Lagerbestell-Positionen zählen mit? → **(Gerald 2026-11-25) Freigabe mit Rumpf-Vorschlag; Antwort Fachbereich bzw. Bestätigung ausstehend, nicht blockierend – Abweichung = Nachbesserung.**
7. „Andere BDE-Ansichten“ = Teileliste, nicht BDE-Terminal? → **(Gerald 2026-11-25) Freigabe mit Rumpf-Vorschlag; Antwort Fachbereich bzw. Bestätigung ausstehend, nicht blockierend – Abweichung = Nachbesserung.**
8. Lagerartikel-Grenze akzeptiert, P-2/P-4 vor Abnahme? → **(Gerald 2026-11-25) Freigabe mit Rumpf-Vorschlag; Antwort Fachbereich bzw. Bestätigung ausstehend, nicht blockierend – Abweichung = Nachbesserung.**
9. Erstimport per Einmal-Skript (Liste als Datei) oder Handpflege? → **(Gerald 2026-11-25) Freigabe mit Rumpf-Vorschlag; Antwort Fachbereich bzw. Bestätigung ausstehend, nicht blockierend – Abweichung = Nachbesserung.**
10. CSV-Zusatzfund mitbeheben? → **(Gerald 2026-11-25) Freigabe mit Rumpf-Vorschlag; Antwort Fachbereich bzw. Bestätigung ausstehend, nicht blockierend – Abweichung = Nachbesserung.**
11. Fräsen-Meldung in der Lagerbestellung sichtbar? → **(Gerald 2026-11-25) Freigabe mit Rumpf-Vorschlag; Antwort Fachbereich bzw. Bestätigung ausstehend, nicht blockierend – Abweichung = Nachbesserung.**
5. (Nachtrag Walter 2026-11-23) KW-Filter: Lagerbestell-Termin = Bestelldatum + 10 Werktage (Feiertage). → **(Gerald 2026-11-23) Ja; die Werktage in unseren Einstellungen (`/Settings`) konfigurierbar.**
12. Pflege-Recht Fräsen-Pfad: Rolle Stammdaten oder schmale Pflege? → **(Gerald 2026-11-23) Neues Pflegerecht vergeben.** Folge: neue Rolle (nur Fräsen-Pfad pflegen) – drei Stellen: Attribut, `codebase/controller.md`, `Views/Users/RoleOverview.cshtml`; Pflegeweg (z. B. aus der Teileliste oder schmale Maske) im Spec-Nachzug festlegen.
13. Termin Lagerbestellung: Bestelldatum = SubmittedAt? 10 Werktage fest oder Einstellung? → **(Gerald 2026-11-23) Ja, `SubmittedAt`; Werktage als Einstellung (AppSetting, Zahl, Default 10, IDEAL).**
14. Kanten/Schweißen: Teileliste einschalten reicht, oder Teile im BDE-Terminal (eigene Spec)? → **(Gerald 2026-11-25) Freigabe mit Rumpf-Vorschlag; Antwort Fachbereich bzw. Bestätigung ausstehend, nicht blockierend – Abweichung = Nachbesserung.**
15. Pfad wie in Spalte E übernehmen (kein `\<Matchcode>.tcn`); „ist enthalten in …“ → Pfad des Artikels oder leer? → **(Gerald 2026-11-25) Freigabe mit Rumpf-Vorschlag; Antwort Fachbereich bzw. Bestätigung ausstehend, nicht blockierend – Abweichung = Nachbesserung.**
16. Belegestation/Twin Kundenseite pflegen oder leer; Zeile 161 nicht importieren? → **(Gerald 2026-11-25) Freigabe mit Rumpf-Vorschlag; Antwort Fachbereich bzw. Bestätigung ausstehend, nicht blockierend – Abweichung = Nachbesserung.**
17. Spalten C/D/G: Bedeutung, Zuschnittmaß im WMS anzeigen? → **(Gerald 2026-11-25) Freigabe mit Rumpf-Vorschlag; Antwort Fachbereich bzw. Bestätigung ausstehend, nicht blockierend – Abweichung = Nachbesserung.**
18. UNC-Freigabe hinter `Y:`? → **(Gerald 2026-11-25) `Y:` = `\\idealfs1\Daten$\Produktion\CAM`.** Erstimport ersetzt das Präfix `Y:` durch diese Freigabe (`Y:\Gerhard\…` → `\\idealfs1\Daten$\Produktion\CAM\Gerhard\…`).
    **Test 2026-11-25 (Gerald):** `file://idealfs1/Daten$/Produktion/CAM/…`-Links von einer http-Testseite (`http://127.0.0.1:8099`) in Edge
    öffnen – „funktioniert“. Vorbehalt: echte WMS-Adresse am Fräsen-PC beim Manual-UAT nochmals prüfen (Zone kann abweichen).
19. Fräsen-Pfad: Merkmal oder eigene Spalte im Artikelstamm? → **(Gerald 2026-11-25) Eigene Spalte im Artikelstamm** (z. B. `Articles.CamPath`,
    Anzeige „Fräsen-Pfad“, reines WMS-Feld, Sage-Abgleich fasst sie nicht an). Folge: F3 und Lösung 3 (Text-Merkmal, `ArticleAttributesController`,
    `SaveValuesAsync`) und AppSetting `BdeTeilelistePfadMerkmal` **entfallen**; dafür Migration + `SQL/XX` + `00_FreshInstall` (2 Stellen).
    Pflege: neues Pflegerecht (Antwort 12) in der BDE-Teileliste und unter „Artikel bearbeiten“.
20. Ein-/Ausschalten? → **(Gerald 2026-11-25) Feature je Standort in den Einstellungen schaltbar** – AppSetting nach Muster `XxxAktiv`
    (`AppSettingKeys.cs:9-43`), z. B. `FraesenPfadAktiv`, Bool, Default aus, kumulativ zu Rolle/Filter (ADR 0011, ADR 0020: Anzeigename, Wirkung,
    Verwendung, Default, Standort). Aus = Spalte, Feld und Pflege nirgends sichtbar.
21. Link und Masken? → **(Gerald 2026-11-25) Link klickbar (`file://`), die GPO ist bereits aktiv.** Ein gemeinsamer Baustein
    (UNC → `file://`-Link), Kopieren bleibt als Rückfall (Antwort 3). Stufe 1: Artikel-Liste, Artikelinfo, Artikel bearbeiten, BDE-Teileliste,
    Lagerbestellung, Bestandsübersicht. **Generell** in Listen mit artikelspezifischen Daten über die Spaltenauswahl einblendbar (Standard
    ausgeblendet); die übrigen Listen (Artikelnummer in 28 Views, viele mit Sage-Zeilen ohne Artikel-Entity) als Folge-Spec.

> [!note] Rumpf nachgezogen 2026-11-25
> Antworten 1, 3, 5 (Nachtrag), 12, 13, 18–21 und „Pfad-Liste, 2026-11-24“ sind in Umfang, F3–F5, F7, F13–F15, Befunde B19–B26, Lösung,
> Migrationen, Einstellungen, Audit/Rollen, Akzeptanzkriterien, Testszenarien und Deploy eingearbeitet.

## Neue Rückfragen aus dem Rumpf-Nachzug (2026-11-25)

22. **(Gerald) Wer bekommt `fraesenpfad_pflegen`?** Die Teileliste öffnet nur mit dem Terminal-Konto der Werkbank (`BdeTeilelisteController.cs:81-91`).
    Bekommt das Terminal-Konto der Fräse die Rolle, kann jeder, der dort arbeitet, Pfade ändern (Audit zeigt nur das Terminal-Konto). Persönliche
    Konten kommen an die Teileliste nur, wenn sie selbst als Terminal hinterlegt sind. *Rumpf:* Code unabhängig davon; Vergabe ist Hand-Schritt. → **(Gerald 2026-11-25) „Pflege hat einen eigenen User – im Artikelstamm.“** Auslegung: ein persönliches Konto mit `fraesenpfad_pflegen` pflegt den Pfad im Artikelstamm; das Terminal-Konto pflegt nicht, der Stift in der Teileliste entfällt.
23. **(Gerald) Antwort 4/12 „Artikel bearbeiten … ODER neue Rolle“:** Der ganze Artikel-Controller verlangt Stammdaten-Lesen, Bearbeiten
    Stammdaten-Schreiben (`ArticlesController.cs:10,100,144`). Soll die neue Rolle dort nur das Pfad-Feld ändern dürfen (eigene schmale Maske oder
    Rollenweiche im Edit, mehr Code), oder pflegt sie nur über den Stift der Teileliste? *Rumpf/Empfehlung:* nur Stift; Artikel bearbeiten bleibt Stammdaten. → **(Gerald 2026-11-25) „ja“** – Auslegung zusammen mit 22: die neue Rolle darf im Artikelstamm **nur das Pfad-Feld** ändern (schmale Pflege, z. B. eigene kleine Maske/Aktion am Artikel); übrige Felder bleiben Stammdaten-Schreiben.
24. **(Gerald) „Lagerbestellung“ in Stufe 1:** Besteller-Seite (`WarehouseRequisitions/Edit`, Pfad unter der Bezeichnung, ohne neue Spalte) und
    Lager-Detailseite (`WarehousePicking/Details`, Spalte über die Spaltenauswahl) – beide (Rumpf) oder nur eine? → **(Gerald 2026-11-25) „Wenn wir sie zur Verfügung haben: alles in den Settings, aber default nicht angezeigt.“** Auslegung: beide Seiten; der Pfad ist überall über die Spaltenauswahl einblendbar, Standard ausgeblendet – auch auf der Besteller-Seite (`WarehouseRequisitions/Edit`, bekommt dafür eine Spaltenauswahl statt „Pfad unter der Bezeichnung“; JS-Zugriff `cells[1]` beachten). Rumpf beim nächsten Nachzug anpassen.

> [!note] Zweiter Rumpf-Nachzug 2026-11-25
> Antworten 22/23 eingearbeitet: Pflege nur im Artikelstamm über **eine** schmale Aktion `Articles/FraesenPfad` (Artikelinfo + Karte auf
> „Artikel bearbeiten“), Edit-POST unverändert, Stift/POST in der Teileliste gestrichen; Zugang Pfleger über `masterdata_read` (B27/B28).
> Geändert: Frontmatter (affected_code, open_questions), Umfang 1, F3, F13, F14, F15, B20, B27, B28, Lösung 3 und 7, Umsetzungsreihenfolge,
> Ponytail, Abhängigkeiten, Einstellungen, Audit/Rollen, AK 5–7 und 21, Testszenarien, Deploy (Manifest). Rückfrage 24 bleibt offen.

## Neue Rückfragen aus dem zweiten Nachzug (2026-11-25)

25. **(Gerald) Zugang des Pflegers zu den Artikelseiten:** Artikelinfo und Artikel-Liste verlangen Stammdaten-Lesen (Klassenfilter
    `ArticlesController.cs:10`, B27). *Rumpf:* der Pfleger bekommt `fraesenpfad_pflegen` **und** `masterdata_read` – kein Code am Filter; er sieht
    damit alle Stammdaten-Masken lesend (u. a. Kreditoren, Debitoren, Werkbänke, Lagerplätze; 11 Controller), ändern kann er nur den Fräsen-Pfad.
    Bestätigen? Alternative: eigenes Lese-Gate nur für die Artikelseiten (Klassenfilter des `ArticlesController` um `fraesenpfad_pflegen`
    erweitern, Menü-Regel `NavigationKatalog.cs:255` mitziehen) – mehr Code, an beiden Standorten wirksam. Nicht blockierend: der Rumpf baut die
    erste Variante; die Alternative ändert nur Filter und Menü-Regel.

Freigabe-Antwort:

25. Zugang Pfleger: `fraesenpfad_pflegen` + `masterdata_read` (Rumpf) oder Lese-Gate nur für Artikelseiten? → **(Gerald 2026-11-25) a reicht** – „der darf im Artikelstamm hinein, aber nur diesen Pfad pflegen, andere Stammdaten nicht“: `fraesenpfad_pflegen` + `masterdata_read`, kein Code am Filter; ändern darf er nur den Pfad.

> [!note] Dritter Rumpf-Nachzug 2026-11-25
> Antworten 24 und 25 eingearbeitet. 24: beide Lagerbestell-Seiten; „Fräsen-Pfad“ in **jeder** Stufe-1-Tabelle nur über die Spaltenauswahl,
> Standard ausgeblendet (Regel unter F13, drei Quellen gleich inkl. `"defaultHidden": true` im View-JSON, B29). Besteller-Seite bekommt eine
> Spaltenauswahl `WarehouseRequisitionItems` (Muster Wareneingang-Positionen, ohne Filter/CSV); `loadStock` liest die Artikelnummer über
> `data-article` statt `row.cells[1]`. Korrektur B24/AK 9a: `cells[1]` diente nur der Lagerplatz-Anzeige, nicht dem Hinzufügen. 25: Variante a
> ist im Rumpf unbedingt (F14, Lösung 3, Rollen, Ponytail), Manifest-Prüfung `Pruefung/FraesenpfadOhneStammdatenLesen` bleibt.
> Geändert: Frontmatter (affected_code, open_questions: 24/25 raus, 26 neu), F13 (Tabelle + Regel), B24, B29 neu, Lösung 3, Umsetzungsreihenfolge
> 2/4, Ponytail, Abhängigkeiten, Audit/Rollen, Listen-View-Pattern, AK 9a/9b/21, TS-171.4b.

## Neue Rückfragen aus dem dritten Nachzug (2026-11-25)

26. **(Gerald, nicht blockierend) Detailseiten:** Artikelinfo und „Artikel bearbeiten“ haben keine Spaltenauswahl; der Rumpf zeigt den Pfad dort
    bei Schalter an immer (Zeile in den Stammdaten bzw. Karte unter dem Formular – der Pfleger braucht das Formular ohnehin). Passt das zu „default
    nicht angezeigt“, oder soll der Pfad dort erst auf Klick erscheinen (z. B. eingeklappter Bereich)? Der Rumpf baut „immer sichtbar“.

Freigabe-Antwort:

26. Detailseiten Artikelinfo/Artikel bearbeiten: Pfad immer sichtbar (Rumpf) oder eingeklappt? → **(Gerald 2026-11-25) Freigabe mit Rumpf-Vorschlag; Antwort Fachbereich bzw. Bestätigung ausstehend, nicht blockierend – Abweichung = Nachbesserung.**
