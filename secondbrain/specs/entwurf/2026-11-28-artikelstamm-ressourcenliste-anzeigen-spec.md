---
type: spec
title: Artikelstamm – Ressourcenliste des Artikels als eigene Liste (live aus Sage)
slug: 2026-11-28-artikelstamm-ressourcenliste-anzeigen-spec
status: Entwurf
created: 2026-11-28
updated: 2026-11-28
source_backlog: "[[2026-11-28-artikelstamm-ressourcenliste-anzeigen]]"
task: ""
jira: WMS-10
worktree: ""
branch: ""
standort: beide
affected_code:
  - IdealAkeWms/Services/SageSdata/SageRessourcenlisteLeser.cs (neu – EIN Lesebaustein für die Ressourcenliste, aus SageTestController.LadeArtikelAsync :161-179 herausgezogen)
  - IdealAkeWms/Controllers/SageTestController.cs (LadeArtikelAsync nutzt den Leser; Konstanten RessourceListe/RessourcePositionen :158-159 wandern in den Leser)
  - IdealAkeWms/Controllers/ArticlesController.cs (neue GET-Aktion Ressourcenliste, Leseclient per [FromServices] – kein Ctor-Umbau)
  - IdealAkeWms/Models/ViewModels/ArtikelRessourcenlisteViewModel.cs (neu)
  - IdealAkeWms/Views/Articles/Ressourcenliste.cshtml (neu)
  - IdealAkeWms/Views/Articles/Info.cshtml (Knopf im Kartenkopf :77) und Edit.cshtml (Knopf neben „Artikelinfo“ :9)
  - IdealAkeWms/Services/ColumnFilterSyntax.cs (Register :76 – Eintrag „Articles/Ressourcenliste“ mit menge = Zahl)
  - IdealAkeWms.Tests/Services/ColumnFilterSyntaxTests.cs (clientOnly :260 um „Articles/Ressourcenliste“ ergänzen)
  - IdealAkeWms.Tests (Leser-Tests mit Fixture ressourcenliste-86003871.json, Controller-Tests)
  - IdealAkeWms/Views/Help/ (Hilfe Artikelstamm + Changelog), docs/TESTSZENARIEN.md (voraussichtlich TS-210)
open_questions:
  - "1. (Fachbereich: Benjamin Andl-Braun) Wo soll man die Ressourcenliste aufrufen? Vorschlag: Knopf „Ressourcenliste (Sage)“ in der Artikelinfo und in „Artikel bearbeiten“, der eine eigene Seite für genau diesen Artikel öffnet. Keine Gesamtliste aller Ressourcenlisten unter Stammdaten."
  - "2. (Fachbereich: Benjamin Andl-Braun) Welche Spalten? Vorschlag: Position, Ebene (Unterbaugruppen eingerückt), Typ (Material/Arbeitsgang), Ressourcenummer (Link zur Artikelinfo, wenn der Artikel im WMS ist), Matchcode, Menge, Einheit, Arbeitsgang, Unterbaugruppe; jede Spalte filterbar, ausblendbar und als CSV exportierbar. Teileart und Ist Lagerartikel nicht in dieser Liste (stehen in der Artikelinfo des verlinkten Teils, aber nur für Lagerartikel)."
  - "3. (Fachbereich: Benjamin Andl-Braun) Sollen Arbeitsgang-Zeilen der Ressourcenliste mit angezeigt werden oder nur Material (Teile)? Vorschlag: beides anzeigen, Typ-Spalte filterbar."
  - "4. (Fachbereich: Benjamin Andl-Braun) Hat ein Artikel in Sage mehrere Ressourcenlisten (Varianten), sollen alle angezeigt werden? Vorschlag: ja, jede Liste als eigener Block mit Variante, Status und Kennzeichen „Standard“."
  - "5. Live aus Sage lesen (jeder Seitenaufruf ein SData-GET, keine Migration, kein Dienst) statt Übernahme ins WMS per Dienst? Folge: In Artikelliste und Artikelinfo gibt es kein Kennzeichen „hat Ressourcenliste“ ohne Sage-Aufruf; der Knopf steht immer da, die Seite meldet „keine Ressourcenliste“. Empfehlung: live."
  - "6. Die Seite wäre das erste SData-Lesen der Web-App für Nicht-Admins (Rollen masterdata_read/masterdata; heute nur Admin-Werkzeuge). Gelesen wird mit dem hinterlegten Dienst-Zugang, nur GET, nur für Artikel, die im WMS-Artikelstamm stehen. Einverstanden?"
  - "7. Standort: beide ohne eigenen Schalter (Ressourcenlisten gibt es in beiden Sage-Mandanten, AKE belegt durch Fixture 86003871) oder nur IDEAL (Gate ProduktionsauftragHierarchisch)? Empfehlung: beide, ohne Schalter; ohne SData-Konfiguration meldet die Seite das."
  - "8. Gemeinsamer Lesebaustein mit [[2026-11-24-bf-auftrag-baugruppen-aufloesen-und-als-fa-zeigen-spec]]: Diese Spec baut SageRessourcenlisteLeser (alle Listen eines Artikels, Baum der Positionen); die BF-Spec nutzt ihn in Stufe B statt eines eigenen Abfrage-Teils in BfRessourcenleser. Reihenfolge: diese Spec zuerst, BF-Spec bekommt bei ihrer Überarbeitung den Verweis. Einverstanden?"
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

