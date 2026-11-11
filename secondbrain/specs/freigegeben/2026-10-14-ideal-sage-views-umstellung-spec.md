---
type: spec
title: "IDEAL: Sage-Zugriffe auf die neuen WMS-Views umstellen (FAInfos, FAListe, Arbeitsgaenge, Artikel)"
slug: 2026-10-14-ideal-sage-views-umstellung-spec
status: Testbereit
created: 2026-10-14
updated: 2026-10-15
source_backlog: "[[2026-10-14-ideal-sage-views-umstellung]]"
task: "[[2026-10-15-ideal-sage-views-umstellung-umsetzung]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - IdealAkeWms/Models/ServiceSettingDefinitions.cs (Defaults FAListe/FAInfos :38-39, zwei neue Keys; Arbeitsgaenge-Default KHKPpsArbeitsplaetze)
  - IdealAkeWms/Models/Standort/StandortSettingsCatalog.cs (:42-59, neue Keys in die Maske)
  - IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs (Drift-Guard-Liste :81-121)
  - IDEALAKEWMSService/Services/FaHierarchySyncService.cs (Fallback :51-56, Existenzpruefung vor :88, fehlende View = Fehler + Mail)
  - IDEALAKEWMSService/Services/FaHierarchySql.cs (ValidateAndQuote wiederverwenden, ggf. Existenz-SQL)
  - IDEALAKEWMSService/Services/SageArbeitsplatzReader.cs (:24-29 Quelle konfigurierbar, Fallback KHKPpsArbeitsplaetze)
  - IDEALAKEWMSService/Services/ISageArbeitsplatzReader.cs (Signatur/Rueckgabe "Quelle fehlt")
  - IDEALAKEWMSService/Services/ProductionWorkplaceSyncService.cs (:48-64 Key lesen, Skip-Pfad ohne Mail)
  - IDEALAKEWMSService/Services/SageImportService.cs (SyncArticlesAsync :369-450 Quelle umschaltbar, Skip-Pfad ohne Mail)
  - IDEALAKEWMSService.Tests (FaHierarchySqlTests, ProductionWorkplaceSyncServiceTests, neuer SQL-Builder-Test Artikel)
  - SQL/Einmalig/<Datum>_IDEAL-Sage-Views_Bereinigung00erArtikel.sql (neu, Einmal-Datenskript nach ADR 0015, daten-destruktiv, nur IDEAL)
  - docs/TESTSZENARIEN.md, README.md (Betriebshinweis), Views/Help/Changelog.cshtml, AppVersion.cs (Web + Service)
open_questions: []
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: "Freigegeben wie Empfehlungen: 1 00er loeschen, 2 Bereinigung in dieser Spec (Einmal-Skript, nur unreferenzierte Zeilen), 3 Articles bleibt (View = nur Sync-Quelle), 4 Spalten wie heute (P4 Pflicht), 5 Views in Sage-Test-DB vorhanden, 6 FA-View fehlt = Fehler+Mail / Artikel+Arbeitsplatz = nur Warnung, 7 eigener Key Sync:ArticlesViewName, 8 Default KHKPpsArbeitsplaetze, Umstellung nach P3, 9 FA-Views vorab per Einstellung nach P2 (inkl. Artnr = 0), IDEAL-Test erst nach den Abnahmelaeufen. Umsetzung nach dem Buendel-Merge ab main."
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-10-15"
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
Die Artikel-View soll ausserdem die „00er-Artikelnummern“ abstellen. Laut Gerald (Antwort 1,
2026-10-15) koennen die 00er geloescht werden: Die View liefert sie nicht mehr, und der Altbestand in
`Articles` wird mit einem Einmal-Datenskript bereinigt, soweit er nicht referenziert ist.
AKE hat diese Views nicht und muss **unveraendert** bleiben.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. **FAInfos/FAListe:** Default und Fallback der View-Namen auf `[vw_IDEAL_AKE_WMS_FAListe]` /
   `[vw_IDEAL_AKE_WMS_FAInfos]` umstellen. Betriebshinweis, dass bestehende DB-Werte von Hand umgestellt
   werden muessen.
2. **Arbeitsgaenge:** Der Werkbank-Sync (`SageArbeitsplatzReader`) liest aus einer per Einstellung
   benannten Quelle. Default und Code-Fallback bleiben die heutige Tabelle `KHKPpsArbeitsplaetze`
   (Rueckfrage 8, entschieden 2026-10-15). Gerald stellt nach P3b auf `[vw_IDEAL_AKE_WMS_Arbeitsgaenge]` um.
3. **Artikel:** Die Quelle des Artikel-Syncs wird je Standort umschaltbar: leer = bisheriger Select
   (AKE), gesetzt = View (IDEAL). Die WMS-Tabelle `Articles` bleibt (Rueckfrage 3, offen, Empfehlung ja).
4. **00er-Artikelnummern:** Pruefabfragen vorher/nachher, Testfall **und** Bereinigung des Altbestands
   durch ein Einmal-Datenskript nach ADR 0015 (Rueckfrage 2, entschieden 2026-10-15; Abschnitt
   „Bereinigung der 00er-Artikel“).
5. **Fehlende Quelle:** Alle drei Syncs pruefen vor dem Lesen, ob die Quelle existiert (Rueckfrage 6,
   entschieden 2026-10-15):
   - **FA-View fehlt:** Fehler im Aktivitaets-Protokoll, Lauf als fehlgeschlagen, **Fehlermail** wie heute.
     Ein Ausfall der FA-Struktur darf nicht still sein. Die FA-Zieltabellen bleiben unberuehrt.
   - **Artikel- oder Arbeitsplatz-Quelle fehlt:** nur Warnung im Aktivitaets-Protokoll, Lauf
     uebersprungen, **keine** Mail, keine Mutation.

**Out-of-Scope (belegt, siehe Fundstellen-Tabelle unten)**
- `SageBestandReader` (Bestand aus `KHKArtikel`/`KHKLagerplatzbestaende`) und alle anderen
  Sage-Lesestellen, die keine Artikel-Stammdaten lesen.
- AKE-Views `vw_AKE_Kommissionierung_WAListe` / `_StuecklistenDB` und der DEPRECATED-AgentJob
  `SQL/AgentJobs/02_Import_Artikel.sql` (Kopf :1-11: „Bei Aenderungen am Service-Sync bitte hier NICHT
  nachziehen“).
- ~~Die Bereinigung bestehender 00er-Artikel (Folgeaufgabe, Rueckfrage 2).~~ *ueberholt 2026-10-15:
  Bereinigung ist jetzt In-Scope (Punkt 4).*
- 00er-Zeilen, die noch referenziert sind: Das Skript listet sie nur. Was mit ihnen geschieht,
  entscheidet Gerald anhand der Liste (nicht Teil dieser Spec).
- `vw_IDEAL_AKE_WMS_FAZusatzinformationen` (`SageZusatzinfoReader.cs:33,50`): ist schon eine WMS-View
  und bleibt, wie sie ist.

**Groesse:** mittel. Drei kleine, gleich gebaute Eingriffe im Service (Quelle konfigurierbar machen und
Existenz pruefen), zwei Katalog-Keys, ein Einmal-Datenskript, Doku. Ungefaehr 11 bis 13 Dateien, keine
EF-Migration und keine neue Oberflaeche. **Kein Split- und kein Epic-Kandidat:** Die Teile teilen sich
dasselbe Muster (`ValidateAndQuote` + Existenzpruefung) und denselben Deploy. Ein eigener Merge je Teil
wuerde nur den Deploy vervielfachen. Teil 1 (FA-Views) braucht ohnehin keinen Code, um wirksam zu werden
(siehe unten). Das Datenskript wird vom Dev-Lauf nur angelegt, nie ausgefuehrt (ADR 0015); es ist ein
Deploy-Schritt des Menschen. ~~Falls Rueckfrage 2 eine Bereinigung verlangt, wird sie eine **eigene**
Folge-Spec.~~ *ueberholt 2026-10-15.*

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

> [!note] Stand 2026-10-15
> Rueckfrage 1 ist beantwortet: „die 00er koennen wir loeschen“, ohne Beispielnummer. Welche Hypothese
> zutrifft, bleibt unverifiziert. Fuer die Bereinigung reicht die Abgrenzung ueber P5b/P6 plus Geralds
> Bestaetigung der Kandidatenliste (Abschnitt „Bereinigung der 00er-Artikel“). Die Hypothesen bleiben
> als Erklaerung stehen, warum die View die 00er nicht mehr liefern soll.

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

### Freigabe-Nachtrag 2026-10-15 (geht vor, Gerald)

