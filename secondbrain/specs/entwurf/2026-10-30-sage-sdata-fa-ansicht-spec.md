---
type: spec
title: FA-Struktur und Stückliste live aus Sage per SData anzeigen (Machbarkeitsnachweis, nur lesen)
slug: 2026-10-30-sage-sdata-fa-ansicht-spec
status: Entwurf
created: 2026-10-30
updated: 2026-10-30
source_backlog: "[[2026-10-30-sage-sdata-fa-ansicht]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
version_vorschlag: "1.71.0"
testszenario_vorschlag: "TS-103"
affected_code:
  - "IdealAkeWms/Services/SageSdata/ISageSdataReadClient.cs (neu)"
  - "IdealAkeWms/Services/SageSdata/SageSdataReadClient.cs (neu)"
  - "IdealAkeWms/Services/SageSdata/SageSdataFaModels.cs (neu)"
  - "IdealAkeWms/Services/SageSdata/SageSdataFaTreeBuilder.cs (neu)"
  - "IdealAkeWms/Controllers/SageSdataFaController.cs (neu)"
  - "IdealAkeWms/Views/SageSdataFa/Index.cshtml (neu)"
  - "IdealAkeWms/Program.cs (AddHttpClient-Registrierung)"
  - "IdealAkeWms/appsettings.json (Abschnitt SageLagerbuchung mit leeren Platzhaltern)"
  - "IdealAkeWms/Models/AppSettingKeys.cs (SageSdataFaAnsichtAktiv)"
  - "IdealAkeWms/Models/Standort/StandortSettingsCatalog.cs (Schalter in GroupToggles)"
  - "IdealAkeWms/Models/ServiceSettingDefinitions.cs (nur Beschreibungstext SageLagerbuchungSslZertifikatPruefen)"
  - "IdealAkeWms/Services/SageTlsPolicy.cs (nur Doku-Kommentar)"
  - "IdealAkeWms/Views/Shared/_Layout.cshtml (Menüpunkt)"
  - "IdealAkeWms/Views/Help/Index.cshtml, Views/Help/Changelog.cshtml"
  - "IdealAkeWms/AppVersion.cs, IDEALAKEWMSService/AppVersion.cs"
  - "IdealAkeWms.Tests/Services/SageSdata/* (neu) + Fixtures/SageSdata/*.json (Kopie der Backlog-Anhänge)"
  - "docs/TESTSZENARIEN.md (TS-103), README.md (AppSettings-Tabelle)"
open_questions:
  - "1. Rückmeldestand je FA: (A) beim Seitenaufbau für alle FAs mitladen (2 Aufrufe je FA, bei Struktur 835: 14 zusätzliche), (B) nur auf Klick je FA-Knoten, oder (C) je Quelle EIN Aufruf mit or-verknüpftem where über alle BelIDs der Struktur (2 zusätzliche Aufrufe, Syntax ungeprüft, siehe Prüfabfrage P-3)?"
  - "2. Soll die Ansicht die Artikelstückliste (Herkunft 1, where=KopfArtikelnummer eq '<Artikel>') als Soll-Spalte neben dem FA-Beleg zeigen, oder reicht der FA-Beleg (Herkunft 2)?"
  - "3. Mengen: Menge aus dtsTeileverwendungsnachweis ist laut Backlog die Menge je Kopf-Einheit. Zusätzlich eine Spalte Bedarf = Sollmenge des FA × Menge anzeigen, oder nur Menge?"
  - "4. Listen-View-Pattern (ADR 0005): Positionstabellen dieser Strukturansicht als begründete Ausnahme ohne Pagination/Spaltenfilter/Spaltenauswahl/CSV (wie BOM-Tree), oder sollen sie filterable-table werden?"
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

# FA-Struktur und Stückliste live aus Sage per SData anzeigen

Quelle: [[2026-10-30-sage-sdata-fa-ansicht]] · Brain: [[sage-sdata-uebersicht]], [[sage-sdata-realtimedata]],
[[integrationen]], [[controller]], [[fallstricke]] · Idee: [[2026-10-30-fa-rueckmeldung-an-sage]] ·
Vorläufer-Client: [[2026-07-29-sage-lagerbuchungen-spec]].

## Ziel / Nutzen (das Warum)

Gerald will **sehen**, wie FA-Hierarchie und FA-Stückliste direkt aus Sage aussehen, ohne sie ins WMS zu übernehmen.
Danach entscheidet er, ob die `FaHierarchy`-Materialisierung (View `FAListe` → `FaHierarchyNodes`) entfallen kann. Den
Vergleich beider Darstellungen macht er selbst im Browser. Die Ansicht ist deshalb ein **Machbarkeitsnachweis zum
Lesen**: schlank, eigenständig, mit Diagnose, welche SData-Aufrufe was geliefert haben.

**Standort:** Die Funktion hängt an keinem Standort. Sie ist weder vom Master `ProduktionsauftragHierarchisch` noch von
OSEON abhängig. Testbar ist sie nur gegen den **AKE-Sage-Test** (`sagetest01.ake.at:5493`, Dataset `ake_TEST2026;1`) über
das Testsystem akenet01.ake.at:4445 (IDEAL-Version mit AKE-Konfiguration). Für IDEAL ist der SData-Host unbekannt
([[sage-sdata-uebersicht]] Abschnitt „Zugang“, „Offen“). Auf idealweb01 ist die Funktion deshalb nicht testbar und bleibt
dort per Schalter aus.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Lese-Client `ISageSdataReadClient` (typed `HttpClient`) für den Vertrag `RealTimeData`, nur GET.
- Eigene schlanke Modelle mit genau den angezeigten Feldern.
- Reiner Baumaufbau Haupt-FA → Sub-FAs → Positionen inkl. sichtbarer Meldung bei mehrdeutiger oder fehlender Zuordnung.
- Neue Read-only-Seite „FA-Struktur (Sage live)“: Eingabe einer FA-Nummer (BelID), Baum, Rückmeldestand (Umfang je
  Rückfrage 1), einklappbarer Diagnose-Bereich mit Rohdaten.
- Feature-Gate AppSetting `SageSdataFaAnsichtAktiv` (Default aus) + Menüpunkt nur bei aktiv.
- Unit-Tests mit den beiden Backlog-Anhängen als Fixtures; Testszenario TS-103.

**Out-of-Scope (Nicht-Ziele, ausdrücklich)**
- **Keine Persistenz, keine Migration, keine neue Tabelle**, kein Sync, keine Materialisierung.
- **Kein Schreiben an Sage.** Der Client kennt nur `HttpMethod.Get`.
- **Keine Änderung** an `FaHierarchySyncService`, `FaHierarchyNode`, `FaHierarchyTreeBuilder`, `FaHierarchyController`
  oder den bestehenden FA-Struktur-Views (`Views/FaHierarchy/*`, `fa-liste-gruppierung.js`). Keine Wiederverwendung
  dieser Darstellung: sie hängt an `FaHierarchyNode`-ViewModels, das wäre die Kopplung, die der Backlog ausschließt.
- **Kein neuer ServiceSettings-Key.** `SData:ReadTimeoutSekunden` wird **nicht** angelegt: Timeout fest 20 s bei der
  Registrierung (Backlog-Vorgabe). Erst anlegen, wenn 20 s im Betrieb nachweislich nicht passen; dann in
  `ServiceSettingDefinitions.All` (Drift-Guard `ServiceSettingDefinitionsTests`).
- **Kein MemoryCache** (Backlog: optional). Grund: Neuladen ist hier der Testfall, und ein Cache würde die Diagnose
  (Aufrufe, Dauer) verfälschen. Nachrüsten, wenn Sage-Last messbar wird.
- IDEAL-Felder der View `FAListe` (Kommissionieren, Arbeitsbereich, Beschichtet …), Verwendung in Kommissionierung/BDE,
  Verlinkung aus bestehenden Ansichten.
- Neue Rolle (keine).

## Fachliche Anforderungen

Belegte Fakten zu Sage (Quelle: Backlog-Anhänge, aufgezeichnet 2026-10-30 am AKE-Test, und [[sage-sdata-realtimedata]]):

