---
type: spec
title: "Bugfix Sicherheit: Anmeldung nur mit Kennwort oder Windows, Erstinstallation, Installer ohne Klartext-Zugangsdaten (BH-1 bis BH-5)"
slug: 2026-11-14-sicherheit-anmeldung-und-installer-spec
status: Entwurf
created: 2026-11-14
updated: 2026-11-14
source_backlog: "[[2026-11-14-sicherheit-anmeldung-und-installer]]"
weitere_quellen:
  - "[[2026-11-14-betriebshandbuch-goal-lauf]]"
  - "[[2026-11-04-ideal-produktiv-installation]]"
  - "[[2026-11-12-freshinstall-leere-db-fehler]]"
bug: "[[2026-11-14-sicherheit-anmeldung-und-installer-bug]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
version_vorschlag: "1.99.24"
sql_vorschlag: "151 (nur wenn Rückfrage 4 die Index-Reparatur bestätigt)"
ts_vorschlag: "TS-139"
adr_vorschlag: "0027"
affected_code:
  - "IdealAkeWms/Controllers/AccountController.cs (Login POST :64-116: Kennwortregel :82-101, Erstkennwort-Schritt)"
  - "IdealAkeWms/Models/ViewModels/LoginViewModel.cs (neu NewPassword, NewPasswordRepeat, KennwortFestlegen)"
  - "IdealAkeWms/Views/Account/Login.cshtml (:75-80 Hinweistext, Felder Erstkennwort)"
  - "IdealAkeWms/Controllers/UsersController.cs (Index :70-94: Hinweis-Banner Benutzer ohne Kennwort)"
  - "IdealAkeWms/Views/Users/Index.cshtml (Banner)"
  - "IdealAkeWms/Program.cs (:55-61 Cookie.SecurePolicy, AddAntiforgery; :259-279 Start-Warnung admin ohne Kennwort)"
  - "IDEALAKEWMSService/Services/MailService.cs (:70-82, je nach Rückfrage 9)"
  - "IDEALAKEWMSService/appsettings.json (:17-20, :25-50 tote Blöcke, je nach Rückfrage 6)"
  - "SQL/00_FreshInstall.sql (:1260-1263 entfernen; :1803/:1838 Guards)"
  - "SQL/151_RepairFreshInstallIndexes.sql (nur nach Rückfrage 4)"
  - "scripts/deploy/Install-IdealAkeWms.ps1 (:82, :269-274, :362-373, :420-449, :480-493)"
  - "scripts/deploy/nachlauf.psd1 (Pruefung/BenutzerOhneKennwort, Info-Einträge, ggf. Pruefung/FreshInstallIndizes, Pruefung/SmtpBenutzer)"
  - "Tests: IdealAkeWms.Tests/Controllers/AccountControllerTests.cs (Login-Fälle neu), IdealAkeWms.Tests/Repositories/FreshInstallIndexSpaltenTests.cs (neu), NachlaufManifestDriftTests"
  - "docs/TESTSZENARIEN.md (TS-139 neu; TS-1.6 :174, TS-40.4 :5250, TS-40.7 :5299 anpassen), docs/betrieb/03, 07, 09, README.md :63, Views/Help/Index.cshtml, Views/Help/Changelog.cshtml, AppVersion.cs (Web + Service)"
  - "Brain (Hauptcheckout): ADR 0027 neu, fallstricke §4, codebase/controller.md (Account), feature-map, changelog"
  - "Bug: [[2026-11-14-sicherheit-anmeldung-und-installer-bug]]"
open_questions:
  - "BH-1 Regel: A streng (Formular nur mit gespeichertem Kennwort; Ausnahme nur der Erstkennwort-Schritt für admin), B nur AD-Benutzer sperren (lokale ohne Kennwort weiter wie heute) oder C = A plus Übergangsschalter? Empfehlung A."
  - "Übergang: Ergebnis der Prüfabfrage P-1 je System (IDEAL-Prod, IDEAL-Test, AKE-Prod, AKE-Test, AKE-Test-IDEAL) – und soll Pruefung/BenutzerOhneKennwort den Prod-Deploy sperren (Phase Vorher, Empfehlung) oder nur hinweisen (Phase Nachher)?"
  - "Lokale QA unter Kestrel (Testbenutzer QA-Agent/QA-Picker ohne Kennwort): Ausnahme nur bei IsDevelopment() mit sichtbarem Hinweis (Empfehlung) oder keine Ausnahme (QA-Testbenutzer bekommen Kennwörter, Ablage außerhalb des Brains)?"
  - "BH-2: Ergebnis P-2 auf IDEAL-Prod (fehlen Indizes aus Abschnitt 15?) – bei Treffern SQL/151 + Pruefung/FreshInstallIndizes (Reparatur, nur IDEAL-Prod) oder Reparatur von Hand? Guards für WarehouseRequisitions/-Items und der Wächter-Test mit in diese Spec? Empfehlung: ja, ja, ja."
  - "BH-3: Parameter -SageLagerbuchungCredential ersatzlos entfernen (Empfehlung) oder behalten und mit Hinweis abbrechen? SMTP-Kennwort: weiter in der Dienst-Datei (ADR 0008/0024) mit Warnhinweis des Installers (Empfehlung) oder eigener ADR für den Umzug nach ServiceSettings (Typ Geheim)?"
  - "BH-4: Installer setzt Sync:ProductionOrdersEnabled nach dem ersten Web-Start in der DB auf false (Variante A, Empfehlung wegen Autostart des Dienstes nach Neustart) oder nur die wirkungslose Zeile streichen (Variante B)? Tote Blöcke Sync (außer Feiertag*), WorkerSettings, ErrorNotification aus IDEALAKEWMSService/appsettings.json entfernen (Empfehlung ja)?"
  - "BH-5a admin ohne Kennwort: Erstkennwort-Schritt bei der ersten Anmeldung (Empfehlung), Installer fragt Kennwort ab und schreibt den Hash (zweite PBKDF2-Implementierung in PowerShell) oder unverändert nur mit Warnung?"
  - "BH-5b Cookie: SecurePolicy SameAsRequest für Sitzungs- und Antiforgery-Cookie (Empfehlung; HTTP-Systeme IDEAL-Test :88 und DEV laufen weiter), Always (bricht HTTP-Systeme) oder unverändert?"
  - "BH-5c SMTP: nur Warnung, wenn ein SMTP-Benutzer ohne SmtpUseSsl gesetzt ist, plus Pruefung/SmtpBenutzer (Empfehlung), oder StartTlsWhenAvailable (Risiko: Relay mit ungültigem Zertifikat -> keine Mails mehr), oder unverändert? Dazu Prüfschritt: MailSettings je Dienst-Datei (Host, Port, SmtpUseSsl, Benutzer gesetzt ja/nein)."
  - "Zusatzfunde (keine Sperre nach Fehlversuchen, keine Mindestlänge, unterschiedliche Meldungen verraten vorhandene Benutzernamen, Installer-Mindestzahl 75 Migrationen BH-7): Folgeaufgabe (Empfehlung) oder in diese Spec?"
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

# Bugfix Sicherheit: Anmeldung, Erstinstallation, Installer (BH-1 bis BH-5)

Bug-Datensatz: [[2026-11-14-sicherheit-anmeldung-und-installer-bug]]. Befunde aus [[2026-11-14-betriebshandbuch-goal-lauf]]
(Tabelle „Befunde beim Erstellen“). Basis Bündel v1.99.23 (`IdealAkeWms/AppVersion.cs:5`), Zielversion **1.99.24**.

## Ziel / Nutzen (das Warum)

- **BH-1:** Heute reicht der Benutzername, um sich als jeder Benutzer ohne gespeichertes Kennwort anzumelden – mit allen seinen
  Rollen, auch `admin`-Rollen eines AD-Benutzers. Das widerspricht der Annahme der Windows-Auth-Specs („kein Passwort-Login für
  AD-User“, `docs/superpowers/specs/2026-07-09-windows-auth-ua-gate-design.md:31`). Ziel: Formular nur mit gespeichertem Kennwort;
  AD-Benutzer ohne Kennwort nur per Windows-SSO. Dazu eine ADR-Entscheidung (0027), die es bisher nicht gibt
  ([[0002-dual-auth-session-login-plus-windows-sso]] regelt nur das Nebeneinander der Wege).
