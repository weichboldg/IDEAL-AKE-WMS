---
type: spec
title: "Bugfix Beschichtungsauftrag: Vorschlag ohne Auswahl, Verpackt-Prüfung bei Übergeben, Entfernen nur im Entwurf, Verpackt oder Entfernen"
slug: 2026-11-25-beschichtungsauftrag-vorschlag-ohne-auswahl-spec
status: Entwurf
created: 2026-11-25
updated: 2026-11-25
source_backlog: "[[2026-11-25-beschichtungsauftrag-vorschlag-ohne-auswahl-bug]]"
bug: "[[2026-11-25-beschichtungsauftrag-ohne-auswahl-entfernen-uebergeben-bug]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
version: "1.99.65"
affected_code:
  - IdealAkeWms/Services/BeschichtungsauftragWorkflow.cs (AnlegenAsync :112-142, BearbeitenAsync :242-344, StatusWechselnAsync :484-517)
  - IdealAkeWms/Controllers/BeschichtungsauftraegeController.cs (nur Doku-Kommentar Create :145-147; Logik unverändert)
  - IdealAkeWms/Views/Beschichtungsauftraege/Details.cshtml (:11, :276, :282, :318-336)
  - IdealAkeWms/wwwroot/js/beschichtung-bemerkung.js (gegenseitige Sperre Verpackt/Entfernen)
  - IdealAkeWms/Views/FaHierarchyBeschichtung/_VorschlagAuftraege.cshtml (Tooltip :28, Kommentar :5-6)
  - IdealAkeWms/Views/Help/Index.cshtml (:1436-1446, :1476-1493, :1537-1540)
  - IdealAkeWms/Views/Help/Changelog.cshtml
  - IdealAkeWms/AppVersion.cs + IDEALAKEWMSService/AppVersion.cs (1.99.65)
  - docs/anwender/15-beschichtung-beschichtungsauftrag.md (:35-56, :67, :75, :87-91, :144-147)
  - docs/TESTSZENARIEN.md (TS-162)
  - IdealAkeWms.Tests/Services/BeschichtungsauftragWorkflowTests.cs
  - IdealAkeWms.Tests/Services/BeschichtungsauftragVergabeAnzeigeTests.cs
  - IdealAkeWms.Tests/Controllers/BeschichtungsauftraegeControllerTests.cs
open_questions:
  - "1 Ohne Haken = alle freien Positionen des HauptFA, alle offen (nicht verpackt); mit Haken bleibt es bei genau den angehakten, alle verpackt (A3 unverändert)?"
  - "2 Verpackt-Prüfung (alle verpackt, mindestens 1 Position) bei Entwurf → Übergeben UND weiter bei Übergeben → Versendet?"
  - "3 In Übergeben weiter änderbar: Haken, Menge, Hinzufügen (nur Entfernen gesperrt)? Folge: eine dort hinzugefügte oder abgehakte Position kann nicht mehr raus."
  - "4 Verpackt oder Entfernen: im UI sperrt „Entfernen“ den Haken „Verpackt“ der Zeile, der Server lehnt den Widerspruch mit Positionsnennung ab?"
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

Der Entwurf soll die Packliste sein. Heute muss der Packende schon in der Vorschlagsliste wissen, was in die Kiste kommt, sonst
entsteht kein Auftrag. Geprüft wird erst beim Versenden, ob alles verpackt ist. Nach dem Übergeben lassen sich noch Positionen
entfernen. Und „Verpackt“ und „Entfernen“ lassen sich gleichzeitig anhaken, dann entscheidet das System still für Entfernen.

Gerald 2026-11-25 (Backlog): ohne Auswahl erstellbar; Prüfung erst bei Entwurf → Übergeben; Löschen ab Übergeben nicht mehr
möglich; Verpackt und Entfernen entweder oder.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** die vier Punkte oben, beide Standorte, Hilfe, Anwenderdoku, TS-162, Tests.

