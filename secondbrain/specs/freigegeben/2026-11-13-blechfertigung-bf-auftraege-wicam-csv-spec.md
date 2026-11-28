---
type: spec
title: Blechfertigung – BF-Aufträge (Warenkorb, Schachtler-Freigabe, WiCAM-CSV, Fertigmeldung Stufe 1)
slug: 2026-11-13-blechfertigung-bf-auftraege-wicam-csv-spec
status: Gemerged
created: 2026-11-17
updated: 2026-11-28
source_backlog: "[[2026-11-13-blechfertigung-bf-auftraege-wicam-csv]]"
task: "[[2026-11-17-goal-lauf-bf-auftraege]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
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
freigabe_entscheidung: "Antworten 1-13 wie Empfehlung (Gerald 2026-11-17 16:33/16:34); V-B; Materialnummer Sage-Feld, fehlt -> Hinweis, keine Übergabe; Lagerartikel per View-Block 3 (Gerald, vorübergehend)"
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-17"
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
- CSV-Kopfwerte aus Sage (Gerät/Arbeitsschritte/Abteilung): PN füllt sie beim Import (Walter 17.11.), das WMS schreibt die Platzhalter. **Überholt für Arbeitsschritte:** seit v1.99.42 je Position aus dem Sage-Merkmal `USER_OSArbeitsschritte` in ORDER_REMARK_1; Walter 2026-11-19 bestätigt „Sage ist das führende System“.

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
„Freigeben“ prüft serverseitig: jede Position hat Materialnummer (ganzzahlig, seit v1.99.42), Matchcode, Hauptlagerplatz; der Besteller hat ein
Kürzel (seit v1.99.42); kein Textfeld enthält `;` (Ausnahme Lagerplatz, dort `;`→`,`), `"`,
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
**Seit v1.99.42 exakt wie die WiCAM-Datei `BF00000002.csv`** (Befund P-1 (2), Walter 2026-11-18 15:28 „Zielform“); Byte-Vergleichstest gegen
`IDEALAKEWMSService.Tests/TestData/BF00000002.csv`. Jede Zeile hat **13 Felder** (12 `;`, auch Leerzeilen), Zeilenende CRLF, kein BOM.

```
Z1   PART_START;;;…
Z2-8 ;CREATE_NEW_ORDER;{BfCsvCreateNewOrder};…      Schlüssel in B, Wert in C:
     ;ORDER_NUMBER;{Nummer}  ;ORDER_REMARK_3;{BfCsvRemark3}  ;ORDER_REMARK_1;{BfCsvRemark1}
     ;ORDER_REMARK_2;{BfCsvRemark2}  ;ORDER_PRIORITY;{BfCsvOrderPriority}  ;ORDER_RELEASE;{BfCsvOrderRelease}
Z9   PART_END;;;…
Z10  (leer, 12 ;)
Z11  ORDER_NC_FILE;ORDER_MATERIAL_NO;ORDER_AMOUNT;ORDER_REMARK;ORDER_POSITION;ORDER_REMARK_2;ORDER_REMARK_4;ORDER_DATE;ORDER_REMARK_5;ORDER_REMARK_1;;;
Z12  (leer)
Z13  Matchcode;Mat.Nr.;Menge;Bemerkung;Lagerplatz;Hauptlagerplatz;Besteller;Schachteltermin;FE-Termin;Arbeitsschritte;;;
Z14+ {Matchcode};{Mat.-Nr. ganzzahlig};{Menge};{Bemerkung};{Lagerplatz ;→,};{Teil vor dem ersten Trenner};{Besteller-Kürzel};{Schachteltermin dd.MM.yyyy};{FE-Termin yyyy-MM-dd};{Arbeitsschritte};;;
```
- Eine Zeile je übergebener Position (nicht gestrichen, mit Materialnummer), keine Auffüllzeilen, keine Anführungszeichen.
- Lagerplatz = voller Sage-Lagerplatz (`K-02;0;0;0` → `K-02,0,0,0`), Hauptlagerplatz = `K-02`; Besteller = `Users.Kuerzel` (Snapshot
  `BfAuftraege.BestellerKuerzel`); Arbeitsschritte = `USER_OSArbeitsschritte` (Snapshot `BfAuftragPositionen.Arbeitsschritte`, Userfeld-Einstellung).
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

> **Ergebnisse 2026-11-17 16:30 (Koordination, SSMS IDEALSQL2\SAGE100TEST, Sage `IDEAL`, WMS `IDEAL_AKE_WMS` (Prod) / `IDEAL_AKE_WMS_TEST`, nur SELECT):**
> - **P-2 Lagerartikel im WMS:** Sage `USER_OSIstLagerartikel <> 0`: BG 274, FT 988, ohne Typ 1. Davon im WMS **PROD nur BG 79 / FT 159** (1 BG ohne Lagerplatz), **TEST BG 133 / FT 634**.
>   Ursache: `vw_IDEAL_AKE_WMS_Artikel` (`Sync:ArticlesViewName` PROD und TEST) liefert nur Block 1 = Artikel aus der FAListe (offene FAs) und Block 2 = aktive
>   Bestellartikel (`IstBestellartikel = -1`), **nicht** die Lagerartikel. → Der Warenkorb fände die meisten Lagerteile nicht. Abhilfe ohne Code: Block 3 in der View
>   (`USER_OSIstLagerartikel <> 0`, aktiv, ohne 00-Nummern) – View ist unsere (Gerald 16.11.); oder Variante V-B (SData-Nachlese) muss diese Menge selbst lesen.
> - **Gerald 2026-11-17 16:33 zu P-2:** Die Artikel kommen über die FAs Schritt für Schritt ins WMS; **vorübergehend ist die Erweiterung der View um die Lagerartikel (Block 3) notwendig** – Gerald passt `vw_IDEAL_AKE_WMS_Artikel` an (zuerst Test). Kein Bauauftrag für den Dev-Lauf, aber Vorbedingung für den Warenkorb-Test.
> - **P-3 Materialnummer:** `USER_OSMaterialnummerPN` **nicht durchgängig gepflegt**: 4301 BLANK→3 (568), 4301 GEB+F→1 (339) **und →3 (2)**, 4509 G400→9 (69), 4016 BLANK→5 (22),
>   LEXAN-PLEXI→6, 4301 POL+F→10, PVC→19, FORMROHR→20 (2) **und leer (11)**, 4310 FEDERSTAHL→18, Jackodur 40→80 (1) **und leer (66)**; ohne Nummer: Jackodur 20 (96), Jackodur 30 (18),
>   „-“ (47), leer (14). → Rückfrage 2: Sage-Feld nur mit Bereinigung (Widersprüche 4301 GEB+F, FORMROHR, Jackodur) – oder WMS-Tabelle mit Seed. Ob Jackodur/„-“ überhaupt
>   WiCAM-Teile sind, klärt der Fachbereich.
> - **P-4 Bestellmenge/Einheiten (Lagerartikel, Grundvariante):** 1.263 Artikel, **0** mit Basis ≠ Lager, **0** mit nicht ganzzahliger Bestellmenge, **45** mit Bestellmenge 0/leer
>   (→ Menge frei). Rückfrage 10 damit gegenstandslos.
> - **P-5 Feiertage:** PROD 2026: 25, 2027: 17, 2028: 15; TEST je 15. → PROD enthält offenbar schon Betriebsurlaub/Zwickeltage für 2026 (25), 2027/2028 noch nicht;
>   Abgleich mit dem Excel-Blatt `Feiertage_Tab4` durch den Fachbereich.
> - **P-6 Matchcode:** Lagerartikel in Sage: 0 mit `;`, 0 mit `"`, 0 nicht ANSI, max. Länge 49 → CSV ohne Maskierung möglich. (Im ganzen WMS-Artikelstamm PROD 779 Treffer,
>   z. B. 00393375 „1-Leiter-Federleiste; RM 5 …“ – keine Lagerteile.)
> - **P-1, P-7** offen (Gerald: Pool-Datei als Bytes, Schreibrecht Dienstkonto).


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

