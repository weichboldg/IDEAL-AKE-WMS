---
type: spec
title: "Bugfix Sage-AG-Anlage: JSON-Rumpf für FertigungsauftragService nach $template ausrichten (IDEAL)"
slug: 2026-11-20-bugfix-sage-ag-anlage-json-rumpf-spec
status: Entwurf
created: 2026-11-20
updated: 2026-11-20
source_backlog: "[[2026-11-20-bugfix-sage-ag-anlage-json-rumpf]]"
bug: "[[2026-11-20-sage-ag-anlage-json-rumpf-bug]]"
bezug: "[[2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen-spec]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: IDEAL
version_vorschlag: "1.99.47 (Worktree 1.99.45 laut IdealAkeWms/AppVersion.cs:5; 1.99.46 Vorschlag Teil 6, 1.99.48 Vorschlag BDE-Liste Isolierungsfräse; 1.99.47 ist nach dem Sammellauf v1.99.45 frei. Dev-Lauf prüft erneut)"
testszenario_vorschlag: "TS-172 (docs/TESTSZENARIEN.md im Worktree endet bei TS-170; TS-165 BDE mobil, TS-169 Teil 6/BF-Warenkorb, TS-171 BDE-Liste reserviert. Dev-Lauf prüft erneut)"
sql_vorschlag: "keine"
affected_code:
  - "IDEALAKEWMSService/Services/SageFaArbeitsgangClient.cs:27-43 (Feldkonstanten), :57-73 (Serialize), :86/:93-99 (Logging, Fehlerweg)"
  - "IDEALAKEWMSService/Services/SageFaArbeitsgangService.cs:157-177 (Rumpf bei Fehler ins Aktivitäts-Protokoll)"
  - "IDEALAKEWMSService.Tests/Services/SageFaArbeitsgangServiceTests.cs:295-313 (bestehender Payload-Test, Erwartung ServiceOption)"
  - "IDEALAKEWMSService.Tests/TestData/Sage/<neu> FertigungsauftragService-template-IDEAL-<datum>.xml (gelesenes $template)"
  - "IDEALAKEWMSService.Tests/Services/<neu> SageFaArbeitsgangRumpfTests.cs (Rumpf gegen Template)"
  - "scripts/deploy/nachlauf.psd1 (Einstellung + Pruefung)"
  - "docs/TESTSZENARIEN.md (TS-172), Views/Help/Changelog.cshtml, beide AppVersion.cs"
  - "Bug: [[2026-11-20-sage-ag-anlage-json-rumpf-bug]]"
open_questions:
  - "1 Wiederholtest-Modus: Für den erneuten E1b auf :88 Sync:SageFaArbeitsgaengeMaxWiederholungen = 0 setzen (nur ein Versuch, Senden nur über den Knopf) und danach wieder 3 – einverstanden?"
  - "2 Bearbeiter: Steht WMS laut Prüfschritt PT-4 nicht als gültiger Wert in Sage, welcher Wert kommt in Sync:SageFaArbeitsgaengeBearbeiter (fester Sage-Mitarbeiter, Sage-Benutzer)?"
  - "3 Freigabe-Umfang: Gilt die Freigabe für den Wiederholtest an FA 1065144 auch für einen zweiten Versuch am selben Tag, falls Sage nach dem Rumpf-Fix fachlich ablehnt (z. B. ValidateMitarbeiter), oder je Versuch neu?"
  - "4 Rückbau: Bleibt die bei Erfolg in Prod-Sage angelegte AG-Position 162 an FA 1065144 stehen, oder baut Gerald sie in Sage zurück (das WMS löscht nichts)?"
epic: false
etappen: []
deploy:
  web: true        # nur Version/Changelog/Hilfe, kein Web-Code
  service: true    # Client + Service
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

# Bugfix Sage-AG-Anlage: JSON-Rumpf nach `$template` ausrichten

> [!info] Quelle und Standort
> Backlog [[2026-11-20-bugfix-sage-ag-anlage-json-rumpf]] (keine Anhänge). Bezug: [[2026-11-17-arbeitsgaenge-aus-wms-in-sage-fa-anlegen-spec]],
> Abschnitte „Befund E1b“, „Prüfabfragen“ P-1 bis P-5, QA-Befunde F3/N1. Bug-Datensatz: [[2026-11-20-sage-ag-anlage-json-rumpf-bug]].
> **Nur IDEAL.** AKE hat den Schalter aus und bekommt Arbeitsgänge aus OSEON; der Code läuft dort nicht (Schalter, `ProduktionsauftragHierarchisch`).

## Ziel / Nutzen (das Warum)

Der erste Schreibtest (E1b, 2026-11-20) scheiterte, bevor Sage fachlich prüfte: `SDataSerializationException: Ungültige
Elementdefinition` in `DataContainerJsonReader.Read()`. Ohne einen Rumpf, den Sage einliest, legt die Funktion keine einzige
Arbeitsgang-Position an. Ziel: Den Rumpf an das gelesene `$template` und an die schon funktionierenden Schreib-Clients anlehnen, das mit
einem Unit-Test gegen das gespeicherte Template absichern, bei Fehlern den gesendeten Rumpf sichtbar protokollieren und danach E1b an
FA 1065144 wiederholen.

## Umfang (In-Scope / Out-of-Scope)

**Teil davon**
- Prüfschritt PT-1 bis PT-5 (nur lesen) vor dem Dev-Lauf: vollständiges `$template`, Feldliste `FaBelegArbeitsgangPosition`, Enum-Namen,
  Bearbeiter, Stand der Warteschlange.
- `SageFaArbeitsgangClient.Serialize` nach dem Template umbauen; die `TODO(P-1..P-4)` werden durch Belege ersetzt oder bleiben als
  benannter offener Punkt.
- Unit-Test „Rumpf gegen gespeichertes `$template`“, Anpassung des bestehenden Payload-Tests.
- Gesendeter Rumpf im Aktivitäts-Protokoll und im Serilog-Log, wenn Sage ablehnt.
- Manifest-Einträge für den Wiederholtest (Einstellung, Prüfung).
- Wiederholtest E1b an FA 1065144 (nur nach Freigabe Gerald, TS-172.4).

**Nicht Teil davon**
- Keine Änderung an Vorschau, Warteschlange, Status-Automat, Nachlesen (F3) oder der Unklar-Regel (N1, v1.99.42).
- Kein Einlasten (ServiceOption 5), keine Userfelder/WMS-Kennung an der Position (P-3 bleibt offen), keine `BacklogPrimaryKeys`.
- Kein Wechsel des Schreibziels (`IDEAL_TEST_2026_05_OuS;1` war am 20.11. nicht erreichbar, Nachtrag 07:42 der Bezugs-Spec).
- AKE nicht.

## Fachliche Anforderungen

### Ist – der Rumpf heute (`IDEALAKEWMSService/Services/SageFaArbeitsgangClient.cs`)

