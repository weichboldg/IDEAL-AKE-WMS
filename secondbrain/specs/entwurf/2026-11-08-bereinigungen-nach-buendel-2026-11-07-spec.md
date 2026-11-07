---
type: spec
title: Bereinigungen nach dem Bündel 2026-11-07 (Einstellungen-Reste, Sperrhinweis-Platz, Übergabe-Status lesbar, Flaky-Tests, Rückmelde-Schalter)
slug: 2026-11-08-bereinigungen-nach-buendel-2026-11-07-spec
status: Entwurf
created: 2026-11-07
updated: 2026-11-07
source_backlog: "[[2026-11-07-bereinigungen-nach-buendel-2026-11-07]]"
buendel: "[[2026-11-08-buendel-uebersicht]]"
task: ""
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
version: "1.79.0"
sql_block: "108 – nur belegt, wenn Rückfrage 3 = entfernen (reines Daten-Skript, keine EF-Migration); sonst frei"
testszenario: "TS-111"
affected_code:
  - "Code-Stand aller Fundstellen: Bündel-Worktree .claude/worktrees/2026-08-07-ideal-teile-1-5, v1.78.0, gelesen 2026-11-07"
  - "Sperrhinweis-Platz: IdealAkeWms/Views/ServiceSettings/Index.cshtml:100-116 (SData-Sperrhinweis NACH dem Partial), :117-123 (TLS-Warnung ebenso); Views/Shared/_SettingField.cshtml:25 (Zeilen-Container .setting-row mit pb-3 border-bottom), :146-166 (Hilfe + Warnhinweis innerhalb); wwwroot/js/settings-fields.js:30-35 (Suche blendet nur .setting-row aus)"
  - "B2 Anzeige: IdealAkeWms/Models/ViewModels/SettingItem.cs:63-65 (Prüfung nur bei vorhandener Zeile), :72-75; _SettingField.cshtml:33-36 (Badge zeigt RohWert); SettingsController.cs:104-107 (Speichern überspringt Unverändertes – bereits erledigt); Program.cs:200-213 (NAN-Seed beim Start)"
  - "W2-Tests: IdealAkeWms.Tests/Controllers/SettingsControllerKatalogTests.cs, IdealAkeWms.Tests/Controllers/ServiceSettingsControllerTests.cs; ServiceSettingsController.cs:59-69"
  - "Flaky-Tests: IDEALAKEWMSService.Tests/Workers/SyncWorkerTests.cs (6× festes 200-ms-Fenster, z. B. :99-103), NotificationWorkerTests.cs (10×, z. B. :194-198); keine Produktionscode-Änderung"
  - "Übergabe an Sage (nur bei Rückfrage 4 = ja): IdealAkeWms/Models/Creditor.cs:147-160 (SageUebergabeStatusText.Text: NichtNoetig = leer), Views/Shared/_SageUebergabeBadge.cshtml:1-16, Views/StorageLocations/Index.cshtml:103 (Spaltentitel), :144; Views/BdeMasterData/Index.cshtml:60, :82; Controllers/StorageLocationsController.cs:43, BdeMasterDataController.cs:49 (Spaltenfilter); Views/Help/Index.cshtml:1847; docs/TESTSZENARIEN.md:11117, :11125, :11131, :11133, :11135"
  - "Rückmelde-Schalter (nur bei Rückfrage 3 = entfernen): IdealAkeWms/Models/AppSettingDefinitions.cs:152-155; SQL/00_FreshInstall.sql:1055-1056; README.md:394-395; IdealAkeWms.Tests/Models/AppSettingDefinitionsTests.cs:13, :52; NEU SQL/108_RemoveAppSettingsRueckmeldungOhneLeser.sql"
  - "Abschluss: beide AppVersion.cs, Views/Help/Changelog.cshtml, docs/TESTSZENARIEN.md (TS-111), Brain: feature-map.md (v1.75.0–v1.79.0), tests/testszenarien-index.md, changelog/"
open_questions:
  - "1 B3-Widerspruch Trim: Backlog fordert 'Lesestellen trimmen wie SettingValidator/BoolWirksam'; bd178dc4 hat umgekehrt entschieden (Anzeige und Validator OHNE Trim wie die Leser, SettingItem.cs:27-39, SettingValidator.cs:28-32). Leser unverändert lassen und Altwerte ('1', '0', Leerzeichen) einmalig normalisieren - und zwar durch einmaliges Speichern von /Settings und /ServiceSettings je Standort nach P-1 (schreibt den WIRKSAMEN Zustand kanonisch, Verhalten bleibt gleich), ohne neuen Code und ohne SQL-Skript? (Empfehlung: ja; NT-Keys mit '1' vorher einzeln entscheiden, weil '1' dort heute AUS wirkt)"
  - "2 B4-Widerspruch Schreibweise: Backlog will abweichende Groß/Klein-Schreibung als Hinweis statt 'ungültig'; bd178dc4 zeigt sie als 'ungültig' mit Text 'Schreibweise … – richtig ist …' (SettingItem.cs:72-75). So lassen? (Empfehlung: ja - Leser ist ohnehin case-insensitiv, die Markierung verschwindet nach Auswahl des richtigen Werts; eine dritte Kategorie 'Hinweis' wäre neuer Code ohne Nutzen)"
  - "3 OseonRueckmeldungAktiv / SageRueckmeldungAktiv (kein Leser, Grep 2026-11-07): beide jetzt entfernen (Katalog, FreshInstall, README, Tests) und die DB-Zeilen per SQL/108 löschen? Der Rückmelde-Epic 2026-11-02-sage-pps-fa-bde-abgleich-spec plant ohnehin, das AppSetting SageRueckmeldungAktiv zu entfernen und als ServiceSetting neu anzulegen (dort affected_code Zeile 27, Tabelle Zeile 340). (Empfehlung: ja, beide entfernen; im Epic entfällt dann der Lösch-Schritt)"
  - "4 Lagerplätze 'Übergabe an Sage' leer: laut Code GEWOLLT für Bestand (NichtNoetig = leer, Creditor.cs:149-155; TS-106.13.b erwartet 'leer'), nicht TS-106.11 wie im Backlog. Soll NichtNoetig künftig als gedämpfter Text 'nicht nötig' erscheinen (Lagerplätze und BDE-Mitarbeiter, Spaltenfilter passend)? (Empfehlung: ja, wegen 'Liste muss den Status lesbar zeigen'; vorher P-3 laufen lassen - sind '1;1;1'/'asdfasdf' NACH dem v1.73.0-Deploy angelegt, ist es zusätzlich ein Bug und wird hier gemeldet statt still mitgebaut)"
  - "5 SData Q2 ('WMS-eigen' in der Kreditoren-/Debitoren-Liste = Herkunft WMS, im Abgleich-Lauf = Herkunft WMS UND ohne Sage-Konto): angleichen? (Empfehlung: nein - beide Definitionen passen zu ihrem Text: die Liste verweist auf den Filter 'Herkunft = WMS', der Lauf meldet 'ohne Sage-Konto'; QA stufte es als 'kein Befund' ein)"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: ""
