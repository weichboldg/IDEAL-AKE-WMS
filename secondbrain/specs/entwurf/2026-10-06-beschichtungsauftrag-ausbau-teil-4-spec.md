---
type: spec
title: "Beschichtungsauftrag-Ausbau Teil 4 — Oberflaeche (Vorschlagsliste, Reklamation, Dienstleister aus Kreditoren)"
slug: 2026-10-06-beschichtungsauftrag-ausbau-teil-4-spec
status: Entwurf
created: 2026-10-06
updated: 2026-10-06
source_backlog: "[[2026-10-06-beschichtungsauftrag-ausbau]]"
depends_on:
  - "[[2026-10-06-beschichtungsauftrag-ausbau-teil-3-spec]]"
  - "[[2026-10-06-kreditoren-stammdaten-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/BeschichtungsauftraegeController.cs (neu — Liste, Details/Bearbeiten, Anlegen aus Vorschlagsliste (POST), Statuswechsel (POST), Reklamation (POST), Print, Pdf/{id}; Class-Level wie FaHierarchyBeschichtungController.cs:27-28, schreibende Actions mit Action-Level-Filter laut Teil-3-Antwort 3)
  - IdealAkeWms/Views/Beschichtungsauftraege/Index.cshtml (neu, ADR-0005-Liste der Auftraege)
  - IdealAkeWms/Views/Beschichtungsauftraege/Details.cshtml (neu — Kopf + Positionen, Bearbeiten je Status, Status- und Reklamations-Buttons)
  - IdealAkeWms/Views/Beschichtungsauftraege/Print.cshtml (neu — Aussendokument; Briefkopf-Partial aus Teil 2, CSS eingebettet, Barcode als Data-URI)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/Index.cshtml (Vorschlagsliste — Auswahl je Position/Gruppe, Button 'Aus Vorschlagsliste erstellen', Markierung bereits vergebener Positionen; heute reine Leseliste, Zeilen 34-122)
  - IdealAkeWms/Services/BeschichtungsauftragService.cs bzw. Controller (Vergabe-Info je Position fuer die Markierung, ueber das Repository aus Teil 3)
  - IdealAkeWms/Models/Beschichtungsauftrag.cs (neue FK-Spalte CreditorId (int?) + Navigation Creditor auf Models/Creditor.cs, Tabelle dbo.Creditors — Namen vorlaeufig laut Kreditoren-Spec, Bestaetigung gemeinsam bei Schranke 1)
  - IdealAkeWms/Data/ApplicationDbContext.cs (Beziehung Beschichtungsauftrag.Creditor, OnDelete Restrict, Index IX_Beschichtungsauftraege_CreditorId)
  - NUR LESEN IdealAkeWms/Data/Repositories/ICreditorRepository.cs (GetAllAsync gefiltert auf IsActive fuer die Auswahl, GetByIdAsync fuer Empfaengerblock; entsteht mit der Kreditoren-Spec)
  - IdealAkeWms/Migrations/<Zeitstempel>_AddBeschichtungsauftragCreditor.cs + SQL/XX_AddBeschichtungsauftragCreditor.sql + SQL/00_FreshInstall.sql (zwei Stellen)
  - IdealAkeWms/Services/PdfFileNameBuilder.cs (Variante fuer die Auftragsnummer; heute nur Build(string, int hauptFa, DateTime) bei :9)
  - IdealAkeWms/Filters/ + Models/RoleKeys.cs + Services/ICurrentUserService.cs + Services/CurrentUserService.cs (Schreibrolle laut Teil-3-Antwort 3)
  - secondbrain/codebase/controller.md + IdealAkeWms/Views/Users/RoleOverview.cshtml (Pflichtstellen 2 und 3 bei neuer Rolle)
  - IdealAkeWms/Views/Shared/_Layout.cshtml (Navigationseintrag 'Beschichtungsauftraege' neben :112-118, gleiche Bedingung)
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (ViewConfig der neuen Liste)
  - IdealAkeWms/Views/Help/ + Views/Help/Changelog.cshtml + beide AppVersion.cs
  - docs/TESTSZENARIEN.md + secondbrain/tests/testszenarien-index.md
