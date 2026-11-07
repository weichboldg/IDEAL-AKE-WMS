---
type: spec
title: Fehlteile – Karte „Meine Fehlteile“ je Bestelltyp, Werkbank-Auswahl im Modus „Nur meine Werkbaenke“
slug: 2026-11-08-fehlteile-karte-meine-fehlteile-lager-glas-spec
status: Testbereit
created: 2026-11-07
updated: 2026-11-07
source_backlog: "[[2026-11-07-fehlteile-karte-meine-fehlteile-lager-glas]]"
bug: "[[2026-11-08-fehlteile-karte-meine-fehlteile-lager-glas-bug]]"
buendel: "[[2026-11-08-buendel-uebersicht]]"
depends_on: "[[2026-11-07-fehlteile-nur-meine-werkbaenke-trefferzahl-spec]]"
task: "[[2026-11-08-buendel-2026-11-08-goal-lauf]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
version: "1.81.0"
standort: beide
affected_code:
  - "IdealAkeWms/Data/Repositories/IWarehouseRequisitionRepository.cs:73-80 (GetShortageCountsForUserAsync)"
  - "IdealAkeWms/Data/Repositories/WarehouseRequisitionRepository.cs:469-493 (kein Typ-Filter)"
  - "IdealAkeWms/Controllers/WarehouseRequisitionsController.cs:62-64, 91-92"
  - "IdealAkeWms/Views/WarehouseRequisitions/Index.cshtml:48-49, 59-60 (Links ohne type)"
  - "IdealAkeWms/Controllers/MissingPartsController.cs:54-64, 75, 109 (AvailableWorkplaces = GetAllAsync)"
  - "IdealAkeWms/Models/ViewModels/MissingPartsListViewModel (neues Flag fremde Werkbank)"
  - "IdealAkeWms/Views/MissingParts/Index.cshtml:62-85 (Filterkarte: disabled :70, Checkbox ohne false-Wert :79-80)"
  - "IdealAkeWms.Tests/Repositories/WarehouseRequisitionRepositoryTests.cs:701, 739"
  - "IdealAkeWms.Tests/Controllers/WarehouseRequisitionsControllerTests.cs"
  - "IdealAkeWms.Tests/Controllers/MissingPartsControllerTests.cs"
  - "docs/TESTSZENARIEN.md (33.12/33.13 Querverweis, neu TS-113)"
open_questions:
  - "Karte 'Meine Fehlteile' auf /WarehouseRequisitions zaehlt kuenftig nur den aktiven Bestelltyp (Reiter Lager bzw. Glas) und der Link traegt den Typ mit. Das kippt die fruehere bewusste Entscheidung 'Zaehler typ-uebergreifend' (docs/superpowers/plans/2026-07-03-glas-bestellung.md:481,858). Einverstanden? Empfehlung: ja."
  - "Zusatzbefund: Der Haken 'Nur meine Werkbaenke' laesst sich per Klick nicht ausschalten (abgewaehlte Checkbox sendet nichts, Parameter-Default mineOnly=true). In dieser Spec mitbeheben? Empfehlung: ja (eine Zeile in derselben Filterkarte; ohne sie ist die entsperrte Werkbank-Auswahl nur halb nutzbar)."
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

Zwei Fehler auf dem Weg „Lagerbestellungen → Meine Fehlteile“. Beide sorgen dafür, dass Zahl und Liste nicht zusammenpassen bzw. der Werker
nicht eingrenzen kann.

**A – Karte zählt Lager und Glas zusammen.**
- Die Karte „⚠ Meine Fehlteile“ auf `/WarehouseRequisitions` (`Views/WarehouseRequisitions/Index.cshtml:40-69`) bekommt ihre Zahlen von
  `GetShortageCountsForUserAsync(userId)` (`WarehouseRequisitionsController.cs:91-92`).
- Dieser filtert nur Status und Werkbänke des Users, **nicht** den Bestelltyp (`WarehouseRequisitionRepository.cs:479-485`).
- Die Links (`Index.cshtml:48-49,59-60`) übergeben keinen `type`. `MissingPartsController.Index` setzt dann `activeType` auf Lager, sofern der
  User Lager darf (`MissingPartsController.cs:43`), und filtert die Liste nach diesem Typ (`WarehouseRequisitionRepository.cs:404-405`).
