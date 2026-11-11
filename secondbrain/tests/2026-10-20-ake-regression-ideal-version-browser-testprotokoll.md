---
type: testprotokoll
title: "Browser-Testprotokoll AKE-Regression — IDEAL-Version (Bündel v1.64.0) auf neuem Testsystem mit AKE-Konfiguration"
ausfuehrung: "Teil A + C: Gerald · Teil B: Claude Cowork (Abnahme)"
system: "https://akenet01.ake.at:4445/ — neues Testsystem, AKE-Konfiguration (Master ProduktionsauftragHierarchisch = false); Referenz https://akenet01.ake.at:4444/"
referenz: "[[2026-10-20-ake-testsystem-bestandsaufnahme]] (akenet01, v1.30.1, Stand 20.10.2026)"
zweig: "feature/2026-08-07-ideal-teile-1-5 (v1.64.0)"
created: 2026-10-20
---

# AKE-Regression der IDEAL-Version — Test-Playbook

**Ziel.** Die IDEAL-Version (Bündel `feature/2026-08-07-ideal-teile-1-5`, v1.64.0) läuft auf einem **neuen Testsystem mit
AKE-Daten und AKE-Konfiguration**. Jeder Schritt prüft: Verhält sich jedes Feature so wie heute auf akenet01 (v1.30.1)? Wo
sich das Verhalten **gewollt** ändert, steht die neue Erwartung ausdrücklich dabei („Neu ggü. v1.30.1“). Alles andere gilt
als Regression.

