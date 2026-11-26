---
type: spec
title: "IDEAL-Leitstand: nur HauptFA-Zeilen anzeigen, Freigabe-Haken am HauptFA (Schalter „Sub-FAs anzeigen“)"
slug: 2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-2-spec
status: Entwurf
created: 2026-11-26
updated: 2026-11-26
source_backlog: "[[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste]]"
uebersicht: "[[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-uebersicht]]"
depends_on: "[[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-1-spec]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: IDEAL
affected_code:
  - IdealAkeWms/Data/Repositories/IProductionOrderRepository.cs + ProductionOrderRepository.cs (GetForLeitstandGroupedAsync/BuildLeitstandQuery optionaler Parameter nurHauptFa, Zählung Gruppen ohne HauptFA-Zeile)
  - IdealAkeWms/Controllers/PickingLeitstandController.cs (Index Z. 73-292, Parameter mitSubFa)
  - IdealAkeWms/Models/ViewModels/PickingLeitstandViewModel.cs (NurHauptFa, MitSubFa, GruppenOhneHauptFa)
  - IdealAkeWms/Views/PickingLeitstand/Index.cshtml (Filterkarte Schalter, flache Darstellung, Zählzeile, Banner)
  - IdealAkeWms/Views/PickingLeitstand/_PickingLeitstandRow.cshtml (Knopf „Alle Sub-FAs fertigmelden“ in der Zeilen-Aktion, nur HauptFA-Zeile)
  - IdealAkeWms.Tests (Repository InMemory, Controller)
  - IdealAkeWms/Views/Help/Index.cshtml (Abschnitt „Leitstand (Kommissionier-Freigabe)“), Changelog
  - scripts/deploy/nachlauf.psd1
  - docs/TESTSZENARIEN.md
open_questions:
  - "1 Freigabe-Haken am HauptFA: Der Code zeigt ihn bereits an der HauptFA-Zeile (seit v1.46.0). Wo fehlte er konkret (Seite, HauptFA-Nummer, Benutzer)? Bitte die Prüfabfragen P-1 bis P-3 ausführen - liegt ein Daten- oder Rechteproblem vor, wird es hier ergänzt."
  - "2 Schalter „Sub-FAs anzeigen“ in der Filterkarte (Standard aus) für die heutige Gruppenansicht mit Glas/Zukauf/Lack-T/VK-VA je Sub-FA - gewünscht?"
  - "3 Darstellung im Nur-HauptFA-Modus: eine Zeile je HauptFA ohne Gruppenkopf, Knopf „Alle Sub-FAs fertigmelden“ wandert in die Zeilen-Aktionen der HauptFA-Zeile - einverstanden?"
  - "4 Filter im Nur-HauptFA-Modus wirken auf die HauptFA-Zeile; Ausnahme FA-Nr.: eine Sub-FA-Nummer findet ihren HauptFA - einverstanden?"
  - "5 Spalten Lack-T, Glas, Zukauf, VK-VA zeigen an der HauptFA-Zeile nur deren eigene Werte (keine Zusammenfassung der Sub-FAs) - einverstanden?"
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

An der IDEAL wird nur am HauptFA freigegeben und kommissioniert ([[2026-09-25-kommissionierung-nur-hauptfa-spec]], v1.46.0).
Der Leitstand zeigt im hierarchischen Modus trotzdem jede Sub-FA als eigene Zeile. Die Leitstand-Bediener suchen zwischen vielen
Sub-FA-Zeilen ohne Bedienelement die eine HauptFA-Zeile mit dem Freigabe-Haken. Nur HauptFA-Zeilen machen die Seite kurz und die
Freigabe eindeutig.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope (nur hierarchischer Modus, Master `ProduktionsauftragHierarchisch = true`)**

- `/PickingLeitstand` zeigt standardmäßig nur HauptFA-Zeilen.
- Schalter „Sub-FAs anzeigen“ in der Filterkarte (Rückfrage 2) stellt die heutige Gruppenansicht her.
- Banner, wenn Aufträge keine HauptFA-Zeile haben (sonst würden sie still verschwinden).
- Freigabe-Haken und Knopf „Freigeben“ an der HauptFA-Zeile: Bestand prüfen und absichern (Rückfrage 1).

