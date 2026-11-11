---
typ: notiz
status: Erledigt
worktree: .claude/worktrees/2026-08-07-ideal-teile-1-5
branch: feature/2026-08-07-ideal-teile-1-5
version: "1.96.0"
---
# Eigener Lauf: Vorwärts-Merge main → Bündel (Hotfixes v1.30.3/v1.30.4 + Zahnrad)

Entscheidung Gerald 2026-11-10: Die Bugfixes von main kommen in einem **eigenen Lauf** ins Bündel, nicht nebenbei.

## Inhalt von main (Stand `4296dcb0`, gepusht 2026-11-10)
- Zahnrad-Hotfix Lagerbestellung (Merge `e386bfce`).
- OSEON-Hotfix v1.30.3 [[2026-11-07-oseon-sync-neu-ausgeschriebener-auftrag-spec]].
- Kommissionierfotos v1.30.4 [[2026-11-08-kommissionierfotos-ohne-anmeldung-abrufbar-spec]] (Merge `df54dee8`).

## Befehl
```powershell
cd C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5
git merge --no-ff main -m "merge: main (v1.30.3 OSEON, Zahnrad, v1.30.4 Kommissionierfotos) -> Buendel"
```

## Erwartete Konflikte (Probe `git merge-tree` 2026-11-10, 11 Dateien)
- Versionen `IdealAkeWms/AppVersion.cs`, `IDEALAKEWMSService/AppVersion.cs` – Bündelversion behalten/erhöhen.
- `Views/Help/Changelog.cshtml` – beide Einträge behalten.
- `docs/TESTSZENARIEN.md` – Kapitel 124 (Fotos) hinter 123. Die Update-Skript-Spec
  [[2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec]] plant ebenfalls TS-124 → auf 125 umstellen.
- `Program.cs`, `.gitignore` – Foto-Ordner/`LoginRedirectMiddleware` in den Bündel-Startcode einsetzen.
- Kommissionierung (Zahnrad): `PickingController.cs`, `PickingListViewModel.cs`, `Views/Picking/Index.cshtml`,
  `Views/WarehousePicking/Details.cshtml`, `PickingControllerTests.cs` – fachlich lösen, nicht mechanisch.

## Danach
Build + Tests, qa-agent mit Sichtprüfung (Fotos TS-124, Zahnrad, OSEON) auf DEV AKE/IDEAL; Brain-Changelog und
Feature-Map nachziehen. Erst dann Bündel-Deploy (Update-Skript-Spec, offene Frage 5 erledigt sich damit).

## Push des Bündels (Klärung 2026-11-11, Gerald + Koordination)
- Vor dem Lauf geprüft: Worktree sauber, HEAD `a26be406` (v1.95.0).
- `origin/feature/2026-08-07-ideal-teile-1-5` (`6d81a4a3`, Push 05.10.) und das lokale Bündel sind auseinandergelaufen (307 Commits nur auf Remote,
  501 nur lokal). Ursache: Die Historie wurde am 05.10. zweimal per `fast-import` neu geschrieben (Reflog Zeilen 28–30). Gepusht wurde
  die erste Fassung, lokal ging es mit der zweiten weiter (`b877e05a`).
- Gegenprobe Gerald: `git diff --stat 6d81a4a3 b877e05a` ist leer. Remote hat also nichts Eigenes, der Inhalt ist gleich.
- Der Push des Bündels geht deshalb nur mit Force, und zwar **nach** diesem Lauf und **vor** dem Bündel-Merge nach main:
  `git push --force-with-lease=feature/2026-08-07-ideal-teile-1-5:6d81a4a3 origin feature/2026-08-07-ideal-teile-1-5`

## Sicherung vor dem Lauf (Vorgabe Gerald 2026-11-11: „es darf nichts verloren gehen“)
1. Lokale Sicherungszweige bzw. Tags: `backup/buendel-2026-11-11` auf `a26be406`, `backup/origin-buendel-2026-10-05` auf `6d81a4a3`,
   `backup/main-2026-11-11` auf `02500726`.
