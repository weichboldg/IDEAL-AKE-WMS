---
type: spec
title: "FA-Struktur Arbeitsgänge als Symbole — Teil 1: Symbole statt Zeilen (reine Anzeige)"
slug: 2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-1-spec
status: Entwurf
created: 2026-10-06
updated: 2026-10-07
source_backlog: "[[2026-10-06-fa-struktur-arbeitsgaenge-symbole]]"
anhaenge:
  - "[[sage-fa-struktur-arbeitsschritte.png]]"
depends_on: ""
task: ""
worktree: ""
branch: ""
affected_code:
  - "Quelle S entschieden (Antwort 1, 2026-10-06): Symbole = Kuerzel aus FaHierarchyNode.Arbeitsschritte je Baumzeile, Ausschlussliste NICHT gefiltert. Alle Quelle-W-Pfade der Anzeige sind verworfen (unten als VERWORFEN markiert); den Status liefert W erst in Teil 2."
  - "Ort entschieden (Antwort 2, Keyuser 2026-10-07): Variante A — Symbole direkt in den Baumzeilen. Symbol-Inhalt entschieden (Antwort 3): Sage-Kuerzel sichtbar, Name per aria-label + title; Anzeige beim Antippen spaeter (nicht in diesem Teil)."
  - "VERWORFEN (Antwort 2): IdealAkeWms/Views/FaHierarchy/_FaWorkOperationsModal.cshtml als Ort der Symbole (Variante B). Das Modal bleibt inhaltlich wie v1.45 (Z.49-62 je AG eine Zeile) — Bestaetigung offen, Rueckfrage 4"
  - "IdealAkeWms/Views/FaHierarchy/_FaHierarchyNode.cshtml — neue Zelle mit Symbolreihe je Knotenzeile (Zeile Z.55-108, Spalten mit data-col-key Z.72-107) aus n.Arbeitsschritte des Knotens (Feld ist schon geladen). VERWORFEN (Antwort 1): Symbole aus den Arbeitsgaengen des Knotens (Quelle W)"
  - "IdealAkeWms/Views/FaHierarchy/Index.cshtml — neue <th data-col-key> in Kopf- und Filterzeile (Z.105-126), colspan 8 -> 9 (Z.133, Z.204), column-config-JSON (ab Z.282)"
  - "IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs — neue Spalte in ViewConfig FaHierarchyStructure (Z.450)"
  - "IdealAkeWms/Controllers/FaHierarchyController.cs — Index: keine AG-Abfrage; EIN Katalog-Lookup Kuerzel -> Werkbank-Name je Seitenaufbau (IProductionWorkplaceRepository.GetAllOrderedAsync, IProductionWorkplaceRepository.cs:7) fuer aria-label/title. VERWORFEN (Antwort 2): WorkOperations-Endpunkt (Z.139-155) auf Knoten-Kuerzel umstellen (Variante B). VERWORFEN (Antwort 1): gebuendelte AG-Abfrage im Index (Quelle W)"
  - "VERWORFEN (Antwort 1): IdealAkeWms/Data/Repositories/IWorkOperationRepository.cs + WorkOperationRepository.cs — neue AG-Projektion fuer viele HauptFAs war nur fuer Quelle W noetig; Teil 1 aendert das Repository nicht (eine AG-Abfrage fuer den Status plant Teil 2)"
  - "IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs — Tokenisierung von FaHierarchyNode.Arbeitsschritte als reine, unit-testbare Funktion (gleiche Regel wie WorkOperationStructureDetectionService.cs:33/:112-113 (v1.47.4; v1.47.1: :110-111)). VERWORFEN (Antwort 2): FaWorkOperationsViewModel.Create (Z.168-199) auf Knoten-Kuerzel umstellen (Variante B). VERWORFEN (Antwort 1): WorkOperationSummary (Z.210) als Datenquelle der Symbole"
  - "IdealAkeWms/Models/FaHierarchyNode.cs — nur lesen: Arbeitsschritte (Z.41), Arbeitsbereich (Z.40); keine Modellaenderung"
  - "IdealAkeWms/wwwroot/css/site.css — Symbol-Stil (Kontrast WCAG AA), falls Bootstrap-Badges nicht reichen"
  - "docs/TESTSZENARIEN.md + secondbrain/tests/testszenarien-index.md — TS-Kapitel naechste freie Nummer zum Umsetzungszeitpunkt"
beantwortete_rueckfragen:
  - "1 Symbol-Quelle -> S fuer die Anzeige (FaHierarchyNode.Arbeitsschritte je Baumzeile, 1:1 wie Sage, auch an Blaettern), W (WorkOperation) liefert den Status in Teil 2; Ausschlussliste in der Anzeige NICHT filtern (Gerald 2026-10-06, Freigabe-Block Nr. 1)"
  - "2 Ort -> A, Symbole direkt in den Baumzeilen (Keyuser 2026-10-07, Freigabe-Block Nr. 2); hebt die v1.45-Freigabe 'Inhalt erst beim Oeffnen laden' fuer die Symbole auf. Zusatz 'Modal bleibt fuer Details' ist laut Antwort nur Vorschlag der Koordination -> Rueckfrage 4"
  - "3 Symbol-Inhalt -> Sage-Kuerzel sichtbar (z. B. KA); Name per aria-label/Tooltip; Anzeige beim Antippen spaeter (Keyuser 2026-10-07, Freigabe-Block Nr. 3)"
