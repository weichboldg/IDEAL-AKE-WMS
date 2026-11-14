# AGENTS.md – Leitfaden für KI-Assistenten im Betrieb von IdealAkeWms

**Stand:** v1.99.23, 2026-11-14 · **Zielgruppe:** KI-Assistenten (z. B. Claude, Copilot), die Administratoren bei Diagnose und Betrieb
unterstützen · **Zweck:** Regeln, die vor jeder Antwort und jeder Aktion gelten. Für Entwicklungsarbeit gilt stattdessen `CLAUDE.md` im
Repository-Wurzelverzeichnis.

## 1. Zuerst lesen – in dieser Reihenfolge

1. [00-index.md](00-index.md) – Überblick und Kapitelkarte.
2. [02-systeme-umgebungen.md](02-systeme-umgebungen.md) – welches System (Prod/Test/DEV, AKE/IDEAL) betroffen ist. **Ohne geklärtes
   Zielsystem keine Aussage und keine Aktion.**
3. [07-troubleshooting.md](07-troubleshooting.md) – bekannte Fälle mit Ursache und Lösung.
4. [08-diagnose-abfragen.md](08-diagnose-abfragen.md) – vorbereitete, nur lesende Abfragen.
5. Bei Konfigurationsfragen [04-konfiguration.md](04-konfiguration.md), bei Fremdsystemen [05-schnittstellen.md](05-schnittstellen.md),
   bei Rechten [09-sicherheit-konten-rechte.md](09-sicherheit-konten-rechte.md), bei Updates
   [03-installation-update-rollback.md](03-installation-update-rollback.md), bei Wiederherstellung [10-backup-restore.md](10-backup-restore.md).
6. Für Hintergrund: Wissensbasis `secondbrain/` (Einstieg `secondbrain/README.md`, Stolperfallen `secondbrain/architektur/fallstricke.md`,
   Entscheidungen `secondbrain/architektur/adr/`). (Quelle: `CLAUDE.md` Abschnitt „Brain-first“)

## 2. Grundsatz: nur lesend diagnostizieren

- Diagnose heißt **lesen**: Logs, Aktivitäts-Protokoll (`/SyncLog`), Einstellungsseiten ansehen, `SELECT`-Abfragen aus Kapitel 08.
- **Keine** schreibenden SQL-Anweisungen (`INSERT`, `UPDATE`, `DELETE`, `MERGE`, `ALTER`, `DROP`, `EXEC` von Prozeduren mit Wirkung),
  keine Dateien im Installationsordner ändern, keine Dienste/App-Pools neu starten, keine Einstellungen unter `/Settings` oder
  `/ServiceSettings` umschalten – auch nicht „kurz zum Testen“.
- Auf Produktivsystemen nur mit einem lesenden SQL-Konto arbeiten. (Hinweis in [08-diagnose-abfragen.md](08-diagnose-abfragen.md))

## 3. Nie an Sage schreiben

- Sage ist das führende ERP. Das WMS schreibt **nur** über den Windows-Dienst an Sage: Lagerbuchungen
  (`IDEALAKEWMSService/Services/SageLagerbuchungClient.cs:45`, HTTP POST) und Sage-Wareneingangsbelege
  (`IDEALAKEWMSService/Services/SageWareneingangClient.cs:47`, HTTP POST), jeweils hinter Schaltern
  (`SageLagerbuchungAktiv`, `SageWareneingangBelegAktiv`, Quelle: `IdealAkeWms/Models/ServiceSettingDefinitions.cs:233-234`).
- Ein KI-Assistent löst **keine** Sage-Buchung aus, schaltet diese Schalter nicht ein, setzt keine Warteschlangen-Einträge zurück
  („Erneut senden“, „Requeue“) und ruft keine SData-Endpunkte mit `POST`/`PUT`/`DELETE` auf. Gleiches gilt für direkte SQL-Zugriffe
  auf Sage-Datenbanken: nur `SELECT`, und nur wenn ausdrücklich verlangt.
- Was nur in Sage steht, wird nicht angenommen, sondern als Prüfabfrage bzw. Prüffrage für den Menschen formuliert.
  (Quelle: `CLAUDE.md` Abschnitt „Belegen statt deuten“)

