---
typ: testprotokoll
status: bereit
erstellt: 2026-09-09
scope: "BOM-Bridge hierarchisch (v1.36.0, TS-70.1-70.11) — Schranke 2"
spec: "[[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]]"
ausfuehrung: "Claude in Chrome (mcp__claude-in-chrome__*) + Mensch fuer MANUAL/OPS"
---
# UAT-Protokoll BOM-Bridge — ausfuehrbar mit Claude in Chrome

Fokussiertes Protokoll fuer **eine** Spec: [[2026-09-08-bom-schnittstellen-bridge-hierarchisch-spec]]
(v1.36.0, Stueckliste aus der FA-Struktur statt aus dem AKE-Cache). Es loest **Block L** des
Buendel-Protokolls [[2026-09-08-uat-protokoll-ideal-buendel-chrome]] ab — dort steht nur noch ein
Verweis hierher, damit es die Szenarien nicht zweimal gibt.

Grundlage: `docs/TESTSZENARIEN.md`, Kapitel **TS-70** im Worktree
`feature/2026-08-07-ideal-teile-1-5`. Verwandt: [[2026-09-08-bom-bridge-nachlese]],
[[0013-bom-bridge-repository-schnittstelle-statt-cache-kopie]].

---

## 0. Arbeitsanweisung fuer den Agenten (VOR dem ersten Test lesen)

