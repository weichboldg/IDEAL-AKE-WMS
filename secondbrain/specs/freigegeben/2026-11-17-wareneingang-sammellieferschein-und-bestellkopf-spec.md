---
type: spec
title: Wareneingang – Sammellieferschein (Barcode mehrfach, CSV mit laufender Nummer) und Bestellkopf mit AB-Nummer und Kommission
slug: 2026-11-17-wareneingang-sammellieferschein-und-bestellkopf-spec
status: Testbereit
created: 2026-11-17
updated: 2026-11-17
source_backlog: "[[2026-11-17-wareneingang-sammellieferschein-csv-nummerierung]]"
source_backlog_2: "[[2026-11-17-wareneingang-bestellkopf-ab-nummer-kommission]]"
task: "[[2026-11-17-goal-lauf-wareneingang-sammellieferschein]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: AKE
affected_code:
  - IdealAkeWms/Controllers/PurchaseOrdersController.Receive.cs
  - IDEALAKEWMSService/Services/WareneingangCsvService.cs
  - IdealAkeWms/Services/WareneingangCsv.cs
  - IdealAkeWms/Models/PurchaseOrderReceipt.cs
  - IdealAkeWms/Models/SagePurchaseOrderLine.cs
  - IdealAkeWms/Data/ApplicationDbContext.cs
  - IdealAkeWms/Migrations/<neu>_AddSagePurchaseOrderLineAbNummerKommission.cs
  - IDEALAKEWMSService/Services/Sdata/EinkaufBestellungenSyncService.cs
  - IdealAkeWms/Models/ViewModels/PurchaseOrderReceiveViewModel.cs
  - IdealAkeWms/Views/PurchaseOrders/Receive.cshtml
  - IdealAkeWms/Controllers/PurchaseOrdersController.cs
  - IdealAkeWms/Views/PurchaseOrders/Index.cshtml
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs
  - IDEALAKEWMSService.Tests/Services/WareneingangCsvServiceTests.cs
  - IDEALAKEWMSService.Tests/Services/Sdata/EinkaufBestellungenSyncTests.cs
  - IdealAkeWms.Tests/Controllers/PurchaseOrdersReceiveTests.cs
  - IdealAkeWms.Tests/Controllers/PurchaseOrdersControllerTests.cs
  - SQL/<voraussichtlich 157>_AddSagePurchaseOrderLineAbNummerKommission.sql
  - SQL/00_FreshInstall.sql
  - scripts/deploy/nachlauf.psd1
  - IdealAkeWms/Views/Help/Index.cshtml
  - IdealAkeWms/Views/Help/Changelog.cshtml
  - IdealAkeWms/AppVersion.cs
  - IDEALAKEWMSService/AppVersion.cs
  - README.md
  - docs/anwender/05-wareneingang-zu-bestellung.md
  - docs/anwender/06-offene-bestellungen-rahmenabrufe.md
  - docs/TESTSZENARIEN.md
open_questions:
  - "1. enaio: Verarbeitet enaio <Barcode>_n.csv als weitere Bestellung zum selben Dokument (und liest den Barcode aus Spalte 1, nicht aus dem Dateinamen)? Prüfschritt E-1. Blockiert das Einschalten von Teil A auf Prod."
  - "2. Datenweg Teil B: Variante A (DPS belegt BelegProperty1/2 in EinkaufPositionenInfo mit USER_AB/USER_Kom) oder Variante B (zweiter Lesevorgang RealTimeData dtsEKBelegMst.Sage.Wawi im Dienst, braucht dieselbe DPS-Freigabe wie die FA-Struktur)? Teil B startet erst nach dieser Antwort."
  - "3. Sammellieferschein ohne Lieferschein-Nr.: Ist der zweite Einsatz eines Barcodes nur mit nicht leerer, gleicher Lieferschein-Nr. erlaubt?"
  - "4. Bestehende CSV-Ausgangszeilen mit Fehler 'Datei existiert bereits': nur anzeigen und von Hand 'CSV erneut schreiben' (dann nächste freie Nummer) – oder automatisch umstellen?"
  - "5. Spalten AB-Nummer und Kommission in den Listen 'Offene Bestellungen': standardmäßig sichtbar (ausblendbar) oder standardmäßig ausgeblendet?"
  - "6. Standort: Teil B jetzt nur AKE (IDEAL hat keinen Bestellungs-Abgleich) – reicht das, oder soll P-2 auch an IDEAL schon laufen?"
  - "7. Zeitpunkt des Sammellieferschein-Hinweises: nach dem Buchen in der Erfolgsmeldung – oder schon vor dem Buchen beim Scannen?"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: "Teil A bauen, Prod erst nach enaio-Bestätigung; Teil B Variante B RealTimeData dtsEKBelegMst; Spalten in Offene Bestellungen, bei Einbuchung nur Kopf; Rest Empfehlung"
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-17"
---

# Wareneingang – Sammellieferschein und Bestellkopf mit AB-Nummer und Kommission

> [!info] Nummern (vorläufig, beim Dev-Start prüfen)
> Belegt bis **v1.99.32**, SQL/151–155; die BF-Spec [[2026-11-13-blechfertigung-bf-auftraege-wicam-csv-spec]] plant SQL/156 und TS-155/156.
> Voraussichtlich daher: Version **nach** der BF-Spec, SQL **157** (nur Teil B), Testszenario **TS-157**. Der Dev-Lauf prüft alle drei
> gegen den Worktree (`AppVersion.cs`, `SQL/`, `docs/TESTSZENARIEN.md`), `secondbrain/specs/` und `secondbrain/aufgaben/` und trägt die echten
> Nummern hier ein.

## Ziel / Nutzen (das Warum)

**Teil A – Sammellieferschein.** Ein Lieferant schickt einen Lieferschein über mehrere Bestellungen. Im WMS wird zu jeder Bestellung ein
eigener Wareneingang gebucht, aber alle tragen denselben Lieferschein und damit denselben Barcode. Heute lehnt die Maske den zweiten Einsatz
des Barcodes ab (`PurchaseOrdersController.Receive.cs:107-109`), und selbst wenn sie es nicht täte, schriebe der Dienst die zweite CSV nie,
weil `<Barcode>.csv` schon existiert (`WareneingangCsvService.cs:113-117`, Fehler `KollisionText`, nie automatisch wiederholt
`WareneingangCsv.cs:44,47`). enaio bekäme also für die zweite Bestellung nichts.

**Teil B – Bestellkopf.** Das Lager sieht in „Wareneingang zu Bestellung“ und in den Listen „Offene Bestellungen“ (Lager und Einkauf) die
AB-Nummer und die Kommission aus dem Sage-Belegkopf (`KHKEKBelege.USER_AB`, `USER_Kom`). Die Felder dienen nur der Anzeige.

Entscheidung Gerald 2026-11-17 10:48 („passt“ zu den Empfehlungen in beiden Backlog-Notizen): laufende Nummer aus der Datenbank
(`<Barcode>.csv`, dann `_1`, `_2` …), Datei-Prüfung als zusätzlicher Schutz, Mehrfach-Barcode nur bei gleichem Lieferanten und gleicher
Lieferschein-Nr., CSV-Inhalt unverändert; AB-Nummer und Kommission im Bestellkopf und als ausblendbare Spalten in beiden Listen, nicht in
der enaio-CSV; P-1 und P-2 vor dem Dev-Start.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope Teil A**
- Erfassung: zweiter und weiterer Einsatz eines Barcodes, wenn Lieferant und Lieferschein-Nr. mit dem bisherigen Einsatz übereinstimmen;
  sonst Fehler wie heute. Hinweis „Sammellieferschein – Barcode bereits bei Bestellung … verwendet“.
- CSV-Dienst: Dateiname mit laufender Nummer aus `WareneingangCsvExporte`; vorhandene Datei → nächste freie Nummer statt `KollisionText`;
  `Datei` speichert den tatsächlichen Namen.
- Hilfe, Anwenderdoku 05, README (Abschnitt Wareneingang-CSV), Changelog, Testszenario, Manifest.

**In-Scope Teil B**
- Zwei Cache-Spalten an `SagePurchaseOrderLines`, befüllt vom Abgleich „Offene Bestellungen (Sage)“ (nur SData).
- Anzeige im Kopf der Maske „Wareneingang zu Bestellung“, Spalten mit Spaltenfilter in der gemeinsamen Liste (Lager und Einkauf).
- Melden statt still: fehlende Felder, Lesefehler und Füllgrad im Aktivitäts-Protokoll.
- Anwenderdoku 05/06, Hilfe, Changelog, Testszenario, Migration + SQL + FreshInstall + Manifest.

**Out-of-Scope**
- Keine Änderung am CSV-**Inhalt** (9 Felder, Reihenfolge = Vertrag mit enaio, [[fallstricke]] §32). AB-Nummer und Kommission kommen **nicht**
  in die enaio-CSV. (Der allgemeine CSV-Export der Liste „Offene Bestellungen“ enthält die neuen Spalten automatisch, weil er alle Spalten mit
  `data-col-key` ausgibt – das ist ein anderer Export, siehe [[fallstricke]] §23.)
- Keine Anzeige des Dateinamens in „Sage-Wareneingänge“ (Spalte CSV bleibt „geschrieben …“, `WareneingangCsv.cs:71`). Bei Bedarf eigene Notiz.
- Keine Sperre gegen das bekannte Gleichzeitig-Buchen auf zwei Seiten (`docs/anwender/05-…md:160-161`); siehe Technik A3.
- `USER_ABErhalten`, `USER_WA` und die übrigen Kopf-Benutzerfelder aus P-1 (Backlog Z. 37-39) – nicht angefragt.
- Lieferverzug-Mail, Bestellvorschläge, Sage-Wareneingangsbeleg: unverändert.
- Kein Schreiben nach Sage. IDEAL: siehe Rückfrage 6.

## Fachliche Anforderungen

### Teil A – Sammellieferschein

