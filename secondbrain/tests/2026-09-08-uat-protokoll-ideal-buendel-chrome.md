---
typ: testprotokoll
status: bereit
erstellt: 2026-09-08
scope: "IDEAL-Buendel Schranke 2 (Teile 1-8, FA-Struktur, BOM-Guard, FA-Liste-Hierarchie v1.31-v1.35)"
ausfuehrung: "Claude in Chrome (mcp__claude-in-chrome__*) + Mensch fuer MANUAL/OPS"
---
# UAT-Protokoll IDEAL-Buendel — ausfuehrbar mit Claude in Chrome

Dieses Dokument ist fuer einen **Agenten mit Browser-Steuerung** geschrieben. Es beschreibt jeden
Test so, dass er ohne Rueckfrage ausgefuehrt und bewertet werden kann. Grundlage: `docs/TESTSZENARIEN.md`
(TS-60…69 im Worktree `feature/2026-08-07-ideal-teile-1-5`) und die Abnahme-Checkliste vom
2026-09-07. Verwandt: [[2026-09-08-ideal-code-review-nachlese]], [[testszenarien-index]].

---

## 0. Arbeitsanweisung fuer den Agenten (VOR dem ersten Test lesen)

### 0.1 Platzhalter — vom Menschen VOR dem Lauf ausfuellen
| Platzhalter | Bedeutung | Wert |
|---|---|---|
| `{{BASE_URL}}` | IDEAL-Testsystem (Master **an**, materialisierter Bestand) | |
| `{{BASE_URL_AKE}}` | AKE-Testinstanz (Master bleibt **aus**) — NUR fuer Block K | `https://akenet01.ake.at:4444` |
| `{{HAUPTFA}}` | HauptFA-Nummer mit **>= 3 Ebenen** (Sub-FA unter Sub-FA) | |
| `{{SUBFA}}` | eine Sub-FA-Nummer unterhalb von `{{HAUPTFA}}` | |
| `{{ENKEL_SUBFA}}` | eine Sub-FA auf Ebene 3 unterhalb von `{{HAUPTFA}}` | |
| `{{SUBFA_MISSING}}` | Sub-FA mit von Hand gesetztem `SageMissingSince` | |
| `{{HAUPTFA_KASKADE}}` | HauptFA, an der die Kaskade **ausgefuehrt werden darf** (Testobjekt!) | |
| `{{FA_OFFENE_BUCHUNG}}` | Sub-FA unter `{{HAUPTFA_KASKADE}}` mit offener/pausierter BDE-Buchung | |
| `{{ORDER_ID_HIER}}` | `ProductionOrders.Id` eines materialisierten Sub-FA | |
| `{{ORDER_ID_AKE}}` | `ProductionOrders.Id` eines AKE-Auftrags mit Stueckliste (Block K) | |
| `{{WERKBANK}}` | Werkbank-Name am BDE-Terminal, an der `{{HAUPTFA}}`-Sub-FAs offen sind | |
| `{{FA_MEHRFACH}}` | OrderNumber, deren Sub-FAs an `{{WERKBANK}}` **mehrfach** offen sind | |
| `{{FA_FREMD}}` | FA, die laeuft, aber **nicht** an `{{WERKBANK}}` | |
| `{{VMBEDARF_REITER}}` | ein real vorkommender `VMBedarf`-Wert (Vormontage-Reiter) | |
| `{{KOMM_ZIEL}}` | ein real vorkommendes Kommissionier-Ziel | |
| `{{KOLLISION_FA}}` | Sub-FA mit zwei Geschwistern gleicher Sage-Position (Block L-8, Testdaten) | |

Fehlt ein Platzhalter, den ein Test braucht → Test als **BLOCKED** eintragen, nicht raten.

### 0.2 Harte Regeln
1. **Session-Start:** `tabs_context_mcp` aufrufen, dann **einen neuen Tab** anlegen und ausschliesslich
   darin arbeiten. Keine fremden Tabs wiederverwenden.
2. **Keine nativen Dialoge ausloesen.** Die App nutzt `confirm()` in `Picking/Bom`,
   `PickingLeitstand`, `ServiceSettings`, `Settings`, `FaCompletion/Edit`. Ein offener Dialog blockiert
   die Steuerung. Vor JEDEM Klick auf einen Button, der bestaetigt (Kaskade, Freigeben, Speichern in
   den genannten Views), per `javascript_tool` ausfuehren:
   `window.confirm = () => true; window.alert = () => {};`
   Ist das nicht moeglich → Test als **MANUAL** eintragen und ueberspringen.
3. **Mutierende Tests nur an Testobjekten.** Tests mit Kennzeichen **[MUTIERT]** aendern Daten.
   Sie duerfen **ausschliesslich** an den Platzhalter-Objekten `{{HAUPTFA_KASKADE}}`,
   `{{FA_OFFENE_BUCHUNG}}`, `{{WERKBANK}}` ausgefuehrt werden. Nie „Alle sichtbaren freigeben"
   (BulkRelease) ausfuehren — nur die Auswahl pruefen (Zaehler), dann Auswahl aufheben.
4. **Einwegtor nicht anfassen.** `/HierarchieUmstellung` nur **lesen**. Der Master ist nach der
   Materialisierung gesperrt; ein Flip-Versuch ist kein Test, sondern ein Betriebsvorfall.
5. **Beweise:** je Test ein Screenshot `TS-<id>.png` (Dateiname wie Test-ID), Textbehauptungen ueber
   `read_page`/`get_page_text` pruefen, nach jedem Test `read_console_messages` mit
   `pattern: "error|Error|exception|500"` — Treffer im Ergebnis vermerken.
6. **Bewertung:** `PASS` (alle Erwartungen erfuellt) · `FAIL` (mind. eine Erwartung verletzt, mit
   Beobachtung) · `BLOCKED` (Vorbedingung/Testdaten fehlen) · `MANUAL` (nicht per Browser pruefbar).
7. **Abbruch:** HTTP-500-Seite, Login-Schleife oder 3 aufeinanderfolgende Tool-Fehler → anhalten,
   Stand melden, auf den Menschen warten. Nicht in Schleifen wiederholen.
