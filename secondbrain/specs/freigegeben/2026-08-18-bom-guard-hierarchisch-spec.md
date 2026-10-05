---
type: spec
title: "BOM-Knopf im hierarchischen Modus abfangen statt HTTP 500 (UAT-Blocker, Minimal-Fix)"
slug: 2026-08-18-bom-guard-hierarchisch-spec
status: Testbereit
created: 2026-08-18
updated: 2026-09-08
source_backlog: "[[2026-08-18-ideal-nachlese-restarbeiten]]"
depends_on: "[[2026-07-29-standort-ideal-teil-7-spec]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
epic: false
etappen: []
open_questions: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-08-18
---

## Ziel / Nutzen (das Warum)

Nach dem Umlegen des Masters `ProduktionsauftragHierarchisch` am 2026-08-18 stehen 130
materialisierte IDEAL-Auftraege in `ProductionOrders` — und damit auch in FA-Liste und
Kommissionierung. **Ein Klick auf den BOM-Knopf endet dort in HTTP 500:**

```
SqlException: Ungueltiger Objektname "ake.dbo.vw_AKE_Kommissionierung_StuecklistenDB". (208)
   at BomRepository.GetBomItemsAsync(...)  BomRepository.cs:44
   at PickingController.Bom(Int32 id, String filterText)  PickingController.cs:235
```

`BomRepository` fragt bei Cache-Miss live gegen die **AKE-View** — Datenbankname und View fest im
SQL. Auf einem IDEAL-System existiert beides nicht.

**Warum das jetzt gefixt wird, obwohl es in keinem UAT-Punkt steht:** Der Fehler steht in keiner der
30 Checklisten-Positionen (Punkt 11 prueft die AKE-Regression ausdruecklich **mit Toggles auf
`false`**). Er wurde trotzdem gefunden — im normalen Gebrauch, wenige Minuten nach dem Umschalten.
Wer die UAT an echten Strukturen macht, trifft ihn ebenso. Eine Fehlerseite mitten in einer Abnahme
kostet mehr Zeit und Vertrauen, als dieser Fix wert ist.

**Warum NUR ein Minimal-Fix:** Die vollstaendige Loesung
([[2026-08-18-ake-view-abhaengigkeiten-hierarchisch-spec]]) traegt acht Fallstricke, drei davon mit
stillen Falschdaten (Cache-Vergiftung, Cache-First-Reihenfolge, Mengen-Semantik). Das gehoert nicht
in einen Zweig, der mit frischer QA-Evidenz (Web 1174 / Service 221) auf Abnahme wartet. Hier wird
**nur** der Absturz beseitigt — kein neues Repository, kein Cache-Umbau, kein Schluesselwechsel.

## Umfang

**In-Scope**
1. Guard: Im hierarchischen Modus wird der AKE-Stuecklisten-Zugriff **gar nicht erst ausgefuehrt**.
2. Verstaendlicher Hinweis in der Oberflaeche mit Verweis auf `/FaHierarchy`.
3. Sweep der **Aufrufer** von `IBomRepository` — damit nicht ein anderer Einstieg weiterhin wirft.
4. **`supportsSortDefault` der drei IDEAL-Listen auf `true`** (Entscheidung 2026-08-18).
   Sachfremd zum Guard, aber bewusst mitgenommen: Der Epic wird fuer den Guard ohnehin einmal
   geoeffnet und die QA erneut gefahren — diese drei Zeilen kosten dabei nichts. Getrennt haetten
   sie einen eigenen Reopen-Zyklus mit eigener QA erfordert.
   Betroffen: `FaHierarchyKommissionierListen`, `FaHierarchyBeschichtung`,
   `FaHierarchyVormontageEinzeln` (die drei flachen Listen).
   **`FaHierarchyStructure` (Baum) bleibt `false`** — dort ist die Zeilenreihenfolge die Hierarchie;
   eine gespeicherte Spaltensortierung waere semantisch sinnlos.
   Hintergrund: Der Wert stand defensiv auf `false`, weil `table-filter.js` nur das erste `<tbody>`
   sortierte. Der Defekt ist mit Etappe 6 behoben
   ([[2026-08-12-tabellen-sortierung-nur-erste-gruppe-bug]]) — der Grund ist weggefallen.
   **Testszenarien nachziehen:** Die bestehende Vorgabe „bieten *keine* Option Standard-Sortierung"
   (UAT-Punkt 18) kehrt sich fuer diese drei Listen um.

