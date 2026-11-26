---
type: spec
title: "Leitstand Nur-HauptFA: Hinweise „Sub-FA bestimmt …“ / „… nur an Sub-FAs“ als Kurzzeichen statt Satz (schmale Lack-T-, Glas-, Zukauf-, VK–VA-Spalten)"
slug: 2026-11-26-leitstand-hinweis-vk-spalte-breit-spec
status: Entwurf
created: 2026-11-26
updated: 2026-11-26
source_backlog: "[[2026-11-26-leitstand-hinweis-vk-spalte-breit]]"
uebersicht: "[[2026-11-26-leitstand-qa-befunde-f1-f3-uebersicht]]"
depends_on: "[[2026-11-26-leitstand-spaltenfilter-glas-zukauf-ohne-wirkung-spec]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: IDEAL
affected_code:
  - IdealAkeWms/Views/PickingLeitstand/_PickingLeitstandRow.cshtml:18-22 (Texte), :125-132 (Lack-T), :144-147 (Glas/Zukauf), :160-167 (VK–VA)
  - IdealAkeWms/Views/PickingLeitstand/Index.cshtml:144-147 (Zählzeile Nur-HauptFA - Legende)
  - IdealAkeWms/wwwroot/css/site.css (nur falls Variante B; die globale nowrap-Regel :235-239 bleibt unangetastet)
  - IdealAkeWms/Views/Help/Index.cshtml:1949-1952, docs/anwender/01-kommissionierung.md:282-290 (Text + Bild 01-23), docs/TESTSZENARIEN.md:15065-15066 (TS-208.6), neues TS-212
open_questions:
  - "1 Form des Hinweises: (A) sichtbares Kurzzeichen in der Zelle („+Sub“ = Sub-FA bestimmt den Wert weiter, „nur Sub“ = nur an Sub-FAs, gesperrt) mit Volltext als title und für Screenreader, Legende in der Zählzeile über der Tabelle (Empfehlung); (B) Volltext behalten, aber auf eine schmale Breite umbrechen (Zeilen werden mehrzeilig hoch); (C) Hinweis nur noch in title/Legende, Zelle ohne Text - welche Variante, und passen die Kurzzeichen?"
  - "2 Wird der Leitstand an Touch-Geräten ohne Maus bedient? Dort erscheint ein title-Tooltip nicht; die Bedeutung trägt dann nur die Legende über der Tabelle. Reicht das, oder soll das Kurzzeichen antippbar sein (Popover, mehr Code)?"
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

Seit v1.99.88 stehen im Nur-HauptFA-Modus Sätze wie „Arbeitsgang nur an Sub-FAs – ändern mit „Sub-FAs anzeigen““ in den 40 px schmalen
VK–VA-Spalten. Weil Tabellenzellen nie umbrechen, wird jede betroffene Spalte so breit wie der Satz; bei fünf VK–VA-Spalten wächst die Tabelle um
ein Vielfaches und der Bediener scrollt quer, um Freigabe und Kommissionierer zu erreichen. Der Hinweis soll bleiben (er erklärt, warum ein
Klick den Wert nicht ändert - Antwort 8 der Teil-2-Spec), aber kaum Platz brauchen und nicht nur über Farbe wirken.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope (nur IDEAL, nur Nur-HauptFA-Modus: alle Hinweise hängen an `Model.NurHauptFa`, `_PickingLeitstandRow.cshtml:19`, `:113`)**

- Die drei Hinweise in Lack-T, Glas, Zukauf und VK–VA: „Sub-FA bestimmt den Wert weiter“ (W1/W2/W5), „Lackteile nur an Sub-FAs – …“ (W3),
  „Arbeitsgang nur an Sub-FAs – …“ (W4).
- Legende in der vorhandenen Zählzeile des Nur-HauptFA-Modus.
- Hilfe, Anwenderdoku (Text und Bild), Testszenario.

**Out-of-Scope**

- Verhalten: Sperren (W3, W4), Neuladen (`data-neu-laden`), Zusammenfassungsregel - unverändert.
- Globale Regel `.table-responsive td { white-space: nowrap !important; }` (`site.css:235-239`): bleibt; sie trägt den einen Scrollbalken
  (v1.64.1, Kommentar `site.css:189-197`). Keine Änderung an anderen Tabellen.
- „+ n Sub-FAs“ in der FA-Nr.-Zelle (`:52-56`): ist kurz, bleibt.

## Fachliche Anforderungen

### Ist-Stand (belegt; Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, v1.99.89)

