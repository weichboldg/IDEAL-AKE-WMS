---
type: spec
title: "Beschichtungsauftrag-Ausbau Teil 4 — Oberflaeche (Vorschlagsliste, Reklamation, Druck)"
slug: 2026-10-06-beschichtungsauftrag-ausbau-teil-4-spec
status: Entwurf
created: 2026-10-06
updated: 2026-10-07
source_backlog: "[[2026-10-06-beschichtungsauftrag-ausbau]]"
depends_on:
  - "[[2026-10-06-beschichtungsauftrag-ausbau-teil-2-spec]]"
  - "[[2026-10-06-beschichtungsauftrag-ausbau-teil-3-spec]]"
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/BeschichtungsauftraegeController.cs (neu — Liste, Details, Entwurf bearbeiten (POST), Bemerkung (POST), Anlegen je HauptFA (POST), Statuswechsel (POST), Reklamation (GET Auswahl + POST), Print/{id}, Pdf/{id}; Class-Level wie FaHierarchyBeschichtungController.cs:27-28, schreibende Actions mit Action-Level-Schreibfilter laut Teil-3-Antwort 3)
  - IdealAkeWms/Views/Beschichtungsauftraege/Index.cshtml (neu, ADR-0005-Liste der Auftraege)
  - IdealAkeWms/Views/Beschichtungsauftraege/Details.cshtml (neu — Kopf + Positionen, Bearbeiten nur im Entwurf, Bemerkung, Status- und Reklamations-Buttons, Verweise Ursprung/Folgeauftraege)
  - IdealAkeWms/Views/Beschichtungsauftraege/Reklamation.cshtml (neu — Teileauswahl aus den Ursprungspositionen, nichts vorausgewaehlt)
  - IdealAkeWms/Views/Beschichtungsauftraege/Print.cshtml (neu — Aussendokument ab Uebergeben; Briefkopf-Partial aus Teil 2, Positionen summiert nach Artnr mit Maßen, Barcode = Auftragsnummer, CSS eingebettet)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/Summiert.cshtml (aus Teil 1, Standardansicht) und Index.cshtml (Einzelliste, Gruppenkopf Zeilen 64-103) — je HauptFA-Gruppenkopf Button 'Aus Vorschlagsliste erstellen' (POST-Form) bzw. Auftragsnummer(n) als Link
  - IdealAkeWms/Services/BeschichtungsauftragService.cs bzw. Controller (Vergabe-Info je HauptFA/Position fuer die Markierung, ueber das Repository aus Teil 3)
  - IdealAkeWms/Services/PdfFileNameBuilder.cs (Variante fuer die Auftragsnummer; heute nur Build(string, int hauptFa, DateTime) bei :9)
  - IdealAkeWms/Filters/ + Models/RoleKeys.cs + Services/ICurrentUserService.cs + Services/CurrentUserService.cs (Schreibrolle laut Teil-3-Antwort 3)
  - secondbrain/codebase/controller.md + IdealAkeWms/Views/Users/RoleOverview.cshtml (Pflichtstellen 2 und 3 bei neuer Rolle)
  - IdealAkeWms/Views/Shared/_Layout.cshtml (Navigationseintrag 'Beschichtungsauftraege' neben :112-118, gleiche Bedingung)
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (ViewConfig der neuen Liste)
  - IdealAkeWms/Views/Help/ + Views/Help/Changelog.cshtml + beide AppVersion.cs
  - docs/TESTSZENARIEN.md + secondbrain/tests/testszenarien-index.md
open_questions:
  - "Dienstleister Pflicht: Antwort 4 klaert Quelle und Ueberschreiben, nicht die Pflicht. Muss der Dienstleister (Empfaenger des Aussendokuments, Antwort 5) beim Wechsel nach Uebergeben gefuellt sein? Empfehlung: ja, Pflicht beim Wechsel nach Uebergeben, im Entwurf optional."
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
> Worktree). SQL-Nummer, TS-Nummer und Version: naechste freie zum Umsetzungszeitpunkt (SQL in **beiden**
> Zweigen pruefen). Code-Belege beziehen sich auf den Buendel-Stand
> `.claude/worktrees/2026-08-07-ideal-teile-1-5` (v1.47.1).

Teil 4 von 4 — Uebersicht: [[2026-10-06-beschichtungsauftrag-ausbau-uebersicht]]. Standort: **IDEAL**.

