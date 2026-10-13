---
type: spec
title: "Beschichtungsauftrag-Ausbau Teil 2 — Briefkopf / Corporate-Design-Druck"
slug: 2026-10-06-beschichtungsauftrag-ausbau-teil-2-spec
status: Testbereit
created: 2026-10-06
updated: 2026-10-09
source_backlog: "[[2026-10-06-beschichtungsauftrag-ausbau]]"
anhaenge:
  - "[[cd-auszug-druckdokumente]]"
  - "[[IDEAL_AKE_Logo_rgb.jpg]]"
  - "[[IDEAL_AKE_Logo_rgb_klein.jpg]]"
depends_on: ""
task: "[[2026-10-09-beschichtungsauftrag-ausbau-teil-2-umsetzung]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
affected_code:
  - IdealAkeWms/Services/BriefkopfVorlageService.cs (neu, scoped — liest briefkopf.html ueber den Pfad aus AppSettings, entfernt aktive Inhalte, bettet Bilder/Schriften aus Branding\ als Data-URI ein, liefert Styles + Markup + Warnungen oder Ersatz-Kopf; Registrierung in Program.cs)
  - IdealAkeWms/Views/Shared/_PrintBriefkopf.cshtml (neu — Kopf-Partial je .fa-group - Vorlagen-Markup bzw. Ersatz-Kopf, Empfaenger, Dokumenttitel, Datum, Bildschirmhinweis; @inject des Service)
  - IdealAkeWms/Views/Shared/_PrintBriefkopfStyles.cshtml (neu — Styles der Vorlage inkl. @font-face genau EINMAL im head)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/Print.cshtml (Styles-Partial im head, Kopf-Partial direkt nach <div class="fa-group"> :54, vor Filter-Banner :55-61)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/PrintSummiert.cshtml (existiert im Buendel; Styles-Partial im head, Kopf-Partial direkt nach <div class="fa-group"> :56, vor Summen-Banner :57-59)
  - IdealAkeWms/Models/AppSettingKeys.cs (neuer Key BriefkopfVorlagePfad neben Firmenname/Firmenanschrift :74-75)
  - IdealAkeWms/Models/Standort/StandortSettingsCatalog.cs (EIN neues String-Feld "Briefkopf-Vorlage (Pfad)" in GroupFirmendaten nach :38, mit Hint; KEIN neuer Feldtyp)
  - Branding/briefkopf.html + Branding/IDEAL_AKE_Logo_rgb_klein.jpg (neu, Repo-Wurzel ausserhalb des Web-Projekts — Muster-Lieferumfang fuer die IT, nicht im Publish-Output; Schriftdateien NICHT im Repo)
  - IdealAkeWms.Tests/ (Unit-Tests BriefkopfVorlageService mit Temp-Ordner - Pfad-Ausbruch, absolute/UNC/http-Verweise, fehlende Datei, Groessengrenze, Script-/on*-Entfernung, MIME, url() in @font-face, falsche Endung der Vorlage)
  - README.md (AppSettings-Tabelle BriefkopfVorlagePfad + Abschnitt Corporate Design um "Briefkopf-Vorlage" erweitern)
  - IdealAkeWms/Views/Help/ + Views/Help/Changelog.cshtml + beide AppVersion.cs
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "(neu 2026-10-09) Groessengrenzen: briefkopf.html hoechstens 256 KB, je eingebettete Datei hoechstens 1 MB, Summe aller eingebetteten Dateien hoechstens 3 MB? Empfehlung: diese Werte als Konstanten, keine Einstellung."
  - "(neu 2026-10-09) SVG als Bild in der Vorlage zulassen? Empfehlung: nein in dieser Ausbaustufe (Logo ist JPEG), nur png/jpg/jpeg + woff/woff2/ttf/otf."
  - "(neu 2026-10-09) Caching ueber Anfragen hinweg: A) kein Cache, Datei je Druckanfrage einmal lesen (Empfehlung), B) IMemoryCache mit Invalidierung ueber LastWriteTime der Vorlage UND aller eingebetteten Dateien?"
  - "(neu 2026-10-09) Bereinigung ohne HTML-Parser (im Web-Projekt ist keiner referenziert, IdealAkeWms.csproj:10-20): A) schmale Regex-/String-Bereinigung mit ponytail-Kommentar und dokumentierter Grenze (Empfehlung - Vorlage kommt von der IT, Pfad nur durch Admin setzbar), B) neue Abhaengigkeit HtmlSanitizer/AngleSharp?"
beantwortete_rueckfragen:
  - "1 Vorlagen-Form und Logo-Quelle -> HTML-Datei briefkopf.html der IT im Unterordner Branding\\ des Installationsverzeichnisses ausserhalb des Publish-Ordners, Einstellung 'Briefkopf-Vorlage (Pfad)', Bilder als Data-URI, <script> entfernen, Ersatz-Kopf aus Firmenname + Anschrift bei fehlender/unlesbarer Datei, Muster-briefkopf.html im Lieferumfang; kein Upload, kein HTML in der DB, PDF-Vorlage verworfen (Gerald 2026-10-09, Freigabe-Block Nr. 1)"
  - "5 Anschrift -> Nachtrag 2026-10-09: kein mehrzeiliges Feld, kein neuer Feldtyp; Firmenanschrift bleibt einzeilig und dient nur dem Ersatz-Kopf; Anschrift steht in der briefkopf.html (Gerald 2026-10-09, Freigabe-Block Nr. 5 + Nachtrag)"
  - "6 CD-Schrift -> Ja, Lizenz liegt vor; Schriftdateien in Branding\\, @font-face in der briefkopf.html, beim Einlesen als Data-URI eingebettet; fehlt die Datei, Systemschrift (Segoe UI/Arial); nur Briefkopf, uebrige Druckansicht unveraendert (Gerald 2026-10-09, Freigabe-Block Nr. 6)"
  - "7 Kontaktdaten -> stehen in der briefkopf.html, keine neuen Felder (Gerald 2026-10-09, Freigabe-Block Nr. 7)"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: "Freigegeben wie Antworten 1-11: Briefkopf als HTML-Vorlage der IT (Branding\\), Ersatz-Kopf, CD-Schrift nur im Briefkopf, nur Seite 1, keine Kommissionierlisten; Groessengrenzen als Konstanten, kein SVG, kein Cache, eigene Bereinigung. Umsetzung direkt im Buendel."
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-10-09
---

