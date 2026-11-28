---
type: spec
title: BF-Warenkorb – Artikelsuche mit Vorschlägen beim Tippen (Type-ahead)
slug: 2026-11-19-bf-warenkorb-artikelsuche-type-ahead-bug-spec
status: Gemerged
created: 2026-11-19
updated: 2026-11-28
source_backlog: "[[2026-11-19-bugfix-bf-warenkorb-artikelsuche-type-ahead]]"
bug: "[[2026-11-19-bf-warenkorb-artikelsuche-type-ahead-bug]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: IDEAL
version_vorschlag: "1.99.46 (1.99.45 von [[2026-11-19-updater-warntext-treffer-exitcode-spec]] vorgeschlagen; Dev-Lauf prüft erneut)"
testszenario_vorschlag: "TS-169 (TS-165 BDE mobil, TS-167 Updater, TS-168 Artikelinfo reserviert; Dev-Lauf prüft erneut)"
affected_code:
  - "IdealAkeWms/Views/BfAuftraege/Details.cshtml:63-75 (Suchformular: Kommentar, input#q list=, neues <datalist>)"
  - "IdealAkeWms/Views/BfAuftraege/Details.cshtml:90-123 (Trefferzeile: autofocus auf Hinzufügen der passenden Zeile)"
  - "IdealAkeWms/Views/BfAuftraege/Details.cshtml:344-347 (Scripts-Section: neues Skript einbinden)"
  - "IdealAkeWms/wwwroot/js/bf-artikelsuche.js (NEU, Muster wwwroot/js/fa-feld.js:68-89)"
  - "IdealAkeWms/Controllers/Api/BfAuftraegeApiController.cs:19-26 (unverändert genutzt; nur Doku-Kommentar)"
  - "IdealAkeWms/Data/Repositories/BfAuftragRepository.cs:43-68 (unverändert: SucheArtikelAsync + Lagerteile() = der eine Filter)"
  - "IdealAkeWms.Tests/Services/BfAuftragWorkflowTests.cs:104-118 (neuer Test daneben: Suche mit Begriff)"
  - "IdealAkeWms.Tests/Controllers/BfAuftraegeApiControllerTests.cs (NEU)"
  - "IdealAkeWms/Views/Help/Index.cshtml:2280-2283 + Views/Help/Changelog.cshtml"
  - "docs/anwender/14-blechfertigung-bf-auftraege.md:35-38 + docs/anwender/README.md (Versionstabelle)"
  - "docs/TESTSZENARIEN.md (neues Kapitel TS-169, Querverweis aus TS-157 Z. 12944)"
  - "IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs"
  - "scripts/deploy/nachlauf.psd1 (ein Info-Eintrag)"
open_questions:
  - "1. Scan ins Suchfeld startet die Suche sofort (synthetisches Enter des Scanners löst heute keinen Submit aus) – so gewollt? Empfehlung: ja."
  - "2. Nach der Suche Fokus auf den Knopf „Hinzufügen“ der passenden Trefferzeile (Enter fügt mit vorbelegter Menge hinzu, nächster Scan landet weiter im Suchfeld) statt auf dem Mengenfeld? Empfehlung: Knopf."
  - "3. Falls der Terminal-Browser in der nativen Vorschlagsliste Treffer über Matchcode/Bezeichnung nicht anzeigt (nur über die Artikelnummer): im selben Lauf auf eine eigene Liste nach Muster Lagerbestellung umstellen, ohne neue Freigabe? Empfehlung: ja."
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: "Rückfragen 1–3 wie Empfehlung (Gerald 2026-11-19 20:40); freigegeben 20:38"
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-19"
---

## Ziel / Nutzen (das Warum)
Besteller im BF-Warenkorb (IDEAL) sollen beim Tippen passende Blechteile vorgeschlagen bekommen, wie in den anderen Artikelmasken. Heute
kommen Treffer erst nach „Suchen“ und Neuladen. Bug-Datensatz: [[2026-11-19-bf-warenkorb-artikelsuche-type-ahead-bug]].

Root Cause belegt: Die Ursprungs-Spec [[2026-11-13-blechfertigung-bf-auftraege-wicam-csv-spec]] (Z. 279-282) sah den Endpunkt
`GET api/bf-auftraege/artikel` vor. Er ist gebaut (`BfAuftraegeApiController.cs:19-26`), wird aber von keiner View und keinem Skript
aufgerufen. Eine Suche nach `api/bf-auftraege` / `BfAuftraegeApi` im Worktree findet nur die Klasse selbst (Z. 10, 13, 17). Die View ist
bewusst reine Server-Suche (`Details.cshtml:63` „kein eigenes JS“).

## Umfang (In-Scope / Out-of-Scope)
**In-Scope**
- Vorschlagsliste am bestehenden Suchfeld `#q` des BF-Warenkorbs über den **vorhandenen** Endpunkt `GET api/bf-auftraege/artikel`.
- Scan (Hardware-Scanner mit Enter-Suffix) ins Suchfeld startet die Server-Suche (Rückfrage 1).
- Nach der Suche liegt der Fokus auf „Hinzufügen“ der passenden Trefferzeile, Enter fügt hinzu (Rückfrage 2).
- Tests, Hilfe, Anwender-Changelog, Anwenderanleitung Kap. 14, TS-169, Version.

