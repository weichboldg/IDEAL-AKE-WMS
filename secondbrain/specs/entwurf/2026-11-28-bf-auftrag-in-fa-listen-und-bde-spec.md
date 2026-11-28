---
type: spec
title: BF-Aufträge in der BDE-Teileliste sehen, abarbeiten, rückmelden (Stufe D)
slug: 2026-11-28-bf-auftrag-in-fa-listen-und-bde-spec
status: Entwurf
created: 2026-11-28
updated: 2026-11-28
source_backlog: "[[2026-11-28-bf-auftrag-in-fa-listen-und-bde]]"
depends_on: "[[2026-11-24-bf-auftrag-baugruppen-aufloesen-und-als-fa-zeigen-spec]]"
task: ""
jira: "WMS-10"
worktree: ""
branch: ""
standort: IDEAL
affected_code:
  - IdealAkeWms/Models/BdeTeilMeldung.cs (neu BfAuftragPositionId, BfAuftragEinzelteilId)
  - IdealAkeWms/Data/ApplicationDbContext.cs (:1595-1603 Filter UX_BdeTeilMeldungen_Schluessel, neuer UX_BdeTeilMeldungen_BfAuftrag, FKs Restrict)
  - IdealAkeWms/Services/BdeTeilelisteService.cs (LadenAsync :258-273, Lesewege :267/:297/:400/:871, neu BfZeilenAsync nach LagerZeilenAsync :336-373, MeldenAsync :563-584, neu MeldeBfZeileAsync, SpaltenZeile :222-241, Summieren :465-507, TeilelisteZeile :42-75)
  - IdealAkeWms/Controllers/BdeTeilelisteController.cs (:192-209 BF-Zeilen laden, :298-304 Formular-Schlüssel BF|)
  - IdealAkeWms/Views/BdeTeileliste/ (Kennzeichnung BF-Zeile)
  - IdealAkeWms/Controllers/BdeBookingsController.cs (Teilmeldungen-Liste :278-315, BF-Anzeige)
  - IdealAkeWms/Services/BdeTageshistorie.cs (:34, :75-78 Ziel „BF-Auftrag“)
  - IdealAkeWms/Services/BfAuftragWorkflow.cs (StornierenAsync :318-344 Hinweis auf vorhandene Meldungen)
  - IdealAkeWms/Controllers/BfAuftraegeController.cs + Views/BfAuftraege/Details.cshtml (Fortschritt je Einzelteil, nur Anzeige)
  - IdealAkeWms/Migrations/<neu>_AddBdeTeilMeldungBfAuftrag.cs
  - SQL/<nnn>_AddBdeTeilMeldungBfAuftrag.sql
  - SQL/00_FreshInstall.sql
  - scripts/deploy/nachlauf.psd1
  - docs/TESTSZENARIEN.md (neues Kapitel), TS-193/TS-196 Querverweis
  - IdealAkeWms/Views/Help/ (Hilfe BDE-Teileliste + Blechfertigung, Changelog)
  - secondbrain/architektur/adr/<neu> (ergänzt ADR 0038), fallstricke §67 ergänzen
open_questions:
  - "1. Variante A (BF-Teile als eigene Zeilenart in der BDE-Teileliste, nur WMS, Muster Lagerbestellung ADR 0038) oder Variante B (je BG einen Fertigungsauftrag in Sage anlegen und über den vorhandenen FA-Abgleich holen)? Empfehlung A. Beantwortet zugleich Rückfrage 11 der Vorgänger-Spec."
  - "2. Start erst nach Umsetzung der Vorgänger-Spec Stufe B/C (Zeile = Einzelteil aus der Ressourcenliste, Empfehlung) – oder vorab eine Zwischenlösung auf Ebene der heutigen BF-Positionen (BG/FT), die nach der Vorgänger-Spec umgebaut werden muss?"
  - "3. Eigene Werkbank-Option „BF-Aufträge in der Teileliste“ (wie „Lagerbestellungen in der Teileliste“) oder genügt das vorhandene Gate BlechfertigungAktiv plus die Kürzel-Regel (Empfehlung, keine neue Einstellung)?"
  - "4. (Fachbereich: Benjamin Andl-Braun) Mit „FA-Listen“ ist die BDE-Teileliste der Blechfertigung-Werkbänke gemeint (dort sehen, abarbeiten, rückmelden)? Vorschlag: ja, nur dort. FA-Liste, FA-Struktur, Leitstand und Kommissionierliste zeigen weiter nur Sage-FAs; die BF-Aufträge bleiben zusätzlich unter Blechfertigung › BF-Aufträge."
  - "5. (Fachbereich: Benjamin Andl-Braun) „Rückmelden“ heißt Fertigmeldung im WMS (wer, wann, Gut/Ausschuss je Teil und Arbeitsschritt, wie bei Lagerbestellungen), ohne Meldung an Sage und ohne Lagerbuchung? Vorschlag: ja."
  - "6. (Fachbereich: Benjamin Andl-Braun) Welche Zeilen erscheinen: nur die zu fertigenden Einzelteile, oder zusätzlich die bestellte Baugruppe selbst an ihrem Arbeitsschritt (z. B. Schweißen/Zusammenbau), mit den Einzelteilen aufklappbar wie eine FA-Baugruppe? Vorschlag: beides nach derselben Regel – eine Zeile erscheint an jeder Werkbank, deren Kürzel in ihren Arbeitsschritten steht."
  - "7. (Fachbereich: Benjamin Andl-Braun) Ab wann und bis wann erscheint ein BF-Auftrag in der Teileliste, und welcher Termin gilt für die KW-Auswahl? Vorschlag: ab „freigegeben“ und CSV an WiCAM geschrieben, bis „fertig“ oder „storniert“; KW nach dem FE-Termin des BF-Auftrags."
  - "8. (Fachbereich: Benjamin Andl-Braun) Soll der BF-Auftrag automatisch „fertig“ werden, wenn alle Teile an allen Arbeitsschritten fertig gemeldet sind, oder meldet der Schachtler ihn weiter von Hand fertig und sieht in der Detailseite den Fortschritt? Vorschlag: von Hand, mit Fortschritt."
  - "9. (Fachbereich: Benjamin Andl-Braun) Der Laser (LS) hat keine Teileliste (Start-Kürzel ST, KA, SL, SM, PU, ZU, IF; „kein LS“, Antwort 2026-11-16). Reicht es, wenn BF-Teile erst ab dem Schritt nach dem Laser im BDE erscheinen, oder soll auch das Lasern über das BDE gemeldet werden? Vorschlag: ab dem Schritt nach dem Laser; das Lasern kommt über WiCAM."
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

