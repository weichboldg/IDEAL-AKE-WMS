---
type: spec
title: "Schutz gegen Doppel-Absenden und unsichtbare Positionen"
slug: 2026-11-14-schutz-doppel-absenden-und-unsichtbare-positionen-spec
status: Testbereit
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
task: "[[2026-11-14-schutz-doppel-absenden-goal-lauf]]"
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
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-14"
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

**Final (QA 2026-11-14, gegen den echten Diff `1539c0c1..1a73bc6a`):** `IdealAkeWms/` geändert → Web ja; `IDEALAKEWMSService/` nur
`AppVersion.cs` → Dienst ja (nur Version, keine Logik); neue Datei `IdealAkeWms/Migrations/20261114080950_AddPurchaseOrderReceiptAbsendekennung.cs`
→ Migration ja. Paket: `<Worktree>\publish` (= `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish`), v1.99.23,
Commit `1a73bc6a` (Code-Stand `510b3020`, danach nur `docs/`), beide Komponenten + `publish\SQL` + `publish\deploy` + `publish-info.txt`
+ `deploy.zip` (09:39). Reihenfolge auf dem Zielsystem: DB-Backup → `deploy\Update-IdealAkeWms.ps1` (stoppt, Schema-Schritt `SQL/150`
laut Manifest, Web + Dienst, Info „Seiten neu laden“) → Dienst starten. Kein Einmal-Skript. Ablauf Mensch: publish aus dem Worktree →
Testsystem → Test → Merge. **Nach dem Merge nur dann erneut aus `main` publishen, wenn der Merge getestete Dateien mit parallelen
`main`-Änderungen zusammengeführt hat.**

**Nachtrag Nach-QA F1 (2026-11-14):** Paket neu aus Commit `08672fcc` (11:06, `deploy.zip` neu); `08672fcc` ändert nur Web-Code
(`IdealAkeWms/`), keine Migration, kein Manifest-Eintrag nötig; Deploy-Reihenfolge und Komponenten unverändert (Dienst nur Version).

Vorläufige Angaben des Dev-Laufs (bestätigt):

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

## Freigabe-Nachtrag 2026-11-14 09:00 (Gerald, geht dem Rumpf vor) – Client-Sperre global
- **Global für alle POST-Formulare** (statt nur `data-einmal-absenden` an einzelnen Formularen). Automatisch **ausgenommen:** GET-Formulare,
  Formulare mit `target="_blank"` und Formulare mit `data-mehrfach-erlaubt` (Opt-out, z. B. Scan-/AJAX-/Download-Formulare).
- Nach dem Absenden: Absende-Knöpfe sperren, Text „wird gesendet …“; **Freigabe nach 10 Sekunden und bei `pageshow`** (Zurückblättern/bfcache),
  damit nie etwas dauerhaft hängt. Barrierefreiheit: `aria-disabled`/`aria-busy`.
- **Serverseitige Idempotenz bleibt** an den Buchungsstellen der Spec (Wareneingang, Abschluss „Teilweise“, Freigabe, BDE-Fertigmeldung) – die
  Client-Sperre ist nur zusätzlicher Schutz.
- Der Lauf prüft **alle bestehenden POST-Formulare**, setzt nötige Opt-outs und listet sie im QA-Nachweis (Formular, Grund).
- Version/TS/SQL in Lauf-Reihenfolge (Vorschlag 1.99.23, SQL 150, TS ab 138). Rückfrage 3 entfällt (Gerald 09:01: IDEAL soll de-DE sein, noch nicht produktiv) – kein Blocker.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Feste App-Kultur de-DE oder de-AT-Klon mit Tausenderpunkt → **de-DE** (Gerald 2026-11-14).
2. Feste Kultur auch im Dienst → **Nein, eigene Aufgabe** (Gerald 2026-11-14).
3. Prüfschritt idealweb01 Zahlformat (Ergebnis) → **Entfällt: IDEAL soll de-DE sein und ist noch nicht produktiv – kein Blocker** (Gerald 2026-11-14 09:01).
4. Client-Sperre opt-in oder global → **Global für alle POST-Formulare mit Leitplanken (siehe Freigabe-Nachtrag)** (Gerald 2026-11-14).
5. Wareneingang ohne Kennung ablehnen oder buchen → **Ablehnen mit „Seite neu laden“** (Gerald 2026-11-14).
6. CSV ganze Zahlen mit Punkt in Textspalten → **Unverändert lassen** (Gerald 2026-11-14).

