---
type: spec
title: "QR-Code: Matchcode statt Artikelnummer"
slug: 2026-10-23-qr-code-matchcode-statt-artikelnummer-spec
status: Testbereit
created: 2026-10-23
updated: 2026-10-27
source_backlog: "[[2026-10-23-qr-code-matchcode-statt-artikelnummer]]"
task: "[[2026-10-23-qr-code-matchcode-statt-artikelnummer-umsetzung]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - "IdealAkeWms/Models/AppSettingKeys.cs:32 (neuer Key QrErstesFeld direkt nach QrMitFaNummer)"
  - "IdealAkeWms/Program.cs:456-474 (Laufzeit-Seed QrErstesFeld = Artikelnummer im Array missingSettings neben QrMitFaNummer :460)"
  - "IdealAkeWms/Views/Settings/Index.cshtml:45 (Gruppe Allgemein: QrMitFaNummer + QrErstesFeld)"
  - "IdealAkeWms/Controllers/SettingsController.cs:42-51 (SaveSettings: Wert von QrErstesFeld pruefen und kanonisch speichern)"
  - "IdealAkeWms/Views/Shared/_Layout.cshtml:15-28 (body-Attribut data-qr-erstes-feld nur bei Matchcode)"
  - "IdealAkeWms/Data/Repositories/IArticleRepository.cs:16 + ArticleRepository.cs:91-100 (laut Freigabe-Nachtrag: vorhandene GetByMatchcodeExactAsync(matchcode, limit) erweitert, getrimmt, ohne Gross/Klein, Liste mit Limit; kein neues GetByMatchcodeAsync)"
  - "IdealAkeWms/Controllers/ArticlesApiController.cs:57-68 (neuer Endpunkt GET /api/articles/by-matchcode?matchcode=)"
  - "IdealAkeWms/wwwroot/js/barcode-scanner.js:426-443 (globaler Wrapper processScannedValue: Matchcode-Zweig vor der unveraenderten Auswertung)"
  - "IdealAkeWms/Views/Articles/Info.cshtml:31 (Hinweistext QR-Format)"
  - "IdealAkeWms/Controllers/ArticlesController.cs:224-231 (nur bei Freigabe Rueckfrage 3: stiller Matchcode-Fallback FirstOrDefault)"
  - "IdealAkeWms/Views/Help/Index.cshtml:82-138, :171-173, :1683-1684 (Hilfe)"
  - "IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs, Views/Help/Changelog.cshtml (Version v1.68.0, beim /dev vergeben)"
  - "IdealAkeWms.Tests/Repositories/ArticleRepositoryTests.cs, Controllers/ArticlesApiControllerTests.cs, Views/HardwareScannerTests.cs bzw. neue Quelltext-Waechter-Klasse"
  - "README.md:246, :389 (AppSettings-Tabelle + QR-Abschnitt), docs/TESTSZENARIEN.md (TS-101, beim /dev vergeben)"
open_questions:
  - "Pflege von QrErstesFeld global unter /Settings (Gruppe Allgemein neben QrMitFaNummer) oder in den Standorteinstellungen? Empfehlung: /Settings."
  - "Zusaetzlicher dritter Wert Automatisch (erst Artikelnummer, sonst Matchcode)? Empfehlung: nein fuer den Start; ja nur, wenn an einem Standort beide Etikettenarten dauerhaft im Umlauf sind."
  - "Artikelinfo hat heute schon einen stillen Matchcode-Fallback (ArticlesController.cs:226, FirstOrDefault waehlt bei Mehrdeutigkeit still einen Artikel). Mitbeheben mit derselben Repository-Methode und Meldung? Empfehlung: ja."
  - "Bestandsuebersicht uebernimmt die FA aus dem QR immer (data-qr-fa-enabled hart true, StockOverview/Index.cshtml:18), unabhaengig von QrMitFaNummer. So lassen oder an QrMitFaNummer binden? Empfehlung: so lassen, eigener Backlog-Punkt."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-10-27"
---

# QR-Code: Matchcode statt Artikelnummer