**Out-of-Scope**
- Jede Änderung am Filter (Lagerartikel + Artikeltyp BG/FT) oder an `SucheArtikelAsync`. Der Filter bleibt genau einmal in
  `BfAuftragRepository.Lagerteile()` (`BfAuftragRepository.cs:59-68`).
- `api/articles/search` und `_Select2ArticlePartial` (Begründung im Variantenvergleich unten). Zusatzfund ohne Auftrag: Die Klasse
  `ArticlesApiController` hat keinen eigenen Zugriffsfilter (`ArticlesApiController.cs:8-10`). Nur gemeldet, nicht Teil dieses Fixes.
- Hinzufügen per AJAX ohne Neuladen; Positionen bleiben POST-Formulare (`Details.cshtml:106-122`).
- AKE: das Modul ist dort aus (siehe Fachliche Anforderungen, Standort).

## Fachliche Anforderungen
- **F1 Vorschläge beim Tippen:** ab 2 Zeichen, 300 ms nach dem letzten Tastendruck, bis 20 Vorschläge. Jeder Vorschlag zeigt Artikelnummer
  und „Matchcode · Bezeichnung“. Es erscheinen **nur** Artikel, die auch die Server-Suche findet (Sage-Lagerartikel mit Artikeltyp BG/FT),
  weil beide denselben Repository-Aufruf nutzen.
- **F2 Vorschlag übernehmen:** Wählen eines Vorschlags schreibt die Artikelnummer ins Suchfeld und startet die Server-Suche. Meldet ein
  Browser die Auswahl nicht als eigenes Ereignis, genügt Enter.
- **F3 Server-Suche bleibt der Weg zum Hinzufügen:** Die Trefferzeile mit Menge, Status, Lagerplatz und „Hinzufügen“ kommt weiter vom Server
  (`Details.cshtml:76-128`). Keine zweite Darstellung der Trefferzeile in JavaScript.
- **F4 Enter → Hinzufügen:** Nach der Suche hat der Knopf „Hinzufügen“ den Fokus. Gewählt wird die Zeile, deren Artikelnummer exakt dem
  Suchbegriff entspricht (ohne Groß-/Kleinschreibung, getrimmt), sonst die Zeile bei genau einem Treffer, sonst keine. Enter fügt mit der
  vorbelegten Menge hinzu (Bestellmenge bzw. 1, `Details.cshtml:107-108`). Enter im Mengenfeld fügt wie heute hinzu.
- **F5 Scanner:** Ein Scan ohne Fokus landet wie heute im einzigen `[data-hw-scan-ziel]` = `#q` (`hardware-scanner.js:290-295`). Mit
  Enter-Suffix startet er die Suche (F1-Liste ist dabei egal). Liegt der Fokus auf „Hinzufügen“ (F4), ist das kein freies Feld
  (`hardware-scanner.js:228-232`), der nächste Scan geht also wieder ins Suchfeld.
- **F6 Ohne JavaScript:** Suchfeld + „Suchen“ funktionieren unverändert (GET-Formular `Details.cshtml:67-75`). F4 gilt auch ohne JS, weil
  `autofocus` serverseitig gerendert wird.
- **F7 Fehler:** Antwortet der Endpunkt nicht oder mit Fehler (403/404/500), bleibt die Liste leer. Keine Meldung, die Server-Suche bleibt
  nutzbar. Begründung: Vorschläge sind ein Komfort. Fehlende Rechte/Gate zeigt die Seite selbst schon durch dieselben Filter.
- **Standort:** IDEAL. Belegt: BF-Seite und Endpunkt hängen am Gate `RequireBlechfertigungAktiv` (`BfAuftraegeController.cs:19`,
  `BfAuftraegeApiController.cs:12`). Das Gate greift nur bei `BlechfertigungAktiv` = „true“ (`RequireBlechfertigungAktivAttribute.cs:23`),
  Default aus (`AppSettingKeys.cs:138`). Auf AKE bleibt alles unsichtbar.

## Technischer Loesungsentwurf