## QA-Nachweis 2026-11-14 (qa-agent, unabhängig)

Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`. Geprüfter Diff dieser Spec:
`1539c0c1..510b3020` (Commits `32dc3119`, `648d09fe`, `1969e3b3`, `7cd357f0`, `510b3020`) + QA-Commit `1a73bc6a` (nur `docs/TESTSZENARIEN.md`,
TS-138.6a präzisiert). Build/Test über den ganzen Worktree. Ergebnis: **Testbereit** (keine blockierenden Befunde).

### 1. Build und Tests (HEAD `510b3020`; `1a73bc6a` ändert nur `docs/`)

- `dotnet build IdealAkeWms.slnx`: **0 Fehler**, 28 Warnungen (bestehend: CS8601/CS8321/NU1902/NU1903).
- `dotnet test`: `IdealAkeWms.Tests` **2872 bestanden / 0 Fehler / 1 übersprungen** (2873); `IDEALAKEWMSService.Tests` **626 / 0 / 0**.
  Node-Check `csv-export.check.js` läuft in `CsvExportCoverageTests` (Kern_NodeSelbstpruefung) mit – grün.

### 2. Code-Review (selbst, in diesem Lauf)

Gelesen: ganzer Diff ohne Designer-Datei, dazu `Nummernkreis.MitVersuchenAsync`, `PickingOrderWorkflow.AbschliessenAsync`, `ToggleRelease`,
`PurchaseOrdersController.Receive.cs` (GET/POST ganz), `Repository.AddAsync` (eine `SaveChanges` für Bewegung + Receipt), Sage-Queue-Decorator,
`BdeBookingService.InTransactionAsync`/`ReportCompletionAsync`, `site.js`, `bde-terminal.js promptQty`, `Bom.cshtml` (Vater-Position, Abschluss),
`csv-export.js`, `Program.cs`, `SQL/150`, `00_FreshInstall.sql` (2 Stellen), `nachlauf.psd1`.

Geprüft und ohne Befund:
- **SQL/150:** `SET QUOTED_IDENTIFIER ON`/`ANSI_NULLS ON` vor dem gefilterten Index; Tabellen-Guard mit `NOEXEC`; Spalte (`COL_LENGTH`) und Index
  (`sys.indexes`) je eigener Batch; History separat; am Ende `NOEXEC OFF`. Auf beiden DEV-DBs nach dem EF-Migrate nochmals gelaufen: exit 0, idempotent.
  Beide DBs: Spalte 16 Byte, Index `unique=1`, Filter `([Absendekennung] IS NOT NULL)`, History-Zeile vorhanden.
- **FreshInstall:** Spalte + UNIQUE-Index im Tabellenblock, `MigrationId` mit Guard auf die Spalte. **Manifest:** `SQL/150` nach `SQL/149`, Info
  `v1.99.23-SeitenNeuLaden` (Deploy-Schritt „Seiten neu laden“ vorhanden). Drift-Test um `SQL/150` ergänzt.
- **site.js:** Ausnahmen GET (auch `formmethod`), `target`/`formtarget="_blank"`, `data-mehrfach-erlaubt`; Sperre erst per `setTimeout(0)` (Name/Wert
  des Knopfs bleibt in den Formulardaten), `defaultPrevented` vor und nach dem Tick (später registrierte jQuery-Listener), zweites Absenden →
  `preventDefault`; Freigabe nach 10 s und bei `pageshow.persisted`; `aria-disabled` + `aria-busy`; Symbol-Knöpfe: Spinner + `visually-hidden`-Text;
  vorher schon gesperrte Knöpfe bleiben gesperrt (`wmsWarDisabled`). `form.submit()` aus Skripten nicht betroffen. `wmsSperren` mit Rückgabe `null`.
- **Applock:** `sp_getapplock … @LockOwner='Transaction'` als erster Schritt im Lambda von `InTransactionAsync` (EF-Transaktion offen, `ExecuteSqlRaw`
  läuft in ihr); Rückgabe < 0 → Klartext; InMemory-Weiche identisch zu `InTransactionAsync`. Ungültige Ergebnisse committen eine leere Transaktion – Sperre frei.
- **K-1:** Statusprüfung im Versuch, ein Wortlaut (lokale Funktion), `ergebnis.Fehler` vor dem FA-Status-Block. **K-6:** `release` als Zielzustand, alle
  Aufrufer (`_PickingLeitstandRow`, Modal) senden ihn; `ProductionOrders/ToggleRelease` ist nur 301. **WE-1:** Kennung vor Zeilenprüfung, leere/fehlende
  Kennung abgewiesen, `IstSqlFehler(2601/2627)` nur um `AddAsync`, Bewegung + Receipt atomar (eine `SaveChanges`), Queue erst nach Erfolg.
- **Feste Kultur:** Thread-Default + `UseRequestLocalization` mit genau einer Kultur, keine Provider. Formulare mit `type=number` binden weiter invariant (`__Invariant`).
- **Audit:** neue Receipts mit `Created*` wie bisher; abgewiesene Doppelversuche schreiben nichts. Keine Einstellung (ADR 0020 entfällt, Abschnitt „Einstellungen: keine“).
- Fehlerklassen: kein neuer Wert in `<script>` (nur bestehendes `@(Model.Auftrag?.Id ?? 0)`), kein `hidden` + `d-*`, keine stille `?? 0`-Umdeutung
  (`VaterPosition ?? ""` = „kein Vater“, gewollt), keine neue positionsgebundene Liste, Server prüft überall selbst, Abschluss-Text an einer Stelle.

Befunde:

| # | Schwere | Befund | Umgang |
|---|---|---|---|
| F1 | niedrig | Zwei **gleichzeitige** `release=true` für **denselben** FA bestehen beide die Zielzustandsprüfung (Lesen ohne Sperre); Daten richtig (ein Auftragssatz), die zweite Antwort ist aber grün „0 Kommissionierauftrag/-aufträge für 0 FA angelegt; 1 FA ohne offene Restmenge“ statt „bereits freigegeben“ (DEV gemessen). Restfenster wie in C2 für die Rücknahme beschrieben. | Nicht blockierend; Entscheidung Gerald (Folgeaufgabe ja/nein). **Nachtrag: erledigt** – Gerald: zweite Antwort „bereits freigegeben“; umgesetzt `08672fcc`, Nach-QA grün (Abschnitt „Nach-QA F1“). |
| F2 | niedrig (Doku) | TS-138.6a erwartete „Zähler = sichtbare Zeilen“; im IDEAL-Auftrag stehen aber Baugruppen-Zeilen mit Kindern im selben Auftrag, deren Kinder laut C5 zugeklappt starten (KO-2026-00003: 90 Zeilen, 8 verborgen, alle unter sichtbarer Baugruppe mit Aufklapp-Pfeil). | Behoben: TS-138.6a präzisiert, Commit `1a73bc6a`. |
| F3 | Info | Abgefangene UNIQUE-Kollisionen (2601) und Deadlock-Wiederholungen schreibt EF als `[ERR]` ins Web-Log. Kein Mailweg (ErrorNotification gilt nur für Dienst-Syncs). | Kein Handlungsbedarf; beim Log-Lesen beachten. |
| F4 | Info | Applock nur in `ReportCompletionAsync`; andere Wege mit End-Menge (Stopp/Fertig mit `IsFinal`) sind nicht dagegen serialisiert. | Laut Spec nicht im Umfang (BDE-2, übrige BDE-Aktionen). |
| F5 | Info (Zusatzfund, nicht im Diff) | `ToggleRelease` prüft nicht, ob `assignedPickerId` ein Kommissionierer ist (DEV: Freigabe mit Benutzer 4 angenommen, während die Seite „Kein Benutzer als Kommissionierer“ zeigte). | Nur gemeldet, nicht mitgebaut. |

### 3. Nachweisart je Akzeptanzkriterium

| AK | Nachweis |
|---|---|
| AK-1 | automatisiert (`NummernkreisTests`); zusätzlich DEV: verpackter Deadlock `InvalidOperationException → DbUpdateException → SqlException 1205` im Web-Log 09:29:06, „Versuch 1/3, 2/3 gescheitert“, dann Meldung „bereits … – nichts geändert“ |
| AK-2, AK-3 | automatisiert (`PickingOrderWorkflowTests`) + DEV TS-138.1b |
| AK-4 | automatisiert (`PickingLeitstandControllerTests`, Theory 4 Fälle) + DEV TS-138.2 |
| AK-5 | DEV: Zeilen- und Modal-Formular senden `release` (`data-einmal-absenden` laut Nachtrag durch die globale Sperre ersetzt) |
| AK-6, AK-7, AK-8 | automatisiert (`PurchaseOrdersReceiveTests`) + DEV TS-138.4c/f |
| AK-9 | **Manual-UAT** – auf DEV ausgeführt (TS-138.4b, 4/4 OK) |
| AK-10 | **Manual-UAT** – auf DEV ausgeführt (TS-138.5b, 6/6 OK) |
| AK-11 | **Manual-UAT** – auf DEV ausgeführt (TS-138.1b 3/3 + 138.1c; TS-138.3 4/4 OK) |
| AK-12 | **Manual-UAT** – teilweise auf DEV (Doppelklick Wareneingang, Leitstand-Zeile, Abschluss: je 1 POST); Enter doppelt, 10-s-Freigabe, `confirm`-Abbrechen, `btnQtyOk` offen |
| AK-13 | automatisiert (`BomViewModelVaterPositionTests`) + DEV TS-138.6a/c |
| AK-14 | automatisiert (Node `csv-export.check.js`) |
| AK-15 | **Manual-UAT** – DEV AKE: `/StockOverview` 00000905 zeigt `-10.026,000` (kein U+00A0) bei Server-Kultur `de-AT` |
| AK-16 | Build/Tests grün, Version 1.99.23 in beiden `AppVersion.cs`, Changelog, Hilfe, TS-138, TS-86.5 |

### 4. Geänderte bestehende Tests

| Test | Einordnung |
|---|---|
| `PickingLeitstandControllerTests` – alle `ToggleRelease_*` (Aufruf um `release:` ergänzt; Rücknahme sendet `release:false`, Theorie `!isReleasedForPicking`) | Verhalten absichtlich geändert, AK-4 |
| `PurchaseOrdersReceiveTests` – alle POST-Aufrufe über Helfer `Post(...)` mit neuer Kennung | Signaturanpassung (neuer Parameter), Erwartungen unverändert |
| `PickingOrderWorkflowTests.SetupAsync` – optionale Repository-Hülle | Fixture-Erweiterung, bestehende Fälle unverändert |
| `NachlaufManifestDriftTests` – Liste um `SQL/150` | Pflicht-Nachzug (neues Skript) |
| `csv-export.check.js` – Erwartung `-1.234,500` → `-1234,500` | Verhalten absichtlich geändert, AK-14 |

Kein Fall „sonstiges“.

### 5. Checkliste CLAUDE.md

Migration + idempotentes `SQL/150` ✔ · FreshInstall 2 Stellen ✔ · AgentJobs nicht betroffen ✔ · Audit ✔ · Version 1.99.23 Web + Dienst ✔ ·
Anwender-Changelog + Hilfe ✔ · Brain-Changelog [[2026-11-14-v1-99-23-schutz-doppel-absenden]] ✔ · ADR [[0026-schreibaktionen-senden-die-absicht-der-server-prueft-am-schreibort]] ✔ ·
[[fallstricke]] §38 ✔ · [[feature-map]] ✔ · `docs/TESTSZENARIEN.md` TS-138 + TS-86.5 ✔ · [[testszenarien-index]] TS-138 ✔ · Manifest ✔. ADR 0020: keine Einstellung.

**POST-Formular-Inventur (Nachtrag 09:00) – nachgeprüft:** 129 POST-Formulare in 73 Views (Grep `<form[^>]*method="post"`, ohne Groß/Klein;
kein `Html.BeginForm`). Keine POST-Aktion liefert eine Datei (alle `return File(...)` sind PDF-Aktionen mit `[HttpGet]`:
`Beschichtungsauftraege/Pdf`, `FaHierarchyBeschichtung/Pdf` + `PdfSummiert`, `FaHierarchyKommissionierListen/Pdf` + `PdfSummiert`); kein Formular mit
`target`, kein Knopf mit `formtarget`/`formmethod`. **Opt-outs `data-mehrfach-erlaubt`: keine nötig.**

| Formular | Grund, warum kein Opt-out nötig ist |
|---|---|
| `Picking/Bom.cshtml` `transferForm` (Umbuchen) | Seiten-Listener `preventDefault()` + AJAX → globale Sperre greift nicht (`defaultPrevented`) |
| `PickingLeitstand/Index.cshtml` Sammel-Freigabe `formRelease` mit Kommissionierer-Modal | Listener `preventDefault()` öffnet das Modal → frei; das Modal-Formular sendet normal und navigiert |
| `PickingLeitstand/Index.cshtml` `formRelease`/`formUnrelease`/`formNachziehen` ohne Modal | `attachIds` ohne Abbruch → normales Absenden mit Navigation |
| `FaCompletion/Edit.cshtml:201`, `:321`; `OrderRecipients/Edit.cshtml:77`; `OrderRecipients/Index.cshtml:49`; `Roles/Edit.cshtml:74`; `ServiceSettings/Index.cshtml:172` | `onsubmit="return confirm(...)"` – „Abbrechen“ setzt `defaultPrevented` → Knopf bleibt frei; „OK“ navigiert |
| `OrderRecipients/Edit.cshtml:68` | `onsubmit` kopiert nur Werte, kein Abbruch → normales Absenden |
| alle übrigen Formulare (Stammdaten anlegen/ändern/löschen, Lagerbewegungen, Wareneingang, Leitstand-Zeile und -Modal, Settings, Benutzer/Rollen, BDE-Stammdaten, Login/Logout im Layout …) | Standard-PRG: Antwort ist Redirect oder View → Seite lädt neu, die Sperre endet mit der Navigation (sonst nach 10 s bzw. `pageshow`) |

### 6. DEV-Deploy und Testlauf

- Standort laut Frontmatter `beide`. Elevated: ja. `D:\Dev\scripts\dev-deploy.ps1 -Standort AKE|IDEAL -Quelle <Worktree>`: beide **exit 0**, „HTTP 200,
  Fusszeile v1.99.23“, DEV-Absicherung angewendet, Dienste gestartet (AKE http://localhost:8081, IDEAL http://localhost:8082).
- Schalter vor den Tests geprüft (beide DBs): `SageLagerbuchungAktiv`, `SageLagerbuchungUmbuchungAktiv`, `SageWareneingangBelegAktiv`,
  `WareneingangCsvAktiv`, `ErrorNotification:Enabled`, `Notifications:MeldebestandEnabled`, alle `Sync:*Enabled` = **false**. Nichts an Sage geschrieben,
  keine Mail. `SageBookingQueueItems` vor/nach den WE-Läufen 16/16.
- Gleichzeitige POSTs: PowerShell-`HttpClient` mit Windows-Anmeldung, zwei getrennte Sitzungen („Tabs“), Token aus dem Formular, zwei `SendAsync`
  ohne Warten, dann `WaitAll` (ein JavaScript-Werkzeug im Browser stand nicht zur Verfügung). Doppelklicks im Chrome-Tab, Netzwerkprotokoll ausgewertet.

| Schritt | durchgeführt | Ergebnis | Status |
|---|---|---|---|
| 138.1a | AKE KO-2026-00074 (Id 112), eine Zeile per SQL umgebucht (Ist < Soll), Modal „Teilweise“ Doppelklick | 1 POST `AuftragAbschliessen` (200), Status 30, 1 Folgeauftrag | OK |
| 138.1b | 3 Läufe parallel teilweise + teilweise (KO-00070 → 71 → 72) | je eine Antwort 200 mit Folgeauftrag, die andere 400 „Auftrag … ist bereits teilweise abgeschlossen – nichts geändert.“; je genau 1 Folgeauftrag; Log: verpackter Deadlock 1205 wiederholt | OK |
| 138.1c | parallel teilweise + vollständig (KO-00073) | 200 vollständig, 400 „… ist bereits abgeschlossen – nichts geändert.“, 0 Folgeaufträge | OK |
| 138.2a | FA 2646002 (Id 3048): Sitzung 1 `release=true` → KO-2026-00061; Sitzung 2 (veraltet) `release=true` mit anderem Kommissionierer | „FA 2646002 ist bereits freigegeben – nichts geändert.“; Auftrag aktiv, Kommissionierer unverändert | OK |
| 138.2b | Rücknahme Sitzung 1, dann Sitzung 2 | 1. storniert KO-00061; 2. „ist nicht (mehr) freigegeben – nichts geändert.“ | OK |
| 138.2c | Doppelklick Symbolknopf „Freigabe zurücknehmen“ (FA 2627001) | genau 1 POST `ToggleRelease` | OK (Zeile); Modal-Weg NICHT MOEGLICH: Kommissionierer-Auswahl auf DEV leer |
| 138.2d | POST ohne `release` | „Die Seite ist veraltet – bitte neu laden. Es wurde nichts geändert.“ | OK |
| 138.3 | 4 Läufe parallel Freigabe FA 3047 + 3045, danach jeweils parallele Rücknahme | kein 500, je Lauf jeder FA genau 1 aktiver Auftrag (KO-00062 … 69), Rücknahme storniert je 1 | OK; Zusatz gleicher FA parallel → F1 |
| 138.4a | Chrome, Bestellung 2020-128897 Pos. EK007848, Doppelklick „Einbuchen“ | 1 POST, 1 Receipt | OK |
| 138.4b | 4 Läufe parallel, gleiche Kennung, Menge 1 | je 1 Erfolg + 1 Warnung „Diese Eingabe wurde bereits gebucht …“, kein 500; Receipts 1→4, Bewegungen 1→4, Queue 16→16; Log: 4× 2601 auf `IX_PurchaseOrderReceipts_Absendekennung_BelPosId` abgefangen (echter Gleichstand) | OK |
| 138.4c | erneutes Senden mit gebuchter Kennung (Skript) / Zurück-Taste im Chrome | Skript: Warnung „bereits gebucht“, nichts gebucht. Chrome: Seite neu geladen (kein bfcache), Knopf bedienbar, Menge leer → „Keine Menge eingegeben – nichts gebucht.“ | OK (nichts doppelt) |
| 138.4d | Barcode | braucht Sage-WE im Testbetrieb (`SageWareneingangBelegAktiv`) – bleibt aus | NICHT MOEGLICH |
| 138.4e | Menge „abc“ → View mit Fehler, Kennung im Formular erhalten; korrigiert „2“ mit derselben Kennung | genau 1 Buchung | OK |
| 138.4f | ohne Kennung / `Guid.Empty` | „Die Seite ist veraltet – bitte neu laden. Es wurde nichts gebucht.“ | OK |
| 138.4g | Prüf-SQL Dubletten | leer | OK |
| 138.5a | Touch-Terminal „OK“ doppelt | Terminal-Ablauf im Browser nicht automatisierbar (Terminal-Benutzer) | NICHT MOEGLICH |
| 138.5b | IDEAL Werkbank 65 „Entgraten Fläche“ temporär `BdeNurAgFertigmeldung=1`, Werker 1, Terminal 2, AGs 919/915/911/905/897/893 je 2 parallele `ReportCompletion` | je 1× `Success`, 1× „Arbeitsgang ist bereits fertig gemeldet.“; Prüf-SQL je genau 1 `IsFinal` | OK (6/6) |
| 138.5c | Mengen-Modal abbrechen/neu öffnen | nicht im Browser geprüft | NICHT MOEGLICH |
| 138.6a | IDEAL KO-2026-00002/3/4/5 (Server-HTML ausgewertet) | 0 Zeilen verborgen ohne vorhandenen Vater; ohne den Fix wären 45/76/50/73 Zeilen ohne direkten Vater unsichtbar; verborgen nur Kinder sichtbarer Baugruppen mit Aufklapp-Pfeil | OK (nach F2-Präzisierung) |
| 138.6b | AKE Glas-/VK-Auftrag | auf AKE-DEV kein Auftrag mit fehlender Vater-Zeile (4 geprüfte Aufträge vollständig) | NICHT MOEGLICH (keine Daten) |
| 138.6c | volle Stückliste IDEAL FA 131 und AKE FA 3047 | alle Zeilen mit Punkt starten verborgen (250/250, 173/173) – wie bisher | OK |
| 138.7a–c | CSV-Export | Datei-Download nicht ohne Rückfrage; Kern per Node-Test belegt | NICHT MOEGLICH (Mensch) |
| 138.8a | AKE `/StockOverview` 00000905 | `-10.026,000`, kein U+00A0 (Server-Kultur `de-AT`) | OK |
| 138.8b | Wareneingang Menge `2,5` | gespeichert 2.5000 | OK |
| 138.8c | Server-Spaltenfilter `>1000` | nicht ausgeführt | offen (Mensch) |
| 138.9 | allgemeine Sperre | 3 Formulare per Doppelklick je 1 POST (s. o.); Enter doppelt, 10-s-Freigabe, `confirm`-Abbrechen nicht geprüft (keine Dialoge auslösen) | teilweise OK, Rest Mensch |

**Testdaten aufgeräumt:** AKE: 7 Test-Wareneingänge (Receipts 21–31) samt Bewegungen 19546–19556 auf BelPosId 128139 entfernt (vorher geprüft: keine
Queue-/Kommissionier-/Bedarfs-Referenz); Benutzer 4 `IsPicker` zurück auf 0; Freigaben FA 3045/3047 zurückgenommen. Verbleibend: Test-Aufträge
KO-2026-00061 … 00076 (storniert bzw. abgeschlossen; Zeilenstatus in KO-00070 … 00074 per SQL gesetzt), FA 3048 dabei freigegeben und „vollständig
kommissioniert“. IDEAL: 6 Test-Fertigmeldungen storniert (`IsCancelled=1`, Grund „QA v1.99.23 TS-138.5b Testbuchung“), Werkbank 65
`BdeNurAgFertigmeldung` zurück auf 0.

### 7. Publish

`C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` neu erzeugt (Zielordner vorher geleert): `IDEALAKEWMSWeb` +
`IDEALAKEWMSWebService` (beide exit 0, `IdealAkeWms.dll` ProductVersion `1.0.0+510b3020…`), `SQL` (inkl. `150_…`), `deploy`, `publish-info.txt`
(Version 1.99.23, Commit `1a73bc6a47dcaa4bd75f6b22448c49eac8c40a61`), `deploy.zip` neu.

### Manuelle Test-Checkliste für Gerald (offen)

1. TS-138.2c im **Modal** (Freigabe mit Kommissionierer-Auswahl): Doppelklick „Freigeben“ → ein POST (DEV hatte keine Kommissionierer in der Auswahl).
2. TS-138.4d Barcode mit Sage-WE im **Testbetrieb** (nur Testsystem): parallele POSTs → ein Wareneingang mit Barcode, ein ELL-Beleg.
3. TS-138.5a/5c am **Touch-Terminal** IDEAL (Werkbank „nur AG-Fertigmeldung“): „OK“ doppelt → eine Fertigmeldung; Modal abbrechen/neu öffnen → „OK“ bedienbar.
4. TS-138.6b AKE Glas-/VK-Auftrag mit Teilen unter einer Baugruppe des Lager-Auftrags (auf DEV keine Daten): alle Zeilen sichtbar und anhakbar.
5. TS-138.7a–c CSV-Export `/StockOverview`: `-10026,000` ohne Apostroph, Excel-Summe; Text/Datum/Position unverändert.
6. TS-138.8c Server-Spaltenfilter `>1000` auf einer Mengenspalte.
7. TS-138.9 mit DevTools „Slow 3G“: Enter doppelt → ein POST; Knopf nach ≤ 10 s frei; `confirm` → „Abbrechen“ lässt Knopf frei; Symbolknopf behält Breite.
8. ~~Befund F1 entscheiden~~ – entschieden und umgesetzt (`08672fcc`, Nach-QA F1). Offen nur noch: TS-138.3b auf dem Testsystem stichprobenartig.
9. Testsystem-Lauf mit `publish\deploy\Update-IdealAkeWms.ps1` (DB-Backup vorher, `SQL/150`, Info „Seiten neu laden“), TS-138 dort stichprobenartig, dann Merge.

## Nach-QA F1 2026-11-14 (qa-agent, eng begrenzt auf `1a73bc6a..08672fcc`)

Anlass: Befund F1; Entscheidung Gerald: die zweite gleichzeitige Freigabe meldet „FA … ist bereits freigegeben – nichts geändert.“
Umsetzung `08672fcc`: `ProductionOrderPickingStatusRepository.SetReleaseAsync` → `Task<bool>`, relational bedingtes
`UPDATE … WHERE [IsReleasedForPicking] <> @released` (ein Gewinner), InMemory Vergleich; Controller meldet bei `false` die Warnung und
setzt weder Kommissionierer noch erzeugt/storniert er Aufträge. Neues Szenario TS-138.3b.

**1. Build und Tests (HEAD `08672fcc`):** `dotnet build IdealAkeWms.slnx` 0 Fehler (28 Warnungen, bekannt). `dotnet test`:
IdealAkeWms.Tests **2874 bestanden / 1 übersprungen / 0 Fehler** (2875; +2 neue Tests), IDEALAKEWMSService.Tests **626 / 0 / 0**.

**2. Code-Review (selbst, nur dieser Diff):**
- Tabellen-/Spaltennamen geprüft: `ToTable("ProductionOrderPickingStatus")` (`ApplicationDbContext.cs:508`), Spalten `IsReleasedForPicking`
  `BIT NOT NULL DEFAULT 0` und `ProductionOrderId` (`SQL/00_FreshInstall.sql:428 ff.`) – `<>` auf NOT-NULL-Bit ist eindeutig, kein NULL-Loch.
  `ExecuteSqlInterpolatedAsync` parametrisiert (`bool` → `bit`), keine Injektion.
- Zusammenspiel mit dem getrackten `SaveChanges`: Der Satz wird vor dem UPDATE geladen (Snapshot = alter Zustand); `row.IsReleasedForPicking =
  released` gilt danach als geändert und wird im `SaveChanges` noch einmal auf denselben Wert geschrieben – harmlos. Kein Concurrency-Token
  auf der Entität (kein `RowVersion` gemappt), daher keine `DbUpdateConcurrencyException` durch das vorgezogene UPDATE.
- Audit-Felder: `ModifiedAt/By/ByWindows` (und `ReleasedAt/By`) nur beim Gewinner im selben `SaveChanges`; der Verlierer schreibt nichts
  (DEV: `ReleasedAt`/`ModifiedAt` je Lauf vom Gewinner, s. u.).
- Rücknahme `release=false`: gleicher Weg; Gewinner leert `AssignedPicker*` und storniert, Verlierer „FA … ist nicht (mehr) freigegeben –
  nichts geändert.“, ruft `FreigabeZuruecknehmenAsync` nicht auf.
- Einziger Aufrufer `PickingLeitstandController.cs:416` (Grep `SetReleaseAsync`, keine Decorator-Implementierung, `BulkRelease` nutzt den
  Aufruf nicht). Bestehende Repository-Tests (`OnRevoke`, Prio-Folge) unverändert grün; Controller-Tests: nur Mock-Default `ReturnsAsync(true)`
  ergänzt (Typ-Anpassung, kein Verhalten geändert).
- Restrisiko laut `ponytail:`-Kommentar: Anspruch (UPDATE, Autocommit) und Rest-Felder (`SaveChanges`) ohne gemeinsame Transaktion. Scheitert
  das `SaveChanges`, steht `IsReleasedForPicking=1` ohne neue `ReleasedAt`/`Modified*` und ohne Aufträge; der nächste Klick meldet
  „bereits freigegeben“, Abhilfe Rücknahme + neue Freigabe. Sehr unwahrscheinlich (gleiche Zeile, gleiche Verbindung), sichtbar, kein
  Datenverlust → akzeptiert, wie kommentiert.
- Hinweis (kein Befund, nicht im Umfang): gegenläufige Klicks im selben Augenblick (A gibt frei, B nimmt zurück, beide bestehen die
  Vorprüfung in unterschiedlicher Reihenfolge) sind nicht abgedeckt – das ist kein Doppel-Absenden derselben Absicht; vorher genauso.
- **Befunde: keine.**

**3. Nachweisart:** Verlierer-Zweig im Controller und `false` am Repository automatisiert (InMemory:
`ToggleRelease_GleichstandVerloren_BereitsFreigegeben_F1`, `SetReleaseAsync_SchonImZielzustand_False_NichtsGeschrieben`). Das bedingte
UPDATE selbst (raw SQL, echter Gleichstand) ist nicht InMemory-testbar → **Manual-UAT, auf DEV AKE ausgeführt** (TS-138.3b).

**6. DEV-Deploy und Testlauf:** elevated; `dev-deploy.ps1 -Standort AKE` und `-Standort IDEAL` (IDEAL zur Konsistenz, gleicher Code-Stand
auf beiden DEV-Sites) je **exit 0**, „HTTP 200, Fusszeile v1.99.23“, DEV-Absicherung angewendet, Dienst gestartet. Keine Migration in diesem
Diff. Schalter unverändert aus (keine Sage-/Sync-/Mail-Schalter gesetzt). Test per PowerShell-`HttpClient` (zwei getrennte Windows-Sitzungen
„A“/„B“, zwei `SendAsync` ohne Warten, `WaitAll`), FA 2645001 (Id 3047), A mit Kommissionierer 4, B mit Kommissionierer 3 – so ist der
Gewinner in der DB erkennbar.

| Lauf | Freigabe parallel | DB danach | Rücknahme | Status |
|---|---|---|---|---|
| 1 | A grün „1 Kommissionierauftrag/-aufträge für 1 FA angelegt (KO-2026-00077).“, B gelb „FA 2645001 ist bereits freigegeben – nichts geändert.“ | freigegeben, Picker 4 (= A), 1 Auftrag KO-00077 | parallel: A „storniert (KO-2026-00077)“, B „FA 2645001 ist nicht (mehr) freigegeben – nichts geändert.“ | OK |
| 2 | A grün (KO-00078), B gelb „bereits freigegeben“ | Picker 4, 1 neuer Auftrag | parallel: A storniert KO-00078, B „nicht (mehr) freigegeben“ | OK |
| 3 | **B** grün (KO-00079), A gelb „bereits freigegeben“ | Picker **3** (= B, Gewinner), 1 neuer Auftrag | einzeln: storniert | OK |
| 4 | A grün (KO-00080), B gelb | Picker 4, 1 neuer Auftrag | einzeln: storniert | OK |
| 5 (Sperre) | Zeile 4 s per `UPDLOCK` in sqlcmd gehalten, beide POSTs in dieser Zeit (beide Antworten erst nach ~3,7 s ⇒ beide hatten die Vorprüfung bestanden und warteten am UPDATE): A grün (KO-00081), B gelb „bereits freigegeben“ | Picker 4, 1 Auftrag; `ReleasedAt` 11:05:12.096 = Gewinner | – | OK |
| 6 (Sperre) | Rücknahme mit gehaltener Sperre (~3,6 s): A „storniert (KO-2026-00081)“, B „nicht (mehr) freigegeben – nichts geändert.“ | nicht freigegeben, Picker NULL, KO-00081 Status 90 | – | OK |

Je Lauf genau **ein** neuer Auftrag, keine grüne „0 Kommissionierauftrag …“-Meldung mehr, alle Antworten 302 (kein 500); Web-Log
`D:\Dev\AKE\Web\logs\` seit dem Deploy ohne `ERR`/Exception. `ReleasedAt`/`ModifiedAt`/`AssignedPickerId` stammen je Lauf vom Gewinner
(Lauf 3: Picker 3 = Sitzung B). Lauf 5/6 belegen deterministisch den Weg über das bedingte UPDATE (nicht nur die Vorprüfung).
Begleitmeldung „Kein Benutzer als Kommissionierer gekennzeichnet …“ ist DEV-Datenstand (kein Benutzer `IsPicker`), bekannt als F5.

**Testdaten:** FA 3047 am Ende nicht freigegeben, `AssignedPicker*` NULL, `PickingPriority` 34 unverändert. Test-Aufträge KO-2026-00077 …
00081 (Id 115–119) storniert (Status 90) und bleiben wie die früheren Test-Aufträge stehen. Keine Einstellung geändert, nichts an Sage, keine Mail.

**7. Publish:** `C:\git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish` geleert und neu: `IDEALAKEWMSWeb` +
`IDEALAKEWMSWebService` (beide exit 0, ProductVersion `1.0.0+08672fcc…`), `SQL` (inkl. `150_…`), `deploy`, `publish-info.txt`
(Version 1.99.23, Commit `08672fccce8bb39f685f60ff3131a6b3fe55ff58`, 11:06), `deploy.zip` neu.

**Ergebnis:** F1 **erledigt**. Status bleibt **Testbereit**. Offen für Gerald unverändert die Checkliste oben (Punkt 8 erledigt), plus
TS-138.3b stichprobenartig auf dem Testsystem.
