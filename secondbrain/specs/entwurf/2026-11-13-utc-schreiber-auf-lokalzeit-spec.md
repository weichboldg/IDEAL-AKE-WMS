---
type: spec
title: "Zeitstempel-Schreiber mit UTC auf Lokalzeit umstellen (Web, Dienst, raw SQL) – mit Wächter-Test"
slug: 2026-11-13-utc-schreiber-auf-lokalzeit-spec
status: Entwurf
created: 2026-11-13
updated: 2026-11-13
source_backlog: "[[2026-11-13-utc-schreiber-auf-lokalzeit]]"
herkunft: "[[2026-11-12-befunde-ake-regression-dev-spec]] (Rückfrage 6, Freigabe-Antwort 6)"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
affected_code:
  - IdealAkeWms/Controllers/UsersController.cs
  - IdealAkeWms/Controllers/AccountController.cs
  - IdealAkeWms/Controllers/RolesController.cs
  - IdealAkeWms/Controllers/OrderRecipientsController.cs
  - IdealAkeWms/Controllers/TrackingController.cs
  - IdealAkeWms/Program.cs
  - IdealAkeWms/Data/Repositories/RoleRepository.cs
  - IdealAkeWms/Data/Repositories/OrderRecipientRepository.cs
  - IdealAkeWms/Data/Repositories/UserViewPreferenceRepository.cs
  - IdealAkeWms/Data/Repositories/ProductionOrderPickingStatusRepository.cs
  - IdealAkeWms/Views/Shared/_ArtikelSageKarten.cshtml
  - IDEALAKEWMSService/Services/SageImportService.cs
  - IDEALAKEWMSService/Services/SageProductionOrderSql.cs
  - IDEALAKEWMSService/Services/OseonSyncService.cs
  - IDEALAKEWMSService/Services/CoatingDetectionService.cs
  - IDEALAKEWMSService/Services/Sdata/ArticleSageDetailsWriter.cs
  - IDEALAKEWMSService/Services/Sdata/ArtikelBenutzerfelderWriter.cs
  - IdealAkeWms.Tests (neuer Wächter-Test Zeitstempel-Konvention, Repository-Tests)
  - IDEALAKEWMSService.Tests/Services/SageProductionOrderSqlTests.cs
  - docs/TESTSZENARIEN.md
  - "Vorgänger (nicht doppeln): [[2026-11-12-befunde-ake-regression-dev-spec]] – PartRequisitions schon in v1.99.13 umgestellt"
open_questions:
  - "Altdaten dbo.Users (CreatedAt sichtbar in /Users, ModifiedAt): umrechnen oder unverändert? Empfehlung: unverändert (Zeilen mit CreatedBy='system' sind gemischt, SQL-Seed GETDATE vs. Program.cs UtcNow – nicht trennbar). Prüfabfrage A1."
  - "Altdaten dbo.Roles + dbo.UserRoles: umrechnen oder unverändert? Empfehlung: unverändert (nirgends angezeigt; SQL-Seeds mit GETDATE gemischt). Prüfabfrage A2."
  - "Altdaten dbo.OrderRecipientGroups, dbo.OrderRecipients, dbo.ArticleGroupRecipientMappings: umrechnen oder unverändert? Empfehlung: unverändert (nirgends angezeigt). Prüfabfrage A3."
  - "Altdaten dbo.UserViewPreferences: umrechnen oder unverändert? Empfehlung: unverändert (nirgends angezeigt). Prüfabfrage A4."
  - "Altdaten dbo.StorageLocations (nur der Platz NAN, falls ihn Program.cs statt 00_FreshInstall angelegt hat): umrechnen oder unverändert? Empfehlung: unverändert (eine Zeile). Prüfabfrage A5."
  - "Altdaten dbo.ProductionOrderPickingStatus (ReleasedAt, ModifiedAt, CreatedAt) + dbo.ProductionOrderBdeStatus (CreatedAt): umrechnen oder unverändert? Empfehlung: unverändert (nirgends angezeigt, ModifiedAt je Zeile gemischt). Prüfabfrage A6."
  - "Altdaten dbo.WorkOperations (ReportedAt sichtbar in der Teileverfolgung, nur AKE; ModifiedAt): umrechnen oder unverändert? Empfehlung: unverändert, sofern A7 wenige/alte Rückmeldungen zeigt; sonst Einmal-Skript nur für ReportedAt. Prüfabfrage A7."
  - "Altdaten dbo.ProductionOrders (CreatedAt, ModifiedAt, CancelledAt der Dienst-Schreiber): umrechnen oder unverändert? Empfehlung: unverändert (nirgends angezeigt). Prüfabfrage A8."
  - "Altdaten dbo.Articles (CreatedAt der vom Dienst angelegten Artikel, sichtbar in /Articles „Erstellt am“; ModifiedAt): umrechnen oder unverändert? Empfehlung: unverändert (Anzeige nur ohne Filter, 1–2 h Versatz). Prüfabfrage A9."
  - "Altdaten OSEON-Tabellen dbo.ProductionWorkplaces (vom OSEON-Sync angelegt), dbo.OseonProductionOrders, dbo.OseonWorkOperations (nur AKE): umrechnen oder unverändert? Empfehlung: unverändert (nirgends angezeigt). Prüfabfrage A10."
  - "Altdaten der SData-Tabellen dbo.ArticleSageDetails, dbo.ArticleAttributeDefinitions/dbo.ArticleAttributeValues (Sage-Benutzerfelder): umrechnen oder unverändert? Diese Schreiber gibt es nur im Bündel (IDEAL-Prod v1.71.1, AKE-Prod main v1.30.x haben sie nicht), Altdaten also nur auf DEV/Test. Empfehlung: unverändert. Prüfabfrage A11."
  - "ArticleSageDetails: heute UTC geschrieben UND in der Karte „Sage-Zusatzfelder“ per ToLocalTime() korrekt angezeigt (kein Fehlerbild). Umstellen (Writer DateTime.Now + ToLocalTime() entfernen, wie alle anderen) oder als bewusste UTC-Ausnahme stehen lassen? Empfehlung: umstellen – noch nicht in Produktion, eine Konvention statt zwei."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

# Zeitstempel-Schreiber mit UTC auf Lokalzeit umstellen

Quelle: [[2026-11-13-utc-schreiber-auf-lokalzeit]], Herkunft Rückfrage 6 in [[2026-11-12-befunde-ake-regression-dev-spec]].
Code gelesen im Bündel-Worktree `feature/2026-08-07-ideal-teile-1-5` (Stand v1.99.15). Alle Zeilenangaben beziehen sich auf diesen
Stand, Pfade relativ zum Worktree.

## Ziel / Nutzen (das Warum)

