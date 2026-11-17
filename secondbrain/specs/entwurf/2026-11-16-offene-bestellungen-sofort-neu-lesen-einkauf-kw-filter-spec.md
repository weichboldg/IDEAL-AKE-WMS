---
type: spec
title: Offene Bestellungen – sofort neu lesen nach Sage-WE, Ansicht im Einkauf, KW-Filter Liefertermin, Checkbox „Überfällige anzeigen“
slug: 2026-11-16-offene-bestellungen-sofort-neu-lesen-einkauf-kw-filter-spec
status: Testbereit
created: 2026-11-16
updated: 2026-11-17
source_backlog: "[[2026-11-16-offene-bestellungen-delta-nach-sage-we]]"
task: "[[2026-11-17-goal-lauf-offene-bestellungen]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
version_vorschlag: 1.99.31
testszenario: TS-155
sql_nummer: keine
affected_code:
  - "IDEALAKEWMSService/Workers/SageBookingWorker.cs:58-106 (Takt-Schleife), :73-77 (Sage-WE-Schritt), :111-138 (ProcessSageWareneingangAsync) – Anstoß nach Übergabe"
  - "IDEALAKEWMSService/Services/SageWareneingangService.cs:55-56 (RunOnceAsync, heute Task), :123/:154 (Zähler uebergeben), :168 (FinishSuccessAsync) – Rückgabe der Übergaben"
  - "IDEALAKEWMSService/Services/Sdata/EinkaufBestellungenSyncService.cs:62 (RunAsync(dryRun, ct, SdataLeseZiel? ziel), Stand 54f04740), :93-101 (Empty-Guard), :188-195 (Vollersatz in Transaktion) – prozessweite Sperre + optionaler Anlass"
  - "IDEALAKEWMSService/Services/WareneingangCsvService.cs:40-42 (CSV-Ausgangszeile nur fuer SageWeStatus Uebergeben – Beleg fuer Rueckfrage 10)"
  - "IDEALAKEWMSService/Workers/SyncWorker.cs:484-495 (regulärer Lauf Offene Bestellungen), :614-622 (RunResilientAsync) – unverändert bis auf das Warten auf die Sperre"
  - "IdealAkeWms/Models/ServiceSettingDefinitions.cs:120 (Anzeige-Katalog), :177-189 (Bereiche), :284 (Definition Sync:EinkaufBestellungenEnabled) – neue Einstellung daneben"
  - "IdealAkeWms/Controllers/PurchaseOrdersController.cs:15-16 (Class-Filter), :43-57 (ColumnMap liefertermin ohne KW), :74-131 (Index inkl. Wochen :78-81, Horizont :89, nurUeberfaellig :90), :133-149 (ZeilenAsync, Ueberfaellig :147)"
  - "IdealAkeWms/Controllers/PurchaseOrdersController.Receive.cs:14-15, :43-46 – [RequireStockAccess], bleibt unverändert (Prüfanker für einkauf ohne Lager-Recht)"
  - "IdealAkeWms/Controllers/PurchaseOrdersController.SageWe.cs:54-97 (SageWareneingaenge, Rumpf wird gemeinsame Methode mit nurOffene), :89 (CanErneutSenden = CanTransferStockAsync), :101-147 (SageWeErneutSenden/SageWeAlsUebergeben/CsvErneutSchreiben mit [RequireStockKeyUserAccess] – UNVERÄNDERT)"
  - "IdealAkeWms/Views/PurchaseOrders/SageWareneingaenge.cshtml:13 (Knopf Wareneingang zu Bestellung), :43/:48 (Statusfilter), :115-136 (Aktions-Formulare ohne asp-controller)"
  - "IdealAkeWms/Data/Repositories/PurchaseOrderReceiptRepository.cs:87-98 (GetSageWareneingaengeAsync, schliesst nur NichtNoetig aus), :132-134 (SageWeNichtUebergebenAsync ohne Gesendet/NurInSage) – unverändert"
  - "IdealAkeWms/Filters/RequireStockKeyUserAccessAttribute.cs:23 + IdealAkeWms/Services/CurrentUserService.cs:128-129 (CanTransferStockAsync = admin, stock_keyuser, picking) – unverändert, Rueckfrage 9"
  - "IdealAkeWms.Tests/Controllers/SageWareneingaengeListeTests.cs:115, :195, :270 (Reflection RequireStockKeyUserAccess an den drei Aktionen – bleibt gruen)"
  - "IdealAkeWms/Views/PurchaseOrders/Index.cshtml:10-13, :27-30, :36-39 (Wochen-Feld), :62-67 (nur überfällig), :77 (Zurücksetzen), :85-92 (Info-Zeile Muster), :99 (th liefertermin data-date-filter), :120 (Zelle ohne KW), :140-143 (Wareneingang-Knopf)"
  - "IdealAkeWms/ViewModels/PurchaseOrderListViewModel.cs:18 (HorizontBis)"
  - "IdealAkeWms/Models/AppSettingDefinitions.cs:128-130 (OffeneBestellungenVorschauWochen – BLEIBT, Anzeigename/Wirkung/Verwendung neu, Bereich 1-26 unverändert), :168-170 (EinkaufAktiv)"
  - "IdealAkeWms/Models/AppSettingKeys.cs:118 (OffeneBestellungenVorschauWochen – bleibt)"
  - "IdealAkeWms/Views/Shared/_Layout.cshtml:85-87 (showEinkauf), :114-131 (Lager-Menü inkl. Sage-Wareneingänge :128-131), :276-286 (Einkauf-Dropdown, active nur PurchaseProposals)"
  - "IdealAkeWms/Views/Users/RoleOverview.cshtml:218-229 (Zeile einkauf)"
  - "IdealAkeWms/Filters/RequireEinkaufAktivAttribute (Umleitungstext nennt nur Bestellvorschläge)"
  - "IdealAkeWms/Controllers/FaWorklistController.cs:128-148 (Muster Vorbelegung per Redirect + Sentinel)"
  - "IdealAkeWms/Controllers/WarehousePickingController.cs:380-381 (Muster UserViewPreference direkt im Controller)"
  - "IdealAkeWms/Data/Repositories/UserViewPreferenceRepository.cs (SaveAsync setzt Audit-Felder)"
  - "IdealAkeWms/Services/ColumnFilterSyntax.cs:77 (PurchaseOrders/Index: liefertermin = Datum)"
  - "IdealAkeWms/wwwroot/js/table-filter.js:485-545 (Kalender, KW-Klick schreibt 'KW'+kw)"
  - "IdealAkeWms/Data/Repositories/PurchaseOrderReceiptRepository.cs:22-30 (Rest-Regel – UNVERÄNDERT)"
  - "IdealAkeWms/Views/Help/Index.cshtml:79 (Vorschau Wochen), :581-586/:675-684 (KW-Syntax); Views/Help/Changelog.cshtml"
  - "README.md:522; docs/anwender/05-wareneingang-zu-bestellung.md; docs/anwender/06-offene-bestellungen-rahmenabrufe.md:26-27,:38-43,:103,:109,:119; docs/betrieb/04-konfiguration.md:159,:200-201; docs/betrieb/05-schnittstellen.md:69"
  - "IdealAkeWms.Tests/Controllers/PurchaseOrdersControllerTests.cs:38,:179 (Wochen-Setting); IDEALAKEWMSService.Tests/Services/Sdata/EinkaufBestellungenSyncTests.cs; IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs; IdealAkeWms.Tests/Deploy/NachlaufManifestDriftTests.cs"
  - "scripts/deploy/nachlauf.psd1 (Einstellung-/Info-Einträge, Pruefung/RolleEinkauf :142-149)"
  - "docs/anwender/05-wareneingang-zu-bestellung.md; docs/anwender/06-offene-bestellungen-rahmenabrufe.md; docs/betrieb/04-konfiguration.md; docs/betrieb/05-schnittstellen.md (Pflicht-Doku)"
open_questions:
  - "1 Laufzeit des Volllaufs: Prüfabfrage P-1 an AKE-Test (und IDEAL, falls dort aktiv) ausführen. Gilt bei Median > 30 s Variante B (Delta je BelId) statt des Volllaufs? Empfehlung: Variante A (Volllauf) bauen, B nur bei gemessenem Median > 30 s."
  - "2 Positionen ohne Liefertermin: immer sichtbar und markiert (Empfehlung) oder dem KW-Filter / der Checkbox unterworfen?"
  - "3 „Sage-Wareneingänge“ zusätzlich lesend im Menü Einkauf? Empfehlung: nein (vorerst); der Status je Position steht schon in Offene Bestellungen."
  - "4 AppSetting OffeneBestellungenVorschauWochen entfernen (Empfehlung, mit Pruefung/Reparatur im Manifest) oder umwidmen?"
  - "5 KW-Vorbelegung als „KWnn“ wie der Kalender-Klick (Empfehlung) oder als jahresgenauer Datumsbereich Mo...So?"
  - "6 Checkbox „nur überfällig“ beibehalten (Empfehlung) oder streichen?"
  - "7 Fehler im Anstoß: nur Aktivitäts-Protokoll (Empfehlung) oder zusätzlich Fehlermail wie der reguläre Lauf?"
  - "8 Gate der Einkauf-Ansicht: nur EinkaufAktiv (Empfehlung) oder zusätzlich WareneingangBestellungAktiv; Umleitungstext von RequireEinkaufAktiv allgemein fassen (Empfehlung: ja)?"
  - "9 (neu 2026-11-17, nicht blockierend) „Nur Lager-Keyuser“: Der bestehende Filter RequireStockKeyUserAccess lässt admin, stock_keyuser UND picking zu (CurrentUserService.cs:128-129). Unverändert lassen (gebaut wird so) oder für die drei Sage-WE-Aktionen auf admin + stock_keyuser einengen (wirkt dann auch in Lager → Sage-Wareneingänge)?"
  - "10 (neu 2026-11-17, Widerspruch in Antwort 3) „CSV erneut schreiben“ kann in einer Liste nur offener Wareneingänge nie erscheinen: Eine CSV-Ausgangszeile entsteht nur bei SageWeStatus Uebergeben (WareneingangCsvService.cs:40-42). Akzeptiert (gebaut wird: Aktion bleibt nur in Lager → Sage-Wareneingänge wirksam) oder sollen übergebene Vorgänge mit CSV-Fehler in der Einkauf-Liste zusätzlich erscheinen?"
  - "11 (neu 2026-11-17, nicht blockierend) Rücksprung nach einer Sage-WE-Aktion aus der Einkauf-Liste: Die bestehenden Endpunkte leiten auf Lager → Sage-Wareneingänge um (PurchaseOrdersController.SageWe.cs:109/114/128/133/146). So lassen (gebaut wird so) oder Rücksprung in die Einkauf-Liste?"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: "A; ohne Termin sichtbar; Sage-WE lesend im Einkauf; Vorschau-Wochen bleibt (Default 2) als Datumsbereich; Überfällige Lager aus/EK ein"
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-17"
---

# Offene Bestellungen: sofort neu lesen nach Sage-WE, Ansicht im Einkauf, KW-Filter, „Überfällige anzeigen“

Quelle: [[2026-11-16-offene-bestellungen-delta-nach-sage-we]] inkl. „Erweiterung Gerald 2026-11-16 20:07/20:09“. Bezug:
[[2026-11-14-review-befunde-offene-bestellungen]], parallele Spec [[2026-11-16-bestellvorschlaege-nachbesserung-spec]] (Kollisionen siehe unten).

Code gelesen im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, HEAD 223d7015. Alle Zeilenangaben beziehen sich auf diesen Stand.

> [!note] Rumpf nachgezogen 2026-11-17 (Antworten 1–8)
> Die Freigabe-Antworten 1–8 (Gerald 2026-11-17 06:41/06:43) und die Ergänzungen der Koordination vom selben Tag sind in Umfang,
> Anforderungen, Lösungsentwurf, affected_code, Einstellungen, Akzeptanzkriterien, Testszenarien und Deploy eingearbeitet. Neu gelesen und
> nachbelegt auf HEAD **54f04740** (v1.99.30): `PurchaseOrdersController.cs`, `PurchaseOrdersController.SageWe.cs`,
> `SageWareneingaenge.cshtml`, `PurchaseOrderReceiptRepository.cs`, `CurrentUserService.cs`, `ColumnFilterSyntax.cs`, `WareneingangCsvService.cs`,
> `_Layout.cshtml`, `RoleOverview.cshtml`, `AppSettingDefinitions.cs`, `nachlauf.psd1`. Die übrigen Zeilenangaben stammen von 223d7015; der
> Dev-Lauf prüft sie vor dem Ändern. Überholte Textstellen sind mit **ueberholt** markiert. Durch den Nachzug entstanden die Rückfragen 9–11
> (nicht blockierend, gebaut wird die jeweils genannte Variante).

## Nummern (geprüft 2026-11-16, nachgeprüft 2026-11-17 auf 54f04740; Dev-Lauf prüft erneut)

| Was | Stand | Diese Spec |
|---|---|---|
| Version | beide `AppVersion.cs` = **1.99.30** (Bestellvorschläge-Nachbesserung, `IdealAkeWms/AppVersion.cs:5`, `IDEALAKEWMSService/AppVersion.cs:5`); ~~1.99.32~~ ueberholt | **1.99.31** |
| SQL | höchste Datei `SQL/155_AddArticleLastOrderUnassigned.sql` (Manifest `SQL/155` Version 1.99.30, `nachlauf.psd1:101`) | **keine** (falls doch nötig: 156) |
| Testszenario | letztes Kapitel TS-154 (Bestellvorschläge, `docs/TESTSZENARIEN.md:12641`); TS-155 frei | **TS-155** |
| ADR | höchster 0029 | **kein neuer ADR**; nächster freier wäre 0030 |

Die parallele Session (Bestellvorschläge, Status InUmsetzung, derselbe Worktree) kann Nummern belegen, bevor dieser Lauf startet. Der Dev-Lauf
nimmt dann die jeweils nächste freie Nummer und trägt sie hier nach.

## Ziel / Nutzen (das Warum)

