---
type: spec
title: "IDEAL FA-Struktur uebersichtlicher (Variante D: eine Zeile je HauptFA mit Kopfspalten, Baum per Chevron, Teile per Knopf)"
slug: 2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-2-spec
status: InUmsetzung
created: 2026-11-26
updated: 2026-11-26
source_backlog: "[[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu]]"
uebersicht: "[[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-uebersicht]]"
depends_on: "[[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-1-spec]]"
task: "[[2026-11-26-fa-liste-hauptfa-fa-struktur-goal-lauf]]"
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: IDEAL
affected_code:
  - "IdealAkeWms/Views/FaHierarchy/Index.cshtml (Kopfzeile fa-structure-group-head entfaellt; Unterkopf-Zeile nur bei mehreren Auftragskoepfen oder Fehler; neue Kopf-<th>/Filter-<th>/#column-config; Knopf „Teile anzeigen“/„Teile ausblenden“ in der Toolbar; Hinweiszeile Treffer in ausgeblendeten Teilen; Legende als Popover; Hervorheben-Feld BLEIBT)"
  - "IdealAkeWms/Views/FaHierarchy/_FaHierarchyNode.cshtml (Wurzelzeile Level 0 = HauptFA-Zeile: HauptFA-Nr., Badges Lebenslauf/Verwaist/Kombigeraet/Fehler, Zaehler, AG-Knopf; neue Kopfzellen + data-f-<key> je Zeile, nur an der echten Wurzel befuellt; .fa-toggle mit aria-expanded; data-search bleibt)"
  - "IdealAkeWms/Controllers/FaHierarchyController.cs (Index: Parameter mitTeilen; nach Pagination, vor _agStatus.ZuordnenAsync Blaetter ausduennen; RecursiveFilterSearch-Lesen BLEIBT)"
  - "IdealAkeWms/Services/FaHierarchyTreeBuilder.cs (neue statische Methode zum Ausduennen der Blaetter nach dem Aufbau, zaehlt ausgeblendete Teile je Struktur; Build unveraendert)"
  - "IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs (ViewModel: MitTeilen; FaHierarchyStructure: AusgeblendeteTeile)"
  - "IdealAkeWms/wwwroot/js/fa-hierarchy-tree.js (Struktur-Toggle-Code headRow/collapseStruct/expandStruct/toggleStruct raus, Wurzel-Toggle uebernimmt; aria-expanded mitschreiben; #faClientFilter-Block BLEIBT)"
  - "IdealAkeWms/wwwroot/css/site.css (.fa-toggle/.fa-toggle-placeholder Trefferflaeche 28 px; .fa-structure-group-head-Regeln entfallen; neu: Struktur- und Matchcode-Spalte in #faTree position: sticky beim Querscrollen)"
  - "IdealAkeWms/wwwroot/js/fa-hierarchy-tree.js (zusaetzlich: linker Versatz der Matchcode-Spalte = Breite der Struktur-Spalte per ResizeObserver; sticky nur, solange beide die ersten zwei sichtbaren Spalten sind; Hervorheben-Hinweis bei ausgeblendeten Teilen)"
  - "IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (FaHierarchyStructure: 15 Kopfspalten mit Keys/Labels wie ProductionOrders, alle sichtbar - kein DefaultHidden, weder fuer die bisherigen 9 noch fuer die neuen Spalten)"
  - "IdealAkeWms.Tests/Views/FaHierarchyStrukturSpaltenTests.cs (Drift-Waechter an neue Spalten, colspan-Pruefung Index + Partial; neuer Fall Keys/Labels der Kopfspalten = ColumnDefinitions.ProductionOrders) + TabellenZeilenfarbeTests.cs (table-plain-Freigabe fa-head-table wandert ins Partial) + Test Ausduennen"
  - "Views/Help/Index.cshtml (Abschnitt FA-Struktur: Legende/Filter-Erklaerung), docs/TESTSZENARIEN.md, docs/anwender"
open_questions:
  - "1. (beantwortet Gerald 2026-11-26: Variante D) Welche Variante: A (Entruempeln), B (A + Baugruppen-Baum, Teile per Schalter) oder C (Master-Detail)? Empfehlung B."
  - "2. (beantwortet Gerald 2026-11-26: Feld bleibt) Feld „Knoten hervorheben“ ersatzlos streichen (Empfehlung; Spaltenfilter und Strg+F decken es ab, der tote Auto-Expand-Zweig faellt mit)? Hebt die Unterscheidung aus [[2026-08-12-fa-struktur-darstellung-spec]] (In-Scope 3) auf."
  - "3. (beantwortet Gerald 2026-11-26: kein neues DefaultHidden) Spalten per Default ausblenden: SubFA und Soll/Fert. (Empfehlung) — weitere (Status „beschichtet“, Arbeitsbereich)?"
  - "4. (beantwortet Gerald 2026-11-26: Knopf statt Schalter; Blaetter mit Arbeitsschritten -> Annahme, Rueckfrage 10; URL/Benutzer -> Rueckfrage 7) Nur Variante B: Blaetter mit Arbeitsschritten trotzdem zeigen (Empfehlung, falls Pruefabfrage T-1 Treffer > 0) oder strikt alle Blaetter ausblenden? Schalter „Teile anzeigen“ nur in der URL (Empfehlung) oder je Benutzer speichern?"
  - "5. (beantwortet Gerald 2026-11-26: dieselben Spalten, Kopfzellen in Baumzeilen leer) Darstellung des aufgeklappten Baums: eingerueckte Zeilen in DENSELBEN Spalten wie die HauptFA-Zeile, Kopfzellen dort leer (Empfehlung, haelt Festlegung 2026-08-12) oder Baum als colspan-Teilbereich mit eigener Tabelle?"
  - "6. (beantwortet Gerald 2026-11-26: Badge + Kopfdaten-Zeile ab zwei Koepfen) Mehrere Auftragskoepfe (Kombigeraet): Kopfspalten leer + Badge + bestehende Kopfdaten-Tabelle als Zeile direkt unter der HauptFA-Zeile (Empfehlung), Regel „genau ein Kopf“ wie FA-Liste statt IsAmbiguous? Oder Werte je Kopf untereinander in den Zellen?"
  - "7. (beantwortet Gerald 2026-11-26: ein Knopf seitenweit, Zustand in der URL) Knopf „Teile anzeigen“ seitenweit in der Toolbar (Empfehlung, serverseitig, URL-Parameter) oder je HauptFA-Zeile? Zustand nur in der URL (Annahme) oder je Benutzer speichern?"
  - "8. (beantwortet Gerald 2026-11-26: alle Kopfspalten sichtbar, Labels wie FA-Liste) Kopfspalten: sichtbar die sechs heute in Kopf/Unterkopf gezeigten (Kunde, PPS-Status, KO, Fert.-Termin, Montage-Abt., AB-Nr.); Prio, Liefertermin und die Neu-Felder aus Teil 1 per Zahnrad, Default ausgeblendet — einverstanden?"
  - "9. (beantwortet Gerald 2026-11-26: einverstanden) Aus Variante A uebernommen: Legende als Popover + Hilfeseite, langer Erklaertext neben dem Zaehler auf die Hilfeseite, Hervorheben-Feld mit einzeiliger Erklaerung — einverstanden?"
  - "10. (beantwortet Gerald 2026-11-26: Teile mit Arbeitsschritten bleiben sichtbar) Annahme (Rueckfrage 4 erster Teil nicht beantwortet): Kaufteile/Material, an denen in Sage Arbeitsschritte stehen, bleiben auch bei „Teile ausblenden“ sichtbar, damit ihre Arbeitsgang-Symbole nicht verschwinden — so lassen?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: "D (Antworten 1-10, Gerald 2026-11-26)"
freigabe_von: Gerald
freigabe_am: 2026-11-26
---

## Ziel / Nutzen (das Warum)

Gerald: „FA-Struktur … ist derzeit viel zu komplex … Baumstruktur soll prinzipiell erhalten bleiben. Bitte mit
Vorschlag.“ Die Spec war eine Entscheidungsvorlage (Varianten A/B/C, Empfehlung B). **Gerald hat am 2026-11-26 eine
neue Variante D gewaehlt** (siehe Freigabe-Antworten 1-4): je HauptFA genau eine Zeile wie in der FA-Liste, die
Auftragsdaten als eigene Spalten (Zahnrad wie FA-Liste), darunter per Chevron der Baum; im Baum standardmaessig nur
Baugruppen, Teile per Knopf. Mit Teil 1 wird die FA-Struktur der einzige Ort fuer die Sub-FA-Sicht — umso wichtiger,
dass sie lesbar ist.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** Seite `/FaHierarchy` (Index-View, Knoten-Partial, Baum-Skript, Spalten-Definition, Ausduennen der Blaetter
im Controller), nur Darstellung — Umsetzung **Variante D**.

**Out-of-Scope:** Datenquelle, Abgleich und Lebenslauf ([[2026-11-17-fa-struktur-abgleich-statt-full-replace-spec]],
ADR 0032), Arbeitsgang-Status-Regel (`ArbeitsgangStatusRegel`), Modal „Arbeitsgaenge“ (Inhalt), Seite „FA-Struktur (Sage live)“
(`SageSdataFaController`), alle anderen Listen. AKE: Seite ist dort nicht erreichbar (`[RequireHierarchischeStrukturAktiv]`,
`FaHierarchyController.cs:25`).

## Fachliche Anforderungen

### Was die Komplexitaet ausmacht (Ist-Stand, belegt im Buendel-Worktree)

