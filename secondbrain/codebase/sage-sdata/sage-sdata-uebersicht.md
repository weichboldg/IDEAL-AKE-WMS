---
type: codebase-karte
updated: 2026-10-30
quelle: "Sage-100-SData-Schnittstelle des AKE-Testservers, vollständig ausgelesen 2026-10-30 (Vertragsliste, $schema je Vertrag, Lesetests)"
---
# Sage 100 SData-Schnittstelle – Übersicht

Geschwister: [[sage-sdata-commonwawiservices]] · [[sage-sdata-api-vertraege]] · [[sage-sdata-masterdata]] · [[sage-sdata-realtimedata]] · [[sdata-2-0-spezifikation]] (Standard: Query-Syntax, Fehlercodes, Reliable Posting)
Einordnung ins WMS: [[integrationen]]. Anlass: [[2026-10-30-fa-rueckmeldung-an-sage]] (Befund im Anhang [[sdata-befund-ake-test-2026-10-30]]).

> [!warning] DPS 2026-11-04: `RealTimeData` und `MasterData` (die Sage-eigenen Elemente) sind **für Endanwender nicht zu verwenden und von
> Sage nicht freigegeben**. Rückmeldungen gehen über einen eigenen Partner-Service (kostenpflichtig). Siehe [[2026-11-03-dps-anfrage-produktion-sdata]].
> Seit 06.11. liefert der DPS-Lesevertrag `CommonWawiServicesResources` FA, Struktur, Positionen, Mitarbeiter, Lager und Artikel inkl.
> USER-Feldern → [[sage-sdata-commonwawiservicesresources]].
> **WMS-Gesamtkatalog (lesen + schreiben je Bereich):** [[sage-sdata-wms-schnittstellenkatalog]].

> Sage-Version laut Gerald (2026-11-06): **Sage 100 ERP 9.0.10** (für welche Installationen genau: AKE/IDEAL, Test/Produktion – nicht unterschieden).

## Zugang

| | AKE-Test | AKE-Produktion | IDEAL |
|---|---|---|---|
| Basis-URL | `https://sagetest01.ake.at:5493/sdata/ol` | laut `/ServiceSettings` `SData:BaseUrl` (nicht hier notieren) | **unbekannt** – eigener Sage-Server (`idealsql2…`), SData-Host offen |
| Dataset (Mandant) | `ake_TEST2026;1` (das `;1` gehört dazu) | `SData:Dataset` | offen |
| Auth | HTTP Basic, SData-Servicebenutzer (im WMS `SageLagerbuchung:Username/Password`, appsettings-only; seit v1.71.0 an **zwei Stellen**: Dienst-`appsettings.json` und Web-`appsettings.Production.json` – Passwortwechsel an beiden) | dito | offen |
| Zertifikat | Chrome meldet Zertifikatsfehler → Ausnahme je Browsersitzung; der WMS-Dienst (`HttpClient`) bucht trotzdem erfolgreich, Zertifikatskette also serverseitig vertraut, am PC nicht (für DEV relevant, [[2026-10-29-dev-umgebung-lokal]]) | | |

Credentials gehören nie ins Brain. Beispiele mit Zugangsdaten stehen nur in der Postman-Sammlung in
`backlog/anhaenge/2026-07-29-Postman-Lagerbuchungen/` (Benutzername im Sample, kein Kennwort).

## URL-Schema

