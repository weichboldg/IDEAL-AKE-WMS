---
type: spec
title: EAN von Einkaufsartikeln scannen (Lookup über den SData-Satelliten)
slug: 2026-11-07-ean-einkaufsartikel-scan-spec
status: Entwurf
created: 2026-11-07
updated: 2026-11-07
source_backlog: "[[2026-10-16-ean-einkaufsartikel-scan]]"
buendel: "[[2026-11-07-buendel-uebersicht]]"
depends_on: "[[2026-11-06-sage-sdata-stammdaten-sync-spec]]"
task: "[[2026-11-07-buendel-2026-11-07-goal-lauf]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
version: "1.78.0"
sql_block: "114–115 (reserviert, voraussichtlich NICHT genutzt)"
standort: beide
affected_code:
  - "IdealAkeWms/Data/Repositories/IArticleRepository.cs (neu: GetByEanAsync)"
  - "IdealAkeWms/Data/Repositories/ArticleRepository.cs (neu: GetByEanAsync, Muster GetByMatchcodeExactAsync :94-103)"
  - "IdealAkeWms/Controllers/ArticlesApiController.cs (neu: GET /api/articles/by-ean, Muster by-matchcode :59-75)"
  - "IdealAkeWms/wwwroot/js/barcode-scanner.js (Wrapper processScannedValue :458-493: EAN-Zweig vor dem Matchcode-Zweig, Mehrtreffer-Auswahl)"
  - "IdealAkeWms/Views/Help/Index.cshtml (:93, :175 – Text „EAN-Codes kennt das WMS nicht“)"
  - "README.md (:243-244 Barcode-Scanner)"
  - "IdealAkeWms.Tests/Repositories/ArticleRepositoryTests.cs, IdealAkeWms.Tests/Controllers/ArticlesApiControllerTests.cs, IdealAkeWms.Tests/Views/HardwareScannerTests.cs"
  - "Versionsstand: AppVersion.cs (Web + Service) 1.78.0, Views/Help/Changelog.cshtml, docs/TESTSZENARIEN.md"
open_questions:
  - "1 Quelle der EAN: Backlog will eine Spalte EAN in der IDEAL-View vw_IDEAL_AKE_WMS_Artikel; vorhanden ist ArticleSageDetails.EANNummer, den nur der SData-Artikelweg schreibt. Empfehlung: Lookup NUR über den Satelliten (keine Migration, kein Eingriff in den SQL-Weg, ADR 0019). Lücke: an IDEAL bleibt die EAN leer, solange Sync:ArticlesViewName gesetzt ist (SData gesperrt, P-R1) und kein IDEAL-SData-Endpunkt existiert (G2); an AKE erst ab Sync:Quelle:Artikel = SData (Default View, G1 Lizenz). IDEAL-Weg als Folge-Spec, sobald entschieden ist: SData-Endpunkt abwarten oder View-Spalte + eigenes Feld am Artikel (eigener ADR, weil der SQL-Weg dann nicht mehr bit-gleich bleibt)."
  - "2 Mehrere EAN je Artikel: Der SData-Reader übernimmt nur die EAN der Ausprägung mit der kleinsten AuspraegungID (SdataArtikelReader.cs:235-238). Reicht eine EAN je Artikel? Empfehlung: ja für diese Spec; Entscheidung nach den Prüfabfragen P-E2/P-E3. Zeigen sie Artikel mit mehreren EAN in nennenswerter Zahl, eigene Tabelle als Folge-Spec."
  - "3 Mehrtreffer (dieselbe EAN bei mehreren Artikeln): Auswahlfenster wie in der Backlog-Skizze oder nur Meldung wie beim Matchcode (v1.68.0)? Empfehlung: Auswahlfenster mit bis zu 5 Artikeln, darüber Meldung „bitte von Hand wählen“."
  - "4 EAN auch bei Handeingabe (Artikelinfo-Suchfeld, Select2-Artikelsuche) finden? Empfehlung: nein, nur Scan-Wege (Kamera, Hardware-Scanner, Scannen ohne Feld); Handeingabe ist der Rückweg für Problemfälle und bleibt unverändert."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

> [!warning] Zurückgestellt (Gerald 2026-11-07 10:08) – im /goal-Lauf Bündel 2026-11-07 NICHT umsetzen
> Freigabe zurückgenommen. Grund: P-E2 – an AKE haben nur 2 von 279.529 Artikelvarianten eine EAN; Gerald: „EAN wird nicht gewartet.“
> An IDEAL wirkt die Funktion über SData ohnehin noch nicht (G2). Wieder aufnehmen, wenn EANs in Sage gepflegt werden oder IDEAL eine
> EAN-Quelle hat. Bereits eingetragene Prüfabfrage-Ergebnisse bleiben stehen.


## Ziel / Nutzen (das Warum)

Einkaufsartikel tragen vom Hersteller eine EAN/GTIN auf der Verpackung, aber kein WMS-Etikett. Heute endet
ein solcher Scan in „Nicht gefunden“ (Scan-Handler-Spec [[2026-10-15-scan-handler-hardware-scanner-spec]],
Ausklammerung Antwort 4, :995). Ziel: Der Scan der Hersteller-EAN findet den WMS-Artikel auf allen Seiten,
die heute schon Artikel scannen (Wareneingang, Ausgang, Umbuchung, Sammel-Wareneingang, Stückliste,
Artikelinfo, Filterseiten), ohne neue Scan-Kette.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Repository-Abfrage „Artikel zu EAN“ über den vorhandenen Satelliten `ArticleSageDetails.EANNummer`.
- API `GET /api/articles/by-ean?ean=` (Artikelnummer zuerst, dann EAN).
- EAN-Zweig im globalen Scan-Wrapper von `barcode-scanner.js` (wirkt für Kamera, Hardware-Scanner,
  Kamera-Fenster-Ziel und „Scannen ohne Feld“, weil alle über die globale `processScannedValue` laufen).