Konvention des Projekts ist Lokalzeit: die Oberfläche zeigt Zeitstempel ohne Umrechnung (ADR
[[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]], [[fallstricke]] „`SyncLog.Timestamp` ist `DateTime.Now`, nicht `UtcNow`“ –
dort ausdrücklich: „Bei einer echten Multi-Timezone-Umstellung konsistent umstellen, nicht halb“). Die Bedarfsmeldungen sind in v1.99.13
umgestellt ([[2026-11-12-befunde-ake-regression-dev-spec]], Punkt 3) – **hier nicht doppeln**. Diese Spec erfasst **alle übrigen**
UTC-Schreiber vollständig, trennt sichtbare Fehler von unsichtbaren Konventionsbrüchen und von technischen Zeitstempeln, die bewusst
UTC bleiben, und stellt die beiden ersten Gruppen auf Lokalzeit um. Ein Wächter-Test verhindert, dass ein neuer UTC-Schreiber
unbemerkt dazukommt.

Sichtbar falsch ist heute (MEZ, im Sommer 2 h):
- `/Users` „Erstellt am“ (und dessen Spaltenfilter) – 1 h zu früh.
- `/Articles` „Erstellt am“ bei allen vom Artikel-Abgleich angelegten Artikeln – 1 h zu früh, manuell angelegte stimmen.
- Teileverfolgung (AKE) „rückgemeldet am“ – 1 h zu früh.
- `/StorageLocations` „Erstellt am“ des Platzes NAN, nur wenn ihn `Program.cs` statt `00_FreshInstall.sql` angelegt hat.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. Alle Schreiber der Tabelle „Bestand“ unten mit Maßnahme **umstellen**: `DateTime.UtcNow` → `DateTime.Now` (Web), `GETUTCDATE()` →
   Parameter `@Jetzt` = `DateTime.Now` des Dienstrechners (Dienst, raw SQL – wie `PartRequisitionEmailService` in v1.99.13).
2. `ArticleSageDetails` nach Rückfrage 12 (Empfehlung: umstellen + `ToLocalTime()` in `_ArtikelSageKarten.cshtml:18` entfernen).
3. Ein Wächter-Test (Quelltext-Scan) mit Ausnahmeliste der bewusst-UTC-Stellen.
4. Altdaten je Tabelle nach Rückfragen 1–11 (Empfehlung durchgehend: unverändert, keine Einmal-Skripte).
5. Brain: Konvention als ADR festhalten (Vorschlag ADR 0025 „Zeitstempel in Lokalzeit des schreibenden Rechners“), [[fallstricke]]-Eintrag
   um die Ausnahmeliste ergänzen.

**Out-of-Scope**
- `dbo.PartRequisitions` – erledigt in v1.99.13 (siehe oben).
- Die bewusst-UTC-Stellen (Abschnitt „Bewusst UTC“) – bleiben, werden nur in der Wächter-Ausnahmeliste und im ADR benannt.
- `SQL/35_AddEnaioDmsDocuments.sql:14` `DEFAULT GETUTCDATE()` – toter Default (siehe unten), keine Schema-Änderung.
- Umstellung `GETDATE()`-Stellen in SQL-Skripten/AgentJobs auf eine andere Uhr – sie schreiben Lokalzeit des SQL-Servers, kein UTC.
- Zeitstempel aus Fremdsystemen (`OseonWorkOperations.LastStatusReportInOseon`, `EnaioDmsDocuments.CreatedInEnaio`, SData-Termine) – die
  Uhr gehört dem Fremdsystem, nicht dem WMS.

## Fachliche Anforderungen

### Suchweg (Beleg für Vollständigkeit und für alle Negativaussagen)

Durchsucht im Worktree, alle vier Projekte (`IdealAkeWms`, `IDEALAKEWMSService`, beide Testprojekte – es gibt keine weiteren `*.csproj`),
`SQL/` (inkl. `SQL/AgentJobs/`, `SQL/AgentJobs/_archiv/`, `SQL/Einmalig/`, `00_FreshInstall.sql`), `scripts/`, EF-Migrationen
(`IdealAkeWms/Migrations/`), Views und eigenes JS (`wwwroot/js`, ohne `wwwroot/lib`):

| Suche (ripgrep, ohne Groß/Klein) | Treffer im Produktivcode |
|---|---|
| `UtcNow\|GETUTCDATE\|SYSUTCDATETIME\|SYSDATETIMEOFFSET\|ToUniversalTime` | genau die Stellen der Tabellen unten; in `scripts/`: 0; in `SQL/`: nur `35_AddEnaioDmsDocuments.sql:14`; in `Migrations/`: 0 |
| `HasDefaultValueSql\|defaultValueSql` | 0 im ganzen Worktree (kein EF-Default mit SQL-Uhr, auch in keiner Migration) |
| Model-Initialisierer `DateTime CreatedAt/Timestamp … =` | nur `Models/SyncLog.cs:10` `= DateTime.Now`; `AuditableEntity` hat keinen Default (`Models/AuditableEntity.cs:6`, `:9`) |
| `override … SaveChanges` / Zeit in `ApplicationDbContext` | 0 – kein zentraler Audit-Setzer, jede Stelle setzt selbst |
| `TimeProvider` / `GetUtcNow` / `GetLocalNow` | nur `Services/BeschichtungsauftragWorkflow.cs:83` `GetLocalNow()` (Lokalzeit, korrekt) |
| `DateTimeOffset` | nur `HierarchischeStrukturStatus` (bewusst UTC, s. u.) und Lese-/Parse-Stellen von SData-Terminen (`AssumeLocal`) |
| JS `toISOString\|Date.UTC\|getUTC` | nur in `wwwroot/lib` (jquery-validation, html5-qrcode) – kein Browser schickt UTC-Zeiten |
| `SQL/AgentJobs/` Zeitfunktionen | nur `GETDATE()` (`02_Import_Artikel.sql:35`, `_archiv/01_…sql:94/105/120/131`) – Lokalzeit des SQL-Servers |
| SQL-Default-Constraints | alle `DEFAULT GETDATE()`/`SYSDATETIME()` außer `35_AddEnaioDmsDocuments.sql:14` `GETUTCDATE()` |
| Tests mit Assertion gegen `UtcNow` | 0 (`BeCloseTo(…UtcNow…)` o. ä. nicht vorhanden) – die Umstellung bricht keinen bestehenden Test; `TestDataHelper` setzt nur Testdaten |