> [!info] Code-Grundlage
> Alle Datei:Zeile-Angaben sind im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` gemessen (Zweig
> `feature/2026-08-07-ideal-teile-1-5`, v1.67.1, HEAD `a2651019`), nur gelesen. **Version (v1.68.0) und Testkapitel (TS-101)
> vergibt der Dev-Lauf.** Zeilennummern vor dem Start kurz nachmessen, falls das Bündel weitergewachsen ist.
> **Standorte:** AKE und IDEAL gleich. `barcode-scanner.js` und die Scan-Seiten verzweigen nicht nach
> `ProduktionsauftragHierarchisch`. Nur die OSEON-Seite (`Tracking/OseonIndex`) gibt es fachlich nur an AKE.

> [!important] Freigabe-Nachtrag 2026-10-27 (geht dem Rumpf vor)
> Antworten 1–4 wie empfohlen (Block „Freigabe-Antworten“). P1: keine doppelten Matchcodes an IDEAL.
> **Keine zweite Suche:** Statt eines neuen `GetByMatchcodeAsync` wird die vorhandene `GetByMatchcodeExactAsync` (genutzt in
> `ArticlesController.cs:226`) erweitert (getrimmt, ohne Groß/Klein, Ergebnis eindeutig/mehrdeutig/keins) und von Endpunkt und
> Artikelinfo gemeinsam genutzt. Umsetzung im Bündel **nach** dem Scan-Handler-Pilot v1.67.2 (beide ändern `barcode-scanner.js`).

## Ziel / Nutzen (das Warum)

An IDEAL gibt es Etiketten, deren QR-Code im ersten Feld den **Matchcode** trägt statt der Artikelnummer, z. B.
`G-KT-FRR-510-2010--1760B-250-V01;;1065141;09` (laut Hannes der Matchcode von Artikel 50002134). Heute nimmt jede
Scan-Seite das erste Feld als Artikelnummer (`barcode-scanner.js:300-302`), findet den Artikel nicht und meldet
„Nicht gefunden“. Mit einer Einstellung soll das erste Feld als Matchcode gelesen und über den Artikelstamm in die
Artikelnummer übersetzt werden. Danach läuft alles wie heute. Zusammen mit `QrMitFaNummer` ergeben sich vier Fälle:

| `QrErstesFeld` | `QrMitFaNummer` | Feld 1 | Feld 3 |
|---|---|---|---|
| Artikelnummer (Standard) | aus | Artikelnummer | ignoriert |
| Artikelnummer | an | Artikelnummer | FA-Nummer |
| Matchcode | aus | Matchcode → Artikelnummer | ignoriert |
| Matchcode | an | Matchcode → Artikelnummer | FA-Nummer |

Wo Feld 3 tatsächlich ankommt, hängt heute von der Seite ab. Das ist unten belegt und ändert sich nicht.

## Umfang (In-Scope / Out-of-Scope)

### In-Scope

- Neue AppSetting `QrErstesFeld` mit den Werten `Artikelnummer` (Standard) und `Matchcode`. Kein Satz aus vier
  Einzelschaltern.
- Neuer Lese-Endpunkt `GET /api/articles/by-matchcode?matchcode=…` mit Repository-Methode.
- Ein Matchcode-Zweig im **globalen** `processScannedValue`-Wrapper. Er wirkt damit auf Kamera, Bild-Upload und
  Hardware-Scanner auf allen Scan-Seiten.
- Meldungen bei „kein Treffer“, „mehrdeutig“ und Verbindungsfehler.
- Prüfung des Werts beim Speichern unter `/Settings`.
- Hilfe, README, Testszenario, Version.

### Out-of-Scope (nur melden, nicht mitbauen)

- `QrMitFaNummer` auf weitere Seiten ausdehnen (siehe Abschnitt „Wo `QrMitFaNummer` nicht wirkt“, Rückfrage 4).
- Ein Auswahldialog bei mehrdeutigem Matchcode. Die Anforderung verlangt eine Meldung, keine Auswahl.
- Der Modus „Automatisch“, außer Rückfrage 2 wird mit Ja beantwortet.
- Scans in Felder, die **kein** Scan-Ziel sind (Regel (b) des Hardware-Scanners, z. B. ein Spaltenfilter). Dort landet
  der Rohtext wie heute (`hardware-scanner.js:264-267`).
- **Zusatzfund:** Die Standard-Initialisierung `initScanner('btnScanArticle', 'ArticleId', 'article')`
  (`barcode-scanner.js:501`) bindet auch auf der OSEON-Seite. Dort heißt der Knopf ebenfalls `btnScanArticle`
  (`OseonIndex.cshtml:26`), und `initTextInputScanner` bindet ihn ein zweites Mal (`:312`). Ein Klick löst also zwei
  `openScannerModal` aus. Der Hardware-Pfad ist nicht betroffen, weil `ArticleId` dort fehlt
  (`barcode-scanner.js:475`). Das Verhalten ist alt und wurde in TS-99 abgenommen. Hier nur gemeldet.

## Fachliche Anforderungen

1. **Einstellung.** `QrErstesFeld` kennt genau `Artikelnummer` und `Matchcode`. Fehlt die Zeile oder steht ein anderer
   Wert in der DB, gilt `Artikelnummer`, also das heutige Verhalten.
2. **Nur Artikel-Scans.** Die Einstellung wirkt nur bei `scanType === 'article'`. Lagerplatz-Scans und FA-Scans
   (`productionOrder`, `initTextInputScanner(..., 'fa')`) bleiben unverändert. Die FA-Felder lesen immer Feld 3
   (`barcode-scanner.js:292-299`).
3. **Übersetzung.** Bei `Matchcode` wird Feld 1 (vor dem ersten `;`, getrimmt) im Artikelstamm gesucht. Gesucht wird
   exakt, nicht als Teilstring. Leerzeichen am Anfang und Ende zählen auf beiden Seiten nicht, Groß-/Kleinschreibung
   auch nicht. Bei **genau einem** Treffer wird Feld 1 durch dessen Artikelnummer ersetzt. Felder 2 ff. bleiben
   unverändert, danach läuft die heutige Auswertung.
4. **Kein Treffer:** Meldung „Matchcode „<Wert>“ nicht gefunden“. Keine Auswahl, kein Filter, kein FA-Feld wird
   befüllt oder geleert.
5. **Mehrdeutig (≥ 2 Treffer):** Meldung „Matchcode „<Wert>“ ist mehrdeutig (<n> Artikel: <Nr1>, <Nr2>, …) – bitte den
   Artikel von Hand wählen“. Es werden höchstens 5 Nummern gezeigt, bei mehr steht „…“ dahinter. Es wird **nicht** still
   gewählt, und kein Feld wird befüllt.
6. **Verbindungs-/Serverfehler:** Meldung „Matchcode „<Wert>“ konnte nicht geprüft werden“. Die Meldung muss sich von
   „nicht gefunden“ unterscheiden.
7. **Leeres Feld 1** (z. B. `;;1065141;09`): Es gibt keine Abfrage, das heutige Verhalten bleibt.
8. **Kein Fallback auf die Artikelnummer** im Modus `Matchcode`. Ein Etikett `102044;;1065141;09` wird dort als
   Matchcode `102044` gesucht. Das ist gewollt, siehe Rückfrage 2.
9. **Modus `Artikelnummer`** verhält sich Zeichen für Zeichen wie heute: synchron, ohne zusätzliche Abfrage.

## Technischer Loesungsentwurf

### 1. Wo steht der Matchcode? (belegt, nicht aus dem Namen abgeleitet)

| | Beleg |
|---|---|
| Feld | `Article.Matchcode`, `string?`, `[StringLength(200)]` (`Models/Article.cs:30-32`). Spalte `dbo.Articles.Matchcode NVARCHAR(200) NULL` (Migration `20260915094646_AddArticleMatchcode.cs:13-19`, `SQL/91_AddArticleMatchcode.sql:11-13`, `SQL/00_FreshInstall.sql:149`) |
| Eindeutigkeit im WMS | **Nicht erzwungen.** Unique ist nur `ArticleNumber` (`ApplicationDbContext.cs:232`). Auf `Matchcode` gibt es keinen Index und keine Constraint (`ApplicationDbContext.cs:218-238`, Migration ohne Index) |
| Schreibweg 1: Sync | `SageImportService.cs:388-423` liest Spalte 6 **ungetrimmt** (`:422`), Upsert `:469-495` (`[Matchcode] = @Matchcode`, Insert `:491/494`), Parameter `:539`. Quelle: ohne `Sync:ArticlesViewName` `KHKArtikel.Matchcode` (`SageArticleSql.cs:20`, `:36`), mit View deren Spalte `Matchcode` (`SageArticleSql.cs:61-73`). `GROUP BY ArticleNumber` (`:52`, `:72`) schützt nur gegen Artikel-Dubletten, **nicht** gegen gleiche Matchcodes verschiedener Artikel |
| Schreibweg 2: Handpflege | `ArticlesController.Edit` POST `:182` (roh, kein Trim), `Create` `:94-104` (Model-Binding des ganzen `Article`, also auch `Matchcode`) |
| Schreibweg 3: SQL | Nur ein auskommentiertes Einmal-Skript (`SQL/Einmalig/2026-10-15_IDEAL-Sage-Views_Bereinigung00erArtikel.sql:172-175`). Kein AgentJob schreibt `Matchcode` (Grep `Matchcode` in `SQL/`) |
| Leser heute | `SearchAsync` Teilstring (`ArticleRepository.cs:18-28`), Artikelliste (`:54-75`, `:102`), `GetMatchcodesByArticleNumbersAsync` (`:81-89`), `GetByMatchcodeExactAsync` (`:91-92`, nur Artikelinfo `ArticlesController.cs:226`), Bestandsfilter Teilstring (`StockMovementRepository.cs:27-33`, `:88-94`, `:322-328`) |

**Folgerungen:**

- Weder Code noch Schema garantieren, dass ein Matchcode eindeutig ist. Die Mehrdeutigkeits-Meldung ist deshalb
  Pflicht. Ob es heute Dubletten gibt, beantworten P1 und P4.
- Werte können führende oder abschließende Leerzeichen tragen (Sync und Handpflege trimmen nicht). Darum wird auf
  **beiden** Seiten getrimmt.
- Die Groß-/Kleinschreibung hängt sonst an der Kollation. Darum wird ausdrücklich verglichen (`ToUpper` auf beiden
  Seiten), unabhängig von der Kollation und unter EF-InMemory testbar. P3 zeigt die Kollation nur zur Information.
- `Articles` ist eine gefilterte Projektion von Sage ([[fallstricke]] §12). Ein Artikel, der dort fehlt, wird nie
  übersetzt („nicht gefunden“). P2 prüft das für die Testartikel.
- An IDEAL zeigt die Stückliste den Positions-Matchcode `FaHierarchyNode.Matchcode` (seit v1.62.0, [[fallstricke]] §12
  „Stueckliste: Matchcode an IDEAL aus der Struktur“). Weicht er vom Artikelstamm ab und steht er so auf dem
  Etikett, gibt es „nicht gefunden“, obwohl der Wert in der Stückliste sichtbar ist. Die Anforderung legt den
  Artikelstamm als Quelle fest. Das ist ein Risiko, P5 misst es.

### 2. Wo übersetzt wird: Server-Endpunkt (Entscheidung)

**Der Server übersetzt, der Client ruft ihn einmal je Scan auf.** Gründe:

- Der Artikelstamm liegt nur auf dem Server. Auf den Filterseiten (Bestandsübersicht, FA-Liste, Leitstand, OSEON,
  Artikelinfo) hat der Client keine Artikeldaten. Nur die Stückliste trägt Matchcodes im DOM, und an IDEAL stammen sie
  aus einer **anderen** Quelle (Struktur, siehe oben). Eine reine Client-Lösung ginge also nicht für alle Seiten und
  wäre an der Stückliste fachlich eine andere Regel.
- Es gibt eine Regel an **einem** Ort (Repository-Methode). Alle Seiten nutzen sie, und sie ist InMemory-testbar.
- Der Client-Einstieg ist schon vorhanden: Alle Wege laufen über die **globale**, gewrappte `processScannedValue`.
  Das sind Kamera (`barcode-scanner.js:207`), Bild-Upload (`:258`), Hardware-Scanner bei offenem Kamera-Fenster
  (`:481`) und bei Fokus/einzigem Ziel (`:495`). Das hat die Hardware-Scanner-Spec so festgelegt
  ([[2026-10-15-scan-handler-hardware-scanner-spec]], „Die Auswertung bleibt allein in `processScannedValue`“).

**Warum im Wrapper und nicht in der inneren Funktion:** Der Wrapper (`:428-443`) ruft die innere Funktion **synchron**
und feuert danach sofort den `onScanned`-Callback mit `target.value` (`:437-439`). Wer erst in der inneren Funktion
asynchron übersetzt, löst die Callbacks der Filterseiten (Form-Submit) und des Sammel-Wareneingangs **vor** der
Übersetzung aus. Darum steht der Matchcode-Zweig **am Anfang des Wrappers**:

```text
processScannedValue = function (value, targetId, scanType, qrFaEnabled, faTargetId) {
    if (scanType !== 'article' || body hat kein data-qr-erstes-feld="Matchcode")  -> heutiger Ablauf, unverändert, synchron
    feld1 = value.split(';')[0].trim();  leer -> heutiger Ablauf
    cb = passender _textInputScanCallback (jetzt merken, nicht erst nach der Antwort)
    GET /api/articles/by-matchcode?matchcode=<feld1>
      genau 1 Treffer -> neuerWert = nr + (value.indexOf(';') >= 0 ? value.slice(value.indexOf(';')) : '')
                         heutiger Ablauf mit neuerWert (inkl. Callback cb)
      0 / >=2 / Fehler -> Meldung (Anforderungen 4-6), cb verwerfen, nichts befüllen
}
```

- **Eine** Abfrage-Stelle. Die innere Funktion und der Hardware-Listener bleiben unverändert. Die exakten
  Quelltext-Wächter in `HardwareScannerTests.cs:93-96` bleiben grün.
- Auf Select2-Seiten (Ein-/Aus-/Umbuchung) folgt danach die heutige `by-number`-Abfrage (`:330-343`). Das sind zwei
  Requests je Scan. ponytail: bewusst so, denn die alte Kette bleibt unberührt. Ein Sonderweg mit `id`/`text` aus dem
  neuen Endpunkt lohnt erst bei messbarer Latenz.
- **Meldungsort:** Ist das Ziel sichtbar, wie bisher `showScanFeedback` am Ziel. Bei Select2 ist es die `.input-group`
  (Muster `:327-329`). Ist das Ziel `type="hidden"` (`bomScanArticle` `Bom.cshtml:75`, `bulkScanBuffer`
  `InboundBulk.cshtml:106`), steht die Meldung am Scan-Knopf. Dessen Id liefert der Eintrag in `_hwScanTargets`
  (`:20`, `:420`). Am Sammel-Wareneingang ist `#bulkScanFeedback` (`InboundBulk.cshtml:105`, `aria-live`) der bessere Ort,
  wenn trivial erreichbar. Die Meldung ist Text mit Zeichen (✗), nicht nur Farbe. Der Wert wird mit `escapeHtml`
  ausgegeben (`:396-400`). Die Meldung bleibt mindestens so lange sichtbar wie heute (5 s, `:382-384`). Bei
  „mehrdeutig“ ist das ausdrücklich zu prüfen (längerer Text), gegebenenfalls 10 s.
- **Reihenfolge schneller Folgescans:** Antworten können sich überholen. Es gibt bewusst keinen „nur letzte Antwort
  zählt“-Wächter, denn am Sammel-Wareneingang zählt jeder Scan eine Menge hoch (`InboundBulk.cshtml:246-252`), und ein
  verworfener Scan wäre ein stiller Mengenfehler. Als Risiko vermerkt.

### 3. Repository und Endpunkt

- `IArticleRepository.GetByMatchcodeAsync(string matchcode, int limit)` → `List<Article>`:
  `key = matchcode.Trim().ToUpper()`;
  `_dbSet.Where(a => a.Matchcode != null && a.Matchcode.Trim().ToUpper() == key && a.ArticleNumber != Article.DummyArticleNumber)
  .OrderBy(a => a.ArticleNumber).Take(limit)`.
  EF übersetzt `Trim()`/`ToUpper()` zu `LTRIM(RTRIM())`/`UPPER()`. Unter InMemory läuft es als LINQ-to-Objects, also gibt
  es keinen `EF.Functions.Like` ([[fallstricke]] §12). Den DUMMY schließt der Endpunkt aus wie die Suche
  (`ArticlesApiController.cs:39`, `:47`).
  ponytail: ohne Index ist das ein Tabellenscan je Scan. Das ist bei der Artikelzahl eines Standorts unkritisch. Ein
  berechneter, indizierter Spaltenwert lohnt erst bei gemessener Latenz.
