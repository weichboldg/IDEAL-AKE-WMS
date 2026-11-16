---
type: spec
title: Offene Bestellungen – sofort neu lesen nach Sage-WE, Ansicht im Einkauf, KW-Filter Liefertermin, Checkbox „Überfällige anzeigen“
slug: 2026-11-16-offene-bestellungen-sofort-neu-lesen-einkauf-kw-filter-spec
status: Entwurf
created: 2026-11-16
updated: 2026-11-16
source_backlog: "[[2026-11-16-offene-bestellungen-delta-nach-sage-we]]"
task: ""
worktree: ""
branch: ""
standort: beide
version_vorschlag: 1.99.32
testszenario: TS-155
sql_nummer: keine
affected_code:
  - "IDEALAKEWMSService/Workers/SageBookingWorker.cs:58-106 (Takt-Schleife), :73-77 (Sage-WE-Schritt), :111-138 (ProcessSageWareneingangAsync) – Anstoß nach Übergabe"
  - "IDEALAKEWMSService/Services/SageWareneingangService.cs:55-56 (RunOnceAsync, heute Task), :123/:154 (Zähler uebergeben), :168 (FinishSuccessAsync) – Rückgabe der Übergaben"
  - "IDEALAKEWMSService/Services/Sdata/EinkaufBestellungenSyncService.cs:62 (RunAsync), :93-101 (Empty-Guard), :186-193 (Vollersatz in Transaktion) – prozessweite Sperre + optionaler Anlass"
  - "IDEALAKEWMSService/Workers/SyncWorker.cs:484-495 (regulärer Lauf Offene Bestellungen), :614-622 (RunResilientAsync) – unverändert bis auf das Warten auf die Sperre"
  - "IdealAkeWms/Models/ServiceSettingDefinitions.cs:120 (Anzeige-Katalog), :177-189 (Bereiche), :284 (Definition Sync:EinkaufBestellungenEnabled) – neue Einstellung daneben"
  - "IdealAkeWms/Controllers/PurchaseOrdersController.cs:15-16 (Class-Filter), :43-57 (ColumnMap liefertermin ohne KW), :74-131 (Index inkl. Wochen :78-81, Horizont :89, nurUeberfaellig :90), :133-149 (ZeilenAsync, Ueberfaellig :147)"
  - "IdealAkeWms/Controllers/PurchaseOrdersController.Receive.cs:14-15, :43-46 – [RequireStockAccess], bleibt unverändert (Prüfanker für einkauf ohne Lager-Recht)"
  - "IdealAkeWms/Views/PurchaseOrders/Index.cshtml:10-13, :27-30, :36-39 (Wochen-Feld), :62-67 (nur überfällig), :77 (Zurücksetzen), :85-92 (Info-Zeile Muster), :99 (th liefertermin data-date-filter), :120 (Zelle ohne KW), :140-143 (Wareneingang-Knopf)"
  - "IdealAkeWms/ViewModels/PurchaseOrderListViewModel.cs:18 (HorizontBis)"
  - "IdealAkeWms/Models/AppSettingDefinitions.cs:128-130 (OffeneBestellungenVorschauWochen), :168-170 (EinkaufAktiv)"
  - "IdealAkeWms/Models/AppSettingKeys.cs:118 (OffeneBestellungenVorschauWochen)"
  - "IdealAkeWms/Views/Shared/_Layout.cshtml:85-87 (showEinkauf), :114-124 (Lager-Menü), :276-286 (Einkauf-Dropdown, active nur PurchaseProposals)"
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
  - "scripts/deploy/nachlauf.psd1 (Pruefung- und Info-Einträge)"
open_questions:
  - "1 Laufzeit des Volllaufs: Prüfabfrage P-1 an AKE-Test (und IDEAL, falls dort aktiv) ausführen. Gilt bei Median > 30 s Variante B (Delta je BelId) statt des Volllaufs? Empfehlung: Variante A (Volllauf) bauen, B nur bei gemessenem Median > 30 s."
  - "2 Positionen ohne Liefertermin: immer sichtbar und markiert (Empfehlung) oder dem KW-Filter / der Checkbox unterworfen?"
  - "3 „Sage-Wareneingänge“ zusätzlich lesend im Menü Einkauf? Empfehlung: nein (vorerst); der Status je Position steht schon in Offene Bestellungen."
  - "4 AppSetting OffeneBestellungenVorschauWochen entfernen (Empfehlung, mit Pruefung/Reparatur im Manifest) oder umwidmen?"
  - "5 KW-Vorbelegung als „KWnn“ wie der Kalender-Klick (Empfehlung) oder als jahresgenauer Datumsbereich Mo...So?"
  - "6 Checkbox „nur überfällig“ beibehalten (Empfehlung) oder streichen?"
  - "7 Fehler im Anstoß: nur Aktivitäts-Protokoll (Empfehlung) oder zusätzlich Fehlermail wie der reguläre Lauf?"
  - "8 Gate der Einkauf-Ansicht: nur EinkaufAktiv (Empfehlung) oder zusätzlich WareneingangBestellungAktiv; Umleitungstext von RequireEinkaufAktiv allgemein fassen (Empfehlung: ja)?"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

