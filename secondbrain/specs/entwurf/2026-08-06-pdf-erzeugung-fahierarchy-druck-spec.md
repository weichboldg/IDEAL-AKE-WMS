---
type: spec
title: "PDF-Erzeugung fuer FaHierarchy-Druckdokumente (Querschnitts-Baustein)"
slug: 2026-08-06-pdf-erzeugung-fahierarchy-druck-spec
status: Entwurf
created: 2026-09-10
updated: 2026-09-10
source_backlog: "[[2026-08-06-pdf-erzeugung-fahierarchy-druck]]"
depends_on: "[[2026-07-29-standort-ideal-teil-3-spec]], [[2026-07-29-standort-ideal-teil-4-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Services/PdfRenderService.cs (neu — IPdfRenderService, RenderHtmlToPdfAsync(html) -> byte[]; Prozessaufruf msedge --headless --print-to-pdf; Temp-Schreiben/-Aufraeumen/-Timeout gekapselt)
  - IdealAkeWms/Services/RazorViewRenderer.cs (neu — IRazorViewRenderer, rendert eine Razor-View inkl. Model zu einem HTML-String ausserhalb der normalen Controller-Response; noetig, damit dieselbe Print.cshtml sowohl an den Browser (Print-Action) als auch an den PdfRenderService (Pdf-Action) gehen kann)
  - IdealAkeWms/Services/PdfFileNameBuilder.cs (neu — reine Funktion Build(documentArt, hauptFa, timestamp) -> "<Dokumentart>_<HauptFA>_<yyyyMMdd-HHmm>.pdf", unit-testbar)
  - IdealAkeWms/Controllers/FaHierarchyKommissionierListenController.cs (erweitert — neue Action Pdf(int hauptFa, string? target), rendert Print.cshtml auf genau eine Gruppe, liefert File-Download)
  - IdealAkeWms/Controllers/FaHierarchyBeschichtungController.cs (erweitert — neue Action Pdf(int hauptFa), analog)
  - IdealAkeWms/Views/FaHierarchyKommissionierListen/Index.cshtml (erweitert — PDF-Knopf an jeder HauptFA-Gruppenkopfzeile, zusaetzlich zum bestehenden seitenweiten "Drucken"-Knopf)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/Index.cshtml (erweitert — PDF-Knopf analog)
  - IdealAkeWms/Program.cs (DI-Registrierung IPdfRenderService, IRazorViewRenderer)
  - IdealAkeWms/appsettings.json (neue Konfiguration fuer Edge-Pfad/Timeout/Temp-Verzeichnis — Abschnitt/Keys laut offener Rueckfrage)
  - README.md (neuer Konfigurationsabschnitt fuer die PDF-Erzeugung, analog AppSettings-Tabelle)
  - secondbrain/codebase/services.md (Eintrag fuer PdfRenderService/RazorViewRenderer/PdfFileNameBuilder)
  - docs/TESTSZENARIEN.md (neues Kapitel, naechste freie Nummer zum Umsetzungszeitpunkt)
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Worktree-Strategie: weiterbauen in feature/2026-08-07-ideal-teile-1-5 vor dessen Merge, oder neuer Worktree nach dessen Merge?"
  - "Edge-Verfuegbarkeit: fester Pfad vs. Registry-/PATH-Ermittlung; Verhalten bei Server Core ohne Edge."
  - "Timeout-Wert (Sekunden) fuer den msedge-Prozessaufruf."
  - "Fehlerverhalten wenn Edge fehlt, abstuerzt oder den Timeout ueberschreitet — welche Nutzermeldung, welches Logging?"
  - "Temp-Verzeichnis: konkreter Pfad + Konfigurierbarkeit; Cleanup-Strategie fuer verwaiste Dateien."
  - "Parallelitaets-/Concurrency-Verhalten: unbegrenzt parallele msedge-Prozesse oder begrenzt (Semaphore/Queue)?"
  - "Konkrete UI-Umsetzung des Knopfs an der Gruppe (Platzierung, Label, Route/Parameter)."
  - "Zugriffsschutz: reicht der bestehende Class-Level-Access-Filter je Controller, oder braucht der PDF-Download einen eigenen Filter?"
  - "Vormontage (Teil 5) hat aktuell keine Print-Action/-View — wird sie im Rahmen dieser Spec neu gebaut, oder bleibt sie vorerst aussen vor?"
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

