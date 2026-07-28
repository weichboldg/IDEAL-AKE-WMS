---
type: adr
id: 0008
title: Service-Konfiguration ist DB-first ueber einen typisierten Katalog, appsettings nur Default-Referenz
status: accepted
date: 2026-07-08
supersedes: ""
superseded_by: ""
---

> **Nachtraeglich erfasst** (2026-07-27). Bestandsdokumentation; eingefuehrt in v1.25.0
> (Spec `../../../docs/superpowers/specs/2026-07-08-service-settings-typed-catalog-design.md`) und im
> Followup auf saemtliche Sync-Gates ausgedehnt.

## Kontext und Problem

Der Windows-Service fuehrt rund 20 Sync-Bloecke, jeder mit eigenem Enable-Schalter, Intervall und
Caps. Bisher lagen diese Werte in `appsettings.json` des Service-Projekts. Folge: jede Aenderung
war ein Datei-Eingriff auf dem Server mit Service-Restart, es gab keine Uebersicht, welche Keys
ueberhaupt existieren, und die `/ServiceSettings`-Seite in der Web-App zeigte nur einen Teil
davon — Toggles auf der Seite wirkten teils gar nicht, weil der Service den Wert weiterhin aus
`IConfiguration` las. Das war die gefaehrlichste Variante: eine UI, die Kontrolle vortaeuscht.

## Betrachtete Optionen

- **appsettings bleibt fuehrend, UI nur anzeigen** — ehrlich, aber jede Aenderung bleibt ein
  Server-Eingriff.
- **UI schreibt appsettings.json** — kein zweiter Speicherort, aber Schreibzugriff auf
  Programmdateien und Service-Restart pro Aenderung.
- **DB ist fuehrend, appsettings nur Default-Referenz, Keys aus einem Katalog** — ein zweiter
  Speicherort, dafuer Aenderung im Betrieb, typisierte UI und ein Drift-Guard.

## Entscheidung

- **Single Source of Truth der Keys** ist der Katalog
  `../../../IdealAkeWms/Models/ServiceSettingDefinitions.cs` (`.All`) — pro Key: Typ (Bool/Int/String),
  Default, Kategorie, Beschreibung. Der Katalog liegt im **Web**-Projekt; der Service nutzt ihn
  ueber die ProjectReference.
- `Program.cs` seedet die `ServiceSettings`-Tabelle idempotent aus dem Katalog.
- `/ServiceSettings` ist katalog-getrieben und typisiert (Bool-Toggle / Int / String). Bool wird
  als `"true"`/`"false"` normalisiert (nicht `"1"`/`"0"`), Int per `int.TryParse` mit
  `InvariantCulture`, unbekannte Keys werden ignoriert.
- **DB gewinnt.** Der Service liest alle Katalog-Keys aus der DB — inklusive **saemtlicher**
  `Sync:*Enabled`-Block-Gates. Takt-relevante Reads nutzen
  `ServiceSettings.GetBoolSafeAsync` / `GetIntSafeAsync` (try/catch → Default), damit ein
  transienter DB-Fehler den Worker-Loop nicht abwuergt.
- **Ausnahmen, die appsettings-only bleiben:** `ConnectionStrings:*` und `MailSettings:*` (SMTP)
  — Verbindungsgeheimnisse gehoeren nicht in eine per UI editierbare Tabelle. Ebenso
  `Security:AdDomain`.
- Ein **Drift-Guard-Test** (`ServiceSettingDefinitionsTests.All_ContainsDocumentedServiceReadKey`)
  schlaegt fehl, wenn ein Service-Key nicht im Katalog steht.

## Konsequenzen

**Positiv**
- Syncs lassen sich im Betrieb schalten, ohne Server-Zugriff und ohne Restart.
- Vollstaendige, typisierte Uebersicht aller Schalter an einer Stelle.
- Der Drift-Guard verhindert „heimliche" Keys, die nur im Code existieren.

**Negativ / Risiken**
- **Deploy-relevant:** appsettings-Werte greifen nicht mehr. Jeder gewuenschte Sync muss
  **einmalig** in `/ServiceSettings` aktiviert werden — sonst laeuft nach dem Deploy scheinbar
  „nichts" (../../../docs/TESTSZENARIEN.md Kap. 51).
- Zwei Speicherorte bleiben (DB fuer Verhalten, appsettings fuer Geheimnisse) — die Trennlinie
  muss man kennen.
- Der Katalog ist ein zusaetzlicher Pflichtschritt bei jedem neuen Service-Key.
- Der DB-Read der Werte selbst ist nur Manual-UAT; unit-getestet ist die Invariante
  („ohne DB laufen true-Default-Gates, false-Default-Gates skippen, kein Crash").
