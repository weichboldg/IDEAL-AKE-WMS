---
type: spec
title: BDE-Teileliste (vereinfacht) für die Blechfertigung – Fertigmeldung je Teil, einzeln oder als Block
slug: 2026-11-13-bde-vereinfacht-teileliste-blechfertigung-spec
status: Gemerged
created: 2026-11-16
updated: 2026-11-28
source_backlog: "[[2026-11-13-bde-vereinfacht-teileliste-blechfertigung]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: IDEAL
affected_code:
  - IdealAkeWms/Models/ProductionWorkplace.cs
  - IdealAkeWms/Models/BdeTeilMeldung.cs (neu)
  - IdealAkeWms/Data/ApplicationDbContext.cs
  - IdealAkeWms/Data/Repositories/ProductionWorkplaceRepository.cs
  - IdealAkeWms/Data/Repositories/BdeTeilMeldungRepository.cs (neu, mit Interface)
  - IdealAkeWms/Controllers/ProductionWorkplacesController.cs
  - IdealAkeWms/Views/ProductionWorkplaces/Edit.cshtml, Create.cshtml, Index.cshtml, _BdeTeilelisteField.cshtml (neu)
  - IdealAkeWms/Models/ViewModels/ProductionWorkplaceEditViewModel.cs
  - IdealAkeWms/Services/WerkbankKuerzelFamilie.cs
  - IdealAkeWms/Services/BdeTeilelisteService.cs (neu)
  - IdealAkeWms/Services/BdeBookingService.cs (ReportCompletion-Kern wiederverwendbar)
  - IdealAkeWms/Controllers/BdeTerminalController.cs
  - IdealAkeWms/Controllers/BdeTeilelisteController.cs (neu)
  - IdealAkeWms/Controllers/BdeBookingsController.cs (Liste Teilmeldungen + Storno)
  - IdealAkeWms/Views/BdeTeileliste/ (neu: Index, Summiert, Partials)
  - IdealAkeWms/Views/BdeBookings/Teilmeldungen.cshtml (neu)
  - IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs
  - IdealAkeWms/Services/ColumnFilterTypes.cs
  - IdealAkeWms/Services/Beschichtung/BeschichtungResolver.cs (aus der Beschichtungslogik-Spec, nur Positions-Methode ergänzt – Rückfrage 2)
  - IdealAkeWms/wwwroot/js/bde-teileliste.js (neu)
  - IdealAkeWms/wwwroot/css/bde.css
  - IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs
  - IdealAkeWms/Migrations/<neu>_AddBdeTeileliste.cs
  - SQL/154_AddBdeTeileliste.sql
  - SQL/00_FreshInstall.sql
  - SQL/Einmalig/<datum>_v1.99.28_RemoveParentWorkOperationsFromLeafTokensTeileliste.sql
  - scripts/deploy/nachlauf.psd1
  - docs/TESTSZENARIEN.md
  - IdealAkeWms/Views/Help/Index.cshtml, Changelog.cshtml
  - IdealAkeWms/AppVersion.cs, IDEALAKEWMSService/AppVersion.cs
open_questions:
  - "1. Konflikt Struktur-Erkennung (WorkOperationStructureDetectionService.cs:105-106): Variante V1 (Empfehlung: Kürzel von Teileliste-Werkbänken erzeugen keine Vater-AGs mehr aus Blatt-Kindern + Einmal-Skript für den Bestand), V2 (Vater-AGs bleiben, werden ausgeblendet) oder V3 (keine Änderung, Vater-AGs bleiben offen)? Bestand per P-TL3 erheben."
  - "2. Spalte Beschichtet: Der Resolver der Beschichtungslogik liefert das Kennzeichen je FA (HasCoatingParts), die Kommissionier-/Beschichtungsliste zeigt es je Position (FaHierarchyNode.Beschichtet). Positions-Methode im Resolver ergänzen (Empfehlung) oder FA-Kennzeichen der besitzenden Sub-FA zeigen?"
  - "3. Rücknahme-Frist ohne Schicht: Liefert der Schichtkalender kein Schichtende (aus, Feiertag, keine Schicht), gilt das Ende des Meldetags (Empfehlung)?"
  - "4. Filter je Anmeldung: Speicherort UserViewPreference des Terminal-Benutzers mit Werker und Werkbank im Schlüssel (Empfehlung, keine Migration; folgt dem Werker nicht auf einen anderen Terminal-PC) oder eigene Ablage je Werker?"
  - "5. Block-Meldung: über die angehakten Zeilen der aktuellen Seite (\"alle auf dieser Seite\", Seitengröße wählbar; Empfehlung) oder über alle gefilterten Zeilen aller Seiten?"
  - "6. Baugruppe und enthaltenes Teil mit gleichem Schritt: erscheinen nach der Grundregel ohnehin beide als Zeile. Spätere Etappe E8 nur als Kennzeichnung \"gleicher Schritt auch an Baugruppe X\" (Empfehlung) – oder soll das Teil bis dahin ausgeblendet werden?"
  - "7. Standard-Wochenauswahl ohne gespeicherten Filter: aktuelle KW bis aktuelle KW + 1 als feste Vorgabe ohne AppSetting (Empfehlung) oder als AppSetting nach ADR 0020?"
  - "8. Sichtbarkeit: Zeilen ausblenden, deren besitzender Auftrag (Sub-FA bzw. Vater-Sub-FA des Teils) im WMS erledigt oder storniert ist (Empfehlung)? Zeilen ohne materialisierten Auftrag zeigen mit Kennzeichnung \"kein WMS-Auftrag\"?"
epic: true
etappen:
  - "E1 Schema + Werkbank-Option (ohne Verhaltensänderung)"
  - "E2 Teileliste lesen: Einzeln + Summiert + aufklappbare Baugruppen, Terminal-Weiche"
  - "E3 Fertigmelden einzeln und als Block (Teilmeldung, Sub-FA-AG fertig, Doppel-Schutz)"
  - "E4 Rücknahme: Werker bis Schichtende, BDE-Admin über BDE-Buchungen › Teilmeldungen"
  - "E5 Filter je Werker + Werkbank speichern, Woche relativ, Filter zurücksetzen"
  - "H1 Haltepunkt: BeschichtungResolver (Beschichtungslogik E1) committet + Rückfrage 2 beantwortet"
  - "E6 Spalte Beschichtet über den Resolver"
  - "H2 Haltepunkt: Rückfrage 1 beantwortet + P-TL3 erhoben"
  - "E7 Struktur-Erkennung nach Rückfrage 1 + Einmal-Skript Bestand"
  - "E8 (später, nur nach Freigabe-Antwort 6) Kennzeichnung gleicher Schritt Baugruppe/Teil"
  - "E9 Doku, TS-152, Hilfe, Changelog, Manifest, ADR 0030, Brain, Version"
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: "Empfehlungen 1–8 (V1); Bau nach Arbeitsgang-Mapping und Beschichtung"
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-16"
---

# BDE-Teileliste (vereinfacht) für die Blechfertigung

> [!info] Nummern beim Start prüfen
> Vorschlag, beim Start des Dev-Laufs gegen Worktree, `specs/` und `aufgaben/` prüfen: **Version 1.99.28**, **SQL/154**, **TS-152**,
> **ADR 0030**, Migration nach der von 1.99.27 (`AddOperationConfigSageArbeitsgangnummer`).
> Stand beim Schreiben (2026-11-16): `AppVersion.cs:5` = 1.99.23, höchste Datei `SQL/150_AddPurchaseOrderReceiptAbsendekennung.sql`,
> letztes Kapitel `TS-138`, höchster ADR 0026. Reserviert: 1.99.24 (SQL/151, TS-139, ADR 0027), 1.99.25 (TS-140..145, ADR 0028; SQL/152
> dort entfallen), 1.99.26 (TS-146..150, ADR 0029), 1.99.27 (SQL/153, TS-151). In `specs/` und `aufgaben/` ist nichts ab SQL/154,
> TS-152 oder 1.99.28 vergeben (Suche 2026-11-16).

## Ziel / Nutzen (das Warum)

Das BDE-Terminal gruppiert heute nach Haupt-FA und bietet Arbeitsgänge (`WorkOperation`) an (v1.64.0). Das passt für die Montage. In der
**Blechfertigung** werden aber Einzelteile bearbeitet: Der Werker braucht eine Liste der **Teile nach Matchcode** mit Wochenauswahl,
Filter, Summierung und den Spalten der Vormontage-Listen, und er will gefilterte Arbeitsblöcke **einzeln oder als Block fertigmelden**.
Ein Blechteil (Blatt der FA-Struktur) hat heute keinen eigenen Arbeitsgang; die Spec schafft dafür ein eigenes, schlankes Buchungsziel
(Variante A, Auftrag Gerald) und lässt die Rückmeldung nach oben bewusst weg (Antwort Fachbereich 2).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Werkbank-Option „BDE-Teileliste (vereinfacht)“, Default aus, nur IDEAL (Master `ProduktionsauftragHierarchisch`), Pflege in
  Stammdaten › Werkbänke.
- Teileliste am Terminal einer solchen Werkbank: Zeilen = jeder Strukturknoten (Blechteil **und** Baugruppe/Sub-FA), dessen
  Arbeitsschritte das Kürzel der Werkbank enthalten; Baugruppen aufklappbar; Sicht „Einzeln“ und „Summiert“.
- Fertigmeldung ohne Zeit: einzeln (Menge änderbar, Ausschuss optional) und als Block (volle Sollmenge); Teilmeldung mit stabilem
  fachlichem Schlüssel; bei Baugruppen-Zeilen zusätzlich der eigene Arbeitsgang fertig.
- Schutz gegen Doppelmeldung nach ADR 0026.
- Rücknahme: eigene Meldung bis Schichtende am Terminal; danach nur BDE-Admin über BDE-Buchungen › Teilmeldungen.
- Filter je Werker und Werkbank gespeichert, Woche relativ, „Filter zurücksetzen“.
- Auflösung des Konflikts mit der Struktur-Erkennung (Rückfrage 1) inklusive Bestand.

**Out-of-Scope**
- **BF-Aufträge aus dem Warenkorb / WiCAM-CSV und deren Fertigmeldung über das BDE** ([[2026-11-13-blechfertigung-bf-auftraege-wicam-csv]],
  dortige Stufe 2): andere Datenquelle (Baugruppe ohne Teile), eigene Spec; nicht verschmelzen.
- Rückmeldung nach oben (Vater-Sub-FA, Haupt-FA) – vom Fachbereich ausgeschlossen (Antwort 2).
- Start/Stop, Zeiten, Rüsten an Teileliste-Werkbänken.
- Jedes Schreiben nach Sage (keine PPS-Rückmeldung; [[2026-10-30-fa-rueckmeldung-an-sage]] bleibt Folgearbeit).
- Einstellungen auf Test/Prod setzen; die Option wird nur vom Menschen nach Abnahme eingeschaltet.
- AKE: keine Änderung (Option nur bei Master an sichtbar und speicherbar; AKE-Werkbänke ohne Kürzel).
- Fest verdrahtete Kürzel (`ST`, `KA` …) – es entscheidet allein `ProductionWorkplace.ArbeitsschrittCode`.

## Fachliche Anforderungen

Entscheidungen (verbindlich) aus dem Backlog: Antworten Fachbereich 1–8, Einordnung Koordination, Antwort Benjamin 2026-11-16 (Start-Kürzel
ST, KA, SL, SM, PU, ZU, IF; kein LS).

