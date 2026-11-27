---
type: spec
title: "Lagerbestellung: Entnahme-Vorschlag „überall NAN“ (Nachbesserung zu v1.99.53)"
slug: 2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-4-spec
status: Testbereit
created: 2026-11-23
updated: 2026-11-23
source_backlog: "[[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung]]"
uebersicht: "[[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-uebersicht]]"
nachbesserung_zu: "[[2026-11-23-lagerbestellung-verbrauchsbuchung-bei-lieferung-spec]]"
depends_on: "[[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-teil-3-spec]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
version_vorschlag: "1.99.57 (Dev-Lauf prüft)"
testszenario_vorschlag: "TS-182 (Dev-Lauf prüft; Verweis aus TS-178)"
affected_code:
  - IdealAkeWms/Controllers/WarehousePickingController.cs (VorschlagHinweis :164-170; bei Variante a/b EntnahmeErlaubt :160-161 bzw. Details :188-212)
  - IdealAkeWms/Views/WarehousePicking/Details.cshtml (nur bei Variante a/b)
  - IdealAkeWms.Tests (WarehousePicking-Controller-Tests VorschlagHinweis/EntnahmeErlaubt)
  - docs/TESTSZENARIEN.md (TS-182), Views/Help (Lagerbestellung), Views/Help/Changelog.cshtml, beide AppVersion.cs
  - "Bug-Datensatz: [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-bug]] (Symptom 3, Hinweistext)"
open_questions:
  - "Variante a, b oder c (Antwort 10 der v1.99.53-Spec lockern oder nicht)? Empfehlung c mit korrigiertem Hinweis."
  - "Prüfabfrage: Wie steht SageLagerbuchungAktiv auf dem System der Meldung und auf Produktion (AKE, IDEAL), und wie viele Sage-Plätze haben „Sage-Buchung erlaubt“? Ist das Einschalten für Produktion geplant (wann)?"
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

# Teil 4: Lagerbestellung – „überall NAN“

Übersicht: [[2026-11-23-mengenfelder-tabellen-beschichtung-kommissionierung-uebersicht]] · Backlog-Punkt 6 (nachgereicht).
Nachbesserung zu [[2026-11-23-lagerbestellung-verbrauchsbuchung-bei-lieferung-spec]] (v1.99.53, Testbereit, nicht gemerged). Diese Spec
ändert die v1.99.53-Spec nicht; sie fasst eine dort beantwortete Frage (Antwort 10) wieder an.

## Ziel / Nutzen (das Warum)

Der Lagerist sieht in der Lagerbestellung, wo Bestand liegt, bekommt aber überall „NAN“ als Entnahme-Platz vorgeschlagen und dazu
„Kein Bestand – Vorschlag NAN“. Das wirkt wie ein Fehler und verleitet dazu, von NAN zu buchen, obwohl Ware auf einem echten Platz liegt.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** Vorschlag und Hinweis der Entnahme-Platz-Auswahl auf der Lagerbestellung (`WarehousePicking/Details`), je nach Variante die
Auswahlregel `EntnahmeErlaubt`.
**Out-of-Scope:** Vorschlagsregel der Kommissionierung (`QuellLagerplatzVorschlag` selbst, nutzt dort `IstBuchbar`), Sage-Lagerbuchung,
Lagerbestand-Abgleich, das Setzen von `SageLagerbuchungAktiv` oder `SageBuchungErlaubt` (Betrieb, nicht Code).

## Fachliche Anforderungen

**Belegt (Ursache, Backlog-Befund bestätigt):**
- Vorschlag = `QuellLagerplatzVorschlag.Waehle(bestand, erlaubteIds, nanId)` (`WarehousePickingController.cs:206`): Hauptlagerplatz mit
  Bestand **aus der erlaubten Menge**, sonst erlaubter Platz mit dem meisten Bestand, sonst NAN (`QuellLagerplatzVorschlag.cs:11-17`).
- Erlaubt = `EntnahmePlaetzeAsync` (`:152-157`) → `EntnahmeErlaubt` (`:160-161`): WMS-Platz immer; Sage-Platz nur, wenn ServiceSetting
  `SageLagerbuchungAktiv` = `"true"` **und** `SageWareneingang.IstSageWePlatz(l)` (`SageWareneingang.cs:39-40`: `SageBuchungErlaubt` und
  `SageLagerkennung` und `SageLagerplatzId` gesetzt).
- Ist der Schalter aus, fällt jeder Sage-Platz heraus; bei an fallen alle Sage-Plätze ohne „Sage-Buchung erlaubt“ heraus. Das ist Antwort 10
  der v1.99.53-Spec („Sage-Plätze nur mit Sage-Übergabe zulassen/vorschlagen? → ja“, dort Z. 456, 485). Auf DEV steht der Schalter aus
  (dort Z. 504). Die Zählung der Sage-Plätze auf DEV stammt aus dem Backlog (3284/14 AKE, 228/3 IDEAL) und ist hier nicht nachgemessen.