- Mehrtreffer-Auswahl (Rückfrage 3), Meldungen, Hilfe/README/Changelog, Tests, Testszenario.

**Out-of-Scope**
- **Keine** neue EAN-Spalte und **kein** Eingriff in den Artikel-Sync: Der SData-Weg liest und schreibt
  `EANNummer` bereits (siehe Ist-Stand). View-Spalte für IDEAL → Rückfrage 1, Folge-Spec.
- Mehrere EAN je Artikel/Ausprägung/Lieferant (Rückfrage 2).
- EAN-Suche bei Handeingabe (Rückfrage 4), EAN-Spalte in der Artikelliste.
- Prüfziffer-Validierung (Scanner prüfen EAN-Prüfziffern selbst; ein falscher Wert findet ohnehin nichts).
- GS1-128/DataMatrix mit Anwendungskennzeichen (AI 01) – nicht verlangt.

## Ist-Stand (belegt, Worktree v1.73.0)

**EAN im WMS – vorhanden, nur über SData befüllt**
- `ArticleSageDetail.EANNummer` nvarchar(15), nullable (`Models/ArticleSageDetail.cs:53`,
  `Data/ApplicationDbContext.cs:576`, `SQL/99_AddArticleSageDetails.sql:55`, `SQL/00_FreshInstall.sql:243`).
  1:1-Satellit zu `Article` (UNIQUE `ArticleId`, Cascade, `ApplicationDbContext.cs:592-598`), Navigation
  `Article.SageDetail` (`Models/Article.cs:58`).
- **Einziger Schreibweg:** `ArticleSageDetailsWriter.SchreibenAsync` – schreibt nur Artikel mit
  `Details != null` (`IDEALAKEWMSService/Services/Sdata/ArticleSageDetailsWriter.cs:21`), aufgerufen aus
  `SageImportService.cs:534-539`. `Details` liefert nur `SdataArtikelReader` (`ISageArtikelReader.cs:5-7`);
  `SageArtikelReader` (View/Legacy-SQL) erzeugt die DTOs ohne `Details` (`SageArtikelReader.cs:48-56`).
  Der Vergleichsmodus schreibt den Satelliten nie (`ArtikelQuelleSwitch.cs:12`). Weitere Schreiber:
  Suche nach `ArticleSageDetail`/`ArticleSageDetails` in `*.cs` ergibt sonst nur Lesestellen
  (`ArticleRepository.cs:30-31`, `ArticleInfoViewModel.cs:32`) und den Benutzerfelder-Writer, der nur den
  Namen `Schreiber` mitbenutzt (`ArtikelBenutzerfelderWriter.cs:54,97,107`).
- Herkunft im SData-Reader: `ArtikelVariantenExtended.EANNummer` (`SdataArtikelReader.cs:51`), je Artikel
  **nur die Ausprägung mit der kleinsten `AuspraegungID`** (`SdataArtikelReader.cs:235-238`), ungetrimmt
  übernommen (`SdataArtikelReader.cs:272`). Der Writer spiegelt auch `null` (`ArticleSageDetailsWriter.cs:10`).
- Anzeige schon vorhanden: Artikelinfo, Gruppe „Stamm“, Feld „EAN“ (`Views/Articles/Info.cshtml:146`).

**Wann wird der Satellit befüllt – je Standort**
- Weiche `ArtikelQuelleSwitch` liest `Sync:Quelle:Artikel` (Default `View`, ADR [[0019-quellen-weiche-view-sdata-je-abgleich]]).
- **IDEAL:** `Sync:ArticlesViewName` ist gesetzt → `SageQuelleSperre.Grund("Artikel", view)` sperrt SData
  (`IdealAkeWms/Services/SageSdata/SageQuelleSperre.cs:17-20`, `ArtikelQuelleSwitch.cs:41-49`), `SData` wirkt
  wie `View`. Zusätzlich gibt es noch keinen IDEAL-SData-Endpunkt (SData-Spec G2). **Folge: An IDEAL bleibt
  `EANNummer` leer – das Feature findet dort keine EAN, bis Rückfrage 1 gelöst ist.**
- **AKE:** keine Artikel-View, SData frei (`ArtikelQuelleSwitch.cs:10-12`). Befüllt erst, wenn
  `Sync:Quelle:Artikel = SData` gesetzt ist; Lizenz-Freigabe DPS offen (SData-Spec G1).

**Scan-Kette**
- Alle Artikel-Scans laufen über die globale, gewrappte `processScannedValue` (`barcode-scanner.js:458`):
  Kamera (`:207`, `:258`), Hardware-Scanner (`:544`, `:559`), Scannen ohne Feld (`:583`). Der Wrapper
  übersetzt heute schon Matchcode → Artikelnummer über `/api/articles/by-matchcode` (`:458-493`) und reicht
  danach die Artikelnummer an `_processScannedValueSync` weiter – damit funktionieren Select2-Ziele
  (`by-number`, `:332-351`), Text-Ziele (`:321-329`) und `onScanned`-Rückrufe, z. B. Sammel-Wareneingang
  (`Views/StockMovements/InboundBulk.cshtml:236-264, :293`), ohne Änderung.
- „Scannen ohne Feld“: Code ohne Trennzeichen, der kein Lagerplatz ist → Artikelnummer
  (`hardware-scanner.js:141-147`) → `processScannedValue` → derselbe Wrapper.
- `ArticlesApiController` hat keinen Rollen-Filter (wie `by-number`/`by-matchcode`, `controller.md:39`).
- `GetByMatchcodeExactAsync` (`ArticleRepository.cs:94-103`): Trim/ToUpper beidseitig, DUMMY ausgeschlossen,
  bewusst ohne Index („Index erst bei gemessener Latenz“).

## Fachliche Anforderungen

- F1 Ein Artikel-Scan, dessen Wert **ohne QR-Trennzeichen** nur aus Ziffern mit Länge 8, 12, 13 oder 14
  besteht (GTIN-8/UPC-A/EAN-13/GTIN-14), wird als möglicher EAN-Scan geprüft.