- Texte als Konstanten `_PickingLeitstandRow.cshtml:20-21`; sichtbar je Zelle als `<div class="small text-body-secondary lh-sm">` -
  Lack-T `:125-132`, Glas/Zukauf `:144-147`, VK–VA `:160-167`; zusätzlich `title` an Checkbox (`:123`, `:142`) bzw. an der `<td>` (`:156`).
- Ursache der Breite: `.table-responsive td { white-space: nowrap !important; }` (`site.css:236-238`) - das `<div>` erbt `nowrap`; die Tabelle hat
  `width: max-content` (`site.css:230-233`), jede Spalte wird so breit wie ihr längster Inhalt. Die `<th>`-Breite (`style="width: 40px"`,
  `Index.cshtml:192-196`) ist nur ein Mindestwert.
- Folge für Variante B: Ein eigenes `white-space: normal` am `<div>` reicht in einer `max-content`-Tabelle **nicht** - die Spalte richtet sich nach
  der unumbrochenen Länge; Umbruch braucht zusätzlich eine feste `max-width` am Hinweis. (Aus CSS-Regeln abgeleitet, am Gerät unbelegt -
  der Dev-Lauf prüft es per Screenshot, falls B gewählt wird.)
- Längen: „Sub-FA bestimmt den Wert weiter“ 31 Zeichen, „Arbeitsgang nur an Sub-FAs – ändern mit „Sub-FAs anzeigen““ 58, „Lackteile nur an
  Sub-FAs – …“ 56.
- Vorhandene Muster: sichtbares Kurzzeichen mit `aria-hidden` plus `visually-hidden`-Volltext und `title` (`PurchaseOrders/Index.cshtml:153`);
  Zählzeile des Nur-HauptFA-Modus mit Erklärtext existiert (`Index.cshtml:144-147`, „Lack-T, Glas, Zukauf, VK–VA zusammengefasst …“).
- Kein Test prüft die Hinweistexte (Suche „bestimmt den Wert weiter“ in `IdealAkeWms.Tests`: kein Treffer); Fundstellen in Doku: Hilfe
  `Help/Index.cshtml:1949-1952`, Changelog `:35-36` (historisch, bleibt), `docs/anwender/01-kommissionierung.md:282-290`, `docs/TESTSZENARIEN.md:15065-15066`.

### Soll (Variante A, Empfehlung - Rückfrage 1)

| ID | Anforderung |
|---|---|
| H1 | Statt des Satzes steht in der Zelle ein kurzes sichtbares Textzeichen in `small`: „+Sub“ bei „Sub-FA bestimmt den Wert weiter“, „nur Sub“ bei W3/W4 (gesperrt). Text, nicht Farbe und nicht nur Symbol. |
| H2 | Volltext bleibt erreichbar: als `title` (wie heute) und als `visually-hidden`-Text für Screenreader; das Kurzzeichen selbst `aria-hidden="true"`. |
| H3 | Die Zählzeile (`Index.cshtml:144-147`) erklärt beide Kurzzeichen in Worten: „+Sub = Sub-FA bestimmt den Wert weiter · nur Sub = nur an Sub-FAs, ändern mit „Sub-FAs anzeigen““. |
| H4 | Die Spalten Lack-T, Glas, Zukauf, VK–VA werden durch den Hinweis höchstens so breit wie „nur Sub“ in `small` (bei 40-px-Spalten ungefähr Spaltenbreite). |
| H5 | Kontrast mindestens WCAG AA 4,5:1 (`text-body-secondary` auf Zeilen- und Streifenhintergrund prüfen, auch `table-secondary` erledigter Zeilen). |
| H6 | Verhalten unverändert: `data-neu-laden`, `disabled`, CSV-Werte (Checkbox Ja/Nein, Select-Text, `csv-export.js:92-96` - Kurzzeichen gelangen nicht in die CSV). |

## Technischer Loesungsentwurf

Ponytail: nur Markup im Row-Partial und ein Satz in der Zählzeile; kein CSS, keine neue Komponente.

1. `_PickingLeitstandRow.cshtml`: eine lokale Razor-Funktion `Hinweis(string kurz, string voll)` rendert
   `<div class="small text-body-secondary lh-sm" title="@voll"><span aria-hidden="true">@kurz</span><span class="visually-hidden">@voll</span></div>`;
   die fünf bisherigen `<div>`-Stellen (`:127`, `:131`, `:146`, `:162`, `:166`) rufen sie auf. Konstanten `:20-21` bleiben als Volltext.
