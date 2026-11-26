---
type: spec
title: BDE-Zahnrad – Spalten Verwendungsart, Ist Lagerartikel, Teileart
slug: 2026-11-26-bde-zahnrad-artikelfelder-spec
status: Entwurf
created: 2026-11-26
updated: 2026-11-26
source_backlog: "[[2026-11-26-bde-zahnrad-artikelfelder]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: beide
affected_code:
  - IdealAkeWms/Services/BdeTeilelisteService.cs (ArtikelZusatz, SpaltenZeile, ZusatzAsync, Summieren, TeilelisteAggregat)
  - IdealAkeWms/Services/BdeFaAngebot.cs (BdeFaSichtZeile, FaSichtAsync, Spalten)
  - IdealAkeWms/Views/BdeTeileliste/Index.cshtml
  - IdealAkeWms/Views/BdeTeileliste/Summiert.cshtml
  - IdealAkeWms/Views/BdeTeileliste/Fa.cshtml
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (BdeTeileliste, BdeTeilelisteSummiert, BdeTeilelisteFa)
  - IdealAkeWms/Models/FaHierarchyNode.cs (neu Verwendungsart, nur Teil B)
  - IdealAkeWms/Data/ApplicationDbContext.cs (MaxLength, nur Teil B)
  - IdealAkeWms/Migrations/<neu> AddFaHierarchyNodeVerwendungsart (nur Teil B)
  - IDEALAKEWMSService/Services/FaHierarchySql.cs (BuildNodeSelectSql, nur Teil B)
  - IDEALAKEWMSService/Services/SageFaStrukturReader.cs (ReadNodesAsync, nur Teil B)
  - SQL/172_AddFaHierarchyNodeVerwendungsart.sql (neu, Nummer vorlaeufig, nur Teil B)
  - SQL/00_FreshInstall.sql (nur Teil B)
  - IdealAkeWms/Views/Help/Index.cshtml, Views/Help/Changelog.cshtml, docs/anwender/17-bde-teileliste-werkbank.md
  - docs/TESTSZENARIEN.md
open_questions:
  - "Verwendungsart: Quelle ist die vorhandene, bisher nicht importierte Spalte Verwendungsart der Sage-View vw_IDEAL-AKE_Kommissionierung_FAListe (Stuecklisten-/FA-Position) – einverstanden? (Fachbereich: Walter)"
  - "Teileart: aus der FA-Struktur-Position (FaHierarchyNode.Artikeltyp, View-Spalte Artikeltyp) oder aus dem Artikelmerkmal USER_OSArtikeltyp? (Fachbereich: Walter)"
  - "Lagerbestell-Zeilen: Verwendungsart leer (–), Teileart und Ist Lagerartikel aus dem Artikel – einverstanden?"
  - "Summiert: Werte wie Material/Blechdicke als verschiedene Werte kommagetrennt anzeigen, Filter je Position vor dem Summieren – einverstanden?"
  - "Anzeige Ist Lagerartikel: Ja/Nein statt Sage-Rohwert -1/0 – einverstanden?"
  - "Sichtbarkeit: alle drei Spalten standardmaessig ausgeblendet (nur ueber das Zahnrad einblendbar) – einverstanden?"
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

## Ziel / Nutzen (das Warum)

Walter (IDEAL) will in der BDE-Teileliste sehen, wie ein Teil verwendet wird, ob es ein Lagerartikel ist und welche Teileart es hat,
ohne in Sage nachzuschlagen. Die drei Werte sollen als optionale Spalten im Zahnrad (Spaltenauswahl) der BDE-Ansichten waehlbar
und filterbar sein.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope** – nur die BDE-Ansichten der BDE-Teileliste (Entscheid Gerald 2026-11-26):
- Sicht **Einzeln** (`Views/BdeTeileliste/Index.cshtml`, inkl. Kinderzeilen der Baugruppen),
- Sicht **Summiert** (`Views/BdeTeileliste/Summiert.cshtml`, Aggregat- und aufgeklappte Einzelzeilen),
- **FA-Sicht** (`Views/BdeTeileliste/Fa.cshtml`).

Je Sicht drei neue optionale Spalten: **Verwendungsart**, **Ist Lagerartikel**, **Teileart**, jede mit Server-Spaltenfilter.

