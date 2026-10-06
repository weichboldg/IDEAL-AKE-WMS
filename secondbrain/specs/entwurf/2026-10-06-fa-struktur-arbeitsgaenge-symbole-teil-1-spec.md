---
type: spec
title: "FA-Struktur Arbeitsgänge als Symbole — Teil 1: Symbole statt Zeilen (reine Anzeige)"
slug: 2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec
status: Entwurf
created: 2026-10-06
updated: 2026-10-06
source_backlog: "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole]]"
anhaenge:
  - "[[sage-fa-struktur-arbeitsschritte.png]]"
depends_on: ""
task: ""
worktree: ""
branch: ""
affected_code:
  - "IdealAkeWms/Views/FaHierarchy/_FaWorkOperationsModal.cshtml — heute je AG eine Tabellenzeile (Z.49-62: Kürzel-Badge Z.58, Werkbank Z.59, Sage-Nr. Z.60); Variante B: je Sub-FA EINE Zeile mit einer Symbolreihe statt n Zeilen"
  - "IdealAkeWms/Views/FaHierarchy/_FaHierarchyNode.cshtml — nur Variante A: neue Zelle mit Symbolreihe je Knotenzeile (Zeile Z.55-108, Spalten mit data-col-key Z.72-107); bei Quelle S aus n.Arbeitsschritte des Knotens (Feld ist schon geladen), bei Quelle W aus den Arbeitsgaengen des Knotens"
  - "IdealAkeWms/Views/FaHierarchy/Index.cshtml — nur Variante A: neue <th data-col-key> in Kopf- und Filterzeile (Z.105-126), colspan 8 -> 9 (Z.133, Z.204), column-config-JSON (ab Z.282)"
  - "IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs — nur Variante A: neue Spalte in ViewConfig FaHierarchyStructure (Z.450)"
  - "IdealAkeWms/Controllers/FaHierarchyController.cs — nur Variante A + Quelle W: Index (Z.52-133) laedt die Arbeitsgaenge der Seite gebuendelt (heute nur Existenz-Check Z.91-95); Variante A + Quelle S: keine AG-Abfrage, ggf. EIN Katalog-Lookup Kuerzel -> Werkbank-Name; Variante B: WorkOperations (Z.139-155) unveraendert (Quelle W) bzw. Knoten-Kuerzel statt AG (Quelle S)"
  - "IdealAkeWms/Data/Repositories/IWorkOperationRepository.cs + WorkOperationRepository.cs — nur Variante A + Quelle W: EINE neue Projektion fuer viele HauptFAs (Muster OrderNumbersWithWorkOperationsQuery Z.120-128, Contains -> OPENJSON)"
  - "IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs — Quelle W: WorkOperationSummary (Z.210) wird Datenquelle der Symbole, bei Variante A Arbeitsgaenge je FaHierarchyTreeNode; Quelle S: Tokenisierung von FaHierarchyNode.Arbeitsschritte als reine, unit-testbare Funktion (gleiche Regel wie WorkOperationStructureDetectionService.cs:33/:110-111)"
  - "IdealAkeWms/Models/FaHierarchyNode.cs — nur lesen: Arbeitsschritte (Z.41), Arbeitsbereich (Z.40); keine Modellaenderung"
  - "IdealAkeWms/wwwroot/css/site.css — Symbol-Stil (Kontrast WCAG AA), falls Bootstrap-Badges nicht reichen"
  - "docs/TESTSZENARIEN.md + secondbrain/tests/testszenarien-index.md — TS-Kapitel naechste freie Nummer zum Umsetzungszeitpunkt"
open_questions:
  - "Symbol-Quelle: Zeigen die Symbole S die Sage-Kuerzel aus FaHierarchyNode.Arbeitsschritte (Models/FaHierarchyNode.cs:41, je Baumzeile wie im Sage-Screenshot, auch an Blaettern) oder W die im WMS erkannten WorkOperation-Eintraege (nur materialisierte Sub-FAs, Kuerzel der direkten Kinder eingerechnet, ohne unbekannte/ausgeschlossene/mehrdeutige Kuerzel, dazu ggf. Default-AG 01)? Folge fuer Teil 2: nur W traegt direkt einen Status; S braucht dort eine Zuordnung Kuerzel -> WorkOperation."
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

