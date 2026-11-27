---
type: adr
id: 0022
title: Update je Zielsystem per Skript mit Nachlauf-Manifest
status: accepted
date: 2026-11-11
supersedes: ""
superseded_by: ""
---

> Spec [[2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec]] (v1.98.0), Freigabe Gerald 2026-11-11. Ergaenzt [[0004-migrations-und-sql-disziplin]] (Reihenfolge, Idempotenz) und
> [[0015-einmal-datenskripte-ausserhalb-der-nummerierten-sql-reihe]] (Einmal-Skripte laufen von Hand).

## Kontext und Problem

Der Buendel-Deploy geht auf fuenf Systeme (AKE Prod/Test/Test-IDEAL, IDEAL Prod/Test) mit je anderem Stand. Zwischen v1.31.0 und v1.98.0
liegen 25 Schema-Skripte, 2 Daten-, 3 Einmal-Skripte und rund 40 Hand-Schritte, verstreut ueber mehr als 60 Changelogs. Die README-Tabelle
endete bei SQL/80, die Dienst-Anleitung kannte nur einen Ordner. Ordner und Rechte (App_Data der Fotos, logs, DataProtection) standen
nur als Prosa in den Changelogs.

## Entscheidung

- **Ein Skript je Zielsystem** (`scripts/deploy/Update-IdealAkeWms.ps1`, Windows PowerShell 5.1, Administrator, nur von Hand): ermittelt
  Site/App-Pool ueber den physischen Pfad, den Dienst ueber den Exe-Pfad, die DB aus `appsettings*.json` der Ziel-Umgebung (web.config bzw.
  Dienst-Registry). Zielsysteme in `scripts/deploy/ziele.psd1`.
- **Schema automatisch, aber mit Bestaetigung je Skript**, in Manifest-Reihenfolge (nie Dateisortierung), `sqlcmd -b`; fehlt danach eine
  MigrationId, wird nichts kopiert und nichts gestartet (sonst spielte `Migrate()` die Migration ohne Vorbedingungen ein).
- **Daten- und Einmal-Skripte nie scharf** durch das Skript; hoechstens Trockenlauf, und nur wenn die Datei woertlich
  `DECLARE @Trockenlauf BIT = 1;` enthaelt. Haltepunkt (Altbestand v1.88.0): weiter nur mit „ausgefuehrt“, sonst Abbruch bei gestopptem Web.
- **Manifest-Pflicht:** `scripts/deploy/nachlauf.psd1` traegt jeden Deploy-Schritt (Schema, Daten, Einmalig, Einstellung, Leitstand, Info,
  Ordner). Spec-Vorlage, spec-agent und qa-agent verlangen den Eintrag; `NachlaufManifestDriftTests` prueft SQL-Dateien und Migrationen
  maschinell.
- **Ordner und Rechte** prueft eine gemeinsame Funktion (`Test-OrdnerUndRechte`, Modul `IdealAkeWms.Deploy.psm1`) bei Update und
  Installation, ueber die wirksamen Rechte; gesetzt wird nur hinzufuegend nach einer gesammelten Bestaetigung, die vorherige ACL kommt ins Log.
- **Paket = `<worktree>\publish\`** (Web, Dienst, `SQL\`, `deploy\`, `publish-info.txt` im Format `Schluessel: Wert`). Kopiert wird ohne
  Loeschen; `appsettings*.json`, `web.config`, `Branding\`, `App_Data\`, `wwwroot\Fotos\`, `logs\` werden nie ueberschrieben.

## Begruendung

Das Skript macht die Hand-Schritte vollstaendig und in richtiger Reihenfolge sichtbar, ohne die Verantwortung fuer Einmaliges vom Menschen
wegzunehmen (ADR 0015). Ermittlung statt fester Namen, weil die Systeme nicht einheitlich benannt sind (Test-IDEAL, README-Abweichungen).

## Konsequenzen

- Jede Spec mit Deploy-Schritt traegt einen Manifest-Eintrag; ein vergessener SQL-Eintrag faellt im Dev-Lauf auf (Drift-Test), ein
  vergessener Hand-Schritt nur in der QA.
- Ob ein Einmal-Skript gelaufen ist, weiss das Skript nur ueber Spur-Abfragen; eine Marker-Tabelle ist Folge-Spec (Freigabe-Antwort 1).
- `sqlcmd` muss auf den Web-Servern installiert sein (ODBC-Variante).

## Nachtrag 2026-11-11 (v1.99.0)

Teilweise abgeloest durch [[0023-daten-und-einmal-skripte-fuehrt-das-update-skript-mit-bestaetigung-aus]]: „Daten- und Einmal-Skripte nie scharf durch das Skript“ gilt nicht mehr; Ausfuehrung nur nach Eingabe am Zielserver, Marker `dbo.DeployScriptRuns` (SQL/142), neuer Manifest-Typ `Pruefung`, Phase `VorStart`.
