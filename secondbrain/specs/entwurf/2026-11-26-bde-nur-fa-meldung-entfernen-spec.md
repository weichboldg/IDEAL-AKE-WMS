---
type: spec
title: AppSetting BdeNurFaMeldung entfernen (Altlast) – NurFA-Zweige, StartProductionForOrder, BdeDefaultArbeitsgang, Vorher-Pruefung
slug: 2026-11-26-bde-nur-fa-meldung-entfernen-spec
status: Entwurf
created: 2026-11-26
updated: 2026-11-28
source_backlog: "[[2026-11-25-bde-nur-fa-meldung-entfernen]]"
task: ""
jira: ""
worktree: ""
branch: ""
standort: beide
affected_code:
  - IdealAkeWms/Models/AppSettingKeys.cs
  - IdealAkeWms/Models/AppSettingDefinitions.cs
  - IdealAkeWms/Controllers/BdeApiController.cs
  - IdealAkeWms/Controllers/BdeTerminalController.cs
  - IdealAkeWms/Controllers/BdeTeilelisteController.cs
  - IdealAkeWms/Controllers/SettingsController.cs
  - IdealAkeWms/Controllers/ProductionWorkplacesController.cs
  - IdealAkeWms/Services/BdeBookingService.cs
  - IdealAkeWms/Services/BdeScanResolver.cs
  - IdealAkeWms/Services/WerkbankKuerzelFamilie.cs
  - IdealAkeWms/Services/BdeTeilelisteService.cs
  - IdealAkeWms/Services/BdeDefaultWorkOperationService.cs (loeschen)
  - IdealAkeWms/Services/IBdeDefaultWorkOperationService.cs (loeschen)
  - IdealAkeWms/Program.cs
  - IdealAkeWms/Data/Repositories/OperationConfigRepository.cs
  - IdealAkeWms/Data/Repositories/ProductionWorkplaceRepository.cs
  - IdealAkeWms/Models/ProductionWorkplace.cs
  - IdealAkeWms/Models/ViewModels/ProductionWorkplaceEditViewModel.cs
  - IdealAkeWms/Views/ProductionWorkplaces/Create.cshtml
  - IdealAkeWms/Views/ProductionWorkplaces/Edit.cshtml
  - IdealAkeWms/Views/ProductionWorkplaces/_BdeNurAgFertigmeldungField.cshtml
  - IdealAkeWms/Views/Help/Index.cshtml
  - IdealAkeWms/Views/Help/Changelog.cshtml
  - IdealAkeWms/wwwroot/js/bde-fa-sicht.js
  - IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs (nur Kommentar)
  - IDEALAKEWMSService/AppVersion.cs
  - SQL/170_RemoveAppSettingsBdeNurFaMeldung.sql (neu, Nummer vorlaeufig)
  - SQL/00_FreshInstall.sql
  - scripts/deploy/nachlauf.psd1
  - README.md
  - docs/TESTSZENARIEN.md
  - docs/anwender/17-bde-teileliste-werkbank.md
  - docs/betrieb/00-index.md
  - docs/betrieb/03-installation-update-rollback.md
  - docs/betrieb/04-konfiguration.md
  - docs/betrieb/07-troubleshooting.md
  - docs/betrieb/AGENTS.md
  - IdealAkeWms.Tests (BdeApiControllerTests, BdeTerminalControllerTests, SettingsControllerKatalogTests, AppSettingDefinitionsTests, BdeBookingServiceStufe1Tests, BdeDefaultWorkOperationServiceTests, BdeScanResolverTests, BdeTeilelisteMeldenTests, WerkbankKuerzelFamilieTests, BdeFaSichtViewTests)
open_questions:
  - "(Gerald) Einordnung: Umsetzung als v1.99.82 im Buendel-Worktree NACH dem Fix-Lauf v1.99.81 – mit ins Prod-Paket (vor dem Prod-Deploy) oder erst nach dem Prod-Deploy? Empfehlung: nach dem Prod-Deploy."
  - "(Gerald) Werkbank-Feld ProductionWorkplaces.BdeDefaultArbeitsgang (hat nach dem Entfernen keinen Leser mehr): A Feld aus der Werkbank-Maske nehmen, Spalte und Daten bleiben (keine Migration) / B Spalte per EF-Migration und SQL-Skript loeschen / C Feld sichtbar lassen. Empfehlung: A."
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

## Ziel / Nutzen (das Warum)

`BdeNurFaMeldung` („BDE: nur FA-Meldung“) steht produktiv auf `false` und wird laut Gerald nicht mehr gebraucht (Backlog, Gerald
2026-11-25; Bezugs-Spec [[2026-11-25-bde-masken-vereinheitlichung-spec]] Antwort 2). Die neue BDE-Seite beachtet die Einstellung seit
v1.99.76 nicht mehr. Die Terminal-Seite mit der NurFA-Oberflaeche ist seit v1.99.78 geloescht. Es bleiben nur noch Alt-Zweige, die
etwas abweisen koennen (Antwort 22, G5 der Bezugs-Spec). Dazu kommt die Vorher-Pruefung `Pruefung/BdeNurFaMeldungAn`, die ein Prod-Update
vor dem Stoppen abbricht, wenn der Wert an ist (Besprechungsvorlage [[2026-11-26-loesungsvorschlaege-deploy-sage-doku]] B-3, Option C).

