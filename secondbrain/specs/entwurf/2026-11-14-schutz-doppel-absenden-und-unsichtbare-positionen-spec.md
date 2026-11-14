---
type: spec
title: "Schutz gegen Doppel-Absenden und unsichtbare Positionen"
slug: 2026-11-14-schutz-doppel-absenden-und-unsichtbare-positionen-spec
status: Entwurf
created: 2026-11-14
updated: 2026-11-14
source_backlog: "[[2026-11-14-review-befunde-kommissionierung]]"
weitere_quellen:
  - "[[2026-11-14-review-befunde-wareneingang]]"
  - "[[2026-11-14-review-befunde-bde]]"
  - "[[2026-11-14-review-befunde-tabellen]]"
  - "[[01-kommissionierung]]"
  - "[[02-wareneingang-einkauf]]"
  - "[[03-ideal-hierarchie-beschichtung-bde]]"
  - "[[04-uebrige-module]]"
bug: "[[2026-11-14-schutz-doppel-absenden-und-unsichtbare-positionen-bug]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
version_vorschlag: "1.99.23"
sql_vorschlag: "150"
ts_vorschlag: "TS-138"
affected_code:
  - "IdealAkeWms/Services/Nummernkreis.cs (IstDeadlock :15 -> Ausnahme-Kette; neu IstSqlFehler fuer 1205/2601/2627)"
  - "IdealAkeWms/Services/PickingOrderWorkflow.cs (AbschliessenAsync: Statuspruefung im Versuch nach :174, Fehler-Weitergabe vor :234)"
  - "IdealAkeWms/Controllers/PickingLeitstandController.cs (ToggleRelease :360-424, Parameter release statt Umschalten :395)"
  - "IdealAkeWms/Views/PickingLeitstand/_PickingLeitstandRow.cshtml (Formular :240-268: hidden release, data-einmal-absenden)"
  - "IdealAkeWms/Views/PickingLeitstand/Index.cshtml (Freigabe-Modal :262-287: hidden release=true, data-einmal-absenden)"
  - "IdealAkeWms/Controllers/PurchaseOrdersController.Receive.cs (GET :15-35 und POST :42-146: Absendekennung)"
  - "IdealAkeWms/Models/PurchaseOrderReceipt.cs (neu Absendekennung Guid?)"
  - "IdealAkeWms/Models/ViewModels/PurchaseOrderReceiveViewModel.cs (neu Absendekennung)"
  - "IdealAkeWms/Data/ApplicationDbContext.cs (PurchaseOrderReceipt :1436-1464: gefilterter UNIQUE-Index)"
  - "IdealAkeWms/Data/Repositories/PurchaseOrderReceiptRepository.cs + Interface (neu AbsendekennungGebuchtAsync)"
  - "IdealAkeWms/Views/PurchaseOrders/Receive.cshtml (Formular :72: hidden absendekennung, data-einmal-absenden)"
  - "IdealAkeWms/Services/BdeBookingService.cs (ReportCompletionAsync :268-326: Applock je Arbeitsgang vor :290)"
  - "IdealAkeWms/wwwroot/js/bde-terminal.js (promptQty :450-475: btnQtyOk sperren)"
  - "IdealAkeWms/Views/Picking/Bom.cshtml (Vater-Position :223-238; set-status-btn :1668-1686 sperren)"
  - "IdealAkeWms/Models/ViewModels/BomViewModels.cs (BomViewModel :82: neu VaterPosition)"
  - "IdealAkeWms/wwwroot/js/site.js (neu: zentraler Doppel-Absenden-Schutz)"
  - "IdealAkeWms/wwwroot/js/csv-export.js (cleanValue :20-26, exportRows :127-135/:164)"
  - "IdealAkeWms/Program.cs (feste App-Kultur)"
  - "Migrations/<ts>_AddPurchaseOrderReceiptAbsendekennung.cs + SQL/150_AddPurchaseOrderReceiptAbsendekennung.sql + SQL/00_FreshInstall.sql (2 Stellen)"
  - "scripts/deploy/nachlauf.psd1 (SQL/150, Info Seiten neu laden)"
  - "Tests: PickingOrderWorkflowTests, PickingLeitstandControllerTests (:440-631), PurchaseOrdersReceiveTests, PickingControllerTests, NummernkreisTests (neu), IdealAkeWms.Tests/Js/csv-export.check.js"
  - "docs/TESTSZENARIEN.md (TS-138 neu, TS-86.5 anpassen), Views/Help/Index.cshtml, Views/Help/Changelog.cshtml, AppVersion.cs (Web + Service)"
  - "Bug: [[2026-11-14-schutz-doppel-absenden-und-unsichtbare-positionen-bug]]"
open_questions:
  - "Feste App-Kultur: de-DE (Empfehlung: ICU und NLS mit Tausenderpunkt, passt zu ColumnFilterSyntax.cs:340 und den vier Diensten mit festem de-DE) oder ein de-AT-Klon mit erzwungenem Tausenderpunkt? Reines de-AT scheidet aus (unter ICU U+00A0, DEV-Befund N-B4-2)."
  - "Feste Kultur auch im Windows-Dienst (IDEALAKEWMSService/Program.cs)? Empfehlung: nicht in dieser Spec – die Parse-/Format-Wege des Dienstes sind nicht geprüft; eigene Aufgabe."
  - "Prüfschritt idealweb01 (akenet01 ist mit „4.500,000“ belegt): FA-Liste, Spalte „Stk.“, eine Menge ab 1.000 ansehen – Punkt als Tausender und Komma als Dezimal? Bei Komma als Tausender (en-US) STOPP: dann ändert die feste Kultur das Lesen aller Dezimal-Eingaben."
  - "Client-Sperre: nur an den genannten Formularen per Attribut data-einmal-absenden (Empfehlung) oder global für alle POST-Formulare mit Opt-out?"
  - "Wareneingang: POST ohne Absendekennung (Seite vor dem Update geöffnet, Handgerät mit altem Stand) – ablehnen mit Meldung „Seite neu laden“ (Empfehlung) oder ohne Doppelschutz buchen?"
  - "CSV: ganze Zahlen mit Tausenderpunkt (1.234, ohne Dezimalteil) in Spalten OHNE data-filter-type=number – unverändert lassen (Empfehlung, Verwechslung mit Positionen/Nummern) oder ebenfalls ohne Punkt exportieren?"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: true
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