| Nr | Anforderung | Quelle |
|---|---|---|
| F1 | Werkbank-Option `BdeTeileliste`, Default aus. Wirksam = `BdeAktiv && BdeTeileliste && !BdeNurFaMeldung(global)` und Werkbank mit Kürzel. Wirksam → Terminal zeigt die Teileliste statt der Haupt-FA-Gruppen; es gibt nur Fertigmeldung (Semantik `BdeNurAgFertigmeldung`, Buchung Start = Ende, kein Start/Stop). | Backlog Empf. 1, Antwort vorab 1 |
| F2 | Zeile = jeder `FaHierarchyNode`, dessen Token in `Arbeitsschritte` (Leerzeichen-getrennt, Trim, OrdinalIgnoreCase, wie Dienst) das Kürzel der Terminal-Werkbank enthält – Blatt (`SubFA = 0`) und Baugruppe (`SubFA ≠ 0`). Das Kürzel kommt nur aus `ArbeitsschrittCode` (Kürzel-Familie), nie aus Code-Konstanten. | Antwort 1, Auftrag 2 |
| F3 | Baugruppen-Zeilen sind aufklappbar und zeigen ihre direkten Kinder (`VaterFA = SubFA`) mit Matchcode, Menge, Arbeitsschritte – nur Anzeige. | Antwort 1 |
| F4 | Haben Baugruppe und enthaltenes Teil denselben Schritt, erscheinen beide als Zeile (folgt aus F2). Kennzeichnung „gleicher Schritt auch an Baugruppe …“ erst in E8. | Antwort 1, Rückfrage 6 |
| F5 | Standardspalten sichtbar: Matchcode, Menge, Hauptauftrag, Abteilung, Beschichtet, FE-Termin, Material. Per Zahnrad zuschaltbar: Artikelnummer, Sub-FA, Vater-Sub-FA, Position, Arbeitsschritte, Arbeitsbereich, Kunde, Bemerkung PN, Fertigungsinfo PN, Bezeichnung. | Antwort 5 |
| F6 | Wochenauswahl KW von/bis auf `FE_Termin` des Haupt-FA (`IsoWeekRange`, wie Vormontage). | Antwort 4 |
| F7 | Sicht „Summiert“: Aggregat je Matchcode (Summe Sollmenge, Anzahl Positionen, Haupt-FAs, FE-Termine); Fertigmelden einer Summiert-Zeile meldet alle beitragenden offenen Positionen mit voller Sollmenge. | Backlog, Empf. 4 |
| F8 | Menge einzeln: Gut vorbelegt mit Sollmenge, änderbar; Ausschuss optional; nicht negativ; Gut + Ausschuss > 0. | Antwort 3 |
| F9 | Block: je Zeile volle Sollmenge, Gut = Soll, Ausschuss 0; Bestätigungsdialog nennt Anzahl Zeilen und Summe Sollmenge; Ergebnis als Sammelmeldung (gemeldet n, bereits gemeldet m, abgewiesen k mit Grund). | Antwort 3, Empf. 4 |
| F10 | Teilmeldung je (stabiler Schlüssel + Kürzel): wer (BDE-Mitarbeiter), wann, Werkbank, Terminal, Gut, Ausschuss, Sollmenge zum Meldezeitpunkt, Matchcode. Baugruppen-Zeile: zusätzlich der eigene Arbeitsgang (`WorkOperation` der Sub-FA mit `OperationNumber` = Kürzel) fertig, gleiche Semantik wie `ReportCompletionAsync`. Keine Meldung an Vater-Sub-FA oder Haupt-FA. | Auftrag 3, Einordnung Koordination |
| F11 | Doppelmeldung derselben Zeile (zweiter Tipp, zweites Terminal, Netz-Wiederholung, veraltete Seite) schreibt nichts und meldet „bereits gemeldet von … um … – nichts geändert“. | ADR 0026 |
| F12 | Gemeldete Zeilen sind standardmäßig ausgeblendet; Schalter „gemeldete zeigen“ zeigt sie mit „gemeldet von X, HH:mm, Gut/Ausschuss“. | Ableitung aus F11 |
| F13 | Rücknahme durch den Werker: nur eigene Meldung (gleicher BDE-Mitarbeiter), nur bis Schichtende der Meldung (Rückfrage 3 für Tage ohne Schicht). Danach nur BDE-Admin über BDE-Buchungen › Teilmeldungen (Storno mit Grund). Rücknahme einer Baugruppen-Zeile storniert auch die zugehörige AG-Buchung. | Antwort 8 |
| F14 | Filter (Spaltenfilter, Sicht, Woche relativ als Versatz zur aktuellen KW, „gemeldete zeigen“, Seitengröße) je angemeldetem Werker **und** Werkbank gespeichert und beim nächsten Aufruf geladen; Knopf „Filter zurücksetzen“ löscht die Ablage. | Antwort 6 |
| F15 | Meldungen, deren Schlüssel nach einem Struktur-Sync keine Zeile mehr trifft (Sage hat Position/Artikel geändert), zeigt die Teileliste als Hinweis „n Meldungen ohne passende Strukturzeile“ (Link auf Teilmeldungen-Liste). Nie still. | Melden statt still behandeln |
| F16 | Zeilen mit nicht eindeutigem Schlüssel (P-TL4) werden angezeigt, aber nicht meldbar („Strukturzeile nicht eindeutig – bitte AV“). | Melden statt still behandeln |

## Technischer Lösungsentwurf

### Ist-Stand (belegt, Pfade relativ zum Worktree)

**Struktur-Ids sind nicht stabil.** `FaHierarchySyncService.RunAsync` löscht beide Cache-Tabellen und legt sie neu an
(`IDEALAKEWMSService/Services/FaHierarchySyncService.cs:97-104`, `DELETE FROM dbo.FaHierarchyNodes` + `AddRangeAsync`); `Id` ist
`IDENTITY` (`SQL/00_FreshInstall.sql:1919`). Andere Schreibwege auf `FaHierarchyNodes` gibt es außerhalb der Tests nicht (Suche nach
`FaHierarchyNodes.(Add|Remove)`, `DELETE/TRUNCATE/MERGE … FaHierarchyNodes`: nur `:99`, `:102`). Jede Meldung, die die Node-Id hält,
verliert nach dem nächsten Sync ihre Zeile → **Node-Id ist kein Schlüssel**.

**Stabiler fachlicher Schlüssel.** `(HauptFA, VaterFA, SubFA, Position, Artnr)` (`IdealAkeWms/Models/FaHierarchyNode.cs:19-31`).
- `(VaterFA, SubFA, Position)` allein reicht nicht: Positions-Kollisionen unter demselben Vater kommen vor und werden in der Stückliste
  mit `~n` eindeutig gemacht, Tiebreak über `SubFA` und `Artnr` (`IdealAkeWms/Data/Repositories/FaHierarchyBomRepository.cs:77-95`,
  [[fallstricke]] §10 „Geschwister-Kollisionen … ~n“). Darum `Artnr` im Schlüssel.
- Präzedenz: Der Beschichtungsauftrag hält seine Positionen ebenfalls fachlich (`PositionsSchluessel(VaterFA, SubFA, Position)`,
  [[2026-11-16-beschichtungslogik-je-standort-spec]] Ist-Stand `IBeschichtungsauftragRepository.cs:10`), nicht über die Node-Id.
- Eindeutigkeit ist **ungeprüft** → P-TL4; F16 behandelt Restfälle sichtbar.
- Grenze (bewusst): Ändert Sage Position oder Artikel einer gemeldeten Zeile, erscheint die neue Zeile wieder offen und die alte Meldung
  wird Waise (F15).

**Blatt-Kürzel zählen heute am Vater.** Die Struktur-Erkennung nimmt je Sub-FA die eigene Zeile **und** die direkten Blatt-Kinder in den
Scope (`IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs:100-106`) und legt je Token einen `WorkOperation` am
Vater-Sub-FA an (`:158-179`, Nur-hinzufügen `:149-163`). Ein Blechteil hat also keinen eigenen Arbeitsgang; ein `KA` an Blechteilen
erzeugt `KA` am Korpus ([[2026-09-21-ideal-bde-arbeitsgaenge-aus-struktur-spec]] AK 13, TS-77.18).

**Fertigmeldung ohne Zeit.** `BdeBookingService.ReportCompletionAsync` (`IdealAkeWms/Services/BdeBookingService.cs:268-331`):
Transaktion, `sp_getapplock` je Arbeitsgang (`:275`, `:590-599`), BDE-Gate, Wirksamkeit `NurAgFertigmeldungWirksam` (`:282`),
Familien-Prüfung (`:285`, `:529-545`), verpackt/abgeholt-Sperre (`:288`, `:554-570`), negative Mengen (`:291`), bereits fertig
(`:295-297`), eine `Production/Finished`-Buchung mit `StartedAt == EndedAt` + `IsFinal`-Menge (`:309-328`).
Eine `BdeBooking` braucht einen Arbeitsgang oder eine Tätigkeit (`CK_BdeBookings_Target`/`_TypeTarget`, `SQL/00_FreshInstall.sql:2506-2513`,
`ApplicationDbContext.cs:1522-1526`) → für Blechteile ohne AG ist sie **kein** Buchungsziel; daher die eigene Teilmeldung.

**Kürzel-Familie.** `WerkbankKuerzelFamilie` (`IdealAkeWms/Services/WerkbankKuerzelFamilie.cs:22-29` Familie = gleiche
`ArbeitsschrittCode`, `:46-47` Wirksamkeit Nur-AG-Fertigmeldung). [[fallstricke]] §28: jede Stelle „AGs dieser Werkbank“ ruft den Helfer.

**Terminal und Anmeldung.** Terminal = Web-Benutzer (`BdeTerminal.UserId`, `Models/BdeTerminal.cs:9`; `BdeTerminalController.cs:46`).
Der Werker meldet sich **persönlich** per Personalnummer an (`/api/bde/operator/{nr}`, `wwwroot/js/bde-terminal.js:104-132`); der Zustand
liegt nur im Browser (`currentOperator`, Wechsel `:142-162`), jede Schreibaktion trägt `operatorId`. `BdeOperator.UserId` ist optional
(`Models/BdeOperator.cs:26`). „Anmeldung persönlich“ (Antwort 6) heißt also: BDE-Mitarbeiter, nicht Web-Benutzer.

**Gespeicherte Anzeige.** `UserViewPreference(UserId, ViewKey, SettingsJson)` mit UNIQUE `(UserId, ViewKey)`, `ViewKey` max. 50
(`ApplicationDbContext.cs:1196-1213`). Die API nimmt nur ViewKeys aus `ColumnDefinitions` an (`Controllers/Api/UserViewPreferencesApiController.cs:29-30`),
und das Zahnrad schreibt das **ganze** JSON (`wwwroot/js/column-preferences.js:336`). Filterwerte werden heute nirgends dauerhaft
gespeichert, nur in `sessionStorage` (`wwwroot/js/table-filter.js:34`). → Filter in einem **eigenen** ViewKey, serverseitig geschrieben.

**Schichtende.** `BdeShiftCalendarService.GetShiftEndForBookingAsync(workplaceId, startedAt)` (`Services/BdeShiftCalendarService.cs:19-55`)
liefert das Schichtende oder `null` (Kalender aus, Feiertag, keine Schicht, nach der letzten Schicht).

**Admin-Mittel.** `BdeBookingsController` Klasse `[RequireBdeShiftleadAccess]` (`:12-13`), `Edit`/`Cancel` `[RequireBdeAdminAccess]`
(`:172`, `:204`, `:268-274`, Storno = `IsCancelled` + Grund).

**Vormontage-Bausteine.** `VormontageService`: Kopf-Lookup Kunde/FE-Termin/Abteilung (`Services/VormontageService.cs:97-139`,
Kombigerät-Regel `:105-110`), Positions-Spalten-Getter (`:145-155`), KW-Vorfilter `IsoWeekRange.HauptFasInRange` auf `FE_Termin`
(`:240-246`, `:309-317`), Summiert-Aggregation je Matchcode mit Vorfilter je Position (`:292-371`). `IsoWeekRange.Resolve(von, bis)`
(`Services/IsoWeekRange.cs:58`).

**Beschichtet heute.** Kommissionier- und Beschichtungsliste lesen das Kennzeichen **je Position** `FaHierarchyNode.Beschichtet`
(`Services/KommissionierListenService.cs:66`, `Services/BeschichtungsauftragService.cs:79`). Der geplante `BeschichtungResolver` liefert
`Beschichtet` **je FA** = `HasCoatingParts` ([[2026-11-16-beschichtungslogik-je-standort-spec]] Abschnitt Resolver, `Bestimmen(...)`).
Für ein Blechteil wäre das FA-Kennzeichen des Vaters falsch (Vater ist beschichtet, sobald ein Blatt-Kind beschichtet ist, [[fallstricke]] §20)
→ Rückfrage 2.

