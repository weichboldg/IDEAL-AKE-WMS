---
type: spec
title: "Beschichtungsauftrag-Ausbau Teil 2 — Briefkopf / Corporate-Design-Druck"
slug: 2026-10-06-beschichtungsauftrag-ausbau-teil-2-spec
status: Entwurf
created: 2026-10-06
updated: 2026-10-07
source_backlog: "[[2026-10-06-beschichtungsauftrag-ausbau]]"
anhaenge:
  - "[[cd-auszug-druckdokumente]]"
  - "[[IDEAL_AKE_Logo_rgb.jpg]]"
  - "[[IDEAL_AKE_Logo_rgb_klein.jpg]]"
depends_on: ""
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Views/Shared/_PrintBriefkopf.cshtml (neu — EIN Partial fuer Logo, Absender, Empfaenger, Dokumenttitel, Datum, Fusstext; CSS eingebettet, Logo als Data-URI)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/Print.cshtml (Partial je .fa-group einbinden, vor dem group-head Zeilen 62-63; vorlaeufigen Titel ersetzen)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/PrintSummiert.cshtml (nur falls Teil 1 vorher gemergt ist — sonst bindet Teil 1 nachtraeglich ein)
  - IdealAkeWms/Models/AppSettingKeys.cs (neue Keys fuer Logo-Quelle und Fusstext laut Antwort 1; Firmenname/Firmenanschrift existieren bei :74-75)
  - IdealAkeWms/Models/Standort/StandortSettingsCatalog.cs (neue Felder in GroupFirmendaten, :37-38; neuer mehrzeiliger Feldtyp im Enum StandortFieldType :3 fuer Firmenanschrift, Antwort 5)
  - IdealAkeWms/Views/StandortEinstellungen/Index.cshtml (textarea-Zweig fuer den neuen Feldtyp; heute einzeiliges input bei :88, Antwort 5)
  - IdealAkeWms/Services/ (kleiner Helfer Logo-Datei → Data-URI, nur falls Antwort 1 eine Datei als Quelle waehlt)
  - IdealAkeWms.Tests/ (Helfer-Test: MIME aus Endung, fehlende Datei → kein Absturz)
  - README.md (AppSettings-Tabelle + Abschnitt Corporate Design)
  - IdealAkeWms/Views/Help/ + Views/Help/Changelog.cshtml + beide AppVersion.cs
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions:
  - "Anpassbare HTML-Vorlage (Notiz Punkt 2): vom Anwender in der Oberflaeche gepflegt (gespeicherte Vorlage) oder Datei, die die IT aendert? Woher kommt das Logo (Upload in den Standorteinstellungen)? Empfehlung: fester Partial + Einstellungswerte fuer Logo, Anschrift, Fusstext; Logo als Dateipfad-Einstellung (keine Migration), da AppSettings.Value auf 500 Zeichen begrenzt ist."
  - "CD-Schrift Nimbus Sans Novus (lizenzpflichtig): darf sie auf dem Webserver installiert bzw. als Webfont eingebettet werden (Headless Edge rendert das PDF dort)? Empfehlung: bis zur Klaerung serifenlose Systemschrift (heute Segoe UI/Arial, Print.cshtml:17)."
  - "Kontaktdaten laut CD-Adressblock (Telefon, E-Mail, Web) gibt es in den Standorteinstellungen nicht: in den Fusstext schreiben oder eigene Felder? Empfehlung: Fusstext, keine eigenen Felder."
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

> **Umsetzungsort (Hinweis, keine Entscheidung):** nach Schranke 2 des IDEAL-Buendels, aus main (neuer
> Worktree). TS-Nummer und Version: naechste freie zum Umsetzungszeitpunkt. Code-Belege beziehen sich auf
> den Buendel-Stand `.claude/worktrees/2026-08-07-ideal-teile-1-5` (v1.47.1).

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
gespeicherte Objekt aus Teil 3. Ist Teil 1 vorher gemergt, bindet Teil 2 zusaetzlich `PrintSummiert.cshtml`
ein; sonst holt Teil 1 das nach. Teil 4 nutzt den Partial fuer den Druck des gespeicherten Auftrags.

## Ziel / Nutzen (das Warum)