8. **Windows-Auth:** Chrome verhandelt Negotiate im Intranet automatisch. Erscheint ein
   Login-Prompt → **MANUAL/BLOCKED**, nicht versuchen, Anmeldedaten einzugeben.
9. Reihenfolge einhalten (Block A zuerst). Blockweise berichten, Ergebnistabelle (Abschnitt 12)
   fortschreiben.

### 0.3 Bekannte Markierungen im DOM (fuer `find`/`read_page`)
- Gruppierte FA-Listen: je HauptFA ein `<tbody>`, Kopfzeile mit `colspan` + Chevron; Event
  `fa-liste-group-toggled`; Spalte `data-col-key="parent-sub-order-number"` (default ausgeblendet).
- FA-Struktur: `<table data-view-key="FaHierarchyStructure">`, Zeilen `.fa-node-row`, Kontext
  `.fa-node-context`, Treffer `.fa-filter-hit`, Kopfband `.fa-structure-header`.
- BDE-Terminal: Auswahlliste `#scanSelection .bde-op-btn`, Browse `#operationButtons .bde-op-btn`.
- Spaltenzahnrad: Offcanvas ueber `data-view-key` (Beschichtung `FaHierarchyBeschichtung`,
  Vormontage `FaHierarchyVormontageEinzeln`/`…Summiert`, Stueckliste `Bom`).
- Server-Spaltenfilter: Query-Parameter `colf_<col-key>=<wert>`.

---

## A. Smoke und Vorbedingungen (READ-ONLY)

### A-1 Version und Changelog
1. Navigiere `{{BASE_URL}}/Help/Changelog`.
2. Erwartet: Eintrag **v1.35.0** sichtbar (und v1.31.0 … v1.34.0 darunter). Seite ohne Fehler.
3. Screenshot `A-1.png`.

### A-2 Master-Status lesen
1. Navigiere `{{BASE_URL}}/HierarchieUmstellung`.
2. Erwartet: Status **aktiv (hierarchisch)**, Kennzeichen **gesperrt = ja** (hierarchische Daten
   vorhanden), Runbook-Verweis sichtbar. **Kein** Bedienelement klicken.
3. Negativ: zeigt die Seite „flach"/„nicht gesperrt" → **BLOCKED** fuer alle hierarchischen Bloecke,
   Mensch informieren.

### A-3 Feature-Toggles sichtbar
1. Navigiere `{{BASE_URL}}/Settings`. Erwartet: Gruppe „IDEAL" mit
   `FaHierarchyKommissionierlistenAktiv`, `FaHierarchyBeschichtungAktiv`, `FaHierarchyVormontageAktiv`
   = **true**.
2. Navigiere `{{BASE_URL}}/ServiceSettings`. Erwartet: `Sync:HierarchicalFaEnabled = true`,
   `Sync:ProductionOrdersEnabled = false`, `ProduktionsauftragHierarchisch` **ohne** Schreib-
   Bedienelement (nur Anzeige).
3. Nichts speichern.

### A-4 Testdaten-Existenz
1. Navigiere `{{BASE_URL}}/ProductionOrders?search={{HAUPTFA}}`.
2. Erwartet: eine Gruppe HauptFA `{{HAUPTFA}}` mit Sub-FA-Zeilen, darunter `{{SUBFA}}` und
   `{{ENKEL_SUBFA}}`. Fehlt etwas → **BLOCKED** und Block I/J entsprechend markieren.

---

## B. FA-Struktur `/FaHierarchy` (Teil 2 + TS-62) — READ-ONLY

### B-1 Grunddarstellung und Ausrichtung (TS-62.4)
1. Navigiere `{{BASE_URL}}/FaHierarchy?search={{HAUPTFA}}`.
2. Erwartet: genau **eine** `<table data-view-key="FaHierarchyStructure">`; je Struktur ein `<tbody>`
   mit colspan-Kopfzeile; **nur** die Struktur-Spalte rueckt ein (Chevron + Icon + Artnr); alle
   uebrigen Spalten fluchten ueber alle Ebenen. Screenshot `B-1.png`.

### B-2 Auf-/Zuklappen (TS-62.6)
1. Klicke den Chevron eines Baugruppen-Knotens → dessen Unterbaum verschwindet, andere bleiben.
2. Klicke die Struktur-Kopfzeile → ganzer `<tbody>` klappt zu; erneut → auf.
3. Klicke „Alle zu" / „Alle auf" → wirkt ueber alle Strukturen.
4. Tastatur: Fokus per Tab auf einen Chevron, `Enter` → klappt. Fokusring sichtbar.

### B-3 Typ-Icons und Legende (TS-62.7)
1. Erwartet: Wurzel = Haus/gruen, Baugruppe = Box/blau, Zukauf = Wagen/dunkel, Material = Punkt/grau;
   jedes Icon hat `title`/`aria-label`. Legende ist ein aufklappbares `<details>`.
2. Der Wurzelknoten von `{{HAUPTFA}}` traegt das Wurzel-Icon.

### B-4 Baum-Freitextfilter (TS-62.8)
1. Trage in das Baum-Filterfeld den `Artnr`-Wert von `{{ENKEL_SUBFA}}` ein.
2. Erwartet: Trefferzeile hat `.fa-filter-hit` (amber), Vorfahrenpfad hat `.fa-node-context`
   (gedimmt), nicht-treffende Geschwisterzweige sind ausgeblendet, Zaehler „X von Y Knoten"
   sichtbar und plausibel (X >= 1). Kein sichtbarer Knoten ohne sichtbaren Elternpfad.
3. Filter auf einen Wert setzen, der nicht vorkommt (`zzz-nicht-vorhanden`) → der ganze `<tbody>`
   der Struktur verschwindet, Zaehler „0 von Y".
4. Filter leeren → Ausgangszustand.