- F2 Reihenfolge: **Artikelnummer zuerst** (exakt), danach EAN. Ein rein numerischer Artikel, dessen Nummer
  zufällig GTIN-Länge hat, wird also immer über seine Nummer gefunden.
- F3 Genau ein Treffer → das Ziel wird mit der **Artikelnummer** befüllt (nicht mit der EAN), Rückmeldung
  „Gescannt: <Artikelnummer>“, weiter wie ein Artikelnummer-Scan (Rückruf, Menge hochzählen usw.).
- F4 Kein Treffer → **unverändertes heutiges Verhalten** (Weitergabe an den bisherigen Weg inkl.
  Matchcode-Modus; Ergebnis wie heute „Nicht gefunden …“).
- F5 Mehrere Treffer → Auswahlfenster mit bis zu 5 Artikeln („Artikelnummer – Bezeichnung“), Auswahl befüllt
  wie F3; mehr als 5 → Meldung „EAN <Wert> gehört zu mehr als 5 Artikeln – bitte den Artikel von Hand wählen“,
  nichts befüllen (Rückfrage 3).
- F6 Server/Netz nicht erreichbar → Meldung „EAN <Wert> konnte nicht geprüft werden“, nichts befüllen,
  `nichtGefunden`-Rückruf wie beim Matchcode-Fehlerzweig.
- F7 Codes mit Trennzeichen (Artikel-QR), Lagerplatz- und FA-Scans sind unberührt.

## Technischer Lösungsentwurf

Ponytail-Leiter: Die Daten liegen schon im Satelliten (Sprosse 2), die Scan-Kette hat schon einen
Übersetzungs-Wrapper für Matchcodes (Sprosse 2) – es kommt nur ein zweiter Übersetzungszweig dazu.

1. **Repository** (`IArticleRepository`/`ArticleRepository`, Muster `GetByMatchcodeExactAsync`):
   `Task<List<Article>> GetByEanAsync(string ean, int limit)` –
   `_dbSet.Where(a => a.SageDetail != null && a.SageDetail.EANNummer != null && a.SageDetail.EANNummer.Trim() == key && a.ArticleNumber != Article.DummyArticleNumber).OrderBy(a => a.ArticleNumber).Take(limit)`,
   `key = ean.Trim()`. Trim beidseitig, weil der Reader ungetrimmt schreibt (`SdataArtikelReader.cs:272`).
   Kein Caching-Decorator (es gibt keinen für `IArticleRepository`, `Program.cs:68`). Kein Index (Muster
   Matchcode; `ponytail:`-Kommentar mit Grenze „Tabellenscan je Scan, Index bei gemessener Latenz“).
2. **API** in `ArticlesApiController`: `[HttpGet("by-ean")] GetByEan([FromQuery] string? ean)` –
   leer → `BadRequest()`; `GetByArticleNumberAsync(ean.Trim())` trifft → `articles = [dieser]`, sonst
   `GetByEanAsync(ean, 6)`; Antwortform **identisch zu `by-matchcode`** (`articles[{id, articleNumber, text}]`,
   `more`), damit der Client dieselbe Auswertung nutzt.
3. **Client** (`barcode-scanner.js`, Wrapper `:458`): Den heutigen Wrapper-Rumpf unverändert in eine
   benannte Funktion ziehen (z. B. `_artikelScanBisherig`), davor den EAN-Zweig: nur bei
   `scanType === 'article'`, kein `qrTrennzeichen()` im Wert, `/^(\d{8}|\d{12,14})$/` auf den getrimmten Wert.
   `_textInputScanCallback` wie im Matchcode-Zweig vor dem AJAX sichern (`:465-467`).
   Ergebnis: 1 → `_processScannedValueSync(articleNumber, …)`; 0 → `_artikelScanBisherig(value, …)`;
   >1 → Auswahl (F5); Fehler → Meldung über `_qrMatchcodeMeldung` (Meldungsort-Logik wiederverwenden,
   ggf. neutral umbenennen).
4. **Mehrtreffer-Auswahl:** ein Bootstrap-Modal, einmalig per JS in `barcode-scanner.js` erzeugt (keine
   Änderung an den 14 Scan-Views), Liste aus `<button>`-Elementen, Fokus auf den ersten Eintrag, „Abbrechen“.
   Während das Fenster offen ist, gilt ein weiterer Hardware-Scan nicht als Auswahl (nur Klick/Enter).
   **Skill `frontend-design` ist Pflicht** (Auswahlfenster, Meldungstexte): Bootstrap-5-Muster des
   Kamera-Fensters, Ziele mind. 44 px hoch (Handheld), Kontrast WCAG AA, Treffer nicht nur über Farbe.
5. **Hilfe/README:** `Help/Index.cshtml:175` „EAN-Codes kennt das WMS nicht“ ersetzen durch „EAN von
   Einkaufsartikeln: wird gefunden, wenn der Artikel-Abgleich die EAN aus Sage übernommen hat (Quelle SData);
   an IDEAL derzeit nicht“; `:93` und `README.md:243-244` entsprechend.

Module: [[controller]] (ArticlesApi), [[integrationen]] (SData-Artikelweg, nur lesend genutzt).

## Prüfabfragen (Sage – Mensch führt aus, Ergebnisse vor Abnahme)

Alles in diesem Abschnitt ist **unverifiziert**, bis die Abfragen gelaufen sind. Die SQL-Tabelle hinter
`ArtikelVariantenExtended.EANNummer` ist nicht belegt; P-E1 klärt sie, P-E2–P-E4 setzen
`dbo.KHKArtikelvarianten.EANNummer` nur als **Platzhalter** ein (Treffer aus P-E1 einsetzen). Je
Mandanten-DB (AKE `ake`, IDEAL-DB-Name einsetzen).

