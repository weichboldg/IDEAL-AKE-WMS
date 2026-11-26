---
type: spec
title: "IDEAL FA-Struktur uebersichtlicher (Entscheidungsvorlage: Varianten A/B/C, Baum bleibt)"
slug: 2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-2-spec
status: Entwurf
created: 2026-11-26
updated: 2026-11-26
source_backlog: "[[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu]]"
uebersicht: "[[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-uebersicht]]"
depends_on: "[[2026-11-26-fa-liste-nur-hauptfa-und-fa-struktur-neu-teil-1-spec]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: IDEAL
affected_code:
  - "IdealAkeWms/Views/FaHierarchy/Index.cshtml (Unterkopf-Tabelle nur bei Kombigeraet, Hervorheben-Feld + Erklaertexte raus, Toolbar-Schalter „Teile anzeigen“ [B], #column-config defaultHidden)"
  - "IdealAkeWms/Views/FaHierarchy/_FaHierarchyNode.cshtml (data-search entfaellt mit dem Hervorheben-Feld)"
  - "IdealAkeWms/Controllers/FaHierarchyController.cs (Index: Parameter mitTeilen [B], Blaetter vor dem Baumaufbau weglassen; RecursiveFilterSearch-Lesen entfaellt mit dem Hervorheben-Feld)"
  - "IdealAkeWms/wwwroot/js/fa-hierarchy-tree.js (#faClientFilter-Block raus, subheadRow null-sicher ist schon so)"
  - "IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (FaHierarchyStructure: SubFA, Soll/Fert. DefaultHidden)"
  - "IdealAkeWms/Models/ViewModels/FaHierarchyTreeViewModel.cs (MitTeilen, RecursiveFilterSearch entfaellt)"
  - "Views/Help/Index.cshtml (Abschnitt FA-Struktur: Legende/Filter-Erklaerung hierher), docs/TESTSZENARIEN.md, docs/anwender"
open_questions:
  - "1. Welche Variante: A (Entruempeln), B (A + Baugruppen-Baum, Teile per Schalter) oder C (Master-Detail)? Empfehlung B."
  - "2. Feld „Knoten hervorheben“ ersatzlos streichen (Empfehlung; Spaltenfilter und Strg+F decken es ab, der tote Auto-Expand-Zweig faellt mit)? Hebt die Unterscheidung aus [[2026-08-12-fa-struktur-darstellung-spec]] (In-Scope 3) auf."
  - "3. Spalten per Default ausblenden: SubFA und Soll/Fert. (Empfehlung) — weitere (Status „beschichtet“, Arbeitsbereich)?"
  - "4. Nur Variante B: Blaetter mit Arbeitsschritten trotzdem zeigen (Empfehlung, falls Pruefabfrage T-1 Treffer > 0) oder strikt alle Blaetter ausblenden? Schalter „Teile anzeigen“ nur in der URL (Empfehlung) oder je Benutzer speichern?"
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

Gerald: „FA-Struktur … ist derzeit viel zu komplex … Baumstruktur soll prinzipiell erhalten bleiben. Bitte mit
Vorschlag.“ Diese Spec ist eine **Entscheidungsvorlage**: sie benennt belegt, was die Komplexitaet ausmacht, legt
drei Varianten mit Skizze, Aufwand und Risiko vor und empfiehlt eine. Mit Teil 1 wird die FA-Struktur der einzige
Ort fuer die Sub-FA-Sicht — umso wichtiger, dass sie lesbar ist.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** Seite `/FaHierarchy` (Index-View, Knoten-Partial, Baum-Skript, Spalten-Defaults), nur Darstellung.

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

### Bestehende Festlegungen und wie die Varianten damit umgehen

| Festlegung | Quelle | A | B | C |
|---|---|---|---|---|
| Eine seitenweite Tabelle, ein `<tbody>` je Struktur, flache `<tr>` mit `data-parent-id`, Spalten fluchten, `column-preferences.js` | [[2026-08-12-fa-struktur-darstellung-spec]] In-Scope 2/6 | bleibt | bleibt | **aufgehoben** |
| Spaltenfilter mit Baum-Semantik (Kontextpfad, Zweige ausblenden) | ebd. In-Scope 3 | bleibt | bleibt | neu zu bauen |
| Hervorheben-Feld als eigener, unterscheidbarer Weg | ebd. In-Scope 3 | **aufgehoben** (Rueckfrage 2) | **aufgehoben** (Rueckfrage 2) | entfaellt |
| Typ-Icons, Legende, Kontrast AA | ebd. In-Scope 1/4 | bleibt (Legende → Hilfe) | bleibt | bleibt |
| Modal „Arbeitsgaenge“ je HauptFA, Laden beim Oeffnen | [[2026-09-23-fa-struktur-arbeitsgaenge-anzeige-spec]] | bleibt | bleibt | Inhalt wandert ins Detailfeld |
| AG-Symbole in Baumzeilen, Status nur ueber `ArbeitsgangStatusRegel` | [[2026-10-06-fa-struktur-arbeitsgaenge-symbole-uebersicht]] Teil 1-3, Fallstrick „Arbeitsgang-Status nur ueber ArbeitsgangStatusRegel“ | bleibt | bleibt | bleibt |

