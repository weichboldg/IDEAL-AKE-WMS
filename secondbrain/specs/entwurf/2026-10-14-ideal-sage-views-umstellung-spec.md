---
type: spec
title: "IDEAL: Sage-Zugriffe auf die neuen WMS-Views umstellen (FAInfos, FAListe, Arbeitsgaenge, Artikel)"
slug: 2026-10-14-ideal-sage-views-umstellung-spec
status: Entwurf
created: 2026-10-14
updated: 2026-10-14
source_backlog: "[[2026-10-14-ideal-sage-views-umstellung]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Models/ServiceSettingDefinitions.cs (Defaults FAListe/FAInfos :38-39, zwei neue Keys)
  - IdealAkeWms/Models/Standort/StandortSettingsCatalog.cs (:42-59, neue Keys in die Maske)
  - IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs (Drift-Guard-Liste :81-121)
  - IDEALAKEWMSService/Services/FaHierarchySyncService.cs (Fallback :51-56, Existenzpruefung vor :88)
  - IDEALAKEWMSService/Services/FaHierarchySql.cs (ValidateAndQuote wiederverwenden, ggf. Existenz-SQL)
  - IDEALAKEWMSService/Services/SageArbeitsplatzReader.cs (:24-29 Quelle konfigurierbar)
  - IDEALAKEWMSService/Services/ISageArbeitsplatzReader.cs (Signatur/Rueckgabe "Quelle fehlt")
  - IDEALAKEWMSService/Services/ProductionWorkplaceSyncService.cs (:48-64 Key lesen, Skip-Pfad)
  - IDEALAKEWMSService/Services/SageImportService.cs (SyncArticlesAsync :369-450 Quelle umschaltbar)
  - IDEALAKEWMSService.Tests (FaHierarchySqlTests, ProductionWorkplaceSyncServiceTests, neuer SQL-Builder-Test Artikel)
  - docs/TESTSZENARIEN.md, README.md (Betriebshinweis), Views/Help/Changelog.cshtml, AppVersion.cs (Web + Service)
open_questions:
  - "Was genau ist das Problem der 00er-Artikelnummern? (Beispielnummer + wo sichtbar). Hypothesen H1/H2/H3 im Rumpf; Empfehlung: P5/P6 laufen lassen und eine konkrete Beispielnummer nennen."
  - "Bestehende 00er-Zeilen in Articles bleiben nach der Umstellung stehen (der Artikel-Sync loescht nie). In dieser Spec nur messen und zaehlen, Bereinigung als eigene Folgeaufgabe (Einmal-Skript nach ADR 0015, nur unreferenzierte Zeilen)? Empfehlung: ja."
  - "Bestaetigung: Die WMS-Tabelle Articles bleibt, die View vw_IDEAL_AKE_WMS_Artikel ersetzt nur die QUELLE des Artikel-Syncs? Empfehlung: ja."
  - "Spaltenabbildung vw_IDEAL_AKE_WMS_Artikel -> ArticleNumber/Description/Unit/ArticleGroup/ReorderLevel/SagePrimaryStorageLocation/Matchcode: Spaltennamen und Formate (Ergebnis P4), insbesondere Artikelgruppe nur Code ('940') oder 'Code - Bezeichnung'?"
  - "Gibt es alle vier Views auch in der Sage-Test-DB des IDEAL-Testsystems (P1 dort)? Empfehlung: vor der UAT anlegen, sonst prueft die UAT nur den Skip-Pfad."
  - "Fehlende View: nur Warnung im Aktivitaets-Protokoll (wie FA-Zusatzinfo) oder zusaetzlich Fehlermail? Empfehlung: keine Mail. Folge: ein Tippfehler im FAListe/FAInfos-View-Namen mailt danach nicht mehr (heute: SqlException -> Mail)."
  - "Standort-Schaltung des Artikel-Syncs ueber den neuen String-Key Sync:ArticlesViewName (leer = bisheriger Select) statt Kopplung an den Master ProduktionsauftragHierarchisch? Empfehlung: eigener Key."
  - "Arbeitsgaenge: Default des neuen Keys Sync:ProductionWorkplaceSyncViewName = [vw_IDEAL_AKE_WMS_Arbeitsgaenge] und Rueckweg ohne Deploy durch Setzen auf KHKPpsArbeitsplaetze (Existenzpruefung ohne Typfilter, Tabelle oder View)? Empfehlung: ja."
  - "FAListe/FAInfos schon VOR dem Deploy dieser Spec per Einstellung umstellen (geht ohne Code, sobald P2 null Abweichungen liefert)? Empfehlung: ja, aber erst nach Klaerung von Rueckfrage 1, falls die neue FAListe die Artnr anders formatiert."
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

> [!note] Code-Grundlage und Zeilennummern
> Alle Datei:Zeile-Angaben wurden **nur lesend** im Buendel-Worktree
> `.claude/worktrees/2026-08-07-ideal-teile-1-5` (HEAD `d4108f1a`) gemessen. Die Umsetzung laeuft
> **nach dem Buendel-Merge** auf einem neuen Zweig ab `main`. Der Dev-Lauf misst alle Zeilennummern
> vor dem Start gegen `main` nach. `worktree`/`branch` bleiben bis dahin leer.

## Ziel / Nutzen (das Warum)

Gerald hat in der IDEAL-Sage-Live-DB vier Views angelegt, die fuer das WMS gedacht sind. Das WMS soll
nicht mehr direkt Sage-Tabellen bzw. die alten Kommissionier-Views abfragen, sondern diese vier Views.
Dann kann die Sage-Seite festlegen, was das WMS sieht, ohne dass der WMS-Code mitgeaendert werden muss.
Die Artikel-View soll ausserdem das (noch unklare) Problem der „00er-Artikelnummern“ beheben.
AKE hat diese Views nicht und muss **unveraendert** bleiben.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. **FAInfos/FAListe:** Default und Fallback der View-Namen auf `[vw_IDEAL_AKE_WMS_FAListe]` /
   `[vw_IDEAL_AKE_WMS_FAInfos]` umstellen. Betriebshinweis, dass bestehende DB-Werte von Hand umgestellt
   werden muessen.
2. **Arbeitsgaenge:** Der Werkbank-Sync (`SageArbeitsplatzReader`) liest aus einer per Einstellung
   benannten Quelle, Default `[vw_IDEAL_AKE_WMS_Arbeitsgaenge]`.
3. **Artikel:** Die Quelle des Artikel-Syncs wird je Standort umschaltbar: leer = bisheriger Select
   (AKE), gesetzt = View (IDEAL). Die WMS-Tabelle `Articles` bleibt (Rueckfrage 3).
4. **00er-Artikelnummern:** Pruefabfragen vorher/nachher und Testfall. Bereinigung des Altbestands nur
   nach Entscheidung zu Rueckfrage 2.
5. **Fehlende View:** Alle drei Syncs pruefen vor dem Lesen, ob die Quelle existiert. Fehlt sie, gibt es
   eine Warnung im Aktivitaets-Protokoll und der Lauf wird uebersprungen, ohne Absturz und ohne
   Mutation.

**Out-of-Scope (belegt, siehe Fundstellen-Tabelle unten)**
- `SageBestandReader` (Bestand aus `KHKArtikel`/`KHKLagerplatzbestaende`) und alle anderen
  Sage-Lesestellen, die keine Artikel-Stammdaten lesen.
- AKE-Views `vw_AKE_Kommissionierung_WAListe` / `_StuecklistenDB` und der DEPRECATED-AgentJob
  `SQL/AgentJobs/02_Import_Artikel.sql` (Kopf :1-11: „Bei Aenderungen am Service-Sync bitte hier NICHT
  nachziehen“).