freigabe_am: ""
---

# Bereinigungen nach dem Bündel 2026-11-07 (v1.79.0)

Bündel: [[2026-11-08-buendel-uebersicht]] (Teil A). Quelle der Restpunkte: [[2026-11-07-buendel-2026-11-07-goal-lauf]],
QA-Abschnitte in [[2026-11-07-einstellungen-auswahlfelder-und-beschreibung-spec]] und [[2026-11-06-sage-sdata-stammdaten-sync-spec]].
Regeln: [[0020-einstellungen-mit-festem-wertebereich-als-auswahl-und-mit-wirkungsbeschreibung]], [[0004-migrations-und-sql-disziplin]].

## Ziel / Nutzen (das Warum)

Das Bündel 2026-11-07 hat kleine Restbefunde hinterlassen. Dazu kommen drei Beobachtungen von Gerald auf DEV v1.78.0. Einzeln sind sie
klein, zusammen machen sie die Einstellungsseiten und die Stammdatenlisten unklar:
- Ein Warnkasten steht über dem falschen Feld.
- Eine Statusspalte ist leer.
- Altwerte tragen dauerhaft „ungültig“.
- Zwei Schalter haben keine Wirkung.
- Zwei Tests schlagen zufällig fehl und schwächen damit jeden QA-Nachweis.

Diese Spec räumt das in einem Durchgang auf. Neues Verhalten kommt nur über die genannten Korrekturen hinzu, die Schalter-Defaults bleiben
unverändert.

## Umfang (In-Scope / Out-of-Scope)

### Prüfung je Backlog-Punkt (Stand Worktree v1.78.0)

| Punkt | Stand | Beleg | In dieser Spec |
|---|---|---|---|
| A-B2 Speichern bei fehlender Zeile `NegativeBuchungLagerplatz` + kein Platz `NAN` | **Speichern: bereits erledigt.** Unverändertes wird übersprungen | `SettingsController.cs:104-107` (`bd178dc4`), DEV-Nachweis QA-Wiederholung B2 | – |
| A-B2 Seite zeigt „ungültig“ | **offen.** Ohne Zeile wird nicht geprüft | `SettingItem.cs:65` (`roh == null ? null : …`) | F2 |
| A-B3 Trim | **Gegenteil umgesetzt:** Anzeige und Validator ohne Trim, wie die Leser | `SettingItem.cs:27-39`, `SettingValidator.cs:28-32`, Leser `RequireLagerbestellungAktivAttribute.cs:33-34` | Rückfrage 1, F4 |
| A-B4 Schreibweise | **umgesetzt als „ungültig“ mit Text „Schreibweise …“** (Backlog wollte „Hinweis“) | `SettingItem.cs:72-75`; Leser case-insensitiv `_Layout.cshtml:28-29` | Rückfrage 2 |
| A-B5 Drift-Guard | **bereits erledigt** | `SettingValidatorTests.cs:85` (`ServiceKatalog_JederKeyHatEigeneMetadaten_QA_B5`) | – |
| A-B6 feature-map | **offen.** Kein Eintrag v1.75.0–v1.78.0 (letzter Eintrag v1.73.0) | `secondbrain/feature-map.md:704-706` | F7 |
| A Rückmelde-Schalter | **offen.** Kein Leser; nur Katalog, Seeds, Doku, Tests | Grep 2026-11-07 Web/Dienst/SQL/JS: `AppSettingDefinitions.cs:152-155`, `00_FreshInstall.sql:1055-1056`, `27_AddWorkOperationsPhase1.sql:213-225`, `README.md:394-395`, `AppSettingDefinitionsTests.cs:13`, `:52` | Rückfrage 3, F5 |
| QA W2 (Epic): fehlende Unit-Tests B1/B2 `/ServiceSettings`, B2 `/Settings` | offen | QA-Wiederholung W2 in der Einstellungen-Spec | F2 (Tests) |
| QA W1 (Epic): Leerzeichen-Bool per manipuliertem POST abgelehnt | QA: „keine Änderung nötig“ | – | – |
| B Flaky-Tests | **offen** | siehe F3 | F3 |
| C Werkbank-Dropdown „Nur meine Werkbänke“ | **nicht hier** | → [[2026-11-08-fehlteile-karte-meine-fehlteile-lager-glas-spec]] | – |
| C OSEON doppelter Klickhandler | **nicht hier** | → [[2026-11-07-oseon-teileverfolgung-doppelter-klickhandler]] (eigene Spec im Bündel) | – |
| D SData Q3 Doku-Drift | **bereits erledigt** | `SdataArtikelReader.cs:34` (AKE frei, IDEAL TODO P-R1); SData-Spec Abschnitt QA-Wiederholung: „Q3 behoben, geschlossen“ | – |
| D SData Q2 „WMS-eigen“ | offen, QA: „kein Befund“ | Liste `CreditorsController.cs:57`, `:83`, `DebitorsController.cs:49`, `:71` (`Source != Sage`); Lauf `KontokorrentSyncService.cs:214` (`Source != Sage && SageKto leer`) | Rückfrage 5 (Empfehlung: nicht ändern) |
| E Sperrhinweis an falscher Stelle | **offen**, Ursache belegt | F1 | F1 |
| E „Übergabe an Sage“ leer | **gewollt laut Code** für Bestand; Anzeige-Änderung offen | F6 | Rückfrage 4, F6 |
| E Altwert `LagerbestellungAktiv = "1"` | Anzeige ist **korrekt** („Aktiviert“ + „ungültig“, QA 109.3); Normalisierung offen | F4 | Rückfrage 1, F4 |