> [!info] Nummern nicht festgelegt
> Stand beim Schreiben (2026-11-28, Hauptcheckout `main`): `AppVersion.Version = "1.99.89"` (`IdealAkeWms/AppVersion.cs:5`),
> `docs/TESTSZENARIEN.md` bis **TS-209** (`:15085`). Voraussichtlich nächste freie Version und **TS-210** – beim Dev-Start gegen Worktree,
> `specs/` und `aufgaben/` prüfen (aus derselben Mail WMS-10 entstehen weitere Specs). Keine SQL-Nummer nötig.

## Ziel / Nutzen (das Warum)

Benjamin (IDEAL, Mail „WMS Anpassungen“ Punkt 2, zweiter Satz): „Außerdem sollen die Ressourcenlisten von dem Artikel angezeigt werden,
falls es eine gibt.“ Gerald: als **eigene, anzeigbare Liste**. Heute sieht man die Ressourcenliste eines Artikels nur im Admin-Werkzeug
*Einstellungen › Sage-Test › Stückliste live* (`SageTestController.cs:17` `[RequireAdminAccess]`). Wer im Artikelstamm arbeitet, soll ohne
Sage-Client sehen, woraus ein Artikel laut Sage besteht.

Fachlich wichtig ([[fallstricke]] `:1988-1989`, Glossar „Ressourcenliste (Vorlage)“ `glossar.md:193`): Die Ressourcenliste ist die **Vorlage**
des Artikels, **nicht** die Stückliste eines FA (80141025: 6 Ressourcen, FA 1059463: 13 Positionen). Die Seite sagt das sichtbar.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Neue Seite *Artikel › Ressourcenliste* für einen Artikel des WMS-Artikelstamms, live aus Sage gelesen (nur GET), mehrstufig.
- Einstieg per Knopf aus Artikelinfo und „Artikel bearbeiten“ (Rückfrage 1).
- Ein gemeinsamer Lesebaustein `SageRessourcenlisteLeser`, den auch *Stückliste live* (und später die BF-Auflösung, Rückfrage 8) nutzt.

**Out-of-Scope**
- „Leiste filterbar“ (erster Satz von Punkt 2) – eigene Backlog-Notiz „Artikelstamm Merkmale filterbar“.
- Übernahme der Ressourcenlisten ins WMS (Tabelle, Dienst, Kennzeichen in der Artikelliste) – nur falls Rückfrage 5 anders entscheidet.
- Schreiben/Pflegen der Ressourcenliste (über SData nicht möglich: [[sage-sdata-benutzerfelder-lesetest]] `:580-581`).
- FA-Stückliste, BF-Auflösung (BG → Einzelteile) – [[2026-11-24-bf-auftrag-baugruppen-aufloesen-und-als-fa-zeigen-spec]].
- Teileart/Ist Lagerartikel je Position (Rückfrage 2; nur Lagerartikel haben die Merkmale, BF-Spec Ist-Stand).

## Fachliche Anforderungen