Nutzen: Es gibt einen Buchungsweg weniger, eine Prod-Sperre weniger und zwei Einstellungen weniger in `/Settings`. Bei `false`
aendert sich das Verhalten nicht (Beleg unter „Technischer Loesungsentwurf“, Abschnitt Verhaltensgleichheit).

## Umfang (In-Scope / Out-of-Scope)

**Reihenfolge (Vorbedingung):** Gebaut wird erst **nach** dem BDE-Masken-Epic. Das Epic ist umgesetzt und steht auf `status: Testbereit`
(Bezugs-Spec Frontmatter Z. 5, Etappe 5 = v1.99.78, Worktree-Stand v1.99.80 laut `IDEALAKEWMSService/AppVersion.cs:5`). Es ist noch nicht
gemergt, Schranke 2 steht aus. Ausserdem kommt dieser Lauf erst **nach** dem geplanten Fix-Lauf v1.99.81
([[2026-11-26-loesungsvorschlaege-bde-ui]] „Fix-Lauf v1.99.81“). Der Fix-Lauf fasst dieselben Dateien an: `BdeTerminalController`
mit Waechtern (B-2/H-A) und dem Loeschen der GETs (F-29) sowie `bde-fa-sicht.js`. Vorlaeufige Version: **v1.99.82** im selben
Buendel-Worktree. Ob das vor oder nach dem Prod-Deploy kommt, klaert Rueckfrage 1.

**In-Scope**
- Key `BdeNurFaMeldung` und alle Lesestellen entfernen; Zweige fuer `nurFa == true` streichen (Liste unten).
- Endpunkt `BdeTerminal/StartProductionForOrder` und `BdeDefaultWorkOperationService` (einziger Aufrufer) loeschen.
- Abhaengige Einstellung `BdeDefaultArbeitsgang` (Katalog `abhaengig: BdeNurFaMeldung`, `AppSettingDefinitions.cs:223-225`) entfernen.
  Ihr einziger Laufzeit-Leser ist `BdeDefaultWorkOperationService.cs:63`, also derselbe tote Weg.
- Werkbank-Feld `BdeDefaultArbeitsgang`: wird nach Rueckfrage 2 behandelt (Empfehlung A).
- DB-Zeilen beider Keys per idempotentem Daten-Skript `SQL/170` loeschen (Muster `SQL/147`). FreshInstall ohne die Seed-Zeilen.
- Manifest: `Pruefung/BdeNurFaMeldungAn` entfernen, `SQL/170` eintragen.
- Hilfe, README, Betriebs- und Anwenderdoku, Testszenarien und Kommentare nachziehen.

**Out-of-Scope**
- Die API-Endpunkte ohne Aufrufer (`available-operations`, `operator/{id}/active-booking`, `operator/{nr}`, `today-history`). Sie werden
  nicht geloescht. Das macht F-29 nach der IIS-Log-Pruefung ([[2026-11-26-loesungsvorschlaege-bde-ui]] F-29, Option A). Hier werden in
  diesen Endpunkten nur die NurFA-Zweige gestrichen; die JSON-Form bleibt bis dahin gleich.
- Bestehende Default-Arbeitsgaenge `OperationNumber = "01"` (`CreatedBy = 'BDE-AutoCreate'`, `BdeDefaultWorkOperationService.cs:37,43`)
  und die Buchungen daran. Sie bleiben als Historie stehen. Der Ausschluss `OperationConfigRepository.DefaultOperationNumber`
  (`OperationConfigRepository.cs:80,92`) bleibt ebenfalls, sonst tauchten vorhandene „01“-Zeilen als unkonfigurierte Arbeitsgaenge auf.
- Die historischen SQL-Skripte `SQL/42_AddBde.sql:223-230` und `SQL/43_AddBdeWerkbankSettings.sql` sowie die historischen Changelog-Eintraege
  (`Views/Help/Changelog.cshtml:73,127,4390`). Sie bleiben unveraendert.
- `docs/superpowers/**` (historische Plaene) und `docs/CLAUDE-full-backup-2026-07.md`, beides Archiv.

## Fachliche Anforderungen

1. In der Web-App gibt es keine Einstellung „BDE: nur FA-Meldung“ und keine Einstellung „BDE: Default-Arbeitsgang“ mehr.
2. Der Buchungsdienst bucht nur noch auf Arbeitsgaenge:
   - Start und Fortsetzen pruefen immer die Kuerzel-Familie der Terminal-Werkbank.
   - An einer Werkbank mit wirksamer Option „Nur AG-Fertigmeldung“ ist Ruesten oder Produktion starten immer gesperrt.
3. Eine Buchung direkt auf den FA (`StartProductionForOrder`) gibt es nicht mehr.
4. Eine noch vorhandene DB-Zeile `BdeNurFaMeldung = true` hat bis zu ihrer Loeschung **keine** Wirkung mehr.
5. Das Update-Skript bricht ein Update nicht mehr wegen `BdeNurFaMeldung` ab. Den Wert vor dem Loeschen schreibt das Protokoll von
   `SQL/170` mit.

## Technischer Loesungsentwurf

