---
type: spec
title: "Beschichtungsauftrag-Druck: „ohne FA“ in immer sichtbarer Zelle, abweichende Matchcodes/Haupt-Artnr. einer Summenzeile melden"
slug: 2026-11-26-beschichtungsauftrag-druck-ohne-fa-und-matchcode-abweichung-spec
status: Entwurf
created: 2026-11-26
updated: 2026-11-26
source_backlog: "[[2026-11-26-beschichtungsauftrag-druck-ohne-fa-und-matchcode-abweichung]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: beide
affected_code:
  - IdealAkeWms/Views/Beschichtungsauftraege/Print.cshtml (Badge „ohne FA“ aus der Artnr-Zelle :110 in die Verpackt-Zelle :118-121; neue Hinweise; CSS :34/:36)
  - IdealAkeWms/Models/ViewModels/BeschichtungsauftraegeViewModels.cs (BeschichtungsauftraegeDruck.Summiert :153-163, Record BeschichtungsauftragDruckZeile :168-169)
  - IdealAkeWms.Tests/Controllers/BeschichtungsauftraegeControllerTests.cs (Druck_Summiert_* :522-559 ergaenzen)
  - IdealAkeWms/Views/Help/Index.cshtml (:1609-1615 Druck und PDF)
  - IdealAkeWms/Views/Help/Changelog.cshtml (neuer Eintrag)
  - docs/anwender/15-beschichtung-beschichtungsauftrag.md (:126-129)
  - docs/TESTSZENARIEN.md (neues Kapitel; TS-205.3/205.4 Querverweis)
open_questions:
  - "1 Ort von „ohne FA“ im Druck: nur noch in der Verpackt-Zelle (zweite Zeile wie „Maße uneinheitlich“, Badge an der Artnr entfällt), zusätzlich an der Artnr bleiben, oder ganz entfallen lassen (Artnr ist Pflichtspalte, H-1 nur über URL-Parameter bzw. alte Präferenz erreichbar)?"
  - "2 Abweichung von Matchcode und Haupt-Artnr. innerhalb einer Summenzeile melden wie „Maße uneinheitlich“ (Text „Matchcode uneinheitlich“ / „Haupt-Artnr. uneinheitlich“ in der Verpackt-Zelle, Zeile bleibt summiert)?"
  - "3 Vergleichsregel für Frage 2: Leerzeichen am Rand egal, leer = null, sonst exakt (Groß/Klein zählt als Abweichung) - einverstanden?"
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

Der Beschichtungsauftrag geht als Brief an den Lackierer. Seit v1.99.87 druckt er eine Zeile je Artikel
([[2026-11-26-beschichtung-summe-leitstand-hauptfa-kommissionierauftrag-liste-teil-1-spec]]). Die QA dazu hat zwei Lücken gefunden:

- **H-1:** Das Kennzeichen „ohne FA“ steht nur in der Artnr-Zelle. Fehlt die Spalte im Druck, ist eine freie Zeile nur noch an der
  Reihenfolge (zuletzt) von einer FA-Zeile mit gleicher Artnr zu unterscheiden (DEV AKE BA-2026-00005).
- **H-2:** Weichen Matchcode oder Haupt-Artnr. innerhalb einer Summenzeile ab, druckt der Brief still den Wert der ersten Position.
  Bei den Maßen wird das schon gemeldet („Maße uneinheitlich“).

Beides fällt unter „Melden statt still behandeln“ (CLAUDE.md, ponytail-Ausnahmen).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Bildschirmdruck `Beschichtungsauftraege/Print/{id}` und PDF `Beschichtungsauftraege/Pdf/{id}` (eine View, ein Modell,
  `BeschichtungsauftraegeController.cs:297-338`, PDF rendert `"Print"` `:310`).