Gliederung in zwei Commits im selben Lauf (jeder fuer sich baut und testet gruen):
- **Teil A** – Teileart + Ist Lagerartikel: nur Web, keine Migration. Beide Werte liegen schon im WMS.
- **Teil B** – Verwendungsart: Migration, Dienst und Web. Der Wert liegt noch nicht im WMS. Haengt an Rueckfrage 1.

**Out-of-Scope / Abgrenzung**
- **FA-Struktur** (`FaHierarchy`-Ansichten, „alle Stuecklisten-Eigenschaftsfelder“): Das ueberarbeitet Gerald an anderer Stelle (Chat
  2026-11-26). Hier kommt **keine** Spalte in die FA-Struktur. Das neue Feld `FaHierarchyNode.Verwendungsart` aus Teil B steht dieser Arbeit
  danach zur Verfuegung, wird aber hier nicht angezeigt.
- Tageshistorie, Terminal-Liste (`BdeApiController`), Kommissionierlisten, Vormontage: keine Aenderung.
- SData-Weg der FA-Struktur (`SdataFaStrukturReader`): bleibt unveraendert, siehe „Bekannte Grenze“ unten.
- Kein Schreiben nach Sage.

## Fachliche Anforderungen

**Befund (2026-11-26, nur gelesen; Code Buendel-Worktree v1.99.81):**

1. *Wo die Spalten heute herkommen.* Einzeln/Summiert nutzen die Getter `BdeTeilelisteService.SpaltenEinzeln`
   (`BdeTeilelisteService.cs:201-218`) und `SpaltenZeile` (`:222-241`; Lagerbestell-Zeilen ueberschrieben ueber `Lager(...)`, `:226-235`).
   Summiert filtert alle Positionsspalten **vor** dem Summieren (`BdeTeilelisteController.cs:198-201`); auf dem Aggregat wirken nur
   `matchcode`/`sollmenge`/`anzahl` (`BdeTeilelisteService.cs:247-253`). Die FA-Sicht nutzt `BdeFaAngebot.Spalten` (`BdeFaAngebot.cs:158-182`),
   gefiltert vor der Pagination (`BdeTeilelisteController.cs:162-163`). Keine der drei Spalten gibt es heute.
2. *Teileart = Artikeltyp.* Die FA-Struktur speichert ihn schon: `FaHierarchyNode.Artikeltyp` (`FaHierarchyNode.cs:47`). Am IDEAL kommt er
   aus der Sage-View, Spalte `Artikeltyp` (`FaHierarchySql.cs:146`, `SageFaStrukturReader.cs:125,154`). Die Weiche `Sync:Quelle:FaStruktur`
   steht per Default auf View, SData ist gesperrt (`FaStrukturQuelleSwitch.cs:6-12,69-70`). Die Kopfdoku der View nennt als Herkunft
   „Position bevorzugt, Beleg als Fallback“ (`SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql:42`). Das ist **laut Anhang, nicht gegen den
   View-Text geprueft**: Der View-Text liegt nur in Sage (`:15-16,57`). Der SData-Weg nimmt den Artikeltyp dagegen fuer alle Knoten vom
   Artikel (`SdataFaStrukturReader.cs:265`, R4/ADR 0032).
   Als Artikelmerkmal gibt es ihn auch: `BfArtikelFelder.Artikeltyp = "USER_OSArtikeltyp"` (`BfArtikelFelder.cs:14`).
3. *Ist Lagerartikel* gibt es **nur** als Artikelmerkmal (`BfArtikelFelder.IstLagerartikel`, `BfArtikelFelder.cs:13`). Wichtig fuer die
   Anzeige: Die Nachlese liest nur Artikel mit `USER_OSIstLagerartikel ne 0` (`SdataArtikelReader.cs:336-337,365-367`). Artikel, die
   kein Lagerartikel mehr sind, verlieren alle Blechteile-Merkmale (`BlechteileFelderNachlese.cs:52-63`). Die Nachlese laeuft nur bei
   `Sync:BlechteileFelderNachlesen` = an (Default aus, `BlechteileFelderNachlese.cs:23`). **Folge:** Ein Merkmal ist da, wenn der Artikel ein
   Lagerartikel ist (Wert ≠ 0, ueblich `-1`). Kein Merkmal heisst „kein Lagerartikel“ **oder** „nie nachgelesen“. Dasselbe gilt fuer das
   Merkmal `USER_OSArtikeltyp`: Es fehlt bei jedem Nicht-Lagerartikel. Zahlen im Test-WMS `IDEAL_AKE_WMS_TEST`: 1287 Werte
   `USER_OSIstLagerartikel` und 1286 Werte `USER_OSArtikeltyp`. Das passt dazu (L10: 1.263 Lagerartikel, `SdataArtikelReader.cs:335`).
