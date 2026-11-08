---
type: spec
title: OSEON-Relevanzfilter schaltet sich bei jedem Absenden ab (verstecktes false-Feld vor der Checkbox), inkl. Geschwisterfall BDE-Buchung bearbeiten
slug: 2026-11-08-oseon-relevanzfilter-schaltet-sich-ab-spec
status: Testbereit
created: 2026-11-08
updated: 2026-11-08
source_backlog: "[[2026-11-08-oseon-relevanzfilter-schaltet-sich-ab]]"
bug: "[[2026-11-08-oseon-relevanzfilter-schaltet-sich-ab-bug]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
version: "1.92.0"
sql_nummern: keine
standort: beide
affected_code:
  - "IdealAkeWms/Views/Tracking/OseonIndex.cshtml:52-54 (hidden useRelevanceFilter vor Checkbox)"
  - "IdealAkeWms/Views/BdeBookings/Edit.cshtml:102-103 (hidden Quantities[i].IsFinal vor Checkbox)"
  - "IdealAkeWms/Views/BdeBookings/Edit.cshtml:107-108 (hidden Quantities[i].Delete vor Checkbox)"
  - "IdealAkeWms/Controllers/BdeBookingsController.cs:206-247 (POST Edit, neue Pruefung hoechstens eine Final-Zeile)"
  - "IdealAkeWms/Controllers/BdeBookingsController.cs:230-262 (QA B-1: neu finale Zeilen erst im zweiten Speichern, eine Transaktion; Commit 9c9046c4)"
  - "IdealAkeWms.Tests (View-Text-Waechter + Binder-Nachweis, neu)"
  - "docs/TESTSZENARIEN.md (neu TS-121)"
open_questions: []
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold (Auftrag Nachbesserung)"
freigabe_am: 2026-11-08
---

> [!note] Ausnahme Schranke 1
> Gerald hat am 2026-11-08 ausdrücklich verfügt, dass diese Spec direkt in `specs/freigegeben/` angelegt wird.

## Ziel / Nutzen (das Warum)

Auf der OSEON-Teileverfolgung schaltet jedes Absenden der Filterkarte den Filter „Fremdsystem-Relevanz“ still aus. Das gilt auch für den
Artikel-Scan. Der Werker sieht dann alle Arbeitsgänge statt nur der relevanten und merkt nicht, warum. Derselbe Fehler steckt in „BDE-Buchung
bearbeiten“. Dort sind die Folgen schwerer: „Final“ wird beim Speichern still auf `false` gesetzt, und „Löschen“ wirkt nie.

## Umfang (In-Scope / Out-of-Scope)

**Suche im Worktree (Stand v1.88.0).** Gesucht wurde in allen `*.cshtml`/`*.js`/`*.html` nach `value="false"`, unabhängig von der
Attributreihenfolge. Treffer:

| Fundstelle | Muster | Fehler? |
|---|---|---|
| `Views/Tracking/OseonIndex.cshtml:52-53` | hidden `useRelevanceFilter` **vor** Checkbox | **ja** – in Umfang |
| `Views/BdeBookings/Edit.cshtml:102-103` | hidden `Quantities[@i].IsFinal` **vor** Checkbox | **ja** – in Umfang |
| `Views/BdeBookings/Edit.cshtml:107-108` | hidden `Quantities[@i].Delete` **vor** Checkbox | **ja** – in Umfang |
| `Views/MissingParts/Index.cshtml:89` | hidden **hinter** Checkbox (Muster v1.81.0) | nein |
| `Views/PickingAreas/Edit.cshtml:29, 35, 41` | hidden **hinter** Checkbox | nein |
| `Views/PickingLeitstand/Index.cshtml:80` | hidden `release`, keine Checkbox gleichen Namens | nein |
| `Views/OrderRecipients/Edit.cshtml:73` | hidden `IsActive` in eigenem Formular ohne Checkbox | nein |
| `Views/FaCompletion/Edit.cshtml:246` | `<option value="false">` | nein |
| `wwwroot/js/settings-fields.js:9` | JS setzt den Wert eines einzelnen Hidden-Felds, kein Doppelname | nein |