2. Alle drei normal pushen. Das sind neue Namen, also kein Force. Danach liegt die lokale Bündel-Arbeit (501 Commits) auf GitHub.
3. Zusätzlich eine Offline-Kopie: `git bundle create D:\Backup\IDEAL-AKE-WMS-2026-11-11.bundle --all`, dann `git bundle verify`.
4. Nach dem Merge prüfen: `a26be406` und `main` sind Vorfahren von HEAD. Außerhalb der Konfliktdateien kein Diff gegenüber
   `a26be406`, der nicht aus main kommt.
5. Die Sicherungen werden erst gelöscht, wenn das Bündel in main gemergt und deployt ist, und nur mit Freigabe von Gerald.
- 2026-11-11 07:52: In GitHub Desktop wurde versehentlich „Pull origin“ auf das Bündel gestartet (93 Konflikte gegen den alten Remote-Stand). Abgebrochen, danach geprüft: Worktree sauber, HEAD `a26be406`. **Für das Bündel bis zum Force-Push kein Pull und kein Fetch-and-Merge.**
- **Sicherung erledigt 2026-11-11 07:57 (Gerald):** `main` auf `1753c79e` gepusht. Auf origin liegen `backup/main-2026-11-11` (`1753c79e`), `backup/buendel-2026-11-11` (`a26be406`) und der Tag `backup/origin-buendel-2026-10-05` (`6d81a4a3`). Offline: `D:\Backup\IDEAL-AKE-WMS-2026-11-11.bundle` mit 29 Refs, vollständige Historie, `verify` okay. Der lokal schon vorhandene Tag `backup/ideal-teile-1-5-vor-filter-repo` (`6c253c60`) ist darin enthalten.

## QA-Nachweis v1.96.0 (2026-11-11)

qa-agent, unabhängig vom Merge-Autor. Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`,
HEAD `5492f0db` (Merge `02a0a173` + Testfix). Keine Spec-Statusänderung (laut Auftrag). Kein Commit, kein Push, kein Merge.

### 1. Build und Tests
- Vorfahren: `a26be406` (v1.95.0) und `1753c79e` (main) sind beide Vorfahren von HEAD (`git merge-base --is-ancestor`).
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (Bestand).
- `dotnet test`: IdealAkeWms.Tests 2592 bestanden / 1 übersprungen / 0 Fehler (2593); IDEALAKEWMSService.Tests 512 / 0 / 0 (512).
- Gezielt: `OseonOrderSelectionTests` 5/5; `PhotoController*`, `LoginRedirect*`, `TableToolbarHost*`, `PickingControllerTests.PrintBom_VisiblePositions_KeepClientOrder`, `…Index_WorkbenchColumn_FilledFromWorkplaceAndFilterable` 44/44.

### 2. Code-Review des Merge-Ergebnisses
Geprüft: `git diff a26be406 HEAD` (ohne Brain, 28 Dateien), die main-Seite `git diff f9e2a7a6 1753c79e` (Merge-Basis → main, 31 Dateien), `git diff 1753c79e HEAD` für die übernommenen Dateien.
- **Kein Verlust aus main:** `PhotoStorage.cs`, `LoginRedirectMiddleware.cs`, `PhotoController.cs`, `photo-upload.js`, `OseonSyncService.cs`, `OseonOrderSelection.cs`, `PhotoControllerTests.cs`, `LoginRedirectMiddlewareTests.cs`, `OseonOrderSelectionTests.cs`, `.claude/*`, `CLAUDE.md` sind byte-gleich mit main (kein Diff `1753c79e..HEAD`). `PrintBom.cshtml` hat den main-Rand `margin: 28mm 8mm 8mm 8mm`; `Bom.cshtml` hat `sortActive`; `PickingController.RenderPrintBomAsync` hat `visibleOrder` wie main; `Help/Index.cshtml` Fototext von main übernommen; `.gitignore` beide Seiten (`IdealAkeWms/App_Data/`).
- **Bewusst Bündelstand (fachlich gelöst, nicht verloren):** `Picking/Index.cshtml`, `PickingListViewModel.cs`, Filter-Key `workbench` (Bündel seit v1.88.0; Werkbank steht nach „Bezeichnung“, eine „Kunde“-Spalte gibt es nicht; TS-107.6 trägt den Bündel-Hinweis). `WarehousePicking/Details.cshtml` nutzt den Toolbar-Host `data-table-toolbar` (v1.65.0) statt des main-Hosts. `BomZahnradHostTests.cs` nicht übernommen, abgedeckt durch `TableToolbarHostTests` (WarehousePicking/Details enthalten).
- **Program.cs:** Bündel-Startcode erhalten. Geändert sind nur zwei Stellen: die Foto-Zeile auf `PhotoStorage.GetDirectory` + `MigrateLegacy`, und der Lambda-Login-Redirect, der durch `LoginRedirectMiddleware` ersetzt ist (Registrierung `AddTransient`, Reihenfolge nach `WindowsAutoLoginMiddleware`, vor `UseStaticFiles`; `UseRouting` steht davor). Keine doppelte Login-Logik übrig.
- **photo-upload.js / FA-Id im Auftragsmodus:** `/Picking/Auftrag/{id}` ruft `BomSeiteAsync(auftrag.ProductionOrder, …)` auf, das `ProductionOrderId = order.Id` setzt (`PickingController.cs` ca. Z. 416). `Bom.cshtml:861` gibt den Wert per `var productionOrderId = @Model.ProductionOrderId` (int) an `initPhotoUpload`. Upload und Löschen laufen also gegen die FA-Id, nicht gegen die Auftrags-Id. Auf DEV belegt (unten).
- **Middleware gegen Bündel-URLs:** Seit v1.30.4 kommen nur echte `wwwroot`-Dateien mit Punkt ohne Anmeldung durch. Im Bündel gibt es keine Routen mit Punkt (per `grep` über Http*/Route geprüft). In `wwwroot` liegen auf oberster Ebene nur `favicon.ico` und Ordner. Logo und Favicon der Anmeldeseite liefern anonym 200.
- **Druck-Sortierung im Bündel:** `table-filter.js` setzt `data-sort-dir` (Z. 262/637) wie main. `#bomTable` hat kein `data-fallback-sort-column`, der Client schickt die Liste also nur bei Sortierung oder Filter.
- Defektklassen (Script-Einbettung, `hidden`+`d-*`, stille Fallbacks, positionale Arrays, Validierung nur im Browser, doppelte Regel): im Merge-Diff keine neuen Fundstellen. `Bom.cshtml` gibt nur einen int ins Script.