- **BH-2:** Jede Neuinstallation aus `00_FreshInstall.sql` bricht ab; IDEAL-Prod ist so installiert worden
  ([[2026-11-04-ideal-produktiv-installation]] Z. 53-57). Ob dort deshalb weitere Indizes fehlen, ist ungeklärt.
- **BH-3/BH-4:** Der Installer widerspricht ADR 0024 (Klartext) und täuscht einen Schutz vor, der nicht wirkt.
- **BH-5:** drei kleine Lücken, je mit Entscheidung.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** Anmelderegel im Formular (BH-1) mit Übergang, Erstkennwort-Schritt für `admin` (BH-5a), Korrektur
`00_FreshInstall.sql` mit Nachweis auf leerer DB (BH-2), Installer ohne Klartext-SData und mit wirksamem FA-Schutz (BH-3/BH-4),
Cookie-Flag (BH-5b), SMTP-Hinweis (BH-5c), Manifest, Doku `docs/betrieb/03`, `07`, `09`, Hilfe, Changelog, Testszenarien.

**Out-of-Scope:** Windows-SSO-Logik (`WindowsAutoLoginMiddleware`) bleibt unverändert; keine Sperre nach Fehlversuchen, keine
Kennwort-Richtlinie, kein „Kennwort vergessen“ (Zusatzfunde, Rückfrage 10); kein Umzug des SMTP-Kennworts (ADR 0024 letzter Absatz
verlangt dafür einen eigenen ADR); `SQL/00_FreshInstall.sql`-Kopf mit festem DB-Namen (`:8-17`) bleibt; BH-6/BH-7 (Doku-Drift,
fehlende `<FIRMA>-Prod-*.sql`) nicht.

## Fachliche Anforderungen

### Ist-Stand (belegt)

**Wer setzt eine Sitzung?** Genau zwei Stellen: Formular-Login `AccountController.cs:104-110` und SSO
`WindowsAutoLoginMiddleware.cs:114-118` (Suche nach `SetInt32(` im Web-Projekt, sonst nur diese beiden). API-Controller haben keinen
eigenen Anmeldeweg: `/api/*` ist von der Login-Umleitung ausgenommen (`LoginRedirectMiddleware.cs:23`), geschützt wird über die
Filter mit der Sitzung; im Controller-Code gibt es weder API-Schlüssel noch Bearer/Basic-Prüfung (Suche `apikey|bearer|basic`:
nur der ausgehende Sage-Verbindungstest `ServiceSettingsController.cs:202-203`).

**Formular-Login heute** (`AccountController.cs:74-101`): Benutzer per Name, aktiv (`:75`). Hash vorhanden → Kennwort prüfen
(`:83-91`). Kein Hash → angemeldet, sofern das Feld **leer** ist (`:92-101`). Die Seite sagt dazu „Leer lassen, wenn kein Passwort
vergeben.“ (`Login.cshtml:79`). `Password` ist nicht `[Required]` (`LoginViewModel.cs:11-13`).

**Wo entsteht ein Benutzer, wo ein Hash** (alle Schreibwege; `new User` und `PasswordHash =` im Web- und Dienst-Code):

| Weg | Hash | Beleg |
|---|---|---|
| Start-Seed `admin` | Hash des **leeren** Kennworts; ist der Hash `NULL`, wird bei **jedem** Start wieder der Leer-Hash gesetzt | `Program.cs:260-279` (`:267`, `:275-278`) |
| FreshInstall `admin` | `NULL`, danach Start-Seed | `SQL/00_FreshInstall.sql:1541-1549` |
| `/Users/Create` (lokal) | nur wenn das Feld nicht leer ist | `UsersController.cs:136-137` |
| `/Users/CreateAdUser` | **nie** | `UsersController.cs:200-209` |
| `/Users/Edit` | setzt neu, wenn nicht leer; **kein** Weg leert den Hash | `UsersController.cs:307-308` |
| `/Account/Profile` | setzt neu, wenn nicht leer; leert nie | `AccountController.cs:222-223` |
| Dienst / Sync | legt keine `User` an | Suche `new User` in `IDEALAKEWMSService` ohne Treffer |
| „Kennwort zurücksetzen“ | gibt es nicht | Suche `ResetPassword|ForgotPassword` ohne Treffer |

Folgerung (belegt): Ein Hash, der das **leere** Kennwort bestätigt, entsteht nur über den `admin`-Seed (`Program.cs:267`, `:277`),
denn alle anderen `HashPassword`-Aufrufe sind durch `!string.IsNullOrEmpty(newPassword)` geschützt (`UsersController.cs:136`, `:307`,
`AccountController.cs:222`).

**Wer meldet sich heute wie an** (Belege; Mengen nur per P-1 erhebbar):
- Domänen-PC: SSO, wenn `WindowsAuthAktiv = true` (`WindowsAutoLoginMiddleware.cs:93-94`) und ein aktiver Benutzer mit passendem
  `WindowsUserName` existiert (`:110-112`).
- Handy/Tablet/Nicht-Domäne: Formular (`UserAgentHelper`-Gate, TS-40.7) – oder der Knopf „Mit Windows anmelden“ (ForceSso, TS-40.9,
  TS-40.12 nennt ihn ausdrücklich für Mobilgeräte). Ohne `WindowsAuthAktiv` gibt es den Knopf nicht (`Login.cshtml:85-94`).
- **BDE-Terminal:** ein WMS-Benutzer ist am Gerät angemeldet; das Terminal wird über `BdeTerminal.UserId` dieses Benutzers gefunden
  (`BdeTerminal.cs:7-11`, `BdeTerminalController.cs:43-51`); die Werker melden sich danach per Ausweis-Scan als BDE-Operator an
  (TS-8, `fallstricke.md:439`). Ob die Terminal-Benutzer ein Kennwort haben: **unbekannt → P-1** (Spalte `IstTerminal`).
- **Kommissionierer am Handgerät:** Formular mit eigenem Benutzer; ob mit Kennwort: **unbekannt → P-1** (Spalte `IsPicker`).
- **Belegte Konten ohne Kennwort:** AKE-DEV `lager` (Formular-Login „ohne Passwort“, [[2026-11-10-vorwaerts-merge-main-ins-buendel]]
  Z. 107); in der QA-DB `AKE_WMS_QA_MAIN` `QA-Agent` (admin), `QA-Picker`, `QA-OhnePicking`, weil unter Kestrel kein SSO geht
  ([[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-spec]] Z. 693, [[2026-11-08-kommissionierfotos-ohne-anmeldung-abrufbar-spec]] Z. 300).
- `admin` hat nach jeder Neuinstallation das leere Kennwort (Installer-Text `Install-IdealAkeWms.ps1:29`, `:481`).

### Soll (Empfehlung Variante A, Rückfrage 1)

**Regel R (ADR 0027):** Das Anmeldeformular meldet nur an, wenn das eingegebene Kennwort **nicht leer** ist und den gespeicherten Hash
bestätigt. Benutzer ohne Hash (lokal oder AD) werden im Formular abgewiesen:
„Für diesen Benutzer ist kein Kennwort hinterlegt. Bitte „Mit Windows anmelden“ verwenden oder beim Administrator ein Kennwort setzen
lassen.“ (Ist `WindowsAuthAktiv` aus, entfällt der erste Halbsatz.) Es entsteht keine Sitzung.