- Die Bereinigung bestehender 00er-Artikel (Folgeaufgabe, Rueckfrage 2).
- `vw_IDEAL_AKE_WMS_FAZusatzinformationen` (`SageZusatzinfoReader.cs:33,50`): ist schon eine WMS-View
  und bleibt, wie sie ist.

**Groesse:** mittel. Drei kleine, gleich gebaute Eingriffe im Service (Quelle konfigurierbar machen und
Existenz pruefen), zwei Katalog-Keys, Doku. Ungefaehr 10 bis 12 Dateien, keine Migration und keine neue
Oberflaeche. **Kein Split- und kein Epic-Kandidat:** Die Teile teilen sich dasselbe Muster
(`ValidateAndQuote` + Existenzpruefung) und denselben Deploy. Ein eigener Merge je Teil wuerde nur den
Deploy vervielfachen. Teil 1 (FA-Views) braucht ohnehin keinen Code, um wirksam zu werden (siehe unten).
Falls Rueckfrage 2 eine Bereinigung verlangt, wird sie eine **eigene** Folge-Spec.

## Fachliche Anforderungen

### Ist-Stand (belegt)

| Thema | Beleg | Befund |
|---|---|---|
| Settings sind DB-first, ohne Cache | `IDEALAKEWMSService/Common/ServiceSettings.cs:9-29` | Jeder Lauf liest `[ServiceSettings]` neu. Eine Aenderung in `/ServiceSettings` oder `/StandortEinstellungen` wirkt **ab dem naechsten Lauf, ohne Deploy und ohne Neustart**. |
| Seed setzt nur fehlende Keys | `IdealAkeWms/Program.cs:453-468` | `if (!db.ServiceSettings.Any(s => s.Key == def.Key)) Add(... def.DefaultValue ...)`. Bestehende Zeilen bleiben unberuehrt. **Folge:** Eine Default-Aenderung wirkt **nicht** auf bestehende Installationen (AKE und IDEAL haben die FA-View-Keys schon). Ein **neuer** Key wird dagegen beim naechsten Web-Start mit seinem Default in jede bestehende DB geschrieben. |
| FA-View-Namen | `ServiceSettingDefinitions.cs:38-39`, `StandortSettingsCatalog.cs:44-45`, `FaHierarchySyncService.cs:51-56` | Default und Fallback zeigen auf `[vw_IDEAL-AKE_Kommissionierung_FAListe]`/`…FAInfos]`. Der Fallback greift nur bei leerem/fehlendem DB-Wert. |
| Quoting | `FaHierarchySql.cs:41-60, 62-107` | Whitelist `^[A-Za-z0-9_\-]+$` je Segment, 1-2 Segmente, QUOTENAME-Aequivalent. `vw_IDEAL_AKE_WMS_FAListe` usw. bestehen nur aus `[A-Za-z0-9_]` und **passieren** die Validierung unveraendert. Es ist keine Code-Aenderung am Quoting noetig. |
| Gelesene Spalten FA | `FaHierarchySql.cs:119-143`, Reader `FaHierarchySyncService.cs:204-217, 263-270` | FAListe: 27 Spalten (`HauptFA` … `VMBedarf`). FAInfos: `HauptFA, Abteilung (AS MontageAbteilung), ABNr, Pos, Kunde, Status, Dienstleister, RAL, Bemerkung_Uhrzeit, KO_Termin, FE_Termin, Start_Beschichtung, Beschichten_Retour, Neuer_PT_PPS, Verladetermin_Vsl, Montagestunden, Prio`. Zugriff per `GetOrdinal(Name)`. Fehlt eine Spalte, gibt es eine Exception und keinen Teil-Import. |
| Fehlende FA-View heute | `FaHierarchySyncService.cs:78-163` | Es gibt **keine** Existenzpruefung. `COUNT_BIG` auf eine fehlende View wirft eine `SqlException`, dann folgen `FinishFailedAsync` + rethrow (:155-163) und eine Fehlermail ueber `RunResilientAsync` (`SyncWorker.cs:339`). Das passiert in **jedem** Zyklus. |
| Arbeitsplatz-Read | `SageArbeitsplatzReader.cs:24-29` | `SELECT Arbeitsplatznummer, Bezeichnung1, USER_ArbeitsSchritt FROM KHKPpsArbeitsplaetze WHERE Mandant = @mandant AND Aktiv = -1`. Ordinal-Read: `GetValue().ToString()`, `GetString(1)`, `GetString(2)` (:41-45). |
| Wer liest Arbeitsplaetze | `Program.cs:59` (Service, DI), einziger Konsument `ProductionWorkplaceSyncService.cs:19,28,56`; Aufruf nur `SyncWorker.cs:377-389` | Doppel-Gate `ProduktionsauftragHierarchisch` **und** `Sync:ProductionWorkplaceSyncEnabled`. **AKE** (Master false) ruft den Reader nie auf. Die AKE-Werkbaenke kommen aus OSEON (`OseonSyncService.cs:227`). Weitere Lesestellen von `KHKPpsArbeitsplaetze` gibt es im Service- und Web-Code nicht (Grep `KHKPpsArbeitsplaetze` in `*.cs`/`*.sql`: nur Kommentare, `ServiceSettingDefinitions.cs:42-43` und `SyncLogServices.cs:29`). |
| Werkbank-Match | `ProductionWorkplaceSyncService.cs:71-95` | Abgleich ueber `SageArbeitsplatznummer` (getrimmt, `StringComparer.Ordinal`). Liefert die View die Nummer **anders formatiert** als die Tabelle (z. B. ohne fuehrende Nullen), entstehen Neuanlagen bzw. Index-Verstoesse („Schluesselregel verletzt“, :113-125). Deshalb ist P3 eine Vorbedingung. |
| Artikel-Sync | `SageImportService.cs:369-593`, Aufruf `SyncWorker.cs:85-96` | Gate nur `Sync:ArticlesEnabled` (Default `true`) und **kein** Master-Gate, er laeuft also an **beiden** Standorten. Quelle (:382-424) ist ein UNION aus (a) `KHKPpsRessourcenPositionen` **LEFT JOIN** `KHKArtikel` **ohne Ressourcentyp-Filter** und (b) `KHKArtikel WHERE IstBestellartikel = -1 AND Aktiv = -1`, danach `GROUP BY ArticleNumber` mit `MAX(...)`. Spalten-Ordinal-Read (:436-446). |
| Artikel-Upsert | `SageImportService.cs:484-521` | `IF EXISTS … UPDATE … ELSE INSERT`, Schluessel `[ArticleNumber] = @ArticleNumber`. **Nur Upsert, nie Delete.** Im Code gibt es keinen Loeschweg fuer `Articles` (Grep `Articles.Remove` / `DELETE FROM [dbo].[Articles]` / `articleRepository.Delete` in `IdealAkeWms`, `IDEALAKEWMSService`, `SQL`: 0 Treffer, und `ArticlesController` hat keine Delete-Action). |

### Wo `Articles.ArticleNumber` als Schluessel wirkt (fuer die 00er-Bewertung)