Die IDEAL-Druckdokumente (Kommissionierliste, Beschichtungsauftrag — Teil 3/4, spaeter ggf.
Vormontage — Teil 5) lassen sich heute nur ueber den Browser-Druckdialog ausgeben. Ein
Beschichtungsauftrag muss aber an einen externen Dienstleister **mailbar** sein und ein Beleg muss
sich **ablegen** lassen — beides setzt eine PDF-**Datei** voraus, kein Ausdruck auf Papier. Diese
Spec liefert den dafuer noetigen Baustein: einen Dienst, der aus dem bereits vorhandenen
Print-HTML einen PDF-Bytestrom erzeugt, plus die Anbindung an einen Download-Knopf je HauptFA.

Diese Spec ist bewusst **Infrastruktur, kein Merkmal eines einzelnen Dokuments** — sie wurde aus
[[2026-07-29-standort-ideal-teil-4-spec]] (Befund B-3) herausgeloest, weil Teil 3, 4 und (spaeter)
5 sich dasselbe Druckgeruest teilen. Waere sie in Teil 4 mitgebaut worden, haette Teil 5 sie
danach erneut anfassen muessen.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:**
- Ein generischer Dienst `IPdfRenderService`, der aus einem HTML-String per Headless-Edge-
  Prozessaufruf einen PDF-Bytestrom (`byte[]`) erzeugt — ohne Kenntnis davon, woher das HTML kommt
  oder was mit dem Ergebnis geschieht (siehe Schnitt unten).
- Ein Hilfsdienst `IRazorViewRenderer`, der eine bestehende Razor-View (z. B.
  `Views/FaHierarchyKommissionierListen/Print.cshtml`) mit einem Model zu einem HTML-String
  rendert, OHNE dass dafuer ein zweites Layout entsteht — **dasselbe** Markup wie der
  Bildschirmdruck.
- Ein reiner Namens-Baustein `PdfFileNameBuilder` fuer das Schema
  `<Dokumentart>_<HauptFA>_<yyyyMMdd-HHmm>.pdf`.
- Anbindung an **Teil 3 (Kommissionierliste)** und **Teil 4 (Beschichtungsauftrag)**: je eine neue
  `Pdf(int hauptFa, …)`-Action, die dieselbe `Print.cshtml` auf genau **eine** gefilterte
  HauptFA-Gruppe rendert und als Datei-Download ausliefert. Ein Knopf an der jeweiligen
  HauptFA-Gruppenzeile in `Index.cshtml`, zusaetzlich zum bestehenden seitenweiten
  "Drucken"-Knopf (der unveraendert alle gefilterten Gruppen als Bildschirmdruck zeigt).
- Konfigurierbarkeit von Edge-Pfad, Prozess-Timeout und Temp-Verzeichnis (Details: offene
  Rueckfragen).

**Out-of-Scope (laut Backlog bewusst spaeter):**
- Ablage der PDF-Datei auf einer Dateifreigabe.
- Archivierung in enaio.
- Automatischer Mailversand an den Dienstleister.
- Ein Sammeldokument ueber mehrere HauptFA hinweg — es gibt immer genau ein PDF je HauptFA.
- QuestPDF fuer ein eigenstaendiges, formelles Beschichtungsauftrags-Layout — siehe Randnotiz am
  Ende dieses Abschnitts.
- **Vormontage (Teil 5):** hat aktuell **keine** Bildschirmdruck-Action/-View (nur `Index` und
  `Summiert`), anders als der Bezugsabschnitt des Backlogs suggeriert. Ob diese Spec fuer Teil 5
  eine Print.cshtml + PDF-Knopf neu mitbaut, ist offene Rueckfrage 9.

**Randnotiz (kein Umfang, nur Vermerk):** Sollte sich bei der noch ausstehenden
Corporate-Design-Vorlage fuer den Beschichtungsauftrag zeigen, dass ein formelles Aussendokument
mit Print-CSS nicht sauber zu setzen ist (Briefkopf, Seitenzaehlung, wiederholte Tabellenkoepfe,
praezise Positionierung), waere DAS der Moment, QuestPDF fuer **dieses eine** Dokument zu erwaegen
— mit bekanntem Bedarf statt auf Vorrat. Diese Spec nimmt das nicht vorweg.

## Fachliche Anforderungen

1. Ein Anwender mit Zugriff auf die Kommissionierlisten- bzw. Beschichtungsauftrags-Liste kann an
   jeder HauptFA-Gruppe (nicht nur seitenweit) auf Knopfdruck **genau ein PDF fuer diese Gruppe**
   herunterladen.
