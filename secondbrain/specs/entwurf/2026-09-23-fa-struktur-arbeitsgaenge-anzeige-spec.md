---
type: spec
title: "FA-Struktur: erkannte BDE-Arbeitsgänge je (Sub-)FA sichtbar machen (Modal)"
slug: 2026-09-23-fa-struktur-arbeitsgaenge-anzeige-spec
status: Entwurf
created: 2026-09-23
updated: 2026-09-23
source_backlog: "[[2026-09-23-fa-struktur-arbeitsgaenge-anzeige]]"
depends_on: "[[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - "IdealAkeWms/Data/Repositories/IWorkOperationRepository.cs — NEUE Methode `GetBySubOrderNumbersWithWorkplaceAsync(IReadOnlyCollection<string> subOrderNumbers)`, bundled Read (kein N+1)"
  - "IdealAkeWms/Data/Repositories/WorkOperationRepository.cs — Implementierung, analog `ProductionOrderRepository.GetBySubOrderNumbersAsync` (AsNoTracking, `Include(ProductionOrder)`+`Include(ProductionWorkplace)`, `Where(SubOrderNumber != null && set.Contains(...))`, leere Liste bei leerem Input)"
  - "IdealAkeWms.Tests/Repositories/WorkOperationRepositoryTests.cs — neuer Testfall fuer die bundled Methode (InMemory-testbar)"
  - "IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs — neuer Record `WorkOperationSummary(string OperationNumber, string Name, string? SageArbeitsplatznummer)` + neue Property `FaHierarchyTreeNode.Arbeitsgaenge` (`List<WorkOperationSummary>`, Default leer) — je materialisiertem Knoten (SubFA != 0) seine eigenen erkannten AGs, vom Controller nachtraeglich angehaengt"
  - "IdealAkeWms/Controllers/FaHierarchyController.cs — `Index`: nach dem Aufbau von `pageStructures` bundled AGs laden (Sub-FA-Nummern der SICHTBAREN Seite einsammeln, EINE Repository-Abfrage, Ergebnis je Knoten anhaengen); neue DI-Abhaengigkeit `IWorkOperationRepository`"
  - "IdealAkeWms/Views/FaHierarchy/_FaHierarchyNode.cshtml — Knopf-Platzierung Sub-FA-Zeile (abhaengig von Rueckfrage 1)"
  - "IdealAkeWms/Views/FaHierarchy/Index.cshtml — Knopf an der HauptFA-Struktur-Kopfzeile, EIN gemeinsames Bootstrap-5-Modal `#faArbeitsgaengeModal` je Seite (nicht je Struktur), Inhalt kommt aus einem `<template>`-Element je Struktur (serverseitig vorgerendert, JS klont nur), neue `<script>`-Bloecke `view-config`/Spalten unveraendert"
  - "IdealAkeWms/wwwroot/js/fa-hierarchy-tree.js — Klick-Delegation fuer den neuen AG-Knopf (Event-Delegation auf `#faTree`, analog bestehendem `.fa-toggle`-Muster), Template-Klon in `#faArbeitsgaengeModal .modal-body` + Titel setzen, `bootstrap.Modal`-Aufruf"
  - "docs/TESTSZENARIEN.md — neues Kapitel TS-78 (naechste freie Nummer, verifiziert 2026-09-23 gegen den Worktree-Stand: hoechstes Kapitel TS-77)"
  - "secondbrain/tests/testszenarien-index.md — TS-78 nachziehen"
open_questions:
  - "Knopf nur auf der HauptFA-Struktur-Kopfzeile (ein Klick oeffnet das konsolidierte Modal fuer den GANZEN Baum) — oder zusaetzlich je Sub-FA-Zeile ein eigener Knopf (oeffnet dasselbe konsolidierte Modal, aber mit Sprung/Scroll zum jeweiligen Sub-FA-Abschnitt)? Aendert nur die View/JS-Platzierung, nicht das Datenmodell."
  - "Verhalten bei 0 erkannten AGs fuer einen Auftrag (weder am HauptFA noch an irgendeinem Sub-FA der Struktur): Knopf fuer diese Struktur ganz ausblenden, oder anzeigen und im Modal „keine Arbeitsgaenge erkannt“ ausgeben?"
  - "Sollen Sub-FAs OHNE eigene AGs im Modal trotzdem mit „—“ gelistet werden (Vollstaendigkeit ueber den ganzen Baum), oder nur Sub-FAs MIT mindestens einem erkannten AG erscheinen (reine Trefferliste)?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