- `GetByMatchcodeExactAsync` (`ArticleRepository.cs:91-92`) bleibt unverändert, außer Rückfrage 3 wird mit Ja
  beantwortet.
- `ArticlesApiController`: `[HttpGet("by-matchcode")] GetByMatchcode([FromQuery] string? matchcode)`.
  - Leer/Whitespace → `BadRequest()`.
  - Sonst `GetByMatchcodeAsync(matchcode, 6)` → `Ok(new { articles = [{ id, articleNumber, text }] (max 5), more = count > 5 })`.
  - `text` im Format von `by-number` (`:66`, „Nummer - Bezeichnung“, [[fallstricke]] §3 „Select2-Textformat“).
  - **Query-String statt Route-Segment**, weil ein Matchcode `/`, `#` oder `%` enthalten kann. Ein kodiertes `/` im
    Pfad lehnt IIS je nach Konfiguration ab.

### 4. Einstellung

- `AppSettingKeys.QrErstesFeld = "QrErstesFeld"` nach `:32`.
- **Seed** im Laufzeit-Array `missingSettings` (`Program.cs:456-474`) neben `QrMitFaNummer` (`:460`):
  `("QrErstesFeld", "Artikelnummer", "QR-Code: erstes Feld ist Artikelnummer oder Matchcode (Werte: Artikelnummer | Matchcode)")`.
  Der Seed ist Pflicht, weil `/Settings` nur vorhandene Zeilen zeigt (`Settings/Index.cshtml:53-56` über
  `Model.Settings`, `SettingsController.cs:34`). Er läuft bei jedem Start und ist idempotent (`:464`).
- `/Settings` Gruppe „Allgemein“ (`Settings/Index.cshtml:45`): `new[] { "QrMitFaNummer", "QrErstesFeld" }`. Die Maske
  rendert Nicht-Bool-Werte als Textfeld (`:82-85`). Darum prüft `SaveSettings` (`SettingsController.cs:42-51`) **vor**
  der Schleife: Ist `QrErstesFeld` dabei und weicht er (getrimmt, ohne Groß/Klein) von `Artikelnummer`/`Matchcode` ab,
  kommt `TempData["WarningMessage"] = "QrErstesFeld: erlaubt sind „Artikelnummer“ oder „Matchcode“ – nichts gespeichert."`
  und ein Redirect, und es wird nichts gespeichert. Sonst wird die kanonische Schreibweise gespeichert. Das folgt der
  Regel „melden statt still behandeln“. Ohne die Prüfung fiele ein Tippfehler still auf `Artikelnummer` zurück.
- **Lesen an einer Stelle:** `_Layout.cshtml:15-28`, im selben `@{ }`-Block wie der Hardware-Scanner. Bei exakt
  `Matchcode` (getrimmt, OrdinalIgnoreCase) gilt `qrErstesFeld = "Matchcode"`, sonst `null`.
  `<body … data-qr-erstes-feld="@qrErstesFeld">`. Razor lässt ein `null`-Attribut weg (Kommentar `:18`). Der Wrapper
  liest `document.body.getAttribute('data-qr-erstes-feld') === 'Matchcode'`. Es gibt keinen Controller-ViewBag, also
  auch keine 8 weiteren Stellen wie bei `QrMitFaNummer` (`StockMovementsController.cs:97/118/168/258/280/299/367/384`).
- Abweichung von Rückfrage 1 (falls Standorteinstellungen): Der Eintrag kommt dann in `StandortSettingsCatalog.Fields`
  (`Models/Standort/StandortSettingsCatalog.cs:40-95`) statt in `Settings/Index.cshtml:45`. Es gibt dort nur die Typen
  String/Int/Bool (`:3`), also dieselbe Wertprüfung im `StandortEinstellungenController`. Lesen und Seed bleiben gleich.

### 5. Fundstellen `processScannedValue` (vollständig, `barcode-scanner.js`)

| Zeile | Rolle |
|---|---|
| `:10-21` `initScanner` | liest `data-qr-fa-enabled`/`data-fa-target` am Knopf (`:14-15`), Klick → `openScannerModal` (`:18`), Eintrag in `_hwScanTargets` (`:20`) |
| `:56-155` `openScannerModal` | reicht `qrFaEnabled`/`faTargetId` an Kamera (`:153`) und Bild-Upload (`:146`) |
| `:207` | Kamera-Treffer → `processScannedValue` |
| `:258` | Bild-Upload-Treffer → `processScannedValue` |
| `:284-363` innere Auswertung | QR-Split `:290`, FA-Scan Feld 3 `:292-299`, **Artikel = Feld 1** `:300-302`, FA aus Feld 3 nur bei `qrFaEnabled` `:305-307`; Ziel INPUT `:315-323`, Select2 → `/api/articles/by-number` `:326-345`, natives `<select>` `:348-362` |
| `:406-422` `initTextInputScanner` | **immer** `qrFaEnabled = false`, `faTargetId = null` (`:418`, `:421`) |
| `:427-443` Wrapper | globale `processScannedValue`, `onScanned`-Callback nach dem Füllen → **hier kommt der Matchcode-Zweig hin** |
| `:473-497` Listener `wms:hardware-scan` | Kamera-Fenster als Ziel `:481`, Fokus/einziges Ziel `:495`, beide über die globale Funktion |
| `:500-505` Standard-Inits | `btnScanArticle`→`ArticleId`, `btnScanStorageLocation`, `btnScanSourceLocation`, `btnScanArticleInfo`→`articleNumber` |

`hardware-scanner.js` ruft `processScannedValue` nicht selbst auf, sondern sendet nur das Ereignis (`:258-281`). Regel
(b) „freies Feld“ (`:264-267`) und Regel (d) „einziges `[data-hw-scan-ziel]`“ (`:270-276`) füllen Felder **ohne**
`processScannedValue`. Davon betroffen ist nur das BDE-Terminal (`BdeTerminal/Index.cshtml:47`, `:78`), und es hat
keinen Artikel-QR.

**Serverseitige QR-Auswertung:** Es gibt keine. Grep auf `Split(';')`/`split(';')` im Web-Projekt: QR-Split nur in
`barcode-scanner.js:290`. Die C#-Treffer (`BriefkopfVorlageService.cs:229`, `ColumnFilterSyntax.cs:247`) haben mit QR
nichts zu tun. `ArticlesController.Info` bekommt vom Client nur Feld 1 (`Info.cshtml:219-223`).

### 6. Seiten mit Artikel-QR (vollständig: alle Aufrufe von `initScanner`/`initTextInputScanner` mit `article`)

| # | Seite | Knopf → Ziel | Weiterverarbeitung | FA aus Feld 3 heute | Standort |
|---|---|---|---|---|---|
| 1 | Wareneingang | `Inbound.cshtml:26-27` → `ArticleId` (Select2, `:21`), Init `barcode-scanner.js:501` | `by-number` → Option wählen (`:330-339`) | ja, wenn `QrMitFaNummer` (ViewBag `StockMovementsController.cs:97`, `:118`) → `ProductionOrder` | beide |
| 2 | Warenausgang | `Outbound.cshtml:26-27` → `ArticleId` | wie 1 | ja, wenn `QrMitFaNummer` (`:280`, `:299`) | beide |
| 3 | Umbuchung | `Transfer.cshtml:26-27` → `ArticleId` | wie 1 | ja, wenn `QrMitFaNummer` (`:367`, `:384`) | beide |
| 4 | Sammel-Wareneingang | `InboundBulk.cshtml:96`, Init `:293` → `bulkScanBuffer` (hidden, `:106`) | Callback `onArticleScanned` → `by-number` → Zeile anlegen/hochzählen (`:236-270`) | **nie** | beide |
| 5 | Stückliste (Kommissionieren) | `Bom.cshtml:68`, Init `:722` (nur `!ReadOnly`, `:720`) → `bomScanArticle` (hidden, `:75`) | `change` → Zeile per `data-article-number` = **Ressourcenummer** (`:197`, `:291`, `:329`, `:348`, `:352`; Lookup **`:768`** nachgemessen) → Häkchen; sonst Modal `bomScanNotFoundModal` (`:787-792`) | **nie** (Knopf ohne Attribut) | beide |
| 6 | Artikelinfo | `Info.cshtml:24`, Init `barcode-scanner.js:504` → `articleNumber` (Text) | Auto-Submit (`Info.cshtml:219-223`) → `ArticlesController.Info` `:219-231`: Artikelnummer, sonst Matchcode exakt (`:226`) | **nie** | beide |
| 7 | Bestandsübersicht | `StockOverview/Index.cshtml:17-18`, Init `:159` → `filterArticle` (Text, `:16`) | **kein** Auto-Submit (nur Lagerplatz, `:161-163`); Filter Teilstring auf Artikelnummer/Bezeichnung/Matchcode (`StockMovementRepository.cs:27-33`) | **immer** (hart `data-qr-fa-enabled="true"`, `:18`) → `filterProductionOrder` | beide |
| 8 | FA-Liste | `ProductionOrders/Index.cshtml:39`, Init `:294` → `filterArticleNumber` | Submit; Filter Teilstring auf Kopf-Artikelnummer des FA (`ProductionOrderRepository.cs:113-114`) | **nie** | beide |
| 9 | Kommissionier-Leitstand | `PickingLeitstand/Index.cshtml:39`, Init `:493` → `filterArticleNumber` | wie 8 (`PickingLeitstandController.cs:197`, `:276`) | **nie** | beide |
| 10 | OSEON-Teileverfolgung | `Tracking/OseonIndex.cshtml:26`, Init `:312` → `filterArticle` | Submit (`:313-317`); Teilstring auf OSEON-Artikelnummer (`OseonProductionOrderRepository.cs:74-79`) | **nie** | nur AKE (OSEON) |