# Offene Bestellungen: sofort neu lesen nach Sage-WE, Ansicht im Einkauf, KW-Filter, „Überfällige anzeigen“

Quelle: [[2026-11-16-offene-bestellungen-delta-nach-sage-we]] inkl. „Erweiterung Gerald 2026-11-16 20:07/20:09“. Bezug:
[[2026-11-14-review-befunde-offene-bestellungen]], parallele Spec [[2026-11-16-bestellvorschlaege-nachbesserung-spec]] (Kollisionen siehe unten).

Code gelesen im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, HEAD 223d7015. Alle Zeilenangaben beziehen sich auf diesen Stand.

## Nummern (geprüft 2026-11-16, Dev-Lauf prüft erneut)

| Was | Stand | Diese Spec |
|---|---|---|
| Version | beide `AppVersion.cs` = 1.99.29; 1.99.30 reserviert BDE-Teileliste, 1.99.31 reserviert [[2026-11-16-bestellvorschlaege-nachbesserung-spec]] | **1.99.32** |
| SQL | höchste Datei `SQL/155_AddArticleLastOrderUnassigned.sql`; 156 reserviert (Bestellvorschläge E8) | **keine** (falls doch nötig: 157) |
| Testszenario | letztes Kapitel TS-153 in `docs/TESTSZENARIEN.md`; TS-154 reserviert (Bestellvorschläge) | **TS-155** |
| ADR | höchster 0029 | **kein neuer ADR**; nächster freier wäre 0030 |

Die parallele Session (Bestellvorschläge, Status InUmsetzung, derselbe Worktree) kann Nummern belegen, bevor dieser Lauf startet. Der Dev-Lauf
nimmt dann die jeweils nächste freie Nummer und trägt sie hier nach.

## Ziel / Nutzen (das Warum)

1. Nach einem an Sage übergebenen Wareneingang steht die Bestellung heute bis zu 15 Minuten unverändert in „Offene Bestellungen“, weil der Cache
   `SagePurchaseOrderLines` nur im 15-Minuten-Zyklus voll ersetzt wird (`SyncWorker.cs:484-495`, Intervall `WorkerSettings:SyncIntervalMinutes`,
   `SyncWorker.cs:31`). Die Rest-Regel zieht das im WMS Erfasste bereits ab (`PurchaseOrderReceiptRepository.cs:22-30`), eine EBB mit Rest 0
   bleibt aber sichtbar, bis Sage sie nicht mehr liefert. Ein entprellter Volllauf direkt nach der Übergabe verkürzt das auf Sekunden bis wenige
   Minuten.
2. Der Einkauf braucht die Liste in seinem eigenen Menü, ohne Lager-Rechte und ohne Wareneingang buchen zu können.
3. Die Liste soll wie andere Datumsspalten nach Kalenderwoche filtern und standardmäßig die laufende KW zeigen. Das ersetzt die Einstellung
   „Wochen voraus“.
4. Überfällige Positionen stören das Lager bei der Tagesplanung, der Einkauf braucht sie. Daher eine Checkbox je Ansicht mit
   unterschiedlichem Default, je Benutzer gemerkt.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- F1 Volllauf „Offene Bestellungen (Sage)“ nach erfolgreicher Sage-WE-Übergabe, entprellt, neue ServiceSetting.
- F2 Ansicht „Offene Bestellungen“ im Menü Einkauf (lesend; Wareneingang nur mit Lager-Recht, serverseitig).
- F3 Datumsfilter Liefertermin mit KW-Vorbelegung (beide Ansichten); Entfernen von „Wochen voraus“ (`OffeneBestellungenVorschauWochen`).
- F4 Checkbox „Überfällige anzeigen“ (Lager Default aus, Einkauf Default ein, je Benutzer gespeichert), Hinweiszeile.
- F5 Rest-Regel und Wareneingang (`Receive`) bleiben unverändert – ausdrücklich.