- H-1: „ohne FA“ an einem Ort, der nicht ausgeblendet werden kann (Rückfrage 1).
- H-2: Hinweise „Matchcode uneinheitlich“ und „Haupt-Artnr. uneinheitlich“ je Summenzeile (Rückfragen 2, 3).

**Out-of-Scope**
- Detailseite des Auftrags: sie zeigt „ohne FA“ als Badge in der Spalte Herkunft (`Details.cshtml:293-297`) und je Position ihre
  eigenen Werte; nichts wird summiert, also gibt es dort keine Abweichung zu melden.
- Vorschlagsliste Beschichtung (`FaHierarchyBeschichtung/Print`, `PrintSummiert`): unverändert (wie Teil-1-Antwort 5).
- Groß/Klein der Artnr im Gruppenschlüssel: `Summiert` trimmt, vergleicht aber exakt (`ViewModels.cs:155`, Tupel-Gleichheit). „ab-1“
  und „AB-1“ bleiben zwei Zeilen. Teil 1 hat das als HINWEIS festgehalten. Es wird hier nicht geändert.
- Clientseitig ausgeblendete Pflichtspalten (siehe Ist-Stand, Zusatzfund Z-1): nur gemeldet, nicht gebaut.

## Fachliche Anforderungen

### Ist-Stand (belegt im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, v1.99.89)

- **Badge-Ort.** `Print.cshtml:106-112`: Das Badge `<span class="ohne-fa">ohne FA</span>` steht innerhalb von
  `@if (Model.ShowCol("artnr"))`. Die Verpackt-Zelle (`:118-121`) wird ohne `ShowCol` immer gedruckt. Dort steht bereits
  „Maße uneinheitlich“ als zweite Zeile (`:120`, Klasse `.hinweis` `:36`).
- **Wann fehlt die Artnr-Spalte im Druck?** `artnr` und `matchcode` sind Pflichtspalten: `Locked: true` in
  `ColumnDefinitions.cs:793-794`. Im Zahnrad ist die Checkbox deaktiviert (`column-preferences.js:509`, Zusatz „(Pflichtspalte)“ `:525-529`),
  das Kontextmenü bietet „Spalte ausblenden“ nicht an (`:724-731`), und der Server-Rückfall hält Locked-Spalten sichtbar
  (`PrintVisibleColumns.cs:35`). Erreichbar ist H-1 deshalb nur auf zwei Wegen:
  1. über den Query-Parameter `visibleColumns`: Er wird ungeprüft übernommen (`PrintVisibleColumns.cs:24-25`). So hat die QA
     es auf DEV erzeugt (`?visibleColumns=hauptartnr,matchcode,menge`).
  2. über eine gespeicherte Präferenz mit `artnr` „visible: false“: `applySettings` wendet `visible` ohne Blick auf `locked` an
     (`column-preferences.js:117,144`). `print-visible-columns.js:14-19` schickt dann die im DOM sichtbaren Spalten mit. Ob es
     solche Präferenzen gibt, ist **unbelegt**. Prüfabfrage P-1 zählt sie.
  Folge: TS-205.4 verlangt „per Zahnrad … Artnr. ausblenden“ (`docs/TESTSZENARIEN.md:14914-14915`). Das ist über das Zahnrad nicht
  ausführbar, die QA hat den Schritt per URL nachgestellt (QA-Nachweis Teil 1, Schritt 205.4).
