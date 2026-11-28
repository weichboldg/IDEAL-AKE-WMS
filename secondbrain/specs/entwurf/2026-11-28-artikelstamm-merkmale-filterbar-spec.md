---
type: spec
title: Artikelliste – Merkmal-Spalten (inkl. Sage-Benutzerfelder) im Spaltenfilter filterbar
slug: 2026-11-28-artikelstamm-merkmale-filterbar-spec
status: Entwurf
created: 2026-11-28
updated: 2026-11-28
source_backlog: "[[2026-11-28-artikelstamm-merkmale-filterbar]]"
task: ""
jira: WMS-10
worktree: ""
branch: ""
standort: beide
affected_code:
  - IdealAkeWms/Data/Repositories/ArticleRepository.cs (ApplyArticleColumnFilter, Zeile 178-200)
  - IdealAkeWms/Controllers/ArticlesController.cs (Index, Zeile 51-88)
  - IdealAkeWms/Views/Articles/Index.cshtml (Zeile 75-79)
  - IdealAkeWms.Tests/Repositories/ArticleRepositoryTests.cs
  - IdealAkeWms.Tests/Services/ColumnFilterSyntaxTests.cs
  - Views/Help (Artikel-Hilfe) + docs/anwender (Kapitel Artikel)
  - docs/TESTSZENARIEN.md
open_questions:
  - "(Fachbereich: Benjamin Andl-Braun) Reicht fuer die Merkmal-Spalten die bekannte Textsuche der Filterzeile (enthaelt, =Wert, Anfang%, %Ende, (leer), ! fuer Verneinung, mehrere Werte mit ; oder ,), oder braucht ihr fuer einzelne Merkmale Zahlenvergleiche wie >2 oder 1...3 (z. B. Blechdicke)? Wenn ja: fuer welche Merkmale?"
  - "Sollen alle aktiven Merkmal-Spalten filterbar werden (manuelle Ja/Nein- und Auswahl-Merkmale ebenso wie die Sage-Benutzerfelder) oder nur die Sage-Merkmale? Empfehlung: alle - derselbe Code, keine Mehrkosten, sonst muesste die Oberflaeche erklaeren, warum manche Spalten keinen Filter haben."
  - "Die Spalte \"Erstellt am\" hat heute ebenfalls kein Filterfeld (Index.cshtml:80). Mitnehmen (Datumsfilter ueber ColumnFilterSql.DatePredicate + Register ColumnFilterTypes) oder ausserhalb dieser Spec lassen? Empfehlung: draussen lassen, nicht verlangt."
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

## Ziel / Nutzen (das Warum)

In der Artikelliste (`/Articles`) haben alle festen Spalten ein Filterfeld in der Spaltenfilter-Zeile, die Merkmal-Spalten
(`attr-{DefinitionId}`) aber nicht. Gerade die Sage-Benutzerfelder (z. B. `OSIstLagerartikel`, `OSArtikeltyp`) will der Fachbereich
IDEAL zum Eingrenzen nutzen (Mail „WMS Anpassungen“, Punkt 2, Anhang `2026-11-28-mail-wms-anpassungen-2.png`; Ergänzung Gerald
2026-11-28). „Leiste“ = Spaltenfilter-Zeile der Artikelliste (geklärt, Backlog).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Filterfeld in der Spaltenfilter-Zeile für jede angezeigte (= aktive) Merkmal-Spalte der Artikelliste, serverseitig vor dem
  Paging (ADR 0005), über den vorhandenen Filter-Kern (ADR 0017, `ColumnFilterSyntax` + `ColumnFilterSql`).
- Hilfe-/Anwendertext zur Artikelliste um den Hinweis ergänzen.

**Out-of-Scope**
- Mail Punkt 2, zweiter Satz („Ressourcenlisten von dem Artikel anzeigen“) → eigene Backlog-Notiz
  [[2026-11-28-artikelstamm-ressourcenliste-anzeigen]].
- Mail Punkt 5 (Zahnrad-Felder IstLagerartikel/Verwendungsart/Teileart in FA-Tabellen) → [[2026-11-28-fa-tabellen-zahnrad-lagerartikel-verwendungsart-teileart]].
- Zahlen-/Datumsvergleiche auf Merkmal-Werten (alle Merkmale sind Text-Filter, siehe Rückfrage 1).
- Spalte „Erstellt am“ (siehe Rückfrage 3).
- Änderung der Zellanzeige (Sage-Werte bleiben roh, wie sie heute angezeigt werden).
- Andere Listen mit Merkmalen (z. B. FA-Arbeitsliste).