**Out-of-Scope:**
- Haken in der Vorschlagsliste speichern (v1.99.41, Rückfrage 8 bleibt).
- „Alle anhaken“ im Entwurf. Nachrüsten, wenn lange Listen das Abhaken zäh machen.
- Storno oder Entfernen aus Übergeben. Storno bleibt nur aus Entwurf (`BeschichtungsauftragWorkflow.cs:71`).
- Ein eigener Hinweis im Entwurf „n Positionen offen“. Die Ablehnung beim Übergeben nennt die Positionen ohnehin.

## Fachliche Anforderungen

### Ist-Stand (belegt; Bündel-Worktree, Stand v1.99.64)

1. **Leere Auswahl abgelehnt.** Der Controller baut immer eine `VorschlagAuswahl`, auch eine leere
   (`BeschichtungsauftraegeController.cs:162-171`). `AnlegenAsync` lehnt `IstLeer` ab (`BeschichtungsauftragWorkflow.cs:113-114`).
   Der Pfad `auswahl == null` → `AlleFreien` existiert (`:121-122`, `:145-169`). Kein Anwendungscode ruft ihn auf, nur Tests
   (Suche `AnlegenAsync(` außerhalb `*Tests*`: einziger Aufrufer `BeschichtungsauftraegeController.cs:171`).
   `AlleFreien` meldet schon „keine beschichteten Positionen“ und „alle bereits vergeben“ als Fehler (`:148-164`). Vergebene
   Positionen kommen als Hinweis (`:165-167`). Flach (AKE) und hierarchisch (IDEAL) laufen durch denselben Code (`:118-123`).
2. **Alles aus dem Vorschlag wird verpackt**, auch auf dem Pfad `null` (`:138`, ohne Unterscheidung).
3. **Verpackt-Prüfung nur bei Versendet** (`:494-503`). Bei Übergeben wird nur der Dienstleister geprüft (`:492-493`).
4. **Positionen änderbar in Entwurf und Übergeben** (`PositionenAenderbar`, `:233-234`). Das gilt für Haken (`:304`), Menge
   (`:303`), Entfernen (`:307-311`) und Hinzufügen (`:312`, neu = `Verpackt false`). Der Controller lädt die Kandidaten nach
   derselben Regel (`BeschichtungsauftraegeController.cs:106`). Die Detailseite zeigt die Spalte „Entfernen“, wenn `bearbeitbar`
   gilt (`Details.cshtml:14`, `:276`, `:330-336`).
5. **Verpackt und Entfernen unabhängig.** Zwei Checkboxen (`Details.cshtml:322`, `:333`), beide im `entwurf-form`. Das Formular
   sendet immer `hakenGesendet=true` (`:138`). `BearbeitenAsync` setzt erst den Haken (`:304`), entfernt dann (`:307-311`),
   ohne Prüfung.
6. **Statusknöpfe speichern zuerst** (v1.99.56): Bei bearbeitbarem Auftrag senden sie `entwurf-form` mit `ziel`
   (`Details.cshtml:107-110`). Der Controller speichert, danach kommt der Statuswechsel. Scheitert nur der Wechsel, heißt es
   „Gespeichert.“ plus Warnung (`BeschichtungsauftraegeController.cs:216-226`). Haken, die im selben Klick gesetzt werden,
   zählen also schon für die neue Übergeben-Prüfung.
7. **Hilfe schon vorgreifend:** Laut Hilfe kann Übergeben an „nicht alles verpackt“ scheitern (`Views/Help/Index.cshtml:1444-1445`,
   ebenso `docs/anwender/15-…md:75`). Der Code prüft das bisher nicht (Punkt 3). Mit diesem Fix stimmt der Text.

### Was sich gegenüber v1.99.41 ändert

Spec [[2026-11-18-beschichtungsauftrag-ausbau-checkboxen-versand-druck-spec]]:

| Dort | Neu |
|---|---|
| A3 / AK 4: kein Haken = Fehler „Keine Position angehakt — nichts angelegt.“ | kein Haken = alle freien Positionen, offen (B1) |
| A6 / AK 10: alle verpackt nur für Versendet | zusätzlich für Übergeben (B2) |
| A8 / AK 8, 9: Entfernen in Entwurf **und** Übergeben | Entfernen nur im Entwurf (B3) |
| Rückfrage 6: „Der Rest wird durch Entfernen abgehängt“ | gilt nur noch im Entwurf |
| – | Verpackt und Entfernen schließen sich aus (B4) |

A1, A2, A4, A5, A7, A9–A14 bleiben unverändert.

### Soll

| ID | Anforderung |
|---|---|
| B1 | **Ohne Auswahl erstellbar.** Ist die Auswahl leer (keine Schlüssel, keine Summiert-Artnr, keine Position ohne FA), legt `AnlegenAsync` den Entwurf wie auf dem Pfad `null` mit **allen freien** beschichteten Positionen an (`AlleFreien`). Diese Positionen sind **offen** (`Verpackt = false`). Mit Auswahl gilt A3 unverändert: nur die angehakten Positionen, alle verpackt. Die Meldung „Keine Position angehakt — nichts angelegt.“ entfällt. Die Fehler aus `AlleFreien` (keine beschichteten / alle vergeben) bleiben. |
| B2 | **Verpackt-Prüfung bei Übergeben.** Entwurf → Übergeben nur, wenn der Auftrag mindestens eine Position hat und alle verpackt sind. Sonst Fehler mit den offenen Positionen (`PositionBezeichnung`): „Übergeben nur, wenn alle Positionen verpackt sind — nicht verpackt (n): … Bitte anhaken oder entfernen.“ bzw. „{Nummer} hat keine Positionen — Übergeben nicht möglich.“ Die Dienstleister-Prüfung kommt zuerst (wie heute). Die Prüfung bei Versendet bleibt als Absicherung; ihr Text endet auf „Bitte anhaken.“, weil in Übergeben nichts mehr entfernt werden kann. |
| B3 | **Entfernen nur im Entwurf.** `BearbeitenAsync` lehnt nicht leere `entfernteIds` außerhalb des Entwurfs ab: „Positionen entfernen nur im Entwurf — {Nummer} steht auf {Status}.“ Nichts wird gespeichert. Die Spalte „Entfernen“ erscheint nur im Entwurf; `colspan` der Leerzeile (`Details.cshtml:282`) zieht mit. Haken, Menge und Hinzufügen bleiben in Übergeben (laut Rückfrage 3). Dann steht in Übergeben über „Positionen hinzufügen“ und „Positionen ohne FA“ der Hinweis „In Übergeben hinzugefügte Positionen lassen sich nicht mehr entfernen.“ |
| B4 | **Verpackt oder Entfernen.** Server: steht eine Position-Id in `verpacktIds` **und** in `entfernteIds`, lehnt `BearbeitenAsync` die ganze Änderung ab, je Position: „Position {Bezeichnung}: „Verpackt“ und „Entfernen“ zugleich angehakt — nur eines wählen.“ UI: Wird „Entfernen“ angehakt, wird „Verpackt“ derselben Zeile abgehakt und gesperrt (`disabled`). Wird „Entfernen“ wieder abgehakt, ist „Verpackt“ wieder frei (abgehakt). Die Sperre ist sichtbar (gesperrte Checkbox) und für Screenreader benannt (`aria-disabled` kommt über `disabled`; `title` „Wird entfernt“). Keine Farbe als einziger Träger. |

## Technischer Loesungsentwurf

Ponytail-Leiter: vorhandene Pfade nutzen. Kein neuer Parameter, keine neue Klasse.