# Flache Schluessel mit Absicht: Obsidians Property-Editor kann verschachtelte
# YAML-Objekte NICHT bearbeiten - und genau diesen Block fuellt der Mensch aus.
---

> **Umsetzungsort (Hinweis, kein Frontmatter-Feld):** Laut Backlog im selben Buendel-Worktree wie
> das vorausgesetzte Epic — `.claude/worktrees/2026-08-07-ideal-teile-1-5`
> (`feature/2026-08-07-ideal-teile-1-5`). `worktree`/`branch` bleiben im Frontmatter bewusst LEER;
> der Dev-Lauf setzt sie beim Start (Schranke-1-Konvention).

## Ziel / Nutzen (das Warum)

Seit dem Epic [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]] (v1.44.0, Testbereit) legt
die Struktur-Erkennung echte `WorkOperation`-Zeilen je materialisiertem IDEAL-Sub-FA an —
operativ sichtbar/buchbar am BDE-Terminal. Es fehlt aber ein **Überblick zur Verifikation**: Wo
sieht ein Mensch (Planung, Fertigungsleitung, UAT) je Auftrag auf einen Blick, **welche**
Arbeitsgänge die Struktur-Erkennung tatsächlich angelegt hat? Heute nur zeilenweise im
Aktivitäts-Protokoll (SyncLog), nicht im fachlichen Kontext der FA-Struktur.

Diese Spec ergänzt die bestehende FA-Struktur-Baumanzeige (`FaHierarchyController.Index`, IDEAL
Teil 2) um eine **read-only Detailansicht** (Modal): ein Knopf öffnet je Auftrag ein Fenster mit
der HauptFA-Nummer und ihrer AG-Liste sowie je Sub-FA-Nummer der jeweiligen AG-Liste — die
konsolidierte Sicht des ganzen FA-Baums, ohne den Baum selbst zu verlassen oder das SyncLog zu
durchsuchen.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**

- Ein Knopf in der FA-Struktur-Baumanzeige (`/FaHierarchy`), der ein Bootstrap-5-Modal öffnet.
- Modal-Inhalt: HauptFA-Nummer + ihre erkannten `WorkOperation`-Zeilen, darunter je materialisiertem
  Sub-FA (`SubFA != 0`) dessen eigene erkannte `WorkOperation`-Zeilen. Je AG: `OperationNumber`
  (Kürzel), `Name` (dient zugleich als Werkbank-Anzeige, siehe „Fachliche Anforderungen“),
  `ProductionWorkplace.SageArbeitsplatznummer`.
- Read-only — keine Bearbeitung, kein Anlegen/Löschen von `WorkOperation`-Zeilen aus dieser
  Ansicht (die AGs sind sync-geführt, siehe Epic).
- Bundled Read: die AGs werden für alle auf der aktuellen Seite sichtbaren Aufträge **einmal**
  geladen (kein N+1 je Struktur/Knopf).
- Neue Testszenarien (TS-78) inkl. Negativfall „0 AGs“ (Verhalten gemäß Rückfrage 2).

**Out-of-Scope**

- Keine Lücken-/Unbekannt-Analyse in dieser Ansicht (welche Kürzel nicht erkannt wurden, bleibt im
  SyncLog/der Sammelmeldung des Epics — siehe dort).
- Keine Änderung an `WorkOperationStructureDetectionService`, `ProductionWorkplaceSyncService`
  oder sonstiger Sync-/Anlege-Logik — reine Leseansicht auf bereits vorhandene Daten.
- Keine Änderung am BDE-Terminal oder an `BdeScanResolver`.
- Kein neuer Zugriffsfilter — die FA-Struktur behält ihren bestehenden
  `[RequirePickingOrTrackingOrLeitstandAccess]` (Class-Level, Read-only ⇒ kein Edit-Split nötig).