| # | Treiber | Beleg |
|---|---|---|
| K1 | **Jede Stuecklistenposition ist eine Baumzeile**, auch Kaufteile und Material (Blaetter `SubFA = 0`). Die Tabelle ist „Struktur UND Stueckliste“. | `Models/FaHierarchyNode.cs:6-7`, `:24`; Partial je Knoten `_FaHierarchyNode.cshtml:114-117` |
| K2 | **Zwei Kopfzeilen je Struktur**: Kopfzeile mit bis zu 12 Elementen (Chevron, Ordner-Icon, „HauptFA n“, Lebenslauf-Badge, Kunde, Status, KO, FE, Abteilung, Verwaist/Mehrdeutig/Fehler, AG-Knopf, Knotenzahl) **plus** immer eine Unterkopf-Zeile mit eigener 6-Spalten-Tabelle. Bei eindeutigem Kopf stehen Kunde, Status, KO, FE, Abteilung **doppelt**. | Kopfzeile `Index.cshtml:143-213`, Unterkopf `:215-261` |
| K3 | **Drei Such-/Filterwege** mit unterschiedlicher Wirkung: Filterkarte (Strukturebene, Server), „Knoten hervorheben“ (Client, blendet nichts aus), Spaltenfilter (Client, blendet Zweige aus). Die Seite braucht zwei Erklaertexte, um den Unterschied zu erklaeren. | Filterkarte `:33-57`, Hervorheben + Text `:58-71`, Erklaertext `:92`; JS `fa-hierarchy-tree.js:260-279` und `:281-352` |
| K4 | **9 Spalten + Filterzeile mit 9 Eingaben** (3 Auswahllisten). SubFA (Belegnummer) und Soll/Fert. sind fuer den Ueberblick selten noetig. | `Index.cshtml:115-137`, `ColumnDefinitions.cs:638-654` |
| K5 | **Struktur-Zelle dicht**: Chevron + Typ-Icon + Artnr + Lebenslauf-Badge, darunter Einrueckung je Ebene. | `_FaHierarchyNode.cshtml:74-93` |
| K6 | **Drei Aufklapp-Ebenen**: Struktur, Knoten, „Alle auf/zu“; Standard zeigt nur Ebene 0. | `fa-hierarchy-tree.js:79-156`, `_FaHierarchyNode.cshtml:70` |
| K7 | Oberhalb der Tabelle bis zu 5 Bloecke: Legende, Filterkarte, Hervorheben, AG-Offset-Warnung, Zaehler + Erklaertext. | `Index.cshtml:9-109` |

Toter Zweig nebenbei: Das Benutzer-Setting „Rekursive Filtersuche“ wirkt hier nur im Hervorheben-Feld
(`fa-hierarchy-tree.js:274`, gelesen `FaHierarchyController.cs:121-129`) — der Spaltenfilter klappt Pfade immer auf.
*Nachtrag 2026-11-26:* Das Hervorheben-Feld bleibt (Freigabe-Antwort 2), damit bleibt auch dieses Lesen — kein toter Zweig.

Weitere Ist-Befunde fuer Variante D (Buendel-Worktree):

- Jede Struktur hat **genau eine** Wurzel: `Build` legt je echter Wurzel (`VaterFA == null`) und je Waise eine eigene
  `FaHierarchyStructure` an (`FaHierarchyTreeBuilder.cs:76-86`), `Roots.Add` genau einmal (`:113`). Waisen haben dieselbe
  `HauptFA` und dieselben `OrderInfos` wie die echte Struktur (`:83`, `:86`, `:104`).
- Die Wurzelzeile ist heute schon eine Level-0-`<tr class="fa-node-row">` mit allen 9 Baumspalten (`_FaHierarchyNode.cshtml:56-113`)
  und standardmaessig die einzige sichtbare Zeile (`:70`). Der Struktur-Toggle der Kopfzeile (`Index.cshtml:147`,
  `fa-hierarchy-tree.js:118-139`, `:213-218`) und der Wurzel-Toggle tun fast dasselbe.
- Auftragskopf: `structure.OrderInfos` liegt je Struktur schon im Speicher (`FaHierarchyTreeBuilder.cs:67-69`, `:104`) — keine neue Abfrage.
  Felder `FaHierarchyOrderInfo.cs:21-39`.
- **Zwei verschiedene Eindeutigkeitsregeln:** FA-Liste uebernimmt Kopfwerte nur bei `heads.Count == 1`
  (`ProductionOrdersController.cs:283`), Kombigeraet bei `Count > 1` (`:303`). Die FA-Struktur nennt eine Struktur
  „mehrdeutig“ erst bei mehr als einer **verschiedenen** `MontageAbteilung` (`FaHierarchyTreeBuilder.cs:70-73`) und nimmt
  als Kopf stumm `OrderInfos.FirstOrDefault()` (`Index.cshtml:141`). Zwei Koepfe mit gleicher Abteilung zeigen heute in der
  Kopfzeile den ersten Kopf, die Unterkopf-Tabelle aber beide — ohne Unterkopf-Tabelle waere das ein stilles Auswaehlen.
- `column-preferences.js` blendet Zellen **ueber den Spaltenindex** aus (`:174-179`) und laesst Zeilen mit `td[colspan]`
  stehen (`:176`); neue Spalten aus `#column-config`, die in gespeicherten Praeferenzen fehlen, werden eingefuegt
  (`:97-111`) und nach `defaultHidden` sichtbar/unsichtbar (`:117`).
- Drift-Waechter `FaHierarchyStrukturSpaltenTests`: Kopf-th, Filter-th, `#column-config`, `ColumnDefinitions` gleich
  (`:33-43`), **jede Knotenzeile eine Zelle je Spalte** (`:45-47`), jede `colspan` = Spaltenzahl und mindestens eine
  vorhanden (`:49-52`), `data-f-<key>` je Filter (`:54-61`). `TabellenZeilenfarbeTests.cs:83-84` haengt an `#faTree` und `fa-head-table`.
- `_FaHierarchyNode` wird nur von `FaHierarchy/Index.cshtml:264` und rekursiv von sich selbst benutzt (Grep `Views/`).

### Bestehende Festlegungen und wie die Varianten damit umgehen

| Festlegung | Quelle | A | B | C | **D (gewaehlt)** |
|---|---|---|---|---|---|
| Eine seitenweite Tabelle, ein `<tbody>` je Struktur, flache `<tr>` mit `data-parent-id`, Spalten fluchten, `column-preferences.js` | [[2026-08-12-fa-struktur-darstellung-spec]] In-Scope 2/6 | bleibt | bleibt | **aufgehoben** | **bleibt**; die Kopfzeile mit `colspan` entfaellt, die Wurzel-`<tr>` wird die HauptFA-Zeile (Freigabe-Antwort 5) |
| Spaltenfilter mit Baum-Semantik (Kontextpfad, Zweige ausblenden) | ebd. In-Scope 3 | bleibt | bleibt | neu zu bauen | bleibt; Kopfspalten filtern nur an der HauptFA-Zeile (Treffer = Struktur zugeklappt sichtbar) |
| Hervorheben-Feld als eigener, unterscheidbarer Weg | ebd. In-Scope 3 | ~~aufgehoben~~ (ueberholt: Feld bleibt, Freigabe-Antwort 2) | ~~aufgehoben~~ (ueberholt) | entfaellt | **bleibt** |
| Typ-Icons, Legende, Kontrast AA | ebd. In-Scope 1/4 | bleibt (Legende → Hilfe) | bleibt | bleibt | bleibt; Legende als Popover + Hilfe (Freigabe-Antwort 9) |
| Modal „Arbeitsgaenge“ je HauptFA, Laden beim Oeffnen | [[2026-09-23-fa-struktur-arbeitsgaenge-anzeige-spec]] | bleibt | bleibt | Inhalt wandert ins Detailfeld | bleibt; Knopf sitzt in der HauptFA-Zeile |
| AG-Symbole in Baumzeilen, Status nur ueber `ArbeitsgangStatusRegel` | [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-uebersicht]] Teil 1-3, Fallstrick „Arbeitsgang-Status nur ueber ArbeitsgangStatusRegel“ | bleibt | bleibt | bleibt | bleibt |
| Drei Aufklapp-Ebenen (Struktur, Knoten, Alle) | ebd., K6 | bleibt | bleibt | – | **zwei**: Struktur-Toggle = Wurzel-Toggle, „Alle auf/zu“ bleibt |

## Technischer Loesungsentwurf

> [!note] Entscheidung 2026-11-26
> Gerald hat **Variante D** gewaehlt (Abschnitt unten). A, B und C sind **verworfen** und bleiben nur als Historie stehen;
> einzelne Bausteine aus A/B (Unterkopf nur bei mehreren Koepfen, Blatt-Ausduennen, Hinweiszeile, Legende → Hilfe)
> gehen in D auf und sind dort ausdruecklich genannt. Der Arbeitsauftrag fuer den Dev-Lauf ist **nur** Abschnitt „Variante D“.

### Variante A — Entruempeln (nur Weglassen) — *verworfen 2026-11-26*

```
FA-Struktur                                      [Alle auf] [Alle zu] [⚙]
┌ Filter: HauptFA [____] Kunde [______] Suche [__________] [Filtern] ┐
│ Struktur        │Matchcode│Bezeichnung   │Arb.ber.│AG        │Komm│Status│
│ [filter]        │[filter] │[filter]      │[v]     │[filter]  │[v] │[v]   │
│▼ HauptFA 1060414  Kunde X  FE 14.07.2026          [Arbeitsgänge] 87 Kn.│
│  ⌂ 4711-000      IDEAL-A  Klimagerät     MO       KA SW      PG          │
│   ▸ ◆ 4711-100   RAHMEN   Rahmen geschw. BL       LA KA ✓               │
│   ▸ ◆ 4711-200   …                                                      │
│     • 0815-001   SCHR-M8  Schraube M8    …                               │
```

- Unterkopf-Tabelle nur noch bei **Kombigeraet** (`structure.IsAmbiguous`), sonst weg (K2: −1 Zeile je Struktur, keine Dopplung).
- Hervorheben-Feld, beide Erklaertexte und der `#faClientFilter`-JS-Block raus; `RecursiveFilterSearch`-Lesen im Controller raus (K3, K7).
- Legende und Filter-Erklaerung auf die Hilfeseite (Abschnitt FA-Struktur), auf der Seite ein Link „Hilfe“ (K7).
- `SubFA` und `Soll / Fert.` `DefaultHidden` (Zahnrad holt sie zurueck; gespeicherte Praeferenzen der Benutzer gelten weiter) (K4).
- **Aufwand:** klein (2 Views, 1 JS-Block loeschen, `ColumnDefinitions`, Hilfe). **Risiko:** niedrig, rein subtraktiv; nur Gewohnheit.
- **Bleibt komplex:** K1 — die Zeilenzahl, der eigentliche Hauptgrund.

### Variante B — Baugruppen-Baum (A + Teile per Schalter) — ~~Empfehlung~~ *verworfen 2026-11-26 (Bausteine in D)*

```
FA-Struktur                        [☐ Teile anzeigen] [Alle auf] [Alle zu] [⚙]
┌ Filter: HauptFA [____] Kunde [______] Suche [__________] [Filtern] ┐
│ Struktur         │Matchcode│Bezeichnung    │Arb.ber.│AG         │Komm│Status│
│▼ HauptFA 1060414  Kunde X  FE 14.07.2026        [Arbeitsgänge] 9 Baugr.│
│  ⌂ 4711-000       IDEAL-A  Klimagerät      MO       KA SW       PG          │
│   ▸ ◆ 4711-100    RAHMEN   Rahmen geschw.  BL       LA KA ✓                 │
│   ▸ ◆ 4711-200    WANNE    Kondensatwanne  BL       LA KA                   │
│   ▸ ◆ 4711-300    E-BOX    Schaltkasten    EL       VE                      │
```