1. Nach einem an Sage übergebenen Wareneingang steht die Bestellung heute bis zu 15 Minuten unverändert in „Offene Bestellungen“, weil der Cache
   `SagePurchaseOrderLines` nur im 15-Minuten-Zyklus voll ersetzt wird (`SyncWorker.cs:484-495`, Intervall `WorkerSettings:SyncIntervalMinutes`,
   `SyncWorker.cs:31`). Die Rest-Regel zieht das im WMS Erfasste bereits ab (`PurchaseOrderReceiptRepository.cs:22-30`), eine EBB mit Rest 0
   bleibt aber sichtbar, bis Sage sie nicht mehr liefert. Ein entprellter Volllauf direkt nach der Übergabe verkürzt das auf Sekunden bis wenige
   Minuten.
2. Der Einkauf braucht die Liste in seinem eigenen Menü, ohne Lager-Rechte und ohne Wareneingang buchen zu können. Dazu sieht er die noch
   nicht an Sage übergebenen Wareneingänge („Sage-Wareneingänge“, nur offene) lesend (Antwort 3).
3. Die Liste soll wie andere Datumsspalten nach Datum/Kalenderwoche filtern. Vorbelegt ist ein jahresgenauer Datumsbereich von Montag der
   aktuellen KW bis Sonntag der letzten Vorschau-KW; die Anzahl Wochen kommt aus der bestehenden Einstellung `OffeneBestellungenVorschauWochen`
   (Default 2). Antworten 4 und 5. ~~„Das ersetzt die Einstellung „Wochen voraus““~~ ueberholt durch Antwort 4.
4. Überfällige Positionen stören das Lager bei der Tagesplanung, der Einkauf braucht sie. Daher eine Checkbox je Ansicht mit
   unterschiedlichem Default, je Benutzer gemerkt.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- F1 Volllauf „Offene Bestellungen (Sage)“ nach erfolgreicher Sage-WE-Übergabe, entprellt, neue ServiceSetting.
- F2 Ansicht „Offene Bestellungen“ im Menü Einkauf (lesend; Wareneingang nur mit Lager-Recht, serverseitig).
- F2a Ansicht „Sage-Wareneingänge“ im Menü Einkauf, nur offene (offen, gesendet, Fehler, Klären, nur in Sage buchen); Aktionen nur für
  Lager-Keyuser, serverseitig über die bestehenden Endpunkte geprüft (Antwort 3).
- F3 Datumsfilter Liefertermin mit Vorbelegung als Datumsbereich (beide Ansichten); `OffeneBestellungenVorschauWochen` bleibt und bestimmt
  die Anzahl Wochen; das Feld „Wochen“ der Filterkarte entfällt (Antworten 4, 5).
- F4 Checkbox „Überfällige anzeigen“ (Lager Default aus, Einkauf Default ein, je Benutzer gespeichert), Hinweiszeile. „nur überfällig“ bleibt
  (Antwort 6). Positionen ohne Liefertermin immer sichtbar (Antwort 2).
- F5 Rest-Regel und Wareneingang (`Receive`) bleiben unverändert – ausdrücklich.

**Out-of-Scope**
- Delta-Abgleich je `BelId` (Vorschlag des Backlogs): Variante B wird **nicht** gebaut (Antwort 1: Variante A). B nur, falls P-1 später einen
  Median > 30 s misst; dann eigene Spec.
- Manuelles „Als übergeben markieren“ (`PurchaseOrdersController.SageWe.cs:122`) löst **keinen** Anstoß aus: Es läuft im Web, nicht im Dienst.
  Der reguläre Zyklus holt das nach.
- Änderungen an `PurchaseProposalsController` / Bestellvorschläge (gehört der Parallel-Spec).
- ~~„Sage-Wareneingänge“ im Einkauf (Rückfrage 3).~~ ueberholt durch Antwort 3 → jetzt In-Scope F2a.
- Bereits an Sage übergebene Wareneingänge in der Einkauf-Ansicht (Antwort 3); Lager → Sage-Wareneingänge bleibt unverändert (alle Status).
- Änderungen an den Sage-WE-Aktions-Endpunkten, ihren Filtern und ihrem Rücksprung (Rückfragen 9, 11).
- Jedes Schreiben nach Sage außer der bestehenden Sage-WE-Logik. Der Anstoß liest nur (`EinkaufBestellungenSyncService` schreibt nie nach Sage,
  `ServiceSettingDefinitions.cs:284`).
- Keine Lieferverzug-Mail durch den Anstoß; der Schritt `SyncWorker.cs:497-508` bleibt nur im regulären Zyklus.

## Fachliche Anforderungen

### F1 Volllauf nach Sage-Wareneingang (entschieden: Volllauf, entprellt)

- **F1.1 Auslöser:** Hat ein Takt des Laufs „Sage-Wareneingang“ mindestens eine Position erfolgreich übergeben, gilt ein Anstoß als offen.
  Gezählt wird der bestehende Zähler `uebergeben` in `SageWareneingangService.RunOnceAsync`. Er steigt bei Erfolg (`:154`) und bei „in Sage gefunden“
  (`:123`). Beides heißt: Sage hat den Beleg, der Cache ist veraltet, also zählen beide.
- **F1.2 Entprellung:** Der erste Anstoß läuft sofort, also im selben Takt nach dem Sage-WE-Schritt. Danach läuft höchstens ein Anstoß je
  `Sync:EinkaufBestellungenNachWeMinuten` Minuten. Übergaben innerhalb dieses Fensters halten den Anstoß offen. Er läuft einmal, sobald das Fenster
  abgelaufen ist, im ersten Takt danach (20-s-Takt, `Sync:SageLagerbuchungIntervalSeconds`).
- **F1.3 Abschalten:** `Sync:EinkaufBestellungenNachWeMinuten = 0` schaltet den Anstoß ab. Dann gilt das Verhalten von heute.
- **F1.4 Gates:** Die Gates des regulären Laufs gelten unverändert: `Sync:EinkaufBestellungenEnabled` muss an sein, `WorkerSettings:SyncDryRun` wird
  übernommen. Der Lauf selbst bringt den Empty-Guard mit (`EinkaufBestellungenSyncService.cs:93-101`), die Zugangsprüfungen ebenso.
  Der Sage-WE-Schritt läuft nur bei `SageWareneingangBelegAktiv` (`SageBookingWorker.cs:73`). Damit wirkt F1 praktisch nur bei AKE
  (`ServiceSettingDefinitions.cs:75-76` „AKE (IDEAL ohne Wirkung)“).
- **F1.5 Fehler:** Scheitert der angestoßene Lauf, schreibt er wie bisher „Run fehlgeschlagen …“ unter „Offene Bestellungen (Sage)“
  (`EinkaufBestellungenSyncService.cs:83-89`). Der Worker schreibt zusätzlich eine Warnung in den Lauf „Sage-Wareneingang“: „Offene Bestellungen
  nach Sage-Wareneingang nicht neu gelesen: <Meldung>. Der reguläre Abgleich holt das nach.“ Der Anstoß gilt danach als erledigt. Er wird nicht in
  Schleife wiederholt; der 15-Minuten-Zyklus bleibt die Rückfallebene. Eine Fehlermail gibt es nicht (Antwort 7).
  Der Sage-WE-Schritt und der CSV-Schritt laufen unabhängig weiter.
- **F1.6 Nachvollziehbarkeit:** Der angestoßene Lauf schreibt als erste Info-Zeile „Anlass: Sage-Wareneingang (n Position(en) übergeben)“.
  Daran erkennt man ihn im Aktivitäts-Protokoll vom regulären Lauf.
- **F1.7 Nebenläufigkeit:** Ein angestoßener und ein regulärer Lauf dürfen nie gleichzeitig laufen. Der Vollersatz löscht und fügt in einer
  Transaktion ein (`:186-193`), und `BelPosId` ist UNIQUE (`ApplicationDbContext.cs:1431`). Ein paralleler zweiter Lauf scheitert deshalb beim
  Löschen schon gelöschter Zeilen (Concurrency-Fehler) oder am Index. Ist ein Lauf aktiv, bleibt der Anstoß offen und wird im nächsten Takt
  nachgeholt. Der reguläre Lauf wartet auf das Ende des laufenden Laufs.
- **F1.8 Laufzeit:** Der Anstoß läuft im Takt des `SageBookingWorker` und verzögert die nächste Sage-Lagerbuchung um seine Laufzeit. Die Laufzeit ist
  in dieser Sitzung nicht gemessen (**unbelegt**). Gebaut wird Variante A (Antwort 1). Prüfabfrage P-1 (unten) läuft als Beobachtung im Test
  (TS-155.1) und nach dem Deploy; erst ein gemessener Median > 30 s öffnet Variante B als eigene Spec.

**Variante B (nicht gebaut – Antwort 1; nur Referenz für eine spätere Spec):**
- Statt des Volllaufs liest der Anstoß nur die übergebenen `BelId` per SData: `EinkaufPositionenInfo`, `where BelId eq …`, je Belegart.
- Er ersetzt nur deren Cache-Zeilen. Liefert Sage nichts mehr, werden die Zeilen gelöscht.
- Dafür wird der Zuordnungscode aus `RunAsync` (`:103-141`, Artikel/Kreditor/Platz/Liefertermin) in eine gemeinsame Methode gezogen, die Volllauf und
  Delta beide nutzen.
- Kein Empty-Guard, da gezielt gelesen wird. Mehraufwand: geschätzt ein zusätzlicher Dev-Tag plus Tests für „Beleg verschwunden“.
- ~~Bis zur Entscheidung gilt Variante A.~~ ueberholt: entschieden, Variante A (Antwort 1).

### F2 „Offene Bestellungen“ im Menü Einkauf

- **F2.1 Menü:** Neuer Eintrag „Offene Bestellungen“ im Einkauf-Dropdown (`_Layout.cshtml:276-286`), sichtbar wie das Dropdown selbst
  (`showEinkauf`, `:85-87`: EinkaufAktiv + Rolle einkauf/admin). Die active-Markierung des Dropdowns erkennt auch die neue Seite.
- **F2.2 Zugriff:** Für die Seite gelten `[RequireEinkaufAccess]` und `[RequireEinkaufAktiv]`, dieselben Filter wie Bestellvorschläge
  (`PurchaseProposalsController.cs:15-17`). Ein Lager-Recht ist nicht nötig. Admin sieht die Seite über die Admin-Wildcard (ADR 0006).
  Gate ist nur `EinkaufAktiv`, nicht zusätzlich `WareneingangBestellungAktiv` (Antwort 8); das gilt auch für F2a.
- **F2.3 Lesend:** Die Ansicht zeigt dieselben Zeilen, Spalten, Hinweise und dieselbe Stand-Zeile wie Lager → Offene Bestellungen.
  Der Knopf „Wareneingang“ (je Zeile `Index.cshtml:140-143` und oben `:10-13`) erscheint nur, wenn beides gilt: Der Benutzer hat Lager-Schreibrecht
  (`CanAccessStockAsync`) und `WareneingangBestellungAktiv` ist an. Sonst würde `Receive` mit 404 enden (Class-Filter
  `RequireWareneingangBestellungAktiv`, `PurchaseOrdersController.cs:15-16`).
- **F2.4 Serverseitig:** `Receive` GET und POST bleiben unverändert hinter `[RequireStockReadAccess]` + `[RequireWareneingangBestellungAktiv]` (Class)
  und `[RequireStockAccess]` (Action, `PurchaseOrdersController.Receive.cs:14-15`, `:43-46`). Ein Benutzer nur mit Rolle einkauf bekommt bei direktem
  Aufruf per URL oder POST AccessDenied. Das ist so gebaut und wird per Test belegt, nicht neu gebaut.
- **F2.5 Lager-Ansicht unverändert erreichbar:** Lager → Offene Bestellungen bleibt bei `RequireStockReadAccess` + `RequireWareneingangBestellungAktiv`.
- **F2.6 Rollen:** Es gibt keinen neuen Rollenkey und keinen neuen Filter. Die Rolle einkauf bekommt Lesezugriff auf zwei weitere Seiten
  (Offene Bestellungen, offene Sage-Wareneingänge). Nach CLAUDE.md gehören drei Stellen dazu:
  - Attribut: unverändert wiederverwendet (`RequireEinkaufAccess` + `RequireEinkaufAktiv` am neuen Controller; `RequireStockKeyUserAccess` an den
    Sage-WE-Aktionen unverändert).
  - `secondbrain/codebase/controller.md`: neue Zeile für den Controller (beide Aktionen), Zugriffsmatrix für einkauf ergänzen. Die Brain-Datei pflegt
    der Dev-Lauf im Hauptcheckout.
  - `Views/Users/RoleOverview.cshtml:218-229`: Beschreibung der Zeile einkauf wird zu „Bestellvorschläge, Offene Bestellungen und offene
    Sage-Wareneingänge (nur lesen)“; Liste um `/EinkaufPurchaseOrders` und `/EinkaufPurchaseOrders/SageWareneingaenge` ergänzen, mit dem Satz
    „Aktionen an Sage-Wareneingängen nur mit Lager-Keyuser-Recht (Filter `RequireStockKeyUserAccess`)“.
  - Dazu kommt `nachlauf.psd1` `Pruefung/RolleEinkauf` (`:142-149`): Die Rollenbeschreibung im Reparatur-Insert (`:146`) wird für neue
    Installationen nachgezogen: `Einkauf - Bestellvorschlaege, Offene Bestellungen, offene Sage-Wareneingaenge (lesen).` (nur ASCII). Bestehende
    Rollenzeilen ändert die Reparatur nicht (sie greift nur bei fehlender Rolle).

### F2a „Sage-Wareneingänge“ (nur offene) im Menü Einkauf (Antwort 3)

- **F2a.1 Menü:** Zweiter Eintrag im Einkauf-Dropdown „Sage-Wareneingänge (offen)“, sichtbar wie das Dropdown (`showEinkauf`). Gate nur `EinkaufAktiv`
  (Antwort 8 sinngemäß); die Lager-Ansicht zeigt den Eintrag nur bei `WareneingangBestellungAktiv` (`_Layout.cshtml:128-131`), die Einkauf-Ansicht
  nicht.