**Out-of-Scope** (bewusst, gehoert in die Folge-Spec)
- Eine hierarchische BOM-Quelle, `FaHierarchyBomRepository`, Cache-Umgehung, Schluesselwechsel.
- Der Sweep ueber alle uebrigen `vw_AKE_`/`[ake].[dbo]`-Fundstellen.

**Fachlicher Hintergrund, warum ein Hinweis hier vertretbar ist:** Die FA-Struktur-Ansicht **ist**
bereits die Stueckliste (Befund B4: die IDEAL-`FAListe`-View liefert Struktur UND Stueckliste in
einem). Ein IDEAL-Anwender verliert also nichts — er sieht dieselben Daten unter `/FaHierarchy`,
sogar vollstaendiger.

## Technischer Loesungsentwurf

### Wo der Guard sitzt: im Repository, nicht im Controller

**Entscheidung:** Der Guard sitzt in der `IBomRepository`-Kette (Decorator oder
`BomRepository.GetBomItemsAsync` ganz vorn), **nicht** nur in `PickingController.Bom`.
Begruendung: Ein Controller-Guard schuetzt genau einen Einstieg. Ruft ein anderer Aufrufer
(BDE-Terminal, Fehlteile, eine API) dieselbe Methode, wirft es dort weiter — und der naechste 500er
faellt an einer Stelle auf, an der niemand ihn erwartet.

### Verhalten

- **Master `false` (AKE):** unveraendert. Cache-First, dann Live gegen die AKE-View. **Kein**
  zusaetzlicher Code im Pfad, keine Verhaltensaenderung.
- **Master `true` (hierarchisch):** kein DB-Zugriff. Rueckgabe eines leeren `BomQueryResult` mit
  einer eigenen Quellen-Kennung (z. B. `NICHT_VERFUEGBAR_HIERARCHISCH`), damit die Oberflaeche den
  Zustand von „Stueckliste ist leer" unterscheiden kann.
- **Anzeige:** Statt einer leeren Tabelle ein Hinweis: *„Die Stueckliste wird im hierarchischen
  Modus ueber die FA-Struktur angezeigt"* mit Link auf `/FaHierarchy`. Kein Fehler-Look — es ist
  kein Fehler, sondern ein anderer Weg.
- **Optional, wenn ohne Suchaufwand moeglich:** den BOM-Knopf im hierarchischen Modus ausblenden.
  **Der Guard bleibt trotzdem Pflicht** — er faengt Direktaufrufe der URL und alle uebrigen
  Aufrufer ab.

### Master-Zustand lesen — bestehenden Weg wiederverwenden

Teil 7 hat fuer die `HierarchieUmstellung`-Seite bereits einen web-seitigen Zugriff auf den
Master-Zustand samt gecachtem Sperrstatus gebaut. **Diesen wiederverwenden** — kein zweiter Leseweg,
kein neuer Settings-Key, keine eigene Caching-Logik.

## Fallstricke

1. **Kein `try/catch` um das SQL.** Verlockend, aber falsch: Ein Fangnetz um die Abfrage wuerde
   **jeden** SQL-Fehler verschlucken — auch echte (Timeout, Rechte, Tippfehler im View-Namen) — und
   sie als „Stueckliste nicht verfuegbar" ausgeben. Der Guard muss **explizit am Master-Schalter**
   haengen, nicht am Scheitern.
2. **Leeres Ergebnis darf nicht wie „keine Positionen" aussehen.** Ohne eigene Quellen-Kennung
   waere ein Auftrag ohne Stueckliste nicht von „im hierarchischen Modus nicht verfuegbar" zu
   unterscheiden — der Anwender haelte die Stueckliste fuer leer.