**Warum BDE-Bearbeiten mit in den Umfang gehört:** Der Mechanismus ist identisch. Würde nur OSEON repariert, bliebe der Geschwisterfall
kaputt. Er wirkt sogar auf Daten (Final-Kennzeichen, Löschen) und nicht nur auf eine Anzeige.

**In-Scope:** die drei Fundstellen oben. Dazu die Prüfung „höchstens eine Final-Zeile“ im BDE-POST. Ohne sie würde der Fix einen unbehandelten
DB-Fehler freilegen, siehe Lösungsentwurf.
**Out-of-Scope:** Reparatur früher durch den Fehler verlorener `IsFinal`-Werte. Dafür ist nur eine Prüfabfrage vorgesehen (Abschnitt
Migrations-/SQL). Ebenfalls out of scope: Änderungen an `TrackingController` und `oseon-tracking-lazy.js`, beide sind nach dem Fix korrekt
(Belege unten).

## Fachliche Anforderungen

1. „Fremdsystem-Relevanz“ behält nach jedem Absenden den Zustand, den der Benutzer gewählt hat (angehakt bleibt angehakt, abgewählt bleibt
   abgewählt).
2. Ein Aufruf ohne Parameter (Menü, „Zurücksetzen“, Links aus FA-Liste/Leitstand) startet mit gesetztem Haken. So ist es heute schon (Default
   `true`), und das bleibt so.
3. BDE-Buchung bearbeiten: „Final“ und „Löschen“ wirken so, wie sie angehakt sind.
4. Hakt der Benutzer bei einer Buchung „Final“ an mehr als einer verbleibenden Zeile an, erscheint eine sichtbare Formular-Fehlermeldung und es
   wird nichts gespeichert. Es darf keine 500er-Seite kommen.

## Technischer Loesungsentwurf

**Binder-Nachweis (belegt, nicht angenommen):**
- *Im Browser belegt* für OSEON: Angehakt geht `useRelevanceFilter=false&useRelevanceFilter=true` raus, gebunden wird `false`
  ([[2026-11-08-oseon-teileverfolgung-doppelter-klickhandler-spec]] Z. 260-262, AKE-DEV).
- *Framework:* Einfache Typen bindet ASP.NET Core über `SimpleTypeModelBinder`, der `ValueProviderResult.FirstValue` liest. Das gilt für
  Query und Form und ebenso für `bool?`, weil `Nullable<bool>` über denselben Provider und TypeConverter läuft. Im Umfang ist kein `bool?`
  betroffen: `TrackingController.cs:303` und `:371` sind `bool`, ebenso `BdeBookingEditViewModel.cs:23` (`IsFinal`) und `:25` (`Delete`). Die
  BDE-Stellen binden per POST-Form in eine Collection-Property. Der Endbinder für das bool ist derselbe. Den Nachweis im Projekt liefert der
  Test unter AK 6. Sonst ist diese Aussage für BDE nur gelesen und auf DEV nicht nachgestellt.

**Fix (Leiter Sprosse 2, vorhandenes Muster v1.81.0, `MissingParts/Index.cshtml:85-89`):**
1. `OseonIndex.cshtml`: Zeile 52 (hidden) **hinter** die Checkbox (Z. 53-54) verschieben. Dazu einen Kommentar wie in
   `MissingParts/Index.cshtml:87-88`. Angehakt geht dann `true,false` raus → `true`. Abgewählt geht `false` raus → `false`. Ohne Parameter
   greift der Default `true` (`TrackingController.cs:303`).
2. `BdeBookings/Edit.cshtml`: Zeile 102 hinter 103 und Zeile 107 hinter 108 setzen.
3. `BdeBookingsController.Edit` (POST, `:206`): Vor dem Speichern prüfen, wie viele Zeilen `vm.Quantities` mit `!Delete && IsFinal` hat. Bei
   mehr als einer: `ModelState.AddModelError` mit „Nur eine Mengenzeile je Buchung darf ‚Final‘ sein.“, danach `LoadEditSelectLists()` und
   `View(vm)` wie in `:208-212`. **Grund:** Der eindeutige gefilterte Index `IX_BdeBookingQuantities_Booking_Final`
   (`ApplicationDbContext.cs:1502-1505`, `[IsFinal] = 1`) erlaubt nur eine Final-Zeile je Buchung. Bisher konnte der Fehler nie auftreten, weil
   immer `false` ankam. Nach dem Fix würde `UpdateAsync` mit einer DB-Ausnahme scheitern. Das Prinzip lautet „Melden statt still behandeln“.