open_questions:
  - "(Freigabe-Block Nr. 4, neu 2026-10-07; Rest aus Nr. 2) Modal bestaetigen: Bleibt das Arbeitsgaenge-Modal (v1.45, je AG eine Zeile mit Werkbank/Sage-Nr.) unveraendert neben den neuen Symbolen im Baum? Antwort 2 nennt das nur als Vorschlag der Koordination."
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

**Entschieden (Antwort 1, Gerald 2026-10-06): Quelle S.** Die Symbole zeigen die Sage-Kürzel aus
`FaHierarchyNode.Arbeitsschritte` (`Models/FaHierarchyNode.cs:41`) je Baumzeile, 1:1 wie Sage, auch an
Blättern; Kürzel der Ausschlussliste werden **nicht** herausgefiltert. Den Status (Teil 2) liefert W: die
`WorkOperation` des zuständigen Sub-FA mit `BdeBookingQuantity.IsFinal` (Zuordnungsregel: Antwort 6 in
[[2026-10-06-fa-struktur-arbeitsgaenge-symbole-teil-2-spec]]). Teil 1 liest **keine** `WorkOperation`.

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
- Arbeitsgänge als Symbole statt als Zeilen — Quelle S (Sage-Kürzel je Baumzeile wie im Vorbild,
  Antwort 1), **direkt in den Baumzeilen** der FA-Struktur (Antwort 2 = Variante A, Keyuser 2026-10-07; vormals
  „am in Rückfrage 2 gewählten Ort“ — überholt). ~~W: erkannte `WorkOperation` je Sub-FA~~ — verworfen
  (Antwort 1, 2026-10-06).
- Sichtbar ist das Sage-Kürzel, der Werkbank-Name per `aria-label` und `title` (Antwort 3).
- Reine Anzeige: alle Symbole **einheitlich neutral**, kein Status.
- Leitplanke aus Notiz-Punkt 3 (siehe „Fachliche Anforderungen“ 4).

**Out-of-Scope**
- Jeder Status (erledigt/offen/überfällig) und jede Statusfarbe → Teil 2 (dort aus W:
  `WorkOperation`/`BdeBookingQuantity.IsFinal`).
- Jedes Lesen von `WorkOperation` für die Symbole (Quelle W verworfen, Antwort 1).
- BDE-Terminal, BDE-Auftragsanzeige → Teil 3.
- Änderungen an Struktur-Erkennung, `WorkOperation`-Anlage, Sync, Datenmodell.
- Zählzeile, Lückenanzeige „— kein Arbeitsgang“ und Warnung `UnmatchedOperations` aus v1.45 bleiben
  inhaltlich unverändert (`_FaWorkOperationsModal.cshtml:8-18`, `:39-46`, `:68-84`).
- Spalte Arbeitsbereich/„Abteilung“ (besteht, siehe Vorbild); Knoten `FR-Zuschnitt` und Spalte Artikeltyp.
- Name beim **Antippen** sichtbar machen (Popover o. ä.) — „später“ laut Antwort 3, nicht in diesem Teil.
- Das Arbeitsgänge-Modal (v1.45) wird nicht umgebaut (Antwort 2 „Modal bleibt für Details“, Bestätigung
  Rückfrage 4).

## Fachliche Anforderungen