2. Das PDF entspricht inhaltlich und optisch dem Bildschirmdruck derselben Gruppe — Kopfdaten-
   Tabelle, Positionstabelle, Barcode, Layout. Keine zweite, gepflegte Fassung des Layouts.
3. Der Dateiname folgt dem Schema `<Dokumentart>_<HauptFA>_<yyyyMMdd-HHmm>.pdf`
   (`Kommissionierliste_104857_20260906-1430.pdf`, `Beschichtungsauftrag_104857_20260906-1430.pdf`)
   — keine Umlaute, keine Sonderzeichen, keine Klarnamen von Dienstleister oder Kunde.
4. Das PDF wird als reiner Download ausgeliefert (Content-Disposition attachment). Es entsteht
   keine dauerhafte Serverkopie; erzeugte Temp-Dateien werden nach jedem Aufruf entfernt.
5. Der bestehende Bildschirmdruck (seitenweiter "Drucken"-Knopf, alle gefilterten Gruppen) bleibt
   unveraendert bestehen — der neue PDF-Weg ist eine Ergaenzung, kein Ersatz.
6. Zugriffsschutz und Feature-Toggle des jeweiligen Moduls gelten fuer den PDF-Download
   unveraendert mit (Beschichtungsauftrag: Rolle `beschichtungsauftrag` + Toggle
   `FaHierarchyBeschichtungAktiv`; Kommissionierliste: `RequireLagerProcessingAccess` + Toggle
   `FaHierarchyKommissionierlistenAktiv`).

## Technischer Loesungsentwurf

### Kernentscheidung (laut Backlog vorgegeben, hier nur uebernommen)

**Renderweg: Headless Edge/Chrome per Prozessaufruf**, nicht QuestPDF, nicht wkhtmltopdf, nicht
PuppeteerSharp, nicht pdfme. Begruendung (aus dem Backlog, nach zweifacher vertiefter Pruefung
2026-09-10 bestaetigt): Aufwand ist das massgebliche Kriterium, Layoutaenderungen sind selten, das
Print-HTML existiert fuer alle Dokumente bereits, und die Vorbereitung dafuer (CSS eingebettet,
Bilder als Data-URI) ist in Teil 3/4 bereits erfolgt (siehe Beleg unten). QuestPDF haette eine
kommerzielle Lizenz (IDEAL-AKE vermutlich ueber der 1-Mio.-USD-Freigrenze) UND eine Neuimplementierung
aller drei Layouts in C# zur Folge — beides unnoetiger Aufwand, solange dasselbe Dokument gemeint
ist. pdfme faellt wegen Oekosystembruch (Node auf dem Server ODER Browser-Erzeugung, die dem
Schnitt unten widerspricht) aus. Diese Abwaegung ist **nicht** Gegenstand der offenen Rueckfragen.

```
msedge.exe --headless --disable-gpu --print-to-pdf="<ziel>.pdf" --no-pdf-header-footer "<quelle>.html"
```

### Beleg der Headless-Renderer-Vorbereitung (im Code verifiziert, nicht nur behauptet)

`Views/FaHierarchyKommissionierListen/Print.cshtml` und
`Views/FaHierarchyBeschichtung/Print.cshtml` (Worktree `feature/2026-08-07-ideal-teile-1-5`,
Teil 3/4 noch nicht in main gemergt) haben CSS vollstaendig in einem `<style>`-Block im `<head>`,
keinen externen `<link rel="stylesheet">`. Der Barcode ist als `<img src="data:image/png;base64,…">`
eingebettet. `Print.cshtml` (Beschichtung) traegt sogar wortwoertlich den Kommentar:
„CSS bewusst eingebettet (kein externer Verweis), damit dieses Markup spaeter unveraendert von der
separaten PDF-Erzeugungs-Spec wiederverwendet werden kann." Headless Edge folgt keinen
authentifizierten Links und laedt nichts vom Webserver nach — genau dieser Stolperstein ist damit
bereits ausgeraeumt, sofern kuenftige Aenderungen an diesen Views dasselbe Prinzip beibehalten
(Logo etc. immer als Data-URI, nie als `<img src="/images/...">`).

### Schnitt: Erzeugung und Auslieferung trennen

```
IPdfRenderService.RenderHtmlToPdfAsync(string html, CancellationToken ct = default) : Task<byte[]>
```