Der Beschichtungsauftrag ist das einzige Dokument, das das Haus verlaesst (er geht an den externen
Beschichter). Heute traegt er nur „Beschichtungsauftrag — HauptFA …", Barcode und Kopftabelle
(`Print.cshtml:62-113`) — keinen Absender, kein Logo, kein Empfaengerfeld. Teil 2 liefert einen
gemeinsamen Briefkopf-Baustein, den jede Druckansicht einbinden kann, und haelt Druck und PDF als
**dasselbe** Dokument (Headless Edge rendert dieselbe View, [[2026-08-06-pdf-erzeugung-fahierarchy-druck-spec]]).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- `Views/Shared/_PrintBriefkopf.cshtml`: Logo, Absender (Firmenname + Anschrift), Empfaenger, Dokumenttitel,
  Datum, Fusstext — gestaltet nach den CD-Vorgaben (Anforderung 8).
- Einbindung in den Beschichtungsauftrag-Druck (Einzelsicht; summierte Sicht falls Teil 1 schon da).
- Pflegbarkeit der Werte ueber die Standorteinstellungen (Feldliste `StandortSettingsCatalog`), Anschrift als
  mehrzeiliges Feld (Antwort 5).

**Out-of-Scope**
- Frei editierbares HTML in der Datenbank (siehe Rueckfrage 1).
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

1. **Absender** = `Firmenname` + `Firmenanschrift` aus AppSettings (`AppSettingKeys.cs:74-75`). Beide werden
   heute **geschrieben** (Standorteinstellungen, `StandortSettingsCatalog.cs:37-38`), aber von **keiner**
   Druckansicht gelesen — Grep `Firmenname|Firmenanschrift` in `*.cs`/`*.cshtml` trifft nur Katalog,
   Keys und Tests. Teil 2 ist der erste Leser. `Firmenanschrift` wird **mehrzeilig** gepflegt (Antwort 5:
   neuer Feldtyp im Katalog, Key bleibt); Zeilenumbrueche erscheinen im Druck als Zeilenumbrueche
   (HTML-kodiert, kein Roh-HTML aus der Einstellung).
2. **Logo** laut Antwort 1. Belegte Randbedingungen:
   - `AppSettings.Value` ist `nvarchar(500)`, Pflichtfeld (`ApplicationDbContext.cs:702`, `AppSetting.cs:13`) —
     ein Logo als Data-URI passt dort **nicht** hinein.
   - Das vorhandene `wwwroot/images/ideal-ake-logo.svg` (`_Layout.cshtml:12`, `:21`; `Login.cshtml:54`) ist
     das Gruppenlogo, nicht standortbezogen, und liegt im Publish-Output (wird bei jedem Publish
     ueberschrieben).
   - Im Druck/PDF muss das Logo als **Data-URI** eingebettet sein; der Renderer laedt keine externen Dateien
     (Kommentar `Print.cshtml:13-14`, Pflicht 1 im Briefkopf-Backlog).
   - **Logodatei** ist das CD-Logo aus dem Anhang (JPEG, MIME `image/jpeg`). Empfehlung fuer den Druck:
     `IDEAL_AKE_Logo_rgb_klein.jpg` — bei CD-Logohoehe 17,5 mm ergeben 339 px rund 490 dpi, also genug; die
     grosse Datei wuerde den Data-URI je HauptFA-Dokument vervielfachen (der Partial steht in jeder
     `.fa-group`, `Print.cshtml:52-54`). Wie die Datei zur Anwendung kommt (Pfad-Einstellung oder Upload),
     entscheidet Antwort 1 (offen).
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
7. Fehlt ein Wert (kein Logo konfiguriert, Datei nicht lesbar, Firmenname leer), druckt das Dokument
   trotzdem; der fehlende Teil wird in den `screen-controls` (nur Bildschirm, `Print.cshtml:39-42`, beim
   Druck ausgeblendet `:35`) als Hinweis gemeldet und ins `ILogger` geschrieben — nicht still weggelassen.
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
   - Schrift: laut Manual Nimbus Sans Novus; bis Rueckfrage 6 geklaert ist, serifenlose Systemschrift wie heute
     (`Print.cshtml:17`).
   - Adressblock: Firmenbezeichnung in der Hausfarbe, eine Adresse zusammen mit den Kontaktdetails; woher die
     Kontaktdetails kommen, klaert Rueckfrage 7.
9. **Briefkopf nur auf Seite 1** jedes HauptFA-Dokuments (Antwort 3). „Seite X von Y" ist nice to have: nur
   umsetzen, wenn es am PDF ohne grossen Aufwand traegt (Zeitbox im Dev-Lauf); sonst weglassen und als
   Aufgabe in `secondbrain/aufgaben/` festhalten.