1. **Datenquelle — entschieden: Quelle S (Antwort 1, 2026-10-06).** (Bis zur Rework-Fassung 2026-10-06 stand
   hier „unverändert `WorkOperation`“, danach „offen, Rückfrage 1“ — beides **überholt**.)
   - ~~**Quelle W — erkannte `WorkOperation`:** je materialisiertem Knoten, Zuordnung Knoten → AG über
     `SubFA == ProductionOrder.SubOrderNumber` und `OrderNumber == HauptFA`
     (`FaWorkOperationsViewModel.Create`, `FaHierarchyTreeViewModel.cs:168-199`). Reihenfolge der Symbole =
     `Sequence` (dort Z.177).~~ — **verworfen für die Anzeige (Antwort 1, 2026-10-06)**; W liefert nur den
     Status in Teil 2.
   - **Quelle S — Sage-Kürzel des Knotens (gilt):** `FaHierarchyNode.Arbeitsschritte` (`FaHierarchyNode.cs:41`)
     der Zeile selbst, tokenisiert wie die Erkennung (nur Leerzeichen, leere Teile weg, getrimmt —
     `WorkOperationStructureDetectionService.cs:33`, `:110-111`), Reihenfolge wie im Feld. Das Feld ist im
     Index bereits für **alle** Knoten geladen (`FaHierarchyController.cs:67`, `GetAllAsync` liefert ganze
     Entitäten, `FaHierarchyNodeRepository.cs:28-32`) — keine zusätzliche Abfrage für die Kürzel selbst.
   - **Befund: Wie aus `Arbeitsschritte` eine `WorkOperation` wird** (bleibt als Grundlage für Teil 2 stehen;
     für die Anzeige in Teil 1 nicht mehr umzusetzen; Bündel,
     `IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs`):
     - Nur Knoten mit `SubFA != 0` erzeugen Arbeitsgänge (`:98`). **DirectChildren-Scope:** eigene Zeile
       **plus** alle direkten Kinder (`VaterFA == SubFA`) (`:73-76`, `:101-102`). — **Überholt seit v1.47.4**
       (Teil-2-Antwort 7 W-1, [[2026-10-07-v1-47-4-arbeitsgang-erkennung-kind-sub-fa]]): eigene Zeile plus nur
       die direkten **Blatt**-Kinder (`SubFA == 0`), `WorkOperationStructureDetectionService.cs:103-104`
       (v1.47.4). Die übrigen Zeilenangaben dieses Befunds sind v1.47.1; ab alter Z.92 seit v1.47.4 +2.
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
       ~~Ist das Kind selbst Sub-FA, trägt es dasselbe Kürzel zusätzlich als eigene `WorkOperation` (doppelt
       erfasst).~~ — überholt seit v1.47.4: nur noch **Blatt**-Kinder landen beim Vater, ein Kind-Sub-FA trägt
       seine Kürzel nur selbst. Beispiel laut Screenshot (WMS-Gleichheit ungeprüft, P-K2): Zeile
       `G-KT-Korpus-1320-660-760-…` zeigt in Sage nur `PG`; unter W stünden dort `PG` plus die Kürzel seiner
       direkten Kinder (`LS`, `EG`, `KA`, `SW`, `VM` …, abzüglich ausgeschlossener/unbekannter) — seit v1.47.4
       nur noch die der **Blatt**-Kinder (z. B. ohne `VM` der Mittelholmblende, Antwort 7 in Teil 2).
     - *`WorkOperation` ohne Kürzel:* Default-AG `"01"`; Kürzel, die in Sage nachträglich entfernt wurden.
     - Quelle S zeigt damit das Sage-Bild 1:1, Quelle W das, was BDE/Tracking kennen. Antwort 1 hält fest
       „W und S sollen ja gleich sein“: W wird aus denselben Kürzeln erzeugt; **auf Zeilenebene** sind die
       Mengen laut obigem Befund aber nicht gleich. Die Brücke Kürzel → `WorkOperation` ist Sache von Teil 2
       (Antwort 6 dort, inkl. der dort dokumentierten Widersprüche zum Erkennungs-Scope).
   - **Ausschlussliste:** Kürzel auf `Sync:ProductionWorkplaceSyncAusschlussliste` (z. B. `STO`, `XXX`)
     werden in der Anzeige **nicht** gefiltert — sie erscheinen wie jedes andere Kürzel (Antwort 1). Teil 1
     liest den Service-Key daher gar nicht.
2. **Symbol-Inhalt — entschieden (Antwort 3, Keyuser 2026-10-07):** sichtbar ist das **Sage-Kürzel** (z. B.
   `KA`); der Name steht in `aria-label` und `title`. Name beim Antippen: später (out of scope). (Vormals
   „gemäß Rückfrage 3“ — überholt.) Der Name kommt aus dem Werkbank-Katalog
   (`ProductionWorkplace.ArbeitsschrittCode` → `Name`, wie `WorkOperationStructureDetectionService.cs:80-88`:
   getrimmt, case-insensitiv); er fehlt bei unbekannten/ausgeschlossenen Kürzeln, bei **mehrdeutigen** Kürzeln
   (mehrere Werkbänke) wird kein Name gewählt — dann wie unbekannt (Kürzel als `aria-label`). *Nur noch zur
   Information (Quelle W verworfen, Antwort 1 — relevant für Teil 2):* was die Felder einer `WorkOperation`
   enthalten:
   - `OperationNumber` = Struktur-Kürzel (z. B. `KA`), gesetzt beim Anlegen durch
     `IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs:169` (`OperationNumber = token`).
     Der Default-AG trägt `"01"` (`BdeDefaultWorkOperationService`, [[fallstricke]] §16 „Existenz-Check des
     Default-AG“).
   - `Name` = Kopie von `ProductionWorkplace.Name` zum Anlagezeitpunkt
     (`WorkOperationStructureDetectionService.cs:170`, `Name = val.Wp.Name`).
   - Werkbank-Nummer = `ProductionWorkplace.SageArbeitsplatznummer`, live über die FK
     (`WorkOperationSummary`, `FaHierarchyTreeViewModel.cs:210`; heute im Modal Z.60).