- Alles aus A, dazu: Standard zeigt nur **Knoten mit eigener FA** (`SubFA != 0`: Wurzel + Baugruppen = die FA-Hierarchie).
  Kaufteile/Material per Schalter „Teile anzeigen“ (Toolbar, `form-check form-switch`, Beschriftung als Text).
- Umsetzung serverseitig (weniger Code als im JS): `FaHierarchyController.Index` bekommt `bool mitTeilen = false`; die
  Strukturauswahl `FilterHauptFas` (`:179-216`) laeuft weiter ueber **alle** Knoten (Suche nach einer Teilenummer findet die
  Struktur), nur vor `_treeBuilder.Build` (`:89`) werden Blaetter weggelassen:
  `filteredNodes.Where(n => mitTeilen || n.SubFA != 0 /* + Rueckfrage 4: || !string.IsNullOrWhiteSpace(n.Arbeitsschritte) */)`.
  Pagination/Zurücksetzen nehmen `mitTeilen` mit (Muster Filterkarte). Knotenzahl-Badge zeigt „n Baugruppen“ bzw. „n Knoten“.
- Treffer der Filterkarte nur in einem Teil → Struktur erscheint, Teil unsichtbar: Hinweiszeile „Treffer in ausgeblendeten Teilen –
  „Teile anzeigen“ einschalten“ (melden statt still), wenn `filterText` gesetzt und `!mitTeilen`.
- Die Teile einer Baugruppe bleiben ueber den Schalter und die bestehende Stueckliste (BOM-Bridge, ADR 0013) erreichbar —
  kein neuer Knopf in dieser Spec.
- **Aufwand:** klein bis mittel (A + ein Parameter, ein `Where`, ein Schalter, eine Hinweiszeile, Tests fuer den Filter).
  **Risiko:** mittel-niedrig — wer heute Teile im Baum sucht, braucht einen Klick mehr; Datenrisiko K1-Blaetter mit
  Arbeitsschritten (Pruefabfrage T-1) ist durch Rueckfrage 4 abgefangen. Spaltenfilter und Modal unveraendert.
- **Wirkung:** Zeilenzahl sinkt um den Blatt-Anteil (Pruefabfrage T-2 liefert die Zahl je HauptFA).

### Variante C — Master-Detail (Neubau, nicht empfohlen) — *verworfen 2026-11-26*

```
┌ Baum (schmal) ───────────────┐┌ Detail: 4711-100 Rahmen geschweißt ───────┐
│▼ 1060414 Kunde X  FE 14.07.  ││ Matchcode RAHMEN · Arb.ber. BL · Komm. PG  │
│  ⌂ 4711-000 Klimagerät       ││ Soll 1 / Fert. 0 · beschichtet              │
│   ▸ ◆ 4711-100 Rahmen  ◀──── ││ Arbeitsgänge: LA ✓  KA …                    │
│   ▸ ◆ 4711-200 Wanne         ││ Teile: 0815-001 Schraube M8  12 Stk  …      │
└──────────────────────────────┘└─────────────────────────────────────────────┘
```

- Links nur die Struktur-Spalte (+ AG-Symbole), rechts eine Detailkarte des gewaehlten Knotens mit allen Feldern, Teileliste
  und Arbeitsgaengen (Modal-Inhalt inline).
- Hebt die Tabellen-Festlegung aus 2026-08-12 auf (Zahnrad/Spaltenfilter/`column-preferences` fuer den Baum entfallen oder
  muessen neu gebaut werden), neues Detail-Partial + Endpunkt + JS-Auswahlzustand; an schmalen Terminals (offene Frage
  lg-Breakpoint aus 2026-08-12) passt die Zweiteilung schlecht.
- **Aufwand:** gross (neue Seite faktisch). **Risiko:** hoch (Neubau, Barrierefreiheit Fokusfuehrung, Terminal-Breite).

### Empfehlung — *ueberholt 2026-11-26 (Entscheidung D)*

~~**B.** Der groesste Treiber ist K1 (jede Stuecklistenposition eine Zeile); B nimmt ihn durch Weglassen per Default heraus,
ohne eine Festlegung ausser dem Hervorheben-Feld anzutasten, und ist per Schalter jederzeit umkehrbar. A allein laesst
K1 stehen; C baut neu, was 2026-08-12 bewusst vereinheitlicht hat (Ponytail-Leiter: Sprosse 1 „weglassen“ vor Sprosse 7).~~

~~Frontend (alle Varianten): Skill `frontend-design` ausdruecklich aufrufen; Bootstrap-Bausteine (`form-switch`, `btn-sm`,
`alert`), keine neue Bibliothek; Kontrast AA, Schalter mit sichtbarem Text, Bedeutung nie nur ueber Farbe.~~
(`form-switch` ueberholt: Knopf statt Schalter, Freigabe-Antwort 4.)

### Variante D — eine Zeile je HauptFA, Baum per Chevron — **gewaehlt (Gerald 2026-11-26)**

```
FA-Struktur                          [Teile anzeigen] [Alle auf] [Alle zu] [Legende] [Hilfe] [⚙]
┌ Filter: HauptFA [____] Kunde [______] Suche [__________] [Filtern]                          ┐
│ Knoten hervorheben [______________]  (markiert, blendet nichts aus)                          │
│ Struktur                    │Matchcode│Bezeichnung │Kunde  │PPS-St.│KO-T.   │Fert.-T.│M-Abt.│AB-Nr.│Arb.ber.│Arbeitsgänge     │Komm│Soll/Fert│SubFA  │Status│
│ [filter]                    │[filter] │[filter]    │[filt] │[v]    │[filt]  │[filt]  │[v]   │[filt]│[v]     │[filter]         │[v] │[filt]   │[filt] │[v]   │
│▸ ⌂ 1060414  4711-000 ⓛ      │IDEAL-A  │Klimagerät  │Kunde X│FREI   │01.07.26│14.07.26│MO    │A1234 │MO      │KA SW [Arbeitsg.]│PG  │1 / 0    │1060414│      │
│     9 Knoten · 78 Teile ausgeblendet                                                                                                                │
│▾ ⌂ 1060500  4800-000 [Kombigerät]│KOMBI │Kombigerät  │       │       │        │        │      │      │MO      │KA [Arbeitsg.]   │    │1 / 0    │1060500│      │
│   ┌ Kopfdaten: Montage-Abt. │Kunde │Status │KO-Termin │FE-Termin │AB-Nr. ┐  (colspan-Zeile, nur bei > 1 Auftragskopf)        │
│   │ MO │Kunde Y │FREI │… ┆ EL │Kunde Y │FREI │…                        ┘                                                         │
│   ▸ ◆ 4800-100              │RAHMEN   │Rahmen      │       │       │        │        │      │      │BL      │LA KA ✓          │    │1 / 0    │1060501│      │
│   ▸ ◆ 4800-200              │WANNE    │Wanne       │       │       │        │        │      │      │BL      │LA               │    │1 / 0    │1060502│      │
```

Kopfspalten (Kunde … AB-Nr.) sind nur in der HauptFA-Zeile befuellt; Baumzeilen tragen dort leere Zellen.

**D-1 HauptFA-Zeile = Wurzelzeile.** Die heutige Level-0-`<tr class="fa-node-row">` (`_FaHierarchyNode.cshtml:56-113`) wird
die HauptFA-Zeile; die Kopfzeile `tr.fa-structure-group-head` (`Index.cshtml:143-214`) entfaellt ganz. Ihr Inhalt wandert:

| heute (Kopfzeile/Unterkopf) | in D |
|---|---|
| Chevron Struktur (`Index.cshtml:147-151`) | Wurzel-`.fa-toggle` (`_FaHierarchyNode.cshtml:77-81`) |
| Ordner-Icon + „HauptFA n“ (`:152-155`) | Struktur-Zelle: Wurzel-Icon + **HauptFA-Nummer** fett vor der Artnr |
| Lebenslauf-Badge (`:156`) | bleibt in der Struktur-Zelle (`_FaHierarchyNode.cshtml:92`, gleiches Partial) |
| Kunde, Status, KO, FE, Abteilung als Badges (`:157-181`) + Unterkopf-Tabelle (`:215-252`) | **eigene Kopfspalten** (D-2); Unterkopf nur noch bei mehreren Koepfen (D-3) |
| Verwaist / mehrdeutig / Fehlerhaft (`:182-199`) | Badges in der Struktur-Zelle |
| Knopf „Arbeitsgaenge“ (`:202-209`) | Zelle `operations` der HauptFA-Zeile, hinter den AG-Symbolen |
| Knotenzahl (`:211`) | kleine Zeile in der Struktur-Zelle: „n Knoten · m Teile ausgeblendet“ (U-4) bzw. „n Knoten“ |
| Fehler-Alert (`:253-259`) | eigene `colspan`-Zeile unter der HauptFA-Zeile, nur bei `HasError` |

Den Struktur-Kontext (`OrderInfos`, Badges, Zaehler, `HasWorkOperations`) bekommt das Partial per `view-data` aus der
Index-Schleife; nur Level 0 liest ihn. Kopfzellen befuellt nur bei **echter Wurzel** (`!structure.IsOrphan`) und **genau einem**
Kopf; Waisen-Zeilen tragen das Badge „Verwaist“ und leere Kopfzellen (der Kopf gehoert zur echten Struktur).

**D-2 Kopfspalten (Wiederverwendung mit Teil 1).** Quelle ist ausschliesslich `structure.OrderInfos` (schon geladen,
`FaHierarchyTreeBuilder.cs:104`), keine neue Abfrage. Keys und Labels **identisch** mit der FA-Liste:

Die Kopfspalten sind **alle 15 Kopf-Felder (K)**, die die FA-Liste nach Teil 1 zeigt: die 7 bisherigen FA-Listen-Keys
plus die 8 Teil-1-Neu-Kopfspalten inkl. `pps-status` (Teil 1 Mapping „Neu“, Freigabe-Antworten 2 und 4 von
[[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-1-spec]]). **Alle sichtbar** ohne gespeicherte Praeferenz
(Freigabe-Antwort 8; bewusst anders als in der FA-Liste, wo die Neu-Spalten ausgeblendet starten), per Zahnrad abwaehlbar.

