---
type: adr
id: 0025
title: Fachliche und Audit-Zeitstempel werden in Lokalzeit des schreibenden Rechners geschrieben, UTC nur mit Wächter-Ausnahme
status: accepted
date: 2026-11-13
supersedes: ""
superseded_by: ""
---

> Spec [[2026-11-13-utc-schreiber-auf-lokalzeit-spec]] (v1.99.18, Freigabe-Antworten 1–12, Gerald 2026-11-13).
> Ergaenzt [[0010-aktivitaets-protokoll-mit-isolierten-dbcontexts]] (dort nur fuer `SyncLog.Timestamp` festgelegt) auf alle Zeitstempel.
> Vorgaenger fuer die Bedarfsmeldungen: [[2026-11-12-befunde-ake-regression-dev-spec]] Punkt 3 (v1.99.13).

## Kontext und Problem

Die Oberflaeche zeigt Zeitstempel ohne Umrechnung an; das Aktivitaets-Protokoll schreibt seit ADR 0010 bewusst `DateTime.Now`
([[fallstricke]] „`SyncLog.Timestamp` ist `DateTime.Now`, nicht `UtcNow`“, mit der Mahnung „konsistent umstellen, nicht halb“). Rund 50
Schreiber hielten sich daran, rund 45 Stellen in 18 Dateien nicht: Web-Controller/Repositories mit `DateTime.UtcNow`, Dienst-raw-SQL mit
`GETUTCDATE()`, SData-Writer mit `UtcNow`. Folge (MEZ, Sommer doppelt): `/Users` und `/Articles` „Erstellt am“, Teileverfolgung
„rueckgemeldet am“ und der Platz NAN standen 1 h zu frueh; mehrere Spalten waren je Zeile gemischt (SQL-Seeds `GETDATE()`, Web `Now`,
`Program.cs` `UtcNow`). `ArticleSageDetails` war UTC und wurde per `ToLocalTime()` umgerechnet – eine zweite Konvention. Ein zentraler
Audit-Setzer (`SaveChanges`-Override) existiert nicht, jede Stelle setzt selbst; ein neuer UTC-Schreiber faellt deshalb nur auf, wenn
jemand die Anzeige vergleicht.

## Betrachtete Optionen

1. **Alles auf UTC** und jede Anzeige umrechnen – kehrt ~50 bestehende Schreiber, alle Views, Spaltenfilter und raw-SQL-Datumsvergleiche um;
   Altdaten in Lokalzeit waeren nicht trennbar. Kein Multi-Timezone-Bedarf (beide Standorte in einer Zeitzone).
2. **Lokalzeit, Dienst per `GETDATE()`** – kuerzer im SQL, aber die Zeitzone der SQL-Server ist nicht belegt (AKESQL20, IDEAL-SQL).
3. **Lokalzeit des schreibenden Rechners**: Web `DateTime.Now`, Dienst `DateTime.Now` als SQL-Parameter `@Jetzt` (`SqlDbType.DateTime2`),
   eine Zeit je Lauf/Statement; plus Quelltext-Waechter mit Ausnahmeliste.
4. **Zentraler Uhr-Dienst / `TimeProvider`-Umbau** – neue Abstraktion ohne zweiten Nutzer; `DateTime.Now` ist die bestehende Konvention.

## Entscheidung

Option 3.

- Fachliche und Audit-Zeitstempel (`CreatedAt`, `ModifiedAt`, `…At` mit fachlicher Bedeutung) werden in **Lokalzeit des schreibenden
  Rechners** geschrieben: Web `DateTime.Now`; Dienst `DateTime.Now` als Parameter `@Jetzt`, nie `GETUTCDATE()`/`GETDATE()` im Dienst-Code.
  (`GETDATE()` in SQL-Seeds/AgentJobs bleibt – Lokalzeit des SQL-Servers, kein UTC.)
- Innerhalb eines Laufs/Aufrufs **eine** Zeit (`var jetzt = DateTime.Now` einmal), damit zusammengehoerige Zeilen denselben Stempel tragen
  (z. B. `ReleasedAt = ModifiedAt`, Sammelfreigabe, Reconcile `CancelledAt = ModifiedAt`).