- Kein neuer Listen-View im Sinn von ADR 0005 (Begründung siehe „Technischer Lösungsentwurf“).

## Fachliche Anforderungen

1. Der Knopf ist nur auf Zeilen/Strukturen mit mindestens einem materialisierten Auftrag
   (`SubFA != 0`) sichtbar bzw. wirksam — Blatt-/Materialzeilen (`SubFA == 0`, Kaufteile/Material
   ohne eigenen FA) haben keinen eigenen `WorkOperation`-Datensatz und damit nichts zu zeigen.
2. Titel des Modals = FA-Nummer (HauptFA der Struktur).
3. Aufbau des Modal-Inhalts (Wortlaut des Backlogs, bereits entschieden — **nicht** erneut zur
   Diskussion): zuerst die HauptFA-Nummer mit ihrer eigenen AG-Liste, danach je Sub-FA-Nummer eine
   eigene AG-Liste — die HauptFA-Zeile ist dabei der Wurzelknoten der Struktur
   (`FaHierarchyStructure.Roots`), der ebenfalls ein materialisierter Auftrag mit eigener
   `SubOrderNumber` ist (siehe „Technischer Lösungsentwurf“ zur Begriffsklärung HauptFA vs.
   Wurzel-Sub-FA).
4. Je AG genau drei Angaben, wie im Backlog benannt: `OperationNumber` (Kürzel, z. B. `KA`),
   `Name`/Werkbank (ein Feld — `WorkOperation.Name` wird beim Anlegen als Kopie von
   `ProductionWorkplace.Name` gesetzt, siehe Epic-Spec Baustein b — Name der Arbeitsgang-Zeile UND
   Werkbank-Bezeichnung sind hier identisch, kein zweites Feld nötig), und die
   Sage-Arbeitsplatznummer (`WorkOperation.ProductionWorkplace.SageArbeitsplatznummer`, live
   nachgeschlagen über die FK — nicht zum Anlagezeitpunkt eingefroren).
5. Umfang bewusst NUR erkannte AGs (keine Lücken-/Unbekannt-Analyse) — siehe Out-of-Scope.
6. Die drei offenen Rückfragen (Knopf-Platzierung, Verhalten bei 0 AGs, „—“ für AG-lose Sub-FAs)
   sind vom Menschen an Schranke 1 zu entscheiden (siehe „Offene Rückfragen“) — der technische
   Entwurf unten ist so gebaut, dass beide Varianten je Frage ohne Datenmodell-Änderung umsetzbar
   sind (nur View-/JS-Unterschied).

## Technischer Lösungsentwurf

Referenzmuster: bestehende FA-Struktur-Baumanzeige (`FaHierarchyController.Index`,
`FaHierarchyTreeBuilder`, `Views/FaHierarchy/Index.cshtml` + `_FaHierarchyNode.cshtml`), Repository-
Pattern (ADR 0001), Epic [[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]] für die
`WorkOperation`/`ProductionWorkplace`-Felder.

**Begriffsklärung HauptFA vs. Wurzel-Sub-FA (verifiziert am Code):** `FaHierarchyNode.HauptFA` ist
die Strukturkennung (entspricht `ProductionOrder.OrderNumber`, nach der Hierarchie-Inversion nicht
mehr eindeutig je Zeile). Jeder materialisierte Knoten (auch die Wurzel) hat eine eigene
`SubFA`-Nummer (`!= 0`), die `ProductionOrder.SubOrderNumber` entspricht (verifiziert in
`WorkOperationStructureDetectionService.cs:104`: `var subOrder = node.SubFA.ToString();`, und
`FaHierarchyTreeBuilder.BuildNode`: die Wurzel ist nur über `VaterFA == null` definiert,
unabhängig von `SubFA`). Die „HauptFA-AG-Liste“ des Modals ist damit fachlich die AG-Liste des
**Wurzelknotens** der Struktur (`structure.Roots[0]`, dessen eigene `SubFA`) — beschriftet mit der
HauptFA-Nummer, wie im bestehenden Struktur-Header (`Index.cshtml:144`, `HauptFA @structure.HauptFA`).

**1) Bundled Read (kein N+1), `FaHierarchyController.Index`:**