3. **Andere Aufrufer nicht vergessen.** Vor der Umsetzung die Aufrufer von `GetBomItemsAsync` /
   `IBomRepository` erheben. Der BOM-Knopf war der erste, der angeklickt wurde — nicht zwingend der
   einzige.
4. **Nicht zum Feature ausbauen.** Sobald jemand anfaengt, „dann koennten wir doch gleich aus
   `FaHierarchyNode` lesen", ist es die Folge-Spec mit ihren acht Fallstricken. **Hier nicht.**
   Der Hinweistext muss erkennbar als Zwischenstand formuliert sein, damit niemand BOM fuer IDEAL
   als „erledigt" verbucht.
5. **`OseonConnection` im Konstruktor.** `BomRepository` holt sie im Ctor und wirft bei Fehlen. Auf
   diesem System ist sie gesetzt (der Fehler kam aus dem SQL, nicht aus dem Ctor) — aber falls der
   Guard so frueh sitzt, dass der Ctor gar nicht mehr laufen muss, ist das ein Nebengewinn. Nicht
   erzwingen, nur nicht verschlechtern.

## Akzeptanzkriterien

1. **AKE unveraendert:** Bei Master `false` verhaelt sich der BOM-Knopf bit-identisch zu vorher —
   Cache-Treffer und Cache-Miss je einmal nachgewiesen.
2. **Kein 500 mehr:** Bei Master `true` liefert `/Picking/Bom/<id>` eine normale Seite mit Hinweis,
   keine Exception, **kein** SQL-Zugriff auf `[ake].[dbo]` (im Log nachweisbar).
3. **Guard haengt am Schalter, nicht am Fehler** — kein `try/catch` um die AKE-Abfrage.
4. **Unterscheidbarkeit:** Der Zustand „hierarchisch, nicht verfuegbar" ist im `BomQueryResult`
   maschinell von „leere Stueckliste" unterscheidbar.
5. **Alle Aufrufer abgedeckt:** Jeder erhobene Aufrufer von `IBomRepository` laeuft im
   hierarchischen Modus ohne Exception.
6. **Hinweis verweist auf `/FaHierarchy`** und ist als Zwischenstand erkennbar formuliert.

## Test-Szenarien

- **AKE-Regression:** Master `false`, BOM-Knopf an bekanntem Auftrag — Positionen wie bisher
  (einmal mit gefuelltem, einmal mit leerem Cache).
- **IDEAL-Grundfall:** Master `true`, BOM-Knopf an einem materialisierten FA — Hinweisseite, kein
  Fehler, Link auf `/FaHierarchy` funktioniert.
- **Direktaufruf:** `/Picking/Bom/<id>` per URL im hierarchischen Modus — gleicher Hinweis (Beleg,
  dass der Guard nicht nur an einem ausgeblendeten Knopf haengt).
- **Log-Nachweis:** Im hierarchischen Modus erscheint **keine** SQL-Abfrage gegen
  `vw_AKE_Kommissionierung_StuecklistenDB`.
- **Weitere Aufrufer:** jeder erhobene Einstieg einmal im hierarchischen Modus aufrufen.

## Deploy

**Finalisiert durch QA (2026-08-18) aus dem echten Diff:** `deploy.web = true`, `service = false`,
`migration = false`. Der Diff ruehrt ausschliesslich `IdealAkeWms/` (Repository-Decorator, DI,
ViewModel-Marker, 4 Views) + `IdealAkeWms.Tests/` + `docs/TESTSZENARIEN.md` an — kein Code unter
`IDEALAKEWMSService/`, keine neue Datei unter `*/Migrations/`.

Der Guard geht mit dem bestehenden Web-Publish des Buendels mit — **kein** zusaetzlicher
Deploy-Schritt, **kein** eigener Publish-Lauf nur fuer diesen Fix. Publish erfolgt **aus dem
Worktree** (Mensch-Fluss: Worktree publishen → Testsystem → Test → danach Merge):

