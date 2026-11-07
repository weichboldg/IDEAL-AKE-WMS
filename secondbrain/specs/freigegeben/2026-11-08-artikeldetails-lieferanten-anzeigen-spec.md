---
type: spec
title: Artikelinfo – Sage-Zusatzfelder auffindbar, Haupt-/letzter Lieferant mit Kreditor, Artikel-Lieferanten
slug: 2026-11-08-artikeldetails-lieferanten-anzeigen-spec
status: Testbereit
created: 2026-11-07
updated: 2026-11-08
source_backlog: "[[2026-11-07-artikeldetails-lieferanten-anzeigen]]"
buendel: "[[2026-11-08-buendel-uebersicht]]"
task: "[[2026-11-08-buendel-2026-11-08-goal-lauf-2]]"
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
  migration: true
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

**Nachtrag Deploy v1.85.0** (qa-agent, 2026-11-07). Ermittelt aus dem echten Diff `26e6a278..1dac3bcd`. Dieser Nachtrag ersetzt die Zeile „Migration: nein“ oben.
- **Web:** ja. Betroffen sind `ArticlesController`, `CreditorsController`, die Partial `_ArtikelSageKarten`, `Articles/Index` mit Spaltenauswahl, `Creditors/Details`, die Repositories und `AppVersion`.
- **Service:** ja. Betroffen sind `SdataArtikelReader`, `ISageArtikelReader`, `ArtikelLieferantSyncService` (Mengeneinheiten aus SData) und `AppVersion`.
- **Migration:** ja. Neu ist `IdealAkeWms/Migrations/20261107182445_AddArtikelMengeneinheiten` mit `SQL/140_AddArtikelMengeneinheiten.sql`; `00_FreshInstall.sql` ist an beiden Stellen nachgezogen.
  - Die Migration ist additiv (nur nullable Spalten) und nicht daten-destruktiv.
  - Es gibt kein Einmal-Skript.
- **Reihenfolge Testsystem:**
  1. DB-Backup (Standard).
  2. `SQL/140` (dazu die anderen Bündel-Skripte nach Nummer).
  3. Web publizieren.
  4. Service publizieren.
- Die Fußzeile des Bündels zeigt v1.86.0.

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


## Nachbesserung (Gerald 2026-11-07 19:17) – offen für den nächsten Lauf
1. **Bearbeiten-Seite** `/Articles/Edit/{id}`: Karte **„Lieferanten“** (Hauptlieferant, letzter Lieferant, Lieferantentabelle wie in der
   Artikelinfo, nur lesend, Links zum Kreditor) **und** die Karte „Sage-Zusatzfelder“ ebenfalls anzeigen (nur lesend). Hebt Freigabe-Antwort 1
   insoweit auf: Artikelinfo **und** Bearbeiten zeigen die Sage-Daten; gemeinsames Partial statt doppeltem Markup.
2. **Kreditor-Details:** neue Karte „Artikel dieses Lieferanten“ (Artikel, Bezeichnung, Bestellnummer, Mindestbestellmenge, EK-Einheit,
   Kennzeichen Hauptlieferant, Kennzeichen „letzter Lieferant“, Link zur Artikelinfo, CSV). Hebt Freigabe-Antwort 2 auf (vorher „gehört zu C1“).
3. **Artikelliste:** optionale Spalten „Hauptlieferant“ und „Letzter Lieferant“ (Name + Konto) im Spalten-Zahnrad, Default ausgeblendet,
   filterbar (ADR 0005, Spaltenregistrierung + Drift-Guard). Hebt Freigabe-Antwort 4 für diese zwei Spalten auf.