**Out-of-Scope**
- Delta-Abgleich je `BelId` (Vorschlag des Backlogs): nur als Variante B, falls Rückfrage 1 das verlangt.
- Manuelles „Als übergeben markieren“ (`PurchaseOrdersController.SageWe.cs:122`) löst **keinen** Anstoß aus: Es läuft im Web, nicht im Dienst.
  Der reguläre Zyklus holt das nach.
- Änderungen an `PurchaseProposalsController` / Bestellvorschläge (gehört der Parallel-Spec).
- „Sage-Wareneingänge“ im Einkauf (Rückfrage 3).
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
  Schleife wiederholt; der 15-Minuten-Zyklus bleibt die Rückfallebene. Eine Fehlermail gibt es nicht (Rückfrage 7, Empfehlung).
  Der Sage-WE-Schritt und der CSV-Schritt laufen unabhängig weiter.
- **F1.6 Nachvollziehbarkeit:** Der angestoßene Lauf schreibt als erste Info-Zeile „Anlass: Sage-Wareneingang (n Position(en) übergeben)“.
  Daran erkennt man ihn im Aktivitäts-Protokoll vom regulären Lauf.
- **F1.7 Nebenläufigkeit:** Ein angestoßener und ein regulärer Lauf dürfen nie gleichzeitig laufen. Der Vollersatz löscht und fügt in einer
  Transaktion ein (`:186-193`), und `BelPosId` ist UNIQUE (`ApplicationDbContext.cs:1431`). Ein paralleler zweiter Lauf scheitert deshalb beim
  Löschen schon gelöschter Zeilen (Concurrency-Fehler) oder am Index. Ist ein Lauf aktiv, bleibt der Anstoß offen und wird im nächsten Takt
  nachgeholt. Der reguläre Lauf wartet auf das Ende des laufenden Laufs.
- **F1.8 Laufzeit:** Der Anstoß läuft im Takt des `SageBookingWorker` und verzögert die nächste Sage-Lagerbuchung um seine Laufzeit. Die Laufzeit ist
  in dieser Sitzung nicht gemessen (**unbelegt**). Prüfabfrage P-1 (unten) an AKE-Test ausführen. Ist der Median deutlich größer als 30 s, gilt
  Variante B (Rückfrage 1).

**Variante B (nur falls Rückfrage 1 so entschieden):**
- Statt des Volllaufs liest der Anstoß nur die übergebenen `BelId` per SData: `EinkaufPositionenInfo`, `where BelId eq …`, je Belegart.
- Er ersetzt nur deren Cache-Zeilen. Liefert Sage nichts mehr, werden die Zeilen gelöscht.
- Dafür wird der Zuordnungscode aus `RunAsync` (`:103-141`, Artikel/Kreditor/Platz/Liefertermin) in eine gemeinsame Methode gezogen, die Volllauf und
  Delta beide nutzen.
- Kein Empty-Guard, da gezielt gelesen wird. Mehraufwand: geschätzt ein zusätzlicher Dev-Tag plus Tests für „Beleg verschwunden“.
- Bis zur Entscheidung gilt Variante A.

### F2 „Offene Bestellungen“ im Menü Einkauf

- **F2.1 Menü:** Neuer Eintrag „Offene Bestellungen“ im Einkauf-Dropdown (`_Layout.cshtml:276-286`), sichtbar wie das Dropdown selbst
  (`showEinkauf`, `:85-87`: EinkaufAktiv + Rolle einkauf/admin). Die active-Markierung des Dropdowns erkennt auch die neue Seite.
- **F2.2 Zugriff:** Für die Seite gelten `[RequireEinkaufAccess]` und `[RequireEinkaufAktiv]`, dieselben Filter wie Bestellvorschläge
  (`PurchaseProposalsController.cs:15-17`). Ein Lager-Recht ist nicht nötig. Admin sieht die Seite über die Admin-Wildcard (ADR 0006).
- **F2.3 Lesend:** Die Ansicht zeigt dieselben Zeilen, Spalten, Hinweise und dieselbe Stand-Zeile wie Lager → Offene Bestellungen.
  Der Knopf „Wareneingang“ (je Zeile `Index.cshtml:140-143` und oben `:10-13`) erscheint nur, wenn beides gilt: Der Benutzer hat Lager-Schreibrecht
  (`CanAccessStockAsync`) und `WareneingangBestellungAktiv` ist an. Sonst würde `Receive` mit 404 enden (Class-Filter
  `RequireWareneingangBestellungAktiv`, `PurchaseOrdersController.cs:15-16`).