- Folge: „5 Fehlteile“ auf der Karte, aber 3 in der Liste. Wer im Reiter Glas auf die Karte klickt, landet sogar in der Lager-Liste.
- Herkunft: Beim Glas-Bestelltyp wurde die Karte bewusst typübergreifend gelassen (`docs/superpowers/plans/2026-07-03-glas-bestellung.md:481,858`
  „Zähler bewusst typ-übergreifend … out-of-scope“). Heute ist die Liste aber typgefiltert, daher Rückfrage 1.

**B – Werkbank-Auswahl im Modus „Nur meine Werkbänke“ gesperrt und mit allen Werkbänken.**
- Das Dropdown ist bei `MineOnly` `disabled` (`Views/MissingParts/Index.cshtml:70-71`). Der Werker mit mehreren Werkbänken kann also nicht auf
  eine davon eingrenzen.
- Befüllt wird es mit **allen** Werkbänken (`MissingPartsController.cs:109` `GetAllAsync()`).
- Dokumentiert war das Gegenteil: Anwender-Changelog „Der Werkbank-Dropdown in ‚Meine Fehlteile‘ zeigt ausschliesslich die eigenen, in den
  Stammdaten hinterlegten Werkbaenke“ (`Views/Help/Changelog.cshtml:2112-2114`). Nur der No-Mapping-Zweig lädt die eigenen
  (`MissingPartsController.cs:75`).
- Seit v1.75.0 ([[2026-11-07-fehlteile-nur-meine-werkbaenke-trefferzahl-spec]]) filtert die Liste eine **eigene** `workplaceId` korrekt und eine
  fremde zu leer (`MissingPartsController.cs:61-63`). Das Dropdown kann diese Werte nur nicht erzeugen.

**C – Zusatzbefund (Rückfrage 2): Haken „Nur meine Werkbänke“ lässt sich nicht ausschalten.**
- Die Checkbox (`Index.cshtml:79-80`) sendet abgewählt nichts. Der gesperrte Select wird ebenfalls nicht gesendet. `mineOnly` fällt dann auf den
  Default `true` zurück (`MissingPartsController.cs:32`, belegt durch den Test `Index_DefaultIsMineOnlyTrue`, `MissingPartsControllerTests.cs:189-197`).
- Der Banner „Toggle deaktivieren“ (`Index.cshtml:12-14`, TS-33.13) läuft damit ins Leere.
- Der QA-Lauf von v1.75.0 hat Schritt 108.4 per URL `mineOnly=false` ausgeführt, nicht per Klick (QA-Nachweis der Vorgänger-Spec, Zeile 108.4).
- Laufzeitbestätigung: TS-113 Schritt 6.

Bug-Record: Der Auftrag dieses Spec-Laufs beschränkt die Schreibziele auf die Spec-Dateien. `secondbrain/bugs/…-bug.md` legt der Mensch bzw.
die Koordination an. Das Feld `bug:` bleibt bis dahin leer.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- A: `GetShortageCountsForUserAsync` mit Typ-Parameter. Der Controller übergibt `activeType`, die Links tragen `type` mit.
- B: Das Werkbank-Dropdown in „Meine Fehlteile“ ist im Modus „Nur meine Werkbänke“ bedienbar und listet nur die eigenen Werkbänke. Ist eine fremde
  `workplaceId` per URL gesetzt, erscheint ein Hinweis statt einer stillen Leerliste.
- C (nur wenn Rückfrage 2 = ja): Der Haken lässt sich ausschalten.
- Tests, TS-113, Querverweis aus 33.12/33.13, Anwender-Changelog, Version v1.81.0.

**Out-of-Scope**
- `/MissingPartsLager` (eigene View `Views/MissingPartsLager/Index.cshtml`, kein `mineOnly`).
- Badges, die Spaltenfilter ignorieren (QA-Nebenbeobachtung v1.75.0, 108.3), und der Paging-Text „51–5 von 5“. Beides ist vorbestehend und betrifft
  alle Listen.