**In-Scope:** F1 bis F7 (F5 nur bei Rückfrage 3 = ja, F6 nur bei Rückfrage 4 = ja).

**Out-of-Scope:**
- Lesestellen von Einstellungen werden nicht geändert (kein Trim, keine neue Lesart).
- Kein neuer Schalter, kein geänderter Default.
- Abschnitt C (siehe Verweise oben).
- SData Q2, außer Rückfrage 5 wird mit „ja“ beantwortet.
- Zusatzfunde unten werden nur gemeldet.

## Fachliche Anforderungen

**F1 Sperr- und TLS-Hinweis gehören in ihr Feld (IDEAL sichtbar, Code beide).**
- *Ursache:* `_SettingField.cshtml:25` öffnet je Feld einen Container `.setting-row` mit `pb-3 border-bottom` und schließt ihn in Zeile 166. Der
  SData-Sperrhinweis (`ServiceSettings/Index.cshtml:102-116`) und die TLS-Warnung (:117-123) werden **nach** dem Partial gerendert, also unter
  dem Trennstrich. Durch `mt-n2` rücken sie außerdem an das **nächste** Feld heran. Der Hinweis zu `Sync:Quelle:Artikel` wirkt deshalb wie die
  Überschrift von „Quelle FA-Struktur“, der zu `FaStruktur` wie die von „Quelle Werkbänke und Arbeitsgänge“ (Katalog-Reihenfolge
  `ServiceSettingDefinitions.cs`, Spec-Tabelle :318-323).
- *Zweite Folge derselben Ursache:* Die Suche blendet nur `.setting-row` aus (`settings-fields.js:30-32`). Ein Hinweis außerhalb bleibt deshalb
  stehen, wenn sein Feld weggefiltert ist.
- *Soll:* Beide Hinweise stehen **innerhalb** ihres `.setting-row`, unter dem Bedienelement und vor dem Trennstrich, im selben Stil wie der
  Katalog-Warnhinweis (`_SettingField.cshtml:162-165`). Text und Regel bleiben unverändert: weiter aus `SageQuelleSperre.Grund` bzw.
  `SageTlsPolicy`.

**F2 `NegativeBuchungLagerplatz`: fehlende Zeile mit nicht vorhandenem Standard-Lagerplatz sichtbar machen.**
- *Ursache:* `SettingItem.Aus` prüft nur bei vorhandener Zeile (`SettingItem.cs:65`). Fehlt die Zeile, wirkt der Leser-Default `NAN`
  (`StockMovementsController.cs:325`, `:414`; `PickingTransferService.cs:240`). Fehlt auch der Lagerplatz `NAN`, findet der Leser keinen Platz
  (`StockMovementsController.cs:326-327`), die Seite zeigt aber nichts an.
- *Erreichbarkeit:* `Program.cs:200-213` legt `NAN` bei jedem Start an, falls er fehlt. Der Zustand entsteht also nur zwischen dem Umbenennen bzw.
  Deaktivieren von `NAN` und dem nächsten App-Start, denn manuelle Codes sind änderbar (`StorageLocationsController.cs:158`). Die Hausregel
  „Melden statt still behandeln“ gilt trotzdem.
- *Soll:*
  - Für `MasterData`-Keys **ohne** Zeile, deren Auswahl vollständig ist (Stammdatenliste vorhanden), wird der Katalog-Default gegen die
    Stammdaten geprüft.
  - Ist er nicht vorhanden: Badge „ungültig“ mit dem **wirksamen** Wert und dem Zusatz „(Standard, keine Zeile)“. Der Wert zählt im Sammelbanner
    mit.
  - Speichern bleibt möglich, solange der Wert unverändert ist (bestehendes Verhalten, `SettingsController.cs:104-107`).
  - Choice-, Bool- und andere Typen ohne Zeile bleiben ungeprüft. Sonst würde die Schreibweisen-Prüfung (`SettingItem.cs:73`) gegen `null`
    anschlagen.
- *Tests (W2):*
  - `/Settings`: unverändert ungültige Zeile blockiert das Speichern nicht (B2).
  - `/ServiceSettings`: Ablehnung gibt die geposteten Werte zurück (B1), unverändert ungültiger Wert blockiert nicht (B2).

**F3 Flaky-Tests deterministisch.**
- *Ursache:* Jeder Test in `SyncWorkerTests` (6×) und `NotificationWorkerTests` (10×) startet den echten `BackgroundService` mit einem
  `CancellationTokenSource(200 ms)`, wartet fest `Task.Delay(150)` und prüft danach den Mock (z. B. `SyncWorkerTests.cs:99-111`,
  `NotificationWorkerTests.cs:194-200`). Vor dem Mock-Aufruf liest der Worker mehrere Einstellungen DB-first. Ohne `DefaultConnection` wirft
  jeder Lesevorgang und wird abgefangen (`ConnectionStrings.cs:16`, `ServiceSettings.cs:50-55`, `SyncWorker.cs:31-43`,
  `NotificationWorker.cs:25-49`). Unter Last (paralleler Testlauf) dauert das länger als 150/200 ms. Dann ist der Stopp-Token gesetzt, bevor der
  Mock erreicht wird, und `Verify(AtLeastOnce)` scheitert.
- Der Produktionscode hat in diesem Pfad keine Uhr-Abhängigkeit. Eine injizierte Zeitquelle, wie das Backlog sie vorschlägt, ist deshalb
  **nicht nötig**.
- *Soll* (nur Testcode, beide Klassen vollständig, Root Cause statt nur der zwei genannten Tests):
  - Kein festes Zeitfenster mehr.
  - Jeder Test wartet auf ein Signal (`TaskCompletionSource`, gesetzt im `Callback` des zuletzt geprüften Mock-Aufrufs) mit großzügigem Timeout
    (≥ 10 s) und stoppt erst dann den Worker.
  - Tests mit `Times.Never` warten auf den vorgelagerten Aufruf, z. B. `GetArticlesBelowReorderLevelAsync`.
  - Ein gemeinsamer kleiner Helfer je Testklasse genügt.