## Technischer Loesungsentwurf

### Variante A — Entruempeln (nur Weglassen)

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

### Variante B — Baugruppen-Baum (A + Teile per Schalter) — **Empfehlung**

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

### Variante C — Master-Detail (Neubau, nicht empfohlen)

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

### Empfehlung

**B.** Der groesste Treiber ist K1 (jede Stuecklistenposition eine Zeile); B nimmt ihn durch Weglassen per Default heraus,
ohne eine Festlegung ausser dem Hervorheben-Feld anzutasten, und ist per Schalter jederzeit umkehrbar. A allein laesst
K1 stehen; C baut neu, was 2026-08-12 bewusst vereinheitlicht hat (Ponytail-Leiter: Sprosse 1 „weglassen“ vor Sprosse 7).

Frontend (alle Varianten): Skill `frontend-design` ausdruecklich aufrufen; Bootstrap-Bausteine (`form-switch`, `btn-sm`,
`alert`), keine neue Bibliothek; Kontrast AA, Schalter mit sichtbarem Text, Bedeutung nie nur ueber Farbe.

## Migrations-/SQL-Auswirkungen

Keine Migration, kein Schema, kein `SQL/XX`, `00_FreshInstall.sql` und `SQL/AgentJobs/` unberuehrt.

Pruefabfragen vor der Entscheidung (WMS-DB IDEAL-Test, nur lesen; Tabelle `FaHierarchyNodes`, `SQL/00_FreshInstall.sql:1948-1950`):

```sql
-- T-1: Blaetter mit Arbeitsschritten (Rueckfrage 4) - > 0 heisst: strikt ausblenden versteckt AG-Symbole
SELECT COUNT(*) AS BlaetterMitAg
FROM dbo.FaHierarchyNodes
WHERE SubFA = 0 AND NULLIF(LTRIM(RTRIM(Arbeitsschritte)), N'') IS NOT NULL AND VerschwundenSeit IS NULL;

-- T-2: Zeilen je Struktur heute vs. Variante B (Wirkung)
SELECT HauptFA, COUNT(*) AS Knoten, SUM(CASE WHEN SubFA <> 0 THEN 1 ELSE 0 END) AS Baugruppen
FROM dbo.FaHierarchyNodes
WHERE VerschwundenSeit IS NULL
GROUP BY HauptFA ORDER BY Knoten DESC;
```

## Einstellungen

keine. „Teile anzeigen“ (B) ist ein URL-Parameter der Seite, keine Einstellung (falls Rueckfrage 4 „je Benutzer speichern“:
Benutzer-Profilfeld, dann Nachtrag mit Migration — nicht in diesem Entwurf).

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine — reine Darstellung, nichts wird geschrieben.

## Akzeptanzkriterien

Fuer die empfohlene Variante B (A = AK 1-5 und 9-11; C braucht eine eigene Spec):

