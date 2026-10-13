---
type: spec
title: "Briefkopf: Fußzeile nach Firmen-Word-Vorlage (+ Muster-Vorlage nachziehen)"
slug: 2026-10-13-briefkopf-fusszeile-word-vorlage-spec
status: Entwurf
created: 2026-10-13
updated: 2026-10-13
source_backlog: "[[2026-10-09-briefkopf-seitenzaehlung]]"
anhaenge:
  - "[[2026-10-13-word-vorlage-fusszeile.png]]"
  - "[[2026-10-13-word-vorlage-seite.png]]"
  - "[[2026-10-13-briefkopf-gerald.html]]"
depends_on: "[[2026-10-06-beschichtungsauftrag-ausbau-teil-2-spec]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - IdealAkeWms/Services/BriefkopfVorlageService.cs (Load: Fußzeilen-Bereich <footer class="briefkopf-fuss"> aus dem bereinigten Body herauslösen, eigene div-Balance-Prüfung, gleicher Embedder; Record BriefkopfVorlage um Fuss-Markup erweitern; Ersatz => kein Fuss)
  - IdealAkeWms/Views/Shared/_PrintBriefkopfFuss.cshtml (neu - Fußzeile EINMAL je Druckseite, nur wenn Vorlage nicht Ersatz und Fuss-Markup vorhanden)
  - IdealAkeWms/Views/Shared/_PrintBriefkopfStyles.cshtml (@page-Rand + Positionierung der Fußzeile, nur wenn Fuss-Markup vorhanden)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/Print.cshtml (eine Partial-Zeile im else-Zweig vor der Gruppenschleife :53)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/PrintSummiert.cshtml (eine Partial-Zeile im else-Zweig vor der Gruppenschleife :55)
  - IdealAkeWms/Views/Beschichtungsauftraege/Print.cshtml (eine Partial-Zeile nach den screen-controls :41)
  - Branding/briefkopf.html (Muster - Logo rechts, Schriften NimbusSanNov-*.otf, Fußzeilen-Bereich, Absenderzeile entfällt, Ablage-Kommentar korrigiert)
  - IdealAkeWms.Tests/Services/BriefkopfVorlageServiceTests.cs (neue Fälle Fußzeile; Muster-Test :337-357 anpassen)
  - IdealAkeWms.Tests/Views/BriefkopfEinbindungTests.cs (Fuss-Partial genau einmal in den drei Druckansichten, nicht in Kommissionierlisten)
  - README.md (:415 und Abschnitt Briefkopf-Vorlage :449-465 - Ablage, Fußzeile)
  - IdealAkeWms/Views/Help/Index.cshtml (:780-786, :818-821) + Views/Help/Changelog.cshtml + beide AppVersion.cs
  - docs/TESTSZENARIEN.md (neues Kapitel; TS-82-Vorbedingung :9347 korrigieren) + secondbrain/tests/testszenarien-index.md
open_questions:
  - "AKE Blau #43a6e2 als TEXTfarbe für den AKE-Firmennamen in der Fußzeile (Kontrast 2,70:1) - widerspricht Teil-2 Anforderung 8 / AK 11, dem Test MusterVorlage_... (:355) und der CLAUDE.md-Kontrastregel. Ausnahme nur für diesen Namen in der Fußzeile zulassen? Empfehlung: ja."
  - "Farbbalken: A) randabfallend bis zur Papierkante wie in Word, B) innerhalb der Seitenränder mit Abstand zur Papierkante? Empfehlung: B."
  - "'Seite X von Y': A) weiter weglassen (Aufgabe bleibt offen), B) nur dort, wo ein Druck genau ein Dokument enthält (alle PDFs, Auftragsdruck, Sammeldruck mit genau einem HauptFA) - nur falls der Prüfschritt P3 positiv ist? Empfehlung: B, bei negativem P3 automatisch A."
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

# Briefkopf: Fußzeile nach Firmen-Word-Vorlage

> [!info] Quelle
> Es gibt **keine eigene Backlog-Datei**. Quelle ist der Auftrag von Gerald vom **2026-10-13** (`/spec Briefkopf:
> Fusszeile nach Firmen-Word-Vorlage (+ Muster-Vorlage nachziehen)`). `source_backlog` zeigt deshalb auf die offene Aufgabe
> [[2026-10-09-briefkopf-seitenzaehlung]], deren Nachtrag 2026-10-13 die Fußzeile beschreibt. Anhänge: Fußzeile der
> Word-Vorlage als Bild (2480 × 591 px), ganze Seite der Word-Vorlage, Geralds aktuelle Vorlage. Alle drei waren in
> diesem Lauf lesbar.
>
> Standort: nur **IDEAL**. Die Druckansichten mit Briefkopf gibt es nur im IDEAL-Beschichtungsauftrag
> (Gate `FaHierarchyBeschichtungAktiv`, Teil 2/4). AKE ist nicht betroffen.

Belege beziehen sich auf den Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (HEAD `ead16fc4`, v1.58.1).

## Ziel / Nutzen (das Warum)

Der Beschichtungsauftrag geht als Außendokument an Dienstleister. Heute hat er oben einen Briefkopf aus der
IT-Vorlage, unten aber nichts. Die Firmen-Word-Vorlage `Allg-Dokument-Vorlage_IDEAL_AKE.dotx` hat unten auf jeder Seite
die Anschriften beider Firmen, die Web-Adresse und den zweifarbigen Balken. Ziel: dieselbe Fußzeile auf **jeder**
Druckseite, im Bildschirmdruck und im PDF. Die Gestaltung bleibt wie beim Kopf in der Hand der IT (Vorlage-Datei), und
Positionen dürfen nie unter der Fußzeile verschwinden.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Zweiter Bereich in der Vorlage (`<footer class="briefkopf-fuss">`), gelesen und bereinigt nach denselben Regeln wie
  der Kopf, gemeinsame Größengrenzen.