**Ausnahme E (Erstkennwort, BH-5a, Rückfrage 7):** Bestätigt der gespeicherte Hash das **leere** Kennwort (laut Ist-Stand nur der
`admin`-Seed) und wurde das Feld leer gelassen, zeigt die Anmeldeseite statt einer Sitzung den Schritt „Erstes Kennwort festlegen“
(neues Kennwort + Wiederholung). Der Server prüft beim Speichern erneut, dass der Hash noch das leere Kennwort bestätigt, dass das
neue Kennwort nicht leer ist und beide Eingaben gleich sind; erst dann Hash setzen, Audit-Felder, Sitzung. Danach gilt für `admin`
Regel R. Der Rückweg „admin-Kennwort vergessen“ bleibt der heutige: `PasswordHash` per SQL auf `NULL`, App-Pool neu starten →
Seed setzt den Leer-Hash (`Program.cs:275-278`) → Erstkennwort-Schritt (Doku `07-troubleshooting`).

**Übergang:** Vor dem Deploy erhebt Gerald je System P-1. Jeder aktive **lokale** Benutzer ohne Kennwort, der weiter arbeiten soll
(Terminals, Handgeräte), bekommt vorher über `/Users/Edit` ein Kennwort (geht schon heute, `UsersController.cs:307-308`) oder wird
deaktiviert. AD-Benutzer ohne Kennwort brauchen nur dann eines, wenn sie ohne SSO arbeiten (Mobilgerät ohne Knopf, oder
`WindowsAuthAktiv` aus). Die Prüfung `Pruefung/BenutzerOhneKennwort` zählt genau die Benutzer, die nach dem Deploy **keinen**
Anmeldeweg mehr hätten, und sperrt den Prod-Deploy, solange sie > 0 ist (Rückfrage 2). Kein Schalter (Begründung unter „Einstellungen“).

**Sichtbarkeit (Melden statt still):** `/Users` zeigt einen Hinweis „n aktive Benutzer ohne Kennwort: … (lokal: keine Anmeldung
mehr möglich; AD: nur Windows-Anmeldung)“ und „admin hat noch kein Kennwort (Erstkennwort offen)“. Der Web-Start schreibt eine
Warnung ins Log, solange der `admin`-Hash das leere Kennwort bestätigt.

**Lokale QA unter Kestrel (Rückfrage 3, Empfehlung):** Nur wenn `app.Environment.IsDevelopment()` gilt Regel R nicht für Benutzer
ohne Hash; die Anmeldeseite zeigt dann sichtbar „Entwicklungsmodus: Anmeldung ohne Kennwort erlaubt“. Belegt nur über
`launchSettings.json:9-10`/`:18-19` (`Development`); jede IIS-Site setzt `Production` per `web.config:17-19`, DEV-IIS-Sites `DEV`
([[2026-10-29-dev-umgebung-lokal]] Z. 57/161) – die Ausnahme greift dort nicht.

### Varianten BH-1 (Rückfrage 1)

| | A – streng (Empfehlung) | B – nur AD | C – A + Schalter |
|---|---|---|---|
| Regel | Formular nur mit Kennwort | AD-Benutzer ohne Hash abgewiesen, lokale ohne Hash wie heute | wie A, AppSetting `AnmeldungOhneKennwort` (`gesperrt`/`nur lokale Benutzer`) |
| Lücke danach | keine (bis auf Erstkennwort-Fenster `admin`) | bleibt für jeden lokalen Benutzer ohne Kennwort (Namen wie `lager` sind erratbar) | wie A, solange der Schalter auf `gesperrt` steht |
| Übergang | Kennwörter vorher setzen, Prüfung sperrt Prod | keiner | Schalter erst später umlegen |
| Kosten | klein | klein | Schalter nach ADR 0020 (Katalog, Wirkung, Warnung), zweiter Codeweg, Drift-Guard |

Empfehlung **A**: einfachste Regel, ein Codeweg; der Übergang wird durch die Prüfung erzwungen statt durch einen Schalter, den man
vergisst zurückzustellen. B lässt genau die Lücke offen, die BH-1 meldet. C lohnt nur, wenn P-1 viele lokale Konten zeigt, die nicht
vor dem Deploy ein Kennwort bekommen können.

### BH-2 – Erstinstallation

- `00_FreshInstall.sql:1260-1263` legt `IX_ProductionOrders_IsReleasedForPicking_IsDone` und `IX_ProductionOrders_AssignedPickerId`
  auf `ProductionOrders` an. Die Tabelle hat diese Spalten nicht (`:388-418`; auch `PickingPriority` aus dem INCLUDE nicht). Sie
  liegen in `ProductionOrderPickingStatus` (`:428-448`), mit eigenen Indizes `:456-460`, gleichnamig wie im EF-Modell
  (`ApplicationDbContext.cs:521`, `:524`). → **Zeilen 1260-1263 ersatzlos löschen.**
- **Bestehende DBs** können die falschen Indizes nicht haben: ohne Spalte kein Index; upgegradete DBs haben sie in SQL/60 gelöscht
  (`SQL/60_ProductionOrderSplit.sql:423-441`). P-2a belegt das je System. **Offen ist die Umkehrung:** Bei IDEAL-Prod brach
  Abschnitt 15 (ein Batch `:1206-1278`) ab; ob SQL Server dabei nur die eine Anweisung oder den ganzen Batch verwarf, ist nicht
  belegt – dann fehlten dort bis zu ~30 Indizes, darunter die eindeutigen `IX_ProductionOrders_SubOrderNumber` (`:1250-1251`) und
  `IX_SageBookingQueueItems_StockMovementId` (`:1224-1225`) sowie der FK `FK_StockMovements_ProductionOrders_ProductionOrderId`
  (`:1219-1221`). **Unverifiziert → P-2b auf IDEAL-Prod.**
- QA-Befund 2026-11-12 ([[2026-11-12-freshinstall-leere-db-fehler]]): zusätzlich Meldung 2714 für `WarehouseRequisitions`/
  `WarehouseRequisitionItems`. Belegt: beide `CREATE TABLE` und ihre Indizes stehen **ohne Guard** (`:1803-1836`, `:1838-1869`),
  2714 entsteht also bei einem zweiten Lauf gegen dieselbe DB. Da das Skript per `USE [IDEAL_AKE_WMS]` (`:16`) die per `sqlcmd -d`
  gewählte DB verlässt, lief der QA-Lauf vermutlich gegen eine schon vorhandene `IDEAL_AKE_WMS` auf `.` – unverifiziert; ebenso die
  dortige Meldung „`__EFMigrationsHistory` fehlt“ (die Tabelle entsteht in `:3011-3019`; vermutlich in der falschen DB gesucht).
  → Guards `IF OBJECT_ID(N'dbo.WarehouseRequisitions', N'U') IS NULL` bzw. `…Items` um beide Blöcke (Rückfrage 4).
- **Wächter-Test (billig, Rückfrage 4):** `FreshInstallIndexSpaltenTests` liest `00_FreshInstall.sql`, findet jede
  `CREATE [UNIQUE] [NONCLUSTERED] INDEX [x] ON [dbo].[T](…) [INCLUDE (…)] [WHERE …]`-Anweisung und prüft jede genannte Spalte gegen
  das relationale EF-Modell (Kontext mit `UseSqlServer(<Dummy-Verbindung>)`, nur Modell, keine Verbindung). Tabellen ohne EF-Abbild
  stehen mit Grund in einer Ausnahmeliste. Muster wie `DeployScriptRunsMigrationSqlTests` (Datei lesen, kein SQL Server).
  `ponytail:` prüft nur Index-Spalten, nicht die Spaltenlisten der `CREATE TABLE`; Ausbau, wenn dort Drift auffällt. Findet der Test
  weitere Abweichungen, werden sie gemeldet (Befundliste in der Spec) und nur korrigiert, wenn die Korrektur eindeutig ist.

### BH-3 – Installer ohne Klartext-SData

- Heute: Parameter `-SageLagerbuchungCredential` (`Install-IdealAkeWms.ps1:82`) schreibt Benutzer **und Kennwort** in
  `appsettings.Production.json` (`:269-274`, `:277`) und in die Dienst-`appsettings.json` (`:366-369`, `:372`). Seit ADR 0024 liest der
  Dienst die DB-Zugangsdaten zuerst; der Datei-Weg ist nur noch Übergang mit Warnung je Lauf
  ([[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]] „Reihenfolge“).