> [!warning] Abhängigkeit: erst nach der Vorgänger-Spec
> Diese Spec setzt auf [[2026-11-24-bf-auftrag-baugruppen-aufloesen-und-als-fa-zeigen-spec]] auf (Stufe D, dort Out-of-Scope und Rückfragen 11/12).
> Gebraucht werden daraus die Tabelle `BfAuftragEinzelteile` mit `Menge`, `Arbeitsschritte`, `Gestrichen`, `Art` und `Uebergeben` (Stufe B) und die
> Freigabe auf Einzelteil-Ebene (Stufe C). Beide gibt es im Code noch nicht: `BfAuftragPosition` ist die unterste Ebene (`IdealAkeWms/Models/BfAuftrag.cs:69-101`).
> Mail-Punkt 3 (Baugruppen → Ressourcenliste → Teile) gehört **dort** hin und wird hier nicht spezifiziert. Rückfrage 2 klärt, ob vorab eine
> Zwischenlösung gewünscht ist.

> [!info] Nummern nicht festgelegt
> Stand 2026-11-28: `AppVersion.Version = "1.99.89"` (`IdealAkeWms/AppVersion.cs:5`), höchstes Skript `SQL/169_AddBdeTeilMeldungIstAbschliessendUndStandardsicht.sql`,
> höchstes Kapitel TS-209 (`docs/TESTSZENARIEN.md:15085`). Vorläufig vergeben: SQL/170 ([[2026-11-26-bde-nur-fa-meldung-entfernen-spec]]), SQL/173,
> TS-213…216, v1.99.92…95 (Lauf-Notizen [[2026-11-27-spec-lauf-backlog-0810]], [[2026-11-28-spec-lauf-backlog-1008]]). Die Vorgänger-Spec nennt noch
> SQL/167 und TS-189; beide sind inzwischen belegt (`SQL/167_AddStockMovementAbteilung.sql`, TS-189 Sage-Buchungen). Die Vorgänger-Spec bekommt
> ihre Nummern also beim Dev-Start neu. Diese Spec nimmt die **nächste freie Nummer nach der Vorgänger-Spec** (SQL, TS, Version) und prüft sie beim Dev-Start.

## Ziel / Nutzen (das Warum)

Die BF-Aufträge der Blechfertigung gehen heute als CSV an WiCAM. Danach weiß das WMS nur noch, ob der Schachtler den ganzen Auftrag fertig gemeldet
hat (`BfAuftragWorkflow.FertigMeldenAsync` `:284-313`). Die Werker an Kanten, Stanzen, Schweißen und den anderen Schritten sehen die Teile im BDE nicht.
Die BDE-Teileliste nimmt nur Strukturknoten aus Sage-FAs und Lagerbestell-Positionen auf (`BdeTeilelisteService.LadenAsync` `:258-273`). Mail-Punkt 4
(Benjamin, 2026-11-28) verlangt, dass die BF-Bestellungen dort erscheinen, damit die Mitarbeiter sie **sehen, abarbeiten und rückmelden**. Damit ist
Rückfrage 12 der Vorgänger-Spec im Kern beantwortet.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope (nur IDEAL, Gate `BlechfertigungAktiv`; beschrieben ist Variante A, siehe Rückfrage 1):**
- Dritte Zeilenart **„BF-Auftrag“** in der BDE-Teileliste (Sichten Einzeln und Summiert) neben FA-Zeilen und Lagerbestell-Zeilen.
- Fertigmeldung dieser Zeilen (Teilfertig und Fertig, einzeln und als Block) als `BdeTeilMeldung`: dieselbe Tabelle, derselbe Doppelschutz, dieselbe
  Rücknahme und dasselbe Admin-Storno wie bei Lagerbestellungen (ADR [[0038-teilmeldung-auch-fuer-lagerbestell-positionen]]).
- Die Teilmeldungen-Liste und die Tageshistorie zeigen BF-Meldungen mit der BF-Nummer.
- In der BF-Detailseite wird der Fortschritt je Einzelteil und Arbeitsschritt nur angezeigt.
- Im Storno-Text eines BF-Auftrags erscheint ein Hinweis auf vorhandene Teilmeldungen.

**Out-of-Scope:**
- Mail-Punkt 3 (Auflösung BG → Einzelteile): Vorgänger-Spec.
- BF-Aufträge als Zeilen in FA-Liste, FA-Struktur, Leitstand und Kommissionierliste. Das hängt an Rückfrage 4; der Vorschlag lautet nein (Begründung im Lösungsentwurf).
- Jedes Schreiben nach Sage: keine Fertigmeldung an Sage, kein Sage-FA (Variante B wäre eine eigene Spec), keine Lagerbuchung.
- Automatisches „Fertig“ des BF-Auftrags (Rückfrage 8; der Vorschlag lautet: von Hand wie heute).
- BDE-Seite „FA-Sicht“ und Arbeitsgänge (`WorkOperation`): BF-Teile bekommen keinen Arbeitsgang.
- Laser/WiCAM-Rückmeldung (Rückfrage 9).

## Fachliche Anforderungen

