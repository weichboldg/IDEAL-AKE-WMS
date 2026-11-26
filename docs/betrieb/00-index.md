# 00 – Betriebshandbuch IdealAkeWms: Übersicht

**Stand:** v1.99.84, 2026-11-26 (v1.99.84: FA-Liste/FA-Struktur Nachbesserung, nur Web; v1.99.82/83: FA-Liste nur HauptFA und FA-Struktur je HauptFA eine Zeile, nur Web, kein SQL, keine Einstellung, Manifest-Infos `fa-liste-nur-hauptfa-info`/`fa-struktur-uebersicht-info`, Diagnose A-8.10; v1.99.81: BDE-Fix-Lauf ohne Migration; Kapitel 03 Fräsen-Pfad Format4 + SCHARF, Checkliste Programmordner-ZIP und Nachtrag-Sofortlauf, Zitat Prüfungen korrigiert, SQL/169 Audit-Felder; 07 „Gesendet“ nach Neustart; v1.99.80: Doku-Nachzug 1.99.70–1.99.79 in 03–09 und AGENTS.md, Versionszeilen 1.99.73/74/76/77/79 ergänzt; v1.99.79: Sammel-Fixes, kein SQL; Einmal-Skript Fräsen-Pfad-Erstimport bricht auf AKE ab, Kapitel 03 Tabelle 5.2a um `SQL/168` ergänzt; v1.99.78: Terminal-Seite abgelöst, `BdeTerminalMaxHauptFa` entfernt, BDE am Handy – kein SQL; v1.99.75: BDE-Teileliste Teilfertigmeldung, `SQL/169` vor dem Start von Web und Dienst; v1.99.72: Fräsen-Pfad + Lagerbestell-Positionen in der BDE-Teileliste, `SQL/164`/`SQL/165`, Einmal-Skript Erstimport, Rolle `fraesenpfad_pflegen`; v1.99.71: Lager-/Lagerplatz-Anlage in Sage per Direkt-SQL, beide Schalter aus, kein SQL; v1.99.70: Lauf „Kommissionier-Nachtrag“ + `SQL/168`; Bündel `feature/2026-08-07-ideal-teile-1-5`, Paket 17; v1.99.54–62 ohne Betriebsänderung (nur Web, keine Migration, keine neue Einstellung), v1.99.63 Update-Skript + `SQL/151` (nur IDEAL-Prod); Quelle: `IdealAkeWms/AppVersion.cs:5`; erstellt mit v1.99.23, zuletzt Kapitel 03 und 04 nachgezogen – siehe „Änderungen seit v1.99.23“) ·
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
- **Beim Erstellen gefundene Befunde** (Stand v1.99.23; inzwischen behoben):
  - **Sicherheit:** ~~Benutzer ohne hinterlegtes Kennwort können sich mit leerem Kennwort anmelden~~ – **behoben in v1.99.28** (Formular nur mit
    Kennwort, Erstkennwort mit Pflichtwechsel; [09 Abschnitt 4 und 8](09-sicherheit-konten-rechte.md)). Weitere Risiken in [09 Abschnitt 8](09-sicherheit-konten-rechte.md).
  - **Erstinstallation:** ~~`SQL/00_FreshInstall.sql` bricht bei den Indizes auf `ProductionOrders.IsReleasedForPicking`/`AssignedPickerId` ab~~ –
    **behoben in v1.99.28**; für die Erstinstallation vom 04.11.2026 (IDEAL-Prod) repariert seit v1.99.63 `Pruefung/FreshInstallIndizes` mit `SQL/151`
    ([03 Abschnitt 4](03-installation-update-rollback.md)).
  - **Widersprüchliche Quellen** (z. B. Pfade und Dienstname zwischen `scripts/deploy/ziele.psd1`, `README.md` und `Install-IdealAkeWms.ps1`) sind in den
    Kapiteln an der jeweiligen Stelle genannt. Maßgeblich für Updates ist `scripts/deploy/ziele.psd1` ([02 Abschnitt 1](02-systeme-umgebungen.md)).

## Änderungen seit v1.99.23 (für den Betrieb)