**Lesestellen (Grep im Buendel-Worktree am 2026-11-26, alle Wege).** Die Zeilen in der Backlog-Notiz (Stand 2026-11-25) sind teilweise
verschoben, gelten also nicht mehr. Massgeblich sind die Zeilen hier; der Dev-Lauf greppt vor dem Edit neu.

| # | Stelle | Heute | Aenderung |
|---|---|---|---|
| L1 | `Controllers/BdeApiController.cs:93-97,102` (`ResolveScan`) | `nurFa = !faSicht && Setting` | Lesen und Parameter `faSicht` entfernen; `ResolveAsync` ohne `nurFaMode`; Antwortfeld `nurFaMode = false` fest (JSON-Form bleibt bis F-29) |
| L2 | `BdeApiController.cs:126-130,146` (`GetActiveBooking`) | liest Setting | Lesen weg, `nurFaMode = false` fest |
| L3 | `BdeApiController.cs:217-257` (`GetAvailableOperations`) | NurFA-Zweig (offene FAs als `type = "fa"`) | ganzer `if (nurFa)`-Block weg; `nurFaMode = false` bleibt in beiden Rueckgaben (`:214`, `:290`) |
| L4 | `BdeApiController.cs:23,44` | `_settings` nur fuer L1–L3 genutzt (Grep `_settings` = nur `:94,126,217`) | Abhaengigkeit `IAppSettingRepository` aus dem Konstruktor entfernen |
| L5 | `Controllers/BdeTerminalController.cs:53-70` (`StartProductionForOrder`) | einziger Aufrufer von `IBdeDefaultWorkOperationService` (Grep: nur `:21,27`, `Program.cs:175`) | Aktion loeschen; Feld/Parameter `_defaultWoService` weg (`_settings`, `_ctx` bleiben, genutzt in `Finish` `:130-135`, `OtherActiveBookingsAsync` `:202`) |
| L6 | `Services/BdeDefaultWorkOperationService.cs`, `IBdeDefaultWorkOperationService.cs`, `Program.cs:175` | Find-or-Create „01“ | Dateien loeschen, Registrierung weg |
| L7 | `Services/BdeBookingService.cs:522` (`EnsureStartAllowedAsync`) | `NurAgFertigmeldungWirksam(wp, Setting)` | `NurAgFertigmeldungWirksam(wp)` |
| L8 | `BdeBookingService.cs:543-544` (`EnsureWorkOperationInFamilyAsync`) | NurFA prueft die FA-Werkbank | Zweig streichen, nur Familienpruefung; Doc-Kommentar `:527-533` ohne NurFA-Satz. `_settings`/`ReadBoolSettingAsync` bleiben (`:58-59,203-204,312`) |
| L9 | `BdeBookingService.cs:287-288` | `NurAgFertigmeldungWirksam(wp, nurFaGlobal: false)` | Aufruf ohne Parameter, Kommentar kuerzen |
| L10 | `Services/WerkbankKuerzelFamilie.cs:45-47` | Parameter `nurFaGlobal` | Signatur `NurAgFertigmeldungWirksam(ProductionWorkplace w) => w.BdeAktiv && w.BdeNurAgFertigmeldung`; Kommentar `:51` kuerzen |
| L11 | `Controllers/BdeTeilelisteController.cs:79,163` | Kommentar und `nurFaGlobal: false` | Kommentar kuerzen, Aufruf ohne Parameter |
| L12 | `Services/BdeScanResolver.cs:35,56,91,99,108,111,119,128-131,154-192` | `nurFaMode`, `BuildNurFaCandidatesAsync`, `BdeBookTarget.ProductionOrder` | Parameter und NurFA-Methode weg; Enum-Wert `ProductionOrder` weg. `BdeBookTarget.WorkOperation` und das Feld `Target` bleiben, weil `bde-fa-sicht.js:331` auf `target === 'WorkOperation'` filtert. Kommentar `:27-30` (veraltet) kuerzen |
| L13 | `Services/BdeTeilelisteService.cs:555` | Kommentar „ohne BdeNurFaMeldung“ | Kommentar weg |
| L14 | `Controllers/ProductionWorkplacesController.cs:286-289` | `ViewBag.GlobalDefaultArbeitsgang`, `ViewBag.NurFaGlobal` | beide weg; ist `_appSettings` danach ohne Leser (Grep: nur `:286,288`), entfaellt die Abhaengigkeit |
| L15 | `Views/ProductionWorkplaces/_BdeNurAgFertigmeldungField.cshtml:2,10-16` | Warnkasten bei NurFA global | Kasten und Kommentar weg |
| L16 | `Controllers/SettingsController.cs:129-133` | Pflichtregel „Default-Arbeitsgang bei nur FA-Meldung“ | Regel weg |
| L17 | `SettingsController.cs:75,81-86` | Optionen fuer `BdeDefaultArbeitsgang` aus `_operations` | Eintrag weg; `_operations` ist danach ohne Leser (Grep: nur `:23,46,81`), Abhaengigkeit `IOperationConfigRepository` entfernen |
| L18 | `Models/AppSettingDefinitions.cs:221-225` | zwei Katalogeintraege | beide weg. Kein anderer Eintrag nutzt `abhaengig: AppSettingKeys.BdeNurFaMeldung` oder `…BdeDefaultArbeitsgang` (Grep `AbhaengigVon`/`abhaengig:` mit diesen Keys = nur `:225`) |
| L19 | `Models/AppSettingKeys.cs:10-11` | zwei Konstanten | beide weg |
| L20 | `Models/ProductionWorkplace.cs:51` | Doc „ohne Wirkung, solange BdeNurFaMeldung …“ | Satz weg |
| L21 | `Data/Repositories/OperationConfigRepository.cs:79` | Doc „NurFA-Pfad, BdeDefaultWorkOperationService“ | Doc: „Default-AG der frueheren FA-Meldung (bis v1.99.81); Bestand bleibt ausgeschlossen“ |
| L22 | `IDEALAKEWMSService/Services/WorkOperationStructureDetectionService.cs:95` | Kommentar | Satzteil „Bewusst ohne die globale AppSetting BdeNurFaMeldung …“ streichen |
| L23 | `wwwroot/js/bde-fa-sicht.js:326` | `resolve-scan?faSicht=true&…` | `faSicht=true&` entfernen (Server-Parameter faellt mit L1 weg) |
| L24 | Rueckfrage 2 (A): `ProductionWorkplacesController.cs:120-122,172,223-225`, `ProductionWorkplaceEditViewModel.cs:27-28`, `Create.cshtml:80-87`, `Edit.cshtml:121-128`, `ProductionWorkplaceRepository.cs:16` | Werkbank-Feld Default-Arbeitsgang | Feld aus Maske, ViewModel, Mapping und `EditableProperties` entfernen; Entity-Property `ProductionWorkplace.BdeDefaultArbeitsgang` (`:24-25`) bleibt mit Doc „Altlast ohne Leser (v1.99.82), Spalte bleibt“, das EF-Modell bleibt also gleich |

