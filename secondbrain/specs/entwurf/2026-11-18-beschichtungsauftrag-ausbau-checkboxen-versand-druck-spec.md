---
type: spec
title: "Beschichtungsauftrag: Verpackt-Haken im Vorschlag und am Auftrag, freie Positionen, Status Versendet, Kreditor-Anschrift, Druck überarbeitet"
slug: 2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck-spec
status: Entwurf
created: 2026-11-18
updated: 2026-11-18
source_backlog: "[[2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck]]"
task: ""
jira: ""
worktree: ""
branch: ""
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
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
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
| 1 | E1 Schema + Status Versendet: Model, Migration, SQL (Nummer beim Start), FreshInstall ×2, Übergänge, `IstDruckbar`, Badge, Statusverlauf, Listen-Spalte „Versendet am“, Tests (AK 10, 12, 13, 18) | offen | |
| 2 | E2 Auftrag bis vor Versendet: Haken, Menge/Entfernen/Hinzufügen in Übergeben, freie Positionen am Auftrag, `GetVergabenAsync` ohne OhneFa, Versendet-Regel, Reklamation-Kopie (AK 7-11, 20, 21). Maße IDEAL hängen an **P-3** (Rückfall 0 + Warnung) | offen | |
| 3 | E3 Vorschlagsliste: Haken hinten Liste + Summiert, Anlage nur Auswahl, freie Positionen im Gruppenkopf (AK 1-6, 22) | offen | |
| 4 | E4 Lackierer aus Kreditoren: `CreditorId`-Auswahl, Server-Validierung, IDEAL-Vorbelegung, Anschrift im Briefkopf (AK 14, 15, 17). Anschrift-Inhalt hängt an **P-2** (Bau unabhängig) | offen | |
| 5 | E5 Druck/PDF: je Position, Pos.-Nr., Menge, HauptFA im Kopf, Verpackt-Spalte, Bemerkung-Absicherung; **P-1** am Anfang der Etappe (AK 16, 19) | offen | |
| 6 | E6 Doku: TESTSZENARIEN + Index, Hilfe/Kurzanleitung, Anwender- und Brain-Changelog, Version beide `AppVersion.cs`, Manifest, feature-map, codebase-Karte (Status-Automat), ggf. fallstricke (freie Positionen nie in Vergaben) | offen | |

## Deploy

*Vorläufig – der QA-Lauf bestätigt gegen den echten Diff.*

- **Web-App:** ja (Controller, Workflow, Repository, Views, Migration im Web-Projekt).
- **Service:** nein (kein Dienstcode betroffen; nur Versionsnummer, falls der Bündelstand ohnehin beide ausliefert).
- **Migration:** ja, additiv (fünf Spalten, ein FK, ein Index). Reihenfolge: Schema vor Web-Start (Phase Schema).
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

1. (R1) Bedeutung des Hakens →
2. (R2) Statusfolge, Übergeben → Erledigt entfällt →
3. (R3) Freie Positionen über Artikelsuche →
4. (R4) Lackierer aus Kreditoren mit IsCoater →
5. (R5) AKE und IDEAL →
6. Versendet nur mit allen Haken →
7. Haken auch in Summiert →
8. Haken im Vorschlag nicht speichern →
9. Freie Positionen im Vorschlag: Eingabe, verpackt, Maße →
10. In Übergeben nur Positionen änderbar →
11. Druck je Position statt summiert →
12. Positionsnummer = laufende Nummer →
13. Menge im Druck `0.###`, nur Druck/PDF →
14. Anschrift live aus Kreditor; Sage-Lieferanschrift (P-2) →
15. Altaufträge ohne Datenskript →
16. Bemerkung: P-1 + Rückfrage bei ungespeicherter Bemerkung →
17. Kreditor-Vorbelegung IDEAL über Kurzname →