**Folgewege geprüft, keine Änderung nötig:**
- `oseon-tracking-lazy.js:30-33, 49`: `URLSearchParams.get` liefert den ersten Wert. Nach dem Fix ist das `true` (angehakt) bzw. `false`.
  Heute liefert die Zeile im angehakten Fall `false`, also ist das Nachladen der Gruppen heute ebenso falsch und wird mitkorrigiert.
- Pagination `site.js:118-121`: übernimmt alle anderen Query-Parameter unverändert. Die Reihenfolge der Doppelwerte bleibt dabei erhalten.
- „Zurücksetzen“ (`OseonIndex.cshtml:64`) sowie `_ProductionOrderRow.cshtml:124` und `_PickingLeitstandRow.cshtml:190` senden kein
  `useRelevanceFilter` → Default `true`.

## Migrations-/SQL-Auswirkungen

Keine Migration, kein `SQL/`-Skript, keine FreshInstall-Änderung, keine AgentJobs.

**Prüfabfrage (nur zur Information, unverifiziert, der Mensch führt sie auf der Produktions-DB aus).** Sie findet Buchungen, die seit dem
BDE-Edit möglicherweise ihr Final-Kennzeichen verloren haben. Gezeigt werden Buchungen ohne Final-Zeile, deren Mengenzeilen ein Benutzer
nachträglich geändert hat:
```sql
SELECT b.Id, b.Status, MAX(q.ModifiedAt) AS LetzteAenderung, MAX(q.ModifiedBy) AS GeaendertVon
FROM BdeBookings b JOIN BdeBookingQuantities q ON q.BdeBookingId = b.Id
WHERE b.IsCancelled = 0
GROUP BY b.Id, b.Status
HAVING SUM(CAST(q.IsFinal AS int)) = 0 AND MAX(q.ModifiedAt) IS NOT NULL;
```
Ergebnisse sind Kandidaten und kein Beweis. Eine Datenkorrektur ist nicht Teil dieser Spec. Gibt es Treffer, kommt eine Aufgabe nach
`secondbrain/aufgaben/`.

## Einstellungen

keine

## Audit-Feld-Auswirkungen

Keine neuen Felder. Der BDE-POST setzt `ModifiedAt/ModifiedBy/ModifiedByWindows` an Buchung und Zeilen schon heute
(`BdeBookingsController.cs:222-224, 239-241`). Neu ist nur, dass sich `IsFinal` tatsächlich ändert und gelöschte Zeilen wirklich entfernt
werden. OSEON ist eine reine Lese-Seite.

## Akzeptanzkriterien

1. `OseonIndex.cshtml`: Das Hidden-Feld `useRelevanceFilter` steht im Quelltext **hinter** `id="useRelevanceFilter"`.
2. OSEON, Haken gesetzt, Artikel scannen bzw. „Filtern“: Die URL enthält `useRelevanceFilter=true&useRelevanceFilter=false`, der Haken bleibt
   gesetzt und die Liste bleibt auf relevante Arbeitsgänge gefiltert.
3. OSEON, Haken abgewählt, „Filtern“: Die URL enthält nur `useRelevanceFilter=false`, der Haken bleibt leer. Ein weiteres Absenden ändert
   daran nichts.
4. OSEON ohne Parameter (`/Tracking/OseonIndex`, „Zurücksetzen“): Der Haken ist gesetzt.
5. `BdeBookings/Edit.cshtml`: Beide Hidden-Felder (`IsFinal`, `Delete`) stehen hinter ihrer Checkbox.
6. Ein Unit-Test bindet `bool` und `bool?` mit den Werten `["true","false"]` über den echten MVC-Binder (`SimpleTypeModelBinder` mit
   Query- oder Form-ValueProvider) und erwartet `true`. Mit `["false","true"]` erwartet er `false`. Der Test hält den Binder-Nachweis im
   Projekt fest.
7. View-Text-Wächter (Muster `FehlteileKarteV181Tests.cs:148-161`): Er schlägt fehl, sobald in `OseonIndex.cshtml` oder
   `BdeBookings/Edit.cshtml` ein Hidden-`false`-Feld vor seiner gleichnamigen Checkbox steht.