3. Knoten ohne Arbeitsgang bleiben sichtbar mit Strich (Antwort 3 der v1.45-Freigabe). Mit Quelle S
   heißt das: Knoten mit leerem `Arbeitsschritte` (im Vorbild z. B. die Wurzel).
4. **Leitplanken (verbindlich, unabhängig von Rückfrage 2–3):**
   - Symbol-Inhalt ist **Text** (Kürzel), nicht nur Form oder Farbe. Farbe trägt in Teil 1 keine Bedeutung.
   - Kontrast WCAG AA: 4,5:1 für den Symboltext, 3:1 für die Symbolkontur gegen den Zeilenhintergrund
     (auch gegen Hover-/Hervorhebungsfarben der Baumzeile).
   - Der Name ist **nicht nur per `title`-Tooltip** erreichbar: `title` erscheint an Touch-Terminals der
     Fertigung nicht. Zusätzlich `aria-label` je Symbol (Antwort 3: „Name per aria-label/Tooltip“); eine
     sichtbare Namensanzeige auf Touch kommt später (vormals „entscheidet Rückfrage 3“ — überholt). Ohne
     eindeutigen Werkbank-Treffer (unbekanntes, ausgeschlossenes oder mehrdeutiges Kürzel): Kürzel als
     `aria-label`-Text, kein erfundener Name.
   - Die Symbolreihe bricht um statt die Zeile horizontal zu sprengen.

## Technischer Loesungsentwurf

Muster: bestehende Badge-Darstellung des Kürzels (`_FaWorkOperationsModal.cshtml:58`,
`badge bg-secondary`), Typ-Icons mit Form + Farbe + `title`/`aria-label` (`_FaHierarchyNode.cshtml:27-53`,
`:85-88`). Keine neue UI-Bibliothek. **frontend-design-Skill ist im Dev-Lauf Pflicht** (Views/CSS).

Quelle ist entschieden: **S** (Antwort 1). Ort ist entschieden: **Variante A — Baumzeilen** (Antwort 2,
Keyuser 2026-10-07). (Vormals „Offen ist nur noch der Ort … keine ist gewählt“ — überholt.)

| | Quelle S (Sage-Kürzel `Arbeitsschritte`) — gilt |
|---|---|
| **Variante A (Baum) — gilt** | Kürzel je Zeile **inkl. Blätter**, wie Sage. Keine AG-Abfrage — Feld ist geladen (`FaHierarchyController.cs:67`); für Namen **ein** Katalog-Lookup `ArbeitsschrittCode → Name` je Seitenaufbau. HTML-Wachstum (Kollision v1.45, unten) wird mit Antwort 2 in Kauf genommen. |
| ~~**Variante B (Modal)**~~ | ~~Das Modal listet heute nur Wurzel und Sub-FAs (`FaHierarchyTreeViewModel.cs:185`); Kürzel der Blätter fehlten dort.~~ — verworfen (Antwort 2). |

> [!failure]- Verworfen (Antwort 1, 2026-10-06): Spalte „Quelle W (`WorkOperation`)“
> - ~~Variante A + W: Symbole nur an materialisierten Knoten (`SubFA != 0`), inkl. Kürzel der direkten
>   Kinder. Eine gebündelte AG-Abfrage je Seite.~~
> - ~~Variante B + W: wie bisher skizziert, kleinster Diff.~~

**Variante A — Symbole direkt in den Baumzeilen (gilt, Antwort 2)**
- Neue Spalte „Arbeitsgänge“ in der Tree-Table. Pflichten der bestehenden Baumtabelle:
  `<th data-col-key>` in Kopf- und Filterzeile (`Index.cshtml:105-126`), `colspan="8"` → 9 an den
  Struktur-Kopfzeilen (`Index.cshtml:133`, `:204`), Registrierung in `ColumnDefinitions.FaHierarchyStructure`
  (`ColumnDefinitions.cs:450`, sonst Prefs-API 400 — [[fallstricke]] §3 „Neuer viewKey ohne
  `ColumnDefinitions`-Registrierung“), Eintrag im column-config-JSON (`Index.cshtml:282ff`), optional
  `data-f-<key>` für den Baum-Spaltenfilter (`_FaHierarchyNode.cshtml:60-67`).
- Daten (Quelle S): `n.Arbeitsschritte` je Knoten ist bereits geladen (`FaHierarchyController.cs:67`) —
  **keine** AG-Abfrage; Tokenisierung in der reinen Funktion; für den Namen höchstens **ein** Katalog-Lookup
  `ArbeitsschrittCode → Name` je Seitenaufbau (kein N+1). Der Existenz-Check `HasWorkOperations`
  (`FaHierarchyController.cs:91-95`) bleibt unverändert.