| Fakt | Beleg |
|---|---|
| `dtsFaBelegsuche.Sage.PPS?where=StrukturID eq 835` liefert 7 FAs (835 + 836–841) | Anhang `dtsFaBelegsuche-struktur-835.json` Z. 2, 5–11 |
| Haupt-FA: `BelID = StrukturID`; Sub-FAs `VerursacherTyp = "FA"`. **`VerursacherTyp` des Haupt-FA ist `MAN` (manuell) ODER `VK` (aus Verkaufsauftrag) – im Test-Mandanten 12 von 14 Wurzeln `VK`, nur 835 und 819 `MAN`** | ebd. Z. 5 bzw. 6–11; Prüfabfrage Koordination 2026-10-30 (alle 35 FAs) |
| Sage-Booleans kommen als `-1`/`0` (`IstEingelastet: -1`, `IstBegonnen: 0`) | ebd. Z. 5; [[sage-sdata-uebersicht]] Fallstricke |
| Termine kommen **mit** Offset (`2026-02-03T08:45:00+01:00`) | ebd. Z. 5 |
| Termine kommen laut Brain auch **ohne** Offset (`2026-02-03T00:00:00`) | [[sage-sdata-realtimedata]] Z. 14 |
| `dtsTeileverwendungsnachweis.Sage.PPS?where=BelID eq 835` → 11 Positionen (10 × MA, 1 × AG „999 TESTFertigung“), `Herkunft 2` | Anhang `dtsTeileverwendungsnachweis-belid-835-836.json` Z. 6–16 |
| BelID 836 → 1 Position (MA `00722001`, `Menge 0.6`, Dezimalwert) | ebd. Z. 21 |
| `Ressourcetyp`: MA = Material, AG = Arbeitsgang, VS = Verschnitt; `Herkunft` 1 = Artikelstückliste (BelID null), 2 = FA-Beleg | ebd. Z. 3 (`_aufgezeichnet`) |
| `Ressourcenummer`, `Arbeitsgangnummer`, `Arbeitsplatznummer` sind **Strings** (`"999"`, `"9999"`) | ebd. Z. 16 |
| Echte Antworten tragen zusätzlich `$url`, `$updated`, `$descriptor` | beide Anhänge Z. 3 |
| Numerischer `where`-Wert ohne Anführungszeichen funktioniert (`StrukturID eq 835`, `BelID eq 835`) | beide Anhänge Z. 2 |
| String-`where` ohne `'…'` → HTTP 500 | [[sage-sdata-uebersicht]] Fallstricke |
| `count` wird bei `dtsTeileverwendungsnachweis` ignoriert, ohne `where` 237.122 Zeilen | [[sage-sdata-realtimedata]] Z. 91 |
| Vater-Kind ist **kein** Feld; MA-Position = Sub-FA, wenn `Ressourcenummer` = `Artikelnummer` eines Sub-FA derselben Struktur | Backlog „Was Sage liefert“; im Fixture zutreffend: 6 MA-Positionen von 835 = Artikel von 836–841 |
| `dtsRueckKorrektur`/`dtsFertigmeldungen` lieferten im Test-Mandanten leere Listen | Backlog Punkt 3; [[sage-sdata-realtimedata]] Z. 20–21 |

Anforderungen:

- **F-1 Eingabe:** Ein Feld „FA-Nummer (Sage-BelID)“, nur ganze Zahl > 0, per GET `?belId=<n>` (URL teil- und
  bookmarkbar). Ein Weg für Haupt- **und** Sub-FA: erst `dtsFaBelegsuche?where=BelID eq <n>&select=BelID,StrukturID`,
  dann die Struktur über die gefundene `StrukturID`. Damit entfällt die Unterscheidung „HauptFA oder BelID“ (ein
  Haupt-FA hat `BelID = StrukturID`). Wurde eine Sub-FA eingegeben, zeigt die Seite den Hinweis
  „BelID 836 gehört zur Struktur 835 – angezeigt wird die ganze Struktur“.
- **F-2 Struktur:** `dtsFaBelegsuche?where=StrukturID eq <s>&orderBy=BelID&select=<FA-Felder>`. Je FA angezeigt:
  BelID, Artikelnummer, Matchcode, VerursacherTyp, Sollmenge, Fertigungmenge, Istmenge, Status (Eingelastet,
  Begonnen, Beendet, Gesperrt aus `IstEingelastet`/`IstBegonnen`/`IstBeendet`/`IstGesperrt`), Sollstarttermin,
  Sollendtermin, GeaendertAm. Die Status-Beschriftung ist der Sage-Feldname ohne „Ist“, also keine eigene Deutung.
  `IstGesperrt` ist **nicht** im Fixture enthalten, Wertebereich daher ungeprüft (Prüfabfrage P-1).
- **F-3 Positionen je FA:** `dtsTeileverwendungsnachweis?where=BelID eq <BelID>&select=<Pos-Felder>` für **jede** FA der
  Struktur. Angezeigt: Ressourcetyp (Badge mit Text MA/AG/VS), Ressourcenummer, Matchcode, Menge, Arbeitsgangnummer,
  Arbeitsplatznummer, Materialteileart, Beendet. AG-Positionen sind als eigener Typ erkennbar (Badge-**Text** „AG“ und
  Arbeitsgang/Arbeitsplatz-Spalten, nicht nur Farbe). Reihenfolge = Antwortreihenfolge von Sage. Eine Positionsnummer
  gibt es in dieser Quelle nicht ([[sage-sdata-realtimedata]] Z. 89: „Fehlt … BelPosID“).
- **F-4 Zuordnung Sub-FA:** Eine MA-Position einer FA der Struktur verweist auf eine Sub-FA, wenn ihre `Ressourcenummer`
  der `Artikelnummer` **genau einer** Sub-FA entspricht **und** diese Artikelnummer bei **genau einer** MA-Position der
  Struktur vorkommt. Dann zeigt die Position „→ Sub-FA <BelID>“ (Sprungmarke), und die Sub-FA hängt eine Ebene tiefer.
  Die Regel gilt für Positionen **jeder** FA der Struktur, nicht nur des Haupt-FA. Damit funktionieren auch mehrstufige
  Strukturen; im Fixture gibt es nur eine Ebene. Diese Verallgemeinerung ist eine Design-Annahme und wird mit
  Prüfabfrage P-2 gegen die echte Sage-Verknüpfung (`BelIDNachfolger`) abgeglichen.
- **F-5 Melden statt still zuordnen:** Sichtbare Warnung (`alert-warning`, Text), keine stille Zuordnung, wenn
  a) zwei oder mehr Sub-FAs dieselbe Artikelnummer haben („Sub-FAs 837, 842 haben denselben Artikel 86003866 –
  Zuordnung nicht eindeutig“) **oder** dieselbe Artikelnummer bei mehreren MA-Positionen vorkommt,
  b) eine Sub-FA keiner Position zugeordnet werden kann,
  c) die Struktur keinen Haupt-FA (`BelID = StrukturID`) hat. *(Nachgezogen Koordination 2026-10-30: `VerursacherTyp` des
  Haupt-FA ist **kein** Kriterium – `MAN` und `VK` sind beide normal; er wird nur angezeigt.)*
  Betroffene Sub-FAs erscheinen dann unter einer eigenen Überschrift „Nicht eindeutig zugeordnete Sub-FAs“ am Ende,
  mit ihren Positionen. Nichts wird weggelassen.
- **F-6 Rückmeldestand:** je FA aus `dtsRueckKorrektur` (Anzahl, Σ `MengeGut`) und `dtsFertigmeldungen` (Anzahl,
  Σ `MengeGut`). Beide werden **getrennt** angezeigt und nicht addiert. Ob Fertigmeldungen eine Teilmenge der
  Rückmeldungen sind, ist nicht belegt (beide tragen `RueckmeldungID`, [[sage-sdata-realtimedata]] Z. 62–64). Ist nichts
  vorhanden, steht dort „keine Rückmeldung“. Lade-Zeitpunkt und Aufrufzahl je **Rückfrage 1**. Feldtypen ungeprüft,
  weil keine Fixture mit Daten existiert (Prüfabfrage P-4).
- **F-7 Diagnose (einklappbar, `<details>`):** je Aufruf: URL (ohne Zugangsdaten), HTTP-Status, Dauer in ms,
  Zeilenzahl, ggf. Fehlertext. Je Aufruf ein eigenes `<details>` „Rohdaten anzeigen“ mit dem JSON-Body, gekürzt auf
  4000 Zeichen wie `SageLagerbuchungClient.Cap` (IDEALAKEWMSService/Services/SageLagerbuchungClient.cs:77-78).
- **F-8 Nicht konfiguriert:** Fehlt `SData:BaseUrl`, `SData:Dataset` oder `SageLagerbuchung:Username`, zeigt die Seite
  `alert-info` „Sage-SData nicht konfiguriert: <fehlende Keys>“, setzt **keinen** Aufruf ab und wirft keinen Fehler.
- **F-9 Fehlerfälle sichtbar:** Nicht erreichbar/Timeout/TLS-Fehler (Status 0 + Exception-Text), 401/403/500 (Status +
  gekürzter Body), leere Struktur („BelID <n> in Sage nicht gefunden“) erscheinen als Banner **und** in der Diagnose.
  Ein Fehler bei den Positionen einer FA bricht die Seite nicht ab: Die FA zeigt „Positionen nicht geladen (HTTP 403)“,
  die übrigen werden angezeigt.
- **F-10 TLS-Hinweis:** Ist `SageLagerbuchungSslZertifikatPruefen = false`, zeigt die Seite einen Warnbanner (analog
  `SageBookingQueueController`, Controllers/SageBookingQueueController.cs:77-83).

## Technischer Loesungsentwurf

