---
type: spec
title: "Wareneingang zu Bestellung: Barcode-Feld und CSV-Übergabe auf einen UNC-Pfad"
slug: 2026-11-12-wareneingang-barcode-lieferschein-csv-export-spec
status: Entwurf
created: 2026-11-12
updated: 2026-11-12
source_backlog: "[[2026-11-12-wareneingang-barcode-lieferschein-csv-export]]"
depends_on: "[[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]]"
schwester_spec: "[[2026-11-12-offene-bestellungen-mahnmail-lieferverzug]] (Backlog; Spec entsteht parallel)"
version: "1.99.9 (Vorschlag; Kollision mit Kürzel-Nachtrag und Mahnmail, Rückfrage 10)"
sql_nummern: "144 (vorläufig; nächste freie im Bündel-Worktree, SQL/143 ist die letzte; Dev-Lauf prüft neu, Rückfrage 10)"
testszenario: "TS-128 (Vorschlag; letzte vergebene Nummer im Bündel TS-127)"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: AKE
affected_code:
  - "IdealAkeWms/Models/PurchaseOrderReceipt.cs:49-61 (neue Spalten Barcode, LieferantName neben Lieferschein)"
  - "NEU IdealAkeWms/Models/WareneingangCsvExport.cs (Ausgangstabelle, AuditableEntity, Enum WareneingangCsvStatus)"
  - "IdealAkeWms/Data/ApplicationDbContext.cs:58,1414-1438 (DbSet + Konfiguration Receipt-Spalten, neue Tabelle)"
  - "IdealAkeWms/Controllers/PurchaseOrdersController.Receive.cs:45 (Post-Parameter barcode), :74-79 (Prüfung analog Lieferschein), :114-123 (Speichern am Receipt), :140-157 (BuildReceiveAsync: Schalter)"
  - "IdealAkeWms/Models/ViewModels/PurchaseOrderReceiveViewModel.cs:14-24 (Barcode, BarcodeAktiv, BarcodeMaxLaenge)"
  - "IdealAkeWms/Views/PurchaseOrders/Receive.cshtml:60-66 (Feld neben Lieferschein-Nr., Kamera-Knopf), :185-201 (Scripts: html5-qrcode + barcode-scanner.js wie ProductionOrders/Index.cshtml:275-276)"
  - "IdealAkeWms/wwwroot/js/barcode-scanner.js:71 (Fenstertitel), :415-430 (initTextInputScanner: 'raw' wird heute als 'article' ausgewertet)"
  - "IdealAkeWms.Tests/Views/HardwareScannerTests.cs:122-138 (Wächter-Liste der Kamera-Knöpfe um Receive erweitern)"
  - "IdealAkeWms/Controllers/PurchaseOrdersController.SageWe.cs:33-80 (Spalten Barcode/CSV, Hinweis), neue Action 'CSV erneut schreiben' (Muster :82-98)"
  - "IdealAkeWms/Views/PurchaseOrders/SageWareneingaenge.cshtml:69-84 (zwei Spalten mit data-col-key, Hinweis)"
  - "IdealAkeWms/Data/Repositories/PurchaseOrderReceiptRepository.cs:60-71,105-107 (+ IPurchaseOrderReceiptRepository: Barcode-Duplikat, Export-Zeilen je Vorgang)"
  - "IdealAkeWms/Services/SageWareneingang.cs:53-56 (Muster AusHinweis für den CSV-Hinweis)"
  - "IdealAkeWms/Views/ServiceSettings/Index.cshtml:12-14 (Hinweis 'Sage-Wareneingang deaktiviert – CSV-Übergabe wartet')"
  - "IdealAkeWms/Models/ServiceSettingDefinitions.cs:33-120 (Meta), Basisliste ab :147 (zwei neue Keys)"
  - "IdealAkeWms/Services/Settings/SettingValidator.cs:68-69 (String heute ungeprüft; Pfad-Prüfung für WareneingangCsvPfad)"
  - "IdealAkeWms/Models/AppSettingKeys.cs:115-120 + AppSettingDefinitions.cs:85-91 (WareneingangBarcodeAktiv)"
  - "IdealAkeWms/Services/SyncLogger/SyncLogServices.cs:38,59,96 (neuer Lauf WareneingangCsv + DisplayName)"
  - "IDEALAKEWMSService/Workers/SageBookingWorker.cs:66-75 (eigener Schritt nach dem Sage-WE-Schritt)"
  - "NEU IDEALAKEWMSService/Services/WareneingangCsvService.cs (Ausgangstabelle füllen, CSV bauen, .tmp schreiben, umbenennen; ISyncLogger letzter Ctor-Parameter)"
  - "IDEALAKEWMSService/Program.cs:150 (DI-Registrierung neben SageWareneingangService)"
  - "SQL/144_AddWareneingangBarcodeCsv.sql (Nummer prüfen), SQL/00_FreshInstall.sql (zwei Stellen)"
  - "scripts/deploy/nachlauf.psd1:82-83 (Schema nach SQL/143), Pruefung-Block ab :85, Nachher-Block ab :193"
  - "docs/TESTSZENARIEN.md (TS-128), README.md (AppSettings + ServiceSettings), Views/Help/Index.cshtml + Changelog.cshtml, beide AppVersion.cs"
open_questions:
  - "1 Auftrag an dieser Stelle abgeschnitten ('Ausgangstabelle mit Status, atomares Schreiben (.tmp →'): Was fehlt noch? Vorschläge a-f zu Dateinamen-Kollision, Bereinigung, Encoding/Trennzeichen, Wiederholung bei Netzfehler, Dienstkonto-Rechte, Zeitlimit"
  - "2 Wer liest die CSV (enaio-Import, Zweck unbestätigt) und welches Format erwartet er? (Empfehlung: Gerald klärt mit der enaio-Betreuung; bis dahin Format laut 1c)"
  - "3 CSV-Spalten: Vorschlag 9 Felder, eine Datenzeile je Datei; BelID des Sage-WE-Belegs ist nicht auf allen Wegen vorhanden -> nur Sage-Belegnummer? Kreditorname als Snapshot am Wareneingang? (Empfehlung ja/ja)"
  - "4 Barcode Pflichtfeld oder optional? (Empfehlung optional, kein Pflicht-Schalter in diesem Schritt)"
  - "5 Erlaubte Zeichen und Länge des Barcodes (gültiger Dateiname)? Muster-Aufkleber nötig (Empfehlung A-Z a-z 0-9 - _, 1-50 Zeichen, keine Umformung)"
  - "6 Duplikate: Barcode schon an einem anderen Wareneingang bzw. Datei existiert? (Empfehlung Sperre beim Buchen + nie überschreiben)"
  - "7 Sage-WE beim Buchen aus (bzw. nur NurInSage-Zeilen): Diese Wareneingänge werden nie 'Übergeben', die CSV käme also nie. Feld dann sperren? Widerspruch zum Hinweis 'wartet' (Empfehlung Feld gesperrt mit Hinweis)"
  - "8 Ein Schalter oder zwei: Feld in der Maske (AppSetting) getrennt vom Dienst-Schalter der CSV-Übergabe? (Empfehlung zwei)"
  - "9 Hardware-Scan ohne angetipptes Feld geht nach dem Öffnen einer Bestellung ins Barcode-Feld (einziges registriertes Scan-Ziel). Akzeptiert? (Empfehlung ja)"
  - "10 Version und SQL-Nummer: Kollision mit dem System-Kürzel-Nachtrag (Teil 2, braucht vermutlich eine Spalte) und der Mahnmail-Spec. Reihenfolge? (Empfehlung Vergabe in Bau-Reihenfolge durch den Dev-Lauf)"
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