| `data-col-key` | Label | Quelle `FaHierarchyOrderInfo` | Beleg Key/Label FA-Liste | Default | Baum-Filter |
|---|---|---|---|---|---|
| `customer` | Kunde | `Kunde` | `ColumnDefinitions.cs:31` | sichtbar | Text |
| `pps-status` | PPS-Status | `Status` | Teil 1 Mapping „Neu“ (Teil-1-Freigabe-Antwort 4) | sichtbar | Auswahl |
| `konstruktions-termin` | KO-Termin | `KO_Termin` (dd.MM.yyyy) | `:37` | sichtbar | Text |
| `production-date` | Fert.-Termin | `FE_Termin` (dd.MM.yyyy) | `:53` | sichtbar | Text |
| `montage-abteilung` | Montage-Abt. | `MontageAbteilung` | `:36` | sichtbar | Auswahl |
| `ab-nummer` | AB-Nr. | `ABNr` | `:35` | sichtbar | Text |
| `prio` | Prio | `Prio` | `:34` | sichtbar (Freigabe-Antwort 8) | Text |
| `delivery-date` | Liefertermin | `Verladetermin_Vsl` (dd.MM.yyyy) | `:54` | sichtbar (Freigabe-Antwort 8) | Text |
| `ab-position` | AB-Pos. | `Pos` | Teil 1 Mapping „Neu“ | sichtbar | Text |
| `dienstleister` | Beschichter | `Dienstleister` | Teil 1 Mapping „Neu“ | sichtbar | Text |
| `ral` | RAL | `RAL` | Teil 1 Mapping „Neu“ | sichtbar | Text |
| `beschichtung-retour` | Beschicht. retour | `Beschichten_Retour` (dd.MM.yyyy) | Teil 1 Mapping „Neu“ | sichtbar | Text |
| `neuer-pt-pps` | Neuer PT (PPS) | `Neuer_PT_PPS` (dd.MM.yyyy) | Teil 1 Mapping „Neu“ | sichtbar | Text |
| `montagestunden` | Montage-Std. | `Montagestunden` | Teil 1 Mapping „Neu“ | sichtbar | Text |
| `bemerkung-uhrzeit` | Bemerkung/Uhrzeit | `Bemerkung_Uhrzeit` | Teil 1 Mapping „Neu“ | sichtbar | Text |

~~`prio`/`delivery-date`/Neu-Felder „ausgeblendet (Rueckfrage 8)“~~ *(ueberholt durch Freigabe-Antwort 8: alle sichtbar.)*

**Keine Kopfspalten aus den W-Feldern (Strukturwurzel) von Teil 1, auch nicht `material`.** Begruendung: Kopfspalten tragen
nur Auftragskopf-Werte, die es je Struktur genau einmal gibt (`structure.OrderInfos`); die W-Felder sind Felder **jedes**
`FaHierarchyNode` (`Models/FaHierarchyNode.cs:40-52`), also Knotenwerte. Als Kopfspalte nur an der HauptFA-Zeile befuellt
wuerde z. B. „Material“ an jeder Baugruppe leer stehen, obwohl sie ein eigenes Material hat — eine falsche Aussage. Der
Teil der W-Felder, der fuer den Baum zaehlt, ist schon als Baumspalte abgedeckt und an jedem Knoten befuellt:
`arbeitsbereich` → `workarea` (`_FaHierarchyNode.cshtml:96`), `komm-ziel` → `picking-target` (`:99`), `fertigungsmenge`
→ `quantity` „Soll / Fert.“ (`:105`), `arbeitsschritte` → `operations` mit Status-Symbolen (`:98`). Nicht im Baum sind
`material`, `artikelgruppe`, `hauptlagerplatz`, `bemerkung`, `fertigungsinfo` — als echte Knotenspalten (an jeder Zeile
befuellt) waeren sie eine neue Anforderung, die weder Backlog noch Freigabe-Antworten verlangen: **Zusatzfund, nicht mitbauen.**

- ~~Die sechs sichtbaren sind genau die Werte, die heute Kopfzeile + Unterkopf zeigen — kein Informationsverlust.~~
  *(ueberholt: alle 15 sichtbar.)* Die sechs heute in Kopfzeile + Unterkopf gezeigten Werte sind darunter — kein Informationsverlust.
- Reihenfolge: `structure`, `matchcode`, `description`, Kopfspalten, dann die bisherigen `workarea` … `status` (per Zahnrad umsortierbar).
- Bisherige 9 Spalten: **kein** neues `DefaultHidden` (Freigabe-Antwort 3). Die heutige Spalte „Status“ (`status`, beschichtet)
  behaelt Key und Label; der Kopf-Status heisst deshalb „PPS-Status“ (Kollision vermieden).
- Wiederverwendung: Ein gemeinsames Partial mit der FA-Liste ist nicht moeglich — `_ProductionOrderRow` bindet
  `ProductionOrderListItem`, das Baum-Partial `FaHierarchyTreeNode`. Wiederverwendet werden Keys, Labels, Datumsformat und
  die schon geladenen Kopfdaten; ein Test haelt Keys/Labels **aller 15** Kopfspalten deckungsgleich mit `ColumnDefinitions.ProductionOrders`
  (Teil 1 wird vorher gebaut, Uebersicht; damit existieren dort alle 15 Keys). `DefaultHidden` vergleicht der Test bewusst **nicht**
  (FA-Liste ausgeblendet, FA-Struktur sichtbar — Freigabe-Antwort 8).
- Filter: alle Kopfspalten im **Baum-Spaltenfilter** (Client, `data-f-<key>` an **jeder** Knotenzeile, leer ausser an der
  HauptFA-Zeile). Textfilter laufen ueber `ColumnFilterSyntax` Typ `text` (`fa-hierarchy-tree.js:289`, `:299`); Datumsspalten
  filtern deshalb als Text (`14.07.2026`, `.07.2026`), **nicht** mit KW/Bereich — `ponytail:` Baumfilter kennt nur `text`,
  Datums-Typ nachruesten, wenn verlangt. Auswahlfilter befuellen sich aus den Seitenwerten (`:368-388`).
- Semantik: ein Filter auf eine Kopfspalte trifft nur HauptFA-Zeilen → die Struktur bleibt zugeklappt sichtbar, andere
  Strukturen verschwinden (`:325-329`). Das ist gewollt.

**D-3 Mehrere Auftragskoepfe / Kombigeraet** (entschieden, Freigabe-Antwort 6). Regel wie FA-Liste: Kopfwerte nur bei
`OrderInfos.Count == 1` (`ProductionOrdersController.cs:283`); bei `Count > 1` bleiben **alle** Kopfzellen leer (kein stilles
`FirstOrDefault`), die Struktur-Zelle traegt das Badge „mehrdeutig (Kombigeraet)“ (bei `IsAmbiguous`) bzw. „n Auftragskoepfe“
(sonst), und **direkt unter der HauptFA-Zeile** steht die bestehende Kopfdaten-Zeile `tr.fa-structure-subhead` mit `fa-head-table`
(Markup aus `Index.cshtml:215-248` ins Partial bei Level 0 verschoben, Inhalt unveraendert, `colspan` = Spaltenzahl;
die `table-plain`-Freigabeliste `TabellenZeilenfarbeTests.cs:81-86` nennt dann `_FaHierarchyNode.cshtml` statt des zweiten `Index.cshtml`-Eintrags). Sie haengt nicht am Chevron (Kopfdaten, kein Baum) und
verschwindet nur mit der ganzen Struktur beim Spaltenfilter (`fa-hierarchy-tree.js:331-332` zeigt sie wieder). `colspan`-Zeilen
laesst `column-preferences.js` stehen (`:176`). Teil 1 F4 verweist genau auf diese Tabelle — bleibt gueltig.
`Count == 0`: Kopfzellen leer, Badge „ohne Kopfdaten“ (heute Text `Index.cshtml:251`; melden statt still).
Pruefabfrage T-3 zeigt, ob „mehrere Koepfe mit gleicher Abteilung“ vorkommt.
Badge-Texte festgeschrieben (Kritische Pruefung S-3; Antwort 6 „Kombigeraet“ ist damit abgedeckt): **„mehrdeutig (Kombigeraet)“**
bei `Count > 1` und `IsAmbiguous`, **„n Auftragskoepfe“** (n = `OrderInfos.Count`) bei `Count > 1` ohne `IsAmbiguous`,
**„ohne Kopfdaten“** bei `Count == 0`.

**D-4 Klappen.** Struktur-Toggle-Code entfaellt (`fa-hierarchy-tree.js:41`, `:62-65`, `:118-139`, `:213-218`, die
`headRow`/`setStructChevron`-Teile in `:142-156`, `:173-176`); der Wurzel-Chevron uebernimmt (`expandNode`/`collapseNode` `:79-98`).
Standard unveraendert: nur Level 0 sichtbar (`_FaHierarchyNode.cshtml:70`) = eine Zeile je HauptFA. „Alle auf/zu“ bleibt.
`aria-expanded` wird in `setChevron` (`:58-61`) mitgeschrieben (U-2); Trefferflaeche 28 px (U-1).

**D-5 Teile-Knopf und Ausduennen** (Bausteine aus B; entschieden: Knopf statt Schalter Freigabe-Antwort 4, ein Knopf
seitenweit mit Zustand in der URL Freigabe-Antwort 7, Teile mit Arbeitsschritten bleiben sichtbar Freigabe-Antwort 10).

- `FaHierarchyController.Index` bekommt `bool mitTeilen = false`. `FilterHauptFas` (`:179-216`) und `_treeBuilder.Build` (`:89`)
  laufen **unveraendert ueber alle Knoten** (Teilenummer findet die Struktur; Waisen-Erkennung `FaHierarchyTreeBuilder.cs:65`, `:78-80`
  bleibt gleich — deshalb **nach** dem Aufbau ausduennen, nicht davor).
- Nach der Pagination (`:94-97`), vor `_agStatus.ZuordnenAsync` (`:106`): bei `!mitTeilen` eine neue statische Methode in
  `FaHierarchyTreeBuilder` entfernt rekursiv Kinder mit `SubFA == 0` **und** leeren `Arbeitsschritte` (Freigabe-Antwort 10)
  und setzt `FaHierarchyStructure.AusgeblendeteTeile`. Strukturzahl/Pagination bleiben gleich; Modal „Arbeitsgaenge“
  (`WorkOperations`, eigener Aufbau `:159-171`) zeigt weiter alles.
- Knopf in der Toolbar (`data-table-toolbar`, `Index.cshtml:95`): `<a class="btn btn-sm btn-outline-secondary" aria-pressed="…">`
  mit sichtbarem Text **„Teile anzeigen“** bzw. **„Teile ausblenden“**, Link auf die aktuelle URL mit umgeschaltetem `mitTeilen`
  (Filter und Seite bleiben). Kein `form-switch`. Filterkarte traegt `mitTeilen` als Hidden-Feld; „Zuruecksetzen“ setzt ihn zurueck.
  Pagination arbeitet ueber `data-page` (`_Pagination.cshtml:43-68`); der gemeinsame Handler baut die Ziel-URL aus
  `window.location.href` und setzt nur `page`/`pageSize` (`wwwroot/js/site.js:118-136`, Kern `:126-135`) — `mitTeilen`
  bleibt beim Blaettern also ohne Zusatzcode erhalten (Kritische Pruefung H-1).
