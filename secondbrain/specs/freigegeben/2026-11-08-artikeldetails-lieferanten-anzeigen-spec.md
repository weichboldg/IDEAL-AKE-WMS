---
type: spec
title: Artikelinfo – Sage-Zusatzfelder auffindbar, Haupt-/letzter Lieferant mit Kreditor, Artikel-Lieferanten
slug: 2026-11-08-artikeldetails-lieferanten-anzeigen-spec
status: Testbereit
created: 2026-11-07
updated: 2026-11-07
source_backlog: "[[2026-11-07-artikeldetails-lieferanten-anzeigen]]"
buendel: "[[2026-11-08-buendel-uebersicht]]"
task: "[[2026-11-08-buendel-2026-11-08-goal-lauf]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
version: "1.80.0"
standort: beide
affected_code:
  - "IdealAkeWms/Controllers/ArticlesController.cs:23-43 (Ctor), :221-342 (Info)"
  - "IdealAkeWms/Models/ViewModels/ArticleInfoViewModel.cs:32 (SageDetail) + neue Felder"
  - "IdealAkeWms/Views/Articles/Info.cshtml:134-189 (Karte Sage-Zusatzfelder), :155 (Haupt-/letzter Lieferant als Rohtext)"
  - "IdealAkeWms/Views/Articles/Index.cshtml:82-85 (Artikelnummer ohne Link zur Artikelinfo)"
  - "IdealAkeWms/Views/Articles/Edit.cshtml:6 (Kopf ohne Link zur Artikelinfo)"
  - "IdealAkeWms/Data/Repositories/IArticleRepository.cs:19-20 + ArticleRepository.cs:30-31 (neue Lesemethode Lieferanten)"
  - "IdealAkeWms/Data/Repositories/CreditorRepository.cs:7-19 (neue Lesemethode je Sage-Konto)"
  - "IdealAkeWms/Views/Help/Index.cshtml:228-260 (Hilfe Artikelinfo)"
  - "IdealAkeWms.Tests/Controllers/ArticlesControllerTests.cs:16-54 (Build), neue Tests"
  - "docs/TESTSZENARIEN.md (TS-106.3.d ueberholt, neu TS-112)"
open_questions:
  - "Ort der Anzeige: Variante A = bestehende Artikelinfo (/Articles/Info) erweitern und aus Artikelliste + Bearbeiten-Seite verlinken; Variante B = Karte zusaetzlich in der Bearbeiten-Seite bzw. eigene Detailseite. Empfehlung A (Anzeige existiert seit v1.73.0, es fehlt nur der Weg dorthin)."
  - "Kreditor-Details Rueckrichtung (Artikel dieses Lieferanten): in dieser Spec? Empfehlung: nein, mit dem Backlog Einkauf/Bestellvorschlaege zusammen bauen (braucht paginierte Liste nach ADR 0005)."
  - "Zusaetzliche Spalten in der Artikel-Lieferanten-Tabelle: Wiederbeschaffungszeit und/oder Einzelpreis? Empfehlung: Wiederbeschaffungszeit ja, Einzelpreis nein (Preise waeren fuer alle mit masterdata_read sichtbar)."
  - "Sage-Zusatzfelder als Spalten in der Artikelliste (/Articles)? Empfehlung: nein (eigener Backlog-Punkt, falls gewuenscht; Join + Spaltenfilter + Spaltenkatalog fuer 100.600 Artikel)."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-07"
---

## Ziel / Nutzen (das Warum)

Gerald sieht den letzten Lieferanten nicht. Der Befund der Koordination (Backlog, 11:45) sagt „angezeigt wird nichts ausser den
Benutzerfeld-Merkmalen“. Im Code ist das **so nicht richtig**:

- Die Sage-Zusatzfelder werden seit v1.73.0 angezeigt, und zwar in der **Artikelinfo** `/Articles/Info?articleNumber=…`
  (`Views/Articles/Info.cshtml:134-189`, Daten aus `ArticlesController.cs:339` → `ArticleRepository.cs:30-31`). Dort stehen auch
  Hauptlieferant und letzter Lieferant, allerdings nur als Rohkonto (`Info.cshtml:155`).
- Eine Seite `/Articles/Details` gibt es nicht. `ArticlesController` hat nur `Index`, `Create`, `Edit` und `Info` (`ArticlesController.cs:50,86,108,222`).
  Die Bearbeiten-Seite `Edit.cshtml` zeigt keine Satelliten-Felder.
- Die Artikelinfo ist aus der Artikelliste und aus der Bearbeiten-Seite **nicht verlinkt** (`Articles/Index.cshtml:82-85,113-118`, `Edit.cshtml:6`).
  Erreichbar ist sie nur über die Dashboard-Kachel (`Views/Home/_ArtikelinfoTile.cshtml:2`) oder die URL (`Help/Index.cshtml:232-233`).
  Wer vom Artikelstamm kommt, findet sie deshalb nicht.
- Fehlt der Satellit, wird die Karte **still ausgeblendet** (`Info.cshtml:134`, so abgenommen in TS-106.3.d). Es gibt keinen Hinweis, wodurch
  die Felder gefüllt würden.
- `ArticleSuppliers` hat keine Oberfläche (`Models/ArticleSupplier.cs:5` „keine Oberflaeche“). Einziger Lesezugriff ist der Abgleich
  `ArtikelLieferantSyncService`.