- **Falscher Hinweis (Zusatzbefund):** `VorschlagHinweis` (`:164-170`) sucht den Vorschlag im Bestand; NAN hat keinen Bestand → Text
  „Kein Bestand – Vorschlag NAN“ (`:168`), auch wenn der Artikel auf ausgeschlossenen Sage-Plätzen Bestand hat. Die Lagerplatz-Spalte derselben
  Zeile zeigt diesen Bestand (`StockLocationInfo.LagerplatzText(bestand)`, `:211`). Die Seite widerspricht sich also selbst.

**Varianten (Entscheidung Gerald):**

| | Variante | Wirkung | Risiko |
|---|---|---|---|
| a | Antwort 10 lockern: bei Schalter aus Sage-Plätze vorschlagen und wählbar, sichtbarer Hinweis | Entnahme vom echten Platz | Der Lagerbestand-Abgleich bucht die WMS-Entnahme auf Sage-Plätzen zurück (Grund für Antwort 10) – Bestand im WMS springt wieder hoch, Verbrauch fehlt in Sage |
| b | Sage-Plätze mit Bestand nur anzeigen/vorschlagen, Buchen bleibt gesperrt | Lagerist sieht den richtigen Platz | Vorschlag, den man nicht buchen darf, ist verwirrend; Buchung landet trotzdem auf NAN oder WMS-Platz |
| **c** | So lassen, bis `SageLagerbuchungAktiv` produktiv an ist – **plus** ehrlicher Hinweis | Regel bleibt sauber, Widerspruch verschwindet | NAN bleibt Vorschlag, solange der Schalter aus ist |

**Empfehlung: c mit korrigiertem Hinweis.** Antwort 10 schützt vor einer stillen Rückbuchung; das gilt weiter. Der Hinweis wird wahr:
- Liegt Bestand nur auf ausgeschlossenen Plätzen: „Bestand nur auf Sage-Plätzen ohne Sage-Buchung (z. B. LL;1;4;0: 12) – Vorschlag NAN.
  Sage-Lagerbuchung ist aus bzw. am Platz nicht erlaubt.“ (höchstens drei Plätze nennen, Rest „+n“).
- Kein Bestand irgendwo: wie heute „Kein Bestand – Vorschlag NAN“.

Bei Variante a oder b gilt der Hinweis-Text sinngemäß mit (a: „Entnahme wird vom Lagerbestand-Abgleich zurückgebucht“).

## Technischer Loesungsentwurf

Variante c: nur `VorschlagHinweis` bekommt die Menge der erlaubten Ids (liegt in `Details` als `erlaubteIds` vor, `:191`) und unterscheidet
„Bestand außerhalb der erlaubten Plätze“ von „kein Bestand“. Reine Funktion → Unit-Test. Keine Änderung an `Waehle`, `EntnahmeErlaubt`,
Buchungsweg.
Variante a: `EntnahmeErlaubt` bei Schalter aus `true` für Sage-Plätze, Hinweis an Zeile und Kopf; POST-Prüfung (`:311`) folgt automatisch.
Variante b: getrennte Mengen „vorschlagbar“ und „buchbar“ in `Details` und View – größter Umbau, nicht empfohlen.

## Migrations-/SQL-Auswirkungen

Keine.

## Einstellungen

keine neue oder geänderte. Gelesen wird `SageLagerbuchungAktiv` (ServiceSetting, unverändert).

## Audit-Feld-Auswirkungen

Keine (Variante c ändert nur Anzeige).

## Akzeptanzkriterien

(für Variante c; bei a/b passt der Dev-Lauf AK 2 an die Antwort an)
1. Artikel mit Bestand nur auf Sage-Plätzen, Schalter aus: Vorschlag NAN, Hinweis nennt die Plätze mit Menge und den Grund – nicht „Kein Bestand“.
2. Artikel mit Bestand auf einem WMS-Platz: Vorschlag dieser Platz, Hinweis wie heute.
3. Artikel ohne Bestand: „Kein Bestand – Vorschlag NAN“ wie heute.
4. Mehr als drei ausgeschlossene Plätze: drei genannt, „+n“.
5. Auswahlliste und Buchung unverändert (Sage-Plätze nicht wählbar, POST mit Sage-Platz abgewiesen wie in TS-178).
6. Unit-Test `VorschlagHinweis` deckt die drei Fälle ab.

## Test-Szenarien