| Nr | Anforderung | Quelle |
|---|---|---|
| F1 | Eine BF-Zeile erscheint in der Teileliste einer Werkbank, wenn (a) `BlechfertigungAktiv` = true, (b) die Werkbank die Teileliste wirksam hat (`WerkbankKuerzelFamilie.TeilelisteWirksam`), (c) das Kürzel der Werkbank ein Token der Arbeitsschritte der Zeile ist (`BdeTeilelisteService.TraegtText` `:141-142`, dieselbe Regel wie für Lagerbestellungen) und (d) der BF-Auftrag im sichtbaren Status ist (Rückfrage 7; Vorschlag `Freigegeben` und `CsvStatus = Geschrieben`). | Mail Pkt 4, Rückfrage 3/7 |
| F2 | Zeilenquelle = übergebenes Einzelteil (nicht gestrichen, Art Einzelteil, mit Material-Nr.) einer nicht gestrichenen BG-Position, Sollmenge = Einzelteil-Menge. Nach Rückfrage 6 zusätzlich die BG-Position selbst mit ihren Arbeitsschritten, Einzelteile als aufklappbare Kinder (Muster `TeilelisteZeile.Kinder` `:63`). Einzelteile „vom Lager“ oder mit Hinweis erscheinen nie. | Vorgänger F2/F5, Rückfrage 6 |
| F3 | Spalten wie bei FA-Zeilen. Abweichend: Hauptauftrag = BF-Nummer (z. B. `BF00000012`), Position = BG-Position, Abteilung leer, Kunde leer, Beschichtet leer, FE-Termin = `BfAuftrag.FeTermin` (Rückfrage 7), Material/Arbeitsschritte/Matchcode aus dem Einzelteil-Snapshot. In Summiert erscheint die BF-Nummer in „Haupt-FAs“ wie „LB &lt;Nr&gt;“ bei Lagerbestellungen (`:486-487`). | Muster F10 Lagerbestellung |
| F4 | Die BF-Zeile ist textlich gekennzeichnet („BF-Auftrag“, Symbol und Text, nicht nur Farbe) und filterbar wie jede Zeile. | CLAUDE.md Kontrast |
| F5 | Melden: Teilfertig/Fertig, einzeln, als Block und über die Summiert-Zeile, mit derselben Mengenregel (`MengenFuerArtOk` `:663-677`). Am Schreibort wird erneut geprüft: Gate, Werkbank wirksam, Auftrag im sichtbaren Status, Einzelteil übergeben und nicht gestrichen, Kürzel in den Arbeitsschritten, noch kein Abschluss. Jede Abweisung kommt mit Grund in die Sammelmeldung. | ADR 0026, 0037 |
| F6 | Doppelmeldung (zweites Terminal, Wiederholung) speichert nichts und meldet „bereits gemeldet von … um …“. Anwendungssperre je BF-Zeile und Kürzel, gefilterter UNIQUE als letzte Wache. | ADR 0026, 0038 |
| F7 | Rücknahme durch den Werker und Storno durch den BDE-Admin funktionieren unverändert (`ZuruecknehmenAsync` `:821`, `BdeBookingsController`). Die Teilmeldungen-Liste zeigt bei BF-Meldungen „BF00000012 Pos 3“ statt Haupt-FA. | ADR 0038 Folgen |
| F8 | Die BF-Detailseite zeigt je Einzelteil die Kürzel aus seinen Arbeitsschritten mit dem Stand offen / teilgemeldet / fertig (wer, wann). Nur Anzeige. Rollen wie heute. | Mail „rückmelden“ sichtbar machen |
| F9 | Storniert der Schachtler einen freigegebenen BF-Auftrag mit Teilmeldungen, nennt die Erfolgsmeldung die Anzahl. Die Meldungen bleiben stehen (kein stilles Löschen), die Zeilen verschwinden aus der Teileliste. | Melden statt still |
| F10 | AKE und IDEAL mit `BlechfertigungAktiv = false` verhalten sich unverändert. FA-Zeilen und Lagerbestell-Zeilen ändern sich nicht (Bestand, Waisen-Zählung, AG-Status). | Regression |

## Technischer Loesungsentwurf

### Ist-Stand (belegt)

| Befund | Beleg |
|---|---|
| Die Teileliste kennt zwei Zeilenarten: FA-Knoten und Lagerbestell-Positionen als Projektion (`Node` nicht gespeichert, `HauptFA 0`) | `BdeTeilelisteService.cs:40-48`, `LagerZeilenAsync` `:336-373`, Concat `:427` |
| Formular-Schlüssel `LB|<Id>` wird vor `TeilSchluessel.Parse` abgetrennt | `BdeTeilelisteService.cs:145-151`, `BdeTeilelisteController.cs:300-304` |
| Melden verzweigt je Art; die Lagerbestell-Variante bucht nicht, ändert die Bestellung nicht und schreibt nichts an Sage | `MeldenAsync` `:579-582`, `MeldeLagerZeileAsync` `:595-659` |
| `BdeTeilMeldungen` trägt zwei Arten; jeder Leseweg filtert `WarehouseRequisitionItemId` | ADR 0038, [[fallstricke]] §67; Lesewege `BdeTeilelisteService.cs:267, 297, 353, 400, 863, 871`, `BdeBookingsController.cs:278, 302`, `BdeTageshistorie.cs:34` |
| UNIQUE-Filter: FA `… AND [WarehouseRequisitionItemId] IS NULL`, Lager `… IS NOT NULL` | `ApplicationDbContext.cs:1595-1603` |
| Status `Bestellt` wird nur beim Abschicken gesetzt, `Freigegeben` nur bei der Freigabe. Es gibt keinen Rückweg von `Freigegeben` nach `Bestellt`, und kein Raw-SQL ändert oder löscht BF-Aufträge | `BfAuftragWorkflow.cs:172, 227, 300, 335`; Suche `Status = BfAuftragStatus.` (alle .cs) und `UPDATE/DELETE … BfAuftr` (alle Dateien): keine weiteren Treffer |
| Positionen werden nur im Entwurf entfernt | `PositionEntfernenAsync` `:139-150` (`PruefeBearbeitung` alsSchachtler:false → nur Entwurf `:353-354`) |
| Termine des Auftrags werden bei der Freigabe festgeschrieben | `FreigebenAsync` `:220-221` (`Schachteltermin`, `FeTermin`) |
| `BfAuftragPosition.SageFa` wird nie geschrieben (nur Deklaration, Mapping, Schema) | `BfAuftrag.cs:98`, `ApplicationDbContext.cs:1884`, `SQL/158:82`; Suche `SageFa\b` (alle .cs/.cshtml/.sql) |
| Die Teileliste gehört zum Gate `BdeTeileliste` an der Werkbank, Lagerbestellungen zusätzlich zur Werkbank-Option `BdeTeilelisteLagerbestellungen` | `BdeTeilelisteController.cs:192-193`, `ProductionWorkplacesController.cs:268-269` |
| Gate Blechfertigung: nur `"true"` öffnet | `RequireBlechfertigungAktivAttribute.cs:23`, Default `"false"` `AppSettingDefinitions.cs:191` |

