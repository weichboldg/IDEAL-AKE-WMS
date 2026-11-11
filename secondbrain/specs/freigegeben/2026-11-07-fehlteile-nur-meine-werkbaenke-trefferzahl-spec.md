---
type: spec
title: Fehlteile „Nur meine Werkbaenke“ in SQL filtern (Trefferzahl, Seiten, Tab-Badges)
slug: 2026-11-07-fehlteile-nur-meine-werkbaenke-trefferzahl-spec
status: Testbereit
created: 2026-11-07
updated: 2026-11-07
source_backlog: "[[2026-10-28-fehlteile-nur-meine-werkbaenke-trefferzahl]]"
bug: "[[2026-11-07-fehlteile-nur-meine-werkbaenke-trefferzahl-bug]]"
buendel: "[[2026-11-07-buendel-uebersicht]]"
task: "[[2026-11-07-buendel-2026-11-07-goal-lauf]]"
worktree: "C:\\Git\\IDEAL-AKE-WMS\\.claude\\worktrees\\2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
version: "1.75.0"
standort: beide
affected_code:
  - "[[2026-11-07-fehlteile-nur-meine-werkbaenke-trefferzahl-bug]]"
  - "IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs:65-71 (Signatur GetMissingPartsAsync)"
  - "IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs:389-447 (Werkbank-Filter :406-407)"
  - "IdealAkeWms/Controllers/MissingPartsController.cs:52-63, 96-129"
  - "IdealAkeWms/Controllers/MissingPartsLagerController.cs:44-45, 70-73 (nur Signatur-Anpassung)"
  - "IdealAkeWms.Tests/Controllers/MissingPartsControllerTests.cs"
  - "IdealAkeWms.Tests/Controllers/MissingPartsLagerControllerTests.cs"
  - "IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs:472"
  - "docs/TESTSZENARIEN.md (Kap. 33.12 ff. + neues TS)"
open_questions:
  - "Tab-Badges im Modus 'Nur meine Werkbaenke' zaehlen kuenftig nur die eigenen Werkbaenke (bisher alle Werkbaenke bei mehreren Werkbaenken bzw. fremder workplaceId). Einverstanden? Empfehlung: ja - Badge und Liste muessen dieselbe Menge zeigen."
  - "Zusatzfund: Karte 'Meine Fehlteile' auf /WarehouseRequisitions zaehlt Lager+Glas zusammen (GetShortageCountsForUserAsync ohne Typ), der Link landet aber in einer typ-gefilterten Liste. Eigener Backlog-Eintrag? Empfehlung: ja, nicht in dieser Spec."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-07"
---

## Ziel / Nutzen (das Warum)

Werker mit mehreren Werkbaenken sehen in „Meine Fehlteile“ heute nur die eigenen Fehlteile, die zufaellig auf der
gerade geladenen Seite liegen; Trefferzahl, Seitennavigation und Tab-Badges stimmen nicht. Fehlteile werden so
uebersehen. Root Cause und Reproduktion: [[2026-11-07-fehlteile-nur-meine-werkbaenke-trefferzahl-bug]].
Ausgelagert aus [[2026-10-16-aktivitaetsprotokoll-filter-und-namen-spec]] (Rueckfrage/Antwort 4).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- `GetMissingPartsAsync` nimmt eine Werkbank-**Liste** statt einer Werkbank; Filter in SQL vor `CountAsync`/`Skip`/`Take`.
- `MissingPartsController.Index`: Werkbank-Menge einmal bestimmen, an Liste **und** beide Badge-Aufrufe geben;
  In-Speicher-Nachfilter (`:104-117`) und Sentinel `-1` entfallen.
- `MissingPartsLagerController`: nur auf die neue Signatur umstellen, Verhalten unveraendert.
- Tests, `docs/TESTSZENARIEN.md`, Version v1.75.0, Anwender-Changelog.

**Out-of-Scope (Zusatzfunde, nur gemeldet)**
- Karte „Meine Fehlteile“ auf `/WarehouseRequisitions` (`WarehouseRequisitionsController.cs:91-92`,
  `WarehouseRequisitionRepository.cs:467-491`) zaehlt beide Typen, Liste ist typgefiltert → Rueckfrage 2.
