---
type: spec
title: "FA-Struktur: erkannte BDE-Arbeitsgänge je (Sub-)FA sichtbar machen (Modal)"
slug: 2026-09-23-fa-struktur-arbeitsgaenge-anzeige-spec
status: Testbereit
created: 2026-09-23
updated: 2026-09-24
source_backlog: "[[2026-09-23-fa-struktur-arbeitsgaenge-anzeige]]"
depends_on: "[[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]]"
task: "[[2026-09-24-fa-struktur-arbeitsgaenge-anzeige-umsetzung]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - "IdealAkeWms/Data/Repositories/IWorkOperationRepository.cs — ZWEI neue Methoden: (1) `GetOrderNumbersWithWorkOperationsAsync(IReadOnlyCollection<string> orderNumbers)` -> HashSet<string>, schlanker Existenz-Check fuer den Seitenaufbau (nur Schluessel, keine Details); (2) `GetBySubOrderNumbersWithWorkplaceAsync(IReadOnlyCollection<string> subOrderNumbers)` -> Details fuer EINE Struktur beim Oeffnen des Modals"
  - "IdealAkeWms/Data/Repositories/WorkOperationRepository.cs — Implementierung, analog `ProductionOrderRepository.GetBySubOrderNumbersAsync` (AsNoTracking, `Include(ProductionOrder)`+`Include(ProductionWorkplace)`, `Where(SubOrderNumber != null && set.Contains(...))`, leere Liste bei leerem Input)"
  - "IdealAkeWms.Tests/Repositories/WorkOperationRepositoryTests.cs — Testfaelle fuer beide Methoden (InMemory) PLUS ein ToQueryString-Test, der belegt, dass das Contains der Existenz-Abfrage als EIN Parameter (OPENJSON, EF >= 8) uebersetzt wird"
  - "IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs — je Struktur `bool HatArbeitsgaenge` (Knopf-Sichtbarkeit, Antwort 2); NEUES ViewModel `FaArbeitsgaengeViewModel` fuer das Modal-Partial (HauptFA, Liste aller materialisierten Knoten mit ihren `WorkOperationSummary(OperationNumber, Name, SageArbeitsplatznummer)`-Eintraegen, Zaehler Sub-FAs gesamt / ohne Arbeitsgang). KEINE Arbeitsgaenge mehr am Seiten-Baum."
  - "IdealAkeWms/Controllers/FaHierarchyController.cs — `Index`: EINE schlanke Existenz-Abfrage ueber die HauptFA-Nummern der sichtbaren Strukturen -> `HatArbeitsgaenge` je Struktur. NEUE GET-Action `Arbeitsgaenge(int hauptFa)` -> PartialView `_FaArbeitsgaengeModal`: laedt NUR die Knoten dieser einen HauptFA (NICHT `_nodeRepository.GetAllAsync()`, das alle Strukturen laedt) plus deren Arbeitsgaenge; Baum-Aufbau ueber den bestehenden `FaHierarchyTreeBuilder`. Erbt den Class-Level-Zugriffsfilter. Neue DI-Abhaengigkeit `IWorkOperationRepository`"
  - "IdealAkeWms/Views/FaHierarchy/_FaArbeitsgaengeModal.cshtml — NEUES Partial: Zaehlzeile (N Sub-FAs, M ohne Arbeitsgang), HauptFA-Abschnitt (Wurzelknoten), danach ALLE materialisierten Sub-FAs, Zeilen ohne Arbeitsgang mit Strich und optisch zurueckgenommen. Serverseitig gerendert (Razor kodiert, kein Escaping-Risiko). `_FaHierarchyNode.cshtml` bleibt UNVERAENDERT (Antwort 1: kein Zeilen-Knopf)"
  - "IdealAkeWms/Views/FaHierarchy/Index.cshtml — AG-Knopf am ENDE der Kopfzeilen-Flex-Reihe (`Index.cshtml:135`), nur gerendert wenn `HatArbeitsgaenge`; EIN gemeinsames Bootstrap-5-Modal `#faArbeitsgaengeModal` je Seite (Shell ohne Inhalt). KEINE `<template>`-Elemente mehr — das Seiten-HTML enthaelt keine Modal-Inhalte"
  - "IdealAkeWms/wwwroot/js/fa-hierarchy-tree.js — Klick-Delegation auf `#faTree` (Muster `.fa-toggle`): Titel setzen, Ladezustand anzeigen, Partial per `fetch` holen und in `.modal-body` einsetzen; bei Fehler eine Meldung im Modal, NIE ein leeres Modal; `bootstrap.Modal.getOrCreateInstance(...)`"
  - "docs/TESTSZENARIEN.md — neues Kapitel TS-78 (naechste freie Nummer, verifiziert 2026-09-23 gegen den Worktree-Stand: hoechstes Kapitel TS-77)"
  - "secondbrain/tests/testszenarien-index.md — TS-78 nachziehen"
open_questions: []
beantwortete_rueckfragen:
  - "Knopf nur auf der HauptFA-Struktur-Kopfzeile (ein Klick oeffnet das konsolidierte Modal fuer den GANZEN Baum) — oder zusaetzlich je Sub-FA-Zeile ein eigener Knopf (oeffnet dasselbe konsolidierte Modal, aber mit Sprung/Scroll zum jeweiligen Sub-FA-Abschnitt)? Aendert nur die View/JS-Platzierung, nicht das Datenmodell."
  - "Verhalten bei 0 erkannten AGs fuer einen Auftrag (weder am HauptFA noch an irgendeinem Sub-FA der Struktur): Knopf fuer diese Struktur ganz ausblenden, oder anzeigen und im Modal „keine Arbeitsgaenge erkannt“ ausgeben?"
  - "Sollen Sub-FAs OHNE eigene AGs im Modal trotzdem mit „—“ gelistet werden (Vollstaendigkeit ueber den ganzen Baum), oder nur Sub-FAs MIT mindestens einem erkannten AG erscheinen (reine Trefferliste)?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: "Knopf nur an der HauptFA-Kopfzeile, ausgeblendet bei 0 Arbeitsgaengen; Modal listet ALLE Sub-FAs (ohne Arbeitsgang mit Strich) plus Zaehlzeile; Inhalt wird erst beim Oeffnen geladen (eine Struktur je Klick), Seitenaufbau nur schlanker Existenz-Check"
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-09-23
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
  Sub-FA (`SubFA != 0`) dessen eigene erkannte `WorkOperation`-Zeilen — und zwar **alle**
  materialisierten Sub-FAs, auch die **ohne** Arbeitsgang (mit Strich), dazu eine Zaehlzeile
  "N Sub-FAs · M ohne Arbeitsgang" (Antwort 3). Je AG: `OperationNumber`
  (Kürzel), `Name` (dient zugleich als Werkbank-Anzeige, siehe „Fachliche Anforderungen“),
  `ProductionWorkplace.SageArbeitsplatznummer`.