# Schutz gegen Doppel-Absenden und unsichtbare Positionen

Querschnitts-Bugfix für die DEV-Regression vom 2026-11-13 (Bündel v1.99.22, 90bbbbd7). Bug-Datensatz:
[[2026-11-14-schutz-doppel-absenden-und-unsichtbare-positionen-bug]]. Pfade sind Worktree-relativ
(`.claude/worktrees/2026-08-07-ideal-teile-1-5`); alle Zeilen am 2026-11-14 im Worktree nachgelesen.

## Ziel / Nutzen (das Warum)

Fünf Schreibwege führen eine Anfrage zweimal aus, wenn sie zweimal ankommt (Doppeltipp am Terminal, Netz-Wiederholung am
Handgerät, zweite Person, veraltete Seite): zweiter Folgeauftrag, stornierte Aufträge, doppelte Einbuchung mit zwei Sage-Belegen,
doppelte Fertigmeldung. Alle haben dieselbe Ursache: Der Server prüft den Ausgangszustand nicht dort, wo geschrieben wird, und
der Client sperrt den Knopf nicht. Das Muster wird **einmal** gelöst (Regel + zwei gemeinsame Bausteine), die Stellen wenden es an.
Dazu zwei Anzeige-Fehler, die in derselben Regression aufgefallen sind: unsichtbare Unterpositionen im Kommissionierauftrag (N-1)
und kulturabhängige Zahlen im CSV-Export (N-B4-2).

## Umfang (In-Scope / Out-of-Scope)

**Im Umfang** (Belege im Bug-Datensatz und in den Quell-Notizen):

| ID | Kurz | Quelle |
|---|---|---|
| K-1 + N-2 | Abschluss „Teilweise“ doppelt: Deadlock wird nicht wiederholt (500); wird er wiederholt, fehlt die Statusprüfung → zweiter Folgeauftrag | [[2026-11-14-review-befunde-kommissionierung]], [[01-kommissionierung]] |
| N-2 | Parallele Freigabe: 500, FA freigegeben ohne Auftrag; betrifft auch den BA-Nummernkreis | [[01-kommissionierung]] |
| K-6 | Freigabe-Knopf ist Umschalter; veraltete Seite/Doppelklick storniert Aufträge | [[2026-11-14-review-befunde-kommissionierung]] |
| WE-1 / WE-4 | Wareneingang doppelt gebucht; Barcode doppelt (Ursache meist WE-1) | [[2026-11-14-review-befunde-wareneingang]] |
| BDE-1 | Doppelte AG-Fertigmeldung | [[2026-11-14-review-befunde-bde]] |
| N-1 | Unterpositionen ohne Vater-Zeile im KO-Auftrag unsichtbar | [[01-kommissionierung]] |
| N-B4-2 | CSV-Zahlen abhängig von der Server-Kultur; dazu feste App-Kultur | [[2026-11-14-review-befunde-tabellen]], [[04-uebrige-module]] |

**Nicht im Umfang** (bleiben in den Quell-Notizen offen, eigene Specs):
- Kommissionierung: K-2 (Zeilen ohne Stücklisten-Treffer – N-1 löst nur die *vorhandenen* Zeilen ohne Vater), K-3, K-4, K-5, K-7
  (RowVersion → 500; bleibt in Restfenstern dieser Spec sichtbar), K-8, K-9, K-10, K-11, K-12 (nur soweit hier Tests entstehen),
  K-13, K-14, K-15, K-16, N-3, N-4, N-5, N-6; alle B-Befunde aus [[2026-11-14-review-befunde-kommissionierbereiche]].
- Wareneingang: WE-2 (nicht atomares Speichern – siehe Hinweis in „Technischer Lösungsentwurf“ C3), WE-3, WE-5, WE-6, N-7, N-8, N-9.
- BDE: BDE-2, N-B3-5; Doppel-Schutz der übrigen BDE-Aktionen (Start, Stop, Unterbrechen) über den Knopf hinaus.
- Tabellen: N-B4-3. Feste Kultur im Windows-Dienst (Rückfrage 2). Doppel-Reklamation im Beschichtungsauftrag.

## Fachliche Anforderungen

1. Ein zweites Absenden derselben Handlung schreibt **nichts** und zeigt eine sichtbare Meldung „bereits …“ – nie still, nie HTTP 500.
2. Eine Leitstand-Seite mit veraltetem Stand kann eine Freigabe nicht ungewollt zurücknehmen (oder umgekehrt); sie meldet den
   tatsächlichen Stand.
3. Zwei gleichzeitige Freigaben bzw. Abschlüsse enden mit genau einem Ergebnis (ein Satz Aufträge, höchstens ein Folgeauftrag) und
   ohne 500er.
4. Knöpfe, die eine Buchung auslösen, sind nach dem ersten Klick bis zur Antwort gesperrt (Komfort, nicht der Schutz).
5. Der Kommissionierauftrag zeigt **jede** seiner Zeilen, auch wenn die Baugruppe (Vater-Position) in einem anderen Auftrag liegt.
6. Der CSV-Export schreibt Mengen ohne Tausendertrenner mit Dezimalkomma, unabhängig von der Kultur des Servers; die Web-App läuft
   mit einer festen Kultur.

## Technischer Lösungsentwurf

### A. Das gemeinsame Muster (einmal, als Regel – Dev-Lauf schreibt ADR 0026)