open_questions:
  - "Doppelvergabe in der Vorschlagsliste (Notiz Punkt 5, Umsetzung der Teil-3-Antwort 4): markieren mit Auftragsnummer und nicht auswaehlbar, oder ausblenden (ggf. mit Schalter 'vergebene zeigen')?"
  - "Auswahl in der Vorschlagsliste: einzelne Positionen, ganze HauptFA-Gruppe oder beides? Gilt die Auswahl nur auf der aktuellen Seite?"
  - "Reklamation: Ablauf (Folgeauftrag laut Teil-3-Antwort 2) und welche Details sind 'leicht editierbar' (Positionen entfernen/hinzufuegen, Menge, Bemerkung/Grund, Dienstleister, Termine)?"
  - "Dienstleister aus dem Kreditoren-Stamm: ab welchem Status Pflicht, Vorbelegung aus dem Sage-Freitext ueber die Zuordnung der Kreditoren-Spec, Sage-Freitext daneben anzeigen?"
  - "Druck des gespeicherten Auftrags: Briefkopf aus Teil 2 als Pflicht (dann depends_on ergaenzen) und Empfaengeradresse aus dem Kreditor?"
  - "Positionsdarstellung im Druck: summiert nach dem Schluessel aus Teil 1 oder Einzelpositionen? Barcode = Auftragsnummer?"
  - "Bearbeitbarkeit je Status: Kopf und Positionen nur im Entwurf, danach nur Statuswechsel und Bemerkung?"
  - "Drucken im Status Entwurf erlaubt? Wenn ja, mit sichtbarer Kennzeichnung 'ENTWURF'?"
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

> **Umsetzungsort (Hinweis, keine Entscheidung):** nach Schranke 2 des IDEAL-Buendels, aus main (neuer
> Worktree). SQL-Nummer, TS-Nummer und Version: naechste freie zum Umsetzungszeitpunkt (SQL in **beiden**
> Zweigen pruefen). Code-Belege beziehen sich auf den Buendel-Stand
> `.claude/worktrees/2026-08-07-ideal-teile-1-5` (v1.47.1).

Teil 4 von 4 — Uebersicht: [[2026-10-06-beschichtungsauftrag-ausbau-uebersicht]]. Standort: **IDEAL**.

**Abhaengigkeiten:** Teil 3 (Objekt, Workflow, Schreibrolle) und [[2026-10-06-kreditoren-stammdaten-spec]]
(Kreditor-Entitaet). ~~Die Kreditoren-Spec existiert zum Zeitpunkt dieser Spec noch nicht — Entitaets-,
Feld- und Rollennamen des Kreditors sind hier Platzhalter und werden nach deren Freigabe eingesetzt.~~
*(ueberholt 2026-10-06: die Kreditoren-Spec liegt vor, Namen sind unten eingesetzt — siehe Namensabgleich.)*
Teil 2 (Briefkopf) ist weich vorausgesetzt (Rueckfrage 5).

> [!note] Namensabgleich (2026-10-06)
> Die Kreditor-Namen in dieser Spec stammen aus [[2026-10-06-kreditoren-stammdaten-spec]] und sind
> **vorlaeufig**: Entitaet `Creditor`, Tabelle `Creditors`, Sage-Schluessel `SageCreditorNumber`, Felder
> `ShortName`, `IsActive`, `Name`, `Street`, `PostalCode`, `City`, `Country`, Repository `ICreditorRepository`.
> Abgeleitet hier: FK-Spalte `Beschichtungsauftrag.CreditorId`, Navigation `Creditor`, Migration
> `AddBeschichtungsauftragCreditor`, EF-Konventionsnamen `FK_Beschichtungsauftraege_Creditors_CreditorId` und
> `IX_Beschichtungsauftraege_CreditorId`. Alle diese Namen werden **zusammen** bei Schranke 1 beider Specs
> bestaetigt. Der Tabellenteil `Beschichtungsauftraege` haengt zusaetzlich an Teil 3 („Namen im Dev-Lauf final",
> [[2026-10-06-beschichtungsauftrag-ausbau-teil-3-spec]]) und zieht bei einer Aenderung dort mit. Rollennamen fuer den Kreditor braucht Teil 4 nicht — das Setzen des Kreditors am Auftrag laeuft
> ueber die Schreibrolle aus Teil 3; die Stammdaten-Rollen (`masterdata`/`masterdata_read`) betreffen nur die
> Pflege des Stamms.

