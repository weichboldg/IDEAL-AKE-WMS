---
type: spec
title: "Stueckliste: Komm.-Ziel-Dropdown-Filter + gespeicherter Standardfilter (Badge fuer beide Felder)"
slug: 2026-09-18-stueckliste-kommissionierziel-filter-spec
status: Entwurf
created: 2026-09-18
updated: 2026-09-18
source_backlog: "[[2026-09-18-stueckliste-kommissionierziel-filter]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - "IdealAkeWms/Models/User.cs (Z. 86-91, direkt nach DefaultFilterBomDescription1) — neues Feld DefaultFilterBomKommissionierziel, string?, [StringLength(200)], gleiche Mini-Syntax-Doku wie das Vorbildfeld"
  - "IdealAkeWms/Migrations/*_AddUserDefaultFilterBomKommissionierziel.cs (NEU, dotnet ef migrations add) + IdealAkeWms/Migrations/ApplicationDbContextModelSnapshot.cs (Snapshot-Update automatisch durch den EF-Migrationslauf)"
  - "SQL/92_AddUserDefaultFilterBomKommissionierziel.sql (NEU, exakte Vorlage SQL/87_AddUserDefaultFilterBomDescription1.sql: COL_LENGTH-Guard, ALTER TABLE Users ADD ... NVARCHAR(200) NULL, DDL-Batch + separater __EFMigrationsHistory-Insert-Batch)"
  - "SQL/00_FreshInstall.sql (zwei Stellen: Spalte Users.DefaultFilterBomKommissionierziel im Schema-Block + MigrationId-Zeile fuer die neue Migration)"
  - "IdealAkeWms/Models/ViewModels/UserEditViewModel.cs + IdealAkeWms/Models/ViewModels/ProfileViewModel.cs (neues Feld DefaultFilterBomKommissionierziel analog DefaultFilterBomDescription1)"
  - "IdealAkeWms/Views/Users/Edit.cshtml + IdealAkeWms/Views/Account/Profile.cshtml (neues Formularfeld direkt neben dem Bezeichnung-1-Standardfilter)"
  - "IdealAkeWms/Controllers/UsersController.cs + IdealAkeWms/Controllers/AccountController.cs (Feld beim Lesen/Speichern durchreichen, Audit-Felder wie bestehend ueber ICurrentUserService)"
  - "IdealAkeWms/Controllers/PickingController.cs (Bom-Action, Z. ~303-305/452-454 — currentUser.DefaultFilterBomKommissionierziel analog defaultFilterBomDescription1 ins BomViewModel legen)"
  - "IdealAkeWms/Models/ViewModels/BomViewModels.cs (BomViewModel: neue Property DefaultFilterBomKommissionierziel)"
  - "IdealAkeWms/Views/Picking/Bom.cshtml (Z. 153 Filterzeilen-Input auf Dropdown/Select2 umstellen NUR im hierarchischen Modus; Z. 666 #column-config unveraendert — Spalte existiert bereits; Z. 985-1000 column-preferences-ready-Handler: defaultBomKommissionierziel lesen + window.setColumnFilter('kommissionieren', ...) analog Z. 998; neuer sichtbarer Aktiv-Badge + Ein-Klick-Reset FUER BEIDE Felder (Bezeichnung-1-Default UND Komm.-Ziel-Default), da die Luecke laut Code-Pruefung (Backlog-Frage 7) heute fuer Bezeichnung 1 bereits besteht)"
  - "IdealAkeWms/wwwroot/js/select2 (bereits geladen, Bom.cshtml Z. 683-686) — Dropdown-Aufbau fuer die Filterzeile der Spalte kommissionieren, gespeist aus den im BomViewModel bereits geladenen Items (.Kommissionieren distinct), kein neuer Serverzugriff"
  - "IdealAkeWms/Views/Picking/PrintBom.cshtml (nur falls Rueckfrage Q6 mit 'ja' beantwortet wird: ShowCol-Whitelist Z. 196-251 um kommissionieren/hauptlagerplatz erweitern, Kopf-/Zellen-Bloecke ergaenzen, gefilterte-Ansicht-Hinweis)"
  - "IdealAkeWms/Views/Picking/Bom.cshtml (nur falls Q6 = ja: colNames-Map Z. 1015 um Spalte kommissionieren erweitern, damit der Druck-Filterhinweis den Spaltennamen kennt)"
  - "docs/TESTSZENARIEN.md (Kapitel TS-70 — BOM-Bridge — um Abschnitt 'Komm.-Ziel-Filter' ergaenzen)"
  - "secondbrain/tests/testszenarien-index.md (TS-70-Eintrag nachziehen)"
open_questions:
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
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
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
statt still filtern") — und zwar fuer **beide** betroffenen Felder (siehe Fachliche Anforderungen 4).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**

