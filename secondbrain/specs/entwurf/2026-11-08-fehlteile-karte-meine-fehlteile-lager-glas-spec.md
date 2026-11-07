---
type: spec
title: Fehlteile – Karte „Meine Fehlteile“ je Bestelltyp, Werkbank-Auswahl im Modus „Nur meine Werkbaenke“
slug: 2026-11-08-fehlteile-karte-meine-fehlteile-lager-glas-spec
status: Entwurf
created: 2026-11-07
updated: 2026-11-07
source_backlog: "[[2026-11-07-fehlteile-karte-meine-fehlteile-lager-glas]]"
bug: "[[2026-11-08-fehlteile-karte-meine-fehlteile-lager-glas-bug]]"
buendel: "[[2026-11-08-buendel-uebersicht]]"
depends_on: "[[2026-11-07-fehlteile-nur-meine-werkbaenke-trefferzahl-spec]]"
task: ""
worktree: ""
branch: ""
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
freigabe_von: ""
freigabe_am: ""
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

*Vorläufig, der QA-Lauf bestätigt gegen den echten Diff.*

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

1. Karte „Meine Fehlteile“ zählt nur den aktiven Bestelltyp, Link mit Typ →
2. Haken „Nur meine Werkbänke“ ausschaltbar machen (in dieser Spec) →
