---
type: spec
title: "Klärung am Zielsystem: Lagerplatzbezeichnung in Artikelbestände, Menge IST in der Kommissionierung am AKE-Test"
slug: 2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-5-spec
status: Gemerged
created: 2026-11-23
updated: 2026-11-28
source_backlog: "[[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung]]"
uebersicht: "[[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-uebersicht]]"
depends_on: "[[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-4-spec]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
version_vorschlag: "1.99.58 (nur Punkt 5, Spalte Menge IST)"
testszenario_vorschlag: "TS-183 (Spalte Menge IST); Punkt 3 ohne Code"
affected_code:
  - "IdealAkeWms/Views/Picking/Bom.cshtml (neue Spalte ist-menge: <th>, <td>, #column-config; Eingabe aus der Menge-Zelle verschoben)"
  - "IdealAkeWms/Models/ViewModels/ColumnDefinitions.cs (ColumnDefinitions.Bom: Spalte ist-menge, Zweitregistrierung)"
  - "docs/TESTSZENARIEN.md (TS-183), Views/Help/Changelog.cshtml, beide AppVersion.cs"
open_questions:
  - "Prüfabfrage A (Artikelbestände): Auf welchem System und mit welcher Version wurde „keine Lagerplatzbezeichnung“ gesehen, und was liefern die WMS- und Sage-Abfragen unten dort?"
  - "(Fachbereich) Was ist mit „Lagerplatzbezeichnung“ gemeint: der Code (z. B. LL;1;4;0, steht fett in der Zelle) oder der Klartext darunter (Sage-Platzbezeichnung)?"
  - "Kommissionierung AKE-Test: Reicht die vorhandene Ist-Menge (seit v1.88.0 Eingabefeld unter der Soll-Menge in der Spalte „Menge“ des Kommissionierauftrags), sobald das Bündel auf :4444 läuft – oder ist eine eigene Spalte „Menge IST“ gewünscht?"
epic: false
etappen: []
deploy:
  web: true
  service: false
  migration: false
freigabe_entscheidung: "freigegeben"
freigabe_von: "Gerald"
freigabe_am: 2026-11-23
---

# Teil 5: Klärung – Artikelbestände (Punkt 3) und Menge IST (Punkt 5)

Übersicht: [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-uebersicht]] · Backlog-Punkte 3 und 5.

## Ziel / Nutzen (das Warum)

Beide Meldungen lassen sich am Bündel-Code nicht nachvollziehen: Die Anzeige ist da, die Eingabe ist da. Bevor jemand Code ändert, muss
feststehen, was am Zielsystem tatsächlich läuft und welche Daten dort liegen. Sonst wird ein Fehler „behoben“, den es im Code nicht gibt.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** Prüfschritte und Entscheidungsfragen; Festhalten des belegten Code-Stands.
**Out-of-Scope:** Jede Code-Änderung. Ergibt die Prüfung einen Befund, kommt er als Nachtrag in diese Spec (dann mit AK, Version, TS-183).

## Fachliche Anforderungen

### Punkt 3: Artikelbestände ohne Lagerplatzbezeichnung

**Belegt (Code im Bündel):**
- Die Zelle „Lagerplatz“ zeigt den Code fett und den Klartext `StorageLocationDescription` in einer zweiten Zeile, nur wenn nicht leer
  (`StockOverview/Index.cshtml:145-163`, Bedingung `:159`).
- Alle Lesepfade füllen `StorageLocationDescription` aus `StorageLocations.Description` (`StockMovementRepository.cs:51, 64, 111, 124, 148, 246, 282`).
- `Code` ist bei Sage-Plätzen die Sage-**Kurzbezeichnung** (`LagerplatzSyncService.cs:124, 140`), `Description` die Sage-**Platzbezeichnung**
  (`:126, 142`). Bei jedem Abgleich mit Unterschied wird `Description` überschrieben – leer in Sage = `NULL` im WMS (`:204, 213`). Ein im WMS
  gepflegter Klartext an einem Sage-Platz hält also nur bis zum nächsten Lauf.