# Wareneingang zu Bestellung: Barcode-Feld und CSV-Übergabe auf einen UNC-Pfad

Backlog [[2026-11-12-wareneingang-barcode-lieferschein-csv-export]]. Baut auf [[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]]
auf: F6 Lieferschein-Nr., Status `Uebergeben`, „Als übergeben markieren“ und Q-2 aus dem Freigabe-Nachtrag 2026-11-12 (E5).
Schwester: [[2026-11-12-offene-bestellungen-mahnmail-lieferverzug]] (Spec parallel).

> [!warning] Auftrag unvollständig
> Der technische Teil des Auftrags endet mitten im Satz: „Ausgangstabelle mit Status, atomares Schreiben (.tmp →“. Umgesetzt wird nur, was
> dasteht: eine Ausgangstabelle mit Status und atomares Schreiben (`<Barcode>.tmp`, dann Umbenennen auf `<Barcode>.csv`). Alles Weitere ist als
> Vorschlag markiert und hängt an **Rückfrage 1**.

## Ziel / Nutzen (das Warum)

Der Lieferschein des Lieferanten bekommt im Lager einen Barcode-Aufkleber. Das WMS erfasst diesen Barcode beim Wareneingang und legt nach der
erfolgreichen Übergabe an Sage eine Datei `<Barcode>.csv` mit den Indexdaten des Wareneingangs auf eine Netzfreigabe. Vermuteter Zweck
(unbestätigt, Rückfrage 2): Das DMS (enaio) ordnet den gescannten Lieferschein über den Barcode zu und übernimmt die Indexdaten aus der CSV. So
entfällt das Verschlagworten von Hand.

Die Datei entsteht erst nach der Sage-Übergabe (Entscheidung Gerald 2026-11-12). Dann steht die Sage-Belegnummer des Wareneingangs fest, und es
gibt keine CSV zu einem Wareneingang, der in Sage nie ankam.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- F1 Feld „Barcode (Lieferschein)“ im Kopf von „Wareneingang zu Bestellung“, neben der Lieferschein-Nr. (F6 der Vorgänger-Spec). Sichtbar nur bei
  eingeschaltetem AppSetting. Befüllbar per Hardware-Scanner, Kamera oder Tastatur. Gespeichert am Wareneingang.
- F2 Ausgangstabelle `WareneingangCsvExporte` mit Status, eine Zeile je Sage-WE-Vorgang (= ein Beleg = ein Lieferschein = eine Datei).
- F3 Dienst-Schritt „Wareneingang-CSV“: schreibt `<Barcode>.tmp` auf den konfigurierten UNC-Pfad und benennt die Datei dann auf `<Barcode>.csv` um.
  Er schreibt nur, wenn der Vorgang „an Sage übergeben“ ist (auch nach „Als übergeben markieren“), der eigene Dienst-Schalter an ist **und** der
  Sage-Wareneingang an ist.
- F4 Sichtbarkeit: Spalten „Barcode“ und „CSV“ in der Liste „Sage-Wareneingänge“, Hinweis „Sage-Wareneingang deaktiviert – CSV-Übergabe wartet“ in
  der Liste und in `/ServiceSettings`, „CSV erneut schreiben“ von Hand bei Fehler.

**Out-of-Scope**
- Ein Ersatzweg ohne Sage-Wareneingang. Der Vorschlag „CSV direkt nach der WE-Buchung“ ist verworfen (Entscheidung Gerald 2026-11-12).
- IDEAL: Dort ist der Sage-Wareneingang immer aus, weil es keinen SData-Endpunkt gibt (`ServiceSettingDefinitions.cs:68-70`, Standort-Text
  „AKE (IDEAL aus …)“). Damit entsteht dort nie eine CSV. IDEAL bekommt den Code, die Schalter bleiben aus.
- Das Lesen bzw. der Import in enaio sowie das Aufräumen verarbeiteter Dateien auf der Freigabe. Das ist Sache des Lesers.
- Eine Etikettendruck-Funktion für Barcode-Aufkleber.
- Rahmenabrufe (EBZ): [[2026-11-11-wareneingang-teil-3-rahmenabrufe]].

## Fachliche Anforderungen

### F1 Barcode-Feld in der Wareneingangsmaske
- Feld „Barcode (Lieferschein)“ im Kopf des Buchungsformulars, direkt neben „Lieferschein-Nr. (Lieferant)“ (`Receive.cshtml:60-66`). Es wird einmal
  je Absenden erfasst, wie die Lieferschein-Nr.: eine Lieferung = ein Lieferschein = ein Barcode.
- Sichtbar nur bei `WareneingangBarcodeAktiv` = an (Rückfrage 8). Ist der Schalter aus, gibt es kein Feld, und der Post-Parameter wird ignoriert.
- Ist der Sage-Wareneingang **beim Öffnen** aus (`vm.SageWeAktiv` = false, `PurchaseOrdersController.Receive.cs:148`), ist das Feld gesperrt.
  Hinweis darunter: „Sage-Wareneingang ist aus – für diese Buchung entsteht keine CSV-Datei.“ *Vorläufig nach Empfehlung zu Rückfrage 7.*
- Getrimmt, Länge und Zeichen nach Rückfrage 5. *Vorschlag:* nur `A-Z a-z 0-9 - _`, 1 bis 50 Zeichen. Ein anderer Wert ergibt den
  ModelState-Fehler „Barcode: nur Buchstaben, Ziffern, - und _, höchstens 50 Zeichen (wird als Dateiname verwendet).“, und es wird nichts gebucht.
  Es gibt keine stille Umformung, weil sonst Aufkleber und Dateiname auseinanderlaufen.
- Optional (Rückfrage 4). Leer = kein Barcode, keine CSV.
- Duplikat (Rückfrage 6, *Vorschlag*): Gibt es den Barcode schon an einem anderen Wareneingang, ohne Groß/Klein (Windows-Dateinamen sind
  case-insensitiv), kommt der ModelState-Fehler „Barcode <x> ist bereits dem Wareneingang zu Bestellung <Nr> vom <Datum> zugeordnet.“ Es wird
  nichts gebucht. Die Prüfung läuft vor dem Buchen in derselben Prüfphase wie Lieferschein und Zeilen (`PurchaseOrdersController.Receive.cs:74-85`).
- Gespeichert in `PurchaseOrderReceipt.Barcode` an **jedem** Receipt dieses Absendens, im selben Insert wie die Bewegung
  (`PurchaseOrdersController.Receive.cs:114-123`), also atomar mit der Buchung. Das Muster ist `Lieferschein` (`:118`).
- Bei Fehlern bleibt die Eingabe stehen (`vm.Barcode`, wie `vm.Lieferschein` an `:75`).
- **Scannen** (Belege siehe „Technischer Lösungsentwurf“):
  - Hardware-Scanner mit angetipptem Feld: Der Scan landet im Feld (Regel b, `hardware-scanner.js:284-288`).
  - Ohne angetipptes Feld landet der Scan im Barcode-Feld (Rückfrage 9).
  - Kamera-Knopf neben dem Feld (Muster `ProductionOrders/Index.cshtml:26-30`). Der Wert wird **roh** übernommen: keine Artikel-QR-Zerlegung, keine
    EAN- oder Matchcode-Auflösung.
  - Rückmeldung „✓ Gescannt: <Wert>“ wie bei den übrigen Scanfeldern (`barcode-scanner.js:374-394`).

### F2 Ausgangstabelle
- Eine Zeile je `SageWeVorgang` (UNIQUE): `Barcode`, `Status`, `Versuche`, `Fehler`, `Datei` (voller Pfad der geschriebenen `.csv`),
  `GeschriebenAm`, Audit-Felder.
- Status (`WareneingangCsvStatus`, als int, ohne 0):
  - `Offen = 1`: angelegt, noch nicht geschrieben.
  - `Geschrieben = 2`
  - `Fehler = 3`
- **Angelegt wird die Zeile vom Dienst**, sobald alle Receipts eines Vorgangs mit Barcode den Status `Uebergeben` haben und es noch keine Zeile gibt.
  Gründe:
  - Der Weg „Als übergeben markieren“ (`PurchaseOrderReceiptRepository.cs:88-103`) ist so ohne Änderung abgedeckt.
  - Der Barcode selbst ist schon atomar mit der Buchung gespeichert (F1).
  - Die Buchung legt keine zweite, nicht-atomare Zeile an. `Receive` speichert je Bewegung über `_movements.AddAsync` (`:125`).
- „Wartet“ ist deshalb **kein** Zeilen-Status, sondern wird für die Anzeige abgeleitet (F4): Barcode vorhanden, aber noch keine Zeile.

### F3 Schreiben durch den Dienst
- Eigener Schritt im Takt des `SageBookingWorker`, **nach** dem Sage-WE-Schritt (`SageBookingWorker.cs:72-75`). So bekommt ein Beleg, der im selben
  Takt übergeben wird, seine CSV sofort. Eine Instanz ist zuständig, es gibt keinen zweiten BackgroundService.
- Gate:
  - `WareneingangCsvAktiv` = an,
  - **und** `SageWareneingangBelegAktiv` = an (derselbe gelesene Wert wie `:72`),
  - **und** `WareneingangCsvPfad` nicht leer.
  - Sonst wird nichts geschrieben, und es entsteht kein Protokoll-Eintrag je Takt (Muster `SageWareneingangService.cs:71`).
  - Pfad leer bei eingeschaltetem Schalter: Hinweis in der Liste (F4) und eine Pruefung im Manifest.
- Ablauf je Takt:
  1. Ausgangszeilen für neue übergebene Vorgänge mit Barcode anlegen (`Offen`, `CreatedBy` = `system:wareneingang-csv`).
  2. Zeilen `Offen` abarbeiten, höchstens N je Takt (Rückfrage 1f).
  3. Je Zeile CSV bauen (F3a). Existiert `<Pfad>\<Barcode>.csv` schon → `Fehler` „Datei existiert bereits – nicht überschrieben“.
  4. Sonst `<Barcode>.tmp` schreiben (überschreibt nur eine eigene `.tmp`-Leiche desselben Barcodes).
  5. Dann `File.Move(tmp, csv, overwrite: false)`, ein Umbenennen auf derselben Freigabe.
  6. Erfolg → `Geschrieben`, `Datei`, `GeschriebenAm`.
  7. Jede Ausnahme → `Fehler` mit Text (gekappt auf 1000 Zeichen, [[fallstricke]] §31 „SyncLogs.Message“), `Versuche + 1`, `.tmp` best effort
     löschen.
- Wiederholung nach `Fehler`: **offen, Rückfrage 1d.** Bis zur Antwort gilt: kein automatisches Wiederholen, nur „CSV erneut schreiben“ von Hand.
- Aktivitäts-Protokoll: eigener Lauf `SyncLogServices.WareneingangCsv`, DisplayName „Wareneingang-CSV“.
  - Counts `geschrieben`, `fehler`.
  - Je Fehler eine Warnung mit Barcode und Text.
  - Ein Lauf-Eintrag nur, wenn es etwas zu tun gab (ADR 0010).

#### F3a Dateiinhalt – *Vorschlag, hängt an Rückfrage 1c, 2 und 3*

| Spalte (Kopfzeile) | Quelle | Beleg |
|---|---|---|
| `Barcode` | `PurchaseOrderReceipt.Barcode` | neu |
| `Kreditornummer` | `PurchaseOrderReceipt.LieferantKto` | `PurchaseOrderReceipt.cs:41`, gesetzt `Receive.cs:116` |
| `Kreditorname` | neu `PurchaseOrderReceipt.LieferantName` (Snapshot beim Buchen aus `SagePurchaseOrderLine.LieferantName`) | Cache-Zeile wird bei jedem Abgleich ersetzt und fehlt, sobald Sage die Bestellung schließt (`EinkaufBestellungenSyncService.cs:141,172`); nach der Übergabe ist das der Normalfall |
| `Bestellnummer` | `PurchaseOrderReceipt.Belegnummer` | `PurchaseOrderReceipt.cs:40` |
| `Lieferschein` | `PurchaseOrderReceipt.Lieferschein` | F6, `:61` |
| `SageBelegnummer` | `PurchaseOrderReceipt.SageWeBelegnummer` | gesetzt beim Senden (`SageWareneingangService.cs:149`), bei der Memo-Suche (`:117`) und von Hand (`PurchaseOrderReceiptRepository.cs:96`) |
| `Erfasser` | `PurchaseOrderReceipt.CreatedBy` (Anzeigename des angemeldeten Benutzers) | `Receive.cs:89,122` |
| `Datum` | `PurchaseOrderReceipt.CreatedAt` (Zeit der WE-Buchung, Ortszeit) | `Receive.cs:87,122` |
| `UebergebenAm` | `PurchaseOrderReceipt.SageWeAm` | `SageWareneingangService.cs:149` |

- Die Felder sind je Vorgang gleich, weil ein Absenden genau eine Bestellung betrifft (`Receive(int belId, …)`). Daher **eine** Datenzeile je Datei,
  ohne Positionen.
- **BelID des Sage-Wareneingangsbelegs wird nicht geliefert.** Das WMS speichert sie auf keinem Weg:
  - Der Client liest nur `BelegnummerFormatiert` (`SageWareneingangClient.cs:10,81,92`).
  - Die Memo-Suche übernimmt die Anzeige „Belegjahr-Belegnummer“ (`SageWareneingangService.cs:117,185`).
  - „Als übergeben markieren“ nimmt nur eine von Hand getippte Belegnummer (`PurchaseOrdersController.SageWe.cs:105-113`).
  - Eine BelID wäre also nur für einen Teil der Vorgänge da (Rückfrage 3).
- Format-Vorschlag (Rückfrage 1c):
  - Trennzeichen `;`, eine Kopfzeile, UTF-8 **mit** BOM, Zeilenende CRLF.
  - Datum `yyyy-MM-dd HH:mm`.
  - Felder mit `;`, `"` oder Zeilenumbruch in `"…"`, inneres `"` verdoppelt.
  - Leere Werte bleiben leer.

### F4 Anzeige und Bedienung
- Liste „Sage-Wareneingänge“ (`PurchaseOrdersController.SageWe.cs:50-80`) bekommt zwei Spalten.
  - „Barcode“ (`data-col-key="barcode"`).
  - „CSV“ (`data-col-key="csv"`). Text je Zeile, nie nur Farbe:
    - `–`: kein Barcode.
    - „Sage-Wareneingang deaktiviert – CSV-Übergabe wartet“: Barcode vorhanden, keine Ausgangszeile, Sage-WE aus.
    - „CSV-Übergabe aus – wartet“: Barcode vorhanden, keine Ausgangszeile, CSV-Schalter aus.
    - „wartet auf Sage-Übergabe“: Vorgang noch nicht `Uebergeben`.
    - „offen“
    - „geschrieben <dd.MM.yyyy HH:mm>“
    - „Fehler: <Text>“
    - „keine CSV (nur in Sage)“: Status `NurInSage`, ohne Vorgang (`Receive.cs:121`).
  - Beide Spalten stehen in `SageWeColumnMap` (Server-Spaltenfilter auf dem gerenderten Text, ADR 0005).
  - Zusatzaufwand: eine Abfrage der Ausgangszeilen je angezeigtem Vorgang, kein N+1.
- Hinweis (Entscheidung Gerald), sichtbar als `alert-warning` mit Zeichen und Text:
  - Ist `WareneingangCsvAktiv` an und `SageWareneingangBelegAktiv` aus, zeigen die Liste und `/ServiceSettings` „⚠ Sage-Wareneingang deaktiviert –
    CSV-Übergabe wartet“. Liegen wartende Vorgänge mit Barcode vor, kommt deren Anzahl dazu.
  - Muster: `SageWareneingang.AusHinweis` (`SageWareneingang.cs:53-56`) und `ServiceSettings/Index.cshtml:12-14`.
  - Zusätzlich bei eingeschaltetem CSV-Schalter und leerem Pfad: „⚠ CSV-Übergabe an, aber kein Pfad gesetzt – es wird nichts geschrieben.“
- „CSV erneut schreiben“ (nur bei `Fehler`):
  - Setzt die Zeile auf `Offen`, `Fehler` = null, Audit-Felder aus `ICurrentUserService`.
  - `[HttpPost]`, `[ValidateAntiForgeryToken]`, `[RequireStockKeyUserAccess]` (Muster `PurchaseOrdersController.SageWe.cs:84-98`).
  - Hinweis im Knopf-Bereich: „Vorher auf der Freigabe prüfen, ob <Barcode>.csv schon existiert bzw. verarbeitet wurde.“
- Die Maske `Receive` zeigt keinen CSV-Status, die Liste ist der eine Ort dafür.

## Technischer Loesungsentwurf

- **Leiter (ponytail):** Dieselbe Status-, Listen- und Hinweis-Mechanik wie der Sage-WE wird wiederverwendet: Worker-Schritt, AusHinweis-Muster,
  Listen-Spalten, Muster für die Hand-Aktion. Es gibt keinen neuen BackgroundService, keine neue Liste und keinen neuen Rollen-Filter.
  Dateizugriff nur mit `System.IO` (`File.WriteAllText` + `File.Move`), keine Bibliothek. Der CSV-Bau ist eine reine statische Funktion (testbar),
  kein CSV-Paket.
- **Scan, belegt:**
  - Regel (b) „fokussiertes freies Textfeld“ bekommt jeden Hardware-Scan roh (`hardware-scanner.js:228-232,284-288`).
  - Lädt die Seite `barcode-scanner.js`, gilt: Ein Scan ohne angetipptes Feld geht an das **einzige** registrierte Ziel aus `initTextInputScanner`
    (`barcode-scanner.js:639-648`). Das ist dann der Barcode-Knopf, denn die Bestellnummer im Filterkopf ist nur `data-hw-scan-ziel` und wird
    nicht registriert (`Receive.cshtml:30`). Rückfrage 9.
  - **Befund:** `initTextInputScanner(…, 'raw')` wertet heute als `'article'` aus (`barcode-scanner.js:427,429`). Ein Barcode würde am
    QR-Trennzeichen `;` abgeschnitten (`:296,308`). Ein 8- oder 12- bis 14-stelliger Ziffern-Barcode ginge in die EAN-Auflösung (`:500-503`), im
    Matchcode-Modus zusätzlich in die Matchcode-Suche (`:459`).
  - `'raw'` wird heute nirgends genutzt (alle Aufrufer: `OseonIndex.cshtml:314,323`, `StockMovements/Index.cshtml:168`,
    `InboundBulk.cshtml:297`, `ProductionOrders/Index.cshtml:288,294`, `PickingLeitstand/Index.cshtml:503,509`).
  - **Korrektur (eine Stelle):** `'raw'` bekommt `scanType 'raw'`. `processScannedValue` übernimmt den Wert dann unverändert (Zweig ohne
    `article`/`productionOrder`, `:294,321-328`), und die EAN- und Matchcode-Wrapper greifen nur bei `'article'` (`:459,501`).
  - Fenstertitel für `'raw'`: „Barcode scannen“ statt „Lagerplatz Barcode scannen“ (`:71`).
  - Kamera-Knopf mit `data-hw-scan-kamera-ziel`, weil die Seite zwei Scanfelder hat. Wächter-Liste
    `HardwareScannerTests.cs:122-135` um `PurchaseOrders/Receive.cshtml|btnScanBarcode` ergänzen.
  - Kein `confirm()` nach Scan-Fehlern ([[fallstricke]] „Scanner-Endlosschleife“).
- **Web, Buchen:**
  - `Receive(int belId, List<…>? zeilen, string? lieferschein = null, string? barcode = null)`.
  - Prüfung und Duplikat-Abfrage (`IPurchaseOrderReceiptRepository.BarcodeVergebenAsync(string)` → erster Treffer mit Bestellnummer und Datum
    oder null).
  - `Barcode` und `LieferantName = l.LieferantName` am Receipt.
  - Schalter in `BuildReceiveAsync` über `IAppSettingRepository` wie `LieferscheinPflicht` (`:149-150`).
- **Dienst:** `WareneingangCsvService.RunOnceAsync(string pfad, CancellationToken)`:
  - Statische, reine `CsvInhalt(...)` und `IstGueltigerBarcode(...)`. Die Barcode-Regel liegt einmal in `IdealAkeWms` und wird im Dienst
    wiederverwendet: der Dienst prüft vor dem Schreiben erneut, falls die DB einen Altwert enthält.
  - Die Dateioperationen laufen hinter einem kleinen Interface (`IWareneingangCsvDatei` mit `Existiert`, `SchreibeTmp`, `Umbenennen`, `LoescheTmp`),
    damit Kollision, Fehler und Erfolg InMemory testbar sind. Ein Interface mit einer echten Implementierung ist hier gerechtfertigt, weil der Test
    sonst eine UNC-Freigabe bräuchte.
  - Die echte Implementierung nutzt `File.Exists`, `File.WriteAllText(tmp, inhalt, new UTF8Encoding(true))` und
    `File.Move(tmp, ziel, overwrite: false)`.
  - DI wie `SageWareneingangService` (`Program.cs:150`). DI-Auflösungstest wie bei den übrigen Diensten.
- **Worker:** nach `:72-75` ein Aufruf, gegated wie F3. `WareneingangCsvAktiv` wird mit `GetBoolSafeAsync` gelesen: Ein Lesefehler heißt „nicht
  schreiben“ und ist harmlos (anders als Q-2).
- **Settings-Prüfung `WareneingangCsvPfad`:** Heute prüft `SettingValidator` Strings nicht (`SettingValidator.cs:68-69`). Neu ist eine
  key-spezifische Prüfung: leer **oder** beginnt mit `\\`, `Path.IsPathFullyQualified`, keine ungültigen Pfadzeichen. Sonst lehnt `/ServiceSettings`
  ab mit „„Wareneingang-CSV: Pfad“: nur ein UNC-Pfad wie \\server\freigabe\ordner oder leer.“ (ADR 0020 Punkt 2). Die Erreichbarkeit prüft der Server
  **nicht**: Der Web-App-Pool ist ein anderes Konto als der Dienst.
- **Oberfläche:** Skill `frontend-design` ausdrücklich aufrufen. Bootstrap 5, `min-height:44px` wie die Nachbarfelder (`Receive.cshtml:63`), Hinweise
  mit Zeichen und Text (WCAG AA, Farbe nie allein).

## Migrations-/SQL-Auswirkungen

| Migration | Skript | Inhalt | daten-destruktiv |
|---|---|---|---|
| `AddWareneingangBarcodeCsv` | `SQL/144_AddWareneingangBarcodeCsv.sql` (Nummer vor dem Anlegen prüfen, Rückfrage 10) | `PurchaseOrderReceipts`: `Barcode` nvarchar(50) NULL + Index `IX_PurchaseOrderReceipts_Barcode`; `LieferantName` nvarchar(50) NULL (Länge wie `SagePurchaseOrderLine.LieferantName`, `ApplicationDbContext.cs:1394`; entfällt, falls Rückfrage 3 den Kreditornamen streicht). Neue Tabelle `WareneingangCsvExporte`: `Id` int IDENTITY PK, `SageWeVorgang` uniqueidentifier NOT NULL (UNIQUE `UX_WareneingangCsvExporte_SageWeVorgang`), `Barcode` nvarchar(50) NOT NULL, `Status` int NOT NULL, `Versuche` int NOT NULL DEFAULT 0 (nur im SQL, kein `HasDefaultValue`, [[fallstricke]] wie `SageWeVersuche`), `Fehler` nvarchar(1000) NULL, `Datei` nvarchar(400) NULL, `GeschriebenAm` datetime2 NULL, Audit-Spalten wie `PurchaseOrderReceipts` (`CreatedBy`/`CreatedByWindows` nvarchar(200) NOT NULL, `Modified*` NULL); Index `Status` | nein (additiv) |

- ADR 0004:
  1. Model.
  2. `dotnet ef migrations add AddWareneingangBarcodeCsv`.
  3. Idempotentes SQL: Spalten mit `COL_LENGTH`-Guard, Tabelle mit `OBJECT_ID`-Guard, Indizes mit `sys.indexes`-Guard, DDL je eigener Batch (Muster
     `SQL/143_AddPurchaseOrderReceiptSageWe.sql`).
  4. `__EFMigrationsHistory`-Insert in eigenem Batch.
  5. `SQL/00_FreshInstall.sql` an **beiden** Stellen: Spalten in `PurchaseOrderReceipts` und neue Tabelle **und** `MigrationId`.
- `SQL/AgentJobs/*`: nicht betroffen (keine Pflichtspalte an einer AgentJob-Tabelle, kein Folge-MERGE).
- Kein Seed: Die zwei ServiceSettings legt der Web-Start aus dem Katalog an (wie in der Vorgänger-Spec, `Program.cs` Seed-Block). Das AppSetting
  braucht keine Zeile (`seed: false`).
- Die Web-App nicht lokal aus dem Worktree starten, solange die Migration pending ist ([[fallstricke]]).
- Nummer: Im Worktree liegt `SQL/143_AddPurchaseOrderReceiptSageWe.sql` als höchste Nummer (geprüft 2026-11-12). Zwei weitere Arbeiten können
  vorher eine Nummer brauchen:
  - der Freigabe-Nachtrag 14:20 „System-Kürzel“ der Vorgänger-Spec („Das Feld dafür beim Senden speichern“ = vermutlich eine Spalte),
  - die Mahnmail-Spec (Protokoll bzw. Kreditor-Feld wahrscheinlich).
  - Der Dev-Lauf vergibt in Bau-Reihenfolge und zieht Spec und Manifest nach.

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `WareneingangBarcodeAktiv` (AppSetting, Gruppe „Lager und Buchungen“, abhängig von `WareneingangBestellungAktiv`), Anzeigename **„Wareneingang: Barcode-Feld (Lieferschein-Aufkleber)“** | Bool (`SettingType.Bool`, Schalter) | an / aus (Katalog, Server prüft) | aus; `seed: false` | An: In „Wareneingang zu Bestellung“ steht neben der Lieferschein-Nr. das Feld „Barcode (Lieferschein)“ (Scan oder Eingabe), gespeichert am Wareneingang. Aus: kein Feld. Die Datei schreibt nur der Dienst-Schalter `WareneingangCsvAktiv`. | Lager → Wareneingang zu Bestellung; Liste Lager → Sage-Wareneingänge (Spalte Barcode) | AKE (IDEAL ohne Wirkung, Sage-Wareneingang aus) | – (keine Außenwirkung) |
| `WareneingangCsvAktiv` (ServiceSetting, neue Gruppe „Wareneingang-CSV“, abhängig von `SageWareneingangBelegAktiv`), Anzeigename **„Wareneingang: CSV-Übergabe auf Netzlaufwerk“** | Bool (Schalter) | an / aus (Katalog, Server prüft) | aus | An: Für jeden an Sage übergebenen Wareneingang mit Barcode schreibt der Dienst `<Barcode>.csv` auf den Pfad `WareneingangCsvPfad`, zuerst als `.tmp`, dann umbenannt. Er schreibt nur bei eingeschaltetem Sage-Wareneingang, sonst wartet die Übergabe (Hinweis). Aus: nichts wird geschrieben, die Übergabe wartet. Beim Einschalten werden wartende Vorgänge nachgeholt. | Dienst: Sage-Buchungs-Worker, Lauf „Wareneingang-CSV“; Liste „Sage-Wareneingänge“ (Spalte CSV, Hinweis); `/ServiceSettings` (Hinweis) | AKE (IDEAL aus) | „Schreibt Dateien auf die Netzfreigabe aus ‚Wareneingang-CSV: Pfad‘; ein anderes System (vermutlich enaio) liest sie. Das Dienstkonto braucht dort Schreib- und Änderungsrecht. Wirkt nur bei eingeschaltetem Sage-Wareneingang. Beim Einschalten werden alle wartenden Vorgänge sofort geschrieben.“ |
| `WareneingangCsvPfad` (ServiceSetting, Gruppe „Wareneingang-CSV“), Anzeigename **„Wareneingang-CSV: Pfad“** | String (Freitext, Pfad) | leer oder UNC-Pfad `\\server\freigabe[\ordner]`; Server prüft Form (siehe Entwurf), nicht die Erreichbarkeit | leer (= nichts schreiben, Hinweis) | Zielordner der CSV-Dateien. Dateien liegen direkt darin, keine Unterordner. | Dienst: Lauf „Wareneingang-CSV“ | AKE | „Ein falscher Pfad legt Dateien an einer Stelle ab, die der Leser nicht sieht. Pfad mit dem Betreiber des Lesers (enaio) abstimmen.“ |

- ServiceSettings in `ServiceSettingDefinitions` an **beiden** Stellen: Basisliste (Typ, Default, Kategorie, Beschreibung) und `Meta` (Anzeigename,
  Verwendung, Standort, Warnhinweis), `AbhaengigVon` für `WareneingangCsvAktiv`. Drift-Guard `ServiceSettingDefinitionsTests` bekommt beide Keys.
- AppSetting: Konstante in `AppSettingKeys`, `B(...)` in `AppSettingDefinitions.All` direkt nach `WareneingangLieferscheinPflicht`
  (`AppSettingDefinitions.cs:88-91`), Drift-Guard `AppSettingDefinitionsTests`.
- Für die Wiederholung (Rückfrage 1d) entsteht ein Key nur, wenn die Antwort einen fordert. Er ist dann mit Grenzen in `IntMeta` aufzunehmen
  (`ServiceSettingDefinitions.cs:123-130`).
- Sonst ändert sich keine Einstellung.

## Audit-Feld-Auswirkungen

- `PurchaseOrderReceipt.Barcode` und `LieferantName` entstehen im Insert beim Buchen (`Created*` wie heute, `Receive.cs:122`). Kein Update-Weg ändert
  den Barcode (eine Korrektur ist nicht vorgesehen; bei Bedarf Folge-Aufgabe).
- `WareneingangCsvExport : AuditableEntity`:
  - Anlage durch den Dienst: `CreatedBy`/`CreatedByWindows` = `system:wareneingang-csv`.
  - Jeder Statuswechsel durch den Dienst setzt `ModifiedAt`, `ModifiedBy` und `ModifiedByWindows` auf denselben Namen (Muster
    `SageWareneingangService.cs:213-215`).
  - „CSV erneut schreiben“ setzt `Modified*` aus `ICurrentUserService` (Muster `PurchaseOrderReceiptRepository.cs:82`).

## Rollen und Zugriff

- Keine neue Rolle, kein neuer Filter.
  - Buchen mit Barcode: `RequireStockAccess` (unverändert, `Receive.cs:42`).
  - Liste: `RequireStockReadAccess` (Klasse) + Feature-Gate `RequireWareneingangBestellungAktiv`, unverändert.
  - „CSV erneut schreiben“: `RequireStockKeyUserAccess` wie „Erneut senden“.
- Der Knopf ist sichtbar nur mit `CanErneutSenden` (`PurchaseOrdersController.SageWe.cs:72`).

## Prüfschritte (Mensch) – abhängige Aussagen gelten bis dahin als unbelegt

- **P-C1 Dienstkonto am AKE-Dienst-Server** (PowerShell, nur lesend):
  `Get-CimInstance Win32_Service | Where-Object { $_.Name -like '*IDEALAKEWMS*' } | Select-Object Name, StartName`
  - Bei `LocalSystem` greift der Dienst als Computerkonto `<DOMÄNE>\<SERVER>$` auf die Freigabe zu.
  - Dieses Konto (bzw. das Dienstkonto) braucht auf Freigabe **und** NTFS das Recht „Ändern“: anlegen, schreiben, umbenennen, eigene `.tmp`
    löschen.
- **P-C2 Schreibrecht** am Zielordner, ohne Datei anzulegen: `icacls "\\<server>\<freigabe>\<ordner>"`. Mit dem Ergebnis aus P-C1 abgleichen.
- **P-C3 Leser:** Wer liest den Ordner, welches Dateimuster liest er (`*.csv`, ignoriert `.tmp`?), und welches Format erwartet er? Rückfrage 2.

## Akzeptanzkriterien

1. `WareneingangBarcodeAktiv` aus: Die Maske zeigt kein Barcode-Feld. Ein gepostetes `barcode` wird ignoriert, am Receipt bleibt `Barcode` NULL.
2. Schalter an und Sage-WE an: Das Feld steht neben „Lieferschein-Nr. (Lieferant)“. Ein eingegebener gültiger Barcode steht nach dem Buchen an
   **allen** Receipts dieses Absendens, `LieferantName` ebenso.
3. Ungültiger Barcode (Zeichen bzw. Länge nach Rückfrage 5): ModelState-Fehler, **keine** Bewegung, kein Receipt. Die Eingaben bleiben stehen.
4. Barcode schon an einem anderen Wareneingang, auch in anderer Groß- und Kleinschreibung: Fehler mit Bestellnummer und Datum, nichts gebucht.
5. Hardware-Scan mit angetipptem Barcode-Feld: Der Wert steht unverändert im Feld, auch mit `;` und auch als 13-stellige Ziffernfolge. Es gibt keine
   EAN- oder Matchcode-Auflösung.
6. Kamera-Scan über den Knopf am Barcode-Feld füllt das Feld roh. Der Fenstertitel lautet „Barcode scannen“.
7. Nach der Übergabe an Sage (Status `Uebergeben`) mit CSV-Schalter an, Sage-WE an und gesetztem Pfad gilt:
   - In einem Takt entsteht genau eine Ausgangszeile und genau eine Datei `<Barcode>.csv` mit Kopfzeile und einer Datenzeile.
   - Es bleibt keine `.tmp`.
   - Die Zeile steht auf `Geschrieben` mit `Datei` und `GeschriebenAm`.
8. Ein Vorgang, der per „Als übergeben markieren“ auf `Uebergeben` kommt, bekommt seine CSV mit der von Hand eingetragenen Belegnummer.
9. `SageWareneingangBelegAktiv` aus bei CSV-Schalter an: keine Datei, keine Ausgangszeile, kein Protokoll-Eintrag je Takt. Liste **und**
   `/ServiceSettings` zeigen „Sage-Wareneingang deaktiviert – CSV-Übergabe wartet“.
10. Nach dem Wiedereinschalten des Sage-WE wird ein bereits übergebener, wartender Vorgang im nächsten Takt geschrieben.
11. `<Barcode>.csv` existiert schon auf der Freigabe: Die Datei bleibt unverändert, die Zeile steht auf `Fehler` „Datei existiert bereits – nicht
    überschrieben“, und das Protokoll hat eine Warnung.
12. Pfad nicht erreichbar bzw. kein Recht: `Fehler` mit Text, `Versuche` + 1, keine Ausnahme bricht den Worker-Takt ab. Die Sage-Lagerbuchungen
    desselben Takts laufen weiter.
13. „CSV erneut schreiben“ (nur bei `Fehler`, nur mit `stock_keyuser`/`admin`/`picking`) setzt die Zeile auf `Offen`. Audit-Felder sind gesetzt.
    Ohne Recht kommt 403 bzw. kein Knopf.
14. `/ServiceSettings` lehnt `WareneingangCsvPfad` = `C:\temp` bzw. `abc` ab (Meldung, Wert unverändert), nimmt `\\server\freigabe\ordner` und leer an.
15. Liste: Spalten „Barcode“ und „CSV“ mit `data-col-key`. Der Spaltenfilter auf „geschrieben“ bzw. „wartet“ filtert über alle Seiten (Server-Mode).
    Der CSV-Export der Liste enthält beide Spalten.
16. Drift-Guards grün: ServiceSettings, AppSettings, `NachlaufManifestDriftTests` (SQL/144), `HardwareScannerTests` (Kamera-Knopf-Liste),
    `CsvExportCoverageTests`.
17. IDEAL (Sage-WE aus): Maske und Liste verhalten sich wie vorher, solange `WareneingangBarcodeAktiv` aus ist. Der Dienst schreibt nichts.

## Test-Szenarien

Neu **TS-128 „Wareneingang: Barcode und CSV-Übergabe“** in `docs/TESTSZENARIEN.md`, danach [[testszenarien-index]].
Vorbedingungen:
- DEV AKE mit Worktree-Stand, `WareneingangBestellungAktiv` an.
- Eine offene Test-Bestellung im Cache.
- Test-Freigabe `\\<DEV-Server>\<Test>\wms-csv` mit Ändern-Recht für das DEV-Dienstkonto.
- Kein Sage-Schreiben durch Agenten: Die Übergabe wird für die Tests per SQL (Status `Uebergeben`, Belegnummer `QA-…`) bzw. per „Als übergeben
  markieren“ hergestellt.

- **128.1 Feld aus/an:** Schalter aus → kein Feld (AK 1). Schalter an, Sage-WE aus → Feld gesperrt mit Hinweis (Rückfrage 7). Sage-WE an → Feld
  aktiv.
- **128.2 Buchen mit Barcode:** gültiger Barcode, zwei Positionen → beide Receipts haben ihn (SQL-Kontrolle). Negativ: ungültige Zeichen, 51 Zeichen,
  Duplikat in Kleinbuchstaben → jeweils Meldung, nichts gebucht (AK 2-4).
- **128.3 Scannen:**
  - Netum mit angetipptem Feld: `ABC;123` und `4006381333931` kommen unverändert an.
  - Ohne angetipptes Feld landet der Scan im Barcode-Feld.
  - Kamera am Handy: Code wird roh übernommen.
  - Mit `QrErstesFeld = Matchcode`: keine Matchcode-Meldung (AK 5-6).
- **128.4 CSV nach Übergabe:** Vorgang per SQL bzw. „Als übergeben markieren“ übergeben → nach einem Takt ist `<Barcode>.csv` vorhanden, keine
  `.tmp`. Inhalt prüfen: UTF-8-BOM, Kopfzeile, Felder; Liste „geschrieben …“, Aktivitäts-Protokoll „Wareneingang-CSV“ `geschrieben=1` (AK 7-8).
- **128.5 Sage-WE aus:** CSV-Schalter an, Sage-WE aus → keine Datei, Hinweis in Liste und `/ServiceSettings`. Wieder an → nächster Takt schreibt (AK 9-10).
- **128.6 Kollision:** Datei `<Barcode>.csv` vorher von Hand anlegen → `Fehler`, Datei unverändert. Datei entfernen, „CSV erneut schreiben“ →
  geschrieben (AK 11, 13).
- **128.7 Netzfehler:** Pfad auf eine nicht vorhandene Freigabe → `Fehler`, Takt läuft weiter (Sage-Lagerbuchung im selben Takt sichtbar). Recht
  entziehen → `Fehler` „Zugriff verweigert“ (AK 12).
- **128.8 Einstellungen:** Pfad-Prüfung (AK 14). Anzeigename, Wirkung, Verwendung, Standort und Warnhinweis sichtbar. Hinweis bei Pfad leer + Schalter an.
- **128.9 Rollen:** Benutzer ohne `stock_keyuser` sieht keinen Knopf, ein POST direkt wird abgewiesen (AK 13).
- **128.10 IDEAL:** DEV IDEAL nach Deploy → keine Änderung, kein Protokoll-Eintrag „Wareneingang-CSV“ (AK 17).

Automatisiert:
- `WareneingangCsvServiceTests` mit gefälschtem `IWareneingangCsvDatei`: anlegen, schreiben, Kollision, Ausnahme, Gate Sage-WE aus, Gate Pfad leer,
  „Als übergeben“-Weg, NurInSage ohne Vorgang.
- `CsvInhalt`: Quoting, BOM, Datumsformat.
- `PurchaseOrdersReceiveTests`: Barcode gültig, ungültig, Duplikat, Schalter aus.
- JS-Selbstprüfung bzw. View-Wächter für `'raw'`.
- DI-Auflösungstest.

## Etappen (nur bei epic: true)

Entfällt (kein Epic). Umsetzung als ein Commit-Paket im Bündel-Worktree; bei Turn-Knappheit sinnvoller Schnitt: (1) Migration + Feld + Scan,
(2) Dienst + Liste + Einstellungen + Manifest.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt | – | – |

## Deploy

*Vorläufig – der qa-agent bestätigt gegen den echten Diff.*

- **Web-App:** ja (Maske, Scan-JS, Liste, Einstellungen, Katalog, Repository).
- **Service:** ja (neuer Schritt im `SageBookingWorker`, `WareneingangCsvService`).
- **Migration:** ja, `SQL/144_AddWareneingangBarcodeCsv.sql` / `<Zeitstempel>_AddWareneingangBarcodeCsv`, additiv, nicht daten-destruktiv.
- **Reihenfolge Testsystem:**
  1. DB-Backup (Standard).
  2. Update-Skript (führt SQL/144 aus).
  3. Web, dann Dienst.
  4. P-C1/P-C2 erledigen.
  5. Pfad setzen.
  6. Erst dann `WareneingangCsvAktiv` und `WareneingangBarcodeAktiv` einschalten.
- **Manifest-Einträge** `scripts/deploy/nachlauf.psd1` (ADR 0022/0023; Texte nur ASCII; Version = vergebene Version, Vorschlag 1.99.9):
  - Schema, im Block „Schema“ direkt **nach** `SQL/143` (`nachlauf.psd1:82-83`, Deploy-Reihenfolge):
    `@{ Id='SQL/144'; Version='1.99.9'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'; Datei='144_AddWareneingangBarcodeCsv.sql'; MigrationId='<Zeitstempel>_AddWareneingangBarcodeCsv'; Voraussetzung='additiv'; Quelle='<changelog-slug>' }`
  - Pruefung (SQL-prüfbarer Zustand: Schalter an, aber Pfad leer oder Sage-WE aus), Block „Pruefungen“:
    `@{ Id='Pruefung/WareneingangCsv'; Version='1.99.9'; Typ='Pruefung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'; Braucht=@('SQL/144'); Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''WareneingangCsvAktiv'' AND LOWER(LTRIM(RTRIM([Value]))) = N''true'') AND (NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''WareneingangCsvPfad'' AND LTRIM(RTRIM(ISNULL([Value], N''''))) <> N'''') OR NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''SageWareneingangBelegAktiv'' AND LOWER(LTRIM(RTRIM([Value]))) = N''true'')) THEN 1 ELSE 0 END'; Text='WareneingangCsvAktiv ist an, aber WareneingangCsvPfad leer oder SageWareneingangBelegAktiv aus: es wird keine CSV geschrieben (/ServiceSettings > Wareneingang-CSV).'; Quelle='<changelog-slug>' }`
  - Einstellung, Block „Nachher“:
    `@{ Id='v1.99.9-WareneingangCsv'; Version='1.99.9'; Typ='Einstellung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'; Text='Wareneingang-CSV: zuerst Dienstkonto pruefen (P-C1, LocalSystem = Computerkonto) und Recht Aendern auf der Freigabe setzen (P-C2), dann /ServiceSettings > Wareneingang-CSV: Pfad (\\server\freigabe\ordner) setzen, WareneingangCsvAktiv einschalten, zuletzt /Settings > Lager und Buchungen: WareneingangBarcodeAktiv. IDEAL bleibt aus.'; Quelle='<changelog-slug>' }`
  - Info, Block „Nachher“:
    `@{ Id='v1.99.9-LagerBarcode'; Version='1.99.9'; Typ='Info'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'; Text='Lager informieren: Wareneingang zu Bestellung hat ein Feld Barcode (Lieferschein-Aufkleber); die CSV entsteht erst nach der Uebergabe an Sage. Stand in Lager -> Sage-Wareneingaenge, Spalte CSV.'; Quelle='<changelog-slug>' }`
  - Kein `Ordner`-Eintrag: Der Zielpfad steht in der DB, nicht im Manifest, und gehört einem fremden Server. Das Recht bleibt Hand-Schritt (Einstellung
    oben). Kein Daten- oder Einmal-Skript.
- **Publish-Befehle (im Worktree):**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- **Standort:** AKE. IDEAL bekommt den Code, alle drei Schalter bleiben aus.
- Brain-Update (Hauptcheckout):
  - [[feature-map]], [[datenmodell]] (Spalten + Tabelle), [[services]] (Lauf „Wareneingang-CSV“, Gate), [[integrationen]] (Datei-Ausgang auf
    UNC), [[controller]] (Action + Recht), [[glossar]] („Barcode (Lieferschein-Aufkleber)“).
  - [[fallstricke]]:
    - `'raw'` war `'article'`.
    - Atomares Umbenennen nur auf derselben Freigabe.
    - Dienstkonto LocalSystem = Computerkonto.
    - Die CSV entsteht nur aus `Uebergeben`, `NichtNoetig` wartet nie.
  - Brain-Changelog.

## Offene Rueckfragen

1. **Auftrag abgeschnitten.** Der technische Teil endet bei „Ausgangstabelle mit Status, atomares Schreiben (.tmp →“. Was fehlt noch? Vorschläge,
   jeweils mit Empfehlung:
   a) *Dateinamen-Kollision:* Existiert `<Barcode>.csv`, wird nie überschrieben → `Fehler`, Klärung von Hand. Empfehlung so.
   b) *Bereinigung des Barcodes:* keine Umformung, nur Prüfung beim Erfassen (Rückfrage 5). Grund: Aufkleber und Dateiname müssen identisch sein.
   c) *Encoding/Trennzeichen:* UTF-8 mit BOM, `;`, Kopfzeile, CRLF, `yyyy-MM-dd HH:mm`, Quoting nach RFC 4180. Empfehlung so, sofern der Leser
      (Rückfrage 2) nichts anderes verlangt.
   d) *Wiederholung bei Netzfehler:* Empfehlung automatisch, frühestens nach 15 Minuten, höchstens 3-mal (zwei Int-Einstellungen mit Grenzen wie
      `Sync:SageWareneingang*`), danach nur „CSV erneut schreiben“. Kollision (a) nie automatisch. Ohne Antwort baut der Dev-Lauf nur die Hand-Aktion.
   e) *Dienstkonto-Rechte:* „Ändern“ auf Freigabe und NTFS für das Dienstkonto bzw. das Computerkonto (P-C1/P-C2). Das Update-Skript prüft das
      nicht (der Pfad steht in der DB). Empfehlung: Hand-Schritt im Manifest, keine Skript-Prüfung in diesem Schritt.
   f) *Zeitlimit:* Ein hängender UNC-Zugriff (SMB-Timeout ca. 20-60 s) hält den Worker-Takt auf und damit auch die Sage-Lagerbuchungen derselben
      Instanz. Empfehlung höchstens 10 Dateien je Takt (Konstante) und `.tmp`-Leichen nur für den eigenen Barcode löschen. Ein eigener Worker nur,
      wenn das im Betrieb stört.
