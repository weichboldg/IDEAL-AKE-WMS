# 00 – Betriebshandbuch IdealAkeWms: Übersicht

**Stand:** v1.99.23, 2026-11-14 (Bündel `feature/2026-08-07-ideal-teile-1-5`, Code `08672fcc`; Quelle: `IdealAkeWms/AppVersion.cs:5`) ·
**Zielgruppe:** Administratoren und IT-Kollegen ohne Entwicklerwissen · **Zweck:** Betrieb, Update, Überwachung und Fehlersuche des
Warehouse-, Produktions- und Betriebsdatensystems der Standorte AKE und IDEAL – mit Fundstelle zu jeder Aussage.

## So ist das Handbuch aufgebaut

| Kapitel | Inhalt | Wann lesen |
|---|---|---|
| [01 – Architektur und Technologie](01-architektur-technologie.md) | Web-App, Windows-Dienst, Datenbank, Fremdsysteme; Tech-Stack; Datenflüsse als Diagramme; Hintergrundjobs | Einarbeitung |
| [02 – Systeme und Umgebungen](02-systeme-umgebungen.md) | Steckbrief je System (Prod/Test/DEV, AKE/IDEAL): Server, URL, Pfade, Dienst, Datenbank | vor jeder Arbeit: „welches System?“ |
| [03 – Installation, Update und Rückfall](03-installation-update-rollback.md) | Erstinstallation, Update-Skript mit Prüfungen und Haltepunkten, Rollback, Checkliste | vor jedem Update |
| [04 – Konfiguration](04-konfiguration.md) | appsettings-Dateien vs. ServiceSettings vs. AppSettings; betriebskritische Schalter mit Wirkung | Einstellungen prüfen/ändern |
| [05 – Schnittstellen](05-schnittstellen.md) | Sage (SQL, SData lesend/schreibend), OSEON, enaio/Vault, Mail, CSV/UNC, AD, Windows-Anmeldung – mit benötigten Rechten | Fremdsystem-Störung |
| [06 – Logs und Überwachung](06-logs-ueberwachung.md) | Log-Dateien, Ereignisanzeige, Aktivitäts-Protokoll, Sage-Warteschlangen, tägliche/wöchentliche Prüfungen | Routine, Störung |
| [07 – Troubleshooting](07-troubleshooting.md) | Tabelle Symptom → Ursache → Prüfung → Lösung (belegte Fälle), Eskalationsweg | bei jeder Störung zuerst |
| [08 – Diagnose-Abfragen](08-diagnose-abfragen.md) | nur lesende SQL-Abfragen mit erwartetem Ergebnis | Diagnose |
| [09 – Sicherheit, Konten und Rechte](09-sicherheit-konten-rechte.md) | Dienstkonten, SQL- und Ordnerrechte, Anmeldung, Rollen, Ablage der Geheimnisse, bekannte Risiken | Einrichtung, Audit |
| [10 – Backup und Wiederherstellung](10-backup-restore.md) | was zu sichern ist, was das Update-Skript sichert, Restore-Ablauf | vor Updates, im Notfall |
| [AGENTS.md](AGENTS.md) | Regeln für KI-Assistenten: nur lesend diagnostizieren, nie an Sage schreiben, Änderungen nur nach Bestätigung | wenn eine KI mitarbeitet |

## Schnelleinstieg

| Aufgabe | Weg |
|---|---|
| Seite lädt nicht / Fehler 500 | [07 Abschnitt 1](07-troubleshooting.md) → [06 Abschnitt 3](06-logs-ueberwachung.md) (Ereignisanzeige, stdout-Log) |
| Anmeldung klappt nicht | [07 Abschnitt 2](07-troubleshooting.md), Hintergrund [09 Abschnitt 4](09-sicherheit-konten-rechte.md) |
| Daten aus Sage kommen nicht an | [06 Abschnitt 4](06-logs-ueberwachung.md) (`/SyncLog`) → [08 Abschnitt 2](08-diagnose-abfragen.md) → [05](05-schnittstellen.md) |
| Buchungen gehen nicht an Sage | [06 Abschnitte 5–6](06-logs-ueberwachung.md) → [07 Abschnitt 3](07-troubleshooting.md) |
| Update einspielen | [03 Abschnitt 5 und 8](03-installation-update-rollback.md), vorher [10](10-backup-restore.md) |
| Update zurücknehmen | [03 Abschnitt 6](03-installation-update-rollback.md) – Datenbank-Migrationen laufen nur vorwärts |
| Welche Version läuft? | Fußzeile der Web-App; [08 Abschnitt 1](08-diagnose-abfragen.md) |

