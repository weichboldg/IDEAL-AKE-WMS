---
typ: uat-ergebnis
lauf: 2026-09-08
protokoll: "[[2026-09-08-uat-protokoll-ideal-buendel-chrome]]"
basis_url: "http://idealweb01.ideal.ideal-ake.at:88 (IDEAL-Testsystem, Master an)"
benutzer: admin (Windows-Auth)
ausfuehrung: Claude in Chrome
ergebnis: "PASS 32 (5 mit *) · FAIL 1 · BLOCKED 16 · MANUAL 4"
empfehlung: "Schranke 2 NOCH NICHT — Nachlauf nach Konfig-Fix + Testdaten"
---
# UAT IDEAL-Buendel — Lauf 1 vom 08.09.2026 (mit Triage)

Ausgefuehrt nach [[2026-09-08-uat-protokoll-ideal-buendel-chrome]] durch Claude in Chrome.
Triage am Code (Worktree `feature/2026-08-07-ideal-teile-1-5`) am selben Tag. Mutierende Schritte
(F-2, I-13/4, J-1…J-3) wurden **nicht** ausgefuehrt (keine Freigabe).

## Kurzfazit

Die **Listen- und Strukturfunktionen** (Bloecke B–E, Kern von I) sind stabil: 32 PASS, kein
Funktionsfehler. Ein **FAIL ist Konfiguration** (A-3), zwei Auffaelligkeiten sind **Kleinbugs**
(H-2), der Rest ist Konfiguration oder Testdaten. Ein **struktureller Befund** kam neu dazu: Ohne
Arbeitsgaenge (IDEAL hat kein OSEON) sind Teil 8 Normal-Modus und Teileverfolgung nicht abnehmbar.

## Verwendete Platzhalter (vom Agenten aus dem System ermittelt)

| Platzhalter | Wert |
|---|---|
| `{{HAUPTFA}}` | 1035235 (KLUMAIER X TANNER, 167 Knoten, Tiefe 0–4) |
| `{{SUBFA}}` / `{{ENKEL_SUBFA}}` | 1043421 (Ebene 1) / 1043422 (Ebene 2, Eltern 1043421) |
| `{{ORDER_ID_HIER}}` | 101 (= Sub-FA 1043421) |
| `{{KOMM_ZIEL}}` / `{{VMBEDARF_REITER}}` | PG / DI (Abgleich), PG (Reiterwechsel) |
| nicht vorgegeben → BLOCKED | `SUBFA_MISSING`, `HAUPTFA_KASKADE`, `FA_OFFENE_BUCHUNG`, `WERKBANK`, `FA_MEHRFACH`, `FA_FREMD`, `BASE_URL_AKE`, `ORDER_ID_AKE` |

## Ergebnistabelle (Original des Agenten, gekuerzt um Screenshot-IDs)

