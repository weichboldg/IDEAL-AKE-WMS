---
type: spec
title: Qualitätsprüfung – Prüfvorlagen, Prüfmaske mit Fotos, dateibasierte Übergabe an enaio
slug: 2026-11-08-qualitaetsmasken-fragebogen-foto-enaio-spec
status: Entwurf
created: 2026-11-07
updated: 2026-11-07
source_backlog: "[[2026-11-07-qualitaetsmasken-fragebogen-foto-enaio]]"
buendel: "[[2026-11-08-buendel-uebersicht]]"
version: 1.86.0
sql_nummern: "122-127 (genutzt 122, 123, 124; 125-127 Reserve)"
testszenario: TS-118
task: ""
worktree: ""
branch: ""
standort: beide
affected_code:
  - IdealAkeWms/Models/InspectionTemplate.cs (neu)
  - IdealAkeWms/Models/InspectionQuestion.cs (neu)
  - IdealAkeWms/Models/Inspection.cs (neu)
  - IdealAkeWms/Models/InspectionAnswer.cs (neu)
  - IdealAkeWms/Models/InspectionFile.cs (neu)
  - IdealAkeWms/Data/ApplicationDbContext.cs
  - IdealAkeWms/Data/Repositories/IInspectionTemplateRepository.cs + InspectionTemplateRepository.cs (neu)
  - IdealAkeWms/Data/Repositories/IInspectionRepository.cs + InspectionRepository.cs (neu)
  - IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs (neue Methode HauptFA per OrderNumber)
  - IdealAkeWms/Controllers/InspectionTemplatesController.cs (neu)
  - IdealAkeWms/Controllers/QualityInspectionsController.cs (neu)
  - IdealAkeWms/Views/InspectionTemplates/* (neu)
  - IdealAkeWms/Views/QualityInspections/* (neu, inkl. Protocol.cshtml fuer PDF)
  - IdealAkeWms/Views/ProductionOrders/_ProductionOrderRow.cshtml (Zeilenaktion Endpruefung)
  - IdealAkeWms/Views/Shared/_Layout.cshtml (Menue)
  - IdealAkeWms/wwwroot/js/photo-upload.js (compressImage herausloesen)
  - IdealAkeWms/wwwroot/js/image-compress.js (neu)
  - IdealAkeWms/wwwroot/js/quality-inspection.js (neu)
  - IdealAkeWms/Models/RoleKeys.cs
  - IdealAkeWms/Services/ICurrentUserService.cs + CurrentUserService.cs
  - IdealAkeWms/Filters/RequireQualityInspectionAccessAttribute.cs (neu)
  - IdealAkeWms/Filters/RequireInspectionTemplateAccessAttribute.cs (neu)
  - IdealAkeWms/Filters/RequireQualitaetspruefungAktivAttribute.cs (neu)
  - IdealAkeWms/Models/AppSettingKeys.cs + AppSettingDefinitions.cs
  - IdealAkeWms/Models/ServiceSettingDefinitions.cs
  - IdealAkeWms/Models/SettingDefinition.cs (optionales Feld Pfad)
  - IdealAkeWms/Services/Settings/SettingValidator.cs (Pfadpruefung)
  - IdealAkeWms/Services/SyncLogger/SyncLogServices.cs
  - IdealAkeWms/Services/Inspections/InspectionEvaluator.cs (neu, rein)
  - IdealAkeWms/Services/Inspections/EnaioCsvWriter.cs (neu, rein)
  - IdealAkeWms/Program.cs (Rollen-Seed)
  - IDEALAKEWMSService/Services/QsEnaioUebergabeService.cs (neu)
  - IDEALAKEWMSService/Workers/SyncWorker.cs (neuer Schritt)
  - IDEALAKEWMSService/Workers/CleanupWorker.cs (Aufbewahrung)
  - IDEALAKEWMSService/Program.cs (Registrierung)
  - SQL/122_AddInspectionTemplates.sql, SQL/123_AddInspections.sql, SQL/124_AddInspectionFiles.sql, SQL/00_FreshInstall.sql
  - Views/Users/RoleOverview.cshtml, Views/Help/Index.cshtml, Views/Help/Changelog.cshtml, README.md, docs/TESTSZENARIEN.md
  - AppVersion.cs (Web + Service) -> 1.86.0
open_questions:
  - "1. Speicherort Fotos/Protokoll bis zur Übergabe: Datenbank (varbinary) oder Dateisystem? Empfehlung Datenbank."
  - "2. Fotogröße/Kompression: wie Kommissionierung (Breite max. 1920 px, JPEG 0,75) plus Servergrenze 10 MB/Datei und 20 Fotos je Prüfung? Empfehlung ja."
  - "3. Aufbewahrung: Dateiinhalte N Tage nach erfolgreicher Übergabe löschen, Prüfdaten dauerhaft behalten? Empfehlung 90 Tage, als Einstellung."
  - "4. Was macht die enaio-Import-Aktion nach dem Import mit CSV und Dateien (verschieben, löschen, Fehlerordner)? Davon hängt die Erfolgserkennung ab."
  - "5. Freigabepfad je Standort und Dienstkonto mit Schreibrecht; hat IDEAL eine enaio-Freigabe?"
  - "6. CSV-Definition: bestätigt der enaio-Betreuer Spalten, UTF-8 ohne BOM, Dateinamen? Was genau ist die Spalte Kundenauftrag (Bestellnummer des Kunden?)"
  - "7. Rollen: zwei neue Rollen (prüfen / Vorlagen pflegen) statt Anbindung an bestehende Rollen? Empfehlung ja."
  - "8. Endprüfung bei IDEAL nur am HauptFA (komplettes Gerät)? Empfehlung ja."
  - "9. Gibt es Seriennummern je Gerät in Sage? Bis dahin optionales Eingabe-/Scanfeld? Empfehlung Feld jetzt, Sage-Anbindung später."
  - "10. Vorlagen je Artikelkategorie/Gerätetyp schon in diesem Epic? Empfehlung nein, eine aktive Vorlage je Prüfart."
  - "11. Folgeprozess bei n.i.O. (Sperrlager, Reklamation, Nacharbeit) in diesem Epic? Empfehlung nein, nur dokumentieren (Bemerkung Pflicht)."
epic: true
etappen:
  - "E1 Prüfvorlagen: Modell, Rollen, Schalter, Vorlagen-Editor (SQL/122)"
  - "E2 Prüfung durchführen, Prüfart Endprüfung am FA, Aufrufvertrag, Liste/Detail (SQL/123)"
  - "E3 Fotos + PDF-Prüfprotokoll (SQL/124) - abhängig von Rückfrage 1/2"
  - "E4 enaio-Übergabe im Dienst (CSV + Dateien, Status, Protokoll) - UAT abhängig von Rückfrage 5/6"
  - "E5 Aufbewahrung + Abhol-Erkennung - abhängig von Rückfrage 3/4"
  - "E6 Doku, Hilfe, TS-118, Version 1.86.0, Brain"
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

> [!warning] Zurückgestellt (Gerald 2026-11-07 19:19): „brauchen wir noch nicht“ – NICHT umsetzen. Die Freigabe-Antworten bleiben stehen; bei Wiederaufnahme nur Freigabe neu setzen.


# Qualitätsprüfung – Prüfvorlagen, Prüfmaske mit Fotos, Übergabe an enaio

> [!info] Bündel 2026-11-08, Teil B – eigener Dev-Lauf
> Teil von [[2026-11-08-buendel-uebersicht]]. Version **1.86.0**, SQL-Block **122–127**, Testszenario **TS-118**.
> Dieses Epic baut die Prüfmaske. [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]] und
> [[2026-11-08-versandmasken-geraete-fas-spec]] rufen sie nur über den Aufrufvertrag (Abschnitt T3) auf.

## Ziel / Nutzen (das Warum)

Qualitätsprüfungen (Wareneingang, Warenausgang/Versand, Endprüfung) laufen heute außerhalb des WMS. Ziel: Prüfungen nach einem
gepflegten Fragebogen am Tablet/Handy durchführen, mit Fotos belegen, Ergebnis i. O./n. i. O. festhalten und das Prüfprotokoll (PDF) samt
Fotos und einer selbst definierten CSV-Indexdatei **dateibasiert** an enaio übergeben. Dateibasiert entkoppelt: Ist enaio oder die Freigabe
nicht erreichbar, prüft das Lager trotzdem weiter; die Übergabe holt nach.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Prüfvorlagen (Fragebogen) je Prüfart mit Fragetypen Ja/Nein, Messwert mit Grenzen, Auswahl, Freitext, Foto (Pflicht-Foto über Mindestanzahl).
- Prüfmaske (mobil tauglich) mit Bezugsdaten, Bewertung, Abschluss, Liste und Detail.
- Prüfart **Endprüfung am FA** komplett mit Einstieg (FA-Liste und FA-Nummer scannen). Prüfarten **Wareneingang** und **Warenausgang** sind
  im Modell, im Vorlagen-Editor und im Aufrufvertrag vorhanden; ihre Einstiegsknöpfe bauen die beiden anderen Epics.
- Fotos über die native Kamera-Eingabe, PDF-Protokoll über die vorhandene PDF-Erzeugung.
- Dienst-Lauf „QS-Übergabe an enaio“: je Prüfung PDF + Fotos + CSV in den Unterordner der Prüfart auf einer Freigabe; Status je Prüfung.
- Aufbewahrung/Bereinigung der Dateiinhalte, Abhol-Erkennung (soweit enaio-Verhalten es zulässt).

**Out-of-Scope**
- REST-Anbindung an enaio (OSREST / DMS Service) und Rückmeldung einer enaio-Dokument-ID. Später, falls ein Rücklink gebraucht wird.
- Weg über Sage-DMS (`DMSRegisterExternalDocumentsService`, Recherche Koordination) – Gerald hat dateibasiert entschieden.
- Folgeprozesse bei n. i. O. (Sperrlager, Reklamation an Lieferant, Nacharbeit) – siehe Rückfrage 11.
- Vorlagen je Artikelkategorie/Gerätetyp – siehe Rückfrage 10.
- Einstiegsknöpfe in Wareneingang- und Versandmaske (gehören zu den anderen Epics).
- Anzeige der in enaio importierten Protokolle als Badge am FA (vorhandener Lese-Weg `EnaioDmsSyncService` liest nur die View
  `vw_IDEAL-AKE_Fertigungsauftraege`; ob dort ein neuer Dokumenttyp erscheint, entscheidet der enaio-Betreuer – Folgearbeit).
- Korrektur des vorhandenen Kommissionierungs-Foto-Uploads (Zusatzfund Z1 unten) – nur gemeldet.

## Fachliche Anforderungen

**F1 Prüfarten.** Feste Liste: `Wareneingang`, `Warenausgang` (Versand), `Endpruefung`. Der Wert ist zugleich der Name des Unterordners auf
der enaio-Freigabe und der Wert der CSV-Spalte `QmTyp` (Vorgabe Gerald 2026-11-07 16:09). Kein Einstellungswert – feste Konstante.

**F2 Prüfvorlage.** Name, Prüfart, Beschreibung, aktiv/inaktiv, Versionszähler. Je Prüfart ist höchstens **eine** Vorlage aktiv (Rückfrage 10).
Fragen mit Reihenfolge, Text, Typ, Pflicht, aktiv/inaktiv:
- *Ja/Nein*: optional „erwartete Antwort“ (Ja oder Nein); Abweichung = n. i. O.
- *Messwert*: Einheit, optional Min und/oder Max; außerhalb = n. i. O.
- *Auswahl*: Optionen (je Zeile eine), Teilmenge „n. i. O.-Optionen“.
- *Freitext*: keine Bewertung.
- *Foto*: Mindestanzahl Fotos (0 = freiwillig). Bewertung keine; erfüllt/nicht erfüllt beim Abschluss.
Speichern einer Vorlage erhöht den Versionszähler. Fragen mit Antworten werden nicht gelöscht, nur deaktiviert.

**F3 Versionierung über Schnappschuss.** Jede Antwort speichert Fragetext, Typ, Einheit, Grenzen, erwartete Antwort und Optionen zum
Zeitpunkt der Prüfung; die Prüfung speichert Vorlagen-Id und -Version. Spätere Vorlagenänderungen verändern abgeschlossene Prüfungen und
deren Protokoll nicht. (Damit entfällt eine eigene Versionstabelle.)

**F4 Prüfung anlegen.** Über den Aufrufvertrag (T3) mit Prüfart und Bezugsdaten. Bezugsdaten werden beim Anlegen als Text in die Prüfung
kopiert (Schnappschuss, unabhängig von späteren Sage-Änderungen). Ohne aktive Vorlage für die Prüfart: kein Anlegen, Hinweis „Für diese
Prüfart ist keine aktive Prüfvorlage hinterlegt.“

**F5 Endprüfung am FA.** Einstieg (a) Zeilenaktion in der FA-Liste, (b) „Neue Endprüfung“ mit FA-Nummer eintippen oder scannen (vorhandener
Kamera-/Hardware-Scanner). Die FA wird über `OrderNumber` **und** `ProductionOrder.IsHauptFa` gesucht (bei AKE trifft das jede flache FA,
weil dort `SubOrderNumber == OrderNumber` gilt, `ProductionOrder.cs:15-17`; bei IDEAL nur das Gerät, Rückfrage 8). Stornierte FA
(`IsCancelled`) → abgelehnt mit Meldung. Mehrere Endprüfungen je FA sind erlaubt (Nachprüfung). Übernommene Bezugsdaten:
`FaNummer = OrderNumber`, `Artikelnummer`, `Artikelbezeichnung = Description1`, `Kunde = Customer`; bei IDEAL zusätzlich AB-Nummer(n) und
Position(en) aus `FaHierarchyOrderInfo` zum HauptFA (mehrere Zeilen je HauptFA möglich, `FaHierarchyOrderInfo.cs:7-9` → verschiedene Werte mit
„ + “ verbinden). Bei AKE bleibt die AB-Nummer leer: das WMS führt sie dort nicht (`ProductionOrder` hat kein AB-Feld, `ProductionOrder.cs:12-110`;
AB-Nummer gibt es nur im IDEAL-Cache `FaHierarchyOrderInfo.ABNr`, `FaHierarchyOrderInfo.cs:23`).

**F6 Durchführen.** Eine Seite, Fragen als Karten in Vorlagenreihenfolge, große Bedienelemente (Tablet/Handy). Zwischenspeichern jederzeit
(Status `Entwurf`). Seriennummer optional als Eingabe-/Scanfeld (Rückfrage 9). Bemerkung frei; **bei n. i. O. Pflicht**.

**F7 Bewertung und Abschluss.** Beim Abschluss prüft der Server: alle Pflichtfragen beantwortet, Messwert ist eine Zahl, Foto-Mindestanzahl
erfüllt. Bewertung je Antwort nach F2. Gesamtergebnis: hat **eine** Antwort n. i. O., ist das Ergebnis zwingend **n. i. O.**; sonst wählt der Prüfer
(Vorbelegung i. O.) und darf auch n. i. O. setzen. Abgeschlossene Prüfungen sind unveränderlich. Prüfer = angemeldeter Benutzer, Zeit =
Abschlusszeitpunkt (Ortszeit wie im übrigen Dienst, z. B. `SageBookingWorker.cs:115`).

**F8 Fotos.** Je Foto-Frage aufnehmen über `<input type="file" accept="image/*" capture="environment">`, clientseitig verkleinert, sofort
hochgeladen in die Entwurfs-Prüfung, als Vorschaubild angezeigt, im Entwurf löschbar. Anzeige nur über einen Controller mit Rollenfilter, **nie**
unter `wwwroot` (siehe Z1).

**F9 Prüfprotokoll (PDF).** Beim Abschluss erzeugt: Kopf (Briefkopf wie die vorhandenen Druckdokumente), Prüfart, Prüfungsnummer `QS-<Id>`,
Bezugsdaten, Prüfer, Zeit, Vorlage + Version, je Frage Antwort und Bewertung, Fotos als Vorschaubilder, Gesamtergebnis, Bemerkung. Fehlt
Edge oder scheitert die Erzeugung, wird die Prüfung trotzdem abgeschlossen; Detailseite zeigt einen Warnbanner und den Knopf
„Protokoll erzeugen“. Ohne Protokoll keine Übergabe an enaio.

**F10 Übergabe an enaio (Dienst).** Siehe T5 und CSV-Definition T6. Status je Prüfung: `Offen` → `Uebergeben` (alle Dateien + CSV geschrieben)
→ optional `Abgeholt` (E5, Rückfrage 4); `Fehler` mit Text und Versuchszähler. Abgeschlossene Prüfung = automatisch `Offen`. Knopf „Erneut
übergeben“ (nur Vorlagen-Rolle) setzt `Uebergeben`/`Fehler` zurück auf `Offen`. Bei Schalter aus bleiben Prüfungen `Offen`; die Liste zeigt das.

**F11 Liste.** „Qualitätsprüfungen“: alle Prüfungen mit Prüfart, Nummer, Datum, Prüfer, Bezug (FA / Bestellung / Lieferschein / AB),
Artikel, Ergebnis, Status, enaio-Status. Filterkarte und Spaltenfilter (ADR 0005). Detail mit Antworten, Fotos, PDF-Download.

## Technischer Loesungsentwurf

**T1 Modell (englische Bezeichner, deutsche UI – CLAUDE.md Sprachregel; der Backlog-Vorschlag `QsVorlagen`/`QsFragen` … wird so umbenannt).**
Alle Entitäten erben `AuditableEntity` (ADR 0003).
- `InspectionTemplate`: `Name` (200), `Type` (`InspectionType`), `Description` (1000, null), `IsActive`, `Version` (int). Gefilterter Unique-Index
  `(Type) WHERE IsActive = 1` **plus** App-Guard im Repository (EF InMemory erzwingt Unique-Indizes nicht – Memory „InMemory Unique Indexes“).
- `InspectionQuestion`: `TemplateId` (FK Cascade), `SortOrder`, `Text` (500), `Type` (`QuestionType`: YesNo/Measurement/Choice/FreeText/Photo),
  `IsRequired`, `ExpectedYes` (bool?), `Min`/`Max` (decimal(18,4)?), `Unit` (20), `Options`/`FailOptions` (1000, zeilengetrennt), `MinPhotos` (int?), `IsActive`.
- `Inspection`: `Type`, `TemplateId` (FK Restrict), `TemplateVersion`, `Status` (Draft/Completed), `Result` (Pass/Fail, null im Entwurf),
  `ProductionOrderId` (int?, FK Restrict), Bezug-Schnappschuss als Text: `OrderNumber`, `CustomerOrder`, `AbNumber`, `AbPosition`, `Customer`,
  `ArticleNumber`, `ArticleDescription`, `SerialNumber`, `SupplierAccount`, `SupplierName`, `PurchaseOrderNumber`, `PurchaseOrderPosition`,
  `DeliveryNoteNumber` (je nvarchar 100–200, null), `Remark` (2000), `CompletedAt`, `CompletedBy`; enaio: `EnaioStatus`
  (Open/Transferred/PickedUp/Failed), `EnaioAttempts`, `EnaioLastError` (2000), `EnaioTransferredAt`. Index `(Status, EnaioStatus)`, `(OrderNumber)`.
- `InspectionAnswer`: `InspectionId` (FK Cascade), `QuestionId` (FK Restrict, null-bar für Robustheit), Schnappschuss (`QuestionText`,
  `QuestionType`, `Unit`, `Min`, `Max`, `ExpectedYes`, `Options`, `FailOptions`, `SortOrder`, `IsRequired`, `MinPhotos`), Werte `BoolValue`,
  `DecimalValue`, `TextValue`, Bewertung `Assessment` (None/Pass/Fail).
- `InspectionFile` (E3): `InspectionId` (FK Cascade), `AnswerId` (int?, Foto gehört zur Frage), `Kind` (Photo/Report), `FileName` (200),
  `ContentType` (100), `SizeBytes`, `Content` (varbinary(max), **null-bar** – nach Aufbewahrung geleert), `ContentRemovedAt`.
  Gilt bei Antwort 1 = Datenbank; bei Antwort 1 = Dateisystem ersetzt `RelativePath` die Spalte `Content` (sonst gleich).

**T2 Bewertung rein und testbar.** `InspectionEvaluator` (statisch, ohne DB): Bewertung je Antwort, Pflicht-/Foto-Prüfung, Gesamtergebnis
nach F7. Unit-Tests decken jeden Fragetyp, Grenzen inklusiv, fehlende Grenzen, n. i. O.-Zwang.

**T3 Aufrufvertrag (für die anderen Epics).**
`GET /QualityInspections/Create?type=<Wareneingang|Warenausgang|Endpruefung>&<Bezugsfelder>&returnUrl=<lokal>`
Bezugsfelder (alle optional, Text): `productionOrderId`, `orderNumber`, `customerOrder`, `abNumber`, `abPosition`, `customer`, `articleNumber`,
`articleDescription`, `serialNumber`, `supplierAccount`, `supplierName`, `purchaseOrderNumber`, `purchaseOrderPosition`, `deliveryNoteNumber`.
Die Seite zeigt die Bezugsdaten zur Bestätigung; `POST Create` legt den Entwurf an. `returnUrl` nur mit `Url.IsLocalUrl` (sonst ignoriert); nach
Abschluss Rücksprung dorthin, sonst Detailseite. Für `Endpruefung` mit `productionOrderId` oder `orderNumber` füllt der Server die FA-Felder
selbst (F5) – Aufrufer liefern dann nur die Id. Der Vertrag ist ein reiner Link: die anderen Epics brauchen keine Typen dieses Epics.
Wareneingang/Versand übergeben ihre Sage-Bezüge (Bestell-Nr./Pos., Kreditor-Kto, Lieferschein, AB) als Text – keine FKs auf deren künftige Tabellen.

**T4 Web.**
- `InspectionTemplatesController` (MVC): Index (klein, unpaginiert → Spaltenfilter im **Client-Mode**, zulässig nach ADR 0005 für kleine,
  unpaginierte Ansichten), Create/Edit (Vorlage + Fragenliste, Reihenfolge per Hoch/Runter-Knöpfen), Aktivieren/Deaktivieren.
  Fragetyp „Foto“ im Editor erst ab E3 auswählbar.
- `QualityInspectionsController` (MVC): Index (Listen-View-Pattern, Referenz `ProductionOrdersController.Index`: `PageSize.Resolve`,
  `PaginationState`, `_Pagination`, Filterkarte Prüfart/Ergebnis/Status/enaio-Status/Zeitraum, Server-Spaltenfilter `data-server-column-filter="true"`,
  alle `<th>` mit `data-col-key`, `ColumnFilterHelper.ReadFromQuery`; Datumsfilter in C# nach der Projektion nur falls berechnet – hier sind
  die Daten gespeichert, daher in SQL), Create (GET/POST), Edit (Entwurf ausfüllen, POST Zwischenspeichern/Abschließen), Details,
  `DeleteDraft`, `Protocol` (PDF-Download), `RegenerateProtocol`, `ResetEnaio`; Foto-Endpunkte `UploadPhoto` (POST, multipart),
  `Photo/{fileId}` (GET, liefert Bild aus der Ablage), `DeletePhoto` (POST, nur Entwurf). Alle POST mit `[ValidateAntiForgeryToken]`;
  Rückmeldungen nur `TempData["SuccessMessage"]`/`["WarningMessage"]`.
- FA-Suche: neue Repository-Methode `GetHauptFaByOrderNumberAsync(string)` mit `.Where(ProductionOrder.IsHauptFa)` (fallstricke §17). Die
  vorhandene `GetByOrderNumberAsync` taugt nicht: sie liefert `FirstOrDefault` ohne HauptFA-Filter (`ProductionOrderRepository.cs:193-196`),
  bei IDEAL also eine beliebige Zeile der Gruppe (`OrderNumber` ist dort nicht eindeutig, `ProductionOrder.cs:15-17`).
- FA-Liste: Zeilenaktion „Endprüfung“ in `_ProductionOrderRow.cshtml`, nur sichtbar mit Prüfrolle und Schalter an, bei IDEAL nur an HauptFA-Zeilen.
- Foto-JS: `compressImage` liegt heute innerhalb von `initPhotoUpload` (`photo-upload.js:19-50`, Aufruf mit 1920/0,75 in `photo-upload.js:53`).
  Herauslösen nach `wwwroot/js/image-compress.js` (global), `photo-upload.js` ruft es unverändert auf, `quality-inspection.js` ebenso.
  Gemeinsames JS genau einmal anfassen; Kommissionierungs-Fotos verhalten sich danach gleich (Regression in TS-118).
- Kamera: native Eingabe wie in `Views/Picking/Bom.cshtml:436` (`accept="image/*" capture="environment"`). `barcode-scanner.js` nutzt für den
  Live-Scan `getUserMedia` (`barcode-scanner.js:24`, `:40-44`) und hat als Rückfall dieselbe native Datei-Eingabe (`barcode-scanner.js:107`) –
  für Fotos ist der Live-Stream unnötig (eigene Vorschau-UI, sicherer Kontext nötig); die native Eingabe öffnet auf Tablet/Handy die
  Kamera-App und auf dem PC einen Dateidialog.
- Upload-Prüfung serverseitig: nur Entwurf, nur Frage vom Typ Foto dieser Prüfung, Größe ≤ 10 MB, Inhalt beginnt mit JPEG- oder PNG-Signatur
  (nicht dem `ContentType` des Browsers vertrauen), höchstens 20 Fotos je Prüfung (Rückfrage 2). Dateiname serverseitig gebildet.
- PDF: View `QualityInspections/Protocol.cshtml` über `IRazorViewRenderer.RenderToStringAsync` (`RazorViewRenderer.cs:12-14`) und
  `IPdfRenderService.RenderHtmlToPdfAsync` (`PdfRenderService.cs:6-9`); Briefkopf wie die vorhandenen Druckdokumente
  (`AppSettingKeys.BriefkopfVorlagePfad`, `AppSettingDefinitions.cs:186`). Fotos als Data-URI-Vorschaubilder eingebettet (Edge rendert aus
  einer temporären HTML-Datei, `PdfRenderService.cs:53-60` – Bild-URLs der App wären dort ohne Sitzung nicht ladbar). Das PDF wird als
  `InspectionFile` (`Kind = Report`) gespeichert. Das PDF entsteht im Web, weil Edge und `ControllerContext` nur dort verfügbar sind.

**T5 Dienst-Lauf „QS-Übergabe an enaio“.**
- Neuer Schritt im `SyncWorker` nach dem enaio-DMS-Schritt (`SyncWorker.cs:138-142`), über `RunResilientAsync`, gesteuert durch
  `Qs:EnaioUebergabeAktiv`. Kein eigener Worker: Minuten-Latenz genügt, der Lauf schreibt nur Dateien. Ein eigener kurzgetakteter Worker wie
  `SageBookingWorker` (`SageBookingWorker.cs:9-22`) ist hier nicht nötig.
- Aktivitäts-Protokoll Pflicht (ADR 0010): Service-Name `QsEnaioUebergabe` in `SyncLogServices` (neben `EnaioDms`, `SyncLogServices.cs:16`), in
  `All` und `DisplayNames` („QS-Übergabe an enaio“), `ISyncLogger` als letzter Ctor-Parameter, deutsche Counts-Keys `Übergeben`, `Fehler`,
  `Übersprungen`.
- Ablauf je Lauf: Pfad lesen; leer → Warnung im Protokoll, Lauf endet ohne Fehler. Pfad nicht erreichbar → Lauf scheitert (Fehlermail über
  den vorhandenen Weg von `RunResilientAsync`). Je Prüfung mit `Status = Completed` und `EnaioStatus in (Open, Failed)` (Stapel 50):
  ohne Protokoll → `Übersprungen` + Protokollzeile; sonst Zielordner `<Pfad>\<QmTyp>` anlegen, PDF und Fotos schreiben, dann CSV als
  `QS-<Id>.csv.tmp` schreiben und per `File.Move(..., overwrite: true)` in `QS-<Id>.csv` umbenennen (enaio sieht nie eine halbe CSV, Vorgabe
  Gerald). Erfolg → `Transferred`, `EnaioTransferredAt`; Ausnahme je Prüfung → `Failed`, `EnaioAttempts++`, `EnaioLastError`, nächste Prüfung.
  Dateinamen sind je Prüfung fest, ein Wiederholungslauf überschreibt nur eigene Dateien. `Transferred` wird nie automatisch erneut gesendet.
- Audit: `ModifiedAt/By/ByWindows` = Zeitpunkt + Service-Name (ADR 0003).
- Der Dienst liest Dateiinhalte aus der Ablage (Antwort 1). Bei Ablage in der DB braucht er keinen gemeinsamen Pfad mit dem Web.

**T6 CSV-Definition (selbst definiert, Vorgabe Gerald 2026-11-07 16:09).**
- Datei je Prüfung: `QS-<Id>.csv`, im selben Unterordner wie die Dateien. Kodierung **UTF-8 ohne BOM** (Rückfrage 6), Zeilenende CRLF,
  Trennzeichen `;`, Kopfzeile mit den Spaltennamen unten. Felder mit `;`, `"`, CR oder LF in `"` gesetzt, `"` verdoppelt. Leere Werte = leeres Feld.
- **Eine Zeile je Datei** (Protokoll + jedes Foto), Kopfdaten in jeder Zeile wiederholt.
- Dateinamen: `QS-<Id>_Protokoll.pdf`, `QS-<Id>_Foto-<nn>.jpg|.png` (nn zweistellig, Reihenfolge Frage dann Aufnahme).
- Datum `yyyy-MM-dd`, Zeit `HH:mm:ss` (Ortszeit).

| # | Spalte | Inhalt |
|---|---|---|
| 1 | `QmTyp` | `Wareneingang` / `Warenausgang` / `Endpruefung` |
| 2 | `PruefungNr` | `QS-<Id>` |
| 3 | `PruefDatum` | Abschlussdatum |
| 4 | `PruefZeit` | Abschlusszeit |
| 5 | `Pruefer` | Benutzername des Prüfers (`CompletedBy`) |
| 6 | `Ergebnis` | `IO` / `NIO` |
| 7 | `Kundenauftrag` | `CustomerOrder` (Bedeutung Rückfrage 6) |
| 8 | `AbNummer` | `AbNumber` (mehrere mit „ + “) |
| 9 | `AbPosition` | `AbPosition` |
| 10 | `Kunde` | `Customer` |
| 11 | `FaNummer` | `OrderNumber` |
| 12 | `Seriennummer` | `SerialNumber` |
| 13 | `Artikelnummer` | `ArticleNumber` |
| 14 | `Artikelbezeichnung` | `ArticleDescription` |
| 15 | `Lieferant` | `SupplierAccount` (Kreditor-Kto) |
| 16 | `LieferantName` | `SupplierName` |
| 17 | `Bestellnummer` | `PurchaseOrderNumber` |
| 18 | `BestellPosition` | `PurchaseOrderPosition` |
| 19 | `Lieferschein` | `DeliveryNoteNumber` |
| 20 | `Dateiname` | Name der Datei dieser Zeile |
| 21 | `Dokumentart` | `Protokoll` / `Foto` |
| 22 | `Beschreibung` | `Prüfprotokoll` bzw. Fragetext des Fotos |

`EnaioCsvWriter` ist rein (Prüfung + Dateiliste rein, String raus) und unit-getestet (Quoting, Umlaute, leere Felder, Reihenfolge).

**T7 Wiederverwendung und Abgrenzung zum vorhandenen enaio-Wissen.**
- Vorhanden ist nur die **Lese**-Richtung: `EnaioDmsSyncService` liest per SQL die View aus `Sync:EnaioDmsViewName` über `EnaioDmsConnection`
  (`EnaioDmsSyncService.cs:39`, `:52-72`) und schreibt `EnaioDmsDocument`; `LinkVorlage.Build` baut Anzeige-Links aus `EnaioLinkVorlage`
  (`LinkVorlage.cs:14-22`, Settings `AppSettingDefinitions.cs:195-199`). Nichts davon schreibt nach enaio.
- Die Übergabe nutzt weder `EnaioDmsConnection` noch die Link-Vorlage; sie hat eigene Schalter mit Präfix `Qs:`. Wiederverwendet werden das
  Lauf-Muster (`RunResilientAsync` + Aktivitäts-Protokoll) und das Schalter-Muster `Sync:EnaioDmsEnabled` (Default aus,
  `ServiceSettingDefinitions.cs:139`).

**T8 Rollen und Zugriff (ADR 0006, Read/Edit-Split, Gate kumulativ ADR 0011).**
- `quality_inspection` – „Qualitätsprüfung“: Prüfungen anlegen, durchführen, Liste/Detail/PDF sehen.
- `quality_inspection_templates` – „Qualitätsprüfung (Vorlagen)“: zusätzlich Vorlagen pflegen, „Erneut übergeben“, fremde Entwürfe löschen; schließt die Prüfrolle ein.
- `admin` darf alles (Wildcard).
- Filter: `[RequireQualityInspectionAccess]` (Class-Level `QualityInspectionsController`), `[RequireInspectionTemplateAccess]` (Class-Level
  `InspectionTemplatesController`, Action-Level `ResetEnaio`), dazu kumulativ `[RequireQualitaetspruefungAktiv]` an beiden Controllern
  (Muster `[RequireFaHierarchyBeschichtungAktiv]`, `secondbrain/codebase/controller.md` Z. 79: MVC → Redirect Home + WarningMessage).
  `ICurrentUserService` bekommt `HasQualityInspectionAccessAsync` / `HasInspectionTemplateAccessAsync` (Muster `CurrentUserService.cs:137-141`).
- **Drei Stellen (CLAUDE.md):** Filter-Attribut, `secondbrain/codebase/controller.md` (Tabelle Filter → Rollen → Controller),
  `Views/Users/RoleOverview.cshtml`. Dazu Rollen-Seed: `RoleKeys.cs`, `defaultRoles` in `Program.cs:216-231`, SQL/122 (Muster
  `SQL/78_AddStockReadRole.sql`) und `00_FreshInstall.sql` (Muster Z. 1607-1616).
- Entwürfe bearbeiten/löschen: Ersteller oder Vorlagen-Rolle. Abgeschlossene Prüfungen: niemand.

**T9 Hilfe.** Hilfeseite: Abschnitt Qualitätsprüfung mit Ablauf, Fragetypen, Bewertungsregel, Fotos, enaio-Übergabe inkl. CSV-Spalten (Memory „Hilfe-Detail-Regel“).

## Migrations-/SQL-Auswirkungen

Jede Etappe mit Schemaänderung: Model → `dotnet ef migrations add` → idempotentes `SQL/<Nr>_*.sql` mit `OBJECT_ID`-Guard (DDL in eigenem Batch) →
`__EFMigrationsHistory`-Insert in eigenem Batch → `SQL/00_FreshInstall.sql` an **zwei** Stellen (Schema + `MigrationId`) (ADR 0004).

| Nr | Etappe | Inhalt |
|---|---|---|
| 122 | E1 | `InspectionTemplates`, `InspectionQuestions` inkl. gefiltertem Unique-Index; Rollen `quality_inspection`, `quality_inspection_templates` (`IF NOT EXISTS`) |
| 123 | E2 | `Inspections` (inkl. enaio-Statusfeldern, damit E4 keine Migration braucht), `InspectionAnswers`, Indizes |
| 124 | E3 | `InspectionFiles` (Spalte `Content varbinary(max) NULL` bzw. `RelativePath` je nach Rückfrage 1) |
| 125–127 | – | Reserve; nicht genutzte Nummern gibt der Dev-Lauf an das Bündel zurück. Dev-Lauf prüft die Nummern vor dem Anlegen erneut. |

Nicht destruktiv (nur neue Tabellen). `SQL/AgentJobs/` nicht betroffen: kein Pflichtfeld/Folge-MERGE an bestehenden Tabellen; keine
Agent-Job-Tabelle wird gelesen oder geschrieben. ServiceSettings und AppSettings brauchen kein SQL – sie werden aus den Katalogen geseedet
(`SettingDefinition.cs:41-44`). DB-Wachstum: siehe Rückfrage 1/3.

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `QualitaetspruefungAktiv` (AppSetting, Gruppe „Qualitätsprüfung“, Anzeigename „Qualitätsprüfung“) | Bool, Lesart `NurTrueAktiviert` | an / aus | `false` (Seed ja) | An: Menü „Qualitätsprüfung“, Prüfmaske, Vorlagen und FA-Zeilenaktion verfügbar. Aus: Seiten leiten auf Home mit Hinweis um, Zeilenaktion ausgeblendet; vorhandene Daten bleiben. | Web: Menü, `QualityInspectionsController`, `InspectionTemplatesController`, FA-Liste | beide | – |
| `Qs:EnaioUebergabeAktiv` (ServiceSetting, Gruppe „Qualitätsprüfung“, Anzeigename „QS-Übergabe an enaio“) | Bool, Lesart `LeerIstDefault` | an / aus | `false` | An: Dienst schreibt je abgeschlossener Prüfung PDF, Fotos und CSV in den Unterordner der Prüfart unter `Qs:EnaioUebergabePfad`. Aus: nichts wird geschrieben, Prüfungen bleiben „Offen“ und werden beim Einschalten nachgeholt. | Dienst: Lauf „QS-Übergabe an enaio“; Liste Qualitätsprüfungen (Status) | beide | „Schreibt Prüfprotokolle, Fotos und CSV-Indexdateien auf die enaio-Freigabe; enaio importiert sie als Dokumente. Beim Einschalten werden alle offenen Prüfungen übergeben.“ |
| `Qs:EnaioUebergabePfad` (ServiceSetting, Anzeigename „QS-Übergabe: Freigabepfad“) | String mit Pfadprüfung | leer oder voll qualifizierter Pfad (UNC `\\server\freigabe\...` oder `X:\...`); Server prüft beim Speichern `Path.IsPathFullyQualified` und ungültige Zeichen | leer | Wurzel der Übergabe; darunter legt der Dienst die Unterordner `Wareneingang`, `Warenausgang`, `Endpruefung` an. Leer = Lauf überspringt mit Warnung. | Dienst: Lauf „QS-Übergabe an enaio“ | je Standort eigener Wert | – (Wirkung über den Schalter) ; Abhängig von `Qs:EnaioUebergabeAktiv` |
| `Cleanup:QsDateienAufbewahrungTage` (ServiceSetting, Anzeigename „QS-Dateien: Aufbewahrung“), E5 | Int, Min 0, Einheit Tage | 0 = nie löschen, sonst ≥ 1 | `90` (Rückfrage 3) | Löscht Foto- und PDF-Inhalte von Prüfungen, die seit mehr als N Tagen übergeben sind; Prüfdaten, Antworten und Dateinamen bleiben. Nicht übergebene Prüfungen werden nie bereinigt. | Dienst: tägliche Bereinigung (`CleanupWorker`, Muster `CleanupWorker.cs:54`) | beide | „Löscht Fotos und Protokolle im WMS endgültig; danach liegen sie nur noch in enaio.“ |

Pfadprüfung: `SettingValidator` kennt für String heute keine Prüfung (`SettingValidator.cs:68-69`). Ergänzt wird ein optionales Feld
`Pfad` (bool) in `SettingDefinition` und ein Zweig im `String`-Fall von `SettingValidator.Validate`. Der Drift-Guard (ADR 0020 Punkt 5)
greift für alle vier Keys (Beschreibung, Verwendung, Standort Pflicht). Kein Knopf „Pfad testen“ in `/ServiceSettings`: die Web-App läuft
unter dem App-Pool-Konto, der Dienst unter seinem Dienstkonto – ein Test aus dem Web wäre irreführend; der Lauf meldet das Ergebnis im
Aktivitäts-Protokoll. Die Unterordnernamen sind keine Einstellung (feste Vorgabe Gerald, F1).

## Audit-Feld-Auswirkungen

Alle fünf neuen Entitäten erben `AuditableEntity`. Web setzt bei jedem Update `ModifiedAt`, `ModifiedBy`, `ModifiedByWindows` aus
`ICurrentUserService`; Dienst (Übergabe, Bereinigung) setzt den Service-Namen. `CompletedAt`/`CompletedBy` zusätzlich als fachliche Felder
(Prüfer), nicht als Ersatz für die Audit-Felder.

## Akzeptanzkriterien

1. Mit `QualitaetspruefungAktiv = false` sind Menü, Vorlagen, Prüfungen und FA-Zeilenaktion nicht erreichbar (Redirect Home + Hinweis).
2. Ohne Rolle `quality_inspection`/`quality_inspection_templates`/`admin` führt jeder Aufruf der Prüfseiten zu AccessDenied; mit
   `quality_inspection` sind Vorlagen nicht erreichbar.
3. Eine zweite aktive Vorlage derselben Prüfart wird beim Speichern abgelehnt (Meldung), in SQL zusätzlich durch den gefilterten Unique-Index.
4. Speichern einer Vorlage erhöht `Version`; eine Frage mit Antworten kann nur deaktiviert, nicht gelöscht werden.
5. Nach Änderung eines Fragetexts zeigt eine vorher abgeschlossene Prüfung (Detail und PDF) weiterhin den alten Text und ihre Vorlagenversion.
6. „Neue Endprüfung“ mit FA-Nummer findet bei AKE die FA, bei IDEAL nur die HauptFA-Zeile; eine stornierte FA wird abgelehnt.
7. Bei IDEAL übernimmt die Endprüfung AB-Nummer(n) aus `FaHierarchyOrderInfo`; bei AKE bleibt das Feld leer.
8. Ohne aktive Vorlage der Prüfart lässt sich keine Prüfung anlegen (Hinweis).
9. Abschluss mit unbeantworteter Pflichtfrage, nicht-numerischem Messwert oder zu wenigen Fotos wird mit Feldmeldung abgelehnt.
10. Ein Messwert außerhalb Min/Max, eine abweichende Ja/Nein-Antwort oder eine n. i. O.-Option setzt das Gesamtergebnis zwingend auf n. i. O.;
    der Prüfer kann es nicht auf i. O. stellen. Bei n. i. O. ist die Bemerkung Pflicht.
11. Abgeschlossene Prüfungen lassen sich nicht mehr ändern (POST auf Edit/UploadPhoto/DeletePhoto → abgelehnt).
12. Foto aufnehmen am Tablet öffnet die Kamera; das Foto erscheint als Vorschaubild; Datei > 10 MB oder Nicht-Bild wird abgelehnt.
13. Ein Foto ist nur über den Controller mit Prüfrolle abrufbar, nicht über eine statische URL.
14. Abschluss erzeugt ein PDF-Protokoll mit allen Antworten, Bewertungen, Vorschaubildern, Ergebnis; ohne Edge wird trotzdem abgeschlossen,
    die Detailseite warnt und bietet „Protokoll erzeugen“.
15. Kommissionierungs-Fotos (Stückliste) funktionieren nach dem Herauslösen von `compressImage` unverändert.
16. Mit `Qs:EnaioUebergabeAktiv = true` und gültigem Pfad schreibt der nächste Lauf je offener Prüfung `QS-<Id>_Protokoll.pdf`, die Fotos und
    zuletzt `QS-<Id>.csv` in `<Pfad>\<QmTyp>`; der Status wird `Uebergeben`. Eine `.csv.tmp` bleibt nicht zurück.
17. Die CSV entspricht T6: UTF-8 ohne BOM, `;`, Kopfzeile, eine Zeile je Datei, korrektes Quoting.
18. Ist der Pfad nicht beschreibbar, wird die Prüfung `Fehler` mit Text, der Lauf erscheint im Aktivitäts-Protokoll unter „QS-Übergabe an enaio“;
    der nächste Lauf versucht es erneut. Prüfungen ohne Protokoll werden übersprungen und gezählt.
19. „Erneut übergeben“ (nur Vorlagen-Rolle) setzt den Status auf `Offen`; der nächste Lauf überschreibt die eigenen Dateien.
20. `/ServiceSettings` lehnt einen relativen Pfad für `Qs:EnaioUebergabePfad` beim Speichern ab.
21. (E5) Nach Ablauf von `Cleanup:QsDateienAufbewahrungTage` sind Inhalte übergebener Prüfungen geleert, Detail zeigt „Datei nur noch in enaio“;
    nicht übergebene Prüfungen bleiben unangetastet; 0 löscht nie.
22. (E5, nur wenn Antwort 4 es zulässt) Verschwindet die CSV nach dem Import aus dem Ordner, setzt der Lauf den Status `Abgeholt`.
23. Liste „Qualitätsprüfungen“ hat Pagination, Filterkarte und Server-Spaltenfilter auf jeder Spalte.

## Test-Szenarien

Neues Kapitel **TS-118 Qualitätsprüfung** in `docs/TESTSZENARIEN.md`, danach `secondbrain/tests/testszenarien-index.md`. Skizze:
- **TS-118.1 Schalter und Rollen** – Vorbedingung: Benutzer ohne/mit je Rolle. Schalter aus/an, Aufrufe, erwartete Redirects/AccessDenied. (AK 1, 2)
- **TS-118.2 Vorlage pflegen** – je Fragetyp eine Frage, Reihenfolge ändern, zweite aktive Vorlage (negativ), Frage mit Antworten löschen (negativ). (AK 3, 4)
- **TS-118.3 Endprüfung AKE** – FA scannen, Fragen beantworten, i. O. abschließen; stornierte FA (negativ). (AK 6, 8)
- **TS-118.4 Endprüfung IDEAL** – HauptFA mit AB-Nummer, Sub-FA-Nummer eingeben (findet HauptFA bzw. keine Sub-FA-Prüfung). (AK 6, 7)
- **TS-118.5 Bewertung** – Messwert außerhalb, falsche Ja/Nein, n. i. O.-Option; Versuch i. O. zu setzen (negativ), Bemerkung leer (negativ). (AK 9, 10)
- **TS-118.6 Fotos** – Tablet-Kamera, Vorschau, Löschen im Entwurf, 11-MB-Datei und PDF-als-Foto (negativ), Foto-URL ohne Anmeldung (negativ). (AK 12, 13)
- **TS-118.7 Protokoll** – PDF-Inhalt prüfen; Edge umbenannt → Warnbanner, „Protokoll erzeugen“. (AK 14)
- **TS-118.8 Vorlagenänderung nach Prüfung** – Text ändern, alte Prüfung zeigt alten Text. (AK 5)
- **TS-118.9 Regression Kommissionierungs-Fotos** (AK 15)
- **TS-118.10 enaio-Übergabe** – Testordner als Pfad, Schalter an, Lauf abwarten, Dateien und CSV prüfen (in Editor: Kodierung, Trennzeichen,
  Umlaute); Ordner schreibgeschützt (negativ, Fehlerstatus, Protokoll); Erneut übergeben; relativer Pfad in `/ServiceSettings` (negativ). (AK 16–20)
- **TS-118.11 Aufbewahrung/Abholung** (E5) – Aufbewahrung 1 Tag mit zurückdatierter Übergabe; CSV manuell entfernen → `Abgeholt`. (AK 21, 22)
- **TS-118.12 Liste** – Pagination, Filterkarte, Spaltenfilter. (AK 23)

Automatisiert: Unit-Tests `InspectionEvaluator`, `EnaioCsvWriter`, Pfadprüfung `SettingValidator`; Repository-Tests (App-Guard eine aktive
Vorlage, HauptFA-Suche); Controller-Tests (Rollen, Entwurf/abgeschlossen, Upload-Prüfung); Dienst-Test Übergabe gegen temporären Ordner
(Status, Dateinamen, `.tmp` → `.csv`); Drift-Guard der Kataloge; DI-Resolution-Test für den neuen Dienst (Muster `BomDiResolutionTests`).
Nicht InMemory-testbar und daher Manual-UAT: gefilterter Unique-Index, Netzwerkfreigabe/Dienstkonto, Kamera, Edge-PDF.

## Etappen (nur bei epic: true)

Ein Worktree, ein Merge am Ende; je Etappe ein Commit, Build + Tests grün. Branch während der Arbeit mit
`scripts/sync-worktree.ps1 -Slug <slug>` auf main-Stand halten. Jede Etappe ist ohne offene Rückfrage baubar; wo eine Antwort fehlt, baut der
Lauf mit der Empfehlung und vermerkt das als Blocker für die Abnahme, statt anzuhalten.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| 1 | **E1 Prüfvorlagen**: `InspectionType`/`QuestionType`, `InspectionTemplate`/`InspectionQuestion`, Migration + SQL/122 (inkl. Rollen), Rollen/Filter/`ICurrentUserService`, AppSetting `QualitaetspruefungAktiv` + Gate, Menü, Vorlagen-Liste/Editor (Foto-Typ noch verborgen), Tests | offen |  |
| 2 | **E2 Prüfung Endprüfung**: `Inspection`/`InspectionAnswer`, Migration + SQL/123, `InspectionEvaluator`, Aufrufvertrag T3, HauptFA-Suche, Prüfmaske/Abschluss, Liste (ADR 0005)/Detail, FA-Zeilenaktion, Tests. Prüfrolle/HauptFA-Regel nach Empfehlung (Rückfrage 7/8) | offen |  |
| 3 | **E3 Fotos + PDF**: `InspectionFile`, Migration + SQL/124, `image-compress.js` herauslösen, Upload/Anzeige/Löschen, Foto-Typ im Editor frei, Protokoll-View + PDF beim Abschluss, „Protokoll erzeugen“. **Abhängig von Rückfrage 1/2** (Speicherort, Grenzen) – ohne Antwort mit Empfehlung (DB) bauen | offen |  |
| 4 | **E4 enaio-Übergabe**: ServiceSettings `Qs:EnaioUebergabeAktiv`/`Qs:EnaioUebergabePfad` + Pfadprüfung, `EnaioCsvWriter`, `QsEnaioUebergabeService` + SyncWorker-Schritt, `SyncLogServices`, „Erneut übergeben“, Tests gegen temporären Ordner. **UAT abhängig von Rückfrage 5/6** (echte Freigabe, CSV-Bestätigung) | offen |  |
| 5 | **E5 Aufbewahrung + Abholung**: `Cleanup:QsDateienAufbewahrungTage` im `CleanupWorker`; Abhol-Erkennung `Abgeholt`. **Abhängig von Rückfrage 3/4** – ohne Antwort zu 4 nur Aufbewahrung bauen, Abhol-Erkennung als Blocker vermerken | offen |  |
| 6 | **E6 Abschluss**: Hilfe, README (AppSettings/ServiceSettings), `RoleOverview`, TS-118 + Testindex, Version 1.86.0 in beiden `AppVersion.cs`, Anwender-Changelog, Brain (feature-map, controller.md, services.md, integrationen.md, Changelog-Note) | offen |  |

**Reihenfolge im Bündel:** Dieses Epic E1–E2 vor den Andock-Etappen von [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]] und
[[2026-11-08-versandmasken-geraete-fas-spec]] – erst dann existiert der Aufrufvertrag T3. Die anderen Epics können ihre eigenen Masken parallel
bauen und docken in ihrer letzten Etappe nur per Link an (Prüfart `Wareneingang` bzw. `Warenausgang` + Bezugsfelder). E3–E6 sind von ihnen
unabhängig.

## Deploy

*Vorläufig – der qa-agent bestätigt gegen den echten Diff.*

- **Web-App:** ja (Controller, Views, JS, Rollen, AppSetting, Settings-Validierung).
- **Service:** ja (Übergabe-Lauf, Bereinigung, ServiceSettings-Katalog).
- **Migration:** ja – SQL/122, 123, 124 vor dem Web-Deploy einspielen (nur neue Tabellen, nicht destruktiv; trotzdem DB-Backup wie üblich).
- **Reihenfolge:** SQL → Web → Dienst. Schalter bleiben aus. Vor dem Einschalten von `Qs:EnaioUebergabeAktiv`: Freigabe anlegen, Dienstkonto
  mit Ändern-Recht, Import-Aktion in enaio durch den Betreuer eingerichtet, Pfad in `/ServiceSettings` gesetzt.
- **Standort:** beide; IDEAL nur Prüfung, Übergabe erst nach Antwort 5.
- **Publish-Befehle:**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

## Zusatzfunde (gemeldet, nicht mitgebaut)

- **Z1 Kommissionierungs-Fotos öffentlich abrufbar.** `PhotoController` speichert unter `wwwroot/Fotos/Kommissionierung`
  (`PhotoController.cs:30-45`). Die Login-Middleware lässt jeden Pfad mit Punkt durch (`Program.cs:429`), `UseStaticFiles` liefert danach aus
  (`Program.cs:447`) – ein Foto ist also ohne Anmeldung über `/Fotos/Kommissionierung/<Datei>.jpg` erreichbar. Außerdem prüft
  `PhotoController.Delete` nur den Dateinamen, nicht den FA-Bezug (`PhotoController.cs:81-96`). Deshalb liegen QS-Fotos nie unter `wwwroot`.
  Eigene Aufgabe empfohlen.
- **Z2** `SettingValidator` prüft String-Pfade nicht; `BriefkopfVorlagePfad` (`AppSettingDefinitions.cs:186`) könnte das neue Feld `Pfad` später
  ebenfalls nutzen – nicht Teil dieses Epics.

## Offene Rueckfragen

1. **Speicherort Fotos/Protokoll bis zur Übergabe – Datenbank oder Dateisystem?** Empfehlung **Datenbank** (`varbinary(max)`, nach Aufbewahrung
   geleert): Web und Dienst brauchen keinen gemeinsamen Pfad (dass beide auf demselben Server laufen, ist nirgends belegt – README nennt nur
   „auf den Zielserver kopieren“, `README.md:594`), Backup und Transaktion sind einheitlich, nichts landet unter `wwwroot` (Z1). Größe: bei
   ca. 0,3–0,5 MB je Foto bleibt die DB durch die Aufbewahrung (Frage 3) begrenzt. Dateisystem wäre nur bei sehr vielen Fotos/Tag besser.
2. **Fotogröße/Kompression.** Empfehlung wie Kommissionierung: Breite max. 1920 px, JPEG-Qualität 0,75 (`photo-upload.js:29-32`, `:53`);
   Server lehnt > 10 MB und Nicht-Bilder ab; höchstens 20 Fotos je Prüfung.
3. **Aufbewahrung.** Empfehlung: Prüfdaten dauerhaft; Foto-/PDF-Inhalte 90 Tage nach erfolgreicher Übergabe löschen (Einstellung, 0 = nie).
4. **enaio-Verhalten nach dem Import** (verschieben, löschen, Fehlerordner?) – vom enaio-Betreuer. Empfehlung: Erfolg = „Übergeben“ (geschrieben);
   „Abgeholt“ nur, wenn enaio die CSV entfernt; einen Fehlerordner liest das WMS erst, wenn sein Name bekannt ist.
5. **Freigabepfad je Standort und Dienstkonto.** Welcher UNC-Pfad, welches Konto (Empfehlung: das Dienstkonto des WMS-Dienstes mit Ändern-Recht
   auf genau diese Freigabe)? Hat IDEAL eine enaio-Freigabe/-Instanz? Bis zur Antwort bleibt die Übergabe bei IDEAL aus.
6. **CSV-Bestätigung durch den enaio-Betreuer**: Spalten T6, UTF-8 **ohne** BOM, Dateinamen, eine Zeile je Datei. Und: was ist „Kundenauftrag“
   in Geralds Liste neben „AB-Nummer“ – die Bestellnummer des Kunden? Bis zur Antwort wird `Kundenauftrag` nur gefüllt, wenn ein Aufrufer sie
   liefert (Versand-Epic, Feld „Bestellnummer Kunde“); bei der Endprüfung bleibt sie leer.
7. **Rollen.** Empfehlung: zwei neue Rollen `quality_inspection` (prüfen) und `quality_inspection_templates` (Vorlagen, Erneut übergeben), statt
   Prüfen an `stock`/`picking`/`fa_completion` zu hängen – Prüfer kommen aus Lager, Versand und Montage.
8. **Endprüfung bei IDEAL nur am HauptFA (komplettes Gerät)?** Empfehlung ja (`FaHierarchyNode.cs:17`: HauptFA = Fertigungsauftragsnummer des
   kompletten Geräts).
9. **Seriennummern.** Das WMS führt keine (kein Feld in `IdealAkeWms/Models`, Suche nach „Seriennummer“/„SerialNumber“ ohne Treffer). Gibt es
   sie in Sage je Gerät? Empfehlung: jetzt optionales Eingabe-/Scanfeld; Sage-Anbindung als Folgearbeit. Prüfschritt für den Menschen:
   Sage-Betreuer/DPS fragen, ob und in welcher Tabelle Seriennummern je FA/Gerät geführt werden – keine Annahme im WMS.
10. **Vorlagen je Artikelkategorie/Gerätetyp.** Empfehlung: nicht in diesem Epic; eine aktive Vorlage je Prüfart. Hinweis: `Article.ArticleCategoryId`
    (`Article.cs:46`) wird im Web nur manuell gepflegt (`ArticlesController.cs:188`); ob die Kategorien bei IDEAL gefüllt sind, ist ungeprüft.
11. **Folgeprozess bei n. i. O.** Empfehlung: in diesem Epic nur dokumentieren (Ergebnis + Pflicht-Bemerkung + Fotos); Sperrlager/Reklamation/
    Nacharbeit als eigener Backlog-Eintrag.

## Freigabe-Nachtrag 2026-11-07 19:17 (Gerald, geht dem Rumpf vor)
- Alle Rückfragen **wie empfohlen** (Antworten unten), Abweichungen nur wo dort vermerkt.
- **Etappe E0 vor E1:** Alle Prüfabfragen dieser Spec, die lesend am AKE-Test möglich sind (SData nur GET mit `orderBy`; Sage-SQL bzw.
  WMS-DB nur SELECT), führt der Dev selbst aus und trägt die Ergebnisse ein. **Weicht ein Ergebnis von der Spec-Annahme ab: Blocker melden,
  nicht raten** (Muster B1 vom 07.11.). Prüfabfragen, die IDEAL-Systeme oder Menschen brauchen, bleiben offen; betroffene Teile hinter
  Schalter/Standort aus.
- **Etappen, die an Sage schreiben, bleiben blockiert und aus** (laut Bündel-Übersicht). Keine Schreibtests an Sage in diesem Lauf.
- Worktree laut Lauf-Auftrag (Empfehlung Ü1: nach Merge des Bündels frisch von `main`).

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Speicherort Fotos/Protokoll (DB / Dateisystem) → **wie empfohlen (Gerald 2026-11-07 19:17)**
2. Fotogröße/Kompression (1920 px, 0,75, 10 MB, 20 Fotos) → **wie empfohlen (Gerald 2026-11-07 19:17)**
3. Aufbewahrung (90 Tage nach Übergabe, Prüfdaten dauerhaft) → **wie empfohlen (Gerald 2026-11-07 19:17)**
4. enaio-Verhalten nach Import / Fehlerordner → **Gerald 2026-11-07 19:17: Gerald klärt mit dem enaio-Betreuer; bis dahin Erfolgserkennung konfigurierbar (Einstellung nach ADR 0020), Übergabe-Schalter Default aus.**
5. Freigabepfad je Standort, Dienstkonto, IDEAL-enaio → **Gerald 2026-11-07 19:17: Pfad und Dienstkonto als Einstellung je Standort, Default leer = Übergabe aus; IDEAL aus bis beantwortet.**
6. CSV-Bestätigung enaio-Betreuer; Bedeutung „Kundenauftrag“ → **Gerald 2026-11-07 19:17: CSV-Format wie in der Spec; Bestätigung durch den enaio-Betreuer holt Gerald, bis dahin Übergabe aus. „Kundenauftrag“ = AB-Nummer laut Spec-Empfehlung.**
7. Zwei neue Rollen → **wie empfohlen (Gerald 2026-11-07 19:17)**
8. Endprüfung IDEAL nur am HauptFA → **wie empfohlen (Gerald 2026-11-07 19:17)**
9. Seriennummern (Feld jetzt, Sage später) → **wie empfohlen (Gerald 2026-11-07 19:17)**
10. Vorlagen je Artikelkategorie (nicht in diesem Epic) → **wie empfohlen (Gerald 2026-11-07 19:17)**
11. n. i. O.-Folgeprozess (nur dokumentieren) → **wie empfohlen (Gerald 2026-11-07 19:17)**