4. Zeitstempel „aus Sage gelesen am …“ prüfen: Artikelinfo 00722001 zeigte 09:18, der Artikellauf auf AKE-DEV lief 11:39–11:41 (UTC-Anzeige?).
5. **Artikel-Sync übernimmt die Mengeneinheit aus SData** (Gerald 2026-11-07 19:18): Bei `Sync:Quelle:Artikel = SData` die Mengeneinheiten
   aus `Artikel`/`ArtikelVariantenExtended` übernehmen – Basismengeneinheit nach `Articles.Unit` (heute aus der View), Lager- und
   EK-Mengeneinheit samt Umrechnungsfaktor in `ArticleSageDetails` (neue Spalten, SQL-Nummer laut Übersicht). Feldnamen vorher lesend am
   AKE-Test belegen (E0-Regel); View-Weg unverändert (ADR 0019). Anzeige in Artikelinfo/Bearbeiten. Bezug: Wareneingang B-2 (Einheit) –
   mit der Lagermengeneinheit kann die Sperre „Einheit weicht ab“ gezielt prüfen.
   **Präzisierung Gerald 19:18:** mindestens **Lagereinheit und Bestelleinheit**, sonst alles, was SData zu Einheiten liefert. Laut
   [[sage-sdata-artikel-felder]] Z. 19 am Artikel: `Basismengeneinheit`, `DezimalstellenBasis`, `Lagermengeneinheit`, `DezimalstellenLager`,
   `UmrechnungsFaktorLME`, `Verkaufsmengeneinheit`, `DezimalstellenVK`, `UmrechnungsfaktorVK`, `MengenberechnungVK`, `VerpackungseinheitVK`
   (+ weitere der Zeile); **Bestelleinheit je Lieferant** aus `ArtikelLieferantInfo` (`Einkaufsmengeneinheit`, `MengenberechnungEK`,
   `DezimalstellenEK`, `UmrechnungsfaktorEK`, `VPEK` – Teile davon in `ArticleSuppliers` schon vorhanden: prüfen, ergänzen). Anzeige je Einheit
   mit Umrechnung zur Basiseinheit.

## Umsetzung Nachbesserung (Dev, 2026-11-08, Lauf 2) – v1.85.0, Commit `5e3340ce`

- **Punkt 1:** Partial `Views/Shared/_ArtikelSageKarten.cshtml` (Sage-Zusatzfelder + Lieferanten) für Artikelinfo und „Artikel bearbeiten“;
  Daten über `ArticlesController.SageKartenAsync` (ein Kreditor-Aufruf), auch beim Wiederanzeigen nach Validierungsfehler.
- **Punkt 2:** Kreditor-Details Karte „Artikel dieses Lieferanten“ (`IArticleRepository.GetArtikelJeLieferantAsync`), Client-Spaltenfilter + CSV
  (einzige filterbare Tabelle der Seite); Register `ColumnFilterTypes` „Creditors/Details“ (client-only).
- **Punkt 3:** Artikelliste mit Spaltenauswahl (`ColumnDefinitions.Articles`, viewKey `Articles`), Spalten `hauptlieferant`/`letzter-lieferant`
  `defaultHidden`, Server-Spaltenfilter über denselben Ausdruck „Name (Konto)“ bzw. Konto wie die Anzeige (`GetLieferantTexteAsync`).
- **Punkt 4 (Befund):** Zeitstempel wird in UTC gespeichert und korrekt lokal angezeigt; `ModifiedAt` ändert sich aber nur bei einer Wertänderung
  (`ArticleSageDetailsWriter`), daher 09:18 trotz Lauf 11:39. Text jetzt „letzte Änderung durch den Artikel-Abgleich“ (kein „gelesen am“).
- **Punkt 5 – E0 (lesend AKE-Test 2026-11-08):** `Artikel`: `Basismengeneinheit`, `DezimalstellenBasis`, `Lagermengeneinheit`, `DezimalstellenLager`,
  `UmrechnungsFaktorLME`, `Verkaufsmengeneinheit`, `DezimalstellenVK`, `UmrechnungsfaktorVK`, `VerpackungseinheitVK`, `UmrechnungsfaktorVPVK` belegt
  (z. B. EK012566 Basis RLL / Lager Stk; EK023609 Basis Stk / VK PKG). `ArtikelLieferantInfo`: `Einkaufsmengeneinheit`, `DezimalstellenEK`,
  `UmrechnungsfaktorEK`, `UmrechnungsfaktorVPEK`, `MengenberechnungEK` belegt; **`VPEK` gibt es nicht** (HTTP 500) – weggelassen.
  `MengenberechnungVK/EK` (am Test nur `null`, Typ unbelegt) nicht übernommen. Neue Spalten per `SQL/140_AddArtikelMengeneinheiten.sql`.
- **Abweichung zur Nachbesserung (bitte bestätigen):** „Basismengeneinheit nach `Articles.Unit` (heute aus der View)“ trifft nicht zu – beide Wege
  schreiben heute die **Lagermengeneinheit** nach `Articles.Unit` (`SageArticleSql.cs:16/32`, `SdataArtikelReader.cs:254`). `Unit` ist die
  Bestandseinheit des WMS; ein Wechsel auf die Basiseinheit änderte Bestände/Buchungen. **Nicht umgestellt**; alle Einheiten stehen zusätzlich im
  Satelliten und in der Anzeige. Soll `Unit` trotzdem Basiseinheit werden: eigene Spec (Bestandsumrechnung, Sage-Lagerbuchung).