Dieser Dienst kennt **nur** HTML rein, PDF-Bytes raus — keinen Dateinamen, keinen HTTP-Kontext,
kein Zielsystem. Intern:
1. Eindeutiges Temp-Verzeichnis/-Dateipaar per GUID anlegen (kollisionsfrei bei Parallelaufrufen).
2. HTML-Datei schreiben.
3. `msedge.exe` als `Process` starten (Muster wie `PrintService.PrintFileAsync`:
   `UseShellExecute = false`, `RedirectStandardOutput/Error`, `CreateNoWindow = true`), mit
   Timeout ueber eine `CancellationTokenSource` — bei Ablauf `process.Kill(entireProcessTree: true)`.
4. Exit-Code **und** Existenz/Groesse der PDF-Datei pruefen (Edge liefert nicht in jedem Fehlerfall
   einen Exit-Code ungleich 0).
5. PDF-Bytes lesen.
6. `finally`: beide Temp-Dateien loeschen — auch bei Timeout oder Exception.
7. Bei Fehlern eine eigene `PdfRenderException` werfen, die der Aufrufer in eine
   `TempData["WarningMessage"]` uebersetzt (TempData kennt kein `ErrorMessage`, siehe CLAUDE.md).

**Bewusst kein neuer Zweck fuer `IPrintService`.** `PrintService`/`rundll32 mshtml.dll,PrintHTML`
ist der physische Drucker-Weg und aktuell im App-Code nirgends konsumiert (Verifiziert: kein
Controller ruft `IPrintService.PrintFileAsync` auf main oder im Worktree — nur DI-Registrierung).
`IPdfRenderService` ist ein **eigener** Dienst mit eigenem Zweck; beide Namen bewusst nicht
verwechseln.

### HTML-Erzeugung fuer genau eine Gruppe

Die bestehenden `Print()`-Actions rendern **alle** aktuell gefilterten HauptFA-Gruppen als ein
Bildschirmdokument (mehrere `page-break-after`-Bloecke). Fuer den PDF-Knopf an der Gruppe wird
**dieselbe** `Print.cshtml` mit einem auf eine Gruppe reduzierten ViewModel gerendert:

```csharp
// FaHierarchyKommissionierListenController
public async Task<IActionResult> Pdf(int hauptFa, string? target)
{
    var columnFilters = ColumnFilterHelper.ReadFromQuery(HttpContext?.Request);
    var result = await _service.BuildAsync(target, columnFilters, 1, int.MaxValue, paginate: false);
    var group = result.Groups.FirstOrDefault(g => g.HauptFA == hauptFa);
    if (group is null) return NotFound();

    var vm = new FaHierarchyKommissionierPrintViewModel
    {
        Groups = new[] { group },
        Barcodes = new() { [hauptFa] = _barcode.EncodeCode128Base64(hauptFa.ToString(CultureInfo.InvariantCulture)) },
        SelectedTarget = target
    };

    var html = await _viewRenderer.RenderToStringAsync(ControllerContext, "Print", vm);
    var pdfBytes = await _pdfRenderService.RenderHtmlToPdfAsync(html);
    var fileName = PdfFileNameBuilder.Build("Kommissionierliste", hauptFa, DateTime.Now);
    return File(pdfBytes, "application/pdf", fileName);
}
```

Analog fuer `FaHierarchyBeschichtungController.Pdf(int hauptFa)` mit Dokumentart
`Beschichtungsauftrag`. Dieselben Filter/Query-Parameter wie beim seitenweiten Druck werden
mitgegeben, damit das Einzel-PDF exakt die Sicht abbildet, aus der heraus der Anwender geklickt
hat.

`IRazorViewRenderer` ist ein kleiner, generischer Wrapper um `ICompositeViewEngine` +
`ITempDataProvider` (Standardmuster fuer "Razor-View zu String ausserhalb der Response"). Er
gehoert — wie `IPdfRenderService` und `PdfFileNameBuilder` — zum Querschnitts-Baustein und wird von
beiden Controllern injiziert, nicht dupliziert.

### UI

An der HauptFA-Gruppenkopfzeile in `Index.cshtml` (dort, wo heute `<strong>HauptFA
@group.HauptFA</strong>` steht) kommt ein zusaetzlicher, kleiner Link/Knopf "PDF" hinzu, der
`Url.Action("Pdf", new { hauptFa = group.HauptFA, target = Model.SelectedTarget })` (bzw. ohne
`target` bei Beschichtung) aufruft. Konkrete Platzierung/Beschriftung: offene Rueckfrage 7.