- **F2a.2 Zeilenmenge:** Nur Wareneingänge im Status **Offen, Gesendet, Fehler, Klären, NurInSage** (Enum `PurchaseOrderReceipt.cs:15-26`). Es fehlen
  **Uebergeben** (Antwort 3) und wie heute `NichtNoetig` (`PurchaseOrderReceiptRepository.cs:92`). Die Statusauswahl der Filterkarte bietet nur diese
  fünf Werte; ein per URL übergebenes `status=Uebergeben` ergibt eine leere Liste (die Einschränkung gilt immer, nicht nur über das Auswahlfeld).
  Hinweis: Die bestehende Zählung `SageWeNichtUebergebenAsync` (`:132-134`) zählt nur Offen/Fehler/Klären; sie bleibt für den Q-2-Hinweis
  unverändert und ist **nicht** die Definition dieser Liste.
- **F2a.3 Lesend:** Spalten, Hinweise (Q-2 `AusHinweis`, CSV-Hinweise), Spaltenfilter und Pagination wie Lager → Sage-Wareneingänge
  (`SageWareneingaenge.cshtml`). Der Knopf „Wareneingang zu Bestellung“ (`:13`) erscheint nur unter derselben Bedingung wie in F2.3
  (Lager-Schreibrecht + `WareneingangBestellungAktiv`).
- **F2a.4 Aktionen:** „Erneut senden“, „Als übergeben markieren“ und „CSV erneut schreiben“ erscheinen nur, wenn `CanTransferStockAsync()` wahr ist
  (wie heute `:89`/`:115`/`:128`) **und** `WareneingangBestellungAktiv` an ist. Grund: Die Formulare posten auf die bestehenden Endpunkte im
  `PurchaseOrdersController` (`SageWe.cs:101-147`), dessen Class-Gate `RequireWareneingangBestellungAktiv` sonst 404 liefert.
  - Serverseitig unverändert: `[RequireStockKeyUserAccess]` (Filter `RequireStockKeyUserAccessAttribute.cs:23` → `CanTransferStockAsync` =
    admin, stock_keyuser, picking, `CurrentUserService.cs:128-129`) plus Class-Filter `RequireStockReadAccess` + `RequireWareneingangBestellungAktiv`
    + `[ValidateAntiForgeryToken]`. Ein Benutzer nur mit einkauf bekommt bei direktem POST AccessDenied. Ob picking hier weiter zählen soll:
    Rückfrage 9; gebaut wird der bestehende Filter unverändert.
  - Die Formulare in der View bekommen ausdrücklich `asp-controller="PurchaseOrders"`. Ohne das zielte `asp-action` aus dem neuen Controller auf
    `/EinkaufPurchaseOrders/SageWeErneutSenden` (gibt es nicht).
  - Rücksprung nach der Aktion: wie heute auf Lager → Sage-Wareneingänge (`RedirectToAction(nameof(SageWareneingaenge))`); Rückfrage 11.
  - „CSV erneut schreiben“ wird in dieser Liste praktisch nie sichtbar: Eine CSV-Ausgangszeile entsteht nur für übergebene Vorgänge
    (`WareneingangCsvService.cs:40-42`), die hier fehlen. Rückfrage 10; gebaut wird die Bedingung unverändert (kein Sonderfall).
- **F2a.5 Lager-Ansicht unverändert:** Lager → Sage-Wareneingänge zeigt weiter alle Status außer NichtNoetig.

### F3 Datumsfilter Liefertermin mit Vorbelegung als Datumsbereich (Antworten 4, 5)

- **F3.1 KW-tauglich rendern** (bleibt nötig für den Kalender-Klick auf andere Wochen, Antwort 4 „KW-/Kalenderfilter für andere Zeiträume“): Die Zelle Liefertermin und `ColumnMap["liefertermin"]` rendern `dd.MM.yyyy KWnn` (ISO-Woche,
  `ISOWeek.GetWeekOfYear`, wie `Views/ProductionOrders/Index.cshtml:7-13`). Heute fehlt dort die KW (`PurchaseOrdersController.cs:43-57`,
  `Index.cshtml:120`). Der Kalender-Klick schreibt `KWnn` ins Filterfeld (`table-filter.js:520`), das als Text-Enthält-Filter wirkt. Ohne KW
  im Zelltext findet der KW-Filter also heute **nichts**. Zeilen ohne Termin rendern weiter „ohne Termin“.
- **F3.2 Kalender:** `<th … data-date-filter data-col-key="liefertermin">` gibt es schon (`Index.cshtml:99`), den Spaltentyp Datum auch
  (`ColumnFilterSyntax.cs:77`). Kalender, Bereich `a...b` und Vergleiche funktionieren damit ohne neuen Code.
- **F3.3 Vorbelegung:** Beim Erstaufruf jeder Ansicht leitet der Controller um, und zwar auf dieselbe Aktion mit
  `colf_liefertermin=<Mo>...<So>` und einem Merker `kw=1`. Erstaufruf heißt: kein `colf_liefertermin` und kein Merker in der Query.
  - `N = OffeneBestellungenVorschauWochen` (`GetIntValueAsync(…, 2)`, wie heute `PurchaseOrdersController.cs:78`), auf 1–26 begrenzt wie heute `:79`.
  - `Mo = ISOWeek.ToDateTime(ISOWeek.GetYear(heute), ISOWeek.GetWeekOfYear(heute), DayOfWeek.Monday)`, `So = Mo.AddDays(7 * N - 1)`,
    beide als `dd.MM.yyyy`. Beispiel N = 2, heute Di 17.11.2026 (KW 47): `16.11.2026...29.11.2026`. N = 1 = nur aktuelle KW.
  - Jahresgenau und über den Jahreswechsel korrekt, weil echte Daten statt `KWnn` (Antwort 5). Die Bereichs-Syntax `a...b` für Datumsspalten
    gibt es im Filter-Kern (`ColumnFilterSyntax.cs:309-316`); der Zellwert wird am Anfang des Zelltexts gelesen (`CellValue`, `:344-347`), also
    funktioniert der Bereich auch mit `dd.MM.yyyy KWnn`. „ohne Termin“ hat keinen Datumswert und trifft den Bereich nie – deshalb F4.2.
  - ~~`colf_liefertermin=KW<aktuelle ISO-KW>`~~ ueberholt durch Antwort 5.
  - Muster ist `FaWorklistController.cs:128-148`.
  - Der Merker überlebt das Leeren des Filters, weil `applyServerFilters` nur `colf_*`/`page` entfernt. Leert der Benutzer das Feld, sieht er alle
    Termine und es wird **nicht** erneut vorbelegt.
  - „Zurücksetzen“ (`Index.cshtml:77`) führt wieder ohne Parameter auf die Aktion und belegt neu vor.
- **F3.4 Einstellung bleibt, Horizont-Filter und Feld „Wochen“ entfallen** (Antwort 4):
  - `OffeneBestellungenVorschauWochen` bleibt (Key `AppSettingKeys.cs:118`, Bereich 1–26 und Default 2 unverändert, `AppSettingDefinitions.cs:128-130`).
    Neu sind Anzeigename, Wirkung und Verwendung (siehe „Einstellungen“): Sie bestimmt nur noch die Vorbelegung des Spaltenfilters Liefertermin.
  - Bedeutungswechsel: bisher „heute + N × 7 Tage“ (`:81`), jetzt „Montag der aktuellen KW bis Sonntag der N-ten KW“. Das Ende liegt je nach
    Wochentag 1–7 Tage früher als bisher (bisher `heute + 7N`, jetzt `Mo + 7N - 1`); ein gespeicherter Wert bleibt gültig. Deploy-Hinweis (Manifest).
  - Entfernt werden: das Feld „Wochen“ der Filterkarte (`Index.cshtml:36-39`), der Parameter `wochen`, der Horizont-Filter
    (`PurchaseOrdersController.cs:79-81`, `:89` nur noch „ohne Termin“-Regel nach F4.2), `HorizontBis` (`PurchaseOrderListViewModel.cs:18`, Stand-Zeile
    `Index.cshtml:27-30` ohne „bis …“) und `Wochen` im ViewModel. Andere Zeiträume wählt der Benutzer im Spaltenfilter (Kalender, KW-Klick,
    Bereich, Vergleich).
  - Fundstellen, die mitziehen (Text statt Entfernen): `README.md:522`, `Help/Index.cshtml:79-80`, `docs/anwender/06…` (`:26-27`, `:41`, `:103`,
    `:119`). `PurchaseOrdersControllerTests.cs:38` (Setting-Mock bleibt, Erwartung jetzt Redirect-Bereich) und `:179-182` (Validierung 1–26 bleibt
    gültig, unverändert grün).
  - ~~„… und die AppSetting `OffeneBestellungenVorschauWochen` … werden entfernt (Rückfrage 4, Empfehlung)“, „verwaist unter /Settings →
    Manifest-Prüfung mit Reparatur“~~ ueberholt durch Antwort 4: Es gibt keine verwaiste Zeile, keine Reparatur.
- **F3.5 Grenze:** Die Vorbelegung ist jahresgenau. Ein vom Benutzer per Kalender geklicktes `KWnn` bleibt jahreslos (Text-Enthält, `KW4` trifft
  auch KW40–49) – dieselbe Grenze wie in allen Datumsspalten (Help `:581-586`), hier nicht geändert. ~~Rückfrage 5 offen~~ ueberholt.

### F4 Checkbox „Überfällige anzeigen“

- **F4.1 Begriff:** „überfällig“ = `Liefertermin < heute` (bestehende Regel `PurchaseOrdersController.cs:147`).
- **F4.2 Sichtbarkeitsregel**, präzise, je Zeile nach allen anderen Filtern (Lieferant, Bestellung, Artikel, Lagerplatz, Abrufe, übrige
  Spaltenfilter):
  - Zeile **ohne Termin** → immer sichtbar, Hinweis „ohne Termin“ (Antwort 2).
  - Zeile **überfällig** → sichtbar genau dann, wenn „Überfällige anzeigen“ an ist. Der Liefertermin-Spaltenfilter wirkt auf überfällige Zeilen
    **nicht**. Sonst blendete die Vorbelegung (ab Montag der aktuellen KW) alle Überfälligen aus früheren Wochen aus, und die Checkbox im Einkauf
    wäre wirkungslos.
  - Zeile **nicht überfällig** → sichtbar genau dann, wenn sie den Liefertermin-Spaltenfilter erfüllt (leerer Filter = alle).
  - Überfällige der laufenden KW (Montag bis gestern) folgen ebenfalls nur der Checkbox, sie bleiben also im Lager bei Default AUS ausgeblendet.
    So gilt eine einzige Regel ohne Sonderfall.
- **F4.3 „nur überfällig“** bleibt (Antwort 6) und zeigt nur überfällige Zeilen; es schließt „Überfällige anzeigen“ ein. Ist es
  angehakt, gilt die Checkbox als an.
- **F4.4 Hinweiszeile:** Sind überfällige Zeilen wegen der Checkbox ausgeblendet, erscheint über der Tabelle die Zeile
  „n überfällige Position(en) ausgeblendet – anzeigen mit „Überfällige anzeigen““. Gezählt wird nach allen anderen Filtern und vor der
  Pagination. Bei n = 0 erscheint keine Zeile. Muster ist die Info-Zeile der ausgeblendeten Abrufe (`Index.cshtml:85-92`, Zählung
  `PurchaseOrdersController.cs:100-106`). Die Zeile steht als Text und hängt nicht nur an einer Farbe.
- **F4.5 Default und Speichern je Benutzer:**
  - Default: Lager **aus**, Einkauf **ein** (freigabe_entscheidung „Überfällige Lager aus/EK ein“, bestätigt von Gerald 2026-11-17).
  - Gespeichert wird in der bestehenden Tabelle `UserViewPreferences` (`UserId`, `ViewKey`, `SettingsJson`; AuditableEntity), je Ansicht ein
    eigener ViewKey: `PurchaseOrders.Ueberfaellige` (Lager) und `EinkaufPurchaseOrders.Ueberfaellige` (Einkauf), `SettingsJson` = `{"an":true|false}`.
  - Zugriff über `IUserViewPreferenceRepository.GetByUserAndViewAsync` / `SaveAsync` direkt im Controller (Muster
    `WarehousePickingController.cs:380-381`).
  - **Nicht** über den Spalten-Prefs-JSON des ViewKeys „PurchaseOrders“: `column-preferences.js` verwirft unbekannte Keys (mergeWithDefaults),
    und die API prüft ViewKeys gegen ColumnDefinitions (`UserViewPreferencesApiController`).
  - Der Parameter `ueberfaellige` ist `bool?`. `null` (Erstaufruf, Pagination-Links ohne Parameter) liest die Präferenz. Ein gesetzter Wert aus dem
    Filterformular wird angewendet und gespeichert, wenn er von der gespeicherten Präferenz abweicht.
  - In `Users/Edit` erscheint der Eintrag als Ansicht mit ViewKey-Namen (`Users/Edit.cshtml:211`). „Zurücksetzen“ dort löscht ihn, danach gilt
    wieder der Default.
- **F4.6 Pagination und Filter-Links** übernehmen `ueberfaellige` (wie heute `nurUeberfaellig`), damit Seite 2 dieselben Zeilen zählt.

### F5 Unverändert

- Rest-Regel `ErfasstJeBelPos.Abzuziehen/Rest` (`PurchaseOrderReceiptRepository.cs:22-30`), die Maske `Receive` und das Ausblenden der EBB mit Rest 0
  (`PurchaseOrdersController.cs:98-99`) bleiben unverändert.

## Technischer Loesungsentwurf

### Dienst (F1)

1. `SageWareneingangService.RunOnceAsync` (`:55-56`) gibt `Task<int>` zurück, die Anzahl `uebergeben`. Bei früher Rückkehr ohne Arbeit (`:73`) und
   beim Werfen (`:170-176`) gilt: Was bis zum Wurf übergeben wurde, zählt nicht mehr. Ein Fehlerlauf löst also keinen Anstoß aus; der nächste
   erfolgreiche Takt tut es.
   Weitere Aufrufer per Grep prüfen (Tests) und anpassen.