**Cockpit.** Zeigt nur laufende Buchungen (`BdeApiController.GetCockpit`, `Controllers/BdeApiController.cs:172-202`, Quelle
`GetActiveCockpitAsync`), keine offenen Arbeitsgänge. Offene AGs wirken im Terminal-Angebot (`:255-286`) und im FA-Baum-Status
(`ArbeitsgangStatusRegel`, [[fallstricke]] §25).

### Varianten zum Konflikt Struktur-Erkennung (Rückfrage 1)

Ohne Änderung bleiben Vater-AGs aus Blatt-Kürzeln (z. B. `KA` am Korpus) an Teileliste-Kürzeln **dauerhaft offen**, weil die Teileliste nicht
nach oben meldet. Auswirkung (belegt): FA-Baum-Status offen/überfällig (`ArbeitsgangStatusRegel`), Terminal-Angebot an Werkbänken derselben
Familie **ohne** Teileliste (`BdeApiController.cs:255-286`), Arbeitsgang-Modal der FA-Struktur. Das Cockpit ist **nicht** betroffen (nur laufende
Buchungen, `:172-202`). Eigene Auswertungen über offene IDEAL-Arbeitsgänge gibt es im Code nicht (`OseonReportingController` ist AKE/OSEON).

| | V1 (Empfehlung) | V2 | V3 |
|---|---|---|---|
| Regel | Für Kürzel, die an einer wirksamen Teileliste-Werkbank stehen, nimmt die Erkennung **keine Blatt-Kinder** in den Vater-Scope; die eigene Zeile des Sub-FA zählt weiter. | Vater-AGs bleiben; Terminal-Angebot und FA-Baum blenden AGs mit Teileliste-Kürzel ohne eigenes Token aus. | nichts |
| Bestand | Einmal-Skript (Trockenlauf) löscht ungebuchte Vater-AGs dieser Kürzel, deren Token nur von Blatt-Kindern stammt; gebuchte nur melden (Muster `SQL/Einmalig/2026-10-07_v1.47.4_RemoveParentWorkOperationsFromChildSubFaTokens.sql`). | keiner | keiner |
| Code | ~5 Zeilen in `WorkOperationStructureDetectionService.cs:105-106` + ein Zähler | Filter an drei Lesestellen (Angebot, Scan, FA-Baum), Regel „ohne eigenes Token“ muss dort jeweils neu berechnet werden | – |
| Datenwahrheit | AG existiert nur, wo Sage ihn anschreibt (passt zu Antwort 2 „am richtigen Teil angeschrieben“) | AG existiert, wird versteckt; Zählungen über `WorkOperations` zählen ihn | dauerhaft offen |

Empfehlung V1: kleinster Eingriff, eine Stelle, und die Daten stimmen statt der Anzeige. Folge: Schaltet jemand die Option später wieder aus,
legt der nächste Lauf die Vater-AGs wieder an (Nur-hinzufügen) – das steht im Warnhinweis der Option.

### Bausteine

1. **Model / Schema (E1)**
   - `ProductionWorkplace.BdeTeileliste` (bool, Default false), Display „BDE-Teileliste (vereinfacht)“.
   - Neue Entität `BdeTeilMeldung : AuditableEntity`: `HauptFA` int, `VaterFA` int?, `SubFA` int, `Position` int?, `Artnr` nvarchar(200)?,
     `Matchcode` nvarchar(200)? (Anzeige-Schnappschuss), `Kuerzel` nvarchar(20), `ProductionWorkplaceId`, `BdeOperatorId`, `BdeTerminalId`
     (FKs Restrict), `GoodQuantity`/`ScrapQuantity`/`SollmengeBeiMeldung` decimal(18,4), `GemeldetAm` datetime2 (Lokalzeit, ADR 0025),
     `BdeBookingId` int? (FK Restrict; nur Baugruppen-Zeile), `IsCancelled` bit, `CancellationReason` nvarchar(500)?.
   - Gefilterter UNIQUE `UX_BdeTeilMeldungen_Schluessel (HauptFA, VaterFA, SubFA, Position, Artnr, Kuerzel) WHERE IsCancelled = 0`
     (SQL Server behandelt NULL im Unique-Index als gleich – gewollt). Schlüssellänge 16 + 400 + 40 Byte < 1700.
   - Indizes `(ProductionWorkplaceId, GemeldetAm)`, `(BdeOperatorId, GemeldetAm)`.
2. **Option pflegen (E1)** – Muster `BdeNurAgFertigmeldung`: Partial `_BdeTeilelisteField.cshtml`, ViewModel, Create/Edit
   (`ProductionWorkplacesController.cs:120`, `:213`), `EditableProperties` (`ProductionWorkplaceRepository.cs:17`), Spalte „BDE“ im Index
   (`ProductionWorkplacesController.cs:50` „Aktiv nur Fertigmeldung“ → zusätzlich „Teileliste“). Feld nur sichtbar bei Master an.
   Server-Validierung beim Speichern (ModelState-Fehler, nichts gespeichert): Option an erfordert Master an, `BdeAktiv` und
   `ArbeitsschrittCode` nicht leer.
   `WerkbankKuerzelFamilie.TeilelisteWirksam(w, nurFaGlobal) => w.BdeAktiv && w.BdeTeileliste && !nurFaGlobal && Code(w) != null`.
3. **Lesen (E2)** – `BdeTeilelisteService` (kein Interface, eine Implementierung; Muster `VormontageService`):
   - Daten: `IFaHierarchyNodeRepository.GetAllAsync()` + `IFaHierarchyOrderInfoRepository.GetAllAsync()` (gecachte Decorators), offene
     Meldungen der Werkbank-Kürzel aus `BdeTeilMeldungen`, Auftragsstatus der besitzenden Sub-FAs (`ProductionOrders.SubOrderNumber` =
     `SubFA` bzw. `VaterFA` als Text, wie `SQL/Einmalig/2026-10-07_…sql:55`) nach Rückfrage 8.
   - Token-Zerlegung: vorhandenes `FaArbeitsschrittSymbole.Tokenize` (`Models/ViewModels/FaHierarchyTreeViewModel.cs:261-264`, laut
     [[2026-11-16-arbeitsgang-mapping-sage-arbeitsgangnummer-spec]] dieselbe Regel wie der Dienst) – keine neue Split-Logik.
   - Kopfwerte, KW-Vorfilter, Spalten-Getter und Summiert-Aggregation: `VormontageService.BuildKopfLookup`, `IsoWeekRange.HauptFasInRange`,
     `BuildPositionsSpalten` wiederverwenden; die Aggregation als eigene kleine reine Methode (Schlüssel Matchcode wie `:329-330`), weil
     sie die beitragenden Schlüssel für F7 mitführen muss.
   - Spaltenfilter: `ColumnFilterHelper.ReadFromQuery` + `Apply`, Typen in `ColumnFilterTypes` (Menge Zahl, FE-Termin Datum; Getter
     formatieren wie die Zelle, [[fallstricke]] §26).
   - Pagination: `PageSize.Resolve` + `PaginationState` + `_Pagination`; Seiteneinheit = Zeile (Einzeln) bzw. Aggregat (Summiert).
4. **Terminal-Weiche (E2)** – `BdeTerminalController.Index` (`:41-73`): ist die aktive Werkbank `TeilelisteWirksam`, Redirect auf
   `BdeTeileliste/Index?workplaceId=…`. Die neue Seite übernimmt die Werker-Anmeldung (gleiche API, `operatorId` im Query der
   Folgeseiten, serverseitig gegen aktive `BdeOperators` geprüft) und die Werkbank-Auswahl; ohne Werker keine Liste. Klassen-Filter
   `[RequireBdeActive]` + `[RequireBdeUserAccess]`.
5. **Melden (E3)** – `BdeTeilelisteService.MeldenAsync(operatorId, workplaceId, terminalId, IReadOnlyList<(Schluessel, Gut, Ausschuss)>)`:
   - Je Zeile in eigener Transaktion: `sp_getapplock` auf `BDE-Teilmeldung:{HauptFA}|{VaterFA}|{SubFA}|{Position}|{Artnr}|{Kuerzel}`
     (Muster `BdeBookingService.cs:590-599`), dann Prüfungen in der Reihenfolge [[fallstricke]] §28: BDE-Gate → Teileliste wirksam → Zeile
     existiert jetzt und trägt das Kürzel (gegen den aktuellen Struktur-Stand, nicht gegen die Seite) → Schlüssel eindeutig (F16) → verpackt/
     abgeholt am besitzenden Auftrag (Logik `EnsureOrderNotPackedAsync`) → Mengen (`IstNegativ`, Summe > 0) → nicht bereits gemeldet.
   - Baugruppen-Zeile: im selben Transaktionsrahmen den `WorkOperation` der Sub-FA mit `OperationNumber` = Kürzel fertig melden. Dazu wird der
     Rumpf von `ReportCompletionAsync` (`:271-330`) in einen privaten Kern ohne eigene Transaktion gezogen; der bestehende öffentliche Weg ruft
     ihn unverändert in `InTransactionAsync`. Die Wirksamkeitsprüfung `:282` akzeptiert `NurAgFertigmeldungWirksam || TeilelisteWirksam`.
     Kein AG vorhanden oder AG bereits fertig → Teilmeldung trotzdem speichern, Hinweis in der Sammelmeldung („Arbeitsgang KA an Sub-FA …
     nicht vorhanden/bereits fertig“). `BdeBookingId` der erzeugten Buchung in die Teilmeldung.
   - DB-Gleichstand: UNIQUE-Verletzung über `Nummernkreis.IstSqlFehler(ex, 2601, 2627)` (`Services/Nummernkreis.cs:21`) → „bereits gemeldet“.
   - Controller `BdeTeilelisteController.Melden` (POST, AntiForgery, JSON); Client sperrt den Knopf mit `window.wmsSperren` (`wwwroot/js/site.js:243`).
     Der Client sendet die Absicht (Schlüssel + Mengen), nie „nächste Zeile“ (ADR 0026).
6. **Rücknahme (E4)**
   - Terminal: `BdeTeilelisteController.Zuruecknehmen(meldungId, operatorId)`: nur `BdeOperatorId == operatorId`, nur `DateTime.Now <` Frist
     (Frist = `GetShiftEndForBookingAsync(workplaceId, GemeldetAm)`, sonst Rückfrage 3), sonst Meldung „nur noch über den BDE-Admin“.
   - Admin: `BdeBookingsController.Teilmeldungen` (Liste, Klasse Shiftlead) + `StornoTeilmeldung` (`[RequireBdeAdminAccess]`, Grund Pflicht).
   - Beide: `IsCancelled = true`, Grund, Modified*; verknüpfte `BdeBooking` ebenfalls `IsCancelled` + Grund „Teilmeldung zurückgenommen“.
     Bereits storniert → „bereits zurückgenommen – nichts geändert“.
7. **Filter speichern (E5)** – Repository `IUserViewPreferenceRepository` direkt aus dem Controller (nicht über die API, die nur
   ColumnDefinitions-Keys annimmt): `UserId` = Web-Benutzer des Terminals, `ViewKey` = `BdeTL:W{workplaceId}:O{operatorId}` (≤ 50 Zeichen),
   JSON `{ sicht, kwVonOffset, kwBisOffset, gemeldeteZeigen, pageSize, colf: {…} }`. Beim GET ohne Query-Parameter werden die Werte geladen
   und die Offsets per `ISOWeek` in `YYYY-Www` der aktuellen Woche umgerechnet; jeder Filter-GET speichert. „Filter zurücksetzen“ =
   `DeleteByUserAndViewAsync` + Redirect auf die Vorgabe (Rückfrage 7). Zahnrad wie überall über ViewKey `BdeTeileliste` /
   `BdeTeileliste/Summiert` in `ColumnDefinitions` (gilt je Terminal-Benutzer, nicht je Werker – Hinweis in der Hilfe).