4. *Verwendungsart.* `KHKArtikel` hat **keine** Spalte `USER_OSVerwendungsart`, nur `USER_ELVerwendung` (Befund Sage `IDEAL`, 2026-11-26).
   Im Test-WMS gibt es kein Merkmal dafuer. Die Spalte steht an `KHKPpsRessourcenkopf/-Positionen`, `KHKPpsFaBelege`,
   `KHKPpsFaBelegePositionen` und `KHKPpsStlImport*`. **Die FA-Struktur-View traegt schon eine Spalte `Verwendungsart`**, die der WMS-Import
   bewusst nicht selektiert (`vw_IDEAL-AKE_Kommissionierung_FAListe.sql:13-14,45`; geprueft per INFORMATION_SCHEMA am 2026-08-11 am
   IDEAL-Testsystem). Der Node-Select liest sie nicht (`FaHierarchySql.cs:139-152`). Woraus die View sie bildet, ist **ungeprueft**
   (Pruefabfrage P-1).
   *Korrektur zur Backlog-Notiz:* Die 13 `USER_OS*`-Felder des SData-Wegs (`SdataFaStrukturReader.cs:165-170`) liest er am **FA-Kopf**
   (`dtsFaBelegsuche`, Praefix `KHKPpsFaBelege_`, `:159-163,199-205`). Es sind keine FA-Positionsfelder. Blaetter nehmen dort die Felder des
   Artikels (`:252`).
5. *Lagerbestell-Zeilen* sind nicht gespeicherte Projektionen mit `HauptFA = 0` und ohne FA-Position (`BdeTeilelisteService.cs:359-363`).
   Sie erscheinen nur, wenn der Artikel das Merkmal Arbeitsschritte traegt (`:349`). Laut Befund 3 sind das nur Lagerartikel.
6. *FA-Sicht.* Eine Zeile ist ein Arbeitsgang eines WMS-Auftrags. `Knoten` ist die Strukturzeile der Sub-FA. Am AKE und auf Haupt-FA-Ebene
   ist er `null` (`BdeFaAngebot.cs:12-15,116-119,143-146`). Die Artikelnummer ist `ProductionOrder.ArticleNumber` (`:165`).

**Anforderungen** (Annahmen nach den Empfehlungen ⭐ der Rueckfragen; die Freigabe kann sie aendern):

| Spalte (Col-Key) | Einzeln / Kinderzeilen (FA-Zeile) | Lagerbestell-Zeile | Summiert (Aggregat) | FA-Sicht |
|---|---|---|---|---|
| Teileart (`teileart`) | `Node.Artikeltyp` ⭐ | Merkmal `USER_OSArtikeltyp` des Artikels | verschiedene Werte, kommagetrennt | `Knoten?.Artikeltyp` |
| Ist Lagerartikel (`ist-lagerartikel`) | Merkmal `USER_OSIstLagerartikel` des Artikels (`Node.Artnr`): ≠ 0 → „Ja“, sonst „Nein“ ⭐ | wie links (immer „Ja“, Befund 5) | verschiedene Werte, kommagetrennt | Merkmal zu `ProductionOrder.ArticleNumber` |
| Verwendungsart (`verwendungsart`) | `Node.Verwendungsart` (Teil B) | leer („–“) ⭐ | verschiedene Werte, kommagetrennt | `Knoten?.Verwendungsart` |

- Leere Werte zeigen „–“, wie die Blechdicke (`Index.cshtml:136`). Getter = Zelle = Filter (fallstricke §26, ADR 0005).
- Alle drei Spalten sind im Zahnrad waehlbar und per Default ausgeblendet ⭐ (`DefaultHidden: true`).
- Am AKE: Die FA-Sicht hat die Spalten im Zahnrad, sie bleiben aber leer („–“), wie heute Material/Arbeitsschritte (`Knoten` null). Es
  gibt keine Fehlermeldung. Die Teile-Sichten Einzeln/Summiert gibt es am AKE nur bei einer Werkbank mit Teileliste.

