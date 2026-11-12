---
type: spec
title: Update-Skript prüft und führt aus, mit Bestätigung (v1.99.0)
slug: 2026-11-11-update-skript-prueft-und-fuehrt-aus-mit-bestaetigung-spec
status: Testbereit
created: 2026-11-11
updated: 2026-11-12
freigabe_von: "Gerald Weichbold (Chat Koordination, „Ok“ 2026-11-11 18:38)"
freigabe_am: 2026-11-11
source_backlog: "[[2026-11-11-buendel-deploy-update-skript]]"
task: "[[2026-11-11-buendel-deploy-update-skript]]"
depends_on: "[[2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
affected_code:
  - "scripts/deploy/Update-IdealAkeWms.ps1 (Prüfungen, Ausführung Daten/Einmalig, Marker, Backup-Pfad, Fehlerzustand, Bugfixes :110 und :496)"
  - "scripts/deploy/IdealAkeWms.Deploy.psm1 (neu: Invoke-SqlTransaktion, Split-SqlBatches, Show-Zustand; nur wenn beide Skripte es brauchen)"
  - "scripts/deploy/nachlauf.psd1 (Typ Pruefung, neue Ausfuehrung Transaktion/Reparatur, SQL/142, Umzug Nachlauf -> Pruefung)"
  - "SQL/142_AddDeployScriptRuns.sql (neu; Nummer prüft der Dev-Lauf)"
  - "SQL/00_FreshInstall.sql (Tabelle DeployScriptRuns + MigrationId)"
  - "SQL/110_BackfillStockMovementProductionOrderId.sql (Zeile TREFFER im Trockenlauf-Zweig)"
  - "SQL/Einmalig/2026-11-08_v1.88.0_Kommissionierauftraege_Altbestand.sql (Zeile TREFFER im Trockenlauf-Zweig)"
  - "IdealAkeWms/Models/DeployScriptRun.cs (neu), IdealAkeWms/Data/ApplicationDbContext.cs, IdealAkeWms/Migrations/<ts>_AddDeployScriptRuns.cs (neu)"
  - "IdealAkeWms/Program.cs (Startup-Seed Kommissionierbereiche nach Muster OperationConfigs :326-359)"
  - "IdealAkeWms.Tests/Deploy/NachlaufManifestDriftTests.cs (Erweiterung, ein bestehender Test geändert)"
  - "IdealAkeWms.Tests/Repositories/DeployScriptRunsMigrationSqlTests.cs (neu, Muster *MigrationSqlTests)"
  - "IdealAkeWms.Tests (Startup-Seed Kommissionierbereiche, falls der Seed in eine testbare Methode wandert)"
  - "IdealAkeWms/AppVersion.cs, IDEALAKEWMSService/AppVersion.cs, Views/Help/Changelog.cshtml (v1.99.0)"
  - "README.md (Update-Abschnitt: Prüfungen, Ausführung mit Bestätigung, Marker)"
  - "docs/TESTSZENARIEN.md (neues Kapitel TS-126)"
  - "Hauptcheckout: .claude/agents/spec-agent.md und secondbrain/_templates/spec.md (Typ Pruefung, Ausfuehrung-Werte), ADR 0023 (neu), Verweis-Nachtrag in ADR 0015 und ADR 0022"
open_questions:
  - "1. Einträge mit Ausfuehrung = Transaktion (SQL/108, v1.47.4): wie angeordnet in Phase Nachher bei laufender App (Sperren bleiben bis zur Entscheidung übernehmen/verwerfen bestehen) oder nach dem Kopieren und vor dem Start bei gestopptem Web und Dienst (Empfehlung)?"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
---

## Ziel / Nutzen (das Warum)

Der erste Lauf am Zielsystem IDEAL-Test (11.11.2026, [[2026-11-11-buendel-deploy-update-skript]] Z. 46-73) hat drei Lücken des
Update-Skripts v1.98.1 gezeigt:
- Es meldet Zustände, die SQL-prüfbar und reparierbar sind, nur als Hand-Schritt oder gar nicht. Fehlende Kommissionierbereiche ließen den
  Altbestand-Trockenlauf scheitern (Z. 58-65), RCSI war aus (Z. 50).
- Daten- und Einmal-Skripte führt es nie aus. Der Mensch muss sie in SSMS starten, ohne Nachweis, ob sie auf dem System schon liefen.
- Es bricht an vermeidbaren Stellen roh ab: StrictMode an `Update-IdealAkeWms.ps1:110`, Backup-Pfad an Z. 47-49 der Aufgabe.

Gerald hat am 11.11.2026 um 16:42/16:44 entschieden (Aufgabe Z. 66-71):
- Das Skript prüft solche Punkte selbst und repariert sie nach Bestätigung.
- Daten- und Einmal-Skripte führt es mit Trockenlauf bzw. in einer Transaktion aus.
- Die Marker-Tabelle kommt jetzt. Damit ist Freigabe-Antwort 1 der Vorgänger-Spec überholt (deren Nachtrag 16:44).

Die Verantwortung bleibt beim Menschen am Zielserver: Jeder schreibende Schritt braucht dort eine Eingabe.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. Neuer Manifest-Typ `Pruefung`: lesende Prüfabfrage, optional Reparatur, Bestätigung durch den Menschen (Abschnitt „Prüfungen“).
2. Ausführung von Daten- und Einmal-Skripten durch das Update-Skript (Abschnitt „Daten- und Einmal-Skripte“):
   - mit Trockenlauf-Schalter: Trockenlauf automatisch, scharf nur nach `SCHARF` und Backup aus demselben Lauf;
   - ohne Schalter: in einer Transaktion, danach `uebernehmen`/`verwerfen`.
3. Marker-Tabelle `DeployScriptRuns`: Migration, `SQL/142`, FreshInstall an beiden Stellen (ADR 0004). Bereits gelaufene Einträge werden
   nicht mehr angeboten. Altstand über Spuren oder einmalige Erfassung.
4. ADR 0023 als Vorschlag. Er ersetzt die Sätze „nie scharf“ aus ADR 0015 und ADR 0022 teilweise; beide bekommen einen Verweis-Nachtrag.
5. Bugfixes aus dem ersten Zielsystem-Lauf:
   - `:110` StrictMode;
   - `:496` `$PaketVersion?`;
   - Backup-Pfad vorab prüfen;
   - Zustand und Weg nach jedem unerwarteten Fehler;
   - Startup-Seed der Kommissionierbereiche.
6. Nachlauf-Liste nur noch für Einstellungen (Oberfläche), Leitstand und Anwender-Info; SQL-Prüfbares wandert nach `Pruefung`.
7. Drift-Test-Erweiterung, TS-126, README, Version v1.99.0.

**Out-of-Scope**
- Offene Kleinbefunde der Vorgänger-Spec F4, F6, F8, N1 (dort Checkliste Punkt 14), außer F4 teilweise über Zustandsmeldung (AK 21).
- Umstellung der Schema-Skripte von `sqlcmd` auf SqlClient (Zusatzidee unten, nicht bauen).
- Ausführung der 00er-Bereinigung durch das Skript: Sie braucht eine von Hand gepflegte `@Behalten`-Liste und P7 auf Sage
  (`SQL/Einmalig/2026-10-15_IDEAL-Sage-Views_Bereinigung00erArtikel.sql:22-35`). Sie bleibt `anzeigen` und bekommt nur die Marker-Erfassung.
- Hotfix-1.30.1 (bewusst nicht im Manifest, `nachlauf.psd1:14-15`).
- Eine Anzeige der Marker-Tabelle in der Web-App.

### Grenzen für den späteren Dev-/QA-Lauf (hart)
- Kein Lauf gegen akenet01 (Ports 4443/4444/4445), idealweb01 oder AKESQL20. Zielsysteme fährt nur Gerald, von Hand, in einer
  Administrator-PowerShell am Zielserver.
- Kein Zugriff auf Sage, weder lesend noch schreibend (Drift-Test `DeploySkripte_NurAscii_OhneSage_RechteNurHinzufuegend`,
  `NachlaufManifestDriftTests.cs:118-130`, bleibt grün).
- Agenten führen echte Daten- und Einmal-Skripte (`SQL/108`, `SQL/110`, `SQL/Einmalig/*`) **nie scharf** aus, auch nicht in einer
  Transaktion mit anschließendem Verwerfen, auch nicht auf DEV. Die Scharf- und Transaktionspfade testen Agenten nur mit einem synthetischen
  Testskript in einer Paketkopie gegen eine DEV-Kopie-DB (TS-126, Vorbedingungen). Trockenläufe der echten Skripte sind erlaubt.
- Kein Push, kein Merge, kein Aufräumen von Worktree/Branch.

## Fachliche Anforderungen

### Belegter Ausgangsstand (v1.98.1, Bündel-Worktree)

| Befund | Beleg |
|---|---|
| Daten/Einmal laufen nie scharf; nur Trockenlauf über `sqlcmd` mit Literal-Prüfung | `Update-IdealAkeWms.ps1:268-280`, `:454-457`, `:541` |
| Lesende Abfragen über SqlClient mit Schlüsselwort-Wächter | `:220-227` (Wächter `:221`) |
| Skripte über `sqlcmd -b -I -f 65001` | `:228-244` |
| RCSI nur als Warnung, nur Firma IDEAL | `:365-367` |
| Einmal-Spuren nur angezeigt | `:257-261`, `:368` |
| Haltepunkt: weiter nur mit Eingabe `ausgefuehrt` | `:450-459` |
| Backup-Pfad ungeprüft; leerer Standardpfad ergibt `\<DB>_….bak` | `:388-400`, `:390-392` |
| Hauptlauf-Fehler zeigt nur die Meldung, keinen Zustand | `:557-559` |
| Seed Kommissionierbereiche nur in `SQL/133:190-198` und `00_FreshInstall.sql:2902-2905` | Migration `20261107193219_AddPickingAreasAndOrders` hat kein `InsertData`/`Sql` (Grep `InsertData\|migrationBuilder\.Sql` über `IdealAkeWms/Migrations`: 16 Dateien, diese nicht darunter) |
| Ohne aktiven Standardbereich legt die App keine Kommissionieraufträge an | `IdealAkeWms/Services/PickingOrderWorkflow.cs:89-95` |
| Ohne aktiven Standardbereich bricht das Altbestand-Skript ab | `SQL/Einmalig/2026-11-08_v1.88.0_…Altbestand.sql:52-57` |
| Web-Start: `Migrate()`, dann Seeds nach Muster „wenn fehlt/leer, anlegen“ | `IdealAkeWms/Program.cs:203`, `:205-218` (NAN), `:221-254` (Rollen), `:295-324` (Settings), `:326-359` (OperationConfigs, `if (!db.OperationConfigs.Any())`) |
| GO-Aufteilung gibt es im Repo schon: Regex `(?im)^\s*GO\s*$` | `IdealAkeWms/Migrations/20260512120355_AddProductionOrderSplit.cs:50` |

### Prüfungen (Typ `Pruefung`)

**Manifest-Felder** (zusätzlich zu `Id`, `Version`, `Typ`, `Firma`, `System`, `Phase`, `Braucht`, `Text`, `Quelle`):

| Feld | Pflicht | Bedeutung |
|---|---|---|
| `Pruefung` | ja | lesendes SQL, liefert **eine Zahl**: 0 = in Ordnung, > 0 = Anzahl Abweichungen. Muss den Wächter `:221` passieren. |
| `Ausfuehrung` | ja | `Reparatur` (mit `Reparatur` oder `Datei`) oder `anzeigen` (nur Hinweis, Behebung von Hand laut `Text`) |
| `Reparatur` | eines von beiden bei `Reparatur` | SQL, läuft über `Invoke-SqlTransaktion` (in einer Transaktion) |
| `Datei` | eines von beiden bei `Reparatur` | idempotentes Skript im SQL-Ordner, läuft über `sqlcmd` wie ein Schema-Skript (`Invoke-SqlDatei`, `:234-244`) |
| `OhneTransaktion` | nein | `$true` für Anweisungen, die SQL Server in einer Benutzertransaktion ablehnt (`ALTER DATABASE`) |

- Alle SQL-Felder als **einfach gequotete** psd1-Literale mit `''` als Escape. Nur so liest sie der Parser des Drift-Tests
  (`NachlaufManifestDriftTests.cs:40-42`).
- Dateien bleiben ASCII. Umlaute in SQL-Texten per `NCHAR(228)` usw. (`DeploySkripte_NurAscii…`, [[fallstricke]] §23).
- **Phase:** `Haltepunkt` = nach dem Schema, Web und Dienst gestoppt; `Nachher` = nach dem Start.
- **Kein Versionsfilter.** Prüfungen laufen bei jedem Deploy, denn genau der Fall „Version schon neu, Zustand trotzdem falsch“ ist der
  IDEAL-Test-Befund. Firma- und System-Filter wie bisher (`Test-PasstFirma`, `:246-250`).

**Ablauf:**
1. **Vorprüfung (Schritt 1, auch bei `-NurPruefen`):**
   - Jede passende Prüfung lesend ausführen. Ergebnis je Zeile: `OK`, `Abweichung n` (mit Hinweis „wird in 4a repariert“ bzw. „von Hand:
     <Text>“), `nach Schema` (eine `Braucht`-MigrationId fehlt noch, Prüfung läuft dann in 4a) oder `Fehler: <Meldung>`.
   - Eine fehlende `Braucht`-Id bricht hier **nicht** ab. Das ist eine eigene, nicht werfende Variante von `Test-Braucht` (`:262-267`).
2. **Stoppen:** Hat eine Prüfung der Phase `Haltepunkt` eine Abweichung mit Reparatur (oder steht sie auf „nach Schema“), stoppt das
   Skript App-Pool **und** Dienst, auch bei `-Komponente Web|Dienst`. Das ist dieselbe Regel wie bei fehlenden Schema-Skripten (`:373`).
3. **Schritt 4a „Prüfungen und Haltepunkt“**, nach dem Schema und **vor** den Einmal-Haltepunkten. Das Altbestand-Skript braucht den
   Standardbereich. Je Prüfung der Phase `Haltepunkt`:
   1. erneut prüfen;
   2. bei Abweichung `Text` und Reparatur anzeigen und „Reparatur ausführen? [j/n]“ fragen;
   3. vor der Reparatur live prüfen, dass App-Pool und Dienst `Stopped` sind, sonst Abbruch;
   4. reparieren und erneut prüfen. Ist das Ergebnis nicht 0 → Abbruch, Web und Dienst bleiben gestoppt.
   - „n“ → Warnung, die in der Nachlauf-Liste erscheint, und weiter.
   - Einträge mit `Ausfuehrung = anzeigen` → nur die Warnung.
4. **Schritt 7:** Prüfungen der Phase `Nachher` laufen lesend nach dem Start. Abweichungen erscheinen im Block „Prüfungen“ der Nachlauf-Liste.
   Reparaturen der Phase `Nachher` gibt es in dieser Spec nicht.
5. Reparaturen stehen nur im Protokoll (Transcript) mit SQL, Ergebnis vorher/nachher und Bestätigung, **nicht** in der Marker-Tabelle. Eine
   Prüfung bewertet den Zustand jedes Mal neu; ein Marker würde nichts verhindern.

**Prüfungen dieser Spec** (Abfragetexte sind Vorgabe; der Dev-Lauf darf sie syntaktisch glätten, nicht fachlich ändern):

| Id | Firma | Phase | Ausf. | `Pruefung` (0 = OK) | Reparatur | Beleg |
|---|---|---|---|---|---|---|
| `Pruefung/Kommissionierbereiche` | beide | Haltepunkt | Reparatur | `SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.PickingAreas) THEN 0 ELSE 1 END`, `Braucht=@('SQL/133')` | `Datei='133_AddPickingAreasAndOrders.sql'` (idempotent, Seed nur bei leerer Tabelle, `SQL/133:191`) | Aufgabe Z. 58-65, 72 |
| `Pruefung/Standardbereich` | beide | Haltepunkt | anzeigen | `SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.PickingAreas) AND NOT EXISTS (SELECT 1 FROM dbo.PickingAreas WHERE IsDefault = 1 AND IsActive = 1) THEN 1 ELSE 0 END`, `Braucht=@('SQL/133')` | – (Text: Stammdaten → Kommissionierbereiche, Standard festlegen) | `PickingOrderWorkflow.cs:90-94`; die Reparatur oben greift bei nicht leerer Tabelle nicht |
| `Pruefung/RCSI` | beide | Haltepunkt | Reparatur | `SELECT CASE WHEN d.is_read_committed_snapshot_on = 0 AND EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] IN (N''Sync:HierarchicalFaEnabled'', N''ProduktionsauftragHierarchisch'') AND LOWER(LTRIM(RTRIM([Value]))) = N''true'') THEN 1 ELSE 0 END FROM sys.databases d WHERE d.name = DB_NAME()` | `Reparatur='ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE'`, `OhneTransaktion=$true` | RCSI braucht der FA-Struktur-Import (TS-57.0, `docs/TESTSZENARIEN.md:6385-6395`); Schalter `ServiceSettingDefinitions.cs:155, 158`. Bedingung über die Schalter statt `Firma=IDEAL`, damit auch AKE Test-IDEAL (Firma AKE, IDEAL-Betrieb) geprüft wird. AKE hat beide `false` → 0. |
| `Pruefung/RollenBeschichtungsauftrag` | beide (v1.99.1; bis v1.99.0 AKE) | Haltepunkt | Reparatur | `SELECT COUNT(*) FROM (VALUES (N''beschichtungsauftrag''), (N''beschichtungsauftrag_schreiben'')) v(k) WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.[Key] = v.k)` | INSERT der fehlenden Zeilen (unten) | Freigabe-Antwort 6 der Vorgänger-Spec; kein Seed in SQL, FreshInstall oder Startup (Grep `beschichtungsauftrag` über `*.cs`/`*.sql`: nur `RoleKeys.cs:24, 26`) |
| `Pruefung/DummyArtikel` | beide | Haltepunkt | Reparatur | `SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.Articles WHERE ArticleNumber = N''DUMMY'') THEN 0 ELSE 1 END` | `Datei='86_SeedDummyArticle.sql'` (idempotent, `SQL/86:15`) | Kandidatentabelle; ohne DUMMY antwortet die Lagerbestellung mit `BadRequest` (`WarehouseRequisitionsApiController.cs:138-140`) |
| `Pruefung/QrTrennzeichenKomma` | beide | Nachher | anzeigen | `SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = N''QrTrennzeichen'' AND [Value] = N'','') THEN (SELECT COUNT(*) FROM dbo.StorageLocations WHERE Code LIKE N''%,%'') ELSE 0 END` | – | ersetzt `v1.69.0-Trennzeichen` (`nachlauf.psd1:166-167`) |
| `Pruefung/Firmenanschrift` | IDEAL | Nachher | anzeigen | `SELECT COUNT(*) FROM dbo.AppSettings WHERE [Key] = N''Firmenanschrift'' AND [Value] LIKE N''%38, 4840%''` | – (Korrektur unter /Settings) | ersetzt `v1.49.0-Anschrift` (`:141-142`); Key `AppSettingKeys.cs:86` |
| `Pruefung/ArticlesViewName` | IDEAL | Nachher | anzeigen | `SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.ServiceSettings WHERE [Key] = N''Sync:ArticlesViewName'' AND LTRIM(RTRIM(ISNULL([Value], N''''))) <> N'''') THEN 0 ELSE 1 END` | – | Teil von `v1.60.0-IDEAL-Views` (`:152-153`); dieselbe Bedingung prüft das 00er-Skript (`…Bereinigung00erArtikel.sql:65-70`) |

Reparatur `Pruefung/RollenBeschichtungsauftrag` (Spalten wie `SQL/78:12-17`; Namen aus `Views/Users/RoleOverview.cshtml:195, 208`;
`IsSystem = 1` sperrt Schlüssel und Löschen, `RolesController.cs:143-147, 187-191`, wie bei den anderen im Code verwendeten Rollen):

```sql
INSERT INTO dbo.Roles ([Key], [Name], [Description], [IsSystem], [SortOrder], [CreatedAt], [CreatedBy], [CreatedByWindows])
SELECT v.k, v.n, v.d, 1, (SELECT ISNULL(MAX(SortOrder), 0) FROM dbo.Roles) + v.o, SYSDATETIME(), N'Update-Skript', LEFT(SUSER_SNAME(), 200)
FROM (VALUES (N'beschichtungsauftrag', N'Beschichtungsauftrag (IDEAL)', N'Beschichtungsauftrag-Liste und Bildschirmdruck (Lesen).', 1),
             (N'beschichtungsauftrag_schreiben', N'Beschichtungsauftr' + NCHAR(228) + N'ge (Schreibrecht)',
              N'Gespeicherte Beschichtungsauftraege anlegen und bearbeiten; schliesst Lesen ein.', 2)) v(k, n, d, o)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.[Key] = v.k);
```
Das Zuweisen an Anwender bleibt Hand-Schritt (Info-Eintrag `v1.51.0-Rolle`, Text gekürzt). An IDEAL stammen die Rollen aus der Installation
(`nachlauf.psd1:144`). Die Prüfung wäre dort harmlos, bleibt aber wie angeordnet AKE.

**RCSI-Sonderregeln:**
- Vor der Reparatur zeigt das Skript lesend die anderen Sitzungen auf der DB:
  `SELECT session_id, login_name, host_name, program_name FROM sys.dm_exec_sessions WHERE database_id = DB_ID() AND session_id <> @@SPID`.
- `WITH ROLLBACK IMMEDIATE` beendet diese Sitzungen. Die Bestätigungsfrage nennt das ausdrücklich.
- Die Reparatur läuft außerhalb einer Transaktion. SQL Server lehnt `ALTER DATABASE` in einer Benutzertransaktion ab.

### Daten- und Einmal-Skripte

> **Freigabe-Antwort 1 (Gerald 2026-11-11), gilt vor dem Text unten:** Die Transaktions-Einträge (`SQL/108`, v1.47.4) laufen **vor dem Start**,
> bei gestopptem Web und Dienst, in einem eigenen Schritt zwischen 5a und 6 (Phase `VorStart`). Wo der Text unten „Nachher“ für diese
> beiden Einträge sagt, gilt `VorStart`. Trockenlauf-Einträge mit Startbedingung „nach Web- und Dienst-Start“ (`SQL/110`) bleiben `Nachher`.