## QA Nachbesserung v1.85.0 (qa-agent, 2026-11-07, Bündel 2026-11-08 Lauf 2) – **NICHT Testbereit** (Befund Q-1)

Review-Bereich `git diff 26e6a278 5e3340ce` im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (HEAD `5e3340ce`). Selbst in diesem Lauf geprüft, nicht delegiert.

### 1. Build und Tests (Worktree, HEAD `5e3340ce`)
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (bestehende Nullable-Warnungen).
- `dotnet test`: **IdealAkeWms.Tests 2480 bestanden / 1 übersprungen / 0 Fehler (2481)**; **IDEALAKEWMSService.Tests 503 / 0 / 0 (503)**.

### 2. Review – Befunde und Behandlung
| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| Q-1 | **Mittel (blockiert)** | Kreditor-Details, Karte „Artikel dieses Lieferanten“: unpaginierte Client-Mode-Tabelle (`ArticleRepository.GetArtikelJeLieferantAsync` mit `ponytail:`-Kommentar; `Views/Creditors/Details.cshtml`). ADR 0005 erlaubt Client-Mode nur für **kleine** Ansichten; die Spec selbst (Offene Rückfrage 2) begründete, dass ein Lieferant Tausende Artikel hat und dafür eine Liste mit Pagination braucht. **Gemessen auf AKE-DEV:** K30601 Gasperlmair 9.166 Zeilen, K30687 GLAS-TECH 7.342, die Seite K30601 hat **4,4 MB HTML** (Server 367 ms; zum Vergleich K31013: 121 Zeilen, 81 KB). Am Desktop bedienbar, an Fertigungsterminals oder am Telefon nicht belegt. | Offen. Entscheidung Mensch/Dev: (a) Server-Paging + Server-Spaltenfilter + CSV über alle Treffer (ADR-0005-Standard) oder (b) bewusste Ausnahme als ADR-/Spec-Vermerk. |
| Q-2 | Niedrig | Der Spaltenfilter der Karte bleibt **über Kreditoren hinweg** stehen: `data-view-key="CreditorArtikel"` ist für alle Kreditoren gleich, und `table-filter.js` speichert je viewKey in sessionStorage. Belegt: Filter „00722001“ auf K31013 gesetzt, danach öffnet K30601 mit demselben Filter und 0 Zeilen. Der Kopf zeigt „(9166)“, ein Hinweis „keine Treffer“ fehlt. Der Filtertext steht sichtbar im Feld, der Effekt ist also nicht still. | Mit Q-1 beheben (z. B. viewKey je Kreditor oder ohne Persistenz). |
| Q-3 | Hinweis | Artikelliste: Ein aktiver Filter auf einer ausgeblendeten Spalte (Default `hauptlieferant` aus, Aufruf `?colf_hauptlieferant=loysch`) filtert (116 Treffer), das Filterfeld ist aber unsichtbar. Das ist allgemeines Verhalten von `column-preferences.js` und nicht neu durch diesen Diff. | Nur vermerkt. |
| Q-4 | Hinweis | Text „1 Nachkommastellen“ (Partial `E()`), der Singular fehlt. | Kosmetik, optional. |
| Q-5 | Prüfabfrage | Die Richtung der Sage-Faktoren ist am Code nicht belegbar. Die Anzeige „1 <Einheit> = <Faktor> <Basiseinheit>“ unterstellt, dass `UmrechnungsFaktorLME`, `UmrechnungsfaktorVK`, `UmrechnungsfaktorVPVK` und `UmrechnungsfaktorEK` jeweils **zur Basismengeneinheit** gelten. | Mensch prüft in Sage an EK012566 (Basis RLL / Lager Stk): Gilt 1 Stk = Faktor × RLL oder umgekehrt? Bezieht sich der EK-Faktor auf die Basis- oder die Lagereinheit? |

**Ohne Befund geprüft:**
- **ADR 0004:** Die EF-Migration `20261107182445_AddArtikelMengeneinheiten` fügt nur nullable Spalten hinzu. `SQL/140` hat einen Tabellen-Guard und `COL_LENGTH`-Guards, die DDL steht je in einem eigenen Batch, der History-Insert separat. `00_FreshInstall.sql` ist an beiden Stellen nachgezogen: Schema beider Tabellen **und** MigrationId mit Spalten-Guard. `SQL/AgentJobs` ist nicht betroffen.
- **ADR 0005, Artikelliste:**
  - `ColumnDefinitions.Articles` ist registriert (viewKey `Articles`, im `GetByViewKey`-Switch).
  - Der Inline-`#column-config` hat dieselben Keys in derselben Reihenfolge wie die `<th>`.
  - Der Spaltenfilter-Ausdruck in `ApplyArticleColumnFilter` ist zeichengleich zu `GetLieferantTexteAsync` (Name + " (" + Konto + ")" bzw. getrimmtes Konto).
  - SQL-Gegenprobe auf AKE-DEV: Filter „loysch“ ergibt 116 = 116, „K31013“ beim letzten Lieferanten 108 = 108.