> [!important] Regel „Absicht statt Umschalter, Prüfung am Schreibort“
> 1. Der Client sendet die **Absicht** (Zielzustand oder eine Kennung des Absendens), nie „dreh um“.
> 2. Der Server prüft den **Ausgangszustand dort, wo geschrieben wird** – in der Transaktion bzw. unter Sperre und in **jedem**
>    Wiederholversuch erneut.
> 3. Passt der Ausgangszustand nicht: nichts schreiben, Meldung „bereits …“. MVC-Formular → `TempData["WarningMessage"]` +
>    Redirect; AJAX → die Server-Meldung im vorhandenen Kanal der Seite (Kommissionierung `alert` mit Text, BDE-Toast).
> 4. Die Knopf-Sperre im Browser ist Komfort und nie der einzige Schutz (Desktop-Chrome unterdrückt den Doppelklick schon heute,
>    Netz-Wiederholung und zweites Terminal nicht – WE-1/BDE-1-Messung).

Wo der Zustand liegt, bestimmt den Mechanismus – deshalb je Stelle ein anderes, aber immer dasselbe Prinzip:

| Stelle | Absicht | Prüfung am Schreibort | DB-Schutz für den echten Gleichstand |
|---|---|---|---|
| K-1 Abschluss | Auftrag-Id (vorhanden) | Status im Versuch nach dem Neulesen | Serializable + RowVersion (vorhanden) + Wiederholung (N-2) |
| K-6 Freigabe | `release=true/false` | `release == ps.IsReleasedForPicking` → Meldung | Erzeugung idempotent (vorhanden) + Wiederholung (N-2) |
| WE-1 Wareneingang | `absendekennung` (Guid aus dem GET) | Kennung schon gebucht → Meldung | gefilterter UNIQUE (`Absendekennung`, `BelPosId`), SQL/150 |
| BDE-1 Fertigmeldung | Arbeitsgang-Id (vorhanden) | `IsFinal`-Prüfung (vorhanden) **unter** Applock | `sp_getapplock` je Arbeitsgang, Transaktions-Besitz |

### B. Gemeinsame Bausteine

**B1 – Datenbankfehler über die ganze Ausnahme-Kette erkennen (N-2).** `Nummernkreis.cs:15` prüft nur `ex` und
`ex.InnerException`. Neu `Nummernkreis.IstSqlFehler(Exception ex, params int[] nummern)` läuft die `InnerException`-Kette ab
(`for (var e = ex; e != null; e = e.InnerException)`); `IstDeadlock(ex) => IstSqlFehler(ex, 1205)`. Damit greift der Catch-Filter `:28`
auch bei der EF-Verpackung `InvalidOperationException` → `DbUpdateException` → `SqlException 1205`, und der Wiederholversuch läuft.
Wirkt ohne weitere Änderung auf `PickingOrderWorkflow` (`:100`, `:170`) und `BeschichtungsauftragWorkflow` (`:362`, `:365`, `:380`, `:400`)
– dort wird ein verpackter Deadlock künftig zur Klartext-Meldung statt 500. WE-1 nutzt denselben Helfer für 2601/2627.

**B2 – Knopf-Sperre zentral in `site.js`.** Gesucht: `site.js` hat keinen Submit-Schutz (Grep `submit|disabled = true|data-confirm`: kein
Treffer); vorhanden sind nur seitenlokale Sperren (`Bom.cshtml:1438`, `:1895`, `PickingLeitstand/Index.cshtml:474`, `:756`). Neu in
`site.js` (wird über `_Layout.cshtml:466` auf jeder Seite vor den Seiten-Skripten geladen, auch am BDE-Terminal):
- **Formulare:** ein `submit`-Listener am `document` (Bubbling, läuft also **nach** seitenlokalen Listenern) für
  `form[data-einmal-absenden]`: Läuft das Formular schon → `preventDefault()` (zweites Absenden verworfen). Hat ein Seiten-Listener
  abgebrochen (`e.defaultPrevented`) → nichts sperren. Sonst Formular markieren und die Submit-Knöpfe **per `setTimeout(…, 0)`**
  sperren (`disabled` + `aria-busy="true"`) – sofortiges `disabled` nähme Name/Wert des auslösenden Knopfs aus den Formulardaten.
  `pageshow` mit `persisted` (Zurück-Taste, bfcache) hebt Markierung und Sperre auf.
- **AJAX-Knöpfe:** `window.wmsSperren(knoepfe)` sperrt die Knöpfe und gibt eine Freigabe-Funktion zurück; sind sie schon gesperrt,
  Rückgabe `null` (Aufrufer bricht ab). Der Aufrufer gibt bei Fehler frei; bei Erfolg navigiert die Seite bzw. schließt das Modal.

Opt-in per Attribut (Rückfrage 4): Seiten, die eine Datei herunterladen oder in neuem Tab drucken, laden nicht neu – eine globale
Sperre ließe dort die Knöpfe gesperrt stehen.

### C. Anwendung je Befund

**C1 – K-1 + N-2, Abschluss.** In `AbschliessenAsync` nach dem Neulesen (`PickingOrderWorkflow.cs:174`) dieselbe Statusprüfung wie
`:164`: ist der Auftrag nicht mehr `Offen`/`InArbeit`, `lauf.Fehler = "Auftrag … ist bereits … – nichts geändert."` und ohne
`SaveChanges` zurück (die leere Transaktion committet nichts). Nach `MitVersuchenAsync` (`:226`): steht `ergebnis.Fehler`, wird er als
`r.Fehler` zurückgegeben, **bevor** der FA-Status-Block `:234-242` läuft. Der Text der Vorprüfung `:166` und der Prüfung im Versuch
kommt aus einer lokalen Funktion (ein Wortlaut). Controller `PickingController.cs:480` gibt `BadRequest(Fehler)` zurück, die Seite zeigt
ihn per `alert` (`Bom.cshtml:1682-1684`) – sichtbar, keine Änderung nötig. Client: `.set-status-btn` (`Bom.cshtml:1668`) beim Klick
über `wmsSperren` beide Knöpfe sperren, im `error`-Zweig freigeben.