- Read-only — keine Bearbeitung, kein Anlegen/Löschen von `WorkOperation`-Zeilen aus dieser
  Ansicht (die AGs sind sync-geführt, siehe Epic).
- **Zweistufiges Laden (B2):** beim Seitenaufbau **ein** schlanker Existenz-Check (nur: hat eine
  Struktur Arbeitsgaenge?), die Details erst beim Oeffnen des Modals fuer **eine** Struktur. Kein N+1,
  kein Vorabladen von Modal-Inhalten.
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

1. Der Knopf erscheint **nur an der HauptFA-Kopfzeile** (Antwort 1) und **nur, wenn die Struktur
   mindestens einen erkannten Arbeitsgang hat** (Antwort 2) — sonst wird er gar nicht gerendert.
   Blatt-/Materialzeilen (`SubFA == 0`, Kaufteile/Material ohne eigenen FA) haben ohnehin keinen eigenen
   `WorkOperation`-Datensatz.
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

   **ERLEDIGT (2026-09-23):** Alle drei sind beantwortet, siehe Freigabe-Antworten. Die Punkte 7 und 8
   unten halten die Folgen fest.
7. **Vollstaendigkeit statt Trefferliste (Antwort 3).** Das Modal listet **alle** materialisierten
   Sub-FAs der Struktur, Zeilen ohne Arbeitsgang mit Strich und optisch zurueckgenommen, nicht
   versteckt. Oben eine Zaehlzeile "N Sub-FAs · M ohne Arbeitsgang". Begruendung: Die Frage, die in der
   Fertigung wehtut, ist nicht "wo wird gearbeitet", sondern **"welches Teil hat gar keinen
   Arbeitsgang"** — also am Terminal durchfaellt. Eine Trefferliste verschwiege genau diesen Fall.
8. **Laden beim Oeffnen (B2).** Die Seite enthaelt keine Modal-Inhalte; beim Klick wird genau eine
   Struktur geladen. Das begrenzt die Datenmenge von selbst, unabhaengig von der Seitengroesse.

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

**1) Zweistufig: schlanker Existenz-Check beim Seitenaufbau, Details erst beim Oeffnen
(ENTSCHIEDEN 2026-09-23, B2).**

**Warum nicht mehr vorab laden:** Der urspruengliche Entwurf renderte fuer JEDE sichtbare Struktur
ein verstecktes `<template>` mit allen Arbeitsgaengen. Das war vertretbar, solange das Modal nur
Treffer zeigte. Durch Antwort 3 enthaelt es jetzt den **vollstaendigen** Baum aller Sub-FAs — vorab fuer
alle Strukturen einer Seite gerendert, waechst das Seiten-HTML mit der Seitengroesse, bei "Alle"
unbegrenzt. Die damalige Entscheidung gegen einen Endpunkt war unter den damaligen Voraussetzungen
richtig; Antwort 3 hat die Voraussetzung geaendert.

**Stufe 1 — Seitenaufbau (`Index`):** Die Seite muss nur wissen, ob eine Struktur **ueberhaupt**
erkannte Arbeitsgaenge hat (Antwort 2: Knopf ausblenden bei 0). Dafuer **eine** schlanke Abfrage ueber
die HauptFA-Nummern der sichtbaren Strukturen, die **nur Schluessel** liefert:

```csharp
Task<HashSet<string>> GetOrderNumbersWithWorkOperationsAsync(IReadOnlyCollection<string> orderNumbers);
// WorkOperation JOIN ProductionOrder, Where(set.Contains(po.OrderNumber)),
// Select(po.OrderNumber).Distinct()
```

Schluessel ist die **HauptFA** (`ProductionOrder.OrderNumber`, siehe Begriffsklaerung) — hoechstens ein
Wert je sichtbarer Struktur, nicht je Sub-FA. Ergebnis -> `bool HatArbeitsgaenge` je Struktur. Das
Seiten-HTML enthaelt **keine** Arbeitsgaenge und keine Sub-FA-Listen.

**Stufe 2 — Oeffnen des Modals:** `GET /FaHierarchy/Arbeitsgaenge?hauptFa={n}` -> PartialView
`_FaArbeitsgaengeModal`. Die Action laedt
(a) die **eine** Struktur dieser HauptFA — **nur deren Knoten**, gefiltert nach `HauptFA`.
**Achtung, Falle:** Der bestehende Lesepfad der Baumanzeige baut den Baum aus
`_nodeRepository.GetAllAsync()` (`FaHierarchyController.cs:64/78`) — also aus **allen** Knoten aller
Strukturen. Ihn fuer den Endpunkt wiederzuverwenden hiesse, bei **jedem Klick** alles zu laden, um eine
einzige Struktur zu zeigen — genau die Last, die das Laden beim Oeffnen vermeiden soll.
**Verbindlich:** Knoten per `HauptFA` filtern (vorhandene Repository-Methode nutzen, falls es sie gibt;
sonst eine kleine neue `GetByHauptFaAsync`). Wiederverwendet wird nur der **Baum-Aufbau**
(`FaHierarchyTreeBuilder`), nicht die Ladeabfrage;
(b) deren Arbeitsgaenge ueber

```csharp
Task<List<WorkOperation>> GetBySubOrderNumbersWithWorkplaceAsync(IReadOnlyCollection<string> subOrderNumbers);
```