> **Umsetzungsort (Entscheidung Gerald 2026-10-09):** direkt im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` auf Zweig `feature/2026-08-07-ideal-teile-1-5` (kein eigener Worktree, ein Lauf zur Zeit). TS-Nummer und Version: naechste freie zum Umsetzungszeitpunkt. Code-Belege beziehen sich auf
> den Buendel-Stand `.claude/worktrees/2026-08-07-ideal-teile-1-5` (v1.47.1).

> [!note] Rework 2026-10-09 (Antworten 1, 5 inkl. Nachtrag, 6, 7)
> Der bisherige Entwurf „fester Partial mit Einstellungswerten fuer Logo, Anschrift, Fusstext" und das
> mehrzeilige Anschriftsfeld sind **ueberholt**. Der Briefkopf kommt jetzt aus einer HTML-Datei der IT
> (`Branding\briefkopf.html`), die der Server einliest, bereinigt und mit eingebetteten Bildern/Schriften
> ausgibt. Die ersetzten Textstellen stehen woertlich im Abschnitt „Ueberholter Entwurf (Antworten
> 1/5/6/7, 2026-10-09)" vor den Offenen Rueckfragen. Zeilenbelege in diesem Lauf am Buendel-Stand
> mit Teil 1 (v1.48.0, `FaHierarchyBeschichtungController.cs:142`) nachgezogen.

Teil 2 von 4 — Uebersicht: [[2026-10-06-beschichtungsauftrag-ausbau-uebersicht]]. Uebernimmt den
Backlog-Eintrag [[2026-09-17-druck-briefkopf-logo-adresse]] fuer den Beschichtungsauftrag.
Standort: Druckansicht ist IDEAL-spezifisch; die Standorteinstellungen-Maske ist fuer beide Standorte
dieselbe (Firmendaten je Installation).

**Grundlage Corporate Design (Antwort 2):** CD-Manual Stand 01/23, Auszug [[cd-auszug-druckdokumente]]
(Farben, Typografie, Satzspiegel A4, Logozone, Adressblock) und die Logodateien
[[IDEAL_AKE_Logo_rgb.jpg]] (2379 × 945 px, 300 dpi) und [[IDEAL_AKE_Logo_rgb_klein.jpg]] (854 × 339 px,
300 dpi), alle im Ordner `backlog/anhaenge/2026-10-06-beschichtungsauftrag-ausbau/`. Das Logo ist das
gemeinsame IDEAL/AKE-Logo (Schriftzug IDEAL in IDEAL Blau, AKE in AKE Blau; im Spec-Lauf als Bild gesichtet).
Die Abschnitte „Hinweise fuer die Umsetzung" im Auszug sind Koordinations-Auslegung, nicht Manual.

![[IDEAL_AKE_Logo_rgb_klein.jpg|300]]

**Keine Pflicht-Abhaengigkeit:** Der Partial haengt sich an die bestehende `Print.cshtml` (existiert im
Buendel, nach Schranke 2 in main). Er braucht weder das summierte Ergebnis aus Teil 1 noch das
gespeicherte Objekt aus Teil 3. ~~Ist Teil 1 vorher gemergt, bindet Teil 2 zusaetzlich `PrintSummiert.cshtml`
ein; sonst holt Teil 1 das nach.~~ *(ueberholt 2026-10-09: Teil 1 liegt im Buendel —
`Views/FaHierarchyBeschichtung/PrintSummiert.cshtml` existiert, Actions `PrintSummiert`/`PdfSummiert`
`FaHierarchyBeschichtungController.cs:177`, `:198-219`. Teil 2 bindet beide Druckansichten ein.)* Teil 4 nutzt
den Partial fuer den Druck des gespeicherten Auftrags.

## Ziel / Nutzen (das Warum)

Der Beschichtungsauftrag ist das einzige Dokument, das das Haus verlaesst (er geht an den externen
Beschichter). Heute traegt er nur „Beschichtungsauftrag — HauptFA …", Barcode und Kopftabelle
(`Print.cshtml:62-113`) — keinen Absender, kein Logo, kein Empfaengerfeld. Teil 2 liefert einen
gemeinsamen Briefkopf-Baustein, den jede Druckansicht einbinden kann, und haelt Druck und PDF als
**dasselbe** Dokument (Headless Edge rendert dieselbe View, [[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]]).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Briefkopf aus der HTML-Datei der IT (`Branding\briefkopf.html`, Antwort 1): Einlesen, Bereinigen, Einbetten
  von Bildern und Schriften als Data-URI, Ersatz-Kopf bei fehlender/unlesbarer Datei (Anforderungen 1, 2, 10, 11).
- `Views/Shared/_PrintBriefkopf.cshtml` (+ `_PrintBriefkopfStyles.cshtml`): Vorlagen-Markup bzw. Ersatz-Kopf,
  dazu aus dem WMS Empfaenger, Dokumenttitel, Datum.
- Einbindung in **beide** Beschichtungsauftrag-Drucke (Einzelsicht `Print`, summierte Sicht `PrintSummiert`)
  und damit in beide PDF-Actions.
- **Eine** neue Einstellung „Briefkopf-Vorlage (Pfad)" in den Standorteinstellungen (Gruppe Firmendaten).
- Muster-`briefkopf.html` nach CD-Satzspiegel als Lieferumfang (Repo `Branding/`, Deploy-Schritt fuer die IT).
- ~~Pflegbarkeit der Werte ueber die Standorteinstellungen (Feldliste `StandortSettingsCatalog`), Anschrift als
  mehrzeiliges Feld (Antwort 5).~~ *(ueberholt mit Antwort 5 Nachtrag + Antwort 7)*

**Out-of-Scope**
- Frei editierbares HTML in der Datenbank und Upload in der Oberflaeche (Antwort 1: ausdruecklich nicht).
- PDF als Vorlage/Overlay (Antwort 1: verworfen).
- Mehrzeiliges Anschriftsfeld, neuer Feldtyp `StandortFieldType`, eigene Felder fuer Telefon/E-Mail/Web, eigener
  Fusstext-Key (Antworten 5 Nachtrag, 7) — alles steht in der `briefkopf.html`.
- Platzhalter in der Vorlage (z. B. `{{Firmenname}}`) — die Datei ist je Server eigen (Antwort 1), braucht keine.
- CD-Schrift ausserhalb des Briefkopfs (Antwort 6).
- Ein eigenes PDF-Layout oder QuestPDF — nur falls Print-CSS nachweislich scheitert
  ([[2026-09-17-druck-briefkopf-logo-adresse]], Abschnitt QuestPDF; nicht vorwegnehmen).
- ~~Empfaengeradresse aus dem Kreditoren-Stamm — Teil 4 / [[2026-10-06-kreditoren-stammdaten-spec]].~~
  *(ueberholt mit Teil-4-Antworten 4/5: Empfaenger im Auftragsdruck ist der Beschichter des Auftrags als
  Freitext; Kreditoren-Stamm spaeter.)* Der Partial nimmt den Empfaenger als Text entgegen.
- Pruefschritt vor dem Versand („wer prueft das Dokument") — gehoert zum spaeteren Freigabe-Schritt
  (Teil 3, Rueckfrage 6).
- Kommissionierlisten — **kein** Briefkopf (Antwort 4). ~~nur falls Rueckfrage 4 das bejaht~~ *(entschieden
  mit Antwort 4)*
- Umbau der uebrigen Druckansicht (Kopftabelle, Positionen) auf CD-Typografie — Briefkopf ja, Rest bleibt
  (Konsistenz vor Eigenstaendigkeit, CLAUDE.md).

## Fachliche Anforderungen

1. **Absender, Logo, Kontaktdaten** stehen in der `briefkopf.html` der IT (Antworten 1, 7) — je Server eine
   eigene Datei (IDEAL/AKE). Das WMS erzeugt diesen Teil **nicht** aus Einstellungswerten.
   `Firmenname` + `Firmenanschrift` (`AppSettingKeys.cs:74-75`, einzeilige String-Felder
   `StandortSettingsCatalog.cs:37-38`, Eingabe `<input type="text">` `StandortEinstellungen/Index.cshtml:88`)
   bleiben **unveraendert einzeilig** und werden nur im **Ersatz-Kopf** gelesen (Antwort 5 Nachtrag). Heute
   liest sie keine Druckansicht — Grep `Firmenname|Firmenanschrift` in `*.cs`/`*.cshtml` trifft nur Katalog,
   Keys, Tests und `Help/Changelog.cshtml:531-534`; Teil 2 ist der erste Leser. Ersatz-Kopf = Firmenname
   (IDEAL Blau, fett) + Firmenanschrift in **einer** Zeile, HTML-kodiert (Razor-Standard), kein Logo.
2. **Logo/Bilder und Schriften** liegen als Dateien im selben Ordner `Branding\` wie die `briefkopf.html`
   (Antworten 1, 6) und werden beim Einlesen als **Data-URI** eingebettet. Belegte Gruende, warum nur
   Einbetten traegt — fuer **beide** Ausgabewege:
   - **Bildschirmdruck:** `Print`/`PrintSummiert` liefern die View direkt an den Browser
     (`FaHierarchyBeschichtungController.cs:90-108`, `return View(vm)` :107). Der Browser loest relative
     Verweise gegen die Web-URL auf; ausgeliefert wird nur `wwwroot` (`Program.cs:561-563`,
     `UseStaticFiles`/`MapStaticAssets` ohne eigenen FileProvider). `Branding\` liegt ausserhalb — ein
     `<img src="logo.jpg">` liefe ins 404.
   - **PDF:** `Pdf`/`PdfSummiert` rendern dieselbe View per `IRazorViewRenderer.RenderToStringAsync`
     (`FaHierarchyBeschichtungController.cs:131`, `:217`) zu einem String; `PdfRenderService` schreibt ihn als
     `in.html` in ein frisches GUID-Laufverzeichnis unter `%TEMP%\IdealAkeWms-Pdf\`
     (`PdfRenderService.cs:21`, `:49-55`), und Edge oeffnet **diese Datei** (`EdgeProcessRunner.cs:37-39`).
     Relative Verweise zeigen damit in das Temp-Verzeichnis, in dem nur `in.html` liegt — nichts wird gefunden.
     *(Praezisierung zur Formulierung „HTML-String ohne Basis-URL": die Basis ist das Temp-Verzeichnis, im
     Ergebnis gleich.)* Absolute `file:`-Pfade koennte Edge dagegen laden — deshalb verbietet Anforderung 11
     sie, statt sich darauf zu verlassen.
   - `AppSettings.Value` ist `nvarchar(500)`, Pflichtfeld (`ApplicationDbContext.cs:702`) — Bilder/Schriften
     gehoeren nicht in die Datenbank; dort steht nur der Pfad.
   - **Logodatei** im Muster: `IDEAL_AKE_Logo_rgb_klein.jpg` (854 × 339 px) — bei CD-Logohoehe 17,5 mm rund
     490 dpi, genug. Die grosse Datei vervielfacht den Data-URI je HauptFA-Dokument (das Kopf-Markup steht in
     jeder `.fa-group`, `Print.cshtml:52-54`).
   - Die **CD-Schrift** Nimbus Sans Novus bindet die Vorlage per `@font-face` mit `url(<datei>)` relativ zu
     `Branding\` ein (Antwort 6). Die Schriftdateien liefert die IT aus der Lizenz; sie kommen **nicht** ins
     Repo.
3. **Empfaenger** = Dienstleister-Freitext aus `FaHierarchyOrderInfo.Dienstleister` (einzige heutige Quelle,
   nur vom Sync geschrieben, `FaHierarchySyncService.cs:282`). Fehlt er, steht im Empfaengerfeld sichtbar
   „Empfaenger fehlt — in Sage nachpflegen" statt einer Leerstelle. Bei mehrdeutigem Kopf (Kombigeraet)
   steht „mehrdeutig — siehe Kopfzeilen" und die bestehende Mehrdeutigkeits-Box bleibt.
4. **Nebenbefund 2026-09-17 („Dienstleister? · RAL 0"):** Die Liste rendert die Werte literal —
   `Index.cshtml:87` zeigt „— Dienstleister?" bei leerem Dienstleister, `:88` zeigt „RAL x" nur bei nicht
   leerem RAL. „RAL 0" heisst daher: der gespeicherte RAL-Wert ist die Zeichenkette `0`. Das spricht fuer
   eine **Datenluecke in Sage**, nicht fuer einen Anzeigefehler — **unverifiziert**, Pruefabfrage in den
   Test-Szenarien. Teil 2 aendert daran nichts, macht die Luecke im Briefkopf aber sichtbar (Anforderung 3).
5. **Ein Baustein:** Alle Druckansichten, die einen Briefkopf bekommen, binden denselben Partial ein —
   keine Kopien, kein eigenes PDF-Layout.
6. **Bildschirmdruck und PDF identisch:** Was am Bildschirm im Briefkopf steht, steht auch im PDF (Logo
   inklusive).
7. **Nie abbrechen, immer melden** (Antwort 1 + „Melden statt still behandeln", CLAUDE.md): Ist der Pfad leer,
   fehlt die Datei, ist sie unlesbar, zu gross oder hat sie die falsche Endung, druckt das Dokument mit dem
   **Ersatz-Kopf** (Anforderung 1). Fehlt nur ein Bild oder eine Schrift, druckt es mit der Vorlage ohne dieses
   Bild bzw. mit Systemschrift. Jeder solche Fall erscheint als **Bildschirmhinweis** im Kopf-Partial (Klasse,
   die per `@media print` ausgeblendet wird — dasselbe Muster wie `.screen-controls`, `Print.cshtml:35`; Edge
   rendert das PDF mit Print-Medium, der Hinweis steht also nicht im PDF) **und** als `LogWarning` mit Pfad
   und Grund. ~~der fehlende Teil wird in den `screen-controls` (nur Bildschirm, `Print.cshtml:39-42`) als
   Hinweis gemeldet~~ *(ueberholt 2026-10-09: der Hinweis steht im Kopf-Partial je Dokument, weil die
   `screen-controls` vor der Gruppenschleife liegen, `Print.cshtml:39-42` vs. `:52`)*.
8. **CD-Vorgaben** ([[cd-auszug-druckdokumente]], Antwort 2) fuer den Briefkopf:
   - Farben: Dokumenttitel/Firmenbezeichnung in IDEAL Blau `#053153` (dieselbe Farbe nutzt die Druckansicht
     schon, `Print.cshtml:18`, `:24-25`), Lauftext Grau 85 `#4a494a`, Akzente AKE Blau `#43a6e2`. Kontrast
     (WCAG AA): AKE Blau erreicht auf Weiss nur rund 2,7:1 (eigene Rechnung, im Dev-Lauf mit Werkzeug
     bestaetigen) — daher nur fuer Linien/Dekor, nicht als Textfarbe; im Logo selbst unveraendert.
   - Logo: Hoehe 17,5 mm, vertikal mittig in der Logozone (oberste 37,125 mm der Seite, dort keine anderen
     Elemente).
   - Satzspiegel A4: Raender links/rechts 17,5 mm, unten 24,75 mm als Zielwert. Per `@page` nur uebernehmen,
     wenn Bildschirmdruck und PDF dasselbe Ergebnis zeigen; sonst bleibt die heutige Randlogik
     (`Print.cshtml:17`, `:35`) und der Befund steht im QA-Nachweis.
   - Schrift: Nimbus Sans Novus **nur im Briefkopf**, aus `Branding\` eingebettet (Antwort 6). Font-Stack in der
     Vorlage `'Nimbus Sans Novus', 'Segoe UI', Arial, sans-serif` — fehlt die Datei, faellt der Briefkopf auf die
     Systemschrift der Druckansicht zurueck (`Print.cshtml:17`, `PrintSummiert.cshtml:17`:
     `font-family: 'Segoe UI', Arial, sans-serif`). Die uebrige Druckansicht bleibt bei dieser Schrift.
     ~~bis Rueckfrage 6 geklaert ist, serifenlose Systemschrift wie heute~~ *(ueberholt mit Antwort 6)*
   - Adressblock: Firmenbezeichnung in der Hausfarbe, eine Adresse zusammen mit den Kontaktdetails — **in der
     `briefkopf.html`** (Antwort 7). ~~woher die Kontaktdetails kommen, klaert Rueckfrage 7~~ *(entschieden)*
   - Diese CD-Vorgaben setzt die **Muster-`briefkopf.html`** um; was die IT spaeter in der Datei aendert, liegt
     in ihrer Verantwortung (Antwort 1). Das WMS prueft die Gestaltung nicht.
9. **Briefkopf nur auf Seite 1** jedes HauptFA-Dokuments (Antwort 3). „Seite X von Y" ist nice to have: nur
   umsetzen, wenn es am PDF ohne grossen Aufwand traegt (Zeitbox im Dev-Lauf); sonst weglassen und als
   Aufgabe in `secondbrain/aufgaben/` festhalten. Einschaetzung siehe Loesungsentwurf („Seitenzaehlung").
10. **Einstellung „Briefkopf-Vorlage (Pfad)"** (Antwort 1): neuer AppSettings-Key `BriefkopfVorlagePfad`
    (Definitionsort `AppSettingKeys.cs` neben `:74-75`, Kommentar wie `:70-73`: kein Seed, keine Migration,
    Zeile entsteht beim ersten Speichern). Katalogeintrag `new(AppSettingKeys.BriefkopfVorlagePfad,
    "Briefkopf-Vorlage (Pfad)", StandortFieldType.String, StandortBackend.App, GroupFirmendaten, Hint)` nach
    `StandortSettingsCatalog.cs:38` — Aufbau laut Record `StandortField(Key, Label, Type, Backend, Group, Hint)`
    (`:6-12`); die Gruppe `Firmendaten` (`:25`) steht in `GroupOrder` schon an erster Stelle (`:63-66`); der
    Hint wird unter dem Feld angezeigt (`StandortEinstellungen/Index.cshtml:90-92`). Hint-Text: „Vollstaendiger
    Pfad zur briefkopf.html im Ordner Branding\ des Installationsverzeichnisses. Leer oder Datei fehlt:
    Ersatz-Kopf aus Firmenname und Anschrift." Wert = **absoluter Pfad zur Datei**; der Ordner der Datei ist
    der Branding-Ordner fuer alle eingebetteten Ressourcen. Laengen: Key `nvarchar(100)`
    (`ApplicationDbContext.cs:701`), Value `nvarchar(500)` (`:702`) — reicht fuer einen Pfad; die Maske
    speichert String-Werte roh ohne Laengenpruefung (`StandortEinstellungenController.cs:75-77`), ein Pfad ueber
    500 Zeichen scheitert also wie heute jedes ueberlange String-Feld beim Speichern (kein neues Verhalten).
    Aendern duerfen nur **Admins**: `[RequireAdminAccess]` auf `StandortEinstellungenController`
    (`:20`, Filter prueft `IsAdminAsync`, `RequireAdminAccessAttribute.cs:23`). Zweiter Schreibweg mit derselben
    Rolle: die generische Maske `/Settings` schreibt jeden geposteten Key (`SettingsController.cs:10`, `:45-50`).
    Nach dem Speichern greift der neue Pfad sofort — der Writer entfernt den AppSettings-Cache-Eintrag nach dem
    Commit (`StandortSettingsWriter.cs:91-92`).
11. **Sicherheit der Vorlage und der eingebetteten Ressourcen** (Antwort 1 „`<script>` entfernen", hier zu Ende
    gedacht). Grundsatz: die Datei kommt von der IT, der Pfad nur vom Admin — aber das Ergebnis sehen
    **Nicht-Admins** (Rolle `beschichtungsauftrag`, `FaHierarchyBeschichtungController.cs:28`) und Headless Edge
    rendert es auf dem Webserver. Deshalb:
    - **Nur lesen.** Das WMS schreibt, verschiebt oder loescht nichts in `Branding\`.
    - **Vorlagen-Endung** muss `.html`/`.htm` sein — sonst Ersatz-Kopf + Warnung. *Warum:* ohne diese Pruefung
      koennte ein auf `appsettings.json`/`web.config` gesetzter Pfad Verbindungszeichenfolgen in einen Druck
      bringen, den Nicht-Admins sehen.
    - **Ressourcen nur aus dem Branding-Ordner:** jeder Verweis in `<img src>` und CSS `url(...)` (in `<style>`
      und in `style`-Attributen) wird mit `Path.GetFullPath(Path.Combine(brandingDir, ref))` aufgeloest und nur
      eingebettet, wenn das Ergebnis unterhalb von `brandingDir` liegt (Vergleich mit abschliessendem
      Trennzeichen, ordinal ignore-case). Abgelehnt — nicht eingebettet, **nicht** nachgeladen, Warnung:
      `../`-Ausbruch, absolute Pfade (`Path.IsPathRooted`, Laufwerk, UNC `\\server\…`), jedes Schema
      (`http:`, `https:`, `file:`, protokollrelativ `//`). Bereits vorhandene `data:`-URIs bleiben, sofern ihr
      MIME-Typ erlaubt ist.
    - **Erlaubte Dateitypen** (Endung → MIME): `png` → `image/png`, `jpg`/`jpeg` → `image/jpeg`, `woff` →
      `font/woff`, `woff2` → `font/woff2`, `ttf` → `font/ttf`, `otf` → `font/otf`. **SVG** nicht in dieser
      Ausbaustufe (neue Rueckfrage 9): als `<img>` laeuft SVG-Script im Browser zwar nicht, inline oder ueber
      `<object>`/`<embed>` aber schon, und das Logo ist ohnehin JPEG.
    - **Aktive Inhalte entfernen:** Elemente `script` (samt Inhalt), `iframe`, `object`, `embed`, `link`,
      `base`, `meta`; alle Attribute `on*`; `href`/`src`-Werte mit `javascript:`/`vbscript:`; `srcset`; CSS
      `@import`. *Warum ueber Antwort 1 hinaus:* `on*`-Attribute und `javascript:`-URLs fuehren Script genauso
      aus wie `<script>`; `link`/`@import`/`srcset` laden Fremdressourcen am Einbettungs-Pfad vorbei; `base`
      verbiegt alle relativen Verweise der Druckansicht; `meta` kann per `http-equiv=refresh` weiterleiten.
      Die eigenen Bedienknoepfe der Druckansicht (`onclick` in `Print.cshtml:40-41`) sind **nicht** betroffen —
      bereinigt wird nur der Inhalt der Vorlage.
    - **Groessengrenzen** (neue Rueckfrage 8, Vorschlag): Vorlage ≤ 256 KB, je eingebettete Datei ≤ 1 MB, Summe
      ≤ 3 MB. Vorlage zu gross → Ersatz-Kopf; Ressource zu gross → nicht eingebettet. Jeweils Warnung.
    - **Umsetzungsweg** der Bereinigung ohne HTML-Parser im Projekt (`IdealAkeWms.csproj:10-20`): neue
      Rueckfrage 11.