8. BDE-Edit: „Final“ an genau einer Zeile anhaken → speichern → erneut öffnen: Der Haken ist gesetzt, in der DB steht `IsFinal = 1`.
9. BDE-Edit: „Löschen“ anhaken → speichern: Die Zeile ist weg.
10. BDE-Edit: „Final“ an zwei verbleibenden Zeilen → Die Formular-Fehlermeldung erscheint, nichts wird gespeichert, es kommt kein 500er. Dazu
    gibt es einen Controller-Test in `BdeBookingsControllerTests.cs`.
11. `dotnet build` und `dotnet test` laufen grün. Beide `AppVersion.cs` stehen auf 1.92.0, und der Anwender-Changelog hat einen Eintrag.

## Test-Szenarien

Neu **TS-121** in `docs/TESTSZENARIEN.md` (Kapitel 7 „OSEON Teileverfolgung“, BDE-Teil mit Verweis auf Kap. 36), danach
`secondbrain/tests/testszenarien-index.md` nachziehen.

- **TS-121.1 Haken bleibt nach Artikel-Scan.** Vorbedingung: Standort AKE (OSEON-Daten), mindestens ein Auftrag mit nicht relevanten
  Arbeitsgängen. Schritte: `/Tracking/OseonIndex` öffnen (Haken gesetzt) → Artikel per QR/Handscanner scannen. Erwartet: Der Haken ist
  gesetzt, nur relevante Arbeitsgänge erscheinen, auch in aufgeklappten (nachgeladenen) Gruppen.
- **TS-121.2 Abgewählt bleibt abgewählt.** Haken abwählen → „Filtern“ → Werkbank wechseln → „Filtern“. Erwartet: Der Haken bleibt bei beiden
  Absendungen leer, und alle Arbeitsgänge erscheinen.
- **TS-121.3 Direktaufruf ohne Parameter = Default true.** `/Tracking/OseonIndex` direkt aufrufen, außerdem „Zurücksetzen“ und den
  OSEON-Link aus der FA-Liste nutzen. Erwartet: Der Haken ist jeweils gesetzt.
- **TS-121.4 Seitenwechsel.** Haken gesetzt, Filter abgeschickt, auf Seite 2 wechseln. Erwartet: Der Haken ist weiter gesetzt.
- **TS-121.5 BDE Final speichern.** BDE-Admin öffnet eine Buchung → „Final“ an einer Zeile → Speichern → erneut öffnen. Erwartet: Der Haken
  ist gesetzt.
- **TS-121.6 BDE Löschen.** „Löschen“ an einer Zeile → Speichern. Erwartet: Die Zeile fehlt.
- **TS-121.7 Negativ BDE zwei Final.** „Final“ an zwei Zeilen → Speichern. Erwartet: Die Fehlermeldung erscheint im Formular, die Daten sind
  unverändert, es kommt keine Fehlerseite.
- **TS-121.8 Negativ Rolle.** Ein Benutzer ohne BDE-Admin ruft `/BdeBookings/Edit/{id}` auf. Erwartet: Zugriff verweigert, wie bisher
  (`RequireBdeAdminAccess`, `BdeBookingsController.cs:204`).

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt (kein Epic) | – | – |

## Deploy

*Final, QA Reparatur 1 am echten Diff `cf869e97` + `9c9046c4`.*

- **Web-App:** ja (Views, `BdeBookingsController`, Changelog, `AppVersion`)
- **Service:** ja, nur `IDEALAKEWMSService/AppVersion.cs` (Versionsgleichstand)
- **Migration:** nein, kein SQL, kein Einmal-Skript
- **Publish-Befehle** (im Worktree, Teil des Bündels):
  - `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  - `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- Ablauf: Publish aus dem Worktree → Testsystem → Test → Merge. Nach dem Merge nur dann erneut aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen zusammengeführt hat.
- Standort beide. OSEON-Teil sinnvoll nur auf AKE testbar (IDEAL ohne OSEON), BDE-Teil auf beiden.

## Offene Rueckfragen

Keine. Der Umfang (Geschwisterfall BDE) folgt aus dem Auftrag, Fehler am gleichen Muster mitzunehmen.

## Blocker (Grundsatzfragen)

