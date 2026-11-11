---
type: spec
title: Kein Seiten-Querscrollen durch die Navigationsleiste; Kontrast Kartenlink „Fehlteile (wird nachgeliefert)“
slug: 2026-11-08-seiten-querscrollen-navigationsleiste-spec
status: Testbereit
created: 2026-11-08
updated: 2026-11-08
source_backlog: "[[2026-11-08-seiten-querscrollen-navigationsleiste]]"
bug: "[[2026-11-08-seiten-querscrollen-navigationsleiste-bug]]"
task: "[[2026-11-08-buendel-2026-11-08-goal-lauf]]"
worktree: .claude/worktrees/2026-08-07-ideal-teile-1-5
branch: feature/2026-08-07-ideal-teile-1-5
version: "1.94.0"
sql_nummern: keine
testszenario: "TS-123"
standort: beide
affected_code:
  - IdealAkeWms/Views/Shared/_Layout.cshtml (Z. 47 ul.navbar-nav, Z. 371 span.user-info)
  - IdealAkeWms/Views/WarehouseRequisitions/Index.cshtml (Z. 50 Kartenlink)
  - IdealAkeWms/Views/Help/Changelog.cshtml
  - IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs
  - docs/TESTSZENARIEN.md (TS-123)
  - "[[2026-11-08-seiten-querscrollen-navigationsleiste-bug]]"
open_questions: []
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold (Auftrag Nachbesserung)"
freigabe_am: 2026-11-08
---

> [!info] Ausnahme Schranke 1
> Gerald hat am 2026-11-08 ausdrücklich angeordnet, die Spec direkt freigegeben anzulegen. Es gibt keine Grundsatzfragen, deshalb ist die Spec nicht auf Entwurf zurückgestuft.

## Ziel / Nutzen (das Warum)

Im Fensterbereich von 992 px bis etwa 1600 px darf die Seite nicht mehr waagrecht scrollen, und der Benutzername darf nicht abgeschnitten werden. Das stellt die Regel aus v1.64.1 wieder her: genau ein Scrollbalken, keine Seiten-Querscrollleiste (siehe [[2026-10-19-tabelle-seitenbreite-doppelter-scrollbalken-bug]]). Dazu bekommt der Fehlteile-Kartenlink einen lesbaren Kontrast. Die Terminals in der Fertigung stehen teils bei schlechtem Licht.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. Die Navigationsleiste bricht im ausgeklappten Zustand (≥ 992 px) in eine zweite Zeile um, statt über den Rand zu laufen. Der Benutzer-Knopf bleibt ganz sichtbar.
2. Der Kartenlink „N Fehlteile (wird nachgeliefert)“ auf `/WarehouseRequisitions` erreicht mindestens 4,5:1.
3. Version 1.94.0, Anwender-Changelog, TS-123.