Kein Artikel-QR (nur Lagerplatz/FA, unberührt): `OutboundAll.cshtml:26`, `LocationTransfer.cshtml:128-129`,
`StockMovements/Index.cshtml:162` (FA), FA-Knöpfe in 8/9/10, `StockOverview` `btnScanPO`/`btnScanLocationFilter`,
`Bom.cshtml:721` (Ziel-Lagerplatz), BDE-Terminal.

**Wirkung von `Matchcode` je Seite (nach der Übersetzung):** 1–6 bekommen die exakte Artikelnummer und verhalten sich
wie mit einem Artikelnummer-Etikett. 7–10 setzen die Artikelnummer als **Teilstring-Filter**. Das ist unverändert und
kann wie heute weitere Artikel treffen, deren Nummer sie enthält. Auf Seite 7 übersteuert eine übernommene FA den
Artikelfilter (`StockOverviewController.cs:48-54`: mit FA wird `filterArticle` nicht ausgewertet). Das ist alt und wird
hier nur benannt. An Seite 6 übersetzt der Server im Modus `Artikelnummer` schon heute einen Matchcode (`:226`).

### 7. Wo `QrMitFaNummer` heute nicht wirkt

- **Wirkt nur** auf den Seiten 1–3 (Ein-/Aus-/Umbuchung, `data-qr-fa-enabled` aus ViewBag, je `:27`).
- **Seite 7 Bestandsübersicht:** hart `true` (`StockOverview/Index.cshtml:18`). Die FA wird immer übernommen, die
  Einstellung zählt nicht (Rückfrage 4).
- **Seite 4 Sammel-Wareneingang:** Der Controller setzt `ViewBag.QrMitFaNummer` (`StockMovementsController.cs:168`,
  `:258`), aber die View liest ihn nicht. `initTextInputScanner` setzt fest `false` (`barcode-scanner.js:418`, `:421`).
- **Seiten 5, 6, 8, 9, 10:** nie. Die Knöpfe tragen kein Attribut (Stückliste, Artikelinfo), oder es greift
  `initTextInputScanner` mit festem `false`.
- **Folge für die vier Fälle:** Die Fälle „+FA“ sind nur auf den Seiten 1–3 (und 7, dort immer) sichtbar. Auf allen
  anderen Seiten fallen „Matchcode“ und „Matchcode+FA“ zusammen. Das ist gewollt, die Spec ändert daran nichts.

## Migrations-/SQL-Auswirkungen

**Keine Migration, kein `SQL/XX`, `SQL/00_FreshInstall.sql` und `SQL/AgentJobs/` unberührt.**

- Es ändert sich kein Schema. `Articles.Matchcode` gibt es seit Migration 91. `AppSettings` ist Key/Value
  (`Models/AppSetting.cs:5-20`), ein neuer Key ist eine Datenzeile.
- Die Zeile legt der **Laufzeit-Seed** in `Program.cs` an. Vorbild ist v1.67.0 (Vault/enaio-Schalter: nur Laufzeit-Seed,
  kein SQL, kein FreshInstall-Eintrag; Grep `VaultLinksAktiv` trifft kein `SQL/`). Eine Frischinstallation bekommt
  die Zeile beim ersten Start. Fehlt sie trotzdem, gilt `Artikelnummer` (Anforderung 1). `QrMitFaNummer` hatte
  historisch zusätzlich `SQL/28` und eine FreshInstall-Zeile (`00_FreshInstall.sql:954`). Das braucht es hier nicht,
  weil der Seed die gleiche Aufgabe idempotent erfüllt.
- Neuer Index auf `Matchcode`: nein (ponytail-Notiz oben).
- Der Dev-Lauf bestätigt: `dotnet ef migrations has-pending-model-changes` ist leer.

## Audit-Feld-Auswirkungen

Keine. `AppSetting` erbt **nicht** `AuditableEntity` (`Models/AppSetting.cs:5`), und `SetValueAsync` setzt nur `Value`
(`AppSettingRepository.cs:32-47`). Das ist unverändert und für alle AppSettings so. Endpunkt und Übersetzung lesen nur.
Wird Rückfrage 3 mit Ja beantwortet, ändert sich ebenfalls nur ein Leseweg.

## Rollen / Zugriffsfilter

- **Keine neue Rolle, kein neuer Filter.** `controller.md` und `RoleOverview.cshtml` bleiben unverändert.
- Neuer Endpunkt in `ArticlesApiController`. Die Klasse hat **keinen** Rollen-Filter (`ArticlesApiController.cs:8-10`),
  wie `by-number`. Geschützt ist sie nur durch den Login-Redirect in `Program.cs:562-586`. Das ist hier richtig: Die
  Scan-Seiten gehören zu verschiedenen Rollen (`stock`/`stock_read`, `picking`, `leitstand`, `tracking`,
  Stammdaten-Lesen, `controller.md:59-69`), und ein engerer Filter würde einzelne Seiten brechen. Freigelegt werden
  Artikelnummer und Bezeichnung, die über `by-number`/`search` schon erreichbar sind.
- `/Settings`: `[RequireAdminAccess]` (`SettingsController.cs:10`). Bei Rückfrage 1 = Standorteinstellungen ebenfalls
  nur Admin.
- **Listen-View-Pattern:** keine neue Tabellen-View, keine Spalte, keine Pagination betroffen.

## Akzeptanzkriterien

Testdaten: **E1** = `G-KT-FRR-510-2010--1760B-250-V01;;1065141;09` (Matchcode von 50002134, durch P2 bestätigt),
**E2** = `102044;;1065141;09` (Artikelnummer).

**Einstellung**

1. Nach dem Deploy steht unter `/Settings` → „Allgemein“ `QrErstesFeld` mit dem Wert `Artikelnummer`, neben
   `QrMitFaNummer`. Das gilt bei Antwort 1 = /Settings, sonst entsprechend in den Standorteinstellungen.
2. Speichern mit `matchcode` (Kleinschreibung) speichert `Matchcode`. Speichern mit `Matchcod` zeigt die gelbe Meldung
   „QrErstesFeld: erlaubt sind …“ und speichert **keinen** Wert der Maske.
3. Bei `Artikelnummer` trägt `<body>` kein Attribut `data-qr-erstes-feld`, bei `Matchcode` trägt es
   `data-qr-erstes-feld="Matchcode"`.

**Fall A: Artikelnummer, FA aus (heutiges Verhalten, Regression)**

4. Wareneingang, E2 scannen: Artikel 102044 ist gewählt, das FA-Feld ist leer. Im Netzwerk-Tab gibt es **keinen**
   Aufruf von `by-matchcode`.
5. Wareneingang, E1 scannen: „Nicht gefunden: G-KT-FRR-510-2010--1760B-250-V01“ wie heute.

**Fall B: Artikelnummer, FA an**

6. Wareneingang, E2: Artikel 102044 und FA `1065141`.

**Fall C: Matchcode, FA aus**

7. Wareneingang, E1: Artikel 50002134 ist gewählt, das FA-Feld ist leer.
8. Wareneingang, E2: Meldung „Matchcode „102044“ nicht gefunden“. Es ist kein Artikel gewählt, und das FA-Feld ist
   unverändert. Ausnahme: P2 zeigt, dass `102044` Matchcode eines Artikels ist.

**Fall D: Matchcode, FA an**

9. Wareneingang, Warenausgang und Umbuchung, je E1: Artikel 50002134 und FA `1065141`.

**Alle Seiten im Modus Matchcode (E1)**

10. Sammel-Wareneingang: Es entsteht eine Zeile 50002134 mit Menge 1. Ein zweiter Scan erhöht auf 2.
11. Stückliste eines FA, der 50002134 als Ressourcenummer enthält: Die Zeile wird abgehakt und hervorgehoben. Bei einem
    FA ohne den Artikel erscheint das bestehende Modal „nicht in der Kommissionierliste“ mit `50002134`.
12. Artikelinfo zeigt Artikel 50002134.
13. In der Bestandsübersicht steht im Artikelfilter `50002134`, im FA-Filter `1065141`.
14. In FA-Liste und Leitstand steht im Filter „Artikelnummer“ `50002134`, das Formular wird abgeschickt.
15. OSEON (nur AKE): Im Filter „Artikelnummer“ steht `50002134`, das Formular wird abgeschickt.
16. Jeder Fall wirkt gleich über Kamera, Bild-Upload und Hardware-Scanner (Tastatur-Emulation **und** Fokus-Eingabe am
    Netum).

**Negativfälle (Modus Matchcode)**

17. **Kein Treffer:** `G-KT-GIBT-ES-NICHT;;1065141;09` ergibt „Matchcode „G-KT-GIBT-ES-NICHT“ nicht gefunden“. Kein Feld
    wird befüllt, auch kein FA-Feld, und auf den Filterseiten wird nicht abgeschickt.
18. **Mehrdeutig:** Wenn zwei Artikel den Matchcode von E1 tragen, erscheint „… ist mehrdeutig (2 Artikel: …)“ mit
    beiden Nummern. Es wird nichts gewählt und nichts abgeschickt.
19. **Groß/Klein:** `g-kt-frr-510-2010--1760b-250-v01;;1065141;09` ergibt 50002134.
20. **Leerzeichen:** `  G-KT-FRR-510-2010--1760B-250-V01  ;;1065141;09` ergibt 50002134. Ein in der DB mit
    abschließendem Leerzeichen gepflegter Matchcode wird ebenfalls gefunden.
21. **Leeres Feld 1:** `;;1065141;09` löst keine `by-matchcode`-Abfrage aus und verhält sich wie im Modus
    Artikelnummer.
22. **Lagerplatz- und FA-Scans** sind im Modus Matchcode unverändert. Der Ziel-Lagerplatz in der Stückliste und
    „FA Nummer“ in der FA-Liste mit E1 ergeben `1065141`.
23. **Server nicht erreichbar** (z. B. Netzwerk im Browser trennen): Es erscheint „konnte nicht geprüft werden“, nicht
    „nicht gefunden“.
24. Die Meldungen sind lesbar (Text mit ✗, Kontrast ≥ 4,5:1) und stehen am Ziel oder am Scan-Knopf, auch bei
    versteckten Zielen (Stückliste, Sammel-Wareneingang).

**Technik**

25. Repository-Test (InMemory): exakter Treffer; Groß/Klein; Leerzeichen auf beiden Seiten; Teilstring trifft **nicht**;
    zwei Treffer ergeben 2; DUMMY ausgeschlossen; `limit` greift.