**Bestand, der wiederverwendet wird (ponytail-Sprosse 2):**
- `SdataUrlBuilder.BuildResourceUrl(baseUrl, application, serviceContract, dataset, relativeResource)`
  (IdealAkeWms/Services/SdataUrlBuilder.cs:26-39): `;` im Dataset und `$` bleiben literal, `/sdata` wird nicht
  verdoppelt. `ServiceContract` = Konstante `"RealTimeData"`, **nicht** der ServiceSetting `SData:ServiceContract`.
  Der steht auf `CommonWawiServices` (Models/ServiceSettingDefinitions.cs:56) und gehört der Lagerbuchung.
- `SageTlsPolicy.SettingKey` / `ShouldVerifyCertificate` (IdealAkeWms/Services/SageTlsPolicy.cs:15, 22-23), fail-safe.
- Muster der typed-HttpClient-Registrierung mit TLS-Callback, das den Schalter **zur Laufzeit** liest, aus dem Dienst:
  IDEALAKEWMSService/Program.cs:98-122. Basic-Auth wie IDEALAKEWMSService/Services/SageLagerbuchungClient.cs:49-51.
  Fehlerbehandlung (kein Throw bei HTTP-Fehler, `OperationCanceledException` durchreichen, sonst fangen) wie ebd.
  Z. 53-74.
- Konfiguration wie `SageBookingWorker` (IDEALAKEWMSService/Workers/SageBookingWorker.cs:97-105): `SData:BaseUrl`,
  `SData:Application` (Fallback `"ol"`), `SData:Dataset` aus ServiceSettings; Zugang aus
  `IConfiguration["SageLagerbuchung:Username"/"Password"]`. Im Web über `IServiceSettingRepository.GetValueAsync`
  (Data/Repositories/IServiceSettingRepository.cs:9; Web-Nutzung wie SageBookingQueueController.cs:77).
- Zugriffsfilter `[RequirePickingOrTrackingOrLeitstandAccess]`
  (Filters/RequirePickingOrTrackingOrLeitstandAccessAttribute.cs:26-33: CanPick ODER CanViewTracking ODER
  CanManagePickingRelease ODER HasVorbauAccess). Derselbe Class-Level-Filter wie `FaHierarchyController`
  (Controllers/FaHierarchyController.cs:24). *Hinweis:* [[controller]] Z. 65 nennt nur „picking ODER tracking ODER
  leitstand“, der Code prüft zusätzlich Vorbau. Das ist ein Zusatzfund für das Brain-Update und nicht Teil dieses Auftrags.
- Feature-Toggle-Semantik „nur exakt `true` aktiviert“ wie `RequireFaHierarchyKommissionierlistenAktivFilter`
  (Filters/RequireFaHierarchyKommissionierlistenAktivAttribute.cs:45-58: sonst Redirect Home + `WarningMessage`).

**Befund zum Web-Zugang:** Die Web-`appsettings.json` hat **keinen** Abschnitt `SageLagerbuchung`
(IdealAkeWms/appsettings.json:1-40). Nur der Dienst hat ihn (IDEALAKEWMSService/appsettings.json:21-24). Der bestehende
Web-Verbindungstest nimmt Benutzer und Passwort deshalb aus dem Formular (Views/ServiceSettings/Index.cshtml:173-183,
ServiceSettingsController.cs:135-139). Dieses Feature braucht den Abschnitt also **neu in der Web-`appsettings.json`**
(leere Platzhalter im Repo, echte Werte nur auf dem Server, ADR [[0008-servicesettings-db-first-mit-typisiertem-katalog]]:
keine Credentials in der DB). Das ist ein Deploy-Schritt.

**Neue Bausteine (Minimum):**

1. `Services/SageSdata/SageSdataFaModels.cs`: Records mit genau den angezeigten Feldern.
   - `SageFaKopf`: `int BelID, int StrukturID, string? Artikelnummer, string? Matchcode, decimal? Sollmenge,
     decimal? Fertigungmenge, decimal? Istmenge, int? IstEingelastet, int? IstBegonnen, int? IstBeendet,
     int? IstGesperrt, string? VerursacherTyp, DateTimeOffset? Sollstarttermin, DateTimeOffset? Sollendtermin,
     DateTimeOffset? GeaendertAm`. Feldnamen exakt wie in der Antwort (`Istmenge` klein, nicht `IstMenge` wie in
     dtsFaUebersicht).
   - `SageFaPosition`: `string? Ressourcetyp, string? Ressourcenummer, string? Matchcode, decimal? Menge,
     string? Arbeitsgangnummer, string? Arbeitsplatznummer, string? Materialteileart, int? Herkunft, int? BelID,
     int? IstBeendet`.
   - `SageRueckmeldung`: `int? BelID, decimal? MengeGut` (für beide Rückmelde-Quellen, nur die gezählten Felder).
   - `SageBool.IsTrue(int? v) => v is not null and not 0` (Sage `-1` = wahr). Rohwerte bleiben `int?`, kein eigener
     JsonConverter.
   - `SdataCall`: `string Url, int Status, long DauerMs, int Zeilen, string? Fehler, string? RohdatenGekuerzt`.
   - Deserialisierung: `System.Text.Json`, Wrapper `{ "$resources": [...] }` (`[JsonPropertyName("$resources")]`).
     Unbekannte Felder (`$url`, `$updated`, `$descriptor`) werden ignoriert, das ist der STJ-Standard.
2. `Services/SageSdata/ISageSdataReadClient.cs` + `SageSdataReadClient.cs`: **eine** Methode
   `Task<(IReadOnlyList<T> Zeilen, SdataCall Call)> GetAsync<T>(SdataLeseZiel ziel, string dataSource, string where, string select, CancellationToken ct)`.
   `SdataLeseZiel` ist ein Record aus BaseUrl, Application, Dataset, Username, Password und wird im Controller aus
   Settings und Konfiguration gebaut (wie `SageBookingEndpoint`). Regeln:
   - `where` **leer → `ArgumentException`**, kein Request. Das ist die Code-Sperre gegen 237k Zeilen.
   - URL = `SdataUrlBuilder.BuildResourceUrl(BaseUrl, Application, "RealTimeData", Dataset, $"{dataSource}?where={where}&select={select}&format=json")`
     (+ `&orderBy=BelID` für die Struktur). Den `where`-Wert **nicht** mit `Uri.EscapeDataString` kodieren: das machte
     aus `'` `%27`, und ob Sage das akzeptiert, ist ungeprüft. Leerzeichen kodiert die `Uri`-Pipeline als `%20`. Der Test
     prüft die tatsächlich gesendete `RequestUri`.
   - `where`-Werte werden **nur aus `int`** gebaut (BelID/StrukturID). Freitext gelangt nie in die Abfrage, das ist die
     Vertrauensgrenze. Für Rückfrage 2 (Artikelnummer) gilt: Wert in `'…'`, enthaltene `'` verdoppeln **oder** nur
     Artikelnummern aus der vorherigen Sage-Antwort verwenden (keine Nutzereingabe).
   - Basic-Auth wie SageLagerbuchungClient.cs:49-51, `Stopwatch` für die Dauer, Body auf 4000 Zeichen gekürzt.
   - Fehler: Bei HTTP ≠ 2xx keine Exception, sondern leere Liste + `SdataCall` mit Status und Body. Netzwerk-, TLS-,
     Timeout- und JSON-Fehler werden gefangen → Status 0 + `ex.Message`. `OperationCanceledException` bei
     `ct.IsCancellationRequested` wird weitergeworfen.
   - Registrierung in `Program.cs` neben Z. 152:
     `AddHttpClient<ISageSdataReadClient, SageSdataReadClient>(c => c.Timeout = TimeSpan.FromSeconds(20))`
     `.ConfigurePrimaryHttpMessageHandler(sp => …)` nach dem Muster aus IDEALAKEWMSService/Program.cs:102-121. Der
     Callback gibt bei `SslPolicyErrors.None` sofort `true` zurück. Nur bei einem Zertifikatsfehler liest er den
     Schalter über `sp.GetRequiredService<IServiceScopeFactory>()` → Scope → `IServiceSettingRepository.GetValueAsync(SageTlsPolicy.SettingKey)`.
     Grund: Das Repository ist scoped (Program.cs:84), der Handler aber ein Singleton im Pool. Fail-safe: Exception
     beim Lesen → `null` → geprüft.
3. `Services/SageSdata/SageSdataFaTreeBuilder.cs`: **reine** statische Funktion
   `Build(IReadOnlyList<SageFaKopf> fas, IReadOnlyDictionary<int, IReadOnlyList<SageFaPosition>> positionen)` →
   `SageFaStruktur` mit einer **flachen, tiefensortierten Liste** `Knoten (Fa, Ebene, Positionen, je Position
   ZugeordneteSubFaBelId?)`, Liste `NichtZugeordnet` und `Meldungen`. Ablauf: Root bestimmen (F-5c). Dann die
   Artikelnummer-Indizes der Sub-FAs und der MA-Positionen bilden, eindeutige Paare zuordnen (F-4) und von der Wurzel
   aus mit einer `visited`-Menge tiefensortiert ablaufen (Zyklenschutz). Alles Übrige landet in `NichtZugeordnet` +
   Meldung (F-5). Kein Bezug zu `FaHierarchyTreeBuilder`.
