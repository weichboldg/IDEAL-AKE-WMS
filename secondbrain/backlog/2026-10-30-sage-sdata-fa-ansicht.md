---
typ: feature
anhaenge:
  - anhaenge/2026-10-30-sage-sdata-fa-ansicht/dtsFaBelegsuche-struktur-835.json
  - anhaenge/2026-10-30-sage-sdata-fa-ansicht/dtsTeileverwendungsnachweis-belid-835-836.json
---
# FA-Struktur und Stückliste live aus Sage per SData anzeigen (Machbarkeitsnachweis, nur lesen)

**Auftrag Gerald 2026-10-30:** Die SData-Schnittstelle im IDEAL-Worktree als **Anzeige** einbauen, inklusive Testmöglichkeit.
Keine Übernahme in WMS-Tabellen, kein Sync, kein Schreiben. Zweck: zeigen, wie FA-Hierarchie und Stückliste direkt aus Sage aussehen
würden – möglicherweise ersparen wir uns damit die `FaHierarchy`-Materialisierung (View `FAListe` → `FaHierarchyNodes`).

Grundlage: [[sage-sdata-uebersicht]], [[sage-sdata-realtimedata]] (Datenquellen und Lesetests), Idee [[2026-10-30-fa-rueckmeldung-an-sage]].

## Was Sage liefert (am AKE-Test belegt, 2026-10-30)

Vertrag `RealTimeData`, nur GET, JSON mit `format=json`, Antwort `{"$resources":[…]}`:

1. **Hierarchie:** `dtsFaBelegsuche.Sage.PPS?where=StrukturID eq <HauptFA>` → alle FAs der Struktur. Haupt-FA: `BelID = StrukturID`,
   `VerursacherTyp = MAN`; Sub-FAs: `VerursacherTyp = FA`. Felder: BelID, StrukturID, Artikelnummer, Matchcode, Sollmenge, Fertigungmenge,
   Istmenge, IstEingelastet/IstBegonnen/IstBeendet/IstReaktiviert/Aktiv/IstGesperrt (Sage-Boolean -1/0), Soll-/Isttermine, GeaendertAm,
   AenderungsIndex, Fertigungsart, Zeichnungsnummer, Projekt. Beispiel Struktur 835: 7 FAs (Anhang).
2. **Stückliste je FA:** `dtsTeileverwendungsnachweis.Sage.PPS?where=BelID eq <BelID>` → Positionen des FA-Belegs (`Herkunft 2`):
   Ressourcetyp (MA | AG | VS), Ressourcenummer, Matchcode, Menge, Arbeitsgangnummer, Arbeitsplatznummer, Materialteileart, IstBeendet.
   Beispiel 835: 10 Material + 1 Arbeitsgang „999 TESTFertigung“ (Anhang). `where=KopfArtikelnummer eq '<Artikel>'` liefert zusätzlich die
   Artikelstückliste (`Herkunft 1`, BelID null) – Soll gegen FA.
3. **Rückmeldestand je FA:** `dtsRueckKorrektur.Sage.PPS?where=BelID eq <BelID>` (Typ, Menge, MengeGut, Start/Ende, Mitarbeiter,
   Arbeitsplatz) und `dtsFertigmeldungen.Sage.PPS?where=BelID eq <BelID>` (MengeGut, DatumZeitEnde). Im Test-Mandanten leer.

Vater-Kind-Zuordnung ist **nicht** als Feld enthalten: Eine MA-Position des Haupt-FA ist genau dann ein Sub-FA, wenn ihre Ressourcenummer
der Artikelnummer eines Sub-FA derselben Struktur entspricht; sonst Kaufteil/Blatt. Mehrdeutig, wenn zwei Sub-FAs denselben Artikel haben
→ sichtbar melden, nicht still zuordnen (Hausregel „Melden statt still behandeln“).

Nicht per SData: `dtsFaBelegStruktur`, `dtsFaPositionen`, `dtsFaFortschritt` (leer, parametrisiert); `MasterData` komplett 403.

## Umfang

- **Neue Read-only-Ansicht** „FA-Struktur (Sage live)“ – eigener Controller neben `FaHierarchyController`, gleicher Class-Level-Read-Filter
  (`RequirePickingOrTrackingOrLeitstandAccess`). Eingabe: HauptFA (StrukturID) **oder** eine BelID (dann StrukturID über
  dtsFaBelegsuche ermitteln). Ausgabe:
  - Baum Haupt-FA → Sub-FAs → Positionen (MA/AG/VS), je Knoten Status-Felder und Mengen; AG-Positionen sichtbar als eigener Typ.
  - Spalte/Badge Rückmeldestand aus dtsRueckKorrektur/dtsFertigmeldungen (Anzahl, Summe MengeGut), leer = „keine Rückmeldung“.
  - **Unabhängig von der FaHierarchy-Materialisierung:** keine Kopplung an `FaHierarchyNode`, `FaHierarchyTreeBuilder` oder die
    bestehenden FA-Struktur-Views. Eigene, schlanke Modelle direkt aus der SData-Antwort. Wenn die vorhandene Baumdarstellung
    (`Views/FaHierarchy`, `fa-liste-gruppierung.js`) sich ohne Verrenkung wiederverwenden lässt, gern – sonst eine einfache
    eingebettete Tabelle/Baumliste. Einfachste tragfähige Lösung (ponytail), Beispiel: Bootstrap-Accordion je Sub-FA mit Positionstabelle.
  - **Diagnose-Bereich** (einklappbar): jede abgesetzte URL mit Statuscode, Dauer und Zeilenzahl; Schalter „Rohdaten anzeigen“ (JSON gekürzt).