**F1 Einstieg.** Artikelinfo (Kartenkopf, `Info.cshtml:77`) und „Artikel bearbeiten“ (`Edit.cshtml:9`) bekommen den Knopf
„Ressourcenliste (Sage)“ → `GET /Articles/Ressourcenliste?articleNumber=…`. Der Knopf steht immer da (Rückfrage 5: ohne Sage-Aufruf ist nicht
bekannt, ob es eine Liste gibt). Ein Sage-Aufruf passiert **nur** beim Öffnen der Seite, nie beim Laden der Artikelinfo.

**F2 Seite.** Kopf: Artikelnummer, Matchcode (aus dem WMS), Hinweis-Kasten „Ressourcenliste (Vorlage) aus Sage – nicht die Stückliste eines
Fertigungsauftrags“ (Text trägt die Bedeutung, nicht die Farbe; wie `SageTest/Stueckliste.cshtml:62-66`). Je gefundener Liste ein Block:
Überschrift mit Variante, Bezeichnung/Matchcode der Liste, Status, „Standard“ wenn `IstDefault` ≠ 0, Von-/Bis-Menge wenn ≠ 0; darunter die
Positionen als Tabelle, Unterbaugruppen eingerückt (Spalten nach Rückfrage 2, Arbeitsgänge nach Rückfrage 3).

**F3 Mehrere Listen.** Alle Listen des Artikels (Rückfrage 4), höchstens `MaxListen` (10) je Aufruf; liefert Sage mehr (`Gesamt` > Anzahl),
Banner „Sage meldet n Ressourcenlisten, angezeigt werden die ersten 10“ (Melden statt still).

**F4 Meldungen statt leerer Seite.**
- Artikel nicht im WMS-Artikelstamm → 404 (keine Sage-Abfrage für frei eingetippte Nummern).
- Artikelnummer mit `'`, `&` oder `#` → Hinweis, keine Anfrage (Regel wie `SageTestController.cs:139`, `where` wird als Text gebaut).
- SData nicht konfiguriert / Kennwort nicht entschlüsselbar → Banner mit den fehlenden Teilen (`SdataWebStand.NichtKonfiguriert`), keine Anfrage.
- Mandant nicht aus dem Dataset ableitbar (`SageTestAufbereitung.MandantAusDataset`) → Banner „SData:Dataset ohne Mandant“.
- Sage nicht erreichbar / HTTP-Fehler → Banner mit Diagnose (Texte wie `SageTestController.cs:151-154`), Eintrag im Log (Warnung).
- 0 Listen → Hinweis „Für Artikel X gibt es in Sage keine Ressourcenliste.“ (kein Fehler).
- Liste ohne Positionen → Hinweis im Block.

**F5 Verlinkung.** Ressourcenummer einer Position ist ein Link zur Artikelinfo (`Info?articleNumber=…`); steht der Artikel nicht im WMS,
zeigt die Artikelinfo ihre vorhandene Meldung „nicht gefunden“ (`ArticlesController.cs:292-295`) – kein Vorab-Abgleich nötig.

**F6 Stückliste live unverändert.** *Sage-Test › Stückliste live* zeigt im Artikel-Modus dasselbe wie heute, liest aber über den Leser. Neu
dort: ebenfalls alle Listen (statt nur der ersten, heute `count` 1 und `Zeilen[0]`, `SageTestController.cs:163-171`) – nur falls Rückfrage 4
„ja“; sonst bleibt der Admin-Modus bei der ersten Liste und der Leser bekommt die Anzahl als Parameter.

## Technischer Loesungsentwurf

### Ist-Stand (belegt)