**Negativbelege (kein weiterer Leser).**
- `wwwroot`: kein Treffer fuer `nurFa`, `NurFa`, `nurFaMode`, `BdeDefaultArbeitsgang`, `StartProductionForOrder` (Grep `IdealAkeWms/wwwroot`
  ohne `lib/`). `bde-terminal.js` und `Views/BdeTerminal/` gibt es nicht mehr (Glob leer, geloescht v1.99.78).
- Die Routen `available-operations` und `active-booking` haben im Repo keinen Aufrufer: Grep `api/bde/` findet nur `bde-cockpit.js:46`
  (`cockpit`) und `bde-fa-sicht.js:270,326` (`activities`, `resolve-scan`). Externe Aufrufer lassen sich aus dem Code nicht ausschliessen
  (F-29). Deshalb bleibt die JSON-Form mit `nurFaMode: false`.
- Dienst: einziger Treffer ist der Kommentar L22 (Grep `IDEALAKEWMSService/**/*.cs`). Das Projekt hat keinen `BdeDefaultArbeitsgang`-Leser.
- Projekte im Repo: nur `IdealAkeWms`, `IDEALAKEWMSService` und die zwei Testprojekte (Glob `**/*.csproj`). Kein weiterer Client im Repo.
- `scripts/`: nur `nachlauf.psd1:227-231` (Pruefung) und `:590` (Text des Info-Eintrags `v1.99.32-TeilelisteOption`: „wirkungslos bei
  BdeNurFaMeldung“). Der Info-Eintrag ist historisch (Version 1.99.32) und wird **nicht** geaendert; er erscheint nur bei Updates von
  < 1.99.32.

**Verhaltensgleichheit bei `false` (Prod).** Jede gestrichene Stelle liefert heute bei `false` denselben Pfad wie danach:
- L7: `NurAgFertigmeldungWirksam(wp, false)` = `BdeAktiv && BdeNurAgFertigmeldung` (`WerkbankKuerzelFamilie.cs:46-47`).
- L8: Der Zweig `:543` wird bei `false` uebersprungen.
- L1: Die FA-Sicht schickt `faSicht=true`, also war `nurFa` dort schon `false`.
- L2/L3: Bei `false` laeuft der Normalpfad.
- L5: Bei `false` antwortet die Aktion immer `Invalid` (`:64-65`). Danach antwortet sie 404, und es gibt keinen Aufrufer.
- L14/L15: Der Kasten wird nicht gezeigt.
- L16: Die Regel greift nur bei `true`.
- `ReadBoolSettingAsync` (`BdeBookingService.cs:610-614`) wertet nur `"true"` aus.

Das Verhalten aendert sich nur auf Systemen mit `true`. Laut QA-Notiz der Bezugs-Spec ist das **DEV AKE** („`BdeNurFaMeldung` war bereits
true“, Bezugs-Spec Z. 1263). Dort sind danach Starts an NurAg-Werkbaenken gesperrt, und die Familienpruefung gilt wie an IDEAL.

**Muster:** keine neuen Muster. Fallstricke beachten: Pflegefelder nur ueber `EditableProperties` (`ProductionWorkplaceRepository.cs:12-21`,
Fallstrick §24). Brain-Nachtraege in `architektur/` und `glossar/` nur additiv (README Regel 2): Glossar „AG-Fertigmeldung“ Z. 43, ADR 0030 Z. 24,
ADR 0011 Z. 51 und fallstricke Z. 1313, 1695, 2049 bekommen je einen Nachtrag „seit v1.99.82 entfernt“; nichts wird ueberschrieben.