**C2 – K-6, Freigabe mit Zielzustand.** `ToggleRelease(int id, bool? release, int? assignedPickerId, string? returnUrl)` – Name der
Aktion bleibt (Routen, Tests, 301 aus `ProductionOrdersController.cs:62`). Nach dem Lesen von `ps` (`:374`) und vor allen anderen
Prüfungen:
- `release == null` → Warnung „Die Seite ist veraltet – bitte neu laden. Es wurde nichts geändert.“, Redirect.
- `release == ps.IsReleasedForPicking` → Warnung „FA {n} ist bereits freigegeben – nichts geändert.“ bzw. „FA {n} ist nicht (mehr)
  freigegeben – nichts geändert.“, Redirect, **kein** `SetReleaseAsync`, **keine** Stornierung.
- sonst `newReleased = release.Value` statt `!ps.IsReleasedForPicking` (`:395`), Rest unverändert.
Views: Zeilen-Formular `_PickingLeitstandRow.cshtml:240` bekommt `<input type="hidden" name="release" value="@(!item.IsReleasedForPicking)">`
(lowercase `true`/`false`) und `data-einmal-absenden`; Modal-Formular `Index.cshtml:262` `release=true` und `data-einmal-absenden`.
`BulkRelease` sendet `release` bereits (`:429`) – unverändert. Restfenster: zwei Personen nehmen gleichzeitig zurück → beide bestehen die
Prüfung, die zweite storniert nichts mehr (nur noch Offen/InArbeit, `:255-256`); ein RowVersion-Konflikt dabei bleibt K-7.

**C3 – WE-1 / WE-4, Absendekennung.** Verifiziert: Die Review-Fundstelle `:190-293` passt nicht zur Datei (202 Zeilen); der POST steht
`PurchaseOrdersController.Receive.cs:42-146`, `Guid.NewGuid()` `:108`, Buchung je Zeile `:141`.
- Modell `PurchaseOrderReceipt.Absendekennung` (`Guid?`), gesetzt an **jedem** Receipt eines Absendens (auch `NichtNoetig`/`NurInSage` –
  deshalb nicht `SageWeVorgang`, das bei Sage-WE aus `NULL` ist, `:108`, `:137`, und eine eigene Bedeutung trägt).
- ViewModel `Absendekennung`: GET `:28` → `Guid.NewGuid()`; POST übernimmt den geposteten Wert, damit eine Korrektur nach
  Prüffehler (`:100`, nichts gebucht) mit derselben Kennung absendet. View `Receive.cshtml:72`: Hidden-Feld + `data-einmal-absenden`.
- POST: Kennung fehlt oder `Guid.Empty` → Rückfrage 5 (Empfehlung: Warnung „Seite neu laden“, nichts gebucht). Direkt nach `vm != null`
  (`:49-53`), **vor** der Zeilenprüfung: `await _receipts.AbsendekennungGebuchtAsync(k)` → Warnung „Diese Eingabe wurde bereits gebucht –
  nichts doppelt gebucht. Bitte den Stand der Bestellung prüfen.“ + Redirect `Receive(belId)`. Vor der Zeilenprüfung, weil der zweite POST
  sonst am inzwischen kleineren Rest scheitert („Überlieferung bitte bestätigen“) statt „bereits gebucht“ zu sagen.
- Echter Gleichstand: gefilterter UNIQUE-Index (`Absendekennung`, `BelPosId`) `WHERE Absendekennung IS NOT NULL`. Eindeutig je
  Absenden, weil eine Maske je `BelPosId` genau eine Zeile hat (`SagePurchaseOrderLines.BelPosId` UNIQUE, `ApplicationDbContext.cs:1413`;
  Eingaben je `BelPosId` gruppiert, `Receive.cs:154`). Die Schleife `:109-142` fängt `DbUpdateException` mit
  `Nummernkreis.IstSqlFehler(ex, 2601, 2627)` → dieselbe Warnung; andere Fehler werfen weiter. Der Sage-Queue-Decorator reiht erst
  **nach** erfolgreichem `base.AddAsync` ein (`SageBookingEnqueueingStockMovementRepository.cs:43`, `:69`) – die abgewiesene Zeile
  erzeugt keinen Queue-Eintrag.
- WE-4: Doppel-Barcode aus demselben Absenden ist damit ausgeschlossen. Restfenster: zwei **verschiedene** Absenden mit demselben
  Aufkleber gleichzeitig (Prüfung `:91` ohne Sperre) – bleibt, in der Hilfe nicht erwähnt, im Brain als Restrisiko.
- Hinweis WE-2 (nicht im Umfang): Scheitert Zeile 3 nach gespeicherten Zeilen 1–2, weist ein erneutes Absenden derselben Seite mit
  „bereits gebucht“ ab, obwohl Zeile 3 fehlt. Die Meldung sagt deshalb „Stand prüfen“; Neuladen holt eine neue Kennung.

**C4 – BDE-1, Applock je Arbeitsgang.** In `ReportCompletionAsync` als erster Schritt im `InTransactionAsync`-Lambda (vor `:273`,
spätestens vor `:290`), nur relational (dieselbe Provider-Weiche wie `:574`): `sp_getapplock @Resource = 'BDE-Fertigmeldung:<WorkOperationId>',
@LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000`. Die zweite Anfrage wartet, bis die erste committet; ihr
`AnyAsync` `:290` liest danach den committeten Stand (ReadCommitted bzw. RCSI liest je Anweisung, Transaktion `:577` ohne Isolationsangabe)
und meldet die vorhandene Meldung „Arbeitsgang ist bereits fertig gemeldet.“ (Toast `bde-terminal.js:442-445`). Rückgabewert < 0
(Zeitüberschreitung) → `BdeBookingResult.Invalid("Arbeitsgang wird gerade an einem anderen Terminal gemeldet – bitte erneut versuchen.")`.
Kein Index möglich: `IsFinal` liegt an `BdeBookingQuantities`, der Arbeitsgang an `BdeBookings`, stornierte Buchungen zählen nicht
(`IX_BdeBookingQuantities_Booking_Final` gilt je Buchung, `SQL/00_FreshInstall.sql:2585`). Client: `btnQtyOk.onclick`
(`bde-terminal.js:466`) zuerst `wmsSperren(this)`, `promptQty` gibt den Knopf beim Öffnen wieder frei (vor `modal.show()` `:463`) – wirkt
auf jede Mengen-Bestätigung, nicht nur die Fertigmeldung.