- **Woher Matchcode und Haupt-Artnr. kommen.** Positionen sind ein Snapshot bei der Anlage; danach schreibt kein Weg die drei Felder
  (Entität `BeschichtungsauftragPosition.cs:5-11`; Suche `\.(Matchcode|HauptArtnr|Artnr)\s*=` im Web-Projekt: kein Treffer auf
  Positionen). Schreibwege:
  - FA-Position aus dem Knoten: `Snapshot` (`BeschichtungsauftragWorkflow.cs:694-704`) übernimmt `n.HauptArtnr`, `n.Artnr`, `n.Matchcode`.
    - AKE (flache Quelle): `HauptArtnr = ProductionOrder.ArticleNumber` des Auftrags, Matchcode aus dem Artikelstamm je Artnr
      (`BeschichtungFlacheQuelle.cs:143,149`).
    - IDEAL SData: `HauptArtnr` = Artikel des Kopfs je HauptFA (`SdataFaStrukturReader.cs:379,401`), Matchcode aus dem Sage-Artikel je
      Artnr (`:254`).
    - IDEAL View: beide Werte aus der Sage-View je Zeile (`FaHierarchySql.cs:143`). Ob sie je HauptFA bzw. Artnr gleich sind, ist ein
      **Sage-Fakt und unbelegt**.
  - Freie Position: `HauptArtnr = null`, Matchcode aus dem Artikelstamm (`BeschichtungsauftragWorkflow.cs:470-471`). Sie mischt sich nie
    mit FA-Positionen, denn `OhneFa` steht im Schlüssel (`ViewModels.cs:155`).
  - Reklamation: `Kopie` übernimmt die Werte des Ursprungs (`BeschichtungsauftragWorkflow.cs:706-717`).
  Innerhalb einer Quelle sind die Werte zum Anlagezeitpunkt je Artnr also gleich (AKE, SData). Abweichungen entstehen, wenn Positionen
  derselben Artnr **zu verschiedenen Zeitpunkten** in den Auftrag kommen (Anlage, später „Positionen hinzufügen“ `:370-395`) und
  dazwischen der Stamm oder die Sage-View geändert wurde, oder an der IDEAL-View-Quelle ohne diese Garantie.
- **Summierung heute.** `Summiert` (`ViewModels.cs:153-163`) nimmt Haupt-Artnr. und Matchcode der ersten Position und prüft nur die Maße
  (`:161`). Das Record hat das Feld `MasseUneinheitlich` (`:168-169`). Leser: nur `Print.cshtml` und `BeschichtungsauftraegeControllerTests.cs`
  (Suche `BeschichtungsauftragDruckZeile|MasseUneinheitlich`).

### Soll

| ID | Anforderung |
|---|---|
| S1 | **(nach Rückfrage 1, Empfehlung)** „ohne FA“ steht im Druck und im PDF in der immer gedruckten Verpackt-Zelle, als eigene Zeile unter dem Verpackt-Stand, gleicher Stil wie „Maße uneinheitlich“ (Text mit Rahmen, auch im Schwarzweißdruck lesbar). Das Badge in der Artnr-Zelle entfällt, damit es nur einen Ort gibt. |
| S2 | **(nach Rückfrage 2, Empfehlung)** Weicht innerhalb einer Summenzeile der Matchcode einer Position von dem der ersten Position ab, trägt die Verpackt-Zelle zusätzlich „Matchcode uneinheitlich“. Gleiches gilt für die Haupt-Artnr. mit „Haupt-Artnr. uneinheitlich“. Die Zeile bleibt summiert, gedruckt wird weiter der Wert der ersten Position (wie bei den Maßen, Teil-1 S2). |
| S3 | **(nach Rückfrage 3, Empfehlung)** Vergleichsregel für S2: Leerzeichen am Rand werden entfernt, `null` und leer gelten als gleich, sonst ein exakter Vergleich (`StringComparison.Ordinal`). Grund: Gedruckt wird der Text der ersten Position, und jede sichtbare Abweichung soll auffallen. |
| S4 | Reihenfolge der Hinweise in der Verpackt-Zelle: Verpackt-Stand, dann „ohne FA“, „Maße uneinheitlich“, „Matchcode uneinheitlich“, „Haupt-Artnr. uneinheitlich“, je eigene Zeile, nur wenn zutreffend. |
| S5 | Positionen ohne Artnr sind je eine eigene Zeile (Teil-1 S1) und bekommen nie einen Uneinheitlich-Hinweis. |
| S6 | Keine Einstellung, keine Standort-Weiche: AKE und IDEAL drucken gleich (wie Teil-1 Antwort 4). |