**A1 – Zweiter Einsatz eines Barcodes.** Beim Buchen mit Barcode (`vm.Barcode != null`, Sage-WE aktiv, gültiger Barcode) gilt statt der
heutigen Sperre (`Receive.cs:107-109`):
- Es gibt noch keinen Wareneingang mit diesem Barcode (ohne Groß/Klein, wie heute `PurchaseOrderReceiptRepository.cs:137-142`) → buchen wie heute.
- Es gibt einen. Verglichen wird mit dem **ersten** Wareneingang dieses Barcodes (Rückgabe von `BarcodeVergebenAsync`):
  - **gleicher Lieferant**: `LieferantKto` des ersten Wareneingangs = `LieferantKto` der aktuellen Bestellung (`rows[0].Line.LieferantKto`),
    getrimmt, ohne Groß/Klein; ist einer der beiden leer, gilt das als ungleich,
  - **gleiche Lieferschein-Nr.**: `Lieferschein` des ersten Wareneingangs = `vm.Lieferschein` (bereits getrimmt, `Receive.cs:91`), ohne Groß/Klein;
    beide müssen nicht leer sein (Rückfrage 3, Empfehlung).
  - Beides erfüllt → buchen. Die Erfolgsmeldung bekommt einen zweiten Satz:
    „Sammellieferschein – Barcode {Barcode} bereits bei Bestellung {Belegnummer des ersten} verwendet; die CSV-Datei erhält eine laufende
    Nummer (_1, _2 …).“ (Zeitpunkt: Rückfrage 7.)
  - Sonst Fehler, nichts gebucht. Der Text beginnt wie heute (Regressionsschutz `PurchaseOrdersReceiveTests.cs:318`) und nennt den Grund:
    „Barcode {B} ist bereits dem Wareneingang zu Bestellung {Nr} vom {Datum} zugeordnet – anderer Lieferant.“ bzw.
    „… – für einen Sammellieferschein bitte dieselbe Lieferschein-Nr. eingeben (dort: {LS}).“ bzw. bei leerer Nr. dort
    „… – dort ohne Lieferschein-Nr., kein Sammellieferschein möglich.“

Vergleich mit dem ersten reicht: Weitere Einsätze entstehen nur über genau diese Regel, also haben alle denselben Lieferanten und dieselbe
Lieferschein-Nr. wie der erste (Ausnahme: Gleichzeitig-Buchen, A3).

**A2 – Dateiname.** Der Dienst vergibt beim Schreiben jeder Ausgangszeile den Namen:
1. Start-Nummer `n` = Anzahl der Ausgangszeilen mit **demselben Barcode** (ohne Groß/Klein), die schon eine Datei geschrieben haben
   (`Datei IS NOT NULL`; `Datei` wird nur bei Erfolg gesetzt, `WareneingangCsvService.cs:79-82`). Die eigene Zeile zählt nicht.
2. Name(`n`) = `n == 0 ? <Barcode>.csv : <Barcode>_<n>.csv` (Barcode wie in der Ausgangszeile, keine Umformung).
3. Ist Name(`n`) **belegt**, `n + 1` und erneut prüfen. Belegt heißt: Eine **andere** Ausgangszeile hat diesen Dateinamen in `Datei`
   (Vergleich über den Dateinamen ohne Pfad, ohne Groß/Klein – egal, welcher Barcode), **oder** die Datei liegt im Zielordner (`File.Exists`).
4. Geschrieben wird wie bisher atomar über `.tmp` + `File.Move(…, overwrite: false)` ([[fallstricke]] §32). `Datei` = voller Pfad des
   tatsächlich geschriebenen Namens (heute fest `e.Barcode + ".csv"`, `WareneingangCsvService.cs:81` – das ist der zweite Ort, den die
   Änderung treffen muss).
5. Inhalt unverändert: Feld 1 = `e.Barcode` ohne Suffix (`CsvInhalt`, `WareneingangCsvService.cs:136-145`).

Folgen:
- Die Nummer kommt aus der Datenbank, nicht aus dem Ordner. Hat enaio `<Barcode>.csv` schon abgeholt, bekommt der zweite Vorgang trotzdem
  `_1` – genau der Grund für die Entscheidung (Backlog Sammellieferschein, Empfehlung 1).
- `KollisionText` entsteht im Schreibpfad nicht mehr. Die Konstante bleibt für bestehende Zeilen und für „nie automatisch wiederholen“
  (`WareneingangCsv.cs:44,47`; Altzeilen: Rückfrage 4).
- Prüfung 3 über **alle** Barcodes schützt einen Sonderfall: Barcode `ABC` (zweite Datei `ABC_1.csv`) und ein anderer Barcode `ABC_1`
  (Regex erlaubt `_` und Ziffern, `WareneingangCsv.cs:31-32`). Der zweite bekommt dann `ABC_1_1.csv`. Deshalb muss enaio den Barcode aus
  Spalte 1 lesen, nicht aus dem Dateinamen (Rückfrage 1).

**A3 – Nebenläufigkeit.**
- *Zwei Exporte mit gleichem Barcode im selben Takt:* Die Schleife arbeitet die Zeilen nacheinander ab und speichert nach jeder Zeile
  (`WareneingangCsvService.cs:72-96`, `SaveChangesAsync` Z. 95). Die Namensvergabe der zweiten Zeile liest die Datenbank **nach** dem Speichern
  der ersten, sieht also deren `Datei`. Keine weitere Sperre nötig.
- *Zwei Dienste gleichzeitig:* Nicht vorgesehen – der Dienst setzt genau eine Instanz voraus (`WareneingangCsvService.cs:17`, wie
  [[fallstricke]] §42). Bleibt so.
- *Datei erscheint zwischen Prüfung und Umbenennen:* `File.Move(…, overwrite: false)` wirft, der Fehler wird wie jeder Schreibfehler
  gezählt und nach dem Abstand automatisch wiederholt (nicht in `NieWiederholen`). Der nächste Versuch nimmt die nächste freie Nummer.
- *Gleichzeitig buchen auf zwei Seiten (Maske):* Die Barcode-Prüfung ist weiter „prüfen, dann speichern“ ohne Index; zwei Seiten können
  denselben Barcode im selben Moment annehmen (bekannt, `docs/anwender/05-…md:160-161`). Neu: Statt `KollisionText` bekommt der zweite
  Vorgang `_1` – auch bei verschiedenem Lieferanten. Akzeptiert (selten, nicht neu, jetzt ohne hängenden Fehler).
- *Bekannte Grenze (unverändert seit v1.99.11):* Gelingt das Umbenennen, scheitert aber danach das Speichern, bleibt die Zeile offen. Liegt
  die Datei beim nächsten Takt noch im Ordner, bekommt der Vorgang zusätzlich die nächste Nummer (Inhalt doppelt); hat enaio sie schon
  abgeholt, wird derselbe Name erneut geschrieben. Im Code als `ponytail:`-Kommentar festhalten.

**A4 – „CSV erneut schreiben“.** Setzt wie heute nur Fehler-Zeilen auf Offen (`PurchaseOrderReceiptRepository.cs:159-168`). Der Name wird
beim nächsten Takt nach A2 neu vergeben. Eine Fehler-Zeile hat nie eine `Datei` (wird nur bei Erfolg gesetzt), zählt also nicht mit.

**Schema-Änderung Teil A: keine.** Alles Nötige steht schon in `WareneingangCsvExporte`: `Barcode` (Pflicht, 50), `Datei` (400), Status
(`ApplicationDbContext.cs:1489-1503`). Ein eindeutiger Index auf den Dateinamen ist nicht nötig, weil nur eine Dienst-Instanz schreibt (A3)
und `Datei` den vollen Pfad hält (der Pfad kann über `WareneingangCsvPfad` wechseln; ein Index darauf schützte den Namen nicht).

### Teil B – AB-Nummer und Kommission

**B1 – Quelle.** Nur SData, wie der ganze Abgleich (`EinkaufBestellungenSyncService.cs:30`, kein View-Weg). Stand der Belege:
- `EinkaufPositionenInfo` und `EinkaufBelegekopfInfo` liefern mit `include=SpecificUserfields` **keine** USER-Felder, nur leere
  DPS-Platzhalter `BelegProperty1..8` (P-1, Backlog Bestellkopf Z. 35-36).
- `RealTimeData/dtsEKBelegMst.Sage.Wawi` liefert `KHKEKBelege_USER_AB` und `KHKEKBelege_USER_Kom` (mit Tabellen-Präfix wie R1 der FA-Struktur,
  [[fallstricke]] §41). Ein `where` auf die USER-Namen bricht mit SQL-Fehler ab; filtern nur über `BelID` u. a. (Backlog Z. 40-42).
- Damit gibt es zwei Wege (Rückfrage 2):
  - **Variante A:** DPS belegt `BelegProperty1` = `USER_AB`, `BelegProperty2` = `USER_Kom` in `EinkaufPositionenInfo`. Code: zwei Felder mehr im
    `Select` (`EinkaufBestellungenSyncService.cs:36-38`) und im Zeilen-Record (`:11-15`). Kein zweiter Leseweg.
  - **Variante B:** Nach dem Lesen der Positionen je offenem Beleg ein RealTimeData-Lesevorgang `dtsEKBelegMst.Sage.Wawi`,
    `where` = `BelID eq … or …` in Blöcken zu 50 (`SageSdataQuellen.OrBloecke`, `SageSdataFaModels.cs:89-97`), `select=BelID,
    KHKEKBelege_USER_AB,KHKEKBelege_USER_Kom`, `orderBy=BelID` (Pflicht, `SageSdataReadClient.cs:79-82`). Muster:
    `SdataFaStrukturReader.BelegsucheAsync` (`SdataFaStrukturReader.cs:196-212`). **Braucht die DPS-Freigabe für RealTimeData im Dienst**, die
    Gerald für die FA-Struktur ohnehin anfragt (Freigabe-Antwort 1 der [[2026-11-16-fa-struktur-ideal-ueber-sdata-spec]]; Regel heute nur dort
    in `FaStrukturQuelleSwitch.RealTimeDataErlaubt`, [[fallstricke]] §40).
  - *Empfehlung:* Gerald fragt DPS **beides in einer Anfrage**: (1) `BelegProperty1/2` belegen, (2) RealTimeData im Dienst freigeben. A
    bevorzugt (Konfiguration statt Code, ein Leseweg). B nur, wenn A nicht geht und (2) freigegeben ist.