### Varianten für den Endpunkt (Vorgabe Gerald: genau ein Filter)
| | A: `api/articles/search` erweitern | **B: vorhandenen `GET api/bf-auftraege/artikel` nutzen (Empfehlung)** |
|---|---|---|
| Filter | müsste den BF-Filter als dritten Modus neben `type` aufnehmen (`ArticlesApiController.cs:26-49`); filtert heute **im Speicher** nach `SearchAsync(limit*5)` | ruft `IBfAuftragRepository.SucheArtikelAsync` (`BfAuftraegeApiController.cs:21`) – **dieselbe Methode** wie die Server-Suche (`BfAuftraegeController.cs:112`), Filter in SQL (`BfAuftragRepository.cs:58-68`) |
| Zugriff | Klasse ohne `Require…`-Filter (`ArticlesApiController.cs:8-10`); BF-Gate/Rolle müsste nachgezogen werden, ohne die 10 anderen Aufrufer zu stören | Class-Level `[RequireBfBestellenAccess]` + `[RequireBlechfertigungAktiv]` (`BfAuftraegeApiController.cs:11-12`) = exakt die Filter der Seite (`BfAuftraegeController.cs:18-19`); API-Zweig liefert 403 bzw. 404 (`RequireSchachtlerAccessAttribute.cs:47-49`, `RequireBlechfertigungAktivAttribute.cs:33-36`) |
| Andere Aufrufer | `_Select2ArticlePartial.cshtml:18` (eingebunden in 9 Views, u. a. `Beschichtungsauftraege/Details.cshtml:439`, `StockMovements/Inbound.cshtml:118`) + direkt `beschichtung-frei.js:39`, `InboundBulk.cshtml:154`, `WarehouseRequisitions/Edit.cshtml:161` | keine |
| Code-Änderung Backend | ja | **keine** |

**Empfehlung B.** Der „schmale Endpunkt unter `api/bf-auftraege`“ existiert schon und erfüllt die Vorgabe „Filter genau einmal“ bereits:
Server-Suche und Type-ahead laufen durch `SucheArtikelAsync` → `Lagerteile()`. Auch `Hinzufuegen` prüft über denselben Filter
(`BfAuftragWorkflow.cs:78-79` → `GetArtikelAsync`, `BfAuftragRepository.cs:55-56`). Variante A würde den Filter ein zweites Mal
(im Speicher) schreiben, und die BF-Spec hat genau das abgelehnt (Kommentar `BfAuftragRepository.cs:58`).

Hinweis zum Zugriff: Die Seite rechnet Treffer nur für den Eigentümer eines Entwurfs (`BfAuftraegeController.cs:112`), der Endpunkt prüft
das nicht. Das ist unkritisch, weil er nur Artikelstammdaten liest, die jeder mit BF-Recht über die Seite ebenso suchen kann. Keine
Änderung.

### Warum nicht `_Select2ArticlePartial`
Der Baustein ruft fest `/api/articles/search` (`_Select2ArticlePartial.cshtml:18`) und erwartet `{id, text}` mit Artikel-**Id**
(`ArticlesApiController.cs:50-54`). Select2 ersetzt außerdem das `<input>` durch ein `<select>`. Dann wäre das GET-Formular ohne JS
kaputt (F6), und der Scanner-Weg über `data-hw-scan-ziel` + synthetisches Enter (`hardware-scanner.js:240-246`) liefe ins Leere (vgl.
[[fallstricke]] „Ein Dropdown auf einer Client-Mode-Filterspalte: Input als Quell-Element behalten“). Der Baustein wird darum nicht
verwendet.

### UI: natives `<datalist>` am bestehenden Feld (Muster `fa-feld.js`)
Das Projekt hat schon ein Type-ahead über eine native Vorschlagsliste: `wwwroot/js/fa-feld.js:68-89` (`<datalist id="faVorschlaege">`,
`api/stock/fa-search`). Diese Stufe der Leiter (Plattform-Mittel) behält das `<input>`. Scanner, Enter und GET-Formular bleiben, und es
kommt keine Bibliothek dazu.

1. `Details.cshtml:63`: Kommentar anpassen (Server-Suche + Vorschläge über `api/bf-auftraege/artikel`, gleicher Filter).
2. `Details.cshtml:70`: `list="bfArtikelVorschlaege"` am `#q`; direkt darunter `<datalist id="bfArtikelVorschlaege"></datalist>`.
   `autocomplete="off"` und `data-hw-scan-ziel` bleiben.
3. Neue Datei `wwwroot/js/bf-artikelsuche.js` (IIFE, ~30 Zeilen, Muster `fa-feld.js`), nur geladen bei `Model.BestellerBearbeitet`
   (`Details.cshtml:344-347`):
   - `input`: Debounce 300 ms, ab 2 Zeichen `fetch('/api/bf-auftraege/artikel?q=…&limit=20', { credentials: 'same-origin' })`.
     Laufnummer gegen veraltete Antworten (wie `seq` in `fa-feld.js:56-60`). Optionen per `document.createElement('option')`,
     `value = articleNumber`, `textContent = [matchcode, description]` mit „ · “ verbunden. Nie `innerHTML` mit Daten. Fehler → Liste leeren (F7).
   - `input` mit `e.inputType === 'insertReplacementText'` (Auswahl aus der Liste) → `form.requestSubmit()` (F2).
   - `keydown` Enter mit `!e.isTrusted` (synthetisches Enter des Hardware-Scanners, `hardware-scanner.js:245`) → `preventDefault()` +
     `form.requestSubmit()` (F5). Echte Tastatur-Enter bleiben beim Browser (nativer Submit bzw. Listenauswahl), deshalb kein Eingriff.
     Dasselbe Muster gibt es schon: Seiten-Enter-Handler werten das synthetische Enter aus ([[fallstricke]] „Hardware-Scanner:
     unsichtbares Fokusfeld …“).
