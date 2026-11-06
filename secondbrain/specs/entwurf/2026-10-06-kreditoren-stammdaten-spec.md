---
type: spec
title: Kreditoren-Stammdaten (Dienstleister/Beschichter) — Stammdatenbereich im WMS, Sync vorbereitet
slug: 2026-10-06-kreditoren-stammdaten-spec
status: Entwurf
created: 2026-10-06
updated: 2026-10-06
source_backlog: "[[2026-10-06-kreditoren-stammdaten]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - "NEU IdealAkeWms/Models/Creditor.cs (erbt AuditableEntity, Models/AuditableEntity.cs:3-11)"
  - "NEU IdealAkeWms/Data/Repositories/ICreditorRepository.cs + CreditorRepository.cs (Registrierung wie Program.cs:66)"
  - "NEU IdealAkeWms/Controllers/CreditorsController.cs (Vorlage ProductionWorkplacesController.cs:12, :44-80)"
  - "NEU IdealAkeWms/Views/Creditors/Index.cshtml, Create.cshtml, Edit.cshtml"
  - "NEU IdealAkeWms/Models/ViewModels/CreditorEditViewModel.cs"
  - "IdealAkeWms/Data/ApplicationDbContext.cs (DbSet neben :54; Entity-Konfiguration, Index-Muster :210-213)"
  - "IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs:467 (GetByViewKey: viewKey Creditors registrieren)"
  - "IdealAkeWms/Views/Shared/_Layout.cshtml:282-301 (Menue Stammdaten)"
  - "IdealAkeWms/Views/Users/RoleOverview.cshtml:42-60 (masterdata/masterdata_read-Beschreibung)"
  - "NUR LESEN IdealAkeWms/Data/Repositories/IFaHierarchyOrderInfoRepository (GetAllAsync, genutzt in Services/BeschichtungsauftragService.cs:100) fuer die Zuordnungsmeldung"
  - "NEU IdealAkeWms/Migrations/<ts>_AddCreditors.cs"
  - "NEU SQL/<NN>_AddCreditors.sql (NN = naechste freie in BEIDEN Zweigen; Buendel SQL/93, main SQL/88)"
  - "SQL/00_FreshInstall.sql (Schema + MigrationId)"
  - "IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs, Views/Help/Changelog.cshtml, Views/Help/Index.cshtml"
  - "docs/TESTSZENARIEN.md (neues Kapitel)"
  - "UNVERAENDERT (Bezug) IdealAkeWms/Models/FaHierarchyOrderInfo.cs:10,27; IDEALAKEWMSService/Services/FaHierarchySyncService.cs:129-138,282; IDEALAKEWMSService/Services/FaHierarchySql.cs:135-143"
open_questions:
  - "1 Felder: Name, Kurzname, Strasse, PLZ, Ort, Land, E-Mail, Telefon, Ansprechpartner, Bemerkung, aktiv/inaktiv, Sage-Kreditorennummer als spaeterer Sync-Schluessel - so bestaetigt? Pflichtfelder? (Empfehlung Notiz: genau diese Liste; Spec-Agent: Pflicht nur Name + Kurzname, Kreditorennummer optional als Zeichenkette mit eindeutigem gefiltertem Index.)"
  - "2 Zuordnung Freitext FaHierarchyOrderInfo.Dienstleister -> Kreditor: Kurzname exakt, Zuordnungstabelle oder manuell? (Notiz: nicht zuordenbare Texte sichtbar melden, nicht still uebergehen; Spec-Agent: Kurzname exakt, getrimmt, gross/klein egal, Kurzname eindeutig; Meldekarte in der Kreditorenliste; Zuordnungstabelle erst, wenn Pruefabfrage A Schreibvarianten zeigt.)"
  - "3 Datenhoheit: bis zum Sync fuehrt das WMS, danach Sage fuehrend? Sage-Quelle (Tabelle/View) ermittelt Gerald. (Empfehlung Notiz: Muster ADR 0014 - Sage fuehrend mit Abweichungsmeldung; in dieser Spec nur das Schluesselfeld vorbereiten, kein Sync-Code.)"
  - "4 Zugriff: bestehende Rollen masterdata/masterdata_read oder neue Rolle? (Empfehlung Notiz: neue oder bestehende Stammdaten-Rolle, bei neuer Rolle drei Stellen; Spec-Agent: bestehende Rollen, keine neue Rolle.)"
  - "5 Standortbezug: Kreditoren fuer AKE und IDEAL gemeinsam oder je Standort? Menuepunkt auch bei AKE sichtbar oder eigener Schalter? (Notiz: keine Empfehlung; Spec-Agent: je Installation ein Stamm ohne Standortspalte, kein eigener Schalter - Annahme 'eine DB je Standort' bitte bestaetigen.)"
  - "6 Abgrenzung 'Dienstleister in der WMS-Oberflaeche editierbar': hier nur Kreditor-Stammsatz pflegen; Auswahl des Dienstleisters je Beschichtungsauftrag erst im Beschichtungsauftrag-Teil 4? (Spec-Agent: ja - FaHierarchyOrderInfos wird bei jedem Sync geloescht und neu befuellt, eine Aenderung dort ginge verloren.)"
  - "7 Loeschen: nur deaktivieren oder hartes Loeschen solange unreferenziert? (Spec-Agent: nur deaktivieren, kein Loesch-Button - der Beschichtungsauftrag wird den Kreditor referenzieren.)"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