> **Abgleich mit dem echten Diff (Dev-Lauf 2026-11-17, Commits `09063984` … `9f2a0802`, Version 1.99.35):** Web ja, Dienst ja, Migration
> **SQL/158** `158_AddBfAuftraege.sql` / `20261117154645_AddBfAuftraege` (nicht destruktiv, FreshInstall an beiden Stellen, AgentJobs nicht
> betroffen). Manifest-Einträge wie unten mit `<v>` = 1.99.35 und `156` → `158` eingetragen: `SQL/158`, `Pruefung/RolleSchachtler`,
> `Pruefung/BfEmpfaengergruppe`, `Pruefung/BfCsvPfad`, `Pruefung/FeiertageIdeal`, `v1.99.35-BfPoolRecht`, `-BlechfertigungAktiv`, `-BfCsv`,
> `-BlechteileFelder`. **P-1-Platzhalter:** das CSV-Layout (`IdealAkeWms/Services/BfCsv.cs`, Testvorlage
> `IDEALAKEWMSService.Tests/TestData/BfCsv_PlatzhalterBisP1.csv`) folgt der Vorlage im Lösungsentwurf; vor `BfCsvAktiv = true` mit der
> Musterdatei aus dem WiCAM-Pool abgleichen und die Testvorlage ersetzen (der `-BfCsv`-Manifesttext sagt das). Der qa-agent bestätigt.

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

1. Datenquelle (V-A / V-B / V-C) → **Gerald 2026-11-17 16:33 „der Rest passt“ (Empfehlung): V-B** (schmaler SData-Nachlese-Schritt, Merkmale + Bestellmenge). Vorbedingung: Lagerartikel per View-Block 3 im WMS (Gerald, vorübergehend, siehe Prüfabfragen P-2).
2. Materialnummer aus USER_OSMaterialnummerPN → **Gerald 2026-11-17 16:33 „der Rest passt“ (Empfehlung): Sage-Feld.** P-3 zeigt Lücken und Widersprüche (4301 GEB+F 1/3, FORMROHR 20/leer, Jackodur ohne Nummer) → Bereinigung in Sage durch den Fachbereich vor Echtbetrieb; fehlt die Nummer, Hinweis im Warenkorb und keine Übergabe dieser Position (bestätigt mit der Freigabe 2026-11-17 16:34).
3. Bezugstag Abschicken oder Freigabe → **Gerald 2026-11-17 16:33 „der Rest passt“ (Empfehlung): Freigabetag.**
4. Status vorne in der Bemerkung → **Gerald 2026-11-17 16:33 „der Rest passt“ (Empfehlung): ja, „EILT - “ vorne.**
5. Freigabe/Fertig je Auftrag → **Gerald 2026-11-17 16:33 „der Rest passt“ (Empfehlung): je Auftrag.**
6. Bestellrollen → **Gerald 2026-11-17 16:33 „der Rest passt“ (Empfehlung): CanOrderLagerAsync-Rollen + schachtler, keine neue Bestellrolle.**
7. Rolle schachtler (Key/Name) → **Gerald 2026-11-17 16:33 „der Rest passt“ (Empfehlung): Key `schachtler`, „Schachtler (Blechfertigung)“; Personen bestätigt Andi.**
8. CSV-Aufbau aus P-1 → **Gerald 2026-11-17 16:33 „der Rest passt“ (Empfehlung): ja, exakt aus P-1 (Pool-Datei als Testvorlage); bis dahin Vorlage aus dem Lösungsentwurf vorläufig.**
9. Trenner/Kodierung/Datumsformat fest → **Gerald 2026-11-17 16:33 „der Rest passt“ (Empfehlung): fest.**
10. Mengenregel bei abweichender Einheit → **Gerald 2026-11-17 16:33 „der Rest passt“: gegenstandslos – P-4: 0 Lagerartikel mit Basis ≠ Lager; Default (Vielfaches nur bei gleicher Einheit) bleibt als Absicherung.**
11. Storno-Regeln → **Gerald 2026-11-17 16:33 „der Rest passt“ (Empfehlung): ja.**
12. Auswertung ohne eigene Positionsliste → **Gerald 2026-11-17 16:33 „der Rest passt“ (Empfehlung): ja.**
13. Mail nur beim Abschicken → **Gerald 2026-11-17 16:33 „der Rest passt“ (Empfehlung): ja.**

## QA-Nachweis (qa-agent, 2026-11-17)

> [!warning] Ergebnis: **nicht Testbereit** – Status bleibt `InUmsetzung`
> Build, Tests, Review und DEV-Deploy sind grün; der Review-Befund F1 ist behoben und auf DEV nachgeprüft. Offen bleibt
> **B-1** (Fachentscheidung, kein Code-Fix durch QA): Die Freigabe ist mit echten Lagerplätzen praktisch immer gesperrt.
> Publish nach `<Worktree>\publish\` wurde deshalb **nicht** erstellt (erst nach grünem Stand).

**Geprüfter Stand:** Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`,
Review-Bereich `eff0c1a4..9f2a0802`, dazu QA-Fix `c6c04f9f`. Version 1.99.35, SQL/158, TS-157, Standort IDEAL (Frontmatter `standort: IDEAL`).

### 1. Build und Tests
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 29 Warnungen (vorbestehend, u. a. NU1903).
- `dotnet test` vor dem Fix: IDEALAKEWMSService.Tests 714 bestanden / 0 übersprungen / 0 Fehler; IdealAkeWms.Tests 3208 / 1 / 0 (3209).
- `dotnet test` nach dem Fix `c6c04f9f`: Service 714 / 0 / 0; Web **3216** / 1 / 0 (3217, +8 `BfAuftraegeMengeTests`).

### 2. Code-Review (selbst, synchron)
Geprüft: alle Dateien des Diffs, u. a. `BfAuftragWorkflow`, `BfAuftraegeController`, `BfAuftraegeApiController`, Views Index/Warenkorb/Details,
Filter `RequireBlechfertigungAktiv`/`RequireSchachtlerAccess`/`RequireBfBestellenAccess`, `CurrentUserService` (CanSchachteln/CanBfBestellen),
`BfAuftragRepository` (Lagerteile-SQL, MAX+1, serialisierbare Transaktion), `BfCsv`, `BfArtikelFelder`, Dienst `BfCsvService`,
`BfAuftragEmailService`, Einhängung in `SageBookingWorker`/`SyncWorker`, `BlechteileFelderNachlese` + `SdataArtikelReader.BlechteileFelderLesenAsync`
(nur GET), `SageImportService`-Hook, Settings-Kataloge, `SyncLogServices`, `_Layout`, `RoleOverview`, SQL/158 + FreshInstall, Manifest, `.gitattributes`.
Fokusklassen: keine Razor-Werte in `<script>` (nur statische JSON-Konfig), kein `hidden` + `d-*`, Mengenregel serverseitig bei Hinzufügen/Ändern/
Freigabe, Regel an einem Ort (`MengenFehler`, `FreigabeFehler`), kein stiller `?? 0`-Fallback (F1-Fix: nicht lesbar → 0 → sichtbare Fehlermeldung).

| # | Befund | Behandlung |
|---|---|---|
| **F1** | `AddItem`/`UpdateItem`/`UpdatePosition` banden `decimal menge` mit Kultur de-DE; Details rendert `Menge.ToString(Invariant)` = „40.000“. Auf DEV belegt: unverändertes „Speichern“ machte aus **40 → 40000** (besteht die Mengenregel als Vielfaches!), Eingabe „1.5“ → **15**. | **Behoben** `c6c04f9f`: Menge als Text binden, `BfAuftraegeController.Menge` → `BeschichtungsauftraegeController.TryMenge` (Punkt/Komma, Invariant), Details rendert `0.###`; Test `BfAuftraegeMengeTests` (8 Fälle). Auf DEV nachgeprüft: 40 bleibt 40, 1.5 → 1.500. |
| **B-1** | Spec (Abschnitte F3 und „Freigabe-Sperren“) verbietet `;` in **jedem** CSV-Feld, auch Hauptlagerplatz (= `ORDER_POSITION`). Lagerplatz-Codes aus Sage haben das Format `XX;n;n;n` (DEV IDEAL 228 von 230, DEV AKE 3285 von 3294; Artikel-Hauptlagerplatz z. B. `S-03;0;0;0`). Folge: Freigabe für praktisch jede Position mit Sage-Vorbelegung gesperrt („Pos 1 Hauptlagerplatz: Semikolon“, auf DEV belegt). | **Offen – Entscheidung Gerald** (mit P-1): Was erwartet WiCAM in `ORDER_POSITION`? Optionen: (a) nur Lagerkennung vor dem ersten `;` (`S-03`), (b) `;` ersetzen, (c) Feld in Anführungszeichen, falls WiCAM das liest. Danach kleiner Fix in `BfCsv`/`FreigabeFehler` + Test. |
| H-1 | Abschicken mit veralteter RowVersion: `DbUpdateConcurrencyException` läuft in die Nummern-Wiederholung (5×) und endet mit „keine freie BF-Nummer“ statt „von anderem Benutzer geändert“. Selten (gleicher Besteller in zwei Tabs). | Hinweis, nicht behoben. |
| H-2 | Zwei gleichzeitige erste `AddItem` könnten zwei Entwürfe anlegen; globaler Doppelklick-Schutz (v1.99.23) mindert. | Hinweis. |
| H-3 | `Index` lädt alle Aufträge mit Positionen in den Speicher und filtert dort. Bei erwarteter Menge vertretbar. | Hinweis. |
| H-4 | Mail-Dienst warnt bei Gruppe ohne aktive Empfänger jeden Takt – gleich wie `WarehouseRequisitionEmailService`. | Kein Befund (bestehendes Muster). |
| H-5 | AK 9 „ohne Materialnummer → abgelehnt“ widerspricht Antwort 2; umgesetzt ist Antwort 2 (Hinweis, Position nicht übergeben; gesperrt nur, wenn keine Position übergeben wird). | Konform zur Freigabe; TS-157 beschreibt es. |

