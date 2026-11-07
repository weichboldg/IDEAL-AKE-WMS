---
type: spec
title: Fehlteile „Nur meine Werkbaenke“ in SQL filtern (Trefferzahl, Seiten, Tab-Badges)
slug: 2026-11-07-fehlteile-nur-meine-werkbaenke-trefferzahl-spec
status: Entwurf
created: 2026-11-07
updated: 2026-11-07
source_backlog: "[[2026-10-28-fehlteile-nur-meine-werkbaenke-trefferzahl]]"
bug: "[[2026-11-07-fehlteile-nur-meine-werkbaenke-trefferzahl-bug]]"
buendel: "[[2026-11-07-buendel-uebersicht]]"
task: ""
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
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
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

*Vorlaeufig — der QA-Lauf bestaetigt gegen den echten Diff.*

- **Web-App:** ja (Controller + Repository der Web-App)
- **Service:** nein
- **Migration:** nein
- **Version:** v1.75.0 in beiden `AppVersion.cs` (Web + Service, Buendel-Regel), Eintrag in `Views/Help/Changelog.cshtml`.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
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

1. Tab-Badges zaehlen im Modus „Nur meine Werkbaenke“ nur eigene Werkbaenke →
2. Zusatzfund Karte „Meine Fehlteile“ (Lager+Glas) als eigener Backlog-Eintrag →