1. **`AnlegenAsync` (B1):**
   - Statt der Ablehnung bei `IstLeer` gilt: `var alle = auswahl is null || auswahl.IstLeer;`. Dann
     `alle ? AlleFreien(...) : await AuswahlAsync(...)`.
   - `:138`: `p.Verpackt = !alle`. Der Kommentar „A3: was aus der Vorschlagsauswahl kommt, ist verpackt“ wird präzisiert.
   - Controller `Create` bleibt unverändert, nur der Doku-Kommentar (`:145-147`, „Nichts angehakt = Warnung“) ändert sich.
2. **`StatusWechselnAsync` (B2):** Die Prüfung `:494-503` wird ein privater Baustein
   `VerpacktPruefen(Beschichtungsauftrag a, string zielText, string abhilfe)`. Er wird für `Uebergeben` (nach der
   Dienstleister-Prüfung) und für `Versendet` aufgerufen. Fehlermeldungsliste wie heute über `PositionBezeichnung`.
3. **`BearbeitenAsync` (B3, B4):** Vor dem `Anwenden` und bei den anderen Eingabeprüfungen (`:256-264`):
   - `if (entfernteIds.Count > 0 && a.Status != BeschichtungsauftragStatus.Entwurf)` → Fehler (B3).
   - `foreach (var p in a.Positionen.Where(p => entfernteIds.Contains(p.Id) && verpacktIds?.Contains(p.Id) == true))` → Fehler (B4).
   - `PositionenAenderbar` bleibt (Haken/Menge/Hinzufügen in Übergeben). Der Doc-Kommentar `:232` nennt Entfernen nicht mehr.
4. **Details.cshtml (B3, B4):**
   - `var entfernbar = bearbeitbar && Model.IstEntwurf;` → Spalte `:276`, Zellen `:330-336`, `colspan` `:282`.
   - Kommentar `:11` nachziehen.
   - Checkboxen: `data-verpackt` / `data-entfernen` je Zeile, damit das Skript die Paare findet (z. B. `data-pos="@p.Id"`).
5. **JS (B4):** ein paar Zeilen in `wwwroot/js/beschichtung-bemerkung.js`. Die Datei ist das Seitenskript der Detailseite und
   wird immer geladen (`Details.cshtml:443`); eine neue Datei lohnt sich dafür nicht. Ablauf: `change` auf `[data-entfernen]`
   → zugehöriges `[data-verpackt]` `checked = false`, `disabled = this.checked`. Weil die Prüfung „ungespeicherte Änderungen“
   den `FormData`-Stand vergleicht (`:12-16`), gilt eine gesperrte Checkbox richtig als Änderung.
6. **Vorschlag (B1):** `_VorschlagAuftraege.cshtml:28` Tooltip: „Auftrag im Entwurf anlegen: mit den angehakten Positionen
   (verpackt) — ohne Haken mit allen freien Positionen (offen) — plus Positionen ohne FA“. Kommentar `:5-6` nachziehen.
   `Index.cshtml` / `Summiert.cshtml` (Vorschlag) brauchen keine Änderung (Haken-Spalte unverändert, `Index.cshtml:132-139`,
   `Summiert.cshtml:133-140`).
7. **Frontend-Pflicht:** Der Dev-Lauf ruft `frontend-design` explizit auf (CLAUDE.md). Die Änderung ist klein (Spalte nur im
   Entwurf, gesperrte Checkbox). Bootstrap und das vorhandene Markup gewinnen.

## Migrations-/SQL-Auswirkungen

Keine. Begründung: Es ändern sich nur Regeln in `BeschichtungsauftragWorkflow`, eine View und ein Skript. Neue Spalten,
Tabellen oder Statuswerte gibt es nicht. `Verpackt` (bit) und alle Status-Werte gibt es seit SQL/161 (v1.99.41, TS-162-Kopf).
Kein `SQL/`-Skript, kein FreshInstall, keine EF-Migration. `SQL/AgentJobs/`: nicht betroffen. Kein Agent-Job schreibt
Beschichtungsaufträge (Spec v1.99.41, Abschnitt Migrationen; der Dev-Lauf wiederholt die Suche `Beschichtungsauftrag` in
`SQL/AgentJobs/`).