## Technischer Loesungsentwurf

- `_PrintBriefkopf.cshtml` als Partial mit kleinem Model (Dokumenttitel, Empfaengertext, Mehrdeutig-Flag,
  Datum). Absender und Fusstext liest der Partial selbst per `@inject IAppSettingRepository` — dasselbe
  Muster wie `_Layout.cshtml:2` und `:59`. Kein neuer Service fuer Textwerte.
- Logo (falls Antwort 1 = Datei/Pfad): ein kleiner statischer Helfer liest die Datei, bestimmt den MIME-Typ
  aus der Endung (`svg`, `png`, `jpg`) und liefert `data:<mime>;base64,…` oder `null` bei Fehler. Kein
  Caching auf Vorrat; nur falls Messung es verlangt.
- **PDF-Pfad pruefen:** Die `Pdf`-Action rendert ueber `IRazorViewRenderer.RenderToStringAsync`
  (`FaHierarchyBeschichtungController.cs:127`). `@inject` im Partial muss dort ebenso aufloesen — im Dev-Lauf
  am erzeugten PDF nachweisen, nicht annehmen.
- CSS des Briefkopfs im Partial eingebettet (`<style>` im Partial oder im `<head>` der einbindenden View),
  kein `<link>`.
- Wiederholung ueber Seiten (Antwort 3): „nur erste Seite je Dokument" ist mit der heutigen Struktur
  geschenkt (jede `.fa-group` ist ein Dokument mit `page-break-after`, `Print.cshtml:22-23`) — der Partial
  steht einmal am Anfang jeder `.fa-group`. ~~„Jede Seite"~~ *(verworfen mit Antwort 3)*. „Seite X von Y"
  (nice to have) frueh am PDF pruefen — Seitenzaehler je `.fa-group` (nicht je Gesamtdruck) ist mit Print-CSS
  nicht trivial; traegt es nicht in der Zeitbox, weglassen und als Aufgabe festhalten — kein QuestPDF-Umbau
  dafuer.
- Standorteinstellungen: neue Felder in `GroupFirmendaten`; Allow-List-Prinzip des Katalogs bleibt
  (`StandortSettingsCatalog.cs:16-21`). Antwort 5: neuer mehrzeiliger Feldtyp im Enum `StandortFieldType`
  (`StandortSettingsCatalog.cs:3`) + textarea-Zweig in `Index.cshtml` neben `:88`; `Firmenanschrift` bekommt
  diesen Typ. Im Druck Zeilenumbrueche HTML-kodiert in `<br>` umsetzen.
- Zugriff unveraendert: Standorteinstellungen `[RequireAdminAccess]` (`StandortEinstellungenController.cs:20`);
  Druck unter den bestehenden Filtern des Beschichtungsauftrags.

## Migrations-/SQL-Auswirkungen

Bei der Empfehlung (Werte als AppSettings, Logo als Dateipfad) **keine**: AppSettings-Zeilen entstehen beim
ersten Speichern der Maske (`AppSettingKeys.cs:70-73`, kein Seed). Falls Antwort 1 einen Logo-**Upload in
die Datenbank** waehlt, braucht es eine neue Speicherung (Tabelle oder Spalte groesser 500 Zeichen) — dann
Migration dreifach nach ADR 0004 (Model → `dotnet ef migrations add` → `SQL/XX_*.sql` mit `OBJECT_ID`-Guard,
DDL in eigenem Batch → `__EFMigrationsHistory`-Insert getrennt → `SQL/00_FreshInstall.sql` an beiden Stellen),
Nummer = naechste freie in **beiden** Zweigen zum Umsetzungszeitpunkt (heute Buendel `SQL/93`, main `SQL/88`),
und `deploy.migration` auf `true`.

## Audit-Feld-Auswirkungen

Keine neue Entitaet. `AppSetting` ist keine `AuditableEntity`; die Standorteinstellungen schreiben ueber den
bestehenden `StandortSettingsWriter` (unveraendert). Bei einer DB-Logo-Variante (siehe oben): neue Entitaet
erbt `AuditableEntity` (ADR 0003), `ModifiedAt/ModifiedBy/ModifiedByWindows` aus `ICurrentUserService`.

## Akzeptanzkriterien

1. Der Beschichtungsauftrag-Druck zeigt je HauptFA-Dokument einen Briefkopf mit Logo, Firmenname,
   Anschrift, Empfaenger, Dokumenttitel und Datum.