- ~~Daten (Quelle W): **eine** gebündelte AG-Abfrage je Seitenaufbau über die HauptFA-Nummern, Projektion
  (`OrderNumber`, `SubOrderNumber`, `OperationNumber`, `Name`, `Sequence`, `SageArbeitsplatznummer`), Muster
  `WorkOperationRepository.cs:120-128`, `Contains` → `OPENJSON`; ersetzt den Existenz-Check.~~ —
  **verworfen (Antwort 1, 2026-10-06)**. Eine gebündelte AG-Abfrage plant erst Teil 2 für den Status.
- **Kollision mit v1.45 (zu benennen, nicht zu verschweigen):** Die Freigabe von
  [[2026-09-23-fa-struktur-arbeitsgaenge-anzeige-spec]] lautet „Inhalt wird erst beim Öffnen geladen,
  Seitenaufbau nur schlanker Existenz-Check“. Grund dort: das Seiten-HTML wächst sonst mit der
  Seitengröße, bei „Alle“ bis `AllCap = 5000` Strukturen ([[fallstricke]] §3 „Pagination-AllCap 5000“).
  Variante A rendert die Symbole in **jede** Knotenzeile — auch in die initial eingeklappten
  (`_FaHierarchyNode.cshtml:68`, `display:none`, aber im HTML). Variante A **hebt diese Freigabe für die
  Symbole auf** — mit Antwort 2 (Keyuser 2026-10-07) so entschieden. Der Existenz-Check und das Laden des
  Modals beim Öffnen bleiben unverändert. ~~Mögliche Begrenzung: Symbole nur bis zu einer Seitengröße, darüber
  Hinweis~~ — nicht verlangt, entfällt; die Ladezeit bei „Alle“ prüft S6.
- Erfordert: View-Zelle, Tokenisierungs-Funktion mit Unit-Test, Spalten-Registrierung, Katalog-Lookup für
  Namen (eine Abfrage, `IProductionWorkplaceRepository.GetAllOrderedAsync`, `IProductionWorkplaceRepository.cs:7`;
  Lookup getrimmt + case-insensitiv wie `WorkOperationStructureDetectionService.cs:80-88`). ~~(Quelle W) Index-Umbau, Repository-Methode, ViewModel-Feld je Knoten,
  ToQueryString-Test des `Contains`~~ — verworfen (Antwort 1).

~~**Variante B — Symbole im bestehenden Modal**~~ — **verworfen (Antwort 2, Keyuser 2026-10-07).** Text bleibt
als Historie; das Modal wird in Teil 1 nicht geändert (Bestätigung: Rückfrage 4).
- `_FaWorkOperationsModal.cshtml`: je Knoten **eine** Zeile; die Spalten „Kürzel/Werkbank/Sage-Nr.“
  (Z.25-27) werden zu einer Spalte „Arbeitsgänge“ mit Symbolreihe aus den Knoten-Kürzeln. Zählzeile,
  Strich-Zeilen, Warnung `UnmatchedOperations` bleiben.
- Laden zeitlich unverändert (v1.45-Freigabe bleibt gültig, Inhalt erst beim Öffnen): Endpunkt
  `WorkOperations` (`FaHierarchyController.cs:139-155`), kein neuer Listen-View. Quelle S verlangt eine
  ViewModel-Änderung, weil die Einträge heute nur Wurzel + Sub-FAs umfassen (`FaHierarchyTreeViewModel.cs:185`)
  und die Kürzel aus dem Knoten statt aus den AGs kommen.
- ~~Kleinster Diff (Quelle W): eine View, ggf. CSS.~~ — verworfen (Antwort 1).

**Zugriffsschutz:** unverändert `[RequirePickingOrTrackingOrLeitstandAccess]` auf Klassenebene
(`FaHierarchyController.cs:23`); beide Varianten fügen keine Action hinzu.

## Migrations-/SQL-Auswirkungen

Keine. Reine Lesedarstellung auf `FaHierarchyNodes` und ggf. `ProductionWorkplaces` (Namens-Lookup);
`WorkOperations` liest Teil 1 nicht mehr (Antwort 1). Keine
Migration, kein `SQL/XX_*.sql`, `SQL/00_FreshInstall.sql` unverändert, `SQL/AgentJobs/` unberührt.

## Audit-Feld-Auswirkungen

Keine — es wird nichts geschrieben.

## Rollen / Zugriffsfilter

Unverändert (siehe oben). Kein neuer Filter, keine Änderung an `controller.md`/`RoleOverview`.

## Listen-View-Pattern (ADR 0005)

- Variante A (gilt): Erweiterung des bestehenden Baum-Listen-Views `FaHierarchyStructure` → Spaltenpflichten
  wie oben (`data-col-key` auf allen `<th>`, Registrierung, column-prefs). Pagination/Filterkarte
  bestehen bereits.
- ~~Variante B: kein Listen-View (Modal-Detailansicht, Begründung wie v1.45-Spec Abschnitt 2).~~ — verworfen
  (Antwort 2).

## Akzeptanzkriterien