**B2 – Speicherort.** Zwei neue, nullable Spalten am Cache `SagePurchaseOrderLines`: `AbNummer`, `Kommission` (Kopfwert, an jeder Position
des Belegs gleich – wie `LieferantName`, das ebenfalls Kopfwert je Zeile ist, `EinkaufBestellungenSyncService.cs:182`). Begründung: Der
Cache wird je Lauf vollständig ersetzt (`:210-217`); eine eigene Kopftabelle bräuchte einen zweiten Vollersatz in derselben Transaktion,
ohne Nutzen. Länge = Sage-Spaltenlänge laut **P-2a** (vorher nicht festlegen). Ist ein Wert trotzdem länger: nicht schreiben, Zählwert +
Warnung (Regel [[fallstricke]] „Sage-Spalten sind teils länger als die WMS-Spalten“, Abschnitt Sage-SData-Lesen) – nie kürzen.

**B3 – Werte.** Getrimmt, leer → `null` (vorhandener Helfer `Text`, `EinkaufBestellungenSyncService.cs:247`). Zahl oder Text: wie Sage liefert
(`AlsText`-Muster, `SdataFaStrukturReader.cs:214-215`); Format laut P-2.

**B4 – Melden statt still.**
- *Feld fehlt in der Antwort* (Variante B: Schlüssel `KHKEKBelege_USER_AB` bzw. `…_USER_Kom` fehlt in **jeder** gelieferten Zeile): eine
  Warnung je Lauf „Sage liefert {Feld} nicht (Feldname/Präfix prüfen) – AB-Nummer bzw. Kommission bleibt leer.“ Grund: Unbekannte Namen im
  RealTimeData-`select` kommen still nicht ([[fallstricke]] §41). Ein vorhandener Schlüssel mit leerem Wert ist dagegen „leer in Sage“.
  Variante A: ein unbekannter Name im `select` gibt HTTP 500 ([[fallstricke]] Sage-SData-Lesen), der Lauf scheitert laut wie heute; leere Werte
  sind nicht von „nicht belegt“ unterscheidbar → nur der Füllgrad als Info.
- *Lesefehler beim Kopf-Lesen (nur Variante B):* Der Positions-Abgleich läuft trotzdem durch (sonst blockiert ein Anzeigefeld den
  Wareneingang). AB-Nummer und Kommission werden je `BelId` aus dem bisherigen Cache übernommen (`bestand`, `:117`), Warnung
  „Kopfdaten (AB-Nummer/Kommission) nicht lesbar: {Fehler} – Werte vom letzten Lauf.“
- *Kein Kopf zu einer BelID* (Variante B): Zählwert, Werte leer.
- Neue deutsche Counts-Keys (ADR 0010): `ab_nummer` (Belege mit Wert), `kommission` (Belege mit Wert), `kopf_ohne_treffer`, `kopf_zu_lang`.

**B5 – Anzeige.**
- *Maske „Wareneingang zu Bestellung“:* Im Kopf nach „Lieferant“ zwei Zeilen „AB-Nummer“ und „Kommission“
  (`Receive.cshtml:56-57`). Leer → „–“ (Feld sichtbar, damit klar ist, dass es nichts gibt). Quelle: erste Cache-Zeile der Bestellung
  (`k = rows[0].Line`, `Receive.cs:185`), zwei neue Eigenschaften im `PurchaseOrderReceiveViewModel`.
- *Listen „Offene Bestellungen“ (Lager und Einkauf):* Es gibt **eine** Liste für beide (`PurchaseOrdersController.ListeAsync`,
  `PurchaseOrdersController.cs:114-192`; der Einkauf ruft sie über `EinkaufPurchaseOrdersController.cs:26`), **eine** View
  (`Views/PurchaseOrders/Index.cshtml`, `data-view-key="PurchaseOrders"`, Z. 117) und **einen** Spaltenkatalog
  (`ColumnDefinitions.PurchaseOrders`, `ColumnDefinitions.cs:744-764`). Also je Spalte genau ein Eintrag an drei Stellen:
  `<th data-filterable data-col-key="ab-nummer">AB-Nummer</th>` / `kommission` nach „Bestellung“ (Z. 122), `ColumnMap`-Getter mit dem
  gerenderten Zelltext (`PurchaseOrdersController.cs:47-61`, [[fallstricke]] §26 „Getter rendern wie die Zelle“), `ColumnDef(…, Locked: false,
  DefaultHidden: <Rückfrage 5>)`. Textspalten brauchen keinen Eintrag im Typ-Register (`ColumnFilterSyntax.cs:80` listet nur Datum/Zahl).
  Die Spaltenwahl ist je Benutzer für Lager und Einkauf **gemeinsam** (gleicher View-Key) – Bestand, nicht neu.

## Technischer Loesungsentwurf

Kleinste Lösung (ponytail): kein neuer Typ, keine neue Abhängigkeit, kein neuer Schalter. Reihenfolge im Dev-Lauf: **Teil A** (Commit 1,
unabhängig von Rückfrage 2), **Teil B** (Commit 2, erst nach Antwort 2 und P-2).

**Teil A**
1. `Receive.cs:107-109`: `BarcodeVergebenAsync` bleibt (Rückgabe = erster Wareneingang). Der Vergleich Lieferant/Lieferschein steht direkt
   dort (A1); bei Gleichheit kein Fehler, sondern ein Merker für den Hinweis in der Erfolgsmeldung (`Receive.cs:171`). Kein neuer
   Repository-Aufruf. `TempData` nur `SuccessMessage`/`WarningMessage` (CLAUDE.md).
2. `WareneingangCsvService.Schreiben` (`:110-129`): liefert zusätzlich den geschriebenen Pfad (z. B. Tupel oder `out`); `File.Exists(ziel)
   → KollisionText` (Z. 117) entfällt zugunsten der Namensvergabe A2. `RunOnceAsync` setzt `e.Datei` aus dem Rückgabewert (Z. 81).
   `.tmp`-Name bleibt `<Barcode>.tmp` (Zeilen laufen nacheinander, A3).
3. Namensvergabe A2 als kleine Methode im Dienst: eine Abfrage der belegten Dateinamen (`Datei != null`, `Id != e.Id`, Vergleich in C# über
   `Path.GetFileName` mit `StringComparer.OrdinalIgnoreCase`), Start-Nummer aus den Zeilen mit gleichem Barcode (ohne Groß/Klein:
   `ToUpper` auf beiden Seiten wie `PurchaseOrderReceiptRepository.cs:136-141`, [[fallstricke]] §26 „Groß/Klein in SQL macht die Kollation“),
   dann Schleife mit `File.Exists`. Vorfilter der Abfrage z. B. `Datei.ToUpper().Contains(<BARCODE>)` – der Rest in C#.
4. Doku-Kommentare nachziehen: `PurchaseOrderReceipt.Barcode` („Im WMS nur an EINEM Absenden“, `PurchaseOrderReceipt.cs:67-69`),
   Klassenkommentar `WareneingangCsvService.cs:13-18` („nie ueberschreiben“ bleibt, „<Barcode>.csv“ → mit laufender Nummer).

**Teil B**
1. Modell `SagePurchaseOrderLine` + `AbNummer`, `Kommission` (`string?`), `HasMaxLength(<P-2a>)` in `ApplicationDbContext.cs:1413-1437`.
2. `dotnet ef migrations add AddSagePurchaseOrderLineAbNummerKommission` → SQL-Skript → FreshInstall (Migrations-Abschnitt).
3. `EinkaufBestellungenSyncService`: Variante A oder B nach Rückfrage 2 (B1); Zuordnung in `new SagePurchaseOrderLine { … }` (`:179-189`)
   über `BelId`; Meldungen B4; Counts in `FinishSuccessAsync` (`:220-235`). Gilt für beide Aufrufer (regulärer Lauf und Anstoß nach Sage-WE,
   `:69-83`), weil beide `RunCoreAsync` nutzen.
4. ViewModel + `Receive.cshtml`, `Index.cshtml`, `ColumnMap`, `ColumnDefinitions` (B5).

Muster: Listen-View-Pattern ADR 0005 (Server-Mode-Spaltenfilter, `data-col-key` an jedem `<th>`, Pagination unverändert), SData-Lesen wie
ADR 0029/[[fallstricke]] §40/§41, Aktivitäts-Protokoll ADR 0010. Module: [[module]] Wareneingang, Dienst `SageBookingWorker` →
`WareneingangCsvService` (`SageBookingWorker.cs:217`), Abgleich „Offene Bestellungen (Sage)“.

## Migrations-/SQL-Auswirkungen

- **Teil A: keine** (siehe A4 „Schema-Änderung Teil A“).
- **Teil B: ja, additiv, nicht daten-destruktiv.**
  - EF-Migration `AddSagePurchaseOrderLineAbNummerKommission`.
  - `SQL/<voraussichtlich 157>_AddSagePurchaseOrderLineAbNummerKommission.sql`: Kopf wie `SQL/155` (Zweck, Version, Spec, „rein additiv,
    füllt der nächste Abgleich“), Guard „falsche Datenbank“ auf `dbo.SagePurchaseOrderLines`, je Spalte
    `IF COL_LENGTH(N'dbo.SagePurchaseOrderLines', N'AbNummer') IS NULL ALTER TABLE … ADD [AbNummer] NVARCHAR(<P-2a>) NULL;` im eigenen Batch,
    History-Insert in separatem Batch, `SET NOEXEC OFF`.
  - `SQL/00_FreshInstall.sql` an **zwei** Stellen: Spalten im `CREATE TABLE [dbo].[SagePurchaseOrderLines]` (Z. 2360 ff.) **und** der
    MigrationId-Insert mit Bedingung „Spalte vorhanden, sonst steht SQL/157 noch aus“ (Muster Z. 3344).
  - `SQL/AgentJobs/`: nicht betroffen (kein Treffer `SagePurchaseOrderLines` dort; der Cache wird nur vom Dienst geschrieben,
    `EinkaufBestellungenSyncService.cs:213-215`).
  - Kein DB-Backup-Sonderfall (nur zwei NULL-Spalten). Die Werte erscheinen nach dem ersten Abgleich nach dem Deploy.