### Konfiguration

Edge-Pfad, Prozess-Timeout und Temp-Verzeichnis sind **Web-App-Konfiguration**
(`appsettings.json`/`IConfiguration`), nicht `AppSettings` (fachliche Feature-Toggles der DB) und
nicht `ServiceSettings` (der Windows-Service ist an dieser Funktion nicht beteiligt — die
PDF-Erzeugung laeuft synchron im Web-Request). Analog zu `PrintService`, das seinen
Werkzeug-Pfad (`rundll32.exe`) ebenfalls hart im Code haelt, koennte ein Minimalansatz auch ohne
Konfiguration auskommen (Standardpfad + `Path.GetTempPath()`); ob das ausreicht oder eine
`appsettings.json`-Sektion noetig ist, haengt an offenen Rueckfragen 2/3/5.

### Nicht InMemory-testbar — wie bei anderen externen Prozessen im Projekt

Der eigentliche `msedge`-Aufruf ist wie LDAP/Negotiate (siehe `fallstricke.md`, Abschnitt 4) nicht
sinnvoll unit-testbar — er braucht einen echten Edge-Prozess auf einem echten Dateisystem. Damit
trotzdem etwas automatisiert bleibt, wird empfohlen, den eigentlichen Prozessaufruf hinter einer
duennen, mockbaren Grenze zu kapseln (z. B. `IEdgeProcessRunner.RunAsync(htmlPath, pdfPath, timeout)`
gibt `(exitCode, stderr)` zurueck), sodass `PdfRenderService` (Timeout-Handling, Cleanup-Logik,
Fehlerpfade) unit-testbar bleibt, waehrend nur der Runner selbst Manual-UAT bleibt.
`PdfFileNameBuilder` ist eine reine Funktion und vollstaendig unit-testbar.

## Migrations-/SQL-Auswirkungen

Keine. Es entstehen keine neuen Tabellen/Spalten, keine EF-Migration, kein `SQL/XX_*.sql`, keine
Aenderung an `SQL/00_FreshInstall.sql`. Die Funktion ist rein lesend/rendernd auf bereits
vorhandenen `FaHierarchyNode`/`FaHierarchyOrderInfo`-Daten (Teil 1/3/4).

## Audit-Feld-Auswirkungen

Keine. Es wird keine fachliche Entitaet angelegt oder veraendert (`AuditableEntity` betrifft nur
persistente Schreibpfade); die PDF-Erzeugung ist ein reiner Lese-/Rendering-Vorgang ohne
Datenbank-Schreibzugriff.

## Akzeptanzkriterien

1. Ein Klick auf den PDF-Knopf einer HauptFA-Gruppe in der Kommissionierlisten-Index-Ansicht
   (Teil 3) liefert einen Download `Kommissionierliste_<HauptFA>_<yyyyMMdd-HHmm>.pdf`, der genau
   die Positionen dieser einen Gruppe enthaelt — keine anderen HauptFA-Gruppen.
2. Derselbe Ablauf fuer den Beschichtungsauftrag (Teil 4) liefert
   `Beschichtungsauftrag_<HauptFA>_<yyyyMMdd-HHmm>.pdf` mit identischem Kopf-/Positionsinhalt wie
   der Bildschirmdruck derselben Gruppe.
3. Das erzeugte PDF zeigt alle im Bildschirmdruck sichtbaren Elemente korrekt (Kopfdaten-Tabelle,
   Positionstabelle, Barcode als Bild) — kein gebrochenes Bild-Icon, kein ungestyltes HTML, keine
   fehlenden Ressourcen.
4. `IPdfRenderService.RenderHtmlToPdfAsync` nimmt ausschliesslich einen HTML-String entgegen und
   liefert ausschliesslich `byte[]` zurueck — ohne Kenntnis von Dateiname, HTTP-Response oder
   Zielsystem (Code-Review-Kriterium: keine Abhaengigkeit auf `HttpContext`/`IActionResult` in
   `PdfRenderService.cs`).
5. Nach jedem Aufruf (Erfolg, Fehler oder Timeout) bleiben keine Temp-Dateien (HTML/PDF) im
   konfigurierten Temp-Verzeichnis zurueck.
