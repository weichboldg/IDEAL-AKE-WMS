---
type: adr
id: 0010
title: Jeder Hintergrund-Service protokolliert in ein Aktivitaets-Protokoll mit isolierten DbContexts
status: accepted
date: 2026-05-26
supersedes: ""
superseded_by: ""
---

> **Nachtraeglich erfasst** (2026-07-27). Bestandsdokumentation; eingefuehrt in v1.15.0
> (Spec `../../../docs/superpowers/specs/2026-05-26-synclog-pflicht-alle-syncs-design.md`), auf
> Non-Sync-Services ausgedehnt in v1.15.1.

## Kontext und Problem

Der Windows-Service fuehrt viele unbeaufsichtigte Laeufe (Sage-Import, OSEON, enaio, BOM-Cache,
Mails, BDE-Auto-Pause, Cleanup). Ging etwas schief, stand es allenfalls in der Serilog-Datei auf
dem Server — fuer den Admin in der Web-App unsichtbar. Fragen wie „laeuft der Artikel-Sync
ueberhaupt?" oder „warum sind gestern 400 FAs storniert worden?" waren ohne Server-Zugriff nicht
beantwortbar.

Ein naiver Ansatz — Protokollzeilen ueber denselben `DbContext` schreiben, den der Sync benutzt —
hat einen fatalen Fehler: laeuft der Sync in einer Transaktion und rollt zurueck, verschwindet
das Protokoll der Fehlersuche mit.

## Betrachtete Optionen

- **Nur Serilog-Dateien** — nichts zu bauen, aber fuer Fachadmins unerreichbar.
- **Protokoll ueber den Sync-DbContext** — einfach, aber Diagnose-Zeilen rollen mit der
  Transaktion zurueck; genau im Fehlerfall fehlen sie.
- **Eigener Lauf-/Eintrags-Lebenszyklus mit frischem DbContext pro Zeile** — mehr Verbindungen,
  dafuer transaktionsunabhaengig.

## Entscheidung

- `ISyncLogger` / `ISyncRun` (`../../../IdealAkeWms/Services/SyncLogger/`) bilden einen Lauf ab:
  `BeginRunAsync(service)` → Info-/Warn-/Error-Detailzeilen → `FinishSuccess` /
  `FinishFailedAsync`.
- **Jede Zeile wird mit einem frischen `DbContext` aus `IDbContextFactory<ApplicationDbContext>`
  geschrieben** — Diagnose-Logs duerfen nicht in Sync-Transaktionen mitrollen.
- Jeder Sync-Service ist protokollpflichtig; die Service-Namen sind Konstanten in
  `SyncLogServices.All` (ein Name, der dort fehlt, ist im Protokoll-Filter nicht auswaehlbar).
- Counts-Keys sind **deutschsprachig** und wiederverwendet: `neu`, `aktualisiert`,
  `uebersprungen`, `fehler`, plus fachliche wie `storniert`, `reaktiviert`, `nullgesetzt`,
  `erledigt-gesetzt`.
- Detailzeilen sind **gecappt** (typisch 100/Lauf), damit ein Massen-Effekt das Protokoll nicht
  flutet; die Counts zaehlen weiterhin alles.
- `Timestamp` ist **Lokalzeit** (`DateTime.Now`, Model-Default) — die UI zeigt ihn ohne
  Konversion an.
- Fachlich getrennte Abschnitte bekommen einen **eigenen Lauf**, nicht Detailzeilen im
  Nachbarlauf (Beispiel: `ProductionOrderReconciliation` laeuft getrennt vom `ProductionOrder`-Import).
- Sync-Fehler loesen zusaetzlich eine konfigurierbare **Fehlermail** aus
  (`ISyncErrorNotifier`, `ErrorNotification:*`); der Notifier wirft selbst nie.
- Konvention: `ISyncLogger` ist der **letzte** Konstruktor-Parameter, nach `ILogger<T>`. Die
  Connection-String-Validierung liegt **innerhalb** des try-Blocks nach `BeginRunAsync`, damit
  `FinishFailedAsync` auch bei Config-Fehlern feuert.

UI-Label ist „Aktivitaets-Protokoll", Tabelle und Klassen behalten den historischen Namen
`SyncLog`/`SyncLogger` — bewusste Asymmetrie, Route bleibt `/SyncLog/Index`.

## Konsequenzen

**Positiv**
- Admins sehen ohne Server-Zugriff, was der Service getan hat, mit welchem Ergebnis.
- Protokolle ueberleben Rollbacks — genau im Fehlerfall sind sie da.
- Gleiches Vokabular ueber alle Services macht Laeufe vergleichbar.

**Negativ / Risiken**
- Eine DB-Verbindung pro Protokollzeile; bei sehr geschwaetzigen Services relevant, daher die
  Caps.
- Die Tabelle waechst unbegrenzt — deshalb gibt es seit v1.25.0 den `CleanupWorker`
  (`Cleanup:AktivitaetsprotokollAufbewahrungTage`, Default 180, `0` = nie loeschen).
- **Wer `DateTime.UtcNow` setzt, produziert Eintraege, die in der DESC-Sortierung „zwei Stunden
  frueher" erscheinen und unter aelteren verschwinden** — das war der Bug v1.15.0–v1.15.2.
- Protokollzeilen in raw-SQL-Pfaden sind nicht InMemory-testbar (Manual-UAT).