| Zeile | Inhalt |
|---|---|
| `:10` | `SageFaArbeitsgangRequest(int BelId, string SageArbeitsgangnummer, decimal Menge, string Bearbeiter)` – je AG ein Aufruf |
| `:25` | Ressource `$service/FertigungsauftragService`, URL über `SdataUrlBuilder.BuildResourceUrl` (`:54-55`) |
| `:27-29` | Kommentar: Feldnamen **nicht** am IDEAL-Ziel belegt, nur aus `$schema` IDEAL Prod (Ideen-Notiz 15:44) |
| `:30-31` | `ServiceOption` = **Zahl 4** |
| `:32` | `Handle` auf **oberster Ebene** = BelID, `TODO(P-1)` |
| `:34` | Positionsliste `PositionenArbeitsgang` (QA F2, Name aus `$schema` AKE-Test L7) |
| `:35-37` | je Position `Handle` = BelID (`TODO(P-1)`), `PositionHandle` = 0 (`TODO(P-1)`) |
| `:38-39` | `RessourceTyp` = **Zahl 1** (`TODO(P-2)`, Freigabe-Antwort 1 der Bezugs-Spec) |
| `:40-42` | `RessourceNummer` = Sage-AG (Text), `Menge` = Fertigungsmenge, `Bearbeiter` (`TODO(P-4)`) |
| `:43` | `TODO(P-3)` WMS-Kennung im Positions-Userfeld – nicht gebaut |
| `:57-73` | `Serialize`: `JsonSerializer.Serialize(new Dictionary<string, object> { … })`, flaches JSON ohne Hülle |
| `:84-89` | POST, `application/json`, Basic-Auth im Header (nicht im Rumpf) |
| `:86` | `LogInformation` mit URL, FA, AG **und dem ganzen Rumpf** vor jedem Senden |
| `:93` | `LogInformation` der Antwort (gekappt auf 4000 Zeichen, `:151`) |
| `:94-99` | Nicht-2xx = eindeutiger Fehler (nicht unklar); Text = `HTTP <Code> <Reason>: <ErrorText/ResultText>` |

Der heute gesendete Rumpf (E1b, Werte aus der Bezugs-Spec: FA 1065144, AG 162, Bearbeiter `WMS`; Menge = Fertigungsmenge des FA):

```json
{"ServiceOption":4,"Handle":1065144,"PositionenArbeitsgang":[{"Handle":1065144,"PositionHandle":0,"RessourceTyp":1,"RessourceNummer":"162","Menge":<Fertigungsmenge>,"Bearbeiter":"WMS"}]}
```

### Vergleich mit den funktionierenden Schreib-Clients und dem Lesebefund

Muster 1 – `SageLagerbuchungClient` (`IDEALAKEWMSService/Services/SageLagerbuchungClient.cs:45-47`): `JsonSerializer.Serialize(request)` eines
typisierten Objekts mit `[JsonPropertyName]` exakt wie das Sample (`SageLagerbuchungPayload.cs:5-15`). Flaches JSON: Kopffelder `Memo`,
`Standardtext`, Liste `Lagerbuchungen` als **Array von Objekten ohne Element-Hülle** (`:14`, `:17-29`). Enum `Lagerbewegungsart` als **Text**
(`"Zugang"`, `"Entnahme"`, `"Umbuchung"`, `SageLagerbuchungPayloadBuilder.cs:39-41,51,56`; Enum `ExtLagerbewegungsart` NotSpecified=0,
Zugang=1, Entnahme=2, Umbuchung=3 laut [[sage-sdata-commonwawiservices]] Z. 37). Belegt: Schreibtests A1/A2 am AKE-Test, HTTP 200 `Ok`
([[sdata-befund-ake-test-2026-10-30]] §14). Die Feldnamen des `$template` stimmen mit den JSON-Namen überein (§9, letzte Zeile).

Muster 2 – `SageWareneingangClient` (`SageWareneingangClient.cs:36,48`, Request `SageWareneingangPayload.cs:13-53`): ebenfalls flaches JSON ohne
`request`-Hülle, Listen als Arrays (`TransformSettings`, `StandardBelegPositionen`, `BelegPositionLagermengen`). Enums hier als **Zahl**
(`Erfassungsart` 4100, `BelegTransformArt` 1, `Positionstyp` 1, `Mengenart` 1). Belegt: Beleg 2027-398675 am AKE-Test, Request-Text
wörtlich in [[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]] Z. 693-700, Antwort HTTP 200 `ResultNumber 200`.

Lesebefund `$template` IDEAL (Backlog Z. 29-49): unter `<request>` stehen `ServiceOption = NotSpecified` (Enum als Text),
`PositionenMaterial/FaBelegMaterialPosition`, `PositionenArbeitsgang/FaBelegArbeitsgangPosition` mit Userfeld- und Backlog-Listen; **kein
`Handle` unter `request` sichtbar**. Die skalaren Felder der AG-Position fehlen in der Anzeige.

| # | Punkt | WMS heute | Muster / Lesebefund | Bewertung |
|---|---|---|---|---|
| U1 | Hülle `request` | keine (`:57`) | beide Muster ohne Hülle, belegt (Wareneingang 2027-398675) | **kein Unterschied**, Hülle bleibt weg |
| U2 | `Handle` oben | gesendet (`:60`) | im Template unter `request` nicht sichtbar | **Unterschied.** Stärkster Kandidat für „Ungültige Elementdefinition“: ein Element, das der Request-Typ nicht kennt. Nur durch Ausschluss gefolgert – die Meldung nennt das Element nicht |
| U3 | `ServiceOption` | Zahl 4 | Template zeigt Text; Muster: Text (Lagerbuchung) **und** Zahl (Wareneingang) werden eingelesen | Unterschied in der Form, als Ursache **unwahrscheinlich** (Zahl-Enums liest derselbe Vertrag ein). Soll: Text wie im Template |
| U4 | Name Positionsliste | `PositionenArbeitsgang` | Template: `PositionenArbeitsgang` | **gleich** |
| U5 | Element-Hülle je Position | keine (Array von Objekten) | Template-XML: Element `FaBelegArbeitsgangPosition` je Eintrag; beide Muster senden Listen-Einträge ohne Hülle | **kein Unterschied** – der XML-Elementname ist der Typ des Eintrags, kein JSON-Schlüssel (aus den Mustern abgeleitet, für diesen Service **ungeprüft**) |
| U6 | Felder je Position (`Handle`, `PositionHandle`, `RessourceTyp`, `RessourceNummer`, `Menge`, `Bearbeiter`) | gesendet | Template-Anzeige abgeschnitten; Namen nur aus `$schema`-Auszug (Ideen-Notiz Z. 115-116, AKE L7) | **ungeprüft** – PT-1/PT-2 |
| U7 | `RessourceTyp` | Zahl 1 | Gelesen heißt der Wert `"AG"` (`PpsFaBelegePositionenInfo`, QA P-5 der Bezugs-Spec, Z. 586-588); Enum-Name zu 1 unbekannt | Zahl-Form wird eingelesen (U3); Text erst, wenn PT-3 den Namen liefert |
| U8 | Typen | int, string, decimal über `Dictionary<string, object>` | Muster: typisierte Klassen | kein belegter Unterschied; Typen laut PT-2 (`xs:int`/`xs:string`/`xs:decimal`) prüfen |
| U9 | Kopffelder, die das Template evtl. verlangt (`Bearbeiter`, FA-Handle unter anderem Namen, `FaBelegNeu`) | fehlen | Template-Anzeige abgeschnitten; AKE L7 nennt `FaBelegNeu{…}` als Anlage-Teil | **ungeprüft** – PT-1/PT-2 |

Folgerung: Belegt ist nur U2 als Abweichung vom Template; U6 und U9 sind offen, bis die Feldliste vollständig vorliegt. Darum hängt der
Dev-Lauf am Prüfschritt (Abschnitt „Ablauf und Schranken“).

### Soll-Rumpf

**In einem Satz:** flaches JSON ohne `request`-Hülle und **ohne `Handle` auf oberster Ebene**, `ServiceOption` als Enum-Text
`"AddOrChangePositionArbeitsgang"`, eine Liste `PositionenArbeitsgang` mit genau einem Objekt, dessen Feldnamen exakt die
Kindelemente von `FaBelegArbeitsgangPosition` aus dem gespeicherten `$template` sind (FA-Bezug über `Handle` der Position).

```json
{
  "ServiceOption": "AddOrChangePositionArbeitsgang",
  "PositionenArbeitsgang": [
    {
      "Handle": 1065144,
      "PositionHandle": 0,
      "RessourceTyp": 1,
      "RessourceNummer": "162",
      "Menge": 1.0,
      "Bearbeiter": "WMS"
    }
  ]
}
```

