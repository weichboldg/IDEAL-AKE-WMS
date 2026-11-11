---
type: spec
title: "Briefkopf: Fußzeile nach Firmen-Word-Vorlage (+ Muster-Vorlage nachziehen)"
slug: 2026-10-13-briefkopf-fusszeile-word-vorlage-spec
status: Testbereit
created: 2026-10-13
updated: 2026-10-14
source_backlog: "[[2026-10-09-briefkopf-seitenzaehlung]]"
anhaenge:
  - "[[2026-10-13-word-vorlage-fusszeile.png]]"
  - "[[2026-10-13-word-vorlage-seite.png]]"
  - "[[2026-10-13-briefkopf-gerald.html]]"
depends_on: "[[2026-10-06-beschichtungsauftrag-ausbau-teil-2-spec]]"
task: "[[2026-10-14-briefkopf-fusszeile-word-vorlage-umsetzung]]"
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
open_questions: []
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: "Freigegeben wie Empfehlungen: 1 ja (AKE Blau nur fuer AKE-Firmennamen in der Fusszeile), 2 B (Balken innerhalb der Seitenraender), 3 B (Seite X von Y nur bei genau einem Dokument je Druck, wenn P3 positiv, sonst A). Direkt im Buendel."
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-10-14"
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

### Freigabe-Nachtrag 2026-10-14 (geht vor, Gerald)

- **Rückfrage 1 = ja:** AKE Blau `#43a6e2` als Textfarbe ist **nur** für den AKE-Firmennamen in der Fußzeile erlaubt;
  Anschrift/Telefon/Mail bleiben Grau 85. Der Kontrast-Test wird auf genau diese eine Regel eingegrenzt, sonst bleibt das Verbot.
- **Rückfrage 2 = B:** Balken innerhalb der Seitenränder, etwa 5 mm über der Unterkante — nicht randabfallend.
- **Rückfrage 3 = B:** „Seite X von Y" nur dort, wo ein Druck genau ein Dokument enthält (jedes PDF, Auftragsdruck,
  Sammeldruck mit genau einer HauptFA); Sammeldruck mit mehreren HauptFA ohne Zählung. Ist P3 negativ, entfällt die
  Zählung ganz (= A) und der Lauf meldet das. Aufgabe [[2026-10-09-briefkopf-seitenzaehlung]] bleibt für den Sammeldruck offen.
- Übrige „Entscheidungen dieser Spec" unverändert bestätigt (Muster ohne Icons, Absenderzeile entfällt, `<footer class="briefkopf-fuss">`,
  `@page`-Rand nur mit Fußzeilen-Bereich, Fußzeile einmal je Druckansicht, Branding-Ordner im Programmordner).

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

*Vom QA-Lauf 2026-10-14 am echten Diff `ead16fc4..d4108f1a` bestätigt.* Geänderte Pfade: `IdealAkeWms/` (Service,
3 Partials/Views, Hilfe, Changelog, AppVersion), `IDEALAKEWMSService/AppVersion.cs` (nur Versionsnummer/Datum),
`Branding/briefkopf.html` (nicht im Publish-Output), Tests, README, `docs/`. Keine Datei unter `*/Migrations/`, kein `SQL/`.

- **Web-App:** ja.
- **Service:** nein. Nur `AppVersion.cs` hat sich geändert (Hausregel). Ein Re-Deploy ist nicht nötig; danach zeigt der Service
  im Log weiter v1.58.x. Das ist gewollt.
