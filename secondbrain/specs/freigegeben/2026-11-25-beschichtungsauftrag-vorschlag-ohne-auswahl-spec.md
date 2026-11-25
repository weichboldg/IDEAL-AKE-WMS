---
type: spec
title: "Bugfix Beschichtungsauftrag: Vorschlag ohne Auswahl, Verpackt-Prüfung bei Übergeben, Entfernen nur im Entwurf, Verpackt oder Entfernen"
slug: 2026-11-25-beschichtungsauftrag-vorschlag-ohne-auswahl-spec
status: Testbereit
freigabe_von: Gerald
freigabe_am: 2026-11-25
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

## Deploy (final, qa-agent 2026-11-25, aus dem Diff `8ffa4c4a^..8ffa4c4a`)

- **Web-App:** ja (`IdealAkeWms/`: Workflow, Controller-Kommentar, Details/Vorschlag-View, `beschichtung-bemerkung.js`, Hilfe, Changelog)
- **Service:** nein (nur `IDEALAKEWMSService/AppVersion.cs`, kein Verhalten). Paket enthält trotzdem beide Komponenten (eine Version).
- **Migration:** nein (keine Datei unter `*/Migrations/`, kein `SQL/`-Skript)
- **Manifest** `scripts/deploy/nachlauf.psd1`: kein Eintrag nötig (kein SQL, keine Einstellung, kein Ordner, keine Leitstand-Aktion). Altbestand: Entwürfe mit offenen Positionen fragen jetzt beim Übergeben; Aufträge in Übergeben: verpackte nicht mehr entfernbar (Anwender-Changelog v1.99.65).
- **Publish (erledigt):** `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` mit `IDEALAKEWMSWeb`, `IDEALAKEWMSWebService`, `SQL\`, `deploy\`; `publish-info.txt`: Version 1.99.65, Commit `8ffa4c4ab51d7dce0681304eea64ac7a37c81997`, Branch `feature/2026-08-07-ideal-teile-1-5`.
- **Reihenfolge Testsystem:** `publish\` kopieren, dann `deploy\Update-IdealAkeWms.ps1` (Web; Dienst nur Versionsstand). Kein DB-Backup-Sonderfall (keine Schemaänderung).
- Nach dem Merge nur dann neu aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen zusammengeführt hat:
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`

## QA-Nachweis (qa-agent, 2026-11-25)

Worktree `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5`, HEAD `8ffa4c4a`, geprüfter Bereich `8ffa4c4a^..8ffa4c4a`.
Maßstab: Freigabe-Antworten (R-1: Rückfrage beim Übergeben statt harter Sperre; R-2: in Übergeben nur nicht verpackte entfernbar)
vor dem Spec-Text. Daraus folgende Abweichungen vom Soll-Text (B2-Sperre bei Übergeben, B3 „Entfernen nur im Entwurf“,
Versendet-Text „Bitte anhaken.“) sind gewollt und kein Befund.

### 1. Build und Tests
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 30 Warnungen (alt, nicht aus diesem Diff).
- `dotnet test`: `IdealAkeWms.Tests` 3504 bestanden / 1 übersprungen / 0 Fehler (3505); `IDEALAKEWMSService.Tests` 790 / 0 / 0.

### 2. Code-Review (selbst, synchron)
Geprüft: `BeschichtungsauftragWorkflow` (AnlegenAsync, `Entfernbar`, BearbeitenAsync-Prüfungen vor `Anwenden`, StatusWechselnAsync),
Controller `Create`/`Bearbeiten` (Speichern, dann Status), `Details.cshtml` (Spalte Entfernen, Modal, `data-*`), `beschichtung-bemerkung.js`
(Sperre, Modal, `requestSubmit`, Zusammenspiel mit dem Capture-Handler `data-bemerkung-pruefen`), `_VorschlagAuftraege.cshtml`, Hilfe,
Anwenderdoku, TS-162. Bekannte Fehlerklassen: keine Werte in `<script>` (Bezeichnung über `data-*` + `textContent`, Razor-kodiert);
kein `hidden` + `d-*` (Modal über Bootstrap); kein stiller Fallback; keine neue Parallel-Array-Bindung (`freiArtikelId`/`freiMenge`
unverändert); B4 und R-2 erzwingt der Server, das UI ist nur Komfort; eine Regel `Entfernbar` für Server und View.

| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| B-1 | niedrig | `PositionBezeichnung` (Artnr + Pos.) ist an IDEAL nicht eindeutig: gleiche Artnr/Pos. unter verschiedenen VaterFA. Auf DEV IDEAL zeigte das Übergeben-Fenster zweimal „50002326 (Pos. 1)“, die Versendet-Meldung „50000296 (Pos. 1), 50000296 (Pos. 1)“. Der Helper ist alt (v1.99.41), wird durch das neue Fenster aber häufiger gesehen. | offen, Entscheid Mensch: VaterFA in die Bezeichnung aufnehmen (eine Stelle, `PositionBezeichnung` in `BeschichtungsauftragWorkflow.cs`). Nicht mitgebaut (Umfang). |
| B-2 | niedrig | `beschichtung-bemerkung.js`: Die gewählte Modal-Schaltfläche wird vor `requestSubmit` gesperrt. Blockiert die Browser-Validierung das Absenden (ungültiges Feld im `entwurf-form`), bleibt das Fenster mit gesperrter Schaltfläche offen; Ausweg Abbrechen bzw. neu laden. Auf DEV nicht aufgetreten. | offen, kosmetisch; kein Datenrisiko (nichts gesendet). |
| B-3 | Hinweis | F1 bestätigt: „entfernen“ bei lauter offenen Positionen ergibt Übergeben mit 0 Positionen (StatusWechselnAsync prüft bei Übergeben keine Anzahl, Versendet sperrt mit „keine Positionen“). F2 auf DEV AKE bestätigt (kein Fenster, Server meldet Dienstleister, nichts entfernt). F3 aus dem Code bestätigt (ohne JS = beibehalten). | bekannt, von den Antworten gedeckt; F1 als Entscheid in der Checkliste. |
| B-4 | Hinweis | Brain-Changelog v1.99.65 und `feature-map.md`-Eintrag fehlen noch. | Brain-Update am Ende des goal-Laufs. |

### 3. Beweisart je Akzeptanzkriterium (nach Freigabe-Antworten)
| AK | Beweis |
|---|---|
| 1 ohne Haken alle frei, offen | automatisiert (`Create_NichtsAngehakt_AlleFreienOffen`, `Anlegen_OhneAuswahl_AlleFreienOffen`) + DEV IDEAL/AKE OK |
| 2 Teilvergabe, Hinweis | automatisiert (`Anlegen_LeereAuswahl_AlleFreienOffen_HinweisVergebene`) + DEV OK |
| 3 mit Haken verpackt | automatisiert (bestehende Tests über `Alle`, neuer Test Teil 1) |
| 4/5/6 Übergeben | laut R-1 Rückfrage statt Sperre: Fenster = Browser-JS, Manual-UAT (DEV OK); Server: `Uebergeben_MitOffenenPositionen_Erlaubt_SperreErstBeiVersendet` automatisiert |
| 7 Versendet-Sperre | automatisiert + DEV OK |
| 8 Entfernen in Übergeben (R-2) | automatisiert (`Bearbeiten_InUebergeben_VerpackteEntfernen_Abgelehnt_NichtsGeaendert`, `…OffeneEntfernen_SofortFrei`, Controller-Test); Spalte „nicht entfernbar“ DEV OK |
| 9 Haken/Menge/Hinzufügen in Übergeben | automatisiert + DEV OK |
| 10 UI-Sperre | Browser-JS, Manual-UAT, DEV IDEAL OK |
| 11 Server lehnt Verpackt+Entfernen ab | automatisiert (Theory Entwurf/Übergeben); echte Formular-Bindung nicht InMemory-beweisbar |
| 12 Reklamations-Entwurf | gleicher Code, kein eigener Test, Manual-UAT offen |
| 13 AKE = IDEAL | DEV beide Standorte OK |
| 14 Build/Tests | grün (oben) |