| Feld | Wert | Herkunft | Stand |
|---|---|---|---|
| Hülle | keine | U1, belegt durch beide Muster | belegt |
| `Handle` oben | entfällt | U2, Lesebefund | belegt (Template), Ursache gefolgert |
| `ServiceOption` | `"AddOrChangePositionArbeitsgang"` | Name aus `$schema` AKE-Test ([[sdata-befund-ake-test-2026-10-30]] §10, Z. 227-228); Text-Form wie Template | **ungeprüft für IDEAL** – PT-3 |
| `PositionenArbeitsgang` | Array, ein Eintrag | Template, U4/U5 | Name belegt, Array-Form abgeleitet |
| `Handle` (Position) | BelID des FA | AKE L7 `PositionenMaterial[] {Handle, PositionHandle, …}` ([[sage-sdata-benutzerfelder-lesetest]] Z. 588), Bedeutung „FA-Handle“ | **ungeprüft** – PT-2 (Bezeichnung/Doku des Feldes) |
| `PositionHandle` | 0 (= neue Position) | heute `:37` | **ungeprüft** – PT-2; ist das Feld nicht Pflicht, wird es weggelassen |
| `RessourceTyp` | Zahl 1, oder der Enum-Name aus PT-3 | Freigabe-Antwort 1 der Bezugs-Spec (DefaultRessourceTyp 1) | Wert entschieden, Form **ungeprüft** |
| `RessourceNummer` | Sage-AG als Text | Freigabe-Antwort 1; gelesen `RessourceNummer "401"` als Text (QA P-5) | Typ **ungeprüft** – PT-2 |
| `Menge` | Fertigungsmenge des FA (Beispiel 1.0) | Freigabe-Antwort 11 | Name **ungeprüft** – PT-2 |
| `Bearbeiter` | `Sync:SageFaArbeitsgaengeBearbeiter` (heute `WMS` auf :88) | Freigabe-Antwort 12; Ideen-Notiz Z. 115 nennt das Feld an der Position | Ort (Position oder Kopf) und gültiger Wert **ungeprüft** – PT-2, PT-4, Rückfrage 2 |

Regel für den Dev-Lauf: Was PT-1/PT-2 anders belegen (Name, Ort, Pflicht), gewinnt gegen diese Tabelle. Ein Feld, das im Template nicht
vorkommt, wird **nicht** gesendet. Pflichtfelder (`sme:isMandatory`) der AG-Position, die das WMS nicht füllen kann, sind ein Blocker
und gehen als neue Rückfrage in die Spec, nicht als geratener Wert in den Code.

### Prüfschritt (nur lesen; Gerald bzw. Koordination, :88 → Prod `IDEAL;1`, wie beim Lesebefund)

Basis: `https://idealsage100tes.ideal.ideal-ake.at:5493/sdata/ol`. Ergebnis wird in diese Spec unter „Befund Prüfschritt“ eingetragen,
das Template zusätzlich als Datei übergeben.

- **PT-1 Vollständiges `$template`.** Register *Sage-Test → Abfrage (GET)*, Ressource
  `$service/FertigungsauftragService/$template`, Vertrag `CommonWawiServices`, Dataset `IDEAL;1`.
  - Die Antwort steht im Feld „Antwort“ (`Views/SageTest/Index.cshtml:148`, scrollbar). Der Server schickt bis 50 000 Zeichen
    (`IdealAkeWms/Controllers/ServiceSettingsController.cs:222`). Die Kürzung „nach ~3,8 KB“ war die Anzeige, nicht die Antwort: den
    **ganzen** Text markieren und kopieren und als `FertigungsauftragService-template-IDEAL-2026-11-xx.xml` speichern.
  - Kontrolle: Der Text endet mit `</entry>` und **nicht** mit `… (gekuerzt)`. Sonst PT-2 im Browser.
  - Gesucht: alle Kindelemente von `<request>` und von `<FaBelegArbeitsgangPosition>` in Reihenfolge, mit Vorbelegung.
- **PT-2 Feldliste `FaBelegArbeitsgangPosition` aus `$schema`.** Direkt im Browser (angemeldet, Zertifikatsausnahme wie in
  [[sdata-befund-ake-test-2026-10-30]]): `GET …/CommonWawiServices/IDEAL;1/$schema` und „Speichern unter“. Das Vertragsschema ist größer
  als 50 000 Zeichen (AKE: 0,41 MB, §10), im Register wäre es gekürzt.
  - Im XML suchen: `<xs:complexType name="FaBelegArbeitsgangPosition…">` (genauer Typname laut `type`-Attribut des Elements
    `FaBelegArbeitsgangPosition`) sowie den Request-Typ von `FertigungsauftragService`.
  - Je `xs:element` notieren: `name`, `type`, `sme:isMandatory`, `sme:maxLength`, ggf. `sme:label`/Doku.
  - Je Enum-Typ (`ServiceOption`, `RessourceTyp`) die `xs:enumeration`-Werte.
- **PT-3 Enum-Namen.** Register *Sage-Test → Feldliste*, Vertrag `CommonWawiServicesResources`, Ressource
  `PpsFertigungsauftragServiceOptionInfo`, danach `PpsFaBelegPositionRessourceTypenInfo` ([[sage-sdata-commonwawiservicesresources]] Z. 34),
  Anzahl 5. `orderBy` ist Pflicht (`SageTestController.cs:65-66`): erster Aufruf mit beliebigem Feldnamen zeigt die Schemafelder auch bei
  Fehler (`:117`), dann mit dem Schlüsselfeld wiederholen. Gesucht: Name zu Wert 4 und Name zu Wert 1 bzw. zum Arbeitsgang.
- **PT-4 Bearbeiter.** Feldliste, Vertrag `CommonWawiServicesResources`, Ressource `PpsMitarbeiterInfo` (77 Einträge an IDEAL, Ideen-Notiz
  Z. 102), Schlüsselfeld wie PT-3 bestimmen, dann `where <Nummernfeld> eq 'WMS'`. Gesucht: Gibt es `WMS`? Sagt PT-2 (Label/Doku), ob
  `Bearbeiter` ein PPS-Mitarbeiter oder ein Sage-Benutzer ist?
- **PT-5 Warteschlange und Schalter auf :88** (SQL auf der WMS-DB des Systems :88):

  ```sql
  SELECT Id, BelId, Kuerzel, SageArbeitsgangnummer, Status, Versuche, LEFT(Fehler, 150) AS Fehler, ModifiedAt
  FROM dbo.SageFaArbeitsgangAnlagen
  WHERE Status IN (1, 2, 5)          -- 1 Offen, 2 Gesendet, 5 Fehler (IdealAkeWms/Models/SageFaArbeitsgangAnlage.cs:4-14)
  ORDER BY BelId, Reihenfolge;

  SELECT [Key], [Value] FROM dbo.ServiceSettings
  WHERE [Key] IN (N'SageFaArbeitsgaengeAnlegenAktiv', N'Sync:SageFaArbeitsgaengeBearbeiter',
                  N'Sync:SageFaArbeitsgaengeMaxWiederholungen', N'Sync:SageFaArbeitsgaengeWiederholAbstandMinuten',
                  N'SData:BaseUrl', N'SData:Dataset');
  ```

  Erwartet: genau die Zeile 1065144/162 mit Status 5, Versuche 1; Schalter `false`. Jede weitere Zeile mit Status 1 oder 5 würde beim
  Einschalten mitgesendet (siehe Wiederholtest).

Bis PT-1 bis PT-4 vorliegen, sind alle Aussagen zu Feldnamen, Pflichtfeldern, Enum-Namen und `Bearbeiter` in dieser Spec **unverifiziert**.

### Logging des gesendeten Rumpfs bei Fehler