2. `SageBookingWorker` bekommt zwei Instanzfelder `_anstossOffen` (bool, plus Anzahl für F1.6) und `_letzterAnstoss` (DateTime?, Lokalzeit
   `DateTime.Now`, ADR 0025). `ProcessSageWareneingangAsync` (`:111-138`) setzt `_anstossOffen`, wenn `uebergeben > 0` ist.
3. Neuer Schritt im Takt, direkt nach dem Sage-WE-Schritt (`:73-77`), in eigenem `try` wie der CSV-Schritt (`:91-103`):

   ```
   minuten = ServiceSettings.GetIntAsync("Sync:EinkaufBestellungenNachWeMinuten", 2)
   wenn _anstossOffen && minuten > 0 && GetBool("Sync:EinkaufBestellungenEnabled")
        && (_letzterAnstoss == null || jetzt - _letzterAnstoss >= minuten min):
       scope → EinkaufBestellungenSyncService
       lief = await svc.TryRunExclusiveAsync(dryRun, ct, anlass: "Sage-Wareneingang (n Position(en) übergeben)")
       wenn lief == false (Sperre belegt): nichts, bleibt offen
       sonst: _anstossOffen = false; _letzterAnstoss = Startzeit
   catch: Warnung in SyncLogServices.SageWareneingang (F1.5); _anstossOffen = false; _letzterAnstoss = Startzeit
   ```

   - Ist `Sync:EinkaufBestellungenEnabled` aus, wird der offene Anstoß verworfen (`_anstossOffen = false`), damit er nicht Stunden später
     nach dem Einschalten unerwartet läuft.
   - Neustart des Dienstes setzt beide Felder zurück. Das ist unkritisch, der reguläre Lauf startet beim Dienststart ohnehin.
4. **Sperre:** `EinkaufBestellungenSyncService` bekommt ein `private static readonly SemaphoreSlim Sperre = new(1, 1)`.
   Der Service ist Scoped (`IDEALAKEWMSService/Program.cs:88`), daher muss die Sperre statisch sein. Beide BackgroundServices laufen im selben
   Prozess (`Program.cs:164-167`).
   - `RunAsync` (regulär, `SyncWorker.cs:484-495`) wartet mit `WaitAsync(ct)` auf die Sperre.
   - Der Anstoß nutzt `TryRunExclusiveAsync` = `WaitAsync(0)`; ist die Sperre belegt, kommt `false` zurück.
   - `// ponytail: prozessweite Sperre; reicht, solange nur EIN Dienst je DB läuft (Ist-Zustand). Mehrere Dienste je DB -> sp_getapplock.`
5. Optionaler Parameter `string? anlass` an `RunAsync`. Ist er gesetzt, schreibt der Lauf direkt nach dem Start `LogInfoAsync("Anlass: …")`.
6. Der Ctor des `SageBookingWorker` bleibt bei `ISyncLogger` als letztem Parameter (ADR 0010). Der Service wird per Scope aufgelöst, wie
   `SageWareneingangService` schon heute.
7. Neue ServiceSetting im Katalog (ADR 0008, Drift-Guard `ServiceSettingDefinitionsTests`), siehe „Einstellungen“:
   - Definition neben `:284`
   - Anzeige-Eintrag neben `:120`
   - Bereich `(0, 60, "min")` in der Bereichstabelle `:177-189`

### Web (F2–F4)

1. **Gemeinsame Listenlogik:** Der Rumpf von `PurchaseOrdersController.Index` (`:74-131`) wird zu einer Methode
   `Task<PurchaseOrderListViewModel> BuildListAsync(PurchaseOrderListAnsicht ansicht, …filter…)`. Sie liegt im selben Controller als `internal`
   oder in einer kleinen statischen Hilfsklasse neben dem Controller. Kein neuer Service mit Interface nötig: eine Implementierung, zwei Aufrufer.
   - `ansicht` ∈ {Lager, Einkauf} bestimmt ViewKey der Präferenz, Default der Checkbox, Aktion/Controller der Formular- und Pagination-Links und
     `CanReceive`.
   - Ebenso wird der Rumpf von `SageWareneingaenge` (`PurchaseOrdersController.SageWe.cs:54-97`) zu
     `BuildSageWeListAsync(bool nurOffene, …filter…)` im selben Helfer. `nurOffene = true` filtert nach dem Repository-Aufruf auf
     `Offen | Gesendet | Fehler | Klaeren | NurInSage` (eine statische Menge, z. B. `SageWareneingang.OffeneStatus`, auch für die Statusauswahl der View),
     vor Spaltenfilter und Pagination. Das Repository bleibt unverändert. Die Lager-Aktion ruft mit `nurOffene = false` und verhält sich wie heute.
   - Der Helfer braucht `_receipts`, `_serviceSettings`, `_user` (bzw. für die Liste `_lines`, `_locations`, `_settings`, `IUserViewPreferenceRepository`);
     sie werden als Parameter übergeben (statische Klasse) – kein neuer DI-Service.
2. **Neuer Controller** `EinkaufPurchaseOrdersController` (Route `/EinkaufPurchaseOrders`, UI-Titel „Offene Bestellungen“) mit
   `[RequireEinkaufAccess][RequireEinkaufAktiv]` auf Klassenebene und zwei Aktionen:
   - `Index` mit Redirect-Vorbelegung (F3.3), rendert `Views/PurchaseOrders/Index.cshtml` (explizit per Pfad) mit `ansicht = Einkauf`.
   - `SageWareneingaenge` (F2a), rendert `Views/PurchaseOrders/SageWareneingaenge.cshtml` (explizit per Pfad) mit `nurOffene = true`. Das ViewModel
     bekommt `NurOffene` (Titel „Sage-Wareneingänge (offen)“, Statusauswahl nur die fünf Werte, Breadcrumb Einkauf), `CanReceive` (Knopf
     „Wareneingang zu Bestellung“) und `CanErneutSenden = CanTransferStockAsync() && WareneingangBestellungAktiv`. In der Lager-Ansicht bleibt
     `CanErneutSenden` wie heute (`:89`), dort ist das Gate per Class-Filter ohnehin erfüllt.
   - Aktions-Formulare und der Knopf „Wareneingang zu Bestellung“ in `SageWareneingaenge.cshtml` bekommen `asp-controller="PurchaseOrders"`; das
     Filterformular, „Zurücksetzen“ und die Pagination bleiben auf dem aufrufenden Controller.
   - Warum nicht in `PurchaseProposalsController`: die Parallel-Spec baut den gerade um.
   - Warum nicht eine Aktion im `PurchaseOrdersController`: dessen Class-Filter (StockRead + WareneingangBestellungAktiv) müsste auf Action-Ebene
     wandern. Damit änderte sich die Absicherung von `Receive` und `SageWe`, die F2.4 gerade unverändert lassen soll.
3. **Liefertermin-Filter getrennt anwenden:** `columnFilters` aus `ColumnFilterHelper.ReadFromQuery` kommt ohne den Key `liefertermin` in
   `ColumnFilterHelper.Apply` (`:108-111`). Der Liefertermin-Filter wird danach nur auf nicht überfällige Zeilen mit Termin angewendet (F4.2).
   - Dabei wird die bestehende Prüfung des Filter-Kerns (ADR 0017) genutzt (`ColumnFilterSyntax`, dieselbe Grammatik wie `Apply`), kein zweiter
     Parser.
   - Ist im Kern nur `Apply` auf Listen öffentlich, wird `Apply` auf die Teilmenge „nicht überfällig mit Termin“ angewendet und mit
     „ohne Termin“ + „überfällig (falls Checkbox)“ vereinigt. Die Reihenfolge stellt die bestehende Sortierung (`:108`) danach wieder her.
4. **View:**
   - Wochen-Feld raus.
   - Checkbox „Überfällige anzeigen“ (`asp-for`-Muster mit Hidden-False, damit „aus“ ankommt) neben „nur überfällig“.
   - Hinweiszeile F4.4 im Stil von `:85-92`.
   - Stand-Zeile ohne „bis …“.
   - Liefertermin-Zelle mit KW (F3.1).
   - Überschrift/Breadcrumb je Ansicht („Lager → …“ bzw. „Einkauf → …“).
   - `view-config` viewKey bleibt „PurchaseOrders“ für die Spalten-Prefs beider Ansichten. Gleiche Spalten, ein Satz Spaltenwahl je Benutzer, keine
     ColumnDefinitions-Änderung.
5. **Layout:** zwei Menüeinträge im Einkauf-Dropdown (`_Layout.cshtml:282-284`): „Offene Bestellungen“ und „Sage-Wareneingänge (offen)“;
   active-Prüfung (`:279`) erweitert um Controller „EinkaufPurchaseOrders“. Sichtbar nur mit `showEinkauf` (EinkaufAktiv + einkauf/admin, `:85-87`).
6. **Umleitungstext** von `RequireEinkaufAktiv` (`RequireEinkaufAktivAttribute.cs:33`) lautet heute „Einkauf – Bestellvorschläge ist deaktiviert
   (Einstellung „Einkauf aktiv“).“. Er wird zu „Einkauf ist deaktiviert (Einstellung „Einkauf aktiv“).“ (Antwort 8).
7. ~~**AppSetting entfernen** (F3.4), Tests anpassen.~~ ueberholt (Antwort 4). Stattdessen: Anzeigename, Wirkung und Verwendung von
   `OffeneBestellungenVorschauWochen` in `AppSettingDefinitions.cs:128-130` neu (Text siehe „Einstellungen“); Key, Default, Bereich, Gruppe `GLager`,
   `seed: false` bleiben. Der Controller liest den Wert nur noch für die Vorbelegung (F3.3).
8. **ServiceSetting-Anzeige** `Sync:EinkaufBestellungenEnabled` (`ServiceSettingDefinitions.cs:120`): Verwendung um „Einkauf → Offene Bestellungen“
   ergänzen.

### Muster

- Repository-Pattern (ADR 0001): `IUserViewPreferenceRepository`.
- Listen-View-Pattern (ADR 0005): Pagination, Filterkarte und Server-Spaltenfilter bleiben; die neue Ansicht nutzt dieselbe View.
- Read/Edit-Split und Feature-Gate kumulativ (ADR 0006/0011).
- Aktivitäts-Protokoll (ADR 0010), ServiceSettings-Katalog (ADR 0008), Einstellungen (ADR 0020), Lokalzeit (ADR 0025).

### Kollisionen mit der Parallel-Spec [[2026-11-16-bestellvorschlaege-nachbesserung-spec]] (gleicher Worktree)

| Datei | Parallel-Spec | Diese Spec | Umgang |
|---|---|---|---|
| `SyncWorker.cs` | neuer Schritt „Letzte Bestellung (Einkauf)“ (schon da, `:463-482`) | nur Sperre im Service, `SyncWorker` selbst unverändert | keine Textkollision erwartet |
| `ServiceSettingDefinitions.cs` | neue Keys | ein neuer Key | nach deren Commit rebasen, Einträge getrennt anfügen |
| `AppSettingDefinitions.cs` / `AppSettingKeys.cs` | ggf. neue Keys | Texte eines Keys geändert (~~ein Key entfernt~~ ueberholt, Antwort 4) | dito |
| `_Layout.cshtml` Einkauf-Dropdown, `RoleOverview.cshtml`, `controller.md` | ggf. Text Einkauf | Menüeintrag, Rollen-Text | Text am Ende gemeinsam formulieren |
| `nachlauf.psd1`, `Changelog.cshtml`, `TESTSZENARIEN.md`, `docs/betrieb/04` | eigene Einträge 1.99.30 / TS-154 (committet, `54f04740`) | 1.99.31 / TS-155 | Reihenfolge der Versionen einhalten |
| `SagePurchaseOrderLines.Liefertermin` | F5 liest ihn | nur Anzeige/Filter | kein Konflikt |

Empfehlung: diese Spec **nach** dem Commit der Bestellvorschläge-Nachbesserung im selben Worktree bauen. Stand 2026-11-17: Die Nachbesserung ist
feature-complete committet (`fd9f49db`, QA-Manifest `54f04740`, v1.99.30); die Bedingung ist damit erfüllt.

## Migrations-/SQL-Auswirkungen

- Keine EF-Migration, kein `SQL/XX_*.sql`, keine Änderung an `00_FreshInstall.sql`. Alle Daten liegen in bestehenden Tabellen
  (`UserViewPreferences`, `ServiceSettings`).
- Die neue ServiceSetting braucht keinen Seed. Fehlt die Zeile, gilt der Katalog-Default (ADR 0008).
- `OffeneBestellungenVorschauWochen` bleibt (Antwort 4); ein gespeicherter Wert in `dbo.AppSettings` gilt weiter. Kein Daten- oder
  Reparaturskript. ~~„Die entfernte AppSetting … wird über eine Manifest-Prüfung mit Reparatur entfernt“~~ ueberholt.
- `SQL/AgentJobs/`: nicht betroffen. Weder Pflichtfeld noch Folge-MERGE ändern sich.

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `Sync:EinkaufBestellungenNachWeMinuten` (neu, ServiceSetting, Kategorie „Sage-Abgleich (SData)“, Anzeigename „Offene Bestellungen nach Sage-Wareneingang neu lesen: Mindestabstand“) | Int | 0–60 min (Bereich, Server validiert); 0 = aus | 2 | Nach mindestens einer erfolgreichen Sage-WE-Übergabe liest der Dienst die offenen Bestellungen sofort neu, danach höchstens einmal je n Minuten. Gates wie der reguläre Lauf (`Sync:EinkaufBestellungenEnabled`, DryRun). | Dienst: Sage-Buchungs-Worker, Lauf „Sage-Wareneingang“ → Lauf „Offene Bestellungen (Sage)“ | AKE (IDEAL ohne Wirkung, solange Sage-WE dort aus ist) | Kein Schreiben nach Sage. Hinweis im Beschreibungstext: „Jeder Lauf liest alle offenen Bestellungen aus Sage; in der Zeit wartet die nächste Sage-Lagerbuchung.“ |
| `OffeneBestellungenVorschauWochen` (AppSetting, **bleibt, Texte geändert**, Gruppe Lager, kein Seed; Anzeigename neu „Offene Bestellungen: Vorbelegung Liefertermin (Wochen ab aktueller KW)“) | Int | 1–26 Wochen (Bereich, Server validiert wie heute `AppSettingDefinitions.cs:130`) | 2 | Belegt beim ersten Aufruf den Spaltenfilter Liefertermin mit dem Datumsbereich Montag der aktuellen KW bis Sonntag der N-ten KW vor (2 = aktuelle + nächste KW, 1 = nur aktuelle KW). Überfällige folgen der Checkbox „Überfällige anzeigen“, Positionen ohne Termin sind immer sichtbar. Andere Zeiträume über den Spaltenfilter. | Lager → Offene Bestellungen und Einkauf → Offene Bestellungen (Spaltenfilter Liefertermin vorbelegt) | beide | keiner (nur Anzeige) |
| ~~`OffeneBestellungenVorschauWochen` (AppSetting, entfernt)~~ | ueberholt durch Antwort 4 | | | | | | |

