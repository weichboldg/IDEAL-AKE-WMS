---
type: spec
title: Blechfertigung – BF-Aufträge (Warenkorb, Schachtler-Freigabe, WiCAM-CSV, Fertigmeldung Stufe 1)
slug: 2026-11-13-blechfertigung-bf-auftraege-wicam-csv-spec
status: Entwurf
created: 2026-11-17
updated: 2026-11-17
source_backlog: "[[2026-11-13-blechfertigung-bf-auftraege-wicam-csv]]"
task: ""
worktree: ""
branch: ""
standort: IDEAL
affected_code:
  - IdealAkeWms/Models/BfAuftrag.cs (neu)
  - IdealAkeWms/Models/BfAuftragPosition.cs (neu)
  - IdealAkeWms/Models/BfAuftragStatus.cs (neu)
  - IdealAkeWms/Data/ApplicationDbContext.cs
  - IdealAkeWms/Data/Repositories/BfAuftragRepository.cs (neu, mit Interface)
  - IdealAkeWms/Services/BfAuftragWorkflow.cs (neu – Nummer, Freigabe, Termine, Bemerkung, Prüfungen)
  - IdealAkeWms/Services/BfArtikelFelder.cs (neu – Lesen der Merkmale/Bestellmenge für Warenkorb)
  - IdealAkeWms/Controllers/BfAuftraegeController.cs (neu)
  - IdealAkeWms/Controllers/BfAuftraegeApiController.cs (neu – Artikelsuche Warenkorb)
  - IdealAkeWms/Views/BfAuftraege/ (neu – Index, Warenkorb, Details)
  - IdealAkeWms/Filters/RequireSchachtlerAccessAttribute.cs (neu)
  - IdealAkeWms/Filters/RequireBlechfertigungAktivAttribute.cs (neu)
  - IdealAkeWms/Models/RoleKeys.cs
  - IdealAkeWms/Services/CurrentUserService.cs (+ ICurrentUserService)
  - IdealAkeWms/Views/Users/RoleOverview.cshtml
  - IdealAkeWms/Models/AppSettingDefinitions.cs (+ AppSettingKeys)
  - IdealAkeWms/Models/ServiceSettingDefinitions.cs
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs
  - IdealAkeWms/Views/Shared/_Layout.cshtml (Menüeintrag)
  - IdealAkeWms/Services/SyncLogger/SyncLogServices.cs
  - IDEALAKEWMSService/Services/BfCsvService.cs (neu)
  - IDEALAKEWMSService/Services/BfAuftragEmailService.cs (neu)
  - IDEALAKEWMSService/Workers/SageBookingWorker.cs
  - IDEALAKEWMSService/Workers/SyncWorker.cs
  - IDEALAKEWMSService/Program.cs
  - IDEALAKEWMSService/Services/Sdata/SdataArtikelReader.cs (nur bei Rückfrage 1 = V-B)
  - IDEALAKEWMSService/Services/SageImportService.cs (nur bei Rückfrage 1 = V-B)
  - IdealAkeWms/Migrations/<neu>_AddBfAuftraege.cs
  - SQL/<nnn>_AddBfAuftraege.sql (voraussichtlich 156)
  - SQL/00_FreshInstall.sql
  - scripts/deploy/nachlauf.psd1
  - docs/TESTSZENARIEN.md
  - IdealAkeWms/Views/Help/ (Hilfeseite + Changelog)
open_questions:
  - "1. Datenquelle der Artikelfelder an IDEAL (IstLagerartikel, Artikeltyp, Material/Materialnummer, FertigungsInfoPN, Sage-Bestellmenge): V-A P-R1 zuerst aufheben, V-B schmaler SData-Nachlese-Schritt im View-Modus, V-C Artikel-View in Sage erweitern? Empfehlung V-B."
  - "2. Materialnummer aus dem Sage-Feld USER_OSMaterialnummerPN statt einer WMS-Zuordnungstabelle Material -> Nummer? Empfehlung Sage-Feld, Tabelle nur, wenn P-3 Luecken zeigt."
  - "3. Bezugstag fuer Schachteltermin, FE-Termin und TT.MM in der Bemerkung: Abschicktag des Bestellers oder Freigabetag des Schachtlers? Empfehlung Freigabetag."
  - "4. Bemerkung: Status als Etikett vorne vor die Excel-Formel setzen ('EILT - FertigungsInfoPN - TT.MM - Pos')? Empfehlung ja."
  - "5. Freigabe und Fertigmeldung je BF-Auftrag (Schachtler kann vorher Positionen streichen, Menge/Lagerplatz aendern) statt je Position? Empfehlung je Auftrag."
  - "6. Wer darf bestellen (Warenkorb)? Empfehlung dieselben Rollen wie die Lagerbestellung (CanOrderLagerAsync) plus schachtler und admin."
  - "7. Rolle Schachtler: Key schachtler, Anzeigename 'Schachtler (Blechfertigung)' - passt? Personen (Ivica Baresic, Rainer Kopp) bestaetigt Andi; Zuweisung ist Betrieb, nicht Code."
  - "8. Exakter CSV-Aufbau (Spalte der Kopfschluessel A oder B, Leerzeilen 10/12, deutsche Ueberschrift Zeile 13 ja/nein, Zeilenende, Trenner am Zeilenende): aus einer echten Datei im Auftragspool (P-1) uebernehmen?"
  - "9. Trennzeichen ';', Kodierung Windows-1252 und die zwei Datumsformate fest im Code statt als Einstellung? Empfehlung fest."
  - "10. Mengenregel bei Artikeln mit Basismengeneinheit ungleich Lagermengeneinheit (BV-1/P-N1): Vielfaches dann frei lassen oder Freigabe sperren? Empfehlung: nach P-4 entscheiden, Default frei mit Hinweis."
  - "11. Storno: Besteller bis zur Freigabe, Schachtler bis zur Fertigmeldung (nach Freigabe nur WMS-Status, die CSV liegt schon im Pool)? Empfehlung ja."
  - "12. Ersatz der Ablage 'Bestellungen-Auswertung': reichen Auftragsliste und Positionstabelle im Detail mit CSV-Export, oder braucht es eine eigene Positionsliste ueber alle Auftraege? Empfehlung: Stufe 1 ohne eigene Positionsliste."
  - "13. Mail an die Empfaengergruppe nur beim Abschicken (wie Lagerbestellung) - keine Mail an den Besteller bei Freigabe/Fertig? Empfehlung ja."
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

> [!info] Nummern nicht festgelegt
> Version, SQL-Nummer, TS-Nummer und ADR-Nummer **beim Dev-Start gegen Worktree, `specs/` und `aufgaben/` prüfen**. Stand beim
> Schreiben (2026-11-17): Worktree `AppVersion.Version = "1.99.31"` (`IdealAkeWms/AppVersion.cs:5`); Manifest bis `SQL/155`
> (`scripts/deploy/nachlauf.psd1:101`), 151 und 153/154 vergeben bzw. reserviert; `docs/TESTSZENARIEN.md` bis TS-155 (`:12690`), TS-152
> reserviert die BDE-Teileliste; höchstes ADR 0029, 0030 reserviert die BDE-Teileliste. Die BDE-Teileliste
> ([[2026-11-13-bde-vereinfacht-teileliste-blechfertigung-spec]]) beansprucht SQL/154 und die nächste freie Version.
> **Voraussichtlich:** Version nach der BDE-Teileliste (z. B. v1.99.33), **SQL/156**, **TS-156**, ggf. ADR 0031.

## Ziel / Nutzen (das Warum)