### Variantenvergleich (Rückfrage 1)

| | **A – WMS-intern in der Teileliste (Empfehlung)** | B – Sage-FA je BG |
|---|---|---|
| Idee | BF-Einzelteile als dritte Zeilenart, Meldung als `BdeTeilMeldung` (ADR 0038 ein zweites Mal angewendet) | Bei der Freigabe legt das WMS je BG einen Sage-FA an; der FA kommt über View → `FaHierarchyNodes` → Materialisierung in alle FA-Übersichten |
| Sage | nichts | Schreibweg zum **Anlegen** fehlt: `FertigungsauftragService` wird nur mit ServiceOption 4 für AG-Positionen in bestehenden FAs benutzt (`IDEALAKEWMSService/Services/SageFaArbeitsgangClient.cs:9-25`). Ob Sage die Ressourcenliste als FA-Stückliste übernimmt und der FA in `vw_IDEAL_AKE_WMS_FAListe` erscheint, ist **ungeprüft** (Prüfschritt P-D3) |
| Zeitpunkt sichtbar | sofort nach Freigabe | erst nach Sage-Anlage und dem nächsten Struktur-Abgleich + Materialisierung |
| Nebenwirkungen | keine außerhalb der Teileliste | Sage-Disposition und Reservierungen, FA erscheint auch in Kommissionierliste und Leitstand (gewollt?), WiCAM-CSV läuft parallel weiter, Fehlerpfad „FA angelegt, WMS-Freigabe gescheitert“ |
| Rückmeldung an Sage | nein (Rückfrage 5) | möglich, aber eigener Weg ([[2026-10-30-fa-rueckmeldung-an-sage]] ist nur Idee) |
| Aufwand | 1 Migration (2 Spalten, 1 Index), Service-Erweiterung nach vorhandenem Muster | Sage-Schreibtest, neuer Schreibweg mit Queue, Fehler- und Rückwege, Freigabe-Umbau |
| Die BF-Nummer und die `int`-Schlüssel der Struktur (`FaHierarchyNode.cs:19-25`) | kein Konflikt (eigene Spalten) | kein Konflikt (echte Sage-BelID) |

Ein interner FA in `ProductionOrders`/`FaHierarchyNodes` (die dritte denkbare Variante) scheidet aus. Die Materialisierung markiert jeden
`ProductionOrder`, den die Struktur nicht liefert, als „in Sage vermisst“ und meldet ihn (`FaMaterializationPlanner.cs:78-85`). Die Knoten sind
`int` (`FaHierarchyNode.cs:19-25`) und vertragen `BF00000001` nicht (Befund der Vorgänger-Spec, Lösungsentwurf „Anzeige wie ein FA“).

**Empfehlung A.** Sie deckt Mail-Punkt 4 vollständig ab, denn Werker sehen die Teile im BDE, arbeiten sie ab und melden sie zurück. Muster und
Doppelschutz gibt es schon, und am Sage-Bestand ändert sich nichts. B lohnt erst, wenn die Teile in Sage nachkalkuliert oder in der
Kommissionierung geführt werden sollen. Dann wäre B eine eigene Spec mit Sage-Schreibtest.

### „FA-Listen“ (Rückfrage 4)

Die FA-Liste (IDEAL) ist eine gruppierte, serverseitig gefilterte und paginierte Liste über `ProductionOrders` mit Termin- und Kopfdaten-Postfiltern
(`ProductionOrdersController.cs:77-166`, nur Haupt-FA seit v1.99.82). Eine zweite Datenquelle darin bräuchte eigene Spalten-Getter, Termine und
Paginierung über zwei Quellen und brächte dem Werker nichts, weil er im BDE arbeitet. Darum der Vorschlag: „FA-Listen“ = BDE-Teileliste. FA-Liste,
FA-Struktur und Leitstand bleiben unverändert. Die BF-Liste (`BfAuftraegeController.Index`) bleibt die Übersicht für Besteller und Schachtler.

### Bausteine (Variante A)

1. **Schema:** `BdeTeilMeldung.BfAuftragPositionId int?` (FK → `BfAuftragPositionen`, Restrict) und `BfAuftragEinzelteilId int?`
   (FK → `BfAuftragEinzelteile`, Restrict). Eine BF-Meldung hat immer `BfAuftragPositionId`. `BfAuftragEinzelteilId` ist bei einer Einzelteil-Zeile
   gesetzt und bei einer BG-Zeile null (Rückfrage 6). Platzhalter wie ADR 0038: `HauptFA 0`, `VaterFA NULL`, `SubFA 0`, `Position NULL`,
   `BdeBookingId NULL`, `WarehouseRequisitionItemId NULL`. Restrict ist unkritisch. Positionen werden nur im Entwurf entfernt, Einzelteile nur bei
   „Neu auflösen“ im Status `Bestellt` ersetzt (Vorgänger F4), und Meldungen entstehen erst ab `Freigegeben`, einen Rückweg gibt es nicht (Ist-Stand).