4. `Controllers/SageSdataFaController.cs`: `[RequirePickingOrTrackingOrLeitstandAccess]`, eine Action
   `Index(int? belId, CancellationToken ct)`, reine GET-Lesepfad-Action (schreibt nichts). Ablauf: Gate
   `SageSdataFaAnsichtAktiv` (inline, gleiche Semantik wie der Kommissionierlisten-Filter. Ein eigenes Attribut lohnt
   sich für eine einzige Action nicht) → Konfiguration lesen (F-8) → TLS-Hinweis (F-10) → ohne `belId` nur das
   Eingabeformular → sonst Aufrufe **sequenziell**, eine Anfrage nach der anderen, wegen der Serverlast-Fallstricke in
   [[sage-sdata-uebersicht]] → Builder → View. Ein ViewModel (Klasse im Controller oder in `Models/ViewModels`).
5. `Views/SageSdataFa/Index.cshtml`: Bootstrap 5, `<h2 class="page-header">`, Eingabe als `card filter-card`. Je Knoten
   eine Karte mit `id="fa-<BelID>"`, eingerückt nach `Ebene`, Kopfzeile mit FA-Feldern und Status-Badges, darunter
   `table table-sm` der Positionen. Sub-FA-Positionen verlinken per `#fa-<BelID>`. Haupt-FA offen, Sub-FAs als
   `<details>` (natives Auf/Zu, kein JS). Diagnose als `<details>` am Seitenende. **Kein** eigenes JS, keine neue
   Bibliothek. Beim Umsetzen ist der Skill **`frontend-design` Pflicht** (CLAUDE.md), mit Vorrang für
   Bootstrap-Konsistenz und WCAG AA. Status nie nur per Farbe: jedes Badge trägt Text.
6. Menü (`_Layout.cshtml` neben Z. 123-129): `@if (showFaHierarchy && sageSdataFaAktiv)` → Nav-Link „FA-Struktur
   (Sage live)“. Die Bedingung `showFaHierarchy` entspricht dem Controller-Filter (Z. 90), der Schalter wird wie
   Z. 71-73 gelesen.
7. Schalter: `AppSettingKeys.SageSdataFaAnsichtAktiv = "SageSdataFaAnsichtAktiv"` (Default aus, kein Seed) und eine
   Zeile in `StandortSettingsCatalog.Fields`, Gruppe `GroupToggles` (Models/Standort/StandortSettingsCatalog.cs:57-61),
   damit Admins ihn ohne DB-Zeile setzen können. `/Settings` zeigt nur vorhandene Zeilen
   (SettingsController.cs:30-37). Die Beschreibung von `SageLagerbuchungSslZertifikatPruefen`
   (ServiceSettingDefinitions.cs:63, „Wirkt nur auf den Sage-Lagerbuchungs-Client“) und der Doku-Kommentar in
   SageTlsPolicy.cs:5 werden auf „Sage-SData-Clients (Lagerbuchung + FA-Ansicht)“ korrigiert. Key und Default bleiben
   unverändert.

**Aufrufzahl je Seitenaufbau** (Struktur 835, 7 FAs): 1 (BelID → StrukturID) + 1 (Struktur) + 7 (Positionen) = **9**,
plus Rückmeldestand je Rückfrage 1: A = +14, B = +0 (Klick: +2 je FA), C = +2. Rückfrage 2 = ja bringt +1 je FA
(Artikelstückliste). Der Backlog-Testfall „3 bis 9 Aufrufe“ stimmt nur für B oder ohne Rückmeldestand; TS-103 wird
nach der Antwort angepasst.

**Prüfabfragen (vom Menschen auszuführen, Ergebnis in die Spec):**
- **P-1 (IstGesperrt/Status-Wertebereich):**
  `GET {Basis}/RealTimeData/ake_TEST2026;1/dtsFaBelegsuche.Sage.PPS?where=StrukturID eq 835&select=BelID,IstGesperrt,Aktiv,IstReaktiviert&format=json`
  → Erwartung: Werte nur `-1`/`0`. Bis dahin gilt `IstGesperrt` als ungeprüft.
- **P-2 (Zuordnungsregel F-4 gegen die echte Verknüpfung):** auf der AKE-Test-Sage-DB (Tabellennamen laut Doku-Kopf von
  `SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql:23-27`, dort „laut Anhang“, also nicht gegen den
  CREATE-Text geprüft):
  ```sql
  SELECT p.BelID, p.Position, p.RessourceNummer, p.BelIDNachfolger
  FROM KHKPpsFaBelegePositionen p
  WHERE p.BelID IN (SELECT BelID FROM KHKPpsFaBelege WHERE StrukturID = 835) AND p.RessourceTyp = 'MA'
  ORDER BY p.BelID, p.Position;
  ```
  → Erwartung: Jede Zeile mit `BelIDNachfolger <> 0` hat eine `RessourceNummer` gleich der Artikelnummer der Sub-FA
  `BelIDNachfolger`. Zusätzlich eine **mehrstufige** Struktur und eine mit **gleichem Artikel in zwei Sub-FAs** suchen:
  ```sql
  SELECT StrukturID, Artikelnummer, COUNT(*) AS Anzahl
  FROM KHKPpsFaBelege WHERE StrukturID <> BelID
  GROUP BY StrukturID, Artikelnummer HAVING COUNT(*) > 1;
  ```
  (liefert Kandidaten für TS-103.6).
- **P-3 (nur bei Rückfrage 1 = C):**
  `GET …/dtsRueckKorrektur.Sage.PPS?where=BelID eq 835 or BelID eq 836&select=BelID,MengeGut&format=json`
  → 200 statt 500? `or` ist für SData allgemein dokumentiert ([[sage-sdata-uebersicht]] „Abfrageparameter“), für diese
  Quelle aber ungeprüft.
- **P-4 (Rückmelde-Feldtypen):** in einem Mandanten mit Rückmeldungen
  `GET …/dtsRueckKorrektur.Sage.PPS?where=BelID eq <FA mit Rückmeldung>&select=BelID,Typ,Menge,MengeGut&format=json`
  und `GET …/dtsFertigmeldungen.Sage.PPS?where=BelID eq <…>&select=BelID,MengeGut&format=json`. Damit prüfen, ob
  `MengeGut` eine Zahl ist. Ohne Daten bleibt F-6 ungeprüft (Test-Mandant leer).
- **P-5 (Gleichheit der FA-Nummern WMS ↔ Sage, nur für Geralds Vergleich):** Laut Doku-Kopf der View ist
  `FAListe.HauptFA = KHKPpsFaBelege.StrukturID` und `SubFA = BelIDNachfolger`
  (vw_IDEAL-AKE_Kommissionierung_FAListe.sql:23, 27, „laut Anhang“). Dann entspricht die WMS-HauptFA der Eingabe dieser
  Seite. Am IDEAL-System prüfbar mit
  `SELECT TOP 5 HauptFA, VaterFA, SubFA, Artnr FROM [vw_IDEAL-AKE_Kommissionierung_FAListe] WHERE HauptFA = <n>`
  gegen `SELECT BelID, StrukturID, Artikelnummer FROM KHKPpsFaBelege WHERE StrukturID = <n>`.

## Prüfergebnisse (Koordination 2026-10-30, AKE-Test per SData, nur GET)

- **P-1 erledigt:** Struktur 835, alle 7 FAs: `IstGesperrt 0`, `Aktiv -1`, `IstReaktiviert 0`, `IstGedruckt -1`, `IstCadFreigegeben 0`.
  Wertebereich -1/0 bestätigt.
- **P-3 erledigt:** `where=BelID eq 835 or BelID eq 836` → `dtsFaBelegsuche` 200/2 Zeilen, `dtsTeileverwendungsnachweis` 200/12 Zeilen
  (11 + 1), `dtsRueckKorrektur` 200/0 Zeilen. **`or` funktioniert** auf allen drei Quellen → Variante C ist machbar.
- **P-2 (Kandidaten):** über alle 35 FAs des Test-Mandanten (`where=BelID gt 0`) keine Struktur mit zwei Sub-FAs gleichen Artikels;
  14 Strukturen, 7 davon mit Sub-FAs (819:8, 835:7, 846:3, 849:3, 855:3, 842:2, 852:2). TS-103.6 bleibt „nur Unit-Test“, bis Sage-SQL
  (P-2, Gerald) etwas anderes zeigt. 8 FAs `IstBeendet -1`, 0 inaktiv.
- **Befund Wurzel-Kriterium:** `VerursacherTyp` der Wurzeln: `MAN` (835, 819) oder `VK` (12 Strukturen); alle Sub-FAs `FA`. F-5c und die
  Faktentabelle wurden entsprechend nachgezogen. Die Fixture `dtsFaBelegsuche-struktur-835.json` zeigt nur den `MAN`-Fall – der
  Unit-Test braucht zusätzlich eine Wurzel mit `VK`.
- **P-4, P-5 offen** (Sage-SQL bzw. Mandant mit Rückmeldungen, Gerald).