Freigegeben wie die Empfehlungen; alle neun Antworten stehen im Freigabe-Block. Kurz: Articles bleibt (View nur
Sync-Quelle, eigener Key `Sync:ArticlesViewName`, leer = alter Select); Arbeitsplaetze-Default `KHKPpsArbeitsplaetze`,
Umstellung durch Gerald nach P3; fehlende FA-View = Fehler + Mail, fehlende Artikel-/Arbeitsplatz-Quelle = nur Warnung;
00er-Bereinigung per Einmal-Skript in dieser Spec (nur unreferenzierte Zeilen, Ablauf und Kopf-Wortlaut siehe Skript-Aufbau);
FA-Views vorab per Einstellung nach P2 (inkl. `Artnr`). ~~Umsetzung nach dem Buendel-Merge auf neuem Zweig ab `main`.~~ **Geaendert 2026-10-15 (Gerald: „bitte die dev"): Umsetzung
direkt im Buendel** (stehende Regel 2026-10-09). Zeilennummern gegen den Buendel-HEAD messen; `SQL/Einmalig/` im Buendel neu anlegen.

## Technischer Loesungsentwurf

Leitlinie (ponytail): **ein** Muster fuer alle drei Quellen, ausschliesslich aus vorhandenen Bausteinen.

1. **Name validieren:** `FaHierarchySql.ValidateAndQuote` (`FaHierarchySql.cs:41`) wird unveraendert
   wiederverwendet. Ein ungueltiger Name fuehrt wie heute bei FA (`FaHierarchySyncService.cs:64-76`) zu
   Protokoll-Fehler, `FinishFailedAsync` und Fehlermail, ohne dass SQL ausgefuehrt wird. Der Name der
   Exception (`FaHierarchyViewNameException`) wird nicht umbenannt.
2. **Existenz pruefen:** ein kleiner gemeinsamer Helfer `SELECT OBJECT_ID(@name)` mit dem **geklammerten**
   Namen als Parameter. Er laeuft **ohne** Typfilter `'V'`, damit eine Tabelle als Rueckweg-Quelle zulaessig
   bleibt (Rueckfrage 8: Default ist die Tabelle `KHKPpsArbeitsplaetze`). Was bei `NULL` passiert, ist je
   Quelle verschieden (Rueckfrage 6, entschieden 2026-10-15):
   - **Artikel und Arbeitsplaetze:** Warnzeile
     `"Sage-Quelle <name> nicht vorhanden oder keine Leseberechtigung — Sync uebersprungen."`, dann
     `FinishSuccessAsync` mit den ueblichen Counts = 0 und `messageSuffix` „Quelle nicht vorhanden“ (Muster
     `FaZusatzinfoSyncService.cs:62-75`). Kein throw, keine Mutation, **keine Mail**.
   - **FA (FAListe/FAInfos):** siehe Punkt 3, Fehler mit Mail.
   *Warum der Text „oder keine Leseberechtigung“:* `OBJECT_ID` liefert in SQL Server auch dann `NULL`, wenn der
   Login keine Metadaten-Sicht auf das Objekt hat. Eine fehlende `GRANT SELECT` auf eine neue View sieht also
   genauso aus wie eine fehlende View (siehe P1b).
3. **FA (Teil 1):** Default (`ServiceSettingDefinitions.cs:38-39`) und Fallback (`FaHierarchySyncService.cs:53, 56`)
   werden auf `[vw_IDEAL_AKE_WMS_FAListe]` / `[vw_IDEAL_AKE_WMS_FAInfos]` gesetzt. Die Existenzpruefung (2)
   fuer **beide** Views laeuft vor dem Empty-Guard (`:88-89`). Fehlt eine, gibt es eine Fehlerzeile
   `"Sage-Quelle <name> nicht vorhanden oder keine Leseberechtigung — FA-Struktur-Sync abgebrochen."`,
   `FinishFailedAsync` und einen throw. Die Fehlermail entsteht ueber den bestehenden Weg
   `RunResilientAsync` → `NotifyErrorAsync` (`SyncWorker.cs:339, 481, 495-501`), also wie heute bei der
   `SqlException`, nur mit klarer Meldung. Die Zieltabellen `FaHierarchyNodes`/`FaHierarchyOrderInfos`
   bleiben unberuehrt, weil der Abbruch vor jedem Schreibzugriff liegt. In `FaHierarchySqlTests` kommt
   ein `InlineData` mit den neuen Namen dazu. Die alten Testdaten bleiben (sie pruefen den Bindestrich-Fall).
4. **Arbeitsgaenge (Teil 2):** neuer Key `Sync:ProductionWorkplaceSyncViewName` (String, Default
   `KHKPpsArbeitsplaetze`, Kategorie `FA-Hierarchie`; Rueckfrage 8, entschieden 2026-10-15). Warum nicht
   die View als Default: `Program.cs:453-467` legt einen neuen Key beim Web-Start mit seinem Default in jeder
   bestehenden DB an; ein View-Default wuerde IDEAL beim Deploy umschalten, bevor P3 belegt ist. Gerald
   stellt nach P3b von Hand auf `[vw_IDEAL_AKE_WMS_Arbeitsgaenge]` um. Ihn liest `ProductionWorkplaceSyncService`
   (neben `:48-51`, `GetValueSafeAsync`). Leer bedeutet Fallback auf denselben Default `KHKPpsArbeitsplaetze`
   (Code-Konstante = Katalog-Default, der Drift-Guard prueft den Katalog). `ValidateAndQuote` macht daraus
   `[KHKPpsArbeitsplaetze]`; die Whitelist laesst den Namen durch (`FaHierarchySql.cs:28-29, 100-106`). Der
   Lauf mit Default liest damit dieselbe Tabelle wie heute (`SageArbeitsplatzReader.cs:24-29`, dort
   unqualifiziert). Der Service validiert,
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
     Die Platzhalter `<…>` werden mit den Spaltennamen aus dem **P4-Ergebnis** gefuellt (die View-Definition
     liegt nicht im Repo). Antwort 4 (2026-10-15): Die Spalten „sollten gleich sein“ wie heute, Artikelgruppe
     als Code. Das ist **unverifiziert**; P4 (inkl. P4b Gruppenformat, P4c Stammabgleich) bleibt Pflicht vor
     dem Umstellen. **Ueberholt 2026-10-15 (P4, Gerald, Fix v1.60.1):** Die View liefert bereits die
     **WMS-Namen** `ArticleNumber, Description, Matchcode, Unit, ArticleGroup, ReorderLevel,
     PrimaryStorageLocation` (alle nvarchar); `BuildViewSql` liest genau diese. Die folgende Annahme „Spaltennamen
     des heutigen Selects“ war falsch (Lauf scheiterte mit „Ungueltiger Spaltenname“). Offen fuer Gerald bleiben
     **P4a** (Dubletten je `ArticleNumber`), **P4b** (Artikelgruppe als reiner Code), **P4c** (Nummern 1:1 in
     `KHKArtikel`) und **P4e** (Hauptlagerplatz = `KHKLagerplaetze.Kurzbezeichnung`). ~~Liegt beim Start des Dev-Laufs keine P4-Spaltenliste vor, baut der Dev-Lauf den Zweig mit
     den Spaltennamen des heutigen Selects (`Artikelnummer`, `Bezeichnung1`, `Lagermengeneinheit`,
     `Artikelgruppe`, `Meldebestand`, `Kurzbezeichnung`, `Matchcode`, `SageImportService.cs:401-407, 389-390`)
     und vermerkt das als **unverifiziert** im Spec-Nachweis; TS-9X.4 deckt eine Abweichung auf (Lauf
     scheitert an fehlender Spalte, Fehlermail ueber den Fehlerpfad).~~ `GROUP BY`/`MAX` bleibt als Schutz gegen Dubletten in der View, sonst wuerde der Upsert dieselbe
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
7. **Bereinigung der 00er-Artikel:** Einmal-Datenskript, siehe naechster Abschnitt.

### Bereinigung der 00er-Artikel (Einmal-Datenskript, daten-destruktiv, nur IDEAL)

Entschieden 2026-10-15 (Antwort 1 „koennen wir loeschen“, Rueckfrage 2 per Koordination; Gerald bestaetigt
mit der Freigabe). Es gibt keinen Loeschweg fuer `Articles` im Code (Ist-Stand-Tabelle, Zeile „Artikel-Upsert“),
deshalb ein Datenskript und keine Code-Aenderung.

**Ablage und Form (ADR 0015):** `SQL/Einmalig/<YYYY-MM-DD>_IDEAL-Sage-Views_Bereinigung00erArtikel.sql`
(Datum = Anlagetag im Dev-Lauf). Keine Nummer, kein `__EFMigrationsHistory`-Eintrag, kein Eintrag in
`00_FreshInstall.sql`, keine EF-Migration. Vorlage ist das vorhandene
`SQL/Einmalig/2026-09-28_Hotfix-1.30.1_ResetAutosaveZeroQuantityPicked.sql` (liegt auf `main`, am Buendel-HEAD
noch nicht vorhanden; die Umsetzung startet ab `main` nach dem Buendel-Merge). Der Dev-Lauf legt das Skript an
und fuehrt es **nie** aus.

**Definition „00er-Kandidat“ (Annahme, Bestaetigung durch Gerald ueber die Liste):**
`Articles.ArticleNumber LIKE N'00%'` **und** die Nummer steht **nicht** in der Ergebnisliste von P5b (00er, die
`vw_IDEAL_AKE_WMS_Artikel` noch liefert). Die WMS-DB kann die Sage-View nicht direkt lesen (der Service nutzt
eine eigene `SageConnection`, `SageImportService.cs:374`; Datenbankname der IDEAL-Sage-DB im Repo nicht
belegt), deshalb traegt Gerald das P5b-Ergebnis von Hand in die Tabellenvariable `@Behalten` ein
(erwartet: leer). Das Skript listet die Kandidaten; **Gerald bestaetigt die Liste, bevor geloescht wird**.

**Referenzen: eine Zeile gilt als referenziert, sobald EINE der folgenden Stellen trifft** (dann nur auflisten,
nie loeschen):

| Referenz | Beleg | Verhalten beim Loeschen ohne Pruefung |
|---|---|---|
| `StockMovements.ArticleId` (FK) | `ApplicationDbContext.cs:348-351`, Snapshot `:4190-4194`, `00_FreshInstall.sql:192` | **Restrict**: DELETE scheitert. Deckt auch Bestand (Summe der Bewegungen), Sage-Lagerbuchung (`SageBookingQueueItems.StockMovementId`, `SageBookingQueueItem.cs:15`; Payload liest `article.ArticleNumber`, `SageLagerbuchungPayloadBuilder.cs:37`) und die Korrekturen des Bestandsabgleichs ab |
| `ArticleAttributeValues.ArticleId` (FK) | `ApplicationDbContext.cs:311-314`, Snapshot `:3754-3758`, `00_FreshInstall.sql:1382` | **Cascade**: Merkmale wuerden still mitgeloescht |
| `FaWorkStepSpecs.ArticleId` (FK, nullable) | `ApplicationDbContext.cs:590-593`, Snapshot `:3998-4001`, `00_FreshInstall.sql:520` | **SetNull**: Vorbau-Spezifikation verloere still den Artikel |
| `PickingItems.BomArticleNumber` (Text) | `PickingItem.cs:16`; Match in `PickingTransferService.cs:167-183` | Kommissionier-Umbuchung findet den Artikel nicht mehr und ueberspringt still (fallstricke §1) |
| `WarehouseRequisitionItems.ArticleNumber` (Text) | `WarehouseRequisitionItem.cs:12`; `WarehouseRequisitionRepository.cs:124, 458` | Lagerbestellposition ohne Stammsatz |
| `PartRequisitions.ArticleNumber` (Text) | `PartRequisition.cs:11`; `PartRequisitionRepository.cs:53, 62, 85` | Fehlteilmeldung ohne Stammsatz |
| `ProductionOrders.ArticleNumber` (Text) | `ProductionOrder.cs:65`, `00_FreshInstall.sql:278` | FA-Artikel ohne Stammsatz (Matchcode-Anzeige, `ArticleRepository.cs:81-89`) |
| `OseonProductionOrders.ArticleNumber` (Text, AKE) | `OseonProductionOrder.cs:19`, `00_FreshInstall.sql:812` | an IDEAL erwartet leer; konservativ mitgeprueft |
| `BeschichtungsauftragPositionen.Artnr` / `.HauptArtnr` (Text) | `BeschichtungsauftragPosition.cs:22-23`, `00_FreshInstall.sql:2216-2217` | Beschichtungsposition ohne Stammsatz |
| `FaHierarchyNodes.Artnr` / `.HauptArtnr` (Sage-Spiegel) | `FaHierarchyNode.cs:30-31`, `00_FreshInstall.sql:1787-1788` | Cache, aber ein Treffer heisst: Sage nutzt die Nummer in einer aktuellen FA-Struktur, also nicht loeschen |
| `CachedBomItems.Ressourcenummer`, `CachedBomHeaders.Artikelnummer` (Sage-Spiegel, AKE) | `CachedBomItem.cs:23`, `CachedBomHeader.cs:17`, `00_FreshInstall.sql:1743, 1761` | wie oben, konservativ |

Vollstaendigkeit belegt durch: (1) alle `HasOne("IdealAkeWms.Models.Article"…)` im Model-Snapshot (genau die drei
FKs oben) und alle `REFERENCES [dbo].[Articles]` in `00_FreshInstall.sql` (dieselben drei, `:192, :520, :1382`);
(2) alle Entity-Properties `*ArticleNumber*`, `Artnr`, `Artikelnummer`, `Ressourcenummer` in `IdealAkeWms/Models`
ohne ViewModels (Grep; Treffer = Tabelle oben) und alle gleichnamigen Spalten in `SQL/00_FreshInstall.sql`.
`Articles` selbst hat ausgehende FKs (`ArticleCategoryId`, `PrimaryStorageLocationId`,
`ApplicationDbContext.cs:237-248`), die beim Loeschen keine Rolle spielen. Der DUMMY-Artikel (`Article.cs:12`)
faellt nicht unter `00%`. Unverifiziert: ob es auf der IDEAL-DB Tabellen ausserhalb des EF-Modells gibt, die
Artikelnummern halten (P7 prueft das).

**Skript-Aufbau (Skizze, Dev-Lauf formuliert aus):**
1. Kopf nach ADR 0015: „NICHT ERNEUT AUSFUEHREN“ mit diesem Grund (Wortlaut, Freigabe 2026-10-15): „Die
   Ausnahmeliste (Artikel, die die View noch liefert) ist von Hand aus P5b eingetragen und ein Stand vom Tag der
   Bestaetigung. Ein spaeterer zweiter Lauf wuerde mit dieser veralteten Liste 00er-Artikel loeschen, die die View
   inzwischen wieder liefert oder die seither neu angelegt wurden — ungeprueft. Deshalb bricht das Skript ab, sobald
   die Sicherungstabelle existiert; ein neuer Lauf braucht eine neue Liste und eine neue Bestaetigung." Reihenfolge im Kopf (siehe
   Deploy). Kennzeichnung **daten-destruktiv, DB-Backup vor dem Lauf**.
2. `SET NOCOUNT ON; SET XACT_ABORT ON;` Schalter `DECLARE @Loeschen bit = 0;` (0 = nur auflisten, 1 = loeschen)
   und `DECLARE @Behalten TABLE (ArticleNumber nvarchar(100) PRIMARY KEY);` mit dem P5b-Ergebnis.
3. **Guards**, je mit `RAISERROR(..., 16, 1)` und Abbruch, ohne `GO` dazwischen:
   - nur IDEAL: `ServiceSettings` `[Key] = N'ProduktionsauftragHierarchisch'` hat `[Value] = 'true'`
     (Vergleich ohne Gross/Klein, wie `ServiceSettings.cs:35`);
   - Reihenfolge: `Sync:ArticlesViewName` ist nicht leer (sonst holt der alte Select die 00er zurueck);
   - bei `@Loeschen = 1`: die Sicherungstabelle `dbo.Articles_Bereinigung00er_Sicherung` existiert noch nicht
     (`OBJECT_ID`-Guard; verhindert Doppellauf und ueberschriebene Sicherung).
   `ServiceSettings` hat keinen Aenderungszeitpunkt (`00_FreshInstall.sql:981-987`); dass nach dem Setzen ein
   Artikel-Sync erfolgreich lief, prueft deshalb der Mensch im Aktivitaets-Protokoll (Deploy-Schritt), nicht das Skript.
4. Kandidaten nach `#Kandidaten` mit je einer Zaehlspalte pro Referenz der Tabelle oben und einer Summe
   `Referenzen`. Ausgabe: (a) Zaehlzeile `Kandidaten / davon referenziert / davon loeschbar`, (b) Liste der
   **referenzierten** Zeilen mit allen Zaehlspalten, (c) Liste der **loeschbaren** Zeilen (`Id, ArticleNumber,
   Description, CreatedAt`). Bei `@Loeschen = 0` endet das Skript hier.
5. Bei `@Loeschen = 1` in **einer** Transaktion: Referenzen erneut zaehlen (Zeitfenster zwischen Liste und
   Loeschen), `SELECT a.*, SYSDATETIME() AS GesichertAm, SYSTEM_USER AS GesichertVon INTO
   dbo.Articles_Bereinigung00er_Sicherung` fuer die loeschbaren Zeilen, `DELETE` genau dieser Ids,
   `SELECT @@ROWCOUNT AS Geloescht` und Abgleich mit der Zeilenzahl der Sicherung (ungleich → `ROLLBACK` + Fehler).
6. **Rueckweg** als auskommentierter Block am Ende: `SET IDENTITY_INSERT dbo.Articles ON; INSERT INTO
   dbo.Articles (<alle Spalten>) SELECT <alle Spalten> FROM dbo.Articles_Bereinigung00er_Sicherung;
   SET IDENTITY_INSERT dbo.Articles OFF;` Zusaetzlich das DB-Backup vor dem Lauf. Die Sicherungstabelle wird erst
   nach der Abnahme von Hand entfernt (`DROP TABLE`, eigener Deploy-Protokoll-Eintrag); sie ist kein Teil des
   Schemas und kommt nicht nach FreshInstall.

Audit: Ein DELETE hat keine `Modified*`-Felder. Den Nachweis tragen die Sicherungstabelle (`GesichertAm`,
`GesichertVon`) und der Deploy-Protokoll-Eintrag je System (System, Datum, Zaehlergebnis, `Geloescht`), wie
ADR 0015 ihn verlangt.

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
> [!warning] Ueberholt 2026-10-15 (v1.60.2): P3 oben setzt Spalten voraus, die die View nicht hat
> Laut `sys.columns` (Gerald) liefert `vw_IDEAL_AKE_WMS_Arbeitsgaenge` nur `Arbeitsgangnummer`, `Bezeichnung`, `AG` (varchar),
> **kein** `Mandant`/`Aktiv`. Gueltig ist **P3b** (Pflicht vor dem Umstellen):

**P3b Arbeitsplaetze View vs. Tabelle (Pflicht vor dem Umstellen, erwartet in beide Richtungen 0 Zeilen)**
```sql
SELECT CAST(Arbeitsplatznummer AS varchar(31)) AS Nr, Bezeichnung1 AS Bez, USER_ArbeitsSchritt AS AG
FROM KHKPpsArbeitsplaetze WHERE Mandant = <M> AND Aktiv = -1
EXCEPT
SELECT CAST(Arbeitsgangnummer AS varchar(31)), Bezeichnung, AG FROM vw_IDEAL_AKE_WMS_Arbeitsgaenge;

SELECT CAST(Arbeitsgangnummer AS varchar(31)), Bezeichnung, AG FROM vw_IDEAL_AKE_WMS_Arbeitsgaenge
EXCEPT
SELECT CAST(Arbeitsplatznummer AS varchar(31)), Bezeichnung1, USER_ArbeitsSchritt
FROM KHKPpsArbeitsplaetze WHERE Mandant = <M> AND Aktiv = -1;
```
Leer in beide Richtungen belegt die Annahme, dass die View genau die aktiven Arbeitsplaetze des Mandanten liefert, mit
gleich formatierter Nummer.

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