## Migrations-/SQL-Auswirkungen

- **Keine EF-Migration** (bei Rueckfrage 2 = A). Das EF-Modell bleibt gleich, die Spalte `ProductionWorkplaces.BdeDefaultArbeitsgang` bleibt
  (`SQL/00_FreshInstall.sql:359`, `SQL/43`). Bei B folgt eine eigene Migration mit Drop-Skript (dann `SQL/171` mit `COL_LENGTH`-Guard und
  FreshInstall-Nachzug). Das wird erst nach der Antwort ausgearbeitet.
- **Neu `SQL/170_RemoveAppSettingsBdeNurFaMeldung.sql`** (Nummer vorlaeufig, hoechste im Worktree ist `169`, Glob `SQL/1[6-9][0-9]_*.sql`;
  der Dev-Lauf prueft die naechste freie Nummer, falls v1.99.81 ein Skript belegt). Es ist ein reines Daten-Skript nach dem Muster
  `SQL/147_RemoveAppSettingSageSdataFaAnsichtAktiv.sql`:
  - `SET NOCOUNT ON; GO`, dann `IF EXISTS (… [Key] IN (N'BdeNurFaMeldung', N'BdeDefaultArbeitsgang')) DELETE …; PRINT` mit `@@ROWCOUNT`,
    sonst `PRINT '… nichts zu tun'`.
  - Idempotent, ohne eigene Transaktionssteuerung, ohne `ROLLBACK`, ohne sqlcmd-Direktiven (ADR 0023, Transaktion).
  - Ein `OBJECT_ID`-Guard ist nicht noetig: `dbo.AppSettings` gibt es ab FreshInstall. Kein `__EFMigrationsHistory`-Eintrag.
  - Der Kopf nennt den Warnhinweis „LOESCHT DATEN“ und die Vorher-Abfrage.
- **FreshInstall:** Seed-Zeilen `SQL/00_FreshInstall.sql:1093-1094` entfernen. Danach ist `SQL/170` auf einer frischen DB ein No-op
  („nichts zu tun“).
- **Startup-Seed:** Er legt nur Katalog-Keys mit `seed: true` an (`AppSettingDefinitions.cs:313-314`). Nach L18 legt der Web-Start keine der
  beiden Zeilen neu an. Zwischen Web-Start und `SQL/170` stehen vorhandene Zeilen unter „Nicht im Katalog“ in `/Settings`
  (Muster `BdeTerminalMaxHauptFa`, v1.99.78), ohne Wirkung.

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `BdeNurFaMeldung` (AppSetting) | – | – | – | **entfaellt**: Key, Katalogeintrag, alle Lesestellen; DB-Zeile per `SQL/170` | – | beide | – |
| `BdeDefaultArbeitsgang` (AppSetting) | – | – | – | **entfaellt**: abhaengig von `BdeNurFaMeldung`, einziger Leser war `BdeDefaultWorkOperationService`; DB-Zeile per `SQL/170` | – | beide | – |
| `ProductionWorkplaces.BdeDefaultArbeitsgang` (Werkbank-Feld, kein AppSetting) | – | – | – | Rueckfrage 2 (A: Maskenfeld weg, Spalte bleibt ohne Leser) | – | beide | – |

Keine neue Einstellung.

## Audit-Feld-Auswirkungen

Keine neuen Audit-Felder. `SQL/170` loescht Zeilen, in der Tabelle bleibt also keine Spur. Den Nachweis tragen:
- die `Pruefabfrage` im Manifest. Sie schreibt `Key`, `Value`, `ModifiedAt` und `ModifiedBy` vor dem Lauf ins Protokoll.
- der Marker `dbo.DeployScriptRuns` (ADR 0023).

Werkbank-Speichern setzt `ModifiedAt/By/ByWindows` wie bisher. Bei A schreibt `UpdateAsync` die Spalte `BdeDefaultArbeitsgang` nicht mehr,
der Wert bleibt in der DB unveraendert.

## Akzeptanzkriterien

1. Grep `BdeNurFaMeldung` in `IdealAkeWms/` und `IDEALAKEWMSService/` (`*.cs`, `*.cshtml`, `*.js`, ohne `Migrations/`) trifft nur die
   historischen Eintraege in `Views/Help/Changelog.cshtml` und den neuen Changelog-Eintrag.
2. Grep `BdeDefaultArbeitsgang` im Anwendungscode trifft bei A nur `Migrations/`, `Models/ProductionWorkplace.cs` (Entity-Property mit
   Altlast-Doc) und SQL (FreshInstall-Spalte, `SQL/42`, `SQL/43`, `SQL/170`).
3. `IBdeDefaultWorkOperationService`, `BdeDefaultWorkOperationService` und `BdeTerminalController.StartProductionForOrder` gibt es nicht
   mehr. Ein POST auf `/BdeTerminal/StartProductionForOrder` liefert 404.
4. `/Settings`, Gruppe BDE: keine Zeilen „BDE: nur FA-Meldung“ und „BDE: Default-Arbeitsgang“. Nach `SQL/170` steht unter „Nicht im
   Katalog“ keine der beiden Zeilen.
