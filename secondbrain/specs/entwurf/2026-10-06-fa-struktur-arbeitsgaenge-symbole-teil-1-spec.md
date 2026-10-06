---
type: spec
title: "FA-Struktur Arbeitsgänge als Symbole — Teil 1: Symbole statt Zeilen (reine Anzeige)"
slug: 2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec
status: Entwurf
created: 2026-10-06
updated: 2026-10-06
source_backlog: "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole]]"
depends_on: ""
task: ""
worktree: ""
branch: ""
affected_code:
  - "IdealAkeWms/Views/FaHierarchy/_FaWorkOperationsModal.cshtml — heute je AG eine Tabellenzeile (Z.49-62: Kürzel-Badge Z.58, Werkbank Z.59, Sage-Nr. Z.60); Variante B: je Sub-FA EINE Zeile mit einer Symbolreihe statt n Zeilen"
  - "IdealAkeWms/Views/FaHierarchy/_FaHierarchyNode.cshtml — nur Variante A: neue Zelle mit Symbolreihe je Knotenzeile (Zeile Z.55-108, Spalten mit data-col-key Z.72-107)"
  - "IdealAkeWms/Views/FaHierarchy/Index.cshtml — nur Variante A: neue <th data-col-key> in Kopf- und Filterzeile (Z.105-126), colspan 8 -> 9 (Z.133, Z.204), column-config-JSON (ab Z.282)"
  - "IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs — nur Variante A: neue Spalte in ViewConfig FaHierarchyStructure (Z.450)"
  - "IdealAkeWms/Controllers/FaHierarchyController.cs — nur Variante A: Index (Z.52-133) laedt die Arbeitsgaenge der Seite gebuendelt (heute nur Existenz-Check Z.91-95); Variante B: WorkOperations (Z.139-155) unveraendert"
  - "IdealAkeWms/Data/Repositories/IWorkOperationRepository.cs + WorkOperationRepository.cs — nur Variante A: EINE neue Projektion fuer viele HauptFAs (Muster OrderNumbersWithWorkOperationsQuery Z.120-128, Contains -> OPENJSON)"
  - "IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs — WorkOperationSummary (Z.210) wird Datenquelle der Symbole; Variante A: Arbeitsgaenge je FaHierarchyTreeNode"
  - "IdealAkeWms/wwwroot/css/site.css — Symbol-Stil (Kontrast WCAG AA), falls Bootstrap-Badges nicht reichen"
  - "docs/TESTSZENARIEN.md + secondbrain/tests/testszenarien-index.md — TS-Kapitel naechste freie Nummer zum Umsetzungszeitpunkt"
open_questions:
  - "Wo erscheinen die Symbole: A direkt in den Baumzeilen je (Sub-)FA (widerspricht der v1.45-Freigabe 'Inhalt erst beim Oeffnen laden', braucht gebuendelte Abfrage je Seite, kein N+1) oder B im bestehenden Modal? Bei A: bleibt das Modal wie v1.45, wird es ebenfalls auf Symbole umgestellt, oder entfaellt es?"
  - "Was zeigt ein Symbol: Kuerzel (OperationNumber) bzw. Werkbank-Nummer, Name im Tooltip (Empfehlung der Notiz)? Welches der beiden Kuerzel steht sichtbar im Symbol?"
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

> **Umsetzungsort (Hinweis, keine Entscheidung):** nach Schranke 2 des IDEAL-Bündels, aus `main`
> (neuer Worktree). Code-Grundlage dieser Spec ist der Bündel-Stand
> `.claude/worktrees/2026-08-07-ideal-teile-1-5` (v1.47.1); alle Zeilenangaben beziehen sich darauf und
> sind nach dem Merge gegen `main` nachzuprüfen. TS-Nummer und Version: nächste freie zum
> Umsetzungszeitpunkt. `worktree`/`branch` setzt der Dev-Lauf.

## Ziel / Nutzen (das Warum)

Die Arbeitsgänge eines Auftrags sollen auf einen Blick als kompakte Symbolreihe erfassbar sein statt als
Tabellenzeilen (Anforderung Gerald, [[2026-10-06-fa-struktur-arbeitsgaenge-symbole]]). Das ist die
Darstellungsgrundlage, auf die Teil 2 (Status-Farben) und Teil 3 (BDE-Auftragsanzeige) aufsetzen.
Standort: **IDEAL** — die angezeigten Arbeitsgänge legt die Struktur-Erkennung aus v1.44.0 an
([[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]]), die FA-Struktur liest die IDEAL-Sage-Views.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Arbeitsgänge je (Sub-)FA als Symbole statt als Zeilen, am in Rückfrage 1 gewählten Ort.
- Reine Anzeige: alle Symbole **einheitlich neutral**, kein Status.
- Leitplanke aus Notiz-Punkt 3 (siehe „Fachliche Anforderungen“ 4).