```sql
-- P-E1 Wo fuehrt Sage EAN/GTIN (auch Lieferanten-/Verpackungs-EAN)?
SELECT c.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS c
WHERE c.TABLE_NAME LIKE 'KHK%'
  AND (c.COLUMN_NAME LIKE '%EAN%' OR c.COLUMN_NAME LIKE '%GTIN%'
       OR c.COLUMN_NAME LIKE '%Barcode%' OR c.COLUMN_NAME LIKE '%Strichcode%');

-- P-E2 Befuellung und Mehrfach-EAN je Artikel (Auspraegungen)
SELECT COUNT(*) AS Zeilen,
       COUNT(NULLIF(LTRIM(RTRIM(v.EANNummer)), '')) AS MitEan,
       COUNT(DISTINCT CASE WHEN NULLIF(LTRIM(RTRIM(v.EANNummer)), '') IS NOT NULL THEN v.Artikelnummer END) AS ArtikelMitEan
FROM dbo.KHKArtikelvarianten v;
SELECT v.Artikelnummer, COUNT(DISTINCT LTRIM(RTRIM(v.EANNummer))) AS AnzahlEan
FROM dbo.KHKArtikelvarianten v
WHERE NULLIF(LTRIM(RTRIM(v.EANNummer)), '') IS NOT NULL
GROUP BY v.Artikelnummer
HAVING COUNT(DISTINCT LTRIM(RTRIM(v.EANNummer))) > 1;

-- P-E3 Dieselbe EAN bei mehreren Artikeln (-> Mehrtreffer F5)
SELECT LTRIM(RTRIM(v.EANNummer)) AS Ean, COUNT(DISTINCT v.Artikelnummer) AS Artikel
FROM dbo.KHKArtikelvarianten v
WHERE NULLIF(LTRIM(RTRIM(v.EANNummer)), '') IS NOT NULL
GROUP BY LTRIM(RTRIM(v.EANNummer))
HAVING COUNT(DISTINCT v.Artikelnummer) > 1;

-- P-E4 Format: Laenge und nur Ziffern (F1-Muster 8/12/13/14 passend? fuehrende Nullen?)
SELECT LEN(LTRIM(RTRIM(v.EANNummer))) AS Laenge,
       CASE WHEN LTRIM(RTRIM(v.EANNummer)) NOT LIKE '%[^0-9]%' THEN 'nur Ziffern' ELSE 'sonstige' END AS Art,
       COUNT(*) AS Anzahl
FROM dbo.KHKArtikelvarianten v
WHERE NULLIF(LTRIM(RTRIM(v.EANNummer)), '') IS NOT NULL
GROUP BY LEN(LTRIM(RTRIM(v.EANNummer))),
         CASE WHEN LTRIM(RTRIM(v.EANNummer)) NOT LIKE '%[^0-9]%' THEN 'nur Ziffern' ELSE 'sonstige' END;
```

> **Ergebnis P-E1–P-E4 (Dev, 2026-11-07, AKE-Test `ake_TEST2026` auf AKESQL20, nur `SELECT`, Mandant 1):**
> - **P-E1:** Stamm-EAN gibt es nur in **`KHKArtikelVarianten.EANNummer` varchar(15)** – der Platzhalter oben ist damit die echte Spalte.
>   Weitere Treffer sind keine Artikel-Stammfelder: `KHKIVBelegePositionen(.Historie).EANNummer` (Belegpositionen), `KHKEInvoiceImportPositionen.XMLEANNummer`
>   (E-Rechnung), `KHKPpsArbeitsplaetze.NurBarcode`, `…AnsprechpartnerID` (Namenstreffer „EAN“ in „…ansprEchpArtNer…“ nicht relevant). Keine Lieferanten-/Verpackungs-EAN.
> - **P-E2:** 279.529 Varianten, **2 mit EAN**, 2 Artikel mit EAN; **kein** Artikel mit mehr als einer EAN → Annahme „eine EAN je Artikel“ trifft zu.
> - **P-E3:** **keine** EAN bei mehreren Artikeln → Mehrtreffer kommen am AKE-Test nicht vor (Auswahlfenster nur über Testdaten prüfbar).
> - **P-E4:** beide 13 Stellen, nur Ziffern (EAN-13) → Muster 8/12/13/14 passt.
> - Die zwei Artikel (beide Bestellartikel, aktiv → in der AKE-SData-Menge): **00726502** „Teroson-IX HOB38KG“ = `4002872120264`,
>   **EK011488** „Dachboden-Dämmelement Open plus 160mm, 1000x500mm“ = `9007646008637`. SData `ArtikelVariantenExtended` liefert für
>   00722001 `EANNummer = null` (lesend 2026-11-07).
> - **Einordnung:** Kein Blocker nach Auftrag (P-E2/P-E3 weichen nicht ab). Fachlich wirkt die Funktion am AKE-Test aber nur für diese zwei Artikel:
>   die EANs sind in Sage AKE praktisch nicht gepflegt. Ob IDEAL die EANs pflegt (Backlog-Anlass), ist erst am IDEAL-Endpunkt prüfbar (G2).

WMS-seitig (DB `IDEAL_AKE_WMS`, je Standort; beantwortbar ohne Sage):

```sql
-- P-E5 Befuellung des Satelliten nach einem SData-Artikellauf
SELECT COUNT(*) AS Satellitenzeilen, COUNT(NULLIF(LTRIM(RTRIM(EANNummer)), '')) AS MitEan FROM dbo.ArticleSageDetails;
-- P-E6 Artikelnummern in GTIN-Form (F2: diese werden immer ueber die Nummer gefunden)
SELECT COUNT(*) FROM dbo.Articles
WHERE ArticleNumber NOT LIKE '%[^0-9]%' AND LEN(ArticleNumber) IN (8, 12, 13, 14);
```

Abhängig von P-E2/P-E3/P-E4 (unverifiziert): ob eine EAN je Artikel reicht (Rückfrage 2), wie oft F5 greift,
ob UPC-A (12) in Sage mit führender Null als 13 Stellen steht (dann findet ein 12-stelliger Scan nichts –
Folgepunkt, nicht in dieser Spec).