Konsolen-Treffer stammten — sofern nicht anders vermerkt — von der Browser-Erweiterung KeePassXC
bzw. Extension-Messaging („ext"), **kein** App-Fehler.

| Test | Ergebnis | Beobachtung |
|---|---|---|
| A-1 | PASS | v1.35.0 (07.09.2026), v1.34…v1.31 darunter |
| A-2 | PASS | „Hierarchisch aktiviert", „gesperrt — hierarchische Daten vorhanden", Runbook-Verweis, kein Flip-Bedienelement |
| A-3 | **FAIL** | `Sync:ProductionOrdersEnabled = true` (erwartet false). Toggles/Master sonst korrekt |
| A-4 | PASS | Gruppe 1035235 mit 39 Sub-FAs inkl. 1043421/1043422. `?search=` wirkt nicht — nur Filterkarte (POST) |
| B-1 | PASS | 1 Tabelle, tbody je Struktur, nur Strukturspalte rueckt ein (6/24/42/60/78 px). Baum laedt zugeklappt |
| B-2 | PASS | Chevron/Kopfzeile/„Alle" wirken, Tastatur ok. Beobachtung: nach Kopfzeile zu→auf steht Baum wieder auf Wurzel-only |
| B-3 | PASS | Icons/Farben/`title`, Legende `<details>` |
| B-4 | PASS | Artnr 50001546 → 1 Treffer, Pfad gedimmt, „1 von 167"; Nicht-Treffer → tbody weg |
| B-5 | PASS | Arbeitsbereich S-01 → 5 von 167; Hervorheben (`fa-hl`) blendet nichts aus. Vorbehalt: Amber ueberdeckt Hellblau auf Trefferzeilen |
| B-6 | PASS | Persistenz ok, Locks ok, keine Sortierung. **PUT `/api/user-view-preferences/…` → HTTP 503, Wert trotzdem gespeichert** |
| B-7 | MANUAL | `resize_window` von der Umgebung nicht umgesetzt |
| C-1 | PASS | `?target=PG`: 3 Gruppen/15 Positionen, mehrere Ebenen, kein Banner, Footer zaehlt Gruppen |
| C-2 | PASS | Summiert; ohne KW bewusst leer (Hinweis); KW 2026-W04 → Summen identisch zu C-1 |
| C-3 | PASS | 3 Dokumente je HauptFA, Barcode, Mengen = C-1 |
| D-1 | PASS | 2 Gruppen, Kopf mit Dienstleister/RAL/Terminen, alle Ebenen |
| D-2 | PASS | `colf_matchcode` → Gruppe ohne Treffer verschwindet; Footer zaehlt Gruppen. Paging nicht pruefbar (2 Gruppen) |
| D-3 | PASS | Druck mit Filter exakt 2 Zeilen; ohne Treffer Hinweistext |
| D-4 | PASS | Persistenz, Locks, Std-Sortierung, Sortierung je Gruppe |
| E-1 | PASS | Reiter DI/ISO/LADE/LB/PG/SW/TB/VM; Teilmengen je Reiter |
| E-2 | PASS | Summiert DI: 4 Matchcodes, Handabgleich identisch — **keine Doppelzaehlung** |
| E-3 | PASS | KW 2026-W05 reduziert korrekt |
| E-4 | PASS | Locks + Std-Sortierung beide Sichten, Persistenz |
| F-1 | PASS | Gruppen, Master nur Badges + Link, kein Eingabefeld |
| F-2 / F-3 | MANUAL | mutierend / Fremdbenutzer |
| G-1 | PASS | wie A-2; Master in ServiceSettings ohne Schreib-Bedienelement |
| H-1 | PASS* | `/Picking` leer → Guard ueber Stuecklisten-Button in `/ProductionOrders`: Hinweisseite, Badge, Link, kein 500 |
| H-2 | PASS | Hinweisseite ok. **Konsole (App): `TypeError: Cannot read properties of null (reading 'addEventListener')`**; Kopfzeile zeigt HauptFA-Nr. statt Sub-FA |
| H-3 | PASS* | PrintBom ohne Exception, „0 Positionen". FaWorklist-Teil BLOCKED (Zugriff verweigert fuer admin) |
| I-1 | PASS* | ProductionOrders + PickingLeitstand gruppiert, „4 Auftraege · 130 Sub-FAs". BLOCKED: Picking (leer), FaCompletion/FaWorklist (Zugriff verweigert), Tracking (keine AGs) |
| I-2 | PASS | Enkel 1043422 als eigene Zeile |
| I-3 | PASS | Chevron nur eine Gruppe, Reload alle offen |
| I-4 | PASS | Gruppe zu → Filter 1043421 → Reload offen, Zeile sichtbar (keine Hervorhebung = Befund F2). Negativ klar „keine gefunden" |
| I-5 | PASS | Sub-vor-Haupt ueber Filterkarte und `colf_order-number` |
| I-6 | BLOCKED | nur 4 Gruppen, min. Seitengroesse 25 |
| I-7 | PASS | FA-Nr = SubOrderNumber, Elternspalte stimmt gegen `/FaHierarchy` |
| I-8 | BLOCKED | kein `SUBFA_MISSING` |
| I-9 | PASS | „Alle sichtbaren" 130 → Gruppe zu → 91 (−39); nicht freigegeben |
| I-10 / I-11 / I-12 | BLOCKED | Picking leer / FaWorklist Zugriff / keine Arbeitsgaenge |
| I-13 | PASS* / MANUAL | Bootstrap-Modal: „HauptFA 1035235 — 39 Sub-FA(s) … keine Gruppen-Ruecknahme". Zaehler „mit offener Buchung" nicht sichtbar |
| I-14 | BLOCKED | Fertigungsartikel nicht im Artikelstamm; kein „offene FAs"-Abschnitt |
| I-15 | PASS | Klick „Artikelnummer": **jede** Gruppe sortiert |
| J-1 … J-6 | BLOCKED | Werkbank/FA-Platzhalter fehlen; keine Arbeitsgaenge |
| K-1 … K-4 | BLOCKED | `BASE_URL_AKE` fehlt |

## Triage (am Code verifiziert)

| # | Befund | Urteil | Beleg | Aktion |
|---|---|---|---|---|
| T1 | **A-3** `Sync:ProductionOrdersEnabled=true` | **Konfiguration, hohes Risiko** — der AKE-FA-Sync laeuft parallel zur Materialisierung; sein UPDATE ist OrderNumber-gekoppelt (Fund **H1** in [[2026-09-08-ideal-code-review-nachlese]]) | `SageProductionOrderSql.cs:23-34`, `SyncWorker.cs:43` | **OPS sofort:** auf `false` setzen. **Dev:** H1-Code-Guard vor Produktiv-Deploy |
| T2 | **H-2** JS-TypeError auf der Guard-Hinweisseite | **Kleinbug** — Inline-Script greift ungeschuetzt auf `btnExpandAll`/`btnCollapseAll`/`btnPrintBom` zu, die im Hinweis-Zweig nicht gerendert werden | `Views/Picking/Bom.cshtml:41` (Zweig `HierarchicalUnavailable`), `:916`, `:927`, `:972` | Dev (klein): Script hinter den Zweig oder Null-Guards; dazu Kopfzeile auf SubOrderNumber (Z4-Konsistenz). Re-QA |
| T3 | **B-6** PUT 503, trotzdem persistiert | **Nicht aus der App** — `UserViewPreferencesApiController` kennt kein 503 | `Controllers/Api/UserViewPreferencesApiController.cs:39` | OPS: IIS/App-Pool/Proxy auf Port 88 pruefen (Request-Queue?) |
| T4 | FaWorklist/FaCompletion „Zugriff verweigert" fuer admin | **Konfiguration** — beide Module haengen am AppSetting `FaCompletionAktiv`; Admin-Wildcard intakt | `FaWorklistController.cs:82-86`, `RequireFaCompletionAccessAttribute.cs:27-28`, `CurrentUserService.cs:134` | OPS: `FaCompletionAktiv=true` am Testsystem → I-1/I-11/H-3 nachtestbar. Design-Notiz: Toggle aus → AccessDenied statt Home+Warning (vorbestehend, weicht von der Konvention ab) |
| T5 | **I-13** Zaehler „mit offener Buchung" fehlt | **Kein Bug** — wird nur bei `> 0` eingeblendet; es gab 0 offene Buchungen | `Views/PickingLeitstand/Index.cshtml:366, :461-462` | Dev (UX, klein): immer anzeigen („davon 0 …"), damit die Abnahme entscheidbar ist |
| T6 | **I-14** Artikelinfo ohne „offene FAs", Fertigungsartikel fehlen | **Testdaten + bekannte Luecke** — der Abschnitt haengt am BOM-Cache (bei IDEAL leer) | `ArticlesController.cs:259/285` | Genau der Umfang von [[2026-09-08-bom-schnittstellen-bridge-hierarchisch]] (Artikelinfo, Klasse P). OPS: Artikel-Sync-Quelle am IDEAL pruefen |
| T7 | Tracking leer, Block J BLOCKED | **Struktureller Befund** — `WorkOperations` entstehen nur per OSEON-Sync (AKE) oder `FindOrCreateDefaultAsync` beim NurFA-Start; IDEAL hat kein OSEON | `BdeDefaultWorkOperationService.cs:19-44`, `BdeTerminalController.cs:65` | **Konsequenz:** [[2026-09-08-arbeitsgaenge-aus-arbeitsschritte]] ist **Voraussetzung** fuer die Abnahme von Teil 8 Normal-Modus, Teileverfolgung und FaWorklist-AG-Spalten bei IDEAL — nicht Nice-to-have. Prioritaet hoch. Fuer den Nachlauf: **NurFA-Modus (J-2/J-3) ist ohne AGs testbar** (legt Default-AG an), Normal-Modus (J-1) nicht |
| T8 | B-2 Baumzustand nach Kopfzeile, B-5 Amber ueber Hellblau, A-4 `?search=` | **UX / Protokollfehler** | — | B-2/B-5 vermerken; Protokoll: Filterkarte statt Query-Parameter |
| T9 | I-6, I-8, K | **Testdaten / Umgebung** | — | siehe Nachlauf |