- Werkbank-Dropdown ist im Modus „Nur meine Werkbaenke“ deaktiviert (`Views/MissingParts/Index.cshtml:70`), der
  Werker kann also nicht auf eine seiner Werkbaenke eingrenzen; zudem listet das Dropdown alle Werkbaenke
  (`MissingPartsController.cs:134`). Kein Fehler im Sinne des Backlogs, nicht anfassen.

**Pruefung „gleiches Muster anderswo“** (alle Aufrufer der Logik, Worktree-Stand v1.73.0):
- `GetMissingPartsAsync` wird nur von `MissingPartsController.cs:96,120,125` und `MissingPartsLagerController.cs:44,70,72`
  aufgerufen (Grep `*.cs`, sonst nur Tests; kein Caching-Decorator fuer das Repository).
- `mineOnly` / „Nur meine Werkbaenke“ gibt es nur in MissingParts (Grep `mineOnly|Nur meine` in `IdealAkeWms/`;
  Treffer in `WarehouseRequisitions/Index.cshtml:49,60` sind nur Links auf MissingParts).
- `GetShortageCountsForUserAsync` (`WarehouseRequisitionRepository.cs:471-480`) filtert bereits in SQL per
  `userWorkplaceIds.Contains(...)` — korrekt, dient als Vorlage.
- `WarehouseRequisitionsController.Index` (`:68-89`) filtert vor dem Paginieren (im Speicher, aber ueber alle Zeilen) — kein Befund.
- Gleiches Fehlerbild im Aktivitaetsprotokoll (`SyncLogController`) wurde in v1.70.0 behoben (andere Spec).

## Fachliche Anforderungen

1. „Nur meine Werkbaenke“ an, ohne Werkbank-Auswahl: Liste, Trefferzahl, Seiten und **beide** Tab-Badges zeigen
   genau die Fehlteile aller dem User zugeordneten Werkbaenke (`ProductionWorkplaceUsers`).
2. „Nur meine Werkbaenke“ an, `workplaceId` ist eine eigene Werkbank: nur diese Werkbank (wie bisher).
3. „Nur meine Werkbaenke“ an, `workplaceId` ist eine fremde Werkbank (nur per URL moeglich): Liste leer,
   Trefferzahl 0, Badges 0 (bisher Badges = alle Werkbaenke) → Rueckfrage 1.
4. „Nur meine Werkbaenke“ an, keine Zuordnung: Banner + leere Liste wie bisher (`:66-92` unveraendert).
5. „Nur meine Werkbaenke“ aus: `workplaceId` leer = alle Werkbaenke, sonst diese eine (unveraendert).
6. Fehlteile (Lager) (`/MissingPartsLager`): Verhalten unveraendert.

## Technischer Loesungsentwurf

Muster: Repository-Filter in SQL vor Count/Paging (ADR [[0005-listen-view-pattern-mit-server-side-spaltenfilter]]),
Vorlage `GetShortageCountsForUserAsync` (`WarehouseRequisitionRepository.cs:480`).

**Repository** (`IWarehouseRequisitionRepository.cs:68`, `WarehouseRequisitionRepository.cs:392, 406-407`):
Parameter `int? workplaceFilter` ersetzen durch `IReadOnlyCollection<int>? workplaceIds`
(`null` = kein Werkbank-Filter, leere Liste = keine Treffer):

```csharp
if (workplaceIds != null)
    q = q.Where(i => workplaceIds.Contains(i.WarehouseRequisition.ProductionWorkplaceId));
```

Kein zweiter Parameter, keine Ueberladung: es gibt genau zwei produktive Aufrufer. `Contains` uebersetzt EF 10
zu einem `OPENJSON`-Parameter (Fallstrick „`Contains` ueber eine lange Liste“, [[fallstricke]]); die Werkbank-Liste
eines Users ist ohnehin klein.

**MissingPartsController.Index** (`:52-63`, `:96-129`): eine Variable `IReadOnlyCollection<int>? workplaceIds`
- `mineOnly == false`: `workplaceId.HasValue ? [workplaceId.Value] : null`
- `mineOnly == true` (nach dem unveraenderten No-Mapping-Return): `workplaceId.HasValue`
  ? (eigene → `[workplaceId.Value]`, fremde → `[]`) : alle eigenen Ids.