## Migrations-/SQL-Auswirkungen

**Keine.** Keine Model-Änderung, keine EF-Migration, kein `SQL/XX_*.sql`, `SQL/00_FreshInstall.sql` unverändert, keine
`SQL/AgentJobs/*` betroffen. Das neue AppSetting `SageSdataFaAnsichtAktiv` bekommt **keinen** Seed: Es gilt Default aus
ohne Zeile, und die Zeile entsteht beim Speichern über die Standorteinstellungen-Maske (wie
`FaHierarchyVormontageAktiv`, AppSettingKeys.cs:77-82). Kein neuer ServiceSettings-Key, nur ein geänderter
Beschreibungstext im Katalog. Diesen Text zeigt `/ServiceSettings` aus dem Katalog an (ServiceSettingsController.cs:197-204).

## Audit-Feld-Auswirkungen

Keine. Es wird keine fachliche Entität angelegt oder geändert. Die einzige mögliche Schreiboperation ist das Speichern
des Schalters über die bestehende Standorteinstellungen-Maske, deren Audit-Verhalten unverändert bleibt.

## Akzeptanzkriterien

1. Schalter `SageSdataFaAnsichtAktiv` fehlt oder ist ≠ `true`: Der Menüpunkt fehlt, und `/SageSdataFa` leitet auf Home um
   mit `WarningMessage` „Die Sage-Live-Ansicht der FA-Struktur ist deaktiviert.“. Es geht kein SData-Aufruf raus.
2. Benutzer ohne picking/tracking/leitstand/Pickfreigabe/Vorbau-Zugriff: `/SageSdataFa` → `Account/AccessDenied`, auch
   bei aktivem Schalter.
3. Schalter an, berechtigter Benutzer: Der Menüpunkt „FA-Struktur (Sage live)“ ist sichtbar und öffnet die Seite mit
   Eingabefeld. Ohne `belId` geht kein Aufruf raus.
4. Fehlt `SData:BaseUrl`, `SData:Dataset` oder `SageLagerbuchung:Username`: Die Seite zeigt „nicht konfiguriert“ mit den
   fehlenden Keys, kein Aufruf, kein Fehler (HTTP 200).
5. `belId=835` am AKE-Test: 7 FAs. Haupt-FA 835 mit 11 Positionen (10 MA, 1 AG „999 TESTFertigung“, Arbeitsplatz 9999).
   Die 6 MA-Positionen 86003870/66/62/57/55/53 zeigen „→ Sub-FA 836…841“. Sub-FA 836 hat 1 Position (00722001, Menge
   0,6). Keine Mehrdeutigkeitsmeldung.
6. `belId=836`: Hinweis „gehört zur Struktur 835“, danach dieselbe Anzeige wie Kriterium 5.
7. Unbekannte BelID (z. B. 999999999): „BelID … in Sage nicht gefunden“, keine Folgeaufrufe (Diagnose: 1 Aufruf).
8. Ungültige Eingabe (Text, 0, negativ): Validierungsmeldung, kein Aufruf.
9. Mehrdeutigkeit (zwei Sub-FAs gleicher Artikel) erzeugt eine sichtbare Warnung, nennt die BelIDs und den Artikel, und
   führt die Sub-FAs unter „Nicht eindeutig zugeordnete Sub-FAs“. Unit-Test belegt das, manuell nur bei vorhandenem
   Datensatz (P-2).
10. Sage nicht erreichbar / Timeout (20 s) / TLS-Fehler: Fehlerbanner + Diagnose mit Status 0 und Meldung, keine
    Fehlerseite.
11. HTTP 401/403/500 einer Quelle: Status und gekürzter Body in der Diagnose. Fehler bei Positionen einer FA erscheinen
    an dieser FA, der Rest wird angezeigt.
12. Diagnose listet jeden Aufruf mit URL (ohne Passwort/Benutzer), Status, Dauer, Zeilenzahl. „Rohdaten anzeigen“ zeigt
    den JSON-Body, gekürzt auf höchstens 4000 Zeichen.
13. Die URL enthält `/sdata/{App}/RealTimeData/{Dataset}/` mit literalem `;` (kein `%3B`), `format=json`, und **immer**
    ein `where`. Ein Aufruf mit leerem `where` wirft im Client `ArgumentException`, bevor ein Request rausgeht
    (Unit-Test).
14. `SageLagerbuchungSslZertifikatPruefen=false`: Warnbanner auf der Seite, und Aufrufe gegen ein Zertifikat mit Fehler
    gelingen. `true`: Ein solcher Aufruf scheitert mit TLS-Fehler in der Diagnose.
15. Der Rückmeldestand erscheint je Rückfrage 1. Ohne Daten steht „keine Rückmeldung“; Rückmeldungen und
    Fertigmeldungen werden getrennt gezeigt.
16. Status-Badges tragen Text. Sage `-1` = wahr, `0` = falsch (Unit-Test). Termine mit und ohne Offset werden gelesen
    (Unit-Test).
17. Keine Änderung an `FaHierarchy*`-Code/-Views und `FaHierarchySyncService` (Diff-Prüfung im QA-Lauf). Keine
    Migration, kein neuer ServiceSettings-Key.
18. `dotnet build IdealAkeWms.slnx` und `dotnet test` grün.

## Test-Szenarien

**Unit-Tests (`IdealAkeWms.Tests/Services/SageSdata/`, xUnit + FluentAssertions, Stil wie `SdataUrlBuilderTests`):**
- Fixtures: die beiden Backlog-Anhänge **in das Testprojekt kopieren** nach `IdealAkeWms.Tests/Fixtures/SageSdata/`
  (`<None Include="Fixtures\SageSdata\*.json" CopyToOutputDirectory="PreserveNewest" />`). `secondbrain/` ist im Worktree
  per sparse-checkout ausgeblendet, ein Pfad dorthin trägt nicht. Die Fixtures haben eine Hülle (`_quelle`,
  `belid_835`/`belid_836`): Der Test zieht den `$resources`-Teil per `JsonDocument` heraus.
- `SageSdataFaTreeBuilderTests`: Struktur 835 + Positionen 835/836 → Root 835, 6 Sub-FAs auf Ebene 1, je eine
  zugeordnete Position, 4 Kaufteile + 1 AG ohne Zuordnung, keine Meldung. Abgeleitete Fixture mit zweiter Sub-FA
  gleichen Artikels → Meldung + beide in `NichtZugeordnet`. Sub-FA ohne passende Position → Meldung. Ohne Root → Meldung.
  Synthetische zweistufige Struktur → Ebene 2. Zyklus (Sub-FA verweist auf Vorfahren-Artikel) → kein Endlos-Lauf.
- `SageSdataReadClientTests`: `HttpMessageHandler` als kleine Stub-Klasse im Test (`Func<HttpRequestMessage, HttpResponseMessage>`)
  oder per Moq `Protected().Setup<Task<HttpResponseMessage>>("SendAsync", …)`. Moq 4.20 ist vorhanden
  (IdealAkeWms.Tests.csproj:15). Bisher gibt es im Repo keinen HTTP-Client-Test, das ist also der erste. Prüft: gesendete
  `RequestUri` (Contract `RealTimeData`, `;` literal, `where=StrukturID%20eq%20835`, `format=json`), Methode GET,
  Basic-Header, Deserialisierung der Fixture (7 bzw. 11 Zeilen, `Menge 0.6m`, `Istmenge 0`, `IstEingelastet -1` →
  `SageBool.IsTrue` true), Felder `$url`/`$updated` werden ignoriert, 403 → leere Liste + Status 403 ohne Exception,
  `HttpRequestException` → Status 0, leeres `where` → `ArgumentException` ohne Request, Body > 4000 wird gekürzt.
- Controller-Test (InMemory/Moq, Muster vorhandener Controller-Tests): Gate aus → Redirect + WarningMessage. Nicht
  konfiguriert → kein Client-Aufruf (`Verify(Times.Never)`). Gültiger Ablauf mit gemocktem Client → ViewModel mit
  9 Calls.

**Manuell: neues Kapitel `## TS-103 — FA-Struktur live aus Sage (SData, nur lesen) (v1.71.0)` in `docs/TESTSZENARIEN.md`,
danach [[testszenarien-index]] nachziehen:**
- *Vorbedingungen:* Testsystem akenet01.ake.at:4445. `/ServiceSettings`: `SData:BaseUrl` = AKE-Sage-Test, `SData:Dataset`
  = `ake_TEST2026;1`. Web-`appsettings.json` **auf dem Server** mit `SageLagerbuchung:Username/Password` des
  SData-Servicebenutzers. Benutzer mit Rolle picking (und ein zweiter ohne passende Rolle). Vorab prüfen, dass der
  Web-Server `sagetest01.ake.at:5493` erreicht (Netzwerk/Firewall vom IIS-Host aus ungeprüft; der Dienst-Host ist
  nicht zwingend derselbe).