- Soll (Empfehlung, Rückfrage 5): Parameter und beide Schreibblöcke **entfernen**; die Abschlussliste (`:480-493`) bekommt
  „SData-Zugang unter /ServiceSettings › Sage-Lagerbuchung eintragen (verschlüsselt gespeichert)“. Ein Aufruf mit dem alten Parameter
  scheitert laut an PowerShell („Parameter nicht gefunden“) – gewollt. Die Kommentarzeile `:82` und die `.EXAMPLE` (`:32-39`) nachziehen.
- **SMTP-Kennwort** liegt heute nur in der Dienst-Datei (`MailService.cs:72-73` liest `MailSettings:SmtpUsername/SmtpPassword` aus der
  Konfiguration; Web sendet keine Mails – Suche `SmtpClient` nur im Dienst). ADR 0024 hält SMTP ausdrücklich bei ADR 0008
  (appsettings-only). Ein Umzug bräuchte einen eigenen ADR. Empfehlung: Speicherort unverändert, der Installer schreibt es weiter
  (`:362-365`), gibt aber eine Warnung aus: „SMTP-Kennwort steht im Klartext in <Datei>; anonymes internes Relay bevorzugen,
  Dateirechte eng halten.“

### BH-4 – FA-Schutz bei der Installation

- Belegt wirkungslos: der Dienst liest `Sync:ProductionOrdersEnabled` nur aus `dbo.ServiceSettings`
  (`SyncWorker.cs:43` → `ServiceSettings.GetBoolSafeAsync` `Common/ServiceSettings.cs:50-55` → `GetValueAsync` `:19-29`, SQL auf die
  Tabelle; fehlt die Zeile oder ist die DB nicht erreichbar, gilt der Code-Default `true`, **nicht** die Datei). Die Zeile existiert
  nach der Installation immer: FreshInstall-Seed `true` (`00_FreshInstall.sql:1112`) bzw. Katalog-Seed beim Web-Start
  (`Program.cs:314-327`, Default `ServiceSettingDefinitions.cs:199`).
- Dasselbe gilt für die übrigen Blöcke der Dienst-Datei: `WorkerSettings:*` (`SyncWorker.cs:31-32`, `NotificationWorker.cs:25`,
  `CleanupWorker.cs:35`) und `ErrorNotification:*` (`SyncErrorNotifier.cs:32-33` u. a.) werden nur über `ServiceSettings.*` gelesen;
  eine direkte Konfigurationslesung `configuration["Sync:…"]`/`["WorkerSettings:…"]`/`["ErrorNotification:…"]` gibt es im Dienst nicht
  (Suche ohne Treffer). **Einzige Ausnahme:** `Sync:FeiertagSyncEnabled/-CountryCode/-Region/-JahreVoraus` werden per
  `Configure<HolidaySyncOptions>(GetSection("Sync"))` gebunden (`IDEALAKEWMSService/Program.cs:110`, `HolidaySyncService.cs:12-27`) und
  dienen als Rückfall, wenn die DB-Zeile fehlt (`HolidaySyncService.cs:60-80`).
- Lücke hinter dem gemeinten Schutz: Der Dienst wird mit Starttyp **Automatisch** registriert (`Install-IdealAkeWms.ps1:382`, `:405`);
  nach einem Server-Neustart vor Teil 2 läuft der FA-Abgleich mit `true`.
- Soll (Empfehlung Variante A, Rückfrage 6): Zeile `:371` und den Text in `:373` streichen; in Schritt 6 nach erfolgreichem Aufwärmen
  (`$script:WebGestartet`, `:442`/`:446`) per `System.Data.SqlClient` (Muster `Get-SchemaStand` `:421-429`)
  `UPDATE dbo.ServiceSettings SET [Value] = N'false' WHERE [Key] = N'Sync:ProductionOrdersEnabled'` ausführen und mit „OK FA-Abgleich
  aus (Sync:ProductionOrdersEnabled = false) – nach Teil 2 unter /ServiceSettings einschalten“ melden; startet das Web nicht, Warnung
  mit genau dieser Anweisung. Abschlussliste Punkt 3 ergänzen. Variante B: nur streichen und dokumentieren.
- Tote Blöcke (Empfehlung): aus `IDEALAKEWMSService/appsettings.json` `ErrorNotification` (`:17-20`), `WorkerSettings` (`:46-50`) und
  im Block `Sync` (`:25-45`) alles außer den vier `Feiertag*`-Schlüsseln entfernen. Bestehende Server-Dateien bleiben unberührt (harmlos).

### BH-5 – drei Entscheidungen

| Punkt | Ist (Beleg) | Empfehlung | Folgen |
|---|---|---|---|
| a) `admin` ohne Kennwort | Seed mit Leer-Hash `Program.cs:260-279`; Installer-Text `:29`, `:481` | Erstkennwort-Schritt (Ausnahme E) + Start-Warnung + Hinweis `/Users` | Fenster bleibt bis zur ersten Anmeldung (wie heute, nur kürzer); Installer-Text: „beim ersten Anmelden als admin ein Kennwort festlegen“. Alternative Installer-Abfrage braucht eine zweite PBKDF2-Implementierung (`PasswordService.cs:8-22`) in PowerShell – Drift-Risiko, nicht testbar. |
| b) Cookie ohne `Secure` | `Program.cs:55-61` setzt nur `HttpOnly`/`IsEssential`/`Name`; Antiforgery nicht konfiguriert | `options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest` für Sitzung und `AddAntiforgery(o => o.Cookie.SecurePolicy = …SameAsRequest)` | HTTPS-Systeme (IDEAL-Prod, AKE-Prod/Test/Test-IDEAL, `ziele.psd1:9`, `:11-13`) senden das Cookie nur noch verschlüsselt; HTTP-Systeme (IDEAL-Test :88 `:10`, DEV `:14-15`) laufen unverändert. `Always` würde dort die Anmeldung brechen. Den heutigen Default (laut ASP.NET-Core-Doku `None`) belegt der Dev-Lauf in den Browser-Entwicklerwerkzeugen. |
| c) SMTP ohne TLS | `SmtpUseSsl` false → `SecureSocketOptions.None` (`MailService.cs:71`, `:78`): auch ein angebotenes STARTTLS wird nicht genutzt; ein gesetzter Benutzer meldet sich unverschlüsselt an (`:81-82`) | Warnung im Dienst-Log beim Senden, wenn `SmtpUsername` gesetzt und `SmtpUseSsl` false, plus `Pruefung/SmtpBenutzer` im Update-Skript | kein Verhaltenswechsel. Alternative `StartTlsWhenAvailable` verschlüsselt opportunistisch, aber ein Relay mit ungültigem/selbstsigniertem Zertifikat lässt dann jeden Versand scheitern (Fehlermails, Lieferverzug, Meldebestand). Wie die Relays je Standort konfiguriert sind, steht nur in den Server-Dateien → Prüfschritt (Rückfrage 9). |

## Technischer Loesungsentwurf

Kein neues Muster, keine neue Abhängigkeit, kein neuer Filter, keine Migration.