(Implementierung wie urspruenglich entworfen: `AsNoTracking`, `Include(ProductionOrder)` +
`Include(ProductionWorkplace)`, `Where(SubOrderNumber != null && set.Contains(...))`, sortiert nach
`SubOrderNumber`, dann `Sequence`; leere Liste bei leerem Input) — jetzt aber nur mit den Sub-FAs
**einer** Struktur.

Daraus baut die Action das `FaArbeitsgaengeViewModel`: **alle** materialisierten Knoten (`SubFA != 0`)
der Struktur, je Knoten seine `WorkOperationSummary`-Eintraege (leer -> Strich), dazu die Zaehler
"Sub-FAs gesamt" und "ohne Arbeitsgang" (Antwort 3). Der Wurzelknoten fuehrt den HauptFA-Abschnitt an.

**Wurzelknoten (O1, vom Menschen bestaetigt 2026-09-23):** Der Wurzelknoten **kann** eigene Arbeitsschritte
tragen. Der Begriff "HauptFA-AG-Liste" ist damit korrekt. Traegt er im Einzelfall keine, zeigt der
HauptFA-Abschnitt einen Strich — dieselbe Regel wie fuer die Sub-FAs.

**Begrenzung ergibt sich von selbst:** Je Klick genau **eine** Struktur. Eine globale Obergrenze fuer
das Modal ist damit nicht noetig.

**EF-`Contains` (B2a):** Stufe 2 fragt nur die Sub-FAs eines Baums ab — klein. Stufe 1 fragt die
HauptFA-Nummern **aller** sichtbaren Strukturen ab; bei "Alle" koennen das viele sein. EF Core ab
Version 8 uebersetzt `Contains` ueber eine Liste als **einen** `OPENJSON`-Parameter und umgeht damit die
2100-Parameter-Grenze. **Nicht annehmen, sondern belegen:** ToQueryString-Test (AK 10).

**2) Kein neuer Listen-View (ADR 0005):** Das Modal zeigt eine feste, beim Oeffnen geladene
Teilmenge (**eine** Struktur, durch Antwort 3 inklusive aller Sub-FAs) ohne
eigene Pagination, Filterung oder Sortierbedürfnis — die Baumanzeige selbst bleibt der einzige
paginierte/gefilterte Listen-View (`viewKey FaHierarchyStructure`, unverändert). Das Modal ist
Detailansicht **einer** Struktur, geladen ueber einen schlanken Detail-Endpunkt ohne eigenen
Pagination-/Filter-Bedarf — analog den bestehenden Bootstrap-Modals der Anwendung
(`Views/Picking/Bom.cshtml`, `Views/PickingLeitstand/Index.cshtml`), die ebenfalls keine eigene
Listen-View-Struktur tragen.

**3) View/JS (frontend-design Pflicht — Modal-Umsetzung):**

- **EIN** gemeinsames Modal pro Seite (`#faArbeitsgaengeModal`, Bootstrap 5, `modal-dialog-scrollable`
  wegen potenziell mehrerer Sub-FA-Abschnitte), nicht ein Modal je Struktur — analog dem
  bestehenden Site-Muster (ein Modal-Shell, JS befüllt den Inhalt je Trigger, siehe
  `Views/Picking/Bom.cshtml`, `Views/PickingLeitstand/Index.cshtml`). Vermeidet DOM-Aufblähung bei
  vielen Strukturen auf einer Seite.
- Der Modal-**Inhalt** wird **nicht** mehr in der Seite vorgerendert (keine `<template>`-Elemente), sondern
  beim Oeffnen als **serverseitig gerendertes Partial** geholt. Razor kodiert die Werte — kein
  Escaping-Risiko, kein clientseitiges Nachbauen von Tabellen.
- **Knopf-Platzierung und -Sichtbarkeit (Antworten 1, 2; S1 am Markup entschieden):** Genau **ein** Knopf
  je Struktur, am **Ende** der Kopfzeilen-Flex-Reihe (`d-flex align-items-center flex-wrap gap-2`,
  `Index.cshtml:135`). Bei `HatArbeitsgaenge == false` wird er **gar nicht gerendert** — am Zeilenende
  einer Flex-Reihe mit `gap` entsteht dadurch kein Layout-Sprung. **Kein** Zeilen-Knopf an Sub-FAs,
  `_FaHierarchyNode.cshtml` bleibt unveraendert. Attribute: `data-hauptfa="@structure.HauptFA"`.
- `fa-hierarchy-tree.js` bekommt eine Klick-Delegation auf `#faTree` (Muster `.fa-toggle`,
  `table.addEventListener('click', ...)`). Ablauf je Klick: Titel "FA <HauptFA>" setzen, **Ladezustand**
  in `.modal-body` anzeigen, Modal oeffnen (`bootstrap.Modal.getOrCreateInstance(...)`), Partial per
  `fetch` holen und einsetzen. **Bei Fehler** erscheint im Modal eine Meldung ("Arbeitsgaenge konnten nicht
  geladen werden") — **nie** ein leeres Modal, das wie "keine Arbeitsgaenge" aussaehe.
- **Modal-Inhalt (Antwort 3):** oben die **Zaehlzeile** "N Sub-FAs · M ohne Arbeitsgang", damit die
  Luecke ohne Scrollen sichtbar ist; dann der HauptFA-Abschnitt (Wurzelknoten), dann **alle**
  materialisierten Sub-FAs. Sub-FAs ohne Arbeitsgang mit Strich, **optisch zurueckgenommen, nicht
  versteckt**.
- Bootstrap-5-Modal, bestehende Muster (Konsistenz vor Eigenständigkeit), Kontrast WCAG AA (Tabellen
  im Modal nutzen dieselben Bootstrap-Table-/Badge-Klassen wie der Baum selbst, keine neuen Farben).

**4) Zugriffsschutz:** unveraendert — `[RequirePickingOrTrackingOrLeitstandAccess]` (Class-Level,
Read). Die **neue GET-Action `Arbeitsgaenge`** liegt auf demselben Controller und **erbt** den Filter.
Reine Leseaktion — kein POST, kein Antiforgery-Token, kein Edit-Split noetig. Am Code bestaetigen, dass
der Filter tatsaechlich auf Klassenebene sitzt und keine Action ihn aussetzt.

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
2. Das Modal zeigt oben die Zaehlzeile "N Sub-FAs · M ohne Arbeitsgang", dann die AG-Liste des
   Wurzelknotens (HauptFA-Abschnitt), danach **alle** materialisierten Sub-FAs (`SubFA != 0`, ohne die
   Wurzel) mit Sub-FA-Nummer als Überschrift — **auch** solche ohne Arbeitsgang, diese mit Strich und
   optisch zurueckgenommen.
