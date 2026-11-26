---
type: aufgabe
status: offen
created: 2026-11-26
version: 1.99.80
source: "[[2026-11-25-kommission-lagerplatz-bde-goal-lauf]]"
---
# Besprechung 08:30 – kritische Prüfung Bündel v1.99.70–v1.99.80

Rolle: Advocatus Diaboli. Nur gelesen, **kein Code geändert**. Basis: Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Stand
`3c5a97df`. Lauf-Notiz: [[2026-11-25-kommission-lagerplatz-bde-goal-lauf]]. Mail-Entwurf: [[2026-11-26-mail-wms-neuerungen-v1-99-70-80]].
Pfade ohne Präfix beziehen sich auf den Worktree.

## Für 8:30 – die 5 wichtigsten Punkte

1. **Kommissionier-Nachtrag läuft beim ersten Dienststart auf Prod sofort, mitten am Tag.** `Sync:KommissionierNachtragEnabled` ist
   standardmäßig an (`IdealAkeWms/Models/ServiceSettingDefinitions.cs:292`). Der Nachtrag holt einen verpassten Termin sofort nach
   (`:293`, Infotext `:169-170`). Er läuft synchron im SyncWorker-Zyklus über alle freigegebenen Fertigungsaufträge und liest je FA die
   Stückliste. Dabei legt er sofort Kommissionieraufträge an. → Vor dem Deploy auf `false` setzen und abends gezielt einschalten.
2. **Der Terminal-Wächter (H-A) deckt nur das Starten ab, nicht das Pause- oder Fertigmelden.** Die Mail behauptet aber: „seit 1.99.79
   nimmt das WMS nur Buchungen vom eigenen Terminal an“ (Mail-Entwurf Z. 77-78).
   - Ohne Terminalprüfung: `Pause`, `Finish`, `FinishGroup`, `ReportPartial`, `CloseOthers`
     (`IdealAkeWms/Controllers/BdeTerminalController.cs` ca. Z. 97/125/221/244/267). Sie übernehmen `bookingId` und `operatorId` vom
     Client.
   - Mit Wächter (`FremdesTerminalAsync`, Z. 273-280): nur die Start-, Fortsetzen- und Fertigmelden-Wege.
   - → Entweder vor dem Prod-Deploy nachziehen oder den Mailsatz korrigieren.
3. **Die Prüfung `Pruefung/BdeNurFaMeldungAn` kann den Prod-Deploy abbrechen.** Sie läuft in Phase Vorher an beiden Standorten
   (`scripts/deploy/nachlauf.psd1`). Ist auf Prod `BdeNurFaMeldung = true`, bricht das Skript vor dem Stoppen ab. → Den Prod-Wert an
   AKE und IDEAL heute vorab abfragen und gegebenenfalls in `/Settings` ausschalten. Die Einstellung ist laut
   `AppSettingDefinitions.cs:221-222` eine Altlast.
4. **Der Prod-Deploy umfasst mehr als die geprüften Versionen v1.99.70–80.** AKE-Prod steht auf v1.99.63/64
   ([[2026-11-24-v1-99-64-sdata-group-by-ake-prod]]). Mitgeliefert werden also auch v1.99.65–69 samt deren SQL und Manifest-Einträgen.
   Diese hat diese Prüfung nicht gesehen. Außerdem fehlte im Auftragsbereich `6e426113~1..3c5a97df` der Code von v1.99.70 (`fa1b754b`);
   geprüft wurde deshalb ab `fa1b754b~1`. Die Mail nennt nur SQL/168 und SQL/169 (Z. 72), neu für Prod sind aber auch 164 und 165.