- **F2.4 Serverseitig:** `Receive` GET und POST bleiben unverändert hinter `[RequireStockReadAccess]` + `[RequireWareneingangBestellungAktiv]` (Class)
  und `[RequireStockAccess]` (Action, `PurchaseOrdersController.Receive.cs:14-15`, `:43-46`). Ein Benutzer nur mit Rolle einkauf bekommt bei direktem
  Aufruf per URL oder POST AccessDenied. Das ist so gebaut und wird per Test belegt, nicht neu gebaut.
- **F2.5 Lager-Ansicht unverändert erreichbar:** Lager → Offene Bestellungen bleibt bei `RequireStockReadAccess` + `RequireWareneingangBestellungAktiv`.
- **F2.6 Rollen:** Es gibt keinen neuen Rollenkey. Die Rolle einkauf bekommt Lesezugriff auf eine zweite Seite. Nach CLAUDE.md gehören drei Stellen dazu:
  - Attribut: unverändert wiederverwendet (`RequireEinkaufAccess` am neuen Controller).
  - `secondbrain/codebase/controller.md`: neue Zeile für den Controller, Zugriffsmatrix für einkauf ergänzen. Die Brain-Datei pflegt der Dev-Lauf im
    Hauptcheckout.
  - `Views/Users/RoleOverview.cshtml:218-229`: Text der Zeile einkauf wird zu „Bestellvorschläge und Offene Bestellungen (nur lesen)“.
  - Dazu kommt `nachlauf.psd1` `Pruefung/RolleEinkauf`: Die Rollenbeschreibung im Reparatur-Insert wird für neue Installationen gleich nachgezogen. Text
    wie oben, nur ASCII.

### F3 Datumsfilter Liefertermin mit KW-Vorbelegung

- **F3.1 KW-tauglich rendern:** Die Zelle Liefertermin und `ColumnMap["liefertermin"]` rendern `dd.MM.yyyy KWnn` (ISO-Woche,
  `ISOWeek.GetWeekOfYear`, wie `Views/ProductionOrders/Index.cshtml:7-13`). Heute fehlt dort die KW (`PurchaseOrdersController.cs:43-57`,
  `Index.cshtml:120`). Der Kalender-Klick schreibt `KWnn` ins Filterfeld (`table-filter.js:520`), das als Text-Enthält-Filter wirkt. Ohne KW
  im Zelltext findet der KW-Filter also heute **nichts**. Zeilen ohne Termin rendern weiter „ohne Termin“.
- **F3.2 Kalender:** `<th … data-date-filter data-col-key="liefertermin">` gibt es schon (`Index.cshtml:99`), den Spaltentyp Datum auch
  (`ColumnFilterSyntax.cs:77`). Kalender, Bereich `a...b` und Vergleiche funktionieren damit ohne neuen Code.
- **F3.3 Vorbelegung:** Beim Erstaufruf jeder Ansicht leitet der Controller um, und zwar auf dieselbe Aktion mit `colf_liefertermin=KW<aktuelle ISO-KW>`
  und einem Merker `kw=1`. Erstaufruf heißt: kein `colf_liefertermin` und kein Merker in der Query.
  - Muster ist `FaWorklistController.cs:128-148`.
  - Der Merker überlebt das Leeren des Filters, weil `applyServerFilters` nur `colf_*`/`page` entfernt. Leert der Benutzer das Feld, sieht er alle
    Termine und es wird **nicht** erneut vorbelegt.
  - „Zurücksetzen“ (`Index.cshtml:77`) führt wieder ohne Parameter auf die Aktion und belegt neu vor.
- **F3.4 Wochen entfällt:** Das Feld „Wochen“ (`Index.cshtml:36-39`), der Parameter `wochen`, der Horizont-Filter (`PurchaseOrdersController.cs:78-81`,
  `:89`), `HorizontBis` (`PurchaseOrderListViewModel.cs:18`, Stand-Zeile `Index.cshtml:27-30`) und die AppSetting
  `OffeneBestellungenVorschauWochen` (`AppSettingDefinitions.cs:128-130`, `AppSettingKeys.cs:118`) werden entfernt (Rückfrage 4, Empfehlung).
  - Fundstellen, die mitziehen: `README.md:522`, `Help/Index.cshtml:79`, `PurchaseOrdersControllerTests.cs:38`, `:179`, `docs/anwender/06…:26-27`,
    `:41`, `:103`, `:119`.
  - In `SQL/` gibt es keinen Treffer: Die Einstellung wurde nie per SQL geseedet. Eine Zeile in `dbo.AppSettings` gibt es nur, wenn jemand einen
    Wert ≠ 2 gespeichert hat (`SettingsController.cs:111-119` speichert nur Abweichungen). Sie erschiene danach unter /Settings als verwaist
    (`SettingsController.cs:66`) → Manifest-Prüfung mit Reparatur (Deploy).