**Altbestand (keine Daten-Änderung):**
- Entwürfe mit offenen Positionen, z. B. Altaufträge vor v1.99.41 (Rückfrage 15 dort: `Verpackt = 0`) oder später
  hinzugefügte Positionen: Sie müssen vor dem Übergeben angehakt oder entfernt werden. Gewollt (B2).
- Aufträge, die schon in Übergeben sind: Entfernen geht nicht mehr. Haken setzen geht weiter.

## Einstellungen

keine.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine neuen Schreibwege. Die Anlage setzt `Created*` über `Erstellt` (`:127`, `Snapshot` `:668`), Bearbeiten setzt `Modified*`
über `Geaendert` (`:305`, `:313`), der Statuswechsel ebenfalls (`:515`). Abgelehnte Änderungen (B2–B4) schreiben nichts: Die
Prüfung läuft vor `Anwenden` bzw. vor dem Setzen des Status.

**Rollen / Zugriff:** unverändert. Schreiben mit `beschichtungsauftrag_schreiben` (`[RequireBeschichtungsauftragWriteAccess]`
an `Create`, `Bearbeiten`, `Status`, `BeschichtungsauftraegeController.cs:150`, `:184`, `:262`). Lesen ohne Checkboxen.
**Listen-View-Pattern:** keine neue Tabelle. Die Detailtabelle bleibt im Client-Modus (Ausnahme laut `Details.cshtml:261`).
Die Spalte „Entfernen“ hat weiterhin kein `data-col-key` und `data-csv-export="false"`.

## Akzeptanzkriterien

1. Vorschlagsliste (Liste und Summiert), kein Haken, keine Position ohne FA → „Aus Vorschlagsliste erstellen“ legt einen
   Entwurf mit allen freien beschichteten Positionen des HauptFA an. Alle sind „offen“. Keine Warnung „Keine Position angehakt“.
2. Bei Teilvergabe nimmt der leere Vorschlag nur die freien Positionen. Der Hinweis nennt die schon vergebenen.
3. Mit Haken (Liste, Summiert oder nur Position ohne FA) entsteht der Entwurf wie bisher nur aus der Auswahl, alles „verpackt“.
4. Entwurf mit mindestens einer offenen Position → „Auf Übergeben setzen“ → abgelehnt, die Meldung nennt jede offene Position.
   Status bleibt Entwurf. Haken und andere Eingaben aus demselben Klick sind gespeichert („Gespeichert.“ + Warnung).
5. Entwurf ohne Positionen → Übergeben abgelehnt („keine Positionen“).
6. Alle angehakt, Dienstleister gesetzt → Übergeben gelingt. Fehlt der Dienstleister, kommt zuerst dessen Meldung.
7. Übergeben → Versendet: ist in Übergeben ein Haken gelöst oder eine Position hinzugefügt worden, wird abgelehnt
   („… Bitte anhaken.“). Nach dem Anhaken gelingt es.
8. In Übergeben hat die Positionstabelle keine Spalte „Entfernen“. Ein direkter POST mit `entfernen` wird abgelehnt
   („Positionen entfernen nur im Entwurf“). Nichts ist geändert, auch nicht Haken oder Menge aus demselben POST.
9. In Übergeben lassen sich Haken, Menge und Hinzufügen weiter speichern (laut Rückfrage 3). Über dem Hinzufügen steht der
   Hinweis, dass solche Positionen nicht mehr entfernbar sind.
10. Entwurf: „Entfernen“ anhaken → „Verpackt“ derselben Zeile ist abgehakt und gesperrt. „Entfernen“ abhaken → „Verpackt“
    wieder bedienbar.