6. Ist `msedge.exe` am ermittelten Pfad nicht vorhanden, bricht die Aktion kontrolliert ab (kein
   HTTP 500, keine unbehandelte Exception) und der Anwender erhaelt eine verstaendliche Meldung.
7. Ueberschreitet der msedge-Prozess den konfigurierten Timeout, wird er beendet (kein
   Zombie-Prozess in der Prozessliste danach) und die Aktion bricht kontrolliert mit Fehlermeldung
   ab.
8. Zwei gleichzeitige PDF-Anfragen (zwei Browser-Tabs/Anwender) erzeugen unabhaengige,
   GUID-benannte Temp-Dateien und liefern beide ihr jeweils korrektes PDF, ohne sich gegenseitig
   zu ueberschreiben.
9. Der PDF-Download ist nur fuer Anwender mit dem jeweils bestehenden Zugriffsrecht der Liste
   erreichbar (Beschichtungsauftrag: Rolle `beschichtungsauftrag`; Kommissionierliste:
   `RequireLagerProcessingAccess`) — ein Anwender ohne diese Rolle erhaelt denselben
   Zugriffs-Fehler wie bei der bestehenden `Index`-/`Print`-Action.
10. Der Dateiname enthaelt keine Umlaute, keine Sonderzeichen und keine Klarnamen von
    Dienstleister oder Kunde — nur Dokumentart (ASCII), HauptFA (numerisch) und Zeitstempel.
11. `PdfFileNameBuilder.Build(...)` ist unit-getestet (Grenzfaelle: verschiedene Dokumentarten,
    HauptFA-Werte, Zeitstempel-Formatierung).
12. Der bestehende seitenweite "Drucken"-Knopf (Bildschirmdruck aller gefilterten Gruppen) bleibt
    unveraendert funktionsfaehig — Regressionscheck.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (naechste freie Nummer zum Umsetzungszeitpunkt — Stand
2026-09-10 ist im Worktree `feature/2026-08-07-ideal-teile-1-5` TS-71 zuletzt vergeben und TS-72
bereits fuer die tbody/CSS-Sortierungskorrektur reserviert, vermutlich also TS-73; im Dev-Lauf
gegen den dann aktuellen Stand pruefen). Skizze der Szenarien:

- **TS-x.1 Happy Path Kommissionierliste:** HauptFA mit mehreren Positionen filtern, PDF-Knopf an
  der Gruppe klicken, Datei oeffnen, Inhalt mit Bildschirmdruck derselben Gruppe vergleichen
  (Positionen, Kopf, Barcode lesbar).
- **TS-x.2 Happy Path Beschichtungsauftrag:** analog, inkl. Kombigeraet-Fall (mehrere
  Kopfvarianten) — PDF zeigt denselben mehrdeutig-Hinweis wie der Bildschirmdruck.
- **TS-x.3 Dateiname:** heruntergeladene Datei traegt exakt
  `<Dokumentart>_<HauptFA>_<yyyyMMdd-HHmm>.pdf`, keine Umlaute/Sonderzeichen.
- **TS-x.4 Temp-Aufraeumen:** vor und nach mehreren PDF-Erzeugungen das Temp-Verzeichnis pruefen —
  keine liegen gebliebenen Dateien.
- **TS-x.5 Parallelaufruf:** zwei Browser-Tabs gleichzeitig PDF fuer zwei verschiedene HauptFA
  anfordern — beide korrekt, keine Vermischung.
- **TS-x.6 Edge fehlt/Timeout (Manual-UAT, ggf. simuliert durch falschen Pfad/kuenstliche
  Verzoegerung):** kontrollierte Fehlermeldung statt 500/Absturz.
- **TS-x.7 Zugriffsschutz:** Anwender ohne die jeweilige Rolle erhaelt beim direkten Aufruf der
  `Pdf`-URL denselben Zugriffs-Fehler wie bei der `Index`-Action.
- **TS-x.8 Regression Bildschirmdruck:** bestehender seitenweiter "Drucken"-Knopf unveraendert
  funktionsfaehig, unabhaengig vom neuen PDF-Knopf.