Die Checkbox-Präferenz (F4.5) ist keine Einstellung im Sinn von ADR 0020. Sie ist eine Benutzer-Ansichtspräferenz (`UserViewPreferences`).

## Audit-Feld-Auswirkungen

- `UserViewPreference` ist `AuditableEntity`. `UserViewPreferenceRepository.SaveAsync` setzt `Created*`/`Modified*` bereits. Der Controller übergibt
  Benutzer und Windows-Benutzer aus `ICurrentUserService` wie im Muster `WarehousePickingController.cs:380-381`.
- `SagePurchaseOrderLines` ist keine AuditableEntity (`ApplicationDbContext.cs:1411`). Der Anstoß ändert daran nichts. `SyncedAt` setzt der Lauf
  wie bisher (`EinkaufBestellungenSyncService.cs:69`).
- Keine weiteren Schreibwege.

## Akzeptanzkriterien

1. Nach einer erfolgreichen Sage-WE-Übergabe steht im Aktivitäts-Protokoll binnen eines Takts (≤ 20 s plus Laufzeit) ein Lauf
   „Offene Bestellungen (Sage)“ mit der Info „Anlass: Sage-Wareneingang (n Position(en) übergeben)“.
2. Eine zweite Übergabe innerhalb von 2 Minuten nach dem Anstoß löst keinen sofortigen Lauf aus. Genau ein Lauf folgt im ersten Takt nach Ablauf
   der 2 Minuten.
3. Mit `Sync:EinkaufBestellungenNachWeMinuten = 0` folgt auf eine Übergabe kein Anstoß; der 15-Minuten-Zyklus läuft unverändert.
4. Mit `Sync:EinkaufBestellungenEnabled = aus` folgt kein Anstoß, auch nicht später nach dem Einschalten.
5. Ein Sage-WE-Takt ohne Übergabe (nichts offen, nur Fehler, Zugang fehlt) löst keinen Anstoß aus.
6. Scheitert der angestoßene Lauf, stehen „Run fehlgeschlagen …“ unter „Offene Bestellungen (Sage)“ und eine Warnung unter „Sage-Wareneingang“.
   Lagerbuchung, Sage-WE und WE-CSV laufen weiter, und es geht keine Fehlermail raus.
7. Angestoßener und regulärer Lauf überlappen nie (keine zwei „Run gestartet“ ohne dazwischenliegendes Ende). Ein durch die Sperre verschobener
   Anstoß läuft im nächsten freien Takt.
8. Der Anstoß versendet keine Lieferverzug-Mail.
9. `Sync:EinkaufBestellungenNachWeMinuten` steht unter /ServiceSettings mit Anzeigename, Wirkung, Verwendung, Standort und Bereich 0–60. Der Wert 61
   oder -1 wird beim Speichern abgelehnt.
10. Ein Benutzer mit Rolle einkauf und ohne Lager-Rolle sieht bei `EinkaufAktiv = an` im Menü Einkauf „Offene Bestellungen“. Die Seite zeigt die Liste
    ohne Knopf „Wareneingang“.
11. Derselbe Benutzer bekommt bei GET `/PurchaseOrders/Receive?…` und bei POST `/PurchaseOrders/Receive` AccessDenied, ohne Buchung.
12. Ein Benutzer mit einkauf und Lager-Schreibrecht sieht in der Einkauf-Ansicht den Knopf „Wareneingang“, sofern `WareneingangBestellungAktiv` an ist.
    Ist es aus, fehlt der Knopf.
13. Bei `EinkaufAktiv = aus` fehlt der Menüeintrag. Ein Direktaufruf leitet mit WarningMessage auf die Startseite um.
14. Ein Benutzer ohne einkauf/admin bekommt bei Direktaufruf der Einkauf-Ansicht AccessDenied.
15. Beide Ansichten zeigen beim Erstaufruf im Spaltenfilter Liefertermin `<Mo aktuelle KW>...<So der N-ten KW>` (`dd.MM.yyyy...dd.MM.yyyy`,
    N = `OffeneBestellungenVorschauWochen`, Default 2) und nur Positionen mit Termin in diesem Bereich, dazu die Positionen ohne Termin und die
    Überfälligen gemäß Checkbox. ~~`KW<aktuelle ISO-KW>`~~ ueberholt (Antwort 5).
15a. Mit `OffeneBestellungenVorschauWochen = 1` umfasst die Vorbelegung genau Montag bis Sonntag der aktuellen KW. In der KW 53/1 (Jahreswechsel)
    reicht der Bereich korrekt ins Folgejahr (Unit-Test mit festem Datum, z. B. Mi 30.12.2026 → `28.12.2026...10.01.2027` bei N = 2).
16. Jede Liefertermin-Zelle zeigt `dd.MM.yyyy KWnn`. Ein Klick auf eine andere KW im Kalender filtert auf diese KW.
17. Wer den Liefertermin-Filter leert, sieht alle Termine. Die Seite belegt dann nicht neu vor, auch nicht beim Blättern. „Zurücksetzen“ belegt neu
    vor.
18. Das Feld „Wochen“ der Filterkarte gibt es nicht mehr. Die AppSetting `OffeneBestellungenVorschauWochen` steht unter /Settings mit neuem
    Anzeigenamen „Offene Bestellungen: Vorbelegung Liefertermin (Wochen ab aktueller KW)“, Wirkung und Verwendung; 0 und 27 werden abgelehnt.
    ~~„… gibt es nicht mehr … kein verwaister Eintrag (nach Manifest-Reparatur)“~~ ueberholt (Antwort 4).
19. Lager-Ansicht, erster Aufruf eines Benutzers: Die Checkbox „Überfällige anzeigen“ ist aus, überfällige Positionen fehlen, und die Hinweiszeile
    nennt ihre Anzahl. Einkauf-Ansicht: Die Checkbox ist an, Überfällige aus allen Wochen sind sichtbar und rot mit Hinweis „überfällig“.
20. Ändert ein Benutzer die Checkbox und filtert, bleibt der Zustand beim nächsten Aufruf derselben Ansicht erhalten, auch nach Ab- und Anmelden. Die
    andere Ansicht behält ihren eigenen Zustand.
21. Positionen ohne Liefertermin sind in beiden Ansichten immer sichtbar, auch bei gesetztem Liefertermin-Filter und Checkbox aus (Antwort 2).
22. „nur überfällig“ zeigt ausschließlich überfällige Positionen, unabhängig von der Checkbox und vom KW-Filter.
23. Rest-Regel, Ausblenden von EBB mit Rest 0 und die Maske Wareneingang verhalten sich wie vor der Änderung (bestehende Tests bleiben grün).
24. Ein Benutzer mit Rolle einkauf (ohne Lager-Rolle) sieht bei `EinkaufAktiv = an` im Menü Einkauf „Sage-Wareneingänge (offen)“. Die Liste zeigt
    nur Wareneingänge im Status offen (wird gesendet), gesendet, Fehler, Klären, nur in Sage buchen; „an Sage übergeben“ fehlt, auch bei
    `?status=Uebergeben` per URL (leere Liste). Die Statusauswahl bietet genau diese fünf Werte. Es gibt keine Aktionsknöpfe und keinen Knopf
    „Wareneingang zu Bestellung“.
25. Derselbe Benutzer bekommt bei direktem POST auf `/PurchaseOrders/SageWeErneutSenden`, `/PurchaseOrders/SageWeAlsUebergeben` und
    `/PurchaseOrders/CsvErneutSchreiben` AccessDenied; der Status der Wareneingänge bleibt unverändert.
26. Ein Benutzer mit einkauf und Lager-Keyuser-Recht (stock_keyuser) sieht in der Einkauf-Liste bei Fehler/Klären „Erneut senden“ und „Als übergeben
    markieren“, sofern `WareneingangBestellungAktiv` an ist; die Aktion wirkt und führt auf Lager → Sage-Wareneingänge zurück. Mit
    `WareneingangBestellungAktiv` aus fehlen die Knöpfe.
27. Lager → Sage-Wareneingänge zeigt unverändert alle Status außer „nicht nötig“, inklusive „an Sage übergeben“ (bestehende Tests
    `SageWareneingaengeListeTests` grün).
28. Bei `EinkaufAktiv = aus` fehlt auch „Sage-Wareneingänge (offen)“ im Menü; Direktaufruf `/EinkaufPurchaseOrders/SageWareneingaenge` leitet mit
    „Einkauf ist deaktiviert (Einstellung „Einkauf aktiv“).“ auf die Startseite um. Benutzer ohne einkauf/admin → AccessDenied.
29. Build und alle Tests grün. Drift-Guards (`ServiceSettingDefinitionsTests`, `AppSettingDefinitionsTests`, `NachlaufManifestDriftTests`) sind grün.

## Test-Szenarien

Neues Kapitel **TS-155 „Offene Bestellungen: sofort neu lesen, Einkauf-Ansicht, KW-Filter, Überfällige“** in `docs/TESTSZENARIEN.md`, danach
`secondbrain/tests/testszenarien-index.md` nachziehen. Standort: AKE-DEV für TS-155.1–155.4 und TS-155.11 (Sage-WE nur AKE), beide für
TS-155.5–155.10. Kapitelüberschrift im Dokument: „TS-155 — Offene Bestellungen: sofort neu lesen, Einkauf-Ansichten, Datumsvorbelegung,
Überfällige (v1.99.31)“.

- **TS-155.1 Anstoß nach Übergabe** – Vorbedingungen: `SageWareneingangBelegAktiv` an, `Sync:EinkaufBestellungenEnabled` an, Einstellung 2.
  - Schritte: Wareneingang zu einer EBB-Position buchen und warten, bis der Status „übergeben“ ist. Dann Aktivitäts-Protokoll öffnen und
    Offene Bestellungen neu laden.
  - Erwartet: Lauf „Offene Bestellungen (Sage)“ mit „Anlass: Sage-Wareneingang (1 Position(en) übergeben)“; „Stand“ springt auf die Uhrzeit des
    Anstoßes, die voll gelieferte Position ist weg.
  - Beobachtung (Antwort 1): Laufzeit des angestoßenen Laufs notieren und P-1 ausführen; Median > 30 s im Testbericht vermerken (Anlass für
    Variante B als eigene Spec, kein Abnahmehindernis).
- **TS-155.2 Entprellung** – Zwei Wareneingänge im Abstand von etwa 30 s buchen.
  - Erwartet: Ein Anstoß nach dem ersten, keiner nach dem zweiten innerhalb von 2 min. Genau ein weiterer Anstoß etwa 2 min nach dem ersten.
  - Negativ: kein dritter Lauf ohne neue Übergabe.
- **TS-155.3 0 = aus und Gate aus** – (a) Einstellung 0, Wareneingang buchen → kein Anstoß, der nächste reguläre Lauf ändert den Stand.
  (b) `Sync:EinkaufBestellungenEnabled` aus, Wareneingang buchen, nach 3 min wieder an → kein Anstoß-Lauf mit „Anlass“.
  (c) 61 und -1 speichern → Ablehnung.
- **TS-155.4 Fehler im Anstoß** – SData-Fehler erzwingen (auf DEV z. B. `SData:BaseUrl` vorübergehend auf einen falschen Port; **nur DEV**, Wert
  danach zurück). Dann Wareneingang buchen.
  - Erwartet: „Run fehlgeschlagen“ unter Offene Bestellungen (Sage), Warnung unter Sage-Wareneingang, keine Fehlermail, keine Lieferverzug-Mail.
    Die nächsten Takte der Lagerbuchung laufen weiter, und nach Korrektur bringt der reguläre Lauf den Stand nach.
  - Negativ: Der Cache ist danach nicht leer (Empty-Guard / Transaktion).
- **TS-155.5 Einkauf-Ansicht lesend** – Benutzer nur mit Rolle einkauf, `EinkaufAktiv` an.
  - Erwartet: Menü Einkauf → Offene Bestellungen sichtbar, Liste wie im Lager, kein Knopf „Wareneingang“; Menü Lager fehlt.
  - Negativ: `/PurchaseOrders/Receive?belPosId=…` per URL → AccessDenied; POST per Browser-Konsole oder Testwerkzeug → AccessDenied, keine
    Bewegung. `/PurchaseOrders` (Lager-Ansicht) → AccessDenied, weil kein Lager-Lesen.
- **TS-155.6 Einkauf mit Lager-Recht** – Rollen einkauf und lager.
  - Erwartet: Knopf „Wareneingang“ in der Einkauf-Ansicht, Buchung möglich. Mit `WareneingangBestellungAktiv` aus fehlt der Knopf.
- **TS-155.7 Gates** – `EinkaufAktiv` aus → kein Menüeintrag, Direktaufruf leitet mit Warnung zur Startseite. Benutzer ohne einkauf → AccessDenied.
  Admin → Zugriff.
- **TS-155.8 Datumsvorbelegung** – Beide Ansichten frisch öffnen (Einstellung `OffeneBestellungenVorschauWochen` = 2).
  - Erwartet: Spaltenfilter Liefertermin = `<Mo aktuelle KW>...<So nächste KW>` (z. B. am 17.11.2026 `16.11.2026...29.11.2026`); nur Termine in
    diesem Bereich plus „ohne Termin“ plus Überfällige gemäß Checkbox. Zellen mit `dd.MM.yyyy KWnn`.
  - Weiter: Einstellung unter /Settings auf 1 → neu öffnen (Zurücksetzen). Erwartet: Bereich nur aktuelle KW.
  - Weiter: Kalender → andere KW klicken. Erwartet: deren Positionen.
  - Weiter: Filter leeren und blättern. Erwartet: alle Termine, keine erneute Vorbelegung. „Zurücksetzen“ → wieder vorbelegter Bereich.
  - Negativ: kein Feld „Wochen“ in der Filterkarte; /Settings: 0 und 27 werden abgelehnt; Anzeigename „… Vorbelegung Liefertermin (Wochen ab
    aktueller KW)“.