### B-5 Auswahlfilter + zwei Mechanismen (TS-62.9/.10)
1. Waehle im `<select>` „Arbeitsbereich" einen vorkommenden Wert → nur passende Zeilen (mit Pfad).
2. Setze zusaetzlich das **Hervorheben**-Feld → Hervorhebung wirkt nur innerhalb der gefilterten Zeilen;
   Beschriftungen machen beide Mechanismen unterscheidbar („Hervorheben" blendet nichts aus).

### B-6 Spaltenauswahl (TS-62.12–.15) — FUNKTIONALER TEST, nicht nur Sichtpruefung
**Warum verschaerft (Lauf 1):** Der Speicher-`PUT` auf `/api/user-view-preferences/{viewKey}` kam
mit **HTTP 503** zurueck, der Wert war aber danach da. Ein 503 auf diesem Endpunkt heisst im Betrieb
nicht „Serverfehler", sondern **„meine Einstellungen gehen verloren"**: das Zahnrad, Breiten,
Reihenfolge und die Standard-Sortierung sehen funktionsfaehig aus und sind beim naechsten
Seitenaufruf weg. Deshalb wird hier die **Persistenz** geprueft, nicht die Optik — und jeder 503 ist
ein FAIL, auch wenn der Wert diesmal ankam.
1. Oeffne das Zahnrad → Offcanvas. Blende eine nicht-gesperrte Spalte aus, schliesse, lade die Seite
   neu → bleibt ausgeblendet (Persistenz).
2. Struktur- und Matchcode-Spalte lassen sich **nicht** ausblenden (locked).
3. Kein Spaltenkopf ist sortierbar (kein `data-sortable`), Zahnrad bietet **keine**
   „Standard-Sortierung" (TS-62.11).
4. **Netzwerk mitlesen** (`read_network_requests`, Filter `user-view-preferences`) waehrend Schritt 1
   und 5. Erwartet: **jeder** `PUT` → `200`. Jeder andere Status = FAIL, Status + Uhrzeit notieren.
   Das Speichern ist um 1,5 s entprellt — nach der letzten Aenderung 3 s warten, sonst wird der
   `PUT` gar nicht erst gesendet und „kein 503" waere ein Fehlschluss.
5. **Zweite Aenderungsart** zusaetzlich pruefen, weil sie ueber denselben Endpunkt laeuft: eine
   Spalte per Ziehen schmaler machen **und** (wo angeboten) „Standard-Sortierung speichern".
   Danach neu laden → Breite und Sortierung muessen erhalten sein.
6. **Zwei-Tab-Gegenprobe** (deckt eine vermutete Ursache ab, siehe unten): dieselbe Liste in zwei
   Tabs oeffnen, in beiden **kurz nacheinander** eine Spalte umschalten, beide neu laden.
   Erwartet: beide Aenderungen ueberleben, kein `500`/`503`. Tritt hier ein Fehler auf, ist die
   Ursache ein Wettlauf zweier gleichzeitiger Speicher-Aufrufe, kein IIS-Problem.
7. Spalte wieder einblenden (Aufraeumen).

**Bei Fehlschlag: diese drei Belege sichern** (sie entscheiden, ob der Fehler aus der Anwendung oder
aus IIS kommt — im Anwendungscode gibt es kein 503, siehe Analyse unten):
- Serilog-Zeile des Requests (`logs/`): Steht der `PUT` mit Status 503 drin, hat die Anwendung
  geantwortet. Fehlt der Request ganz, hat IIS vor der Anwendung abgewiesen.
- IIS-Logzeile (`sc-status` **und** `sc-substatus`) zur selben Uhrzeit.
- Windows-Ereignisanzeige, Quelle `IIS-W3SVC-WP` / `ASP.NET Core Module`: Recycling oder
  Rapid-Fail-Protection des App-Pools im selben Zeitfenster?

### B-7 Schmaler Bildschirm (TS-62.5)
1. `resize_window` auf 900×800. Erwartet: hintere Spalten ausgeblendet, Tabelle horizontal scrollbar,
   Struktur-Spalte nicht komprimiert. Zurueck auf 1400×900.

---

## C. Kommissionierlisten (Teil 3, nach Etappe 8) — READ-ONLY

### C-1 Liste je Ziel
1. Navigiere `{{BASE_URL}}/FaHierarchyKommissionierListen?target={{KOMM_ZIEL}}`.
2. Erwartet: Gruppen je HauptFA, nur Positionen mit gesetztem Kommissionier-Ziel, **alle Ebenen**
   (auch `SubFA != 0`-Zeilen), **kein** Anomalie-Banner. Seitengroesse-Umschalter vorhanden;
   `TotalCount` zaehlt Gruppen.
3. Screenshot `C-1.png`.

### C-2 Summiert + KW-Filter
1. Navigiere `{{BASE_URL}}/FaHierarchyKommissionierListen/Summiert?target={{KOMM_ZIEL}}`.
2. Erwartet: Spalten HauptFA/Artnr/Sollmenge/Ziel; KW-Filter (`<input type="week">` von/bis) auf
   `KO_Termin`. Eine KW waehlen → Summen reduzieren sich; leeren → Ausgangswerte.

### C-3 Druck spiegelt Filter
1. Navigiere `{{BASE_URL}}/FaHierarchyKommissionierListen/Print?target={{KOMM_ZIEL}}`.
2. Erwartet: ein Ausdruck je HauptFA mit Barcode = HauptFA, Positionsmenge identisch zu C-1.

---

## D. Beschichtungsauftrag `/FaHierarchyBeschichtung` (Teil 4, TS-60) — READ-ONLY

### D-1 Grunddarstellung (TS-60.8)
1. Navigiere `{{BASE_URL}}/FaHierarchyBeschichtung`.
2. Erwartet: beschichtete Positionen **aller Ebenen**, gruppiert nach HauptFA, Gruppenkopf mit
   Dienstleister/RAL/Terminen; Spalten HauptFA/HauptArtnr/Artnr/Matchcode/Sollmenge/Beschichtet/
   Breite/Hoehe/Tiefe. Kein Anomalie-Banner.

### D-2 Server-Spaltenfilter + Gruppen-Paging (TS-60.9)
1. Navigiere `{{BASE_URL}}/FaHierarchyBeschichtung?colf_matchcode=<Matchcode einer sichtbaren Zeile>`.
2. Erwartet: nur passende Positionen; Gruppen ohne Treffer verschwinden komplett (kein leerer Kopf).
3. Seitengroesse auf 25 stellen → Seitenzahl entspricht **Gruppen**anzahl, nicht Zeilen.

### D-3 Druck (TS-60.10)
1. Klicke „Drucken" bei aktivem Filter aus D-2.
2. Erwartet: ein Dokument je HauptFA-Gruppe (Seitenumbruch), Barcode = HauptFA, exakt die gefilterte
   Menge. Filter ohne Treffer → Hinweistext statt leerem Dokument.

### D-4 Spaltenauswahl (TS-60.14–.22)
1. Zahnrad: eine nicht-gesperrte Spalte ausblenden, Reload → persistiert; `HauptFA`+`Matchcode`
   locked; „Standard-Sortierung speichern" **verfuegbar**.
2. Bei >= 2 Gruppen: Spaltenaenderung wirkt in **jeder** Gruppe; Klick auf einen Spaltenkopf sortiert
   **innerhalb jeder Gruppe** separat (nicht nur die erste).
3. Spalte wieder einblenden.

---

## E. Vormontage `/FaHierarchyVormontage` (Teil 5, TS-61) — READ-ONLY

### E-1 Sicht 1 + Reiter (TS-61.13/.14)
1. Navigiere `{{BASE_URL}}/FaHierarchyVormontage?bereich={{VMBEDARF_REITER}}`.
2. Erwartet: Reiter je Arbeitsbereich; Positionen mit `VMBedarf` (Blatt **und** Baugruppe) nach
   HauptFA gruppiert; Spalten inkl. Sollmenge, Fertigungmenge, VMBedarf, Material. Reiterwechsel
   liefert andere Teilmenge.

### E-2 Sicht 2 Summiert + Mengenabgleich (TS-61.15/.16)
1. Navigiere `{{BASE_URL}}/FaHierarchyVormontage/Summiert?bereich={{VMBEDARF_REITER}}`.
2. Erwartet: je Matchcode **eine** Zeile mit **zwei** Summen (Sollmenge und Fertigungmenge).
3. Abgleich: fuer einen Matchcode die Zeilen aus E-1 von Hand addieren → Summe stimmt (keine
   Doppelzaehlung ueber Ebenen).

### E-3 KW-Filter auf FE_Termin (TS-61.17b)
1. In Sicht 2 KW von/bis setzen → Summen reduzieren sich auf HauptFAs mit `FE_Termin` in der KW;
   leeren → Ausgangswerte.

### E-4 Spaltenauswahl beide Sichten (TS-61.21–.28)
1. Zahnrad in Sicht 1 (`FaHierarchyVormontageEinzeln`) und Sicht 2 (`FaHierarchyVormontageSummiert`):
   Persistenz, Locks (Sicht 1 HauptFA+Matchcode, Sicht 2 Matchcode), Standard-Sortierung in
   **beiden** verfuegbar. Aufraeumen.

---

## F. Standorteinstellungen `/StandortEinstellungen` (Teil 6, TS-67)

### F-1 Gruppierte Anzeige + Master read-only (AK1/AK2) — READ-ONLY
1. Navigiere `{{BASE_URL}}/StandortEinstellungen`.
2. Erwartet: Gruppen Firmendaten · Mandant/Sage-Views · Feature-Schalter · Struktur-Import.
   `ProduktionsauftragHierarchisch` nur als **Status** (aktiv, gesperrt ja, Stand) mit Link
   „Umschalten" → `/HierarchieUmstellung`; **kein** Eingabefeld dafuer. Screenshot `F-1.png`.

### F-2 Atomares Speichern (AK4) — [MUTIERT, selbstheilend]
1. `javascript_tool`: `window.confirm = () => true;`
2. Firmenname auf `UAT-Test {{Datum}}` setzen **und** Baum-Maximaltiefe auf `abc`. Speichern.
3. Erwartet: Fehleranzeige, **beide** eingegebenen Werte bleiben im Formular, **kein** Wert wurde
   gespeichert (Reload: Firmenname unveraendert, Maximaltiefe unveraendert).
4. Kein weiterer Speichervorgang.

### F-3 Admin-only (AK6) — MANUAL
Mit einem Nicht-Admin-Benutzer nicht per Browser-Agent pruefbar (Windows-Auth). Als **MANUAL**
eintragen.

---

## G. Einwegtor `/HierarchieUmstellung` (Teil 7, TS-63) — READ-ONLY

### G-1 Gesperrt-Zustand (TS-63.4)
1. Navigiere `{{BASE_URL}}/HierarchieUmstellung`.
2. Erwartet: „gesperrt" mit Begruendung (hierarchische Daten vorhanden), Runbook-Verweis, kein
   aktiver Flip-Button bzw. Button deaktiviert. **Nichts klicken.**
3. `{{BASE_URL}}/ServiceSettings`: Master ohne Schreib-Bedienelement (Wiederholung A-3, hier als
   Nachweis dokumentieren).

Alle uebrigen TS-63/64/65-Faelle (Flip, Datenlauf, Sammelmail, Audit-Log) sind **OPS/MANUAL**
(Abschnitt 11).

---

## H. BOM-Guard (TS-68) — READ-ONLY

### H-1 IDEAL-Grundfall (TS-68.2)
1. Navigiere `{{BASE_URL}}/Picking`, suche `{{SUBFA}}`, klicke den Stuecklisten-/BOM-Knopf.
2. Erwartet: **Hinweisseite** („…wird im hierarchischen Modus ueber die FA-Struktur angezeigt"),
   Badge „FA-Struktur", funktionierender Link zu `/FaHierarchy`. **Kein 500**, keine Exception in der
   Konsole. Screenshot `H-1.png`.

### H-2 Direktaufruf (TS-68.3)
1. Navigiere `{{BASE_URL}}/Picking/Bom/{{ORDER_ID_HIER}}` → gleicher Hinweis.

### H-3 Weitere Aufrufer (TS-68.5)
1. Navigiere `{{BASE_URL}}/Picking/PrintBom/{{ORDER_ID_HIER}}` → keine Exception, leeres
   Ergebnis/Hinweis.
2. Navigiere `{{BASE_URL}}/FaWorklist`, oeffne die read-only Stueckliste eines Sub-FA → gleicher Guard.

TS-68.4 (Log-Nachweis „keine Query gegen `vw_AKE_…`") ist **OPS/MANUAL**.

---

## I. FA-Liste hierarchiefaehig (v1.35, TS-69)

### I-1 Sechs Ansichten gruppiert (TS-69.1) — READ-ONLY
Fuer jede URL: navigiere, pruefe: `<tbody>` je HauptFA, colspan-Kopfzeile, Chevron, Gruppen beim Laden
**aufgeklappt**, Kopf „N Auftraege · M Sub-FAs" (TS-69.19). Screenshot `I-1-<view>.png`.
1. `{{BASE_URL}}/ProductionOrders`
2. `{{BASE_URL}}/Picking`
3. `{{BASE_URL}}/PickingLeitstand`
4. `{{BASE_URL}}/FaCompletion`
5. `{{BASE_URL}}/FaWorklist`
6. `{{BASE_URL}}/Tracking`

### I-2 Nachfahren-Scope (TS-69.2) — READ-ONLY
1. `{{BASE_URL}}/ProductionOrders?search={{HAUPTFA}}` → in der Gruppe erscheint `{{ENKEL_SUBFA}}` als
   eigene Zeile (nicht nur direkte Kinder).

### I-3 Klappen + Reload (TS-69.3) — READ-ONLY
1. Chevron der Gruppe `{{HAUPTFA}}` klicken → nur diese Gruppe zu. Reload → alle wieder offen.

### I-4 Suchtreffer in zugeklappter Gruppe (TS-69.4) — READ-ONLY, KRITISCH
1. Gruppe `{{HAUPTFA}}` zuklappen. Dann Suche/Filterkarte nach `{{SUBFA}}` absenden.
2. Erwartet: Seite laedt neu, Gruppe ist **aufgeklappt**, die Zeile `{{SUBFA}}` ist sichtbar.
   Hinweis: Treffer-**Hervorhebung** ist laut Befund F2 nicht implementiert — deren Fehlen ist
   **kein** FAIL, nur zu vermerken.
3. Negativ: Suche nach `999999999` → klare „kein Treffer"-Anzeige, nicht scheinbar leer.

### I-5 Sub-vor-Haupt (TS-69.5) — READ-ONLY
1. Suche `{{SUBFA}}` → genau diese Zeile (in ihrer Gruppe). Suche `{{HAUPTFA}}` → ganze Gruppe.
2. Dasselbe ueber Spaltenfilter `colf_order-number` bzw. FA-Nummern-Spalte.

### I-6 Paginierung ueber Gruppen (TS-69.6) — READ-ONLY
1. Seitengroesse auf den kleinsten Wert stellen, durch die Seiten blaettern.
2. Erwartet: keine Gruppe ueber zwei Seiten getrennt; Seitenzahl = Gruppenanzahl / Seitengroesse.

### I-7 Spalten-Mapping + Elternspalte (TS-69.7) — READ-ONLY
1. FA-Nummer-Zelle zeigt SubOrderNumber, Kopf die HauptFA.
2. Zahnrad: Spalte „Uebergeordnete Sub-FA" einblenden → Werte plausibel gegen `/FaHierarchy`
   (`{{ENKEL_SUBFA}}` zeigt `{{SUBFA}}`; Wurzel leer). Wieder ausblenden.

### I-8 SageMissingSince-Badge (TS-69.8) — READ-ONLY
1. Suche `{{SUBFA_MISSING}}` → eigener Badge/Tooltip „in Struktur vermisst seit …", Zeile bleibt
   bedienbar. Kein „In Sage geloescht"-Badge gleichzeitig.

### I-9 Leitstand Bulk-Select (TS-69.10) — READ-ONLY (keine Freigabe!)
1. `{{BASE_URL}}/PickingLeitstand`. „Alle sichtbaren auswaehlen" klicken → Zaehler notieren.
2. Gruppe `{{HAUPTFA}}` **zuklappen** → Zaehler sinkt um die Zeilen dieser Gruppe (Event
   `fa-liste-group-toggled` hebt die Auswahl unsichtbarer Zeilen auf).
3. **Nicht** „Markierte freigeben" klicken. Auswahl aufheben.

### I-10 Kommissionierung Prio-Reihenfolge (TS-69.11) — READ-ONLY
1. `{{BASE_URL}}/Picking`: dringlichste HauptFA-Gruppe steht zuerst; innerhalb der Gruppe
   Prio-Reihenfolge (bewusst **nicht** SubOrderNumber). Klick auf eine Sub-FA-Zeile oeffnet die
   Stueckliste (→ H-1-Hinweis).

### I-11 FaWorklist AG-Status je Sub-FA (TS-69.12) — READ-ONLY
1. `{{BASE_URL}}/FaWorklist`, Arbeitsgang waehlen → Merkmal-Spalten + Status-Auswahlfeld je Sub-FA
   sichtbar. **Nicht** aendern.

### I-12 Teileverfolgung 3 Ebenen (TS-69.13) — READ-ONLY
1. `{{BASE_URL}}/Tracking?search={{HAUPTFA}}` → HauptFA → Sub-FA → AG, zweistufiger Fortschritt
   (HauptFA „fertige Sub-FAs/alle", Sub-FA „rueckgemeldete AGs/alle"), Chevron auf beiden Ebenen,
   „Alle auf/zu". **Nicht** rueckmelden.

### I-13 Kaskade Dialog + Ausfuehrung (TS-69.14/.15) — [MUTIERT, nur `{{HAUPTFA_KASKADE}}`]
1. `{{BASE_URL}}/PickingLeitstand?search={{HAUPTFA_KASKADE}}`.
2. `javascript_tool`: `window.confirm = () => false;` (erst nur den Dialog-Text lesen!). Klicke in der
   Gruppen-Kopfzeile „Alle Sub-FAs fertigmelden". Falls der Dialog als eigenes Modal (kein `confirm`)
   erscheint: Text per `read_page` lesen.
3. Erwartet im Dialog: **Gesamtzahl** Sub-FAs **und separat** „mit offener Buchung" (>= 1 wegen
   `{{FA_OFFENE_BUCHUNG}}`).
4. Nur wenn der Mensch fuer diesen Lauf freigegeben hat: `window.confirm = () => true;`, erneut
   klicken → alle Nachfahren (auch tiefere Ebenen) `IsDoneBde=true`; SuccessMessage; ein
   Log-Eintrag (OPS prueft). Sonst Schritt 4 als **MANUAL**.
5. Keine Ruecknahme-Aktion auf Gruppenebene vorhanden (TS-69.18).

### I-14 Artikelinfo Z4 (TS-69.19) — READ-ONLY
1. `{{BASE_URL}}/Articles/Info` fuer einen Artikel aus `{{HAUPTFA}}` → Fusszeile „N Auftraege ·
   M Sub-FAs offen", FA-Nr-Spalte zeigt Sub-FA-Nummer.

### I-15 Client-Sortierung im gruppierten Modus (Vorbehalt) — READ-ONLY
1. In `/ProductionOrders` auf einen Spaltenkopf klicken → Beobachtung notieren, ob **jede** Gruppe
   sortiert wird oder nur die erste (bekannter Vorbehalt; Ergebnis dokumentieren, kein FAIL).

---

## J. BDE Sub-FA-Scan `/BdeTerminal` (Teil 8, TS-66) — teils [MUTIERT]

Vorbedingung: Terminal auf `{{WERKBANK}}` einstellen, Operator anmelden (Ablauf laut Terminal-UI).
Scans werden als Tastatureingabe in das Scan-Feld gesetzt (`form_input`/`computer` Typing + Enter).

### J-1 Normal-Modus, mehrere Sub-FAs (TS-66.1) — [MUTIERT bei Auswahl]
1. Scan `{{FA_MEHRFACH}},<AG-Nr>` (Format `FA-Nr,AG-Nr`).
2. Erwartet: **Auswahlliste** `#scanSelection .bde-op-btn` mit Sub-FA-Nr + Bezeichnung + AG. Keine
   stille Buchung. Screenshot `J-1.png`.
3. Nur mit Freigabe: einen Eintrag waehlen → genau dieser Sub-FA wird gebucht. Sonst abbrechen.

### J-2 Nur-FA-Modus (TS-66.2) — [MUTIERT bei Auswahl]
1. Nur-FA-Meldung aktivieren, Scan `{{FA_MEHRFACH}}` ohne AG → **dieselbe** Auswahlliste; kein
   „last wins" auf einen Button. Auswahl nur mit Freigabe (startet Produktion).

### J-3 Eindeutiger Treffer (TS-66.3) — [MUTIERT]
1. Scan einer FA mit genau einem offenen AG an `{{WERKBANK}}` → **Direktbuchung ohne Auswahl**
   (Start-Buttons erscheinen). Nur mit Freigabe.

### J-4 Nicht an dieser Werkbank (TS-66.4) — READ-ONLY
1. Scan `{{FA_FREMD}},<AG>` → Meldung „Dieser Auftrag hat an dieser Werkbank keinen offenen
   Arbeitsgang." (nicht der generische Unbekannt-Fehler). Keine Buchung.

### J-5 Unbekannte FA (TS-66.6) — READ-ONLY
1. Scan `000000,01` → „Arbeitsgang nicht gefunden" unveraendert.

### J-6 Teileverfolgung Filter (TS-66.9) — READ-ONLY
1. `{{BASE_URL}}/Tracking?search={{FA_MEHRFACH}}` → **alle** Sub-FAs der OrderNumber gelistet.

---

## K. Flachmodus-Regression auf `{{BASE_URL_AKE}}` (Master aus) — READ-ONLY, HART

Auf der IDEAL-Instanz **nicht** ausfuehrbar (Einwegtor).

**Harte Vorbedingung — Deploy, nicht Adresse:** Der Buendel-Stand muss **auch auf
`https://akenet01.ake.at:4444` publiziert sein**. In Lauf 1 war dieser Block als „Instanz fehlt"
notiert; richtig ist: die Instanz gab es, publiziert war dort nur der alte Stand. Solange das so
ist, prueft der ganze Block nichts und bleibt **BLOCKED** — ein gruenes Ergebnis waere hier
schlimmer als gar keines, weil es eine Regression bescheinigen wuerde, die nie getestet wurde.

**Der Master bleibt auf `akenet01` auf `false`.** Block K prueft den Flachmodus. Ein Flip waere
dort ein Einwegtor (ADR 0012) und ist ausdruecklich kein Bestandteil des Tests.

### K-1 Sechs Ansichten bit-identisch (TS-69.9)
Fuer `/ProductionOrders`, `/Picking`, `/PickingLeitstand`, `/FaCompletion`, `/FaWorklist`, `/Tracking`:
genau **ein** `<tbody>`, keine Gruppen-Kopfzeilen, keine sichtbare neue Spalte, zeilenbasierte
Paginierung, im Leitstand **kein** „Alle Sub-FAs fertigmelden". Zahnrad: Spalte „Uebergeordnete
Sub-FA" existiert dort ggf. als ausgeblendete Option (bekannt, benign) — vermerken, kein FAIL.

### K-2 BOM-Knopf AKE (TS-68.1)
`{{BASE_URL_AKE}}/Picking/Bom/{{ORDER_ID_AKE}}` → Positionen wie bisher, Quelle SAGE/CACHE, kein
Hinweis „FA-Struktur".

### K-3 Sortier-Fix flache Listen (TS-61.30)
`/ProductionOrders` Spaltenkopf klicken → sortiert wie zuvor (ein `<tbody>`). Stichprobe
`/PickingLeitstand`, `/FaWorklist`.

### K-4 Standorteinstellungen AKE (TS-67 AK3)
`{{BASE_URL_AKE}}/StandortEinstellungen` → leere IDEAL-Felder / „Flach (AKE-Standard)" ohne Fehler.

---

## L. BOM-Bridge (v1.36.0, TS-70) — ersetzt Block H (Guard) im Nachlauf

Vorbedingung: Buendel-Stand >= `22d31ae` deployt (v1.36.0 im Changelog, A-1 erneut pruefen). Block H
gilt dann nicht mehr — die Hinweisseite existiert nicht mehr.

### L-0 ZUERST — Vorbau-/Vervollstaendigungs-BOM einer HauptFA zeigt die Vollstruktur (Ruling 4) — READ-ONLY, **am Bildschirm entscheiden**
**Warum zuerst:** Das einzige Ruling mit Folgen in der Halle. Die Scope-Regel „HauptFA → komplette
Struktur" wurde bewusst auch auf die read-only Stueckliste (FA-Abarbeitungsliste/FA-Vervollstaendigung)
und auf `PrintBom`/`PrintPicking` ausgedehnt. Wer dort fuer eine HauptFA Material holt, sieht jetzt
**alle Teile aller Ebenen**, nicht nur die Baugruppen der obersten Ebene — richtig, wenn am HauptFA
das ganze Geraet kommissioniert wird; falsch, wenn dort nur die oberste Ebene gemeint ist.

> **Nicht am Ausdruck entscheiden (Vorgabe 2026-09-09).** Die Druck-Whitelist in `PrintBom.cshtml`
> kennt „Ebene" und „Komm.-Ziel" nicht (Nachlese-Punkt 14). Genau diese beiden Spalten braucht man
> aber, um die Frage zu beurteilen. Ein Ausdruck ohne sie zeigt eine lange, flache Teileliste ohne
> erkennbare Tiefe — und verleitet zu einem „zurueckbauen" **aus dem falschen Grund**. Die
> Entscheidung faellt am Bildschirm; der Ausdruck wird erst danach bewertet (L-7).

1. `{{BASE_URL}}/FaWorklist` (Vorbedingung `FaCompletionAktiv=true`) → read-only Stueckliste der
   **HauptFA-Zeile** `{{HAUPTFA}}` oeffnen; ebenso `{{BASE_URL}}/FaCompletion` → Stuecklisten-Link.
2. Erwartet (Ist-Verhalten): alle Ebenen flach mit Pfad-Positionen, Baugruppen als eigene Zeilen
   (Chevron), Blaetter darunter; Mengen = Sollmenge. Screenshot `L-0-liste.png`.
3. **Spalten „Ebene" und „Komm.-Ziel" einblenden** (Zahnrad, falls nicht sichtbar) und dem
   Fachbereich **diese Ansicht** vorlegen: „Holt der Werker an der HauptFA das ganze Geraet oder nur
   die oberste Ebene?" Ergebnis als Entscheidung in die Aufgabe
   [[2026-09-08-bom-schnittstellen-bridge-hierarchisch-umsetzung]] eintragen.
4. Gegenprobe Sub-FA: dieselbe Stueckliste an `{{SUBFA}}` → nur direkte Kinder (siehe L-2).
5. Bewertung: PASS = Verhalten entspricht der Fachentscheidung; FAIL = Fachbereich will an der
   HauptFA nur die oberste Ebene → Rueckbau ist ein Einzeiler (`BomScopes.ForOrder` im
   `ReadOnlyBomBuilder`/`PrintBom` auf `DirectChildren`), aber vor dem Merge zu entscheiden.
6. **Das Ergebnis steuert L-7.** Vollstruktur bleibt → der Ausdruck muss die beiden Spalten
   bekommen (Whitelist-Ergaenzung vor dem Merge). Rueckbau → der Ausdruck bleibt wie er ist.

### L-1 HauptFA-Vollansicht (TS-70.1, 70.9) — READ-ONLY
1. `{{BASE_URL}}/ProductionOrders?…` Gruppe `{{HAUPTFA}}`, Stuecklisten-Knopf an der **HauptFA-Zeile**
   (Sub-FA == HauptFA) → oder direkt `{{BASE_URL}}/Picking/Bom/<Id der HauptFA-Zeile>`.
2. Erwartet: echte Stueckliste (kein Hinweis), Badge „FA-Struktur", **alle Ebenen** flach mit Pfad-Positionen
   (`3`, `3.7`, `3.7.2`), Baugruppen mit Chevron auf-/zuklappbar, Spalten `Komm.-Ziel`, `Hauptlagerplatz`,
   `Ebene`, `Vater-Sub-FA`; Tooltip an der Pos.-Zelle „Sage-Position: n"; Zahnrad → Spalte „Sage-Pos."
   einblendbar (default aus). Mengen = Sollmenge (keine Hochrechnung). Druck-Knopf vorhanden.
3. Screenshot `L-1.png`. Konsole ohne App-Fehler.

### L-2 Sub-FA-Ansicht (TS-70.2) — READ-ONLY
1. `{{BASE_URL}}/Picking/Bom/{{ORDER_ID_HIER}}` (Sub-FA `{{SUBFA}}`).
2. Erwartet: nur die **direkten Kinder** von `{{SUBFA}}`, keine Enkel; Kopfzeile „FA {{SUBFA}} | HauptFA {{HAUPTFA}}".

### L-3 Komm.-Ziel-Filter (TS-70.3) — READ-ONLY
1. In L-1 Spaltenfilter `Komm.-Ziel` auf `{{KOMM_ZIEL}}` setzen → nur passende Zeilen; leeren → alle
   Zeilen zurueck (kein vorab gefilterter Zustand). Nicht vorkommender Wert → leere, erkennbar
   gefilterte Ansicht.

### L-4 Artikelinfo (TS-70.4) — READ-ONLY
1. `{{BASE_URL}}/Articles/Info?articleNumber=<Artnr eines Bauteils aus {{HAUPTFA}}>` (Suche ueber die
   Artikel-Seite, falls der Parameter anders heisst).
2. Erwartet: Tabelle „HauptFA | Sub-FA (Baugruppe) | Termine", HauptFA fett; Fusszeile „… basierend auf
   FA-Struktur, Mengen = Sollmenge".

### L-5 Menue (TS-70.5) — READ-ONLY
1. Als Admin (beide Rechte): Menue „Kommissionierung" ist ein Dropdown mit „Kommissionierung" +
   „Kommissionierlisten". Nur-Lager-Nutzer: kein Dropdown, Einzel-Link (MANUAL).

### L-6 Klasse-D-Skip im Protokoll (TS-70.7) — READ-ONLY
1. Aktivitaets-Protokoll oeffnen (Menue Verwaltung → Protokoll) nach einem Sync-Zyklus.
2. Erwartet: Laeufe `CoatingDetection`, `FaWorkStepDetection`, `BomCache` als **uebersprungen
   (hierarchischer Modus)**, nicht als Fehler — **drei** Laeufe; der spezifische BOM-Cache-Pfad loggt nur
   in Serilog (OPS).

### L-7 Druck (TS-70.11) — READ-ONLY, **Bewertung haengt am Ergebnis von L-0**
1. In L-1 „Stueckliste drucken" → Vollstruktur mit Pfad-Positionen, **ohne** die Spalten Komm.-Ziel,
   Hauptlagerplatz, Sage-Pos., Ebene und Vater-Sub-FA (eigene Whitelist in `PrintBom.cshtml`).
2. **Fall A — L-0 endete mit Rueckbau auf direkte Kinder:** Die Luecke ist ein bewusster Zustand.
   Der Ausdruck zeigt dann eine flache Liste der obersten Ebene, fuer die Ebene und Vater-Sub-FA
   nichts aussagen wuerden. **PASS**, nichts zu tun.
3. **Fall B — L-0 endete mit „Vollstruktur bleibt":** Die Luecke ist **ein Mangel**, kein
   dokumentierter Zustand. Ein mehrstufiger Ausdruck ohne „Ebene" und „Komm.-Ziel" ist in der Halle
   nicht benutzbar — man sieht den Zeilen ihre Tiefe und ihr Kommissionier-Ziel nicht mehr an.
   **FAIL**, und die Whitelist ist **vor dem Merge** um mindestens diese beiden Spalten zu ergaenzen
   (Nachlese-Punkt 14 wandert damit von „nach dem Merge" auf „merge-blockierend").

### L-8 Kollision (TS-70.10) — READ-ONLY, nur mit Testdaten
1. Vorbedingung: zwei Geschwister mit gleicher Sage-Position unter demselben Sub-FA (`{{KOLLISION_FA}}`,
   sonst BLOCKED). Erwartet: beide Zeilen da, zweite mit Pfad `n~2` + Badge „Kollision".

## 11. Nicht per Browser pruefbar — OPS/MANUAL (Mensch)

| Punkt | Quelle |
|---|---|
| DB-Backup, sysjobs-Deaktivierung (AK13), Deploy-Reihenfolge, App-Neustart als DDL-Beweis | Checkliste „Vor dem Start" |
| PPSImport-Grant, `SageConnection`, View-Namen | Teil 1 |
| Materialisierungs-Datenlauf (Regel 1–3, `SageMissingSince`, Sammelmail genau 1×) | TS-64 |
| Master-Flip / Audit-Eintrag / Guard-Ablehnung per POST | TS-63.1–.9 |
| Auto-Erledigt-Guard (`Sync:FaZusatzinfoAutoErledigtEnabled`) | TS-65 |
| Log-Nachweis „keine Query gegen `vw_AKE_…`" | TS-68.4 |
| Kaskade-Atomaritaet (simulierter Fehler) + Log-Eintrag | TS-69.16/.17 |
| Rollen-Regressionen mit Nicht-Admin-/Nicht-Rollen-Benutzern | TS-60.12, 61.19, 67 AK6, 69.20 |
| Kontrastwerte am realen Terminal (WCAG AA) | TS-62.3 |
| Writer-Rollback `StandortSettingsWriter` (nur relational) | Review-Nachlese |

---

## 12. Ergebnistabelle (vom Agenten fortschreiben)

| Test | Ergebnis | Beobachtung / Abweichung | Konsole (Treffer) | Screenshot |
|---|---|---|---|---|
| A-1 | | | | |
| A-2 | | | | |
| A-3 | | | | |
| A-4 | | | | |
| B-1 | | | | |
| B-2 | | | | |
| B-3 | | | | |
| B-4 | | | | |
| B-5 | | | | |
| B-6 | | | | |
| B-7 | | | | |
| C-1 | | | | |
| C-2 | | | | |
| C-3 | | | | |
| D-1 | | | | |
| D-2 | | | | |
| D-3 | | | | |
| D-4 | | | | |
| E-1 | | | | |
| E-2 | | | | |
| E-3 | | | | |
| E-4 | | | | |
| F-1 | | | | |
| F-2 | | | | |
| F-3 | MANUAL | | | |
| G-1 | | | | |
| H-1 | | | | |
| H-2 | | | | |
| H-3 | | | | |
| I-1 | | | | |
| I-2 | | | | |
| I-3 | | | | |
| I-4 | | | | |
| I-5 | | | | |
| I-6 | | | | |
| I-7 | | | | |
| I-8 | | | | |
| I-9 | | | | |
| I-10 | | | | |
| I-11 | | | | |
| I-12 | | | | |
| I-13 | | | | |
| I-14 | | | | |
| I-15 | | | | |
| J-1 | | | | |
| J-2 | | | | |
| J-3 | | | | |
| J-4 | | | | |
| J-5 | | | | |
| J-6 | | | | |
| K-1 | | | | |
| K-2 | | | | |
| K-3 | | | | |
| K-4 | | | | |

## 13. Abschlussbericht (Format)

```
UAT IDEAL-Buendel — Lauf vom <Datum>, Basis-URL <…>
PASS: n · FAIL: n · BLOCKED: n · MANUAL: n
FAILs (je Test-ID: Beobachtung, Screenshot)
BLOCKED (fehlende Platzhalter/Testdaten)
Konsolen-Fehler (Test-ID → Meldung)
Empfehlung: Schranke 2 nehmen / nicht nehmen, mit Begruendung
```