```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

Kein Service-Publish (Guard betrifft nur den Web-Prozess), keine Migration, keine DB-Aenderung.

Hinweis: nach dem Merge nur dann erneut aus `main` publishen, wenn der Merge tatsaechlich
getestete Dateien mit parallelen `main`-Aenderungen zusammengefuehrt hat.

**Folgen fuer das Buendel:** Der Epic ging von `Testbereit` auf `InUmsetzung` zurueck; die QA wurde
danach **erneut gefahren** (Build + beide Test-Suiten, s. QA-Nachweis unten), bevor Schranke 2
ansteht. Das ist der bewusst getragene Preis dafuer, die UAT nicht in einen 500er laufen zu lassen.
Version bleibt unveraendert (v1.34.0, bestaetigt in beiden `AppVersion.cs`) — kein eigener Bump fuer
einen Guard.

## QA-Nachweis (2026-08-18)

- **Build:** `dotnet build IdealAkeWms.slnx` im Worktree `feature/2026-08-07-ideal-teile-1-5`
  (Commit `1173cf1`) — **erfolgreich**, 0 Fehler (9 Vorbestehende Warnungen, keine davon aus diesem
  Diff).
- **Web-Suite:** `dotnet test IdealAkeWms.Tests` — **1219 erfolgreich, 1 uebersprungen
  (Integrationstest, vorbestehend), 0 Fehler**, gesamt 1220.
- **Service-Suite:** `dotnet test IDEALAKEWMSService.Tests` — **231 erfolgreich, 0 Fehler**.
- **Testszenarien:** TS-68.1 – 68.6 (`docs/TESTSZENARIEN.md`) gegen AK 1–6 geprueft — jedes AK hat
  ein zugehoeriges TS, Formulierung stimmt mit dem Code ueberein. TS-59.19/60.17/61.25 korrekt
  umgekehrt (`supportsSortDefault: true` in `FaHierarchyKommissionierListen/Index.cshtml`,
  `FaHierarchyBeschichtung/Index.cshtml`, `FaHierarchyVormontage/Index.cshtml`; Baum
  `FaHierarchy/Index.cshtml` bleibt `false`). Kapitel 68 bereits im Hauptcheckout-Index
  (`secondbrain/tests/testszenarien-index.md`) referenziert.
- **Code-Review (manuell, kein Finding):**
  - Guard haengt strikt am Master-Schalter (`HierarchischeStrukturKeys.Master` via
    `IServiceSettingRepository.GetValueAsync`, wiederverwendeter Teil-7-Leseweg) — **kein**
    `try/catch` um den inneren Aufruf (Fallstrick 1 eingehalten).
  - Marker `BomDataSources.HierarchicalUnavailable` ("NICHT_VERFUEGBAR_HIERARCHISCH") ist von
    `KEINE_DATEN` unterscheidbar und in `Bom.cshtml` getrennt behandelt (Fallstrick 2).
  - Alle erhobenen `IBomRepository`-Aufrufer bestaetigt abgedeckt: `PickingController.Bom` (Zeile
    235) + `PrintBom` (Zeile 483, 579), `ReadOnlyBomBuilder.BuildAsync` (Zeile 64) — Letzterer wird
    von `FaWorklistController.Bom` und `FaCompletionController.Bom` verwendet, beide rendern
    explizit `View("~/Views/Picking/Bom.cshtml", vm)` — **derselbe** View-Guard greift also fuer
    alle vier Einstiege (AK 5 vollstaendig erfuellt, nicht nur den Picking-Knopf).
  - DI-Factory in `Program.cs` resolved den inneren Decorator ueber den **konkreten** Typ
    `CachedBomRepository` (nicht `IBomRepository`) — keine Selbstreferenz-Gefahr in der
    Registrierungskette bestaetigt.
  - `GuardedServiceSettingRepository.GetValueAsync` reicht ungefiltert an das innere Repository
    durch (kein Einfluss des Schreib-Guards auf den Lesepfad) — die Master-Abfrage im BOM-Guard ist
    unbeeinflusst korrekt.
  - Keine echten Findings. Keine stillen Fixes noetig.
- **Nicht InMemory-testbar (Manual-UAT, kein Blocker):** Log-Nachweis „kein `[ake].[dbo]`-Zugriff im
  hierarchischen Modus" und die visuelle Hinweisseite — beides nur am laufenden System pruefbar.

**Ergaenzung der UAT-Checkliste:** ein Punkt „BOM-Knopf im hierarchischen Modus zeigt den Hinweis
statt einer Fehlerseite" — damit der Fix in der Abnahme auch bestaetigt wird.

## Offene Rueckfragen

Keine. Umfang, Verhalten und Abgrenzung sind entschieden; die vollstaendige Loesung ist als eigene
Spec ausgelagert.

## Freigabe-Antworten (2026-08-18)

1. **Guard wird VORGEZOGEN** — vor dem Merge, im bestehenden Epic-Worktree. Der Epic geht dafuer
   von `Testbereit` auf `InUmsetzung` zurueck; die QA ist danach erneut zu fahren (Build + beide
   Suiten), bevor Schranke 2 ansteht. Bewusst getragener Preis: Die UAT soll nicht in eine
   Fehlerseite laufen.
2. **`supportsSortDefault` auf `true`** fuer die drei flachen IDEAL-Listen (Umfang Punkt 4);
   der Baum bleibt `false`.
3. **Die Vollloesung** ([[2026-08-18-ake-view-abhaengigkeiten-hierarchisch-spec]]) bleibt
   **ausserhalb** dieses Laufs und ist als **erster Block des naechsten Entwicklungszyklus**
   gesetzt — nach dem Merge, aus `main`. Kein Widerspruch zum Vorziehen des Guards: Der Guard
   verhindert das Betreten eines Datenpfads, die Vollloesung baut einen.

Damit ist die Spec startklar — Status setzen und nach `specs/freigegeben/` verschieben.

## Manuelle Test-Checkliste (Schranke 2)

Am Test-/IDEAL-System, nach dem Web-Publish aus dem Worktree:

1. **AKE-Regression (TS-68.1, AK 1):** Master `ProduktionsauftragHierarchisch` = `false`. BOM-Knopf
   an einem bekannten AKE-FA — einmal mit gefuelltem BOM-Cache, einmal mit geleertem Cache (z. B.
   nach App-Pool-Recycle) — Positionen erscheinen bit-identisch zu vorher, kein Hinweis-Badge.
2. **BOM-Knopf im hierarchischen Modus zeigt den Hinweis statt einer Fehlerseite (TS-68.2, AK 2/6 —
   vom Spec-Freigabegespraech geforderter Kernpunkt):** Master = `true`. BOM-Knopf an einem
   materialisierten IDEAL-FA (z. B. einem der 130 aus dem Umlegungs-Vorfall) — **kein HTTP 500**,
   stattdessen Hinweisseite „Stückliste im hierarchischen Modus" mit Badge „FA-Struktur" und
   funktionierendem Link/Button zu `/FaHierarchy`.
3. **Direktaufruf (TS-68.3, AK 2):** `/Picking/Bom/<id>` direkt in der Adresszeile im hierarchischen
   Modus aufrufen — derselbe Hinweis erscheint (belegt, dass der Guard nicht nur am Knopf haengt).
4. **Log-Nachweis (TS-68.4, AK 2/3):** Serilog-Log waehrend Schritt 2/3 pruefen — **keine**
   SQL-Abfrage gegen `vw_AKE_Kommissionierung_StuecklistenDB` bzw. `[ake].[dbo]`. Kein
   SqlException-Eintrag.
5. **Weitere Aufrufer (TS-68.5, AK 5):** im hierarchischen Modus `/Picking/PrintBom/<id>` aufrufen
   sowie die read-only Vorbau-Stückliste (FA-Abarbeitungsliste, `/FaWorklist/Bom/<id>` bzw.
   `/FaCompletion/Bom/<id>`, je nach aktivem Toggle) — beide zeigen denselben Hinweis, keine
   Exception.
6. **Unterscheidbarkeit (TS-68.6, AK 4):** einen AKE-FA ohne Stückliste (Master `false`) aufrufen —
   Badge „Keine Daten gefunden" (rot) statt des blauen „FA-Struktur"-Hinweises; die beiden Zustaende
   sind optisch klar unterschiedlich.
7. **Sortier-Umkehr (TS-59.19/60.17/61.25):** auf allen drei flachen IDEAL-Listen
   (`/FaHierarchyKommissionierListen`, `/FaHierarchyBeschichtung`, `/FaHierarchyVormontage`) im
   Spalten-Dialog pruefen, dass „Standard-Sortierung speichern" jetzt **angeboten** wird, eine
   Sortierung speichern, Reload → Sortierung bleibt erhalten. Auf `/FaHierarchy` (Baum) pruefen,
   dass die Option weiterhin **fehlt**.
8. **Negativfall:** waehrend Schritt 2 pruefen, dass keine leere Tabelle mit „Keine
   Stücklisten-Positionen gefunden" erscheint (das waere die falsche, mit „leerer Stückliste"
   verwechselbare Darstellung) — es muss die Hinweis-Box sein, nicht die Tabelle.

## Re-QA Nachtrag UAT-Lauf 1 (2026-09-08)

**Anlass:** UAT-Lauf 1 am IDEAL-Testsystem (2026-09-08,
[[2026-09-08-uat-ergebnis-ideal-buendel-lauf-1]]) fand Befund **U1** (Triage-Eintrag T2): Die
Guard-Hinweisseite (`DataSource == HierarchicalUnavailable`) rendert weder `#bomTable` noch die
Expand-/Collapse-/Druck-Buttons; das ungeschützte `DOMContentLoaded`-Skript griff trotzdem auf sie
zu → `TypeError: Cannot read properties of null (reading 'addEventListener')` in der Browser-
Konsole. Zusätzlich zeigte die Kopfzeile auf der Hinweisseite die HauptFA-Nummer statt der
Sub-FA-Nummer (Z4-Konvention-Inkonsistenz zu den übrigen sechs Ansichten dieses Bündels).