- **F3.5 Bekannte Grenze:** `KWnn` hat kein Jahr. `KW4` trifft auch KW40–49 (Text-Enthält). Das ist dieselbe Grenze wie in allen anderen
  Datumsspalten (Help `:581-586`). Ein jahresgenauer Bereich `TT.MM.JJJJ...TT.MM.JJJJ` (Mo–So) wäre die Alternative (Rückfrage 5).

### F4 Checkbox „Überfällige anzeigen“

- **F4.1 Begriff:** „überfällig“ = `Liefertermin < heute` (bestehende Regel `PurchaseOrdersController.cs:147`).
- **F4.2 Sichtbarkeitsregel**, präzise, je Zeile nach allen anderen Filtern (Lieferant, Bestellung, Artikel, Lagerplatz, Abrufe, übrige
  Spaltenfilter):
  - Zeile **ohne Termin** → immer sichtbar, Hinweis „ohne Termin“ (Rückfrage 2, Empfehlung).
  - Zeile **überfällig** → sichtbar genau dann, wenn „Überfällige anzeigen“ an ist. Der Liefertermin-Spaltenfilter wirkt auf überfällige Zeilen
    **nicht**. Sonst blendete die KW-Vorbelegung alle Überfälligen aus früheren Wochen aus, und die Checkbox im Einkauf wäre wirkungslos.
  - Zeile **nicht überfällig** → sichtbar genau dann, wenn sie den Liefertermin-Spaltenfilter erfüllt (leerer Filter = alle).
  - Überfällige der laufenden KW (Montag bis gestern) folgen ebenfalls nur der Checkbox, sie bleiben also im Lager bei Default AUS ausgeblendet.
    So gilt eine einzige Regel ohne Sonderfall.
- **F4.3 „nur überfällig“** bleibt (Rückfrage 6, Empfehlung) und zeigt nur überfällige Zeilen; es schließt „Überfällige anzeigen“ ein. Ist es
  angehakt, gilt die Checkbox als an.
- **F4.4 Hinweiszeile:** Sind überfällige Zeilen wegen der Checkbox ausgeblendet, erscheint über der Tabelle die Zeile
  „n überfällige Position(en) ausgeblendet – anzeigen mit „Überfällige anzeigen““. Gezählt wird nach allen anderen Filtern und vor der
  Pagination. Bei n = 0 erscheint keine Zeile. Muster ist die Info-Zeile der ausgeblendeten Abrufe (`Index.cshtml:85-92`, Zählung
  `PurchaseOrdersController.cs:100-106`). Die Zeile steht als Text und hängt nicht nur an einer Farbe.
- **F4.5 Default und Speichern je Benutzer:**
  - Default: Lager **aus**, Einkauf **ein**.
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
2. **Neuer Controller** `EinkaufPurchaseOrdersController` (Route `/EinkaufPurchaseOrders`, UI-Titel „Offene Bestellungen“) mit
   `[RequireEinkaufAccess][RequireEinkaufAktiv]`, eine Aktion `Index` mit Redirect-Vorbelegung (F3.3). Er rendert dieselbe View
   `Views/PurchaseOrders/Index.cshtml` (explizit per Pfad) mit `ansicht = Einkauf`.
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
5. **Layout:** Menüeintrag im Einkauf-Dropdown, active-Prüfung erweitert um Controller „EinkaufPurchaseOrders“.
6. **Umleitungstext** von `RequireEinkaufAktiv` lautet heute „Einkauf – Bestellvorschläge ist deaktiviert…“. Er wird auf „Einkauf ist deaktiviert…“
   verallgemeinert (Rückfrage 8).
7. **AppSetting entfernen** (F3.4), Tests anpassen.

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
| `AppSettingDefinitions.cs` / `AppSettingKeys.cs` | ggf. neue Keys | ein Key entfernt | dito |
| `_Layout.cshtml` Einkauf-Dropdown, `RoleOverview.cshtml`, `controller.md` | ggf. Text Einkauf | Menüeintrag, Rollen-Text | Text am Ende gemeinsam formulieren |
| `nachlauf.psd1`, `Changelog.cshtml`, `TESTSZENARIEN.md`, `docs/betrieb/04` | eigene Einträge 1.99.31 / TS-154 | 1.99.32 / TS-155 | Reihenfolge der Versionen einhalten |
| `SagePurchaseOrderLines.Liefertermin` | F5 liest ihn | nur Anzeige/Filter | kein Konflikt |

Empfehlung: diese Spec **nach** dem Commit der Bestellvorschläge-Nachbesserung im selben Worktree bauen.

## Migrations-/SQL-Auswirkungen