> [!warning] Hinweis Abgleich — Vorbelegung (nicht geloest, siehe Rueckfrage 4)
> Rueckfrage 4 empfiehlt die Vorbelegung des Kreditors „ueber die Zuordnung der Kreditoren-Spec". Die
> Kreditoren-Spec sieht dafuer nur eine **Meldefunktion** vor (`CreditorAssignment.FindUnassigned(infos, creditors)`,
> Text → „nicht zugeordnet"/„inaktiv"), **keine** wiederverwendbare Abfrage „Dienstleister-Text → Kreditor".
> Zudem ist `ShortName` nur bei bestaetigtem Kurzname-Abgleich (deren Rueckfrage 2) eindeutig. Wird die
> Vorbelegung freigegeben, muss eine der beiden Specs die Zuordnungsfunktion als gemeinsame Methode vorsehen —
> bei Schranke 1 gemeinsam entscheiden.

## Ziel / Nutzen (das Warum)

Teil 3 schafft das gespeicherte Objekt, aber niemand kann es bedienen. Teil 4 liefert den Arbeitsablauf aus
der Anforderung: Positionen in der heutigen Ansicht (Vorschlagsliste) waehlen → Auftrag anlegen → speichern
→ drucken (Freigabe kommt spaeter), Reklamation mit leicht editierbaren Details, Dienstleister als
gepflegter Kreditor statt Sage-Freitext — damit das Dokument eine echte Empfaengeradresse traegt.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Vorschlagsliste: Auswahl + Button **„Aus Vorschlagsliste erstellen"** in `/FaHierarchyBeschichtung`,
  Markierung bereits vergebener Positionen.
- Neue Liste `/Beschichtungsauftraege` (ADR 0005), Detail/Bearbeiten, Statuswechsel-Buttons.
- Button **„Reklamation"** am Auftrag, Details editierbar (Rueckfrage 3).
- Dienstleister-Auswahl aus dem Kreditoren-Stamm (FK auf den Kreditor) — Migration.
- Druck + PDF des gespeicherten Auftrags.
- Schreibrolle und Action-Level-Filter laut Teil-3-Antwort 3 (drei Pflichtstellen bei neuer Rolle).

**Out-of-Scope**
- Freigabe-Schritt (Teil-3-Rueckfrage 6), Positions-Haekchen Reklamation/Neubeschichtung und Anzeige des
  urspruenglichen HauptFA (Teil-3-Rueckfrage 7).
- Pflege des Kreditoren-Stamms selbst und Zuordnung Sage-Freitext → Kreditor (Kreditoren-Spec).
- Mailversand, enaio-Ablage.
- AKE (eine AKE-Variante war in [[2026-07-29-standort-ideal-teil-4-spec]] Antwort 3 angedacht, ist nicht
  Teil dieser Anforderung).

## Fachliche Anforderungen

1. **Vorschlagsliste** = die bestehende Einzelliste (`Index.cshtml`, Filter `Beschichtet == true`,
   `BeschichtungsauftragService.cs:85`). Auswahl laut Rueckfrage 2; bereits vergebene Positionen laut
   Rueckfrage 1 (sperrende Status aus Teil-3-Antwort 4), mit Auftragsnummer als Link.
2. **„Aus Vorschlagsliste erstellen"** ruft den Teil-3-Workflow; Ablehnungen (Doppelvergabe, Position nicht
   mehr im Cache, Kopf mehrdeutig) erscheinen als sichtbare Sammelmeldung mit Positionsliste
   (`TempData["WarningMessage"]` bzw. `ModelState`); bei Erfolg Redirect auf das Detail des neuen Auftrags
   mit `TempData["SuccessMessage"]` inkl. Nummer.