Nutzen: Einkauf und Lager sehen am Artikel, von wem er kommt (mit Name und Sprung zum Kreditor) und wer ihn sonst noch liefert,
ohne Sage zu öffnen.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope (Variante A, siehe Rückfrage 1)**
- Artikelinfo: Hauptlieferant und letzter Lieferant mit Kreditor-Name und Link auf `/Creditors/Details/{id}`. Ohne Kreditor im WMS wird nur das Konto angezeigt.
- Artikelinfo: neue Karte „Lieferanten“ mit den Zeilen aus `ArticleSuppliers`.
- Artikelinfo: Fehlt der Satellit, erscheint ein Hinweis statt einer ausgeblendeten Karte. Für eine leere Lieferantenliste gilt dasselbe.
- Link Artikelnummer → Artikelinfo in der Artikelliste, Knopf „Artikelinfo“ im Kopf der Bearbeiten-Seite.
- Hilfe (Artikelinfo), Anwender-Changelog, Version v1.80.0, TS-112, TS-106.3.d als überholt markieren.

**Out-of-Scope**
- Pflege jeder Art (Sage bleibt führend, G6).
- Kreditor-Details: Artikel dieses Lieferanten (Rückfrage 2, Empfehlung: nein).
- Sage-Spalten in der Artikelliste (Rückfrage 4, Empfehlung: nein).
- Umbau der übrigen Satelliten-Gruppen (Stamm, Lager, Dispo, Fertigung, Kennzeichen, Maße). Sie bleiben, wie sie sind (`Info.cshtml:141-164`).

## Fachliche Anforderungen

1. **Hauptlieferant / letzter Lieferant.** Den Wert aus `ArticleSageDetails.Hauptlieferant` bzw. `.LetzterLieferant` (Sage-Konto, z. B. `K31013`)
   getrimmt gegen `Creditors.SageKto` nachschlagen.
   - Treffer: Anzeige `<Name> (<Konto>)` als Link auf die Kreditor-Details. Ist der Kreditor inaktiv, folgt der Text „– inaktiv“
     (nicht nur Farbe).
   - Kein Treffer: nur das Konto als Text (Backlog).
   - Wert leer: „—“.
2. **Artikel-Lieferanten.** Tabelle aller `ArticleSuppliers` des Artikels. Spalten: Lieferant (`<Name> (<Konto>)` mit Link, inaktiv wie in 1),
   Bestellnummer (`SageBestellnummer`), Mindestbestellmenge, EK-Einheit (`Einkaufsmengeneinheit`), Hauptlieferant („Ja“ bzw. leer)
   und, falls Rückfrage 3 es so entscheidet, Wiederbeschaffungszeit. Sortierung: Hauptlieferant zuerst, dann nach Kreditor-Name.
3. **Hinweis statt leerer Karte.**
   - Satellit fehlt: Die Karte „Sage-Zusatzfelder“ erscheint mit dem Hinweis *„Für diesen Artikel liegen noch keine Sage-Zusatzfelder vor. Sie werden
     nur vom Artikel-Abgleich mit Quelle SData gefüllt (Service-Einstellung `Sync:Quelle:Artikel` = `SData`).“* Haupt- und letzter
     Lieferant zeigen in diesem Fall „—“.
   - Keine Lieferanten-Zeile: *„Keine Artikel-Lieferanten aus Sage gelesen. Sie werden vom Lauf „Artikel-Lieferanten“ gefüllt
     (Service-Einstellung `Sync:ArtikelLieferantEnabled`; braucht die Kreditoren aus Sage, `Sync:KreditorenEnabled`).“*
   - Die Karte „Lieferanten“ steht **unabhängig** vom Satelliten. Der Abgleich der Artikel-Lieferanten läuft getrennt vom Artikel-Abgleich
     (`ArtikelLieferantSyncService.cs:58-91` liest nur `Articles` und `Creditors`, nicht den Satelliten).
4. **Auffindbarkeit.**
   - In der Artikelliste wird die Artikelnummer zum Link auf `/Articles/Info?articleNumber=<Nummer>`. Der Zelltext bleibt gleich, damit
     Spaltenfilter und CSV-Export unverändert arbeiten.
   - Die Bearbeiten-Seite bekommt im Kopf einen Knopf „Artikelinfo“.
5. Die Seite bleibt nur lesend. Es gibt keine Bearbeiten-Elemente für Satellit oder Lieferanten.

## Technischer Loesungsentwurf

Muster: Repository-Pattern (ADR [[0001-repository-pattern-mit-decorator-fuer-caching]]). `IArticleRepository` und `ICreditorRepository`
haben keinen Caching-Decorator (`Program.cs:68,110`).

**Repository**
- `IArticleRepository.GetSuppliersAsync(int articleId)` liefert `List<ArticleSupplierRow>`. Das ist ein Projektions-Record (CreditorId, Name,
  SageKto, IsActive, SageBestellnummer, Mindestbestellmenge, Einkaufsmengeneinheit, Wiederbeschaffungszeit, IstHauptlieferant), gebaut über
  einen Join `ArticleSuppliers` ⨝ `Creditors` mit `AsNoTracking`. Eine Navigation gibt es nicht, nur FKs (`ApplicationDbContext.cs:1342-1343`).
  Die Sortierung laut Anforderung 2 geschieht in SQL.
