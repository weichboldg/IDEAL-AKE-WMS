---
type: changelog
version: 1.39.0
date: 2026-09-14
---
# v1.39.0 — IDEAL: PDF-Erzeugung fuer FaHierarchy-Druckdokumente (Querschnitts-Baustein)

Umsetzung der freigegebenen Spec [[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]] im
**Buendel-**Worktree `feature/2026-08-07-ideal-teile-1-5`. Umsetzungsnotiz
[[2026-08-06-pdf-erzeugung-fahierarchy-druck-umsetzung]], Plan
`docs/superpowers/plans/2026-09-14-pdf-erzeugung-fahierarchy-druck.md` (7 Tasks, SDD).

**Warum:** Die IDEAL-Druckdokumente (Kommissionierliste Teil 3, Beschichtungsauftrag Teil 4) liessen
sich bisher nur ueber den Browser-Druckdialog ausgeben. Ein Beschichtungsauftrag muss aber an einen
externen Dienstleister **mailbar** und ein Beleg **ablegbar** sein — beides braucht eine PDF-**Datei**.
Herausgeloest aus Teil 4 (Befund B-3), weil Teil 3/4/(spaeter)5 sich dasselbe Druckgeruest teilen.

## Umgesetzt

- **Renderweg Headless Edge** (Freigabe: kein QuestPDF/pdfme): `IPdfRenderService` (Singleton) nimmt
  HTML, liefert `byte[]` — kein Dateiname, kein HTTP-Kontext. `IEdgeProcessRunner` kapselt den
  `msedge --headless --print-to-pdf`-Aufruf (einzige nicht InMemory-testbare Grenze, echter
  Smoke-Test). `RazorViewRenderer` rendert **dieselbe** `Print.cshtml` zu einem String, damit
  Bildschirmdruck und PDF nie auseinanderlaufen. `PdfFileNameBuilder` liefert
  `<Dokumentart>_<HauptFA>_<yyyyMMdd-HHmm>.pdf`.
- **Ein PDF je HauptFA-Gruppe**: neue Action `Pdf(int hauptFa, …)` an beiden Verbraucher-Controllern
  rendert genau eine gefilterte Gruppe und liefert einen Direkt-Download. `hauptFa` als Routen-Segment,
  `target`/`colf_*` bleiben im Query — das PDF bildet exakt die gefilterte Sicht ab (Klarstellung zu
  Freigabe-Antwort 7). Knopf „PDF" (Icon **und** Text) je Gruppen-Kopfzeile, **kein** `target="_blank"`.
- **Start-Probe statt Scheitern beim Klick** (Freigabe-Antwort 2, Vorbild `HierarchischeStrukturStatus`):
  `PdfRenderStatus` (Singleton) sucht `msedge.exe` einmal beim Start (Config → Registry App Paths →
  Standardpfade → PATH). Fehlt Edge, erscheint der PDF-Knopf gar nicht und das Log traegt eine Warnung.
- **Robustheit**: `SemaphoreSlim` (Default 2, **anfrageuebergreifend** dank Singleton, festgenagelt durch
  `PdfRenderServiceDiResolutionTests`) — warten statt abweisen. GUID-Laufverzeichnis je Aufruf,
  `finally`-Loeschen **plus** Aufraeumen verwaister Laeufe (App-Pool-Recycle). Timeout 30 s
  (konfigurierbar), Laufzeit protokolliert. Filter-Hinweis „Gefilterte Ansicht" im Druck-/PDF-Kopf,
  sobald ein Filter aktiv ist (Teilmenge sieht nicht wie das Ganze aus).

## Gemessene Spec-Abweichungen (vor dem Bau am Dev-System gemessen)

- **`msedge.exe` ist ein Launcher**: der Prozess endet nach ~250 ms mit Exit 0, das PDF schreiben
  Kindprozesse ~1,5 s spaeter. Der Runner wartet deshalb auf die **fertige Datei** (`%%EOF`), nicht auf
  den Prozess; Timeout-Kill ueber das Startzeit-Fenster (Kinder sind Waisen). Ein eigenes
  `--user-data-dir` je Lauf ist Pflicht. Dauerwissen in [[fallstricke]] §11.

## Nicht enthalten (bewusst)

- Ablage auf Dateifreigabe, enaio-Archivierung, automatischer Mailversand, Sammel-PDF ueber mehrere
  HauptFA — alle laut Backlog spaeter.
- **Vormontage (Teil 5)** hat noch keine Bildschirmdruck-Ansicht → eigener Backlog-Punkt
  [[2026-09-14-vormontage-druckansicht]] (offene Vorfrage: `Index` oder `Summiert` drucken?).

## Deploy

- **Web: ja** (neue Services/Actions/Views/`appsettings.json`-Sektion `PdfRender`). Service: nein.
  Migration: nein.
- **Betriebs-Vorbedingung**: Microsoft Edge auf dem Web-Server, ausfuehrbar unter der App-Pool-Identitaet;
  `%TEMP%\IdealAkeWms-Pdf` beschreibbar. In die Buendel-Deploy-Notiz
  [[2026-08-07-ideal-teile-1-5]] gezogen (Freigabe-Antwort 1).
- **Merge-Commit:** offen (Schranke 2, Mensch).

## Nachweis

Build 0 Fehler; `EdgeProcessRunnerSmokeTests` erzeugt am Dev-System ein echtes PDF (~15,7 KB). Genaue
Testzahlen im Spec-QA-Abschnitt (qa-agent). Testszenarien **TS-74.1–74.14**
([[testszenarien-index]] → `docs/TESTSZENARIEN.md`).

## Zugehoerig

Spec [[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]] · Umsetzung
[[2026-08-06-pdf-erzeugung-fahierarchy-druck-umsetzung]] · [[fallstricke]] §11 ·
services-Karte [[services]] · Buendel [[2026-08-07-ideal-teile-1-5]].