- `_user.GetCurrentAppUserId() ?? 0` (`MissingPartsController.cs:58`). Vorbestehend, nicht anfassen.

**Prüfung „gleiches Muster anderswo“**
- `GetShortageCountsForUserAsync` hat genau einen produktiven Aufrufer, `WarehouseRequisitionsController.cs:92` (Grep `*.cs`, sonst nur
  `WarehouseRequisitionRepositoryTests.cs:701,739`). Es gibt keinen Caching-Decorator.
- `mineOnly` gibt es nur in `MissingParts` (Grep in `Views/`: nur `MissingParts/Index.cshtml` und die zwei Links in `WarehouseRequisitions/Index.cshtml:49,60`).
- Eine Checkbox, die mit Default `true` bindet, ohne `false`-Wert: Der Dev-Lauf greppt Controller-Parameter `bool … = true` gegen
  `type="checkbox"` ohne versteckten `false`-Wert und meldet weitere Funde nur, ohne sie mitzubauen.

## Fachliche Anforderungen

1. **Karte je Typ.** Im Reiter Lager zählt die Karte nur Lager-Fehlteile, im Reiter Glas nur Glas. Beide Zeilen der Karte (Positionen und
   „aus N Bestellungen“) gelten für den aktiven Typ. Der Link öffnet `/MissingParts` mit demselben Tab, `type` und `mineOnly=true`.
   Die Zahl auf der Karte ist dann gleich der Trefferzahl der Liste ohne Spaltenfilter.
2. **Werkbank-Auswahl „Nur meine Werkbänke“.**
   - Das Dropdown ist bedienbar und enthält „— Alle —“ (= alle eigenen Werkbänke) sowie die eigenen Werkbänke nach Name.
   - Eine Auswahl filtert die Liste und beide Badges auf diese Werkbank. Das funktioniert seit v1.75.0, `MissingPartsController.cs:61-63`.
3. **„Nur meine Werkbänke“ aus.** Das Dropdown listet alle Werkbänke wie bisher (`GetAllAsync`).
4. **Fremde Werkbank per URL** (`mineOnly=true&workplaceId=<fremd>`). Liste und Badges bleiben leer wie in v1.75.0. Zusätzlich erscheint ein sichtbarer
   Hinweis *„Die gewählte Werkbank ist dir nicht zugeordnet – die Liste ist deshalb leer.“* Das Dropdown steht auf „— Alle —“.
   Hausregel: melden statt still behandeln.
5. **Keine Zuordnung.** Banner und Leerliste wie bisher (TS-33.13).
6. **(Rückfrage 2)** Abwählen des Hakens lädt die Liste mit `mineOnly=false`. Anwählen lädt sie mit `mineOnly=true`.

## Technischer Loesungsentwurf

**A – Repository und Controller**
- `GetShortageCountsForUserAsync(int userId, WarehouseRequisitionType? type)`: `null` = alle Typen, Semantik wie `GetMissingPartsAsync`
  (`WarehouseRequisitionRepository.cs:404-405`). In `:479-482` kommt `&& (type == null || i.WarehouseRequisition.Type == type)` dazu bzw. ein
  `if (type.HasValue) q = q.Where(…)`. XML-Doku im Interface (`IWarehouseRequisitionRepository.cs:73-77`) ergänzen.
- `WarehouseRequisitionsController.cs:92`: `GetShortageCountsForUserAsync(userId, activeType)`. `activeType` steht bereits ab `:62-64`.
- `Views/WarehouseRequisitions/Index.cshtml:48-49,59-60`: zusätzlich `asp-route-type="@Model.ActiveType"`.

**B – MissingPartsController.Index**
- `AvailableWorkplaces` (`:109`): `mineOnly ? userWorkplaces : (await _workplaces.GetAllAsync()).OrderBy(w => w.Name).ToList()`.
  `userWorkplaces` ist schon geladen und sortiert (`:59`, `ProductionWorkplaceRepository.cs:70-77`, `OrderBy(w => w.Name)`). Die Variable aus dem
  `if`-Block heben. Der No-Mapping-Zweig (`:75`) nutzt dieselbe Variable statt eines zweiten Aufrufs.