**Out-of-Scope**
- Jeder Status (erledigt/offen/überfällig) und jede Statusfarbe → Teil 2.
- BDE-Terminal, BDE-Auftragsanzeige → Teil 3.
- Änderungen an Struktur-Erkennung, `WorkOperation`-Anlage, Sync, Datenmodell.
- Zählzeile, Lückenanzeige „— kein Arbeitsgang“ und Warnung `UnmatchedOperations` aus v1.45 bleiben
  inhaltlich unverändert (`_FaWorkOperationsModal.cshtml:8-18`, `:39-46`, `:68-84`).

## Fachliche Anforderungen

1. **Datenquelle** unverändert: `WorkOperation` je materialisiertem Knoten, Zuordnung Knoten → AG über
   `SubFA == ProductionOrder.SubOrderNumber` und `OrderNumber == HauptFA`
   (`FaWorkOperationsViewModel.Create`, `FaHierarchyTreeViewModel.cs:168-199`). Reihenfolge der Symbole =
   `Sequence` (dort Z.177).
2. **Symbol-Inhalt** gemäß Rückfrage 2. Belegt am Code, was die Felder bei IDEAL enthalten:
   - `OperationNumber` = Struktur-Kürzel (z. B. `KA`), gesetzt beim Anlegen durch
     `IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs:169` (`OperationNumber = token`).
     Der Default-AG trägt `"01"` (`BdeDefaultWorkOperationService`, [[fallstricke]] §16 „Existenz-Check des
     Default-AG“).
   - `Name` = Kopie von `ProductionWorkplace.Name` zum Anlagezeitpunkt
     (`WorkOperationStructureDetectionService.cs:170`, `Name = val.Wp.Name`).
   - Werkbank-Nummer = `ProductionWorkplace.SageArbeitsplatznummer`, live über die FK
     (`WorkOperationSummary`, `FaHierarchyTreeViewModel.cs:210`; heute im Modal Z.60).
3. Knoten ohne Arbeitsgang bleiben sichtbar mit Strich (Antwort 3 der v1.45-Freigabe).
4. **Leitplanken (verbindlich, unabhängig von Rückfrage 1/2):**
   - Symbol-Inhalt ist **Text** (Kürzel), nicht nur Form oder Farbe. Farbe trägt in Teil 1 keine Bedeutung.
   - Kontrast WCAG AA: 4,5:1 für den Symboltext, 3:1 für die Symbolkontur gegen den Zeilenhintergrund
     (auch gegen Hover-/Hervorhebungsfarben der Baumzeile).
   - Der Name ist **nicht nur per `title`-Tooltip** erreichbar: `title` erscheint an Touch-Terminals der
     Fertigung nicht. Zusätzlich `aria-label` bzw. visuell versteckter Text je Symbol; wo der Name
     sichtbar gebraucht wird, entscheidet Rückfrage 2.
   - Die Symbolreihe bricht um statt die Zeile horizontal zu sprengen.

## Technischer Loesungsentwurf

Muster: bestehende Badge-Darstellung des Kürzels (`_FaWorkOperationsModal.cshtml:58`,
`badge bg-secondary`), Typ-Icons mit Form + Farbe + `title`/`aria-label` (`_FaHierarchyNode.cshtml:27-53`,
`:85-88`). Keine neue UI-Bibliothek. **frontend-design-Skill ist im Dev-Lauf Pflicht** (Views/CSS).

Rückfrage 1 ist offen; beide Varianten werden skizziert, **keine** ist gewählt.

**Variante A — Symbole direkt in den Baumzeilen**
- Neue Spalte „Arbeitsgänge“ in der Tree-Table. Pflichten der bestehenden Baumtabelle:
  `<th data-col-key>` in Kopf- und Filterzeile (`Index.cshtml:105-126`), `colspan="8"` → 9 an den
  Struktur-Kopfzeilen (`Index.cshtml:133`, `:204`), Registrierung in `ColumnDefinitions.FaHierarchyStructure`
  (`ColumnDefinitions.cs:450`, sonst Prefs-API 400 — [[fallstricke]] §3 „Neuer viewKey ohne
  `ColumnDefinitions`-Registrierung“), Eintrag im column-config-JSON (`Index.cshtml:282ff`), optional
  `data-f-<key>` für den Baum-Spaltenfilter (`_FaHierarchyNode.cshtml:60-67`).