**Vorbild aus Sage** (Anhang der Notiz, Screenshot Gerald 2026-10-06, für diesen Lauf lesbar):

![[sage-fa-struktur-arbeitsschritte.png]]

Sage „Fertigungsaufträge bearbeiten“ zeigt die Struktur als Baum mit den Spalten **Bezeichnung |
Abteilung | Arbeitsschritte | Artikeltyp**. Abgelesen:
- Die Arbeitsschritte stehen **je Baumzeile als Kürzel in EINER Spalte**, leerzeichengetrennt (`SW`, `ZS`,
  `BE`, `PG`, `VM`, `LS KA`, `LS EG KA`, `LS KA PG`, `LS KA SW`, `LS EG KA SW`, `LS EG KA PG`) — auch an
  Zeilen mit Stapel-Symbol (in Sage vermutlich Blätter; ob sie im WMS `SubFA = 0` haben, ist ungeprüft,
  Prüfschritt P-K2).
- Die Abteilung steht **je Zeile** (`H1-03`, `S-01`, `H2-02`, `K-02`); leer bei `KTZ-L3-76-5Z1T RAL`
  (Wurzel), `G-KT-Korpus-Gepunktet` und `G-KT-Korpus-1320-660-760-…`. Artikeltyp ist im Ausschnitt leer.
- Ein Knoten `FR-Zuschnitt` (unter `FR-4301-40-20-1,25-0720,0MM-1X-V01`) hat ein eigenes Symbol, keine
  Abteilung, keine Arbeitsschritte. Bedeutung **ungeprüft** — nicht Teil des Umfangs.

Bezug zum WMS (belegt, Bündel): `FaHierarchyNode` hat `Arbeitsbereich` und `Arbeitsschritte`
(`Models/FaHierarchyNode.cs:40-41`). Der FA-Struktur-Baum zeigt `Arbeitsbereich` schon heute je Zeile
(`Views/FaHierarchy/Index.cshtml:109`, `_FaHierarchyNode.cshtml:93`), `Arbeitsschritte` nicht. Ob die
Sage-Spalten „Abteilung“ und „Arbeitsschritte“ genau diese Felder sind, ist **nicht aus den Namen
abgeleitet**, sondern Prüfschritt P-K1. Zur Einordnung (nicht als Deutung): Laut [[0014-werkbank-datenhoheit-sage-fuehrend-mit-abweichungsmeldung]]
(Nachtrag 2026-09-22, Z.103-105, „Arbeitsbereich ist Zielort“, [[2026-09-21-adr-0014-arbeitsbereich-ist-zielort-spec]])
ist der WMS-`Arbeitsbereich` Sage `USER_OSAbteilung`, ein Zielort, keine Werkbank. Bestätigt P-K1 die
Gleichheit, ist der Abteilungs-Teil des Vorbilds im Baum bereits umgesetzt; Teil 1 ändert daran nichts.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Arbeitsgänge als Symbole statt als Zeilen — aus der in Rückfrage 1 gewählten Quelle (S: Sage-Kürzel je
  Baumzeile wie im Vorbild, W: erkannte `WorkOperation` je Sub-FA), am in Rückfrage 2 gewählten Ort.
- Reine Anzeige: alle Symbole **einheitlich neutral**, kein Status.
- Leitplanke aus Notiz-Punkt 3 (siehe „Fachliche Anforderungen“ 4).

**Out-of-Scope**
- Jeder Status (erledigt/offen/überfällig) und jede Statusfarbe → Teil 2.
- BDE-Terminal, BDE-Auftragsanzeige → Teil 3.
- Änderungen an Struktur-Erkennung, `WorkOperation`-Anlage, Sync, Datenmodell.
- Zählzeile, Lückenanzeige „— kein Arbeitsgang“ und Warnung `UnmatchedOperations` aus v1.45 bleiben
  inhaltlich unverändert (`_FaWorkOperationsModal.cshtml:8-18`, `:39-46`, `:68-84`).