- **Serilog: vorhanden.** Der Client loggt den ganzen Rumpf vor jedem Senden auf Information (`SageFaArbeitsgangClient.cs:86`), die
  Antwort danach (`:93`). Der Dienst schreibt Information (`IDEALAKEWMSService/appsettings.json:28-33`: Default `Information`, Override nur
  `Microsoft*`). Der Rumpf des E1b-Versuchs steht also schon im Dienst-Log von :88. Neu ist nur eine **Warning**-Zeile im Fehlerweg
  (`:94-99`) mit Status und Rumpf. So findet man ihn beim Filtern auf Warnungen.
- **Aktivitäts-Protokoll: fehlt heute.** Im Fehlerfall schreibt der Dienst nur `Error | ResponseRaw` (`SageFaArbeitsgangService.cs:170-176`).
  - Neu: Lehnt Sage ab (`!r.Success && !r.Unklar`), kommt eine zweite Warnzeile in denselben Lauf:
    `FA <BelId>, Arbeitsgang <Nr>: gesendeter Rumpf <JSON>`. Den Rumpf liefert `SageFaArbeitsgangClient.Serialize(request)`, die
    statische Funktion von `:57`. Gekappt wird auf 800 Zeichen mit der vorhandenen `Kappen`-Funktion (`:231`), die Warnung insgesamt
    bleibt unter 900 Zeichen wie bisher (`:176`).
  - Die Spalte `Fehler` der Warteschlange (1000 Zeichen, `SageFaArbeitsgangAnlage.cs:24`) und die Fehlermail bleiben unverändert.
- **Keine Geheimnisse:** Benutzer und Kennwort stehen nur im `Authorization`-Header (`:88-89`), nie im Rumpf. Der Rumpf enthält BelID,
  AG-Nummer, Menge und das Bearbeiter-Kürzel. Der bestehende Test prüft schon „kein Kennwort im Body“
  (`SageFaArbeitsgangServiceTests.cs:312`). Der neue Test prüft dasselbe für die Protokollzeile.

### Wiederholtest E1b (nach Freigabe Gerald)

- Ziel wie E1b: :88, Prod `IDEAL;1`, FA **1065144**, AG **162**. Kein anderer FA.
- `SageFaArbeitsgaengeAnlegenAktiv` bleibt **aus**, bis Gerald den Wiederholtest freigibt, und wird direkt danach wieder ausgeschaltet.
- **Dubletten-Risiko (N1):**
  - Beim Einschalten sendet der Dienst alle Zeilen mit Status `Offen` sofort. Zeilen mit `Fehler` und `1 ≤ Versuche ≤ MaxWiederholungen`,
    die nicht „unklar“ sind, sendet er nach dem Abstand automatisch erneut (`SageFaArbeitsgangService.cs:74-79`).
  - Die E1b-Zeile (Fehler, Versuche 1) würde also bei Default 3 ohne Klick erneut gesendet, ebenso jede andere Fehler-Zeile aus PT-5.
  - Schutz vor jedem Senden: Der Dienst liest den FA in Sage (`:112`, `:130-137`). Eine dort erkannte AG wird nicht erneut gesendet.
  - Erkannt wird nur über `RessourceNummer`/`ArbeitsgangNummer` bei `RessourceTyp` „AG“ (QA P-5 der Bezugs-Spec). Legt Sage die Position
    anders an, sieht der Leser sie nicht. Dieser Fall endet seit v1.99.42 als „unklar“ (`:199-207`) und wird nicht automatisch wiederholt.
  - Ein HTTP-Fehler **nach** einer Teilanlage in Sage gälte dagegen als eindeutiger Fehler (`SageFaArbeitsgangClient.cs:94-99`) und würde
    wiederholt.
- **Daher Empfehlung (Rückfrage 1):** Für den Test `Sync:SageFaArbeitsgaengeMaxWiederholungen = 0`.
  - Mit 0 wird die E1b-Zeile nicht automatisch gesendet (`Versuche 1 > 0`).
  - Gerald löst den einen Versuch über „FA … anlegen“ aus: Der Knopf reiht die Fehler-Zeile erneut ein, sie geht auf Status `Offen`
    (`IdealAkeWms/Data/Repositories/SageFaArbeitsgangAnlageRepository.cs:48-56`).
  - Danach wieder 3 (oder der von Gerald gewählte Wert).
- Ablauf und Erwartung: TS-172.4.

## Technischer Loesungsentwurf

Ponytail-Leiter: Es entsteht kein neuer Client und keine neue Klasse für den Rumpf. Der vorhandene `Serialize` und seine Konstanten
werden angepasst, und das Logging nutzt den vorhandenen `Serialize` und `Kappen`.

1. **`SageFaArbeitsgangClient.cs`**
   - `FeldFaHandle` (`:32`) und den Eintrag `[FeldFaHandle] = r.BelId` (`:60`) streichen.
   - `ServiceOptionAgAnlegen` (`:31`) wird der Text aus PT-3 (erwartet `"AddOrChangePositionArbeitsgang"`). Der Dictionary-Wert ist dann
     ein `string`.
   - Positionsfelder (`:35-42`, `:63-71`) genau nach PT-1/PT-2: Namen, Reihenfolge wie Template, nur Felder aus dem Template.
     `RessourceTyp` als Zahl 1, es sei denn, PT-3 liefert einen Namen; dann Text.
   - Die `TODO(P-1/P-2/P-4)` werden zu Belegkommentaren mit Fundstelle des Prüfschritts (Datum, Quelle). `TODO(P-3)` bleibt.
   - `Dictionary<string, object>` bleibt. Eine typisierte Klasse wie in den Mustern wäre nur Umbau ohne Wirkung; die Namen sichert der
     Template-Test.
   - Fehlerweg `:94-99`: eine `_logger.LogWarning("Sage-FA-AG abgelehnt {Status} FA {BelId} AG {Ag}. Gesendet: {Json}", …)` mit dem
     vorhandenen lokalen `json`.
2. **`SageFaArbeitsgangService.cs:157-177`**
   - Request in eine lokale Variable ziehen.
   - Im `else`-Zweig bei `!r.Unklar` zusätzlich
     `run.LogWarningAsync(Kappen(Text(a, "gesendeter Rumpf " + Kappen(SageFaArbeitsgangClient.Serialize(req), 800)), 900)!)`.
   - Sonst ändert sich nichts.
3. **Tests**
   - Neu `IDEALAKEWMSService.Tests/TestData/Sage/FertigungsauftragService-template-IDEAL-<datum>.xml` = Ergebnis PT-1, unverändert gespeichert.
     `TestData\**` wird schon kopiert (`IDEALAKEWMSService.Tests.csproj:27-29`, Muster WiCAM-CSV).
   - Neu `SageFaArbeitsgangRumpfTests`:
     - Template per `XDocument` lesen. Unter dem Element `request` die Kindnamen (LocalName) sammeln, ebenso unter dem ersten
       `FaBelegArbeitsgangPosition`.
     - `Serialize(new SageFaArbeitsgangRequest(1065144, "162", 1m, "WMS"))` per `JsonDocument` parsen.
     - Prüfen: (a) Jeder Schlüssel oberster Ebene steht unter `request`. (b) Jeder Schlüssel des Positionsobjekts steht unter
       `FaBelegArbeitsgangPosition`. (c) Es gibt kein `Handle` auf oberster Ebene. (d) `ServiceOption` ist ein String und gleich dem
       Namen aus PT-3. (e) Jedes `sme:isMandatory`-Feld der Position aus PT-2 ist gesetzt (nur wenn das Schema als zweite Datei
       vorliegt, sonst entfällt (e) mit Kommentar).
     - Gegenprobe im Test-Kommentar: Mit dem alten Rumpf schlägt (a) an `Handle` fehl (Red-Green beim Umbau einmal zeigen).
   - Anpassen `Client_PostAnFertigungsauftragService_PayloadServiceOption4_RessourceTyp1` (`SageFaArbeitsgangServiceTests.cs:295-313`):
     `:306` `GetInt32()` → `GetString()` mit dem Enum-Namen. Neu: `doc.RootElement.TryGetProperty("Handle", out _)` ist false. Ändern
     sich Positionsnamen (PT-2), werden `:308-311` nachgezogen. Grund: gewolltes neues Verhalten, kein „Fixture bis grün“.
   - Neu im selben Test-File: `Senden_SageLehntAb_ProtokollEnthaeltRumpf_OhneKennwort` (FakeClient liefert `Success false, Unklar false`;
     `_log`-Lauf enthält eine Warnung mit `PositionenArbeitsgang`, Länge ≤ 900, ohne Kennwort aus `Endpoint`).
   - Unverändert, aber mitlaufen lassen: `Client_Http500_MitErrorText_IstFehler_NichtUnklar` (`:315-323`), `Client_Timeout_IstUnklar`
     (`:325-331`), `Auswerten_…` (`:333-342`), alle Dienst-Tests mit `FakeClient` (`:24`). Sie hängen nicht am Rumpf.