## Grundregeln für den Betrieb

1. **Erst das Zielsystem klären** ([02](02-systeme-umgebungen.md)) – DEV, Test und Prod haben eigene Datenbanken und Schalter.
2. **Diagnose ist lesend.** Schreibende Eingriffe (SQL, Einstellungen, Neustart) nur bewusst, mit Backup und möglichst nach dem Update-Skript bzw.
   einer dokumentierten Lösung aus [07](07-troubleshooting.md).
3. **Sage ist führend.** Was das WMS an Sage sendet, steuern Schalter in `/ServiceSettings` ([04](04-konfiguration.md), [05](05-schnittstellen.md));
   nicht „zum Testen“ einschalten.
4. **Keine Geheimnisse in Tickets, Mails oder diesem Handbuch** – nur Platzhalter `<Kennwort>` und den Ablageort ([09](09-sicherheit-konten-rechte.md)).

## Belege und offene Punkte

- Jede Aussage trägt ihre Quelle: `Pfad:Zeile` (relativ zum Repository) oder eine Notiz der Wissensbasis `secondbrain/` als `[[Notizname]]`.
  Beim Erstellen wurden alle Datei-Fundstellen maschinell geprüft (Datei vorhanden, Zeile im Bereich; Stand 2026-11-14).
- Was nicht belegt werden konnte, steht als **„zu prüfen“** im Text (rund 60 Stellen). Die häufigsten offenen Angaben: IIS-Site/App-Pool,
  Dienstname und Dienstkonto je Prod-/Testsystem ([02](02-systeme-umgebungen.md)), Datenbank-Wartungsplan ([10](10-backup-restore.md)),
  Empfänger der Fehlermail und Eskalationsweg ([06](06-logs-ueberwachung.md), [07](07-troubleshooting.md)), Rechte des Dienstkontos in der Sage-DB
  ([05](05-schnittstellen.md)). Diese Angaben lassen sich am Server ablesen (z. B. Update-Skript mit `-NurPruefen`, IIS-Manager, `services.msc`).
- **Beim Erstellen gefundene Befunde** (nicht behoben, an die Entwicklung gemeldet):
  - **Sicherheit:** Benutzer ohne hinterlegtes Kennwort – darunter alle aus dem AD angelegten – können sich über das Anmeldeformular mit **leerem
    Kennwort** anmelden (Quelle: `IdealAkeWms/Controllers/AccountController.cs:94-101`, `IdealAkeWms/Controllers/UsersController.cs:199-209`).
    Weitere Risiken in [09 Abschnitt 8](09-sicherheit-konten-rechte.md).
  - **Erstinstallation:** `SQL/00_FreshInstall.sql:1260-1263` legt Indizes auf `ProductionOrders.IsReleasedForPicking`/`AssignedPickerId` an; diese
    Spalten gibt es in `ProductionOrders` nicht (Tabelle ab `SQL/00_FreshInstall.sql:388`). Eine Neuinstallation bricht dort ab – siehe
    [03 Abschnitt 4](03-installation-update-rollback.md).
  - **Widersprüchliche Quellen** (z. B. Pfade und Dienstname zwischen `scripts/deploy/ziele.psd1`, `README.md` und `Install-IdealAkeWms.ps1`) sind in den
    Kapiteln an der jeweiligen Stelle genannt. Maßgeblich für Updates ist `scripts/deploy/ziele.psd1` ([02 Abschnitt 1](02-systeme-umgebungen.md)).

## Weitere Dokumente

- `README.md` (Repository-Wurzel): Installation, IIS, AppSettings – ausführliche Entwickler- und Betriebsdoku.
- `docs/anwender/`: Anleitungen für Anwender je Modul.
- `docs/TESTSZENARIEN.md`: Abnahme-Szenarien (TS-Nummern), auf die Kapitel 07/08 verweisen.
- Wissensbasis `secondbrain/` (Hauptcheckout): Entscheidungen (`architektur/adr/`), Stolperfallen (`architektur/fallstricke.md`), Codekarte (`codebase/`),
  Release-Historie (`changelog/`), Deploy-Notizen (`aufgaben/`).