## Fachliche Anforderungen

1. Jede in der Artikelliste angezeigte Merkmal-Spalte hat in der Filterzeile ein Textfeld wie die anderen Spalten.
2. Der Filter wirkt auf den **angezeigten Zelltext** (Regel „Getter rendern wie die Zelle“, fallstricke §26):
   - Ja/Nein-Merkmal (`AttributeType.Boolean`): Zelle „Ja“ bei `BooleanValue == true`, „Nein“ bei vorhandener Wertzeile mit
     `false`/`null`, leer ohne Wertzeile (`Index.cshtml:110-115`).
   - Text-Merkmal (`AttributeType.Text`, heute nur Sage-Benutzerfelder, `ArtikelBenutzerfelderWriter.cs:52`): `TextValue` roh
     (`Index.cshtml:116-119`).
   - Auswahl-Merkmal (`AttributeType.Dropdown`): `SelectedOption.Value`, leer ohne Option (`Index.cshtml:120-123`).
3. Die volle Filtersyntax gilt wie in jeder Textspalte: enthält, `=Wert`, `Anfang%`, `%Ende`, `(leer)`, `!` (Verneinung), mehrere
   Werte mit `;`/`,`. Groß/Klein egal (CI-Kollation, P-2).
4. Mehrere Spaltenfilter (fest + Merkmal) wirken UND-verknüpft, Trefferzahl („n Artikel gesamt“) und Paging stimmen.
5. Filter auf eine Merkmal-Spalte, die nicht angezeigt wird (inaktive oder unbekannte Definition, manipulierte URL), wirkt nicht –
   analog zum Fräsen-Pfad (`ArticlesController.cs:59-61`).

## Technischer Loesungsentwurf

Ponytail-Leiter: Sprosse 2 – alles vorhanden, kein neuer Filteransatz.

**Belege (Ist-Stand Hauptcheckout):**
- `Index.cshtml:75-79` rendert `<th data-col-key="attr-@def.Id">` bewusst ohne `data-filterable`; `table-filter.js:152` baut Filterfelder
  nur für `th[data-filterable]` und schreibt sie als `colf_<col-key>` in die URL (`table-filter.js:69`).
- `ArticlesController.Index` liest `ColumnFilterHelper.ReadFromQuery` (`ArticlesController.cs:58`) und gibt alles an
  `ArticleRepository.GetPaginatedAsync` → `BuildArticleQuery` → `ApplyArticleColumnFilter` (`ArticleRepository.cs:109-117, 178-200`).
  Unbekannte Keys fallen dort auf `_ => null` und filtern nicht.
- Datenmodell: `Article.AttributeValues` (Navigation, `Article.cs:68`), `ArticleAttributeValue` mit `BooleanValue`/`TextValue`/
  `SelectedOptionId` (`ArticleAttributeValue.cs`), **UNIQUE (ArticleId, ArticleAttributeDefinitionId)** (`ApplicationDbContext.cs:370`) –
  je Artikel und Merkmal höchstens eine Wertzeile, `FirstOrDefault` ist damit eindeutig.
- Vorbild Subquery-Selektor im selben Switch: `"hauptlieferant"` (`ArticleRepository.cs:184-186`) – Subquery mit `FirstOrDefault` als
  Selektor für `ColumnFilterSql.Predicate`; `(leer)` greift über `IsNullOrWhiteSpace` auch, wenn keine Zeile existiert
  (`ColumnFilterSyntax.cs:124`).

**Änderungen:**
1. `ArticleRepository.ApplyArticleColumnFilter`: neuer Fall vor `_ => null`, z. B.
   ```csharp
   _ when key.StartsWith("attr-", StringComparison.Ordinal) && int.TryParse(key.AsSpan(5), out var defId)
       => ColumnFilterSql.Predicate<Article>(f, a => a.AttributeValues
           .Where(v => v.ArticleAttributeDefinitionId == defId)
           .Select(v => v.ArticleAttributeDefinition.AttributeType == AttributeType.Boolean ? (v.BooleanValue == true ? "Ja" : "Nein")
                      : v.ArticleAttributeDefinition.AttributeType == AttributeType.Text ? v.TextValue
                      : v.SelectedOption != null ? v.SelectedOption.Value : null)
           .FirstOrDefault()),
   ```
   Der Typ wird in SQL über die Definition entschieden (CASE) – keine Signaturänderung, keine zusätzliche Abfrage. Der Ausdruck ist
   die SQL-Form der Zellanzeige `Index.cshtml:110-123` und muss mit ihr gleich bleiben (Kommentar an beiden Stellen).
