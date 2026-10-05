---
type: spec
title: "Stueckliste: Komm.-Ziel-Dropdown-Filter + gespeicherter Standardfilter (Badge fuer alle vier Standardfilter)"
slug: 2026-09-18-stueckliste-kommissionierziel-filter-spec
status: Testbereit
created: 2026-09-18
updated: 2026-09-21
source_backlog: "[[2026-09-18-stueckliste-kommissionierziel-filter]]"
task: "[[2026-09-21-stueckliste-kommissionierziel-filter-umsetzung]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - "IdealAkeWms/Models/User.cs (Z. 86-91, direkt nach DefaultFilterBomDescription1) — neues Feld DefaultFilterBomKommissionierziel, string?, [StringLength(200)], gleiche Mini-Syntax-Doku wie das Vorbildfeld"
  - "IdealAkeWms/Migrations/*_AddUserDefaultFilterBomKommissionierziel.cs (NEU, dotnet ef migrations add) + IdealAkeWms/Migrations/ApplicationDbContextModelSnapshot.cs (Snapshot-Update automatisch durch den EF-Migrationslauf)"
  - "SQL/92_AddUserDefaultFilterBomKommissionierziel.sql (NEU, exakte Vorlage SQL/87_AddUserDefaultFilterBomDescription1.sql: COL_LENGTH-Guard, ALTER TABLE Users ADD ... NVARCHAR(200) NULL, DDL-Batch + separater __EFMigrationsHistory-Insert-Batch)"
  - "SQL/00_FreshInstall.sql (zwei Stellen: Spalte Users.DefaultFilterBomKommissionierziel im Schema-Block + MigrationId-Zeile fuer die neue Migration)"
  - "IdealAkeWms/Models/ViewModels/UserEditViewModel.cs + IdealAkeWms/Models/ViewModels/ProfileViewModel.cs (neues Feld DefaultFilterBomKommissionierziel analog DefaultFilterBomDescription1)"
  - "IdealAkeWms/Views/Users/Edit.cshtml + IdealAkeWms/Views/Account/Profile.cshtml (neues Formularfeld direkt neben dem Bezeichnung-1-Standardfilter; als TEXTFELD mit OR-Syntax wie die Vorlage, ausdruecklich KEIN Dropdown - in den Einstellungen ist keine Stueckliste geladen; unter @if auf den Master-Schalter: NUR bei ProduktionsauftragHierarchisch = true sichtbar - bewusste Abweichung von der Vorlage, siehe FA 2)"
  - "IdealAkeWms/Controllers/UsersController.cs + IdealAkeWms/Controllers/AccountController.cs (Feld beim Lesen/Speichern durchreichen, Audit-Felder wie bestehend ueber ICurrentUserService; ZUSAETZLICH: Master-Schalter ProduktionsauftragHierarchisch lesen und ins ViewModel reichen - NEUE Verdrahtung, heute liest keiner der beiden Controller ihn; Muster wie PickingController)"
  - "IdealAkeWms/Controllers/PickingController.cs (Bom-Action, Z. ~303-305/452-454 — currentUser.DefaultFilterBomKommissionierziel analog defaultFilterBomDescription1 ins BomViewModel legen)"
  - "IdealAkeWms/Models/ViewModels/BomViewModels.cs (BomViewModel: neue Property DefaultFilterBomKommissionierziel)"
  - "IdealAkeWms/Views/Picking/Bom.cshtml (Z. 153 Filterzeilen-Input auf Dropdown/Select2 umstellen NUR im hierarchischen Modus; Z. 666 #column-config unveraendert — Spalte existiert bereits; Z. 985-1000 column-preferences-ready-Handler: defaultBomKommissionierziel lesen + window.setColumnFilter('kommissionieren', ...) analog Z. 998; neuer sichtbarer Aktiv-Badge + Ein-Klick-Reset FUER ALLE VIER still vorbelegten Standardfilter dieser Ansicht (Beschaffung, Artikelgruppe, Bezeichnung 1, Komm.-Ziel) - ein Bedienelement, per Konfiguration je Feld; benannter Leerzustand bei null Treffern; Auto-Aufklappen der Treffer-Aeste bei Filterauswahl, Muster wie FA-Liste)"
  - "IdealAkeWms/wwwroot/js/select2 (bereits geladen, Bom.cshtml Z. 683-686) — Dropdown-Aufbau fuer die Filterzeile der Spalte kommissionieren, gespeist aus den im BomViewModel bereits geladenen Items (.Kommissionieren distinct), kein neuer Serverzugriff"
  - "IdealAkeWms/Views/Picking/PrintBom.cshtml (VERBINDLICH, Q6 = ja - als LETZTER, abtrennbarer Block: neue ShowCol-Kopf-/Zellbloecke fuer kommissionieren, hauptlagerplatz und die Ebene-Spalte (Spaltenschluessel am Code pruefen) - die Druckvorlage kennt die hierarchischen Spalten strukturell noch nicht; Hinweis Gefilterte Ansicht im Kopf)"
  - "IdealAkeWms/Views/Picking/Bom.cshtml (VERBINDLICH: colNames-Map Z. 1015 um die hierarchischen Spalten erweitern, damit der Druck-Filterhinweis den Spaltennamen kennt)"
  - "docs/TESTSZENARIEN.md (Kapitel TS-70 — BOM-Bridge — um Abschnitt 'Komm.-Ziel-Filter' ergaenzen)"
  - "secondbrain/tests/testszenarien-index.md (TS-70-Eintrag nachziehen)"
open_questions: []
beantwortete_rueckfragen:
  - "Q2: Dropdown Einzel- oder Mehrfachauswahl (kommasepariert, OR-Semantik wie DefaultFilterBomDescription1/DefaultWorkbenches)?"
  - "Q3: Dropdown-Werte fest verdrahtet oder aus den geladenen BomViewModel-Items (.Kommissionieren distinct) abgeleitet?"
  - "Q4: Verhalten, wenn der gespeicherte Standardwert in der aktuellen Stueckliste nicht vorkommt (Filter ignorieren + Hinweis vs. leer filtern + Hinweis)?"
  - "Q6: Soll ein aktiver Komm.-Ziel-Filter in Druck/PDF (PrintBom) durchschlagen?"
  - "Q7-Scope: Badge + Ein-Klick-Reset fuer BEIDE Standardfilter-Felder (Bezeichnung 1 UND Komm.-Ziel) bauen, nicht nur fuer das neue Feld — Bestaetigung erbeten"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: true
freigabe_entscheidung: "Mehrfachauswahl kommasepariert; Dropdown nur in der Stueckliste (DISTINCT-Items), Textfeld in den Einstellungen; Filter anwenden + benannter Leerzustand; Druck verbindlich als letzter abtrennbarer Block; Badge fuer alle vier Standardfilter; Einstellungsfeld nur bei Master true"
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-09-21
---

## Ziel / Nutzen (das Warum)

[[2026-09-13-kommissionierung-nur-hauptfa]] macht die hierarchische Stueckliste eines HauptFA zur
**Vollstruktur** (Ruling 4 bleibt bestehen) — sie wird dadurch lang, weil jetzt alles, was frueher auf
mehrere Sub-FA-Stuecklisten verteilt war, auf einer Liste steht. Ein Kommissionierziel-Filter laesst
jeden Kommissionierer nur seinen Bereich sehen; ein **gespeicherter** Standardfilter erspart ihm, das
bei jedem Aufruf neu einzustellen. Das ist der natuerliche Begleiter dieser Aenderung, kein
eigenstaendiges Feature ohne Anlass.

**Wichtiger Umfangs-Korrektur gegenueber dem Backlog:** Am Code verifiziert (2026-09-21, Worktree
`feature/2026-08-07-ideal-teile-1-5`), dass Backlog-Punkt 1 ("Spalte + Zahnrad") **bereits vollstaendig
umgesetzt** ist (BOM-Bridge v1.36, Testbereit, noch nicht in `main` gemergt):

- `Views/Picking/Bom.cshtml` Z. 153: `<th data-filterable data-col-key="kommissionieren">Komm.-Ziel</th>`,
  Z. 248: `<td>@item.Kommissionieren</td>` — beide unter `@if (Model.Hierarchical)`.
- `#column-config`-JSON Z. 666: `{ "key": "kommissionieren", "label": "Komm.-Ziel", ... }`, ebenfalls
  unter `@if (Model.Hierarchical)`. Zusaetzlich in `ColumnDefinitions.cs` Z. 257
  (`new ColumnDef("kommissionieren", "Komm.-Ziel", Locked: false)`) serverseitig registriert.