## Einstellungen

keine. Die Sammellieferschein-Regel gilt ohne Schalter (entschieden, Umschaltung wäre YAGNI). `WareneingangCsvAktiv`, `WareneingangCsvPfad`,
`WareneingangBarcodeAktiv`, `Sync:EinkaufBestellungenEnabled` bleiben unverändert.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

- `WareneingangCsvExporte`: Modified* setzt der Dienst wie bisher (`WareneingangCsvService.cs:94`, Kennung `system:wareneingang-csv`);
  „CSV erneut schreiben“ wie bisher mit Benutzer (`PurchaseOrderReceiptRepository.cs:165`).
- `PurchaseOrderReceipts`: unverändert (Erfassung wie heute, `Receive.cs:145-156`).
- `SagePurchaseOrderLines`: kein `AuditableEntity` (Vollersatz, `SagePurchaseOrderLine.cs:7-8`); Nachvollziehbarkeit über `SyncedAt` und das
  Aktivitäts-Protokoll.
- Rollen/Filter: keine Änderung. Buchen weiter `RequireStockAccess` (`Receive.cs:43`), Listen `RequireStockReadAccess` +
  `RequireWareneingangBestellungAktiv` (`PurchaseOrdersController.cs:17-18`) bzw. `RequireEinkaufAccess` + `RequireEinkaufAktiv`
  (`EinkaufPurchaseOrdersController.cs:14-15`).
- Listen-View-Pattern: keine neue Tabelle; zwei Spalten in einer bestehenden Server-Mode-Tabelle mit Spaltenfilter, Katalog und Zahnrad.

## Akzeptanzkriterien

**Teil A**
1. **Gleicher Lieferant + gleiche LS-Nr.:** Bestellung X (Lieferant K, LS `L1`, Barcode `B`) ist gebucht. Bestellung Y desselben Lieferanten
   mit LS `l1` (andere Schreibweise) und Barcode `b` → gebucht; Erfolgsmeldung enthält „Sammellieferschein – Barcode b bereits bei Bestellung X
   verwendet“.
2. **Anderer Lieferant:** wie 1, aber Y von Lieferant K2 → nichts gebucht, Meldung beginnt mit „Barcode b ist bereits dem Wareneingang zu
   Bestellung X vom “ und endet mit „anderer Lieferant.“
3. **Andere LS-Nr.:** gleicher Lieferant, LS `L2` → nichts gebucht, Meldung nennt „dieselbe Lieferschein-Nr. … (dort: L1)“.
4. **LS leer:** gleicher Lieferant, LS leer (oder bei X leer) → nichts gebucht (Rückfrage 3).
5. **Nummerierung im Takt:** Zwei übergebene Vorgänge mit Barcode `B` → `B.csv` und `B_1.csv`; `Datei` je Zeile = tatsächlicher Pfad;
   Feld 1 beider Dateien = `B`; 9 Felder, sonst unverändert (TS-153-Format).
6. **Nach Abholung:** `B.csv` wurde entfernt (enaio), dann wird der zweite Vorgang übergeben → `B_1.csv`, nicht `B.csv`.
7. **Groß/Klein:** erster Vorgang `B`, zweiter `b` → zweite Datei `b_1.csv`.
8. **Fremde Datei:** `B_1.csv` liegt schon im Ordner (keine DB-Zeile) → zweiter Vorgang schreibt `B_2.csv`; `B_1.csv` unverändert; kein
   `KollisionText`, kein Fehler, kein Zählwert `fehler`.
9. **Fremder Barcode mit Suffix-Namen:** Ausgangszeile von Barcode `B_1` hat `B_1.csv` geschrieben; zweiter Vorgang von `B` → `B_2.csv`.
10. **Altzeile Kollision:** Eine Zeile mit `Fehler = KollisionText` wird weiter nicht automatisch wiederholt; nach „CSV erneut schreiben“ schreibt
    der nächste Takt die nächste freie Nummer.
11. **Erneut schreiben nach Pfadfehler:** Zeile mit Pfadfehler, danach erreichbar → Name nach A2 (Fehler-Zeilen zählen nicht mit).
12. **Bestand:** Ein Barcode, der nur einmal verwendet wird, ergibt weiter `<Barcode>.csv`; Format, BOM, CRLF, Erfasser unverändert (TS-153).

**Teil B** (nach Rückfrage 2)
13. **Kopf Maske:** Bestellung mit AB-Nummer `AB-4711` und Kommission `K-12` in Sage → Maske zeigt beide unter „Lieferant“; ohne Werte „–“.
14. **Listen:** In Lager → Offene Bestellungen und Einkauf → Offene Bestellungen stehen die Spalten „AB-Nummer“ und „Kommission“, jede Position des
    Belegs mit demselben Wert; Spaltenfilter `4711` findet die Zeilen; Zahnrad blendet sie aus (Einstellung bleibt nach Neuladen).
15. **Default-Sichtbarkeit** wie Rückfrage 5.
16. **Feld fehlt (Variante B):** Antwort ohne Schlüssel `KHKEKBelege_USER_AB` → Warnung im Protokoll „Sage liefert KHKEKBelege_USER_AB nicht …“,
    Werte leer, Lauf erfolgreich.
17. **Lesefehler Kopf (Variante B):** Kopf-Lesen scheitert → Positionen trotzdem aktualisiert, AB/Kommission aus dem vorigen Lauf, Warnung.
18. **Zu lang:** Wert länger als die Spalte → `null` + Zählwert `kopf_zu_lang` + Warnung; Lauf erfolgreich.
19. **Nicht in der enaio-CSV:** Die Wareneingang-CSV hat weiter 9 Felder ohne AB/Kommission.
20. **Counts:** Protokoll-Eintrag „Offene Bestellungen (Sage)“ enthält `ab_nummer`, `kommission` (und bei B `kopf_ohne_treffer`, `kopf_zu_lang`).

**Allgemein**
21. Build und `dotnet test` grün; `NachlaufManifestDriftTests` grün; `FreshInstallIndexSpaltenTests` grün.
22. IDEAL (DEV): keine sichtbare Änderung außer zwei leeren Spalten bzw. „–“, falls die Liste dort erreichbar ist; Schema angewendet.

## Test-Szenarien

**Unit-Tests**
- `WareneingangCsvServiceTests`: neu AK 5–9, 11 (Dateien im Temp-Ordner wie bisher); **Bestandstest geändert:**
  `DateiExistiert_NieUeberschrieben_Fehler_Warnung_KeineAutoWiederholung_AK11` (`WareneingangCsvServiceTests.cs:124-145`) erwartet jetzt
  `BC1_1.csv` geschrieben, `BC1.csv` = „fremd“ unverändert, Status Geschrieben – Verhalten **absichtlich** geändert (AK 8). Ein eigener Test
  sichert, dass eine Altzeile mit `KollisionText` nicht automatisch läuft (AK 10).
- `PurchaseOrdersReceiveTests`: AK 1–4; `Barcode_Duplikat_OhneGrossKlein_Fehler_MitBestellungUndDatum_AK4` (`:311-319`) bleibt grün (beide ohne
  LS → Fehler, Präfix unverändert).
- `EinkaufBestellungenSyncTests`: AK 16–18, 20 mit echten Feldnamen aus der Feldliste ([[fallstricke]] §41 „Testantworten immer mit den echten
  Feldnamen“); Zuordnung über `BelId`; Dry-Run schreibt nichts.
- `PurchaseOrdersControllerTests`: Spaltenfilter `ab-nummer`/`kommission` über `ColumnMap`; Katalog enthält die Keys (Prefs-API sonst 400,
  [[fallstricke]] §3 „Neuer viewKey …“ sinngemäß für neue Spalten).

**Manuell: neues Kapitel `TS-157` (voraussichtlich) in `docs/TESTSZENARIEN.md`**, Hinweise an TS-129.5 („ab v1.99.x Sammellieferschein
erlaubt, siehe TS-157“) und TS-129.14 („Kollision: jetzt nächste Nummer, siehe TS-157“). Danach [[testszenarien-index]].

Vorbedingungen: DEV AKE, Worktree-Stand, `WareneingangBarcodeAktiv` an, Test-Freigabe als Zielordner, Übergabe per SQL bzw. „Als übergeben
markieren“ (kein Sage-Schreiben durch Agenten). Zwei offene Bestellungen X, Y desselben Lieferanten K, eine Bestellung Z von K2. SQL-Eingriffe nur
auf DEV, Ausgangswerte notieren und zurücksetzen.