| Befund | Beleg |
|---|---|
| Einzige Lesung der Ressourcenliste im Web: `PpsRessourcenListenInfo` (Vertrag `VertragResourcesSpecific`), `where Mandant eq {m} and Artikelnummer eq '{a}'`, `orderBy Artikelnummer`, `include PpsRessourcenPositionenResolvedInfo,SpecificUserfields`, `count` 1, nur `Zeilen[0]` | `SageTestController.cs:158-179` |
| Baum der Positionen über `RowGuid`/`RowGuidParent` mit Zyklus-Schutz, Fallback Einrückung nach `Ebene`; Zeilenmodell `StuecklistenZeile` (Position, Ebene, Ressourcetyp, Ressourcenummer, Bezeichnung = Matchcode ?? Bezeichnung1, Menge, Arbeitsgang, ArtikelnummerUBG, Platz) | `SageTestAufbereitung.cs:34-52, 182-208, 234-251` |
| Feldnamen der Positionen am AKE-Test: `Ressourcetyp` (`MA`/`AG`), `Ebene`, `RowGuidParent`, `Menge`, `IstGeloescht`, `Lagermengeneinheit`, `Arbeitsgangnummer`, `Matchcode`, `ArtikelnummerUBG`; Kopf: `Variante`, `IstDefault`, `Status`, `VonMenge`, `BisMenge`, `Matchcode` | Fixture `IdealAkeWms.Tests/Fixtures/SageSdata/ressourcenliste-86003871.json` |
| IDEAL (Prod, nur GET, L12): 80141025 → 1 Liste (Variante 1, `IstDefault 0`, Status G), 6 Positionen Ebene 1, Felder `Ebene, BelPosIdParent, Ressourcetyp, Ressourcenummer, Arbeitsgangnummer` | [[sage-sdata-benutzerfelder-lesetest]] `:1113` |
| `PpsRessourcenListenInfo` braucht `Mandant` im `where` (einziges Schlüsselfeld) | [[sage-sdata-benutzerfelder-lesetest]] `:370-371` |
| `strRessourcenlisteKopf`/`strRessourcenPosition` (Backlog) sind Sage-Business-Objekte der Masterdata-Liste, **nicht** die Quelle des WMS-Lesewegs; das WMS liest die Ressourcenliste ausschließlich über `PpsRessourcenListenInfo` | [[sage-sdata-masterdata]] `:51-53`; Negativbeleg: kein Treffer `strRessourcen` in `*.cs` (Suche 2026-11-28) |
| SData-Lesen in der Web-App heute nur in Admin-Controllern | `SageTestController.cs:17`, `SageSdataFaController.cs:21`, `SageFaArbeitsgaengeController.cs:18`; Zugang `SdataZugangsdaten.WebStandAsync` `:93-112` |
| `ArticlesController`: Klasse `[RequireMasterDataReadAccess]` (admin, masterdata_read, masterdata), Info liest nur WMS-Daten | `ArticlesController.cs:10, 273-384`; [[controller]] `:53-54` |
| `ArticlesController` wird in 4 Testdateien per `new` gebaut | `ArticlesControllerTests`, `ArtikelinfoLieferantenTests`, `FraesenPfadTests`, `PurchaseProposalsNachbesserungE4Tests` |
| Client-Mode-Tabellen mit Zahlspalten stehen im Register `ColumnFilterTypes` und in `clientOnly` des Drift-Tests | `ColumnFilterSyntax.cs:76`, `ColumnFilterSyntaxTests.cs:260` |

### Bausteine

1. **`SageRessourcenlisteLeser`** (`Services/SageSdata/`, statisch wie `SageFaArbeitsgaenge.LesenAsync` `SageFaArbeitsgaenge.cs:86`):
   `Task<RessourcenlistenErgebnis> LesenAsync(ISageSdataReadClient client, SdataLeseZiel ziel, string artikel, int mandant, int maxListen, CancellationToken ct)`.
   - Prüft die Artikelnummer (`'`, `&`, `#` → `ArgumentException` mit deutschem Text; Aufrufer zeigen ihn).
   - Ein `QueryPageAsync` mit der heutigen Abfrage aus `SageTestController.cs:163-164`, `count = maxListen`.
   - Ergebnis: `Gesamt`, `Aufruf` (URL, Zeilen, ms – wie `vm.Aufrufe`), je Liste `Kopf` (JsonElement für den Admin-Modus), `Variante`, `Matchcode`,
     `Status`, `IstDefault`, `VonMenge`, `BisMenge` und `Zeilen = SageTestAufbereitung.RessourcenBaum(...)` (vorhandener Baum, kein zweiter).
   - Wirft `SdataLeseException` weiter (Aufrufer entscheiden über die Meldung).
   - Positionen mit `IstGeloescht` ≠ 0: **anzeigen oder weglassen ist ungeklärt** (Fixture hat nur 0) → werden angezeigt und mit „gelöscht“
     markiert (Melden statt still); P-R2 zeigt, ob es an IDEAL vorkommt. `StuecklistenZeile` bekommt dafür `IstGeloescht` und `Einheit`
     (`Lagermengeneinheit`) – zwei Felder in `Zeile(...)` `SageTestAufbereitung.cs:234-251`.