### 3. Nachweisart je AK
| AK | Nachweis | Ergebnis |
|---|---|---|
| 1 | DEV-Browser + `BfAuftraegeFilterTests` | OK (admin: kein Menü, Umleitung mit Hinweis) |
| 2 | DEV (raw SQL, nicht InMemory) | OK: BG 50000263, FT 50000425/50000335 gefunden; ZK 50000153 und Nicht-Lager 50000169 nicht |
| 3 | DEV | OK (Matchcode, Teileart, Material, Materialnummer, Lagerplatz, Menge = Bestellmenge 20) |
| 4 | Unit `BfAuftragWorkflowTests` + DEV | OK (30 → „ganzes Vielfaches der Bestellmenge 20“, 40 ok, auch beim Schachtler) |
| 5 | DEV | OK („–“, „EILT“ = Default `BfStatuswerte`) |
| 6 | DEV + Unit | OK Sperre ohne Gruppe, `BF00000001…03`; **Gleichzeitigkeit/Unique-Index Manual-UAT offen** |
| 7 | – | NICHT MÖGLICH (`Sync:BfAuftragEmailEnabled` auf DEV verboten) |
| 8 | Unit Filter + DEV | OK: Rolle reporting → 302 auf Liste/Details, kein Menü; lagerbestellung → nur eigene, kein „Auswahl fertig melden“, keine Ändern/Freigeben-Knöpfe, fremder Auftrag 403; schachtler → alle Aufträge mit Aktionen. Direkt-POST als Besteller nicht im Browser ausgeführt (Filter-Unit-Test belegt das Attribut). |
| 9 | Unit + DEV | Gemäß Antwort 2 (H-5); `;`-Sperre wirkt (siehe B-1) |
| 10 | Unit + DEV | OK: Freigabe Di 17.11., Feiertag Mi 18.11. → Schachteln **19.11.2026**, FE **02.12.2026**; Bemerkung BG „EILT - LASERN - 17.11 - 1“, FT „17.11“ |
| 11 | Unit `BfCsvServiceTests` + DEV | OK Datei, Kopf, A–H, CRLF, keine `.tmp`, Position ohne Materialnummer fehlt; **Byte-Gleichheit zu P-1 offen** (Vorlage ist Platzhalter) |
| 12 | Unit + DEV | OK: vorhandene `BF00000002.csv` unverändert, CSV `Fehler` „Datei existiert bereits im Pool – nicht überschrieben.“; nach Löschen „CSV erneut schreiben“ → geschrieben |
| 13 | Unit `Schreibfehler_WiederholungErstNachAbstand` | Netzwerkpfad ohne Recht: Manual-UAT offen |
| 14 | DEV | Einzeln OK (Status Fertig, Audit-Felder, 0 `StockMovements`); **Mehrfachauswahl auf DEV nicht ausgeführt** |
| 15 | DEV + Unit | OK: Besteller storniert eigenen „Bestellt“; Schachtler storniert „Freigegeben“ mit Pool-Hinweis |
| 16 | DEV | Pagination, Filterkarte, Spaltenfilter, CSV-Export-Knopf sichtbar; Filter/Export nicht durchgeklickt |
| 17 | – | NICHT MÖGLICH (braucht Artikel-Abgleich mit `Sync:ArticlesEnabled` + SData IDEAL) |
| 18 | DEV AKE + Build/Tests | OK (AKE 8081: `/BfAuftraege` 302, kein Menülink) |

### 4. Geänderte bestehende Tests
`NachlaufManifestDriftTests` (+`SQL/158`), `AppSettingDefinitionsTests` (Anzahl 60 → 65), `ServiceSettingDefinitionsTests` (+12 InlineData),
`ColumnFilterSyntaxTests` (Client-Modus-Liste + `BfAuftraege/Details`, passt zur Spec), `FakeCurrentUserService` (+2 Interface-Methoden):
alle Katalog-/Typ-Erweiterungen, keine Kategorie „sonstige“.

### 5. Checkliste / ADR 0020
Migration + `SQL/158_AddBfAuftraege.sql` (Guards, eigene Batches, History-Insert getrennt) – Scratch-DB: 1. Lauf legt an, 2. Lauf No-op;
DEV-DBs No-op. FreshInstall an beiden Stellen. AgentJobs nicht betroffen. Audit-Felder gesetzt (DEV: `ModifiedBy` „Gerald Weichbold“ bzw.
`system:bf-csv`). Version 1.99.35 in beiden `AppVersion.cs`, beide Changelogs vorhanden, TS-157 in `docs/TESTSZENARIEN.md` und im Testindex.
Rolle `schachtler` an drei Stellen + Manifest-Reparatur. ADR 0020: Empfängergruppe als Auswahl aus Stammdaten (auf DEV gesehen), Bereiche 0–30/0–60,
FE ≥ Schachtel serverseitig, Warnhinweis bei `BlechfertigungAktiv`; Dienst-Keys im Katalog mit Choice/Muster/Bereich. `.gitattributes` betrifft nur
`IDEALAKEWMSService.Tests/TestData/` (einzige CSV im Repo). Manifest-Einträge (Deploy-Abschnitt) vollständig vorhanden.

### 6. DEV-Deploy und TS-157
- Erhöhte Shell bestätigt. Vor jedem Deploy Zielordner geleert (Web 65, Dienst 72 Einträge; behalten: `App_Data`, `logs`, `appsettings.DEV.json*`,
  `deploy-info.txt`) – **Q-3:** `dev-deploy.ps1` leert selbst nicht.
- Deploy 1 (Stand `9f2a0802`) und Deploy 2 (Stand `c6c04f9f`) je AKE + IDEAL: Exit 0, „HTTP 200, Fusszeile v1.99.35“. Migration
  `20261117154645_AddBfAuftraege` in beiden DEV-DBs, gefilterter Unique-Index `UX_BfAuftraege_Nummer` vorhanden.
- Testdaten DEV IDEAL (Kennung `QA-BF157`): Rolle schachtler (Manifest-SQL), Gruppe „QA-BF157 Lagerteile“ + Empfänger `qa-bf157@example.invalid`,
  6 Sage-Merkmal-Definitionen + Werte für Artikel 445/449/448/441/442, Feiertag 18.11.2026, `BlechfertigungAktiv`/`BfEmpfaengergruppeId`,
  `BfCsvPfad = D:\Dev\wicam-pool\ideal-qa` (nur lokal), `BfCsvAktiv = true`. Rollenwechsel des eigenen DEV-Benutzers (admin → lagerbestellung →
  reporting → schachtler → admin).
- Schritte: Gate aus OK · Suche OK · Mengenregel OK · F1 FEHLER → behoben, nachgeprüft OK · Abschicken gesperrt/OK · Mail NICHT MÖGLICH ·
  Schachtler Menge/Lagerplatz OK · Freigabe mit Sage-Lagerplatz **FEHLER (B-1)**, mit `QA-UEB-1` OK · Termine/Bemerkung OK · CSV + Protokoll
  `BfAuftragCsv` („geschrieben=1, fehler=0“) OK · Kollision + erneut schreiben OK · Fertig OK · Storno beide Wege OK · Rollen OK · AKE OK ·
  Einstellungsmaske OK · V-B NICHT MÖGLICH.
- **Aufgeräumt und belegt:** BfAuftraege/Positionen, Merkmal-Werte/-Definitionen, Gruppe/Empfänger, Feiertag, beide AppSettings-Zeilen,
  Rolle `schachtler` gelöscht (alle Zählungen 0); `BfCsvAktiv = false`, `BfCsvPfad` leer; `Sync:BfAuftragEmailEnabled`,
  `Sync:BlechteileFelderNachlesen`, `SageLagerbuchungAktiv` unverändert false; Benutzer 8 wieder nur `admin`; Ordner `D:\Dev\wicam-pool\ideal-qa`
  entfernt. Die SyncLogs-Einträge `BfAuftragCsv` bleiben als Protokoll stehen. Nichts an Sage geschrieben, keine Test-/Prod-Einstellung berührt.