### 4. Geänderte bestehende Tests
- `BeschichtungsauftraegeControllerTests.Create_NichtsAngehakt_Warnung_KeinAuftrag` → `…_AlleFreienOffen`: Verhalten absichtlich geändert (B1, AK 1).
- `BeschichtungsauftragWorkflowTests.Anlegen_PositionenAusVorschlag_SindVerpackt` → `Anlegen_OhneAuswahl_AlleFreienOffen`: absichtlich geändert (B1).
- `BeschichtungsauftragWorkflowTests.Bearbeiten_InUebergeben_HakenMengeEntfernenKandidatUndFrei_…`: p1/p2 vor Übergeben angehakt, damit p3 offen und entfernbar bleibt: absichtlich geändert (R-2, AK 8/9).
- `BeschichtungsauftragVergabeAnzeigeTests.Reklamation_ZaehltMit_…`: Haken vor Versendet gesetzt, weil die Anlage ohne Auswahl jetzt offen ist: Folge von B1 (Setup, keine Erwartung abgeschwächt).
- Kein Fall „sonstiges“.

### 5. Checkliste CLAUDE.md
Migration/SQL/FreshInstall/AgentJobs: entfällt (keine Schemaänderung). Audit: keine neuen Schreibwege, Prüfungen vor `Anwenden`
(DEV: `ModifiedBy`/`ModifiedAt` an geänderten Positionen gesetzt). Version 1.99.65 in beiden `AppVersion.cs`, Anwender-Changelog ja.
`docs/TESTSZENARIEN.md` TS-162 (162.3/.7/.8 ersetzt, 162.21–162.25 neu) und `testszenarien-index.md` nachgezogen. Hilfe,
`docs/anwender/15-…` und Index-Stand 1.99.65. ADR 0020: keine Einstellung. Brain-Changelog/feature-map: siehe B-4.

### 6. DEV-Deploy und Testlauf
- `standort: beide`. `dev-deploy.ps1` (erhöhte Shell) aus dem Worktree: **AKE** Exit 0, „HTTP 200, Fusszeile v1.99.65“, http://localhost:8081/;
  **IDEAL** Exit 0, „HTTP 200, Fusszeile v1.99.65“, http://localhost:8082/. DEV-Absicherung durch das Skript neu gesetzt.
- Browser-Screenshots liefen in ein Zeitlimit; geprüft wurde daher über den Barrierefreiheitsbaum (Zustand `disabled`, Fenster-Inhalt)
  und `sqlcmd` gegen die DEV-DB.

| Schritt | Ort / Daten | Ergebnis | Status |
|---|---|---|---|
| 162.3 Liste ohne Haken | IDEAL HauptFA 1060414, BA-2026-00009 | 12 Positionen, alle offen; Hinweis „4 Positionen bereits in BA-2026-00006, …“; keine Warnung „Keine Position angehakt“ | OK |
| 162.3 Summiert ohne Haken | AKE HauptFA 2625004, BA-2026-00003 | 4 Positionen offen; Hinweis „1 Positionen bereits in BA-2026-00002 vergeben …“ | OK |
| 162.24 Sperre | IDEAL BA-9 | Entfernen an: Verpackt `disabled`; Entfernen aus: frei; erneut an + Speichern: Position entfernt (DB), als Kandidat wieder frei | OK |
| 162.22 Abbrechen | IDEAL BA-9 | Fenster „8 Positionen“; Abbrechen: Status 10, 11 Positionen unverändert (DB) | OK |
| 162.23 Haken im selben Klick | IDEAL BA-9 | 1 Haken gesetzt, Übergeben: Fenster nennt 7 | OK |
| 162.21 entfernen und übergeben | IDEAL BA-9 | Status 30, nur die 4 verpackten verbleiben, entfernte im Vorschlag frei | OK |
| 162.7 Übergeben bearbeiten | IDEAL BA-9 | Haken gelöst, Menge 1→2, Kandidat 1067200 hinzugefügt: gespeichert, Audit gesetzt; verpackte Zeilen „nicht entfernbar“, offene mit Checkbox; offene Position entfernt | OK |
| 162.7 Negativ-POST `entfernen` verpackt | – | Entwicklertools nicht verfügbar; Server-Regel automatisiert belegt | NICHT MOEGLICH (Browser) |
| 162.8 Versendet mit offenen | IDEAL BA-9 | abgelehnt „Versendet nur, wenn alle Positionen verpackt sind — nicht verpackt (2): …“, Status bleibt 30; nach Haken + Entfernen Versendet gesetzt | OK |
| 162.23 ohne Dienstleister | AKE BA-2026-00001 | kein Fenster, „Dienstleister fehlt …“, 5 Positionen unverändert | OK |
| 162.22 beibehalten und übergeben | AKE BA-2026-00001 | Status 30 mit 5 offenen Positionen, Kreditor QA-LACK1 | OK |
| 162.23 alle verpackt | AKE BA-2026-00003 | kein Fenster, sofort Status 30 | OK |
| 162.25 Negativ-POST Verpackt+Entfernen | – | Entwicklertools nicht verfügbar; automatisiert (Theory) belegt | NICHT MOEGLICH (Browser) |
| 162.25 Terminal/Tastatur, ohne Farbe | – | ohne Screenshot nicht sichtbar prüfbar; `disabled` im Baum belegt | NICHT MOEGLICH |