**Commit:** `a8d75de` (Worktree `feature/2026-08-07-ideal-teile-1-5`,
`.claude/worktrees/2026-08-07-ideal-teile-1-5`) — Vorgänger-QA-Stand des Gesamtbündels war
`dde9a17` (Testbereit, Web 1243/+1 skip, Service 232).

**1. Build:** `dotnet build IdealAkeWms.slnx` im Worktree — **erfolgreich, 0 Fehler** (12
vorbestehende Warnungen, keine davon aus diesem Diff).

**2. Tests, beide Suiten grün:**
- `dotnet test IdealAkeWms.Tests` → **1245 erfolgreich, 0 Fehler, 1 übersprungen**
  (`ProductionOrderEagerCreateAgentJobTests…`, vorbestehender Integration-Skip), gesamt 1246 — +2
  gegenüber `dde9a17` (neuer Theory-Test `Bom_ViewModel_CarriesSubOrderNumber`, 2 Fälle).
- `dotnet test IDEALAKEWMSService.Tests` → **232 erfolgreich, 0 Fehler, 0 übersprungen** —
  unverändert (Diff berührt `IDEALAKEWMSService` nicht).

**3. Diff-Review (`git show a8d75de`), Ergebnis: keine Befunde.**
- **AKE-Flachmodus bit-identisch bestätigt:** Die Kopfzeile zeigt `Model.SubOrderNumber ??
  Model.OrderNumber`; der Zusatz „| HauptFA …" erscheint nur, wenn `SubOrderNumber !=
  OrderNumber` — im Flachmodus ist `SubOrderNumber == OrderNumber` (Teil-7-Backfill-Invariante),
  der Zusatz entfällt, Markup ist bit-identisch zu vorher. Verifiziert zusätzlich per neuem Test
  (`InlineData("4711","4711")`).
- **Druck-Knopf:** weiterhin gerendert in jedem Modus außer `HierarchicalUnavailable`
  (`@if (Model.DataSource != BomDataSources.HierarchicalUnavailable)`); auf der Guard-Hinweisseite
  bewusst nicht angeboten (dort gibt es nichts zu drucken).
- **JS-Frühabbruch greift nur, wo legitim kein `#bomTable` existiert:** `#bomTable` (Zeile 161)
  liegt im `else`-Zweig zu `DataSource == HierarchicalUnavailable` (Zeile 51–71) — dieser
  `else`-Zweig deckt **auch** den Fall `KEINE_DATEN` ab (Zeile 21–23 ist nur die Badge-Auswahl
  *innerhalb* der Kopfzeile, keine zweite Verzweigung der Tabelle). Der leere Zustand rendert
  **innerhalb** von `#bomTable` eine Hinweiszeile im `tbody`
  (`@if (!Model.Items.Any())`, Zeile 347) — die Tabelle selbst bleibt vorhanden. Der Guard
  `if (!document.getElementById('bomTable')) return;` (Zeile 685) fängt daher **ausschließlich**
  den `HierarchicalUnavailable`-Zweig ab; kein legitimer Pfad mit vorhandener Tabelle wird
  abgewürgt. Am DOM geprüft (nicht am Modus) — schützt auch künftige Varianten ohne Tabelle.
  **Kein Befund.**