**P5b 00er, die die neue Artikel-View noch liefert (Grundlage `@Behalten` im Bereinigungsskript, erwartet 0 Zeilen)**
```sql
SELECT <ArtNr> FROM vw_IDEAL_AKE_WMS_Artikel WHERE <ArtNr> LIKE N'00%' ORDER BY <ArtNr>;
```

**P6 00er in der WMS-DB IDEAL, vorher/nachher (Testfall, siehe TS)** — Messung. Die verbindliche
Referenzpruefung fuer das Loeschen macht das Bereinigungsskript mit der vollstaendigen Referenzliste.
```sql
SELECT COUNT(*) AS Anzahl00, SUM(CASE WHEN Description IS NULL THEN 1 ELSE 0 END) AS OhneBezeichnung
FROM dbo.Articles WHERE ArticleNumber LIKE N'00%';
-- Dubletten-Kandidaten 00xxx vs xxx:
SELECT a.Id, a.ArticleNumber, b.Id AS IdKurz, b.ArticleNumber AS NummerKurz
FROM dbo.Articles a
JOIN dbo.Articles b ON b.ArticleNumber = SUBSTRING(a.ArticleNumber, PATINDEX(N'%[^0]%', a.ArticleNumber + N'x'), 100)
WHERE a.ArticleNumber LIKE N'0%' AND b.Id <> a.Id;
-- Referenzen der 00er-Zeilen (Kurzfassung; vollstaendig im Bereinigungsskript):
SELECT a.Id, a.ArticleNumber,
  (SELECT COUNT(*) FROM dbo.StockMovements m           WHERE m.ArticleId = a.Id)                 AS Bewegungen,
  (SELECT COUNT(*) FROM dbo.ArticleAttributeValues v   WHERE v.ArticleId = a.Id)                 AS Merkmale,
  (SELECT COUNT(*) FROM dbo.PickingItems p             WHERE p.BomArticleNumber = a.ArticleNumber) AS PickingItems,
  (SELECT COUNT(*) FROM dbo.WarehouseRequisitionItems w WHERE w.ArticleNumber = a.ArticleNumber)   AS Lagerbestellpos,
  (SELECT COUNT(*) FROM dbo.PartRequisitions r         WHERE r.ArticleNumber = a.ArticleNumber)   AS Fehlteile
FROM dbo.Articles a WHERE a.ArticleNumber LIKE N'00%';
```

**P7 Referenz-Vollstaendigkeit auf der WMS-DB IDEAL (vor dem Bereinigungslauf)**
```sql
-- erwartet genau 3 Zeilen: StockMovements (NO_ACTION), FaWorkStepSpecs (SET_NULL), ArticleAttributeValues (CASCADE)
SELECT OBJECT_NAME(fk.parent_object_id) AS Tabelle, fk.name, fk.delete_referential_action_desc
FROM sys.foreign_keys fk WHERE fk.referenced_object_id = OBJECT_ID(N'dbo.Articles');
-- erwartet nur Tabellen aus der Referenz-Tabelle der Spec (plus Articles selbst):
SELECT t.name AS Tabelle, c.name AS Spalte
FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id
WHERE c.name LIKE N'%ArticleNumber%' OR c.name LIKE N'%Artnr%' OR c.name LIKE N'%Artikelnummer%'
   OR c.name LIKE N'%Ressourcenummer%' OR c.name = N'ArticleId'
ORDER BY t.name, c.name;
```
Weicht P7 ab, wird die neue Stelle in das Skript aufgenommen, bevor es mit `@Loeschen = 1` laeuft.

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
- **Daten:** Der Artikel-Sync bleibt reiner Upsert (nicht destruktiv). ~~Eine moegliche Bereinigung der
  00er-Zeilen ist **nicht** Teil dieser Spec (Rueckfrage 2).~~ *ueberholt 2026-10-15.* **Daten-destruktiv** ist
  das Einmal-Datenskript `SQL/Einmalig/<Datum>_IDEAL-Sage-Views_Bereinigung00erArtikel.sql` (ADR 0015, keine
  EF-Migration, keine Nummer, kein History-Eintrag, kein FreshInstall-Eintrag). Pflicht: **DB-Backup vor dem
  Lauf**, Sicherungstabelle `dbo.Articles_Bereinigung00er_Sicherung` im selben Lauf, Lauf nur auf IDEAL
  (Guard), Ausfuehrung je System im Deploy-Protokoll. Ein `OBJECT_ID`-Guard gibt es fuer die
  Sicherungstabelle; das Skript ist im Ergebnis wiederholbar (ein zweiter Lauf findet keine neuen loeschbaren
  Kandidaten, solange die View sie nicht liefert), traegt aber trotzdem den ADR-0015-Warnkopf (Grund siehe
  Skript-Aufbau Punkt 1).

## Audit-Feld-Auswirkungen

Es gibt keine neuen Entitaeten und keine neuen Schreibwege. Die bestehenden Schreibwege setzen die
Audit-Felder unveraendert weiter:
- Artikel-Upsert: `ModifiedAt/By/ByWindows = GETUTCDATE(), 'IDEALAKEWMSService', SYSTEM_USER` bzw.
  `Created*` (`SageImportService.cs:495-497, 515-518`). Das gilt fuer beide Quellen identisch, weil
  der Upsert-Block nicht angefasst wird.
- Werkbank: `Created*`/`Modified*` = `ProductionWorkplaceSync` (`ProductionWorkplaceSyncService.cs:106, 152`).
- `FaHierarchyNodes`/`FaHierarchyOrderInfos`: Full-Refresh-Cache mit `SyncedAt`, unveraendert.
- ServiceSettings-Aenderungen in der Maske: bestehender Weg, unveraendert.
- Bereinigungsskript: Es loescht nur, es aktualisiert nichts, deshalb keine `Modified*`-Felder. Nachweis ueber
  die Sicherungstabelle (`GesichertAm`, `GesichertVon = SYSTEM_USER`, alle Original-Spalten inkl. `Created*`/
  `Modified*`) und das Deploy-Protokoll (ADR 0015).

## Rollen / Zugriff / Listen-Views

- Es gibt keine neue Rolle und keinen neuen Filter. Die neuen Keys sind nur ueber `/ServiceSettings`
  (`ServiceSettingsController.cs:16` `[RequireAdminAccess]`) und `/StandortEinstellungen`
  (`StandortEinstellungenController.cs:20` `[RequireAdminAccess]`) pflegbar.
- **Keine neue Tabellen-View**, deshalb fallen keine Pflichten aus dem Listen-View-Pattern an.
- Das Bereinigungsskript fuehrt ein DBA/Gerald direkt auf der DB aus; keine Rolle in der Anwendung betroffen.

## Akzeptanzkriterien

1. `ServiceSettingDefinitions` traegt fuer `Sync:FaHierarchyListeViewName`/`…InfosViewName` die Defaults
   `[vw_IDEAL_AKE_WMS_FAListe]`/`[vw_IDEAL_AKE_WMS_FAInfos]`. Der Fallback in `FaHierarchySyncService`
   liefert bei leerem DB-Wert dieselben Namen. Ein Unit-Test zeigt, dass `ValidateAndQuote` beide Namen
   unveraendert geklammert zurueckgibt.
2. Ein bestehender DB-Wert dieser Keys bleibt nach Web-Start und Deploy **unveraendert** (Manual-UAT: Wert
   vorher/nachher in `/ServiceSettings` vergleichen). README/Betriebshinweis nennt die manuelle Umstellung.
3. FA-Sync bei fehlender FAListe- **oder** FAInfos-View *(geaendert 2026-10-15, Rueckfrage 6)*: Es gibt eine
   Fehlerzeile „Sage-Quelle <name> nicht vorhanden oder keine Leseberechtigung — FA-Struktur-Sync abgebrochen“,
   der Lauf endet als fehlgeschlagen, eine **Fehlermail** geht raus (bei `ErrorNotification:Enabled`), der
   Service laeuft weiter, und die Zeilenzahl von `FaHierarchyNodes`/`FaHierarchyOrderInfos` bleibt gleich.
4. FA-Sync bei ungueltigem View-Namen: Das Verhalten bleibt unveraendert wie in TS-57.8 (Fehler, Fehlermail,
   kein SQL).
5. Neuer Key `Sync:ProductionWorkplaceSyncViewName` (String, Default **`KHKPpsArbeitsplaetze`**, *geaendert
   2026-10-15, Rueckfrage 8*) steht in `ServiceSettingDefinitions.All`, in der Drift-Guard-Liste und in
   `StandortSettingsCatalog.Fields`. Der Code-Fallback bei leerem/fehlendem Wert ist derselbe Name. Die
   Beschreibung ist hoechstens 500 Zeichen lang, `All_FieldsFitServiceSettingsColumns` ist gruen.
6. Der Werkbank-Sync liest bei IDEAL aus der konfigurierten Quelle mit **demselben** Filter
   `Mandant = @mandant AND Aktiv = -1` und denselben drei Spalten. *(geaendert 2026-10-15, v1.60.2: das gilt nur fuer die
   Tabelle `KHKPpsArbeitsplaetze`; jede andere Quelle wird als View mit `Arbeitsgangnummer, Bezeichnung, AG` ohne
   Mandant/Aktiv gelesen, Vorbedingung P3b.)* Nach dem Deploy (Default
   `KHKPpsArbeitsplaetze`) liefert der erste Lauf dieselben Counts wie die Laeufe davor. Nach Geralds
   Umstellung auf `[vw_IDEAL_AKE_WMS_Arbeitsgaenge]` bei P3b = 0 Abweichungen liefert der erste Lauf `neu = 0`,
   `abweichung = 0` (Manual-UAT).
7. Werkbank-Sync bei fehlender Quelle: Es gibt eine Warnzeile und Skip, an `ProductionWorkplaces` aendert sich
   nichts, es wird keine Exception geworfen und **keine** Fehlermail verschickt. Bei ungueltigem Namen gibt es
   Fehler und Fehlermail ohne SQL. Beides ist unit-getestet ueber gemockten Reader bzw. die Validierung.
8. AKE unberuehrt (Arbeitsgaenge): Bei `ProduktionsauftragHierarchisch = false` laeuft der Werkbank-Sync nicht
   und es gibt keinen Protokoll-Lauf `ProductionWorkplaceSync`. Das Gate in `SyncWorker` ist unveraendert
   (Code-Review plus Manual-UAT auf AKE-Test).
9. Neuer Key `Sync:ArticlesViewName` (String, Default `""`) steht im Katalog, in der Drift-Guard-Liste und in
   der Standort-Maske.
10. Artikel-Sync bei leerem `Sync:ArticlesViewName`: Das ausgefuehrte Sage-SQL ist **zeichengleich** mit dem
    heutigen Select. Ein Unit-Test prueft die reine Auswahlfunktion gegen die Konstante. AKE: Counts
    `gelesen/neu/aktualisiert` im ersten Lauf nach Deploy liegen in der Groessenordnung der Laeufe davor
    (Manual-UAT).
11. Artikel-Sync bei gesetztem Namen: liest aus der View mit der Spaltenabbildung aus P4 (*geaendert 2026-10-15,
    v1.60.1:* View-Spalten = WMS-Namen `ArticleNumber … PrimaryStorageLocation`; Artikelgruppe als Code bis P4b offen),
    upsertet mit unveraendertem Upsert und unveraenderten Audit-Feldern, Counts-Keys unveraendert
    (`gelesen/neu/aktualisiert/hauptlagerplatz_fehlt`). Ein Unit-Test prueft das erzeugte SQL (geklammerter
    Name, Aliase, `GROUP BY`).
12. Artikel-Sync bei gesetztem Namen und fehlender View: Warnzeile und Skip, **keine** Fehlermail, **kein**
    Rueckfall auf den alten Select, `Articles` unveraendert. Bei ungueltigem Namen gibt es Fehler und
    Fehlermail ohne SQL.
13. 00er: P6 wurde auf der IDEAL-WMS-DB **vor** und **nach** dem ersten View-Lauf ausgefuehrt, und das
    Ergebnis steht im TS-Protokoll. Nach der Umstellung entstehen **keine neuen** Zeilen `ArticleNumber
    LIKE '00%'` (Count nachher <= vorher, Dubletten-Abfrage ohne neue Treffer). *(geaendert 2026-10-15: Die
    Bereinigung des Altbestands regeln AK 16-19.)*
14. `dotnet build IdealAkeWms.slnx` und `dotnet test` sind gruen.
15. Doku nachgezogen: `docs/TESTSZENARIEN.md` (neues TS), `secondbrain/tests/testszenarien-index.md`,
    `codebase/services.md` (Key-Tabelle + Reader-Quellen), `codebase/integrationen.md` (Sage-Quellen-Tabelle
    :32/:37), README-Betriebshinweis, Anwender-Changelog, beide `AppVersion.cs`.
16. Bereinigungsskript liegt unter `SQL/Einmalig/<Datum>_IDEAL-Sage-Views_Bereinigung00erArtikel.sql` (keine
    Nummer, kein History-Eintrag, kein FreshInstall-Eintrag, keine EF-Migration) und enthaelt: Warnkopf und
    Reihenfolge nach ADR 0015, Kennzeichnung daten-destruktiv + DB-Backup, `@Loeschen = 0` als Vorgabe,
    `@Behalten` fuer das P5b-Ergebnis, die drei Guards (IDEAL-Master, `Sync:ArticlesViewName` gesetzt,
    Sicherungstabelle noch nicht vorhanden), eine Zaehlspalte fuer **jede** Referenz der Referenz-Tabelle, die
    Sicherung per `SELECT … INTO` vor dem `DELETE` in derselben Transaktion, `@@ROWCOUNT`-Ausgabe mit Abgleich
    gegen die Sicherung und den auskommentierten Rueckweg (Code-Review).