**Abhaengigkeiten (Stand 2026-10-07):** Teil 3 (Objekt, Workflow, Schreibrolle) und Teil 2 (Briefkopf-Partial,
Pflicht laut Antwort 5). **Keine** Abhaengigkeit mehr zur [[2026-10-06-kreditoren-stammdaten-spec]]: der
Dienstleister wird als Freitext ueberschrieben (Antwort 4, Nachtrag Gerald); die Auswahl aus dem
Kreditoren-Stamm kommt spaeter, ausserhalb dieses Pakets. Teil 1 ist weich: der Auftragsdruck summiert nach
dem Teil-1-Schluessel (Antwort 6, eigene GroupBy auf den gespeicherten Positionen, kein Code aus Teil 1
noetig); ist Teil 1 gemergt (empfohlene Reihenfolge), bekommt auch dessen `Summiert.cshtml` den Anlege-Button.
~~(Kreditor-Entitaet). Die Kreditoren-Spec existiert zum Zeitpunkt dieser Spec noch nicht — Entitaets-,
Feld- und Rollennamen des Kreditors sind hier Platzhalter und werden nach deren Freigabe eingesetzt.~~
~~*(ueberholt 2026-10-06: die Kreditoren-Spec liegt vor, Namen sind unten eingesetzt — siehe Namensabgleich.)*
Teil 2 (Briefkopf) ist weich vorausgesetzt (Rueckfrage 5).~~ *(ueberholt mit Antworten 4 und 5)*

> [!note] Namensabgleich (2026-10-06) — UEBERHOLT 2026-10-07 (verworfen mit Antwort 4: kein Kreditor-FK in Teil 4)
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

> [!warning] Hinweis Abgleich — Vorbelegung — UEBERHOLT 2026-10-07 (geloest mit Antwort 4: Vorbelegung = Sage-Text `FaHierarchyOrderInfo.Dienstleister`, keine Zuordnung zum Kreditor noetig)
> Rueckfrage 4 empfiehlt die Vorbelegung des Kreditors „ueber die Zuordnung der Kreditoren-Spec". Die
> Kreditoren-Spec sieht dafuer nur eine **Meldefunktion** vor (`CreditorAssignment.FindUnassigned(infos, creditors)`,
> Text → „nicht zugeordnet"/„inaktiv"), **keine** wiederverwendbare Abfrage „Dienstleister-Text → Kreditor".
> Zudem ist `ShortName` nur bei bestaetigtem Kurzname-Abgleich (deren Rueckfrage 2) eindeutig. Wird die
> Vorbelegung freigegeben, muss eine der beiden Specs die Zuordnungsfunktion als gemeinsame Methode vorsehen —
> bei Schranke 1 gemeinsam entscheiden.

## Ziel / Nutzen (das Warum)

Teil 3 schafft das gespeicherte Objekt, aber niemand kann es bedienen. Teil 4 liefert den Arbeitsablauf aus
der Anforderung: aus der heutigen Ansicht (Vorschlagsliste) je HauptFA einen Auftrag anlegen → im Entwurf
nachbearbeiten → uebergeben → drucken (Freigabe kommt spaeter), Reklamation mit leicht editierbaren Details,
Dienstleister am Auftrag ueberschreibbar. ~~Positionen ... waehlen~~ *(verworfen mit Antwort 2)*;
~~Dienstleister als gepflegter Kreditor statt Sage-Freitext — damit das Dokument eine echte Empfaengeradresse
traegt~~ *(verworfen mit Antwort 4: Freitext genuegt, Kreditoren-Stamm spaeter)*.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Vorschlagsliste: Button **„Aus Vorschlagsliste erstellen"** je HauptFA-Gruppe in der Beschichtungs-Ansicht
  (summierte Standardsicht aus Teil 1 und Einzelliste), Markierung bereits vergebener Positionen mit
  Auftragsnummer. ~~Auswahl~~ *(verworfen mit Antwort 2: keine Einzelauswahl per Haekchen)*
- Neue Liste `/Beschichtungsauftraege` (ADR 0005), Detail, Bearbeiten im Entwurf, Bemerkung, Statuswechsel-Buttons.
- Button **„Reklamation"** am Auftrag: Teile auswaehlen, nichts vorausgewaehlt, Vermerk am Ursprung (Antwort 3).
- Dienstleister als Freitext am Auftrag, vorbelegt aus Sage, ueberschreibbar (Antwort 4).
  ~~Dienstleister-Auswahl aus dem Kreditoren-Stamm (FK auf den Kreditor) — Migration.~~ *(verworfen mit
  Antwort 4)*
- Druck + PDF des gespeicherten Auftrags ab `Uebergeben` (Antwort 8), mit Briefkopf (Antwort 5), summiert
  (Antwort 6).
- Schreibrolle und Action-Level-Filter laut Teil-3-Antwort 3 (drei Pflichtstellen bei neuer Rolle).

**Out-of-Scope**
- Freigabe-Schritt (Teil-3-Rueckfrage 6), Positions-Haekchen Reklamation/Neubeschichtung und Anzeige des
  urspruenglichen HauptFA (Teil-3-Rueckfrage 7).
- Dienstleister-Auswahl aus dem Kreditoren-Stamm, Kreditor-Fremdschluessel, Empfaengeradresse aus dem Kreditor
  — spaeter (Antwort 4, Nachtrag Gerald); Pflege des Stamms selbst ist die Kreditoren-Spec.