- **Read-Only-Modus (`ReadOnly=true`, FA-Abarbeitungsliste) unverändert:** `ReadOnlyBomBuilder`
  befüllt jetzt zusätzlich `SubOrderNumber`; Rendering-Pfad (`else`-Zweig, `#bomTable` vorhanden)
  unangetastet, kein Regressions-Risiko.
- **XSS/Encoding:** `@(Model.SubOrderNumber ?? Model.OrderNumber)` und
  `@Model.OrderNumber` laufen durch Razors automatisches HTML-Encoding (kein `Html.Raw`) — keine
  neue Injektionsfläche.
- **Konvention:** keine TempData-Nutzung in diesem Diff (reine Anzeige/Guard), Sprachregel
  eingehalten (Code/Kommentare Englisch bzw. neutral, UI-Text Deutsch), keine Stilverstöße.
- **Test-Setup** (`SetupForBom`) mockt alle Listen-/Dictionary-Rückgaben explizit (Moq-Fallstrick
  laut `secondbrain/architektur/fallstricke.md` beachtet), Theory deckt hierarchisch
  (`SubOrderNumber != OrderNumber`) **und** flach (`==`) ab.

**4. Testszenarien:** `docs/TESTSZENARIEN.md` TS-68.2 um den U1-Nachtrag ergänzt (Konsole ohne
`TypeError`, Kopfzeile nennt Sub-FA-Nummer + HauptFA-Zusatz nur bei Abweichung, kein Druck-Knopf
auf der Hinweisseite) **inkl. Negativfall** (Flachmodus: Kopfzeile unverändert „FA <Nr>" ohne
Zusatz, Druck-Knopf vorhanden). Anwender-Changelog (`Views/Help/Changelog.cshtml`) um den
Abnahmetest-Nachtrag ergänzt. Kein Versions-Bump (v1.35.0 bleibt unreleased).