- **TS-155.9 Überfällige** – Lager: Checkbox aus, Hinweiszeile „n überfällige Position(en) ausgeblendet“, n stimmt mit dem Lauf „nur überfällig“
  überein. Checkbox an → Überfällige aller Wochen rot mit Hinweis, Hinweiszeile weg.
  - Abmelden und wieder anmelden: Der Zustand bleibt. Die Einkauf-Ansicht steht unabhängig davon auf an.
  - In Users/Edit die Präferenz zurücksetzen → wieder Default.
  - Negativ: Überfällige der laufenden KW (vor heute) sind bei Checkbox aus nicht sichtbar.
- **TS-155.10 Unverändert** – Rest-Regel: Teilmenge buchen → Rest sinkt sofort, Position bleibt bis zum Abgleich. Rahmenabruf-Verhalten wie
  TS-Kapitel Wareneingang Teil 3.
- **TS-155.11 Sage-Wareneingänge (offen) im Einkauf** – Vorbedingungen: AKE-DEV, `EinkaufAktiv` an, Wareneingänge in mehreren Status vorhanden
  (mindestens einer „an Sage übergeben“ und einer „Fehler“ oder „Klären“; auf DEV z. B. per falschem `SData:BaseUrl` einen Fehler erzeugen, Wert
  danach zurück).
  - (a) Benutzer nur einkauf: Einkauf → Sage-Wareneingänge (offen). Erwartet: nur offen/gesendet/Fehler/Klären/nur in Sage buchen, kein
    „an Sage übergeben“; Statusauswahl mit genau diesen fünf Werten; keine Aktionsknöpfe, kein Knopf „Wareneingang zu Bestellung“.
    Negativ: `?status=Uebergeben` → leere Liste. POST „Erneut senden“ per Testwerkzeug mit gültigem Token → AccessDenied, Status unverändert.
  - (b) Benutzer einkauf + stock_keyuser: Knöpfe „Erneut senden“/„Als übergeben markieren“ bei Fehler/Klären sichtbar; „Erneut senden“ →
    Erfolgsmeldung in Lager → Sage-Wareneingänge, Eintrag danach „offen“. Mit `WareneingangBestellungAktiv` aus fehlen die Knöpfe.
  - (c) Vergleich: Lager → Sage-Wareneingänge zeigt weiterhin auch „an Sage übergeben“.
  - (d) `EinkaufAktiv` aus → Menüeintrag fehlt, Direktaufruf leitet mit „Einkauf ist deaktiviert …“ um.

**Automatische Tests (InMemory/Moq)**
- `SageWareneingangService`: Rückgabe = Anzahl übergeben (Erfolg, „in Sage gefunden“, nichts offen = 0, Wurf).
- Anstoß-Logik: Die Entscheidung (offen, Minuten, Gate, letzter Anstoß, jetzt) → laufen ja/nein liegt als kleine statische Funktion
  (`internal static bool AnstossFaellig(...)`), damit sie ohne Worker-Schleife testbar ist. Fälle: erster sofort, im Fenster nein, nach Fenster ja,
  0 = nein, Gate aus = nein und verwirft.
- `EinkaufBestellungenSyncService`: Sperre belegt → `TryRunExclusiveAsync` = false ohne Lauf; Anlass-Zeile geschrieben
  (bestehendes `EinkaufBestellungenSyncTests`-Muster).
- `PurchaseOrdersControllerTests` (bestehende Wochen-Tests `:38`, `:179` ersetzen):
  - Redirect mit `colf_liefertermin=<Mo>...<So>` + `kw=1` beim Erstaufruf (N = 1 und 2, Jahreswechsel mit festem Datum – die Datumsberechnung
    als `internal static` Funktion mit `heute`-Parameter, damit testbar); kein Redirect mit Merker. ~~`KWnn`~~ ueberholt (Antwort 5).
  - Sichtbarkeitsregel F4.2 (ohne Termin immer, überfällig nur mit Checkbox, KW-Filter nur auf nicht überfällige); Hinweiszahl.
  - Präferenz-Default je Ansicht, Präferenz gespeichert und gelesen.
  - CanReceive false ohne Lager-Recht oder ohne WareneingangBestellungAktiv.
- Neuer `EinkaufPurchaseOrdersController`: Attribute per Reflection (`RequireEinkaufAccess`, `RequireEinkaufAktiv`). `Receive` behält
  `RequireStockAccess` (Reflection-Test, damit niemand die Absicherung entfernt).
- Sage-WE-Liste `nurOffene` (Muster `SageWareneingaengeListeTests`): Uebergeben fehlt, die fünf offenen Status sind da, `status=Uebergeben` → leer,
  `nurOffene = false` unverändert; `CanErneutSenden` false ohne `CanTransferStockAsync` oder ohne `WareneingangBestellungAktiv`.
  `EinkaufPurchaseOrdersController.SageWareneingaenge` erbt die Class-Attribute (Reflection). Die bestehenden Reflection-Tests
  `SageWareneingaengeListeTests.cs:115/:195/:270` bleiben grün.
- Drift-Guards: ServiceSetting im Katalog, AppSetting-Bereich 1–26 weiter gültig (`PurchaseOrdersControllerTests.cs:179-182`), Manifest-Einträge
  gültig. ~~„AppSetting entfernt“~~ ueberholt.
- **Manual-UAT** (nicht InMemory-testbar): echte Sage-Übergabe und SData-Lauf (TS-155.1–155.4), echte Nebenläufigkeit beider Worker,
  Kalender-Klick im Browser, AccessDenied über die echte Filter-Pipeline.

## Etappen (nur bei epic: true)

Kein Epic, entfällt.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