Keine.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  Ausnahme Schranke 1: Freigabe per Auftrag Gerald Weichbold 2026-11-08 (Nachbesserung). Keine offenen Rueckfragen.
-->

– (keine Rückfragen)

## Umsetzung (Dev 2026-11-08, v1.92.0)

Commit `cf869e97`. Hidden-„false“ hinter die Checkbox in `Tracking/OseonIndex.cshtml` und `BdeBookings/Edit.cshtml` (IsFinal, Delete);
`BdeBookingsController.Edit` lehnt mehr als eine verbleibende Final-Zeile ab („Nur eine Mengenzeile je Buchung darf „Final“ sein.“). Die
Edit-Seite hatte keine Validation-Summary → ergänzt (`ModelOnly`, Muster der übrigen Masken), sonst wäre die Meldung unsichtbar.
Tests `CheckboxHiddenFeldV192Tests` (echter `SimpleTypeModelBinder` bool/bool? in beiden Reihenfolgen, View-Wächter, BDE-Edit). TS-121.

## QA v1.92.0 (2026-11-08) – Ergebnis: NICHT Testbereit (Befund B-1)

qa-agent. Prüfbereich: Commit `cf869e97`. Der Status bleibt `InUmsetzung`.

### 1. Build, Tests, Deploy
- Build 0 Fehler; Tests Web 2542/1/0, Dienst 507/0/0 (Worktree HEAD `2efdb023`).
- AKE-DEV und IDEAL-DEV laufen auf v1.94.0 aus dem Worktree; IDEAL hat dieser QA-Lauf deployt (Exit 0).
- `deploy.service` ist auf `true` korrigiert: Der Commit ändert `IDEALAKEWMSService/AppVersion.cs` (nur Version).

### 2. Code-Review
Gelesen: `OseonIndex.cshtml`, `oseon-tracking-lazy.js:49-55` (liest `useRelevanceFilter` per Query, erster Wert, Default `true`), `BdeBookings/Edit.cshtml`, `BdeBookingsController.Edit` (POST), `CheckboxHiddenFeldV192Tests`.
Geschwisterfälle per Grep über alle Views: Alle übrigen Hidden-„false“-Felder mit gleichnamiger Checkbox stehen bereits dahinter (`MissingParts/Index`, `PickingAreas/Edit`). `OrderRecipients/Edit:73` und `PickingLeitstand/Index:80` haben keine Checkbox gleichen Namens.

**Befund B-1 (blockierend, DEV reproduziert): „Final“ von einer Zeile auf eine andere verschieben ergibt einen 500er.**
- Ort: `IdealAkeWms/Controllers/BdeBookingsController.cs:229-245` (Schleife setzt `q.IsFinal = row.IsFinal` für alle Zeilen, dann ein `UpdateAsync`).
- Ablauf: Buchung mit Final an Zeile 2, der Benutzer setzt Final an Zeile 1 und nimmt es an Zeile 2 weg. Die neue Prüfung (Z. 210) zählt nur eine Final-Zeile und lässt durch. EF schreibt die UPDATEs in Schlüsselreihenfolge: Zeile 1 wird `IsFinal=1`, solange Zeile 2 noch 1 ist. Der gefilterte UNIQUE-Index `IX_BdeBookingQuantities_Booking_Final` schlägt an.
- DEV-Beleg: Buchung 12, `/BdeBookings/Edit/12` → Fehlerseite (Anfrage-ID `00-e50b98e44e6288fc5234180878ba0255-…`). Web-Log `D:/Dev/AKE/Web/logs/idealakewms-20261108.log`: „Cannot insert duplicate key row in object 'dbo.BdeBookingQuantities' with unique index 'IX_BdeBookingQuantities_Booking_Final'. The duplicate key value is (12).“ Daten unverändert (Rollback).
- Bezug: AK 8 („Final an genau einer Zeile anhaken → speichern“) und AK 10 („kein 500er“). Vor v1.92.0 kam „Final“ nie an, deshalb war der Weg bisher verdeckt. Es ist dieselbe Klasse wie QA Q2 bei den Kommissionierbereichen (EF ordnet UPDATEs nicht nach einem gefilterten UNIQUE-Index).
- Vorschlag: In `Edit` erst alle Zeilen, die Final verlieren, auf `false` setzen und speichern, dann die neue Final-Zeile setzen und speichern, beides in einer Transaktion (`BdeBookingService` hat bereits ein `InTransactionAsync`-Muster). Alternativ ablehnen mit Meldung „Final zuerst an der bisherigen Zeile abwählen und speichern“. Der InMemory-Test kann das nicht zeigen (kein UNIQUE-Index); Nachweis auf DEV.

