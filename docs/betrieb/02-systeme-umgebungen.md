# 02 – Systeme und Umgebungen

**Stand:** v1.99.23, 2026-11-14 · **Zielgruppe:** IT-Administratoren · **Zweck:** Steckbrief je Installation (Prod/Test/DEV, AKE und
IDEAL): Server, Adresse, IIS, Dienst, Konto, Datenbank, Pfade. Vor jeder Diagnose oder Änderung zuerst hier das Zielsystem klären.

Übersicht: [00-index.md](00-index.md). Architektur: [01-architektur-technologie.md](01-architektur-technologie.md). Konten und Rechte im
Detail: [09-sicherheit-konten-rechte.md](09-sicherheit-konten-rechte.md).

---

## 1. Maßgebliche Quelle

Die Zielsysteme stehen in **`scripts/deploy/ziele.psd1`** (eine Zeile je System: Firma, System, Prod ja/nein, Web-Pfad, Dienst-Pfad, URL).
Alles Weitere – IIS-Site, App-Pool, Dienstname, Dienstkonto, Datenbank – ermittelt das Update-Skript bei jedem Lauf **selbst** und zeigt
es vor jeder Änderung an. (Quelle: `scripts/deploy/ziele.psd1:1-16`, `scripts/deploy/Update-IdealAkeWms.ps1:163-226`, `:648-662`)

So ermittelt das Skript (nur lesend):

| Was | Wie | Beleg |
|---|---|---|
| IIS-Site / App-Pool | Site oder Anwendung, deren physischer Pfad = Web-Pfad; genau ein Treffer, sonst Abbruch | `scripts/deploy/Update-IdealAkeWms.ps1:169-183` |
| Dienst | Windows-Dienst, dessen Programmpfad = `<Dienst-Pfad>\IDEALAKEWMSService.exe` (kein Name wird vorausgesetzt) | `scripts/deploy/Update-IdealAkeWms.ps1:184-191` |
| Umgebung Web | `ASPNETCORE_ENVIRONMENT` aus `web.config` (sonst `Production`) | `scripts/deploy/Update-IdealAkeWms.ps1:138-142`, `IdealAkeWms/web.config:18` |
| Umgebung Dienst | `DOTNET_ENVIRONMENT` aus dem Registry-Wert `Environment` des Dienstes, sonst Maschinen-Variable, sonst `Production` | `scripts/deploy/Update-IdealAkeWms.ps1:194-198` |
| Datenbank | `ConnectionStrings:DefaultConnection` aus `appsettings.json`, überschrieben von `appsettings.<Umgebung>.json`; Web und Dienst müssen auf **dieselbe** DB zeigen, sonst Abbruch | `scripts/deploy/Update-IdealAkeWms.ps1:144-160`, `:199-203` |
| Dienstkonto / Pool-Identität | `StartName` des Dienstes; `processModel` des App-Pools | `scripts/deploy/Update-IdealAkeWms.ps1:220-222`, `scripts/deploy/IdealAkeWms.Deploy.psm1:106-124` |