- **Eindeutigkeit:** `Creditors.SageKto` ist eindeutig (`IX_Creditors_SageKto`), `FirstOrDefault` ist daher deterministisch.
- **Schreibweg der neuen Felder:** `ArticleSageDetailsWriter` schreibt per `SetValues` nach Namen. Die Property-Namen des Records sind gleich den Entity-Namen (geprüft).
- **Script-Blöcke und Ausgabe:** Merkmalsnamen laufen über `@Json.Serialize`. Es gibt kein `Html.Raw`, kein `hidden` zusammen mit `d-*` und keine stillen `?? 0`-Fallbacks (fehlend = „—“). Die XSS-Probe `VerpackungseinheitVK = '<i>K'` wird als Text angezeigt.
- **Kontrast und Farbe:** „Ja“ und „– inaktiv“ stehen als Text, Hinweise in `text-body-secondary`, keine neue Farbe.
- **ADR 0020:** keine neue Einstellung.
- **Edit-POST-Fehlerpfad:** lädt `SageKarten` nach (`ArticlesController.cs:168`).

**Abweichung Punkt 5 bestätigt:** `Articles.Unit` kommt auf beiden Wegen aus der **Lagermengeneinheit**.
- View/Legacy: `SageArticleSql.cs:16,32` (`CAST(a.Lagermengeneinheit …) AS Unit`).
- SData: `SdataArtikelReader.cs:254` (3. Argument `a.Lagermengeneinheit` → `SageArtikelDto.Unit`).
- Upsert: `SageImportService.cs:514`.
- Datenbeleg: 00722001 hat `Unit = lfm`, gleich der EK- und der Lagereinheit.

Die Nichtumstellung ist richtig. „Basis → Unit“ wäre eine Bestandsumstellung.

**`VPEK`:** Der Code verwendet `VPEK` nicht, der Select nutzt `UmrechnungsfaktorVPEK`. Beleg ist Dev-E0 (HTTP 500 für `VPEK`); nicht nachgemessen, weil QA keinen Sage-Zugriff hat.

### 3. Nachweisart je Punkt
| Punkt | Nachweis |
|---|---|
| 1 Bearbeiten-Karten | Quelltext-Test `ArtikelinfoLieferantenTests` (Partial eingebunden) + **DEV OK** (`/Articles/Edit/2963`). Fehlerpfad nach Validierungsfehler: Manual-UAT. |
| 2 Kreditor-Artikel | InMemory `Punkt2_ArtikelJeLieferant_HauptUndLetzterKennzeichen` + **DEV OK** (Filter). CSV: Manual-UAT. |
| 3 Listenspalten | InMemory-Texte + SQL-Übersetzung (`ToQueryString`) + DefaultHidden. Das LIKE-Ergebnis ist nur auf DEV prüfbar (SQL-Gegenprobe OK). |
| 4 Zeitstempel | Quelltext-Test + DEV (Text „letzte Änderung durch den Artikel-Abgleich: 07.11.2026 09:18“). Die Ursache (ModifiedAt nur bei Wertänderung) ist am Writer belegt. |
| 5 Mengeneinheiten | Anzeige über InMemory/Quelltext + DEV mit Testwerten. **SData-Lesen (Feldnamen, Werte) = Manual-UAT**, weil der SData-Lauf auf DEV nicht erlaubt ist. |

### 4. Geänderte bestehende Tests
- `ArtikelinfoLieferantenTests`: Die View-Prüfung liest jetzt `_ArtikelSageKarten.cshtml`. Das Verhalten ist durch Nachbesserung 1 absichtlich geändert (Markup ins Partial verschoben), die Erwartungen sind unverändert.
- `CreditorsControllerTests`: nur der Ctor-Parameter `IArticleRepository` (Kompilier-Fix).
- `ColumnFilterTypesDriftTests`: `Creditors/Details` steht jetzt in der Client-only-Liste. Das folgt aus Nachbesserung 2 (Client-Mode-Tabelle) und hängt an Q-1.

Keiner der Tests fällt in die Kategorie „sonstige“.