- Keine EF-Migration, kein `SQL/XX_*.sql`, keine Änderung an `00_FreshInstall.sql`. Alle Daten liegen in bestehenden Tabellen
  (`UserViewPreferences`, `ServiceSettings`).
- Die neue ServiceSetting braucht keinen Seed. Fehlt die Zeile, gilt der Katalog-Default (ADR 0008).
- Die entfernte AppSetting `OffeneBestellungenVorschauWochen` wurde nie per SQL angelegt (Grep `SQL/`: kein Treffer). Eine Restzeile gibt es nur
  bei manueller Abweichung. Sie wird über eine Manifest-Prüfung mit Reparatur entfernt, nicht über ein Schema-Skript (Deploy).
- `SQL/AgentJobs/`: nicht betroffen. Weder Pflichtfeld noch Folge-MERGE ändern sich.

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `Sync:EinkaufBestellungenNachWeMinuten` (neu, ServiceSetting, Kategorie „Sage-Abgleich (SData)“, Anzeigename „Offene Bestellungen nach Sage-Wareneingang neu lesen: Mindestabstand“) | Int | 0–60 min (Bereich, Server validiert); 0 = aus | 2 | Nach mindestens einer erfolgreichen Sage-WE-Übergabe liest der Dienst die offenen Bestellungen sofort neu, danach höchstens einmal je n Minuten. Gates wie der reguläre Lauf (`Sync:EinkaufBestellungenEnabled`, DryRun). | Dienst: Sage-Buchungs-Worker, Lauf „Sage-Wareneingang“ → Lauf „Offene Bestellungen (Sage)“ | AKE (IDEAL ohne Wirkung, solange Sage-WE dort aus ist) | Kein Schreiben nach Sage. Hinweis im Beschreibungstext: „Jeder Lauf liest alle offenen Bestellungen aus Sage; in der Zeit wartet die nächste Sage-Lagerbuchung.“ |
| `OffeneBestellungenVorschauWochen` (AppSetting, **entfernt**) | – | – | – (war 2) | ersetzt durch KW-Vorbelegung des Liefertermin-Filters (F3) | bisher Lager → Offene Bestellungen | beide | – |

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
15. Beide Ansichten zeigen beim Erstaufruf im Spaltenfilter Liefertermin `KW<aktuelle ISO-KW>` und nur Positionen dieser KW, dazu die Positionen ohne
    Termin und die Überfälligen gemäß Checkbox.
16. Jede Liefertermin-Zelle zeigt `dd.MM.yyyy KWnn`. Ein Klick auf eine andere KW im Kalender filtert auf diese KW.
17. Wer den Liefertermin-Filter leert, sieht alle Termine. Die Seite belegt dann nicht neu vor, auch nicht beim Blättern. „Zurücksetzen“ belegt neu
    vor.
18. Das Feld „Wochen“ und die AppSetting „Offene Bestellungen: Vorschau (Wochen)“ gibt es nicht mehr. /Settings zeigt dazu keinen verwaisten
    Eintrag (nach Manifest-Reparatur).
19. Lager-Ansicht, erster Aufruf eines Benutzers: Die Checkbox „Überfällige anzeigen“ ist aus, überfällige Positionen fehlen, und die Hinweiszeile
    nennt ihre Anzahl. Einkauf-Ansicht: Die Checkbox ist an, Überfällige aus allen Wochen sind sichtbar und rot mit Hinweis „überfällig“.
20. Ändert ein Benutzer die Checkbox und filtert, bleibt der Zustand beim nächsten Aufruf derselben Ansicht erhalten, auch nach Ab- und Anmelden. Die
    andere Ansicht behält ihren eigenen Zustand.
21. Positionen ohne Liefertermin sind in beiden Ansichten immer sichtbar (vorbehaltlich Rückfrage 2).
22. „nur überfällig“ zeigt ausschließlich überfällige Positionen, unabhängig von der Checkbox und vom KW-Filter.
23. Rest-Regel, Ausblenden von EBB mit Rest 0 und die Maske Wareneingang verhalten sich wie vor der Änderung (bestehende Tests bleiben grün).
24. Build und alle Tests grün. Drift-Guards (`ServiceSettingDefinitionsTests`, `AppSettingDefinitionsTests`, `NachlaufManifestDriftTests`) sind grün.

## Test-Szenarien

Neues Kapitel **TS-155 „Offene Bestellungen: sofort neu lesen, Einkauf-Ansicht, KW-Filter, Überfällige“** in `docs/TESTSZENARIEN.md`, danach
`secondbrain/tests/testszenarien-index.md` nachziehen. Standort: AKE-DEV für TS-155.1–155.4 (Sage-WE nur AKE), beide für TS-155.5–155.10.