> [!info] Aufgegangen in [[2026-11-06-sage-sdata-stammdaten-sync-spec]] (Entscheidung Gerald 2026-11-06, „alles auf einmal“)
> Umsetzung im Epic dort (F10, Etappe 8) zusammen mit Debitoren und dem Sage-Abgleich. Diese Spec bleibt als Detail-Anhang gültig;
> bei Widerspruch gilt die neue Spec. Nicht separat umsetzen.


> [!info] Umsetzungsort (Entscheidung Gerald 2026-10-09)
> Umsetzung direkt im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` auf Zweig `feature/2026-08-07-ideal-teile-1-5` (kein eigener Worktree, ein Lauf zur Zeit). Die Code-Grundlage
> dieser Spec ist der Buendel-Stand `.claude/worktrees/2026-08-07-ideal-teile-1-5` (v1.47.1); alle
> Datei:Zeile-Angaben beziehen sich darauf. **Version**, **SQL-Nummer** und **TS-Kapitelnummer** sind
> jeweils die **naechste freie zum Umsetzungszeitpunkt** (heute: Buendel v1.47.1, SQL/93, TS bis 80;
> `main` SQL/88). Die SQL-Nummer muss in **beiden** Zweigen frei sein.

## Ziel / Nutzen (das Warum)

Das WMS kennt heute **keinen** Geschaeftspartner-Stamm. Der Beschichter eines Auftrags existiert nur
als Text, und genau dieser Text soll kuenftig auf einem eigenen Beschichtungsauftrag stehen und
gedruckt werden ([[2026-10-06-beschichtungsauftrag-ausbau]], Baustein 4). Ein Auftrag braucht dafuer
einen Empfaenger mit Adresse, Ansprechpartner und Mail — also einen gepflegten Stammsatz statt eines
Freitexts. Diese Spec legt den Stamm an und bereitet den spaeteren Sage-Sync ueber einen Schluessel
vor, ohne ihn zu bauen.

**Ausgangslage, am Code belegt:**

- **Kein Kreditor-/Lieferanten-Modell.** Suche nach `Kreditor|Supplier|Lieferant|Vendor` im Buendel
  (ohne `bin/obj/Migrations`) trifft nur Kommentare/Doku, z. B. `Models/WarehouseRequisitionItem.cs:33`
  (Notiz an den Einkauf) — keine Entitaet, keine Tabelle. Ebenso liest kein Service eine
  Sage-Kontokorrent-/Adresstabelle: die `KHK*`-Zugriffe im Service sind Artikel, Lager, Lagerplaetze,
  Buchungen und `KHKPpsArbeitsplaetze` (`SageImportService.cs:392-410`, `SageBestandReader.cs:21-32`,
  `SageArbeitsplatzReader.cs:26`, `SageLagerplatzReader.cs:21-26`, `SageBuchungLookupReader.cs:12-15`).
- **`FaHierarchyOrderInfo.Dienstleister` ist ein durchgereichter Text** (`Models/FaHierarchyOrderInfo.cs:27`,
  `nvarchar(200)` laut `ApplicationDbContext.cs:1044`, `SQL/89_AddFaHierarchy.sql:67`,
  `SQL/00_FreshInstall.sql:1812`).
  - **Einziger Schreiber:** `FaHierarchySyncService.ReadOrderInfosAsync` (`IDEALAKEWMSService/Services/FaHierarchySyncService.cs:265, 282`)
    aus `SELECT i.[Dienstleister] FROM <FAInfos-View>` (`FaHierarchySql.cs:135-143`). Repo-weite Suche
    nach `Dienstleister` findet sonst nur Lesestellen, Tests und Migrations-Designer — **keine**
    Oberflaeche, kein Repository schreibt das Feld.
  - **Lesestellen:** `Views/FaHierarchyBeschichtung/Index.cshtml:87` (Kopfzeile, `"— Dienstleister?"` bei leer),
    `Views/FaHierarchyBeschichtung/Print.cshtml:84, 97` (Kopftabelle), Warntext
    `Services/BeschichtungsauftragService.cs:123`.
  - **Herkunft:** Die View `vw_IDEAL-AKE_Kommissionierung_FAInfos` liest laut Doku-Kopf aus
    `[PPSImport].[dbo].[vw_PPSData]` — der **PPS-Anwendung der IDEAL**, nicht aus einer Sage-Kerntabelle
    (`SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAInfos.sql:9`). Die Spaltenbeschreibung „Wer
    beschichtet" (`:32`) ist nur Doku-Text; der CREATE-VIEW-Text liegt nicht im Repo (`:50`). Ob neben dem
    Text eine Kreditorennummer existiert, ist **unverifiziert** (Pruefabfrage C).
  - **Lebensdauer:** `FaHierarchyOrderInfos` wird bei **jedem** Lauf vollstaendig geloescht und neu
    befuellt (`FaHierarchySyncService.cs:129-138`: `DELETE FROM dbo.FaHierarchyOrderInfos` + `AddRangeAsync`
    in einer Transaktion). Die Klasse ist bewusst **keine** `AuditableEntity`, „reine Cache-Tabelle"
    (`FaHierarchyOrderInfo.cs:10`). **Folge:** Jede WMS-Aenderung an dieser Tabelle — Text oder
    Fremdschluessel — waere nach dem naechsten Lauf weg (vgl. [[0009-app-status-in-satelliten-tabellen-neben-sage-master]]).
  - **Standort:** Der Lauf ist nur bei `Sync:HierarchicalFaEnabled = true` aktiv (`SyncWorker.cs:337`),
    also IDEAL. Bei AKE ist die Tabelle leer.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Entitaet `Creditor` (Kreditor) mit Stammdaten laut Rueckfrage 1, Migration dreifach.
- Stammdaten-Oberflaeche `/Creditors`: Liste (ADR 0005), Anlegen, Bearbeiten, Deaktivieren/Reaktivieren.
- Zugriff ueber Read/Edit-Split (Rueckfrage 4).
- **Zuordnungsmeldung:** Die Kreditorenliste zeigt, welche `Dienstleister`-Texte aus
  `FaHierarchyOrderInfos` keinem Kreditor zugeordnet werden koennen (Rueckfrage 2). Nur lesend.
- Vorbereitung des Syncs: Feld `SageCreditorNumber` mit eindeutigem gefiltertem Index — sonst nichts.

**Out-of-Scope**
- **Sync aus Sage** (laut Notiz „in Zukunft"): kein Service, kein Reader, kein `ServiceSettings`-Key,
  kein `SyncLogServices`-Eintrag. Eigene Spec, sobald die Sage-Quelle feststeht (Rueckfrage 3).
- **Auswahl des Dienstleisters am Beschichtungsauftrag** und dessen Druck mit Kreditor-Adresse —
  gehoert zu [[2026-10-06-beschichtungsauftrag-ausbau]] Baustein 4 (Rueckfrage 6).
- Jede Aenderung an `FaHierarchyOrderInfo`, `FaHierarchySyncService`, der FAInfos-View und den
  Beschichtungs-Views.
- Debitoren/Kunden, Lieferanten fuer den Einkauf, Bestellwesen-Anbindung (`OrderRecipients`).

## Fachliche Anforderungen

1. **Stammsatz** (vorbehaltlich Rueckfrage 1): Name, Kurzname, Strasse, PLZ, Ort, Land, E-Mail, Telefon,
   Ansprechpartner, Bemerkung, Aktiv-Kennzeichen, Sage-Kreditorennummer.
2. **Validierung:** Name und Kurzname Pflicht; E-Mail, falls gefuellt, syntaktisch gueltig
   (`[EmailAddress]` wie `Models/OrderRecipient.cs:15`); Sage-Kreditorennummer, falls gefuellt, getrimmt und
   eindeutig; Kurzname eindeutig, sofern Rueckfrage 2 den Kurzname-Abgleich bestaetigt. Duplikate
   werden als `ModelState`-Fehler am Feld gemeldet — App-Layer-Guard, weil InMemory UNIQUE-Indexe nicht
   erzwingt ([[fallstricke]] „InMemory-DB erzwingt keine UNIQUE-Indexe"); der DB-Index bleibt die
   zweite Linie.
3. **Kreditorennummer als Zeichenkette**, nie numerisch geparst — Praezedenzfall
   `ProductionWorkplace.SageArbeitsplatznummer` (`Models/ProductionWorkplace.cs:30-36`: „immer als
   Zeichenkette, getrimmt (nie int.Parse — `0000` ≠ `0`)").
4. **Deaktivieren statt Loeschen** (vorbehaltlich Rueckfrage 7). Inaktive Kreditoren bleiben in der
   Liste ueber den Status-Filter sichtbar und sind reaktivierbar.
5. **Zuordnungsmeldung** (vorbehaltlich Rueckfrage 2): Die Liste zeigt oberhalb der Tabelle eine
   Hinweiskarte „Dienstleister-Texte ohne Kreditor" mit Anzahl und den Texten selbst (je Text die Zahl
   der Auftragszeilen). Grundlage: alle nicht-leeren, getrimmten `FaHierarchyOrderInfo.Dienstleister`,
   verglichen mit den Kurznamen **aktiver** Kreditoren (getrimmt, `OrdinalIgnoreCase`). Ein Text, der
   nur auf einen **inaktiven** Kreditor passt, wird als eigener Fall gemeldet („zugeordnet, aber
   inaktiv"). Sind alle zugeordnet oder ist die Tabelle leer (AKE), erscheint die Karte nicht. Fuer
   Bearbeiter gibt es je Text einen Link „Als Kreditor anlegen" mit vorbelegtem Kurznamen.
6. **Nur-Lesen-Rolle** sieht Liste und Details ohne Anlegen-/Bearbeiten-/Deaktivieren-Knoepfe, mit dem
   Hinweis wie in `Views/ProductionWorkplaces/Index.cshtml:16-19`.

## Technischer Loesungsentwurf

**Muster:** Repository ([[0001-repository-pattern-mit-decorator-fuer-caching]]) ohne Caching-Decorator
(kleine Tabelle, selten gelesen — Decorator erst bei Bedarf), MVC-Controller nach dem Read/Edit-Split
der Stammdaten ([[0006-rollenkonzept-statische-keys-mit-admin-wildcard]]), Listen-View
([[0005-listen-view-pattern-mit-server-side-spaltenfilter]]), Audit ([[0003-auditableentity-als-entity-basis]]).

**Modell** `Models/Creditor.cs : AuditableEntity` (Namen vorbehaltlich Rueckfrage 1):

> [!note] Namensabgleich (2026-10-06)
> Die Code-Namen dieser Spec sind **vorlaeufig**: Entitaet `Creditor`, Tabelle/`DbSet` `Creditors`,
> Sage-Schluessel `SageCreditorNumber`, Felder `ShortName`, `IsActive`, `Name`, `Street`, `PostalCode`,
> `City`, `Country`, Repository `ICreditorRepository`/`CreditorRepository`, Indexe
> `IX_Creditors_SageCreditorNumber`/`IX_Creditors_ShortName`. [[2026-10-06-beschichtungsauftrag-ausbau-teil-4-spec]]
> setzt darauf auf (FK `Beschichtungsauftrag.CreditorId`, Navigation `Creditor`, Auswahl ueber `IsActive`,
> Empfaengeradresse aus `Name`/`Street`/`PostalCode`/`City`/`Country`). Die Namen werden **zusammen** bei
> Schranke 1 beider Specs bestaetigt; aendert Rueckfrage 1 einen Namen, zieht Teil 4 im selben Zug mit.

| Property | Typ / Laenge | Anmerkung |
|---|---|---|
| `Name` | `string`, 200, Pflicht | Firmenname |
| `ShortName` | `string`, 50, Pflicht | Kurzname; Abgleichsfeld fuer Rueckfrage 2 |
| `Street`, `PostalCode`, `City`, `Country` | `string?`, 200/20/100/100 | Adresse |
| `Email` | `string?`, 200, `[EmailAddress]` | |
| `Phone` | `string?`, 50 | |
| `ContactPerson` | `string?`, 200 | |
| `Remark` | `string?`, 1000 | |
| `IsActive` | `bool`, Default `true` | |
| `SageCreditorNumber` | `string?`, Laenge **offen** | Laenge erst nach Pruefabfrage B festlegen; bis dahin 31 wie `SageArbeitsplatznummer` (`ProductionWorkplace.cs:34`) |

Indexe in `ApplicationDbContext` (Muster `:210-213`): `IX_Creditors_SageCreditorNumber` UNIQUE mit
Filter `[SageCreditorNumber] IS NOT NULL`; `IX_Creditors_ShortName` UNIQUE (nur bei Kurzname-Abgleich,
Rueckfrage 2). Indexnamen explizit per `HasDatabaseName`, damit EF-Migration und SQL-Skript denselben
Namen tragen ([[fallstricke]] Abschnitt SqlError 3701).

**Repository** `ICreditorRepository`/`CreditorRepository`, registriert in `Program.cs` neben `:66`:
`GetAllAsync()`, `GetByIdAsync(int)`, `AddAsync`, `UpdateAsync`, `ExistsShortNameAsync(string, int? exceptId)`,
`ExistsSageCreditorNumberAsync(string, int? exceptId)`. Kein `DeleteAsync` (Rueckfrage 7).

**Zuordnungsmeldung:** Keine neue Abfrage — `IFaHierarchyOrderInfoRepository.GetAllAsync()` existiert
(genutzt in `BeschichtungsauftragService.cs:100`); Gruppierung und Abgleich in einer kleinen statischen,
direkt testbaren Methode (z. B. `CreditorAssignment.FindUnassigned(infos, creditors)`). Die Meldung ist eine
Web-Lesefunktion — kein SyncLog (ADR 0010 gilt nur fuer Hintergrund-Services, vgl. `BeschichtungsauftragService.cs:33-34`).

**Controller** `CreditorsController`: Class-Level `[RequireMasterDataReadAccess]`, Edit-Actions
`[RequireMasterDataAccess]` (Muster `ProductionWorkplacesController.cs:12, 82`). Actions: `Index`,
`Create` (GET/POST, optional `?shortName=` zum Vorbelegen), `Edit` (GET/POST), `SetActive` (POST,
`[ValidateAntiForgeryToken]`). Rueckmeldung ueber `TempData["SuccessMessage"]`/`["WarningMessage"]`, kein
`ErrorMessage` (CLAUDE.md). Audit-Felder aus `ICurrentUserService` wie `OrderRecipientsController.cs:74-81, 146-148`.

**Liste** `Views/Creditors/Index.cshtml` (Pflichten ADR 0005, vier Bestandteile):
- Pagination: `PageSize.Resolve`/`ResolveRaw` + `PaginationState` + `_Pagination`-Partial
  (`ProductionWorkplacesController.cs:59-80`).
- Filterkarte `card filter-card mb-3` mit Status-Filter Aktiv (Default) / Inaktiv / Alle; Referenz
  `ProductionOrdersController.Index` + `Views/ProductionOrders/Index.cshtml` (die Werkbank-Liste hat
  **keine** Filterkarte und ist dafuer keine Vorlage).
- Server-Spaltenfilter `data-server-column-filter="true"`, jedes `<th>` mit `data-col-key`, `ColumnMap`
  liefert den gerenderten Zelltext (Status-Badge „Aktiv"/„Inaktiv"), Filter vor der Pagination,
  `TotalCount` aus der gefilterten Menge.
- Spaltenpraeferenzen: `column-preferences.js` vor `table-filter.js`, `#view-config`/`#column-config`,
  viewKey `Creditors` in `ColumnDefinitions.GetByViewKey` (`ColumnDefinitions.cs:467`) registrieren —
  sonst speichert die Liste still nichts (ADR 0005, Nachtrag 2026-08-12).