Nach dem Bestimmen von `pageStructures` (bereits paginierte, sichtbare Strukturen) werden alle
materialisierten Knoten (`SubFA != 0`) der sichtbaren Strukturen rekursiv eingesammelt (ein
Hilfs-Walk über `structure.Roots` → `Children`, analog dem bestehenden rekursiven Aufbau in
`FaHierarchyTreeBuilder`). Die gesammelten `SubFA`-Werte (als `string`, wie im Epic) gehen in
**eine** neue Repository-Abfrage:

```csharp
Task<List<WorkOperation>> GetBySubOrderNumbersWithWorkplaceAsync(IReadOnlyCollection<string> subOrderNumbers);
```

Implementierung (`WorkOperationRepository`, analog `ProductionOrderRepository.GetBySubOrderNumbersAsync`):

```csharp
public async Task<List<WorkOperation>> GetBySubOrderNumbersWithWorkplaceAsync(IReadOnlyCollection<string> subOrderNumbers)
{
    if (subOrderNumbers == null || subOrderNumbers.Count == 0)
        return new List<WorkOperation>();
    var set = subOrderNumbers.ToList();
    return await _dbSet
        .AsNoTracking()
        .Include(wo => wo.ProductionOrder)
        .Include(wo => wo.ProductionWorkplace)
        .Where(wo => wo.ProductionOrder.SubOrderNumber != null && set.Contains(wo.ProductionOrder.SubOrderNumber))
        .OrderBy(wo => wo.ProductionOrder.SubOrderNumber).ThenBy(wo => wo.Sequence)
        .ToListAsync();
}
```

Das Ergebnis wird nach `ProductionOrder.SubOrderNumber` gruppiert (`Dictionary<string,
List<WorkOperationSummary>>`), auf `WorkOperationSummary(OperationNumber, Name,
ProductionWorkplace?.SageArbeitsplatznummer)` projiziert, und beim selben rekursiven Walk **je
Knoten** in die neue Property `FaHierarchyTreeNode.Arbeitsgaenge` gehängt (leere Liste, falls kein
Treffer). Damit trägt **jeder** materialisierte Baumknoten seine eigenen AGs direkt am
View-Model-Objekt, das die Views ohnehin schon rekursiv durchlaufen (`_FaHierarchyNode.cshtml`).

**Warum am Knoten und nicht an der Struktur:** `FaHierarchyTreeNode` ist bereits der Ort für
knotenspezifische, rein präsentative Zusatzdaten (`IconType`, siehe `FaNodeClassifier`) — die
AG-Liste ist naturgemäß dieselbe Art Zusatzdatum, nur diesmal aus einer Datenbankabfrage statt
berechnet. Kein neues Kopplungsobjekt nötig.

**2) Kein neuer Listen-View (ADR 0005):** Das Modal zeigt eine feste, kleine, bereits geladene
Teilmenge (typischerweise ein bis niedriger zweistelliger Bereich an AG-Zeilen je Struktur) ohne
eigene Pagination, Filterung oder Sortierbedürfnis — die Baumanzeige selbst bleibt der einzige
paginierte/gefilterte Listen-View (`viewKey FaHierarchyStructure`, unverändert). Das Modal ist
Detailansicht auf bereits serverseitig geladene Daten, kein zusätzlicher Datenzugriffspfad mit
eigenem Pagination-/Filter-Bedarf — analog den bestehenden Bootstrap-Modals der Anwendung
(`Views/Picking/Bom.cshtml`, `Views/PickingLeitstand/Index.cshtml`), die ebenfalls keine eigene
Listen-View-Struktur tragen.

**3) View/JS (frontend-design Pflicht — Modal-Umsetzung):**

- **EIN** gemeinsames Modal pro Seite (`#faArbeitsgaengeModal`, Bootstrap 5, `modal-dialog-scrollable`
  wegen potenziell mehrerer Sub-FA-Abschnitte), nicht ein Modal je Struktur — analog dem
  bestehenden Site-Muster (ein Modal-Shell, JS befüllt den Inhalt je Trigger, siehe
  `Views/Picking/Bom.cshtml`, `Views/PickingLeitstand/Index.cshtml`). Vermeidet DOM-Aufblähung bei
  vielen Strukturen auf einer Seite.