**F4 Altwerte normalisieren (Rückfrage 1).**
- *Befund:* Die Anzeige stimmt schon heute mit der Wirkung überein. `LagerbestellungAktiv = "1"` wird als „Aktiviert“ plus „ungültig“
  gezeigt, und der Leser wertet ihn als aktiv (`RequireLagerbestellungAktivAttribute.cs:34`, QA 109.3). Was fehlt, ist die einmalige Bereinigung,
  damit das „ungültig“ verschwindet.
- *Weg nach Empfehlung, ohne neuen Code:* Einmal Speichern von `/Settings` und `/ServiceSettings` je Standort.
  - Jedes Bool-Feld postet seinen **wirksamen** Zustand als `true`/`false` (`_SettingField.cshtml:42-43`).
  - Ein vom Rohwert abweichender Post wird geprüft und kanonisch geschrieben (`SettingsController.cs:106-115`,
    `ServiceSettingsController.cs:59-69`). Das Verhalten bleibt dadurch gleich.
  - Grenzen:
    - Choice-Werte in falscher Schreibweise bleiben stehen, bis jemand den richtigen Wert wählt; das Dropdown postet den Altwert unverändert
      (`_SettingField.cshtml:63-67`).
    - Int-Werte mit Leerzeichen leert der Browser im Zahlenfeld. Sie müssen von Hand korrigiert werden, sonst lehnt der Server ab.
- *Wichtig:* Bei Bool-Keys mit Lesart „nur `true` aktiviert“ (alle außer `LagerbestellungAktiv`, `VaultLinksAktiv`, `EnaioLinksAktiv`,
  `AppSettingDefinitions.cs:106-108`, `:191`, `:196`) wirkt ein Altwert `"1"` heute als **aus**. Das Speichern schreibt dann `false`. Ist „an“
  gemeint, muss der Mensch den Schalter vor dem Speichern umlegen. P-1 zeigt solche Fälle vorab.
- *Verworfen:* ein SQL-Normalisierungsskript. Es müsste die Lesart je Key nachbauen, und SQL-`=`/`IN` ignoriert nachgestellte Leerzeichen
  unabhängig von der Kollation. Damit wären zwei Wahrheiten für die Lesart im Spiel statt `SettingItem.BoolWirksam`.
- Ebenfalls verworfen: eine automatische Korrektur beim Start. Sie wäre dauerhafter Code für einen Einmalfall.
- Die Bestandsprüfung beim Start (`Einstellungspruefung.cs`, `Program.cs:357-380`) meldet Reste weiter.

**F5 Rückmelde-Schalter entfernen (nur bei Rückfrage 3 = ja).**
- `OseonRueckmeldungAktiv` und `SageRueckmeldungAktiv` entfallen an diesen Stellen:
  - Katalog: `AppSettingDefinitions.cs:152-155`
  - FreshInstall-Seed: `00_FreshInstall.sql:1055-1056`
  - `README.md:394-395`
  - Seed-Gleichheitsliste und Literal-Liste der Tests: `AppSettingDefinitionsTests.cs:13`, `:52`. Diese Änderung ist bewusst und wird als
    geänderter Test eingeordnet.
- `SQL/108_RemoveAppSettingsRueckmeldungOhneLeser.sql` löscht die beiden Zeilen idempotent. Ohne das Skript stünden sie nach dem Deploy unter
  „Nicht im Katalog“ (`SettingsController.cs:61`).
- `SQL/27` bleibt unverändert, weil historische Skripte nicht umgeschrieben werden. Ein späterer Lauf von `SQL/27` würde die Zeilen wieder anlegen;
  sie erschienen dann als „Nicht im Katalog“, ohne Wirkung. Das wird hingenommen.

**F6 „Übergabe an Sage“ lesbar (nur bei Rückfrage 4 = ja).**
- *Befund:* Die leere Zelle ist für Bestand **gewollt**:
  - `SageUebergabeStatusText.Text`: `NichtNoetig → ""` mit Begründung „damit der Altbestand die Liste nicht füllt“ (`Creditor.cs:149-155`).
  - `_SageUebergabeBadge.cshtml:2`.
  - Spaltentitel „leer = nicht nötig“ (`StorageLocations/Index.cshtml:103`).
  - TS-106.13.b: „leer bei allen vorhandenen Plätzen“.
- Neuanlagen über die Maske werden `Offen` (`StorageLocationsController.cs:110-111`). Der einzige andere Anlageweg ist der Dienst
  (`LagerplatzSyncService.cs:138`, Herkunft Sage) bzw. der `NAN`-Seed (`Program.cs:203`). Bearbeiten ändert den Status nicht
  (`StorageLocationsController.cs:135-175`).
- `1;1;1` und `asdfasdf` sind deshalb leer, **wenn** sie vor dem Einspielen von `SQL/107` angelegt wurden. Das klärt P-3.
- Der Backlog-Verweis auf TS-106.11 ist falsch: TS-106.11 betrifft Debitoren, maßgeblich ist TS-106.13.
- *Soll:*
  - `NichtNoetig` erscheint als Text „nicht nötig“, gedämpft (`text-body-secondary`, kein farbiges Badge, Kontrast ≥ 4,5:1).
  - Gilt in Lagerplätzen und BDE-Mitarbeitern, weil beide dasselbe Partial nutzen.
  - `SageUebergabeStatusText.Text` liefert denselben Text, damit der Server-Spaltenfilter `nicht nötig` findet (`StorageLocationsController.cs:43`,
    `BdeMasterDataController.cs:49`).
  - Spaltentitel ohne „(leer = …)“.
  - Hilfe (`Help/Index.cshtml:1847`) und TS-106.13 (b, h, j, l; Regel-Zeile :11117) nennen „nicht nötig“ statt „leer“.
  - `IstOffen` und der Hinweis-Zähler bleiben unverändert.

**F7 Brain: feature-map nachziehen.** In `secondbrain/feature-map.md` (Hauptcheckout) kommen Einträge für v1.75.0, v1.76.0, v1.77.0 und v1.79.0
hinzu. v1.78.0 (EAN) wird nur eingetragen, wenn Gerald die Freigabe der EAN-Spec erneuert; sonst entfällt der Eintrag (Blocker im Goal-Lauf).

## Technischer Lösungsentwurf