- **TS-155.1 Anstoß nach Übergabe** – Vorbedingungen: `SageWareneingangBelegAktiv` an, `Sync:EinkaufBestellungenEnabled` an, Einstellung 2.
  - Schritte: Wareneingang zu einer EBB-Position buchen und warten, bis der Status „übergeben“ ist. Dann Aktivitäts-Protokoll öffnen und
    Offene Bestellungen neu laden.
  - Erwartet: Lauf „Offene Bestellungen (Sage)“ mit „Anlass: Sage-Wareneingang (1 Position(en) übergeben)“; „Stand“ springt auf die Uhrzeit des
    Anstoßes, die voll gelieferte Position ist weg.
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
- **TS-155.8 KW-Vorbelegung** – Beide Ansichten frisch öffnen.
  - Erwartet: Spaltenfilter Liefertermin = `KW<aktuell>`; nur diese KW plus „ohne Termin“ plus Überfällige gemäß Checkbox. Zellen mit KW.
  - Weiter: Kalender → andere KW klicken. Erwartet: deren Positionen.
  - Weiter: Filter leeren und blättern. Erwartet: alle Termine, keine erneute Vorbelegung. „Zurücksetzen“ → wieder aktuelle KW.
  - Negativ: kein Feld „Wochen“; /Settings ohne „Vorschau (Wochen)“.
- **TS-155.9 Überfällige** – Lager: Checkbox aus, Hinweiszeile „n überfällige Position(en) ausgeblendet“, n stimmt mit dem Lauf „nur überfällig“
  überein. Checkbox an → Überfällige aller Wochen rot mit Hinweis, Hinweiszeile weg.
  - Abmelden und wieder anmelden: Der Zustand bleibt. Die Einkauf-Ansicht steht unabhängig davon auf an.
  - In Users/Edit die Präferenz zurücksetzen → wieder Default.
  - Negativ: Überfällige der laufenden KW (vor heute) sind bei Checkbox aus nicht sichtbar.
- **TS-155.10 Unverändert** – Rest-Regel: Teilmenge buchen → Rest sinkt sofort, Position bleibt bis zum Abgleich. Rahmenabruf-Verhalten wie
  TS-Kapitel Wareneingang Teil 3.

**Automatische Tests (InMemory/Moq)**
- `SageWareneingangService`: Rückgabe = Anzahl übergeben (Erfolg, „in Sage gefunden“, nichts offen = 0, Wurf).
- Anstoß-Logik: Die Entscheidung (offen, Minuten, Gate, letzter Anstoß, jetzt) → laufen ja/nein liegt als kleine statische Funktion
  (`internal static bool AnstossFaellig(...)`), damit sie ohne Worker-Schleife testbar ist. Fälle: erster sofort, im Fenster nein, nach Fenster ja,
  0 = nein, Gate aus = nein und verwirft.
- `EinkaufBestellungenSyncService`: Sperre belegt → `TryRunExclusiveAsync` = false ohne Lauf; Anlass-Zeile geschrieben
  (bestehendes `EinkaufBestellungenSyncTests`-Muster).
- `PurchaseOrdersControllerTests` (bestehende Wochen-Tests `:38`, `:179` ersetzen):
  - Redirect mit `colf_liefertermin=KWnn` + `kw=1` beim Erstaufruf; kein Redirect mit Merker.
  - Sichtbarkeitsregel F4.2 (ohne Termin immer, überfällig nur mit Checkbox, KW-Filter nur auf nicht überfällige); Hinweiszahl.
  - Präferenz-Default je Ansicht, Präferenz gespeichert und gelesen.
  - CanReceive false ohne Lager-Recht oder ohne WareneingangBestellungAktiv.
- Neuer `EinkaufPurchaseOrdersController`: Attribute per Reflection (`RequireEinkaufAccess`, `RequireEinkaufAktiv`). `Receive` behält
  `RequireStockAccess` (Reflection-Test, damit niemand die Absicherung entfernt).
- Drift-Guards: ServiceSetting im Katalog, AppSetting entfernt, Manifest-Einträge gültig.
- **Manual-UAT** (nicht InMemory-testbar): echte Sage-Übergabe und SData-Lauf (TS-155.1–155.4), echte Nebenläufigkeit beider Worker,
  Kalender-Klick im Browser, AccessDenied über die echte Filter-Pipeline.

## Etappen (nur bei epic: true)

Kein Epic, entfällt.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

**Provisorisch, der QA-Lauf bestätigt gegen den echten Diff.**