1. Der bestehende Text-Spaltenfilter der Spalte `kommissionieren` in `Bom.cshtml` wird durch ein
   Dropdown (Select2, bereits geladen) ersetzt — **nur im hierarchischen Modus** gerendert (Spalte
   existiert im AKE-Flachmodus gar nicht, siehe Fachliche Anforderung 3).
2. Neues Feld `User.DefaultFilterBomKommissionierziel` (Migration 92) nach exakter Vorlage
   `DefaultFilterBomDescription1`, inkl. Benutzereinstellungen (`Users/Edit`, `Account/Profile`) und
   Anwendung in `Bom.cshtml` (`window.setColumnFilter('kommissionieren', ...)`).
3. Sichtbarer Aktiv-Badge + Ein-Klick-Aufhebung fuer einen aktiven Standardfilter — **fuer beide**
   Felder gleichzeitig (`DefaultFilterBomDescription1` UND das neue
   `DefaultFilterBomKommissionierziel`), da die Luecke fuer das bestehende Feld schon heute existiert
   (siehe Fachliche Anforderung 4/Q7-Scope).
4. Verhalten bei gespeichertem Wert ohne Treffer in der aktuellen Stueckliste (Q4, offen — Empfehlung
   siehe unten).
5. Testszenarien-Ergaenzung in TS-70 (BOM-Bridge).

**Out-of-Scope**

- Die Spalte selbst neu bauen oder ins Zahnrad aufnehmen — bereits vorhanden (siehe oben).
- Aenderungen am AKE-Flachmodus — die Spalte/Filterzeile existiert dort nicht und bleibt es
  (`@if (Model.Hierarchical)`-Gate unveraendert).
- Druck/PDF-Durchschlag des Filters — nur bedingt in Scope, abhaengig von Freigabe-Antwort zu Q6
  (siehe Fachliche Anforderung 5); ohne "ja" bleibt `PrintBom.cshtml` unangetastet.
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

2. **Gespeicherter Standardfilter — 1:1-Kopie des bestehenden Musters.** Neues Feld
   `DefaultFilterBomKommissionierziel` (`string?`, `[StringLength(200)]`) an `User`, direkt neben
   `DefaultFilterBomDescription1`. Gleicher Weg: Benutzereinstellungen (`Users/Edit.cshtml`,
   `Account/Profile.cshtml`) → `PickingController.Bom` liest es aus `currentUser` → `BomViewModel`
   → `Bom.cshtml`-`column-preferences-ready`-Handler ruft
   `window.setColumnFilter('kommissionieren', defaultBomKommissionierziel)`, exakt wie
   `defaultBomDescription1` heute schon fuer `description1` tut (Z. 995/998). **Kein neuer
   Mechanismus.**

3. **AKE-Flachmodus bleibt unberuehrt — bereits durch das bestehende Gating sichergestellt.** Spalte
   und Filterzeile fuer `kommissionieren` rendern ausschliesslich unter `@if (Model.Hierarchical)`.
   Ein gespeicherter Default, der via `window.setColumnFilter('kommissionieren', …)` angewandt wird,
   findet im flachen AKE-Modus schlicht **keine** Filterzeile fuer diesen Key — der dokumentierte
   No-op-Pfad (`table-filter.js` ignoriert `setColumnFilter` auf einen nicht existierenden `col-key`,
   siehe Bom.cshtml Z. 980-982 zur allgemeinen Race-Condition-Behandlung) greift, **kein** Leerfiltern
   der AKE-Stueckliste. Diese Aussage ist eine verifizierte Design-Eigenschaft des bestehenden Codes,
   kein neu zu bauendes Verhalten — sie deckt die urspruengliche Backlog-Frage 5 ab, ohne
   Zusatzaufwand.