4. `Details.cshtml:90-123`: einmal vor der Schleife die Fokus-Zeile bestimmen (exakter Artikelnummer-Treffer, sonst einziger Treffer, F4).
   Am Knopf `Hinzufügen` (Z. 122) `autofocus="@(t.ArticleNumber == fokusNr)"`. Razor lässt ein `false`-Boolean-Attribut weg.
   Kein JS nötig.

**frontend-design-Pflicht:** Die Umsetzung ändert View und Oberflächen-JS. Darum den Skill `frontend-design` **explizit** aufrufen
(CLAUDE.md). Leitplanken: Bootstrap-Standard des Feldes bleibt, keine neue UI-Bibliothek, Fokusrahmen am „Hinzufügen“-Knopf gut sichtbar
(WCAG AA, Fertigungsterminals).

### Ponytail
Wiederverwendet: Endpunkt, Repository-Methode, Filter, Zugriffsattribute, `fa-feld.js`-Muster, natives `<datalist>`, Server-Trefferzeile.
Weggelassen: neuer Endpunkt, Select2, JS-Trefferzeile, AJAX-Hinzufügen, Fehlermeldung bei Vorschlagsfehler (F7).

## Migrations-/SQL-Auswirkungen
Keine. Kein Model, keine Migration, kein `SQL/XX_*.sql`, `00_FreshInstall.sql` und `SQL/AgentJobs/` unberührt. Nächste freie SQL-Nummer
bleibt 164 (höchste vorhanden `SQL/163_AddBfPositionArbeitsschritte.sql`).

## Einstellungen
keine

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

Bestehend und unverändert: `BlechfertigungAktiv` (Gate) und die Sage-Benutzerfelder der Blechfertigung. Ohne „Ist Lagerartikel“ und
„Artikeltyp“ bleiben auch die Vorschläge leer. Die Warnung dazu steht schon über der Suche (`Details.cshtml:30-37`).

## Audit-Feld-Auswirkungen
Keine. Der Endpunkt liest nur (`AsNoTracking`, `BfAuftragRepository.cs:63`). Hinzufügen läuft unverändert über `AddItem` →
`BfAuftragWorkflow.HinzufuegenAsync` (`BfAuftraegeController.cs:121-124`, `BfAuftragWorkflow.cs:74`).

## Akzeptanzkriterien
1. Im BF-Warenkorb erscheint nach Eingabe von mindestens 2 Zeichen innerhalb von etwa 1 s eine Vorschlagsliste. Bei 1 Zeichen erscheint keine.
2. Die Vorschläge enthalten nur Artikel, die die Server-Suche mit demselben Begriff auch findet. Ein Artikel mit Artikeltyp ≠ BG/FT oder
   „Ist Lagerartikel“ leer/0 erscheint in keinem der beiden.
3. Matchcode und Bezeichnung sind in jedem Vorschlag sichtbar. Ein Begriff, der nur in der Bezeichnung vorkommt, liefert Vorschläge
   (siehe Rückfrage 3).
4. Auswahl eines Vorschlags führt zur Trefferzeile dieses Artikels. Enter genügt, falls der Browser die Auswahl nicht selbst absendet.
5. Nach einer Suche mit exakter Artikelnummer (auch in anderer Groß-/Kleinschreibung) hat „Hinzufügen“ dieser Zeile den Fokus. Enter
   legt die Position mit der vorbelegten Menge an (Erfolgsmeldung, Position in der Tabelle).
6. Bei mehreren Treffern ohne exakten Treffer hat kein Knopf den Fokus.
7. Hardware-Scan (Enter-Suffix) ohne Fokus in einem Feld: Code steht im Suchfeld, die Suche startet ohne weiteren Tastendruck.
8. Hardware-Scan mit Fokus auf „Hinzufügen“: Der Code landet im Suchfeld, nicht in einem Mengenfeld, und die Suche startet.
9. Mit abgeschaltetem JavaScript: Suchen über Feld + „Suchen“/Enter funktioniert wie vorher. AK 5 gilt ebenfalls.
10. Benutzer ohne BF-Recht: `GET /api/bf-auftraege/artikel?q=xx` → 403. Bei `BlechfertigungAktiv` ≠ true → 404 (für jeden Benutzer).
11. `ArticlesApiController` und `_Select2ArticlePartial.cshtml` sind im Diff unverändert. `BfAuftragRepository.SucheArtikelAsync`/`Lagerteile()`
    ist inhaltlich unverändert.
12. Build und alle Tests grün, darunter die neuen Tests (siehe Test-Szenarien).

## Test-Szenarien

### Automatisiert (EF InMemory)
- **`BfAuftragWorkflowTests.Artikelsuche_MitSuchbegriff_NurLagerartikelBgFt`** (neben Z. 104-118, gleiche `Fx`-Hilfe): Artikel BG1, FT1
  (typ FT), ZK1 (typ ZK), NL1 (ist „0“), OHNE (ist null) – alle mit Bezeichnung „Teil …“. `SucheArtikelAsync("Teil", 50)` → genau BG1,
  FT1. `SucheArtikelAsync("MC ZK1", 50)` → leer. Deckt den `q`-Zweig ab, den der bestehende Test (q = null) nicht prüft.