Dieselbe Variable an den Listen-Aufruf und beide Badge-Aufrufe. Zeilen `:103-117` (Namens-Nachfilter,
`total`-Ueberschreibung, `-1`-Zweig) und die Variable `effectiveWorkplaceId` entfallen; `total` kommt nur noch
aus dem Repository. Der Sonderfall „genau eine Werkbank“ (`:61-62`) faellt in die allgemeine Regel.

**MissingPartsLagerController** (`:44-45`, `:70-73`): `workplaceId.HasValue ? new[] { workplaceId.Value } : null`.

**View**: keine Aenderung (`WorkplaceFilter`/`MineOnly` im ViewModel bleiben).

## Migrations-/SQL-Auswirkungen

Keine. Kein Schema, keine Migration, kein `SQL/XX`, kein `00_FreshInstall.sql`, keine `SQL/AgentJobs/`.
Reservierter SQL-Block wird nicht gebraucht.

## Einstellungen

keine. „Nur meine Werkbaenke“ ist ein URL-Parameter (`mineOnly`, Default `true`, `MissingPartsController.cs:32`),
kein AppSetting und kein Benutzer-Default; die Werkbank-Zuordnung ist Stammdatum (`ProductionWorkplaceUsers`).

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine — reiner Lesepfad, keine Entitaet wird geschrieben.

## Rollen / Zugriff

Unveraendert: `MissingPartsController` `[RequireStockOrLagerbestellungAccess]` + `[RequireLagerbestellungAktiv]`
(`:10-11`), `MissingPartsLagerController` `[RequireLagerProcessingAccess]` + `[RequireLagerbestellungAktiv]` (`:10-11`).
Keine neue Rolle, kein neuer Filter.

## Listen-View-Pattern

Pagination (`PageSize.Resolve`, `PaginationState`, `_Pagination`) und Server-Spaltenfilter
(`data-server-column-filter="true"`, `Views/MissingParts/Index.cshtml:89`) bestehen; die Aenderung stellt die
Reihenfolge Filter → Count → Paging nach ADR 0005 fuer den Werkbank-Filter her. Keine neue Tabelle.

## Akzeptanzkriterien

1. Signatur `GetMissingPartsAsync(..., IReadOnlyCollection<int>? workplaceIds, ...)`; `null` filtert nicht, leere
   Liste liefert `([], 0)`.
2. Repository-Test (InMemory): Fehlteile auf Werkbank 1, 2, 3; Aufruf mit `[1, 2]` und `pageSize = 1` →
   `TotalCount` = Anzahl auf 1+2, Seite 2 enthaelt eine Zeile aus 1 oder 2, nie aus 3.
3. Controller-Test: `mineOnly=true`, User hat Werkbaenke 7 und 8, kein `workplaceId` → alle drei Repo-Aufrufe
   (Liste + 2 Badges) erhalten genau `{7, 8}`; `Pagination.TotalCount` = vom Repo gelieferter Wert (Mock liefert
   z. B. 25 bei 10 Zeilen → 25, nicht 10).
4. Controller-Test: `mineOnly=true`, `workplaceId=99` fremd → alle drei Aufrufe mit leerer Liste; Items leer, TotalCount 0.
5. Controller-Test: `mineOnly=true`, eine Werkbank 7 → alle Aufrufe mit `{7}` (bestehender Test `:55-66` angepasst).
6. Controller-Test: `mineOnly=false`, `workplaceId=5` → `{5}`; ohne `workplaceId` → `null` (bestehender Test `:44-52` angepasst).
7. `MissingPartsLagerController`: `workplaceId` → `{id}` bzw. `null`; bestehende Tests gruen (nur `It.IsAny<int?>`
   → `It.IsAny<IReadOnlyCollection<int>?>` getauscht).
8. Im Controller gibt es keinen Nachfilter auf `rawRows` und keine Zuweisung an `total` ausser aus dem Repository.
9. Manuell: Szenario unten Schritt 1–4 auf dem Testsystem bestanden.
10. `dotnet build IdealAkeWms.slnx` und `dotnet test` gruen.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (naechste freie TS-Nummer, Stand Worktree nach TS-106; der Dev-Lauf prueft
gegen die anderen Buendel-Specs), Querverweis aus 33.12; danach `secondbrain/tests/testszenarien-index.md`.