| Version | Änderung | Kapitel |
|---|---|---|
| 1.99.24 | nicht vergeben | – |
| 1.99.25 | Beschichtung: Quelle Kennzeichen/Termin einstellbar; Lackierteil-Erkennung im Import-Hook nur mit `Sync:CoatingDetectionEnabled` | [04](04-konfiguration.md) |
| 1.99.26 | FA-Struktur IDEAL über SData nur im Vergleich (gesperrt bis DPS-Freigabe), `orderBy` bei jeder SData-Abfrage, Stückliste live für beendete Haupt-FAs | [04](04-konfiguration.md), [05](05-schnittstellen.md) |
| 1.99.27 | als eigene Version nicht ausgeliefert; der Inhalt (Sage-Arbeitsgangnummer an den Arbeitsgang-Stammdaten, `SQL/153`) kam mit v1.99.32 | [03 Abschnitt 5.2a](03-installation-update-rollback.md) |
| 1.99.28 | Anmeldung nur mit Kennwort, Erstkennwort mit Pflichtwechsel, admin-Seed `wms-admin`, Secure-Cookies, Installer ohne SData-Zugang, FreshInstall repariert; Migration `SQL/152`; Prüfungen `BenutzerOhneKennwort`, `SmtpBenutzer`, `FreshInstallIndizes` | [03](03-installation-update-rollback.md), [07](07-troubleshooting.md), [08 A-7.5](08-diagnose-abfragen.md), [09](09-sicherheit-konten-rechte.md) |
| 1.99.29 | Wareneingang-CSV ohne Kopfzeile, Erfasser = sAMAccountName; Nachbesserung B-2/B-3 (Haken nur mit Kennwort, `/Users`-Hinweis) | [05 Abschnitt 8](05-schnittstellen.md), [07 Abschnitt 4](07-troubleshooting.md), [09 Abschnitt 4](09-sicherheit-konten-rechte.md) |
| 1.99.30 | Bestellvorschläge: letzte Bestellung ohne Artikel-Lieferant (`SQL/155`), Uhrzeiten `Sync:LetzteBestellungUhrzeitNacht`/`…Mittag`; `EinkaufOffeneBedarfe` wirkt auch in der Artikelinfo | [03 5.2a](03-installation-update-rollback.md), [04 5.2/5.5](04-konfiguration.md) |
| 1.99.31 | offene Bestellungen nach Sage-Wareneingang sofort neu lesen (`Sync:EinkaufBestellungenNachWeMinuten`) | [04 5.2](04-konfiguration.md), [05 Abschnitt 3](05-schnittstellen.md) |
| 1.99.32 | BDE-Teileliste (`SQL/154`), Sage-Arbeitsgangnummer an den Arbeitsgang-Stammdaten (`SQL/153`); Einmal-Skript IDEAL löscht Vater-Arbeitsgänge an Blatt-Teilen (DB-Backup) | [03 5.2a/5.4](03-installation-update-rollback.md), [04 5.2](04-konfiguration.md) |
| 1.99.33 | Wareneingang-CSV Sammellieferschein `<Barcode>_1.csv`; AB-Nummer/Kommission der offenen Bestellungen (`SQL/156`); Prüfung `WareneingangCsvKollision` | [05 Abschnitt 3 und 8](05-schnittstellen.md) |
| 1.99.34 | Beschichtung AKE: Positionstext (`SQL/157`), Prüfung `BeschichtungPositionOhneTextFlach` | [03 5.2a/5.3](03-installation-update-rollback.md) |
| 1.99.35 | Blechfertigung IDEAL: BF-Aufträge, WiCAM-Pool, Rolle `schachtler` (`SQL/158`); neue Einstellungen `BfCsv*`, `BfEmpfaengergruppeId`, `Sync:BlechteileFelderNachlesen` | [04 5.2/5.4/5.5](04-konfiguration.md), [05 Abschnitt 8a](05-schnittstellen.md), [08 A-8.2](08-diagnose-abfragen.md) |
| 1.99.36 | Beschichtung AKE Nachbesserung, BDE-Kontrast; keine Migration | – |
| 1.99.37 | FA-Struktur IDEAL als Abgleich statt Komplett-Ersetzen (`SQL/159`, **ändert Daten, Dienst gestoppt**), `Sync:FaStrukturAufbewahrungTage`; Prüfung `FaStrukturSchluesselDoppelt` | [03 5.2a/6](03-installation-update-rollback.md), [04 5.2](04-konfiguration.md), [05 Abschnitt 2](05-schnittstellen.md), [08 A-8.5](08-diagnose-abfragen.md) |
| 1.99.38 | Arbeitsgänge in Sage-FAs anlegen (`SQL/160`, neuer Sage-Schreibweg, Schalter `SageFaArbeitsgaengeAnlegenAktiv` bleibt aus) | [04 5.4](04-konfiguration.md), [05 Abschnitt 4a](05-schnittstellen.md), [08 A-8.1](08-diagnose-abfragen.md), [AGENTS.md](AGENTS.md) |
| 1.99.39 | Arbeitsgang-Anlage: Erfolg erst nach Nachlesen; Prüfung `ArtikelQuelleSData` (AKE); keine Migration | [05 Abschnitt 4a](05-schnittstellen.md) |
| 1.99.40 | 27 Sage-Benutzerfelder als Service-Einstellung `Sync:Userfeld:*`; IDEAL: Kreditoren-Mindestbestellwert leer setzen; keine Migration | [04 5.6](04-konfiguration.md), [08 A-8.6](08-diagnose-abfragen.md) |
| 1.99.41 | Beschichtungsauftrag: Kreditor, Versendet, Verpackt/ohne FA (`SQL/161`); Prüfungen `BeschichterKreditoren`, `BeschichtungsauftragUebergebenOffen` | [03 5.2a/5.3](03-installation-update-rollback.md) |
| 1.99.42 | WiCAM-CSV exakt nach `BF00000002.csv`, Benutzer-Kürzel (`SQL/162`), Arbeitsschritte je BF-Position (`SQL/163`, **ändert Daten**); Einmal-Skript Beschichter-Kreditor nachtragen; „Antwort unklar“ nie automatisch wiederholen | [03 5.2a/5.4](03-installation-update-rollback.md), [05 Abschnitt 4a und 8a](05-schnittstellen.md), [08 A-8.2/A-8.3](08-diagnose-abfragen.md) |
| 1.99.43 | Kommissionierliste IDEAL: nur nach Leitstand-Freigabe, Standard-Ziele K-01/K-02 (zwei neue AppSettings, 3 Prüfungen); kein SQL, Dienst nur Versionsnummer (Paket 8) | [03 Abschnitt 4](03-installation-update-rollback.md), [04 5.5](04-konfiguration.md), [08 A-8.4](08-diagnose-abfragen.md) |
| 1.99.44 | Sammel-Bugfix: `Pruefung/FaStrukturSchluesselDoppelt` nur mit `RTRIM` (führende Leerzeichen keine Dublette mehr); Reparatur-Name der Rolle „Beschichtungsauftrag“ ohne „(IDEAL)“; Menü „Empfänger“ auch bei Lager-/Glasbestellung und Blechfertigung; kein SQL, keine neue Einstellung | [03 5.3](03-installation-update-rollback.md), [04 5.5](04-konfiguration.md) |
| 1.99.45 | Sammellauf: Startseite und Menü aus einer Quelle (`Services/Navigation/NavigationKatalog.cs`, ADR 0034), OSEON AG-Übersicht nur mit `TeileverfolgungAktiv`; Artikelinfo-Bestand exakt; Update-Skript: alle Prüf-/Warntexte nach einem Muster, Exit-Code getrennt vom Ergebnis; zwei Info-Einträge in `nachlauf.psd1`; kein SQL, keine neue Einstellung | [03 Abschnitt 5](03-installation-update-rollback.md), [04 5.5](04-konfiguration.md), [07](07-troubleshooting.md) |
| 1.99.46 | Startseite: Kacheln wieder mit Symbolen (`NavZiel.SymbolPfad` in `NavigationKatalog.cs`); Web + Dienst nur Versionsnummer; kein SQL, keine Einstellung, keine Manifest-Einträge | – |
| 1.99.47 | Lagerplatz ausbuchen mit FA-Filter (Spalte FA, Serverprüfung, gemischte Zeilen, IDEAL Haupt-FA ⊇ Sub-FA); Web + Dienst nur Versionsnummer; kein SQL, keine Einstellung; Manifest: Info `v1.99.47-LagerplatzAusbuchenFaFilter` | – |
| 1.99.48/49 | reserviert, nicht ausgeliefert | – |
| 1.99.50 | Startseite: jedes Kachel-Symbol nur einmal (Symbolfamilien Rahmen/Blatt/Tropfen); kein SQL, keine Einstellung | – |
| 1.99.51 | Startseite: Reiter „Favoriten“, je Benutzer in der DB (Ansichts-Einstellung `Startseite.Favoriten`, `HomeController.cs:19`; „Ansichten zurücksetzen“ in `/Users` löscht sie mit); kein SQL, keine Einstellung; Manifest: Info `v1.99.51-StartseiteFavoriten` | – |
| 1.99.52 | Wareneingang zu Bestellung: Bestellnummer ohne Jahr, Fokus-Zeile, Zahnrad (Spalten, Ansicht `PurchaseOrderReceive`), Hauptlagerplatz als Ziel-Vorschlag mit Hinweistext; kein SQL, keine Einstellung | – |
| 1.99.53 | Lager-/Glasbestellung: Lieferung beim Abschließen ausbuchen. `SQL/166` (additiv: `StockMovements.WarehouseRequisitionItemId` NULL, FK, Index; keine Datenänderung); neue AppSetting `LagerbestellungVerbrauchsbuchungAktiv` (Standard **aus**). **Warnung:** Am Einschalttag endet die Handbuchung dieser Lieferungen in Sage – sonst doppelte Entnahme. Manifest: `Einstellung/LagerbestellungVerbrauchsbuchungAktiv`, `Info/Lagerbestellung-Entnahmeplatz`. SQL/164 und 165 sind reserviert, nicht vorhanden | [03 5.2a](03-installation-update-rollback.md), [04 5.5](04-konfiguration.md) |
| 1.99.59–1.99.62 | Nur Web, kein SQL, keine neue Einstellung, Dienst nur Versionsnummer (Paket 15 = 1.99.59, Paket 16 = 1.99.62). Anwender-sichtbar: Tabellenfarben, Versionsnummer auf der Anmeldeseite, Menü FA-Struktur. **1.99.61:** FA-Struktur (Menü, Kachel, `/FaHierarchy`) nur, wenn die bestehende Dienst-Einstellung `ProduktionsauftragHierarchisch` = `true` ist – bei AKE verschwindet der (leere) Punkt | [04](04-konfiguration.md) |
| 1.99.63 | Update-Skript: `SQL/151_RepairFreshInstallIndexes.sql` (additiv, keine Migration) legt auf **IDEAL-Prod** die 24 fehlenden Indizes der Erstinstallation an – Reparatur `Pruefung/FreshInstallIndizes` im Haltepunkt (vorher Abbruch in der Vorprüfung); neue Vorprüfung `Pruefung/FreshInstallIndizesDuplikate`; Kommissionierlisten-Prüfungen erst nach `SQL/159`; Abbruchtext der Vorprüfung korrigiert. Paket 17 | [03 Abschnitt 4](03-installation-update-rollback.md) |
| 1.99.66 | Nur Web, kein SQL, keine neue Einstellung. Seite „Sage-Buchungen“ (`/SageBookingQueue`) ersetzt „Sage-Lagerbuchungen“ und die Liste „Sage-Wareneingänge“; `/PurchaseOrders/SageWareneingaenge` und `/EinkaufPurchaseOrders/SageWareneingaenge` leiten dauerhaft um. Neuer Lesefilter `RequireStockReadOrEinkaufAccess`. Manifest: `v1.99.66-SageBuchungenInfo` | [06 §5/§6](06-logs-ueberwachung.md), [07](07-troubleshooting.md), [08 §4](08-diagnose-abfragen.md) |
| 1.99.68 | Web **und** Dienst. `SQL/167` (additiv: `StockMovements.Abteilung` NVARCHAR(200) NULL, kein Backfill; Migration `20261125134408_AddStockMovementAbteilung`); keine neue Einstellung. Sage-Lagerbuchung sendet je Zeile `Referenz` (Kennung Abteilung Benutzer, max. 31 Zeichen) und Zeilen-`Memo` (`WMS: SM#…# Benutzer Abteilung:…`), das Kopf-Memo bleibt unverändert (Doppelbuchungsschutz); Sage-Wareneingang: Kopf-Memo `WMS: WE#…# Benutzer` (Suche nach `WE#…#` trifft weiter). Dienst erst **nach** dem Schema-Schritt starten. Manifest: `SQL/167`, `v1.99.68-SageReferenzInfo` | [05](05-schnittstellen.md), [03](03-installation-update-rollback.md) |
| 1.99.70 | Neuer Dienst-Lauf „Kommissionier-Nachtrag“ (einmal je Tag, `Sync:KommissionierNachtragEnabled` an, `Sync:KommissionierNachtragUhrzeit` 2; erster Dienststart nach dem Update läuft sofort einmal, im DryRun nie). `SQL/168` (additiv: `ProductionOrderPickingStatus.StuecklisteKleiner` NVARCHAR(1000) NULL, kein Backfill). Leitstand-Knöpfe „Fehlende Kommissionieraufträge …“ nur noch Admin. Manifest: `v1.99.70-KommNachtrag-Lauf`, `v1.99.70-KommNachtrag-Knopf` | [04](04-konfiguration.md), [07](07-troubleshooting.md), [08 A-6.5](08-diagnose-abfragen.md) |
| 1.99.71 | Web **und** Dienst, kein SQL, keine Migration. Lager-/Lagerplatz-Anlage in Sage **per Direkt-SQL** über die bestehende `SageConnection` (einziger direkter Sage-Schreibweg, ADR 0036); neue Service-Einstellungen `SageLagerplatzAnlageAktiv` (aus) und `SageLagerAnlageLagertechniken` (`Aus`). Vor dem Einschalten: GRANT auf der Sage-Mandanten-DB, Prüfabfragen, Einschalt-Checkliste. Manifest: `v1.99.71-SageLagerplatzRechte`, `v1.99.71-SageLagerplatzAnlageAktiv`, `v1.99.71-SageLagerAnlageLagertechniken`, `Pruefung/LagerorteFuerSageAnlage` | [04 5.1](04-konfiguration.md), [05 § 4b](05-schnittstellen.md), [08 A-8.7](08-diagnose-abfragen.md), [09 § 2.1](09-sicherheit-konten-rechte.md) |
| 1.99.72 | Web **und** Dienst (Dienst nur `AppVersion`, Blechteile-Nachlese liest zusätzlich `USER_OSBlechdicke`). `SQL/164` (`Articles.CamPath`) und `SQL/165` (Werkbank-Option, `BdeTeilMeldungen.WarehouseRequisitionItemId`, Neuaufbau des UNIQUE-Index mit Filter `[IsCancelled] = 0 AND [WarehouseRequisitionItemId] IS NULL`, zweiter gefilterter UNIQUE) – Dienst und Web gestoppt. Einmal-Skript `2026-11-25_v1.99.72_FraesenPfad_Erstimport` (nur IDEAL, Trockenlauf/SCHARF). Neue Rolle `fraesenpfad_pflegen` (Prüfung mit Reparatur), neue AppSettings `FraesenPfadAktiv` (aus) und `BdeTeilelisteLagerbestellungArbeitstage` (10) | [03 5.2a](03-installation-update-rollback.md), [04 5.5](04-konfiguration.md), [09 § 5](09-sicherheit-konten-rechte.md) |
| 1.99.73 | Nur Web (Dienst nur `AppVersion`), kein SQL, keine Migration. BDE-Teileliste *Einzeln*: Spalte AG-Status, Baugruppen-Teile aufklappbar, Handy-Ansicht (Karten unter 992 px). Manifest-Info `v1.99.73-BdeTeilelisteAgStatusMobilInfo` (IDEAL). Browser-Cache am Terminal ggf. leeren. | [03](03-installation-update-rollback.md) |
| 1.99.74 | Nur Web (Dienst nur `AppVersion`), kein SQL. BDE-Teileliste *Summiert*: Aufträge aufklappbar, Summen-Haken. Manifest-Info `v1.99.74-BdeTeilelisteSummiertInfo` (IDEAL). | [03](03-installation-update-rollback.md) |
| 1.99.75 | Web **und** Dienst (Dienst nur `AppVersion`). `SQL/169` **vor dem Start von Web und Dienst**: `BdeTeilMeldungen.IstAbschliessend` (Bestand = 1), beide gefilterten UNIQUE neu mit `[IstAbschliessend] = 1`, `ProductionWorkplaces.BdeStandardsichtTeile` mit Vorbelegung (noch ohne Leser). Down nicht verlustfrei bei mehreren Meldungen je Zeile. Manifest: `SQL/169`, `v1.99.75-BdeTeilfertigInfo` | [03 5.2a](03-installation-update-rollback.md) |
| 1.99.76 | Web **und** Dienst (Dienst nur `AppVersion`), keine Migration – setzt `SQL/169` (1.99.75) voraus. BDE-Seite mit Reitern Teile/FA an beiden Standorten, Werkbank-Auswahl *BDE-Standardsicht*. `BdeNurFaMeldung` ist Altlast (Vorprüfung `Pruefung/BdeNurFaMeldungAn`), Nachher-Prüfung `Pruefung/BdeStandardsichtOhneTeileSicht` (IDEAL), Einstellung `v1.99.76-BdeStandardsichtEinstellung`. | [03](03-installation-update-rollback.md), [04](04-konfiguration.md) |
| 1.99.77 | Web **und** Dienst (Dienst nur `AppVersion`), keine DB-Änderung, keine neue Einstellung. Reiter FA: Meine Buchungen, Scan, Tätigkeit starten. Manifest-Info `v1.99.77-BdeFaSichtBuchungenInfo`. | [03](03-installation-update-rollback.md) |
| 1.99.78 | Nur Web (Dienst nur `AppVersion`), kein SQL, keine Migration. Terminal-Seite abgelöst: *BDE → Terminal* leitet auf die BDE-Seite, neue Seite Tageshistorie. AppSetting `BdeTerminalMaxHauptFa` entfernt – die DB-Zeile bleibt und erscheint unter `/Settings` als verwaist (nicht löschen nötig). Manifest: `v1.99.78-BdeTerminalAbloesungInfo`, `v1.99.78-BdeHandyKontenInfo` (Handy-Konten anlegen) | [09 § 4.1](09-sicherheit-konten-rechte.md) |
| 1.99.79 | Web **und** Dienst, kein SQL, keine Migration. Sammel-Fixes: Buchungen nur mit dem Terminal des angemeldeten Benutzers ([07](07-troubleshooting.md)), Lagerplatz-Konflikt 50014 nennt die belegende PlatzID ([05](05-schnittstellen.md)), Blechdicke ohne Einstellungszeile wird geleert, Einmal-Skript Fräsen-Pfad bricht auf AKE ab. Kein Manifest-Eintrag. | [03](03-installation-update-rollback.md), [07](07-troubleshooting.md) |
| 1.99.80 | Nur Doku und Hilfeseite (Web und Dienst nur `AppVersion`), kein SQL, keine Einstellung. Betriebshandbuch-Nachzug 1.99.70–1.99.79 (Kapitel 00, 03–09, AGENTS.md). | – |
| 1.99.81 | BDE-Fix-Lauf (Web): Wächter an allen Terminal-Aktionen, neue Seite *BDE-Buchungen › Hängende Pausen* (Abschließen nur BDE-Admin), keine Migration, keine neue Einstellung. `SQL/169` setzt bei der Erstanlage die Änderungsfelder; Einmal-Skript Fräsen-Pfad mit `…\CAM\Format4\` und im Prod-Fenster SCHARF; Manifest-Hinweis `BdeTerminalMaxHauptFa` „entfällt“. Kapitel 03 (Checkliste, Zitat Prüfungen), 04, 07 | [03](03-installation-update-rollback.md), [04](04-konfiguration.md), [07](07-troubleshooting.md) |
| 1.99.82 | FA-Liste IDEAL nur HauptFA-Zeilen, 17 Strukturfelder (Web; Dienst nur `AppVersion`); kein SQL, keine Einstellung; Manifest: Info `fa-liste-nur-hauptfa-info` (IDEAL) | [08 A-8.10](08-diagnose-abfragen.md) |
| 1.99.83 | FA-Struktur je HauptFA eine Zeile, Teile ausblenden (URL `mitTeilen`), Sticky-Spalten (Web; Dienst nur `AppVersion`); kein SQL, keine Einstellung; Manifest: Info `fa-struktur-uebersicht-info` (IDEAL) | [08 A-8.10](08-diagnose-abfragen.md) |
| 1.99.84 | FA-Liste ohne Zeichen HauptFA, FA-Struktur Spaltenfolge/Bezeichnung ausgeblendet (Web; Dienst nur `AppVersion`); kein SQL, keine Einstellung | – |

Anwender-Sicht: `docs/anwender/` – neu Kapitel 13 „Anmeldung und Benutzer“.

## Weitere Dokumente

- `README.md` (Repository-Wurzel): Installation, IIS, AppSettings – ausführliche Entwickler- und Betriebsdoku.
- `docs/anwender/`: Anleitungen für Anwender je Modul.
- `docs/TESTSZENARIEN.md`: Abnahme-Szenarien (TS-Nummern), auf die Kapitel 07/08 verweisen.
- Wissensbasis `secondbrain/` (Hauptcheckout): Entscheidungen (`architektur/adr/`), Stolperfallen (`architektur/fallstricke.md`), Codekarte (`codebase/`),
  Release-Historie (`changelog/`), Deploy-Notizen (`aufgaben/`).