- Spalten: Kurzname, Name, Ort, Land, Ansprechpartner, Telefon, E-Mail, Sage-Nr., Status.

**Menue:** Eintrag „Kreditoren" im Dropdown „Stammdaten" (`_Layout.cshtml:288-301`), sichtbar fuer
`hasMasterDataRead` (`:282`); kein eigener Schalter (vorbehaltlich Rueckfrage 5).

**Freitext-Sicherheit:** Kreditor-Felder werden nur ueber Razor-Encoding ausgegeben; wo ein Wert in ein
JS-Literal muss, gilt das `JsonSerializer.Serialize`-Muster ([[fallstricke]] „Freitext nie per
`'@Html.Raw(...)'` in ein JS-Literal").

**Frontend:** Views-Arbeit → Skill `frontend-design` explizit aufrufen; Bootstrap 5 und vorhandene Partials
vor eigenem Stil, WCAG AA (CLAUDE.md).

**Pruefschritte fuer den Menschen (externe Fakten, nicht im Code belegbar):**

- **A — Schreibvarianten im Ist-Bestand** (WMS-DB IDEAL, entscheidet Rueckfrage 2):
  ```sql
  SELECT LTRIM(RTRIM([Dienstleister])) AS DienstleisterText, COUNT(*) AS Zeilen
  FROM dbo.FaHierarchyOrderInfos
  GROUP BY LTRIM(RTRIM([Dienstleister]))
  ORDER BY DienstleisterText;
  ```
- **B — Sage-Quelle fuer Kreditoren** (Sage-DB IDEAL **und** AKE, entscheidet Rueckfrage 1/3; die Spec
  nimmt keine Tabelle an):
  ```sql
  SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE
  FROM INFORMATION_SCHEMA.TABLES
  WHERE TABLE_NAME LIKE '%Kontokorrent%' OR TABLE_NAME LIKE '%Kredit%'
     OR TABLE_NAME LIKE '%Lieferant%'    OR TABLE_NAME LIKE '%Adress%'
  ORDER BY TABLE_NAME;
  -- danach je Kandidat:
  SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
  FROM INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_NAME = '<Kandidat>'
  ORDER BY ORDINAL_POSITION;
  ```
  Festzuhalten: Schluesselspalte (Name, Typ, Laenge), Kennzeichen Kreditor vs. Debitor, wo Adresse,
  Mail, Telefon, Ansprechpartner liegen.
- **C — Fuehrt das PPS neben dem Text eine Kreditorennummer?** (Sage-Server IDEAL; waere der robustere
  Zuordnungsschluessel als ein Text):
  ```sql
  SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH
  FROM [PPSImport].INFORMATION_SCHEMA.COLUMNS
  WHERE TABLE_NAME = 'vw_PPSData'
  ORDER BY ORDINAL_POSITION;
  USE [PPSImport]; EXEC sp_helptext 'dbo.vw_PPSData';
  ```

Bis A–C vorliegen, gelten alle Aussagen zu Sage-Feldern, -Laengen und PPS-Inhalt als **unverifiziert**.

## Migrations-/SQL-Auswirkungen

Neue Tabelle — Reihenfolge nach [[0004-migrations-und-sql-disziplin]]:

1. Model + `DbSet<Creditor> Creditors` + Konfiguration in `ApplicationDbContext`.
2. `dotnet ef migrations add AddCreditors --project IdealAkeWms`.
3. `SQL/<NN>_AddCreditors.sql`, NN = naechste freie Nummer in **beiden** Zweigen zum Umsetzungszeitpunkt:
   - `IF OBJECT_ID(N'dbo.Creditors', N'U') IS NULL CREATE TABLE ...` in eigenem Batch (`GO`), inkl.
     Audit-Spalten (`CreatedAt`, `CreatedBy`, `CreatedByWindows` NOT NULL; `Modified*` NULL) und
     `IsActive BIT NOT NULL DEFAULT 1`;
   - je Index ein eigener Batch mit `IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = ...)`;
     gefilterter UNIQUE-Index exakt mit dem EF-Namen und -Filter (Muster `SQL/93_AddProductionWorkplaceSageFields.sql:27-33`);
   - `__EFMigrationsHistory`-Insert mit Guard in separatem Batch.
4. `SQL/00_FreshInstall.sql` an **zwei** Stellen: Tabelle + Indexe **und** `MigrationId`.

- **Nicht daten-destruktiv** (nur neue Tabelle) — kein Backup-Hinweis noetig ueber das Uebliche hinaus.
- **`SQL/AgentJobs/`:** nicht betroffen — kein Job und kein MERGE liest oder schreibt Kreditoren (es gibt
  heute keine Kreditor-Tabelle, siehe Ausgangslage).
- Index-Anlage und Filter sind **Manual-UAT** (InMemory fuehrt keine DDL aus, [[fallstricke]] Abschnitt SqlError 3701).

## Audit-Feld-Auswirkungen

- `Creditor` erbt `AuditableEntity` (`Models/AuditableEntity.cs:3-11`).
- **Anlegen:** `CreatedAt`, `CreatedBy` (`GetDisplayName()`), `CreatedByWindows` (`GetWindowsUserName()`).
- **Jedes Update** — Bearbeiten **und** Aktiv-Schalter: `ModifiedAt`, `ModifiedBy`, `ModifiedByWindows`.
- Zeitbasis wie im Referenz-Controller (`OrderRecipientsController.cs:78, 146`: `DateTime.UtcNow`).
- `FaHierarchyOrderInfo` bleibt unveraendert ohne Audit (Cache-Tabelle, `FaHierarchyOrderInfo.cs:10`).
- Kuenftiger Sync schreibt seinen Service-Namen in `Modified*` — Sache der Sync-Spec.

## Akzeptanzkriterien

1. Unter Stammdaten → „Kreditoren" oeffnet `/Creditors` eine Liste mit Pagination (25/50/100/Alle,
   Benutzer-Default greift), Filterkarte (Status Aktiv/Inaktiv/Alle, Default Aktiv) und Server-Spaltenfilter
   auf **jeder** Spalte; ein Spaltenfilter wirkt ueber alle Seiten, die Seitenzahl stimmt danach.