### 5. Checkliste
- Migration + idempotentes `SQL/140` + FreshInstall an beiden Stellen: ja.
- Audit: keine neue Schreibstelle außer dem Service-Writer (setzt ModifiedAt/By/ByWindows bei Änderung).
- Version 1.85.0 in beiden `AppVersion.cs`; Anwender-Changelog und Hilfe ergänzt.
- `docs/TESTSZENARIEN.md`: TS-112.12–112.16 im Worktree.
- **Offen:**
  - `secondbrain/tests/testszenarien-index.md` nennt 112.12–112.16 noch nicht.
  - Der Brain-Changelog v1.85.0 fehlt noch.
  - Frontmatter `deploy.migration` steht noch auf `false`, ist jetzt aber `true`.

### 6. DEV-Deploy und Testlauf
- **Deploy** `dev-deploy.ps1 -Quelle <worktree>` (elevated):
  - **AKE:** Exit 0, „HTTP 200, Fusszeile v1.85.0“.
  - **IDEAL:** Exit 0, „HTTP 200, Fusszeile v1.85.0“.
  - Die Version ist gleich der Branch-AppVersion 1.85.0.
- **SQL/140 je zweimal:**
  - AKE_WMS_DEV: Lauf 1 und 2 Exit 0 (No-op, EF hatte schon migriert).
  - IDEAL_WMS_DEV: Lauf 1 und 2 Exit 0 (No-op).
  - Die MigrationId ist vorhanden, alle 11 Spalten sind da.
  - Zusätzlich der **DDL-Pfad** auf der Wegwerf-DB `QA_SQL140_TMP` (nur die zwei Tabellen + History): Lauf 1 legt 9 + 2 Spalten an und trägt die History ein, Lauf 2 ändert nichts, Exit 0/0. Die DB ist danach gelöscht.
- **Testdaten AKE-DEV** (per SQL gesetzt, danach zurückgesetzt):
  - `ArticleSageDetails`, ArticleId 2963 (00722001): Basis `Stk`/0, Lager `lfm`/6, VK `PKG`/10/1, VP `<i>K`/100.
  - `ArticleSuppliers`, Id 4522: `UmrechnungsfaktorEK` 1→6, `DezimalstellenEK` 2, `UmrechnungsfaktorVPEK` 12.
- **Reset:**
  - Alle neuen Spalten wieder NULL (Kontrolle: 0/0 Zeilen belegt).
  - `UmrechnungsfaktorEK` = 1.000000.
  - `ModifiedAt` unberührt (NULL).
  - Kein `Sync:*Enabled` steht auf true.

| Schritt | Ergebnis | Status |
|---|---|---|
| 112.12 Bearbeiten 00722001 | Karten „Sage-Zusatzfelder“ (mit Mengeneinheiten) und „Lieferanten“ unter dem Formular, nur lesend | OK |
| 112.12 Fehlerpfad | Formular nicht abgeschickt (die Client-Validierung blockt, der Server-Pfad ist im Browser nicht erreichbar); im Code belegt | NICHT MÖGLICH |
| 112.13 Mengeneinheiten | Mit Testwerten: „Stk, 0 Nachkommastellen“, „lfm (1 lfm = 6 Stk), 2 Nachkommastellen“, „PKG (1 PKG = 10 Stk)…“, Lieferanten-Tabelle „lfm (1 lfm = 6 Stk)“. Echte SData-Werte erst nach einem Lauf | OK (Anzeige) / NICHT MÖGLICH (SData-Lauf) |
| 112.14 Kreditor K31013 | Karte mit 121 Zeilen, Filter „00722001“ → 1 Zeile, Haupt/Letzter „Ja“, Link zur Artikelinfo. CSV-Knopf vorhanden, Download nicht ausgelöst | OK / CSV offen |
| 112.14 großer Kreditor K30601 | 9.166 Zeilen, 4,4 MB; der Filter aus K31013 bleibt stehen | **FEHLER → Q-1, Q-2** |
| 112.14 Kreditor ohne Artikel | IDEAL-DEV Kreditor 1: Hinweis mit `Sync:ArtikelLieferantEnabled` | OK |
| 112.15 Artikelliste | Spalten standardmäßig aus, im Zahnrad einblendbar; Anzeige „"carlo" Loysch GmbH (K31013)“; Filter „loysch“ 116 Treffer (SQL 116), über alle Seiten; Spaltenwahl nach Neuladen erhalten; Antwortzeit mit Filter 1,1–1,4 s bei 105.632 Artikeln | OK |
| 112.16 Zeitstempel | „letzte Änderung durch den Artikel-Abgleich: 07.11.2026 09:18“ | OK (Unveränderlichkeit bei einem Lauf ohne Änderung: NICHT MÖGLICH, Lauf aus) |
| 112.6/112.7 Hinweise | AKE-Artikel 00193056 ohne Lieferanten: Hinweis. IDEAL `DUMMY` in Info und Edit: beide Hinweise | OK |
| 112.10 IDEAL-DEV | Info, Edit, Kreditor-Details und Artikelliste (auch mit `colf_hauptlieferant`) liefern HTTP 200 | OK |
| Browser-Konsole | keine App-Fehler (nur eine Meldung der Browser-Erweiterung) | OK |