2. **Doppelschutz:** neuer gefilterter UNIQUE `UX_BdeTeilMeldungen_BfAuftrag (BfAuftragPositionId, BfAuftragEinzelteilId, Kuerzel) WHERE [IsCancelled] = 0
   AND [IstAbschliessend] = 1 AND [BfAuftragPositionId] IS NOT NULL`. NULL im Unique-Index zählt als gleich, deshalb ist die BG-Zeile je Position und
   Kürzel eindeutig. `UX_BdeTeilMeldungen_Schluessel` bekommt zusätzlich `AND [BfAuftragPositionId] IS NULL`. Der Lager-Index braucht nichts, weil
   BF-Meldungen `WarehouseRequisitionItemId NULL` haben. App-Guard für InMemory wie bei Lagerbestellungen.
3. **Art-Filter in jedem Leseweg (§67 erweitert):** FA-Wege lesen `WarehouseRequisitionItemId == null && BfAuftragPositionId == null`
   (`BdeTeilelisteService.cs:267, 297, 400, 871`). Die Lager-Wege bleiben bei `WarehouseRequisitionItemId != null`, BF-Wege lesen
   `BfAuftragPositionId != null`. Der Dev-Lauf prüft jeden Leseweg per Grep `BdeTeilMeldungen` und hält das Ergebnis in der Spec fest.
4. **Lesen:** `BfZeilenAsync(kuerzel)` neben `LagerZeilenAsync`. Es lädt die Aufträge im sichtbaren Status (F1/Rückfrage 7) mit Positionen und
   Einzelteilen, filtert nach F2 und `TraegtText(Arbeitsschritte, kuerzel)` und projiziert auf `FaHierarchyNode` (wie `:359-363`) plus neues
   `TeilelisteZeile.Bf` (record `BfPosition(AuftragId, Nummer, PositionId, Position, EinzelteilId?, Termin)`). `FormSchluessel` wird
   `BF|<PositionId>|<EinzelteilId oder leer>`. `Bauen` hängt die Zeilen wie `lager` an. Der KW-Filter nimmt `Bf.Termin`, wie bei `Lager` (`:434`).
   `SpaltenZeile` und `Summieren` behandeln `Bf` wie `Lager` (`:226-235`, `:476-491`). Der Aufruf im Controller (`:192-209`) übergibt
   `mitBf = BlechfertigungAktiv` (Rückfrage 3: ohne Werkbank-Option).
5. **Melden:** `MeldenAsync` verzweigt auf `MeldeBfZeileAsync`, gebaut nach `MeldeLagerZeileAsync` mit Sperre, Prüfung am Schreibort (F5),
   `MengenFuerArtOk`, `SpeichernAsync` mit Gewinner-Laden und `IstAbschliessend` immer ausdrücklich (§ zu ADR 0037). `MeldeAuftrag` bekommt
   `BfPositionId`/`BfEinzelteilId`. Der Controller trennt `BF|` vor `TeilSchluessel.Parse` ab. Ein ungültiger Schlüssel weist wie bisher die ganze Anfrage ab.
6. **Anzeige außerhalb der Teileliste:** Die Teilmeldungen-Liste zeigt für `hauptfa` und Ziel „BF &lt;Nr&gt; Pos &lt;n&gt;“ (`BdeBookingsController.cs:302, 315`),
   die Tageshistorie das Ziel „BF-Auftrag“ (`BdeTageshistorie.cs:34, 78`). In der BF-Detailseite kommt eine Abfrage der Meldungen je Position und
   Einzelteil dazu; die Symbole entstehen aus `NeutraleSymbole` plus Meldungsstand (kein `FaAgStatusService`).
7. **Storno-Hinweis:** `StornierenAsync` zählt die nicht stornierten BF-Meldungen des Auftrags und hängt sie an den Text an (F9).

### Ponytail
Wiederverwendet werden Zeilenart-Muster, Meldepfad, Mengenregel, Rücknahme, Admin-Storno, Sammelmeldung, Token-Regel und Spalten-Getter der
Lagerbestellung. Bewusst weggelassen: neue Tabelle für BF-Meldungen (ADR 0038 Begründung), Werkbank-Option (Rückfrage 3), Arbeitsgänge für
BF-Teile, Anzeige in FA-Liste/Struktur/Leitstand (Rückfrage 4), Auto-Fertig (Rückfrage 8) und Sage-Rückmeldung.

## Migrations-/SQL-Auswirkungen

- Model → `dotnet ef migrations add AddBdeTeilMeldungBfAuftrag` → `SQL/<nnn>_AddBdeTeilMeldungBfAuftrag.sql` (nächste freie Nummer **nach** der
  Vorgänger-Spec, die `BfAuftragEinzelteile` anlegt; ohne diese Tabelle scheitert der FK):
  - je Spalte `IF COL_LENGTH(N'dbo.BdeTeilMeldungen', N'BfAuftragPositionId') IS NULL ALTER TABLE … ADD … INT NULL;`, dasselbe für
    `BfAuftragEinzelteilId`, jeweils eigener Batch; FKs `FK_BdeTeilMeldungen_BfAuftragPositionen_…` / `…_BfAuftragEinzelteile_…` mit
    `IF OBJECT_ID(N'…', N'F') IS NULL`, `ON DELETE NO ACTION`.
  - Vorbedingung im Skript: `IF OBJECT_ID(N'dbo.BfAuftragEinzelteile', N'U') IS NULL RAISERROR('Vorgänger-Skript fehlt', 16, 1)`.
  - `UX_BdeTeilMeldungen_Schluessel` neu mit erweitertem Filter: nur wenn `sys.indexes.filter_definition` den neuen Teil noch nicht enthält,
    `DROP INDEX` + `CREATE UNIQUE … WHERE … AND [BfAuftragPositionId] IS NULL` (Muster SQL/165/169). Der Bestand hat keine BF-Meldungen, darum
    kein Konflikt beim Neuaufbau.
  - `UX_BdeTeilMeldungen_BfAuftrag` und Index `IX_BdeTeilMeldungen_BfAuftragEinzelteilId` mit `IF NOT EXISTS (sys.indexes …)`.
  - `__EFMigrationsHistory`-Insert in separatem Batch mit Guard.
- `SQL/00_FreshInstall.sql` an **zwei** Stellen: Spalten, FKs und Indizes bei `BdeTeilMeldungen` (nach `BfAuftragEinzelteile`, sonst FK ohne Ziel)
  sowie `MigrationId`.
