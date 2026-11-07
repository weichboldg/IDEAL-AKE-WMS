---
typ: aufgabe
status: offen
erstellt: 2026-10-29
---
# Lokale Entwicklungs-/QA-Umgebung „DEV“ (AKE_ und IDEAL_) auf Geralds PC

**Auftrag Gerald 2026-10-29:** Lokale DEV-Umgebung mit Datenbanken, Website und Dienst je Standort, damit QA vor dem Deploy auf die
Testserver lokal laufen kann. SQL Express und IIS sind installiert. Präfix `AKE_` = Hauptcheckout (`main`), `IDEAL_` = Bündel-Worktree.

**Entscheidungen Gerald 2026-10-29:**
1. Daten: **Kopie der Test-DBs** (Backup → lokaler Restore), nicht leer.
2. Sage: **nur lesen, Buchen aus.** DEV darf nie an Sage buchen und nie Mails senden.
3. Ausführung: **Claude Code als Administrator** auf Geralds PC (Cowork kann IIS/SQL von hier nicht steuern).

## Namen und Ziele

| | AKE | IDEAL |
|---|---|---|
| Code-Quelle | `C:\Git\IDEAL-AKE-WMS` (main) | `.claude\worktrees\2026-08-07-ideal-teile-1-5` |
| Datenbank (`.\SQLEXPRESS`) | `AKE_WMS_DEV` | `IDEAL_WMS_DEV` |
| IIS-Site / App-Pool | `AKE_WMS_DEV`, http://localhost:8081 | `IDEAL_WMS_DEV`, http://localhost:8082 |
| Windows-Dienst | `AKE_WMS_DEV_Service` | `IDEAL_WMS_DEV_Service` |
| Ordner (existieren) | `D:\Dev\AKE\Web`, `D:\Dev\AKE\Service` | `D:\Dev\IDEAL\Web`, `D:\Dev\IDEAL\Service` |
| Backup | `D:\akewms.bak` | `D:\idealwms.bak` |

Ports und Instanzname sind Vorschläge; Claude Code prüft Belegung/Instanz und meldet Abweichungen.

## Belege aus dem Code (Hauptcheckout, nur gelesen)

- Web führt beim Start `db.Database.Migrate()` aus (`IdealAkeWms/Program.cs:140`) → restaurierte DB wird auf den Code-Stand gehoben.
- Sage-Buchung nur über SData-HTTP, Schalter `SageLagerbuchungAktiv` in `dbo.ServiceSettings` (`SageBookingWorker.cs:66`,
  `ServiceSettingDefinitions.cs:36`), Ziel `SData:BaseUrl`. Dienst liest Schalter aus der DB (`IDEALAKEWMSService/Common/ServiceSettings.cs:25`).
- Mails nur aus dem Dienst, nur wenn `MailSettings:SmtpHost` gesetzt (`MailService.cs:27-30`).
- Stückliste liest Sage direkt mit Drei-Teil-Namen `[ake].[dbo].[vw_AKE_Kommissionierung_StuecklistenDB]` (`BomRepository.cs:41`) –
  lokal gibt es keine DB `ake` → Live-Stückliste aus Sage geht in DEV vorerst **nicht** (BOM-Cache aus der Kopie schon).
- SQL Express hat keinen SQL-Agent (Agent-Jobs irrelevant, Syncs laufen im Dienst) und max. **10 GB je Datenbank**.

## Ablauf (Claude Code)

1. **Voraussetzungen prüfen, nichts installieren ohne Rückfrage:** SQL-Express-Instanz, IIS-Features (Windows-Authentifizierung,
   anonyme Auth), ASP.NET Core Hosting Bundle .NET 10, .NET 10 SDK, freie Ports.
2. **Restore** der von Gerald gelieferten `.bak` (COPY_ONLY) als `AKE_WMS_DEV`/`IDEAL_WMS_DEV`; vorher `RESTORE FILELISTONLY` →
   Datengröße > 10 GB: STOPP und melden.