3. Je AG-Zeile sind `OperationNumber`, `Name` (Werkbank) und `SageArbeitsplatznummer` sichtbar.
4. **Seitenaufbau:** `/FaHierarchy` loest **genau eine** zusaetzliche Abfrage aus — den schlanken
   Existenz-Check `GetOrderNumbersWithWorkOperationsAsync` —, unabhaengig von der Anzahl sichtbarer
   Strukturen. Das Seiten-HTML enthaelt **keine** Arbeitsgaenge und keine Sub-FA-Listen.
   **Oeffnen:** Ein Klick laedt die Knoten und Arbeitsgaenge genau **einer** Struktur — **nicht**
   `GetAllAsync()`. Verifizierbar per Query-Zaehlung im Dev-Lauf.
5. Kein Schreibzugriff möglich: Das Modal enthält keine Formularfelder, keine Buttons außer
   „Schließen“.
6. Nutzer ohne `[RequirePickingOrTrackingOrLeitstandAccess]`-Berechtigung sehen `/FaHierarchy`
   weiterhin gar nicht (Verhalten unverändert, keine Regression durch diese Änderung).
7. Strukturen ohne einen einzigen materialisierten Auftrag (nur Blatt-/Materialzeilen) zeigen keinen
   AG-Knopf.
8. **0 erkannte Arbeitsgaenge:** Hat eine Struktur keinen einzigen erkannten Arbeitsgang, wird der Knopf
   **nicht gerendert** (Antwort 2, S1). Kein Layout-Sprung in der Kopfzeile.
9. **Vollstaendigkeit:** Die Zaehlzeile stimmt mit der Liste ueberein — N = Anzahl aller
   materialisierten Sub-FAs der Struktur, M = Anzahl derer ohne Arbeitsgang (Antwort 3).
10. **EF-`Contains` belegt:** Ein ToQueryString-Test zeigt, dass das `Contains` des Existenz-Checks als
    **ein** Parameter (`OPENJSON`) uebersetzt wird — keine 2100-Parameter-Grenze, auch bei "Alle".
11. **Kein leeres Modal:** Waehrend des Ladens zeigt das Modal einen Ladezustand; schlaegt das Laden
    fehl, eine Fehlermeldung. Ein leeres Modal, das wie "keine Arbeitsgaenge" aussaehe, darf nicht
    auftreten.
12. **Wurzelknoten (O1):** Traegt der Wurzelknoten eigene Arbeitsschritte, erscheinen sie im
    HauptFA-Abschnitt; traegt er keine, steht dort ein Strich.

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
3. Erwartung: Modal oeffnet sich, Titel = HauptFA-Nummer, oben die Zaehlzeile "N Sub-FAs · M ohne
   Arbeitsgang", dann HauptFA-Abschnitt, dann **alle** Sub-FAs der Struktur — solche ohne Arbeitsgang mit
   Strich. Je AG-Zeile Kuerzel/Name/Sage-Nummer sichtbar und korrekt (Abgleich gegen SyncLog-Eintraege
   desselben Laufs); die Zaehlzeile stimmt mit der Liste ueberein.
4. Modal schließen (X, Escape, Klick außerhalb) — keine Datenänderung, Seite bleibt im selben
   Filter-/Pagination-Zustand.

**Szenario B — Struktur ohne materialisierten Auftrag:**
1. Struktur mit ausschließlich Blatt-/Materialzeilen (`SubFA == 0` überall) suchen/anlegen.
2. Erwartung: kein AG-Knopf sichtbar.

**Szenario C — 0 erkannte AGs (Negativfall):**
1. Struktur mit materialisiertem Auftrag, aber ohne erkannte `WorkOperation`-Zeilen (z. B. nur
   ausgeschlossene oder unbekannte Kuerzel) suchen.
2. Erwartung: **kein** AG-Knopf in der Kopfzeile, die uebrigen Kopfzeilen-Elemente stehen unveraendert
   (kein Layout-Sprung).

**Szenario D — Zweistufiges Laden:**
1. Seite mit mehreren Strukturen (z. B. `pageSize=50`) aufrufen.
2. Erwartung: genau **eine** zusaetzliche Abfrage (Existenz-Check) in der Server-Query-Zaehlung; der
   Seitenquelltext enthaelt **keine** Arbeitsgaenge.
3. Ein Modal oeffnen: Erwartung — nur Knoten und Arbeitsgaenge dieser **einen** Struktur werden
   nachgeladen, **nicht** alle Knoten (`GetAllAsync()`).

**Szenario E — Grosse Seite:**
1. Seitengroesse "Alle" waehlen.
2. Erwartung: Seite laedt ohne spuerbare Verzoegerung durch die Arbeitsgaenge; ein beliebiges Modal
   oeffnet sich korrekt.

**Szenario F — Fehler beim Laden:**
1. Modal fuer eine HauptFA oeffnen, deren Struktur nicht (mehr) existiert, oder den Endpunkt
   unerreichbar machen.
2. Erwartung: Fehlermeldung im Modal — **kein** leeres Modal.

`secondbrain/tests/testszenarien-index.md` wird im selben Dev-Lauf um TS-78 ergänzt.

## Deploy

- **Web-App:** ja (Controller, Repository, Views, JS).
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:**