DEV-Testdaten angelegt/geändert: IDEAL BA-2026-00009 (Versendet), AKE BA-2026-00001 (Übergeben, offen), AKE BA-2026-00003 (Übergeben).

### 7. Publish
`C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` neu (Zielordner vorher geleert), beide `dotnet publish` Exit 0,
`SQL\` und `deploy\` kopiert, `publish-info.txt` Version 1.99.65 / Commit `8ffa4c4ab51d7dce0681304eea64ac7a37c81997`.

**Ergebnis: Testbereit** (kein FEHLER; Befunde B-1/B-2 niedrig und offen für den Menschen).

## Manuelle Testcheckliste (Mensch, Schranke 2)

1. 162.7 Negativ: per Entwicklertools in Übergeben `entfernen` mit der Id einer verpackten Position senden: Meldung „… ist verpackt …“, nichts geändert.
2. 162.25: POST mit gleicher Id in `verpackt` und `entfernen` (Entwurf und Übergeben): abgelehnt mit Positionsnennung.
3. 162.25: Terminal / nur Tastatur: gesperrte Verpackt-Checkbox wird übersprungen und ist ohne Farbe als gesperrt erkennbar.
4. AK 12: Reklamations-Entwurf: Verpackt/Entfernen, Rückfrage beim Übergeben, Entfernen in Übergeben nur offen.
5. Sichtprüfung Übergeben-Fenster (Kontrast, Tastatur: Esc, Tab-Reihenfolge) an beiden Standorten.
6. Entscheid B-1: Positionsbezeichnung um VaterFA ergänzen? (IDEAL zeigt doppelte Einträge.)
7. Entscheid F1/B-3: „entfernen und übergeben“ bei lauter offenen Positionen erzeugt Übergeben mit 0 Positionen; so lassen?
8. Testsystem-Lauf mit dem Paket `publish\` (v1.99.65) an AKE und IDEAL, danach Merge.

## FREIGABE-NACHTRAG 2026-11-25 (Gerald, nach QA) – Umsetzung als v1.99.67

Antworten Gerald 2026-11-25 auf die QA-Befunde:
- **B-1 Positionsbezeichnung:** „ja, wäre schon nett“ → `PositionBezeichnung` nennt zusätzlich die VaterFA (wenn gesetzt), damit
  Meldungen und Übergeben-Fenster an IDEAL eindeutig sind (z. B. `50002326 (Pos. 1, VaterFA 4711)`).
- **F1 Übergeben mit 0 Positionen:** „macht keinen Sinn, dann wäre eher den Vorschlag löschen“ → Übergeben, das nach dem Entfernen
  0 Positionen übrig ließe, lehnt der Server ab. Sind **alle** Positionen offen, bietet das Fenster statt „entfernen und übergeben“
  den Weg „Entwurf stornieren“ an (vorhandener Übergang Entwurf → Storniert; ein Löschen von Aufträgen gibt es nicht).
- B-2 (Knopf bleibt nach Browser-Validierung gesperrt) wird im selben Zug mitbehoben (Knopf erst nach bestandener Prüfung sperren).

## QA Nachtrag v1.99.67 (qa-agent, 2026-11-25)

Geprüft: Commit `fcc894c6` (`fcc894c6^..fcc894c6`, v1.99.67) im Worktree `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5`,
HEAD `fcc894c64ad876374dda25f361be57258406a4ae`. Maßstab: FREIGABE-NACHTRAG 2026-11-25 (B-1, F1, B-2) plus übernommener Leerzeichen-Fix
`Views/SageBookingQueue/Index.cshtml`. Status bleibt **Testbereit**.

### 1. Build und Tests
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 30 Warnungen (alt, nicht aus diesem Diff).
- `dotnet test`: `IdealAkeWms.Tests` 3530 bestanden / 1 übersprungen / 0 Fehler (3531); `IDEALAKEWMSService.Tests` 790 / 0 / 0.

### 2. Code-Review (selbst, synchron)
Gelesen: Diff vollständig, dazu `BeschichtungsauftragWorkflow.BearbeitenAsync` / `StatusWechselnAsync` / `PositionBezeichnung` (alle 3 Aufrufer
im Workflow + 3 Stellen in `Details.cshtml`), Controller `Bearbeiten` (Speichern, dann Statuswechsel), Statusknöpfe und `storno-form` in
`Details.cshtml` (Storno-Knopf mit `onclick=confirm`), `beschichtung-bemerkung.js` komplett (Capture-Handler, Sperre, Fenster, `nichtsBliebe`,
`checkValidity`). Fehlerklassen: keine Werte in `<script>` (Bezeichnung weiter über Razor-kodiertes `data-bezeichnung` + `textContent`);
`hidden` nur an Elementen ohne `d-*` (`btn`, `p.small`; Bootstrap-Reboot `[hidden]{display:none!important}`); kein stiller Fallback
(`hinzu?.Count ?? 0` zählt eine fehlende Liste als leer, korrekt); keine neue Parallel-Array-Bindung; F1 erzwingt der Server an beiden Wegen
(Bearbeiten mit `ziel`, Status), das Fenster ist Komfort; `requestSubmit()` auf `storno-form` löst das `onclick=confirm` des Storno-Knopfs
nicht aus (Fenster = Rückfrage, auf DEV bestätigt: kein Browser-Dialog).

| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| N-1 | mittel (Altbestand, nicht aus diesem Diff) | **Leerer Entwurf lässt sich nicht mehr bearbeiten.** Jede Absendung von `entwurf-form` aus einem Entwurf mit 0 Positionen endet in „Eingabe nicht gespeichert: ungültiger Wert in .“ (leerer ModelState-Schlüssel). DEV IDEAL BA-2026-00011: Kandidat angehakt + „Speichern“ → weiter 0 Positionen (DB); „Auf Übergeben setzen“ → dieselbe Meldung statt „Übergeben mit 0 Positionen …“. Vermutete Ursache (nicht per Debugger belegt): `Dictionary<int, string>? mengen` in `BeschichtungsauftraegeController.Bearbeiten` (seit `8d60cb10`/`b525cfba`) fällt ohne `mengen[..]`-Felder auf das leere Präfix zurück und versucht die übrigen Formularschlüssel (`ziel`, `rowVersion`, …) als int-Schlüssel zu binden. Folge: Der Hinweis der Ansicht „Keine Positionen — unten hinzufügen …“ führt ins Leere; Ausweg nur Stornieren. Sicherheit gewahrt (nichts gespeichert, Status bleibt Entwurf). Seit F1 relevanter, weil „Speichern allein darf den Entwurf leeren“ ausdrücklich erlaubt ist. InMemory-Tests rufen die Action direkt auf und sehen den Binder nicht. | offen, Entscheid Mensch: eigener kleiner Bugfix (z. B. `[Bind(Prefix = "mengen")]` am Parameter). Nicht mitgebaut (Umfang). |
| N-2 | Hinweis | Regel „bliebe nichts übrig“ steht zweimal: Server (`BearbeitenAsync`: verbleibend + `hinzu` + `frei` = 0) und Client (`nichtsBliebe`: offene = verbleibende, keine `schluessel`/`freiArtikelId`). Der Client wählt nur den angebotenen Knopf, der Server entscheidet; Drift wäre kosmetisch. | akzeptiert. |
| N-3 | Hinweis | Der Server zählt `hinzu` ohne `Distinct` und `frei`-Zeilen vor deren Prüfung mit; ungültige/vergebene Einträge lehnt dieselbe Prüfung („alles oder nichts“) ohnehin ab, ein doppelter Schlüssel ergibt mindestens eine Position. Kein Fehlerfall gefunden. | keine Änderung. |

### 3. Beweisart je Punkt des Nachtrags
| Punkt | Beweis |
|---|---|
| B-1 VaterFA in `PositionBezeichnung` | automatisiert (Theory `PositionBezeichnung_NenntVaterFaWennGesetzt`, 6 Fälle inkl. ohne FA / ohne VaterFA / PositionText); DEV IDEAL: Fenster, Aria-Labels (Haken, Entfernen, Menge) und Versendet-Meldung mit „(Pos. n, VaterFA x)“ OK; DEV AKE ohne VaterFA „87033912 (Pos. 5)“ OK |
| F1 Server Bearbeiten mit `ziel` | automatisiert (`Bearbeiten_MitZielUebergeben_AlleEntfernt_Abgelehnt_NichtsGespeichert`, `…AberNeueHinzu_Erlaubt`, `Bearbeiten_OhneZiel_AlleEntfernt_ErlaubtImEntwurf`, Controller `Bearbeiten_ZielUebergeben_AlleEntfernt_Warnung_NichtsGespeichert`) + DEV OK; echte Formular-Bindung nur DEV |
| F1 Server Status | automatisiert (`Uebergeben_OhnePositionen_Abgelehnt_BleibtEntwurf`); über die Oberfläche nicht erreichbar (leerer Entwurf scheitert vorher an N-1) |
| F1 Fenster „Entwurf stornieren“ | Browser-JS, Manual-UAT; DEV IDEAL OK |
| B-2 Knopf nach Validierungsabbruch frei | Browser-JS, Manual-UAT; DEV IDEAL OK (Meldungsblase selbst nicht sichtbar prüfbar) |
| Leerzeichen SageBookingQueue, Aria-Label Menge | kein Test; Aria-Label auf DEV gesehen, Leerzeichen per Diff |

### 4. Geänderte bestehende Tests
- `BeschichtungsauftragWorkflowTests` (3 Stellen: Versendet-Meldung, Entfernen verpackter in Übergeben, Verpackt+Entfernen): erwarteter Text
  „A2 (Pos. 2)“ → „A2 (Pos. 2, VaterFA 100)“ usw. — Verhalten absichtlich geändert (B-1). Kein Fall „sonstiges“.

### 5. Checkliste CLAUDE.md
Keine Migration, kein SQL, kein FreshInstall/AgentJobs. Audit: keine neuen Schreibwege (F1 lehnt vor dem Speichern ab; Storno über den
bestehenden Übergang, DEV: `ModifiedAt`/`ModifiedBy` gesetzt). Version 1.99.67 in beiden `AppVersion.cs`, Anwender-Changelog, Hilfe,
`docs/anwender/15-…` + `README.md`-Stand, `docs/TESTSZENARIEN.md` (162.26–162.29 + Fußzeile) und `testszenarien-index.md` nachgezogen.
ADR 0020: keine Einstellung. Manifest `nachlauf.psd1`: kein Eintrag nötig.

### 6. DEV-Deploy und Testlauf
`standort: beide`. `dev-deploy.ps1` (erhöhte Shell, Admin = True) aus dem Worktree: **AKE** Exit 0, „HTTP 200, Fusszeile v1.99.67“,
„Fertig: AKE v1.99.67 auf http://localhost:8081/“; **IDEAL** Exit 0, „HTTP 200, Fusszeile v1.99.67“, „Fertig: IDEAL v1.99.67 auf
http://localhost:8082/“. DEV-Absicherung vom Skript neu gesetzt. Screenshots liefen wieder ins Zeitlimit; geprüft über den
Barrierefreiheitsbaum (sichtbar/hidden, Texte) und `sqlcmd`.

| Schritt | Ort / Daten | Ergebnis | Status |
|---|---|---|---|
| 162.26 Bezeichnung mit VaterFA | IDEAL BA-6 (Übergeben), BA-10 | Aria-Labels „Position 50002313 (Pos. 1, VaterFA 1067190) verpackt/entfernen“, „Menge … (Pos. 1, VaterFA 1067190)“, ohne FA „50000005 (ohne FA)“; Versendet auf BA-6 abgelehnt „… nicht verpackt (2): 50002313 (Pos. 1, VaterFA 10…“, Status bleibt 30; Fensterliste BA-10 mit VaterFA je Zeile | OK |
| 162.26 gleiche Artnr+Pos. unter zwei VaterFA | – | auf DEV IDEAL keine zwei freien gleichen Artnr/Pos. mehr (die Dubletten 50002326 / 50000296 stecken in BA-9, Versendet); Format automatisiert belegt | NICHT MOEGLICH (Daten) |
| 162.26 AKE ohne VaterFA | AKE BA-2026-00004 | „87033912 (Pos. 5)“, „86006770 (Pos. 9.2)“ | OK |
| 162.27 alle offen → Fenster | IDEAL BA-10 (8 offen, ohne Haken angelegt) | Hinweis „Keine Position ist verpackt …“ sichtbar, „entfernen und übergeben“ hidden, „Entwurf stornieren“ sichtbar | OK |
| 162.27 eine verpackt | IDEAL BA-10, 1 Haken gesetzt | „entfernen und übergeben“ sichtbar, „Entwurf stornieren“ und Hinweis hidden; Haken gelöst → wieder Stornieren | OK |
| 162.27 Stornieren | IDEAL BA-10 | kein Browser-Dialog, Status 90, `StorniertAt`/`ModifiedBy` gesetzt; alle 8 Positionen in der Vorschlagsliste wieder frei | OK |
| 162.27 zweiter Lauf mit „Positionen hinzufügen“ | – | keine passende Datenlage (Kandidaten + alle offen) ohne weiteren Entwurf; Client-Zweig `schluessel`/`freiArtikelId` nur per Review | NICHT MOEGLICH (Daten) |
| 162.28 alle „Entfernen“ + Übergeben | IDEAL BA-10 | kein Fenster; Meldung „Übergeben mit 0 Positionen ist nicht möglich (BA-2026-00010) — nichts gespeichert …“; DB: Status 10, 8 Positionen, `ModifiedAt` NULL | OK |
| 162.28 Speichern allein leert | IDEAL BA-2026-00011 (1 Position) | Entfernen + Speichern → Positionen (0), Status Entwurf | OK |
| 162.28 Übergeben auf leerem Entwurf | IDEAL BA-11 | Meldung „Eingabe nicht gespeichert: ungültiger Wert in .“ statt der 0-Positionen-Meldung; Status bleibt 10 (DB) | FEHLER (N-1, Altbestand) |
| 162.28 Status-POST ohne JS | – | Entwicklertools nicht verfügbar; automatisiert belegt | NICHT MOEGLICH (Browser) |
| 162.29 Validierungsabbruch | IDEAL BA-10, Tag in „Start Beschichtung“ gelöscht | „beibehalten“: Fenster schließt, nichts gesendet (DB unverändert, `ModifiedAt` NULL), keine Entfernen-Haken; erneut geöffnet ist „beibehalten“ wieder klickbar (schließt erneut) | OK |
| 162.29 Meldungsblase am Feld | – | ohne Screenshot nicht sichtbar prüfbar | NICHT MOEGLICH |
| 162.29 gültig, einmal absenden | IDEAL BA-8, AKE BA-4 | je ein Statuswechsel | OK |
| 162.21 entfernen und übergeben (Regression) | AKE BA-4: Kreditor QA-LACK1, Pos. 5 verpackt | Fenster „1 Position: 86006770 (Pos. 9.2)“, entfernen → Status 30, nur die verpackte Position bleibt, `ModifiedBy` gesetzt | OK |
| 162.22 beibehalten (Regression) | IDEAL BA-8 (2 offen) | Status 30 mit 2 offenen Positionen | OK |
| 162.23 Haken im selben Klick (Regression) | IDEAL BA-10 | Fenster nennt 7 statt 8 | OK |
| 162.24 Sperre (Regression) | AKE BA-4 | Entfernen an → Verpackt `disabled`; Entfernen aus → frei | OK |

DEV-Testdaten angelegt/geändert: IDEAL BA-6 (Versendet-Versuch, Speichern ohne Änderung), BA-8 (→ Übergeben, 2 offen; „Start Beschichtung“
Tag auf **12.07.2026** überschrieben durch einen fehlgeschlagenen Validierungs-Testaufbau (vorheriger Tag nicht notiert)), BA-2026-00010 (angelegt, storniert),
BA-2026-00011 (angelegt, **leerer Entwurf** – wegen N-1 nur noch stornierbar); AKE BA-2026-00004 (angelegt, Übergeben mit 1 Position).

### 7. Publish
`C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-08-07-ideal-teile-1-5\publish\` neu: `IDEALAKEWMSWeb`, `IDEALAKEWMSWebService`, `SQL`, `deploy`
vorher geleert, beide `dotnet publish` Exit 0; `publish-info.txt`: Version 1.99.67, Commit `fcc894c64ad876374dda25f361be57258406a4ae`,
Branch `feature/2026-08-07-ideal-teile-1-5`, Erstellt 2026-11-25 14:37. Ältere `paket*.zip` im Ordner unverändert.

### Deploy (final für v1.99.67, aus dem Diff `fcc894c6^..fcc894c6`)
- **Web-App:** ja (`IdealAkeWms/`: Workflow, Controller, `Details.cshtml`, `beschichtung-bemerkung.js`, Hilfe, Changelog, SageBookingQueue-View).
- **Service:** nein (nur `IDEALAKEWMSService/AppVersion.cs`); das Paket enthält beide Komponenten (eine Version).
- **Migration:** nein; kein SQL, kein Einmal-Skript, kein Manifest-Eintrag.
- Reihenfolge Testsystem: `publish\` kopieren, `deploy\Update-IdealAkeWms.ps1` (Web; Dienst nur Versionsstand). Kein DB-Backup-Sonderfall.
- Nach dem Merge nur dann neu aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen `main`-Änderungen zusammengeführt hat
  (`dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`,
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`).