- Hinweiszeile (`alert alert-info`, `role="status"`), wenn `filterText` gesetzt, `!mitTeilen` und auf der Seite Teile ausgeblendet:
  „Die Suche kann in ausgeblendeten Teilen getroffen haben – „Teile anzeigen“ blendet sie ein.“
- Spaltenfilter und Hervorheben sehen nur gerenderte Zeilen. Bei `!mitTeilen` ergaenzt der Zaehler (`#faNodeCounter`,
  `fa-hierarchy-tree.js:159-166`) „(Teile ausgeblendet)“ — melden statt still.
- **Hervorheben ohne Treffer** (Kritische Pruefung S-4): Liefert das Feld „Knoten hervorheben“ (`#faClientFilter`,
  `fa-hierarchy-tree.js:260-279`) bei `!mitTeilen` und nicht leerer Eingabe **keinen** markierten Knoten, zeigt die Seite
  denselben Hinweis wie der Spaltenfilter: „Kein Treffer in den angezeigten Zeilen – Teile sind ausgeblendet, „Teile anzeigen“
  blendet sie ein.“ (`role="status"`, unter dem Feld; verschwindet bei Treffer oder leerem Feld). Ob Teile ausgeblendet sind,
  liest das Skript aus einem Daten-Attribut der Tabelle (z. B. `data-teile-ausgeblendet="true"`, gesetzt bei `!mitTeilen`
  und mindestens einer Struktur mit `AusgeblendeteTeile > 0`); dasselbe Attribut steuert den Zaehler-Zusatz oben.
- Zustand nur in der URL (Freigabe-Antwort 7; nach neuem Aufruf wieder ausgeblendet; Deep Link aus Teil 1 `?filterHauptFa=`
  zeigt den Standard).

**D-6 Aus A uebernommen** (entschieden, Freigabe-Antwort 9): Legende (`Index.cshtml:9-29`) als Bootstrap-Popover „Legende“ in der Toolbar plus
Hilfeseite (U-3; falls Popper nicht im Bundle: `<details>` bleibt, keine neue Bibliothek); Erklaertext neben dem Zaehler
(`:92`) auf die Hilfeseite; Hervorheben-Feld bleibt mit Funktion, sein Erklaertext (`:62-70`) wird ein Satz, der Hinweis auf
„Rekursive Filtersuche“ bleibt.

**D-7 Frontend.** Skill `frontend-design` ausdruecklich aufrufen; Bootstrap-Bausteine (`btn-sm`, `badge`, `alert`, Popover),
keine neue Bibliothek; Kontrast AA; Knopf mit Text und `aria-pressed`; Bedeutung nie nur ueber Farbe. ~~Breite: 15 sichtbare
Spalten~~ *(ueberholt durch Freigabe-Antwort 8)* Breite: **24 sichtbare Spalten** ohne Praeferenz (9 bisherige + 15 Kopfspalten)
→ `table-responsive` mit genau einem Scrollbalken (Regel v1.64.1), `structure`/`matchcode` bleiben Locked (= Pflichtspalten, im
Zahnrad nicht abwaehlbar, `column-preferences.js:509`, `:525`; `ColumnDefinitions.cs:644-645`).

**Struktur und Matchcode bleiben beim Querscrollen sichtbar** (Kritische Pruefung S-2). Vorhandenes Muster gesucht: `Locked`
heisst im Projekt nur „Pflichtspalte“ (s. o.), nicht „angeheftet“; `position: sticky` gibt es nur fuer den Ersatz-Scrollbalken
(`site.css:1029-1033`, `site.js:25-116`) und die Leitstand-Aktionsleiste (`site.css:1247-1250`, vertikal) — eine angeheftete
Spalte gibt es noch nicht. Deshalb das Minimum mit Plattform-Mitteln (Ponytail Sprosse 4, CSS vor JS):
- CSS in `site.css`, nur unter `#faTree`: `th`/`td` mit `data-col-key="structure"` → `position: sticky; left: 0`, mit
  `data-col-key="matchcode"` → `position: sticky; left: var(--fa-sticky-left-2, 0)`; `z-index` ueber den Nachbarzellen,
  Kopf-/Filterzeile ueber den Body-Zellen; deckender Hintergrund (Bootstrap-Tabellenzellen sind deckend, die Hervorheben-Markierung
  und die Zeilenfarben muessen an den angehefteten Zellen sichtbar bleiben — `TabellenZeilenfarbeTests` beachten); eine schmale
  rechte Trennlinie an der Matchcode-Spalte. `colspan`-Zeilen (Kopfdaten, Fehler) haben keinen `data-col-key` und bleiben unberuehrt.
- JS in `fa-hierarchy-tree.js` (wenige Zeilen): `ResizeObserver` auf `th[data-col-key="structure"]` setzt `--fa-sticky-left-2`
  auf dessen Breite (aendert sich beim Aufklappen tieferer Ebenen). Weil die Spalten per Zahnrad umsortierbar sind
  (`SupportsReorder: true`, `ColumnDefinitions.cs:640`), gilt sticky nur, solange `structure` und `matchcode` die ersten zwei
  sichtbaren Spalten sind; sonst nimmt das Skript die Klasse an der Tabelle weg (keine ueberlappenden Zellen).

**Was D an der Festlegung 2026-08-12 bricht und wie (entschieden, Freigabe-Antwort 5):**

| Bruch | Loesung (entschieden) |
|---|---|
| HauptFA-Zeile hat Spalten, die Baumzeilen nicht haben | Baum als eingerueckte Zeilen in **denselben** Spalten; Kopfzellen in Baumzeilen leer. Haelt „eine Tabelle, flache `<tr>`, Spalten fluchten“, den Drift-Waechter „je Knotenzeile eine Zelle je Spalte“ und das index-basierte Ausblenden von `column-preferences.js` (`:174-179`). |
| Kopfzeile mit `colspan` entfaellt | `colspan` bleibt nur fuer Kopfdaten-/Fehler-Zeile (D-3). Weil sie zwischen HauptFA-Zeile und Kindern stehen muss, rendert sie das Partial bei Level 0 (nach der eigenen `<tr>`, vor den Kindern); der Waechter `StrukturKopfzeilen_ColspanGleichSpaltenzahl` (`FaHierarchyStrukturSpaltenTests.cs:49-52`, liest heute nur `Index.cshtml`) prueft dann Index **und** Partial. |
| Struktur-Toggle und Knoten-Toggle verschmelzen | eine Klapp-Ebene weniger (K6) — gewollt. |

Verworfene Alternative: Baum als `colspan`-Teilbereich mit eigener Tabelle unter der HauptFA-Zeile — Zahnrad und
Spaltenfilter wirken dort nicht (`column-preferences.js:176` ueberspringt `colspan`-Zeilen), eine zweite Tabellen- und
Filterlogik waere neu zu bauen (Aufwand wie C).

- **Aufwand:** mittel — View-Umbau (Kopfzeile raus, Wurzelzeile erweitert), Partial mit 15 Kopfzellen, JS-Block loeschen +
  `aria-expanded`, eine Ausduenn-Methode, ein Parameter, ein Knopf, eine Hinweiszeile (Filterkarte) + Hervorheben-Hinweis,
  `ColumnDefinitions`, CSS-Trefferflaeche + sticky-Spalten, Tests (Drift-Waechter, Ausduennen, Keys/Labels).
- **Risiko:** mittel — (1) Doppelrolle der Wurzelzeile (Kopf + Knoten) und Waisen-Sonderfall; (2) breite Tabelle an
  Fertigungs-Terminals (24 Spalten, gemildert durch angeheftete Struktur-/Matchcode-Spalte, D-7, und das Zahnrad); (3) Spaltenfilter/Hervorheben finden ausgeblendete Teile nicht (gemeldet, D-5); (4) gespeicherte
  Benutzer-Praeferenzen: neue Spalten werden eingefuegt (`column-preferences.js:97-111`), keine Datenmigration noetig.
- **Wirkung:** je HauptFA eine Zeile mit allen Auftragsdaten (K2, K7 weg), Zeilenzahl im Baum sinkt um den Blatt-Anteil (K1, T-2).

## Migrations-/SQL-Auswirkungen

Keine Migration, kein Schema, kein `SQL/XX`, `00_FreshInstall.sql` und `SQL/AgentJobs/` unberuehrt.

Pruefabfragen (WMS-DB IDEAL-Test, nur lesen; Tabelle `FaHierarchyNodes`, `SQL/00_FreshInstall.sql:1948-1950`):

```sql
-- T-1: Blaetter mit Arbeitsschritten (bleiben laut Freigabe-Antwort 10 sichtbar) - liefert Testdaten fuer AK 8
SELECT COUNT(*) AS BlaetterMitAg
FROM dbo.FaHierarchyNodes
WHERE SubFA = 0 AND NULLIF(LTRIM(RTRIM(Arbeitsschritte)), N'') IS NOT NULL AND VerschwundenSeit IS NULL;

-- T-2: Zeilen je Struktur heute vs. Variante B (Wirkung)
SELECT HauptFA, COUNT(*) AS Knoten, SUM(CASE WHEN SubFA <> 0 THEN 1 ELSE 0 END) AS Baugruppen
FROM dbo.FaHierarchyNodes
WHERE VerschwundenSeit IS NULL
GROUP BY HauptFA ORDER BY Knoten DESC;

-- T-3 (Variante D, Freigabe-Antwort 6): HauptFA mit mehreren Auftragskoepfen; Abteilungen = 1 heisst
-- "mehrere Koepfe, gleiche Abteilung" - heute stilles FirstOrDefault in der Kopfzeile
SELECT HauptFA, COUNT(*) AS Koepfe, COUNT(DISTINCT ISNULL(MontageAbteilung, N'')) AS Abteilungen
FROM dbo.FaHierarchyOrderInfos
GROUP BY HauptFA HAVING COUNT(*) > 1
ORDER BY Koepfe DESC;
```

~~T-1 bis T-3 sind Entscheidungshilfen vor Schranke 1~~ *(ueberholt: Antworten 6 und 10 gelten unabhaengig vom Ergebnis)*
T-1 bis T-3 liefern die Testdaten fuer das Manual-UAT (Kombigeraet, Kaufteil mit Arbeitsschritten; Kritische Pruefung H-2) und
laufen vor dem UAT, kein Deploy-Schritt (Tabellenname `FaHierarchyOrderInfos` wie in Teil 1 W-3).