2. **Leser und Zweck:** Die enaio-Zuordnung über Barcode und CSV ist unbestätigt. Wer liest den Ordner? Erwartet der Leser feste Feldnamen, eine
   Reihenfolge, ein Dateimuster (ignoriert er `.tmp`?), ein anderes Encoding oder Datumsformat? Löscht bzw. verschiebt er verarbeitete Dateien?
   *Empfehlung:* Gerald klärt das mit der enaio-Betreuung vor dem Einschalten. Bis dahin gilt das Format aus 1c, die Feldnamen aus F3a sind
   Platzhalter.
3. **CSV-Spalten:** Vorschlag F3a mit 9 Feldern und einer Datenzeile je Datei. Zwei Unterfragen:
   (a) Die **BelID** des Sage-Wareneingangsbelegs ist nicht auf allen Wegen vorhanden („Als übergeben markieren“ kennt nur die Belegnummer). Reicht
   die Sage-Belegnummer? *Empfehlung:* ja, nur `SageBelegnummer`.
   (b) **Kreditorname** braucht einen Snapshot am Wareneingang (neue Spalte), weil der Bestell-Cache nach der Übergabe verschwindet. *Empfehlung:*
   ja. Weitere Felder (Windows-Benutzer, Positionen, Mengen)? *Empfehlung:* nein.