4. Kein neuer ADR. Ergänzung in `fallstricke.md` §49 als Nachtrag: „SData-JSON für `$service`-Aufrufe: flach, ohne `request`-Hülle,
   Listen als Arrays ohne Element-Hülle, nur Elemente aus dem `$template`; unbekanntes Element = `Ungültige Elementdefinition`
   (HTTP 500, vor der fachlichen Prüfung)“, mit dem Warum (E1b). Den Teil „unbekanntes Element“ nur aufnehmen, wenn der Wiederholtest
   ihn bestätigt.

## Migrations-/SQL-Auswirkungen

Keine. Kein Modell, keine Migration, kein `SQL/<Nr>`, `00_FreshInstall.sql` unverändert, `SQL/AgentJobs/` nicht betroffen. Die
Prüfabfrage PT-5 und der Manifest-Eintrag `Pruefung/…` lesen nur.

## Einstellungen

Keine neue und keine geänderte Einstellung (Key, Typ, Default, Wertebereich bleiben). Für den Wiederholtest werden zwei vorhandene
Werte **vorübergehend** auf :88 umgestellt. Das ist ein Betriebsschritt (Manifest „Einstellung“), keine Code-Änderung:

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `SageFaArbeitsgaengeAnlegenAktiv` (vorhanden, `ServiceSettingDefinitions.cs:75`) | Bool | Ja/Nein | `false` | unverändert | nur für den Wiederholtest an, danach aus | IDEAL (:88) | vorhanden: schreibt in Sage-FAs |
| `Sync:SageFaArbeitsgaengeMaxWiederholungen` (vorhanden, `:82`, `:215`, `:303`) | Int | 0–10 | `3` | unverändert | für den Wiederholtest 0 (Rückfrage 1), danach wieder 3 | IDEAL (:88) | – |

## Audit-Feld-Auswirkungen

Keine neuen. Die Warteschlangen-Zeile bekommt wie bisher `ModifiedAt/By/ByWindows` vom Dienst (`SageFaArbeitsgangService.cs:222-229`,
`ServiceActor`), beim erneuten Einreihen vom Benutzer (`SageFaArbeitsgangAnlageRepository.cs:53-55`).

## Rollen / Zugriff

Unverändert: Seite und Knopf nur Admin (`[RequireAdminAccess]`, Bezugs-Spec Antwort 10), Feature-Gate `ProduktionsauftragHierarchisch` und
Schalter. Das Aktivitäts-Protokoll zeigt den Rumpf nur denen, die es heute schon sehen. Der Rumpf enthält keine Zugangsdaten.

## Listen-View-Pattern

Nicht betroffen (keine neue oder geänderte Tabellenansicht).

## Akzeptanzkriterien

1. `Serialize` liefert flaches JSON ohne `request`-Hülle und **ohne** Schlüssel `Handle` auf oberster Ebene.
2. Jeder Schlüssel oberster Ebene und jeder Schlüssel des Positionsobjekts steht als Kindelement von `request` bzw.
   `FaBelegArbeitsgangPosition` im gespeicherten `$template` (Test `SageFaArbeitsgangRumpfTests` grün; mit dem alten Rumpf rot).
3. `ServiceOption` wird als der Enum-Name gesendet, den PT-3 für Wert 4 belegt.
4. Die Positionswerte entsprechen den Freigabe-Antworten der Bezugs-Spec: FA-Bezug = BelID, `RessourceNummer` = Sage-AG als Text,
   `RessourceTyp` = 1 bzw. dessen Name, `Menge` = Fertigungsmenge, `Bearbeiter` = Einstellung. Felder, die nicht im Template stehen,
   werden nicht gesendet.
5. Lehnt Sage ab (Nicht-2xx), steht im Aktivitäts-Protokoll des Laufs eine Warnung mit FA, AG und gesendetem Rumpf, höchstens 900
   Zeichen, ohne Benutzer und Kennwort. Das Serilog-Log des Dienstes enthält eine Warning-Zeile mit Status und Rumpf.
6. Bei Erfolg und bei „unklar“ ändert sich das Protokoll nicht. Die Spalte `Fehler` und die Fehlermail sind unverändert.
7. `dotnet build IdealAkeWms.slnx` 0 Fehler; `dotnet test` beider Testprojekte grün. Der einzige geänderte bestehende Test ist
   `Client_PostAnFertigungsauftragService_PayloadServiceOption4_RessourceTyp1`, mit Begründung.
8. Der Schalter-Default bleibt `false`. Auf keinem System wird er durch Deploy oder Skript eingeschaltet.
9. Nach Freigabe Gerald: Der Wiederholtest legt an FA 1065144 genau **eine** AG-Position mit `RessourceNummer` 162 an. Die Zeile steht nach
   dem Nachlesen auf `Angelegt`. „Sage-Stand prüfen“ zeigt „in Sage schon vorhanden“, ein zweites „FA … anlegen“ endet als
   „Übersprungen“ ohne POST.
10. Lehnt Sage im Wiederholtest fachlich ab (z. B. `ValidateMitarbeiter`), steht der Sage-Text als Fehler an der Zeile, der Rumpf im
    Protokoll. Mit `MaxWiederholungen = 0` folgt kein automatischer zweiter POST.

## Test-Szenarien

Neues Kapitel **TS-172 „Sage-AG-Anlage: Rumpf nach `$template`, Rumpf im Protokoll, Wiederholtest E1b (v1.99.47)“** in
`docs/TESTSZENARIEN.md`, danach Zeile 172 in `secondbrain/tests/testszenarien-index.md`. Querverweis aus TS-159.x (Schreibtest) auf TS-172.4.

- **TS-172.1 Automatisch (Dev-Lauf):** `SageFaArbeitsgangRumpfTests` und der angepasste Payload-Test sind grün, der Protokoll-Test auch
  (AK 1–6).
- **TS-172.2 DEV IDEAL, Schalter aus:** Nach dem Deploy steht `SageFaArbeitsgaengeAnlegenAktiv` auf `false`. Auf der Seite gibt es keinen
  Knopf, im Dienst-Log keine Zeile „Sage-FA-AG POST“ und keinen Lauf `SageFaArbeitsgaenge` (AK 8). Negativ: ein direkter POST auf
  `Anlegen` bei Schalter aus wird mit Warnung abgewiesen (Regression TS-159.8).
- **TS-172.3 DEV IDEAL, Prüfung im Update-Skript:** In der DEV-DB eine Zeile `SageFaArbeitsgangAnlagen` mit Status 5, Versuche 1 und
  Fehler „HTTP 500 …“ per SQL anlegen. Danach `-NurPruefen`: Die Prüfung `Pruefung/sage-fa-ag-auto-wiederholung` meldet den Treffer.
  - Mit `Fehler` = „Antwort unklar …“ oder `MaxWiederholungen = 0` kein Treffer.
  - Zeile wieder löschen.