## Einstellungen

keine. „Teile anzeigen“/„Teile ausblenden“ (D-5) ist ein URL-Parameter der Seite (`mitTeilen`), keine Einstellung, nicht je
Benutzer gespeichert (Freigabe-Antwort 7). ~~(falls Rueckfrage 7 „je Benutzer speichern“: Benutzer-Profilfeld, dann Nachtrag mit
Migration — nicht in diesem Entwurf)~~ *(ueberholt)*. Das
Benutzer-Setting „Rekursive Filtersuche“ bleibt unveraendert in Gebrauch (Hervorheben-Feld bleibt).

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine — reine Darstellung, nichts wird geschrieben.

## Akzeptanzkriterien

Fuer **Variante D** (gewaehlt 2026-11-26). Die fruehere AK-Liste fuer B ist *ueberholt*; umgekehrt gegenueber ihr sind
AK 2 (Hervorheben bleibt, Freigabe-Antwort 2) und AK 4 (kein `DefaultHidden`, Freigabe-Antworten 3 und 8). AK 12-14 stammen
aus der UI-Pruefung und sind an D angepasst; AK 19 und die Ergaenzung in AK 10 stammen aus der Kritischen Pruefung (S-2, S-4).

1. Je Struktur genau **eine** HauptFA-Zeile (die Wurzel-`<tr>`, Level 0) mit HauptFA-Nummer in der Struktur-Zelle; es gibt keine Kopfzeile `fa-structure-group-head` mehr und bei genau einem Auftragskopf keine Unterkopf-Tabelle.
2. Feld „Knoten hervorheben“ bleibt mit unveraenderter Wirkung (markiert, blendet nichts aus; bei „Rekursive Filtersuche“ klappt es die Pfade auf); `#faClientFilter`-Code, `data-search` und das Lesen von `RecursiveFilterSearch` im Controller bleiben.
3. Legende ist auf der Seite als Popover erreichbar und steht mit der Filter-Erklaerung auf der Hilfeseite (Abschnitt FA-Struktur); die Seite verlinkt dorthin; der Erklaertext neben dem Zaehler ist entfernt; unter „Knoten hervorheben“ steht genau ein Erklaersatz.
4. Ohne gespeicherte Praeferenz sind **alle 24 Spalten sichtbar**: die bisherigen 9 (auch SubFA, Soll/Fert., Status, Arbeitsbereich) und alle 15 Kopfspalten aus D-2 (Kunde, PPS-Status, KO-Termin, Fert.-Termin, Montage-Abt., AB-Nr., Prio, Liefertermin, AB-Pos., Beschichter, RAL, Beschicht. retour, Neuer PT (PPS), Montage-Std., Bemerkung/Uhrzeit); jede ausser Struktur und Matchcode ist per Zahnrad abwaehlbar; vorhandene Benutzer-Praeferenzen bleiben wirksam, die neuen Spalten erscheinen dort zusaetzlich sichtbar. Eine Spalte „Material“ gibt es in der FA-Struktur nicht.
5. Bei genau einem Auftragskopf zeigt die HauptFA-Zeile in jeder Kopfspalte den Wert aus `FaHierarchyOrderInfo` (Datum dd.MM.yyyy); Baumzeilen und Waisen-Zeilen haben in den Kopfspalten leere Zellen; jede Knotenzeile hat genau eine Zelle je Spalte.
6. Ab zwei Auftragskoepfen: alle Kopfzellen leer, Badge „mehrdeutig (Kombigeraet)“ (bei `IsAmbiguous`) bzw. „n Auftragskoepfe“ (sonst), darunter die Kopfdaten-Zeile mit allen Koepfen (unabhaengig vom Chevron sichtbar). Kein Kopf: Kopfzellen leer, Badge „ohne Kopfdaten“.
7. Standard ist zugeklappt; der Chevron der HauptFA-Zeile klappt die direkten Kinder auf, erneut geklickt den ganzen Unterbaum zu; „Alle auf/zu“ wirkt auf alle Strukturen der Seite.
8. Standard (`mitTeilen` fehlt): im Baum nur Knoten mit `SubFA != 0` und Blaetter mit Arbeitsschritten; die Struktur-Zelle zeigt „n Knoten · m Teile ausgeblendet“.
9. Ein Knopf „Teile anzeigen“ in der Toolbar (Button mit Text und `aria-pressed`, kein Schalter) wirkt auf alle Strukturen der Seite, zeigt alle Knoten wie heute und heisst dann „Teile ausblenden“; der Zustand steht nur in der URL (`mitTeilen`), bleibt beim Blaettern und beim Filtern ueber die Filterkarte erhalten, „Zuruecksetzen“ setzt ihn zurueck, ein neuer Aufruf ohne Parameter zeigt wieder den Standard.
10. Filterkarte „Suche“ mit einer Teilenummer findet die Struktur auch bei ausgeblendeten Teilen und zeigt die Hinweiszeile (`role="status"`); bei aktivem Spaltenfilter und ausgeblendeten Teilen nennt der Zaehler „(Teile ausgeblendet)“; liefert „Knoten hervorheben“ bei ausgeblendeten Teilen keinen Treffer, erscheint unter dem Feld der Hinweis „Kein Treffer in den angezeigten Zeilen – Teile sind ausgeblendet, „Teile anzeigen“ blendet sie ein.“ (`role="status"`), der bei Treffer oder leerem Feld verschwindet; bei `mitTeilen=true` erscheint keiner dieser Hinweise.
11. Baum-Spaltenfilter (auch auf Kopfspalten, Text- und Auswahlfilter), Kontextpfad, AG-Symbole mit Status und das Modal „Arbeitsgaenge“ (Knopf jetzt in der HauptFA-Zeile) funktionieren; das Modal zeigt unabhaengig von `mitTeilen` alle Knoten.
12. Klapp-Knoepfe (alle `.fa-toggle`, inkl. HauptFA-Zeile) haben eine Trefferflaeche von mindestens 28 × 28 px und tragen `aria-expanded` passend zum Zustand.
13. Typ-Icons haben `title` und `aria-label`; die Legende ist auf der Seite als Popover erreichbar.
14. Bei ausgeblendeten Teilen zeigt die HauptFA-Zeile die Zahl der ausgeblendeten Teile.
15. Sprung aus der FA-Liste (Teil 1, `?filterHauptFa=`) zeigt die Struktur in der neuen Darstellung, Teile ausgeblendet.
16. Kontrast AA fuer Knopf-Beschriftung, Badges, Hinweiszeile und Zaehler (rechnerisch aus den CSS-Werten, Muster 2026-08-12).
17. Keys und Labels aller 15 Kopfspalten in `ColumnDefinitions.FaHierarchyStructure` stimmen mit `ColumnDefinitions.ProductionOrders` ueberein (Test; `DefaultHidden` wird bewusst nicht verglichen).
18. Build + alle Tests gruen; Drift-Waechter `FaHierarchyStrukturSpaltenTests` (inkl. colspan in Index + Partial) und `TabellenZeilenfarbeTests` angepasst; neue Tests: Ausduennen nach `Build` entfernt Blaetter ohne Arbeitsschritte, behaelt Blaetter mit Arbeitsschritten, zaehlt `AusgeblendeteTeile` richtig, und `FilterHauptFas` findet die Struktur weiter ueber eine Teilenummer.
19. Bei 1280 px Fensterbreite und Querscrollen der Tabelle bleiben Struktur- und Matchcode-Spalte (Kopf-, Filter- und Datenzeilen) links sichtbar, ohne die Nachbarspalten zu ueberdecken oder durchscheinen zu lassen; Hervorheben-Markierung und Zeilenfarbe sind an den angehefteten Zellen sichtbar; beim Aufklappen tieferer Ebenen rueckt die Matchcode-Spalte mit. Werden die beiden per Zahnrad von den ersten zwei Plaetzen weg sortiert, scrollen sie normal mit (keine ueberlappenden Zellen). Weiterhin genau ein waagrechter Scrollbalken (v1.64.1).

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (naechste freie Nummer nach Teil 1; Dev-Lauf prueft), Index nachziehen; ~~die
Szenarien zum Hervorheben-Feld aus dem Kapitel zu [[2026-08-12-fa-struktur-darstellung-spec]] (Nummer per Index ermitteln) als „abgeloest“ kennzeichnen.~~
Im Kapitel zu [[2026-08-12-fa-struktur-darstellung-spec]] (Nummer per Index ermitteln) die Schritte zur Struktur-Kopfzeile als „abgeloest durch“ das neue Kapitel kennzeichnen.

Das Szenario zum Hervorheben-Feld bleibt gueltig (Feld bleibt, Freigabe-Antwort 2) — ~~als „abgeloest“ kennzeichnen~~ (ueberholt);
abgeloest sind nur die Schritte zur Struktur-Kopfzeile und zur immer sichtbaren Kopfdaten-Tabelle.

- **Vorbedingung:** IDEAL-Testsystem, Master an, BDE an (sonst keine AG-Status, Fallstrick „FA-Baum-Status nur bei BDE an“), ein normaler HauptFA, ein Kombigeraet (T-3), eine Struktur mit Kaufteilen, moeglichst ein Kaufteil mit Arbeitsschritten (T-1) und eine verwaiste Struktur; ein Benutzer mit gespeicherter Zahnrad-Praeferenz fuer „FA-Struktur“.
- **Positiv:** eine Zeile je HauptFA mit Kopfwerten, gegen die FA-Liste derselben HauptFA vergleichen (dort die Neu-Spalten per Zahnrad einblenden) (AK 1, 5); Spalten-Default ohne Praeferenz (alle 24 sichtbar) und mit alter Praeferenz (neue Spalten kommen sichtbar dazu) (AK 4); bei 1280 px nach rechts scrollen, Struktur/Matchcode bleiben stehen, Baum aufklappen, danach die beiden im Zahnrad nach hinten sortieren (AK 19); Chevron auf/zu, „Alle auf/zu“ (AK 7, 12); Teile-Knopf an/aus, blaettern, filtern, zuruecksetzen (AK 8, 9, 14); Modal „Arbeitsgaenge“ (AK 11); Sprung aus der FA-Liste (AK 15); Hervorheben mit und ohne „Rekursive Filtersuche“ (AK 2); Legende-Popover (AK 3, 13).
- **Suche/Filter:** Filterkarte mit Teilenummer bei ausgeblendeten Teilen → Struktur + Hinweiszeile (AK 10); Spaltenfilter Kunde → nur passende HauptFA-Zeilen, zugeklappt; Spaltenfilter Matchcode einer Baugruppe → Pfad als Kontext (AK 11); Spaltenfilter auf eine Teilenummer bei ausgeblendeten Teilen → kein Treffer, Zaehler nennt „(Teile ausgeblendet)“ (AK 10); „Knoten hervorheben“ mit derselben Teilenummer → Hinweis unter dem Feld, nach „Teile anzeigen“ wird das Teil markiert und kein Hinweis erscheint (AK 10).
- **Kombigeraet/Sonderfaelle:** mehrere Koepfe → leere Kopfzellen, Badge, Kopfdaten-Zeile (AK 6); Struktur ohne Kopfdaten → Badge „ohne Kopfdaten“; verwaiste Struktur → Badge „Verwaist“, leere Kopfzellen (AK 5).
- **Negativ:** Struktur ohne Baugruppen (nur Wurzel + Teile) zeigt im Standard nur die HauptFA-Zeile ohne Chevron, mit Zaehler der ausgeblendeten Teile; Kaufteil mit Arbeitsschritten bleibt sichtbar (AK 8); Benutzer ohne Recht → kein Zugriff wie bisher; AKE → Seite nicht erreichbar wie bisher.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | (kein Epic) | – | – |