**Befunde**
- Keine B-Befunde (nichts Blockierendes).
- **H-1 Testindex veraltet:** `secondbrain/tests/testszenarien-index.md`, Zeilen 107 und 124: Die Statusspalte sagt noch „nicht gemergt im Bündel“ bzw. „Vorwärts-Merge … durch den Menschen“. Der Hauptagent zieht sie mit Brain-Changelog und Feature-Map auf „im Bündel v1.96.0 übernommen“. Von mir nicht geändert, damit keine Schreibkollision entsteht.
- **H-2 TS-124.7 Kollisionsfall:** Liegt im Ziel schon eine Datei gleichen Namens, bleibt die Quelle in `wwwroot` liegen. Bei **jedem** Start kommen dann erneut die Warnung „… nicht verschoben“ und „verschoben: 0, uebersprungen: 1“. Das ist so gebaut (`PhotoStorage.MigrateLegacy`). Für Prod heißt das: Steht nach dem Deploy „uebersprungen > 0“ im Log, die Dateien von Hand abgleichen.
- **H-3 Restdatei unter `wwwroot`, anonym abgerufen:** Antwort `302 → /Account/AccessDenied` (0 Byte), nicht die Anmeldeseite. Die Middleware lässt die echte Datei durch, aber der Endpunkt `GetFile` mit `RequirePickingAccess` greift vor `UseStaticFiles`. Es wird nichts ausgeliefert, TS-124.7 ist erfüllt; nur der Wortlaut weicht ab.
- **H-4 „Auftrag drucken“** (`AuftragDrucken`, Link im Auftragsmodus) übernimmt keine Sortierung. Das ist ein eigenes Dokument mit Barcode und lag nicht im Umfang von v1.30.3. Der Knopf „Stückliste drucken“ oben übernimmt die Sortierung auch im Auftragsmodus.
- **H-5 TS-120.15 an IDEAL nicht anwendbar:** Dort ist `PickerAssignmentEnabled` aus, die Freigabe-Dialoge werden gar nicht gerendert. An AKE steht der Hinweis 3× im Markup (drei Dialoge, 0 aktive Kommissionierer).