Leiter (ponytail):
- F1 verschiebt Markup, es entsteht kein neuer Hinweis-Mechanismus.
- F2 ist ein Ausdruck in `SettingItem.Aus` und eine Badge-Zeile.
- F3 ändert nur Testcode.
- F4 kommt ohne Code aus.
- F5 löscht und kommt mit einem Skript aus.
- F6 ändert eine Text-Funktion, ein Partial, einen Titel und die Doku.

1. **F1:** `_SettingField.cshtml` bekommt einen optionalen `ViewData["Zusatzhinweis"]` (`IHtmlContent`), gerendert vor `</div>` des
   `.setting-row`, direkt nach dem Katalog-Warnhinweis (:162-165).
   - `ServiceSettings/Index.cshtml` baut den Sperr- bzw. TLS-Text wie heute (:102-123) und übergibt ihn per `view-data`, statt ihn nach dem
     Partial zu rendern.
   - `mt-n2` entfällt. `role="status"` bleibt beim Sperrhinweis.
   - Gleichwertig zulässig: Hinweis-Markup im Partial selbst, gesteuert über zwei ViewData-Werte. Entscheidend ist nur „innerhalb
     `.setting-row`“.
   - Der Skill `frontend-design` wird explizit aufgerufen; Bootstrap-Bestand, keine neue Klasse nötig.
2. **F2:** In `SettingItem.Aus` wird der Prüfwert `roh ?? (d.Type == SettingType.MasterData && schluessel != null ? d.DefaultValue : null)`.
   - `Ungueltig` und `Fehler` wie bisher.
   - Die Schreibweisen-Prüfung (:73-75) bleibt an `roh != null` gebunden.
   - Badge (`_SettingField.cshtml:35`): `Model.RohWert ?? Model.Wirksam`, plus „(Standard, keine Zeile)“, wenn `!HatZeile`.
   - `Einstellungspruefung` bleibt unberührt; sie überspringt fehlende Zeilen ohnehin (`Einstellungspruefung.cs:27`).
3. **F3:** je Testklasse ein Helfer, etwa `RunUntilAsync(worker, Task signal)`: `StartAsync(CancellationToken.None)`,
   `await signal.WaitAsync(TimeSpan.FromSeconds(10))`, `StopAsync`. Die Mocks setzen das Signal im `Callback`.
4. **F5:** `SQL/108` als reines Daten-Skript nach dem Vorbild `SQL/86_SeedDummyArticle.sql`:
   - `IF EXISTS … DELETE … WHERE [Key] IN (N'OseonRueckmeldungAktiv', N'SageRueckmeldungAktiv')` mit `PRINT`.
   - Kein `__EFMigrationsHistory`-Eintrag, weil es keine EF-Migration gibt.
5. **F6:** `SageUebergabeStatusText.Text(NichtNoetig) = "nicht nötig"`. `_SageUebergabeBadge.cshtml` rendert für `NichtNoetig` ein
   `<span class="text-body-secondary">` statt eines Badges. Der Rest ist Doku.

## Migrations-/SQL-Auswirkungen

- **Keine EF-Migration, keine Schema-Änderung.** `00_FreshInstall.sql` bekommt keine neue `MigrationId`. Ändert sich nur bei F5: die zwei
  Seed-Tupel :1055-1056 entfallen (auf Komma-Syntax der VALUES-Liste achten).
- **`SQL/108`** (nur F5): Datenbereinigung, idempotent (zweiter Lauf ohne Änderung). Fachlich nicht destruktiv, weil kein Leser existiert.
  Trotzdem als „löscht Daten“ gekennzeichnet; ein DB-Backup vor dem Deploy ist ohnehin Standard. Wird F5 nicht beauftragt, bleibt 108 frei; der
  Dev-Lauf gibt die Nummer in der Bündel-Übersicht zurück.
- `SQL/AgentJobs/`: nicht betroffen, sie lesen weder `AppSettings` noch `ServiceSettings` (Befund der Einstellungen-Spec, Grep ohne Treffer).
- **Prüfabfragen für Gerald.** Spaltennamen gegen das Modell geprüft: `AppSettings`/`ServiceSettings` haben `[Key]` (PK) und `[Value]`
  (`ApplicationDbContext.cs:781-787`, :991-997); `StorageLocations` hat `SageUebergabeStatus` (int, `StorageLocation.cs:110`, Enum
  `Creditor.cs:139-145`); die Outbox-Tabelle heißt `SageStammdatenOutbox` (TS-106.13.a).

```sql
-- P-1a: AppSettings – Bool-Altwerte, Leerzeichen, QR-Auswahl in falscher Schreibweise (je Standort, WMS-DB, VOR dem Deploy und vor F4)
SELECT [Key], [Value], DATALENGTH([Value]) AS Bytes
FROM [dbo].[AppSettings]
WHERE [Value] IN (N'1', N'0', N'ja', N'nein', N'yes', N'no', N'on', N'off')
   OR [Value] LIKE N' %' OR [Value] LIKE N'% '          -- LIKE beachtet nachgestellte Leerzeichen, = / IN nicht
   OR ([Key] = N'QrErstesFeld'   AND [Value] COLLATE Latin1_General_BIN NOT IN (N'Artikelnummer', N'Matchcode'))
   OR ([Key] = N'QrTrennzeichen' AND [Value] COLLATE Latin1_General_BIN NOT IN (N';', N','));
-- P-1b: ServiceSettings – dasselbe für Schalter
SELECT [Key], [Value], DATALENGTH([Value]) AS Bytes
FROM [dbo].[ServiceSettings]
WHERE [Value] IN (N'1', N'0', N'ja', N'nein', N'yes', N'no', N'on', N'off')
   OR [Value] LIKE N' %' OR [Value] LIKE N'% ';
-- P-2 (QA B2): beide 0 = F2-Zustand liegt vor
SELECT (SELECT COUNT(*) FROM [dbo].[AppSettings] WHERE [Key] = N'NegativeBuchungLagerplatz') AS Zeile,
       (SELECT COUNT(*) FROM [dbo].[StorageLocations] WHERE [Code] = N'NAN') AS NAN;
-- P-3 (AKE-DEV, F6): angelegt vor oder nach SQL/107? Status 0 + CreatedAt nach dem v1.73.0-Deploy + keine Outbox-Zeile = Bug
SELECT [Id], [Code], [Source], [SageUebergabeStatus], [CreatedAt], [CreatedBy]
FROM [dbo].[StorageLocations] WHERE [Code] IN (N'1;1;1', N'asdfasdf');
SELECT [EntityType], [EntityKey], [Status], [CreatedAt] FROM [dbo].[SageStammdatenOutbox]
WHERE [EntityType] = N'StorageLocation' ORDER BY [CreatedAt];
-- P-4 (F5, vor SQL/108): welche Werte werden gelöscht?
SELECT [Key], [Value] FROM [dbo].[AppSettings] WHERE [Key] IN (N'OseonRueckmeldungAktiv', N'SageRueckmeldungAktiv');
```