## Technischer Loesungsentwurf

Ponytail-Leiter: Alle drei Werte laufen ueber vorhandene Wege (Artikel-Zusatz, Strukturknoten, Getter-Dictionaries). Neu ist nur das eine
Feld `Verwendungsart` am Knoten, weil der Wert sonst nirgends im WMS liegt.

**Teil A (Web):**
1. `ArtikelZusatz` (`BdeTeilelisteService.cs:35`) bekommt `IstLagerartikel` und `Artikeltyp`. `ZusatzAsync` (`:311-330`) nimmt
   `BfArtikelFelder.IstLagerartikel` und `BfArtikelFelder.Artikeltyp` in `felder` auf. Die Abfrage bleibt eine einzige, ohne
   Artikelnummernliste. Die Konstruktoraufrufe (`:324-325,328`, Tests) ziehen mit.
2. `SpaltenZeile` (`:222-241`):
   - `teileart`: FA-Zeile `z.Node.Artikeltyp`, Lagerbestell-Zeile `Zusatz(...).Artikeltyp`. Dafuer im `LagerZeilenAsync`-Node
     (`:359-363`) `Artikeltyp = z?.Artikeltyp` setzen; dann reicht ein Getter auf `Node.Artikeltyp` ueber `SpaltenEinzeln`.
   - `ist-lagerartikel`: `Zusatz(zusatz, z.Node.Artnr)?.IstLagerartikel` → „Ja“ bei Wert ≠ "0" (Regel wie `BfAuftragRepository.cs:64-65`),
     sonst „Nein“. Die Regel steht als **eine** statische Methode im Service. Zelle und Filter rufen sie beide auf.
3. Summiert: `TeilelisteAggregat` bekommt `Teilearten`, `Lagerartikel` und (Teil B) `Verwendungsarten` ueber
   `VormontageService.FormatTexte` aus denselben Gettern. Muster: `Materialien`/`Blechdicken`, `:494-495`. `SpaltenSummiert` bleibt
   unveraendert, die Filter wirken je Position vor dem Summieren (Controller `:198-201`).
4. FA-Sicht: `BdeFaSichtZeile` bekommt `string? IstLagerartikel`. `FaSichtAsync` (`BdeFaAngebot.cs:102-148`) laedt das Merkmal
   `IstLagerartikel` mit **einer** Abfrage ueber `ArticleAttributeValues` (`SyncSource = "Sage"`, wie `ZusatzAsync`). In `Spalten` kommen
   `teileart` (`Knoten?.Artikeltyp`), `ist-lagerartikel` und (Teil B) `verwendungsart` (`Knoten?.Verwendungsart`) hinzu.
5. Views: Je Sicht `<th data-filterable data-col-key="…">`, `<td>` ueber den Getter (`Model.Spalten[...]` bzw. `sp[...]`) und den Eintrag im
   Inline-`#column-config`-JSON. `ColumnDefinitions` (`:478-506`, `:548-576`, `:596-615`) ziehen identisch mit; `ViewKeyDriftGuardTests`
   prueft das. Summiert: Aggregatzeile und Einzelzeilen (`Summiert.cshtml:78-79,124-125`). Einzeln: auch die Kinderzeilen.
6. Spaltenfilter: Textfilter (Default). `ColumnFilterSyntax.cs:48-51` braucht keinen Eintrag (dort stehen nur Zahl/Datum).
7. Skill `frontend-design` ist Pflicht (Views geaendert). Es gibt nur Tabellenspalten, keinen neuen Stil.

**Teil B (Verwendungsart, nach Rueckfrage 1):**
1. `FaHierarchyNode.Verwendungsart` (`string?`, `HasMaxLength(200)` wie `Artikeltyp`, `ApplicationDbContext.cs:1151`). Migration
   `AddFaHierarchyNodeVerwendungsart`.
2. `FaHierarchySql.BuildNodeSelectSql` (`:139-152`) nimmt `f.[Verwendungsart]` auf. `SageFaStrukturReader.ReadNodesAsync` (`:116-163`)
   liest das Feld ueber `GetOrdinal`/`Str`.