- Beide Lesewege liefern die Platzbezeichnung: View `SageLagerplatzReader.cs:24, 43` (`KHKLagerplaetze.Platzbezeichnung`), SData
  `SdataLagerplatzReader.cs:24, 67` (`$select` enthält `Platzbezeichnung`). Schalter: ServiceSetting `Sync:Quelle:Lagerplaetze`
  (`SageQuelleSchalter.cs:16-19`, Default View).
- **Korrektur zum Backlog:** `LagerplatzQuelleSwitch.cs:16` ist nur ein Vergleichsfeld für die Betriebsart „Vergleich“, kein Beleg, dass SData
  die Platzbezeichnung leer liefert. Ob SData sie füllt, ist ein Sage-Fakt (Prüfabfrage A3).
- DEV-Zahlen laut Backlog (AKE_WMS_DEV 3298 aktive Plätze, 2 ohne Bezeichnung; IDEAL_WMS_DEV 231, alle mit) – hier nicht nachgemessen.

**Unbelegt:** Zustand am System der Meldung. Hypothesen, in dieser Reihenfolge zu prüfen: (1) dort läuft eine Version, die die Zeile noch nicht
zeigt; (2) Sage hat die Platzbezeichnung leer und der Abgleich hat sie genullt; (3) gemeint ist etwas anderes als der Klartext (Rückfrage 2).

### Punkt 5: Kommissionierung am AKE-Test ohne Mengeneingabe

**Belegt (Code im Bündel):**
- Seit v1.88.0 wird je Kommissionierauftrag kommissioniert ([[2026-11-08-v1-88-0-kommissionierauftraege]]; ADR
  [[0021-kommissionierauftrag-als-beleg-neben-der-stueckliste]]). Die FA-Stückliste ist nur lesend (`Picking/Bom.cshtml:60`).
- Auf der Auftragsseite steht unter der Soll-Menge ein Eingabefeld Ist-Menge (`Picking/Bom.cshtml:280-288`: `type="text"
  inputmode="decimal"`, Vorgabe = Soll, Server prüft > 0 und ≤ Soll). Ohne Auftrag zeigt die Zelle nur die Menge bzw. „Ist …“ (`:289-292`).
- Laut [[2026-11-12-deploy-fahrplan-buendel-v1-99]] Z. 69 läuft auf `akenet01:4444` v1.30.x – vor v1.88.0. Damit erklärt sich die Meldung
  ohne Code-Befund. **Nicht belegt:** welche Version heute tatsächlich auf :4444 läuft (Prüfschritt B).
- Der Vorschlag „eigene Spalte Menge IST“ wäre eine neue Spalte `ist-menge` in der Stückliste (drei Quellen: `<th data-col-key>`, `#column-config`,
  `ColumnDefinitions.Bom`; [[fallstricke]] Bom-Zweitregistrierung) – nur nach Rückfrage 3.

## Technischer Loesungsentwurf

Kein Code. Ablauf:
1. Gerald führt die Prüfabfragen aus und beantwortet die Rückfragen.
2. Ergebnis „Version zu alt“ (A1/B1) → nichts bauen; die Punkte sind mit dem Bündel-Deploy erledigt. Spec auf `status: Verworfen` oder als
   erledigt vermerken (Mensch).
3. Ergebnis „Sage leer“ (A2/A3) → Datenpflege in Sage; optional Nachtrag „Klartext-Rückfall“ (Entscheidung Gerald).
4. Ergebnis „eigene Spalte gewünscht“ → Nachtrag mit Spalte `ist-menge`, AK, TS-183.

## Prüfabfragen (Mensch, nur SELECT)

**A – Artikelbestände (auf dem System der Meldung):**
- A1: Version unten in der Fußzeile bzw. `/Help/Changelog` ablesen.
- A2 (WMS-DB):
  ```sql
  SELECT COUNT(*) AS Aktiv,
         SUM(CASE WHEN ISNULL(Description,'') = '' THEN 1 ELSE 0 END) AS OhneBezeichnung,
         SUM(CASE WHEN ISNULL(Description,'') = '' AND Source = 'Sage' THEN 1 ELSE 0 END) AS OhneBezeichnungSage,
         MAX(ModifiedAt) AS LetzteAenderung
  FROM StorageLocations WHERE IsActive = 1;
  SELECT [Key], [Value] FROM ServiceSettings WHERE [Key] = 'Sync:Quelle:Lagerplaetze';
  ```