**Marker zuerst.** Für jeden Eintrag vom Typ `Daten` oder `Einmalig` liest das Skript die Marker-Zeile (`ManifestId = Id`).
- Gibt es sie, erscheint der Eintrag als „erledigt am <ExecutedAt> (<Mode>)“ und wird **nicht** angeboten.
- Ein Haltepunkt mit Marker hält nicht.
- Fehlt die Tabelle noch (vor `SQL/142`, z. B. bei `-NurPruefen`), meldet das Skript „Marker-Tabelle fehlt (kommt mit SQL/142)“ und arbeitet
  ohne Marker weiter. Die Abfrage braucht einen `OBJECT_ID`-Guard.

**A) `Ausfuehrung = Trockenlauf`** (Altbestand: Phase `Haltepunkt`; `SQL/110`: Phase `Nachher`)
1. Trockenlauf **ohne Rückfrage**, Datei unverändert. Die Literal-Prüfung `Test-Trockenlauf` (`:268-271`) bleibt Voraussetzung, sonst nur
   Anzeige. Ausgabe als Datei in den Log-Ordner (wie `:277`).
2. **Treffer:** Der Trockenlauf-Zweig beider Skripte schreibt vor seinem `RETURN` die Zeile `TREFFER: <n>`. Ein `PRINT` mit Unterabfrage ist in
   T-SQL nicht erlaubt, deshalb über eine Variable: `DECLARE @Treffer INT = (…); PRINT CONCAT(N'TREFFER: ', @Treffer);`. Das Skript liest die
   Zeile mit `^TREFFER: (\d+)$`.
   - Altbestand: `SELECT COUNT(*) FROM #Fa`, vor `…Altbestand.sql:94`.
   - `SQL/110`: `SELECT COUNT(*) FROM #Kandidat k JOIN #Text x ON x.FaText = k.FaText WHERE x.Klasse = N'uebernommen'`, vor `SQL/110:69`.
     Das ist genau die Menge, die der Scharflauf ändert (`:74-82`). Damit gilt „zweiter Trockenlauf muss 0 liefern“ (`nachlauf.psd1:88`).
   - Eine weitere Kopie der Fachregel im Skript gibt es dadurch nicht.
   - Fehlt die Zeile oder endet der Trockenlauf mit Exit ≠ 0: Warnung, nur Anzeige, kein Scharflauf. Beim Haltepunkt folgt der Abbruch
     wie bisher.
3. **Treffer 0** → „entfällt“, dann „Als ‚entfällt‘ vermerken? [j/n]“; j → Marker `Entfaellt`. Ein Haltepunkt geht ohne Eingabe weiter.
   Das ersetzt für den Fall 0 die Haltepunkt-Regel aus Freigabe-Antwort 3 der Vorgänger-Spec (Auftrag Gerald 2026-11-11, Punkt „0 Treffer
   → entfällt“; Beleg IDEAL-Test, Aufgabe Z. 72).
4. **Treffer > 0:**
   1. Ausgabe zeigen.
   2. Liegt aus **diesem Lauf** kein Backup vor (Schritt 2 abgelehnt oder nur „liegt vor“ bestätigt), das Backup jetzt anbieten (dieselbe
      Funktion wie Schritt 2). Ohne Backup aus diesem Lauf gibt es keinen Scharflauf.
   3. Dann: „Zum Ausführen SCHARF eintippen“. Weiter nur bei genau `SCHARF` (Groß/Klein beachtet, wie `-cne` in `:553`).
   4. Scharflauf: Das Skript ersetzt **genau ein** Vorkommen von `DECLARE @Trockenlauf BIT = 1;` im Arbeitsspeicher durch `… = 0;`. Die Datei
      bleibt unverändert. Bei 0 oder mehr als 1 Vorkommen → Abbruch.
   5. Ausführung über `Invoke-SqlTransaktion`. Der Marker `Scharf` (mit Treffer, Ausgabe, SHA-256 der Originaldatei) wird **in derselben
      Transaktion** geschrieben, danach **sofort Commit**.
      - Es gibt keine zweite Rückfrage: Die Zählung ist mit `SCHARF` freigegeben.
      - Eine offene Transaktion während einer Eingabe hielte Sperren auf `StockMovements` bzw. `PickingOrders` (`SQL/110` läuft bei
        laufender App, Sperr-Eskalation träfe neue Buchungen).
   6. Danach ein zweiter Trockenlauf, Anzeige von `TREFFER`. Ist er nicht 0, folgt eine Warnung.
   - Andere Eingabe als `SCHARF`: beim Haltepunkt Abbruch, Web und Dienst bleiben gestoppt (Freigabe-Antwort 3, kein Überspringen). In der
     Phase `Nachher` bleibt der Eintrag offen in der Nachlauf-Liste.

**B) `Ausfuehrung = Transaktion`** (neu; `SQL/108` und v1.47.4, beide Phase `Nachher`; Phase siehe Rückfrage 1)
1. Voraussetzung (`Braucht`), `Text` und vorhandene `Pruefabfrage` (`SQL/108`: P-4, `nachlauf.psd1:82`) anzeigen.
2. Frage: „<Id> jetzt in einer Transaktion ausführen? [j = ausführen / n = später / e = lief schon, nur vermerken]“.
   - `e` → Marker `VonHand` mit Notiz „von Hand erfasst“.
   - `n` → der Eintrag bleibt offen.
3. Backup: wie A4. Aus diesem Lauf oder bestätigt „liegt vor“ wie Schritt 2. ADR 0015 verlangt das Backup vor dem Skript.
   **Ab v1.99.1 (Freigabe-Nachtrag Punkt 3): nur ein Backup aus diesem Lauf; „liegt vor“ reicht nicht mehr.**
4. `j` → `Invoke-SqlTransaktion`:
   1. Datei in Batches teilen (Regex wie `20260512120355_AddProductionOrderSplit.cs:50`).
   2. Alle Batches in **einer** SqlClient-Transaktion ausführen. `PRINT` kommt über `InfoMessage`, jede Ergebnismenge über
      `NextResult()` als Tabelle in Konsole und Protokoll.
   3. Marker `Transaktion` in derselben Transaktion schreiben.
   4. Hinweis anzeigen: „Sperren bestehen bis zur Entscheidung; Web und Dienst laufen“, mit verstrichener Zeit.
   5. Eingabe `uebernehmen` → Commit, `verwerfen` → Rollback. Andere Eingaben werden erneut abgefragt; leere oder geschlossene Eingabe gilt
      als `verwerfen`.
5. Fehler in einem Batch → Rollback (ein `Rollback` auf eine vom Server bereits beendete Transaktion abfangen, `XACT_ABORT`), kein Marker,
   Meldung mit Batch-Nummer. Die DB bleibt unverändert.

**C) `Ausfuehrung = anzeigen`** (00er-Bereinigung) bleibt Anzeige. Fehlt der Marker, fragt das Skript in der Nachlauf-Liste: „Wurde <Id> auf
diesem System schon ausgeführt? [j/n]“. j → Marker `VonHand`.

**`Invoke-SqlTransaktion` lehnt eine Datei ab** (Abbruch vor der Ausführung, Prüfung als Text), wenn sie enthält:
- `sqlcmd`-Direktiven (Zeilen, die mit `:` beginnen; `$(`);
- `ROLLBACK`. Ein inneres `ROLLBACK` beendet die äußere Transaktion samt Marker.

Heute betrifft das keine der vier Dateien (Grep: `ROLLBACK` nur in der 00er-Bereinigung, `…Bereinigung00erArtikel.sql:157`; keine
Direktiven in `SQL/Einmalig`, `SQL/108`, `SQL/110`). Innere `BEGIN TRANSACTION … COMMIT` sind erlaubt: Sie zählen `@@TRANCOUNT` nur hoch und
wieder herunter (`…v1.47.4….sql:76-83`, `…Altbestand.sql:98-130`, `SQL/110:73-84`).

**Technische Lösung: SqlClient-Transaktion mit GO-Aufteilung statt `sqlcmd`** (Empfehlung, Gründe):
1. `sqlcmd -i` kann eine Transaktion nicht über eine interaktive Eingabe hinweg offen halten. Der Prozess endet mit dem Skript, und eine
   offene Transaktion wird beim Trennen zurückgerollt. Ein „Probelauf mit `ROLLBACK`, dann echter Lauf“ zeigt nicht dasselbe Ergebnis, das
   danach festgeschrieben wird: Zwischen den Läufen können sich Daten ändern.
2. Das Skript nutzt `System.Data.SqlClient` schon für Lesen und Backup (`:220-227`, `:394-399`). Es kommt keine neue Abhängigkeit dazu.
3. Die betroffenen Dateien brauchen nichts, was nur `sqlcmd` kann: keine `:r`/`:setvar`/`$(`, nur `GO`-Trenner (`SQL/108:12, 21, 35`) und
   `PRINT`. SqlClient setzt `QUOTED_IDENTIFIER ON` wie `sqlcmd -I`.
4. Nur so kann der Marker in derselben Transaktion stehen wie die Datenänderung (Auftrag Punkt 3).
5. Schema-Skripte und Reparatur-Dateien bleiben bei `sqlcmd` (bewährt, `-b`-Semantik). Mehr Umbau ist hier nicht verlangt.

**Reihenfolge und Voraussetzungen** bleiben, wie sie sind: Phasen `Vorher`/`Schema`/`Haltepunkt`/`Nachher`, `Braucht`, `Voraussetzung`.
In Schritt 4a laufen zuerst die Prüfungen, dann die Haltepunkt-Einträge, beide in Manifest-Reihenfolge.

### Marker-Tabelle `dbo.DeployScriptRuns`

Ersetzt Freigabe-Antwort 1 der Vorgänger-Spec (überholt laut deren Nachtrag 16:44). Eine Zeile je Manifest-Eintrag und DB. Der Nachweis liegt
in derselben DB und stimmt deshalb auch nach einem Restore.

| Spalte | Typ | Bedeutung |
|---|---|---|
| `Id` | int IDENTITY, PK | |
| `ManifestId` | nvarchar(200) NOT NULL, **UNIQUE** (`UX_DeployScriptRuns_ManifestId`) | `Id` aus `nachlauf.psd1`. UNIQUE verhindert Doppelläufe auch bei zwei parallelen Sitzungen. |
| `FileName` | nvarchar(400) NULL | `Datei` des Eintrags |
| `FileSha256` | nvarchar(64) NULL | SHA-256 der Originaldatei |
| `Mode` | nvarchar(30) NOT NULL | `Scharf` \| `Transaktion` \| `Entfaellt` \| `Spur` \| `VonHand` (Werte deutsch wie das Manifest-Vokabular) |
| `Hits` | int NULL | `TREFFER` des Trockenlaufs bzw. Spur-Wert |
| `Output` | nvarchar(max) NULL | gesammelte PRINT-/Ergebnis-Ausgabe |
| `PackageVersion` | nvarchar(20) NOT NULL | `Version` aus `publish-info.txt` |
| `PackageCommit` | nvarchar(64) NULL | `Commit` aus `publish-info.txt` |
| `ExecutedAt` | datetime2 NOT NULL | `SYSDATETIME()` |
| `ExecutedBy` | nvarchar(200) NOT NULL | `LEFT(SUSER_SNAME(), 200)` |
| `Note` | nvarchar(500) NULL | z. B. „von Hand erfasst“ |

**Wiederholung:**
- Ein Eintrag mit Marker wird nie wieder angeboten.
- Soll ein Skript bewusst ein zweites Mal laufen (z. B. `SQL/110` nach einem weiteren Epic, `SQL/110:7`), bekommt es einen neuen
  Manifest-Eintrag mit eigener `Id` (z. B. `SQL/110#2`). Die Regel ist so ohne Zusatzfeld eindeutig.
- `Verwerfen` und Fehler hinterlassen keine Zeile.

**Altstand (vor dem Marker):**
- **Spur vorhanden und > 0, kein Marker** → „Spur zeigt: lief schon (n). Als gelaufen vermerken? [j/n]“; j → Marker `Spur`. Vorhandene
  Spuren:
  - Altbestand: `CreatedBy = N'Migration-KO-Altbestand'` (`nachlauf.psd1:77`); das Skript selbst bricht dann ab (`…Altbestand.sql:47-51`).
  - 00er: Sicherungstabelle (`:98`). Nur Hinweis, die Tabelle kann laut Skriptkopf wieder fehlen.
  - `SQL/108` neu: `SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] IN (N''OseonRueckmeldungAktiv'', N''SageRueckmeldungAktiv'', N''StammdatenADGruppe'')) THEN 0 ELSE 1 END`.
    1 = nichts zu tun (lief schon oder war nie nötig; das Skript ist idempotent, `SQL/108:8`).
- **Ohne Spur** (v1.47.4: das DELETE hinterlässt keine): einmalige Erfassung durch Gerald über `e` (Fluss B) bzw. j (Fluss C).
  - Stand laut Freigabe-Antwort 4 der Vorgänger-Spec: auf keinem System gelaufen.
  - IDEAL-Test 11.11.: Altbestand-Trockenlauf 0, also entfällt (Aufgabe Z. 72). Beim nächsten Lauf liefert der Trockenlauf wieder 0, und
    der Marker `Entfaellt` entsteht dann.
- **Neuinstallation:** Die Tabelle ist leer (FreshInstall).
  - `Einmalig`-Einträge werden nie nach Version gefiltert (Vorgänger-Spec, Manifest-Abschnitt). Auf einem frischen System erscheinen sie
    deshalb einmal.
  - Altbestand und `SQL/110` erledigen sich über `TREFFER: 0` → `Entfaellt`.
  - v1.47.4 und 00er sind auf `System = Test` der Firma IDEAL beschränkt (`nachlauf.psd1:90, 95`).
  - Daten-Einträge (`SQL/108`) erscheinen nur bei einem Versionssprung über v1.79.0 (`Test-Neuer`, `:251`).
  - Vorbelegte Marker in der FreshInstall braucht es daher nicht.

### ADR-Vorschlag (Brain, Hauptcheckout; ADRs werden nie umgeschrieben)

Neuer **ADR 0023 „Daten- und Einmal-Skripte führt das Update-Skript mit Bestätigung am Zielserver aus; Marker-Tabelle“**. Er ersetzt in Teilen:
- ADR 0015, Abschnitt Entscheidung, Satz „Der Dev-Lauf legt das Skript an, führt es **nie** aus; die Ausführung ist ein Deploy-Schritt des
  Menschen“. Der Dev-Lauf führt weiterhin nie aus. Am Zielserver darf jetzt das Update-Skript ausführen, aber nur nach der Eingabe des
  Menschen dort: Trockenlauf, `SCHARF` mit Backup aus demselben Lauf, bzw. Transaktion mit `uebernehmen`.
- ADR 0015, Konsequenzen, „kein technischer Marker … bei häufigerem Bedarf neu entscheiden“: ersetzt durch `DeployScriptRuns`.
- ADR 0022, Entscheidung, „Daten- und Einmal-Skripte nie scharf durch das Skript“ und Konsequenz „Marker-Tabelle ist Folge-Spec“.

Unverändert gelten:
- Ordner `SQL/Einmalig/`, Warnkopf, Zählung, Audit-Felder, Reihenfolge im Kopf (ADR 0015);
- Agenten führen Daten- und Einmal-Skripte nie scharf aus, auf keinem System;
- kein unbeaufsichtigter Modus, kein Schalter, der Bestätigungen überspringt (Vorgänger-Spec, Ordner und Rechte, Betrieb).

ADR 0015 und ADR 0022 bekommen je eine Nachtragszeile „Teilweise abgelöst durch [[0023-…]] (2026-11-xx)“, nach dem Muster von ADR 0004
Z. 67. Den Text schreibt der Dev-Lauf; der Dateiname folgt dem ADR-Titel.

### Bugfixes aus dem ersten Zielsystem-Lauf

**1. StrictMode: Eigenschaft auf `$null`** (`Set-StrictMode -Version 2`, `Update-IdealAkeWms.ps1:31`; `Install-IdealAkeWms.ps1:92` mit
`Latest`). Scan des Ordners `scripts/deploy` nach `(<Ausdruck>).<Eigenschaft>`, der `$null` liefern kann:

| Fundstelle | Ausdruck | Kann `$null` sein? | Maßnahme |
|---|---|---|---|
| `Update-IdealAkeWms.ps1:110` | `(Get-Command sqlcmd … \| Select-Object -First 1).Source` | **ja**, wenn `sqlcmd` nicht im PATH ist (Befund Aufgabe Z. 56-57) | `Select-Object -First 1 -ExpandProperty Source`. Bei leerer Pipeline kommt `$null` ohne Fehler, dann greift die Pfadsuche `:111-114` und die Meldung `:115-118`. |
| `Update-IdealAkeWms.ps1:264` | `($manifest \| Where-Object { $_.Id -eq $b }).MigrationId` | ja, wenn ein `Braucht` auf eine unbekannte Id zeigt | Drift-Test „jede `Braucht`-Id existiert“ (unten); im Skript lesbarer Abbruch statt StrictMode-Fehler |
| `Update-IdealAkeWms.ps1:174` | `($dienste.Name)` im Abbruchtext bei 0 Treffern | Member-Enumeration auf leerem Array; ob PS 5.1 + StrictMode 2 hier wirft, ist **nicht gemessen** | im Dev-Lauf unter `powershell.exe` messen; wirft es, `@($dienste \| ForEach-Object { $_.Name })` |
| `Update-IdealAkeWms.ps1:67` | `($firmaZiele.System)` im Abbruchtext | wie `:174`, nur bei leerer Firma in `ziele.psd1` | wie `:174` |
| `Update-IdealAkeWms.ps1:178` | `(Get-ItemProperty … -ErrorAction SilentlyContinue).PSObject.Properties` | nur, wenn der Registry-Schlüssel des über Win32_Service gefundenen Dienstes fehlt; praktisch nicht | messen wie `:174`, sonst unverändert |
| `Update-IdealAkeWms.ps1:390` | `….Rows[0][0]` von `SERVERPROPERTY('InstanceDefaultBackupPath')` | DBNull → `''` → Pfad `\<DB>_….bak` | abgedeckt durch Bugfix 3 (leerer Pfad → Abfrage) |
| `Update-IdealAkeWms.ps1:259, 365` | `.Rows[0][0]` | `:259` liegt im `try`; `:365` `sys.databases` liefert für `DB_NAME()` immer eine Zeile | keine |
| `Install-IdealAkeWms.ps1:327` | `(Get-WebBinding … -Protocol https).AddSslCertificate(…)` | nein, die Bindung entsteht in `:325-326` | keine |
| `Install-IdealAkeWms.ps1:198, 204` | `$aspnet.Name`, `(Get-Item …).VersionInfo` | nein, abgesichert durch `:197`, `:200` | keine |
| `IdealAkeWms.Deploy.psm1:107, 210` | `(Get-Item "IIS:\AppPools\$pool").processModel`, `(Get-Item $edge).VersionInfo` | nein: Pool ermittelt bzw. `if ($edge)` | keine |

**2. `"$Name?"` / `"$Name:"` in Zeichenketten.** In PS 5.1 gehört `?` zum Variablennamen; `"$PaketVersion?"` liest also die nicht gesetzte
Variable `PaketVersion?` (StrictMode → Fehler). Scan `scripts/deploy`:
- `\$[A-Za-z_]\w*\?` → ein Treffer: `Update-IdealAkeWms.ps1:496` (`… v$PaketVersion?"`). Fix: `v${PaketVersion}?`.
- `\$[A-Za-z_]\w*:` → nur gültige Bereichs- und Laufwerksbezüge: `$script:` (`IdealAkeWms.Deploy.psm1:7-10, 56, 61, 93, 171, 184`;
  `Install-IdealAkeWms.ps1:107, 109, 431, 441, 445, 453, 493, 495`) und `$env:` (`IdealAkeWms.Deploy.psm1:121, 141, 208`;
  `Install-IdealAkeWms.ps1:195, 224, 293, 315, 387, 388, 456`; `Update-IdealAkeWms.ps1:230`). Kein Fehler. Wo ein Doppelpunkt nach einer
  Variablen steht, ist er schon geklammert (`Install-IdealAkeWms.ps1:141, 165`: `${zweck}:`, `${name}:`; `IdealAkeWms.Deploy.psm1:43-48`:
  `${pfad}:`).
- Drift-Test-Wächter gegen `\$[A-Za-z_]\w*\?` in `scripts/deploy/*` (unten).

**3. Backup-Pfad vorab prüfen** (Schritt 2, `:388-400`; auch in der Vorprüfung, damit `-NurPruefen` den IDEAL-Test-Fall zeigt):
- Pfad = `-BackupPfad`, sonst `InstanceDefaultBackupPath`. Ist er leer → fragen.
- Prüfung auf dem **SQL-Server**: `SELECT file_is_a_directory FROM sys.dm_os_file_exists(N'<Pfad>')` über `Invoke-Lesen`, Hochkomma
  verdoppelt.
  - `1` → weiter.
  - `0` → „Ordner <Pfad> fehlt auf <Server>“, dann Eingabe eines anderen Pfads (Schleife; leer = Abbruch vor dem Stoppen).
  - Abfrage scheitert (Version/Recht) → Warnung „Pfad nicht prüfbar: <Meldung>“ und Frage „Trotzdem diesen Pfad verwenden? [j/n]“; n →
    anderen Pfad eingeben.
- Bei `-NurPruefen` nur der Bericht.
- **Nicht belegt:** `sys.dm_os_file_exists` gibt es ab SQL Server 2017. IDEALSQL2\SAGE100TEST ist MSSQL15 = 2019 (Aufgabe Z. 47). Für
  AKESQL20 und die Rechte des ausführenden Kontos prüft Gerald vorab in SSMS als dieses Konto (Prüfschritt P-B1):
  `SELECT SERVERPROPERTY('ProductVersion') AS Version; SELECT * FROM sys.dm_os_file_exists(CAST(SERVERPROPERTY('InstanceDefaultBackupPath') AS NVARCHAR(4000)));`