| Stelle | Beleg | Art der Kopplung |
|---|---|---|
| Eindeutigkeit | `ApplicationDbContext.cs:232` | `HasIndex(ArticleNumber).IsUnique()` |
| Bewegungen / Bestand | `ApplicationDbContext.cs:344-351` | `StockMovement.ArticleId` FK (Restrict) ueber die **Id**, nicht die Nummer |
| Artikel-Merkmale | `ApplicationDbContext.cs:299-313` | `ArticleAttributeValue.ArticleId` FK |
| FA-Vorbau-Spezifikation | `ApplicationDbContext.cs:590-593` | `FaWorkStepSpec.ArticleId` FK (SetNull) |
| Kommissionier-Umbuchung | `PickingTransferService.cs:167-183` | String-Match `PickingItem.BomArticleNumber` gegen `Articles.ArticleNumber`. **Kein Treffer bedeutet: die Zeile wird still uebersprungen und es entsteht keine `StockMovement`** (fallstricke §1, Bug vom 16.02.2026). |
| IDEAL-Stueckliste | `FaHierarchyBomRepository.cs:179` | `Ressourcenummer = node.Artnr` aus FAListe. Daraus wird `PickingItem.BomArticleNumber`. |
| Sage-Lagerbestand-Abgleich | `LagerbestandSyncService.cs:97, 129-133` | `KHKArtikel.Artikelnummer` gegen `Articles.ArticleNumber` (OrdinalIgnoreCase). Kein Treffer ergibt eine Warnung „nicht im WMS, uebersprungen“. |
| Sage-Lagerbuchung (SData) | `SageLagerbuchungPayloadBuilder.cs:37` | `Artikelnummer = article.ArticleNumber` geht **an Sage**. Weicht die WMS-Nummer vom Sage-Artikelstamm ab, bucht Sage auf einen unbekannten bzw. falschen Artikel. |
| Matchcode in FA-Listen | `ArticleRepository.cs:81-89` | String-Match (OrdinalIgnoreCase) |
| Lagerbestellung / Fehlteile | `WarehouseRequisitionRepository.cs:124, 458`, `PartRequisitionRepository.cs:53, 62, 85`, `ApplicationDbContext.cs:1218-1220` | String-Match auf `ArticleNumber` |
| Lackierteil-Erkennung (AKE) | `CoatingDetectionService.cs:153` | `JOIN Articles a ON a.ArticleNumber = i.Ressourcenummer` |
| OSEON-Kategorie (AKE) | `OseonSyncService.cs:687-689` | `ArticleNumber = @ArticleNumber COLLATE Latin1_General_CI_AS` |
| Empfaenger-Mapping Artikelgruppe | `OrderRecipientRepository.cs:98, 111` | **exakter** Vergleich `ArticleGroup == articleGroup`. Das Format von `Articles.ArticleGroup` (heute der reine Code `a.Artikelgruppe`, `SageImportService.cs:388/404`) darf sich durch die View **nicht** aendern (fallstricke §1 „Artikelgruppe BOM vs Articles“). |

**Bewertung:** Bestand und Bewegungen haengen an der **Id**. Aendert sich nur die Quelle, nicht aber das
Nummernformat, bleibt alles unberuehrt. Kritisch wird es nur, wenn die View fuer **denselben** Sage-Artikel
eine **andere Schreibweise** liefert als heute. Dann legt der Upsert eine **neue** Zeile an (Dublette
`00xxx`/`xxx`), Bestand und Bewegungen bleiben an der alten Id, und Sage-Lagerbuchung, Bestand-Abgleich und
Kommissionier-Umbuchung treffen je nach Schreibweise die falsche oder keine Zeile. Deshalb gilt die
Vorbedingung P4c: **jede** Artikelnummer der View existiert 1:1 in `KHKArtikel.Artikelnummer`.

### 00er-Artikelnummern: Hypothesen (unverifiziert, Rueckfrage 1)

Im Brain ist das Problem nicht dokumentiert (Grep `00er|fuehrende|Nullen` in `secondbrain/`: keine
fachliche Fundstelle). Was der Code zulaesst:

- **H1, Nicht-Artikel-Ressourcen:** Zweig (a) des Artikel-Selects nimmt **jede** `Ressourcenummer` aus
  `KHKPpsRessourcenPositionen`, und zwar per **LEFT** JOIN und **ohne** Ressourcentyp-Filter
  (`SageImportService.cs:392-396`). Die IDEAL-FAListe filtert dagegen auf `RessourceTyp = 'MA'`
  (`SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql:18-19`). Ressourcen ohne Artikelstamm (z. B.
  Arbeitsplatz-Ressourcen; Arbeitsplatznummern wie `0000`/`2300` werden in
  [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]] genannt) koennten deshalb als „Artikel“ ohne
  Bezeichnung in `Articles` landen. Die View wuerde sie nicht mehr liefern. **Altbestand bleibt** (kein
  Delete).
- **H2, Formatabweichung:** Dieselbe Nummer steht in Sage-Quellen unterschiedlich (mit/ohne fuehrende
  Nullen). Folge: Dubletten bzw. Fehltreffer an den String-Match-Stellen oben.
- **H3:** etwas anderes, das nur Gerald kennt.

Die Pruefabfragen P5/P6 unterscheiden H1 und H2.

## Technischer Loesungsentwurf

Leitlinie (ponytail): **ein** Muster fuer alle drei Quellen, ausschliesslich aus vorhandenen Bausteinen.

1. **Name validieren:** `FaHierarchySql.ValidateAndQuote` (`FaHierarchySql.cs:41`) wird unveraendert
   wiederverwendet. Ein ungueltiger Name fuehrt wie heute bei FA (`FaHierarchySyncService.cs:64-76`) zu
   Protokoll-Fehler, `FinishFailedAsync` und Fehlermail, ohne dass SQL ausgefuehrt wird. Der Name der
   Exception (`FaHierarchyViewNameException`) wird nicht umbenannt.
2. **Existenz pruefen:** ein kleiner gemeinsamer Helfer `SELECT OBJECT_ID(@name)` mit dem **geklammerten**
   Namen als Parameter. Er laeuft **ohne** Typfilter `'V'`, damit eine Tabelle als Rueckweg-Quelle zulaessig
   bleibt (Rueckfrage 8). `NULL` bedeutet: Warnzeile
   `"Sage-Quelle <name> nicht vorhanden oder keine Leseberechtigung — Sync uebersprungen."`, dann
   `FinishSuccessAsync` mit den ueblichen Counts = 0 und `messageSuffix` „Quelle nicht vorhanden“ (Muster
   `FaZusatzinfoSyncService.cs:62-75`). Es gibt keinen throw und keine Mutation. Eine Mail gibt es nur, wenn
   Rueckfrage 6 sie verlangt.
   *Warum der Text „oder keine Leseberechtigung“:* `OBJECT_ID` liefert in SQL Server auch dann `NULL`, wenn der
   Login keine Metadaten-Sicht auf das Objekt hat. Eine fehlende `GRANT SELECT` auf eine neue View sieht also
   genauso aus wie eine fehlende View (siehe P1b).
3. **FA (Teil 1):** Default (`ServiceSettingDefinitions.cs:38-39`) und Fallback (`FaHierarchySyncService.cs:53, 56`)
   werden auf `[vw_IDEAL_AKE_WMS_FAListe]` / `[vw_IDEAL_AKE_WMS_FAInfos]` gesetzt. Die Existenzpruefung (2)
   fuer **beide** Views laeuft vor dem Empty-Guard (`:88-89`). Fehlt eine, wird uebersprungen, und die
   Zieltabellen `FaHierarchyNodes`/`FaHierarchyOrderInfos` bleiben unberuehrt. In `FaHierarchySqlTests` kommt
   ein `InlineData` mit den neuen Namen dazu. Die alten Testdaten bleiben (sie pruefen den Bindestrich-Fall).
4. **Arbeitsgaenge (Teil 2):** neuer Key `Sync:ProductionWorkplaceSyncViewName` (String, Default
   `[vw_IDEAL_AKE_WMS_Arbeitsgaenge]`, Kategorie `FA-Hierarchie`). Ihn liest `ProductionWorkplaceSyncService`
   (neben `:48-51`, `GetValueSafeAsync`). Leer bedeutet Fallback auf denselben Default. Der Service validiert,
   quotet und reicht den Namen an den Reader weiter. Der Reader prueft die Existenz und liest **dieselben drei
   Spalten mit demselben Filter** `WHERE Mandant = @mandant AND Aktiv = -1` aus `{quoted}`. Rueckgabe nach
   dem Muster `SageZusatzinfoReadResult(ViewExists, Rows)`. Die Doppel-Gate-Logik in `SyncWorker.cs:377-378`
   bleibt **unveraendert**. Damit ist belegt, dass AKE den Code nicht erreicht.