11. Direkter POST mit derselben Id in `verpackt` und `entfernen` → abgelehnt, die Meldung nennt die Position, nichts gespeichert.
12. Reklamations-Entwurf: B2–B4 gelten genauso (Entfernen im Entwurf, Prüfung bei Übergeben).
13. AKE (flache Quelle) und IDEAL verhalten sich gleich.
14. Build und alle Tests grün. Die angepassten und neuen Tests (unten) sind Teil des Laufs.

## Test-Szenarien

**docs/TESTSZENARIEN.md, TS-162** (Kopfzeile um „ab v1.99.65 geändert: 162.3, 162.7, 162.8; neu 162.21–162.24“ ergänzen):

| Nr | Änderung |
|---|---|
| 162.3 | **ersetzt:** nichts anhaken, erstellen → Entwurf mit allen freien Positionen, alle „☐ offen“; bei Teilvergabe Hinweis mit den vergebenen |
| 162.7 | **ersetzt:** Übergeben: Haken lösen, Menge ändern, Kandidat + Position ohne FA hinzufügen, Speichern → gespeichert; Spalte „Entfernen“ fehlt; Negativ: POST mit `entfernen` (Entwicklertools) → „Positionen entfernen nur im Entwurf“ |
| 162.8 | **ergänzt:** Versendet-Meldung endet auf „Bitte anhaken.“ |
| 162.21 | Entwurf mit 1 offenen Position, Dienstleister gesetzt → „Auf Übergeben setzen“ → abgelehnt, Position genannt, übrige Eingaben gespeichert; anhaken → Übergeben gelingt |
| 162.22 | Negativ: Entwurf ohne Positionen → Übergeben abgelehnt; Entwurf ohne Dienstleister und mit offener Position → Dienstleister-Meldung zuerst |
| 162.23 | Entwurf: bei einer verpackten Position „Entfernen“ anhaken → „Verpackt“ abgehakt + gesperrt; wieder lösen → bedienbar; Speichern mit Entfernen → Position weg und im Vorschlag frei |
| 162.24 | Negativ: POST mit gleicher Id in `verpackt` und `entfernen` (Entwicklertools, Sperre umgangen) → abgelehnt, Position genannt, nichts geändert. Terminal / nur Tastatur: gesperrte Checkbox erkennbar ohne Farbe |

Standort: DEV **AKE und IDEAL** (an DEV AKE `FaHierarchyBeschichtungAktiv` vorher `true`, danach Ausgangswert, wie TS-162).
Danach `secondbrain/tests/testszenarien-index.md` nachziehen.

**Unit-Tests (betroffen):**
- `BeschichtungsauftragWorkflowTests`:
  - Fixture `Status` (`:120-125`): vor `Uebergeben` alle Positionen anhaken (wie `Versenden` `:127-131`). Sonst scheitern alle
    Tests mit `fx.Anlegen()` + Übergeben, weil der Pfad `null` jetzt offen anlegt.
  - `Uebergeben_OhneDienstleister_…` (`:368`): vor dem zweiten Übergeben anhaken.
  - `Anlegen_PositionenAusVorschlag_SindVerpackt` (`:374-380`) → umdrehen: ohne Auswahl offen. Neuer Test: leere
    `VorschlagAuswahl` = alle freien, offen.
  - `Versendet_NichtAlleVerpackt_…` (`:395`): erwarteter Text „anhaken“ statt „anhaken oder entfernen“.
  - `Bearbeiten_InUebergeben_HakenMengeEntfernenKandidatUndFrei_…` (`:425-457`) teilen: Haken, Menge, Kandidat und frei in
    Übergeben bleiben grün; Entfernen in Übergeben → Fehler, nichts geändert; Entfernen im Entwurf → sofort frei.
  - neu: Übergeben mit offener Position bzw. ohne Positionen abgelehnt; Verpackt + Entfernen gleiche Id abgelehnt.