- A3 (Sage-DB des Mandanten, gleiche Filter wie `SageLagerplatzReader.cs:23-29`):
  ```sql
  SELECT COUNT(*) AS Plaetze,
         SUM(CASE WHEN ISNULL(lp.Platzbezeichnung,'') = '' THEN 1 ELSE 0 END) AS OhnePlatzbezeichnung
  FROM KHKLagerorte lo JOIN KHKLagerplaetze lp ON lo.Lagerkennung = lp.Lagerkennung
  WHERE lo.Mandant = 1 AND lo.Aktiv = -1;
  ```
- A4: Steht `Sync:Quelle:Lagerplaetze` auf SData oder Vergleich, im Aktivitäts-Protokoll (Dienst „Lagerplatz“ bzw. Quellenvergleich) nach
  Abweichungen im Feld `Description` suchen.

**B – Kommissionierung AKE-Test:**
- B1: Version auf `https://akenet01.ake.at:4444/` ablesen.
- B2: Einen Kommissionierauftrag öffnen (`/Picking/Auftrag/<Id>`), nicht die FA-Stückliste – erst dort gibt es die Ist-Menge.

## Migrations-/SQL-Auswirkungen

Keine.

## Einstellungen

keine

## Audit-Feld-Auswirkungen

Keine.

## Akzeptanzkriterien

1. Für Punkt 3 liegt das Ergebnis von A1–A3 (und A4, falls SData/Vergleich) in dieser Spec, mit Entscheidung „nichts / Datenpflege / Nachtrag“.
2. Für Punkt 5 liegt die Version auf :4444 vor; nach Bündel-Deploy ist die Ist-Menge auf der Auftragsseite eingebbar (Abnahme über TS-120).
3. Ohne Code-Befund wird kein Code geändert.

## Test-Szenarien

Kein eigenes Kapitel, solange kein Code entsteht. Nach dem Bündel-Deploy auf :4444: TS-120 (Kommissionierauftrag, Ist-Menge) ausführen.
Bei einem Nachtrag mit Code: Kapitel **TS-183**.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt (kein Epic) | – | – |

## Deploy

*Vorläufig; durch den Nachtrag „Menge IST“ überholt – QA-bestätigt siehe QA-Nachweis unten: Web ja, Dienst nur Versionsnummer, keine Migration, kein Manifest.*
- **Web-App:** ja (Nachtrag, `Picking/Bom.cshtml`, `ColumnDefinitions.Bom`)
- **Service:** nein (nur Versionsnummer)
- **Migration:** nein
- **Manifest-Einträge:** keine
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Offene Rueckfragen

1. Prüfabfrage A (Artikelbestände): Auf welchem System und mit welcher Version wurde „keine Lagerplatzbezeichnung“ gesehen, und was liefern
   die WMS- und Sage-Abfragen A2/A3 (ggf. A4) dort?
2. (Fachbereich) Was ist mit „Lagerplatzbezeichnung“ gemeint: der Code (z. B. `LL;1;4;0`, steht fett in der Zelle) oder der Klartext darunter
   (Sage-Platzbezeichnung)?
3. Kommissionierung AKE-Test: Reicht die vorhandene Ist-Menge (seit v1.88.0 Eingabefeld unter der Soll-Menge in der Spalte „Menge“ des
   Kommissionierauftrags), sobald das Bündel auf :4444 läuft – oder ist eine eigene Spalte „Menge IST“ gewünscht? Empfehlung: vorhandenes
   Feld, erst nach dem Deploy erneut bewerten.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. → Bestätigt: In „Artikelbestände“ fehlt die Bezeichnung tatsächlich. Vermutung Gerald: die Stammdaten (Sage-Platzbezeichnung) sind dort anders/leer. Kein Code; Prüfabfragen A2/A3 bei Gelegenheit, Datenpflege in Sage.
2. → Der Klartext (Platzbezeichnung) unter dem Code.
3. → Eigene Spalte „Menge IST“ gewünscht – siehe Nachtrag unten.