| Nr | Schritt | Erwartet |
|---|---|---|
| 157.1 | X buchen: LS `L1`, Barcode `B` | gebucht wie heute |
| 157.2 | Y buchen: LS `l1`, Barcode `b` | gebucht; Erfolgsmeldung mit „Sammellieferschein – Barcode b bereits bei Bestellung X verwendet …“ (AK 1) |
| 157.3 | Negativ: Z (Lieferant K2), LS `L1`, Barcode `B` | nichts gebucht, „… zugeordnet – anderer Lieferant.“ (AK 2) |
| 157.4 | Negativ: weitere Bestellung von K, LS `L2`, Barcode `B` | nichts gebucht, „… dieselbe Lieferschein-Nr. … (dort: L1)“ (AK 3) |
| 157.5 | Negativ: weitere Bestellung von K, LS leer, Barcode `B` | nichts gebucht (AK 4) |
| 157.6 | X und Y übergeben, ein Takt | `B.csv` und `b_1.csv`, Feld 1 = `B` bzw. `b`; SQL `SELECT Barcode, Datei FROM dbo.WareneingangCsvExporte` zeigt beide Pfade (AK 5, 7) |
| 157.7 | `B.csv` aus dem Ordner entfernen (enaio simuliert), dritte Bestellung von K mit `L1`/`B` buchen und übergeben | `B_2.csv`, nicht `B.csv` (AK 6) |
| 157.8 | Negativ: vor der Übergabe einer weiteren Bestellung `B_3.csv` von Hand anlegen | `B_4.csv`; `B_3.csv` unverändert; Spalte CSV „geschrieben“, kein Fehler (AK 8) |
| 157.9 | Altzeile: `UPDATE dbo.WareneingangCsvExporte SET Status = 3, Fehler = <KollisionText>, Datei = NULL WHERE Id = <neue Zeile>` | auch nach 15 min kein Versuch; „CSV erneut schreiben“ → nächster Takt schreibt die nächste freie Nummer (AK 10) |
| 157.10 | Bestand: Bestellung mit neuem Barcode `C` | `C.csv`, Format wie TS-153 (AK 12) |
| 157.11 | Teil B: Bestellung mit AB/Kommission in Sage-Test (aus P-2c) nach einem Abgleich öffnen | Maske zeigt beide; Bestellung ohne Werte zeigt „–“ (AK 13) |
| 157.12 | Lager → Offene Bestellungen und Einkauf → Offene Bestellungen | Spalten vorhanden, Filter `<Teil der AB-Nummer>` trifft, Zahnrad blendet aus und merkt es (AK 14, 15) |
| 157.13 | Aktivitäts-Protokoll „Offene Bestellungen (Sage)“ | Counts `ab_nummer`, `kommission`; keine Warnung „liefert … nicht“ (AK 20) |
| 157.14 | Negativ Variante B (nur DEV, Wegwerf-Sonde wie TS-153): Feldname ohne Präfix | Warnung „Sage liefert … nicht“, Werte leer (AK 16) |
| 157.15 | enaio-CSV aus 157.6 | 9 Felder, keine AB/Kommission (AK 19) |
| 157.16 | DEV IDEAL nach Deploy | Schema da (`COL_LENGTH`), keine sichtbare Änderung bzw. leere Spalten (AK 22) |

Der echte Dienstweg (Takt mit Sage-WE an) ist auf DEV nur mit Sage-Schreiben möglich → wie TS-153 per Sonde, echter Weg Manual-UAT auf dem
Testsystem.

## Etappen (nur bei epic: true)

Entfällt (`epic: false`). Teil A und Teil B sind getrennte Commits im selben Lauf (siehe Lösungsentwurf).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