5. **Die Mail verspricht Funktionen, die standardmäßig aus sind.**
   - Fräsen-Pfad (Mail Z. 37-38 und 66-67): hängt an `FraesenPfadAktiv`, Default `false`, kein Seed (`AppSettingDefinitions.cs:254-256`).
     Freischalten erst nach dem Einmal-Skript, das wegen Rückfrage 15/16 noch nicht scharf laufen darf.
   - Lagerbestellungen an der Isolierungsfräse (Z. 63-64): brauchen die Werkbank-Option `BdeTeilelisteLagerbestellungen`, Default 0
     (SQL/165).
   - Rollenhinweis (Z. 67): unvollständig, pflegen dürfen auch admin und masterdata (`CurrentUserService.cs` `CanPflegeFraesenPfadAsync`).
   - Handy-Konten: noch nicht angelegt.
   - → Mail auf „wird freigeschaltet, sobald …“ umformulieren.

Zählung: **BLOCKER 4 · SOLLTE 8 · HINWEIS 10**

## BLOCKER (vor Prod-Deploy)

### B-1 Sofortiger Massenlauf des Kommissionier-Nachtrags beim ersten Dienststart
- **Beleg:**
  - Default `"true"`: `ServiceSettingDefinitions.cs:292`.
  - Fälligkeit: letzter Slot heute bzw. gestern, der kleiner oder gleich jetzt ist und nach dem letzten Start liegt (`LetzteBestellungPlaner.Faellig`).
  - Der Marker „Run gestartet“ fehlt nach dem Deploy, also ist der Lauf sofort fällig.
  - Manifest-Info `v1.99.70-KommNachtrag-Lauf` beschreibt genau das.
  - Der Lauf hängt synchron im SyncWorker (nach FA-Import, BOM und Hierarchie, `RunResilientAsync`), mit einem Scope je FA und einem
    Stücklisten-Read je FA. Bis er fertig ist, warten die anderen Sync-Läufe.
- **Risiko:**
  - Der Leitstand sieht tagsüber schlagartig neue Kommissionieraufträge, ohne Vorwarnung.
  - Andere Syncs (FA-Import) verzögern sich.
  - Ein Nachtrag für Altbestand mit „altbestand_pruefen“ fällt zum ungünstigsten Zeitpunkt an.
- **Vorschlag:** In der Deploy-Checkliste als Vorher-Schritt ergänzen: `/ServiceSettings` → `Sync:KommissionierNachtragEnabled = false`.
  Nach dem Deploy abends einschalten. Dann läuft er einmal sofort, beobachtet im Aktivitäts-Protokoll „Kommissionier-Nachtrag“, und
  danach ab 02:00. Alternativ: Deploy erst am Abend, Leitstand vorab informieren.

### B-2 Terminal-Wächter unvollständig, die Mail sagt das Gegenteil (H-A)
- **Beleg:**
  - `BdeTerminalController.cs:273-280`: `FremdesTerminalAsync` steht nur in StartProductionForOrder, StartSetup, StartProduction,
    StartActivity, Resume und ReportCompletion.
  - Ohne den Wächter laufen `Pause` (Z. 97), `Finish` (125), `FinishGroup` (221), `ReportPartial` (244) und `CloseOthers` (267).
  - Die GETs `PausedBookings` und `ActiveBookings` (operatorId vom Client) sind ebenfalls ohne Wächter und zudem verwaist (F-29).
  - `BdeTeilelisteController.Zuruecknehmen` (ca. Z. 399) prüft kein Terminal. Der Service lässt nur eigene Meldungen vor Frist zu.
- **Risiko:** Ein beliebiges BDE-Konto kann fremde Buchungen pausieren oder abschließen, wenn es bookingId und operatorId kennt. Das
  geht schon mit zwei offenen Tabs oder einem Handy an der falschen Werkbank. Die Mail sichert „nur vom eigenen Terminal“ zu. Ein Werker,
  der sich darauf verlässt, bemerkt eine Fremdbuchung nicht.