- `SQL/AgentJobs/`: nicht betroffen (keine Pflichtspalte, keine MERGE-Ziele; der Dev-Lauf prüft per Grep `BdeTeilMeldungen`).
- Additiv, nicht destruktiv. Der Index-Neuaufbau ist kurz, `BdeTeilMeldungen` ist eine kleine Tabelle.

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| keine neue | – | – | – | Gelesen, unverändert: `BlechfertigungAktiv` (öffnet die BF-Zeilen), Werkbank-Option `BdeTeileliste`, Master `ProduktionsauftragHierarchisch` | `BdeTeilelisteController`, `BdeTeilelisteService` | IDEAL | – |

Nur wenn Rückfrage 3 eine eigene Werkbank-Option verlangt: `ProductionWorkplace.BdeTeilelisteBfAuftraege` (bool, Default aus, nur speicherbar mit
`BdeTeileliste`, Muster `ProductionWorkplacesController.cs:268-269`). Die Option bräuchte dann eine zweite Spalte in derselben Migration und eine
Zeile in dieser Tabelle.

## Audit-Feld-Auswirkungen

- `BdeTeilMeldung` erbt `AuditableEntity`: Anlage mit `CreatedAt = DateTime.Now`, `CreatedBy/-Windows` aus `ICurrentUserService` (Terminal-Benutzer),
  Werker in `BdeOperatorId` wie `MeldeLagerZeileAsync` `:649-657`. Rücknahme/Storno setzen `Modified*` über die vorhandenen Wege.
- BF-Auftrag, Positionen und Einzelteile werden durch Meldungen **nicht** geändert (kein `Modified*`, kein RowVersion-Konflikt mit dem Schachtler).
  Beim Storno bleibt es beim vorhandenen `Geaendert(a, jetzt)`.

## Rollen und Zugriff

Unverändert. Teileliste, Melden und Rücknahme laufen über `[RequireBdeActive]` + `[RequireBdeUserAccess]`, die Teilmeldungen-Liste über
`[RequireBdeShiftleadAccess]` und das Storno über `[RequireBdeAdminAccess]`. Die BF-Detailseite mit Fortschritt bleibt bei `RequireBfBestellenAccess`
(Besteller nur eigene, `BfAuftraegeController.cs:91`) + `RequireBlechfertigungAktiv`. Werker ohne BF-Rolle sehen die BF-Zeile, kommen aber nicht auf
die BF-Detailseite: In der Teileliste gibt es keinen Link, sondern nur den Text „BF00000012“. Keine neue Rolle → `controller.md` und `RoleOverview` bleiben.

## Listen-View (ADR 0005)

Keine neue Listenseite. Die BF-Zeilen laufen durch die vorhandene Teileliste (Server-Spaltenfilter, Pagination, Zahnrad) und die vorhandene
Teilmeldungen-Liste. Ihre Getter liefern den angezeigten Text (Zelle = Filter, [[fallstricke]] §26). Die Fortschrittsanzeige in der BF-Detailseite
gehört zur vorhandenen Client-Mode-Positionstabelle (`BfAuftraege/Details`), neue Spalten mit `data-col-key`.

## Prüfabfragen (Mensch)

- **P-D1 Kürzel der BF-Teile (WMS-DB IDEAL Test, nach Umsetzung der Vorgänger-Spec):** Welche Arbeitsschritte tragen die Einzelteile, und
  welche davon haben eine Teileliste-Werkbank?
  ```sql
  SELECT LTRIM(RTRIM(s.value)) AS Kuerzel, COUNT(*) AS Einzelteile,
         MAX(CASE WHEN w.Id IS NULL THEN 0 ELSE 1 END) AS HatTeilelisteWerkbank
  FROM dbo.BfAuftragEinzelteile e CROSS APPLY STRING_SPLIT(e.Arbeitsschritte, ' ') s
  LEFT JOIN dbo.ProductionWorkplaces w ON LTRIM(RTRIM(w.ArbeitsschrittCode)) = LTRIM(RTRIM(s.value))
       AND w.BdeAktiv = 1 AND w.BdeTeileliste = 1 AND w.SageMissingSince IS NULL
  WHERE LTRIM(RTRIM(s.value)) <> ''
  GROUP BY LTRIM(RTRIM(s.value)) ORDER BY 1;
  ```
  Kürzel ohne Teileliste-Werkbank erscheinen nirgends (Rückfrage 9).
- **P-D2 Vor der Auflösung, auf heutigem Stand:** dieselbe Frage auf den BF-Positionen
  (`FROM dbo.BfAuftragPositionen e` statt `BfAuftragEinzelteile`). Sie zeigt, ob `USER_OSArbeitsschritte` heute überhaupt Kürzel wie `KA`/`ST` trägt.
- **P-D3 (nur Variante B, Sage IDEAL_TEST, Schreibtest durch Gerald/Walter):** Für eine BG mit Ressourcenliste in Sage von Hand einen FA anlegen,
  dann prüfen, ob er in der View erscheint und welche Stückliste er hat:
  `SELECT * FROM <IDEAL_TEST-DB>.dbo.[vw_IDEAL_AKE_WMS_FAListe] WHERE HauptFA = <neue BelID> ORDER BY VaterFA, Position;`
  Alle Aussagen über Variante B bleiben bis dahin **ungeprüft**.

## Akzeptanzkriterien

1. Ein BF-Auftrag im sichtbaren Status (Rückfrage 7) mit einem Einzelteil, dessen Arbeitsschritte `KA` enthalten, erscheint in der Teileliste jeder
   wirksamen `KA`-Werkbank als Zeile mit Hauptauftrag = BF-Nummer, Sollmenge = Einzelteil-Menge und FE-Termin = `BfAuftrag.FeTermin`. An einer
   `ST`-Werkbank erscheint er nicht.
2. Gestrichene Einzelteile, Einzelteile gestrichener Positionen, Einzelteile „vom Lager“ oder mit Hinweis sowie Aufträge im Status Entwurf, Bestellt,
   Fertig oder Storniert erscheinen nicht.