2. **`SageTestController.LadeArtikelAsync`** ruft den Leser (F6); Konstanten `RessourceListe`/`RessourcePositionen` ziehen in den Leser.
3. **`ArticlesController.Ressourcenliste(string? articleNumber, [FromServices] ISageSdataReadClient client, [FromServices] IServiceSettingRepository serviceSettings, [FromServices] IConfiguration configuration, [FromServices] IDataProtectionProvider schutz, CancellationToken ct)`**
   – `[HttpGet]`, erbt `[RequireMasterDataReadAccess]`. `[FromServices]` statt Ctor-Parameter: die vier Testdateien bleiben unberührt.
   Ablauf: Artikel per `GetByArticleNumberAsync` (sonst 404) → `WebStandAsync` → Mandant → Leser (`MaxListen = 10`) → View. Kein Schreiben.
4. **View `Articles/Ressourcenliste.cshtml`**: Muster `SageTest/Stueckliste.cshtml` (Einrückung per `padding-left` nach `Tiefe`, Hinweis-Kasten,
   `<details>` „Aufrufe an Sage“), aber ohne Admin-Spalte „Alle Felder“ und ohne `_EinstellungenTabs`. Je Liste eine `table.filterable-table`
   mit `data-view-key="ArtikelRessourcenliste"`, Client-Spaltenfilter, Spaltenauswahl über `data-table-toolbar`, CSV-Export (vorhanden, `csv-export.js`).
5. **Knöpfe** in `Info.cshtml` und `Edit.cshtml` (`btn btn-outline-secondary`, wie „Artikelinfo“ `Edit.cshtml:9`).

### Ponytail
Wiederverwendet: SData-Client und Zugang, `RessourcenBaum`, `MandantAusDataset`, Hinweis-Kasten und Tabellenmuster aus *Stückliste live*,
Client-Spaltenfilter/Spaltenauswahl/CSV-Export. Weggelassen: Tabelle/Dienst/Cache (Rückfrage 5), Kennzeichen „hat Ressourcenliste“ in
Artikelliste/-info (bräuchte je Artikel einen Sage-Aufruf oder die Dienst-Kopie), eigene Gesamtliste unter Stammdaten (Rückfrage 1),
Merkmale je Position (Rückfrage 2), neuer Ctor-Parameter.

### Abstimmung mit der BF-Spec (Rückfrage 8)
[[2026-11-24-bf-auftrag-baugruppen-aufloesen-und-als-fa-zeigen-spec]] plant in `BfRessourcenleser` Schritt (1) dieselbe Abfrage mit
`count` ≥ 2 (dort Rückfrage 6). Der Leser hier liefert genau das (alle Listen bis `maxListen`, Baum, `Gesamt`). Die BF-Spec braucht danach
nur noch Schritt (2) (Artikelfelder der Kandidaten) und ihre Auflöse-Regel. Diese Spec ändert die BF-Spec nicht; der Verweis kommt bei deren
Überarbeitung (Rework) hinein.

## Migrations-/SQL-Auswirkungen

Keine. Keine Migration, kein `SQL/`-Skript, `SQL/00_FreshInstall.sql` und `SQL/AgentJobs/` unberührt (Lesen live aus Sage, nichts wird gespeichert).

## Einstellungen

keine. Genutzt werden die vorhandenen ServiceSettings `SData:BaseUrl`, `SData:Application`, `SData:ServiceContract`, `SData:Dataset` und der
SData-Zugang (`SdataZugangsdaten.WebStandAsync`), unverändert. Kein Feature-Schalter (Rückfrage 7 – falls „nur IDEAL“: Gate über den vorhandenen
Master-Schalter `ProduktionsauftragHierarchisch`, ebenfalls keine neue Einstellung).

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine – es wird nichts geschrieben. Nur Lesen in Sage (GET) und im WMS (`Articles`).

## Rollen und Zugriff

- Seite: `[RequireMasterDataReadAccess]` von der Klasse (admin, masterdata_read, masterdata). Keine neue Rolle, kein neuer Filter →
  `controller.md`/`RoleOverview.cshtml` nur um die Aktion ergänzen (Brain-Update im Dev-Lauf), keine Drei-Stellen-Pflicht.