26. Controller-Test: leer ergibt 400; 0/1/6 Treffer ergeben `articles.Count` 0/1/5 und `more` false/false/true.
27. Quelltext-Wächter: Der Matchcode-Zweig steht **nur** im globalen Wrapper, die innere Funktion enthält kein
    `by-matchcode`. `HardwareScannerTests` bleibt unverändert grün.
28. Build und alle Tests sind grün. `has-pending-model-changes` ist leer.

## Test-Szenarien

Neues Kapitel **TS-101 „QR-Code: erstes Feld Matchcode“** in `docs/TESTSZENARIEN.md` (Nummer beim /dev), Index in
`secondbrain/tests/testszenarien-index.md`. Regressionsbezug: QR mit FA (`docs/TESTSZENARIEN.md:492` ff.), TS-99
(Hardware-Scanner).

**Vorbedingungen:** Testsystem IDEAL (und AKE für OSEON), Prüfabfragen P1–P5 gelaufen, Artikel 50002134 mit Bestand
auf einem Lagerplatz, ein FA mit 50002134 als Komponente (für die Stückliste). E1/E2 als gedruckte Etiketten oder
QR-Bilder. Für Groß/Klein, Leerzeichen und „kein Treffer“ QR-Bilder mit einem beliebigen QR-Generator erzeugen und über
„Bild hochladen“ scannen. Ersatzweise in der Browser-Konsole
`processScannedValue('<Text>', 'ArticleId', 'article', true, 'ProductionOrder')`.

| Nr | Einstellung | Schritte | Erwartung |
|---|---|---|---|
| 101.1 | Artikelnummer / FA aus | Wareneingang: E2, dann E1 | AK 4, 5 |
| 101.2 | Artikelnummer / FA an | Wareneingang: E2 | AK 6 |
| 101.3 | Matchcode / FA aus | Wareneingang: E1, dann E2 | AK 7, 8 |
| 101.4 | Matchcode / FA an | Ein-, Ausgang, Umbuchung: E1 | AK 9 |
| 101.5 | Matchcode | Sammel-Wareneingang: E1 zweimal | AK 10 |
| 101.6 | Matchcode | Stückliste mit/ohne 50002134: E1 | AK 11 |
| 101.7 | Matchcode | Artikelinfo, Bestandsübersicht, FA-Liste, Leitstand, OSEON (AKE): E1 | AK 12–15 |
| 101.8 | Matchcode | 101.3 mit Kamera, Bild-Upload, Hardware-Scanner (Tastatur und Netum Fokus-Eingabe) | AK 16 |
| 101.9 | Matchcode | kein Treffer, Groß/Klein, Leerzeichen, leeres Feld 1 | AK 17, 19, 20, 21 |
| 101.10 | Matchcode | Mehrdeutig: an einem zweiten Artikel unter „Artikel bearbeiten“ den Matchcode von E1 eintragen, E1 scannen; danach zurücksetzen | AK 18. **Hinweis:** Der nächste Artikel-Sync überschreibt den Hand-Wert, wenn Sage den Artikel liefert (`SageImportService.cs:471`, `:482`), also zügig testen. |
| 101.11 | Matchcode | Lagerplatz- und FA-Scans mit E1 | AK 22 |
| 101.12 | Matchcode | Netzwerk trennen, E1 | AK 23 |
| 101.13 | — | `/Settings`: `matchcode`, dann `Matchcod` speichern | AK 2 |

## Etappen (nur bei epic: true)

Entfällt (`epic: false`). Die Spec ist ein Einzelschritt im Bündel-Worktree.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt | – | – |

## Deploy

> [!note] Final (qa-agent 2026-10-27) aus dem echten Diff `a9462036..ca48b22d`.

- **Web-App: ja.** Geändert unter `IdealAkeWms/`: `barcode-scanner.js`, `_Layout.cshtml`, `Articles/Info.cshtml`, `Settings/Index.cshtml`,
  `Help/Index.cshtml`, `Help/Changelog.cshtml`, `ArticlesApiController`, `ArticlesController`, `SettingsController`, `ArticleRepository`/
  `IArticleRepository`, `AppSettingKeys`, `Program.cs` (Laufzeit-Seed), `AppVersion.cs`.
- **Dienst: ja, nur Version.** Unter `IDEALAKEWMSService/` ändert sich ausschließlich `AppVersion.cs` (1.68.0). Kein Dienst-Code; der
  Dienst-Publish hält nur die angezeigte Versionsnummer gleich und ist beim Bündel-Deploy ohnehin fällig.
- **Migration: nein.** Keine Datei unter `*/Migrations/`, `has-pending-model-changes` leer, kein `SQL/`, kein `SQL/Einmalig/`. Die Zeile
  `QrErstesFeld` legt der Web-Start an (Seed). Kein DB-Backup aus diesem Grund nötig.
- **Publish-Befehle** (im Worktree `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5`):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- **Reihenfolge:** Web publizieren und starten (Seed legt `QrErstesFeld = Artikelnummer` an) → `/Settings` prüfen → Dienst (nur Version).
- **Ablauf:** Publish aus dem Worktree → Testsystem → Test (Checkliste am Ende) → Merge (Teil des Bündel-Merges, Schranke 2).
- **Nach dem Merge** nur dann erneut aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen kombiniert hat.
- **Nach dem Deploy je Standort:** `/Settings` → Allgemein → `QrErstesFeld` steht auf `Artikelnummer` (= bisheriges Verhalten). An IDEAL
  erst nach bestandenem TS-101 auf `Matchcode` stellen, AKE bleibt `Artikelnummer`.
- **Browser-Cache:** `barcode-scanner.js` mit `asp-append-version`, kein Hard-Reload nötig.

## Prüfabfragen für den Menschen (vor UAT; Ergebnis in die Spec)

Die Aussagen „E1 ist Matchcode von 50002134“, „Matchcodes sind eindeutig“ und „Matchcodes haben keine
Rand-Leerzeichen“ sind **unbelegt**, bis diese Abfragen gelaufen sind.

```sql
-- P1 (WMS-DB je Standort): Dubletten nach der Regel der Spec (getrimmt, ohne Gross/Klein)
SELECT UPPER(LTRIM(RTRIM(Matchcode))) AS MatchcodeNorm, COUNT(*) AS Anzahl,
       STRING_AGG(ArticleNumber, ', ') AS Artikel   -- SQL Server >= 2017
FROM dbo.Articles
WHERE Matchcode IS NOT NULL AND LTRIM(RTRIM(Matchcode)) <> ''
GROUP BY UPPER(LTRIM(RTRIM(Matchcode)))
HAVING COUNT(*) > 1
ORDER BY Anzahl DESC;

-- P2 (WMS-DB IDEAL): Testdaten
SELECT ArticleNumber, '[' + Matchcode + ']' AS MatchcodeMitRand, DATALENGTH(Matchcode) AS Bytes
FROM dbo.Articles
WHERE ArticleNumber IN ('50002134', '102044')
   OR UPPER(LTRIM(RTRIM(Matchcode))) IN ('G-KT-FRR-510-2010--1760B-250-V01', '102044');

-- P3 (WMS-DB): Kollation (nur Information, die Spec vergleicht kollationsunabhaengig)
SELECT DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS DbCollation,
       c.collation_name AS SpaltenCollation
FROM sys.columns c WHERE c.object_id = OBJECT_ID('dbo.Articles') AND c.name = 'Matchcode';

-- P4 (WMS-DB): Matchcodes mit Rand-Leerzeichen (DATALENGTH, weil '=' abschliessende Leerzeichen ignoriert)
SELECT COUNT(*) AS MitRandLeerzeichen
FROM dbo.Articles
WHERE Matchcode IS NOT NULL AND DATALENGTH(Matchcode) <> DATALENGTH(LTRIM(RTRIM(Matchcode)));

-- P5 (WMS-DB IDEAL): Positions-Matchcode der Struktur vs. Artikelstamm fuer den Testartikel
SELECT DISTINCT n.Artnr, n.Matchcode AS StrukturMatchcode, a.Matchcode AS ArtikelMatchcode
FROM dbo.FaHierarchyNodes n LEFT JOIN dbo.Articles a ON a.ArticleNumber = n.Artnr
WHERE n.Artnr = '50002134' OR n.Matchcode LIKE '%G-KT-FRR-510-2010--1760B-250-V01%';

-- P6 (Sage-DB des Standorts): Ist der Matchcode in Sage eindeutig?
-- a) Ohne Sync:ArticlesViewName (bisheriger Select, KHKArtikel). Mandant-Spalte ggf. ergaenzen, falls die DB mehrere fuehrt.
SELECT UPPER(LTRIM(RTRIM(Matchcode))) AS MatchcodeNorm, COUNT(*) AS Anzahl
FROM dbo.KHKArtikel
WHERE Matchcode IS NOT NULL AND LTRIM(RTRIM(Matchcode)) <> ''
GROUP BY UPPER(LTRIM(RTRIM(Matchcode))) HAVING COUNT(*) > 1;
-- b) Mit Sync:ArticlesViewName (IDEAL: Artikel-View, Name aus den Standorteinstellungen)
-- SELECT UPPER(LTRIM(RTRIM(Matchcode))), COUNT(*) FROM <ArtikelView> GROUP BY UPPER(LTRIM(RTRIM(Matchcode))) HAVING COUNT(*) > 1;
```

Lesart: P1 > 0 heißt, diese Etiketten bekommen im Betrieb „mehrdeutig“. Das ist das gewollte Verhalten, aber vor der
Umstellung auf `Matchcode` mit dem Lager klären. P6 > 0 heißt, Sage selbst garantiert keine Eindeutigkeit. P5 mit
Abweichung bedeutet das Risiko „Etikett trägt Positions-Matchcode“ (siehe Risiken).

**Ergebnis P1 (Gerald 2026-10-27, WMS-DB IDEAL):** keine doppelten Matchcodes. Die Meldung „mehrdeutig“ bleibt als Schutz, ist an IDEAL heute aber nicht zu erwarten.

## Risiken

- **Mehrdeutige Matchcodes** (nicht erzwungen, siehe P1/P6). Abgefangen durch die Meldung. Bei vielen Dubletten ist
  der Modus `Matchcode` im Alltag lästig, das ist eine fachliche Entscheidung vor der Umstellung.