**4. Zustand und Weg nach jedem unerwarteten Fehler.**
- Der `catch` im Hauptlauf (`:558`) ruft zusätzlich `Show-Zustand`. Ausgabe:
  - Zustand **live** gelesen: App-Pool (`Get-WebAppPoolState`), Site (`(Get-Website).state`), Dienst (`(Get-Service).Status`);
  - der zuletzt begonnene Schritt (Skriptvariable, gesetzt bei jedem `Write-Schritt` in `Invoke-System`);
  - fehlende MigrationIds, falls die History lesbar ist;
  - der Weg zum Weitermachen, je Schritt:

| Schritt beim Fehler | Weg (Text) |
|---|---|
| Ermittlung, 1, 2 | Nichts gestoppt, nichts geändert. Ursache beheben, Skript neu starten. |
| 3, 4, 4a | Web und/oder Dienst gestoppt. **Nicht von Hand starten** (`Migrate()` spielte fehlende Migrationen ohne Vorbedingungen ein, `Program.cs:203`). Ursache beheben, Skript neu starten: Es setzt bei den fehlenden Schema-Skripten, Prüfungen und Haltepunkten fort. |
| 5, 5a | Dateien ggf. teilweise kopiert, Web und Dienst gestoppt. Skript neu starten (kopiert erneut). |
| 6 | Wie gemeldet: App-Pool gestoppt, Dienst nicht gestartet. Log `<Web>\logs` bzw. `<Dienst>\logs` prüfen, Skript neu starten. |
| 7 | Web und Dienst laufen. Offene Nachlauf-Einträge: Skript neu starten; die Marker verhindern Doppelläufe. |

- Abbrüche vor dem ersten System (Paket, `sqlcmd`, `ziele.psd1`, Ermittlung; `:43-217`) melden lesbar „ABBRUCH: … Nichts gestoppt,
  nichts geändert.“ statt einer rohen Ausnahme mit Stack: Skriptweiter `trap`, Stack nur bei Nicht-`ABBRUCH:`-Fehlern wie `:558`. Das
  deckt QA-Befund N2 mit ab. F4 (kein Protokoll vor dem ersten System) bleibt offen.

**5. Kommissionierbereiche beim Web-Start.**
- In `Program.cs` nach dem OperationConfigs-Block (`:326-359`) dasselbe Muster: `if (!db.PickingAreas.Any())` legt Lager (L, 10,
  Standard), Glas (G, 20), Vorkommissionierung (VK, 30, `IstVorkommissionierung`) an. Werte wie `SQL/133:194-196`, `CreatedBy`/
  `CreatedByWindows = "system"`, `CreatedAt = DateTime.Now` wie `:353-355`.
- **Nicht** in der bestehenden Migration `20261107193219_AddPickingAreasAndOrders`. Systeme mit diesem History-Eintrag (IDEAL-Test)
  spielten eine geänderte Migration nie mehr ein; der Startup-Seed erreicht jedes System bei jedem Start.
- Der Startup-Seed läuft erst in Schritt 6. Für den Altbestand-Haltepunkt (4a) bleibt deshalb `Pruefung/Kommissionierbereiche` nötig.
- Bei nicht leerer Tabelle ändert der Seed nichts. Den Standardbereich schützt die Pflege (`PickingAreasController.cs:78`).

**Hinweis Überschneidung:** Die Aufgabe nennt einen „Befehl v1.98.2“ für `:110` und den Seed (Z. 56-57, 65). Hat ein v1.98.2-Lauf diese
Punkte vor dem Start dieser Spec schon gebaut, übernimmt der Dev-Lauf den Stand und baut nur den Rest.

### Nachlauf-Liste nur noch Einstellungen, Leitstand, Anwender-Info

Die Nachlauf-Liste (`Show-Nachlauf`, `:525-545`) zeigt danach:
- Einträge `Einstellung` (Oberfläche `/Settings`, `/ServiceSettings`, `/Roles`), `Leitstand` und `Info`;
- offene Daten- und Einmal-Einträge mit ihrem Fluss (A/B/C);
- einen Block „Prüfungen“ mit allen Abweichungen dieses Laufs.

Umzug der heutigen Manifest-Einträge (`nachlauf.psd1:108-198`):

| Eintrag | heute | neu |
|---|---|---|
| `v1.31.0-IDEAL-Schalter` (`:135-136`) | Einstellung inkl. „RCSI = 1“ | RCSI-Teil → `Pruefung/RCSI`; Text ohne RCSI, bleibt Einstellung |
| `v1.49.0-Anschrift` (`:141-142`) | Einstellung | → `Pruefung/Firmenanschrift` (anzeigen); Eintrag entfällt |
| `v1.51.0-Rolle` (`:143-145`) | Einstellung „anlegen und zuweisen“ | Anlegen → `Pruefung/RollenBeschichtungsauftrag`; Eintrag bleibt als Einstellung „zuweisen“ |
| `v1.60.0-IDEAL-Views` (`:152-153`) | Einstellung | `Sync:ArticlesViewName` leer → `Pruefung/ArticlesViewName`; P1–P5b auf Sage bleiben Hand-Schritt (kein Sage-Zugriff), Eintrag bleibt |
| `v1.69.0-Trennzeichen` (`:166-167`) | Info | → `Pruefung/QrTrennzeichenKomma`; Eintrag entfällt |
| `SQL/108` (`:80-84`) | Daten, anzeigen | Daten, `Transaktion`, `Spur` neu, Text ohne `{Sqlcmd}`-Handaufruf |
| `SQL/110` (`:85-89`) | Daten, Trockenlauf (nur Trockenlauf) | Daten, Trockenlauf (Fluss A mit `SCHARF`), Text neu |
| Altbestand (`:74-79`) | Einmalig, Haltepunkt, Trockenlauf | Fluss A; Text neu (kein SSMS-Handlauf mehr) |
| v1.47.4 (`:90-94`) | Einmalig, anzeigen | Einmalig, `Transaktion`, Text neu |
| 00er (`:95-100`) | Einmalig, anzeigen | unverändert anzeigen + Marker-Erfassung (Fluss C) |
| `v1.58.0-P123`, `v1.61.0-P5`, `v1.57.0-PT1`, `v1.77.0-Pruefung`, `v1.92.0-Final` | Info mit Verweis auf eine Prüfabfrage in einer Spec | **Kandidaten:** Liefert die Abfrage der Quell-Spec eindeutig eine Abweichungszahl, übernimmt der Dev-Lauf sie als `Pruefung` (anzeigen), sonst bleibt der Eintrag Info. Die Abfragetexte hat diese Spec nicht geprüft. |
| `v1.93.0-Zaehlung` (`:128-129`) | Info | bleibt Info; reine Zählung ohne Soll, ggf. mit vorhandenem Feld `Pruefabfrage` (`:536-539`) |
| `v0-Backup`, `v1.32.0-sysjobs`, `v1.39.0-Edge`, `v1.49.0-Branding`, `v1.71.0-AppsettingsProd`, `v1.96.0-FotosZaehlen`, `v1.96.0-FotosRest` | Info | bleiben; vom Skript selbst erledigt bzw. geprüft (Schritt 2, Sonderregel 90, 5a, Fotozählung `:463-507`) |
| alle übrigen Einstellung/Leitstand/Info (`v1.40.0`, `v1.44.0`, `v1.56.0`, `v1.58.1`, `v1.61.0-IDEAL-Altzeilen`, `v1.63.0-*`, `v1.64.0`, `v1.67.0`, `v1.68.0`, `v1.71.0-IDEAL-SData`, `v1.72.0`, `v1.73.0`, `v1.79.0`, `v1.84.0-*`, `v1.85.0`, `v1.88.0-Leitstand`, `v1.89.0-Altbestand`, `v1.90.0`, `v1.93.0-Mail`, `v1.96.0-Dienst`, `v1.97.0`) | | bleiben (Oberfläche, Leitstand, Info) |

`v1.71.0-IDEAL-SData` („SData-FA-Ansicht aus, kein SData-Host“) widerspricht der Ankündigung, dass IDEAL SData einschaltet (Aufgabe Z. 51-55).
Der Text bleibt hier unverändert, Zusatzfund.

### Kandidaten: Seeds/Default-Zeilen, die nur im SQL-Skript stehen

Durchsucht: alle `SQL/[1-9]*.sql` und `SQL/00_FreshInstall.sql` (Grep `INSERT [INTO] [dbo.]<Tabelle>` ohne `__EFMigrationsHistory`,
`MERGE`: keine Treffer); verglichen mit `InsertData`/`migrationBuilder.Sql` in `IdealAkeWms/Migrations` (16 Dateien) und den Startup-Seeds
`Program.cs:205-359`.

| Skript:Zeile | Was | in EF-Migration? | anderer Weg in jede DB | Vorschlag |
|---|---|---|---|---|
| `133:190-198`, FreshInstall `2902-2905` | Kommissionierbereiche Lager/Glas/VK | **nein** | keiner | **Pruefung** (Reparatur `SQL/133`) + **Startup-Seed** (Bugfix 5) |
| `86:15-24`, FreshInstall `2608-2616` | DUMMY-Artikel | **nein** (reines Datenskript, `86:2`) | keiner | **Pruefung** (Reparatur `SQL/86`) |
| – (kein Seed) | Rollen `beschichtungsauftrag`, `beschichtungsauftrag_schreiben` | nein | keiner (an IDEAL aus der Installation) | **Pruefung** AKE (Auftrag). Alternative, nicht gebaut: Startup-Rollenliste `Program.cs:221-236` |
| – (DB-Option) | RCSI | – | Installation/Hand | **Pruefung** mit Reparatur |
| `32:39-63`, FreshInstall `1554` | Rollen admin, masterdata, picking, stock, stock_keyuser, tracking, reporting | nein | Startup `Program.cs:221-254` | kein Kandidat |
| `37:54`, `42:203-213` | Rollen leitstand, bde_user/_shiftlead/_admin | nein | Startup `:231, 233-235` | kein |
| `67:16`, `74:16`, `77:23`, `78:12`; FreshInstall `1573-1625` | masterdata_read, lagerbestellung, glasbestellung, stock_read | ja (`Sql`, z. B. `20260608060227_AddMasterDataReadRole.cs:13-16`) | stock_read zusätzlich Startup `:228` | kein |
| `68:270-345`; FreshInstall `796-806` | WorkSteps, FaAttributeDefinitions/-Options, Rolle vorbau, FaWorkSteps-Übernahme | ja (`20260612102225_FaWorkStepsAndAttributes.cs:385-440`) | – | kein |
| `34:24`; FreshInstall `1639` | Arbeitsgang-Stammdaten (OSEON-Kürzel) | nein | Startup `:326-359` (bei leerer Tabelle) | kein |
| `60:197-329` | Satelliten-Backfill FA-Split | bedingt: Migration liest `Migrations/Scripts/60_…sql` (`20260512120355_AddProductionOrderSplit.cs:27-33`), ohne Datei nichts | – | kein; historisch, alle Systeme ≥ SQL/88 (Vorgänger-Spec) |
| `32:99-143` | UserRoles aus Alt-Rechtespalten | nein | – | kein; einmalige Überführung 2026-03 |
| `13:12, 24` | `NegativeBuchungErlaubt`, `NegativeBuchungLagerplatz` | nein | Katalog `seed: false` (`AppSettingDefinitions.cs:76-82`); Leser mit Default bei fehlender Zeile (`StockMovementsController.cs:338, 432`; `PickingTransferService.cs:172`) | kein |
| `19:6` | `BeschichtungAbholtage` | nein | Katalog ohne Seed (`AppSettingDefinitions.cs:63-66`); Leser mit Default (`FaWorklistController.cs:201`, `PickingLeitstandController.cs:106`, `ProductionOrdersController.cs:142`) | kein |
| `16:6`, `27:215, 223` | `StammdatenADGruppe`, `OseonRueckmeldungAktiv`, `SageRueckmeldungAktiv` | nein | – (ohne Leser, `SQL/108` löscht sie, `108:14-35`) | kein (Gegenteil) |
| `27:207`, `28:9`, `29:84, 88`, `36:117`, `37:63`, `38:50`, `42:219-229`, `45:34, 39`, `47:5`, `48:54`, `49:6`, `51:6`, `53:68`; FreshInstall `1055`, `2554-2566` | AppSettings (Teileverfolgung, QrMitFaNummer, OSEON-Ampel, Bestellungen, Leitstand, Zuweisung, BDE-Schalter, Schichtkalender, Reporting-Tage, Lagerbestellempfänger, Windows-Auth) | nein | Startup `Program.cs:295-307` aus `AppSettingDefinitions` (`seed: true`, `:99-170`, `:187-190`) | kein |
| `25:24-44`, `48:60-76`, `53:73`; FreshInstall `1106-1160` | ServiceSettings (Meldebestand, Sync-Schalter, Feiertage, Auto-Pause, Lagerbestell-Mail) | nein | Startup `Program.cs:311-324` aus `ServiceSettingDefinitions.All` (`:35-43, 97-111`) | kein |
| FreshInstall `1533`, `1543`, `1674` | Lagerplatz NAN, Benutzer admin, Admin-Rolle | nein | Startup `Program.cs:205-218, 256-291` | kein |

Ergebnis: Echte Lücken (nur SQL, kein anderer Weg) sind die **Kommissionierbereiche** und der **DUMMY-Artikel**. Dazu kommen die beiden
**Rollen** (nirgends geseedet) und **RCSI** (DB-Option).

## Technischer Lösungsentwurf

Muster: Ermittlung, Bestätigung und Protokoll wie v1.98.x. Lesen über `Invoke-Lesen`, neu `Invoke-SqlTransaktion` (SqlClient) für
Transaktion und Scharflauf, `Invoke-SqlDatei` (`sqlcmd`) für Schema und Reparatur-Dateien.

**Ablauf je System** (Änderungen gegenüber der Vorgänger-Spec, Tabelle „Ablauf je System“):

| # | Schritt | neu |
|---|---|---|
| 1 | Vorprüfung | Prüfungen lesend (Status je Zeile); Marker-Stand der Daten- und Einmal-Einträge (bzw. „Marker-Tabelle fehlt“); Backup-Pfad-Bericht; RCSI-Zeile `:365-367` entfällt zugunsten `Pruefung/RCSI` |
| 2 | Backup | Pfadprüfung (Bugfix 3); merkt sich „Backup in diesem Lauf: <Datei>“ für Fluss A/B |
| 3 | Stoppen | Stop-Regel erweitert um Prüfungen mit Reparatur (Phase Haltepunkt) |
| 4 | Schema | unverändert; `SQL/142` als letzter Schema-Eintrag |
| 4a | Prüfungen und Haltepunkt | zuerst Prüfungen (Phase Haltepunkt), dann Einmal-Haltepunkte nach Fluss A; die Eingabe `ausgefuehrt` (`:456-457`) entfällt |
| 5, 5a, 6 | unverändert | Bugfix `:496` |
| 7 | Nachlauf | Prüfungen Phase Nachher (lesend); Daten/Einmalig nach Fluss A/B/C; Liste nach dem Umzug oben; Block „Prüfungen“ |
| alle | Fehler | `Show-Zustand` (Bugfix 4) |

`-NurPruefen` bleibt rein lesend (AK 10 der Vorgänger-Spec): keine Reparatur, kein Trockenlauf, kein Marker. Die Trockenläufe sind
gefahrlos, laufen aber erst in 4a/7, damit `-NurPruefen` keine Skripte startet.

**Neue Funktionen:**
- `Invoke-SqlTransaktion($e, [string]$text, [string]$markerId, …)`:
  - SqlConnection mit `InfoMessage`-Handler, `BeginTransaction()`;
  - Batches über `Split-SqlBatches` (Regex `(?im)^\s*GO\s*$`), je Batch `ExecuteReader` und `NextResult`-Schleife in DataTables,
    `CommandTimeout = 0` (wie Backup `:396`);
  - Marker-INSERT parametrisiert;
  - Rückgabe Ausgabe und Transaktionsobjekt bzw. Commit-Modus.
  - Nur im Update-Skript, solange das Installationsskript es nicht braucht (keine Abstraktion auf Vorrat).
- `Show-Zustand($e)` (Bugfix 4).
- `Test-BrauchtNurLesend` (nicht werfend, für Vorprüfung und Prüfungen).

**Neue Ausfuehrung-Werte im Manifest:** `Transaktion` (Daten/Einmalig ohne Trockenlauf-Schalter) und `Reparatur` (Pruefung). Die
Vorlagenzeile in `secondbrain/_templates/spec.md` („Ausfuehrung (automatisch only for Schema|Trockenlauf|anzeigen)“) und
`.claude/agents/spec-agent.md` (DEPLOY SECTION) ziehen nach: Typ `Pruefung`; Ausfuehrung `automatisch` nur Schema; `Trockenlauf`/
`Transaktion`/`anzeigen` für Daten/Einmalig; `Reparatur`/`anzeigen` für Pruefung. Das geschieht im Hauptcheckout, wie Freigabe-Antwort 10
der Vorgänger-Spec.

**Drift-Test `NachlaufManifestDriftTests` (Erweiterung):**
1. `SchemaReihenfolge` (`:22-26`) um `"SQL/142"` am Ende ergänzen.
2. **Geänderter bestehender Test** `Manifest_DatenUndEinmalSkripte_NieAutomatisch` (`:97-106`) → `Manifest_DatenUndEinmalSkripte_NurMitBestaetigung`:
   - erlaubt `anzeigen`, `Trockenlauf`, `Transaktion`, nie `automatisch`;
   - Trockenlauf-Dateien enthalten `DECLARE @Trockenlauf BIT = 1;` **genau einmal** und `TREFFER:`;
   - Trockenlauf- und Transaktion-Dateien enthalten weder `ROLLBACK` noch `sqlcmd`-Direktiven.
3. Neu `Manifest_Pruefungen_Gueltig`:
   - jede `Pruefung` hat das Feld `Pruefung`, das den Schlüsselwort-Wächter aus `Update-IdealAkeWms.ps1:221` passiert (Regex im Test
     gespiegelt, Kommentar mit Fundstelle);
   - `Ausfuehrung` ist `Reparatur` (dann genau eines von `Reparatur`/`Datei`) oder `anzeigen`;
   - `Phase` ist `Haltepunkt` oder `Nachher`.
4. Neu `Manifest_BrauchtVerweistAufVorhandeneIds` (sichert `:264`).
5. `Manifest_IdsEindeutig_FelderGueltig` (`:108-116`): zusätzlich `Typ` ∈ {Schema, Daten, Einmalig, Pruefung, Einstellung, Leitstand, Info,
   Ordner}.
6. Neu `DeploySkripte_KeineVariableMitFragezeichenImText` (`\$[A-Za-z_]\w*\?` in `scripts/deploy/*` → rot).
7. Rotprobe: Ein entfernter Eintrag `SQL/142` macht `Manifest_EnthaeltJedeMigrationUndJedesSkript` rot (wie
   `Manifest_EntfernterEintrag_MachtWaechterRot`, `:66-72`).

**Zusatzidee (nicht bauen):** Auch Schema-Skripte über `Invoke-SqlTransaktion`/SqlClient laufen lassen. Dann entfiele `sqlcmd` als
Voraussetzung auf den Web-Servern ganz (Aufgabe Z. 13; Vorgänger-Spec Ermittlung Punkt 8). Eigene Entscheidung, falls gewünscht.

## Migrations-/SQL-Auswirkungen

- **Model:** `IdealAkeWms/Models/DeployScriptRun.cs` (Spalten oben). **Kein** `AuditableEntity`: Das Protokoll wird nur angehängt, nie
  geändert. Vorbild `SyncLog` (`Models/SyncLog.cs:5`) und `AppSetting` ([[fallstricke]] §8). `DbSet<DeployScriptRun> DeployScriptRuns`,
  Konfiguration in `ApplicationDbContext` (Längen, `HasIndex(ManifestId).IsUnique().HasDatabaseName("UX_DeployScriptRuns_ManifestId")`).
  Kein Repository: Die Anwendung liest und schreibt die Tabelle nicht, nur das Update-Skript (ADR 0001 nicht berührt).
- **Migration:** `dotnet ef migrations add AddDeployScriptRuns`. Rein additiv, kein `Sql(...)`, keine Index-Tricks ([[fallstricke]] §9 „Guards
  in beiden“ trifft nicht zu: neue Tabelle, neuer Index).
- **`SQL/142_AddDeployScriptRuns.sql`:**
  - `CREATE TABLE` im eigenen Batch mit `IF OBJECT_ID(N'[dbo].[DeployScriptRuns]', N'U') IS NULL`;
  - Index mit `sys.indexes`-Guard im eigenen Batch;
  - History-Insert in eigenem Batch;
  - `SET QUOTED_IDENTIFIER ON` wie `SQL/133:9-11`.
  - Nummer: 142 ist die nächste freie Nummer im Worktree (höchste Datei `141_AddPickingOrderAltbestandPruefen.sql`). Der Entwurf
    [[2026-11-08-wareneingang-teil-2-sage-we-beleg-rahmenabrufe-spec]] sieht für seine Etappe E2 ebenfalls „SQL/142 bzw. 114“ vor. Der
    Dev-Lauf prüft die Nummer beim Start (ADR 0004 Nachtrag: nächste freie Nummer zum Start).
- **`SQL/00_FreshInstall.sql`:** (a) Tabelle und Index im konsolidierten Schema, z. B. nach dem PickingAreas-Block (`:2724-2905`); (b)
  MigrationId nach `:3139-3141`.
- **Manifest:** Eintrag `SQL/142` (Schema, Firma beide, Version 1.99.0) am Ende der Schema-Liste.
- **`SQL/AgentJobs/*`:** nicht betroffen (kein Job liest oder schreibt die Tabelle; Inhalt: `02_Import_Artikel.sql`, `_archiv/`).
- **Datenskripte:** `SQL/110` und das Altbestand-Skript bekommen nur die Zeile `TREFFER` im Trockenlauf-Zweig. Der Scharf-Zweig bleibt
  unverändert, ebenso die Literal-Zeile `DECLARE @Trockenlauf BIT = 1;`.