Keine weiteren Befunde.

### 3. Beweisart je AK
| AK | Beweis |
|---|---|
| 1, 5, 7 Hidden hinter Checkbox | automatisch (View-Text-Wächter) |
| 6 Binder nimmt den ersten Wert | automatisch (echter `SimpleTypeModelBinder`) |
| 2, 3, 4 OSEON im Browser | Manual-UAT, auf DEV erledigt |
| 8 Final speichern | Manual-UAT: nur „setzen, wo keine Final-Zeile ist“ OK, „verschieben“ FEHLER (B-1) |
| 9 Löschen | automatisch nur der Binder; Browser nicht ausgeführt |
| 10 zwei Final → Formularfehler | automatisch (InMemory) + DEV OK |

### 4. Geänderte bestehende Tests
`BdeBookingsControllerTests`: neuer `internal` Zugriff `CreateForTests` (reine Typänderung, keine Erwartung geändert).

### 5. Checkliste
Keine Migration, keine Einstellung. Version 1.92.0, Anwender- und Brain-Changelog (`2026-11-08-v1-92-0-oseon-relevanzfilter-bde-checkboxen`) vorhanden. TS-121 in `docs/TESTSZENARIEN.md` und im Index.

### 6. DEV-Testlauf (AKE http://localhost:8081)
| Schritt | Ergebnis | |
|---|---|---|
| 121.3 `/Tracking/OseonIndex` direkt | Haken gesetzt | OK |
| 121.1 Haken gesetzt, „Filtern“ | URL `…useRelevanceFilter=true&useRelevanceFilter=false`, Haken bleibt gesetzt | OK |
| 121.1 Artikel per QR/Handscanner | kein Scanner am QA-Platz | NICHT MOEGLICH, für den Menschen |
| 121.2 Haken abwählen, „Filtern“ | URL nur `useRelevanceFilter=false`, Haken leer | OK |
| 121.4 Seite 2 (abgewählt und gesetzt) | `…=false&page=2` bzw. `…=true&useRelevanceFilter=false&page=2`, Haken jeweils erhalten | OK |
| 121.7 Buchung 12: Final an zweiter Zeile zusätzlich | „Nur eine Mengenzeile je Buchung darf „Final“ sein.“ im Formular, Daten unverändert | OK |
| 121.5a Final an Zeile 2 abwählen, speichern | SQL: beide `IsFinal=0` (Abwahl kommt an) | OK |
| 121.5b danach Final an Zeile 1 setzen, speichern | SQL: Zeile 1 `IsFinal=1` | OK |
| 121.5c Final in einem Schritt von Zeile 2 auf Zeile 1 verschieben | **500er-Fehlerseite**, siehe B-1 | FEHLER |
| 121.6 Löschen | nicht ausgeführt (echte Mengenzeile auf DEV) | NICHT MOEGLICH ohne Datenverlust, für den Menschen |
| 121.8 Rolle | unverändert (`RequireBdeAdminAccess`), nicht ausgeführt | NICHT MOEGLICH (kein zweites Konto) |

Zurückgesetzt: Buchung 12 und ihre Mengenzeilen per SQL auf die Ausgangswerte (IsFinal, ReportedAt, Start/Ende, Modified*).
Screenshots (temporär):
- `C:/Users/GERALD~1.WEI/AppData/Local/Temp/claude-chrome-screenshots-cF2Dyv/screenshot-1794124787504-10.jpg` (Formularfehler zwei Final)
- `C:/Users/GERALD~1.WEI/AppData/Local/Temp/claude-chrome-screenshots-cF2Dyv/screenshot-1794124829135-11.jpg` (500er)

### Fehlerbericht
- Was: TS-121.5 in der Variante „Final verschieben“.
- Erster Fehler: SqlException 2601 auf `IX_BdeBookingQuantities_Booking_Final`.
- Vermutete Ursache: Reihenfolge der EF-UPDATEs gegenüber dem gefilterten UNIQUE-Index (`BdeBookingsController.cs:229-245`).
- Reparaturversuch 1 offen.