- Je Struktur wird der fertige Modal-**Inhalt** serverseitig in ein natives, unsichtbares
  `<template id="faArbeitsgaengeContent-@structure.HauptFA">`-Element gerendert (kein Escaping-
  Risiko wie bei einem `data-*`-HTML-Attribut, kein Client-seitiges Nachbauen der Tabellen — die
  Werte kommen 1:1 aus den bereits geladenen `FaHierarchyTreeNode.Arbeitsgaenge`-Listen).
- Der/die Knöpfe (Platzierung siehe Rückfrage 1) tragen `data-bs-toggle="modal"
  data-bs-target="#faArbeitsgaengeModal" data-content-id="faArbeitsgaengeContent-@structure.HauptFA"
  data-fa-title="FA @structure.HauptFA"`.
- `fa-hierarchy-tree.js` bekommt eine Klick-Delegation auf `#faTree` (analog dem bestehenden
  `.fa-toggle`-Muster, `table.addEventListener('click', ...)`): Klick auf einen AG-Knopf klont den
  Inhalt des referenzierten `<template>` in `#faArbeitsgaengeModal .modal-body`, setzt den Titel,
  öffnet das Modal über `bootstrap.Modal.getOrCreateInstance(...)`.
- Bootstrap-5-Modal, bestehende Muster (Konsistenz vor Eigenständigkeit), Kontrast WCAG AA (Tabellen
  im Modal nutzen dieselben Bootstrap-Table-/Badge-Klassen wie der Baum selbst, keine neuen Farben).

**4) Zugriffsschutz:** unverändert — `[RequirePickingOrTrackingOrLeitstandAccess]` (Class-Level,
Read). Kein Edit-Split nötig (reine Anzeige, kein neuer POST-Endpunkt — alles serverseitig beim
`Index`-Aufruf mitgeladen, kein AJAX-Endpunkt für das Modal).

## Migrations-/SQL-Auswirkungen

**Keine.** Reine Leseansicht auf bestehende Tabellen (`WorkOperation`, `ProductionWorkplace`,
`FaHierarchyNode`). Keine neue Spalte, kein neuer Index, keine Migration, `SQL/00_FreshInstall.sql`
unverändert.

## Audit-Feld-Auswirkungen

**Keine.** Die Ansicht schreibt nichts — weder `WorkOperation` noch `ProductionWorkplace` noch
`FaHierarchyNode` werden verändert. `ModifiedAt/ModifiedBy/ModifiedByWindows` bleiben unberührt.

## Akzeptanzkriterien

1. Auf `/FaHierarchy` öffnet ein Klick auf den AG-Knopf einer Struktur mit mindestens einem
   materialisierten Auftrag (`SubFA != 0`) ein Bootstrap-5-Modal mit Titel „FA `<HauptFA>`“.
2. Das Modal zeigt zuerst die AG-Liste des Wurzelknotens (HauptFA-Zeile), danach je materialisiertem
   Sub-FA (`SubFA != 0`, ohne die Wurzel) eine eigene AG-Liste mit Sub-FA-Nummer als Überschrift.
3. Je AG-Zeile sind `OperationNumber`, `Name` (Werkbank) und `SageArbeitsplatznummer` sichtbar.
4. Das Laden der Seite `/FaHierarchy` löst **genau eine** zusätzliche `WorkOperation`-Abfrage aus
   (unabhängig von der Anzahl sichtbarer Strukturen/Knöpfe auf der Seite) — verifizierbar per
   Logging/Query-Zählung im Dev-Lauf.
5. Kein Schreibzugriff möglich: Das Modal enthält keine Formularfelder, keine Buttons außer
   „Schließen“.
6. Nutzer ohne `[RequirePickingOrTrackingOrLeitstandAccess]`-Berechtigung sehen `/FaHierarchy`
   weiterhin gar nicht (Verhalten unverändert, keine Regression durch diese Änderung).
7. Strukturen ohne einen einzigen materialisierten Auftrag (nur Blatt-/Materialzeilen) zeigen keinen
   AG-Knopf.