Die Produktion bei IDEAL bestellt Blechteile (Lagerteile: Baugruppen BG und Einzelteile FT) heute über die Excel
`VL_KO-014_ALOOP-Auftragspool-Lagerteile-füllen-WMS.xlsm`. Ein Makro legt je Bestellung eine CSV in den WiCAM-Auftragspool
(`N:\u\pn\apools\`). Das WMS übernimmt diesen Weg: Warenkorb wie die Lagerbestellung, Prüfung und Freigabe durch den Schachtler, CSV durch
den Dienst, Fertigmeldung als Status. Der BF-Auftrag ist in **Stufe 1** der interne Auftrag mit eigenem Nummernkreis – **ohne Sage-Schreiben**.
Das Feld „Sage-FA“ bleibt für Stufe 2 leer vorgesehen, damit Stufe 2 nichts umbaut.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope (nur IDEAL, Schalter Default aus):**
- Neue Entitäten `BfAuftrag` + `BfAuftragPosition` (AuditableEntity), Nummernkreis `BF` + 8 Ziffern, vergeben beim Abschicken.
- Warenkorb (Entwurf) mit Artikelsuche nur über Lagerartikel (IstLagerartikel ≠ 0, Artikeltyp BG/FT), Vorbelegung Menge/Lagerplatz,
  Mengenregel „ganzes Vielfaches der Sage-Bestellmenge, leer/0 = frei“, Status-Auswahl „kein Status“/EILT/weitere aus Einstellung.
- Abschicken → Nummer, Mail an Empfängergruppe (Dienst).
- Liste der BF-Aufträge (ADR 0005) mit CSV/Excel-Export; Detailseite.
- Neue Rolle `schachtler`: Prüfen (Menge/Lagerplatz/Position streichen), Freigeben (= an WiCAM übergeben), Fertig melden, Stornieren.
- CSV je BF-Auftrag durch den Dienst in den Auftragspool (UNC), Kopf wie die Excel, Spalten A–H, `;`, Windows-1252, Wiederholung,
  „CSV erneut schreiben“ bei Fehler.
- Einstellungen nach ADR 0008/0020 (Gate, Empfängergruppe, Status-Werte, Arbeitstage, WiCAM-Kopfwerte, Pfad, Wiederholung, Mail).
- Datenquelle der Artikelfelder an IDEAL – je nach Rückfrage 1 (Empfehlung V-B, Abschnitt „Datenquelle“).

**Out-of-Scope:**
- Sage-Schreiben (Stufe 2: `FertigungsauftragService` oder `InterneFertigungBuchungService`, eigene Spec + Schreibtest).
- Lagerbuchung bei Fertigmeldung (Andi: „keine automatische Einbuchung“, Backlog Antwort 8).
- BDE-Fertigmeldung am letzten Arbeitsgang. Grund: Kürzel ohne Sage-Arbeitsgang (ZS 65, MT 20, MK 7, ML 6, ISO 4, PG 4, T 2, SW 2, EP 1;
  [[2026-11-17-pruefabfragen-gerald]]) und die BDE-Teileliste schließt BF/WiCAM ausdrücklich aus
  ([[2026-11-13-bde-vereinfacht-teileliste-blechfertigung-spec]], Out-of-Scope).
- Ablage im Ordner `…\Bestellungen-Auswertung` (Walter 17.11.: nicht mehr nötig).
- WiCAM-Rückmeldung, Ressourcenlisten der BG.
- CSV-Kopfwerte aus Sage (Gerät/Arbeitsschritte/Abteilung): PN füllt sie beim Import (Walter 17.11.), das WMS schreibt die Platzhalter.

## Fachliche Anforderungen

**F1 Warenkorb (Besteller).** Ein Entwurf je Benutzer (Muster Lagerbestellung). Position = fortlaufend ab 1. Je Position:

| Feld | Quelle / Regel |
|---|---|
| Artikel | Suche nur Artikel mit IstLagerartikel ≠ 0 und Artikeltyp ∈ {BG, FT} (ZK nie, Backlog Antwort 3) |
| Matchcode | `Article.Matchcode` (Snapshot) |
| Teileart | Artikeltyp (BG/FT, Snapshot) |
| Material / Materialnummer | Material-Text + Nummer (Rückfrage 2); ohne Nummer Warnung im Warenkorb, Freigabe gesperrt |
| FertigungsInfoPN | Merkmal `USER_OSFertigungsInfoPN` (Snapshot) |
| Hauptlagerplatz | vorbelegt mit dem Artikel-Hauptlagerplatz, änderbar (Auswahl aus aktiven Lagerplätzen) |
| Menge | vorbelegt mit Sage-Bestellmenge; Pflicht > 0; ist die Bestellmenge > 0, nur ganzes Vielfaches (Server prüft); leer/0 → frei |
| Status | Auswahl „kein Status“ + Werte aus `BfStatuswerte` (Default „EILT“) |
| Besteller / Bestelldatum | Login / Abschicktag (am Kopf) |

**F2 Abschicken.** Mindestens eine Position; Empfängergruppe gesetzt (sonst gesperrt mit Hinweis, wie `WarehouseRequisitionsController.Submit`);
Nummer `BF00000001` ff. aus eigener Folge; Status `Bestellt`; Mail an die Gruppe (Dienst).

**F3 Prüfen/Freigeben (Schachtler).** Im Status `Bestellt`: Menge (Mengenregel bleibt), Hauptlagerplatz ändern, Position streichen.
„Freigeben“ prüft serverseitig: jede Position hat Materialnummer, Matchcode, Hauptlagerplatz; kein Textfeld enthält `;`, `"`,
Zeilenumbruch oder ein Zeichen außerhalb Windows-1252 (Melden statt still ersetzen). Dann berechnet der Server Schachteltermin und FE-Termin,
schreibt sie samt Bemerkung je Position fest, Status `Freigegeben`, CSV-Status `Offen`.

**F4 CSV (Dienst).** Eine Datei je BF-Auftrag, Name `<BF-Nummer>.csv`, Inhalt siehe „CSV-Aufbau“. Fehler → CSV-Status `Fehler` mit Text,
automatische Wiederholung wie die Wareneingang-CSV; Namenskollision → keine automatische Wiederholung; Schachtler kann „CSV erneut schreiben“.

**F5 Fertig melden (Schachtler).** Im Status `Freigegeben` (CSV geschrieben) per Knopf in der Liste (Auswahl mehrerer Aufträge per Checkbox)
oder im Detail → `Fertig`. Nur Status, keine Lagerbuchung.

**F6 Storno** nach Rückfrage 11. **F7 Liste** mit Filtern und Export (Abschnitt „Listen-View“).

## Technischer Loesungsentwurf

### Ist-Stand (belegt)

**Artikelfelder an IDEAL – heute nicht im WMS (Kernbefund):**
- Der Artikel-Abgleich an IDEAL läuft über die View. `SageArticleSql.BuildViewSql` liest fest sieben Spalten: ArticleNumber, Description,
  Unit, ArticleGroup, ReorderLevel, PrimaryStorageLocation, Matchcode (`IDEALAKEWMSService/Services/SageArticleSql.cs:61-73`). Der View-Reader
  liest dieselben (`SageArtikelReader.cs:20-60`). Keine Bestellmenge, keine `USER_OS*`.
- Benutzerfelder (als Text-Merkmale) und `ArticleSageDetails` (u. a. `Bestellmenge`, `Models/ArticleSageDetail.cs:70`) schreibt der Abgleich
  **nur** für SData-Zeilen: `SageImportService.cs:558-563` (`Details is not null`) und `:566-572` (`Benutzerfelder is not null`);
  `ArtikelBenutzerfelderWriter.cs:26` nimmt nur Artikel mit `Benutzerfelder != null`.
- SData für Artikel ist an IDEAL gesperrt, solange `Sync:ArticlesViewName` gesetzt ist (`ArtikelQuelleSwitch.cs:41-49`, TODO P-R1), und das
  Manifest verlangt dort den View-Namen (`nachlauf.psd1:171-174`, `Pruefung/ArticlesViewName`).
- `FaHierarchyNode` trägt `Matchcode`, `Hauptlagerplatz`, `FertigungsInfoPN`, `Artikeltyp`, `Material` (`Models/FaHierarchyNode.cs:32-43`),
  aber nur für Knoten in einer FA-Struktur, und IDEAL-Prod hat 0 Knoten ([[2026-11-17-pruefabfragen-gerald]]). Für **freie** Bestellung
  ungeeignet. `IstLagerartikel`/`MaterialnummerPN` kommen im Code nirgends vor (Suche `IstLagerartikel|MaterialnummerPN` in `*.cs,*.sql,*.cshtml,
  *.js,*.psd1`: kein Treffer; `USER_OSAbteilung` nur im FA-Struktur-Reader `SdataFaStrukturReader.cs:149,159,242`).
- In Sage am Artikel gepflegt und per SData an IDEAL gelesen (L10, [[sage-sdata-benutzerfelder-lesetest]] :1063-1084): `USER_OSIstLagerartikel`
  (`ne 0` → 1.263 Artikel, FT 988, BG 274), `USER_OSArtikeltyp`, `USER_OSArbeitsschritte`, `USER_OSMaterial`, `USER_OSMaterialnummerPN`,
  `USER_OSAbteilung`, `USER_OSFertigungsInfoPN`. Beispiel Unter-FA 1065143: `OSMaterial 4301 BLANK, OSMaterialnummerPN 3` (:1094) – passt
  zur Excel-Liste (3 = 4301 BLANK). Filter `ne ''` liefert wegen NULL-Semantik 0 Zeilen (`SdataArtikelReader.cs:66`).
- Wiederverwendbar: `SdataArtikelReader.BenutzerfelderLesenAsync` (`:125-176`, internal, prüft unbekannte Felder einzeln, schon vom
  FA-Struktur-Reader genutzt, `SdataFaStrukturReader.cs:111`), `InBloeckenAsync` (`:297`, privat), `SelectVarianten` mit `Bestellmenge`
  (`:56-58`), `ArtikelBenutzerfelderWriter.SchreibenAsync` (ganze Datei: Definition je Feld über `SyncSource="Sage"` + `SyncFieldName`,
  leerer Wert entfernt die Zeile `:85-91`, Audit nur bei Änderung `:103-110`, „Feld diesmal nicht gelesen = unberührt“ `:75`).
- `Article.Matchcode`, `PrimaryStorageLocationId`, `SagePrimaryStorageLocation` kommen aus der View (`SageImportService.cs:463-500`).

**Muster, die wiederverwendet werden:**

| Baustein | Beleg | Verwendung im BF-Auftrag |
|---|---|---|
| Warenkorb/Entwurf, Abschicken, Empfängergruppe, RowVersion | `WarehouseRequisition` + `WarehouseRequisitionsController.Submit` (`:191-244`: Eigentümer, nur Draft, ≥1 Position, Gruppe aus AppSetting sonst gesperrt, `DbUpdateConcurrencyException` → WarningMessage) | Ablauf F1/F2 gleich; eigene Entität, weil Freigabe/CSV/Fertig/Nummer anders sind (kein dritter `WarehouseRequisitionType`, `WarehouseRequisitionType.cs`: Lager = 1, Glas = 2) |
| Liste mit Filter + Pagination | `WarehouseRequisitionsController.Index` (`:35-114`: ColumnMap, `ColumnFilterHelper.Apply`, `PageSize`, `PaginationState`) | Index |
| Empfängergruppe als Stammdatenverweis | `AppSettingDefinitions.cs:156-161` (`M(..., SettingStammdaten.Bestellempfaengergruppe)`), Validierung `Program.cs:381`, `SettingsController.cs:88` | `BfEmpfaengergruppeId` |
| Mail an Gruppe aus dem Dienst | `WarehouseRequisitionEmailService` (Gruppe → `Recipients.Where(IsActive)`, `IMailService.SendAsync`, `MarkEmailSentAsync`), Aufruf `SyncWorker.cs:153-186` mit Schalter | `BfAuftragEmailService`, Schalter `Sync:BfAuftragEmailEnabled` |
| Nummernkreis mit UNIQUE-Index + Wiederholung | `Nummernkreis.MitVersuchenAsync`, `IstSqlFehler(ex, 2601, 2627)`; Anwendung `BeschichtungsauftragWorkflow.cs:377-398` (MAX+1, serialisierbar) | BF-Nummer; **nicht** `Nummernkreis.Format` (liefert `PRAEFIX-JJJJ-NNNNN`), sondern `$"BF{lfd:D8}"` |
| Arbeitstage + Feiertage | `BusinessDayService.AddBusinessDays(date, days, holidays)` (`Services/BusinessDayService.cs:29-50`: Mo–Fr, Feiertage aus, Starttag zählt nicht = Excel `WORKDAY`); `IHolidayRepository.GetHolidayDatesAsync` (`HolidayRepository.cs:17-21`, Decorator `CachedHolidayRepository`, `Program.cs:96`) | Termine |
| CSV durch den Dienst auf UNC | `WareneingangCsvService` (Status Offen/Geschrieben/Fehler, `Versuche`, Mindestabstand, max. 10 Dateien je Takt, `.tmp` + `File.Move(…, overwrite:false)`, Kollision = keine Wiederholung, SyncLog-Counts `geschrieben`/`fehler`); Takt `SageBookingWorker.cs:104-118` (eigenes try), Einstellungen `:207-218`; Registrierung `IDEALAKEWMSService/Program.cs:156`; `WareneingangCsv.PfadMuster` (UNC-Regex) | `BfCsvService` als eigener Schritt im `SageBookingWorker` mit eigenem Schalter (nicht an `sageWeAn` gebunden) |
| „CSV erneut schreiben“ | `PurchaseOrdersController.SageWe.cs:161`, `PurchaseOrderReceiptRepository.CsvErneutSchreibenAsync` (`:159`) | Aktion Schachtler |
| Excel-Export | `wwwroot/js/csv-export.js` hängt sich an jede `table.filterable-table`, Server-Mode lädt mit `pageSize=0` nach (Deckel 5000), `;` + UTF-8-BOM | Liste + Detail ohne neuen Code |
| Rolle + Gate | `RequireEinkaufAccessAttribute` → `CanAccessEinkaufAsync` = `HasAnyRoleAsync(Admin, Einkauf)` (`CurrentUserService.cs:113-114`); `RequireEinkaufAktivAttribute` (AppSetting genau "true", sonst WarningMessage + Redirect); Rolle per Manifest-Reparatur (`nachlauf.psd1:142-149`), nicht im Seed (`Program.cs:232-244`) | `schachtler`, `RequireSchachtlerAccess`, `RequireBlechfertigungAktiv` |
| Bestellrollen | `CanOrderLagerAsync` = Admin, Picking, Stock, StockKeyUser, Lagerbestellung (`CurrentUserService.cs:117-119`) | Bestellen (Rückfrage 6) |

### Warum der Dienst die CSV schreibt (nicht die Web-App)
Der Pool liegt auf einer Freigabe (`\\idealcam.ideal.ideal-ake.at\fertigung\u\pn\apools\`, Backlog „CSV“). `WareneingangCsv.PfadMuster`
dokumentiert, dass der App-Pool unter einem anderen Konto läuft als der Dienst; das Schreibrecht wird für **ein** Konto (Dienst) eingerichtet,
wie bei der Wareneingang-CSV. Dazu kommen Wiederholung und Fehlerstatus ohne Benutzer-Wartezeit. Die Web-App berechnet nur Termine und
Bemerkung bei der Freigabe und schreibt sie fest; der Dienst formatiert nur (keine Feiertagslogik im Dienst).

### Datenquelle der Artikelfelder (Rückfrage 1)
- **V-A – P-R1 zuerst:** eigener Folge-Spec hebt die Artikel-Sperre an IDEAL auf (Weg in [[2026-11-16-fa-struktur-ideal-ueber-sdata-spec]]
  :274-286), dann kommen Merkmale und `SageDetail.Bestellmenge` aus dem normalen Abgleich. Sauberste Lösung, aber BF hängt an P-R1 (Filter der
  IDEAL-Artikel-View belegen, Vergleichslauf, Umschalten des produktiven Lager-Abgleichs).
- **V-B – schmaler Nachlese-Schritt (Empfehlung):** im Artikel-Abgleich, nur wenn die Quelle View ist und `Sync:BlechteileFelderNachlesen` an:
  eine SData-Abfrage `Artikel` mit `where USER_OSIstLagerartikel ne 0`, `orderBy Artikelnummer` (L10 belegt 1.263 Zeilen), Felder
  `USER_OSIstLagerartikel, USER_OSArtikeltyp, USER_OSMaterial, USER_OSMaterialnummerPN, USER_OSFertigungsInfoPN` (+ `USER_OSArbeitsschritte`,
  `USER_OSAbteilung` für spätere BDE); dazu `Bestellmenge` aus `ArtikelVariantenExtended` in Blöcken (`InBloeckenAsync` → internal, Varianten-Wahl
  wie `SdataArtikelReader.cs:291`). Schreiben über den vorhandenen `ArtikelBenutzerfelderWriter` (Bestellmenge als Merkmal mit
  `SyncFieldName = "Bestellmenge"`). Artikel, die ein Merkmal `IstLagerartikel` tragen, aber nicht mehr geliefert werden, bekommen leere Werte →
  Writer entfernt sie (`:85-91`, sonst blieben Altwerte stehen). Info-Zeile im Lauf `Article`, Warnungen des Writers (Melden statt still).
  Fällt später P-R1, schaltet man den Schalter aus; `BfArtikelFelder` liest Bestellmenge dann aus `SageDetail`, sonst aus dem Merkmal (eine Stelle).
  Risiko: ein SData-Fehler darf den View-Abgleich nicht brechen → eigener try, Warnung, Lauf bleibt erfolgreich.
- **V-C – Sage-View erweitern:** Felder in die IDEAL-Artikel-View aufnehmen und `BuildViewSql` + Upsert erweitern. Braucht eine Sage-seitige
  Änderung am produktiven Lager-Abgleich und neue Spalten/Writer im WMS – mehr Code als V-B, und die View-Abhängigkeit soll ohnehin weg (ADR 0029).

Der Rest der Spec ist von der Variante unabhängig: der Warenkorb liest über `BfArtikelFelder` (Merkmale per `SyncSource="Sage"` +
`SyncFieldName`, Bestellmenge `SageDetail` vor Merkmal). Die Artikelsuche filtert in SQL (Join auf `ArticleAttributeValues`), nicht im
Speicher wie `ArticlesApiController.Search` (`:34-41` sucht `limit*5` und filtert danach – bei 1.263 von vielen Artikeln fehlen sonst Treffer).

### Datenmodell (neu)
`BfAuftrag : AuditableEntity` – `Id`; `Nummer nvarchar(10) NULL` (UNIQUE gefiltert `WHERE Nummer IS NOT NULL`; Entwurf ohne Nummer);
`Status` (`BfAuftragStatus`: Entwurf, Bestellt, Freigegeben, Fertig, Storniert); `BestellerUserId`, `BestellerName` (Snapshot für
`ORDER_REMARK_4`); `BestelltAm`; `OrderRecipientGroupId` (bei Abschicken aus Einstellung); `EmailSentAt`; `FreigegebenAm/Von`;
`Schachteltermin date NULL`, `FeTermin date NULL` (bei Freigabe festgeschrieben); `CsvStatus` (Keine, Offen, Geschrieben, Fehler),
`CsvVersuche`, `CsvFehler`, `CsvDatei`, `CsvGeschriebenAm` (Felder wie `WareneingangCsvExport`); `FertigAm/Von`; `StorniertAm/Von`,
`StornoGrund`; `[Timestamp] RowVersion`; `Positionen`. Index auf `Status`, `CsvStatus`.

`BfAuftragPosition : AuditableEntity` – `Id`, `BfAuftragId` (FK, Cascade), `Position int`, `ArticleNumber`, `Matchcode`, `Teileart`,
`Material`, `Materialnummer`, `FertigungsInfoPN`, `Hauptlagerplatz nvarchar`, `Menge decimal(18,3)`, `SageBestellmenge decimal(18,3) NULL`
(Snapshot für die Mengenregel), `Status nvarchar(50) NULL` (Etikett), `Bemerkung nvarchar(400) NULL` (bei Freigabe berechnet),
`Gestrichen bit`, `SageFa nvarchar(50) NULL` (Stufe 2, in Stufe 1 nie geschrieben). UNIQUE (`BfAuftragId`, `Position`).

`SageFa` je Position, weil Stufe 2 laut Koordination einen Sage-FA je BF-Position anlegt (Backlog „Stufe 2“).

### Ablauf und Regeln (Web, `BfAuftragWorkflow`)
- **Nummer:** im Abschicken, `Nummernkreis.MitVersuchenAsync` + MAX der Nummer (feste Breite, Textmaximum = Zahlmaximum) + 1, Muster wie
  `BeschichtungsauftragWorkflow.cs:377-398`; der UNIQUE-Index entscheidet bei Gleichzeitigkeit.
- **Mengenregel:** `SageBestellmenge > 0` → `Menge % SageBestellmenge == 0` und `Menge > 0`; sonst `Menge > 0`. Gilt beim Hinzufügen,
  beim Ändern durch den Schachtler und noch einmal bei der Freigabe (ADR 0026: der Server prüft am Schreibort).
- **Termine (bei Freigabe):** `Schachteltermin = AddBusinessDays(Bezugstag, BfSchachtelterminArbeitstage, holidays)`,
  `FeTermin = AddBusinessDays(Bezugstag, BfFeTerminArbeitstage, holidays)`; Bezugstag nach Rückfrage 3.
- **Bemerkung je Position (bei Freigabe), Excel-Formel + Status vorne (Rückfrage 4):**
  `[Status + " - "]` + `[FertigungsInfoPN + " - "]` + `Bezugstag:dd.MM` + `[" - " + Position, wenn Teileart = "BG"]`.
  Leer, wenn keine Menge (kann nach der Mengenregel nicht vorkommen; gestrichene Positionen gehen nicht in die CSV).
- **Matchcode (ORDER_NC_FILE):** Excel `TRIM(CLEAN(UPPER(x)))` → Steuerzeichen < 32 entfernen, Leerzeichenfolgen auf eines kürzen, trimmen,
  `ToUpperInvariant()`. Excel-`TRIM` kürzt auch innere Mehrfach-Leerzeichen – das ist Absicht, Test deckt es ab.
- **Freigabe-Sperren:** Materialnummer leer, Matchcode leer, Hauptlagerplatz leer, verbotene Zeichen (`;`, `"`, CR/LF, nicht in
  Windows-1252 kodierbar) → Freigabe abgelehnt, WarningMessage mit Position und Feld.
- **Gleichzeitigkeit:** `RowVersion`; jede Aktion sendet die Absicht (Status-Übergang), der Server prüft den Ausgangsstatus (ADR 0026).

### CSV-Aufbau (Dienst, `BfCsvService`)
Vorläufige Vorlage nach Backlog-Analyse und Auftrag; **die exakte Zeichenfolge wird aus einer echten Pool-Datei übernommen (P-1, Rückfrage 8)**
und als Unit-Test-Vergleich festgeschrieben. Unbelegt bis P-1: Spalte der Kopfschlüssel (Backlog-Tabelle sagt „Schlüssel (B)“, Auftrag sagt
`CREATE_NEW_ORDER;YES`), Leerzeilen 10 und 12, deutsche Überschrift Zeile 13, Trenner am Zeilenende.

```
PART_START
CREATE_NEW_ORDER;{BfCsvCreateNewOrder}
ORDER_NUMBER;{Nummer}
ORDER_REMARK_3;{BfCsvRemark3}          (Default *HEADER_LINE_19)
ORDER_REMARK_1;{BfCsvRemark1}          (Default *HEADER_LINE_17)
ORDER_REMARK_2;{BfCsvRemark2}          (Default *HEADER_LINE_18)
ORDER_PRIORITY;{BfCsvOrderPriority}    (Default 1)
ORDER_RELEASE;{BfCsvOrderRelease}      (Default YES)
PART_END
ORDER_NC_FILE;ORDER_MATERIAL_NO;ORDER_AMOUNT;ORDER_REMARK;ORDER_POSITION;ORDER_REMARK_4;ORDER_DATE;ORDER_REMARK_5
{Matchcode};{Materialnummer};{Menge};{Bemerkung};{Hauptlagerplatz};{Besteller};{Schachteltermin:dd.MM.yyyy};{FeTermin:yyyy-MM-dd}
```
- Nur Spalten A–H, eine Zeile je nicht gestrichener Position, keine Leerzeilen-Auffüllung.
- Menge: ganzzahlig ohne Nachkommastellen, sonst Dezimalkomma (`de-AT`).
- Kodierung `Encoding.GetEncoding(1252)` mit `EncoderFallback.ExceptionFallback` (nie still `?`); dafür einmal
  `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)` im Dienst-Start (im Code bisher nicht verwendet, Suche ohne Treffer; der
  Provider ist Teil des .NET-Frameworks, kein neues Paket). Zeilenende CRLF (Excel), bis P-1 es anders zeigt.
- Schreiben wie `WareneingangCsvService.Schreiben`: `.tmp`, dann `File.Move(tmp, ziel, overwrite:false)`; Kollision → `Fehler` ohne
  automatische Wiederholung. Max. 10 Dateien je Takt, Wiederholung nach `Sync:BfCsvWiederholAbstandMinuten` bis `Sync:BfCsvMaxWiederholungen`.
- Aktivitäts-Protokoll (ADR 0010): Service-Name `BfAuftragCsv` in `SyncLogServices.All` + `DisplayNames` („Blechfertigung: WiCAM-CSV“),
  Counts `geschrieben`, `fehler`; `ISyncLogger` als letzter Ctor-Parameter. Mail-Schritt analog `BfAuftragEmail` („Blechfertigung: Mail“),
  Counts `gesendet`, `fehler`.

### Web: Controller, Views, Menü
- `BfAuftraegeController` (MVC): Class-Level `[RequireBlechfertigungAktiv]` + Lesezugriff (Besteller-Rollen ODER schachtler); Actions
  `Index`, `Details`, `Warenkorb`, `AddItem`, `UpdateItem`, `RemoveItem`, `Submit`, `CancelOwn` (Besteller); `[RequireSchachtlerAccess]` auf
  `UpdatePosition`, `StreichePosition`, `Freigeben`, `FertigMelden` (Mehrfachauswahl), `Stornieren`, `CsvErneutSchreiben`.
  TempData nur `SuccessMessage`/`WarningMessage`.
- `BfAuftraegeApiController` (`ControllerBase`, `[ApiController]`): `GET api/bf-auftraege/artikel?q&limit` mit denselben Filtern, liefert
  Matchcode, Teileart, Material, Materialnummer, FertigungsInfoPN, Hauptlagerplatz, Bestellmenge (+ Hinweis „keine Materialnummer“).
- Menü „Blechfertigung“ nur bei `BlechfertigungAktiv` und passender Rolle. Frontend-Arbeit mit Skill `frontend-design`, Bootstrap-Muster der
  Lagerbestellung.

### Ponytail
Wiederverwendet statt neu: Warenkorb-Ablauf, Empfängergruppe/Mail, Nummernkreis-Helfer, `AddBusinessDays` + Feiertage, CSV-Schreibmuster,
`csv-export.js`, Benutzerfelder-Leser/-Writer, Rollen-/Gate-Attribute. Bewusst weggelassen: Zwischenstatus „In Prüfung“ (der Status
`Bestellt` ist die Prüfphase), eigene Material-Tabelle (Rückfrage 2), eigene Positionsliste (Rückfrage 12), Einstellungen für Trenner/Kodierung/
Datumsformat (Rückfrage 9).

> [!note] Schnittempfehlung (kein Split/Epic gesetzt)
> Der Umfang passt knapp in einen Dev-Lauf. Falls nicht, in dieser Reihenfolge je ein Commit (eine Version):
> **A** Datenquelle (V-B, nur Dienst, prüfbar über Artikel-Merkmale) → **B** Schema, Nummernkreis, Warenkorb, Abschicken, Mail, Rolle, Liste
> → **C** Freigabe, Termine, Bemerkung, CSV-Dienst, erneut schreiben → **D** Fertig/Storno, Hilfe, Manifest, TESTSZENARIEN. A ist ohne B
> nutzlos, aber risikolos; C hängt an P-1 (CSV-Vorlage). Hängt Rückfrage 1 an V-A, entfällt A und B/C warten auf den P-R1-Spec.

## Migrations-/SQL-Auswirkungen

- Model → `dotnet ef migrations add AddBfAuftraege` → `SQL/<nnn>_AddBfAuftraege.sql` (voraussichtlich 156): `IF OBJECT_ID(N'dbo.BfAuftraege')
  IS NULL CREATE TABLE …` und `dbo.BfAuftragPositionen` je in eigenem Batch; Indizes mit `IF NOT EXISTS (sys.indexes …)`: gefilterter
  UNIQUE `UX_BfAuftraege_Nummer WHERE Nummer IS NOT NULL`, `IX_BfAuftraege_Status`, `IX_BfAuftraege_CsvStatus`,
  `UX_BfAuftragPositionen_Auftrag_Position`; FK auf `OrderRecipientGroups` (NO ACTION) und Positionen → Kopf (CASCADE).
  `__EFMigrationsHistory`-Insert in separatem Batch.
- `SQL/00_FreshInstall.sql` an **beiden** Stellen (Tabellen + `MigrationId`).
- Nicht daten-destruktiv, nur neue Tabellen. `SQL/AgentJobs/` nicht betroffen (keine Pflichtfeld-Änderung an bestehenden Tabellen).
- Keine Seed-Daten (Material-Tabelle entfällt bei Rückfrage 2 = Sage-Feld; sonst neue Tabelle `BfMaterialnummern` + Seed der 30 Einträge aus
  dem Backlog als eigenes idempotentes Skript mit `WHERE NOT EXISTS` und Pflegemaske unter Stammdaten).
- Rolle `schachtler`: kein Seed in `Program.cs`, Manifest-Reparatur wie `einkauf`.
- Unter V-B keine Schema-Änderung für die Datenquelle (Merkmale in vorhandenen Tabellen).

## Einstellungen

AppSettings (`/Settings`, Gruppe neu „Blechfertigung“, Web):

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `BlechfertigungAktiv` | bool | Auswahl true/false | false | Schaltet Menü, Warenkorb, Liste und Aktionen der BF-Aufträge frei | `RequireBlechfertigungAktiv`, Menü | IDEAL (AKE bleibt aus) | – |
| `BfEmpfaengergruppeId` | Stammdatenverweis | Auswahl aus Bestellempfängergruppen (`SettingStammdaten.Bestellempfaengergruppe`) | leer | Gruppe, die beim Abschicken die Mail bekommt; leer = Abschicken gesperrt | `BfAuftragWorkflow.Submit` | IDEAL | Sendet Mails an die Mitglieder der Gruppe (über den Dienst) |
| `BfStatuswerte` | Text (kommagetrennt) | Werte ohne `;`, `"`, Komma im Wert, je ≤ 30 Zeichen; Server validiert | `EILT` | Auswahl im Warenkorb; „kein Status“ ist immer da | Warenkorb, Bemerkung | IDEAL | Wert steht vorne in der WiCAM-Bemerkung |
| `BfSchachtelterminArbeitstage` | int | 0–30 | 1 | Arbeitstage ab Bezugstag bis Schachteltermin (`ORDER_DATE`) | Freigabe | IDEAL | Wirkt auf die Termine in WiCAM |
| `BfFeTerminArbeitstage` | int | 0–60, ≥ Schachteltermin-Tage (Server prüft) | 10 | Arbeitstage ab Bezugstag bis FE-Termin (`ORDER_REMARK_5`) | Freigabe | IDEAL | Wirkt auf die Termine in WiCAM |
| (V-B) `Sync:BlechteileFelderNachlesen` | bool (ServiceSetting) | Auswahl true/false | false | Liest im View-Modus die Blechteile-Felder per SData nach und schreibt sie als Merkmale | Artikel-Abgleich | IDEAL | Liest aus Sage (nur lesend), braucht SData-Zugang |

ServiceSettings (`/ServiceSettings`, `ServiceSettingDefinitions.All`, Gruppe „Blechfertigung WiCAM“, Dienst):

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `BfCsvAktiv` | bool | Auswahl true/false | false | Dienst schreibt freigegebene BF-Aufträge als CSV | `SageBookingWorker` → `BfCsvService` | IDEAL | Schreibt Aufträge in den WiCAM-Auftragspool – WiCAM legt daraus Aufträge an |
| `BfCsvPfad` | Text (UNC) | UNC nach `WareneingangCsv.PfadMuster` (Server validiert) | leer | Zielordner der CSV | `BfCsvService` | IDEAL | Dienstkonto braucht Schreibrecht; geplant `\\idealcam.ideal.ideal-ake.at\fertigung\u\pn\apools\` |
| `Sync:BfCsvMaxWiederholungen` | int | 0–20 | wie `Sync:WareneingangCsvMaxWiederholungen` | Automatische Wiederholungen bei Schreibfehler | `BfCsvService` | IDEAL | – |
| `Sync:BfCsvWiederholAbstandMinuten` | int | 1–1440 | wie `Sync:WareneingangCsvWiederholAbstandMinuten` | Mindestabstand zwischen Versuchen | `BfCsvService` | IDEAL | – |
| `BfCsvCreateNewOrder` | Auswahl | YES, NO | YES | Wert der Kopfzeile `CREATE_NEW_ORDER` | CSV-Kopf | IDEAL | Ändert das Verhalten des WiCAM-Imports |
| `BfCsvOrderPriority` | int | 1–9 (Bereich mit Walter bestätigen) | 1 | `ORDER_PRIORITY` | CSV-Kopf | IDEAL | Ändert die Priorität in WiCAM |
| `BfCsvOrderRelease` | Auswahl | YES, NO | YES | `ORDER_RELEASE`: NO = Teile im A-Pool gesperrt | CSV-Kopf | IDEAL | NO sperrt die Teile in WiCAM |
| `BfCsvRemark1` / `BfCsvRemark2` / `BfCsvRemark3` | Text | ohne `;`, `"`, Zeilenumbruch, ≤ 100 | `*HEADER_LINE_17` / `*HEADER_LINE_18` / `*HEADER_LINE_19` | Kopfwerte `ORDER_REMARK_1/2/3` (PN füllt beim Import) | CSV-Kopf | IDEAL | Änderung bricht ggf. die PN-Übernahme |
| `Sync:BfAuftragEmailEnabled` | bool | Auswahl true/false | false | Dienst sendet die Bestell-Mail an die Empfängergruppe | `SyncWorker` → `BfAuftragEmailService` | IDEAL | Sendet Mails |

Fest im Code (Rückfrage 9): Trenner `;`, Windows-1252, `ORDER_DATE` `dd.MM.yyyy`, `ORDER_REMARK_5` `yyyy-MM-dd`, Spaltenschlüssel.

## Audit-Feld-Auswirkungen

- `BfAuftrag` und `BfAuftragPosition` erben `AuditableEntity`; jede Änderung setzt `ModifiedAt/By/ByWindows` aus `ICurrentUserService`
  (Web) bzw. Service-Akteur `system:bf-csv` / `system:bf-mail` + Windows-Konto des Dienstes (Muster `WareneingangCsv.ServiceActor`,
  `ArtikelBenutzerfelderWriter.cs:30-31`). Zeit `DateTime.Now` (ADR 0025).
- Fachliche Zeitstempel zusätzlich (`BestelltAm`, `FreigegebenAm/Von`, `FertigAm/Von`, `StorniertAm/Von`, `CsvGeschriebenAm`, `EmailSentAt`).
- V-B: Merkmale mit Audit nur bei echter Änderung (vorhandener Writer).

## Rollen und Zugriff

- Neu `RoleKeys.Schachtler = "schachtler"`, `ICurrentUserService.CanSchachtelnAsync()` = `HasAnyRoleAsync(Admin, Schachtler)`,
  `RequireSchachtlerAccessAttribute`. **Drei Stellen:** Attribut, `secondbrain/codebase/controller.md` (Filtertabelle wie `:70`),
  `Views/Users/RoleOverview.cshtml`; zusätzlich `docs/betrieb/08-diagnose-abfragen.md:461` (Rollenliste).
- Bestellen: nach Rückfrage 6 (Empfehlung `CanOrderLagerAsync` ODER Schachtler). Lesen der Liste: Besteller sehen eigene Aufträge,
  Schachtler/admin alle (Muster Owner-Check der Lagerbestellung).
- Gate kumulativ (ADR 0011): `RequireBlechfertigungAktiv` zusätzlich zu jedem Rollenfilter.
- Rolle per Manifest-Reparatur angelegt; Zuweisung der Personen ist Betrieb (Rückfrage 7).

## Listen-View (ADR 0005)

`BfAuftraege/Index`: `PageSize.Resolve` + `PaginationState` + `_Pagination`; Filterkarte (Status, Zeitraum Bestelldatum, „nur meine“);
Server-Spaltenfilter (`data-server-column-filter="true"`, jede `<th>` mit `data-col-key`, `ColumnFilterHelper.ReadFromQuery`/`Apply`,
Typen über `ColumnFilterTypes.For`). Spalten: Auswahl (Checkbox, nur Schachtler), Nummer, Status, CSV-Status, Besteller, Bestellt am,
Positionen, Freigegeben am/von, Schachteltermin, FE-Termin, Fertig am. Datumsspalten filtern nach der Berechnung (hier gespeichert,
also in SQL möglich). `table.filterable-table` → Excel-Export über `csv-export.js`. Detailseite: Positionstabelle (klein, unpaginiert,
vorgefiltert auf den Auftrag → Client-Mode erlaubt) ebenfalls exportierbar.

## Prüfabfragen (Mensch, vor Freigabe bzw. vor Dev-Start)

Sage-Spaltennamen in SQL sind aus den SData-Feldnamen abgeleitet und **unbelegt** – schlägt eine Abfrage fehl, Spaltennamen in SSMS nachsehen.

- **P-1 Muster-CSV (Rückfrage 8):** eine heute vom Makro erzeugte Datei aus dem Pool als Bytes ansehen:
  `Get-ChildItem \\idealcam.ideal.ideal-ake.at\fertigung\u\pn\apools\BF*.csv | Sort LastWriteTime -Desc | Select -First 3` und
  `Format-Hex <datei> | Select -First 40`. Ergebnis: Spalte der Schlüssel, Leerzeilen, Zeilenende, Kodierung (Umlaut-Bytes), Trenner am Zeilenende.
  Datei als Testvorlage ins Repo (`IDEALAKEWMSService.Tests/TestData/`).
- **P-2 Lagerartikel im WMS vorhanden** (`IDEALSQL2\SAGE100TEST`, bzw. Prod):
  ```sql
  SELECT k.Mandant, k.USER_OSArtikeltyp, COUNT(*) AS Sage,
         SUM(CASE WHEN a.Id IS NULL THEN 0 ELSE 1 END) AS ImWms,
         SUM(CASE WHEN a.Id IS NOT NULL AND a.PrimaryStorageLocationId IS NULL AND ISNULL(a.SagePrimaryStorageLocation,'') = '' THEN 1 ELSE 0 END) AS OhneLagerplatz
  FROM IDEAL.dbo.KHKArtikel k
  LEFT JOIN IDEAL_AKE_WMS.dbo.Articles a ON a.ArticleNumber = k.Artikelnummer COLLATE DATABASE_DEFAULT
  WHERE k.USER_OSIstLagerartikel <> 0
  GROUP BY k.Mandant, k.USER_OSArtikeltyp;
  ```
- **P-3 Materialnummer gepflegt (Rückfrage 2):**
  ```sql
  SELECT k.USER_OSMaterial, k.USER_OSMaterialnummerPN, COUNT(*) AS Anzahl
  FROM IDEAL.dbo.KHKArtikel k WHERE k.USER_OSIstLagerartikel <> 0
  GROUP BY k.USER_OSMaterial, k.USER_OSMaterialnummerPN ORDER BY Anzahl DESC;
  ```
  Erwartung bei „Sage-Feld“: jede Material-Zeile genau eine Nummer, keine leeren Nummern; Abgleich mit der 30er-Liste im Backlog.
- **P-4 Bestellmenge und Einheiten (Rückfrage 10):**
  ```sql
  SELECT TOP 50 k.Artikelnummer, k.USER_OSArtikeltyp, v.Bestellmenge, k.Basismengeneinheit, k.Lagermengeneinheit
  FROM IDEAL.dbo.KHKArtikel k JOIN IDEAL.dbo.KHKArtikelVarianten v ON v.Artikelnummer = k.Artikelnummer AND v.Mandant = k.Mandant
  WHERE k.USER_OSIstLagerartikel <> 0 AND (k.Basismengeneinheit <> k.Lagermengeneinheit OR v.Bestellmenge <> FLOOR(v.Bestellmenge));
  ```
- **P-5 Feiertage IDEAL-WMS** (Excel-Blatt `Feiertage_Tab4` enthält auch Betriebsurlaub/Zwickeltage):
  `SELECT YEAR([Date]) AS Jahr, COUNT(*) FROM dbo.Holidays GROUP BY YEAR([Date]) ORDER BY Jahr;` – gegen das Excel-Blatt vergleichen; fehlende
  Tage unter Stammdaten > Feiertage nachtragen.
- **P-6 Zeichen im Matchcode** (WMS):
  `SELECT ArticleNumber, Matchcode FROM dbo.Articles WHERE Matchcode LIKE N'%;%' OR Matchcode LIKE N'%"%' OR Matchcode <> CAST(CAST(Matchcode AS VARCHAR(400)) COLLATE Latin1_General_CI_AS AS NVARCHAR(400));`
- **P-7 Schreibrecht Dienstkonto** auf den Pool (als Dienstkonto): `New-Item <pfad>\wms-test.tmp; Remove-Item <pfad>\wms-test.tmp`.

## Akzeptanzkriterien

1. `BlechfertigungAktiv = false`: Menü fehlt, jede BF-URL leitet mit WarningMessage um (auch für admin).
2. Die Artikelsuche im Warenkorb liefert nur Artikel mit IstLagerartikel ≠ 0 und Artikeltyp BG oder FT; ZK, Nicht-Lagerartikel und DUMMY nie.
3. Eine neue Position übernimmt Matchcode, Teileart, Material, Materialnummer, FertigungsInfoPN, Hauptlagerplatz und Menge = Sage-Bestellmenge.
4. Menge 30 bei Bestellmenge 20 wird abgelehnt (Hinweis), 40 angenommen; bei Bestellmenge 0/leer ist jede Menge > 0 erlaubt; 0 und negativ nie.
5. Die Status-Auswahl zeigt „kein Status“ und genau die Werte aus `BfStatuswerte`.
6. Abschicken ohne gesetzte `BfEmpfaengergruppeId` wird gesperrt; mit Gruppe erhält der Auftrag die nächste Nummer `BF` + 8 Ziffern, Status
   `Bestellt`; zwei gleichzeitige Abschicken erzeugen zwei verschiedene Nummern.
7. Mit `Sync:BfAuftragEmailEnabled = true` geht genau eine Mail an die aktiven Empfänger der Gruppe; `EmailSentAt` gesetzt; Protokoll `BfAuftragEmail`.
8. Ohne Rolle `schachtler` (und nicht admin) sind Freigeben, Fertig melden, Position ändern/streichen und „CSV erneut schreiben“ verboten (Filter, nicht nur ausgeblendet).
9. Freigabe mit einer Position ohne Materialnummer oder mit `;` im Matchcode wird abgelehnt und nennt Position und Feld.
10. Freigabe schreibt Schachteltermin und FE-Termin nach `AddBusinessDays` mit Feiertagen fest (Beispiel: Bezugstag Freitag, Montag Feiertag
    → Schachteltermin Dienstag) und je Position die Bemerkung nach der Regel (BG mit `- Pos`, FT ohne).
11. Mit `BfCsvAktiv = true` und gültigem Pfad liegt nach einem Dienst-Takt `<Nummer>.csv` im Ziel; Inhalt Byte-gleich zur Vorlage aus P-1
    (Kopf, Spalten A–H, Windows-1252, Datumsformate `dd.MM.yyyy`/`yyyy-MM-dd`, Matchcode bereinigt und groß), gestrichene Positionen fehlen.
12. Existiert die Datei schon, bleibt sie unverändert, CSV-Status `Fehler` mit Text, keine automatische Wiederholung; „CSV erneut schreiben“ setzt `Offen`.
13. Ein Netzwerkfehler führt zu `Fehler` und automatischer Wiederholung nach Abstand bis zur Höchstzahl; das Protokoll `BfAuftragCsv` zählt `fehler`.
14. „Fertig melden“ (einzeln und Mehrfachauswahl) setzt nur Aufträge im Status `Freigegeben` auf `Fertig`; keine `StockMovement`-Zeile entsteht.
15. Storno nach Rückfrage 11; ein stornierter Auftrag wird nie als CSV geschrieben.
16. Die Liste hat Pagination, Filterkarte, Server-Spaltenfilter auf allen Spalten und Export nach Excel.
17. V-B: Nach einem Artikel-Abgleich an IDEAL tragen die 1.263 Lagerartikel die Merkmale; ein Artikel, dessen IstLagerartikel in Sage auf 0
    geht, verliert das Merkmal beim nächsten Lauf; ein SData-Fehler bricht den View-Abgleich nicht (Warnung im Protokoll).
18. AKE (Schalter aus) verhält sich unverändert; Build und alle Tests grün.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (voraussichtlich **TS-156** – beim Dev-Start prüfen) + `secondbrain/tests/testszenarien-index.md`:
- **Vorbedingungen:** IDEAL-DEV, `BlechfertigungAktiv` an, Empfängergruppe „Bestellung Lagerteile“ mit Test-Postfach, Benutzer A (lagerbestellung),
  Benutzer S (schachtler), Benutzer X (ohne beide); `BfCsvPfad` auf Testordner, `BfCsvAktiv` an; DEV-Mail per PickupDirectory (.eml);
  Feiertag am nächsten Arbeitstag angelegt; Artikel-Merkmale vorhanden (V-B-Lauf oder Testdaten).
- **Schritte:** 1 A sucht BG und FT, legt 3 Positionen an (eine BG mit Bestellmenge 20, eine FT mit leerer Bestellmenge, eine mit Status EILT);
  2 Menge 30 → Hinweis, 40 → ok; 3 Abschicken → Nummer, Mail im Pickup-Ordner; 4 S ändert Lagerplatz, streicht eine Position, gibt frei;
  5 Termine prüfen (Feiertag übersprungen); 6 nach Dienst-Takt CSV im Testordner mit Notepad++ (Kodierung ANSI) gegen P-1-Datei vergleichen;
  7 S meldet fertig über Checkbox in der Liste; 8 Liste filtern und exportieren.
- **Negativfälle:** X sieht kein Menü/403-Umleitung; A kann nicht freigeben (direkte POST-URL); Gruppe leer → Abschicken gesperrt;
  Material ohne Nummer → Freigabe gesperrt; Matchcode mit `;` → Freigabe gesperrt; Datei existiert → Fehler, erneut schreiben nach Löschen;
  Pfad ohne Recht → Fehler + Wiederholung; Schalter aus → keine Datei, Auftrag bleibt `Offen`; Doppelklick Freigabe → genau eine Freigabe;
  AKE: Menü fehlt.

## Etappen (nur bei epic: true)

Kein Epic. Schnittempfehlung siehe Hinweis im Lösungsentwurf.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt | – | – |

## Deploy

**Vorläufig – der qa-agent bestätigt gegen den echten Diff.**

- **Web-App:** ja (Controller, Views, Rolle, AppSettings, Migration).
- **Service:** ja (`BfCsvService`, `BfAuftragEmailService`, Worker-Schritte, bei V-B Artikel-Abgleich).
- **Migration:** ja, `SQL/<nnn>_AddBfAuftraege.sql` (voraussichtlich 156), nicht destruktiv.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, Nummern/Version beim Dev-Start einsetzen):
  - `@{ Id='SQL/156'; Version='<v>'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'; Datei='156_AddBfAuftraege.sql'; MigrationId='<id>_AddBfAuftraege'; Text='Tabellen BfAuftraege und BfAuftragPositionen (Blechfertigung).' }`
  - `@{ Id='Pruefung/RolleSchachtler'; Version='<v>'; Typ='Pruefung'; Firma='beide'; Phase='Haltepunkt'; Ausfuehrung='Reparatur'; Pruefung='SELECT COUNT(*) FROM (VALUES (N''schachtler'')) v(k) WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.[Key] = v.k)'; Reparatur='INSERT INTO dbo.Roles (…) wie Pruefung/RolleEinkauf'; Text='Rolle schachtler fehlt.' }`
  - `@{ Id='<v>-BlechfertigungAktiv'; Version='<v>'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'; Text='Erst nach Einrichtung (Gruppe, Rolle, Pfad, Feiertage): /Settings > Blechfertigung > BlechfertigungAktiv = true. AKE bleibt aus.' }`
  - `@{ Id='<v>-BfCsv'; Version='<v>'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'; Text='/ServiceSettings > Blechfertigung WiCAM: BfCsvPfad = \\idealcam.ideal.ideal-ake.at\fertigung\u\pn\apools\, BfCsvAktiv = true, Sync:BfAuftragEmailEnabled = true.' }`
  - `@{ Id='Pruefung/BfEmpfaengergruppe'; Version='<v>'; Typ='Pruefung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'; Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = N''BlechfertigungAktiv'' AND LOWER([Value]) = N''true'') AND NOT EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = N''BfEmpfaengergruppeId'' AND LEN(LTRIM(RTRIM(ISNULL([Value], N'''')))) > 0) THEN 1 ELSE 0 END'; Text='Blechfertigung an, aber keine Empfaengergruppe: Abschicken gesperrt. Gruppe mit bestellung.lagerteile@ideal-ake.at anlegen und unter /Settings waehlen.' }`
  - `@{ Id='Pruefung/BfCsvPfad'; Version='<v>'; Typ='Pruefung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'; Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''BfCsvAktiv'' AND LOWER(LTRIM(RTRIM([Value]))) = N''true'') AND NOT EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''BfCsvPfad'' AND LEN(LTRIM(RTRIM([Value]))) > 0) THEN 1 ELSE 0 END'; Text='BfCsvAktiv an, aber BfCsvPfad leer: es wird keine CSV geschrieben.' }`
  - `@{ Id='Pruefung/FeiertageIdeal'; Version='<v>'; Typ='Pruefung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'; Pruefung='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Holidays WHERE [Date] >= CAST(GETDATE() AS date)) THEN 0 ELSE 1 END'; Text='Keine kuenftigen Feiertage: Schachtel-/FE-Termine ignorieren Feiertage und Betriebsurlaub. Stammdaten > Feiertage pflegen (Excel-Blatt Feiertage_Tab4).' }`
  - `@{ Id='<v>-BfPoolRecht'; Version='<v>'; Typ='Info'; Firma='IDEAL'; Phase='Vorher'; Ausfuehrung='anzeigen'; Text='Dienstkonto braucht Schreibrecht auf \\idealcam.ideal.ideal-ake.at\fertigung\u\pn\apools\ (P-7 der Spec).' }`
  - nur V-B: `@{ Id='<v>-BlechteileFelder'; Version='<v>'; Typ='Einstellung'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'; Text='/ServiceSettings: Sync:BlechteileFelderNachlesen = true (SData-Zugang IDEAL vorhanden); nach dem naechsten Artikel-Abgleich Merkmale IstLagerartikel pruefen.' }`
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