1. **`AccountController.Login` POST** (`:64-116`) – die Kennwortprüfung `:82-101` wird ersetzt:
   - `user.PasswordHash` leer → wenn `IsDevelopment()` (Rückfrage 3) wie heute anmelden, sonst Meldung R, View, keine Sitzung.
   - Feld leer → wenn `VerifyPassword(hash, "")` (Ausnahme E): ist `vm.NewPassword` leer → View mit `KennwortFestlegen = true`
     und Hinweis; sonst prüfen (nicht leer, gleich `NewPasswordRepeat`, Hash bestätigt am Schreibort weiterhin das leere Kennwort –
     ADR 0026 „Server prüft am Schreibort“), dann `PasswordHash = HashPassword(neu)`, `ModifiedAt/By/ByWindows`, `UpdateAsync`,
     Sitzung wie `:104-110`, `TempData["SuccessMessage"] = "Kennwort festgelegt."`. Sonst „Falsches Passwort.“
   - Feld nicht leer → `VerifyPassword` wie heute.
   - **Kein** neuer Pfad: der Schritt postet an dieselbe Action `/Account/Login`; die LoginRedirect-Ausnahmeliste
     (`LoginRedirectMiddleware.cs:20-28`, [[fallstricke]] „LoginRedirect zählt Account-Pfade einzeln auf“) bleibt unverändert.
     `NormalizeUserForSession` greift auf `/account/*` ohnehin (`WindowsAutoLoginMiddleware.cs:68`).
   - Die Meldungen bleiben im bestehenden `vm.ErrorMessage` (`Login.cshtml:56-61`), kein TempData-`ErrorMessage`.
2. **`LoginViewModel`**: `NewPassword`, `NewPasswordRepeat` (`DataType.Password`), `KennwortFestlegen` (bool).
   **`Login.cshtml`**: Hinweis `:79` ersetzen durch „Kein Kennwort? Mit Windows anmelden oder beim Administrator melden.“; bei
   `KennwortFestlegen` die zwei Felder mit `autocomplete="new-password"` und Überschrift „Erstes Kennwort für admin festlegen“;
   im Entwicklungsmodus der Hinweis (Rückfrage 3). Skill `frontend-design` ist Pflicht (Bootstrap-Bestand, Kontrast AA).
3. **`UsersController.Index`** (`:70-94`): aus der schon geladenen Liste `users` (`:77`) die aktiven ohne Hash bzw. `admin` mit
   Leer-Hash ermitteln (`VerifyPassword` einmal je Leer-Kandidat – nur `admin`) → `ViewBag`, Banner in `Views/Users/Index.cshtml`
   (Bootstrap `alert-warning`, Text nicht nur Farbe). Keine neue Spalte, daher keine Listen-View-Pflichten.
4. **`Program.cs`**: nach dem Seed (`:259-279`) `if (passwordService.VerifyPassword(adminUser.PasswordHash!, ""))
   app.Logger.LogWarning("Benutzer admin hat noch kein Kennwort – bei der ersten Anmeldung festlegen.")`; Cookie-Flags (BH-5b).
5. **`MailService`** (BH-5c Empfehlung): vor `ConnectAsync` (`:79`) `LogWarning`, wenn `username` gesetzt und `!useSsl`.
6. **`00_FreshInstall.sql`**: `:1260-1263` löschen; Guards `:1803`/`:1838`. Nachweis siehe AK 9-11.
7. **`Install-IdealAkeWms.ps1`**: BH-3/BH-4 wie oben; Text `:29`, `:481` auf den Erstkennwort-Schritt umstellen.
8. **Manifest** `nachlauf.psd1`: Einträge unter „Deploy“.
9. **ADR 0027** (Dev-Lauf schreibt die Datei im Hauptcheckout, `secondbrain/architektur/adr/`): Entwurf

   > **0027 – Formular-Anmeldung nur mit gespeichertem Kennwort; AD-Benutzer ohne Kennwort nur per Windows.**
   > Kontext: BH-1. Optionen A/B/C wie oben. Entscheidung: Regel R, Ausnahme E (Erstkennwort `admin`), Entwicklungs-Ausnahme
   > (falls Rückfrage 3 so entschieden), kein Schalter, Übergang über `Pruefung/BenutzerOhneKennwort`. Konsequenzen: Terminals und
   > Handgeräte brauchen Kennwörter; QA unter Kestrel; Rückweg admin per SQL `NULL` + Neustart; ergänzt (nicht ersetzt) ADR 0002.

## Migrations-/SQL-Auswirkungen

- **Keine EF-Migration**, kein Modellwechsel, keine `MigrationId`, keine AgentJobs betroffen (nur Index-/Guard-Zeilen im Konsolidat).
- `SQL/00_FreshInstall.sql`: Stelle (a) Schema-Objekte korrigiert (`:1260-1263` weg, zwei Guards); Stelle (b) `MigrationId`-Block
  unverändert. ADR 0004 bleibt erfüllt (Konsolidat = Modell).
- **SQL/151 nur nach Rückfrage 4** (P-2b zeigt fehlende Indizes auf IDEAL-Prod): `SQL/151_RepairFreshInstallIndexes.sql` = der
  korrigierte Abschnitt 15 (`:1209-1277` ohne die zwei Zeilen), jede Anweisung mit `IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE
  name = …)` bzw. `OBJECT_ID(…,'F') IS NULL`, eigener Batch, keine `ROLLBACK`, keine sqlcmd-Direktiven, kein History-Eintrag (keine
  Migration). Läuft nur auf IDEAL-Prod (FreshInstall-Herkunft, Namen identisch); auf upgegradeten DBs können gleichwertige Indizes
  unter EF-Namen existieren – dort **nicht** laufen lassen (Duplikate).
- Prüfabfragen (lesend, Gerald in SSMS auf der WMS-DB des jeweiligen Systems; **nicht** auf Sage):

```sql
-- P-1 Benutzer ohne Kennwort (je System: IDEAL-Prod, IDEAL-Test, AKE-Prod, AKE-Test, AKE-Test-IDEAL)
SELECT [Key], [Value] FROM dbo.AppSettings WHERE [Key] IN (N'WindowsAuthAktiv', N'WindowsAuthBerechtigungsgruppe');

SELECT CASE WHEN u.WindowsUserName IS NULL THEN N'Lokal' ELSE N'AD' END AS Typ,
       u.Id, u.Name, u.WindowsUserName, u.IsPicker,
       CASE WHEN EXISTS (SELECT 1 FROM dbo.BdeTerminals t WHERE t.UserId = u.Id) THEN 1 ELSE 0 END AS IstTerminal,
       STUFF((SELECT N', ' + r.[Key] FROM dbo.UserRoles ur JOIN dbo.Roles r ON r.Id = ur.RoleId
              WHERE ur.UserId = u.Id ORDER BY r.SortOrder FOR XML PATH('')), 1, 2, N'') AS Rollen
FROM dbo.Users u
WHERE u.IsActive = 1 AND (u.PasswordHash IS NULL OR u.PasswordHash = N'')
ORDER BY Typ, u.Name;

SELECT CASE WHEN WindowsUserName IS NULL THEN N'Lokal' ELSE N'AD' END AS Typ, COUNT(*) AS AktivOhneKennwort
FROM dbo.Users WHERE IsActive = 1 AND (PasswordHash IS NULL OR PasswordHash = N'')
GROUP BY CASE WHEN WindowsUserName IS NULL THEN N'Lokal' ELSE N'AD' END;
-- Ob admin noch das leere Kennwort hat, zeigt SQL nicht (Hash mit Salz, PasswordService.cs:10-21):
-- Prüfschritt im Browser des alten Stands: admin + leeres Kennwort -> wird angemeldet = sofort Kennwort setzen.

-- P-2a falsche Indizes/Spalten (erwartet: NULL, NULL und 0 Zeilen)
SELECT COL_LENGTH(N'dbo.ProductionOrders', N'AssignedPickerId') AS Spalte1, COL_LENGTH(N'dbo.ProductionOrders', N'IsReleasedForPicking') AS Spalte2;
SELECT name FROM sys.indexes WHERE name IN (N'IX_ProductionOrders_IsReleasedForPicking_IsDone', N'IX_ProductionOrders_AssignedPickerId');

-- P-2b fehlen Indizes aus Abschnitt 15? (IDEAL-Prod; Liste = 00_FreshInstall.sql:1209-1277 ohne :1260-1263)
SELECT v.n AS Fehlt FROM (VALUES
 (N'IX_StockMovements_ArticleId'),(N'IX_StockMovements_StorageLocationId'),(N'IX_StockMovements_Timestamp'),
 (N'IX_StockMovements_SourceStorageLocationId'),(N'IX_StockMovements_ProductionOrderId'),(N'IX_SageBookingQueueItems_Status'),
 (N'IX_SageBookingQueueItems_StockMovementId'),(N'IX_StorageLocations_IsActive'),(N'IX_StorageLocations_Source'),
 (N'IX_StorageLocations_IstBuchbar'),(N'IX_StorageLocations_StorageAreaId'),(N'IX_StorageAreas_Lagerkennung'),
 (N'UQ_ArticleSageDetails_ArticleId'),(N'IX_SyncLogs_Timestamp_Desc'),(N'IX_SyncLogs_Service_Level'),
 (N'IX_WorkstationUsers_WorkstationId'),(N'IX_WorkstationUsers_UserId'),(N'IX_ProductionOrders_OrderNumber'),
 (N'IX_ProductionOrders_SubOrderNumber'),(N'IX_ProductionOrders_ArticleNumber'),(N'IX_ProductionOrders_IsDone'),
 (N'IX_ProductionOrders_IsCancelled'),(N'IX_ProductionOrders_ProductionWorkplaceId'),(N'IX_PickingItems_ProductionOrderId'),
 (N'IX_PickingItems_POId_IsPicked'),(N'IX_ProductionWorkplaceUsers_ProductionWorkplaceId_UserId'),
 (N'IX_ProductionWorkplaceUsers_UserId'),(N'IX_WorkOperations_ProductionOrderId'),(N'IX_WorkOperations_ProductionOrderId_Sequence'),
 (N'IX_WorkOperations_ProductionWorkplaceId')) v(n)
WHERE NOT EXISTS (SELECT 1 FROM sys.indexes i WHERE i.name = v.n)
UNION ALL
SELECT N'FK_StockMovements_ProductionOrders_ProductionOrderId' WHERE OBJECT_ID(N'dbo.FK_StockMovements_ProductionOrders_ProductionOrderId', N'F') IS NULL;
```