- Neues ViewModel-Flag `ForeignWorkplaceSelected` = `mineOnly && workplaceId.HasValue && !userWorkplaceIds.Contains(workplaceId.Value)`.
  Die Bedingung steht schon in `:61-62`. In der View ist es ein `alert alert-warning` mit dem Text aus Anforderung 4.
  Hinweis: Das ist ein ViewModel-Flag, kein TempData. Es gibt keinen Redirect, und TempData kennt nur Success/Warning für Redirects (CLAUDE.md).

**B – View `Views/MissingParts/Index.cshtml`**
- `:70-71`: `disabled` und `title` entfallen.
- Die Option „— Alle —“ bleibt (`:72`). Im Modus „Nur meine“ bedeutet sie „alle eigenen“, im anderen Modus „alle“. Der Controller liefert die passende
  Liste, die View braucht keine Fallunterscheidung.

**C – Checkbox (Rückfrage 2)**
- Direkt **nach** der Checkbox (`:79-80`) kommt `<input type="hidden" name="mineOnly" value="false" />`. Angehakt werden `true,false` gesendet,
  der `bool`-Binder nimmt den ersten Wert. Abgewählt wird nur `false` gesendet. Das ist das Muster des ASP.NET-Checkbox-Helpers.
- Das ist ein natives Plattform-Mittel, kein JavaScript.

**Frontend:** Skill `frontend-design` ist Pflicht (View-Änderung). Bootstrap-Bestand, Hinweis mit Text, nicht nur Farbe.

**Dev-Befund (2026-11-08, Umsetzung `747c1ced`):** Grep „Controller-Parameter `bool … = true` gegen Checkbox ohne `false`-Feld“: außer
`mineOnly` nur `TrackingController.cs:303,371` `useRelevanceFilter`. Dort steht das versteckte `false`-Feld **vor** der Checkbox
(`Views/Tracking/OseonIndex.cshtml:52-53`) – angehakt kommen `false,true`, der bool-Binder nimmt den ersten Wert (`false`). Ob der Filter
„Fremdsystem-Relevanz“ sich damit wieder einschalten lässt, ist ungeprüft (Laufzeit). Nur gemeldet, nicht mitgebaut (eigener Backlog-Punkt).

## Migrations-/SQL-Auswirkungen

Keine. Kein Schema, keine Migration, kein `SQL/XX`, kein `00_FreshInstall.sql`, keine `SQL/AgentJobs/`. Falls der Dev-Lauf doch Schema braucht,
stoppt er und meldet das als offene Frage. Er vergibt keine SQL-Nummer selbst.

## Einstellungen

keine. `mineOnly` ist ein URL-Parameter mit Default `true` (`MissingPartsController.cs:32`), kein AppSetting. Die Werkbank-Zuordnung ist
Stammdatum (`ProductionWorkplaceUsers`). Der Master-Schalter `LagerbestellungAktiv` (Gate `RequireLagerbestellungAktiv`) bleibt unverändert.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine. Das ist ein reiner Lesepfad, keine Entität wird geschrieben.

## Rollen / Zugriff

Unverändert:
- `WarehouseRequisitionsController`: `[RequirePickingOrStockOrLagerbestellungAccess]` + `[RequireLagerbestellungAktiv]` (`:10-11`).
- `MissingPartsController`: `[RequireStockOrLagerbestellungAccess]` + `[RequireLagerbestellungAktiv]` (`:10-11`).
- Typ-Berechtigung über `CanOrderLagerAsync`/`CanOrderGlasAsync` (`MissingPartsController.cs:41-46`, `WarehouseRequisitionsController.cs:60-64`).
  Der Link überträgt nur den schon erlaubten `activeType`, `MissingParts` prüft ihn erneut.

Es gibt keine neue Rolle und keinen neuen Filter.

## Listen-View-Pattern

Es entsteht keine neue Tabelle. In `MissingParts/Index` bestehen Pagination, Filterkarte und Server-Spaltenfilter (`:87-90,127`). Die Änderung
macht den Werkbank-Filter der Filterkarte im Modus „Nur meine“ bedienbar. Die Reihenfolge Filter → Count → Paging bleibt unverändert (v1.75.0).