### Manuelle Checkliste (nach dem Fix)
1. Final von Zeile 2 auf Zeile 1 in einem Speichern verschieben: gespeichert, kein 500er.
2. TS-121.1 mit Handscanner.
3. TS-121.6 Löschen einer Testzeile.
4. TS-121.8 mit einem Konto ohne BDE-Admin.

*Überholt durch „QA Reparatur 1“ unten.*

## QA Reparatur 1 v1.92.0 (2026-11-08) – Ergebnis: Testbereit

qa-agent. Prüfbereich: Diff `2efdb023..9c9046c4` (Fix B-1). Build und Tests über den ganzen Worktree (HEAD `9c9046c4`).

### 1. Build, Tests, Deploy
- `dotnet build IdealAkeWms.slnx`: 0 Fehler (28 Warnungen, unverändert vorhanden).
- `dotnet test --no-build`: IDEALAKEWMSService.Tests 507 bestanden / 0 übersprungen / 0 Fehler; IdealAkeWms.Tests 2543 / 1 / 0.
- AKE-DEV (http://localhost:8081): vom Dev-Lauf mit diesem Stand deployt. Geprüft: `D:/Dev/AKE/Web/IdealAkeWms.dll` vom 09:11:27, also nach Commit `9c9046c4` (09:10:50). Die Fußzeile zeigt v1.94.0, das ist die `AppVersion` des Worktrees (Bündel). Nicht erneut deployt.
- IDEAL-DEV: in diesem Lauf nicht neu deployt. Der Fix betrifft nur den Controller und ist auf AKE-DEV belegt.

### 2. Code-Review (`BdeBookingsController.cs:230-262`, Test, TS)
Gelesen: `Edit` (POST) ganz, `BdeBookingRepository.UpdateAsync` (Z. 167-171: `Update` + `SaveChangesAsync` auf demselben scoped `ApplicationDbContext` wie `_ctx` im Controller, also wirkt die Transaktion für beide Speicherungen), `Program.cs:144` (Repository scoped).
- Reihenfolge stimmt: Das erste `UpdateAsync` schreibt das Löschen der Zeilen, das Abwählen der alten Final-Zeile und alle übrigen Felder. Die neue Final-Zeile wird dabei zwar mitgeschrieben, behält aber `IsFinal = 0`. Erst das zweite `UpdateAsync` setzt sie auf 1. Zu keinem Zeitpunkt gibt es zwei Final-Zeilen, auch nicht, wenn die alte Final-Zeile im selben Speichern gelöscht wird.
- Transaktion: Es gibt keine `EnableRetryOnFailure`/Execution-Strategy (Grep über `IdealAkeWms/*.cs` ohne Treffer). Eine Benutzer-Transaktion ist daher zulässig. Scheitert das zweite Speichern, rollt `await using` ohne Commit zurück. Unter InMemory ist `tx` null, der Ablauf bleibt gleich.
- Unverändert: Die Vorprüfung „höchstens eine Final-Zeile“ (Z. 210) läuft vor dem Laden. Sie ist serverseitig, nicht nur im Browser.
- Hinweis, kein Befund: Im Web-Log steht beim Speichern die EF-Warnung `SavepointsDisabledBecauseOfMARS`. Ursache ist MARS in der Verbindungszeichenfolge zusammen mit der expliziten Transaktion. Das ist harmlos, weil kein Savepoint gebraucht wird.
- Hinweis, kein Befund, nicht Teil dieses Diffs: Rendert der POST nach einem Formularfehler die Seite neu, zeigt „Effektive Zeit“ nur „—“. `ViewBag.EffectiveDuration` wird nur im GET gesetzt. Das ist kosmetisch und stammt aus `cf869e97`.
- Hinweis, älteres Verhalten: Beim Speichern rundet das Formular `ReportedAt` auf Millisekunden.

**Befunde: keine.**

### 3. Beweisart je AK (Ergänzung)
| AK | Beweis |
|---|---|
| 8 Final speichern inkl. verschieben | Ergebnis automatisch (`BdeEdit_FinalVerschieben_Ergebnis_QaB1`, InMemory ohne Index). Die Reihenfolge gegen den UNIQUE-Index ist Manual-UAT und auf DEV erledigt (OK). |
| 9 Löschen | Manual-UAT auf DEV erledigt (OK) |
| 10 zwei Final → Formularfehler | automatisch + DEV OK |
| übrige | wie im ersten QA-Lauf |

### 4. Geänderte bestehende Tests
`BdeBookingsControllerTests.CreateForTests`: Bekommt jetzt `HttpContext` und `TempData`. Das ist eine reine Testinfrastruktur-Änderung, weil der Erfolgsweg `TempData["SuccessMessage"]` setzt. Keine Erwartung wurde geändert. Neu: `CheckboxHiddenFeldV192Tests.BdeEdit_FinalVerschieben_Ergebnis_QaB1`.

### 5. Checkliste
Keine Migration, keine Einstellung, keine Versionsänderung im Fix-Commit. Version 1.92.0 bleibt der Eintrag dieser Spec. TS-121.5b steht in `docs/TESTSZENARIEN.md` (Worktree), der Index ist nachgezogen.

### 6. DEV-Testlauf (AKE http://localhost:8081, Buchung 12, BDE-Admin Gerald Weichbold)
Ausgangszustand (SQL): Zeile 1 `IsFinal=0`, Zeile 2 `IsFinal=1`.
| Schritt | Ergebnis | |
|---|---|---|
| 121.5b Final von Zeile 2 auf Zeile 1, Speichern | POST 302 → Liste; SQL: Zeile 1 = 1, Zeile 2 = 0; kein „duplicate key“ im Web-Log | OK |
| 121.5 erneut öffnen | Haken an Zeile 1 gesetzt, an Zeile 2 leer | OK |
| 121.5b zurück: Final von Zeile 1 auf Zeile 2, Speichern | POST 302; SQL: Zeile 1 = 0, Zeile 2 = 1 | OK |
| 121.7 Final zusätzlich an Zeile 1 (zwei Final), Speichern | POST 200, im Formular „Nur eine Mengenzeile je Buchung darf „Final“ sein.“, SQL unverändert | OK |
| 121.6 per SQL angelegte Testzeile (Id 27, Gut 9, `CreatedBy='QA-TS-121.6'`) „Löschen“, Speichern | POST 302; SQL: Zeile 27 ist weg, Zeilen 1 und 2 unverändert | OK |
| 121.1 Handscanner | kein Scanner am QA-Platz | NICHT MOEGLICH, für den Menschen |
| 121.8 Rolle | kein zweites Konto | NICHT MOEGLICH, für den Menschen |

Web-Log `D:/Dev/AKE/Web/logs/idealakewms-20261108.log`: POSTs in Z. 831, 852 und 888 mit 302, in Z. 871 mit 200. Alle vier „duplicate key“-Treffer im Log (Z. 551-659) stammen aus dem ersten QA-Lauf.
Zurückgesetzt: Buchung 12 und die Zeilen 1/2 per SQL auf die Ausgangswerte. Dazu gehören `IsFinal`, `ReportedAt` mit voller Genauigkeit, Start/Ende/Status und `Modified*`, bei den Zeilen `NULL`. Die Testzeile 27 hat der Test selbst gelöscht. Danach geprüft: Alles ist identisch mit dem Ausgangszustand.
Screenshot (temporär): `C:/Users/GERALD~1.WEI/AppData/Local/Temp/claude-chrome-screenshots-cF2Dyv/screenshot-1794125735917-12.jpg` (Formularfehler bei zwei Final).

### Manuelle Testcheckliste für den Menschen (offen)
1. TS-121.1 auf AKE mit Handscanner: Der Haken bleibt nach dem Scan gesetzt, auch in aufgeklappten Gruppen.
2. TS-121.8 mit einem Konto ohne BDE-Admin: `/BdeBookings/Edit/{id}` verweigert den Zugriff.
3. Testsystem-Lauf (Publish aus dem Worktree, Web und Service): TS-121.2 bis 121.7 einmal durchspielen, besonders 121.5b (Final verschieben) auf AKE und auf IDEAL.
4. Optional, wenn eine Buchung mit drei Zeilen vorhanden ist: In einem Speichern die bisherige Final-Zeile löschen und eine andere Zeile final setzen. Erwartet: gespeichert, kein 500er.