- **`BfAuftraegeApiControllerTests`** (neu):
  - `Artikel_LiefertDieselbenArtikelWieDieServerSuche`: echter `BfAuftragRepository` auf InMemory, gleiche Artikel wie oben. `Artikel("Teil", 20)`
    → `OkObjectResult`, Artikelnummern = `repo.SucheArtikelAsync("Teil", 20)`. Belegt „ein Filter“ (AK 2).
  - `Zugriff_GleicheFilterWieDieSeite`: per Reflection tragen `BfAuftraegeApiController` und `BfAuftraegeController` beide
    `RequireBfBestellenAccessAttribute` und `RequireBlechfertigungAktivAttribute` auf Klassenebene (AK 10, Wächter gegen Auseinanderlaufen).
- Nicht InMemory-testbar und darum Manual-UAT: Datalist-Darstellung, `requestSubmit`, Scanner-Ereignisse, `autofocus`, SQL-Kollation
  (Groß-/Kleinschreibung der Contains-Suche).

### Manuell – neues Kapitel TS-169 in `docs/TESTSZENARIEN.md`
**TS-169 — BF-Warenkorb: Artikelsuche mit Vorschlägen, Scanner, Enter (v1.99.46)**, Standort **IDEAL-DEV**, keine Migration. Querverweis aus
TS-157 (Z. 12944) ergänzen.

*Vorbedingungen:* `BlechfertigungAktiv` = true. Sage-Benutzerfelder der Blechfertigung konfiguriert (keine gelbe Warnung über der Suche).
Benutzer A mit Lager-Bestellrecht (kein `schachtler`), Benutzer B ohne BF-Recht. Hardware-Scanner mit Enter-Suffix aktiv (Profil).
Drei Testartikel per Prüfabfrage auf der **WMS-DB IDEAL-DEV** bestimmen:
```sql
SELECT TOP 20 a.ArticleNumber, a.Matchcode, a.Description, d.SyncFieldName, v.TextValue
FROM Articles a
JOIN ArticleAttributeValues v ON v.ArticleId = a.Id
JOIN ArticleAttributeDefinitions d ON d.Id = v.ArticleAttributeDefinitionId
WHERE d.SyncSource = 'Sage' AND d.SyncFieldName IN (<BfArtikelFelder.IstLagerartikel>, <BfArtikelFelder.Artikeltyp>)
ORDER BY a.ArticleNumber;
```
Die Platzhalter durch die Konstanten aus `IdealAkeWms/Services/BfArtikelFelder.cs` ersetzen. Spalten- und Tabellennamen vor dem Lauf gegen
`SQL/00_FreshInstall.sql` prüfen. Gewählt werden: **X** = Lagerartikel, Typ BG; **Y** = Typ z. B. ZK (kein BG/FT); **Z** = Typ BG,
aber „Ist Lagerartikel“ 0/leer.

*Schritte und erwartetes Verhalten:*
1. A öffnet *Blechfertigung → BF-Warenkorb*, tippt 1 Zeichen → keine Liste. 2 Zeichen der Nummer von X → Liste mit X (Nummer + Matchcode ·
   Bezeichnung) (AK 1, 3).
2. Teil der **Bezeichnung** von X tippen → X in der Liste (AK 3; scheitert das, Rückfrage 3 anwenden und Befund notieren).
3. X wählen → Seite zeigt die Trefferzeile X, „Hinzufügen“ hat den sichtbaren Fokus. Enter → Erfolgsmeldung, Position X mit vorbelegter
   Menge in der Tabelle (AK 4, 5).
4. Nummer von X in Kleinbuchstaben eintippen, Enter → wie 3 (AK 5).
5. Kurzen Begriff mit mehreren Treffern suchen → mehrere Zeilen, kein Knopf fokussiert (AK 6). In einer Zeile Menge ändern, Enter im
   Mengenfeld → Position mit dieser Menge (F4).
6. Ohne Fokus (Klick auf freie Seitenfläche) Barcode von X scannen → Code im Suchfeld, Suche startet allein (AK 7). Danach (Fokus auf
   „Hinzufügen“) gleich nochmal scannen → Code im Suchfeld, nicht im Mengenfeld, neue Suche (AK 8).
7. Browser: JavaScript aus → Suchfeld + „Suchen“ liefert X, „Hinzufügen“ fokussiert (AK 9). JavaScript wieder an.

*Negativfälle:*
8. Nummer bzw. Matchcode von **Y** tippen → kein Vorschlag. Enter/Suchen → „Kein Lagerartikel (Artikeltyp BG oder FT) …“ (AK 2).
9. Nummer von **Z** tippen → kein Vorschlag, Server-Suche ebenfalls ohne Treffer (AK 2).
10. Als B im selben Browser `/api/bf-auftraege/artikel?q=<X>` aufrufen → 403 (AK 10). Als admin `BlechfertigungAktiv` aus → dieselbe URL
    → 404, Menü weg. Danach wieder an.