3. **Liste der Auftraege** (ADR 0005): Pagination (`PageSize.Resolve` + `PaginationState` + `_Pagination`),
   Filterkarte (mind. Status), Server-Spaltenfilter an **jeder** Spalte (`data-server-column-filter="true"`,
   `data-col-key`, `ColumnFilterHelper.ReadFromQuery`); Spalten mind. Nummer, Status, Dienstleister, RAL,
   Start, Retour, Anzahl Positionen, Ursprungsauftrag, Angelegt am/von. Datumsspalten in C# nach der
   Projektion filtern. Spalten in `<script id="column-config">` **und** `ColumnDefinitions` (fallstricke.md,
   Zeilen 960-966).
4. **Detail:** Kopf, Positionen, Statushistorie aus den Statusfeldern, Verweis Ursprungs-/Folgeauftrag;
   Bearbeitbarkeit laut Rueckfrage 7; Statuswechsel nur ueber die erlaubten Uebergaenge (Teil 3).
5. **Reklamation** laut Rueckfrage 3 — der Button erzeugt den Folgeauftrag und oeffnet ihn editierbar.
6. **Dienstleister:** Auswahl eines aktiven Kreditors (`Creditor.IsActive == true`); der Sage-Freitext (`Dienstleister`-Snapshot aus
   Teil 3) bleibt sichtbar, damit Abweichungen auffallen. Pflicht laut Rueckfrage 4. Fehlende Zuordnung wird
   gemeldet, nicht still leer gedruckt.
7. **Druck/PDF** des Auftrags laut Rueckfragen 5, 6 und 8; Druck und PDF sind dasselbe Dokument (dieselbe
   View ueber `IRazorViewRenderer` + `IPdfRenderService`, Muster `FaHierarchyBeschichtungController.cs:110-136`);
   PDF-Knopf nur bei `PdfRenderStatus.IsAvailable`, ohne `target="_blank"`; Dateiname ohne Umlaute/Klarnamen,
   mit Auftragsnummer.
8. **Zugriff:** Class-Level `[RequireBeschichtungsauftragAccess]` + `[RequireFaHierarchyBeschichtungAktiv]`
   auf dem neuen Controller (Lesen); **jede** schreibende Action (Anlegen, Bearbeiten, Status, Reklamation,
   Kreditor setzen) zusaetzlich Action-Level-Schreibfilter laut Teil-3-Antwort 3 (ADR 0006 Read/Edit-Split).
   Schreib-Buttons nur bei Schreibrecht rendern. POSTs mit `[ValidateAntiForgeryToken]`.
9. **Navigation:** Eintrag „Beschichtungsauftraege" unter derselben Bedingung wie der bestehende Eintrag
   (`_Layout.cshtml:112-118`: Rolle + Toggle).

## Technischer Loesungsentwurf

- **Neuer Controller** `BeschichtungsauftraegeController` statt Ausbau von `FaHierarchyBeschichtungController`:
  dieser ist die Lese-Sicht auf den Sage-Cache; das gespeicherte Objekt hat eigene Routen mit `id`. Die
  Vorschlagsliste bleibt im bestehenden Controller und POSTet an `BeschichtungsauftraegeController.Create`.
- **Auswahl in der Vorschlagsliste:** `<form method="post">` um die Tabelle, Checkbox je Positionszeile mit dem
  Herkunftsschluessel (HauptFA, VaterFA, SubFA, Position) als Wert, ggf. Gruppen-Checkbox in der
  Gruppenkopfzeile (`Index.cshtml:64-103`). Die Checkbox-Spalte ist keine Datenspalte — kein `data-col-key`,
  nicht im Spaltenzahnrad; `colCount` (`Index.cshtml:11`) anpassen. Spaltenfilter-JS
  (`table-filter.js`) darf die Checkbox-Spalte nicht als Filterspalte behandeln — im Dev-Lauf pruefen.
- **Vergabe-Markierung:** eine Abfrage „Schluessel → Auftragsnummer" fuer die Positionen der aktuellen Seite
  ueber `IBeschichtungsauftragRepository` (kein Join gegen den Cache, keine N+1-Abfrage je Zeile).