```
{Basis}                                   Vertragsliste (Atom)
{Basis}/{Vertrag}                         Datasets des Vertrags
{Basis}/{Vertrag}/{Dataset}/$schema       XSD mit sme:-Annotationen (Rollen, Rechte, Labels, Pflichtfelder)
{Basis}/{Vertrag}/{Dataset}/{Ressource}   GET Liste; …/{Ressource}(Schlüssel) GET Einzelsatz; POST/PUT/DELETE je sme:can*
{Basis}/{Vertrag}/{Dataset}/$service/{Op} POST Service-Operation (Body = Request-Typ)
{Basis}/{Vertrag}/{Dataset}/{Ressource}/$service, /$queries, /$schema
```
Abfrageparameter: `count`, `startIndex`, `select=F1,F2`, `where=<Feld> eq <Wert>` (`eq, ne, gt, ge, lt, le, like, and, or`),
`orderBy`, `include=$children`, `format=json` (JSON `{"$resources":[…]}`; sonst Atom-XML mit `<sdata:payload>`).
Schema-Annotationen: `sme:role` (resourceKind | serviceOperation), `sme:canGet/Put/Post/Delete`, `sme:isMandatory`, `sme:label`,
`sme:maxLength`, `sme:path`, `sme:tags` (`ResourceType:=DataStructures | DataRecords | DataSources | DataValues`).

## Verträge (19)

| Vertrag | Version | Inhalt | Für das WMS |
|---|---|---|---|
| **CommonWawiServices** | 9.1.156 | 25 Service-Operationen: Lagerbuchung, Lagerumbuchung, Inventur, Belege VK/EK (anlegen/ändern/Feld setzen/sperren), Picklisten, Dispositionsstapel, Preisanpassung, Druck/Export (PDF als Base64), Etiketten, DMS, Healthcheck, Semaphoren | **genutzt** (`LagerbuchungService`); Kandidaten: `StandardBelegService` (EK-Bestellung/Wareneingang), `DispositionStapel*` (Bedarfe), `HealthcheckInfoService`, `ExportPrintBelegeService`, `PrintEtikettenBasicService` |
| CommonWawiServicesResources | – | generische Query-Ressourcen (Adressen, Artikel …) | `$schema` am AKE-Test **HTTP 500** („Ungültiger Objektname“) – defekt/unkonfiguriert |
| CommonWawiServicesResourcesSpecific | – | Sage-Kommissionierung (Picklisten-Items), Sammelmappe, Lizenz | nur wenn Picklisten je aus Sage kommen |
| apiArtikel.Sage.API | 1.0.0 | `eptArtikel` GPCD (Artikelstamm inkl. Meldebestand, Hauptlagerplatz, ~150 AKE-USER-Felder), Artikelgruppen | **Lesepfad für Artikel ohne Direkt-SQL**, Meldebestand schreibbar |
| apiBelegerfassung.Sage.API | 1.0.0 | `eptEKBeleg`/`eptVKBeleg` (G C D) + Belegsuche, Positionen | **Bestellungen lesen/suchen**, Neuanlage einfach |
| apiKunden.Sage.API / apiLieferanten.Sage.API | 1.1.0 | `eptKunden`/`eptLieferanten` GPCD + udf | **Lieferantenstamm** lesen (ohne Artikel-Lieferanten-Zuordnung) |
| **MasterData** | – | 790 AppDesigner-Datenstrukturen/-Datensätze: PPS (`strFaBeleg` GPCD+Execute, FA beenden, Ressourcenlisten, Arbeitsplätze/-gänge), Wawi (EK-Beleg GPCD+Execute, Lagerplatzbuchungen GPCD+Execute, Lagerplätze, Lager, Artikel-Lieferanten), System (Lieferanten-/Kundenstamm), Partner-Pakete | **interne Client-API**, `MethodName` undokumentiert, `strFaBeleg` → **403**; `rec…` ungeprüft |
| **RealTimeData** | – | 1219 Datenquellen, nur lesen: 79 PPS (FA-Übersicht/-Suche, Fertigmeldungen, Rückmeldungen, BDE-Logbuch, offene WE je FA), 491 Wawi (Bestellungen, offene WE, bestätigte Liefertermine, Lieferantenartikel, Bestände je Platz, Lagerbuchungen, Fehlbestände, Dispo), Partner (Logistik-FA, Meldebestand-Dispo) | **Abgleich WMS ↔ Sage lesend**, Ersatz für Direkt-SQL-Reads; parametrisierte Quellen (Struktur/Positionen/Fortschritt) liefern leer |
| apiDMSConnector.100036671.DPSBS_DMS_Connector | 9.0.8001 | Partner-DMS: Rechnungseingang, Einkaufsbelege mit WE/RE je Position, offene EK-Vorgänge, Lieferanten, Konten | Lesepfad „Bestellung→Wareneingang→Rechnung“ (Rechte ungeprüft) |
| ApplicationServerTest, authentication, ControlCenterData/-Designer/-Metadata, Database, DataContext, DataReferences, ErrorLogging | – | Sage-Client-Infrastruktur | nicht relevant; `authentication/$schema` antwortete nicht (Timeout) |