- **Etikett trägt den Positions-Matchcode** der IDEAL-Struktur statt den des Artikelstamms (P5). Ergebnis ist
  „nicht gefunden“. Abhilfe wäre eine eigene Spec (Struktur als zweite Quelle), nicht hier.
- **Artikel fehlt in `Articles`** (gefilterte Projektion, [[fallstricke]] §12). Ergebnis ist „nicht gefunden“, wie
  heute bei Artikelnummer-Etiketten.
- **Gemischter Etikettenbestand:** Im Modus `Matchcode` scheitern Artikelnummer-Etiketten (AK 8). Rückfrage 2.
- **Reihenfolge schneller Folgescans** kann sich durch die asynchrone Abfrage überholen (nur Modus `Matchcode`). Am
  Sammel-Wareneingang zählt jeder Scan trotzdem. Die Reihenfolge der Zeilen kann abweichen, die Mengen nicht.
- **Zwei Requests je Scan** auf Select2-Seiten (Latenz am WLAN-Handheld). Die Mitigation ist oben als ponytail-Notiz
  beschrieben.
- **Freitext in `/Settings`:** abgefangen durch die Prüfung beim Speichern. Ein per SQL gesetzter Fremdwert fällt still
  auf `Artikelnummer` zurück (Anforderung 1). Das ist bewusst die sichere Seite.

## Hilfe, Doku, Brain (Pflicht im Dev-Lauf)

- **Hilfe `Views/Help/Index.cshtml`:**
  - Karte „Barcode / QR-Code Scanner“ (`:82-138`), neuer Abschnitt `<h6>QR-Code: erstes Feld (seit v1.68.0)</h6>` mit:
    - der Vier-Fälle-Tabelle aus „Ziel / Nutzen“,
    - der Regel „exakt, ohne Groß/Klein, Leerzeichen am Rand egal“,
    - den drei Meldungstexten wörtlich und was dann zu tun ist („Artikel von Hand wählen“, „Artikelstamm prüfen“),
    - dem Hinweis, dass im Modus Matchcode Artikelnummer-Etiketten **nicht** mehr gehen,
    - der Liste, wo die FA aus Feld 3 übernommen wird (Ein-/Aus-/Umbuchung bei `QrMitFaNummer`, Bestandsübersicht
      immer, sonst nie),
    - „Einschalten (Admin): `/Settings` → Allgemein → `QrErstesFeld`“.
  - Artikelinfo (`:171-173`): „Das erste Feld ist je nach Einstellung `QrErstesFeld` die Artikelnummer oder der
    Matchcode“.
  - Bestandsübersicht (`:1683-1684`): derselbe Satz. Dazu der Hinweis, dass die übernommene FA den Artikelfilter
    übersteuert (belegt, `StockOverviewController.cs:48-54`).
- **`Info.cshtml:31`:** Hinweistext „QR-Code-Format: erstes Feld = Artikelnummer bzw. Matchcode (Einstellung), weitere
  Felder durch ;“.
- **README:** AppSettings-Tabelle `:389` neue Zeile `QrErstesFeld | Artikelnummer | …`; Abschnitt `:246` ergänzen.
- **Changelog:** `Views/Help/Changelog.cshtml` und `secondbrain/changelog/2026-10-27-v1-68-0-qr-matchcode.md`.
- **Brain:**
  - [[feature-map]] aktualisieren.
  - [[fallstricke]] §3 neuer Eintrag „Matchcode ist nicht eindeutig: nie `FirstOrDefault`, Übersetzung nur über
    `by-matchcode`; asynchrone Scan-Übersetzung gehört in den Wrapper, sonst feuert `onScanned` vorher“ (mit Warum).
  - `codebase/controller.md` Zeile `ArticlesApiController` um den Endpunkt ergänzen.
  - Testindex nachziehen.

## Offene Rueckfragen

1. **Pflege global (`/Settings`) oder je Standort (Standorteinstellungen)?**
   **Empfehlung: `/Settings`, Gruppe „Allgemein“, direkt neben `QrMitFaNummer`.** Gründe: Beide Werte ergeben zusammen
   die vier Fälle und gehören in eine Maske. Getrennt wäre die Kombination für den Admin schwer zu durchschauen.
   „Global“ heißt dabei nicht „für beide Standorte gleich“: Auch die Standorteinstellungen schreiben in dieselbe Tabelle
   `AppSettings` (`StandortSettingsCatalog.cs:17`, `:43` `StandortBackend.App`), und jeder Standort hat dort seine eigenen
   Werte (Firmenname, Hardware-Scanner). Der Unterschied ist nur die Maske. Die Standorteinstellungen kennen zudem nur
   String/Int/Bool (`:3`), also kein Auswahlfeld. Gegenargument: Der Hardware-Scanner, mit dem das zusammenhängt, liegt in
   den Standorteinstellungen. Dann wandert `QrMitFaNummer` aber besser mit, und das ist außerhalb des Umfangs.
2. **Zusätzlich Modus „Automatisch“ (erst Artikelnummer, sonst Matchcode)?**
   **Empfehlung: nein für den Start.** Gründe:
   - Er verwischt, welches Etikett gemeint ist. Ein Feld 1, das zugleich Artikelnummer eines Artikels und Matchcode
     eines anderen ist, nähme still die Artikelnummer.
   - Er kostet bei jedem Matchcode-Etikett eine zusätzliche, scheiternde `by-number`-Abfrage.
   - Die Artikelinfo arbeitet heute schon so (`ArticlesController.cs:224-226`). Für die Suche per Hand reicht das.
   **Ja**, wenn an einem Standort beide Etikettenarten dauerhaft nebeneinander im Umlauf sind. Hannes' Testetiketten E1
   und E2 tragen beide FA 1065141, das deutet darauf hin. Bitte bestätigen. Bei Ja wird es ein dritter Wert
   `Automatisch` derselben Einstellung, keine neue Einstellung. Der Wrapper fragt dann zuerst `by-number`, danach
   `by-matchcode`. Mehrdeutig und kein Treffer verhalten sich wie im Modus Matchcode.
3. **Artikelinfo: stiller Matchcode-Fallback mitbeheben?** `ArticlesController.Info` sucht bei fehlender Artikelnummer
   per `GetByMatchcodeExactAsync`, und das ist `FirstOrDefault` (`ArticleRepository.cs:91-92`, Aufruf
   `ArticlesController.cs:226`). Bei zwei Artikeln mit gleichem Matchcode zeigt die Seite still einen davon. Das
   widerspricht der Regel dieser Spec („keine stille Wahl“).
   **Empfehlung: ja, mitbeheben.** Gleiche Methode `GetByMatchcodeAsync(…, 6)`; bei ≥ 2 Treffern
   `ViewBag.NotFound`-artige Meldung „Matchcode … ist mehrdeutig (…)“ statt der Artikelseite. Das sind wenige Zeilen,
   der Test `ArticlesControllerTests.cs:235-258` wird angepasst. `GetByMatchcodeExactAsync` fällt dann weg.
4. **Bestandsübersicht übernimmt die FA immer** (`StockOverview/Index.cshtml:18`, hart `true`), unabhängig von
   `QrMitFaNummer`. Zusammen mit `StockOverviewController.cs:48-54` heißt das: Ein QR mit FA filtert dort nach der FA,
   nicht nach dem Artikel.
   **Empfehlung: so lassen, eigener Backlog-Punkt.** Das Verhalten ist alt und wird seitenspezifisch genutzt
   (FA-Bestand). Eine Änderung hätte eine eigene fachliche Wirkung und gehört nicht in diese Spec.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Pflege global (/Settings, Gruppe Allgemein) oder Standorteinstellungen? (Empfehlung /Settings) → **Wie empfohlen: /Settings neben QrMitFaNummer.** (Gerald 2026-10-27)
2. Zusätzlicher Modus „Automatisch“? (Empfehlung nein; ja bei dauerhaft gemischtem Etikettenbestand) → **Nein, vorerst nicht.** (Gerald 2026-10-27)
3. Stillen Matchcode-Fallback der Artikelinfo mitbeheben? (Empfehlung ja) → **Ja.** (Gerald 2026-10-27)
4. Bestandsübersicht: FA-Übernahme hart „an“ so lassen? (Empfehlung ja, eigener Backlog-Punkt) → **Ja, so lassen; eigener Backlog-Punkt.** (Gerald 2026-10-27)

## Umsetzung (Dev-Lauf 2026-10-27, v1.68.0)

Basis `a9462036` (v1.67.2), Commit `ca48b22d` (wip feature-complete). Nur Web (Dienst nur `AppVersion.cs`), keine Migration
(`dotnet ef migrations has-pending-model-changes`: „No changes have been made to the model since the last migration.“). TS-101.

**Einstellung:** `AppSettingKeys.QrErstesFeld`; Laufzeit-Seed `("QrErstesFeld", "Artikelnummer", …)` in `Program.cs` neben
`QrMitFaNummer`; `/Settings` Gruppe „Allgemein“ `QrMitFaNummer`, `QrErstesFeld`. `SettingsController.SaveSettings` prüft **vor** der
Schleife: getrimmt/ohne Groß-Klein `Artikelnummer`|`Matchcode` → kanonisch gespeichert, sonst `WarningMessage` „QrErstesFeld: erlaubt sind
„Artikelnummer“ oder „Matchcode“ – nichts gespeichert.“ + Redirect, kein Wert der Maske gespeichert (auch leer = ungültig).
`_Layout.cshtml`: `data-qr-erstes-feld="Matchcode"` nur bei exakt Matchcode, sonst kein Attribut.

**Eine Suche (Freigabe-Nachtrag 2026-10-27):** `GetByMatchcodeExactAsync(string matchcode, int limit)` → `List<Article>` statt
`Article?` (`FirstOrDefault`): `Trim().ToUpper()` auf beiden Seiten, DUMMY ausgeschlossen, sortiert nach Artikelnummer, `Take(limit)`.
Kein neues `GetByMatchcodeAsync`. Genutzt von:
- `GET /api/articles/by-matchcode?matchcode=` (`ArticlesApiController.GetByMatchcode`): leer → 400, sonst
  `{ articles: [{ id, articleNumber, text }] (max 5), more }` mit Limit 6; `text` wie `by-number`.