- **Kreditor-FK:** `Beschichtungsauftrag.CreditorId` (int?) + Navigation `Creditor` auf `Creditor`
  (Tabelle `Creditors`), `OnDelete Restrict`, per eigener Migration. Auswahlliste nur Kreditoren mit
  `IsActive == true` ueber `ICreditorRepository.GetAllAsync()`; Anzeige in der Auswahl mit `ShortName` und
  `Name`. Namen vorlaeufig (Namensabgleich oben).
- **Druck:** `Views/Beschichtungsauftraege/Print.cshtml` mit `_PrintBriefkopf` (Teil 2), Empfaengerblock aus
  `Creditor` (`Name`, `Street`, `PostalCode`, `City`, `Country`), Kopf, Positionen (laut Rueckfrage 6), CSS eingebettet, Barcode als Data-URI
  (`IBarcodeService.EncodeCode128Base64`, wie `FaHierarchyBeschichtungController.cs:94`).
- **PDF:** `[HttpGet("Beschichtungsauftraege/Pdf/{id:int}")]`; Fehler `PdfRenderException` →
  `TempData["WarningMessage"]` + Redirect auf das Detail. `PdfFileNameBuilder` um eine Variante fuer eine
  String-Kennung erweitern (heute `Build(string, int hauptFa, DateTime)`, `PdfFileNameBuilder.cs:9`); Nummer
  nur ASCII.
- **Concurrency:** RowVersion aus Teil 3 als Hidden-Feld in Bearbeiten/Status-Formularen; Konflikt →
  verstaendliche Meldung + Neuladen.
- `frontend-design`-Skill im Dev-Lauf Pflicht (Views); Bootstrap-5-Muster, Kontrast WCAG AA, Status nie
  nur ueber Farbe (Badge mit Text).

## Migrations-/SQL-Auswirkungen

Eine Spalte (Kreditor-FK `CreditorId`) auf der Teil-3-Tabelle:
- Model → `dotnet ef migrations add AddBeschichtungsauftragCreditor --project IdealAkeWms` →
  `SQL/XX_AddBeschichtungsauftragCreditor.sql` idempotent (`COL_LENGTH(N'dbo.Beschichtungsauftraege', N'CreditorId') IS NULL`
  fuer die Spalte `CreditorId INT NULL`, `OBJECT_ID(N'dbo.FK_Beschichtungsauftraege_Creditors_CreditorId', N'F')` fuer
  den FK-Constraint auf `dbo.Creditors(Id)` ohne Cascade, `sys.indexes` fuer `IX_Beschichtungsauftraege_CreditorId`;
  Namen exakt wie von EF erzeugt, DDL je eigener Batch) → `__EFMigrationsHistory`-Insert separat →
  `SQL/00_FreshInstall.sql` zwei Stellen.
- XX = naechste freie Nummer in **beiden** Zweigen zum Umsetzungszeitpunkt; die Tabelle `dbo.Creditors` muss vorher
  angelegt sein (Reihenfolge der Skripte im Deploy beachten).
- Nicht daten-destruktiv (nullable Spalte). `SQL/AgentJobs/` unberuehrt.
- Falls die Kreditoren-Spec und Teil 4 im selben Wartungsfenster deployt werden: Skriptreihenfolge
  Kreditoren → Teil 3 → Teil 4.

## Audit-Feld-Auswirkungen

Keine neue Entitaet. Alle Schreibvorgaenge laufen ueber den Teil-3-Workflow und setzen dort `Created*` bzw.
`ModifiedAt`, `ModifiedBy`, `ModifiedByWindows` (aus `ICurrentUserService`) — auch das Setzen des Kreditors
und jede Positions-/Kopfaenderung im Entwurf. Statuswechsel zusaetzlich Statuszeitpunkt + Benutzer.

## Akzeptanzkriterien

1. In `/FaHierarchyBeschichtung` lassen sich Positionen laut Rueckfrage 2 waehlen; „Aus Vorschlagsliste
   erstellen" legt einen Auftrag im Status `Entwurf` an und oeffnet dessen Detail mit Erfolgsmeldung inkl.
   Nummer.