- `BeschichtungsauftragVergabeAnzeigeTests` (`:110`): vor Übergeben anhaken.
- `BeschichtungsauftraegeControllerTests`:
  - `Create_NichtsAngehakt_Warnung_KeinAuftrag` (`:144-153`) → umdrehen: Redirect auf Details, 3 Positionen, alle offen.
  - Tests mit `Anlegen()` laufen über `Alle` (angehakt, `:92-100`) und bleiben grün.
  - neu: `Bearbeiten` mit `entfernen` in Übergeben → Warnung, nichts geändert.
- `BeschichtungFlacheQuelleTests`: rufen `AnlegenAsync(hauptFa)` ohne Übergeben auf (Suche `Uebergeben|Verpackt` ohne
  Treffer). Voraussichtlich unverändert; der Dev-Lauf bestätigt das am Testlauf.

**Hilfe / Anwenderdoku:**
- `Views/Help/Index.cshtml`: Kurzanleitung `:1436-1446` (ohne Haken = alle offen, im Entwurf abhaken, Übergeben prüft),
  Status `:1476-1493` (Übergeben nur alles verpackt, Entfernen nur im Entwurf, Verpackt/Entfernen schließen sich aus),
  Störungen `:1537-1540` (Eintrag „Keine Position angehakt“ ersetzen durch „Übergeben abgelehnt: nicht verpackt“).
- `Views/Help/Changelog.cshtml`: Eintrag v1.99.65.
- `docs/anwender/15-beschichtung-beschichtungsauftrag.md`: `:35-56` (Abschnitt Anhaken, Satz „Nichts angehakt“ `:56`), `:67`,
  `:75`, `:87-91` (Übergeben, Entfernen nicht mehr), FAQ `:144-147`. Stand-Version im Index. Fotos per Playwright gegen DEV
  (Entwurf mit Spalte Entfernen, Übergeben ohne).

## Etappen (nur bei epic: true)

entfällt (kein Epic).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

*Vorläufig, der qa-agent bestätigt am Diff.*

- **Web-App:** ja (Workflow, View, JS, Hilfe)
- **Service:** nein (der Dienst schreibt keine Beschichtungsaufträge)
- **Migration:** nein
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`): keine. Kein SQL, keine Einstellung, kein Ordner. Die Verhaltensänderung
  steht im Anwender-Changelog.
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` (im Worktree)
- **Version:** 1.99.65 in beiden `AppVersion.cs` (Worktree-Stand 1.99.64, `IdealAkeWms/AppVersion.cs:5`; keine andere Spec in
  `entwurf/` reserviert 1.99.65+). Der Dev-Lauf prüft das erneut.

## Offene Rueckfragen

1. **Ohne Haken: alle freien Positionen offen oder leerer Auftrag?** Mit Haken bleibt A3 (nur die angehakten, alle verpackt).
   **Empfehlung:** alle freien, offen. Den Pfad gibt es schon (`AlleFreien`, `:145-169`), samt Meldungen für „alles vergeben“
   und Teilvergabe. Er entspricht dem Verhalten vor v1.99.41 (Teil-4-Antwort 2). Im Entwurf wird dann abgehakt und der Rest
   entfernt. Ein leerer Auftrag hieße, jede Position einzeln über „Positionen hinzufügen“ zu holen. Die Vorschlagsliste wäre
   dann umsonst geöffnet.
2. **Verpackt-Prüfung bei Übergeben und weiter bei Versendet?** **Empfehlung:** an beiden Stellen. In Übergeben lassen sich
   Haken lösen (`:304`) und offene Positionen hinzufügen (`:312`). Ohne die zweite Prüfung ginge so ein Auftrag mit
   unverpackten Teilen auf Versendet. Die Prüfung bei Versendet kostet einen Aufruf desselben Bausteins.