- **Vorschlag (Entscheid Gerald):**
  - (a) Den Wächter mit einer Prüfung „Buchung gehört zum Terminal bzw. zur Werkbank dieses Kontos“ auf die fünf POSTs ziehen. Das ist
    ein kleiner Bugfix vor Prod, alles läuft schon über `FremdesTerminalAsync`.
  - (b) Bewusst als BDE-internes Vertrauensmodell akzeptieren (operatorId ist ohnehin nicht authentisiert) und den Mailsatz auf „Starten
    nur vom eigenen Terminal“ korrigieren.
  - Ohne a oder b nicht ausrollen.

### B-3 `Pruefung/BdeNurFaMeldungAn` kann den Prod-Deploy vor dem Stoppen abbrechen
- **Beleg:** `scripts/deploy/nachlauf.psd1`, Eintrag `Pruefung/BdeNurFaMeldungAn`, Phase Vorher, Firma beide.
  `AppSettingDefinitions.cs:221-222` nennt BdeNurFaMeldung eine Altlast. Die Mail (Z. 83) sagt nur „das Update-Skript meldet es“.
- **Risiko:** Ein abgebrochenes Wartungsfenster. Schlimmer: Jemand schaltet die Einstellung in Eile aus, ohne zu prüfen, ob AKE-Werkbänke
  noch im FA-Meldemodus arbeiten.
- **Vorschlag:** Heute an beiden Prod-Datenbanken abfragen:
  `SELECT [Key],[Value] FROM dbo.AppSettings WHERE [Key]=N'BdeNurFaMeldung'`. Bei `true`: Fachbereich fragen, dann vor dem
  Wartungsfenster ausschalten.

### B-4 Schranke 2 nicht genommen, die Abnahme ist lückenhaft
- **Beleg:**
  - Lauf-Notiz: TS-201.3.3 und TS-198.6/.7 nicht durchführbar.
  - Handy-Konten nicht angelegt.
  - F-16: `USER_OSBlechdicke` an Sage IDEAL unbelegt (das DEV-IDEAL-Dienstkonto hat keinen Login auf `IDEAL_TEST_2026_05_03`).
  - Ein UAT-Agent arbeitet gerade noch.
  - Status Testbereit, nicht gemergt.
- **Vorschlag:** Erst nach Abschluss der UAT, nach dem Entscheid zu B-2 und nach dem Merge (Schranke 2) ausrollen. Die offenen
  Testszenarien ausdrücklich als „nach Prod nachholen“ ins Protokoll aufnehmen, statt sie stillschweigend zu überspringen.

## SOLLTE

### S-1 Deploy-Umfang v1.99.65–69 ungeprüft
- **Beleg:** AKE-Prod steht auf 1.99.63/64 ([[2026-11-24-v1-99-64-sdata-group-by-ake-prod]]).
- **Vorschlag:** Kurz die Manifest-Einträge (`nachlauf.psd1`) und die SQL-Skripte von 1.99.65–69 durchsehen. Den IDEAL-Prod-Stand
  erfragen.

### S-2 Mail korrigieren (siehe Top-5 Nr. 5)
- **Beleg:**
  - FraesenPfadAktiv: Default false, kein Seed (`AppSettingDefinitions.cs:254-256`).
  - Werkbank-Option `BdeTeilelisteLagerbestellungen`: DEFAULT 0 (SQL/165, `Models/ProductionWorkplace.cs:64`).
  - SQL-Liste (Mail Z. 72) unvollständig: 164 und 165 fehlen.
  - Rollenhinweis (Z. 67) unvollständig.
  - Terminal-Satz: siehe B-2.

### S-3 Sage-Mandant fest 1 und STRING_AGG (H-C)
- **Beleg:**
  - `IDEALAKEWMSService/Services/SqlSageLagerplatzAnlage.cs:14-15`: `Mandant = 1` mit ponytail-Kommentar.
  - `STRING_AGG … WITHIN GROUP` setzt SQL Server 2017 oder neuer voraus.