- **Nicht daten-destruktiv** (neue leere Tabelle). Die Ausführung der Daten- und Einmal-Skripte am Ziel ist es teils; dafür gibt es das
  Backup aus demselben Lauf (Fluss A/B).

## Einstellungen

keine. Das Skript liest weiterhin nur `ConnectionStrings:DefaultConnection` und die Umgebung. Die Prüfungen lesen AppSettings und
ServiceSettings, ändern aber keine. Manifest und `ziele.psd1` sind Paketkonfiguration, keine Einstellungen nach ADR 0020.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

- `DeployScriptRuns`: kein `AuditableEntity` (oben). Wer und wann stehen in `ExecutedBy` und `ExecutedAt`.
- Reparatur Rollen: `CreatedAt = SYSDATETIME()`, `CreatedBy = N'Update-Skript'`, `CreatedByWindows = SUSER_SNAME()`. `Roles` verlangt
  `CreatedBy`/`CreatedByWindows` (`ApplicationDbContext.cs:126-127`).
- Reparatur über `SQL/133` bzw. `SQL/86`: deren Audit-Werte (`SQL133-Seed`, `System-Seed`).
- Startup-Seed Kommissionierbereiche: `system` wie `Program.cs:354-355`.
- Die ausgeführten Daten- und Einmal-Skripte bringen ihre Audit-Behandlung selbst mit (`SQL/110:76-78`, `…Altbestand.sql:35, 110, 125`).
  v1.47.4 löscht nur.
- Rollen und Zugriffsfilter: keine Änderung. Die Rollen-Reparatur legt nur fehlende Zeilen bestehender `RoleKeys` an. `controller.md` und
  `RoleOverview.cshtml` bleiben.
- Listen-View-Pattern: keine neue Tabellenansicht.

## Akzeptanzkriterien

1. `nachlauf.psd1` enthält die acht Prüfungen der Tabelle „Prüfungen dieser Spec“ mit den Feldern laut Feldtabelle; `ParseFile` unter
   `powershell.exe` 5.1 fehlerfrei, Datei ASCII.
2. Vorprüfung (auch `-NurPruefen`) zeigt je passender Prüfung `OK`, `Abweichung n`, `nach Schema` oder `Fehler`. `-NurPruefen` ändert nichts:
   kein Stoppen, keine Reparatur, kein Trockenlauf, kein Marker.
3. Abweichung mit Reparatur in Phase Haltepunkt → App-Pool **und** Dienst werden gestoppt, auch bei `-Komponente Web`.
4. In 4a laufen die Prüfungen vor den Einmal-Haltepunkten. Je Abweichung kommt eine Bestätigungsfrage. Vor der Reparatur steht die
   Live-Prüfung „App-Pool und Dienst Stopped“, sonst Abbruch. Nach der Reparatur liefert die Prüfung 0, sonst Abbruch mit gestopptem Web und
   Dienst. „n“ → Warnung in der Nachlauf-Liste.
5. Leere `PickingAreas` → nach 4a drei Bereiche, Lager aktiv und Standard. Der folgende Altbestand-Trockenlauf scheitert nicht mehr an
   `…Altbestand.sql:52-57`.
6. `Pruefung/RCSI`:
   - zeigt vor der Reparatur die anderen Sitzungen der DB, nennt `ROLLBACK IMMEDIATE` in der Frage, läuft ohne Transaktion;
   - danach `is_read_committed_snapshot_on = 1`;
   - liefert auf einer DB mit beiden Schaltern `false` die 0.
7. `Pruefung/RollenBeschichtungsauftrag` (AKE; ab v1.99.1 beide) legt nur fehlende Rollen an (`IsSystem = 1`, Audit laut Abschnitt); vorhandene bleiben
   unverändert. An IDEAL läuft die Prüfung nicht.
8. `Pruefung/DummyArtikel` legt über `SQL/86` den Artikel `DUMMY` an, wenn er fehlt.
9. Fluss A:
   - Trockenlauf ohne Rückfrage;
   - fehlende `TREFFER`-Zeile → nur Anzeige;
   - `TREFFER: 0` → „entfällt“ und nach j Marker `Entfaellt`; ein Haltepunkt läuft ohne Eingabe weiter.
10. Fluss A mit Treffer > 0:
    - ohne Backup aus diesem Lauf wird erst das Backup angeboten; abgelehnt → kein Scharflauf;
    - nur die Eingabe `SCHARF` startet den Scharflauf;
    - genau ein Literal wird im Speicher ersetzt, die Datei bleibt unverändert (Hash vorher = nachher);
    - Lauf und Marker `Scharf` in einer Transaktion, danach Commit, dann zweiter Trockenlauf mit `TREFFER`-Anzeige;
    - Fehler → Rollback, kein Marker;
    - andere Eingabe: beim Haltepunkt Abbruch bei gestopptem Web und Dienst, in Phase Nachher bleibt der Eintrag offen.
11. Fluss B:
    - Frage `j`/`n`/`e`; `e` → Marker `VonHand`;
    - `j` → alle Batches in einer Transaktion, PRINT und Ergebnismengen sichtbar, Marker in derselben Transaktion;
    - `uebernehmen` → Commit;
    - `verwerfen` oder leere Eingabe → Rollback, kein Marker, Daten unverändert (Prüfabfrage vorher = nachher);
    - Fehler → Rollback mit Batch-Nummer.
12. `Invoke-SqlTransaktion` lehnt Dateien mit `ROLLBACK` oder `sqlcmd`-Direktiven ab, bevor etwas ausgeführt wird.
13. Fluss C: Ohne Marker fragt die Nachlauf-Liste, ob das Skript schon lief; j → Marker `VonHand`.
14. Ein Eintrag mit Marker erscheint als „erledigt am … (Mode)“ und wird nicht angeboten. Ein zweiter Marker mit derselben `ManifestId`
    scheitert am UNIQUE-Index, das Skript rollt dann zurück.
15. Altstand: Spur > 0 ohne Marker → Angebot Marker `Spur`.
16. Ohne Marker-Tabelle (vor `SQL/142`) kein Absturz, sondern die Meldung „Marker-Tabelle fehlt (kommt mit SQL/142)“.
17. `SQL/142` läuft zweimal ohne Fehler; danach gibt es Tabelle, UNIQUE-Index und MigrationId. Die FreshInstall enthält Tabelle und
    MigrationId (Test `DeployScriptRunsMigrationSqlTests`, Muster `OperationConfigsMigrationSqlTests`).
18. Ohne `sqlcmd` im PATH und ohne Fallback-Pfad: Abbruch mit der Installationsmeldung (`:115-118`), kein StrictMode-Fehler.
19. Die Rückfrage nach der Version im Browser zeigt `v<Paketversion>?`, ohne StrictMode-Fehler. Wächter-Test gegen `$Name?` grün; eine
    eingebaute Testzeile macht ihn rot.
20. Backup-Pfad:
    - fehlender Ordner auf dem SQL-Server → Frage nach einem anderen Pfad, leer = Abbruch vor dem Stoppen;
    - leerer Standardpfad → Frage;
    - nicht prüfbar → Warnung und Frage;
    - `-NurPruefen` berichtet den Pfadstatus.
21. Jeder unerwartete Fehler im Hauptlauf zeigt den Live-Zustand von App-Pool, Site und Dienst, den letzten Schritt und den Weg laut Tabelle.
    Abbrüche vor dem ersten System zeigen „Nichts gestoppt, nichts geändert“ ohne Stack.
22. Web-Start mit leerer `PickingAreas` legt die drei Bereiche an; bei nicht leerer Tabelle ändert er nichts.
23. Manifest nach dem Umzug: Die Nachlauf-Liste enthält nur noch Einstellung, Leitstand, Info, offene Daten- und Einmal-Einträge und den Block
    „Prüfungen“. Die umgezogenen Einträge laut Tabelle sind entfernt bzw. gekürzt.
24. Drift-Test-Erweiterungen 1-7 grün; je eine Rotprobe (entfernter `SQL/142`-Eintrag; Trockenlauf-Datei ohne `TREFFER:`) macht ihn rot.
25. Kein Sage-Zugriff (bestehender Test grün). Der Grep der DEV-Protokolle zeigt keinen Scharf- oder Transaktionslauf eines echten Skripts aus
    `SQL/108`, `SQL/110` oder `SQL/Einmalig`, nur Trockenläufe und das synthetische Testskript.
26. `dotnet build IdealAkeWms.slnx` und `dotnet test` grün; `ParseFile` für alle Dateien in `scripts/deploy` fehlerfrei; ASCII.
27. ADR 0023 und die Verweis-Nachträge in ADR 0015/0022 stehen im Brain. Spec-Vorlage und `spec-agent.md` kennen Typ `Pruefung` und die neuen
    Ausfuehrung-Werte.

## Test-Szenarien

Neues Kapitel **TS-126 — Update-Skript prüft und führt aus, mit Bestätigung (v1.99.0)** in `docs/TESTSZENARIEN.md` (TS-125 ist das letzte
Kapitel, `docs/TESTSZENARIEN.md:11474`). Danach `secondbrain/tests/testszenarien-index.md`.

**Vorbedingungen (Agent):**
- **Nur DEV.** Eine Kopie der DEV-IDEAL-DB auf `.` (z. B. `IDEAL_WMS_TS126`, aus einem DEV-Backup), dazu eine Test-Anwendung und ein
  Testdienst unter `D:\Dev\deploytest` mit eigener `appsettings.DEV.json` auf diese Kopie. Muster TS-125.7 (Vorgänger-Spec, Nachweis
  DEV); danach entfernen.
- Paketkopie mit eigener `ziele.psd1`-Zeile und **eigenem Manifest**. Darin ein synthetischer Eintrag `Test/Synthetisch` (Datei nur in der
  Paketkopie), der in eine Tabelle `dbo.TS126_Probe` der Kopie-DB schreibt; einmal mit Trockenlauf-Schalter und `TREFFER`, einmal ohne.
- Echte Daten- und Einmal-Skripte nur im Trockenlauf.
- Agent bricht ab, wenn die angezeigte DB nicht `.`/`localhost` ist.

| Nr | Szenario | Erwartet | Wer |
|---|---|---|---|
| 126.1 | Kopie-DB: Kommissionierbereiche leer (vorher abhängige `PickingOrders`/`PickingOrderLines` der Kopie leeren), DUMMY gelöscht, RCSI aus und `Sync:HierarchicalFaEnabled = true`; `-NurPruefen` | Vorprüfung: drei Abweichungen, `RollenBeschichtungsauftrag` nicht gelistet (Firma IDEAL; ab v1.99.1 gelistet, siehe 126.21), Backup-Pfad-Bericht, nichts geändert, Pool/Dienst vorher = nachher | Agent |
| 126.2 | Volllauf auf die Kopie | 3. beide gestoppt; 4a: je Reparatur eine Frage; RCSI zeigt Sitzungen; danach drei Bereiche (Lager Standard), DUMMY da, RCSI 1; Altbestand-Trockenlauf mit `TREFFER: n` | Agent |
| 126.3 | Reparatur mit „n“ | Warnung, Lauf geht weiter, Warnung in der Nachlauf-Liste | Agent |
| 126.4 | `Pruefung/Standardbereich`: Bereiche vorhanden, keiner Standard | nur Hinweis, keine Reparatur | Agent |
| 126.5 | Fluss A, synthetisch, `TREFFER: 0` | „entfällt“, nach j Marker `Entfaellt`; zweiter Lauf: „erledigt am …“ | Agent |
| 126.6 | Fluss A, synthetisch, Treffer > 0, ohne Backup aus dem Lauf | Backup-Angebot; abgelehnt → kein Scharflauf; Eingabe `scharf` (klein) → kein Scharflauf (Nachher: offen; als Haltepunkt: Abbruch, beide gestoppt) | Agent |
| 126.7 | wie 126.6, Backup j, `SCHARF` | Probe-Zeilen geschrieben, Marker `Scharf` mit Hash, zweiter Trockenlauf `TREFFER: 0`; Datei-Hash unverändert | Agent |
| 126.8 | Fluss B, synthetisch, `verwerfen` | Ausgabe sichtbar, danach Probe-Tabelle unverändert, kein Marker | Agent |
| 126.9 | Fluss B, synthetisch, `uebernehmen` | Zeilen da, Marker `Transaktion` | Agent |
| 126.10 | Fluss B, `e` | Marker `VonHand`, nichts ausgeführt | Agent |
| 126.11 | Synthetisches Skript mit `ROLLBACK` bzw. `:r` | Ablehnung vor der Ausführung | Agent |
| 126.12 | Marker doppelt (Zeile von Hand vorab eingefügt, dann Fluss B `j`) | UNIQUE-Fehler → Rollback, Meldung, Daten unverändert | Agent |
| 126.13 | `-NurPruefen` gegen eine Kopie-DB ohne `SQL/142` (vor Schema) | „Marker-Tabelle fehlt …“, kein Absturz | Agent |
| 126.14 | `sqlcmd` nicht im PATH (Sitzung mit PATH ohne sqlcmd, Fallback-Pfade nicht vorhanden simuliert per Paketkopie) | lesbarer Abbruch `:115-118`, kein StrictMode-Fehler | Agent |
| 126.15 | Fußzeile nicht lesbar → Rückfrage Version | Text `… v1.99.0?`, kein Fehler | Agent |
| 126.16 | `-BackupPfad D:\GibtEsNicht` | „Ordner fehlt auf …“, Frage nach anderem Pfad; leer → Abbruch vor dem Stoppen | Agent |
| 126.17 | Fehler in Schritt 4 provozieren (synthetisches Schema-Skript mit Fehler in der Paketkopie) | Zustand: Pool Stopped, Dienst Stopped, Schritt 4, Weg „nicht von Hand starten … Skript neu starten“ | Agent |
| 126.18 | Web-Start mit leerer `PickingAreas` (Kopie-DB, Test-Anwendung) | drei Bereiche mit `CreatedBy = system`; zweiter Start ändert nichts | Agent |
| 126.19 | Drift-Test | grün; Rotproben (Eintrag `SQL/142` entfernt; `TREFFER:` aus Testkopie entfernt; `$x?` in Testkopie) | Agent |
| 126.20 | Erster Volllauf je Zielsystem v1.99.0 | Prüfungen laut Vorprüfung; IDEAL-Test: v1.47.4 per Fluss B (Ausgabe prüfen, Meldeliste an den Fachbereich, dann `uebernehmen` oder `verwerfen`), 00er per Fluss C; Test-IDEAL: Kommissionierbereiche-Prüfung | **Mensch** (Gerald) |

**Negativfälle** stehen in 126.3, 126.4, 126.6, 126.8, 126.11-126.17.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | kein Epic | – | – |

Empfohlene Commit-Reihenfolge im Dev-Lauf (kein Epic, nur zur Orientierung):
1. Bugfixes 1-5 und Startup-Seed;
2. Marker (Model, Migration, SQL/142, FreshInstall, Test);
3. Prüfungen;
4. Fluss A/B/C und Manifest-Umzug;
5. Drift-Test, TS-126, README, Version.

## Deploy

**Final v1.99.12 (qa-agent 2026-11-12, aus dem echten Diff `0dc0cea0..56906459`; ersetzt das Paket v1.99.2):**
- `deploy.web: true` (nur `IdealAkeWms/AppVersion.cs` und die Changelog-Karte); `deploy.service: true` (nur `IDEALAKEWMSService/AppVersion.cs`);
  `deploy.migration: false` fuer diesen Nachtrag (keine Datei unter `*/Migrations/`, kein SQL). Fachlich geaendert sind nur `deploy\Update-IdealAkeWms.ps1`
  und `deploy\nachlauf.psd1` im Paket. Das Paket ist das ganze Buendel v1.99.12 (alle Specs bis v1.99.11 mit ihren Migrationen); je Zielsystem
  gilt weiter der v1.99.0-Ablauf unten, nur mit diesem Paket.