3. **DEV-Absicherung SQL direkt nach dem Restore, vor jedem Start** (je DB):
   - `SageLagerbuchungAktiv` = `false`, `SData:BaseUrl` = `''`
   - alle `Sync:%Enabled` = `false`, `ErrorNotification:Enabled` = `false`, `Notifications:MeldebestandEnabled` = `false`
   - Prüf-SELECT der geänderten Zeilen ins Protokoll.
4. **Konfiguration ohne Repo-Änderung:** je Standort und Anwendung `appsettings.DEV.json` im Zielordner (Name genau so – ASP.NET
   lädt `appsettings.{Umgebung}.json` automatisch, `appsettings_dev` würde ignoriert). Inhalt:
   - `DefaultConnection` → **immer die lokale DEV-DB** (nie die Test-DB: DEV würde sonst gemeinsame Testdaten ändern).
   - `SageConnection`, `OseonConnection`, `EnaioDmsConnection` → die Lese-Quellen des **jeweiligen Testsystems** (AKE bzw. IDEAL),
     von Gerald je Standort einzutragen; Claude Code setzt Platzhalter `<<TESTSYSTEM-…>>` und listet sie im Ergebnis.
   - `MailSettings:SmtpHost` = `""`, `ErrorNotification:Enabled` = false, Sync-Schalter aus.
   - Hinweis: Die Live-Stückliste (`BomRepository.cs:41`, `[ake].[dbo]…`) nutzt keine Verbindungszeichenfolge, sondern den
     Drei-Teil-Namen auf dem Server der WMS-DB → in DEV trotz Eintrag nicht verfügbar. Umgebung `DEV` über
   App-Pool-Umgebungsvariable `ASPNETCORE_ENVIRONMENT=DEV` bzw. Dienst-Umgebung `DOTNET_ENVIRONMENT=DEV` (übersteht Re-Deploy,
   da `appsettings.DEV.json` nicht im Publish liegt).
5. **Identitäten (Nachtrag Gerald 2026-10-29):** Dienst-Konto und App-Pool-Anmeldedaten hinterlegt **Gerald manuell** nach der
   Einrichtung (Dienst-Benutzer bzw. IIS-App-Pool-Identität). Claude Code legt App-Pool mit `ApplicationPoolIdentity` und Dienst
   als virtuelles Konto `NT SERVICE\<Dienstname>` an; für den Rauchtest bekommen `IIS APPPOOL\<Pool>` und
   `NT SERVICE\<Dienstname>` ein SQL-Login mit `db_owner` nur auf der eigenen DEV-DB. Zusätzlich schreibt er ein Skript `D:\Dev\scripts\dev-sql-logins.sql` (Parameter: Konto-Name je Standort →
   SQL-Login + `db_owner` nur auf der eigenen DEV-DB), das Gerald nach dem Hinterlegen der Konten ausführt.
   Geralds Windows-Benutzer hat `sa`-Rechte auf `.\SQLEXPRESS` (Restore/Absicherung laufen damit).
6. **IIS:** Site, App-Pool (No Managed Code, Start Mode AlwaysRunning, Idle Timeout 0), Windows- **und** anonyme Auth (README
   „Windows-Anmeldung / SSO“), Preload an.
7. **Publish** aus der jeweiligen Code-Quelle (`dotnet publish -c Release`) in die Zielordner; Dienst mit `sc.exe create` registrieren,
   Starttyp **manuell**.
8. **Re-Deploy-Skript** außerhalb des Repos: `D:\Dev\scripts\dev-deploy.ps1 -Standort AKE|IDEAL` (Dienst/App-Pool stoppen,
   Publish, `appsettings.DEV.json` nicht überschreiben, starten, Fußzeilen-Version per HTTP prüfen).
9. **Rauchtest:** Startseite beider Sites (HTTP 200, Version in der Fußzeile), Dienst-Log ohne Fehler, Prüf-SELECT aus Schritt 3
   nach dem ersten Dienst-Start erneut (Schalter weiter aus).

## Grenzen