**Referenzen.**
- Verhalten v1.30.1: [[2026-10-20-ake-testsystem-bestandsaufnahme]] — im Folgenden „BA §x“.
- Änderungen v1.31–v1.64: Brain-Changelogs `secondbrain\changelog\` und Anwender-Changelog des Bündels.
- Szenarien: `docs\TESTSZENARIEN.md` im Bündel. Zitiert als TS-xx; die AKE-Regressionspunkte stehen dort jeweils
  als „AKE“ (z. B. TS-89.7, TS-93.10, TS-94.1, TS-95.2, TS-96.9, TS-97.9, TS-97.10).
- Menü-Bedingungen: Bündel `IdealAkeWms\Views\Shared\_Layout.cshtml`. Service-Schlüssel und Defaults:
  `IdealAkeWms\Models\ServiceSettingDefinitions.cs`.

**Ablauf.**
1. Teil A erledigt Gerald **vor und während des Aufbaus** des Testsystems.
2. Danach den Freigabe-Block ankreuzen, die Platzhalter `{…}` ausfüllen und Teil B als eigene Aufgabe an Cowork geben
   (Arbeitsweise ABNAHME).
3. Teil C prüft Gerald von Hand: Drucker, Mails, Sage, Hardware, andere Benutzer.

> [!important] Schwerpunkte (Gerald, 20.10.2026)
> **Priorität 1:**
> - **Service** (Hintergrund-Jobs): BS
> - **Lager**: B1
> - **FA-Liste, Leitstand, Kommissionierung**: B2, B3
> - **Parallelvergleich** mit akenet01: BV
>
> **Priorität 2:** Bestellungen (B4), Teileverfolgung und Reporting (B5), Stammdaten (B7), Querschnitt (B8).
>
> **Priorität 3:** BDE (B6). BDE ist derzeit nicht im Einsatz. Die Schritte laufen nur, wenn Zeit bleibt; ein Befund
> dort blockiert die Abnahme nicht.
>
> **Grundidee:** Beide Systeme lesen denselben Sage-Testmandanten (kein eigener vorhanden). Alles, was aus Sage berechnet
> wird, muss deshalb im selben Sync-Fenster **auf beiden Systemen gleich** aussehen — FAs, Termine, Stücklisten,
> Sage-Bestände. Jede Abweichung außer den unter „Neu“ genannten ist eine Regression. Das prüft BV.

> [!warning] Adresse des neuen Testsystems
> Das neue Testsystem ist **https://akenet01.ake.at:4445/**. Es läuft auf demselben Host wie die Referenz akenet01
> (Port **4444**) — nur der Port unterscheidet die beiden. Die Projektanweisungen erlauben Cowork nur akenet01:4444 und
> idealweb01. Der Auftrag für Teil B muss Port **4445** deshalb ausdrücklich nennen und erlauben, sonst bricht Cowork
> ab („falsches System“). Cowork prüft vor jedem Schritt den Port in der Adresszeile. Schreibende Schritte laufen nur
> auf 4445 — Ausnahme ist der eigens freigegebene Zwillingstest B1-S6 auf 4444.
>
> **Cookies:** Browser trennen Cookies nach Host, **nicht nach Port**. Beide Systeme teilen sich im selben Browser also
> Sitzungs-, Anmelde- und Antiforgery-Cookies, sofern die Anwendung keine portspezifischen Cookie-Namen setzt
> (ungeprüft, siehe QA-Auftrag). Mögliche Folgen: Abmeldung beim Wechsel zwischen den Tabs oder Formularfehler 400 beim
> Speichern. Für BV und B1-S6 deshalb, wenn möglich, zwei getrennte Browserprofile nutzen. Tritt eine solche Abmeldung
> oder ein Fehler 400 auf, ist das zuerst ein Befund zur Testumgebung, nicht zur Version.

> [!info] Lesend und schreibend
> Schritte mit **[L]** sind rein lesend und laufen ohne Freigabe. Schritte mit **[S]** verändern Testdaten (buchen,
> freigeben, Bestellung abschicken …). Sie laufen **nur**, wenn Gerald genau diesen Schritt im Freigabe-Block ankreuzt;
> sonst sind sie NICHT PRUEFBAR. Auf dem neuen Testsystem ist Schreiben gewollt — ein AKE-Regressionstest ohne Buchungen
> beweist wenig. Jeder [S]-Schritt nennt seine Spur, damit die Änderung im Bericht nachvollziehbar ist.

---

## Teil A — Gerald, Aufbau des Testsystems (vor Teil B)

### A1 Datenbasis
- [ ] Kopie der **AKE-WMS-DB** wählen und notieren: `{DB_QUELLE}`.
  - Empfehlung: **akenet01** (`IDEAL_AKE_WMS` auf dem Test-SQL). Dann gelten die Zahlen und Testdaten aus BA §1 und §5
    unmittelbar als Vergleich.
  - Eine Produktionskopie ist realistischer. Dann muss Teil B mit eigenen Testdaten-Platzhaltern arbeiten.
- [ ] Backup der Kopie vor jedem Skript.
- [ ] Schema-Stand der Kopie prüfen. Erwartet ist main bis `SQL/88`:
  ```sql
  SELECT TOP 5 MigrationId FROM dbo.__EFMigrationsHistory ORDER BY MigrationId DESC;
  ```
- [ ] Konfiguration der Quelle sichern. Sie dient später als Vergleich für A5:
  ```sql
  SELECT [Key],[Value] FROM dbo.AppSettings ORDER BY [Key];
  SELECT [Key],[Value] FROM dbo.ServiceSettings ORDER BY [Key];
  ```
- [ ] Auf dem SQL-Server des Testsystems darf **kein SQL-Agent-Job** `ProductionOrders` oder `Articles` schreiben. `sysjobs`
  prüfen; AgentJob 01 ist im Bündel archiviert.

### A2 Prüfabfragen vor der Migration (auf der Kopie) — Ergebnisse eintragen

| Nr | Abfrage | Erwartet | Ergebnis |
|---|---|---|---|
| P-2 | `SELECT DATABASEPROPERTYEX(DB_NAME(),'Collation');` | `…_CI_…` (Pflicht für die neue Filtersyntax, TS-91) | ______ |
| P-T1 | `SELECT wo.OperationNumber, COUNT(*) FROM dbo.WorkOperations wo GROUP BY wo.OperationNumber;` und `SELECT [Key],[Value] FROM dbo.AppSettings WHERE [Key] IN ('BdeAktiv','BdeNurFaMeldung');` | nur `01` bzw. NurFA = true. Auf akenet01 ist NurFA = **AN** (BA §2.1) | ______ |
| P-T1K | Kollision OSEON-AG-Namen ↔ BDE-Kürzel (Abfrage TS-90, „P-T1 Kollision“) | leer. Treffer **vor Teil B melden** | ______ |
| P-1 | Gespeicherte Standardfilter mit Komma in Zahl- oder Datumskontext (TS-91, P-1) | Treffer notieren. Ab v1.58 heißt ODER dort `;` | ______ |
| P-3 | Server-Kultur: eine Menge mit Nachkommastellen zeigt `2,5` | `de-AT` bzw. `de-DE` | ______ |
| E1 | Lief das Einmal-Skript `SQL\Einmalig\2026-09-28_Hotfix-1.30.1_…` auf der Quell-DB? (Deploy-Protokoll) | ja → **nicht** erneut ausführen | ______ |

### A3 Migration auf v1.64.0 (Reihenfolge laut Brain/README des Bündels)

1. [ ] Dienst und App-Pool **stoppen**.
2. [ ] Die SQL-Skripte des Bündels **vor dem ersten App-Start** einspielen, jeweils mit History-Insert.
   `Program.cs` ruft `Database.Migrate()` auf; die Migration `InvertProductionOrderHierarchy` scheitert, wenn `SubOrderNumber`
   schon existiert.
3. [ ] Reihenfolge: `89` → **`90_InvertProductionOrderHierarchy`** (datenkonvertierend auf der Kerntabelle, Backup zwingend) →
   `91_AddArticleMatchcode` → `92_AddUserDefaultFilterBomKommissionierziel` → `93_AddProductionWorkplaceSageFields` →
   `94_AddBeschichtungsauftrag`.
4. [ ] **`95_RenameOperationConfigsAndAddAudit`**: vorher prüfen, dass `20260330142035_AddOseonOperationConfig` in der History
   steht. Bei „ABBRUCH“ anhalten.
5. [ ] `96_AddOperationConfigSourceAndSageMissingSince` → `97_AddProductionWorkplaceSageMissingSinceAndNurAgFertigmeldung`.
6. [ ] **Nicht** ausführen (nur IDEAL): `2026-10-07_v1.47.4_…` und `2026-10-15_IDEAL-Sage-Views_Bereinigung00erArtikel`.
   Letzteres bricht auf AKE ab; TS-93.13 a prüft genau das.
7. [ ] **Web zuerst** starten (legt die neuen Schlüssel aus v1.60 an), **dann** den Dienst.

> Hinweis (laut Prüfbericht, von mir nicht nachgelesen): Die Update-Tabelle in der README des Bündels nennt nach Skript 80
> nur 95 und 97. Die Liste oben folgt dem Inhalt
> von `SQL\`. Den Widerspruch bitte beim Deploy klären.

### A4 appsettings des neuen Systems
- [ ] `ConnectionStrings`:
  - `DefaultConnection` → die neue Test-DB, **nie** die akenet01- oder Produktions-DB. Ein lokaler Start gegen AKESQL20
    wäre ein Deploy.
  - `Sage` → derselbe AKE-Sage-Testmandant wie akenet01 (kein eigener vorhanden, Gerald 20.10.2026).
  - `Oseon` und `EnaioDms` wie akenet01.
- [ ] `MailSettings`: auf ein Test-Postfach umleiten oder Versand aus (siehe A5).
- [ ] `PdfRender:*`: für AKE nicht nötig (PDF gibt es nur in IDEAL-Modulen).
- [ ] `DataProtection:KeysPath`, `Security:AdDomain` wie akenet01.
- [ ] IIS: Windows- **und** anonyme Authentifizierung aktiv (Hilfe, Windows-Anmeldung). Die Bindung `{SYSTEM_URL}` mit
  gültigem Zertifikat — Kamera-Scan geht nur über HTTPS.

### A5 Konfiguration setzen — AKE-Pflichtwerte

Nach dem ersten Web-Start unter `/Settings`, `/ServiceSettings` und `/StandortEinstellungen` prüfen bzw. setzen.

**Teil 1: Werte, die die IDEAL-Version neu einführt.** Ein falscher Wert hier testet AKE im IDEAL-Modus.

| Schlüssel | Pflichtwert AKE | Wert auf idealweb01 (nicht übernehmen) | Beleg |
|---|---|---|---|
| `ProduktionsauftragHierarchisch` (Einwegtor) | **false** | true | `ServiceSettingDefinitions.cs`; StandortEinstellungen zeigt dort „Hierarchisch aktiviert“ |
| `FaHierarchyKommissionierlistenAktiv` / `FaHierarchyBeschichtungAktiv` / `FaHierarchyVormontageAktiv` | **false / false / false** | AN / AN / AN | `_Layout.cshtml` (Toggle-Gates) |
| `Sync:HierarchicalFaEnabled` | **false** | AN | Definitionen |
| `Sync:WorkOperationStructureDetectionEnabled`, `Sync:ProductionWorkplaceSyncEnabled` | **false** (laufen ohnehin nur bei Master = true) | AN | Definitionen |
| `Sync:OperationConfigSyncEnabled` | **false** (für AKE ist keine eigene View bekannt) | AN | Definitionen, TS-94.1 |
| `Sync:ArticlesViewName` | **leer** („Leer = bisheriger Select (AKE)“) | `[vw_IDEAL_AKE_WMS_Artikel]` | Definitionen, TS-93.10 |
| `Sync:FaZusatzinfoAutoErledigtEnabled` | **true** („Default an (AKE-Verhalten)“) | aus | Definitionen |
| `BdeTerminalMaxHauptFa` | 5 (Seed; wirkt nur bei Gruppierung) | — | Changelog v1.64 |
| `Firmenname`, `Firmenanschrift`, `BriefkopfVorlagePfad` | leer lassen (nur IDEAL-Druck) | IDEAL-Werte | StandortEinstellungen |

**Teil 2: Wie akenet01.** Werte laut BA §2.1 und §2.2:
- AppSettings: `LackierteilKategorieName = Lackierteile`, `NegativeBuchungErlaubt = AN`, `TeileverfolgungAktiv = AN`,
  `BestellungenAktiv = AN`, `DefaultLagerbestellempfaengerId = 1`, `DefaultGlasbestellempfaengerId = 4`,
  `GlasArtikelgruppen = 880,…,886`, `KommissionierungMitZuweisung = AN`, `FaCompletionAktiv = AN`,
  `WindowsAuthAktiv = AN`, `QrMitFaNummer = AN`, BDE wie in BA §2.1 (insbesondere `BdeNurFaMeldung = AN`,
  `BdeDefaultArbeitsgang = PRODUKTION`, Mehrfachbuchung AN, Schichtkalender AN).
- Sync-Schalter: OSEON, enaio, BOM-Cache (10 Wochen / 500), FaWorkStepDetection, CoatingDetection, Lagerplätze und
  Lagerbestand, Reconcile, FaZusatzinfo.

**Teil 3: Schutzwerte des Testsystems.** Gerald entscheidet je Zeile:
- [ ] `Sync:PartRequisitionEmailEnabled` / `Sync:WarehouseRequisitionEmailEnabled` / `ErrorNotification:*` /
  `Notifications:*`: an **nur** mit umgeleitetem Mailserver, sonst aus.
- [ ] `Notifications:AppBaseUrl = {SYSTEM_URL}`, sonst zeigen Mail-Links auf akenet01.
- [ ] `SageLagerbuchungAktiv`: **aus** (Standard für dieses Playbook). Es gibt **keinen eigenen Sage-Testmandanten**
  (Gerald, 20.10.2026). Das neue System und akenet01 müssten beide in `ake_TEST2026;1` buchen; dessen Bestände liest der
  Lagerbestand-Sync beider Systeme zurück. B1-S5 und C3 deshalb nur, wenn Gerald dafür ein Zeitfenster festlegt und
  `SageLagerbuchungAktiv` auf akenet01 solange **aus** ist. Sonst sind beide NICHT PRUEFBAR.
  Lesende Sage-Zugriffe (FA-, Artikel-, Lagerplatz- und Bestand-Sync, Stückliste) gegen denselben Mandanten sind
  unkritisch: Sie schreiben nur in die WMS-Datenbank des jeweiligen Systems.
- [ ] `OseonRueckmeldungAktiv` / `SageRueckmeldungAktiv` = false.

### A6 Nach dem ersten Sync-Zyklus (≈ 15 Minuten), `/SyncLog`

- [ ] Läufe `ProductionOrder`, `Article`, `Lagerplatz`, `Lagerbestand`, `BomCache`, `FaWorkStepDetection`, `CoatingDetection`,
  `OseonTracking`, `EnaioDms` erfolgreich. Counts plausibel zu BA §2.3 (Article: gelesen ≈ 100.600,
  hauptlagerplatz_fehlt ≈ 106).
- [ ] **Kein** Lauf `FaHierarchy…`, `ProductionWorkplaceSync`, `WorkOperationStructure…`, `OperationConfigSync` (TS-93.10,
  TS-96.9).
- [ ] `ProductionOrderReconciliation`: storniert = 0. Wenn nicht: sofort anhalten — falscher Sage-Mandant.
- [ ] FaZusatzinfo: erwartet ist dieselbe Warnung wie auf akenet01 („View … nicht vorhanden — Sync übersprungen“), solange
  die View auf der Sage-Testdatenbank fehlt (BA §6.1).
- [ ] Danach erst Teil B.
- [ ] Prüfabfragen nach der Migration:
  ```sql
  SELECT COUNT(*) FROM dbo.ProductionOrders WHERE OrderNumber <> SubOrderNumber;           -- erwartet 0
  SELECT [Value] FROM dbo.ServiceSettings WHERE [Key]='ProduktionsauftragHierarchisch';   -- erwartet false
  SELECT COUNT(*) FROM dbo.ProductionWorkplaces WHERE ArbeitsschrittCode IS NOT NULL;     -- P5, erwartet 0
  ```
- [ ] P-1 gespeicherte Standardfilter (nach der Migration, weil `DefaultFilterBomKommissionierziel` erst mit SQL/92 kommt;
  Quelle: Spec `2026-09-23-suchsyntax-spaltenfilter-erweitern-spec`, Abschnitt „Prüfabfrage P-1“). Treffer notieren; ein
  Admin korrigiert sie in der Benutzerpflege, es gibt keine automatische Umschreibung.
  ```sql
  SELECT u.Id, u.Name, v.Spalte, v.Wert
  FROM dbo.Users u
  CROSS APPLY (VALUES
      ('DefaultFilterBeschaffung',             u.DefaultFilterBeschaffung),
      ('DefaultFilterArtikelgruppe',           u.DefaultFilterArtikelgruppe),
      ('DefaultFilterFaWorklistDescription1',  u.DefaultFilterFaWorklistDescription1),
      ('DefaultFilterBomDescription1',         u.DefaultFilterBomDescription1),
      ('DefaultFilterBomKommissionierziel',    u.DefaultFilterBomKommissionierziel)
  ) v(Spalte, Wert)
  WHERE v.Wert IS NOT NULL AND (
         v.Wert LIKE '%[%]%' OR v.Wert LIKE '%;%' OR v.Wert LIKE '%...%'
      OR REPLACE(v.Wert, ' ', '') LIKE '[!=<>]%'
      OR REPLACE(v.Wert, ' ', '') LIKE '%[,;][=<>]%'
      OR (v.Wert LIKE '%(leer)%' AND v.Wert <> '!(leer)'));
  ```
- [ ] P-2b Kollation je Spalte (Quelle: Spec, Abschnitt Deploy) — erwartet leer:
  ```sql
  SELECT t.name AS Tabelle, c.name AS Spalte, c.collation_name
  FROM sys.columns c JOIN sys.tables t ON t.object_id = c.object_id
  WHERE c.collation_name IS NOT NULL AND c.collation_name NOT LIKE '%[_]CI[_]%';
  ```

### A7 Testdaten und Testbenutzer

Platzhalter für Teil B. Die Vorschläge gelten bei DB-Quelle akenet01 (BA §5).

| Platzhalter | Bedeutung | Vorschlag (akenet01-Kopie) | Wert |
|---|---|---|---|
| `{SYSTEM_URL}` | Adresse des neuen Systems | **https://akenet01.ake.at:4445/** (Gerald, 20.10.2026) | https://akenet01.ake.at:4445/ |
| `{FA_A}` | offener FA mit Stückliste (Sage oder OSEON) | 2604043 (Werkbank A3, 106 BOM-Zeilen) | ______ |
| `{FA_B}` | offener, **nicht** freigegebener FA für Freigabe-Tests | 2614167 | ______ |
| `{FA_FREI}` | bereits freigegebener FA mit Kommissionierer | 2614027 (Picker „test“, Prio 15) | ______ |
| `{ART_BESTAND}` | Artikel mit Bestand auf buchbarem Platz | 00102251 (342 Stk, GL;0;0;0) | ______ |
| `{ART_MATCHCODE}` | Artikel mit Matchcode aus Sage (nach dem Artikel-Sync) | ______ (Gerald: `SELECT TOP 5 ArticleNumber, Matchcode FROM dbo.Articles WHERE Matchcode IS NOT NULL`) | ______ |
| `{LP_TEST}` | manueller, buchbarer Testplatz ohne Sage-Meldung | Test | ______ |
| `{LP_SAGE}` | Platz mit „An Sage melden“ | GL;0;0;0 | ______ |
| `{WAGEN}` | Kommissionierwagen | Wagen 1 | ______ |
| `{OP_PNR}` | BDE-Operator (Personalnummer) | 4444 (Fred Ferdl) | ______ |
| `{TERM_WB}` | Werkbank des Terminals von Geralds Benutzer | Stanzarei | ______ |
| `{FA_TERM_EIGEN}` | offener FA **mit** Werkbank = `{TERM_WB}` | ______ (auf akenet01 hat keiner der 14 offenen FAs „Stanzarei“) | ______ |
| `{FA_TERM_FREMD}` | offener FA mit **anderer** oder **ohne** Werkbank | 2604043 (A3) / 2614027 (ohne) | ______ |
| `{LB_ENTWURF}` | eigene Lagerbestellung im Entwurf | #52 (Glas) | ______ |
| `{LB_OFFEN}` | abgeschickte Lagerbestellung mit ≥ 2 Positionen | #57 (A1, 2 Pos.) | ______ |

---

## Freigabe-Block (Gerald kreuzt an — ohne Haken startet Cowork nicht bzw. der Schritt ist NICHT PRUEFBAR)

- [ ] Teil A vollständig. A6 ohne Stopp, Master = false, P5 = 0.
- [ ] Das Testsystem zeigt unter `/Help/Changelog` v1.64.0 (oder `{VERSION}`). Gerald ist als Admin per Windows-Anmeldung im
  Browser angemeldet.
- [ ] `{SYSTEM_URL}` ist im Auftrag als erlaubte Adresse genannt.
- [ ] Platzhalter aus A7 ausgefüllt (`KEIN_FALL`, wo es keinen gibt).
- [ ] Schreibende Schritte, die Cowork ausführen darf (einzeln ankreuzen):
  - [ ] B1-S Lagerbuchungen (Ein-, Aus-, Um-, Mehrfach-Einbuchung auf `{LP_TEST}`)
  - [ ] B1-S6 Zwillingstest: dieselben Lagerbuchungen **auch auf akenet01** (Testplatz, kein Sage-Platz)
  - [ ] BV lesend auf akenet01 **und** `{SYSTEM_URL}` (beide Adressen im Auftrag nennen)
  - [ ] B1-S5 Einbuchung auf `{LP_SAGE}`. Erzeugt eine Sage-Meldung — nur im Zeitfenster aus A5 Teil 3, mit
    Sage-Lagerbuchung auf akenet01 aus.
  - [ ] B2-S FA-Vervollständigung / Abarbeitungsliste (Status, Merkmal)
  - [ ] B3-S Leitstand-Freigabe, Prio, Zurücknehmen; Kommissionieren und Umbuchen auf Wagen
  - [ ] B4-S Bedarfsmeldung, Lagerbestellung anlegen/abschicken/abschließen
  - [ ] B5-S Arbeitsgang-Stammdaten Negativtest (Speichern ohne Offset)
  - [ ] B6-S BDE-Terminal-Buchungen mit `{OP_PNR}`
  - [ ] Downloads (CSV), jeweils mit Rückfrage im Chat

---

## Teil B — Browser (Cowork, Arbeitsweise ABNAHME)

Je Schritt: **Weg · Aktion · Erwartet (Referenz v1.30.1) · Neu ggü. v1.30.1 · Bezug.** Was unter „Neu“ steht, ist **kein**
Fehler, sondern die geprüfte Erwartung. Auf **jeder** geöffneten Seite zusätzlich die Browser-Konsole auf Fehler der Anwendung
prüfen und Abweichungen festhalten.

### B0 Rahmen

**B0.1 Version und Startseite [L]**
- Weg: `/Help/Changelog`, dann `/`.
- Erwartet:
  - Neuester Eintrag v1.64.0 (bzw. `{VERSION}`). Darunter v1.47.1 mit dem Hinweis „AKE-Hotfix 1.30.1 übernommen“.
  - Dashboard mit denselben Kacheln wie BA §3: Lager ×4; Kommissionierung, Fertigungsaufträge, Artikelinfo; Leitstand;
    Teileverfolgung OSEON; BDE ×3; OSEON AG-Übersicht; Artikel, Lagerplätze, Benutzer, Arbeitsplätze.

**B0.2 Menü als Admin [L]**
- Aktion: alle Dropdowns lesen und mit BA §3 vergleichen.
- Erwartet: Lager, Fertigungsaufträge (FA-Liste, FA-Vervollständigen, FA-Abarbeitungsliste), Leitstand › Kommissionierung,
  **Kommissionierung als einfacher Link mit Badge** (kein Dropdown, weil die Kommissionierlisten aus sind), Bestellungen
  (5 Einträge), Teileverfolgung, BDE (5), Reporting, Stammdaten.
- **Neu ggü. v1.30.1** (`_Layout.cshtml`):
  - (a) Hauptmenüpunkt **„FA-Struktur“** zwischen Lager und Fertigungsaufträge. Er hat keinen Feature-Schalter und ist an
    die Rolle wie die FA-Liste gebunden (Bündel `_Layout.cshtml`, `showFaHierarchy`). Auf akenet01 (v1.30.1) gibt es ihn
    **nicht**: Menü ohne Link, `/FaHierarchy` liefert 404 (geprüft 20.10.2026, 08:13).
  - (b) Stammdaten › **„Arbeitsgänge“** nach Rollen.
  - (c) Stammdaten › **„Standorteinstellungen“** nach Service-Einstellungen.
- **FEHLER**, wenn „Beschichtung“, „Vormontage“ oder „Kommissionierlisten“ erscheinen (TS-89.7).

**B0.3 FA-Struktur auf AKE [L]**
- Weg: Menü FA-Struktur (`/FaHierarchy`).
- Erwartet: Die Seite lädt ohne Fehlerseite und ohne Skriptfehler und zeigt keine Strukturen bzw. einen Leer-Hinweis, weil
  der Struktur-Import aus ist.
- Text und Aussehen festhalten. Das Verhalten auf AKE ist in keinem Szenario beschrieben. Bei einer Fehlerseite: FEHLER.
  Zeigt sie Inhalt: Beobachtung, Einordnung durch die Koordination.

**B0.4 Einstellungen [L]**
- Weg: `/Settings`, `/ServiceSettings`, `/StandortEinstellungen`.
- Erwartet: die Werte aus A5. In `/StandortEinstellungen` zeigt die Karte „FA-Hierarchie – Master-Einwegtor“ **nicht
  aktiviert** und nicht gesperrt. Mandant/Dataset gleich `ake_TEST2026;1` bzw. dem Wert aus A5.
- Neu: die Gruppe „FA-Hierarchie“ in den Service-Einstellungen und die neuen AppSettings (BA §2.1 letzte Zeilen).
  Abweichung von A5 → FEHLER der Konfiguration (melden, nicht ändern).

**B0.5 Aktivitäts-Protokoll [L]**
- Weg: `/SyncLog`, Dienst-Filter.
- Erwartet: wie A6. Im Dienst-Dropdown alle Dienste aus BA §2.3.
- Neu: zusätzliche Dienstnamen im Dropdown. Nur Einträge dazu wären auffällig.

**B0.6 Arbeitsgang-Konfiguration umgezogen [L]**
- Weg: `/Settings/OperationConfig`.
- Erwartet: Weiterleitung auf Stammdaten › Arbeitsgänge (Changelog v1.54). Dieselben 12 Kürzel mit denselben Offsets
  und derselben Relevanz wie BA §2.1: A-BT 0 nein · B −1 · BG 2 · BG-SaP1 2 · EG 0 · MS 4 · RE 5 · RO 2 · RS 4 · SL 5 ·
  ST 0 · ZB 0 nein.
- Neu: Spalte „Quelle“ (bei diesen Einträgen „Fremdsystem“ oder „Manuell“), Zahnrad, Änderungsprotokoll. Kein Hinweis
  „nicht in Sage“, weil der Abgleich aus ist (TS-94, AKE-Regression).

### BS Service (Hintergrund-Jobs) — Priorität 1

Quelle der Job-Logik: `secondbrain\codebase\services.md` (Sync-Services, Gates, Entscheidungs-Helper). Im Browser sichtbar
ist nur das **Aktivitäts-Protokoll** `/SyncLog`. Deshalb vergleicht jeder Schritt die Counts des neuen Systems mit
akenet01 im **selben Takt**. Beide lesen denselben Sage-Mandanten, die Counts müssen also gleich oder erklärbar
verschieden sein.

**Vorgehen für alle BS-Schritte [L]:** Auf beiden Systemen `/SyncLog` mit Dienst-Filter öffnen und den letzten
abgeschlossenen Lauf lesen. Die Zeitpunkte sollten ≤ 15 Minuten auseinanderliegen. Festhalten: Zeitpunkt, Stufe, Counts
beider Systeme nebeneinander.

| Nr | Dienst (Filter) | Gate | Erwartet neues System im Vergleich zu akenet01 | Neu in der IDEAL-Version |
|---|---|---|---|---|
| BS1 | ProductionOrder | `Sync:ProductionOrdersEnabled` | erfolgreich; `neu`/`aktualisiert` wie akenet01 im selben Fenster. Erster Lauf nach der Migration darf höher sein | Upsert mit `SubOrderNumber` (SQL/90); bei AKE gilt immer `SubOrderNumber = OrderNumber` (A6-Abfrage = 0) |
| BS2 | ProductionOrderReconciliation | `Sync:ProductionOrderReconcileEnabled` | `storniert=0, reaktiviert=0` wie akenet01. Ein Storno, den akenet01 nicht hat → **sofort melden** (stärkstes Regressionssignal) | — |
| BS3 | Article | `Sync:ArticlesEnabled`, `Sync:ArticlesViewName` leer | `gelesen` **gleich** akenet01 (20.10.: 100600), `hauptlagerplatz_fehlt` gleich (106). **Keine** Warnung „View … nicht vorhanden“ (die käme nur bei gesetztem View-Namen) | Ab v1.40 liest der Sync den Matchcode: Beim ersten Lauf ist `aktualisiert` hoch (Matchcode wird befüllt), danach wie akenet01. Ab v1.60 läuft der Sync über `SageArticleSql.LegacySql`, laut Changelog zeichengleich zum alten Select |
| BS4 | FaZusatzinfo | `Sync:FaZusatzinfoEnabled` | gleiche Warnung wie akenet01: „View dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen nicht vorhanden“ (BA §6.1) | Auto-Erledigt hängt zusätzlich an `Sync:FaZusatzinfoAutoErledigtEnabled` (true) |
| BS5 | Lagerplatz | `Sync:LagerplaetzeEnabled` | `neu/aktualisiert/konflikte/deaktiviert` = 0 wie akenet01 | — |
| BS6 | Lagerbestand | `Sync:LagerbestandEnabled` | `einbuchungen/ausbuchungen/nullgesetzt` gleich akenet01, `uebersprungen` gleich (≈ 140) | — |
| BS7 | BomCache | `Sync:BomCacheEnabled` + Klasse-D-Gate (Master false → läuft) | `fa im fenster`, `fa gecacht`, `artikel ohne bom` gleich akenet01 (20.10.: 14 / 14 / 1). **Kein** Count `uebersprungen_hierarchisch` | Klasse-D-Gate (v1.36) |
| BS8 | FaWorkStepDetection | wie BS7 | `neu=0` nach dem ersten Lauf, `suchbegriffe … mit treffer` gleich (10). Kein `uebersprungen_hierarchisch` | Klasse-D-Gate |
| BS9 | CoatingDetection | wie BS7, `LackierteilKategorieName = Lackierteile` | `geprueft` und `mit_lackierteilen` gleich akenet01 (500 / 0) | Klasse-D-Gate |
| BS10 | OseonTracking, OseonWorkplaces, OseonArticleCategories | OSEON-Schalter | erfolgreich, Counts in derselben Größenordnung. Timeouts und Deadlocks gibt es auf akenet01 auch (BA §2.3), sie sind kein Versionsbefund | — |
| BS11 | EnaioDms | `Sync:EnaioDmsEnabled` | `aktualisiert` gleich (≈ 31264) | — |
| BS12 | Holiday, CleanupAktivitaetsprotokoll | Feiertag- bzw. Cleanup-Schlüssel | täglich erfolgreich; Feiertage 45 wie akenet01 (`/Settings`) | — |
| BS13 | PartRequisitionEmail, WarehouseRequisitionEmail | Mail-Schalter (A5 Teil 3) | aus → kein Lauf; an → wie akenet01 (`ohne_empfaenger` bei fehlender Zuordnung) | — |
| BS14 | SageLagerbuchung | `SageLagerbuchungAktiv` | aus → kein Lauf (A5 Teil 3) | — |
| BS15 | **nicht erwartet** | — | Kein Lauf `FaHierarchy`, `FaMaterialization`, `ProductionWorkplaceSync`, `WorkOperationStructureDetection`, `OperationConfigSync`, `HierarchieUmstellung` | Diese Dienste sind neu und bei AKE aus |
| BS16 | Fehler und Warnungen gesamt | — | `/SyncLog` mit Stufe Error, danach Warning: Liste vergleichen. **Nur** auf dem neuen System auftretende Meldungen einzeln festhalten | — |

**BS17 Service-Einstellungen nach dem Deploy [L]**
- Weg: `/ServiceSettings`.
- Erwartet: Jeder Schalter, der auf akenet01 an ist (BA §2.2), ist auch hier an. Laut services.md greifen appsettings-Werte
  nicht mehr, jeder Sync muss hier aktiv sein. Ein vergessener Schalter zeigt sich in BS1–BS14 als fehlender Lauf —
  dann FEHLER der Konfiguration (melden, nicht ändern).

### BV Parallelvergleich akenet01 ↔ neues System — Priorität 1 [L auf beiden Systemen]

Beide Systeme im selben 15-Minuten-Fenster öffnen (nach BS). Cowork arbeitet lesend auf akenet01 und `{SYSTEM_URL}`. Der
Auftrag muss **beide** Adressen nennen. Unterschiede festhalten als: Feld | akenet01 | neu.

> Was **gleich** sein muss, kommt aus Sage oder ist daraus berechnet. Was **abweichen darf**: Daten, die nach dem Zeitpunkt
> der DB-Kopie in einem der Systeme von Hand geändert wurden (Freigaben, Buchungen, Status) — deshalb BV **vor** den
> [S]-Schritten ausführen — sowie die Spalten und Elemente unter „Neu“ in B1–B3.

**BV1 FA-Liste**
- Weg: `/ProductionOrders`, ohne Filter, Seitengröße „Alle“.
- Vergleich je FA: FA Nr., Stk., Kunde, Artikelnummer, Bezeichnung 1/2, Werkbank, Kältemittel, Ventil, Ausführung E/Z,
  Maschine, Sage-Status, **Beschicht., BG-Termin, Komm., Fert.-Termin, Liefertermin**, Lack-T.
- Erwartet: dieselbe FA-Menge, dieselben Werte. Einzige gewollte Abweichung: die Spalte Matchcode.
- Danach dasselbe mit „Erledigte anzeigen“ (Badge „In Sage gelöscht“ identisch).

**BV2 Terminrechnung**
- Die Termine kommen nicht aus Sage, sondern werden berechnet. Formel laut Einstellungen und Hilfe, mit den Werten aus
  BA §2.1 (KommissionierTage 4, VorkommissionierTage 1, BeschichtungTage 10, Abholtage Di/Do):
  - Komm. = Fert.-Termin − 4 Arbeitstage.
  - BG = Komm. − 1 Arbeitstag, oder der Wert „Abweichende Vorkommissioniertage“ der Werkbank.
  - Beschichtung = BG − 10 Arbeitstage, dann zurück auf den vorherigen Abholtag. Nur bei FAs mit Lackierteilen, weil
    `LackierteilKategorieName` gesetzt ist.
- Stichproben aus akenet01 am 20.10., nachgerechnet:
  - 2604043: Fert. 19.08. → Komm. 13.08. → BG 12.08.; keine Beschichtung.
  - 2625004: Fert. 22.07. → Komm. 16.07. → BG 15.07. → −10 AT = 01.07. (Mi) → Abholtag **Di 30.06.**
- Erwartet: identische Termine auf beiden Systemen für alle FAs aus BV1. Abweichende Werkbank-Werte: A2 = 0, C1 = 15 Tage.
  Gibt es einen offenen FA an A2 oder C1, ihn ausdrücklich vergleichen (Badge „BG n“ neben der Werkbank).

**BV3 Leitstand**
- Weg: `/PickingLeitstand`.
- Vergleich: Glas, Zukauf, VK–VA (Status), Status, Freigabe, Prio, Kommissionierer.
- Erwartet: identisch (stammt aus der DB-Kopie). Abweichung nur, wenn seit der Kopie auf akenet01 freigegeben wurde — dann
  die Zeile benennen.

**BV4 Stückliste**
- Für `{FA_A}` und zwei weitere FAs (einer mit Quelle SAGE, einer mit OSEON, wenn vorhanden): `/Picking/Bom/<Id>` auf beiden
  Systemen, alle Baugruppen aufgeklappt.
- Vergleich: Quelle-Badge, Zeilenzahl, je Zeile Pos., Baugruppe, Ressourcenummer, Bezeichnung 1/2, Menge, Beschaffung,
  Artikelgruppe, Kategorie, **Menge der Lagerplätze als Menge** (die Darstellung „+n weitere“ ist neu, der Inhalt
  nicht).
- Erwartet: identisch bis auf Matchcode und die Lagerplatz-Darstellung. Mengen sind keine Hochrechnung: sie müssen
  zeichengleich sein.

**BV5 Kommissionier-Worklist**
- `/Picking?showAll=true`: gleiche FAs, Reihenfolge nach Prio, gleiche Kommissionierer.

**BV6 Bestände aus Sage**
- `/StockOverview`, Lagerplatz-Filter je Sage-Platz (z. B. `GL;0;0;0` und zwei weitere mit Bestand), dazu Artikelinfo für
  `{ART_BESTAND}` und zwei weitere Artikel.
- Erwartet: Nach je einem Lagerbestand-Lauf (BS6) auf beiden Systemen sind die Mengen auf **Sage-Plätzen** identisch.
  Manuelle Plätze (Test, Wagen …) dürfen abweichen, wenn seit der Kopie gebucht wurde.
- Reihenfolge der Plätze: Hauptlagerplatz zuerst, auf beiden gleich.

**BV7 Artikel und Lagerplätze**
- `/Articles`: Gesamtzahl (20.10.: 105632).
- `/StorageLocations`: Gesamtzahl (3293); Filter „Nur buchbare“ (10).
- Erwartet: gleich.

**BV8 Ergebnis**
- Tabelle aller Abweichungen mit Einordnung „gewollt laut Neu“ / „erklärbar durch Buchung nach der Kopie“ / „unerklärt“.
  Nur „unerklärt“ zählt als FEHLER.

### B1 Lager

**B1.1 Einbuchung, Formular [L]**
- Weg: `/StockMovements/Inbound`.
- Erwartet (BA §4.1):
  - Felder Artikel, Menge (**vorbelegt 1**, min 0,001), Lagerplatz (nur buchbare, ohne Wagen), Fertigungsauftrag.
  - Tabelle offener Bedarfsmeldungen. Knopf „Einbuchung speichern“.
- Neu: Die Artikelsuche schlägt erst **ab 3 Zeichen** vor. Prüfen mit 2 Zeichen (keine Treffer) und 3 Zeichen (Treffer).
  Sie findet Artikel auch über den **Matchcode** (`{ART_MATCHCODE}`) (Changelog v1.40).

**B1.2 FA-Lagerplatz-Hinweis [L]**
- Aktion: in der Einbuchung die FA `{FA_A}` eintippen, Feld verlassen.
- Erwartet laut Hilfe:
  - Ein Platz mit Bestand → Platz übernommen.
  - Mehrere Plätze → gelbe Warnung mit Mengen.
  - Kein Bestand → kein Hinweis.
- Ergebnis und Text festhalten (TS-2.12 ff.).

**B1-S1 Einbuchung [S]**
- Aktion: `{ART_BESTAND}`, Menge 1, `{LP_TEST}`, ohne FA → speichern.
- Erwartet: Erfolgsmeldung. In der Bewegungshistorie ganz oben „Einbuchung“, Benutzer Gerald. Der Bestand auf
  `{LP_TEST}` ist +1. **Kein** neuer Eintrag in Sage-Lagerbuchungen (Platz ohne Sage-Meldung).
- Spur: Bewegung.

**B1-S2 Ausbuchung [S]**
- Aktion: gleicher Artikel, Menge 1, `{LP_TEST}`.
- Erwartet: Bestand wieder alt, Bewegung „Ausbuchung“. Das Mengenfeld ist beim Öffnen **0** (unverändert, Changelog v1.30).

**B1-S3 Umbuchung [S]**
- Aktion: Menge 1 von `{LP_TEST}` nach `TEST 1` (vorher B1-S1 erneut).
- Erwartet: eine Bewegung „Umbuchung“, kein Sage-Eintrag.

**B1-S4 Mehrfach-Einbuchung [S]**
- Weg: `/StockMovements/InboundBulk`, Platz `{LP_TEST}`.
- Aktion: zwei Zeilen mit demselben Artikel und eine leere Zeile → speichern.
- Erwartet: **nichts gebucht**, Eingaben bleiben, leere Zeile markiert. Leere Zeile entfernen → speichern → Buchung.
- Neu: keine. Bezug TS-2.23/2.24.

**B1-S5 Sage-Meldung [S, nur mit Haken]**
- Aktion: Einbuchung `{ART_BESTAND}` 1 Stk auf `{LP_SAGE}`.
- Erwartet: Unter Sage-Lagerbuchungen ein neuer Eintrag „Offen“, nach ≤ 20 s „Gesendet“ bzw. „Bestätigt“
  (oder „Fehler“ mit Text). Danach mit B1-S2 auf `{LP_SAGE}` wieder ausbuchen.
- Ergebnis und Status mit Zeit festhalten.

**B1-S6 Zwillingstest Lager-Logik [S auf beiden Systemen, nur mit eigenem Haken]**

Dieselbe Aktion auf akenet01 und auf dem neuen System; Meldungstext, Ergebnis und Bewegung müssen gleich sein. Testplatz
auf beiden: `{LP_TEST}`. Nichts auf Sage-Plätzen.
- (a) **Ausbuchung über Bestand:** Bestand von `{ART_BESTAND}` auf `{LP_TEST}` ablesen, dann Bestand + 1 ausbuchen.
  Erwartet: gleiches Verhalten auf beiden. `NegativeBuchungErlaubt = AN`, Fallback-Platz NAN; ob die Differenz auf NAN
  gebucht oder abgewiesen wird, zeigt akenet01 — das neue System muss es gleich tun. Danach zurückbuchen.
- (b) **Umbuchung auf belegten Wagen:** Auf `{WAGEN}` liegt Ware zu FA X; aus der Stückliste eines anderen FA Y auf
  denselben Wagen umbuchen. Erwartet: Modal „Kommissionierwagen belegt“ auf beiden, gleicher Text, Abbruch möglich.
- (c) **Menge 0 oder leer** bei Ein-, Aus- und Umbuchung: gleiche Validierung (min 0,001).
- (d) **Einbuchung mit offener Bedarfsmeldung:** Artikel einer offenen Bedarfsmeldung einbuchen und die Meldung auswählen.
  Erwartet: die Meldung ist danach „Erfüllt“, auf beiden gleich. Nur, wenn eine passende Meldung existiert.

**B1.3 Bestände [L]**
- Weg: `/StockOverview`.
- Aktionen:
  1. Filter Artikel `{ART_BESTAND}` → 1 Zeile.
  2. Filter FA `{FA_A}` → Netto je Platz.
  3. Spaltenfilter Lagerplatz `GL;0;0;0`.
- Erwartet: wie BA §4.1, Hauptlagerplatz mit ★.
- Neu (v1.58):
  - Spaltenfilter mit `=`, `%`, `(leer)`, ODER mit `;` und dem „?“-Knopf.
  - Filter Bestand `>100` wirkt. `5,5` in einer Zahlspalte ist eine Dezimalzahl, kein ODER.
  - Knopf **CSV** neben dem Zahnrad (Download nur nach Rückfrage, B8.2).

**B1.4 Bewegungshistorie [L]**
- Weg: `/StockMovements`.
- Aktionen:
  1. Bewegungsart-Dropdown: 5 Werte wie BA §4.1.
  2. Spaltenfilter „Bewegungsart“ = `Ausbuchung`.
  3. „Datum/Zeit“ = heutiges Datum, Monat `MM.JJJJ`, Jahr.
  4. FA-Filter `{FA_A}-1` (WA-Format) über den Scan-Knopf, falls ohne Kamera nicht möglich: manuell `{FA_A}`.
- Erwartet: Filter wirken (Changelog v1.30); `Menge` hat keinen Textfilter.
- Neu: Matchcode im Artikelfilter suchbar (v1.40); neue Syntax.

**B1.5 Sage-Lagerbuchungen [L]**
- Weg: `/SageBookingQueue`.
- Erwartet: Statusfilter, Spalten wie BA §4.1; „Erneut senden“ nur an Fehler-Zeilen. Gelbes TLS-Banner nur, wenn die
  Zertifikatsprüfung aus ist.
- Bei DB-Quelle akenet01: die Einträge #1–#16 übernommen.

**B1.6 Lagerplatz ausbuchen / umbuchen [L]**
- Weg: `/StockMovements/OutboundAll` und `/StockMovements/LocationTransfer`.
- Aktion: Platz `{LP_TEST}` → „Laden“.
- Erwartet: Bestandsliste des Platzes, Knopf „Alle X Artikel …“. **Nicht** auslösen.

### B2 Fertigungsaufträge

**B2.1 FA-Liste [L]**
- Weg: `/ProductionOrders`.
- Erwartet:
  - Gleiche Anzahl offener FAs wie die Quelle (akenet01: 14), **flache Liste**: keine Gruppenköpfe, keine
    „Übergeordnete Sub-FA“, keine Zählung „N Aufträge · M Sub-FAs“.
  - Spalten wie BA §4.2, Datumsspalten mit KW, je Zeile Stückliste, enaio, VAULT und OSEON-i.
- Neu:
  - Spalte **Matchcode**, standardmäßig sichtbar. Sie bleibt leer, bis der Artikel-Sync Matchcodes geliefert hat — das ist
    kein Fehler (Changelog v1.38/v1.40).
  - Knopf CSV.
  - Filter Fert.-Termin mit `>=01.10.2026`, Bereich `01.10.2026...31.12.2026`, ODER mit `kw41;kw42`.
- Bezug: TS-69.9, TS-72, TS-73.20.

**B2.2 Filter wie bisher [L]**
- Aktionen:
  1. Kunde `Nadler` → 3 Treffer (akenet01: 2614027/167/168).
  2. FA `{FA_A}` → 1.
  3. Artikelnummer `S0309613` → 2 (2604043, 2621027).
  4. „Erledigte anzeigen“ an → mehr Zeilen.
  5. Spaltenfilter Werkbank `!A3` → ohne A3.
  6. Text-ODER **mit Komma** `A3,D1` → A3 und D1. Das Komma bleibt in Textspalten erlaubt (TS-91.B10).

**B2.3 Zahnrad [L]**
- Aktion: Zahnrad öffnen; Kältemittel ausblenden → Spalte weg; „Auf Standard zurücksetzen“ → wieder da.
- Erwartet: Zahnrad sichtbar und bedienbar. Zur Regel `[hidden]` siehe B8.1.
- Anzeige-Einstellungen am Ende wiederherstellen.

**B2.4 FA-Vervollständigen [L]**
- Weg: `/FaCompletion`, dann `/FaCompletion/Edit/<Id {FA_A}>`.
- Erwartet: Liste wie BA §4.2. Edit mit Reiter ALLGEMEIN und den FA-Vorbau-AG-Reitern, Werkbank-Auswahl, „+ FA-Vorbau-AG“,
  Merkmale, Spezifikationen. Stückliste read-only ohne Picking-Spalten.
- Neu: In der read-only-Stückliste die Spalte Matchcode (TS-95.3).

**B2-S1 Merkmal speichern [S]**
- Aktion: im VK-Reiter von `{FA_A}` ein Dropdown-Merkmal setzen und wieder auf „(leer)“.
- Erwartet: Nach dem Speichern bleibt der VK-Reiter aktiv; der Wert erscheint in der Abarbeitungsliste (B2.5).

**B2.5 FA-Abarbeitungsliste [L]**
- Weg: `/FaWorklist`, ohne Auswahl, dann FA-Vorbau-AG = VK.
- Erwartet:
  - Ohne AG der Hinweis „Bitte zuerst einen FA-Vorbau-Arbeitsgang auswählen …“.
  - Mit VK Spalten wie BA §4.2 (akenet01: 13 Zeilen), Statusauswahl Offen / in Bearbeitung / Fertig.
- Neu: Matchcode-Spalte, CSV.

**B2-S2 Status [S]**
- Aktion: in einer Zeile Status „in Bearbeitung“ → bleibt sichtbar; „Fertig“ → Zeile verschwindet, mit „Erledigte anzeigen“
  wieder da; zurück auf „Offen“.
- Erwartet: Der Leitstand zeigt in der VK-Zelle denselben Wert (BA §4.3).

### B3 Leitstand und Kommissionierung

**B3.1 Leitstand [L]**
- Weg: `/PickingLeitstand`.
- Erwartet: Spalten wie BA §4.3 inklusive Glas, Zukauf, VK–VA (3-Wert-Auswahl), Status, Freigabe, Kommissionierer.
- **„Freigeben“ an jeder nicht freigegebenen Zeile.** Am AKE ist jede FA ihre eigene HauptFA (`IsHauptFa`, Changelog v1.46
  „Am Standort AKE ändert sich nichts“). Kein Knopf „Alle Sub-FAs fertigmelden“.
- Neu: Matchcode-Spalte, CSV, neue Filtersyntax.
- Bezug: TS-79.9, TS-79.14.

**B3-S1 Freigabe mit Zuweisung [S]**
- Aktion: an `{FA_B}` „Freigeben“.
- Erwartet: Modal „Kommissionierer zuweisen“ mit Pflichtfeld (Zuweisung an). Nach Auswahl ist die Zeile freigegeben, die
  Prio wird auf die nächste freie Nummer vorgeschlagen, das Badge im Menü erhöht sich um 1.
- Danach „Zurücknehmen“ → Badge wie vorher.

**B3-S2 Sammelfreigabe [S]**
- Aktion: zwei Zeilen markieren → „Markierte freigeben“.
- Erwartet: ein Modal mit Kommissionierer, beide freigegeben. Danach „Markierte zurücknehmen“.

**B3.2 Kommissionierung (Worklist) [L]**
- Weg: Menülink Kommissionierung.
- Erwartet:
  - Ist Gerald nicht zugewiesen: „Aktuell sind keine Aufträge für Sie …“.
  - Mit „Alle anzeigen“ die freigegebenen FAs mit Prio, FA, Artikel, Bezeichnung, Kunde, Stk., Komm.-Termin (KW), Status,
    Kommissionierer (akenet01: 1 Zeile `{FA_FREI}`).
- Neu: Matchcode-Spalte. Die Warteschlange zeigt nur HauptFAs — am AKE also alle.

**B3.3 Stückliste [L]**
- Weg: `/Picking/Bom/<Id {FA_A}>`.
- Erwartet (BA §4.3): Quelle-Badge (SAGE oder OSEON), Zeilenzahl wie die Quelle (akenet01: 106), Spalten, Baum auf/zu,
  Quell-Lagerplatz-Dropdowns laden per Ajax, Wagen-Umbuchung, Modals, Fotos, „Stückliste drucken“.
- Neu:
  - (a) Spalte **Matchcode** nach „Bezeichnung 2“ mit Werten aus dem Artikelstamm (TS-95.2).
  - (b) Spalte **Lagerplatz** zeigt höchstens 3 Plätze (Hauptplatz zuerst, dann nach Menge), den Rest unter
    „+n weitere“. Die Spalte ist filterbar und findet auch verdeckte Plätze (v1.62).
  - (c) Ist ein Profil-Standardfilter aktiv, oben ein **Hinweis-Badge** mit Wert und Knopf zum Aufheben (v1.42; Geralds
    Profil hat Bezeichnung 1 = „Verdampfer“).
  - (d) **Zahnrad sichtbar** rechts über der Tabelle (v1.47.3).
  - (e) „?“-Knopf der Filtersyntax.
- **Kein** Komm.-Ziel-Filter, keine Spalten Ebene, Vater-Sub-FA oder Sage-Pos. (nur IDEAL).

**B3.4 Stückliste drucken [L]**
- Aktionen:
  1. „Stückliste drucken“ ohne Filter → Druckansicht öffnet (Druckdialog mit Escape schließen).
  2. Filter so setzen, dass **keine** Zeile sichtbar ist → „Stückliste drucken“.
- Erwartet:
  1. Druck per POST, auch bei großer Stückliste kein „HTTP-Fehler 404.15“ (v1.30.1).
  2. Kein Druck, Hinweis „Keine sichtbaren Positionen – nichts zu drucken.“ (neu v1.46).
- Im Druck mit aktivem Filter steht im Kopf „Gefilterte Ansicht“ (v1.42).

**B3-S3 Kommissionieren [S]**
- Aktion: in `{FA_A}` zwei Bauteile anhaken, Quell-Lagerplatz wählen, Ziel `{WAGEN}` → „Gepickte Artikel umbuchen“.
- Erwartet: zwei Umbuchungen in der Historie; der Wagen erscheint **nicht** in Bestands- und Quell-Dropdowns der Stückliste.
- Danach mit „Lagerplatz umbuchen“ `{WAGEN}` → `{LP_TEST}` zurück (Bestätigung). Das ist dann auch der [S]-Test zu B1.6.

**B3.5 Quell-Lagerplatz-Vorschlag [L]**
- In der Stückliste von `{FA_A}` die Spalte „Quell-Lagerplatz“ an drei Bauteilen mit Bestand ansehen.
- Erwartet (laut Changelog v1.14 und v1.25): Vorschlag ist der Hauptlagerplatz, sonst der buchbare Platz mit der höchsten
  Menge, sonst NAN. Nie ein nicht buchbarer Platz oder ein Wagen. Gleich wie auf akenet01 (BV4).

**B3-S5 FA abschließen [S]**
- Aktion: an `{FA_B}` (nach B3-S1 zurückgenommen) „Abgeschlossen“ bzw. den Erledigt-Knopf wählen.
- Erwartet: Der FA verschwindet aus FA-Liste, Leitstand und Worklist und ist mit „Erledigte anzeigen“ wieder sichtbar
  (Changelog v1.21.1). Danach, wenn die Anwendung es zulässt, wieder öffnen; sonst festhalten.

**B3-S6 Lagerbestellung aus der Stückliste [S]**
- Aktion: in `{FA_A}` ein Bauteil mit einer Glas-Artikelgruppe (880–886) und eines mit einer anderen Gruppe je über
  „Lagerbestellung“.
- Erwartet: Das Glas-Bauteil landet im offenen Glas-Entwurf, das andere im Lager-Entwurf; ein Entwurf wird neu angelegt,
  wenn keiner offen ist. Werkbank = erste eigene Werkbank (Hilfe). Danach die Positionen wieder entfernen.

**B3-S4 Status [S]**
- Aktion: Modal „Kommissionierung Status“ → „Teilkommissioniert“.
- Erwartet: Der Status im Leitstand wechselt. Danach wieder auf den Ausgangsstatus, sofern möglich; sonst festhalten.

### B4 Bestellungen

**B4.1 Bedarfsmeldungen [L]**
- Weg: `/PartRequisitions`.
- Erwartet: Spalten wie BA §4.4, „Stornieren“ mit Modal (nicht auslösen), „Alle anzeigen“.
- Neu: Suche findet über den Matchcode.

**B4-S1 Bedarfsmeldung [S]**
- Aktion: in `{FA_A}` an einem Bauteil „Bestellen“, Menge 1, Priorität Normal, Bemerkung „Regressionstest“.
- Erwartet: erscheint in `/PartRequisitions` als „Offen“; Mail laut A5. Danach stornieren.

**B4.2 Lagerbestellungen — meine Listen [L]**
- Weg: `/WarehouseRequisitions`.
- Erwartet: Reiter Lager und Glas, „+ Neue Liste“; im Glas-Reiter `{LB_ENTWURF}`.

**B4-S2 Lagerbestellung erfassen [S]**
- Aktion: Reiter Lager → „+ Neue Liste“ → Artikel mit 3 Zeichen suchen → Menge 2. Eine unbekannte EK-Nummer → DUMMY mit
  Bezeichnung. Kommentar setzen → „Abschicken“.
- Erwartet: „Wird automatisch gespeichert“; DUMMY-Position möglich; nach dem Abschicken unter „Lager: Eingehende Listen“ mit
  Kommentar sichtbar.
- Ein Glas-Artikel (Gruppe 880–886) wird im Lager-Reiter **nicht** angeboten.

**B4.3 Lager: Eingehende Listen und Detail [L]**
- Weg: `/WarehousePicking`, Detail `{LB_OFFEN}`.
- Erwartet:
  - Statusfilter wie BA §4.4.
  - Im Detail: Ist-Felder **leer**, Notiz Lager, Notiz EK, Radios Fehlteil / Wird nicht nachgeliefert, Knöpfe „Drucken und
    Abschließen“, „Speichern + Abschließen“, „Stornieren“.
- Zahnrad rechts über der Tabelle sichtbar.

**B4-S3 Abschließen ohne Ist [S]** (an der eigenen Bestellung aus B4-S2)
- Aktion: „Speichern + Abschließen“ ohne Ist-Mengen.
- Erwartet: gelber Hinweis mit den offenen Positionen, Liste bleibt offen. **Danach muss das Zahnrad weiter sichtbar sein**
  (Fix v1.47.5).
- Dann Pos. 1 Ist = 2, Pos. 2 Ist leer + „Fehlteil“ → abschließen.
- Erwartet: Status „Teilgeliefert“; die Position erscheint unter „Lager: Fehlteile“ › Offene Fehlteile. Ein leeres Ist wird
  nie als 0 gespeichert.
- Bezug TS-18.10.

**B4.4 Fehlteile [L]**
- Weg: `/MissingParts`, `/MissingPartsLager`.
- Erwartet: Reiter mit Anzahl, Spalten wie BA §4.4, Lager zusätzlich „Lagerplatz (Bestand)“.
- Neu: Ausschluss `!a;b` wirkt auf alle Begriffe (v1.58.1).

### B5 Teileverfolgung, Reporting, Arbeitsgänge

**B5.1 Rückmeldungen [L]**
- Weg: `/Tracking`.
- Erwartet: FA-Gruppen mit Fortschritt, „Rückmelden“ je AG, Filter. Nichts zurückmelden.

**B5.2 OSEON Aufträge [L]**
- Weg: `/Tracking/OseonIndex`.
- Erwartet: Kundenaufträge paginiert (akenet01: 490 / 20 Seiten). Aufklappen lädt Subaufträge per Ajax mit Spinner.
  Ampelfarben; Artikelfilter; „Fertige anzeigen“; Lagerbestand-Modal.
- Gegenprobe: aus der FA-Liste der i-Knopf → vorgefiltert auf den FA.

**B5.3 OSEON AG-Übersicht [L]**
- Weg: `/OseonReporting/OperationsOverview`.
- Erwartet: KPI-Karten, Tabs, Filter. Banner „… AG(s) ohne Config-Eintrag“ mit ähnlicher Zahl wie akenet01 (10518 am
  20.10.).
- Neu: Der Banner verweist auf die Arbeitsgang-Stammdaten (TS-16.5). Ampeln und Termine unverändert (TS-94,
  AKE-Regression).

**B5-S1 Arbeitsgang ohne Offset [S]**
- Weg: Stammdaten › Arbeitsgänge.
- Aktion: neuen Eintrag `ZZTEST` ohne Offset speichern.
- Erwartet: gelbe Meldung, **nichts gespeichert** (neu v1.55).
- Mit Offset 0 speichern → angelegt, Quelle „Manuell“ → danach wieder löschen bzw. deaktivieren.

### B6 BDE — Priorität 3, BDE derzeit nicht im Einsatz (AKE: `BdeNurFaMeldung = AN`, Terminal an `{TERM_WB}`)

**B6.1 Terminal öffnen [L]**
- Weg: `/BdeTerminal`.
- Erwartet: Kopf „Werkbank: …“ und angemeldeter Benutzer, Schritte 1. Operator / 2. Arbeitsgang wählen, Aktuelle Buchungen,
  Tageshistorie, ggf. Hinweis auf pausierte Aufträge (BA §4.6).

**B6-S1 NurFA unverändert [S]** (TS-97.10)
- Aktion: Operator `{OP_PNR}` scannen bzw. eingeben → Liste „Produktionsaufträge“ → `{FA_TERM_EIGEN}` starten.
- Erwartet: Start wie bisher, grüner Toast, Buchung unter „Aktuelle Buchungen“, Ziel `<FA>/01`. Keine Gruppenköpfe, kein
  „Weitere“.
- Neu (v1.64): der gewählte Eintrag ist markiert, die Aktionsknöpfe stehen unter der Karte „2. Arbeitsgang wählen“.

**B6-S2 Mengen [S]**
- Aktion: „Mengen melden“ bzw. Pause mit Gut = **−1**.
- Erwartet: abgewiesen, Feld min = 0, nichts gebucht (neu v1.64).
- Dann Gut 1, Ausschuss 0 → Pause → Fortsetzen → Beenden mit Gut 1.
- Erwartet: in `/BdeBookings` (heute) Einträge mit Status wie BA §4.6.

**B6-S3 FA einer anderen oder keiner Werkbank [S] — zentraler AKE-Prüfpunkt**
- Aktion: am Terminal `{TERM_WB}` den FA `{FA_TERM_FREMD}` starten.
- Referenz v1.30.1 (am 20.10. auf akenet01 nachgesehen, FA-Liste mit „Erledigte anzeigen“): Heute wird an fremden
  Werkbänken gebucht.
  - Buchung #56 „2614157/01“ an **Stanzarei**, FA-Werkbank **A1**.
  - #58 „2609192/01“ an Stanzarei, FA-Werkbank A1.
  - #62 „2605166/01“ an Stanzarei, FA-Werkbank A2.
  - #59–#61 an A2 auf FAs mit Werkbank A2 bzw. A1 (2609192).
  - Einschränkung: Die Liste zeigt die heutige Werkbank; ob sie zum Buchungszeitpunkt dieselbe war, ist ungeprüft.
- Erwartet laut TESTSZENARIEN TS-96.10 c: „FA der Terminal-Werkbank angenommen …, FA einer anderen oder ohne Werkbank
  abgewiesen“ (dort nur Unit-Test `NurFa_ChecksOrderWorkplace_NotAgWorkplace`).
- Ergebnis **genau festhalten** (angeboten in der Liste? Start angenommen oder Meldung mit Text?). Das ist eine mögliche
  **Verhaltensänderung für den AKE-Alltag**. Die Koordination ordnet ein, ob sie gewollt ist.

**B6-S4 Pausierte Buchung an anderer Werkbank fortsetzen [S]**
- Nur, wenn Gerald ein zweites Terminal oder eine zweite Werkbank einrichtet.
- Erwartet (v1.63): Fortsetzen nur an einer Werkbank derselben Familie. AKE-Werkbänke ohne Kürzel bilden je eine eigene
  Familie, also wird es abgewiesen.
- Sonst NICHT PRUEFBAR.

**B6.2 Cockpit [L]**
- Weg: `/BdeCockpit`.
- Erwartet: BDE-aktive Werkbänke (akenet01: A1, A2, A3, Stanzarei). Während B6-S1 ist die Werkbank „Produktion“ (grün),
  Aktualisierung ca. 5 s.

**B6.3 Buchungen [L]**
- Weg: `/BdeBookings`.
- Erwartet: Filter Von/Bis (Standard heute), Operator, Werkbank; Spalten und Aktionen wie BA §4.6.
- Neu: Spaltenfilter typisiert (Gut `>0`, Start als Datum), CSV.

**B6.4 Stammdaten, Schichtkalender, Werkbänke [L]**
- Weg: `/BdeMasterData` (3 Reiter), `/BdeShiftCalendar`, `/ProductionWorkplaces`, Edit einer BDE-Werkbank.
- Erwartet: wie BA §4.6 und §4.7.
- Neu:
  - In `/ProductionWorkplaces` die Spalte „Nicht in Sage seit“ (leer bei allen AKE-Werkbänken).
  - Im Edit die Option **„Nur AG-Fertigmeldung“ (aus)** und ein pflegbares Kürzel-Feld, weil die AKE-Werkbänke keine
    Sage-Nummer haben (TS-96.9).
  - Abweichende Vorkommissioniertage unverändert (A2 = 0, C1 = 15).

### B7 Stammdaten und Verwaltung

**B7.1 Artikel und Artikelinfo [L]**
- Weg: `/Articles`, Suche `{ART_MATCHCODE}`; `/Articles/Info` mit `{ART_BESTAND}` und mit dem **exakten Matchcode**.
- Erwartet: wie BA §4.7.
- Neu:
  - Matchcode-Spalte und -Suche.
  - Die Artikelinfo findet über den exakten Matchcode; ein Teiltext reicht nicht (v1.40).
  - „Teil enthalten in“ zeigt am AKE die FA wie bisher.

**B7.2 Lagerplätze, Kategorien, Merkmale, FA-Vorbau-AG, FA-Merkmale, Empfänger [L]**
- Aktion: je Liste öffnen, eine Edit-Seite öffnen, nichts speichern.
- Erwartet: Inhalte wie BA §4.7. Etiketten-Druckansicht öffnet; Druckdialog mit Escape schließen.

**B7.3 Benutzer, Rollen, Profil [L]**
- Weg: `/Users`, `/Users/RoleOverview`, `/Roles`, `/Account/Profile`.
- Erwartet: Benutzerliste mit Typ AD/Lokal; „AD-Benutzer anlegen“ schlägt Gruppenmitglieder vor (nicht anlegen).
- Rollen: die 17 aus BA §2.4.
  - Neu: `beschichtungsauftrag` / `beschichtungsauftrag_schreiben` **nur**, falls per Skript angelegt (laut Bericht kein
    Seed). Festhalten, was erscheint.
- Profil: Felder wie BA §4.7. Das Feld „Standard-Kommissionierziel“ erscheint am AKE **nicht** (v1.42: „nur am Standort
  IDEAL“).

**B7.4 Hilfe [L]**
- Weg: `/Help`.
- Erwartet: alle Kapitel aus akenet01. Neu: Kurzanleitungen (CSV, Filtersyntax „oder mit ;“, Arbeitsgänge); die Karte
  „Beschichtung (IDEAL)“ ist erlaubt.
- Den Link „Hilfe“ in der Fußzeile prüfen.

### B8 Querschnitt (auf allen Listen aus B1–B7)

**B8.1 Ausgeblendete Elemente und Zahnrad [L]**
- Hintergrund: Die globale Regel `[hidden]{display:none!important}` ist seit v1.46 neu (Brain, fallstricke).
- Aktion: auf jeder Liste prüfen, ob die Hinweisboxen und das Zahnrad an derselben Stelle erscheinen wie auf akenet01:
  - Stückliste: Standardfilter-Box.
  - Einbuchung: FA-Hinweis.
  - Lagerbestellung: gelber Hinweis.
  - Leitstand: Sammel-Leiste nach dem Markieren.
  - Kommissionierung: „Alle anzeigen“.
- Fehlt ein Element, das in BA §4 beschrieben ist: FEHLER mit Screenshot.

**B8.2 CSV [L, Download nur nach Rückfrage]**
- Aktion: FA-Liste mit Filter Werkbank `A3` → CSV.
- Erwartet: Die Datei enthält genau die gefilterten Zeilen über alle Seiten, die sichtbaren Spalten in Bildschirm-Reihenfolge,
  Umlaute korrekt, Datum ohne KW, Haken als Ja/Nein (TS-86.1).

**B8.3 Pagination und Seitengröße [L]**
- Aktion: Bestände auf 50 → 2. Seite → Browser zurück.
- Erwartet: Filter und Seite stehen in der Adresse; „Alle“ ist bei 5.000 gekappt (Banner).
- Am Ende die Seitengröße zurück.

**B8.4 Filter-Persistenz [L]**
- Aktion: Spaltenfilter setzen → F5.
- Erwartet: Der Filter bleibt im selben Tab; „Zurücksetzen“ leert ihn.

---

## Teil C — Gerald, von Hand (Cowork kann das nicht)

| Nr | Prüfung | Erwartet | Bezug |
|---|---|---|---|
| C1 | **Drucker**: große Stückliste (> 100 Zeilen), Lagerbestellung „Drucken und Abschließen“, Lagerplatz-Etikett | Druck vollständig. Prüfen, ob „Drucken reagiert erst beim zweiten Klick“ (offene Beobachtung TS-80) auch an AKE-Druckern auftritt | v1.30.1, Übergabe 2026-09-30 |
| C2 | **Mails** (umgeleitet): Bedarfsmeldung, Lagerbestellung (Lager und Glas, Betreff „Glasbestellung #…“), Storno „[STORNO]“, Meldebestand, Fehlermail | Inhalt und Links zeigen auf `{SYSTEM_URL}`; der Kommentar steht in der Bestellmail | A5 |
| C3 | **Sage**: Buchung aus B1-S5 im Sage-Testmandanten sichtbar; Requeue eines Fehler-Eintrags ohne Doppelbuchung | wie Hilfe „Sage-Lagerbuchungen“ | TS-56 |
| C4 | **Scanner und Kamera**: Artikel-QR mit FA an 3. Stelle (QrMitFaNummer), Lagerplatz-Barcode, WA-Code in der Historie, iPad-Kamera | Scan setzt Artikel, FA und Platz | TS-2.x, TS-30 |
| C5 | **Rollen mit echten Benutzern**: je ein Benutzer nur picking, nur stock, nur stock_read, nur lagerbestellung, nur glasbestellung, nur vorbau, nur fa_completion, nur leitstand, nur bde_user, nur masterdata_read | Menüs und Zugriffe wie `/Users/RoleOverview`. Neu: Picker, Tracker, Leitstand und Vorbau sehen „FA-Struktur“ | TS-34, `_Layout.cshtml` |
| C6 | **Windows-Anmeldung**: Auto-Login, Abmelden und Formular, „Mit Windows anmelden“, AD-Benutzer anlegen | wie v1.23–v1.25 | TS-40 |
| C7 | **BDE Auto-Pause** am Schichtende (laufende Buchung über das Schichtende) | Status AutoPaused mit Schicht-Endzeit | TS-15 |
| C8 | **FA-Zusatzinfos und Auto-Erledigt**: nur mit vorhandener Sage-View (BA §6.1): ein FA mit Sage-Status verpackt/abgeholt | verschwindet aus FA-Liste, Leitstand und Worklist; BDE-Start gesperrt mit Meldung | TS-55, `Sync:FaZusatzinfoAutoErledigtEnabled` |
| C9 | **Reconciliation und Lagerbestand-Sync**: Testfall in Sage-Test (FA löschen bzw. Bestand ändern) | Storno bzw. Korrekturbuchung „Sage-Einbuchung/-Ausbuchung“ mit Notiz | TS-50, TS-53 |
| C10 | **Performance**: FA-Liste, Leitstand, Bestände, Bewegungshistorie, OSEON-Index — Ladezeit im Vergleich zu akenet01 | nicht spürbar langsamer | v1.36 (BOM-Weiche), v1.38 (Selektoren) |

---

## Bericht (nach Projektanweisung, Arbeitsweise ABNAHME)

- Chat: Tabelle Schritt | Ergebnis | Beobachtung | Screenshot ja/nein. Jeder FEHLER in 2–3 Sätzen.
- Datei: `secondbrain\tests\<JJJJ-MM-TT>-ake-regression-ideal-version-browser-testbericht\<…>.md` mit `bilder\`.
- Eine Zeile am Ende dieses Protokolls (eine Spec gibt es dafür nicht):
  `- Browser-Abnahme <Datum>: [[<Berichtsdatei>]] - n OK, n FEHLER, n nicht pruefbar, n Protokoll passt nicht`
- Besonders hervorheben: B0.2, B0.3, B3.3, B6-S3 und B8.1 — das sind die wahrscheinlichsten AKE-Regressionen.

## Offene Punkte für Gerald (vor Teil A)

1. Läuft auf akenet01 v1.30.1 oder v1.30.2 (Zahnrad-Hotfix aus dem Worktree, nicht in main)? `/Help/Changelog` zeigt am
   20.10. v1.30.1 als neuesten Eintrag.
2. Soll `vw_IDEAL_AKE_WMS_FAZusatzinformationen` auf der AKE-Sage-Testdatenbank angelegt werden? Ohne sie sind C8 und die
   BDE-Sperre nicht prüfbar.
3. B6-S3 (Werkbank-Prüfung im NurFA-Modus): **zurückgestellt**, BDE ist derzeit nicht im Einsatz (Gerald, 20.10.2026).
   Vor einer BDE-Einführung bei AKE klären: Auf akenet01 wurde an Stanzarei auf FAs mit Werkbank A1/A2 gebucht (#56,
   #58, #62); laut TS-96.10 c wäre das in der IDEAL-Version abgewiesen.
4. ~~Eigener Sage-Testmandant?~~ Nein (Gerald, 20.10.2026). Darum ist die Sage-Lagerbuchung am neuen System aus (A5
   Teil 3); B1-S5 und C3 laufen nur in einem abgestimmten Zeitfenster.
5. README-Updatetabelle des Bündels ohne die Skripte 81–94 und 96 (A3, Hinweis) — aus dem Prüfbericht, von mir nicht
   selbst nachgelesen.