Leser je Feld: alle Views mit `CreatedAt`/`ModifiedAt` (17 Dateien, Suche `CreatedAt|ModifiedAt` in `Views/`), alle C#-Lesestellen
(`.CreatedAt`/`.ModifiedAt` außerhalb von Zuweisungen, Vergleiche `<`/`>`/`OrderBy` auf Zeitspalten in C# und raw SQL), die Feldnamen
`ReleasedAt`, `ReportedAt`, `CancelledAt`, `CachedAt` einzeln über `*.cs`, `*.cshtml`, `*.js`.

### Bestand – umstellen (Fehlerbild ja)

| # | Tabelle.Spalte | Schreiber (UTC) | Leser / Anzeige | rechnet um? | Standort |
|---|---|---|---|---|---|
| F1 | `Users.CreatedAt` | `Controllers/UsersController.cs:131` (Anlegen), `:206` (AD-Benutzer anlegen), `Program.cs:260` (Admin-Seed) | `Views/Users/Index.cshtml:64` „Erstellt am“, Spaltenfilter `UsersController.cs:67` | nein | beide |
| F2 | `Articles.CreatedAt` (nur Dienst-Anlage) | `IDEALAKEWMSService/Services/SageImportService.cs:476` `GETUTCDATE()` (einziger Dienst-Anleger; manuelle Anlage `ArticlesController.cs:103` schreibt schon `DateTime.Now`) | `Views/Articles/Index.cshtml:118` „Erstellt am“ (ohne Filter, `:75` ohne `data-filterable`) | nein | beide |
| F3 | `WorkOperations.ReportedAt` | `Controllers/TrackingController.cs:255` (einziger Schreiber außer Rücknahme `:284` = null) | `Views/Tracking/Index.cshtml:268`, `Views/Tracking/ByWorkplace.cshtml:81` | nein | AKE (OSEON, `TrackingController` = Teileverfolgung, [[controller]]) |
| F4 | `StorageLocations.CreatedAt` (nur NAN) | `Program.cs:208` – greift nur, wenn NAN fehlt; `00_FreshInstall.sql:1537` legt NAN vorher mit `GETDATE()` an | `Views/StorageLocations/Index.cshtml:169`, Filter `StorageLocationsController.cs:44` | nein | beide |

### Bestand – umstellen (Fehlerbild nein: nirgends angezeigt oder verglichen, nur Konventionsbruch bzw. Mischung in derselben Spalte)

| # | Tabelle.Spalte | Schreiber (UTC) | Andere Schreiber derselben Spalte | Leser | Standort |
|---|---|---|---|---|---|
| N1 | `Users.ModifiedAt` | `AccountController.cs:225` (Profil), `UsersController.cs:310` (Bearbeiten) | – | keiner | beide |
| N2 | `Roles.CreatedAt` | `RolesController.cs:97`, `Program.cs:243` | SQL-Seeds `32_AddRoles.sql`, `37_…`, `42_…`, `67_…`, `68_…`, `74_…`, `77_…`, `78_…`, `00_FreshInstall.sql:1555 ff.` (GETDATE) | keiner | beide |
| N3 | `Roles.ModifiedAt` | `RolesController.cs:169` | – | keiner | beide |
| N4 | `UserRoles.CreatedAt` | `Data/Repositories/RoleRepository.cs:78`, `Program.cs:281` | `32_AddRoles.sql:99 ff.`, `00_FreshInstall.sql:1675` | keiner | beide |
| N5 | `OrderRecipientGroups.CreatedAt` / `.ModifiedAt` | `OrderRecipientsController.cs:78` / `:146` | – | keiner | beide |
| N6 | `OrderRecipients.CreatedAt` / `.ModifiedAt` | `OrderRecipientsController.cs:203` / `:232` | – | keiner | beide |
| N7 | `ArticleGroupRecipientMappings.CreatedAt` | `Data/Repositories/OrderRecipientRepository.cs:122` | – | keiner | beide |
| N8 | `UserViewPreferences.CreatedAt` / `.ModifiedAt` | `Data/Repositories/UserViewPreferenceRepository.cs:38` / `:27` | – | keiner | beide |
| N9 | `ProductionOrderPickingStatus.ReleasedAt` | `ProductionOrderPickingStatusRepository.cs:78` (Einzel), `:246`/`:268` (Sammel) | – | nur Durchreichen in `PickingLeitstandController.cs:149` → `PickingLeitstandViewModel.cs:102`; kein View/JS liest es (Suche `ReleasedAt` in `*.cshtml`/`*.js`: 0) | beide |
| N10 | `ProductionOrderPickingStatus.ModifiedAt` | `ProductionOrderPickingStatusRepository.cs:88`, `:284`; `CoatingDetectionService.cs:247`, `:255` | **gemischt**: `ProductionOrderPickingStatusRepository.cs:62/103/117/131/166`, `FaZusatzinfoSyncService.cs:226` schreiben `DateTime.Now` | keiner | beide (Coating nur AKE: Gate `CoatingDetectionService.cs:40`) |
| N11 | `ProductionOrderPickingStatus.CreatedAt` | `SageImportService.cs:177` (Eager-Create) | `60_ProductionOrderSplit.sql:197` | keiner | beide¹ |
| N12 | `ProductionOrderBdeStatus.CreatedAt` | `SageImportService.cs:186` | `60_ProductionOrderSplit.sql:240` | keiner | beide¹ |
| N13 | `WorkOperations.ModifiedAt` | `TrackingController.cs:258`, `:287` | – | keiner | AKE |
| N14 | `ProductionOrders.CreatedAt` | `SageProductionOrderSql.cs:17`, `:18` | AgentJob `_archiv/01` (GETDATE, außer Betrieb), `FaMaterializationSyncService` (Lokalzeit, IDEAL) | keiner | beide¹ |
| N15 | `ProductionOrders.ModifiedAt` | `SageProductionOrderSql.cs:31`, `SageImportService.cs:291`, `:327`, `OseonSyncService.cs:641` | `FaMaterializationSyncService.cs:180` (`now` = `DateTime.Now`, `:127`), Web-Controller mit `DateTime.Now` (z. B. `FaCompletionController.cs:431`) | keiner | beide¹ |
| N16 | `ProductionOrders.CancelledAt` | `SageImportService.cs:326` (Reconcile) | – (Reaktivierung `:290` setzt NULL) | Model `ProductionOrder.cs:106`, kein View/Mail/SQL liest es | beide¹ |
| N17 | `Articles.ModifiedAt` | `SageImportService.cs:453` | `ArticlesController.cs:203` (`DateTime.Now`) | keiner | beide |
| N18 | `ProductionWorkplaces.CreatedAt` (OSEON-Anlage) | `OseonSyncService.cs:255` | Web-Anlage, `ProductionWorkplaceSyncService` (Lokalzeit, IDEAL) | keiner | AKE |
| N19 | `OseonProductionOrders.CreatedAt` / `.ModifiedAt` | `OseonSyncService.cs:432` / `:421` | – | keiner | AKE |
| N20 | `OseonWorkOperations.CreatedAt` / `.ModifiedAt` | `OseonSyncService.cs:589` / `:580` | – | keiner | AKE |
| N21 | `ArticleAttributeDefinitions.CreatedAt`, `ArticleAttributeValues.CreatedAt` / `.ModifiedAt` (Sage-Benutzerfelder) | `IDEALAKEWMSService/Services/Sdata/ArtikelBenutzerfelderWriter.cs:30` (`jetzt`, verwendet `:54`, `:97`, `:106`) | **gemischt**: Web `ArticleAttributesController.cs:148`, `ArticleAttributeRepository.cs:176` (`DateTime.Now`) | keiner | beide (Quelle SData) |

¹ `SageImportService.SyncProductionOrdersAsync` hat kein Hierarchie-Gate; er läuft, wenn `Sync:ProductionOrdersEnabled` (Default `true`,
`Workers/SyncWorker.cs:43`) gesetzt ist. Ob er bei IDEAL aktiv ist, entscheidet die Konfiguration, nicht der Code – für die Umstellung
unerheblich, für die Altdaten-Prüfabfrage A8 auf beiden DBs laufen lassen.

Kommentar mit veraltetem Inhalt, mit anpassen: `SageImportService.cs:323-324` („CancelledAt via GETUTCDATE() (wie ModifiedAt)“).

### Entscheidung offen (UTC, Anzeige rechnet um – kein Fehlerbild)

| # | Tabelle.Spalte | Schreiber | Leser | rechnet um? |
|---|---|---|---|---|
| E1 | `ArticleSageDetails.CreatedAt` / `.ModifiedAt` | `IDEALAKEWMSService/Services/Sdata/ArticleSageDetailsWriter.cs:27` (`jetzt`, verwendet `:36`, `:47`) | `Views/Shared/_ArtikelSageKarten.cshtml:18` `(sd.ModifiedAt ?? sd.CreatedAt).ToLocalTime()` (Kommentar `:17` „UTC gespeichert, lokal angezeigt“) | **ja** – EF liest `datetime2` als `Kind=Unspecified`, `ToLocalTime()` behandelt das als UTC → Anzeige korrekt |

Wird E1 umgestellt (Rückfrage 12), müssen **Writer und Anzeige im selben Commit** wechseln, sonst zeigt die Karte 1–2 h zu spät.
Die Schreiber `ArticleSageDetailsWriter`/`ArtikelBenutzerfelderWriter` liegen nur im Bündel (nicht in `main`; Deploy-Fahrplan
[[2026-11-12-deploy-fahrplan-buendel-v1-99]]: IDEAL-Prod v1.71.1, AKE-Prod main v1.30.x) – Altdaten nur auf DEV/Test.

### Bewusst UTC – bleibt, nur ausweisen

| # | Stelle | Warum UTC richtig ist | Leser |
|---|---|---|---|
| U1 | `IdealAkeWms/Services/PdfRenderService.cs:89` `cutoff = DateTime.UtcNow - …` | Vergleich mit `Directory.GetCreationTimeUtc(dir)` (`:94`), Dateisystem-Zeit; nicht gespeichert | nur `:94` |
| U2 | `IdealAkeWms/Services/HierarchischeStruktur/HierarchischeStrukturStatus.cs:28` `LastRefreshedUtc = DateTimeOffset.UtcNow` | im Speicher, `DateTimeOffset` trägt den Versatz; nicht gespeichert | `Views/StandortEinstellungen/Index.cshtml:55` `.LocalDateTime`, `Views/HierarchieUmstellung/Index.cshtml:81` `.ToLocalTime()` – rechnen um, korrekt |
| U3 | `CachedBomHeaders.CachedAt` – `IDEALAKEWMSService/Services/BomCacheSyncService.cs:510`, `:529` `GETUTCDATE()` | intern konsistent: verglichen nur mit `DateTime.UtcNow.AddHours(-maxAgeHrs)` (`:100`, `:224`) über `:119`, `:233`; nie angezeigt (Suche `CachedAt` in Views/JS: 0). Umstellen hieße drei Stellen gemeinsam ändern, ohne Nutzen | `:119`, `:233` (über `ReadHeaderHashesAsync` `:454-474`) |
| U4 | `SQL/35_AddEnaioDmsDocuments.sql:14` `CreatedAt … DEFAULT GETUTCDATE()` | **toter Default**: der einzige Schreiber setzt `CreatedAt` immer explizit auf `@Now` = `DateTime.Now` (`IDEALAKEWMSService/Services/EnaioDmsSyncService.cs:143`, `:161`, `:166`); `00_FreshInstall.sql:2645` hat gar keinen Default | – |

Hinweise zu U3: Der EF-Pfad `Data/Repositories/BomCacheRepository.cs:109`, `:120` schreibt `DateTime.Now`, hat aber keinen
Produktionsaufrufer (Suche `UpsertBomAsync`: nur `BomRepositoryMasterSwitch`/`FaHierarchyBomRepository`-Durchreichung und Tests;
vgl. [[fallstricke]] „Raw-SQL-Pfade sind nicht InMemory-testbar“). Der Doc-Kommentar `Models/FaHierarchyNode.cs:61` („Lokalzeit, analog
`CachedBomHeader.CachedAt`“) ist damit irreführend – Zusatzfund, im Dev-Lauf den Kommentar korrigieren (nur Text).
`SyncLogger/SyncRun.cs:86` und `PartRequisitionEmailService.cs:269` enthalten `UtcNow`/`GETUTCDATE` nur im Kommentar.

### Regel nach der Umstellung

- Fachliche und Audit-Zeitstempel (`CreatedAt`, `ModifiedAt`, `…At` mit fachlicher Bedeutung) werden in **Lokalzeit des schreibenden
  Rechners** geschrieben: Web `DateTime.Now`, Dienst `DateTime.Now` als SQL-Parameter (nicht `GETDATE()` – Zeitzone des SQL-Servers ist
  nicht belegt, gleiche Begründung wie [[2026-11-12-befunde-ake-regression-dev-spec]] Punkt 3).
- Innerhalb **eines** Laufs/Statements eine Zeit (`var jetzt = DateTime.Now` einmal, wie heute `:246` im Sammel-Release), damit
  zusammengehörige Zeilen denselben Stempel tragen.
- Ausnahmen sind nur U1–U3 (+ der tote Default U4); sie stehen in der Ausnahmeliste des Wächter-Tests und im ADR.

## Technischer Loesungsentwurf

Kleinster Eingriff, keine neue Abstraktion (kein Uhr-Dienst, kein `TimeProvider`-Umbau – `DateTime.Now` ist die bestehende Konvention
an rund 50 Stellen, siehe Suche `ModifiedAt = DateTime.Now`).

**Web:** `DateTime.UtcNow` → `DateTime.Now` in `UsersController.cs:131/206/310`, `AccountController.cs:225`,
`RolesController.cs:97/169`, `OrderRecipientsController.cs:78/146/203/232`, `TrackingController.cs:255/258/287`,
`Program.cs:208/243/260/281`, `RoleRepository.cs:78`, `OrderRecipientRepository.cs:122`, `UserViewPreferenceRepository.cs:27/38`,
`ProductionOrderPickingStatusRepository.cs:78/88/246`. In `TrackingController.Report` eine lokale Variable für `ReportedAt` und
`ModifiedAt` (heute zwei getrennte `UtcNow`-Aufrufe `:255`/`:258`); ebenso `SetReleaseAsync` `:78`/`:88`.

**Dienst, raw SQL:** `GETUTCDATE()` → `@Jetzt`, Parameter `DateTime.Now` als `SqlDbType.DateTime2` (wie `@jetzt` in
`PartRequisitionEmailService`):
- `SageProductionOrderSql.cs:17/18/31` (Konstante im Builder) + Parameter am Aufrufer `SageImportService.cs:123-130`.
- `SageImportService.cs:177/186` (Eager-Create, `eagerCmd` `:189`), `:291` (`reactCmd`), `:326/327` (`cancelCmd`), `:453/476` (Artikel-Upsert).
- `OseonSyncService.cs:255` (Werkbank-Insert), `:421/432` (Auftrags-MERGE), `:580/589` (AG-MERGE), `:641` (Werkbank-Übertrag).
- `CoatingDetectionService.cs:247/255`.
- Je Lauf einmal `var jetzt = DateTime.Now;` am Methodenanfang, an alle Kommandos derselben Methode durchreichen.

**Dienst, EF:** `ArtikelBenutzerfelderWriter.cs:30` → `DateTime.Now`; `ArticleSageDetailsWriter.cs:27` → `DateTime.Now` **und**
`_ArtikelSageKarten.cshtml:17-18` ohne `ToLocalTime()`, Kommentar angepasst (nur bei Rückfrage 12 = umstellen).

**Wächter-Test (neu, `IdealAkeWms.Tests`):** nach dem Muster der vorhandenen Quelltext-Scans (z. B. `Views/VaultEnaioLinksTests.cs`,
`Helpers/CsvExportCoverageTests.cs`): alle `*.cs`/`*.cshtml` unter `IdealAkeWms/` und `IDEALAKEWMSService/` (ohne `bin`, `obj`,
`wwwroot/lib`, `Migrations`) nach `UtcNow|GETUTCDATE|SYSUTCDATETIME` durchsuchen; erlaubt nur die Ausnahmeliste
`PdfRenderService.cs`, `HierarchischeStrukturStatus.cs`, `BomCacheSyncService.cs`, `SyncRun.cs` (Kommentar),
`PartRequisitionEmailService.cs` (Kommentar). Die Fehlermeldung nennt Datei:Zeile und verweist auf ADR 0025. Ein Treffer außerhalb der
Liste lässt den Test fehlschlagen.

**Tests (bestehendes Muster, InMemory):**
- `ProductionOrderPickingStatusRepositoryTests`: `SetReleaseAsync` und `SetReleaseBatchAsync` – `ReleasedAt`/`ModifiedAt` nahe
  `DateTime.Now` (Toleranz 1 Minute; wie `PartRequisitionRepositoryTests` in v1.99.13).
- `UserViewPreferenceRepositoryTests`, `OrderRecipientRepositoryTests` (`SetMappingsForArticleGroupAsync`), Rollen-Zuweisung
  (`RoleRepository.SetUserRolesAsync`): je ein Zeit-Assert.
- `SageProductionOrderSqlTests`: zusätzlich `BuildUpsert(true|false)` enthält kein `GETUTCDATE` und `@Jetzt` in INSERT und UPDATE;
  Spalten-/Wertezahl bleibt gleich (bestehende Asserts `:93-94`, `:116-117` laufen unverändert).
- Raw-SQL-Pfade (`SageImportService`, `OseonSyncService`, `CoatingDetectionService`) sind nicht InMemory-testbar → Manual-UAT mit
  Abfrage ([[fallstricke]] „Raw-SQL-Pfade sind nicht InMemory-testbar“).

Muster: Repository-Pattern unverändert, keine neue Liste, kein neuer Dienst, keine neue Abhängigkeit. Kein Epic: rund 45 geänderte
Zeilen in 18 Dateien, keine Schema-Änderung, ein Dev-Lauf; Reihenfolge der Commits frei (Web, Dienst, Wächter zuletzt).

## Migrations-/SQL-Auswirkungen

- **Keine Schema-Änderung, keine EF-Migration**, kein `SQL/1xx_*.sql`; `SQL/00_FreshInstall.sql` und `SQL/AgentJobs/` unberührt
  (AgentJobs schreiben `GETDATE()`, kein UTC – Suchweg oben). SQL-Nummer 148 bleibt frei.
- `SQL/35_AddEnaioDmsDocuments.sql:14` bleibt (toter Default, U4); ihn zu entfernen bräuchte ein eigenes Schema-Skript ohne Nutzen.
- **Altdaten:** Empfehlung für alle Tabellen „unverändert“ (Rückfragen 1–11), wie bei den Bedarfsmeldungen entschieden. Gründe: die
  meisten Felder werden nirgends angezeigt; mehrere Spalten sind schon heute gemischt (SQL-Seeds mit `GETDATE()`, Web mit `DateTime.Now`,
  Program.cs mit `UtcNow` – alle mit `CreatedBy = 'system'`), eine Umrechnung kann die UTC-Zeilen nicht sicher von den lokalen trennen.
  Falls eine Rückfrage mit „umrechnen“ beantwortet wird: Einmal-Skript je Tabelle unter `SQL/Einmalig/` nach ADR
  [[0015-einmal-datenskripte-ausserhalb-der-nummerierten-sql-reihe]] und ADR 0023 (genau ein `DECLARE @Trockenlauf BIT = 1;`, Zeile
  `TREFFER: n` im Trockenlauf, kein `ROLLBACK`, keine sqlcmd-Direktiven), sommerzeitrichtig per
  `AT TIME ZONE 'UTC' AT TIME ZONE 'W. Europe Standard Time'`, nur Zeilen des UTC-Schreibers (Filter `CreatedBy`/`ModifiedBy` wie in der
  Prüfabfrage) und nur vor einem festen Stichtag = Deploy-Zeitpunkt; ein zweiter Lauf darf nichts verschieben (Marker `DeployScriptRuns`).

**Prüfabfragen (lesend, je Tabelle; auf AKE-Prod, IDEAL-Prod und den DEV/Test-DBs ausführen, Ergebnis in die Freigabe-Antwort):**

```sql
-- A1 Users (UTC: UsersController/AccountController = CreatedBy Anzeigename; 'system' gemischt)
SELECT CreatedBy, COUNT(*) AS Zeilen, MIN(CreatedAt) AS Erste, MAX(CreatedAt) AS Letzte,
       SUM(CASE WHEN ModifiedAt IS NOT NULL THEN 1 ELSE 0 END) AS MitAenderung
FROM dbo.Users GROUP BY CreatedBy ORDER BY Zeilen DESC;

-- A2 Roles + UserRoles
SELECT 'Roles' AS Tabelle, CreatedBy, COUNT(*) AS Zeilen, MIN(CreatedAt) AS Erste, MAX(CreatedAt) AS Letzte,
       SUM(CASE WHEN ModifiedAt IS NOT NULL THEN 1 ELSE 0 END) AS MitAenderung
FROM dbo.Roles GROUP BY CreatedBy
UNION ALL
SELECT 'UserRoles', CreatedBy, COUNT(*), MIN(CreatedAt), MAX(CreatedAt), NULL
FROM dbo.UserRoles GROUP BY CreatedBy;

-- A3 Empfaenger
SELECT 'OrderRecipientGroups' AS Tabelle, COUNT(*) AS Zeilen, MIN(CreatedAt) AS Erste, MAX(CreatedAt) AS Letzte FROM dbo.OrderRecipientGroups
UNION ALL SELECT 'OrderRecipients', COUNT(*), MIN(CreatedAt), MAX(CreatedAt) FROM dbo.OrderRecipients
UNION ALL SELECT 'ArticleGroupRecipientMappings', COUNT(*), MIN(CreatedAt), MAX(CreatedAt) FROM dbo.ArticleGroupRecipientMappings;

-- A4 Ansichtseinstellungen
SELECT COUNT(*) AS Zeilen, MIN(CreatedAt) AS Erste, MAX(COALESCE(ModifiedAt, CreatedAt)) AS Letzte FROM dbo.UserViewPreferences;

-- A5 NAN-Platz
SELECT Id, Code, CreatedAt, CreatedBy FROM dbo.StorageLocations WHERE Code = 'NAN';

-- A6 Status-Satelliten
SELECT 'PickingStatus' AS Tabelle, CreatedBy, COUNT(*) AS Zeilen,
       SUM(CASE WHEN ReleasedAt IS NOT NULL THEN 1 ELSE 0 END) AS MitFreigabe, MIN(ReleasedAt) AS ErsteFreigabe, MAX(ReleasedAt) AS LetzteFreigabe
FROM dbo.ProductionOrderPickingStatus GROUP BY CreatedBy
UNION ALL
SELECT 'BdeStatus', CreatedBy, COUNT(*), NULL, NULL, NULL FROM dbo.ProductionOrderBdeStatus GROUP BY CreatedBy;

-- A7 Teileverfolgung (AKE)
SELECT COUNT(*) AS Zeilen, SUM(CASE WHEN ReportedAt IS NOT NULL THEN 1 ELSE 0 END) AS Rueckgemeldet,
       MIN(ReportedAt) AS Erste, MAX(ReportedAt) AS Letzte
FROM dbo.WorkOperations;

-- A8 Fertigungsauftraege (UTC = CreatedBy/ModifiedBy 'IDEALAKEWMSService')
SELECT CreatedBy, COUNT(*) AS Zeilen, MIN(CreatedAt) AS Erste, MAX(CreatedAt) AS Letzte,
       SUM(CASE WHEN CancelledBy = 'System-Reconcile' THEN 1 ELSE 0 END) AS ReconcileStorno,
       SUM(CASE WHEN ModifiedBy = 'IDEALAKEWMSService' THEN 1 ELSE 0 END) AS DienstGeaendert
FROM dbo.ProductionOrders GROUP BY CreatedBy ORDER BY Zeilen DESC;

-- A9 Artikel (UTC = CreatedBy/ModifiedBy 'IDEALAKEWMSService')
SELECT CreatedBy, COUNT(*) AS Zeilen, MIN(CreatedAt) AS Erste, MAX(CreatedAt) AS Letzte,
       SUM(CASE WHEN ModifiedBy = 'IDEALAKEWMSService' THEN 1 ELSE 0 END) AS DienstGeaendert
FROM dbo.Articles GROUP BY CreatedBy ORDER BY Zeilen DESC;

-- A10 OSEON (AKE)
SELECT 'ProductionWorkplaces' AS Tabelle, COUNT(*) AS Zeilen, MIN(CreatedAt) AS Erste, MAX(CreatedAt) AS Letzte
FROM dbo.ProductionWorkplaces WHERE CreatedBy = 'IDEALAKEWMSService'
UNION ALL SELECT 'OseonProductionOrders', COUNT(*), MIN(CreatedAt), MAX(CreatedAt) FROM dbo.OseonProductionOrders
UNION ALL SELECT 'OseonWorkOperations', COUNT(*), MIN(CreatedAt), MAX(CreatedAt) FROM dbo.OseonWorkOperations;

-- A11 SData-Tabellen (nur Bündel-Stand; auf Prod vorher pruefen, ob die Tabelle existiert)
IF OBJECT_ID(N'dbo.ArticleSageDetails', N'U') IS NOT NULL
    SELECT 'ArticleSageDetails' AS Tabelle, COUNT(*) AS Zeilen, MIN(CreatedAt) AS Erste, MAX(COALESCE(ModifiedAt, CreatedAt)) AS Letzte
    FROM dbo.ArticleSageDetails;
SELECT 'ArticleAttributeDefinitions' AS Tabelle, COUNT(*) AS Zeilen FROM dbo.ArticleAttributeDefinitions WHERE CreatedBy = 'IDEALAKEWMSService'
UNION ALL SELECT 'ArticleAttributeValues', COUNT(*) FROM dbo.ArticleAttributeValues WHERE CreatedBy = 'IDEALAKEWMSService';
```

Zur Information (nicht entscheidungsrelevant): Uhr des SQL-Servers, auf der `GETDATE()` in SQL-Seeds/AgentJobs beruht –
`SELECT SYSDATETIMEOFFSET() AS Server, GETDATE() AS Lokal, GETUTCDATE() AS Utc;` (AKESQL20 und IDEAL-SQL). Nicht belegt, daher unverifiziert.

## Einstellungen

keine

## Audit-Feld-Auswirkungen

- Betroffen sind nur die **Werte** von `CreatedAt`/`ModifiedAt` (und `ReleasedAt`, `ReportedAt`, `CancelledAt`) der Tabellen oben – künftig
  Lokalzeit. `ModifiedBy`/`ModifiedByWindows` setzen alle Stellen schon heute (Belege in den Zeilen der Tabellen, z. B.
  `UsersController.cs:311-312`, `TrackingController.cs:259-260`, raw SQL `'IDEALAKEWMSService', SYSTEM_USER`) – unverändert.
- Keine neue Entität, kein neues Pflichtfeld. Kein Rollen- oder Filter-Effekt; keine neue Liste (Listen-View-Pattern nicht berührt).

## Akzeptanzkriterien

1. `/Users`: ein neu angelegter Benutzer (Formular **und** „AD-Benutzer anlegen“) zeigt in „Erstellt am“ die Ortszeit der Anlage
   (± 1 Minute); der Spaltenfilter „Erstellt am“ mit dem heutigen Datum findet ihn.
2. Benutzer bearbeiten und eigenes Profil speichern setzen `Users.ModifiedAt` auf Ortszeit (Abfrage).
3. Rolle anlegen, Rolle bearbeiten, Rollen zuweisen: `Roles.CreatedAt/ModifiedAt`, `UserRoles.CreatedAt` in Ortszeit (Abfrage).
4. Empfängergruppe anlegen/bearbeiten, Empfänger anlegen/bearbeiten, Artikelgruppen-Zuordnung speichern: Zeiten in Ortszeit (Abfrage).
5. Ansichtseinstellung speichern (neu und Änderung): `UserViewPreferences.CreatedAt/ModifiedAt` in Ortszeit.
6. Leitstand: Einzelfreigabe, Sammelfreigabe und Rücknahme schreiben `ReleasedAt`/`ModifiedAt` in Ortszeit; alle Zeilen einer
   Sammelfreigabe tragen denselben Zeitstempel.
7. AKE, Teileverfolgung: „Rückmelden“ zeigt „dd.MM.yyyy HH:mm — Name“ in Ortszeit (Übersicht und je Werkbank); `ReportedAt` = `ModifiedAt`;
   „Rücknahme“ setzt `ModifiedAt` in Ortszeit.
8. Dienst, Artikel-Abgleich: ein neu angelegter Artikel zeigt in `/Articles` „Erstellt am“ die Ortszeit des Laufs; eine Änderung setzt
   `Articles.ModifiedAt` in Ortszeit.
9. Dienst, FA-Import: neue FA und ihre Status-Zeilen (`ProductionOrderPickingStatus`, `ProductionOrderBdeStatus`) mit `CreatedAt` in Ortszeit;
   geänderte FA `ModifiedAt`; Reconcile-Storno `CancelledAt` = `ModifiedAt` in Ortszeit; Reaktivierung `ModifiedAt` in Ortszeit.
10. AKE, Dienst: OSEON-Sync schreibt neue Werkbänke, Aufträge, Arbeitsgänge und den Werkbank-Übertrag in Ortszeit; Lackierteil-Erkennung
    setzt `ModifiedAt` in Ortszeit.
11. Dienst, SData: Sage-Benutzerfelder (`ArticleAttributeDefinitions/Values`) in Ortszeit; `ArticleSageDetails` nach Rückfrage 12 – bei
    „umstellen“ zeigt die Karte „Sage-Zusatzfelder“ die Ortszeit der letzten Änderung ohne Versatz (Writer und Anzeige gemeinsam).
12. Bewusst-UTC-Stellen U1–U3 unverändert: PDF-Erzeugung räumt verwaiste Verzeichnisse weiter auf, Standort-/Hierarchie-Seiten zeigen den
    Stand in Ortszeit, BOM-Cache überspringt unveränderte Artikel innerhalb der Höchstalter-Frist wie bisher.
13. Wächter-Test: findet im Produktivcode kein `UtcNow`/`GETUTCDATE`/`SYSUTCDATETIME` außerhalb der Ausnahmeliste; ein testweise
    eingefügtes `DateTime.UtcNow` in einem Controller lässt ihn mit Datei:Zeile fehlschlagen.
14. Altdaten je Tabelle wie in den Freigabe-Antworten 1–11 entschieden; Zahlen der Prüfabfragen im Brain-Changelog.
15. Build und alle Tests grün; neue Tests: Wächter, Zeit-Asserts der Repositories (Toleranz 1 Minute gegen `DateTime.Now`),
    `SageProductionOrderSqlTests` ohne `GETUTCDATE`.

## Test-Szenarien

Für `docs/TESTSZENARIEN.md` (Nummer Vorschlag **TS-133**, der Lauf vergibt neu; danach `secondbrain/tests/testszenarien-index.md`):

**TS-133 „Zeitstempel in Ortszeit“** (AKE und IDEAL; Schritte mit „AKE“ nur dort)
- Vorbedingung: DEV auf der neuen Version; Uhrzeit des Testrechners notieren; Abfrage-Zugriff auf die DEV-DB.
- 133.1 Benutzer: Benutzer über Formular und über „AD-Benutzer anlegen“ anlegen → `/Users` „Erstellt am“ = Ortszeit; Spaltenfilter
  „Erstellt am“ `=<heute>` findet beide. Benutzer bearbeiten, eigenes Profil speichern →
  `SELECT TOP 3 Name, CreatedAt, ModifiedAt FROM dbo.Users ORDER BY COALESCE(ModifiedAt, CreatedAt) DESC`.
- 133.2 Rollen und Empfänger: Rolle anlegen/bearbeiten/zuweisen; Empfängergruppe, Empfänger, Artikelgruppen-Zuordnung → Abfrage je Tabelle
  `ORDER BY Id DESC`, Zeiten = Ortszeit.
- 133.3 Ansichtseinstellung: Spaltenauswahl einer Liste ändern → `dbo.UserViewPreferences` neueste Zeile = Ortszeit.
- 133.4 Leitstand: einen FA einzeln freigeben, zwei per Sammelfreigabe, einen zurücknehmen →
  `SELECT ProductionOrderId, ReleasedAt, ModifiedAt FROM dbo.ProductionOrderPickingStatus ORDER BY ModifiedAt DESC` – Ortszeit, Sammelzeilen gleich.
- 133.5 (AKE) Teileverfolgung: Arbeitsgang rückmelden → Anzeige Ortszeit in Übersicht und Werkbank-Ansicht; zurücknehmen → Anzeige weg,
  `ModifiedAt` Ortszeit.
- 133.6 Dienst-Läufe (Testsystem mit aktivem Sync; DEV hat die Syncs gesperrt): nach einem Lauf
  `SELECT TOP 5 ArticleNumber, CreatedAt, ModifiedAt FROM dbo.Articles WHERE CreatedBy='IDEALAKEWMSService' OR ModifiedBy='IDEALAKEWMSService' ORDER BY COALESCE(ModifiedAt, CreatedAt) DESC`,
  analog `dbo.ProductionOrders`, `dbo.ProductionOrderPickingStatus` (CreatedBy `IDEALAKEWMSService`/ModifiedBy `CoatingDetection`), AKE:
  `dbo.OseonProductionOrders`, `dbo.OseonWorkOperations` – Zeiten = Laufzeit des Dienstes (Aktivitäts-Protokoll) ± 1 Minute.
- 133.7 SData: Artikelinfo eines per SData gelesenen Artikels → Karte „Sage-Zusatzfelder“ „letzte Änderung“ = Ortszeit des Laufs (nach
  Rückfrage 12); Benutzerfeld-Werte `dbo.ArticleAttributeValues` neueste Zeile Ortszeit.
- Negativfälle: PDF-Druck (z. B. Auftrag drucken) funktioniert unverändert; `/StandortEinstellungen` „Stand der Sperr-Prüfung“ und
  `/HierarchieUmstellung` zeigen Ortszeit; BOM-Cache-Lauf (AKE) überspringt unveränderte Artikel wie vorher (Aktivitäts-Protokoll
  „uebersprungen“); Altdaten unverändert bzw. umgerechnet laut Freigabe (Stichprobe einer alten Zeile).

## Etappen (nur bei epic: true)

entfällt (kein Epic – rund 45 Zeilen in 18 Dateien, keine Schema-Änderung, ein Dev-Lauf)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

**Vorläufig** – der QA-Lauf bestätigt gegen den echten Diff. Version Vorschlag **v1.99.16** (Bündel steht auf v1.99.15; der Lauf prüft),
beide `AppVersion.cs`, Anwender-Changelog (Satz: „Zeitstempel einheitlich in Ortszeit; ältere Einträge bleiben, wie sie sind“),
Brain-Changelog mit den Zahlen der Prüfabfragen, ADR 0025 (Vorschlag), [[fallstricke]]-Ergänzung.

- **Web-App:** ja (Controller, Repositories, `Program.cs`-Seeds, ggf. `_ArtikelSageKarten.cshtml`)
- **Service:** ja (`SageImportService`, `SageProductionOrderSql`, `OseonSyncService`, `CoatingDetectionService`, SData-Writer)
- **Migration:** nein
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): **keine** – bei Empfehlung „Altdaten unverändert“ gibt es kein Skript, keine
  Einstellung, kein Recht, keinen Ordner. **Nur falls** eine Rückfrage 1–11 mit „umrechnen“ beantwortet wird, je Tabelle:
  `Id = Ortszeit<Tabelle>` (z. B. `OrtszeitWorkOperations`), Version = Version dieses Fixes, Typ `Einmalig`, Firma laut Tabelle
  (`AKE` für A7/A10, sonst `beide`), Phase `Nachher`, Ausführung `Trockenlauf`, Text „<Tabelle>: alte UTC-Zeitstempel einmalig in Ortszeit
  umrechnen; Trockenlauf zeigt TREFFER, dann bestätigen“; Skript unter `SQL/Einmalig/` nach ADR 0015/0023.
