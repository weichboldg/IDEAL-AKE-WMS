---
type: backlog
created: 2026-11-14
quelle: "[[2026-11-14-wochenend-review-test-anwenderdoku]]"
status: neu
modul: UTC → Lokalzeit (v1.99.18, ADR 0025, ZeitstempelKonventionTests)
stand: "Bündel v1.99.22, 90bbbbd7, feature/2026-08-07-ideal-teile-1-5"
---
# Review-Befunde Lokalzeit (Teil A3, lesend)

Gegenstand: `git diff main...HEAD` im Bündel-Worktree. Nichts behoben (Schranke 1). Bezug: [[2026-11-13-utc-schreiber-auf-lokalzeit-spec]],
[[0025-zeitstempel-in-lokalzeit-des-schreibenden-rechners]].

## LZ-1 – Dienst schreibt OSEON-Stammdaten mit GETDATE() (Rest-Stelle)
- **Schwere:** niedrig
- **Fundstelle:** `IDEALAKEWMSService/Services/OseonSyncService.cs:744` (`[ModifiedAt] = GETDATE()`), `:751` (`CreatedAt` per `GETDATE()`)
- **Befund:** ADR 0025: „Dienst `DateTime.Now` als Parameter `@Jetzt`, nie `GETUTCDATE()`/`GETDATE()` im Dienst-Code“. Beide Stellen schreiben
  Audit-Zeitstempel mit der SQL-Server-Uhr (Zeitzone laut ADR nicht belegt). Bereits als H-1 in [[2026-11-13-buendel-v1-99-16-goal-lauf]]
  vermerkt, aber ohne eigene Aufgabe.
- **Vorschlag:** `@Jetzt = DateTime.Now` (SqlDbType.DateTime2) wie in den übrigen Dienst-Schreibern.

## LZ-2 – Wächter erkennt GETDATE() im Dienst nicht
- **Schwere:** Hinweis
- **Fundstelle:** `IdealAkeWms.Tests/Helpers/ZeitstempelKonventionTests.cs:26` (Regex nur `UtcNow|GETUTCDATE|SYSUTCDATETIME`)
- **Befund:** Die ADR-Regel „nie `GETDATE()` im Dienst-Code“ ist nicht bewacht; LZ-1 wurde deshalb nicht rot. Lesende Vergleiche mit
  `GETDATE()` gibt es legitim (`BomCacheSyncService.cs:423`, `CoatingDetectionService.cs:108`, `OseonSyncService.cs:99`).
- **Vorschlag:** Zweiter Fall im Wächter: `GETDATE\(\)` nur in `IDEALAKEWMSService/`, mit Ausnahmeliste für die drei lesenden Vergleiche
  (oder Muster `= GETDATE()`/`, GETDATE()` in INSERT/UPDATE).

## Geprüft ohne Befund
- Kein `UtcNow`/`GETUTCDATE`/`SYSUTCDATETIME` außerhalb der Ausnahmeliste in `IdealAkeWms/` und `IDEALAKEWMSService/` (Wächter grün,
  Treffer manuell gegengeprüft; außerhalb der gescannten Ordner nur der tote Default `SQL/35_AddEnaioDmsDocuments.sql:14`, in ADR 0025 als U4
  begründet). Einzige verbliebene `ToLocalTime()`-Anzeige `Views/HierarchieUmstellung/Index.cshtml:81` gehört zur Ausnahme U2.
- SData-Writer und neue Sync-Dienste nehmen eine Zeit je Lauf (`var jetzt/now = DateTime.Now`).
- Update-Skript-Marker (`SYSDATETIME()`) siehe [[2026-11-14-review-befunde-update-skript]] UP-3.