Sage-Partner = Nummer `100036671` (Pakete `AKE_*`, `DPSBS_*`, `IdealAKE_*`); zweiter Partner `050002003` (`AKE_Belege`, `IDEALAKE_DMSConnector`).

## Was die Schnittstelle kann und was nicht (Stand AKE-Test 2026-10-30)

| Bedarf | Weg | Status |
|---|---|---|
| Lagerbuchung Zugang/Entnahme/Umbuchung | `LagerbuchungService` | im WMS-Code seit v1.28.0 (produktiv eingebaut); am Testsystem getestet; am produktiven Sage-System **noch nicht von DPS lizenziert**, daher dort nicht im Einsatz (Gerald, 2026-11-03) |
| FA-Status, Rückmeldestand, Fertigmeldungen lesen | `RealTimeData` dtsFaUebersicht/dtsFaBelegsuche/dtsFertigmeldungen/dtsRueckKorrektur/dtsLagerRueckmeldungen | **verfügbar**, Lesetest OK |
| FA-Hierarchie lesen (Haupt-FA + Sub-FAs) | `RealTimeData` dtsFaBelegsuche `where=StrukturID eq <HauptFA>` | **verfügbar**, Lesetest OK (7 FAs für Struktur 835); im WMS als Anzeige „FA-Struktur (Sage live)“ (v1.71.0, [[2026-10-30-sage-sdata-fa-ansicht-spec]]); `or` im `where` funktioniert auf allen vier gelesenen Quellen |
| FA-Stückliste/Positionen lesen | `RealTimeData` **dtsTeileverwendungsnachweis `where=BelID eq <FA>`** (Ressourcetyp MA/AG/VS, Menge, Arbeitsgang); dtsFaBelegStruktur/dtsFaPositionen bleiben leer | **verfügbar**, Lesetest OK (11 Positionen für 835); ohne BelPosID/Soll-Ist je Position → für IDEAL-Felder weiter View `FAListe` |
| Artikelstückliste / Verwendungsnachweis | dtsTeileverwendungsnachweis `where=KopfArtikelnummer eq '<Artikel>'` bzw. `Artikelnummer eq '<Teil>'`; dtsRessourcenlisten (Köpfe, 28.806) | verfügbar; `MasterData/recArtikelstammStueckliste`, `strRessourcenlisteKopf`, `recArtikelstammRessourcenkopf` → **403** |
| Fertigmeldung, Materialentnahme zum FA, AG-Zeiten, FA beenden | kein Service; nur `MasterData/strFaBeleg` (PUT/Execute, undokumentiert) | **403, nicht nutzbar** → Sage-Partner |
| Bestellungen lesen (Kopf, Positionen, bestätigte Termine, offene WE, auch je FA-Position) | `apiBelegerfassung/eptEKBeleg(+Suche)`, `RealTimeData` dtsEKBelegMst/dtsEKPositionen/dtsOffeneEK_WE/dtsOffeneEK_WEFa/dtsLiefertermineBestaetigt/dtsOffenewareneingaenge | verfügbar (Lesetest offen) |
| Bestellung anlegen/ändern, Wareneingang buchen | `StandardBelegService` (Erfassungsart 4100, `BelegPositionLagermengen`), `UpdateBelegEntityService`, `BasisBelegService`, `eptEKBeleg` POST | verfügbar (Schreibtest offen, nur Test-Mandant) |
| Bedarf an den Einkauf geben | `DispositionStapelService` + `DispositionStapelPositionenService` (Lieferant*, MengeBasis*) | verfügbar (Test offen) |
| Lieferanten lesen | `apiLieferanten/eptLieferanten`, `MasterData/recLieferantenstamm` (147 Felder + ArtikelLieferanten) | verfügbar |
| Lieferanten-Artikel (Bestellnummer, Preis, WBZ, Mindestmenge) | `RealTimeData` dtsLieferantenArtikel, `MasterData/recArtikelLieferanten` | verfügbar |
| Artikel inkl. Meldebestand/Hauptlagerplatz | `apiArtikel/eptArtikel` | verfügbar, PUT möglich |
| Lagerplätze, Bestände je Platz, Lagerbuchungsprotokoll | `RealTimeData` dtsLagerplaetzestamm/dtsLagerplatzbestaende/dtsLagerplatzbuchungen | verfügbar → Ersatz für Direkt-SQL in `LagerplatzSyncService`, `LagerbestandSyncService`, `SageBuchungLookupReader` |
| Beleg-PDF, Etiketten (auch FA-Material) | `ExportPrintBelegeService`, `PrintEtikettenBasicService` | verfügbar |
| Erreichbarkeit prüfen | `HealthcheckInfoService` | verfügbar |