- Spalte Arbeitsbereich/„Abteilung“ (besteht, siehe Vorbild); Knoten `FR-Zuschnitt` und Spalte Artikeltyp.

## Fachliche Anforderungen

1. **Datenquelle — offen, Rückfrage 1.** (Bis zur Rework-Fassung 2026-10-06 stand hier „unverändert
   `WorkOperation`“ — **überholt**, seit das Sage-Vorbild Kürzel je Baumzeile zeigt.)
   - **Quelle W — erkannte `WorkOperation`:** je materialisiertem Knoten, Zuordnung Knoten → AG über
     `SubFA == ProductionOrder.SubOrderNumber` und `OrderNumber == HauptFA`
     (`FaWorkOperationsViewModel.Create`, `FaHierarchyTreeViewModel.cs:168-199`). Reihenfolge der Symbole =
     `Sequence` (dort Z.177).
   - **Quelle S — Sage-Kürzel des Knotens:** `FaHierarchyNode.Arbeitsschritte` (`FaHierarchyNode.cs:41`)
     der Zeile selbst, tokenisiert wie die Erkennung (nur Leerzeichen, leere Teile weg, getrimmt —
     `WorkOperationStructureDetectionService.cs:33`, `:110-111`), Reihenfolge wie im Feld. Das Feld ist im
     Index bereits für **alle** Knoten geladen (`FaHierarchyController.cs:67`, `GetAllAsync` liefert ganze
     Entitäten, `FaHierarchyNodeRepository.cs:28-32`) — keine zusätzliche Abfrage für die Kürzel selbst.
   - **Befund: Wie aus `Arbeitsschritte` eine `WorkOperation` wird** (Bündel,
     `IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs`):
     - Nur Knoten mit `SubFA != 0` erzeugen Arbeitsgänge (`:98`). **DirectChildren-Scope:** eigene Zeile
       **plus** alle direkten Kinder (`VaterFA == SubFA`) (`:73-76`, `:101-102`).
     - Je Sub-FA zählt jedes Kürzel einmal, case-insensitiv, Reihenfolge = erstes Auftreten (`:106`, `:113`);
       `Sequence` zählt auch übersprungene Kürzel mit (`:114` vor `:116`). Die Knoten werden ohne
       `OrderBy` gelesen (`:70`) — die Reihenfolge der Kinder-Kürzel ist damit nicht fachlich festgelegt.
     - Kürzel auf der Ausschlussliste `Sync:ProductionWorkplaceSyncAusschlussliste` (`:65-67`; Default
       `STO,XXX,x01,BS,BS2,FRE,AKE` laut [[services]]): **kein** Arbeitsgang, **keine** Meldung (`:116`).
     - Kürzel, das mehrere Werkbänke trifft: kein Arbeitsgang, Warnung im Protokoll (`:120-124`, `:193-203`).
     - Kürzel ohne Werkbank (`ProductionWorkplace.ArbeitsschrittCode`, `:80-88`): **kein** Arbeitsgang,
       Sammelmeldung, Mail nur bei Änderung (`:129-132`, `:207-221`).
     - Angelegt wird nur für `ProductionOrder`s mit `SubOrderNumber` = `SubFA`, die nicht erledigt, nicht
       storniert und nicht fertig kommissioniert sind (`:142-145`). **Nur hinzufügen:** vorhandene
       (`ProductionOrderId`, `OperationNumber`) werden übersprungen, nichts wird gelöscht (`:151-163`).
     - Daneben legt `BdeDefaultWorkOperationService` bei NurFA-Buchungen den Default-AG `"01"` an
       (`IdealAkeWms/Services/BdeDefaultWorkOperationService.cs:27-40`) — ohne Gegenstück in `Arbeitsschritte`.
   - **Differenz S ↔ W (Folge des Befunds):**
     - *Kürzel ohne `WorkOperation`:* unbekannte, ausgeschlossene, mehrdeutige Kürzel; Kürzel an Blättern
       (`SubFA = 0`, haben nie eine eigene); Kürzel an Sub-FAs, deren `ProductionOrder` fehlt oder beim
       Erkennungslauf schon erledigt/storniert/fertig kommissioniert war.
     - *`WorkOperation` an einem anderen Knoten:* die Kürzel eines Kindes landen beim **Vater**-Sub-FA.
       Ist das Kind selbst Sub-FA, trägt es dasselbe Kürzel zusätzlich als eigene `WorkOperation` (doppelt
       erfasst). Beispiel laut Screenshot (WMS-Gleichheit ungeprüft, P-K2): Zeile
       `G-KT-Korpus-1320-660-760-…` zeigt in Sage nur `PG`; unter W stünden dort `PG` plus die Kürzel seiner
       direkten Kinder (`LS`, `EG`, `KA`, `SW`, `VM` …, abzüglich ausgeschlossener/unbekannter).
     - *`WorkOperation` ohne Kürzel:* Default-AG `"01"`; Kürzel, die in Sage nachträglich entfernt wurden.
     - Quelle S zeigt damit das Sage-Bild 1:1, Quelle W das, was BDE/Tracking kennen.