- Einzelauswahl von Positionen per Haekchen (Antwort 2).
- Mailversand, enaio-Ablage.
- AKE (eine AKE-Variante war in [[2026-07-29-standort-ideal-teil-4-spec]] Antwort 3 angedacht, ist nicht
  Teil dieser Anforderung).

## Fachliche Anforderungen

1. **Vorschlagsliste** = die Beschichtungs-Ansicht (Filter `Beschichtet == true`,
   `BeschichtungsauftragService.cs:85`) — Standard ist die summierte Sicht aus Teil 1, daneben die Einzelliste
   (`Index.cshtml`). Je HauptFA-Gruppenkopf (`Index.cshtml:64-103` bzw. Gruppenkopf der Summiert-Sicht) steht
   der Button **„Aus Vorschlagsliste erstellen"** (nur mit Schreibrecht). Er uebernimmt **automatisch alle**
   beschichteten Positionen des HauptFA (Antwort 2); ~~Auswahl je Position/Gruppe~~ *(verworfen mit Antwort 2)*.
   Bereits vergebene Positionen werden **markiert** mit Auftragsnummer als Link und sind nicht erneut vergebbar
   ausser ueber Reklamation (Antwort 1, Teil-3-Antwort 4): der Gruppenkopf nennt die Auftragsnummer(n); in der
   Einzelliste traegt zusaetzlich jede vergebene Position ein Badge „in BA-…" in einer bestehenden Zelle (keine
   neue Datenspalte, daher keine neue Spaltenfilter-Pflicht). Sind alle Positionen des HauptFA vergeben,
   erscheint statt des Buttons nur die Auftragsnummer. Teilweise vergebene HauptFA: Teil-3-Rueckfrage 12 (bis
   zur Antwort lehnt der Workflow ab, die Sammelmeldung nennt die vergebenen Positionen).
2. **„Aus Vorschlagsliste erstellen"** POSTet den HauptFA an den Teil-3-Workflow (`AnlegenAsync(hauptFa)`).
   Ablehnungen (Doppelvergabe, keine beschichteten Positionen mehr im Cache) erscheinen als sichtbare
   Sammelmeldung mit Positionsliste (`TempData["WarningMessage"]`); der Hinweis „Kopf aus Sage mehrdeutig"
   (Kombigeraet, Teil-3-Anforderung 4) erscheint nach erfolgreicher Anlage als `WarningMessage` im Detail. Bei
   Erfolg Redirect auf das Detail des neuen Auftrags mit `TempData["SuccessMessage"]` inkl. Nummer.
3. **Liste der Auftraege** (ADR 0005): Pagination (`PageSize.Resolve` + `PaginationState` + `_Pagination`),
   Filterkarte (mind. Status), Server-Spaltenfilter an **jeder** Spalte (`data-server-column-filter="true"`,
   `data-col-key`, `ColumnFilterHelper.ReadFromQuery`); Spalten mind. Nummer, HauptFA, Status, Dienstleister,
   RAL, Start, Retour, Anzahl Positionen, Ursprungsauftrag, Angelegt am/von. Datumsspalten in C# nach der
   Projektion filtern. Spalten in `<script id="column-config">` **und** `ColumnDefinitions` (fallstricke.md,
   Zeilen 960-966).
4. **Detail:** Kopf, Positionen, Statushistorie aus den Statusfeldern, Verweis Ursprungs-/Folgeauftrag;
   Statuswechsel nur ueber die erlaubten Uebergaenge (Teil 3). **Bearbeitbarkeit** (Antwort 7): Kopf
   (Dienstleister, RAL, Start, Retour) und Positionen (Menge, entfernen) nur im `Entwurf`; danach nur
   Statuswechsel und Bemerkung.
5. **Reklamation** (Antwort 3, Teil-3-Antwort 2): Button am Auftrag im Status `Erledigt` (ob auch `Uebergeben`:
   Teil-3-Rueckfrage 11) oeffnet eine Auswahl der Ursprungspositionen, **nichts** vorausgewaehlt; Absenden legt
   den Folgeauftrag (`Entwurf`) an und oeffnet ihn editierbar. ~~Folgeauftrag mit allen Positionen des Ursprungs
   vorbelegt~~ *(verworfen mit Antwort 3)*. **Vermerk am Ursprungsauftrag:** dessen Detail und die Liste zeigen
   „Reklamiert → BA-…" mit Link auf jeden Folgeauftrag (aus `UrsprungsauftragId`, Teil 3); der Folgeauftrag
   zeigt den Ursprung. Kopf-Vorbelegung des Folgeauftrags: Teil-3-Rueckfrage 13.