- **Lese-Client im Web:** typed `HttpClient` `ISageSdataReadClient` (Services), `GET {BaseUrl}/sdata/{Application}/RealTimeData/{Dataset}/{dts}?where=…&select=…&format=json`
  über den vorhandenen `SdataUrlBuilder.BuildResourceUrl` (ServiceContract = `RealTimeData`), Basic-Auth, TLS gemäß `SageTlsPolicy`
  (Key `SageLagerbuchungSslZertifikatPruefen`), Timeout 20 s, `where`-Strings in einfachen Anführungszeichen, **nie ohne `where`**
  (count wird ignoriert, 237k Zeilen). Konfiguration aus den **bestehenden** ServiceSettings `SData:BaseUrl`, `SData:Application`,
  `SData:Dataset`; Zugangsdaten appsettings-only, gleiche Keys wie der Dienst (`SageLagerbuchung:Username/Password`) – ADR 0008, keine
  Credentials in der DB. Fehlt BaseUrl oder Zugang: Seite zeigt „nicht konfiguriert“, kein Fehler.
- **Feature-Gate:** AppSetting `SageSdataFaAnsichtAktiv` (bool, Default false, ADR 0011) + Menüpunkt nur bei aktiv. Keine neue Rolle.
- **Kein** neuer ServiceSettings-Key außer bei Bedarf `SData:ReadTimeoutSekunden` (dann in `ServiceSettingDefinitions.All`).
- Optional: MemoryCache 60 s je HauptFA, damit Neuladen die Sage-Instanz nicht belastet.

## Nicht im Umfang

Persistenz, Sync, Materialisierung, Schreiben an Sage, IDEAL-Felder der View `FAListe` (Kommissionieren, Arbeitsbereich, Beschichtet …),
Änderungen an `FaHierarchySyncService` oder an bestehenden Ansichten, Verwendung in Kommissionierung/BDE.

## Test

- **Unit (InMemory-fähig):** Baum-Aufbau und Sub-FA-Zuordnung aus den beiden JSON-Anhängen (Struktur 835, Positionen 835/836);
  Mehrdeutigkeitsfall (zwei Sub-FAs gleicher Artikel) → Meldung; URL-Aufbau (`where`-Quoting,
  `RealTimeData` als Contract, `;` im Dataset literal – vgl. `SdataUrlBuilderTests`); Sage-Boolean -1 → true.
- **Manuell (HTTP nicht InMemory-testbar), Testszenario in `docs/TESTSZENARIEN.md`:** Testsystem **akenet01.ake.at:4445** (IDEAL-Version
  mit AKE-Konfiguration, `SData:BaseUrl` zeigt auf den AKE-Sage-Test, Dataset `ake_TEST2026;1`). Fälle: Struktur 835 (7 FAs, 11 Positionen
  am Haupt-FA, 1 Position an 836, 1 AG); BelID 836 als Eingabe → springt auf Struktur 835; unbekannte Nummer → „nicht gefunden“;
  BaseUrl leer → „nicht konfiguriert“; TLS-Schalter aus/an; Diagnose zeigt 3 bis 9 Aufrufe mit Status 200.
  **Nicht** auf idealweb01 testbar: der IDEAL-Sage-Server hat noch keinen bekannten SData-Host (offen, siehe [[sage-sdata-uebersicht]]).

## Entscheidungen (Gerald 2026-10-30)

- Nur Anzeige, nichts wird abgeholt oder gespeichert.
- **Muss nicht an die FaHierarchy-Materialisierung angepasst sein** – darf und soll einfacher gelöst werden. Ob die
  Materialisierung später entfällt, wird nach dem Sehen entschieden; ein Vergleich beider Darstellungen macht Gerald selbst im Browser.
- Umsetzung im **Bündel-Worktree** `.claude/worktrees/2026-08-07-ideal-teile-1-5` (Zweig `feature/2026-08-07-ideal-teile-1-5`), wie die
  übrigen IDEAL-Teile.

## Offene Fragen für die Spec

1. Rückmeldestand je Sub-FA mitladen (n+1 Aufrufe, bei 7 FAs 14 Requests) oder nur auf Klick je Knoten?
2. Soll die Ansicht auch die Artikelstückliste (`Herkunft 1`) als Soll-Spalte zeigen, oder reicht der FA-Beleg?
3. Mengen-Einheit: `Menge` aus dem Teileverwendungsnachweis ist die Menge je Kopf-Einheit; Sollmenge × Menge für den Bedarf anzeigen?