2. **Symbol-Inhalt** gemäß Rückfrage 3. Bei Quelle S ist der sichtbare Text das Sage-Kürzel selbst; ein Name
   ist nur über den Werkbank-Katalog (`ProductionWorkplace.ArbeitsschrittCode` → `Name`, wie `:80-88`)
   auffindbar und fehlt bei unbekannten/ausgeschlossenen Kürzeln. Belegt am Code, was die Felder bei Quelle W
   enthalten:
   - `OperationNumber` = Struktur-Kürzel (z. B. `KA`), gesetzt beim Anlegen durch
     `IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs:169` (`OperationNumber = token`).
     Der Default-AG trägt `"01"` (`BdeDefaultWorkOperationService`, [[fallstricke]] §16 „Existenz-Check des
     Default-AG“).
   - `Name` = Kopie von `ProductionWorkplace.Name` zum Anlagezeitpunkt
     (`WorkOperationStructureDetectionService.cs:170`, `Name = val.Wp.Name`).
   - Werkbank-Nummer = `ProductionWorkplace.SageArbeitsplatznummer`, live über die FK
     (`WorkOperationSummary`, `FaHierarchyTreeViewModel.cs:210`; heute im Modal Z.60).
3. Knoten ohne Arbeitsgang bleiben sichtbar mit Strich (Antwort 3 der v1.45-Freigabe). Bei Quelle S
   heißt das: Knoten mit leerem `Arbeitsschritte` (im Vorbild z. B. die Wurzel).
4. **Leitplanken (verbindlich, unabhängig von Rückfrage 1–3):**
   - Symbol-Inhalt ist **Text** (Kürzel), nicht nur Form oder Farbe. Farbe trägt in Teil 1 keine Bedeutung.
   - Kontrast WCAG AA: 4,5:1 für den Symboltext, 3:1 für die Symbolkontur gegen den Zeilenhintergrund
     (auch gegen Hover-/Hervorhebungsfarben der Baumzeile).
   - Der Name ist **nicht nur per `title`-Tooltip** erreichbar: `title` erscheint an Touch-Terminals der
     Fertigung nicht. Zusätzlich `aria-label` bzw. visuell versteckter Text je Symbol; wo der Name
     sichtbar gebraucht wird, entscheidet Rückfrage 3. Bei Quelle S ohne Werkbank-Treffer: Kürzel als
     `aria-label`-Text, kein erfundener Name.
   - Die Symbolreihe bricht um statt die Zeile horizontal zu sprengen.

## Technischer Loesungsentwurf

Muster: bestehende Badge-Darstellung des Kürzels (`_FaWorkOperationsModal.cshtml:58`,
`badge bg-secondary`), Typ-Icons mit Form + Farbe + `title`/`aria-label` (`_FaHierarchyNode.cshtml:27-53`,
`:85-88`). Keine neue UI-Bibliothek. **frontend-design-Skill ist im Dev-Lauf Pflicht** (Views/CSS).

Rückfrage 1 (Quelle S/W) und Rückfrage 2 (Ort A/B) sind offen; alle Kombinationen werden skizziert,
**keine** ist gewählt. Die Varianten A/B unten beschreiben den Ort; wie die Quelle sie verändert:

| | Quelle S (Sage-Kürzel `Arbeitsschritte`) | Quelle W (`WorkOperation`) |
|---|---|---|
| **Variante A (Baum)** | Kürzel je Zeile **inkl. Blätter**, wie Sage. Keine AG-Abfrage — Feld ist geladen (`FaHierarchyController.cs:67`); für Namen höchstens **ein** Katalog-Lookup `ArbeitsschrittCode → Name` je Seitenaufbau. HTML-Wachstum (Kollision v1.45, unten) bleibt. | Symbole nur an materialisierten Knoten (`SubFA != 0`), inkl. Kürzel der direkten Kinder. Eine gebündelte AG-Abfrage je Seite (unten). |
| **Variante B (Modal)** | Das Modal listet heute nur Wurzel und Sub-FAs (`FaHierarchyTreeViewModel.cs:185`); Kürzel der Blätter fehlten dort — Modal müsste alle Knoten listen oder Blätter weglassen (weitere Festlegung). | Wie bisher skizziert, kleinster Diff. |

**Variante A — Symbole direkt in den Baumzeilen** (Datenteil unten gilt für Quelle W)
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
  das muss Rückfrage 2 ausdrücklich so entscheiden. Mögliche Begrenzung (nur falls A gewählt):
  Symbole nur bis zu einer Seitengröße, darüber Hinweis — wäre eine weitere Festlegung.
- Erfordert (Quelle W) Index-Umbau, Repository-Methode, ViewModel-Feld je Knoten, Spalten-Registrierung,
  Unit-Test der Zuordnung und ToQueryString-Test des `Contains`. Quelle S: View-Zelle, Tokenisierungs-
  Funktion mit Unit-Test, Spalten-Registrierung; ggf. Katalog-Lookup für Namen.

**Variante B — Symbole im bestehenden Modal**
- `_FaWorkOperationsModal.cshtml`: je Knoten **eine** Zeile; die Spalten „Kürzel/Werkbank/Sage-Nr.“
  (Z.25-27) werden zu einer Spalte „Arbeitsgänge“ mit Symbolreihe. Zählzeile, Strich-Zeilen, Warnung
  `UnmatchedOperations` bleiben.
- Laden unverändert (v1.45-Freigabe bleibt gültig): Endpunkt `WorkOperations`
  (`FaHierarchyController.cs:139-155`), keine Controller-/Repository-Änderung, kein neuer Listen-View.
- Kleinster Diff (Quelle W): eine View, ggf. CSS. Quelle S: zusätzlich ViewModel-Änderung, weil die
  Einträge heute nur Wurzel + Sub-FAs umfassen (`FaHierarchyTreeViewModel.cs:185`).

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
1. Arbeitsgänge erscheinen als Symbole, nicht als je eine Tabellenzeile — je nach Rückfrage 1:
   - *Quelle W:* je materialisiertem Knoten seine `WorkOperation`s in `Sequence`-Reihenfolge.
   - *Quelle S:* je angezeigter Knotenzeile die Kürzel aus `Arbeitsschritte` in Feld-Reihenfolge; Prüfwerte
     laut Vorbild (sofern P-K1 sie bestätigt): `G-KT-A-BODEN-1320-660-V01` → `LS` `KA` `PG`;
     `FR-4301-40-20-1,25-1530,0MM-V01` → `ZS`; `G-KT-Mittelholmblende-40-760-V02` → `VM`;
     `G-KT-Korpus-Gepunktet` → `BE`.
2. Jedes Symbol zeigt den in Rückfrage 3 festgelegten Text; der Name ist per Tastaturfokus/Screenreader
   erreichbar (`aria-label` o. ä.), nicht nur per Maus-Hover (Quelle S ohne Werkbank-Treffer: Kürzel).
3. Alle Symbole sind gleich gestaltet (keine Status- oder Farbunterscheidung).
4. Kontrast Symboltext ≥ 4,5:1, Symbolkontur ≥ 3:1 — gemessen am Testsystem.
5. Knoten ohne Arbeitsgang zeigen weiterhin den Strich; Zählzeile und `UnmatchedOperations`-Warnung
   unverändert.
