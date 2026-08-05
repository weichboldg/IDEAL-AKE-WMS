---
type: spec
title: "Massen-/Mehrfachartikel-Einbuchung (mehrere Artikel + Menge, ein Lagerplatz + eine FA)"
slug: 2026-08-05-wms-bugs-improvements-teil-2-spec
status: Entwurf
created: 2026-08-05
updated: 2026-08-05
source_backlog: "[[2026-08-05-WmsBugs&Improvements]]"
depends_on: ""
task: ""
worktree: ""
branch: ""
affected_code:
  - IdealAkeWms/Controllers/StockMovementsController.cs
  - IdealAkeWms/Models/ViewModels/StockMovementCreateViewModel.cs (neues Multi-ViewModel)
  - IdealAkeWms/Views/StockMovements/Inbound.cshtml
  - IdealAkeWms/Views/StockMovements/ (neue View fuer Mehrfach-Einbuchung, Name offen)
  - IdealAkeWms/wwwroot/js/barcode-scanner.js (ggf. Erweiterung fuer Mehrfach-Scan-Modus)
  - docs/TESTSZENARIEN.md
  - secondbrain/tests/testszenarien-index.md
open_questions: []
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

## Ziel / Nutzen (das Warum)

Aktuell erlaubt die Einbuchungs-Maske (`/StockMovements/Inbound`) nur einen Artikel pro
Formular-Absenden — Lagerplatz und FA-Nummer werden bei jedem Artikel neu eingetragen. Wenn ein
Wareneingang mehrere Artikel für denselben Lagerplatz/dieselbe FA umfasst (üblicher Fall bei
Sammel-Lieferungen), führt das zu unnötig vielen Einzelbuchungen mit wiederholter Eingabe. Ziel
ist eine Mehrfachartikel-Einbuchung, bei der Lagerplatz und FA **einmalig** erfasst werden und
darunter beliebig viele Artikel-Zeilen (Artikel + Menge) gescannt/eingetragen werden können.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Neue eigene Seite/Route (`/StockMovements/InboundBulk`, Freigabe-Antwort 1) für mehrere
  Artikel-Zeilen (Artikel-Auswahl/Scan + Menge je Zeile) unter einem gemeinsamen Kopf
  (Lagerplatz + FA-Nummer, wie bisher einmalig).
- Serverseitige Verarbeitung: pro Zeile eine `StockMovement`-Zeile vom Typ `Einbuchung` (identisch
  zum bestehenden Einzel-Pfad in `StockMovementsController.Inbound(POST)`), gleicher Lagerplatz
  und gleiche FA-Nummer für alle Zeilen des Formulars.
- **Buchung ausschließlich über `IStockMovementRepository.AddAsync` je Zeile** — identischer
  Repository-Pfad wie die Einzel-Einbuchung, inkl. Audit-Feld-Logik UND des mit v1.28.0 gemergten
  Sage-Enqueue-Decorators (Freigabe-Antwort S1). Kein Direkt-DbContext-Bypass.
- Wiederverwendung der bestehenden Audit-Feld-Logik (`ICurrentUserService`), unverändert.
- Sequenzieller Kamera-Scan (Freigabe-Antwort 2): ein Scan trägt in die Zeilen-Liste ein; beim
  erneuten Scan eines bereits gelisteten Artikels wird die Menge der bestehenden Zeile +1 gezählt
  (Freigabe-Antwort B1).
- Testszenarien-Ergänzung.

**Out-of-Scope**
- Keine Änderung an Aus- oder Umbuchung (nur Einbuchung, wie im Backlog gefordert).
- Keine Änderung an der bestehenden Einzel-Einbuchungsseite selbst — sie bleibt als Weg für den
  Einzelfall bestehen. (Die Mehrfach-Einbuchung ist mit Freigabe-Antwort 1 eine eigene Seite; ein
  Umschalt-Modus auf der bestehenden Seite ist damit gegenstandslos und entfällt.)