**Out-of-Scope**

- Flacher Modus (AKE): unverändert, Code-Pfad `PickingLeitstandController.cs:294-` wird nicht berührt.
- FA-Liste `/ProductionOrders`: nutzt dieselbe Repository-Methode (`ProductionOrdersController.cs:246`), bleibt unverändert
  (neuer Parameter mit Default `false`).
- Freigabe-Logik, Kaskade „Alle Sub-FAs fertigmelden“, Kommissionieraufträge: nur Ort des Knopfs ändert sich (Rückfrage 3).
- Backlog [[2026-09-25-leitstand-subfa-readonly-stueckliste]] (schreibgeschützte Sub-FA-Stückliste): betrifft nur noch die
  Ansicht „Sub-FAs anzeigen“, bleibt eigener Eintrag.

## Fachliche Anforderungen

### Ist-Stand (belegt; Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, AppVersion 1.99.81)

- Hierarchischer Zweig: `GetForLeitstandGroupedAsync` lädt alle Zeilen der Seiten-Gruppen, je Gruppe nach `SubOrderNumber`
  sortiert (`Data/Repositories/ProductionOrderRepository.cs:54-92`). Paginiert wird über Gruppen (`:69-75`); `TotalRowCount` zählt
  Sub-FAs (`:67`). Filterbasis `BuildLeitstandQuery(..., hierarchical: true)` (`:99-156`); FA-Nr. trifft `OrderNumber` **oder**
  `SubOrderNumber` (`:107-111`).
- Dieselbe Methode nutzt die FA-Liste (`Controllers/ProductionOrdersController.cs:246`).
- View: ein `<tbody>` je Gruppe mit Kopfzeile „HauptFA …“, Badge „n Sub-FAs“ (`group.Items.Count`) und Knopf „Alle Sub-FAs
  fertigmelden“ (`Views/PickingLeitstand/Index.cshtml:199-234`, Knopf `:216-226`, Dialog `:372-409`, Skript `:460-`). Zählzeile
  „x Aufträge · y Sub-FAs“ (`:113-120`). Der flache Zweig rendert `Model.Items` ohne Gruppenkopf (`:242-256`).
- **Freigabe-Haken am HauptFA gibt es schon:** `_PickingLeitstandRow.cshtml:17` `isHauptFaRow = Model.Hierarchical && !item.IsSubFa`;
  Bulk-Checkbox `:20-31` bei `LeitstandAktiv && CanManagePickingRelease && !item.IsDone && (!Hierarchical || isHauptFaRow)`;
  Freigabe-Knopf `:241-285` mit derselben Bedingung, „Freigeben“ zusätzlich nur mit Artikelnummer (`:260`). `item.IsSubFa` kommt aus
  `ProductionOrder.IsSubFaOf` (`PickingLeitstandController.cs:163`), Definition `IsHauptFa = SubOrderNumber == "" || SubOrderNumber == OrderNumber`
  (`Models/ProductionOrder.cs:29-40`).
- Die HauptFA-Zeile entsteht an der IDEAL nur, wenn der Strukturknoten mit `SubFA = HauptFA` materialisiert ist
  (`IDEALAKEWMSService/Services/FaMaterializationSyncService.cs:76-83`, `:149-153`: `SubOrderNumber = n.SubFA`). Laut Spec
  [[2026-11-18-ideal-kommissionierliste-filter-spalten-leitstand-spec]] A5 ist der Wurzelknoten `SubFA = HauptFA`. Fehlt sie, gibt es
  in der Gruppe keinen Haken.
- Möglichkeiten, warum der Haken am HauptFA fehlt (aus dem Code, am Gerät unbelegt): `LeitstandAktiv` aus (`Controller :193-194`;
  Freigabe-Antwort 3 der Spec 2026-11-18: „Leitstand an IDEAL wird aktiviert“), Benutzer ohne Leitstand-Recht
  (`CanManagePickingRelease`, `:198`), HauptFA erledigt (`IsDone || IsDonePicking`, `:140`), keine HauptFA-Zeile. → P-1 bis P-3.