## Migrations-/SQL-Auswirkungen

**Keine.** Spalte, Migration (`20261106184123_AddArticleSageDetails`), `SQL/99_AddArticleSageDetails.sql` und
FreshInstall (`:243`) existieren bereits. Kein Index (Begründung oben, Muster Matchcode). Der reservierte
Block **SQL/114–115 wird nicht genutzt** und kann im Bündel freigegeben werden. `SQL/AgentJobs/` unberührt
(der Artikel-AgentJob schreibt den Satelliten nicht, `ArticleSageDetail.cs:7-9`).

## Einstellungen

Keine neue oder geänderte Einstellung. Begründung: Der EAN-Zweig greift nur bei GTIN-förmigen Scans, prüft
die Artikelnummer zuerst und fällt ohne Treffer auf das heutige Verhalten zurück – ein Schalter hätte nichts
abzuschalten (YAGNI).
Wirksam wird das Feature über die **bestehende, unveränderte** ServiceSetting `Sync:Quelle:Artikel`
(ADR 0019/0020, Auswahlliste `View | SData | Vergleich`): nur mit `SData` füllt der Artikel-Abgleich
`EANNummer`. Das gehört in die Betriebsnotiz/Hilfe, nicht in einen neuen Key.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| keine | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine: Das Feature liest nur. Der Satellit wird weiterhin ausschließlich vom Artikel-Abgleich (SData) mit
Audit `IDEALAKEWMSService` geschrieben (`ArticleSageDetailsWriter.cs:36, 47-49`).

## Rollen / Zugriff

Keine neue Rolle, kein neuer Filter. `by-ean` steht in `ArticlesApiController` ohne Rollen-Filter, gleich
`by-number`/`by-matchcode` (`controller.md:39`); liefert nur Id, Artikelnummer, Bezeichnung. Die Scan-Seiten
behalten ihre bestehenden Filter. `controller.md:39` um `by-ean` ergänzen.

## Listen-View-Pattern

Nicht betroffen (keine neue Tabellenansicht; das Auswahlfenster ist eine Liste von höchstens 5 Knöpfen).

## Akzeptanzkriterien

1. `GET /api/articles/by-ean?ean=` mit leerem/Leerzeichen-Wert → 400.
2. `by-ean` mit einem Wert, der exakt eine Artikelnummer ist → genau dieser Artikel, auch wenn ein anderer
   Artikel dieselbe Zeichenfolge als EAN trägt (F2).
3. `by-ean` mit einer EAN, die genau ein Artikel im Satelliten trägt (auch mit Leerzeichen am Ende in der DB)
   → dieser Artikel; DUMMY-Artikel wird nie geliefert.
4. `by-ean` mit einer EAN an 2 Artikeln → beide, sortiert nach Artikelnummer, `more = false`; an 6 Artikeln →
   5 Artikel, `more = true`.
5. `by-ean` ohne Treffer / Artikel ohne Satellitenzeile → `articles` leer.
6. Kamera- und Hardware-Scan einer bekannten EAN in Wareneingang (Select2) → Artikel ausgewählt,
   Rückmeldung „Gescannt: <Artikelnummer>“.
7. Dasselbe im Sammel-Wareneingang → Zeile angelegt bzw. Menge erhöht (Rückruf läuft mit der Artikelnummer).
8. Dasselbe in Artikelinfo und Stückliste → Artikel gefunden bzw. Feld mit der Artikelnummer befüllt.
9. „Scannen ohne Feld“ (Einbuchung) mit EAN → Artikel befüllt.
10. Unbekannte EAN → Verhalten exakt wie vor der Änderung (Text, Rückruf `nichtGefunden`), in beiden
    `QrErstesFeld`-Modi.
11. EAN an 2–5 Artikeln → Auswahlfenster; Auswahl befüllt wie AK 6; „Abbrechen“ befüllt nichts; mehr als 5 →
    Meldung F5, nichts befüllt.
12. Artikel-QR (mit Trennzeichen), Lagerplatz-, FA-Scans und nicht GTIN-förmige Werte lösen **keinen**
    `by-ean`-Aufruf aus (Netzwerk-Tab).