1. Bei eindeutigem Auftragskopf gibt es je Struktur genau eine Kopfzeile; die Unterkopf-Tabelle erscheint nur bei Kombigeraet und zeigt dann alle Koepfe.
2. Feld „Knoten hervorheben“, seine Erklaerung und der Erklaertext neben dem Zaehler sind entfernt; `fa-hierarchy-tree.js` enthaelt keinen `#faClientFilter`-Code mehr; der Controller liest `RecursiveFilterSearch` nicht mehr.
3. Legende und Filter-Erklaerung stehen auf der Hilfeseite (Abschnitt FA-Struktur); die Seite verlinkt dorthin.
4. Ohne gespeicherte Praeferenz sind SubFA und Soll/Fert. ausgeblendet und per Zahnrad einblendbar; vorhandene Benutzer-Praeferenzen bleiben wirksam.
5. Spaltenfilter (Baum-Semantik), Struktur-/Knoten-Klappen, „Alle auf/zu“, AG-Symbole mit Status und das Modal „Arbeitsgaenge“ funktionieren unveraendert.
6. Standard (`mitTeilen` fehlt): nur Knoten mit `SubFA != 0` (bzw. nach Rueckfrage 4 zusaetzlich Blaetter mit Arbeitsschritten); Knotenzahl-Badge „n Baugruppen“.
7. Schalter „Teile anzeigen“ an: alle Knoten wie heute; Zustand bleibt beim Blaettern und Filtern erhalten, „Zuruecksetzen“ setzt ihn zurueck.
8. Filterkarte „Suche“ mit einer Teilenummer findet die Struktur auch bei ausgeblendeten Teilen und zeigt die Hinweiszeile „Treffer in ausgeblendeten Teilen“.
9. Sprung aus der FA-Liste (Teil 1, `?filterHauptFa=`) zeigt die Struktur in der neuen Darstellung.
10. Kontrast AA fuer Schalter-Beschriftung, Hinweiszeile und Kopfzeile (rechnerisch aus den CSS-Werten, Muster 2026-08-12).
11. Build + alle Tests gruen; neuer Test: Blatt-Filter vor `Build` laesst `FilterHauptFas` unberuehrt (Teilenummer findet Struktur).

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (naechste freie Nummer nach Teil 1; Dev-Lauf prueft), Index nachziehen; die
Szenarien zum Hervorheben-Feld aus dem Kapitel zu [[2026-08-12-fa-struktur-darstellung-spec]] (Nummer per Index ermitteln) als „abgeloest“ kennzeichnen.

- **Vorbedingung:** IDEAL-Testsystem, Master an, BDE an (sonst keine AG-Status, Fallstrick „FA-Baum-Status nur bei BDE an“), ein normaler HauptFA, ein Kombigeraet, eine Struktur mit Kaufteilen.
- **Positiv:** AK 1, 4, 5, 6, 7, 9.
- **Suche:** AK 8 (Teilenummer), Spaltenfilter auf Matchcode einer Baugruppe.
- **Negativ:** Kombigeraet zeigt Unterkopf (AK 1); Struktur ohne Baugruppen (nur Wurzel + Teile) zeigt im Standard nur die Wurzel ohne Chevron; Benutzer ohne Recht → kein Zugriff wie bisher.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | (kein Epic) | – | – |

## Deploy

*Vorlaeufig — der qa-agent bestaetigt gegen den echten Diff.*

- **Web-App:** ja (Views, JS, Controller, ColumnDefinitions, Hilfe)
- **Service:** nein
- **Migration:** nein
- **Manifest-Eintraege** (`scripts/deploy/nachlauf.psd1`):
  - Id `fa-struktur-uebersicht-info`, Version = Version des Laufs, Typ `Info`, Firma `IDEAL`, Phase `Nachher`, Ausfuehrung `anzeigen`,
    Text „FA-Struktur zeigt im Standard nur Baugruppen; Kaufteile ueber „Teile anzeigen“. Feld „Hervorheben“ entfallen – Spaltenfilter nutzen.“ (Text je nach gewaehlter Variante)
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` (im Worktree)

## Offene Rueckfragen

1. Welche Variante: **A** (Entruempeln), **B** (A + Baugruppen-Baum, Teile per Schalter) oder **C** (Master-Detail)? Empfehlung **B** (bitte zusaetzlich `freigabe_entscheidung` setzen).
2. Feld „Knoten hervorheben“ ersatzlos streichen (Empfehlung: Spaltenfilter und Strg+F decken es ab, der tote Auto-Expand-Zweig faellt mit)? Das hebt die bewusste Unterscheidung „hervorheben vs. ausblenden“ aus [[2026-08-12-fa-struktur-darstellung-spec]] auf.
3. Per Default ausblenden: SubFA und Soll/Fert. (Empfehlung) — sollen weitere Spalten (Status „beschichtet“, Arbeitsbereich) ebenfalls ins Zahnrad?
4. Nur bei B: Blaetter mit Arbeitsschritten trotzdem zeigen (Empfehlung, falls Pruefabfrage T-1 > 0) oder strikt alle Blaetter ausblenden? Schalter „Teile anzeigen“ nur in der URL (Empfehlung) oder je Benutzer speichern (dann Migration, Nachtrag)?

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. →
2. →
3. →
4. →