6. Viele Arbeitsgänge an einem Knoten (≥ 8) brechen um, ohne horizontales Scrollen der Zeile.
7. Kein Schreibzugriff, Zugriffsschutz unverändert (Benutzer ohne Kommissionierung/Tracking/Leitstand:
   `/FaHierarchy` verweigert).

Nur Variante A:
8. *Quelle W:* Seitenaufbau `/FaHierarchy` löst **genau eine** AG-Abfrage aus, unabhängig von der Anzahl
   Strukturen (Query-Zählung im Dev-Lauf); ToQueryString-Test belegt `OPENJSON` mit einem Parameter.
   *Quelle S:* Seitenaufbau löst **keine** AG-Abfrage aus und höchstens **eine** Katalog-Abfrage
   (Query-Zählung im Dev-Lauf).
9. Neue Spalte ist ein-/ausblendbar und verschiebbar (column-prefs), `GET` der Prefs-API für
   `FaHierarchyStructure` liefert 200.

Nur Variante B:
10. Seitenaufbau unverändert (nur Existenz-Check); Modal lädt weiterhin genau eine Struktur.

Nur Quelle S:
11. Tokenisierung per Unit-Test belegt: `"LS EG KA PG"` → `LS`,`EG`,`KA`,`PG`; doppelte Leerzeichen und
    Rand-Leerzeichen erzeugen keine leeren Symbole; `null`/leer → Strich.
12. Ausgeschlossene oder unbekannte Kürzel werden **angezeigt** wie alle anderen (Teil 1 hat keinen Status;
    Grundannahme der Quelle S „Sage-Bild 1:1“ — soll die Ausschlussliste auch die Anzeige filtern, gehört das
    in Antwort 1); die Unterscheidung „hat `WorkOperation`“ ist Sache von Teil 2.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (Nummer: nächste freie zum Umsetzungszeitpunkt), Index in
`secondbrain/tests/testszenarien-index.md` nachziehen. Bestehendes Kapitel TS-78 (v1.45) auf geänderte
Darstellung prüfen.

- **Vorbedingungen:** IDEAL-Testsystem, Struktur-Erkennung (v1.44.0) hat AGs angelegt, Benutzer mit
  Kommissionierung/Tracking/Leitstand.
- **S1 Symbole sichtbar:** Struktur mit `KA`/`SW`-AGs öffnen (Baum bzw. Modal) → Symbole in richtiger
  Reihenfolge, Text laut Rückfrage 3.
- **S1a (Quelle S) Abgleich mit Sage:** Die Struktur aus dem Screenshot (`KTZ-L3-76-5Z1T RAL`) im WMS und
  in Sage „Fertigungsaufträge bearbeiten“ nebeneinander öffnen → je Zeile dieselben Kürzel wie in Sage
  (Stichproben: A-BODEN `LS KA PG`, FR-4301-…-1530 `ZS`, Mittelholmblende `VM`, Korpus-Gepunktet `BE`);
  auch Blattzeilen tragen Symbole; Wurzel ohne Kürzel → Strich.
- **S1b (Quelle W) Abgleich mit Erkennung:** Sub-FA mit Kindern öffnen → Symbole = Kürzel der eigenen Zeile
  plus der direkten Kinder, ohne Kürzel der Ausschlussliste und ohne im Aktivitätsprotokoll als unbekannt
  gemeldete; Blattzeilen ohne Symbole.
- **S2 Name erreichbar:** Tastatur-Fokus auf Symbol / Screenreader → Name wird angesagt; Touch-Gerät →
  Name auf dem in Rückfrage 3 festgelegten Weg erreichbar.
- **S3 Lücke:** Sub-FA ohne AG (Quelle W) bzw. Knoten mit leerem `Arbeitsschritte` (Quelle S) → Strich,
  Zählzeile stimmt.
- **S4 Viele AGs:** Umbruch, keine horizontale Scrollleiste in der Zeile.
- **S5 Kontrast:** Messung am Fertigungsterminal/Testgerät (WCAG AA).
- **S6 (nur A) Große Seite:** Seitengröße „Alle“ → eine AG-Abfrage (Quelle W) bzw. keine (Quelle S),
  Ladezeit akzeptabel, Spalte per Spaltenauswahl aus-/einblendbar.