4. **Aktiver Standardfilter muss sichtbar sein — Pflicht fuer BEIDE Felder.** Hausregel „sichtbar
   machen statt still filtern" (F1 im Backlog). Heute werden `DefaultFilterBeschaffung`,
   `DefaultFilterArtikelgruppe` und `DefaultFilterBomDescription1` beim Laden nur in das jeweilige
   Filterzeilen-Input **vorbelegt** (Z. 993-998) — der Wert ist im Feld sichtbar, aber es gibt
   **keinen** dedizierten „gefiltert"-Hinweis und **keinen** Ein-Klick-Reset. Diese Spec schliesst die
   Luecke **fuer `description1` UND `kommissionieren` gemeinsam** (nicht nur fuer das neue Feld —
   sonst entstuende eine Asymmetrie zwischen zwei gleich behandelten Standardfiltern derselben
   Ansicht). Konkret: ein sichtbarer Hinweis/Badge oberhalb der Tabelle ("Gefiltert: Bezeichnung 1 =
   X" bzw. "Kommissionierziel = Y") mit einem Link/Button, der den jeweiligen Spaltenfilter per
   `window.setColumnFilter(key, '')` zuruecksetzt und `updateBomVisibility()` erneut auslaufen laesst
   (dieselbe Funktion, die der bestehende Wrapper Z. 972-975 schon aufruft).

5. **Druck (Q6, offen).** Der Druck-Handler (Z. 1002-1039) uebergibt aktive Filter bereits als
   `filterInfo` an `PrintBom`, aber die `colNames`-Map (Z. 1015) kennt nur die Spalten 1-8 (bis
   Artikelgruppe) — verifiziert, die hierarchischen Spalten fehlen dort. `PrintBom.cshtml` selbst hat
   **keine feste Whitelist-Liste**, sondern eine generische `ShowCol(key)`-Pruefung (Z. 196) gegen
   `Model.VisibleColumns`; verifiziert, dass `kommissionieren`/`hauptlagerplatz` dort **nirgends** als
   Kopf-/Zellblock auftauchen (Z. 213-251 zeigen ausschliesslich die acht AKE-Basisspalten plus
   Lagerplatz) — die Druckvorlage kennt die hierarchischen Spalten strukturell noch nicht, unabhaengig
   vom `visibleColumns`-Parameter. Falls Q6 mit „ja" beantwortet wird, braucht es: (a) den Spaltennamen
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
   vorkommenden `Kommissionieren`-Werten (distinct, keine feste/veraltete Liste) — vorbehaltlich
   Freigabe-Antwort Q3.
3. Ist die Stueckliste leer oder hat keine Zeile ein Kommissionierziel, zeigt das Dropdown einen
   erkennbaren Leer-Zustand (kein stiller Absturz, keine leere graue Flaeche ohne Erklaerung).
4. Im flachen AKE-Modus (`Model.Hierarchical == false`) existiert weder die Spalte noch die
   Filterzeile noch das Dropdown — unveraendert zum heutigen Verhalten.
5. Ein Benutzer kann in seinen Benutzereinstellungen (bzw. im eigenen Profil) einen
   Standard-Kommissionierziel-Filter setzen; beim naechsten Aufruf der Stueckliste eines
   hierarchischen Auftrags ist der Filter automatisch aktiv.
6. Ein gesetzter Standard-Kommissionierziel-Filter wird beim Aufruf einer **AKE**-Stueckliste
   (flacher Modus) **nicht** angewendet und filtert dort **nichts** leer.
7. Ist ein Standardfilter (Bezeichnung 1 ODER Kommissionierziel) beim Laden der Stueckliste aktiv,
   erscheint ein sichtbarer Hinweis/Badge mit dem gefilterten Wert; ein Klick darauf hebt den
   jeweiligen Filter auf und zeigt wieder alle Zeilen (vorbehaltlich des Baum-Zustands).
8. Enthaelt der gespeicherte Standardwert einen Wert, der in der aktuell geladenen Stueckliste nicht
   vorkommt, filtert die Ansicht **nicht** still auf 0 Zeilen ohne Erklaerung — Verhalten gemaess
   Freigabe-Antwort zu Q4.
9. (Nur falls Q2 = Mehrfachauswahl) Mehrere im Dropdown gewaehlte Kommissionierziele werden per
   OR verknuepft (jede Zeile mit einem der gewaehlten Ziele bleibt sichtbar) und komma-separiert im
   Benutzerprofil gespeichert.
10. (Nur falls Q6 = ja) Ein beim Druckaufruf aktiver Kommissionierziel-Filter erscheint im
    Ausdruck (`PrintBom`) als Filterhinweis im Kopfbereich, und die Spalte `Komm.-Ziel` erscheint im
    Ausdruck nur, wenn sie auf dem Bildschirm sichtbar war (`visibleColumns`-Parameter).
11. Migration 92 laeuft auf einer bestehenden Datenbank fehlerfrei und idempotent (zweimaliges
    Ausfuehren von `SQL/92_*.sql` aendert nichts am zweiten Lauf).

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
  auf einen in der aktuellen Liste nicht vorkommenden Wert setzen, Stueckliste oeffnen → Verhalten
  gemaess Freigabe-Antwort Q4 (kein stilles 0-Zeilen-Ergebnis ohne Hinweis).
- Bestehenden Standardfilter „Bezeichnung 1" pruefen: Badge erscheint jetzt ebenfalls sichtbar (Nachweis
  der geschlossenen Luecke aus Fachlicher Anforderung 4/Q7).
- (Falls Q2 = Mehrfachauswahl) zwei Kommissionierziele waehlen → Zeilen beider Ziele bleiben
  sichtbar (OR), Speichern im Profil → kommasepariert im Feld.
- (Falls Q6 = ja) Mit aktivem Kommissionierziel-Filter drucken → Ausdruck zeigt Filterhinweis im
  Kopf; ohne aktiven Filter → kein Hinweis, vollstaendige Liste.
- (Falls Q6 = nein) Mit aktivem Kommissionierziel-Filter drucken → Ausdruck zeigt unveraendert die
  vollstaendige, ungefilterte Stueckliste (dokumentiertes Verhalten, kein Bug).

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

## Offene Rueckfragen

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

1. →
2. →
3. →
4. →
5. →