## Empfehlung

**Schranke 2 noch nicht nehmen** — Zustimmung zum Agenten-Urteil, aber die Gruende sind praeziser:
1. **T1** ist eine harte Deploy-Vorbedingung und muss vor jeder weiteren Abnahme stehen.
2. Die **Rueckmeldefaehigkeit** (Block J, I-12, I-13 mit offener Buchung) ist mangels
   Arbeitsgaengen ungeprueft — und **T7** zeigt, dass sie bei IDEAL ohne den Arbeitsgaenge-Block
   im Normal-Modus gar nicht erreichbar ist.
3. Block K (AKE-Regression) ist ungeprueft.

Die abgenommenen Bloecke B–E und der Kern von I brauchen **keinen** Nachtest.

## Nachlauf — was vorher passieren muss (ca. 30–45 min Lauf)

**OPS (Testsystem):**
- [ ] `Sync:ProductionOrdersEnabled = false` (T1)
- [ ] `FaCompletionAktiv = true` (T4)
- [ ] 503 auf `/api/user-view-preferences` klaeren (T3)
- [ ] **Buendel-Stand auf das AKE-Testsystem `https://akenet01.ake.at:4444` publizieren** (Block K /
      TS-70.6). **Korrektur 2026-09-09:** Die Instanz hat nie gefehlt — es fehlte der **Deploy**
      dorthin; publiziert war der alte Stand. Master bleibt dort auf `false`. `ORDER_ID_AKE` (ein
      AKE-Auftrag mit Stueckliste) danach benennen.