**5. Skills:** `superpowers:verification-before-completion` angewandt (Build/Test-Ausgabe in
diesem Lauf frisch erzeugt); `code-review`-Betrachtung als Teil des Diff-Reviews oben — keine
Findings, kein Nacharbeitsbedarf.

**Ergebnis: `Testbereit` bestätigt** (Status unverändert, kein Rücksprung nötig — reiner
Nachtrag ohne Regression).

### Manuelle Checkpunkte für den Nachtrag (zusätzlich zur Checkliste oben)

9. **U1a — Konsole ohne Fehler (TS-68.2):** BOM-Knopf im hierarchischen Modus öffnen, Browser-
   Konsole (F12) prüfen → **kein** `TypeError … addEventListener`.
10. **U1b — Kopfzeile nennt die Sub-FA (TS-68.2):** auf der Hinweisseite steht „Stückliste - FA
    <SubOrderNumber>"; ist `SubOrderNumber != OrderNumber`, folgt zusätzlich „| HauptFA
    <OrderNumber>"; im Flachmodus (AKE) bleibt die Kopfzeile unverändert „FA <OrderNumber>" ohne
    Zusatz.
11. **U1c — kein Druck-Knopf auf der Hinweisseite:** im hierarchischen Modus erscheint „Stückliste
    drucken" nicht; im Flachmodus und im Read-Only-Modus (FA-Abarbeitungsliste) bleibt er
    vorhanden.