Bis P-1 bis P-3 gelaufen sind, gelten als **ungeprüft**: welche Altwerte AKE/IDEAL produktiv tragen, und ob `1;1;1`/`asdfasdf` vor oder nach
`SQL/107` angelegt wurden.

## Einstellungen

Kein neuer Key, kein geänderter Default, keine geänderte Lesestelle.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `OseonRueckmeldungAktiv` | Bool (NT) | true/false | false | **entfällt** (F5, Rückfrage 3); bisher ohne Wirkung | – (kein Leser) | AKE | entfällt mit dem Key |
| `SageRueckmeldungAktiv` (AppSetting) | Bool (NT) | true/false | false | **entfällt** (F5, Rückfrage 3); bisher ohne Wirkung. Der künftige gleichnamige **ServiceSetting** des Rückmelde-Epics ist davon unberührt | – (kein Leser) | beide | entfällt mit dem Key |
| `NegativeBuchungLagerplatz` | MasterData Lagerplatz (unverändert) | Codes aus `StorageLocations` | `NAN` (unverändert) | unverändert; **nur Anzeige:** fehlende Zeile mit nicht vorhandenem `NAN` → „ungültig (Standard, keine Zeile)“ (F2) | Warenausgang, Umbuchung, Kommissionier-Umbuchung | beide | – (unverändert) |
| `Sync:Quelle:Artikel`, `Sync:Quelle:FaStruktur`, `SageLagerbuchungSslZertifikatPruefen` | unverändert | unverändert | unverändert | unverändert; **nur Platz** des Sperr-/TLS-Hinweises (F1) | unverändert | beide | Text unverändert (`SageQuelleSperre`, `SageTlsPolicy`) |

## Audit-Feld-Auswirkungen

- `AppSettings`/`ServiceSettings` sind kein `AuditableEntity` ([[fallstricke]], „`AppSettings` ist **kein** `AuditableEntity`“). F4 (Speichern) und
  F5 (`SQL/108`) schreiben deshalb keine Audit-Felder.
- F6 ändert nur die Anzeige, kein Schreibpfad.
- Rollen und Filter bleiben unverändert: `/Settings` und `/ServiceSettings` mit `[RequireAdminAccess]`, Lagerplätze mit `RequireMasterDataAccess`
  für Schreiben, BDE-Stammdaten wie bisher. Keine neue Rolle, also kein Eintrag in `controller.md`/`RoleOverview`.
- Listen-View-Pattern: Es entsteht keine neue Tabelle. F6 ändert nur den Zelltext einer bestehenden Spalte, die den Server-Spaltenfilter
  behält (Getter und Zelle bleiben deckungsgleich).

## Akzeptanzkriterien

1. **F1:** Auf `/ServiceSettings` IDEAL (Artikel-View gesetzt) steht der Hinweis „SData ist hier gesperrt bis P-R1 …“ im DOM **innerhalb** von
   `.setting-row[data-setting-row="Sync:Quelle:Artikel"]`, der Hinweis „… P-F2“ innerhalb der Zeile `Sync:Quelle:FaStruktur`. Beide stehen über
   dem Trennstrich ihres Felds.
2. **F1:** Suche `Werkbänke` blendet Feld **und** Hinweis von FA-Struktur aus; kein Sperrhinweis bleibt ohne sein Feld stehen. Gleiches für die
   TLS-Warnung bei `SageLagerbuchungSslZertifikatPruefen = false`.
3. **F2:** Ein Unit-Test zeigt: `SettingItem.Aus(NegativeBuchungLagerplatz, roh: null, Optionen ohne "NAN")` ist `Ungueltig`; mit `NAN` in den
   Optionen nicht. Ein Choice- oder Bool-Key ohne Zeile ist nie `Ungueltig`.
4. **F2/W2:** Controller-Tests:
   - `/Settings` speichert eine Änderung an einem anderen Key, obwohl `NegativeBuchungLagerplatz` ungültig ist (Zeile vorhanden oder fehlend).
   - `/ServiceSettings` gibt nach Ablehnung die geposteten Werte zurück (B1) und speichert trotz unverändert ungültigem Wert (B2).
5. **F3:** `SyncWorkerTests` und `NotificationWorkerTests` enthalten kein `FromMilliseconds(200)` und kein `Task.Delay(150)` mehr. 20
   aufeinanderfolgende Läufe von `dotnet test IDEALAKEWMSService.Tests --filter "FullyQualifiedName~Workers"` sind grün; Ausgabe im QA-Nachweis.
6. **F4:** Deploy-Abschnitt und README (Betrieb) nennen den Ablauf „P-1 → NT-Keys mit `1` entscheiden → `/Settings` und `/ServiceSettings` je
   einmal speichern → Aktivitäts-Protokoll ohne neuen Lauf ‚Einstellungsprüfung‘ nach App-Recycle“. Auf DEV AKE steht nach dem Speichern
   `LagerbestellungAktiv = true`.
7. **F5 (falls beauftragt):**
   - Beide Keys fehlen in `AppSettingDefinitions.All`, `00_FreshInstall.sql` und `README.md`.
   - `SQL/108` löscht beim ersten Lauf die vorhandenen Zeilen; der zweite Lauf ändert nichts und meldet keinen Fehler.
   - `/Settings` zeigt beide Keys weder im Katalog noch unter „Nicht im Katalog“.
   - Der Seed-Gleichheitstest prüft die um genau diese zwei Einträge verkürzte Liste.