4. **Pflichtfeld oder optional?** *Empfehlung:* optional und ohne Pflicht-Schalter in diesem Schritt. Eine Lieferung ohne Aufkleber soll nicht am
   Wareneingang hängen. Ein Schalter wie `WareneingangLieferscheinPflicht` lässt sich später ergänzen.
5. **Validierung:** Welches Format haben die Aufkleber (Muster bitte)? *Empfehlung:* nur `A-Z a-z 0-9 - _`, 1 bis 50 Zeichen, sonst Fehler, keine
   Umformung. Damit ist der Wert immer ein gültiger Dateiname (keine `\ / : * ? " < > |`, kein Punkt am Ende, keine reservierten Namen wie `CON`).
6. **Duplikate:** *Empfehlung:* Ein Barcode darf im WMS nur an **einem** Wareneingang (Absenden) hängen, ohne Groß/Klein. Ein zweiter Versuch wird
   beim Buchen abgewiesen. Existiert die Datei auf der Freigabe, wird nie überschrieben (1a).
7. **Sage-WE beim Buchen aus:** Solche Wareneingänge bekommen `NichtNoetig` (`Receive.cs:119`) und werden nie „Übergeben“. Dasselbe gilt für
   reine `NurInSage`-Buchungen ohne Vorgang (`:121`). Eine CSV entstünde also **nie**, auch nicht nach dem Einschalten. Der Hinweis „CSV-Übergabe
   wartet“ wäre für sie falsch. *Empfehlung:* Bei ausgeschaltetem Sage-WE ist das Feld gesperrt, mit dem Hinweis aus F1. „Wartet“ gilt nur für
   Vorgänge, die übergeben werden können.