- Keine Änderung an der FA-Lagerplatz-Hinweis-Logik aus Teil 1 — beide Teile sind unabhängig
  mergbar; der Mehrfach-Pfad kann den (bereits gefixten oder noch ungefixten) Hinweis pro Zeile
  grundsätzlich wiederverwenden, ist aber nicht von Teil 1 blockiert.
- Bedarfsmeldungen-Fulfillment (`fulfilledRequisitionIds`) im Mehrfach-Modus ist bewusst
  zurückgestellt (Freigabe-Antwort 3) und als eigene Ideen-/Backlog-Notiz vorzumerken (siehe
  „## Finalisierung"). Kein Fulfillment-Handling auf der neuen Seite.

## Fachliche Anforderungen

1. Ein Formular mit genau einem Lagerplatz-Feld und einem FA-Nummer-Feld (wie heute), darunter
   eine dynamische Liste von Zeilen mit je Artikel-Auswahl (Scan oder Suche, wie bisher via
   Select2/`btnScanArticle`) und Menge.
2. Mindestens eine Zeile ist Pflicht; Zeilen können hinzugefügt/entfernt werden (Client-seitig,
   analog dem bestehenden Artikel-Hinzufügen-Muster in `Views/WarehouseRequisitions/Edit.cshtml`).
3. **Standardmenge je neu angelegter Zeile = 1** (Cross-Ref zu Teil-5, dessen Freigabe-Antwort 2
   diese Vorbelegung für die Mehrfach-Einbuchung fordert; erspart am Handscanner das Tippen der
   häufigsten Menge).
4. **Mehrfach-Scan desselben Artikels zählt hoch (Freigabe-Antwort B1):** Wird ein bereits in der
   Liste stehender Artikel erneut gescannt, erhöht sich die Menge der bestehenden Zeile um 1
   (Stück-für-Stück-Zählen), statt eine zweite Zeile anzulegen. Ein optionaler Schalter „neue Zeile
   erzwingen" deckt Sonderfälle ab, in denen bewusst eine zweite Zeile für denselben Artikel
   gewünscht ist.
5. Beim Absenden wird für jede gültige Zeile eine eigene `StockMovement`-Zeile
   (`MovementType.Einbuchung`) mit dem gemeinsamen Lagerplatz und der gemeinsamen FA-Nummer
   angelegt.
6. Erfolgsmeldung nennt die Anzahl gebuchter Artikel (`TempData["SuccessMessage"]`, analog
   `OutboundAllConfirm`: „{count} Artikel erfolgreich ausgebucht.").
7. **Keine künstliche Zeilen-Obergrenze (Freigabe-Antwort 4).** Die ASP.NET-Core-Framework-Grenze
   (`FormOptions.ValueCountLimit`, Default ~1024 Form-Felder) bleibt bestehen; wird sie erreicht,
   erscheint eine sprechende Fehlermeldung statt eines stillen HTTP-400 (siehe Lösungsentwurf S3).

## Ist-Zustand (Code-Referenzen)

- `IdealAkeWms/Controllers/StockMovementsController.cs:86-153` (`Inbound` GET/POST): verarbeitet
  genau **ein** `StockMovementCreateViewModel` (ein `ArticleId`, eine `Quantity`, ein
  `StorageLocationId`, ein `ProductionOrder`) pro Submit.
- `IdealAkeWms/Models/ViewModels/StockMovementCreateViewModel.cs:1-32`: flaches ViewModel, keine
  Zeilen-Liste.
- `IdealAkeWms/Views/StockMovements/Inbound.cshtml`: ein Artikel-Select2-Feld, ein Mengenfeld, ein
  Lagerplatz-Feld, ein FA-Feld — kein Mechanismus für mehrere Zeilen.
- Vorbild für „mehrere Zeilen unter einem Formular, client-seitig hinzufügen/entfernen" existiert
  bereits im Projekt: `Views/WarehouseRequisitions/Edit.cshtml:73-195` (Artikelsuche + dynamische
  Zeilenliste via Fetch-Calls, kein volles Form-Submit pro Zeile) — als Referenzmuster geeignet,
  auch wenn dort jede Zeile sofort einzeln per AJAX gespeichert wird (Draft-Bestellung), während
  die Einbuchung ein einmaliges Buchungs-Ereignis mit `Timestamp = DateTime.Now` für alle Zeilen
  gemeinsam sein sollte.

## Technischer Lösungsentwurf

**Gewählter Ansatz** (Freigabe-Antwort 1 — eigene Seite): neue Action
`StockMovementsController.InboundBulk` (GET zeigt das Formular, POST verarbeitet alle Zeilen in
einem Request — kein Zeilen-für-Zeilen-AJAX wie bei WarehouseRequisitions, weil eine Einbuchung
ein einmaliges Buchungs-Ereignis ist und alle Zeilen denselben `Timestamp` tragen sollen).

- Neues ViewModel `StockMovementBulkInboundViewModel`:
  ```
  int StorageLocationId
  string? ProductionOrder
  List<StockMovementBulkInboundLine> Lines   // { int ArticleId, decimal Quantity, string? ArticleDisplay }
  List<StorageLocation> StorageLocations
  ```
- View mit Kopf-Feldern (Lagerplatz, FA-Nummer — je einmal) + dynamischer Zeilen-Tabelle
  (Artikel-Select2 + Menge je Zeile + „Zeile hinzufügen"/„Zeile entfernen"-Buttons, client-seitig
  ohne Server-Rundtrip pro Zeile). Neue Zeilen werden mit Menge 1 vorbelegt (Fachliche
  Anforderung 3).
- **Buchungs-Pfad (S1 — hart):** Der POST-Handler bucht jede Zeile über
  `IStockMovementRepository.AddAsync` — denselben Repository-Pfad wie die Einzel-Einbuchung. Damit
  greifen sowohl die Audit-Feld-Logik als auch der mit v1.28.0 gemergte Sage-Enqueue-Decorator, der
  bei einer `Einbuchung` auf einem Sage-freigegebenen Lagerplatz je Zeile ein `SageBookingQueueItem`
  erzeugt. Das zitierte Vorbild `OutboundAllConfirm` (`StockMovementsController.cs:367-405`) bucht
  bereits bewusst per `AddAsync` je Zeile (`:399`) mit gemeinsamem `now` — genau dieses Muster ist
  zu übernehmen. **Verboten:** ein Direkt-`_context.StockMovements.AddRange(...)` + einmaliges
  `SaveChanges()` für „Atomarität/Performance" — das umgeht den Decorator, und die Bulk-Einbuchungen
  erreichten Sage nie (stiller Rückschritt des frisch gelieferten Features).
- POST-Handler ermittelt `Timestamp = DateTime.Now` einmalig vor der Schleife und erzeugt je Zeile
  eine `StockMovement` (`ArticleId`, `Quantity`, `StorageLocationId` = Kopf-Wert,
  `ProductionOrder` = Kopf-Wert, `MovementType.Einbuchung`, gemeinsamer Timestamp, Audit-Felder wie
  im bestehenden Einzel-Pfad).
- **Fehlerbehandlung (S2 — alles-oder-nichts + Eingaben erhalten):** Zuerst werden ALLE Zeilen
  validiert (Artikel gesetzt, Menge > 0, Lagerplatz/FA gültig) — VOR dem ersten `AddAsync`. Ist
  auch nur eine Zeile ungültig, wird NICHTS gebucht (keine Teilbuchung, Bewegungshistorie
  unverändert). Das Formular wird mit ALLEN eingegebenen Zeilen + Kopf-Werten neu gerendert (View
  zurückgeben, KEIN `RedirectToAction`, das die Eingaben verliert), die fehlerhafte(n) Zeile(n)
  über `ModelState` markiert, damit der Nutzer korrigieren und erneut speichern kann
  (Freigabe-Antwort 5).
- **Framework-Grenze (S3):** Bewusst keine App-seitige Zeilen-Obergrenze (Freigabe-Antwort 4).
  Wird die ASP.NET-Core-`FormOptions.ValueCountLimit` (Default ~1024) durch sehr viele
  Zeilen-Felder überschritten, ist das mit einer sprechenden Fehlermeldung abzufangen (statt eines
  stillen HTTP-400 aus dem Model-Binding).
- **Scan-Integration (Freigabe-Antworten 2 + B1):** Der Kamera-Scan trägt sequenziell in die
  Zeilen-Liste ein. Bei einem Scan wird geprüft, ob der Artikel bereits gelistet ist: wenn ja,
  Menge der bestehenden Zeile +1; wenn nein, neue Zeile (Menge 1) anlegen. Der optionale Schalter
  „neue Zeile erzwingen" überspringt die Zusammenführung. Umsetzung als Erweiterung von
  `processScannedValue`/`initScanner` in `barcode-scanner.js` — dabei den bestehenden
  Einzel-Scan-Pfad (`initScanner` für die Einzel-Einbuchung) nicht brechen (höchstes
  Regressionsrisiko, siehe H3 der Kritischen Prüfung).

## Migrations-/SQL-Auswirkungen

Keine. `StockMovement` bleibt unverändert; es werden nur mehrere Instanzen der bestehenden
Entität angelegt, wie es `OutboundAllConfirm` bereits heute für Ausbuchungen tut.

## Audit-Feld-Auswirkungen

Jede erzeugte `StockMovement`-Zeile setzt `CreatedAt`/`CreatedBy`/`CreatedByWindows` wie im
bestehenden Einzel-Einbuchungspfad (`_currentUserService`), identisch für alle Zeilen eines
Bulk-Submits (gemeinsamer `Timestamp`).

## Rollen- und Zugriffsfilter-Auswirkungen

Keine neue Rolle. Die neue Action liegt im selben Controller wie `Inbound` und erhält denselben
Filter `[RequireStockAccess]` (admin, stock, stock_keyuser, picking) — konsistent mit der
bestehenden Einbuchung.

## Listen-View-Pattern-Pflichten

Nicht anwendbar — die Mehrfach-Einbuchung ist ein Eingabeformular, keine Listen-View im Sinne von
ADR 0005 (keine Pagination/Spaltenfilter-Pflicht).

## Akzeptanzkriterien

1. Auf dem neuen Formular können mindestens 2 Zeilen mit unterschiedlichen Artikeln + Mengen
   erfasst und in einem Schritt gebucht werden.
2. Nach dem Absenden existiert für jede gültige Zeile genau eine neue `StockMovement`
   (`MovementType.Einbuchung`) mit dem gemeinsamen Lagerplatz und der gemeinsamen FA-Nummer.
3. Lagerplatz- und FA-Feld müssen nur einmal ausgefüllt werden, nicht pro Zeile.
4. Die Erfolgsmeldung nennt die Anzahl gebuchter Zeilen.
5. Eine leere Zeilenliste (0 Zeilen) wird abgelehnt (ModelState-Fehler, analog dem bestehenden
   `ModelState.IsValid`-Check).
6. Jede neu angelegte Zeile ist mit Menge 1 vorbelegt.
7. **Mehrfach-Scan (B1):** Wird derselbe Artikel per Scanner zweimal erfasst, entsteht EINE Zeile
   mit Menge 2 (nicht zwei Zeilen à 1) — außer der Schalter „neue Zeile erzwingen" ist aktiv.
8. **Sage-Regression (S1 — hart):** Jede Bulk-Zeile wird über `IStockMovementRepository.AddAsync`
   gebucht (identischer Pfad wie die Einzel-Einbuchung inkl. Audit UND Sage-Enqueue-Decorator,
   v1.28.0); KEIN Direkt-DbContext-Bypass. Auf einem Sage-freigegebenen Lagerplatz + globalem
   Toggle entsteht damit pro Zeile ein `SageBookingQueueItem`.
9. **Keine Teilbuchung (S2):** Ist mindestens eine Zeile ungültig (Artikel fehlt, Menge ≤ 0),
   wird NICHTS gebucht — die Bewegungshistorie bleibt unverändert (Validierung ALLER Zeilen VOR
   dem ersten `AddAsync`).
10. **Eingaben-Erhalt beim Fehler (S2):** Nach einem Validierungsfehler wird das Formular mit ALLEN
    eingegebenen Zeilen + Kopf-Werten neu gerendert (kein `RedirectToAction`), die fehlerhafte(n)
    Zeile(n) sind markiert; der Nutzer korrigiert und speichert erneut.

## Test-Szenarien

Neue Szenarien in `docs/TESTSZENARIEN.md` Kapitel 2 (Lager):

**Szenario A — Mehrfach-Einbuchung (Happy Path):**
- **Vorbedingung:** Rolle mit `RequireStockAccess` (z. B. `stock`), mindestens 2 existierende
  Artikel, ein buchbarer Lagerplatz.
- **Schritte:** Mehrfach-Einbuchung öffnen, Lagerplatz + FA-Nummer einmalig setzen, 3 Artikel-
  Zeilen mit unterschiedlichen Mengen erfassen, absenden.
- **Erwartetes Verhalten:** 3 neue `StockMovement`-Einträge in der Bewegungshistorie, alle mit
  demselben Lagerplatz/derselben FA-Nummer, jeweils korrekte Artikel/Menge; Erfolgsmeldung nennt
  die Anzahl (3).

**Szenario B — Mehrfach-Scan desselben Artikels zählt hoch (B1):**
- **Schritte:** Denselben Artikel dreimal hintereinander scannen (ohne „neue Zeile erzwingen").
- **Erwartetes Verhalten:** EINE Zeile mit Menge 3; nach Absenden ein `StockMovement` mit Menge 3.
- **Variante:** Schalter „neue Zeile erzwingen" aktiv → drei Zeilen à Menge 1.

**Szenario C — Sage-Enqueue pro Zeile (S1, Regression):**
- **Vorbedingung:** Lagerplatz mit `SageBuchungErlaubt = true`, globaler Sage-Toggle aktiv, 2
  Artikel.
- **Schritte:** Bulk-Einbuchung mit 2 Zeilen auf diesem Lagerplatz absenden.
- **Erwartetes Verhalten:** Pro Zeile entsteht ein `SageBookingQueueItem` (2 Einträge) — identisch
  zur Einzel-Einbuchung; kein DbContext-Bypass, der den Decorator umgeht.

**Szenario D — Negativfall / keine Teilbuchung (S2):**
- **Schritte:** 3 Zeilen erfassen, eine davon ohne Artikelauswahl oder mit Menge 0, absenden.
- **Erwartetes Verhalten:** NICHTS wird gebucht (Bewegungshistorie unverändert, kein
  `StockMovement`); das Formular kommt mit allen 3 eingegebenen Zeilen + Kopf-Werten zurück, die
  fehlerhafte Zeile markiert. Nach Korrektur + erneutem Absenden werden alle 3 gebucht.

`secondbrain/tests/testszenarien-index.md` Kapitel 2 entsprechend ergänzen.

## Deploy

- **Web-App:** ja.
- **Service:** nein.
- **Migration:** nein.
- **Publish-Befehle:**

```
dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
```

## Offene Rückfragen

1. UX-Entscheidung nötig: eigene neue Seite/Route (z. B. `/StockMovements/InboundBulk`) oder ein
   Umschalt-Modus auf der bestehenden Einbuchungsseite? Wirkt sich auf Navigation,
   Rollen-Sichtbarkeit und Testszenarien aus.
2. Soll das Scannen mehrerer Artikel per Kamera-Scanner sequenziell in einer Zeilen-Liste
   erfolgen (Scan fügt automatisch eine neue Zeile hinzu), oder bleibt der Scan-Button je Zeile
   wie bisher (ein Scan pro Zeile, Zeilen manuell hinzufügen)?
3. Bedarfsmeldungen-Erfüllung (`fulfilledRequisitionIds`, aktuell in `Inbound()` verdrahtet) —
   soll das im Mehrfach-Modus pro Zeile einzeln wählbar sein, oder entfällt diese Funktion dort
   bewusst (weniger Bildschirmplatz pro Zeile)?
4. Obergrenze der Zeilenzahl pro Mehrfach-Einbuchung (Performance/Bedienbarkeit auf mobilen
   Handscannern)?
5. Negativ-/Fehlerverhalten: wenn eine von mehreren Zeilen ungültig ist (z. B. Artikel nicht
   gefunden, Menge ≤ 0) — soll die gesamte Buchung transaktional abgelehnt werden
   (alles-oder-nichts) oder sollen gültige Zeilen gebucht und ungültige zurückgemeldet werden
   (analog dem `quick-add`-Skipped-Pattern in `WarehouseRequisitionsApiController`)?

## Freigabe-Antworten (Mensch füllt aus — Schranke 1)

1. →ich würde eine eigene Seite - einfacher für den user
2. →sequenziell
3. →lassen wir bewusst hinten - als idee vormerken
4. →keine
5. →hinweismeldung und korrigieren lassen - danach nochmals speichern

## Kritische Pruefung (2026-08-05)

Anwalt-des-Teufels-Durchsicht vor der Freigabe. Geprueft gegen Spec, Backlog, ADR 0001/0003/0006,
und den echten Code (`StockMovementsController.Inbound`/`OutboundAllConfirm`,
`StockMovementCreateViewModel`, `Repository.AddAsync`, den **gerade gemergten** Sage-Lagerbuchungs-
Decorator auf `IStockMovementRepository.AddAsync`). Alle fuenf Freigabe-Antworten sind beantwortet
und in sich stimmig (eigene Seite, sequenzieller Scan, Fulfillment spaeter, keine Zeilen-Grenze,
Fehler → Hinweis + korrigieren + erneut speichern). Es gibt aber substanzielle Befunde.

### BLOCKER — vor der Freigabe zu klaeren

**B1 — Antwort 2 („sequenziell") legt das Verhalten beim MEHRFACH-Scan desselben Artikels nicht fest.**
„Scan fuegt eine neue Zeile hinzu" ist beim Handscanner-Alltag zweideutig: Wenn der Lagermitarbeiter
zehnmal denselben Artikel scannt (zehn Stueck), soll das **zehn Zeilen a Menge 1** ergeben — oder
**eine Zeile, deren Menge sich auf 10 hochzaehlt**? Beides ist ueblich; die Wahl aendert View, JS und
Testszenarien.
  **Frage an den Menschen:** Beim erneuten Scan eines bereits in der Liste stehenden Artikels —
  (a) jedes Mal eine neue Zeile (Menge bleibt 1, Nutzer korrigiert Mengen manuell), oder
  (b) Menge der bestehenden Zeile +1 (Stueck-fuer-Stueck-Zaehlen)?
  **Empfehlung:** (b) — beim Stueckgut-Wareneingang zaehlt man i. d. R. hoch; ein Toggle „neue
  Zeile erzwingen" kann Sonderfaelle abdecken. Bitte entscheiden.
Antwort B - bei gleichen Artikel Anzahl hochzählen
### SOLLTE — macht den Dev-Lauf sicherer

**S1 — Cross-Feature-Interaktion mit der GERADE gemergten Sage-Lagerbuchung ist nicht bedacht (wichtigster technischer Punkt).**
Jede Bulk-Zeile erzeugt eine `Einbuchung`. Seit v1.28.0 haengt am `IStockMovementRepository.AddAsync`
der Sage-Enqueue-Decorator: eine `Einbuchung` auf einem Sage-freigegebenen Lagerplatz wird an Sage
gemeldet. Das zitierte Vorbild `OutboundAllConfirm` bucht bewusst **per `AddAsync` je Zeile**
(`StockMovementsController.cs:399`) — dadurch feuert der Decorator korrekt pro Zeile. **Risiko:** Wer
fuer „Atomaritaet/Performance" auf einen Direkt-`_context.StockMovements.AddRange(...)` + einmaliges
`SaveChanges()` umbaut, **umgeht den Decorator** — die Bulk-Einbuchungen erreichen Sage dann **nie**
(stiller Rueckschritt des frisch gelieferten Features).
  **Vorschlag:** In-Scope + ein hartes **Regressions-Akzeptanzkriterium**: „Jede Bulk-Zeile wird ueber
  `IStockMovementRepository.AddAsync` gebucht (identischer Pfad wie Einzel-Einbuchung inkl. Audit UND
  Sage-Enqueue); kein Direkt-DbContext-Bypass." Zusatz-Testszenario: Bulk-Einbuchung auf einem
  Lagerplatz mit `SageBuchungErlaubt=true` + globalem Toggle an → pro Zeile entsteht ein
  `SageBookingQueueItem`.
  Antwort: Gute Idee, so umsetzen.

**S2 — Antwort 5 („Hinweis + korrigieren + erneut speichern") verlangt zwei ungenannte Eigenschaften.**
Damit „korrigieren und erneut speichern" funktioniert, muss der POST bei einer ungueltigen Zeile
(a) **die gesamte Buchung ablehnen — KEINE Teilbuchung** (kein `StockMovement` entsteht) und
(b) das Formular **mit den bereits eingegebenen Zeilen** neu rendern (ein naiver `RedirectToAction`
verliert die Eingaben). Beides steht aktuell nicht in der Spec.
  **Vorschlag:** AK6 von „entweder/oder" auf die getroffene Entscheidung festnageln und zwei AKs
  ergaenzen: „Bei mindestens einer ungueltigen Zeile wird NICHTS gebucht (Bewegungshistorie unveraendert)"
  und „nach dem Fehler bleiben alle eingegebenen Zeilen + Kopf-Werte im Formular erhalten, mit
  Markierung der fehlerhaften Zeile(n)". Validierung ALLER Zeilen VOR dem ersten `AddAsync`.
Antwort: passt!

**S3 — Antwort 4 („keine" Obergrenze) trifft eine stillschweigende Framework-Grenze.** ASP.NET Core
begrenzt Form-Felder per `FormOptions.ValueCountLimit` (Default **1024**) und die Model-Binding-
Collection-Groesse; jenseits davon schlaegt das Binden fehl (kein sauberer Fachfehler). „Keine
App-Grenze" ist praktisch ok (am Handscanner zaehlt man Dutzende, nicht Tausende), aber die Spec
sollte das benennen: bewusst **keine** kuenstliche Grenze, aber ein sprechender Hinweis statt eines
stillen HTTP-400, falls die Framework-Grenze doch erreicht wird.

### HINWEIS — Beobachtung ohne Handlungszwang

**H1 — Doppelte Artikel-Zeilen** (derselbe Artikel in zwei Zeilen) erzeugen zwei `StockMovement`-
Zeilen. Das ist konsistent (eine Bewegung je Zeile), sollte aber je nach B1-Entscheidung mitgedacht
werden (bei Variante (b) wuerde ein Re-Scan zusammengefuehrt statt verdoppelt).

**H2 — Body-Aufraeumung nach den Antworten:** Der Out-of-Scope-Vorbehalt „falls Umschalt-Modus"
(Zeilen 60-62) ist mit Antwort 1 (eigene Seite) gegenstandslos; die zurueckgestellte Fulfillment-
Funktion (Antwort 3) bitte als eigene **Ideen-/Backlog-Notiz** vormerken, damit sie nicht verloren geht.

**H3 — Groesse:** ok fuer einen Dev-Lauf (neue Action + ViewModel + View + sequenzielles Scan-JS +
Tests, ~5-6 Dateien, nur Web). Das sequenzielle Scan-JS (`barcode-scanner.js`-Erweiterung) ist der
kniffligste Teil und traegt das meiste Regressionsrisiko fuer den bestehenden Einzel-Scan — dort auf
Nicht-Brechen des bestehenden `initScanner` achten.

### Empfehlung

**NACHBESSERUNG NOETIG: eine offene Entscheidung (B1 Mehrfach-Scan desselben Artikels: neue Zeile vs.
Menge hochzaehlen) plus zwei sicherheitsrelevante Praezisierungen (S1 Sage-Enqueue-Pfad nicht umgehen,
S2 keine Teilbuchung + Eingaben beim Fehler erhalten). Danach ist Teil-2 ein sauberer Web-only-Dev-Lauf.**

## Finalisierung (2026-08-05)

Alle Blocker/SOLLTE-Befunde der Kritischen Pruefung sind aufgeloest; die Spec ist ohne weitere
Rueckfrage umsetzbar. Aufgeloest wie folgt:

- **B1 (Mehrfach-Scan desselben Artikels) — Variante (b), Menge hochzaehlen.** Beim erneuten Scan
  eines bereits gelisteten Artikels wird die Menge der bestehenden Zeile +1 gezaehlt
  (Stueck-fuer-Stueck), plus optionaler Schalter „neue Zeile erzwingen" fuer Sonderfaelle.
  Verankert in Fachliche Anforderung 4, Loesungsentwurf (Scan-Integration), AK 7, Testszenario B.
- **S1 (Sage-Interaktion) — In-Scope + hartes Regressions-AK.** Buchung ausschliesslich ueber
  `IStockMovementRepository.AddAsync` je Zeile (identischer Pfad wie Einzel-Einbuchung inkl. Audit
  UND Sage-Enqueue-Decorator, v1.28.0); Direkt-DbContext-Bypass ausdruecklich verboten. Verankert
  in In-Scope, Loesungsentwurf (Buchungs-Pfad), AK 8, Testszenario C (`SageBookingQueueItem` pro
  Zeile).
- **S2 (Fehlerverhalten) — festgenagelt.** Validierung ALLER Zeilen VOR dem ersten `AddAsync`; bei
  mind. einer ungueltigen Zeile wird NICHTS gebucht (keine Teilbuchung, Bewegungshistorie
  unveraendert); das Formular wird mit ALLEN Zeilen + Kopf-Werten neu gerendert (kein
  `RedirectToAction`), fehlerhafte Zeile(n) markiert. Verankert in Loesungsentwurf
  (Fehlerbehandlung), AK 9 + AK 10, Testszenario D.
- **Standardmenge je Zeile = 1** (Cross-Ref Teil-5 Antwort 2). Verankert in Fachliche
  Anforderung 3, AK 6.
- **S3 (keine kuenstliche Zeilen-Obergrenze)** — Freigabe-Antwort 4 uebernommen, aber die
  Framework-Grenze `FormOptions.ValueCountLimit` (~1024) benannt: sprechende Meldung statt stillem
  HTTP-400. Verankert in Fachliche Anforderung 7, Loesungsentwurf (Framework-Grenze).
- **H2 (Body-Aufraeumung)** — Der Out-of-Scope-Vorbehalt „falls Umschalt-Modus" ist mit der eigenen
  Seite gegenstandslos und entfernt/klargestellt. Das zurueckgestellte Bedarfsmeldungen-Fulfillment
  (Freigabe-Antwort 3) ist als Out-of-Scope-Punkt mit Verweis auf eine eigene Ideen-/Backlog-Notiz
  gefuehrt. **Folgeaufgabe:** Ideen-Notiz „Bulk-Einbuchung: Bedarfsmeldungen-Fulfillment pro Zeile"
  im Backlog anlegen (bewusst zurueckgestellt, nicht verloren).
- **open_questions (Frontmatter)** auf `[]` getrimmt — alle fuenf urspruenglichen Rueckfragen sind
  durch die Freigabe-Antworten + B1 entschieden.

Unveraendert (bewusst): `status: Entwurf`, der Block „## Freigabe-Antworten", die „## Kritische
Pruefung". Kein Anwendungscode angefasst.

BEREIT ZUR FREIGABE