2. Das per `Pdf`-Action erzeugte PDF enthaelt denselben Briefkopf inkl. Logo (kein gebrochenes Bild).
3. Firmenname/Anschrift/Fusstext aendern sich nach Speichern in den Standorteinstellungen ohne Deploy im
   naechsten Druck.
4. Fehlt der Dienstleister in Sage, zeigt das Empfaengerfeld sichtbar „Empfaenger fehlt — in Sage
   nachpflegen".
5. Bei Kombigeraet zeigt das Empfaengerfeld „mehrdeutig — siehe Kopfzeilen"; die Mehrdeutigkeits-Box
   bleibt.
6. Ohne konfiguriertes oder lesbares Logo wird trotzdem gedruckt; am Bildschirm erscheint ein Hinweis
   (nicht im Ausdruck), im Log eine Warnung.
7. Der Briefkopf existiert als **ein** Partial; keine Druckansicht enthaelt eine Kopie seines Markups.
8. Der Briefkopf steht nur auf der ersten Seite jedes HauptFA-Dokuments, nachgewiesen an einem mehrseitigen
   PDF (nicht nur am Bildschirm). „Seite X von Y" ist entweder je Dokument korrekt oder gar nicht vorhanden
   (dann Aufgabe in `secondbrain/aufgaben/`).
9. Kein externer Verweis im gerenderten Druck-HTML (`<link>`, `<img src="/…">`).
10. Druck-Inhalt unterhalb des Briefkopfs (Kopftabelle, Positionen, Barcode, Filter-Banner) unveraendert
    (Regression TS-60/TS-74).
11. Der Briefkopf folgt [[cd-auszug-druckdokumente]]: Logo-Hoehe 17,5 mm in der Logozone, Titel/Firmenbezeichnung
    in `#053153`, Lauftext in `#4a494a`; AKE Blau wird nicht als Textfarbe verwendet.
12. `Firmenanschrift` ist in den Standorteinstellungen ein mehrzeiliges Feld; eine zweizeilige Anschrift
    erscheint im Druck und im PDF zweizeilig; HTML-Zeichen in der Anschrift werden als Text ausgegeben.
13. Die Kommissionierlisten-Drucke enthalten keinen Briefkopf (Regression).

## Test-Szenarien

Kapitel TS-60/TS-74 erweitern oder neues Kapitel (naechste freie Nummer):
- **Pruefschritt Nebenbefund (Sage, vor Abnahme):** fuer den HauptFA mit „Dienstleister? · RAL 0"
  ```sql
  SELECT [HauptFA], [Abteilung], [Dienstleister], [RAL], [Start_Beschichtung], [Beschichten_Retour]
  FROM [vw_IDEAL-AKE_Kommissionierung_FAInfos] WHERE [HauptFA] = <HauptFA>;
  ```
  und im WMS `SELECT HauptFA, MontageAbteilung, Dienstleister, RAL FROM dbo.FaHierarchyOrderInfos WHERE HauptFA = <HauptFA>;`
  Gleich ⇒ Datenluecke in Sage; verschieden ⇒ Sync-Befund, melden.
- Happy Path: Firmendaten + Logo gepflegt → Druck und PDF zeigen Briefkopf.
- Aenderung Firmendaten → naechster Druck aktualisiert.
- Negativ: Logo-Pfad falsch → Druck ohne Logo + Bildschirmhinweis + Log.
- Negativ: Dienstleister leer → „Empfaenger fehlt".
- Kombigeraet → „mehrdeutig".
- Mehrseitiges Dokument (viele Positionen) → Briefkopf nur auf Seite 1, am PDF geprueft; Seitenzaehlung je
  Dokument korrekt oder nicht vorhanden.
- Anschrift mehrzeilig pflegen (laut CD: „In der Schörihub 28" / „4810 Gmunden") → Druck zweizeilig;
  Eingabe mit `<b>` → als Text sichtbar.
- CD-Abgleich am Ausdruck: Logo-Hoehe 17,5 mm (Lineal), Farben laut Auszug.
- Regression: Kommissionierliste drucken → kein Briefkopf.
- Regression: Inhalt unter dem Briefkopf unveraendert.

## Etappen (nur bei epic: true)