2. Ausgeblendete Spalten bleiben nach Reload erhalten (viewKey `Creditors` registriert; kein 400 von
   `UserViewPreferencesApi`).
3. `masterdata` und `admin` koennen anlegen, bearbeiten, deaktivieren und reaktivieren.
4. `masterdata_read` sieht Liste und Details, aber keine Bearbeitungs-Knoepfe und den Nur-Lesen-Hinweis;
   ein direkter Aufruf von `Create`/`Edit`/`SetActive` fuehrt auf AccessDenied.
5. Ohne Stammdaten-Rolle ist der Menuepunkt unsichtbar und `/Creditors` fuehrt auf AccessDenied.
6. Anlegen ohne Name oder Kurzname wird mit Feldfehler abgewiesen; ungueltige E-Mail ebenso.
7. Eine bereits vergebene Sage-Kreditorennummer (nach Trim) wird mit Feldfehler abgewiesen, ohne
   Datenbank-Exception; leere Nummern duerfen mehrfach vorkommen.
8. Bei bestaetigtem Kurzname-Abgleich (Rueckfrage 2) gilt Kriterium 7 sinngemaess fuer den Kurznamen
   (gross/klein egal).
9. Nach Anlegen sind `CreatedAt/By/ByWindows` gesetzt; nach Bearbeiten **und** nach Deaktivieren sind
   `ModifiedAt/By/ByWindows` gesetzt (DB-Pruefung).