5. **Negativ:** Steht die DB-Zeile `BdeNurFaMeldung = true` noch da (vor `SQL/170` oder von Hand gesetzt), hat das keine Wirkung:
   a) Ruesten oder Produktion starten an einer Werkbank mit „Nur AG-Fertigmeldung“ wird mit „An dieser Werkbank wird nur fertig gemeldet.“
      abgewiesen.
   b) Start eines AG der Kuerzel-Familie, dessen FA an einer anderen Werkbank haengt, ist erlaubt.
   c) `resolve-scan` liefert Arbeitsgang-Kandidaten mit `nurFaMode: false`.
   d) `available-operations` liefert Arbeitsgaenge und Taetigkeiten mit `nurFaMode: false`, keine `type: "fa"`.
   e) `active-booking` liefert `nurFaMode: false`.
6. Die FA-Sicht (beide Standorte) scannt ohne `faSicht`-Parameter und loest wie bisher auf (Exact markiert, Ambiguous zeigt die Auswahl,
   Running/NotInScope/NotFound zeigen eine Meldung).
7. Werkbank Anlegen/Bearbeiten zeigt keinen NurFA-Warnkasten. Bei A gibt es kein Feld „Default-Arbeitsgang (BDE)“, und Speichern laesst
   den DB-Wert `ProductionWorkplaces.BdeDefaultArbeitsgang` unveraendert (Vorher/Nachher-Abfrage gleich).
8. Speichern in `/Settings` hat keine Pflichtregel zum Default-Arbeitsgang mehr. Die Regel „FE-Termin ≥ Schachteltermin“ lehnt weiter ab
   und behaelt die Eingaben (Ersatz fuer `Save_Abgelehnt_SeiteMitEingaben_QA_B1`).
9. `SQL/170` laeuft auf DEV AKE und DEV IDEAL je zweimal ohne Fehler. Der erste Lauf meldet die geloeschten Zeilen, der zweite „nichts zu
   tun“. Die Spur-Abfrage liefert danach 1. Eine FreshInstall-DB hat keine der beiden Zeilen, `SQL/170` meldet dort „nichts zu tun“.
10. `scripts/deploy/nachlauf.psd1` enthaelt keinen Eintrag `Pruefung/BdeNurFaMeldungAn`, aber den Eintrag `SQL/170` (siehe Deploy).
    `NachlaufManifestDriftTests` ist gruen. `Update-IdealAkeWms.ps1 -NurPruefen` auf DEV listet `BdeNurFaMeldungAn` nicht mehr.
11. Jede Angabe `nachlauf.psd1:<Zeile>` in `docs/betrieb/*.md` zeigt nach dem Edit auf den genannten Eintrag. Durch das Entfernen von
    `:227-231` und das Einfuegen von `SQL/170` verschieben sich die Zeilen dahinter (Grep `nachlauf.psd1:` in `docs/betrieb`). Das schliesst
    den Nebenbefund B-3 zu `03-installation-update-rollback.md:215` ein, falls v1.99.81 ihn noch nicht korrigiert hat.
12. Bestehende „01“-Arbeitsgaenge und Buchungen daran bleiben sichtbar und bedienbar: BDE-Buchungen, FA-Struktur und Fertig melden einer
    laufenden Buchung. Nichts wird geloescht.
13. Hilfe (`Views/Help/Index.cshtml:1907-1912,2059,2142-2143`), README (`:436,499,500`), `docs/anwender/17:24`, `docs/betrieb/00-index`
    (neue Zeile), `03:215,393`, `04:235`, `07:102` und `AGENTS.md:61` erwaehnen die Einstellung nicht mehr als bedienbar. Wo es noetig ist,
    steht „entfernt mit v1.99.82“. Die Changelog-Seite hat einen Eintrag fuer v1.99.82.
14. Build 0 Fehler, Tests Web und Dienst gruen. `AppSettingDefinitionsTests` hat Zaehlungen −2 und keine Paare `BdeNurFaMeldung` /
    `BdeDefaultArbeitsgang` (`:23`). Die Version steht in beiden `AppVersion.cs`.

## Test-Szenarien

**Neu TS-202** — `BdeNurFaMeldung` entfernt (v1.99.82). Die Nummer ist vorlaeufig: Belegt der Fix-Lauf v1.99.81 TS-202, gilt die naechste
freie Nummer laut `secondbrain/tests/testszenarien-index.md`. Dazu kommt eine Indexzeile.
- **202.1 Einstellungen (beide DEV):** `/Settings` › BDE ohne die zwei Zeilen. Vor `SQL/170` stehen sie unter „Nicht im Katalog“
  (DEV AKE: Wert `true`). Danach sind sie weg.
- **202.2 Negativ Start an NurAg-Werkbank (DEV AKE, Zeile vorher `true`):** Werkbank mit „Nur AG-Fertigmeldung“, POST `StartProduction`
  aus der Browser-Konsole. Erwartung: Abweisung „An dieser Werkbank wird nur fertig gemeldet.“ (vorher erlaubt).