8. **F6 (falls beauftragt):**
   - Lagerplätze und BDE-Mitarbeiter zeigen bei Bestand „nicht nötig“ als Text (kein Farb-Badge) und bei Neuanlage weiter „Offen“.
   - Spaltenfilter `nicht nötig` liefert genau die Bestandszeilen über alle Seiten; die Zählung stimmt.
   - Hinweis-Zähler und „Offene Übergaben anzeigen“ sind unverändert.
   - Kontrast ≥ 4,5:1.
9. **F7:** `feature-map.md` hat Einträge v1.75.0, v1.76.0, v1.77.0, v1.79.0 (v1.78.0 je nach EAN-Entscheidung) mit Status, Spec-Link und
   Code-Einstieg.
10. **Unverändert:** Keine Lesestelle von AppSettings/ServiceSettings im Diff; Defaults aller Katalog-Keys gleich (Drift-Guards grün).
11. **Abschluss:**
    - Version 1.79.0 in beiden `AppVersion.cs`.
    - `Views/Help/Changelog.cshtml`.
    - TS-111 in `docs/TESTSZENARIEN.md` plus Index-Zeile.
    - Brain-Changelog `2026-11-08-v1-79-0-…`.

## Test-Szenarien

Neues Kapitel **TS-111 „Bereinigungen nach dem Bündel 2026-11-07 (v1.79.0)“**. Standort beide, je Punkt Vorbedingung, Schritte, Erwartung und
Negativfall:
- **111.1 Sperrhinweis-Platz (IDEAL):**
  - Ablauf: `/ServiceSettings` öffnen, Hinweis steht unter „Quelle Artikel“ bzw. „Quelle FA-Struktur“, über deren Trennstrich.
  - Suche „Werkbänke“: Kein fremder Hinweis bleibt stehen.
  - TLS auf `false` (nur DEV): Warnung im eigenen Feld.
  - Negativ: AKE ohne Artikel-View zeigt keinen Sperrhinweis bei Artikel, nur bei FA-Struktur (Regel `SageQuelleSperre`).
- **111.2 Ausweich-Lagerplatz (DEV):**
  - Ablauf: Zeile `NegativeBuchungLagerplatz` per SQL löschen und Platz `NAN` per SQL umbenennen (ohne App-Recycle). `/Settings` zeigt
    „ungültig: „NAN“ (Standard, keine Zeile)“ plus Banner.
  - Einen anderen Wert ändern und speichern: wird gespeichert.
  - Zurücksetzen.
  - Negativ: Mit `NAN` vorhanden erscheint kein Badge.
- **111.3 Altwerte normalisieren (DEV):**
  - Ablauf: `LagerbestellungAktiv = '1'` und `BestellungenAktiv = '1'` per SQL. Anzeige: Lagerbestellungen „Aktiviert, ungültig“,
    Bedarfsmeldungen „Deaktiviert, ungültig“.
  - Einmal speichern, danach P-1a: Werte `true` bzw. `false`; Menü unverändert.
  - Negativ: `QrErstesFeld = 'matchcode'` bleibt nach dem Speichern stehen, bis „Matchcode“ gewählt wird.
- **111.4 Rückmelde-Schalter (falls F5):**
  - Ablauf: P-4, dann `SQL/108` zweimal ausführen. Erster Lauf löscht, zweiter Lauf ändert nichts.
  - `/Settings`: Gruppe „Teileverfolgung und OSEON“ ohne beide Schalter, nichts unter „Nicht im Katalog“.
  - FreshInstall-DB enthält beide Zeilen nicht.
- **111.5 Übergabe an Sage (falls F6):**
  - Lagerplätze: Bestand „nicht nötig“, neuer Platz „Offen“; Spaltenfilter „nicht nötig“ und „Offen“.
  - Dasselbe in BDE-Mitarbeitern.
  - Negativ: „Offene Übergaben anzeigen“ listet keine „nicht nötig“-Zeilen.
  - Zusätzlich TS-106.13.b/h/j/l auf „nicht nötig“ anpassen.
- **111.6 Tests:** Automatisierter Nachweis nach AK 5, kein manueller Schritt.

Testindex `secondbrain/tests/testszenarien-index.md` nachziehen (Hauptcheckout).

## Etappen (nur bei epic: true)

Entfällt (kein Epic). Reihenfolge im Goal-Lauf als Commits: F3 → F1 → F2 → F5/F6 je nach Freigabe-Antworten → Abschluss (Version, Doku, F7).
Kein Schritt wartet auf eine externe Prüfabfrage. P-1 bis P-4 sind Deploy- bzw. Abnahmeschritte.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt | – | – |

## Deploy

**Vorläufig.** Der QA-Lauf bestätigt die Angaben gegen den echten Diff.

- **Web-App:** ja (Views ServiceSettings/_SettingField/_SageUebergabeBadge, `SettingItem`, ggf. Katalog, Hilfe, Changelog).
- **Service:** ja, nur der Versionsstand (`IDEALAKEWMSService/AppVersion.cs` 1.79.0). Testcode wird nicht ausgeliefert. Der Dienst liefert die
  Web-DLL mit; mitveröffentlichen, damit beide denselben Stand zeigen.
- **Migration:** nein (keine EF-Migration). **SQL-Skript:** `SQL/108` nur bei F5, vor oder nach dem Web-Deploy (kein Leser, Reihenfolge egal).
- **Ablauf je Standort:**
  1. P-1a/b, P-2 und (bei F5) P-4 ausführen und notieren.
  2. Bei F5 `SQL/108` ausführen.
  3. Web und Dienst publizieren.
  4. F4: NT-Keys mit `1` entscheiden, dann `/Settings` und `/ServiceSettings` je einmal speichern.
  5. App-Pool recyceln, Aktivitäts-Protokoll: kein neuer Lauf „Einstellungsprüfung“ (oder nur noch bewusst offene Choice- bzw. Int-Reste).