11. Netzwerk-Fehler simulieren (DevTools offline) und tippen → keine Liste, keine Fehlermeldung. Online „Suchen“ funktioniert (F7).
12. Regression: Schachtler öffnet einen bestellten Auftrag → keine Suchkarte, kein Skript-Fehler in der Konsole (Skript nur bei
    `BestellerBearbeitet`).

Danach `secondbrain/tests/testszenarien-index.md` nachziehen.

## Etappen (nur bei epic: true)
Entfällt (kein Epic).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy
*Vorläufig. Der QA-Lauf bestätigt es am echten Diff.*

- **Web-App:** ja (View, neues JS, Hilfe/Changelog).
- **Service:** nein. `IDEALAKEWMSService/AppVersion.cs` wird nur für die Versionsgleichheit mitgezogen, ein Dienst-Deploy ist fachlich nicht nötig.
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022), genau einer:
  `@{ Id='v1.99.46-BfWarenkorbVorschlaege-Info'; Version='1.99.46'; Typ='Info'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'`
  `Text='BF-Warenkorb: Artikelsuche schlaegt beim Tippen vor (ab 2 Zeichen, nur Lagerartikel BG/FT). Scan ins Suchfeld sucht sofort, danach fuegt Enter den Artikel hinzu. Besteller informieren.'`
  `Quelle='<Brain-Changelog-Slug v1.99.46>' }`
  Keine Schema-, Daten-, Einmal-, Pruefungs- oder Einstellungs-Einträge.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
- **Version:** v1.99.46 in **beiden** `AppVersion.cs` (1.99.45 ist von [[2026-11-19-updater-warntext-treffer-exitcode-spec]] vorgeschlagen,
  [[2026-11-19-artikelinfo-bestand-exakt-bug-spec]] hat noch keine feste Nummer). Der Dev-Lauf prüft die Nummer gegen den Stand im Bündel.
- **Doku-Pflichten:** `Views/Help/Changelog.cshtml` (Eintrag v1.99.46), `Views/Help/Index.cshtml:2280-2283` (Warenkorb: Vorschläge,
  Scan sucht sofort, Enter fügt hinzu), `docs/anwender/14-blechfertigung-bf-auftraege.md:35-38` (Schritt 2/3 neu formulieren: tippen →
  Vorschlag wählen oder scannen → Enter) + Zeile v1.99.46 in `docs/anwender/README.md` (Versionstabelle ab Z. 59). Dazu Brain-Changelog,
  [[feature-map]] und Bug-Datensatz auf `behoben`.

**Final (QA 2026-11-19, aus dem echten Diff `f1747e10..f3cdd143`):**
- web: **ja** (Änderungen unter `IdealAkeWms/`).
- service: **ja, nur Versionsnummer** (`IDEALAKEWMSService/AppVersion.cs` → 1.99.45, damit Web und Dienst denselben Stand melden; keine Dienst-Logik geändert).
- migration: **nein** (keine Datei unter `*/Migrations/`, kein neues `SQL/`-Skript, kein Einmal-Skript).
- Paket fertig publiziert: `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` (IDEALAKEWMSWeb + IDEALAKEWMSWebService + SQL + deploy), `publish-info.txt` Version 1.99.45, Commit `f3cdd14397d69935c974a25eaaeb045c9d10cacb`.
- Ablauf Mensch: Paket aufs Testsystem → `deploy\Update-IdealAkeWms.ps1` → testen → mergen. Nach dem Merge nur dann aus main neu publizieren, wenn der Merge getestete Dateien mit parallelen main-Änderungen zusammengeführt hat.
- Reihenfolge: kein Schema, kein Daten-Skript → kein zusätzlicher Backup-/Skript-Schritt aus diesem Paket; Web und Dienst wie üblich über das Update-Skript.
- Manifest (`scripts/deploy/nachlauf.psd1`): `v1.99.45-BfWarenkorbVorschlaege` (Info, IDEAL) – im QA-Fix f3cdd143 ergänzt (F1).

## Offene Rueckfragen
1. **Scan startet die Suche sofort?** Das synthetische Enter des Hardware-Scanners löst heute keinen Submit aus (`hardware-scanner.js:244-245`).
   Ein Scan füllt also nur das Feld. Mit F5 sucht ein Scan sofort. Empfehlung: **ja**, sonst muss nach jedem Scan noch Enter gedrückt
   werden. Das ist auch das bestehende Verhalten der BDE-Seiten.
2. **Fokus nach der Suche auf „Hinzufügen“ statt auf „Menge“?** Mit Fokus auf dem Knopf fügt Enter mit der vorbelegten Menge hinzu, und der
   nächste Scan landet im Suchfeld (ein Knopf ist kein freies Feld, `hardware-scanner.js:228-232`). Mit Fokus auf dem Mengenfeld kann man
   die Menge sofort tippen, aber ein Scan würde **in die Menge eingefügt**. Empfehlung: **Knopf**.