### 7. Publish
Nicht erstellt (Stand nicht Testbereit, B-1 offen). Nach der B-1-Entscheidung und dem Fix: Zielordner leeren, beide `dotnet publish`-Befehle
aus dem Deploy-Abschnitt, `SQL\` → `publish\SQL`, `scripts\deploy\` → `publish\deploy`, `publish-info.txt`.
Deploy-Komponenten laut echtem Diff bestätigt: Web ja, Dienst ja, Migration SQL/158 ja (Reihenfolge: DB-Backup, Web-Update mit Migration, Dienst).
Nach dem Merge nur neu publishen, wenn der Merge getestete Dateien mit parallelen main-Änderungen zusammenführt.

### Manuelle Checkliste (offen für den Menschen)
1. **B-1 entscheiden:** Format von `ORDER_POSITION` (Lagerplatz mit `;`) – zusammen mit P-1.
2. **P-1:** echte Pool-Datei besorgen, `BfCsv_PlatzhalterBisP1.csv` ersetzen, Byte-Vergleich (AK 11).
3. **P-7:** Schreibrecht des Dienstkontos auf den WiCAM-Pool; Pfad ohne Recht → Fehler + Wiederholung (AK 13).
4. **View-Block 3** (Lagerartikel im WMS) bzw. V-B-Lauf mit `Sync:BlechteileFelderNachlesen` auf Test (AK 17).
5. Fachbereich: Materialnummern in Sage bereinigen (P-3: 4301 GEB+F 1/3, FORMROHR, Jackodur).
6. **Feiertage IDEAL** pflegen (P-5; Manifest `Pruefung/FeiertageIdeal`).
7. Mail beim Abschicken mit `Sync:BfAuftragEmailEnabled` auf Test (AK 7).
8. Zwei gleichzeitige Abschicken → zwei Nummern; Doppelklick Freigabe → eine Freigabe (AK 6, Unique-Index).
9. Mehrfach-Fertigmeldung über die Listen-Checkboxen (AK 14); Liste filtern und exportieren (AK 16).
10. Direkt-POST Freigeben als Besteller → 403 (AK 8, Browser).
11. Gesamtlauf TS-157 auf dem Testsystem nach Deploy (DB-Backup, Web-Update mit Migration 158, Dienst).

## Entscheidung B-1 (Gerald 2026-11-17 18:42)
- Variante **(c)**: Felder mit dem Trennzeichen `;` (z. B. Hauptlagerplatz `S-03;0;0;0` in `ORDER_POSITION`) werden **vorerst** in doppelte Anführungszeichen gesetzt (wie Excel-CSV-Export, enthaltene `"` verdoppelt). Logik kann später umgebaut werden; Bestätigung mit P-1 (Pool-Datei).
- Zusätzlich gewünscht: **WiCAM-Test** – Gerald 18:44: **nur Teil (a)**: Button „Test-CSV herunterladen“ am BF-Auftrag (gleicher Erzeuger wie der Dienst, kein Statuswechsel, nichts in den Pool). Kein Register „WiCAM-Test“ in den Service-Einstellungen; P-7 (Schreibrecht Dienstkonto) bleibt bei Gerald.

## Umsetzung B-1 und Test-CSV (Goal-Lauf 2026-11-17 abends)

Commit `0b31a17d` (v1.99.35, keine neue SQL), Lauf-Notiz [[2026-11-17-goal-lauf-sammel-abend]].
- `IdealAkeWms/Services/BfCsv.cs`: `Feld` fasst `;`/`"` wie Excel ein — im Code markiert „VORLAEUFIG, WIE EXCEL, DURCH P-1 ZU BESTAETIGEN“;
  `UnzulaessigesZeichen` sperrt `;` nicht mehr (weiter `"`, Zeilenumbruch, nicht Windows-1252). `KopfAsync` gemeinsam für Dienst und Web.
- `BfAuftraegeController.TestCsv` (POST, `RequireSchachtlerAccess`): Download `<BF-Nummer>.csv`, kein Statuswechsel, nichts im Pool,
  Aktivitäts-Protokoll `BfAuftragCsv` („Test-CSV … heruntergeladen durch …“, `heruntergeladen = 1`). Kein Register „WiCAM-Test“.
- Tests: `BfAuftragWorkflowTests` (Semikolon sperrt nicht, `Feld`, `KopfAsync`), `BfCsvServiceTests.Inhalt_LagerplatzMitSemikolon_InAnfuehrungszeichen`,
  `BfAuftraegeFilterTests` (TestCsv). TS-157.21–157.23. **P-1 bleibt offen** (WiCAM-Import einer Datei mit eingefasstem Feld).

## QA-Nachweis B-1 + Test-CSV (qa-agent, 2026-11-17 abends)

Geprüft: Commit `0b31a17d` (Bereich `c6c04f9f..8e0a346e`, Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, HEAD `8e0a346e`,
v1.99.36). Review synchron in diesem Lauf, nicht delegiert. Ergebnis: **Testbereit** (B-1 umgesetzt, nichts Blockierendes offen; P-1 bleibt Manual-UAT).

### 1. Build und Tests (ganzer Worktree)
- `dotnet build IdealAkeWms.slnx`: **0 Fehler**, 29 Warnungen (vorbestehend, u. a. CS8620 `BfAuftragWorkflowTests.cs:321`, NU1902/1903).
- `dotnet test`: `IDEALAKEWMSService.Tests` **715 bestanden / 0 übersprungen / 0 Fehler**; `IdealAkeWms.Tests` **3229 bestanden / 1 übersprungen / 0 Fehler** (3230).

### 2. Code-Review `0b31a17d`
Gelesen: `BfCsv.cs` (ganz), `BfAuftraegeController.TestCsv` + Klassenfilter, `SageBookingWorker` (Kopf über `KopfAsync`), `Details.cshtml`, `BfAuftrag.Uebergeben`
(`Models/BfAuftrag.cs:95`), `SyncLogServices.BfAuftragCsv`, Tests.

| # | Befund | Schwere | Fundstelle | Behandlung |
|---|--------|---------|------------|------------|
| R1 | Test-CSV prüft `FreigabeFehler` nicht: ein Zeilenumbruch in Matchcode/Bemerkung würde im Download die Zeile brechen (die Freigabe lehnt ihn ab; `"` fasst `Feld` korrekt ein, nicht-1252 wird gemeldet) | niedrig | `BfAuftraegeController.cs` `TestCsv` | Hingenommen: nur Download, nichts im Pool; Freigabe bleibt die Sperre. |
| R2 | Knopf/Aktion auch bei stornierten und fertigen Aufträgen (Bedingung nur `Nummer is not null`) | Hinweis | `Details.cshtml` (`Model.IstSchachtler && a.Nummer is not null`) | Hingenommen, harmlos (kein Statuswechsel). |
| R3 | `KopfAsync` liest die Priorität jetzt per `int.TryParse(InvariantCulture)` statt `GetIntSafeAsync`; leer/ungültig = Default 1 | Hinweis | `BfCsv.cs` `KopfAsync` | Gleichwertig; durch `KopfAsync_LeerUndUngueltigGebenDefault` belegt. |

Sicherheit Test-CSV: `[HttpPost]` + `[ValidateAntiForgeryToken]` + `[RequireSchachtlerAccess]` + Klasse `[RequireBfBestellenAccess]`/`[RequireBlechfertigungAktiv]`;
Dateiname aus der serverseitigen BF-Nummer; kein Pfad, kein Pool-Zugriff, kein Statuswechsel; Audit im Aktivitäts-Protokoll. Pflicht-Fehlerklassen: kein
`<script>`/`@Html.Raw`, kein `hidden`+`d-*`, kein stiller Fallback (Kopf-Defaults sind die dokumentierten Spec-Defaults), keine parallelen Arrays, Regel
„`;` erlaubt, `"`/Umbruch/nicht-1252 gesperrt“ an einem Ort (`UnzulaessigesZeichen` + `Feld`), Dienst und Web teilen `Inhalt`/`KopfAsync`/`Kodierung`.