5. **Artikel (Teil 3):** neuer Key `Sync:ArticlesViewName` (String, Default `""`, Kategorie `Sync`). Das Muster
   „leer = inaktiv“ steht in ADR 0011, und `""`-Defaults gibt es schon (`ServiceSettingDefinitions.cs:48, 51, 75`).
   - **Leer:** Der bisherige Select `sageSql` (`SageImportService.cs:382-424`) bleibt **zeichengleich**. Das
     ist AKE und der IDEAL-Zustand bis zur Umstellung.
   - **Gesetzt:** validieren, Existenz pruefen und dann einen Select auf die View mit **gleicher Spaltenreihenfolge
     und gleichen Aliasen**, damit der Reader (:433-447) und der Upsert (:482-572) unveraendert bleiben:
     ```sql
     SELECT CAST(<ArtNr>      AS nvarchar(100)) AS ArticleNumber,
            MAX(CAST(<Bez>    AS nvarchar(500))) AS Description,
            MAX(CAST(<Einheit> AS nvarchar(20))) AS Unit,
            MAX(CAST(<ArtGrp> AS nvarchar(100))) AS ArticleGroup,
            MAX(CAST(<Melde>  AS nvarchar(20)))  AS ReorderLevel,
            MAX(CAST(<Hlp>    AS nvarchar(100))) AS PrimaryStorageLocation,
            MAX(CAST(<Match>  AS nvarchar(200))) AS Matchcode
     FROM {quotedView}
     WHERE <ArtNr> IS NOT NULL AND <ArtNr> <> ''
     GROUP BY CAST(<ArtNr> AS nvarchar(100))
     ```
     Die Platzhalter `<…>` werden **erst nach P4 / Rueckfrage 4** gefuellt (die View-Definition liegt nicht im
     Repo). `GROUP BY`/`MAX` bleibt als Schutz gegen Dubletten in der View, sonst wuerde der Upsert dieselbe
     Zeile je Lauf mehrfach ueberschreiben. Das ist dasselbe Verhalten wie heute (:413-423).
   - Die Auswahl des Selects liegt in einer kleinen, **reinen** Funktion (Muster `FaHierarchySql`), damit beide
     Zweige ohne Sage unit-testbar sind.
   - **Fehlt die konfigurierte View, wird uebersprungen und nicht auf den alten Select zurueckgefallen.** Ein
     stiller Rueckfall wuerde genau die Daten wieder importieren, die die View abloesen soll (Melden statt
     still behandeln).
6. **Katalog und Maske:** Beide neuen Keys kommen in `ServiceSettingDefinitions.All`, in die
   Drift-Guard-Liste `ServiceSettingDefinitionsTests.cs:81-121` (auch die beiden bestehenden FA-Keys stehen
   dort schon, :103-104) und in `StandortSettingsCatalog.Fields` (Gruppe „Mandant / Sage-Views“, Hinweis
   `ViewNameHint`). Beschreibungen sind hoechstens 500 Zeichen lang (fallstricke §16,
   `All_FieldsFitServiceSettingsColumns`).

### Sage-Lesestellen mit Artikelbezug: Bewertung (Grep `KHKArtikel|KHKPps…|KHKLagerplaetze|vw_` im Worktree)

| Fundstelle | Liest | Bewertung |
|---|---|---|
| `SageImportService.cs:382-424` | Artikelstamm fuer `Articles` | **umstellen** (Teil 3) |
| `SageBestandReader.cs:24-36` | `KHKArtikel` + `KHKLagerplatzbestaende` + `KHKLagerplaetze`: **Bestand** je Artikel/Platz, nicht der Stamm | **nicht umstellen.** Der Bestand ist Sage-Wahrheit aus den Bestandstabellen, die View liefert keine Bestaende (laut Backlog „Infos zu allen Artikeln“). Das gilt unter der Bedingung P4c (View-Nummern = `KHKArtikel.Artikelnummer`), sonst findet `LagerbestandSyncService.cs:129` die Artikel nicht mehr. Nebenbefund: `A.Mandant = 1` ist fest codiert (:33). Das ist nicht Teil dieser Spec, nur gemeldet. |
| `SageLagerplatzReader.cs:25-26` | `KHKLagerorte`/`KHKLagerplaetze` (Lagerplaetze) | kein Artikelbezug, bleibt |
| `SageBuchungLookupReader.cs:15` | `KHKLagerplatzbuchungen` (Memo-Lookup) | kein Artikelbezug, bleibt |
| `SageLagerbuchungPayloadBuilder.cs:37` | schreibt `Article.ArticleNumber` an Sage | kein Read, aber **abhaengig** von P4c |
| `SageImportService.cs:57` | `vw_AKE_Kommissionierung_WAListe` (FA-Import) | AKE-View, nicht Teil des Auftrags, bleibt |
| `BomCacheSyncService.cs:294`, `BomRepository.cs:44` (Web) | `[ake].[dbo].[vw_AKE_Kommissionierung_StuecklistenDB]` | AKE-Stueckliste. IDEAL nutzt `FaHierarchyBomRepository` (ADR 0013), bleibt |
| `SageArbeitsplatzReader.cs:24-29` | `KHKPpsArbeitsplaetze` | **umstellen** (Teil 2) |
| `FaHierarchySyncService.cs:51-56` | FAListe/FAInfos | **umstellen** (Teil 1) |
| `SageZusatzinfoReader.cs:33, 50` | `vw_IDEAL_AKE_WMS_FAZusatzinformationen` | schon eine WMS-View, bleibt |
| `SQL/AgentJobs/02_Import_Artikel.sql` | `[ake].[dbo].KHK…` | DEPRECATED, laut Kopf ausdruecklich nicht nachziehen |
| `EnaioDmsSyncService.cs:52` | enaio-View | nicht Sage, bleibt |

### Pruefabfragen fuer Gerald (Sage nicht erreichbar, alle Ergebnisse **unverifiziert**, bis sie gelaufen sind)

Auf der **Sage-DB IDEAL live** und auf der **Sage-Test-DB** des IDEAL-Testsystems, jeweils unter dem Login
der `SageConnection` des Services:

**P1 Existenz (+ P1b Berechtigung)**
```sql
SELECT SCHEMA_NAME(o.schema_id) AS [schema], o.name, o.type_desc
FROM sys.objects o
WHERE o.name IN (N'vw_IDEAL_AKE_WMS_FAInfos', N'vw_IDEAL_AKE_WMS_FAListe',
                 N'vw_IDEAL_AKE_WMS_Arbeitsgaenge', N'vw_IDEAL_AKE_WMS_Artikel',
                 N'vw_IDEAL-AKE_Kommissionierung_FAInfos', N'vw_IDEAL-AKE_Kommissionierung_FAListe');
-- P1b: als Service-Login, erwartet je 1
SELECT HAS_PERMS_BY_NAME(N'dbo.vw_IDEAL_AKE_WMS_FAListe', 'OBJECT', 'SELECT')   AS FAListe,
       HAS_PERMS_BY_NAME(N'dbo.vw_IDEAL_AKE_WMS_FAInfos', 'OBJECT', 'SELECT')   AS FAInfos,
       HAS_PERMS_BY_NAME(N'dbo.vw_IDEAL_AKE_WMS_Arbeitsgaenge', 'OBJECT', 'SELECT') AS Arbeitsgaenge,
       HAS_PERMS_BY_NAME(N'dbo.vw_IDEAL_AKE_WMS_Artikel', 'OBJECT', 'SELECT')   AS Artikel;
```