**Empfehlung zum Nachschlagen der Live-Werte:** `Update-IdealAkeWms.ps1 -Firma <AKE|IDEAL> -System <Name> -NurPruefen` am Zielserver. Das
stoppt nichts und schreibt nichts, gibt aber Site, Pool, Dienst, Konto, Umgebung, DB, URL und Mail-Ziel aus.
(Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:740-747`, `README.md:73`)

---

## 2. Übersicht aller Systeme

| Firma | System | Prod | Server | Startseite (URL) | Web-Pfad | Dienst-Pfad |
|---|---|---|---|---|---|---|
| IDEAL | Prod | ja | idealweb01 | `https://idealweb01.ideal.ideal-ake.at/` | `C:\inetpub\wwwroot\IDEALAKEWMS` | `C:\IDEAL\IdealAkeWmsService` |
| IDEAL | Test | nein | idealweb01 | `http://idealweb01.ideal.ideal-ake.at:88/` | `C:\inetpub\wwwroot\IDEALAKEWMS_Test` | `C:\IDEAL\IdealAkeWmsService_Test` |
| AKE | Prod | ja | akenet01 | `https://akenet01.ake.at:4443/` | `C:\inetpub\wwwroot\IDEALAKEWMS` | `C:\AKE\IdealAkeWmsService` |
| AKE | Test | nein | akenet01 | `https://akenet01.ake.at:4444/` | `C:\inetpub\wwwroot\IDEALAKEWMS_Test` | `C:\AKE\IdealAkeWmsService_Test` |
| AKE | Test-IDEAL | nein | akenet01 | `https://akenet01.ake.at:4445/` | `C:\inetpub\wwwroot\IdealAkeWMS-TESTIDEAL` | `C:\AKE\IDEALAKEWMSService-TestIDEAL` |
| AKE | DEV | nein | Entwickler-PC | `http://localhost:8081/` | `D:\Dev\AKE\Web` | `D:\Dev\AKE\Service` |
| IDEAL | DEV | nein | Entwickler-PC | `http://localhost:8082/` | `D:\Dev\IDEAL\Web` | `D:\Dev\IDEAL\Service` |

(Quelle: `scripts/deploy/ziele.psd1:5-15`)

Hinweise:
- **AKE Test-IDEAL** ist eine eigene Installation auf akenet01 für den IDEAL-Code-Stand mit AKE-Konfiguration. Das Skript behandelt sie
  als Firma AKE. (Quelle: [[2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec]] Abschnitt „Zielsysteme“)
- IDEAL-Prod und AKE-Prod haben denselben Web-Pfad, liegen aber auf verschiedenen Servern. (Quelle: [[fallstricke]] §30)
- Startseiten-URL in `ziele.psd1` hat Vorrang vor der IIS-Bindung; passt sie nicht zur Bindung, warnt das Skript. (Quelle:
  `scripts/deploy/ziele.psd1:3-4`, `scripts/deploy/Update-IdealAkeWms.ps1:204-217`)

---

## 3. Steckbriefe

Legende: „Brain“ = Wissensbasis `secondbrain/`. Was dort nicht steht, ist als **zu prüfen** markiert (am Server mit `-NurPruefen` ablesbar).

### 3.1 IDEAL – Prod

| Merkmal | Wert | Beleg |
|---|---|---|
| Server | idealweb01 | `scripts/deploy/ziele.psd1:5`, `:9` |
| URL | `https://idealweb01.ideal.ideal-ake.at/` | `scripts/deploy/ziele.psd1:9` |
| IIS-Site / App-Pool | **zu prüfen.** Installationsskript-Vorgaben: Site `IDEAL AKE WMS`, Pool `IDEALAKEWMS` (können beim Aufruf abweichen) | `scripts/deploy/Install-IdealAkeWms.ps1:54-55` |
| Dienst | Anzeigename `IDEAL AKE WMS Service`; interner Name **zu prüfen** (Installationsskript-Vorgabe `IDEALAKEWMSService`) | [[2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec]] Tabelle „Zielsysteme“, `scripts/deploy/Install-IdealAkeWms.ps1:56`, `:380` |
| Dienstkonto | ein AD-Konto für App-Pool **und** Dienst (Vorgabe bei der Neuinstallation); Kontoname **zu prüfen** | [[2026-11-04-ideal-produktiv-installation]] Tabelle „Dateien“, `scripts/deploy/Install-IdealAkeWms.ps1:148-159` |
| WMS-DB | `IDEAL_AKE_WMS` (Name fest für IDEAL-Skripte); SQL-Server **zu prüfen** | `scripts/deploy/Install-IdealAkeWms.ps1:178-180`, [[2026-11-04-ideal-produktiv-installation]] „Festgehaltene Entscheidungen“ |
| Sage | Mandant/Server **zu prüfen**; SData an IDEAL noch nicht freigegeben | `README.md:130-131`, [[2026-11-04-ideal-produktiv-installation]] „Offen“ |
| DataProtection | bei Installation Standardpfad `C:\ProgramData\IdealAkeWms\DataProtection-Keys` (offen: eigener Ordner) | [[2026-11-04-ideal-produktiv-installation]] „Offen“, `scripts/deploy/Install-IdealAkeWms.ps1:53` |
| Letzter bekannter Stand | v1.71.1 (Neuinstallation 2026-11-04) | [[2026-11-12-deploy-fahrplan-buendel-v1-99]] Abschnitt „IDEAL-Prod“ |

> **Widerspruch / zu prüfen:** Die Installation lief laut Brain auf `idealweb01.ideal.ideal-ake.at:80` (HTTP), `ziele.psd1` nennt
> `https://…/` (Port 443). Außerdem sind die Install-Vorgabepfade (`C:\inetpub\IDEALAKEWMS\Web`, `C:\Services\IDEALAKEWMSService`) andere
> als in `ziele.psd1`. Am Server mit `-NurPruefen` klären. (Quelle: [[2026-11-04-ideal-produktiv-installation]] „Offen“,
> `scripts/deploy/Install-IdealAkeWms.ps1:51-52`, `scripts/deploy/ziele.psd1:9`)

### 3.2 IDEAL – Test

| Merkmal | Wert | Beleg |
|---|---|---|
| Server | idealweb01 (gleicher Host wie Prod) | `scripts/deploy/ziele.psd1:10` |
| URL | `http://idealweb01.ideal.ideal-ake.at:88/` | `scripts/deploy/ziele.psd1:10` |
| IIS-Site / App-Pool | **zu prüfen** | – |
| Dienst | Anzeigename `IDEAL AKE WMS Service TEST` | [[2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec]] Tabelle „Zielsysteme“ |
| WMS-DB | SQL-Instanz `IdealSQL2\SAGE100TEST` (SQL Server 2019, Port 54723 – fest? zu prüfen); DB-Name laut DEV-Kopie `IDEAL_AKE_WMS_TEST` | [[2026-11-11-buendel-deploy-update-skript]] „Befund erster -NurPruefen“, [[2026-11-12-deploy-fahrplan-buendel-v1-99]] „Einmal je Server“, [[2026-10-29-dev-umgebung-lokal]] „Angelegte Objekte“ |
| Backup-Ordner (auf IDEALSQL2) | `E:\SQLBackup\WMS` (Standard-Backup-Pfad der Instanz existiert nicht) | [[2026-11-12-deploy-fahrplan-buendel-v1-99]] „Einmal je Server“, [[2026-11-11-buendel-deploy-update-skript]] 16:09 |
| Sage (lesend) | `idealsql2.ideal.ideal-ake.at\SAGE100TEST`, DB `IDEAL_TEST_2026_05_03` | [[2026-10-29-dev-umgebung-lokal]] „Nachtrag 2026-10-29“ |
| Ausführendes Konto (Deploy) | `ideal\t1_adminweg` (am 11.11. aus „Protected Users“ genommen; SPNs der Instanz offen) | [[2026-11-11-buendel-deploy-update-skript]] 16:05 |
| Dienstkonto | **zu prüfen** | – |
| Besonderheit | Fußzeile ist auf dem Server selbst nicht lesbar (Loopback) → Skript fragt nach der Version | [[2026-11-12-deploy-fahrplan-buendel-v1-99]] „IDEAL-Test“ |
| Letzter bekannter Stand | v1.98.1 (11.11.2026) | [[2026-11-12-deploy-fahrplan-buendel-v1-99]] „IDEAL-Test“ |

> Achtung Cookies: Prod und Test laufen auf demselben Hostnamen; Cookies unterscheiden keine Ports (Session-Cookie `IdealAkeWms.Session`).
> (Quelle: [[2026-11-04-ideal-produktiv-installation]] „Offen“, `IdealAkeWms/Program.cs:60`)

### 3.3 AKE – Prod

| Merkmal | Wert | Beleg |
|---|---|---|
| Server | akenet01 | `scripts/deploy/ziele.psd1:11` |
| URL | `https://akenet01.ake.at:4443/` | `scripts/deploy/ziele.psd1:11` |
| IIS-Site / App-Pool | **zu prüfen** | – |
| Dienst | Anzeigename `IDEAL AKE WMS Service` | [[2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec]] Tabelle „Zielsysteme“ |
| WMS-DB | `AKESQL20.ake.at` / `IDEAL_AKE_WMS` | `README.md:17`, [[fallstricke]] §8 „Lokaler dotnet run … gegen AKESQL20“ |
| Sage (lesend) | Repo-Vorgabe `AKESQL20.ake.at` / DB `ake` – Wert am Server **zu prüfen** | `IDEALAKEWMSService/appsettings.json:4`, `README.md:967` |
| OSEON | `aketrumpf01.ake.at\TRUMPFSQL2` / `T1000_V01_V001` | `IDEALAKEWMSService/appsettings.json:5` |
| enaio | `AKESQL20.ake.at` / `enaio` | `IDEALAKEWMSService/appsettings.json:6` |
| Dienstkonto, Backup-Ordner | **zu prüfen** (Backup-Ordner auf AKESQL20 „festlegen und mit `-BackupPfad` mitgeben“) | [[2026-11-12-deploy-fahrplan-buendel-v1-99]] „Einmal je Server“ |
| Letzter bekannter Stand | main v1.30.x; ob Hotfix v1.30.4 deployt ist, ist nicht vermerkt | [[2026-11-12-deploy-fahrplan-buendel-v1-99]] „AKE-Prod“ |

### 3.4 AKE – Test

| Merkmal | Wert | Beleg |
|---|---|---|
| Server / URL | akenet01, `https://akenet01.ake.at:4444/` | `scripts/deploy/ziele.psd1:12` |
| Dienst | Anzeigename `IDEAL AKE WMS Service TEST` | [[2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec]] Tabelle „Zielsysteme“ |
| WMS-DB | DB-Name laut DEV-Kopie `IDEAL_AKE_WMS_Test`; Server **zu prüfen** | [[2026-10-29-dev-umgebung-lokal]] „Angelegte Objekte“ |
| Sage | Sage-Test `ake_TEST2026` auf `AKESQL20.ake.at`; SData `https://sagetest01.ake.at:5493/`, Dataset `ake_TEST2026;1` (Stand 20.10.) | [[2026-10-29-dev-umgebung-lokal]] „Nachtrag 2026-10-29“, [[2026-10-20-ake-testsystem-bestandsaufnahme]] §2.2 |
| Besonderheit | TLS-Prüfung der Sage-Schnittstelle dort aus (nur Testsystem zulässig) | [[2026-10-20-ake-testsystem-bestandsaufnahme]] §6 Punkt 6 |
| Letzter bekannter Stand | v1.30.1 / v1.30.x (Schema bis SQL/88) | [[2026-10-20-ake-testsystem-bestandsaufnahme]] Kopf, [[2026-11-12-deploy-fahrplan-buendel-v1-99]] „AKE-Test“ |

### 3.5 AKE – Test-IDEAL

| Merkmal | Wert | Beleg |
|---|---|---|
| Server / URL | akenet01, `https://akenet01.ake.at:4445/` | `scripts/deploy/ziele.psd1:13` |
| Dienst | Anzeigename `IDEAL AKE WMS IDEAL-Test Service` | [[2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec]] Tabelle „Zielsysteme“ |
| WMS-DB | `AKESQL20.ake.at` / `IDEAL_AKE_WMS_TESTIDEAL` | [[2026-11-12-deploy-fahrplan-buendel-v1-99]] „AKE Test-IDEAL“ |
| Backup-Ordner | `F:\Backup_DB` | ebd. |
| Konto | `akenb\svc_idealakewms` (im Protokoll-Eintrag genannt; ob Dienst-/Pool-Konto oder ausführendes Konto: **zu prüfen**) | ebd. |
| Offen | Branding-Ordner, `appsettings.Production.json` (Sage-Zugang), Microsoft Edge (PDF) fehlen noch | ebd. |
| Letzter bekannter Stand | v1.99.12 (Volllauf 13.11.2026 08:54–09:01) | ebd. |

### 3.6 DEV (Entwickler-PC, je Standort)

| Merkmal | AKE | IDEAL | Beleg |
|---|---|---|---|
| URL / IIS-Site = App-Pool | `http://localhost:8081`, `AKE_WMS_DEV` | `http://localhost:8082`, `IDEAL_WMS_DEV` | [[2026-10-29-dev-umgebung-lokal]] „Angelegte Objekte“ |
| Dienst (Starttyp manuell) | `AKE_WMS_DEV_Service` | `IDEAL_WMS_DEV_Service` | ebd. |
| Konto (Pool und Dienst) | `AKENB\svc_idealakewms` (`db_owner` nur auf den DEV-DBs) | gleich | ebd. „Nachtrag 2026-10-30 (Dienstkonto)“, [[0018-qa-agent-deployt-und-testet-auf-lokaler-dev-umgebung]] |
| DB (lokale Standardinstanz `.`, Express) | `AKE_WMS_DEV` | `IDEAL_WMS_DEV` | [[2026-10-29-dev-umgebung-lokal]] „Angelegte Objekte“ |
| Umgebung | `DEV` (`appsettings.DEV.json`; `web.config` wird nach jedem Publish auf `DEV` gesetzt) | gleich | ebd. „Abweichungen vom Plan“ Punkt 2 |
| DataProtection-Schlüssel | `D:\Dev\AKE\keys` | `D:\Dev\IDEAL\keys` | ebd. „Angelegte Objekte“ |
| Wiederherstellung | `D:\akewms.bak` | `D:\idealwms.bak` | [[0018-qa-agent-deployt-und-testet-auf-lokaler-dev-umgebung]] „Folgen“ |

Regeln für DEV: nie an Sage buchen, keine echten Mails (Sage-Buchung, Syncs, Mail-Schalter aus). (Quelle: [[2026-10-29-dev-umgebung-lokal]]
„Entscheidungen“, [[0018-qa-agent-deployt-und-testet-auf-lokaler-dev-umgebung]] Entscheidung)

---

## 4. Konfigurationsdateien je System (Ablage)

| Datei | Ort | Inhalt | Beleg |
|---|---|---|---|
| `appsettings.json` | Web-Ordner und Dienst-Ordner | Verbindungen, Mail (Dienst), Logging; Repo-Vorgabe zeigt auf AKESQL20 | `IdealAkeWms/appsettings.json:1-40`, `IDEALAKEWMSService/appsettings.json:1-120` |
| `appsettings.Production.json` | Web-Ordner (nicht im Paket) | überschreibt die Repo-Verbindungen; ggf. `SageLagerbuchung`-Zugang (Rückfall) | `scripts/deploy/Install-IdealAkeWms.ps1:275-278`, `scripts/deploy/IdealAkeWms.Deploy.psm1:206-208` |
| `web.config` | Web-Ordner | `ASPNETCORE_ENVIRONMENT`, stdout-Log, Auth | `IdealAkeWms/web.config:5-18` |
| Registry `HKLM\SYSTEM\CurrentControlSet\Services\<Dienst>` Wert `Environment` | Dienst | `DOTNET_ENVIRONMENT` (nur wo gesetzt, z. B. DEV) | `scripts/deploy/Update-IdealAkeWms.ps1:194-196` |
| `Branding\` | Web-Ordner | Briefkopf-Vorlage + Schriften (nicht im Paket) | `README.md:575-583` |

Kennwörter (SQL, SMTP, SData) stehen nur in diesen Dateien bzw. verschlüsselt in der DB – hier nie Werte notieren, nur `<Kennwort>`.
Das Update-Skript überschreibt keine dieser Dateien. (Quelle: `scripts/deploy/Update-IdealAkeWms.ps1:632-639`, `README.md:518-540`)
Einzelheiten: [04-konfiguration.md](04-konfiguration.md), [09-sicherheit-konten-rechte.md](09-sicherheit-konten-rechte.md).

## 5. Deploy-Reihenfolge der Systeme (Bündel v1.99.x)

IDEAL-Test → AKE Test-IDEAL → AKE-Test → Abnahme und Merge nach main → IDEAL-Prod → AKE-Prod. Produktion erst nach dem Merge.
(Quelle: [[2026-11-12-deploy-fahrplan-buendel-v1-99]] „Reihenfolge“) Ablauf je System: [03-installation-update-rollback.md](03-installation-update-rollback.md).