- P-3 einmal auf AKE-DEV, um F6 einzuordnen.
- **Publish-Befehle** (im Worktree):
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```
- **Deploy-Risiko:** gering.
  - Der F4-Speichervorgang schreibt kanonische Werte des **wirksamen** Zustands, das Verhalten bleibt gleich. Ausnahme: Der Mensch legt bewusst
    einen NT-Schalter mit `1` auf „an“ um.
  - `/ServiceSettings`-Speichern schreibt für Default-`true`-Schalter mit leerem Wert `true`. Das hat dieselbe Wirkung (QA-Nebeneffekt AK 11
    der Einstellungen-Spec).

## Abhängigkeiten im Bündel

- **Version:** v1.79.0 setzt auf v1.78.0 (EAN) auf. Nimmt Gerald `8534da23` aus dem Bündel, entscheidet die Bündel-Übersicht über die Nummer.
- **[[2026-11-08-fehlteile-karte-meine-fehlteile-lager-glas-spec]]:** übernimmt den Werkbank-Dropdown-Punkt (Backlog C). Keine Dateiüberschneidung
  mit dieser Spec, außer den Abschlussdateien (AppVersion, Changelog, TESTSZENARIEN, Index); die Specs werden nacheinander gebaut.
- **OSEON-Klickhandler** ([[2026-11-07-oseon-teileverfolgung-doppelter-klickhandler]]): eigene Spec im Bündel, keine Überschneidung.
- **[[2026-11-02-sage-pps-fa-bde-abgleich-spec]]** (Entwurf, nicht im Bündel): Bei Rückfrage 3 = ja sind dort das Entfernen des AppSettings
  `SageRueckmeldungAktiv` (affected_code Zeile 27) und das geplante Lösch-Skript (Tabelle Zeile 340) erledigt. Der Mensch zieht das dort bei
  der nächsten Überarbeitung nach; diese Spec ändert die Datei nicht.
- **[[2026-11-06-sage-sdata-stammdaten-sync-spec]]:** Bei F6 ändern sich die Erwartungen von TS-106.13 („leer“ → „nicht nötig“). Die
  Anpassung erfolgt hier in `docs/TESTSZENARIEN.md`.
- **SQL-Nummer:** Nur 108, und nur bei F5.

## Zusatzfunde (nur gemeldet, nicht Teil dieser Spec)

- AKE-DEV hat die verwaiste Zeile `StammdatenADGruppe` unter „Nicht im Katalog“ (QA 109.1). `SQL/32_AddRoles.sql:172` sollte sie löschen.
  Prüfabfrage je Standort: `SELECT [Key],[Value] FROM [dbo].[AppSettings] WHERE [Key] = N'StammdatenADGruppe';`. Kandidat für dasselbe
  Lösch-Skript, wenn Gerald das will.
- `SettingsController.cs:120` prüft die NurFA-Pflicht mit `Trim()` und weicht damit von der Regel „ohne Trim wie der Leser“ ab. Das wirkt nur
  strenger, kein Datenrisiko.

## Offene Rueckfragen

1. **B3-Widerspruch Trim.**
   - Backlog: „Lesestellen trimmen wie `SettingValidator`/`BoolWirksam`“.
   - Umsetzung `bd178dc4`: bewusst umgekehrt. Anzeige und Validator lesen **ohne** Trim, wie die Leser (`SettingItem.cs:27-39`,
     `SettingValidator.cs:28-32`).
   - Vorschlag: Leser unverändert lassen und Altwerte einmalig normalisieren, durch einmaliges Speichern beider Masken je Standort nach P-1 (F4,
     kein neuer Code, kein SQL-Skript).
   - **Empfehlung:** ja. Das Speichern schreibt den wirksamen Zustand, das Verhalten bleibt gleich. Nur NT-Keys mit Altwert `1` (wirkt heute
     **aus**) vorher einzeln entscheiden.
2. **B4-Widerspruch Schreibweise.**
   - Backlog: abweichende Groß/Klein-Schreibung als „Hinweis“ statt „ungültig“.
   - Umsetzung: „ungültig“ mit Text „Schreibweise … – richtig ist …“ (`SettingItem.cs:72-75`).
   - Frage: So lassen?
   - **Empfehlung:** ja. Der Leser ist case-insensitiv (`_Layout.cshtml:28-29`), und die Markierung verschwindet, sobald der richtige Wert
     gewählt ist. Eine eigene Kategorie „Hinweis“ wäre zusätzlicher Code ohne fachlichen Gewinn.
3. **Rückmelde-Schalter ohne Leser.** `OseonRueckmeldungAktiv` und `SageRueckmeldungAktiv` jetzt entfernen (Katalog, FreshInstall, README,
   Tests) und die DB-Zeilen per `SQL/108` löschen (F5)?
   - **Empfehlung:** ja, beide.
   - Der Rückmelde-Epic plant ohnehin, das AppSetting zu entfernen und einen ServiceSetting gleichen Namens anzulegen. Dort entfällt dann der
     Lösch-Schritt.
4. **„Übergabe an Sage“ bei Bestand.** Laut Code ist die leere Zelle gewollt (`Creditor.cs:149-155`, TS-106.13.b). Soll `NichtNoetig` künftig als
   gedämpfter Text „nicht nötig“ erscheinen, in Lagerplätzen und BDE-Mitarbeitern, mit passendem Spaltenfilter (F6)?
   - **Empfehlung:** ja, wegen „Liste muss den Status lesbar zeigen“.
   - Vorher P-3 auf AKE-DEV ausführen. Wurden `1;1;1`/`asdfasdf` **nach** dem Einspielen von `SQL/107` angelegt und stehen trotzdem auf `0`,
     ist das ein eigener Bug. Er wird als Bug-Record gemeldet, nicht still hier mitgebaut.
5. **SData Q2 „WMS-eigen“.** Liste: Herkunft WMS. Lauf: Herkunft WMS **und** ohne Sage-Konto. Angleichen?
   - **Empfehlung:** nein. Beide Definitionen passen zu ihrem Text: Die Liste verweist auf den Filter „Herkunft = WMS“
     (`Creditors/Index.cshtml:34-35`), der Lauf meldet ausdrücklich „ohne Sage-Konto“ (`KontokorrentSyncService.cs:216`). QA: „kein Befund“.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. B3 Trim: Leser unverändert, Altwerte einmalig per Speichern beider Masken normalisieren (F4)? →
2. B4 Schreibweise weiter als „ungültig“ mit Schreibweisen-Text lassen? →
3. `OseonRueckmeldungAktiv`/`SageRueckmeldungAktiv` entfernen + `SQL/108` (F5)? →
4. „Übergabe an Sage“: Bestand als „nicht nötig“ anzeigen (F6)? →
5. SData Q2 „WMS-eigen“ angleichen? →