### 3. Nachweisart
Automatisiert: Fotoregeln (Pfad, FA-Zugehörigkeit, Migration), Login-Middleware (Unit), OSEON-Auswahl, Druckreihenfolge (Server), Werkbank-Filter (Server), Toolbar-Host (Quelltextwächter). Manual-UAT, nicht als grün gezählt: echte Pipeline, IIS/App_Data, OSEON-MERGE (raw SQL), Druckvorschau 28 mm, Client-Sortierung → Druck. Was davon auf DEV ging, ist unten belegt.

### 4. Geänderte bestehende Tests
- `HardwareScannerTests.QrErstesFeld_…` (`5492f0db`): Die Rohstring-Erwartung wird mit `.Replace("\r\n", "\n")` normalisiert; die gelesene JS-Datei normalisiert derselbe Test schon. Ursache: Der Worktree ist seit 07:38 mit CRLF ausgecheckt (`core.autocrlf=true`, `file` zeigt CRLF). Das ist eine Korrektur der Testumgebung (Zeilenende), das Verhalten bleibt gleich. Kein Befund.
- `PickingControllerTests`: nur 2 Tests ergänzt (aus main portiert), keine bestehenden geändert.

### 5. Checkliste
Version 1.96.0 in beiden `AppVersion.cs`. Anwender-Changelog: Karte v1.96.0 oben, Karten v1.30.2–1.30.4 als Historie. `docs/TESTSZENARIEN.md`: Kapitel 107 vor TS-108, Kapitel 124 hinter TS-123, Endzeile ergänzt. Keine Migration, kein SQL, keine neue Einstellung (ADR 0020 entfällt). Brain-Changelog, Feature-Map und Index: offen beim Hauptagenten (H-1).

### 6. DEV-Deploy und Testlauf
Shell mit Administratorrechten (Administrator: True). Die App-Pools `AKE_WMS_DEV`/`IDEAL_WMS_DEV` laufen als `akenb\svc_idealakewms` (SpecificUser). `D:\Dev\<S>\Web` vererbt „Authentifizierte Benutzer“ das Recht Ändern (M).
- **Fotos vor dem Deploy:** Beide Sites hatten 0 Fotos in `wwwroot\Fotos\Kommissionierung`; an IDEAL fehlte der Ordner ganz. Für TS-124.7 habe ich Testfotos angelegt. AKE: 3 Dateien in `wwwroot` (2× FA 2614027, 1× „9999999“ als fremder FA), dazu absichtlich 1 gleichnamige Datei schon in `App_Data` (Kollision). IDEAL: 1 Datei `QATEST_…`.
- **AKE:** `dev-deploy.ps1 -Standort AKE -Quelle <Worktree>` → Exit 0, „HTTP 200, Fusszeile v1.96.0“, Absicherung gelaufen, Dienst Running. Log: „Kommissionierfotos verschoben: 2, uebersprungen: 1.“ plus Warnung zur Kollisionsdatei. Das passt zu 3 Dateien, davon 1 Kollision.
- **IDEAL:** gleicher Aufruf → Exit 0, v1.96.0, Dienst Running. Log: „Kommissionierfotos verschoben: 1, uebersprungen: 0.“ `App_Data` hat die App selbst angelegt (Owner `AKENB\svc_idealakewms`).
- **App_Data-Rechte:** Auf DEV war **kein icacls nötig**. Der Pool-Benutzer ist ein authentifizierter Benutzer und erbt Ändern; Anlegen, Verschieben und Hochladen funktionierten. Auf Test und Prod das Recht des dortigen Pool-Kontos auf `<Site>\App_Data` prüfen.