8. **Beschichtet (E6, nach H1)** – nach Freigabe-Antwort 2; Empfehlung: Resolver bekommt `bool? BeschichtetPosition(BeschichtungKontext k,
   FaHierarchyNode n)` = `n.Beschichtet`, wenn die Kennzeichen-Quelle `FaStruktur` ist, sonst `null` (Zelle „–“). Die Teileliste liest nur diese
   Methode. Ob Kommissionier-/Beschichtungsliste nachziehen, ist Sache der Beschichtungslogik-Spec (gemeldet, nicht mitgebaut).
9. **Struktur-Erkennung (E7, nach H2)** – bei V1: In `WorkOperationStructureDetectionService` die Kürzel wirksamer Teileliste-Werkbänke einmal
   je Lauf bestimmen (`BdeAktiv && BdeTeileliste && SageMissingSince == null`, Werkbänke sind schon geladen `:83-90`) und Token dieser Kürzel aus
   Blatt-Kindern (`:106`) überspringen; Zähler `teileliste_blatt_uebersprungen` (deutsch, ADR 0010). Die globale AppSetting `BdeNurFaMeldung`
   liest der Dienst bewusst nicht (Dienst kennt nur die Werkbank-Option; Hinweis in der Hilfe). Einmal-Skript siehe Migrationen.
10. **Kennzeichnung gleicher Schritt (E8, später)** – nur nach Freigabe-Antwort 6: Blatt-Zeile mit Hinweis „gleicher Schritt auch an
    Baugruppe {Matchcode Vater}“ (Text + Symbol, nicht nur Farbe).

### Oberfläche (Terminal, `frontend-design` Pflicht)

Bootstrap 5 und bestehende Muster (Vormontage-Tabelle, `bde.css`). Fertigungs-Terminal: große Trefferflächen (≥ 44 px), Kontrast WCAG AA
(4,5:1 Text, 3:1 Bedienelemente), Farbe nie allein Bedeutungsträger (gemeldet = Text „gemeldet“ + Symbol), Zeilenzustände ohne `opacity`
([[fallstricke]] §25), genau ein Scrollbalken (v1.64.1). Spaltenfilter Server-Mode (`data-server-column-filter="true"`, alle `<th>` mit
`data-col-key`), Filterkarte mit KW von/bis, Reiter Einzeln/Summiert, „gemeldete zeigen“, „Filter zurücksetzen“. Checkboxen je Zeile +
„alle auf dieser Seite“, Knopf „Auswahl fertigmelden (n Zeilen, Summe x)“. Einzelmeldung in einem Panel mit Gut/Ausschuss. Hardware-Scanner:
`data-hw-scan-ziel` am Werker-Feld wie im Terminal.

### Ponytail (bewusst nicht gebaut)

- Kein `WorkOperation` je Blechteil (Variante B des Backlogs): greift in Erkennung, Terminal und alle AG-Zählungen ein.
- Keine neue Tabelle für Filter (UserViewPreference), keine neue Rolle, keine AppSetting (Rückfrage 7), kein Interface für den Service.
- Keine eigene Split-/KW-/Filterlogik: `Tokenize`, `IsoWeekRange`, `ColumnFilterHelper`, `VormontageService`-Getter.
- Kein Idempotenz-Schlüssel je Anfrage: der fachliche Schlüssel + gefilterter UNIQUE tragen (ADR 0026 Option 3).

## Migrations-/SQL-Auswirkungen

- **Model → `dotnet ef migrations add AddBdeTeileliste`** (Spalte `ProductionWorkplaces.BdeTeileliste` bit NOT NULL DEFAULT 0, Tabelle
  `BdeTeilMeldungen` mit FKs, Indizes, gefiltertem UNIQUE). Additiv, nicht destruktiv.
- **`SQL/154_AddBdeTeileliste.sql`** (Nummer beim Start prüfen), Muster `SQL/97` / `SQL/150`:
  - `IF COL_LENGTH(N'dbo.ProductionWorkplaces', N'BdeTeileliste') IS NULL ALTER TABLE … ADD [BdeTeileliste] BIT NOT NULL CONSTRAINT
    [DF_ProductionWorkplaces_BdeTeileliste] DEFAULT 0;` – eigener Batch.
  - `IF OBJECT_ID(N'dbo.BdeTeilMeldungen', N'U') IS NULL CREATE TABLE …` – eigener Batch; Indizes je `IF NOT EXISTS (sys.indexes …)`;
    der gefilterte UNIQUE mit `WHERE [IsCancelled] = 0`.
  - `__EFMigrationsHistory`-Insert in separatem Batch mit Guard (`COL_LENGTH … IS NOT NULL AND OBJECT_ID(…) IS NOT NULL`).
- **`SQL/00_FreshInstall.sql` an zwei Stellen:** Spalte in `CREATE TABLE ProductionWorkplaces` (bei `:359`) und Tabelle `BdeTeilMeldungen`
  im BDE-Abschnitt (nach `BdeBookingQuantities`, `:2562-2584`), dazu der `MigrationId`-Insert mit Guard.
- **`SQL/AgentJobs/`:** nicht betroffen (Suche nach `ProductionWorkplaces`, `WorkOperations`, `UserViewPreferences`: keine Treffer).
- **Einmal-Skript (E7, nur V1)** `SQL/Einmalig/<datum>_v1.99.28_RemoveParentWorkOperationsFromLeafTokensTeileliste.sql` (ADR 0015/0023):
  Kandidat = `WorkOperation` mit `CreatedBy = 'WorkOperationStructureDetection'`, `OperationNumber` = Kürzel einer Werkbank mit
  `BdeAktiv = 1 AND BdeTeileliste = 1 AND SageMissingSince IS NULL`, an einer Sub-FA, deren **eigene** Arbeitsschritte das Token nicht
  enthalten, aber ein direktes Blatt-Kind. Ohne `BdeBookings` und `IsReported = 0` → löschen; sonst nur melden. Genau eine Zeile
  `DECLARE @Trockenlauf BIT = 1;`, im Trockenlauf `PRINT 'TREFFER: ' + …` (Zahl zu löschender). Keine Wiederholungssperre nötig: nach E7
  entstehen solche AGs nicht mehr, ein zweiter Lauf findet 0 (`TREFFER: 0`). Datenlöschend → DB-Backup vorher.
- Kein Daten-Backfill; die Option ist überall aus.

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| Werkbank-Option `ProductionWorkplace.BdeTeileliste`, Anzeigename „BDE-Teileliste (vereinfacht)“ (kuratierte Maske Stammdaten › Werkbänke) | bool (Schalter) | an/aus; an nur speicherbar bei Master an, `BdeAktiv` an und Kürzel gesetzt (Server prüft) | aus | Terminal dieser Werkbank zeigt die Teileliste nach Matchcode statt der Haupt-FA-Gruppen; nur Fertigmeldung ohne Zeit; meldet Teile und den eigenen Arbeitsgang von Baugruppen. Wirkungslos, solange `BdeNurFaMeldung` global an ist. | `BdeTerminalController.Index`, `BdeTeilelisteController`, `BdeBookingService` (Wirksamkeit), Struktur-Erkennung (E7) | IDEAL (bei Master aus nicht sichtbar) | „Ändert die Arbeitsgang-Erkennung: Für das Kürzel dieser Werkbank entstehen keine Arbeitsgänge mehr an der Baugruppe aus Blechteilen. Ausschalten legt sie beim nächsten Lauf wieder an.“ (nur V1) |

Keine AppSetting/ServiceSetting neu oder geändert (Rückfrage 7). Gelesen, unverändert: `BdeNurFaMeldung`, `BdeSchichtkalenderAktiv`,
Master `ProduktionsauftragHierarchisch`, `Sync:WorkOperationStructureDetectionEnabled`. Nichts wird auf Test/Prod gesetzt.

## Audit-Feld-Auswirkungen

- `BdeTeilMeldung` erbt `AuditableEntity`: Anlage `CreatedAt = DateTime.Now`, `CreatedBy`/`CreatedByWindows` aus `ICurrentUserService`
  (Terminal-Benutzer); der Werker steht fachlich in `BdeOperatorId`.
- Rücknahme/Storno: `ModifiedAt`, `ModifiedBy`, `ModifiedByWindows` (Werker-Rücknahme: Terminal-Benutzer; Admin: Admin). Die mitstornierte
  `BdeBooking` bekommt dieselben Modified*-Werte (Muster `BdeBookingsController.Cancel`).
- Werkbank-Option: Modified* wie heute im Edit (`ProductionWorkplacesController.cs:218-220`).
- Filterablage: `UserViewPreferenceRepository.SaveAsync` setzt Created*/Modified* schon (`:21-43`).
- Dienst (E7): schreibt nur weniger `WorkOperation`; Einmal-Skript löscht ohne Audit (wie Präzedenz-Skript), Ergebnis ins Deploy-Protokoll.

## Rollen / Zugriff

- Teileliste und Melden/Rücknahme: `[RequireBdeActive]` + `[RequireBdeUserAccess]` (admin, bde_user, bde_shiftlead, bde_admin;
  [[controller]] Zeile 74) – wie das Terminal.
- Teilmeldungen-Liste: Klasse `BdeBookingsController` `[RequireBdeShiftleadAccess]`; Storno `[RequireBdeAdminAccess]`.
- Werkbank-Option: `[RequireMasterDataAccess]` (Edit, `ProductionWorkplacesController.cs:186`).
- Keine neue Rolle, kein neuer Filter → `controller.md` und `RoleOverview` nur um die neuen Aktionen in bestehenden Zeilen ergänzen.

## Listen-View-Pattern (ADR 0005)

Zwei neue Tabellen-Views, beide mit `PageSize.Resolve` + `PaginationState` + `_Pagination`, Filterkarte, Server-Spaltenfilter
(`data-server-column-filter="true"`, `data-col-key` an jedem `<th>`, `ColumnFilterHelper.ReadFromQuery`), Zahnrad über `ColumnDefinitions`,
Datumsfilter in C# nach der Kopfwert-Bildung, kein hartcodiertes Take:
- **Teileliste** (`BdeTeileliste`, `BdeTeileliste/Summiert`); Summiert-Seiteneinheit = Aggregatzeile (begründete Ausnahme wie Vormontage).
  Aufgeklappte Kinder sind Unterzeilen ohne eigene Filterung.
- **Teilmeldungen** (`BdeTeilmeldungen`) in BDE-Buchungen: Spalten Gemeldet am, Werker, Werkbank, Kürzel, Haupt-FA, Sub-FA, Position, Matchcode,
  Gut, Ausschuss, Status (offen/zurückgenommen), Grund; Filterkarte Zeitraum + „zurückgenommene zeigen“.

## Prüfabfragen für den Menschen (nur lesend)

Auf der **WMS-DB IDEAL** (Test, danach Prod):

- **P-TL1** – Welche WMS-Werkbänke tragen die Start-Kürzel? (Ergänzt P-AG5 der Arbeitsgang-Mapping-Spec; Ergebnis dort mitverwenden.)
  ```sql
  SELECT Id, Name, SageArbeitsplatznummer, ArbeitsschrittCode, BdeAktiv, BdeNurAgFertigmeldung, SageMissingSince
  FROM dbo.ProductionWorkplaces
  WHERE LTRIM(RTRIM(ArbeitsschrittCode)) IN (N'ST', N'KA', N'SL', N'SM', N'PU', N'ZU', N'IF')
  ORDER BY ArbeitsschrittCode, Id;
  ```
  Erwartung: je Kürzel mindestens eine Werkbank ohne `SageMissingSince`. `ArbeitsschrittCode` stammt aus Sage `USER_ArbeitsSchritt` bzw.
  der View-Spalte `AG` (`ProductionWorkplace.cs:38-42`); ob das dem `USER_PNKZ` aus Benjamins Liste entspricht, ist **ungeprüft** – fehlt
  ein Kürzel, gibt es dafür kein Teileliste-Terminal.