- `ArticlesController.Info` (Antwort 3): ≥ 2 Treffer → `ViewBag.Mehrdeutig` „Matchcode „…“ ist mehrdeutig (n Artikel: …) – bitte die
  Artikelnummer eingeben.“ (Warn-Alert in `Info.cshtml`), 1 Treffer → Artikel, 0 → „nicht gefunden“ wie bisher. Hinweistext QR-Format
  angepasst.

**Client:** Der globale Wrapper in `barcode-scanner.js` ist jetzt zweistufig: `_processScannedValueSync` = der bisherige Wrapper-Rumpf
(unverändert, inkl. `nichtGefunden` aus v1.67.2), davor `processScannedValue` mit dem Matchcode-Zweig. Nur bei `scanType === 'article'`,
Attribut `Matchcode` und nicht leerem Feld 1 eine Abfrage; sonst synchron derselbe Ablauf wie v1.67.2. Treffer → Feld 1 ersetzt, Rest ab dem
ersten `;` unverändert, dann `_processScannedValueSync`. Kein Treffer / mehrdeutig (`articles > 1` oder `more`) / Fehler → Meldung am Ziel
(`showScanFeedback` mit optionalem Text, 10 s; Select2 an `.input-group`, verstecktes Ziel am Scan-Knopf aus `_hwScanTargets`), dann
`nichtGefunden()` (v1.67.2-Vertrag: „Scannen ohne Feld“ zeigt zusätzlich „Code … nicht zuordenbar“). Nichts befüllt, keine FA, kein
`onScanned` (kein Submit). Der passende `_textInputScanCallback` wird beim Scan reserviert und nur bei Treffer wieder gesetzt.
Damit gilt der Zweig für Kamera, Bild-Upload, Hardware-Scanner (Tasten und Text, Kamera-Fenster und Regel a/d) und das Scan-Feld ohne Fokus.

**Tests:**
- `ArticleRepositoryTests`: drei Bestandstests auf die neue Signatur (`List`, `BeEmpty` statt `BeNull`), neu Trim+Groß/Klein (E1-Matchcode
  mit Rand-Leerzeichen) sowie mehrdeutig sortiert / DUMMY aus / Limit / kein Fallback auf Artikelnummer `102044`.
- `ArticlesControllerTests`: drei Bestandstests auf die Liste umgestellt, neu `Info_MatchcodeMehrdeutig_MeldungStattStillErsterTreffer`.
- `ArticlesApiControllerTests`: leer/Whitespace → 400; 0/1/6 Treffer → 0/1/5 und `more` false/false/true; Textformat.
- Neu `SettingsControllerQrErstesFeldTests`: `matchcode`/` ARTIKELNUMMER ` kanonisch; `Matchcod`/leer → nichts gespeichert, Meldung.
- `HardwareScannerTests.QrErstesFeld_MatchcodeZweigNurImGlobalenWrapper_ModusArtikelnummerSynchron` (AK 27: eine Abfrage-Stelle, innere
  Funktion ohne `by-matchcode`, synchroner Zweig wörtlich, Layout/Seed/Settings-Gruppe).
- Wrapper per Wegwerf-Mock in Node durchgespielt (Artikelnummer synchron ohne Abfrage, Lagerplatz unberührt, leeres Feld 1, E1 mit Rand-
  Leerzeichen → `50002134;;1065141;09` + FA-Ziel + `nichtGefunden` durchgereicht, drei Meldungstexte, Callback reserviert).

**Abweichungen:**
- **Geänderte Bestandstests:** die sechs `GetByMatchcodeExactAsync`-Tests (Signatur, vom Nachtrag verlangt) und
  `HardwareScannerTests.Layout_LaedtSkriptNurBeiSchalterAn_VorDenSeitenSkripten`: prüfte `<body …data-hw-scan-abstand="@hwScanAbstand">` mit
  schließendem `>`; jetzt ohne `>`, weil `data-qr-erstes-feld` dahinter steht. Inhaltlich unverändert.
- `affected_code` nennt noch `GetByMatchcodeAsync`; laut Nachtrag ersetzt durch die erweiterte `GetByMatchcodeExactAsync`.
- Mehrdeutig-Meldung: bei mehr als 5 Treffern „mehr als 5 Artikel: …, …“ (die genaue Zahl liefert der Endpunkt mit Limit 6 nicht).
- Sammel-Wareneingang: Meldung am Scan-Knopf, nicht in `#bulkScanFeedback` (Spec: „wenn trivial erreichbar“ - es bräuchte einen
  Seiten-Sonderfall im gemeinsamen Skript).
- Skill `frontend-design` erst **nach** den View-Änderungen aufgerufen (Pflicht: davor); nachträgliche Prüfung ohne Änderungsbedarf
  (bestehende Muster, `text-danger` + ✗, `alert-warning`).

**Nachweis:** `dotnet build IdealAkeWms.slnx` 0 Fehler, 28 Warnungen (unverändert). `dotnet test`: Web **2195 bestanden, 1 übersprungen**,
Dienst **328 bestanden**.

## QA-Nachweis v1.68.0 (qa-agent, 2026-10-27)

Geprüft: `git diff a9462036 ca48b22d` im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (22 Dateien, +394/−31). Im Worktree ist kein
`secondbrain/` sichtbar (sparse-checkout greift). Web und Dienst wurden nicht gestartet.

### 1. Build und Tests (selbst ausgeführt)

| Prüfung | Ergebnis |
|---|---|
| `dotnet build IdealAkeWms.slnx` | **0 Fehler**, 28 Warnungen (vorbestehend, z. B. CS8601 in `BdeTerminalAuftraegeTests.cs:23`) |
| `dotnet test` – `IdealAkeWms.Tests` | **2195 bestanden, 1 übersprungen, 0 fehlgeschlagen** (gesamt 2196) |
| `dotnet test` – `IDEALAKEWMSService.Tests` | **328 bestanden, 0 übersprungen, 0 fehlgeschlagen** |
| `dotnet ef migrations has-pending-model-changes --project IdealAkeWms` | „No changes have been made to the model since the last migration.“ |
| SQL-Übersetzung der Repository-Abfrage (Wegwerf-Test mit nie verbundenem SqlServer-Context, `ToQueryString()`, danach gelöscht, `git status` sauber) | `SELECT TOP(@p) … FROM [dbo].[Articles] AS [a] WHERE [a].[Matchcode] IS NOT NULL AND UPPER(LTRIM(RTRIM([a].[Matchcode]))) = @key AND [a].[ArticleNumber] <> N'DUMMY' ORDER BY [a].[ArticleNumber]`: übersetzbar, kein Client-Eval. Ausführung gegen echte Daten bleibt Manual-UAT (P1–P4). |

### 2. Code-Review (selbst, in diesem Lauf)

Gelesen: der ganze Diff, `barcode-scanner.js` vollständig (innere Auswertung, Wrapper, `_hwScanTargets`, Hardware-Listener,
`hwScanOhneFeld`), Auto-Submit der Artikelinfo (`Info.cshtml:219-223`), `InboundBulk.cshtml:293`, `Picking/Bom.cshtml:722`, Injektion in
`_Layout.cshtml`, Klassenkopf `ArticlesApiController`, alle Aufrufer von `GetByMatchcodeExactAsync` (nur Endpunkt und `ArticlesController.Info`,
kein Decorator).

**Geprüfte Punkte (ohne Befund):**
- **Modus Artikelnummer bitgleich zu v1.67.2:** `_processScannedValueSync` ist der wörtliche bisherige Wrapper-Rumpf. Der neue Wrapper ruft
  ihn bei leerem `matchcode` sofort synchron mit allen sechs Argumenten (inkl. `nichtGefunden`) auf (`barcode-scanner.js:453-459`). Ohne
  Attribut wird `value` nicht angefasst (Kurzschluss vor `String(value)`). Kein Request, Callback-Reihenfolge unverändert.
- **Matchcode-Zweig:** nur `scanType === 'article'` und Attribut exakt `Matchcode`. Lagerplatz/FA (`storageLocation`/`productionOrder`) laufen
  den synchronen Weg (AK 22). Leeres Feld 1 → synchron (AK 21). Treffer → nur Feld 1 ersetzt, Rest ab dem ersten `;` unverändert (`:474-476`).
  0 / mehrdeutig (`length > 1` oder `more`) / AJAX-Fehler → nur Meldung + `nichtGefunden()`: kein Füllen, kein `clearAndFillFaNumber`, kein
  `change`, kein `onScanned`. Abgelaufene Anmeldung (Login-Redirect liefert HTML) landet über den JSON-Parse-Fehler im `error`-Zweig =
  „konnte nicht geprüft werden“, nicht „nicht gefunden“.
- **`_textInputScanCallback`-Reservierung:** beim Scan genommen und genullt, nur bei Treffer zurückgesetzt und dann von
  `_processScannedValueSync` verbraucht (Sammel-WE `onArticleScanned`, Submit der Filterseiten). Die Artikelinfo hängt am `change`-Listener,
  der bei Fehlschlag nicht feuert → kein Submit.
- **`nichtGefunden` aus v1.67.2** wird in beiden Zweigen durchgereicht (`hwScanOhneFeld` → „Code … nicht zuordenbar“).
- **Repository:** `Trim().ToUpper()` beidseitig, DUMMY aus, `OrderBy` + `Take(limit)`. SQL-Server-Übersetzung oben belegt. Die alte Fassung
  (`FirstOrDefault`, ohne DUMMY-Ausschluss) ist vollständig ersetzt.
- **Endpunkt:** Query-String, leer/Whitespace → 400, Limit 6, max. 5 Einträge + `more`, `text` wie `by-number`. Kein Rollen-Filter, wie
  `by-number` (Klasse ohne Filter, `ArticlesApiController.cs:8-10`). Legt nichts Neues offen.
- **Artikelinfo mehrdeutig:** ≥ 2 Treffer → `ViewBag.Mehrdeutig`, Model null, kein `NotFound`. Ausgabe `@mehrdeutig` ist HTML-kodiert.
- **SaveSettings:** Prüfung vor der Schleife. Bei ungültigem Wert (auch leer) `WarningMessage` + Redirect ohne ein einziges `SetValueAsync`,
  bei gültigem die kanonische Schreibweise. Serverseitig, nicht nur im Browser.