- **Paket:** `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` (v1.99.12, `publish-info.txt` Commit
  `56906459440af1fff9242bcb6cdccb1848102af0`, Erstellt 2026-11-12 17:17; vorher geleert). Beide Komponenten, `SQL\`, `deploy\`.
- Neu fuer den Bediener: Ist `MailSettings:PickupDirectory` in der Dienst-`appsettings*.json` eines Prod-Systems gesetzt, bricht das Skript in
  der Vorpruefung ab (auch `-NurPruefen`), bevor etwas gestoppt wird. Wert von Hand entfernen, Skript neu starten. Der Deploy-Schritt steht als
  `Pruefung/MailPickup` in `nachlauf.psd1`.
- Nach dem Merge nur dann aus `main` neu publizieren, wenn der Merge getestete Dateien mit parallelen main-Aenderungen zusammenfuehrt
  (Befehle unten).

**Final v1.99.2 (qa-agent 2026-11-11, aus dem echten Diff `f79ba893..81b95ac2`; ersetzt das Paket v1.99.1):**
- `deploy.web: true` (nur `IdealAkeWms/AppVersion.cs` und die Changelog-Karte); `deploy.service: true` (nur `IDEALAKEWMSService/AppVersion.cs`);
  `deploy.migration: false` fuer diesen Nachtrag (keine Datei unter `*/Migrations/`, kein SQL). Fachlich geaendert ist nur
  `deploy\Update-IdealAkeWms.ps1` im Paket. Weil v1.99.0 noch auf keinem Zielsystem lief, gilt je Zielsystem der v1.99.0-Ablauf unten (inkl.
  `SQL/142`, P-B1), nur mit diesem Paket; die Frontmatter `deploy.*` bleibt deshalb fuer die ganze Spec `true`.
- **Paket:** `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` (v1.99.2, `publish-info.txt` Commit
  `81b95ac205e6841270e6dce6797f66305ee76e47`, Erstellt 2026-11-11 20:50; vorher geleert). Beide Komponenten, `SQL\`, `deploy\`.
- Neu fuer den Bediener: Die Vorpruefung (auch `-NurPruefen`) faehrt den Altbestand-Trockenlauf lesend. Treffer > 0 → in Schritt 2 Backup
  anlegen (Pflicht; n oder Fehler = Abbruch vor dem Stoppen). 0 Treffer → kein Haltepunkt, 4a fragt nur „als 'entfaellt' vermerken?“.
- Nach dem Merge nur dann aus `main` neu publizieren, wenn der Merge getestete Dateien mit parallelen main-Aenderungen zusammenfuehrt
  (Befehle unten).

**Final v1.99.1 (qa-agent 2026-11-11, aus dem echten Diff `55ec218a..f79ba893`; ersetzt das Paket v1.99.0 unten):**
- `deploy.web: true` (nur `IdealAkeWms/AppVersion.cs` und die Changelog-Karte); `deploy.service: true` (nur `IDEALAKEWMSService/AppVersion.cs`);
  `deploy.migration: false` (keine neue Datei unter `*/Migrations/`, kein neues SQL). Fachlich geaendert sind nur `deploy\Update-IdealAkeWms.ps1`
  und `deploy\nachlauf.psd1` im Paket. Weil v1.99.0 noch auf keinem Zielsystem lief, gilt fuer jedes Zielsystem der v1.99.0-Ablauf unten
  unveraendert (inkl. `SQL/142`, P-B1), nur mit diesem Paket.
- **Paket:** `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` (v1.99.1, `publish-info.txt` Commit
  `f79ba8938f4e27547aed686684d75724ffcae2d6`; die Binaries wurden aus `9071dcf7` gebaut, `f79ba893` aendert nur `docs/`). Beide Komponenten,
  `SQL\`, `deploy\`.
- Neu fuer den Bediener: Backup in Schritt 2 anlegen lassen. Ohne Backup aus demselben Lauf bleiben `SCHARF` (Fluss A) und `uebernehmen`
  (Fluss B, v1.47.4 an IDEAL-Test) gesperrt; „liegt vor“ reicht dafuer nicht mehr. An IDEAL-Systemen bietet 4a jetzt auch die Rollen-Reparatur an.
- Nach dem Merge nur dann aus `main` neu publizieren, wenn der Merge getestete Dateien mit parallelen main-Aenderungen zusammenfuehrt
  (Befehle unten).

**Final v1.99.0 (qa-agent 2026-11-11, aus dem echten Diff `e1ecc0c0..55ec218a`):**
- `deploy.web: true` (`IdealAkeWms/`: `Program.cs` Startup-Seed, `Models/DeployScriptRun.cs`, `ApplicationDbContext`, `AppVersion.cs`,
  `Views/Help/Changelog.cshtml`); `deploy.service: true` (`IDEALAKEWMSService/AppVersion.cs`; der Dienst referenziert das Web-Projekt und damit
  das geaenderte EF-Modell, [[fallstricke]] §5); `deploy.migration: true` (neu `IdealAkeWms/Migrations/20261111174202_AddDeployScriptRuns.cs`,
  `SQL/142_AddDeployScriptRuns.sql`, additiv). Dazu Paketinhalt `deploy\` (Update-Skript, Manifest) und `SQL\` (110 und Altbestand mit Zeile
  `TREFFER`).
- **Paket:** `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` (v1.99.0, Commit `55ec218a`; beide Komponenten,
  `SQL\`, `deploy\`, `publish-info.txt`). Ablauf Gerald: Paket auf den Zielserver kopieren, `deploy\Update-IdealAkeWms.ps1 -NurPruefen`, dann
  Volllauf (Schritt-Reihenfolge des Skripts: Backup -> Stoppen -> Schema inkl. `SQL/142` -> 4a Pruefungen/Reparaturen und Altbestand-Haltepunkt ->
  Kopieren -> 5a -> 5b Transaktion vor dem Start (v1.47.4 nur IDEAL-Test) -> Start -> Nachlauf). Vorher P-B1 (Manifest-Eintrag `v1.99.0-PB1`).
- Nach dem Merge nur dann aus `main` neu publizieren, wenn der Merge getestete Dateien mit parallelen main-Aenderungen zusammenfuehrt:
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` und
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`.

**Vorläufig** (Spec-Agent; der qa-agent bestätigt es am echten Diff):
- **Web-App:** ja (`Program.cs` Startup-Seed, EF-Modell, `AppVersion.cs`, Changelog-Karte).
- **Service:** ja (`AppVersion.cs`; der Dienst referenziert das Web-Projekt, [[fallstricke]] §5, also auch das geänderte EF-Modell).
- **Migration:** ja, `SQL/142_AddDeployScriptRuns.sql` (additiv). Das Update-Skript spielt es selbst als letzten Schema-Eintrag ein.
  Reihenfolge: Schema → Prüfungen → Haltepunkt → Kopieren → Start.
- **Deploy-Risiken:**
  - Die Migration ist additiv und unkritisch. Ohne `SQL/142` bricht das Skript wie gewohnt vor dem Kopieren ab (`:445-446`, `Migrate()`).
  - **RCSI-Reparatur** beendet mit `ROLLBACK IMMEDIATE` alle anderen Sitzungen auf der WMS-DB (SSMS-Fenster, Agent-Jobs). Das Skript
    zeigt sie vorher an.
  - **Fluss B** hält während der Rückfrage Sperren bei laufender App (Rückfrage 1).
  - **Scharfläufe** sind daten-ändernd; ohne Backup aus demselben Lauf gibt es keinen Scharflauf.
  - Der Startup-Seed schreibt nur bei leerer `PickingAreas`.
- **Prüfschritt vor dem ersten Lauf (Mensch, P-B1):** Abfrage aus Bugfix 3 je SQL-Server (AKESQL20, IDEALSQL2) als ausführendes Konto;
  dazu Recht `ALTER` auf die DB für RCSI: `SELECT HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'ALTER') AS AlterDb;`.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022):

| Id | Version | Typ | Firma | Phase | Ausfuehrung | Text (Kurz) |
|---|---|---|---|---|---|---|
| `SQL/142` | 1.99.0 | Schema | beide | Schema | automatisch | Marker-Tabelle `DeployScriptRuns`, additiv; MigrationId `<ts>_AddDeployScriptRuns`; Schema-Reihenfolge: nach `SQL/141` |
| `Pruefung/Kommissionierbereiche` | 1.88.0 | Pruefung | beide | Haltepunkt | Reparatur | leer → `SQL/133` erneut |
| `Pruefung/Standardbereich` | 1.88.0 | Pruefung | beide | Haltepunkt | anzeigen | Standard unter Stammdaten → Kommissionierbereiche festlegen |
| `Pruefung/RCSI` | 1.31.0 | Pruefung | beide | Haltepunkt | Reparatur | RCSI an, wenn FA-Struktur-Import oder Master an; `OhneTransaktion` |
| `Pruefung/RollenBeschichtungsauftrag` | 1.51.0 | Pruefung | beide (v1.99.1; bis v1.99.0 AKE) | Haltepunkt | Reparatur | fehlende Rollen anlegen |
| `Pruefung/DummyArtikel` | `<Version von SQL/86>`* | Pruefung | beide | Haltepunkt | Reparatur | fehlt → `SQL/86` |
| `Pruefung/QrTrennzeichenKomma` | 1.69.0 | Pruefung | beide | Nachher | anzeigen | Lagerplatz-Codes mit Komma bei Trennzeichen `,` |
| `Pruefung/Firmenanschrift` | 1.49.0 | Pruefung | IDEAL | Nachher | anzeigen | Anschrift „38, 4840“ unter /Settings korrigieren |
| `Pruefung/ArticlesViewName` | 1.60.0 | Pruefung | IDEAL | Nachher | anzeigen | `Sync:ArticlesViewName` unter /ServiceSettings setzen |
| `SQL/108` (geändert) | 1.79.0 | Daten | beide | Nachher | Transaktion | P-4 anzeigen, dann Fluss B; Spur neu |
| `SQL/110` (geändert) | 1.87.0 | Daten | beide | Nachher | Trockenlauf | Fluss A |
| Altbestand (geändert) | 1.88.0 | Einmalig | beide | Haltepunkt | Trockenlauf | Fluss A |
| v1.47.4 (geändert) | 1.47.4 | Einmalig | IDEAL, System Test | Nachher | Transaktion | Fluss B, Meldeliste an den Fachbereich |
| 00er (geändert) | 1.60.0 | Einmalig | IDEAL, System Test | Nachher | anzeigen | Fluss C |
| `v1.31.0-IDEAL-Schalter`, `v1.51.0-Rolle` (gekürzt) | wie bisher | Einstellung | wie bisher | Nachher | anzeigen | ohne RCSI bzw. nur „zuweisen“ |
| `v1.49.0-Anschrift`, `v1.69.0-Trennzeichen` | – | – | – | – | – | entfernt (→ Prüfungen) |
| `v1.99.0-Info` | 1.99.0 | Info | beide | Nachher | anzeigen | Betrieb: Update-Skript prüft und repariert nach Bestätigung, führt Daten-/Einmal-Skripte mit `SCHARF` bzw. `uebernehmen` aus; Marker in `DeployScriptRuns` |

\* Die Version von `SQL/86` belegt der Dev-Lauf aus dem Changelog (Teil-7-Bezug, Kopf `SQL/86:2`). Prüfungen werden nicht nach Version
gefiltert, das Feld dient nur als Beleg.
- **Publish-Befehle:** laut `qa-agent.md` Punkt 7 (Paket `<worktree>\publish\` mit `IDEALAKEWMSWeb\`, `IDEALAKEWMSWebService\`, `SQL\`,
  `deploy\`, `publish-info.txt`).
- **Brain (Hauptcheckout):**
  - ADR 0023 und die Verweis-Nachträge 0015/0022;
  - [[fallstricke]] §30 ergänzen, je mit Warum: StrictMode und `.Eigenschaft` auf `$null`; `"$x?"` liest Variable `x?`; „Seed nur im
    SQL-Skript fehlt nach `Migrate()`“; „offene Transaktion während einer Eingabe hält Sperren“;
  - feature-map, Testindex, Brain-Changelog v1.99.0, Aufgabe [[2026-11-11-buendel-deploy-update-skript]] (Stand je System).

## Offene Rueckfragen

1. **Phase der Transaktion-Einträge (`SQL/108`, v1.47.4).** Wie angeordnet bleibt es bei Phase `Nachher`. Dann laufen Web und Dienst,
   während die Transaktion auf `uebernehmen`/`verwerfen` wartet.
   - v1.47.4 löscht in `WorkOperations` (`…v1.47.4….sql:76-83`). Bei vielen Zeilen kann SQL Server die Sperre auf die Tabelle ausweiten, und
     BDE-Buchungen warten dann bis zur Entscheidung.
   - Alternative: beide Einträge nach dem Kopieren und Prüfen der Ordner (5a), **vor** dem Start (6), bei gestopptem Web und Dienst.
   - Der Skriptkopf von v1.47.4 verlangt nur, dass der neue Dienst deployt ist, bevor das Skript läuft (`:29-30`, „der alte
     Erkennungs-Lauf … legt … wieder an“). Das ist nach dem Kopieren erfüllt.
   - `SQL/108` hat keine Startbedingung.
   - **Empfehlung:** vor dem Start (neuer Abschnitt in 5a/6). Die Spec beschreibt bis zur Antwort Phase `Nachher`.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Phase der Transaktion-Einträge (SQL/108, v1.47.4): Nachher wie angeordnet oder vor dem Start bei gestopptem Web und Dienst (Empfehlung) → **Vor dem Start, bei gestopptem Web und Dienst (Gerald 2026-11-11 18:38). Eigener Abschnitt zwischen 5a und 6; Phase `VorStart` statt `Nachher`.**

## Umsetzung (Dev-Lauf 2026-11-11)

Buendel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Commits `1b285e9b` (Umsetzung) und `b30e310d` (feature-complete, zwei
Fixes aus den DEV-Laeufen) auf v1.98.1 `e1ecc0c0`. SQL-Nummer 142 beim Start geprueft (hoechste Datei `141_…`).

- **Freigabe-Antwort 1:** neuer Schritt **5b „Vor dem Start“** nach 5a, Phase `VorStart`: `SQL/108` und v1.47.4 (`Ausfuehrung =
  Transaktion`). Offene VorStart-Eintraege stoppen Web **und** Dienst auch bei `-Komponente Web`; vor der Ausfuehrung prueft das Skript live,
  dass beide gestoppt sind (`Assert-Gestoppt`). `SQL/110` bleibt `Nachher`.
- **Pruefungen** (8 laut Spec, `nachlauf.psd1`): Vorpruefung zeigt je Zeile OK/Abweichung/nach Schema/Fehler; Reparaturen in 4a vor den
  Haltepunkten, nur nach j und bei gestopptem Web und Dienst, danach erneute Pruefung = 0, sonst Abbruch. RCSI: listet vorher die anderen
  Sitzungen (eigener Pool wird vorher geleert), Frage nennt `ROLLBACK IMMEDIATE`, Reparatur ohne Transaktion. Block „Pruefungen“ in der
  Nachlauf-Liste mit dem Stand nach dem Lauf.
- **Fluss A/B/C** und Marker laut Spec (`Invoke-Eintrag`, `Invoke-FlussA`, `Invoke-FlussB`, `Invoke-SqlTransaktion`, `Write-Marker`).
  `Invoke-SqlTransaktion` lehnt `ROLLBACK` und `sqlcmd`-Direktiven vor der Ausfuehrung ab. Die Eingabe `ausgefuehrt` am Haltepunkt entfaellt.
  Einmalig-Haltepunkte werden nicht mehr nach Version gefiltert (wie die Nachlauf-Liste): der Marker verhindert die Wiederholung.
- **Marker:** `DeployScriptRun` (kein AuditableEntity), Migration `20261111174202_AddDeployScriptRuns`, `SQL/142_AddDeployScriptRuns.sql`,
  FreshInstall an beiden Stellen, Manifest `SQL/142` als letzter Schema-Eintrag.
- **Bugfixes:** `:110` `-ExpandProperty Source`; `:67`/`:174` `@(… | ForEach-Object { $_.X })` (unter PS 5.1 + StrictMode 2 **gemessen**:
  beide warfen bei leerem Array; `:178` wirft nicht, unveraendert); `:264` lesbarer Abbruch `Get-MigrationIdVon` + Drift-Test; `:496`
  `v${PaketVersion}?`; Backup-Ordner vorab (`sys.dm_os_file_exists`, Frage nach anderem Ordner, „nicht pruefbar“ → Warnung + Frage);
  `Show-Zustand` bei jedem Fehler im Systemlauf, `trap` fuer Abbrueche vor dem ersten System („Nichts gestoppt, nichts geaendert“, Stack nur
  bei unerwarteten Fehlern; deckt N2 ab); Startup-Seed Kommissionierbereiche (`Program.cs` nach dem OperationConfigs-Block).
- **Manifest-Umzug** laut Tabelle: `v1.49.0-Anschrift`, `v1.69.0-Trennzeichen` entfernt; `v1.31.0-IDEAL-Schalter` ohne RCSI,
  `v1.51.0-Rolle` nur „zuweisen“, `v1.60.0-IDEAL-Views` mit Verweis auf die Pruefung; `SQL/108` mit Spur; Texte der Daten-/Einmal-Eintraege
  neu; `v1.99.0-Info`. Die fuenf Kandidaten (`v1.58.0-P123`, `v1.61.0-P5`, `v1.57.0-PT1`, `v1.77.0-Pruefung`, `v1.92.0-Final`) bleiben
  **Info**: ihre Abfragen stehen in den Quell-Specs und wurden in diesem Lauf nicht auf eine eindeutige Abweichungszahl geprueft (Spec:
  „sonst bleibt der Eintrag Info“).
- `SQL/110` und Altbestand: Zeile `TREFFER` im Trockenlauf-Zweig (Scharf-Zweig und Literal unveraendert).
- Drift-Tests: `SchemaReihenfolge` + `SQL/142`; `Manifest_DatenUndEinmalSkripte_NurMitBestaetigung` (geaendert aus `…_NieAutomatisch`);
  neu `Manifest_TrockenlaufDateiOhneTreffer_MachtWaechterRot`, `Manifest_Pruefungen_Gueltig` (Waechter-Kopie gegen den Skripttext
  abgeglichen), `Manifest_BrauchtVerweistAufVorhandeneIds`, `DeploySkripte_KeineVariableMitFragezeichenImText` (mit Rotprobe),
  `Manifest_EntfernterEintrag142_MachtWaechterRot`; Typ-Liste in `Manifest_IdsEindeutig_FelderGueltig`. Neu `DeployScriptRunsMigrationSqlTests`.
- Version 1.99.0, Changelog-Karte, README (Update-Abschnitt), TS-126 in `docs/TESTSZENARIEN.md`.
- Brain (Hauptcheckout): ADR [[0023-daten-und-einmal-skripte-fuehrt-das-update-skript-mit-bestaetigung-aus]], Nachtraege in ADR 0015/0022,
  [[fallstricke]] §30 (+5), Vorlage `_templates/spec.md` und `.claude/agents/spec-agent.md` (Typ `Pruefung`, Phase `VorStart`,
  Ausfuehrung-Werte).

**Nachweis Build/Test** (Worktree): `dotnet build IdealAkeWms.slnx` 0 Fehler (28 Warnungen, Bestand); `dotnet test`: Web **2609/2610**
bestanden (1 uebersprungen, Bestand), Dienst **512/512**; Deploy-Tests 16/16. `ParseFile` unter `powershell.exe` 5.1: 0 Parsefehler fuer alle
5 Dateien in `scripts/deploy`; ASCII (Drift-Test).

**DEV-Laeufe TS-126** (nur DEV: Kopie `IDEAL_WMS_TS126` aus einem COPY_ONLY-Backup von `IDEAL_WMS_DEV`, Test-Site `TS126_WMS` :8093 mit Pool
`TS126`, Testdienst `IDEAL_WMS_TS126_Service` LocalSystem, Paketkopie `D:\Dev\deploytest\ts126\paket` mit eigener `ziele.psd1`-Zeile und
eigenem Manifest; Sage/enaio/SData/SMTP der Test-Anwendung auf nicht erreichbare Ziele umgestellt; danach alles entfernt – Site, Pool, Dienst,
DB, Login, Backups, Ordner):

| Nr | Ergebnis |
|---|---|
| 126.1 + 126.13 | `-NurPruefen` (DB vor SQL/142): Abweichung 1 bei Kommissionierbereiche, RCSI, DummyArtikel; Firmenanschrift 1 (von Hand); keine Rollen-Pruefung (IDEAL); „Marker-Tabelle fehlt (kommt mit SQL/142)“; Backup-Ordner „vorhanden“; Plan stoppt beide, Reparaturen, Haltepunkt Altbestand, vor dem Start v1.47.4; Exit 0, nichts geaendert |
| 126.2 | Volllauf: SQL/142 eingespielt; 4a je Reparatur eine Frage; SQL/133 → „Kommissionierbereiche … angelegt“ (Lager Standard); RCSI „Andere Sitzungen: 0“ → 1; SQL/86 → DUMMY; Altbestand-Trockenlauf `TREFFER: 0` → „entfaellt“; v1.47.4 mit n → offen; Start „Fusszeile v1.99.0“ |
| 126.3 | DummyArtikel mit n: „Reparatur abgelehnt, Abweichung 1 bleibt“, Lauf weiter, Abweichung im Block „Pruefungen“ |
| 126.4 | alle Bereiche ohne Standard, `-NurPruefen`: `Pruefung/Standardbereich` „Abweichung 1 – von Hand: …“, keine Reparatur |
| 126.5 | synthetisch `TREFFER: 0` → „entfaellt“, j → Marker `Entfaellt`; Folgelauf „erledigt am 11.11.2026 19:00 (Entfaellt, Treffer 0)“; ebenso der echte Altbestand-Trockenlauf (Marker `Entfaellt`) |
| 126.6 | Treffer 2/3 ohne Backup aus dem Lauf → „Kein Backup aus diesem Lauf“, n → kein Scharflauf, offen; `scharf` (klein) → „nicht SCHARF bestaetigt“, offen; Haltepunkt-Variante (Treffer 2, `nein`) → „ABBRUCH: Haltepunkt … Web und Dienst bleiben gestoppt“ + Zustand |
| 126.7 | Backup j, `SCHARF`: „Geschrieben: 3“, Commit, Marker `Scharf` Hits 3, `FileSha256` = Hash der Datei (unveraendert), zweiter Trockenlauf `TREFFER: 0` |
| 126.8 | Fluss B `verwerfen`: Ausgabe „B geschrieben: 2“ + Tabelle sichtbar, danach Probe-Tabelle leer, kein Marker |
| 126.9 | Fluss B `uebernehmen`: 2 Zeilen, Marker `Transaktion`; Folgelauf zeigt „erledigt“, Plan „vor dem Start“ ohne den Eintrag |
| 126.10 | `e` → Marker `VonHand` „von Hand erfasst“ |
| 126.11 | `ROLLBACK`-Datei und `:r`-Datei: „abgelehnt … Nichts ausgefuehrt“ |
| 126.12 | Skript schreibt Marker vorab → „Fehler in Batch 1 … Cannot insert duplicate key … UX_DeployScriptRuns_ManifestId“, Rollback, Probe-Zeile nicht in der DB |
| 126.14 | PATH ohne sqlcmd, Fallback-Pfade in einer Skriptkopie unbrauchbar: „ABBRUCH: sqlcmd nicht gefunden. Installieren: …“ + „Nichts gestoppt, nichts geaendert.“, Exit 1, kein StrictMode-Fehler (vorher mit nur PATH ohne sqlcmd: Fallback-Pfad genommen) |
| 126.15 | `-SiteUrl` auf eine Seite ohne Fusszeile: Schritt 6 fragt im Browser nach, Antwort j, kein Fehler |
| 126.16 | `-BackupPfad D:\GibtEsNicht`: Vorpruefung „FEHLT“; Schritt 2 „Ordner … fehlt auf .“, leere Eingabe → „ABBRUCH: kein Backup-Ordner angegeben.“, Zustand Schritt 2 „Nichts gestoppt“ |
| 126.17 | synthetisches Schema-Skript mit Fehler: „SQL/999 fehlgeschlagen“, Zustand: App-Pool Stopped, Dienst Stopped, letzter Schritt „4. Schema“, fehlende MigrationId, Weg „NICHT von Hand starten …“ |
| 126.18 | Web-Start mit leerer `PickingAreas`: Lager/Glas/VK mit `CreatedBy = system`, Lager Standard, VK Vorkommissionierung; Neustart aendert nichts (3 Zeilen, gleiche Ids) |
| 126.19 | Drift-Tests gruen, eingebaute Rotproben (Eintrag `SQL/142` entfernt, `TREFFER:` entfernt, `$x?`) |
| AK 6 | RCSI aus + fremde Sitzung (offene Transaktion): „Andere Sitzungen: 1 – 60 AKENB\gerald.weichbold PC-WEICHBOLD SQLCMD“, Reparatur j → fremde Sitzung beendet („kill state“), RCSI 1; Pruefabfrage gegen `AKE_WMS_DEV` (beide Schalter false) = 0 |
| AK 7 | Reparatur-SQL aus dem Manifest gegen die Kopie in Transaktion + ROLLBACK: Pruefung 2 → 0, Rollen mit `IsSystem = 1`, `CreatedBy = Update-Skript`; zweite Reparatur legt nichts an |
| AK 25 | Protokolle: „in EINER Transaktion“ nur fuer `Test/SynthA`, `Test/SynthB`, `Test/SynthBDup`; echte Skripte nur im Trockenlauf (Altbestand) |

Zwei Fehler fielen erst im DEV-Lauf auf und sind behoben (`b30e310d`, [[fallstricke]] §30): Cast-Reihenfolge bei `GetNewClosure()`
(der Abbruch zeigte nebenbei `Show-Zustand` im Schritt 5b), `.Count` auf einem einzelnen Batch.

**Zusatzfunde (nicht gebaut):**
- Z1: DEV `IDEAL_WMS_DEV` hat RCSI aus bei `ProduktionsauftragHierarchisch = true` – die Pruefung wird dort beim naechsten DEV-Lauf
  anschlagen.
- Z2 (**erledigt v1.99.1**, Freigabe-Nachtrag Punkt 1: Pruefung jetzt Firma beide): DEV IDEAL hat die Rollen `beschichtungsauftrag`/`_schreiben` nicht; die Spec nimmt an, IDEAL habe sie aus der Installation
  (`nachlauf.psd1`, Text `v1.51.0-Rolle`). Die Pruefung bleibt wie angeordnet AKE. Pruefabfrage fuer IDEAL-Test/-Prod:
  `SELECT [Key] FROM dbo.Roles WHERE [Key] LIKE N'beschichtungsauftrag%'`.
- Z3: Die Kandidaten aus dem Manifest-Umzug (P-1/2/3, P5, P-T1, P-E1, Final) sind nicht auf eine Abweichungszahl geprueft (Folge-Aufgabe).

## QA-Nachweis (qa-agent 2026-11-11)

Gepruefter Bereich: `e1ecc0c0..b30e310d` (Commits `1b285e9b`, `b30e310d`) plus QA-Fix `55ec218a`. Build und Tests ueber den ganzen
Buendel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`.

**1. Build und Tests** (Worktree, vor und nach dem QA-Fix gleich):
- `dotnet build IdealAkeWms.slnx`: 0 Fehler (28 bzw. 24 Warnungen, Bestand).
- `dotnet test`: IdealAkeWms.Tests **2609 bestanden / 1 uebersprungen / 0 Fehler** (2610); IDEALAKEWMSService.Tests **512 / 0 / 0**.
  Deploy-Filter (`NachlaufManifestDriftTests` + `DeployScriptRunsMigrationSqlTests`): 16/16.
- `ParseFile` unter `powershell.exe` 5.1.26100: 0 Parsefehler fuer alle 5 Dateien in `scripts/deploy`; `Import-PowerShellDataFile nachlauf.psd1`
  fehlerfrei (81 Eintraege nach dem Fix).