- Neu ist, dass diese Rollen einen SData-GET auslösen (Rückfrage 6). Schutz: nur Artikel aus dem WMS-Stamm, Zeichenprüfung, nur Lesevertrag
  `…ResourcesSpecific`, kein frei wählbares `where`.

## Listen-View (ADR 0005)

Kein Server-Mode: Die Tabelle ist auf **einen** Artikel vorgefiltert, unpaginiert und klein (AKE 17, IDEAL 6 Positionen belegt) → Client-Mode
wie `SageTest/Stueckliste` und `BfAuftraege/Details`. Pflichten: alle `<th>` mit `data-col-key`, `data-filterable`, Menge mit
`data-filter-type="number"`; Register `ColumnFilterTypes.Lists["Articles/Ressourcenliste"] = L(("menge", N))` (`ColumnFilterSyntax.cs:76`) und
Eintrag in `clientOnly` (`ColumnFilterSyntaxTests.cs:260`). Keine Filterkarte/Pagination (eine Seite = ein Artikel).

## Prüfabfragen (Mensch bzw. Agent gegen Sage-**Test**, nie Prod)

Die Fixture ist AKE; für IDEAL ist nur 80141025 belegt (L12). Ergebnisse in diese Spec eintragen.

- **P-R1 Mehrere Listen je Artikel an IDEAL (Rückfrage 4):** Tabellen- und Spaltennamen sind **unbelegt** (Backlog nennt `KHKPpsRessourcenkopf`,
  der Lesetest `KHKPpsRessourcen` `:606`). Zuerst nachsehen, dann zählen:
  ```sql
  -- auf der IDEAL-Testdatenbank (Name des Testmandanten einsetzen)
  SELECT name FROM sys.tables WHERE name LIKE N'KHKPpsRessourcen%';
  SELECT TOP 0 * FROM dbo.<Kopftabelle aus Zeile 1>;   -- Spalten fuer Artikelnummer/Variante/Mandant ablesen
  SELECT <Artikelnummer-Spalte>, COUNT(*) AS Listen
  FROM dbo.<Kopftabelle> WHERE <Mandant-Spalte> = 1
  GROUP BY <Artikelnummer-Spalte> HAVING COUNT(*) > 1 ORDER BY Listen DESC;
  ```
  Alternativ ohne SQL im WMS-Testsystem IDEAL als Admin: *Sage-Test › Feldliste*, Vertrag `…ResourcesSpecific`, Ressource
  `PpsRessourcenListenInfo`, where `Mandant eq 1 and Artikelnummer eq '<Artikel>'`, orderBy `Artikelnummer`, Anzahl 5.
- **P-R2 Werte an IDEAL:** dieselbe Feldliste mit include-Positionen für eine BG mit Unterbaugruppe: Werte von `Ressourcetyp` (AKE `MA`/`AG`),
  kommt `IstGeloescht` ≠ 0 vor, ist `RowGuid`/`RowGuidParent` gefüllt (sonst Fallback Einrückung nach `Ebene`), heißt das Einheitenfeld auch
  `Lagermengeneinheit`. Antwort als IDEAL-Fixture sichern (ohne Zugangsdaten) – dieselbe Fixture braucht die BF-Spec (dort P-B3).

## Akzeptanzkriterien

1. Artikelinfo und „Artikel bearbeiten“ zeigen den Knopf „Ressourcenliste (Sage)“; das Laden der Artikelinfo löst **keinen** SData-Aufruf aus
   (Controller-Test mit Mock: `ISageSdataReadClient` in `Info` nicht aufgerufen).
2. Für einen Artikel mit Ressourcenliste zeigt die Seite je Liste einen Block mit Variante/Status/„Standard“ und alle Positionen; Positionen
   der Ebene 2 stehen eingerückt unter ihrer Unterbaugruppe (Leser-Test mit Fixture `ressourcenliste-86003871.json`: 17 Zeilen, 6 mit Tiefe 1).