**Out-of-Scope (Zusatzfunde, nicht mitbauen)**
- Weitere `text-warning`-Texte auf hellem Grund: `Views/FaCompletion/Edit.cshtml:96`, `Views/Picking/Bom.cshtml:632` und `Views/WarehousePicking/Details.cshtml:131`. Kontrast nicht gemessen.
- Der Nachbarlink „Wird nicht nachgeliefert“ (`Index.cshtml:61`, `text-danger` #dc3545 auf Weiß). Er liegt mit etwa 4,5:1 genau an der Grenze und bleibt unverändert.
- Menü umbauen oder Menüpunkte zusammenlegen.

## Fachliche Anforderungen

- **Wovon die Breite abhängt (Befund, belegt):** Wie viele Menüpunkte eine Person sieht, hängt an ihren Rollen und an AppSettings-Schaltern, die je Standort verschieden stehen (`_Layout.cshtml:49-96`).
  - Eher IDEAL: FA-Struktur, „FA-Struktur (Sage live)“ (`SageSdataFaAnsichtAktiv`), Beschichtung (`FaHierarchyBeschichtungAktiv` + Rolle), Vormontage (`FaHierarchyVormontageAktiv`), Kommissionierlisten (`FaHierarchyKommissionierlistenAktiv`).
  - Eher AKE: Teileverfolgung (`TeileverfolgungAktiv`), BDE (`BdeAktiv`), Reporting (Rolle), Leitstand (`LeitstandAktiv`).
  - Länger wird die Zeile außerdem durch einen langen Benutzernamen (`_Layout.cshtml:377`).
- **Nicht nur IDEAL:** Der Fehler ist auf beiden Standorten gemessen.
  - IDEAL-DEV bei 1520 px: QA A1 in [[2026-11-08-bereinigungen-nach-buendel-2026-11-07-spec]] Z. 606, QA A2 in [[2026-11-08-artikeldetails-lieferanten-anzeigen-spec]] Z. 428.
  - IDEAL bei 1384 px: `#navbarNav` reicht bis x = 1600 (Browser-Testbericht 2026-10-21, Z1).
  - AKE-DEV bei 1520 px: QA B1 N-3 in [[2026-11-08-wareneingang-aus-sage-bestellungen-spec]] Z. 761.
  - Wo das Problem auftritt, entscheiden also Rolle und Schalter, nicht der Standort. Die Lösung darf deshalb keine Standort-Weiche haben.
- **Unverifiziert:** Welche Schalter auf DEV und produktiv je Standort an sind, ist nicht geprüft. Für die Sichtprüfung vorher je Standort-DB ausführen:
  ```sql
  SELECT [Key], [Value] FROM dbo.AppSettings
  WHERE [Key] IN ('SageSdataFaAnsichtAktiv','FaHierarchyBeschichtungAktiv','FaHierarchyVormontageAktiv',
                  'FaHierarchyKommissionierlistenAktiv','TeileverfolgungAktiv','BdeAktiv','LeitstandAktiv',
                  'FaCompletionAktiv','BestellungenAktiv','LagerbestellungAktiv');
  ```

## Technischer Loesungsentwurf

**Navbar (kleinste Lösung im Bootstrap-5.3.3-Bestand):**
- In `_Layout.cshtml:47` an `ul.navbar-nav` die Utility `flex-wrap` anhängen: `class="navbar-nav me-auto flex-wrap"`. Im ausgeklappten Zustand ist die Liste eine Flex-Zeile und darf dann umbrechen. Im Hamburger-Zustand (< 992 px) ist sie eine Spalte, dort ändert sich nichts.
- In `_Layout.cshtml:371` an `span.user-info` die Utilities `flex-shrink-0 text-nowrap` anhängen. Der Benutzer-Knopf schrumpft dann nicht und bricht nicht um. Die vorhandenen Klassen `d-none d-lg-inline` bleiben.
- Keine Änderung an `site.css`. Die Breakpoint-Regel `@media (max-width: 991.98px)` (`site.css:1042`) passt weiter zu `-lg`.

**Verworfen: `navbar-expand-xxl` (Vorschlag aus dem Auftrag).** Gründe:
1. Der Überlauf reicht bis etwa 1600 px (Z1-Messung). `-xxl` klappt nur unter 1400 px ein, also bliebe der Fehler bei 1400–1600 px, genau bei den gemeldeten 1520 px.
2. Der Wechsel müsste drei Stellen synchron halten: `_Layout.cshtml:35`, `d-lg-inline` / `d-lg-none` in Z. 371/401 und `site.css:1042`.
3. Desktop-Arbeitsplätze mit 1280/1366 px bekämen einen Hamburger und damit einen Klick mehr je Navigation.

`flex-wrap` deckt jede Breite und jede Menge an Menüpunkten ab.

**Kartenlink:** In `Views/WarehouseRequisitions/Index.cshtml:50` `text-warning` durch `text-warning-emphasis` ersetzen. Das ist #664d03 (`bootstrap.css:52`, v5.3.3), auf Weiß etwa 8:1, und dasselbe Muster wie die Q-8-Korrektur `text-danger-emphasis` in B1. Die Bedeutung hängt nicht an der Farbe allein: Der Text sagt „Fehlteile (wird nachgeliefert)“, der Kartentitel trägt „⚠ Meine Fehlteile“, und der Pfeil „→“ zeigt den Link an. Das `<small class="text-muted">` im Link (#6c757d, etwa 4,7:1) bleibt.

**frontend-design:** Der Skill ist beim Dev-Lauf Pflicht. Konsistenz geht vor Eigenständigkeit: nur Bootstrap-Utilities, kein neues CSS, keine Neugestaltung des Menüs.

## Migrations-/SQL-Auswirkungen

Keine. Keine Migration, kein `SQL/`-Skript, FreshInstall und `SQL/AgentJobs/` unberührt.

## Einstellungen

keine

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine. Es werden nur Views geändert, keine Entität wird geschrieben.

## Akzeptanzkriterien

1. Bei 1280, 1366, 1520 und 1920 px ist `document.documentElement.scrollWidth <= window.innerWidth`, als Admin auf AKE-DEV und auf IDEAL-DEV, jeweils auf `/`, `/ServiceSettings`, `/StorageLocations` und der Artikelinfo.
2. Bei diesen Breiten ist der Benutzer-Knopf rechts vollständig sichtbar (voller Name, kein Abschneiden, kein Umbruch innerhalb des Namens) und öffnet „Mein Profil“ und „Abmelden“.
3. Reichen die Menüpunkte nicht in eine Zeile, brechen sie in eine zweite Zeile um. Jedes Dropdown öffnet vollständig und ist bedienbar, auch die in der zweiten Zeile.
4. Unter 992 px bleibt das Hamburger-Menü wie bisher, mit Benutzerblock unten im aufgeklappten Menü (`navbar-user-mobile`).
5. Tabellen behalten das v1.64.1-Verhalten: genau ein Scrollbalken, und zwar am Tabellen-Wrapper bzw. als Sticky-Leiste.
6. Der Kartenlink „N Fehlteile (wird nachgeliefert)“ misst auf Weiß ≥ 4,5:1 (DevTools-Kontrastanzeige). Text und ⚠ bleiben, die Bedeutung ist auch ohne Farbe erkennbar.
7. **Sichtprüfung am Bildschirm (Pflicht):** Screenshots bei 1280, 1520 und 1920 px, je mit AKE-Menü (AKE-DEV) und IDEAL-Menü (IDEAL-DEV), als Admin. Ablage im QA-Nachweis der Spec. Ohne diese sechs Screenshots ist die Spec nicht Testbereit.
8. Build grün, alle Tests grün. Version 1.94.0 in beiden `AppVersion.cs`, Eintrag in `Views/Help/Changelog.cshtml`.

## Test-Szenarien

Neues Kapitel **TS-123 „Menüleiste ohne Seiten-Querscrollen, Kontrast Fehlteile-Link“** in `docs/TESTSZENARIEN.md`, danach `secondbrain/tests/testszenarien-index.md`.

- **Vorbedingungen:** Admin-Konto auf AKE-DEV und IDEAL-DEV. Die SQL-Abfrage aus „Fachliche Anforderungen“ ist ausgeführt und die Schalter sind notiert. Für 123.6 gibt es mindestens ein eigenes Fehlteil im Status „wird nachgeliefert“.
- **123.1** Admin, AKE-DEV, 1920 px: Menü einzeilig, keine Seiten-Querscrollleiste, Name vollständig.
- **123.2** Admin, AKE-DEV, 1520/1366/1280 px: ggf. zweizeilig, keine Seiten-Querscrollleiste, Name vollständig. Mit DevTools `scrollWidth <= innerWidth` prüfen.
- **123.3** Wie 123.1/123.2 auf IDEAL-DEV (mehr Menüpunkte).
- **123.4** Je ein Dropdown aus der zweiten Zeile (z. B. „Stammdaten“) und der Benutzer-Knopf öffnen. Die Menüs sind nicht abgeschnitten und nicht verdeckt.
- **123.5** Auf `/StorageLocations` mit breiter Tabelle bei 1520 px: genau ein waagrechter Balken, und zwar an der Tabelle (Regression TS-95.17–95.20).
- **123.6** `/WarehouseRequisitions` mit eigenen Fehlteilen: Der Link „N Fehlteile (wird nachgeliefert)“ ist dunkel-ocker, Kontrast ≥ 4,5:1, und ein Klick öffnet `/MissingParts` mit Reiter „wird nachgeliefert“.
- **Negativfälle:**
  - **123.7** Fenster 960 px: Hamburger-Menü, kein Desktop-Benutzerknopf. Nach dem Aufklappen sind die Menüpunkte untereinander und der Benutzerblock unten. Kein Umbruch-Effekt sichtbar.
  - **123.8** Benutzer mit nur einer Rolle (z. B. nur Lager): Menü einzeilig, die Leiste ist nicht höher als vorher.
  - **123.9** Benutzer ohne Fehlteile: Die Karte „Meine Fehlteile“ erscheint nicht (`Index.cshtml:40`).
  - **123.10** Graustufen-Ansicht (DevTools „Emulate vision deficiencies: achromatopsia“): Die Fehlteile-Zeilen sind über den Text unterscheidbar.

## Etappen (nur bei epic: true)

Entfällt (epic: false).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

Provisorisch. Der QA-Lauf bestätigt die Angaben gegen den echten Diff.

- **Web-App:** ja (Views)
- **Service:** nein (nur der Versions-Bump in `AppVersion.cs`, kein Funktionsunterschied)
- **Migration:** nein
- **Publish-Befehle (im Worktree):**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Blocker (Grundsatzfragen)

Keine.

## Offene Rueckfragen

Keine. Die Wahl zwischen `flex-wrap` und `navbar-expand-xxl` ist im Lösungsentwurf mit Messbeleg entschieden. Die AppSettings-Abfrage ist ein Prüfschritt vor der Sichtprüfung, keine Entscheidung.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

Keine offenen Rückfragen. Freigabe per Auftrag (Gerald Weichbold, 2026-11-08).

## Umsetzung (Dev 2026-11-08, v1.94.0)

Commit `2efdb023`. `_Layout.cshtml`: `ul.navbar-nav me-auto flex-wrap`, `span.user-info … flex-shrink-0 text-nowrap`;
`WarehouseRequisitions/Index.cshtml`: `text-warning` → `text-warning-emphasis`. Wächter `LayoutQuerscrollenV194Tests`. TS-123. Sichtprüfung
(Screenshots 1280/1520/1920 px, AKE und IDEAL) durch die QA. frontend-design-Skill aufgerufen; Bootstrap-Bordmittel, kein neues CSS.

## QA v1.94.0 (2026-11-08) – Ergebnis: Testbereit

qa-agent. Prüfbereich: Commit `2efdb023`.

### 1. Build, Tests, Deploy
- Build 0 Fehler; Tests Web 2542/1/0, Dienst 507/0/0.
- AKE-DEV: Fußzeile v1.94.0. IDEAL-DEV: in diesem Lauf `dev-deploy.ps1 -Standort IDEAL -Quelle <worktree>`, Exit 0, „HTTP 200, Fusszeile v1.94.0“.
- `deploy.service` ist auf `true` korrigiert: Der Commit ändert `IDEALAKEWMSService/AppVersion.cs` (nur Version).

### 2. Code-Review
Drei Klassen-Änderungen:
- `_Layout.cshtml`: `flex-wrap` an `ul.navbar-nav`; `flex-shrink-0 text-nowrap` an `span.user-info`.
- `WarehouseRequisitions/Index.cshtml`: `text-warning` → `text-warning-emphasis`.

Kein `hidden` neben `d-*`, kein neues CSS. Der Fehlteile-Link trägt seine Bedeutung über den Text. **Befunde: keine.**

### 3. Beweisart
- View-Wächter `LayoutQuerscrollenV194Tests` (neu): automatisch.
- Layout und Kontrast: Sichtprüfung mit Messung. Headless Chrome mit Windows-Anmeldung, `Emulation.setDeviceMetricsOverride`; gemessen wurden `documentElement.scrollWidth`, Zeilen der Menüleiste, Rechteck des Benutzerknopfs und Kontrast nach WCAG aus `getComputedStyle`.

### 4. Geänderte bestehende Tests
keine.

### 5. Checkliste
Keine Migration, keine Einstellung. Version 1.94.0 in beiden `AppVersion.cs`, Anwender- und Brain-Changelog (`2026-11-08-v1-94-0-menueleiste-umbruch-kontrast`) vorhanden. TS-123 in `docs/TESTSZENARIEN.md` und im Index.

### 6. DEV-Testlauf (Admin Gerald Weichbold)
| Schritt | AKE (8081) | IDEAL (8082) | |
|---|---|---|---|
| 123.1 1920 px | 1 Menüzeile, Höhe 62 px; scrollWidth 1905 ≤ innerWidth 1920; Name „Gerald Weichbold“ vollständig (scrollWidth 125 = clientWidth 125) | gleich | OK |
| 123.2 1520 px | 2 Menüzeilen, 96 px; 1505 ≤ 1520; Name vollständig | gleich (IDEAL mit FA-Struktur (Sage live), Beschichtung, Vormontage) | OK |
| 123.2 1280 px | 2 Zeilen, 96 px; 1265 ≤ 1280; Benutzerknopf rechts bei x 1128–1253 | gleich | OK |
| 123.4 Dropdowns bei 1280 px | „Stammdaten“ (zweite Zeile) öffnet x 320–546, y 90–782 (innerhalb 1280×900); Benutzermenü x 1093–1253 | „Stammdaten“ x 647–844, y 90–718; Benutzermenü gleich | OK |
| 123.5 `/StorageLocations` 1520 px | Seite 1505 ≤ 1520; die Tabelle scrollt selbst (`.table-responsive` 2340 > 1457, overflow-x auto) | – | OK |
| 123.6 `/WarehouseRequisitions` | „14 Fehlteile (wird nachgeliefert) aus 10 Bestellungen →“: #664d03 auf Weiß = **7,99:1**, fett 600, 16 px; Seite 1505 ≤ 1520 | – | OK |
| 123.7 960 px | Hamburger sichtbar (`display:block`), Desktop-Benutzerknopf `display:none`; 945 ≤ 960 | – | OK |
| 123.6 Klick auf den Link → Reiter „wird nachgeliefert“ | nicht geklickt (Ziel-URL im Code: `MissingParts/Index?tab=WillBeRestocked&mineOnly=true`) | – | NICHT MOEGLICH im Lauf, für den Menschen |
| 123.8 Benutzer mit nur einer Rolle, 123.10 Graustufen | kein zweites Konto, keine DevTools-Emulation | – | NICHT MOEGLICH, für den Menschen |

Screenshots (temporär, Scratchpad `C:/Users/GERALD~1.WEI/AppData/Local/Temp/claude/8eae370df6a92eae/8d5dd4d6-9b10-461a-954c-3abd2e65c8bf/scratchpad/nav/`):
- `ake-home-1280.png`, `ake-home-1520.png`, `ake-home-1920.png`
- `ideal-home-1280.png`, `ideal-home-1520.png`, `ideal-home-1920.png`
- `ake-warehousereq-1520.png`, `ake-storagelocations-1520.png`, `ake-960-960.png`
- `ake-dropdown-1280.png`, `ideal-dropdown-1280.png`

### Deploy (aus dem echten Diff)
web: ja · service: ja (nur Version) · migration: nein.
```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
```
Nach dem Merge nur dann aus main neu publizieren, wenn der Merge getestete Dateien mit parallelen main-Änderungen kombiniert hat.

### Manuelle Checkliste (offen)
1. Testsystem AKE und IDEAL bei 1366 px und am Fertigungsterminal: keine Seiten-Querscrollleiste, Name vollständig.
2. 123.6: Link anklicken → `/MissingParts`, Reiter „wird nachgeliefert“.
3. 123.8 mit einem Benutzer, der nur eine Rolle hat.
4. 123.10 Graustufen-Emulation.