### 0.1 Platzhalter — vom Menschen VOR dem Lauf ausfuellen
| Platzhalter | Bedeutung | Wert |
|---|---|---|
| `{{BASE_URL}}` | IDEAL-Testsystem, Master **an** | `http://idealweb01.ideal.ideal-ake.at:88` |
| `{{HAUPTFA}}` | HauptFA-Nummer mit **>= 3 Ebenen** (Sub-FA unter Sub-FA) | |
| `{{ORDER_ID_HAUPT}}` | `ProductionOrders.Id` der **HauptFA-Zeile** (dort ist `SubOrderNumber == OrderNumber`) | |
| `{{SUBFA}}` | eine Sub-FA-Nummer unterhalb von `{{HAUPTFA}}`, die selbst Kinder hat | |
| `{{ORDER_ID_SUB}}` | `ProductionOrders.Id` von `{{SUBFA}}` | |
| `{{ARTNR_BAUTEIL}}` | Artikelnummer eines Bauteils, das unter `{{HAUPTFA}}` vorkommt | |
| `{{KOMM_ZIEL}}` | ein real vorkommendes Kommissionier-Ziel (Spalte „Komm.-Ziel") | |
| `{{KOLLISION_FA}}` | `ProductionOrders.Id` eines Sub-FA mit **zwei Geschwistern gleicher Sage-Position** | |

Fehlt ein Platzhalter, den ein Test braucht → Test als **BLOCKED** eintragen, **nicht raten** und
**nicht** auf einen anderen Auftrag ausweichen.

### 0.2 Vorbedingungen — als Test B-1 pruefen, nicht voraussetzen
- Buendel-Stand **v1.36.0** auf `{{BASE_URL}}` publiziert.
- Master `ProduktionsauftragHierarchisch = true`, materialisierter Bestand vorhanden.
- `FaCompletionAktiv = true` (sonst sind B-2 und B-9 nicht erreichbar; in Lauf 1 stand der
  Schalter aus und hat drei Tests blockiert).
- `Sync:ProductionOrdersEnabled = false` (sonst ueberschreibt der AKE-FA-Sync Mengen und Termine
  der Sub-FAs waehrend des Laufs — Befund H1, siehe [[2026-09-08-ideal-code-review-nachlese]]).

### 0.3 Harte Regeln
1. **Session-Start:** `tabs_context_mcp`, dann **einen neuen Tab** anlegen und nur darin arbeiten.
2. **Dieses Protokoll ist vollstaendig READ-ONLY.** Es aendert keine Auftragsdaten. Die einzigen
   erlaubten Schreibvorgaenge sind **Spaltenpraeferenzen** (Zahnrad) und **Filtereingaben** — beide
   nur fuer den Testbenutzer, beide am Ende zurueckzusetzen. Kein Freigeben, kein Fertigmelden,
   kein Anhaken von Pick-Zeilen.
3. **Keine nativen Dialoge ausloesen.** `Picking/Bom` nutzt `confirm()`. Vor dem ersten Klick in
   dieser View per `javascript_tool` absichern:
   `window.confirm = () => true; window.alert = () => {};`
   Geht das nicht → betroffenen Test als **MANUAL** eintragen.
4. **Nie den Master anfassen.** `/HierarchieUmstellung` hoechstens lesen. Der Flip ist ein
   Einwegtor (ADR 0012) und kein Test.
5. **Beweise je Test:** ein Screenshot `TS-70-<nr>.png`, Textbehauptungen ueber
   `read_page`/`get_page_text` belegen, danach `read_console_messages` mit
   `pattern: "error|Error|exception|500|503"` — Treffer im Ergebnis vermerken, auch wenn der Test
   sonst besteht.
6. **Bewertung:** `PASS` · `FAIL` (mit Beobachtung, nicht nur „geht nicht") · `BLOCKED`
   (Vorbedingung fehlt) · `MANUAL` (nicht per Browser pruefbar).
7. **Nicht interpretieren, sondern berichten.** Wo dieses Protokoll eine Entscheidung des
   Fachbereichs vorsieht (B-0), wird beobachtet und dokumentiert — **nicht** bewertet.

---

## 1. Block A — Vorbedingungen

### A-1 Stand und Modus
1. `{{BASE_URL}}/Help/Changelog` oeffnen.
2. Erwartet: Eintrag **v1.36.0** vorhanden (BOM-Bridge). Fehlt er → **alles Weitere BLOCKED**, der
   alte Stand laeuft.
3. `{{BASE_URL}}/ProductionOrders` oeffnen: Liste ist nach HauptFA **gruppiert** (Gruppen-Kopfzeilen),
   also hierarchischer Modus aktiv. Screenshot `TS-70-A1.png`.

---

## 2. Block B — die Spec (TS-70)

### B-0 ZUERST — Scope-Entscheidung Vollstruktur (Ruling 4) — BEOBACHTEN, nicht bewerten
**Warum zuerst:** Das einzige Ruling mit Folgen in der Halle. „HauptFA → komplette Struktur" gilt
bewusst auch fuer die read-only Stueckliste in FA-Abarbeitungsliste und FA-Vervollstaendigung. Wer
dort Material fuer eine HauptFA holt, sieht **alle Teile aller Ebenen** statt nur der obersten.
Richtig, wenn am HauptFA das ganze Geraet kommissioniert wird; falsch, wenn nur die oberste Ebene
gemeint ist.

> **Am Bildschirm entscheiden, nicht am Ausdruck.** Dem Ausdruck fehlen „Ebene" und „Komm.-Ziel"
> (eigene Whitelist in `PrintBom.cshtml`). Genau diese beiden Spalten braucht man fuer die Frage.
> Ein Ausdruck ohne sie zeigt eine lange flache Teileliste ohne erkennbare Tiefe und verleitet zu
> einem Rueckbau **aus dem falschen Grund**.

1. `{{BASE_URL}}/FaWorklist` oeffnen, zur HauptFA-Zeile `{{HAUPTFA}}` die Stueckliste oeffnen.
   Dasselbe ueber `{{BASE_URL}}/FaCompletion`.
2. Zahnrad: Spalten **„Ebene"** und **„Komm.-Ziel"** einblenden, falls nicht sichtbar.
3. Beobachten und festhalten: Wie viele Zeilen, wie viele Ebenen, wie tief? Screenshot
   `TS-70-B0.png`, zusaetzlich `get_page_text` der Tabelle in den Bericht.
4. **Der Agent faellt hier kein Urteil.** Ergebnis lautet immer `MANUAL — Entscheidung Fachbereich`,
   mit der Beobachtung aus Schritt 3 als Entscheidungsgrundlage.
5. Der Mensch traegt die Entscheidung in
   [[2026-09-08-bom-schnittstellen-bridge-hierarchisch-umsetzung]] ein. **Sie steuert B-10.**

### B-1 HauptFA-Vollansicht (TS-70.1, TS-70.9)
1. `{{BASE_URL}}/Picking/Bom/{{ORDER_ID_HAUPT}}`.
2. Erwartet:
   - Kopfzeile „Stueckliste - FA `{{HAUPTFA}}`" **ohne** zusaetzliches „| HauptFA" (Kopf und Sub-FA
     sind hier identisch).
   - Badge **„FA-Struktur"** sichtbar (Quelle = FA-Hierarchie, nicht SAGE/CACHE).
   - **Alle Ebenen** in einer flachen Tabelle mit Pfad-Positionen der Form `3`, `3.7`, `3.7.2`.
   - Baugruppen-Zeilen mit Chevron, auf- und zuklappbar; Zuklappen blendet die Kinder aus.
   - Spalten `Komm.-Ziel`, `Hauptlagerplatz`, `Ebene`, `Vater-Sub-FA` vorhanden.
   - **Mengen = Sollmenge**, also **keine** Multiplikation mit der Auftragsmenge. Gegenprobe:
     eine Zeile mit Sollmenge 1 bleibt 1, auch wenn die Auftragsmenge groesser ist.
3. Jeder Knoten erscheint **genau einmal**. Stichprobe: eine Position aus zwei verschiedenen
   Baugruppen (`3.2` und `7.2`) existiert doppelt als Zeile, aber mit **verschiedenen** Pfaden.
4. Screenshot `TS-70-B1.png`.

### B-2 Sub-FA-Ansicht (TS-70.2)
1. `{{BASE_URL}}/Picking/Bom/{{ORDER_ID_SUB}}`.
2. Erwartet: **nur die direkten Kinder** von `{{SUBFA}}`, keine Enkel. Kopfzeile
   „Stueckliste - FA `{{SUBFA}}` | HauptFA `{{HAUPTFA}}`".
3. Gegenprobe: eine Enkel-Position, die in B-1 sichtbar war, fehlt hier.
4. Screenshot `TS-70-B2.png`.

### B-3 Sage-Position sichtbar (TS-70.9, Freigabe-Bedingung a)
1. In B-1: mit `read_page` das `title`-Attribut einer Pos.-Zelle lesen → Text
   **„Sage-Position: n"**.
2. Zahnrad → Spalte **„Sage-Pos."** einblenden (standardmaessig ausgeblendet). Erwartet: Spalte
   erscheint, zeigt die **Original**-Position, waehrend die Pos.-Spalte weiter den Pfad zeigt.
3. Spalte wieder ausblenden (Aufraeumen).

### B-4 Komm.-Ziel-Filter (TS-70.3)
1. In B-1 den Spaltenfilter `Komm.-Ziel` auf `{{KOMM_ZIEL}}` setzen.
2. Erwartet: nur passende Zeilen; die Ansicht ist erkennbar gefiltert.
3. Filter leeren → **alle** Zeilen zurueck, kein vorab gefilterter Restzustand.
4. Nicht vorkommenden Wert eingeben → leere, erkennbar gefilterte Ansicht **ohne** Fehler; Konsole
   sauber.

### B-5 Spaltenpraeferenzen speichern wirklich (TS-70.1 Randbedingung)
**Warum eigener Test:** In Lauf 1 kam der Speicher-`PUT` mit **HTTP 503** zurueck. Ein
fehlgeschlagener Speichervorgang heisst fuer den Anwender „meine Spaltenauswahl ist beim naechsten
Aufruf weg" — deshalb wird hier die **Persistenz** geprueft, nicht die Optik.
1. `read_network_requests` aktivieren, Filter `user-view-preferences`.
2. In B-1 eine nicht gesperrte Spalte ausblenden, dann **3 Sekunden warten** (das Speichern ist um
   1,5 s entprellt — ohne Wartezeit geht gar kein `PUT` raus und „kein Fehler" waere ein
   Fehlschluss).
3. Erwartet: genau ein `PUT` auf `/api/user-view-preferences/Bom` mit **Status 200**. Jeder andere
   Status = **FAIL**, Status und Uhrzeit notieren.
4. Seite neu laden → Spalte ist weiterhin ausgeblendet.
5. Zweite Aenderungsart: eine Spalte per Ziehen schmaler machen, 3 s warten, neu laden → Breite
   erhalten.
6. **Zwei-Tab-Gegenprobe:** dieselbe Seite in einem zweiten Tab oeffnen, in beiden kurz
   nacheinander eine Spalte umschalten, beide neu laden. Erwartet: beide Aenderungen ueberleben,
   kein `500`/`503`. Ein Fehler hier deutet auf einen Wettlauf zweier gleichzeitiger
   Speicheraufrufe, **nicht** auf IIS.
7. Aufraeumen: alle Spalten wieder in den Ausgangszustand.
8. **Bei Fehlschlag** diese drei Belege anfordern (nur der Mensch kommt heran): Serilog-Zeile des
   Requests, IIS-Logzeile mit `sc-status` **und** `sc-substatus`, Windows-Ereignisanzeige zu
   `IIS-W3SVC-WP` im selben Zeitfenster. Im Anwendungscode gibt es kein 503 — die Belege
   entscheiden, ob der Fehler aus IIS oder aus der Anwendung kommt.

### B-6 Artikelinfo HauptFA/Sub-FA (TS-70.4)
1. `{{BASE_URL}}/Articles/Info?articleNumber={{ARTNR_BAUTEIL}}` (Weg ueber die Artikel-Suche, falls
   der Parameter abweicht).
2. Erwartet: Tabelle mit **HauptFA** als primaerer, fett gesetzter Geraete-Spalte und
   **„Sub-FA (Baugruppe)"** als Zusatzspalte. Fusszeile weist auf die FA-Struktur hin und darauf,
   dass **Mengen = Sollmenge** sind.
3. Gegenprobe: die angezeigte Menge ist **nicht** mit der Auftragsmenge multipliziert.
4. Screenshot `TS-70-B6.png`.

### B-7 Menue „Kommissionierung" (TS-70.5)
1. Als Benutzer mit **beiden** Rechten (Picking + Lager): Menuepunkt „Kommissionierung" ist ein
   **Dropdown** mit „Kommissionierung" (Picking-Workflow) und „Kommissionierlisten".
2. Beide Einstiege sind am selben HauptFA unabhaengig bedienbar — der eine ersetzt den anderen nicht.
3. Nur-Lager-Benutzer sehen **kein** Dropdown, sondern einen Einzel-Link → als **MANUAL**
   vermerken, wenn kein solcher Benutzer verfuegbar ist.

### B-8 Guard vollstaendig ersetzt (TS-70.8)
1. `{{BASE_URL}}/Picking/Bom/{{ORDER_ID_SUB}}`.
2. Erwartet: **echte** Stueckliste. Der alte Hinweistext aus TS-68 („im hierarchischen Modus nicht
   verfuegbar") darf **nirgends** mehr erscheinen.
3. Konsole pruefen: der in Lauf 1 gemeldete `TypeError` der alten Hinweisseite ist weg.

### B-9 Read-only-Stueckliste in beiden Modulen
1. Aus `{{BASE_URL}}/FaWorklist` und `{{BASE_URL}}/FaCompletion` je die Stueckliste zu `{{SUBFA}}`
   oeffnen.
2. Erwartet: identischer Inhalt zu B-2 (beide laufen ueber denselben Builder), keine Pick-Steuerung,
   keine Bedarfsmeldung.

### B-10 Druck (TS-70.11) — Bewertung haengt an B-0
1. In B-1 „Stueckliste drucken" (`{{BASE_URL}}/Picking/PrintBom/{{ORDER_ID_HAUPT}}`).
2. Beobachtung: Vollstruktur mit Pfad-Positionen, **ohne** die Spalten Komm.-Ziel,
   Hauptlagerplatz, Sage-Pos., Ebene, Vater-Sub-FA. Screenshot `TS-70-B10.png`.
3. **Fall A — B-0 endete mit „Rueckbau auf direkte Kinder":** die Luecke ist ein bewusster Zustand,
   der Ausdruck zeigt dann eine flache Liste der obersten Ebene → **PASS**.
4. **Fall B — B-0 endete mit „Vollstruktur bleibt":** die Luecke ist **ein Mangel**. Ein
   mehrstufiger Ausdruck ohne „Ebene" und „Komm.-Ziel" ist in der Halle nicht benutzbar → **FAIL**,
   Whitelist in `PrintBom.cshtml` **vor dem Merge** ergaenzen.
5. Steht die Entscheidung aus B-0 noch aus → **BLOCKED**, mit der Beobachtung aus Schritt 2.

### B-11 Positions-Kollision (TS-70.10, Freigabe-Bedingung b)
1. Ohne `{{KOLLISION_FA}}` → **BLOCKED**, nicht improvisieren.
2. `{{BASE_URL}}/Picking/Bom/{{KOLLISION_FA}}`.
3. Erwartet: **beide** Geschwister sind sichtbar (kein stiller Verlust), die zweite Zeile traegt den
   Pfad-Suffix `~2` und ein rotes Kennzeichen **„Kollision"**.
4. Falls im selben Auftrag vorhanden: Blatt-Waisen tragen einen `W<n>`-Pfad und das Kennzeichen
   **„Waise"** (gelb).

---

## 3. Block C — nicht per Browser pruefbar (Mensch)

| Punkt | Warum nicht im Browser | Zustaendig |
|---|---|---|
| **TS-70.6 Flachmodus bit-identisch** auf `https://akenet01.ake.at:4444`, Master bleibt **aus** | Zweites System; **braucht zuerst den Deploy des Buendel-Stands dorthin** — in Lauf 1 lief dort der alte Stand | Mensch (ausdruecklich selbst) |
| **TS-70.7 Klasse-D-Skip** im Aktivitaets-Protokoll: Coating-, WorkStep- und BOM-Cache-Sync erscheinen als bewusster Skip, nicht als Fehler | Braucht einen abgewarteten Sync-Zyklus; der vierte Pfad (`SyncSpecificArticleNumbersAsync`) loggt nur nach Serilog | Mensch/OPS, Browser nur fuer die drei sichtbaren Laeufe |
| **DI-Aufloesungsbeweis** beim App-Start mit Master `true` | Startvorgang, kein UI | Mensch/OPS |
| **Scope-Entscheidung aus B-0** | Fachliche Entscheidung, kein Testergebnis | Fachbereich |

Der Agent darf die drei sichtbaren Laeufe aus TS-70.7 im Aktivitaets-Protokoll pruefen und als
Teilergebnis melden — die Serilog-Zeile bleibt beim Menschen.

---

## 4. Ergebnistabelle (vom Agenten fortschreiben)

| Test | Ergebnis | Beobachtung | Screenshot |
|---|---|---|---|
| A-1 | | | |
| B-0 | MANUAL | Entscheidungsgrundlage: … | |
| B-1 | | | |
| B-2 | | | |
| B-3 | | | |
| B-4 | | | |
| B-5 | | PUT-Status: … | |
| B-6 | | | |
| B-7 | | | |
| B-8 | | | |
| B-9 | | | |
| B-10 | | Fall A/B/offen: … | |
| B-11 | | | |

## 5. Abschlussbericht — Form

1. Zaehlung `PASS / FAIL / BLOCKED / MANUAL`.
2. Je `FAIL`: erwartetes gegen beobachtetes Verhalten, betroffene URL, Konsolenausgabe.
3. Je `BLOCKED`: welcher Platzhalter oder welche Vorbedingung fehlte.
4. Gesamturteil **nur** zu den drei Fragen, die diese Spec entscheidet:
   - Liefert die Stueckliste im hierarchischen Modus die richtigen Zeilen und Mengen (B-1, B-2, B-6)?
   - Bleiben die Zeilen ueber Kollisionen und Waisen hinweg unterscheidbar (B-11)?
   - Ist die Scope-Frage aus B-0 entschieden, und passt der Druck dazu (B-10)?
5. **Keine Empfehlung zum Merge.** Schranke 2 ist eine Entscheidung des Menschen.