**C5 – N-1, Vater-Position nur, wenn vorhanden.** Heute: Vater-Position aus dem Positionstext (`Bom.cshtml:224-227`), Zeile mit
`TreeLevel > 0` startet versteckt (`:238`, `TreeLevel` = Anzahl Punkte, `PickingController.cs:332`) und wird nur sichtbar, wenn
`expandedState[parentPos]` (`:954`) – im Auftragsmodus fehlen Vater-Zeilen (`PickingController.cs:291-293`). Neu
`BomViewModel.VaterPosition(BomItemViewModel item)` (`BomViewModels.cs:82`): nächster **vorhandener** Vorfahre in `Items` (Positionen
einmal als `HashSet`, Punkt-Präfixe von hinten), sonst `null`. View: `data-parent-pos` nur bei Vater, `display:none` nur bei Vater
(statt `TreeLevel > 0`); Einrückung `:257` bleibt `TreeLevel`. Die Stückliste ohne Lücken verhält sich unverändert (Vater immer
vorhanden); fehlt nur die direkte Vater-Zeile, hängt die Zeile unter dem Großvater und erscheint mit dessen Aufklappen. „Alle aufklappen“
(`:1042-1050`) und die Klapp-Rekursion (`:984-1011`, über `data-parent-pos`) brauchen keine Änderung.

**C6 – N-B4-2, CSV und App-Kultur.**
- *App-Kultur* (Rückfrage 1, Empfehlung `de-DE`): `Program.cs` setzt heute keine Kultur (Grep `DefaultThreadCurrent|RequestLocalization`
  im Worktree: kein Treffer; keine `InvariantGlobalization`/`UseNls` in csproj/json). Neu am Anfang von `Program.cs`
  `CultureInfo.DefaultThreadCurrentCulture`/`…UICulture` = App-Kultur und in der Pipeline `app.UseRequestLocalization(...)` mit genau
  dieser einen Kultur (Browser-Sprache wählt nichts anderes). Begründung `de-DE`: Tausenderpunkt unter ICU **und** NLS; der Server-Filter
  liest Zahlen mit Punkt als Tausender (`ColumnFilterSyntax.cs:340`, `:349`) – mit U+00A0 aus ICU-`de-AT` scheitert er; vier Dienste formatieren
  schon fest `de-DE` (`KommissionierListenService.cs:61`, `VormontageService.cs:70`, `WarehousePickingPrintLayout.cs:136`,
  `BeschichtungsauftragService.cs:59`). Monatsnamen kommen in Web-Views nicht vor (Grep `MMMM|MMM|ToLongDateString`: nur Wochentag
  `OseonReporting/OperationsOverview.cshtml:160`, in beiden Kulturen gleich). Folge: Model-Binding von `decimal` (fallstricke
  „keine feste Request-Kultur“) liest überall deutsch – auf akenet01 unverändert (Tausenderpunkt belegt), idealweb01 Prüfschritt (Rückfrage 3).
- *CSV:* `cleanValue(raw, zahlSpalte)` (`csv-export.js:20`): nach der Whitespace-Normalisierung `:21` (macht U+00A0/U+202F zu Leerzeichen)
  eine Gruppen-Zahl `^[+-]?\d{1,3}([. ]\d{3})+(,\d+)?$` ohne Trenner schreiben – **mit** Dezimalkomma immer, **ohne** Dezimalteil nur in
  Spalten mit `<th data-filter-type="number">` (Rückfrage 6). Danach greift die Injection-Prüfung `:24` wie bisher (`-10026,000` ist
  `PLAIN_NUMBER`, kein Apostroph). `exportRows` (`:127-135`) merkt sich je Spalte `th.getAttribute('data-filter-type') === 'number'` und
  reicht es an `:164` weiter; Kopfzeile und Gruppenschlüssel (`:165`, `:167`) ohne. Unabhängig von der App-Kultur, damit auch alte
  Bildschirmstände und künftige Kulturwechsel richtig exportieren.

## Migrations-/SQL-Auswirkungen

- **Model** `PurchaseOrderReceipt.Absendekennung` (`Guid?`), `ApplicationDbContext` (`:1436-1464`):
  `HasIndex(e => new { e.Absendekennung, e.BelPosId }).IsUnique().HasFilter("[Absendekennung] IS NOT NULL").HasDatabaseName("IX_PurchaseOrderReceipts_Absendekennung_BelPosId")`.
- **EF-Migration** `AddPurchaseOrderReceiptAbsendekennung`.
- **`SQL/150_AddPurchaseOrderReceiptAbsendekennung.sql`** (nächste freie Nummer, Worktree `SQL/149_WidenBelegnummer45.sql` ist die
  letzte; der Dev-Lauf prüft erneut – die Bestellvorschläge-Spec E5 braucht ebenfalls „150 oder höher“): Spalte mit
  `COL_LENGTH('dbo.PurchaseOrderReceipts','Absendekennung') IS NULL`-Guard in eigenem Batch; Index mit `sys.indexes`-Guard in eigenem Batch;
  `__EFMigrationsHistory`-Insert in separatem Batch. Additiv, nicht destruktiv, bestehende Zeilen bleiben `NULL` (vom Filter ausgenommen).
- **`SQL/00_FreshInstall.sql`** an beiden Stellen (Spalte + Index im Tabellen-Block, `MigrationId`).
- **`SQL/AgentJobs/`**: nicht betroffen (Grep `PurchaseOrderReceipts|BdeBookingQuantities`: kein Treffer).
- BDE-1, K-1, K-6, N-1, N-B4-2: keine Schema-Änderung.

## Einstellungen

keine. (Die App-Kultur ist eine feste Code-Konstante, keine Einstellung: ein Wert, der sich nie ändern soll.)

## Audit-Feld-Auswirkungen