17. Lauf mit `@Loeschen = 0` auf dem IDEAL-Testsystem: aendert keine Zeile (`Articles`-Anzahl vorher = nachher)
    und liefert Zaehlzeile, Liste referenziert und Liste loeschbar. Ein Kandidat mit genau **einer** Referenz
    (je Testfall eine andere Referenzart, mindestens `StockMovements`, `ArticleAttributeValues`,
    `FaWorkStepSpecs`, `PickingItems`) steht in der Liste „referenziert“, nicht in „loeschbar“ (Manual-UAT).
18. Lauf mit `@Loeschen = 1` auf dem IDEAL-Testsystem: `Geloescht` = Zeilenzahl der Sicherungstabelle = Zahl
    „loeschbar“ aus Lauf 17; alle referenzierten 00er-Zeilen sind unveraendert vorhanden;
    `ArticleAttributeValues`/`FaWorkStepSpecs` haben dieselbe Zeilenzahl wie vorher; der naechste Artikel-Sync
    legt keine der geloeschten Nummern neu an (P6 „nach Bereinigung“). Ein zweiter Lauf mit `@Loeschen = 1`
    bricht am Sicherungstabellen-Guard ab, ohne etwas zu aendern.
19. Guards: Auf einem System mit `ProduktionsauftragHierarchisch <> true` (AKE-Test) bricht das Skript ab, ohne
    etwas zu aendern; ebenso auf IDEAL-Test bei leerem `Sync:ArticlesViewName`. Der Rueckweg-Block stellt auf dem
    Testsystem die geloeschten Zeilen mit unveraenderter `Id` wieder her (Manual-UAT, danach erneut bereinigen
    oder Testsystem aus Backup).

## Test-Szenarien

Entwurf fuer `docs/TESTSZENARIEN.md`, **Nummer beim /dev messen** (am Buendel-HEAD ist TS-92 die
hoechste). Arbeitstitel: **TS-9X — IDEAL: Sage-Quellen auf WMS-Views umstellen**. Die Sage-Reads sind nicht
InMemory-testbar, deshalb ist der Grossteil Manual-UAT (fallstricke §8).

**Vorbedingungen:** IDEAL-Testsystem, Master `ProduktionsauftragHierarchisch = true`,
`Sync:HierarchicalFaEnabled = true`, `Sync:ProductionWorkplaceSyncEnabled = true`, `Sync:ArticlesEnabled = true`.
P1 (inkl. P1b), P2-P4 und P5b sind auf der **Sage-Test-DB** ausgefuehrt und das Ergebnis ist protokolliert. Die
Views existieren dort (Antwort 5, Gerald 2026-10-15). DB-Backup der WMS-Test-DB liegt vor. P6 „vorher“ und P7
sind ausgefuehrt.

- **TS-9X.1 FA-Views per Einstellung:** In `/StandortEinstellungen` FAListe = `[vw_IDEAL_AKE_WMS_FAListe]`,
  FAInfos = `[vw_IDEAL_AKE_WMS_FAInfos]` setzen und den naechsten Zyklus abwarten. *Erwartet:* Lauf
  `FaHierarchy` erfolgreich, `neu` in derselben Groessenordnung wie im Lauf davor. Stichprobe einer FA in
  `/FaHierarchy` und in der Kommissionierliste unveraendert.
- **TS-9X.2 Bestehender Wert bleibt:** Vor dem Deploy den Wert der FA-Keys notieren, deployen, Web neu
  starten. *Erwartet:* Wert unveraendert (Seed ueberschreibt nicht).
- **TS-9X.3 Arbeitsgaenge aus View:** Nach dem Deploy steht `Sync:ProductionWorkplaceSyncViewName` mit Default
  `KHKPpsArbeitsplaetze` in `/ServiceSettings`. Zyklus abwarten. *Erwartet:* Lauf `ProductionWorkplaceSync`
  erfolgreich mit denselben Counts wie vor dem Deploy. Dann (P3b = 0) den Key auf
  `[vw_IDEAL_AKE_WMS_Arbeitsgaenge]` setzen und Zyklus abwarten. *Erwartet:* Lauf erfolgreich, `neu = 0`,
  `abweichung = 0`. Werkbank-Liste unveraendert.
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
  vorhanden“, **keine** Fehlermail, `Articles`-Anzahl unveraendert, Service laeuft weiter, andere Syncs im
  Zyklus laufen. Dasselbe mit `Sync:ProductionWorkplaceSyncViewName = [vw_GibtEsNicht]` (keine Mail,
  `ProductionWorkplaces` unveraendert).
- **Negativ TS-9X.6b FA-View fehlt:** FAListe-Key = `[vw_GibtEsNicht]` (danach dasselbe mit dem FAInfos-Key).
  *Erwartet:* Lauf `FaHierarchy` **fehlgeschlagen** mit Fehlerzeile „… nicht vorhanden oder keine
  Leseberechtigung — FA-Struktur-Sync abgebrochen“, **Fehlermail** (falls `ErrorNotification:Enabled`),
  Zeilenzahl `FaHierarchyNodes`/`FaHierarchyOrderInfos` unveraendert, Service laeuft weiter. Key danach
  zuruecksetzen.
- **Negativ TS-9X.7 Ungueltiger Name:** `Sync:ArticlesViewName = vw_x;DROP`. *Erwartet:* Lauf als Fehler,
  Fehlermail (falls `ErrorNotification:Enabled`), kein SQL. Dasselbe fuer den Arbeitsgaenge-Key.
- **Negativ TS-9X.8 Rueckweg ohne Deploy:** `Sync:ArticlesViewName` leeren. *Erwartet:* der naechste Lauf
  nutzt den alten Select (`gelesen` wie vor der Umstellung). Arbeitsgaenge-Key zurueck auf
  `KHKPpsArbeitsplaetze`: Lauf erfolgreich, Counts wie vor der Umstellung. **Achtung:** Den Artikel-Rueckweg
  erst **nach** TS-9X.10-12 testen oder danach den View-Namen wieder setzen und einen Sync abwarten, sonst
  holt der alte Select die 00er zurueck.
- **TS-9X.9 AKE unberuehrt (AKE-Testsystem):** nach dem Deploy ohne Einstellungsaenderung. *Erwartet:* Lauf
  `Article` mit Counts wie zuvor, `Sync:ArticlesViewName` leer, kein Lauf `ProductionWorkplaceSync`, kein
  Lauf `FaHierarchy`.
- **TS-9X.10 00er-Bereinigung, Liste (IDEAL-Test):** Vorbedingung TS-9X.4 erfolgreich (View gesetzt, ein
  Artikel-Sync danach erfolgreich im Aktivitaets-Protokoll). Testdaten: vier 00er-Zeilen ohne Sage-Bezug
  auswaehlen bzw. anlegen und je **eine** Referenz geben (eine Bewegung, ein Artikel-Merkmal, eine
  `FaWorkStepSpec`, ein `PickingItem` mit dieser `BomArticleNumber`); `@Behalten` mit dem P5b-Ergebnis fuellen.
  Skript mit `@Loeschen = 0` ausfuehren. *Erwartet:* `Articles`-Anzahl unveraendert; die vier Zeilen stehen
  unter „referenziert“ mit der richtigen Zaehlspalte; Zaehlzeile = P6-Anzahl00 minus `@Behalten`-Treffer.
  Gerald bestaetigt die Liste „loeschbar“ (im Protokoll vermerken).
- **TS-9X.11 00er-Bereinigung, Loeschen (IDEAL-Test):** DB-Backup, dann Skript mit `@Loeschen = 1`. *Erwartet:*
  `Geloescht` = „loeschbar“ aus TS-9X.10 = Zeilen in `dbo.Articles_Bereinigung00er_Sicherung`; die vier
  referenzierten Zeilen und ihre Merkmale/Spezifikation unveraendert; naechster Artikel-Sync legt keine
  geloeschte Nummer neu an (P6 „nach Bereinigung“: nur noch referenzierte 00er). Die Kommissionierung des
  `PickingItem`-Testfalls bucht weiterhin (wie TS-9X.5). Zweiter Lauf mit `@Loeschen = 1`: Abbruch am
  Sicherungstabellen-Guard, keine Aenderung.
- **Negativ TS-9X.12 Guards und Rueckweg:** (a) AKE-Testsystem: Skript ausfuehren. *Erwartet:* Abbruch
  „kein IDEAL-System“, keine Aenderung. (b) IDEAL-Test, `Sync:ArticlesViewName` kurz leeren: Abbruch mit
  Reihenfolge-Hinweis, keine Aenderung; Key sofort wieder setzen (vor dem naechsten Zyklus). (c) Rueckweg-Block
  ausfuehren. *Erwartet:* die geloeschten Zeilen sind mit derselben `Id` wieder da (`COUNT` = Sicherung).
  Danach Sicherungstabelle droppen und TS-9X.11 wiederholen oder das Testsystem aus dem Backup herstellen.
- **Unit (gruen im Build):** `ValidateAndQuote` mit neuen Namen; Artikel-SQL-Auswahl leer gleich Konstante,
  gesetzt gleich View-SQL; `ProductionWorkplaceSyncService` mit Reader-Ergebnis „Quelle fehlt“ ergibt Skip
  ohne Insert/Update und ohne Exception; ungueltiger Name ergibt `FinishFailed` ohne Reader-Aufruf; leerer
  Arbeitsgaenge-Key ergibt Quelle `[KHKPpsArbeitsplaetze]`; Drift-Guard mit beiden neuen Keys und
  Default `KHKPpsArbeitsplaetze`. Das Bereinigungsskript ist nicht unit-testbar (raw SQL, fallstricke §8),
  Nachweis ueber TS-9X.10-12.

## Etappen (nur bei epic: true)