- **Vorschlag:** Vor dem Einschalten von `SageLagerplatzAnlageAktiv` je Standort `SELECT DISTINCT Mandant FROM KHKLagerplaetze` und
  `SELECT @@VERSION` auf der Sage-Instanz ausführen (auf Prod durch den Menschen). Die Prüfung blockiert den Deploy nicht, weil der
  Schalter standardmäßig aus ist (`ServiceSettingDefinitions.cs:319`).

### S-4 Lagerplatz-Anlage: Altbestand der Outbox vor dem Einschalten prüfen
- **Beleg:**
  - `StorageLocationRepository.cs:19-28` (seit v1.73.0 E9) reiht jeden neu angelegten Platz ein.
  - Der Controller setzt beim Anlegen `StorageAreaId = null` (`StorageLocationsController.cs:123-124`).
  - Der Service sendet nur Einträge mit Manual, StorageAreaId ungleich null und IsActive.
  - Die Spec (Z. 116) schließt Altbestand aus.
- **Vorschlag:** Vor dem Einschalten zählen, was im Sinne der Spec „sendbar“ wäre. Erwartet wird 0 bzw. nur die frisch erfassten Plätze.
  Sonst legt der erste Zyklus unerwartet Plätze in Sage an.

### S-5 Abbruch während des Sage-Aufrufs lässt den Eintrag 10 Minuten auf „Gesendet“
- **Beleg:**
  - `SageLagerplatzAnlageService`: Status Gesendet und Versuch +1 vor dem Sage-Aufruf.
  - catch `when (ex is not OperationCanceledException)`.
  - Wiederaufnahme erst nach `Sync:SageLagerbuchungStuckMinutes` (Default 10).
  - Der Sage-Befehl ist mit UPDLOCK/HOLDLOCK idempotent, also ist das fachlich unkritisch.
- **Vorschlag:** In der Betriebsdoku festhalten: „Gesendet“ nach einem Dienst-Neustart ist bis zu 10 Minuten normal, nicht „Erneut
  senden“ drücken.