2. Bereits vergebene Positionen erscheinen laut Rueckfrage 1 (markiert mit Auftragsnummer bzw. ausgeblendet)
   und sind nicht ein zweites Mal waehlbar; ein manipulierter POST mit einer vergebenen Position wird vom
   Workflow abgelehnt (Sammelmeldung).
3. `/Beschichtungsauftraege` erfuellt ADR 0005 vollstaendig (Pagination, Filterkarte, Server-Spaltenfilter an
   jeder Spalte).
4. Statuswechsel-Buttons zeigen nur erlaubte Uebergaenge; jeder Wechsel ist im Detail mit Zeitpunkt und
   Benutzer sichtbar.
5. „Reklamation" erzeugt laut Rueckfrage 3 einen Folgeauftrag mit Verweis; Ursprung und Folge verlinken
   sich gegenseitig im Detail.
6. Der Dienstleister ist aus Kreditoren mit `IsActive == true` waehlbar (inaktive fehlen in der Auswahl); die
   Wahl landet in `Beschichtungsauftraege.CreditorId`; der Sage-Freitext bleibt sichtbar; die Pflicht
   laut Rueckfrage 4 wird beim Statuswechsel geprueft und mit Klartext gemeldet.
7. Druck und PDF des Auftrags zeigen Briefkopf (falls Teil 2 vorhanden), Empfaengeradresse aus dem Kreditor
   (`Name`, `Street`, `PostalCode`, `City`, `Country`),
   Kopf und Positionen laut Rueckfrage 6; PDF-Dateiname enthaelt die Auftragsnummer und keine Klarnamen.
8. Benutzer nur mit Leserecht sehen Liste, Detail, Druck und PDF, aber keine Schreib-Buttons; ein direkter
   POST auf eine schreibende Action endet in AccessDenied.
9. Toggle `FaHierarchyBeschichtungAktiv` aus → alle neuen Seiten gesperrt, Navigationseintrag fehlt.
10. Paralleles Bearbeiten desselben Auftrags fuehrt beim zweiten Speichern zu einer Concurrency-Meldung.
11. Bei neuer Rolle: Attribut, `controller.md` und `RoleOverview.cshtml` enthalten sie.
12. Bestehende Liste, Druck und PDF der Vorschlagsliste funktionieren ohne Auswahl unveraendert (Regression
    TS-60/TS-74).

## Test-Szenarien

Neues Kapitel (naechste freie Nummer), Manual-UAT am IDEAL-Testsystem mit beschichteten Positionen
(Vorbedingung wie TS-60: produktivnahe Daten):
- Happy Path: Positionen waehlen → anlegen → Kreditor setzen → uebergeben → drucken/PDF → erledigt.
- Doppelvergabe: dieselbe Position erneut waehlen (UI) und per manipuliertem POST → Ablehnung.
- Sync zwischen Seitenaufbau und Anlegen (Position verschwunden) → Ablehnung mit Liste.
- Kombigeraet / mehrere HauptFA mit abweichendem Dienstleister/RAL → Verhalten laut Teil-3-Antwort 5.
- Reklamation aus `Erledigt` → Folgeauftrag, Details editieren, Verweise in beiden Richtungen.
- Unerlaubter Statuswechsel (direkter POST) → Ablehnung.
- Rechte: Leserecht ohne Schreib-Buttons; POST → AccessDenied; Toggle aus.
- Concurrency: zwei Browser, gleicher Auftrag.
- PDF ohne Edge → Knopf fehlt; PDF-Fehler → WarningMessage mit Rueckfallweg Bildschirmdruck.
- Liste: Spaltenfilter je Spalte, Pagination, Statusfilter.

## Etappen (nur bei epic: true)

Nicht zutreffend (`epic: false`).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| 1 | — | — | — |

## Deploy

*Vorlaeufig — der QA-Lauf bestaetigt gegen den echten Diff.*