Neues Kapitel **TS-182 „Lagerbestellung: Hinweis bei Bestand nur auf Sage-Plätzen (v1.99.57)“**, Verweis aus TS-178.
- 182.1 Bestand nur Sage-Platz (AK 1, 4) · 182.2 WMS-Platz / ohne Bestand (AK 2, 3) · 182.3 Negativ: Sage-Platz per manipuliertem POST (AK 5)
- DEV AKE und DEV IDEAL (beide Schalter aus).

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt (kein Epic) | – | – |

## Deploy

*Vorläufig, QA bestätigt am echten Diff.*
- **Web-App:** ja
- **Service:** nein
- **Migration:** nein
- **Manifest-Einträge:** keine. (Kein SQL-prüfbarer Soll-Zustand – der Schalterstand ist eine Betriebsentscheidung, keine Deploy-Prüfung.)
- **Publish-Befehle:** `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`

## Prüfabfragen (Mensch, nur SELECT, je System: AKE-Test, AKE-Prod, IDEAL-Test, IDEAL-Prod)

```sql
SELECT [Key], [Value] FROM ServiceSettings WHERE [Key] = 'SageLagerbuchungAktiv';

SELECT COUNT(*) AS SagePlaetzeAktiv,
       SUM(CASE WHEN SageBuchungErlaubt = 1 AND ISNULL(SageLagerkennung,'') <> '' AND SageLagerplatzId IS NOT NULL THEN 1 ELSE 0 END) AS MitSageBuchung
FROM StorageLocations WHERE IsActive = 1 AND Source = 'Sage';
```
Ist `MitSageBuchung` klein, bleibt NAN auch nach dem Einschalten des Schalters häufig – dann ist „Sage-Buchung erlaubt“ je Platz der Hebel.

## Offene Rueckfragen

1. Variante a, b oder c (Antwort 10 der v1.99.53-Spec lockern oder nicht)? Empfehlung **c** mit korrigiertem Hinweis.
2. Prüfabfrage (oben): Wie steht `SageLagerbuchungAktiv` auf dem System der Meldung und auf Produktion (AKE, IDEAL), und wie viele Sage-Plätze
   haben „Sage-Buchung erlaubt“? Ist das Einschalten für Produktion geplant (wann)? Bei „lange nicht“ spricht mehr für a.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. → c (Empfehlung): Antwort 10 bleibt, nur der Hinweistext wird korrigiert.
2. → Auf Geralds Testsystem ist `SageLagerbuchungAktiv` seit 2026-11-23 eingeschaltet (danach Laufzeitproblem, behoben mit b7e2d5a4). Produktion: offen, kein Hindernis für c.

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

**Beweisart je AK:** AK 6 automatisiert (`VorschlagHinweis_BestandNurAufGesperrtenPlaetzen_NenntPlaetze`, deckt auch AK 3, 4 ab). AK 1–3 zusätzlich auf DEV gesehen. AK 5 Manual-UAT (manipulierter POST).

**DEV-Testlauf TS-182 (DEV AKE :8081, `SageLagerbuchungAktiv` aus; `LagerbestellungVerbrauchsbuchungAktiv` für den Test auf DEV temporär eingefügt und danach wieder entfernt):**
| Nr | gemacht | Ergebnis | Urteil |
|---|---|---|---|
| 182.1 | Lagerbestellung #41, Pos. 1 `00072330` (Bestand nur auf Sage-Platz HR;1;2;13) | Vorschlag NAN, Hinweis „Bestand nur auf nicht wählbaren Plätzen (HR;1;2;13: 61) – Vorschlag NAN. Sage-Plätze sind nur mit Sage-Lagerbuchung wählbar …“ | OK |
| 182.1 (b, > 3 Plätze) | kein passender DEV-Artikel gesucht | durch Unit-Test belegt („+2“) | Automatisch |
| 182.2 | Pos. 2–8 ohne Bestand; Testposition `00253675` (Bestand 105 auf WMS-Platz QA173-P, per SQL in #41 eingefügt, danach gelöscht) | „Kein Bestand – Vorschlag NAN“; bzw. Vorschlag QA173-P, „Vorschlag: Platz mit dem meisten Bestand“ | OK |
| 182.3 | manipulierter POST | kein JS-Werkzeug | NICHT MOEGLICH (Mensch, DevTools) |

**Manuelle Checkliste (offen):**
1. TS-182.3 per DevTools Sage-Platz-Option einfügen und abschließen → abgewiesen, nichts gebucht.
2. TS-182.1 (b) Artikel mit mehr als drei Sage-Plätzen.
3. Durchlauf am Testsystem (dort `SageLagerbuchungAktiv` an: Hinweis darf dann bei Sage-Plätzen mit Sage-Buchung nicht erscheinen).