- **Negativ:** Benutzer ohne Berechtigung → Zugriff verweigert; Struktur ohne materialisierten Auftrag →
  keine Symbole (Quelle W) bzw. Sage-Kürzel ohne Fehler (Quelle S); Quelle S: unbekanntes Kürzel (nicht im
  Werkbank-Katalog) → Symbol mit Kürzel, `aria-label` ohne Namen, kein Fehler.

## Pruefschritte fuer den Menschen (ungeprüft — WMS-Datenbank IDEAL)

Beide Abfragen laufen auf der **IDEAL-WMS-Datenbank** (nicht Sage). Sie sind vom Spec-Lauf **nicht**
ausgeführt; alle Aussagen „laut Vorbild“ in dieser Spec hängen an ihrem Ergebnis.

**P-K1 — Sind Sage „Abteilung“/„Arbeitsschritte“ die Felder `Arbeitsbereich`/`Arbeitsschritte`?**
(Abfrage aus der Notiz, unverändert):

```sql
SELECT Artnr, Bezeichnung1, Arbeitsbereich, Arbeitsschritte
FROM dbo.FaHierarchyNodes
WHERE Artnr IN ('G-KT-A-BODEN-1320-660-V01', 'FR-4301-40-20-1,25-1530,0MM-V01',
                'G-KT-Mittelholmblende-40-760-V02', 'G-KT-Korpus-Gepunktet')
   OR Bezeichnung1 IN ('G-KT-A-BODEN-1320-660-V01', 'G-KT-Korpus-Gepunktet');
-- Screenshot: A-BODEN = H1-03 / "LS KA PG"; FR-4301-...-1530 = S-01 / "ZS";
--             Mittelholmblende = H2-02 / "VM"; Korpus-Gepunktet = (leer) / "BE"
```

Stimmen alle vier Zeilen, gilt die Gleichheit für diese Stichprobe. Abweichung → vor Antwort 1 klären. Ob
„Abteilung“ fachlich dasselbe meint wie der WMS-`Arbeitsbereich`, beantwortet die Abfrage nur für die Werte,
nicht für die Bedeutung (vgl. ADR-0014-Nachtrag oben).

**P-K2 — Blätter, Sub-FAs und Werkbank-Abdeckung der Kürzel** (für die Differenz S ↔ W):

```sql
-- a) Knotentyp der Screenshot-Struktur: SubFA = 0 heisst Blatt (keine eigene WorkOperation)
SELECT n.HauptFA, n.VaterFA, n.SubFA, n.Artnr, n.Bezeichnung1, n.Arbeitsschritte
FROM dbo.FaHierarchyNodes n
WHERE n.HauptFA = (SELECT TOP 1 HauptFA FROM dbo.FaHierarchyNodes
                   WHERE Artnr = 'G-KT-A-BODEN-1320-660-V01')
ORDER BY n.VaterFA, n.Position;

-- b) Welche Kuerzel haben eine Werkbank (ArbeitsschrittCode)? Ohne Treffer -> keine WorkOperation
SELECT t.value AS Kuerzel, COUNT(*) AS Knotenzeilen,
       COUNT(DISTINCT wp.Id) AS Werkbaenke
FROM dbo.FaHierarchyNodes n
CROSS APPLY STRING_SPLIT(n.Arbeitsschritte, ' ') t
LEFT JOIN dbo.ProductionWorkplaces wp ON LTRIM(RTRIM(wp.ArbeitsschrittCode)) = t.value
WHERE t.value <> ''
GROUP BY t.value
ORDER BY Werkbaenke, Kuerzel;
```

`Werkbaenke = 0` → Kürzel unbekannt oder auf der Ausschlussliste (Wert des Service-Keys
`Sync:ProductionWorkplaceSyncAusschlussliste` in den Service-Einstellungen abgleichen); `> 1` → mehrdeutig. Ob `FR-Zuschnitt` im WMS
als Knoten existiert, zeigt a) ebenfalls (nur zur Information, außerhalb des Umfangs).

## Etappen (nur bei epic: true)