2. `ArticlesController.Index`: `GetActiveDefinitionsOrderedAsync` (heute Zeile 65) **vor** `GetPaginatedAsync` laden und alle
   `columnFilters`-Keys `attr-*` entfernen, deren Id nicht unter den aktiven Definitionen ist (Anforderung 5; Muster Zeile 61).
3. `Index.cshtml:77-78`: Kommentar ersetzen, `data-filterable` an die Merkmal-`<th>` setzen. Kein `data-filter-type` (Text ist
   Standard) → **kein** Eintrag in `ColumnFilterTypes.Lists` nötig, der Drift-Wächter bleibt unberührt.
4. Tests (Muster vorhanden):
   - InMemory: je Typ (Boolean Ja/Nein/ohne Zeile, Text, Dropdown) Treffer, `(leer)`, `!wert`; Testdaten kleingeschrieben
     (fallstricke §26 „Groß/Klein in SQL macht die Kollation“), Muster `ColumnFilterSyntaxTests.cs:279`.
   - SqlServer-`ToQueryString()`: `colf_attr-<id>` erzeugt eine korrelierte Subquery und kein Client-Eval, Muster
     `ArticleRepositoryTests.cs:185-225` / `ArtikelNachbesserungV185Tests.cs:83`.
   - Controller: Filter auf inaktive Definition wird verworfen.

**Leistung:** je aktivem Merkmal-Filter eine korrelierte Subquery; der UNIQUE-Index (ArticleId, ArticleAttributeDefinitionId) trägt
den Zugriff. `ponytail:` kein Zusatz-Index; erst bei gemessener Latenz.

**Rollen/Zugriff:** unverändert. Klassenfilter `[RequireMasterDataReadAccess]` (`ArticlesController.cs:10`); Filtern ist reiner Lesezugriff.

## Migrations-/SQL-Auswirkungen

Keine Migration, kein `SQL/XX_*.sql`, kein FreshInstall, keine AgentJobs. Prüfschritt P-2 (Kollation) für die zwei neu gefilterten
Spalten auf **beiden** Servern (WMS-DB, nicht Sage):

```sql
SELECT DATABASEPROPERTYEX(DB_NAME(), 'Collation') AS DbKollation;
SELECT TABLE_NAME, COLUMN_NAME, COLLATION_NAME
FROM INFORMATION_SCHEMA.COLUMNS
WHERE (TABLE_NAME = 'ArticleAttributeValues' AND COLUMN_NAME = 'TextValue')
   OR (TABLE_NAME = 'ArticleAttributeOptions' AND COLUMN_NAME = 'Value');
```
Erwartung: `_CI_` in allen Zeilen. Nicht belegt, bis gelaufen.

Optional zur Einschätzung, welche Rohwerte die Sage-Ja/Nein-artigen Felder tragen (für die Hilfe-Beispiele):
```sql
SELECT d.Name, v.TextValue, COUNT(*) AS Anzahl
FROM ArticleAttributeValues v JOIN ArticleAttributeDefinitions d ON d.Id = v.ArticleAttributeDefinitionId
WHERE d.SyncSource = 'Sage' AND d.IsActive = 1
GROUP BY d.Name, v.TextValue ORDER BY d.Name, Anzahl DESC;
```

## Einstellungen

keine

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine – reiner Lesepfad, es wird nichts geschrieben.

## Akzeptanzkriterien