## Deploy

*Vorlaeufig — der qa-agent bestaetigt gegen den echten Diff.*

- **Web-App:** ja (Views, Partial, JS, CSS, Controller, TreeBuilder-Methode, ColumnDefinitions, Hilfe)
- **Service:** nein
- **Migration:** nein (kein Schema, kein `SQL/XX`, keine Daten- oder Einmal-Skripte; T-1..T-3 sind Testdaten-Abfragen vor dem UAT, kein Deploy-Schritt)
- **Manifest-Eintraege** (`scripts/deploy/nachlauf.psd1`):
  - Id `fa-struktur-uebersicht-info`, Version = Version des Laufs, Typ `Info`, Firma `IDEAL`, Phase `Nachher`, Ausfuehrung `anzeigen`,
    Text „FA-Struktur: je HauptFA eine Zeile mit allen Auftragsdaten als Spalten – alle sichtbar, ueber das Zahnrad ausblendbar; Struktur und Matchcode bleiben beim Querscrollen stehen. Baugruppen per Pfeil aufklappen, Kaufteile/Material ueber den Knopf „Teile anzeigen“ (Teile mit Arbeitsschritten bleiben immer sichtbar). Gespeicherte Spalteneinstellungen bleiben, neue Spalten kommen sichtbar dazu.“
  - ~~Text „FA-Struktur: je HauptFA eine Zeile mit den Auftragsdaten als Spalten (Zahnrad); Baugruppen per Pfeil aufklappen, Kaufteile/Material ueber den Knopf „Teile anzeigen“. Gespeicherte Spalteneinstellungen bleiben, neue Spalten kommen dazu.“~~ (ueberholt: Kritische Pruefung H-5, Freigabe-Antwort 8)
  - ~~Text „FA-Struktur zeigt im Standard nur Baugruppen; Kaufteile ueber „Teile anzeigen“. Feld „Hervorheben“ entfallen – Spaltenfilter nutzen.“~~ (ueberholt: Feld bleibt)
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` (im Worktree)

## UI-Pruefung (Skill ui-ux-pro-max, 2026-11-26)

Geprueft gegen die Prioritaeten Barrierefreiheit → Bedienung/Touch → Layout. Die Skill-Datenbank lieferte fuer Baum-Tabellen
keine spezifischen Treffer; die Punkte stuetzen sich auf ihre WCAG-2.2-Regeln (Target Size 24 px, Kontrast) und den Code.
Ergebnis: **Variante B bleibt die Empfehlung.** C verliert zusaetzlich bei Terminal-Breite und Fokusfuehrung. Vier Befunde,
alle klein und in B mitzunehmen (U-1 bis U-4 gelten auch fuer A):

| # | Befund | Beleg | Massnahme |
|---|---|---|---|
| U-1 | Klapp-Knopf je Knoten ist **16 px breit, Chevron 12 px** — unter WCAG 2.2 AA 2.5.8 (24 × 24 px), an Fertigungs-Terminals mit Handschuh/Touch zu klein. | `site.css:1375-1384`, `_FaHierarchyNode.cshtml:77-78` | `.fa-toggle` und `.fa-toggle-placeholder` auf min. 28 × 28 px Trefferflaeche (Icon bleibt klein, Einrueckung wandert mit). |
| U-2 | Klapp-Knoepfe melden ihren Zustand nicht: `role="button"` ohne `aria-expanded`. | `_FaHierarchyNode.cshtml:77`; kein `aria-expanded` in `fa-hierarchy-tree.js` | `aria-expanded="true/false"` an Knoten- und Struktur-Toggle setzen, im JS beim Klappen mitschreiben. |
| U-3 | Legende wandert auf die Hilfeseite → Typ-Icons muessen ohne Legende verstaendlich bleiben. | `_FaHierarchyNode.cshtml:29, :87` (`aria-label` vorhanden) | Zusaetzlich `title` am Icon (Maus-Tooltip); auf der Seite neben „Hilfe“ ein Bootstrap-Popover „Legende“ statt eines Seitenwechsels. Keine neue Bibliothek. |
| U-4 | „Melden statt still“: mit B verschwinden Teile ohne Spur, wenn keine Suche gesetzt ist. | Variante B, Hinweiszeile nur bei `filterText` | Knotenzahl-Badge zeigt **„9 Baugruppen · 78 Teile ausgeblendet“**; Hinweiszeile mit `role="status"`. |

Ohne Befund: Fokusrahmen sichtbar (`site.css:1394-1398`), Icons nach Form und Farbe unterscheidbar (`_FaHierarchyNode.cshtml:29`),
Schalter „Teile anzeigen“ als `form-switch` mit sichtbarem Text, Zustand in der URL (Deep Link aus Teil 1 bleibt stabil).

Zusaetzliche Akzeptanzkriterien:

12. Klapp-Knoepfe (Knoten und Struktur) haben eine Trefferflaeche von mindestens 28 × 28 px und tragen `aria-expanded` passend zum Zustand.
13. Typ-Icons haben `title` und `aria-label`; die Legende ist auf der Seite als Popover erreichbar.
14. Bei ausgeblendeten Teilen zeigt die Kopfzeile die Zahl der ausgeblendeten Teile.

> [!note] Nachtrag 2026-11-26 — Anpassung an Variante D
> Die Empfehlung „Variante B“ dieser Pruefung ist durch Geralds Entscheidung fuer D *ueberholt*; die Befunde gelten weiter:
> U-1/U-2 fuer alle `.fa-toggle` (der Struktur-Toggle entfaellt, die HauptFA-Zeile nutzt den Wurzel-Toggle, D-4);
> U-3 unveraendert (Popover, D-6); U-4 als Zaehler in der Struktur-Zelle der HauptFA-Zeile, Text „n Knoten · m Teile
> ausgeblendet“, weil auch Blaetter mit Arbeitsschritten sichtbar bleiben (D-5). „Schalter als `form-switch`“ unter
> „Ohne Befund“ ist ueberholt: Knopf mit Text und `aria-pressed` (Freigabe-Antwort 4). AK 12-14 stehen an D angepasst
> in den Akzeptanzkriterien oben („Kopfzeile“ → HauptFA-Zeile).

## Offene Rueckfragen

1. *(beantwortet, Freigabe-Antwort 1: Variante D)* Welche Variante: **A** (Entruempeln), **B** (A + Baugruppen-Baum, Teile per Schalter) oder **C** (Master-Detail)? Empfehlung **B** (bitte zusaetzlich `freigabe_entscheidung` setzen).
2. *(beantwortet, Freigabe-Antwort 2: Feld bleibt)* Feld „Knoten hervorheben“ ersatzlos streichen (Empfehlung: Spaltenfilter und Strg+F decken es ab, der tote Auto-Expand-Zweig faellt mit)? Das hebt die bewusste Unterscheidung „hervorheben vs. ausblenden“ aus [[2026-08-12-fa-struktur-darstellung-spec]] auf.
3. *(beantwortet, Freigabe-Antwort 3: kein neues DefaultHidden)* Per Default ausblenden: SubFA und Soll/Fert. (Empfehlung) — sollen weitere Spalten (Status „beschichtet“, Arbeitsbereich) ebenfalls ins Zahnrad?
4. *(beantwortet, Freigabe-Antwort 4: Knopf statt Schalter; erster Teil → Rueckfrage 10, URL/Benutzer → Rueckfrage 7)* Nur bei B: Blaetter mit Arbeitsschritten trotzdem zeigen (Empfehlung, falls Pruefabfrage T-1 > 0) oder strikt alle Blaetter ausblenden? Schalter „Teile anzeigen“ nur in der URL (Empfehlung) oder je Benutzer speichern (dann Migration, Nachtrag)?
5. *(beantwortet, Freigabe-Antwort 5: dieselben Spalten)* **Baum-Darstellung unter der HauptFA-Zeile:** eingerueckte Zeilen in **denselben** Spalten, Kopfzellen in Baumzeilen leer (Empfehlung — haelt „eine Tabelle, flache `<tr>`, Spalten fluchten“ aus [[2026-08-12-fa-struktur-darstellung-spec]], Zahnrad und Spaltenfilter wirken weiter) oder der Baum als eigener `colspan`-Teilbereich mit eigener Tabelle (Zahnrad/Spaltenfilter wirken dort nicht, Aufwand wie Variante C)?
6. *(beantwortet, Freigabe-Antwort 6: Badge + Kopfdaten-Zeile ab zwei Koepfen)* **Mehrere Auftragskoepfe (Kombigeraet):** Kopfspalten leer + Badge + die heutige Kopfdaten-Tabelle als Zeile direkt unter der HauptFA-Zeile (Empfehlung), und zwar schon ab **zwei Koepfen** (Regel der FA-Liste) statt erst bei zwei verschiedenen Montage-Abteilungen (heutige FA-Struktur-Regel, die bei gleicher Abteilung stumm den ersten Kopf zeigt)? Alternative: Werte je Kopf untereinander in den Zellen („MO: 14.07. / EL: 21.07.“). Pruefabfrage T-3 zeigt, wie oft das vorkommt.
7. *(beantwortet, Freigabe-Antwort 7: seitenweit, URL)* **Knopf „Teile anzeigen“:** seitenweit oben in der Toolbar, wirkt auf alle HauptFA der Seite (Empfehlung: serverseitig, weniger Code, kleinere Seite) oder je HauptFA-Zeile ein eigener Knopf (dann muessen alle Teile immer mitgeladen und per Skript ein-/ausgeblendet werden)? Zustand nur in der Adresszeile (Annahme; nach neuem Aufruf wieder ausgeblendet) oder je Benutzer merken (dann Migration, Nachtrag)?
8. *(beantwortet, Freigabe-Antwort 8: alle Kopfspalten sichtbar, „Fert.-Termin“)* **Kopfspalten:** sichtbar die sechs, die heute oben im Kopf stehen (Kunde, PPS-Status, KO-Termin, Fert.-Termin, Montage-Abt., AB-Nr.); Prio, Liefertermin und die neuen Felder aus Teil 1 (soweit dort bestaetigt) ueber das Zahnrad, standardmaessig ausgeblendet — einverstanden? (Antwort 3 betraf nur die bisherigen Spalten; bei den neuen ist „im Zahnrad“ gerade der Sinn.) Label „Fert.-Termin“ wie in der FA-Liste statt „FE-Termin“?
9. *(beantwortet, Freigabe-Antwort 9: einverstanden)* **Aus Variante A uebernommen:** Legende als Aufklapp-Hinweis („Legende“-Knopf) plus Hilfeseite, langer Erklaertext neben dem Zaehler auf die Hilfeseite, Erklaerung unter „Knoten hervorheben“ auf einen Satz gekuerzt — einverstanden?
10. *(beantwortet, Freigabe-Antwort 10: sichtbar lassen)* **Annahme, bitte bestaetigen:** Manche Kaufteile/Materialien haben in Sage eigene Arbeitsschritte (z. B. „KA“) und zeigen deshalb Arbeitsgang-Symbole. Diese Teile bleiben auch bei „Teile ausblenden“ sichtbar, damit ihre Arbeitsgaenge nicht verschwinden (Pruefabfrage T-1 zeigt, wie viele es sind). Oder sollen bei „Teile ausblenden“ wirklich **alle** Teile weg sein?

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

*Antworten 1-10 im Chat gegeben und vom Orchestrator im Auftrag eingetragen (Ausnahme vom Unveraendert-Gebot).*

1. → Neue Variante D statt A/B/C: je HauptFA genau eine Zeile wie in der FA-Liste, alle FA-Details nebeneinander als eigene Spalten (Zahnrad/column-preferences wie FA-Liste); die heutige Kopfzeile + Unterkopf-Tabelle (Dopplung K2) entfallen und gehen in diese Zeile auf; ueber Chevron klappt darunter der Baum (Baugruppen/Teile) auf. Kombigeraet sauber loesen. *(Gerald 2026-11-26 Chat)*
2. → Nein — Feld „Knoten hervorheben“ bleibt. *(Gerald 2026-11-26 Chat)*
3. → Keine neuen DefaultHidden — SubFA, Soll/Fert., Status beschichtet, Arbeitsbereich bleiben standardmaessig eingeblendet („eingeblendet“ war gemeint). *(Gerald 2026-11-26 Chat)*
4. → Im aufgeklappten Baum standardmaessig nur Baugruppen (SubFA != 0); Kaufteile/Material per Knopf (Button, kein Switch) „Teile anzeigen“/„Teile ausblenden“ einblenden. Frage zu Blaettern mit Arbeitsschritten nicht verstanden → Default der Empfehlung (sichtbar lassen) als Annahme, siehe Rueckfrage 10; Zaehler „n Teile ausgeblendet“ bleibt. *(Gerald 2026-11-26 Chat)*
5. → Eingerueckt in denselben Spalten (Empfehlung), Kopf-Spalten in Baumzeilen leer. *(Gerald 2026-11-26 Chat)*
6. → Badge „Kombigeraet“ + Kopf-Tabelle als Zeile darunter, ab zwei Koepfen (Regel der FA-Liste) (Empfehlung). *(Gerald 2026-11-26 Chat)*
7. → Ein Knopf fuer die ganze Seite in der Toolbar, Zustand in der URL (Empfehlung). *(Gerald 2026-11-26 Chat)*
8. → Alle Kopf-Spalten standardmaessig sichtbar (abweichend von der Empfehlung „6 sichtbar“); Labels wie FA-Liste („Fert.-Termin“). *(Gerald 2026-11-26 Chat)*
9. → Legende als Popover + Hilfeseite, Langtext neben Zaehler auf die Hilfeseite, unter „Knoten hervorheben“ ein Satz (Empfehlung). *(Gerald 2026-11-26 Chat)*
10. → Trotzdem anzeigen: Teile mit Arbeitsschritten bleiben auch bei „Teile ausblenden“ sichtbar (Empfehlung, Annahme bestaetigt). *(Gerald 2026-11-26 Chat)*

## Kritische Pruefung (2026-11-26)

Geprueft: Spec inkl. Freigabe-Antworten 1-10, Backlog-Notiz, Uebersicht, Teil 1 (Status), Stichproben im Buendel-Worktree.

### BLOCKER

- **B-1 Abhaengigkeit Teil 1 unbeantwortet.** `depends_on` Teil 1; die Kopfspalte `pps-status` haengt an Teil-1-Rueckfrage 4
  (eigene Spalte oder „Sage-Status“), die Neu-Felder (`ab-position`, `dienstleister`, `ral` …) an Teil-1-Rueckfrage 2 — die
  Freigabe-Antworten von [[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-1-spec]] sind alle leer (Zeilen 311-315).
  Mit Antwort 8 („alle Kopfspalten sichtbar“) ist offen, welche Spalten das ueberhaupt sind.
  **Frage an Gerald:** Teil 1 zuerst beantworten und gemeinsam freigeben (Empfehlung) — oder Teil 2 entkoppeln: Kopfspalten
  nur mit den heute vorhandenen FA-Listen-Keys (Kunde, KO-Termin, Fert.-Termin, Montage-Abt., AB-Nr., Prio, Liefertermin,
  Status aus `OrderInfos.Status` unter neuem Key), Neu-Felder per Nachtrag nach Teil 1?

### SOLLTE

- **S-1 Antwort 8 nicht im Rumpf.** D-2-Tabelle (`prio`, `delivery-date`, Neu-Felder „ausgeblendet“), AK 4 („die weiteren
  Kopfspalten ausgeblendet“) und D-7 („15 sichtbare Spalten“) widersprechen Antwort 8 (alle Kopfspalten sichtbar). Der Dev-Lauf
  arbeitet nach dem Rumpf → vor dem Lauf nachziehen: alle Kopfspalten `DefaultHidden = false`, AK 4 neu: „Ohne gespeicherte
  Praeferenz sind alle Spalten sichtbar (9 bisherige + alle Kopfspalten)“.
- **S-2 Tabellenbreite.** Mit Antwort 8: 9 bisherige + 8 Kopfspalten + bis zu 7 Neu-Felder ≈ 24 sichtbare Spalten. An
  Fertigungs-Terminals ist das ein langer Querscroll (eine Leiste nach v1.64.1, aber trotzdem). Vorschlag: AK ergaenzen
  „bei 1280 px Breite bleiben Struktur und Matchcode beim Querscrollen sichtbar (Locked/sticky)“ — oder Antwort 8 bewusst
  bestaetigen lassen. Kein Blocker, weil die Entscheidung klar ist; das Zahnrad nimmt Spalten zurueck.
- **S-3 „vorbehaltlich Rueckfrage“ entfernen.** AK 3 (Rueckfrage 9), AK 6 (Rueckfrage 6), AK 8 (Annahme 10), AK 9 (Rueckfrage 7)
  und D-5 („Annahme“) sind beantwortet — Vorbehalte streichen, damit die AK eindeutig pruefbar sind. Badge-Text vereinheitlichen:
  Antwort 6 sagt „Kombigeraet“, D-3/AK 6 „mehrdeutig (Kombigeraet)“ bzw. „n Auftragskoepfe“ — Rumpf-Text ist feiner und deckt
  Antwort 6 ab; so festschreiben.
- **S-4 Hervorheben vs. ausgeblendete Teile.** Antwort 2 behaelt das Hervorheben-Feld; es sieht wie der Spaltenfilter nur
  gerenderte Zeilen. Wer dort eine Teilenummer eingibt, findet bei ausgeblendeten Teilen nichts und bekommt keinen Hinweis
  (D-5 meldet nur Filterkarte und Spaltenfilter). Vorschlag AK 10 ergaenzen: „Hervorheben ohne Treffer bei ausgeblendeten
  Teilen zeigt denselben Hinweis wie der Spaltenfilter.“
- **S-5 `freigabe_entscheidung` setzen** (Varianten-Spec): bei der Freigabe `D` eintragen.

### HINWEIS

- **H-1 Pagination belegt:** `site.js:126-135` baut die URL aus `window.location.href` und setzt nur `page` — `mitTeilen`
  bleibt beim Blaettern erhalten. Der Vermerk „der Dev-Lauf belegt“ (D-5) ist damit erledigt.
- **H-2 Pruefabfragen T-1..T-3** sind nicht gelaufen. Sie aendern keine Entscheidung mehr (Antworten 6 und 10 gelten
  unabhaengig vom Ergebnis), liefern aber die Testdaten fuer die Szenarien (Kombigeraet, Kaufteil mit AG) — vor dem Manual-UAT laufen lassen.
- **H-3 Groesse:** ~10 Dateien (2 Views, JS, CSS, Controller, TreeBuilder, ViewModel, ColumnDefinitions, 2-3 Tests, Hilfe) —
  in einem Dev-Lauf machbar, kein Split noetig.
- **H-4 Abschnitt „UI-Pruefung“** sagt noch „Variante B bleibt die Empfehlung“; der Nachtrag darunter stellt das richtig — so lassen (Historie).
- **H-5 Deploy plausibel:** Web only, keine Migration, Manifest-Info ok; Text um „alle Spalten sichtbar, Zahnrad zum Ausblenden“ ergaenzen (S-1).

**NACHBESSERUNG NOETIG: Teil 1 unbeantwortet (B-1) und Antwort 8 nicht in Rumpf/AK (S-1).**

### Nachpruefung (2026-11-26, nach Rework 5690e85c)

B-1 erledigt (Teil 1 beantwortet, Kopfspalten in D-2 = 7 FA-Listen-Keys + 8 Teil-1-Kopffelder, Material bewusst keine
Kopfspalte). S-1 erledigt (AK 4: alle 24 sichtbar), S-2 erledigt (AK 19 sticky Struktur/Matchcode, kein Projektmuster
vorhanden → Minimum CSS sticky + ResizeObserver), S-3 erledigt (keine Vorbehalte mehr in AK/D-5), S-4 erledigt (AK 10),
H-1 erledigt (Beleg `site.js:126-135`). Offen nur S-5: `freigabe_entscheidung: D` setzt der Mensch.
Reihenfolge: Teil 1 vor Teil 2 (gemeinsamer Lauf).

**BEREIT ZUR FREIGABE** (zusammen mit Teil 1).