3. **Schutz gegen eine fehlende View-Spalte:** Fehlt `Verwendungsart` in der produktiven View, wirft `GetOrdinal`, und der ganze
   FA-Struktur-Abgleich faellt aus. Darum vor dem Select einmal `COL_LENGTH(@view, 'Verwendungsart')` pruefen (Muster `ObjectExists`,
   `FaHierarchySql.cs:118-124`). Fehlt die Spalte, `NULL AS [Verwendungsart]` selektieren und **eine Warnung ins Aktivitaets-Protokoll**
   schreiben: „Sage-View ohne Spalte Verwendungsart – BDE-Spalte bleibt leer“. Melden statt still behandeln.
4. Der Abgleich uebernimmt das Feld ohne eigenen Code: `Uebernehmen` kopiert alle Eigenschaften per `SetValues`
   (`FaHierarchySyncService.cs:351-373`). **Folge beim ersten Lauf nach dem Deploy:** Jeder Knoten mit Verwendungsart zaehlt einmal als
   „geaendert“, und `SyncedAt` springt (`:366-370`). Das ist erwartet und gehoert in den Deploy-Hinweis.
5. Getter: `verwendungsart` in `SpaltenEinzeln` (`n.Verwendungsart`), Lagerbestell-Zeile → `null` ueber `Lager("verwendungsart", _ => null)`,
   FA-Sicht `Knoten?.Verwendungsart`.

**Bekannte Grenze (nicht mitbauen, Aufgabe anlegen):** Der SData-Weg (`SdataFaStrukturReader.FelderSetzen`, `:249-273`) setzt
`Verwendungsart` nicht. Wird `Sync:Quelle:FaStruktur` spaeter auf SData gestellt, leert `SetValues` das Feld. Blaetter haetten dort ohnehin
keine Quelle, denn sie nehmen Artikelfelder, und der Artikel hat keine Verwendungsart (Befund 4). Ein neues Userfeld im Katalog
`SageUserfelder` FaStruktur (`SageUserfelder.cs:58-70`, Keys `Sync:Userfeld:FaStruktur:*` in `ServiceSettingDefinitions.All` mit
Drift-Guard) braeuchte es erst dann. `VergleichsFelder` (`FaStrukturQuelleSwitch.cs:20-51`) bleibt ohne Verwendungsart, sonst meldet der
Vergleich jeden Knoten als Abweichung. → Folgeaufgabe in `secondbrain/aufgaben/` plus Eintrag in `fallstricke.md`.

**Pruefabfragen fuer den Menschen** (Sage `IDEAL` bzw. Testmandant, nur lesen). Die davon abhaengigen Aussagen oben sind als ungeprueft
markiert:
- **P-1 Herkunft Verwendungsart in der View:**
  `SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.[vw_IDEAL-AKE_Kommissionierung_FAListe]'));` – im Text nach `Verwendungsart` suchen
  (Position, Beleg oder Ressource?).
- **P-2 Spalte in der produktiven View vorhanden:**
  `SELECT COL_LENGTH(N'dbo.[vw_IDEAL-AKE_Kommissionierung_FAListe]', N'Verwendungsart');` – auf Test **und** Produktion (NULL = fehlt).
- **P-3 Werte und Fuellgrad:**
  `SELECT Verwendungsart, COUNT(*) AS Anzahl FROM dbo.[vw_IDEAL-AKE_Kommissionierung_FAListe] GROUP BY Verwendungsart ORDER BY Anzahl DESC;`
- **P-4 Teileart Position vs. Artikel (fuer Rueckfrage 2):**
  `SELECT COUNT(*) FROM dbo.[vw_IDEAL-AKE_Kommissionierung_FAListe] f JOIN dbo.KHKArtikel a ON a.Artikelnummer = f.Artnr AND a.Mandant = <IDEAL-Mandant> WHERE ISNULL(f.Artikeltyp,'') <> ISNULL(a.USER_OSArtikeltyp,'');`
  (0 = gleichgueltig, welche Quelle; > 0 = fachlich entscheiden).