- Fußzeile **einmal je Druckseite im DOM**, im Druck auf jeder Seite wiederholt. Gilt für
  `FaHierarchyBeschichtung/Print`, `FaHierarchyBeschichtung/PrintSummiert` und `Beschichtungsauftraege/Print`, damit
  auch für deren PDFs (`Pdf`, `PdfSummiert`, `Beschichtungsauftraege/Pdf` rendern dieselben Views,
  `FaHierarchyBeschichtungController.cs:136`, `:224`).
- Reservierter unterer Seitenrand, nur wenn die Vorlage eine Fußzeile hat.
- Muster `Branding/briefkopf.html` nachziehen (Punkt 5 des Auftrags).
- README, Hilfe, Changelog, TESTSZENARIEN (Ablageort `Branding\` korrigieren).
- Optional „Seite X von Y" laut Rückfrage 3.

**Out-of-Scope**
- „Seite X von Y" im Sammeldruck mit mehreren HauptFA. Die Aufgabe [[2026-10-09-briefkopf-seitenzaehlung]] bleibt
  offen.
- Kommissionierlisten (bekommen weiter weder Kopf noch Fuß; `BriefkopfEinbindungTests.cs:72-73`).
- Härtung der Bereinigung (Aufgabe [[2026-10-09-briefkopf-csp-und-bereinigung]], unverändert offen). Die Fußzeile
  fällt unter dieselbe dokumentierte `ponytail:`-Grenze (`BriefkopfVorlageService.cs:143-149`).
- Icon-Dateien im Repo (Entscheidung E1 unten).
- Änderungen an Satzspiegel, Schrift oder Inhalt des übrigen Dokuments (Tabellen, Banner, Barcode).

## Fachliche Anforderungen

1. **Inhalt der Fußzeile** (Muster, IT kann ändern):
   - links: „IDEAL Kältetechnik GmbH" (IDEAL Blau `#053153`, semibold), darunter In der Schörihub 28, 4810 Gmunden, AT /
     +43 7612 660 61 / office@ideal-ake.at;
   - Mitte: „AKE Ausseer Kälte- und Edelstahltechnik GmbH" (AKE Blau `#43a6e2`, semibold, siehe Rückfrage 1), darunter
     Pichl 66, 8984 Bad Mitterndorf, AT / +43 3624 211 00 / **office@ideal-ake.at**. Gerald 2026-10-13: für beide Firmen
     nur diese Adresse; `office@ake.at` im Seitenbild der Word-Vorlage und im CD-Manual ist veraltet;
   - rechts, auf Höhe der letzten Zeile: `www.ideal-ake.at` (semibold, IDEAL Blau);
   - darunter ein Balken, links IDEAL Blau, rechts AKE Blau, Übergang 45° schräg („/" von links unten nach rechts oben,
     etwa in der Seitenmitte). Reines CSS, kein Bild. Lage und Breite: Rückfrage 2;
   - Fließtext Grau 85 `#4a494a`, Schrift Nimbus Sans Novus wie im Kopf.
   - Maße aus `2026-10-13-word-vorlage-fusszeile.png` (2480 px = 210 mm, also 11,81 px/mm): Textblock rund 11 mm hoch,
     Zeilenabstand rund 3 mm (Schrift etwa 7–7,5 pt), Balken rund 5 mm hoch (59 px), Lücke Text→Balken rund 7 mm.
     Das sind Zielwerte für das Muster und keine Prüfregel des WMS.
2. **Quelle:** Der Inhalt kommt aus der Vorlage-Datei, nicht aus dem WMS. Fehlt der Bereich, gibt es keine Fußzeile,
   keinen Hinweis und keinen reservierten Rand: Der Druck sieht dann aus wie heute (abwärtskompatibel).
   Im Ersatz-Kopf-Fall gibt es ebenfalls keine Fußzeile.
3. **Jede Seite:** Die Fußzeile erscheint auf jeder Druckseite, auch auf Folgeseiten und auf jeder Seite jedes HauptFA im
   Sammeldruck. Der Kopf bleibt nur auf Seite 1 jedes Dokuments (Teil 2, Antwort 3; `_PrintBriefkopf.cshtml:4-5`).
4. **Nichts darunter:** Kein Inhalt (Tabellenzeile, Banner, Bemerkung) wird von der Fußzeile verdeckt. Zeilen umbrechen
   auf die nächste Seite. Das gilt für den Bildschirmdruck (Chromium: Edge/Chrome am Arbeitsplatz) und für das
   Headless-Edge-PDF.
5. **Am Bildschirm** (vor dem Drucken) steht die Fußzeile einmal am Ende der Seite. So ist sie sichtbar, ohne Inhalt
   zu überdecken.
6. **Bildschirmhinweise** zu Fehlern im Fußzeilen-Bereich (abgelehnte Bilder, fehlende Dateien usw.) erscheinen in
   derselben gelben Hinweisliste wie die des Kopfs (`_PrintBriefkopf.cshtml:32-35`) und als `LogWarning`
   (`BriefkopfVorlageService.cs:71-72`). Im Druck und im PDF erscheinen sie nicht.

### Entscheidungen dieser Spec (ohne Rückfrage, Widerspruch an Schranke 1 möglich)

- **E1 Icons (Ort/Telefon/Mail): Das Muster kommt ohne Icons.** Das WMS unterstützt sie trotzdem ohne Zusatzcode:
  `<img src="ort.png">` im Fußbereich wird wie das Logo als Data-URI eingebettet (PNG ist erlaubt,
  `BriefkopfVorlageService.cs:42-46`). Begründung: Im Repo gibt es keine Icon-Quellen. Die Icons der Word-Vorlage stecken
  nur im Rasterbild `image2.png` (je Icon etwa 35 px bei 300 dpi). Ausgeschnitten würden sie auf dem Laser bei 3 mm
  Größe unscharf, und sie müssten zweifarbig (IDEAL/AKE) sechs Dateien werden. Die Angaben sind ohne Icon eindeutig
  (Anschrift, Telefonnummer, E-Mail). Will Gerald oder die IT die Icons, legt die IT die PNGs in `Branding\` und
  ergänzt die `<img>` in der Datei, ohne Deploy. Der Kommentar im Muster zeigt dafür ein Beispiel.
- **E2 Absenderzeile im Kopf entfällt im Muster**, sobald die Fußzeile die Anschrift trägt. Die Word-Vorlage zeigt oben
  nur das Logo (`2026-10-13-word-vorlage-seite.png`). Auch die hellblaue Linie unter der Absenderzeile entfällt (`.absender`
  mit `border-bottom`, `Branding/briefkopf.html:36`). Unter dem Logo folgt direkt der WMS-Teil (Empfänger/Dokument/Datum,
  `_PrintBriefkopf.cshtml:27-31`). Das WMS blendet nichts automatisch aus: Was im Kopf steht, entscheidet allein die
  Vorlage-Datei. Der **Ersatz-Kopf** (Firmenname · Anschrift aus den Standorteinstellungen) bleibt unverändert, weil es
  dort keine Fußzeile gibt.
- **E3 Markierung des Bereichs: `<footer class="briefkopf-fuss">…</footer>`** statt des vorgeschlagenen
  `<div class="briefkopf-fuss">`. Begründung: `footer` wird nie verschachtelt. Das Herauslösen ist deshalb ein einziger
  Regex-Treffer ohne Tiefenzählung über verschachtelte `div`. Der Kopf darf weiter beliebig viele `div` enthalten.
  Geralds aktuelle Vorlage hat noch keinen Fußbereich, es muss also nichts migriert werden.
- **E4 Seitenränder:** `@page` wird nur gesetzt, wenn eine Fußzeile da ist. Seiten- und Oberrand bleiben nah am heutigen
  Chromium-Standard (rund 10 mm). So brechen Tabellen nicht anders um als in TS-60/TS-81 abgenommen. Der Unterrand ist der
  CD-Unterrand **24,75 mm** ([[cd-auszug-druckdokumente]], Satzspiegel). Darin liegt die Fußzeile. Die CD-Seitenränder von
  17,5 mm werden bewusst nicht übernommen (Konsistenz vor Eigenständigkeit; Teil 2 Anforderung 8 erlaubte `@page` nur, wenn
  Bildschirmdruck und PDF gleich aussehen).
- **E5 Ablage `Branding\`:** Der Ordner ist ein Unterordner **im Programmordner** der Web-App (z. B.
  `C:\inetpub\wwwroot\IDEALAKEWMS_Test\Branding\`), **nicht** in dessen Unterordner `wwwroot\`. Nach jedem Deploy wird
  geprüft, ob er noch da ist (Gerald 2026-10-09/2026-10-13). Damit ist die heutige Doku überholt: „neben dem
  Web-Publish-Ordner" (`README.md:415`, `:454-455`), „Folge-Deploys fassen `Branding\` nicht an" (`README.md:465`),
  ebenso `Branding/briefkopf.html:9` und `docs/TESTSZENARIEN.md:9347`. Beleg, dass der Ordner nicht ausgeliefert wird:
  Statische Dateien kommen nur aus `wwwroot` (`Program.cs:570` `UseStaticFiles()` ohne eigenen FileProvider, `:572`
  `MapStaticAssets()`), und `web.config` leitet alle Anfragen an das ASP.NET-Core-Modul (`web.config:11-16`, in-process).

## Technischer Loesungsentwurf

Muster: vorhandener Briefkopf-Baustein aus Teil 2 (Service + zwei Partials), um einen dritten Partial erweitert. Keine
neue Abhängigkeit, kein Repository, keine Einstellung.

### 1. Service (`BriefkopfVorlageService.Load`)

- Record `BriefkopfVorlage` (`:16-20`) erhält `string Fuss` (leer = keine Fußzeile). `Ersatz(...)` (`:141`) liefert
  `Fuss = ""`.
- Nach `Sanitize` und dem Herauslösen der Styles (`:114-119`), und nachdem `markup` aus dem Body gewonnen ist
  (`:121-124`): den **ersten** `<footer\b[^>]*\bclass\s*=\s*["'][^"']*\bbriefkopf-fuss\b[^"']*["'][^>]*>(.*?)</footer\s*>`
  aus `markup` herausschneiden. Der Inhalt der Gruppe wird `Fuss`.
  - weitere `briefkopf-fuss`-Bereiche: entfernen und Hinweis „mehr als ein Fußzeilen-Bereich, nur der erste wird
    gedruckt";
  - `<footer` ohne `</footer>`: `footer` in die Liste offener Elemente aufnehmen (`:126`), Ersatz-Kopf mit Hinweis wie
    bei `<style>` ohne Ende. Das entspricht dem Verhalten bei kaputtem Markup (QA I-1);
  - die div-Balance (`:129-131`) wird **getrennt** für Kopf-Markup und Fuss-Markup geprüft. Eine Unwucht im Fuß würde
    sonst den Body der Druckseite verschieben.
- `EmbedCssUrls`/`EmbedSrcAttributes` (`:133-136`) laufen mit **derselben** `Embedder`-Instanz auch über `Fuss`. Größen-
  und Ausgabegrenzen (`:33-39`) gelten damit gemeinsam, und eine Datei, die Kopf und Fuß beide nutzen, zählt nur einmal.
- Der Bereich wird erkannt, egal ob die IT ihn direkt in `<body>` oder verschachtelt schreibt.

### 2. Fuss-Partial `_PrintBriefkopfFuss.cshtml` (neu)

```
@inject BriefkopfVorlageService Briefkopf
@{ var v = await Briefkopf.GetAsync(); }
@if (!v.IstErsatz && !string.IsNullOrWhiteSpace(v.Fuss))
{
    <div class="briefkopf briefkopf-fussbereich">@Html.Raw(v.Fuss)</div>
}
```

- Die Klasse `briefkopf` sorgt dafür, dass die Regeln der IT (alle unter `.briefkopf`, Muster-Kommentar) auch im Fuß
  greifen, ohne die Skopierregel aufzuweichen. `GetAsync` liest die Vorlage einmal je Anfrage (`:61-75`), der dritte
  Aufruf liest also nichts nach.
- Eingebunden wird der Partial **genau einmal je View** und nicht je `.fa-group`. Die Data-URIs stehen so einmal im HTML
  (anders als beim Logo, `_PrintBriefkopf.cshtml:16-17`):
  - `FaHierarchyBeschichtung/Print.cshtml` und `PrintSummiert.cshtml`: im `else`-Zweig vor der `foreach`. Der Leerfall
    („nichts zu drucken") bekommt damit keine Fußzeile;
  - `Beschichtungsauftraege/Print.cshtml`: nach den `screen-controls` (`:38-41`).

### 3. Styles (`_PrintBriefkopfStyles.cshtml`)

Nur wenn `!IstErsatz && Fuss` nicht leer ist, zusätzlich ausgeben:

- Bildschirm: `.briefkopf-fussbereich { margin-top: 10mm; }` (statisch am Ende, Anforderung 5).
- Druck: `@page { size: A4; margin: 10mm 10mm 24.75mm 10mm; }` (E4) und die Positionierung der Fußzeile, so dass sie
  sich je Seite wiederholt und im reservierten Unterrand liegt.
- Das Aussehen (Spalten, Farben, Balken) gehört der Vorlage (IT). Das WMS bestimmt nur **wo** die Fußzeile steht und
  **wie viel** Platz reserviert ist. Vertrag mit der IT, im Muster-Kommentar dokumentiert: Die Fußzeile ist höchstens
  so hoch wie der Unterrand abzüglich 2 mm Abstand zum Inhalt.

**Technik für „jede Seite + Platz reserviert": nicht belegt, deshalb zuerst messen.** Für Chromium gilt nach
Kenntnisstand, dass `position: fixed` im Druck auf jeder Seite wiederholt wird. Das ist im Repo nicht belegt und wurde in
diesem Lauf nicht gemessen. Ungeklärt ist auch, ob ein fixiertes Element in den `@page`-Rand gezeichnet wird. Der
Dev-Lauf beginnt deshalb mit **Prüfschritt P1/P2** (unten) an einer Minimal-HTML-Datei und nimmt die **erste** Variante,
die P1 **und** P2 besteht:

| # | Variante | Reservierung | Risiko |
|---|---|---|---|
| V1 | `@media print { .briefkopf-fussbereich { position: fixed; left: 0; right: 0; bottom: -<Unterrand>; height: <Unterrand>; } }` | `@page`-Unterrand (exakt, kein Fluss-Eingriff) | wird das fixe Element in den Seitenrand gezeichnet? |
| V2 | `@page` Unterrand klein (z. B. 5 mm), Fußzeile `position: fixed; bottom: 0` im Seitenbereich | Abstandhalter in einer `tfoot`-Zeile: Inhalt jeder `.fa-group` in eine Rahmen-Tabelle, deren `tfoot` sich je Seite wiederholt | Eingriff in drei Views, verschachtelte Tabellen, `page-break-after` in Tabellenzelle |
| V3 | wie V2 | `box-decoration-break: clone` + `padding-bottom` auf `.fa-group` | Chromium-Unterstützung für Blockfragmente |

Hält keine Variante in **beiden** Wegen (Bildschirmdruck und Headless-PDF), wird **nicht** gebaut. Der Dev-Lauf
dokumentiert das Messergebnis in der Spec und meldet sich, statt halb zu liefern. Die gewählte Variante samt
Messprotokoll kommt mit Warum in `fallstricke.md` (Abschnitt 11 PDF-Erzeugung oder ein neuer Druck-Abschnitt).

**Balken drucken:** Chromium lässt Hintergründe im Bildschirmdruck weg, wenn „Hintergrundgrafiken" aus ist. Das
Muster setzt deshalb am Balken `-webkit-print-color-adjust: exact; print-color-adjust: exact;` und den Verlauf
`linear-gradient(135deg, #053153 50%, #43a6e2 50%)`. Eine Winkelangabe hält den Übergang bei jedem Seitenverhältnis auf
45°. Ob das im Druckdialog ohne Haken „Hintergrundgrafiken" und im Headless-PDF tatsächlich druckt, prüft P4. Bei
negativem P4 bauen zwei Rahmen-Dreiecke den Balken (Rahmen drucken immer).

### 4. „Seite X von Y" (nur bei Rückfrage 3 = B)

Heute erzeugt jeder PDF-Weg genau **ein** Dokument je Datei (`Pdf`: eine Gruppe, `FaHierarchyBeschichtungController.cs:124-129`;
`PdfSummiert`: `:210-216`; der Auftragsdruck hat immer eine `.fa-group`, `Beschichtungsauftraege/Print.cshtml:43`). Ein
`@page`-Zähler wäre dort je Dokument korrekt. Im Sammeldruck mit mehreren HauptFA zählt er über alle Dokumente; das ist
der Grund der offenen Aufgabe. Variante B: eine `@page { @bottom-right { content: "Seite " counter(page) " von " counter(pages); } }`-Regel
nur ausgeben, wenn die View **genau ein** Dokument rendert (`Model.Groups.Count == 1` bzw.
`Rows.Select(r => r.HauptFA).Distinct().Count() == 1`; Auftragsdruck immer). Damit ist Teil-2 AK 14 („je Dokument korrekt
oder gar nicht") eingehalten. Lage: im Unterrand rechts über der Web-Adresse, endgültig nach P3. **Voraussetzung:** P3
zeigt, dass der Edge des Servers und der Browser am Arbeitsplatz `@page`-Randfelder mit `counter(pages)` rendern, auch
mit `--no-pdf-header-footer` (`EdgeProcessRunner.cs:38`). Nach Kenntnisstand unterstützt Chromium Randfelder seit
Version 131. Das ist in diesem Lauf **nicht belegt**, deshalb steht es als Prüfschritt da. Gemessene Edge-Version am
2026-09-14: 152.0.4191.66 (`fallstricke.md` Abschnitt 11). Ob das der Server war, ist dort nicht vermerkt.

### 5. Muster `Branding/briefkopf.html`

Vorlage ist Geralds Datei `2026-10-13-briefkopf-gerald.html`, mit diesen Änderungen:
- Logozone `justify-content: flex-end` (Logo rechts oben; hat Gerald schon, `:29`).
- Schriften `NimbusSanNov-Reg.otf` / `NimbusSanNov-SemBol.otf` mit `format('opentype')` (Gerald `:26-27`; heute im Repo
  `NimbusSansNovus-*.woff2`, `Branding/briefkopf.html:31-32`).
- `.absender` entfällt (E2). Der Kommentar `:22-23` bei Gerald („Fußzeile kann diese Datei noch nicht abbilden") wird
  ersetzt.
- Fußbereich `<footer class="briefkopf-fuss">` mit drei Spalten (`display: flex; justify-content: space-between;
  align-items: flex-end`), Balken als eigenes Element, alle Regeln unter `.briefkopf …`.
- Kommentar: Ablage laut E5, Fuß-Regeln (höchstens so hoch wie reserviert, gleiche Datei-/Script-Regeln wie im Kopf,
  fehlt der Bereich, gibt es keine Fußzeile), Beispiel für ein optionales Icon (E1). AKE-Mail `office@ideal-ake.at`.
- In der Kopfzeile des Kommentars aus „ab v1.49.0" „Fußzeile ab v<neu>" ergänzen (Version beim Dev messen).

### 6. Tests (automatisiert)

`BriefkopfVorlageServiceTests` (Temp-Ordner-Muster der Datei):
- Fußbereich vorhanden: `Fuss` enthält den Inhalt, `Markup` enthält ihn nicht mehr.
- kein Fußbereich: `Fuss` leer, keine Hinweise.
- Ersatz-Kopf: `Fuss` leer.
- `<footer class="briefkopf-fuss">` ohne Ende: Ersatz + Hinweis.
- zwei Fußbereiche: nur der erste, Hinweis.
- Unwucht von `div` nur im Fuß: Ersatz + Hinweis.
- Bild im Fuß: Data-URI; `http`-Bild im Fuß: abgelehnt + Hinweis.
- Script/`on*` im Fuß: entfernt.
- Summengrenze gilt über Kopf und Fuß gemeinsam.

`MusterVorlage_…` (`:337-357`) anpassen:
- Hinweise nennen `NimbusSanNov` statt `NimbusSansNovus` (`:349`).
- Die Anschrift steht jetzt in `Fuss` und nicht mehr in `Markup` (`:347`); AKE-Block und `office@ideal-ake.at` zweimal
  prüfen, `office@ake.at` darf nicht vorkommen.
- Kontrast-Assertion `:355` laut Rückfrage 1.

`BriefkopfEinbindungTests`: `_PrintBriefkopfFuss` genau einmal in den drei Views (auch `Beschichtungsauftraege/Print.cshtml`,
den der Test heute nicht prüft), **nicht** innerhalb der `.fa-group`. Kommissionierlisten enthalten den Fuß-Partial
nicht (bestehender Test `:72-73` deckt `_PrintBriefkopf*` per `NotContain("_PrintBriefkopf")` schon ab).

Nicht automatisierbar und deshalb Manual-UAT: Seitenumbruch, Wiederholung je Seite, PDF-Rendering, Farben
(`fallstricke.md`, „Tests, EF, SQL Server").

### Prüfschritte (vor bzw. zu Beginn des Dev-Laufs, Ergebnis in die Spec)

- **P0 — Edge-Version am Webserver** (PowerShell auf dem Server, Pfad wie in der PDF-Einstellung konfiguriert):
  `(Get-Item '<Pfad zu msedge.exe>').VersionInfo.ProductVersion` → Ergebnis: ______
- **P1 — Bildschirmdruck:** Minimal-HTML (drei `.fa-group` mit `page-break-after`, je 80 Tabellenzeilen, Fußzeile laut
  Variante) in Edge **und** Chrome am Arbeitsplatz öffnen, Druckvorschau mit Rändern „Standard". OK, wenn die Fußzeile auf
  jeder Seite steht, keine Zeile unter ihr liegt und jede Gruppe auf einer neuen Seite beginnt.
- **P2 — Headless-PDF** mit genau den Argumenten aus `EdgeProcessRunner.cs:37-39`:
  `"<msedge.exe>" --headless --disable-gpu --no-first-run --no-default-browser-check --user-data-dir="%TEMP%\fuss-spike" --no-pdf-header-footer --print-to-pdf="%TEMP%\fuss-spike.pdf" "<pfad>\spike.html"`
  OK wie P1, zusätzlich: `@page`-Ränder wirken im PDF genauso wie in der Vorschau.
- **P3 — nur bei Rückfrage 3 = B:** dieselbe Datei mit `@page { @bottom-right { content: counter(page) " / " counter(pages); } }`.
  OK, wenn in P1 und P2 „1 / 3" … erscheint.
- **P4 — Balken:** Druckvorschau mit **ausgeschaltetem** „Hintergrundgrafiken" und P2-PDF. OK, wenn der Balken
  zweifarbig mit 45°-Übergang sichtbar ist.

## Migrations-/SQL-Auswirkungen

Keine. Keine Entity, keine Spalte, kein Key, kein `SQL/XX_*.sql`, kein FreshInstall-Nachtrag, keine Agent-Jobs. Die
Vorlage liegt als Datei im Dateisystem, der Pfad in der bestehenden Einstellung `BriefkopfVorlagePfad` (Teil 2).

## Audit-Feld-Auswirkungen

Keine. Reiner Lese-/Render-Pfad, es wird nichts geschrieben.

## Rollen / Zugriff / Listen-View-Pattern

- Rollen und Filter unverändert: Die drei Druckansichten behalten ihre Class-/Action-Filter und das Gate
  `FaHierarchyBeschichtungAktiv` (Teil 2/4). Keine neue Rolle, kein neuer Filter, also auch keine Drei-Stellen-Pflicht.
- Keine neue Tabellen- oder Listenansicht. Das Listen-View-Pattern ist nicht betroffen (Druckansichten sind ungepaged,
  wie bisher).

## Akzeptanzkriterien

1. Mit einer Vorlage mit `<footer class="briefkopf-fuss">` steht im **Bildschirmdruck** (Edge und Chrome, Ränder
   „Standard") auf **jeder** Seite die Fußzeile, auch auf Folgeseiten eines HauptFA und auf allen Seiten aller HauptFA
   im Sammeldruck (`Print`, `PrintSummiert`).
2. Dasselbe im **PDF** (`Pdf`, `PdfSummiert`, `Beschichtungsauftraege/Pdf`).
3. Keine Tabellenzeile, kein Banner und keine Bemerkung liegt ganz oder teilweise unter der Fußzeile. Zwischen letzter
   Inhaltszeile und Fußzeile bleiben mindestens 2 mm (am Ausdruck und im PDF mit Lineal bzw. Zoom geprüft).
4. Der Kopf steht weiterhin nur auf Seite 1 jedes Dokuments. Jeder HauptFA beginnt im Sammeldruck auf einer neuen Seite.
5. Die Fußzeile des Musters zeigt die Inhalte aus Anforderung 1 in der dort genannten Anordnung und Farbe. Beide Blöcke
   zeigen `office@ideal-ake.at`, `office@ake.at` kommt nicht vor.
6. Der Balken ist zweifarbig mit 45°-Übergang und erscheint im Bildschirmdruck auch **ohne** „Hintergrundgrafiken" und
   im PDF. Lage und Breite laut Rückfrage 2.
7. Ohne Fußbereich in der Vorlage: keine Fußzeile, kein Hinweis, keine `@page`-Regel im Quelltext der Druckseite. Der
   Druck ist identisch mit dem heutigen (Vergleich der Seitenzahl eines mehrseitigen HauptFA vorher/nachher).
8. Im Ersatz-Kopf-Fall (Pfad leer, Datei fehlt, kaputtes Markup) gibt es keine Fußzeile.
9. Fehler im Fußbereich (Bild fehlt, `http`-Bild, Script, `on*`) behandelt das WMS wie im Kopf: entfernt bzw. nicht
   eingebettet, gelber Hinweis nur am Bildschirm, `LogWarning`. Im Druck und im PDF erscheint kein Hinweis.
10. `<footer class="briefkopf-fuss">` ohne `</footer>` oder mit ungleicher `div`-Zahl führt zu Ersatz-Kopf + Hinweis.
    Zwei Fußbereiche: Nur der erste wird gedruckt, mit Hinweis.
11. Im Quelltext jeder Druckseite steht die Fußzeile genau **einmal** (nicht je HauptFA). Alle `src=`/`url(` darin
    beginnen mit `data:`.
12. Am Bildschirm steht die Fußzeile einmal am Seitenende und überdeckt nichts.
13. Kommissionierlisten (Liste und Summiert) zeigen weder Kopf noch Fußzeile.
14. Das Muster `Branding/briefkopf.html` hat das Logo rechts oben, die Schriften `NimbusSanNov-Reg.otf`/`-SemBol.otf` mit
    `format('opentype')`, den Fußbereich, keine Absenderzeile und den korrigierten Ablage-Kommentar (E5). Der Muster-Test
    ist grün.
15. README, Hilfe (`Help/Index.cshtml:786` „Der Briefkopf steht nur auf der ersten Seite …" um die Fußzeile ergänzt,
    `:819` Ablage) und die TS-82-Vorbedingung nennen die Ablage laut E5.
16. *(nur bei Rückfrage 3 = B)* PDF und Auftragsdruck zeigen „Seite X von Y" je Dokument korrekt. Der Sammeldruck mit
    mehreren HauptFA zeigt **keine** Seitenzählung, der mit genau einem HauptFA zeigt sie korrekt.
17. Die Prüfschritte P0–P4 sind mit Ergebnis in der Spec festgehalten, die gewählte Variante (V1/V2/V3) samt Warum in
    `fallstricke.md`.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md`, Nummer beim `/dev` messen (Stand jetzt: höchstes Kapitel TS-91 → voraussichtlich
**TS-92**). Entwurf:

**TS-NN — IDEAL: Fußzeile nach Word-Vorlage auf dem Beschichtungsauftrag (v<neu>)**
Spec: `2026-10-13-briefkopf-fusszeile-word-vorlage-spec`. **Vorbedingungen:** wie TS-82. Zusätzlich `Branding\` im
Programmordner der Web-App mit dem neuen Muster (Fußbereich) und den Schriftdateien `NimbusSanNov-Reg.otf`/`-SemBol.otf`.
Kandidaten mit vielen Positionen: Abfrage aus TS-82.3. Automatisiert: `BriefkopfVorlageServiceTests`,
`BriefkopfEinbindungTests`. Seitenumbruch und Darstellung sind Manual-UAT.

- **TS-NN.1 — Bildschirmdruck, ein HauptFA, mehrseitig (AK 1, 3, 4).** „Liste" → HauptFA mit ≥ 40 Positionen →
  „Drucken" → Druckvorschau (Edge, Ränder „Standard"). **Erwartet:** Kopf nur Seite 1, Fußzeile auf jeder Seite, letzte
  Zeile jeder Seite vollständig über der Fußzeile, nächste Zeile oben auf der Folgeseite.
- **TS-NN.2 — Sammeldruck, mehrere HauptFA (AK 1, 4, 11).** Summiert ohne HauptFA-Filter (≥ 3 HauptFA, einer davon
  mehrseitig) → „Drucken". **Erwartet:** jeder HauptFA auf neuer Seite mit Kopf, Fußzeile auf allen Seiten. Quelltext
  (Strg+U): `briefkopf-fussbereich` genau einmal.
- **TS-NN.3 — PDF (AK 2, 3).** PDF-Knopf am mehrseitigen HauptFA (Liste und Summiert) und PDF eines übergebenen Auftrags.
  **Erwartet:** wie TS-NN.1; PDF → Eigenschaften → Schriftarten: Nimbus Sans Novus eingebettet.
- **TS-NN.4 — Lange Positionsliste, Grenzzeile (AK 3).** HauptFA aus TS-NN.1: an jedem Seitenübergang die letzte Zeile
  am Ausdruck mit Lineal prüfen. **Erwartet:** ≥ 2 mm Abstand zur Fußzeile, keine halbe Zeile, keine Zeile doppelt oder
  fehlend (Zeilenzahl am Ausdruck = Zeilenzahl am Bildschirm).
- **TS-NN.5 — Inhalt und CD (AK 5, 6).** Am Ausdruck: links IDEAL-Block (Name IDEAL Blau, semibold), Mitte AKE-Block
  (Farbe laut Rückfrage 1), rechts `www.ideal-ake.at`, beide Mails `office@ideal-ake.at`, Balken zweifarbig mit
  45°-Übergang. Zusätzlich drucken mit **ausgeschaltetem** „Hintergrundgrafiken": Der Balken bleibt sichtbar.
- **TS-NN.6 — Vorlage ohne Fußbereich (AK 7).** `<footer class="briefkopf-fuss">…</footer>` aus der Vorlage löschen,
  TS-NN.1 wiederholen. **Erwartet:** keine Fußzeile, kein gelber Hinweis, gleiche Seitenzahl wie vor dem Deploy (vorher
  notieren), Quelltext ohne `@page`.
- **TS-NN.7 — Ersatz-Kopf (AK 8).** Pfad in den Standorteinstellungen leeren. **Erwartet:** Ersatz-Kopf, keine Fußzeile,
  gelber Hinweis nur am Bildschirm. Pfad wiederherstellen.
- **TS-NN.8 — Fehler im Fußbereich (AK 9, 10).** Nacheinander im Fuß: (a) `<img src="https://www.ideal-ake.at/x.png">`,
  (b) `<img src="fehlt.png">`, (c) `<script>alert(1)</script>`, (d) `</footer>` löschen, (e) ein überzähliges `</div>`
  im Fuß, (f) den Fußbereich verdoppeln. **Erwartet:** (a)/(b) Fuß ohne Bild + Hinweis; (c) kein Dialog, Quelltext ohne
  `script`; (d)/(e) Ersatz-Kopf + Hinweis, alle HauptFA vollständig; (f) eine Fußzeile + Hinweis. Danach wiederherstellen.
- **TS-NN.9 — Am Bildschirm (AK 12).** Druckansicht öffnen, nicht drucken. **Erwartet:** Fußzeile einmal am Ende, nichts
  überdeckt, beim Scrollen kein schwebender Balken.
- **TS-NN.10 — Regression Kommissionierliste (AK 13).** Kommissionierliste Liste und Summiert drucken. **Erwartet:** kein
  Kopf, keine Fußzeile, Seitenränder wie bisher.
- **TS-NN.11 — Ablage nach Deploy (AK 15, E5).** Nach dem Publish auf das Testsystem prüfen, dass
  `<Programmordner>\Branding\briefkopf.html` noch existiert und `https://<host>/Branding/briefkopf.html` **nicht**
  ausgeliefert wird (404).
- **TS-NN.12 — nur bei Rückfrage 3 = B (AK 16).** PDF eines mehrseitigen HauptFA: „Seite 1 von N" … „Seite N von N".
  Sammeldruck mit drei HauptFA: keine Seitenzählung. Sammeldruck gefiltert auf einen HauptFA: korrekte Zählung.

**Auswirkung auf das Protokoll [[2026-10-13-ts-81-90-browser-testprotokoll]]** (wird hier **nicht** geändert; gilt für
Läufe **nach** dem Deploy dieser Spec und nur, wenn die IT das neue Muster übernimmt):

- **B42 / TS-82.2:** Die Erwartung „darunter Absenderzeile ‚IDEAL Kaeltetechnik GmbH - In der Schoerihub 28 …' … duenne
  hellblaue Linie" (`:745-746`) gilt nicht mehr. Unter dem Logo (rechts oben) folgt direkt Empfänger/Dokument/Datum, die
  Anschrift steht in der Fußzeile auf jeder Seite. Mit Geralds heutiger Vorlage (ohne Fußbereich) bleibt B42 unverändert.
- **B43 / TS-82.2 (PDF):** Zusätzlich Fußzeile im PDF auf jeder Seite.
- **B44 / TS-82.3:** „Briefkopf nur auf Seite 1" bleibt gültig. Neu: Fußzeile auf jeder Seite, „Folgeseiten beginnen mit
  Positionen" bleibt. „Keine Seite X von Y" (`:759-760`) bleibt nur bei Rückfrage 3 = A gültig. Bei B gilt es für den
  Sammeldruck mit mehreren HauptFA weiter, für den einzelnen HauptFA nicht.
- **B46 / TS-81.14:** „Briefkopf wie in B42" erbt die Änderung von B42.
- **B54 / TS-84.8:** Auftragsdruck und -PDF zeigen zusätzlich die Fußzeile. Sonst unverändert.
- **TS-82.5 (Ersatz-Kopf):** ergänzen „keine Fußzeile". **TS-82.7 (Schriften fehlen):** die Dateinamen heißen im neuen
  Muster `NimbusSanNov-*.otf`. **TS-82.14:** Der Satz „kein `@page`-Umbau" (`docs/TESTSZENARIEN.md:9403`) gilt nur noch
  ohne Fußbereich; mit Fußbereich Unterrand 24,75 mm (E4). **TS-82-Vorbedingung** (`:9347`): Ablage laut E5.
- B15 (TS-82.13) und B16 (TS-82.12) bleiben unberührt.

## Etappen (nur bei epic: true)

Kein Epic. Die Umsetzung erfolgt direkt im Bündel-Worktree (Auftrag Gerald) in einem Commit. Reihenfolge im Dev-Lauf:
P0–P4 messen → Service + Tests → Partials/Styles → Muster → Doku.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | (entfällt) | – | – |

## Deploy

*Vorläufig. Der QA-Lauf bestätigt das am echten Diff.*

- **Web-App:** ja (Service, Partials, drei Views, Hilfe, Version).
- **Service:** nein (`AppVersion.cs` des Service wird nach Hausregel mitgezählt, ein Deploy ist nicht nötig).
- **Migration:** nein.
- **Zusätzlich, manuell (IT/Gerald):** neues Muster `Branding/briefkopf.html` als Vorlage nehmen, Geralds Datei im
  `Branding\` des Programmordners um den Fußbereich ergänzen. Nach dem Deploy prüfen, dass `Branding\` noch da ist
  (TS-NN.11). Ohne Fußbereich in der Datei ändert sich am Druck nichts (AK 7), die Reihenfolge Deploy/Datei ist also
  beliebig.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
- Version und TS-Nummer beim `/dev` neu messen (Stand jetzt v1.58.1, TS-91).

## Offene Rueckfragen

1. **AKE Blau als Textfarbe (Konflikt).** Der Auftrag und das CD (Adressblock: „Firmenbezeichnung … in der Hausfarbe",
   [[cd-auszug-druckdokumente]]) wollen den AKE-Firmennamen in `#43a6e2`. Teil 2 hat AKE Blau wegen 2,70:1 Kontrast auf
   Weiß bewusst **nicht** als Textfarbe zugelassen (Teil-2-Spec Anforderung 8 und AK 11; Test
   `BriefkopfVorlageServiceTests.cs:355` verbietet `color: #43a6e2` in den Vorlage-Styles; CLAUDE.md: Zielwert WCAG AA).
   Ausnahme **nur** für den AKE-Firmennamen in der Fußzeile zulassen (Test wird auf diese eine Regel eingegrenzt)?
   *Empfehlung: ja.* Gedrucktes Außendokument, CD-Vorgabe, Name semibold. Die Farbe trägt keine Bedeutung allein, und der
   Name ist auch ohne Farbe lesbar. Alternative: Name in IDEAL Blau, nur der Balken in AKE Blau.
2. **Balken: randabfallend oder innerhalb der Ränder?** A) bis an die Papierkante wie in Word. Im PDF identisch zur
   Vorlage; Bürodrucker drucken die äußeren ~4–5 mm meist nicht, der Balken wird dort abgeschnitten bzw. bekommt einen
   weißen Rand. Technisch muss die Fußzeile dafür über die `@page`-Ränder hinaus gezeichnet werden (zusätzliches Risiko in
   P1/P2). B) zwischen den Seitenrändern (rund 10 mm links/rechts), mit etwa 5 mm Abstand zur Unterkante. Druckt auf jedem
   Gerät gleich, weicht aber sichtbar von Word ab. *Empfehlung: B.*
3. **„Seite X von Y" als Option?** A) weiter weglassen, die Aufgabe bleibt offen. B) nur dort, wo ein Druck genau ein
   Dokument enthält (alle PDFs, Auftragsdruck, Sammeldruck mit genau einem HauptFA), Sammeldruck mit mehreren HauptFA
   ohne Zählung (Lösungsentwurf 4). Nur, wenn P3 positiv ist. *Empfehlung: B, bei negativem P3 automatisch A*, und die
   Aufgabe [[2026-10-09-briefkopf-seitenzaehlung]] bleibt für den Sammeldruck offen.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. AKE Blau als Textfarbe nur für den AKE-Firmennamen in der Fußzeile zulassen? →
2. Balken randabfallend (A) oder innerhalb der Seitenränder (B)? →
3. „Seite X von Y": weglassen (A) oder nur bei genau einem Dokument je Druck (B, wenn P3 positiv)? →
