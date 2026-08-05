---
type: codebase-karte
updated: 2026-07-27
---
# Integrationen

Vier Fremdsysteme, vier Connection Strings, ein Grundsatz: das WMS **liest** aus den
Fremdsystemen und schreibt nur in klar benannten Ausnahmen zurueck.
Geschwister: [[module]] · [[controller]] · [[services]] · [[datenmodell]]

## Connection Strings

| Name | System | Anmerkung |
|---|---|---|
| `DefaultConnection` | WMS (`IDEAL_AKE_WMS` auf `AKESQL20.ake.at`) | eigene DB |
| `SageConnection` | Sage ERP | Views + Tabellen, read-only |
| `OseonConnection` | OSEON / TRUMPF | Stored Procedures + Tabellen, read-only (Rueckmeldung optional) |
| `EnaioDmsConnection` | enaio DMS | View, read-only |

Connection Strings bleiben **appsettings-only** und wandern nie in die per UI editierbare
`ServiceSettings`-Tabelle — siehe [[0008-servicesettings-db-first-mit-typisiertem-katalog]].

---

## Sage (ERP) — fuehrendes System fuer Auftraege, Artikel, Bestand

**Was gelesen wird**

| Quelle | Ziel | Weg |
|---|---|---|
| `vw_AKE_Kommissionierung_WAListe` | `ProductionOrders` | SQL-Agent-Job `../../SQL/AgentJobs/01_Import_Produktionsauftraege.sql` (MERGE + Folge-MERGEs fuer PickingStatus/BdeStatus) |
| `KHKPpsRessourcenPositionen` + `KHKArtikel` | `Articles` | SQL-Agent-Job `../../SQL/AgentJobs/02_Import_Artikel.sql` |
| `vw_AKE_Kommissionierung_StuecklistenDB` | Stueckliste (BOM) | `BomRepository` zur Laufzeit → [[0007-bom-quelle-sage-view-mit-oseon-fallback]] |
| `vw_IDEAL_AKE_WMS_FAZusatzinformationen` | `ProductionOrderExtraInfo` | `FaZusatzinfoSyncService` (`Sync:FaZusatzinfoEnabled`) |
| Lagerplatz-Stammdaten | `StorageLocations` | `LagerplatzSyncService` (`Sync:LagerplaetzeEnabled`) |
| Lagerbestand | Korrektur-Buchungen | `LagerbestandSyncService` (`Sync:LagerbestandEnabled`) |
| Artikel inkl. Hauptlagerplatz + Meldebestand | `Articles` | `SageImportService.SyncArticlesAsync` (raw SQL) |

**Was zurueckgeschrieben wird:** nur Rueckmeldungen, und nur wenn `SageRueckmeldungAktiv` gesetzt
ist.

### Aufbau der BOM-View `vw_AKE_Kommissionierung_StuecklistenDB`

Die View liegt in der `ake`-Datenbank. Die Feldsemantik ist die Quelle mehrerer Fallstricke:

| Feld | Bedeutung |
|---|---|
| `Artikelnummer` | **Geraete**-Artikelnummer (= FA-Artikel). Redundant, nur zur Zuordnung — **nie** fuer Bauteil-Operationen. |
| `Ressourcenummer` | Die eigentliche **Bauteil**-Artikelnummer, die Kern-Info je Zeile. |
| `Position` | Hierarchische Position (`15`, `15.1`, `15.1.1`) — **definiert die Baumstruktur**. Die Ebene wird aus der Anzahl der Punkte berechnet. |
| `Baugruppe` | Artikelnummer der uebergeordneten Baugruppe. |
| `Bezeichnung1` / `Bezeichnung2` | Bezeichnung der Ressourcenummer. |
| `Menge` | Menge **in dieser Baugruppe** — **nicht** die Gesamtmenge im Geraet. |
| `Beschaffungsartikel` | Sage-Feld `IstBestellartikel`, Info fuer den Kommissionierer. |
| `Artikelgruppe` | Info fuer den Kommissionierer (Format `"940 - Kleinmaterial"`). |

Sortiert wird mit `NaturalPositionComparer` (sonst kaeme 1, 10, 11, 2).

**Fallen dieser Integration** (Details in [[fallstricke]]):
- Boolean-Spalten sind BIT mit **`-1`** fuer TRUE (VB6-Legacy) → `= -1` filtern, nie `= 1`.
- Artikelgruppen kommen als `"940 - Kleinmaterial"`, gespeichert wird `"940"`.
- `Artikelnummer` (Geraet) vs `Ressourcenummer` (Bauteil) nicht verwechseln.
- Sage liefert 0-Bestand-Zeilen **nicht** — daher das Nullsetzen verwaister Paare mit Guard und
  Cap.
- `SubOrderNumber` nur schreiben, wenn die Spalte existiert (`COL_LENGTH`-Check) — die IDEAL- und
  die AKE-Linie haben unterschiedliche Schemata.