- **P-5 Rohwerte Ist Lagerartikel im WMS:**
  `SELECT v.TextValue, COUNT(*) FROM ArticleAttributeValues v JOIN ArticleAttributeDefinitions d ON d.Id = v.ArticleAttributeDefinitionId WHERE d.SyncSource = 'Sage' AND d.SyncFieldName = 'USER_OSIstLagerartikel' GROUP BY v.TextValue;`
  (erwartet nur `-1` bzw. Werte ≠ 0, Befund 3).

## Migrations-/SQL-Auswirkungen

- **Teil A:** keine.
- **Teil B:** EF-Migration `AddFaHierarchyNodeVerwendungsart` (eine nullable Spalte, nicht destruktiv). `SQL/172_AddFaHierarchyNodeVerwendungsart.sql`
  (Nummer **vorlaeufig**: hoechste im Worktree ist `169`, `170`/`171` sind von [[2026-11-26-bde-nur-fa-meldung-entfernen-spec]] belegt; der
  Dev-Lauf prueft neu). Spalten-Guard `IF COL_LENGTH('dbo.FaHierarchyNodes','Verwendungsart') IS NULL ALTER TABLE … ADD [Verwendungsart]
  nvarchar(200) NULL;` im eigenen Batch, danach im separaten Batch der `__EFMigrationsHistory`-Insert mit Guard.
  `SQL/00_FreshInstall.sql` an beiden Stellen: Spalte in `CREATE TABLE FaHierarchyNodes` und `MigrationId`.
- `SQL/AgentJobs/`: nicht betroffen. Kein Agent-Job schreibt `FaHierarchyNodes` (Grep `FaHierarchyNodes` in `SQL/` trifft nur Schema- und
  Einmal-Skripte).
- Die Sage-View wird nicht geaendert (Sage-verwaltet).

## Einstellungen

keine. Es gibt keine neue oder geaenderte AppSetting oder ServiceSetting. Die Anzeige von Ist Lagerartikel und Teileart (Lagerbestell-Zeilen)
haengt von der vorhandenen `Sync:BlechteileFelderNachlesen` ab (Default aus). Das steht in der Hilfe, die Einstellung selbst bleibt
unveraendert.

## Audit-Feld-Auswirkungen

Keine. `FaHierarchyNode` ist bewusst keine `AuditableEntity`: Sie ist eine Cache-Tabelle, die Nachvollziehbarkeit kommt ueber SyncLog und
`SyncedAt` (`FaHierarchyNode.cs:8-10`). Die neue Spalte erbt das. Alle anderen Aenderungen sind reine Lesewege.

**Rollen/Zugriff:** unveraendert. Es gibt keine neue Aktion, die Spalten erscheinen in vorhandenen Ansichten hinter den bestehenden Filtern des
`BdeTeilelisteController`.

## Akzeptanzkriterien

1. Einzeln, Summiert und FA-Sicht bieten im Zahnrad je die Spalten „Verwendungsart“, „Ist Lagerartikel“ und „Teileart“ an. Bei einem
   Benutzer ohne gespeicherte Spaltenauswahl sind sie ausgeblendet.
2. Einzeln, FA-Zeile eines Blechteils: Teileart = `FaHierarchyNode.Artikeltyp` der Zeile. Ist Lagerartikel = „Ja“, wenn der Artikel das Merkmal
   `USER_OSIstLagerartikel` mit Wert ≠ 0 traegt, sonst „Nein“.
3. Einzeln, Kinderzeilen einer Baugruppe zeigen die drei Werte nach derselben Regel wie Elternzeilen.
4. Einzeln, Lagerbestell-Zeile: Teileart = Merkmal `USER_OSArtikeltyp`, Ist Lagerartikel = „Ja“, Verwendungsart = „–“.
5. Spaltenfilter (Server-Mode) auf jede der drei Spalten liefert genau die Zeilen, deren Zellentext passt. Pagination zaehlt nach dem Filter.
   Beispiel: Filter Ist Lagerartikel = `Nein` zeigt keine Lagerbestell-Zeile.
6. Summiert: Die Aggregatzeile zeigt die verschiedenen Werte der beitragenden Positionen kommagetrennt. Ein Filter auf eine der Spalten
   filtert je Position **vor** dem Summieren: Summe, Positionen und Hauptauftraege aendern sich entsprechend.