**P2 Spaltengleichheit FA (Vorbedingung Teil 1).** Erwartet: **0 Zeilen**. Mindestens alle in
`FaHierarchySql.cs:121-142` gelesenen Spalten muessen gleich heissen und gleich typisiert sein.
```sql
WITH alt AS (SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = N'vw_IDEAL-AKE_Kommissionierung_FAListe'),
     neu AS (SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = N'vw_IDEAL_AKE_WMS_FAListe')
SELECT COALESCE(a.COLUMN_NAME, n.COLUMN_NAME) AS Spalte, a.DATA_TYPE AS Typ_alt, n.DATA_TYPE AS Typ_neu
FROM alt a FULL OUTER JOIN neu n ON n.COLUMN_NAME = a.COLUMN_NAME
WHERE a.COLUMN_NAME IS NULL OR n.COLUMN_NAME IS NULL OR a.DATA_TYPE <> n.DATA_TYPE;
-- dasselbe mit ..._FAInfos
-- Inhalt (erwartet: gleiche Zahl, EXCEPT in beide Richtungen 0 Zeilen; Abweichungen in Artnr = Hinweis auf H2):
SELECT COUNT_BIG(*) FROM [vw_IDEAL-AKE_Kommissionierung_FAListe];  SELECT COUNT_BIG(*) FROM [vw_IDEAL_AKE_WMS_FAListe];
SELECT HauptFA, VaterFA, SubFA, [Position], Artnr FROM [vw_IDEAL-AKE_Kommissionierung_FAListe]
EXCEPT SELECT HauptFA, VaterFA, SubFA, [Position], Artnr FROM [vw_IDEAL_AKE_WMS_FAListe];
SELECT HauptFA, VaterFA, SubFA, [Position], Artnr FROM [vw_IDEAL_AKE_WMS_FAListe]
EXCEPT SELECT HauptFA, VaterFA, SubFA, [Position], Artnr FROM [vw_IDEAL-AKE_Kommissionierung_FAListe];
SELECT HauptFA, Abteilung FROM [vw_IDEAL-AKE_Kommissionierung_FAInfos] EXCEPT SELECT HauptFA, Abteilung FROM [vw_IDEAL_AKE_WMS_FAInfos];
SELECT HauptFA, Abteilung FROM [vw_IDEAL_AKE_WMS_FAInfos] EXCEPT SELECT HauptFA, Abteilung FROM [vw_IDEAL-AKE_Kommissionierung_FAInfos];
```

**P3 Arbeitsgaenge (Vorbedingung Teil 2).** `<M>` ist der Wert von `Sync:ProductionWorkplaceSyncMandant`
(Default 1).
```sql
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = N'vw_IDEAL_AKE_WMS_Arbeitsgaenge'
  AND COLUMN_NAME IN (N'Arbeitsplatznummer', N'Mandant', N'Aktiv', N'Bezeichnung1', N'USER_ArbeitsSchritt');
-- erwartet 5 Zeilen; Bezeichnung1/USER_ArbeitsSchritt Texttyp (Reader GetString, SageArbeitsplatzReader.cs:43-44); Aktiv mit -1 = aktiv
SELECT Arbeitsplatznummer, Bezeichnung1, USER_ArbeitsSchritt FROM KHKPpsArbeitsplaetze WHERE Mandant = <M> AND Aktiv = -1
EXCEPT SELECT Arbeitsplatznummer, Bezeichnung1, USER_ArbeitsSchritt FROM vw_IDEAL_AKE_WMS_Arbeitsgaenge WHERE Mandant = <M> AND Aktiv = -1;
-- und umgekehrt. Erwartet 0 Zeilen; v. a. Arbeitsplatznummer identisch formatiert (sonst Neuanlagen, ProductionWorkplaceSyncService.cs:95).
```

**P4 Artikel (Grundlage Rueckfrage 4).**
```sql
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = N'vw_IDEAL_AKE_WMS_Artikel' ORDER BY ORDINAL_POSITION;
EXEC sp_helptext N'vw_IDEAL_AKE_WMS_Artikel';
SELECT TOP 20 * FROM vw_IDEAL_AKE_WMS_Artikel ORDER BY NEWID();                         -- Stichprobe
-- P4a Dubletten:      SELECT <ArtNr>, COUNT(*) FROM vw_IDEAL_AKE_WMS_Artikel GROUP BY <ArtNr> HAVING COUNT(*) > 1;
-- P4b Gruppenformat:  SELECT TOP 10 <ArtGrp> FROM vw_IDEAL_AKE_WMS_Artikel WHERE <ArtGrp> LIKE N'% - %';   -- erwartet 0 Zeilen (nur Code)
-- P4c Stammabgleich:  SELECT v.<ArtNr> FROM vw_IDEAL_AKE_WMS_Artikel v
--                     WHERE NOT EXISTS (SELECT 1 FROM KHKArtikel a WHERE a.Artikelnummer = v.<ArtNr>);       -- erwartet 0 Zeilen
-- P4d Menge alt/neu:  Ergebnis-Zeilen des heutigen Selects (SageImportService.cs:382-424 1:1 ausfuehren) vs SELECT COUNT(DISTINCT <ArtNr>) FROM vw_...
-- P4e Hauptlagerplatz: <Hlp> muss KHKLagerplaetze.Kurzbezeichnung sein (Match auf StorageLocations.Code, SageImportService.cs:466-478, 528-551)
```

**P5 00er auf Sage (H1 vs H2)**
```sql
SELECT TOP 100 r.Ressourcenummer, COUNT(*) AS Positionen, MAX(CASE WHEN a.Artikelnummer IS NULL THEN 1 ELSE 0 END) AS OhneArtikelstamm
FROM KHKPpsRessourcenPositionen r
LEFT JOIN KHKArtikel a ON a.Artikelnummer = r.Ressourcenummer
WHERE r.Ressourcenummer LIKE N'00%' OR a.Artikelnummer IS NULL
GROUP BY r.Ressourcenummer ORDER BY r.Ressourcenummer;
```

**P6 00er in der WMS-DB IDEAL, vorher/nachher (Testfall, siehe TS)**
```sql
SELECT COUNT(*) AS Anzahl00, SUM(CASE WHEN Description IS NULL THEN 1 ELSE 0 END) AS OhneBezeichnung
FROM dbo.Articles WHERE ArticleNumber LIKE N'00%';
-- Dubletten-Kandidaten 00xxx vs xxx:
SELECT a.Id, a.ArticleNumber, b.Id AS IdKurz, b.ArticleNumber AS NummerKurz
FROM dbo.Articles a
JOIN dbo.Articles b ON b.ArticleNumber = SUBSTRING(a.ArticleNumber, PATINDEX(N'%[^0]%', a.ArticleNumber + N'x'), 100)
WHERE a.ArticleNumber LIKE N'0%' AND b.Id <> a.Id;
-- Referenzen der 00er-Zeilen (Grundlage Rueckfrage 2):
SELECT a.Id, a.ArticleNumber,
  (SELECT COUNT(*) FROM dbo.StockMovements m           WHERE m.ArticleId = a.Id)                 AS Bewegungen,
  (SELECT COUNT(*) FROM dbo.ArticleAttributeValues v   WHERE v.ArticleId = a.Id)                 AS Merkmale,
  (SELECT COUNT(*) FROM dbo.PickingItems p             WHERE p.BomArticleNumber = a.ArticleNumber) AS PickingItems,
  (SELECT COUNT(*) FROM dbo.WarehouseRequisitionItems w WHERE w.ArticleNumber = a.ArticleNumber)   AS Lagerbestellpos,
  (SELECT COUNT(*) FROM dbo.PartRequisitions r         WHERE r.ArticleNumber = a.ArticleNumber)   AS Fehlteile
FROM dbo.Articles a WHERE a.ArticleNumber LIKE N'00%';
```