**2. Code-Review** (selbst, in diesem Lauf). Gelesen: `Update-IdealAkeWms.ps1` vollstaendig (902 Zeilen), `nachlauf.psd1`-Diff, `SQL/142`,
`00_FreshInstall.sql` (beide Stellen), Migration `20261111174202_AddDeployScriptRuns.cs`, `DeployScriptRun.cs`, `ApplicationDbContext`,
`Program.cs`-Seed, `SQL/110` und Altbestand (Zeile `TREFFER` gegen die Scharf-Menge: `#Kandidat` ist schon auf `ProductionOrderId IS NULL`
gefiltert, `SQL/110:27-30`, also TREFFER = UPDATE-Menge; Altbestand `COUNT(#Fa)` = INSERT-Menge), Drift-Tests, AppVersion, Changelog, TS-126.
PS-5.1-Messungen (StrictMode 2): Punktzugriff auf einen fehlenden Hashtable-Schluessel **wirft**; daher alle per `$m.X` gelesenen Manifest-Felder
je Verwendung gegen alle 81 Eintraege geprueft (Id/Version/Typ/Firma/Phase/Ausfuehrung ueberall, Quelle bei Listeneintraegen, Pruefung+Text bei
Pruefungen, Datei bei Trockenlauf/Transaktion): vollstaendig. Leere Rueckgabe von `Get-BrauchtFehlend`, `GetNewClosure`-Liste, `Split-SqlBatches`
mit CRLF und kleinem `go`: wie erwartet. Keine Manifest-Id trippt den Lese-Waechter.
Unabhaengige Probe `Invoke-SqlTransaktion` (Funktionen per AST aus dem Skript, PS 5.1 + StrictMode 2, Wegwerf-DB `QA_TS126_PROBE` auf `.`, danach
geloescht): Fehler nach einer Ergebnismenge, nur-DML-Fehler, Fehler im 2. Batch, `RAISERROR` nach SELECT -> jeweils Rollback, Probe-Tabelle
unveraendert, kein Marker; `verwerfen` und leere Eingabe -> Rollback; `uebernehmen` und `Commit` -> Daten + Marker `Transaktion`; `ROLLBACK` in
der Datei -> abgelehnt, nichts ausgefuehrt; Marker doppelt -> UNIQUE-Fehler, Rollback, Daten unveraendert. `SQL/142` zweimal auf der Wegwerf-DB:
Exit 0/0, Tabelle, UNIQUE-Index, MigrationId.
Standard-Defektklassen: keine Razor-/JS-Aenderung ausser der statischen Changelog-Karte (Skript-Einbettung, `hidden`+`d-*`, Positionsbindung
entfallen). Stille Fallbacks: Pruef-/Spur-Fehler werden sichtbar ("Fehler: …", "keine bzw. nicht lesbar"). Eine Regel an zwei Orten: Lese-Waechter
im Skript und im Test; der Test prueft das Literal im Skript (`Manifest_Pruefungen_Gueltig`).

| # | Schwere | Fundstelle | Befund | Behandlung |
|---|---|---|---|---|
| F1 | Mittel | `scripts/deploy/nachlauf.psd1` | Deploy-Schritt P-B1 der Spec (Abschnitt Deploy: SQL-Version, `sys.dm_os_file_exists`, `HAS_PERMS_BY_NAME … ALTER`) stand nicht im Manifest. Ohne ALTER-Recht fuehrt "j" bei der RCSI-Reparatur zum Abbruch in 4a bei gestopptem Web und Dienst (`Update-IdealAkeWms.ps1:440`). | **behoben** `55ec218a`: Vorher-Eintrag `v1.99.0-PB1` (erscheint in der Vorpruefung, solange installiert < 1.99.0; auf DEV, schon 1.99.0, daher nicht sichtbar). Drift-Tests gruen. |
| F2 | Niedrig | `Update-IdealAkeWms.ps1:382` | Scheitert der Marker-INSERT (UNIQUE), meldete das Skript "Fehler in Batch <letzter Daten-Batch>". | **behoben** `55ec218a` (`$nr = 'Marker'` vor dem INSERT); Probe: "Fehler in Batch Marker". |
| F3 | Niedrig | `Update-IdealAkeWms.ps1:680-683` | Vorpruefung listet Daten-Eintraege (`SQL/108`, `SQL/110`) als "offen", auch wenn `Test-Neuer` sie ausschliesst (installiert >= Version); sie werden weder angeboten noch ausgefuehrt, die Nachlauf-Liste laesst sie weg. Nur Anzeige. | offen (Kosmetik) |
| F4 | Niedrig | `Update-IdealAkeWms.ps1:473, 493, 499` | Das Backup-Angebot in Fluss A/B bricht den ganzen Systemlauf ab, wenn der Mensch "j" sagt und dann keinen Ordner angibt oder das BACKUP scheitert, auch in Schritt 7 (Web laeuft); Rest der Nachlauf-Liste und Block "Pruefungen" fehlen dann. `Show-Zustand` meldet den Zustand richtig. | offen (Neustart des Skripts setzt fort) |
| H1 | Hinweis | `Update-IdealAkeWms.ps1:578, 388` | Fluss B setzt Phase `VorStart` voraus (`Assert-Gestoppt`, Text "Web und Dienst gestoppt"); ein kuenftiger `Transaktion`-Eintrag in `Nachher` braeche ab. Der Drift-Test verbietet die Kombination nicht. | Hinweis fuer kuenftige Manifest-Eintraege |

**3. Beweisart je AK** ("Agent-DEV" = TS-126-Lauf des Dev-Laufs auf der DEV-Kopie bzw. QA-Lauf auf DEV; kein automatischer Beweis):

| AK | Beweis |
|---|---|
| 1 | automatisiert (`Manifest_Pruefungen_Gueltig`, `Manifest_IdsEindeutig_FelderGueltig`, ASCII-Test) + QA `ParseFile`/`Import-PowerShellDataFile` 5.1 |
| 2 | Agent-DEV: Dev 126.1; QA `-NurPruefen` AKE-DEV und IDEAL-DEV (unten), danach Marker 0, RCSI unveraendert, nichts gestoppt |
| 3 | Code-Review (`:690-693`) + Dev 126.2 (Komponente Beide); **`-Komponente Web` nicht gelaufen -> Manual-UAT** |
| 4, 5, 6, 7, 8 | Agent-DEV (Dev 126.2, 126.3, AK 6, AK 7, 126.2) |
| 9, 10, 11 | Agent-DEV (Dev 126.5-126.10) + QA-Probe `Invoke-SqlTransaktion`; echte Trockenlaeufe QA: Zeile `TREFFER` vorhanden, Datei-Hash unveraendert |
| 12 | Agent-DEV (Dev 126.11) + QA-Probe |
| 13 | **nur Code-Review** (`:528-531`) -> Manual-UAT (126.20, 00er an IDEAL-Test) |
| 14 | Agent-DEV (Dev 126.5, 126.12) + QA-Probe (UNIQUE) |
| 15 | **nur Code-Review** (`:519-524`) -> Manual-UAT (Systeme < v1.79.0 mit `SQL/108`-Spur 1) |
| 16 | Agent-DEV (Dev 126.13) |
| 17 | automatisiert (`DeployScriptRunsMigrationSqlTests`, nur Text) + QA `SQL/142` zweimal auf Wegwerf-DB + DEV-Migrate auf beiden DEV-DBs (Tabelle, `UX_…`, MigrationId) |
| 18, 21 | Agent-DEV (Dev 126.14, 126.16, 126.17) |
| 19 | automatisiert (`DeploySkripte_KeineVariableMitFragezeichenImText` mit Rotprobe) + Dev 126.15 |
| 20 | Agent-DEV (Dev 126.16 fehlender Ordner; QA `-NurPruefen` "vorhanden"); leerer Standardpfad und "nicht pruefbar" **nur Code-Review** |
| 22 | Agent-DEV (Dev 126.18); DEV-Deploy: nicht leere `PickingAreas` unveraendert (3 Zeilen `SQL133-Seed` auf beiden DEV-DBs) |
| 23 | QA `-NurPruefen` (Nachlauf-Liste nur Einmalig + Block "Pruefungen") + Drift |
| 24, 26 | automatisiert gruen (oben) |
| 25 | automatisiert (Sage-Test) + Grep der QA-Protokolle: kein "in EINER Transaktion", kein Scharflauf; echte Skripte nur im Trockenlauf |
| 27 | Brain-Commit `6b0befaf` im Hauptcheckout (ADR 0023, Nachtraege 0015/0022, Vorlage, `spec-agent.md`) |

**4. Geaenderte bestehende Tests:** `Manifest_DatenUndEinmalSkripte_NieAutomatisch` -> `…_NurMitBestaetigung` (Verhalten absichtlich
geaendert, Drift-Erweiterung 2 / AK 24); `Manifest_IdsEindeutig_FelderGueltig` (Phase `VorStart`, Typ-Liste; Erweiterung 5 / AK 24);
`SchemaReihenfolge` um `SQL/142` (Erweiterung 1). Kein "anderes".

**5. Checkliste:** Migration + `SQL/142` mit Guards, History im eigenen Batch; FreshInstall beide Stellen; AgentJobs nicht betroffen;
`DeployScriptRun` bewusst kein AuditableEntity, Seed mit `CreatedBy/CreatedByWindows = system`; Version 1.99.0 in beiden `AppVersion.cs`,
Anwender-Changelog, Brain-Changelog [[2026-11-11-v1-99-0-update-skript-prueft-und-fuehrt-aus]]; feature-map; TS-126 in `docs/TESTSZENARIEN.md`
und im Testindex; README. Keine neue `SQL/Einmalig`-Datei. **5a ADR 0020:** keine Einstellung neu oder geaendert.

**6. DEV** (`standort: beide`):
- `dev-deploy.ps1 -Standort AKE` und `-Standort IDEAL` aus dem Worktree (Stand `b30e310d`), erhoehte Shell: Exit 0/0, "HTTP 200, Fusszeile
  v1.99.0", DEV-Absicherung angewandt, Dienste `Running`. Der QA-Fix `55ec218a` aendert nur `scripts/deploy` (nicht Teil der DEV-Sites); der
  Code der Sites ist identisch.
- DB nach Deploy: `20261111174202_AddDeployScriptRuns` in der History, `DeployScriptRuns` + `UX_DeployScriptRuns_ManifestId` (unique), 0 Marker,
  `PickingAreas` 3 Zeilen (Lager Standard), beide DEV-DBs.
- `Update-IdealAkeWms.ps1 -NurPruefen` aus `publish\deploy` gegen DEV (`-System Test -WebPfad D:\Dev\<F>\Web -DienstPfad D:\Dev\<F>\Service
  -SiteUrl http://localhost:808x/`, DB laut Ermittlung `.`): Exit 0.
  - AKE-DEV: Kommissionierbereiche, Standardbereich, RCSI, Dummy, Komma OK; **RollenBeschichtungsauftrag Abweichung 2** (DEV hat die Rollen
    nicht); Plan: beide stoppen, Reparatur Rollen, Haltepunkt Altbestand. OK.
  - IDEAL-DEV: **RCSI Abweichung 1** (Z1), **Firmenanschrift Abweichung 1** (von Hand), Rest OK, keine Rollen-Pruefung (Firma IDEAL); Plan vor
    dem Start: v1.47.4. OK.
  - Danach unveraendert: Marker 0, RCSI 0, keine `Migration-KO-Altbestand`-Auftraege (beide DBs).
- Echte Trockenlaeufe (Datei unveraendert, Hash gleich): Altbestand AKE-DEV `TREFFER: 15`, IDEAL-DEV `TREFFER: 0`; `SQL/110` AKE-DEV `TREFFER: 0`,
  IDEAL-DEV `TREFFER: 1`. Kein Scharf- oder Transaktionslauf eines echten Skripts, keine Reparatur auf DEV (RCSI-Reparatur auf IDEAL_WMS_DEV
  bewusst nicht bestaetigt).
- Browser: kein Szenario dieser Spec braucht den Browser (Fusszeile per HTTP im Deploy und in der Vorpruefung gelesen).
- AKE-DEV-Sync-Schalter: nicht angefasst (setzt der Koordinator nach diesem Lauf).

**7. Publish:** `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` neu (Ordner vorher geleert): `IDEALAKEWMSWeb\`,
`IDEALAKEWMSWebService\`, `SQL\`, `deploy\`, `publish-info.txt` (Version 1.99.0, Commit `55ec218ae1420051398e167366252d8ff925f5e8`, Branch
`feature/2026-08-07-ideal-teile-1-5`, Erstellt 2026-11-11 19:17). Manifest: `SQL/142`, `v1.99.0-Info`, `v1.99.0-PB1` vorhanden.

**Manuelle Checkliste (Gerald, Schranke 2):**
1. P-B1 je SQL-Server (AKESQL20, IDEALSQL2\SAGE100TEST) als ausfuehrendes Konto: Version >= 2017, `sys.dm_os_file_exists` lesbar,
   `HAS_PERMS_BY_NAME(<DB>, 'DATABASE', 'ALTER')`.
2. Je Zielsystem zuerst `-NurPruefen`; Pruefungen und Plan mit den Erwartungen vergleichen (IDEAL-Test: RCSI, Kommissionierbereiche, v1.47.4
   vor dem Start; AKE: Rollen).
3. TS-126.20 Volllauf je Zielsystem v1.99.0 (IDEAL-Test: v1.47.4 per Fluss B mit Meldeliste, dann `uebernehmen`/`verwerfen`; 00er per Fluss C,
   deckt AK 13; Test-IDEAL: Kommissionierbereiche-Reparatur; AKE: Rollen-Reparatur, danach Zuweisen unter /Roles).
4. AK 15: auf einem System mit offenem `SQL/108` und Spur 1 das Angebot "als gelaufen vermerken (Marker Spur)" beobachten.
5. Optional AK 3: ein Lauf mit `-Komponente Web` und offener Reparatur stoppt App-Pool **und** Dienst.
6. AK 20 Rest (leerer Standard-Backup-Pfad, "nicht pruefbar") nur, falls es am Ziel auftritt.
7. Zusatzfunde Z1 (RCSI auf IDEAL_WMS_DEV), Z2 (Rollen an IDEAL), F3/F4 (offen, Niedrig) entscheiden.
8. Danach Merge (Schranke 2).

## Freigabe-Nachtrag 2026-11-11 (Gerald, Chat): Nachbesserung v1.99.1

Aus dem QA-Bericht v1.99.0; sonst bleibt alles wie Testbereit.
1. `Pruefung/RollenBeschichtungsauftrag` fuer **Firma beide** statt nur AKE. Befund: DEV-IDEAL (Kopie IDEAL-Test vom 29.10.) hat die Rollen
   nicht; nur IDEAL-Prod hat sie aus der Installation (Teil 2). Spec-Text und Manifest nachziehen.
2. Vorpruefung: `SQL/108` und `SQL/110` nicht als „offen“ anzeigen, wenn der Versionsfilter sie ausschliesst. Anzeige und Ausfuehrung nach
   derselben Regel (eine Funktion).
3. Backup-Angebot: „j“ ohne Ordner bricht den Lauf nicht ab. Nach dem Ordner fragen, bis er gueltig ist, oder „ueberspringen“ anbieten. Ohne
   Backup aus dem Lauf bleiben `SCHARF` und `uebernehmen` gesperrt, mit klarer Meldung.
Tests auf DEV, Version 1.99.1, Changelog, Publish, DEV wiederherstellen + 5 AKE-DEV-Sync-Schalter. Grenzen wie bisher.

## Umsetzung Nachtrag v1.99.1 (Dev-Lauf 2026-11-11)

Commit `9071dcf7` im Buendel (auf `55ec218a`).
1. `nachlauf.psd1`: `Pruefung/RollenBeschichtungsauftrag` Firma `beide`, Text „nur IDEAL-Prod hat sie aus der Installation“;
   `v1.51.0-Rolle` Text nachgezogen.
2. `Update-IdealAkeWms.ps1`: **eine** Funktion `Test-Faellig` (Einmalig immer, sonst `Test-Neuer`) fuer Vorpruefung, Plan
   (`Get-OffeneEintraege`), 4a, 5b und Nachlauf-Liste. Nicht faellige Eintraege zeigt die Vorpruefung als „nicht faellig (eingefuehrt mit vX,
   installiert vY)“ statt „offen“.
3. `Get-BackupOrdner`: leere Eingabe = „Backup uebersprungen“ (`$null`), Frage wiederholt sich bis zu einem gueltigen Ordner; `Invoke-Backup`
   liefert `$true`/`$false` (Fehler = Warnung, kein Abbruch). Schritt 2: uebersprungen/fehlgeschlagen → „Liegt ein aktuelles Backup vor?“
   (n → Abbruch vor dem Stoppen wie bisher). `Test-BackupFuerLauf`: **SCHARF und uebernehmen nur mit Backup aus diesem Lauf** (laut
   Nachtrag; ein in Schritt 2 nur als „liegt vor“ bestaetigtes Backup reicht fuer Fluss B nicht mehr, Abweichung von B3), sonst Meldung „Ohne
   Backup aus diesem Lauf bleibt SCHARF/uebernehmen gesperrt …“, Eintrag offen, Lauf geht weiter (am Haltepunkt Abbruch wie bisher).
4. Version 1.99.1, Changelog-Karte, README, TS-126.21–126.25.

**Nachweis:** `dotnet build IdealAkeWms.slnx` 0 Fehler; `dotnet test` Web **2609/2610** (1 uebersprungen, Bestand), Dienst **512/512**;
`ParseFile` 0 Fehler. DEV-Laeufe auf einer neuen Kopie `IDEAL_WMS_TS126` (aus `IDEAL_WMS_DEV` v1.99.0, Aufbau wie oben, danach entfernt),
Paket v1.99.1 (`9071dcf7`) mit synthetischen Eintraegen `Test/SynthA` (Nachher, Trockenlauf, 3 Treffer) und `Test/SynthB` (VorStart,
Transaktion):

| Nr | Ergebnis |
|---|---|
| 126.21 | `-NurPruefen` (IDEAL): `[Pruefung/RollenBeschichtungsauftrag] Abweichung 2 - wird in 4a … repariert`; Volllauf j → Reparatur in EINER Transaktion, „repariert (vorher 2, jetzt 0)“, beide Rollen in der Kopie |
| 126.22 | „SQL/108: nicht faellig (eingefuehrt mit v1.79.0, installiert v1.99.0)“, „SQL/110: nicht faellig (… v1.87.0 …)“; Plan und Nachlauf-Liste ohne beide |
| 126.23 | SynthA `TREFFER: 3`, Backup-Angebot j, Ordner `D:\GibtEsNicht` fehlt, leer → „Backup uebersprungen“, „Ohne Backup aus diesem Lauf bleibt SCHARF gesperrt …“, Eintrag offen, Lauf zu Ende (Exit 0) |
| 126.24 | Schritt 2 nur „liegt vor“; SynthB j → „Kein Backup aus diesem Lauf“, n → „bleibt uebernehmen gesperrt“, nichts ausgefuehrt (Probe 0 Zeilen, 0 Marker), Lauf weiter, Start v1.99.1 |
| 126.25 | Schritt 2 Backup j mit `-BackupPfad D:\GibtEsNicht`: „Ordner … fehlt“, Eingabe `D:\AuchNicht` → „fehlt“, leer → „Backup uebersprungen“, dann „Liegt ein aktuelles Backup vor?“ j, Lauf weiter |

## QA-Nachweis Nachtrag v1.99.1 (qa-agent 2026-11-11)

Gepruefter Bereich: `55ec218a..9071dcf7` (Commit `9071dcf7`) plus QA-Fix `f79ba893` (nur `docs/`). Build und Tests ueber den ganzen
Buendel-Worktree.

**1. Build und Tests** (Worktree, Stand `9071dcf7`):
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (Bestand).
- `dotnet test`: IdealAkeWms.Tests **2609 bestanden / 1 uebersprungen / 0 Fehler** (2610); IDEALAKEWMSService.Tests **512 / 0 / 0**
  (darin `NachlaufManifestDriftTests`, `DeployScriptRunsMigrationSqlTests`).
- `ParseFile` unter `powershell.exe` 5.1.26100.9711: 0 Parsefehler fuer alle 5 Dateien in `scripts/deploy`; `Import-PowerShellDataFile nachlauf.psd1`:
  81 Eintraege.

**2. Code-Review** (selbst, in diesem Lauf). Gelesen: Diff `Update-IdealAkeWms.ps1` und `nachlauf.psd1` mit Umgebung (`Test-Neuer :262`,
`Invoke-Pruefung :415-448`, Backup-Block `:451-506`, Fluss A/B `:546-591`, Vorpruefung/Plan `:651-705`, Schritt 2 `:715-724`, 4a, 5b,
`Show-Nachlauf`), `Read-Eingabe`/`Read-Ja`/`Write-*` im Modul (nur Host-Ausgabe, keine Pipeline-Verschmutzung der Rueckgabewerte), Changelog-Karte,
README, TS-126.21–126.25, AppVersion beider Projekte.
- Auftragspunkt 1: Manifest `Firma='beide'`, Texte nachgezogen; Rollen-Pruefung erscheint an IDEAL (DEV-Beleg unten). Der 4a-Text nennt
  „Zuweisen bleibt Hand-Schritt (unter /Roles)“ (`:419`), daher reicht `v1.51.0-Rolle` mit Firma AKE.
- Auftragspunkt 2: `Test-Faellig` (`:510`) ist die einzige Regel fuer Vorpruefung (`:688`), Plan (`Get-OffeneEintraege :511`), 4a, 5b und
  Nachlauf-Liste; per Grep keine verbliebene Inline-Kopie (`Test-Neuer` nur noch in `Test-Faellig` und in der Vorher-Liste `:692`, die keine
  Daten-Eintraege enthaelt). Bei unbekannter Version liefert `Test-Neuer` true; „nicht faellig … installiert v“ mit leerer Version kann nicht entstehen.
- Auftragspunkt 3: `Get-BackupOrdner` fragt bis gueltig, leer = `$null`; `Invoke-Backup` liefert genau einen Bool, Fehler nur als Warnung;
  Schritt 2 faellt bei uebersprungen/fehlgeschlagen auf „Liegt ein aktuelles Backup vor?“ zurueck (n = Abbruch vor dem Stoppen wie bisher);
  `Test-BackupFuerLauf` gibt nur bei `$script:BackupDatei` frei. **Verschaerfung Fluss B:** Der Nachtrag sagt woertlich „Ohne Backup aus dem Lauf
  bleiben `SCHARF` und `uebernehmen` gesperrt“. Die Umsetzung (nur Backup aus diesem Lauf, „liegt vor“ reicht nicht mehr) entspricht dem Wortlaut;
  B3 der Spec ist mit Vermerk „ab v1.99.1“ nachgezogen.
- Unabhaengige Probe (PS 5.1 + StrictMode 2, Funktionen per AST aus dem Skript, Wegwerf-DB `QA_V1991_PROBE` auf `.` und Ordner
  `D:\Dev\deploytest\qa_v1991_bak`, beide danach geloescht): `-BackupPfad D:\GibtEsNicht` + Eingabe `D:\AuchNicht` + leer → „Backup uebersprungen“,
  Rueckgabe `False` (ein Objekt); ungueltig → gueltiger Ordner → Backup geschrieben, `True`; „nicht pruefbar“ (Zugriff verweigert) → „Trotzdem?“
  n → erneute Ordnerfrage; `BACKUP` scheitert (DB fehlt) → Warnung „Backup fehlgeschlagen …“, `False`, kein Abbruch; `Test-BackupFuerLauf` n →
  „bleibt uebernehmen gesperrt“ `False`, j → `True`, j + Fehler → „bleibt SCHARF gesperrt“ `False`; `Test-Faellig`: Daten 1.79.0 bei 1.99.0 False,
  Daten 1.99.1 True, Einmalig alt True, Daten bei unbekannter Version True.
- Standard-Defektklassen: keine Razor-/JS-Aenderung ausser der statischen Changelog-Karte; keine Einstellung.

| # | Schwere | Fundstelle | Befund | Behandlung |
|---|---|---|---|---|
| F5 | Niedrig | `docs/TESTSZENARIEN.md:11535, :11550` | TS-126.1 erwartete „keine Rollen-Pruefung (IDEAL)“, TS-126.16 „leer → Abbruch vor dem Stoppen“; beides widerspricht v1.99.1 (126.21, 126.25), der manuelle Test waere als FEHLER gelaufen. | **behoben** `f79ba893` (Erwartung je „ab v1.99.1 …“ mit Verweis) |
| F6 | Niedrig | Spec (AK 7, Deploy-Tabelle, TS-Tabelle 126.1, B3) | Auftragspunkt 1 verlangt „Spec-Text nachziehen“; AK 7, Deploy-Tabelle und 126.1 nannten noch AKE, B3 noch „liegt vor“. | **behoben** in dieser Spec (Vermerke „ab v1.99.1“) |
| H2 | Hinweis | `Update-IdealAkeWms.ps1:721` | `$script:BackupLiegtVor` wird noch gesetzt, aber nirgends mehr gelesen (toter Zustand). | offen (Kosmetik) |
| H3 | Hinweis | `Update-IdealAkeWms.ps1:549, 560` | Am **Haltepunkt** (Altbestand mit Treffer > 0) fuehrt „Backup j, Ordner leer“ weiterhin zum Abbruch mit gestopptem Web und Dienst. Ursache ist die Haltepunkt-Regel (ohne `SCHARF` Abbruch), nicht der fehlende Ordner; die Meldung ist klar, ein Neustart setzt fort. Vom Dev-Lauf dokumentiert („am Haltepunkt Abbruch wie bisher“). Vorbeugung: Backup in Schritt 2 anlegen. | Entscheidung Gerald |

F3 und F4 aus v1.99.0 sind mit Punkt 2 bzw. 3 erledigt (F4 ausser am Haltepunkt, siehe H3).

**3. Beweisart je Auftragspunkt** (kein automatisierter Test fuer das Skriptverhalten):

| Punkt | Beweis |
|---|---|
| 1 Rollen-Pruefung beide | QA `-NurPruefen` IDEAL-DEV (Abweichung 2, Plan „Reparaturen: … Pruefung/RollenBeschichtungsauftrag“) + Dev 126.21 (Reparatur auf der Kopie) + Drift-Tests (Manifest gueltig) |
| 2 „nicht faellig“, eine Regel | QA `-NurPruefen` AKE- und IDEAL-DEV (`SQL/108`, `SQL/110` „nicht faellig“, Nachlauf-Liste ohne beide) + Code-Review (eine Funktion) + QA-Probe `Test-Faellig` |
| 3 Backup ueberspringen, Sperre | QA-Probe (echtes `BACKUP` und `sys.dm_os_file_exists` gegen `.`) + Dev 126.23–126.25 (Kopie-DB, Volllauf) + Code-Review Schritt 2 |

**4. Geaenderte bestehende Tests:** keine (Diff ohne Testdateien).

**5. Checkliste:** keine Migration, kein SQL; Version 1.99.1 in beiden `AppVersion.cs`; Anwender-Changelog-Karte; Brain-Changelog
[[2026-11-11-v1-99-1-update-skript-nachbesserung]]; README; TS-126.21–126.25 in `docs/TESTSZENARIEN.md` und im Testindex; kein neuer
Deploy-Schritt (die Manifest-Aenderungen sind selbst der Inhalt). **5a ADR 0020:** keine Einstellung.

**6. DEV** (`standort: beide`):
- `dev-deploy.ps1 -Standort AKE` und `-Standort IDEAL` aus dem Worktree (Stand `9071dcf7`), erhoehte Shell: Exit 0/0, „HTTP 200, Fusszeile
  v1.99.1“, DEV-Absicherung angewandt, Dienste `Running`, App-Pools `Started`.
- `Update-IdealAkeWms.ps1 -NurPruefen` aus `publish\deploy` (`-System Test -WebPfad D:\Dev\<F>\Web -DienstPfad D:\Dev\<F>\Service -SiteUrl
  http://localhost:808x/ -LogPfad D:\Dev\deploytest\qa_v1991_logs`), Exit 0/0:
  - AKE-DEV: installiert v1.99.1; Rollen Abweichung 2, Rest OK; `SQL/108`/`SQL/110` „nicht faellig (eingefuehrt mit v1.79.0 / v1.87.0, installiert
    v1.99.1)“; Altbestand offen; Plan: Reparatur Rollen, Haltepunkt Altbestand, vor dem Start nichts. **OK**
  - IDEAL-DEV: RCSI Abweichung 1 (Z1), **Rollen Abweichung 2** (neu, Punkt 1), Firmenanschrift 1 (von Hand); `SQL/108`/`SQL/110` „nicht faellig“;
    Plan: Reparaturen RCSI und Rollen, Haltepunkt Altbestand, vor dem Start v1.47.4; Nachlauf-Liste nur die drei Einmalig-Eintraege. **OK**
  - Danach unveraendert: Marker 0, RCSI 0, Rollen 0 (beide DBs), nichts gestoppt.