## Nachtrag 2026-11-23 nach Freigabe: Spalte „Menge IST“ (Punkt 5)

Entscheidung Gerald (Antwort 3): Die Ist-Menge bekommt eine **eigene Spalte**, auch wenn das Feld seit v1.88.0 schon unter der Soll-Menge steht.
Punkt 3 bleibt ohne Code (Antwort 1: Stammdaten am Zielsystem).

**Umsetzung (minimal, nur `Picking/Bom.cshtml` + Registrierung):**
- Neue Spalte `data-col-key="ist-menge"`, Kopf „Menge IST“, `text-end`, direkt rechts neben „Menge“. Gerendert nur, wenn `Model.Auftrag != null`
  (Auftragsseite); die FA-Stückliste ohne Auftrag bleibt unverändert.
- Zelleninhalt: das bestehende Eingabefeld (`Bom.cshtml:283-287`, Klasse `ist-menge`, `inputmode="decimal"`, Vorgabe = Soll) wandert unverändert
  in diese Zelle. Bei übertragenen bzw. nicht bearbeitbaren Zeilen steht dort die gebuchte Ist-Menge (`N3`) oder leer.
- Die Zelle „Menge“ zeigt nur noch die Soll-Menge; der Zweig „Ist …“ (`:289-292`) entfällt auf der Auftragsseite (steht jetzt in der Spalte).
- Registrierung an allen Pflichtstellen: `<th data-col-key>`, `#column-config` (`Bom.cshtml:726`), `ColumnDefinitions.Bom` ([[fallstricke]]
  Bom-Zweitregistrierung). Kein Spaltenfilter auf der Eingabespalte.
- Das Absenden liest `row.querySelector('.ist-menge')` (`Bom.cshtml:1584`) – bleibt gültig, da die Klasse bleibt. Serverprüfung (> 0, ≤ Soll) unverändert.
- Teil 1 (Pfeil/Mausrad) betrifft das Feld nicht, es ist `type="text"`.

**Akzeptanzkriterien Nachtrag:**
1. Auf `/Picking/Auftrag/<Id>` gibt es die Spalte „Menge IST“ mit Eingabefeld je offener Zeile, Vorgabe = Soll.
2. Abschließen/Übertragen bucht die eingegebene Ist-Menge wie bisher; 0, negativ oder > Soll wird weiterhin abgewiesen.
3. Spalte lässt sich über das Zahnrad aus-/einblenden und verschieben; die FA-Stückliste ohne Auftrag zeigt keine Spalte „Menge IST“.
4. Drift-Guard-Tests (Spaltenregistrierung) grün.

**TS-183:** Auftrag öffnen → Spalte sichtbar, Wert ändern (z. B. 2,5 bei Soll 3) → übertragen → Ist 2,5 gebucht; Negativfälle 0 / 4 / Text;
Spalte ausblenden + Reload; FA-Stückliste ohne Auftrag ohne Spalte; am AKE-Test erst nach Bündel-Deploy (:4444 hat v1.30.x).

## QA-Nachweis 2026-11-23 (qa-agent)

**Gemeinsamer Nachweis (QA-Lauf 2026-11-23 abends, qa-agent, Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Diff `1cfb92d7..b79a4b1a`):**
- **Build:** `dotnet build IdealAkeWms.slnx` 0 Fehler (30 Warnungen, vorbestehend).
- **Tests:** `IdealAkeWms.Tests` 3480 bestanden / 1 übersprungen / 0 Fehler; `IDEALAKEWMSService.Tests` 790 / 0 / 0. Nach dem QA-Fix `b79a4b1a` Web erneut 3480/1/0.
- **DEV-Deploy** (`D:\Dev\scripts\dev-deploy.ps1`, Quelle Worktree, Web + Dienst, erhöhte Shell): AKE und IDEAL je zweimal (vor und nach QA-Fix), Exit 0,
  „HTTP 200, Fusszeile v1.99.58“, „Fertig: AKE v1.99.58 auf http://localhost:8081/“ bzw. „Fertig: IDEAL v1.99.58 auf http://localhost:8082/“. Footer = AppVersion 1.99.58.