## Technischer Loesungsentwurf

Ponytail: keine neue Datei außer Tests, keine neue Klasse.

1. **Record** `BeschichtungsauftragDruckZeile` (`ViewModels.cs:168-169`) bekommt `bool MatchcodeUneinheitlich, bool HauptArtnrUneinheitlich`
   (nach `MasseUneinheitlich`). `OhneFa` ist schon da.
2. **`Summiert`** (`:153-163`): Im `Select` stehen neben `MasseUneinheitlich` zwei weitere `g.Any(...)`-Prüfungen mit einer privaten
   statischen Hilfsfunktion im selben Typ: `static bool Gleich(string? a, string? b) => string.Equals(a?.Trim() ?? "", b?.Trim() ?? "", StringComparison.Ordinal);`
   Die Regel bleibt an einem Ort, `Summiert`. Den `<summary>` anpassen.
3. **`Print.cshtml`**:
   - In der Artnr-Zelle (`:106-112`) entfällt das Badge (nach Rückfrage 1).
   - Verpackt-Zelle (`:118-121`): nach dem Verpackt-Text je zutreffendem Hinweis `<br /><span class="hinweis">…</span>` in der Reihenfolge S4.
   - CSS: `.ohne-fa` (`:34`) entfällt, falls nicht mehr benutzt. `.hinweis` (`:36`) bleibt der eine Stil.
   - Kommentar `:75-77` um die neuen Hinweise ergänzen.
   - colspan der Summenzeile (`:80`, `:125-138`) bleibt unverändert, weil keine Spalte hinzukommt.
   - View-Änderung: Skill `frontend-design` ausdrücklich aufrufen (CLAUDE.md). Druckbild ruhig halten wie v1.99.42, Text statt Farbe.
4. **Tests** (`BeschichtungsauftraegeControllerTests.cs`, Muster `Druck_Summiert_MasseUneinheitlich_NurBeiAbweichung` `:541-546`): neue reine
   Unit-Tests `Druck_Summiert_MatchcodeUneinheitlich_*` und `Druck_Summiert_HauptArtnrUneinheitlich_*` für gleich, abweichend, nur Leerzeichen,
   null gegen leer und Groß/Klein. Dazu ein Test, dass eine Position ohne Artnr nie uneinheitlich ist.
5. **Hilfe** `Views/Help/Index.cshtml:1609-1615`, **Anwenderdoku** `docs/anwender/15-…md:126-129`: „ohne FA“ jetzt in der Spalte Verpackt,
   neue Hinweise erklären. Changelog-Eintrag. Die Version vergibt der Dev-Lauf (Bündel steht bei v1.99.89).

## Migrations-/SQL-Auswirkungen

Keine. Reine Darstellung, kein Schema, keine Daten, `SQL/00_FreshInstall.sql` und `SQL/AgentJobs/` unberührt.

## Einstellungen

keine.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine. Der Druck liest nur (`GetDetailAsync`, `Controller :322`).

## Rollen / Zugriff

Unverändert. Druck und PDF stehen unter denselben Filtern wie bisher am `BeschichtungsauftraegeController`, keine neue Rolle und kein neuer Filter.

## Listen-View-Pattern

Nicht betroffen. Der Druck ist keine Listen-View, und an der Detailtabelle ändert sich nichts.

## Akzeptanzkriterien

1. Auftrag mit einer FA-Position und einer freien Position derselben Artnr: Der Druck zeigt zwei Zeilen. Die freie Zeile trägt in der
   Verpackt-Zelle die Zeile „ohne FA“, die FA-Zeile nicht.