Gemeinsam:
1. Arbeitsgänge erscheinen als Symbole, nicht als je eine Tabellenzeile (Quelle S, Antwort 1):
   - ~~*Quelle W:* je materialisiertem Knoten seine `WorkOperation`s in `Sequence`-Reihenfolge.~~ —
     verworfen (Antwort 1, 2026-10-06).
   - Je angezeigter Knotenzeile (auch Blätter) die Kürzel aus `Arbeitsschritte` in Feld-Reihenfolge; Prüfwerte
     laut Vorbild (sofern P-K1 sie bestätigt): `G-KT-A-BODEN-1320-660-V01` → `LS` `KA` `PG`;
     `FR-4301-40-20-1,25-1530,0MM-V01` → `ZS`; `G-KT-Mittelholmblende-40-760-V02` → `VM`;
     `G-KT-Korpus-Gepunktet` → `BE`.
2. Jedes Symbol zeigt sichtbar das Sage-Kürzel (Antwort 3); der Werkbank-Name steht in `aria-label` und
   `title` und ist per Tastaturfokus/Screenreader erreichbar, nicht nur per Maus-Hover (ohne eindeutigen
   Werkbank-Treffer: Kürzel). (Vormals „den in Rückfrage 3 festgelegten Text“ — überholt.)
3. Alle Symbole sind gleich gestaltet (keine Status- oder Farbunterscheidung).
4. Kontrast Symboltext ≥ 4,5:1, Symbolkontur ≥ 3:1 — gemessen am Testsystem.
5. Knoten ohne Arbeitsgang zeigen weiterhin den Strich; Zählzeile und `UnmatchedOperations`-Warnung
   unverändert.
6. Viele Arbeitsgänge an einem Knoten (≥ 8) brechen um, ohne horizontales Scrollen der Zeile.
7. Kein Schreibzugriff, Zugriffsschutz unverändert (Benutzer ohne Kommissionierung/Tracking/Leitstand:
   `/FaHierarchy` verweigert).

Variante A (gilt, Antwort 2; vormals „Nur Variante A“):
8. Seitenaufbau `/FaHierarchy` löst **keine** zusätzliche AG-Abfrage aus (nur der bestehende
   Existenz-Check) und höchstens **eine** Katalog-Abfrage (Query-Zählung im Dev-Lauf).
   ~~*Quelle W:* genau eine AG-Abfrage, ToQueryString-Test `OPENJSON`.~~ — verworfen (Antwort 1).
9. Neue Spalte ist ein-/ausblendbar und verschiebbar (column-prefs), `GET` der Prefs-API für
   `FaHierarchyStructure` liefert 200.

~~Nur Variante B:~~
10. ~~Seitenaufbau unverändert (nur Existenz-Check); Modal lädt weiterhin genau eine Struktur.~~ — verworfen
    (Antwort 2). Stattdessen: Das Arbeitsgänge-Modal (Knopf an der Kopfzeile) öffnet und zeigt unverändert wie
    v1.45 (Bestätigung Rückfrage 4).

Quelle S (gilt, Antwort 1 — vormals „Nur Quelle S“):
11. Tokenisierung per Unit-Test belegt: `"LS EG KA PG"` → `LS`,`EG`,`KA`,`PG`; doppelte Leerzeichen und
    Rand-Leerzeichen erzeugen keine leeren Symbole; `null`/leer → Strich.
12. Kürzel der Ausschlussliste `Sync:ProductionWorkplaceSyncAusschlussliste` (z. B. `STO`, `XXX`) und
    unbekannte Kürzel **werden angezeigt** wie alle anderen, in Feld-Reihenfolge, ohne Fehler (Antwort 1:
    „Ausschlussliste in der Anzeige NICHT filtern“). Prüfbar: Knoten mit `Arbeitsschritte = "STO KA"` zeigt
    `STO` und `KA`. Die Unterscheidung „hat `WorkOperation`“ ist Sache von Teil 2.
13. Blattzeilen (`SubFA = 0`) mit nicht leerem `Arbeitsschritte` zeigen ihre eigenen Kürzel (nicht die des
    Vaters); eine Sub-FA-Zeile zeigt nur die Kürzel ihrer eigenen Zeile, **nicht** die der direkten Kinder.
    (~~bei Variante B: ob Blätter im Modal erscheinen, legt Rückfrage 2 fest~~ — verworfen, Antwort 2.)

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (Nummer: nächste freie zum Umsetzungszeitpunkt), Index in
`secondbrain/tests/testszenarien-index.md` nachziehen. Bestehendes Kapitel TS-78 (v1.45) auf geänderte
Darstellung prüfen.

- **Vorbedingungen:** IDEAL-Testsystem, Struktur-Erkennung (v1.44.0) hat AGs angelegt, Benutzer mit
  Kommissionierung/Tracking/Leitstand.
- **S1 Symbole sichtbar:** `/FaHierarchy`, Struktur mit `KA`/`SW` in `Arbeitsschritte` aufklappen → in der
  Spalte „Arbeitsgänge“ der Baumzeilen Symbole in Feld-Reihenfolge, sichtbarer Text = Sage-Kürzel (Antwort 3);
  Maus-Hover zeigt den Werkbank-Namen. Arbeitsgänge-Modal über den Kopfzeilen-Knopf öffnet unverändert.