- **103.1** Schalter aus → kein Menüpunkt. URL direkt → Home + Warnung.
- **103.2** Standorteinstellungen → „Sage-Live-Ansicht FA-Struktur aktiv“ an → Menüpunkt da (für picking), fehlt für den
  Benutzer ohne Rolle. Dessen URL-Aufruf → Zugriff verweigert.
- **103.3** `belId=835` → 7 FAs, 11 Positionen am Haupt-FA, 1 AG, 1 Position an 836, Sprungmarken funktionieren.
  Diagnose: Aufrufzahl je Rückfrage-1-Antwort (ohne Rückmeldestand 9), alle Status 200.
- **103.4** `belId=836` → Hinweis „gehört zur Struktur 835“, Anzeige wie 103.3.
- **103.5** `belId=999999999` → „nicht gefunden“, Diagnose 1 Aufruf. Eingabe `abc`/`0` → Validierungsmeldung, kein
  Aufruf.
- **103.6** Mehrdeutige Zuordnung: Struktur aus P-2 mit gleichem Artikel in zwei Sub-FAs → Warnung, Sub-FAs unter
  „Nicht eindeutig zugeordnet“. Gibt es im Test-Mandanten keine: als „nur Unit-Test belegt“ protokollieren.
- **103.7** Leere Struktur/Positionen: FA ohne Positionen zeigt „keine Positionen“. Rückmeldestand im Test-Mandanten
  „keine Rückmeldung“.
- **103.8** `SData:BaseUrl` leeren → „nicht konfiguriert: SData:BaseUrl“, keine Diagnosezeilen. Wert zurücksetzen.
- **103.9** Sage nicht erreichbar: BaseUrl auf einen nicht existierenden Host/Port → nach ≤ 20 s Fehlerbanner, Diagnose
  Status 0.
- **103.10** 403/401: falsches Passwort in der Server-`appsettings.json` (App-Pool recyceln) → Status 401/403 in der
  Diagnose, Banner, keine Fehlerseite. Danach zurück.
- **103.11** TLS: `SageLagerbuchungSslZertifikatPruefen=false` → Warnbanner, Aufrufe 200. `true` → bei
  nicht-vertrauter Kette TLS-Fehler in der Diagnose. Ist die Kette am Server vertraut
  ([[sage-sdata-uebersicht]] „Zertifikat“), bleibt 200; das als Befund notieren.
- **103.12** Rohdaten: „Rohdaten anzeigen“ zeigt JSON, gekürzt auf höchstens 4000 Zeichen; URL ohne Zugangsdaten.
- **103.13** Vergleich (nur informativ, keine Abnahmebedingung): dieselbe Struktur unter „FA-Struktur“ (materialisiert)
  öffnen, sofern dort vorhanden (P-5).
- **Nicht auf idealweb01 testbar** (kein SData-Host für IDEAL): Schalter dort aus lassen.

## Etappen (nur bei epic: true)

Entfällt (`epic: false`). Eine Umsetzung, ein Commit im Bündel-Worktree.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt | – | – |

## Deploy

*Vorläufig, der QA-Lauf bestätigt das gegen den echten Diff.*

- **Web-App:** ja (neuer Controller/View/Client, Layout, Program.cs).
- **Service:** nein (nur der Versions-Bump in `IDEALAKEWMSService/AppVersion.cs`; ein Service-Deploy ist allein dafür
  nicht nötig).
- **Migration:** nein.
- **Version:** Worktree steht auf **1.70.0** (IdealAkeWms/AppVersion.cs:5, IDEALAKEWMSService/AppVersion.cs:5,
  2026-10-28) → Vorschlag **1.71.0** in beiden `AppVersion.cs` + `Views/Help/Changelog.cshtml` + Brain-Changelog
  `secondbrain/changelog/2026-10-30-v1-71-0-sage-sdata-fa-ansicht.md`. Keine andere Spec reserviert 1.71.
- **Server-Konfiguration (Pflicht, sonst „nicht konfiguriert“):** In der `appsettings.json` der Web-App auf dem Server den
  Abschnitt `"SageLagerbuchung": { "Username": "…", "Password": "…" }` mit den Werten aus der Dienst-`appsettings.json`
  eintragen. Der Publish bringt nur leere Platzhalter mit, und nach jedem Publish ist zu prüfen, dass die Server-Werte
  nicht überschrieben wurden. Credentials nie ins Repo oder Brain.
- **Schalter:** Standorteinstellungen → „Sage-Live-Ansicht FA-Struktur aktiv“ nur am Testsystem einschalten.
- **Publish-Befehle (im Worktree):**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

**Brain-Update beim Umsetzen** (in den Hauptcheckout): [[feature-map]] (neuer Eintrag), [[controller]] (Zeile
`SageSdataFaController`, plus Korrektur Vorbau in Z. 65), [[integrationen]] (Abschnitt „SData lesend“),
[[sage-sdata-uebersicht]] (Status „FA-Hierarchie lesen“ → im WMS als Anzeige), README AppSettings-Tabelle, Hilfeseite
(konkreter Abschnitt laut Hilfe-Detail-Regel), [[testszenarien-index]].

## Offene Rueckfragen

1. **Rückmeldestand je FA:** (A) beim Seitenaufbau für alle FAs mitladen: 2 Aufrufe je FA, bei Struktur 835 also 14
   zusätzliche, einfachster Code. (B) nur auf Klick je FA-Knoten: braucht eine zweite Action plus ein kleines Stück JS
   oder einen Seiten-Reload mit `?rueckmeldung=<BelID>`. (C) je Quelle **ein** Aufruf mit `or`-verknüpftem `where` über
   alle BelIDs der Struktur: 2 zusätzliche Aufrufe, Syntax für diese Quellen ungeprüft (P-3). *Empfehlung:* C, falls P-3
   200 liefert, sonst A. Beide brauchen kein JS, und die Sage-Last bleibt bei Einzelaufrufen pro Seitenaufbau.
2. **Artikelstückliste (Herkunft 1) als Soll-Spalte?** Ja = +1 Aufruf je FA (`where=KopfArtikelnummer eq '<Artikel>'`,
   nur `Herkunft = 1` verwenden) und eine Gegenüberstellung je Position. Allerdings fehlt ein gemeinsamer
   Positionsschlüssel (keine BelPosID), Abgleich also nur über Ressourcenummer. *Empfehlung:* nein für diesen
   Machbarkeitsnachweis, als Folgeaufgabe festhalten.
3. **Mengen:** Nur `Menge` (je Kopf-Einheit) anzeigen oder zusätzlich `Bedarf = Sollmenge(FA) × Menge`? Ob `Menge` je
   Kopf-Einheit oder bereits je FA-Menge gilt, ist in Sage nicht belegt (im Fixture ist `Sollmenge` überall 1, der Fall
   ist also nicht unterscheidbar). *Empfehlung:* nur `Menge`, Spaltenkopf „Menge je Einheit“, bis ein FA mit Sollmenge > 1
   die Semantik zeigt.
4. **Listen-View-Pattern (ADR [[0005-listen-view-pattern-mit-server-side-spaltenfilter]]):** Die Positionstabellen als
   begründete Ausnahme „hierarchische Spezialdarstellung“ führen (wie BOM-Tree, ADR 0005 Z. 61-63): keine Pagination,
   kein Spaltenfilter, keine Spaltenauswahl, kein CSV (Tabellen **ohne** `filterable-table`, damit der Wächter
   `CsvExportCoverageTests` sie nicht erfasst)? Oder als `filterable-table` im Client-Mode? *Empfehlung:* Ausnahme,
   weil es eine Detail- und Diagnoseansicht einer einzigen Struktur ist und keine Datenliste.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

*Vorschlag der Koordination 2026-10-30 nach den Prüfergebnissen; von Gerald noch nicht bestätigt – gilt erst mit der Freigabe.*

1. Rückmeldestand: A (alle mitladen) / B (auf Klick) / C (ein or-Aufruf je Quelle, nach P-3) → **C.** P-3 belegt `or` auf
   dtsRueckKorrektur, dtsFertigmeldungen (gleiche Quelle-Art) und dtsFaBelegsuche. Zwei Aufrufe je Seitenaufbau, kein JS.
2. Artikelstückliste (Herkunft 1) als Soll-Spalte anzeigen? → **Nein**, als Folgeaufgabe festhalten (ohne BelPosID kein
   Positionsschlüssel für den Abgleich).
3. Mengen: nur Menge oder zusätzlich Bedarf = Sollmenge × Menge? → **Nur Menge**, Spaltenkopf „Menge je Einheit“. Semantik bei
   Sollmenge 1 nicht unterscheidbar; keine Rechnung auf ungeprüfter Basis.
4. Positionstabellen als ADR-0005-Ausnahme (ohne filterable-table) oder Client-Mode-Spaltenfilter? → **ADR-0005-Ausnahme**
   (Detail-/Diagnoseansicht einer einzelnen Struktur, wie BOM-Tree; Tabellen ohne `filterable-table`).

## Kritische Pruefung (2026-10-30)

*Anwalt des Teufels vor Schranke 1. Stichproben gegen den Worktree `feature/2026-08-07-ideal-teile-1-5` (HEAD d7dc3d03).
Keine Freigabe, keine Antwort im Freigabe-Block geändert.*