**Ergebnis Nachtrag: Testbereit.** Der Nachtrag erfüllt B-1, F1 und B-2; der einzige FEHLER (N-1) ist Altbestand und blockiert nicht,
gehört aber vor der Abnahme entschieden.

### Manuelle Testcheckliste Nachtrag (Mensch, Schranke 2)
1. Entscheid N-1: leerer Entwurf nicht mehr speicherbar („ungültiger Wert in .“) – eigener Bugfix vor oder nach dem Merge? (Repro: DEV IDEAL BA-2026-00011.)
2. 162.26 mit echten Dubletten (gleiche Artnr + Pos. unter zwei VaterFA, z. B. 50002326 an IDEAL): Fenster und Versendet-Meldung unterscheidbar.
3. 162.27 zweiter Lauf: alle offen + einen Kandidaten unter „Positionen hinzufügen“ anhaken → Fenster bietet „entfernen und übergeben“ statt Stornieren.
4. 162.28 per Entwicklertools: `Status` mit `ziel=Uebergeben` auf einem leeren Entwurf → Meldung „Übergeben mit 0 Positionen …“.
5. 162.29 Sichtprüfung: Browser-Meldung erscheint am Datumsfeld, nicht verdeckt.
6. Sichtprüfung Fenster (Kontrast roter Knopf „Entwurf stornieren“, Tastatur/Esc) an beiden Standorten.
7. DEV IDEAL BA-8: „Start Beschichtung“ steht durch den QA-Lauf auf 12.07.2026 (Tag überschrieben) – bei Bedarf korrigieren.
8. Testsystem-Lauf mit dem Paket `publish\` (v1.99.67) an AKE und IDEAL, danach Merge.

## FREIGABE-NACHTRAG 2 2026-11-25 (Gerald) – N-1 leerer Entwurf, Umsetzung als v1.99.69

QA-Befund N-1 (Altbestand): Ein Entwurf mit 0 Positionen lässt sich nicht absenden („Eingabe nicht gespeichert: ungültiger Wert
in .“), vermutet Model-Binding `Dictionary<int,string>? mengen` in `BeschichtungsauftraegeController.Bearbeiten`. Gerald 2026-11-25:
„ja – bitte gleich ausbessern“ → Root Cause belegen, beheben, Test, im Anschluss an die Referenz-Spec (v1.99.68) als v1.99.69.