Der Dev-Lauf gleicht die P-2b-Liste gegen den Abschnitt 15 des korrigierten Skripts ab (gleiche Namen, keine vergessen).

## Einstellungen

Kein neuer Schlüssel. Bewusst **kein** Schalter für die Anmelderegel (Variante C verworfen, Begründung unter „Varianten BH-1“).
`MailSettings:*` bleiben appsettings-only (ADR 0008/0024), unverändert in Name und Bedeutung.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `Sync:ProductionOrdersEnabled` (bestehend, nur **Installationswert** geändert, Variante A Rückfrage 6) | Bool (Schalter) | `true` / `false` | Katalog unverändert `true` (`ServiceSettingDefinitions.cs:199`); **Neuinstallation per Skript: `false`** | aus = der Dienst holt keine Fertigungsaufträge aus Sage | Dienst `SyncWorker.cs:43`, Pflege `/ServiceSettings` | beide | „Einschalten startet den FA-Abgleich aus Sage beim nächsten Zyklus – erst nach Teil 2 / Standort-Konfiguration.“ (nur Installer-Ausgabe; Katalogtext unverändert) |

## Audit-Feld-Auswirkungen

- Erstkennwort-Schritt (Ausnahme E): `ModifiedAt = DateTime.Now` (Lokalzeit, ADR 0025), `ModifiedBy` = Name des Benutzers (`admin`),
  `ModifiedByWindows = _currentUserService.GetWindowsUserName()` – beim Formular-Login `"SYSTEM"` (bewusst akzeptiert, [[fallstricke]]
  §4 „Antiforgery-Wurzel-Fix“). Gleiches Muster wie `AccountController.cs:225-227`.
- Kennwort setzen über `/Users/Edit` unverändert (`UsersController.cs:310-312`).
- Installer-`UPDATE` auf `ServiceSettings` (BH-4 A): Tabelle ohne Audit-Spalten (`00_FreshInstall.sql:1097-1102`, kein `AuditableEntity`,
  vgl. [[fallstricke]] „AppSettings ist kein AuditableEntity“) – die Installer-Ausgabe und das Transcript (`Install-…ps1:104`) belegen es.
- Rückweg „admin-Kennwort per SQL `NULL`“: Hand-Schritt ohne Audit; Doku nennt das.

## Akzeptanzkriterien

1. Lokaler aktiver Benutzer ohne Kennwort: Formular mit leerem Feld → Meldung R, keine Sitzung (Startseite leitet wieder auf
   `/Account/Login`). Mit beliebigem Text im Feld → dieselbe Meldung.
2. AD-Benutzer ohne Kennwort: Formular → Meldung R (mit Hinweis „Mit Windows anmelden“, wenn `WindowsAuthAktiv`); SSO am Domänen-PC
   meldet ihn weiter an (TS-40.1, Manual-UAT).
3. Benutzer mit Kennwort: richtiges Kennwort → angemeldet; falsches → „Falsches Passwort.“; leeres Feld → „Falsches Passwort.“ (kein
   Erstkennwort-Schritt, weil der Hash das leere Kennwort nicht bestätigt).
4. `admin` mit Leer-Hash, leeres Feld → Schritt „Erstes Kennwort festlegen“, keine Sitzung. Leeres neues Kennwort oder abweichende
   Wiederholung → Meldung, keine Sitzung, Hash unverändert. Gültige Eingabe → angemeldet, `ModifiedAt/By/ByWindows` gesetzt; danach
   `admin` mit leerem Feld → „Falsches Passwort.“
5. Ein manipulierter POST mit `NewPassword` für einen Benutzer, dessen Hash das leere Kennwort **nicht** bestätigt (oder ohne Hash),
   ändert nichts (Hash unverändert, keine Sitzung).
6. `IsDevelopment()` (nur `dotnet run`): Benutzer ohne Hash meldet sich mit leerem Feld an, Hinweis „Entwicklungsmodus …“ ist auf der
   Anmeldeseite sichtbar – nur falls Rückfrage 3 so entschieden; unter IIS (`Production`/`DEV`) gilt AK 1.
7. `/Users` zeigt für Admins den Hinweis mit Anzahl und Namen der aktiven Benutzer ohne Kennwort, getrennt lokal/AD, und
   „admin hat noch kein Kennwort“, solange der Leer-Hash besteht; ohne Treffer kein Hinweis.
8. Web-Start mit Leer-Hash bei `admin` schreibt genau eine Warnung ins Log.
9. `00_FreshInstall.sql` (Kopie mit ersetztem Kopf `:7-17` → `USE [WMS_FreshInstall_Pruefung]`) läuft mit
   `sqlcmd -S . -E -C -b -I -f 65001` auf einer neu angelegten leeren DB der **lokalen** Instanz `.` mit Exit-Code 0 und ohne
   „Msg“/„Meldung“ durch; ein zweiter Lauf gegen dieselbe DB ebenso. Vorher `SELECT @@SERVERNAME` ins Protokoll – nie gegen
   `AKESQL20` oder einen IDEAL-Server. Danach `DROP DATABASE [WMS_FreshInstall_Pruefung]`. Die Kopie liegt im Scratch-Ordner, nicht im Repo.
10. In der Prüf-DB aus AK 9: alle Namen aus P-2b vorhanden; `SELECT COUNT(*) FROM dbo.__EFMigrationsHistory` = Anzahl der Migrationen
    in `IdealAkeWms/Migrations` (ohne `.Designer.cs` und Snapshot); ein Web-Start (Kestrel, `ConnectionStrings__DefaultConnection`
    auf die Prüf-DB) läuft ohne Migrationsfehler und legt `admin` mit Leer-Hash an (Erstkennwort-Schritt erreichbar). Erst danach AK 9 `DROP`.
11. `FreshInstallIndexSpaltenTests` grün; die Ausnahmeliste nennt je Tabelle den Grund. Mit den alten Zeilen `:1260-1263` schlägt der
    Test fehl (im Dev-Lauf einmal belegt).