- `<th>` und `#column-config` **sind in Sync** — kein Drift im Sinne von `fallstricke.md`
  ("`data-col-key` ist Pflicht auf allen `<th>`", "Neuer `viewKey` ohne Registrierung → Prefs-API 400").
  Die Spalte ist also bereits per Zahnrad ein-/ausblendbar.
- Die Spalte hat als `data-filterable`-Spalte heute bereits einen **Text**-Spaltenfilter
  (Client-Mode, `table-filter.js`), noch **kein** Dropdown.

Der tatsaechliche Umfang dieser Spec reduziert sich damit auf: (a) den bestehenden Text-Filter der
Spalte `kommissionieren` durch ein Dropdown ersetzen, (b) einen je Benutzer gespeicherten
Standardfilter dafuer, (c) die Sichtbarkeit eines aktiven Standardfilters (Hausregel „sichtbar machen
statt still filtern") — und zwar fuer **alle vier** still vorbelegten Standardfilter dieser Ansicht (siehe Fachliche
Anforderungen 4 und ANTWORTEN zu B1).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**

1. Der bestehende Text-Spaltenfilter der Spalte `kommissionieren` in `Bom.cshtml` wird durch ein
   Dropdown (Select2, bereits geladen) ersetzt — **nur im hierarchischen Modus** gerendert (Spalte
   existiert im AKE-Flachmodus gar nicht, siehe Fachliche Anforderung 3).
2. Neues Feld `User.DefaultFilterBomKommissionierziel` (Migration 92) nach exakter Vorlage
   `DefaultFilterBomDescription1`, inkl. Benutzereinstellungen (`Users/Edit`, `Account/Profile`) und
   Anwendung in `Bom.cshtml` (`window.setColumnFilter('kommissionieren', ...)`).
3. Sichtbarer Aktiv-Badge + Ein-Klick-Aufhebung fuer einen aktiven Standardfilter — **fuer alle vier**
   still vorbelegten Standardfilter dieser Ansicht (`DefaultFilterBeschaffung`,
   `DefaultFilterArtikelgruppe`, `DefaultFilterBomDescription1` und das neue
   `DefaultFilterBomKommissionierziel`). Die Luecke besteht fuer die drei bestehenden Felder schon
   heute (vorbestehender Mangel, siehe Fachliche Anforderung 4).
4. **Benannter Leerzustand**: Filter wird angewandt; trifft er keine einzige Position, erscheint
   ausdruecklich „Standardfilter Kommissionierziel `<Wert>` aktiv — keine Treffer in dieser
   Stueckliste" plus Ein-Klick-Reset (Freigabe-Antwort 3).
5. **Auto-Aufklappen der Treffer-Aeste** bei Filterauswahl — Muster der FA-Liste, damit ein Wert aus
   einem zugeklappten Ast nicht scheinbar „nichts" filtert (Hinweis H2).
6. **Einstellungsfeld nur bei Master `true`** in `Users/Edit` und `Account/Profile`, inkl. der dafuer
   noetigen Master-Verdrahtung in beiden Controllern (Freigabe-Antwort 6, S1).
7. **Druck/PDF-Durchschlag — VERBINDLICH, als letzter abtrennbarer Block** (Freigabe-Antwort 4):
   `PrintBom.cshtml` bekommt Kopf-/Zellbloecke fuer die hierarchischen Spalten, `colNames`-Map
   erweitert, Hinweis „Gefilterte Ansicht" im Kopf. Schliesst den offenen Druck-Befund der
   BOM-Bridge mit. **Erweist sich dieser Block als groesser als erwartet, darf der Dev-Lauf nach
   Punkt 1-6 anhalten und melden** — dann wird der Druck eine eigene Folge-Spec.
8. Testszenarien-Ergaenzung in TS-70 (BOM-Bridge).

**Out-of-Scope**

- Die Spalte selbst neu bauen oder ins Zahnrad aufnehmen — bereits vorhanden (siehe oben).
- Aenderungen an Spalte und Filterzeile im AKE-Flachmodus — sie existieren dort nicht und bleiben es
  (`@if (Model.Hierarchical)`-Gate unveraendert). **Nicht** out of scope ist dagegen das Ausblenden
  des neuen **Einstellungsfelds** bei Master `false` — das ist neu und gehoert zum Umfang (In-Scope 6).
- Ein Dropdown in den **Benutzereinstellungen** — dort bleibt es beim Textfeld mit OR-Syntax (keine
  Stueckliste geladen, keine Wertequelle). Falls spaeter gewuenscht: eigene Wertequelle, dann fuer alle
  Standardfilter-Felder zugleich.
- Neue Rolle, neues AppSetting/Feature-Toggle — nicht erforderlich, die Spalte haengt bereits am
  bestehenden `ProduktionsauftragHierarchisch`-Master und an den bestehenden Picking-Zugriffsfiltern.
- Arbeitsgaenge/Materialisierung/BDE — unberuehrt.

## Fachliche Anforderungen

1. **Dropdown statt Freitext auf der Spalte `kommissionieren`.** Der heutige Text-Input in der
   Filterzeile (Client-Mode, `table-filter.js`) wird fuer genau diese eine Spalte durch ein
   Select2-Dropdown ersetzt. Die anderen Spaltenfilter der Stueckliste (Baugruppe, Ressourcenummer,
   Bezeichnung 1/2, Beschaffung, Artikelgruppe, Kategorie, Hauptlagerplatz, Sage-Pos., Ebene,
   Vater-Sub-FA) bleiben **unveraendert** Freitext — nur die eine Spalte wird umgestellt, wie im
   Backlog ausdruecklich verlangt ("Werteauswahl statt Freitext" analog zur Kommissionierliste oben).
   **Mehrfachauswahl** (Select2 nativ), Werte per OR verknuepft. Die Optionen sind die `DISTINCT`-Werte
   der **geladenen** Positionen. Bei Auswahl werden die Aeste mit Treffern **automatisch aufgeklappt**
   (Muster FA-Liste) — sonst filtert ein Wert aus einem zugeklappten Ast scheinbar „nichts".

2. **Gespeicherter Standardfilter — 1:1-Kopie des bestehenden Musters.** Neues Feld
   `DefaultFilterBomKommissionierziel` (`string?`, `[StringLength(200)]`) an `User`, direkt neben
   `DefaultFilterBomDescription1`. Gleicher Weg: Benutzereinstellungen (`Users/Edit.cshtml`,
   `Account/Profile.cshtml`) → `PickingController.Bom` liest es aus `currentUser` → `BomViewModel`
   → `Bom.cshtml`-`column-preferences-ready`-Handler ruft
   `window.setColumnFilter('kommissionieren', defaultBomKommissionierziel)`, exakt wie
   `defaultBomDescription1` heute schon fuer `description1` tut (Z. 995/998). **Kein neuer
   Mechanismus.**
   **Zwei Orte, zwei Formen — ausdruecklich getrennt:** In der **Stueckliste** ist der Filter ein
   Dropdown (FA 1). In den **Benutzereinstellungen** ist das Feld ein **Textfeld mit OR-Syntax**, 1:1
   wie die Vorlage — **kein** Dropdown, denn dort ist keine Stueckliste geladen und es gaebe keine
   Werte.
   **Eine bewusste Abweichung von der Vorlage:** `DefaultFilterBomDescription1` ist hausweit sichtbar,
   das neue Feld erscheint **nur bei Master `true`** (Freigabe-Antwort 6) — weil AKE kein
   Kommissionierziel fuehrt. Dafuer lesen `UsersController` und `AccountController` den
   Master-Schalter neu ein. **Nicht „harmonisieren"** — der Unterschied folgt aus der Datenlage.

3. **AKE-Flachmodus bleibt unberuehrt — bereits durch das bestehende Gating sichergestellt.** Spalte
   und Filterzeile fuer `kommissionieren` rendern ausschliesslich unter `@if (Model.Hierarchical)`.
   Ein gespeicherter Default, der via `window.setColumnFilter('kommissionieren', …)` angewandt wird,
   findet im flachen AKE-Modus schlicht **keine** Filterzeile fuer diesen Key — der dokumentierte
   No-op-Pfad (`table-filter.js` ignoriert `setColumnFilter` auf einen nicht existierenden `col-key`,
   siehe Bom.cshtml Z. 980-982 zur allgemeinen Race-Condition-Behandlung) greift, **kein** Leerfiltern
   der AKE-Stueckliste. Diese Aussage ist eine verifizierte Design-Eigenschaft des bestehenden Codes,
   kein neu zu bauendes Verhalten — sie deckt die urspruengliche Backlog-Frage 5 ab, ohne
   Zusatzaufwand.