3. Der Hinweis „Ressourcenliste (Vorlage) – nicht die Stückliste eines Fertigungsauftrags“ steht auf jeder Seite mit Ergebnis.
4. Liefert Sage mehr Listen als 10, erscheint das Banner mit der Gesamtzahl.
5. Artikel ohne Ressourcenliste → Hinweis „keine Ressourcenliste“, HTTP 200, kein Fehlerbanner.
6. Artikelnummer, die nicht im WMS-Artikelstamm steht → 404, kein SData-Aufruf.
7. Artikelnummer mit `'`, `&` oder `#` → Hinweis, kein SData-Aufruf.
8. SData nicht konfiguriert bzw. Mandant nicht ableitbar → Banner mit dem fehlenden Teil, kein SData-Aufruf.
9. `SdataLeseException` (nicht erreichbar / HTTP-Fehler) → Banner mit Diagnose, Seite bricht nicht ab, Warnung im Log.
10. Ressourcenummer verlinkt auf die Artikelinfo dieser Nummer.
11. Jede Spalte hat einen Client-Spaltenfilter (Menge als Zahl), ist ausblendbar und landet im CSV-Export; `ColumnFilterSyntaxTests` grün.
12. Ohne Rolle admin/masterdata_read/masterdata → Zugriff verweigert (Filter wie die übrige Artikelseite).
13. *Sage-Test › Stückliste live* (Artikel-Modus) zeigt nach dem Umbau dieselben Zeilen wie vorher (Regressionstest mit der Fixture), Admin-Spalte
    „Alle Felder“ bleibt dort.
14. AKE und IDEAL verhalten sich gleich (bzw. nach Rückfrage 7); Build und alle Tests grün.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (voraussichtlich **TS-210** „Artikelstamm: Ressourcenliste (Sage) anzeigen“) +
`secondbrain/tests/testszenarien-index.md`.
- **Vorbedingungen:** Testsystem IDEAL (bzw. AKE nach Rückfrage 7) mit eingerichtetem SData-Zugang; Benutzer R (nur masterdata_read), Benutzer O
  (ohne Stammdaten-Rolle); Artikel A mit Ressourcenliste inkl. Unterbaugruppe (IDEAL z. B. 80141025; AKE 86003871), Artikel B ohne Liste,
  Artikel C mit mehreren Varianten (aus P-R1, falls vorhanden).
- **Schritte:** 1 R öffnet Artikelinfo A → Knopf sichtbar, Seite lädt ohne Wartezeit auf Sage. 2 Knopf → eigene Seite, Hinweis „Vorlage“,
  Positionen, Unterbaugruppe eingerückt. 3 Spalte Typ auf „AG“ filtern, Menge „> 1“, Spalte ausblenden, CSV exportieren. 4 Link einer Ressourcenummer →
  Artikelinfo des Teils. 5 Artikel B → Hinweis „keine Ressourcenliste“. 6 Artikel C → mehrere Blöcke mit Variante. 7 Admin: *Stückliste live* A →
  unverändert. 8 „Artikel bearbeiten“ (Rolle masterdata) → Knopf führt auf dieselbe Seite.
- **Negativfälle:** O ruft `/Articles/Ressourcenliste?articleNumber=A` direkt → verweigert; Nummer, die nicht im WMS steht → 404; `X'1` → Hinweis;
  SData-Kennwort falsch (Testsystem) → Banner, Seite bleibt bedienbar; Sage-Dienst gestoppt → Banner mit Diagnose.
- **Automatisch:** Leser mit Fixture (Baum, Kopf-Felder, `Gesamt` > Anzahl, 0 Listen, Liste ohne Positionen, Zeichenprüfung); Controller (404,
  Banner-Fälle, kein Aufruf aus `Info`); Regressionstest `SageTestController` Artikel-Modus; `ColumnFilterSyntaxTests`.

## Etappen (nur bei epic: true)

Kein Epic.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt | – | – |

## Deploy

**Vorläufig – der qa-agent bestätigt gegen den echten Diff.**

- **Web-App:** ja (Controller, Leser, Views, Hilfe).
- **Service:** nein (kein Dienstcode berührt).
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): keine Pflichteinträge (kein SQL, keine Einstellung, kein Ordner). Falls Rückfrage 7 =
  „beide“ und SData an einem Standort noch nicht eingerichtet ist, ein Info-Eintrag:
  `@{ Id='<v>-RessourcenlisteSdata'; Version='<v>'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'; Text='Artikelstamm: Knopf „Ressourcenliste (Sage)“ liest live per SData. Ohne SData-Zugang (Service-Einstellungen > SData) zeigt die Seite nur einen Hinweis.' }`
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Offene Rueckfragen