## Technischer Loesungsentwurf

**Ablauf (ein Baustein, zwei Ausgabewege):**

1. `BriefkopfVorlageService` (neu, **scoped**, in `Program.cs` registriert) liest `BriefkopfVorlagePfad` ueber
   `IAppSettingRepository` (gecacht per Decorator, ADR 0001) und baut **einmal je Anfrage** (Ergebnis im Feld
   des scoped Service gemerkt) ein Ergebnis `{ Styles, Markup, Hinweise, IstErsatz }`:
   - Pfad leer / Datei fehlt / unlesbar / falsche Endung / zu gross → `IstErsatz = true`, Hinweis + `LogWarning`.
   - Sonst: UTF-8 lesen → aktive Inhalte entfernen (Anforderung 11) → alle `<style>`-Bloecke herausloesen
     (→ `Styles`) → Markup = Inhalt von `<body>` (ohne `<body>` die Datei ohne `<head>`/`<style>`) →
     `<img src>` und `url(...)` gegen den Branding-Ordner aufloesen, pruefen, als
     `data:<mime>;base64,…` einsetzen. Abgelehnte/fehlende Bilder: `<img>`-Element entfernen (kein
     Broken-Image-Symbol). Abgelehnte/fehlende Schrift: die `url(...)`-Quelle entfaellt, die `@font-face`-Regel
     laedt nichts, der Font-Stack faellt auf Segoe UI/Arial zurueck. Je Fall Hinweis + `LogWarning`.
   - Ausnahmen beim Lesen werden gefangen und zu Ersatz-Kopf + Warnung — **nie** eine Ausnahme in die View
     (Antwort 1: Druck bricht nicht ab; ein Fehler hier wuerde sonst auch die PDF-Action ueber
     `RenderToStringAsync` abbrechen, `FaHierarchyBeschichtungController.cs:131`, die nur
     `PdfRenderException` faengt, `:135`).
2. `_PrintBriefkopfStyles.cshtml` (im `<head>` von `Print.cshtml` und `PrintSummiert.cshtml`, nach dem
   bestehenden `<style>` `:15-36` bzw. `:15-38`) gibt `Styles` **einmal je Seite** aus. *Warum getrennt:* die
   `@font-face`-Data-URIs (Schriftdatei, base64) stuenden sonst in jeder `.fa-group` — der Bildschirmdruck
   rendert die **ganze** gefilterte Menge ohne Paging (`FaHierarchyBeschichtungController.cs:93-94`).
3. `_PrintBriefkopf.cshtml` steht **direkt nach** `<div class="fa-group">` (`Print.cshtml:54`,
   `PrintSummiert.cshtml:56`), also **vor** Filter- und Summen-Banner (`Print.cshtml:55-61`,
   `PrintSummiert.cshtml:57-66`) — die CD-Logozone ist eine Sperrzone fuer andere Elemente
   ([[cd-auszug-druckdokumente]]). Inhalt: `<div class="briefkopf">@Html.Raw(Markup)</div>` bzw. Ersatz-Kopf,
   darunter Empfaenger, Dokumenttitel, Datum (kleines Model wie bisher: Titel, Empfaengertext, Mehrdeutig-Flag,
   Datum), darunter die Hinweise (nur Bildschirm). Der Service kommt per `@inject` — Muster `_Layout.cshtml:1-3`.
   Das Logo-`<img>` steht damit je Dokument einmal im HTML (bekannte Grenze, siehe unten).
4. **Bildschirmdruck und PDF identisch:** beide Wege rendern dieselbe View. `RazorViewRenderer` uebergibt den
   echten `ControllerContext` an den `ViewContext` (`RazorViewRenderer.cs:28-38`), `@inject` loest also aus
   denselben Request-Services auf. Weil alle Ressourcen eingebettet sind, braucht Edge weder Netz noch
   Dateizugriff ausserhalb von `in.html` (Anforderung 2). Im Dev-Lauf am erzeugten PDF nachweisen, nicht
   annehmen.

**CSS-Kapselung:** Die Styles der Vorlage stehen im `<head>` der ganzen Druckseite. Damit Nimbus und
Briefkopf-Regeln nicht auf Kopftabelle und Positionen wirken (Antwort 6), skopiert die Muster-Vorlage **jede**
Regel unter `.briefkopf` (der Wrapper aus Schritt 3); README und ein Kommentar in der Muster-Datei nennen diese
Regel fuer die IT. Verworfen: Declarative Shadow DOM — `@font-face` in Shadow-Root-Styles wird von Chromium
nach allgemeiner Kenntnis ignoriert (hier **nicht** gemessen), die Schrift kaeme also nicht an.

**Seitenzaehlung „Seite X von Y" (nice to have, nicht eingeplant) — Einschaetzung:**
- Edge-Kopf-/Fusszeilen sind bewusst abgeschaltet (`--no-pdf-header-footer`, `EdgeProcessRunner.cs:38`). Sie
  waeren auch keine Loesung: Inhalt (Datum, Titel, URL) ist ueber die Kommandozeile nicht steuerbar;
  Vorlagen mit Seitenzahl gibt es nur ueber das DevTools-Protokoll (`Page.printToPDF`
  `headerTemplate`/`footerTemplate`) — Umbau des Runners, nicht verhaeltnismaessig.
- CSS-Randfelder `@page { @bottom-right { content: "Seite " counter(page) " von " counter(pages); } }` sind
  billig. Im **PDF** (genau eine HauptFA-Gruppe je Datei, `FaHierarchyBeschichtungController.cs:124`) waere die
  Zaehlung je Dokument korrekt; im **Bildschirmdruck** mehrerer Gruppen laeuft der Zaehler ueber den
  Gesamtdruck. Ob die eingesetzte Edge-Version Randfelder unterstuetzt, ist hier **nicht** geprueft.
- Folge: im Dev-Lauf hoechstens als Zeitbox am PDF probieren; ist der Bildschirmdruck nicht je Dokument
  korrekt, weglassen und als Aufgabe festhalten (AK 14).

**Caching:** je Anfrage einmal lesen ist durch den scoped Service gesetzt. Ueber Anfragen hinweg entscheidet
neue Rueckfrage 10 (Empfehlung A: kein Cache — ein paar hundert KB lesen je Druck ist unerheblich, und
Aenderungen der IT wirken ohne Invalidierungslogik sofort; Hauspraxis waere sonst `IMemoryCache`,
`Program.cs:138`, mit Schluessel Pfad + `LastWriteTimeUtc` der Vorlage **und** aller eingebetteten Dateien —
nur die Vorlage zu pruefen verpasst ein getauschtes Logo).

**Bekannte Grenze (ponytail-Kommentar im Code):** Das Logo steht als Data-URI je `.fa-group` im HTML; bei
vielen HauptFA im Bildschirmdruck waechst die Seite linear (Logo klein ~854 × 339 px). Upgrade-Pfad, falls es
stoert: Logo als CSS-Klasse mit `background-image` einmal im `<head>` (dann Druck-Hintergrundgrafik pruefen).

- Standorteinstellungen: **ein** neues String-Feld (Anforderung 10); Allow-List-Prinzip bleibt
  (`StandortSettingsCatalog.cs:16-21`, `StandortEinstellungenController.cs:50-58`). Kein neuer Feldtyp, keine
  Aenderung an `Index.cshtml`.
- Zugriff unveraendert: Standorteinstellungen `[RequireAdminAccess]` (`StandortEinstellungenController.cs:20`);
  Druck unter den bestehenden Filtern des Beschichtungsauftrags (`FaHierarchyBeschichtungController.cs:28-29`).
- Optionaler Guertel, **nicht** eingeplant: eine CSP-`<meta>` mit `img-src data:; font-src data:` in den
  Druckansichten wuerde Fremdladen auch browserseitig sperren. Erst wenn die Bereinigung sich als luecken-
  haft erweist.