4. **Aktiver Standardfilter muss sichtbar sein — Pflicht fuer ALLE VIER Felder.** Hausregel „sichtbar
   machen statt still filtern" (F1 im Backlog). Heute werden `DefaultFilterBeschaffung`,
   `DefaultFilterArtikelgruppe` und `DefaultFilterBomDescription1` beim Laden nur in das jeweilige
   Filterzeilen-Input **vorbelegt** (Z. 993-998) — der Wert ist im Feld sichtbar, aber es gibt
   **keinen** dedizierten „gefiltert"-Hinweis und **keinen** Ein-Klick-Reset. Diese Spec schliesst die
   Luecke **fuer alle vier Felder gemeinsam** — Beschaffung, Artikelgruppe, Bezeichnung 1 und
   Kommissionierziel. Nur zwei davon sichtbar zu machen, verschoebe die Asymmetrie nur (ANTWORTEN zu
   B1). Ein Bedienelement, je Feld konfiguriert. Konkret: ein sichtbarer Hinweis/Badge oberhalb der Tabelle ("Gefiltert: Bezeichnung 1 =
   X" bzw. "Kommissionierziel = Y") mit einem Link/Button, der den jeweiligen Spaltenfilter per
   `window.setColumnFilter(key, '')` zuruecksetzt und `updateBomVisibility()` erneut auslaufen laesst
   (dieselbe Funktion, die der bestehende Wrapper Z. 972-975 schon aufruft).

5. **Druck (Q6 = ja, VERBINDLICH — als letzter, abtrennbarer Block).** Der Druck-Handler (Z. 1002-1039) uebergibt aktive Filter bereits als
   `filterInfo` an `PrintBom`, aber die `colNames`-Map (Z. 1015) kennt nur die Spalten 1-8 (bis
   Artikelgruppe) — verifiziert, die hierarchischen Spalten fehlen dort. `PrintBom.cshtml` selbst hat
   **keine feste Whitelist-Liste**, sondern eine generische `ShowCol(key)`-Pruefung (Z. 196) gegen
   `Model.VisibleColumns`; verifiziert, dass `kommissionieren`/`hauptlagerplatz` dort **nirgends** als
   Kopf-/Zellblock auftauchen (Z. 213-251 zeigen ausschliesslich die acht AKE-Basisspalten plus
   Lagerplatz) — die Druckvorlage kennt die hierarchischen Spalten strukturell noch nicht, unabhaengig
   vom `visibleColumns`-Parameter. Mit Q6 = ja braucht es verbindlich: (a) den Spaltennamen
   in `colNames` (Bom.cshtml Z. 1015), (b) neue `ShowCol("kommissionieren")`-Kopf-/Zellbloecke in
   `PrintBom.cshtml`, (c) denselben „gefilterte Ansicht"-Hinweis im Ausdruck, den `filterInfo` heute
   schon fuer die AKE-Spalten liefert — sonst haelt jemand eine gefilterte Papierliste fuer
   vollstaendig (dieselbe Gefahr wie F1).

## Technischer Loesungsentwurf

**Muster:** Listen-View-Pattern (ADR [[0005-listen-view-pattern-mit-server-side-spaltenfilter]]) —
`Bom.cshtml` ist die dokumentierte, bewusste **Client-Mode-Ausnahme** (unpaginierte, durch die
Baumstruktur bereits vorgefilterte Ansicht, siehe BOM-Bridge-Spec Design F: "ADR 0005 erlaubt
Client-Mode ausdruecklich fuer unpaginierte, vorgefilterte Ansichten wie diese"). Diese Spec bleibt
konsequent im Client-Mode: kein Umbau auf `data-server-column-filter`, kein `ColumnFilterHelper`,
keine Server-Pagination — ein Umstieg auf Server-Mode waere eine grundlegende, hier nicht angeforderte
Architekturaenderung dieser Ansicht.

**Dropdown-Umsetzung (Client-Mode, ehrlich benannter Mehraufwand):** Ein Dropdown auf einer
Client-Mode-Filterspalte ist mehr Aufwand als das bestehende Freitext-Input, weil
`table-filter.js`/`updateBomVisibility()` bisher reinen Textvergleich auf gerenderten Zellentext
fahren (siehe `fallstricke.md`, "Universal-Filter-Pattern: Getter liefert gerenderten Text" — dasselbe
Prinzip gilt hier fuer den Client-Filter). Vorlage fuer "Werteauswahl statt Freitext" ist die
Kommissionierliste (`FaHierarchyKommissionierListen` bzw. deren zugehoerige Serverfilter-Logik) — dort
ist die Werteauswahl serverseitig ueber `ColumnFilterHelper`/`?colf_`-Query gebaut, hier fehlt dieser
Unterbau (Client-Mode). Die Dropdown-Werte werden daher **nicht** ueber eine neue DB-Abfrage bezogen,
sondern **client-seitig aus den bereits geladenen `BomViewModel`-Items** (`Model.Items.Select(i =>
i.Kommissionieren).Distinct()`) aufgebaut — die Zeilen liegen zum Renderzeitpunkt der View ohnehin
vollstaendig vor, kein zusaetzlicher Repository-/DB-Zugriff (ponytail Sprosse 2/3: wiederverwenden,
was schon da ist). Select2 ist auf der Seite bereits eingebunden (Z. 683-686) — keine neue
Abhaengigkeit.

**Filterlogik:** `updateBomVisibility()` (bestehende Funktion, kombiniert Baum-Zustand und
Spaltenfilter, siehe `fallstricke.md` "BOM-Baum hat Vorrang vor dem Spaltenfilter") bleibt der
zentrale Einstiegspunkt; das Dropdown feuert bei Auswahlaenderung denselben Pfad wie heute das
Text-Input (`input`-Event bzw. direkter Aufruf von `applyColumnFilterNow()`/`updateBomVisibility()`,
je nach Select2-Change-Event-Bindung), NICHT ein synthetisches `input`-Event (siehe Fallstrick
"Programmatisches `input.value = …` loest kein `input`-Event aus").

**Standardfilter-Feld:** exakte Kopie des `DefaultFilterBomDescription1`-Wegs (Modell → Migration →
Benutzereinstellungen-Views → Controller → BomViewModel → `column-preferences-ready`-Handler in
`Bom.cshtml`). Bei Mehrfachauswahl (siehe Q2) wird der gespeicherte Wert komma-separiert erwartet,
identisch zur bestehenden OR-`,`-Mini-Syntax der anderen `DefaultFilter*`-Felder.

**Badge/Reset:** kleine, rein clientseitige Ergaenzung im bestehenden `column-preferences-ready`-Block
(Z. 985-1000) — kein neuer Endpunkt, kein Server-Roundtrip. Zeigt je aktiv gesetztem Default (Werte
nicht leer) einen Hinweis mit Klartextwert und ruft bei Klick `window.setColumnFilter(key, '')` +
erneutes `updateBomVisibility()`.

## Migrations-/SQL-Auswirkungen

Migration **92** (naechste freie Nummer im Worktree, aktueller Max ist
`91_AddArticleMatchcode.sql`): `dotnet ef migrations add AddUserDefaultFilterBomKommissionierziel
--project IdealAkeWms`, danach `SQL/92_AddUserDefaultFilterBomKommissionierziel.sql` nach exakter
Vorlage `SQL/87_AddUserDefaultFilterBomDescription1.sql` (`COL_LENGTH`-Guard, `ALTER TABLE
[dbo].[Users] ADD [DefaultFilterBomKommissionierziel] NVARCHAR(200) NULL;` in eigenem Batch,
`__EFMigrationsHistory`-Insert in separatem Batch). `SQL/00_FreshInstall.sql` an **beiden** Stellen
nachziehen: Schema-Block (`Users`-Tabellendefinition) und `MigrationId`-Liste. Nullable Spalte an
einer Bestandstabelle, **nicht daten-destruktiv**, kein Backup-Hinweis noetig ueber das ohnehin
uebliche Vorsichtsmass hinaus. `SQL/AgentJobs/*` nicht betroffen (kein Pflichtfeld, kein
Folge-MERGE).

## Audit-Feld-Auswirkungen

Keine neue Entitaet. `User` erbt `AuditableEntity`; das Speichern von
`DefaultFilterBomKommissionierziel` laeuft ueber denselben bestehenden Update-Pfad wie
`DefaultFilterBomDescription1` (`UsersController`/`AccountController`), der bereits `ModifiedAt`,
`ModifiedBy`, `ModifiedByWindows` aus `ICurrentUserService` setzt — keine Aenderung an dieser Logik
noetig, nur ein zusaetzliches Feld im selben Update.

## Akzeptanzkriterien

1. In der hierarchischen Stueckliste (`Model.Hierarchical == true`) zeigt die Filterzeile der Spalte
   `Komm.-Ziel` ein Dropdown (Select2) statt eines Freitext-Inputs; Auswahl eines Werts filtert die
   Tabelle exakt wie der bisherige Freitextfilter.
2. Die Dropdown-Optionen entsprechen den in der aktuell geladenen Stueckliste tatsaechlich
   vorkommenden `Kommissionieren`-Werten (distinct, keine feste/veraltete Liste). Die Optionen stammen
   aus den **geladenen** Positionen der Stueckliste — **nicht** aus den Benutzereinstellungen, dort
   ist das Feld ein Textfeld.
3. Ist die Stueckliste leer oder hat keine Zeile ein Kommissionierziel, zeigt das Dropdown einen
   erkennbaren Leer-Zustand (kein stiller Absturz, keine leere graue Flaeche ohne Erklaerung).
4. Im flachen AKE-Modus (`Model.Hierarchical == false`) existiert weder die Spalte noch die
   Filterzeile noch das Dropdown — unveraendert zum heutigen Verhalten.
5. Ein Benutzer kann in seinen Benutzereinstellungen (bzw. im eigenen Profil) einen
   Standard-Kommissionierziel-Filter setzen; beim naechsten Aufruf der Stueckliste eines
   hierarchischen Auftrags ist der Filter automatisch aktiv.
6. Ein gesetzter Standard-Kommissionierziel-Filter wird beim Aufruf einer **AKE**-Stueckliste
   (flacher Modus) **nicht** angewendet und filtert dort **nichts** leer.
7. Ist einer der **vier** Standardfilter (Beschaffung, Artikelgruppe, Bezeichnung 1 oder
   Kommissionierziel) beim Laden der Stueckliste aktiv,
   erscheint ein sichtbarer Hinweis/Badge mit dem gefilterten Wert; ein Klick darauf hebt den
   jeweiligen Filter auf und zeigt wieder alle Zeilen (vorbehaltlich des Baum-Zustands).
8. **Leerzustand ist benannt, nicht still.** Trifft der gespeicherte Kommissionierziel-Standard in der
   aktuellen Stueckliste **keine einzige** Position, zeigt die Ansicht ausdruecklich
   *„Standardfilter Kommissionierziel `<Wert>` aktiv — keine Treffer in dieser Stueckliste"* und
   bietet den **Ein-Klick-Reset** an. Trifft er **teilweise** (OR ueber mehrere Werte), erscheinen die
   passenden Zeilen **ohne** Leerzustands-Meldung. Weder still ignorieren (Widerspruch zum Badge)
   noch still leer (Anwender haelt die Stueckliste fuer leer).
9. Mehrere im Dropdown gewaehlte Kommissionierziele werden per
   OR verknuepft (jede Zeile mit einem der gewaehlten Ziele bleibt sichtbar) und komma-separiert im
   Benutzerprofil gespeichert.
10. Ein beim Druckaufruf aktiver Kommissionierziel-Filter erscheint im Ausdruck (`PrintBom`) als
    Hinweis **„Gefilterte Ansicht"** im Kopfbereich, und die hierarchischen Spalten (Komm.-Ziel,
    Ebene) erscheinen im Ausdruck, sofern sie auf dem Bildschirm sichtbar waren
    (`visibleColumns`-Parameter). *Letzter, abtrennbarer Block — siehe In-Scope 7.*
11. Migration 92 laeuft auf einer bestehenden Datenbank fehlerfrei und idempotent (zweimaliges
    Ausfuehren von `SQL/92_*.sql` aendert nichts am zweiten Lauf).
12. **Einstellungsfeld nur bei Master `true`.** Bei `ProduktionsauftragHierarchisch = false` ist das
    Feld „Standard-Filter Kommissionierziel" weder in `Users/Edit` noch in `Account/Profile`
    sichtbar; `DefaultFilterBomDescription1` bleibt dort unveraendert sichtbar.
13. **Das Einstellungsfeld ist ein Textfeld mit OR-Syntax**, kein Dropdown — identisch zur Vorlage.
14. **Auto-Aufklappen:** Waehlt der Anwender im Dropdown einen Wert, der nur in zugeklappten Aesten
    vorkommt, werden diese Aeste aufgeklappt und die Treffer sichtbar — ein korrekt arbeitender
    Filter darf nicht wie ein defekter aussehen.
15. **Bestandsfelder:** Beschaffung, Artikelgruppe und Bezeichnung 1 zeigen bei gesetztem Standard
    ebenfalls den Badge mit Reset — Nachweis, dass der vorbestehende Mangel geschlossen ist.

## Test-Szenarien

Ergaenzung in `docs/TESTSZENARIEN.md`, Kapitel **TS-70** (BOM-Bridge), neuer Abschnitt
"Komm.-Ziel-Filter":

- Hierarchische Stueckliste oeffnen (HauptFA, `FullStructure`) → Filterzeile der Spalte `Komm.-Ziel`
  zeigt ein Dropdown, keine Freitexteingabe mehr.
- Einen Wert waehlen → nur Zeilen mit diesem Kommissionierziel (bzw. deren aufgeklappte
  Baugruppen-Eltern) bleiben sichtbar; Baum-Zustand (auf-/zugeklappt) bleibt weiterhin fuehrend
  (bestehendes `updateBomVisibility()`-Verhalten).
- Dropdown-Optionen mit den tatsaechlich in der Liste vorkommenden Werten abgleichen (kein Eintrag,
  der in der aktuellen Stueckliste nicht vorkommt).
- **Negativfall — leere Stueckliste/keine Kommissionierziele:** Dropdown zeigt einen erkennbaren
  Leerzustand statt eines funktionslosen leeren Felds.
- Im Benutzerprofil einen Standard-Kommissionierziel-Filter setzen → Stueckliste eines
  hierarchischen Auftrags erneut oeffnen → Filter ist vorbelegt **und** als Badge sichtbar; Klick auf
  den Badge/Reset hebt den Filter auf.
- **Negativfall — AKE-Flachmodus mit gesetztem Default:** AKE-Stueckliste oeffnen (Standort mit
  Master aus) → Liste ist **vollstaendig** sichtbar, kein stilles Leerfiltern, keine
  Kommissionierziel-Filterzeile vorhanden.
- **Negativfall — gespeicherter Wert kommt in der aktuellen Stueckliste nicht vor:** Standardfilter
  auf einen in der aktuellen Liste nicht vorkommenden Wert setzen, Stueckliste oeffnen → Meldung
  „Standardfilter Kommissionierziel `<Wert>` aktiv — keine Treffer in dieser Stueckliste" mit
  Ein-Klick-Reset; Reset zeigt wieder alle Zeilen.
- **Teiltreffer:** Standard `KA-02,S-01` gesetzt, nur `KA-02` kommt vor → `KA-02`-Zeilen sichtbar,
  **keine** Leerzustands-Meldung.
- **Wert aus zugeklapptem Ast:** Ast zuklappen, im Dropdown einen Wert waehlen, der nur dort vorkommt →
  Ast klappt auf, Treffer sichtbar.
- **AKE-Einstellungen:** Mit Master `false` Benutzereinstellungen oeffnen → Feld
  „Standard-Filter Kommissionierziel" fehlt; „Standard-Filter Bezeichnung 1" ist vorhanden.
- Alle **vier** Standardfilter nacheinander setzen (Beschaffung, Artikelgruppe, Bezeichnung 1,
  Kommissionierziel) → je ein Badge mit Reset (Nachweis der geschlossenen Luecke aus Fachlicher
  Anforderung 4).
- Zwei Kommissionierziele waehlen → Zeilen beider Ziele bleiben
  sichtbar (OR), Speichern im Profil → kommasepariert im Feld.
- Mit aktivem Kommissionierziel-Filter drucken → Ausdruck zeigt „Gefilterte Ansicht" im Kopf und
  die hierarchischen Spalten; ohne aktiven Filter → kein Hinweis, vollstaendige Liste.

Nach Abschluss `secondbrain/tests/testszenarien-index.md` nachziehen (TS-70-Eintrag ergaenzen).

## Deploy

- **Web-App:** ja — `IdealAkeWms.csproj` (Views, Controller, Migration).
- **Service:** nein — `IDEALAKEWMSService` unberuehrt.
- **Migration:** ja — Migration 92, nullable Spalte an `Users`, nicht daten-destruktiv.
- **Kontext:** Die betroffene Spalte/Filterzeile existiert aktuell ausschliesslich im noch nicht
  gemergten Epic-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (Branch
  `feature/2026-08-07-ideal-teile-1-5`). Diese Spec wird dort als zusaetzliche Etappe umgesetzt — kein
  zusaetzlicher eigener Deploy-Schritt, sie geht im selben Publish/Merge des Epic-Buendels mit
  (Vorbedingung: der Dev-Lauf bestaetigt diesen provisorischen Ausfuehrungsort gegen den dann
  aktuellen Worktree-/Branch-Stand).
- **Publish-Befehle (nachgelagerter Fall, im Worktree auszufuehren):**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
- Reihenfolge bei Deploy: DB-Migration (Skript 92) vor oder mit dem Web-Deploy einspielen — reine
  additive Spalte, keine Ausfallzeit-Anforderung, kein Service-Stop noetig.
- Ablauf: Publish **aus dem Worktree** → Testsystem → Test (siehe Checkliste unten) → **danach**
  Merge (Schranke 2, Mensch). Nach dem Merge nur dann erneut aus `main` publishen, wenn der Merge
  tatsaechlich getestete Dateien mit parallelen `main`-Aenderungen zusammengefuehrt hat (dieser
  Branch ist ein Buendel-Worktree mit mehreren Etappen — Merge-Diff pruefen).

## Offene Rueckfragen — ALLE BEANTWORTET (2026-09-21)

> Die Fragen unten sind im Abschnitt „Freigabe-Antworten" beantwortet und zusammen mit den Befunden
> der Kritischen Pruefung (B1, S1-S4, H1, H2) in Umfang, Fachliche Anforderungen, `affected_code`,
> Akzeptanzkriterien und Testszenarien eingearbeitet. Sie stehen hier nur noch als **Protokoll**,
> nicht als Auftrag. **Massgeblich fuer den Dev-Lauf ist der Rumpf.**

1. → **Dropdown: Einzel- oder Mehrfachauswahl?** Backlog-Tendenz: Mehrfachauswahl,
   kommasepariert gespeichert — konsistent zu `DefaultFilterBomDescription1` (OR-Mini-Syntax) und
   `DefaultWorkbenches` (ebenfalls kommasepariert). Empfehlung dieser Spec: Mehrfachauswahl
   uebernehmen, da ein Kommissionierer durchaus zwei Bereiche betreuen kann und die Syntax im
   Datenmodell ohnehin schon dafuer vorgesehen ist.
2. → **Dropdown-Werte fest oder aus den Daten?** Backlog-Tendenz: datengetrieben. Empfehlung:
   aus den bereits geladenen `BomViewModel`-Items (`.Kommissionieren` distinct) ableiten — kein neuer
   DB-Zugriff, die Zeilen liegen beim Rendern schon vor. Randfall leere Liste (siehe AK 3).
3. → **Verhalten bei gespeichertem Wert ohne Treffer in der aktuellen Stueckliste?** Zwei Varianten
   zur Wahl: (a) Filter automatisch ignorieren + sichtbarer Hinweis "gespeicherter Filter X kommt hier
   nicht vor, zeige alle" oder (b) Filter anwenden (Ergebnis: 0 Zeilen) mit klarem Hinweis statt
   stiller Leere. Keine Empfehlung aus dem Backlog erkennbar — Entscheidung des Menschen erbeten.
4. → **Soll der Filter in Druck/PDF (PrintBom) durchschlagen?** Falls ja: Aufwand wie unter
   Fachlicher Anforderung 5 beschrieben (colNames-Map + PrintBom-Whitelist-Ergaenzung +
   Filterhinweis im Ausdruck). Falls nein: kein Eingriff in `PrintBom.cshtml` noetig, Ausdruck bleibt
   wie heute vollstaendig. Keine Tendenz aus dem Backlog ableitbar.
5. → **Bestaetigung: Badge + Ein-Klick-Reset fuer BEIDE Standardfilter-Felder bauen** (Bezeichnung-1-
   Default UND Komm.-Ziel-Default), nicht nur fuer das neue Feld — die Luecke besteht laut
   Code-Pruefung bereits heute fuer `DefaultFilterBomDescription1` (nur Vorbelegung im Input, kein
   Hinweis/Reset). Empfehlung dieser Spec: ja, sonst entsteht eine Asymmetrie zwischen zwei gleich
   behandelten Feldern derselben Ansicht.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. → **Mehrfachauswahl, kommasepariert gespeichert.** Konsistent mit dem bestehenden Feldformat
   (`DefaultFilterBomDescription1` nutzt OR-Syntax mit `,`) und mit `DefaultWorkbenches`. Und es
   kostet nichts extra: **Select2 ist bereits geladen und kann Mehrfachauswahl nativ.** Wer zwei
   Bereiche betreut, soll nicht zwischen ihnen umschalten muessen.

2. → **In der Stueckliste: aus den geladenen Items. In den Benutzereinstellungen: NICHT — dort gibt es
   keine geladenen Items.**
   Die Empfehlung der Spec ist fuer die **Stueckliste** richtig: immer aktuell, keine Pflege. Sie
   **uebersieht aber den zweiten Ort**: Das Standard-Feld wird in den **Benutzereinstellungen**
   gesetzt, und dort ist **keine Stueckliste geladen** — ein datengetriebenes Dropdown haette dort
   keine Werte.
   **Verbindlich:**
   - **Stueckliste:** Dropdown mit den `DISTINCT`-Werten der geladenen Positionen.
   - **Benutzereinstellungen:** das Feld folgt **1:1 der Vorlage `DefaultFilterBomDescription1`** —
     also **Textfeld mit OR-Syntax**, wie heute. Das ist der Kern der „1:1-Kopie"-Entscheidung, und
     ein Dropdown dort braeuchte eine eigene Wertequelle (`DISTINCT` ueber alle
     `FaHierarchyNode.Kommissionieren`), die es fuer das Vorlagenfeld auch nicht gibt.
   *Falls spaeter ein Dropdown auch in den Einstellungen gewuenscht ist:* eigene Wertequelle ueber
   alle Strukturknoten — aber dann fuer **beide** Felder, nicht nur fuer das neue. Nicht jetzt.

3. → **Filter anwenden — und bei null Treffern das Ergebnis erklaeren, nicht verschweigen.**
   Die beiden stillen Varianten sind beide schlecht:
   - **Still ignorieren** zeigt alles, waehrend der Badge „Standardfilter aktiv" sagt — ein
     Widerspruch am Bildschirm.
   - **Still leer** laesst den Anwender glauben, die Stueckliste habe keine Positionen.
   **Verbindlich:** Der Filter wird angewendet. Da mehrere Werte per OR wirken, trifft er in der Regel
   teilweise (`KA-02,S-01` gespeichert, nur `KA-02` vorhanden → die `KA-02`-Zeilen erscheinen).
   **Nur wenn gar nichts trifft**, zeigt der Leerzustand **ausdruecklich**: *„Standardfilter
   Kommissionierziel `X` aktiv — keine Treffer in dieser Stueckliste."* plus den **Ein-Klick-Reset**
   aus Antwort 5. Hausregel: sichtbar machen statt still filtern.

4. → **JA, der Filter schlaegt in Druck und PDF durch — und das schliesst nebenbei eine bekannte
   Luecke.**
   Begruendung: dasselbe Prinzip wie AK 2 der PDF-Spec — der Ausdruck entspricht dem gefilterten
   Bildschirm, und ein aktiver Filter wird im Kopf als **„Gefilterte Ansicht"** ausgewiesen. Sonst
   haelt jemand eine Teilmenge auf Papier fuer das Ganze.
   **Der Mehrwert:** Die Druck-Whitelist in `PrintBom.cshtml` kennt das Kommissionier-Ziel **ohnehin
   noch nicht** — das ist der offene Befund aus der BOM-Bridge (v1.36). Und er ist durch
   [[2026-09-13-kommissionierung-nur-hauptfa]] inzwischen **faellig**: Bleibt die Vollstruktur an der
   HauptFA-Stueckliste (Ruling 4), braucht der Ausdruck Ebene und Komm.-Ziel, sonst ist er
   unbrauchbar. **Diese Spec schliesst den Befund mit** — Whitelist und `colNames`-Map werden hier
   nachgezogen, nicht in einem eigenen Lauf.

5. → **BESTAETIGT: Badge und Ein-Klick-Reset fuer BEIDE Felder.**
   Die Luecke ist **vorbestehend** — `DefaultFilterBomDescription1` belegt heute nur das Eingabefeld
   vor, ohne Hinweis und ohne Reset. Nur das neue Feld sichtbar zu machen, erzeugte eine Asymmetrie
   zwischen zwei gleich behandelten Feldern derselben Ansicht: Der eine Filter kuendigt sich an, der
   andere wirkt still. **Beide zugleich**, gleiches Bedienelement.

6. → **AKE: nicht anzeigen, wo das Feld nicht verwendet wird [ENTSCHEIDUNG 2026-09-18].**
   AKE fuehrt kein Kommissionierziel. Was dort nicht befuellt werden kann, erscheint auch nicht.
   Die Code-Pruefung dieser Spec hat bereits verifiziert, dass Spalte und Filter nur im
   hierarchischen Modus existieren — das wird hiermit von einer **Eigenschaft** zur **festgelegten
   Regel**, damit es niemand spaeter „harmonisiert".
   **Gilt fuer alle drei Stellen, nicht nur die Spalte:**
   - **Spalte** in der Stueckliste — nur bei Master `true` (bereits so gebaut).
   - **Dropdown-Filter** — nur bei Master `true`.
   - **Einstellungsfeld** „Standard-Filter Kommissionierziel" in den Benutzereinstellungen —
     **ebenfalls nur bei Master `true`**. Ein Feld, das auf einer AKE-Installation gar nichts
     bewirkt, waere dort nur verwirrend. **Das ist neu gegenueber dem Code-Stand** und gehoert in den
     Umfang.
   Der **Badge** erscheint ohnehin nur bei aktivem Filter und damit bei AKE nie.
   `DefaultFilterBomDescription1` bleibt davon **unberuehrt** — es ist ein hausweites Feld und wird
   bei beiden Standorten angezeigt.

   **Ausdruecklich KEIN Widerspruch zum Matchcode**, der „hausweit sichtbar" entschieden wurde: Der
   Matchcode hat eine AKE-Quelle (`KHKArtikel.Matchcode`, per erweiterter Query), das
   Kommissionierziel hat **keine**. Verschiedene Datenlage, verschiedene Regel — wer die beiden
   angleichen will, muss zuerst diese Frage neu stellen.

## ANTWORTEN auf die Kritische Pruefung (2026-09-18)

### Zu B1 — Badge fuer ALLE VIER Felder. [ENTSCHEIDUNG]

Der Blocker folgt direkt aus **meiner eigenen Begruendung** in Antwort 5: *„Nur das neue Feld
sichtbar zu machen, erzeugte eine Asymmetrie zwischen zwei gleich behandelten Feldern derselben
Ansicht."* Nach genau dieser Logik darf es nicht bei zwei bleiben, wenn die Ansicht **vier**
Standardfilter still vorbelegt (Z. 993-998). „Beide" haette die Asymmetrie nur verschoben — von
1-zu-2 auf 2-zu-4.

**Verbindlich: Badge und Ein-Klick-Reset fuer Beschaffung, Artikelgruppe, Bezeichnung 1 und
Kommissionierziel.** Hausregel ohne Ausnahme: Ein Filter, der beim Oeffnen bereits wirkt, muss sich
zu erkennen geben.
Der Mehraufwand ist gering — das Bedienelement wird einmal gebaut; ob es zwei oder vier Felder
bedient, ist eine Zeile Konfiguration, kein zweiter Baustein.

**Die Tragweite ist groesser als die Spec:** Beschaffung und Artikelgruppe filtern **heute** still,
seit sie eingefuehrt wurden. Wer mit gesetztem Standard eine Stueckliste oeffnet, sieht moeglicherweise
seit Wochen eine Teilmenge, ohne es zu wissen. Das ist ein **vorbestehender Mangel**, den diese Spec
nebenbei behebt.

### Zu S1 — uebernommen. Die AKE-Ausblendung ist echte Zusatzverdrahtung.

Am Code bestaetigt: Weder `AccountController` noch `UsersController` liest heute
`ProduktionsauftragHierarchisch`. **Beide Einstellungsseiten** brauchen das Master-Flag, damit das
Kommissionierziel-Feld bei `false` verschwindet. Ins `affected_code` aufnehmen.
**Bewusste Abweichung von der „1:1-Kopie"** der Vorlage, im Rumpf so zu benennen: Das Vorlagenfeld
`DefaultFilterBomDescription1` ist hausweit sichtbar, das neue Feld nicht — weil es fuer AKE keine
Quelle hat (Antwort 6).

### Zu S2 — uebernommen. Druck-Durchschlag an allen vier Stellen ENTKONDITIONALISIEREN.

Antwort 4 lautet **ja**. Damit ist „nur falls Q6 = ja" in `affected_code` (Z. 24-25),
Out-of-Scope (Z. 97-98) und AK 10 zu streichen — es ist **verbindlicher Umfang**, keine Bedingung.

### Zu S3 — uebernommen. Der Leerzustand wird ein testbares AK.

AK 8 verweist heute nur auf „Verhalten gemaess Q4". Stattdessen ausformulieren:
*Trifft der gespeicherte Kommissionierziel-Standard in der aktuellen Stueckliste keine einzige
Position, zeigt der Leerzustand ausdruecklich „Standardfilter Kommissionierziel `<Wert>` aktiv —
keine Treffer in dieser Stueckliste" und bietet den Ein-Klick-Reset an. Trifft er teilweise (OR),
erscheinen die passenden Zeilen ohne Leerzustand-Meldung.*

### Zu S4 — uebernommen. Die zwei Orte werden im Rumpf ausdruecklich getrennt.

FA 1 (Stueckliste) = **Dropdown** aus den geladenen Items. FA 2 (Benutzereinstellungen) = **Textfeld
mit OR-Syntax**, ausdruecklich **kein** Dropdown — dort ist keine Stueckliste geladen. Die
Q3-Empfehlung ist auf die **Stueckliste** zu beschraenken, damit sie nicht fuer beide gelesen wird.

### Zu H1 — der Druck bleibt im Umfang, aber als ABTRENNBARER letzter Block.

Der Hinweis ist berechtigt: Filter-Durchschlag in den Druck macht `PrintBom` strukturell zu einem
eigenen Baustein. Ihn herauszunehmen hiesse aber, die ohnehin **faellige** Whitelist-Erweiterung
(Ebene, Komm.-Ziel — offen seit der BOM-Bridge, faellig durch „Kommissionierung nur auf HauptFA")
weiter aufzuschieben.
**Vorgabe fuer den Dev-Lauf:** Der Druck wird **zuletzt** gebaut, nach Dropdown, Standardfilter,
Badge und AKE-Ausblendung. **Erweist er sich als groesser als erwartet, darf der Lauf nach dem
Bildschirm-Teil anhalten und melden** — dann wird der Druck eine eigene kleine Folge-Spec, statt
dass ein halbfertiger Druck im Zweig steht.

### Zu H2 — Werte aus zugeklappten Aesten: aufklappen wie bei der FA-Liste.

Waehlt jemand einen Wert, der nur in zugeklappten Aesten vorkommt, sieht er scheinbar nichts — die
passenden Zeilen sind da, aber verborgen. **Derselbe Fall wie die Suche in zugeklappten Gruppen der
FA-Liste**, dort geloest durch automatisches Aufklappen der Treffer-Aeste. **Dasselbe Muster hier
uebernehmen**, nicht neu erfinden. Ohne das wirkt ein korrekt arbeitender Filter defekt.

**Nach der Einarbeitung von B1 und S1-S4 in Rumpf und `affected_code` ist die Spec freigabereif.**

## Kritische Pruefung (2026-09-21)

> Anwalt-des-Teufels-Durchsicht **vor** dem Dev-Lauf. Die sechs Freigabe-Antworten sind ausgefuellt,
> aber **noch nicht in den Rumpf gezogen** — genau dort entstehen die Risse. Wo moeglich am Code
> (Worktree `feature/2026-08-07-ideal-teile-1-5`) gegengeprueft. Ein BLOCKER (echte Entscheidung),
> vier SOLLTE (Antworten in den Rumpf ziehen), zwei HINWEISE.

### BLOCKER

**B1 — Zwei stille Standardfilter auf DERSELBEN Ansicht bleiben ohne Badge; die Hausregel, mit der
diese Spec den Badge begruendet, wird nur halb angewandt.** `Bom.cshtml` Z. 993-998 belegt **vier**
Default-Filter still vor: `DefaultFilterBeschaffung`, `DefaultFilterArtikelgruppe`,
`DefaultFilterBomDescription1` **und** (neu) `DefaultFilterBomKommissionierziel`. Antwort 5 / Fachliche
Anforderung 4 spendieren den sichtbaren Badge + Reset aber **nur zweien** (Bezeichnung 1 +
Kommissionierziel). `Beschaffung` und `Artikelgruppe` filtern nach dieser Spec **weiterhin still** —
exakt der Zustand, den die Spec mit „sichtbar machen statt still filtern" als Luecke brandmarkt. Die
Begruendung „sonst entsteht eine Asymmetrie zwischen zwei gleich behandelten Feldern derselben
Ansicht" trifft auf **alle vier** zu, nicht auf zwei.
→ **Entscheidung des Menschen:** Badge fuer **alle vier** stillen Default-Filter dieser Ansicht (die
konsequente Anwendung der Hausregel), oder bewusst nur die zwei — und dann im Rumpf **explizit**
festhalten, dass `Beschaffung`/`Artikelgruppe` weiterhin still bleiben und warum. So oder so darf der
Widerspruch nicht unentschieden in die Umsetzung.

### SOLLTE

**S1 — Antwort 6 (AKE: Einstellungsfeld ausblenden) braucht neue Verdrahtung und widerspricht dem
„1:1-Kopie/kein neuer Mechanismus"-Rumpf; beides nachziehen.** Verifiziert: **weder**
`AccountController` **noch** `UsersController` liest heute `ProduktionsauftragHierarchisch` (Grep:
keine Treffer in beiden). Das Vorlagenfeld `DefaultFilterBomDescription1` ist hausweit sichtbar — das
neue Feld nur bei Master `true` anzuzeigen ist also **echte Zusatzarbeit**, nicht im `affected_code`:
(a) den Master-Schalter in `UsersController.Edit` **und** `AccountController` (Profil) in die jeweiligen
ViewModels/ViewBag reichen (Muster wie in `PickingController`/`HierarchischeStrukturKeys`), (b) das
Feld in `Users/Edit.cshtml` + `Account/Profile.cshtml` unter dieses `@if` stellen. Zusaetzlich:
Fachliche Anforderung 2 nennt das Feld eine „**1:1-Kopie … kein neuer Mechanismus**" — Antwort 6 macht
daraus bewusst **eine Abweichung** (Master-Gate auf einem Einstellungsfeld, das die Vorlage nicht hat).
Diesen einen dokumentierten Unterschied in FA2 vermerken, sonst „harmonisiert" ihn spaeter jemand weg.
`affected_code`-Zeile zu `Users/Edit`+`Profile` entsprechend ergaenzen.

**S2 — Antwort 4 (Druck JA) ist beschlossen, steht im Rumpf aber noch ueberall als „nur falls Q6=ja".**
`PrintBom.cshtml`-Whitelist + `colNames`-Map (Bom.cshtml Z. 1015) + „gefilterte Ansicht"-Hinweis sind
mit Antwort 4 **fest in Scope**. Im Rumpf sind sie aber noch konditional: `affected_code` Z. 24-25
(„nur falls Q6=ja"), Out-of-Scope Z. 97-98 („nur bedingt in Scope … ohne ‚ja' bleibt PrintBom
unangetastet"), AK 10 („Nur falls Q6=ja"), TS-Zeilen 277-280. Alle vier Stellen entkonditionalisieren:
aus Out-of-Scope streichen, in In-Scope aufnehmen, AK 10 unbedingt formulieren. Damit schliesst diese
Spec — wie Antwort 4 sagt — den offenen BOM-Bridge-Druck-Befund (Komm.-Ziel/Ebene fehlen im Ausdruck)
tatsaechlich mit; das gehoert sichtbar in den Umfang, nicht in einen Konditional.

**S3 — Antwort 3 (Leerzustand) ist noch kein pruefbares AK.** AK 8 sagt nur „filtert **nicht** still
auf 0 Zeilen ohne Erklaerung — Verhalten gemaess Freigabe-Antwort zu Q4" — ein Verweis, kein Kriterium.
Antwort 3 ist konkret und testbar: Filter wird **angewandt**; bei Mehrfach-OR trifft er meist teilweise;
**nur wenn gar nichts trifft**, erscheint der **benannte** Leerzustand *„Standardfilter Kommissionierziel
`X` aktiv — keine Treffer in dieser Stueckliste."* **plus** der Ein-Klick-Reset aus Antwort 5. AK 8 auf
genau diese Fassung umschreiben (benannter Text + Reset + Teiltreffer-Fall), sonst ist „mit Hinweis"
Auslegungssache im Dev-Lauf.

**S4 — Antwort 2 (zwei Orte) im Rumpf explizit trennen.** Der Rumpf behandelt beide Orte **nicht
sauber getrennt**: Fachliche Anforderung 1 = Dropdown (Stueckliste), FA 2 = „1:1-Kopie" (Einstellungen)
— aber **nirgends** steht ausdruecklich, dass das Einstellungsfeld ein **Textfeld** bleibt und **kein**
Dropdown wird. Die (offene) Empfehlung zu Q3 „aus den geladenen Items" liest sich, als gaelte sie fuer
beide Orte; genau das schliesst Antwort 2 aus (in den Einstellungen sind **keine** Items geladen). FA 2
/ `affected_code` (Z. 18) um einen Satz ergaenzen: **Stueckliste = Dropdown aus DISTINCT-Items;
Einstellungen = Textfeld mit OR-Syntax, 1:1 wie die Vorlage.** Sonst baut der Dev-Lauf womoeglich ein
quellenloses Dropdown in die Einstellungen.

### HINWEIS

**H1 — Q6=ja vergroessert den Umfang spuerbar.** `PrintBom.cshtml` kennt die hierarchischen Spalten
**strukturell** nicht (nur die acht AKE-Basisspalten + Lagerplatz, verifiziert Z. 213-251) — Komm.-Ziel
im Ausdruck heisst neue Kopf-/Zellbloecke, nicht nur ein Whitelist-Flag. Zusammen mit Dropdown-auf-
Client-Mode (selbst als „ehrlicher Mehraufwand" benannt), neuem Feld+Migration, Master-Gating-Verdrahtung
(S1) und Badge fuer >2 Felder (B1) ist das ein **grosser** Einzel-Dev-Lauf. Kein Split-Zwang, aber der
Dev-Lauf sollte die Druck-Erweiterung als eigenstaendigen, zuletzt umgesetzten Baustein behandeln, damit
er kippbar bleibt, falls die Zeit knapp wird.

**H2 — Dropdown-Wertequelle bei aufgeklapptem Teilbaum.** Antwort 2/AK 2 speisen das Dropdown aus den
**geladenen** `Model.Items`. Bei der Vollstruktur ist das die ganze Struktur — gut. Aber der Baum-Zustand
(auf-/zugeklappt) hat laut `fallstricke.md` Vorrang vor dem Spaltenfilter: Ein Wert, der nur in einem
**eingeklappten** Ast vorkommt, steht dann im Dropdown, filtert aber sichtbar „nichts", bis der Nutzer
aufklappt. Kein Fehler (die Zeilen sind da, nur versteckt), aber beim Dev-Lauf im Blick behalten, damit
es nicht faelschlich als „Filter kaputt" gemeldet wird.

### Empfehlung

**NACHBESSERUNG NOETIG:** ein BLOCKER (B1 — Badge-Umfang: alle vier stillen Default-Filter oder bewusst
nur zwei, dann explizit) plus vier Rumpf-Nachziehungen der bereits gegebenen Antworten (S1 Master-Gating
+ Verdrahtung, S2 Druck entkonditionalisieren, S3 Leerzustand als AK, S4 Zwei-Orte-Trennung). Die
Antworten selbst sind stimmig und am Code gedeckt — es fehlt nur ihre Einarbeitung in Rumpf und
`affected_code`, bevor der Dev-Lauf startet.

## QA-Nachweis (2026-09-21, qa-agent)

**Commit-Range geprueft:** `f04a7e0~1..5f933f1` (Basis `ed70c2b`, Feature-Commits `f04a7e0` +
`92139a4` + `5f933f1`) im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch
`feature/2026-08-07-ideal-teile-1-5`. Diff: 23 Dateien, +4610/-15.

**Build:** `dotnet build IdealAkeWms.slnx` → **0 Fehler**, 12 Warnungen (alle vorbestehend,
NU1902-Paket-Advisories + zwei Nullref-/ungenutzte-lokale-Funktion-Warnungen ausserhalb dieser
Aenderung) — verifiziert per grep gegen die geaenderten Dateien, keine der 12 Warnungen stammt aus
dieser Spec.

**Test:** `dotnet test IdealAkeWms.slnx`
- `IdealAkeWms.Tests`: **1391 erfolgreich, 0 Fehler, 1 uebersprungen** (Integrationstest,
  vorbestehend uebersprungen), gesamt 1392 — exakt die im Auftrag erwartete Zahl.
- `IDEALAKEWMSService.Tests`: **277 erfolgreich, 0 Fehler**, gesamt 277 — exakt die erwartete Zahl.
- Die drei Controller-Test-Fixes (`AccountControllerTests`, `UsersControllerTests`,
  `UsersControllerAdUserTests`) ergaenzen nur den neuen `Mock<IServiceSettingRepository>`-Ctor-Param,
  keine Assertion-Aenderung — minimaler, korrekter Diff.

**AK-Abgleich gegen den echten Diff (automatisiert/statisch pruefbar vs. Manual-UAT):**

| AK | Nachweis | Automated/Manual |
|---|---|---|
| 11 (Migration 92 idempotent) | `SQL/92_AddUserDefaultFilterBomKommissionierziel.sql` mit `COL_LENGTH`-Guard, DDL + `__EFMigrationsHistory`-Insert in getrennten Batches, exakt nach Vorlage `SQL/87_*.sql`; `SQL/00_FreshInstall.sql` an beiden Stellen (Schema-Block Z. 47, `MigrationId`-Liste) nachgezogen | Statisch verifiziert (Code-Lesung) — Ausfuehrung gegen echte DB bleibt Manual-UAT |
| 2/5/9/13 (Feld-Durchreichung, Mehrfachauswahl kommasepariert, Textfeld in Einstellungen) | `User.cs` neues Feld, `AccountController`/`UsersController` lesen/schreiben es 1:1 wie `DefaultFilterBomDescription1`, `PickingController.Bom` reicht es ins `BomViewModel`, `Views/Users/Edit.cshtml`+`Account/Profile.cshtml` als `<input>` (kein Select) | Statisch verifiziert — Bildschirm-Wirkung Manual-UAT |
| 6/12 (Master-Gating) | NEUE `IServiceSettingRepository`-Injektion in beiden Controllern, `IsHierarchicalMasterAsync()` liest `HierarchischeStrukturKeys.Master`, `HierarchicalMaster` ins ViewModel, `@if (Model.HierarchicalMaster)` um beide Einstellungsfelder | Statisch verifiziert — Sichtbarkeit je Standort Manual-UAT |
| 10 (Druck) | `PrintBomItem` + Print-Mapping um `Kommissionieren`/`Hauptlagerplatz`/`Ebene` ergaenzt, `PrintBom.cshtml` `ShowCol`-Bloecke, `colNames`-Map in `Bom.cshtml` von numerisch auf col-key→Label umgestellt (dabei nebenbei einen vorbestehenden Bug korrigiert: `getActiveFilters()` liefert col-key-Strings, die alte numerische Map traf nie), Kopf-Hinweis „Gefilterte Ansicht" | Statisch verifiziert — Ausdruck-Optik Manual-UAT |
| 1/3/4/7/8/14/15 (Dropdown, Leerzustand, Badge, Auto-Aufklappen) | Client-Mode-JS in `Bom.cshtml` (`setupKommissionierzielDropdown`, `renderDefaultFilterBadges`, `checkKommissionierzielEmptyState`, `expandAncestorsOfMatching`) vollstaendig gelesen, Logik gegen AK-Text geprueft — bewusste ADR-0005-Client-Mode-Ausnahme, **keine Server-Tests moeglich** | **Manual-UAT** (Spec sagt das selbst korrekt: "Client-Mode-Ansicht — die Bildschirm-Interaktion ist Manual-UAT") |

**TS-70 / Testindex:** `docs/TESTSZENARIEN.md` Abschnitt „Komm.-Ziel-Filter + gespeicherter
Standardfilter + Badge (v1.42.0)" deckt alle 15 AKs textlich ab, markiert explizit
"Client-Mode-Ansicht — die Bildschirm-Interaktion ist Manual-UAT" (kein falscher Automatisierungs-
Anspruch). `secondbrain/tests/testszenarien-index.md` TS-70-Eintrag hat den v1.42.0-Nachtrag bereits
(Zeile 96, Wikilink auf diese Spec) — verifiziert vorhanden, keine Nacharbeit noetig.

**Version/Changelog:** `AppVersion.cs` in beiden Projekten auf `1.42.0`/`2026-09-21`. Anwender-
Changelog `Views/Help/Changelog.cshtml` mit konkreten Details (4 Punkte, kein Pauschalverweis) —
erfuellt die Hausregel „Hilfe-Detail-Regel".

**Code-Review (Ponytail-Massstab, kein separater Subagent verfuegbar in dieser Umgebung — vollstaendige
Selbstpruefung des gesamten Diffs):** Kein neuer Mechanismus, kein neues Paket (Select2 war schon
geladen), verstecktes `<input data-col-key>`-Element als Sync-Ziel statt Umbau der bestehenden
Filter-Pipeline (`table-filter.js` unangetastet) — kleinstmoeglicher Diff fuer eine Dropdown-Umstellung
im Client-Mode. Badge/Reset ist ein Bedienelement mit Array-Konfiguration (4 Eintraege), kein
Vierfach-Code. Keine Abstraktion ohne zweiten Verwendungsfall. Keine fehlenden Validierungen
gefunden (`[StringLength(200)]` konsistent zur Vorlage). Ein Nebenbefund positiv vermerkt: die
`colNames`-Korrektur (numerisch → col-key) behebt einen vorbestehenden, stummen Bug im
Druck-Filterhinweis — kein Scope-Creep, sondern Voraussetzung fuer AK 10.

**Ergebnis: GRUEN.** `status: Testbereit` gesetzt.

## Manuelle Test-Checkliste (Mensch, Schranke 2 — vor Merge)

Alle Punkte am Testsystem nach Deploy (Web + Migration 92) pruefen, IDEAL-Standort (Master
`ProduktionsauftragHierarchisch = true`) sofern nicht anders vermerkt:

1. Hierarchische Stueckliste eines HauptFA oeffnen → Filterzeile „Komm.-Ziel" zeigt ein
   Select2-Dropdown, keine Freitexteingabe mehr.
2. Dropdown-Optionen mit den tatsaechlich vorkommenden Kommissionierzielen der Liste abgleichen
   (keine veraltete/feste Liste, keine Werte, die dort nicht vorkommen).
3. Einen Wert waehlen → nur passende Zeilen (bzw. deren aufgeklappte Eltern) bleiben sichtbar.
4. Einen Ast zuklappen, dann im Dropdown einen nur dort vorkommenden Wert waehlen → Ast klappt
   automatisch auf, Treffer wird sichtbar.
5. Zwei Werte gleichzeitig waehlen → Zeilen beider Werte bleiben sichtbar (OR).
6. Im eigenen Profil (`Account/Profile`) einen Standard-Kommissionierziel-Filter setzen (Textfeld,
   z. B. `KA-02,S-01`) → speichern → Stueckliste eines hierarchischen Auftrags erneut oeffnen →
   Filter ist vorbelegt **und** als Badge oberhalb der Tabelle sichtbar.
7. Badge anklicken → Filter wird aufgehoben, alle Zeilen wieder sichtbar (Baum-Zustand bleibt wie er
   war).
8. Standardfilter auf einen in der aktuellen Liste **nicht vorkommenden** Wert setzen, Stueckliste
   oeffnen → Meldung „Standardfilter Kommissionierziel `<Wert>` aktiv — keine Treffer in dieser
   Stueckliste" erscheint, mit Reset-Knopf; Reset zeigt wieder alle Zeilen.
9. Standardfilter `KA-02,S-01` setzen, wenn nur `KA-02` in der Liste vorkommt → `KA-02`-Zeilen
   sichtbar, **keine** Leerzustands-Meldung (Teiltreffer).
10. Alle vier Standardfilter nacheinander setzen (Beschaffung, Artikelgruppe, Bezeichnung 1,
    Kommissionierziel) → je ein eigener Badge mit Reset erscheint.
11. In den Benutzereinstellungen (`Users/Edit`, eigenes Profil) pruefen: Feld „Standard-Filter
    Kommissionierziel" ist ein **Textfeld** (kein Dropdown) mit Platzhaltertext/Hilfetext zur
    Komma-Syntax.
12. **AKE-Standort** (Master `false`) oder Testsystem mit Master aus: AKE-Stueckliste oeffnen →
    keine Komm.-Ziel-Spalte, keine Filterzeile dafuer; ein evtl. gesetzter Standard filtert **nichts**
    leer, Liste vollstaendig sichtbar.
13. **AKE-Einstellungen:** `Users/Edit` bzw. Profil oeffnen (Master `false`) → Feld „Standard-Filter
    Kommissionierziel" ist **nicht** vorhanden; „Standard-Filter Bezeichnung 1" ist weiterhin da.
14. Mit aktivem Kommissionierziel-Filter auf „Drucken" klicken → Ausdruck zeigt im Kopf „Gefilterte
    Ansicht" **und** die Spalten Komm.-Ziel/Hauptlagerplatz/Ebene (soweit am Bildschirm sichtbar).
15. Ohne aktiven Filter drucken → kein „Gefilterte Ansicht"-Hinweis, vollstaendige Liste wie bisher.
16. Migration 92 am Zielsystem: `SQL/92_AddUserDefaultFilterBomKommissionierziel.sql` einspielen,
    danach ein zweites Mal ausfuehren → zweiter Lauf meldet keine Aenderung (Idempotenz-Kontrolle).