- **S1a Abgleich mit Sage (Quelle S, gilt):** Die Struktur aus dem Screenshot (`KTZ-L3-76-5Z1T RAL`) im WMS und
  in Sage „Fertigungsaufträge bearbeiten“ nebeneinander öffnen → je Zeile dieselben Kürzel wie in Sage
  (Stichproben: A-BODEN `LS KA PG`, FR-4301-…-1530 `ZS`, Mittelholmblende `VM`, Korpus-Gepunktet `BE`);
  auch Blattzeilen tragen Symbole; Wurzel ohne Kürzel → Strich.
- ~~**S1b (Quelle W) Abgleich mit Erkennung:** Sub-FA mit Kindern öffnen → Symbole = Kürzel der eigenen Zeile
  plus der direkten Kinder, ohne Kürzel der Ausschlussliste und ohne im Aktivitätsprotokoll als unbekannt
  gemeldete; Blattzeilen ohne Symbole.~~ — **verworfen (Antwort 1, 2026-10-06).**
- **S1c Ausschlussliste nicht gefiltert:** Knoten, dessen `Arbeitsschritte` ein Kürzel der Ausschlussliste
  enthält (Wert des Service-Keys `Sync:ProductionWorkplaceSyncAusschlussliste`, z. B. `STO`), öffnen → das
  Kürzel erscheint als Symbol wie jedes andere; Sub-FA-Zeile zeigt nur ihre eigenen Kürzel, nicht die der
  Kinder.
- **S2 Name erreichbar:** Tastatur-Fokus auf Symbol / Screenreader → Name wird angesagt (`aria-label`).
  ~~Touch-Gerät → Name auf dem in Rückfrage 3 festgelegten Weg erreichbar.~~ — Antippen-Anzeige kommt später
  (Antwort 3); am Touch-Gerät nur prüfen, dass Antippen keinen Fehler auslöst.
- **S3 Lücke:** Knoten mit leerem `Arbeitsschritte` → Strich, Zählzeile stimmt. (~~Sub-FA ohne AG,
  Quelle W~~ — verworfen, Antwort 1.)
- **S4 Viele AGs:** Umbruch, keine horizontale Scrollleiste in der Zeile.
- **S5 Kontrast:** Messung am Fertigungsterminal/Testgerät (WCAG AA).
- **S6 Große Seite (Variante A gilt):** Seitengröße „Alle“ → keine zusätzliche AG-Abfrage (~~eine, Quelle W~~ —
  verworfen), Ladezeit akzeptabel, Spalte per Spaltenauswahl aus-/einblendbar.