6. **Dienstleister** (Antwort 4): Freitextfeld am Auftrag, vorbelegt mit dem Standard-Beschichter aus der
   PPS-View (`FaHierarchyOrderInfo.Dienstleister`, `FaHierarchyOrderInfo.cs:27`, nur vom Sync geschrieben
   `FaHierarchySyncService.cs:282`), im `Entwurf` ueberschreibbar — auch und gerade beim Reklamations-
   Folgeauftrag. Weicht der Wert vom aktuellen Sage-Wert ab, zeigt das Detail den Sage-Wert daneben (live aus
   dem Cache; fehlt der HauptFA dort, kein Vergleich). Pflicht laut Rueckfrage 9; bis zur Antwort gilt: ein
   leerer Dienstleister wird nie still gedruckt — der Briefkopf zeigt sichtbar „Empfaenger fehlt — am Auftrag
   nachtragen".
   ~~Auswahl eines aktiven Kreditors (`Creditor.IsActive == true`) ... Fehlende Zuordnung wird gemeldet~~
   *(verworfen mit Antwort 4)*
7. **Druck/PDF** des Auftrags; Druck und PDF sind dasselbe Dokument (dieselbe View ueber `IRazorViewRenderer` +
   `IPdfRenderService`, Muster `FaHierarchyBeschichtungController.cs:110-136`); PDF-Knopf nur bei
   `PdfRenderStatus.IsAvailable`, ohne `target="_blank"`; Dateiname ohne Umlaute/Klarnamen, mit Auftragsnummer.
   - **Nur ab `Uebergeben`** (Antwort 8): im `Entwurf` (und `Storniert`) keine Druck-/PDF-Knoepfe; ein direkter
     Aufruf von `Print`/`Pdf` fuer einen Entwurf fuehrt mit `WarningMessage` „Im Entwurf nicht druckbar — erst
     uebergeben" zurueck auf das Detail. ~~Drucken im Entwurf erlaubt, ohne Wasserzeichen~~ *(verworfen mit
     Antwort 8)*
   - **Briefkopf** aus Teil 2 (`_PrintBriefkopf`) ist Pflicht (Antwort 5); **Empfaenger** = Dienstleister des
     Auftrags (Freitext). ~~Empfaengeradresse aus dem Kreditor~~ *(verworfen mit Antwort 4)*
   - **Positionen summiert** nach dem Teil-1-Schluessel (Antwort 6): da ein Auftrag genau einen HauptFA hat
     (Teil-3-Antwort 5), ist das eine Zeile je `Artnr` mit Summe der gespeicherten `Menge`, Matchcode und
     **Maße angedruckt** (aus der ersten Position der Gruppe; abweichende Maße → „Maße uneinheitlich" wie in
     Teil 1, Anforderung 1).
   - **Barcode = Auftragsnummer** (Antwort 6), Code 128 ueber `IBarcodeService.EncodeCode128Base64`
     (`FaHierarchyBeschichtungController.cs:94`).