**Testdaten:**
- [ ] `SageMissingSince` an einem Sub-FA setzen (I-8)
- [ ] Im Leitstand ein Testobjekt (`HAUPTFA_KASKADE`) freigeben → Picking nicht mehr leer (I-10, H-1 Originalpfad)
- [ ] An `WERKBANK` per **NurFA-Start** eine Buchung erzeugen → `FA_OFFENE_BUCHUNG` + Default-AG (I-13 Zaehler, I-12 teilweise, J-2/J-3)
- [ ] `FA_FREMD` benennen (J-4)

**Dev (klein, vor dem Merge, Re-QA des Buendels):**
- [ ] T2 JS-Guard + Kopfzeile in `Bom.cshtml`
- [ ] T5 Zaehler immer anzeigen
- [ ] optional Befund F2 (toter Auto-Expand-Zweig) im selben Nachtrag

**Strategisch:** [[2026-09-08-arbeitsgaenge-aus-arbeitsschritte]] von „Folgeblock" auf
**Voraussetzung fuer Teil-8-Abnahme** hochstufen.

---

## Nachtrag 2026-09-09 — zwei Triage-Punkte am Code nachgezogen

### T1 vertieft: `Sync:ProductionOrdersEnabled` haengt sehr wohl an der Materialisierung
Auftrag des Menschen: „beim Setzen kurz gegenpruefen, ob der Schalter mit der FA-Materialisierung
zusammenhaengt — falls ja, erklaert er moeglicherweise mehr als nur eine leere Liste." **Er tut es,
und er erklaert mehr.** Beide Syncs schreiben in **dieselbe** Tabelle `ProductionOrders`, im selben
Zyklus, in dieser Reihenfolge (`SyncWorker.cs:43` vor `:357`):