- **TS-172.4 Wiederholtest E1b an :88 – erst nach Freigabe Gerald:**
  - Vorbedingung: PT-1 bis PT-5 eingetragen; Publish v1.99.47 auf :88; `Sync:SageFaArbeitsgaengeBearbeiter` gültig laut PT-4;
    `MaxWiederholungen` laut Rückfrage 1; PT-5 zeigt außer 1065144/162 keine Zeile mit Status 1/5.
  - Schritte: Schalter an → Seite, FA-Filter 1065144, „Sage-Stand prüfen“ → „FA … anlegen“ (Dialog nennt nur 162) → bestätigen →
    Dienst-Takt abwarten → Aktivitäts-Protokoll `SageFaArbeitsgaenge` öffnen → „Sage-Stand prüfen“ → Schalter **aus**.
  - Lesend gegenprüfen:
    `GET …/CommonWawiServicesResources/IDEAL;1/PpsFaBelegePositionenInfo?where=BelID eq 1065144&orderBy=BelPosID&format=json`
    (Feldliste) → genau eine Position mit `RessourceTyp` „AG“ und `RessourceNummer` 162. Danach in der Sage-Oberfläche am FA Position,
    Ressource, Einlastung und Kosten ansehen.
  - Erwartet: AK 9. Ergebnis (Rumpf aus dem Dienst-Log, Antwort, Sage-Stand) wird in diese Spec und in die Bezugs-Spec („Befund E1b“)
    eingetragen.
  - Negativ: sofort ein zweites „FA … anlegen“ → „Übersprungen“, kein POST im Dienst-Log. Lehnt Sage fachlich ab → AK 10, kein
    automatischer zweiter POST, Schalter aus, Befund in die Spec.
- **AKE-Regression:** keine Änderung sichtbar. Der Schalter bleibt aus, die Seite ist bei `ProduktionsauftragHierarchisch` = aus nicht
  erreichbar (TS-159.13).

## Ablauf und Schranken

Kein Epic. Die Reihenfolge ist fest, und jede Schranke ist ein Mensch-Schritt:

| # | Schritt | Wer | Schranke |
|---|---|---|---|
| S0 | PT-1 bis PT-5 lesen, Ergebnis + Template-Datei in die Spec | Gerald/Koordination | **vor dem Dev-Lauf.** Fehlt PT-1 vollständig, startet der Dev-Lauf nicht. Er trägt das als Blocker ein, statt aus dem abgeschnittenen Auszug zu raten |
| S1 | Rumpf, Logging, Tests, Manifest, Version, TS-172, Doku; ein Commit | Dev-Lauf | Build + Tests grün; QA-Lauf mit TS-172.1–172.3 auf DEV IDEAL |
| S2 | Publish auf :88, Wiederholtest TS-172.4 | Gerald | **Freigabe Gerald** vor dem Einschalten; Schalter danach aus |

## Deploy

*Vorläufig, der QA-Lauf bestätigt am echten Diff.*

- **Web-App:** ja, nur wegen Version (`IdealAkeWms/AppVersion.cs`), Anwender-Changelog und Hilfe-Satz zur Sage-AG-Anlage. Kein
  Controller- oder View-Code.
- **Service:** ja (`SageFaArbeitsgangClient`, `SageFaArbeitsgangService`). Den Dienst vor dem Kopieren stoppen.
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022; Texte ASCII wie die Nachbarn, `NachlaufManifestDriftTests`):

| Id | Version | Typ | Firma | Phase | Ausführung | Text |
|---|---|---|---|---|---|---|
| `v1.99.47-sage-fa-ag-wiederholtest` | 1.99.47 | Einstellung | IDEAL | Nachher | anzeigen | „SageFaArbeitsgaengeAnlegenAktiv bleibt AUS. Fuer den Wiederholtest an FA 1065144 (nur nach Freigabe Gerald): Sync:SageFaArbeitsgaengeMaxWiederholungen = 0, Schalter an, FA anlegen, Schalter aus, danach Wiederholungen wieder 3. AKE: nichts.“ |
| `Pruefung/sage-fa-ag-auto-wiederholung` | 1.99.47 | Pruefung | IDEAL | Nachher | anzeigen | Prüfung (`Braucht=@('SQL/160')`): `SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.SageFaArbeitsgangAnlagen a WHERE a.Status = 1 OR (a.Status = 5 AND a.Versuche >= 1 AND (a.Fehler IS NULL OR a.Fehler NOT LIKE N'Antwort unklar%') AND a.Versuche <= ISNULL((SELECT TRY_CAST([Value] AS int) FROM dbo.ServiceSettings WHERE [Key] = N'Sync:SageFaArbeitsgaengeMaxWiederholungen'), 3))) THEN 1 ELSE 0 END`; Text „Sage-AG-Warteschlange hat offene oder automatisch wiederholbare Zeilen: beim Einschalten von SageFaArbeitsgaengeAnlegenAktiv sendet der Dienst sie an Sage. Vorher unter Stammdaten > Arbeitsgaenge > Sage pruefen.“ (Regel = `SageFaArbeitsgangService.cs:74-79`, `SageFaArbeitsgaenge.UnklarAnfang` `:40`) |

  Keine Daten-/Einmal-Skripte, also kein Trockenlauf (ADR 0023 nicht berührt).
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` und
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`. Danach Update-Skript auf
  :88; der Schalter bleibt aus.

## Offene Rueckfragen

1. **Wiederholtest-Modus:** Für den erneuten E1b auf :88 `Sync:SageFaArbeitsgaengeMaxWiederholungen = 0` setzen? Dann gibt es nur einen
   Versuch, gesendet wird nur über den Knopf „FA … anlegen“. Danach wieder 3. Grund: Mit Default 3 sendet der Dienst die E1b-Zeile und
   jede andere Fehler-Zeile nach dem Einschalten von selbst (`SageFaArbeitsgangService.cs:74-79`).
2. **Bearbeiter:** Steht `WMS` laut PT-4 nicht als gültiger Wert in Sage, welcher Wert kommt in `Sync:SageFaArbeitsgaengeBearbeiter`: ein
   fester Sage-Mitarbeiter (Nummer?) oder ein Sage-Benutzer? Am AKE-Test scheiterte ein Rückmelde-Aufruf an `ValidateMitarbeiter`
   ([[sdata-befund-ake-test-2026-10-30]] §11). Dieselbe Ablehnung ist nach dem Rumpf-Fix der wahrscheinlichste nächste Fehler.
3. **Freigabe-Umfang:** Gilt die Freigabe für den Wiederholtest an FA 1065144 auch für einen zweiten Versuch am selben Tag, wenn Sage nach
   dem Rumpf-Fix fachlich ablehnt und der Fehler mit einer Einstellung behebbar ist (z. B. Bearbeiter)? Oder braucht jeder Versuch eine
   eigene Freigabe?
4. **Rückbau:** Bleibt die bei Erfolg in Prod-Sage angelegte AG-Position 162 an FA 1065144 stehen (Test-FA laut Befund E1b)? Oder baut
   Gerald sie in Sage zurück? Das WMS löscht nichts.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

