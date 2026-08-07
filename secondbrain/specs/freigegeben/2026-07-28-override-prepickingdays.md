---
type: spec
title: ProductionWorkplace.OverridePrePickingDays wirksam machen (Variante A) oder entfernen (Variante B)
slug: 2026-07-28-override-prepickingdays
status: Gemerged
created: 2026-07-28
updated: 2026-07-28
qa:
  verdikt: TESTBEREIT
  datum: 2026-07-28
  commit: ae7ee17
  blocker: 0
merge:
  datum: 2026-07-28
  merge_commit: 0548449
  build: gruen
  tests: gruen (1024 Web + 176 Service, 0 Fehler, 1 uebersprungen)
source_backlog: "[[2026-07-28-override-prepickingdays]]"
freigabe:
  entscheidung: A
  datum: 2026-07-28
task: ""
worktree: .claude/worktrees/override-prepickingdays
branch: feature/override-prepickingdays
version: 1.27.0
affected_code:
  - IdealAkeWms/Models/ProductionWorkplace.cs
  - IdealAkeWms/Models/ViewModels/ProductionWorkplaceEditViewModel.cs
  - IdealAkeWms/Controllers/ProductionWorkplacesController.cs
  - IdealAkeWms/Views/ProductionWorkplaces/Index.cshtml
  - IdealAkeWms/Views/ProductionWorkplaces/Create.cshtml
  - IdealAkeWms/Views/ProductionWorkplaces/Edit.cshtml
  - IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs (LeitstandOrderRow)
  - IdealAkeWms/Data/Repositories/ProductionOrderRepository.cs (GetForLeitstandAsync)
  - IdealAkeWms/Controllers/ProductionOrdersController.cs
  - IdealAkeWms/Controllers/PickingLeitstandController.cs
  - IdealAkeWms/Controllers/FaWorklistController.cs
  - IdealAkeWms/Models/ViewModels/ProductionOrderListViewModel.cs (ProductionOrderListItem)
  - IdealAkeWms/Models/ViewModels/PickingLeitstandViewModel.cs (PickingLeitstandItem)
  - IdealAkeWms/Models/ViewModels/FaWorklistViewModel.cs (FaWorklistRow)
  - IdealAkeWms/Views/ProductionOrders/Index.cshtml
  - IdealAkeWms/Views/PickingLeitstand/Index.cshtml
  - IdealAkeWms/Views/FaWorklist/Index.cshtml
  - IdealAkeWms.Tests/Repositories/ProductionWorkplaceRepositoryTests.cs
  - IdealAkeWms.Tests/Controllers/ProductionOrdersControllerSlimTests.cs
  - IdealAkeWms.Tests/Controllers/PickingLeitstandControllerTests.cs
  - IdealAkeWms.Tests/Controllers/FaWorklistControllerTests.cs
open_questions: []
beantwortete_fragen:
  - "Grundsatzentscheidung A vs. B -> BEANTWORTET: A (integrieren), Freigabe-Antwort 1"
  - "Bestandsdaten Produktiv-DB -> BEANTWORTET: genau eine Werkbank (Id=1, A1, OverridePrePickingDays=7), Freigabe-Antwort 2"
  - "Tiefe der UI-Rueckmeldung -> BEANTWORTET: Pro-Zeile-Tooltip/Badge an der Werkbank-Zelle, Freigabe-Antwort 3"
  - "PrePickingDaysResolver extrahieren -> BEANTWORTET: ja, Regel an einer Codestelle, Freigabe-Antwort 4"
  - "FaWorklist/Index dieselbe Rueckmeldung -> BEANTWORTET: ja, Freigabe-Antwort 5"
  - "Report/Export auf die Spalte -> ENTFAELLT (nur fuer Variante B relevant)"
---

## Ziel / Nutzen (das Warum)

`ProductionWorkplace.OverridePrePickingDays` ("Abweichende Vorkommissioniertage") ist ein Stammdatenfeld
je Werkbank, das seit der Werkbank-Einfuehrung (Migration `20260306081711_AddProductionWorkplaces`,
v-Alt) gepflegt, gespeichert und in der Werkbank-Liste angezeigt wird — aber **von keiner
Terminberechnung gelesen wird**. Ein Admin, der pro Werkbank einen abweichenden
Vorkommissionier-Vorlauf setzt, erwartet eine Wirkung auf den Vorkommissioniertermin (und
kaskadierend auf den Beschichtungstermin) der FAs dieser Werkbank — es passiert nichts, ohne
Fehlermeldung oder Hinweis. Das ist eine stille Fehlfunktion in einem produktiven
Termin-relevanten System.

Ziel dieser Spec: die Diskrepanz zwischen Stammdaten-Erwartung und tatsaechlichem Verhalten
auflösen — entweder durch Wirksammachen des Feldes (Variante A) oder durch kontrolliertes
Entfernen inkl. UI (Variante B). Beide Varianten werden hier vollstaendig ausgearbeitet; **Variante
A wird empfohlen** (Begruendung siehe unten), die Grundsatzentscheidung selbst bleibt aber eine
offene Rueckfrage an den Menschen (Schranke 1).

## Ist-Zustand (verifiziert)

**Modell** (`IdealAkeWms/Models/ProductionWorkplace.cs:16-18`):
```csharp
[Display(Name = "Abweichende Vorkommissioniertage")]
[Range(0, 365)]
public int? OverridePrePickingDays { get; set; }
```
Nullable `int`, `Range(0, 365)`. Semantik ist bereits implizit durch die vorhandene UI festgelegt:
`null` = "Standard" (globaler Wert gilt), `HasValue` (inkl. `0`) = expliziter Override.

**Nur folgende Treffer existieren fuer `OverridePrePickingDays` im Anwendungscode** (grep-verifiziert,
Migrations-/Snapshot-Rauschen ausgenommen):