### Antwort auf die Auftragsfrage „Reicht fuer FAInfos/FAListe ein Einstellungswechsel ohne Deploy?“

**Ja, funktional.** Die Namen bestehen die Whitelist (`FaHierarchySql.cs:28-29`). Die Settings werden
je Lauf ungecacht gelesen (`ServiceSettings.cs:9-13`), und die Maske `/StandortEinstellungen`
(`StandortSettingsCatalog.cs:44-45`, `[RequireAdminAccess]` `StandortEinstellungenController.cs:20`)
schreibt die Werte. Vorbedingung ist P2 = 0 Abweichungen. Der Code-Anteil von Teil 1 ist nur
**Default, Fallback, Existenzpruefung und Doku**. Die Default-Aenderung wirkt **nur auf Neuinstallationen**
(`Program.cs:455-467` seedet nur fehlende Keys). Auf IDEAL und AKE stehen die alten Werte in der DB und
muessen von Hand umgestellt werden. Bei AKE ist das unnoetig, weil `Sync:HierarchicalFaEnabled` dort aus
ist (`SyncWorker.cs:337`).

## Migrations-/SQL-Auswirkungen

- **Keine EF-Migration, kein `SQL/XX_*.sql`, `00_FreshInstall.sql` unberuehrt.** Es gibt keine
  Schema-Aenderung. Die zwei neuen ServiceSettings-Zeilen seedet `Program.cs:453-468` beim Web-Start
  idempotent aus dem Katalog. Neuere Keys stehen auch heute nicht in FreshInstall (z. B.
  `Sync:HierarchicalFaEnabled`, Grep in `SQL/`: nur `Sync:ArticlesEnabled` aus der Altzeit,
  `00_FreshInstall.sql:997`, `25_AddServiceSettings.sql:43-45`).
- **`SQL/AgentJobs/`:** nicht betroffen (02_Import_Artikel ist DEPRECATED und ausdruecklich nicht
  nachzuziehen).
- **Sage-Seite:** Die Views sind Sage-verwaltet und **keine** WMS-Migration. Optional kommen Doku-Dateien
  `SQL/sage-views/vw_IDEAL_AKE_WMS_*.sql` dazu (Spaltenliste aus P2-P4, Muster der beiden vorhandenen
  Dateien, „reine Versionskontroll-Dokumentation“). Das entscheidet der Dev-Lauf, es ist kein AK.
- **Daten:** nicht destruktiv. Der Artikel-Sync bleibt reiner Upsert. Eine moegliche Bereinigung der
  00er-Zeilen ist **nicht** Teil dieser Spec (Rueckfrage 2).

## Audit-Feld-Auswirkungen

Es gibt keine neuen Entitaeten und keine neuen Schreibwege. Die bestehenden Schreibwege setzen die
Audit-Felder unveraendert weiter:
- Artikel-Upsert: `ModifiedAt/By/ByWindows = GETUTCDATE(), 'IDEALAKEWMSService', SYSTEM_USER` bzw.
  `Created*` (`SageImportService.cs:495-497, 515-518`). Das gilt fuer beide Quellen identisch, weil
  der Upsert-Block nicht angefasst wird.
- Werkbank: `Created*`/`Modified*` = `ProductionWorkplaceSync` (`ProductionWorkplaceSyncService.cs:106, 152`).
- `FaHierarchyNodes`/`FaHierarchyOrderInfos`: Full-Refresh-Cache mit `SyncedAt`, unveraendert.
- ServiceSettings-Aenderungen in der Maske: bestehender Weg, unveraendert.

## Rollen / Zugriff / Listen-Views

- Es gibt keine neue Rolle und keinen neuen Filter. Die neuen Keys sind nur ueber `/ServiceSettings`
  (`ServiceSettingsController.cs:16` `[RequireAdminAccess]`) und `/StandortEinstellungen`
  (`StandortEinstellungenController.cs:20` `[RequireAdminAccess]`) pflegbar.
- **Keine neue Tabellen-View**, deshalb fallen keine Pflichten aus dem Listen-View-Pattern an.

## Akzeptanzkriterien

1. `ServiceSettingDefinitions` traegt fuer `Sync:FaHierarchyListeViewName`/`…InfosViewName` die Defaults
   `[vw_IDEAL_AKE_WMS_FAListe]`/`[vw_IDEAL_AKE_WMS_FAInfos]`. Der Fallback in `FaHierarchySyncService`
   liefert bei leerem DB-Wert dieselben Namen. Ein Unit-Test zeigt, dass `ValidateAndQuote` beide Namen
   unveraendert geklammert zurueckgibt.
2. Ein bestehender DB-Wert dieser Keys bleibt nach Web-Start und Deploy **unveraendert** (Manual-UAT: Wert
   vorher/nachher in `/ServiceSettings` vergleichen). README/Betriebshinweis nennt die manuelle Umstellung.
3. FA-Sync bei fehlender FAListe- **oder** FAInfos-View: Es gibt eine Warnzeile „… nicht vorhanden oder keine
   Leseberechtigung — Sync uebersprungen“, der Lauf endet mit Suffix „Quelle nicht vorhanden“, es wird
   keine Exception geworfen, und die Zeilenzahl von `FaHierarchyNodes`/`FaHierarchyOrderInfos` bleibt
   gleich. Die Fehlermail haengt von Rueckfrage 6 ab.
4. FA-Sync bei ungueltigem View-Namen: Das Verhalten bleibt unveraendert wie in TS-57.8 (Fehler, Fehlermail,
   kein SQL).
5. Neuer Key `Sync:ProductionWorkplaceSyncViewName` (String, Default `[vw_IDEAL_AKE_WMS_Arbeitsgaenge]`) steht
   in `ServiceSettingDefinitions.All`, in der Drift-Guard-Liste und in `StandortSettingsCatalog.Fields`. Die
   Beschreibung ist hoechstens 500 Zeichen lang, `All_FieldsFitServiceSettingsColumns` ist gruen.
6. Der Werkbank-Sync liest bei IDEAL aus der konfigurierten Quelle mit **demselben** Filter
   `Mandant = @mandant AND Aktiv = -1` und denselben drei Spalten. Bei P3 = 0 Abweichungen liefert der
   erste Lauf nach Umstellung `neu = 0`, `abweichung = 0` (Manual-UAT).
7. Werkbank-Sync bei fehlender Quelle: Es gibt eine Warnzeile und Skip, an `ProductionWorkplaces` aendert sich
   nichts, es wird keine Exception geworfen. Bei ungueltigem Namen gibt es Fehler und Fehlermail ohne SQL.
   Beides ist unit-getestet ueber gemockten Reader bzw. die Validierung.
8. AKE unberuehrt (Arbeitsgaenge): Bei `ProduktionsauftragHierarchisch = false` laeuft der Werkbank-Sync nicht
   und es gibt keinen Protokoll-Lauf `ProductionWorkplaceSync`. Das Gate in `SyncWorker` ist unveraendert
   (Code-Review plus Manual-UAT auf AKE-Test).
9. Neuer Key `Sync:ArticlesViewName` (String, Default `""`) steht im Katalog, in der Drift-Guard-Liste und in
   der Standort-Maske.
10. Artikel-Sync bei leerem `Sync:ArticlesViewName`: Das ausgefuehrte Sage-SQL ist **zeichengleich** mit dem
    heutigen Select. Ein Unit-Test prueft die reine Auswahlfunktion gegen die Konstante. AKE: Counts
    `gelesen/neu/aktualisiert` im ersten Lauf nach Deploy liegen in der Groessenordnung der Laeufe davor
    (Manual-UAT).