## Akzeptanzkriterien

1. Repository-Test (InMemory): User mit Werkbank 1. Dort 2 Lager-Fehlteile in Bestellung L1 und 3 Glas-Fehlteile in G1 und G2, Status
   `WillBeRestocked`. `GetShortageCountsForUserAsync(u, Lager)` → Positionen 2, Bestellungen 1. `(u, Glas)` → 3 und 2. `(u, null)` → 5 und 3.
   `NoRestock` analog. Die bestehenden Tests `:701`, `:739` laufen mit `null` grün.
2. Controller-Test `WarehouseRequisitionsController.Index(type: Glas)` bei Glas-Recht: Der Repository-Aufruf erhält `Glas`. Ohne `type` bei
   Lager-Recht erhält er `Lager`.
3. View: Beide Karten-Links enthalten `type=<aktiver Typ>`. Nachweis über den Testsystem-Lauf TS-113.2 oder einen View-Text-Test.
4. Controller-Test `MissingParts`, `mineOnly=true`, User mit Werkbänken 7 und 8: `AvailableWorkplaces` = genau {7, 8} nach Name, `GetAllAsync` wird
   **nicht** aufgerufen.
5. Controller-Test `mineOnly=false`: `AvailableWorkplaces` = Ergebnis von `GetAllAsync`.
6. Controller-Test `mineOnly=true`, `workplaceId=99` fremd: `ForeignWorkplaceSelected = true`, Items leer, beide Badges 0. Mit eigener Werkbank ist das Flag
   `false`.
7. View: Der Werkbank-`<select>` in `MissingParts/Index.cshtml` hat kein `disabled` mehr. Das Hinweis-Alert erscheint nur bei
   `ForeignWorkplaceSelected`.
8. (Rückfrage 2) View: Hinter der Checkbox `mineOnly` steht ein verstecktes Feld `mineOnly=false`. Manuell: Abwählen zeigt alle Werkbänke (TS-113.6).
9. `dotnet build IdealAkeWms.slnx` und `dotnet test` sind grün. Anwender-Changelog v1.81.0 ist ergänzt.
10. Manuell: TS-113 auf AKE-DEV bestanden, IDEAL-DEV als Smoke-Test.

## Test-Szenarien

Neues Kapitel **TS-113** in `docs/TESTSZENARIEN.md`, Querverweis aus 33.12 und 33.13. Danach `secondbrain/tests/testszenarien-index.md`.
Seitengröße nur aus `{25, 50, 100, Alle}` (`Services/PageSize.cs:15`, Befund B1 aus v1.75.0).

**TS-113 – Karte „Meine Fehlteile“ je Bestelltyp, Werkbank-Auswahl „Nur meine Werkbänke“ (v1.81.0)**
- Vorbedingungen: `LagerbestellungAktiv = true`. User U mit Lager- **und** Glas-Bestellrecht, zugeordnet zu Werkbank A und B. Werkbank C fremd.
  Fehlteile `WillBeRestocked`: 2 Lager auf A, 3 Glas auf B (in 2 Bestellungen), 4 Lager auf C.
- Schritte / Erwartet:
  1. Als U `/WarehouseRequisitions`, Reiter Lager. **Erwartet:** Karte „2 Fehlteile (wird nachgeliefert) aus 1 Bestellungen“.
  2. Link anklicken. **Erwartet:** `/MissingParts`, Typ-Reiter Lager aktiv, Trefferzahl 2 = Zahl der Karte. Die URL enthält `type=Lager`.
  3. Zurück, Reiter Glas. **Erwartet:** Karte „3 … aus 2 Bestellungen“. Der Link öffnet Typ-Reiter Glas mit 3 Treffern.
  4. In `/MissingParts` (Lager, Haken „Nur meine Werkbänke“ an) das Werkbank-Dropdown öffnen. **Erwartet:** bedienbar, nur „— Alle —“, A und B.
     C fehlt.
  5. Werkbank A wählen. **Erwartet:** nur Zeilen von A, Trefferzahl und beide Badges passend. Haken bleibt an.
  6. Haken abwählen (Klick, nicht URL). **Erwartet:** Seite lädt mit `mineOnly=false`, Haken aus, Titel „Fehlteile“, das Dropdown listet alle
     Werkbänke inkl. C. *(nur bei Rückfrage 2 = ja; sonst Befund notieren)*
  7. Haken wieder anwählen. **Erwartet:** zurück auf die eigenen Werkbänke.