### 3. Nachweisart
| Punkt | Nachweis |
|---|---|
| `;` sperrt Freigabe nicht | automatisch (`FreigabeFehler_SemikolonImLagerplatzSperrtNicht`) + DEV (TS-157.21) |
| Einfassung wie Excel | automatisch (`Feld_FasstSemikolonWieExcelEin`, `Inhalt_LagerplatzMitSemikolon_InAnfuehrungszeichen`) + DEV-Datei |
| Test-CSV nur Schachtler | automatisch (`BfAuftraegeFilterTests` `TestCsv`) für den Filter; Besteller-Browserlauf Manual-UAT |
| Test-CSV ohne Statuswechsel, Protokoll | DEV (Manual) |
| WiCAM liest das eingefasste Feld | **Manual-UAT P-1** (nicht automatisierbar) |

### 4. Geänderte bestehende Tests
- `BfAuftragWorkflowTests` – `InlineData("Matchcode", "A;B", "Semikolon")` entfernt: **Verhalten absichtlich geändert** (Entscheidung B-1).
- `BfAuftraegeFilterTests` – `InlineData("TestCsv", true)` ergänzt: Erweiterung.
- `docs/TESTSZENARIEN.md` TS-157.9 auf `A"B` umgestellt: absichtlich (B-1). Keine Kategorie „sonstiges“.

### 5. Checkliste / ADR 0020
Keine Migration, keine neue/geänderte Einstellung (ADR 0020 n/a), Version 1.99.35 in beiden `AppVersion.cs` (danach 1.99.36), Anwender-Changelog,
Brain-Changelog `2026-11-17-v1-99-35-blechfertigung-bf-auftraege`, TS-157.21–157.23 in `docs/TESTSZENARIEN.md` und im Testindex. Kein neuer
Deploy-Schritt für `scripts/deploy/nachlauf.psd1` (Manifesttext `-BfCsv` nennt P-1 bereits).

### 6. DEV-Deploy und TS
Deploy `D:\Dev\scripts\dev-deploy.ps1 -Quelle <Worktree>` (elevated) nach grünem Build, Zielordner `D:\Dev\{AKE,IDEAL}\{Web,Service}` vorher geleert
(erhalten: `App_Data`, `logs`, `appsettings.DEV.json*`): **AKE** Exit 0 „HTTP 200, Fusszeile v1.99.36“; **IDEAL** Exit 0 „HTTP 200, Fusszeile v1.99.36“.
DEV-Absicherung lief je Standort. Testdaten DEV IDEAL: `BlechfertigungAktiv` (Ausgangswert: Zeile fehlte) temporär `true`, Auftrag `BF99990001`
(Id 4, 2 Positionen, Pos 1 Lagerplatz `S-03;0;0;0`) und Entwurf Id 5 per SQL angelegt; **nach dem Test gelöscht, Gate-Zeile entfernt**. `BfCsvAktiv`
blieb `false`, `BfCsvPfad` leer – nichts in einen Pool geschrieben, nichts an Sage.

| TS | Durchgeführt | Ergebnis | Status |
|---|---|---|---|
| 157.21 | Auftrag mit `S-03;0;0;0` geöffnet und freigegeben | kein Sperrblock, Freigabe ok (Schachteln 18.11.2026, FE 01.12.2026, Bemerkung `17.11 - 1`); Datenzeile `ABDECKUNG QA;1.4301;2;17.11 - 1;"S-03;0;0;0";QA Tester;18.11.2026;2026-12-01` (8 Spalten) | OK (CSV aus Download; Dienst-Pooldatei NICHT MÖGLICH, `BfCsvAktiv` aus) |
| 157.22 | Test-CSV im Status bestellt und freigegeben | `BF99990001.csv` 404 Byte, CRLF, keine Byte > 127; bestellt: Termine/Bemerkung leer + Hinweis am Knopf; Status/CSV-Spalte unverändert (1/0 bzw. 2/1, `CsvDatei` NULL); `SyncLogs` `BfAuftragCsv`: „Test-CSV BF99990001.csv heruntergeladen durch Gerald Weichbold (AKENB\gerald.weichbold) – kein Statuswechsel …“ + `heruntergeladen=1` (2 Läufe) | OK; „byte-gleich zur Pool-Datei“ NICHT MÖGLICH (kein Pool auf DEV) – Code-Weg identisch (`Inhalt`/`KopfAsync`/`Kodierung`) |
| 157.23 | Entwurf ohne Nummer geöffnet | Warenkorb-Ansicht, kein Knopf | OK; Besteller ohne schachtler + Direkt-POST NICHT MÖGLICH (eine Windows-Identität mit admin) – Filter automatisch belegt |

### 7. Publish
**Nicht erstellt** – Auftrag des Aufrufers: „kein Publish (Publish Paket 1 mache ich danach selbst)“.

### Deploy (aus dem echten Diff `0b31a17d`)
Web ja (Controller, View, `BfCsv`), Dienst ja (`SageBookingWorker` nutzt `BfCsv.KopfAsync`), Migration nein. Zusammen mit dem Bündelstand v1.99.36
ausliefern (Migration SQL/158 aus v1.99.35 bleibt: DB-Backup, Web-Update mit Migration, Dienst). Nach dem Merge nur neu publishen, wenn der Merge
getestete Dateien mit parallelen main-Änderungen zusammenführt.

### Manuelle Checkliste (offen, ergänzt die obige)
1. **P-1:** echte Pool-Datei / WiCAM-Import einer Datei mit `"S-03;0;0;0"` – bestätigt die vorläufige Einfassung oder `BfCsv.Feld` wird umgebaut.
2. Test-CSV auf dem Testsystem byte-gleich mit der Dienst-Datei desselben Auftrags vergleichen (mit `BfCsvAktiv` + Testpfad).
3. Besteller ohne schachtler: kein Knopf, Direkt-POST `/BfAuftraege/TestCsv` → verweigert (TS-157.23).
4. Punkte 2–11 der Checkliste oben (P-7, Mail, Doppelklick, Feiertage, Gesamtlauf TS-157 auf Test) bleiben offen.

## Befund P-1 (Koordination 2026-11-18 09:55) – Pool-Datei `1068259-MC.CSV` von Gerald
Datei aus dem A-Pool (10.587 Bytes), **nicht** aus der BF-Lagerbestellung, sondern aus der Stücklisten-Kommissionierung (FA 1068259, Kunde „MC“,
`Kom-Ablageort …\04_Stücklisten-Kommisionierung`). Belegt:
- **Kodierung Windows-1252** (ä = 0xE4, ü = 0xFC), kein BOM; **Zeilenende CRLF**; **jede Zeile 28 Felder** (27 `;`), auch Leerzeilen;
  Datei auf **200 Zeilen aufgefüllt** (`;;;…;` bis Zeile 200).
- **Kopf PART_START … PART_END in Zeilen 1–15, Schlüssel in Spalte B, Wert in Spalte C** (Spalte A leer; `PART_START`/`PART_END` in Spalte A).
  Kopfschlüssel hier: CREATE_NEW_ORDER YES, ORDER_NUMBER (= FA-Nr.), **ORDER_POSITION (= Kunden-Kürzel „MC“)**, ORDER_DATE `TT.MM.JJJJ`,
  ORDER_REMARK_5 `JJJJ-MM-TT`, ORDER_REMARK_2 (Abteilung K-02), ORDER_REMARK_4 (Bearbeiter-Kürzel), ORDER_REMARK_3/1/8 `*HEADER_LINE_19/17/26`,
  ORDER_PRIORITY 1, ORDER_REMARK_7 (Partie GN), **ORDER_RELEASE NO**.
- Zeile 16 leer, **Zeile 17 Positions-Kopf** `ORDER_NC_FILE;ORDER_NC_ARCHIV;ORDER_MATERIAL_NO;ORDER_THICKNESS;ORDER_AMOUNT;ORDER_REMARK_2;ORDER_REMARK_8;NESTING_ROTATION;ORDER_REMARK`,
  Zeilen 18–19 Excel-Hilfstexte (deutsche Überschriften), **Positionen ab Zeile 20** (Artikel, Archiv leer, Material-Nr., Dicke, Menge, Lagerplatz-Kürzel
  wie `S-01`/`H2-02`, …) plus Hilfsspalten rechts.
- **Excel fasst ein Feld mit `;` in `"…"` ein und verdoppelt enthaltene `"`** (Zeile 2, Formeltext) → stützt B-1 Variante (c).
- **Sicherheitsbefund:** In den Excel-Hilfsspalten (Zeilen 6/7, Spalte AA/AB) stehen **Benutzername und Kennwort der WiCAM-Datenbank im Klartext**; sie
  landen in jeder Pool-Datei. Das WMS schreibt keine Hilfsspalten. Empfehlung: aus der Excel-Vorlage entfernen, Kennwort ändern. Datei deshalb
  **nicht** unverändert als Testvorlage ins Repo (nur bereinigt).