- Verschwindet ein FA aus der View, storniert die **Reconciliation im Service** (nie im AgentJob).
- Die AgentJobs sind Teil des Deploy-Vertrags → [[0004-migrations-und-sql-disziplin]].

---

## OSEON / TRUMPF — Fertigungssteuerung, Teileverfolgung

**Was gelesen wird:** Kundenauftraege, Subauftraege, Arbeitsgaenge (`OseonSyncService`,
`Sync:OseonTrackingEnabled`), Artikelkategorien (`Sync:OseonArticleCategoryEnabled`) und die
BOM-Stored-Procedure als Fallback.

**Der BOM-Fallback konkret:** Stored Procedure `sp_AKE_Kommissionierung_OseonStuecklistenDB` auf
`aketrumpf01.ake.at\TRUMPFSQL2`, Datenbank `T1000_V01_V001` (Connection String
`OseonConnection`). Die verwendete Quelle wird im BOM-Header als Badge angezeigt: **SAGE** (grau),
**OSEON** (gelb), **Keine Daten gefunden** (rot) → [[0007-bom-quelle-sage-view-mit-oseon-fallback]].

**Delta-Sync** ueber `LastChangedInOseon` mit 5 Minuten Puffer.

**Teileverfolgung** (`TrackingController`, `Views/Tracking/`):
- 3-Ebenen-Baum: KundenAuftragsNr → Subauftraege → Arbeitsgaenge.
- Ampelsystem Rot/Gelb/Blau/Gruen/Grau aus Soll-Terminen und Status
  (`OseonTrafficLightService`, Schwellen `OseonAmpelGelbTage`, `OseonAmpelBlauTage`).
- AG-Konfiguration in `OseonOperationConfig`: Offset-Tage und OSEON-Relevanz je Arbeitsgang.
- Status-Codes: 10 = Unvollstaendig, 20 = Gueltig, 30 = Freigegeben, 60 = In Arbeit,
  70 = Gesperrt, 90 = Fertig, 95 = Storniert.
- Server-seitige Paginierung (25 Gruppen/Seite) + **Lazy-Load** der Sub-Ebenen ueber
  `/Tracking/OseonGroupDetails`.

**Reporting** (`OseonReportingController`): AG-Uebersicht mit Horizont
(`OseonReportingHorizonDays`) und Ueberfaellig-Slice (`OseonReportingOverdueLookbackDays`).

**Was zurueckgeschrieben wird:** Rueckmeldungen nur bei `OseonRueckmeldungAktiv`.

**Fallen** (Details in [[fallstricke]]):
- `pa.ID` ist **bigint** → `long`.
- `GetSubOrdersForCustomerOrderAsync` darf WorkOperations **nicht** auf `relevantOperationNames`
  filtern — der ViewModel-Builder braucht ALLE Ops, um „nur nicht-relevante Ops = Fertig" zu
  erkennen.
- Handler fuer AJAX-nachgeladene Zeilen nur per Event-Delegation binden.
- Modul-Gate ist `TeileverfolgungAktiv`.

---

## enaio (DMS) — Werkstattauftraege und Zeichnungen

**Quelle:** View `vw_IDEAL-AKE_Fertigungsauftraege`, FA-Nummer aus `WaNummerTrimmed`.
**Ziel:** `EnaioDmsDocument`. **Service:** `EnaioDmsSyncService` (`Sync:EnaioDmsEnabled`).

**Full-Sync, kein Delta** — die `angelegt`-Spalte ist statisch (Bulk-Import 2013); MERGE
verhindert Duplikate.

`DocumentType` kennt drei Werte: `Werkstattauftrag`, `Werkstattauftrag+Zeichnung`, `Zeichnung`.
Icon und Reihenfolge sind **zentral**: Partial `Views/Shared/_EnaioDmsBadges.cshtml`
(`EnaioDmsBadgesViewModel`), Sortier-Vorrang in `EnaioDmsDocumentRepository.GetByOrderNumbersAsync`.
Vier Views erben das (ProductionOrders, PickingLeitstand, FaWorklist, FaCompletion) — neue
Badge-Darstellungen **nicht** inline kopieren.

`object1.id` ist int → `Convert.ToInt64(reader.GetValue(0))`.

---

## Active Directory — nur fuer Anmeldung und Benutzeranlage

- `ActiveDirectoryService` (`IActiveDirectoryService`) liest die Mitglieder der Gruppe aus
  `WindowsAuthBerechtigungsgruppe` live per LDAP (`System.DirectoryServices.AccountManagement`,
  Windows-only). Optionaler Container ueber `Security:AdDomain`.
- Verwendet wird das **nur** beim Anlegen von AD-Benutzern (`UsersController.CreateAdUser`,
  inkl. serverseitiger E-Mail-Uebernahme aus `UserPrincipal.EmailAddress`).