3. **In Übergeben: Haken, Menge, Hinzufügen weiter erlaubt?** v1.99.41 Rückfrage 10 hat es so freigegeben. Gerald nennt jetzt
   nur Löschen. **Folge:** Wer in Übergeben eine Position hinzufügt oder einen Haken löst, kann sie nicht mehr entfernen und den
   Auftrag nicht stornieren (Storno nur aus Entwurf, `:71`). Versendet geht dann erst, wenn sie angehakt ist. Fehlt das Teil
   wirklich, bleibt nur: anhaken und mitschicken, oder der Auftrag hängt. **Empfehlung:** Haken und Menge erlauben. Hinzufügen
   ebenfalls erlauben, weil die Anwenderdoku ausdrücklich auf „Teil vergessen → am Auftrag hinzufügen“ setzt
   (`docs/anwender/15-…md:144`). Unter „Positionen hinzufügen“ in Übergeben kommt ein Hinweis „In Übergeben nicht mehr
   entfernbar“. Alternative: Hinzufügen und Haken in Übergeben sperren (`PositionenAenderbar` = nur Entwurf, nur Menge bleibt).
   Dann ist Übergeben eingefroren. Sackgassen gibt es dann nicht, Nachträge gehen aber nur über einen neuen Auftrag.
4. **Entweder-oder: UI-Sperre und Server-Ablehnung?** **Empfehlung:** ja, beides. Das UI sperrt „Verpackt“, solange
   „Entfernen“ angehakt ist, denn Entfernen ist die stärkere Absicht. Der Server lehnt einen Widerspruch ab und nennt die
   Position, statt still zu entscheiden (CLAUDE.md „melden statt still behandeln“). Ein Radio-Paar je Zeile („verpackt / offen /
   entfernen“) wäre ohne JS möglich. Es ändert aber die Spalte „Verpackt“, die Client-Filter und den CSV-Export
   (`Details.cshtml:275`, `:321`), also mehr Umbau als nötig.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Ohne Haken = alle freien Positionen, offen; mit Haken A3 unverändert → **Gerald 2026-11-25: „ohne Haken, User fragen:
   ‚Positionen ohne Verpackt-Kennzeichen‘ aus BA entfernen oder nicht verpackt beibehalten.“** Offen (R-1): Wann wird gefragt –
   (a) beim Erstellen aus der Vorschlagsliste (nicht angehakte Positionen weglassen oder als offen übernehmen), oder (b) beim
   Wechsel Entwurf → Übergeben (offene Positionen jetzt entfernen oder Übergeben abbrechen)? → **(b) (Gerald 2026-11-25):
   Beim Wechsel Entwurf → Übergeben mit offenen Positionen fragt das WMS: „Positionen ohne Verpackt-Kennzeichen entfernen“
   (dann übergeben) oder „nicht verpackt beibehalten“ (übergeben mit offenen Positionen). Erstellen ohne Haken übernimmt alle
   freien Positionen offen. Harte Sperre „alle verpackt“ erst bei Versendet.**
2. Verpackt-Prüfung bei Übergeben und weiter bei Versendet → **passt (Gerald 2026-11-25).**
3. In Übergeben Haken, Menge, Hinzufügen erlaubt (nur Entfernen gesperrt) → **„so erlauben. dann wäre aber entfernen
   wahrscheinlich auch noch …“ (Gerald 2026-11-25, Satz unvollständig).** Offen (R-2): Entfernen in Übergeben doch erlauben –
   für alle Positionen oder nur für nicht verpackte? → **korrekt (Gerald 2026-11-25): in Übergeben dürfen nicht verpackte
   Positionen entfernt werden, verpackte nicht; Entwurf unverändert.**
4. Verpackt oder Entfernen: UI-Sperre + Server-Ablehnung mit Positionsnennung → **entweder verpackt oder entfernen, nicht beides
   (Gerald 2026-11-25).**