- **202.3 Negativ Familie (DEV IDEAL):** Zeile per SQL auf `true` setzen. AG der Kuerzel-Familie an der Terminal-Werkbank, FA an anderer
  Werkbank. Erwartung: Start geht. Danach `SQL/170` laufen lassen.
- **202.4 Negativ Endpunkt:** POST `/BdeTerminal/StartProductionForOrder` mit Antiforgery-Token aus der Seite. Erwartung: 404, in
  `WorkOperations` keine neue Zeile `OperationNumber = '01'`.
- **202.5 FA-Sicht-Scan (beide):** FA-Scan Exact, Ambiguous, Running, NotFound wie in TS-197/198 (Netzwerk-Tab: URL ohne `faSicht`).
- **202.6 Werkbank-Maske:** kein Warnkasten. Bei A kein Feld Default-Arbeitsgang; Speichern laesst den Spaltenwert gleich (Abfrage unten).
- **202.7 SQL/170 zweimal und FreshInstall** (Wegwerf-DB), Spur = 1.
- **202.8 Update-Skript `-NurPruefen` (DEV):** keine Zeile `BdeNurFaMeldungAn`.
- **202.9 Bestand „01“:** eine vorhandene „01“-Buchung (DEV AKE) in BDE-Buchungen und FA-Struktur sichtbar.

**Bestehende Szenarien als ueberholt markieren** (nicht loeschen), je mit „ueberholt v1.99.82, siehe TS-202“:
- TS-3.11 (`docs/TESTSZENARIEN.md:998-1012`), TS-8.14 (`:2763-2768`) und der Abschnitt bei `:2844`
- TS-90.9 (`:9858`), TS-97.10 (`:10350`), TS-152.2 Teil „`BdeNurFaMeldung` an“ (`:12790`)
- TS-197.6 (`:14402-14404`) sowie die Negativschritte `:14446`, `:14477`
- Vorbedingungen mit „`BdeNurFaMeldung` aus“ (`:10272-10311`, `:12781`, `:9819-9822`) bleiben stehen. Sie bekommen nur einen
  Klammerhinweis „(Einstellung entfernt ab v1.99.82)“.

**Unit-Tests (Dev-Lauf).**
- Loeschen: `BdeDefaultWorkOperationServiceTests`, `BdeTerminalControllerTests.StartProductionForOrder_*` (`:318-349`),
  `BdeApiControllerTests.GetAvailableOperations_NurFaMode_ExcludesPackedOrders` (`:471`), `ResolveScan_NurFaMode_ReturnsProductionOrderTarget`
  (`:627`), `GetAvailableOperations_NurFa_AusgabeUnveraendert_OhneStatus_AK8` (`:744`), `BdeScanResolverTests.NurFa_*` (`:260-315`),
  `BdeBookingServiceStufe1Tests.NurFa_ChecksOrderWorkplace_NotAgWorkplace` (`:158`), `SettingsControllerKatalogTests.Save_NurFaMeldungOhneDefaultArbeitsgang_Abgelehnt_AK13` (`:136`).
- Anpassen: Setups ohne `BdeNurFaMeldung`-Mock (`BdeApiControllerTests`, `BdeTerminalControllerTests:17-36`,
  `BdeBookingServiceStufe1Tests:22-25,183-187`, `BdeTeilelisteMeldenTests:17,34,160-163,216`); `ResolveAsync` ohne `nurFaMode`;
  `WerkbankKuerzelFamilieTests:67-71` ohne `nurFa`-Spalte; `ResolveScan_FaSicht_IgnoriertNurFaMeldung` (`:654`) wird zu einem Test „Scan
  ohne Parameter loest auf Arbeitsgaenge auf“; `NurFaGlobal_ReportCompletionGateIgnoriertNurFa_StartAllowed_AK32`
  (`BdeBookingServiceStufe1Tests:319`) ohne NurFA-Teil; `Save_Abgelehnt_SeiteMitEingaben_QA_B1` mit der FE-/Schachteltermin-Regel;
  `SettingsControllerKatalogTests:83` ohne `BdeDefaultArbeitsgang`; `BdeFaSichtViewTests:38` prueft den `resolve-scan`-Aufruf ohne `faSicht`.
- Neu (klein): `BdeBookingServiceStufe1Tests`: Familienpruefung und Startsperre an einer NurAg-Werkbank, auch wenn das Settings-Mock
  `BdeNurFaMeldung = "true"` liefert (beweist AK 5a/5b).

## Etappen (nur bei epic: true)

Entfaellt (`epic: false`). Ein Dev-Lauf, ein Commit. Danach QA wie im Buendel.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfaellt | – | – |

## Deploy

> [!warning] Vorlaeufig, der qa-agent bestaetigt das gegen den echten Diff.

- **Web-App:** ja (Controller, Services, Views, JS, Katalog).
- **Service:** ja, nur `AppVersion.cs` und ein Kommentar (Buendel-Regel: Version in beiden `AppVersion.cs`). Es aendert sich keine Logik,
  bei A auch nicht das geteilte EF-Modell.