- **P-TL2** – Wie oft stehen die Kürzel an Blättern und an Baugruppen?
  ```sql
  SELECT LTRIM(RTRIM(s.value)) AS Kuerzel,
         SUM(CASE WHEN n.SubFA = 0 THEN 1 ELSE 0 END) AS Blatt,
         SUM(CASE WHEN n.SubFA <> 0 THEN 1 ELSE 0 END) AS Baugruppe
  FROM dbo.FaHierarchyNodes n CROSS APPLY STRING_SPLIT(n.Arbeitsschritte, ' ') s
  WHERE LTRIM(RTRIM(s.value)) IN (N'ST', N'KA', N'SL', N'SM', N'PU', N'ZU', N'IF')
  GROUP BY LTRIM(RTRIM(s.value)) ORDER BY 1;
  ```
- **P-TL3** – Bestand der Vater-AGs, die nur aus Blatt-Kürzeln stammen (Grundlage Rückfrage 1):
  > **Ergebnis 2026-11-17 (IDEAL TEST, [[2026-11-17-pruefabfragen-gerald]]):** SQL unten bricht mit Meldung 130 ab (EXISTS im Aggregat) – umgestellt ausgeführt: IF 4, KA 120, SM 6, ST 120 AGs, **0 gebucht/rückgemeldet**. P-TL5: BdeSchichtkalenderAktiv false, BdeShifts 0 Zeilen (PROD und TEST).
  ```sql
  SELECT n.SubFA, n.VaterFA, LTRIM(RTRIM(s.value)) AS Token INTO #Tok
  FROM dbo.FaHierarchyNodes n CROSS APPLY STRING_SPLIT(n.Arbeitsschritte, ' ') s
  WHERE n.Arbeitsschritte IS NOT NULL AND LTRIM(RTRIM(s.value)) <> '';
  SELECT wo.OperationNumber, COUNT(DISTINCT wo.Id) AS AGs,
         COUNT(DISTINCT CASE WHEN EXISTS (SELECT 1 FROM dbo.BdeBookings b WHERE b.WorkOperationId = wo.Id) OR wo.IsReported = 1 THEN wo.Id END) AS GebuchtOderRueckgemeldet
  FROM dbo.WorkOperations wo
  JOIN dbo.ProductionOrders po ON po.Id = wo.ProductionOrderId
  JOIN dbo.FaHierarchyNodes p ON p.SubFA <> 0 AND po.SubOrderNumber = CAST(p.SubFA AS NVARCHAR(100))
  WHERE wo.CreatedBy = N'WorkOperationStructureDetection'
    AND wo.OperationNumber IN (N'ST', N'KA', N'SL', N'SM', N'PU', N'ZU', N'IF')
    AND NOT EXISTS (SELECT 1 FROM #Tok t WHERE t.SubFA = p.SubFA AND t.Token = wo.OperationNumber)
    AND EXISTS (SELECT 1 FROM #Tok t WHERE t.VaterFA = p.SubFA AND t.SubFA = 0 AND t.Token = wo.OperationNumber)
  GROUP BY wo.OperationNumber ORDER BY 1;
  DROP TABLE #Tok;
  ```
- **P-TL4** – Ist der fachliche Schlüssel eindeutig?
  ```sql
  SELECT HauptFA, VaterFA, SubFA, Position, Artnr, COUNT(*) AS n
  FROM dbo.FaHierarchyNodes GROUP BY HauptFA, VaterFA, SubFA, Position, Artnr HAVING COUNT(*) > 1;
  ```
  Erwartung 0 Zeilen; sonst greift F16 für diese Zeilen (Zahl an Gerald melden).
- **P-TL5** – Liefert der Schichtkalender ein Schichtende (Rückfrage 3)?
  ```sql
  SELECT [Key], [Value] FROM dbo.AppSettings WHERE [Key] = N'BdeSchichtkalenderAktiv';
  SELECT ProductionWorkplaceId, DayOfWeek, StartTime, EndTime FROM dbo.BdeShifts ORDER BY ProductionWorkplaceId, DayOfWeek, StartTime;
  ```

Bis P-TL1..5 ausgeführt sind, gelten als **ungeprüft**: Kürzel = `USER_PNKZ`, Eindeutigkeit des Schlüssels, Größe des Bestands (V1),
Schichtende an IDEAL.

## Akzeptanzkriterien

1. Nach Migration bzw. `SQL/154` existieren `ProductionWorkplaces.BdeTeileliste` (bit, Default 0) und `BdeTeilMeldungen` mit gefiltertem
   UNIQUE; zweiter Lauf von `SQL/154` ändert nichts, kein Fehler; FreshInstall legt beides samt History an.
2. Stammdaten › Werkbänke zeigt den Schalter nur bei Master an. Speichern mit Option an und leerem Kürzel oder `BdeAktiv` aus → Fehlermeldung,
   nichts gespeichert. AKE (Master aus): Feld fehlt, ein manipulierter POST mit `BdeTeileliste=true` wird abgewiesen.
3. Terminal mit wirksamer Option leitet auf die Teileliste; ohne Option bleibt das Terminal unverändert (Regression v1.64.0). Mit
   `BdeNurFaMeldung` global an ist die Option wirkungslos.
4. Die Teileliste einer Werkbank mit Kürzel `KA` zeigt genau die Knoten, deren Token `KA` enthalten (Blatt und Baugruppe), keine anderen;
   Groß/Klein und Leerzeichen im Token spielen keine Rolle.
5. Eine Baugruppen-Zeile klappt auf und zeigt alle direkten Kinder; die Kinder sind nicht meldbar.
6. Standardspalten Matchcode, Menge, Hauptauftrag, Abteilung, Beschichtet (ab E6), FE-Termin, Material sind sichtbar; Abteilung „Kombigerät“
   bei mehreren Abteilungen wie Vormontage; Zahnrad blendet die übrigen ein.
7. KW von/bis filtert auf `FE_Termin` des Haupt-FA; Spaltenfilter wirken serverseitig vor der Pagination; Menge und FE-Termin filtern mit
   Zahl-/Datumssyntax.
8. Summiert zeigt je Matchcode Summe Sollmenge und Anzahl; Fertigmelden einer Summiert-Zeile legt je beitragender offener Position eine
   Teilmeldung mit voller Sollmenge an.
9. Einzelmeldung: Gut vorbelegt mit Soll, änderbar; Ausschuss optional; negative Menge oder Summe 0 → Meldung, nichts gespeichert.
10. Block über n angehakte Zeilen: Dialog nennt n und Summe; danach n Teilmeldungen mit Gut = Soll; Sammelmeldung nennt gemeldet/bereits
    gemeldet/abgewiesen.
11. Baugruppen-Zeile melden setzt zusätzlich den `WorkOperation` der Sub-FA mit diesem Kürzel fertig (eine `Production/Finished`-Buchung,
    `StartedAt == EndedAt`, `IsFinal`); Vater-Sub-FA und Haupt-FA bleiben unberührt. Fehlender/fertiger AG → Teilmeldung gespeichert + Hinweis.
12. Zweimal dieselbe Zeile melden (zweiter Tipp, zweites Terminal, alte Seite) → genau eine Teilmeldung, zweite Antwort „bereits gemeldet …
    – nichts geändert“, nie HTTP 500.
13. Nach einem FA-Struktur-Sync (neue Node-Ids) bleibt eine gemeldete Zeile gemeldet (Schlüssel statt Id). Ändert sich die Position in
    Sage, zeigt die Liste den Hinweis „n Meldungen ohne passende Strukturzeile“.
14. Werker A nimmt seine Meldung vor Schichtende zurück → Zeile wieder offen, bei Baugruppe auch die AG-Buchung storniert. Werker B kann
    die Meldung von A nicht zurücknehmen; nach Schichtende auch A nicht (Meldung „nur noch über den BDE-Admin“).
15. BDE-Admin storniert in BDE-Buchungen › Teilmeldungen mit Grund; Schichtleiter sieht die Liste, kann aber nicht stornieren.
16. Filter (Sicht, Woche, Spaltenfilter, gemeldete zeigen, Seitengröße) bleiben je Werker und Werkbank erhalten: anderer Werker oder andere
    Werkbank → eigene Werte. Woche wird relativ gespeichert: „aktuelle KW“ zeigt in der Folgewoche die neue aktuelle KW. „Filter
    zurücksetzen“ stellt die Vorgabe her.
17. (E7, V1) Nach einem Erkennungslauf entsteht für ein Teileliste-Kürzel kein neuer Vater-AG aus Blatt-Kindern; die eigene Zeile des Sub-FA
    erzeugt ihren AG weiter; Werkbänke ohne Option unverändert; Zähler `teileliste_blatt_uebersprungen` im Protokoll.
18. Einmal-Skript im Trockenlauf gibt genau eine Zeile `TREFFER: n` aus und ändert nichts; scharf löscht es nur ungebuchte Kandidaten;
    zweiter Trockenlauf `TREFFER: 0`.
19. Kein Code-Pfad schreibt nach Sage; keine Konstante mit Kürzeln (`"KA"`, `"ST"` …) im Produktivcode (Wächter-Test per Quelltext-Suche).
20. Build und alle Tests grün.

## Test-Szenarien

Neues Kapitel **TS-152 „BDE-Teileliste (vereinfacht) Blechfertigung“** in `docs/TESTSZENARIEN.md` (Nummer beim Start prüfen), danach
`secondbrain/tests/testszenarien-index.md`.

- **Vorbedingungen:** IDEAL-Test, Master an, BDE aktiv, P-TL1..5 ausgeführt; Werkbank „Kanten“ mit Kürzel `KA`, `BdeAktiv`, Option an; ein
  Terminal-Benutzer; zwei BDE-Mitarbeiter A und B; ein Haupt-FA mit Korpus (Sub-FA, eigenes Token `KA`) und zwei Blechteilen `KA` (Blatt);
  Schichtkalender mit Schicht bis 14:00 (für 152.8).
- **152.1 Option pflegen:** an/aus, Validierung (leeres Kürzel, BDE aus), AKE-Regression ohne Feld.
- **152.2 Terminal-Weiche:** Werkbank mit Option → Teileliste; Werkbank ohne → Haupt-FA-Gruppen wie v1.64.0; `BdeNurFaMeldung` an → keine Teileliste.
- **152.3 Zeilen und Spalten:** nur `KA`-Knoten; Korpus aufklappbar; Standardspalten; Zahnrad.
- **152.4 Woche und Filter:** KW-Auswahl, Spaltenfilter Matchcode/Menge/FE-Termin, Pagination.
- **152.5 Einzeln melden:** Gut ändern, Ausschuss; Zeile verschwindet; „gemeldete zeigen“ zeigt Werker und Zeit.
- **152.6 Block und Summiert:** drei Zeilen anhaken → Dialog mit 3 und Summe; Summiert-Zeile melden → alle beitragenden Positionen.
- **152.7 Baugruppe:** Korpus melden → AG `KA` am Korpus fertig (FA-Struktur-Symbol erledigt), Haupt-FA unverändert.
- **152.8 Rücknahme:** A nimmt eigene Meldung zurück; B versucht A's Meldung → abgewiesen; nach Schichtende A → abgewiesen; Admin storniert in
  BDE-Buchungen › Teilmeldungen; Schichtleiter sieht ohne Storno-Knopf.
- **152.9 Filter speichern:** A setzt Filter, Werker wechseln, B sieht eigene; A erneut → Filter da; Folgetag/Folgewoche relativ; zurücksetzen.
- **152.10 Struktur-Sync:** Sync laufen lassen → gemeldete Zeilen bleiben gemeldet.
- **152.11 Erkennung + Einmal-Skript (E7):** Trockenlauf TREFFER, scharf, zweiter Trockenlauf 0; neuer Erkennungslauf legt keinen Vater-AG an.
- **Negativfälle:** N1 Doppeltipp und zwei Terminals gleichzeitig auf dieselbe Zeile → eine Meldung (Manual-UAT, nicht InMemory-prüfbar,
  [[fallstricke]] §8 und §38); N2 veraltete Seite nach Meldung durch anderen Werker → „bereits gemeldet“; N3 negative Menge / 0; N4 verpackter Auftrag →
  abgewiesen; N5 Zeile nach Sage-Änderung weg → Hinweis Waisen; N6 Benutzer ohne BDE-Rolle → kein Zugriff; N7 manipulierter POST mit fremdem
  Kürzel/Werkbank → abgewiesen; N8 Werker nicht angemeldet → keine Liste.