- **Publish-Befehle** (im Worktree):
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```
- Reihenfolge: Dienst stoppen → Web → Dienst (kein DB-Schritt). QA testet auf AKE-DEV und IDEAL-DEV (`standort: beide`); OSEON- und
  Teileverfolgungs-Schritte nur AKE; Dienst-Schritte (133.6) brauchen ein Testsystem mit aktiven Syncs.

## Offene Rueckfragen

1. **Altdaten `dbo.Users`** (`CreatedAt` sichtbar in `/Users`, `ModifiedAt`): umrechnen oder unverändert? Empfehlung **unverändert** –
   Zeilen mit `CreatedBy = 'system'` sind gemischt (SQL-Seed `GETDATE()` vs. `Program.cs:260` `UtcNow`), nicht sicher trennbar. Prüfabfrage A1.
2. **Altdaten `dbo.Roles` + `dbo.UserRoles`:** umrechnen oder unverändert? Empfehlung **unverändert** (nirgends angezeigt, SQL-Seeds gemischt).
   Prüfabfrage A2.
3. **Altdaten `dbo.OrderRecipientGroups`, `dbo.OrderRecipients`, `dbo.ArticleGroupRecipientMappings`:** umrechnen oder unverändert?
   Empfehlung **unverändert** (nirgends angezeigt). Prüfabfrage A3.
4. **Altdaten `dbo.UserViewPreferences`:** umrechnen oder unverändert? Empfehlung **unverändert** (nirgends angezeigt). Prüfabfrage A4.
5. **Altdaten `dbo.StorageLocations`** (nur NAN, falls von `Program.cs` angelegt): umrechnen oder unverändert? Empfehlung **unverändert**
   (eine Zeile). Prüfabfrage A5.
6. **Altdaten `dbo.ProductionOrderPickingStatus`** (`ReleasedAt`, `ModifiedAt`, `CreatedAt`) **+ `dbo.ProductionOrderBdeStatus`**
   (`CreatedAt`): umrechnen oder unverändert? Empfehlung **unverändert** (nirgends angezeigt; `ModifiedAt` je Zeile gemischt). Prüfabfrage A6.
7. **Altdaten `dbo.WorkOperations`** (`ReportedAt` sichtbar in der Teileverfolgung, nur AKE; `ModifiedAt`): umrechnen oder unverändert?
   Empfehlung **unverändert**, sofern A7 wenige oder alte Rückmeldungen zeigt; sonst Einmal-Skript nur für `ReportedAt`. Prüfabfrage A7.
8. **Altdaten `dbo.ProductionOrders`** (`CreatedAt`, `ModifiedAt`, `CancelledAt` der Dienst-Schreiber): umrechnen oder unverändert?
   Empfehlung **unverändert** (nirgends angezeigt). Prüfabfrage A8.
9. **Altdaten `dbo.Articles`** (`CreatedAt` der Dienst-Anlage, sichtbar in `/Articles` „Erstellt am“; `ModifiedAt`): umrechnen oder
   unverändert? Empfehlung **unverändert** (nur Anzeige ohne Filter, 1–2 h Versatz). Prüfabfrage A9.
10. **Altdaten OSEON-Tabellen** `dbo.ProductionWorkplaces` (OSEON-Anlage), `dbo.OseonProductionOrders`, `dbo.OseonWorkOperations` (nur AKE):
    umrechnen oder unverändert? Empfehlung **unverändert** (nirgends angezeigt). Prüfabfrage A10.
11. **Altdaten SData-Tabellen** `dbo.ArticleSageDetails`, `dbo.ArticleAttributeDefinitions`/`dbo.ArticleAttributeValues` (Sage-Benutzerfelder):
    umrechnen oder unverändert? Die Schreiber gibt es nur im Bündel (IDEAL-Prod v1.71.1, AKE-Prod main v1.30.x ohne sie) – Altdaten nur auf
    DEV/Test. Empfehlung **unverändert**. Prüfabfrage A11.
12. **`ArticleSageDetails` umstellen oder bewusste UTC-Ausnahme?** Heute UTC geschrieben und in der Karte per `ToLocalTime()` korrekt
    angezeigt (kein Fehlerbild). Empfehlung **umstellen** (Writer `DateTime.Now` + `ToLocalTime()` entfernen, im selben Commit) – noch nicht
    in Produktion, eine Konvention statt zwei.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Altdaten Users umrechnen/unverändert (Ergebnis A1) →
2. Altdaten Roles + UserRoles umrechnen/unverändert (Ergebnis A2) →
3. Altdaten Empfänger-Tabellen umrechnen/unverändert (Ergebnis A3) →
4. Altdaten UserViewPreferences umrechnen/unverändert (Ergebnis A4) →
5. Altdaten StorageLocations NAN umrechnen/unverändert (Ergebnis A5) →
6. Altdaten PickingStatus + BdeStatus umrechnen/unverändert (Ergebnis A6) →
7. Altdaten WorkOperations (AKE) umrechnen/unverändert (Ergebnis A7) →
8. Altdaten ProductionOrders umrechnen/unverändert (Ergebnis A8) →
9. Altdaten Articles umrechnen/unverändert (Ergebnis A9) →
10. Altdaten OSEON-Tabellen (AKE) umrechnen/unverändert (Ergebnis A10) →
11. Altdaten SData-Tabellen umrechnen/unverändert (Ergebnis A11) →
12. ArticleSageDetails umstellen oder UTC-Ausnahme →