- **Abweichung zur Vorlage im Lösungsentwurf** (Schlüssel in A, Kopf ohne POSITION/DATE/REMARK_5/REMARK_4, Positionen A–H mit anderen Spalten):
  erheblich. **Offen:** Ist die Datei der BF-Lagerbestellung (BF*.csv) gleich aufgebaut? → eine echte `BF*.csv` beschaffen (P-1 bleibt offen), dann
  Vorlage im Code angleichen (Dev-Lauf).

## Befund P-1 (2) – echte BF-Dateien (Gerald 2026-11-18 15:14): BF00000001.csv (Excel-Makro, Pool) und BF00000002.csv, Makro `VL_KO-014_ALOOP-Auftragspool-Lagerteile-füllen-WMS.xlsm`
Byte-genau ausgewertet (Koordination 15:20):
- **BF00000001.csv** (Excel `SaveAs xlCSV, Local:=True`, VBA `ErstelleCSV`, Dateiname = Zelle C3 + „.csv“ nach `N:\u\pn\apools\`): Windows-1252, CRLF, kein BOM,
  **25 Felder je Zeile**, **94 Zeilen** (Tabellenbereich bis Zeile 94 aufgefüllt), Hilfsspalten J–Y (Ablagepfade, Import-Tabelle).
- **BF00000002.csv**: gleicher Aufbau, aber **13 Felder**, **16 Zeilen**, ohne Hilfstexte (vermutlich bereinigte Fassung – Herkunft offen).
- **Aufbau (beide):** Z1 `PART_START` (Spalte A); Z2–8 **Schlüssel in Spalte B, Wert in Spalte C**: CREATE_NEW_ORDER YES, ORDER_NUMBER `BF0000000x`,
  ORDER_REMARK_3/1/2 `*HEADER_LINE_19/17/18`, ORDER_PRIORITY 1, **ORDER_RELEASE YES**; Z9 `PART_END`; Z10 leer;
  **Z11** `ORDER_NC_FILE;ORDER_MATERIAL_NO;ORDER_AMOUNT;ORDER_REMARK;ORDER_POSITION;ORDER_REMARK_2;ORDER_REMARK_4;ORDER_DATE;ORDER_REMARK_5;ORDER_REMARK_1`
  (**10 Spalten**); Z12 leer (bzw. Hilfstext rechts); **Z13 deutsche Überschrift** `Matchcode;Mat.Nr.;Menge;Bemerkung;Lagerplatz;Hauptlagerplatz;Besteller;Schachteltermin;FE-Termin;Arbeitsschritte`;
  **ab Z14 Positionen**.
- **Positionswerte:** Matchcode GROSS (TRIM/CLEAN/UPPER); Mat.Nr. **ganzzahlig** („1“, „3“); Menge ganzzahlig; Bemerkung `[Status - ]TT.MM[ - Pos bei BG]`
  („18.11 - 1“, „EILT - 18.11“, „18.11“); **ORDER_POSITION = voller Lagerplatz mit Kommas** („K-02,0,0,0“, „H4-02,0,0,0“ – kein `;`, keine Anführungszeichen);
  **ORDER_REMARK_2 = Hauptlagerplatz** („K-02“); **ORDER_REMARK_4 = Besteller-Kürzel** („WAK“, „MAW“); ORDER_DATE = Bestelldatum + 1 Arbeitstag `TT.MM.JJJJ`;
  ORDER_REMARK_5 = Bestelldatum + 10 Arbeitstage `JJJJ-MM-TT`; **ORDER_REMARK_1 = Arbeitsschritte** („S-01“, „ST EG KA“).
- **Abweichungen WMS v1.99.36ff (`IdealAkeWms/Services/BfCsv.cs:110-131`):** Schlüssel in Spalte A statt B; Z10/Z12/Z13 fehlen; nur 8 Positionsspalten
  (es fehlen ORDER_REMARK_2 Hauptlagerplatz und ORDER_REMARK_1 Arbeitsschritte, Reihenfolge anders); ORDER_POSITION = Hauptlagerplatz statt voller
  Lagerplatz; Lagerplatz-`;` in Anführungszeichen statt **Komma**; Besteller = Name statt Kürzel; keine feste Feldanzahl.
- **Folge:** B-1 Variante (c) wird durch **„`;` → `,`“** ersetzt (so steht es in der echten Datei). Antwort 4 der Walter-Mail („Arbeitsschritte nicht in die CSV“)
  ist überholt – die WMS-Excel schreibt sie je Position (Spalte J). Offen: Feldanzahl (13 wie BF00000002 genügt?), Besteller-Kürzel-Quelle.
- **Gerald 2026-11-18 15:20:** Besteller-Kürzel ist ein Personenkürzel (z. B. **WAK = Walter Kronberger**); **wird im WMS je Benutzer definiert** (neues Feld, kein Sage-Wert). CSV-Spalte ORDER_REMARK_4 = dieses Kürzel; fehlt es → Hinweis bei der Freigabe.
- **Walter (über Gerald 2026-11-18 15:28):** **BF00000002 ist die Zielform**; Lagerplatz **`;` → `,`** bestätigt. Neue Logik von Benjamin (BG bestellen, FT-Einzelteile an WiCAM,
  BF-Auftrag wie FA anzeigen) → eigener Backlog [[2026-11-18-bf-auftrag-baugruppen-aufloesen-und-als-fa-zeigen]].

## Nachtrag v1.99.42 (Goal-Lauf [[2026-11-18-goal-lauf-nachbesserungen-v1-99-42]])

Umgesetzt nach Befund P-1 (2) + Walter 15:28 und Gerald 15:20 (Kürzel je WMS-Benutzer): Rumpf „CSV-Aufbau“ und F3 oben nachgezogen.
B-1 Variante (c) (Anführungszeichen) ist durch „`;` → `,`“ ersetzt; `;` sperrt außerhalb des Lagerplatzes wieder. Commits `1a972e72`
(Kürzel, SQL/162), `9fd5181d` (CSV, SQL/163, Testvorlage `BF00000002.csv`, Platzhalter-Vorlage entfernt). TS-157.9/.11/.21 angepasst, neu TS-163.1–163.6.
Offen bleibt nur der echte WiCAM-Import (Manual-UAT 163.1). Status unverändert bis zum QA-Lauf.

## QA-Nachweis v1.99.42 (qa-agent, 2026-11-18)

Gemeinsamer Lauf über die Nachbesserungen v1.99.42 (Changelog [[2026-11-18-v1-99-42-nachbesserungen]]), Diff `9b6b3e0d..0af91b73` (7 Commits)
im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, plus QA-Fix `b5463336`. Selbst in diesem Lauf geprüft, nicht delegiert.
Derselbe Block steht in allen vier betroffenen Specs (BF-CSV, Beschichtungsauftrag-Ausbau, Sage-Userfelder, Sage-AG-Anlage).

**1. Build und Tests** (Stand `b5463336`): `dotnet build IdealAkeWms.slnx` 0 Fehler, 30 Warnungen (neu: CS8604 `BfCsv.cs:124`, siehe H2).
`dotnet test`: IdealAkeWms.Tests **3291 bestanden / 1 übersprungen / 0 Fehler** (vor dem QA-Fix 3288/1/0); IDEALAKEWMSService.Tests **790/0/0**.

**2. Review** (Diff samt Umfeld gelesen: `BfCsv`, `BfAuftragWorkflow.FreigabeFehler/FreigebenAsync`, `BfAuftragRepository`, `UsersController.KuerzelPruefenAsync`,
`BeschichtungsauftragWorkflow.BearbeitenAsync`, `Print.cshtml`, `_PrintBriefkopfFuss.cshtml`, `SageUserfelder.Erklaeren` mit allen Aufrufern,
`SageFaArbeitsgangService` Kandidaten/Nachlesen, `SageFaArbeitsgangAnlageRepository.UnklarBestaetigenAsync`, View `SageFaArbeitsgaenge/Index`,
SQL/162, SQL/163, Einmal-Skript, FreshInstall, Manifest, beide Migrationen):

| Nr | Schwere | Befund | Fundstelle | Behandlung |
|---|---|---|---|---|
| F1 | mittel | `IstUnklar` prüfte `StartsWith(UnklarText)`, und genau dieser Text wurde in v1.99.42 geändert. Unklar-Zeilen, die v1.99.38–1.99.41 mit dem alten Text gespeichert haben, wären nach dem Update **automatisch erneut gesendet** worden (Dublette in Sage) und hätten keinen Bestätigen-Knopf bekommen | `IdealAkeWms/Services/SageFaArbeitsgaenge.cs:37-41` (vorher), `Views/SageFaArbeitsgaenge/Index.cshtml:164` | **behoben** `b5463336`: gemeinsamer Anfang `UnklarAnfang = "Antwort unklar"` für Dienst-Regel, Repository und View; Test `SageFaArbeitsgaengeControllerTests.IstUnklar_ErkenntAltenUndNeuenText` (alter Text, neuer Text, echter Fehler). Auf DEV IDEAL nachgeprüft (163.12) |
| H1 | Hinweis | EF-Migration `20261118145602_AddBfPositionArbeitsschritte` legt nur die Spalte an; den Nachtrag der Arbeitsschritte macht allein `SQL/163` | Migration, `SQL/163_AddBfPositionArbeitsschritte.sql` | angenommen: Update-Skript führt SQL/163 in der Schema-Phase aus; das UPDATE läuft auch nach EF-Migrate (auf DEV belegt: 2 Positionen nachgetragen, getrimmt). Kein Fix |
| H2 | Hinweis | neue Warnung CS8604 `Z("", "ORDER_NUMBER", a.Nummer)` mit `string?` | `IdealAkeWms/Services/BfCsv.cs:124` | kosmetisch (freigegebene Aufträge haben immer eine Nummer). Offen |
| H3 | Hinweis | Test-CSV vor der Freigabe hat `ORDER_REMARK_4` leer: Kürzel wird erst bei der Freigabe festgeschrieben (wie Termine/Bemerkung) | `BfAuftragWorkflow` Freigabe, `BfAuftraegeController` Test-CSV | gewollt (Snapshot der Freigabe). Kein Fix |
| H4 | Hinweis | `[RegularExpression]` für das Kürzel läuft vor dem Trimmen: „ WAK“ mit Leerzeichen wird abgelehnt statt getrimmt | `UserEditViewModel` Kuerzel | harmlos (klare Meldung). Kein Fix |
| H5 | Hinweis | Einmal-Skript zeigt in der Zählung `NULL` statt `0`, wenn kein Altauftrag passt (DEV AKE) | `SQL/Einmalig/2026-11-18_v1.99.42_BeschichtungsauftragKreditorNachtragen.sql` Zählblock | kosmetisch; maßgeblich `TREFFER: 0` stimmt. Kein Fix |

Ohne Befund geprüft: keine Werte in `<script>`-Blöcken, kein `hidden` neben `d-*`, kein neues `?? 0`, keine Parallel-Arrays; Kürzel-Eindeutigkeit serverseitig
(`KuerzelPruefenAsync` + gefilterter UNIQUE-Index `UQ_Users_Kuerzel`); Kürzel-, Semikolon- und Ganzzahl-Sperre serverseitig in **einer** Funktion `FreigabeFehler`
(Anzeige und Freigabe); Kreditor-Nachtrag serverseitig (schon gesetzt / kein aktiver Beschichter → abgelehnt); Einmal-Skript-Regel = `KreditorVorbelegen`
(zwei Orte, gleiche Regel, im Skript-Kopf benannt); `Bestaetigen` nur Admin (Class-Level `RequireAdminAccess`), schreibt nichts an Sage.
Audit: Users (Edit), BF-Freigabe, Beschichtungsauftrag (`Geaendert`), Sage-AG-Bestätigen, Einmal-Skript – auf DEV in der DB gesehen.
Migrations-Disziplin: SQL/162 + SQL/163 mit `COL_LENGTH`/`sys.indexes`-Guards, DDL je Batch, History separat; FreshInstall an **beiden** Stellen (Spalten
`Users.Kuerzel`, `BfAuftraege.BestellerKuerzel`, `BfAuftragPositionen.Arbeitsschritte`, Index, beide `MigrationId`); AgentJobs nicht betroffen; nicht daten-destruktiv.
Manifest `nachlauf.psd1`: SQL/162, SQL/163, Einmal-Skript (Trockenlauf, `Braucht` SQL/161), BfCsv-Text nachgezogen; `NachlaufManifestDriftTests` grün.
Version 1.99.42 in beiden `AppVersion.cs`, Anwender-Changelog, TS-163 in `docs/TESTSZENARIEN.md` + Index-Zeile 163. ADR 0020: keine neue/geänderte Einstellung.

**3. Nachweisart:** automatisch belegt: CSV-Aufbau/Kodierung (`BfCsvServiceTests`), Freigabe-Sperren (`BfAuftragWorkflowTests`), Kürzel-Prüfung (`UsersControllerTests`),
Kreditor-Nachtrag (`BeschichtungsauftragWorkflowTests`), Userfeld-Meldung (`SageUserfelderTests`), Unklar-Regel (`SageFaArbeitsgangServiceTests`, `SageFaArbeitsgaengeControllerTests`).
Manual-UAT (nicht als grün gezählt): UNIQUE-Index, SQL/162/163, Einmal-Skript, echtes Model-Binding der Formulare, Druck/PDF-Seitenzähler, WiCAM-Import, echtes SData.
Die Manual-UAT-Teile sind unten auf DEV abgearbeitet, soweit möglich.

**4. Geänderte bestehende Tests** – alle *Verhalten durch die Nachbesserung gewollt geändert*, kein „Fixture bis grün“:
`BfCsvServiceTests` (Vorlage → echte `BF00000002.csv`; Semikolon-Test → `Lagerplatz_KommaStattSemikolon`; Kodierungs-Test von BestellerName auf Arbeitsschritte,
weil der Name nicht mehr in die CSV geht) – P-1 (2); `BfAuftragWorkflowTests` (Fixture-Benutzer mit Kürzel, Materialnummer „M-1“ → „11“ wegen Ganzzahl-Sperre,
Semikolon-Test umbenannt/angepasst, Test für entfernte `Feld()` gelöscht) – P-1 (2) + Kürzel; `SageFaArbeitsgangServiceTests.Nachlesen_KeinFund_…` (Fehler → unklar) – N1;
`NachlaufManifestDriftTests` (SQL/162, SQL/163 ergänzt) – Liste.

**6. DEV-Deploy** (erhöhte Shell; Zielordner `D:\Dev\{AKE,IDEAL}\{Web,Service}` vorher geleert, erhalten `appsettings.DEV.json*`, `logs`, `App_Data`):
`dev-deploy.ps1 -Quelle <Worktree>` je Standort zweimal (Stand `0af91b73`, dann `b5463336`): **AKE** Exit 0 „HTTP 200, Fusszeile v1.99.42“,
**IDEAL** Exit 0 „HTTP 200, Fusszeile v1.99.42“; DEV-Absicherung lief je Standort. Beide Migrationen in beiden DEV-DBs; danach `SQL/162` und `SQL/163`
per `sqlcmd` je DB: Exit 0, idempotent (Spaltenbreiten 20/20/400 Byte, `UQ_Users_Kuerzel` gefiltert `[Kuerzel] IS NOT NULL`).

TS-163 auf DEV IDEAL (Testdaten Kennung `QA-163`; `BfCsvPfad = D:\Dev\wicam-pool\ideal-qa`, `BfCsvAktiv = true` nur für den Test):

| TS | Durchgeführt | Ergebnis | Status |
|---|---|---|---|
| 163.1 | `BF99990163` freigegeben, Dienst schrieb `BF99990163.csv` (792 Byte, `CsvStatus` 2) | 15 Zeilen, nur CRLF, jede Zeile 12 `;`, keine `"`, nur ASCII; Zeilen 1–13 byte-gleich zur Vorlage `BF00000002.csv` bis auf die Auftragsnummer; Position ohne Materialnummer fehlt | OK (Byte-Aufbau); WiCAM-Import **NICHT MÖGLICH** (kein Pool, Manual-UAT) |
| 163.2 | Datenzeilen geprüft | `G-KT-I-BLENDE-SW-80-510-V01;3;2;EILT - 18.11 - 1;K-02,0,0,0;K-02;WAK;19.11.2026;2026-12-02;ST EG KA;;;` und `G-TK-I-BLENDE-SW-80-710-V01;1;5;18.11;H4-02,0,0,0;H4-02;WAK;19.11.2026;2026-12-02;LA KA;;;` (Arbeitsschritte aus SQL/163-Nachtrag, getrimmt) | OK |
| 163.3 | `BF99990164` geöffnet | Block nennt „Pos 1 Matchcode: Semikolon“, „Pos 2: Materialnummer „1,5“ ist nicht ganzzahlig“ und das fehlende Kürzel | OK |
| 163.4 | Testuser Kürzel `wak` gespeichert, Liste `?colf_kuerzel=wak` | DB `WAK`, Audit gesetzt; Spalte „Kürzel“, Filter liefert genau Testuser | OK |
| 163.5 | zweiter Benutzer `WAK`, dann `A B`, dann `zu-lang-12345` | „Kürzel WAK ist schon an Testuser vergeben.“; Formatmeldung „nur Buchstaben und Ziffern, höchstens 10 Zeichen“; `maxlength=10` kappt; nichts gespeichert | OK |
| 163.6 | Freigeben ohne Kürzel; Kürzel nachgetragen; erneut freigeben | erst „Freigabe von BF99990163 abgelehnt: Besteller Testuser hat kein Kürzel …“, Status bleibt, nichts geändert; dann freigegeben ohne Neuabschicken, Details „Testuser (WAK)“, `BestellerKuerzel` WAK | OK |
| 163.7 | `BA-2026-00006` (Übergeben) per SQL auf `CreditorId NULL`, Dienstleister `qa-lack1 `; Auswahl „Lackierer (Kreditor) nachtragen“ → QA-LACK1, Speichern | `CreditorId` 2289, Dienstleister „QA Lackiererei Voll GmbH“, RAL/Termine/Positionen unverändert, Audit gesetzt; Auswahl danach weg | OK |
| 163.8 | – | Direkt-POST ohne Entwicklertools nicht ausführbar; Leser ohne Schreibrecht nicht prüfbar (nur eine Admin-Identität) | NICHT MÖGLICH – Server-Regel automatisch belegt (`Kreditor_InUebergeben_NurNachtragen_SolangeLeer`) |
| 163.9 | Einmal-Skript **nur Trockenlauf** auf DEV IDEAL und AKE | IDEAL: `BA-2026-00006 → 2289 QA Lackiererei Voll GmbH`, `TREFFER: 1`, nichts geändert; stornierte Aufträge mit passendem Kurznamen nicht dabei. AKE: `TREFFER: 0`. Nach 163.7 zweiter Trockenlauf IDEAL `TREFFER: 0` | OK; Scharflauf bewusst nicht |
| 163.10 | `BA-2026-00007` mit 36 Positionen (32 temporär kopiert): PDF-Knopf `/Beschichtungsauftraege/Pdf/7` und Druck über Chrome-Druckmodul (`--print-to-pdf` der Druckansicht) | Barcode klein rechts neben dem Titel, nur waagrechte Linien, Tabellenkopf auf Seite 2 wiederholt, „Seite 1 von 2“ / „Seite 2 von 2“ in **Druck und PDF**; DEV ohne Briefkopf-Vorlage → Zählung auch ohne Fußbereich; einseitig „Seite 1 von 1“ | OK |
| 163.11 | – | braucht Abgleich gegen SData (`Sync:*Enabled` auf DEV verboten) | NICHT MÖGLICH – automatisch belegt (`SageUserfelderTests.Erklaeren_LangeSqlDiagnose_HinweisVorn_AusDiagnose_NieDoppelt`) |
| 163.12 | Warteschlange FA 1060414 per SQL: `PU/180` Fehler mit **altem** Unklar-Text, `IS/200` mit neuem, `VL/190` „HTTP 400“; Seite `?fa=1060414`; Knopf bei PU | Knopf an beiden Unklar-Zeilen, keiner bei HTTP 400; Klick → „FA 1060414: Arbeitsgang 180 als „in Sage vorhanden“ bestätigt“, DB Status 4 (InSageVorhanden), Audit gesetzt. Schalter blieb aus, nichts an Sage | OK (Umlaute der per sqlcmd eingefügten Testtexte verstümmelt – Testdaten-Artefakt, nicht App) |

Nachweise 163.10 im Hauptcheckout `secondbrain/tests/2026-11-18-qa-v1-99-42-druck-pdf/`: `ts-163-10-druck-chrome-ba-2026-00007-2-seiten.pdf` (Druck),
`ts-163-10-pdf-wms-ba-2026-00007-2-seiten.pdf` (PDF-Knopf), `ts-163-10-pdf-wms-ba-2026-00007-1-seite.pdf`, `ts-163-10-druckansicht-bildschirm-ba-2026-00007.png`
(Bildschirmansicht). Bildschirmfotos aus dem Browser-Tab scheiterten am Renderer-Timeout der Erweiterung; Druck und PDF liegen daher als gerenderte PDF-Dateien vor.

**Aufgeräumt (DEV IDEAL, per Abfrage belegt):** BF-Aufträge/-Positionen 0, Merkmal-Definition/-Werte `QA-163` gelöscht, `BfCsvAktiv = false`, `BfCsvPfad` leer,
Ordner `ideal-qa` entfernt, Testuser-Kürzel NULL, `BA-2026-00006` zurück auf `CreditorId 2290` / „QA Lackierer ohne Anschrift“, temporäre Positionen an
`BA-2026-00007` gelöscht, `OperationConfigs` PU/IS/VL `SageArbeitsgangnummerManuell` NULL, Sage-AG-Warteschlange leer. `SageLagerbuchungAktiv`,
`SageFaArbeitsgaengeAnlegenAktiv`, alle `Sync:*Enabled` unverändert aus. SyncLogs bleiben. Nichts an Sage geschrieben, kein Test-/Produktivsystem berührt.

**7. Publish:** in diesem Lauf nicht erstellt – der Aufrufer macht Paket 7 selbst (`publish\` nicht angefasst).

**Deploy (aus dem echten Diff):** Web ja, Dienst ja (`SageFaArbeitsgangService`, Sdata-Abgleiche mit `Erklaeren(ex)`, `BfCsvService` über `BfCsv`),
Migration **ja** (SQL/162, SQL/163; additiv, SQL/163 mit Daten-Nachtrag). Reihenfolge: DB-Backup → SQL/162, SQL/163 (Update-Skript Schema-Phase) → Web → Dienst →
Einmal-Skript Kreditor im Trockenlauf, Scharflauf nur nach Freigabe der Liste + Backup. Vor `BfCsvAktiv`: Kürzel aller BF-Besteller pflegen.
Nach dem Merge nur neu publishen, wenn der Merge getestete Dateien mit parallelen main-Änderungen zusammenführt.

**Manuelle Checkliste (offen, für den Menschen):**
1. 163.1 WiCAM-Import einer vom Dienst geschriebenen Datei am echten Pool (Testsystem IDEAL), Vergleich mit `BF00000002.csv`.
2. 163.8 Direkt-POST mit zweitem Kreditor bzw. Nicht-Beschichter; Leser ohne Schreibrecht sieht keine Auswahl.
3. 163.9 Scharflauf des Einmal-Skripts auf dem Testsystem (nach Backup, über das Update-Skript), danach Trockenlauf `TREFFER: 0`.
4. 163.10 Druck in Edge am Drucker-Dialog sichtbar prüfen (hier über das Chrome-Druckmodul belegt).
5. 163.11 falsche Userfeld-Einstellung gegen echtes SData (Testsystem) je Abgleich, danach zurücksetzen.
6. 163.12 Sage-AG unklar im echten Ablauf; E1b (Schreibtest am IDEAL-Ziel) bleibt offen.
7. Kürzel für alle BF-Besteller vor Go-live pflegen.
8. Testsystem-Durchlauf TS-163 komplett mit dem Publish-Paket, danach Merge (Schranke 2).

## Antworten Walter 2026-11-19 (über Gerald 06:52) auf die Mail vom 18.11.
1. **Zielform:** BF00000001 und BF00000002 importieren in WiCAM gleich; die Leerzeilen/rechten Spalten entstehen nur durch Excel-Formeln
   beim Speichern, der Import endet an der ersten leeren Zeile. **BF00000002 bevorzugt** → entspricht v1.99.42 (Byte-Test gegen die Vorlage).
2. **Lagerplatz:** `;` gilt beim Importer als Trenner → **`;` durch `,` ersetzen** bestätigt → entspricht v1.99.42.
3. **Arbeitsschritte:** **aus Sage befüllen**, Sage ist führend (seit 18.11. geändert) → entspricht v1.99.42 (Snapshot `USER_OSArbeitsschritte`
   beim Hinzufügen der Position). Offen: Ändert sich das Merkmal nach dem Hinzufügen, bleibt der alte Wert im Warenkorb/Auftrag.
4. **ORDER_RELEASE YES:** Walter dafür (Freigabe passiert im WMS über den Export), **Entscheidung bei Andi (Andi.Peric) offen**; Einstellung
   `BfCsvOrderRelease` (Default YES) deckt beide Fälle ohne Code ab. Walters Frage „welche Datei hatte NO?“: `1068259-MC.CSV` aus dem A-Pool
   (Stücklisten-Kommissionierung FA 1068259, Kunde MC), siehe Befund P-1 oben.