- **Unit-Tests:** Zeilenauswahl (Token, Blatt/Baugruppe), Schlüssel-Gleichheit über neue Node-Ids, Summiert mit beitragenden Schlüsseln,
  Melden (Mengen, bereits gemeldet, AG-Kern, verpackt), Frist (Schichtende/Fallback), Filter-JSON mit relativen Wochen, Validierung der
  Option, Erkennung V1 (Blatt-Token übersprungen nur für Teileliste-Kürzel), Wächter Kürzel-Konstanten.

## Etappen (nur bei epic: true)

Ein Worktree (`feature/2026-08-07-ideal-teile-1-5`), ein Merge am Ende; je Etappe ein Commit, Build + Tests grün. Branch während der Arbeit mit
`scripts/sync-worktree.ps1` aktuell halten. Reihenfolge **nach** 1.99.27 (Arbeitsgang-Mapping) wegen der Nummern; eine Code-Abhängigkeit zur
Arbeitsgang-Mapping-Spec gibt es nicht (die Teileliste braucht keine Sage-Arbeitsgangnummer), nur P-AG5 und P-TL1 überschneiden sich.
Eine Etappe mit Haltepunkt, dessen Bedingung fehlt, wird als Blocker vermerkt; die übrigen laufen weiter (E8 und E7 hängen am Ende).

| # | Etappe | Inhalt | Abhängig von | Status | Commit |
|---|--------|--------|--------------|--------|--------|
| 1 | Schema + Option | Model, Migration, SQL/154, FreshInstall, Maske + Validierung, `TeilelisteWirksam`; keine Verhaltensänderung | – | offen | |
| 2 | Teileliste lesen | `BdeTeilelisteService`, Controller, Views Einzeln/Summiert, Aufklappen, Terminal-Weiche, Werker-Anmeldung, Listen-View-Pattern (ohne Beschichtet) | E1; Rückfrage 8 (Default Empfehlung bauen, falls offen: Blocker-Vermerk) | offen | |
| 3 | Melden | einzeln, Block, Summiert, Doppel-Schutz, AG-Kern aus `ReportCompletionAsync`, Sammelmeldung, Waisen-Hinweis | E2; Rückfrage 5 | offen | |
| 4 | Rücknahme | Terminal bis Schichtende, Admin-Liste Teilmeldungen + Storno | E3; Rückfrage 3 | offen | |
| 5 | Filter speichern | UserViewPreference-Ablage, relative Woche, Zurücksetzen | E2; Rückfragen 4, 7 | offen | |
| H1 | Haltepunkt | `BeschichtungResolver` im Branch committet (Beschichtungslogik E1) und Rückfrage 2 beantwortet | – | offen | |
| 6 | Beschichtet | Spalte über den Resolver | H1 | offen | |
| H2 | Haltepunkt | Rückfrage 1 beantwortet, P-TL3 erhoben | – | offen | |
| 7 | Struktur-Erkennung | V1: Dienst + Einmal-Skript (V2/V3 nach Antwort) | H2 | offen | |
| 8 | Gleicher Schritt (später) | Kennzeichnung Baugruppe/Teil | Freigabe-Antwort 6; darf entfallen | offen | |
| 9 | Abschluss | TS-152, Hilfe, Changelog, README, Manifest, ADR 0030 („Teilmeldung mit fachlichem Schlüssel neben WorkOperation“), fallstricke (Node-Id instabil, Schlüssel), codebase-Karte, feature-map, Version 1.99.28 beide `AppVersion.cs` | alle | offen | |

## Deploy

*Von der QA 2026-11-17 gegen den echten Diff `b4972700..c7c968fe` (+ uncommitteter F1-Fix) bestätigt.*

- **Web-App:** ja (`IdealAkeWms/`: Option, Teileliste, Melden, Rücknahme, Admin-Liste Teilmeldungen, Beschichtet je Position, Hilfe/Changelog).
- **Service:** ja (`IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs`, V1 Blatt-Token übersprungen; `AppVersion.cs`).
- **Migration:** ja, additiv – `IdealAkeWms/Migrations/20261117075856_AddBdeTeileliste.cs` = `SQL/154_AddBdeTeileliste.sql` (Spalte
  `ProductionWorkplaces.BdeTeileliste` Default 0 + Tabelle `BdeTeilMeldungen` mit gefiltertem UNIQUE `UX_BdeTeilMeldungen_Schluessel`).
- **Einmal-Skript (datenlöschend, nur IDEAL):** `SQL/Einmalig/2026-11-17_v1.99.32_RemoveParentWorkOperationsFromLeafTokensTeileliste.sql`,
  Default `@Trockenlauf = 1`. Wirkt erst, wenn die Option an einer Werkbank gesetzt ist (ohne Option: 0 Kandidaten).
- **Manifest** `scripts/deploy/nachlauf.psd1`: `SQL/154`, `Einmalig/2026-11-17_v1.99.32_…` (Trockenlauf, Braucht SQL/154),
  `v1.99.32-TeilelisteOption` (Einstellung), `v1.99.32-TeilelisteInfo`, `v1.99.32-BeschichtungListen` (Info) – alle vorhanden.
- **Reihenfolge Testsystem/PROD IDEAL:** 1. DB-Backup → 2. `SQL/154` (bzw. Migration beim ersten Request) → 3. Web + Service →
  4. Option an den Start-Werkbänken setzen (nach Abnahme TS-152) → 5. Einmal-Skript **Trockenlauf**, Zählung prüfen und freigeben →
  6. DB-Backup → 7. Einmal-Skript mit `@Trockenlauf = 0` → 8. zweiter Trockenlauf muss `TREFFER: 0` zeigen.
  AKE: nur 1–3 (Option ist bei Master aus nicht verfügbar, Einmal-Skript nicht ausführen).