```bash
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

## Offene Rückfragen — ALLE BEANTWORTET (2026-09-23)

> Beantwortet im Abschnitt "Freigabe-Antworten"; zusammen mit den Befunden der Kritischen Pruefung (B1,
> B2, S1, O1) in Umfang, Fachliche Anforderungen, Technischen Loesungsentwurf, Akzeptanzkriterien und
> Testszenarien eingearbeitet. Nur noch **Protokoll**, kein Auftrag. **Massgeblich ist der Rumpf.**

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

1. → **Nur auf der HauptFA-Kopfzeile — ein konsolidiertes Modal.**
   Die Arbeitsgaenge eines Geraets sind ueber den Baum verteilt; der Sinn der Ansicht ist die
   **Gesamtschau**. Ein Knopf je Sub-FA-Zeile zeigte immer nur einen Ausschnitt und erzeugte bei 39
   Sub-FAs 39 Knoepfe — mehr Bedienelemente als Information.
   Und die Struktur-Ansicht ist bereits ein Tree-Table mit Chevrons, Badges und Spaltenfiltern; jede
   weitere Zeilen-Schaltflaeche kostet dort Ruhe.
   Zeigt sich im Betrieb, dass jemand gezielt **einen** Sub-FA sehen will, ist ein Zeilen-Knopf spaeter
   nachruestbar — mit Beleg statt auf Vorrat.

2. → **Knopf ausblenden.**
   Ein Bedienelement, das nichts als „nichts gefunden" liefert, ist schlechter als keines — der
   Anwender klickt, wartet und lernt nichts.
   **Aber der Fall darf nicht unsichtbar werden**, denn er hat eine **Ursache**: Fuer diese Struktur ist
   noch keine Werkbank oder kein Kuerzel gepflegt — genau das, was der Sync bereits als
   Unbekannt-Meldung protokolliert. Das ist der richtige Ort dafuer, nicht ein leeres Modal, das
   jeder Anwender einzeln entdeckt.
   *Falls der Knopf ohnehin in der Kopfzeile neben anderen Elementen steht und sein Verschwinden das
   Layout springen liesse:* dann stattdessen **ausgegraut mit Titel-Text** „keine Arbeitsgaenge
   erkannt" — gleicher Informationswert, ohne Klick ins Leere. Der Dev-Lauf entscheidet das am
   tatsaechlichen Layout.

3. → **ALLE Sub-FAs listen, auch die ohne Arbeitsgang — mit „—".** Das ist die wichtigste der drei
   Antworten.
   Eine reine Trefferliste beantwortet nur „wo wird gearbeitet". Die Frage, die in der Fertigung
   wehtut, ist die andere: **„Welches Teil hat noch gar keinen Arbeitsgang?"** — also welches faellt
   am Terminal durch, weil sein Kuerzel unbekannt ist oder die Werkbank fehlt.
   Eine Trefferliste **verschweigt genau diesen Fall**: Das Teil erscheint nicht, und niemand merkt,
   dass es fehlt. Dieselbe Klasse wie die gefilterte Liste, die vollstaendig aussieht.
   **Verbindlich:** Das Modal zeigt den **vollstaendigen** Baum aus Sub-FAs, Zeilen ohne Arbeitsgang
   mit „—". Eine **Zaehlzeile** oben nennt beide Zahlen (z. B. „39 Sub-FAs · 12 ohne Arbeitsgang"),
   damit die Luecke ohne Scrollen sichtbar ist.
   *Nicht noetig, aber naheliegend:* Zeilen mit „—" optisch zuruecknehmen, damit die Treffer weiter
   gut lesbar bleiben — zuruecknehmen, nicht verstecken.

## Kritische Pruefung (2026-09-23)

> Anwalt-des-Teufels-Durchsicht **vor** dem Dev-Lauf. Die drei Freigabe-Antworten stehen im Block,
> aber **noch nicht im Rumpf** — Antwort 3 erweitert den Umfang spuerbar. Am Code (Worktree
> `feature/2026-08-07-ideal-teile-1-5`) gegengeprueft. Zwei Punkte sind **sauber**, zwei sind BLOCKER,
> einer eine am Markup getroffene Festlegung, einer eine Datenfrage, die nur der Mensch/eine
> Testinstanz beantwortet.

### Sauber (bestaetigt)

- **Zugriffsschutz (Schwerpunkt 5):** `FaHierarchyController` traegt `[RequirePickingOrTrackingOrLeitstandAccess]`
  auf **Klassenebene** (`FaHierarchyController.cs:23`), die `Index`-Action (Z.49) ist abgedeckt. Es gibt
  **keine** neue Action (Modal wird im `Index` serverseitig mitgerendert, kein AJAX-Endpunkt) — die
  Spec-Aussage stimmt. Nichts zu tun.
- **Datenverfuegbarkeit fuer Antwort 3 (Schwerpunkt 1, Teil „braucht es die Strukturknoten?"):** Der Baum
  wird bereits **vollstaendig serverseitig** gebaut — `_treeBuilder.Build(_nodeRepository.GetAllAsync()…)`
  (`FaHierarchyController.cs:64/78`). Die **komplette** Sub-FA-Liste je Struktur liegt damit schon in
  `structure.Roots`→`Children` vor. `GetBySubOrderNumbersWithWorkplaceAsync` muss sie **nicht** holen
  (die Methode liefert nur die WorkOperations der Treffer-Sub-FAs); die „alle Sub-FAs, auch ohne AG"-Liste
  aus Antwort 3 kommt aus dem ohnehin vorhandenen Baum-Walk. **Keine zweite Rundreise noetig.** ✓

### BLOCKER

**B1 — Antwort 3 ist nicht im Rumpf; der Rumpf beschreibt noch eine Trefferliste.** Antwort 3 macht aus
der reinen Treffer-Ansicht eine **Vollstaendigkeits-Ansicht**: ALLE Sub-FAs des Baums, auch die **ohne**
Arbeitsgang mit „—", plus eine **Zaehlzeile** oben („39 Sub-FAs · 12 ohne Arbeitsgang"), „—"-Zeilen
optisch zurueckgenommen. Der Rumpf sagt aber noch durchgaengig „je Sub-FA **dessen erkannte**
`WorkOperation`-Zeilen" (In-Scope Z.66-69, Fachliche Anforderung 3 Z.93-98, AK 2 Z.219-220, Szenario A
Z.247-248) — als waeren nur Treffer gemeint. **Datentechnisch tragbar** (siehe „Sauber" oben: der volle
Baum liegt vor), aber In-Scope, Fachliche Anforderung 3, die AK und TS-78 muessen auf „alle Sub-FAs +
Zaehlzeile + ‚—'-Zeilen" umgeschrieben werden. Solange das nur im Antwortblock steht, baut der Dev-Lauf
die alte Trefferliste.

**B2 — Keine Obergrenze; „kein N+1" beantwortet die Mengenfrage nicht (Schwerpunkt 2).** Nachgerechnet:
- **N+1 ist korrekt vermieden** — **eine** Abfrage je Seite, das Ergebnis wird beim Baum-Walk je Knoten
  angehaengt, keine Abfrage je Struktur/Knopf. AK 4 stimmt insoweit.
- **Aber es gibt keine Obergrenze.** `PageSize.Resolve` erlaubt „Alle" (0) → **`AllCap = 5000`**
  Strukturen je Seite (`PageSize.cs:12`). Der Bundled-Read sammelt die Sub-FA-Nummern **aller** sichtbaren
  Strukturen in eine `set.Contains(...)`-IN-Liste: bei „Alle" × ~39 Sub-FAs sind das bis zu
  **~195.000** Werte in **einer** Abfrage. Zweitens — und akuter — rendert der Entwurf **je Struktur** ein
  serverseitiges `<template>` mit **allen** Sub-FA-Zeilen (Antwort 3): bei 5000 Strukturen entstehen
  Zehntausende versteckter Template-Zeilen im ausgelieferten HTML. „Eine Abfrage" ist erfuellt, die
  **Seitenlast ist unbegrenzt**.
  → **Zu entscheiden (Mensch/Dev):** (a) EF-Core-10-Uebersetzung von `Contains` auf grosse Listen
  verifizieren — seit EF 8 wird das ueber `OPENJSON` als **ein** Parameter uebersetzt, umgeht also die
  2100-Parameter-Grenze; das ist zu **bestaetigen**, nicht anzunehmen. (b) Eine sinnvolle Grenze fuer die
  Modal-Inhalte festlegen: entweder das `<template>` je Struktur **erst beim ersten Oeffnen** per kleinem
  Endpunkt laden (bewusst gegen die „kein AJAX-Endpunkt"-Entscheidung abzuwaegen), oder die
  Modal-Funktion bei „Alle"/sehr grossen Seiten deaktivieren/deckeln. So oder so gehoert eine
  **Obergrenz-Aussage + ein Mengen-AK** in den Rumpf — heute fehlt beides.

### FESTLEGUNG statt Wahl (Schwerpunkt 3, Antwort 2)

**S1 — Am Markup entschieden: Knopf AUSBLENDEN, kein Ausgegraut-Fallback.** Antwort 2 laesst „ausblenden
ODER ausgegraut, je nach Layout-Sprung" offen und delegiert an den Dev-Lauf. Am tatsaechlichen Markup
verifiziert: die Struktur-Kopfzeile ist eine `d-flex align-items-center flex-wrap gap-2`-Reihe
(`Index.cshtml:135`) mit variabler Badge-Zahl (Kunde/Status/KO/FE/Abteilung/Verwaist). Ein AG-Knopf **am
Ende** dieser Reihe kann per `display:none` entfallen, **ohne** Layout-Sprung — Flexbox mit `gap`
hinterlaesst am Zeilenende kein Loch, und nachfolgende Elemente gibt es dort nicht. Damit greift Antwort
2s Primaerfall („ausblenden") vorbehaltlos; der Ausgegraut-Zweig ist hier gegenstandslos. **In den Rumpf
als Festlegung** („Knopf am Ende der Kopfzeilen-Flex-Row, bei 0 AGs `display:none`"), die
„Dev-Lauf entscheidet"-Formulierung streichen.

### OFFEN — muss vom Menschen/einer Testinstanz kommen (Schwerpunkt 4)

**O1 — Traegt der Wurzelknoten selbst Arbeitsschritte, oder ist seine AG-Liste praktisch leer?** Der Rumpf
setzt „HauptFA-AG-Liste = AG-Liste des Wurzelknotens" (Z.124-126). Ob der Wurzelknoten (`VaterFA IS NULL`)
eigene `Arbeitsschritte` traegt und damit ueberhaupt eigene `WorkOperation`s bekommt, ist eine **Datenfrage**
— am Code **nicht** verifizierbar (`FaHierarchyNode` ist in **keiner** von dieser Session erreichbaren DB
angelegt; die Ableitung im Epic haengt an den realen Sage-Daten). **Falls die Wurzel-Liste in der Praxis
leer ist**, ist der Begriff „HauptFA-AG-Liste" missverstaendlich (er verspricht AGs, die dort nie stehen —
die Arbeit sitzt auf den Sub-FAs). Dann sollte der Rumpf sagen: die HauptFA-Zeile zeigt ihre eigene Liste
(ggf. „—"), der Inhalt sind die Sub-FA-Abschnitte. **Bitte auf der befuellten IDEAL-Instanz pruefen**
(`SELECT Arbeitsschritte FROM FaHierarchyNode WHERE VaterFA IS NULL`) und den Begriff im Rumpf entsprechend
schaerfen — nicht raten.

### Empfehlung

**NACHBESSERUNG NOETIG:** B1 (Antwort 3 — Vollstaendigkeits-Ansicht mit Zaehlzeile/‚—' in Rumpf, AK, TS
ziehen), B2 (Obergrenze + Mengen-AK festlegen, EF-Contains-Uebersetzung bestaetigen), S1 (Knopf-Ausblenden
als Festlegung statt Dev-Lauf-Wahl) und O1 (Wurzel-AG-Liste auf der Testinstanz klaeren, Begriff schaerfen).
Der Kern — bundled Read, Zugriffsschutz, „am Knoten statt an der Struktur" — traegt; die Nachbesserung
betrifft den durch Antwort 3 gewachsenen Umfang und die fehlende Mengengrenze.

## Umsetzung (Dev-Lauf 2026-09-24) — Abweichungen vom Entwurf, begründet

Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Commits `912e6bea` (Repository + Tests),
`71ffeb18` (feature-complete, v1.45.0), `2d06d1f0` (Review-Fix).

1. **Englische Code-Namen** (CLAUDE.md-Sprachregel hat Vorrang vor den deutschen Arbeitsnamen der Spec):
   `HatArbeitsgaenge` → `FaHierarchyStructure.HasWorkOperations`, Action `Arbeitsgaenge` → `WorkOperations`
   (Route `GET /FaHierarchy/WorkOperations?hauptFa=`), `FaArbeitsgaengeViewModel` → `FaWorkOperationsViewModel`,
   Partial `_FaArbeitsgaengeModal` → `_FaWorkOperationsModal`, Modal-Id `#faWorkOperationsModal`. UI-Texte deutsch.