1. In `/Articles` hat jede angezeigte Merkmal-Spalte ein Filterfeld in der Spaltenfilter-Zeile.
2. Text-Merkmal (Sage-Benutzerfeld): Eingabe eines Teilworts zeigt nur Artikel, deren Zelle es enthält; Trefferzahl und Paging passen.
3. Ja/Nein-Merkmal: `=ja` zeigt nur „Ja“-Zellen, `=nein` nur „Nein“-Zellen, `(leer)` nur Artikel ohne Wertzeile.
4. Auswahl-Merkmal: `=<Optionswert>` zeigt nur Artikel mit dieser Option.
5. `!wert` und `!(leer)` kehren den Filter um; `a;b` trifft beide Werte.
6. Groß/Klein spielt keine Rolle (SQL Server, P-2 grün).
7. Merkmal-Filter kombiniert mit Artikelnummer-/Bezeichnungsfilter und dem Suchfeld liefert die Schnittmenge.
8. Filterwert bleibt nach Seitenwechsel und nach Ein-/Ausblenden der Spalte im Zahnrad in der URL erhalten und wirkt weiter.
9. Manipulierte URL `?colf_attr-<Id einer inaktiven Definition>=x` bzw. `colf_attr-abc=x` filtert nicht (volle Liste).
10. Ohne Merkmal-Filter ist das Verhalten der Liste unverändert (bestehende Tests grün).
11. `ToQueryString()`-Test belegt SQL-Ausführung (keine In-Memory-Nachfilterung).

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (Nummer vergibt der Dev-Lauf, nächste freie TS-Nummer), Index
`secondbrain/tests/testszenarien-index.md` nachziehen. DEV AKE und DEV IDEAL.

- **Vorbedingung:** Rolle mit `masterdata_read`; mindestens ein aktives Text-Merkmal (Sage), ein Ja/Nein- und ein Auswahl-Merkmal mit
  gesetzten und fehlenden Werten. Merkmal-Spalten im Zahnrad eingeblendet.
- **TS-x.1** Sage-Text-Merkmal: Teilwort filtern → nur passende Zeilen, Zähler stimmt.
- **TS-x.2** Ja/Nein-Merkmal: `=ja`, `=nein`, `(leer)` → je genau die erwarteten Zeilen.
- **TS-x.3** Auswahl-Merkmal: `=<Option>`; `!<Option>` zeigt alle anderen inkl. leerer.
- **TS-x.4** Kombination Merkmal + Artikelnummer + Suchfeld → Schnittmenge; Seite 2 aufrufen → Filter bleibt.
- **TS-x.5** Groß/Klein: `JA` und `ja` liefern dasselbe.
- **Negativ TS-x.6** URL mit `colf_attr-<inaktive Id>=x` → volle Liste, kein Fehler.
- **Negativ TS-x.7** Nur-Lesen-Benutzer: Filtern funktioniert, keine Bearbeiten-Knöpfe (Rollen unverändert).

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt (kein Epic) | – | – |

## Deploy

Vorläufig – der QA-Lauf bestätigt gegen den echten Diff.

- **Web-App:** ja (Repository, Controller, View)
- **Service:** nein
- **Migration:** nein
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): keine. P-2 ist ein Prüfschritt der Abnahme (Abfrage oben), kein
  Deploy-Zustand.
- **Publish-Befehle:**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Offene Rueckfragen

1. (Fachbereich: Benjamin Andl-Braun) Reicht für die Merkmal-Spalten die bekannte Textsuche der Filterzeile (enthält, `=Wert`,
   `Anfang%`, `%Ende`, `(leer)`, `!` für Verneinung, mehrere Werte mit `;` oder `,`), oder braucht ihr für einzelne Merkmale
   Zahlenvergleiche wie `>2` oder `1...3` (z. B. Blechdicke)? Wenn ja: für welche Merkmale?
2. Sollen alle aktiven Merkmal-Spalten filterbar werden (manuelle Ja/Nein- und Auswahl-Merkmale ebenso wie die Sage-Benutzerfelder)
   oder nur die Sage-Merkmale? Empfehlung: alle – derselbe Code, keine Mehrkosten, sonst müsste die Oberfläche erklären, warum manche
   Spalten keinen Filter haben.
3. Die Spalte „Erstellt am“ hat heute ebenfalls kein Filterfeld (`Index.cshtml:80`). Mitnehmen (Datumsfilter über
   `ColumnFilterSql.DatePredicate` + Register `ColumnFilterTypes`) oder außerhalb dieser Spec lassen? Empfehlung: draußen lassen,
   nicht verlangt.

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
-->

1. Textsuche reicht oder Zahlenvergleiche (Fachbereich) →
2. Alle Merkmale oder nur Sage-Merkmale →
3. „Erstellt am“ mitnehmen →