- **Publish:** im Auftrag dieses QA-Laufs ausdrücklich **nicht** erstellt („publish comes at the end of the run“) – folgt am Ende des
  Bündellaufs aus dem Worktree:
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```
- Nach dem Merge nur dann neu aus `main` publishen, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen zusammenführt.

## Folgearbeit (nach Merge als Aufgabe festhalten)

- Teilmeldungen an Sage zurückmelden ([[2026-10-30-fa-rueckmeldung-an-sage]]), sobald die Arbeitsplatz-Zuordnung geklärt ist.
- Ggf. Kommissionier-/Beschichtungsliste auf die Positions-Methode des Resolvers umstellen (Rückfrage 2).
- BF-Aufträge / WiCAM ([[2026-11-13-blechfertigung-bf-auftraege-wicam-csv]]).

## Offene Rueckfragen

1. **Konflikt Struktur-Erkennung.** `WorkOperationStructureDetectionService.cs:105-106` legt Kürzel von Blatt-Kindern am Vater-Sub-FA an; ohne
   Rückmeldung nach oben blieben diese AGs für Teileliste-Kürzel dauerhaft offen (FA-Baum-Status, Terminal-Angebot anderer Familien-Werkbänke;
   Cockpit nicht betroffen). V1, V2 oder V3 (Tabelle oben)? Bestand per P-TL3.
   *Empfehlung:* V1 mit Einmal-Skript (Trockenlauf, gebuchte nur melden).
2. **Beschichtet je Position.** Der Resolver liefert das Kennzeichen je FA; Kommissionier-/Beschichtungsliste zeigen es je Position. Für ein
   Blechteil wäre das Vater-Kennzeichen falsch (§20).
   *Empfehlung:* Positions-Methode `BeschichtetPosition` im Resolver (liefert `FaHierarchyNode.Beschichtet` bei Quelle `FaStruktur`), die Teileliste
   liest nur sie. Alternative: FA-Kennzeichen der besitzenden Sub-FA.
3. **Rücknahme-Frist ohne Schichtende.** `GetShiftEndForBookingAsync` liefert `null` bei Kalender aus, Feiertag, keiner Schicht.
   *Empfehlung:* dann gilt das Ende des Meldetags (23:59:59). P-TL5 zeigt, ob IDEAL einen Kalender hat.
4. **Filter je Anmeldung.** Der Werker ist ein `BdeOperator`, die Ablage `UserViewPreference` hängt am Web-Benutzer.
   *Empfehlung:* Terminal-Web-Benutzer + ViewKey `BdeTL:W{Werkbank}:O{Werker}`, keine Migration. Grenze: folgt dem Werker nicht auf einen anderen
   Terminal-PC. Alternative: eigene Ablage je Werker (Migration).
5. **Block-Umfang.** *Empfehlung:* angehakte Zeilen der aktuellen Seite mit „alle auf dieser Seite“ (Seitengröße wählbar): der Werker sieht, was
   er meldet. Alternative: alle gefilterten Zeilen aller Seiten (größeres Risiko, Ungesehenes zu melden).
6. **Gleicher Schritt an Baugruppe und Teil.** Nach der Grundregel erscheinen beide Zeilen von Anfang an (kein Zusatzcode).
   *Empfehlung:* so lassen; E8 bringt später nur die Kennzeichnung. Alternative: Teil bis E8 ausblenden (Zusatzcode, der später wieder fällt).
7. **Standard-Wochenauswahl.** *Empfehlung:* feste Vorgabe aktuelle KW bis +1 ohne AppSetting (jeder Werker speichert ohnehin seine eigene).
   Alternative: AppSetting nach ADR 0020 (Auswahlliste, Standort IDEAL).
8. **Sichtbarkeit erledigter Aufträge.** *Empfehlung:* Zeilen ausblenden, wenn der besitzende Auftrag (Baugruppe: eigene Sub-FA; Blechteil:
   Vater-Sub-FA) im WMS erledigt oder storniert ist; Zeilen ohne WMS-Auftrag zeigen mit Kennzeichnung „kein WMS-Auftrag“ (meldbar).

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Konflikt Struktur-Erkennung: V1, V2 oder V3? P-TL3 erhoben? → **Gerald 2026-11-16 17:28 „Rest passt“ (Empfehlung Koordination): V1 mit Einmal-Skript (Trockenlauf, gebuchte nur melden); P-TL3 vor dem Dev-Lauf erheben (Gerald).**
2. Beschichtet je Position: Positions-Methode im Resolver oder FA-Kennzeichen? → **Gerald 2026-11-16 17:28 „Rest passt“ (Empfehlung Koordination): Positions-Methode BeschichtetPosition im Resolver.**
3. Rücknahme-Frist ohne Schichtende: Ende des Meldetags? → **Gerald 2026-11-16 17:28 „Rest passt“ (Empfehlung Koordination): ja, Ende des Meldetags; P-TL5 zeigt den Kalender.**
4. Filter je Anmeldung: UserViewPreference mit Werker im Schlüssel oder eigene Ablage? → **Gerald 2026-11-16 17:28 „Rest passt“ (Empfehlung Koordination): UserViewPreference, ViewKey mit Werkbank und Werker, keine Migration.**
5. Block-Umfang: aktuelle Seite oder alle gefilterten? → **Gerald 2026-11-16 17:28 „Rest passt“ (Empfehlung Koordination): angehakte Zeilen der aktuellen Seite.**
6. Gleicher Schritt: beide Zeilen sofort, E8 nur Kennzeichnung? → **Gerald 2026-11-16 17:28 „Rest passt“ (Empfehlung Koordination): ja.**
7. Standard-Wochenauswahl: feste Vorgabe oder AppSetting? → **Gerald 2026-11-16 17:28 „Rest passt“ (Empfehlung Koordination): feste Vorgabe aktuelle KW bis +1.**
8. Sichtbarkeit erledigter/stornierter Aufträge und Zeilen ohne WMS-Auftrag? → **Gerald 2026-11-16 17:28 „Rest passt“ (Empfehlung Koordination): erledigte/stornierte ausblenden, „kein WMS-Auftrag“ kennzeichnen und meldbar.**

## QA-Nachweis (qa-agent, 2026-11-17)

Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, Commit-Bereich `b4972700..c7c968fe`
(E1–E7, E9; **E8 entfallen** laut Freigabe-Antwort 6 „darf entfallen“). Version 1.99.32, `SQL/154`, TS-152, ADR 0030.

### 1. Build und Tests

- `dotnet build IdealAkeWms.slnx`: **0 Fehler**, 28 Warnungen (vorbestehende Nullable-Warnungen).
- `dotnet test`:
  - `IdealAkeWms.Tests`: **3104 bestanden, 1 übersprungen, 0 Fehler** (3105 gesamt).
  - `IDEALAKEWMSService.Tests`: **685 bestanden, 0 übersprungen, 0 Fehler**.

### 2. Code-Review (selbst, synchron in diesem Lauf)

Geprüft: alle Dateien des Diffs – `BdeTeilelisteService`, `BdeTeilelisteController`, `BdeBookingsController` (Teilmeldungen + `StornoTeilmeldung`
mit `[RequireBdeAdminAccess]`), `BdeBookingService` (Kern-Split `ReportCompletionCoreAsync`, `AnwendungssperreAsync`), `BdeTerminalController`
(Weiche), `ProductionWorkplacesController` + `_BdeTeilelisteField`, `WerkbankKuerzelFamilie.TeilelisteWirksam`,
`BeschichtungResolver.BeschichtetPosition`, Views `BdeTeileliste/*` und `BdeBookings/Teilmeldungen`, `bde-teileliste.js`, `bde.css`,
`ColumnDefinitions`/`ColumnFilterTypes`, Migration, `SQL/154`, `00_FreshInstall.sql`, Einmal-Skript, `WorkOperationStructureDetectionService`,
`nachlauf.psd1`, alle neuen und geänderten Tests.

| # | Befund | Schwere | Fundstelle | Behandlung |
|---|---|---|---|---|
| F1 | Doppelt kodiertes UTF-8 (Mojibake) in den drei Validierungsmeldungen der Option – sichtbarer UI-Text | mittel | `IdealAkeWms/Controllers/ProductionWorkplacesController.cs:263,265,267` | **Behoben (trivial), NICHT committet** – Änderung liegt im Worktree und muss mit dem nächsten Commit mit. Mit dem Fix auf DEV deployt und im Browser geprüft. |
| F2 | Weißer Text auf Marken-`.btn-success` (#82BF6E, ca. 2,2:1) und `.btn-secondary` (#43A6E2, ca. 2,7:1) unter WCAG AA; die neuen Terminal-Knöpfe „Fertig melden“/„Filtern“ erben das | niedrig–mittel (vorbestehend, global) | `IdealAkeWms/wwwroot/css/site.css:6,8,151-170` | Nicht behoben (globales Theme, außerhalb des Umfangs) → Folgeaufgabe Kontrast (Checkliste Nr. 11). |
| F3 | AG-Suche der Baugruppen-Meldung nimmt `FirstOrDefault` ohne Sortierung, falls mehrere Aufträge dieselbe `SubOrderNumber` tragen | niedrig | `IdealAkeWms/Services/BdeTeilelisteService.cs:367-368` | Hingenommen: `SubOrderNumber` ist je Sub-FA eindeutig; nur Hinweis. |

Keine Befunde in den Pflicht-Fehlerklassen:
- `<script>`-Blöcke nur `type="application/json"` mit statischem JSON, kein `@Html.Raw` in JS-Strings.
- Kein `hidden` neben `d-*` im Diff.
- Keine stillen Fallbacks: leere Menge = Sollmenge nur bei Block/Summiert (so spezifiziert, AK 10); Text statt Zahl → „Menge ist keine Zahl – nichts gemeldet“.
- Kein positionsweises Binden paralleler Arrays: Zeilen reisen als `TeilSchluessel`-Text.
- Validierung serverseitig: Option (Master/BdeAktiv/Kürzel), Mengen, Werker, Werkbank, Kürzel im Token, verpackt, Frist.
- Eine Regel an einem Ort: `TeilelisteWirksam` für Terminal, Service und Controller; das Einmal-Skript spiegelt die Dienst-Regel (Kürzel aus Werkbänken mit `BdeAktiv` + Option, ohne `SageMissingSince`, nur `CreatedBy='WorkOperationStructureDetection'`), gegen `WorkOperationStructureDetectionService` abgeglichen.

Bewertung der Abweichungen des Umsetzers:
- **`BdeTeilelisteService` mit `ApplicationDbContext` direkt (Abweichung ADR 0001):** vertretbar – Transaktion + `sp_getapplock` + UNIQUE brauchen einen Kontext; im Code begründet.
- **Melden als Formular-POST mit PRG:** passend zum Terminal (Antiforgery, kein Doppel-POST beim Zurück). OK.
- **Applock-Ressource = SHA256 des Schlüssels:** hält die 255-Zeichen-Grenze (Test `Sperre_Hoechstens…`); eigentlicher Schutz ist der gefilterte UNIQUE. OK.
- **Summiert mit eigenem ViewKey `BdeTeilelisteSummiert`:** andere Spalten als Einzeln, daher richtig. OK.
- **`operatorId` im Query:** gleiches Vertrauensmodell wie das BDE-Terminal; je Aufruf gegen aktive BDE-Mitarbeiter geprüft, Rücknahme prüft Eigentum. Hingenommen.
- **Einmal-Skript statt P-TL3-SQL:** `CASE WHEN EXISTS` in einem Nicht-Aggregat-`SELECT INTO #Cand`, Summen danach über `#Cand` → kein Meldung 130. `@Trockenlauf BIT = 1` Default, Abbruch ohne `SQL/154`, `XACT_ABORT ON`, löscht nur nicht rückgemeldete Kandidaten ohne BDE-Buchung, nur Zeilen der Erkennung (manuelle WorkOperations unberührt).

Checkliste CLAUDE.md:
- Migration + idempotentes `SQL/154` mit Guards, DDL in eigenen Batches, History-Insert separat und geschützt: **ja**.
- FreshInstall an beiden Stellen (Schema + `MigrationId`): **ja**.
- Audit-Felder: `BdeTeilMeldung` erbt `AuditableEntity`; Created*/Modified* auf DEV belegt (Meldungen 1, 2, 5 und Buchung 24 mit `ModifiedBy=Gerald Weichbold`, `ModifiedByWindows=AKENB\gerald.weichbold`).
- Version 1.99.32 in beiden `AppVersion.cs`, Anwender- und Brain-Changelog: **ja** (E9).
- `docs/TESTSZENARIEN.md` TS-152: **ja**; Index-Zeile 152 in [[testszenarien-index]] von der QA ergänzt.
- ADR 0020: keine neue AppSetting/ServiceSetting. Die Werkbank-Option hat Anzeigename, Wirkung, Default „aus“, Warnhinweis zur Außenwirkung (Erkennung) und Serverprüfung.
- Read/Edit-Split: Teilmeldungen lesbar mit BDE-Lesezugriff, Storno nur `[RequireBdeAdminAccess]`.
- Listen-View-Pattern: Pagination, Filterkarte, Server-Spaltenfilter (`data-server-column-filter`, alle `<th>` mit `data-col-key`).
- Keine Kürzel-Konstanten: Wächter `TeilelisteKuerzelKonstantenWaechterTests`. Kein Sage-Schreibweg.

### 3. Nachweisart je Akzeptanzkriterium

| AK | Nachweis |
|---|---|
| 1 | **DEV (echtes SQL):** Migration in beiden DEV-DBs, gefilterter Index `has_filter=1`; `SQL/154` zweiter Lauf auf IDEAL_WMS_DEV und AKE_WMS_DEV: „existiert bereits“, Exit 0, History genau 1. FreshInstall per Review. |
| 2 | Unit (`BdeTeilelisteOptionTests`) + DEV: IDEAL-Schalter sichtbar und gespeichert, Validierungstexte (F1); AKE-Maske ohne Feld. Manipulierter AKE-POST nur per Unit (`Create_MasterAus_OptionAbgewiesen`). |
| 3 | Unit (`Terminal_LeitetNurBeiWirksamerTeilelisteUm`) + DEV: WP 67 leitet um, WP 65 und AKE-Terminal unverändert. `BdeNurFaMeldung` nur Unit. |
| 4, 5, 6 | Unit (`BdeTeilelisteServiceTests`) + DEV-Sichtprüfung (Korpus aufklappbar, Standardspalten). |
| 7 | Unit (KW-Filter) + DEV: KW W29–W30 auf `FE_Termin`; Server-Spaltenfilter gegen echtes SQL: **Manual-UAT**. |
| 8, 9, 10 | Unit + DEV: Summiert → „Gemeldet: 3“ (Gut = Soll); N3 negativ/0/Text abgewiesen. Block-Bestätigungsdialog (`confirm()`): **Manual-UAT**. |
| 11 | Unit + DEV: Korpus gemeldet → Buchung 24 an WO 523, Vater/Haupt unberührt. |
| 12 | Unit + **DEV N1**: 6 parallele POSTs auf dieselbe Zeile → genau 1× „Gemeldet: 1“, 5× „bereits gemeldet … nichts geändert“, alle HTTP 200. Zwei echte Terminals: Manual-UAT. |
| 13 | Unit + DEV: neue Node-Id (1963363 → 1963780) → Zeile bleibt „gemeldet von Hans Guck“; Position 2 → 92 → Hinweis „1 Meldung(en) ohne passende Strukturzeile“. Echter Sync: **Manual-UAT**. |
| 14, 15 | Unit + DEV (siehe 152.8). Frist mit echtem Schichtkalender und Schichtleiter-Sicht: **Manual-UAT** (IDEAL ohne Kalender → Ende Meldetag, Rückfrage 3). |
| 16 | Unit (relative Woche, JSON) + DEV: je Werker/Werkbank eigene `UserViewPreferences` (`BdeTL:W67:O1`, `BdeTL:W67:O2`). Folgewoche: Manual-UAT. |
| 17 | Unit (`Detect_TeilelisteWerkbank_*`, `Detect_OhneWirksameOption_*`); echter Dienstlauf auf DEV gesperrt (`Sync:WorkOperationStructureDetectionEnabled` aus): **Manual-UAT**. |
| 18 | **DEV (echtes SQL), nur Trockenlauf:** `TREFFER: 119`, WorkOperations 632 vor/nach. Zusatz in zurückgerollter Transaktion: ein rückgemeldeter Kandidat (WO 525) steht in der Meldeliste und zählt nicht (`TREFFER: 118`). Scharfer Lauf + zweiter Trockenlauf: **Manual-UAT** (auch zurückgerollt in dieser Umgebung nicht zugelassen). |
| 19 | Wächter-Test + Review. |
| 20 | Grün (siehe 1). |

### 4. Geänderte bestehende Tests

| Test | Änderung | Einordnung |
|---|---|---|
| `ProductionWorkplacesControllerTests.CreateController` | `internal`, Parameter `master`, neuer Ctor-Parameter `IServiceSettingRepository` | nur Typ/Signatur (Compile-Fix + Wiederverwendung) |
| `NachlaufManifestDriftTests` | `"SQL/154"` in die Liste der Manifest-Pflicht-SQLs | Erweiterung um die neue Datei (AK 1) |
| `BeschichtungResolverTests` | nur neue Theorie `BeschichtetPosition_NurBeiQuelleFaStruktur` angehängt | neu, nichts Bestehendes geändert |

Kein Befund „sonstig“.

### 6. DEV-Deploy und Testlauf

- **Deploy** über `D:\Dev\scripts\dev-deploy.ps1`, Quelle = Worktree (Commit `c7c968fe` + F1-Fix), beide Standorte, Web **und** Service
  (Binärdateien 17.11.2026 09:37). Footer: **AKE http://localhost:8081 = v1.99.32**, **IDEAL http://localhost:8082 = v1.99.32**;
  `AKE_WMS_DEV_Service` und `IDEAL_WMS_DEV_Service` laufen; Migration `20261117075856_AddBdeTeileliste` in beiden DEV-DBs.