| Schritt | Ausführung | Ergebnis | Status |
|---|---|---|---|
| 124.1 ohne Anmeldung | anonym `GET /Fotos/Kommissionierung/<datei>.jpg` an 8081 + 8082, Browser ohne Session | 302 → `/Account/Login?returnUrl=…` | OK (AKE, IDEAL) |
| 124.2 Pfad mit Punkt | anonym `/Home/Index.x`, `/favicon.ico`, `/images/ideal-ake-logo.svg` | 302 Login; Favicon/Logo 200; Anmeldeseite mit Logo und CSS (Screenshot) | OK (AKE, IDEAL) |
| 124.3 mit Recht | Browser mit Windows-Anmeldung (admin), Foto-URL direkt; `/Picking/Auftrag/20` (KO-2026-00002 vorübergehend auf InArbeit, danach zurück auf 40) | Bild 200×120 ausgeliefert; Foto-Bereich zeigt die 2 Fotos von FA 2614027, das fremde „9999999“ nicht; Fuß v1.96.0. IDEAL: Abruf mit Session 200 image/jpeg | OK (AKE, IDEAL) |
| 124.4 ohne Recht | Formular-Login `lager` (ohne Passwort, nur stock/stock_keyuser), Foto-URL | 302 → `/Account/AccessDenied` | OK (AKE) |
| 124.5 Hochladen/Löschen | `POST /api/photos/upload` (productionOrderId=363, Token) mit Windows-Session, danach eigene Datei löschen | 200; Datei `2614027_20261111081615_3.jpg` liegt in `App_Data\…`, nicht in `wwwroot`; Löschen 200, Datei weg | OK (AKE); Löschen über die Kachel im Browser nicht geklickt (confirm-Dialog) → Mensch |
| 124.6 fremder FA | `POST /api/photos/delete` po=363 + `9999999_…jpg`; Pfad `x/2614027_a.jpg` | 400 / 400, Datei bleibt | OK (AKE) |
| 124.7 Migration | Logs wie oben; Recycle der App-Pools beider Sites | IDEAL: zweiter Start ohne Verschiebungszeile; AKE: nur die Kollisionswarnung erneut (H-2). Restdatei anonym → 302 AccessDenied, 0 Byte (H-3). Test-Restdatei danach entfernt | OK |
| Fehlende Datei / anderes Format | IDEAL `…nichtda_1.jpg` / `x.png` mit Session | 404 / 400 | OK |
| Zahnrad Lagerbestellung (TS-18.10/TS-98) | `/WarehousePicking/Details/53` AKE | Zahnrad und CSV rechts über der Tabelle sichtbar (Screenshot) | OK |
| Werkbank /Picking (TS-107.6) | `/Picking?status=alle&showAll=true` AKE | Spalte „Werkbank“ nach „Bezeichnung“, Wert D2 bei 3 Aufträgen; `colf_workbench=d2` → 3, `=xyz` → 0 | OK |
| Druck-Sortierung (TS-107.5) | `/Picking/Bom/363`, Standardfilter-Chip entfernt, „Bezeichnung 1“ ▲, „Stückliste drucken“ | Druckansicht in Reihenfolge 6, 16, 8, 5, 17, 12, 1 … = Ansicht (Screenshot) | OK |
| Druckrand 28 mm (TS-107.4) | Quelltext `PrintBom.cshtml` `@page margin: 28mm 8mm 8mm 8mm` | Druckvorschau nicht geöffnet (der Druckdialog des Browsers blockiert die Automatisierung) | NICHT MOEGLICH → Mensch |
| OSEON TS-107.1–107.3 | `Sync:OseonTrackingEnabled=false` auf beiden DEV-DBs (geprüft), nicht eingeschaltet | Unit-Tests 5/5 grün; MERGE nur am AKE-Test prüfbar | NICHT MOEGLICH → Mensch |
| Stichprobe Kommissionieraufträge | `/Picking` (Hinweis „keine zugewiesen“, Filter „aller Kommissionierer“), Auftrag öffnen, FA-Stückliste mit Auftragsliste, `/WarehousePicking`, `/Help/Changelog` an beiden Sites | alles 200, Inhalte wie erwartet | OK |
| TS-120.15 Leitstand-Hinweis | `/PickingLeitstand` AKE, 0 aktive Kommissionierer | Hinweis 3× im Markup (Freigeben, Sammel-Freigabe, Kommissionierer ändern); IDEAL ohne Dialoge (H-5) | OK (Markup), Sichtprüfung → Mensch |