10. Deaktivierte Kreditoren verschwinden aus dem Default-Filter, erscheinen unter „Inaktiv"/„Alle" und
    lassen sich reaktivieren; es gibt keinen Loesch-Knopf (vorbehaltlich Rueckfrage 7).
11. Am IDEAL-System listet die Hinweiskarte jeden nicht-leeren Dienstleister-Text aus
    `FaHierarchyOrderInfos` ohne passenden aktiven Kreditor mit Zeilenzahl; nach Anlegen eines Kreditors
    mit passendem Kurznamen (andere Gross-/Kleinschreibung, Leerzeichen am Rand) verschwindet der Text.
12. Ein Text, der nur auf einen inaktiven Kreditor passt, wird als „zugeordnet, aber inaktiv" gemeldet.
13. Ist `FaHierarchyOrderInfos` leer (AKE) oder alles zugeordnet, erscheint keine Hinweiskarte und kein Fehler.
14. Kreditor-Werte mit Sonderzeichen (`'`, `"`, `<`, `&`) werden in Liste, Formular und Hinweiskarte
    korrekt angezeigt und brechen kein Skript.
15. Beschichtungsauftrag-Ansicht und -Druck sind unveraendert (`Dienstleister` weiterhin als Text).
16. Migration laeuft auf einer Bestands-DB und einer FreshInstall-DB; das SQL-Skript ist zweimal
    hintereinander fehlerfrei ausfuehrbar.