3. **Ausweichweg, falls die native Liste nur nach Artikelnummer filtert:** Chromium und Firefox vergleichen den Eingabetext nach heutigem
   Stand auch mit dem sichtbaren Text der Option. Am Terminal-Browser ist das ungeprüft (TS-169 Schritt 2). Zeigt der Test keine Treffer
   über Matchcode/Bezeichnung, soll der Dev-/QA-Lauf dann im selben Lauf auf eine eigene Bootstrap-Liste nach Muster Lagerbestellung
   (`WarehouseRequisitions/Edit.cshtml:78-79, 153-177`, gleicher Endpunkt, Auswahl → Submit) umstellen, ohne neue Freigabe?
   Empfehlung: **ja**.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)
<!--
  Von der Koordination eingetragen, wortgetreu, je Antwort:
  <Nr>. <Name>, <yyyy-mm-dd>, [<Jira-Schluessel> Kommentar](<Link>): "<Antwort>"
-->

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Scan ins Suchfeld startet die Suche sofort (Empfehlung ja) → **ja** (Gerald 2026-11-19 20:40)
2. Fokus nach der Suche auf „Hinzufügen“ statt auf „Menge“ (Empfehlung Knopf) → **ja, Fokus auf „Hinzufügen“** (Gerald 2026-11-19 20:40)
3. Ausweichweg eigene Liste ohne neue Freigabe, falls die native Liste nicht nach Bezeichnung zeigt (Empfehlung ja) → **ja, Ausweichweg eigene Liste ohne neue Freigabe erlaubt** (Gerald 2026-11-19 20:40)

## QA-Nachweis (qa-agent, 2026-11-19, Sammellauf v1.99.45)

Unabhängiger Lauf, synchron, nicht delegiert. Worktree `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`.
Geprüfter Commit-Bereich `f1747e10..HEAD` (a1ba945d Spec 1, 980beee9 Spec 2, b524d262 Spec 3, 0df40eeb Spec 4, defa0c05 Release, 947bb014 Doku) plus QA-Fix **f3cdd143**.

### 1. Build und Tests (selbst ausgeführt, nach dem QA-Fix wiederholt)
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 30 Warnungen (Bestand, u. a. CS8321 FaWorklist, CS8620 BfAuftragWorkflowTests:388).
- `dotnet test`: IdealAkeWms.Tests **3390 bestanden / 1 übersprungen / 0 Fehler** (3391); IDEALAKEWMSService.Tests **790 / 0 / 0**.

### 2. Review (eigener Lauf, Diff samt Umfeld gelesen)
Befunde des Sammellaufs – alle **nicht blockierend**, alle in **f3cdd143** behoben, danach Build und Tests erneut grün, DEV neu deployt:
- **F1 (Spec BF-Warenkorb):** Manifest-Info-Eintrag fehlte, obwohl die Spec genau einen verlangt → `v1.99.45-BfWarenkorbVorschlaege` (Info, IDEAL, Nachher) ergänzt.
- **F2 (Spec Artikelinfo / Release):** Anwender-Changelog legte nahe, Kommissionierwagen zählten in der Artikelinfo nicht mehr – falsch, dort zählen sie weiter (Rückfrage 2 betrifft nur den Lagerplatz-Text der Lager-/Glasbestellung) → Satz korrigiert.
- **F3 (Spec Updater):** `Invoke-Pruefung` gab bei reinen Anzeige-Prüfungen erst die gelbe Zeile „Reparatur nach Bestaetigung“ und danach die Warnung „nur Anzeige“ aus – widersprüchlich → Anzeige-Prüfung kehrt jetzt vor der gelben Zeile zurück (Ergebnis/Exit unverändert, Parse-Check 0 Fehler).

Beobachtungen ohne Befund: Dashboard ohne SVG-Symbole (bewusste Vereinfachung); Kongruenztest T2 prüft Einzelrollen (Regeln monoton, ausreichend); Abschnitt nutzt `aria-label` statt `aria-labelledby` (geringfügig); `limit` von `api/bf-auftraege/artikel` ohne Obergrenze (Bestand vor diesem Diff); `StockLocationInfo.LagerplatzText` sortiert `ThenBy(Code)` kulturabhängig (geringfügig).

Gezielt geprüfte Fehlerklassen: kein `@Html.Raw` in `<script>` (bf-artikelsuche.js baut Optionen per `createElement`); kein `hidden` neben `d-*`; keine stillen Ersatzwerte (leere Artikelnummer → leerer Text ohne Repository-Aufruf); keine Parallel-Array-Bindung; keine reine Browser-Validierung (Suche bleibt serverseitig `SucheArtikelAsync`); Sichtbarkeitsregel je Menüziel an EINER Stelle (`NavigationSicht`, ADR 0034). Menü-zu-Filter-Kongruenz: alte Menübedingungen aus `_Layout.cshtml` gegen die Regeln in `NavigationKatalog.cs` einzeln verglichen – gleichwertig, einzige Neuerung „Artikelinfo“ unter Stammdaten.