- Neue Receipts: Audit wie bisher (`CreatedAt/By/ByWindows`, `Receive.cs:138`); die neue Spalte ist kein Audit-Feld.
- Abgewiesene Doppelversuche (K-1, K-6, WE-1, BDE-1) schreiben **nichts** – auch keine `Modified*`-Felder.
- K-1 im erfolgreichen Wiederholversuch: Audit wie bisher (`PickingOrderWorkflow.cs:178`, `:221`).
- Keine Änderung an `AuditableEntity` oder Service-Namen.

## Rollen / Zugriffsfilter

Unverändert: `ToggleRelease` `[RequireLeitstandAccess]`, `AuftragAbschliessen` `[RequirePickingAccess]` (`PickingController.cs:476`),
`Receive` POST `[RequireStockAccess]` (`Receive.cs:42`), `ReportCompletion` hinter `RequireBdeActive` + `RequireBdeUserAccess`. Kein neuer
Filter, keine neue Rolle. Listen-View-Pattern: keine neue Tabellenansicht.

## Akzeptanzkriterien

1. **AK-1 (N-2):** `Nummernkreis.IstDeadlock` erkennt `SqlException 1205` in jeder Tiefe der `InnerException`-Kette; eine verpackte
   Ausnahme (`InvalidOperationException` → `DbUpdateException` → 1205) löst in `MitVersuchenAsync` einen Wiederholversuch aus (Unit-Test,
   SqlException per Reflection; falls nicht erzeugbar: Manual-UAT AK-11).
2. **AK-2 (K-1):** Ist der Auftrag zwischen Vorprüfung und Versuch abgeschlossen worden, liefert `AbschliessenAsync` den Fehler „… ist
   bereits … – nichts geändert.“, legt **keinen** Folgeauftrag an und ändert weder Kopf, Zeilen noch FA-Status (Unit-Test mit Repository-Doppel,
   das im ersten Versuch den Auftrag abschließt und eine `DbUpdateConcurrencyException` wirft).
3. **AK-3 (K-1):** Zwei Aufrufe nacheinander auf denselben Auftrag: zweiter Aufruf Fehler, genau ein Folgeauftrag (Unit-Test).
4. **AK-4 (K-6):** `ToggleRelease` mit `release=true` auf einem freigegebenen FA → `WarningMessage` „… bereits freigegeben – nichts
   geändert.“, kein `SetReleaseAsync`, kein Auftrag storniert, Kommissionierer unverändert; `release=false` auf nicht freigegebenem FA analog;
   ohne `release` → „Seite ist veraltet …“, keine Änderung (Controller-Tests). Bestehende `ToggleRelease_*`-Tests senden `release`.
5. **AK-5 (K-6):** Beide Leitstand-Formulare (Zeile, Modal) senden `release` und tragen `data-einmal-absenden` (Wächter-Test auf die
   `.cshtml`-Texte oder Sichtprüfung TS-138.2).
6. **AK-6 (WE-1):** Zweiter POST mit derselben `absendekennung` (nacheinander) → `WarningMessage` „… bereits gebucht …“, keine neue
   `StockMovement`, kein neuer Receipt (Controller-Test InMemory, App-Layer-Prüfung).
7. **AK-7 (WE-1):** Jeder Receipt eines Absendens trägt dieselbe `Absendekennung`, auch bei Sage-WE aus (`NichtNoetig`) und bei
   `NurInSage`-Zeilen (Unit-Test).
8. **AK-8 (WE-1):** POST ohne Kennung → gemäß Rückfrage 5 (Empfehlung: Warnung, nichts gebucht) (Controller-Test).
9. **AK-9 (WE-1, Gleichstand):** Zwei **gleichzeitige** POSTs mit derselben Kennung → genau ein Wareneingang, der zweite sieht die
   Warnung, kein HTTP 500, kein zweiter Sage-Queue-Eintrag (**Manual-UAT**, UNIQUE-Index nicht InMemory-prüfbar – fallstricke §8).
10. **AK-10 (BDE-1):** Zwei gleichzeitige `ReportCompletion` für denselben Arbeitsgang → in 6 von 6 Läufen genau eine `IsFinal`-Buchung, die
    zweite Anfrage meldet „bereits fertig gemeldet“ (**Manual-UAT**, Applock nicht InMemory-prüfbar). Nacheinander (InMemory): bestehender
    Schutz bleibt grün.
11. **AK-11 (N-2/K-1, Gleichstand):** Zwei gleichzeitige `ToggleRelease` (verschiedene FAs) und zwei gleichzeitige Abschlüsse „Teilweise“
    desselben Auftrags → kein HTTP 500, jeder FA hat seine Aufträge, genau ein Folgeauftrag (**Manual-UAT**, Deadlock nicht InMemory-prüfbar).
12. **AK-12 (Client):** An `form[data-einmal-absenden]` sendet ein Doppelklick bzw. doppeltes Enter genau einen POST; der Name/Wert des
    auslösenden Knopfs kommt an; nach Zurück-Taste ist der Knopf wieder bedienbar; ein Seiten-Listener, der das Absenden abbricht, lässt den
    Knopf frei. `.set-status-btn` und `btnQtyOk` sind nach dem ersten Klick gesperrt (Manual-UAT, Netzwerk-Drosselung in DevTools).
13. **AK-13 (N-1):** `BomViewModel.VaterPosition` liefert den nächsten vorhandenen Vorfahren bzw. `null` (Unit-Tests: `3.1` ohne `3` → `null`;
    `3.1.2` mit `3`, ohne `3.1` → `3`; `3.1` mit `3` → `3`). Die Auftragsseite zeigt alle Zeilen eines Auftrags ohne Vater-Zeilen sichtbar
    (Manual-UAT IDEAL KO mit Sub-FA-Baugruppen, AKE Glas-/VK-Auftrag); die volle Stückliste klappt wie bisher.
14. **AK-14 (N-B4-2, CSV):** `cleanValue('-10 026,000')` → `-10026,000`, `cleanValue('10.026,000')` → `10026,000`,
    `cleanValue('-1.234,500')` → `-1234,500` (kein Apostroph); `cleanValue('1.234')` ohne Zahl-Spalte → `1.234`, mit Zahl-Spalte → `1234`;
    Injection-Fälle `csv-export.check.js:9-14` unverändert (Node-Selbstprüfung `Kern_NodeSelbstpruefung`).