11. Artikel-Sync bei gesetztem Namen: liest aus der View mit der in Rueckfrage 4 festgelegten Abbildung,
    upsertet mit unveraendertem Upsert und unveraenderten Audit-Feldern, Counts-Keys unveraendert
    (`gelesen/neu/aktualisiert/hauptlagerplatz_fehlt`). Ein Unit-Test prueft das erzeugte SQL (geklammerter
    Name, Aliase, `GROUP BY`).
12. Artikel-Sync bei gesetztem Namen und fehlender View: Warnzeile und Skip, **kein** Rueckfall auf den alten
    Select, `Articles` unveraendert. Bei ungueltigem Namen gibt es Fehler und Fehlermail ohne SQL.
13. 00er: P6 wurde auf der IDEAL-WMS-DB **vor** und **nach** dem ersten View-Lauf ausgefuehrt, und das
    Ergebnis steht im TS-Protokoll. Nach der Umstellung entstehen **keine neuen** Zeilen `ArticleNumber
    LIKE '00%'` (Count nachher <= vorher, Dubletten-Abfrage ohne neue Treffer). Die Bereinigung des
    Altbestands haengt an Rueckfrage 2.
14. `dotnet build IdealAkeWms.slnx` und `dotnet test` sind gruen.
15. Doku nachgezogen: `docs/TESTSZENARIEN.md` (neues TS), `secondbrain/tests/testszenarien-index.md`,
    `codebase/services.md` (Key-Tabelle + Reader-Quellen), `codebase/integrationen.md` (Sage-Quellen-Tabelle
    :32/:37), README-Betriebshinweis, Anwender-Changelog, beide `AppVersion.cs`.

## Test-Szenarien

Entwurf fuer `docs/TESTSZENARIEN.md`, **Nummer beim /dev messen** (am Buendel-HEAD ist TS-92 die
hoechste). Arbeitstitel: **TS-9X — IDEAL: Sage-Quellen auf WMS-Views umstellen**. Die Sage-Reads sind nicht
InMemory-testbar, deshalb ist der Grossteil Manual-UAT (fallstricke §8).

**Vorbedingungen:** IDEAL-Testsystem, Master `ProduktionsauftragHierarchisch = true`,
`Sync:HierarchicalFaEnabled = true`, `Sync:ProductionWorkplaceSyncEnabled = true`, `Sync:ArticlesEnabled = true`.
P1 (inkl. P1b) und P2-P4 sind auf der **Sage-Test-DB** ausgefuehrt und das Ergebnis ist protokolliert. Die
Views existieren dort (Rueckfrage 5). DB-Backup der WMS-Test-DB liegt vor. P6 „vorher“ ist ausgefuehrt.

- **TS-9X.1 FA-Views per Einstellung:** In `/StandortEinstellungen` FAListe = `[vw_IDEAL_AKE_WMS_FAListe]`,
  FAInfos = `[vw_IDEAL_AKE_WMS_FAInfos]` setzen und den naechsten Zyklus abwarten. *Erwartet:* Lauf
  `FaHierarchy` erfolgreich, `neu` in derselben Groessenordnung wie im Lauf davor. Stichprobe einer FA in
  `/FaHierarchy` und in der Kommissionierliste unveraendert.
- **TS-9X.2 Bestehender Wert bleibt:** Vor dem Deploy den Wert der FA-Keys notieren, deployen, Web neu
  starten. *Erwartet:* Wert unveraendert (Seed ueberschreibt nicht).
- **TS-9X.3 Arbeitsgaenge aus View:** Nach dem Deploy steht `Sync:ProductionWorkplaceSyncViewName` mit Default
  in `/ServiceSettings`. Zyklus abwarten. *Erwartet:* Lauf `ProductionWorkplaceSync` erfolgreich, `neu = 0`,
  `abweichung = 0` (bei P3 = 0). Werkbank-Liste unveraendert.
- **TS-9X.4 Artikel aus View:** `Sync:ArticlesViewName = [vw_IDEAL_AKE_WMS_Artikel]` setzen und Zyklus
  abwarten. *Erwartet:* Lauf `Article` erfolgreich, `gelesen` entspricht P4d. Stichprobe von 5 Artikeln
  (Bezeichnung, Einheit, Artikelgruppe **als Code**, Hauptlagerplatz, Matchcode) in `/Articles` gegen Sage.
  Danach P6 „nachher“ ausfuehren: keine neuen 00er-Zeilen, keine neuen Dubletten.
- **TS-9X.5 Folgeprozesse:** Eine Kommissionierung an einem HauptFA mit 2 Positionen umbuchen. *Erwartet:* je
  Position eine `StockMovement` (kein stiller Skip, `PickingTransferService`). Eine Lagerbuchung erzeugen.
  *Erwartet:* Sage-Lagerbuchung erfolgreich (`SageBookingQueueItems.Status`). Lagerbestand-Sync ohne neue
  Warnungen „Artikel … nicht im WMS“.
- **Negativ TS-9X.6 Quelle fehlt:** `Sync:ArticlesViewName = [vw_GibtEsNicht]`. *Erwartet:* Lauf `Article` mit
  Warnzeile „… nicht vorhanden oder keine Leseberechtigung — Sync uebersprungen“, Suffix „Quelle nicht
  vorhanden“, `Articles`-Anzahl unveraendert, Service laeuft weiter, andere Syncs im Zyklus laufen. Dasselbe
  mit `Sync:ProductionWorkplaceSyncViewName` und mit dem FAListe-Key (FA-Zieltabellen unveraendert).
- **Negativ TS-9X.7 Ungueltiger Name:** `Sync:ArticlesViewName = vw_x;DROP`. *Erwartet:* Lauf als Fehler,
  Fehlermail (falls `ErrorNotification:Enabled`), kein SQL. Dasselbe fuer den Arbeitsgaenge-Key.
- **Negativ TS-9X.8 Rueckweg ohne Deploy:** `Sync:ArticlesViewName` leeren. *Erwartet:* der naechste Lauf
  nutzt den alten Select (`gelesen` wie vor der Umstellung). Arbeitsgaenge-Key = `KHKPpsArbeitsplaetze`:
  Lauf erfolgreich (nur bei Bestaetigung von Rueckfrage 8).
- **TS-9X.9 AKE unberuehrt (AKE-Testsystem):** nach dem Deploy ohne Einstellungsaenderung. *Erwartet:* Lauf
  `Article` mit Counts wie zuvor, `Sync:ArticlesViewName` leer, kein Lauf `ProductionWorkplaceSync`, kein
  Lauf `FaHierarchy`.
- **Unit (gruen im Build):** `ValidateAndQuote` mit neuen Namen; Artikel-SQL-Auswahl leer gleich Konstante,
  gesetzt gleich View-SQL; `ProductionWorkplaceSyncService` mit Reader-Ergebnis „Quelle fehlt“ ergibt Skip
  ohne Insert/Update; ungueltiger Name ergibt `FinishFailed` ohne Reader-Aufruf; Drift-Guard mit beiden
  neuen Keys.

## Etappen (nur bei epic: true)