- `ICreditorRepository.GetBySageKtosAsync(IReadOnlyCollection<string> konten)` liefert `List<Creditor>` (`AsNoTracking`, `konten.Contains(c.SageKto)`).
  Die Konten trimmt der Aufrufer. Die WMS-Seite ist schon getrimmt: `KontokorrentSyncService.cs:138,153` (Sage-Abgleich) und
  `CreditorsController.cs:118,161` via `Normalize` (manuell). Der Satellit schreibt die Sage-Werte ungetrimmt durch
  (`ArticleSageDetailsWriter.cs:38,45` `SetValues`, `SdataArtikelReader.cs:280`). Deshalb ist das `Trim()` beim Aufrufer Pflicht.
- Groß/Klein: Der Abgleich vergleicht `OrdinalIgnoreCase` (`ArtikelLieferantSyncService.cs:90-91`). In SQL Server entscheidet die Kollation der
  Spalte `Creditors.SageKto`. Die Zuordnung im Controller läuft deshalb über ein `Dictionary(StringComparer.OrdinalIgnoreCase)`.

**Controller** `ArticlesController.Info` (`:325-340`)
- `ICreditorRepository` kommt in den Ctor (`:23-43`; Test-`Build()` `ArticlesControllerTests.cs:45-48` anpassen).
- Nach `GetSageDetailAsync` werden die beiden Konten (falls nicht leer) gesammelt und mit **einem** Aufruf `GetBySageKtosAsync` nachgeschlagen.
- `GetSuppliersAsync(article.Id)` wird immer aufgerufen.

**ViewModel** `ArticleInfoViewModel`: neue Felder `Creditor? HauptlieferantKreditor`, `Creditor? LetzterLieferantKreditor` und
`List<ArticleSupplierRow> Lieferanten`.

**View** `Info.cshtml`
- In der Gruppe „Bestand und Dispo (Sage)“ entfallen die zwei Tupel-Zeilen „Hauptlieferant“/„Letzter Lieferant“ (`:155`). Die Tupel rendern
  nur Text (`:178`), Links passen dort nicht hinein.
- `@if (Model.SageDetail is { } sd) {…} else {Karte mit Hinweis}` (`:134`).
- Neue Karte „Lieferanten“ direkt nach der Sage-Karte: `<dl>` mit Hauptlieferant und letztem Lieferant, darunter die Tabelle bzw. der Hinweis.
  Den Kreditor-Text baut **eine** lokale Razor-Funktion für beide Stellen (dl und Tabelle).
- Tabelle: `table table-sm` in `.table-responsive`, **ohne** `filterable-table`. Das ist eine kleine Nebentabelle auf einer Detailseite und folgt der Regel
  [[fallstricke]] „`table-filter.js` und `column-preferences.js` bedienen genau EINE Tabelle je Seite“ (Kleine Tabellen bleiben ohne Filter
  und ohne `filterable-table`). Die übrigen Tabellen der Artikelinfo sind ebenfalls ohne Filter (`Info.cshtml:196,241`).
  Dahinter steht die Annahme, dass ein Artikel wenige Lieferanten hat. Prüfschritt P2 unten bestätigt das.

**Links**
- `Articles/Index.cshtml:83`: `<strong><a asp-action="Info" asp-route-articleNumber="@article.ArticleNumber">…</a></strong>`. Der Text
  bleibt die Nummer, CSV und Spaltenfilter lesen den Zelltext.
- `Edit.cshtml:6`: Kopf mit `d-flex justify-content-between flex-wrap gap-2` (ADR 0005 Layout) plus Knopf `btn btn-outline-secondary` „Artikelinfo“.

**Frontend:** Skill `frontend-design` ist Pflicht (CLAUDE.md). Bootstrap-Bestand, Kontrast WCAG AA, „inaktiv“ als Text.

**Prüfschritte vor/bei der Umsetzung** (lesend, WMS-Datenbank AKE-DEV bzw. AKE-Test; Sage selbst wird nicht abgefragt)
- **P1 – Kontoformat letzter Lieferant (unverifiziert):** Für `Hauptlieferant` ist das Format belegt. Der Abgleich der Artikel-Lieferanten
  vergleicht `ArtikelVariantenExtended.Hauptlieferant` mit `ArtikelLieferantInfo.Lieferant` und `Creditors.SageKto`
  (`ArtikelLieferantSyncService.cs:89-91,98-100,120`) und fand am AKE-DEV 15.424 Hauptlieferanten (Backlog-Befund). Für `LetzterLieferant`
  fehlt ein solcher Beleg. Anforderung 1 für den letzten Lieferanten gilt deshalb erst nach diesem Prüfschritt:
  ```sql
  SELECT 'Haupt' AS Feld, COUNT(*) AS gesamt, SUM(CASE WHEN c.Id IS NULL THEN 1 ELSE 0 END) AS ohneKreditor
  FROM ArticleSageDetails d LEFT JOIN Creditors c ON c.SageKto = LTRIM(RTRIM(d.Hauptlieferant))
  WHERE NULLIF(LTRIM(RTRIM(d.Hauptlieferant)), '') IS NOT NULL
  UNION ALL
  SELECT 'Letzter', COUNT(*), SUM(CASE WHEN c.Id IS NULL THEN 1 ELSE 0 END)
  FROM ArticleSageDetails d LEFT JOIN Creditors c ON c.SageKto = LTRIM(RTRIM(d.LetzterLieferant))
  WHERE NULLIF(LTRIM(RTRIM(d.LetzterLieferant)), '') IS NOT NULL;
  SELECT TOP 10 d.LetzterLieferant FROM ArticleSageDetails d WHERE d.LetzterLieferant IS NOT NULL;
  ```
  Erwartung: `ohneKreditor` ist bei „Letzter“ in derselben Größenordnung wie bei „Haupt“. Ist der Anteil deutlich höher (anderes Format, z. B. ohne `K`):
  Befund in die Spec schreiben, letzten Lieferanten nur als Konto anzeigen und Rückfrage an den Menschen, **nicht** raten.