**TS-1xx — Fehlteile „Nur meine Werkbaenke“ ueber alle Seiten (v1.75.0)**
- Vorbedingungen: User U mit Werkbaenken A und B, fremde Werkbank C; Seitengroesse 10. Mindestens 12 Fehlteile
  (Tab „Wird nicht nachgeliefert“) auf C mit juengerem Abschlussdatum, 3 auf A, 2 auf B; analog einige
  „Offene Fehlteile“ auf A und C.
- Schritte / Erwartet:
  1. Als U `Lager → Meine Fehlteile`, Tab „Wird nicht nachgeliefert“. **Erwartet:** 5 Zeilen (A+B), Trefferzahl 5,
     eine Seite, keine Zeile von C. Badge „Wird nicht nachgeliefert“ = 5, Badge „Offene Fehlteile“ = Anzahl auf A+B.
  2. Seitengroesse 2. **Erwartet:** 3 Seiten, zusammen genau die 5 Zeilen, Trefferzahl 5.
  3. Spaltenfilter Werkbank = `A`. **Erwartet:** 3 Zeilen, Trefferzahl 3.
  4. Haken „Nur meine Werkbaenke“ aus. **Erwartet:** alle Werkbaenke inkl. C, Badges zaehlen alle.
- Negativfaelle:
  5. URL `/MissingParts?mineOnly=true&workplaceId=<Id von C>`. **Erwartet:** leere Liste, Trefferzahl 0, beide Badges 0.
  6. User mit genau einer Werkbank: Verhalten wie bisher (nur diese Werkbank, Badges passend).
  7. User ohne Zuordnung: Banner wie 33.13.
  8. `Lager → Fehlteile (Lager)` mit und ohne Werkbank-Auswahl: unveraendert wie 33.14.

## Etappen (nur bei epic: true)