*(noch keine)*

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. Wiederholtest-Modus (`MaxWiederholungen = 0` für den Test, danach 3) → **ja, `MaxWiederholungen = 0` für den Test, danach wieder 3** (Gerald 2026-11-20 09:30)
2. Bearbeiter-Wert, falls `WMS` laut PT-4 ungültig → **technischer Sage-Mitarbeiter „WMS“ – Gerald legt ihn in Sage an, falls PT-4 ihn als ungültig zeigt** (Gerald 2026-11-20 09:30)
3. Freigabe-Umfang (zweiter Versuch am selben Tag ja/nein) → **je Versuch neu fragen (jeder Schreibversuch gegen Prod mit Geralds Okay)** (Gerald 2026-11-20 09:30)
4. Rückbau der Test-AG 162 an FA 1065144 (stehen lassen / Gerald baut zurück) → **stehen lassen (Test-FA)** (Gerald 2026-11-20 09:30)

## Ergebnis Prüfschritte (Koordination 2026-11-20 09:35, :88 → Prod `IDEAL;1`, nur GET über `ServiceSettings/TestSageConnection`)
**PT-1 `$template`** (`$service/FertigungsauftragService/$template`, HTTP 200, 3 856 Byte, vollständig): unter `<request>` nur `ServiceOption`
(Wert `NotSpecified`), `PositionenMaterial/FaBelegMaterialPosition`, `PositionenArbeitsgang/FaBelegArbeitsgangPosition`; die Positionen
enthalten im Template nur die Listen `FaBelegPositionUserfields`, `GenericUserfields`, `BacklogPrimaryKeys` (Skalare im Template nicht
vorbelegt). `$service/…/$schema` → HTTP 401; Schema daher über Dataset-`$schema` (HTTP 200).
**PT-2 Felder laut `$schema`** (Anzeige am WMS bei 50 013 Byte abgeschnitten, die betroffenen Typen sind vollständig enthalten):
- `FertigungsauftragServiceRequestSample--type`: `ServiceOption` (Typ `ExtFertigungsauftragServiceOption--enum`, optional), `FaBelegNeu`
  (`FaBelegNeu--type`, optional, nillable), `PositionenMaterial` (`FaBelegMaterialPosition--list`), `PositionenArbeitsgang`
  (`FaBelegArbeitsgangPosition--list`). **Kein Feld `Handle` auf Request-Ebene** → der WMS-Rumpf (`SageFaArbeitsgangClient.cs:60`,
  `[FeldFaHandle] = r.BelId` oben) sendet ein Element, das der Typ nicht kennt – stärkster Kandidat für „Ungültige Elementdefinition“.
- `FaBelegArbeitsgangPosition--type`: `Bearbeiter` (string, nillable), **`Handle` (integer)**, **`PositionHandle` (integer)**,
  **`RessourceTyp` (`ExtFaBelegPositionRessourceTyp--enum`)**, **`RessourceNummer` (string)**, **`Menge` (decimal)**, dazu die drei Listen.
  → Feldnamen der Position stimmen mit dem WMS überein; `RessourceNummer` ist **string** (WMS sendet Text – ok).
**PT-3 Enum-Werte** (Koordination 10:45, direkter GET `…/CommonWawiServices/IDEAL;1/$schema` im Browser, 418 878 Zeichen, ungekappt):
- `ExtFertigungsauftragServiceOption--enum`: 0 NotSpecified · 1 NeuerFertigungsauftrag · 2 Fertigungsauftragsaenderung ·
  3 AddOrChangePositionMaterial · **4 AddOrChangePositionArbeitsgang** · 5 FertigungsauftragEinlasten → WMS sendet 4 = **richtig**.
- `ExtFaBelegPositionRessourceTyp--enum`: 0 NotSpecified · **1 Material** · **2 Arbeitsgang** · 3 Fremdfertigung · 4 Werkzeug ·
  5 Arbeitsplatz · 6 Verschnitt → **WMS sendet 1 = Material – FALSCH, richtig ist 2 (Arbeitsgang).** Die Konstante
  `RessourceTypArbeitsgang = 1` (`SageFaArbeitsgangClient.cs:39`) und Freigabe-Antwort 1 der Bezugs-Spec („DefaultRessourceTyp 1“) sind zu
  korrigieren. (Beim Lesen meldet Sage den Typ als Text „AG“ – das ist die Lese-Darstellung, nicht der Enum-Name.)
- Form: Werte sind Zahlen mit Bezeichnung; ob der JSON-Leser Zahl oder Bezeichnung erwartet, bleibt offen – das Template zeigt die
  Bezeichnung (`NotSpecified`). Empfehlung: Bezeichnung senden (`AddOrChangePositionArbeitsgang`, `Arbeitsgang`), Unit-Test darauf.
**PT-4 Bearbeiter** (Gerald 2026-11-20 11:11, Screenshot Sage 100 → Mitarbeiter, Mandant IDEAL Kältetechnik): Mitarbeiter **`M00153`**,
Matchcode „TEST, WMS“, Vorname WMS, Nachname TEST, aktiv, ohne Benutzer/Gruppe – neu angelegt. **Welchen Wert Sage im Feld `Bearbeiter`
prüft (Mitarbeiternummer `M00153` oder Kürzel/Matchcode), ist nicht belegt.** Empfehlung: `Sync:SageFaArbeitsgaengeBearbeiter` = `M00153`
(Nummer ist der Schlüssel des Mitarbeiterstamms); der Wiederholtest zeigt es – lehnt Sage ab, steht der Grund in `ErrorText` der Antwort.
**PT-5** siehe Spec – offen.
Folgerung für den Rumpf: oberstes `Handle` **entfernen**; `ServiceOption` = `AddOrChangePositionArbeitsgang`, `RessourceTyp` = **`Arbeitsgang` (2, nicht 1!)** als Enum-Bezeichnung; Positionen unter
`PositionenArbeitsgang` mit `Handle` = BelID, `PositionHandle` = 0, `RessourceNummer`, `Menge`, `Bearbeiter`.

## Handtest Claude Code 2026-11-20 (Gerald sendet, Skript von Claude Code; nur FA 1065144 / Sage-AG 162)

Freigabe Gerald: höchstens drei POSTs gegen Prod `IDEAL;1`, nur FA 1065144 / AG 162. Gesendet mit
[[2026-11-20-sage-ag-anlage-handtest-body.ps1]]: `HttpWebRequest`, Basic-Auth, `application/json; charset=utf-8`, kein Accept-Header,
also wie `SageFaArbeitsgangClient.cs:84-89`. Der Fehler-Body wird vollständig gelesen. Kennwort über `Get-Credential`, nicht gespeichert.
Die volle Antwort steht in `secondbrain/tests/2026-11-20-sage-ag-anlage-handtest-protokoll.txt`. Bearbeiter `M00153` (PT-4), Menge 1, wie im Handtest Geralds.

### Vergleich mit dem funktionierenden Weg (Auftrag 1)

| Punkt | Lagerbuchung / Wareneingang (am AKE-Test erfolgreich) | AG-Anlage | Beleg |
|---|---|---|---|
| Header, Content-Type, Auth | Basic, `StringContent(json, UTF8, "application/json")`, kein Accept | gleich | `SageLagerbuchungClient.cs:45-51`, `SageFaArbeitsgangClient.cs:84-89`, `Program.cs:145-166` |
| Hülle | flach, keine `request`-Hülle | flach | `SageLagerbuchungPayload.cs:10-15`, Wareneingang-Request in [[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]] Z. 693-696 |
| Leere Listen | immer mitgesendet (`JsonIgnoreCondition.Never`, `Seriennummern`/`Chargen`) | fehlen | `SageLagerbuchungClient.cs:14-17`, `SageLagerbuchungPayload.cs:27-28` |
| Enum-Form | Text (Lagerbuchung) und Zahl (Wareneingang), beide eingelesen | Zahl | `SageLagerbuchungPayloadBuilder.cs:41`, Spec Wareneingang Z. 695 |
| Antwortformat | Atom-XML trotz JSON-Request, bei HTTP 200 | Atom-XML | Spec Wareneingang Z. 697 |
| Ziel | **nur AKE-Test** `ake_TEST2026;1` (DPS 9.0.156.0, [[sdata-befund-ake-test-2026-10-30]] §10, §14) | **IDEAL;1**, DPS-Build-Pfad `commonwawiservice-9010` | Stacktrace unten |