### 5. CLAUDE.md-Checkliste
Migration/SQL/FreshInstall/AgentJobs: entfällt (kein Schema). Audit-Felder: entfällt (keine Entität geändert). Version 1.99.45 in beiden `AppVersion.cs` + `Views/Help/Changelog.cshtml`: ja. `docs/TESTSZENARIEN.md` TS-167…170 im Worktree, Testindex im Hauptcheckout Zeilen 167–170: ja. Hilfeseite, Anwender-/Betriebsdoku (Kapitel 18 mit Fotos): ja. 5a Einstellungen (ADR 0020): keine neue oder geänderte Einstellung.

### 6. DEV-Deploy
Nach dem QA-Fix aus dem Worktree, erhöhte Shell, Web und Dienst je Standort: `dev-deploy.ps1 -Standort AKE` Exit 0, „HTTP 200, Fusszeile v1.99.45“; `-Standort IDEAL` Exit 0, Fusszeile v1.99.45. DEV-Schalter nur gelesen, nichts umgestellt, nichts an Sage geschrieben.

### 7. Publish
`C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\IDEALAKEWMSWeb` und `\IDEALAKEWMSWebService` vorher geleert, beide `dotnet publish -c Release` Exit 0; `SQL\` → `publish\SQL`, `scripts\deploy\` → `publish\deploy`; `publish-info.txt`: Version 1.99.45, Commit `f3cdd14397d69935c974a25eaaeb045c9d10cacb`, Branch feature/2026-08-07-ideal-teile-1-5, Spec 2026-11-19-v1-99-45-sammellauf, Erstellt 2026-11-19 21:19, Komponenten IDEALAKEWMSWeb + IDEALAKEWMSWebService.

### 3. Nachweisart je AK
| AK | Nachweis |
|---|---|
| 1 | DEV IDEAL: Tippen „blec“ → ein `GET /api/bf-auftraege/artikel?limit=20&q=blec` 200 nach Entprellen; 1-Zeichen-Grenze und Listenanzeige Manual-UAT (Browser-JS) |
| 2 | Auto `ApiArtikel_LiefertDieselbenArtikelWieDieServerSuche`, `Artikelsuche_MitSuchbegriff_NurLagerartikelBgFt` |
| 3 | Manual-UAT (native `<datalist>` je Browser, Rückfrage 3) |
| 4, 5, 6 | Manual-UAT (`requestSubmit`, server-gerendertes `autofocus`; Code geprüft: exakter Treffer case-insensitiv/getrimmt, sonst Einzeltreffer) |
| 7, 8 | Manual-UAT (Hardware-Scanner, synthetisches Enter `!isTrusted`) |
| 9 | Manual-UAT (ohne JS; Formular unverändert GET) |
| 10 | Auto `ApiArtikel_GleicheZugriffsfilterWieDieSeite` (gleiche Filter-Attribute); echte 403/404 Manual-UAT |
| 11 | Diff-Beleg: `ArticlesApiController`, `_Select2ArticlePartial.cshtml`, `BfAuftragRepository` nicht im Diff |
| 12 | grün (s. 1) |

### 4. Geänderte bestehende Tests
Keine bestehenden Tests geändert; nur neue Tests in `BfAuftragWorkflowTests`.

### 6c. DEV-Schritte TS-169 (IDEAL, nur Ansicht/Suche, nichts hinzugefügt)
| Schritt | Ergebnis | Status |
|---|---|---|
| Warenkorb öffnen | GET legt keinen Entwurf an (`BfAuftraegeController.Warenkorb`), Suchfeld da | OK |
| Tippen ≥ 2 Zeichen | Fetch `api/bf-auftraege/artikel?limit=20&q=blec` → 200 | OK |
| Vorschläge sichtbar mit Matchcode/Bezeichnung | Datalist-Optionen im Barrierebaum nicht auslesbar, Screenshot-Timeout | NICHT MÖGLICH (Mensch) |
| Hinzufügen per Enter, Fokus | würde Position anlegen | NICHT MÖGLICH (bewusst nicht ausgelöst) |
| Scanner-Schritte | keine Hardware | NICHT MÖGLICH |

### Manuelle Checkliste (offen für den Menschen)
1. IDEAL BF-Warenkorb am Terminal-Browser: 1 Zeichen → keine Liste, 2 Zeichen → Liste mit Matchcode und Bezeichnung (Rückfrage 3).
2. Begriff nur aus der Bezeichnung → Vorschläge erscheinen.
3. Vorschlag wählen → Trefferzeile; exakte Artikelnummer (andere Schreibweise) → Fokus auf „Hinzufügen“, Enter legt Position an.
4. Mehrere Treffer ohne exakten → kein Knopf fokussiert.
5. Hardware-Scan ohne Fokus und mit Fokus auf „Hinzufügen“ → Code im Suchfeld, Suche startet.
6. Ohne BF-Recht → 403; `BlechfertigungAktiv` ≠ true → 404.
7. Testsystem-Lauf, dann Merge.