Entfaellt (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| - | entfaellt | - | - |

## Deploy

*Vorlaeufig. Der QA-Lauf bestaetigt das gegen den echten Diff.*

- **Web-App:** ja. Der Katalog (`ServiceSettingDefinitions`, `StandortSettingsCatalog`) liegt im Web-Projekt, der
  Seed der neuen Keys laeuft beim **Web**-Start (`Program.cs:453-468`), dazu Changelog und AppVersion.
- **Service:** ja. Reader und Sync-Services.
- **Migration:** nein (kein Schema, keine SQL-Datei, siehe oben).
- **Publish-Befehle** (aus dem Worktree, nach dem Merge aus dem Repo-Root):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

**Reihenfolge IDEAL**
0. Gerald fuehrt P1-P6 aus (live **und** Test-DB). Die GRANTs fuer den Service-Login sind gesetzt (P1b).
1. *(optional, ohne Deploy, Rueckfrage 9)* FA-View-Keys in `/StandortEinstellungen` umstellen, sobald P2 = 0.
2. **Web zuerst** deployen (seedet die neuen Keys: Arbeitsgaenge mit View-Default, Artikel leer), dann den
   **Service**. Wird nur der Service deployt, fehlen die Key-Zeilen, und der Service nimmt den Fallback im
   Code. Das ist funktional gleich, aber in der Oberflaeche unsichtbar.
3. Ab dem ersten Service-Zyklus liest der Werkbank-Sync aus `vw_IDEAL_AKE_WMS_Arbeitsgaenge`. **Die
   Vorbedingung P3 muss vor dem Deploy erfuellt sein.**
4. Artikel: `Sync:ArticlesViewName` erst setzen, nachdem P4/P6-vorher protokolliert sind, dann P6-nachher.

**AKE:** Web und Service deployen, keine Einstellung aendern. `Sync:ArticlesViewName` bleibt leer, der
Werkbank-Sync laeuft dort nicht.

**Rueckweg:** Er laeuft ohne Code-Rollback ueber die Einstellungen (FA-Keys auf die alten Namen,
`Sync:ArticlesViewName` leeren, Arbeitsgaenge-Key auf `KHKPpsArbeitsplaetze`). **Achtung:** Artikel, die im
View-Modus neu angelegt wurden, bleiben stehen (kein Delete). Beim Code-Rollback bleiben die zwei neuen
Key-Zeilen als verwaiste ServiceSettings-Zeilen liegen. Sie sind harmlos.

## Risiken (Kurzliste)

- **Nummernformat (H2):** Liefert eine neue View Artikel- oder Arbeitsplatznummern anders formatiert, entstehen
  Dubletten bzw. neue Werkbaenke, und die String-Match-Stellen (Kommissionier-Umbuchung, Sage-Buchung,
  Bestand-Abgleich) laufen still ins Leere. Abwehr: P3/P4c als **harte Vorbedingung** und TS-9X.5.
- **Bestehende PickingItems:** Aendert die neue FAListe das `Artnr`-Format, behalten bereits initialisierte
  `PickingItems` die alte `BomArticleNumber` und finden den Artikel nicht mehr (`PickingTransferService.cs:173-183`,
  fallstricke §1 „PickingItems ggf. loeschen“). Das ist nur relevant, wenn P2 Abweichungen in `Artnr` zeigt.
- **OBJECT_ID = NULL bei fehlender Berechtigung:** Das wird als „nicht vorhanden“ gemeldet, deshalb steht
  die Berechtigung im Meldetext und P1b in den Vorbedingungen.
- **Namensverwechslung:** Die View heisst `…_Arbeitsgaenge`, liefert aber **Arbeitsplaetze** (Werkbaenke).
  Glossar: Werkbank = Sage-Arbeitsplatz, Arbeitsgang = `WorkOperation`. Das gehoert beim /dev als Hinweis in
  `fallstricke.md` (§16), damit niemand die View fuer die Arbeitsgang-Quelle haelt.

## Offene Rueckfragen

1. **Was genau ist das „Problem der 00er-Artikelnummern“?** Bitte eine Beispielnummer nennen und sagen, wo
   der Fehler sichtbar wird (Artikelliste? Kommissionierung? Sage-Buchung?). Hypothesen: H1 Nicht-Artikel-Ressourcen
   durch LEFT JOIN ohne Typfilter (`SageImportService.cs:392-396`), H2 Formatabweichung derselben Nummer,
   H3 anderes. *Empfehlung:* P5 und P6 laufen lassen, dann ist H1 oder H2 belegt.
2. **Altbestand der 00er-Zeilen in `Articles`:** Der Sync loescht nie (`SageImportService.cs:484-521`), die
   Zeilen bleiben also auch nach der Umstellung sichtbar. *Empfehlung:* In dieser Spec nur messen (P6
   vorher/nachher). Die Bereinigung wird eine **eigene Folgeaufgabe**: ein Einmal-Datenskript nach ADR 0015
   loescht nur Zeilen ohne Referenzen (P6-Referenzabfrage = 0 ueberall), referenzierte Zeilen werden zur
   manuellen Entscheidung gelistet.
3. **Bestaetigung „Articles bleibt“:** Die View ersetzt nur die **Quelle** des Artikel-Syncs, nicht die
   WMS-Tabelle. Begruendung: FKs `StockMovements`, `ArticleAttributeValues`, `FaWorkStepSpecs`
   (`ApplicationDbContext.cs:299-313, 344-351, 590-593`), Hauptlagerplatz-Lock und AKE.
   *Empfehlung:* ja.
4. **Spaltenabbildung der Artikel-View** (Ergebnis P4): Welche View-Spalte liefert Artikelnummer,
   Bezeichnung, Lagermengeneinheit, Artikelgruppe, Meldebestand, Hauptlagerplatz-Kurzbezeichnung und
   Matchcode? Kommt die Artikelgruppe als reiner Code (`940`, Pflicht wegen `OrderRecipientRepository.cs:98/111`)?
   Ist die View je Artikelnummer eindeutig (P4a)? *Empfehlung:* Spaltennamen 1:1 wie in `KHKArtikel` bzw.
   `KHKLagerplaetze.Kurzbezeichnung`, Artikelgruppe nur Code.
5. **Views in der Sage-Test-DB vorhanden?** (P1 dort.) *Empfehlung:* vor der UAT dort anlegen, sonst prueft
   die UAT nur den Skip-Pfad (TS-9X.6).
6. **Fehlende View: Fehlermail ja/nein?** *Empfehlung:* keine Mail, nur Warnzeile plus Suffix wie bei
   FA-Zusatzinfo (kein Mail-Spam alle 15 min auf der Test-DB). Folge: Ein Tippfehler im FA-View-Namen faellt
   dann nur im Aktivitaets-Protokoll auf, heute fuehrt er ueber die `SqlException` zu einer Mail.
7. **Standort-Schaltung Artikel:** ein eigener String-Key `Sync:ArticlesViewName` (leer = alter Select)
   statt einer Kopplung an den Master `ProduktionsauftragHierarchisch`? *Empfehlung:* eigener Key. Er ist
   explizit, ohne Deploy umkehrbar und trennt die Artikel-Quelle von der FA-Hierarchie.
8. **Arbeitsgaenge-Key:** Default `[vw_IDEAL_AKE_WMS_Arbeitsgaenge]`, und Rueckweg ohne Deploy durch
   `KHKPpsArbeitsplaetze` als Wert (Existenzpruefung ohne Typfilter, Tabelle oder View erlaubt)?
   *Empfehlung:* ja.
9. **FA-Views schon vor dem Deploy umstellen?** Das geht ohne Code, sobald P2 = 0. *Empfehlung:* ja, aber erst
   nach Klaerung von Rueckfrage 1, falls die neue FAListe die `Artnr` anders schreibt (siehe Risiko
   „Bestehende PickingItems“).

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. 00er-Problem (Beispielnummer, wo sichtbar, Ergebnis P5/P6) →
2. Altbestand 00er: nur messen, Bereinigung als Folgeaufgabe? →
3. Articles bleibt, View ersetzt nur die Sync-Quelle? →
4. Spaltenabbildung Artikel-View (Ergebnis P4, Artikelgruppe nur Code?) →
5. Views in der Sage-Test-DB vorhanden / werden angelegt? →
6. Fehlende View: Fehlermail ja/nein? →
7. Artikel-Schaltung ueber eigenen Key Sync:ArticlesViewName? →
8. Arbeitsgaenge-Default View + Rueckweg per KHKPpsArbeitsplaetze? →
9. FA-Views vor dem Deploy per Einstellung umstellen? →