### BLOCKER

- **B-1 Antworten sind nicht vom Menschen.** Der Freigabe-Block trägt den Vermerk „Vorschlag der Koordination … von Gerald
  noch nicht bestätigt“. Formal ist damit **keine** der vier Rückfragen beantwortet.
  → *Frage an Gerald:* Bestätigst du die Vorschläge 1–4 (C / Nein / Nur Menge / Ausnahme) so, oder änderst du einzelne?
  Danach den Vermerk entfernen.
- **B-2 Antwort 1 stützt sich auf eine Prüfung, die es nicht gibt.** Antwort 1 sagt: „P-3 belegt `or` auf dtsRueckKorrektur,
  **dtsFertigmeldungen** … und dtsFaBelegsuche“. Laut „Prüfergebnisse“ (P-3) geprüft wurden aber **dtsFaBelegsuche,
  dtsTeileverwendungsnachweis und dtsRueckKorrektur**. dtsFertigmeldungen ist ungeprüft („gleiche Quelle-Art“ ist eine
  Deutung aus dem Namen).
  → *Frage:* P-3 für `dtsFertigmeldungen.Sage.PPS?where=BelID eq 835 or BelID eq 836&select=BelID,MengeGut&format=json`
  nachholen (ein GET), **oder** eine Rückfallregel festlegen: liefert der `or`-Aufruf ≠ 200, dann je FA einzeln.
- **B-3 Entscheidungen stehen nur im Antwortblock, nicht im Rumpf.** Der Dev-Lauf arbeitet nach dem Rumpf. Offen gelassen
  sind dort noch:
  - F-6, AK 15, TS-103.3 und der Absatz „Aufrufzahl je Seitenaufbau“ sagen „je Rückfrage 1“. Das müsste heißen: Variante
    C, je Quelle **ein** `or`-Aufruf über alle BelIDs, Zeilen per `BelID` den FAs zuordnen. Aufrufzahl für 835 ohne
    weitere Änderung: 1 + 1 + 7 + 2 = **11**.
  - Frage 3 ist im Rumpf offen: F-3 nennt die Spalte nur „Menge“. Der Spaltenkopf „Menge je Einheit“ fehlt dort, ebenso
    der Ausschluss einer Bedarf-Spalte.
  - Frage 4 ist im Rumpf offen: F-3 und View-Beschreibung (Baustein 5) sagen nicht „ohne `filterable-table`,
    ADR-0005-Ausnahme“. Ohne diesen Satz baut der Dev-Lauf nach CLAUDE.md-Musterpflicht Spaltenfilter ein oder
    scheitert am `CsvExportCoverageTests`-Wächter.
  - Frage 2: „als Folgeaufgabe festhalten“ fehlt in der Liste „Brain-Update beim Umsetzen“ (Aufgabe in
    `secondbrain/aufgaben/` anlegen).
  - Frontmatter `open_questions` beim Nachziehen leeren bzw. als beantwortet markieren.

### SOLLTE

- **S-1 Wurzel-Kriterium (Schwerpunkt 1): Regel konsistent, Nachweis lückenhaft.** F-5c, Faktentabelle und Builder
  („Root bestimmen (F-5c)“) verwenden einheitlich nur `BelID = StrukturID`; `VerursacherTyp` wird nirgends geprüft.
  Gut. Es fehlt aber:
  - ein Unit-Test mit **`VK`-Wurzel**. Er steht nur in „Prüfergebnisse“, nicht in „Test-Szenarien“ → dort aufnehmen
    (Root `VK` und Root `MAN` → beide Root, keine Meldung).
  - ein manueller Fall mit `VK`-Wurzel. 12 von 14 Wurzeln im Test-Mandanten sind `VK`, TS-103 testet aber nur 835
    (`MAN`). Vorschlag: **TS-103.3b** `belId=846` (3 FAs; Wurzeltyp vor dem Test per Diagnose bestätigen) → gleiche
    Darstellung, keine Meldung.
  - ein Wächter gegen `StrukturID ≤ 0`/`null` aus dem ersten Aufruf. Sonst würde `where=StrukturID eq 0` abgesetzt,
    dessen Treffermenge unbekannt ist. Vorschlag: Meldung „BelID <n> hat keine Struktur-ID in Sage“, kein Folgeaufruf.
    Ob es Einzel-FAs ohne `StrukturID` gibt, ist ungeprüft (im Test-Mandanten 14 Strukturen für 35 FAs, Einzel-FAs also
    vermutlich mit `StrukturID = BelID`).
- **S-2 Variante C: URL-Länge und Kodierung (Schwerpunkt 2).**
  - *Länge:* je BelID ca. 20 Zeichen (` or BelID eq 123456`). Das Limit des Sage-SData-Hosts ist unbekannt. HTTP.sys
    erlaubt standardmäßig 16 KB Request-Zeile (`MaxFieldLength`), das wären rund 700 FAs. Ob Sage auf HTTP.sys läuft, ist
    ungeprüft. Vorschlag (eine Schleife): BelIDs in **Blöcken zu höchstens 50** abfragen. Bei Strukturen ≤ 50 FAs bleibt
    es bei einem Aufruf je Quelle, der Code ist derselbe.
  - *Kodierung:* `new Uri(...)` kodiert Leerzeichen im Query als `%20` und lässt `'` sowie `;` literal. Das entspricht
    der URL, die am 2026-10-30 im Browser funktioniert hat. Die Spec verbietet `Uri.EscapeDataString` bereits richtig.
    **Fallstrick im Test:** `RequestUri.ToString()` liefert die **unkodierte** Form. Die Assertion
    `where=StrukturID%20eq%20835` muss gegen `RequestUri.AbsoluteUri` laufen, sonst prüft der Test das Falsche.
  - *Positionen auch per `or`?* P-3 hat `or` auf `dtsTeileverwendungsnachweis` belegt (200, 12 Zeilen = 11 + 1), und
    `SageFaPosition` trägt bereits `BelID`. Damit würden aus 7 Positionsaufrufen **1** (835: insgesamt 1 + 1 + 1 + 2 =
    **5** statt 11). Der Preis: F-9 „Fehler bei Positionen **einer** FA“ entfällt, ein Fehler trifft dann den ganzen
    Block. → *Entscheidung für Gerald:* Positionen einzeln (wie Rumpf) oder gebündelt? Empfehlung: gebündelt, weil das
    auch S-5 entschärft.
- **S-3 Zugangsdaten der Web-App (Schwerpunkt 3): `appsettings.Production.json` statt `appsettings.json`.**
  - *Belege:* `IdealAkeWms/web.config:18` setzt `ASPNETCORE_ENVIRONMENT=Production`. `Program.cs:11` nutzt
    `WebApplication.CreateBuilder`, das lädt `appsettings.Production.json` automatisch und lässt es `appsettings.json`
    überschreiben. Die Datei liegt nicht im Repo, also bringt der Publish sie auch nicht mit und überschreibt sie nicht.
  - *Lücke in der Deploy-Doku:* Das README kennt die Regel „appsettings.json beim Update nie überschreiben“ nur für den
    **Dienst** (README.md:688-698). Für die Web-App gibt es keine Update-Prozedur. Die Web-`appsettings.json` im Repo
    trägt die echten Connection Strings, wird also heute vermutlich mitkopiert. Eingetragene Zugangsdaten wären damit
    beim nächsten Publish weg, genau der Fall, den die Spec mit „nach jedem Publish prüfen“ abfangen will.
  - *Umgebungsvariable* ist schlechter: Sie gehört in `web.config` `<environmentVariables>`, und `web.config` kommt mit
    jedem Publish neu. Alternativ ginge sie auf App-Pool-Ebene, was mehr IIS-Handarbeit ist.
  - **Vorschlag:**
    - Web-`appsettings.json` bekommt **keinen** `SageLagerbuchung`-Abschnitt (aus `affected_code` streichen).
    - Auf dem Server einmalig `appsettings.Production.json` neben die DLL legen, mit
      `{"SageLagerbuchung":{"Username":"…","Password":"…"}}`.
    - Deploy-Abschnitt und README (neuer Unterabschnitt „Web-App: Sage-SData-Zugang“) entsprechend formulieren.
    - Einschränkung: Ein Publish, der das Zielverzeichnis vorher leert, löscht auch diese Datei. Das gehört als Satz in
      den Deploy-Abschnitt.
  - *Hinweis:* Benutzer und Passwort liegen dann an zwei Stellen (Dienst-`appsettings.json` und Web-Production-Datei).
    Ein Passwortwechsel muss beide treffen. Das gehört in [[sage-sdata-uebersicht]] „Zugang“.