- **P2 – Zeilen je Artikel:** `SELECT TOP 5 ArticleId, COUNT(*) AS n FROM ArticleSuppliers GROUP BY ArticleId ORDER BY n DESC;`.
  Bei mehr als etwa 30 Zeilen bei einem Artikel: Befund melden (Darstellung ohne Filter überdenken), nicht still kappen.

**Ergebnis P1/P2 (Dev, 2026-11-08, AKE-DEV `AKE_WMS_DEV`, nur lesend):** P1 – Hauptlieferant 16.496 belegt, 0 ohne Kreditor;
letzter Lieferant 22.677 belegt, 0 ohne Kreditor; Stichprobe `K30021`, `K30823`, `K30610` → gleiches Kontoformat, Anforderung 1 gilt für beide.
P2 – höchstens 8 Lieferanten je Artikel (ArticleId 732, 2851, 72279, 74802) → Tabelle ohne Filter passt. Umsetzung `ac92a15c`.

## Migrations-/SQL-Auswirkungen

Keine. Es gibt kein neues Schema. `ArticleSageDetails`, `ArticleSuppliers` und `Creditors` existieren seit v1.73.0 (`ApplicationDbContext.cs:562-597,
1338-1359`). Es gibt keine Migration, kein `SQL/XX`, keine Änderung an `00_FreshInstall.sql` und keine `SQL/AgentJobs/`. Falls der Dev-Lauf doch Schema braucht,
stoppt er und meldet das als offene Frage. Er vergibt keine SQL-Nummer selbst.

## Einstellungen

keine. Die Hinweistexte **nennen** die bestehenden Service-Einstellungen `Sync:Quelle:Artikel`, `Sync:ArtikelLieferantEnabled` und
`Sync:KreditorenEnabled` (`Models/ServiceSettingDefinitions.cs:80,87,89,192,194`), ändern sie aber nicht.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine. Das ist ein reiner Lesepfad, keine Entität wird geschrieben. Die vorhandene Zeile „aus Sage gelesen am …“ (`Info.cshtml:140,168`) bleibt.

## Rollen / Zugriff

Unverändert. `ArticlesController` hat class-level `[RequireMasterDataReadAccess]` (`ArticlesController.cs:10`), `Info` hat kein eigenes Attribut.
`CreditorsController.Details` steht unter demselben Filter (`CreditorsController.cs:15,96`). Wer die Artikelinfo sieht, darf dem
Kreditor-Link also folgen. Der Knopf „Artikelinfo“ auf `Edit` braucht keine eigene Prüfung, denn `Edit` verlangt mit `masterdata` mehr
(`ArticlesController.cs:107`). Es gibt keine neue Rolle und keinen neuen Filter.

## Listen-View-Pattern

Es entsteht keine neue Listen-View. Die Lieferanten-Tabelle ist eine vorgefilterte Nebentabelle einer Detailseite ohne Pagination und ohne
`filterable-table`. Begründung oben (fallstricke, Regel „Je Seite eine filterbare Haupttabelle“). Die Artikelliste behält Pagination und
Server-Spaltenfilter, nur die Zelle wird ein Link.

## Akzeptanzkriterien

1. `ArticlesController.Info` lädt die Artikel-Lieferanten mit genau einem Aufruf `GetSuppliersAsync(article.Id)` und schlägt Haupt- und letzten
   Lieferanten mit genau einem Aufruf `GetBySageKtosAsync` nach. Ohne Satellit oder mit leeren Konten wird die Methode nicht aufgerufen.
   Nachweis: Controller-Test.
2. Controller-Test: Der Satellit hat `Hauptlieferant = " K31013 "` (mit Leerzeichen), der Kreditor `SageKto = "K31013"` → `HauptlieferantKreditor`
   ist gesetzt. `LetzterLieferant = "K99999"` ohne Kreditor → `LetzterLieferantKreditor` ist null.
3. Repository-Test (InMemory): `GetSuppliersAsync` liefert nur Zeilen des Artikels, mit Kreditor-Name, Hauptlieferant zuerst und dann nach Name.
   `GetBySageKtosAsync` liefert nur die angefragten Konten.
4. Artikelinfo mit Satellit und Kreditor: „Hauptlieferant“ zeigt `<Name> (K31013)` als Link auf `/Creditors/Details/<id>`. Mit inaktivem
   Kreditor steht zusätzlich „– inaktiv“ da. Ohne Kreditor im WMS wird nur das Konto ohne Link gezeigt. Gilt für den letzten Lieferanten
   vorbehaltlich P1.
5. Artikelinfo ohne Satellit: Die Karte „Sage-Zusatzfelder“ zeigt den Hinweistext aus Anforderung 3 (nennt `Sync:Quelle:Artikel`), die Seite sonst
   unverändert. Das ersetzt die Erwartung in TS-106.3.d.
6. Artikelinfo: Die Karte „Lieferanten“ zeigt alle `ArticleSuppliers` des Artikels mit den Spalten aus Anforderung 2. Ohne Zeilen erscheint der
   Hinweistext mit `Sync:ArtikelLieferantEnabled`.