- Anzeigen rechnen nicht um (kein `ToLocalTime()` auf gespeicherten `DateTime`-Spalten).
- **Altdaten bleiben unveraendert** (Freigabe-Antworten 1–11): kein Einmal-Skript; gemischte Spalten sind nicht sicher trennbar.
- **Waechter** `IdealAkeWms.Tests/Helpers/ZeitstempelKonventionTests.cs` durchsucht `*.cs`/`*.cshtml` in `IdealAkeWms/` und
  `IDEALAKEWMSService/` (ohne `bin`, `obj`, `Migrations`, `wwwroot/lib`) nach `UtcNow|GETUTCDATE|SYSUTCDATETIME`. Erlaubt nur
  (Datei → Trefferzahl, Grund):

| Datei | Treffer | Warum UTC (bzw. kein Schreiber) |
|---|---|---|
| `IdealAkeWms/Services/PdfRenderService.cs` | 1 | U1: Vergleich mit `Directory.GetCreationTimeUtc`, Dateisystem-Zeit, nicht gespeichert |
| `IdealAkeWms/Services/HierarchischeStruktur/HierarchischeStrukturStatus.cs` | 1 | U2: `DateTimeOffset` im Speicher; Views rechnen per `.LocalDateTime`/`.ToLocalTime()` um |
| `IDEALAKEWMSService/Services/BomCacheSyncService.cs` | 4 | U3: `CachedBomHeaders.CachedAt` intern konsistent UTC (2 Schreiber, 2 Vergleiche), nie angezeigt |
| `IdealAkeWms/Services/SyncLogger/SyncRun.cs` | 1 | nur Kommentar |
| `IDEALAKEWMSService/Services/PartRequisitionEmailService.cs` | 1 | nur Kommentar |
| `IdealAkeWms/Views/Help/Changelog.cshtml` | 1 | historischer Anwender-Changelog-Text |

  Zusaetzlich toter Default U4 `SQL/35_AddEnaioDmsDocuments.sql:14` `DEFAULT GETUTCDATE()` (einziger Schreiber setzt `CreatedAt` explizit
  auf `DateTime.Now`; `00_FreshInstall.sql` hat keinen Default) – nicht im Waechter-Scan (SQL-Skripte), keine Schema-Aenderung.
  Ein zweiter Test prueft, dass die Ausnahmeliste nicht veraltet (Trefferzahl je Datei exakt).

## Konsequenzen

- **Positiv:** eine Konvention in Web, Dienst und SData; „Erstellt am“/„rueckgemeldet am“ stimmen fuer neue Zeilen; ein neuer UTC-Schreiber
  bricht den Build-Testlauf mit Datei:Zeile statt erst in der Abnahme aufzufallen.
- **Negativ:** Altdaten bleiben gemischt – Zeilen von vor v1.99.18 koennen 1–2 h zu frueh stehen (u. a. `/Users`, `/Articles`, Teileverfolgung,
  `ArticleSageDetails` auf DEV/Test: deren alte UTC-Werte erscheinen nach dem Entfernen von `ToLocalTime()` jetzt 1–2 h zu frueh).
- **Risiko:** Lokalzeit ist um die Sommerzeit-Umstellung (eine Stunde im Oktober doppelt) nicht eindeutig; fuer Audit-/Anzeigezwecke
  hingenommen, kein Zeitstempel dient als Schluessel oder Reihenfolge-Garant ueber die Umstellung hinweg.
- **Neue Ausnahme** nur mit Begruendung in der Waechter-Liste **und** Nachtrag hier (bzw. neuer ADR, der diesen superseded).
- Raw-SQL-Pfade (`SageImportService`, `OseonSyncService`, `CoatingDetectionService`) sind nicht InMemory-testbar ([[fallstricke]]
  „Raw-SQL-Pfade sind nicht InMemory-testbar“) – Nachweis per Manual-UAT TS-135.8.