## Fallstricke

- **Serverlast:** Mehrere `$schema`-Abfragen in Serie (MasterData 2,5 MB, RealTimeData 5,9 MB, `authentication` hängt) haben die
  Abfragen aus Chrome minutenlang blockiert; Postman kam gleichzeitig durch. Eine Anfrage nach der anderen, Infrastruktur-Verträge auslassen.
- **Chrome-Zertifikatsausnahme** verfällt; Fehlerseite sieht aus wie „Server weg“.
- `format=json` wird vom Server respektiert, Postman zeigte trotzdem XML (Accept-Header) – egal für den Dienst.
- Sage-Booleans `-1`/`0` auch über SData (`IstEingelastet = -1`).
- `LagerumbuchungCompleteService` hat den Pfad `$service/LagerumbuchungComplete` (ohne „Service“).
- Datenquellen bringen alle USER_-Felder der Stammtabellen mit (bis +205 Felder) – immer `select` setzen.
- `MasterData`-Strukturen enthalten UI-Zustand (`Menu*`, `*Load`, `IsDirty`) – nicht als Datenmodell lesen.
- Rechte sind je Vertrag verschieden: `RealTimeData` 200, `MasterData` (str **und** rec: strFaBeleg, strRessourcenlisteKopf,
  recArtikelstammStueckliste, recArtikelstammRessourcenkopf) 403 mit demselben Benutzer. `MasterData` ist für den Servicebenutzer komplett zu.
- `count` wird nicht von jeder Datenquelle beachtet (`dtsTeileverwendungsnachweis` lieferte 237.122 Zeilen trotz `count=200`) – nie ohne
  `where` auf große Quellen.
- `where` mit String ohne Anführungszeichen → HTTP 500 (`dtsRessourcenlisten?where=Artikelnummer eq 86003871`); mit `'…'` 200.

## Offen

- IDEAL-Sage-Server: SData-Host, Dataset, Vertragsstand, Partner-Pakete (vermutlich andere).
- Lesetests `api*`-Verträge und `MasterData/rec…` mit dem Servicebenutzer.
- Parameterübergabe für dtsFaBelegStruktur/dtsFaPositionen/dtsFaFortschritt (Partner fragen).
- Produktions-Services (Fertigmeldung, Materialentnahme, FA beenden) → Anfrage Sage-Partner, siehe Fragenkatalog in
  [[2026-10-30-fa-rueckmeldung-an-sage]].
