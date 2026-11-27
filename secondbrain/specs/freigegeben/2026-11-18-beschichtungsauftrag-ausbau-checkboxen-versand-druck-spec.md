---
type: spec
title: "Beschichtungsauftrag: Verpackt-Haken im Vorschlag und am Auftrag, freie Positionen, Status Versendet, Kreditor-Anschrift, Druck überarbeitet"
slug: 2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck-spec
status: Testbereit
created: 2026-11-18
updated: 2026-11-18
source_backlog: "[[2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck]]"
task: "[[2026-11-18-goal-lauf-beschichtungsauftrag-ausbau]]"
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
affected_code:
  - IdealAkeWms/Models/BeschichtungsauftragStatus.cs (neuer Wert Versendet = 35)
  - IdealAkeWms/Models/Beschichtungsauftrag.cs (CreditorId, VersendetAt, VersendetByUserId)
  - IdealAkeWms/Models/BeschichtungsauftragPosition.cs (Verpackt, OhneFa)
  - IdealAkeWms/Data/ApplicationDbContext.cs (Konfiguration der neuen Spalten, FK auf Creditors)
  - IdealAkeWms/Migrations/<ts>_<Name>.cs (neu)
  - SQL/<NNN>_<Name>.sql (neu, Nummer beim Dev-Start prüfen)
  - SQL/00_FreshInstall.sql (Abschnitt 17j + MigrationId)
  - IdealAkeWms/Data/Repositories/IBeschichtungsauftragRepository.cs / BeschichtungsauftragRepository.cs (GetVergabenAsync ohne OhneFa-Zeilen)
  - IdealAkeWms/Services/BeschichtungsauftragWorkflow.cs (Übergänge, Anlegen mit Auswahl, Bearbeiten bis vor Versendet, Verpackt, freie Positionen, Kreditor)
  - IdealAkeWms/Controllers/BeschichtungsauftraegeController.cs (Create mit Auswahl, Bearbeiten, Details-Kandidaten, Print-Modell)
  - IdealAkeWms/Models/ViewModels/BeschichtungsauftraegeViewModels.cs (ListRow, Detail, Print, IstDruckbar)
  - IdealAkeWms/Models/ViewModels/PrintBriefkopfModel.cs (Brief-Anschrift)
  - IdealAkeWms/Views/Shared/_PrintBriefkopf.cshtml (Empfänger mehrzeilig)
  - IdealAkeWms/Views/Beschichtungsauftraege/Details.cshtml
  - IdealAkeWms/Views/Beschichtungsauftraege/Index.cshtml (Spalte Versendet am)
  - IdealAkeWms/Views/Beschichtungsauftraege/Print.cshtml
  - IdealAkeWms/Views/Beschichtungsauftraege/_StatusBadge.cshtml
  - IdealAkeWms/Views/FaHierarchyBeschichtung/Index.cshtml (Haken hinten)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/Summiert.cshtml (Haken hinten)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/_VorschlagAuftraege.cshtml (Formular-Id, freie Positionen)
  - IdealAkeWms/Models/ViewModels/FaHierarchyBeschichtungViewModels.cs (Summiert-Zeile: freie Positionen der Zeile)
  - IdealAkeWms/Services/BeschichtungsauftragService.cs (nur falls die Summiert-Zeile die Zahl freier Positionen braucht)
  - IdealAkeWms/Services/ColumnFilterSyntax.cs (Datumsspalte versendetam)
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (Beschichtungsauftraege, BeschichtungsauftragPositionen)
  - IdealAkeWms/Views/Shared/_Select2ArticlePartial.cshtml (nur wiederverwendet)
  - IdealAkeWms/Data/Repositories/CreditorRepository.cs (nur gelesen)
  - IdealAkeWms/Services/CreditorAssignment.cs (nur Regel wiederverwendet, Rückfrage 17)
  - IdealAkeWms/Views/Help/* (Kurzanleitung Beschichtung, Changelog)
  - IdealAkeWms.Tests (Workflow-, Controller-, View-Drift-Tests)
  - scripts/deploy/nachlauf.psd1
  - docs/TESTSZENARIEN.md
open_questions:
  - "1 (R1) Bedeutung des Hakens: verpackt / zum Versand vorbereitet, je Position am Auftrag gespeichert, im Vorschlag Auswahl für die Anlage?"
  - "2 (R2) Statusfolge Entwurf → Übergeben → Versendet → Erledigt; direkter Übergang Übergeben → Erledigt entfällt; Positionen änderbar bis vor Versendet?"
  - "3 (R3) Freie Positionen = Artikel aus dem Artikelstamm (Suche) + Menge, Kennzeichen „ohne FA“, kein Freitext-Artikel?"
  - "4 (R4) Lackierer = Kreditor mit IsCoater (aktiv), Anschrift aus dem Kreditor, Freitext Dienstleister nur als Rückfall?"
  - "5 (R5) Gilt für AKE und IDEAL gleich?"
  - "6 Versendet nur, wenn alle Positionen verpackt sind (harte Sperre)?"
  - "7 Haken auch in der Summiert-Ansicht (Standardansicht): ein Haken = alle freien Positionen dieser Artikelnummer im HauptFA?"
  - "8 Haken in der Vorschlagsliste werden nicht gespeichert (gehen bei Neuladen/Filterwechsel verloren) - einverstanden?"
  - "9 Freie Positionen im Vorschlag: Eingabe im Gruppenkopf (Artikel + Menge), gelten als verpackt; Maße aus ArticleSageDetails × 10?"
  - "10 Was ist im Status Übergeben änderbar: nur Positionen (hinzufügen, entfernen, Menge, Verpackt) oder auch der Kopf (Kreditor, RAL, Termine)?"
  - "11 Druck/PDF je Position statt summiert je Artikelnummer (hebt Teil-4-Antwort 6 auf)?"
  - "12 Positionsnummer im Druck = laufende Nummer 1..n oder Stücklisten-Position?"
  - "13 Menge im Druck: ganze Zahlen ohne Nachkommastellen, Bruchteile sichtbar statt gerundet; nur Druck/PDF?"
  - "14 Brief-Anschrift: zum Druckzeitpunkt live aus dem Kreditor (keine Kopie am Auftrag); Sage-Herkunft = Lieferanschrift (LieferStrasse/-PLZ/-Ort/-Land) richtig?"
  - "15 Altaufträge: bestehende Positionen bleiben unverpackt (kein Datenskript)?"
  - "16 Bemerkung: Prüfschritt P-1 plus Hinweis bei ungespeicherter Bemerkung vor Drucken/PDF/Statuswechsel?"
  - "17 Kreditor-Vorbelegung an der IDEAL aus dem Sage-Dienstleistertext über den Kurznamen (Regel CreditorAssignment)?"
epic: true
etappen:
  - "E1 Schema + Status Versendet (Migration, SQL, FreshInstall, Übergänge, Badge, Statusverlauf, Liste)"
  - "E2 Auftrag bearbeiten bis vor Versendet (Verpackt-Haken, Positionen in Übergeben, freie Positionen am Auftrag, Versendet-Regel)"
  - "E3 Vorschlagsliste: Haken hinten (Liste + Summiert), Anlage nur angehakter Positionen, freie Positionen im Vorschlag"
  - "E4 Lackierer aus Kreditoren (IsCoater), Vorbelegung, Brief-Anschrift"
  - "E5 Druck/PDF überarbeiten (je Position, Pos.-Nr., Menge, HauptFA im Kopf, Verpackt-Spalte, Bemerkung)"
  - "E6 Doku, Testszenarien, Hilfe, Changelog, Manifest, Brain"
deploy:
  web: true
  service: false
  migration: true
freigabe_entscheidung: "R1–R5 wie Backlog-Antworten 11:48; Rückfragen 6–17 wie Empfehlung (12:55)"
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-18"
---

## Ziel / Nutzen (das Warum)

Der Fachbereich packt die Lackierteile für den Beschichter und braucht dafür eine Liste, auf der er abhakt, was in der
Kiste ist. Heute übernimmt „Aus Vorschlagsliste erstellen“ **alle** freien Positionen eines HauptFA (Teil-4-Antwort 2,
`BeschichtungsauftragWorkflow.cs:95-137`). Nach dem Übergeben ist nichts mehr änderbar außer Bemerkung und Status
(Teil-4-Antwort 7, `:184-186`, `:255-256`). Es gibt keinen Zustand für „Ware ist unterwegs“. Der Ausdruck summiert je
Artikelnummer, zeigt eine Anzahl-Spalte, die wie eine Positionsnummer aussieht, und hat als Empfänger nur einen
Freitext ohne Anschrift.

Nutzen:

1. Der Haken „verpackt“ steuert, was in den Auftrag kommt, und begleitet die Position bis zum Versand.
2. Teile ohne FA-Bezug (z. B. Ersatzteile, Muster) können mitgeschickt werden.
3. Der Status **Versendet** trennt „beim Lackierer“ von „übergeben, aber noch im Haus“.
4. Das Aussendokument ist ein Brief an den Lackierer: Anschrift aus dem Kreditor, Positionsnummer vorne, Haken hinten,
   ganzzahlige Mengen, Bemerkung zuverlässig gedruckt.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**

- Haken je Zeile **hinten** in der Vorschlagsliste (Ansicht Liste und Summiert). Nur angehakte Positionen gehen in den
  neuen Auftrag.
- Freie Positionen (Artikel aus dem Artikelstamm + Menge, Kennzeichen „ohne FA“) im Vorschlag und am Auftrag.
- Am Auftrag: Haken „verpackt“ je Position änderbar; Positionen hinzufügen, entfernen, Menge ändern bis vor Versendet.
- Neuer Status **Versendet** zwischen Übergeben und Erledigt, mit Zeitpunkt und Benutzer, in Liste, Filter, Badge,
  Statusverlauf.
- Lackierer als Auswahl aus den Kreditoren mit `IsCoater`; Freitext `Dienstleister` bleibt Rückfall.
- Druck/PDF: Spalte „Positionen“ weg, Positionsnummer in Spalte 1, Menge ganzzahlig, HauptFA im Kopfblock, Brief-Anschrift
  aus dem Kreditor, Haken-Spalte hinten, Bemerkung (Befund + Absicherung).
- Beide Standorte (R5).

**Out-of-Scope (nicht Teil davon)**

- Kistenzettel ([[2026-11-17-beschichtung-kistenzettel]], wartet auf Beispiel von Till).
- Freitext-Artikel ohne Artikelstamm (R3: Artikelsuche).
- „Alle anhaken“ je Gruppe. Kann nachgerüstet werden, wenn lange Gruppen das Abhaken zäh machen.
- Haken in der Vorschlagsliste speichern, bevor ein Auftrag existiert (Rückfrage 8).
- Neue Rolle. Gepackt und versendet wird mit `beschichtungsauftrag_schreiben`.
- Reparatur des IDEAL-Kreditorenabgleichs ([[2026-11-18-sage-userfelder-als-einstellung-spec]], v1.99.40 eigene Spec).
- Stornieren aus Übergeben/Versendet; Reklamation aus Versendet. Die Regeln bleiben wie bisher: Storno nur aus Entwurf,
  Reklamation nur aus Erledigt.
- Mengenformat außerhalb von Druck/PDF (Liste, Detail, Vorschlag), sofern Rückfrage 13 nichts anderes entscheidet.
- Sage-Schreibzugriffe jeder Art.

## Fachliche Anforderungen

### Ist-Stand (belegt; Pfade relativ zum Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Stand v1.99.40)

**Status und Übergänge**

- Enum `Entwurf = 10`, `Uebergeben = 30`, `Erledigt = 40`, `Storniert = 90`; 20 reserviert für „Freigegeben“
  (`Models/BeschichtungsauftragStatus.cs:7-17`). Gespeichert als `tinyint` ohne CHECK-Constraint (Byte-Enum, kein
  `HasConversion`, `Data/ApplicationDbContext.cs:1755-1780`; keine CK-Fundstelle in `SQL/`).
- Erlaubte Übergänge: Entwurf→Übergeben, Übergeben→Erledigt, Entwurf→Storniert (`Services/BeschichtungsauftragWorkflow.cs:52-58`).
  Übergeben nur mit Dienstleister (`:327-328`). Zeitpunkt und Benutzer je Schritt in `UebergebenAt/ByUserId` usw.
  (`:333-338`, Model `Beschichtungsauftrag.cs:43-48`).
- Statusverlauf in der Detailseite ist eine feste Liste aus drei Schritten (`Views/Beschichtungsauftraege/Details.cshtml:71-76`).
  Badge-Farbe je Status in `_StatusBadge.cshtml:5-11` (unbekannte Werte `bg-dark`).
- Druckbar nur Übergeben und Erledigt (`Models/ViewModels/BeschichtungsauftraegeViewModels.cs:124-125`).
- Statusfilter der Liste läuft über `Enum.GetValues` (`Views/Beschichtungsauftraege/Index.cshtml:27-30`); der Spaltenfilter
  `status` über `Anzeige()` (`BeschichtungsauftraegeViewModels.cs:48`). Ein neuer Enum-Wert erscheint dort ohne Zusatzcode.
- Sperrende Vergaben = alle Positionen in Aufträgen mit Status ≠ Storniert (`Data/Repositories/BeschichtungsauftragRepository.cs:37-44`).
  Versendet sperrt damit automatisch.

**Anlage aus der Vorschlagsliste**

- Knopf „Aus Vorschlagsliste erstellen“ je HauptFA-Gruppe (`Views/FaHierarchyBeschichtung/_VorschlagAuftraege.cshtml:19-27`).
  Er sendet nur `hauptFa` und `zurueck`; der Controller ruft `AnlegenAsync(hauptFa)` (`Controllers/BeschichtungsauftraegeController.cs:139-155`).
- `AnlegenAsync` übernimmt alle beschichteten, nicht vergebenen Positionen (`BeschichtungsauftragWorkflow.cs:95-137`).
- Die Liste (`Views/FaHierarchyBeschichtung/Index.cshtml:56-125`) zeigt je Position eine Zeile ohne Haken; vergebene
  Positionen tragen ein Badge „in BA-…“ (`:112-115`).
- Die Summiert-Ansicht (Standard seit v1.48.0, `Summiert.cshtml`) fasst je `(HauptFA, Artnr)` zusammen
  (`Services/BeschichtungsauftragService.cs:219-242`). Die Zeile trägt **keine** Positionsschlüssel
  (`Models/ViewModels/FaHierarchyBeschichtungViewModels.cs:64-81`).
- Beide Ansichten pagen je HauptFA-Gruppe. Eine Gruppe liegt nie auf zwei Seiten (`BeschichtungsauftragService.cs:246-252`;
  Liste `FaHierarchyListBuilder.cs:141-161` laut [[2026-11-16-beschichtungslogik-je-standort-spec]]). Haken einer Gruppe
  lassen sich deshalb mit einem Absenden übermitteln.
- Vorbild für Eingaben außerhalb des `<form>`: `form="entwurf-form"` (`Details.cshtml:126-128`, `:245`, `:257`).
- Vorbild „Haken je Zeile“: Kommissionierliste `Views/Picking/Bom.cshtml:241-244` (`picking-checkbox`, sofort per API).

**Bearbeiten**

- Kopf, Mengen und Entfernen nur im Entwurf (`BeschichtungsauftragWorkflow.cs:178-214`), Hinzufügen nur im Entwurf
  (`:250-256`). Kandidaten lädt der Controller nur im Entwurf (`BeschichtungsauftraegeController.cs:105-108`).
- Mengen > 0 Pflicht (`:190-191`). Entfernte Positionen sind sofort wieder frei (`:207-211`).
- Bemerkung ist in jedem Status außer Storniert änderbar (`:304-317`), aber über ein **eigenes** Formular
  (`Details.cshtml:191-196`). Der Kopf/Positionen-Entwurf (`:126-128`) und jeder Statusknopf (`:102-114`) sind getrennte
  Formulare.

**Positionsschlüssel und Vergabe**

- `PositionsSchluessel(VaterFA, SubFA, Position, Artnr?, PositionText?)` (`Data/Repositories/IBeschichtungsauftragRepository.cs:13-28`).
  Ohne `PositionText` (IDEAL) sind Artnr und Text null.
- Eine Position ohne Strukturherkunft hätte den Schlüssel `(null, 0, null)`. Der kann mit einem IDEAL-Knoten mit
  `VaterFA = null, SubFA = 0, Position = null` zusammenfallen. Ob es solche Knoten gibt, ist nicht geprüft und auch nicht
  nötig: freie Positionen bekommen ein eigenes Kennzeichen und laufen nicht durch die Vergabe (siehe Soll A7).
- Wo `PositionsSchluessel.Von(p)` aus Auftragspositionen gebildet wird: `BeschichtungsauftragWorkflow.cs:225`, `:231`,
  `:259`, `:268`; Repository `:42`; Detail-View über `SchluesselText`.

**Kreditor / Lackierer**

- `Beschichtungsauftrag.Dienstleister` ist Freitext(200) (`Models/Beschichtungsauftrag.cs:24-25`). Im Entwurf änderbar
  (`Details.cshtml:140-145`). An der IDEAL wird er aus der PPS-View vorbelegt (`BeschichtungsauftragWorkflow.cs:424-445`),
  an der AKE bleibt er leer (`:127-130`).
- `Creditor` hat `Name`, `ShortName`, `Street`, `PostalCode`, `City`, `Country`, `IsCoater`, `IsActive` (`Models/Creditor.cs:15-68`).
  Kreditoren werden nie gelöscht, nur deaktiviert (`:11`, Repository ohne Delete `Data/Repositories/CreditorRepository.cs:6`).
- **Herkunft der Adressfelder** (alle Schreibwege):
  - Manuell (`Source = Manual`): `CreditorsController.cs:246-249` (`Normalize(src.Street)` usw.).
  - Sage-Lese-Abgleich Kontokorrent (Dienst): `IDEALAKEWMSService/Services/Sdata/KontokorrentSyncService.cs:318-321`.
    Geschrieben wird `Street ← LieferStrasse`, `PostalCode ← LieferPLZ`, `City ← LieferOrt`, `Country ← LieferLand`.
    Das sind die SData-Felder der Adresse (`SelectAdressen`, `:70`). Ein leerer Sage-Wert überschreibt nicht (`:305`, „F1:
    null = nicht schreiben“), ein zu langer wird gemeldet, nicht gekürzt (`:306-317`).
  - Es gibt **keinen** weiteren Schreiber dieser vier Felder im Code (Suche `.(Street|PostalCode|City|Country) =` über
    Web, Dienst, Tests: nur diese beiden Stellen plus Testdaten).
  - Ob `Liefer*` in Sage die Anschrift ist, an die ein Brief gehen soll, oder eine abweichende Lieferanschrift, steht
    nur in Sage → Prüfabfrage P-2, Rückfrage 14.
- `IsCoater` ist ein reines WMS-Feld. Es wird nur manuell gesetzt (`CreditorsController.cs:260`, Vorbelegung bei Neuanlage
  aus der Dienstleister-Meldung `:137`). Der Abgleich schreibt es nie (Test `KontokorrentArtikelLieferantTests.cs:66`).
  Die Lackierer-Auswahl ist also nur so gut wie die Pflege von „Beschichter“ in `/Creditors`.
- Freitext→Kreditor-Zuordnung existiert schon: `CreditorAssignment.FindUnassigned` über `ShortName`, exakt, getrimmt,
  Groß/Klein egal (`Services/CreditorAssignment.cs:14-34`).
- IDEAL: Der Kreditoren-Abgleich scheiterte an `USER_EKMindestbestellwert`. Das behebt v1.99.40
  ([[2026-11-18-sage-userfelder-als-einstellung-spec]]). Bis dahin können IDEAL-Kreditoren fehlen.

**Druck / PDF**

- Bildschirmdruck (`Print`) und PDF (`Pdf`) rendern **dieselbe** View `Print.cshtml` aus **demselben** Modell
  (`BeschichtungsauftraegeController.cs:245-286`; PDF über `IRazorViewRenderer.RenderToStringAsync(..., "Print", vm)`
  `:258`). Edge erzeugt das PDF headless aus dieser HTML-Datei (`Services/PdfRenderService.cs:49-60`). Druck und PDF
  verwenden also beide die Print-Media-Regeln.
- Kopf: `<h1>Beschichtungsauftrag {Nummer} — HauptFA {HauptFA}</h1>` (`Print.cshtml:46`), Kopftabelle RAL/Start/Retour/
  Reklamation (`:48-56`), Bemerkung nur wenn gefüllt (`:57-60`, Klasse `.bemerkung`, `white-space: pre-line` `:32`).
  Der Briefkopf zeigt zusätzlich „Dokument … — HauptFA …“ (`Views/Shared/_PrintBriefkopf.cshtml:29`).
- Empfänger = `Dienstleister`-Text, einzeilig; leer → „Empfänger fehlt — am Auftrag nachtragen“
  (`Models/ViewModels/PrintBriefkopfModel.cs:20-43`, `_PrintBriefkopf.cshtml:28`).
- Positionen **summiert je Artnr** über `BeschichtungsauftraegeDruck.Summiere` → `AggregateSummiert`
  (`BeschichtungsauftraegeViewModels.cs:129-137`).
- Spalte „Positionen“ = `row.PositionCount` (Anzahl summierter Positionen, `Print.cshtml:73`, `:93`). `ShowCol` druckt sie
  immer, weil der Key `positionen` nicht im Spaltenkatalog `BeschichtungsauftragPositionen` steht
  (`BeschichtungsauftraegeViewModels.cs:116-118`, `ColumnDefinitions.cs:629-644`).
- Menge `FormatMenge` = `"N3"` → „5,000“ (`Services/BeschichtungsauftragService.cs:80`). `Menge` ist `decimal(18,4)`
  (`ApplicationDbContext.cs:1790`) und wird beim Bearbeiten als Dezimalzahl angenommen (`BeschichtungsauftraegeController.cs:319-320`).
  Nicht ganzzahlige Mengen sind also möglich.

### Befund Bemerkung „Druck nein, PDF ja“ (aus dem Code; Ursache am Gerät unbelegt)

1. **Kein Codeunterschied zwischen Druck und PDF.** Gleiche View, gleiches Modell, gleiche Datenquelle
   (`GetDetailAsync`, ungetrackt aus der DB, `BeschichtungsauftragRepository.cs:83-90`), beide mit Print-Media. Keine
   Druck-CSS-Regel blendet `.bemerkung` aus. Die einzigen `@media print`-Regeln betreffen `.screen-controls`
   (`Print.cshtml:33`), `.briefkopf-hinweis` (`_PrintBriefkopfStyles.cshtml:18`) und die Fußzeile (`:31-35`).
2. **Belegter stiller Verlustweg:** Die Bemerkung hat ein eigenes Formular (`Details.cshtml:191-196`). Wer sie eintippt
   und dann „Entwurf speichern“ (`:270`), einen Statusknopf (`:102-114`) oder „Drucken“ (`:27`, Link, öffnet neuen Tab)
   drückt, verliert sie bzw. druckt den alten Stand. Es gibt weder einen Hinweis noch eine Rückfrage. Speichert der
   Benutzer danach und lädt das PDF, ist die Bemerkung da. Das passt zum gemeldeten Bild.
3. **Weitere mögliche Ursachen, nicht im Repo prüfbar:**
   - Ein Druck-Tab, der vor dem Speichern geöffnet wurde und erneut gedruckt wird (Seite nicht neu geladen).
   - CSS der Word-Briefkopfvorlage. Sie wird ungefiltert eingebettet (`_PrintBriefkopfStyles.cshtml:38-44`), der Pfad
     kommt aus der Einstellung `BriefkopfVorlagePfad` (`Services/BriefkopfVorlageService.cs:72`). Eine allgemeine Regel
     dort (z. B. auf `div`) könnte im Bildschirmdruck anders wirken als in Edge headless.
4. Daraus: Prüfschritt **P-1** (Reproduktion) und die Absicherung A12 (Rückfrage 16).

### Soll

| ID | Anforderung | Quelle |
|---|---|---|
| A1 | **Haken hinten je Zeile in der Vorschlagsliste**, Ansicht Liste: eine Checkbox je Position in einer neuen letzten Spalte „Verpackt“. Vergebene Positionen (Badge „in BA-…“) sind nicht anhakbar (Checkbox `disabled` + Text). Die Checkboxen gehören über `form="besch-create-{HauptFA}"` zum Anlege-Formular ihrer Gruppe. | Backlog 1, R1 |
| A2 | Ansicht **Summiert**: ein Haken je Zeile `(HauptFA, Artnr)` = alle **freien** Positionen dieser Artnr im HauptFA. Ist die Zeile ganz vergeben: nicht anhakbar. Ist sie teilweise vergeben: anhakbar, Hinweis „n von m frei“. Der Server erweitert die Artnr auf die freien Positionen. | Rückfrage 7 |
| A3 | **Anlage nur mit Auswahl:** `AnlegenAsync(hauptFa, schluessel, artnrSummiert, freiePositionen)` legt den Entwurf nur mit den angehakten Positionen an. Kein Haken und keine freie Position → Fehler „Keine Position angehakt — nichts angelegt.“ Prüfung je Schlüssel wie `PositionenHinzufuegenAsync` (im Cache, beschichtet, nicht vergeben, `:282-293`). Eine teilweise ungültige Auswahl wird ganz abgelehnt, mit Liste. Angelegte Positionen tragen `Verpackt = true`. Der Rest bleibt frei und erscheint weiter in der Vorschlagsliste. *Ersetzt Teil-4-Antwort 2 (keine Einzelauswahl).* | Backlog 1, R1 |
| A4 | **Freie Positionen im Vorschlag:** Im Gruppenkopf (Anlege-Formular) „+ Position ohne FA“: Artikelsuche (`_Select2ArticlePartial`, `/api/articles/search`) + Menge > 0, beliebig viele Zeilen. Sie werden mit angelegt. | Backlog 2, R3, Rückfrage 9 |
| A5 | **Neuer Status `Versendet = 35`**, Anzeige „Versendet“. Übergänge: Entwurf→Übergeben, **Übergeben→Versendet**, **Versendet→Erledigt**, Entwurf→Storniert. Übergeben→Erledigt entfällt. Zeitpunkt/Benutzer in `VersendetAt` / `VersendetByUserId`. Der Statusverlauf zeigt vier Schritte. Badge mit Text (Farbe nie allein, Kontrast ≥ 4,5:1). | Backlog 10, R2 |
| A6 | **Versendet nur, wenn alle Positionen verpackt sind** und mindestens eine Position existiert. Sonst Fehler mit den unverpackten Positionen (Artnr/Pos.); Abhilfe im Text: „anhaken oder entfernen“. | Rückfrage 6 |
| A7 | **Freie Positionen** (`OhneFa = true`): `HauptFA` = Auftrag, `VaterFA = null`, `SubFA = 0`, `Position = null`, `PositionText = null`, `Artnr`/`Matchcode` aus dem Artikelstamm, `HauptArtnr = null`, Menge eingegeben, Maße laut Rückfrage 9. Sie zählen **nie** als Vergabe (`GetVergabenAsync` filtert `!OhneFa`), stehen nicht in den „eigenen Schlüsseln“ der Kandidaten-/Doppelprüfung (`Workflow.cs:225`, `:259`) und dürfen mehrfach (auch gleicher Artikel) vorkommen. Anzeige „ohne FA“ als Text-Badge statt Herkunft. Reklamation kopiert sie mit (`Kopie` übernimmt `OhneFa`). | Backlog 2, R3 |
| A8 | **Am Auftrag bis vor Versendet** (Entwurf und Übergeben): Haken „Verpackt“ je Position, Menge, Entfernen, Positionen hinzufügen (Kandidaten wie heute + freie Positionen). Alles in **einem** Formular mit einem Knopf „Speichern“, RowVersion wie heute. In Übergeben ist der Kopf laut Rückfrage 10 gesperrt oder offen. Ab Versendet ist nur noch Bemerkung und Status änderbar. | Backlog 10, R1, R2, Rückfrage 10 |
| A9 | **Lackierer aus Kreditoren:** neues Feld `Beschichtungsauftrag.CreditorId` (FK `Creditors`, nullable). Auswahlliste = aktive Kreditoren mit `IsCoater`, Text „Kurzname — Name, Ort“. Der Server prüft beim Speichern: Kreditor existiert, aktiv, `IsCoater`. Mit Kreditor setzt der Workflow `Dienstleister = Creditor.Name` (Text bleibt für Liste, Filter, Altbestand). Ohne Kreditor bleibt der Freitext `Dienstleister` (Rückfall); die Pflicht „Dienstleister vor Übergeben“ (`:327-328`) gilt unverändert. Neben der Auswahl ein Link „Beschichter pflegen“ auf `/Creditors?coater=…` für Benutzer mit Stammdatenrecht. | Backlog 8, R4 |
| A10 | **Vorbelegung IDEAL:** Bei der Anlage, wenn der Sage-Dienstleistertext (`KopfVorbelegen`) genau einem aktiven `IsCoater`-Kreditor über den Kurznamen zugeordnet ist (Regel `CreditorAssignment`), wird `CreditorId` gesetzt. Sonst bleibt nur der Text, Hinweis „Kein Beschichter-Kreditor zu ‚…‘ — Kreditor wählen oder Kurzname pflegen“. AKE: keine Vorbelegung (heute leer, `:127-130`). | Rückfrage 17 |
| A11 | **Druck/PDF:** (a) Positionen **je Position**, nicht summiert. (b) Spalte 1 „Pos.“ = laufende Nummer 1..n in Druckreihenfolge (Reihenfolge wie Detail: `Position`, `PositionText` natürlich, `SubFA`; freie Positionen zuletzt). (c) Spalte „Positionen“ entfällt. (d) Menge ohne Nachkommastellen bei ganzen Zahlen, Bruchteile sichtbar (`0.###`, wie `FormatMass` `BeschichtungsauftragService.cs:83`), keine Rundung. (e) HauptFA als erste Zeile der Kopftabelle; die Überschrift trägt nur die Nummer. (f) Letzte Spalte „Verpackt“ mit ☑ / ☐ und Text-Alternative. (g) Empfänger als Brief-Anschrift: Name, Straße, PLZ Ort, Land (Land nur wenn gefüllt) aus dem Kreditor, zum Druckzeitpunkt gelesen. Ohne Kreditor: Dienstleister-Text wie heute, am Bildschirm Hinweis „Keine Anschrift — Lackierer aus den Kreditoren wählen“ (`.briefkopf-hinweis`, nicht gedruckt). Kreditor ohne Straße/PLZ/Ort: ebenso Hinweis. (h) Druckbar ab Übergeben, also Übergeben, Versendet, Erledigt. | Backlog 3-9, Rückfragen 11-14 |
| A12 | **Bemerkung absichern:** Ist die Bemerkung im Textfeld ungespeichert geändert, zeigen „Drucken“, „PDF“, „Speichern“ und die Statusknöpfe eine Rückfrage „Bemerkung ist nicht gespeichert — trotzdem fortfahren?“; dazu ein sichtbarer Text „nicht gespeichert“ am Feld. Kein `<script>` im Razor-Markup außerhalb `@section Scripts`, kein `@Html.Raw`. Ergibt P-1 eine andere Ursache (Vorlagen-CSS), wird sie zusätzlich behoben und hier als Nachtrag dokumentiert. | Backlog 5, Befund, Rückfrage 16 |
| A13 | **Liste der Aufträge:** neue Spalte „Versendet am“ (`versendetam`, Datumsfilter nach der Projektion, Register `ColumnFilterSyntax.cs:50`). Status-Dropdown und Spaltenfilter `status` zeigen „Versendet“. | Listen-View-Pattern |
| A14 | **Audit:** jede Änderung an Auftrag und Position setzt `ModifiedAt/By/ByWindows` (`Geaendert`, `Workflow.cs:491-496`); neue Positionen `Created*` (`Erstellt`, `:484-489`). | ADR 0003 |

## Technischer Loesungsentwurf

Muster: Schreiben nur über `BeschichtungsauftragWorkflow` (ein Workflow-Aufruf je Request, Controller-Kopf
`BeschichtungsauftraegeController.cs:21-22`), Datenzugriff über `IBeschichtungsauftragRepository` / `ICreditorRepository`
(ADR 0001), Read/Edit-Split unverändert (ADR 0006), Listen-View-Pattern (ADR 0005). Ponytail-Leiter: vorhandene Bausteine
statt neuer.

1. **Modell (E1)**
   - `BeschichtungsauftragStatus.Versendet = 35`; `Anzeige()` braucht keinen Zweig.
   - `Beschichtungsauftrag`: `int? CreditorId`, `Creditor? Creditor`, `DateTime? VersendetAt`, `int? VersendetByUserId`.
   - `BeschichtungsauftragPosition`: `bool Verpackt`, `bool OhneFa`.
   - DbContext: FK `CreditorId → Creditors.Id`, `OnDelete(Restrict)`, Index auf `CreditorId`.
2. **Workflow (E1/E2/E3)**
   - `ErlaubteUebergaenge` laut A5; Fehlertext `:325-326` anpassen.
   - `StatusWechselnAsync`: Zweig Versendet (A6) und `VersendetAt/By`.
   - `EntwurfBearbeitenAsync` → `BearbeitenAsync`: zulässig in Entwurf **und** Übergeben. Zusätzlich `verpackt`-Menge,
     neue freie Positionen und `CreditorId`. Kopfteil laut Rückfrage 10.
   - `PositionenHinzufuegenAsync` und `GetHinzufuegbarePositionenAsync`: Statusprüfung „vor Versendet“ statt „Entwurf“.
     Neue Positionen `Verpackt = false`.
   - `AnlegenAsync`: Auswahlparameter (A3). Die Prüfung bleibt in der Serializable-Transaktion; Schlüsselprüfung aus
     `PositionenHinzufuegenAsync` in einen gemeinsamen privaten Baustein ziehen statt kopieren.
   - `Snapshot` / `Kopie`: `Verpackt` setzen (Anlage `true`, Hinzufügen und Reklamation `false`), `Kopie` übernimmt `OhneFa`.
   - Freie Position: Artikel per `IArticleRepository` (Id → ArticleNumber, Matchcode). Maße laut Rückfrage 9 aus
     `ArticleSageDetails` (Rechenweg wie `BeschichtungFlacheQuelle.Projektion`, Nachtrag 4).
3. **Repository:** `GetVergabenAsync` zusätzlich `&& !p.OhneFa` (`BeschichtungsauftragRepository.cs:39`). `GetDetailAsync`
   und `GetAlleAsync` mit `.Include(a => a.Creditor)` (Druck-Anschrift, Liste unverändert über `Dienstleister`).
4. **Controller:**
   - `Create(int hauptFa, string? zurueck, string[]? schluessel, string[]? artnr, int[]? freiArtikelId, string[]? freiMenge)`;
     Parser `TrySchluessel`, `TryMenge` wiederverwenden.
   - `Bearbeiten` um `verpackt[]`, `creditorId`, freie Positionen erweitert.
   - `Details`: Kandidaten auch in Übergeben (`:106`); Kreditorliste nur mit Schreibrecht laden.
   - `PrintModelAsync`: Zeilen je Position statt `Summiere`.
5. **Views**
   - Vorschlag `Index`/`Summiert`: letzte Spalte, `colCount` +1, `<th>` ohne `data-col-key`, `data-csv-export="false"`
     (Vorbild `Details.cshtml:222`). `_VorschlagAuftraege`: Formular mit `id`, Abschnitt „+ Position ohne FA“.
     **Drift-Wächter:** Die Spaltenkataloge in `ColumnDefinitions` und das `#column-config`-JSON bleiben gleich, weil die
     Haken-Spalte keine konfigurierbare Spalte ist. Ob der View-Drift-Test die Spalte trotzdem erfasst, prüft der
     Dev-Lauf am Test.
   - Detail: Spalte „Verpackt“ (Checkbox bzw. Text „verpackt“/„offen“), Kreditor-Auswahl, Statusverlauf vier Schritte,
     A12-Skript in `@section Scripts`.
   - Print: A11.
   - `_PrintBriefkopf`: Empfänger mehrzeilig. `PrintBriefkopfModel.FuerAuftrag` bekommt optionale Anschriftzeilen; die
     anderen Aufrufer (Vorschlagsliste-Druck) bleiben unverändert.
6. **Frontend-Pflicht:** Der Dev-Lauf ruft den Skill `frontend-design` **explizit** auf (CLAUDE.md). Bootstrap 5 und
   vorhandene Partials gewinnen. Kontrast WCAG AA (4,5:1 Text, 3:1 Bedienelemente). Haken-Zustand nie nur über Farbe
   (Text bzw. ☑/☐ mit `aria-label`). Checkboxen ≥ 24 px Trefferfläche (Terminal in der Halle).

## Migrations-/SQL-Auswirkungen

- **Eine** EF-Migration (Name z. B. `AddBeschichtungsauftragVersandUndVerpackt`). Nummern **nicht** festgelegt: beim
  Dev-Start gegen den Worktree prüfen. Zuletzt belegt bei Spec-Erstellung: **v1.99.40, SQL/160, TS-161, ADR 0033** (die
  Auftragsnotiz nannte v1.99.39 / TS-160 / ADR 0032; der Worktree ist inzwischen weiter).
- Spalten (additiv, nicht destruktiv):
  - `Beschichtungsauftraege.CreditorId int NULL` + FK `FK_Beschichtungsauftraege_Creditors_CreditorId` (NO ACTION) +
    Index `IX_Beschichtungsauftraege_CreditorId`
  - `Beschichtungsauftraege.VersendetAt datetime2 NULL`, `VersendetByUserId int NULL` (wie `UebergebenByUserId`: ohne
    FK, Muster der bestehenden Spalten in Abschnitt 17j prüfen und übernehmen)
  - `BeschichtungsauftragPositionen.Verpackt bit NOT NULL CONSTRAINT DF_… DEFAULT 0`
  - `BeschichtungsauftragPositionen.OhneFa bit NOT NULL CONSTRAINT DF_… DEFAULT 0`
- `SQL/<NNN>_<Name>.sql`: je Spalte `IF COL_LENGTH(N'dbo.<Tabelle>', N'<Spalte>') IS NULL` (Tabellen-Guard
  `OBJECT_ID(N'dbo.Beschichtungsauftraege', N'U')`). DDL je Objekt in eigenem Batch (`GO`); FK und Index mit eigenem
  Existenz-Guard (`sys.foreign_keys` / `sys.indexes`). `__EFMigrationsHistory`-Insert in separatem Batch mit
  `IF NOT EXISTS`.
- `SQL/00_FreshInstall.sql` an **zwei** Stellen: Abschnitt 17j (`:2792-2860`, Spalten + FK; `Creditors` entsteht vorher
  bei `:2207`) und die `MigrationId`-Liste.
- Status-Wert 35 braucht keine Schemaänderung (`tinyint` ohne CHECK).
- Altbestand: Alle vorhandenen Positionen bekommen `Verpackt = 0`, `OhneFa = 0`. Kein Datenskript (Rückfrage 15).
  Offene Übergeben-Aufträge müssen vor „Versendet“ angehakt werden.
- `SQL/AgentJobs/`: nicht betroffen. Kein Skript nennt `Beschichtungsauftrag` (Suche laut
  [[2026-11-16-beschichtungslogik-je-standort-spec]], Ist-Stand Nachtrag 3; der Dev-Lauf wiederholt die Suche).
- Kein Backup-Pflichtfall (additiv). Das Update-Skript macht ohnehin `v0-Backup`.

## Einstellungen

keine. Die Lackierer-Auswahl ist ein Stammdatenverweis im Formular (Auswahl aus `Creditors`, Server validiert, A9) und
keine Einstellung. Bestehende Einstellungen bleiben unverändert: `FaHierarchyBeschichtungAktiv` (Gate),
`BriefkopfVorlagePfad`.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

- `Beschichtungsauftrag` und `BeschichtungsauftragPosition` erben schon `AuditableEntity` (`Beschichtungsauftrag.cs:12`,
  `BeschichtungsauftragPosition.cs:12`).
- Jeder neue Schreibweg setzt Audit über die vorhandenen `Erstellt` / `Geaendert` (`Workflow.cs:484-496`):
  - Haken umstellen: Position `Geaendert`, nur wenn der Wert wechselt; Auftrag `Geaendert`.
  - Freie Position anlegen: Position `Erstellt`; Auftrag `Geaendert`.
  - Kreditor wählen / ändern: Auftrag `Geaendert`.
  - Versendet: `VersendetAt` / `VersendetByUserId` (`_user.GetCurrentAppUserId()`) + Auftrag `Geaendert`.
- Kein Dienst schreibt diese Tabellen (kein Service-Name nötig).

## Akzeptanzkriterien

1. Vorschlagsliste, Ansicht Liste: Jede Position hat hinten eine Checkbox; vergebene Positionen sind nicht anhakbar und
   nennen ihren Auftrag.
2. Ansicht Summiert: Jede Zeile hat hinten eine Checkbox. Ein Haken übernimmt alle freien Positionen dieser Artnr des
   HauptFA. Bei teilweiser Vergabe steht „n von m frei“.
3. „Aus Vorschlagsliste erstellen“ mit zwei von fünf angehakten Positionen legt einen Entwurf mit genau diesen zwei an,
   beide „verpackt“. Die drei anderen bleiben frei und anhakbar.
4. Ohne Haken und ohne freie Position: Warnung „Keine Position angehakt — nichts angelegt.“, kein Auftrag, keine Nummer
   verbraucht.
5. Ist ein angehakter Schlüssel inzwischen vergeben (zweiter Benutzer), wird nichts angelegt, die Meldung nennt die
   Position und den Auftrag.
6. Freie Position im Vorschlag (Artikel per Suche, Menge 3) landet im Auftrag mit „ohne FA“, Artnr/Matchcode aus dem
   Artikelstamm, Menge 3. Menge 0, negativ oder Text → Ablehnung ohne Anlage.
7. Zwei Aufträge desselben HauptFA dürfen denselben freien Artikel enthalten (keine Doppelvergabe-Meldung).
8. Im Entwurf und in Übergeben: Haken setzen/lösen, Menge ändern, Position entfernen, Position (Kandidat oder frei)
   hinzufügen und speichern. Die Werte stehen nach Neuladen so da; `ModifiedAt/By/ByWindows` der geänderten Position und
   des Auftrags sind gesetzt.
9. Eine in Übergeben entfernte Position ist sofort wieder in der Vorschlagsliste frei.
10. Übergeben → Versendet mit einer unverpackten Position: abgelehnt, Meldung nennt die Position. Nach dem Haken:
    gelingt, `VersendetAt/ByUserId` gesetzt, Statusverlauf zeigt „Versendet“ mit Zeit und Benutzer.
11. In Versendet und Erledigt sind Haken, Mengen und Positionen nicht änderbar (keine Eingabefelder; ein direkter POST
    wird vom Workflow abgelehnt). Bemerkung bleibt änderbar.
12. Übergeben → Erledigt direkt ist nicht mehr möglich (kein Knopf; direkter POST abgelehnt). Versendet → Erledigt geht.
13. Liste `/Beschichtungsauftraege`: Statusfilter „Versendet“ liefert nur solche Aufträge. Spaltenfilter `status`
    „Versendet“ und Datumsfilter „Versendet am“ wirken serverseitig vor dem Paging. Pagination bleibt erhalten.
14. Lackierer-Auswahl zeigt nur aktive Kreditoren mit „Beschichter“. Ein manipulierter POST mit einem Nicht-Beschichter
    oder inaktivem Kreditor wird abgelehnt. Mit Kreditor steht dessen Name als Dienstleister in der Liste.
15. IDEAL: Sage-Dienstleister „X“ und genau ein aktiver Beschichter-Kreditor mit Kurzname „X“ → Kreditor vorbelegt. Kein
    oder mehrdeutiger Treffer → nur Text + Hinweis.
16. Druck und PDF eines Auftrags mit Kreditor: Empfänger als Brief-Anschrift (Name, Straße, PLZ Ort, Land), HauptFA in der
    Kopftabelle, Spalte 1 „Pos.“ 1..n, keine Spalte „Positionen“, Menge „5“ statt „5,000“ (und „2,5“ bei Bruchteil),
    letzte Spalte „Verpackt“ ☑/☐, eine Zeile je Position, freie Positionen mit „ohne FA“.
17. Druck ohne Kreditor: Dienstleister-Text als Empfänger; der Hinweis „Keine Anschrift …“ steht nur am Bildschirm, nicht
    im Ausdruck und nicht im PDF.
18. Druckbar in Übergeben, Versendet, Erledigt; im Entwurf und storniert abgelehnt wie heute.
19. Bemerkung eintippen, nicht speichern, „Drucken“/„PDF“/Statusknopf/„Speichern“ → Rückfrage erscheint; Abbrechen lässt
    den Text stehen. Gespeicherte Bemerkung steht in Druck **und** PDF.
20. Reklamation aus Erledigt kopiert freie Positionen mit „ohne FA“; alle kopierten Positionen sind „offen“ (nicht
    verpackt).
21. Benutzer nur mit `beschichtungsauftrag` (Lesen) sehen Haken als Text, keine Checkboxen, keine Auswahl, keine
    Statusknöpfe; direkte POSTs → Zugriff verweigert.
22. AKE und IDEAL verhalten sich gleich (R5); an der AKE gilt der Kommissioniert-Schalter der Vorschlagsliste
    (Nachtrag 4) unverändert, die Haken überleben das Umschalten nicht (Rückfrage 8).
23. Build und alle Tests grün. Neue Unit-Tests mindestens für: Übergänge inkl. Ablehnung Übergeben→Erledigt, A6,
    Anlage mit Auswahl, freie Positionen nicht in Vergaben, Bearbeiten in Übergeben / Ablehnung in Versendet,
    Kreditor-Validierung, Druckzeilen je Position mit laufender Nummer und Mengenformat.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` (Nummer beim Dev-Start = nächste freie TS, zuletzt TS-161), Querverweis bei
TS-84 und TS-145. Danach `secondbrain/tests/testszenarien-index.md` nachziehen. Durchführung auf DEV **AKE und IDEAL**
(Gate `FaHierarchyBeschichtungAktiv` auf DEV AKE vorher `true`, danach Ausgangswert, wie TS-145).

Vorbedingungen: Benutzer L (nur `beschichtungsauftrag`), S (`beschichtungsauftrag_schreiben`). Ein HauptFA/WA mit
≥ 5 beschichteten Positionen, davon zwei mit gleicher Artnr. Ein aktiver Kreditor mit „Beschichter“ und vollständiger
Anschrift, einer ohne Anschrift, einer inaktiv.

| Nr | Schritte | Erwartung |
|---|---|---|
| x.1 | S: Vorschlag Liste, 2 von 5 anhaken, erstellen | Entwurf mit 2 verpackten Positionen; Vorschlag zeigt 3 frei (AK 3) |
| x.2 | S: Summiert, Zeile mit 2 Positionen derselben Artnr anhaken | beide im Auftrag (AK 2) |
| x.3 | S: nichts anhaken, erstellen | Warnung, kein Auftrag, nächste Nummer unverändert (AK 4) |
| x.4 | Zwei Tabs, gleiche Position anhaken, nacheinander erstellen | zweiter abgelehnt mit Nummer des ersten (AK 5) |
| x.5 | S: „+ Position ohne FA“, Artikel suchen, Menge 3 / 0 / -1 / „abc“ | 3 → im Auftrag „ohne FA“; Rest abgelehnt (AK 6) |
| x.6 | Zweiter Auftrag mit demselben freien Artikel | keine Doppelvergabe-Meldung (AK 7) |
| x.7 | Entwurf → Übergeben; dort Haken lösen, Menge ändern, Position entfernen, Kandidat + freie Position hinzufügen | gespeichert, Audit gesetzt, entfernte Position im Vorschlag frei (AK 8, 9) |
| x.8 | Übergeben → Versendet mit einer offenen Position | abgelehnt mit Positionsangabe; nach Haken gelingt es (AK 10) |
| x.9 | Versendet: Bearbeiten versuchen (UI und direkter POST per Entwicklertools) | keine Felder; POST abgelehnt; Bemerkung änderbar (AK 11) |
| x.10 | Übergeben → Erledigt per manipuliertem POST | abgelehnt (AK 12) |
| x.11 | Liste: Status „Versendet“, Spaltenfilter status/Versendet am, Seitenwechsel | nur passende, Filter in URL, Pagination stimmt (AK 13) |
| x.12 | Kreditor wählen; POST mit inaktivem / Nicht-Beschichter | angenommen bzw. abgelehnt (AK 14) |
| x.13 | IDEAL: HauptFA mit Sage-Dienstleister = Kurzname eines Beschichters anlegen; Gegenprobe ohne Treffer | vorbelegt bzw. Hinweis (AK 15) |
| x.14 | Druck + PDF mit Kreditor, mit und ohne Bemerkung | AK 16; Bemerkung in beiden (AK 19) |
| x.15 | Druck ohne Kreditor / Kreditor ohne Anschrift | Text-Empfänger; Hinweis nur am Bildschirm (AK 17) |
| x.16 | Bemerkung tippen, ohne Speichern Drucken / PDF / Statusknopf | Rückfrage; Abbrechen behält Text (AK 19) |
| x.17 | Reklamation aus Erledigt mit freier Position | kopiert, „ohne FA“, alle offen (AK 20) |
| x.18 | L: Detail, Vorschlag, direkter POST | nur Text, keine Checkboxen; POST verweigert (AK 21) |
| x.19 | AKE: Kommissioniert-Schalter umschalten mit gesetzten Haken | Haken weg, Hinweis im Hilfe-Text; Anlage nur mit sichtbaren Haken (AK 22) |
| x.20 | Kontrast/Bedienung: Terminal-Ansicht, Zoom 200 %, nur Tastatur | Haken erreichbar und sichtbar fokussiert; Zustand ohne Farbe erkennbar |

Negativfälle sind x.3, x.4, x.5 (ungültige Mengen), x.8, x.9, x.10, x.12, x.18.

**Prüfschritte (Mensch / Dev-Lauf, vor bzw. in den Etappen)**

- **P-1 (E5, Bemerkung):** Auf DEV mit gespeicherter Bemerkung (≥ 2 Zeilen) Bildschirmdruck aus Edge **und** Chrome
  sowie PDF. Ist die Bemerkung im Druck sichtbar, gilt der stille Verlustweg (Befund 2) als Ursache. Wenn nicht: das
  eingebettete CSS der Briefkopfvorlage (`BriefkopfVorlagePfad`) auf Regeln für `div`/`.bemerkung`/`@media print`
  prüfen und das Ergebnis als Nachtrag festhalten. Außerdem den Fachbereich fragen (über Gerald), ob vor dem Drucken
  „Bemerkung speichern“ gedrückt wurde.
- **P-2 (E4, Anschrift; Mensch, beide Standorte):**
  ```sql
  -- WMS-DB: Beschichter-Kreditoren und Vollständigkeit der Anschrift
  SELECT Id, Name, ShortName, Source, SageKto, Street, PostalCode, City, Country, IsActive
  FROM dbo.Creditors WHERE IsCoater = 1 ORDER BY IsActive DESC, Name;
  ```
  Für zwei Beschichter mit `Source = 'Sage'` in Sage die Anschrift ansehen und prüfen, ob „Lieferstraße/-PLZ/-Ort“ die
  Briefanschrift ist oder eine abweichende Lieferanschrift. Spaltennamen in Sage gegen den SData-Select
  `KontokorrentSyncService.cs:70` abgleichen; ob die SQL-Spalten von `KHKAdressen` gleich heißen, ist **unverifiziert**.
  Bis zur Bestätigung gilt A11 (g) als **unverifiziert** für Sage-Kreditoren.
- **P-3 (E2, Maße freie Positionen IDEAL):**
  ```sql
  -- WMS-DB IDEAL: liefern ArticleSageDetails dort Werte?
  SELECT TOP 10 a.ArticleNumber, d.Breite, d.Hoehe, d.Laenge
  FROM dbo.ArticleSageDetails d JOIN dbo.Articles a ON a.Id = d.ArticleId
  WHERE d.Breite IS NOT NULL ORDER BY a.ArticleNumber;
  ```
  Drei Artikel gegen die Maße in Sage IDEAL vergleichen. Ist die Einheit nicht cm, gilt ×10 an der IDEAL nicht.
  Rückfall: Maße 0 plus Logger-Warnung (wie B1 im QA-Nachweis Nachtrag 4).
- **P-4 (vor Deploy, Mensch):** Mengengerüst Altbestand je System:
  `SELECT Status, COUNT(*) FROM dbo.Beschichtungsauftraege GROUP BY Status;`. Übergeben-Aufträge müssen nach dem Deploy
  vor „Versendet“ angehakt werden (Rückfrage 15).

## Etappen (nur bei epic: true)

**Warum Epic statt Split:** Die Teile teilen Migration, Workflow und Detail-View und sind einzeln fachlich nicht nutzbar.
Ein „Versendet“ ohne Haken hat keine Regel, Haken ohne Druck-Spalte sind halb. Ein Split würde dieselben Dateien
mehrfach mergen. Gebaut wird im Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5` (wie Teil 1–4 und die
Beschichtungslogik-Spec), je Etappe ein Commit, **ein** Merge mit dem Bündel. Den Branch während der Arbeit mit
`scripts/sync-worktree.ps1` auf main-Stand halten. Jede Etappe baut und testet grün für sich. Nur E1 ändert das Schema.
Keine Etappe wartet auf eine offene Frage, wenn die Freigabe-Antworten vorliegen. Abhängigkeiten von Prüfschritten sind
markiert; die Etappe baut dann den Rückfall und hält den Prüfschritt als Blocker fest, statt das Epic anzuhalten.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| 1 | E1 Schema + Status Versendet: Model, Migration, SQL (Nummer beim Start), FreshInstall ×2, Übergänge, `IstDruckbar`, Badge, Statusverlauf, Listen-Spalte „Versendet am“, Tests (AK 10, 12, 13, 18) | erledigt | 5c042c3d |
| 2 | E2 Auftrag bis vor Versendet: Haken, Menge/Entfernen/Hinzufügen in Übergeben, freie Positionen am Auftrag, `GetVergabenAsync` ohne OhneFa, Versendet-Regel, Reklamation-Kopie (AK 7-11, 20, 21). Maße IDEAL hängen an **P-3** (Rückfall 0 + Warnung) | erledigt | b525cfba |
| 3 | E3 Vorschlagsliste: Haken hinten Liste + Summiert, Anlage nur Auswahl, freie Positionen im Gruppenkopf (AK 1-6, 22) | erledigt | 4b2b490c |
| 4 | E4 Lackierer aus Kreditoren: `CreditorId`-Auswahl, Server-Validierung, IDEAL-Vorbelegung, Anschrift im Briefkopf (AK 14, 15, 17). Anschrift-Inhalt hängt an **P-2** (Bau unabhängig) | erledigt | 5ff2c7a8 |
| 5 | E5 Druck/PDF: je Position, Pos.-Nr., Menge, HauptFA im Kopf, Verpackt-Spalte, Bemerkung-Absicherung; **P-1** am Anfang der Etappe (AK 16, 19) | erledigt | b1ce8608 |
| 6 | E6 Doku: TESTSZENARIEN + Index, Hilfe/Kurzanleitung, Anwender- und Brain-Changelog, Version beide `AppVersion.cs`, Manifest, feature-map, codebase-Karte (Status-Automat), ggf. fallstricke (freie Positionen nie in Vergaben) | erledigt | cf11a777 |

## Deploy

*Vom QA-Lauf 2026-11-18 gegen den echten Diff `c39126c3..9b6b3e0d` bestätigt.*

- **Web-App:** ja (Controller, Workflow, Repository, Views, JS, Migration im Web-Projekt).
- **Service:** nur Versionsnummer (`IDEALAKEWMSService/AppVersion.cs` 1.99.41, sonst kein Dienstcode). Wird mit dem
  Bündelstand ohnehin ausgeliefert.
- **Migration:** ja, additiv: `20261118120203_AddBeschichtungsauftragVersandUndVerpackt` = `SQL/161_AddBeschichtungsauftragVersandUndVerpackt.sql`
  (fünf Spalten, ein FK, ein Index; FreshInstall an beiden Stellen). Kein Daten-/Einmal-Skript. Reihenfolge:
  DB-Backup → SQL/161 (Phase Schema, Update-Skript automatisch) → Web → Dienst → Nachher-Prüfungen aus dem Manifest
  ansehen (Beschichter-Kreditoren, offene Übergeben-Aufträge, Info).
- **Manifest:** die vier Einträge unten stehen in `scripts/deploy/nachlauf.psd1` (geprüft).
- **Publish:** im QA-Lauf bewusst **nicht** angelegt (Auftrag: `publish\paket6-*` macht der Folgelauf). Ablauf des
  Menschen: Publish aus dem Worktree → Testsystem → Test → Merge. Nach dem Merge nur dann aus `main` neu publishen, wenn
  der Merge getestete Dateien mit parallelen main-Änderungen zusammengeführt hat.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023):
  - `Id='SQL/<NNN>'; Version='<v>'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch';
    Datei='<NNN>_<Name>.sql'; MigrationId='<ts>_<Name>'; Voraussetzung='additiv'; Quelle='<brain-changelog>'`
  - `Id='Pruefung/BeschichterKreditoren'; Version='<v>'; Typ='Pruefung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen';
    Pruefung='SELECT CASE WHEN OBJECT_ID(N''dbo.Creditors'', N''U'') IS NULL THEN 0 WHEN EXISTS (SELECT 1 FROM dbo.Creditors WHERE IsCoater = 1 AND IsActive = 1) THEN 0 ELSE 1 END';
    Text='Kein aktiver Kreditor mit „Beschichter“: die Lackierer-Auswahl am Beschichtungsauftrag ist leer, gedruckt wird nur der Dienstleister-Text ohne Anschrift. In /Creditors Beschichter kennzeichnen (P-2 der Spec 2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck).'`
  - `Id='Pruefung/BeschichtungsauftragUebergebenOffen'; Version='<v>'; Typ='Pruefung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen';
    Pruefung='SELECT CASE WHEN OBJECT_ID(N''dbo.Beschichtungsauftraege'', N''U'') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM dbo.Beschichtungsauftraege WHERE Status = 30) END';
    Text='Beschichtungsaufträge im Status Übergeben: ab dieser Version führt der Weg über „Versendet“ (alle Positionen verpackt), Übergeben → Erledigt direkt geht nicht mehr. Fachbereich informieren (P-4, Rückfrage 15).'`
  - `Id='v<v>-BeschichtungVersendetInfo'; Version='<v>'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen';
    Text='Beschichtungsauftrag: Positionen werden im Vorschlag angehakt (nur angehakte kommen in den Auftrag), neuer Status Versendet, Lackierer aus den Kreditoren. Hilfeseite Beschichtung.'`
  - Kein Daten-/Einmal-Skript (Rückfrage 15 Empfehlung). Entscheidet Rückfrage 15 anders, kommt ein Trockenlauf-Skript
    nach ADR 0023 dazu (`DECLARE @Trockenlauf BIT = 1;`, `TREFFER: n`, Wiederholungssperre mit `TREFFER: 0`).
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  (Dienst nur mit dem Bündelstand).

## Offene Rueckfragen

Hinweis: Rückfragen 1–5 sind die R1–R5 des Backlogs. Gerald hat sie dort am 2026-11-18 11:48 beantwortet. Die Antwort
steht je Frage als Zitat; sie ist **nicht** in den Freigabe-Block eingetragen. „wie Backlog“ dort genügt. Die
**Empfehlung** ist die des Spec-Agenten.

1. **(R1) Bedeutung des Hakens.** Backlog-Antwort: „in der Vorschlagsliste … ‚zum Versand vorbereitet / verpackt‘;
   angehakte Zeilen gehen in den Auftrag … am Auftrag änderbar … gedruckt in der letzten Spalte.“
   **Empfehlung:** genau so. Im Vorschlag ist der Haken die Auswahl für die Anlage; gespeichert wird er erst am Auftrag
   als `Verpackt` je Position.
2. **(R2) Statusfolge.** Backlog-Antwort: „Entwurf → Übergeben → Versendet → Erledigt; Bearbeiten bis vor Versendet.“
   **Empfehlung:** so, und der heutige direkte Übergang Übergeben → Erledigt (`Workflow.cs:56`) entfällt. Storno bleibt
   nur aus Entwurf.
3. **(R3) Freie Positionen.** Backlog-Antwort: „Artikelsuche im Artikelstamm + Menge, Kennzeichen ‚ohne FA‘.“
   **Empfehlung:** so; kein Freitext-Artikel. Freie Positionen zählen nie als Vergabe.
4. **(R4) Lackierer.** Backlog-Antwort: „aus Kreditoren mit IsCoater, Anschrift aus dem Kreditor; Freitext nur Rückfall.“
   **Empfehlung:** so; nur aktive Beschichter wählbar, Server prüft. Bekannte Einschränkung: `IsCoater` wird nur von Hand
   gepflegt (`CreditorsController.cs:260`).
5. **(R5) Standort.** Backlog-Antwort: „AKE und IDEAL.“ **Empfehlung:** beide, keine Standortweiche.
6. **Versendet nur mit allen Haken?** R1 der Koordination nannte „alle Haken oder Rest abhängen“, die Antwort lässt es
   offen. **Empfehlung:** harte Sperre (A6). Der Rest wird durch „Entfernen“ abgehängt und ist dann wieder frei.
7. **Haken in der Summiert-Ansicht** (Standardansicht der Vorschlagsliste)? **Empfehlung:** ja, ein Haken je Zeile =
   alle freien Positionen dieser Artnr im HauptFA (A2). Sonst müsste man zum Packen immer erst auf „Liste“ umschalten.
8. **Haken im Vorschlag nicht speichern?** Sie gehen bei Neuladen, Filter- oder Ansichtswechsel verloren.
   **Empfehlung:** ja, nicht speichern. Wer über längere Zeit packt, legt den Entwurf früh an und hakt dort weiter. Das
   Speichern im Vorschlag bräuchte eine eigene Tabelle ohne Auftrag.
9. **Freie Positionen im Vorschlag: Eingabe, Haken, Maße.** **Empfehlung:** Eingabe im Gruppenkopf neben „Aus
   Vorschlagsliste erstellen“ (Artikel + Menge), angelegt als verpackt. Maße aus `ArticleSageDetails` × 10 (wie
   Nachtrag 4 der Beschichtungslogik-Spec); an der IDEAL erst nach P-3, bis dahin 0 mit Warnung.
10. **Was ist in Übergeben änderbar?** **Empfehlung:** nur Positionen (Haken, Menge, Entfernen, Hinzufügen). Der Kopf
    (Kreditor, RAL, Termine) bleibt nach Teil-4-Antwort 7 nur im Entwurf änderbar, weil das Dokument dann schon beim
    Lackierer sein kann. Folge: Wer nach dem Ausdruck Positionen ändert, druckt neu.
11. **Druck je Position statt summiert?** Teil-4-Antwort 6 legte „summiert nach Artnr“ fest. Positionsnummer in Spalte 1
    und ein Haken je Position passen nicht zu einer Summenzeile, die mehrere Positionen mit verschiedenem Haken enthält.
    **Empfehlung:** je Position (A11 a). Teil-4-Antwort 6 wird für den Auftragsdruck aufgehoben; der Druck der
    Vorschlagsliste (Summiert) bleibt.
12. **Positionsnummer** = laufende Nummer 1..n oder Stücklisten-Position (AKE `51.5.2`, IDEAL `Position`)?
    **Empfehlung:** laufende Nummer. Freie Positionen haben keine Stücklisten-Position, und der Lackierer braucht eine
    eindeutige Zeilennummer für Rückfragen. Die Stücklisten-Position bleibt in der Detailseite sichtbar.
13. **Menge im Druck ganzzahlig.** **Empfehlung:** Format `0.###`: „5“ statt „5,000“, Bruchteile („2,5“) bleiben
    sichtbar statt still gerundet. Nur Druck/PDF. Alternative: Eingabe am Auftrag auf ganze Zahlen beschränken (dann auch
    IDEAL-Sollmengen mit Bruchteil prüfen).
14. **Brief-Anschrift: live oder Kopie, und welche Sage-Anschrift?** **Empfehlung:** live aus dem Kreditor zum
    Druckzeitpunkt, keine Kopie am Auftrag (weniger Spalten; Nachdruck alter Aufträge zeigt dann die aktuelle Anschrift).
    Ob `LieferStrasse/-PLZ/-Ort/-Land` (Quelle des Abgleichs, `KontokorrentSyncService.cs:318-321`) die Briefanschrift
    ist, klärt P-2.
15. **Altaufträge:** Alle vorhandenen Positionen starten mit „nicht verpackt“. **Empfehlung:** kein Datenskript.
    Offene Übergeben-Aufträge werden nach dem Deploy angehakt (Prüfung im Manifest zeigt die Anzahl, P-4); Erledigte
    drucken leere Kästchen. Alternative: Einmal-Skript setzt `Verpackt = 1` für Erledigt.
16. **Bemerkung:** Ursache am Gerät unbelegt (Befund). **Empfehlung:** P-1 durchführen und den belegten stillen
    Verlustweg mit A12 schließen (Rückfrage vor Drucken/PDF/Speichern/Status bei ungespeicherter Bemerkung).
    Alternative: Bemerkung in das Hauptformular ziehen (dann aber auch in Versendet/Erledigt ein Formular nötig).
17. **Kreditor-Vorbelegung IDEAL** aus dem Sage-Dienstleistertext über den Kurznamen? **Empfehlung:** ja, nur bei genau
    einem aktiven Beschichter-Treffer (A10). Die Regel gibt es schon in `CreditorAssignment`.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. (R1) Bedeutung des Hakens → **Gerald 2026-11-18 11:48 (Backlog-Antwort) – wie Backlog/Empfehlung.**
2. (R2) Statusfolge, Übergeben → Erledigt entfällt → **Gerald 2026-11-18 11:48 (Backlog-Antwort) – wie Backlog/Empfehlung.**
3. (R3) Freie Positionen über Artikelsuche → **Gerald 2026-11-18 11:48 (Backlog-Antwort) – wie Backlog/Empfehlung.**
4. (R4) Lackierer aus Kreditoren mit IsCoater → **Gerald 2026-11-18 11:48 (Backlog-Antwort) – wie Backlog/Empfehlung.**
5. (R5) AKE und IDEAL → **Gerald 2026-11-18 11:48 (Backlog-Antwort) – wie Backlog/Empfehlung.**
6. Versendet nur mit allen Haken → **Gerald 2026-11-18 12:55 „können wir starten“ – wie Empfehlung.**
7. Haken auch in Summiert → **Gerald 2026-11-18 12:55 „können wir starten“ – wie Empfehlung.**
8. Haken im Vorschlag nicht speichern → **Gerald 2026-11-18 12:55 „können wir starten“ – wie Empfehlung.**
9. Freie Positionen im Vorschlag: Eingabe, verpackt, Maße → **Gerald 2026-11-18 12:55 „können wir starten“ – wie Empfehlung.**
10. In Übergeben nur Positionen änderbar → **Gerald 2026-11-18 12:55 „können wir starten“ – wie Empfehlung.**
11. Druck je Position statt summiert → **Gerald 2026-11-18 12:55 „können wir starten“ – wie Empfehlung.**
12. Positionsnummer = laufende Nummer → **Gerald 2026-11-18 12:55 „können wir starten“ – wie Empfehlung.**
13. Menge im Druck `0.###`, nur Druck/PDF → **Gerald 2026-11-18 12:55 „können wir starten“ – wie Empfehlung.**
14. Anschrift live aus Kreditor; Sage-Lieferanschrift (P-2) → **Gerald 2026-11-18 12:55 „können wir starten“ – wie Empfehlung.**
15. Altaufträge ohne Datenskript → **Gerald 2026-11-18 12:55 „können wir starten“ – wie Empfehlung.**
16. Bemerkung: P-1 + Rückfrage bei ungespeicherter Bemerkung → **Gerald 2026-11-18 12:55 „können wir starten“ – wie Empfehlung.**
17. Kreditor-Vorbelegung IDEAL über Kurzname → **Gerald 2026-11-18 12:55 „können wir starten“ – wie Empfehlung.**

## QA-Nachweis (qa-agent, 2026-11-18)

Geprüfter Bereich: `c39126c3..cf11a777` (E1 5c042c3d, E2 b525cfba, E3 4b2b490c, E4 5ff2c7a8, E5 b1ce8608, E6 cf11a777)
plus QA-Fix `9b6b3e0d`. Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Version 1.99.41.

### 1. Build und Tests

- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 29 Warnungen (Bestand).
- `dotnet test` vor dem QA-Fix: IdealAkeWms.Tests 3286 bestanden / 1 übersprungen / 0 Fehler (3287);
  IDEALAKEWMSService.Tests 779 / 0 / 0.
- `dotnet test` nach dem QA-Fix (HEAD 9b6b3e0d): **IdealAkeWms.Tests 3287 bestanden / 1 übersprungen / 0 Fehler (3288)**;
  **IDEALAKEWMSService.Tests 779 / 0 / 0**.

### 2. Code-Review (selbst, in diesem Lauf)

Gelesen: Workflow (Übergänge, AnlegenAsync/AuswahlAsync, BearbeitenAsync, FreiePositionenAsync, StatusWechselnAsync,
KreditorVorbelegen, Kopie), Controller (Create, Bearbeiten, TryFreiePositionen, Print/Pdf, Filter/Antiforgery),
Repository (GetVergabenAsync `!OhneFa`, GetBeschichterAsync, GetArtikelMitMassenAsync, GetDetailAsync), Views Details/Index/
Print/_PrintBriefkopf/FaHierarchyBeschichtung Index+Summiert/_VorschlagAuftraege/_BeschichtungFreiePositionen,
`beschichtung-frei.js`, `beschichtung-bemerkung.js`, PrintBriefkopfModel, SQL/161, FreshInstall, Migration, Manifest.
Gezielt geprüfte Fehlerklassen: kein `@Html.Raw` in `<script>`; `#bemerkung-ungespeichert` mit `hidden` ohne `d-*`;
keine stillen `?? 0` (Maße IDEAL = 0 nur mit sichtbarem Hinweis + Log-Warnung, A7/P-3); Server erzwingt Menge > 0,
Kreditor-Prüfung, A6, Übergänge; Regel „freie Position nie Vergabe“ nur an einer Stelle (`GetVergabenAsync`).

| # | Schwere | Befund | Fundstelle | Behandlung |
|---|---|---|---|---|
| F1 | mittel | Parallel-Array-Bindung: der `<select name="freiArtikelId">` der Zeile „Position ohne FA“ hatte keine Option; eine Zeile ohne Artikel (oder per allowClear geleert) sendete nichts, `freiMenge` aber jede Zeile. `TryFreiePositionen` paart per Index → Zeile 1 (kein Artikel, Menge 5) + Zeile 2 (Artikel X, Menge leer) wurde still als „X mit 5“ angenommen | `IdealAkeWms/wwwroot/js/beschichtung-frei.js:14` (vorher), Controller `TryFreiePositionen` | **behoben** in 9b6b3e0d (leere Option, jede Zeile sendet), Guard-Test `BeschichtungsauftraegeViewTests.FreiePositionen_ArtikelAuswahl_SendetJedeZeile`; auf DEV IDEAL nachgeprüft (leere Zeile 1 + Zeile 2 mit „abc“ → Meldung nennt korrekt „Position ohne FA 2“) |
| H1 | niedrig | Druck zeigt die Spalte „Verpackt“ immer, unabhängig von der Spaltenauswahl (`ShowCol("verpackt")` wird nicht gefragt) | `Views/Beschichtungsauftraege/Print.cshtml` | gemeldet, nicht gebaut (AK 16 verlangt die Spalte) |
| H2 | niedrig | Reklamations-Auswahlseite zeigt eine freie Position als „— / 0 / —“ statt „ohne FA“ (im neuen Auftrag korrekt „ohne FA“) | `Views/Beschichtungsauftraege/Reklamation.cshtml` | gemeldet, kosmetisch |
| H3 | niedrig | `BearbeitenAsync` nimmt freie Positionen auch in einem Reklamations-Entwurf per direktem POST an; die UI bietet es dort nicht an | `Services/BeschichtungsauftragWorkflow.cs` BearbeitenAsync | gemeldet; fachlich unschädlich |
| H4 | niedrig | `AnlegenAsync(null)` („alle freien“) wird nur noch von Tests benutzt | Workflow AnlegenAsync | gemeldet (toter Pfad) |
| H5 | niedrig | Wird ein Kreditor inaktiv, fällt `CreditorId` beim nächsten Kopf-Speichern im Entwurf auf Freitext zurück – mit sichtbarer Warnung | Workflow BearbeitenAsync | akzeptiert (meldet statt still) |
| H6 | niedrig | EF-Migration legt Default-Constraints mit generierten Namen an, SQL/161 mit benannten `DF_…` – Namen driften zwischen migrierten und per Skript angelegten DBs | Migration vs. `SQL/161_…sql` | gemeldet; betrifft nur spätere DROP-Skripte |
| H7 | niedrig | Menge `0.###` rundet die 4. Nachkommastelle (Spalte decimal(18,4)) | Print.cshtml FormatMass | wie Rückfrage 13 entschieden |
| bekannt | – | Ein Auftrag nur aus Positionen ohne FA erscheint nicht im Gruppenkopf „Aufträge:“ der Vorschlagsliste | Vorschlag `_VorschlagAuftraege` | laut Auftrag gemeldet, nicht gebaut |

Rechte: alle Schreib-Actions `[RequireBeschichtungsauftragWriteAccess]` + `[ValidateAntiForgeryToken]`, Klasse
`[RequireBeschichtungsauftragAccess]` + `[RequireFaHierarchyBeschichtungAktiv]`. Audit: Erstellt/Geaendert an Kopf und
Position (auf DEV belegt, s. u.).

### 3. Beweisart je Akzeptanzkriterium

| AK | Beweis |
|---|---|
| 1, 2 | Unit (`BeschichtungsauftragVergabeAnzeigeTests`, Controller-Tests) + DEV-Browser (IDEAL Liste/Summiert, „n von m frei“) |
| 3, 4, 5 | Unit (Controller/Workflow: Auswahl, leer, teilweise ungültig) + DEV (3, 4); 5 Zwei-Tab-Lauf **Manual-UAT** (InMemory beweist Serializable nicht) |
| 6, 7 | Unit (Workflow freie Positionen, GetVergaben ohne OhneFa) + DEV IDEAL/AKE |
| 8, 9 | Unit (BearbeitenAsync in Übergeben) + DEV IDEAL (Audit, Freigabe der entfernten Position) |
| 10, 11, 12 | Unit (Übergänge, A6, Ablehnung ab Versendet, Übergeben→Erledigt) + DEV (10, UI-Teil von 11/12); direkter POST **Manual-UAT** |
| 13 | Unit (Listenfilter) + DEV Spalte „Versendet am“; Filter/Paging im Browser **Manual-UAT** |
| 14 | Unit (Kreditor-Prüfung) + DEV (Auswahl nur aktive Beschichter); manipulierter POST **Manual-UAT** |
| 15 | Unit (Vorbelegung) + DEV IDEAL positiv (GL); Gegenprobe ohne/mehrere Treffer **Manual-UAT** |
| 16, 17, 18, 19 | View-Tests + DEV Chrome (Druck + PDF); Edge **Manual-UAT**; Rückfrage-Dialog (confirm) **Manual-UAT** |
| 20 | Unit (Kopie) + DEV IDEAL |
| 21 | Filter-Attribute (Code) – Benutzer L **Manual-UAT** |
| 22 | DEV AKE (Schalter) |
| 23 | Build/Tests grün (oben) |

### 4. Geänderte bestehende Tests

Alle eingeordnet, keine Kategorie „sonstige“: Create-Aufrufe mit Auswahl `Alle` (A3); `PositionenHinzufuegen` →
`Bearbeiten(nurPositionen, schluessel)` (A8), `PositionenHinzufuegen_LeereAuswahl_Warnung` entfernt (A8, ein Formular);
Status-Helfer über Versenden (A5/A6); Theorie verbotener Übergänge erweitert (A5); Enum-Test 10,30,35,40,90 (A5);
„nur im Entwurf“ → „nur in Entwurf und Übergeben“ (A8); Druck-Test summiert → je Position (A11, Rückfrage 11);
View-Tests ShowCol-Schlüssel (A11c); Umbenennung `EntwurfBearbeitenAsync` → `BearbeitenAsync` und BOM in Testdateien
(nur Typ/Format).

### 5. Checkliste CLAUDE.md / ADR 0020

Migration + idempotentes SQL/161 (COL_LENGTH-/sys-Guards, DDL in eigenen Batches, History-Insert separat): ja.
FreshInstall beide Stellen: ja. AgentJobs: nicht betroffen. Audit-Felder: ja. Version beide `AppVersion.cs` 1.99.41 +
Anwender-Changelog: ja. TS-162 in `docs/TESTSZENARIEN.md` und im Testindex: ja. Manifest `nachlauf.psd1` (SQL/161,
zwei Prüfungen, Info): ja. Neue/geänderte Einstellungen: keine (ADR 0020 entfällt).

### 6. DEV-Deploy und Testlauf

- Dienste/Sites gestoppt, `D:\Dev\{AKE,IDEAL}\{Web,Service}` geleert (je Standort Web 65 / Service 72 Einträge;
  behalten: `appsettings.DEV.json*`, `logs`, `App_Data`).
- `dev-deploy.ps1 -Standort AKE|IDEAL -Quelle <worktree>`: beide Exit 0, HTTP 200, Fußzeile **v1.99.41**,
  DEV-Sicherheits-SQL angewendet, Dienst gestartet. Migration `20261118120203_…` in `__EFMigrationsHistory` von
  AKE_WMS_DEV und IDEAL_WMS_DEV. Der Deploy enthielt den QA-Fix (damals uncommitted, identisch mit 9b6b3e0d).
- `FaHierarchyBeschichtungAktiv` war auf beiden DEV-DBs bereits `true` – nichts geändert, nichts zurückzusetzen.
- Testdaten: Kreditoren QA-LACK1 (aktiv, Beschichter, volle Anschrift), QA-LACK2 (aktiv, Beschichter, ohne Anschrift),
  QA-LACK3 (inaktiv), QA-LACK4 (kein Beschichter) in beiden DEV-DBs. IDEAL: BA-2026-00001..00004 per SQL storniert,
  um HauptFA 1060414 frei zu bekommen; neu BA-2026-00006/00007/00008. AKE: neu BA-2026-00002.

| Schritt | Ergebnis | Bewertung |
|---|---|---|
| 162.1 | IDEAL Liste: 2 Positionen angehakt → BA-2026-00006 mit 2 Positionen, Verpackt=1, OhneFa=0 | OK |
| 162.2 | IDEAL Summiert: Zeile 50000295 (3 Positionen) angehakt → alle 3 im Auftrag BA-2026-00007; Zeile danach „vollständig vergeben“; Teilvergabe 50002326 zeigt „2 von 3 frei“ | OK |
| 162.3 | nichts angehakt → „Keine Position angehakt — nichts angelegt.“, nächste Nummer unverändert | OK |
| 162.4 | Zwei-Tab-Lauf nicht durchgeführt (Unit-Test deckt die Ablehnung) | NICHT MOEGLICH (Zeit; Mensch) |
| 162.5 | „abc“ → „Menge „abc“ ist keine Zahl“, 0 → „Menge muss größer 0 sein“, leere Menge mit Artikel → abgelehnt, nichts angelegt; 3 → Position „ohne FA“ Menge 3. IDEAL: Maße 0 + Hinweis „Maße 0 bei Positionen ohne FA (IDEAL …)“; AKE: Maße 12,4/67,3/294,6 wie die FA-Positionen derselben Artnr. -1 nicht einzeln getippt (gleiche Serverregel wie 0) | OK |
| 162.6 | zweiter Auftrag (BA-2026-00006) mit demselben freien Artikel 50000005 → gespeichert, keine Doppelvergabe-Meldung | OK |
| 162.7 | BA-2026-00006 in Übergeben: Haken gelöst, Menge 1,5, Position 50002326 entfernt, freie Position hinzugefügt, gespeichert; Kopf ohne Eingabefelder; Audit `ModifiedAt/By/ByWindows` an Kopf und Position gesetzt; 50002326 im Vorschlag wieder „3 frei“. Kandidat hinzufügen nicht einzeln geklickt | OK |
| 162.8 | Versendet mit offener freier Position → „Versendet nur, wenn alle Positionen verpackt sind — nicht verpackt (1): 50000005 (ohne FA)…“; nach Haken + Speichern gelingt es, Statusverlauf „Versendet 18.11.2026 13:43 · Gerald Weichbold“, `VersendetAt/ByUserId` gesetzt | OK |
| 162.9 | UI: Versendet zeigt keine Eingabefelder für Positionen. Direkter POST nicht abgesetzt | UI OK / POST NICHT MOEGLICH (Unit-Test deckt) |
| 162.10 | kein Knopf Übergeben→Erledigt (nur „Auf Versendet setzen“). Manipulierter POST nicht abgesetzt | UI OK / POST NICHT MOEGLICH (Unit-Test deckt) |
| 162.11 | Liste zeigt Spalte „Versendet am“ (BA-2026-00007: 18.11.2026 13:43). Filter/Paging nicht durchgespielt | teilweise; Rest Mensch |
| 162.12 | Auswahl zeigt nur GL, QA-LACK1, QA-LACK2 (nicht QA-LACK3 inaktiv, nicht QA-LACK4); QA-LACK1 gewählt → Dienstleister „QA Lackiererei Voll GmbH“, CreditorId gesetzt. POST mit inaktivem/Nicht-Beschichter nicht abgesetzt | OK / POST NICHT MOEGLICH (Unit-Test deckt) |
| 162.13 | IDEAL: HauptFA 1060414 mit Sage-Dienstleister „GL“ → Kreditor GL vorbelegt. Gegenprobe ohne/mehrere Treffer nicht durchgeführt | positiv OK, Gegenprobe Mensch |
| 162.14 / P-1 | Chrome: Druck BA-2026-00007 – Brief-Anschrift Name/Straße/PLZ Ort/Land, HauptFA in der Kopftabelle, Pos. 1..4, Menge „1“/„3“, Verpackt ☑, „ohne FA“ markiert, **Bemerkung 2 Zeilen sichtbar**. PDF (`/Pdf/7`, 71 976 Bytes) im Chrome-PDF-Viewer: gleicher Inhalt, **Bemerkung 2 Zeilen sichtbar**, Briefkopf-Hinweis nicht im PDF. Edge nicht geprüft | Chrome OK; Edge NICHT MOEGLICH (nur Chrome-Automation) |
| 162.15 | BA-2026-00006 mit QA-LACK2 (ohne Anschrift): Empfänger = Name, gelber Hinweis „Anschrift des Kreditors unvollständig …“ am Bildschirm | OK (Ausdruck ohne Hinweis: im PDF von 00007 belegt) |
| 162.16 | Bemerkung getippt → „⚠ nicht gespeichert“ erscheint am Feld. Rückfrage-Dialog nicht ausgelöst (Automationsgrenze) | Anzeige OK / confirm NICHT MOEGLICH |
| 162.17 | Reklamation aus BA-2026-00007 (Erledigt) mit freier Position + einer FA-Position → BA-2026-00008, „ohne FA“ übernommen, beide „offen“ (Verpackt=0), Kreditor QA-LACK1 übernommen | OK |
| 162.18 | Benutzer L nicht verfügbar (Windows-Anmeldung = Admin) | NICHT MOEGLICH |
| 162.19 | AKE: Haken gesetzt, „auch kommissionierte zeigen“ umgeschaltet → Seite neu, Haken weg | OK |
| 162.20 | Tastatur-/Zoom-Prüfung nicht durchgeführt; Zustand ist als Text sichtbar („vergeben“, „verpackt“, „offen“) | teilweise; Rest Mensch |

Dienst: keine Dienständerung außer Version; kein Sage-Schreibweg berührt, `SageFaArbeitsgaengeAnlegenAktiv` aus.

### 7. Publish

Laut Auftrag **nicht** angelegt (`publish\paket6-*` macht der Folgelauf).

### Manuelle Testcheckliste für den Menschen

1. 162.4 Zwei Tabs, gleiche Position, nacheinander erstellen → zweiter abgelehnt mit Nummer des ersten.
2. 162.5 Menge -1 einmal tippen (Erwartung wie 0).
3. 162.9 / 162.10 / 162.12 direkte POSTs per Entwicklertools (Versendet bearbeiten, Übergeben→Erledigt, inaktiver bzw. Nicht-Beschichter-Kreditor) → abgelehnt.
4. 162.11 Statusfilter „Versendet“, Spaltenfilter `status` und „Versendet am“, Seitenwechsel.
5. 162.13 IDEAL Gegenprobe: Sage-Dienstleister ohne passenden bzw. mit zwei passenden Beschichtern → nur Text + Hinweis.
6. 162.14 / P-1 in **Edge** (Druck + PDF) mit gespeicherter zweizeiliger Bemerkung.
7. 162.16 Rückfrage „Bemerkung ist nicht gespeichert — trotzdem fortfahren?“ bei Drucken/PDF/Statusknopf/Speichern; Abbrechen lässt den Text stehen.
8. 162.18 mit einem Benutzer nur `beschichtungsauftrag` (Lesen): keine Checkboxen/Statusknöpfe, POST verweigert.
9. 162.20 Terminal-Ansicht, Zoom 200 %, nur Tastatur.
10. P-2 (Sage-Anschrift der Beschichter) und P-4 (Mengengerüst Altbestand) vor dem Deploy.
11. Gesamter TS-162 auf dem Testsystem nach Publish (Folgelauf) – vorher Beschichter in `/Creditors` kennzeichnen.

## Befund Testsystem IDEAL :88 (Koordination 2026-11-18 15:05)
- **Lücke:** Bestehende Aufträge haben nach SQL/161 `CreditorId = NULL`; die Vorbelegung (Kurzname) greift nur bei Neuanlage, der Kopf ist ab Übergeben gesperrt
  (Antwort 10) → Altaufträge bekommen **nie** eine Brief-Anschrift. Am :88 von Hand per UPDATE nachgetragen (BA-2 „GoodLack“ ohne Treffer).
- **Nachbesserung (nächster Lauf):** (a) Einmal-Skript mit Trockenlauf im Manifest: CreditorId für Altaufträge nach der Kurzname-Regel nachtragen;
  (b) Kreditor auch in Übergeben wählbar, solange keiner gesetzt ist; (c) prüfen, ob im Entwurf Drucken/PDF fehlen soll.

## Nachtrag v1.99.42 (Goal-Lauf [[2026-11-18-goal-lauf-nachbesserungen-v1-99-42]], Befund 15:05 + Backlog 15:14)

- **Kreditor nachtragen:** in *Übergeben* wählbar, solange `CreditorId` leer (Auswahl „Lackierer (Kreditor) nachtragen“, gespeichert mit
  „Speichern“; danach fest). Server prüft: schon gesetzt → abgelehnt, kein aktiver Beschichter → abgelehnt. Kopf (RAL, Termine) bleibt fest.
- **Einmal-Skript** `SQL/Einmalig/2026-11-18_v1.99.42_BeschichtungsauftragKreditorNachtragen.sql`: Altaufträge ohne Kreditor, nicht storniert,
  Dienstleister = Kurzname genau eines aktiven Beschichters (Regel wie `KreditorVorbelegen`); `@Trockenlauf=1` im Manifest, Melde-Liste für
  „kein Beschichter“/„mehrdeutig“; Scharflauf nur nach Freigabe + DB-Backup.
- **Druck:** Barcode 10 mm rechts neben dem Titel, nur waagrechte Linien, „Seite x von y“ unten rechts in Druck und PDF unabhängig vom
  Fußbereich der Vorlage (`_PrintBriefkopfFuss.cshtml`). Nachweis-Screenshot im QA-Abschnitt.
- Commit `9d0a55e3`, TS-163.7–163.10. Status unverändert bis zum QA-Lauf.

## QA-Nachweis v1.99.42 (qa-agent, 2026-11-18)

Gemeinsamer Lauf über die Nachbesserungen v1.99.42 (Changelog [[2026-11-18-v1-99-42-nachbesserungen]]), Diff `9b6b3e0d..0af91b73` (7 Commits)
im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, plus QA-Fix `b5463336`. Selbst in diesem Lauf geprüft, nicht delegiert.
Derselbe Block steht in allen vier betroffenen Specs (BF-CSV, Beschichtungsauftrag-Ausbau, Sage-Userfelder, Sage-AG-Anlage).

**1. Build und Tests** (Stand `b5463336`): `dotnet build IdealAkeWms.slnx` 0 Fehler, 30 Warnungen (neu: CS8604 `BfCsv.cs:124`, siehe H2).
`dotnet test`: IdealAkeWms.Tests **3291 bestanden / 1 übersprungen / 0 Fehler** (vor dem QA-Fix 3288/1/0); IDEALAKEWMSService.Tests **790/0/0**.

**2. Review** (Diff samt Umfeld gelesen: `BfCsv`, `BfAuftragWorkflow.FreigabeFehler/FreigebenAsync`, `BfAuftragRepository`, `UsersController.KuerzelPruefenAsync`,
`BeschichtungsauftragWorkflow.BearbeitenAsync`, `Print.cshtml`, `_PrintBriefkopfFuss.cshtml`, `SageUserfelder.Erklaeren` mit allen Aufrufern,
`SageFaArbeitsgangService` Kandidaten/Nachlesen, `SageFaArbeitsgangAnlageRepository.UnklarBestaetigenAsync`, View `SageFaArbeitsgaenge/Index`,
SQL/162, SQL/163, Einmal-Skript, FreshInstall, Manifest, beide Migrationen):

| Nr | Schwere | Befund | Fundstelle | Behandlung |
|---|---|---|---|---|
| F1 | mittel | `IstUnklar` prüfte `StartsWith(UnklarText)`, und genau dieser Text wurde in v1.99.42 geändert. Unklar-Zeilen, die v1.99.38–1.99.41 mit dem alten Text gespeichert haben, wären nach dem Update **automatisch erneut gesendet** worden (Dublette in Sage) und hätten keinen Bestätigen-Knopf bekommen | `IdealAkeWms/Services/SageFaArbeitsgaenge.cs:37-41` (vorher), `Views/SageFaArbeitsgaenge/Index.cshtml:164` | **behoben** `b5463336`: gemeinsamer Anfang `UnklarAnfang = "Antwort unklar"` für Dienst-Regel, Repository und View; Test `SageFaArbeitsgaengeControllerTests.IstUnklar_ErkenntAltenUndNeuenText` (alter Text, neuer Text, echter Fehler). Auf DEV IDEAL nachgeprüft (163.12) |
| H1 | Hinweis | EF-Migration `20261118145602_AddBfPositionArbeitsschritte` legt nur die Spalte an; den Nachtrag der Arbeitsschritte macht allein `SQL/163` | Migration, `SQL/163_AddBfPositionArbeitsschritte.sql` | angenommen: Update-Skript führt SQL/163 in der Schema-Phase aus; das UPDATE läuft auch nach EF-Migrate (auf DEV belegt: 2 Positionen nachgetragen, getrimmt). Kein Fix |
| H2 | Hinweis | neue Warnung CS8604 `Z("", "ORDER_NUMBER", a.Nummer)` mit `string?` | `IdealAkeWms/Services/BfCsv.cs:124` | kosmetisch (freigegebene Aufträge haben immer eine Nummer). Offen |
| H3 | Hinweis | Test-CSV vor der Freigabe hat `ORDER_REMARK_4` leer: Kürzel wird erst bei der Freigabe festgeschrieben (wie Termine/Bemerkung) | `BfAuftragWorkflow` Freigabe, `BfAuftraegeController` Test-CSV | gewollt (Snapshot der Freigabe). Kein Fix |
| H4 | Hinweis | `[RegularExpression]` für das Kürzel läuft vor dem Trimmen: „ WAK“ mit Leerzeichen wird abgelehnt statt getrimmt | `UserEditViewModel` Kuerzel | harmlos (klare Meldung). Kein Fix |
| H5 | Hinweis | Einmal-Skript zeigt in der Zählung `NULL` statt `0`, wenn kein Altauftrag passt (DEV AKE) | `SQL/Einmalig/2026-11-18_v1.99.42_BeschichtungsauftragKreditorNachtragen.sql` Zählblock | kosmetisch; maßgeblich `TREFFER: 0` stimmt. Kein Fix |

Ohne Befund geprüft: keine Werte in `<script>`-Blöcken, kein `hidden` neben `d-*`, kein neues `?? 0`, keine Parallel-Arrays; Kürzel-Eindeutigkeit serverseitig
(`KuerzelPruefenAsync` + gefilterter UNIQUE-Index `UQ_Users_Kuerzel`); Kürzel-, Semikolon- und Ganzzahl-Sperre serverseitig in **einer** Funktion `FreigabeFehler`
(Anzeige und Freigabe); Kreditor-Nachtrag serverseitig (schon gesetzt / kein aktiver Beschichter → abgelehnt); Einmal-Skript-Regel = `KreditorVorbelegen`
(zwei Orte, gleiche Regel, im Skript-Kopf benannt); `Bestaetigen` nur Admin (Class-Level `RequireAdminAccess`), schreibt nichts an Sage.
Audit: Users (Edit), BF-Freigabe, Beschichtungsauftrag (`Geaendert`), Sage-AG-Bestätigen, Einmal-Skript – auf DEV in der DB gesehen.
Migrations-Disziplin: SQL/162 + SQL/163 mit `COL_LENGTH`/`sys.indexes`-Guards, DDL je Batch, History separat; FreshInstall an **beiden** Stellen (Spalten
`Users.Kuerzel`, `BfAuftraege.BestellerKuerzel`, `BfAuftragPositionen.Arbeitsschritte`, Index, beide `MigrationId`); AgentJobs nicht betroffen; nicht daten-destruktiv.
Manifest `nachlauf.psd1`: SQL/162, SQL/163, Einmal-Skript (Trockenlauf, `Braucht` SQL/161), BfCsv-Text nachgezogen; `NachlaufManifestDriftTests` grün.
Version 1.99.42 in beiden `AppVersion.cs`, Anwender-Changelog, TS-163 in `docs/TESTSZENARIEN.md` + Index-Zeile 163. ADR 0020: keine neue/geänderte Einstellung.

**3. Nachweisart:** automatisch belegt: CSV-Aufbau/Kodierung (`BfCsvServiceTests`), Freigabe-Sperren (`BfAuftragWorkflowTests`), Kürzel-Prüfung (`UsersControllerTests`),
Kreditor-Nachtrag (`BeschichtungsauftragWorkflowTests`), Userfeld-Meldung (`SageUserfelderTests`), Unklar-Regel (`SageFaArbeitsgangServiceTests`, `SageFaArbeitsgaengeControllerTests`).
Manual-UAT (nicht als grün gezählt): UNIQUE-Index, SQL/162/163, Einmal-Skript, echtes Model-Binding der Formulare, Druck/PDF-Seitenzähler, WiCAM-Import, echtes SData.
Die Manual-UAT-Teile sind unten auf DEV abgearbeitet, soweit möglich.

**4. Geänderte bestehende Tests** – alle *Verhalten durch die Nachbesserung gewollt geändert*, kein „Fixture bis grün“:
`BfCsvServiceTests` (Vorlage → echte `BF00000002.csv`; Semikolon-Test → `Lagerplatz_KommaStattSemikolon`; Kodierungs-Test von BestellerName auf Arbeitsschritte,
weil der Name nicht mehr in die CSV geht) – P-1 (2); `BfAuftragWorkflowTests` (Fixture-Benutzer mit Kürzel, Materialnummer „M-1“ → „11“ wegen Ganzzahl-Sperre,
Semikolon-Test umbenannt/angepasst, Test für entfernte `Feld()` gelöscht) – P-1 (2) + Kürzel; `SageFaArbeitsgangServiceTests.Nachlesen_KeinFund_…` (Fehler → unklar) – N1;
`NachlaufManifestDriftTests` (SQL/162, SQL/163 ergänzt) – Liste.

**6. DEV-Deploy** (erhöhte Shell; Zielordner `D:\Dev\{AKE,IDEAL}\{Web,Service}` vorher geleert, erhalten `appsettings.DEV.json*`, `logs`, `App_Data`):
`dev-deploy.ps1 -Quelle <Worktree>` je Standort zweimal (Stand `0af91b73`, dann `b5463336`): **AKE** Exit 0 „HTTP 200, Fusszeile v1.99.42“,
**IDEAL** Exit 0 „HTTP 200, Fusszeile v1.99.42“; DEV-Absicherung lief je Standort. Beide Migrationen in beiden DEV-DBs; danach `SQL/162` und `SQL/163`
per `sqlcmd` je DB: Exit 0, idempotent (Spaltenbreiten 20/20/400 Byte, `UQ_Users_Kuerzel` gefiltert `[Kuerzel] IS NOT NULL`).

TS-163 auf DEV IDEAL (Testdaten Kennung `QA-163`; `BfCsvPfad = D:\Dev\wicam-pool\ideal-qa`, `BfCsvAktiv = true` nur für den Test):

| TS | Durchgeführt | Ergebnis | Status |
|---|---|---|---|
| 163.1 | `BF99990163` freigegeben, Dienst schrieb `BF99990163.csv` (792 Byte, `CsvStatus` 2) | 15 Zeilen, nur CRLF, jede Zeile 12 `;`, keine `"`, nur ASCII; Zeilen 1–13 byte-gleich zur Vorlage `BF00000002.csv` bis auf die Auftragsnummer; Position ohne Materialnummer fehlt | OK (Byte-Aufbau); WiCAM-Import **NICHT MÖGLICH** (kein Pool, Manual-UAT) |
| 163.2 | Datenzeilen geprüft | `G-KT-I-BLENDE-SW-80-510-V01;3;2;EILT - 18.11 - 1;K-02,0,0,0;K-02;WAK;19.11.2026;2026-12-02;ST EG KA;;;` und `G-TK-I-BLENDE-SW-80-710-V01;1;5;18.11;H4-02,0,0,0;H4-02;WAK;19.11.2026;2026-12-02;LA KA;;;` (Arbeitsschritte aus SQL/163-Nachtrag, getrimmt) | OK |
| 163.3 | `BF99990164` geöffnet | Block nennt „Pos 1 Matchcode: Semikolon“, „Pos 2: Materialnummer „1,5“ ist nicht ganzzahlig“ und das fehlende Kürzel | OK |
| 163.4 | Testuser Kürzel `wak` gespeichert, Liste `?colf_kuerzel=wak` | DB `WAK`, Audit gesetzt; Spalte „Kürzel“, Filter liefert genau Testuser | OK |
| 163.5 | zweiter Benutzer `WAK`, dann `A B`, dann `zu-lang-12345` | „Kürzel WAK ist schon an Testuser vergeben.“; Formatmeldung „nur Buchstaben und Ziffern, höchstens 10 Zeichen“; `maxlength=10` kappt; nichts gespeichert | OK |
| 163.6 | Freigeben ohne Kürzel; Kürzel nachgetragen; erneut freigeben | erst „Freigabe von BF99990163 abgelehnt: Besteller Testuser hat kein Kürzel …“, Status bleibt, nichts geändert; dann freigegeben ohne Neuabschicken, Details „Testuser (WAK)“, `BestellerKuerzel` WAK | OK |
| 163.7 | `BA-2026-00006` (Übergeben) per SQL auf `CreditorId NULL`, Dienstleister `qa-lack1 `; Auswahl „Lackierer (Kreditor) nachtragen“ → QA-LACK1, Speichern | `CreditorId` 2289, Dienstleister „QA Lackiererei Voll GmbH“, RAL/Termine/Positionen unverändert, Audit gesetzt; Auswahl danach weg | OK |
| 163.8 | – | Direkt-POST ohne Entwicklertools nicht ausführbar; Leser ohne Schreibrecht nicht prüfbar (nur eine Admin-Identität) | NICHT MÖGLICH – Server-Regel automatisch belegt (`Kreditor_InUebergeben_NurNachtragen_SolangeLeer`) |
| 163.9 | Einmal-Skript **nur Trockenlauf** auf DEV IDEAL und AKE | IDEAL: `BA-2026-00006 → 2289 QA Lackiererei Voll GmbH`, `TREFFER: 1`, nichts geändert; stornierte Aufträge mit passendem Kurznamen nicht dabei. AKE: `TREFFER: 0`. Nach 163.7 zweiter Trockenlauf IDEAL `TREFFER: 0` | OK; Scharflauf bewusst nicht |
| 163.10 | `BA-2026-00007` mit 36 Positionen (32 temporär kopiert): PDF-Knopf `/Beschichtungsauftraege/Pdf/7` und Druck über Chrome-Druckmodul (`--print-to-pdf` der Druckansicht) | Barcode klein rechts neben dem Titel, nur waagrechte Linien, Tabellenkopf auf Seite 2 wiederholt, „Seite 1 von 2“ / „Seite 2 von 2“ in **Druck und PDF**; DEV ohne Briefkopf-Vorlage → Zählung auch ohne Fußbereich; einseitig „Seite 1 von 1“ | OK |
| 163.11 | – | braucht Abgleich gegen SData (`Sync:*Enabled` auf DEV verboten) | NICHT MÖGLICH – automatisch belegt (`SageUserfelderTests.Erklaeren_LangeSqlDiagnose_HinweisVorn_AusDiagnose_NieDoppelt`) |
| 163.12 | Warteschlange FA 1060414 per SQL: `PU/180` Fehler mit **altem** Unklar-Text, `IS/200` mit neuem, `VL/190` „HTTP 400“; Seite `?fa=1060414`; Knopf bei PU | Knopf an beiden Unklar-Zeilen, keiner bei HTTP 400; Klick → „FA 1060414: Arbeitsgang 180 als „in Sage vorhanden“ bestätigt“, DB Status 4 (InSageVorhanden), Audit gesetzt. Schalter blieb aus, nichts an Sage | OK (Umlaute der per sqlcmd eingefügten Testtexte verstümmelt – Testdaten-Artefakt, nicht App) |

Nachweise 163.10 im Hauptcheckout `secondbrain/tests/2026-11-18-qa-v1-99-42-druck-pdf/`: `ts-163-10-druck-chrome-ba-2026-00007-2-seiten.pdf` (Druck),
`ts-163-10-pdf-wms-ba-2026-00007-2-seiten.pdf` (PDF-Knopf), `ts-163-10-pdf-wms-ba-2026-00007-1-seite.pdf`, `ts-163-10-druckansicht-bildschirm-ba-2026-00007.png`
(Bildschirmansicht). Bildschirmfotos aus dem Browser-Tab scheiterten am Renderer-Timeout der Erweiterung; Druck und PDF liegen daher als gerenderte PDF-Dateien vor.

**Aufgeräumt (DEV IDEAL, per Abfrage belegt):** BF-Aufträge/-Positionen 0, Merkmal-Definition/-Werte `QA-163` gelöscht, `BfCsvAktiv = false`, `BfCsvPfad` leer,
Ordner `ideal-qa` entfernt, Testuser-Kürzel NULL, `BA-2026-00006` zurück auf `CreditorId 2290` / „QA Lackierer ohne Anschrift“, temporäre Positionen an
`BA-2026-00007` gelöscht, `OperationConfigs` PU/IS/VL `SageArbeitsgangnummerManuell` NULL, Sage-AG-Warteschlange leer. `SageLagerbuchungAktiv`,
`SageFaArbeitsgaengeAnlegenAktiv`, alle `Sync:*Enabled` unverändert aus. SyncLogs bleiben. Nichts an Sage geschrieben, kein Test-/Produktivsystem berührt.

**7. Publish:** in diesem Lauf nicht erstellt – der Aufrufer macht Paket 7 selbst (`publish\` nicht angefasst).

**Deploy (aus dem echten Diff):** Web ja, Dienst ja (`SageFaArbeitsgangService`, Sdata-Abgleiche mit `Erklaeren(ex)`, `BfCsvService` über `BfCsv`),
Migration **ja** (SQL/162, SQL/163; additiv, SQL/163 mit Daten-Nachtrag). Reihenfolge: DB-Backup → SQL/162, SQL/163 (Update-Skript Schema-Phase) → Web → Dienst →
Einmal-Skript Kreditor im Trockenlauf, Scharflauf nur nach Freigabe der Liste + Backup. Vor `BfCsvAktiv`: Kürzel aller BF-Besteller pflegen.
Nach dem Merge nur neu publishen, wenn der Merge getestete Dateien mit parallelen main-Änderungen zusammenführt.

**Manuelle Checkliste (offen, für den Menschen):**
1. 163.1 WiCAM-Import einer vom Dienst geschriebenen Datei am echten Pool (Testsystem IDEAL), Vergleich mit `BF00000002.csv`.
2. 163.8 Direkt-POST mit zweitem Kreditor bzw. Nicht-Beschichter; Leser ohne Schreibrecht sieht keine Auswahl.
3. 163.9 Scharflauf des Einmal-Skripts auf dem Testsystem (nach Backup, über das Update-Skript), danach Trockenlauf `TREFFER: 0`.
4. 163.10 Druck in Edge am Drucker-Dialog sichtbar prüfen (hier über das Chrome-Druckmodul belegt).
5. 163.11 falsche Userfeld-Einstellung gegen echtes SData (Testsystem) je Abgleich, danach zurücksetzen.
6. 163.12 Sage-AG unklar im echten Ablauf; E1b (Schreibtest am IDEAL-Ziel) bleibt offen.
7. Kürzel für alle BF-Besteller vor Go-live pflegen.
8. Testsystem-Durchlauf TS-163 komplett mit dem Publish-Paket, danach Merge (Schranke 2).