2. `Index.cshtml:144-147`: Legende H3 an den vorhandenen `<span class="ms-2">` anhängen.
3. Variante B (falls gewählt): statt 1 eine Klasse `.leitstand-hinweis { white-space: normal; max-width: 5rem; }` in `site.css` (Name im Bestand
   prüfen) - Wirkung per Screenshot belegen (Ist-Stand, Folge für Variante B).
4. Doku: Hilfe `:1949-1952` und `docs/anwender/01-kommissionierung.md:282-290` auf die Kurzzeichen umstellen, Bild 01-23 per Playwright gegen DEV
   IDEAL neu (nur ansehen). TS-208.6 (`docs/TESTSZENARIEN.md:15065-15066`) nennt die Kurzzeichen.
5. Skill `frontend-design` ausdrücklich aufrufen; Kontrast H5 messen und im QA-Nachweis festhalten.

## Migrations-/SQL-Auswirkungen

Keine Migration, kein SQL. `SQL/00_FreshInstall.sql`, `SQL/AgentJobs/` unberührt.

## Einstellungen

keine

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine (nur Darstellung). Rollen unverändert.

## Akzeptanzkriterien

1. DEV IDEAL, Nur-HauptFA-Modus, HauptFA mit W4 in allen fünf VK–VA-Spalten: jede VK–VA-Spalte ist höchstens so breit wie „nur Sub“ (Screenshot vorher/nachher).
2. W1/W2/W5-Zellen zeigen „+Sub“, W3/W4-Zellen „nur Sub“; Maus über der Zelle bzw. Checkbox zeigt den Volltext.
3. Screenreader (NVDA oder Browser-Barrierefreiheitsbaum) liest den Volltext, nicht „+Sub“.
4. Die Zählzeile erklärt beide Kurzzeichen in Worten.
5. Kontrast der Kurzzeichen ≥ 4,5:1 auf normalen, gestreiften und erledigten Zeilen.
6. Sperren, Neuladen nach Speichern und CSV-Export verhalten sich wie vorher (TS-208.6 grün).
7. Mit „Sub-FAs anzeigen“ und an der AKE: keine Kurzzeichen (wie heute keine Hinweise).

## Test-Szenarien

Neues Kapitel **TS-212** in `docs/TESTSZENARIEN.md` (Nummer beim Dev-Lauf nachprüfen), Index nachziehen; TS-208.6 Texte anpassen:

- **TS-212.1** Breite: Auftrag mit W4 in VK–VA, Spaltenbreiten vorher/nachher (AK 1).
- **TS-212.2** Kurzzeichen und Tooltip je Fall W1-W5 (AK 2), Legende (AK 4).
- **TS-212.3** Barrierefreiheit: Screenreader-Text, Kontrast (AK 3, 5).
- **TS-212.4** Regression: Klick in W1/W2/W5 lädt neu, W3/W4 gesperrt, CSV ohne Kurzzeichen; Schalter an → keine Kurzzeichen (AK 6, 7).

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | kein Epic | – | – |

## Deploy

*Vorläufig - der QA-Lauf bestätigt gegen den echten Diff.*

- **Web-App:** ja (Views; bei Variante B auch `site.css`)
- **Service:** nein
- **Migration:** nein
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): keine
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` (im Worktree)

## Offene Rueckfragen

1. **Form des Hinweises:** (A) Kurzzeichen „+Sub“ / „nur Sub“ in der Zelle, Volltext als Tooltip und für Screenreader, Legende über der Tabelle
   (Empfehlung - schmalste Spalten, Bedeutung als Text, keine globale CSS-Änderung); (B) Volltext auf schmaler Breite umbrechen (Zeilen werden
   bei 58 Zeichen mehrzeilig hoch); (C) nur Tooltip und Legende, Zelle ohne Text (schmalste Variante, aber in der Zelle kein Hinweis mehr sichtbar).
   Welche Variante, und passen die Kurzzeichen?
2. **Touch-Bedienung:** Wird der Leitstand an Touch-Geräten ohne Maus bedient? Dort erscheint der Tooltip nicht; die Bedeutung trägt dann nur die
   Legende. Reicht das (Empfehlung), oder soll das Kurzzeichen antippbar sein (Popover, mehr Code)?

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. Variante A (Kurzzeichen, Empfehlung), B (Umbruch) oder C (nur Tooltip/Legende)? Kurzzeichen „+Sub“ / „nur Sub“ ok? →
2. Touch-Bedienung: Legende reicht (Empfehlung) oder antippbares Popover? →