- **S-4 TLS-Callback (Schwerpunkt 4): DB-Zugriff aus dem Handshake herausnehmen.**
  - *Stand der Spec:* Der Callback öffnet je Zertifikatsfehler einen Scope und liest synchron
    (`.GetAwaiter().GetResult()`) über `IServiceSettingRepository`.
  - *Deadlock:* gibt es in ASP.NET Core nicht, dort fehlt der `SynchronizationContext`. Der Aufruf blockiert aber einen
    Threadpool-Thread mitten im TLS-Handshake.
  - *DB-Ausfall:* Der SQL-Connect-Timeout (Standard 15 s) läuft **innerhalb** des HttpClient-Timeouts von 20 s. Ein
    DB-Ausfall zeigt sich deshalb als „Timeout“ statt als TLS-Fehler, also eine irreführende Diagnose.
  - *Fehlender Schutz:* Anders als im Dienst (`ServiceSettings.GetValueSafeAsync`, IDEALAKEWMSService/Program.cs:115-117)
    fängt das Web-Repository keine Ausnahme ab (`ServiceSettingRepository.GetValueAsync` Z. 31-35 ohne try/catch). Das
    try/catch → `null` → „prüfen“ müsste explizit im Callback stehen.
  - *Schwere Abhängigkeit:* `IServiceSettingRepository` ist im Web der `GuardedServiceSettingRepository` mit
    `IProductionOrderRepository` (Program.cs:84-87). Pro Handshake würde also ein zusätzliches Repository samt
    DbContext aufgelöst.
  - *Lebensdauer:* Der `sp` aus `ConfigurePrimaryHttpMessageHandler` ist der Handler-Scope und lebt so lange wie der
    Handler (2 min). Wer daraus direkt einen scoped Dienst zieht, teilt einen DbContext über parallele Handshakes.
  - **Vorschlag (eine Sprosse tiefer):**
    - Der Controller liest den Schalter ohnehin für den Banner (F-10). Er gibt ihn im `SdataLeseZiel` mit.
    - Der Client schreibt ihn je Request in `HttpRequestMessage.Options` (`HttpRequestOptionsKey<bool>`).
    - Der Callback bekommt die `request` als ersten Parameter und liest dort: fehlt der Wert, wird geprüft (fail-safe).
    - Damit entfallen DB, Scope und blockierender Aufruf im Handshake vollständig.
  - Bleibt es beim Scope-Weg, sind drei Pflichtsätze nötig: neuer Scope **je Callback-Aufruf**, konkreter
    `ServiceSettingRepository` statt der Guarded-Variante, explizites try/catch.
  - *Für beide Wege gilt:* Der Callback läuft nur beim **Aufbau** einer Verbindung. Ein Umschalten wirkt erst auf neue
    Verbindungen, also nach Ablauf der Handler-Lebensdauer (2 min) oder nach einem App-Pool-Recycle. TS-103.11 muss das
    als Schritt nennen, sonst wirkt der Test wie ein Fehler.
- **S-5 Fehlerpfad und Zeitbudget (Schwerpunkt 5).**
  - *Worst Case heute:* 9–11 Aufrufe × 20 s = 180–220 s. `web.config` `requestTimeout="00:05:00"` (Z. 16) fängt das zwar
    noch, für einen Machbarkeitsnachweis an einem Terminal ist das aber nicht akzeptabel.
  - *Lücke:* Die Spec sagt nicht, was nach einem Fehler beim **ersten** Aufruf (BelID → StrukturID) oder beim
    Strukturaufruf passiert. F-9 regelt nur „nicht gefunden“ und Positionsfehler.
  - **Vorschlag:**
    1. Fehler oder Status 0 bei Lookup oder Struktur → Abbruch, Banner, keine Folgeaufrufe.
    2. Bei Status 0 (nicht erreichbar/Timeout/TLS) in **irgendeinem** Aufruf werden alle restlichen Aufrufe nicht mehr
       abgesetzt und erscheinen in der Diagnose als „übersprungen (vorheriger Verbindungsfehler)“.
    3. HTTP-Fehler (401/403/500) einzelner Positionsaufrufe brechen **nicht** ab, wie bisher in F-9.
  - Damit gilt der Worst Case „≤ 20 s bis zum Banner“ aus TS-103.9 wirklich. Ein zusätzliches Gesamtbudget per
    CancellationTokenSource ist dann nicht nötig. AK 10 entsprechend ergänzen.
- **S-6 Testfälle unvollständig (Schwerpunkt 6).** Vorhanden: gleicher Artikel in zwei Sub-FAs, Sub-FA ohne Position, ohne
  Root, zweistufig, Zyklus. Ergänzen:
  - (a) `VK`-Wurzel (S-1).
  - (b) **zweite Hälfte von F-5a**: eine Sub-FA, aber derselbe Artikel an **zwei** MA-Positionen. Erwartet: Meldung,
    keine Zuordnung.
  - (c) Termin **ohne** Offset (`"2026-02-03T00:00:00"`). AK 16 fordert den Test, eine Quelle dafür nennt die Spec nicht.
    Synthetischer String; vorher festlegen, ob er als lokale Zeit gilt.
  - (d) VS-Position wird angezeigt, aber nie zugeordnet.
  - (e) bei Variante C: `or`-Antwort mit Zeilen mehrerer BelIDs wird korrekt auf die FAs verteilt; Zeilen einer fremden
    BelID werden ignoriert und gemeldet.
  - (f) Controller: Eingabe einer Sub-FA (836) → Hinweis „gehört zur Struktur 835“.
  - (g) `StrukturID ≤ 0` (S-1).
  - Die Form der synthetischen Daten festlegen. Vorschlag: im Testcode als Records aufgebaut, **keine** zusätzlichen
    JSON-Dateien. Die zwei echten Fixtures bleiben unverändert, damit sie Sage-Wirklichkeit bleiben.
- **S-7 Diagnose ohne Zugangsdaten (Schwerpunkt 7).**
  - *Header:* Basic-Auth steht nur im Header und erscheint weder in `RequestUri` noch in den Meldungen von
    `HttpRequestException`/`AuthenticationException`/`TaskCanceledException`. Diese nennen Host/Port bzw. den
    TLS-Grund.
  - *Restrisiko:* Ein `SData:BaseUrl` mit UserInfo (`https://user:pw@host`) landet über `SdataUrlBuilder` 1:1 in der
    angezeigten URL und im Rohtext. Den Wert pflegt ein Admin in `/ServiceSettings`, unwahrscheinlich, aber genau der
    Fall „nie in der Diagnose“.
  - *Vorschlag:* F-8 behandelt eine `BaseUrl` mit `@` vor dem Host als „nicht konfiguriert (Zugangsdaten gehören nicht in
    die URL)“. Das ist eine Prüfung statt eines Maskierungs-Codes.
  - Außerdem festlegen, dass der Client die URL **nicht** zusätzlich mit Zugangsdaten loggt. Ein Logging wie im
    Lagerbuchungs-Client mit URL ist unkritisch.

### HINWEIS

- **H-1 Seitengröße der Quellen ungeprüft.** `count` wird bei `dtsTeileverwendungsnachweis` ignoriert (unbegrenzt). Ob
  `dtsFaBelegsuche` ohne `count` eine Standard-Seitengröße hat, ist offen: Der größte Test lieferte 35 Zeilen. Bei sehr
  großen Strukturen könnte Sage still kürzen. Prüfabfrage, sobald ein Mandant mit > 100 FAs erreichbar ist:
  `dtsFaBelegsuche?where=BelID gt 0&select=BelID&format=json` → Zeilenzahl gegen `SELECT COUNT(*) FROM KHKPpsFaBelege`.
  Bis dahin genügt eine Diagnosezeile mit der Zeilenzahl, die es laut F-7 ohnehin gibt.
- **H-2 Größe:** Rund 12 Dateien plus Tests in einer Schicht (Client, Builder, Controller, View) und keine Migration.
  Das passt in einen Dev-Lauf, kein Split nötig.
- **H-3 Regression:** gering. Geteilt sind nur `SdataUrlBuilder` (nur gelesen) und zwei Beschreibungstexte
  (`ServiceSettingDefinitions.cs:63`, `SageTlsPolicy.cs:5`). Kein Test prüft diese Texte (Grep in `IdealAkeWms.Tests`
  ohne Treffer). AK 17 (Diff ohne `FaHierarchy*`) ist als harte Bedingung vorhanden.
- **H-4 Stichproben bestätigt:** `SdataUrlBuilder.cs:26-39`, `SageTlsPolicy.cs:15/22-23`,
  `IDEALAKEWMSService/Program.cs:98-122`, `SageLagerbuchungClient.cs:49-74, 77-78`, `ServiceSettingDefinitions.cs:63`,
  `_Layout.cshtml:71-73, 90, 123-129`, `Program.cs:84, 152`. Die Fundstellen der Spec stimmen.
- **H-5 Zusatzfund `controller.md:65` (Vorbau)** ist richtig als Brain-Update-Punkt vorgemerkt, nicht Teil dieses Auftrags.

**Empfehlung:** NACHBESSERUNG NOETIG: Antworten von Gerald bestätigen lassen (B-1), P-3 für dtsFertigmeldungen nachholen
oder Rückfall festlegen (B-2), Entscheidungen in Rumpf/AK/TS ziehen (B-3). S-3 bis S-5 vor dem Dev-Lauf entscheiden.