- Kein Git-Befehl außer lesend, keine Änderung an Repo-Dateien (Code, `appsettings*.json` im Repo, CLAUDE.md, `.claude\`).
- Keine Aktion auf AKESQL20 / den Testservern (kein Backup durch Claude Code, keine Rechte-Änderung) – die `.bak` liefert Gerald.
- Sage-Lesen (Syncs, Live-Stückliste) erst in einer zweiten Stufe, wenn das Maschinenkonto Leserechte auf Sage hat
  (ungeprüft, Gerald/DBA). Bis dahin alle Syncs aus.

## Offen (Gerald)

- Backups: `D:\akewms.bak`, `D:\idealwms.bak` (Gerald 2026-10-29). Lese-Verbindungen der Testsysteme in die `appsettings.DEV.json`.
- Nach der Einrichtung: Dienst-Konten und App-Pool-Anmeldedaten hinterlegen, dann `dev-sql-logins.sql` ausführen.
- Danach Stufe 2 entscheiden: Sage-Leserechte für das Maschinenkonto oder DEV bleibt ohne Sage-Syncs.

## Ergebnis 2026-10-29

Eingerichtet durch Claude Code (Admin-Shell, `AKENB\gerald.weichbold`). Kein Repo-Datei geändert, kein Commit, kein schreibender
Git-Befehl, keine Aktion auf AKESQL20 oder den Testservern.

### Angelegte Objekte

| | AKE | IDEAL |
|---|---|---|
| Code-Stand | `main`, v1.30.1 | Bündel-Worktree, v1.70.0 |
| Datenbank (lokale **Standard-Instanz `.`**, Express 17.0) | `AKE_WMS_DEV` (aus `D:\akewms.bak`, Quelle `IDEAL_AKE_WMS_Test`, Backup 29.10. 08:57) | `IDEAL_WMS_DEV` (aus `D:\idealwms.bak`, Quelle `IDEAL_AKE_WMS_TEST`, Backup 29.10. 09:12) |
| Größe Daten / Log | 1 864 MB / 3 400 MB | 2 568 MB / 456 MB |
| Letzte Migration (DB = Code, beim Start nichts nachgezogen) | `20260806120650_AllowMultipleDummyRequisitionItems` | `20261019092105_AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung` |
| IIS-Site / App-Pool | `AKE_WMS_DEV`, http://localhost:8081 | `IDEAL_WMS_DEV`, http://localhost:8082 |
| App-Pool | No Managed Code, AlwaysRunning, Idle 0, kein periodisches Recycling, `ApplicationPoolIdentity`, Umgebungsvariable `ASPNETCORE_ENVIRONMENT=DEV` | ebenso |
| Site | Windows- und anonyme Auth an, Preload an | ebenso |
| Dienst (Starttyp **manuell**, `NT SERVICE\<Name>`, Registry-`Environment` `DOTNET_ENVIRONMENT=DEV`) | `AKE_WMS_DEV_Service` → `D:\Dev\AKE\Service\IDEALAKEWMSService.exe` | `IDEAL_WMS_DEV_Service` → `D:\Dev\IDEAL\Service\IDEALAKEWMSService.exe` |
| SQL-Logins (Rauchtest, `db_owner` nur eigene DB) | `IIS APPPOOL\AKE_WMS_DEV`, `NT SERVICE\AKE_WMS_DEV_Service` | `IIS APPPOOL\IDEAL_WMS_DEV`, `NT SERVICE\IDEAL_WMS_DEV_Service` |
| Ordnerrechte | Pool: RX `Web`, M `Web\logs`, M `keys`; Dienst: RX `Service`, M `Service\logs` | ebenso |
| DataProtection-Schlüssel | `D:\Dev\AKE\keys` | `D:\Dev\IDEAL\keys` |

Maschinenweit: IIS-Features `IIS-WindowsAuthentication` und `IIS-ApplicationInit` aktiviert; Abschnitte
`windowsAuthentication`/`anonymousAuthentication` auf Server-Ebene entsperrt (sonst 500.19 durch die `web.config`). Hosting Bundle
10.0.14 hat Gerald installiert.

Skripte in `D:\Dev\scripts`:
- `dev-absicherung-ake.sql`, `dev-absicherung-ideal.sql` – DEV-Absicherung (idempotent, legt fehlende Schalter als `false` an).
- `dev-deploy.ps1 -Standort AKE|IDEAL [-OhneDienst]` – stoppen → Publish → `web.config` auf DEV → Site starten + Startseite →
  Absicherung → Dienst starten. Bricht vor jedem Start ab, wenn `appsettings.DEV.json` fehlt. Getestet: AKE, PowerShell 5.1, exit 0.
- `dev-sql-logins.sql` – vorbereitet, **nicht ausgeführt** (Variablen `Konto`, `DevDb`; Aufruf im Kopf des Skripts).

Protokolle in `D:\Dev\logs`.

### DEV-Absicherung (Schritt 3, vor dem ersten Start)

Vorher (Testsystem-Stand):
- AKE: **alle 18 Schalter `true`**, darunter `SageLagerbuchungAktiv`, `ErrorNotification:Enabled` und
  `Notifications:MeldebestandEnabled`; `SData:BaseUrl` = `https://sagetest01.ake.at:5493/`.
- IDEAL: `true` bei Meldebestand, Articles, Feiertag, HierarchicalFa, Lagerbestand, Lagerplätze, OperationConfigSync,
  ProductionWorkplaceSync, WorkOperationStructureDetection und dem Altnamen `Sync:FaWorkStepStructureDetectionEnabled`;
  `SageLagerbuchungAktiv` und `SData:BaseUrl` waren schon aus bzw. leer.

Nachher und nach dem ersten Dienst-Start erneut geprüft: AKE 18/18, IDEAL 24/24 Schalter `false`, `SData:BaseUrl` leer.
0 Fehler/Warnungen in `SyncLogs` seit Start. Die Dienste schreiben nachweislich in die DEV-DBs (AKE 8, IDEAL 6 neue
`SyncLogs`-Zeilen: BdeAutoPause, Cleanup, FaMaterialization).

### Rauchtest

| | Startseite | Fußzeile | Dienst |
|---|---|---|---|
| AKE | HTTP 200 nach Windows-Anmeldung (Negotiate, `AKENB\gerald.weichbold`) | **v1.30.1** | läuft, „SyncWorker gestartet. Version 1.30.1“, keine Fehler |
| IDEAL | HTTP 200, aber nur die **Anmeldemaske** (`/Account/Login`) | **nicht geprüft** – Version 1.70.0 nur in der DLL belegt | läuft, „Version 1.70.0 (2026-10-28)“, keine Fehler |

IDEAL: In den Testdaten ist `WindowsAuthAktiv=false`, und es gibt keinen Benutzer-Datensatz für `gerald.weichbold`. Kurz testweise
auf `true` gesetzt: Negotiate klappt, Middleware meldet „KEIN aktiver Benutzer-Datensatz“. Danach wieder auf `false` zurückgesetzt.
Fußzeile prüft Gerald nach Formular-Anmeldung mit einem IDEAL-Testbenutzer.

### Platzhalter in den `appsettings.DEV.json`

In allen vier Dateien (`D:\Dev\{AKE,IDEAL}\{Web,Service}\appsettings.DEV.json`) je Standort:
- `ConnectionStrings:SageConnection` = `<<TESTSYSTEM-AKE-SageConnection>>` / `<<TESTSYSTEM-IDEAL-SageConnection>>`
- `ConnectionStrings:OseonConnection` = `<<TESTSYSTEM-AKE-OseonConnection>>` / `<<TESTSYSTEM-IDEAL-OseonConnection>>`
- `ConnectionStrings:EnaioDmsConnection` = `<<TESTSYSTEM-AKE-EnaioDmsConnection>>` / `<<TESTSYSTEM-IDEAL-EnaioDmsConnection>>`

Weitere Einträge:
- `DefaultConnection` = `Server=.;Database=<DEV-DB>;Trusted_Connection=True;…`
- `MailSettings:SmtpHost` = `""`, `ErrorNotification:Enabled` = false, `SData:BaseUrl` = `""`.
- Alle `Sync:*Enabled` stehen auf false. Im Web zusätzlich `DataProtection:KeysPath`.
- Hinweis: Das Web braucht `OseonConnection` (`BomRepository.cs:17`); der Platzhalter fällt erst beim Aufruf auf, nicht beim Start.

### Abweichungen vom Plan

1. **SQL-Instanz:** Auf Geralds Wunsch die **Standard-Instanz `.` (MSSQLSERVER, ebenfalls Express)** statt `.\SQLEXPRESS`.
   Die 10-GB-Grenze gilt dort genauso.
2. **`web.config` setzt `ASPNETCORE_ENVIRONMENT=Production`** (aus dem Repo). Das hat Vorrang vor der App-Pool-Variable: Die Site
   hätte `appsettings.DEV.json` ignoriert und wäre gegen AKESQL20 gelaufen. In der veröffentlichten `web.config` (nicht im Repo) auf
   `DEV` gesetzt. `dev-deploy.ps1` macht das nach jedem Publish erneut und bricht ab, wenn es nicht greift. Die
   App-Pool-Variable bleibt zusätzlich gesetzt.
3. **Altname `Sync:FaWorkStepStructureDetectionEnabled=true`** in der IDEAL-Kopie. Wird vom Code nicht gelesen
   (`ServiceSettingDefinitions.cs:43`, „nie deployt“), trotzdem auf false. Die Absicherung setzt jetzt **alle** `Sync:%Enabled`.
4. **Ordner** `Web`/`Service`/`scripts` gab es noch nicht – angelegt, dazu `D:\Dev\logs` und `D:\Dev\{AKE,IDEAL}\keys`.
5. **Eigene DataProtection-Schlüssel je Site** statt des Standards `%ProgramData%\IdealAkeWms` (dort fehlt dem Pool das
   Schreibrecht, und beide Sites hätten Schlüssel geteilt). Beide Sites laufen auf `localhost` und teilen sich deshalb die Cookies
   (Cookies unterscheiden keine Ports). Beim Wechsel zwischen 8081 und 8082 kann eine erneute Anmeldung nötig sein.
6. **IDEAL-Fußzeile** im Rauchtest nicht geprüft (siehe oben).
7. **Ungeschaltete Dienst-Jobs** laufen auch in DEV und schreiben nur lokal: BDE-AutoPause, Aktivitätsprotokoll-Bereinigung,
   IDEAL FA-Materialisierung (beim ersten Lauf 12 Lack-Flags gesetzt).

### Offen (Gerald)

- Platzhalter `<<TESTSYSTEM-…>>` in den vier `appsettings.DEV.json` ersetzen (bis Stufe 2 nicht nötig, Syncs aus).
- Dienst-Konten und App-Pool-Anmeldedaten hinterlegen, dann `dev-sql-logins.sql` je Konto ausführen. Die Rauchtest-Logins
  `IIS APPPOOL\…`/`NT SERVICE\…` danach bei Bedarf entfernen.
- IDEAL: Fußzeile nach Formular-Anmeldung prüfen. Optional `WindowsAuthAktiv` + eigener Benutzer in `IDEAL_WMS_DEV`.

### Nachtrag 2026-10-29 (Testsystem-Verbindungen)

- In allen vier `appsettings.DEV.json` gesetzt (Vorstand je Datei als `appsettings.DEV.json.bak-20261029`):
  - AKE: Sage `AKESQL20.ake.at / ake_TEST2026`, Oseon `aketrumpf01.ake.at\TRUMPFSQL2 / T1000_V01_V001`, enaio `AKESQL20.ake.at / enaio`.
  - IDEAL: Sage `idealsql2.ideal.ideal-ake.at\SAGE100TEST / IDEAL_TEST_2026_05_03`, enaio `AKESQL20.ake.at / enaio`.
  - `MailSettings`: `smtp.ake.at:25`, ohne SSL, Absender `IDEAL AKE WMS DEV <wms-dev@ake.at>` (ersetzt die Vorgabe „SmtpHost leer“).
- **Noch Platzhalter:** IDEAL `OseonConnection`. Der gelieferte Wert `Server=\TRUMPFSQL2` hat keinen Rechnernamen; Gerald nennt den Host.
- Mail-Schalter (`Sync:*EmailEnabled`, `ErrorNotification:Enabled`, `Notifications:MeldebestandEnabled`) bleiben `false`; erst ein
  Einschalten verschickt Mails an die Empfänger aus den Testdaten.
- Rauchtest nach Neustart von App-Pools und Diensten: AKE HTTP 200, Fußzeile **v1.30.1**; IDEAL HTTP 200, Fußzeile **v1.70.0**
  (Windows-Anmeldung mit dem von Gerald angelegten Benutzer, `WindowsAuthAktiv=true`). Prüf-SELECT: 0 Schalter ungleich false,
  `SData:BaseUrl` leer, Dienst-Logs ohne Fehler. Abweichung 6 ist damit erledigt.

### Nachtrag 2026-10-30 (Dienstkonto)

- Gerald hat beide Dienste (`AKE_WMS_DEV_Service`, `IDEAL_WMS_DEV_Service`) und beide App-Pools (`AKE_WMS_DEV`, `IDEAL_WMS_DEV`) auf
  `AKENB\svc_idealakewms` (UPN `svc_idealakewms@ake.at`) umgestellt. Geprüft per `appcmd … /text:processModel.userName` und Besitzer der `w3wp.exe`.
- `dev-sql-logins.sql` für `AKENB\svc_idealakewms` auf `AKE_WMS_DEV` und `IDEAL_WMS_DEV` ausgeführt → Login + `db_owner` (geprüft über
  `sys.database_role_members`).
- **Fallstrick:** Die alte Prüfabfrage `IS_ROLEMEMBER` scheiterte mit Msg 15404 / 0x5, weil `NT Service\MSSQLSERVER` das AD nicht lesen
  darf. Login, Benutzer und Rolle waren trotzdem angelegt. Das Skript prüft jetzt über die Katalogsicht und endet mit exit 0.
- Ordnerrechte nicht nötig: „Authentifizierte Benutzer“ haben Ändern auf `D:\Dev\…` (vererbt).
- Rauchtest nach Neustart: AKE HTTP 200 **v1.30.1**, IDEAL HTTP 200 **v1.70.0**. Beide Dienste laufen und schreiben in ihre DEV-DB,
  0 Error/Warning in `SyncLogs`, 0 Schalter ungleich false, `SData:BaseUrl` leer.
- Rauchtest-Logins auf Geralds Zuruf entfernt (Benutzer in beiden DEV-DBs + Server-Login): `IIS APPPOOL\AKE_WMS_DEV`,
  `NT SERVICE\AKE_WMS_DEV_Service`, `IIS APPPOOL\IDEAL_WMS_DEV`, `NT SERVICE\IDEAL_WMS_DEV_Service`. Einziges WMS-Login ist jetzt
  `AKENB\svc_idealakewms`. Danach Neustart: beide Sites HTTP 200 (v1.30.1 / v1.70.0), beide Dienste ohne Fehler.
- Aus dem Restore mitgekommen (verwaiste Gruppen-Benutzer ohne lokales Login, nicht angefasst): `AKENB\SQL_AKE-BDELight_DBOwner`
  in `AKE_WMS_DEV`, `IDEAL\SQL_IdealAKEWMS_DBOwner` in `IDEAL_WMS_DEV`.
- Offen: IDEAL `OseonConnection` hat weiter einen Platzhalter.

### Nachtrag 2026-10-30 (qa-agent nutzt DEV)

- Entscheidung Gerald: Der qa-agent aktualisiert je Standort Web **und** Dienst auf DEV und testet dort → [[0018-qa-agent-deployt-und-testet-auf-lokaler-dev-umgebung]].
- `dev-deploy.ps1` hat jetzt `-Quelle <Checkout/Worktree>` (ohne Angabe wie bisher: AKE = main, IDEAL = Bündel-Worktree). Parse-Check ok, noch nicht mit `-Quelle` gelaufen.
- Spec-Vorlage + spec-agent: neues Frontmatter-Feld `standort: AKE | IDEAL | beide`.
- qa-agent: Schritt 6 „DEV deploy and test run“ eingetragen. **Offen:** `tools:`-Zeile des qa-agent um `PowerShell` und die
  `mcp__claude-in-chrome__*`-Werkzeuge erweitern – die automatische Freigabe hat das als Selbst-Rechteerweiterung abgelehnt, Gerald trägt es selbst ein.

### Nachtrag 2026-10-30 (erster Lauf mit `-Quelle`, qa-agent)

- `dev-deploy.ps1 -Standort IDEAL -Quelle C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5` → **Exit 0**, Fußzeile
  **v1.71.0**, Dienst „Version 1.71.0“. `-Quelle` funktioniert. Spec: [[2026-10-30-sage-sdata-fa-ansicht-spec]].
- **Befund S-1 (Skript):** Das Skript vergleicht die Fußzeilen-Version nicht mit der `AppVersion` der Quelle. Es gibt sie nur aus
  und endet auch bei falscher Version oder Anmeldemaske (`v?`) mit Exit 0. Der qa-agent muss die Version selbst prüfen.
  Vorschlag: `AppVersion.cs` der Quelle lesen (`Version = "x.y.z"`) und bei Abweichung `throw`.
- **Befund S-2 (Absicherung/Reihenfolge):** Im VORHER-Teil der Absicherung stand in `IDEAL_WMS_DEV` `SData:BaseUrl =
  https://sagetest01.ake.at:5493/`. Am 29.10. war der Wert noch leer, Urheber unbekannt (`ServiceSettings` ohne Audit-Spalten). Das
  Skript startet die Site **vor** der Absicherung (wegen Migrate), also lief das Web kurz mit dem befüllten Wert. Das Risiko ist
  gering: Der Dienst startet erst danach, und die Web-Lesefunktion hängt am Gate (aus). Trotzdem empfehle ich, die Absicherung
  zusätzlich **vor** dem Site-Start laufen zu lassen (idempotent), damit kein Fenster entsteht.
- Ausgabe enthält nur vorbestehende Build-Warnungen (NU1902/NU1903, CS8321).

## Nachtrag 2026-10-30 (Gerald): `SData:BaseUrl` bleibt auf DEV IDEAL gesetzt

Auf Geralds Anweisung („BaseUrl soll enthalten bleiben und wenn gelöscht wieder eingefügt“) setzt
`D:\Dev\scripts\dev-absicherung-ideal.sql` `SData:BaseUrl` jetzt auf `https://sagetest01.ake.at:5493/` statt auf `''`
(UPDATE bzw. INSERT, idempotent; Sicherung `dev-absicherung-ideal.sql.bak-2026-10-30`). Zweck: Lesezugriff der Ansicht
„FA-Struktur (Sage live)“ (v1.71.0). **Buchen bleibt gesperrt** über `SageLagerbuchungAktiv = false` (weiter im Schalter-Set
des Skripts, Prüfzeile `NichtFalse` = 0). Erklärt auch den Befund S-2: Der Wert vor dem Deploy war Geralds manueller
Testeintrag. `dev-absicherung-ake.sql` ist unverändert (BaseUrl dort weiter leer).

### Nachtrag 2026-11-02 (Sage-Lesen in DEV)

- `D:\Dev\IDEAL\Web\appsettings.DEV.json`: Abschnitt `SageLagerbuchung` (Benutzer `sage` + Passwort) aus `D:\Dev\AKE\Service\appsettings.DEV.json`
  übernommen (Vorstand `.bak-20261102`), App-Pool neu gestartet. Seitenprüfung `/SageSdataFa` durch Gerald im Browser (HttpClient landete auf der Anmeldemaske).
- DEV-Absicherung (`dev-absicherung-{ake,ideal}.sql`, Vorstand `.bak-20261102`) fasst `SData:BaseUrl` **nicht mehr an** – Sage-Lesen ist in DEV erlaubt.
  Weiter gesperrt: `SageLagerbuchungAktiv`, alle `Sync:*Enabled`, `ErrorNotification:Enabled`, `Notifications:MeldebestandEnabled` (jeweils `false`).
  Stand danach: AKE `SData:BaseUrl` leer, IDEAL `https://sagetest01.ake.at:5493/`.
- qa-agent: Hinweis in 6b ergänzt, `PowerShell` in `tools:`. `SData:BaseUrl` aus „Hard limits on DEV“ gestrichen (Gerald 2026-11-02).
- `SData:Dataset = ake_TEST2026;1` in `IDEAL_WMS_DEV` bleibt vorerst so (Gerald 2026-11-02).