**Muster-`briefkopf.html` (Lieferumfang, Antwort 1) — Ablageort-Vorschlag `Branding/` in der Repo-Wurzel:**
- Inhalt: nach CD-Satzspiegel ([[cd-auszug-druckdokumente]]) — Logozone 37,125 mm hoch, Logo 17,5 mm hoch
  vertikal mittig, linker Rand 17,5 mm; Adressblock IDEAL („IDEAL Kaeltetechnik GmbH · In der Schörihub 28,
  4810 Gmunden · Österreich · +43 7612 660 61 · office@ideal-ake.at · www.ideal-ake.at" laut Manual-Beispiel),
  Firmenbezeichnung `#053153`, Lauftext `#4a494a`, AKE Blau `#43a6e2` nur fuer Linien; `@font-face` fuer Nimbus
  Sans Novus Regular und Semi Bold mit **Platzhalter-Dateinamen** (die IT passt sie an ihre Lizenzdateien an);
  alle Regeln unter `.briefkopf`; Kommentar oben mit den Regeln fuer die IT (erlaubte Dateitypen, nur relative
  Verweise im selben Ordner, kein Script, Groessengrenzen). Daneben `IDEAL_AKE_Logo_rgb_klein.jpg` (Kopie des
  Anhangs). Eine AKE-Variante ist nicht Teil des Lieferumfangs (der Beschichtungsauftrag ist IDEAL-Druck).
- *Warum Repo-Wurzel `Branding/`:* (a) nicht unter `IdealAkeWms/` — was dort liegt, kann ueber die
  SDK-Content-Regeln in den Publish-Output geraten (Beispiel fuer gewollte Kopie: `IdealAkeWms.csproj:34-37`
  kopiert ein SQL-Skript per `CopyToPublishDirectory`), Antwort 1 verlangt aber ausserhalb; (b) der Ordnername
  gleicht dem Zielordner, die IT kopiert ihn 1:1; (c) Hauspraxis fuer Dateien, die die IT/der DBA von Hand
  ausrollt, ist ein eigener Ordner in der Wurzel (`SQL/`). Einen `deploy/`-Ordner gibt es im Repo nicht; `publish/`
  ist ignoriert (`.gitignore:4`).
- Schriftdateien kommen **nicht** ins Repo (Lizenz deckt die Ablage auf dem Server, Antwort 6 — die Weitergabe
  ueber Git ist damit nicht gesagt).

## Migrations-/SQL-Auswirkungen

**Keine.** Antwort 1 schliesst Upload und HTML in der Datenbank aus; der einzige neue Wert ist der Pfad als
AppSettings-Zeile, die beim ersten Speichern der Maske entsteht (`AppSettingKeys.cs:70-73`, kein Seed;
Schreibweg `StandortSettingsWriter.cs:54-61` legt fehlende Zeilen an). Kein Schema, kein `SQL/XX_*.sql`, kein
`00_FreshInstall.sql`-Nachtrag, keine `SQL/AgentJobs/`. `deploy.migration: false`.
~~Falls Antwort 1 einen Logo-Upload in die Datenbank waehlt, braucht es eine neue Speicherung … Migration
dreifach nach ADR 0004 …~~ *(ueberholt mit Antwort 1: kein Upload)*

## Audit-Feld-Auswirkungen

Keine neue Entitaet. `AppSetting` erbt nicht von `AuditableEntity` (`AppSetting.cs:5`, `public class
AppSetting` ohne Basisklasse); die Standorteinstellungen schreiben ueber den bestehenden
`StandortSettingsWriter` (unveraendert). Die Vorlagen-Datei wird nur gelesen — keine Schreibspur im WMS.
~~Bei einer DB-Logo-Variante …~~ *(ueberholt mit Antwort 1)*

## Akzeptanzkriterien

*Neu gefasst 2026-10-09 (Antworten 1/5/6/7); die ersetzten Fassungen von AK 1, 3, 6, 9, 12 stehen im Abschnitt
„Ueberholter Entwurf".*

1. Mit gueltiger Vorlage zeigen `Print` und `PrintSummiert` je HauptFA-Dokument oben den Briefkopf aus der
   `briefkopf.html` (Logo, Absender, Kontaktdaten) und darunter Empfaenger, Dokumenttitel und Datum.
2. Die PDFs aus `Pdf` und `PdfSummiert` enthalten denselben Briefkopf inkl. Logo (kein gebrochenes Bild) und —
   bei vorhandener Schriftdatei — die Schrift Nimbus Sans Novus (PDF-Eigenschaften → Schriftarten).
3. Eine Aenderung an `briefkopf.html` oder am Logo in `Branding\` wirkt ohne Deploy und ohne App-Neustart im
   naechsten Druck; ein geaenderter Pfad in den Standorteinstellungen ebenso.
4. Fehlt der Dienstleister in Sage, zeigt das Empfaengerfeld sichtbar „Empfaenger fehlt — in Sage
   nachpflegen".
5. Bei Kombigeraet zeigt das Empfaengerfeld „mehrdeutig — siehe Kopfzeilen"; die Mehrdeutigkeits-Box
   bleibt.
6. Ist der Pfad leer, fehlt die Datei, ist sie unlesbar, groesser als die Grenze oder nicht `.html`/`.htm`,
   druckt der Beschichtungsauftrag (Bildschirm und PDF) mit dem Ersatz-Kopf aus Firmenname + Firmenanschrift in
   einer Zeile; am Bildschirm steht ein Hinweis mit dem Grund (nicht im Ausdruck, nicht im PDF), im Log eine
   Warnung mit Pfad und Grund. Kein Fehlerbild, kein Abbruch, keine `WarningMessage`-Umleitung der PDF-Action.
7. Der Briefkopf hat **einen** Baustein (Service + Kopf-Partial + Styles-Partial); keine Druckansicht enthaelt
   eine Kopie seines Markups.
8. Der Briefkopf steht nur auf der ersten Seite jedes HauptFA-Dokuments, nachgewiesen an einem mehrseitigen
   PDF (nicht nur am Bildschirm).
9. Im gerenderten Druck-HTML (Quelltext der Druckseite und `in.html`-Inhalt) steht kein externer Verweis aus
   dem Briefkopf: kein `<link>`, kein `src=`/`url(` ausser `data:`.
10. Druck-Inhalt unterhalb des Briefkopfs (Kopftabelle, Positionen, Barcode, Filter- und Summen-Banner)
    unveraendert, Schrift dort weiter Segoe UI/Arial (Regression TS-60/TS-74; Nimbus nur im Briefkopf).
11. Die Muster-`briefkopf.html` folgt [[cd-auszug-druckdokumente]]: Logo-Hoehe 17,5 mm in der Logozone,
    Firmenbezeichnung in `#053153`, Lauftext in `#4a494a`; AKE Blau nicht als Textfarbe; alle CSS-Regeln unter
    `.briefkopf`.
12. `Firmenanschrift` bleibt in den Standorteinstellungen ein einzeiliges Textfeld; es gibt keinen neuen
    Feldtyp (Regression gegen den ueberholten Entwurf).
13. Die Kommissionierlisten-Drucke enthalten keinen Briefkopf (Regression).
14. „Seite X von Y" ist entweder je Dokument korrekt (PDF **und** Bildschirmdruck) oder gar nicht vorhanden; im
    zweiten Fall liegt eine Aufgabe in `secondbrain/aufgaben/`.
15. Die Standorteinstellungen zeigen in „Firmendaten" das Feld „Briefkopf-Vorlage (Pfad)" mit Hinweistext;
    speichern koennen es nur Admins (Nicht-Admin → AccessDenied).
16. Fehlt ein eingebettetes **Bild**, druckt der Briefkopf ohne dieses Bild und ohne Broken-Image-Symbol;
    Hinweis + Log-Warnung nennen die Datei.
17. Fehlt eine **Schriftdatei**, druckt der Briefkopf in Segoe UI/Arial; Hinweis + Log-Warnung nennen die Datei.
18. Ein `<script>`-Element, ein `on*`-Attribut oder eine `javascript:`-URL in der Vorlage erscheint nicht im
    gerenderten HTML; die eigenen Druck-/Schliessen-Knoepfe der Druckansicht funktionieren weiter.
19. Ein Verweis mit `../`-Ausbruch, absolutem Pfad, UNC-Pfad oder `http(s):`-URL wird weder eingebettet noch
    nachgeladen (kein Zugriff im Webserver-/Proxy-Log); Hinweis + Log-Warnung.
20. Eine Ressource ueber der Einzelgrenze bzw. ueber der Summengrenze wird nicht eingebettet; Hinweis +
    Log-Warnung. Die Grenzwerte entsprechen Rueckfrage 8.
21. Zeigt der Pfad auf eine Nicht-HTML-Datei (z. B. `appsettings.json`), erscheint **kein** Inhalt dieser Datei
    im Druck — Ersatz-Kopf wie AK 6.
22. Unit-Tests decken die Bereinigung und Einbettung ab (Pfad-Ausbruch, absolut/UNC/http, fehlende Datei,
    Groessengrenze, Script/`on*`/`javascript:`, MIME-Zuordnung, `url()` in `@font-face`, falsche Endung);
    `dotnet test` gruen.

## Test-Szenarien

Kapitel TS-60/TS-74 erweitern oder neues Kapitel (naechste freie Nummer):
- **Pruefschritt Nebenbefund (Sage, vor Abnahme):** fuer den HauptFA mit „Dienstleister? · RAL 0"
  ```sql
  SELECT [HauptFA], [Abteilung], [Dienstleister], [RAL], [Start_Beschichtung], [Beschichten_Retour]
  FROM [vw_IDEAL-AKE_Kommissionierung_FAInfos] WHERE [HauptFA] = <HauptFA>;
  ```
  und im WMS `SELECT HauptFA, MontageAbteilung, Dienstleister, RAL FROM dbo.FaHierarchyOrderInfos WHERE HauptFA = <HauptFA>;`
  Gleich ⇒ Datenluecke in Sage; verschieden ⇒ Sync-Befund, melden.
*Neu gefasst 2026-10-09; ersetzte Punkte siehe „Ueberholter Entwurf".*

**Vorbedingung (Testsystem IDEAL):** Ordner `Branding\` im Installationsverzeichnis neben (nicht in) dem
Web-Publish-Ordner, darin Muster-`briefkopf.html`, `IDEAL_AKE_Logo_rgb_klein.jpg` und die Nimbus-Schriftdateien;
App-Pool-Identitaet hat Leserecht. In den Standorteinstellungen „Briefkopf-Vorlage (Pfad)" = voller Pfad zur
Datei; Firmenname/Firmenanschrift gepflegt (Anschrift laut CD „In der Schörihub 28, 4810 Gmunden", Antwort 5).
`FaHierarchyBeschichtungAktiv = true`, Tester mit Rolle `beschichtungsauftrag`, Edge auf dem Server.

- **Datei vorhanden (Happy Path):** Druck Einzelsicht + Summiert und je ein PDF → Briefkopf mit Logo,
  Absender, Kontaktzeile in Nimbus; darunter Empfaenger/Titel/Datum; Kopftabelle/Positionen in Segoe UI. PDF:
  Schriftarten-Dialog zeigt Nimbus Sans Novus. Quelltext der Druckseite: kein `<link>`, nur `data:`-Quellen.
- **Aenderung ohne Deploy:** Telefonnummer in `briefkopf.html` aendern, Logo-Datei tauschen → naechster Druck
  zeigt beides.
- **Datei fehlt:** Datei umbenennen → Druck und PDF mit Ersatz-Kopf „Firmenname · Anschrift" (einzeilig), am
  Bildschirm Hinweis „Briefkopf-Vorlage nicht gefunden: <Pfad>", Hinweis nicht im Ausdruck/PDF, Log-Warnung.
  Variante: Pfad-Feld leer → gleiches Bild mit Grund „nicht konfiguriert".
- **Bild fehlt:** Logo-Datei umbenennen → Briefkopf ohne Logo, kein Broken-Image-Symbol, Hinweis + Log nennen
  die Datei.
- **Schrift fehlt:** Schriftdateien umbenennen → Briefkopf in Segoe UI/Arial, Hinweis + Log nennen die Datei.
- **Script-Tag in Datei:** `<script>alert(1)</script>` und `<img src="logo.jpg" onerror="alert(2)">` sowie
  `<a href="javascript:alert(3)">x</a>` in die Vorlage → kein Dialog, weder am Bildschirm noch beim PDF; im
  Quelltext fehlen `script`, `onerror`, `javascript:`; Drucken-/Schliessen-Knopf funktionieren.
- **Pfad-Ausbruch:** `<img src="..\..\web\appsettings.json">` bzw. `src="../x.png"` → nicht eingebettet,
  Hinweis + Log. Ebenso `src="C:\Windows\…png"`, `src="\\server\share\x.png"`,
  `src="https://www.ideal-ake.at/logo.png"` → nicht eingebettet, kein Abruf (Proxy-/Firewall-Log bzw.
  Browser-Netzwerk-Tab leer).
- **Datei zu gross:** Vorlage ueber 256 KB (z. B. mit Kommentar aufblaehen) → Ersatz-Kopf + Hinweis; Logo
  ueber 1 MB (grosse Logodatei hochskaliert) → ohne Logo + Hinweis. *(Grenzwerte laut Rueckfrage 8.)*
- **Falsche Endung:** Pfad auf `appsettings.json` setzen → Ersatz-Kopf, kein Inhalt der JSON-Datei im Druck.
- **Rechte:** Nicht-Admin ruft `/StandortEinstellungen` auf → AccessDenied.
- Negativ: Dienstleister leer → „Empfaenger fehlt".
- Kombigeraet → „mehrdeutig".
- Mehrseitiges Dokument (viele Positionen) → Briefkopf nur auf Seite 1, am PDF geprueft; Seitenzaehlung je
  Dokument korrekt oder nicht vorhanden.
- CD-Abgleich am Ausdruck: Logo-Hoehe 17,5 mm (Lineal), Logozone frei von anderen Elementen, Farben laut Auszug.
- Regression: `Firmenanschrift` in den Standorteinstellungen weiter einzeiliges Feld.
- Regression: Kommissionierliste drucken → kein Briefkopf.
- Regression: Inhalt unter dem Briefkopf unveraendert (Einzelsicht und Summiert).

## Etappen (nur bei epic: true)

Nicht zutreffend (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| 1 | — | — | — |

## Deploy

> [!info] Finalisiert vom qa-agent 2026-10-09 gegen den echten Diff `cc86d373..742f7855` (Nachlauf: bis `020bf8af` unveraendert)
> Geaendert nur `IdealAkeWms/` (Service, Partials, Views, Modell, Key, Katalog, `Program.cs`, Hilfe, Changelog,
> `AppVersion.cs`), `IDEALAKEWMSService/AppVersion.cs` (nur Versionsnummer), `Branding/` (Repo-Wurzel, nicht im
> Publish-Output — kein csproj verweist darauf), Tests, `README.md`, `docs/`. Keine Datei unter `*/Migrations/`,
> kein `SQL/`. ⇒ **deploy.web: true, deploy.service: false, deploy.migration: false** (Frontmatter stimmt).
> Der Service-`AppVersion.cs`-Bump allein verlangt keinen Service-Deploy (kein Verhaltensunterschied); wer die
> Versionsanzeige gleich halten will, publiziert ihn beim naechsten regulaeren Service-Deploy mit.
>
> **Ablauf Mensch:** Publish aus dem Worktree → Testsystem IDEAL → TS-82 → Merge. Reihenfolge am Testsystem:
> 1. Web publizieren: `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
>    (im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`), auf den IIS-Pfad kopieren.
> 2. IIS-Manager → Site → Erweiterte Einstellungen → Physischer Pfad notieren; im **Elternordner** `Branding\` anlegen.
> 3. `Branding/briefkopf.html` + `Branding/IDEAL_AKE_Logo_rgb_klein.jpg` aus dem Repo dorthin kopieren.
> 4. Nimbus-Sans-Novus-Lizenzdateien (woff2/woff/ttf/otf) dazulegen, Dateinamen in den zwei `@font-face`-Zeilen der
>    `briefkopf.html` anpassen.
> 5. Leserecht der App-Pool-Identitaet auf `Branding\` pruefen.
> 6. Standorteinstellungen → Firmendaten → „Briefkopf-Vorlage (Pfad)" = voller Pfad zur `briefkopf.html`; Firmenname /
>    Anschrift fuer den Ersatz-Kopf pflegen („In der Schörihub 28, 4810 Gmunden").
> 7. Kein DB-Schritt, kein Einmal-Skript, kein Service-Neustart.
>
> Nach dem Merge nur dann erneut aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen
> `main`-Aenderungen zusammengefuehrt hat.

*Vorlaeufig (Spec-Agent) — oben vom QA-Lauf bestaetigt.*

- **Web-App:** ja (Service, Partials, Views, Katalog, Key).
- **Service:** nein.
- **Migration:** nein (Antwort 1: kein Upload, keine DB-Speicherung). ~~ja, falls Antwort 1 einen DB-Upload
  waehlt~~ *(ueberholt)*
- **Betrieb / IT (je Server, IDEAL und AKE getrennt — Antwort 1):**
  1. Im Installationsverzeichnis **neben** dem Web-Publish-Ordner einen Ordner `Branding\` anlegen (nicht darin:
     ein spiegelnder Kopiervorgang beim Deploy wuerde ihn sonst loeschen). Den tatsaechlichen Ordneraufbau des
     Servers kennt die Spec nicht — **Pruefschritt:** IIS-Manager → Site „IDEAL AKE WMS" → Erweiterte
     Einstellungen → Physischer Pfad notieren; `Branding\` gehoert in dessen **Elternordner** (oder einen
     anderen festen Ort ausserhalb).
  2. Inhalt von `Branding/` aus dem Repo (Muster-`briefkopf.html` + Logo) dorthin kopieren; die lizenzierten
     Nimbus-Schriftdateien dazulegen und die Dateinamen in `@font-face` der Vorlage anpassen.
  3. Leserecht fuer die App-Pool-Identitaet auf `Branding\` (Edge laeuft unter derselben Identitaet, braucht
     den Ordner aber nicht — alles ist eingebettet).
  4. Standorteinstellungen → Firmendaten → „Briefkopf-Vorlage (Pfad)" = voller Pfad zur `briefkopf.html`.
  5. Firmenname/Firmenanschrift fuer den Ersatz-Kopf pflegen, Anschrift IDEAL auf „In der Schörihub 28, 4810
     Gmunden" korrigieren (Antwort 5; heute Tippfehler „38, 4840" laut [[cd-auszug-druckdokumente]]) — macht
     Gerald in den Einstellungen.
  6. Folge-Deploys ueberschreiben `Branding\` nicht; Aenderungen an der Vorlage macht die IT direkt dort.
- ~~Logo-Datei (`IDEAL_AKE_Logo_rgb_klein.jpg` aus dem Anhang, Weg laut Antwort 1) bereitstellen — bei
  Pfad-Variante lesbar fuer die App-Pool-Identitaet~~ *(ueberholt durch Schritte 1-4)*
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Ueberholter Entwurf (Antworten 1/5/6/7, 2026-10-09)

*Protokoll — nicht umsetzen. Woertlich aus der Fassung vom 2026-10-07, ersetzt durch die Abschnitte oben.*

> [!quote]- Fachliche Anforderung 1 (alt)
> **Absender** = `Firmenname` + `Firmenanschrift` aus AppSettings (`AppSettingKeys.cs:74-75`). Beide werden
> heute **geschrieben** (Standorteinstellungen, `StandortSettingsCatalog.cs:37-38`), aber von **keiner**
> Druckansicht gelesen — Grep `Firmenname|Firmenanschrift` in `*.cs`/`*.cshtml` trifft nur Katalog,
> Keys und Tests. Teil 2 ist der erste Leser. `Firmenanschrift` wird **mehrzeilig** gepflegt (Antwort 5:
> neuer Feldtyp im Katalog, Key bleibt); Zeilenumbrueche erscheinen im Druck als Zeilenumbrueche
> (HTML-kodiert, kein Roh-HTML aus der Einstellung).

> [!quote]- Fachliche Anforderung 2 (alt)
> **Logo** laut Antwort 1. Belegte Randbedingungen:
> - `AppSettings.Value` ist `nvarchar(500)`, Pflichtfeld (`ApplicationDbContext.cs:702`, `AppSetting.cs:13`) —
>   ein Logo als Data-URI passt dort **nicht** hinein.
> - Das vorhandene `wwwroot/images/ideal-ake-logo.svg` (`_Layout.cshtml:12`, `:21`; `Login.cshtml:54`) ist
>   das Gruppenlogo, nicht standortbezogen, und liegt im Publish-Output (wird bei jedem Publish
>   ueberschrieben).
> - Im Druck/PDF muss das Logo als **Data-URI** eingebettet sein; der Renderer laedt keine externen Dateien
>   (Kommentar `Print.cshtml:13-14`, Pflicht 1 im Briefkopf-Backlog).
> - **Logodatei** ist das CD-Logo aus dem Anhang (JPEG, MIME `image/jpeg`). Empfehlung fuer den Druck:
>   `IDEAL_AKE_Logo_rgb_klein.jpg` — bei CD-Logohoehe 17,5 mm ergeben 339 px rund 490 dpi, also genug; die
>   grosse Datei wuerde den Data-URI je HauptFA-Dokument vervielfachen (der Partial steht in jeder
>   `.fa-group`, `Print.cshtml:52-54`). Wie die Datei zur Anwendung kommt (Pfad-Einstellung oder Upload),
>   entscheidet Antwort 1 (offen).

> [!quote]- Technischer Loesungsentwurf (alt)
> - `_PrintBriefkopf.cshtml` als Partial mit kleinem Model (Dokumenttitel, Empfaengertext, Mehrdeutig-Flag,
>   Datum). Absender und Fusstext liest der Partial selbst per `@inject IAppSettingRepository` — dasselbe
>   Muster wie `_Layout.cshtml:2` und `:59`. Kein neuer Service fuer Textwerte.
> - Logo (falls Antwort 1 = Datei/Pfad): ein kleiner statischer Helfer liest die Datei, bestimmt den MIME-Typ
>   aus der Endung (`svg`, `png`, `jpg`) und liefert `data:<mime>;base64,…` oder `null` bei Fehler. Kein
>   Caching auf Vorrat; nur falls Messung es verlangt.
> - **PDF-Pfad pruefen:** Die `Pdf`-Action rendert ueber `IRazorViewRenderer.RenderToStringAsync`
>   (`FaHierarchyBeschichtungController.cs:127`). `@inject` im Partial muss dort ebenso aufloesen — im Dev-Lauf
>   am erzeugten PDF nachweisen, nicht annehmen.
> - CSS des Briefkopfs im Partial eingebettet (`<style>` im Partial oder im `<head>` der einbindenden View),
>   kein `<link>`.
> - Wiederholung ueber Seiten (Antwort 3): „nur erste Seite je Dokument" ist mit der heutigen Struktur
>   geschenkt (jede `.fa-group` ist ein Dokument mit `page-break-after`, `Print.cshtml:22-23`) — der Partial
>   steht einmal am Anfang jeder `.fa-group`. ~~„Jede Seite"~~ *(verworfen mit Antwort 3)*. „Seite X von Y"
>   (nice to have) frueh am PDF pruefen — Seitenzaehler je `.fa-group` (nicht je Gesamtdruck) ist mit Print-CSS
>   nicht trivial; traegt es nicht in der Zeitbox, weglassen und als Aufgabe festhalten — kein QuestPDF-Umbau
>   dafuer.
> - Standorteinstellungen: neue Felder in `GroupFirmendaten`; Allow-List-Prinzip des Katalogs bleibt
>   (`StandortSettingsCatalog.cs:16-21`). Antwort 5: neuer mehrzeiliger Feldtyp im Enum `StandortFieldType`
>   (`StandortSettingsCatalog.cs:3`) + textarea-Zweig in `Index.cshtml` neben `:88`; `Firmenanschrift` bekommt
>   diesen Typ. Im Druck Zeilenumbrueche HTML-kodiert in `<br>` umsetzen.
> - Zugriff unveraendert: Standorteinstellungen `[RequireAdminAccess]` (`StandortEinstellungenController.cs:20`);
>   Druck unter den bestehenden Filtern des Beschichtungsauftrags.

> [!quote]- Akzeptanzkriterien (alt, nur die ersetzten)
> 1. Der Beschichtungsauftrag-Druck zeigt je HauptFA-Dokument einen Briefkopf mit Logo, Firmenname,
>    Anschrift, Empfaenger, Dokumenttitel und Datum.
> 3. Firmenname/Anschrift/Fusstext aendern sich nach Speichern in den Standorteinstellungen ohne Deploy im
>    naechsten Druck.
> 6. Ohne konfiguriertes oder lesbares Logo wird trotzdem gedruckt; am Bildschirm erscheint ein Hinweis
>    (nicht im Ausdruck), im Log eine Warnung.
> 7. Der Briefkopf existiert als **ein** Partial; keine Druckansicht enthaelt eine Kopie seines Markups.
> 9. Kein externer Verweis im gerenderten Druck-HTML (`<link>`, `<img src="/…">`).
> 12. `Firmenanschrift` ist in den Standorteinstellungen ein mehrzeiliges Feld; eine zweizeilige Anschrift
>     erscheint im Druck und im PDF zweizeilig; HTML-Zeichen in der Anschrift werden als Text ausgegeben.

> [!quote]- Test-Szenarien (alt, nur die ersetzten)
> - Happy Path: Firmendaten + Logo gepflegt → Druck und PDF zeigen Briefkopf.
> - Aenderung Firmendaten → naechster Druck aktualisiert.
> - Negativ: Logo-Pfad falsch → Druck ohne Logo + Bildschirmhinweis + Log.
> - Anschrift mehrzeilig pflegen (laut CD: „In der Schörihub 28" / „4810 Gmunden") → Druck zweizeilig;
>   Eingabe mit `<b>` → als Text sichtbar.

> [!quote]- affected_code (alt, Frontmatter)
> - IdealAkeWms/Views/Shared/_PrintBriefkopf.cshtml (neu — EIN Partial fuer Logo, Absender, Empfaenger, Dokumenttitel, Datum, Fusstext; CSS eingebettet, Logo als Data-URI)
> - IdealAkeWms/Views/FaHierarchyBeschichtung/Print.cshtml (Partial je .fa-group einbinden, vor dem group-head Zeilen 62-63; vorlaeufigen Titel ersetzen)
> - IdealAkeWms/Views/FaHierarchyBeschichtung/PrintSummiert.cshtml (nur falls Teil 1 vorher gemergt ist — sonst bindet Teil 1 nachtraeglich ein)
> - IdealAkeWms/Models/AppSettingKeys.cs (neue Keys fuer Logo-Quelle und Fusstext laut Antwort 1; Firmenname/Firmenanschrift existieren bei :74-75)
> - IdealAkeWms/Models/Standort/StandortSettingsCatalog.cs (neue Felder in GroupFirmendaten, :37-38; neuer mehrzeiliger Feldtyp im Enum StandortFieldType :3 fuer Firmenanschrift, Antwort 5)
> - IdealAkeWms/Views/StandortEinstellungen/Index.cshtml (textarea-Zweig fuer den neuen Feldtyp; heute einzeiliges input bei :88, Antwort 5)
> - IdealAkeWms/Services/ (kleiner Helfer Logo-Datei → Data-URI, nur falls Antwort 1 eine Datei als Quelle waehlt)
> - IdealAkeWms.Tests/ (Helfer-Test: MIME aus Endung, fehlende Datei → kein Absturz)

## Offene Rueckfragen

1. **„Anpassbare HTML-Vorlage" (Notiz Punkt 2):** vom Anwender in der Oberflaeche gepflegt (gespeicherte
   Vorlage) oder als Datei, die die IT aendert? Woher kommt das Logo (Upload in den Standorteinstellungen)?
   *Empfehlung der Koordination:* fester Partial mit Einstellungswerten fuer Logo, Anschrift, Fusstext — frei
   editierbares HTML in der Datenbank bringt Sicherheits- und Pflegerisiken. *Ergaenzung aus dem Code:* fuer
   das Logo eine Einstellung mit dem **Dateipfad** (Datei ausserhalb des Publish-Ordners, von der IT
   abgelegt), weil `AppSettings.Value` auf 500 Zeichen begrenzt ist und eine Datei im Publish-Ordner bei
   jedem Publish ueberschrieben wird. Ein Upload in der Oberflaeche braucht eine neue Speicherung samt
   Migration.
   *Beantwortet mit Antwort 1 (2026-10-09): HTML-Datei `Branding\briefkopf.html` der IT + Einstellung
   „Briefkopf-Vorlage (Pfad)" — abweichend von der Empfehlung; eingearbeitet in Anforderungen 1, 2, 7, 10, 11.*
2. **Corporate-Design-Vorlage:** Liegt sie vor (Logo in Druckaufloesung, Schrift, Farben, Anordnung des
   Adressblocks)? Ohne sie bleibt das Ergebnis vorlaeufig — Vorbedingung, nicht Begleitfrage
   (Briefkopf-Backlog, Frage 1).
   *Beantwortet mit Antwort 2: liegt vor, Anhang [[cd-auszug-druckdokumente]] + Logos (Anforderung 8).*
3. **Briefkopf je Seite:** nur auf der ersten Seite jedes HauptFA-Dokuments oder auf jeder Seite? Ist
   „Seite X von Y" gewuenscht?
   *Beantwortet mit Antwort 3: nur Seite 1; „Seite X von Y" nice to have (Anforderung 9).*
   *Empfehlung:* nur erste Seite, ~~keine Seitenzaehlung~~ *(verworfen mit Antwort 3: Seitenzaehlung nice to
   have)* — mit der heutigen Seitenstruktur ohne Zusatzaufwand;
   „jede Seite"/„X von Y" ist mit Print-CSS nicht trivial und waere der Anlass fuer die QuestPDF-Pruefung.
4. **Kommissionierlisten:** Briefkopf dort gleich, reduziert (nur Logo) oder gar nicht?
   *Beantwortet mit Antwort 4: gar nicht.*
   *Empfehlung:* gar nicht in diesem Teil — die Listen sind intern; der Partial ist so gebaut, dass ein
   spaeteres Einbinden eine Zeile ist.
5. **Anschrift mehrzeilig:** `Firmenanschrift` wird heute als einzeiliges `<input type="text">` gepflegt
   (`StandortEinstellungen/Index.cshtml:88`). Mehrzeiliges Feld (textarea, Zeilenumbrueche im Druck
   uebernommen) oder getrennte Felder Strasse/PLZ/Ort/Land?
   ~~*Beantwortet mit Antwort 5: ein mehrzeiliges Feld, Key bleibt.*~~ *(ueberholt durch den Nachtrag)*
   *Beantwortet mit Antwort 5 Nachtrag (2026-10-09): kein mehrzeiliges Feld, kein neuer Feldtyp; Anschrift steht
   in der `briefkopf.html`, `Firmenanschrift` bleibt einzeilig fuer den Ersatz-Kopf (Anforderung 1, AK 12).*
   *Empfehlung:* ein mehrzeiliges Feld (neuer Feldtyp im Katalog) — kleinster Eingriff, bestehender Key bleibt.
6. **CD-Schrift (neu 2026-10-07, aus dem CD-Auszug):** Laut Manual Nimbus Sans Novus — lizenzpflichtig. Darf
   sie auf dem Webserver installiert bzw. als Webfont (Data-URI) eingebettet werden? Der PDF-Renderer (Headless
   Edge) laeuft auf dem Webserver und sieht nur dort installierte oder eingebettete Schriften.
   *Empfehlung:* bis zur Klaerung serifenlose Systemschrift wie heute (`Print.cshtml:17`, Segoe UI/Arial);
   blockiert die Umsetzung nicht.
   *Beantwortet mit Antwort 6 (2026-10-09): ja, Lizenz liegt vor; Schriftdateien in `Branding\`, eingebettet;
   Fallback Systemschrift; nur Briefkopf (Anforderungen 2, 8; AK 2, 10, 17).*
7. **Kontaktdaten im Adressblock (neu 2026-10-07, aus dem CD-Auszug):** Der CD-Adressblock fuehrt Telefon,
   E-Mail und Web; die Standorteinstellungen haben dafuer keine Felder (`StandortSettingsCatalog.cs:37-38`:
   nur Firmenname/Firmenanschrift). In den Fusstext schreiben oder eigene Felder?
   *Empfehlung:* in den Fusstext (eine Einstellung, keine neuen Felder).
   *Beantwortet mit Antwort 7 (2026-10-09): stehen in der `briefkopf.html`; keine neuen Felder, auch kein
   Fusstext-Key (Anforderungen 1, 8).*
8. **Groessengrenzen (neu 2026-10-09, aus Antwort 1 + Code):** Die Vorlage und jede eingebettete Datei landen
   base64-kodiert in jeder Druckseite und in `in.html`; ohne Grenze kann eine versehentlich abgelegte grosse
   Datei jeden Druck aufblaehen. Grenzen: Vorlage ≤ 256 KB, je Datei ≤ 1 MB, Summe ≤ 3 MB?
   *Empfehlung:* diese Werte als Konstanten im Service (keine Einstellung — YAGNI); die grosse Logodatei
   (2379 × 945 px) wird ohnehin nicht gebraucht.
9. **SVG in der Vorlage (neu 2026-10-09):** SVG als Bild zulassen?
   *Empfehlung:* nein in dieser Ausbaustufe — das Logo ist JPEG; SVG traegt Script-Risiko (inline bzw. ueber
   `object`/`embed` ausfuehrbar, als `<img>` nicht) und braeuchte eine eigene Bereinigung.
10. **Caching ueber Anfragen (neu 2026-10-09, Antworten sagen nichts):** A) kein Cache, je Druckanfrage einmal
    lesen; B) `IMemoryCache` mit Schluessel Pfad + `LastWriteTimeUtc` der Vorlage und aller eingebetteten
    Dateien.
    *Empfehlung:* A — Druck ist selten, Lesen kostet Millisekunden, Aenderungen der IT wirken ohne
    Invalidierungslogik sofort (AK 3). B erst, wenn eine Messung es verlangt.
11. **Bereinigung ohne HTML-Parser (neu 2026-10-09, aus dem Code):** Das Web-Projekt referenziert keinen
    HTML-Parser (`IdealAkeWms.csproj:10-20`). A) schmale Regex-/String-Bereinigung (Elemente/Attribute/Schemata
    laut Anforderung 11) mit `ponytail:`-Kommentar, der die Grenze nennt (nicht gegen boeswillig verschachteltes
    Markup gebaut); B) neue Abhaengigkeit (z. B. HtmlSanitizer auf AngleSharp-Basis).
    *Empfehlung:* A — die Vorlage kommt von der IT, der Pfad ist nur durch Admins setzbar (Anforderung 10); die
    Bereinigung ist Schutz vor Versehen, nicht vor einem Angreifer mit Dateizugriff auf den Server (der haette
    ohnehin mehr Moeglichkeiten). Upgrade-Pfad B, falls die Vorlage je aus Anwender-Hand kommen sollte.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Vorlagen-Form und Logo-Quelle → **HTML-Datei auf dem Server (Gerald 2026-10-09, abweichend vom Vorschlag fester Partial): Die IT legt `briefkopf.html` samt Logo im Installationsverzeichnis im Unterordner `Branding\` ab, ausserhalb des Publish-Ordners (Deploy ueberschreibt sie nicht). Eine Einstellung „Briefkopf-Vorlage (Pfad)“ zeigt darauf; je Server (IDEAL/AKE) eine eigene Datei. Beim Einlesen: Bilder als Data-URI einbetten (Headless Edge), `<script>` entfernen. Datei fehlt/unlesbar ⇒ einfacher Ersatz-Kopf aus Firmenname + Anschrift, Warnung ins Log, Druck bricht nicht ab. Der Lauf liefert eine Muster-`briefkopf.html` nach CD-Satzspiegel mit. Kein Upload in der Oberflaeche, kein HTML in der Datenbank. PDF als Vorlage verworfen (Overlay braucht PDF-Bibliothek, Druckansicht zeigt es nicht).**
2. Corporate-Design-Vorlage vorhanden → **Ja (Gerald 2026-10-06): CD-Manual Stand 01/23 und Logos liegen vor – Auszug und Logos in `backlog/anhaenge/2026-10-06-beschichtungsauftrag-ausbau/` ([[cd-auszug-druckdokumente]]: Farben, Typografie, Satzspiegel A4, Logozone, Adressblock).**
3. Briefkopf je Seite / Seitenzaehlung → **Briefkopf nur auf Seite 1; Seitenzählung „Seite X von Y“ ist nice to have – nur umsetzen, wenn ohne großen Aufwand möglich (Gerald 2026-10-06).**
4. Kommissionierlisten → **Nein, kein Briefkopf auf den Kommissionierlisten (Gerald 2026-10-06).**
5. Anschrift mehrzeilig → **Mehrzeilig (Gerald 2026-10-06, korrigiert): ein mehrzeiliges Feld in den Standorteinstellungen (neuer Feldtyp im Katalog, bestehender Key `Firmenanschrift` bleibt), Zeilenumbrüche werden im Druck übernommen. Der heutige Wert hatte einen Tippfehler; Gerald korrigiert ihn auf die Angabe laut CD-Manual (In der Schörihub 28, 4810 Gmunden); bestaetigt 2026-10-09, Korrektur macht Gerald in den Einstellungen – keine Sonderlogik im Code.** **Nachtrag (Gerald 2026-10-09): Mehrzeiliges Feld entfaellt – die Anschrift steht in der `briefkopf.html` (Antwort 1). `Firmenanschrift` bleibt einzeilig wie heute und dient nur dem Ersatz-Kopf; kein neuer Feldtyp.**
6. CD-Schrift Nimbus Sans Novus auf Server/eingebettet zulaessig → **Ja (Gerald 2026-10-09): Lizenz liegt vor, die Schriftdateien duerfen im Ordner `Branding\` abgelegt werden. Die `briefkopf.html` bindet sie per `@font-face` ein; beim Einlesen werden Schriftdateien wie Bilder als Data-URI eingebettet (Headless Edge). Fehlt die Schriftdatei, faellt der Druck auf die Systemschrift zurueck (Segoe UI/Arial, `Print.cshtml:17`). Die Schrift gilt fuer den Briefkopf; die uebrige Druckansicht wird nicht umgebaut.**
7. Kontaktdaten: Fusstext oder eigene Felder → **Erledigt durch Antwort 1 (2026-10-09): Telefon, E-Mail, Web stehen in der `briefkopf.html`; keine neuen Felder.**
8. (neu 2026-10-09) Groessengrenzen Vorlage 256 KB / je Datei 1 MB / Summe 3 MB → **Ja, als Konstanten im Service (Gerald 2026-10-09); Ueberschreitung ⇒ Ersatz-Kopf + Log-Warnung.**
9. (neu 2026-10-09) SVG als Bild in der Vorlage zulassen (Empfehlung nein) → **Nein, nur JPG/PNG (Gerald 2026-10-09).**
10. (neu 2026-10-09) Caching ueber Anfragen: A kein Cache (Empfehlung) / B IMemoryCache mit LastWriteTime → **A – kein Cache (Gerald 2026-10-09).**
11. (neu 2026-10-09) Bereinigung: A Regex/String mit ponytail-Grenze (Empfehlung) / B neue Abhaengigkeit HtmlSanitizer → **A – schlanke eigene Bereinigung mit ponytail-Grenze, keine neue Abhaengigkeit (Gerald 2026-10-09).**

## Umsetzung (Dev-Lauf 2026-10-09)

Direkt im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (Zweig `feature/2026-08-07-ideal-teile-1-5`),
Start `cc86d373` (v1.48.0). Plan `docs/superpowers/plans/2026-10-09-beschichtungsauftrag-ausbau-teil-2.md`.
Commits `1c560d30` (Service + Einstellung), `1cda91dc` (Partials + Einbindung), `742f7855` (feature-complete, v1.49.0).

**Ruling zu Antwort 8 (kein Widerspruch, Auslegung):** Antwort 8 sagt „Überschreitung ⇒ Ersatz-Kopf"; der Rumpf
(Anforderung 11, AK 20) unterscheidet: Vorlage zu groß ⇒ Ersatz-Kopf, einzelne Ressource/Summe zu groß ⇒ nicht
eingebettet. Umgesetzt wie der Rumpf (der Rumpf ist der Auftrag, die Antwort bestätigt die Grenzwerte als Konstanten).
Kosten falls falsch: eine Zeile im Service (`Reject` → Ersatz) plus zwei Tests.

**Was gebaut ist:**
- `Services/BriefkopfVorlageService.cs` (scoped, je Anfrage einmal gelesen, kein Cache darüber): `Load(pfad)` statisch
  und dateibasiert — Endung `.html/.htm`, Existenz, 256 KB, Lesefehler ⇒ Ersatz; schlanke Regex-Bereinigung
  (`ponytail:`-Grenze im Code: Schutz vor Versehen, nicht gegen böswilliges Markup) — Kommentare, `script`/`iframe`/
  `object`/`embed`/`svg`/`noscript`/`template` samt Inhalt, `link`/`base`/`meta`, `on*`, `srcset`, `javascript:`/
  `vbscript:` in href/src/action, `@import`; `<style>` → Styles, `<body>`-Inhalt (sonst Datei ohne head) → Markup;
  `url(...)` in Styles und style-Attributen sowie `src` aller Elemente gegen den Branding-Ordner aufgelöst
  (kein Schema, kein absoluter/UNC-Pfad, kein Ausbruch), nur png/jpg/jpeg/woff/woff2/ttf/otf, je Datei ≤ 1 MB, Summe
  ≤ 3 MB (jede Datei einmal angerechnet), vorhandene `data:`-URIs nur mit erlaubtem MIME. Abgelehnt/fehlend: `<img>`
  entfällt (kein Broken-Image), `url(...)` → `none` (Schrift fällt auf Segoe UI/Arial zurück). Jeder Fall = Hinweis.
  `GetAsync` ergänzt Firmenname/Firmenanschrift und loggt jeden Hinweis als Warnung.
- `AppSettingKeys.BriefkopfVorlagePfad`, Katalogfeld „Briefkopf-Vorlage (Pfad)" in Firmendaten mit Hint (Wortlaut
  Anforderung 10), DI in `Program.cs`. Kein neuer Feldtyp; `Firmenanschrift` unverändert einzeilig.
- `Views/Shared/_PrintBriefkopfStyles.cshtml` (einmal im head: Vorlagen-Styles + WMS-Grundregeln, Hinweise
  `@media print` aus) und `Views/Shared/_PrintBriefkopf.cshtml` (Vorlagen-Markup bzw. Ersatz-Kopf, Empfänger/Dokument/
  Datum, Hinweise), Modell `PrintBriefkopfModel` (Empfänger-Regel). Eingebunden in
  `FaHierarchyBeschichtung/Print.cshtml` und `PrintSummiert.cshtml` direkt nach `<div class="fa-group">` — damit nur auf
  Seite 1 jedes Dokuments; die PDF-Actions rendern dieselben Views.
- Muster `Branding/briefkopf.html` (Repo-Wurzel, nicht im Publish-Output) nach CD-Auszug: Logozone 37,125 mm nur mit
  Logo 17,5 mm, Absenderzeile „IDEAL Kältetechnik GmbH · In der Schörihub 28, 4810 Gmunden · …" (Firma `#053153`, Text
  `#4a494a`), AKE Blau nur als Linie, `@font-face` mit Platzhalter-Dateinamen, alle Regeln unter `.briefkopf`,
  Kommentar mit den Regeln für die IT; daneben `IDEAL_AKE_Logo_rgb_klein.jpg`. Schriftdateien nicht im Repo.
- README (AppSettings-Zeile + Abschnitt „Briefkopf-Vorlage"), Hilfe, Anwender-Changelog, Version 1.49.0, TS-82.

**Bewusst nicht gebaut:**
- **„Seite X von Y" (Anforderung 9, AK 14):** weggelassen. CSS-Seitenzähler (`@page` Randfelder) zählen im
  Bildschirmdruck mehrerer HauptFA-Dokumente über den ganzen Druck — Chromium kann den Seitenzähler nicht je Dokument
  zurücksetzen; „je Dokument korrekt" ginge nur im PDF (eine Gruppe je Datei). AK 14 verlangt „korrekt oder gar nicht".
  Aufgabe [[2026-10-09-briefkopf-seitenzaehlung]].
- **`@page`-Satzspiegel 17,5/24,75 mm:** nicht umgebaut (Anforderung 8: nur wenn Bildschirm und PDF gleich — ohne
  Messung am Server nicht belegbar); die Ränder bleiben die heutigen der Druckansicht. Logozone/Logo-Höhe stehen in der
  Vorlage selbst.

**Nachweis (Dev-Lauf, vor QA):**
```
dotnet build IdealAkeWms.slnx         -> 0 Fehler
dotnet test IdealAkeWms.Tests         -> Bestanden! Fehler: 0, erfolgreich: 1512, übersprungen: 1, gesamt: 1513
dotnet test IDEALAKEWMSService.Tests  -> Bestanden! Fehler: 0, erfolgreich: 272, gesamt: 272
```
Neu: 38 Tests (`BriefkopfVorlageServiceTests` inkl. Muster-Wächter, `BriefkopfEinbindungTests`), vorher rot
(Kompilierfehler). `docs/TESTSZENARIEN.md` byte-genau LF. **Nicht** automatisiert und nicht lokal gerendert: die
fertige Druckseite/das PDF (Browser, Edge, Schrift im PDF, Seitenumbruch) — Manual-UAT TS-82.

## QA-Nachweis (qa-agent, 2026-10-09)

Unabhaengiger Lauf, synchron, ohne Unteragenten. Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Zweig
`feature/2026-08-07-ideal-teile-1-5`, Diff `cc86d373..742f7855` (3 Commits `1c560d30`, `1cda91dc`, `742f7855`, v1.49.0).
Im Worktree ist kein `secondbrain/` sichtbar (sparse-checkout greift). **Ergebnis: Status bleibt `InUmsetzung`** —
Build und Tests gruen, aber ein offener Important-Befund (I-1).

### 1. Build und Tests (selbst ausgefuehrt)

```
dotnet build IdealAkeWms.slnx                   -> 12 Warnung(en), 0 Fehler (vorbestehend, keine in geaenderten Dateien)
dotnet test IdealAkeWms.Tests --no-build        -> Bestanden! Fehler: 0, erfolgreich: 1512, übersprungen: 1, gesamt: 1513
   (Skip vorbestehend: ProductionOrderEagerCreateAgentJobTests.EagerCreate_TwoMergesProduce2Rows_PerNewOrder_AndAreIdempotent)
dotnet test IDEALAKEWMSService.Tests --no-build -> Bestanden! Fehler: 0, erfolgreich: 272, übersprungen: 0, gesamt: 272
```

Zusaetzlich Sonden gegen `BriefkopfVorlageService.Load` (datei-basiertes `dotnet run` mit Projektverweis, nur im
Scratchpad, nichts im Repo) — Ergebnisse bei den Befunden zitiert.

### 2. Code-Review (Befunde)

Geprueft: `Services/BriefkopfVorlageService.cs` (komplett), `Views/Shared/_PrintBriefkopf.cshtml`,
`_PrintBriefkopfStyles.cshtml`, `Models/ViewModels/PrintBriefkopfModel.cs`, Einbindung in
`FaHierarchyBeschichtung/Print.cshtml` und `PrintSummiert.cshtml`, `_BeschichtungKopfDruck.cshtml` (Mehrdeutig-Regel),
`RazorViewRenderer.cs:28-38` + `FaHierarchyBeschichtungController.cs:115-139, 198-219` (Scope im PDF-Weg),
`AppSettingKeys.cs`, `StandortSettingsCatalog.cs`, `Program.cs:114`, `Branding/briefkopf.html`, beide neuen
Testdateien, README-/Hilfe-/Changelog-/TESTSZENARIEN-Diff. Hausklassen: kein Wert in `<script>`-Bloecken, kein
`hidden` + `d-*`, keine stillen Fallbacks (leere OrderInfos ⇒ sichtbarer Text „Empfänger fehlt"), keine parallelen
Arrays, keine reine Browser-Validierung — ohne Befund.

**Critical:** keine. Kein Nicht-Admin-Weg zu Script oder Fremdladen: Pfad nur ueber `[RequireAdminAccess]`
(Standorteinstellungen, `/Settings`), die Vorlage ist eine Datei auf dem Server; alle Werte, die Nicht-Admins oder Sage
beeinflussen (Firmenname, Firmenanschrift, Dienstleister, Dokument, HauptFA), gibt Razor HTML-kodiert aus
(`_PrintBriefkopf.cshtml:24-31`). Kein realistisches Versehen der IT fuehrt Script aus oder laedt fremd — alle
gefundenen Umgehungen (M-1) brauchen absichtlich gebautes Markup.

**Important:**

- **I-1 — Unausgeglichenes Markup der Vorlage zerstoert die Druckseite still (Anforderung 7, AK 10).** `Load` gibt
  Markup, das der Bereinigung entgeht, unveraendert und **ohne Hinweis** weiter; `_PrintBriefkopf.cshtml:19` setzt es
  per `Html.Raw` in jede `.fa-group`. Sonden (je `hints=0`):
  - `<div>offen<!-- vergessenes Ende` → Markup unveraendert. `BriefkopfVorlageService.cs:133` entfernt nur
    **geschlossene** Kommentare; ein offenes `<!--` im `<body>` macht im Browser **den ganzen Rest der Seite** zum
    Kommentar — Kopftabelle, Positionen, Barcode und im Bildschirmdruck alle folgenden HauptFA-Dokumente fehlen (die
    Druckseite enthaelt kein `-->`). Der Bildschirmhinweis steht hinter dem Markup und verschwindet mit.
  - `<style>.briefkopf{color:red}` ohne `</style>` → bleibt im Markup (`:107` verlangt das Endtag); der Browser liest
    den Rest der Seite als CSS. Gleiches gilt fuer `<title>`, `<textarea>`, `<xmp>`, `<plaintext>`.
  - Ein ueberzaehliges `</div>` schliesst `.briefkopf`, dann `.briefkopf-block`, dann die `.fa-group` (Wrapper
    `_PrintBriefkopf.cshtml:14, 19, 37`) — `page-break-after: always` (`Print.cshtml:22`) greift direkt nach dem
    Briefkopf, Kopftabelle und Positionen rutschen auf eine eigene Seite.

  Warum Important: ein einzelner Tippfehler der IT in genau der Datei, die sie laut Muster pflegen soll, trifft jeden
  Druck aller Anwender, das Dokument geht nach aussen, und das WMS meldet nichts. Vorschlag (wenige Zeilen + Tests):
  Kommentar-Regex auf `<!--.*?(-->|\z)`; nach dem Herausloesen der Styles verbliebene Rawtext-Oeffner
  (`style|title|textarea|xmp|plaintext|noembed|noframes`) entfernen; `<div`/`</div>`-Zaehlung ungleich ⇒ Ersatz-Kopf +
  Hinweis „Vorlage hat unausgeglichene div-Tags". Danach TS-82 um einen Negativfall ergaenzen.

**Minor:**

- **M-1 — Bereinigungs-Umgehungen (innerhalb der dokumentierten ponytail-Grenze, `BriefkopfVorlageService.cs:126-129`).**
  Per Sonde bestaetigt, alle nur mit absichtlich gebautem Markup:
  `<scr<meta>ipt>alert(1)</scr<meta>ipt>` ⇒ `<script>alert(1)</script>` (`:134` laeuft vor `:135`; das Entfernen von
  `<meta>` setzt ein neues Script-Tag zusammen); Attribute ohne Leerzeichen-Trenner `<div/onclick=…>`,
  `<a href="x"onclick="…">` (`:136` verlangt `\s+`); entity-kodiertes `href="&#106;avascript:…"` (`:141-147`
  dekodiert nicht); Fremdladen ueber `<img/src="https://…">` und `<img alt="a>b" src="https://…">` (`:165-167`:
  `[^>]*` bricht am `>` im Attribut, `\ssrc` braucht Leerzeichen), `<table background="https://…">`,
  `<video poster="https://…">`, CSS `image-set('https://…')` ohne `url(` (`:158`); Ausbruch aus dem `<style>` per
  `</style x>` oder `</style/>` (`:107` verlangt `</style\s*>`) — der Kommentar `_PrintBriefkopfStyles.cshtml:59`
  („kann kein </style> enthalten") ist damit ungenau. `<a href="data:text/html,…">` bleibt (Chromium blockiert
  Top-Level-Navigation zu `data:`), `<form action>`/`formaction` bleiben (nur per Klick). Bewertung: kein Critical.
  Billigster Guertel, falls gewuenscht: CSP-`<meta>` in beiden Druckansichten mit `img-src data:; font-src data:`
  (sperrt alle Fremdlade-Varianten browserseitig; gegen Inline-Handler hilft sie nicht, weil die Druckknoepfe `onclick`
  nutzen). Upgrade-Pfad laut Code: HtmlSanitizer. → als Folgeaufgabe festhalten.
- **M-2 — Attribut-Regex wirkt auch auf Text.** `:136` laeuft ueber den ganzen Text, nicht nur in Tags: Sonde
  `<p>Kontakt online = 24/7</p>` ⇒ `<p>Kontakt>` (Text **und** `</p` verloren). Selten im Briefkopf, aber still.
- **M-3 — Mehrfachverweis vervielfacht die Ausgabe.** Die Summengrenze rechnet jede Datei einmal (`:198`), jeder
  Verweis setzt den Data-URI aber erneut ein (Sonde: 5 × `<img src=logo.jpg>` ⇒ 5 Data-URIs). Eine absichtlich gebaute
  256-KB-Vorlage mit tausenden Verweisen auf eine 1-MB-Datei erzeugt einen Gigabyte-String (OOM in `Regex.Replace`,
  ausserhalb des `try` von `:82-101`). Nur IT/Admin; realistisch (1–2 Verweise) unkritisch. Die Wiederholung je
  `.fa-group` ist bekannt und kommentiert (`_PrintBriefkopf.cshtml:17-18`).
- **M-4 — Pfad in Anfuehrungszeichen** (Explorer „Als Pfad kopieren") ⇒ Ersatz-Kopf mit irrefuehrendem Hinweis „keine
  .html/.htm-Datei: <Arbeitsverzeichnis>\"C:\…\"" (Sonde). Gemeldet, nicht still; `Trim().Trim('"')` in `:84` faengt es ab.
- **M-5 — Hinweise zeigen Serverpfade und Ausnahmetexte** (`:91`, `:100`, `:202`, `:216`) auch Nicht-Admins mit Rolle
  `beschichtungsauftrag` am Bildschirm. Von AK 6 / TS-82.5 so verlangt („nicht gefunden: <Pfad>") — akzeptiert, benannt.
- **M-6 — Logozone relativ zum heutigen Seitenrand.** Ohne `@page` (bewusst, „Bewusst nicht gebaut") beginnt die
  37,125-mm-Logozone unter dem Standard-Druckrand von Edge, nicht an der Papierkante; die CD-Lage des Logos ist um diesen
  Rand verschoben. Steht als Hinweis in TS-82.14 — akzeptiert.
- **M-7 — Junctions/Symlinks im Branding-Ordner** werden nicht aufgeloest (`:191-193` prueft den normalisierten Pfad).
  Nur die IT kann sie anlegen, Typen sind auf Bild/Schrift begrenzt — akzeptiert.
- **M-8 — Nicht unter `.briefkopf` skopierte Regeln der IT** wirken auf die ganze Druckseite (AK 10 haengt an der
  Disziplin der IT). So im Entwurf gewaehlt, in Muster-Kommentar und README benannt — akzeptiert.
- **M-9 — Mehrdeutig-Regel an zwei Stellen** (`PrintBriefkopfModel.cs:20` `Count > 1`, `_BeschichtungKopfDruck.cshtml:4`
  `Model.Count > 1`). Heute gleich; driftet eine, widersprechen sich Empfaengerfeld und Box. Benannt.
- **M-10 — Brain-Checkliste:** `secondbrain/feature-map.md` hat keinen Eintrag fuer v1.49.0/Briefkopf (Grep
  `briefkopf|1.49` ohne Treffer; v1.48.0 steht bei `:650`). Testindex (`testszenarien-index.md:108`), Aufgabe
  [[2026-10-09-briefkopf-seitenzaehlung]] und Brain-Changelog `2026-10-09-v1-49-0-briefkopf-beschichtungsauftrag` sind
  vorhanden.

Positiv belegt: Pfadpruefung — jedes `:` (Schema, Laufwerk, ADS) und fuehrendes `/`/`\` (UNC, protokollrelativ,
rooted) abgelehnt (`:187`), danach `GetFullPath` + Praefixvergleich mit Trennzeichen, ordinal ignore-case (`:153`,
`:191-193`); Endung der Vorlage vor dem Lesen geprueft (`:85-87`); Groessen vor dem Lesen (`:92`, `:203-206`); alle
Datei-Ausnahmen gefangen (`:98-101`, `:192`, `:214-217`), keine Ausnahme erreicht die View. Scope: `GetAsync` merkt das
Ergebnis im scoped Feld (`:59`, `:70`), beide Partials teilen es; `RazorViewRenderer` baut den `ViewContext` aus dem
echten `ControllerContext` (`RazorViewRenderer.cs:28-38`), `@inject` loest im PDF-Weg also aus demselben Request-Scope
auf. `Styles` steht in einem **eigenen** `<style>` nach den WMS-Grundregeln (`_PrintBriefkopfStyles.cshtml:45-63`) — ein
offener Kommentar / eine offene Klammer in der Vorlagen-CSS reisst die `@media print`-Regel fuer die Hinweise (`:55`)
nicht mit. Ersatz-Kopf HTML-kodiert (`_PrintBriefkopf.cshtml:24-25`), Hinweise nur Bildschirm, Einbindung direkt nach
`<div class="fa-group">` vor Filter-/Summen-Banner (`Print.cshtml:56`, `PrintSummiert.cshtml:58`), Inhalt darunter
unveraendert (Diff je View nur +2 Zeilen), keine Kommissionierlisten.

### 3. Muster `Branding/briefkopf.html` gegen den CD-Auszug

Logozone `37.125mm` nur mit dem Logo `17.5mm` hoch, Flex vertikal mittig (`briefkopf.html:34-35, 41-43`); Absender als
eine Zeile laut Manual-Beispiel (`:44-47`), Firma `#053153` (`:37`), Lauftext `#4a494a` (`:33`), AKE Blau `#43a6e2` nur
als Linie (`:36`); Font-Stack `'Nimbus Sans Novus', 'Segoe UI', Arial, sans-serif`; alle Regeln unter `.briefkopf`
(`@font-face` per Definition global). Kontrast auf Weiss (WCAG-Formel, QA-Rechnung): `#053153` **13,37:1**, `#4a494a`
**8,96:1** (beide AA fuer Fliesstext), `#43a6e2` **2,70:1** (zu wenig fuer Text — korrekt nur als Linie); Warnfarbe
`#7a5900` auf Weiss 6,45:1, auf `#fff8e1` 6,07:1. Nicht im Publish-Output: `Branding/` liegt in der Repo-Wurzel, kein
`*.csproj` (Web, Service, beide Tests) und keine `Directory.Build.*` verweisen darauf. Logodatei 34 381 Bytes.

### 4. Einstellung, Doku, Version, Zeilenenden

`AppSettingKeys.BriefkopfVorlagePfad` (+ Kommentar kein Seed/keine Migration), Katalogfeld String in Firmendaten mit
Hint-Wortlaut laut Anforderung 10, kein neuer Feldtyp, `Firmenanschrift` unveraendert. `Program.cs` `AddScoped`.
README AppSettings-Zeile + Abschnitt „Briefkopf-Vorlage", Hilfe `Help/Index.cshtml`, Anwender-Changelog v1.49.0, beide
`AppVersion.cs` 1.48.0 → 1.49.0. Keine Migration, kein SQL, keine AgentJobs, keine Audit-Felder (keine Entitaet).
**Zeilenenden** (python, `git cat-file blob HEAD:<datei>`, `b'\r\n'`-Zaehlung) aller 18 geaenderten Textdateien: 0 CRLF,
durchgehend LF, wie in `cc86d373` (z. B. `docs/TESTSZENARIEN.md` 0/9398, `README.md` 0/816, `Help/Changelog.cshtml`
0/2172); kein BOM.

### 5. Geaenderte bestehende Tests

Keine. Beide Testdateien sind neu (`git diff --name-status`: `A BriefkopfVorlageServiceTests.cs`,
`A BriefkopfEinbindungTests.cs`).

### 6. Beweisart je AK

| AK | Beweis | Test / Szenario |
|---|---|---|
| 1 | Manual-UAT (Einbindung strukturell automatisiert) | `BeschichtungDruck_BindetBeidePartialsEin_KopfDirektNachFaGroup`, `Load_ValidTemplate_EmbedsImageAndFont_AsDataUri`; TS-82.2 |
| 2 | Manual-UAT | TS-82.2 (PDF, Schrift im PDF) |
| 3 | Manual-UAT (Laden je Scope automatisiert) | `GetAsync_ReadsSettings_LogsHints_AndLoadsOncePerScope`; TS-82.4 |
| 4 | automatisiert + Manual-UAT | `Empfaenger_MissingDienstleister_IsVisibleHint`, `Empfaenger_NoHead_IsVisibleHint`; TS-82.11 |
| 5 | automatisiert + Manual-UAT | `Empfaenger_Kombigeraet_IsAmbiguousHint`; TS-82.11 |
| 6 | automatisiert (Service) + Manual-UAT (Bildschirm/Druck/PDF/Log) | `Load_NoPath_IsErsatz_NotConfigured`, `Load_FileMissing_IsErsatz_WithPath`, `Load_TemplateTooLarge_IsErsatz`, `Load_PathIsDirectory_FallsBackToErsatz`, `Load_WrongExtension_IsErsatz_AndContentNeverAppears`; TS-82.5 |
| 7 | automatisiert | `BeschichtungDruck_BindetBeidePartialsEin_KopfDirektNachFaGroup` |
| 8 | Manual-UAT | TS-82.3 |
| 9 | automatisiert (Normalfaelle) + Manual-UAT; Grenze M-1 | `Load_RemovesActiveContent`, `Load_ForeignOrEscapingReference_NotEmbedded`; TS-82.9 |
| 10 | Manual-UAT; **durch I-1 verletzbar** | TS-82.2, TS-82.13 |
| 11 | automatisiert (Skopierung, Logo) + Manual-UAT (mm, Farben) + QA-Kontrastrechnung | `MusterVorlage_Laedt_LogoEingebettet_NurSchriftdateienFehlen_AlleRegelnUnterBriefkopf`; TS-82.14 |
| 12 | Manual-UAT | TS-82.12 |
| 13 | automatisiert (Quelltext) + Manual-UAT | `KommissionierDruck_HatKeinenBriefkopf`; TS-82.13 |
| 14 | erfuellt durch Weglassen + Aufgabe | [[2026-10-09-briefkopf-seitenzaehlung]]; TS-82.3 |
| 15 | Manual-UAT (Filter vorbestehend) | TS-82.12 |
| 16 | automatisiert + Manual-UAT | `Load_ImageMissing_ImgRemoved_HintNamesFile`; TS-82.6 |
| 17 | automatisiert + Manual-UAT | `Load_FontMissing_UrlBecomesNone_HintNamesFile`; TS-82.7 |
| 18 | automatisiert + Manual-UAT (Browser) | `Load_RemovesActiveContent`; TS-82.8 |
| 19 | automatisiert (9 Faelle) + Manual-UAT (Netzwerk/Proxy) | `Load_ForeignOrEscapingReference_NotEmbedded`, `Load_SvgFile_Rejected`, `Load_DataUri_AllowedKept_SvgRemoved`; TS-82.9 |
| 20 | automatisiert + Manual-UAT | `Load_ResourceOverFileLimit_NotEmbedded`, `Load_ResourcesOverSumLimit_LaterOnesNotEmbedded`, `Load_SameFileTwice_CountsOnceAgainstSum`; TS-82.10 |
| 21 | automatisiert + Manual-UAT | `Load_WrongExtension_IsErsatz_AndContentNeverAppears`; TS-82.5 (c) |
| 22 | automatisiert, gruen | `BriefkopfVorlageServiceTests` + `BriefkopfEinbindungTests` (Lauf oben) |

**Bewusst Weggelassenes:** „Seite X von Y" (AK 14) — Begruendung tragfaehig (Seitenzaehler laeuft im Sammeldruck ueber
alle Dokumente; AK 14 verlangt „je Dokument korrekt oder gar nicht"), Aufgabe vorhanden; AK 14 damit erfuellt.
`@page`-Satzspiegel — Anforderung 8 erlaubt ihn nur bei belegter Gleichheit Bildschirm/PDF; ohne Servermessung ist
Weglassen spec-konform (Folge M-6). Ruling zu Antwort 8 (Ressource zu gross ⇒ nicht eingebettet statt Ersatz-Kopf)
folgt dem Rumpf (Anforderung 11, AK 20) — nachvollziehbar.

**TS-82 ohne Rueckfragen abarbeitbar?** Ja, mit zwei Luecken: TS-82.3 nennt keinen Weg zu einem mehrseitigen HauptFA
(Vorschlag: HauptFA mit den meisten beschichteten Positionen per Abfrage auf `dbo.FaHierarchyNodes`, Spaltennamen vor
Gebrauch pruefen); I-1 hat noch keinen Negativfall (nach dem Fix ergaenzen).

### 7. Manuelle Test-Checkliste (Mensch, Testsystem IDEAL)

1. Deploy-Schritte 1–7 aus „Deploy" ausfuehren.
2. TS-82.1: Sage-Pruefabfrage „Dienstleister? · RAL 0" ausfuehren, Ergebnis eintragen.
3. TS-82.2: Liste + Summiert drucken, je ein PDF — Logo, Absenderzeile, Empfaenger/Dokument/Datum; PDF-Eigenschaften →
   Schriftarten zeigt Nimbus Sans Novus; Kopftabelle/Positionen in Segoe UI unveraendert.
4. TS-82.3: mehrseitiger HauptFA als PDF — Briefkopf nur auf Seite 1, keine Seitenzaehlung.
5. TS-82.4: Telefonnummer in `briefkopf.html` aendern, Logo tauschen → naechster Druck zeigt beides ohne Neustart.
6. TS-82.5 a/b/c: Pfad leer, Datei umbenannt, Pfad auf `appsettings.json` → Ersatz-Kopf, gelber Hinweis nur am
   Bildschirm (Druckvorschau und PDF ohne Hinweis), Log-Warnung „Briefkopf: …", kein JSON-Inhalt, PDF ohne Umleitung.
7. TS-82.6 / 82.7: Logo bzw. Schriftdateien umbenennen → ohne Logo (kein Broken-Image) bzw. Systemschrift, Hinweis
   nennt die Datei.
8. TS-82.8: Script / `onerror` / `javascript:` in die Vorlage → kein Dialog, Quelltext ohne diese Teile,
   Drucken/Schliessen funktionieren.
9. TS-82.9: `..\..\x.png`, `C:\Windows\x.png`, `\\server\share\x.png`, `https://www.ideal-ake.at/logo.png` → nicht
   geladen (F12 → Netzwerk ohne fremde Hosts), Hinweise „abgelehnt"; Quelltext: alle `src=`/`url(` mit `data:`.
10. TS-82.10: Vorlage > 256 KB → Ersatz-Kopf; Logo > 1 MB → ohne Logo, jeweils Hinweis.
11. TS-82.11: HauptFA ohne Dienstleister → „Empfänger fehlt — in Sage nachpflegen"; Kombigeraet → „mehrdeutig — siehe
    Kopfzeilen" + gelbe Box.
12. TS-82.12: Feld „Briefkopf-Vorlage (Pfad)" mit Hinweistext sichtbar, Anschrift einzeilig; Nicht-Admin →
    `/StandortEinstellungen` → AccessDenied.
13. TS-82.13: Kommissionierliste (Liste, Summiert) drucken → kein Briefkopf.
14. TS-82.14: Lineal am Ausdruck — Logo 17,5 mm, Logozone frei, Farben; Lage relativ zum Druckrand notieren (M-6).
15. **Nach dem I-1-Fix zusaetzlich:** `<!-- offen` im `<body>` der Vorlage bzw. ein ueberzaehliges `</div>` → Druck
    vollstaendig (Kopftabelle + Positionen auf Seite 1) bzw. Ersatz-Kopf, jeweils mit Hinweis.

**Status:** bleibt `InUmsetzung` wegen I-1. Nach dem Fix (je Fall ein Test) erneuter QA-Lauf; M-1 und M-10 als
Folgeaufgabe bzw. Brain-Nachtrag, M-2 bis M-9 nach Ermessen.

### Nachlauf nach 020bf8af (qa-agent, 2026-10-09)

Synchron, ohne Unteragenten. Gleicher Worktree, Commit `020bf8af` (3 Dateien: `Services/BriefkopfVorlageService.cs`,
`BriefkopfVorlageServiceTests.cs`, `docs/TESTSZENARIEN.md`). **Ergebnis: I-1 geschlossen, keine offenen Critical/Important
⇒ Status `Testbereit`.**

**Build und Tests (selbst ausgefuehrt):**

```
dotnet build IdealAkeWms.slnx                   -> 12 Warnung(en), 0 Fehler (unveraendert vorbestehend)
dotnet test IdealAkeWms.Tests --no-build        -> Bestanden! Fehler: 0, erfolgreich: 1521, übersprungen: 1, gesamt: 1522
   (+9 = 6 Theory-Faelle Load_UnbalancedMarkup_IsErsatz_WithHint + Load_TextThatLooksLikeAnAttribute_StaysUntouched
    + Load_QuotedPath_IsAccepted + Load_ManyReferencesToSameFile_StopAtOutputBudget; Skip vorbestehend wie oben)
dotnet test IDEALAKEWMSService.Tests --no-build -> Bestanden! Fehler: 0, erfolgreich: 272, übersprungen: 0, gesamt: 272
```

Geaenderte bestehende Tests: keine (nur Ergaenzungen in der im Zweig neuen Datei `BriefkopfVorlageServiceTests.cs`).

**Sonden gegen `BriefkopfVorlageService.Load`** (datei-basiertes `dotnet run` mit Projektverweis, nur Scratchpad):

| Eingabe | Ergebnis |
|---|---|
| `<div>offen<!-- vergessenes Ende` | Ersatz + „nicht geschlossenen Kommentar" |
| `<style>.briefkopf{color:red}` (ohne Ende) | Ersatz + „unvollständiges Element <style>" |
| `<style>…</style x><img src=logo.jpg>` | Ersatz + „unvollständiges Element <style>" |
| `<style>.a{}</style x><div>a</div><style>.b{}</style>` | Ersatz + „Style-Block mit fehlerhaftem Ende" |
| je offen `<title>`, `<textarea>`, `<xmp>`, `<plaintext>`, `<noembed>` | Ersatz + „unvollständiges Element <…>" |
| `<div>a</div></div>` / `<div class=x>offen` | Ersatz + „ungleich viele <div> (1) und </div> (2)" bzw. „(1)/(0)" |
| `<div>a</div >` · `<DIV>a</DIV>` · `<DIV>a</div>` | geladen, unveraendert (Leerzeichen/Grossschreibung korrekt gezaehlt) |
| `<div>` im `<head>`, mit und ohne `<body>`, auch offenes `<div>` im head | geladen, nur Body-Markup (`<div>a</div>`) — head wird verworfen, zaehlt nicht |
| `<!-- <div> im Kommentar --><div>a</div>` | geladen (Kommentare vor der Zaehlung entfernt) |
| `<title>Fragment</title>` mit/ohne `<head>`, offenes `<title>` im `<head>` | geladen, title entfernt |
| `<div><textarea>x</textarea>b</div>` | geladen, `<div>b</div>` (Paar entfernt) |
| `Branding/briefkopf.html` (Muster, Kopie mit Logo) | geladen, Logo als `data:image/jpeg`, nur die 2 Hinweise „Schriftdatei nicht gefunden" (erwartet ohne Lizenzdateien) |
| `<div title="<!--">a</div>` | **Ersatz** + Kommentar-Hinweis (falsch-positiv, aber laut — akzeptiert) |
| `<div data-x="<div">a</div>` | **Ersatz** + div-Hinweis (falsch-positiv, laut — akzeptiert) |
| `<table><tr><td>offen` · `<a href=x>offen<div>a</div>` · `</div><div>a` · `<div title="</div>">` | **geladen, ohne Hinweis** — siehe M-11 |

**Befunde:**

- **I-1 — geschlossen.** Alle drei belegten Mechanismen (offener Kommentar, verbliebener Rawtext-Oeffner inkl.
  `</style x>`, ungleiche div-Zahl) fuehren jetzt zu Ersatz-Kopf + Hinweis (`BriefkopfVorlageService.cs:111-113, 118-119,
  126-131`); je Fall ein Test (`Load_UnbalancedMarkup_IsErsatz_WithHint`), Negativfall TS-82.15 in `docs/TESTSZENARIEN.md`.
  Legitime Vorlagen (Muster, div im Kommentar/head, title im head, Fragmente) laden weiter. Die Kommentar-Entfernung
  laeuft jetzt **vor** `Sanitize` — damit verschwindet auch der `<script>`-Text im Muster-Kommentar vor allen Regeln.
- **M-2 — geschlossen.** Attribut-Regeln nur noch in `<[a-z][^>]*>` (`:153-159`); `<p>Kontakt online = 24/7</p>`
  bleibt unveraendert, `onclick` im Tag wird weiter entfernt (Sonde + Test).
- **M-3 — geschlossen.** `Charge` zaehlt jede Verwendung gegen 8 MB Data-URI-Zeichen (`:244-250`), auch Cache-Treffer
  (`:221`). Sonde 20 × 900-KB-Bild ⇒ 6 eingebettet (7,2 MB Markup), 14 Hinweise „zu oft verwendet"; 5 × Kleinlogo ⇒ 5
  eingebettet, 0 Hinweise. Rest-Grenze: die Ausgabe wird je `.fa-group` wiederholt (bekannt, `_PrintBriefkopf.cshtml:17-18`).
- **M-4 — geschlossen.** `Trim().Trim('"').Trim()` (`:88`); Sonde `  "C:\…\briefkopf.html" ` ⇒ geladen; Test vorhanden.
- **M-1** — nicht gebaut, als [[2026-10-09-briefkopf-csp-und-bereinigung]] festgehalten. Akzeptiert.
- **M-10** — `secondbrain/feature-map.md` weiter ohne Eintrag v1.49.0/Briefkopf (Grep `briefkopf|1\.49` ohne Treffer).
  Brain-Nachtrag, nicht zweiggebunden, blockiert nicht.
- **M-11 (neu, Minor) — Restgrenze der Markup-Pruefung.** Die Pruefung deckt Kommentar, Rawtext und div-**Anzahl** ab,
  nicht Reihenfolge und andere Elemente: `</div><div>a` (gleiche Anzahl, falsche Reihenfolge) schliesst die `.fa-group`
  trotzdem; ein offenes `<table>` zieht im Browser den Rest der Seite in die Tabelle, ein offenes `<a>`/`<b>` macht ihn
  zum Link/fett — alles ohne Hinweis. Bewertung Minor: die IT pflegt laut Muster div/span/img; die Faelle verlangen einen
  anderen Tippfehler als die belegten. Billigster naechster Schritt (falls gewuenscht): div-Tiefe nie < 0 pruefen (eine
  Schleife) und `table|a|b|strong|span|p` mitzaehlen; vollstaendig nur mit Parser (Upgrade-Pfad HtmlSanitizer/AngleSharp,
  heute keine Abhaengigkeit). Vorschlag: in [[2026-10-09-briefkopf-csp-und-bereinigung]] mit aufnehmen.

**TS-82.3:** Kandidaten-Abfrage auf `dbo.FaHierarchyNodes (HauptFA, Beschichtet)` — Spalten belegt
(`Models/FaHierarchyNode.cs:19, 56`, `SQL/00_FreshInstall.sql:1792`), Filter `Beschichtet == true` wie im Druckweg
(`FaHierarchyBeschichtungController.cs:19`). Luecke aus Abschnitt 6 damit geschlossen; I-1-Negativfall = TS-82.15.

**Deploy:** unveraendert gueltig — `020bf8af` aendert nur `IdealAkeWms/`, Tests und `docs/` (kein `*/Migrations/`, kein
`SQL/`, kein Service). Publish aus dem Worktree mit Stand `020bf8af`.

**Manuelle Checkliste — Ergaenzung zu Punkt 15:** TS-82.15 (a) `<!-- offen`, (b) `<style>` ohne Ende, (c) ueberzaehliges
`</div>`, (d) `<div>` ohne Gegenstueck — je Ersatz-Kopf + gelber Hinweis mit Grund, Kopftabelle/Positionen und alle
weiteren HauptFA vollstaendig, keine Leerseite; danach Vorlage wiederherstellen und pruefen, dass das Muster wieder laedt.

**Status:** `Testbereit` (Schranke 2: Manual-UAT TS-82 + Merge durch den Menschen).
- Entscheidung Gerald 2026-10-12 zur Auslegung von Antwort 8: bleibt wie umgesetzt – nur eine zu grosse Vorlage fuehrt zum Ersatz-Kopf; zu grosse Bilder/Schriften werden weggelassen (Hinweis + Log). Keine Code-Aenderung.