15. **AK-15 (App-Kultur):** Die Web-App rendert unabhängig von der Server-Kultur `1.234,500` (Tausenderpunkt, Dezimalkomma) und liest
    Dezimal-Eingaben deutsch; auf AKE-DEV zeigt `/StockOverview` Artikel 00000905 `-10.026,000` statt `-10 026,000` (Manual-UAT).
16. **AK-16:** Build und alle Tests (Web + Service) grün; Version 1.99.23 (bzw. nächste freie) in beiden `AppVersion.cs`, Anwender-Changelog,
    Hilfe (CSV-Zahlen, Meldungen „bereits …“), `docs/TESTSZENARIEN.md` TS-138 + TS-86.5 angepasst.

## Test-Szenarien

Neu **TS-138 — Doppel-Absenden und unsichtbare Positionen (v1.99.23)** in `docs/TESTSZENARIEN.md`; TS-86.5 ändern: „`1.234,500` am
Bildschirm steht in der CSV als `1234,500` und ist eine Zahl“. Danach [[testszenarien-index]] nachziehen. Vorbedingung für alle: Bündel
v1.99.23 auf DEV AKE und DEV IDEAL, SQL/150 eingespielt, Benutzer mit `leitstand` + `picking` + `stock`.

- **TS-138.1 — Abschluss „Teilweise“ doppelt (AKE, beide).** Auftrag mit einer umgebuchten Zeile Ist < Soll. (a) Im Modal zweimal schnell
  „Teilweise“ → Knöpfe nach dem ersten Klick gesperrt, ein Folgeauftrag. (b) Zwei gleichzeitige `POST /Picking/AuftragAbschliessen`
  (zwei Tabs bzw. `fetch` wie in [[01-kommissionierung]]) → kein 500, genau ein Folgeauftrag, zweite Antwort „… bereits teilweise
  abgeschlossen – nichts geändert.“ Prüf-SQL: `SELECT COUNT(*) FROM PickingOrders WHERE UrsprungsauftragId = <Id>` = 1. Negativ: Mischung
  „Teilweise“ + „Vollständig“ aus zwei Tabs → ein Ergebnis, zweite Meldung „bereits …“.
- **TS-138.2 — Freigabe mit veralteter Seite (AKE).** Tab 1 und Tab 2 auf dem Leitstand, FA nicht freigegeben. Tab 1 „Freigeben“ → Aufträge
  angelegt. Tab 2 (ohne Neuladen) „Freigeben“ → Warnung „FA … ist bereits freigegeben – nichts geändert.“, Aufträge **nicht** storniert,
  Kommissionierer unverändert. Umgekehrt: Tab 1 „Freigabe zurücknehmen“, Tab 2 „Freigabe zurücknehmen“ → zweite Meldung „nicht (mehr)
  freigegeben“. Doppelklick auf „Freigeben“ → ein POST. Negativ: Formular ohne `release` (DevTools Feld entfernen) → „Seite ist veraltet“.
- **TS-138.3 — Parallele Freigaben (AKE, Manual-UAT).** Zwei gleichzeitige `ToggleRelease release=true` für zwei FAs, 4 Läufe → kein 500,
  jeder FA hat Aufträge (Prüfabfrage N-6 in [[01-kommissionierung]] leer für die Test-FAs: kein freigegebener FA ohne Auftrag).
- **TS-138.4 — Wareneingang doppelt (AKE).** Bestellung mit Rest ≥ 10. (a) Doppelklick „Einbuchen“ → ein POST, ein Receipt. (b) Zwei
  gleichzeitige identische POSTs (gleiche Kennung) → ein Receipt, eine Bewegung, zweite Antwort Warnung „… bereits gebucht …“, kein 500.
  (c) Nach der Buchung Zurück-Taste und erneut „Einbuchen“ → Warnung, nichts gebucht. (d) Mit Barcode: (b) wiederholen → ein Receipt mit
  Barcode. Negativ: Prüffehler (Menge „abc“) → Seite mit Fehlern, korrigieren, Einbuchen → **eine** Buchung (Kennung bleibt gültig); POST
  ohne Kennung → Meldung gemäß Rückfrage 5. Prüf-SQL: `SELECT Absendekennung, COUNT(*) FROM PurchaseOrderReceipts WHERE Absendekennung IS NOT NULL GROUP BY Absendekennung, BelPosId HAVING COUNT(*) > 1` = leer. Am Handgerät: Netz kurz trennen während „Einbuchen“, Seite erneut senden → Warnung.
- **TS-138.5 — Doppelte AG-Fertigmeldung (IDEAL, Werkbank „nur AG-Fertigmeldung“, Manual-UAT).** (a) Am Touch-Terminal zweimal auf „OK“
  → eine Fertigmeldung. (b) Zwei gleichzeitige `POST /BdeTerminal/ReportCompletion` für denselben Arbeitsgang, 6 Läufe → je genau eine
  `IsFinal`-Buchung, zweite Antwort Toast „Arbeitsgang ist bereits fertig gemeldet.“ Prüf-SQL:
  `SELECT b.WorkOperationId, COUNT(*) FROM BdeBookingQuantities q JOIN BdeBookings b ON b.Id = q.BdeBookingId WHERE q.IsFinal = 1 AND b.IsCancelled = 0 GROUP BY b.WorkOperationId HAVING COUNT(*) > 1` = leer für die Test-AGs. Testbuchungen danach stornieren.
  Negativ: Mengen-Modal abbrechen und neu öffnen → „OK“ wieder bedienbar.
- **TS-138.6 — Unterpositionen im Auftrag sichtbar (IDEAL + AKE).** IDEAL: Auftrag zu einem HauptFA mit Sub-FA-Baugruppen (wie
  KO-2026-00005) → Zähler „n Positionen“ = Anzahl sichtbarer Zeilen (ohne Filter). AKE: Glas- bzw. VK-Auftrag, dessen Teile unter einer
  Baugruppe des Lager-Auftrags hängen → alle Zeilen sichtbar, anhakbar. Negativ/Regression: volle Stückliste `/Picking/Bom/<id>` startet
  zugeklappt, „Alle aufklappen“/„Alle zuklappen“ wie bisher; Auftrag, dessen Baugruppe im selben Auftrag liegt → Kinder erst nach Aufklappen.