17. Unit-Tests: Abgleichslogik (Trim, Gross/Klein, leer, inaktiv, mehrere Zeilen je Text) und
    Duplikat-Guards (App-Layer) sind abgedeckt; Build + alle Tests gruen.

## Test-Szenarien

Neues Kapitel **TS-<naechste freie>** (Buendel endet bei TS-80) in `docs/TESTSZENARIEN.md`, danach
[[testszenarien-index]] nachziehen. Skizze:

- **TS-x.1 Liste und Filter** — Vorbedingung: ≥ 30 Kreditoren, davon einige inaktiv. Pagination,
  Status-Filter, Spaltenfilter (inkl. `,`-ODER und `!`-NICHT), Spaltenwahl nach Reload. Negativ:
  Filter ohne Treffer zeigt leere Tabelle mit korrekter Gesamtzahl 0.
- **TS-x.2 Anlegen / Validierung** — Pflichtfelder, E-Mail-Format, doppelte Kreditorennummer (mit
  Leerzeichen am Rand), doppelter Kurzname (andere Schreibweise). Erwartet: Feldfehler, keine Exception.
- **TS-x.3 Bearbeiten / Audit** — Feld aendern, in der DB `Modified*` pruefen; Deaktivieren, `Modified*`
  erneut gesetzt; Reaktivieren.