- TS-126.23–126.25 (Volllauf mit Backup-Varianten) auf den echten DEV-DBs: **NICHT MOEGLICH** im Rahmen (ein Volllauf stoppt und repariert);
  belegt durch den Dev-Lauf auf der Kopie-DB und die QA-Probe oben.
- Browser: kein Szenario braucht den Browser.
- AKE-DEV-Sync-Schalter: nicht angefasst (setzt der Koordinator).

**7. Publish:** `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` neu (vorher geleert): `IDEALAKEWMSWeb\`,
`IDEALAKEWMSWebService\`, `SQL\`, `deploy\`, `publish-info.txt` (Version 1.99.1, Commit `f79ba8938f4e27547aed686684d75724ffcae2d6`, Branch
`feature/2026-08-07-ideal-teile-1-5`, Erstellt 2026-11-11 19:53). Binaries aus `9071dcf7` (DLL-Stempel); `f79ba893` aendert nur `docs/`.
Manifest im Paket: Rollen-Pruefung Firma `beide`.

**Manuelle Checkliste (Gerald, Schranke 2; ersetzt die v1.99.0-Liste oben, deren Punkte 4–6 gelten weiter):**
1. P-B1 je SQL-Server wie oben.
2. Je Zielsystem `-NurPruefen` mit dem Paket v1.99.1: IDEAL-Test zeigt jetzt auch die Rollen-Pruefung; `SQL/108`/`SQL/110` „nicht faellig“, wo
   installiert >= Version.
3. TS-126.20 Volllauf je Zielsystem; dabei in Schritt 2 das Backup anlegen lassen (sonst bleibt `uebernehmen` fuer v1.47.4 an IDEAL-Test gesperrt).
   An IDEAL-Test nach der Rollen-Reparatur die Rollen unter /Roles zuweisen.
4. Optional TS-126.23–126.25 an einem Testsystem (leerer Ordner → uebersprungen, Lauf geht weiter).
5. H2/H3 entscheiden; Z1 (RCSI auf IDEAL_WMS_DEV) weiter offen.
6. Danach Merge (Schranke 2).


## Freigabe-Nachtrag 2026-11-11 20:40 (Gerald): Altbestand-Trockenlauf in der Vorprüfung, v1.99.2

Aus dem QA-Bericht v1.99.1 („zur Entscheidung“). Gilt vor dem QA-Abschnitt.
1. Der Trockenlauf des Einmal-Skripts Altbestand (v1.88.0) läuft schon in der **Vorprüfung**, lesend und vor dem Stoppen, wenn der Eintrag für
   das System ansteht (Version, Marker).
2. Liefert er Treffer, wird das Backup in Schritt 2 zur **Pflicht**. Wird es abgelehnt oder schlägt es fehl, bricht der Lauf **vor dem
   Stoppen** ab, Web und Dienst laufen weiter. Am Haltepunkt bleibt die Regel „kein Überspringen“ unverändert.
3. Bei 0 Treffern: „entfällt“ schon in der Vorprüfung, mit Marker-Eintrag laut Spec. Kein Halt am Haltepunkt.
4. Die ungenutzte Variable `$script:BackupLiegtVor` entfernen.
5. DEV-Test mit Treffern (AKE-DEV) und mit 0 Treffern (IDEAL-DEV). Version 1.99.2.

## Umsetzung Nachtrag v1.99.2 (goal-Lauf 2026-11-11)

Commit `81b95ac2` im Buendel (auf `f79ba893`).
- Vorpruefung: fuer offene Haltepunkt-Eintraege mit `Ausfuehrung = Trockenlauf` (heute nur Altbestand) laeuft der Trockenlauf schon hier, lesend,
  vor dem Stoppen (auch bei `-NurPruefen`; ersetzt fuer diesen Fall AK 2 „kein Trockenlauf“ laut Nachtrag). Ergebnis je Eintrag in
  `$script:VorTrocken`: 0 → „entfaellt, kein Halt“, Eintrag faellt aus `$halte` (stoppt nicht mehr); Treffer oder nicht bestimmbar (Exit ≠ 0,
  `Braucht` fehlt noch) → Backup Pflicht, Haltepunkt in 4a wie bisher.
- Schritt 2: bei Pflicht nur „Jetzt BACKUP …?“; n, uebersprungen oder fehlgeschlagen → „ABBRUCH: Backup ist Pflicht (…), aber nicht angelegt.
  Nichts gestoppt, Web und Dienst laufen weiter.“ Ohne Pflicht unveraendert (Backup oder „liegt vor“).
- 4a: Eintraege mit Vorpruefung 0 → „entfaellt (TREFFER: 0 in der Vorpruefung), kein Halt“, Frage „als 'entfaellt' vermerken?“ → Marker
  `Entfaellt` (nach dem Schema, also mit SQL/142). Am Haltepunkt mit Treffern gilt weiter „kein Ueberspringen“.
- `$script:BackupLiegtVor` entfernt (Punkt 4). Version 1.99.2, Changelog-Karte, README, TS-126.26/126.27.

**Nachweis:** Build 0 Fehler; `dotnet test` Web **2609/2610** (1 uebersprungen, Bestand), Dienst **512/512**; `ParseFile` 0 Fehler.

| Nr | Ergebnis |
|---|---|
| 126.26 | AKE-DEV (Testpaket mit DEV-`ziele.psd1`), `-NurPruefen`: „Trockenlauf in der Vorpruefung (lesend)“, `TREFFER: 15`, „Backup in Schritt 2 Pflicht, Haltepunkt in 4a“, Plan „Backup PFLICHT (…Altbestand)“. Volllauf, Backup n: „ABBRUCH: Backup ist Pflicht (…), aber nicht angelegt. Nichts gestoppt, Web und Dienst laufen weiter.“, Zustand App-Pool Started, Dienst Running, Exit 1 |
| 126.27 | IDEAL-DEV `-NurPruefen`: `TREFFER: 0` → „entfaellt, kein Halt“, Plan „Haltepunkt: keiner“. Volllauf auf einer Kopie (`IDEAL_WMS_TS126`, danach entfernt): 4a „entfaellt (TREFFER: 0 in der Vorpruefung), kein Halt“, j → Marker `Entfaellt` (Note „TREFFER 0 in der Vorpruefung“), Start Fusszeile v1.99.2, Exit 0 |

## QA-Nachweis Nachtrag v1.99.2 (qa-agent 2026-11-11)

Gepruefter Bereich: `f79ba893..81b95ac2` (nur der Freigabe-Nachtrag 20:40). Build und Tests ueber den ganzen Buendel-Worktree, Stand `81b95ac2`.

**1. Build und Tests:**
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 24 Warnungen (Bestand, NuGet-Hinweise).
- `dotnet test`: IdealAkeWms.Tests **2609 bestanden / 1 uebersprungen / 0 Fehler** (2610); IDEALAKEWMSService.Tests **512 / 0 / 0**.
- `ParseFile` unter `powershell.exe` 5.1.26100.9711: 0 Parsefehler fuer alle 5 Dateien in `scripts/deploy`.

**2. Code-Review** (selbst, in diesem Lauf). Gelesen: Diff `Update-IdealAkeWms.ps1:627, :696-717, :729-740, :792-801` mit Umgebung
(`Get-OffeneEintraege :511`, `Get-BrauchtFehlend :282`, `Test-Trockenlauf :288`, `Invoke-Trockenlauf :540`, `Get-Treffer :352`,
`Invoke-FlussA :547`, `Get-Marker`/`Write-Marker :322-341`, `-NurPruefen :719`, `Show-Zustand :297`), das Altbestand-Skript
(`SQL/Einmalig/…Altbestand.sql:33-98`: Trockenlauf-Zweig nur `#Fa` und `PRINT`, `RETURN` vor `BEGIN TRANSACTION :101`, also lesend),
`SQL/142` (Spalte `Mode` ohne CHECK, `Entfaellt` zulaessig), Changelog-Karte, README, TS-126.26/27, AppVersion beider Projekte.
- PS 5.1 + StrictMode 2: `$null -eq 0` / `$null -ne 0` liefern False/True (fehlender bzw. nicht bestimmbarer Trockenlauf → Pflicht);
  `Get-Treffer` liefert `[int]` oder `$null`, kein String-Vergleich; `@()` um `$halte`, `$backupPflicht` und die `foreach`-Quelle;
  `$script:VorTrocken` je System neu (`:627`). Unabhaengige Probe (Zeilen 698-708 woertlich aus der Datei, PS 5.1 StrictMode 2, Stubs):
  0 Treffer → `halte` leer, keine Pflicht; 15 → Halt + Pflicht; Exit 1, Exit 0 ohne `TREFFER`, `Braucht` fehlt → je Halt + Pflicht;
  leere Liste → nichts; Transaktion-Eintrag neben 0-Treffer-Eintrag → nur der Transaktion-Eintrag bleibt Haltepunkt, keine Pflicht.
- Punkt 1: Trockenlauf nur fuer Eintraege aus `Get-OffeneEintraege 'Haltepunkt'` (Firma, `Test-Faellig`, Marker, Spur) mit
  `Ausfuehrung = Trockenlauf`, vor dem `-NurPruefen`-Ende und vor Schritt 2/3. Punkt 2: Pflicht-Zweig `:732-736` ohne „liegt vor“-Ausweg;
  `Stop-Mit` in Schritt 2 → `Show-Zustand` „Nichts gestoppt“. 4a mit Treffern unveraendert `Invoke-Eintrag $e $m $true` (kein Ueberspringen).
  Punkt 3: 0 Treffer → aus `$halte`; 4a `:794-798` Marker `Entfaellt` (Hits 0, Note) nach Bestaetigung, erst nach dem Schema (Tabelle aus
  `SQL/142`). Punkt 4: `BackupLiegtVor` per Grep in `scripts/` nicht mehr vorhanden (H2 aus v1.99.1 erledigt). H3 aus v1.99.1 erledigt (mit
  Treffern ist das Backup vor dem Stoppen Pflicht).
- Standard-Defektklassen: keine Razor-/JS-Aenderung ausser der statischen Changelog-Karte; keine Einstellung (5a entfaellt).

| # | Schwere | Fundstelle | Befund | Behandlung |
|---|---|---|---|---|
| F7 | Niedrig | Hauptcheckout `secondbrain/changelog/`, `secondbrain/tests/testszenarien-index.md:128` | Brain-Changelog v1.99.2 fehlte, Testindex nannte nur 126.1–126.25. | **behoben** durch QA (neue Notiz [[2026-11-11-v1-99-2-update-skript-altbestand-vorpruefung]], Index 126.26–126.27) |
| H4 | Hinweis | `Update-IdealAkeWms.ps1:723` / `Show-Nachlauf` | Bei `-NurPruefen` zeigt die Nachlauf-Liste den Altbestand weiter als „offen (Haltepunkt, Trockenlauf)“, obwohl die Vorpruefung „TREFFER: 0 – entfaellt“ meldete (IDEAL-DEV). Im Volllauf steht der Marker vor Schritt 7, dort korrekt. | offen (Kosmetik) |
| H5 | Hinweis | `Update-IdealAkeWms.ps1:701` | Fehlt die Datei oder das Trockenlauf-Literal, wird der Eintrag still „nicht bestimmbar“ (Backup Pflicht) ohne eigene Zeile in der Vorpruefung; der Plan nennt die Id, 4a meldet den Grund. Mit dem Paket heute nicht erreichbar. | offen (Kosmetik) |
| H6 | Hinweis | `Update-IdealAkeWms.ps1:700-704` | Pflicht auch bei „nicht bestimmbar“ (Exit ≠ 0, keine `TREFFER`-Zeile, `Braucht` fehlt); der Nachtrag nennt nur „Treffer“. Strenger, im Dev-Abschnitt dokumentiert; bei einem System vor v1.88.0 (SQL/133/141 fehlen) ist das Backup damit stets Pflicht, passt zu ADR 0004 („133/Altbestand“). | akzeptiert |

**3. Beweisart je Nachtrag-Punkt** (kein automatisierter Test fuer Skriptverhalten):

| Punkt | Beweis |
|---|---|
| 1 Trockenlauf in der Vorpruefung, lesend, vor dem Stoppen | QA `-NurPruefen` AKE- und IDEAL-DEV (Zeile „Trockenlauf in der Vorpruefung (lesend)“, Protokolldatei in `D:\Dev\deploytest\qa_v1992_logs`), Code-Review Skript-Zweig lesend |
| 2 Treffer → Backup Pflicht, Abbruch vor dem Stoppen | QA-Volllauf AKE-DEV mit Backup n (unten), Probe |
| 3 0 Treffer → entfaellt, kein Halt, Marker | QA `-NurPruefen` + Volllauf bis Schritt 2 an IDEAL-DEV (Plan „Haltepunkt: keiner“, Schritt 2 ohne Pflicht); Marker in 4a: Dev-Lauf auf Kopie-DB (126.27) + Code-Review; durch QA auf DEV **NICHT MOEGLICH** (Volllauf stoppt) |
| 4 `BackupLiegtVor` entfernt | Grep |
| 5 DEV-Test, Version 1.99.2 | unten |

**4. Geaenderte bestehende Tests:** keine (Diff ohne Testdateien).

**5. Checkliste:** keine Migration, kein SQL, kein neuer Deploy-Schritt (`nachlauf.psd1` unveraendert, richtig so); Version 1.99.2 in beiden
`AppVersion.cs`; Anwender-Changelog-Karte; README; TS-126.26/27 in `docs/TESTSZENARIEN.md`; Brain-Changelog und Testindex siehe F7.

**6. DEV** (`standort: beide`):
- `dev-deploy.ps1 -Standort AKE` und `-Standort IDEAL` aus dem Worktree (Stand `81b95ac2`), erhoehte Shell: Exit 0/0, „HTTP 200, Fusszeile
  v1.99.2“, DEV-Absicherung angewandt, Dienste gestartet.
- `publish\deploy\Update-IdealAkeWms.ps1 -Firma <F> -System Test -WebPfad D:\Dev\<F>\Web -DienstPfad D:\Dev\<F>\Service -SiteUrl
  http://localhost:808x/ -LogPfad D:\Dev\deploytest\qa_v1992_logs`:

| Schritt | Ergebnis | Status |
|---|---|---|
| 126.26 AKE-DEV `-NurPruefen` | installiert v1.99.2; „Trockenlauf in der Vorpruefung (lesend)“, `TREFFER: 15`, „Backup in Schritt 2 Pflicht, Haltepunkt in 4a“; Plan „Backup PFLICHT (Einmalig/…Altbestand)“, Haltepunkt Altbestand; Exit 0 | OK |
| 126.26 AKE-DEV Volllauf, Backup n | „Backup ist PFLICHT …“, „ABBRUCH: Backup ist Pflicht (…), aber nicht angelegt. Nichts gestoppt, Web und Dienst laufen weiter.“; Zustand live: App-Pool/Site Started, Dienst Running, „Letzter Schritt: 2. Backup“; Exit 1 | OK |
| 126.27 IDEAL-DEV `-NurPruefen` | `TREFFER: 0` → „entfaellt, kein Halt“; Plan „Backup-Angebot“, „Haltepunkt: keiner“; Exit 0 (Nachlauf-Liste siehe H4) | OK |
| 126.27 IDEAL-DEV Volllauf bis Schritt 2 (Backup n, „liegt vor“ n) | kein Pflicht-Text, „ABBRUCH: kein Backup.“, App-Pool Started, Dienst Running; Exit 1 | OK |
| 126.27 4a „entfaellt“ + Marker `Entfaellt` | Volllauf stoppt Web/Dienst und repariert, ausserhalb des QA-Rahmens auf den echten DEV-DBs; belegt durch den Dev-Lauf auf der Kopie `IDEAL_WMS_TS126` | NICHT MOEGLICH |
| DB danach | beide DEV-DBs: `DeployScriptRuns` 0, Altbestand-Auftraege 0, RCSI 0, Rollen `beschichtungsauftrag%` 0 (unveraendert) | OK |