8. Verhalten bei 0 erkannten AGs entspricht der Freigabe-Antwort zu Rückfrage 2.
9. Darstellung AG-loser Sub-FAs entspricht der Freigabe-Antwort zu Rückfrage 3.

## Test-Szenarien

Neues Kapitel **TS-78** in `docs/TESTSZENARIEN.md` (nächste freie Nummer, verifiziert 2026-09-23
gegen den Worktree-Stand `.claude/worktrees/2026-08-07-ideal-teile-1-5/docs/TESTSZENARIEN.md` —
höchstes vorhandenes Kapitel dort ist TS-77 aus dem vorausgesetzten Epic).

**Vorbedingungen:** Epic v1.44.0 gemerged/deployt (mindestens ein Struktur-Lauf mit erkannten
`WorkOperation`-Zeilen), Zugriff mit `[RequirePickingOrTrackingOrLeitstandAccess]`-Berechtigung.

**Szenario A — Modal zeigt konsolidierte AG-Liste:**
1. `/FaHierarchy` aufrufen, eine Struktur mit bekannten AGs (z. B. HauptFA mit `KA`/`SW`-Kürzeln)
   suchen.
2. AG-Knopf klicken.
3. Erwartung: Modal öffnet sich, Titel = HauptFA-Nummer, HauptFA-Zeile zuerst mit ihrer AG-Liste,
   danach je Sub-FA-Nummer der Struktur eine eigene AG-Liste, je AG-Zeile Kürzel/Name/Sage-Nummer
   sichtbar und korrekt (Abgleich gegen SyncLog-Einträge desselben Laufs).
4. Modal schließen (X, Escape, Klick außerhalb) — keine Datenänderung, Seite bleibt im selben
   Filter-/Pagination-Zustand.

**Szenario B — Struktur ohne materialisierten Auftrag:**
1. Struktur mit ausschließlich Blatt-/Materialzeilen (`SubFA == 0` überall) suchen/anlegen.
2. Erwartung: kein AG-Knopf sichtbar.

**Szenario C — 0 erkannte AGs (Negativfall, Verhalten gemäß Freigabe-Antwort 2):**
1. Struktur mit materialisiertem Auftrag, aber ohne erkannte `WorkOperation`-Zeilen (z. B. nur
   ausgeschlossene oder unbekannte Kürzel) suchen.
2. Erwartung: entsprechend der Freigabe-Entscheidung — Knopf ausgeblendet ODER Modal mit Hinweis
   „keine Arbeitsgänge erkannt“.

**Szenario D — Performance/Bundled Read:**
1. Seite mit mehreren Strukturen (z. B. `pageSize=50`) aufrufen.
2. Erwartung: genau eine zusätzliche `WorkOperation`-Abfrage in der Server-Query-Zählung (kein N+1
   je Struktur).

`secondbrain/tests/testszenarien-index.md` wird im selben Dev-Lauf um TS-78 ergänzt.

## Deploy

- **Web-App:** ja (Controller, Repository, Views, JS).
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:**

```bash
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

## Offene Rückfragen

1. Knopf **nur** auf der HauptFA-Struktur-Kopfzeile (ein konsolidiertes Modal für den ganzen Baum) —
   oder **zusätzlich** je Sub-FA-Zeile ein eigener Knopf (öffnet dasselbe konsolidierte Modal, aber
   mit Sprung/Scroll zum jeweiligen Sub-FA-Abschnitt)? Ändert nur die View-/JS-Platzierung, nicht
   das Datenmodell.
2. Verhalten bei **0 erkannten AGs** für einen Auftrag (weder am HauptFA noch an irgendeinem Sub-FA
   der Struktur): Knopf für diese Struktur ausblenden, oder anzeigen und im Modal „keine
   Arbeitsgänge erkannt“ ausgeben (Hinweis auf noch nicht gepflegte Werkbank/Kürzel)?
3. Sollen Sub-FAs **ohne** eigene AGs im Modal trotzdem mit „—“ gelistet werden (Vollständigkeit
   über den ganzen Baum erkennbar), oder erscheinen nur Sub-FAs **mit** mindestens einem erkannten
   AG (reine Trefferliste)?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. →
2. →
3. →