## Folgearbeit (Aufgaben, nicht Teil dieser Spec)
- Stufe 2: Sage-FA je BF-Position (`FertigungsauftragService` / `InterneFertigungBuchungService`), Schreibtest, Duplikatschutz über
  Benutzerfeld mit BF-Nummer, Feld `SageFa`.
- BDE-Fertigmeldung am letzten Arbeitsgang (`USER_OSArbeitsschritte`), sobald die fehlenden Arbeitsgänge (ZS, MT, MK, ML, ISO, PG, T, SW, EP)
  in Sage angelegt sind; Anschluss an die BDE-Teileliste.
- P-R1 (V-A) als eigener Spec, danach `Sync:BlechteileFelderNachlesen` aus.

## Offene Rueckfragen

1. **Datenquelle der Artikelfelder an IDEAL** – V-A (P-R1 zuerst, BF wartet), V-B (schmaler SData-Nachlese-Schritt im View-Modus, Merkmale +
   Bestellmenge als Merkmal), V-C (Sage-View erweitern)? **Empfehlung V-B:** kein Eingriff in den produktiven Lager-Abgleich, nutzt vorhandenen
   Leser/Writer, später per Schalter abschaltbar.
2. **Materialnummer** aus `USER_OSMaterialnummerPN` statt WMS-Tabelle? **Empfehlung Sage-Feld** (L10 :1094 zeigt 4301 BLANK → 3 wie die
   Excel-Liste); Tabelle mit Seed nur, wenn P-3 Lücken oder Widersprüche zeigt.