- Leitstand-Zugriff: Klasse `[RequirePickingOrLeitstandAccess]` (`PickingLeitstandController.cs:12`), Freigabe-Aktionen
  `[RequireLeitstandAccess]` (`:377`, `:465`).

### Soll

| ID | Anforderung |
|---|---|
| L1 | Hierarchischer Modus, Schalter „Sub-FAs anzeigen“ aus (Standard): eine Zeile je HauptFA (`IsHauptFa`). Paginierung und Zähler über HauptFA-Zeilen. Zählzeile „x Aufträge (nur HauptFA)“. |
| L2 | Filter (Filterkarte, Spaltenfilter, Datums-/VK-VA-/Matchcode-Filter in C#) wirken auf die HauptFA-Zeile. Ausnahme FA-Nr. (Filterkarte und Spalte `order-number`): auch eine Sub-FA-Nummer findet ihren HauptFA (Rückfrage 4). |
| L3 | Darstellung ohne Gruppenkopf (flacher Zweig der View); der Knopf „Alle Sub-FAs fertigmelden“ steht in den Zeilen-Aktionen (`row-actions`) der HauptFA-Zeile, nur mit Picking-Recht, gleicher Dialog und gleiche Aktion `CascadeDone` (Rückfrage 3). |
| L4 | Freigabe-Haken, „Freigeben“/„Zurücknehmen“, Prioritätsfeld, Kommissionierer, KO-Links an der HauptFA-Zeile unverändert sichtbar und bedienbar. |
| L5 | Banner (melden statt still), wenn offene Aufträge keine HauptFA-Zeile haben: „n Aufträge ohne HauptFA-Zeile – nur mit „Sub-FAs anzeigen“ sichtbar, Freigabe dort nicht möglich.“ mit Link auf die Ansicht mit Sub-FAs. Gezählt mit demselben `showDone`-Filter. |
| L6 | Schalter „Sub-FAs anzeigen“ an: heutige Gruppenansicht unverändert (Rückfrage 2). Der Schalter steht in der URL (`mitSubFa=true`), Pagination und „Zurücksetzen“ nehmen ihn mit bzw. setzen ihn zurück. |
| L7 | Flacher Modus (AKE) und FA-Liste unverändert. |
| L8 | Spalten Lack-T, Glas, Zukauf, VK-VA an der HauptFA-Zeile zeigen deren eigene Werte (Rückfrage 5). |

## Technischer Loesungsentwurf

Ponytail-Leiter: vorhandene Gruppen-Abfrage mit einem Parameter, vorhandener flacher View-Zweig, vorhandener Dialog.

1. **Repository:** `BuildLeitstandQuery` und `GetForLeitstandGroupedAsync` bekommen `bool nurHauptFa = false`. Bei `true`:
   `q = q.Where(ProductionOrder.IsHauptFa)` (die eine Sub-FA-Definition, Fallstrick „Es gibt genau EINE Sub-FA-Definition“). FA-Nr.
   (`:107-111` und der Spaltenfilter `order-number` in `ApplyLeitstandColumnFilter`): zusätzlich
   `_dbSet.Any(s => s.OrderNumber == o.OrderNumber && s.SubOrderNumber.Contains(f))` (Muster Subquery statt Join wie `:129-133`).
   Neue Methode `CountGruppenOhneHauptFaAsync(bool showDone)` (eine `COUNT(DISTINCT OrderNumber)`-Abfrage mit `NOT EXISTS` HauptFA).
2. **Controller `Index`:** Parameter `bool mitSubFa = false`. Im hierarchischen Zweig `nurHauptFa: !mitSubFa`. Bei `nurHauptFa`
   die gemappten Zeilen als `vm.Items` übergeben (`listGroups.SelectMany(g => g.Items)`), `Hierarchical = true` bleibt (FA-Nr.-Zelle,
   `isHauptFaRow`), neues `vm.NurHauptFa = true`, `vm.GruppenOhneHauptFa = …`.
3. **View `Index.cshtml`:** Filterkarte Checkbox „Sub-FAs anzeigen“ (Muster „Erledigte anzeigen“ `:51-55`). Zweigwahl
   `Model.Hierarchical && !Model.NurHauptFa` → Gruppen, sonst flacher Zweig. Zählzeile L1, Banner L5 (`alert-warning`, Text, nicht
   nur Farbe). `data-hierarchical` nur bei Gruppenansicht `true`, damit `fa-liste-gruppierung.js` nichts sucht.
4. **Row-Partial:** Knopf `btn-cascade-done` (gleiche Klasse, `data-order-number`) in der `row-actions`-Zelle bei
   `Model.Hierarchical && isHauptFaRow && Model.CanPick && NurHauptFa` (neues Feld im `PickingLeitstandRowContext`). Das Skript
   `:460-` sucht nur `.btn-cascade-done` und braucht keine Änderung; `e.stopPropagation()` ist im flachen Zweig harmlos.
5. Frontend: Skill `frontend-design` ausdrücklich aufrufen. Bootstrap-Muster der Filterkarte, Trefferflächen ≥ 24 px, Kontrast AA.

## Migrations-/SQL-Auswirkungen

Keine Migration, kein Schema. Nur LINQ-Abfragen. `SQL/00_FreshInstall.sql`, `SQL/AgentJobs/` unberührt.

Prüfabfragen für Rückfrage 1 (WMS-DB IDEAL, Testsystem zuerst; nur lesen):

```sql
-- P-1: Leitstand an?
SELECT [Key], [Value] FROM dbo.AppSettings WHERE [Key] IN (N'LeitstandAktiv', N'KommissionierungMitZuweisung');

-- P-2: HauptFA-Zeile des betroffenen Auftrags (Nummer einsetzen) - fehlt sie, gibt es keinen Haken
SELECT po.Id, po.OrderNumber, po.SubOrderNumber, po.ArticleNumber, po.IsDone, po.IsCancelled,
       ps.IsDonePicking, ps.IsReleasedForPicking
FROM dbo.ProductionOrders po
LEFT JOIN dbo.ProductionOrderPickingStatus ps ON ps.ProductionOrderId = po.Id
WHERE po.OrderNumber = N'<HauptFA>'
ORDER BY CASE WHEN po.SubOrderNumber = N'' OR po.SubOrderNumber = po.OrderNumber THEN 0 ELSE 1 END, po.SubOrderNumber;

-- P-3: offene Aufträge ohne HauptFA-Zeile (gleiche Abfrage wie die Manifest-Prüfung unten)
SELECT COUNT(DISTINCT po.OrderNumber) AS OhneHauptFa
FROM dbo.ProductionOrders po
WHERE po.IsDone = 0 AND po.IsCancelled = 0
  AND NOT EXISTS (SELECT 1 FROM dbo.ProductionOrders h
                  WHERE h.OrderNumber = po.OrderNumber
                    AND (h.SubOrderNumber = N'' OR h.SubOrderNumber = h.OrderNumber));
```

Spalten belegt: `IsDone`/`IsCancelled` (`ProductionOrderRepository.cs:142`), `ProductionOrderPickingStatus.ProductionOrderId`/
`IsReleasedForPicking` (`SQL/00_FreshInstall.sql:431-436` laut Spec 2026-11-18). Das Leitstand-Recht des Benutzers in `/Users`
prüfen (Rolle `leitstand`, Glossar).

## Einstellungen

keine. „Sub-FAs anzeigen“ ist ein Filter der Seite (URL-Parameter), keine Einstellung. `LeitstandAktiv` bleibt unverändert.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine neuen Schreibwege. Freigabe, Priorität, Kaskade schreiben wie bisher (`SetReleaseAsync`/`SetReleaseBatchAsync`, `CascadeDone`)
mit ihren vorhandenen Audit-Feldern.

## Akzeptanzkriterien

1. IDEAL, Leitstand ohne Schalter: je HauptFA genau eine Zeile; keine Sub-FA-Zeile, kein Gruppenkopf. Pagination zählt HauptFA.
2. HauptFA-Zeile (offen, Benutzer mit Leitstand-Recht, `LeitstandAktiv` an) hat Bulk-Haken und „Freigeben“; „Markierte freigeben“
   gibt sie frei und legt Kommissionieraufträge an wie heute.
3. FA-Nr.-Filter mit einer Sub-FA-Nummer zeigt die HauptFA-Zeile ihres Auftrags.
4. Artikelnummer-Filter mit der Artikelnummer einer Sub-FA (nicht des HauptFA) zeigt nichts (Filter auf HauptFA-Zeile, Rückfrage 4).
5. „Alle Sub-FAs fertigmelden“ in den Zeilen-Aktionen öffnet denselben Dialog mit denselben Zahlen; Fertigmelden wirkt wie heute.
6. Schalter „Sub-FAs anzeigen“ an: Darstellung identisch zu heute (Gruppen, Sub-FA-Zeilen, Badge, Kopf-Knopf). Seitenwechsel behält den Schalter.
7. Auftrag ohne HauptFA-Zeile (Testdaten): Banner nennt die Anzahl; mit Schalter ist die Gruppe sichtbar.
8. AKE (flacher Modus): Leitstand unverändert, kein Schalter, kein Banner.
9. FA-Liste `/ProductionOrders` an der IDEAL unverändert (Sub-FA-Zeilen in Gruppen).
10. Benutzer nur mit Picking-Recht: keine Freigabe-Elemente (wie heute), Kaskaden-Knopf sichtbar.
11. Repository-Test InMemory: `nurHauptFa` liefert nur `IsHauptFa`-Zeilen, FA-Nr.-Ausnahme trifft über die Sub-FA; `nurHauptFa = false` unverändert.
12. Build und alle Tests grün.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (Nummer beim Dev-Start prüfen; nach Teil 1), Verweis auf TS-79 (Kommissionierung nur am HauptFA).

- **Vorbedingung:** IDEAL-Testsystem, Master hierarchisch, `LeitstandAktiv` an, zwei Benutzer (Leitstand-Recht, nur Picking-Recht),
  mindestens drei offene HauptFA mit Sub-FAs, davon einer nicht freigegeben.
- **x.1** Leitstand öffnen → nur HauptFA-Zeilen (AK 1).
- **x.2** HauptFA anhaken, „Markierte freigeben“ (AK 2).
- **x.3** FA-Nr.-Filter mit Sub-FA-Nummer (AK 3); Artikelfilter mit Sub-FA-Artikel (AK 4).
- **x.4** Kaskade aus der Zeile (AK 5).
- **x.5** Schalter „Sub-FAs anzeigen“, Seite 2, Zurücksetzen (AK 6).
- **x.6** Banner mit Auftrag ohne HauptFA-Zeile (AK 7, ggf. nur per Testdaten).
- **Negativ:** AKE-Leitstand (AK 8); FA-Liste IDEAL (AK 9); Benutzer ohne Leitstand-Recht (AK 10).

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | (kein Epic) | – | – |

## Deploy

*Vorläufig, der QA-Lauf bestätigt gegen den echten Diff.*

- **Web-App:** ja
- **Service:** nein
- **Migration:** nein
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`):

| Id | Version | Typ | Firma | Phase | Ausführung | Text |
|---|---|---|---|---|---|---|
| `Pruefung/LeitstandOhneHauptFaZeile` | (Dev-Lauf) | Pruefung | IDEAL | Nachher | anzeigen | P-3 als `COUNT`. „Offene Aufträge ohne HauptFA-Zeile: im Leitstand nur mit „Sub-FAs anzeigen“ sichtbar und nicht freigebbar. Materialisierung prüfen.“ |
| `v<Version>-LeitstandNurHauptFa` | (Dev-Lauf) | Info | IDEAL | Nachher | anzeigen | „Der Leitstand zeigt nur noch HauptFA-Zeilen. Sub-FAs über den Schalter „Sub-FAs anzeigen“ in der Filterkarte. „Alle Sub-FAs fertigmelden“ steht jetzt in der Zeile.“ |

- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` (im Worktree).

## Offene Rueckfragen

1. **Freigabe-Haken am HauptFA – was fehlte genau?** Der Haken und „Freigeben“ stehen seit v1.46.0 an der HauptFA-Zeile
   (`_PickingLeitstandRow.cshtml:20-31`, `:241-285`). Sie fehlen nur, wenn `LeitstandAktiv` aus ist, der Benutzer kein
   Leitstand-Recht hat, der HauptFA erledigt ist oder es keine HauptFA-Zeile gibt. Bitte Seite, HauptFA-Nummer und Benutzer nennen und
   P-1 bis P-3 ausführen. **Empfehlung:** Ergibt sich ein Daten- oder Rechteproblem, wird es behoben bzw. als Nachtrag in diese Spec
   aufgenommen; sonst bleibt es bei L4 (Bestand absichern, AK 2).
2. **Schalter „Sub-FAs anzeigen“?** Ohne Sub-FA-Zeilen sind Glas, Zukauf, Lack-T und VK-VA je Sub-FA im Leitstand nicht mehr
   bedienbar (nur noch in der FA-Liste und der FA-Struktur). **Empfehlung:** ja, Checkbox in der Filterkarte, Standard aus; die
   heutige Gruppenansicht bleibt damit erreichbar, Kosten ein URL-Parameter.
3. **Darstellung.** **Empfehlung:** eine Zeile je HauptFA ohne Gruppenkopf (ein Kopf über einer einzigen Zeile ist nur Lärm); der Knopf
   „Alle Sub-FAs fertigmelden“ zieht in die Zeilen-Aktionen der HauptFA-Zeile. Alternative: Gruppenkopf behalten, Badge ohne Zahl.
4. **Filter.** **Empfehlung:** alle Filter auf die HauptFA-Zeile (was man sieht, wird gefiltert); einzige Ausnahme FA-Nr.: eine
   Sub-FA-Nummer (z. B. vom Werkstattpapier gescannt) findet ihren HauptFA. Alternative: Gruppe erscheint, wenn irgendeine Sub-FA
   passt (dann treffen z. B. Artikel- und Datumsfilter unsichtbare Zeilen).
5. **Sub-FA-Werte an der HauptFA-Zeile zusammenfassen?** (z. B. Lack-T, wenn irgendeine Sub-FA Lackteile hat.) **Empfehlung:** nein,
   die HauptFA-Zeile zeigt ihre eigenen Werte; die Einzelwerte bleiben über den Schalter erreichbar. Eine Zusammenfassung wäre eine neue
   Regel je Spalte.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Freigabe-Haken am HauptFA – Befund und Ergebnis P-1 bis P-3 → **nur noch am haupt fa weil wir ja nur noch hauptfa freigeben und anzeigen wollen** (Gerald 2026-11-26, Chat)
2. Schalter „Sub-FAs anzeigen“ (Standard aus)? → **ja wird eigentlich nicht benötigt werden** (Gerald 2026-11-26, Chat)
3. Eine Zeile je HauptFA ohne Gruppenkopf, Kaskaden-Knopf in der Zeile? → **ja - subfas dürfen nicht gesondert freigegeben werden. Nur noch hauptfa** (Gerald 2026-11-26, Chat)
4. Filter auf HauptFA-Zeile, FA-Nr. findet auch über Sub-FA-Nummer? → **nur wenn checkbox welche eben eigentlich hinfällig ist** (Gerald 2026-11-26, Chat)
5. Keine Zusammenfassung der Sub-FA-Werte an der HauptFA-Zeile? → **alles was für diesen hauptfa inkl subfa (logik haben wir glaube ich schon bei der kommissionierliste so)** (Gerald 2026-11-26, Chat)