2. **Knoten-Laden:** keine neue Methode nötig — `IFaHierarchyNodeRepository.GetByHauptFaAsync` existierte
   bereits (inkl. 5-min-Cache-Decorator). `GetAllAsync()` wird im Endpunkt **nicht** verwendet.
3. **Stufe-2-Abfrage per `OrderNumber` statt Sub-FA-Liste (Code-Review-Befund):** Die entworfene
   `GetBySubOrderNumbersWithWorkplaceAsync(subFas)` hätte einen anderen Schlüssel benutzt als der
   Existenz-Check (`OrderNumber`). Folge: Hat nur eine aus dem Import entfallene Sub-FA (`SageMissingSince`)
   — oder eine hinter Tiefen-Cap/Zyklus — Arbeitsgänge, erscheint der Knopf, das Modal zeigt aber nur
   Striche. Jetzt `GetByOrderNumberWithWorkplaceAsync(hauptFa)` (derselbe Schlüssel wie der Knopf);
   Arbeitsgänge ohne Baum-Knoten meldet das Modal als gelbe Warnung (`UnmatchedOperations`, TS-78.9) —
   Melden statt still behandeln.
4. **Zählzeile:** N und M zählen die Sub-FAs **ohne** die Wurzel (die Wurzel steht als HauptFA-Abschnitt
   separat und zeigt bei fehlendem AG ebenfalls „— kein Arbeitsgang"). Bei M = 0 steht „· alle mit Arbeitsgang".
5. **AK 10 gemessen:** EF Core 10 übersetzt `Contains` über 5000 Schlüssel in **einen** Parameter
   `@set nvarchar(max)` + `OPENJSON` (Testausgabe im Lauf gesehen) — Dauerwissen in [[fallstricke]] §8.
6. **Kein lokaler Start / kein Screenshot:** `Program.cs` ruft `Database.Migrate()` gegen AKESQL20, der
   Bündel-Zweig trägt nicht eingespielte Migrationen ([[fallstricke]] §8). Alles Sichtbare ist Manual-UAT (TS-78).

## QA-Nachweis (2026-09-24)

**Umgebung:** Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch
`feature/2026-08-07-ideal-teile-1-5`, HEAD `2d06d1f0`. Web-App bewusst **nicht** gestartet
(`Program.cs` → `Database.Migrate()` gegen AKESQL20, siehe Abweichung 6 oben).

**1) Build:**
```
dotnet build IdealAkeWms.slnx
→ Der Buildvorgang wurde erfolgreich ausgeführt. 12 Warnung(en) (bestehend, unverändert durch diesen
  Diff — CS8602/CS8321 in TrackingController/FaWorklist/PickingLeitstand/ProductionOrders, NU1902
  MailKit/MimeKit), 0 Fehler.
```