Nicht zutreffend (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

*Final (QA-Lauf 2026-11-07, aus dem echten Diff `a3dced19..e3071344`).*

- **Web-App:** ja — `IdealAkeWms/Controllers/MissingPartsController.cs`, `MissingPartsLagerController.cs`,
  `Data/Repositories/(I)WarehouseRequisitionRepository.cs`, `Views/Help/Changelog.cshtml`, `AppVersion.cs`.
- **Service:** ja, **nur Versions-Bump** (`IDEALAKEWMSService/AppVersion.cs` 1.73.0 → 1.75.0, kein Verhaltenscode).
  Mitpublizieren, damit Web und Service dieselbe Version melden.
- **Migration:** nein (keine Datei unter `*/Migrations/`, kein `SQL/`, kein Einmal-Skript).
- **Version:** v1.75.0 in beiden `AppVersion.cs`, Eintrag in `Views/Help/Changelog.cshtml`.
- **Publish-Befehle** (im Worktree, nach dem Testsystem-Lauf ist die Reihenfolge egal — keine DB-Aenderung):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- Ablauf Mensch: Publish aus dem Worktree → Testsystem → Test → Merge. Nach dem Merge nur dann erneut aus `main`
  publizieren, wenn der Merge getestete Dateien mit parallelen `main`-Aenderungen zusammengefuehrt hat.
- **Standort:** beide — der Code hat keine Standortweiche (kein `ProduktionsauftragHierarchisch`/OSEON-Bezug in
  `MissingPartsController.cs` und `WarehouseRequisitionRepository.cs:389-447`); testbar dort, wo
  `LagerbestellungAktiv` gesetzt ist (Gate `RequireLagerbestellungAktiv`).

## Offene Rueckfragen

1. **Tab-Badges im Modus „Nur meine Werkbaenke“** zaehlen kuenftig nur die eigenen Werkbaenke (bisher alle
   Werkbaenke bei mehreren Werkbaenken bzw. fremder `workplaceId`, `MissingPartsController.cs:120-129`).
   Einverstanden? *Empfehlung: ja — Badge und Liste muessen dieselbe Menge zeigen; die Spec ist so geschrieben.*
2. **Zusatzfund Karte „Meine Fehlteile“** (`/WarehouseRequisitions`) zaehlt Lager+Glas zusammen
   (`WarehouseRequisitionRepository.cs:477-488`, kein Typ-Filter), der Link fuehrt in eine typgefilterte Liste.
   Eigener Backlog-Eintrag? *Empfehlung: ja, nicht in dieser Spec.*

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Tab-Badges zaehlen im Modus „Nur meine Werkbaenke“ nur eigene Werkbaenke → **ja (Gerald 2026-11-07)**
2. Zusatzfund Karte „Meine Fehlteile“ (Lager+Glas) als eigener Backlog-Eintrag → **ja, eigener Backlog-Eintrag (Gerald 2026-11-07)**

## QA-Nachweis (qa-agent, 2026-11-07)

Worktree `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5`, HEAD `e3071344`, Worktree sauber.
Review-Bereich `git diff a3dced19 e3071344` (ein Commit, 11 Dateien, +140/−62). Build und Tests ueber den ganzen Worktree.

### 1. Build und Tests
- `dotnet build IdealAkeWms.slnx`: **0 Fehler**, 28 Warnungen (vorbestehend: CS8601/CS8321/NU1902/NU1903).
- `dotnet test`:
  - `IdealAkeWms.Tests`: **2396 bestanden, 1 uebersprungen, 0 fehlgeschlagen** (2397 gesamt)
  - `IDEALAKEWMSService.Tests`: **497 bestanden, 0 uebersprungen, 0 fehlgeschlagen**

### 2. Code-Review (selbst, in diesem Lauf)
Geprueft: Repository-Filter (`WarehouseRequisitionRepository.cs:389-450`, Filter vor `CountAsync`/`Skip`/`Take`),
Interface, `MissingPartsController.Index` komplett (`:28-128`, inkl. No-Mapping-Return `:66-93`),
`MissingPartsLagerController` (`:40-75`), alle Aufrufer von `GetMissingPartsAsync` (Grep `*.cs`: nur die zwei Controller,
kein Caching-Decorator), Testaenderungen, Changelog-View, `docs/TESTSZENARIEN.md`.
Gezielte Defektklassen: kein `<script>`-Embedding, kein `hidden`/`d-*`, keine View-Aenderung ausser Changelog-HTML;
kein Parallel-Array-Binding; keine reine Browser-Validierung; die Werkbank-Regel lebt jetzt an **einer** Stelle (SQL im
Repository) — der zweite In-Speicher-Filter nach Werkbank-**Name** und der Sentinel `-1` sind entfernt (verringert Drift).

| # | Befund | Schwere | Behandlung |
|---|---|---|---|
| B1 | TS-108 Vorbedingung „Seitengroesse 10“ und Schritt 108.2 „Seitengroesse 2“ sind nicht ausfuehrbar: `PageSize.AllowedOptions = {25, 50, 100, Alle}` (`IdealAkeWms/Services/PageSize.cs:15`), andere Werte fallen still auf 25. Gleiches gilt fuer den Spec-Text (Abschnitt Test-Szenarien). | niedrig (Doku) | Offen fuer den Dev-Lauf: TS-108 auf Seitengroesse 25 umschreiben (z. B. mindestens 26 eigene Fehlteile, mindestens 25 juengere fremde). Auf DEV mit 25 nachgestellt (siehe 6). Blockiert Testbereit nicht. |
| B2 | `AppVersion.Date` steht in beiden `AppVersion.cs` weiter auf `2026-11-06`; Version 1.75.0 und Anwender-Changelog sagen 07.11.2026. | niedrig | Offen fuer den Dev-Lauf (eine Konstante je Datei). |
| B3 | AK 2: Der Repository-Test hat keine Zeile auf Werkbank 3, „nie aus 3“ ist dort nicht direkt gezeigt. Ausschluss fremder Werkbaenke ist durch den `{1}`-Aufruf (schliesst WB2 aus) belegt und auf DEV gegen echten SQL Server (32 fremde, juengere Zeilen) bestaetigt. | niedrig | Akzeptiert, kein Nacharbeiten noetig. |

Nebenbeobachtungen, vorbestehend und nicht Teil des Diffs (nur gemeldet): `_user.GetCurrentAppUserId() ?? 0`
(`MissingPartsController.cs:58`, unveraenderte Zeile); Paging-Text „Eintraege 51–5 von 5“ bei einer Seite ausserhalb des
Bereichs (`PaginationState`, betrifft alle Listen).

### 3. Nachweisart je Akzeptanzkriterium
| AK | Nachweis |
|---|---|
| 1 Signatur, `null` filtert nicht, leer → `([],0)` | automatisiert: `WarehouseRequisitionRepositoryTests.GetMissingPartsAsync_AppliesWorkplaceFilter` (InMemory) |
| 2 Menge + Paging im Repository | automatisiert (InMemory, Logik); `Contains`-Uebersetzung nach SQL Server ist **Manual-UAT**, auf DEV bestaetigt (108.1/108.2) |
| 3 mehrere Werkbaenke, alle drei Aufrufe `{7,8}`, Total aus Repo | automatisiert: `Index_MineOnly_MehrereWerkbaenke_AlleAufrufeMitMenge_TotalAusRepo_AK3` |
| 4 fremde Werkbank → leere Menge, 0 | automatisiert: `Index_MineOnly_FremdeWerkbank_AlleAufrufeLeereMenge_AK4` |
| 5 eine Werkbank `{7}` | automatisiert: `Index_MineOnly_SingleUserWorkplace_FiltersByThatId` |
| 6 mineOnly=false `{5}` / `null` | automatisiert: `Index_MineOnlyFalse_PassesWorkplaceIdToRepoUnchanged`, `Index_MineOnlyFalse_OhneWerkbank_KeinFilter_AK6` |
| 7 Lager-Controller | automatisiert (Signatur-Tausch, Tests gruen) + DEV 108.8 |
| 8 kein Nachfilter, `total` nur aus Repo | Review: `MissingPartsController.cs:97-98` |
| 9 manuell Schritte 1–4 | **Manual-UAT**: auf DEV bestanden, Testsystem steht aus |
| 10 Build + Tests | gruen (siehe 1) |

### 4. Geaenderte bestehende Tests
| Test | Einordnung |
|---|---|
| `MissingPartsControllerTests`: `Build()`-Setup, `Index_DefaultTab_WillBeRestocked`, `Index_TabNone_NormalizedToWillBeRestocked`, `Index_ViewModelHasBothCounts`, Typ-Capture-Test | reiner Typ-Tausch `int?` → `IReadOnlyCollection<int>?` (Compile-Fix) |
| `Index_MineOnlyFalse_PassesWorkplaceIdToRepoUnchanged` | Typ-Tausch `5` → `Ids(5)`, gleiche Aussage (AK 6) |
| `Index_MineOnly_SingleUserWorkplace_FiltersByThatId` | Verhalten bewusst erweitert: zusaetzlich alle drei Aufrufe mit `{7}` (AK 5) |
| `MissingPartsLagerControllerTests` (5 Setups/Verify) | reiner Typ-Tausch (AK 7) |
| `WarehouseRequisitionRepositoryTests.GetMissingPartsAsync_AppliesWorkplaceFilter` | Typ-Tausch `1` → `new[]{1}` + neue Asserts (AK 1/2) |

Kein Fall „sonstiges“; keine Fixture wurde an ein Ergebnis angepasst.

### 5. CLAUDE.md-Checkliste
Migration/SQL/FreshInstall/AgentJobs: entfaellt (reiner Lesepfad). Audit-Felder: entfaellt. Version 1.75.0 in beiden
`AppVersion.cs` + Anwender-Changelog: ja (Datum siehe B2). Brain-Changelog
[[2026-11-07-v1-75-0-fehlteile-nur-meine-werkbaenke-trefferzahl]]: vorhanden. `docs/TESTSZENARIEN.md` TS-108 +
Querverweis 33.12: ja (siehe B1). Testindex Zeile 108: vorhanden. ADR 0020: keine neue oder geaenderte Einstellung im Diff.

### 6. DEV-Deploy und Testlauf
- **Standort:** `beide` (Frontmatter). Shell erhoeht (Administrator = True).
- `dev-deploy.ps1 -Standort AKE -Quelle <worktree>`: Exit 0, „HTTP 200, Fusszeile v1.75.0“, DEV-Absicherung angewandt, Dienst gestartet.
- `dev-deploy.ps1 -Standort IDEAL -Quelle <worktree>`: Exit 0, „HTTP 200, Fusszeile v1.75.0“, DEV-Absicherung angewandt, Dienst gestartet.
- Footer-Version = Branch-AppVersion 1.75.0 an beiden Standorten.
- `LagerbestellungAktiv = true` auf `AKE_WMS_DEV` und `IDEAL_WMS_DEV` (nicht veraendert).
- **Testdaten (AKE_WMS_DEV):** Werkbaenke `QA108-A` (73), `QA108-B` (74), `QA108-C` (75), `QA108-D` (76);
  Anforderungen mit `Comment LIKE 'QA TS-108%'`, Typ Lager, Status Closed: NoRestock A=3, B=25, C=32 (C mit juengerem
  Abschlussdatum), WillBeRestocked A=2, C=4. Benutzer „Gerald Weichbold“ (Id 14) fuer die Schritte auf A+B umgehaengt,
  danach Ursprungszuordnung (nur A1, Id 18) wiederhergestellt. Testdaten bleiben in der DEV-DB.
- Abweichung vom Szenario: Seitengroesse 25 statt 10/2 (B1); daher B mit 25 statt 2 Zeilen, damit es zwei Seiten gibt.

| Schritt | Durchgefuehrt | Ergebnis | Urteil |
|---|---|---|---|
| 108.1 | `/MissingParts?tab=NoRestock`, U mit A+B (zunaechst B=2) | 5 Zeilen (A 3, B 2), „Eintraege 1–5 von 5“, keine C-Zeile; Badges Offene 2 / Wird nicht nachgeliefert 5 | OK |
| 108.2 | B auf 25 erhoeht (eigene = 28), Seitengroesse 25, Seite 1 und 2 | S. 1 „1–25 von 28“, nur A/B; S. 2 „26–28 von 28“, 3 B-Zeilen; Badge 28; Navigation 1/2. Der alte Code haette S. 1 leer gezeigt (32 juengere C-Zeilen) | OK |
| 108.3 | `colf_WorkplaceName=QA108-A` | 3 Zeilen, „1–3 von 3“ (Badges bleiben 2/28: Badges ignorieren Spaltenfilter, vorbestehend) | OK |
| 108.4 | `mineOnly=false` | Liste mit QA108-C zuerst, „1–25 von 63“; Badges 20/63 = SQL-Gegenprobe ueber alle Werkbaenke (20/63) | OK |
| 108.5 | `mineOnly=true&workplaceId=75` (fremd) | „Keine Eintraege“, beide Badges 0, Dropdown zeigt QA108-C | OK |
| 108.6 | U nur auf A | 3 Zeilen, Badges 2/3 | OK |
| 108.7 | U ohne Zuordnung | Info-Banner „keine Werkbank-Zuordnung“, Liste leer, Badges 0/0, keine Fehlermeldung | OK |
| 108.8 | `/MissingPartsLager` ohne und mit `workplaceId=74` | ohne: alle Werkbaenke, Badges 20/63; mit B: „1–25 von 25“, Badges 0/25 | OK |
| IDEAL | `http://localhost:8082/MissingParts?mineOnly=false` | Seite laedt, v1.75.0; keine Fehlteile in IDEAL_WMS_DEV (nur Smoke, kein Datenfall) | OK (Smoke) |

### Gesamturteil
Build und Tests gruen, Review ohne blockierende Befunde (B1/B2 niedrig, offen fuer den Dev-Lauf), alle TS-108-Schritte
auf DEV AKE bestanden, IDEAL-Smoke ok. **Status: Testbereit.**

### Manuelle Checkliste fuer den Menschen (offen)
1. Testsystem-Lauf TS-108.1–108.8 mit echten Daten (Seitengroesse 25 statt 10/2, siehe B1), Standort mit `LagerbestellungAktiv`.
2. IDEAL: TS-108.1/108.5 mit echten Fehlteilen (DEV IDEAL hatte keine Fehlteile, dort nur Smoke).
3. Pruefen, ob der Dev-Lauf B1 (TS-108-Text) und B2 (`AppVersion.Date`) nachgezogen hat.
4. Optional: QA-Testdaten in AKE_WMS_DEV (`QA108-*`, `Comment LIKE 'QA TS-108%'`) behalten oder entfernen.