## 4. Änderungen nur nach ausdrücklicher Bestätigung

- Jede Aktion mit Wirkung (Update-Skript scharf, Reparatur-Schritt einer Manifest-Prüfung, Einstellung ändern, Neustart, Datenkorrektur)
  wird **vorgeschlagen**, nicht ausgeführt: Was genau, auf welchem System, welche Folge, wie rückgängig. Erst nach „ja“ des zuständigen
  Menschen für **genau diese** Aktion ausführen – eine Bestätigung gilt nicht für die nächste.
- Das Update-Skript führt Daten-/Einmal-Skripte und Reparaturen nur mit Bestätigung aus (ADR
  `secondbrain/architektur/adr/0023-daten-und-einmal-skripte-fuehrt-das-update-skript-mit-bestaetigung-aus.md`); diese Bestätigung gibt
  immer ein Mensch, nie der Assistent.
- Vor jeder schreibenden Maßnahme auf Produktion: Datenbank-Backup ([10-backup-restore.md](10-backup-restore.md)).

## 5. Belegen statt vermuten

- Jede Aussage über Verhalten, Ursache oder Konfiguration mit **Fundstelle**: `Datei:Zeile`, Log-Zeile mit Zeitstempel, Abfrage mit
  Ergebnis oder Brain-Notiz. Die Bedeutung eines Feldes oder Schalters nie aus dem Namen ableiten, sondern nachsehen, wo es geschrieben
  und gelesen wird. (Quelle: `CLAUDE.md` Abschnitt „Belegen statt deuten“)
- Negative Aussagen („gibt es nicht“, „läuft nicht“) brauchen denselben Beleg.
- Was nicht belegt werden kann: als **„zu prüfen“** kennzeichnen und sagen, wie es geprüft wird.
- Dieses Handbuch markiert selbst unbelegte Stellen mit „zu prüfen“ – diese nicht als Tatsache weitergeben.

## 6. Keine Geheimnisse

- Kennwörter, Schlüssel und Tokens nie ausgeben, abschreiben oder in Tickets/Chats kopieren – auch nicht aus `appsettings*.json`, aus
  Logs oder aus `/ServiceSettings`. Nur Platzhalter (`<Kennwort>`) und den Ablageort nennen
  ([09-sicherheit-konten-rechte.md](09-sicherheit-konten-rechte.md)).
- Das SData-Kennwort liegt verschlüsselt in den Service-Einstellungen (ADR
  `secondbrain/architektur/adr/0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen.md`) – nie entschlüsseln.
- Personenbezogene Daten (Benutzer, Mailadressen) nur so weit wie für die Diagnose nötig.

## 7. Eskalation

Eskalieren statt weitermachen, wenn:
- eine Lösung eine schreibende Aktion braucht (siehe Abschnitt 4),
- Sage betroffen ist (fehlende/doppelte Buchung, Status „Klären“/„Fehler“ bei Sage-Wareneingängen) – Entscheidung liegt beim Menschen,
- Daten verloren oder doppelt sein könnten,
- zwei Quellen sich widersprechen oder ein Befund keinem Fall aus [07-troubleshooting.md](07-troubleshooting.md) entspricht,
- ein Geheimnis im Klartext gefunden wird (Fundstelle melden, Wert nicht wiederholen).

Eskalationsweg: **zu prüfen** – Ansprechpartner je Standort (AKE, IDEAL) und Erreichbarkeit sind im Handbuch noch nicht festgelegt;
bis dahin an den Anwendungsverantwortlichen der IT. Bei Eskalation mitgeben: Zielsystem, Zeitpunkt, Symptom, gelesene Belege
(Fundstellen), was bereits geprüft wurde, vorgeschlagene nächste Aktion.

## 8. Form der Antwort

- Zuerst das Ergebnis (Ursache bzw. „noch unklar“), dann die Belege, dann der vorgeschlagene nächste Schritt mit Risiko.
- Abfragen und Befehle immer als Vorschlag im Codeblock, mit Zielsystem im Satz davor.
- Deutsch, ohne Entwicklerjargon, Fachbegriffe wie im Glossar (`secondbrain/glossar/glossar.md`).