**2) Test (alle Suiten):**
```
dotnet test IdealAkeWms.slnx
→ IdealAkeWms.Tests.dll:        Fehler: 0, erfolgreich: 1398, übersprungen: 1, gesamt: 1399
  (Skip: ProductionOrderEagerCreateAgentJobTests.EagerCreate_… — bestehend, SQL-Server-Integrationstest)
→ IDEALAKEWMSService.Tests.dll: Fehler: 0, erfolgreich: 268, übersprungen: 0, gesamt: 268
```

**3) AK-10-Beleg gesondert bestätigt:**
```
dotnet test IdealAkeWms.Tests --filter FullyQualifiedName~OrderNumbersWithWorkOperationsQuery_TranslatesContainsAsSingleParameter
→ Bestanden IdealAkeWms.Tests.Repositories.WorkOperationRepositoryFaStructureTests.
  OrderNumbersWithWorkOperationsQuery_TranslatesContainsAsSingleParameter — 1/1 gruen
  (5000 Schluessel → EIN `@set nvarchar(max)` + OPENJSON, kein Mehrfach-Parameter-Modus)
```

**4) Code gegen AK 1–12 geprüft (Diff `f829d00d..2d06d1f0`, 14 Dateien, nur `IdealAkeWms/` +
Versions-Mirror `IDEALAKEWMSService/AppVersion.cs`):**