3. **Bezugstag** für Termine und `TT.MM` der Bemerkung: Abschicktag oder Freigabetag? **Empfehlung Freigabetag** – in der Excel waren
   Bestellen und CSV-Erzeugung derselbe Tag; mit Prüfschritt läge der Schachteltermin sonst ggf. in der Vergangenheit.
4. **Bemerkung:** Status vorne als `EILT - ` vor die Excel-Formel? **Empfehlung ja** (Benjamin Antwort 5: „Status steht vorne“).
5. **Freigabe/Fertigmeldung je Auftrag** statt je Position (Schachtler streicht vorher, ändert Menge/Lagerplatz)? **Empfehlung je Auftrag**
   (eine CSV je BF-Auftrag, Gerald-Vorgabe; Positionen einzeln wären mehrere CSV je Nummer).
6. **Bestellrollen:** **Empfehlung** `CanOrderLagerAsync` (admin, picking, stock, stock_keyuser, lagerbestellung) plus schachtler; keine neue
   Bestellrolle.
7. **Rolle Schachtler:** Key `schachtler`, Anzeigename „Schachtler (Blechfertigung)“? Personen bestätigt Andi (nicht blockierend).
8. **CSV-Aufbau exakt** aus P-1 übernehmen (Schlüsselspalte A/B, Leerzeilen, Zeile 13, Zeilenende, Trenner am Ende)? **Empfehlung ja**, Datei
   als Testvorlage; bis dahin gilt die Vorlage im Lösungsentwurf als vorläufig.