- **Web-App:** ja (Controller, Views, Filter, Layout, Migration-Modell).
- **Service:** nein (nur Versions-Bump).
- **Migration:** ja — `SQL/XX_AddBeschichtungsauftragCreditor.sql` + History-Insert vor dem IIS-Recycle;
  setzt `dbo.Creditors` (Kreditoren-Spec) und die Teil-3-Tabellen voraus.
- **Betrieb:** neue Schreibrolle den zustaendigen Benutzern zuweisen (sonst sieht niemand die Buttons).
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Offene Rueckfragen

1. **Doppelvergabe in der Vorschlagsliste (Notiz Punkt 5):** markieren (mit Auftragsnummer, nicht waehlbar)
   oder ausblenden (ggf. Schalter „vergebene zeigen")? Muss zur Teil-3-Antwort 4 passen.
   *Empfehlung:* markieren, nicht ausblenden — sonst ist nicht erkennbar, warum ein Teil fehlt.
2. **Auswahl:** einzelne Positionen, ganze HauptFA-Gruppe oder beides? Nur aktuelle Seite?
   *Empfehlung:* Checkbox je Position plus Gruppen-Checkbox; Auswahl gilt nur fuer die aktuelle Seite (die
   Seiteneinheit ist ohnehin die HauptFA-Gruppe) — kein seitenuebergreifender Merkzettel.
3. **Reklamation:** Ablauf (laut Teil-3-Antwort 2 Folgeauftrag) und welche Details „leicht editierbar":
   Positionen entfernen/hinzufuegen, Menge, Reklamationsgrund/Bemerkung, Dienstleister, Termine?
   *Empfehlung:* Folgeauftrag mit allen Positionen des Ursprungs vorbelegt, im Entwurf alles editierbar
   (Positionen entfernen, Menge, Grund, Dienstleister, Termine); Positionen hinzufuegen nur aus dem Ursprung.
4. **Dienstleister aus dem Kreditoren-Stamm:** ab welchem Status Pflicht? Vorbelegung aus dem Sage-Freitext
   ueber die Zuordnung der Kreditoren-Spec? Sage-Freitext daneben anzeigen?
   *Empfehlung:* Pflicht beim Wechsel nach `Uebergeben`, im Entwurf optional; Vorbelegung nur bei eindeutiger
   Zuordnung; Sage-Freitext immer daneben sichtbar.
5. **Briefkopf/Empfaenger im Auftragsdruck:** Ist Teil 2 Pflicht vor Teil 4 (dann `depends_on` um Teil 2
   ergaenzen), und kommt die Empfaengeradresse aus dem Kreditor?
   *Empfehlung:* ja zu beidem — der gespeicherte Auftrag ist das Aussendokument; ohne Briefkopf waere es
   unvollstaendig ([[2026-09-17-druck-briefkopf-logo-adresse]]).
6. **Positionsdarstellung im Druck:** summiert nach dem Teil-1-Schluessel (dann Teil 1 als weitere
   Abhaengigkeit) oder Einzelpositionen? Barcode = Auftragsnummer?
   *Empfehlung:* summiert je HauptFA nach dem Teil-1-Schluessel, Barcode = Auftragsnummer (ein Auftrag kann
   mehrere HauptFA enthalten, Teil-3-Antwort 5).
7. **Bearbeitbarkeit je Status:** Kopf und Positionen nur im `Entwurf`, danach nur Statuswechsel und
   Bemerkung?
   *Empfehlung:* ja.
8. **Drucken im Entwurf:** erlaubt? Mit Kennzeichnung „ENTWURF"?
   *Empfehlung:* erlaubt (Ablauf „speichern, ausdrucken"; Freigabe kommt spaeter), ohne Wasserzeichen —
   sonst druckt der heutige Ablauf nur gekennzeichnete Dokumente. Mit dem Freigabe-Schritt neu bewerten.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Doppelvergabe-Darstellung →
2. Auswahl-Einheit / Seite →
3. Reklamation: Ablauf und editierbare Details →
4. Kreditor: Pflicht / Vorbelegung / Sage-Text →
5. Briefkopf-Pflicht und Empfaengeradresse →
6. Positionsdarstellung im Druck / Barcode →
7. Bearbeitbarkeit je Status →
8. Drucken im Entwurf →