- **Negativ:** Benutzer ohne Berechtigung → Zugriff verweigert; Struktur ohne materialisierten Auftrag →
  Sage-Kürzel ohne Fehler (~~keine Symbole, Quelle W~~ — verworfen); unbekanntes Kürzel (nicht im
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

Stimmen alle vier Zeilen, gilt die Gleichheit für diese Stichprobe. Abweichung → vor dem Dev-Lauf klären
(Antwort 1 setzt die Gleichheit voraus; ursprünglich „vor Antwort 1 klären“ — überholt, Antwort liegt vor). Ob
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

- **Web-App:** ja (`_FaHierarchyNode.cshtml`, `Index.cshtml`, ggf. CSS, Tokenisierungs-Funktion,
  Katalog-Lookup im `FaHierarchyController.Index`, `ColumnDefinitions`). ~~Variante B zusätzlich
  `FaWorkOperationsViewModel`~~ — verworfen (Antwort 2). ~~Quelle W: Controller-Umbau, Repository~~ — verworfen
  (Antwort 1).
- **Vor dem Dev-Lauf:** Prüfschritt P-K1 (oben) durch Gerald (vormals „vor Antwort 1“ — überholt).
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:**

```bash
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

## Offene Rueckfragen

> [!success] Frage 1 beantwortet (Antwort 1, Gerald 2026-10-06)
> Quelle S für die Anzeige, W für den Status (Teil 2); Ausschlussliste in der Anzeige nicht filtern. Die Frage
> ist in den Frontmatter-Schlüssel `beantwortete_rueckfragen` verschoben und bleibt unten als Audit-Trail
> stehen. Die Nummerierung 2 und 3 bleibt unverändert und entspricht dem Freigabe-Block Nr. 2/3.

1. ~~**Symbol-Quelle: Sage-Kürzel oder erkannte Arbeitsgänge?**~~ — **beantwortet (Antwort 1)** (neu, aus dem Sage-Vorbild und dem
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
> [!success] Fragen 2 und 3 beantwortet (Keyuser 2026-10-07)
> Nr. 2: Variante A (Baumzeilen); Nr. 3: Sage-Kürzel sichtbar, Name per `aria-label`/Tooltip, Antippen später.
> Übernommen in Umfang, FA 2/4, Lösungsentwurf, AK 2/8-10/13, Testszenarien S1/S2/S6, Deploy. Beide in
> `beantwortete_rueckfragen` verschoben, Text unten als Audit-Trail. Neu Nr. 4: Bestätigung „Modal bleibt“
> (Antwort 2 nennt es nur als Vorschlag der Koordination).

2. ~~**Wo erscheinen die Symbole?**~~ — **beantwortet (Antwort 2: A)** (Notiz-Punkt 1; Folgen für Quelle S siehe Tabelle im Lösungsentwurf.
   Mit Quelle S entfällt bei A die gebündelte AG-Abfrage — es bleibt das HTML-Wachstum; bei B die Frage, ob
   das Modal auch Blätter listet.)
   - **A** direkt in den Baumzeilen je (Sub-)FA — widerspricht der v1.45-Freigabe „Inhalt erst beim
     Öffnen laden, Seitenaufbau nur schlanker Existenz-Check“ und braucht eine gebündelte Abfrage je Seite
     (kein N+1); das Seiten-HTML wächst mit der Seitengröße (bis 5000 Strukturen bei „Alle“).
     Zusatzfrage bei A: Bleibt das Modal wie v1.45, wird es auch auf Symbole umgestellt, oder entfällt es?
   - **B** im bestehenden Modal — v1.45-Freigabe bleibt gültig, kleinster Diff, aber kein Überblick ohne Klick.
   - Die Notiz gibt hier **keine** Empfehlung, sie benennt nur die Kollision. Die Spec wählt nicht.
3. ~~**Was zeigt ein Symbol?**~~ — **beantwortet (Antwort 3: Sage-Kürzel, Name per aria-label/Tooltip)** (Notiz-Punkt 2; bei Quelle S ist das Kürzel gesetzt, offen bleibt dann nur
   der Namensweg) Empfehlung der Notiz: Kürzel/Nummer des Arbeitsgangs bzw.
   der Werkbank, Name im Tooltip. Zu entscheiden: welches Kürzel steht **sichtbar** im Symbol —
   `OperationNumber` (z. B. `KA`, beim Default-AG `01`) oder `SageArbeitsplatznummer` (z. B. `2300`)?
   Hinweis: Ein reiner `title`-Tooltip erscheint an Touch-Terminals nicht; die Spec verlangt den Namen
   zusätzlich per `aria-label` (Leitplanke). Reicht das, oder soll der Name auf Touch sichtbar werden
   (z. B. Antippen öffnet Popover)?
4. **Modal bestätigen** (neu 2026-10-07; Rest aus Nr. 2): Antwort 2 sagt „Modal bleibt für Details (Vorschlag
   Koordination, nicht ausdrücklich beantwortet)“. Bleibt das Arbeitsgänge-Modal (v1.45: Knopf an der
   HauptFA-Kopfzeile, je erkanntem Arbeitsgang eine Zeile mit Kürzel/Werkbank/Sage-Nr.,
   `_FaWorkOperationsModal.cshtml:49-62`) unverändert neben den Symbolen im Baum? Die Spec setzt „bleibt
   unverändert“ als Arbeitsannahme (kleinster Eingriff); alternativ entfiele es (Rückbau Knopf + Endpunkt
   `FaHierarchyController.cs:139-155`).

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

1. Symbol-Quelle — S (Sage-Kürzel aus FaHierarchyNode.Arbeitsschritte) oder W (erkannte WorkOperation); bei S: Ausschlussliste auch in der Anzeige filtern? → **S für die Anzeige, W für den Status (Gerald 2026-10-06: „W und S sollen ja gleich sein“). Die Symbole zeigen die Sage-Kürzel aus `FaHierarchyNode.Arbeitsschritte` je Baumzeile, 1:1 wie Sage, auch an Blättern. W wird aus denselben Kürzeln erzeugt (`WorkOperationStructureDetectionService.cs:13`, `:90-110`, Scope = eigene Zeile + direkte Kinder des Sub-FA); den Status liefert der Arbeitsgang des zuständigen Sub-FA (Regel siehe Teil 2, Antwort 6). Ausschlussliste in der Anzeige NICHT filtern.**
2. Wo erscheinen die Symbole — A (Baumzeilen) oder B (Modal); bei A: Modal behalten/umstellen/entfallen? → **A – Symbole direkt in den Baumzeilen (Keyuser 2026-10-07). Modal bleibt für Details (Vorschlag Koordination, nicht ausdrücklich beantwortet).**
3. Was zeigt ein Symbol — OperationNumber oder SageArbeitsplatznummer sichtbar; Name nur aria-label oder auf Touch sichtbar? → **Sage-Kürzel sichtbar (z. B. `KA`) (Keyuser 2026-10-07). Name per aria-label/Tooltip; Anzeige beim Antippen später.**
4. Modal bestätigen — bleibt das Arbeitsgänge-Modal (v1.45) unverändert neben den Baum-Symbolen, oder entfällt es? →