9. **Trenner, Kodierung, Datumsformate fest** statt Einstellung? **Empfehlung fest** (Walter 17.11. bestätigt Excel-Format; Änderung bräuchte
   ohnehin einen WiCAM-Test).
10. **Mengenregel bei abweichender Einheit** (Basis ≠ Lager, BV-1/P-N1): **Empfehlung** nach P-4; Default: Vielfaches nur bei gleicher Einheit,
    sonst frei mit Hinweis im Warenkorb.
11. **Storno:** Besteller bis Freigabe, Schachtler bis Fertig (nach Freigabe nur WMS-Status, Hinweis „CSV liegt bereits im Pool, in WiCAM
    löschen“)? **Empfehlung ja.**
12. **Auswertung:** Auftragsliste + Positionstabelle im Detail mit Export statt eigener Positionsliste? **Empfehlung ja** für Stufe 1.
13. **Mail** nur beim Abschicken an die Gruppe, keine Mail an Besteller bei Freigabe/Fertig? **Empfehlung ja.**

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Datenquelle (V-A / V-B / V-C) →
2. Materialnummer aus USER_OSMaterialnummerPN →
3. Bezugstag Abschicken oder Freigabe →
4. Status vorne in der Bemerkung →
5. Freigabe/Fertig je Auftrag →
6. Bestellrollen →
7. Rolle schachtler (Key/Name) →
8. CSV-Aufbau aus P-1 →
9. Trenner/Kodierung/Datumsformat fest →
10. Mengenregel bei abweichender Einheit →
11. Storno-Regeln →
12. Auswertung ohne eigene Positionsliste →
13. Mail nur beim Abschicken →