- **Web-App:** ja (Controller, View, Layout, RoleOverview, AppSetting-Katalog, Hilfe, Changelog).
- **Service:** ja (`SageBookingWorker`, `SageWareneingangService`, `EinkaufBestellungenSyncService`, ServiceSetting-Katalog liegt im Web-Projekt und
  wird vom Dienst mitgenutzt).
- **Migration:** nein.
- **Reihenfolge:** Web und Dienst gemeinsam (Update-Skript). Nach dem Start greift die neue Einstellung mit Default 2 sofort. Wer das nicht will,
  setzt sie vorher auf 0.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023):
  - `@{ Id='Pruefung/OffeneBestellungenVorschauWochen'; Version='1.99.32'; Typ='Pruefung'; Firma='beide'; Phase='Haltepunkt'; Ausfuehrung='Reparatur'`
    `Pruefung='SELECT COUNT(*) FROM dbo.AppSettings WHERE [Key] = N''OffeneBestellungenVorschauWochen'''`
    `Reparatur='DELETE FROM dbo.AppSettings WHERE [Key] = N''OffeneBestellungenVorschauWochen'';'`
    `Text='Einstellung Offene Bestellungen: Vorschau (Wochen) entfaellt ab v1.99.32 (ersetzt durch KW-Filter Liefertermin); die Restzeile erschiene unter /Settings als verwaist. Reparatur loescht sie.'`
    `Quelle='<brain-changelog v1.99.32>' }`
  - `@{ Id='v1.99.32-EinkaufOffeneBestellungen'; Version='1.99.32'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'`
    `Text='Rolle einkauf sieht jetzt Einkauf > Offene Bestellungen (nur lesen, Gate EinkaufAktiv). Wareneingang dort nur mit Lager-Recht. Keine Rollenzuweisung noetig.' … }`
  - `@{ Id='v1.99.32-NachWeMinuten'; Version='1.99.32'; Typ='Info'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'`
    `Text='Neu: Sync:EinkaufBestellungenNachWeMinuten (Default 2, 0 = aus). Nach Sage-Wareneingang liest der Dienst die offenen Bestellungen sofort neu; Laufzeit im Aktivitaets-Protokoll beobachten (P-1).' … }`
    Alternativ `Typ='Einstellung'`, falls der Dev-Lauf das Feldformat dafür im Manifest vorfindet. Belegt sind dort heute nur `Info` und
    `Pruefung`-Formate.
  - `Pruefung/RolleEinkauf` (`:140-147`): Beschreibungstext im Reparatur-Insert auf „Einkauf - Bestellvorschlaege und Offene Bestellungen (lesen).“
    anpassen. Die Id bleibt, die Wirkung nur bei fehlender Rolle.
- **Doku:** `Views/Help/Changelog.cshtml` (Anwender) und Hilfeseite (`Help/Index.cshtml:79` Wochen raus; Abschnitt Offene Bestellungen um Einkauf,
  KW-Vorbelegung, Checkbox). Dazu:
  - `docs/anwender/05-wareneingang-zu-bestellung.md` (Stand nach Übergabe aktualisiert sich binnen Minuten)
  - `docs/anwender/06-offene-bestellungen-rahmenabrufe.md` (`:26-27`, `:38-43`, `:103`, `:109`, `:119` – Wochen raus, KW, Checkbox, Einkauf-Menü;
    Screenshot `06-01` veraltet → neu oder als veraltet kennzeichnen)
  - `docs/betrieb/04-konfiguration.md` (neue ServiceSetting neben `:159`, Zeile `:200-201` um Einkauf-Ansicht; AppSetting raus)
  - `docs/betrieb/05-schnittstellen.md` (`:69` Schalterliste, Ablauf Anstoß)
  - `README.md:522`; Brain: `codebase/services.md` (Anstoß, Sperre), `codebase/controller.md` (F2.6), `feature-map.md`, `changelog/`,
    `fallstricke.md` (Eintrag: „KW-Filter wirkt als Text-Enthält, Zelle muss KWnn enthalten“ und „Cache-Vollersatz nie parallel – Sperre“).
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

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Laufzeit P-1 / Variante A oder B →
2. Positionen ohne Liefertermin immer sichtbar? →
3. „Sage-Wareneingänge“ lesend im Einkauf? →
4. `OffeneBestellungenVorschauWochen` entfernen oder umwidmen? →
5. Vorbelegung `KWnn` oder Datumsbereich Mo–So? →
6. „nur überfällig“ beibehalten? →
7. Fehler im Anstoß: nur Protokoll oder auch Fehlermail? →
8. Gate Einkauf-Ansicht nur `EinkaufAktiv`, Umleitungstext allgemein? →
