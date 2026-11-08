---
type: spec
title: OSEON-Relevanzfilter schaltet sich bei jedem Absenden ab (verstecktes false-Feld vor der Checkbox), inkl. Geschwisterfall BDE-Buchung bearbeiten
slug: 2026-11-08-oseon-relevanzfilter-schaltet-sich-ab-spec
status: Freigegeben
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
  - "IdealAkeWms.Tests (View-Text-Waechter + Binder-Nachweis, neu)"
  - "docs/TESTSZENARIEN.md (neu TS-121)"
open_questions: []
epic: false
etappen: []
deploy:
  web: true
  service: false
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

*Vorläufig, der QA-Lauf bestätigt das am echten Diff.*

- **Web-App:** ja (Views + Controller)
- **Service:** nein
- **Migration:** nein
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` (im Worktree)
- Standort beide. OSEON-Teil sinnvoll nur auf AKE-DEV testbar (IDEAL ohne OSEON), BDE-Teil auf beiden.

## Offene Rueckfragen

Keine. Der Umfang (Geschwisterfall BDE) folgt aus dem Auftrag, Fehler am gleichen Muster mitzunehmen.

## Blocker (Grundsatzfragen)

Keine.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  Ausnahme Schranke 1: Freigabe per Auftrag Gerald Weichbold 2026-11-08 (Nachbesserung). Keine offenen Rueckfragen.
-->

– (keine Rückfragen)