- **Autorisierung laeuft nie ueber AD.** Rollen werden explizit pro Benutzer zugewiesen;
  `Role.AdGroup` wurde in v1.23.0 gedroppt →
  [[0006-rollenkonzept-statische-keys-mit-admin-wildcard]].
- Der Anmelde-Weg selbst: [[0002-dual-auth-session-login-plus-windows-sso]].
- Weder LDAP noch der Negotiate-Handshake sind InMemory-testbar → Manual-UAT
  (`../../docs/TESTSZENARIEN.md` Kap. 40).

---

## Feiertage — date.nager.at

`HolidaySyncService` / `HolidayImportService` (`Sync:FeiertagSyncEnabled`), Laendercode
`Sync:FeiertagCountryCode` (Default `AT`), optionale Region `Sync:FeiertagRegion` (z. B. `AT-3`,
`AT-6`), Vorlauf `Sync:FeiertagJahreVoraus` (Default 2). Ziel: `Holidays` — Basis fuer
`BusinessDayService` (Kommissionier-, Vorkommissionier- und Beschichtungstermine) und den
BDE-Schichtkalender.

---

## SMTP — Ausgangsmails

`MailService` (`IMailService`), Konfiguration `MailSettings:*` in appsettings (**nicht** in
ServiceSettings). Absender fuer Bedarfsmeldungen, Lager-/Glasbestellungen und Sync-Fehlermails
(`ErrorNotification:Enabled` / `Recipients`).

Mails gehen **immer** als `multipart/alternative` mit HtmlBody **und** TextBody, wobei der
Textteil die **nackte** URL enthaelt — sonst rendern Outlook/Copilot `[URL]Text` als Rohtext.
Siehe [[fallstricke]].

## Sage-Lagerbuchung (ausgehend, WMS → Sage via SData) — v1.28.0

Erste **ausgehende** Bestandsintegration (Gegenrichtung zu `LagerbestandSyncService`, das Sage→WMS
korrigiert). Manuelle Ein-/Ausbuchungen (`StockMovementsController.Inbound/Outbound/OutboundAllConfirm`)
werden asynchron ueber eine Queue an die Sage-SData-REST-API gemeldet. Details:
[[2026-07-29-sage-lagerbuchungen-spec]].

- **Enqueue:** Decorator `SageBookingEnqueueingStockMovementRepository` (Subclassing von
  `StockMovementRepository`, nur `override AddAsync`) auf `IStockMovementRepository` im Web. Gating
  kumulativ (`SageBookingEnqueueDecision`): nur `Einbuchung`/`Ausbuchung` **UND** globaler Toggle
  `SageLagerbuchungAktiv` **UND** Lagerplatz-Flag `SageBuchungErlaubt`. Enqueue-Fehler werden
  gefangen + protokolliert, **nie geworfen** (die WMS-Buchung ist bereits committed).
- **Queue:** Tabelle `SageBookingQueueItems` (Status Offen→Gesendet→Bestaetigt/Fehler),
  `ISageBookingQueueRepository`/`SageBookingQueueRepository` (geteilt Web+Service).
- **Senden:** `SageBookingWorker` (eigener BackgroundService, Kurztakt) → `ISageLagerbuchungClient`
  (typed HttpClient, Basic-Auth, `POST {SData:BaseUrl}/sdata/{SData:Application}/{SData:ServiceContract}/{SData:Dataset}/$service/LagerbuchungService`
  — reiner Builder `SageLagerbuchungClient.BuildServiceUrl`; `;` im Dataset und `$` der Resource bleiben
  **literal** (kein `Uri.EscapeDataString`, RFC-3986-sub-delims)),
  Payload aus `SageLagerbuchungPayloadBuilder` (Einbuchung→„Zugang" Ziel gesetzt, Ausbuchung→
  „Entnahme" Herkunft gesetzt; `Herkunft-/ZielLagerkennung` = `StorageLocation.Code`,
  `...LagerplatzId` = `SageLagerplatzId` = `KHKLagerplaetze.PlatzID`).
- **Idempotenz:** Korrelation `SM#<id>#` im Sage-`Memo`; vor jedem erneuten Senden Read-Lookup gegen
  Sage `KHKLagerplatzbuchungen` (`SageBuchungLookupReader`, Raw-SQL ueber `SageConnection`).
- **Credentials** appsettings-only (`SageLagerbuchung:Username/Password`, Service); alles andere
  DB-first (`/ServiceSettings` Kategorie „Sage-Lagerbuchung"). Aktivitaets-Protokoll:
  `SyncLogServices.SageLagerbuchung`.
- **Nicht in Scope (Step 1):** Umbuchung, BDE-Rueckmeldung, Serien/Chargen. Manual-UAT
  (`../../docs/TESTSZENARIEN.md` Kap. 56), da HTTP/SData nicht InMemory-testbar.