Nicht zutreffend (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| 1 | — | — | — |

## Deploy

*Vorlaeufig — der QA-Lauf bestaetigt gegen den echten Diff.*

- **Web-App:** ja (Partial, Views, Katalog, Keys).
- **Service:** nein.
- **Migration:** nein bei der Empfehlung; ja, falls Antwort 1 einen DB-Upload waehlt.
- **Betrieb:** Logo-Datei (`IDEAL_AKE_Logo_rgb_klein.jpg` aus dem Anhang, Weg laut Antwort 1) bereitstellen —
  bei Pfad-Variante lesbar fuer die App-Pool-Identitaet; Firmendaten in den Standorteinstellungen pflegen,
  dabei die Anschrift auf IDEAL korrigieren auf „In der Schörihub 28, 4810 Gmunden" (Antwort 5; heute
  Tippfehler „38, 4840" laut [[cd-auszug-druckdokumente]]) — beides je Installation.
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Offene Rueckfragen

1. **„Anpassbare HTML-Vorlage" (Notiz Punkt 2):** vom Anwender in der Oberflaeche gepflegt (gespeicherte
   Vorlage) oder als Datei, die die IT aendert? Woher kommt das Logo (Upload in den Standorteinstellungen)?
   *Empfehlung der Koordination:* fester Partial mit Einstellungswerten fuer Logo, Anschrift, Fusstext — frei
   editierbares HTML in der Datenbank bringt Sicherheits- und Pflegerisiken. *Ergaenzung aus dem Code:* fuer
   das Logo eine Einstellung mit dem **Dateipfad** (Datei ausserhalb des Publish-Ordners, von der IT
   abgelegt), weil `AppSettings.Value` auf 500 Zeichen begrenzt ist und eine Datei im Publish-Ordner bei
   jedem Publish ueberschrieben wird. Ein Upload in der Oberflaeche braucht eine neue Speicherung samt
   Migration.
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
   *Beantwortet mit Antwort 5: ein mehrzeiliges Feld, Key bleibt.*
   *Empfehlung:* ein mehrzeiliges Feld (neuer Feldtyp im Katalog) — kleinster Eingriff, bestehender Key bleibt.
6. **CD-Schrift (neu 2026-10-07, aus dem CD-Auszug):** Laut Manual Nimbus Sans Novus — lizenzpflichtig. Darf
   sie auf dem Webserver installiert bzw. als Webfont (Data-URI) eingebettet werden? Der PDF-Renderer (Headless
   Edge) laeuft auf dem Webserver und sieht nur dort installierte oder eingebettete Schriften.
   *Empfehlung:* bis zur Klaerung serifenlose Systemschrift wie heute (`Print.cshtml:17`, Segoe UI/Arial);
   blockiert die Umsetzung nicht.
7. **Kontaktdaten im Adressblock (neu 2026-10-07, aus dem CD-Auszug):** Der CD-Adressblock fuehrt Telefon,
   E-Mail und Web; die Standorteinstellungen haben dafuer keine Felder (`StandortSettingsCatalog.cs:37-38`:
   nur Firmenname/Firmenanschrift). In den Fusstext schreiben oder eigene Felder?
   *Empfehlung:* in den Fusstext (eine Einstellung, keine neuen Felder).

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Vorlagen-Form und Logo-Quelle →
2. Corporate-Design-Vorlage vorhanden → **Ja (Gerald 2026-10-06): CD-Manual Stand 01/23 und Logos liegen vor – Auszug und Logos in `backlog/anhaenge/2026-10-06-beschichtungsauftrag-ausbau/` ([[cd-auszug-druckdokumente]]: Farben, Typografie, Satzspiegel A4, Logozone, Adressblock).**
3. Briefkopf je Seite / Seitenzaehlung → **Briefkopf nur auf Seite 1; Seitenzählung „Seite X von Y“ ist nice to have – nur umsetzen, wenn ohne großen Aufwand möglich (Gerald 2026-10-06).**
4. Kommissionierlisten → **Nein, kein Briefkopf auf den Kommissionierlisten (Gerald 2026-10-06).**
5. Anschrift mehrzeilig → **Mehrzeilig (Gerald 2026-10-06, korrigiert): ein mehrzeiliges Feld in den Standorteinstellungen (neuer Feldtyp im Katalog, bestehender Key `Firmenanschrift` bleibt), Zeilenumbrüche werden im Druck übernommen. Der heutige Wert hatte einen Tippfehler; Gerald korrigiert ihn auf die Angabe laut CD-Manual (In der Schörihub 28, 4810 Gmunden).**
6. CD-Schrift Nimbus Sans Novus auf Server/eingebettet zulaessig →
7. Kontaktdaten: Fusstext oder eigene Felder →