- **TS-x.4 Rollen** — Benutzer mit `masterdata_read`: keine Knoepfe, Direkt-URL `Edit` → AccessDenied;
  Benutzer ohne Stammdaten-Rolle: kein Menuepunkt, `/Creditors` → AccessDenied; `admin`: alles.
- **TS-x.5 Zuordnungsmeldung (IDEAL)** — Vorbedingung: FA-Hierarchie-Sync gelaufen, Pruefabfrage A
  zeigt Texte. Erwartet: Karte listet dieselben Texte mit denselben Zeilenzahlen; „Als Kreditor anlegen"
  belegt den Kurznamen vor; nach Speichern ist der Text weg; Kreditor deaktivieren → Text als „inaktiv"
  gemeldet.
- **TS-x.6 AKE** — keine Karte, keine Fehlermeldung, Liste voll nutzbar.
- **TS-x.7 Sonderzeichen** — Name `Müller & Söhne 'Pulver' <GmbH>`: Anzeige korrekt, Spaltenfilter
  findet ihn, keine Konsolenfehler.
- **TS-x.8 Regression Beschichtungsauftrag** — Liste und Druck zeigen den Dienstleister-Text wie vorher.

## Etappen (nur bei epic: true)

Entfaellt — `epic: false`, ein Arbeitsgang mit einem Merge.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfaellt | – | – |

## Deploy

> **Vorlaeufig** — der QA-Lauf bestaetigt gegen den echten Diff.

- **Web-App:** ja (neuer Controller, Views, Menue, Modell).
- **Service:** nein (kein Sync in dieser Spec; nur Versions-Bump in `IDEALAKEWMSService/AppVersion.cs`,
  daher kein Service-Deploy noetig, sofern die Hausregel das nicht verlangt).
- **Migration:** ja — `SQL/<NN>_AddCreditors.sql` **vor** dem Web-Deploy ausfuehren (rein additiv, kein
  Service-Stopp noetig).
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
- Nach dem Deploy: Rollen pruefen (wer `masterdata` hat, pflegt Kreditoren); bei IDEAL die Hinweiskarte
  als Startliste fuer die Erstanlage nutzen.

