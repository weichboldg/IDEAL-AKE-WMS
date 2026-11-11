---
type: aufgabe
title: "Neuinstallation IDEAL-Produktivsystem (Bündel v1.71.1) — Installationsskript + SQL"
created: 2026-11-04
status: offen
---

# Neuinstallation IDEAL-Produktivsystem

Auftrag Gerald 2026-11-04: Installationsskript für das IDEAL-Produktivsystem. Die Datenbank legt Gerald vorab leer an.
Erstellt von der Koordination (Cowork). Die Dateien wurden im Chat übergeben und liegen **nicht** im Repo. Ablage unter
`scripts\install\` per Claude Code, falls gewünscht (die Koordination schreibt nicht außerhalb von `secondbrain\`).

## Dateien

| Datei | Inhalt |
|---|---|
| `IDEAL-Prod-1-Datenbank.sql` | Teil 1, Schema. Zuerst Prüfungen: DB `IDEAL_AKE_WMS` vorhanden, Kollation `_CI_`, leer. Abbruch per `SET NOEXEC ON` greift auch in SSMS. Dann `READ_COMMITTED_SNAPSHOT ON`, dann `SQL/00_FreshInstall.sql` des Bündels ab Abschnitt „1. Users“ **unverändert** (ohne `CREATE DATABASE`). 75 Migrationen bis `20261019092105_…`. |
| `IDEAL-Prod-2-Konfiguration.sql` | Teil 2, IDEAL-Werte für AppSettings/ServiceSettings und die Rollen `beschichtungsauftrag`/`_schreiben`. Läuft erst nach dem ersten Web-Start; das prüft das Skript an den Katalog-Schlüsseln. Wiederholbar, löscht nichts. Parameter oben im Skript. |
| `Install-IdealAkeWms-IDEAL.ps1` | IIS: Hosting Bundle und Features prüfen, Auth-Abschnitte entsperren, App-Pool (No Managed Code, AlwaysRunning, Idle 0), Site mit SNI-HTTPS, Preload. Web-Dateien plus `appsettings.Production.json`. Dienst-Dateien plus `appsettings.json`, `New-Service` (nicht gestartet), Rechte, optional sqlcmd für Teil 1/2. Der App-Pool bleibt gestoppt, bis das Schema da ist. Seit 2026-11-04 (Wunsch Gerald) fragt es **ein** AD-Konto ab und verwendet es für App-Pool **und** Dienst (Vorgabe Gerald). SMTP und Sage-SData bleiben optionale Parameter. Das Konto wird gegen das AD geprüft. Danach fragt es die Pfade für Web, Dienst und DataProtection ab (Enter = Standard; per Parameter übergeben = keine Abfrage). SSL ist keine Pflicht (Vorgabe Gerald): ohne `-CertThumbprint` nur HTTP, Port 80 wenn kein `-HttpPort`, mit Warnung (Kamera-Scan braucht HTTPS). Für das Dienstkonto setzt es das Recht „Als Dienst anmelden“. Abschaltbar mit `-OhneAbfrage`. |

## Reihenfolge

1. DB `IDEAL_AKE_WMS` leer anlegen (`COLLATE Latin1_General_CI_AS`).
2. Web und Dienst aus dem Bündel publishen (Befehle laut `qa-agent.md`).
3. Teil 1.
4. Skript (Web, IIS, Dienst registrieren).
5. Web-Start (Migrate ohne offene Migrationen plus Seeds).
6. Teil 2.
7. Dienst starten; der erste Zyklus läuft mit DryRun.
8. DryRun aus.
9. Struktur-Import prüfen.
10. Backup, dann `/HierarchieUmstellung`.
11. Werkbank-Anlage, Arbeitsgang-Abgleich und Arbeitsgang-Erkennung, jeweils mit DryRun.

## Festgehaltene Entscheidungen und Belege

- `Sync:ProductionOrdersEnabled = false` am IDEAL-Standort ([[2026-08-07-ideal-teile-1-5]], UAT-Runde 1). **Widerspruch:**
  idealweb01 zeigte am 20.10.2026 `AN` ([[2026-10-20-ake-testsystem-bestandsaufnahme]] §2.2). Ungeklärt; Teil 2 setzt `false`.
- RCSI ist Pflicht und wird von keinem Repo-Skript gesetzt. Teil 1 setzt es.
- `00_FreshInstall.sql` hat den DB-Namen `IDEAL_AKE_WMS` fest eingebaut und legt sonst eine zweite DB an. Teil 1 ersetzt nur diesen
  Kopf.
- Die Rollen `beschichtungsauftrag`/`beschichtungsauftrag_schreiben` haben keinen Seed (README Z. 419). Teil 2 legt sie an
  (`IsSystem = 0`, wie beim Anlegen über `/Roles`).
- `Sync:ArticlesViewName = [vw_IDEAL_AKE_WMS_Artikel]` vor dem ersten Lauf. Damit entstehen keine 00er-Artikel, und das
  Einmal-Skript `2026-10-15_…Bereinigung00erArtikel` entfällt. Vorher P4 aus [[2026-10-14-ideal-sage-views-umstellung-spec]].
- `admin` hat nach der Installation ein leeres Passwort (FreshInstall und `Program.cs`).

## Offen / ungeprüft

- [ ] PowerShell-Syntax nicht maschinell geprüft (kein PowerShell in der Cowork-Umgebung). Vor dem Lauf auf dem Server:
      `[System.Management.Automation.Language.Parser]::ParseFile('…\Install-IdealAkeWms-IDEAL.ps1',[ref]$null,[ref]$e); $e`
- [x] Teil 1 am 2026-11-04 11:25 auf IDEAL ausgeführt (Gerald). Ein Fehler: `Meldung 207 … Ungültiger Spaltenname "AssignedPickerId"`
      in Abschnitt „15. Indexes“. Ursache im Bündel-`00_FreshInstall.sql`: `IX_ProductionOrders_IsReleasedForPicking_IsDone` und
      `IX_ProductionOrders_AssignedPickerId` verweisen auf Spalten, die in `ProductionOrders` nicht (mehr) stehen — sie liegen in
      `ProductionOrderPickingStatus` (dort mit eigenen Indizes). Betrifft jede Neuinstallation aus `00_FreshInstall.sql` → Fix per /dev.
      Vollständigkeit der übrigen Indizes, Migrationen und RCSI per Prüfabfrage offen.
- [ ] Installationsskript-Lauf 2026-11-04: Site auf `idealweb01.ideal.ideal-ake.at:80` — gleicher Host wie das IDEAL-Testsystem (:88).
      Cookies gelten je Host, nicht je Port (Session-Cookie `IdealAkeWms.Session` in beiden gleich); DataProtection-Pfad ist der
      Standardpfad. Klären: eigener Hostname und eigener DataProtection-Ordner für Produktiv.
- [ ] Werte für IDEAL noch offen: SData (`SData:BaseUrl`, `SData:Dataset`), Sage-Mandant der Werkbank-Anlage, `BdeAktiv`,
      AD-Gruppe für die Windows-Anmeldung, enaio.
- [ ] README des Bündels: Update-Tabelle ohne 81–94 und 96; kein Web-Publish-Befehl; kein Abschnitt zu DataProtection,
      Bindungen und Zertifikat.
- [ ] Dienstname `IDEALAKEWMSService` (README) gegenüber `ServiceName = "IDEAL AKE WMS Service"` (`IDEALAKEWMSService\Program.cs`):
      Wirkung ungeprüft.
- [ ] Produktivinstallation aus dem nicht gemergten Bündel: Schranke 2, Entscheidung Gerald.