- **Migration:** nein. Kein DB-Backup nötig, kein Einmal-Skript.
- **Reihenfolge:** Publish Web aus dem Worktree → Testsystem → TS-92 (inkl. TS-92.11: `Branding\` noch da?) → Merge.
  Nach dem Merge nur dann erneut aus `main` publishen, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen
  zusammengeführt hat.
- **Zusätzlich, manuell (IT/Gerald):** neues Muster `Branding/briefkopf.html` als Vorlage nehmen, Geralds Datei im
  `Branding\` des Programmordners um den Fußbereich ergänzen. Nach dem Deploy prüfen, dass `Branding\` noch da ist
  (TS-NN.11). Ohne Fußbereich in der Datei ändert sich am Druck nichts (AK 7), die Reihenfolge Deploy/Datei ist also
  beliebig.
- **Publish-Befehl** (im Worktree `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5`):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
- Gemessen: v1.59.0, TS-92. Das Bündel enthält weitere nicht deployte Stände (v1.49–v1.58.1). Deren Deploy-Pflichten
  (Migrationen, Einmal-Skripte) stehen in deren Specs und gelten beim Bündel-Deploy weiter.

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

1. AKE Blau als Textfarbe nur für den AKE-Firmennamen in der Fußzeile zulassen? → **Ja (Gerald 2026-10-14).**
2. Balken randabfallend (A) oder innerhalb der Seitenränder (B)? → **B (Gerald 2026-10-14).**
3. „Seite X von Y": weglassen (A) oder nur bei genau einem Dokument je Druck (B, wenn P3 positiv)? → **B, bei negativem P3 automatisch A (Gerald 2026-10-14).**

## Umsetzung (2026-10-14, Dev-Lauf)

Direkt im Bündel `feature/2026-08-07-ideal-teile-1-5`, Start-HEAD `ead16fc4`, Code-Commit `c2d54f35`. Gemessen: Version
vorher v1.58.1 → **v1.59.0**, höchstes TS-Kapitel TS-91 → **TS-92**. Hinweis: Die Datei liegt noch unter `specs/entwurf/`
(Status Freigegeben, der Auftrag nannte `specs/freigegeben/`); nicht verschoben, damit keine Wikilinks brechen.

### Prüfschritte P0–P4 (statische Testseite + lokales Edge, keine App)

Testseite: drei `.fa-group` mit je 80 Tabellenzeilen, Bemerkung nach der Tabelle, Fußzeile laut Variante; PDF mit genau den
Argumenten aus `EdgeProcessRunner` (`--headless --disable-gpu --no-first-run --no-default-browser-check --user-data-dir=…
--no-pdf-header-footer --print-to-pdf=…`). Auswertung des PDFs mit PyMuPDF (nur im Scratchpad): Lage jeder Zeile und der
Fußzeile je Seite, Pixelfarben des Balkens, Text „Seite n von N".

| Prüfschritt | Ergebnis |
|---|---|
| **P0** Edge-Version | Arbeitsplatz **155.0.4283.45**. Server-Version **offen** → TS-92.13 (Gerald). |
| **P1** Bildschirmdruck | **Nur indirekt**: gleiche Chromium-Druck-Engine wie P2; die Druckvorschau selbst wurde nicht angesehen (keine App, kein interaktiver Browser). Sichtprüfung = TS-92.1/92.4/92.5. |
| **P2** Headless-PDF | **V1 scheitert:** `position: fixed` mit negativem `bottom` im `@page`-Rand wird oben auf jeder Seite über den Inhalt gezeichnet, Balken fehlt. **V2 scheitert an AK 3:** Platzhalter im `tfoot` schützt nur Tabellenzeilen, die Bemerkung nach der Tabelle lag 12,5 mm unter der Fußzeile. **V3 trägt:** Fußzeile auf jeder Seite an gleicher Stelle, Abstand letzte Zeile → Fußzeile auf allen Seiten ≥ 2,6 mm, Bemerkung nie darunter, jede Gruppe auf neuer Seite. |
| **P3** Seitenzähler | **Positiv:** `@page { @bottom-right { content: "Seite " counter(page) " von " counter(pages) } }` erscheint trotz `--no-pdf-header-footer` („Seite 1 von 2", „Seite 2 von 2"). → Antwort 3 = **B** umgesetzt. |
| **P4** Balken | **Positiv im PDF:** Pixel links `#043153`, rechts `#42a6e2`, 45°-Übergang. „Hintergrundgrafiken aus" im Druckdialog ist ohne App nicht messbar → TS-92.5 (`print-color-adjust: exact` gesetzt). |

**Gegenmessung mit dem echten Muster** (`Branding/briefkopf.html` + Styles aus `_PrintBriefkopfStyles.cshtml`, 2 HauptFA à 80
Zeilen bzw. 1 HauptFA mit Zähler): Fußtext 274,6–286,4 mm, Balken 289–292 mm (innerhalb der Ränder, 5 mm über der
Blattkante), Abstand zum Inhalt ≥ 3,7 mm, 4 bzw. 2 Seiten ohne Leerblatt, „Seite 2 von 2" korrekt. Gemessen ohne
Nimbus-Dateien (Fallback-Schrift); mit Nimbus kann die Höhe leicht abweichen — Überlänge wird abgeschnitten, nie überdeckt.

### Entscheidungen im Lauf (Rulings)

- **Bauweise V3:** `@page { margin: 10mm 10mm 5mm 10mm }`, Fußzeile `position: fixed; bottom: 0; max-height: 17.75mm;
  overflow: hidden`, `.fa-group { box-decoration-break: clone; padding-bottom: 19.75mm }` (Polster auf jedem
  Seitenfragment). Warum: einzige Variante, die AK 3 in P2 erfüllt. Kosten, wenn falsch: Sichtprüfung TS-92.4 zeigt es.
- **Fuß-Partial hinter den Dokumenten** (nicht vor der Gruppenschleife wie im Lösungsentwurf): so steht die Fußzeile am
  Bildschirm am Seitenende (Anforderung 5/AK 12). Dafür `.fa-group:last-of-type { page-break-after: auto }`, weil
  `:last-child` nach dem angehängten `<footer>` nicht mehr greift (sonst Leerblatt — im Spike gesehen).
- **Zähler** im 5-mm-Unterrand rechts unter dem Balken (7 pt, Grau 85); nur bei genau einem Dokument
  (`Print`: `Groups.Count == 1`, `PrintSummiert`: genau ein HauptFA, Auftragsdruck immer).
- **Balken 3 mm** statt 5 mm (Word), damit Fußzeile ≤ 17,75 mm bleibt (Unterrand gesamt 24,75 mm laut E4).
- **Fußzeile gekappt** auf 17,75 mm (`overflow: hidden`): eine zu hohe IT-Fußzeile wird unten abgeschnitten statt Inhalt zu
  verdecken. Steht im Muster-Kommentar und im README.
- **Service:** erster `<footer class="briefkopf-fuss">` wird `Fuss`; ohne `</footer>` → Ersatz; mehrere → erster + Hinweis;
  `div`-Balance getrennt für Kopf und Fußzeile; ein gemeinsamer `Embedder` (Grenzen gelten zusammen). `HatFuss` ist im
  Ersatz-Fall immer false. Ohne Fußbereich wird **keine** zusätzliche Regel ausgegeben (AK 7, Wächter-Test).

### Nachweis

- `dotnet build IdealAkeWms.slnx` → **0 Fehler** (12 Warnungen, Bestand).
- `dotnet test` → Web **2014 bestanden**, 1 übersprungen (Bestand), 0 Fehler; Service **272 bestanden**, 0 Fehler.
- Neu/angepasst: `BriefkopfVorlageServiceTests` (+10 Fälle Fußbereich: mit/ohne/fremdes `footer`, Ersatz, vier kaputte
  Fälle, zwei Bereiche, aktive Inhalte/Fremdquellen, gemeinsame Summengrenze; Muster-Test: `NimbusSanNov-*`, Anschrift im
  Fuß, 2× `office@ideal-ake.at`, kein `office@ake.at`, `format('opentype')`, AKE Blau als Schrift **nur** in
  `.briefkopf .ake-firma`), `BriefkopfEinbindungTests` (+4: Fuß-Partial genau einmal hinter dem letzten Dokument in allen
  drei Drucken mit dem richtigen Zähler-Modell, Druckregeln nur im `HatFuss`-Zweig).
- Doku: README (Ablage E5, Abschnitt Fußzeile), Hilfe (Druck + Einrichtung), Changelog v1.59.0, `docs/TESTSZENARIEN.md`
  TS-92 neu (12 Fälle + P0-Schritt), TS-82-Vorbedingung/82.5/82.7/82.14 und TS-84.8 nachgezogen (LF erhalten).
- Nicht angefasst: Testprotokolle unter `tests/` (Auswirkungen siehe Abschnitt Test-Szenarien), Aufgabe
  [[2026-10-09-briefkopf-seitenzaehlung]] (hat fremde Änderungen; bleibt für den Sammeldruck mit mehreren HauptFA offen).

## QA (2026-10-14, qa-agent)

Geprüft im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Diff `ead16fc4..c2d54f35`, danach QA-Commit
`d4108f1a`. Web-App nicht gestartet (appsettings zeigen auf AKESQL20 mit Auto-Migration).

### 1. Build und Tests (selbst ausgeführt, nach dem QA-Commit wiederholt)

- `dotnet build IdealAkeWms.slnx` → **0 Fehler**, 12 Warnungen (Bestand).
- `dotnet test` → `IdealAkeWms.Tests`: **2014 bestanden, 1 übersprungen** (`ProductionOrderEagerCreateAgentJobTests`, Bestand),
  0 Fehler. `IDEALAKEWMSService.Tests`: **272 bestanden**, 0 übersprungen, 0 Fehler.

### 2. Code-Review (selbst, in diesem Lauf)

Gelesen: `BriefkopfVorlageService.cs` vollständig (Load, Sanitize, Embedder), `_PrintBriefkopfFuss.cshtml`,
`_PrintBriefkopfStyles.cshtml`, `_PrintBriefkopf.cshtml`, die drei Druck-Views vollständig, die PDF-Aktionen
(`FaHierarchyBeschichtungController.cs:120-136`, `:224`; `BeschichtungsauftraegeController.cs:258`), die Gruppenbildung
`FaHierarchyListBuilder.cs:140-165`, `Branding/briefkopf.html`, beide Testdateien, README, Hilfe, Changelog,
`docs/TESTSZENARIEN.md`, Testindex, `fallstricke.md` §27.

Ergebnisse zu den gezielten Fragen:
- **Script oder fremde Quelle über den Fußbereich?** Nein, nicht über neue Wege. `Sanitize` läuft über die ganze Datei,
  **bevor** der Fuß herausgelöst wird (`BriefkopfVorlageService.cs:121` vor `:137`). `<style>` im Fuß landet vorher in den
  globalen Styles (`:124`, mit `EmbedCssUrls` und `@import`-Entfernung). Der Fuß läuft über **dieselbe** `Embedder`-Instanz
  (`:156-159`): `src=` aller Tags und `url(` in `style`-Attributen, http/absolut/außerhalb abgelehnt. Test
  `Load_Footer_ActiveContentAndForeignSourcesRemoved` belegt das. Bekannte Lücken (`background`-/`poster`-Attribut,
  `image-set()`) gelten für Kopf und Fuß gleich. Sie stehen schon in [[2026-10-09-briefkopf-csp-und-bereinigung]] (Zeile 15);
  der Fuß bringt keinen neuen Weg dazu.
- **Fehlerfälle:** `<footer class="briefkopf-fuss">` ohne Ende → Ersatz (`:138-139`); mehrere → erster + Hinweis (`:140-141`);
  offene Rawtext-Elemente werden in Kopf **und** Fuß gesucht (`:143`); `div`-Balance getrennt (`:147-152`). Alle Fälle sind getestet.
- **Styles nur im HatFuss-Zweig?** Ja. `@page`, Polster, `position: fixed` und `:last-of-type` stehen ausschließlich in
  `@if (vorlage.HatFuss)` (`_PrintBriefkopfStyles.cshtml:20-37`). `HatFuss` ist im Ersatz-Fall false (Record `:22`).
  Ohne Fußbereich gibt der Fuß-Partial nichts aus, `.fa-group:last-child` greift also weiter wie bisher (AK 7).
- **Partial einmal, hinter den Dokumenten:** ja, in allen drei Views, außerhalb der `.fa-group`. In `Print`/`PrintSummiert`
  steht er im `else`-Zweig (der Leerfall hat keine Fußzeile).
- **`.fa-group:last-of-type`:** In allen drei Views ist die letzte `div` unter `<body>` die letzte `.fa-group`. Danach folgen
  nur `<footer>` und `<style>` aus dem Partial, das IT-Markup steckt innerhalb von `<footer>`. In `Beschichtungsauftraege/Print`
  gibt es nur eine Gruppe und keine Seitenumbruch-Regel. Die Regel aus dem Partial (Spezifität 0,2,0) steht im `<head>` nach
  dem View-Style und gewinnt. Das Risiko für spätere Änderungen ist in `fallstricke.md` §27 festgehalten („hinter die Gruppen
  nie ein `div` hängen").
- **PDFs über dieselben Views:** ja. `Pdf` → `RenderToStringAsync(…, "Print", vm)` mit genau einer Gruppe
  (`FaHierarchyBeschichtungController.cs:127-136`), `PdfSummiert` → `"PrintSummiert"` (`:224`), Auftrag-PDF → `"Print"`
  (`BeschichtungsauftraegeController.cs:258`).
- **Zähler-Modell:** `Print`: `Groups.Count == 1`. Die Gruppen sind nach HauptFA eindeutig (`FaHierarchyListBuilder.cs:141`
  `GroupBy(n => n.HauptFA)`). `PrintSummiert`: `Rows.Select(HauptFA).Distinct().Count() == 1`, derselbe Schlüssel wie die
  `GroupBy` der View. Auftrag: immer `true`. Das ist korrekt.
- **Hausregel-Defektklassen:** keine Werte in `<script>`. Kein `hidden` mit `d-*`. Kein `?? 0`. Keine parallelen Arrays,
  keine reine Client-Validierung. „Genau ein Dokument" wird in zwei Views mit verschiedenen Ausdrücken ermittelt, beide
  folgen aber dem Gruppierungsschlüssel der jeweiligen View. Kein Drift-Risiko, das über die View selbst hinausgeht.

Befunde:

| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| Q-1 | Minor | `AppVersion.Date` stand in beiden Projekten noch auf `2026-10-13`, der Changelog nennt für v1.59.0 aber den 14.10.2026. Bisher folgte `Date` immer dem Release-Tag (v1.57.0–v1.58.1). | **Behoben**, QA-Commit `d4108f1a` (beide `AppVersion.cs`). Build 0 Fehler, Tests unverändert grün. |
| Q-2 | Minor | Bei `<footer class=briefkopf-fuss>` **ohne Anführungszeichen** passt `FussMuster` (`:55`) nicht, die Prüfung `:138` aber schon. Ergebnis: Ersatz-Kopf mit dem irreführenden Hinweis „ohne End-Tag". Das passiert nicht still, sondern wird gemeldet. Das Muster der IT quotet. | Offen, nicht behoben (Aufwand/Nutzen gering). Kommt die IT darüber, ist der Hinweis zu ändern. |
| Q-3 | Minor | Ein **verschachteltes** fremdes `<footer>` im Fußbereich beendet den Treffer zu früh: Der Rest des Fußes erscheint im Kopf. E3 setzt bewusst voraus, dass `footer` nie verschachtelt wird. Das Ergebnis ist sichtbar, nicht still. | Offen, gewollt laut E3. |
| Q-4 | Minor | Die Höhe der Muster-Fußzeile beträgt rechnerisch 4 × 7 pt × 1,25 + 2 mm + 3 mm ≈ 17,35 mm. Das sind nur ≈ 0,4 mm Luft zur Kappung bei 17,75 mm. Wird sie überschritten, schneidet `overflow: hidden` **unten**, also zuerst den Balken, ohne Hinweis. Das WMS kann die gerenderte Höhe nicht messen. Die Kappung ist dokumentiert (Muster-Kommentar, README). | Offen. Prüfen in TS-92.5 (Balken vollständig, auch mit echten Nimbus-Dateien). |
| Q-5 | Minor (Nachweis) | AK 17: P0 (Edge-Version am Server) ist offen, P1 (Druckvorschau Edge/Chrome) nur indirekt über die Headless-Engine gemessen, P4 ohne „Hintergrundgrafiken" nicht gemessen. | Manual-UAT: TS-92.13, TS-92.1, TS-92.5. Zählt nicht als grün. |

Keine Critical-, keine Important-Befunde.

### 3. Beweisart je Akzeptanzkriterium

| AK | Beweisart |
|---|---|
| 1 Fußzeile jede Seite, Bildschirmdruck | Spike-Messung (P2, Headless-Engine) + **Manual-UAT** TS-92.1/92.2 (P1 nur indirekt) |
| 2 dasselbe im PDF | Spike-Messung P2 (statische Seite + echtes Muster) + **Manual-UAT** TS-92.3 |
| 3 nichts darunter, ≥ 2 mm | Spike-Messung (≥ 2,6 mm bzw. ≥ 3,7 mm) + **Manual-UAT** TS-92.4 |
| 4 Kopf nur Seite 1, HauptFA neue Seite | **Manual-UAT** TS-92.1/92.2 (Kopf-Lage unverändert, Struktur durch `BriefkopfEinbindungTests` gestützt) |
| 5 Inhalt/Anordnung/Farbe, 2× office@ideal-ake.at | Automatisiert (`MusterVorlage_…`: Inhalte, Mails, kein `office@ake.at`) + **Manual-UAT** TS-92.5 (Anordnung/Farbe) |
| 6 Balken zweifarbig 45°, ohne Hintergrundgrafiken | Spike P4 (PDF-Pixel) + **Manual-UAT** TS-92.5 (Druckdialog) |
| 7 ohne Fußbereich unverändert | Automatisiert (Service `Load_WithoutFooter_…`, Wächter: Druckregeln nur im `HatFuss`-Zweig) + **Manual-UAT** TS-92.6 (Seitenzahl vorher/nachher) |
| 8 Ersatz → keine Fußzeile | Automatisiert (`Ersatz_HasNoFuss`, `HatFuss`-Definition) + Manual-UAT TS-92.7 |
| 9 Fehler im Fuß wie im Kopf | Automatisiert (`Load_Footer_ActiveContentAndForeignSourcesRemoved`); „nur am Bildschirm/LogWarning" ist **Manual-UAT** TS-92.8 |
| 10 ohne `</footer>`/div-Unwucht/zwei Bereiche | Automatisiert (`Load_BrokenFooter_IsErsatz_WithHint` ×4, `Load_TwoFooters_FirstWins_WithHint`) |
| 11 genau einmal im Quelltext, nur `data:` | Automatisiert (Einbindung einmal hinter den Dokumenten; Einbettung `data:`) + Manual-UAT TS-92.2 (Strg+U) |
| 12 Bildschirm: einmal am Ende | **Manual-UAT** TS-92.9 (Lage hinter den Dokumenten automatisiert) |
| 13 Kommissionierlisten ohne Kopf/Fuß | Automatisiert (bestehender `NotContain("_PrintBriefkopf")`) + Manual-UAT TS-92.10 |
| 14 Muster | Automatisiert (`MusterVorlage_…`: Logo rechts, `NimbusSanNov-*`, `format('opentype')`, Fuß, keine Absenderzeile über „Schörihub nicht im Kopf") |
| 15 README/Hilfe/TS-82 Ablage E5 | Review (Diff gelesen: README :415/:451-474, Hilfe :781-789/:821-825, TS-82-Vorbedingung) + Manual-UAT TS-92.11 |
| 16 „Seite X von Y" nur bei einem Dokument | Automatisiert (Zähler-Modell je View, Zähler nur im `@if (Model)`) + Spike P3 + **Manual-UAT** TS-92.12 |
| 17 P0–P4 festgehalten, V3 in fallstricke | Review: Spec-Abschnitt Umsetzung + `fallstricke.md` §27 vorhanden; P0 Server offen → TS-92.13 (Q-5) |

### 4. Geänderte bestehende Tests

- `BriefkopfVorlageServiceTests.MusterVorlage_…` (bisher `:337-357`): **Verhalten bewusst durch die Spec geändert**.
  Anschrift aus `Markup` in `Fuss` (E2/AK 14), Schriftnamen `NimbusSanNov-*` (AK 14). Das Verbot `color: #43a6e2` wird auf
  „nur `.briefkopf .ake-firma`" eingegrenzt (Freigabe 1, AK 5). Der Selektor-Regex ist semantisch gleich, er erfasst
  zusätzlich den Regelrumpf. Der Muster-Test prüft weiterhin, dass jede Regel unter `.briefkopf` steht.
- `BriefkopfEinbindungTests`: nur neue Fälle, kein bestehender verändert.
- Kein Befund der Klasse „Sonstiges" (keine Fixture so lange verbogen, bis der Test grün ist).

### 5. CLAUDE.md-Checkliste

- Migration / `SQL/XX` / FreshInstall / AgentJobs: entfällt (kein Schema). Audit-Felder: entfällt (nur Lesen).
- Version: beide `AppVersion.cs` auf 1.59.0 (Datum durch Q-1 korrigiert), Anwender-Changelog `Help/Changelog.cshtml` ✓.
  Brain-Changelog `secondbrain/changelog/2026-10-14-v1-59-0-briefkopf-fusszeile.md` liegt im Hauptcheckout vor (uncommittet,
  das übernimmt der Aufrufer).
- `docs/TESTSZENARIEN.md`: TS-92.1–92.13 neu, TS-82-Vorbedingung/82.5/82.7/82.14 und TS-84.8 nachgezogen, Stand-Zeile
  ergänzt. **LF** erhalten (`git ls-files --eol`: `i/lf w/lf`, 0 CR-Bytes). Testindex Zeile 119 (TS-92) ✓.
- README und Hilfe ✓. `fallstricke.md` §27 ✓. Keine neue Rolle, kein neuer Filter. Keine Einmal-Skripte.

## Manuelle Test-Checkliste (Schranke 2)

1. **Vor dem Deploy:** an einem mehrseitigen HauptFA (≥ 40 Positionen) die Seitenzahl von Druck und PDF notieren (für Punkt 10).
2. Web aus dem Worktree publishen (Befehl im Abschnitt Deploy) und auf das Testsystem bringen. Danach prüfen:
   `<Programmordner>\Branding\briefkopf.html` existiert noch, `https://<host>/Branding/briefkopf.html` liefert 404 (TS-92.11).
3. Edge-Version am Webserver notieren (TS-92.13).
4. `Branding\briefkopf.html` um den Fußbereich aus dem neuen Muster ergänzen, die Schriften `NimbusSanNov-Reg.otf`/`-SemBol.otf` dazulegen.
5. TS-92.1: Liste → mehrseitiger HauptFA → Drucken (Edge **und** Chrome, Ränder „Standard"). Fußzeile auf jeder Seite,
   Kopf nur auf Seite 1, „Seite X von Y" unten rechts, keine Zeile unter der Fußzeile.
6. TS-92.4 mit Lineal am Ausdruck: ≥ 2 mm Abstand, keine halbe Zeile. Dazu ein HauptFA bzw. Auftrag mit Bemerkung/Banner.
7. TS-92.5: Farben/Anordnung. AKE-Name in AKE Blau, sonst Grau/IDEAL Blau. **Balken vollständig** (3 mm, nicht
   angeschnitten, Q-4), zusätzlich mit ausgeschalteten „Hintergrundgrafiken".
8. TS-92.2: Summiert ohne Filter (≥ 3 HauptFA): Fußzeile auf allen Seiten, **keine** Seitenzählung, keine leere letzte Seite,
   Strg+U: `briefkopf-fussbereich` genau einmal.
9. TS-92.3 + TS-92.12: PDF (Liste, Summiert, Auftrag): „Seite 1 von N" … „N von N", Nimbus im PDF eingebettet. Summiert
   gefiltert auf einen HauptFA: Zählung korrekt.
10. TS-92.6: Fußbereich aus der Vorlage löschen. Keine Fußzeile, keine Zählung, kein Hinweis, Quelltext ohne `@page`, Seitenzahl wie in Punkt 1.
11. TS-92.7: Pfad leeren → Ersatz-Kopf, keine Fußzeile. Wiederherstellen.
12. TS-92.8 (a)–(f) Fehlerfälle im Fuß, Hinweise nur am Bildschirm und im Web-Log. Wiederherstellen.
13. TS-92.9: Druckansicht am Bildschirm, Fußzeile einmal am Ende, kein schwebender Balken.
14. TS-92.10: Kommissionierliste (Liste und Summiert) ohne Kopf und Fuß, Ränder wie bisher.
15. Erst danach mergen (Schranke 2). Nach dem Merge nur bei zusammengeführten parallelen `main`-Änderungen erneut aus `main` publishen.
- Browser-Abnahme 2026-10-21: [[2026-10-21-ideal-v1-59-bis-v1-64-browser-testbericht]] - 45 OK, 2 FEHLER, 1 nicht pruefbar, 0 Protokoll passt nicht