- `standort: IDEAL` laut Frontmatter; AKE nur als Regression.
- Testdaten DEV IDEAL: Werkbank 67 „Kanten“ (KA) über die Maske auf BdeAktiv + Option; Korpus-Knoten 1963692 `Arbeitsschritte` `PU` → `PU  ka `;
  zweiter BDE-Mitarbeiter `QA152B`; Werker A = Hans Guck (Id 1).

| Schritt | Durchgeführt | Ergebnis | Status |
|---|---|---|---|
| 152.1 Option pflegen | Maske IDEAL an/aus, Validierung; AKE-Maske | Schalter nur IDEAL; Fehlermeldungen (nach F1-Fix lesbar); AKE ohne Feld | OK (F1) |
| 152.2 Terminal-Weiche | WP 67 / WP 65 / AKE-Terminal | WP 67 → Teileliste; WP 65 und AKE unverändert; Teileliste-Direktaufruf WP 65 → zurück aufs Terminal | OK |
| 152.2 `BdeNurFaMeldung` an | – | nur Unit | NICHT MÖGLICH (globale Einstellung auf DEV bewusst nicht umgeschaltet) |
| 152.3 Zeilen und Spalten | Liste KA, Korpus aufklappen | nur KA-Knoten, Korpus mit Kindern (nicht meldbar), Standardspalten | OK |
| 152.4 Woche und Filter | KW W29–W30, Spaltenfilter, Pagination | wirken | OK |
| 152.5 Einzeln melden | Gut/Ausschuss, „gemeldete zeigen“ | Zeile weg, „gemeldet von Hans Guck, 17.11. 09:42“ | OK |
| 152.6 Summiert | Summiert-Zeile A-BLATT-A-385-230 (POST) | „Gemeldet: 3“, drei Meldungen Gut = Soll | OK |
| 152.6 Block-Dialog | `confirm()` | – | NICHT MÖGLICH (Browser-Dialog verboten) |
| 152.7 Baugruppe | Korpus melden | Buchung 24 an WO 523, Haupt-FA unberührt | OK |
| 152.8 Rücknahme | A eigene / B fremde / A nach Frist / Baugruppe / Admin | „zurückgenommen“; B: „Nur eigene Meldungen …“; Frist: „Frist abgelaufen (16.11. 23:59) – Rücknahme nur noch über den BDE-Admin“; Baugruppe: „Arbeitsgang-Buchung storniert“ (Buchung 24 `IsCancelled=1`); Admin ohne Grund abgewiesen, mit Grund storniert; B meldet zurückgenommene Zeile neu | OK (per HTTP-POST statt Dialog) |
| 152.8 Schichtende / Schichtleiter | kein Kalender auf IDEAL, keine Schichtleiter-Kennung | – | NICHT MÖGLICH |
| 152.9 Filter speichern | zwei Werker an WP 67 | je Werker eigene Ablage | OK (Folgewoche offen) |
| 152.10 Struktur-Sync | per SQL simuliert (Knoten neu, neue Id) | bleibt gemeldet | OK (simuliert); echter Sync NICHT MÖGLICH (`Sync:HierarchicalFaEnabled` aus) |
| 152.11 Einmal-Skript | Trockenlauf | `TREFFER: 119`, nichts geändert | OK (Trockenlauf); scharf + Erkennungslauf NICHT MÖGLICH |
| N1 parallel | 6 gleichzeitige POSTs | genau 1 Meldung | OK |
| N2 veraltete Seite | zweite Meldung derselben Zeile | „bereits gemeldet von Hans Guck um 09:41 – nichts geändert“ | OK |
| N3 Menge | negativ / 0 / Text | abgewiesen | OK |
| N4 verpackt | – | nur Unit | NICHT MÖGLICH (kein passender DEV-Auftrag) |
| N5 Waisen | Position 2 → 92 | Hinweis „1 Meldung(en) ohne passende Strukturzeile“ | OK |
| N6 ohne BDE-Rolle | – | Rollenfilter per Review | NICHT MÖGLICH (kein zweiter Web-Benutzer) |
| N7 manipulierter POST | fremde Werkbank, kaputter Schlüssel, Schlüssel ohne KA, unbekannter Werker | je Fall abgewiesen | OK |
| N8 nicht angemeldet | ohne Werker | Anmeldemaske, keine Liste | OK |
| AKE-Regression | Terminal, Werkbank-Maske | unverändert, kein Teileliste-Feld | OK |
| Beschichtung TS-141.2 / TS-142.3 | Tooltip „Beschichtung“ FA-Liste + Leitstand | AKE „Berechnet: Kommissioniertermin - 10 Arbeitstage …“; IDEAL „Aus der Sage-View, Feld Start_Beschichtung …“ | OK |

**DEV wiederhergestellt** (Abfrage nach dem Aufräumen: `TM=0 | B24=0 | N1963692=PU | Pos92=0 | Prefs=0 | Op=0 | WP67=0/0`): alle
`BdeTeilMeldungen`, Buchung 24 samt Mengen, `UserViewPreferences BdeTL:%` und Mitarbeiter `QA152B` gelöscht; Korpus wieder `PU`, Position wieder 2,
Werkbank 67 wieder `BdeAktiv=0`, `BdeTeileliste=0`; WO 523 unverändert (`IsReported=0`). Bleibt: der simuliert neu eingefügte Knoten
(inhaltsgleich, Id 1963780 statt 1963363), den der nächste Struktur-Sync ersetzt. Keine Einstellung umgeschaltet, nichts an Sage, keine Mail.

### 7. Publish

Nicht in diesem Lauf (Auftrag: Publish am Ende des Bündellaufs). Vor dem Testsystem-Lauf aus dem Worktree erstellen, nach Commit des F1-Fixes.

### Manuelle Testcheckliste (Mensch)

1. F1-Fix (`IdealAkeWms/Controllers/ProductionWorkplacesController.cs`) committen, dann Publish aus dem Worktree samt `publish-info.txt`.
2. Testsystem IDEAL: Deploy in der Reihenfolge des Deploy-Abschnitts, TS-152 komplett an einer echten Werkbank (Kürzel laut P-TL1).
3. Block-Meldung mit Bestätigungsdialog (152.6) und Rücknahme-Dialog am Terminal-Gerät.
4. 152.8 mit Schichtkalender (falls eingeführt) und Schichtleiter-Sicht ohne Storno-Knopf; N6 Benutzer ohne BDE-Rolle; N4 verpackter Auftrag.
5. 152.2 `BdeNurFaMeldung` an → keine Teileliste, danach zurück.
6. 152.10 echter FA-Struktur-Sync: gemeldete Zeilen bleiben gemeldet.
7. 152.11 Einmal-Skript: Trockenlauf → Zählung freigeben → Backup → scharf → zweiter Trockenlauf `TREFFER: 0`; danach Erkennungslauf ohne neuen Vater-AG, Zähler `teileliste_blatt_uebersprungen` im Aktivitäts-Protokoll.
8. N1 mit zwei echten Terminals gleichzeitig; P-TL4.
9. 152.9 in der Folgewoche: „aktuelle KW“ rückt mit.
10. AKE-Sichtprüfung (Terminal, Werkbank-Maske) auf dem Testsystem.
11. Folgeaufgabe Kontrast `.btn-success`/`.btn-secondary` (F2) anlegen.
## Nachtrag 2026-11-17: Kontrast der Terminal-Knöpfe (Freigabe Gerald 18:48)

QA-Befund F2: weißer Text auf `.btn-success` (#82BF6E, ca. 2,2:1) und `.btn-secondary` (#43A6E2, ca. 2,7:1) ist am Terminal schlecht lesbar.
Auftrag: **nur in der BDE-Teileliste und am BDE-Terminal** (`bde.css`, Views `BdeTeileliste/*`, BDE-Terminal) dunkle Schrift auf diesen Knöpfen,
sodass WCAG AA (4,5:1) erreicht wird; die globalen Markenfarben in `site.css` bleiben unverändert. Sichtprüfung auf DEV IDEAL mit Screenshot.
Weitere Wünsche an die Maske sammelt die Koordination beim Termin 18.11. ([[2026-11-18-termin-0950-walter-fragen]]).

### Umsetzung Nachtrag Kontrast (Goal-Lauf 2026-11-17 abends, v1.99.36, Commit `e40ecfcb`)

`IdealAkeWms/wwwroot/css/bde.css`: `.bde-terminal .btn-success`/`.btn-secondary` (inkl. hover/focus/active/disabled) Schrift `#212529` —
grün 7,1:1, blau 5,8:1, Hover 5,6:1/4,7:1. Die Modals (`meldenModal`, `group-finish-modal`, `close-others-modal`) liegen außerhalb des
Wrappers und tragen jetzt die Klasse `bde-terminal`. `site.css` unverändert. TS-152.12; Sichtprüfung/Screenshot DEV IDEAL durch den qa-agent.
Lauf-Notiz [[2026-11-17-goal-lauf-sammel-abend]].

### QA-Nachweis Nachtrag Kontrast (qa-agent, 2026-11-17 abends)

Geprüft: Commit `e40ecfcb` (Bereich `c6c04f9f..8e0a346e`, HEAD `8e0a346e`, v1.99.36). Status bleibt **Testbereit**.
- **Build/Tests (ganzer Worktree):** Build 0 Fehler; Service 715/0/0, Web 3229 bestanden / 1 übersprungen / 0 Fehler.
- **Review:** `bde.css` (+14 Zeilen, nur Selektoren unter `.bde-terminal`), Klasse `bde-terminal` an `meldenModal`, `group-finish-modal`, `close-others-modal`;
  Wrapper `container-fluid bde-terminal` in `BdeTeileliste/Index`, `Summiert`, `Anmelden`, `BdeTerminal/Index`; `site.css` unverändert (`.btn-success`/`.btn-secondary`
  `site.css:151-170`). **Keine Befunde.** Hinweis außerhalb des Auftrags: „Auswahl fertigmelden“ ist `btn-primary` (deaktiviert hell, aktiv dunkel mit weißer
  Schrift) – nicht betroffen.
- **Geänderte bestehende Tests:** keine. **Nachweisart:** Manual-UAT (CSS, Browser), auf DEV erbracht.
- **DEV IDEAL (v1.99.36, Deploy Exit 0):** Werkbank 65 „Entgraten Fläche“ (Kürzel EG) temporär `BdeTeileliste = 1` (Ausgangswert 0, **danach zurückgesetzt**),
  Werker Personalnr. 123, KW-Bereich 2025-W01 bis 2027-W52 (dadurch ist für Werker 123/Werkbank 65 dieser Filter gespeichert).

| Schritt | Ergebnis | Status |
|---|---|---|
| Teileliste: „Filtern“ (blau) normal und Hover | dunkle Schrift | OK |
| Teileliste: „Fertig melden“ je Zeile (grün) | dunkle Schrift | OK |
| Modal „Fertig melden“ (geöffnet, mit Esc geschlossen, nichts gemeldet) | Knopf „Fertig melden“ dunkle Schrift | OK |
| BDE-Terminal-Modals „Gruppe fertig“/„andere schließen“ | erfordern aktive Buchungen eines Werkers | NICHT MÖGLICH (gleicher Selektor, Klasse im Markup geprüft) |
| Gegenprobe außerhalb BDE: `/BfAuftraege` „Auswahl fertig melden“ (grün) | weiße Schrift unverändert | OK |

Screenshots: ![[ts-152-12-teileliste-filtern.png]] ![[ts-152-12-teileliste-fertig-melden.jpg]] ![[ts-152-12-modal-fertig-melden.png]] ![[ts-152-12-gegenprobe-bf-auftraege.jpg]]
(Ordner `secondbrain/tests/2026-11-17-bde-kontrast-bilder/`).
- **Deploy:** nur Web (`bde.css`, zwei Views), keine Migration. Publish nicht erstellt (Auftrag des Aufrufers).
- **Offen für den Menschen:** BDE-Terminal-Modals am echten Terminal mit Buchungen ansehen (TS-152.12), Lesbarkeit bei Hallenlicht.