Wie bei den IDEAL-Teilen 1–5 gilt: der eigentliche Edge-Prozessaufruf ist nur am echten System
verifizierbar (vergleichbar mit den Manual-UAT-Kapiteln 40/49/50/53/55/56 — externe
Werkzeuge/Fremdsysteme, siehe `secondbrain/tests/testszenarien-index.md`, Abschnitt „Kapitel mit
besonderem Gewicht").

## Deploy

**Vorlaeufig (Dev-Lauf bestaetigt gegen den echten Diff):**

- **Web-App:** ja — neue Services, Controller-Actions, Views, ggf. `appsettings.json`-Erweiterung.
- **Service:** nein — der Windows-Service ist an dieser Funktion nicht beteiligt.
- **Migration:** nein.
- **Voraussetzung auf dem Zielserver:** Microsoft Edge muss vorhanden und ausfuehrbar sein (siehe
  offene Rueckfrage 2) — vor dem ersten Produktiv-Deploy auf dem IIS-Server pruefen, insbesondere
  falls dort Server Core oder eine gehaertete Variante laeuft.
- **Publish-Befehle** (im Worktree, nach Abschluss der Umsetzung):
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  ```

## Offene Rueckfragen

1. Diese Spec baut auf Controllern/Views auf, die aktuell **nur** im noch nicht gemergten
   Worktree `feature/2026-08-07-ideal-teile-1-5` (Teile 1–5, Status Testbereit, wartet auf
   Schranke 2) existieren — auf `main` gibt es weder `FaHierarchyKommissionierListenController`
   noch `FaHierarchyBeschichtungController`. Soll die Umsetzung als weitere Etappe in **genau
   diesem** Worktree erfolgen (vor dessen Merge), oder erst **nach** dessen Merge in main als
   neuer, eigener Worktree?
2. Edge-Verfuegbarkeit auf dem Zielserver (Web-IIS): fester, konfigurierter Pfad zu `msedge.exe`
   oder automatische Ermittlung (Registry/PATH)? Was passiert, wenn der Server (z. B. Server Core)
   kein Edge hat — Feature dauerhaft deaktivieren, oder erst beim ersten Aufruf scheitern?
3. Timeout-Wert (in Sekunden) fuer den msedge-Prozessaufruf — welcher Wert ist fuer die
   erwartete Dokumentgroesse (mehrseitige Stuecklisten) angemessen?
4. Fehlerverhalten im Detail, wenn Edge fehlt, abstuerzt oder den Timeout ueberschreitet: welche
   Nutzermeldung (Text), welches Logging (`ILogger` reicht vermutlich, da Web-seitige
   Lesefunktion ohne Hintergrund-Sync — analog zur Begruendung in Teil 4)?
5. Temp-Verzeichnis: konkreter Pfad (`Path.GetTempPath()`-Unterordner vs. konfigurierbarer Pfad in
   `appsettings.json`) und Cleanup-Strategie fuer verwaiste Dateien, falls ein Aufruf durch
   App-Pool-Recycle oder Serverneustart mitten im Vorgang abbricht (z. B. Aufraeumen aelterer
   Dateien beim naechsten Aufruf, oder eigener Cleanup-Mechanismus)?
6. Parallelitaets-/Concurrency-Verhalten: sollen mehrere gleichzeitige `msedge`-Prozesse
   unbegrenzt zugelassen werden, oder braucht es eine Begrenzung (z. B. `SemaphoreSlim`) gegen
   Ressourcenerschoepfung bei vielen gleichzeitigen Anfragen?
7. Wie soll der Knopf/die Action an der Gruppe konkret aussehen (Platzierung in der
   Gruppenkopfzeile, Beschriftung "PDF" vs. Icon, neuer Tab vs. direkter Download, Route
   `/Controller/Pdf/{hauptFa}` vs. Query-Parameter)?
8. Zugriffsschutz: reicht der bestehende Class-Level-Access-Filter des jeweiligen Controllers
   (`RequireLagerProcessingAccess`/`RequireBeschichtungsauftragAccess`) fuer den PDF-Download,
   oder soll es — wie bei manchen Schreib-Actions — einen eigenen, strengeren Filter geben?
9. Vormontage (Teil 5, `FaHierarchyVormontageController`) hat aktuell **keine** Print-Action/-View
   (nur `Index` und `Summiert`), obwohl der Backlog Teil 3/4/5 gemeinsam als Nutzer dieses
   Bausteins nennt. Wird im Rahmen dieser Spec fuer Vormontage eine `Print.cshtml` + PDF-Knopf neu
   gebaut (Scope-Erweiterung), oder bleibt Vormontage aussen vor, bis Teil 5 selbst einen
   Bildschirmdruck bekommt?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. →
2. →
3. →
4. →
5. →
6. →
7. →
8. →
9. →