8. **Ein Schalter oder zwei?** *Empfehlung:* zwei. `WareneingangBarcodeAktiv` (AppSetting) steuert das Feld, `WareneingangCsvAktiv`
   (ServiceSetting) das Schreiben. So lässt sich die Übergabe anhalten (Freigabe-Wartung), ohne dass das Lager Barcodes verliert.
9. **Scan ohne angetipptes Feld:** Nach dem Öffnen einer Bestellung geht ein Hardware-Scan ohne angetipptes Feld ins Barcode-Feld, auch wenn
   jemand eine neue Bestellnummer scannt. *Empfehlung:* akzeptieren, denn der Aufkleber ist der häufigste Scan auf dieser Seite, und
   „✓ Gescannt: <Wert>“ zeigt das Ziel. Wer eine andere Bestellung öffnen will, tippt das Bestellnummer-Feld an.
10. **Version und SQL-Nummer:** Im Bündel sind noch der System-Kürzel-Nachtrag der Vorgänger-Spec (vermutlich eine Spalte) und die Mahnmail-Spec
    offen. Diese Spec schlägt 1.99.9 / SQL 144 / TS-128 vor. *Empfehlung:* Der Dev-Lauf vergibt Version, SQL- und TS-Nummer in Bau-Reihenfolge und
    zieht Spec und Manifest nach; die Mahnmail-Spec nimmt die nächste TS-Nummer.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Auftrag abgeschnitten – was fehlt (a Kollision, b Bereinigung, c Encoding/Trennzeichen, d Wiederholung, e Dienstkonto-Rechte, f Zeitlimit)? →
2. Leser/Zweck enaio und erwartetes Format? →
3. CSV-Spalten; nur Sage-Belegnummer statt BelID; Kreditorname als Snapshot? →
4. Barcode Pflichtfeld oder optional? →
5. Erlaubte Zeichen und Länge des Barcodes (Muster-Aufkleber)? →
6. Duplikate: Sperre beim Buchen, nie überschreiben? →
7. Sage-WE beim Buchen aus: Feld sperren statt „wartet“? →
8. Ein Schalter oder zwei (Feld vs. Dienst)? →
9. Scan ohne angetipptes Feld geht ins Barcode-Feld – akzeptiert? →
10. Version/SQL-/TS-Nummer in Bau-Reihenfolge durch den Dev-Lauf? →