Entfaellt (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| - | entfaellt | - | - |

## Deploy

*Finalisiert im QA-Lauf 2026-10-15 am echten Diff `d4108f1a..2524bb03` (19 Dateien).*

- **Web-App:** ja — Diff unter `IdealAkeWms/` (`ServiceSettingDefinitions.cs`, `StandortSettingsCatalog.cs`,
  `Views/Help/Changelog.cshtml`, `AppVersion.cs`). Der Seed der zwei neuen Keys laeuft beim **Web**-Start
  (`Program.cs:453-468`).
- **Service:** ja — Diff unter `IDEALAKEWMSService/` (`FaHierarchySql.cs`, `FaHierarchySyncService.cs`,
  `ISageArbeitsplatzReader.cs`, `SageArbeitsplatzReader.cs`, `ProductionWorkplaceSyncService.cs`, neu `SageArticleSql.cs`,
  `SageImportService.cs`, `AppVersion.cs`).
- **Migration:** nein — keine Datei unter `*/Migrations/`, `SQL/00_FreshInstall.sql` und `SQL/AgentJobs/` unberuehrt
  (`git diff --stat` leer). **Aber:** daten-destruktives Einmal-Datenskript
  `SQL/Einmalig/2026-10-15_IDEAL-Sage-Views_Bereinigung00erArtikel.sql` (ADR 0015) — **Mensch-Schritt, nur IDEAL**, nie
  vom Agenten ausgefuehrt.
- **Publish-Befehle** (aus dem Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` → Testsystem → Test → Merge):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- **Reihenfolge:** DB-Backup → Web (seedet die Keys) → Service → Einstellungen (Schritte 3/4 unten) → Einmal-Skript
  (Schritte 5-9 unten). Kein DB-Update/keine Migration.
- Nach dem Merge nur dann erneut aus `main` publishen, wenn der Merge getestete Dateien mit parallelen `main`-Aenderungen
  zusammengefuehrt hat.

**Reihenfolge IDEAL** *(neu gefasst 2026-10-15)*
0. Gerald fuehrt P1-P5b auf Sage aus (live **und** Test-DB; die Views sind auf beiden vorhanden, Antwort 5).
   Die GRANTs fuer den Service-Login sind gesetzt (P1b).
1. *(optional, ohne Deploy, Rueckfrage 9 offen)* FA-View-Keys in `/StandortEinstellungen` umstellen, sobald P2 = 0.
2. **DB-Backup der WMS-DB**, dann **Web zuerst** deployen (seedet die neuen Keys: Arbeitsgaenge mit
   `KHKPpsArbeitsplaetze`, Artikel leer), dann den **Service**. Wird nur der Service deployt, fehlen die
   Key-Zeilen und der Service nimmt den Fallback im Code; das ist funktional gleich, aber in der Oberflaeche
   unsichtbar. Nach dem Deploy liest der Werkbank-Sync **unveraendert** aus `KHKPpsArbeitsplaetze`.
3. Arbeitsgaenge: Gerald setzt `Sync:ProductionWorkplaceSyncViewName = [vw_IDEAL_AKE_WMS_Arbeitsgaenge]`,
   **erst wenn P3b in beide Richtungen leer ist** *(QA v1.60.2: P3 ist ueberholt, die View hat kein Mandant/Aktiv)*. Ersten Lauf pruefen (`neu = 0`, `abweichung = 0`).
4. Artikel: P4 und P6 „vorher“ protokollieren, dann `Sync:ArticlesViewName = [vw_IDEAL_AKE_WMS_Artikel]` setzen.
   **Einen erfolgreichen Lauf `Article` im Aktivitaets-Protokoll abwarten**, dann P6 „nachher“.
5. P7 auf der WMS-DB ausfuehren (Referenz-Vollstaendigkeit). Weicht es ab: Skript ergaenzen, bevor es weitergeht.
6. Bereinigungsskript mit `@Loeschen = 0` und dem P5b-Ergebnis in `@Behalten` ausfuehren. **Gerald bestaetigt
   die Liste „loeschbar“** (und entscheidet ueber die Liste „referenziert“ getrennt, nicht Teil dieser Spec).
7. **Erneut DB-Backup**, dann das Skript mit `@Loeschen = 1` in einem ruhigen Fenster (keine laufende
   Kommissionierung/Lagerbuchung). Ergebnis (System, Datum, Kandidaten, referenziert, `Geloescht`) ins
   Deploy-Protokoll dieser Spec.
8. Naechsten Artikel-Sync abwarten, P6 „nach Bereinigung“: nur noch referenzierte 00er-Zeilen.
9. Nach der Abnahme: `DROP TABLE dbo.Articles_Bereinigung00er_Sicherung` (eigener Protokolleintrag).

Die Schritte 4-8 laufen **zuerst auf dem IDEAL-Testsystem** (TS-9X.4, TS-9X.10-12), dann live.

**AKE:** Web und Service deployen, keine Einstellung aendern. `Sync:ArticlesViewName` bleibt leer, der
Werkbank-Sync laeuft dort nicht. Das Bereinigungsskript wird auf AKE **nicht** ausgefuehrt (der Guard wuerde
abbrechen).

**Rueckweg:** Die Code-Teile laufen ohne Code-Rollback ueber die Einstellungen zurueck (FA-Keys auf die alten
Namen, `Sync:ArticlesViewName` leeren, Arbeitsgaenge-Key auf `KHKPpsArbeitsplaetze`). **Achtung:** Artikel, die im
View-Modus neu angelegt wurden, bleiben stehen (kein Delete); wird `Sync:ArticlesViewName` nach der Bereinigung
geleert, holt der alte Select die 00er zurueck (als neue Zeilen mit neuer `Id`). Die Bereinigung selbst wird
ueber den Rueckweg-Block des Skripts aus `dbo.Articles_Bereinigung00er_Sicherung` (gleiche `Id`; nur solange
dieselben Nummern nicht neu angelegt wurden, sonst scheitert er am Unique-Index `ApplicationDbContext.cs:232`) oder
ueber das DB-Backup aus Schritt 7 rueckgaengig gemacht; nach Schritt 9 nur noch ueber das Backup. Beim Code-Rollback
bleiben die zwei neuen Key-Zeilen als verwaiste ServiceSettings-Zeilen liegen. Sie sind harmlos.

## Risiken (Kurzliste)

- **Nummernformat (H2):** Liefert eine neue View Artikel- oder Arbeitsplatznummern anders formatiert, entstehen
  Dubletten bzw. neue Werkbaenke, und die String-Match-Stellen (Kommissionier-Umbuchung, Sage-Buchung,
  Bestand-Abgleich) laufen still ins Leere. Abwehr: P3/P4c als **harte Vorbedingung** und TS-9X.5.
- **Bestehende PickingItems:** Aendert die neue FAListe das `Artnr`-Format, behalten bereits initialisierte
  `PickingItems` die alte `BomArticleNumber` und finden den Artikel nicht mehr (`PickingTransferService.cs:173-183`,
  fallstricke §1 „PickingItems ggf. loeschen“). Das ist nur relevant, wenn P2 Abweichungen in `Artnr` zeigt.
- **OBJECT_ID = NULL bei fehlender Berechtigung:** Das wird als „nicht vorhanden“ gemeldet, deshalb steht
  die Berechtigung im Meldetext und P1b in den Vorbedingungen.
- **00er-Bereinigung loescht zu viel:** Abwehr ueber die vollstaendige Referenzliste (FKs + Textzuordnungen +
  Sage-Spiegel), P7 auf der echten DB, `@Loeschen = 0` als Vorgabe, Geralds Bestaetigung der Liste, Sicherungstabelle
  und DB-Backup. Restrisiko: Eine Nummer, die nur ausserhalb des WMS (Sage, enaio, Papier) eine Rolle spielt,
  ist fuer das Skript unsichtbar; dagegen hilft nur die Bestaetigung der Liste.
- **00er kommen zurueck:** Laeuft die Bereinigung vor dem View-Umschalten oder wird `Sync:ArticlesViewName`
  spaeter geleert, legt der alte Select sie wieder an. Abwehr: Guard im Skript und Hinweis im Rueckweg.
- **Begriffe (Gerald 2026-10-15, wie Begriffsklaerung [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]]):**
  Die View liefert die IDEAL-**Arbeitsgaenge**. Bei IDEAL ist ein Arbeitsgang ein Sage-Arbeitsplatz, und der ist
  im WMS die Werkbank (`ProductionWorkplace`, Kuerzel `AG` -> `ArbeitsschrittCode`). Die View ist der
  Arbeitsgang-**Katalog**; die BDE-Arbeitsgaenge je FA (`WorkOperation`) entstehen daraus ueber die
  Struktur-Erkennung (Kuerzel der FA-Struktur gegen `ArbeitsschrittCode`). Die View schreibt nicht direkt in
  `WorkOperation`, weil sie keinen FA-Bezug hat.

## Offene Rueckfragen

*Stand 2026-10-15: offen sind nur noch 3, 7 und 9. Die Nummerierung bleibt, damit sie zum Freigabe-Block passt.*

1. ~~**Was genau ist das „Problem der 00er-Artikelnummern“?**~~ **Beantwortet 2026-10-15 (Gerald, Antwort 1):**
   „die 00er koennen wir loeschen“, ohne Beispielnummer. Abgrenzung der Kandidaten ueber P5b/P6, Gerald bestaetigt
   die Liste vor dem Loeschen (Deploy-Schritt 6). Urspruengliche Frage: Beispielnummer und Fundort; Hypothesen
   H1/H2/H3 (`SageImportService.cs:392-396`).
2. ~~**Altbestand der 00er-Zeilen in `Articles`:** nur messen, Bereinigung als Folgeaufgabe?~~ **Entschieden
   2026-10-15 (Koordination, abgeleitet aus Antwort 1; Gerald bestaetigt mit der Freigabe):** Die Bereinigung ist
   Teil **dieser** Spec, als Einmal-Datenskript nach ADR 0015, das nur unreferenzierte Zeilen loescht und
   referenzierte listet (Abschnitt „Bereinigung der 00er-Artikel“).
3. **Bestaetigung „Articles bleibt“:** Die View ersetzt nur die **Quelle** des Artikel-Syncs, nicht die
   WMS-Tabelle. Begruendung: FKs `StockMovements`, `ArticleAttributeValues`, `FaWorkStepSpecs`
   (`ApplicationDbContext.cs:299-313, 344-351, 590-593`), Hauptlagerplatz-Lock und AKE.
   *Empfehlung:* ja.
4. ~~**Spaltenabbildung der Artikel-View**~~ **Beantwortet 2026-10-15 (Gerald, Antwort 4):** „sollte gleich
   sein“ (Spalten wie heute, Artikelgruppe als Code). **Unverifiziert**; P4 (inkl. P4a-P4e) bleibt Pflicht vor
   dem Umstellen. Urspruengliche Frage: welche View-Spalte liefert welches Feld, Artikelgruppe nur Code
   (`OrderRecipientRepository.cs:98/111`), Eindeutigkeit (P4a). **Nachtrag 2026-10-15 (P4):** Die View liefert die
   WMS-Namen (`ArticleNumber, Description, Matchcode, Unit, ArticleGroup, ReorderLevel, PrimaryStorageLocation`), nicht
   die Sage-Namen — „gleich“ hiess „gleich wie das WMS“. Umgesetzt in v1.60.1; P4a/P4b bleiben offen.
5. ~~**Views in der Sage-Test-DB vorhanden?**~~ **Beantwortet 2026-10-15 (Gerald, Antwort 5):** ja, vorhanden.
6. ~~**Fehlende View: Fehlermail ja/nein?**~~ **Entschieden 2026-10-15 (Koordination; Gerald bestaetigt mit der
   Freigabe):** Fehlt eine **FA-View**, bleibt es bei Fehler + Fehlermail, weil ein Ausfall der FA-Struktur nicht
   still sein darf. Fehlt die **Artikel- oder Arbeitsplatz-Quelle**, gibt es nur eine Warnung im
   Aktivitaets-Protokoll, keine Mail. (Die urspruengliche Empfehlung „keine Mail fuer alle“ ist damit fuer FA
   ueberholt.)
7. **Standort-Schaltung Artikel:** ein eigener String-Key `Sync:ArticlesViewName` (leer = alter Select)
   statt einer Kopplung an den Master `ProduktionsauftragHierarchisch`? *Empfehlung:* eigener Key. Er ist
   explizit, ohne Deploy umkehrbar und trennt die Artikel-Quelle von der FA-Hierarchie.
8. ~~**Arbeitsgaenge-Key:** Default `[vw_IDEAL_AKE_WMS_Arbeitsgaenge]`?~~ **Entschieden 2026-10-15
   (Koordination; Gerald bestaetigt mit der Freigabe):** Default und Code-Fallback = `KHKPpsArbeitsplaetze` (die
   heutige Tabelle), **nicht** die View. Grund: `Program.cs:453-467` legt neue Keys beim Deploy mit ihrem Default
   an; ein View-Default wuerde IDEAL umschalten, bevor P3 belegt ist. Gerald stellt nach P3 von Hand um. Die
   Existenzpruefung bleibt ohne Typfilter (Tabelle oder View).
9. **FA-Views schon vor dem Deploy umstellen?** Das geht ohne Code, sobald P2 = 0. *Empfehlung:* ja, sobald P2 = 0
   auch in der Spalte `Artnr` (die EXCEPT-Abfragen in P2). Antwort 1 hat die Frage nach dem `Artnr`-Format nicht
   geklaert; P2 beantwortet sie (siehe Risiko „Bestehende PickingItems“).

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. 00er-Problem (Beispielnummer, wo sichtbar, Ergebnis P5/P6) → **Gerald 2026-10-15: „die 00er können wir löschen". Keine Beispielnummer; Abgrenzung der 00er-Zeilen über P5/P6 (offen).**
2. Altbestand 00er: nur messen, Bereinigung als Folgeaufgabe? → *entschieden 2026-10-15 durch die Koordination (abgeleitet aus Antwort 1): Bereinigung in dieser Spec — **bestaetigt Gerald 2026-10-15***
3. Articles bleibt, View ersetzt nur die Sync-Quelle? → **Ja (Gerald 2026-10-15).**
4. Spaltenabbildung Artikel-View (Ergebnis P4, Artikelgruppe nur Code?) → **Gerald 2026-10-15: „sollte gleich sein" (Spalten wie heute, Artikelgruppe als Code) — unverifiziert, P4 bleibt Pflicht vor dem Umstellen.**
5. Views in der Sage-Test-DB vorhanden / werden angelegt? → **Ja, vorhanden (Gerald 2026-10-15).**
6. Fehlende View: Fehlermail ja/nein? → *entschieden 2026-10-15 durch die Koordination: FA-View fehlt = Fehler + Mail; Artikel-/Arbeitsplatz-Quelle fehlt = nur Warnung, keine Mail — **bestaetigt Gerald 2026-10-15***
7. Artikel-Schaltung ueber eigenen Key Sync:ArticlesViewName? → **Ja (Gerald 2026-10-15).**
8. Arbeitsgaenge-Default View + Rueckweg per KHKPpsArbeitsplaetze? → *entschieden 2026-10-15 durch die Koordination: Default = KHKPpsArbeitsplaetze, Umstellung auf die View durch Gerald nach P3 — **bestaetigt Gerald 2026-10-15***
9. FA-Views vor dem Deploy per Einstellung umstellen? → **Ja, sobald P2 inkl. Spalte Artnr 0 Abweichungen zeigt; auf IDEAL-Test erst nach den laufenden Abnahmelaeufen (Gerald 2026-10-15).**

## Umsetzung (2026-10-15, Dev-Lauf)

Direkt im Bündel `feature/2026-08-07-ideal-teile-1-5`, Start-HEAD `d4108f1a`, Commits `b53b99dd` (Service, Katalog, Tests)
und `2524bb03` (Einmal-Skript, Doku, Version). Gemessen: v1.59.0 → **v1.60.0**, höchstes TS-Kapitel TS-92 → **TS-93**.
Zeilennummern gegen den Bündel-HEAD geprüft; sie stimmen mit der Spec überein. `SQL/Einmalig/` **gab es im Bündel schon**
(zwei Skripte, u. a. `2026-10-07_v1.47.4_…`); der Hinweis „neu anlegen" war überholt.

**Was gebaut ist**
- `FaHierarchySql.SourceExistsAsync` (`SELECT OBJECT_ID(@name)`, Name als Parameter, ohne Typfilter) +
  `SourceMissingMessage` — ein Helfer für alle drei Quellen.
- FA: Fallback in `FaHierarchySyncService` und Katalog-Default auf `[vw_IDEAL_AKE_WMS_FAListe]`/`[…FAInfos]`. Existenz beider
  Views direkt nach `OpenAsync`, vor dem Empty-Guard; fehlt eine → `InvalidOperationException` mit Meldetext in den
  bestehenden catch (LogError + FinishFailed + rethrow), Mail über `RunResilientAsync`. Kein eigener Mail-Aufruf nötig.
- Werkbank: neuer Key `Sync:ProductionWorkplaceSyncViewName` (Default `KHKPpsArbeitsplaetze`, Code-Konstante
  `ProductionWorkplaceSyncService.DefaultSource`, Test prüft Gleichheit mit dem Katalog). Reader nimmt die geklammerte
  Quelle, liefert `SageArbeitsplatzReadResult(SourceExists, Rows)`; Spalten/Filter unverändert. Ungültiger Name →
  FinishFailed + throw (Mail), kein SQL. Quelle fehlt → Warnung, FinishSuccess mit 0-Counts, Suffix „Quelle nicht vorhanden“.
  Der Key wird mit `GetValueSafeAsync` gelesen (Muster der übrigen Werkbank-Keys): bei Settings-DB-Fehler gilt der Default.
- Artikel: Select-Auswahl in der reinen Klasse `SageArticleSql` (`LegacySql` = bisheriger Text, beim Verschieben per
  Skript auf Zeichengleichheit nach Raw-String-Dedent geprüft; `BuildViewSql` mit den Spaltennamen des heutigen Selects —
  **unverifiziert bis P4**, Freigabe-Antwort 4). Key `Sync:ArticlesViewName` mit `GetValueAsync` (nicht Safe: ein
  Settings-Fehler darf nicht still auf den alten Select zurückfallen). Ungültiger Name → bestehender catch (Fehler + Mail).
  View fehlt → Warnung + Skip vor jedem Read.
- Katalog: zwei Keys in `ServiceSettingDefinitions.All` (≤ 500 Zeichen), Drift-Guard-Liste, Standort-Maske (Gruppe
  „Mandant / Sage-Views“).
- `SQL/Einmalig/2026-10-15_IDEAL-Sage-Views_Bereinigung00erArtikel.sql` nach Skript-Aufbau 1–6, Kopf-Wortlaut aus der
  Freigabe. Abweichung zur Skizze: `Referenzen` wird im `SELECT … INTO` über eine abgeleitete Tabelle berechnet (keine
  berechnete Spalte per `ALTER TABLE` auf die Temp-Tabelle im selben Batch). **Nicht ausgeführt.**
- Doku: README (Key-Gruppe + Abschnitt „IDEAL: Sage-Quellen …“ mit Reihenfolge P1–P7), Anwender-Changelog, beide
  `AppVersion.cs` (1.60.0, 2026-10-15), TS-93 (13 Fälle), [[fallstricke]] §16, `codebase/services.md`,
  `codebase/integrationen.md`, Testindex.

**Nachweis**
- `dotnet build IdealAkeWms.slnx` → **0 Fehler**.
- `dotnet test` → Web **2016 bestanden**, 1 übersprungen (Bestand); Service **281 bestanden** (+9: 5 neue
  `ValidateAndQuote`-Fälle, 2 `SageArticleSqlTests`, 2 `ProductionWorkplaceSyncServiceTests`), Web +2 Drift-Guard-Keys.
- Nicht unit-testbar (raw SQL gegen Sage, fallstricke §8): Existenzprüfung, FA-Abbruch mit Mail, Artikel-Skip, View-Select,
  Einmal-Skript → TS-93. Keine Sage-Abfrage ausgeführt (P1–P7 bleiben bei Gerald).

**Befunde (nicht mitgebaut)**
- Doc-Kommentare in `FaHierarchyNode.cs:6`/`FaHierarchyOrderInfo.cs:6` nennen noch die alten View-Namen (nur Kommentar).
- `SageBestandReader.cs:33` Mandant 1 fest codiert (aus der Spec, weiter offen).

## QA (2026-10-15, qa-agent)

Geprueft im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Diff `d4108f1a..2524bb03` (Commits `b53b99dd`, `2524bb03`).
Kein Code geaendert, kein Commit. Web-App/Service nicht gestartet, keine Sage-Abfrage, Einmal-Skript **nicht** ausgefuehrt.

**Verdikt: gruen, keine offenen Critical/Important → `status: Testbereit`.**

### 1. Build und Tests (selbst ausgefuehrt, sauberer Arbeitsbaum)
- `dotnet build IdealAkeWms.slnx` → **0 Fehler**, 24 Warnungen (vorbestehend, z. B. CS8601 in Testdateien).
- `dotnet test --no-build`:
  - `IDEALAKEWMSService.Tests` → **281 bestanden**, 0 uebersprungen, 0 fehlgeschlagen.
  - `IdealAkeWms.Tests` → **2016 bestanden**, 1 uebersprungen (vorbestehend), 0 fehlgeschlagen.

### 2. Code-Review (selbst, in diesem Lauf)
Gelesen: kompletter Code-Diff; Umfeld `SyncWorker.cs:84-96, 335-392, 481-503` (`RunResilientAsync`/`NotifyErrorAsync`),
`SageImportService.SyncArticlesAsync` inkl. catch (:369-575), `FaHierarchySyncService.RunAsync` (:46-165),
`ProductionWorkplaceSyncService.RunAsync`, `ServiceSettings.cs` (Safe/Nicht-Safe), `StandortEinstellungenController.Save`,
`StandortSettingsWriter`, Model-Snapshot (Articles-Spalten, alle `HasOne("…Article")`), `00_FreshInstall.sql`.

- **(a) Zeichengleichheit `SageArticleSql.LegacySql`:** per Compiler nachgewiesen. Der alte Literal (`git show
  d4108f1a:…/SageImportService.cs`, Zeilen 382-424, unveraendert inkl. Einrueckung) wurde in einen temporaeren xUnit-Test
  kopiert und gegen `LegacySql`, `Select(null)` und die Laenge per `Assert.Equal` verglichen → **bestanden**. Der erste
  Versuch scheiterte nur an `\n` vs `\r\n`: die Temp-Datei war LF, beide Quelldateien sind im Arbeitsbaum CRLF
  (`core.autocrlf=true`, Index LF, `git ls-files --eol`). Bei gleicher Zeilenende-Konvention, die beim Build eines
  Checkouts fuer beide Dateien gleich ist, sind die Strings identisch. Temp-Test danach entfernt.
- **(b) Fehler-/Mail-Pfade gegen Rueckfrage 6:**
  FA-View fehlt → `InvalidOperationException` im try (:90-93) → catch `LogError`+`FinishFailed`+rethrow (:156-164) →
  `RunResilientAsync` → `NotifyErrorAsync` = Mail; die Pruefung liegt vor `ScalarCountAsync` und jeder Mutation. OK.
  Artikel-View fehlt → Warnung, `FinishSuccess` mit Suffix „Quelle nicht vorhanden“, `return` vor jedem Read, kein
  Rueckfall, keine Mail. OK. Artikel ungueltiger Name → `ValidateAndQuote` im try → catch → rethrow → Mail, kein Sage-SQL. OK.
  Arbeitsplatz-Quelle fehlt → Reader liefert `SourceExists=false` → Warnung + `FinishSuccess` mit 0-Counts, kein throw. OK.
  Arbeitsplatz ungueltiger Name → `LogError`+`FinishFailed`+throw vor dem Reader-Aufruf → Mail. OK.
  Sage-Verbindungsfehler bei der Existenzpruefung im Reader → bestehender catch (:93-99) → FinishFailed + throw → Mail. OK.
- **(c) SQL-Injection:** Jede Interpolation (`BuildViewSql`, Reader-`FROM {quotedSource}`, FA-Count/Selects) bekommt nur
  das Ergebnis von `FaHierarchySql.ValidateAndQuote` (Whitelist, 1-2 Segmente, Negativfaelle `FaHierarchySqlTests`
  :30-45). `OBJECT_ID(@name)` laeuft mit Parameter. Der Reader interpoliert seinen Parameter ungeprueft; der Vertrag steht
  im Interface, und der einzige Aufrufer `ProductionWorkplaceSyncService.cs:75` validiert vorher. OK.
- **(d) Reader-Signatur:** `GetActiveAsync(string, int, ct)`. Aufrufer: nur `ProductionWorkplaceSyncService.cs:75`;
  DI `Program.cs:59` unveraendert; Mocks nur in `ProductionWorkplaceSyncServiceTests` (angepasst). Die uebrigen
  `GetActiveAsync`-Treffer gehoeren zu `IWorkStepRepository` (andere Methode). OK.
- **(e) Safe vs. Nicht-Safe:** Artikel nutzt `GetValueAsync`: ein Settings-Fehler wirft und fuehrt zu Fehler + Mail statt
  zu einem stillen Rueckfall auf den alten Select. Das ist richtig. Arbeitsplatz nutzt `GetValueSafeAsync` wie die
  uebrigen Werkbank-Keys, siehe M-2.
- **(f) Katalog:** beide Keys in `ServiceSettingDefinitions.All` (Typ String, Defaults `""` / `KHKPpsArbeitsplaetze`),
  in der Drift-Guard-Liste und in `StandortSettingsCatalog.Fields` (Gruppe „Mandant / Sage-Views“, `ViewNameHint`).
  `DefaultSource` == Katalog-Default per Test. Beschreibungen ≤ 500: `All_FieldsFitServiceSettingsColumns` gruen.
  Das Leeren des Artikel-Keys ueber die Maske geht (String → `val = raw`, `raw ??= ""`, `StandortEinstellungenController.cs:58-75`).
- **(g) Einmal-Skript:** Guards mit `RAISERROR(…,16,1)` + `RETURN`, kein `GO` im Skript (ein Batch). OK. `SET NOCOUNT ON;
  SET XACT_ABORT ON;` OK. `@Loeschen = 0` als Vorgabe, `@Behalten` vorhanden. OK. Sicherung per `SELECT a.* … INTO` vor
  dem `DELETE` in **einer** Transaktion, `@@ROWCOUNT`-Abgleich mit `ROLLBACK` vor dem `RAISERROR`, Ausgabe
  `Geloescht`/`Gesichert`. OK.
  Die Referenzliste enthaelt alle 12 Zaehlspalten der Spec-Tabelle (11 Zeilen; Beschichtung und FaHierarchyNodes je 2
  Spalten). Gegengeprueft: Der Snapshot hat genau 3 `HasOne("…Article")` (:3754 Cascade, :3998 SetNull, :4190 Restrict);
  `00_FreshInstall.sql` hat genau 3 `REFERENCES [dbo].[Articles]` (:192, :520, :1382). Alle Snapshot-Properties
  `*ArticleNumber*|Artnr|HauptArtnr|Artikelnummer|Ressourcenummer` liegen in Articles, BeschichtungsauftragPositionen,
  CachedBomHeaders, CachedBomItems, FaHierarchyNodes, OseonProductionOrders, PartRequisitions, PickingItems,
  ProductionOrders und WarehouseRequisitionItems; alle stehen im Skript. Tabellennamen gegen `ToTable(…)` geprueft.
  Die Rueckweg-Spaltenliste (16 Spalten inkl. `ArticleCategoryId`) entspricht den Snapshot-Properties von `Article`
  (16, :50-121; `ArticleCategoryId` in FreshInstall per ALTER :1287-1295). OK. Kopf: „NICHT ERNEUT AUSFUEHREN“,
  Begruendung woertlich wie Skript-Aufbau Punkt 1, daten-destruktiv + DB-Backup, Reihenfolge, kein History-/FreshInstall-
  Eintrag. OK. Datei LF/UTF-8.

**Befunde**

| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| M-1 | Minor | Der Artikel-Skip-Pfad schreibt per `FinishSuccessAsync` nur `gelesen/neu/aktualisiert`, nicht `hauptlagerplatz_fehlt`; der Normal-Lauf hat 4 Keys. Kosmetik im Protokoll. | nicht geaendert, gemeldet |
| M-2 | Minor | Arbeitsplatz-Key mit `GetValueSafeAsync`: faellt bei einem Settings-DB-Fehler nach der Umstellung still auf `KHKPpsArbeitsplaetze` zurueck. Akzeptiert: gleiches Muster wie Mandant/Ausschlussliste; der Lauf schreibt in dieselbe WMS-DB und scheitert dann ohnehin; P3 = 0 ist Vorbedingung, Tabelle und View sind dann gleich. | akzeptiert |
| M-3 | Minor | Nachweisluecke: Der Pfad „ungueltiger Arbeitsplatz-Name → FinishFailed + throw, kein Reader-Aufruf“ ist nicht unit-getestet (der Key kommt aus der DB, der Safe-Read liefert im Test null). Abgedeckt durch die Negativ-Tests von `ValidateAndQuote`, das Code-Review und TS-93.8. | akzeptiert |
| M-4 | Minor | Einmal-Skript: Die erneute Referenzpruefung in der Transaktion hat keine Lock-Hints. Eine Text-Referenz (z. B. `PickingItems`), die genau zwischen Pruefung und `DELETE` entsteht, wuerde nicht gesehen. FK-Referenzen scheitern laut (Restrict + XACT_ABORT). Abwehr: Deploy-Schritt „ruhiges Fenster“. | akzeptiert |
| M-5 | Minor | Einmal-Skript: `@Behalten.ArticleNumber` hat kein `COLLATE DATABASE_DEFAULT`. Weicht die Kollation ab, scheitert der Vergleich laut vor jeder Aenderung (kein Datenrisiko). Optional haerten. | gemeldet |
| M-6 | Minor | Der Kommentar im Rueckweg nennt `UQ_Articles_ArticleNumber` (FreshInstall); auf EF-erzeugten DBs heisst der Index `IX_Articles_ArticleNumber`. Nur Kommentar. | gemeldet |
| M-7 | Minor | `SageArticleSqlTests` prueft `LegacySql` nur per StartWith/Contain, nicht gegen den alten Literal. Die Zeichengleichheit wurde in diesem QA-Lauf einmalig per Compiler nachgewiesen (siehe a); einen dauerhaften Regressionsschutz gibt es nicht. | akzeptiert |
| (Dev) | Minor | Doc-Kommentare `FaHierarchyNode.cs:6`/`FaHierarchyOrderInfo.cs:6` mit alten View-Namen; `SageBestandReader.cs:33` Mandant 1 fest codiert. | aus dem Dev-Lauf, offen |

Keine Critical-, keine Important-Befunde. Gezielt geprueft und ohne Befund: keine Werte in `<script>`-Bloecken, kein
`hidden`+`d-*`, keine Parallel-Array-Bindung (keine View-/JS-Aenderung ausser der statischen Changelog-Karte) und keine
Validierung nur im Browser (die Namen validiert der Service). Zur Frage „eine Regel an mehreren Orten“: Die
Existenzpruefung liegt an EINER Stelle (`FaHierarchySql.SourceExistsAsync`), der Arbeitsplatz-Default ist per Test an
den Katalog gebunden.

### 3. Beweisart je Akzeptanzkriterium
| AK | Beweis |
|---|---|
| 1 | automatisiert: `ValidateAndQuote` mit neuen Namen (`FaHierarchySqlTests`); Default/Fallback per Code-Review |
| 2 | Manual-UAT TS-93.2; README-Hinweis per Code-Review |
| 3 | Code-Review (Pfad oben) + Manual-UAT TS-93.7 (raw SQL gegen Sage, nicht InMemory-testbar) |
| 4 | automatisiert (vorbestehende Negativ-Tests, Code unveraendert) + Manual-UAT TS-93.8 |
| 5 | automatisiert: Drift-Guard, `DefaultSource` == Katalog, `All_FieldsFitServiceSettingsColumns`; Standort-Maske Code-Review + TS-93.2 |
| 6 | automatisiert nur die Quelle `[KHKPpsArbeitsplaetze]` (`Run_OhneEinstellung_LiestDefaultTabelle`); Counts → Manual-UAT TS-93.3 |
| 7 | automatisiert: `Run_QuelleFehlt_WarnungUndSkip_OhneMutationOhneException`; ungueltiger Name Code-Review + TS-93.8 (M-3) |
| 8 | Code-Review (`SyncWorker.cs:377-378` nicht im Diff) + Manual-UAT TS-93.10 |
| 9 | automatisiert Drift-Guard; Maske Code-Review + TS-93.2 |
| 10 | automatisiert `Select(null)` same as `LegacySql` + QA-Compiler-Nachweis der Zeichengleichheit; AKE-Counts Manual-UAT TS-93.10 |
| 11 | automatisiert: erzeugtes View-SQL (`SageArticleSqlTests`); echter Read Manual-UAT TS-93.4 (Spaltennamen unverifiziert bis P4) |
| 12 | Code-Review + Manual-UAT TS-93.6/TS-93.8 |
| 13 | Manual-UAT (P6 vor/nach, TS-93.4) |
| 14 | automatisiert, gruen (oben) |
| 15 | Code-Review: TS-93 in `docs/TESTSZENARIEN.md` (LF, 0 CR), Index-Zeile 93, `services.md`, `integrationen.md`, README, Changelog, beide `AppVersion.cs` 1.60.0/2026-10-15, fallstricke §16 („Die View … liefert ARBEITSPLAETZE“) |
| 16 | Code-Review (oben, g) |
| 17-19 | Manual-UAT TS-93.11 – 93.13 (raw SQL, nicht unit-testbar) |

Gruen gezaehlt sind nur die automatisierten Anteile; Manual-UAT-Kriterien sind bis zur Abnahme offen.

### 4. Geaenderte bestehende Tests
| Test | Einordnung |
|---|---|
| `ProductionWorkplaceSyncServiceTests.Build(...)` (Helper) | reine Typ-Anpassung an die neue Reader-Signatur (`GetActiveAsync(string,int,ct)` → `SageArbeitsplatzReadResult(true, rows)`); bestehende Tests laufen mit `SourceExists = true` und unveraenderten Erwartungen |
| `FaHierarchySqlTests.ValidateAndQuote_valid_names_are_accepted_and_quoted` | nur neue `InlineData` (AK 1), alte Faelle unveraendert |
| `ServiceSettingDefinitionsTests` Drift-Guard | nur zwei neue `InlineData` (AK 5, 9) |

Kein „Sonstiges“. Neu: `SageArticleSqlTests` (2), zwei neue Facts in `ProductionWorkplaceSyncServiceTests`.

### 5. CLAUDE.md-Checkliste
Migration/SQL-Skript/FreshInstall: entfaellt (kein Schema). AgentJobs unberuehrt. Audit-Felder: keine neuen
Schreibwege, Upsert-Block unveraendert. Version 1.60.0 in beiden `AppVersion.cs` + Anwender-Changelog. Brain-Changelog
[[2026-10-15-v1-60-0-ideal-sage-views-umstellung]] vorhanden. feature-map nachgezogen. TS-93 + Index vorhanden.
Einmal-Skript unter `SQL/Einmalig/` (ADR 0015), nicht ausgefuehrt. Keine neue Rolle.

## Manuelle Test-Checkliste (Mensch, Schranke 2)

Vorher: P1/P1b, P2 (inkl. `Artnr`), P3, P4/P4a-e und P5/P5b auf der **Sage-Test-DB** ausfuehren und protokollieren;
DB-Backup der WMS-Test-DB.

1. Web + Service aus dem Worktree publishen (Befehle im Deploy-Abschnitt), Web zuerst.
2. TS-93.2: FA-Key-Werte vorher/nachher gleich; `Sync:ProductionWorkplaceSyncViewName` = `KHKPpsArbeitsplaetze` und
   `Sync:ArticlesViewName` = leer in `/ServiceSettings` und in der Standort-Maske sichtbar.
3. TS-93.3 Teil 1: erster Lauf `ProductionWorkplaceSync` nach dem Deploy mit denselben Counts wie davor.
4. TS-93.10 (AKE-Test): Lauf `Article` mit Counts wie zuvor, kein `ProductionWorkplaceSync`, kein `FaHierarchy`.
5. TS-93.6: `Sync:ArticlesViewName = [vw_GibtEsNicht]` → Warnung, Suffix „Quelle nicht vorhanden“, **keine** Mail,
   `Articles`-Anzahl gleich; dasselbe mit dem Arbeitsplatz-Key. Keys zuruecksetzen.
6. TS-93.7: FAListe-Key = `[vw_GibtEsNicht]` → Lauf fehlgeschlagen, Fehlermail, `FaHierarchyNodes`/`…OrderInfos` gleich;
   danach dasselbe mit dem FAInfos-Key. Keys zuruecksetzen.
7. TS-93.8: `vw_x;DROP` im Artikel- und im Arbeitsplatz-Key → Fehler + Fehlermail, kein SQL.
8. TS-93.1 (nach P2 = 0): FA-Keys auf `[vw_IDEAL_AKE_WMS_FAListe]`/`[vw_IDEAL_AKE_WMS_FAInfos]`, Lauf erfolgreich, Stichprobe.
9. TS-93.3 Teil 2 / TS-93.3b (nach P3b leer, *QA v1.60.2: nicht P3*): Arbeitsplatz-Key auf `[vw_IDEAL_AKE_WMS_Arbeitsgaenge]` → `neu = 0`, `abweichung = 0`.
10. TS-93.4 (nach P4): P6 „vorher“, `Sync:ArticlesViewName = [vw_IDEAL_AKE_WMS_Artikel]`, Lauf erfolgreich, `gelesen` ≈ P4d,
    5er-Stichprobe (Artikelgruppe als Code), P6 „nachher“. Fehlt eine Spalte → Fehlermail → Befund, Key leeren.
11. TS-93.5: Umbuchung an einem HauptFA mit 2 Positionen (je eine `StockMovement`), Sage-Lagerbuchung, Lagerbestand-Sync
    ohne neue Warnungen „nicht im WMS“.
12. P7 auf der WMS-Test-DB; weicht es ab, das Skript ergaenzen lassen, bevor es weitergeht.
13. TS-93.11: Testdaten (4 Referenzarten), Skript mit `@Loeschen = 0` → `Articles`-Anzahl gleich, 4 Zeilen unter
    „referenziert“; Liste „loeschbar“ bestaetigen und vermerken.
14. TS-93.12: Backup, `@Loeschen = 1` → `Geloescht` = `Gesichert` = „loeschbar“; referenzierte Zeilen unveraendert; der naechste
    Artikel-Sync legt keine geloeschte Nummer neu an; ein zweiter Lauf bricht am Sicherungstabellen-Guard ab.
15. TS-93.13: Guard auf AKE-Test (Abbruch „kein IDEAL-System“), Guard bei leerem `Sync:ArticlesViewName` (Key danach sofort
    wieder setzen), Rueckweg-Block stellt die Zeilen mit gleicher `Id` wieder her.
16. TS-93.9 zuletzt: Rueckweg ohne Deploy (Artikel-Key erst nach 13-15 leeren, danach wieder setzen und Sync abwarten).
17. Ergebnisse (System, Datum, Kandidaten, referenziert, `Geloescht`) ins Deploy-Protokoll dieser Spec eintragen; dann Merge.

## Nachbesserung v1.60.1 (2026-10-15, Dev-Lauf)

**Befund (Gerald, IDEAL):** Mit `Sync:ArticlesViewName = [vw_IDEAL_AKE_WMS_Artikel]` scheiterte der Lauf „Article“ mit
„Ungueltiger Spaltenname Artikelnummer/Bezeichnung1/Lagermengeneinheit/Artikelgruppe/Meldebestand/Kurzbezeichnung“. P4: Die
View liefert `ArticleNumber, Description, Matchcode, Unit, ArticleGroup, ReorderLevel, PrimaryStorageLocation` (alle
nvarchar). **Ursache:** v1.60.0 hat die Antwort 4 („sollten gleich sein“) als „gleich wie der alte Sage-Select“ gelesen
und die Spaltennamen daraus abgeleitet, statt auf P4 zu warten. Der Fehlerpfad hat wie vorgesehen laut gemeldet (Fehler +
Mail, keine Mutation).

**Behoben** im Bündel, Start `2524bb03`, Commit `63524941` (v1.60.0 → **v1.60.1**, gemessen):
- `SageArticleSql.BuildViewSql` liest die WMS-Namen; CAST/MAX/GROUP BY wie bisher. `LegacySql` unverändert.
- Dauerhafter Wächter `SageArticleSqlTests.LegacySql_IstZeichengleichMitDemBisherigenSelect`: Länge 1940 + SHA-256 über
  den LF-normalisierten Text, gerechnet aus dem Literal in `SageImportService.cs` bei `d4108f1a` (QA-Minor M-7 erledigt).
  View-Test prüft die WMS-Namen und das Fehlen der Sage-Namen.
- Rückweg-Kommentar im Einmal-Skript nennt beide Indexnamen (`UQ_Articles_ArticleNumber` / `IX_Articles_ArticleNumber`,
  [[fallstricke]] §9) (QA-Minor M-6 erledigt).
- TS-93.4b, Anwender-Changelog, beide `AppVersion.cs`. [[fallstricke]] §16: View-Spaltennamen nie aus einer Antwort ableiten.

**Nachweis:** `dotnet build IdealAkeWms.slnx` → 0 Fehler; `dotnet test` → Web 2016 bestanden / 1 übersprungen (Bestand),
Service **282** bestanden (+1 Hash-Wächter). Nicht unit-testbar: der echte View-Read → TS-93.4b (Gerald).


## QA Nachbesserung v1.60.1 (2026-10-15, qa-agent)

Geprueft im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Diff `2524bb03..63524941` (7 Dateien), HEAD
`63524941`. Kein Code geaendert. **Verdikt: gruen, keine Critical/Important — Status bleibt Testbereit.**

**1. Build + Tests (selbst ausgefuehrt):** `dotnet build IdealAkeWms.slnx` → 0 Fehler (28 Warnungen, Bestand).
`dotnet test` → `IdealAkeWms.Tests` 2016 bestanden / 1 uebersprungen / 0 Fehler (2017); `IDEALAKEWMSService.Tests`
282 bestanden / 0 / 0.

**2. Review `BuildViewSql`** (`IDEALAKEWMSService/Services/SageArticleSql.cs`): liest genau die sieben P4-Spalten,
Aliase in Reihenfolge ArticleNumber(0), Description(1), Unit(2), ArticleGroup(3), ReorderLevel(4),
PrimaryStorageLocation(5), Matchcode(6) = Ordinal-Reader `SageImportService.cs:413-422`. `MAX(CAST(col …)) AS col`
referenziert im Aggregat die Basisspalte (zulaessig). `WHERE` sieht in T-SQL keine SELECT-Aliase → Basisspalte;
`GROUP BY CAST(ArticleNumber AS nvarchar(100))` ist ausdrucksgleich mit der nicht aggregierten SELECT-Spalte → gueltig.
Kein stiller Rueckfall eingebaut. `LegacySql` unveraendert.

**3. Hash-Waechter unabhaengig nachgerechnet:** Literal `sageSql` aus `git show d4108f1a:IDEALAKEWMSService/Services/SageImportService.cs`
(Zeilen 383-423), CRLF→LF, Einrueckung der schliessenden `"""` (16 Leerzeichen) entfernt, per Python: Laenge **1940**,
SHA-256 **8b97486eaa71ab616624be873bdedd9cf2aacb10716f09439071349ac30c5058** — identisch mit dem Test.

**4. Begleitdateien:** Skript-Kommentar nennt `UQ_Articles_ArticleNumber` (`SQL/00_FreshInstall.sql:162`) und
`IX_Articles_ArticleNumber` (EF-Default aus `HasIndex("ArticleNumber")`) — korrekt. TS-93.4b in `docs/TESTSZENARIEN.md`
(0 CR, LF). `AppVersion.cs` Web + Service = 1.60.1. Anwender-Changelog v1.60.1-Karte (committet LF).

**5. Brain gelesen:** Spec-Rumpf (Loesungsentwurf Artikel, AK 11, Rueckfrage 4) nachgezogen; Freigabe-Block
unveraendert (kein Diff gegen HEAD); fallstricke §16 neuer Unterabschnitt (:1225); Changelog
[[2026-10-15-v1-60-1-artikel-view-wms-spalten]]; feature-map (:692); Testindex Zeile 93.

**Geaenderte Bestandstests:** `SageArticleSqlTests.Select_MitView_…` — Verhalten bewusst geaendert (AK 11, v1.60.1).
Neu: `LegacySql_IstZeichengleichMitDemBisherigenSelect`. Keine Fixture-Anpassung „bis gruen“.

**Nachweisart:** AK 11 — erzeugtes SQL automatisiert (`SageArticleSqlTests`); der echte View-Read ist **Manual-UAT**
TS-93.4b (nicht gruen gezaehlt). AK 10 (Legacy zeichengleich) — jetzt automatisiert durch den Hash-Waechter.

**Befunde (alle Minor, nicht behoben — Brain-Text, ausserhalb dieses Laufs):**
- N-1: Nachweis-Tabelle AK 11 (QA-Abschnitt v1.60.0) und Testindex Zeile 93 sagen noch „Spalten unverifiziert bis P4“ —
  durch diesen Abschnitt ueberholt.
- N-2: Changelog/Nachbesserung nennen nur P4a/P4b als offen; laut Spec bleiben auch **P4c** (Nummern 1:1 in
  `KHKArtikel`) und **P4e** (`PrimaryStorageLocation` = `KHKLagerplaetze.Kurzbezeichnung`) Vorbedingung.
- N-3: Deploy-Abschnitt ist am Diff `d4108f1a..2524bb03` finalisiert; fuer v1.60.1 unveraendert gueltig: Web ja
  (nur Version/Changelog), Service ja (`SageArticleSql.cs`), keine Migration, keine neue SQL-Datei.

**Deploy v1.60.1:** beide Publish-Befehle wie im Deploy-Abschnitt (Service traegt den Fix; Web nur fuer die
Versionsanzeige). Kein DB-Update.

**Manuelle Checkliste v1.60.1 (Mensch):**
1. Service (und Web) v1.60.1 aufs IDEAL-Testsystem publishen; Versionsanzeige 1.60.1 pruefen.
2. Vorher P4a (Dubletten), P4b (Gruppe als Code), P4c, P4e auf der Sage-Test-DB protokollieren.
3. `Sync:ArticlesViewName = [vw_IDEAL_AKE_WMS_Artikel]`, Lauf `Article` abwarten: erfolgreich, kein „Ungueltiger
   Spaltenname“, `gelesen` ≈ P4d.
4. Stichprobe 5 Artikel (Bezeichnung, Einheit, Gruppe als Code, Hauptlagerplatz, Matchcode) gegen Sage.
5. Negativ AKE: Key leer → Lauf `Article` wie bisher (Counts in der Groessenordnung der Vorlaeufe).
6. Danach mit der Checkliste oben ab Punkt 10 (TS-93.4 ff.) weiter.

## Nachbesserung v1.60.2 (2026-10-15, Dev-Lauf, Arbeitsplaetze)

**Befund (Gerald, IDEAL-Test):** Mit `Sync:ProductionWorkplaceSyncViewName = [vw_IDEAL_AKE_WMS_Arbeitsgaenge]` scheiterte
der Lauf mit „Ungueltiger Spaltenname Mandant/Aktiv/Arbeitsplatznummer/Bezeichnung1/USER_ArbeitsSchritt“. `sys.columns`:
`Arbeitsgangnummer`, `Bezeichnung`, `AG` (varchar). Zuordnung (Koordination, P3b): `Arbeitsgangnummer` → Arbeitsplatznummer,
`Bezeichnung` → Bezeichnung1, `AG` → USER_ArbeitsSchritt. **Ursache:** v1.60.0 hat fuer die View dieselben Spalten wie die
Tabelle angenommen — wieder eine Spalten-Annahme statt `sys.columns` ([[fallstricke]] §16).

**Behoben** im Bündel, Start `63524941` (HEAD geprüft), Commit `73b37e55` (v1.60.1 → **v1.60.2**, gemessen):
- `SageArbeitsplatzSql` (in `SageArbeitsplatzReader.cs`): zwei Lesewege. Quelle = `KHKPpsArbeitsplaetze` → `TableSql`,
  zeichengleich mit dem Select vor v1.60.0 (unqualifiziert, `WHERE Mandant = @mandant AND Aktiv = -1`, Parameter nur hier).
  Jede andere Quelle → `SELECT Arbeitsgangnummer, Bezeichnung, AG FROM {quoted}` ohne Filter. Gleiche Spaltenreihenfolge,
  dieselben DTO-Felder; Nummer weiter per `GetValue().ToString()`, getrimmt im Sync (`ProductionWorkplaceSyncService.cs:123`).
- **Entscheidung Tabelle vs. View über den Namen:** letztes Segment ohne Klammern, Gross/Klein egal, Schema egal
  (`KHKPpsArbeitsplaetze`, `[KHKPpsArbeitsplaetze]`, `[dbo].[KHKPpsArbeitsplaetze]` = Tabelle). Warum: Der Name ist das
  Einzige, was der Sync sicher kennt; eine Spaltenprüfung per `sys.columns` wäre ein zusätzlicher Roundtrip und eine zweite
  Wahrheit für genau einen bekannten Fall. Grenze: Die Tabelle wird im Default-Schema gelesen, ein konfiguriertes Fremd-Schema
  vor `KHKPpsArbeitsplaetze` ändert daran nichts (es gibt nur `dbo`).
- Tests `SageArbeitsplatzSqlTests` (6 Fälle): Tabelle in drei Schreibweisen = altes SQL zeichengleich (dauerhafter Wächter),
  View in drei Schreibweisen (inkl. `KHKPpsArbeitsplaetze_Kopie`) ohne Mandant/Aktiv und ohne Tabellen-Spalten.
- README: Schritt „Spaltennamen per `sys.columns` holen“ + P3b-Abfrage; TS-93.3b; Changelog; beide `AppVersion.cs`.

**Nachweis:** `dotnet build IdealAkeWms.slnx` → 0 Fehler; `dotnet test` → Web 2016 / 1 übersprungen (Bestand), Service
**288** (+6). Echter View-Read → TS-93.3b (Gerald, nach P3b).

**Bericht (nur zur Information, nichts geändert): Spalten, die der FA-Struktur-Sync liest** — für den Abgleich mit
`vw_IDEAL_AKE_WMS_FAListe` / `_FAInfos` per `sys.columns`. Zugriff per Spaltenname (`GetOrdinal`), Umwandlung tolerant per
`Convert.*` (`FaHierarchySql.cs:137-160`, `FaHierarchySyncService.cs:206-313`). Fehlt eine Spalte, scheitert der Lauf laut.

| View | Spalte (Name exakt) | gelesen als |
|---|---|---|
| FAListe | `HauptFA`, `SubFA` | int (NULL → 0) |
| FAListe | `VaterFA`, `Position` | int, NULL erlaubt |
| FAListe | `HauptArtnr`, `Artnr`, `Matchcode`, `Bezeichnung1`, `Bezeichnung2`, `Artikelgruppe`, `Hauptlagerplatz`, `BemerkungPN`, `FertigungsInfoPN`, `Kommissionieren`, `Arbeitsbereich`, `Arbeitsschritte`, `Artikeltyp`, `Material`, `VMBedarf` | Text |
| FAListe | `Sollmenge`, `Fertigungmenge`, `Breite`, `Hoehe`, `Tiefe` | decimal (NULL → 0) |
| FAListe | `Beschaffungsartikel`, `Beschichtet`, `EKBedarf` | Ja/Nein (`Ja`/`-1`/`1`/true = ja) |
| FAInfos | `HauptFA` | int (auch im EXISTS des FAListe-Selects) |
| FAInfos | `Abteilung` (Alias `MontageAbteilung`), `ABNr`, `Pos`, `Kunde`, `Status`, `Dienstleister`, `RAL`, `Bemerkung_Uhrzeit` | Text |
| FAInfos | `KO_Termin`, `FE_Termin`, `Start_Beschichtung`, `Beschichten_Retour`, `Neuer_PT_PPS`, `Verladetermin_Vsl` | Datum, NULL erlaubt |
| FAInfos | `Montagestunden` | decimal, NULL erlaubt |
| FAInfos | `Prio` | int, NULL erlaubt |

FAListe: 27 Spalten, FAInfos: 17. Schreibweise beachten: `Fertigungmenge` (ohne s), `Bemerkung_Uhrzeit`, `Verladetermin_Vsl`.


## QA Nachbesserung v1.60.2 (2026-10-15, qa-agent)

Diff `63524941..73b37e55` (1 Commit, 7 Dateien), Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Arbeitsbaum sauber.
**Verdikt: gruen, keine Critical/Important — Status bleibt Testbereit.** Code nicht geaendert.

**1. Build/Tests (selbst ausgefuehrt):** `dotnet build IdealAkeWms.slnx` → 0 Fehler (28 Warnungen, Bestand).
`dotnet test` → Web 2016 bestanden / 1 uebersprungen / 0 Fehler; Service 288 bestanden / 0 / 0.

**2. Review (selbst, in diesem Lauf).** Geprueft: `SageArbeitsplatzReader.cs` komplett, `FaHierarchySql.ValidateAndQuote`/
`SourceExistsAsync`, `ProductionWorkplaceSyncService.cs:46-140`, neuer Test, README-/TS-/Changelog-Diff.
- `TableSql` zeichengleich mit `git show d4108f1a:IDEALAKEWMSService/Services/SageArbeitsplatzReader.cs` Z. 24-29: beide
  Raw-Literale per `cat -A` verglichen, nach Abzug der Schliess-Einrueckung identisch (4 Zeilen, keine Trailing Blanks).
- `@mandant` nur bei `isTable` (`SageArbeitsplatzReader.cs:37-38`); View-SQL enthaelt keinen Parameter. Ordinal-Read 0/1/2
  passt zu beiden Selects; Nummer `GetValue().ToString()`, Bezeichnung/Kuerzel `GetString` (varchar ok; ein Nicht-Text-Typ
  scheitert laut, nicht still).
- `IsTable`: letztes Segment nach `Split('.')`, `Trim('[',']')`, OrdinalIgnoreCase. Grenzfaelle per Test: `[dbo].[KHKPpsArbeitsplaetze]`,
  `[khkppsarbeitsplaetze]` = Tabelle, `[KHKPpsArbeitsplaetze_Kopie]` = View. `Split('.')` ist sicher, weil
  `ValidateAndQuote` (`FaHierarchySql.cs:42-106`) je Segment nur `[A-Za-z0-9_-]` zulaesst (kein Punkt im Namen).
- Injection: `{quotedSource}` im View-SQL stammt ausschliesslich aus `ValidateAndQuote` (`ProductionWorkplaceSyncService.cs:61`,
  einziger Aufrufer `:75`). Kein neuer Eingabeweg.
- Sync-Service im Diff unberuehrt: Trimmung `:123-124`, Existenzpruefung im Reader vor dem Select, Skip-Pfad `:76-88`;
  Bestandstests `ProductionWorkplaceSyncServiceTests` gruen.
- Klassen aus der Defekt-Liste: keine Views/JS/Binder im Diff; kein stiller Fallback neu; die Tabelle/View-Regel existiert an
  genau einer Stelle (`SageArbeitsplatzSql`).
- FA-Spaltenliste im Abschnitt „Nachbesserung v1.60.2“ gegen `FaHierarchySql.cs:130-160` und `FaHierarchySyncService.cs:206-313`
  gegengeprueft: FAListe 27, FAInfos 17 (`Abteilung` mit Alias), Typen passend zu `Int32`/`IntN`/`Str`/`Dec`/`DecN`/`DateN`/`Bool`.
  Weitere View-Reads des FA-Syncs nur `COUNT_BIG(*)` (`:97-98`) — vollstaendig.

| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| Q2-1 | Minor | Deploy-Schritt 3 und Checkliste Punkt 9 verlangten noch „P3 = 0“; P3 laeuft auf der View mit „Ungueltiger Spaltenname“ (P3 ist oben als ueberholt markiert). | in dieser Spec korrigiert → P3b (Deploy ist QA-Pflicht) |
| Q2-2 | Minor | `docs/TESTSZENARIEN.md` TS-93.3 Satz 2 „Dann (P3 = 0) Key auf `[vw_…Arbeitsgaenge]`“ ist durch TS-93.3b ueberholt, steht aber unkommentiert; ebenso Testindex Zeile 93 „Umstellung nach P3“ und Spec-Stellen :61/:168/:206/:566/:637. | offen, nicht geaendert (Branch-Doku/Brain nur geprueft); beim naechsten Doku-Lauf auf P3b ziehen. Wer TS-93.3 ausfuehrt, nimmt P3b. |
| Q2-3 | Hinweis | P3b: Ist `Arbeitsgangnummer` in der View kein Texttyp (nur `AG` ist als varchar belegt), wandelt SQL Server im `EXCEPT` die gecastete Tabellenseite nach int zurueck — `0000` vs `0` faellt dann nicht auf. | Gerald: Typ von `Arbeitsgangnummer` aus dem `sys.columns`-Ergebnis ablesen; falls nicht varchar, auch die View-Seite `CAST(... AS varchar(31))`. Der Sync selbst ist typunabhaengig. |
| Q2-4 | Hinweis | Fremd-Schema vor `KHKPpsArbeitsplaetze` (z. B. `[x].[KHKPpsArbeitsplaetze]`): Existenzpruefung prueft `[x]…`, gelesen wird unqualifiziert im Default-Schema. | akzeptiert, in der Nachbesserung als Grenze dokumentiert; es gibt nur `dbo`. |

**3. Beweisart je AK (nur betroffene):** AK 6 Tabellenweg — SQL-Text automatisiert (`SageArbeitsplatzSqlTests`, Waechter);
echter Read beider Wege (Tabelle mit Mandant, View ungefiltert, Spaltennamen der View) = **Manual-UAT** TS-93.3/93.3b (raw SQL
auf Sage, InMemory nicht beweisbar). AK 7 (Skip/Warnung) unveraendert unit-getestet.

**4. Geaenderte bestehende Tests:** keine. Nur neu: `IDEALAKEWMSService.Tests/Services/SageArbeitsplatzSqlTests.cs` (6 Faelle).

**5. Checkliste:** keine Migration/SQL/FreshInstall/AgentJobs; keine Entity-Aenderung (Audit n/a); Version 1.60.2 in beiden
`AppVersion.cs`; Anwender-Changelog v1.60.2 vorhanden; TS-93.3b in `docs/TESTSZENARIEN.md` (LF, 0 CR); Brain-Changelog
[[2026-10-15-v1-60-2-arbeitsplatz-view-spalten]], feature-map, Testindex Zeile 93, Aufgaben-Notiz, [[fallstricke]] §16 Nachtrag
vorhanden und stimmig.

**Deploy v1.60.2:** web ja (nur `AppVersion.cs`, `Changelog.cshtml`), service ja (`SageArbeitsplatzReader.cs`, `AppVersion.cs`),
migration nein — Frontmatter unveraendert gueltig. Publish-Befehle und Reihenfolge wie im Abschnitt „Deploy“.

**Manuelle Checkliste v1.60.2 (Mensch, Schranke 2):**
1. Web + Service aus dem Worktree publishen, Versionsanzeige 1.60.2.
2. Mit Default `KHKPpsArbeitsplaetze`: Lauf `ProductionWorkplaceSync` erfolgreich, Counts wie die Vorlaeufe, Log-Text „aktive Arbeitsplaetze … (Mandant n)“.
3. `sys.columns` der View notieren (Typ von `Arbeitsgangnummer`, siehe Q2-3), dann P3b in beide Richtungen → leer.
4. Key `[vw_IDEAL_AKE_WMS_Arbeitsgaenge]` → Lauf erfolgreich, kein „Ungueltiger Spaltenname“, `neu = 0`, `abweichung = 0`, Werkbank-Liste unveraendert (Nummern mit fuehrenden Nullen).
5. Negativ: Key `[dbo].[KHKPpsArbeitsplaetze]` → wieder Tabellenweg mit Mandant-Filter, Counts wie Schritt 2.
- Browser-Abnahme 2026-10-21: [[2026-10-21-ideal-v1-59-bis-v1-64-browser-testbericht]] - 45 OK, 2 FEHLER, 1 nicht pruefbar, 0 Protokoll passt nicht