Testdaten, die auf DEV bleiben: AKE `App_Data\Fotos\Kommissionierung` mit `2614027_20261111080000_1.jpg`, `2614027_20261111080001_2.jpg`, `9999999_20261111080000_1.jpg`; IDEAL `QATEST_20261111080000_1.jpg`. `PickingOrders.Id=20` steht wieder auf Status 40.

Screenshots: `C:\Users\GERALD~1.WEI\AppData\Local\Temp\claude-chrome-screenshots-cF2Dyv\screenshot-1794381338655-49.jpg` (Foto-Bereich Auftrag), `…-1794381445711-50.jpg` (/Picking mit Werkbank), `…-1794381523970-51.jpg` (Lagerbestellung #53 mit Zahnrad), `…-1794381572268-52.jpg` (Stückliste sortiert), `…-1794381581259-53.jpg` (Druckansicht sortiert).

### 7. Publish
`C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` → `IDEALAKEWMSWeb` (65 Einträge) und `IDEALAKEWMSWebService` (72 Einträge). Beide Ordner vorher geleert, beide Publish-Läufe mit Exit 0. `publish-info.txt`: Version 1.96.0, Commit `5492f0db8715ebb409eac5ff634b4493ca65a213`, Branch `feature/2026-08-07-ideal-teile-1-5`, 2026-11-11 08:22. `publish/` ist per `.gitignore` ignoriert, der Worktree ist sauber.

### Deploy-Hinweis
Web **und** Dienst aus diesem Publish. Keine Migration, kein SQL. Reihenfolge:
1. Dienst stoppen.
2. Web: App-Pool stoppen, kopieren, starten.
3. **Log prüfen:** „Kommissionierfotos verschoben: n, uebersprungen: m“. Bei m > 0 die Dateien von Hand abgleichen (H-2).
4. Schreibrecht des Pool-Kontos auf `App_Data` prüfen.
5. Dienst starten.

Nach dem Merge nur dann neu aus main publishen, wenn der Merge geprüfte Dateien mit parallelen main-Änderungen zusammenführt.

### Offene manuelle Prüfungen (Mensch)
1. TS-107.4 Druckvorschau A4: oberer Rand ca. 28 mm, Folgeseiten ebenso.
2. TS-107.5 im Browser zusätzlich: absteigend ▼ sowie Filter plus Sortierung; ohne Sortierung und Filter Druck nach Position.
3. TS-107.1–107.3 OSEON am AKE-**Test** (Sync-Schalter dort an), inkl. 107.2b.
4. TS-124.5: Löschen über die Foto-Kachel im Browser (confirm-Dialog) und Hochladen per Kamera am Terminal.
5. TS-120.15: Dialoge an AKE sichtbar prüfen (gelber Hinweis, „Freigeben“ gesperrt).
6. Lauf auf dem Testsystem nach dem Bündel-Deploy: TS-124.1/124.3, Log der Fotoverschiebung an AKE und IDEAL, App_Data-Recht.
7. Hauptagent: H-1 (Testindex 107/124), Brain-Changelog v1.96.0, Feature-Map.

## Ergebnis (2026-11-11, Hauptagent)

- Vorprüfung grün: Worktree sauber, HEAD `a26be406`, main `1753c79e`, `backup/buendel-2026-11-11` = `a26be406` lokal und auf origin.
- Merge-Commit `02a0a173` (Eltern `a26be406` + `1753c79e`), danach Testfix `5492f0db`. Kein Push, kein Merge nach main.
- Konflikte (11 Dateien), Entscheidungen:
  - `AppVersion.cs` Web + Dienst: Bündel 1.95.0 / main 1.30.4 → **1.96.0**.
  - `Views/Help/Changelog.cshtml`: Bündel v1.95.0… / main v1.30.4+v1.30.3 (+v1.30.2) → neue Karte v1.96.0 „Übernahme Korrekturen aus AKE
    v1.30.2–1.30.4“ oben, die drei v1.30.x-Karten als Historie vor v1.30.1.
  - `docs/TESTSZENARIEN.md`: Bündel Kapitel 57–123 / main Kapitel 107 + 124 → 107 vor TS-108 (mit Bündel-Hinweis zu TS-107.6), 124
    hinter TS-123; Zusatz TS-18.10 Bündel-Text („v1.47.5 (auf main v1.30.2)“, Verweis `TableToolbarHostTests`).
  - `Program.cs`: Bündel-Startcode (Einstellungsprüfung, Hierarchie-Status, PDF) / main Foto-Ordner → Bündel behalten, Foto-Zeile auf
    `PhotoStorage.GetDirectory`; Middleware und `MigrateLegacy` kamen konfliktfrei.
  - `.gitignore`: Bündel Publish-Artefakte / main `IdealAkeWms/App_Data/` → beide.
  - `PickingController.cs`, `PickingListViewModel.cs`, `Views/Picking/Index.cshtml`: Bündel Kommissionieraufträge v1.88 (Werkbank schon
    drin) / main Werkbank in der alten FA-Liste → **Bündel**. Druck-Sortierung `visibleOrder` (v1.30.3) kam konfliktfrei und bleibt.
  - `Views/WarehousePicking/Details.cshtml`: Bündel Toolbar-Host `data-table-toolbar` (v1.65) / main leerer Host + Kommentar → **Bündel**.
  - `PickingControllerTests.cs`: Bündel-Setup behalten; main-Tests Werkbank-Filter und Druckreihenfolge auf das Bündel-Setup portiert.
  - Nicht übernommen: `IdealAkeWms.Tests/Views/BomZahnradHostTests.cs` (main) – im Bündel seit v1.65.0 durch `TableToolbarHostTests`
    ersetzt (deckt Bom + WarehousePicking/Details), der Host-String passt nicht mehr.
  - OSEON (`OseonSyncService`, `OseonOrderSelection`): im Bündel seit der Basis unverändert → 1:1 von main. Relevanzfilter v1.92.0 liegt
    nur im Web (Repository/View), keine Überschneidung.
- Verlustprüfung: `a26be406` und `main` sind Vorfahren von HEAD. Je Datei der main-Änderungen seit Basis `f9e2a7a6` (31 Dateien): 16 = main
  byte-gleich, 11 zusammengeführt, 4 = Bündel (die drei Werkbank/Zahnrad-Dateien + der ersetzte Test, oben begründet).
  `git diff a26be406 HEAD` (ohne Brain) berührt nur diese 27 Dateien – nichts aus dem Bündel ging verloren.
- Testfix `5492f0db`: `HardwareScannerTests` Rohstring auf LF normalisiert. Ursache nicht der Merge: der Worktree wurde 2026-11-11 07:38
  komplett als CRLF neu ausgecheckt (passt zum abgebrochenen „Pull origin“), Inhalt laut Git unverändert.
- Build 0 Fehler, Tests Web 2592/2593 (1 übersprungen), Dienst 512/512. QA siehe Abschnitt oben: grün, H-1 erledigt (Testindex).
- AKE-DEV: die 5 Sync-Schalter (Articles, ArtikelLieferant, Kreditoren, Debitoren, EinkaufBestellungen) nach dem DEV-Deploy wieder `true`.

## Deploy (Bündel, Ergänzung v1.96.0)

Gilt für den Bündel-Deploy auf allen Systemen (Manifest-Eintrag in [[2026-11-09-update-skript-je-zielsystem-mit-nachlauf-spec]], Nachtrag
2026-11-11). Vor dem Start der neuen Web-Version je Site:
1. `mkdir "<SiteRoot>\App_Data\Fotos\Kommissionierung"`; `icacls "<SiteRoot>\App_Data" /grant "IIS AppPool\<PoolName>:(OI)(CI)M"` (bzw. das
   Pool-Konto; auf DEV liefen die Pools als Domänenkonto und erbten Ändern – nichts nötig).
2. Fotos in `<SiteRoot>\wwwroot\Fotos\Kommissionierung` zählen.
3. Nach dem Start Log „Kommissionierfotos verschoben: n, uebersprungen: m“: n + m = Zählung. m > 0 → Rest-Dateien von Hand abgleichen (die
   Warnung kommt sonst bei jedem Start).
Publish: `<Worktree>\publish\` (Web + Dienst), `publish-info.txt` Version 1.96.0, Commit `5492f0db`.
Push des Bündels (Gerald, nach QA, vor dem Merge nach main):
`git push --force-with-lease=feature/2026-08-07-ideal-teile-1-5:6d81a4a3 origin feature/2026-08-07-ideal-teile-1-5`