13. Server nicht erreichbar → Meldung F6, nichts befüllt.
14. Hilfe und README sagen nicht mehr „EAN-Codes kennt das WMS nicht“ und nennen die IDEAL-Lücke.
15. Build + alle Tests grün; neue Tests für AK 1–5 (Repository + Controller) und ein Wächter, dass der
    EAN-Zweig im Wrapper vor dem Matchcode-Zweig steht und nur für `scanType === 'article'` greift
    (Muster `HardwareScannerTests`).

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md`: **TS-1xx — EAN von Einkaufsartikeln scannen (v1.78.0)** (nächste
freie Nummer nach dem Einstellungen-Epic; Dev-Lauf prüft), danach `secondbrain/tests/testszenarien-index.md`.

- **Vorbedingungen:** DEV **AKE**: `Sync:Quelle:Artikel = SData` gegen den AKE-Test, ein Artikelabgleich
  gelaufen, P-E5 zeigt Zeilen mit EAN. Testdaten per P-E5 auswählen: Artikel A mit eindeutiger EAN; falls P-E3
  nichts liefert, für AK 11 im DEV zwei Satellitenzeilen per SQL auf dieselbe EAN setzen (nur DEV, Hinweis:
  nächster SData-Lauf überschreibt). DEV **IDEAL**: unverändert (View).
- **Schritte AKE:** AK 6–13 an Inbound, InboundBulk, Articles/Info, Picking/Bom (Kamera + Netum,
  Kamera-Fenster-Ziel), Einbuchung „Scannen ohne Feld“; einmal mit `QrErstesFeld = Matchcode`.
- **Schritte IDEAL:** EAN-Scan → „Nicht gefunden“ wie bisher (dokumentierte Lücke, Rückfrage 1); normale
  Artikel-, QR- und Lagerplatz-Scans unverändert.
- **Negativfälle:** unbekannte EAN; 7-/9-/11-stellige Ziffernfolge (kein EAN-Zweig); QR mit Trennzeichen;
  numerische Artikelnummer mit 13 Stellen (über Nummer gefunden, falls P-E6 > 0); Server gestoppt (F6);
  zweiter Hardware-Scan bei offenem Auswahlfenster wählt nichts aus.

## Etappen (nur bei epic: true)

Entfällt (kein Epic).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

**Vorläufig** – der QA-Lauf bestätigt gegen den echten Diff.

- **Web-App:** ja (Controller, Repository, `barcode-scanner.js`, Hilfe).
- **Service:** nein (der SData-Reader schreibt `EANNummer` schon seit v1.73.0).
- **Migration:** nein (SQL/114–115 ungenutzt).
- **Voraussetzung Wirkung:** AKE – `Sync:Quelle:Artikel = SData` (hängt an SData-Spec G1); IDEAL – keine
  Wirkung bis Rückfrage 1 gelöst.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
- Browser-Cache: `barcode-scanner.js` mit `asp-append-version` prüfen, sonst Hinweis „Strg+F5 an den Handhelds“.

**Endgültig (qa-agent 2026-11-07, aus `git diff bd178dc4 8534da23`):**
- **Web:** ja – `ArticlesApiController`, `ArticleRepository`/`IArticleRepository`, `wwwroot/js/barcode-scanner.js`, Hilfe, Changelog, `AppVersion.cs`.
- **Service:** ja, aber nur `IDEALAKEWMSService/AppVersion.cs` (1.78.0) – mitpublizieren, damit Web und Dienst denselben Stand melden.
- **Migration:** nein (keine Datei unter `*/Migrations/`, kein `SQL/`-Skript, kein Einmal-Skript).
- Ablauf: Publish aus dem Worktree → Testsystem → Test → Merge.
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- `barcode-scanner.js` wird mit `?v=`-Hash geladen (im DEV-Lauf gesehen), kein Strg+F5 nötig.
- Nach dem Merge nur dann erneut aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen zusammengeführt hat.

## Risiken / Grenzen (bewusst)

- Ohne SData-Artikelweg wirkungslos (AKE bis Umschaltung, IDEAL bis Rückfrage 1) – Hilfe sagt das.
- Nur eine EAN je Artikel (erste Ausprägung) – Rückfrage 2.
- `QrErstesFeld = Matchcode`: ein GTIN-förmiger Matchcode, der zugleich Artikelnummer eines anderen Artikels
  ist, wird jetzt über die Nummer aufgelöst (F2). Sehr unwahrscheinlich; P-E6 zeigt die Größenordnung.
- Tabellenscan je EAN-Scan (kein Index) – nur bei GTIN-förmigen Scans; Index erst bei gemessener Latenz
  (dafür wäre SQL/114 vorgesehen gewesen).

## Offene Rueckfragen

1. **Quelle der EAN / Lücke IDEAL** – Backlog: Spalte `EAN` in `vw_IDEAL_AKE_WMS_Artikel`. Vorhanden ist
   `ArticleSageDetails.EANNummer`, nur vom SData-Weg geschrieben. Empfehlung: Lookup nur über den Satelliten
   (keine Migration, SQL-Weg bleibt bit-gleich nach ADR 0019). Lücke: IDEAL bleibt ohne EAN, solange die
   Artikel-View gesetzt ist (SData gesperrt, P-R1) und kein IDEAL-SData-Endpunkt existiert (G2); AKE erst ab
   `Sync:Quelle:Artikel = SData` (G1). IDEAL als Folge-Spec: SData-Endpunkt abwarten **oder** View-Spalte +
   eigenes Feld am Artikel (eigener ADR).
2. **Mehrere EAN je Artikel** – heute nur die EAN der kleinsten `AuspraegungID`. Empfehlung: reicht für diese
   Spec; nach P-E2/P-E3 neu entscheiden.
3. **Mehrtreffer** – Auswahlfenster (Backlog-Skizze) oder nur Meldung wie beim Matchcode? Empfehlung:
   Auswahlfenster bis 5 Treffer, darüber Meldung.
4. **Handeingabe** – EAN auch im Artikelinfo-Suchfeld/Select2-Suche finden? Empfehlung: nein, nur Scan-Wege.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Quelle der EAN / Lücke IDEAL → **wie empfohlen (Gerald 2026-11-07): SData-Satellit, IDEAL-Lücke als Folge-Spec**
2. Mehrere EAN je Artikel → **wie empfohlen (Gerald 2026-11-07): erste Variante genügt vorerst; nach P-E2/P-E3 neu entscheiden. P-E1–P-E4 darf der Dev am AKE-Test nur lesend selbst ausführen und in die Spec eintragen.**
3. Mehrtreffer Auswahlfenster oder Meldung → **wie empfohlen (Gerald 2026-11-07): Auswahlfenster bis 5 Treffer**
4. EAN bei Handeingabe → **wie empfohlen (Gerald 2026-11-07): nein, nur Scan**

## QA-Nachweis (qa-agent, 2026-11-07)

Review-Umfang `git diff bd178dc4 8534da23` (Commit `8534da23`, v1.78.0). Build/Tests/DEV über den ganzen Worktree
`.claude/worktrees/2026-08-07-ideal-teile-1-5`.

> [!danger] Freigabe-Widerspruch – Status NICHT auf Testbereit gesetzt
> Diese Spec trägt seit 2026-11-07 10:08 den Vermerk „Zurückgestellt (Gerald) – im /goal-Lauf NICHT umsetzen, Freigabe zurückgenommen“
> (Callout oben, ebenso [[2026-11-07-buendel-uebersicht]]: „Spec 5 EAN zurückgestellt … im Lauf überspringen“); Frontmatter
> `status: Entwurf`, `freigabe_*` leer. Umgesetzt wurde sie trotzdem (`8534da23`, 11:04). Eine spätere Wieder-Freigabe ist im Brain nicht
> belegt. Der QA-Lauf hat alles geprüft, setzt aber keinen Status. **Der Mensch entscheidet:** (a) Freigabe erneuern – dann ist die Spec
> nach diesem Nachweis testbereit (E2 beachten) – oder (b) `8534da23` aus dem Bündel nehmen (Version 1.78.0, Changelogs, TS-110 und
> Index-Zeile 110 zurücknehmen). DEV AKE und IDEAL laufen derzeit mit v1.78.0 einschließlich EAN-Zweig.

### 1. Build und Tests
`dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (Altbestand). `dotnet test`: IdealAkeWms.Tests 2430 bestanden / 1 übersprungen /
0 Fehler; IDEALAKEWMSService.Tests 497 / 0 / 0. Keine Flaky-Ausfälle in diesem Lauf.