| Datei:Zeile | Zweck |
|---|---|
| `IdealAkeWms/Models/ProductionWorkplace.cs:18` | Modell-Property |
| `IdealAkeWms/Models/ViewModels/ProductionWorkplaceEditViewModel.cs:21` | Edit-Form-Property |
| `IdealAkeWms/Controllers/ProductionWorkplacesController.cs:52-54` | Spaltenfilter-Getter (`"pre-picking-days"` -> `"X Tage"` / `"Standard"`) |
| `IdealAkeWms/Controllers/ProductionWorkplacesController.cs:110` | Create: Wert aus VM uebernehmen |
| `IdealAkeWms/Controllers/ProductionWorkplacesController.cs:156` | Edit (GET): Wert in VM laden |
| `IdealAkeWms/Controllers/ProductionWorkplacesController.cs:197` | Edit (POST): Wert speichern |
| `IdealAkeWms/Views/ProductionWorkplaces/Index.cshtml:29,61-68` | Listen-Spalte "Abw. Vorkommissioniertage" (Badge/„Standard") |
| `IdealAkeWms/Views/ProductionWorkplaces/Create.cshtml`, `Edit.cshtml` | Formularfeld |
| `IdealAkeWms.Tests/Repositories/ProductionWorkplaceRepositoryTests.cs:13-134` | reine CRUD-Tests (Speichern/Laden/Nullen) |

**Kein einziger Treffer** in `BusinessDayService`, `CoatingDateCalculator`,
`ProductionOrdersController`, `PickingLeitstandController`, `FaWorklistController`,
`PickingController` — den vier Stellen, die den Vorkommissioniertermin berechnen bzw. anzeigen.
Diese lesen ausschliesslich den **globalen** Settings-Wert `VorkommissionierTage` (Default `1`,
`AppSettingRepository.GetIntValueAsync("VorkommissionierTage", 1)`):

| Stelle | Global gelesen | Vorkommissionierung berechnet? |
|---|---|---|
| `ProductionOrdersController.cs:88,132-133` | ja | ja, ueber `LeitstandOrderRow` (Projection ohne Werkbank-Override) |
| `PickingLeitstandController.cs:80,137-138` | ja | ja, dieselbe `LeitstandOrderRow`-Projection |
| `FaWorklistController.cs:160,213-214` | ja | ja, ueber volle `ProductionOrder`-Entitaeten (`GetAllOrderedAsync`, `Include(o => o.ProductionWorkplace)`) |
| `PickingController.cs:125-153` | ja (nur `KommissionierTage`) | **nein** — dort wird nur `KommissionierTermin` berechnet, `VorkommissionierTermin` kommt in dieser Liste gar nicht vor |

Kaskadeneffekt: `CoatingDateCalculator.Compute` (`IdealAkeWms/Services/CoatingDateCalculator.cs:11-25`)
nimmt den bereits berechneten `VorkommissionierTermin` entgegen und zieht `BeschichtungTage` ab. Eine
Korrektur des Vorkommissioniertermins wirkt sich also automatisch — ohne Code-Aenderung in
`CoatingDateCalculator` selbst — auf den Beschichtungstermin aus.

UI-Tooltip-Fallstrick: `Views/ProductionOrders/Index.cshtml:87` und
`Views/PickingLeitstand/Index.cshtml:119` beschriften die Spalte "BG-Termin" heute mit einem
**fixen** Tooltip `"Kommissioniertermin - @Model.VorkommissionierTage Arbeitstag(e)"` — dieser Text
gilt fuer alle Zeilen gleich, weil aktuell wirklich fuer alle Zeilen derselbe globale Wert gilt.
Bei Variante A wird das pro Zeile ggf. falsch (siehe technischer Loesungsentwurf).

`secondbrain/architektur/fallstricke.md` (Abschnitt „`ProductionWorkplace.OverridePrePickingDays`
ist wirkungslos") und `secondbrain/feature-map.md` (Zeile 141, Abschnitt „Offen / nicht gemerged")
dokumentieren den Missstand bereits als bekannten offenen Punkt.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope (beide Varianten):**
- Entscheidung, wie `ProductionWorkplace.OverridePrePickingDays` behandelt wird.
- Bei Variante A: Integration in alle Stellen, die `VorkommissionierTage` lesen (siehe Tabelle
  oben), inkl. kaskadierender Wirkung auf `BeschichtungTermin`.
- Bei Variante B: vollstaendiger Rueckbau (Modell, Migration mit Drop, ViewModel, Views,
  Spaltenfilter, Tests).
- Anpassung/Ergaenzung der betroffenen Tests, `docs/TESTSZENARIEN.md`,
  `secondbrain/tests/testszenarien-index.md`, Brain-Changelog, `feature-map.md`, `fallstricke.md`
  (Eintrag aufloesen bzw. durch neuen Eintrag ersetzen), Anwender-Changelog + `AppVersion.cs`.

**Out-of-Scope:**
- `PickingController` (Kommissionierliste ohne Vorkommissionierungs-Spalte) — dort existiert kein
  Vorkommissionier-Termin, daher keine Aenderung noetig, unabhaengig von der Variante.
- Refactoring der bestehenden Code-Duplikation (dieselbe Terminberechnungslogik ist in drei
  Controllern separat implementiert) — das ist ein bekanntes, unabhaengiges Aufraeumthema und wird
  hier **nicht** mit erledigt, um den Diff klein zu halten. Ggf. als Folgeaufgabe in
  `secondbrain/aufgaben/` vermerken.
- Aenderung des globalen Settings `VorkommissionierTage` selbst (Name, Default, Pflegeort) — bleibt
  unveraendert.
- Neue Rolle/neuer Zugriffsfilter — es gibt keinen Grund, den bestehenden
  `RequireMasterDataAccess`/`RequireMasterDataReadAccess`-Split fuer `ProductionWorkplacesController`
  anzufassen.

## Fachliche Anforderungen

### Gemeinsam (unabhaengig von der Variante)
1. Nach Umsetzung darf es keine Stammdaten-Eingabemoeglichkeit mehr geben, die wirkungslos ist,
   ohne dass die UI das explizit kennzeichnet.
2. Die Entscheidung muss in `secondbrain/architektur/fallstricke.md` aufgeloest werden (Eintrag
   „ist wirkungslos" entfernen/ersetzen durch einen neuen Eintrag, der die getroffene Loesung und
   ihre Prioritaetsregel dokumentiert) und in `secondbrain/feature-map.md` Zeile 141 (Status von
   „offen" auf „Gemergt" bzw. Verweis auf diese Spec) aktualisiert werden.

### Variante A — Feld in die Terminberechnung integrieren

3. Fuer jede FA-Zeile mit zugeordneter Werkbank (`ProductionOrder.ProductionWorkplaceId`) gilt bei
   der Berechnung des Vorkommissioniertermins folgende **Prioritaetsregel**:
   - Ist `ProductionWorkplace.OverridePrePickingDays` **gesetzt** (`HasValue == true`, inkl. `0`),
     wird dieser Wert als Vorkommissioniertage verwendet.
   - Ist die Werkbank `null` (FA ohne Werkbank-Zuordnung) **oder** ist
     `OverridePrePickingDays == null`, gilt weiterhin der globale Wert `VorkommissionierTage`.
   - `0` ist ein gueltiger, expliziter Override-Wert (Vorkommissionierung fällt auf denselben Tag
     wie der Kommissioniertermin) und **kein** Synonym fuer „Standard verwenden" — das ist bereits
     die von der bestehenden UI verwendete Semantik (`ColumnMap["pre-picking-days"]` unterscheidet
     strikt nach `HasValue`, nicht nach `> 0`).
4. Die Prioritaetsregel gilt konsistent an **allen drei** Stellen, die den Vorkommissioniertermin
   berechnen: `ProductionOrdersController.Index` (FA-Abarbeitungsliste),
   `PickingLeitstandController.Index` (Leitstand), `FaWorklistController.Index`
   (FA-Abarbeitungsliste je Arbeitsgang). Ergebnis fuer dieselbe FA-Nummer muss an allen drei
   Stellen identisch sein.
5. Der kaskadierende Effekt auf `BeschichtungTermin` (ueber `CoatingDateCalculator`) ist
   **gewollt** und muss nicht separat abgeschaltet werden — ein fruehrer/spaeterer
   Vorkommissioniertermin verschiebt den Beschichtungstermin automatisch mit.
6. Die betroffenen Listen muessen erkennbar machen, wenn fuer eine Zeile ein Werkbank-Override
   statt des Standardwerts gilt (Rueckmeldung — das war der urspruengliche Kritikpunkt im
   Backlog-Item: „es passiert nichts, ohne Rueckmeldung"). Minimalauspraegung: Tooltip an der
   Werkbank-Zelle (`title="Vorkommissioniertage: Override X (Standard Y)"`), wenn ein Override
   aktiv ist; keine sichtbare Aenderung, wenn der Standard gilt.
7. Die Spaltenkopf-Tooltips in `Views/ProductionOrders/Index.cshtml:87` und
   `Views/PickingLeitstand/Index.cshtml:119` sind nicht mehr fuer alle Zeilen korrekt und muessen
   auf einen generischen Hinweis umgestellt werden (z. B. „Kommissioniertermin - Vorkommissioniertage
   (Standard @Model.VorkommissionierTage Arbeitstag(e), werkbankabhaengig abweichend)").
8. Bestehende CRUD-Tests fuer `OverridePrePickingDays`
   (`ProductionWorkplaceRepositoryTests.cs`) bleiben gueltig und werden um Tests fuer die neue
   Prioritaetsregel in `BusinessDayService`-Konsumenten ergaenzt (siehe Testszenarien).

### Variante B — Feld entfernen

9. Modell-Property, ViewModel-Property, Formularfelder (Create/Edit), Listen-Spalte (inkl.
   Spaltenfilter-Eintrag `"pre-picking-days"`) werden vollstaendig entfernt.
10. Vor dem Drop: Pruefen, ob in der Produktiv-DB Zeilen mit `OverridePrePickingDays IS NOT NULL`
    existieren (siehe offene Rueckfrage) — falls ja, ist das eine **daten-destruktive Migration**
    (ADR 0004) und muss als solche gekennzeichnet werden inkl. „DB-Backup vor Deploy".
11. Bestehende CRUD-Tests (`ProductionWorkplaceRepositoryTests.cs`, betroffene Faelle) werden
    entfernt bzw. angepasst.
12. Der Fallstricke-Eintrag wird durch eine kurze Notiz ersetzt: Feld existierte, war wirkungslos,
    wurde am `<Datum>` entfernt statt integriert — mit Begruendung, damit die Historie
    nachvollziehbar bleibt (nicht einfach loeschen, additiv schreiben).

## Empfehlung

**Variante A (integrieren) wird empfohlen**, mit folgender Begruendung:

- Das Feld ist seit der ersten Werkbank-Migration (`20260306081711_AddProductionWorkplaces`)
  vorhanden, wurde über >60 nachfolgende Migrationen hinweg nie zur Entfernung vorgeschlagen, und
  ist vollstaendig in Stammdaten-CRUD, Anzeige und Spaltenfilter ausgebaut — das deutet auf einen
  bewussten fachlichen Bedarf hin (unterschiedliche Werkbaenke koennen unterschiedliche
  Vorkommissionier-Vorlaufzeiten brauchen, z. B. je nach Entfernung zum Lager oder
  Ruestaufwand), nicht auf einen Copy-Paste-Rest.
- Es existiert im selben Modell bereits ein strukturell identisches, **funktionierendes**
  Override-Muster: `ProductionWorkplace.BdeDefaultArbeitsgang` überschreibt nachweislich das
  globale BDE-Default-Arbeitsgang-Setting (siehe `docs/TESTSZENARIEN.md` TS-9.3 „Werkbank-Default-
  Arbeitsgang ueberschreibt globales Setting"). Variante A uebertraegt exakt dasselbe,
  bereits akzeptierte Muster auf `OverridePrePickingDays` — geringes Konzeptrisiko.
- Variante B vernichtet stillschweigend bereits erfasste Admin-Konfiguration (falls in Produktion
  gesetzt) und loest den eigentlichen fachlichen Wunsch nicht, sondern verschiebt ihn nur in die
  Zukunft als erneuten Feature-Wunsch.
- Der Umsetzungsaufwand fuer A ist ueberschaubar (keine Migration, drei Controller mit bereits
  bekannter, gleichartiger Berechnungslogik, ein DTO-Feld ergaenzen) im Vergleich zum Rueckbau in B
  (Migration, Formular-/Spaltenfilter-Rueckbau, Datenverlust-Pruefung).

**Aber:** Die Grundsatzentscheidung A vs. B ist eine fachliche/Produkt-Entscheidung, die nur der
Mensch treffen kann (siehe „Offene Rueckfragen" — CLAUDE.md verbietet dem Agenten, hier zu raten).
Diese Spec liefert deshalb fuer **A** die vollstaendigen Umsetzungsschritte und fuer **B** eine
vollstaendige, aber knappere Alternativ-Ausarbeitung, damit nach der Freigabe-Entscheidung ohne
weitere Spec-Runde umgesetzt werden kann.

## Technischer Loesungsentwurf (Variante A, empfohlen)

Muster: kein neues Architektur-Muster, sondern Anwendung des bereits etablierten
„Werkbank-Override-gewinnt-wenn-gesetzt"-Musters (siehe `BdeDefaultArbeitsgang`) auf eine zweite
Stelle. Repository-Pattern bleibt gewahrt (keine neue Roh-SQL, nur ein zusaetzliches projiziertes
Feld in einer bestehenden EF-Projection).

1. **`LeitstandOrderRow`** (`IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs:5-25`) um
   ein Feld erweitern, z. B. `int? WorkplaceOverridePrePickingDays = null` (mit Default fuer
   Konstruktor-Kompatibilitaet, wie es das bestehende Pattern fuer die FA-Zusatzinfos-Felder
   bereits vorschreibt).
2. **`ProductionOrderRepository.GetForLeitstandAsync`** (`ProductionOrderRepository.cs:66-84`): in
   der `.Select(...)`-Projection zusaetzlich
   `o.ProductionWorkplace != null ? o.ProductionWorkplace.OverridePrePickingDays : null` mappen.
   Diese eine Repository-Methode wird von **beiden** `ProductionOrdersController` und
   `PickingLeitstandController` genutzt — eine Aenderung deckt beide Controller ab.
3. In **`ProductionOrdersController.Index`** (Zeile ~128-133) und **`PickingLeitstandController.Index`**
   (Zeile ~133-138) je einen Helfer/Inline-Ausdruck einfuehren:
   ```csharp
   var effectiveVorkommissionierTage = o.WorkplaceOverridePrePickingDays ?? vorkommissionierTage;
   item.VorkommissionierTermin = _businessDayService.SubtractBusinessDays(
       item.KommissionierTermin.Value, effectiveVorkommissionierTage, holidays);
   ```
   Erwaegen, diesen einzeiligen Ausdruck als kleine statische Hilfsmethode
   (z. B. `PrePickingDaysResolver.Resolve(int? overrideValue, int globalValue)`) zu extrahieren,
   damit die Prioritaetsregel nur an **einer** Codestelle steht und von allen drei Controllern
   aufgerufen wird (reduziert das Duplikationsrisiko, ohne den bestehenden groesseren
   Duplikations-Fallstrick der drei Controller anzufassen).
4. In **`FaWorklistController.Index`** (Zeile ~213-214): `order.ProductionWorkplace` ist bereits
   per `Include` geladen (`GetAllOrderedAsync`), daher genuegt
   `order.ProductionWorkplace?.OverridePrePickingDays ?? vorkommissionierTage` inline bzw. Aufruf
   desselben Resolvers wie in Punkt 3.
5. **ViewModels** `ProductionOrderListItem`, `PickingLeitstandItem`, `FaWorklistRow`: optional um
   ein Flag/Wert ergaenzen, das die UI fuer die Rueckmeldung (Anforderung 6) braucht — z. B.
   `int? EffectivePrePickingDaysOverride` (nur gesetzt, wenn ein Override tatsaechlich griff),
   damit die View ohne weitere Berechnung entscheiden kann, ob ein Tooltip/Badge angezeigt wird.
6. **Views** `ProductionOrders/Index.cshtml`, `PickingLeitstand/Index.cshtml`: Werkbank-Zelle
   erhaelt bedingtes `title`-Attribut, wenn `EffectivePrePickingDaysOverride.HasValue`. Header-
   Tooltip fuer "BG-Termin" auf generischen Text umstellen (Anforderung 7). `FaWorklist/Index.cshtml`
   hat aktuell keinen entsprechenden Tooltip an der Werkbank-Spalte — pruefen, ob dort analog
   ergaenzt wird oder aus Konsistenzgruenden bewusst weggelassen wird (siehe offene Rueckfrage zur
   Tiefe der Rueckmeldung).
7. Kein Aenderungsbedarf an `BusinessDayService`, `CoatingDateCalculator`, `PickingController`,
   Modell, Migration oder SQL-Skripten — reine Lese-/Verrechnungslogik in bestehenden Controllern
   und einer bestehenden Projection.

## Technischer Loesungsentwurf (Variante B, Alternative)

1. `IdealAkeWms/Models/ProductionWorkplace.cs`: Property `OverridePrePickingDays` entfernen.
2. `IdealAkeWms/Models/ViewModels/ProductionWorkplaceEditViewModel.cs`: Property entfernen.
3. `IdealAkeWms/Controllers/ProductionWorkplacesController.cs`: `ColumnMap`-Eintrag
   `"pre-picking-days"` entfernen; Zuweisungen in `Create`/`Edit` (GET+POST) entfernen.
4. `IdealAkeWms/Views/ProductionWorkplaces/Index.cshtml`: Spalte "Abw. Vorkommissioniertage"
   (Header `<th data-col-key="pre-picking-days">` + Zelle) entfernen; `colspan` in der
   "Keine Werkbaenke"-Zeile von 6 auf 5 anpassen.
5. `IdealAkeWms/Views/ProductionWorkplaces/Create.cshtml`, `Edit.cshtml`: Formularfeld entfernen.
6. Model → `dotnet ef migrations add DropProductionWorkplaceOverridePrePickingDays` (Spalten-Drop).
7. `SQL/82_DropProductionWorkplaceOverridePrePickingDays.sql` (naechste freie Nummer nach `81_`)
   mit `OBJECT_ID`/`COL_LENGTH`-Guard, DDL (`ALTER TABLE ... DROP COLUMN`) in eigenem Batch, danach
   `__EFMigrationsHistory`-Insert in separatem Batch. **Als daten-destruktiv kennzeichnen** und
   „DB-Backup vor Deploy" im Skriptkopf dokumentieren (analog Migration 65/76, siehe ADR 0004).
8. `SQL/00_FreshInstall.sql`: Spalte an der Stelle der `ProductionWorkplaces`-Tabelle entfernen
   **und** die neue `MigrationId` im History-INSERT-Block ergaenzen (zwei Stellen, ADR 0004).
9. `SQL/AgentJobs/`: pruefen, ob ein Job `ProductionWorkplace`-Spalten mergt/liest (aktueller
   Rechercheeindruck: nein, Werkbaenke sind reine WMS-Stammdaten ohne Sage-Sync) — im selben
   Wartungsfenster nachziehen, falls doch.
10. `IdealAkeWms.Tests/Repositories/ProductionWorkplaceRepositoryTests.cs`: Testfaelle, die
    `OverridePrePickingDays` pruefen (`CreateWorkplace`-Helper-Parameter, mehrere `[Fact]`s),
    entfernen bzw. Helper-Signatur anpassen.

## Migrations-/SQL-Auswirkungen

**Variante A:** keine. Es wird kein Schema geaendert, keine neue Migration, kein neues SQL-Skript,
`00_FreshInstall.sql` bleibt unveraendert. Reine Verhaltensaenderung in Anwendungslogik (Controller
+ Projection).

**Variante B:** siehe „Technischer Loesungsentwurf (Variante B)", Punkte 6-9 — vollstaendiger
Migrations-Workflow nach ADR 0004 (Model → `dotnet ef migrations add` → `SQL/82_*.sql` mit
`OBJECT_ID`-Guard, DDL in eigenem Batch, `__EFMigrationsHistory`-Insert in separatem Batch →
`SQL/00_FreshInstall.sql` an **beiden** Stellen: Tabellendefinition und `MigrationId`). Vor dem
Drop **zwingend** pruefen (siehe offene Rueckfrage), ob in Produktion bereits Werte gesetzt sind —
falls ja, ist die Migration daten-destruktiv und muss so gekennzeichnet werden inkl. „DB-Backup vor
Deploy". `SQL/AgentJobs/` pruefen (voraussichtlich nicht betroffen, da `ProductionWorkplace` kein
Sage-Sync-Ziel ist — bitte im Zuge der Umsetzung nochmal verifizieren).

## Audit-Feld-Auswirkungen

**Variante A:** keine neuen oder geaenderten Audit-Felder. `ProductionWorkplace` bleibt
`AuditableEntity`; Speicherpfad (`ModifiedAt`/`ModifiedBy`/`ModifiedByWindows` beim Edit, gesetzt in
`ProductionWorkplacesController.Edit(POST)` Zeilen 203-205) ist bereits korrekt implementiert und
bleibt unveraendert, da nur das *Lesen* des Feldes an anderer Stelle ergaenzt wird, nicht das
*Schreiben*.

**Variante B:** Entfernen einer Property beruehrt keine Audit-Felder von `ProductionWorkplace`
(bleibt `AuditableEntity`); die Migration selbst schreibt keine fachlichen Audit-Werte, nur Schema.

## Auswirkung auf Rollen/Zugriffsfilter

Keine Aenderung in beiden Varianten. `ProductionWorkplacesController` bleibt
`[RequireMasterDataReadAccess]` (Class-Level) mit `[RequireMasterDataAccess]` auf den
Edit-Actions (bestehender Read/Edit-Split, siehe `secondbrain/codebase/controller.md` Zeilen 50-51).
`ProductionOrdersController`/`PickingLeitstandController`/`FaWorklistController` behalten ihre
bestehenden Zugriffsfilter (`RequirePickingOrTrackingOrLeitstandAccess` u. ae.) unveraendert — es
wird keine neue Berechtigung eingefuehrt, nur eine bestehende Werkbank-Eigenschaft in eine
bestehende, bereits zugriffsgeschuetzte Berechnung einbezogen.

## Listen-View-Pattern-Pflichten

Es wird **keine neue Tabellen-View** eingefuehrt — `ProductionWorkplaces/Index`,
`ProductionOrders/Index`, `PickingLeitstand/Index` und `FaWorklist/Index` sind bestehende,
bereits Pattern-konforme Listen (Pagination, Filterkarte wo vorhanden, Server-Side-Spaltenfilter,
`data-col-key` je `<th>`). Variante A aendert **keine** Spalte/Pagination/Filterlogik strukturell,
sondern nur:
- den Inhalt einer bereits berechneten Datumsspalte (`VorkommissionierTermin`/„BG-Termin", weiterhin
  server-seitig in C# nach der Termin-Berechnung gefiltert, ADR 0005) und
- optional ein zusaetzliches `title`-Attribut an einer bestehenden Zelle (kein neuer `<th
  data-col-key>`, also keine neue Spaltenfilter-Pflicht).

Variante B entfernt eine bestehende Spalte samt ihres `data-col-key="pre-picking-days"`-Eintrags in
`Views/ProductionWorkplaces/Index.cshtml` vollstaendig — das ist konsistent mit ADR 0005, da eine
entfernte Spalte keinen verwaisten Filter-Header hinterlassen darf.

## Auswirkung auf Hintergrund-Services

Keine. Weder `BusinessDayService` noch ein Windows-Service-Worker lesen
`OverridePrePickingDays` oder `VorkommissionierTage` heute (verifiziert: keine Treffer in
`IDEALAKEWMSService`). Terminberechnung ist reine Web-App-Anzeigelogik zur Anfragezeit, kein
persistiertes/gejobtes Datum. Beide Varianten bleiben ohne Beruehrung von `ISyncLogger`,
`SyncLogServices.All` oder `ServiceSettingDefinitions`.

## Akzeptanzkriterien

### Variante A
1. Setzt ein Admin fuer Werkbank `WB-01` `OverridePrePickingDays = 3` (globaler Wert
   `VorkommissionierTage = 1`), zeigt die FA-Abarbeitungsliste (`ProductionOrders/Index`) fuer eine
   FA an `WB-01` einen `VorkommissionierTermin`, der `KommissionierTermin - 3 Arbeitstage`
   entspricht (nicht `-1`).
2. Dieselbe FA zeigt in `PickingLeitstand/Index` und in `FaWorklist/Index` (je gewaehltem
   Arbeitsgang) exakt denselben `VorkommissionierTermin` wie in Kriterium 1.
3. Fuer eine FA an einer Werkbank **ohne** gesetztes `OverridePrePickingDays` (bzw. ohne
   Werkbank-Zuordnung) bleibt der `VorkommissionierTermin` unveraendert gegenueber dem
   Ist-Zustand (globaler Wert `VorkommissionierTage`).
4. Setzt der Admin `OverridePrePickingDays = 0` fuer eine Werkbank, wird `0` als expliziter
   Override verwendet (nicht als „kein Override" interpretiert) — `VorkommissionierTermin ==
   KommissionierTermin` fuer FAs dieser Werkbank.
5. Hat eine FA mit aktivem Beschichtungs-Feature (`LackierteilKategorieName` gesetzt) und
   `HasCoatingParts = true` eine Werkbank mit Override, verschiebt sich `BeschichtungTermin`
   nachweislich mit dem neuen `VorkommissionierTermin` (Kaskade ueber `CoatingDateCalculator`).
6. Die Werkbank-Zelle (oder ein aequivalentes UI-Element) einer FA mit aktivem Override zeigt
   erkennbar (Tooltip/Badge) den abweichenden Wert; FAs ohne Override zeigen keine Aenderung
   gegenueber dem Ist-Zustand.
7. `dotnet build` und `dotnet test` sind gruen; neue/angepasste Unit-Tests decken die
   Prioritaetsregel „Override gewinnt, sonst global" isoliert ab (ohne DB, reine
   Berechnungslogik).
8. Kein Schema-Diff: `git diff` zeigt keine neue Migration, kein neues SQL-Skript, kein
   `PendingModelChangesWarning` beim App-Start.

### Variante B
9. `ProductionWorkplace.OverridePrePickingDays` existiert nach der Migration weder im Modell noch
   in der Datenbank (`sys.columns` zeigt die Spalte nicht mehr).
10. `ProductionWorkplaces/Index`, `Create`, `Edit` zeigen keinerlei Bezug zu
    „Vorkommissioniertage" mehr; kein toter Spaltenfilter-Key.
11. Eine frische Installation via `SQL/00_FreshInstall.sql` erzeugt die `ProductionWorkplaces`-
    Tabelle bereits ohne die Spalte, und `__EFMigrationsHistory` enthaelt die neue `MigrationId`.
12. Existierten vor dem Deploy Zeilen mit `OverridePrePickingDays IS NOT NULL`, ist im
    Deploy-Hinweis dokumentiert, dass diese Werte unwiederbringlich verloren gehen, und ein
    DB-Backup wurde vor dem Deploy angelegt.
13. `dotnet build` und `dotnet test` sind gruen; die entfernten CRUD-Testfaelle sind aus der
    Testsuite entfernt, keine toten Referenzen.

## Test-Szenarien

Neues/erweitertes Kapitel in `docs/TESTSZENARIEN.md`: Kapitel 9 „BDE Phase 2.1 —
Werkbank-Erweiterungen" ist der fachlich naheliegende Ort (dort steht bereits das analoge
Override-Szenario TS-9.3 „Werkbank-Default-Arbeitsgang ueberschreibt globales Setting"). Neue
Nummerierung TS-9.6 ff. anhaengen; Index in `secondbrain/tests/testszenarien-index.md` nachziehen.

**Skizze fuer Variante A (bei Freigabe von A auszuformulieren, Vorbedingungen/Schritte/Erwartung/
Negativfall vollstaendig gemaess Testszenarien-Pflicht):**

- **TS-9.6 — Werkbank-Override ueberschreibt globale Vorkommissioniertage (FA-Liste).**
  Vorbedingung: `VorkommissionierTage = 1` (global), Werkbank `WB-OVERRIDE` mit
  `OverridePrePickingDays = 3`, FA `FA-TEST-01` mit `ProductionWorkplaceId = WB-OVERRIDE` und
  `ProductionDate` an einem bekannten Werktag. Schritte: FA-Abarbeitungsliste oeffnen, Spalte
  "BG-Termin" fuer `FA-TEST-01` ablesen. Erwartung: Termin = `KommissionierTermin` minus 3
  Arbeitstage (nicht 1). Negativfall: FA `FA-TEST-02` an Werkbank ohne Override zeigt weiterhin
  `KommissionierTermin - 1 Arbeitstag`.
- **TS-9.7 — Override konsistent ueber Leitstand und FA-Abarbeitungsliste je AG.** Vorbedingung wie
  TS-9.6. Schritte: denselben FA in `Leitstand` und in `FA-Abarbeitungsliste je AG` oeffnen.
  Erwartung: identischer "BG-Termin"-Wert an beiden Stellen.
- **TS-9.8 — Override = 0 wird als expliziter Wert behandelt, nicht als „kein Override".**
  Vorbedingung: Werkbank `WB-ZERO` mit `OverridePrePickingDays = 0`. Schritte: FA an `WB-ZERO`
  ansehen. Erwartung: `VorkommissionierTermin == KommissionierTermin`. Negativfall: Werkbank ohne
  gesetzten Wert (`null`) verhaelt sich weiterhin wie der globale Standard.
- **TS-9.9 — Override wirkt kaskadierend auf den Beschichtungstermin.** Vorbedingung:
  Beschichtungs-Feature aktiv, FA mit `HasCoatingParts = true` an Werkbank mit Override.
  Erwartung: `Beschichtungstermin` verschiebt sich exakt um die Differenz zwischen Override und
  globalem Wert gegenueber einer vergleichbaren FA ohne Override.
- **TS-9.10 — Rueckmeldung in der UI.** Schritte: Werkbank-Zelle/Tooltip einer FA mit aktivem
  Override pruefen. Erwartung: sichtbarer Hinweis auf den abweichenden Wert. Negativfall: FA ohne
  Override zeigt keinen Hinweis.

**Skizze fuer Variante B (bei Freigabe von B auszuformulieren):**

- **TS-9.6 (B) — Feld ist vollstaendig entfernt.** Schritte: Werkbank anlegen/bearbeiten.
  Erwartung: kein Formularfeld, keine Listen-Spalte, kein Spaltenfilter „Abw.
  Vorkommissioniertage" mehr vorhanden. Negativfall: alte, gespeicherte Deep-Links mit
  `?colf_pre-picking-days=...` fuehren zu keinem Fehler (Filter wird schlicht ignoriert).
- **TS-9.7 (B) — Migration auf einer Instanz mit bereits gesetzten Werten.** Vorbedingung: DB-Backup
  vorhanden, mindestens eine Werkbank mit `OverridePrePickingDays IS NOT NULL`. Schritte: Migration
  einspielen. Erwartung: Spalte ist weg, App startet fehlerfrei, keine Restdaten abrufbar (bewusster
  Datenverlust, im Deploy-Hinweis dokumentiert).

## Offene Rueckfragen

1. **Grundsatzentscheidung A vs. B ist noch nicht getroffen.** Diese Spec empfiehlt A mit
   Begruendung (siehe Abschnitt „Empfehlung"), aber die endgueltige Wahl obliegt dem Menschen bei
   Schranke 1 (Entwurf → Freigegeben). Ohne diese Entscheidung darf **nicht** umgesetzt werden.
2. **Bestandsdaten-Pruefung fehlt.** Es wurde nicht recherchiert (kein DB-Zugriff aus dieser
   Spec-Erstellung heraus), ob in der Produktiv-DB bereits Werkbaenke mit gesetztem
   `OverridePrePickingDays <> NULL` existieren. Das ist fuer beide Varianten relevant: fuer B als
   Datenverlust-Risiko, fuer A als Hinweis auf den tatsaechlichen „Blast Radius" (wie viele FAs
   waeren sofort von einer Terminverschiebung betroffen, sobald das Feld live wirkt?). Vor
   Freigabe sollte ein Mensch mit DB-Zugriff `SELECT Id, Name, OverridePrePickingDays FROM
   ProductionWorkplaces WHERE OverridePrePickingDays IS NOT NULL` gegen die Produktiv-DB laufen
   lassen.
3. **Tiefe der UI-Rueckmeldung bei Variante A.** Reicht ein generischer Tooltip am Spaltenkopf
   ("BG-Termin", Hinweis auf moegliche Werkbank-Abweichung) oder wird eine echte Pro-Zeile-
   Rueckmeldung (Tooltip/Badge an der Werkbank-Zelle mit konkretem Override-Wert, wie in
   Anforderung 6/Akzeptanzkriterium 6 vorgeschlagen) fachlich gefordert? Letzteres ist aufwendiger
   (zusaetzliches Feld in drei ViewModels + drei Views), Ersteres ist schneller, aber weniger
   transparent — genau der Kritikpunkt, der zu diesem Backlog-Item gefuehrt hat („ohne
   Rueckmeldung").
4. **Soll `FaWorklist/Index.cshtml`** (die dritte betroffene Liste) dieselbe Pro-Zeile-Rueckmeldung
   bekommen wie `ProductionOrders/Index` und `PickingLeitstand/Index`, oder genuegt dort die
   korrekte Zahl in der bestehenden `VorkommissionierTermin`-Spalte ohne zusaetzlichen Tooltip
   (diese View hat aktuell keinen Termin-Tooltip am Spaltenkopf, anders als die anderen beiden)?
5. **Bei Variante A: Soll die vorgeschlagene Extraktion einer gemeinsamen
   `PrePickingDaysResolver`-Hilfsmethode** tatsaechlich umgesetzt werden, oder ist die Duplikation
   der Ein-Zeilen-Fallback-Logik in drei Controllern (analog zur bestehenden Duplikation von
   `kommissionierTage`/`vorkommissionierTage`-Reads) im Sinne des „minimalen Diffs" akzeptabel?
6. **Bei Variante B: Betrifft irgendein Report/Export/Sage-Feld** `OverridePrePickingDays`
   ausserhalb des hier recherchierten Anwendungscodes (z. B. ein manuelles SSRS-Reporting oder eine
   Power-BI-Abfrage direkt auf die Tabelle)? Das konnte aus dem Repository heraus nicht
   ausgeschlossen werden und sollte vor dem Drop mit den fachlichen Ansprechpartnern abgeklaert
   werden.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  Startklar erst, wenn HIER jede Frage beantwortet ist, im Frontmatter
  freigabe.entscheidung A oder B steht, status: Freigegeben gesetzt ist UND
  die Datei in specs/freigegeben/ liegt. Der Dev-Lauf liest diesen Block als
  Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  (Hinweis: Frontmatter-Feld 'freigabe:' fehlt in dieser vor dem Template
  erzeugten Spec - beim Freigeben von Hand ergaenzen oder einfach hier unten
  die Entscheidung in Frage 1 eintragen; der Dev-Lauf liest beides.)
-->

1. **A (integrieren) vs. B (entfernen):** → A
2. **Bestandsdaten in Produktiv-DB** (Ergebnis von `SELECT Id, Name, OverridePrePickingDays FROM ProductionWorkplaces WHERE OverridePrePickingDays IS NOT NULL`): → Id	Name	OverridePrePickingDays
1	A1	7
3. **Tooltip-Tiefe** (generischer Spaltenkopf-Hinweis ODER Pro-Zeile-Tooltip an der Werkbank-Zelle): → tooltip pro zeile wäre hilfreich.
4. **PrePickingDaysResolver extrahieren?** (ja = Regel an einer Codestelle / nein = minimaler Diff): → ist doch eigentlich frage 5? -> gleich wie überall
5. **FaWorklist/Index dieselbe Rueckmeldung wie die anderen beiden Listen?** → ist doch eigentlich frage 4 -> ja, tatsächlich umsetzen
6. **Nur bei Variante B** (Report/Export/SSRS/Power-BI auf die Spalte?): →

---

## Evidenz (QA-Lauf 2026-07-28, Schranke 2 vorbereitet)

**Umgesetzte Variante:** A (integrieren). **Version:** 1.27.0.
**Worktree:** `.claude/worktrees/override-prepickingdays` · **Branch:** `feature/override-prepickingdays`
**Stand:** Commit `ae7ee17` „wip: override-prepickingdays Variante A" (25 Dateien).
**Nicht gemergt, nicht gepusht** — `main` unberuehrt.

### Build

`dotnet build IdealAkeWms.slnx`

```
Der Buildvorgang wurde erfolgreich ausgefuehrt.
    0 Fehler
```

Warnungen ausschliesslich Bestand ohne Feature-Bezug: `NU1902` (MailKit/MimeKit 4.12.0
Advisories) und `CS8602` in `IdealAkeWms/Controllers/TrackingController.cs:261`.

### Tests

`dotnet test` (beide Testprojekte, zweifach ausgefuehrt — Umsetzungslauf und unabhaengiger QA-Lauf,
identisches Ergebnis):

```
IdealAkeWms.Tests.dll (net10.0)
  Bestanden! : Fehler: 0, erfolgreich: 1024, uebersprungen: 1, gesamt: 1025

IDEALAKEWMSService.Tests.dll (net10.0)
  Bestanden! : Fehler: 0, erfolgreich:  176, uebersprungen: 0, gesamt:  176
```

Summe **1200 bestanden / 0 Fehler / 1 uebersprungen**. Der Skip ist
`IdealAkeWms.Tests.Integration.ProductionOrderEagerCreateAgentJobTests` — ein vorbestehender
DB-Integrationstest ohne Bezug zu dieser Aenderung.

### Akzeptanzkriterien Variante A — Nachweis

| # | Kriterium | Status | Nachweis |
|---|---|---|---|
| 1 | Override schlaegt global in FA-Liste | erfuellt | `ProductionOrdersController.cs:129-132` + Test `Index_WerkbankOverride_SchlaegtGlobalenVorkommissionierWert` |
| 2 | Gleicher Termin in allen drei Listen | erfuellt | `ProductionOrdersController.cs:129`, `PickingLeitstandController.cs:134`, `FaWorklistController.cs:213` rufen denselben Resolver; Tests erwarten identisches Datum |
| 3 | Ohne Override / ohne Werkbank unveraendert | erfuellt | `PrePickingDaysResolver.cs:17` + `Index_OhneWerkbankOverride_NutztGlobalenWert`, `FaWorklistControllerTests.Index_FaOhneWerkbank_NutztGlobalenWert` |
| 4 | `0` ist expliziter Override | erfuellt | `PrePickingDaysResolver.cs:23` (strikt `HasValue`) + `Resolve_OverrideNull0_IstExpliziterWert_KeinSynonymFuerStandard` sowie je Controller ein `0`-Test |
| 5 | Kaskade auf Beschichtungstermin | erfuellt | Reihenfolge korrekt (`PickingLeitstandController.cs:143-149`, `FaWorklistController.cs:221-226`, `ProductionOrdersController.cs:138-147`); Test `Index_WerkbankOverride_VerschiebtBeschichtungsterminKaskadierend` |
| 6 | Pro-Zeile-UI-Rueckmeldung, alle drei Listen | erfuellt | `Views/ProductionOrders/Index.cshtml:151-159`, `Views/PickingLeitstand/Index.cshtml:195-203`, `Views/FaWorklist/Index.cshtml:134-140` — Badge „BG X" + Tooltip an der Werkbank-Zelle |
| 7 | Build/Tests gruen, Regel isoliert getestet | erfuellt | siehe oben + `IdealAkeWms.Tests/Services/PrePickingDaysResolverTests.cs` (reine Logik, kein DB-Zugriff) |
| 8 | Kein Schema-Diff | erfuellt | `git diff main...HEAD -- SQL/ IdealAkeWms/Migrations/ IdealAkeWms/Models/ProductionWorkplace.cs` → 0 Zeilen; `SQL/00_FreshInstall.sql` unveraendert; keine Migration |

**Prioritaetsregel an genau einer Codestelle:** `IdealAkeWms/Services/PrePickingDaysResolver.cs`
(`Resolve` Zeile 17, `IsOverrideActive` Zeile 23). Grep-verifiziert: keine duplizierte
Inline-Fallback-Logik (`?? vorkommissionierTage`) in den Controllern.

**Constitution-Checkliste:** `AppVersion.cs` in beiden Projekten auf `1.27.0`; Anwender-Changelog
`Views/Help/Changelog.cshtml`; Brain-Changelog
`secondbrain/changelog/2026-07-28-v1-27-0-override-prepickingdays.md`; `feature-map.md` Zeile 141;
`fallstricke.md`-Eintrag „ist wirkungslos" additiv durch die geltende Prioritaetsregel ersetzt;
`docs/TESTSZENARIEN.md` TS-9.6 – TS-9.10 inkl. Inhaltsverzeichnis;
`secondbrain/tests/testszenarien-index.md` synchron.

### Offene Nacharbeit (nicht blockierend)

- Der Kaskaden-Test zu Kriterium 5 existiert nur fuer `PickingLeitstandController`, nicht
  zusaetzlich fuer `ProductionOrdersController` und `FaWorklistController`. Risiko gering (gleicher
  Datenfluss, im Review geprueft), aber eine Luecke gegenueber „an allen drei Stellen isoliert
  bewiesen".
- `secondbrain/feature-map.md:15` nennt weiterhin „39 Eintraege v1.0.0 – v1.26.0" — vorbestehende
  Zaehler-Drift, nicht durch dieses Feature verursacht.

## Manuelle Test-Checkliste (Schranke 2 — Mensch)

**Vorbedingung fuer alle Punkte:** Deploy des Branches auf die Testumgebung, globaler AppSetting
`VorkommissionierTage` bekannt (Default `1`), Werkbank `A1` (Id=1) hat produktiv bereits
`OverridePrePickingDays = 7`.

- [x] **1. Stammdaten-Ausgangslage.** `Stammdaten → Werkbaenke` oeffnen: Werkbank `A1` zeigt in der
      Spalte „Abw. Vorkommissioniertage" den Wert `7 Tage`. Andere Werkbaenke zeigen „Standard".
- [x] **2. FA-Liste (TS-9.6).** `Fertigungsauftraege` oeffnen, eine FA an Werkbank `A1` suchen:
      BG-Termin = Kommissioniertermin **minus 7 Arbeitstage** (nicht minus 1). Werkbank-Zelle zeigt
      Badge „BG 7" mit Tooltip inkl. Standardwert.
      *Negativfall:* FA an einer Werkbank ohne Override zeigt weiterhin Kommissioniertermin minus 1
      Arbeitstag und **kein** Badge.
- [x] **3. Leitstand (TS-9.7).** Dieselbe FA-Nummer im `Kommissionier-Leitstand` aufrufen:
      **identischer** BG-Termin und identisches Badge wie in Punkt 2.
- [x] **4. FA-Abarbeitungsliste je AG (TS-9.7).** Dieselbe FA-Nummer in der
      `FA-Abarbeitungsliste je Arbeitsgang` (passenden Arbeitsgang waehlen) aufrufen: wieder
      **identischer** BG-Termin und identisches Badge.
- [x] **5. Kaskade Beschichtungstermin (TS-9.9).** FA mit Lackierteilen an `A1` mit einer
      vergleichbaren FA ohne Override vergleichen: der Beschichtungstermin verschiebt sich um
      dieselbe Differenz wie der BG-Termin (bei global `1` und Override `7`: 6 Arbeitstage frueher).
- [x] **6. Override `0` (TS-9.8).** Testwerkbank mit `OverridePrePickingDays = 0` anlegen, eine
      Test-FA zuordnen: BG-Termin **gleich** dem Kommissioniertermin, Badge „BG 0" sichtbar, und in
      der Werkbank-Liste steht „0 Tage" — **nicht** „Standard".
- [x] **7. FA ohne Werkbank-Zuordnung.** BG-Termin bleibt beim globalen Standard, kein Badge, keine
      Fehlermeldung, kein Layout-Bruch in allen drei Listen.
- [x] **8. Spaltenkopf-Tooltips.** In allen drei Listen zeigt der Kopf „BG-Termin" den **generischen**
      Text (Standardwert + Hinweis, dass er je Werkbank abweichen kann) statt einer fixen Zahl.
- [x] **9. Spaltenfilter-Regression.** Datumsfilter auf der BG-Termin-Spalte und der Werkbank-Filter
      funktionieren in allen drei Listen unveraendert (Server-Mode, ADR 0005), inkl. Pagination.
- [x] **10. Stammdaten-CRUD-Regression (TS-9.x Bestand).** `Werkbank anlegen / bearbeiten`: Feld
      „Abweichende Vorkommissioniertage" speichert, leert (`null` → „Standard") und validiert
      weiterhin gegen `Range(0, 365)`.
- [x] **11. Fachliche Freigabe des Blast Radius.** Mit der Fertigung abstimmen, dass sich die
      BG-Termine der FAs an Werkbank `A1` mit diesem Release tatsaechlich um 6 Arbeitstage nach vorne
      verschieben (Anwender-Changelog v1.27.0). Falls unerwuenscht: `OverridePrePickingDays` an `A1`
      **vor** dem Deploy leeren — kein Code-Rollback noetig.

**Deploy-Hinweise:** keine Migration, kein SQL-Skript, kein DB-Backup zwingend erforderlich (reine
Lese-/Verrechnungslogik). Einziges Betriebsrisiko ist die unter Punkt 11 beschriebene
Terminverschiebung an Werkbank `A1`.