1. **AKE-FA-Sync** liest `vw_AKE_Kommissionierung_WAListe` und schreibt pro Sage-Zeile
   `UPDATE … WHERE [OrderNumber] = @OrderNumber` (`SageProductionOrderSql.cs:23-45`). Nach der
   Inversion ist `OrderNumber` **nicht mehr eindeutig**: HauptFA und alle ihre Sub-FAs teilen sie.
   Ein einziger Sage-Satz trifft damit **die ganze Gruppe** und setzt dort Menge, Kunde,
   Artikelnummer, Bezeichnungen, Fertigungs- und Liefertermin auf die Werte des **Kopfes**.
2. **FA-Materialisierung** laeuft danach und schreibt fuer bestehende Zeilen Menge, Artikelnummer
   und Bezeichnungen aus dem Struktur-Knoten **zurueck** (`FaMaterializationSyncService.cs:142-150`).

Daraus folgt dreierlei, das ueber „leere Liste" hinausgeht:

- **Selbstheilung nur teilweise.** Die vier Felder, die die Materialisierung besitzt, werden im
  selben Zyklus reparariert. **Kunde, Fertigungstermin und Liefertermin nicht** — die schreibt die
  Materialisierung gar nicht (sie kennt nur 7 Felder). Auf allen Sub-FAs stehen sie dauerhaft mit
  den **Kopf**-Werten. Das ist die stillste Form des Fehlers: fachlich falsch, aber plausibel
  aussehend.
- **Der Schalter erklaert, warum in der FA-Liste ueberhaupt Kunde und Termine stehen.** Genau die
  Luecke, die [[2026-08-20-materialisierung-fachliche-felder-spec]] schliessen soll, wird derzeit
  durch diesen Fehler **verdeckt** zugeschuettet. Wer den Schalter auf `false` setzt, muss damit
  rechnen, dass Kunde/Termine aus der Liste **verschwinden** — das ist dann kein neuer Fehler,
  sondern der ehrliche Zustand. Die Materialisierungs-Spec ist die eigentliche Loesung.
- **Dauerhafter Schreib-Ping-Pong.** Weil die Materialisierung die Werte jeden Zyklus zurueckdreht,
  greift die Aenderungs-Bedingung des UPDATE (`WHERE … AND (Quantity != … OR …)`) **immer** wieder:
  jeder Sub-FA-Satz wird alle 15 Minuten neu geschrieben, `ModifiedAt`/`ModifiedBy` wandern mit.
  Dazu ein Zeitfenster **innerhalb** jedes Zyklus, in dem die Liste die Kopfwerte auf allen Sub-FAs
  zeigt — wer in diesem Moment eine Seite oeffnet oder einen Ausdruck macht, sieht falsche Mengen.

**Bewertung:** Die Deploy-Vorbedingung `Sync:ProductionOrdersEnabled = false` bleibt richtig und
wird dringlicher. Sie ist aber weiterhin **nur Konfiguration** — ein Haken im falschen Feld
verursacht stille Datenverfaelschung. Der Code-Guard aus **H1** in
[[2026-09-08-ideal-code-review-nachlese]] ist damit von „vor Produktiv-Deploy" auf
**Merge-nah** zu heben.