7. Die Gruppe „Bestand und Dispo (Sage)“ enthält „Hauptlieferant“/„Letzter Lieferant“ nicht mehr doppelt.
8. Artikelliste: Die Artikelnummer ist ein Link auf die Artikelinfo dieses Artikels. Spaltenfilter „Artikelnummer“ und CSV-Export liefern
   denselben Text wie vorher.
9. Die Bearbeiten-Seite hat im Kopf den Knopf „Artikelinfo“, der die Artikelinfo des Artikels öffnet.
10. Es gibt keine Eingabe- oder Speicher-Elemente für Satellit und Lieferanten. Kreditor-Name und Konto werden HTML-kodiert ausgegeben (Razor-Standard,
    kein `Html.Raw`).
11. Kontrast WCAG AA. „inaktiv“ und „Ja“ sind Text, nicht nur Farbe. Auf Telefonbreite ist alles ohne Seiten-Querscrollen lesbar (Tabelle in
    `.table-responsive`).
12. `dotnet build IdealAkeWms.slnx` und `dotnet test` sind grün. Hilfe „Artikelinfo“ und Anwender-Changelog v1.80.0 sind ergänzt.
13. Manuell: TS-112 auf AKE-DEV mit echten Daten und auf IDEAL-DEV als Hinweis-Fall bestanden.

## Test-Szenarien

Neues Kapitel **TS-112** in `docs/TESTSZENARIEN.md`. In TS-106.3.d die Erwartung als *überholt durch TS-112* markieren, nicht löschen. Danach
`secondbrain/tests/testszenarien-index.md`.

**TS-112 – Artikelinfo: Lieferanten mit Kreditor, Hinweise, Verlinkung (v1.80.0)**
- Vorbedingungen: AKE-DEV mit `Sync:Quelle:Artikel = SData`, Kreditoren- und Artikel-Lieferanten-Lauf gelaufen. Testartikel 00722001
  (Hauptlieferant `K31013` laut TS-106.3). Prüfschritte P1 und P2 ausgeführt, Ergebnis notiert. Benutzer mit `masterdata_read` ohne `masterdata`.
- Schritte / Erwartet:
  1. `/Articles` → Artikelnummer 00722001 anklicken. **Erwartet:** Die Artikelinfo von 00722001 öffnet sich.
  2. Karte „Lieferanten“: **Erwartet:** Hauptlieferant `<Name> (K31013)` als Link, letzter Lieferant entsprechend (oder nur das Konto, wenn
     kein Kreditor existiert). Die Tabelle enthält mindestens die Zeile zu K31013 mit „Ja“ in der Spalte Hauptlieferant, sie steht oben.
  3. Link anklicken. **Erwartet:** Kreditor-Details des richtigen Kontos (Sage-Konto = K31013).
  4. Als Benutzer mit `masterdata`: `/Articles/Edit/<id>` → Knopf „Artikelinfo“. **Erwartet:** Artikelinfo desselben Artikels.
  5. Gruppe „Bestand und Dispo (Sage)“. **Erwartet:** keine Zeilen „Hauptlieferant“/„Letzter Lieferant“ mehr dort.
- Negativfälle:
  6. Artikel ohne Satellit (SQL: `SELECT TOP 1 a.ArticleNumber FROM Articles a WHERE NOT EXISTS (SELECT 1 FROM ArticleSageDetails d WHERE d.ArticleId = a.Id)`).
     **Erwartet:** Karte „Sage-Zusatzfelder“ mit Hinweis auf `Sync:Quelle:Artikel`. Haupt- und letzter Lieferant „—“.
  7. Artikel ohne `ArticleSuppliers`-Zeile. **Erwartet:** Hinweis mit `Sync:ArtikelLieferantEnabled` statt Tabelle.
  8. Satellit mit unbekanntem Konto (nur Testsystem: `UPDATE ArticleSageDetails SET LetzterLieferant = 'K00000' WHERE ArticleId = <id>`, danach
     zurücksetzen). **Erwartet:** nur „K00000“, kein Link, kein Fehler.
  9. Kreditor inaktiv (nur Testsystem, Herkunft WMS bzw. per SQL `IsActive = 0`, danach zurück). **Erwartet:** Link mit „– inaktiv“.
  10. IDEAL-DEV (`localhost:8082`), beliebiger Artikel. **Erwartet:** Die Hinweise aus 6/7 erscheinen, die Seite lädt ohne Fehler.
  11. Spaltenfilter Artikelnummer und CSV-Export der Artikelliste. **Erwartet:** wie vorher (Text, kein HTML).

## Etappen (nur bei epic: true)