### Fehlerbericht
- **Was:** Q-1 (ADR 0005, unpaginierte Client-Liste mit gemessen bis 9.166 Zeilen / 4,4 MB) und Q-2 (Filter-Persistenz über Kreditoren).
- **Erste Fundstelle:** `IdealAkeWms/Data/Repositories/ArticleRepository.cs` `GetArtikelJeLieferantAsync` (`ponytail:`-Kommentar) und `IdealAkeWms/Views/Creditors/Details.cshtml` (`data-view-key="CreditorArtikel"`).
- **Vermutete Ursache:** eine bewusste Abkürzung im Dev-Lauf; die Datenmenge je Lieferant war nicht gemessen.
- Der Status bleibt **InUmsetzung**. Fixversuch 1 steht aus.

### Manuelle Testliste (offen für den Menschen, nach Behebung von Q-1/Q-2)
1. Q-1 entscheiden: Server-Paging oder dokumentierte Ausnahme.
2. Q-5 als Prüfabfrage in Sage: Bezug und Richtung von `UmrechnungsFaktorLME`, `UmrechnungsfaktorVK`, `UmrechnungsfaktorVPVK` und `UmrechnungsfaktorEK` an EK012566 und EK023609.
3. 112.13 mit echten Daten auf dem Testsystem nach dem SData-Artikel- und dem Artikel-Lieferanten-Lauf (EK012566, EK023609).
4. 112.12 Fehlerpfad: Bearbeiten mit Server-Validierungsfehler speichern → die Karten bleiben sichtbar.
5. CSV der Kreditor-Karte (112.14) und CSV der Artikelliste (112.11): nur Text, kein Link-HTML.
6. 112.16 nach einem Lauf ohne Wertänderung: Zeitstempel unverändert.
7. Kreditor-Details K30601 an einem Fertigungsterminal bzw. Telefon öffnen (Ladezeit, Filter).
8. TS-112 vollständig auf dem Testsystem (AKE und IDEAL) vor dem Merge.

## Reparatur 1 nach QA Nachbesserung (Dev, 2026-11-08) – Commit `1dac3bcd`

- **Q-1 / Q-2:** Variante (a). `CreditorsController.Details(id, page, pageSize)` filtert per `ColumnFilterHelper.Apply` mit Register
  `ColumnFilterTypes.For("Creditors/Details")` **vor** Zählen und Blättern, `PaginationState` + `_Pagination`; Tabelle `data-server-column-filter="true"`
  (Filter nur in der URL, wandert nicht zum nächsten Kreditor); Kopf zeigt die Gesamtzahl, Leerzeile „Keine Treffer für den Spaltenfilter.“.
  „Creditors/Details“ aus der Client-only-Liste des Drift-Tests entfernt. `ponytail:` Die Repo-Abfrage lädt weiterhin alle Zeilen des Lieferanten
  in den Speicher (~9.000 schmale Zeilen); Paging in SQL erst, wenn das messbar bremst. Test `Punkt2_KreditorKarte_ServerFilterVorPaging`.
- **Q-4:** Singular „1 Nachkommastelle“.
- **Q-3:** unverändert (allgemeines Verhalten, nicht durch diesen Diff). **Q-5:** bleibt Prüfabfrage für Gerald.


## QA Reparatur 1 v1.85.0 (qa-agent, 2026-11-07, Bündel 2026-11-08 Lauf 2) – **Testbereit**

Review-Bereich: `git diff 5e3340ce 1dac3bcd`, also das Delta der Reparatur. Den Rest des v1.85.0-Diffs hat „QA Nachbesserung v1.85.0“ oben geprüft.
- Build und Tests liefen auf dem ganzen Worktree, HEAD `405e9af5`.
- Den Review habe ich selbst in diesem Lauf gemacht, nicht delegiert.

### 1. Build und Tests (Worktree, HEAD `405e9af5`)

`dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (die bestehenden Nullable-Warnungen).

`dotnet test`:

| Testprojekt | bestanden | übersprungen | Fehler | gesamt |
|---|---|---|---|---|
| IdealAkeWms.Tests | 2482 | 1 | 0 | 2483 |
| IDEALAKEWMSService.Tests | 506 | 0 | 0 | 506 |

### 2. Review des Reparatur-Deltas – Befunde und Behandlung

| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| Q-1 | behoben | `CreditorsController.Details(id, page, pageSize)` arbeitet in dieser Reihenfolge: `ColumnFilterHelper.Apply` mit Register `ColumnFilterTypes.For("Creditors/Details")`, dann Zählen, dann `Skip/Take`. Dazu kommen `PaginationState` und `_Pagination`. Die Tabelle hat `data-server-column-filter="true"`. Gemessen: Details siehe Abschnitt 6. | Erledigt. Der Rest steht unter `ponytail:` (alle Zeilen des Lieferanten im Speicher, ~9.000 schmale Zeilen, Server-Antwort ohne Auffälligkeit). Das ist dokumentiert und akzeptabel. |
| Q-2 | behoben | Server-Mode: Der Filter steht nur in der URL (`colf_*`). Gemessen: Nach dem Filter auf K30601 öffnet K31013 ungefiltert (121 Zeilen, Filterfeld leer). | Erledigt. |
| Q-4 | behoben | `_ArtikelSageKarten.cshtml` setzt „1 Nachkommastelle“ in den Singular. | Erledigt. Nur im Code geprüft, weil kein DEV-Artikel mit Dezimalstellen = 1 vorliegt. |
| R-1 | Hinweis | Eine Seite außerhalb des Bereichs, z. B. `/Creditors/Details/927?page=999`, zeigt „Eintraege 24951–121 von 121“ und die Leerzeile „Keine Treffer für den Spaltenfilter.“, obwohl kein Filter aktiv ist. Das entsteht nur bei einer von Hand gebauten URL. | Nicht blockierend. Die Referenz-Listen klemmen die Seite ebenfalls nicht (`ProductionOrdersController` hat kein Clamp). Optional: Leerzeilentext nur bei aktivem Filter oder die Seite klemmen. |
| R-2 | Hinweis | CSV ohne Filter bei K30601: Der Server-Mode-Export holt `pageSize=0`. Das ergibt nach `PageSize.AllCap` 5.000 von 9.166 Zeilen, mit der sichtbaren Warnung „Treffer auf 5 000 begrenzt“ (gemessen). | Standardverhalten nach ADR 0005 und CSV-Spec v1.53.0, kein Defekt. Mit Filter wird vollständig exportiert. |

**Ohne Befund geprüft:**
- `LieferantArtikelColumnMap` ist zeichengleich zur Zellanzeige der View. Geprüft habe ich `"—"` für fehlende Werte, `"0.###"` bei der Mindestmenge und „Ja“/leer bei Haupt- und letztem Lieferanten.
- Der Zahlentyp `mindestmenge` steht im Register.
- Der Drift-Test führt `Creditors/Details` nicht mehr als „client-only“. Damit erzwingt er die `ColumnFilterTypes.For`-Verwendung, und sie ist vorhanden.
- Die Leerzeile hat `colspan="7"`. `csv-export.js:104` überspringt `td[colspan]`, die Leerzeile landet also nicht in der CSV.
- Der Kopf „aus Sage (N)“ zeigt die Gesamtzahl, die Pagination die gefilterte Zahl. So ist es in der Spec beschrieben.
- Der Hinweis „Kreditor ohne Artikel“ hängt jetzt an der Gesamtzahl und nicht an der Seite. Er erscheint deshalb nicht fälschlich bei 0 Filtertreffern.
- Im Delta gibt es keine Werte in `<script>`, kein `Html.Raw`, kein `hidden` zusammen mit `d-*` und keine neue Einstellung (ADR 0020 nicht betroffen).

### 3. Nachweisart

| Punkt | Nachweis |
|---|---|
| Filter vor Paging | InMemory `Punkt2_KreditorKarte_ServerFilterVorPaging`. Belegt: Gesamtzahl 2, gefiltert 1. |
| Paging, Server-Filter, kein Wandern, Leerzeile, CSV-Abruf | DEV, nur dort prüfbar: echtes Model-Binding, `site.js` und `csv-export.js`. Ergebnisse in Abschnitt 6. |
| CSV-Datei selbst | Manual-UAT. Den Download habe ich nicht ausgelöst. Die Serverseite des Exports (`pageSize=0`) ist auf DEV geprüft. |

### 4. Geänderte bestehende Tests

`ColumnFilterTypesDriftTests`: `Creditors/Details` ist aus der Client-only-Liste entfernt. Das ist eine absichtlich geänderte Erwartung, sie folgt aus der Behebung von Q-1 (Server-Mode). Die Einordnung lautet nicht „sonstige“.

### 5. Checkliste

- Version 1.85.0 bzw. Bündel 1.86.0 steht in beiden `AppVersion.cs`.
- TS-112.14 ist im Worktree aktualisiert.
- Testindex: TS-112.12–112.16 habe ich in diesem Lauf in `secondbrain/tests/testszenarien-index.md` nachgezogen.
- Frontmatter `deploy.migration` steht jetzt auf `true` (SQL/140), dazu der Nachtrag im Abschnitt Deploy.
- **Noch offen:** Brain-Changelog v1.85.0 (`secondbrain/changelog/`). Er ist nicht blockierend und wird spätestens beim Merge geschrieben.

### 6. DEV-Deploy und Testlauf

Deploy mit `dev-deploy.ps1 -Quelle <Worktree>` in einer elevated Shell (Admin = True):

| Standort | Exit | Ausgabe |
|---|---|---|
| AKE | 0 | „HTTP 200, Fusszeile v1.86.0“ |
| IDEAL | 0 | „HTTP 200, Fusszeile v1.86.0“ |

Die Fußzeile v1.86.0 ist gleich der Branch-AppVersion.

| Schritt | Ergebnis | Status |
|---|---|---|
| 112.14 K30601 (AKE-DEV, Kreditor 547) ohne Filter | Seite 1: 25 Zeilen, „Eintraege 1–25 von 9166“, 367 Seiten, Kopf „aus Sage (9166)“ | OK |
| Filter vor dem Blättern | Auf Seite 3 den Artikelfilter „0039342“ + Enter gesetzt. Die URL wird zu `?colf_artikel=0039342`, die Seite springt auf 1, es bleiben 4 Treffer. | OK |
| Zahlenfilter | `colf_mindestmenge=>1` ergibt 608 Treffer, die SQL-Gegenprobe (`ArticleSuppliers` × `Creditors` K30601, `Mindestbestellmenge > 1`) ebenfalls 608. | OK |
| Filter wandert nicht | Danach K31013 (927) geöffnet: 121 Zeilen, Filterfeld leer | OK |
| Haupt-Filter K31013 | `colf_haupt=Ja` ergibt 116, wie die Listenspalte im Vorlauf | OK |
| Leerzeile bei 0 Treffern | `colf_artikel=XYZNIX` ergibt „Keine Treffer für den Spaltenfilter.“ | OK |
| CSV-Serverabruf | `?pageSize=0&page=1` ergibt 5.000 von 9.166 Zeilen mit der Warnung „Treffer auf 5 000 begrenzt“ | OK (siehe R-2). Datei-Download: NICHT MÖGLICH, weil ich keinen Download auslöse. |
| IDEAL-DEV Kreditor 1 | Karte „aus Sage (0)“ mit dem Hinweis auf `Sync:ArtikelLieferantEnabled` | OK |
| Browser-Konsole | keine Fehler | OK |
| Regression 112.12/112.13/112.15/112.16 | Kein Code im Delta außer dem Text aus Q-4. Den Stand belegt der Vorlauf. | OK (unverändert) |

**DEV-Zustand am Ende:**
- Kein `Sync:*Enabled` steht auf true.
- `SageLagerbuchungAktiv` ist nicht gesetzt.
- In diesem Teil habe ich keine Testdaten geändert.

### Manuelle Testliste (offen für den Menschen)

1. Q-5 als Prüfabfrage in Sage: Bezug und Richtung von `UmrechnungsFaktorLME`, `UmrechnungsfaktorVK`, `UmrechnungsfaktorVPVK` und `UmrechnungsfaktorEK` an EK012566 und EK023609.
2. TS-112.13 mit echten Daten auf dem Testsystem, nach dem SData-Artikellauf und dem Artikel-Lieferanten-Lauf.
3. TS-112.12 Fehlerpfad: Bearbeiten mit einem Server-Validierungsfehler speichern. Die Karten müssen sichtbar bleiben.
4. CSV der Kreditor-Karte mit Filter (vollständig) und ohne Filter bei K30601 (5.000 mit Warnung) prüfen; dazu die CSV der Artikelliste. Erwartet ist nur Text, kein Link-HTML.
5. TS-112.16 nach einem Lauf ohne Wertänderung: Der Zeitstempel bleibt unverändert.
6. K30601 an einem Fertigungsterminal bzw. Telefon öffnen und blättern.
7. Testsystem: Backup, `SQL/140`, Web, Service. Danach TS-112 vollständig an AKE und IDEAL vor dem Merge.