12. Installer: kein Parameter `SageLagerbuchungCredential`, kein Abschnitt `SageLagerbuchung` mit Kennwort in den geschriebenen Dateien;
    Warnung bei `-SmtpCredential`; nach erfolgreichem Aufwärmen steht `Sync:ProductionOrdersEnabled = false` in der DB (Variante A),
    sonst eine Warnung mit der Anweisung. PowerShell-Parser ohne Fehler
    (`[System.Management.Automation.Language.Parser]::ParseFile(…)`); voller Lauf = Manual-UAT bei der nächsten Neuinstallation.
13. `IDEALAKEWMSService/appsettings.json` enthält (falls Rückfrage 6 so) nur noch die gelesenen Abschnitte; Dienst-Tests grün; der
    Feiertag-Rückfall bleibt (Schlüssel vorhanden).
14. HTTPS-System: Sitzungs- und Antiforgery-Cookie tragen `Secure`; HTTP-System (DEV :8081/:8082): Anmeldung funktioniert, Cookies
    ohne `Secure`.
15. SMTP (Empfehlung): mit gesetztem `SmtpUsername` und `SmtpUseSsl = false` schreibt der Dienst je Versand eine Warnung ins Log; die
    Mail geht wie bisher (DEV: Pickup-Ordner → keine Warnung, weil kein SMTP-Weg, `MailService.cs:61-68`).
16. `Update-IdealAkeWms.ps1 -NurPruefen` auf DEV zeigt `Pruefung/BenutzerOhneKennwort` mit Zahl und Hinweis (Nicht-Prod); auf einem
    Prod-Ziel mit Zahl > 0 bricht der Lauf vor dem Stoppen ab (Logik `Update-IdealAkeWms.ps1:701`, im Dev-Lauf per Code-Lesung und
    `-NurPruefen` gegen eine DEV-Kopie mit `Prod=$true`-Testziel oder als Manual-UAT belegt). `NachlaufManifestDriftTests` grün.
17. Doku: `docs/betrieb/03` (Z. 92, 115), `07` (Abschnitt 2: Meldung R, Erstkennwort, Rückweg admin), `09` (Z. 20-21, 153-154,
    R1/R2/R4/R5/R7 Z. 187-193) und `README.md:63` sagen dasselbe wie der Code; Hilfe (Anmeldung, Benutzerverwaltung) und
    `Views/Help/Changelog.cshtml` v1.99.24; beide `AppVersion.cs` auf 1.99.24.
18. Build und alle Tests grün; neue Tests in `AccountControllerTests` für AK 1, 3, 4, 5 (und 6, falls entschieden).

## Test-Szenarien

Neu **TS-139** in `docs/TESTSZENARIEN.md` (danach `secondbrain/tests/testszenarien-index.md`); angepasst: TS-1.6 (`:174` „admin mit
Passwort leer“ → Erstkennwort, Verweis TS-139.4), TS-40.4 (`:5250` „ohne Passwort“ → „ohne Kennwort, Formular gesperrt“), TS-40.7
(`:5299` „Formular-Anmeldung mit lokalen Credentials“ → „mit Kennwort“). Windows-SSO/Negotiate, LDAP und Mobilgeräte = **Manual-UAT**.

- **TS-139.1 Lokaler Benutzer ohne Kennwort.** Vorb.: DEV-IIS (nicht `dotnet run`), Benutzer `ts139-lokal` ohne Kennwort, aktiv, Rolle
  `stock`. Schritte: abmelden, Name, Feld leer → Anmelden; dann mit Text `x`. Erwartet: Meldung R, Anmeldeseite bleibt, `/` leitet auf
  Login. Negativ: inaktiver Benutzer → „Benutzer nicht gefunden oder inaktiv.“ wie bisher.
- **TS-139.2 AD-Benutzer ohne Kennwort.** Vorb.: AKE-Test (HTTPS, IIS), `WindowsAuthAktiv = true`, AD-Benutzer ohne Kennwort. Schritte:
  a) Handy-Browser, Formular, Feld leer; b) Knopf „Mit Windows anmelden“ am Handy; c) Domänen-PC frische Sitzung. Erwartet: a) Meldung R
  mit Windows-Hinweis; b)/c) angemeldet (TS-40.9/40.1). **Manual-UAT.** Negativ: `WindowsAuthAktiv = false` → Meldung ohne Windows-Hinweis, kein Knopf.
- **TS-139.3 Kennwort richtig/falsch/leer.** Vorb.: Benutzer mit Kennwort. Erwartet wie AK 3.
- **TS-139.4 Erstkennwort admin.** Vorb.: Prüf-DB aus AK 10 bzw. DEV-Kopie mit `admin`-Hash `NULL` + App-Pool-Neustart. Schritte: `admin`,
  Feld leer → Schritt erscheint; leer absenden; abweichende Wiederholung; gültig. Erwartet wie AK 4; `/Users`-Hinweis „admin hat noch
  kein Kennwort“ vorher sichtbar, nachher weg; Log-Warnung beim Start vorher vorhanden. Negativ: AK 5 per Browser-Konsole (POST mit
  `NewPassword` für `ts139-lokal`) → nichts geändert.
- **TS-139.5 Übergang Terminal.** Vorb.: BDE aktiv, Terminal-Benutzer ohne Kennwort (DEV). Schritte: `-NurPruefen` → Zahl ≥ 1 mit
  Hinweis; als Admin unter `/Users/Edit` Kennwort setzen; `-NurPruefen` → 0; am Terminal mit Kennwort anmelden → BDE-Terminal öffnet
  mit Default-Werkbank. Negativ: ohne Kennwort → Meldung R.
- **TS-139.6 Entwicklungsmodus** (nur falls Rückfrage 3 = Ausnahme): `dotnet run`, Benutzer ohne Kennwort → angemeldet, Hinweis sichtbar;
  dieselbe DB über DEV-IIS → Meldung R.
- **TS-139.7 Erstinstallation auf leerer DB.** Schritte und Erwartung wie AK 9-10, Protokoll mit `@@SERVERNAME`, Exit-Codes, Zählungen,
  `DROP`. Negativ: Wächter-Test mit den alten Zeilen rot (AK 11).
- **TS-139.8 Index-Prüfung IDEAL-Prod** (nur falls SQL/151): `-NurPruefen` zeigt `Pruefung/FreshInstallIndizes` mit Zahl; Reparatur in 4a
  nach Bestätigung; danach P-2b leer. **Manual-UAT Gerald.**
- **TS-139.9 Installer.** Parser-Prüfung; Code-Lesung: keine Kennwort-Ausgabe, kein `SageLagerbuchung`-Kennwort; Voll-Lauf bei der
  nächsten Neuinstallation (Manual-UAT): `/ServiceSettings` zeigt `Sync:ProductionOrdersEnabled = false`, Ausgabe nennt Einschalten nach Teil 2.
- **TS-139.10 Cookies.** AKE-Test (HTTPS) und DEV (HTTP): Entwicklerwerkzeuge › Anwendung › Cookies `IdealAkeWms.Session` und
  `.AspNetCore.Antiforgery.*`: HTTPS mit `Secure`, HTTP ohne; Anmeldung und ein Formular-POST (Profil speichern) auf beiden ohne 400.
- **TS-139.11 SMTP-Hinweis** (Empfehlung): DEV-Dienst mit `SmtpHost` auf einen nicht vorhandenen Host, `SmtpUsername = test`,
  `SmtpUseSsl = false`, Fehlermail auslösen → Warnung im Notifications-Log, danach der bekannte Versandfehler. Negativ: ohne Benutzer
  keine Warnung. Danach Werte zurück.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt (kein Epic) | – | – |

## Deploy

**Vorläufig** – der QA-Lauf bestätigt gegen den echten Diff.

- **Web-App:** ja (Account, Users-Hinweis, Program.cs).
- **Service:** ja, falls BH-5c (MailService) bzw. die Bereinigung der Dienst-`appsettings.json` umgesetzt wird; die bestehende
  Server-Datei wird nicht überschrieben.