Wichtig: Gegen `IDEAL;1` ist bisher **kein** SData-POST erfolgreich gelaufen. Die belegten Schreibwege stammen alle vom AKE-Test.

### Versuche

| # | Rumpf | HTTP | Antwort (gekürzt) |
|---|---|---|---|
| A | `{"ServiceOption":"AddOrChangePositionArbeitsgang","PositionenArbeitsgang":[{"Handle":1065144,"PositionHandle":0,"RessourceTyp":"Arbeitsgang","RessourceNummer":"162","Menge":1,"Bearbeiter":"M00153"}]}` | 500 | `ResultNumber 500`, `ErrorText`: `System.NullReferenceException … bei DPSBS.OL.SData.CommonWawiService.ServiceHandler.DPSbsSDataServiceHandler.Execute() in C:\Solutions\dpsbs-ol-sdata-commonwawiservice-9010\…\DPSbsSDataServiceHandler.cs:Zeile 187` (11:25:50) |
| C | wie A, dazu `"PositionenMaterial":[]` und je Position `"FaBelegPositionUserfields":[],"GenericUserfields":[],"BacklogPrimaryKeys":[]` (Template-Struktur, Muster Lagerbuchung) | 500 | identisch, Zeile 187 (11:29:00) |
| B | wie A, Enums als Zahl: `"ServiceOption":4`, `"RessourceTyp":2` | 500 | identisch, Zeile 187 (11:32:01) |

Reihenfolge A → C → B, abgestimmt mit Gerald: Nach A sprach der Befund zuerst für fehlende Listen. Kein POST lieferte 2xx, angelegt wurde laut Antwort nichts.
Lesend nachgeprüft (Auftrag 3): **steht aus**, `-Variante Lesen` (GET `PpsFaBelegePositionenInfo?where=BelID eq 1065144&orderBy=BelPosID`).

### Ursache (Stand)

- **Rumpf-Fix E1b wirkt.** Ohne oberstes `Handle` kommt keine `SDataSerializationException` „Ungültige Elementdefinition“ mehr. Der JSON-Leser
  akzeptiert alle drei Rümpfe, Enum als Text **und** als Zahl.
- **Der neue Fehler hängt nicht vom Rumpf ab.** Drei verschiedene Rümpfe liefern denselben Fehler an derselben Stelle: NRE in der generischen
  DPS-Dispatch-Methode `DPSbsSDataServiceHandler.Execute()` Z. 187. Der Stacktrace hat nur diesen einen Rahmen, also keinen Code des
  FertigungsauftragService darunter.
- Fachliche Ablehnungen sehen anders aus: Am AKE-Test kam eine `ArgumentException: ValidateMitarbeiter` mit `ResultText` (§11). Hier ist
  `ResultText` nil.
- **Schluss (gefolgert, nicht belegt):** Der Fehler liegt in der DPS-Installation bzw. im Build 9010 am IDEAL-Server, oder der Dienst erwartet etwas
  außerhalb des Rumpfs. Aus dem WMS ist das nicht weiter eingrenzbar, ohne weiter in Prod zu schreiben.

### Soll-Rumpf für den Dev-Lauf

Variante A gilt als Soll: Enums als Bezeichnung wie das `$template`, kein oberstes `Handle`, Positionsfelder wie PT-2, `RessourceTyp` = `Arbeitsgang`.
Die leeren Listen (Variante C) bringen nachweislich nichts und werden **nicht** gesendet (YAGNI).

```json
{"ServiceOption":"AddOrChangePositionArbeitsgang","PositionenArbeitsgang":[{"Handle":<BelID>,"PositionHandle":0,"RessourceTyp":"Arbeitsgang","RessourceNummer":"<Sage-AG>","Menge":<Fertigungsmenge>,"Bearbeiter":"<Sync:SageFaArbeitsgaengeBearbeiter>"}]}
```

Der Rumpf ist **nicht** durch ein HTTP 2xx belegt. Der Dev-Lauf kann den Rumpf-Fix bauen und testen, der Wiederholtest E1b bleibt aber blockiert, bis DPS
antwortet. `SageFaArbeitsgaengeAnlegenAktiv` bleibt aus.

### Rückfrage an DPS (Gerald)

> `POST …/sdata/ol/CommonWawiServices/IDEAL;1/$service/FertigungsauftragService` (JSON, ServiceOption 4 `AddOrChangePositionArbeitsgang`, eine
> Position `PositionenArbeitsgang` mit Handle = BelID 1065144, RessourceTyp 2, RessourceNummer "162", Menge 1, Bearbeiter "M00153") antwortet
> am 2026-11-20 um 11:25, 11:29 und 11:32 mit `ResultNumber 500` / `NullReferenceException` in `DPSbsSDataServiceHandler.Execute()`
> (`dpsbs-ol-sdata-commonwawiservice-9010`, Zeile 187). Das passiert unabhängig davon, ob die Enums als Text oder Zahl gesendet werden und ob leere Listen dabei sind.
> 1. Was ist an Zeile 187 null?
> 2. Ist der FertigungsauftragService am Mandanten IDEAL vollständig installiert bzw. lizenziert?
> 3. Gibt es einen Beispiel-Request für ServiceOption 4, der bei euch funktioniert?

### Gegenprobe am AKE-Test (Freigabe Gerald 2026-11-20, nur FA 835)

Ziel war die Vermutung „bei IDEAL fehlt etwas in der DPS-Installation“. Dasselbe Skript wurde mit `-System AKE` aufgerufen
(`https://sagetest01.ake.at:5493`, `ake_TEST2026;1`). An IDEAL-Prod wurde dabei nichts gesendet.

| # | Aufruf | HTTP | Ergebnis |
|---|---|---|---|
| L | GET `PpsFaBelegePositionenInfo?where=BelID eq 835` (11:36) | 200 | 11 Positionen: 10 × `MA`, Pos. 11 (BelPosID 10656) `AG` **999** „TESTFertigung“, Arbeitsplatz 9999 |
| A | `{"ServiceOption":"AddOrChangePositionArbeitsgang","PositionenArbeitsgang":[{"Handle":835,"PositionHandle":0,"RessourceTyp":"Arbeitsgang","RessourceNummer":"999","Menge":1,"Bearbeiter":""}]}` (11:37:03) | 500 | `ResultNumber 500`, `ResultText` nil, `NullReferenceException … bei DPSBS.OL.SData.CommonWawiService.ServiceHandler.DPSbsSDataServiceHandler.Execute()`, ohne Zeilenangabe |

`Bearbeiter` war leer, weil der AKE-Test keine PPS-Mitarbeiter hat. Eine fachliche Prüfung hätte deshalb `ValidateMitarbeiter` gemeldet (§11).
Stattdessen kommt dieselbe NRE in derselben Methode wie bei IDEAL, also **vor** jeder fachlichen Prüfung. Angelegt wurde nichts; die Sperrdatei
blieb aus.

**Schluss (belegt):** Der Fehler hängt weder am Rumpf noch an der IDEAL-Installation. Er tritt auf zwei Servern mit zwei DPS-Builds gleich auf.
Entweder hat `DPSbsSDataServiceHandler.Execute()` für ServiceOption 4 einen Fehler, oder der Aufruf braucht etwas, das weder `$template` noch
`$schema` zeigt. Rückfrage 2 an DPS (Installation bei IDEAL) ist damit nachrangig. Die Frage an DPS lautet jetzt: „Auch am Mandanten
`ake_TEST2026;1` (FA 835, AG 999) dieselbe NRE in `Execute()`; was fehlt im Request?“ Weitere Sage-Schreibaufrufe bringen ohne DPS-Antwort nichts.