### S-6 Einmal-Skript Fräsen-Pfad nicht scharf, solange Rückfrage 15/16 (F-14) offen ist
- **Beleg:** `SQL/Einmalig/2026-11-25_v1.99.72_FraesenPfad_Erstimport.sql`.
  - `@Trockenlauf = 1`.
  - Standortwächter auf `ProduktionsauftragHierarchisch` (Z. 34-37) ist ok.
  - Die Abbildung Y: → `\\idealfs1\Daten$\Produktion\CAM\` ist eine Annahme (Rückfrage 15). Die Belegestation (16) ist offen.
- **Vorschlag:** Nur den Trockenlauf auf IDEAL-Prod laufen lassen, die Treffer- und Unbekannt-Liste durch den Fachbereich prüfen lassen,
  erst dann SCHARF.

### S-7 Blechteile-Nachlese an IDEAL erst nach dem Beleg P-1 (F-16)
- **Beleg:** `USER_OSBlechdicke` an Sage IDEAL ist nicht nachgewiesen (Lauf-Notiz). Die Prüfung `Pruefung/BlechteileNachleseAus` meldet
  nur.
- **Vorschlag:** Die Prüfabfrage an `IDEAL_TEST_*` mit einem Konto mit Login ausführen (seit 2026-11-25 erlaubt), bevor
  `Sync:BlechteileFelderNachlesen` an IDEAL eingeschaltet wird.

### S-8 Rückfall SQL/169 nicht verlustfrei
- **Beleg:** Die Down-Migration baut den alten UNIQUE ohne `IstAbschliessend`. Sie scheitert, sobald es Teilfertig-Meldungen gibt
  (Migration `20261125191311_…`, Kommentar in Down; `docs/betrieb/03-installation-update-rollback.md:204`).
- **Vorschlag:** In der Besprechung festhalten: Ein Rückfall nach Prod ist nur über das DB-Backup aus Schritt 2 möglich. Das Backup ist
  damit Pflicht.

## HINWEIS

- **H-1 Prüfbereich:** `6e426113~1..3c5a97df` enthält nicht den Code von v1.99.70 (`fa1b754b`). Geprüft wurde `fa1b754b~1..3c5a97df`.
- **H-2 SQL 164/165/168/169 idempotent:**
  - COL_LENGTH- und OBJECT_ID-Guards vorhanden, DDL in eigenen Batches, History-Insert separat.
  - SQL/169 prüft Dubletten vorab mit Warnung, `NOEXEC ON` und ohne History. Dubletten sind logisch unmöglich, weil der alte Filter
    auf derselben Menge schon eindeutig war.
  - `00_FreshInstall.sql` ist an beiden Stellen nachgezogen: die Spalten, die Indizes mit den Endfiltern und die vier MigrationIds
    `20261125161741`, `20261125173002`, `20261125173021`, `20261125191311`.
  - Die Filter der EF-Migration stimmen mit SQL/169 überein, z. B.
    `[IsCancelled] = 0 AND [IstAbschliessend] = 1 AND [WarehouseRequisitionItemId] IS NULL`.
  - Die Nummernlücke (164/165 nach 166/167) ist harmlos: `Update-IdealAkeWms.ps1:696-697` wählt nach MigrationId in
    Manifest-Reihenfolge.
- **H-3 SQL/169 Vorbelegung ohne Audit-Felder:** `UPDATE ProductionWorkplaces SET BdeStandardsichtTeile = 1` setzt kein `ModifiedAt`
  und kein `ModifiedBy`. Das ist bei einer Schema-Vorbelegung vertretbar, aber eine Abweichung von ADR 0003.
- **H-4 Tageshistorie und Nachtschicht:** Der Filter `[tag, tag+1)` auf StartedAt bzw. GemeldetAm (`BdeTageshistorie.cs`). Eine Schicht
  über Mitternacht erscheint nicht vollständig unter „heute“. Für AKE und IDEAL ohne Nachtschicht ist das heute unkritisch.
- **H-5 `ex.Message` an den Client:** „Serverfehler: …“ im BdeTerminalController und im Altbestand-Hinweis (H-B). Das ist Altbestand,
  gibt aber SQL- und Interna-Texte an Werker weiter.
- **H-6 operatorId nicht authentisiert:** Personalnummer-Login als Vertrauensmodell. Das Grundrauschen hinter B-2.
- **H-7 Nachtrag-Marker hängt am SyncLog:** Scheitert das Schreiben von „Run gestartet“, fehlt der Marker. Dann wiederholt sich der
  Lauf jeden Zyklus: idempotent, aber teuer. F-3 (Marker auch bei Totalausfall) wirkt in die andere Richtung, an einem Tag läuft gar
  nichts nach.
- **H-8 Pflichtmuster eingehalten:**
  - ISyncLogger als letzter Ctor-Parameter (Nachtrag, Lagerplatz-Anlage).
  - `SyncLogServices.All` und DisplayNames ergänzt (`SyncLogServices.cs:44,46,68,111-112`).
  - Deutsche Counts-Keys.
  - `ServiceSettingDefinitions.All`: `:292-293` und `:319-321`, Wertebereich Uhrzeit `:245`.
  - Choice mit `Werte` für Lagertechniken (ADR 0020 erfüllt).
  - Wirkungstexte und Warnhinweise je Schlüssel `:79-86`, `:169-171`.
  - Standortfeld bei `Sync:KommissionierNachtrag*` ist `null`. ADR 0020 verlangt die Angabe „beide“ (kleine Lücke).
- **H-9 BdeTerminalMaxHauptFa:**
  - Kein Leser mehr im Code (grep: nur Kommentar `AppSettingDefinitions.cs:235`, Changelog, Test-Zählung).
  - Die DB-Zeile bleibt und erscheint als verwaist.
  - Der alte Manifest-Eintrag `v1.64.0-MaxHauptFa` (`nachlauf.psd1:453-454`) fordert aber weiter „BdeTerminalMaxHauptFa = 5“ als
    Hand-Schritt. Er widerspricht damit `:707` und sollte entfernt oder als „entfällt seit 1.99.78“ markiert werden.
- **H-10 Gemeinsamer Code unauffällig:**
  - `site.js`: +22 Zeilen, nur ein delegierter Handler auf `[data-pfad-kopieren]`.
  - `site.css`: +3 Zeilen, nur `.fa-ag-markiert`.
  - `ColumnFilterTypes`: nur neue Register-Einträge (`ColumnFilterSyntax.cs:47-50`).
  - Layout: nur ein Menüpunkt hinter `NavZiele.BdeTageshistorie`.
  - Der Pfad-Link ist Razor-kodiert, `FileUri` escaped je Segment (`Helpers/FraesenPfad.cs:24-30`).
  - Keine Regression in Pagination und Spaltenfilter erkennbar.
  - F-11 (Glas-Zeilen in der Teileliste) ist fachlich offen, siehe Tabelle.

## Entscheidungstabelle

| Punkt | Frage | Empfehlung | Begründung |
|---|---|---|---|
| F-3 | Nachtrag-Marker auch bei Totalausfall setzen? | So lassen, aber bei `fehler > 0` eine Fehlermail bzw. Warnung im Protokoll | Ohne Marker läuft er jeden Zyklus erneut, das ist teurer als ein verpasster Tag. Der Ausfall muss aber sichtbar sein („melden statt still behandeln“). |
| F-4 | Nachtrag auf DEV schalten? | Auf DEV einmal von Hand einschalten (Uhrzeit = jetzt+1), danach wieder aus | Nur so ist der echte Nachtlauf einmal beobachtet. Der DEV-Guard verhindert sonst jeden Nachweis vor Prod. |
| F-11 | Lagerbestell-Zeilen ohne Bestellart-Filter (Glas = Typ 2 erscheint) | Filter auf Lagerbestellung (ohne Glas) einbauen, bevor eine Werkbank die Option bekommt | Glas wird nicht gefräst. Falsche Zeilen an der Fräse untergraben die Liste, die Option ist bis dahin aus. |
| F-14 | Einmal-Skript Fräsen-Pfad: Rückfragen 15/16 | Nur Trockenlauf, SCHARF erst nach Antwort des Fachbereichs | Die Laufwerksabbildung ist eine Annahme. Ein falscher Pfad wird massenhaft geschrieben, und das Skript überschreibt später nicht (nur `CamPath IS NULL`). |
| F-16 | `USER_OSBlechdicke` an IDEAL unbelegt | Prüfabfrage an IDEAL-Test mit berechtigtem Konto, vorher Nachlese an IDEAL aus | Feldbedeutung nicht aus dem Namen ableiten. Fehlt das Feld, scheitert die Nachlese. |
| F-20 | CSV-Export enthält Kindzeilen | Wie die Ansicht exportieren (nur sichtbare Zeilen), Kindzeilen höchstens als Option | Muster v1.47.0 „Druck/PDF nur sichtbare Spalten“. Abweichungen verwirren beim Abgleich. |
| F-21 | Summen-Checkbox meldet volle Zielmenge trotz Teilfertig | Rest statt Zielmenge melden (Soll − bereits gemeldet) | Sonst entsteht beim Fertigmelden aus der Summe eine Übermeldung. Das ist ein fachlicher Datenfehler, keine Kosmetik. |
| F-22 | Baugruppe 0/0 abgewiesen, Blech und Lagerbestellung erlauben es | Vereinheitlichen: 0/0 überall nur nach früheren Meldungen erlauben | Eine Regel an einem Ort (Muster ArbeitsgangStatusRegel). Unterschiedliches Verhalten je Zeilenart ist für Werker nicht erklärbar. |
| F-29 | Verwaiste Endpunkte (PausedBookings, ActiveBookings, StartProductionForOrder) | Entfernen | Toter Code ohne Wächter ist Angriffsfläche ohne Nutzen. „Löschen vor Hinzufügen“. |
| F-30 | Fotos 11-02/11-03 | Nach dem Prod-Deploy nachziehen, nicht blockierend | Reine Doku, die DoD ist bis auf zwei Fotos erfüllt. In der Lauf-Notiz als offen führen. |
| H-A | Terminal-Wächter auf Pause/Finish/FinishGroup/ReportPartial/CloseOthers | Nachziehen (Variante a aus B-2) | Die Mail und Werker gehen von „nur eigenes Terminal“ aus. Der vorhandene `FremdesTerminalAsync` macht es zu einem kleinen Fix. |
| H-B | `ex.Message` im Altbestand-Hinweis | Festen deutschen Text zeigen, Details ins Log | Interna gehören nicht in die Oberfläche. Der Hinweis bleibt sichtbar, also wird weiter gemeldet statt still behandelt. |
| H-C | SQL-Server-Version (`STRING_AGG`) und Mandant 1 | `SELECT @@VERSION` und `SELECT DISTINCT Mandant` je Sage-Instanz vor dem Einschalten | Nur prüfen, nicht annehmen. Es blockiert den Deploy nicht, weil der Schalter standardmäßig aus ist. |

## Ergebnis

NICHT DEPLOY-BEREIT: Schranke 2 (UAT + Merge) ist offen, B-2 (Terminal-Wächter vs. Mailzusage) ist nicht entschieden, und zwei
Prod-Vorbedingungen fehlen noch in der Checkliste (Nachtrag-Schalter vor dem ersten Dienststart aus, Wert von `BdeNurFaMeldung` an
beiden Standorten).
## Prod-Lesepruefung 2026-11-26 (Freigabe Gerald: lesend auf AKE- und IDEAL-Server)
Nur SELECT, Manifest-Pruefungen in einer Transaktion mit Rollback ausgefuehrt.
- **AKE-Prod** `AKESQL20.ake.at` / `IDEAL_AKE_WMS` (SQL 15.0.4430, letzte Migration `20261123124036_AddStockMovementWarehouseRequisitionItem`): keine Vorher-Pruefung schlaegt an (B-3 erledigt). Hinweise: DoppeltePrio 10, RolleFraesenpfadPflegen fehlt (Haltepunkt legt an), Lieferverzug Sammelliste/Absender.
- **IDEAL-Prod** `IdealSQL2.ideal.ideal-ake.at\SAGE100TEST` / `IDEAL_AKE_WMS` (SQL 15.0.4280 = 2019, gleiche letzte Migration; Standardinstanz `IdealSQL2` lehnt das Konto ab): keine Vorher-Pruefung schlaegt an, auch nicht `FreshInstallIndizesDuplikate`. Nachher-Hinweise: BeschichterKreditoren, KommissionierlisteLeitstandAus, VorfilterZiele, RolleFraesenpfadPflegen, BlechteileNachleseAus, Lieferverzug. Zwei Nachher-Pruefungen fehlen vor dem Schema-Update erwartungsgemaess (Spalten aus SQL/169).
- **BDE an beiden Prod-Standorten aus:** `BdeAktiv=false`, alle Mehrfach-Schalter und `BdeGleichzeitigerAbschlussBeiMehrfachStart=false`, `BdeNurFaMeldung=false`; verwaiste Zeile `BdeTerminalMaxHauptFa=5` an beiden.
- **H-C:** Sage IDEAL (DB `IDEAL`) liegt auf derselben Instanz SQL 2019 → STRING_AGG ok. AKESQL20 ebenfalls 2019.
- **F-16 belegt:** `KHKArtikel.USER_OSBlechdicke` (und USER_OSMaterial, USER_OSArbeitsschritte) existiert in Sage-Test `IDEAL_TEST_2026_05_03`.
- Entscheide Gerald 2026-11-26: 1C (Nachtrag-Default an lassen, Sofortlauf in Checkliste vermerken), 2A, 3A, 4 Rueckfrage (Restmenge vorschlagen, Ausschuss moeglich, 0 nach Teilmengen erlaubt), 6 gemeinsam auf Testsystem (Testsysteme stehen auf v1.99.62/63).
- **Entscheid 4 (Gerald 2026-11-26, F-22): bestaetigt.** Fertig-Dialog schlaegt nach Teilmeldungen die Restmenge (Soll minus gemeldet) als Gut vor; ein Teil davon kann als Ausschuss eingetragen werden (Gut+Ausschuss > Rest -> Rueckfrage „ueber Soll“); Gut 0 + Ausschuss 0 ist nur nach mindestens einer Teilmeldung erlaubt, sonst weiter verboten. **Doku-Pflicht in v1.99.81:** `docs/anwender` Kapitel BDE (11), Hilfeseite BDE, `docs/TESTSZENARIEN.md` + Testindex, Confluence-Seite 681017386 (Kapitelliste in der Lauf-Notiz), Anwender-Changelog.
- **Block 2:** H-C erledigt (SQL 2019 an beiden Standorten), F-16 erledigt (Feld belegt). Offen nur F-14/S-6 Fraesen-Pfad.
- **Entscheid 7 (Gerald 2026-11-26, F-14/S-6): B – Einmal-Skript Fraesen-Pfad an IDEAL sofort SCHARF** mit heutigen Annahmen (Pfad woertlich, „enthalten in“-Zeilen leer/gemeldet, G-BL und Twin uebernehmen); Antworten des Fachbereichs zu 15a/15b/16 kommen nach, Abweichungen von Hand. **Korrektur Laufwerksabbildung: `Y:\` = `\\idealfs1\Daten$\Produktion\CAM\Format4`** (nicht `…\CAM`). In v1.99.81 nachziehen: `SQL/Einmalig/2026-11-25_v1.99.72_FraesenPfad_Erstimport.sql` Z. 13 + 239, `Helpers/FraesenPfad.cs:11` (Fehlertext), `Views/Articles/_FraesenPfadPflegen.cshtml:11` (U-14), `Views/Help/Index.cshtml:586`, `docs/betrieb/03-installation-update-rollback.md:264`, `docs/TESTSZENARIEN.md` (TS-198), Manifest `nachlauf.psd1` (Text + Voraussetzung „vor SCHARF Antworten 15/16“ streichen, Hinweis SCHARF im Prod-Fenster).
- **Block 3 (Gerald 2026-11-26):**
  - **8 U-1: A** – saubere Meldung mit erklaerendem Hinweistext (was laeuft, warum nichts gebucht, was tun), laufende Buchung oben markieren/anspringen.
  - **9 U-5: B+Admin** – bei Pausierten, die nicht fortsetzbar sind, „Abschliessen“ anbieten (sonst „Fortsetzen“); Werkbank + Datum sichtbar; **zusaetzlich fuer BDE-Admins eine Kontroll-/Behebungsmoeglichkeit** (Liste verwaister Pausiert-Buchungen mit Abschliessen). Statuswechsel mit Audit-Feldern, Wachter wie H-A.
  - **10 U-7: A** – FA-Sicht ganzzahlig, Gruppen-Dialog `step=1`.
  - **11 U-11: A** – Aktionszelle nach oben, Felder per Zahnrad.
  - **12 U-13: A** – Werker-Hinweis kurz/zuklappbar „BDE-Admin informieren“, Link nur Admin; Admin-Waisen-Anzeige Backlog.
  - **Neu K-1:** Der BDE-Kopf (Werker/Werkbank/Meine Buchungen, `Views/BdeTeileliste/_Kopf.cshtml` + FA-Sicht) ist noch nicht einklappbar – in v1.99.81 einklappbar machen (Zustand je Geraet merken, laufende Buchung bleibt als Kurzzeile sichtbar).