| AK | Fundstelle | Befund |
|---|---|---|
| 1 | `Index.cshtml:190-197` (`structure.HasWorkOperations`), `FaHierarchyController.WorkOperations` | Knopf nur bei `HasWorkOperations`, Titel "FA `<HauptFA>`" im Modal per JS gesetzt |
| 2 | `_FaWorkOperationsModal.cshtml` | Zaehlzeile + HauptFA-Abschnitt zuerst, dann Sub-FAs |
| 3 | `WorkOperationSummary(OperationNumber, Name, SageArbeitsplatznummer)` | genau die drei Angaben je AG-Zeile |
| 4 | `FaHierarchyController.Index` — genau ein `GetOrderNumbersWithWorkOperationsAsync`-Aufruf je Seitenaufbau; `WorkOperations`-Action nutzt `GetByHauptFaAsync` (nicht `GetAllAsync`) | bestaetigt |
| 5 | `_FaWorkOperationsModal.cshtml` | keine Formularfelder, nur "Schliessen" |
| 6 | `[RequirePickingOrTrackingOrLeitstandAccess]` auf Klassenebene, `WorkOperations` ist neue Action ohne eigenes Attribut → erbt | bestaetigt |
| 7 | Knopf nur bei `HasWorkOperations` (Blattzeilen ohne `WorkOperation` liefern nie einen Treffer im Existenz-Check) | bestaetigt |
| 8 | `Index.cshtml:188-197` — `@if (structure.HasWorkOperations)` rendert den Knopf gar nicht (kein `display:none`-Fallback noetig, Flex-Reihe mit `gap`) | bestaetigt, S1 umgesetzt |
| 9 | `FaWorkOperationsViewModel.SubFaCount`/`SubFaWithoutOperationsCount`, Test `FaWorkOperationsViewModelTests` | Zaehler aus demselben Baum-Walk wie die Liste — kann nicht auseinanderlaufen |
| 10 | `OrderNumbersWithWorkOperationsQuery_TranslatesContainsAsSingleParameter` (gruen, s. o.) | belegt statt angenommen |
| 11 | `fa-hierarchy-tree.js` `openWorkOperations()` — Ladezustand vor `fetch`, `catch` setzt Fehlermeldung, `r.redirected`/`!r.ok` wirft (verhindert Access-Denied-Seite im Modal) | bestaetigt, nie leeres Modal |
| 12 | `FaWorkOperationsViewModel.Create` — Wurzel (`VaterFA == null`) wird immer als Eintrag aufgenommen, auch ohne eigene AGs (`Operations = new()` → "— kein Arbeitsgang") | bestaetigt |

**5) Abweichungen vom Entwurf:** siehe Abschnitt "Umsetzung (Dev-Lauf 2026-09-24)" oben — alle sachlich
begruendet (Sprachregel, Wiederverwendung `GetByHauptFaAsync`, Schluessel-Korrektur `OrderNumber` per
Review-Befund, Zaehlzeile ohne Wurzel). Keine davon aendert Umfang oder AK-Erfuellung.

**6) `docs/TESTSZENARIEN.md` (Worktree) / `secondbrain/tests/testszenarien-index.md` (Hauptcheckout):**
TS-78 mit 9 Manual-UAT-Szenarien (TS-78.1–78.9) vorhanden, auf AK 1–12 gemappt, Zeile 78 im Index
bereits nachgezogen (inkl. Testzahlen 4+1 und AK-10-Beleg). Keine Ergaenzung noetig.

**7) `superpowers:verification-before-completion` + Code-Review:** Build/Test-Ausgaben oben sind
frisch aus diesem QA-Lauf (nicht uebernommen). Code-Review-Fokus (Zugriffsfilter-Vererbung, N+1,
Schluessel-Konsistenz Knopf/Modal, "kein leeres Modal") einzeln am Code nachvollzogen, siehe Tabelle
oben — keine offenen Befunde. Der im Dev-Lauf dokumentierte Review-Fund (Schluessel-Inkonsistenz
Sub-FA- vs. OrderNumber-Abfrage) ist bereits durch Commit `2d06d1f0` behoben und hier erneut bestaetigt.

**Ergebnis: Testbereit.** Deploy-Abschnitt oben entspricht dem tatsaechlichen Diff (nur Web,
`IDEALAKEWMSService/AppVersion.cs` ist reiner Versions-Mirror ohne Funktionsaenderung — kein
Service-Deploy noetig).

## Manuelle Test-Checkliste (Schranke 2 — nach dem Publish aus dem Worktree)

Alle Schritte in `docs/TESTSZENARIEN.md` TS-78.1–78.9 im Detail; hier die Kurzfassung zum Abhaken:

1. [ ] TS-78.1 — Struktur mit bekannten AGs (`KA`/`SW`) öffnen, Knopf **Arbeitsgänge** klicken: Modal
   zeigt Titel "FA `<HauptFA>`", Zählzeile, HauptFA-Abschnitt, alle Sub-FAs mit Kürzel/Werkbank/Sage-Nr.
2. [ ] TS-78.2 — Modal ist read-only (nur "Schließen"/X), Schließen per X/Escape/Klick außerhalb ändert
   nichts, Klick auf den Knopf klappt die Struktur-Zeile nicht auf/zu.
3. [ ] TS-78.3 — Struktur ohne materialisierten Auftrag UND Struktur mit materialisiertem Auftrag aber
   0 erkannten AGs: in beiden Fällen kein Knopf, kein Layout-Sprung in der Kopfzeile.
4. [ ] TS-78.4 — Seite mit `pageSize=50`: Seitenquelltext enthält keine AG-Details; Modal-Öffnen löst
   genau einen `/FaHierarchy/WorkOperations?hauptFa=…`-Aufruf aus (Netzwerk-Reiter).
5. [ ] TS-78.5 — Seitengröße "Alle": Seite lädt ohne spürbaren Verzug, ein Modal öffnet korrekt.
6. [ ] TS-78.6 — Fehlerfall (Offline oder ungültige `hauptFa`): Modal zeigt zuerst Ladezustand, dann
   rote Fehlermeldung — nie leer.
7. [ ] TS-78.7 — Benutzer ohne Kommissionierung/Tracking/Leitstand: `/FaHierarchy` und
   `/FaHierarchy/WorkOperations?hauptFa=<n>` weiterhin verweigert.
8. [ ] TS-78.8 — Tastaturbedienung (Tab/Enter öffnet, Escape schließt, Fokus kehrt zurück), Kontrast der
   grauen "— kein Arbeitsgang"-Zeilen am Fertigungsterminal lesbar.
9. [ ] TS-78.9 — HauptFA mit einer aus der Struktur entfallenen oder fehlerhaften Sub-FA, die dennoch
   einen Arbeitsgang trägt: Knopf sichtbar, Modal zeigt zusätzlich die gelbe Warnung
   "N Sub-FA(s) mit Arbeitsgängen stehen nicht (mehr) in der Struktur".

Nach bestandenem manuellem Test: Merge durch den Menschen (Schranke 2), danach `status: Gemerged`
setzen — beides außerhalb des QA-Agenten-Mandats.