7. FA-Sicht (IDEAL): Teileart/Verwendungsart = Werte der Sub-FA-Strukturzeile. Ist Lagerartikel nach dem Merkmal der Auftrags-Artikelnummer.
   Haupt-FA-Zeilen ohne Strukturzeile zeigen „–“ bei Teileart/Verwendungsart.
8. FA-Sicht (AKE): Die drei Spalten sind waehlbar. Teileart/Verwendungsart sind „–“, die Seite laedt ohne Fehler.
9. Teil B: Nach einem FA-Struktur-Abgleich (View-Quelle) traegt `FaHierarchyNodes.Verwendungsart` den Wert der View-Spalte, und die
   BDE-Spalte zeigt ihn.
10. Teil B, negativ: Fehlt die View-Spalte (simuliert per Test auf den SQL-Baustein bzw. eine View ohne Spalte), laeuft der Abgleich durch.
    Die Verwendungsart bleibt leer, und das Aktivitaets-Protokoll hat **eine** Warnung mit dem Text aus dem Entwurf.
11. Die FA-Struktur-Ansichten zeigen keine neue Spalte (Abgrenzung).
12. `SQL/172` laeuft zweimal fehlerfrei (idempotent). Eine FreshInstall-DB hat die Spalte und die MigrationId.
13. `dotnet build` und `dotnet test` sind gruen. Neue Tests: Getter der drei Spalten fuer FA- und Lagerbestell-Zeile, Summiert-Aggregat,
    Ja/Nein-Regel, `BuildNodeSelectSql` mit und ohne Spalte (`FaHierarchySqlTests`), `ViewKeyDriftGuardTests`.

## Test-Szenarien

Neues Szenario in `docs/TESTSZENARIEN.md` (naechste freie TS-Nummer, Stand ≥ TS-203; der Dev-Lauf prueft), Index
`secondbrain/tests/testszenarien-index.md` nachziehen. Skizze:

- **Vorbedingungen:** DEV IDEAL; `Sync:BlechteileFelderNachlesen` an, Artikel-Abgleich gelaufen; FA-Struktur-Abgleich nach dem Deploy
  gelaufen (Teil B); Werkbank mit Teileliste und Option Lagerbestellungen; mindestens eine offene Lagerbestellung eines Lagerartikels mit
  Kuerzel.
- **Schritte:** (1) Einzeln: Zahnrad → drei Spalten einblenden; Werte an einem Blechteil, einer Baugruppe samt Kindern und einer
  Lagerbestell-Zeile mit Sage vergleichen (P-3/P-4). (2) Je Spalte filtern, auch `Nein` und einen Verwendungsart-Wert. (3) Summiert: Spalten
  einblenden, Aggregat mit mehreren Positionen pruefen, filtern, Summe vergleichen. (4) FA-Sicht: Spalten einblenden, Sub-FA- und
  Haupt-FA-Zeile pruefen. (5) Seite neu laden: Die Spaltenauswahl bleibt erhalten.
- **Negativ:** Nachlese aus → Ist Lagerartikel zeigt „Nein“ bzw. Teileart der Lagerbestell-Zeile „–“ (Hilfetext erklaert das). Artikel
  ohne WMS-Merkmal → „Nein“. AKE: FA-Sicht mit eingeblendeten Spalten laedt, Werte „–“.
- **Regression:** Bestehende Spalten und Filter (Blechdicke, AG-Status, Material) unveraendert; FA-Struktur ohne neue Spalte.

## Etappen (nur bei epic: true)

entfaellt (kein Epic). Gliederung in Teil A/B als zwei Commits, siehe Umfang.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

*Vorlaeufig, die QA bestaetigt gegen den echten Diff.*

- **Web-App:** ja (Teil A und B).
- **Service:** ja (Teil B: `FaHierarchySql`, `SageFaStrukturReader`). Ohne Teil B: nein.
- **Migration:** ja (Teil B, `SQL/172`). Reihenfolge: Schema vor dem Start von Web und Dienst. Beide lesen bzw. schreiben `FaHierarchyNodes`
  ueber das EF-Modell mit der neuen Eigenschaft; ohne Schema scheitert jede Abfrage auf die Tabelle. Kein DB-Backup-Sonderfall (additiv, nullable).