- Negativfälle:
  8. URL `/MissingParts?mineOnly=true&workplaceId=<Id von C>`. **Erwartet:** Hinweis „Die gewählte Werkbank ist dir nicht zugeordnet …“, Liste leer,
     Badges 0, Dropdown auf „— Alle —“.
  9. User nur mit Glas-Recht: Karte und Link nur Glas. Kein Weg in die Lager-Liste.
  10. User ohne Werkbank-Zuordnung: Die Karte auf `/WarehouseRequisitions` fehlt (Zähler 0). `/MissingParts` zeigt den Banner wie 33.13.
  11. `/MissingPartsLager`: unverändert wie 33.14.

## Etappen (nur bei epic: true)

Nicht zutreffend (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

*Vom QA-Lauf 2026-11-07 gegen `git diff ac92a15c 747c1ced` bestätigt:* Web ja (Controller, Repository, ViewModel, Views, Changelog, `AppVersion.cs`),
Service ja (nur `IDEALAKEWMSService/AppVersion.cs` 1.80.0 → 1.81.0), keine Datei unter `*/Migrations/`, kein `SQL/`. Reihenfolge: publish aus dem Worktree →
Testsystem → Test → Merge. **Nach dem Merge** nur dann erneut aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen
zusammengeführt hat. Hinweis Bündel: Der Worktree enthält auch v1.80.0 (A2), das wegen QA-Befund B1 noch nicht Testbereit ist; ein Publish aus diesem
Worktree liefert beide aus.

- **Web-App:** ja. Betroffen sind `WarehouseRequisitionsController`, `MissingPartsController`, `(I)WarehouseRequisitionRepository`, das ViewModel, die Views
  `WarehouseRequisitions/Index`, `MissingParts/Index`, `Help/Changelog` und `AppVersion.cs`.
- **Service:** ja, **nur Versions-Bump** (`IDEALAKEWMSService/AppVersion.cs` → 1.81.0).
- **Migration:** nein.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- **Standort:** beide. Es gibt keine Standortweiche im betroffenen Code. Testbar dort, wo `LagerbestellungAktiv` gesetzt ist (an beiden DEV-Standorten
  laut QA v1.75.0). IDEAL-DEV hatte keine Fehlteile, dort nur Smoke-Test bzw. Testdaten anlegen.

## Offene Rueckfragen

1. **Karte je Bestelltyp.** Die Karte zählt künftig nur den aktiven Typ (Reiter Lager bzw. Glas), der Link trägt den Typ mit. Damit wird die frühere
   bewusste Entscheidung „Zähler typübergreifend“ (`docs/superpowers/plans/2026-07-03-glas-bestellung.md:481,858`) aufgehoben.
   Einverstanden? *Empfehlung: ja.* Die Liste hinter dem Link ist seit dem Glas-Typ typgefiltert. Eine Zahl, die man nicht anklicken und
   nachzählen kann, ist der gemeldete Fehler.
2. **Zusatzbefund Haken „Nur meine Werkbänke“** lässt sich per Klick nicht ausschalten (abgewählte Checkbox sendet nichts, Default
   `mineOnly = true`, `MissingPartsController.cs:32`, `Index.cshtml:79-80`). In dieser Spec mitbeheben? *Empfehlung: ja.* Es ist ein verstecktes
   Feld in derselben Filterkarte. Ohne den Fix ist die entsperrte Werkbank-Auswahl nur im Modus „Nur meine“ erreichbar, und der Banner-Text aus 33.13
   verweist auf einen Schalter, der nicht wirkt.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Karte „Meine Fehlteile“ zählt nur den aktiven Bestelltyp, Link mit Typ → **wie empfohlen (Gerald 2026-11-07)**
2. Haken „Nur meine Werkbänke“ ausschaltbar machen (in dieser Spec) → **wie empfohlen (Gerald 2026-11-07)**

## QA-Nachweis (qa-agent, 2026-11-07, Bündel 2026-11-08 Lauf „A + Wareneingang“) – Testbereit

**Ergebnis: keine Befunde im Diff → `status: Testbereit`.**

### 1. Build und Tests (Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, HEAD `747c1ced`)
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (vorbestehend).
- `dotnet test`: `IdealAkeWms.Tests` 2451 bestanden / 1 übersprungen / 0 Fehler (2452). `IDEALAKEWMSService.Tests` 497 / 0 / 0.

### 2. Review `git diff ac92a15c 747c1ced` (selbst, in diesem Lauf)
Geprüft: `GetShortageCountsForUserAsync` gegen `GetMissingPartsAsync` (gleiche Status-, Typ- und Werkbank-Bedingung → Karte = Liste ohne Spaltenfilter),
`WarehouseRequisitionsController.Index` (`activeType`), `MissingPartsController.Index` (gehobene `userWorkplaces`, No-Mapping-Zweig, `fremdeWerkbank`,
`WorkplaceFilter` auf null), ViewModel, `MissingParts/Index.cshtml` (Hinweis, `disabled` entfernt, verstecktes `false`-Feld hinter der Checkbox),
`WarehouseRequisitions/Index.cshtml`, Tests.
- **keine Befunde.** Model-Binding `mineOnly=true&mineOnly=false` → `true` (erster Wert) auf DEV bestätigt; abgewählt → nur `false`.
- Hinweise (kein Befund): (a) `WarehouseRequisitionRepositoryTests.cs` hat in Zeile 1 ein BOM bekommen (kosmetisch). (b) Zusatzfund vorbestehend, nicht im Diff:
  Kartenlink „N Fehlteile (wird nachgeliefert)“ ist `text-warning` (#ffc107) auf Weiß, ca. 1,6:1 – unter WCAG AA; eigener Backlog-Punkt empfohlen.
  (c) `useRelevanceFilter` (OSEON) bleibt nur gemeldet ([[2026-11-08-oseon-relevanzfilter-schaltet-sich-ab]]).
- Klassen aus dem QA-Auftrag: kein `<script>`-Wert, kein `hidden`+`d-*`, Regel Karte/Liste weiter an zwei Stellen (SQL je Methode), jetzt aber deckungsgleich und durch AK-1-Test plus DEV 113.1–113.3 belegt.

### 3. Nachweisart je AK
| AK | Nachweis | Ergebnis |
|---|---|---|
| 1 | `FehlteileKarteV181Tests.ShortageCounts_JeTyp_NullAlle_AK1` (InMemory, reine LINQ-Filter) | grün |
| 2 | `Lagerbestellungen_KarteMitAktivemTyp_AK2` | grün |
| 3 | View-Text-Test + DEV 113.1–113.3 (Links tragen `type=Lager`/`type=Glas`) | OK |
| 4, 5, 6 | Controller-Tests + DEV 113.4/113.6/113.8 | OK |
| 7 | View-Text-Test + DEV (Dropdown bedienbar) | OK |
| 8 | View-Text-Test + DEV 113.6 per **Klick** (echtes Model-Binding) | OK |
| 9 | Build/Test, Changelog im Diff | grün |
| 10 | TS-113 auf AKE-DEV, IDEAL-DEV Smoke | OK bis auf 113.9 (NICHT MÖGLICH) |

### 4. Geänderte bestehende Tests
- `WarehouseRequisitionRepositoryTests.cs:731,773` – zusätzliches Argument `null` (Typ-/Compile-Fix, Semantik „alle Typen“ wie vorher); Zeile 1 BOM (kosmetisch).

### 5. Checkliste
Keine Migration, kein SQL, keine Einstellung (ADR 0020 entfällt; `mineOnly` ist URL-Parameter), keine Audit-Felder. Version 1.81.0 in beiden `AppVersion.cs`, Anwender-Changelog im Diff,
Brain-Changelog `[[2026-11-08-v1-81-0-fehlteile-karte-je-bestelltyp]]` vorhanden, TS-113 + Querverweise 33.12/33.13 in `docs/TESTSZENARIEN.md`, Index-Zeile 113 vorhanden.
frontend-design: Hinweis als `alert-warning` mit ⚠ und Text (Farbe nicht alleiniger Träger), `aria-label` am Select; Kontrast `alert-warning` Bootstrap-Standard (AA).

### 6. DEV-Deploy und TS-113
Deploy elevated aus dem Worktree, HEAD `747c1ced`: AKE „HTTP 200, Fusszeile v1.81.0 … Fertig: AKE v1.81.0“ Exit 0; IDEAL „Fertig: IDEAL v1.81.0“ Exit 0. Fußzeile = AppVersion 1.81.0.

Testdaten AKE-DEV (User 14 „Gerald Weichbold“, eigene Werkbank A1 = 18): zusätzlich Zuordnung User 14 → G1 (26), zwei Glas-Bestellungen (Id 68, 69, Status Closed)
mit 2 + 1 Positionen `WillBeRestocked`, alles `CreatedBy='QA-TS113'`. Fremde Werkbank C = QA108-A (73, vorhandene Lager-Fehlteile). Lager-Bestand vorhanden: 14 Pos. aus 10 Bestellungen.

| Schritt | Durchführung | Ergebnis | Urteil |
|---|---|---|---|
| 113.1 | `/WarehouseRequisitions` Reiter Lager | „14 Fehlteile … aus 10 Bestellungen“, „3 Wird nicht nachgeliefert aus 3“ (Glas-3 nicht mitgezählt) | OK |
| 113.2 | Kartenlink geklickt | `/MissingParts?tab=WillBeRestocked&type=Lager&mineOnly=true`, 14 Treffer | OK |
| 113.3 | Reiter Glas, Kartenlink | „3 … aus 2 Bestellungen“ → `type=Glas`, 3 Treffer | OK |
| 113.4 | Dropdown | bedienbar, nur „— Alle —“, A1, G1 | OK |
| 113.5 | A1 gewählt (und G1 im Lager-Reiter) | A1: 14/3, nur A1-Zeilen, Haken an; G1: 0/0 | OK |
| 113.6 | Haken per Klick ab | URL `mineOnly=false`, Titel „Fehlteile“, Dropdown alle Werkbänke inkl. QA108-A | OK |
| 113.7 | Haken per Klick an | `mineOnly=true&mineOnly=false`, Titel „Meine Fehlteile“, nur A1/G1 | OK |
| 113.8 | `?type=Lager&mineOnly=true&workplaceId=73` | Hinweis „Die gewählte Werkbank ist dir nicht zugeordnet – …“, Liste leer, Badges 0, Dropdown „— Alle —“ | OK |
| 113.9 | User nur mit Glas-Recht | – | NICHT MÖGLICH (Anmeldung als anderer Windows-Benutzer im Lauf nicht möglich) |
| 113.10 | IDEAL-DEV (eigener User ohne Zuordnung) | Karte auf `/WarehouseRequisitions` fehlt; `/MissingParts` Banner 33.13; Haken per Klick ab → `mineOnly=false`, Banner weg, Titel „Fehlteile“ | OK |
| 113.11 | `/MissingPartsLager` AKE | lädt, „Einträge 1–20 von 20“ | OK |

DEV-Daten **zurückgesetzt**: Positionen, beide Bestellungen und die Zuordnung 14 → 26 gelöscht (Kontrolle: 0/0/0 mit `CreatedBy='QA-TS113'`; User 14 wieder nur auf 18).
Einzige Spur: Identity von `WarehouseRequisitions` ist um 2 weitergezählt.

### Manuelle Testliste (offen für den Menschen)
1. 113.9 mit einem Benutzer nur mit Glas-Recht (Karte und Link nur Glas, kein Weg in die Lager-Liste).
2. 113.10 an AKE mit einem Benutzer ohne Zuordnung (an IDEAL-DEV bestanden).
3. TS-113 komplett auf dem Testsystem (beide Standorte), Haken per Klick.
4. Telefonbreite der Filterkarte „Meine Fehlteile“ (Dropdown + Haken umbrechen ohne Querscrollen).