- Daten: **eine** gebündelte Abfrage je Seitenaufbau über die HauptFA-Nummern der sichtbaren Strukturen,
  schlanke Projektion (`OrderNumber`, `SubOrderNumber`, `OperationNumber`, `Name`, `Sequence`,
  `SageArbeitsplatznummer`) — **kein N+1** je Struktur oder Knoten. Muster:
  `OrderNumbersWithWorkOperationsQuery` (`WorkOperationRepository.cs:120-128`); EF 10 übersetzt das
  `Contains` zu **einem** `OPENJSON`-Parameter ([[fallstricke]] §8 „`Contains` über eine lange Liste“).
  Sie kann den heutigen Existenz-Check (`FaHierarchyController.cs:91-95`) ersetzen, weil
  `HasWorkOperations` daraus ableitbar ist.
- **Kollision mit v1.45 (zu benennen, nicht zu verschweigen):** Die Freigabe von
  [[2026-09-23-fa-struktur-arbeitsgaenge-anzeige-spec]] lautet „Inhalt wird erst beim Öffnen geladen,
  Seitenaufbau nur schlanker Existenz-Check“. Grund dort: das Seiten-HTML wächst sonst mit der
  Seitengröße, bei „Alle“ bis `AllCap = 5000` Strukturen ([[fallstricke]] §3 „Pagination-AllCap 5000“).
  Variante A rendert die Symbole in **jede** Knotenzeile — auch in die initial eingeklappten
  (`_FaHierarchyNode.cshtml:68`, `display:none`, aber im HTML). Variante A **hebt diese Freigabe auf**;
  das muss Rückfrage 1 ausdrücklich so entscheiden. Mögliche Begrenzung (nur falls A gewählt):
  Symbole nur bis zu einer Seitengröße, darüber Hinweis — wäre eine weitere Festlegung.
- Erfordert Index-Umbau, Repository-Methode, ViewModel-Feld je Knoten, Spalten-Registrierung,
  Unit-Test der Zuordnung und ToQueryString-Test des `Contains`.

**Variante B — Symbole im bestehenden Modal**
- `_FaWorkOperationsModal.cshtml`: je Knoten **eine** Zeile; die Spalten „Kürzel/Werkbank/Sage-Nr.“
  (Z.25-27) werden zu einer Spalte „Arbeitsgänge“ mit Symbolreihe. Zählzeile, Strich-Zeilen, Warnung
  `UnmatchedOperations` bleiben.
- Laden unverändert (v1.45-Freigabe bleibt gültig): Endpunkt `WorkOperations`
  (`FaHierarchyController.cs:139-155`), keine Controller-/Repository-Änderung, kein neuer Listen-View.
- Kleinster Diff: eine View, ggf. CSS.

**Zugriffsschutz:** unverändert `[RequirePickingOrTrackingOrLeitstandAccess]` auf Klassenebene
(`FaHierarchyController.cs:23`); beide Varianten fügen keine Action hinzu.

## Migrations-/SQL-Auswirkungen

Keine. Reine Lesedarstellung auf `WorkOperations`, `ProductionWorkplaces`, `FaHierarchyNodes`. Keine
Migration, kein `SQL/XX_*.sql`, `SQL/00_FreshInstall.sql` unverändert, `SQL/AgentJobs/` unberührt.

## Audit-Feld-Auswirkungen

Keine — es wird nichts geschrieben.

## Rollen / Zugriffsfilter

Unverändert (siehe oben). Kein neuer Filter, keine Änderung an `controller.md`/`RoleOverview`.

## Listen-View-Pattern (ADR 0005)

- Variante A: Erweiterung des bestehenden Baum-Listen-Views `FaHierarchyStructure` → Spaltenpflichten
  wie oben (`data-col-key` auf allen `<th>`, Registrierung, column-prefs). Pagination/Filterkarte
  bestehen bereits.
- Variante B: kein Listen-View (Modal-Detailansicht, Begründung wie v1.45-Spec Abschnitt 2).

## Akzeptanzkriterien

Gemeinsam:
1. Je materialisiertem Knoten erscheinen seine Arbeitsgänge als Symbole in `Sequence`-Reihenfolge, nicht
   als je eine Tabellenzeile.
2. Jedes Symbol zeigt den in Rückfrage 2 festgelegten Text; der AG-Name ist per Tastaturfokus/Screenreader
   erreichbar (`aria-label` o. ä.), nicht nur per Maus-Hover.
3. Alle Symbole sind gleich gestaltet (keine Status- oder Farbunterscheidung).
4. Kontrast Symboltext ≥ 4,5:1, Symbolkontur ≥ 3:1 — gemessen am Testsystem.
5. Knoten ohne Arbeitsgang zeigen weiterhin den Strich; Zählzeile und `UnmatchedOperations`-Warnung
   unverändert.
6. Viele Arbeitsgänge an einem Knoten (≥ 8) brechen um, ohne horizontales Scrollen der Zeile.
7. Kein Schreibzugriff, Zugriffsschutz unverändert (Benutzer ohne Kommissionierung/Tracking/Leitstand:
   `/FaHierarchy` verweigert).