- **Bestandsübersicht unverändert:** keine Datei unter `Views/StockOverview/` und kein `StockOverviewController` im Diff (Antwort 4).
- **Fehlerklassen-Check:** keine Werte in `<script>`-Blöcken (Einstellung als body-Attribut, Razor-kodiert, `null` lässt Razor weg); kein
  `hidden` + `d-*`; Meldungstexte laufen durch `escapeHtml`; kein Parallel-Array-Binding; keine stille Fallback-Zahl. Der Rückfall
  „unbekannter Wert → Artikelnummer“ im Layout ist Anforderung 1 und wird beim Speichern abgefangen.

**Befunde:**

| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| Q1 | niedrig | Der Mehrdeutig-Text steht zweimal (C# `ArticlesController.Info:232-233`, JS `barcode-scanner.js:480-481`) und ist unterschiedlich formuliert („Artikelnummer eingeben“ vs. „von Hand wählen“). Die Such-Regel selbst liegt an einer Stelle (Repository). | Akzeptiert: Die Handlung je Seite ist fachlich verschieden, möglich ist nur Textdrift. Kein Fix. |
| Q2 | niedrig | Bei > 5 Treffern steht „mehr als 5 Artikel“ statt der genauen Zahl (Spec: „<n> Artikel“). | Vom Dev dokumentiert, Limit 6 liefert die Zahl nicht. Akzeptiert. |
| Q3 | niedrig | Sammel-Wareneingang: Meldung am Scan-Knopf statt in `#bulkScanFeedback` (aria-live). | Die Spec erlaubt das („wenn trivial erreichbar“). Akzeptiert, in Checkliste Punkt 11 geprüft. |
| Q4 | kosmetisch | Die Testnamen `GetByMatchcodeExactAsync_SubstringOnly_ReturnsNull` / `_NoMatch_ReturnsNull` prüfen jetzt `BeEmpty`. | Nicht umbenannt (Churn), nur vermerkt. |
| Q5 | Prozess | Skill `frontend-design` erst nach den View-Änderungen aufgerufen. | Vom Dev gemeldet. Die Views nutzen nur vorhandene Muster (`alert-warning`, `text-danger` + ✗). Kein Fix. |
| Q6 | Doku | `affected_code` im Frontmatter nannte noch `GetByMatchcodeAsync`. | Von QA im Frontmatter korrigiert. |

Keine Befunde der Schwere mittel oder hoch. Kein Worktree-Commit nötig.

### 3. Beweisart je Akzeptanzkriterium

| AK | Beweis | Anmerkung |
|---|---|---|
| 1 | Manual-UAT | Seed-Text und Gruppe per Quelltext-Wächter (`HardwareScannerTests.QrErstesFeld_*`), die Anzeige nach echtem Start nur manuell |
| 2 | automatisiert (Logik) + Manual-UAT | `SettingsControllerQrErstesFeldTests` (4 Fälle). Das echte Formular-Binding `Dictionary<string,string>` ist nicht InMemory-beweisbar |
| 3 | Quelltext-Wächter + Manual-UAT | Razor-Rendering des `null`-Attributs nur im Browser |
| 4–16 | **Manual-UAT** | Browser-JavaScript, Select2, Kamera/Hardware-Scanner. Der Node-Mock des Dev ist kein bleibender Test |
| 17, 19–24 | **Manual-UAT** | Browser-JS. Der Server-Teil von 19/20 ist durch die Repository-Tests gedeckt |
| 18 | teilweise automatisiert | Artikelinfo-Zweig: `ArticlesControllerTests.Info_MatchcodeMehrdeutig_*`. Scan-Meldung Manual-UAT |
| 25 | automatisiert (InMemory) | `ArticleRepositoryTests.GetByMatchcodeExactAsync_*`. SQL-Übersetzung von QA belegt (oben), Echtdaten P1–P4 manuell |
| 26 | automatisiert | `ArticlesApiControllerTests.GetByMatchcode_*` (400; 0/1/6 → 0/1/5, more) |
| 27 | automatisiert | `HardwareScannerTests.QrErstesFeld_MatchcodeZweigNurImGlobalenWrapper_ModusArtikelnummerSynchron`, Bestands-Wächter `:93-96` grün |
| 28 | automatisiert | dieser Lauf |

Grün gezählt sind nur 25, 26, 27, 28 und die Server-Teile von 2 und 18. Alles andere ist bis zur Abnahme offen.

### 4. Geänderte Bestandstests

| Test | Klasse |
|---|---|
| `ArticleRepositoryTests.GetByMatchcodeExactAsync_ReturnsExactMatch`, `_SubstringOnly_ReturnsNull`, `_NoMatch_ReturnsNull` | Typ-/Signatur-Anpassung (Liste statt `Article?`, `BeEmpty` statt `BeNull`), Fixtures unverändert. Vom Freigabe-Nachtrag verlangt |
| `ArticlesControllerTests`: Info mit Matchcode-Treffer, mit Substring, unbekannt (`:236`, `:258`, `:271`) | Typ-/Signatur-Anpassung der Moq-Setups (`, 6`, Liste), Erwartungen unverändert |
| `HardwareScannerTests.Layout_LaedtSkriptNurBeiSchalterAn_VorDenSeitenSkripten` | Assertion um das schließende `>` gekürzt, weil `data-qr-erstes-feld` dahinter steht. Verhalten bewusst durch AK 3 geändert, das neue Attribut prüft der neue Wächter. Kein Befund |

Kein Test der Klasse „sonstige“.

### 5. CLAUDE.md-Checkliste

Migration/SQL/FreshInstall/AgentJobs: entfällt (kein Schema, nur Laufzeit-Seed wie v1.67.0). Audit: entfällt (`AppSetting` ohne
`AuditableEntity`, nur Lesewege). Version 1.68.0 in beiden `AppVersion.cs` + `Help/Changelog.cshtml`: ja. Brain-Changelog
[[2026-10-27-v1-68-0-qr-matchcode]]: vorhanden. [[feature-map]]: Abschnitt vorhanden. [[fallstricke]]: Eintrag „Matchcode ist nicht eindeutig;
asynchrone Scan-Übersetzung gehört in den Wrapper“ vorhanden. `codebase/controller.md`: Endpunkt ergänzt. README (Abschnitt + AppSettings-Tabelle)
und Hilfe (Scanner-Karte, Artikelinfo, Bestandsübersicht): ja. `docs/TESTSZENARIEN.md` TS-101.1–101.14 und `tests/testszenarien-index.md` Zeile 101:
ja. Keine neue Rolle. `SQL/Einmalig/`: nichts.

**Ergebnis:** Testbereit gesetzt (qa-agent 2026-10-27). Wartet auf Schranke 2 (Manual-UAT TS-101 + Bündel-Merge).

## Manuelle Test-Checkliste (Mensch, vor dem Merge)

Vorbedingung: Prüfabfragen P1–P5 gelaufen (P1 IDEAL erledigt, keine Dubletten). Web aus dem Worktree auf dem Testsystem IDEAL (für OSEON: AKE).

1. `/Settings` → Allgemein: `QrErstesFeld` = `Artikelnummer` steht neben `QrMitFaNummer` (AK 1).
2. Seitenquelltext: `<body>` ohne `data-qr-erstes-feld` (AK 3). Wareneingang E2 → 102044, FA leer, im Netzwerk-Tab kein `by-matchcode`. E1 →
   „Nicht gefunden: G-KT-FRR-…“ (AK 4, 5). Mit `QrMitFaNummer` an: E2 → 102044 + FA 1065141 (AK 6).
3. `/Settings`: `matchcode` speichern → steht als `Matchcode`. Dann `Matchcod` eingeben und zugleich einen anderen Wert ändern → gelbe
   Meldung, **beide** Werte unverändert (AK 2). `<body data-qr-erstes-feld="Matchcode">` (AK 3).
4. Matchcode, FA aus: Wareneingang E1 → 50002134, FA leer. E2 → „✗ Matchcode „102044“ nicht gefunden“, nichts gewählt (AK 7, 8).
5. Matchcode, FA an: Ein-, Ausgang, Umbuchung je E1 → 50002134 + FA 1065141 (AK 9).
6. Sammel-Wareneingang: E1 zweimal → eine Zeile 50002134, Menge 2 (AK 10).
7. Stückliste: FA mit 50002134 → Zeile abgehakt. FA ohne → Modal „nicht in der Kommissionierliste“ mit 50002134 (AK 11).
8. Artikelinfo E1 → 50002134. Bestandsübersicht E1 → Artikel 50002134, FA 1065141. FA-Liste, Leitstand, OSEON (AKE) → Filter 50002134 und
   abgeschickt (AK 12–15).
9. Punkt 4 über Kamera, Bild-Upload, Hardware-Scanner (Tastatur-Emulation und Netum Fokus-Eingabe) und „Scannen ohne Feld“ auf der
   Einbuchung. Bei kein Treffer dort zusätzlich „Code … nicht zuordenbar“ (AK 16).
10. `G-KT-GIBT-ES-NICHT;;1065141;09` → „nicht gefunden“, kein Feld befüllt (auch FA nicht), auf der FA-Liste **kein** Submit (AK 17).
    Kleinschrift und Rand-Leerzeichen → 50002134 (AK 19, 20). `;;1065141;09` → kein `by-matchcode` im Netzwerk-Tab (AK 21).
11. Mehrdeutig (zweiter Artikel mit dem Matchcode von E1, vor dem nächsten Sync, danach zurücksetzen): Scan → „… ist mehrdeutig (2 Artikel: …) –
    bitte den Artikel von Hand wählen“, ca. 10 s, nichts gewählt/abgeschickt. Artikelinfo mit eingetipptem Matchcode → Mehrdeutig-Meldung
    (AK 18). Auf Stückliste und Sammel-Wareneingang steht die Meldung lesbar am Scan-Knopf, mit ✗ (AK 24).
12. Lagerplatz-Scan und FA-Scan („FA Nummer“ der FA-Liste, Ziel-Lagerplatz der Stückliste) mit E1 im Modus Matchcode unverändert, FA 1065141 (AK 22).
13. Entwicklertools offline, E1 → „konnte nicht geprüft werden“, nicht „nicht gefunden“ (AK 23).
14. Danach an AKE `QrErstesFeld` = `Artikelnummer` lassen, an IDEAL erst nach bestandenem Test auf `Matchcode` stellen.