> **QA-final 2026-11-17 (gegen den echten Diff `5b123c11..d3a00101`):** `IdealAkeWms/` geändert → **Web ja**; `IDEALAKEWMSService/` geändert →
> **Dienst ja**; keine neue Datei unter `*/Migrations/`, kein `SQL/`-Skript → **Migration nein**. Publish liegt fertig in
> `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` (v1.99.31, Commit `d3a00101`, `publish-info.txt`; Web + Dienst,
> `SQL\` und `deploy\` aktuell). Die dort liegende alte `publish\deploy.zip` NICHT verwenden. Reihenfolge: Update-Skript `deploy\Update-IdealAkeWms.ps1`
> (Web + Dienst gemeinsam), kein DB-Backup-Sonderfall (keine Migration), Nachlauf zeigt `v1.99.31-NachWeMinuten` (AKE), `v1.99.31-VorschauWochen`,
> `v1.99.31-EinkaufAnsichten`. Wer den Anstoß nicht will: `Sync:EinkaufBestellungenNachWeMinuten` vor dem Dienststart auf 0. Fluss: Publish aus dem
> Worktree → Testsystem → Test → Merge; nach dem Merge nur dann erneut aus `main` publishen, wenn der Merge getestete Dateien mit parallelen
> `main`-Änderungen zusammengeführt hat.

~~**Provisorisch, der QA-Lauf bestätigt gegen den echten Diff.**~~ ueberholt durch QA-final oben.

> **Abgleich mit dem Diff (Umsetzung 2026-11-17, `8b7d8252`..`9c623d4c`):** Web + Dienst ja, Migration nein – bestätigt. Manifest-Einträge
> `v1.99.31-NachWeMinuten`, `v1.99.31-VorschauWochen`, `v1.99.31-EinkaufAnsichten` und die Rollenbeschreibung in `Pruefung/RolleEinkauf`
> wie unten eingetragen (ASCII, `NachlaufManifestDriftTests` grün), `Quelle='2026-11-17-v1-99-31-offene-bestellungen-sofort-neu-lesen'`.
> Zusätzlich im Diff: `IdealAkeWms/Program.cs` (`AddScoped<PurchaseOrdersController>`), `RequireEinkaufAktivAttribute` (Text „Einkauf ist
> deaktiviert …“), `docs/anwender/08-bestellvorschlaege.md` + TS-136.1 (Text nachgezogen). **Abweichung:** Anlass-Zeile „n Beleg(e)
> übergeben“ statt „Position(en)“ (der Zähler `uebergeben` zählt Vorgänge). Screenshot `06-01` nicht erneuert (veraltet: Feld „Wochen“).

- **Web-App:** ja (Controller, View, Layout, RoleOverview, AppSetting-Katalog, Hilfe, Changelog).
- **Service:** ja (`SageBookingWorker`, `SageWareneingangService`, `EinkaufBestellungenSyncService`, ServiceSetting-Katalog liegt im Web-Projekt und
  wird vom Dienst mitgenutzt).
- **Migration:** nein.
- **Reihenfolge:** Web und Dienst gemeinsam (Update-Skript). Nach dem Start greift die neue Einstellung mit Default 2 sofort. Wer das nicht will,
  setzt sie vorher auf 0.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023). Kein Schema-, Daten- oder Einmal-Skript, daher keine Trockenlauf-Pflichten.
  Formate wie die vorhandenen `Typ='Einstellung'`-Einträge (z. B. `v1.99.4-SageWe`, `nachlauf.psd1:335-337`) und `Typ='Info'` (`:416-421`):
  - `@{ Id='v1.99.31-NachWeMinuten'; Version='1.99.31'; Typ='Einstellung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'`
    `Text='Neu: Sync:EinkaufBestellungenNachWeMinuten (/ServiceSettings, Default 2, 0 = aus). Nach einer Sage-Wareneingang-Uebergabe liest der Dienst die offenen Bestellungen sofort neu, danach hoechstens alle n Minuten. Wirkt nur mit SageWareneingangBelegAktiv und Sync:EinkaufBestellungenEnabled. Laufzeit im Aktivitaets-Protokoll beobachten (P-1); wer den Anstoss nicht will, vor dem Dienststart auf 0 setzen.'`
    `Quelle='<brain-changelog v1.99.31>' }`
  - `@{ Id='v1.99.31-VorschauWochen'; Version='1.99.31'; Typ='Einstellung'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'`
    `Text='OffeneBestellungenVorschauWochen (/Settings) bleibt, belegt jetzt den Spaltenfilter Liefertermin vor: Montag der aktuellen KW bis Sonntag der N-ten KW (2 = aktuelle + naechste KW). Das Feld Wochen in der Filterkarte entfaellt. Ein gespeicherter Wert gilt weiter.'`
    `Quelle='<brain-changelog v1.99.31>' }`
  - `@{ Id='v1.99.31-EinkaufAnsichten'; Version='1.99.31'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'`
    `Text='Rolle einkauf sieht jetzt Einkauf > Offene Bestellungen und Einkauf > Sage-Wareneingaenge (offen), beide nur lesen (Gate EinkaufAktiv). Wareneingang buchen nur mit Lager-Recht, Aktionen an Sage-Wareneingaengen nur mit Lager-Keyuser-Recht. Keine Rollenzuweisung noetig.'`
    `Quelle='<brain-changelog v1.99.31>' }`
  - `Pruefung/RolleEinkauf` (`:142-149`): Beschreibungstext im Reparatur-Insert (`:146`) auf
    `Einkauf - Bestellvorschlaege, Offene Bestellungen, offene Sage-Wareneingaenge (lesen).` anpassen. Id, Version, Pruefung bleiben; Wirkung nur
    bei fehlender Rolle.
  - ~~`Pruefung/OffeneBestellungenVorschauWochen` (Reparatur DELETE der Restzeile), `v1.99.32-EinkaufOffeneBestellungen`, `v1.99.32-NachWeMinuten`
    als `Typ='Info'`~~ ueberholt: Einstellung bleibt (Antwort 4), Version 1.99.31, `Typ='Einstellung'` ist im Manifest belegt.
- **Doku (Pflicht, Dateien belegt per Glob 2026-11-17):** `Views/Help/Changelog.cshtml` (Anwender, v1.99.31) und Hilfeseite (`Help/Index.cshtml:79-80`
  Text „Wochen ab aktueller KW“ statt „heute + X Wochen“; Abschnitt Offene Bestellungen um Einkauf-Menü, Datumsvorbelegung, Checkbox; Abschnitt
  Sage-Wareneingänge um die Einkauf-Ansicht „nur offene“). Dazu:
  - `docs/anwender/05-wareneingang-zu-bestellung.md` (Stand nach Übergabe aktualisiert sich binnen Minuten; Sage-Wareneingänge auch im Einkauf,
    nur offene, nur lesen)
  - `docs/anwender/06-offene-bestellungen-rahmenabrufe.md` (`:26-27`, `:38-43`, `:103`, `:109`, `:119` – Feld Wochen raus, Einstellung als
    Vorbelegung, Datumsbereich, Checkbox, Einkauf-Menü; Screenshot `06-01` veraltet → neu oder als veraltet kennzeichnen)
  - `docs/betrieb/04-konfiguration.md` (neue ServiceSetting neben `:159`; Zeile `:200-201` um die Einkauf-Ansichten; Beschreibung
    `OffeneBestellungenVorschauWochen` neu – ~~„AppSetting raus“~~ ueberholt)
  - `docs/betrieb/05-schnittstellen.md` (`:69` Schalterliste, Ablauf Anstoß, Sperre)
  - `README.md:522` (Text der AppSetting neu, nicht entfernen); Brain: `codebase/services.md` (Anstoß, Sperre), `codebase/controller.md` (F2.6), `feature-map.md`, `changelog/`,
    `fallstricke.md` (Eintrag: „KW-Filter wirkt als Text-Enthält, Zelle muss KWnn enthalten“, „Cache-Vollersatz nie parallel – Sperre“ und
    „View eines Controllers aus einem zweiten Controller rendern: Formulare auf fremde Aktionen brauchen asp-controller“).
- **Publish-Befehle** (im Worktree):
  - `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  - `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

### Prüfabfrage P-1 (Mensch, AKE-Test; IDEAL nur falls `Sync:EinkaufBestellungenEnabled` dort an)

Laufzeit des Laufs „Offene Bestellungen (Sage)“ der letzten 7 Tage (`SyncLogs.Service = 'EinkaufBestellungen'`, `SyncLogServices.cs:38`;
Start = „Run gestartet“ `SyncLogger.cs:21`, Ende = „Run erfolgreich beendet“/„Run fehlgeschlagen“ `SyncRun.cs:40`, `:51`):

```sql
SELECT COUNT(*) AS Laeufe, AVG(x.Sek) AS MittelSek, MAX(x.Sek) AS MaxSek
FROM (
  SELECT DATEDIFF(SECOND, s.Timestamp, e.Timestamp) AS Sek
  FROM dbo.SyncLogs s
  CROSS APPLY (SELECT TOP 1 e.Timestamp FROM dbo.SyncLogs e
               WHERE e.Service = s.Service AND e.Id > s.Id
                 AND (e.Message LIKE N'Run erfolgreich%' OR e.Message LIKE N'Run fehlgeschlagen%')
               ORDER BY e.Id) e
  WHERE s.Service = N'EinkaufBestellungen' AND s.Message = N'Run gestartet'
    AND s.Timestamp >= DATEADD(DAY, -7, SYSDATETIME())
) x;
```

Gibt es dort keine Läufe, liegt der Wert **nicht vor**. Dann die Abfrage am Produktivsystem AKE (lesend) ausführen. Ein Lauf auf akenet01:4445 hatte
1059 Positionen / 341 Köpfe (Testbericht [[2026-11-13-ake-regression-akenet-4445-browser-testbericht]], Zeile 176); eine Laufzeit nennt der Bericht
nicht. Die Aussage „Volllauf ≤ 30 s“ ist bis zum Ergebnis **unbelegt**.

## Offene Rueckfragen

1. **Laufzeit / Variante:** P-1 an AKE-Test (bzw. lesend an AKE-Prod) ausführen. Gilt bei Median > 30 s Variante B (Delta je BelId) statt des
   Volllaufs? *Empfehlung:* Variante A bauen; B nur bei gemessenem Median > 30 s. Bis 30 s ist die Verzögerung der Lagerbuchung je Anstoß tragbar,
   und der Anstoß kommt höchstens alle 2 min.
2. **Positionen ohne Liefertermin:** immer sichtbar und markiert oder dem KW-Filter bzw. der Checkbox unterworfen? *Empfehlung:* immer sichtbar
   (Hinweis „ohne Termin“). Heute schon so (`PurchaseOrdersController.cs:89`), Review-Befunde [[2026-11-14-review-befunde-offene-bestellungen]]
   („bleiben immer sichtbar“). Ein KW-Filter kann sie mangels Datum nie treffen, sie verschwänden sonst ganz.
3. **„Sage-Wareneingänge“ lesend im Einkauf?** *Empfehlung:* nein, vorerst. Der Sage-WE-Status je Position steht in Offene Bestellungen. Die Liste
   „Sage-Wareneingänge“ dient dem Klären/Erneut-Senden (Key-User-Lager, `PurchaseOrdersController.SageWe.cs:101-141`). Bei Bedarf eigene kleine
   Spec.
4. **`OffeneBestellungenVorschauWochen`:** entfernen oder umwidmen (z. B. als „Vorbelegung: Wochen ab heute“)? *Empfehlung:* entfernen. Die
   KW-Vorbelegung ersetzt sie fachlich; eine Restzeile räumt die Manifest-Reparatur.
5. **Form der Vorbelegung:** `KWnn` (wie Kalender-Klick, jahreslos, `KW4` trifft auch KW40–49) oder Datumsbereich `TT.MM.JJJJ...TT.MM.JJJJ` (Mo–So,
   jahresgenau)? *Empfehlung:* `KWnn`, wegen Einheitlichkeit mit allen Datumsspalten. Im Wochenausschnitt der offenen Bestellungen ist eine
   Fehltreffer-KW eines anderen Jahres selten. Bei KW1–5 trifft die Vorbelegung zusätzlich KW10–59; falls das stört, Bereich nur für KW < 10.
6. **„nur überfällig“:** beibehalten oder streichen? *Empfehlung:* beibehalten. Es bleibt der schnellste Weg zur Mahnliste, mit klarer Regel
   (F4.3).
7. **Fehler im Anstoß:** nur Aktivitäts-Protokoll oder zusätzlich Fehlermail? *Empfehlung:* nur Aktivitäts-Protokoll. Ein SData-Ausfall trifft
   auch den regulären Lauf, der über `RunResilientAsync` (`SyncWorker.cs:614-622`) mailt; zwei Mails je Störung wären Rauschen.
8. **Gate der Einkauf-Ansicht:** nur `EinkaufAktiv` oder zusätzlich `WareneingangBestellungAktiv`? Und Umleitungstext von `RequireEinkaufAktiv`
   allgemein fassen? *Empfehlung:* nur `EinkaufAktiv`, dann ist der Einkauf von der Lager-Freischaltung unabhängig, und der Wareneingang-Knopf
   prüft `WareneingangBestellungAktiv` ohnehin. Den Text auf „Einkauf ist deaktiviert …“ verallgemeinern.

*Neu aus dem Rumpf-Nachzug 2026-11-17 (nicht blockierend; gebaut wird jeweils die genannte Variante, bis der Mensch anders entscheidet):*

9. **„Nur Lager-Keyuser“ = bestehender Filter?** `RequireStockKeyUserAccess` prüft `CanTransferStockAsync` = admin, stock_keyuser **und picking**
   (`RequireStockKeyUserAccessAttribute.cs:23`, `CurrentUserService.cs:128-129`). Gebaut wird der Filter unverändert. Soll picking für die drei
   Sage-WE-Aktionen herausfallen? Das wäre ein eigener Filter (drei Rollen-Stellen) und wirkte auch in Lager → Sage-Wareneingänge.
10. **Widerspruch in Antwort 3: „CSV erneut schreiben“ in einer Liste nur offener Wareneingänge.** Eine CSV-Ausgangszeile entsteht nur für
    Vorgänge im Status Uebergeben (`WareneingangCsvService.cs:40-42`); genau die fehlen in der Einkauf-Liste. Die Aktion erscheint dort also nie.
    Gebaut wird: Bedingung unverändert, die Aktion bleibt praktisch nur in Lager → Sage-Wareneingänge wirksam. Oder sollen übergebene Vorgänge
    mit CSV-Fehler in der Einkauf-Liste zusätzlich erscheinen?
11. **Rücksprung nach einer Aktion aus der Einkauf-Liste.** Die Endpunkte leiten auf Lager → Sage-Wareneingänge um
    (`PurchaseOrdersController.SageWe.cs:109`, `:114`, `:128`, `:133`, `:146`). Gebaut wird das unverändert (Keyuser hat Lager-Lesen). Oder Rücksprung
    in die Einkauf-Liste (kleiner Rücksprung-Parameter)?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Laufzeit P-1 / Variante A oder B → **Gerald 2026-11-17 06:41 „passt“ (Empfehlung Koordination): Variante A (Volllauf, entprellt); B nur bei gemessenem Median > 30 s.**
2. Positionen ohne Liefertermin immer sichtbar? → **Gerald 2026-11-17 06:41 „passt“ (Empfehlung Koordination): ja, immer sichtbar mit Hinweis „ohne Termin“.**
3. „Sage-Wareneingänge“ lesend im Einkauf? → **Gerald 2026-11-17 06:43: ja, lesend im Menü Einkauf – aber nur die OFFENEN Wareneingänge (noch nicht an Sage übergeben: offen/gesendet, Fehler, Klären, nur in Sage buchen); bereits übergebene nicht. Aktionen „Erneut senden“, „Als übergeben markieren“, „CSV erneut schreiben“ nur für Lager-Keyuser, serverseitig geprüft.**
4. `OffeneBestellungenVorschauWochen` entfernen oder umwidmen? → **Gerald 2026-11-17 06:41: nicht entfernen – „2 Wochen voraus“ plus Checkbox „Überfällige anzeigen“ ist in Ordnung. Umsetzung: Einstellung bleibt als Vorbelegung des Datumsfilters „Wochen ab aktueller KW“ (Default 2 = aktuelle + nächste KW, 1 = nur aktuelle KW); der KW-/Kalenderfilter bleibt für andere Zeiträume.**
5. Vorbelegung `KWnn` oder Datumsbereich Mo–So? → **Gerald 2026-11-17 06:41 „passt“ (Koordination, abweichend vom spec-agent): Datumsbereich Mo der aktuellen KW bis So der letzten Vorschau-KW (jahresgenau), kein `KWnn`.**
6. „nur überfällig“ beibehalten? → **Gerald 2026-11-17 06:41 „passt“ (Empfehlung Koordination): beibehalten.**
7. Fehler im Anstoß: nur Protokoll oder auch Fehlermail? → **Gerald 2026-11-17 06:41 „passt“ (Empfehlung Koordination): nur Aktivitäts-Protokoll.**
8. Gate Einkauf-Ansicht nur `EinkaufAktiv`, Umleitungstext allgemein? → **Gerald 2026-11-17 06:41 „passt“ (Empfehlung Koordination): nur EinkaufAktiv, Text „Einkauf ist deaktiviert …“.**
9. „Nur Lager-Keyuser“ = bestehender Filter inkl. picking, oder einengen auf admin + stock_keyuser? →
10. „CSV erneut schreiben“ in der Einkauf-Liste nie sichtbar – akzeptiert, oder übergebene Vorgänge mit CSV-Fehler zusätzlich zeigen? →
11. Rücksprung nach Aktion: Lager → Sage-Wareneingänge (wie heute) oder Einkauf-Liste? →

## QA-Nachweis (qa-agent, 2026-11-17)

Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Review-Bereich `5b123c11..a7bc62d4` (8b7d8252, 2f84a012, 9c623d4c, a7bc62d4) plus QA-Commit
`d3a00101`. Ergebnis: **Testbereit** (Manual-UAT siehe Checkliste).

### 1. Build und Tests (HEAD a7bc62d4, nach QA-Commit d3a00101 wiederholt, gleiches Ergebnis)
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (bekannte, keine neuen aus dem Diff).
- `dotnet test`: IdealAkeWms.Tests **3013 bestanden / 1 übersprungen / 0 Fehler** (3014); IDEALAKEWMSService.Tests **682 / 0 / 0**.

### 2. Code-Review (selbst, in diesem Lauf)
Geprüft: Diff aller Code-Dateien (Dienst, Controller, Views, Layout, RoleOverview, Kataloge, Program.cs, Manifest) samt Umgebung
(`EinkaufBestellungenSyncService.RunCoreAsync` inkl. Empty-Guard/Transaktion, `SyncWorker.cs:484-495`, `SageSdataReadClient` Timeout-Pfad,
`_Pagination`, `Users/Edit`, `RequireWareneingangBestellungAktivFilter`). Defektklassen: kein Wert in `<script>`, kein `hidden`+`d-*`, keine
stille Rückfallzahl (unlesbare Präferenz → Default ist Absicht), keine parallelen Arrays, keine nur-Browser-Validierung (Bereich der neuen
ServiceSetting serverseitig über die Bereichstabelle), Regel „überfällig" an EINER Stelle (`Ueberfaellig`), Zelltext einmal (`LieferterminText`
für View und ColumnMap).

Fokus-Punkte:
- **Anstoß nur bei Übergaben > 0:** `RunOnceAsync` liefert `uebergeben` nur nach `FinishSuccessAsync`; früher Ausstieg = 0, Wurf = keine Rückgabe;
  Zugangsfehler-Zweig im Worker gibt 0. `AnstossEntscheidung` (Test-Theorie, 6 Fälle). OK.
- **Entprellung/Setting:** Key in `All`, Anzeige, Bereich 0–60 min, Warnhinweis; Drift-Test grün; auf DEV unter /ServiceSettings sichtbar (s. 6). OK.
- **Sperre/Empty-Guard:** statisches `SemaphoreSlim`, regulär `WaitAsync(ct)`, Anstoß `WaitAsync(0)`; Empty-Guard und Transaktion unverändert
  im gemeinsamen `RunCoreAsync`. Ein SData-Timeout wird vom Client in `SdataLeseException` gewandelt (kein OCE) → Warnpfad greift, keine
  20-s-Wiederholschleife. OK.
- **Anlass-Zeile:** erste Info nach `BeginRunAsync`. Wortlaut „n Beleg(e) übergeben" statt „Position(en)" (im Deploy-Abgleich dokumentierte
  Abweichung, fachlich richtig, weil `uebergeben` Vorgänge zählt). Akzeptiert.
- **Controller als DI-Dienst:** `EinkaufPurchaseOrdersController` setzt `_listen.ControllerContext`. `TempData`/`Url` des injizierten Controllers
  lösen über `HttpContext.RequestServices` auf (gleiche TempData-Instanz je Request), Redirects nennen den Controller ausdrücklich. Filter des
  `PurchaseOrdersController` laufen dabei nicht (gewollt); `ListeAsync`/`SageWeListeAsync` sind `internal` → keine Routen. `ViewData` des injizierten
  Controllers entsteht ohne MVC-Property-Aktivierung mit `EmptyModelMetadataProvider`; beide Views nutzen kein `asp-for`/`DisplayNameFor`, daher
  ohne Wirkung; auf DEV korrekt gerendert (Hinweis B-2).
- **Sage-WE im Einkauf:** Filter `OffeneSageWeStatus` nach dem Repository-Aufruf, auch bei `status=Uebergeben` (DEV: „Keine Eintraege"). Aktionen nur
  bei `CanTransferStockAsync && WareneingangBestellungAktiv`, Formulare mit `asp-controller="PurchaseOrders"` (DEV: POST landete auf
  `/PurchaseOrders/SageWeErneutSenden`, Rücksprung Lager-Liste). Serverseitige Absicherung unverändert. OK.
- **Datumsvorbelegung:** `ISOWeek`, Jahreswechsel-Theorie (30.12.2026 → `28.12.2026...10.01.2027`, 1.1.2027 → KW 53/2026), Clamp 1–26. OK.
- **Checkbox:** Lager aus / Einkauf an, eigener ViewKey, Hidden-False, `bool?`; Speichern nur bei Abweichung, Audit über `SaveAsync`
  (DEV: `CreatedBy`/`CreatedByWindows` gesetzt, bei Änderung `ModifiedAt`). Ohne Termin immer sichtbar. OK.
- **Listen-View-Muster:** Pagination/Filterkarte/Server-Spaltenfilter unverändert; Filterformular trägt `kw` und aktuelle `colf_*` mit. OK.
- **Rollen drei Stellen:** Attribut (wiederverwendet), `controller.md` (Zeilen `RequireEinkaufAccess`/`RequireEinkaufAktiv`), `RoleOverview.cshtml`. OK.
- **Manifest:** drei Einträge v1.99.31 und Rollenbeschreibung `Pruefung/RolleEinkauf`, ASCII, `NachlaufManifestDriftTests` grün. OK.

Befunde:
- **B-1 (behoben, QA-Commit `d3a00101`):** `controller.md:82` nennt für `EinkaufPurchaseOrdersController` „keine POST-Action, Wächter-Test"; einen
  solchen Test gab es nur für `PurchaseProposalsController`. Ergänzt in `PurchaseOrdersControllerTests.Einkauf_Attribute_UndReceiveBleibtGeschuetzt_F2_2_F2_4`:
  keine Action mit Nicht-GET-Methode am Einkauf-Controller.
- **B-2 (Hinweis, kein Fix):** `ViewData` des injizierten `PurchaseOrdersController` nutzt den `EmptyModelMetadataProvider` (Fallback im
  `Controller.ViewData`-Getter). Heute folgenlos; wer den beiden Views später `asp-for`/DataAnnotations-Anzeigen hinzufügt, muss das beachten.
- **B-3 (Beobachtung, nicht aus diesem Diff):** Lager → Sage-Wareneingänge zeigt eine TempData-Warnung doppelt (Layout-Banner und View-Banner),
  z. B. „Nicht eingereiht: Der Sage-Wareneingang ist ausgeschaltet …". Vorbestehend; ggf. eigener Backlog-Eintrag.
- Rückfragen 9–11 bleiben offen (nicht blockierend); gebaut ist jeweils die genannte Variante, auf DEV bestätigt (Rücksprung Lager-Liste).

### 3. Beweisart je Akzeptanzkriterium
| AK | Beweis |
|---|---|
| 1, 2, 3, 4, 6, 7, 8 | Teilautomatisiert (`AnstossEntscheidung`, Rückgabe `RunOnceAsync`, Sperre/Anlass in `EinkaufBestellungenSyncTests`); echte Übergabe, SData-Lauf, Nebenläufigkeit beider Worker: **Manual-UAT** (auf DEV gesperrt: `Sync:EinkaufBestellungenEnabled`, Sage-WE) |
| 5 | Automatisiert (Rückgabe 0 ohne Arbeit); Zugangsfehler-Zweig per Review |
| 9 | Katalog/Bereich automatisiert (Drift-Guard), Anzeige auf DEV geprüft; Ablehnung 61/-1 beim Speichern: **Manual-UAT** |
| 10, 12, 24, 26 | Automatisiert (`CanReceive`/`CanErneutSenden`) + DEV-Browser (s. 6) |
| 11, 14, 25 | Reflection-Tests; GET über die echte Filter-Pipeline auf DEV (11); POST-AccessDenied (11/25) und 14: **Manual-UAT** |
| 13, 28 | Filtertext automatisiert; DEV IDEAL (EinkaufAktiv aus): Umleitung mit Warnung, kein Menü |
| 15, 15a, 17 | Automatisiert (Vorbelegung inkl. KW 53, Redirect/Merker) + DEV |
| 16 | Zelltext/Filter automatisiert, auf DEV per URL `colf_liefertermin=KW48`; Kalender-Klick: **Manual-UAT** |
| 18 | Feld „Wochen" fehlt (Test + DEV); 0/27 Bereich durch bestehenden Test; Anzeige /Settings: **Manual-UAT** |
| 19, 20, 21, 22 | Automatisiert + DEV (Defaults, Speicherung, Hinweiszeile); Ab-/Anmelden: **Manual-UAT** |
| 23, 27, 29 | Bestehende Tests grün + DEV (Lager-Liste zeigt „an Sage übergeben") |

### 4. Geänderte bestehende Tests
- `PurchaseOrdersReceiveTests`, `SageWareneingaengeListeTests` (Ctor-Parameter `UserViewPreferenceRepository`): **nur Typ/Compile-Fix**.
- `PurchaseOrdersControllerTests.Vorschau_HorizontXWochen_…_AK7` ersetzt durch `Erstaufruf_RedirectMitVorbelegungUndMerker_…`/`Sichtbarkeit_…`:
  **Verhalten bewusst geändert** (F3.4/F4, AK 15/17/18/19), im Test kommentiert.
- `PurchaseOrdersControllerTests` Rahmenabruf-Test: Query `?kw=1&colf_hinweis=…` statt `?colf_hinweis=…`: **bewusst** (F3.3, sonst Redirect beim Erstaufruf).
- `SageWareneingangServiceTests` (Senden): Assertion auf Rückgabe ergänzt: **Verhalten erweitert** (F1.1). Kein „Sonstiges".

### 5. Checkliste CLAUDE.md / ADR 0020
Keine Migration/kein SQL (richtig, nur bestehende Tabellen); FreshInstall/AgentJobs nicht betroffen; Audit über `UserViewPreferenceRepository.SaveAsync`
(DEV belegt); Version 1.99.31 in beiden `AppVersion.cs`, Anwender-Changelog ergänzt; `docs/TESTSZENARIEN.md` TS-155 und Index-Zeile 155 im Brain vorhanden.
ADR 0020: `Sync:EinkaufBestellungenNachWeMinuten` mit Bereich 0–60 (Server), Anzeigename, Wirkung, Verwendung, Default 2, Standort, Warnhinweis;
`OffeneBestellungenVorschauWochen` Texte neu, Bereich 1–26 unverändert. Kein Mangel. Manifest-Einträge vorhanden (s. Deploy).

### 6. DEV-Deploy und Testlauf
- Standort `beide` (Frontmatter). Deploy erhöht, beide Komponenten, Quelle Worktree HEAD `a7bc62d4`:
  - AKE: `dev-deploy.ps1 -Standort AKE` → „HTTP 200, Fusszeile v1.99.31 … Fertig: AKE v1.99.31 auf http://localhost:8081/", Exit 0.
  - IDEAL: `dev-deploy.ps1 -Standort IDEAL` → „HTTP 200, Fusszeile v1.99.31 … Fertig: IDEAL v1.99.31 auf http://localhost:8082/", Exit 0.
  - Dienst-Logs `D:\Dev\{AKE,IDEAL}\Service\logs\service-20261117.log`: Worker „Version 1.99.31", keine Fehler. QA-Commit `d3a00101` ändert nur
    einen Test; DEV-Stand fachlich identisch.
- Keine Einstellung auf DEV geändert. Temporäre Testdaten, alle zurückgesetzt und belegt: `UserRoles` von Benutzer 14 (admin entfernt, später
  stock_keyuser ergänzt; Endstand wieder `40 admin`, `81 einkauf`), `PurchaseOrderReceipts.Id=10` kurz auf Klären (Endstand 3, ModifiedAt/By unverändert),
  QA-Präferenzzeile `PurchaseOrders.Ueberfaellige` gelöscht. Nichts an Sage: „Erneut senden" wurde bei ausgeschaltetem Sage-WE abgewiesen.

| Schritt | Ergebnis | Status |
|---|---|---|
| TS-155.1–155.4 Anstoß/Entprellung/0/Fehler | braucht `Sync:EinkaufBestellungenEnabled` und eine echte Sage-WE-Übergabe | NICHT MOEGLICH (DEV-Sperre), Unit-Tests s. 3 |
| 155.5 nur einkauf (AKE) | Menü nur „Einkauf" (Bestellvorschläge, Offene Bestellungen, Sage-Wareneingänge (offen)); Liste ohne Knopf „Wareneingang"; `/PurchaseOrders/Receive?belId=361998` und `/PurchaseOrders` → „Zugriff verweigert" | OK |
| 155.5 POST Receive per Werkzeug | kein Skript-Werkzeug im Lauf | NICHT MOEGLICH |
| 155.6 einkauf + Lager-Recht | als admin: Knopf „Wareneingang zu Bestellung" in der Einkauf-Ansicht (Gate an); Buchung nicht ausgeführt | OK (Buchen offen) |
| 155.7 Gates | IDEAL (EinkaufAktiv aus): kein Menü Einkauf, `/EinkaufPurchaseOrders/SageWareneingaenge` → Startseite mit „Einkauf ist deaktiviert (Einstellung „Einkauf aktiv")." | OK |
| 155.8 Vorbelegung | AKE + IDEAL, Lager + Einkauf: Redirect auf `colf_liefertermin=16.11.2026...29.11.2026&kw=1`; Zellen „23.11.2026 KW48"; `?kw=1` (geleert) → 1165 Zeilen (= 1127 + 38 spätere, SQL-Gegenprobe), auch Seite 2 ohne neue Vorbelegung; `colf_liefertermin=KW48` → genau die KW-48-Zeile; kein Feld „Wochen" | OK |
| 155.8 /Settings auf 1, Kalender-Klick | nicht ausgeführt (Einstellung bleibt; Klick = Manual-UAT) | NICHT MOEGLICH |
| 155.9 Überfällige | Lager Default aus: „1126 überfällige Position(en) ausgeblendet", 1 Zeile im Bereich (SQL: 1 im Bereich, 1128 überfällig, davon 2 als erfasste Rahmenabrufe ausgeblendet); IDEAL „1021 … ausgeblendet"; Checkbox an → Hinweis weg, 1127 Zeilen, Präferenz `{"an":true}` gespeichert (CreatedBy/Windows gesetzt), neuer Aufruf ohne Parameter behält „an", Seite 2 = 26–50 von 1127; Einkauf Default an | OK |
| 155.9 Ab-/Anmelden, Users/Edit-Reset | nicht ausgeführt | NICHT MOEGLICH (Manual-UAT) |
| 155.10 Rest-Regel | nur Regression per Tests | NICHT MOEGLICH (Buchung) |
| 155.11 (a) nur einkauf | Statusauswahl genau 5 Werte; Klären-Eintrag ohne Aktionen und ohne „Wareneingang zu Bestellung"; `?status=Uebergeben` → „Keine Eintraege" | OK |
| 155.11 (a) POST mit Token | kein Werkzeug | NICHT MOEGLICH |
| 155.11 (b) einkauf + stock_keyuser | „Erneut senden"/„Als übergeben markieren" sichtbar; als admin „Erneut senden" geklickt → POST auf PurchaseOrders, Rücksprung Lager → Sage-Wareneingänge, „Nicht eingereiht: Sage-Wareneingang ausgeschaltet", Status unverändert | OK (Erfolgsfall mit Sage-WE an: Manual-UAT) |
| 155.11 (c) Lager-Liste | zeigt weiter „an Sage übergeben" | OK |
| 155.11 (d) EinkaufAktiv aus | s. 155.7 | OK |
| ServiceSettings-Anzeige | `Sync:EinkaufBestellungenNachWeMinuten` mit Name, Wirkung, Verwendung, Standort, Warnhinweis „Achtung", Wert 2 min | OK |

### 7. Publish
`C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` neu erzeugt (Zielordner vorher geleert): `IDEALAKEWMSWeb`,
`IDEALAKEWMSWebService` (beide Exit 0), `SQL\`, `deploy\`, `publish-info.txt` = Version 1.99.31, Commit `d3a00101d0c9124d2541da7d66ece6a17255f616`,
Branch `feature/2026-08-07-ideal-teile-1-5`, Erstellt 2026-11-17 07:24. Die alte `publish\deploy.zip` liegt noch dort; nicht verwenden.

## Manuelle Test-Checkliste (Mensch, offen)

1. Testsystem AKE: Publish v1.99.31 per `deploy\Update-IdealAkeWms.ps1` einspielen, Nachlauf-Einträge v1.99.31 lesen.
2. TS-155.1 echte Sage-WE-Übergabe → Lauf „Offene Bestellungen (Sage)" mit „Anlass: Sage-Wareneingang (n Beleg(e) übergeben)", Position verschwindet; Laufzeit und P-1 notieren.
3. TS-155.2 Entprellung (zwei Übergaben im Abstand von etwa 30 s, genau ein Nachlauf nach 2 min).
4. TS-155.3 (a) Einstellung 0, (b) Gate aus/an, (c) 61 und -1 unter /ServiceSettings speichern → Ablehnung.
5. TS-155.4 Fehler im Anstoß (nur Testsystem/DEV mit falschem `SData:BaseUrl`, danach zurück) → Warnung unter Sage-Wareneingang, Cache nicht leer, keine Mail.
6. TS-155.5 Benutzer nur einkauf: POST `/PurchaseOrders/Receive` per Werkzeug → AccessDenied, keine Bewegung.
7. TS-155.6 einkauf + Lager-Recht: Wareneingang aus der Einkauf-Ansicht buchen; mit `WareneingangBestellungAktiv` aus fehlt der Knopf.
8. TS-155.8 /Settings `OffeneBestellungenVorschauWochen` = 1 → Zurücksetzen → nur aktuelle KW; 0 und 27 abgelehnt; Kalender-Klick auf eine andere KW.
9. TS-155.9 Ab-/Anmelden behält die Checkbox; Users/Edit → Präferenz zurücksetzen → Default.
10. TS-155.10 Teilmenge buchen → Rest sinkt sofort.
11. TS-155.11 (a) POST „Erneut senden" als nur-einkauf → AccessDenied; (b) Keyuser „Erneut senden" mit Sage-WE an → Erfolgsmeldung, Eintrag „offen".
12. Rückfragen 9–11 beantworten (picking bei Sage-WE-Aktionen, Fall „CSV erneut schreiben", Rücksprung).
13. Danach Merge (Schranke 2).