3. Die BG-Zeile erscheint nach Rückfrage 6 mit aufklappbaren Einzelteilen, oder sie erscheint nicht.
4. In Summiert zählt eine BF-Zeile in Summe, Anzahl und offener Summe des Matchcodes mit. „Haupt-FAs“ enthält die BF-Nummer.
5. Teilfertig und Fertig einer BF-Zeile legen eine `BdeTeilMeldung` mit `BfAuftragPositionId` (und `BfAuftragEinzelteilId`) an, ohne `BdeBooking`,
   ohne Lagerbuchung und ohne Statusänderung des BF-Auftrags. Danach ist die Zeile ausgeblendet oder teilgemeldet wie eine FA-Zeile.
6. Eine zweite Fertigmeldung derselben Zeile und desselben Kürzels (zweites Terminal) speichert nichts und meldet „bereits gemeldet von … um …“. In der
   DB greift `UX_BdeTeilMeldungen_BfAuftrag`.
7. Eine manipulierte Anfrage (`BF|` mit fremder Position, Auftrag inzwischen storniert, Kürzel nicht in den Arbeitsschritten, `BlechfertigungAktiv`
   aus) wird mit Grund abgewiesen und schreibt nichts. `BF|abc` weist die ganze Anfrage ab.
8. Rücknahme durch den Werker bis Schichtende und Storno durch den BDE-Admin funktionieren für BF-Meldungen. Die Teilmeldungen-Liste zeigt
   „BF&lt;Nr&gt; Pos &lt;n&gt;“.
9. FA-Zeilen: Waisen-Zählung, „bereits gemeldet“, AG-Status und Ausblenden ändern sich durch BF-Meldungen nicht (Test mit einer BF-Meldung und einer
   FA-Meldung gleichen Kürzels). Lagerbestell-Zeilen ebenso.
10. Die BF-Detailseite zeigt je Einzelteil die Kürzel mit offen / teilgemeldet / fertig (wer, wann). Gemeldet wird dort nicht.
11. Ein Storno eines freigegebenen BF-Auftrags mit Meldungen nennt deren Anzahl. Die Meldungen bleiben, die Zeilen verschwinden aus der Teileliste.
12. Mit `BlechfertigungAktiv = false` erscheinen keine BF-Zeilen. AKE verhält sich unverändert.
13. Migration und `SQL/<nnn>` mit Guards: Ein zweiter Lauf ist ein No-op, ein Lauf ohne `BfAuftragEinzelteile` bricht mit klarer Meldung ab,
    FreshInstall ist an beiden Stellen nachgezogen, und `UX_BdeTeilMeldungen_Schluessel` trägt den neuen Filter.
14. Build und alle Tests sind grün. Neue Unit-Tests decken ab: `Bauen` mit BF-Zeilen, `MeldeBfZeileAsync` (Erfolg, doppelt, storniert, falsches Kürzel,
    Gate aus) und den Art-Filter in den FA-Lesewegen.

## Test-Szenarien

Neues Kapitel in `docs/TESTSZENARIEN.md` („BF-Aufträge in der BDE-Teileliste“, nächste freie TS nach der Vorgänger-Spec) und
`secondbrain/tests/testszenarien-index.md`; Querverweis in TS-193 (Lagerbestell-Zeilen) und TS-196 (Teilfertig).
- **Vorbedingungen:** DEV IDEAL, `BlechfertigungAktiv` an, zwei Teileliste-Werkbänke `KA` und `ST`, ein freigegebener BF-Auftrag mit CSV
  geschrieben, BG mit zwei Einzelteilen (Arbeitsschritte `KA` bzw. `ST KA`), ein Einzelteil gestrichen. Werker W1, W2, BDE-Admin, Schachtler.
- **Schritte:** 1 W1 an `KA` → beide nicht gestrichenen Einzelteile sichtbar, mit BF-Kennzeichnung, Termin, KW-Filter. 2 Summiert → BF-Nummer in
  Haupt-FAs. 3 W1 meldet Teilfertig 2 von 5 → Zeile teilgemeldet. 4 W1 meldet Fertig → Zeile weg, „gemeldete zeigen“ zeigt sie. 5 W2 an `ST` →
  nur das `ST KA`-Teil. 6 Schachtler öffnet die BF-Detailseite → `KA` fertig (W1, Zeit), `ST` offen. 7 W1 nimmt die Meldung zurück → Zeile wieder offen.
  8 BDE-Admin storniert eine Meldung über BDE-Buchungen › Teilmeldungen. 9 Schachtler storniert den Auftrag → Meldung mit Anzahl, Zeilen weg.
- **Negativfälle:** zweites Terminal meldet dieselbe Zeile → „bereits gemeldet“. `BlechfertigungAktiv` aus → keine BF-Zeilen, gesendeter `BF|`-Schlüssel
  wird abgewiesen. Entwurf/Bestellt → nicht sichtbar. FA-Teil mit gleichem Matchcode und Kürzel → beide Zeilen getrennt meldbar. AKE → unverändert.

## Etappen (nur bei epic: true)

Kein Epic. Ein Commit genügt. Liegt die Vorgänger-Spec im selben Bündel, kommt diese Spec **nach** deren Stufe C.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt | – | – |

## Deploy

**Vorläufig – der qa-agent bestätigt gegen den echten Diff.**