Nicht zutreffend (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

*Final (QA-Wiederholung 2026-11-07, gegen den echten Diff `0af3a94e..ac92a15c` + Fix `04742c26`).*

- **Web-App:** ja. Betroffen sind `ArticlesController`, `ArticleInfoViewModel`, die Repositories, die Views `Articles/Info|Index|Edit`, `Help/Index`, `Help/Changelog` und `AppVersion.cs`.
- **Service:** ja, **nur Versions-Bump** (`IDEALAKEWMSService/AppVersion.cs` → 1.80.0), damit Web und Service dieselbe Version melden.
- **Migration:** nein.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- **Standort:** beide. Der Code hat keine Standortweiche. Daten im Satelliten gibt es nur an AKE (an IDEAL ist SData für Artikel gesperrt,
  `ServiceSettingDefinitions.cs:80` „IDEAL gesperrt bis P-R1“). An IDEAL werden deshalb die Hinweis-Fälle getestet.
- **Ablauf:** Publish aus dem Worktree → Testsystem → Test → Merge. Keine Migration, kein Einmal-Skript, keine Reihenfolge-Abhängigkeit.
  Als Bündel-Deploy liefert derselbe Worktree zusätzlich v1.81.0 (Folge-Spec) mit – die Fußzeile zeigt dann v1.81.0.
- Nach dem Merge nur dann erneut aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen zusammengeführt hat.

## Offene Rueckfragen

1. **Ort der Anzeige.** Variante A: die bestehende Artikelinfo erweitern und aus Artikelliste und Bearbeiten-Seite verlinken. Variante B: eine Karte
   „Sage“ zusätzlich in der Bearbeiten-Seite bzw. eine eigene Detailseite `/Articles/Details`. *Empfehlung: A.* Die Anzeige existiert seit
   v1.73.0 (`Info.cshtml:134-189`), es fehlt nur der Weg dorthin. B wäre eine zweite Darstellung derselben Felder. Die Spec ist für A geschrieben.
2. **Kreditor-Details: Artikel dieses Lieferanten** (Backlog „optional“). *Empfehlung: nicht in dieser Spec.* Ein Lieferant kann Tausende Artikel
   haben, das wäre eine echte Liste mit Pagination und Spaltenfilter (ADR 0005). Passt zum Backlog Einkauf/Bestellvorschläge.
3. **Weitere Spalten der Lieferanten-Tabelle.** *Empfehlung: Wiederbeschaffungszeit ja, Einzelpreis nein.* Die Artikelinfo sieht jeder mit
   `masterdata_read`, Einkaufspreise würden damit breit sichtbar.
4. **Sage-Zusatzfelder als Spalten in der Artikelliste** (Befund: „optional Listenspalten“). *Empfehlung: nein, eigener Backlog-Punkt bei Bedarf.*

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Ort der Anzeige (A Artikelinfo erweitern + verlinken / B Karte in Bearbeiten bzw. eigene Detailseite) → **wie empfohlen (Gerald 2026-11-07)**
2. Kreditor-Details: Artikel dieses Lieferanten in dieser Spec? → **wie empfohlen (Gerald 2026-11-07)**
3. Weitere Spalten Lieferanten-Tabelle (Wiederbeschaffungszeit / Einzelpreis)? → **wie empfohlen (Gerald 2026-11-07)**
4. Sage-Zusatzfelder als Spalten in der Artikelliste? → **wie empfohlen (Gerald 2026-11-07)**

## QA-Nachweis (qa-agent, 2026-11-07, Bündel 2026-11-08 Lauf „A + Wareneingang“) – NICHT Testbereit

**Ergebnis: ein Befund (B1, Mittel), Status bleibt `InUmsetzung`.** Alles andere ist grün, siehe unten.

### 1. Build und Tests (Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, HEAD `747c1ced`)
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (vorbestehend).
- `dotnet test`: `IdealAkeWms.Tests` 2451 bestanden / 1 übersprungen / 0 Fehler (2452). `IDEALAKEWMSService.Tests` 497 / 0 / 0.

### 2. Review `git diff 0af3a94e ac92a15c` (selbst, in diesem Lauf)
Geprüft: `ArticlesController.Info` (Trim, Distinct, Dictionary `OrdinalIgnoreCase`, ein Aufruf), `ArticleRepository.GetSuppliersAsync` (Join, Sortierung in SQL, `AsNoTracking`),
`CreditorRepository.GetBySageKtosAsync`, `ArticleSupplierRow`, ViewModel, `Info.cshtml` (lokale Funktion `Lieferant`, Hinweis-Karten), `Index.cshtml`, `Edit.cshtml`, Hilfe, Tests.
- **Kodierung (Auftrag):** in Ordnung. `Lieferant()` gibt Name und Konto über `@name`/`@konto` aus (Razor kodiert), `href` über `Url.Action` (kodiert), kein `Html.Raw`, kein `<script>`.
  Laufzeitbeleg: Kreditor `"carlo" Loysch GmbH` erscheint mit Anführungszeichen als Text.
- **B1 (Mittel, AK 11 verletzt) – Kontrast Kopfzeile Karte „Lieferanten“.** `Info.cshtml:221` `<small class="text-muted">nur lesend · aus Sage</small>` steht im
  `.card-header` mit Hintergrund `--ake-primary` `#053153` (`wwwroot/css/site.css:118-124`). Bootstrap 5.3.3 `text-muted` = `rgba(33,37,41,.75)` → **1,12:1** (Soll 4,5:1);
  im Screenshot praktisch unsichtbar. Vorschlag (eine Zeile): `text-muted` weglassen (weiß, 13,4:1) oder `opacity-75` (8,2:1); `text-white-50` reicht mit 4,47:1 knapp nicht.
  **Gleicher Fehler vorbestehend** in der Kopfzeile „Sage-Zusatzfelder“ `Info.cshtml:167` (seit v1.73.0, nicht im Diff) – im selben Zug mitbeheben, sonst bleibt die
  Karte daneben unlesbar.
- Hinweis (kein Befund): Auf IDEAL zeigt der `dl` „Hauptlieferant —“, obwohl die Tabelle Lieferanten hat (kein Satellit an IDEAL, `IstHauptlieferant` dort für EK000441 überall 0). Spec-konform (Anforderung 1/3).
- Klassen aus dem QA-Auftrag: keine Werte in `<script>`, kein `hidden`+`d-*`, kein stilles `?? 0` (fehlende Werte → „—“), keine Parallel-Arrays, keine reine Browser-Validierung,
  Groß/Klein-Regel an einer Stelle (Controller-Dictionary).

### 3. Nachweisart je AK
| AK | Nachweis | Ergebnis |
|---|---|---|
| 1, 2 | `ArtikelinfoLieferantenTests` (Moq) | grün |
| 3 | InMemory-Test; Sortierung real (SQL-Kollation) auf IDEAL-DEV EK000441 nach Name gesehen | grün |
| 4 | DEV 112.2/112.3/112.9 | OK |
| 5, 6 | DEV 112.6/112.7, IDEAL 112.10 | OK |
| 7 | View-Text-Test + DEV 112.5 | OK |
| 8 | View-Text-Test + DEV Spaltenfilter; CSV nur statisch (csv-export.js liest `textContent`) | OK / CSV Manual-UAT |
| 9 | View-Text-Test + DEV 112.4 | OK |
| 10 | View-Text-Test (`Html.Raw` fehlt) + DEV (Anführungszeichen im Namen) | OK |
| 11 | DEV-Screenshot | **FEHLER (B1)**; Telefonbreite NICHT MÖGLICH (Fenstergröße im Lauf nicht änderbar; statisch: Tabelle in `.table-responsive`, `dl` mit `col-sm-*`) |
| 12 | Build/Test, Hilfe + Changelog im Diff | grün |
| 13 | TS-112 auf DEV | siehe 6 |

### 4. Geänderte bestehende Tests
- `ArticlesControllerTests.cs:48` – nur neuer Ctor-Parameter `Mock.Of<ICreditorRepository>()` (Typ-/Compile-Fix).

### 5. Checkliste
Keine Migration, kein SQL, keine Einstellung (ADR 0020 entfällt), keine Audit-Felder (nur lesend). Version 1.80.0 in beiden `AppVersion.cs`, Anwender-Changelog und Hilfe im Diff,
Brain-Changelog `[[2026-11-08-v1-80-0-artikelinfo-lieferanten]]` vorhanden, TS-112 in `docs/TESTSZENARIEN.md`, TS-106.3.d als überholt markiert, Index-Zeile 112 vorhanden.

### 6. DEV-Deploy und TS-112
Deploy einmal für das ganze Bündel (HEAD `747c1ced`, enthält diesen Commit), elevated:
`dev-deploy.ps1 -Standort AKE` → „HTTP 200, Fusszeile v1.81.0 … Fertig: AKE v1.81.0“, Exit 0; `-Standort IDEAL` → „Fertig: IDEAL v1.81.0“, Exit 0.
Die Fußzeile zeigt v1.81.0 (Folge-Commit derselben Branch), nicht 1.80.0 – erwartet bei einem Bündel-Deploy.

| Schritt | Durchführung | Ergebnis | Urteil |
|---|---|---|---|
| 112.1 | AKE `/Articles?search=00722001`, Klick Artikelnummer | Link `/Articles/Info?articleNumber=00722001`, Artikelinfo öffnet | OK |
| 112.2 | Karte „Lieferanten“ | Haupt und letzter: `"carlo" Loysch GmbH (K31013)` als Link `/Creditors/Details/927`; Tabelle 1 Zeile, Bestellnr VISB00061, 200, lfm, 14, „Ja“ | OK (Kontrast Kopf: B1) |
| 112.3 | Link geklickt | Kreditor-Details, Sage-Konto K31013 | OK |
| 112.4 | `/Articles/Edit/2963` | Knopf „Artikelinfo“ → `/Articles/Info?articleNumber=00722001` | OK (als Benutzer mit `masterdata`; ohne `masterdata` nicht geprüft) |
| 112.5 | Gruppe „Bestand und Dispo (Sage)“ | keine Lieferanten-Zeilen | OK |
| 112.6 | 00713171 (kein Satellit) | Hinweis mit `Sync:Quelle:Artikel`, Haupt/letzter „—“ | OK |
| 112.7 | 00713171 (keine `ArticleSuppliers`) | Hinweis mit `Sync:ArtikelLieferantEnabled` | OK |
| 112.8 | `LetzterLieferant='K00000'` (ArticleId 2963) | nur „K00000“, kein Link | OK |
| 112.9 | Kreditor 927 `IsActive=0` | Link + „– inaktiv“ (Text), auch in der Tabelle | OK |
| 112.10 | IDEAL `EK000441` und `DUMMY` | beide Hinweise bzw. Tabelle mit 7 Lieferanten nach Name, kein Fehler | OK |
| 112.11 | Spaltenfilter `colf_article-number=007220` | 21 Treffer, Links korrekt | OK; CSV-Download NICHT MÖGLICH (Download im Lauf nicht erlaubt) |

DEV-Daten: 112.8/112.9 per SQL gesetzt und **zurückgesetzt** (`LetzterLieferant='K31013'`, `Creditors.IsActive=1`, `ModifiedAt` unverändert NULL).

### Fehlerbericht
- Was: AK 11 (Kontrast WCAG AA) verletzt, B1.
- Erster Fehler: `Info.cshtml:221` `text-muted` im dunklen Kartenkopf, 1,12:1.
- Ursache: Bootstrap-Grau ist für helle Flächen gedacht; der Kartenkopf ist global dunkel (`site.css:118`). Muster vorbestehend in `:167`.
- Nächster Schritt: Fix (beide Zeilen), danach QA erneut nur für B1 (Screenshot) – Build/Test/Deploy wiederholen.

### Manuelle Testliste (erster Lauf – ersetzt durch die Liste in „QA-Wiederholung“ unten)

## QA-Wiederholung (qa-agent, 2026-11-07, nach Fix `04742c26`) – **Testbereit**

**Ergebnis: B1 behoben, keine neuen Befunde. Status `Testbereit`.**

### 1. Build und Tests (Worktree, HEAD `04742c26`)
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (vorbestehend, u. a. CS8601 in `BdeTerminalAuftraegeTests.cs:23`).
- `dotnet test --no-build`: `IdealAkeWms.Tests` 2451 bestanden / 1 übersprungen / 0 Fehler (2452); `IDEALAKEWMSService.Tests` 497 / 0 / 0.

### 2. Review `git diff 747c1ced 04742c26` (selbst, in diesem Lauf)
Ein File, zwei Zeilen: `Info.cshtml:167` und `:221` `<small class="text-muted">` → `<small class="opacity-75">`. Text und Razor-Ausgabe unverändert
(Datum weiter über `@gelesen.ToString(...)`, kodiert). Gegenprobe: in `Info.cshtml` steht `text-muted` nur noch auf hellen Flächen (`:113`, `:176`, `:225`, `:227`,
Card-Footer `:296`, Card-Body `:314`), keiner mehr in einem dunklen `.card-header`. Kein `<script>`, kein `hidden`, keine Fallbacks, keine Teständerung.
**Befunde: keine.**

### 3. Nachweisart – Änderung gegenüber dem ersten Lauf
AK 11 (Kontrast): **OK** durch DEV-Messung (s. u.); Telefonbreite bleibt Manual-UAT. Alle übrigen AK wie im ersten Lauf.

### 4. Geänderte bestehende Tests
Keine im Fix-Commit.

### 5. Checkliste
Unverändert (keine Migration, kein SQL, keine Einstellung, Version bleibt 1.80.0 für diese Spec; Bündel-HEAD 1.81.0).

### 6. DEV-Deploy und Gegenprüfung
Elevated (`IsInRole(Administrator)` = True), Quelle Worktree HEAD `04742c26`:
- `dev-deploy.ps1 -Standort AKE` → „HTTP 200, Fusszeile v1.81.0 … Fertig: AKE v1.81.0 auf http://localhost:8081/“, Exit 0.
- `dev-deploy.ps1 -Standort IDEAL` → „HTTP 200, Fusszeile v1.81.0 … Fertig: IDEAL v1.81.0 auf http://localhost:8082/“, Exit 0.
  Fußzeile v1.81.0 = AppVersion des Bündel-HEAD (diese Spec 1.80.0 ist enthalten).

| Schritt | Durchführung | Ergebnis | Urteil |
|---|---|---|---|
| Kontrast | AKE `/Articles/Info?articleNumber=00722001`, Screenshots beider Kartenköpfe, Pixel per `System.Drawing` ausgelesen | Hintergrund `#053251`; Text „nur lesend · aus Sage gelesen am 07.11.2026 09:18“ und „nur lesend · aus Sage“ hell, gut lesbar. Rechnerisch Weiß × 0,75 auf `#053153` = `rgb(192,204,212)` → **8,17:1** (Soll 4,5:1); Pixel-Maximum im Screenshot 9,7–10,1:1 (Kantenglättung). | OK |
| 112.2 | Karte „Lieferanten“ (AKE, 00722001) | Haupt- und letzter Lieferant `"carlo" Loysch GmbH (K31013)` als Link; Tabelle VISB00061 / 200 / lfm | OK |
| 112.6 | AKE 00713171 | Karte „Sage-Zusatzfelder“ mit Hinweis `Sync:Quelle:Artikel = SData`; Haupt/letzter „—“; dazu Hinweis `Sync:ArtikelLieferantEnabled` (112.7) | OK |
| 112.10 (kurz) | IDEAL `EK000441` | Seite lädt, Kopf „nur lesend · aus Sage“ vorhanden, Fußzeile v1.81.0 | OK |

Screenshots: `C:\Users\GERALD~1.WEI\AppData\Local\Temp\claude-chrome-screenshots-cF2Dyv\screenshot-1794071131012-0.jpg` (Sage-Zusatzfelder),
`…\screenshot-1794071141883-1.jpg` (Lieferanten). Keine DEV-Daten geändert.

Nebenbeobachtung (kein Befund dieser Spec, nicht im Diff): Bei der Fensterbreite des Laufs (1520 px) hat die Seite eine Seiten-Querscrollleiste,
weil die globale Navbar überläuft (Benutzername rechts abgeschnitten) – auch auf der Artikelinfo ohne Lieferanten-Tabelle. Betrifft das Layout global, nicht v1.80.0.

### Manuelle Testliste (offen für den Menschen)
1. Kopfzeilen „Lieferanten“ und „Sage-Zusatzfelder“ am Fertigungsterminal lesbar (Licht, Monitor).
2. 112.4 als Benutzer **nur** mit `masterdata_read` (Artikelinfo erreichbar, Kreditor-Link öffnet).
3. 112.11 CSV-Export der Artikelliste: Spalte Artikelnummer enthält nur die Nummer.
4. Telefonbreite: Artikelinfo ohne Seiten-Querscrollen durch die neuen Karten, Lieferanten-Tabelle scrollt in sich.
5. TS-112 vollständig auf dem Testsystem (AKE und IDEAL) vor dem Merge.