Entfällt (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

**Vorläufig — der QA-Lauf bestätigt gegen den echten Diff.**

- **Web-App:** ja (View, ggf. CSS; Variante A zusätzlich ColumnDefinitions und — Quelle W — Controller,
  Repository, ViewModel; Quelle S zusätzlich Tokenisierungs-Funktion, ggf. Katalog-Lookup).
- **Vor Antwort 1:** Prüfschritte P-K1/P-K2 (oben) durch Gerald.
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:**

```bash
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

## Offene Rueckfragen

1. **Symbol-Quelle: Sage-Kürzel oder erkannte Arbeitsgänge?** (neu, aus dem Sage-Vorbild und dem
   Notiz-Abschnitt „Vorbild aus Sage“) Zeigen die Symbole die Sage-Kürzel aus
   `FaHierarchyNode.Arbeitsschritte` (`Models/FaHierarchyNode.cs:41`, Bündel) oder die im WMS erkannten
   `WorkOperation`-Einträge? Befund und Differenz: „Fachliche Anforderungen“ 1.
   - **S — Sage-Kürzel je Baumzeile:** zeigt das Sage-Bild 1:1, auch an Blättern und auch Kürzel ohne
     Werkbank. Bei Variante A (Baum) keine AG-Abfrage nötig (Feld geladen) — die Lade-Begründung der
     v1.45-Freigabe trifft dann nur noch das HTML-Wachstum. **Folge für Teil 2:** Status gibt es nur über
     `WorkOperation`; Teil 2 braucht dann eine Zuordnung Kürzel → `WorkOperation` (über Sub-FA bzw. Vater-
     Sub-FA + `OperationNumber`) und eine Regel für Kürzel ohne `WorkOperation` (Rückfrage 6 in
     [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]]).
   - **W — erkannte `WorkOperation`:** zeigt, was BDE/Tracking kennen; Status in Teil 2 direkt ableitbar.
     Aber: nur an materialisierten Sub-FAs, Kürzel der direkten Kinder erscheinen beim Vater, unbekannte/
     ausgeschlossene/mehrdeutige Kürzel fehlen, der Default-AG `01` erscheint zusätzlich — die Zeile weicht
     damit vom Sage-Bild ab.
   - Zusatz bei S: Sollen Kürzel der Ausschlussliste (z. B. `STO`, `XXX`) trotzdem angezeigt werden
     (Annahme der Spec: ja, Sage 1:1)?
   - Vorher P-K1/P-K2 ausführen. Die Spec wählt nicht.
2. **Wo erscheinen die Symbole?** (Notiz-Punkt 1; Folgen je Quelle siehe Tabelle im Lösungsentwurf)
   - **A** direkt in den Baumzeilen je (Sub-)FA — widerspricht der v1.45-Freigabe „Inhalt erst beim
     Öffnen laden, Seitenaufbau nur schlanker Existenz-Check“ und braucht eine gebündelte Abfrage je Seite
     (kein N+1); das Seiten-HTML wächst mit der Seitengröße (bis 5000 Strukturen bei „Alle“).
     Zusatzfrage bei A: Bleibt das Modal wie v1.45, wird es auch auf Symbole umgestellt, oder entfällt es?
   - **B** im bestehenden Modal — v1.45-Freigabe bleibt gültig, kleinster Diff, aber kein Überblick ohne Klick.
   - Die Notiz gibt hier **keine** Empfehlung, sie benennt nur die Kollision. Die Spec wählt nicht.
3. **Was zeigt ein Symbol?** (Notiz-Punkt 2; bei Quelle S ist das Kürzel gesetzt, offen bleibt dann nur
   der Namensweg) Empfehlung der Notiz: Kürzel/Nummer des Arbeitsgangs bzw.
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

1. Symbol-Quelle — S (Sage-Kürzel aus FaHierarchyNode.Arbeitsschritte) oder W (erkannte WorkOperation); bei S: Ausschlussliste auch in der Anzeige filtern? →
2. Wo erscheinen die Symbole — A (Baumzeilen) oder B (Modal); bei A: Modal behalten/umstellen/entfallen? →
3. Was zeigt ein Symbol — OperationNumber oder SageArbeitsplatznummer sichtbar; Name nur aria-label oder auf Touch sichtbar? →