- **Web-App:** ja (Teileliste, Melden, Teilmeldungen-Liste, Tageshistorie, BF-Detail).
- **Service:** nein, kein Dienstcode betroffen. Falls der Dienst das Modell mitbringt und der Diff ihn berührt, bestätigt der qa-agent das.
- **Migration:** ja, `SQL/<nnn>_AddBdeTeilMeldungBfAuftrag.sql`, additiv. Reihenfolge: DB-Backup → Vorgänger-Skript (`BfAuftragEinzelteile`) → dieses Skript → Web.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, Nummer/Version beim Dev-Start):
  - `@{ Id='SQL/<nnn>'; Version='<v>'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'; Datei='<nnn>_AddBdeTeilMeldungBfAuftrag.sql'; MigrationId='<id>_AddBdeTeilMeldungBfAuftrag'; Text='Teilmeldungen fuer BF-Auftraege (Spalten, FKs, UNIQUE-Index BF, FA-Index-Filter erweitert).' }` – nach dem Schema-Eintrag der Vorgänger-Spec.
  - `@{ Id='<v>-BfInTeileliste'; Version='<v>'; Typ='Info'; Firma='IDEAL'; Phase='Nachher'; Ausfuehrung='anzeigen'; Text='Freigegebene BF-Auftraege erscheinen in der BDE-Teileliste der Werkbaenke, deren Kuerzel in den Arbeitsschritten der Teile steht (nur bei Blechfertigung aktiv). Rueckmeldung nur im WMS, nicht an Sage.' }`
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Offene Rueckfragen

1. Variante **A** (BF-Teile als eigene Zeilenart in der BDE-Teileliste, nur WMS, Muster Lagerbestellung ADR 0038) oder **B** (je BG einen
   Fertigungsauftrag in Sage anlegen und über den FA-Abgleich holen)? **Empfehlung A** (Vergleich im Lösungsentwurf). Beantwortet zugleich Rückfrage 11
   der Vorgänger-Spec. Entscheidung bitte zusätzlich in `freigabe_entscheidung`.
2. Start erst nach Umsetzung der Vorgänger-Spec Stufe B/C mit Zeile = Einzelteil aus der Ressourcenliste (**Empfehlung**)? Oder vorab eine
   Zwischenlösung auf Ebene der heutigen BF-Positionen (BG/FT), die nach der Vorgänger-Spec wieder umgebaut werden muss?
3. Eigene Werkbank-Option „BF-Aufträge in der Teileliste“ (wie „Lagerbestellungen in der Teileliste“)? Oder genügen das vorhandene Gate
   `BlechfertigungAktiv` und die Kürzel-Regel (**Empfehlung**, keine neue Einstellung)?
4. **(Fachbereich: Benjamin Andl-Braun)** Mit „FA-Listen“ ist die BDE-Teileliste der Blechfertigung-Werkbänke gemeint, also dort sehen, abarbeiten und
   rückmelden? **Vorschlag:** ja, nur dort. FA-Liste, FA-Struktur, Leitstand und Kommissionierliste zeigen weiter nur Sage-FAs; die BF-Aufträge bleiben
   zusätzlich unter Blechfertigung › BF-Aufträge.
5. **(Fachbereich: Benjamin Andl-Braun)** „Rückmelden“ heißt Fertigmeldung im WMS (wer, wann, Gut/Ausschuss je Teil und Arbeitsschritt, wie bei
   Lagerbestellungen), ohne Meldung an Sage und ohne Lagerbuchung? **Vorschlag:** ja.
6. **(Fachbereich: Benjamin Andl-Braun)** Welche Zeilen erscheinen: nur die zu fertigenden Einzelteile? Oder zusätzlich die bestellte Baugruppe selbst
   an ihrem Arbeitsschritt (z. B. Schweißen/Zusammenbau), mit den Einzelteilen aufklappbar wie eine FA-Baugruppe? **Vorschlag:** beides nach derselben
   Regel: Eine Zeile erscheint an jeder Werkbank, deren Kürzel in ihren Arbeitsschritten steht.
7. **(Fachbereich: Benjamin Andl-Braun)** Ab wann und bis wann erscheint ein BF-Auftrag in der Teileliste, und welcher Termin gilt für die KW-Auswahl?
   **Vorschlag:** ab „freigegeben“ und CSV an WiCAM geschrieben, bis „fertig“ oder „storniert“; KW nach dem FE-Termin des BF-Auftrags.
8. **(Fachbereich: Benjamin Andl-Braun)** Soll der BF-Auftrag automatisch „fertig“ werden, wenn alle Teile an allen Arbeitsschritten fertig gemeldet
   sind? Oder meldet der Schachtler ihn weiter von Hand fertig und sieht in der Detailseite den Fortschritt? **Vorschlag:** von Hand, mit Fortschritt.
9. **(Fachbereich: Benjamin Andl-Braun)** Der Laser (LS) hat keine Teileliste (Start-Kürzel ST, KA, SL, SM, PU, ZU, IF; „kein LS“, Antwort 2026-11-16).
   Reicht es, wenn BF-Teile erst ab dem Schritt nach dem Laser im BDE erscheinen? Oder soll auch das Lasern über das BDE gemeldet werden?
   **Vorschlag:** ab dem Schritt nach dem Laser; das Lasern kommt über WiCAM.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)
<!--
  Von der Koordination eingetragen, wortgetreu, je Antwort:
  <Nr>. <Name>, <yyyy-mm-dd>, [<Jira-Schluessel> Kommentar](<Link>): "<Antwort>"
  Das ist fachlicher Input, keine Freigabe. Entscheidend bleibt der Freigabe-Block darunter;
  uebernimmt der Mensch eine Antwort, genuegt dort "wie Fachbereich".
-->

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Variante A (Teileliste, nur WMS) oder B (Sage-FA) →
2. Start nach Vorgänger-Spec Stufe B/C oder Zwischenlösung auf BF-Positionen →
3. Eigene Werkbank-Option oder nur BlechfertigungAktiv →
4. „FA-Listen“ = BDE-Teileliste, übrige FA-Übersichten unverändert (Benjamin) →
5. Rückmelden nur im WMS, nicht an Sage, keine Lagerbuchung (Benjamin) →
6. Zeilen: nur Einzelteile oder auch die Baugruppe mit aufklappbaren Teilen (Benjamin) →
7. Sichtbar ab freigegeben + CSV geschrieben bis fertig/storniert, KW nach FE-Termin (Benjamin) →
8. BF-Auftrag automatisch fertig oder weiter von Hand mit Fortschritt (Benjamin) →
9. BF-Teile erst ab dem Schritt nach dem Laser im BDE (Benjamin) →