- Hinweis: Der erste FA-Struktur-Lauf nach dem Deploy meldet viele „geaendert“ (einmalig, Teil B, Punkt 4).
- **Manifest-Eintraege** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023):
  - Id `SQL/172_AddFaHierarchyNodeVerwendungsart`, Version = Release-Version, Typ `Schema`, Firma `beide`, Phase `Schema`, Ausfuehrung
    `automatisch`, Text „Spalte FaHierarchyNodes.Verwendungsart (BDE-Teileliste)“. Reihenfolge nach `SQL/170`/`171`, falls die im selben
    Release sind (Deploy-Reihenfolge, nicht Dateisortierung).
  - Id `Info/BdeVerwendungsartView`, Typ `Info`, Firma `IDEAL`, Phase `Nachher`, Text „Nach dem naechsten FA-Struktur-Lauf: Aktivitaets-Protokoll
    auf die Warnung ‚Sage-View ohne Spalte Verwendungsart‘ pruefen (P-2).“
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- Hauspflichten im Dev-Lauf: Version in beiden `AppVersion.cs` (naechste freie nach v1.99.81), `Views/Help/Changelog.cshtml`, Hilfe
  `Views/Help/Index.cshtml` (Abschnitt BDE-Teileliste: drei Spalten, Quelle, Abhaengigkeit von der Nachlese), `docs/anwender/17-bde-teileliste-werkbank.md`,
  Brain-Changelog, `feature-map.md`, Fallstrick „Verwendungsart nur ueber View-Weg“ plus Folgeaufgabe SData.

## Offene Rueckfragen

1. **Verwendungsart-Quelle** (Fachbereich: Walter): Am Artikel gibt es das Feld nicht (Befund 4). Vorschlag ⭐: die vorhandene Spalte
   `Verwendungsart` der FA-Struktur-View, also der Wert der Stuecklisten-/FA-Position, wie ihn die FA-Struktur liefert. Alternative: eine
   eigene Quelle (`KHKPpsFaBelegePositionen` oder Ressourcen-Stueckliste) per neuem Lesepfad. Das waere deutlich groesser. Welche Bedeutung
   meint Walter? Die Herkunft in der View klaert P-1.
2. **Teileart-Quelle** (Fachbereich: Walter): FA-Position (`FaHierarchyNode.Artikeltyp`, schon gespeichert) oder Artikelmerkmal
   `USER_OSArtikeltyp`? Vorschlag ⭐: FA-Position fuer FA-Zeilen, Artikelmerkmal nur fuer Lagerbestell-Zeilen. Grund: Das Merkmal fehlt bei
   allen Nicht-Lagerartikeln (Befund 3). P-4 zeigt, ob sich beide ueberhaupt unterscheiden.
3. **Lagerbestell-Zeilen:** Verwendungsart leer („–“), weil es keine FA-Position gibt, Teileart und Ist Lagerartikel aus dem Artikel. Vorschlag ⭐:
   so.
4. **Summiert:** Vorschlag ⭐: verschiedene Werte kommagetrennt (Muster Material/Blechdicke), Filter je Position vor dem Summieren.
   Alternative: die Spalten in Summiert nicht anbieten.
5. **Anzeige Ist Lagerartikel:** Vorschlag ⭐: „Ja“ (Merkmal ≠ 0) / „Nein“ (kein Merkmal oder 0) statt Rohwert `-1/0`. Einschraenkung: „Nein“
   steht auch bei nie nachgelesenen Artikeln (Nachlese aus).
6. **Default-Sichtbarkeit:** Vorschlag ⭐: alle drei per Default ausgeblendet, nur ueber das Zahnrad.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)
<!--
  Von der Koordination eingetragen, wortgetreu, je Antwort:
  <Nr>. <Name>, <yyyy-mm-dd>, [<Jira-Schluessel> Kommentar](<Link>): "<Antwort>"
-->

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Verwendungsart-Quelle (View-Spalte der FA-Struktur?) →
2. Teileart-Quelle (FA-Position oder Artikelmerkmal?) →
3. Lagerbestell-Zeilen (Verwendungsart leer, Rest vom Artikel?) →
4. Summiert (kommagetrennt, Filter je Position?) →
5. Anzeige Ist Lagerartikel (Ja/Nein?) →
6. Default-Sichtbarkeit (ausgeblendet?) →