- **Publish:** `<worktree>\publish\IDEALAKEWMSWeb` + `IDEALAKEWMSWebService` (Ordner vorher geleert, beide Exit 0), `SQL\` → `publish\SQL`, `scripts\deploy\` → `publish\deploy`,
  `publish\publish-info.txt` Version 1.99.58, Commit `b79a4b1a0fef42d220265ad5d81983816dce50aa`. Paket-ZIP/Shares: Sache des Orchestrators (DoD).
- **QA-Fix `b79a4b1a`** (`qa(...teil-5)`): Leerzeilen-`colspan` der Stückliste zählt die neue Spalte „Menge IST“ mit (`Picking/Bom.cshtml`, `emptyColspan` + 1 bei `Model.Auftrag != null`).
- **Geänderte bestehende Tests:** `WarehousePickingVerbrauchsbuchungTests` (Zeile 227: `vm.EntnahmePlaetze` → `vm.Items[0].EntnahmeOptionen`, Typ-/Ortswechsel durch Nachbesserung `b7e2d5a4`, Aussage gleich);
  `ViewKeyDriftGuardTests.ViewKey_Registriert` (verschärft: `supportsReorder` Gleichlauf, Teil 2 AK 6); `ViewKeyDriftGuardTests.WareneingangPositionen_*` `(false,false)`→`(true,false)` (Verhalten absichtlich geändert, Teil 2 Antwort 1). Kein „Sonstiges“.
- **Checkliste CLAUDE.md:** keine Migration/kein SQL/kein FreshInstall/keine Einstellung (ADR 0020 entfällt), keine Audit-Änderung; Version 1.99.54–1.99.58 in beiden `AppVersion.cs` (Dienst nur Nummer), Anwender-Changelog je Version vorhanden;
  TS-178.12, TS-179–183 in `docs/TESTSZENARIEN.md` und im Testindex. **Offen beim Orchestrator:** Brain-Changelog `secondbrain/changelog/` und `feature-map.md` für 1.99.54–1.99.58 fehlen noch; Doku-Nachzug `docs/anwender`/`docs/betrieb` (DoD). Kein Manifest-Eintrag nötig (kein Deploy-Schritt).

**Code-Review (qa-agent, selbst, Diff `1cfb92d7..HEAD`, 24 Dateien):** Befunde gesamt: (B1) `Bom.cshtml` Leerzeilen-`colspan` ohne neue Spalte → behoben `b79a4b1a`.
(H1, Hinweis, nicht behoben) `site.js` Teil 1: Bekommt ein Zahlenfeld `pointerdown`, aber nie den Fokus (z. B. `disabled`), bleibt `step="any"` bis zum nächsten Fokus/Blur – nur Browser-Schrittprüfung betroffen, Server prüft ohnehin; kein Handlungsbedarf.
(H2, Hinweis) Nachbesserung `EntnahmeOptionen`: Rückfall „volle Liste“ greift nur, wenn weder Bestand noch NAN wählbar – dann bleibt die frühere Laufzeit (seltener Fall, bewusst).
Geprüfte Fehlerklassen: kein `@Html.Raw` in `<script>` (neue `#column-config`-Zeile ist statisches JSON in `<text>`); kein `hidden` mit `d-*`; keine neuen stillen Fallbacks (`r.Auftrag?.RowVersion ?? rv` führt höchstens zu „veraltet“-Warnung, nicht still);
Parallel-Arrays: Receive nutzt indizierte Namen `zeilen[i].*`, Umsortieren der Spalten ändert keine Bindung; Spaltenzugriffe in `Bom.cshtml`-JS über `bomColKeyToIndex` (dynamisch), nicht über feste Indizes; `column-preferences.js` ignoriert gespeicherte Schlüssel ohne DOM-Spalte (Stückliste ohne Auftrag);
Serverregel Statuswechsel bleibt in `BeschichtungsauftragWorkflow.StatusWechselnAsync` (eine Stelle), Ist-Menge-Prüfung weiter nur serverseitig in `PickingTransferService`.