Weitere Pflichten des Dev-Laufs: Version in **beiden** `AppVersion.cs` (naechste freie), Anwender-Changelog
`Views/Help/Changelog.cshtml`, **Hilfeseite mit konkreten Details** (Felder, Zuordnungsmeldung, „deaktivieren
statt loeschen"), `README.md` nur falls ein AppSetting entsteht (Rueckfrage 5), Brain: Changelog-Eintrag,
[[feature-map]] (Abschnitt „Stammdaten, Berechtigungen, Listen"), `codebase/controller.md` (Zeilen 39, 51-52:
Controller-Liste und Zaehlung der Stammdaten-Controller), `codebase/datenmodell.md`, Glossarbegriff
„Kreditor".

> [!note] Befund am Rande
> `RoleOverview.cshtml:44, 57` und `codebase/controller.md:51-52` sprechen von **6** Stammdaten-Sichten,
> der Buendel-Stand schuetzt aber schon 8 Controller mit `[RequireMasterDataReadAccess]` (zusaetzlich
> `WorkStepsController.cs:9`, `FaAttributesController.cs:16`). Beim Ergaenzen von „Kreditoren" die Aufzaehlung
> ohne feste Zahl formulieren.

## Offene Rueckfragen

1. **Felder und Pflichtfelder.** Liste: Name, Kurzname, Strasse, PLZ, Ort, Land, E-Mail, Telefon,
   Ansprechpartner, Bemerkung, aktiv/inaktiv, Sage-Kreditorennummer als Schluessel fuer den spaeteren Sync.
   Vollstaendig so? Welche sind Pflicht? Laenge/Typ der Kreditorennummer haengen an der Sage-Quelle
   (Pruefabfrage B). *Empfehlung Notiz:* genau diese Liste. *Spec-Agent:* Pflicht nur Name + Kurzname;
   Kreditorennummer optional, Zeichenkette, eindeutiger gefilterter Index.
2. **Zuordnung Freitext → Kreditor.** `FaHierarchyOrderInfo.Dienstleister` ist ein Text aus der PPS-View
   (`FAInfos.sql:9`), der bei jedem Sync neu kommt. Abgleich per Kurzname exakt, per Zuordnungstabelle
   (mehrere Texte → ein Kreditor) oder manuell? *Empfehlung Notiz:* nicht zuordenbare Texte sichtbar melden,
   nicht still uebergehen (eine der drei Varianten nennt die Notiz nicht als bevorzugt). *Spec-Agent:*
   Kurzname exakt (getrimmt, gross/klein egal, Kurzname eindeutig) plus Hinweiskarte in der Kreditorenliste;
   Zuordnungstabelle erst, wenn Pruefabfrage A Schreibvarianten desselben Beschichters zeigt; liefert
   Pruefabfrage C eine Kreditorennummer im PPS, waere diese der bessere Schluessel.
3. **Datenhoheit und Sage-Quelle.** Bis zum Sync fuehrt das WMS; danach Sage fuehrend? Welche Sage-Tabelle/-View
   ist die Quelle (Pruefabfrage B, ermittelt Gerald)? *Empfehlung Notiz:* Muster [[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]]
   — Sage fuehrend mit unscharfer Abweichungsmeldung. Diese Spec baut nur das Schluesselfeld, keinen Sync;
   bis dahin sind alle Felder im WMS editierbar. Offen: Sollen nach dem Sync die Sage-gefuehrten Felder im
   WMS schreibgeschuetzt werden (wie `SageArbeitsplatznummer`, `Views/ProductionWorkplaces/Edit.cshtml:36`)?
4. **Zugriff.** Bestehende Rollen `masterdata`/`masterdata_read` (`RoleKeys.cs:6-7`, `CurrentUserService.cs:85-89`)
   oder eine neue Rolle (z. B. fuer den Einkauf)? *Empfehlung Notiz:* neue oder bestehende Stammdaten-Rolle;
   bei neuer Rolle drei Stellen (Attribut, `codebase/controller.md`, `Views/Users/RoleOverview.cshtml`).
   *Spec-Agent:* bestehende Rollen — kein neuer Filter, nur Beschreibungstexte nachziehen.
5. **Standortbezug.** Kreditoren fuer AKE und IDEAL gemeinsam oder je Standort? Am Code: Standortdaten wie
   `Firmenname` sind **ein** AppSettings-Wert je Datenbank (`StandortSettingsCatalog.cs:37`), der Master
   `ProduktionsauftragHierarchisch` ebenso (`:61`) — das spricht fuer eine Datenbank je Standort; belegt ist es
   im Brain nicht. Dazu: Menuepunkt auch bei AKE sichtbar, oder hinter einen Schalter (ADR 0011)? *Empfehlung
   Notiz:* keine. *Spec-Agent:* je Installation ein Stamm ohne Standortspalte, kein eigener Schalter — bitte
   „eine DB je Standort" bestaetigen; teilen sich beide Standorte eine DB, braucht der Stamm eine Standortspalte.
6. **Abgrenzung „Dienstleister in der WMS-Oberflaeche editierbar".** Ein Bearbeiten des Texts an
   `FaHierarchyOrderInfos` ist nicht moeglich, ohne dass er beim naechsten Sync verloren geht
   (`FaHierarchySyncService.cs:131-135`). Gilt: in dieser Spec nur der Kreditor-Stammsatz editierbar; die Wahl des
   Dienstleisters je Auftrag kommt mit dem gespeicherten Beschichtungsauftrag ([[2026-10-06-beschichtungsauftrag-ausbau]]
   Baustein 4)? *Empfehlung Notiz:* keine eigene (die Ausbau-Notiz ordnet „aus dem Kreditoren-Stamm waehlbar" Baustein 4 zu).
   *Spec-Agent:* ja, so abgrenzen.
7. **Loeschen.** Nur deaktivieren oder hartes Loeschen, solange kein Beschichtungsauftrag den Kreditor
   referenziert? *Empfehlung Notiz:* keine (nur „aktiv/inaktiv" als Feld). *Spec-Agent:* nur deaktivieren,
   kein Loesch-Knopf — spart die Referenzpruefung und verhindert verwaiste Auftraege.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Felder und Pflichtfelder → **Felder wie empfohlen, zusätzlich Matchcode und eine Ausprägung „Beschichter (ja/nein)“, damit der Stamm für weitere Abläufe filterbar ist (Gerald 2026-10-06). Pflichtfelder noch offen. Umsetzung zurückgestellt – Spec bleibt Entwurf.**
2. Zuordnung Freitext → Kreditor →
3. Datenhoheit und Sage-Quelle →
4. Zugriff (bestehende oder neue Rolle) →
5. Standortbezug (gemeinsam/je Standort, Menue bei AKE) →
6. Abgrenzung „Dienstleister editierbar" →
7. Loeschen oder nur deaktivieren →