8. **Zugriff:** Class-Level `[RequireBeschichtungsauftragAccess]` + `[RequireFaHierarchyBeschichtungAktiv]`
   auf dem neuen Controller (Lesen); **jede** schreibende Action (Anlegen, Entwurf bearbeiten, Bemerkung,
   Status, Reklamation) zusaetzlich Action-Level-Schreibfilter der neuen Berechtigungsgruppe
   „Beschichtungsaufträge" (Teil-3-Antwort 3; ADR 0006 Read/Edit-Split). ~~Kreditor setzen~~ *(verworfen mit
   Antwort 4)*. Schreib-Buttons — auch „Aus Vorschlagsliste erstellen" in der Beschichtungs-Ansicht — nur bei
   Schreibrecht rendern. POSTs mit `[ValidateAntiForgeryToken]`. Neue Rolle = drei Stellen: Attribut,
   `secondbrain/codebase/controller.md`, `Views/Users/RoleOverview.cshtml` (dort klar als Schreibrecht neben der
   Leserolle „Beschichtungsauftrag (IDEAL)", `RoleOverview.cshtml:194`).
9. **Navigation:** Eintrag „Beschichtungsauftraege" unter derselben Bedingung wie der bestehende Eintrag
   (`_Layout.cshtml:112-118`: Rolle + Toggle).

## Technischer Loesungsentwurf

- **Neuer Controller** `BeschichtungsauftraegeController` statt Ausbau von `FaHierarchyBeschichtungController`:
  dieser ist die Lese-Sicht auf den Sage-Cache; das gespeicherte Objekt hat eigene Routen mit `id`. Die
  Vorschlagsliste bleibt im bestehenden Controller und POSTet an `BeschichtungsauftraegeController.Create`.
- **Anlegen aus der Vorschlagsliste:** je HauptFA-Gruppenkopf ein kleines `<form method="post">` mit
  Hidden-Feld `hauptFa` und Antiforgery-Token, Ziel `BeschichtungsauftraegeController.Create`. Keine
  Checkbox-Spalte, `colCount` (`Index.cshtml:11`) bleibt. ~~`<form method="post">` um die Tabelle, Checkbox je
  Positionszeile ... Gruppen-Checkbox~~ *(verworfen mit Antwort 2)*
- **Vergabe-Markierung:** eine Abfrage „Schluessel → Auftragsnummer" fuer die HauptFA der aktuellen Seite
  ueber `IBeschichtungsauftragRepository` (kein Join gegen den Cache, keine N+1-Abfrage je Zeile). Badge-Text
  in einer bestehenden Zelle darf Server-Spaltenfilter und Client-Sortierung (`table-filter.js`) nicht
  verfaelschen — im Dev-Lauf pruefen.
- ~~**Kreditor-FK:** `Beschichtungsauftrag.CreditorId` (int?) + Navigation `Creditor` ... Namen vorlaeufig
  (Namensabgleich oben).~~ *(verworfen mit Antwort 4 — Dienstleister bleibt die Freitext-Spalte aus Teil 3)*
- **Druck:** `Views/Beschichtungsauftraege/Print.cshtml` mit `_PrintBriefkopf` (Teil 2, Empfaenger =
  `Beschichtungsauftrag.Dienstleister`), Kopf (Nummer, HauptFA, RAL, Start, Retour, Bemerkung), Positionen
  summiert je `Artnr` (GroupBy auf den gespeicherten Positionen im ViewModel-Aufbau; Maße-Invariante wie
  Teil 1), CSS eingebettet, Barcode der Auftragsnummer als Data-URI (`IBarcodeService.EncodeCode128Base64`,
  wie `FaHierarchyBeschichtungController.cs:94`). ~~Empfaengerblock aus `Creditor`~~ *(verworfen mit Antwort 4)*
  `Print`/`Pdf` pruefen den Status serverseitig (Entwurf/Storniert → Redirect mit `WarningMessage`).
- **PDF:** `[HttpGet("Beschichtungsauftraege/Pdf/{id:int}")]`; Fehler `PdfRenderException` →
  `TempData["WarningMessage"]` + Redirect auf das Detail. `PdfFileNameBuilder` um eine Variante fuer eine
  String-Kennung erweitern (heute `Build(string, int hauptFa, DateTime)`, `PdfFileNameBuilder.cs:9`); Nummer
  nur ASCII.
- **Concurrency:** RowVersion aus Teil 3 als Hidden-Feld in Bearbeiten/Status-Formularen; Konflikt →
  verstaendliche Meldung + Neuladen.
- `frontend-design`-Skill im Dev-Lauf Pflicht (Views); Bootstrap-5-Muster, Kontrast WCAG AA, Status nie
  nur ueber Farbe (Badge mit Text).

## Migrations-/SQL-Auswirkungen

**Keine** (Stand 2026-10-07). Mit Antwort 4 entfaellt der Kreditor-Fremdschluessel; der Dienstleister ist die
Freitext-Spalte aus Teil 3, `HauptFA` am Kopf ebenfalls Teil 3. Kein `SQL/XX_*.sql`, `SQL/00_FreshInstall.sql`
unveraendert, `SQL/AgentJobs/` unberuehrt. Voraussetzung im Deploy: die Teil-3-Tabellen sind angelegt.
Die neue Rolle ist ein statischer Key (ADR 0006): die bestehende Leserolle `beschichtungsauftrag` hat kein
SQL-Seed (Grep `beschichtungsauftrag` in `*.sql`/`*.cs` des Buendels: nur `RoleKeys.cs:24`,
`RequireBeschichtungsauftragAccessAttribute.cs`, `ICurrentUserService.cs`, `CurrentUserService.cs:138`,
Controller) — die Schreibrolle braucht daher ebenfalls kein Skript.

> [!quote]- UEBERHOLT — verworfen mit Antwort 4 (urspruenglicher Entwurf, nur Audit-Spur, NICHT umsetzen)
> Eine Spalte (Kreditor-FK `CreditorId`) auf der Teil-3-Tabelle:
> - Model → `dotnet ef migrations add AddBeschichtungsauftragCreditor --project IdealAkeWms` →
>   `SQL/XX_AddBeschichtungsauftragCreditor.sql` idempotent (`COL_LENGTH(N'dbo.Beschichtungsauftraege', N'CreditorId') IS NULL`
>   fuer die Spalte `CreditorId INT NULL`, `OBJECT_ID(N'dbo.FK_Beschichtungsauftraege_Creditors_CreditorId', N'F')` fuer
>   den FK-Constraint auf `dbo.Creditors(Id)` ohne Cascade, `sys.indexes` fuer `IX_Beschichtungsauftraege_CreditorId`;
>   Namen exakt wie von EF erzeugt, DDL je eigener Batch) → `__EFMigrationsHistory`-Insert separat →
>   `SQL/00_FreshInstall.sql` zwei Stellen.
> - XX = naechste freie Nummer in **beiden** Zweigen zum Umsetzungszeitpunkt; die Tabelle `dbo.Creditors` muss vorher
>   angelegt sein (Reihenfolge der Skripte im Deploy beachten).
> - Nicht daten-destruktiv (nullable Spalte). `SQL/AgentJobs/` unberuehrt.
> - Falls die Kreditoren-Spec und Teil 4 im selben Wartungsfenster deployt werden: Skriptreihenfolge
>   Kreditoren → Teil 3 → Teil 4.

## Audit-Feld-Auswirkungen

Keine neue Entitaet. Alle Schreibvorgaenge laufen ueber den Teil-3-Workflow und setzen dort `Created*` bzw.
`ModifiedAt`, `ModifiedBy`, `ModifiedByWindows` (aus `ICurrentUserService`) — auch das Ueberschreiben des
Dienstleisters, jede Positions-/Kopfaenderung im Entwurf und jede Bemerkungsaenderung. Statuswechsel zusaetzlich
Statuszeitpunkt + Benutzer. ~~auch das Setzen des Kreditors~~ *(verworfen mit Antwort 4)*

## Akzeptanzkriterien

1. In der Beschichtungs-Ansicht (summiert und Einzelliste) hat jeder HauptFA-Gruppenkopf fuer Benutzer mit
   Schreibrecht den Button „Aus Vorschlagsliste erstellen"; er legt einen Auftrag im Status `Entwurf` mit
   **allen** beschichteten Positionen des HauptFA an und oeffnet dessen Detail mit Erfolgsmeldung inkl. Nummer.
   Es gibt keine Checkboxen.
2. Nach der Anlage zeigt der Gruppenkopf die Auftragsnummer als Link statt des Buttons; die Einzelliste
   markiert jede vergebene Position mit „in BA-…". Ein manipulierter POST fuer einen HauptFA mit vergebenen
   Positionen wird vom Workflow abgelehnt (Sammelmeldung mit Positionen und Auftragsnummer).
3. `/Beschichtungsauftraege` erfuellt ADR 0005 vollstaendig (Pagination, Filterkarte, Server-Spaltenfilter an
   jeder Spalte).
4. Statuswechsel-Buttons zeigen nur erlaubte Uebergaenge; jeder Wechsel ist im Detail mit Zeitpunkt und
   Benutzer sichtbar.
5. „Reklamation" an einem `Erledigt`-Auftrag zeigt die Ursprungspositionen ohne Vorauswahl; Absenden ohne
   Auswahl wird abgelehnt; mit Auswahl entsteht ein `Entwurf`-Folgeauftrag mit genau diesen Positionen. Der
   Ursprung zeigt „Reklamiert → BA-…" (Detail und Liste), der Folgeauftrag den Ursprung; beide verlinkt.
6. Der Dienstleister ist beim Anlegen mit `FaHierarchyOrderInfo.Dienstleister` vorbelegt und im `Entwurf` als
   Freitext ueberschreibbar (auch im Reklamations-Folgeauftrag); nach `Uebergeben` nicht mehr aenderbar.
   Weicht er vom aktuellen Sage-Wert ab, zeigt das Detail den Sage-Wert daneben. Pflicht laut Rueckfrage 9.
6a. Kopf und Positionen sind nur im `Entwurf` aenderbar; in `Uebergeben`/`Erledigt` ist nur die Bemerkung
    editierbar (UI zeigt keine anderen Eingabefelder, ein direkter POST wird abgelehnt).
7. Druck und PDF des Auftrags (ab `Uebergeben`) zeigen den Briefkopf aus Teil 2 mit dem Dienstleister des
   Auftrags als Empfaenger, den Kopf, die Positionen **summiert je Artnr** mit Maßen und den Barcode der
   Auftragsnummer; PDF-Dateiname enthaelt die Auftragsnummer und keine Klarnamen.
7a. Im `Entwurf` gibt es keinen Druck-/PDF-Knopf; ein direkter Aufruf von `Print`/`Pdf` fuehrt mit
    `WarningMessage` zurueck auf das Detail.
8. Benutzer nur mit Leserecht sehen Liste, Detail, Druck und PDF, aber keine Schreib-Buttons; ein direkter
   POST auf eine schreibende Action endet in AccessDenied.
9. Toggle `FaHierarchyBeschichtungAktiv` aus → alle neuen Seiten gesperrt, Navigationseintrag fehlt.
10. Paralleles Bearbeiten desselben Auftrags fuehrt beim zweiten Speichern zu einer Concurrency-Meldung.
11. Bei neuer Rolle: Attribut, `controller.md` und `RoleOverview.cshtml` enthalten sie.
12. Bestehende Liste, Druck und PDF der Vorschlagsliste (Einzel- und Summiert-Sicht) funktionieren unveraendert;
    Benutzer ohne Schreibrecht sehen dort keinen Anlege-Button (Regression TS-60/TS-74).

## Test-Szenarien

Neues Kapitel (naechste freie Nummer), Manual-UAT am IDEAL-Testsystem mit beschichteten Positionen
(Vorbedingung wie TS-60: produktivnahe Daten):
- Happy Path: HauptFA in der summierten Sicht → „Aus Vorschlagsliste erstellen" → Detail (alle Positionen) →
  Menge aendern, eine Position entfernen, Dienstleister ueberschreiben → uebergeben → drucken/PDF (Briefkopf,
  summiert, Maße, Barcode = Nummer) → erledigt.
- Entwurf: Druck-/PDF-Knopf fehlt; direkter Aufruf `Print`/`Pdf` → WarningMessage.
- Nach `Uebergeben`: nur Bemerkung editierbar; direkter POST auf Kopf/Menge → Ablehnung.
- Doppelvergabe: Gruppenkopf zeigt Auftragsnummer statt Button; manipulierter POST → Ablehnung.
- Sync zwischen Seitenaufbau und Anlegen (HauptFA ohne beschichtete Positionen) → Ablehnung mit Klartext.
- Kombigeraet → Anlage gelingt, Kopffelder mit abweichenden Werten leer, Hinweis „Kopf aus Sage mehrdeutig".
- Reklamation aus `Erledigt` → keine Vorauswahl; leere Auswahl → Ablehnung; zwei Teile waehlen → Folgeauftrag
  mit genau diesen Teilen, Dienstleister ueberschreiben, Verweise in beiden Richtungen, Vermerk am Ursprung.
- ~~Positionen waehlen → anlegen → Kreditor setzen~~, ~~mehrere HauptFA~~ *(verworfen mit Antworten 2, 4 und
  Teil-3-Antwort 5)*
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

- **Web-App:** ja (Controller, Views, Filter, Layout, Rolle).
- **Service:** nein (nur Versions-Bump).
- **Migration:** nein — setzt die Teil-3-Tabellen voraus (Teil 3 vorher deployt). ~~ja —
  `SQL/XX_AddBeschichtungsauftragCreditor.sql` + History-Insert; setzt `dbo.Creditors` voraus~~ *(verworfen mit
  Antwort 4)*
- **Voraussetzung:** Teil 2 (Briefkopf-Partial) gemergt und Firmendaten/Logo gepflegt.
- **Betrieb:** neue Schreibrolle den zustaendigen Benutzern zuweisen (sonst sieht niemand die Buttons).
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Offene Rueckfragen

1. **Doppelvergabe in der Vorschlagsliste (Notiz Punkt 5):** markieren (mit Auftragsnummer, nicht waehlbar)
   oder ausblenden (ggf. Schalter „vergebene zeigen")? Muss zur Teil-3-Antwort 4 passen.
   *Empfehlung:* markieren, nicht ausblenden — sonst ist nicht erkennbar, warum ein Teil fehlt.
   *Beantwortet mit Antwort 1: markieren mit Auftragsnummer, erneut nur ueber Reklamation.*
2. **Auswahl:** einzelne Positionen, ganze HauptFA-Gruppe oder beides? Nur aktuelle Seite?
   *Beantwortet mit Antwort 2: alle Positionen des HauptFA automatisch, danach im Entwurf editierbar.*
   *Empfehlung:* ~~Checkbox je Position plus Gruppen-Checkbox; Auswahl gilt nur fuer die aktuelle Seite (die
   Seiteneinheit ist ohnehin die HauptFA-Gruppe) — kein seitenuebergreifender Merkzettel.~~ *(verworfen mit
   Antwort 2)*
3. **Reklamation:** Ablauf (laut Teil-3-Antwort 2 Folgeauftrag) und welche Details „leicht editierbar":
   Positionen entfernen/hinzufuegen, Menge, Reklamationsgrund/Bemerkung, Dienstleister, Termine?
   *Beantwortet mit Antwort 3: Teile auswaehlen, nichts vorbelegen, Vermerk am Ursprung (Kopf-Vorbelegung →
   Teil-3-Rueckfrage 13).*
   *Empfehlung:* ~~Folgeauftrag mit allen Positionen des Ursprungs vorbelegt~~ *(verworfen mit Antwort 3)*, im
   Entwurf alles editierbar (Positionen entfernen, Menge, Grund, Dienstleister, Termine); Positionen
   hinzufuegen nur aus dem Ursprung.
4. **Dienstleister aus dem Kreditoren-Stamm:** ab welchem Status Pflicht? Vorbelegung aus dem Sage-Freitext
   ueber die Zuordnung der Kreditoren-Spec? Sage-Freitext daneben anzeigen?
   *Beantwortet mit Antwort 4 (+ Nachtrag): Vorbelegung aus der PPS-View, Ueberschreiben als Freitext, kein
   Kreditoren-Stamm. Die Pflicht-Frage ist offen geblieben → Rueckfrage 9.*
   *Empfehlung:* Pflicht beim Wechsel nach `Uebergeben`, im Entwurf optional; ~~Vorbelegung nur bei eindeutiger
   Zuordnung~~ *(verworfen mit Antwort 4)*; Sage-Freitext immer daneben sichtbar.
5. **Briefkopf/Empfaenger im Auftragsdruck:** Ist Teil 2 Pflicht vor Teil 4 (dann `depends_on` um Teil 2
   ergaenzen), und kommt die Empfaengeradresse aus dem Kreditor?
   *Beantwortet mit Antwort 5: Briefkopf Pflicht (depends_on Teil 2), Empfaenger = Beschichter des Auftrags.*
   *Empfehlung:* ja zu beidem — der gespeicherte Auftrag ist das Aussendokument; ohne Briefkopf waere es
   unvollstaendig ([[2026-09-17-druck-briefkopf-logo-adresse]]). ~~Empfaengeradresse aus dem Kreditor~~
   *(verworfen mit Antwort 4/5 — Empfaenger ist der Freitext)*
6. **Positionsdarstellung im Druck:** summiert nach dem Teil-1-Schluessel (dann Teil 1 als weitere
   Abhaengigkeit) oder Einzelpositionen? Barcode = Auftragsnummer?
   *Beantwortet mit Antwort 6: summiert, Maße angedruckt, Barcode = Auftragsnummer.*
   *Empfehlung:* summiert je HauptFA nach dem Teil-1-Schluessel, Barcode = Auftragsnummer ~~(ein Auftrag kann
   mehrere HauptFA enthalten, Teil-3-Antwort 5)~~ *(Begruendung ueberholt: Teil-3-Antwort 5 = ein HauptFA je
   Auftrag)*.
7. **Bearbeitbarkeit je Status:** Kopf und Positionen nur im `Entwurf`, danach nur Statuswechsel und
   Bemerkung?
   *Beantwortet mit Antwort 7: ja.*
   *Empfehlung:* ja.
8. **Drucken im Entwurf:** erlaubt? Mit Kennzeichnung „ENTWURF"?
   *Beantwortet mit Antwort 8: nein, gedruckt wird ab „Uebergeben".*
   *Empfehlung:* ~~erlaubt (Ablauf „speichern, ausdrucken"; Freigabe kommt spaeter), ohne Wasserzeichen —
   sonst druckt der heutige Ablauf nur gekennzeichnete Dokumente. Mit dem Freigabe-Schritt neu bewerten.~~
   *(verworfen mit Antwort 8)*
9. **Dienstleister Pflicht (neu 2026-10-07):** Antwort 4 klaert Quelle und Ueberschreiben, nicht die Pflicht.
   Muss der Dienstleister — er ist der Empfaenger des Aussendokuments (Antwort 5), und gedruckt wird ab
   `Uebergeben` (Antwort 8) — beim Wechsel nach `Uebergeben` gefuellt sein?
   *Empfehlung:* ja, Pflicht beim Wechsel nach `Uebergeben` (Workflow lehnt mit Klartext ab), im Entwurf
   optional — dann kann ein Aussendokument nie ohne Empfaenger entstehen.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Doppelvergabe-Darstellung → **Nicht erneut wählbar außer über Reklamation (Keyuser 2026-10-07). Darstellung: markieren mit Auftragsnummer (Vorschlag Koordination).**
2. Auswahl-Einheit / Seite → **Vorschlagsliste automatisch übernehmen (alle Positionen des HauptFA), danach im Entwurf editierbar (Keyuser 2026-10-07) – keine Einzelauswahl per Häkchen.**
3. Reklamation: Ablauf und editierbare Details → **Teile auswählen, nichts vorbelegen; Vermerk am Ursprungsauftrag (Keyuser 2026-10-07).**
4. Kreditor: Pflicht / Vorbelegung / Sage-Text → **Standard-Beschichter aus der PPS-View (`FaHierarchyOrderInfo.Dienstleister`), am Auftrag überschreibbar – besonders bei Reklamation (Keyuser 2026-10-07). Offen: Überschreiben als Freitext oder Auswahl aus dem Kreditoren-Stamm (der ist zurückgestellt).** **Nachtrag (Gerald 2026-10-07): Überschreiben als Freitext genügt; Teil 4 hängt damit nicht am Kreditoren-Stamm (depends_on auf die Kreditoren-Spec entfällt, Auswahl aus dem Stamm später).**
5. Briefkopf-Pflicht und Empfaengeradresse → **Wie vorgeschlagen (Gerald 2026-10-07): Briefkopf aus Teil 2 Pflicht (depends_on Teil 2), Empfänger = Beschichter des Auftrags.**
6. Positionsdarstellung im Druck / Barcode → **Wie vorgeschlagen (Gerald 2026-10-07): summiert nach dem Teil-1-Schlüssel, Maße angedruckt, Barcode = Auftragsnummer.**
7. Bearbeitbarkeit je Status → **Kopf und Positionen nur im Entwurf; danach Status und Bemerkung änderbar (Gerald 2026-10-07).**
8. Drucken im Entwurf → **Nein – im Entwurf nicht druckbar (Gerald 2026-10-07) (abweichend vom Vorschlag). Gedruckt wird ab „Übergeben“.**
9. Dienstleister Pflicht beim Wechsel nach Uebergeben →