2. Derselbe Auftrag mit `?visibleColumns=hauptartnr,matchcode,menge` (Artnr nicht gedruckt): „ohne FA“ steht weiter in der Verpackt-Zelle.
3. In der Artnr-Zelle steht kein Badge „ohne FA“ mehr (nach Rückfrage 1).
4. Drei Positionen gleicher Artnr, eine mit anderem Matchcode: eine Summenzeile mit dem Matchcode der ersten Position und dem Text
   „Matchcode uneinheitlich“ in der Verpackt-Zelle.
5. Wie 4, aber Haupt-Artnr. abweichend: Text „Haupt-Artnr. uneinheitlich“.
6. Matchcode unterscheidet sich nur durch Leerzeichen am Rand, oder `null` gegen leer: kein Hinweis. Unterschied nur in Groß/Klein:
   Hinweis (S3).
7. Gleichzeitig Maße, Matchcode und „ohne FA“ betroffen: alle Hinweise je eigene Zeile in der Reihenfolge S4.
8. Positionen ohne Artnr: nie ein Uneinheitlich-Hinweis.
9. PDF zeigt dieselben Hinweise wie der Bildschirmdruck. Schwarzweiß lesbar (Text, keine reine Farbe).
10. Summenzeile, Pos. 1..n und Spaltenbündigkeit sind unverändert (TS-205-Regression).
11. Detailseite und Vorschlagsliste-Druck sind unverändert.
12. Unit-Tests für `Summiert` (AK 4-8) grün. `dotnet build` und `dotnet test` grün.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md`. Die Nummer vergibt der Dev-Lauf (Stand 2026-11-26: höchste TS-209, `docs/TESTSZENARIEN.md:15085`;
nächste freie TS-210, die Lagerbestellungs-Spec von heute kann parallel laufen). Den Index `secondbrain/tests/testszenarien-index.md` nachziehen.
In TS-205.3/205.4 (`:14913-14915`) einen Querverweis auf das neue Kapitel setzen. TS-205.4 „per Zahnrad … Artnr. ausblenden“ als nicht ausführbar
markieren (Pflichtspalte), nicht löschen.

- **Vorbedingung:** Auftrag im Status Übergeben (DEV per SQL wie TS-205, Bemerkung „QA TS-x“) mit: FA-Position und freier Position gleicher Artnr;
  einer Artnr mit drei Positionen, eine davon mit anderem Matchcode; einer Artnr mit abweichender Haupt-Artnr.; einer Artnr mit Matchcode-Abweichung nur
  in Leerzeichen am Rand.
- **x.1** Drucken → „ohne FA“ in der Verpackt-Zelle der freien Zeile, nicht an der Artnr (AK 1, 3).
- **x.2** Mit `?visibleColumns=hauptartnr,matchcode,menge` drucken → „ohne FA“ bleibt (AK 2).
- **x.3** Matchcode- und Haupt-Artnr-Abweichung → Hinweise (AK 4, 5). Leerzeichen-Variante → kein Hinweis (AK 6).
- **x.4** Kombination der Hinweise, Reihenfolge (AK 7).
- **x.5** PDF gleich Druck, Schwarzweiß (AK 9).
- **x.6** Regression TS-205.1/205.5/205.8 (AK 10). Detailseite und `/FaHierarchyBeschichtung/PrintSummiert` unverändert (AK 11).
- **x.7** Alles auf DEV AKE und DEV IDEAL.
- **Negativ:** Auftrag mit einheitlichen Werten → keine Uneinheitlich-Hinweise. Positionen ohne Artnr → kein Hinweis (AK 8).

**Prüfabfrage P-1 (WMS-DB, nur lesend, je Standort):** Gibt es gespeicherte Präferenzen, die die Pflichtspalten clientseitig ausblenden (Ist-Stand Weg 2)?
JSON-Schlüssel klein, wie `column-preferences.js:67,117` speichert:
```sql
SELECT p.UserId, j.[key], j.visible
FROM dbo.UserViewPreferences p
CROSS APPLY OPENJSON(p.SettingsJson, '$.columns') WITH ([key] nvarchar(100) '$.key', visible bit '$.visible') j
WHERE p.ViewKey = N'BeschichtungsauftragPositionen' AND j.[key] IN (N'artnr', N'matchcode') AND j.visible = 0;
```
Treffer > 0 bestätigt Zusatzfund Z-1. Ein Fehler wegen ungültigem JSON heißt: einzelne Zeilen mit `ISJSON(p.SettingsJson) = 1` vorfiltern.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | (kein Epic) | – | – |

## Deploy

*Vorläufig. Der QA-Lauf bestätigt es am echten Diff.*

- **Web-App:** ja (`Print.cshtml`, ViewModel, Hilfe, Changelog).
- **Service:** nein (fachlich; nur die Versionsnummer, im Bündel ohnehin mit ausgerollt).
- **Migration:** nein.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): keine. Es gibt kein SQL, kein Einmal-Skript, keine Einstellung und keinen Leitstand-Schritt.
  P-1 ist eine einmalige Erkundung, kein Deploy-Zustand.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
  Ablauf Mensch: Publish aus dem Worktree → Testsystem → testen → Merge.

## Zusatzfunde (nur gemeldet, nicht im Umfang)

- **Z-1** `column-preferences.js:117,144` wendet eine gespeicherte Sichtbarkeit auch auf Pflichtspalten an (`locked` wird dort nicht geprüft). Gilt für
  alle Tabellen mit Spaltenauswahl, nicht nur für diese. Zuerst mit P-1 messen; bei Treffern eigener Backlog-Eintrag.
- **Z-2** `PrintVisibleColumns.ResolveAsync` übernimmt `visibleColumns` aus der URL ohne Locked-Ergänzung (`:24-25`), anders als der Server-Rückfall (`:35`).
  Ein Einzeiler (`defs.Where(d => d.Locked)` dazunehmen) würde H-1 ebenfalls schließen. Er gilt aber für alle Druckdokumente, die die Klasse nutzen
  (Kommissionierliste, Beschichtung), deshalb hier nur gemeldet.

## Offene Rueckfragen

1. **Ort von „ohne FA“.** Die Artnr ist eine Pflichtspalte (`ColumnDefinitions.cs:793`), H-1 entsteht nur über den URL-Parameter oder eine alte
   Präferenz (Ist-Stand). Varianten: (a) nur noch in der Verpackt-Zelle, das Badge an der Artnr entfällt; (b) in der Verpackt-Zelle **und** an der Artnr;
   (c) nichts ändern. **Empfehlung (a):** ein fester Ort wie „Maße uneinheitlich“, keine doppelte Anzeige.
2. **Matchcode-/Haupt-Artnr-Abweichung melden?** Laut Ist-Stand ist sie selten (nur bei zeitversetzt angelegten Positionen oder an der IDEAL-View-Quelle),
   sie kann aber nicht ausgeschlossen werden. **Empfehlung:** ja, analog zu „Maße uneinheitlich“: Text in der Verpackt-Zelle, Zeile bleibt summiert, gedruckt
   wird der Wert der ersten Position.
3. **Vergleichsregel.** Leerzeichen am Rand egal, `null` = leer, sonst exakt, also zählt auch Groß/Klein als Abweichung. **Empfehlung:** so, weil der
   gedruckte Text sich dann wirklich unterscheidet. Alternative: Groß/Klein egal (`OrdinalIgnoreCase`).

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

(kein Jira-Vorgang)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Ort von „ohne FA“: (a) nur Verpackt-Zelle / (b) Verpackt-Zelle und Artnr / (c) nichts ändern →
2. Matchcode-/Haupt-Artnr-Abweichung je Summenzeile melden (Text in der Verpackt-Zelle)? →
3. Vergleichsregel: Rand-Leerzeichen egal, null = leer, sonst exakt (Groß/Klein zählt)? →