- **Migration:** nein. SQL/151 nur nach Rückfrage 4, kein EF-Bezug.
- **Reihenfolge Übergang:** (1) P-1 je System, (2) Kennwörter setzen bzw. deaktivieren **im alten Stand**, (3) `admin` mit leerem
  Kennwort prüfen und ggf. setzen, (4) Deploy (Prüfung sperrt Prod sonst), (5) Anwender-Info.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023):

| Id | Version | Typ | Firma | Phase | Ausführung | Inhalt / Text |
|---|---|---|---|---|---|---|
| `Pruefung/BenutzerOhneKennwort` | 1.99.24 | Pruefung | beide | Vorher | anzeigen | `Pruefung='SELECT COUNT(*) FROM dbo.Users u WHERE u.IsActive = 1 AND (u.PasswordHash IS NULL OR u.PasswordHash = N'''') AND (u.WindowsUserName IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = N''WindowsAuthAktiv'' AND LOWER([Value]) = N''true''))'` (Vergleich wie `WindowsAutoLoginMiddleware.cs:93-94`, ohne Trim). Text: „Aktive Benutzer ohne Kennwort, die sich ab v1.99.24 nicht mehr anmelden können (lokal, bzw. AD bei WindowsAuthAktiv aus): vorher unter /Users/Edit ein Kennwort setzen oder deaktivieren; Liste mit P-1 der Spec 2026-11-14-sicherheit-anmeldung-und-installer.“ Prod: Abbruch vor dem Stoppen (Rückfrage 2; bei „nur Hinweis“ Phase Nachher). |
| `v1.99.24-AnmeldungKennwort` | 1.99.24 | Info | beide | Vorher | anzeigen | „Ab v1.99.24 meldet das Formular nur mit Kennwort an. Vorher P-1 ausführen; AD-Benutzer ohne Kennwort nur noch per Windows-Anmeldung; admin mit leerem Kennwort im alten Stand prüfen (wird angemeldet = jetzt Kennwort setzen).“ |
| `v1.99.24-AnwenderInfo` | 1.99.24 | Info | beide | Nachher | anzeigen | „Anwender-Info: Anmeldung am Formular nur noch mit Kennwort; Terminals/Handgeräte mit dem neuen Kennwort anmelden; Domänen-PCs unverändert per Windows.“ |
| `Pruefung/FreshInstallIndizes` (nur Rückfrage 4) | 1.99.24 | Pruefung | IDEAL (`System=@('Prod')`) | Haltepunkt | Reparatur | `Pruefung` = Anzahl aus P-2b (als `SELECT COUNT(*) FROM (…) x`), `Datei='151_RepairFreshInstallIndexes.sql'`. Text: „Indizes aus Abschnitt 15 der Erstinstallation fehlen (Abbruch 2026-11-04): Reparatur legt sie idempotent an (nur IDEAL-Prod, FreshInstall-Herkunft).“ |
| `Pruefung/SmtpBenutzer` (nur Rückfrage 9 = Empfehlung) | 1.99.24 | Pruefung | beide | Nachher | anzeigen | `Appsettings='MailSettings:SmtpUsername'`. Text: „SMTP-Anmeldung als {Wert} ({Datei}): mit SmtpUseSsl = false geht das Kennwort unverschlüsselt – anonymes Relay oder SmtpUseSsl = true (Port 465).“ |

  Keine Schema-, Daten-, Einmal- oder Ordner-Einträge; kein Trockenlauf-Skript. Bestehender Eintrag `v1.71.0-AppsettingsProd`
  („mit Abschnitt SageLagerbuchung“) bleibt; Hinweis an den Dev-Lauf: Text ist seit ADR 0024 überholt – melden, nicht still ändern.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
  DEV beider Standorte (`standort: beide`); Update-Skript nach `ziele.psd1`.

## Offene Rueckfragen

1. **BH-1 Regel:** A streng (Empfehlung), B nur AD-Benutzer, oder C = A + Übergangsschalter? Begründung unter „Varianten BH-1“.
2. **Übergang:** Ergebnis P-1 je System (IDEAL-Prod, IDEAL-Test, AKE-Prod, AKE-Test, AKE-Test-IDEAL) – wie viele lokale/AD-Benutzer,
   Terminals, Kommissionierer ohne Kennwort, und steht `WindowsAuthAktiv` auf `true`? Soll `Pruefung/BenutzerOhneKennwort` den
   Prod-Deploy sperren (Phase Vorher, Empfehlung) oder nur hinweisen (Phase Nachher)?
3. **Lokale QA unter Kestrel:** Ausnahme nur bei `IsDevelopment()` mit sichtbarem Hinweis (Empfehlung: hält die QA-Läufe mit
   `QA-Agent`/`QA-Picker` ohne Geheimnis im Brain lauffähig, greift unter IIS nie) oder keine Ausnahme?
4. **BH-2:** Ergebnis P-2b auf IDEAL-Prod. Bei Treffern: SQL/151 + `Pruefung/FreshInstallIndizes` (Empfehlung) oder Reparatur von
   Hand? Guards für `WarehouseRequisitions`/`-Items` und Wächter-Test mit in diese Spec (Empfehlung: ja, beides)? P-2a auf allen
   Systemen zur Bestätigung (erwartet leer).
5. **BH-3:** `-SageLagerbuchungCredential` ersatzlos entfernen (Empfehlung) oder behalten und mit Hinweis abbrechen? SMTP-Kennwort
   weiter in der Dienst-Datei mit Installer-Warnung (Empfehlung) oder eigener ADR für den Umzug nach `ServiceSettings` (Typ `Geheim`)?
6. **BH-4:** Variante A – Installer setzt `Sync:ProductionOrdersEnabled = false` in der DB (Empfehlung, schließt die Autostart-Lücke)
   oder B – nur die wirkungslose Zeile streichen? Tote Blöcke aus `IDEALAKEWMSService/appsettings.json` entfernen (Empfehlung ja,
   `Feiertag*` bleiben)?
7. **BH-5a:** Erstkennwort-Schritt (Empfehlung), Installer-Abfrage mit Hash in PowerShell, oder unverändert mit Warnung?
8. **BH-5b:** `SecurePolicy = SameAsRequest` für Sitzung und Antiforgery (Empfehlung), `Always` (bricht IDEAL-Test :88 und DEV)
   oder unverändert?
9. **BH-5c:** Warnung + `Pruefung/SmtpBenutzer` (Empfehlung), `StartTlsWhenAvailable` (Zertifikatsrisiko am Relay) oder unverändert?
   Prüfschritt dazu: je Server in der Dienst-`appsettings*.json` `MailSettings:SmtpHost`, `SmtpPort`, `SmtpUseSsl` und ob
   `SmtpUsername` gesetzt ist (Werte, nie das Kennwort, in die Antwort).
10. **Zusatzfunde** (keine Sperre nach Fehlversuchen, keine Mindestlänge, unterschiedliche Meldungen verraten vorhandene Namen
    `AccountController.cs:77`/`:87`, Installer-Mindestzahl 75 Migrationen `Install-IdealAkeWms.ps1:433` = BH-7): Folgeaufgabe
    (Empfehlung) oder in diese Spec?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. BH-1 Regel A / B / C →
2. Übergang: P-1-Ergebnis je System; Prüfung sperrt Prod (Vorher) oder nur Hinweis (Nachher) →
3. Kestrel-QA: Ausnahme bei IsDevelopment() ja/nein →
4. BH-2: P-2b-Ergebnis IDEAL-Prod; SQL/151 + Prüfung ja/nein; Guards + Wächter-Test ja/nein →
5. BH-3: SData-Parameter entfernen ja/nein; SMTP Warnung oder eigener ADR →
6. BH-4: Variante A / B; tote Blöcke entfernen ja/nein →
7. BH-5a: Erstkennwort / Installer-Abfrage / unverändert →
8. BH-5b: SameAsRequest / Always / unverändert →
9. BH-5c: Warnung + Prüfung / StartTlsWhenAvailable / unverändert; MailSettings je Server →
10. Zusatzfunde: Folgeaufgabe oder in diese Spec →