1. **(Fachbereich: Benjamin Andl-Braun)** Wo soll man die Ressourcenliste aufrufen? **Vorschlag:** Knopf „Ressourcenliste (Sage)“ in der
   Artikelinfo und in „Artikel bearbeiten“, der eine eigene Seite für genau diesen Artikel öffnet. Keine Gesamtliste aller Ressourcenlisten unter
   Stammdaten.
2. **(Fachbereich: Benjamin Andl-Braun)** Welche Spalten? **Vorschlag:** Position, Ebene (Unterbaugruppen eingerückt), Typ (Material/Arbeitsgang),
   Ressourcenummer (Link zur Artikelinfo, wenn der Artikel im WMS ist), Matchcode, Menge, Einheit, Arbeitsgang, Unterbaugruppe; jede Spalte
   filterbar, ausblendbar und als CSV exportierbar. Teileart und Ist Lagerartikel nicht in dieser Liste (stehen in der Artikelinfo des verlinkten
   Teils, aber nur für Lagerartikel).
3. **(Fachbereich: Benjamin Andl-Braun)** Sollen Arbeitsgang-Zeilen der Ressourcenliste mit angezeigt werden oder nur Material (Teile)?
   **Vorschlag:** beides anzeigen, Typ-Spalte filterbar.
4. **(Fachbereich: Benjamin Andl-Braun)** Hat ein Artikel in Sage mehrere Ressourcenlisten (Varianten), sollen alle angezeigt werden?
   **Vorschlag:** ja, jede Liste als eigener Block mit Variante, Status und Kennzeichen „Standard“. Daten: P-R1.
5. Live aus Sage lesen (jeder Seitenaufruf ein SData-GET, keine Migration, kein Dienst) statt Übernahme ins WMS per Dienst? Folge: In Artikelliste
   und Artikelinfo gibt es kein Kennzeichen „hat Ressourcenliste“ ohne Sage-Aufruf; der Knopf steht immer da, die Seite meldet „keine
   Ressourcenliste“. **Empfehlung:** live.
6. Die Seite wäre das erste SData-Lesen der Web-App für Nicht-Admins (Rollen masterdata_read/masterdata; heute nur Admin-Werkzeuge,
   `SageTestController.cs:17`, `SageSdataFaController.cs:21`, `SageFaArbeitsgaengeController.cs:18`). Gelesen wird mit dem hinterlegten
   Dienst-Zugang, nur GET, nur für Artikel aus dem WMS-Artikelstamm. Einverstanden?
7. Standort: beide ohne eigenen Schalter (Ressourcenlisten gibt es in beiden Sage-Mandanten, AKE belegt durch Fixture 86003871) oder nur IDEAL
   (Gate `ProduktionsauftragHierarchisch`)? **Empfehlung:** beide, ohne Schalter; ohne SData-Konfiguration meldet die Seite das.
8. Gemeinsamer Lesebaustein mit [[2026-11-24-bf-auftrag-baugruppen-aufloesen-und-als-fa-zeigen-spec]]: Diese Spec baut `SageRessourcenlisteLeser`
   (alle Listen eines Artikels, Baum der Positionen); die BF-Spec nutzt ihn in Stufe B statt eines eigenen Abfrage-Teils in `BfRessourcenleser`.
   Reihenfolge: diese Spec zuerst, die BF-Spec bekommt bei ihrer Überarbeitung den Verweis. Einverstanden?

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

1. Einstieg: Knopf in Artikelinfo + Bearbeiten, eigene Seite je Artikel (Benjamin) →
2. Spalten der Liste, ohne Teileart/Ist Lagerartikel (Benjamin) →
3. Arbeitsgang-Zeilen mit anzeigen (Benjamin) →
4. Mehrere Ressourcenlisten (Varianten) alle anzeigen (Benjamin) →
5. Live aus Sage statt Übernahme per Dienst →
6. SData-Lesen für Stammdaten-Rollen (Nicht-Admin) →
7. Standort beide ohne Schalter oder nur IDEAL →
8. Gemeinsamer Leser, BF-Spec nutzt ihn, diese Spec zuerst →