### 2. Code-Review (selbst, synchron)
Geprüft: `ArticlesApiController.GetByEan`, `ArticleRepository.GetByEanAsync` (EF übersetzt `Trim()` nach SQL, DUMMY-Ausschluss),
`barcode-scanner.js` (EAN-Wrapper, Sicherung von `_textInputScanCallback`, `_eanAuswahl`, Zusammenspiel mit `closeScannerModal` – entfernt
das Kamera-Fenster sofort, daher keine Modal-Stapelung –, Handler `wms:hardware-scan`, „Scannen ohne Feld“, Matchcode-Wrapper), Hilfe, README,
Changelog, TS-110, Tests. Die EAN im Titel geht durch `escapeHtml`, die Trefferknöpfe setzen `textContent` – kein HTML aus Daten. Keine Werte
in `<script>`, kein `hidden` + `d-*`, keine stillen Fallbacks, keine parallelen Arrays, keine Einstellung.

| # | Befund | Schwere | Behandlung |
|---|---|---|---|
| E1 | **Freigabe zurückgenommen, trotzdem umgesetzt** (Kasten oben). | blockierend (Prozess) | offen – Mensch |
| E2 | Zweiter Hardware-Scan bei offenem Auswahlfenster: Das Scanner-Skript schluckt die Tasten (kein Enter auf den fokussierten Knopf, also keine Auswahl – Spec-Punkt 4 formal erfüllt), das Ereignis `wms:hardware-scan` erreicht aber das Seitenziel (Fokus im Fenster → einziges Ziel). Ein Nicht-EAN-Code füllt dann das Feld **unter** dem Fenster; ein zweiter Mehrtreffer-EAN ruft `_eanAuswahl` erneut auf, das das alte Fenster per `alt.remove()` entfernt, ohne die Bootstrap-Instanz zu schließen. Altes `.modal-backdrop` und `body.modal-open` bleiben stehen, die Seite bleibt grau überdeckt bis zum Neuladen. Aus dem Code abgeleitet, auf DEV nicht auslösbar (Abschnitt 6). | mittel (selten, sperrt aber die Seite am Handgerät) | offen. Vorschlag: im Handler `wms:hardware-scan` bei offenem `#eanAuswahlModal` den Scan verwerfen (`d.handled = true; return;`), in `_eanAuswahl` kein zweites Fenster öffnen bzw. ein offenes über `bootstrap.Modal.getInstance(alt).hide()` schließen |
| E3 | Nach Auswahl/Abbrechen setzt niemand den Fokus zurück (Fenster ohne Auslöser per JS geöffnet). Mit Fokus-Eingabe (Netum) kommt der nächste Scan evtl. erst nach Antippen an; „Scannen ohne Feld“ holt den Fokus über `focusout`/MutationObserver selbst zurück. | niedrig | Hinweis, in 110.3 mit Netum prüfen |
| E4 | `secondbrain/codebase/controller.md` nennt `by-ean` noch nicht (der Spec-Abschnitt „Rollen / Zugriff“ verlangt den Nachtrag). | Doku | offen – Bündelabschluss |
| E5 | `feature-map.md` hat keinen Eintrag für v1.78.0 (wie B6 der Einstellungen-Spec). | Doku | offen – Bündelabschluss |
| E6 | Kein Test für AK 3 „DUMMY nie geliefert“ und für AK 2 „auch wenn ein anderer Artikel dieselbe Zeichenfolge als EAN trägt“; kein `ArticleRepositoryTests`-Fall (AK 15 nennt das Repository). Der Code ist korrekt, auf DEV per API belegt (AK 2 nur ohne Konkurrenz-EAN). | niedrig | Hinweis |
| E7 | An AKE haben 67.061 Artikelnummern GTIN-Form (P-E6 auf AKE-DEV). Jeder Scan einer solchen Nummer geht jetzt erst über `by-ean` (Treffer über die Nummer) und danach `by-number`: zwei Anfragen statt einer. Das Ergebnis ist gleich, die Latenz steigt um einen Roundtrip. | Hinweis | keine Änderung nötig |

frontend-design-Prüfung des Auswahlfensters: Treffer und „Abbrechen“ mit `min-height:44px` (das Schließen-X ist Bootstrap-Standard und kleiner),
Fokus auf dem ersten Treffer (`shown.bs.modal`), Treffer als Text „Artikelnummer – Bezeichnung“, nicht über Farbe; `aria-labelledby` gesetzt,
`role="dialog"`/`aria-modal` setzt Bootstrap beim Öffnen; `btn-outline-primary` erfüllt AA. In Ordnung.