**Deploy (QA-bestätigt am echten Diff):** Web ja (`IdealAkeWms/` geändert), Dienst: nur Versionsnummer (`IDEALAKEWMSService/AppVersion.cs`) – fachlich unverändert, mitliefern für gleiche Version; Migration nein; Manifest nein.
Paket: `<worktree>\publish` v1.99.58 (Web + Dienst). Ablauf Mensch: Paket auf Testsystem → testen → Merge. Nach dem Merge nur dann aus `main` neu publizieren, wenn der Merge geprüfte Dateien mit parallelen main-Änderungen zusammengeführt hat:
`dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` und `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`.

**Beweisart je AK (Nachtrag):** Registrierung an drei Stellen + genau ein Eingabefeld automatisiert (`ViewKeyDriftGuardTests.Stueckliste_SpalteMengeIst_DreiQuellen_NurMitAuftrag`). Umbuchen mit Ist-Menge, Zahnrad, CSV: Manual-UAT. Punkt 3 (Lagerplatzbezeichnung): kein Code, Datenpflege (Antwort 1).

**Review-Prüfung Spaltenvarianten:** `<th>` (Z. 190) / `<td>` (Z. 286) / `#column-config` (Z. 744) alle unter `Model.Auftrag != null`, direkt nach `quantity`; Baugruppen- und Artikelzeilen nutzen dieselbe `<td>`-Folge (eine Zeilenvorlage). Leerzeile (`colspan`) zählte die Spalte nicht → **QA-Fix `b79a4b1a`**.
Absenden liest `row.querySelector('.ist-menge')` (zeilenbezogen, unabhängig von der Spaltenposition) → Umsortieren unkritisch. CSV: `data-csv-value` = gebuchte Ist-Menge (N3) bzw. ohne Attribut leer. Lese-Ansicht zeigt jetzt jede gebuchte Ist-Menge (vorher nur bei Abweichung) – im Sinn des Nachtrags.

**DEV-Testlauf TS-183 (DEV AKE :8081):**
| Nr | gemacht | Ergebnis | Urteil |
|---|---|---|---|
| 183.1 | `/Picking/Auftrag/49` (KO-2026-00016, offen) und `/Picking/Auftrag/112` (teilweise abgeschlossen, nur lesend) | Spalte „Menge IST“ rechts neben „Menge“, Eingabefeld je offener Zeile mit Vorgabe Soll (1); „Menge“ nur Soll; Filterzeile ohne Feld unter „Menge IST“, Spalten bündig; lesend: Spalte ohne Eingabe | OK |
| 183.2 | Auftrag 49 Pos. 630: Ist `0,5`, Zeile angehakt, Ziel QA173-W, „Gepickte Artikel umbuchen“ | Server `POST /Picking/AuftragUmbuchen` → **HTTP 400** (Log 18:01:26, 8,7 ms, direkt nach Negotiate-Neuanmeldung); Meldetext steht im Browser-`alert`, der die Chrome-Steuerung blockiert – nicht lesbar; DB unverändert (keine Bewegung, `IstMenge` NULL) | **OFFEN/UNKLAR** – Ursache nicht belegt. Diff-Bezug unwahrscheinlich (`.ist-menge` wird zeilenbezogen gelesen, Feld/Wert unverändert), Verdacht: Antiforgery/Sitzung nach zwei Redeploys (Log: „key … was not found in the key ring“). Mensch: Dialog lesen und 183.2 wiederholen. |
| 183.3–183.5 | nicht ausgeführt (Browser blockiert; CSV = Download) | – | offen (Mensch) |

**Manuelle Checkliste (offen, Reihenfolge):**
1. Im Chrome-Tab „Stückliste - FA 2621027“ (DEV AKE) den offenen Fehlerdialog lesen/schließen, Seite neu laden, **TS-183.2 wiederholen** (Ist 0,5 → umbuchen → „Menge IST“ zeigt 0,500). Schlägt es erneut fehl: Meldetext notieren – dann Befund für Teil 5 bzw. TS-120.
2. TS-183.3 Negativ 0 / 4 / `abc`.
3. TS-183.4 Zahnrad: „Menge IST“ ausblenden/einblenden, verschieben, Eingabe an neuer Stelle.
4. TS-183.5 FA-Stückliste ohne Auftrag ohne Spalte; CSV der Auftragsseite mit Spalte „Menge IST“.
5. Durchlauf am Testsystem (TS-120).