- **TS-138.7 — CSV ohne Tausendertrenner (AKE + IDEAL).** `/StockOverview` mit einer Menge ≥ 1.000 und einer negativen ≥ 1.000 → Bildschirm
  `-10.026,000`, CSV `-10026,000` ohne Apostroph, Excel rechnet (Summe). Spalte mit ganzer Zahl ≥ 1.000 (typisiert) → ohne Punkt. Negativ:
  Text `+43 664 1234567`, `=1+1` weiterhin mit Apostroph; Datum `12.10.2026` unverändert; Position `1.234` in Textspalte unverändert
  (gemäß Rückfrage 6).
- **TS-138.8 — Feste Kultur (beide).** FA-Liste „Stk.“ ≥ 1.000 → Tausenderpunkt; Dezimal-Eingabe `2,5` in einer `decimal`-Maske (z. B.
  Lagerbewegung) → gespeichert 2,5. Server-Spaltenfilter `>1000` auf einer Mengenspalte liefert die erwarteten Zeilen.

Unit-/Node-Tests (Build-Nachweis): AK-1, AK-2, AK-3, AK-4, AK-6, AK-7, AK-8, AK-13, AK-14. Manual-UAT: AK-9, AK-10, AK-11, AK-12, AK-15.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | kein Epic | – | – |

## Deploy

**Vorläufig** – der QA-Lauf bestätigt gegen den echten Diff.

- **Web-App:** ja (Controller, Views, JS, `Program.cs`).
- **Service:** ja, nur wegen `AppVersion.cs` (keine Dienst-Logik; feste Kultur im Dienst nur nach Rückfrage 2).
- **Migration:** ja, SQL/150 (additiv; kein Dienst-Stopp nötig: der einzige Anleger von Receipts ist `Receive.cs:129` – Grep `new PurchaseOrderReceipt|PurchaseOrderReceipts.Add` außerhalb der Tests ohne weiteren Treffer; der Dienst ändert nur Status vorhandener Zeilen).
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023):
  - `@{ Id='SQL/150'; Version='1.99.23'; Typ='Schema'; Firma='beide'; Phase='Schema'; Ausfuehrung='automatisch'; Datei='150_AddPurchaseOrderReceiptAbsendekennung.sql'; MigrationId='<ts>_AddPurchaseOrderReceiptAbsendekennung'; Voraussetzung='additiv, Spalte NULL + gefilterter UNIQUE-Index'; Quelle='<changelog-slug v1.99.23>' }`
    – nach `SQL/149` in Deploy-Reihenfolge.
  - `@{ Id='v1.99.23-SeitenNeuLaden'; Version='1.99.23'; Typ='Info'; Firma='beide'; Phase='Nachher'; Ausfuehrung='anzeigen'; Text='Offene Seiten „Wareneingang zu Bestellung“ und Leitstand (auch am Handgeraet) neu laden: Seiten von vor dem Update werden mit „Seite ist veraltet“ abgewiesen, nichts wird gebucht.' }`
  - Keine Daten-/Einmal-Skripte, keine Prüfung (Schema-Zustand prüft der Schema-Schritt).
- **Prüfschritt vor dem Prod-Update:** Rückfrage 3 (idealweb01 Zahlformat).
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

## Brain-Pflichten des Dev-Laufs (Hauptcheckout)

ADR 0026 „Schreibaktionen senden die Absicht, der Server prüft am Schreibort“ (Nummer prüfen); [[fallstricke]]: §8 „Deadlock ist verpackt –
Kette prüfen“, neuer Eintrag „Doppel-Absenden“ (Regel A + Restfenster WE-4/K-7), „keine feste Request-Kultur“ als **überholt** markieren
(nicht löschen) mit Verweis auf die feste Kultur; [[feature-map]], Changelog v1.99.23, [[testszenarien-index]].

## Offene Rueckfragen

1. **Feste App-Kultur:** `de-DE` (Empfehlung, Begründung C6) oder ein `de-AT`-Klon mit erzwungenem Tausenderpunkt? Reines `de-AT` scheidet
   aus (ICU: U+00A0, DEV-Befund N-B4-2).
2. **Kultur im Dienst:** auch `IDEALAKEWMSService/Program.cs` fest setzen? Empfehlung: nicht in dieser Spec – die kulturabhängigen Parse-/
   Format-Wege des Dienstes sind nicht geprüft; eigene Aufgabe.
3. **Prüfschritt idealweb01** (Fakt außerhalb des Codes): FA-Liste, Spalte „Stk.“, eine Menge ab 1.000 – erscheint `1.234,500`
   (Punkt/Komma)? akenet01 ist mit „4.500,000“ belegt. Bei `1,234.500` STOPP: dann ändert die feste Kultur das Lesen aller Dezimal-Eingaben
   auf diesem Server; bitte Ergebnis melden.
4. **Client-Sperre:** nur an den genannten Formularen per `data-einmal-absenden` (Empfehlung) oder global für alle POST-Formulare mit Opt-out?
5. **Wareneingang ohne Kennung** (Seite vor dem Update geöffnet): ablehnen mit „Seite neu laden“ (Empfehlung) oder ohne Doppelschutz buchen?
6. **CSV, ganze Zahlen mit Punkt** (`1.234`) in Spalten **ohne** `data-filter-type="number"`: unverändert lassen (Empfehlung – Verwechslung
   mit Positionen/Nummern) oder ebenfalls ohne Punkt exportieren?

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Feste App-Kultur de-DE oder de-AT-Klon mit Tausenderpunkt →
2. Feste Kultur auch im Dienst →
3. Prüfschritt idealweb01 Zahlformat (Ergebnis) →
4. Client-Sperre opt-in oder global →
5. Wareneingang ohne Kennung ablehnen oder buchen →
6. CSV ganze Zahlen mit Punkt in Textspalten →