### 3. Nachweisart je AK
| AK | Nachweis |
|---|---|
| 1 | automatisiert (`GetByEan_ArtikelnummerZuerst_…`) + DEV (400 bei `ean=%20%20`) |
| 2 | automatisiert (teilweise, ohne Konkurrenz-EAN) + DEV (`00190220` → Artikel 1) |
| 3 | automatisiert (ungetrimmt gespeicherte EAN) + DEV; DUMMY-Ausschluss nur per Code-Review |
| 4 | automatisiert (`GetByEan_MehrAls5_More`, Mehrtreffer sortiert) + DEV |
| 5 | automatisiert + DEV |
| 6–11, 13 | Manual-UAT (Browser-JS, Scanner, Kamera) – nicht grün gezählt |
| 12 | Wächtertest `HardwareScannerTests.EanZweig_…` (Code-Muster) + Manual-UAT Netzwerk-Tab |
| 14 | Code-Review (Hilfe, README) |
| 15 | erfüllt bis auf den Repository-Test (E6) |

### 4. Geänderte bestehende Tests
Keine – nur neue Tests (`ArticlesApiControllerTests` +2, `HardwareScannerTests` +1).

### 5. Checkliste / ADR 0020
Keine Migration, kein SQL, keine Einstellung (Abschnitt „Einstellungen“ begründet das). Version 1.78.0 in beiden `AppVersion.cs`,
Anwender-Changelog, Brain-Changelog [[2026-11-07-v1-78-0-ean-einkaufsartikel-scan]], TS-110 in `docs/TESTSZENARIEN.md`, Index-Zeile 110 vorhanden.
Offen: E4 (`controller.md`), E5 (`feature-map.md`).

### 6. DEV-Deploy und Testlauf
`standort: beide`. `dev-deploy.ps1 -Standort AKE|IDEAL -Quelle <Worktree>` in erhöhter Shell, je Exit 0, „HTTP 200, Fusszeile v1.78.0“ an
http://localhost:8081 und http://localhost:8082. Testdaten AKE-DEV: `ArticleSageDetails` 5812/5813 (EK000004/EK000005) auf `'9999999999994 '`
(mit Leerzeichen), 5814–5819 (EK000006–EK000012) auf `'8888888888888'`; vorübergehend `HardwareScannerAktiv=true` und
`HardwareScannerMaxZeichenabstandMs=500`. Danach alles zurückgesetzt (8 Satelliten wieder `NULL`, 2 AppSettings-Zeilen gelöscht, MitEan wieder 2).

| Schritt | Ausführung | Ergebnis | Status |
|---|---|---|---|
| AK 3 / 110.1 API | `/api/articles/by-ean?ean=4002872120264` (AKE) | `00726502 - Teroson-IX HOB38KG`, `more:false` | OK |
| AK 4 / 110.3 API | `?ean=9999999999994` | EK000004, EK000005 (sortiert), `more:false`, trotz Leerzeichen in der DB | OK |
| AK 4 (>5) | `?ean=8888888888888` | 5 Artikel EK000006…EK000011, `more:true` | OK |
| AK 2 / 110.5 | `?ean=00190220` | Artikel 00190220 über die Nummer | OK |
| AK 5 / 110.4 | `?ean=1111111111111` | `{"articles":[],"more":false}` | OK |
| AK 1 | `?ean=%20%20` | HTTP 400 | OK |
| 110.1–110.3 Client (Handscanner-Feld im Kamera-Fenster, Einbuchung AKE) | Fenster geöffnet (keine Kamera, Handscanner-Feld fokussiert), `~09…` + Enter per Tastatur-Automation | Die automatisierten Tasten wurden nicht als Scan erkannt, kein `by-ean`-Aufruf im Netzwerk-Log. Kein JS-Konsolenwerkzeug im Lauf, daher kein direkter `processScannedValue(...)`-Aufruf | NICHT MOEGLICH |
| 110.3 Auswahlfenster, E2/E3 | – | braucht den Client-Zweig (s. o.) | NICHT MOEGLICH |
| 110.6 Offline | – | Netz trennen im Automations-Browser nicht möglich | NICHT MOEGLICH |
| 110.7 IDEAL | `ArticleSageDetails` an IDEAL-DEV: 0 Zeilen → dort nie ein EAN-Treffer | per SQL belegt; die Scan-Regression braucht ein Gerät | teilweise, Rest NICHT MOEGLICH |

### Gesamturteil
Code und Tests in Ordnung, API-Verhalten auf DEV belegt. **Nicht Testbereit gesetzt** wegen E1 (Freigabe zurückgenommen); der Status bleibt
`Entwurf`, bis der Mensch entscheidet. Fachlich offen: E2 (Backdrop-Sperre beim zweiten Scan, Vorschlag oben); E3–E7 sind Hinweise bzw. Doku.

### Manuelle Test-Checkliste (Mensch)
1. E1 entscheiden: Freigabe erneuern oder `8534da23` aus dem Bündel nehmen.
2. E2 entscheiden: vor dem Test nachbessern lassen oder als bekannte Grenze akzeptieren.
3. Testsystem AKE: `Sync:Quelle:Artikel = SData`, Artikellauf, P-E5; dann 110.1 (Kamera/Bild) und 110.2 (Netum mit Fokus im Artikelfeld und „Scannen ohne Feld“) mit `4002872120264`.
4. 110.3 mit zwei per SQL gleich gesetzten EANs: Auswahlfenster, Fokus auf dem ersten Eintrag, Auswahl füllt, Abbrechen/Escape füllt nichts; danach ohne Antippen erneut scannen (E3).
5. E2 nachstellen: bei offenem Auswahlfenster einen zweiten Code bzw. dieselbe EAN scannen – bleibt die Seite bedienbar?
6. 110.4 in beiden `QrErstesFeld`-Modi; 110.5 mit einer 8-stelligen Artikelnummer; 110.6 Netz trennen.
7. AK 7/8: Sammel-Wareneingang (Zeile/Menge), Artikelinfo, Stückliste mit EAN.
8. 110.7: QR mit Trennzeichen, Lagerplatz, FA unverändert; IDEAL: EAN-Scan wie bisher „nicht gefunden“.
9. Bündelabschluss: E4 `controller.md` um `by-ean` ergänzen, E5 `feature-map.md` v1.78.0.