- Browser: kein Szenario braucht den Browser. AKE-DEV-Sync-Schalter nicht angefasst (Koordinator).

**7. Publish:** `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` neu (vorher geleert): `IDEALAKEWMSWeb\`,
`IDEALAKEWMSWebService\`, `SQL\`, `deploy\`, `publish-info.txt` (Version 1.99.2, Commit `81b95ac205e6841270e6dce6797f66305ee76e47`, Branch
`feature/2026-08-07-ideal-teile-1-5`, Erstellt 2026-11-11 20:50). Exit 0/0.

**Urteil:** Testbereit. Keine Code-Befunde; F7 (Brain) behoben; H4–H6 Hinweise.

**Manuelle Checkliste (Gerald, Schranke 2; ersetzt die v1.99.1-Liste, deren Punkte mit dem Paket v1.99.2 weiter gelten):**
1. P-B1 je SQL-Server.
2. Je Zielsystem `-NurPruefen` mit dem Paket v1.99.2: Altbestand-Zeile „Trockenlauf in der Vorpruefung“ mit TREFFER-Zahl; Plan „Backup PFLICHT“
   bei Treffern, „Haltepunkt: keiner“ bei 0.
3. TS-126.20 Volllauf je Zielsystem; bei Treffern in Schritt 2 das Backup anlegen lassen (sonst Abbruch vor dem Stoppen), am Haltepunkt
   `SCHARF` nach Freigabe der Zaehlung. Bei 0 Treffern in 4a „als 'entfaellt' vermerken?“ j, dann Marker pruefen
   (`SELECT ManifestId, Mode, Hits, Note FROM dbo.DeployScriptRuns`); das ist der NICHT-MOEGLICH-Schritt oben.
4. Optional TS-126.23–126.25 an einem Testsystem.
5. H4–H6 entscheiden; Z1 (RCSI auf IDEAL_WMS_DEV) weiter offen.
6. Danach Merge (Schranke 2).

## Freigabe-Nachtrag 2026-11-12 17:04 (Gerald): Prüfung MailSettings:PickupDirectory, v1.99.12

Gilt vor dem QA-Abschnitt. Anlass: v1.99.10 führt `MailSettings:PickupDirectory` in der `appsettings.json` des Dienstes ein. Ist der Wert
gesetzt, landen **alle** Mails des Dienstes als `.eml` in einem Ordner statt per SMTP: Lieferverzug, Meldebestand, Fehler- und
Bestell-Mails.
1. Neue Prüfung im Manifest (Typ `Pruefung`, Phase `Vorher` und erneut `Nachher`), liest die Dienst-`appsettings*.json` wie bei der DB-Ermittlung.
   - **Prod-System** (`Prod=$true` in `ziele.psd1`) **und** `PickupDirectory` gesetzt: **Abbruch vor dem Stoppen** mit Meldung (Wert und Datei).
     Keine automatische Reparatur, weil die Datei nicht im Paket ist und nie überschrieben wird.
   - Test-System und gesetzt: Hinweis („Mails gehen nur in <Ordner>“).
   - Nicht gesetzt: OK.
2. In der Ermittlungs-Anzeige eine Zeile „Mail: SMTP <Host> | Ordner <Pfad>“.
3. Tests auf DEV (DEV hat den Wert gesetzt): als Test-Ziel Hinweis, mit simuliertem Prod-Ziel (Ziel-Datei temporär) Abbruch vor dem Stoppen.

## Umsetzung Nachtrag 17:04 (v1.99.12)

Commit `56906459` im Bündel (auf v1.99.11 `0dc0cea0`). Changelog [[2026-11-12-v1-99-12-update-skript-pickup-pruefung]].
- Manifest `scripts/deploy/nachlauf.psd1`: `Pruefung/MailPickup` (Typ `Pruefung`, Firma beide, Phase `Vorher`, Ausfuehrung `anzeigen`), neues Feld
  `Appsettings='MailSettings:PickupDirectory'` statt SQL; Text mit `{Wert}`/`{Datei}`. Kopfkommentar des Manifests ergänzt.
- `Update-IdealAkeWms.ps1`: `Get-Einstellung` (DB-Ermittlung) bekommt `-MitDatei` (Wert + Datei, Umgebungsdatei vor `appsettings.json`), kein
  zweiter Parser. `Get-PruefStand` liefert für `Appsettings`-Einträge 1/0 ohne DB (Leerraum = nicht gesetzt, wie `MailService.cs:31-33`).
  Vorprüfung: Phase `Vorher` mit Abweichung → Prod: „PROD, Abbruch vor dem Stoppen: …“ und nach der Plan-Zeile `Stop-Mit` (vor `-NurPruefen`-Ende,
  Schritt 2 und 3); Test: „Hinweis: Mails gehen nur in <Ordner> (<Datei>)“. Nachlauf-Block „Prüfungen“ zeigt sie erneut (Phase Nachher-Stand).
  „Vorher:“-Liste ohne Typ `Pruefung` (sonst doppelt). Ermittlungs-Zeile `Mail           SMTP <Host> | Ordner <Pfad>` (Dienst-appsettings).
- Tests: `NachlaufManifestDriftTests.Manifest_Pruefungen_Gueltig` erweitert (genau eines von `Pruefung`/`Appsettings`; `Appsettings` nur Phase
  `Vorher` + anzeigen), neu `Pruefung_MailPickup_AbbruchAufProdVorDemStoppen` (Eintrag + Abbruch im Skripttext vor `-NurPruefen`, Schritt 2, 3).
- Version 1.99.12 (beide `AppVersion.cs`), Changelog-Karte, README (Update-Skript-Abschnitt), TS-126.28–126.31.

**Nachweis:** `dotnet build IdealAkeWms.slnx` 0 Fehler; Web **2715/2716** (1 übersprungen, Bestand), Dienst **599/599**; `ParseFile` (PS 5.1.26100) 0
Fehler für alle 5 Dateien in `scripts/deploy`. Probe (PS 5.1, StrictMode 2, Funktionen aus dem Skript-AST): Umgebungsdatei überschreibt Basis →
1 mit Datei `appsettings.DEV.json`; Schlüssel fehlt → 0; nur Leerzeichen → 0.

**DEV-Test** (nur `-NurPruefen`, Paketkopie im Scratchpad mit temporärer `ziele.psd1`, EINE Zeile `AKE`/`DEV` auf `D:\Dev\AKE\Web`/`Service`,
Url `http://localhost:8081/`; keine echte Ziel-Zeile; Kopie danach gelöscht, Protokolle bleiben im Scratchpad `ts126mail\logs`):

| Nr | Ergebnis |
|---|---|
| 126.28 | `Prod=$false`: „Mail           SMTP smtp.ake.at \| Ordner D:\Dev\mail-pickup\ake“; „[Pruefung/MailPickup] Abweichung 1 - Hinweis: Mails gehen nur in D:\Dev\mail-pickup\ake (D:\Dev\AKE\Service\appsettings.DEV.json) …“; Nachlauf zeigt sie erneut; „-NurPruefen: nichts gestoppt“, Exit 0 |
| 126.29 | `Prod=$true` (Eingabe `AKE-PROD`): „PROD, Abbruch vor dem Stoppen: …“; nach dem Plan „ABBRUCH: Produktivsystem AKE-DEV: [Pruefung/MailPickup] … Wert von Hand entfernen … Nichts gestoppt, Web und Dienst laufen weiter.“; Zustand live App-Pool/Site Started, Dienst Running, letzter Schritt „1. Vorpruefung (lesend)“; Exit 1. Vor/nach beiden Läufen: App-Pool Started, Dienst Running, Dienst-Prozess-Startzeit unverändert 16:39:40 |
| 126.30 | Negativ nur per Probe (oben); DEV hat den Wert gesetzt |
| 126.31 | Mensch (Gerald), je Prod-System `-NurPruefen` |

Im Lauf mit `-NurPruefen` lief wie seit v1.99.2 der Altbestand-Trockenlauf lesend (AKE-DEV `TREFFER: 15`). Volllauf bewusst nicht ausgeführt
(Auftrag: nur Prüfmodus); der Prod-Abbruch steht vor `if ($NurPruefen)`, also im Volllauf an derselben Stelle (Drift-Test belegt die Reihenfolge).

**Befunde/Hinweise:** H7 (Hinweis): Setzt `appsettings.json` den Wert und die Umgebungsdatei `""`, meldet das Skript „gesetzt“ (Get-Einstellung
überschreibt nur mit nicht-leeren Werten), der Dienst sieht leer → Abbruch auf der sicheren Seite. H8 (Hinweis): Umgebungsvariablen
`MailSettings__PickupDirectory` liest das Skript nicht (wie bei der DB-Ermittlung).

## QA-Nachweis Nachtrag v1.99.12 (qa-agent 2026-11-12)

Gepruefter Bereich: `0dc0cea0..56906459` (nur der Freigabe-Nachtrag 17:04). Build und Tests ueber den ganzen Buendel-Worktree, Stand `56906459`.

**1. Build und Tests:**
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (Bestand, NuGet-/CS83xx-Hinweise).
- `dotnet test`: IdealAkeWms.Tests **2715 bestanden / 1 uebersprungen / 0 Fehler** (2716); IDEALAKEWMSService.Tests **599 / 0 / 0**.

**2. Code-Review** (selbst, in diesem Lauf). Gelesen: ganzer Diff (`Update-IdealAkeWms.ps1`, `nachlauf.psd1`, `NachlaufManifestDriftTests.cs`,
README, TS-126.28–126.31, Changelog-Karte, beide `AppVersion.cs`) mit Umgebung: `Get-Einstellung :141-152`, `Get-Ermittlung :190-222`,
`Get-PruefStand`/`Format-PruefText :409-420`, Vorpruefung `:684-741` (Reihenfolge Pruefungen → Marker → Trockenlauf → Plan → `$sperre`-Abbruch →
`-NurPruefen`), Hauptlauf `:922-945` (Prod-Eingabe, `catch` → `Show-Zustand`), Kopierschritt `:614-633`/`:829-833` (robocopy `/XF appsettings*.json`,
Schutzstand-Hash), `Show-Nachlauf :889-920`, `Import-Ziele` (psm1 `:38`), `MailService.cs:31-33`, `ServiceSettingDefinitions.cs:13`.
- `Get-Einstellung -MitDatei`: kein zweiter Parser; ohne Schalter Rueckgabe unveraendert (`[string]` oder `$null`), DB-Ermittlung und KeysPath
  unberuehrt. `Get-PruefStand` liefert fuer `Appsettings` immer `[int]` 0/1, damit greift `$st -is [int]` im Sperr-Zweig.
- Abbruch auf Prod: `Stop-Mit` `:732` steht nach dem Plan und vor `if ($NurPruefen)` `:734`, Schritt 2 `:744` und 3; davor laufen nur lesende
  Schritte (Fusszeile, History, SQL-Pruefungen, Marker, Altbestand-Trockenlauf). `throw` → `catch :937` → `Show-Zustand`, Exit 1. Mehrere Systeme:
  Prod laeuft zuletzt (`:84`), Test-Systeme davor sind unberuehrt.
- PS 5.1 + StrictMode 2: `$e.Ziel.Prod` auf Hashtable ohne Schluessel = `$null` (wie bisher `:926`); `$m['Appsettings']` als Index statt Property;
  `.Replace('{Wert}', [string]$null)` sicher. Eigene Probe (`powershell.exe` 5.1, StrictMode 2, Funktionen per AST aus der **Paketkopie**):
  leer, nur Leerzeichen, Schluessel fehlt, Abschnitt fehlt, keine Datei, `null` → 0; Basis gesetzt → 1 mit `appsettings.json`; Umgebungsdatei
  ueberschreibt → 1 mit `appsettings.Prod.json`; H7-Fall → 1 (Basiswert); kaputtes JSON → `ABBRUCH: … ist kein gueltiges JSON`.
- Drift-Test: `Manifest_Pruefungen_Gueltig` jetzt genau eines von `Pruefung`/`Appsettings`, `Appsettings` nur `Vorher` + `anzeigen`; neuer Test
  belegt die Textreihenfolge Sperre < `-NurPruefen` < Schritt 2/3. Beweist die Reihenfolge im Text, nicht das Verhalten (dafuer DEV unten).
- Standard-Defektklassen: keine Razor-/JS-Logik (nur statische Changelog-Karte); keine neue Einstellung im Sinn von ADR 0020 (der Wert ist eine
  bestehende `appsettings`-Zeile, `MailSettings:*` ist bewusst nicht im ServiceSettings-Katalog, `ServiceSettingDefinitions.cs:13`).
- **Abweichung „Phase Vorher UND erneut Nachher“:** reicht. `Show-Nachlauf` ruft nach dem Start (`:885`, Schritt 7) fuer jede Pruefung erneut
  `Get-PruefStand` auf, liest die Datei also wirklich neu (keine Wiederanzeige eines gespeicherten Werts). Ein zweiter Manifest-Eintrag mit Phase
  `Nachher` haette nichts zusaetzlich gefunden: Das Skript kopiert `appsettings*.json` nie (`/XF`) und bricht ab, wenn sie sich waehrend des Kopierens
  aendern (Schutzstand `:832`). Auf Prod kommt ein gesetzter Wert gar nicht bis zum Start. Fachlich gleichwertig, kein Befund.

| # | Schwere | Fundstelle | Befund | Behandlung |
|---|---|---|---|---|
| H7 | Hinweis | `Get-Einstellung :148` | Basis gesetzt, Umgebungsdatei `""`: Skript meldet „gesetzt“, der Dienst sieht leer. Falsch-positiv auf der sicheren Seite (Prod bricht ab; der Wert gehoert dann ohnehin entfernt). | akzeptiert |
| H8 | Hinweis | `Get-Ermittlung :191-195` | Umgebungsvariable `MailSettings__PickupDirectory` (Dienst-Registry `Environment` oder Maschine) liest das Skript nicht, `IConfiguration` des Dienstes schon. Auf den Zielsystemen bisher nur `DOTNET_ENVIRONMENT` ueblich; Restrisiko klein. | offen, Punkt 2 der Checkliste |
| H9 | Hinweis (Kosmetik) | `Invoke-System :652` | Nur Leerzeichen als Wert: Ermittlungszeile zeigt „Ordner    “ statt „-“, die Pruefung sagt richtig OK. | offen (Kosmetik) |

**3. Beweisart je Nachtrag-Punkt** (kein automatisierter Test fuer Skriptverhalten):

| Punkt | Beweis |
|---|---|
| 1 Pruefung im Manifest, liest Dienst-`appsettings*.json` wie die DB-Ermittlung | Drift-Test (Eintrag, Feld, Phase) + Code-Review + Probe |
| 1a Prod + gesetzt → Abbruch vor dem Stoppen, Wert und Datei | QA-DEV (simuliertes Prod, beide Standorte, `-NurPruefen`) + Drift-Test (Reihenfolge); Volllauf: Code-Review (Abbruch vor `-NurPruefen`, also identisch) |
| 1b Test + gesetzt → Hinweis | QA-DEV (beide Standorte) |
| 1c nicht gesetzt → OK | Probe (DEV hat den Wert gesetzt) |
| 1d erneut Nachher | Nachlauf-Liste im `-NurPruefen`-Lauf (derselbe Codepfad wie Schritt 7) + Code-Review; im Volllauf **Manual-UAT** (Checkliste 4) |
| 2 Ermittlungszeile „Mail: SMTP … \| Ordner …“ | QA-DEV |
| 3 DEV-Tests | unten |

**4. Geaenderte bestehende Tests:** `NachlaufManifestDriftTests.Manifest_Pruefungen_Gueltig` – Verhalten absichtlich durch den Nachtrag geaendert
(neue Variante `Appsettings` statt SQL; fuer SQL-Pruefungen bleiben Lese-Waechter, Phase und Ausfuehrung unveraendert geprueft). Kein Fixture angepasst.

**5. Checkliste:** keine Migration, kein SQL; Version 1.99.12 in beiden `AppVersion.cs`; Anwender-Changelog-Karte; README (Update-Skript); TS-126.28–126.31
in `docs/TESTSZENARIEN.md`; Testindex (126.28–126.31) und Brain-Changelog [[2026-11-12-v1-99-12-update-skript-pickup-pruefung]] vorhanden. 5a: keine
neue Einstellung (siehe oben). Der Deploy-Schritt „Wert auf Prod entfernen“ steht als `Pruefung/MailPickup` in `nachlauf.psd1`.

**6. DEV** (`standort: beide`):
- `dev-deploy.ps1 -Standort AKE` und `-Standort IDEAL` aus dem Worktree (`56906459`), erhoehte Shell: Exit 0/0, „HTTP 200, Fusszeile v1.99.12“,
  DEV-Absicherung angewandt, Dienste gestartet.
- Skripttest: Kopie von `publish\` im Scratchpad (`qa_v19912\pkg`) mit temporaerer `ziele.psd1` (je eine Zeile AKE/IDEAL `DEV` auf
  `D:\Dev\<F>\Web`/`Service`, Url `localhost:808x`; keine echte Ziel-Zeile), nur `-NurPruefen`. Gestartet mit leerem `PSModulePath` (sonst findet
  `powershell.exe` aus pwsh heraus `Import-PowerShellDataFile` nicht – Eigenheit der Testumgebung, kein Skriptfehler).

| Schritt | Ergebnis | Status |
|---|---|---|
| 126.28 AKE-DEV `Prod=$false` | „Mail           SMTP smtp.ake.at \| Ordner D:\Dev\mail-pickup\ake“; „[Pruefung/MailPickup] Abweichung 1 - Hinweis: Mails gehen nur in D:\Dev\mail-pickup\ake (D:\Dev\AKE\Service\appsettings.DEV.json) …“; Nachlauf zeigt die Zeile erneut; „-NurPruefen: nichts gestoppt“; Exit 0 | OK |
| 126.28 IDEAL-DEV `Prod=$false` | dasselbe mit `D:\Dev\mail-pickup\ideal` und `D:\Dev\IDEAL\Service\appsettings.DEV.json`; Exit 0 | OK |
| 126.29 AKE-DEV `Prod=$true` (Eingabe `AKE-PROD`, DB j) | „PROD, Abbruch vor dem Stoppen: …“; nach dem Plan „ABBRUCH: Produktivsystem AKE-DEV: [Pruefung/MailPickup] … Wert von Hand entfernen … Nichts gestoppt, Web und Dienst laufen weiter.“; Zustand App-Pool/Site Started, Dienst Running, „Letzter Schritt: 1. Vorpruefung (lesend)“; kein 5a-Bericht, keine Backup-Frage; Exit 1 | OK |
| 126.29 IDEAL-DEV `Prod=$true` (`IDEAL-PROD`) | dasselbe; Exit 1 | OK |
| Dienste vor/nach allen 4 Laeufen | `AKE_WMS_DEV_Service` PID 48152, `IDEAL_WMS_DEV_Service` PID 28364, beide Running, unveraendert | OK |
| 126.29 Volllauf mit simuliertem Prod | nicht ausgefuehrt (Auftrag: nur `-NurPruefen`); der Abbruch steht vor `-NurPruefen`, also identisch | NICHT MOEGLICH (Auftrag) |
| 126.30 Wert leer/Leerzeichen/fehlt | DEV hat den Wert gesetzt; belegt durch die Probe oben | live NICHT MOEGLICH, Probe OK |
| 126.31 je Prod-System | Mensch | offen |

- Schlusszustand DEV: `http://localhost:8081/` und `:8082/` HTTP 200, Fusszeile v1.99.12; `AKE_WMS_DEV_Service`, `IDEAL_WMS_DEV_Service` Running;
  beide DEV-DBs: `Sync:*Enabled` = true 0-mal, `SageLagerbuchungAktiv` false, `SageWareneingangBelegAktiv` false, `Sync:SageSystemKuerzel` DEV;
  `MailSettings:PickupDirectory` in beiden `appsettings.DEV.json` gesetzt. Die 5 AKE-DEV-Sync-Schalter (Auftrag Koordinator) hat QA **nicht**
  eingeschaltet: Die QA-Regel verbietet, `Sync:*Enabled` einzuschalten; zurueck an den Koordinator.
- Browser: kein Szenario braucht den Browser.

**7. Publish:** `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` neu (vorher geleert): `IDEALAKEWMSWeb\`,
`IDEALAKEWMSWebService\`, `SQL\`, `deploy\`, `publish-info.txt` (Version 1.99.12, Commit `56906459440af1fff9242bcb6cdccb1848102af0`, Branch
`feature/2026-08-07-ideal-teile-1-5`, Erstellt 2026-11-12 17:17). Exit 0/0.

**Urteil:** Testbereit. Keine Code-Befunde; H7 akzeptiert, H8/H9 Hinweise.

**Manuelle Checkliste (Gerald, Schranke 2; ergaenzt die v1.99.2-Liste, deren Punkte mit dem Paket v1.99.12 weiter gelten):**
1. TS-126.31: je Prod-System (AKE-Prod, IDEAL-Prod) `-NurPruefen` mit dem Paket v1.99.12: „Mail: SMTP <Host> | Ordner -“ und
   „[Pruefung/MailPickup] OK“. Sonst den Wert aus der genannten Datei entfernen, erneut.
2. H8: je Prod-Dienst einmal `reg query HKLM\SYSTEM\CurrentControlSet\Services\<Dienst> /v Environment` – kein `MailSettings__PickupDirectory`.
3. Je Test-System `-NurPruefen`: Hinweiszeile nur, wenn dort bewusst ein Ablageordner gesetzt ist.
4. TS-126.20 Volllauf je Zielsystem (wie v1.99.2-Liste); in Schritt 7 die Zeile `[Pruefung/MailPickup]` ansehen (Nachher-Pruefung).
5. H9 entscheiden; danach Merge (Schranke 2).