**Final (QA-Lauf 2026-11-17, gegen den echten Diff `5960b6a5..ed919976`):** Web **ja** (`IdealAkeWms/` Controller, Views, Modell,
ViewModel, Hilfe, Changelog), Dienst **ja** (`IDEALAKEWMSService/Services/WareneingangCsvService.cs`, `Sdata/EinkaufBestellungenSyncService.cs`),
Migration **ja** (`20261117102804_AddSagePurchaseOrderLineAbNummerKommission` = `SQL/156`, additiv, kein Backup-Sonderfall). Kein Daten-/Einmal-Skript,
keine Einstellung. **Paket:** `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` (IDEALAKEWMSWeb + IDEALAKEWMSWebService,
`SQL\`, `deploy\`, `publish-info.txt` Version 1.99.33, Commit `ed919976bbc1b24809b56e880b3fbe52f954f9b9`); die alte `publish\publish.zip` (10:19)
ist veraltet – nicht verwenden. Reihenfolge (Update-Skript-Standard): Backup → SQL/156 (Schema) → Web → Dienst; Info `v1.99.33-WareneingangCsvSammellieferschein`
steht in Phase **Vorher** (AKE-Prod erst nach E-1), `Pruefung/WareneingangCsvKollision` in Phase Nachher. Nach dem Merge nur dann neu aus `main`
publishen, wenn der Merge getestete Dateien mit parallelen main-Änderungen zusammengeführt hat.

*Ursprünglich vorläufig (Spec-Agent):*

- **Web-App:** ja (Maske, Liste, Spaltenkatalog, Hilfe, Changelog, Version).
- **Service:** ja (`WareneingangCsvService`, `EinkaufBestellungenSyncService`).
- **Migration:** ja (nur Teil B, `SQL/<voraussichtlich 157>`, additiv). Reihenfolge: Schema vor dem Start von Web und Dienst (Standard des
  Update-Skripts).
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023; nur ASCII, Kopf Z. 2):
  ```powershell
  # Teil B – Block Schema, in Deploy-Reihenfolge nach SQL/156 (BF-Spec)
  @{ Id='SQL/157'; Version='<x>'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'
     Datei='157_AddSagePurchaseOrderLineAbNummerKommission.sql'; MigrationId='<neu>_AddSagePurchaseOrderLineAbNummerKommission'
     Voraussetzung='additiv'; Quelle='<changelog-datei>' }
  # Teil A – Altzeilen sichtbar machen (Rueckfrage 4, Empfehlung)
  @{ Id='Pruefung/WareneingangCsvKollision'; Version='<x>'; Typ='Pruefung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'
     Pruefung='SELECT COUNT(*) FROM dbo.WareneingangCsvExporte WHERE Status = 3 AND Fehler LIKE N''Datei existiert bereits%'''
     Text='Wareneingang-CSV: Zeilen mit Fehler "Datei existiert bereits". Seit <x> nummeriert der Dienst (<Barcode>_n.csv). Pro Zeile auf der Freigabe pruefen, dann in Lager > Sage-Wareneingaenge "CSV erneut schreiben".' }
  # Teil A – Hinweis an enaio (Rueckfrage 1)
  @{ Id='v<x>-WareneingangCsvSammellieferschein'; Version='<x>'; Typ='Info'; Firma='AKE'; Phase='Vorher'; Ausfuehrung='anzeigen'
     Text='Wareneingang-CSV: Sammellieferschein ergibt <Barcode>_1.csv, _2 ... Vor dem Deploy auf Prod mit dem enaio-Betreuer bestaetigen (Spec Pruefschritt E-1); Barcode steht unveraendert in Spalte 1.'
     Quelle='<changelog-datei>' }
  ```
  - `LIKE N'Datei existiert bereits%'` statt des vollen Textes, weil der Text „–“ und „ü“ enthält und die `.psd1` ASCII sein muss.
  - Firma `AKE` für Teil A wie `Pruefung/WareneingangCsv` (`nachlauf.psd1:163`); Schema `beide` wie `SQL/112` (`nachlauf.psd1:73-74`).
  - Phase des Info-Eintrags: `Vorher`, solange Rückfrage 1 offen ist; ist E-1 vor der Freigabe erledigt, `Nachher`.
  - Kein Daten-/Einmal-Skript (Rückfrage 4, Empfehlung „nicht automatisch“). Falls Gerald „automatisch umstellen“ wählt: Typ `Daten`,
    `Trockenlauf`, genau ein `DECLARE @Trockenlauf BIT = 1;`, Zeile `TREFFER: n`, Wiederholungssperre gibt im Trockenlauf `TREFFER: 0` aus.
- **Publish-Befehle** (im Worktree):
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```

**Abgleich mit dem echten Diff (Dev-Lauf 2026-11-17, Commits `32338503`, `19ce63ef`; QA bestätigt noch):**
- Version **1.99.33** (beide `AppVersion.cs`), **SQL/156** `156_AddSagePurchaseOrderLineAbNummerKommission.sql`, MigrationId
  `20261117102804_AddSagePurchaseOrderLineAbNummerKommission`, Spalten `AbNummer` NVARCHAR(250), `Kommission` NVARCHAR(200) (P-2a offen,
  Längen aus dem SData-Schema). FreshInstall an beiden Stellen; AgentJobs nicht betroffen. TS-**156** (nicht 157).
- Manifest wie oben mit `Id='SQL/156'` (Schema-Block nach SQL/153, `SchemaReihenfolge` im Drift-Test ergänzt), `Pruefung/WareneingangCsvKollision`
  (AKE, Nachher, `Status = 3 AND Fehler LIKE N''Datei existiert bereits%''`, `Braucht SQL/146`), Info `v1.99.33-WareneingangCsvSammellieferschein`
  (AKE, **Vorher**, E-1 offen). Kein Daten-/Einmal-Skript.
- Web und Dienst: ja (wie oben). Keine neue Einstellung.

### Doku-Pflichten des Dev-Laufs

- `docs/anwender/05-wareneingang-zu-bestellung.md`: Kopf (Z. 47) um AB-Nummer/Kommission; Barcode (Z. 109-111: „nur an **einem** Wareneingang“ →
  Sammellieferschein-Regel, Dateiname `<Barcode>_n.csv`); CSV-Zeile (Z. 124: „Datei existiert bereits“ nur noch Altzeilen); Stolperfalle
  Z. 160-161 (Gleichzeitig-Buchen ergibt jetzt `_1`).
- `docs/anwender/06-offene-bestellungen-rahmenabrufe.md`: Spalten AB-Nummer/Kommission (Abschnitt Spaltenfilter/Zahnrad, Z. 57-58).
- Hilfe `Views/Help/Index.cshtml`: Wareneingang-CSV (Z. 128-135, Dateiname), Offene Bestellungen (Z. 72-80); Anwender-Changelog.
- `README.md` Abschnitt Wareneingang-CSV (Dateiname).
- Brain: [[fallstricke]] §32 ergänzen („Dateiname aus der DB, nicht aus dem Ordner – Warum: enaio holt ab“, „enaio liest den Barcode aus
  Spalte 1“), §41/§42 bei Variante B („RealTimeData-Kopf `KHKEKBelege_USER_*`, `where` nur auf BelID“); Changelog, [[feature-map]],
  [[testszenarien-index]]; bei Variante B ggf. ADR-Nachtrag zur RealTimeData-Freigabe im Dienst (kein neuer ADR nötig, wenn DPS-Freigabe
  dieselbe Regel trägt wie ADR 0029).

### Prüfabfragen und Prüfschritte (vor dem Dev-Start; nur lesend, nur GET, nichts an Sage schreiben)

**E-1 – enaio (Rückfrage 1, durch Gerald mit dem enaio-Betreuer).** Auf der Test-Freigabe zwei Dateien ablegen, Inhalt im TS-153-Format mit
gleichem Barcode `B` in Spalte 1 und zwei verschiedenen Bestellnummern in Spalte 4: `B.csv`, danach `B_1.csv`. Erwartet: enaio ordnet beide
demselben Dokument (Lieferschein) zu und nimmt die zweite Bestellung hinzu. Zusätzlich fragen: Liest enaio den Barcode aus Spalte 1 oder aus
dem Dateinamen? (Sonderfall `ABC_1_1.csv`, A2.)

**P-1a – SData RealTimeData, Kopf über BelID-Block (Variante B; AKE-Test, Chrome nach Anmeldung).** Zwei BelIDs mit Werten aus P-2c:
```
https://sagetest01.ake.at:5493/sdata/ol/RealTimeData/ake_TEST2026;1/dtsEKBelegMst.Sage.Wawi?where=BelID eq <BelID1> or BelID eq <BelID2>&select=BelID,KHKEKBelege_USER_AB,KHKEKBelege_USER_Kom&orderBy=BelID&format=json
```
Erwartet: genau eine Zeile je BelID, Werte = P-2c. Zusätzlich eine BelID **ohne** Werte: Kommt der Schlüssel mit `null`/`""` oder fehlt er? (Davon
hängt die Unterscheidung „Feld fehlt“ / „leer“ in B4 ab.) URL-Aufbau wie im Client-Test belegt
(`SageSdataReadClientTests.cs:47`).

**P-1b – derselbe Aufruf mit `select=BelID,USER_AB,USER_Kom`** (ohne Präfix): Erwartet laut [[fallstricke]] §41: HTTP 200 ohne die Felder.
Ergebnis festhalten – es belegt den Test zu AK 16.

**P-1c – nur Variante A, nach der DPS-Konfiguration:**
```
https://sagetest01.ake.at:5493/sdata/ol/CommonWawiServicesResources/ake_TEST2026;1/EinkaufPositionenInfo?where=BelegnummerFormatiert eq '<Bestellung aus P-2c>'&select=BelId,BelPosId,BelegProperty1,BelegProperty2&orderBy=BelPosId&format=json
```
Erwartet: `BelegProperty1` = AB-Nummer, `BelegProperty2` = Kommission an jeder Position; auch **ohne** `include=SpecificUserfields` (der Abgleich
liest heute ohne `include`, `EinkaufBestellungenSyncService.cs:100-101`).

**P-2 – Sage-SQL (nur lesend), je Standort AKE (Test und Prod) und IDEAL.** Spaltennamen `Belegkennzeichen`/`Belegdatum` in `KHKEKBelege` sind
nicht im WMS belegt (P-2 des Backlogs nutzt `Belegdatum`); scheitert eine Spalte, die Fehlermeldung melden. Gibt es eine Spalte `Mandant`, je
Mandant gruppieren.
```sql
-- P-2a: Typ und Länge (bestimmt die WMS-Spaltenlänge, B2)
SELECT TABLE_SCHEMA, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'KHKEKBelege' AND COLUMN_NAME IN ('USER_AB', 'USER_Kom');

-- P-2b: Füllgrad der letzten 12 Monate je Belegart
SELECT Belegkennzeichen, COUNT(*) AS Belege,
       SUM(CASE WHEN NULLIF(LTRIM(RTRIM(CAST(USER_AB  AS nvarchar(400)))), N'') IS NOT NULL THEN 1 ELSE 0 END) AS MitAB,
       SUM(CASE WHEN NULLIF(LTRIM(RTRIM(CAST(USER_Kom AS nvarchar(400)))), N'') IS NOT NULL THEN 1 ELSE 0 END) AS MitKom,
       MAX(LEN(CAST(USER_AB  AS nvarchar(400)))) AS MaxLaengeAB,
       MAX(LEN(CAST(USER_Kom AS nvarchar(400)))) AS MaxLaengeKom
FROM dbo.KHKEKBelege
WHERE Belegkennzeichen IN ('EBB', 'EBZ') AND Belegdatum >= DATEADD(MONTH, -12, GETDATE())
GROUP BY Belegkennzeichen;

-- P-2c: Beispiele (BelIDs für P-1a/157.11)
SELECT TOP 20 BelID, Belegkennzeichen, Belegnummer, Belegdatum, USER_AB, USER_Kom
FROM dbo.KHKEKBelege
WHERE Belegkennzeichen IN ('EBB', 'EBZ') AND (USER_AB IS NOT NULL OR USER_Kom IS NOT NULL)
ORDER BY Belegdatum DESC;
```

**P-3 – WMS-DB AKE (Test und Prod): Altzeilen „Datei existiert bereits“ (Rückfrage 4).**
```sql
SELECT Id, SageWeVorgang, Barcode, Status, Fehler, Versuche, CreatedAt, ModifiedAt
FROM dbo.WareneingangCsvExporte
WHERE Fehler LIKE N'Datei existiert bereits%'
ORDER BY Id;
```

**P-4 – WMS-DB AKE: Barcodes, die schon heute an mehreren Vorgängen hängen** (Gleichzeitig-Buchen, A3; beeinflusst die Nummern nach dem
Deploy).
```sql
SELECT UPPER(Barcode) AS Barcode, COUNT(DISTINCT SageWeVorgang) AS Vorgaenge, COUNT(DISTINCT LieferantKto) AS Lieferanten
FROM dbo.PurchaseOrderReceipts
WHERE Barcode IS NOT NULL AND SageWeVorgang IS NOT NULL
GROUP BY UPPER(Barcode)
HAVING COUNT(DISTINCT SageWeVorgang) > 1;
```

**P-5 – WMS-DB AKE: Aufwand Variante B** (Anzahl RealTimeData-Aufrufe je Abgleich, Blöcke zu 50).
```sql
SELECT COUNT(DISTINCT BelId) AS Koepfe, CEILING(COUNT(DISTINCT BelId) / 50.0) AS AufrufeJeLauf
FROM dbo.SagePurchaseOrderLines;
```

Bis P-2 vorliegt, sind Spaltenlänge (B2) und Format (B3) **ungeprüft**; bis P-1a/P-1c vorliegen, sind die Feldnamen der gewählten Variante
**ungeprüft** (P-1 vom 17.11. belegt nur die Namen am Testbeleg 2027-168975 mit leeren Werten).

## Freigabe-Nachtrag (Gerald 2026-11-17 11:23 – geht dem Rumpf vor)

- **Teil B Datenweg = Variante B:** Der Dienst liest die Kopffelder über `RealTimeData/{Dataset}/dtsEKBelegMst.Sage.Wawi`
  (`KHKEKBelege_USER_AB`, `KHKEKBelege_USER_Kom`; Auswahl der Belege nur über normale Felder wie `BelID`/`Belegkennzeichen`, **kein** `where`
  auf die USER-Aliasse – SQL-Fehler „Ungültiger Spaltenname“, Befund in [[2026-11-17-wareneingang-bestellkopf-ab-nummer-kommission]]).
  Teil B wird **jetzt mitgebaut**; Stellen im Rumpf „Teil B erst nach DPS-Antwort“ / „B nur mit DPS-Freigabe“ sind damit überholt.
  Variante A (DPS-Properties) bleibt späterer Umstieg.
- **Anzeige:** AB-Nummer und Kommission als Spalten in Lager → Offene Bestellungen und Einkauf → Offene Bestellungen (sichtbar, ausblendbar).
  In „Wareneingang zu Bestellung“ (Einbuchung) **nur im Kopf**, keine zusätzlichen Spalten in der Positionstabelle.
- **Teil A:** bauen und auf DEV testen; auf Prod erst nach Bestätigung durch den enaio-Betreuer (E-1).
- Antworten 3, 4, 6, 7 wie empfohlen.

## Offene Rueckfragen

1. **enaio verarbeitet `<Barcode>_n.csv`?** Nimmt enaio `<Barcode>_1.csv` als weitere Bestellung zum selben Dokument, und liest es den Barcode aus
   Spalte 1 (nicht aus dem Dateinamen)? *Empfehlung:* vor der Freigabe mit dem enaio-Betreuer per E-1 bestätigen. Teil A darf gebaut und auf DEV
   getestet werden; auf Prod erst nach Bestätigung (Info-Eintrag Phase Vorher). Erwartet enaio einen anderen Mechanismus (z. B. eine CSV mit
   mehreren Zeilen), ist A2 neu zu spezifizieren – siehe auch [[2026-11-16-enaio-rest-statt-csv-wareneingang]].
2. **Datenweg Teil B: Variante A (DPS belegt `BelegProperty1/2`) oder Variante B (RealTimeData `dtsEKBelegMst.Sage.Wawi` im Dienst)?**
   *Empfehlung:* Gerald fragt DPS beides in einer Anfrage (Belegung der Properties **und** Freigabe RealTimeData im Dienst, die für die
   FA-Struktur ohnehin offen ist). A bevorzugt; B nur mit DPS-Freigabe. Bis zur Antwort wird Teil B nicht gebaut; Teil A ist davon unabhängig.
3. **Sammellieferschein ohne Lieferschein-Nr.?** *Empfehlung:* Der zweite Einsatz ist nur mit nicht leerer, gleicher Lieferschein-Nr. erlaubt
   (beide Seiten). Ohne Nummer fehlt der Beleg, dass es derselbe Lieferschein ist, und der Tippfehler-Schutz wäre bei leerer Nr. weg. Ist
   `WareneingangLieferscheinPflicht` an (`Receive.cs:94-95`), ist die Nr. ohnehin immer da.
4. **Altzeilen mit „Datei existiert bereits“?** *Empfehlung:* nicht automatisch umstellen. Eine Kollision kann eine echte Doppelung sein (z. B.
   Umbenennen gelungen, Speichern gescheitert, A3) – ein automatisches `_1` lieferte dann denselben Inhalt doppelt an enaio. Sichtbar machen
   per `Pruefung/WareneingangCsvKollision` (anzeigen), Lager Keyuser prüft die Freigabe und klickt „CSV erneut schreiben“; der Dienst nimmt dann
   die nächste freie Nummer. Laut Vorgänger-Spec ist der Export auf Prod noch nicht produktiv (dort Freigabe-Antwort 2) – P-3 zeigt vermutlich 0.
5. **Spalten AB-Nummer/Kommission standardmäßig sichtbar oder ausgeblendet?** *Empfehlung:* sichtbar (`DefaultHidden: false`), ausblendbar über
   das Zahnrad – Gerald will die Werte sehen; wer sie nicht braucht, blendet sie einmal aus (gilt dann für Lager und Einkauf gemeinsam, gleicher
   View-Key).
6. **Standort Teil B?** Code und Schema gelten für beide Standorte; befüllt wird nur, wo `Sync:EinkaufBestellungenEnabled` läuft – laut Manifest
   heute nur AKE („IDEAL bleibt aus bis SData-Endpunkt“, `nachlauf.psd1:323-324`), ebenso `docs/anwender/05-…md:139-141`. *Empfehlung:* `standort: AKE`,
   P-2 an IDEAL trotzdem mitlaufen lassen (nur lesend), damit die Spaltenlänge für beide passt.
7. **Wann erscheint der Sammellieferschein-Hinweis?** *Empfehlung:* nach dem Buchen in der Erfolgsmeldung (A1) – kein neuer Endpunkt, kein
   JavaScript. Ein Hinweis schon beim Scannen bräuchte eine zusätzliche Abfrage je Eingabe; der Fehlerfall (anderer Lieferant/LS) wird ohnehin vor
   dem Buchen gemeldet.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. enaio verarbeitet `<Barcode>_n.csv` (E-1), Barcode aus Spalte 1? → **Gerald 2026-11-17 11:23 „freigegeben“ (Empfehlung): Teil A bauen und auf DEV testen; Prod erst nach Bestätigung durch den enaio-Betreuer.**
2. Datenweg Teil B: Variante A (DPS BelegProperty1/2) oder B (RealTimeData im Dienst, DPS-Freigabe)? → **Gerald 2026-11-17 11:23: Variante B – „wir greifen vorerst die RealTimeData ab“ (`dtsEKBelegMst.Sage.Wawi`, Felder `KHKEKBelege_USER_AB`/`KHKEKBelege_USER_Kom`, kein `where` auf USER-Aliasse). Teil B wird mitgebaut, nicht auf DPS gewartet. Umstieg auf Variante A später möglich.**
3. Sammellieferschein nur mit nicht leerer, gleicher Lieferschein-Nr.? → **Gerald 2026-11-17 11:23 (Empfehlung): ja.**
4. Altzeilen „Datei existiert bereits“: nur anzeigen + von Hand erneut schreiben, oder automatisch umstellen? → **Gerald 2026-11-17 11:23 (Empfehlung): nur anzeigen (Pruefung/WareneingangCsvKollision) + von Hand erneut schreiben.**
5. Spalten AB-Nummer/Kommission standardmäßig sichtbar oder ausgeblendet? → **Gerald 2026-11-17 11:23: in der Tabelle der offenen Bestellungen (Lager und Einkauf) als Spalten, sichtbar und ausblendbar; bei der Einbuchung (Wareneingang zu Bestellung) nur im Kopf anzeigen, keine Spalten in der Positionstabelle.**
6. Standort Teil B nur AKE (P-2 trotzdem auch an IDEAL)? → **Gerald 2026-11-17 11:23 (Empfehlung): ja.**
7. Hinweis nach dem Buchen (Erfolgsmeldung) oder schon beim Scannen? → **Gerald 2026-11-17 11:23 (Empfehlung): nach dem Buchen in der Erfolgsmeldung.**

## QA-Nachweis (qa-agent, 2026-11-17)

Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`. Geprüfter Bereich `5960b6a5..19ce63ef`
(`32338503`, `19ce63ef`) plus QA-Fix `ed919976` (HEAD). Version **1.99.33**, SQL/156, TS-156.

### 1. Build und Tests
- `dotnet build IdealAkeWms.slnx`: 0 Fehler (28 vorbestehende Warnungen, keine in geänderten Dateien).
- `dotnet test` (vor und nach dem QA-Fix gleich): **IdealAkeWms.Tests 3117 bestanden / 1 übersprungen / 0 Fehler (3118)**;
  **IDEALAKEWMSService.Tests 702 bestanden / 0 / 0 (702)**. `NachlaufManifestDriftTests`, `FreshInstallIndexSpaltenTests` darin grün (AK 21).

### 2. Code-Review (selbst, in diesem Lauf)
Geprüft: kompletter Diff (Receive-Regel, `SammellieferscheinGrund`, `WareneingangCsvService.SchreibenAsync/BelegteDateienAsync/Dateiname`,
`EinkaufBestellungenSyncService.KoepfeAsync`, Modell/DbContext, Migration, SQL/156, FreshInstall beide Stellen, Manifest, Views Index/Receive,
ColumnMap/ColumnDefinitions, Tests, Doku/Hilfe/README/Changelog) sowie Umgebung: `BarcodeVergebenAsync` (erster Wareneingang, `ToUpper`),
`BuildReceiveAsync` (Zeilen nie leer), Schleife `RunOnceAsync` (Speichern je Zeile → A3 trägt), `SageSdataReadClient.GetAsync` (immer
RealTimeData, `orderBy` Pflicht), `SageWareneingaenge.cshtml`. Defektklassen: kein Wert in `<script>` (Spaltenkatalog statisch), kein `hidden`+`d-*`,
kein stilles `?? 0` (Kopfwerte `null` → „–“ sichtbar; fehlendes Feld/Lesefehler/zu lang/ohne Treffer werden gemeldet), keine Parallel-Arrays,
Regel serverseitig im Controller, Groß/Klein-Regel SQL `ToUpper` + C# `OrdinalIgnoreCase` gleichsinnig.

| # | Befund | Behandlung |
|---|---|---|
| F1 | `Views/PurchaseOrders/SageWareneingaenge.cshtml:145`: Hinweis am Knopf „CSV erneut schreiben“ nannte nur `<Barcode>.csv` – seit v1.99.33 kann es `<Barcode>_n.csv` sein. | **Behoben** `ed919976` (Text nennt `_n.csv` und „nächste freie Nummer“). Build + alle Tests danach grün. |
| H1 | Kopf-Lesen über RealTimeData läuft im Dienst ohne eigenen Schalter (anders als `FaStrukturQuelleSwitch.RealTimeDataErlaubt`). Entscheidung Variante B (Gerald 11:23, „kein neuer Schalter“). Untersagt DPS RealTimeData im Dienst, braucht es eine Code-Änderung. | Hinweis, kein Defekt. |
| H2 | AK 11 (Erneut schreiben nach Pfadfehler) hat keinen eigenen Unit-Test; abgedeckt über die Regel „`Datei` nur bei Erfolg“ (Start-Zählung) und den AK-10-Test. | Hinweis, akzeptiert. |
| H3 | Ist der **erste** Wareneingang eines Barcodes nur chargen-/nachweispflichtig (`NurInSage`, keine Ausgangszeile), sagt die Erfolgsmeldung des zweiten „_1, _2 …“, die Datei heißt aber `<Barcode>.csv` (Start = 0). Kosmetisch. | Hinweis, nicht geändert. |

### 3. Nachweisart je AK
| AK | Nachweis |
|---|---|
| 1–4 Sammellieferschein-Regel | automatisiert (`PurchaseOrdersReceiveTests` AK1, AK2, AK3/AK4-Theory; Bestandstest `Barcode_Duplikat_…_AK4` Z. 318 unverändert grün). Echte Formular-Bindung/Maske: **Manual-UAT** (auf DEV nicht möglich, s. 6c) |
| 5–10, 12 Dateinamen | automatisiert (`WareneingangCsvServiceTests`) **und** DEV (echter SQL Server, echte Dateien, Sonde) |
| 11 erneut schreiben nach Pfadfehler | Logik (H2) + AK-10-Test; Manual-UAT |
| 13 Kopf Maske | automatisiert (AK13) + DEV-Browser |
| 14 Listen/Filter | automatisiert (`PurchaseOrdersControllerTests` AK14/15) + DEV-Browser (Lager + Einkauf); Zahnrad-Persistenz **Manual-UAT** |
| 15 Default sichtbar | automatisiert (Katalog `DefaultHidden=false`) + DEV-Browser |
| 16 Feld fehlt | automatisiert (AK16) + DEV-Live-Probe P-1b |
| 17 Lesefehler | automatisiert (AK17); auf DEV nicht ausgelöst |
| 18 zu lang | automatisiert (AK18) |
| 19 nicht in enaio-CSV | DEV (Dateien 9 Felder) + Diff (`CsvInhalt` unverändert) |
| 20 Counts | automatisiert (AK20) + DEV-Live-Lauf |
| 21 Build/Tests | grün |
| 22 IDEAL | DEV IDEAL (Schema, Browser) |

### 4. Geänderte Bestandstests
- `WareneingangCsvServiceTests.DateiExistiert_NieUeberschrieben_Fehler_Warnung_KeineAutoWiederholung_AK11` → `…_NaechsteNummer_KeinFehler_AK8`:
  **Verhalten absichtlich geändert (AK 8)**; „nie automatisch“ für Altzeilen wandert in den neuen AK-10-Test.
- `EinkaufBestellungenSyncTests` (Lauf mit zwei Abfragen): `Invocations.HaveCount(2)` → nur `QueryAllAsync` zählen: **Verhalten absichtlich
  geändert (Teil B, zusätzlicher GET)**; Aussage „zwei Positions-Abfragen, kein or“ bleibt. Standard-Setup `Koepfe(client)` ergänzt.
- `NachlaufManifestDriftTests.SchemaReihenfolge` + `"SQL/156"`: Pflichtnachzug für das neue Schema-Skript.
- Kein „Sonstiges“.

### 5. Checkliste
Migration + `SQL/156` (Guard „falsche DB“, `COL_LENGTH` je Spalte im eigenen Batch, History separat, `SET NOEXEC OFF`) ✓; FreshInstall Schema +
MigrationId (mit Spalten-Bedingung) ✓; AgentJobs nicht betroffen ✓; Audit: `WareneingangCsvExporte` wie bisher, Cache ohne Audit (Vollersatz) ✓;
Version 1.99.33 in beiden `AppVersion.cs` + Anwender-Changelog ✓; README, Hilfe, Anwenderdoku 05/06 ✓; TS-156 in `docs/TESTSZENARIEN.md` + Hinweise
TS-129.5/129.14 ✓; [[testszenarien-index]] Zeile 156 ✓; Manifest `SQL/156`, `Pruefung/WareneingangCsvKollision`, `v1.99.33-WareneingangCsvSammellieferschein`
(ASCII, Phase Vorher) ✓. **5a ADR 0020:** keine neue oder geänderte Einstellung.

### 6. DEV
- a. `standort: AKE`; IDEAL zusätzlich deployt (Auftrag, AK 22).
- b. Zielordner `D:\Dev\{AKE,IDEAL}\{Web,Service}` vor **beiden** Deploys geleert (Befund Q-3; behalten nur `appsettings.DEV.json*`, `logs`,
  `App_Data`, `deploy-info.txt`). `dev-deploy.ps1 -Quelle <Worktree>` (elevated) zweimal – Stand `19ce63ef`, dann final **`ed919976`**: AKE und IDEAL
  je Exit 0, HTTP 200, **Fußzeile v1.99.33**, DEV-Absicherung gelaufen, Dienste Running. EF-Migrate legte die Spalten an; `SQL/156` danach von Hand
  auf beiden DEV-DBs: „existiert bereits“ (idempotent), `COL_LENGTH` 500/400, MigrationId eingetragen.
- c. TS-156:

| Nr | Erledigt | Ergebnis | Status |
|---|---|---|---|
| 156.1–156.5 | – | Buchen mit Barcode verlangt `SageWareneingangBelegAktiv = an`; das Einschalten auf DEV wurde verweigert (Sage-WE-Übergabe bleibt aus). Regel per Unit-Test belegt. | NICHT MÖGLICH |
| 156.6 | Receipts X (`QA157B`, 2027-168561) und Y (`qa157b`, 2027-168594, beide K31705) per SQL als übergeben angelegt; CSV-Lauf per Sonde (`WareneingangCsvService.RunOnceAsync` gegen `AKE_WMS_DEV`, Ordner `D:\Dev\csv-ausgang\ake-qa157`) | `QA157B.csv`, `qa157b_1.csv`; Feld 1 `QA157B` bzw. `qa157b`; `Datei` je Zeile = tatsächlicher Pfad; BOM, CRLF | OK |
| 156.7 | `QA157B.csv` gelöscht, dritter Vorgang (2027-168779) | `QA157B_2.csv`, nicht `QA157B.csv` | OK |
| 156.8 | `QA157B_3.csv` mit Inhalt „fremd“ angelegt, vierter Vorgang (2027-168731) | `QA157B_4.csv`; `_3` unverändert; Status 2, Versuche 0, Protokoll `geschrieben=1, fehler=0` | OK |
| 156.9 | Zeile 7 per SQL auf Status 3, KollisionText, `Datei = NULL`, `ModifiedAt` −1 h, Datei entfernt; Sonde | kein Versuch, kein Protokoll-Eintrag; Browser Lager → Sage-Wareneingänge „CSV erneut schreiben“ → Status 1 (ModifiedBy Gerald Weichbold); Sonde → `QA157B_4.csv`, Status 2 | OK |
| 156.10 | Vorgang mit neuem Barcode `QA157C` (2027-168647, K30072) | `QA157C.csv`, 9 Felder, BOM, CRLF, Erfasser `gerald.weichbold` | OK |
| 156.11 | Abgleich einmal echt per Sonde (nur GET, Cache 1167 → 1160 Zeilen wie ein regulärer Lauf); Browser *Wareneingang zu Bestellung* | 2027-168647: AB-Nummer `499366`, Kommission „–“; 2027-168594: AB „–“, Kommission „Pos. 7 für Hausdamen“; Positionstabelle ohne neue Spalten | OK |
| 156.12 | Lager → Offene Bestellungen `colf_ab-nummer=49936` (Überfällige an); Einkauf → Offene Bestellungen `colf_kommission=Hausdamen` | 23 Zeilen = SQL `AbNummer LIKE '%49936%'` 23, je Beleg derselbe Wert; Einkauf 2 Zeilen (2027-168594); Spalten nach „Bestellung“ sichtbar. Zahnrad nicht geklickt | OK (Zahnrad offen) |
| 156.13 | Sonde `RunAsync(dryRun:true)` gegen AKE-Test-SData (vorhandene DEV-Konfiguration), danach echter Lauf | „Kopfdaten: Quelle SData (RealTimeData) dtsEKBelegMst.Sage.Wawi, 445 Zeile(n) zu 445 Beleg(en)“; Counts `ab_nummer=139, kommission=166, kopf_ohne_treffer=0, kopf_zu_lang=0`; keine Warnung „liefert … nicht“; DryRun ließ den Cache unverändert (Prüfsumme gleich) | OK |
| P-1a/P-1b live | Sonde GET `dtsEKBelegMst.Sage.Wawi`, BelIDs 361915, 361998, 362183, 362351, 362493 | Mit Präfix: HTTP 200, 5 Zeilen, **Schlüssel immer vorhanden**, leer = `null` (362183 `KHKEKBelege_USER_AB="499366"`, `…_USER_Kom=null`; 361998 Kom „Pos. 7 für Hausdamen“) → „Feld fehlt“ und „leer“ unterscheidbar. Ohne Präfix (`USER_AB,USER_Kom`): HTTP 200, nur `BelID` – still weggelassen (fallstricke §41, AK 16). Max. Länge live 43 / 99 | OK |
| 156.14 | – | Lesefehler hieße `SData:BaseUrl` auf DEV ändern; per Unit-Test AK17 belegt | NICHT MÖGLICH (bewusst) |
| 156.15 | Dateien aus 156.6–156.10 | je 9 Felder, keine AB/Kommission | OK |
| 156.16 | IDEAL-DEV | `COL_LENGTH` 500/400, MigrationId da; `localhost:8082/PurchaseOrders` zeigt Spalten AB-Nummer/Kommission (leer, Abgleich dort aus) | OK |

**DEV-Aufräumen:** 5 Test-Receipts (`CreatedBy = QA-157`), 5 Ausgangszeilen (Id 4–8), 6 Bewegungen (Menge 0, Note `QA-157`; eine aus einem
abgebrochenen ersten Insert) gelöscht, Ordner `D:\Dev\csv-ausgang\ake-qa157` entfernt; danach 17 Receipts (max Id 20), Ausgangszeile 3 unverändert,
0 `QA-157`-Bewegungen. **Keine Einstellung geändert** (geprüft: `SageWareneingangBelegAktiv`, `WareneingangCsvAktiv`, `Sync:EinkaufBestellungenEnabled`,
`SageLagerbuchungAktiv` = false). Bleibend: Cache `SagePurchaseOrderLines` auf AKE-DEV durch den einen echten Abgleich auf Sage-Test-Stand
(reiner Spiegel, gewollt); Protokoll-Einträge der Sonden in `SyncLogs`. Sonde (Wegwerf):
`C:\Users\GERALD~1.WEI\AppData\Local\Temp\claude\8eae370df6a92eae\ec0f5dc9-6d40-450e-bc07-1e14a1b12b59\scratchpad\qa157sonde\`.

### 7. Publish
`<Worktree>\publish\IDEALAKEWMSWeb` und `\IDEALAKEWMSWebService` vorher geleert und neu publiziert (Exit 0, `IdealAkeWms.dll` ProductVersion
`1.0.0+ed919976…`), `publish\SQL` (mit `156_…sql`) und `publish\deploy` (Manifest mit `SQL/156`) neu; `publish\publish-info.txt`: Version 1.99.33,
Commit `ed919976bbc1b24809b56e880b3fbe52f954f9b9`, Branch `feature/2026-08-07-ideal-teile-1-5`, Erstellt 2026-11-17 11:57, Komponenten
IDEALAKEWMSWeb + IDEALAKEWMSWebService. `publish\publish.zip` (10:19) ist alt – nicht verwenden.

**Ergebnis: Testbereit.**

## Manuelle Testcheckliste (Mensch, offen)

1. **E-1 enaio** (vor Prod AKE): `B.csv` + `B_1.csv` mit gleichem Barcode in Spalte 1 → gleiches Dokument, zweite Bestellung ergänzt; liest enaio Spalte 1 oder den Dateinamen?
2. TS-156.1–156.5 auf dem Testsystem mit Sage-WE an: zwei Bestellungen desselben Lieferanten mit `L1`/`B` und `l1`/`b` → Erfolgsmeldung „Sammellieferschein …“; Negativ anderer Lieferant, `L2`, LS leer (beide Richtungen).
3. TS-156.6–156.10 über den echten Dienst-Takt auf der Test-Freigabe (UNC, Dienstkonto): `_1`, nach Abholung `_2`, fremde Datei → nächste Nummer.
4. TS-156.9: Update-Skript-Prüfung `Pruefung/WareneingangCsvKollision` zählt die Altzeile; „CSV erneut schreiben“ → nächste freie Nummer.
5. TS-156.12 Zahnrad: AB-Nummer ausblenden, neu laden → bleibt ausgeblendet (Lager und Einkauf gemeinsam).
6. TS-156.14 Lesefehler Kopf (optional, Testsystem): Warnung „Kopfdaten … nicht lesbar – Werte vom letzten Lauf“, Positionen aktualisiert.
7. Prüfabfragen P-3 (Altzeilen „Datei existiert bereits“) und P-4 (Barcodes an mehreren Vorgängen) auf Prod vor dem Deploy.
8. DPS: RealTimeData-Lesen `dtsEKBelegMst` im Dienst ist auf Prod ab v1.99.33 aktiv, sobald `Sync:EinkaufBestellungenEnabled` läuft (H1) – bei Bedarf abstimmen.
9. Testsystem-Durchlauf komplett, danach Merge (Schranke 2).