Nicht betroffen: der BOM-Cache-/Lackier-Haken im selben Sync (`SageImportService.cs:187-203`) —
beide Ziele sind seit v1.36.0 ueber `IHierarchicalModeReader` gegatet und ueberspringen bei
Master `true`. Die FA-Reconciliation haengt an einem eigenen Schalter
(`Sync:ProductionOrderReconcileEnabled`, Default aus) und hat einen Leer-Read-Guard.

### T3 neu bewertet: der 503 ist ein funktionaler Ausfall, kein Betriebs-Nachtrag
Der Mensch hat die Einstufung „OPS" 2026-09-08 zurueckgewiesen — zu Recht in der Wirkung: Ein
fehlgeschlagener `PUT` auf `/api/user-view-preferences/{viewKey}` heisst fuer den Anwender
„meine Spaltenauswahl ist beim naechsten Aufruf weg", nicht „Serverfehler". Das Protokoll prueft
das jetzt funktional (**B-6** in [[2026-09-08-uat-protokoll-ideal-buendel-chrome]], inkl.
Netzwerkmitschnitt, Entprellung und Zwei-Tab-Gegenprobe).

**Was der Code hergibt** (offline verifizierbar, ohne Testsystem):
- Die Anwendung erzeugt **nirgends** einen 503 — weder Controller noch Middleware noch Filter
  (`UserViewPreferencesApiController` kennt nur `Ok`/`NoContent`/`BadRequest`/`Unauthorized`;
  Volltextsuche nach `503`/`ServiceUnavailable` im Web-Projekt ist leer). Ein 503 kann daher nur
  **vor** der Anwendung entstehen: IIS/ANCM, App-Pool-Recycling, Rapid-Fail-Protection oder eine
  volle Anfrage-Warteschlange (`web.config`: `hostingModel="inprocess"`).
- **Widerspruch im Lauf-1-Befund:** dort steht „Persistenz ok … Wert trotzdem gespeichert". Ein
  Request kann nicht gleichzeitig von IIS abgewiesen **und** von der Anwendung geschrieben worden
  sein. Wahrscheinlicher: das Speichern ist um 1,5 s entprellt (`column-preferences.js:20,290`),
  es gingen **mehrere** `PUT`s raus, einer davon fiel in ein Recycling-Fenster. Der Ausfall waere
  dann **sporadisch**, nicht total — das aendert die Dringlichkeit, nicht die Einstufung.
- **Eigenstaendiger Zweitverdacht im Code** (unabhaengig vom 503, deshalb Schritt B-6.6):
  `UserViewPreferenceRepository.SaveAsync` macht ein ungeschuetztes „lesen, sonst anlegen" gegen
  einen eindeutigen Index (`UQ_UserViewPreferences_User_View`). Zwei gleichzeitige Speicher-Aufrufe
  desselben Benutzers auf dieselbe Ansicht (zwei Tabs, oder Ziehen + Umschalten kurz nacheinander)
  koennen beide „nicht vorhanden" sehen und in eine Schluesselverletzung laufen — ein verlorener
  Speichervorgang mit exakt derselben Anwenderwahrnehmung. Das ist ein echter Code-Befund und
  gehoert in [[2026-09-08-ideal-code-review-nachlese]], falls B-6.6 ihn bestaetigt.

**Offen und nur am Testsystem entscheidbar:** ob der 503 aus IIS kommt. Die drei Belege dafuer
(Serilog-Zeile, IIS-`sc-status`/`sc-substatus`, Ereignisanzeige `IIS-W3SVC-WP`) stehen als
Fehlschlag-Anweisung in B-6. Ohne sie bleibt jede Ursachenaussage Spekulation.