Nur Variante A:
8. Seitenaufbau `/FaHierarchy` löst **genau eine** AG-Abfrage aus, unabhängig von der Anzahl Strukturen
   (Query-Zählung im Dev-Lauf); ToQueryString-Test belegt `OPENJSON` mit einem Parameter.
9. Neue Spalte ist ein-/ausblendbar und verschiebbar (column-prefs), `GET` der Prefs-API für
   `FaHierarchyStructure` liefert 200.

Nur Variante B:
10. Seitenaufbau unverändert (nur Existenz-Check); Modal lädt weiterhin genau eine Struktur.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (Nummer: nächste freie zum Umsetzungszeitpunkt), Index in
`secondbrain/tests/testszenarien-index.md` nachziehen. Bestehendes Kapitel TS-78 (v1.45) auf geänderte
Darstellung prüfen.

- **Vorbedingungen:** IDEAL-Testsystem, Struktur-Erkennung (v1.44.0) hat AGs angelegt, Benutzer mit
  Kommissionierung/Tracking/Leitstand.
- **S1 Symbole sichtbar:** Struktur mit `KA`/`SW`-AGs öffnen (Baum bzw. Modal) → Symbole in richtiger
  Reihenfolge, Text laut Rückfrage 2.
- **S2 Name erreichbar:** Tastatur-Fokus auf Symbol / Screenreader → Name wird angesagt; Touch-Gerät →
  Name auf dem in Rückfrage 2 festgelegten Weg erreichbar.
- **S3 Lücke:** Sub-FA ohne AG → Strich, Zählzeile stimmt.
- **S4 Viele AGs:** Umbruch, keine horizontale Scrollleiste in der Zeile.
- **S5 Kontrast:** Messung am Fertigungsterminal/Testgerät (WCAG AA).
- **S6 (nur A) Große Seite:** Seitengröße „Alle“ → eine AG-Abfrage, Ladezeit akzeptabel, Spalte per
  Spaltenauswahl aus-/einblendbar.
- **Negativ:** Benutzer ohne Berechtigung → Zugriff verweigert; Struktur ohne materialisierten Auftrag →
  keine Symbole, kein Fehler.

## Etappen (nur bei epic: true)

Entfällt (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

**Vorläufig — der QA-Lauf bestätigt gegen den echten Diff.**

- **Web-App:** ja (View, ggf. CSS; Variante A zusätzlich Controller, Repository, ViewModel, ColumnDefinitions).
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:**

```bash
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

## Offene Rueckfragen

1. **Wo erscheinen die Symbole?** (Notiz-Punkt 1)
   - **A** direkt in den Baumzeilen je (Sub-)FA — widerspricht der v1.45-Freigabe „Inhalt erst beim
     Öffnen laden, Seitenaufbau nur schlanker Existenz-Check“ und braucht eine gebündelte Abfrage je Seite
     (kein N+1); das Seiten-HTML wächst mit der Seitengröße (bis 5000 Strukturen bei „Alle“).
     Zusatzfrage bei A: Bleibt das Modal wie v1.45, wird es auch auf Symbole umgestellt, oder entfällt es?
   - **B** im bestehenden Modal — v1.45-Freigabe bleibt gültig, kleinster Diff, aber kein Überblick ohne Klick.
   - Die Notiz gibt hier **keine** Empfehlung, sie benennt nur die Kollision. Die Spec wählt nicht.
2. **Was zeigt ein Symbol?** (Notiz-Punkt 2) Empfehlung der Notiz: Kürzel/Nummer des Arbeitsgangs bzw.
   der Werkbank, Name im Tooltip. Zu entscheiden: welches Kürzel steht **sichtbar** im Symbol —
   `OperationNumber` (z. B. `KA`, beim Default-AG `01`) oder `SageArbeitsplatznummer` (z. B. `2300`)?
   Hinweis: Ein reiner `title`-Tooltip erscheint an Touch-Terminals nicht; die Spec verlangt den Namen
   zusätzlich per `aria-label` (Leitplanke). Reicht das, oder soll der Name auf Touch sichtbar werden
   (z. B. Antippen öffnet Popover)?

> Pflicht-Leitplanke aus Notiz-Punkt 3 gilt hier schon (Text statt Farbe, WCAG AA); die Frage „wie genau
> wird Status ohne Farbe codiert“ ist Rückfrage 1 in [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]].

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Wo erscheinen die Symbole — A (Baumzeilen) oder B (Modal); bei A: Modal behalten/umstellen/entfallen? →
2. Was zeigt ein Symbol — OperationNumber oder SageArbeitsplatznummer sichtbar; Name nur aria-label oder auf Touch sichtbar? →