- **Migration:** nein (bei Rueckfrage 2 = A). Daten-Skript `SQL/170` ueber das Manifest.
- **Manifest-Eintraege** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023):
  - **Entfernen:** `Pruefung/BdeNurFaMeldungAn` (`nachlauf.psd1:227-231`, inkl. Kommentarzeile `:227`).
    - Begruendung: Pruefungen laufen bei jedem Update unabhaengig von der Version (`Update-IdealAkeWms.ps1:699`, kein `Test-Faellig`). Ihr
      Grund, die Abweisung im Buchungsdienst (Antwort 22), faellt mit L7/L8 weg.
    - Das neue Paket bringt sein Manifest mit. Damit sperrt beim Update auf v1.99.82 keine Vorher-Pruefung mehr, auch nicht bei `true`.
    - Den Wert protokolliert die `Pruefabfrage` von `SQL/170`.
  - **Neu:**
    `@{ Id='SQL/170'; Version='1.99.82'; Typ='Daten'; Firma='beide'; Phase='VorStart'; Ausfuehrung='Transaktion'`
    `Datei='170_RemoveAppSettingsBdeNurFaMeldung.sql'`
    `Pruefabfrage='SELECT [Key],[Value],ModifiedAt,ModifiedBy FROM [dbo].[AppSettings] WHERE [Key] IN (N''BdeNurFaMeldung'', N''BdeDefaultArbeitsgang'')'`
    `Spur='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] IN (N''BdeNurFaMeldung'', N''BdeDefaultArbeitsgang'')) THEN 0 ELSE 1 END'`
    `Text='Loescht die entfallenen Einstellungen BdeNurFaMeldung und BdeDefaultArbeitsgang (idempotent; Werte oben stehen im Protokoll); in einer Transaktion, danach uebernehmen/verwerfen. Spur 1 = nichts zu tun.'`
    `Quelle='<changelog-slug v1.99.82>' }`
    - Einzufuegen in den Block der Daten-Eintraege nach `SQL/147` (`:354-359`). `Daten` steht nicht in der Schema-Reihenfolge.
    - Kein Trockenlauf-Skript, also keine `TREFFER`-Pflicht.
  - Keine Einstellung, kein Leitstand-Schritt, kein Ordner.
  - Info-Eintrag: nur wenn der Mensch das will. Die Anwender sehen keine Aenderung, deshalb wird keiner vorgeschlagen.
- **Pruefabfragen vor dem Deploy (Mensch, WMS-DB je Zielsystem; nicht verifiziert, Agenten haben keinen DB-Zugang in diesem Lauf):**
  1. `SELECT [Key],[Value],ModifiedAt,ModifiedBy FROM dbo.AppSettings WHERE [Key] IN (N'BdeNurFaMeldung', N'BdeDefaultArbeitsgang');`
     Erwartung Prod: `BdeNurFaMeldung = false` (Gerald 2026-11-25). DEV AKE: `true` (QA 2026-11-25).
  2. Zu Rueckfrage 2: `SELECT Id, Name, BdeDefaultArbeitsgang FROM dbo.ProductionWorkplaces WHERE NULLIF(LTRIM(RTRIM(BdeDefaultArbeitsgang)), N'') IS NOT NULL;`
  3. Info: `SELECT COUNT(*) FROM dbo.WorkOperations WHERE OperationNumber = N'01' AND CreatedBy = N'BDE-AutoCreate';` (Bestand, bleibt).
- **Publish-Befehle (im Worktree):**
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
  Danach das Update-Skript je DEV-System (AKE und IDEAL). `SQL/170` laeuft in Phase VorStart bei gestopptem Web und Dienst.

## Offene Rueckfragen

1. (Gerald) **Einordnung.** Gebaut wird als v1.99.82 im Buendel-Worktree, nach dem Fix-Lauf v1.99.81. Soll das mit ins Prod-Paket (vor
   dem Prod-Deploy) oder erst nach dem Prod-Deploy kommen?
   - **Empfehlung: nach dem Prod-Deploy.** Prod steht auf `false`, die Aenderung bringt dort kein anderes Verhalten.
   - Die Prod-Sperre aus B-3 deckt Option A (`-NurPruefen` vorab) schon ab.
   - Der Lauf fasst den Buchungsdienst an. Das waere ein zusaetzliches Risiko kurz vor dem Fenster.
2. (Gerald) **Werkbank-Feld `ProductionWorkplaces.BdeDefaultArbeitsgang`.** Nach dem Entfernen liest es niemand mehr. Es wird heute in der
   Werkbank-Maske gepflegt (`Edit.cshtml:121-128`, `Create.cshtml:80-87`).
   - **A (empfohlen):** Feld aus Maske, ViewModel und `EditableProperties` nehmen. Spalte und Daten bleiben, keine Migration.
   - **B:** Spalte per EF-Migration und SQL-Skript (`SQL/171`) loeschen.
   - **C:** Feld sichtbar lassen (nicht empfohlen, weil es dann ohne Wirkung waere).
   - Pruefabfrage 2 im Deploy-Abschnitt zeigt, ob Werte gepflegt sind.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

Keine Fachbereichsfragen. Ob der Fachbereich die Einstellung auf Prod noch nutzt, fragt schon B-3 in
[[2026-11-26-loesungsvorschlaege-deploy-sage-doku]] ab.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. Einordnung v1.99.82: vor oder nach dem Prod-Deploy? →
2. Werkbank-Feld `BdeDefaultArbeitsgang`: A / B / C? →
