---
type: spec
title: Einstellungen als typisierter Katalog - Auswahlfelder, Wirkung/Verwendung, Serverprüfung (AppSettings + ServiceSettings)
slug: 2026-11-07-einstellungen-auswahlfelder-und-beschreibung-spec
status: Testbereit
created: 2026-11-07
updated: 2026-11-07
source_backlog: "[[2026-11-07-einstellungen-auswahlfelder-und-beschreibung]]"
buendel: "[[2026-11-07-buendel-uebersicht]]"
task: "[[2026-11-07-buendel-2026-11-07-goal-lauf]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
version: "1.77.0"
sql_block: "110-113 reserviert, voraussichtlich ungenutzt (keine Schema-Änderung)"
affected_code:
  - "Code-Stand aller Fundstellen: Bündel-Worktree .claude/worktrees/2026-08-07-ideal-teile-1-5, v1.73.0 (IdealAkeWms/AppVersion.cs:5), am 2026-11-07 nachgeprüft"
  - "Katalog Service: IdealAkeWms/Models/ServiceSettingDefinition.cs:4-9 (enum Bool/Int/String), :25-31 (record Key/Type/DefaultValue/Category/Description/Multiline); ServiceSettingDefinitions.cs:16-117 (All), :68-73 (Sync:Quelle:* als String), :120-124 (TryGet)"
  - "Katalog App (heute keiner): IdealAkeWms/Models/AppSettingKeys.cs:6-119 (Konstanten), :40-45 (FesteWerte, v1.69.0); AppSetting.cs:5-20 (Key/Value/Description, KEIN AuditableEntity); ApplicationDbContext.cs:781-788 (Value NVARCHAR(500) NOT NULL)"
  - "Seeds AppSettings: IdealAkeWms/Program.cs:289-488 (acht Tupel-Blöcke, nur wenn Zeile fehlt); SQL/00_FreshInstall.sql:1045-1078 (Standardzeilen inkl. KommissionierTage/VorkommissionierTage/BeschichtungTage/WarningThresholdPercent/CriticalThresholdPercent/NegativeBuchungLagerplatz/BeschichtungAbholtage, die Program.cs NICHT seedet); Seed ServiceSettings Program.cs:492-505"
  - "/Settings: Controllers/SettingsController.cs:10 ([RequireAdminAccess]), :30-38 (Index), :42-65 (SaveSettings: FesteWerte-Prüfung :46-56, schreibt JEDEN geposteten Key :58-61); Views/Settings/Index.cshtml:5 (canEdit = HasMasterDataAccessAsync), :35-47 (Gruppen hart verdrahtet), :49 (Rest = Sonstige), :62-64 (Bool-Erkennung am WERT), :82-95 (Dropdown FesteWerte, ungültiger Wert sichtbar :87-90), :257-275 (JS BdeDefaultArbeitsgang nur bei BdeNurFaMeldung)"
  - "Repository: Data/Repositories/AppSettingRepository.cs:15-47 (GetAll/GetValue/GetIntValue/SetValue, null->''), CachedSettingRepository.cs:13 (CachePrefix), :38-42 (Invalidierung)"
  - "/ServiceSettings: Controllers/ServiceSettingsController.cs:16 ([RequireAdminAccess]), :41-99 (SaveSettings: schreibt Key für Key, Int-Fehler -> continue = TEILSPEICHERUNG :64-69), :177-212 (BuildViewModel Katalog∪DB, Orphans :207-209); Views/ServiceSettings/Index.cshtml:7-8 (artikelView), :59 (Key als Label), :64-78 (Sperrhinweis SageQuelleSperre), :79-85 (TLS-Warnung), :86-96 (Master read-only), :97-104 (Bool checked nur bei 'true'), :251-253 (Verbindungstest liest input[name=settings[...]]); Models/ViewModels/ServiceSettingsViewModel.cs:7-31"
  - "Sperrregel: IdealAkeWms/Services/SageSdata/SageQuelleSperre.cs:9-24 (Grund je Bereich); Dienst-Parse IDEALAKEWMSService/Services/Sdata/SageQuelleSchalter.cs:9 (enum View/SData/Vergleich), :33-40 (ordinal, ohne Trim, Unbekannt = View + Warnung)"
  - "Dienst-Lesesemantik: IDEALAKEWMSService/Common/ServiceSettings.cs:31-36 (Bool: leer = Default, sonst nur 'true'), :38-43 (Int: unparsebar = Default)"
  - "Kuratierte Maske (nur Drift-Abgleich, keine Umgestaltung): Models/Standort/StandortSettingsCatalog.cs:6-15 (StandortField Label/Hint/Min/Max/EmptyResetsToDefault), :41-102; Controllers/StandortEinstellungenController.cs:52-103"
  - "Drift-Guard Vorlage: IdealAkeWms.Tests/Models/ServiceSettingDefinitionsTests.cs:10-60, :81-144; Controller-Tests IdealAkeWms.Tests/Controllers/ServiceSettingsControllerTests.cs, SettingsControllerQrErstesFeldTests.cs"
  - "Lesestellen AppSettings: siehe Inventurtabelle im Abschnitt Einstellungen (je Key Datei:Zeile)"
  - "Doku: README.md (AppSettings-Tabelle), Views/Help/Index.cshtml:124-125 (nennt QrErstesFeld/QrTrennzeichen als Key), Views/Help/Changelog.cshtml, docs/TESTSZENARIEN.md (letzte TS-106 :10712), beide AppVersion.cs"
  - "NEU (Pfade festgelegt): IdealAkeWms/Models/AppSettingDefinitions.cs, IdealAkeWms/Services/Settings/SettingValidator.cs, IdealAkeWms/Views/Shared/_SettingField.cshtml, IdealAkeWms.Tests/Models/AppSettingDefinitionsTests.cs, IdealAkeWms.Tests/Services/SettingValidatorTests.cs"
open_questions:
  - "1 Lese-Warnung (Konflikt ADR 0020 Punkt 2 vs. Backlog-Grenze 'gleiche Lesesemantik'): ADR verlangt beim LESEN eines ungültigen Bestandswerts Default + Protokollwarnung. Dafür müssten rund 100 Lesestellen (Web) und die Dienst-Leser umgebaut werden. Reicht eine zentrale Prüfung beim Start der Web-App (eine Warnzeile je ungültigem Wert im Aktivitäts-Protokoll) plus Kennzeichnung auf beiden Seiten, Lesestellen unverändert? (Empfehlung: ja, Etappe 4b baut das; die Lesestellen nehmen schon heute ihren Default.)"
  - "2 BdeDefaultArbeitsgang ist laut Code KEIN Stammdatenverweis: der Wert wird als Name eines neu angelegten Arbeitsgangs verwendet (BdeDefaultWorkOperationService.cs:34-38, :63-65), Beispiel im Seed 'PRODUKTION' (Program.cs:422). Das Backlog nennt ihn Stammdaten-Auswahl. Freitext mit Vorschlagsliste aus den Arbeitsgang-Stammdaten (keine Pflicht-Existenz)? (Empfehlung: ja - eine Pflicht-Auswahl würde heute gültige Werte abweisen.)"
  - "3 OseonRueckmeldungAktiv und SageRueckmeldungAktiv haben keinen Leser im Code (nur Seeds Program.cs:292-293, SQL/27_AddWorkOperationsPhase1.sql:213-225, SQL/00_FreshInstall.sql:1055-1056; Grep über Web, Dienst, SQL, JS). Im Katalog mit Kennzeichnung 'derzeit ohne Wirkung' führen und das Aufräumen als Aufgabe anlegen? (Empfehlung: ja, in diesem Epic nichts löschen.)"
epic: true
etappen:
  - "E1 Inventur + Katalog AppSettings (AppSettingDefinitions, erweiterter Definitions-Record, Seed aus Katalog, keine sichtbare Änderung)"
  - "E2 Darstellung /Settings aus dem Katalog (gemeinsames Feld-Partial, Gruppen, Wirkung/Verwendung/Default/Standort/Warnhinweis, Suche, 'weicht ab')"
  - "E3 Darstellung /ServiceSettings (dasselbe Partial, Metadaten aller Service-Keys, Sync:Quelle:* als Auswahl, Sperrhinweis bleibt)"
  - "E4a Serverprüfung beim Speichern (beide Seiten, ganz oder gar nicht) + Drift-Guard-Tests"
  - "E4b Bestandsprüfung beim Web-Start ins Aktivitäts-Protokoll (hängt an Rückfrage 1, baut nach Empfehlung)"
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-07"
---

# Einstellungen: Auswahlfelder, Wirkung und Verwendung je Einstellung (Epic, v1.77.0)

Regel: [[0020-einstellungen-mit-festem-wertebereich-als-auswahl-und-mit-wirkungsbeschreibung]], ergänzt
[[0008-servicesettings-db-first-mit-typisiertem-katalog]] und [[0011-feature-toggles-ueber-appsettings]].
Bündel: [[2026-11-07-buendel-uebersicht]]. Teilbezug: [[2026-11-06-sage-sdata-stammdaten-sync-spec]], Abschnitt
„Einstellungen (Nachtrag 2026-11-07, ADR 0020)“. Die dort als „bewusste Lücke“ vermerkte Auswahlliste der Quellenschalter wird hier in Etappe E3
umgesetzt; Werte, Defaults und Wirkung kommen unverändert von dort.

## Ziel / Nutzen (das Warum)

Gerald will an beiden Einstellungsorten Auswahllisten statt Freitext, weil das Tippfehler verhindert. Außerdem soll jede Einstellung
erklären, was sie bewirkt und wo sie wirkt. Heute ist das nicht so (Belege in `affected_code`):
- `/Settings` hat **keinen Katalog**. Ob ein Feld ein Schalter ist, entscheidet der **gespeicherte Wert** (`Views/Settings/Index.cshtml:62-64`).
  Die Gruppen sind im View fest verdrahtet (:35-47). Einstellungen ohne DB-Zeile fehlen auf der Seite ganz, zum Beispiel
  `HardwareScannerAktiv`, `SageSdataFaAnsichtAktiv` und `LagerplatzEtikettRolle93x55`. Beim Speichern wird jeder gepostete Key geschrieben,
  auch ein unbekannter (`SettingsController.cs:58-61`).
- `/ServiceSettings` zeigt den technischen Key als Überschrift (`Index.cshtml:59`). Die Quellenschalter `Sync:Quelle:*` sind Freitext.
  Ein Int-Fehler beim Speichern führt zu einer **Teilspeicherung**: Die übrigen Keys sind schon geschrieben (`ServiceSettingsController.cs:45-76`).
- **Zusatzbefund** (wird in E3 mitbehoben, weil das Partial ihn sonst übernähme): Ein Bool-Schalter mit Default `true`, dessen DB-Wert leer ist,
  wirkt im Dienst als `true` (`ServiceSettings.cs:34`). Die Seite zeigt ihn aber als „Deaktiviert“ (`Index.cshtml:99`). Das nächste Speichern
  schreibt dann `false`, ohne dass jemand den Schalter angefasst hat.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- Typisierter Katalog `AppSettingDefinitions.All` für alle AppSettings, die der Code liest (Inventur unten). Der Definitions-Record wird um die
  ADR-0020-Pflichtangaben erweitert und von beiden Katalogen genutzt.
- `/Settings` und `/ServiceSettings` werden aus dem Katalog dargestellt und nutzen dasselbe Feld-Partial: Bedienelement nach Typ, Anzeigename,
  Gruppe, „Bewirkt“, „Verwendet in“, Default, Standort, Warnhinweis, Abhängigkeit, Suche und Kennzeichnung „weicht vom Standard ab“.
- Serverprüfung beim Speichern gegen den Katalog, nach dem Prinzip „ganz oder gar nicht“, auf beiden Seiten.
- Drift-Guard-Tests für beide Kataloge und den Abgleich mit `StandortSettingsCatalog`.
- Bestandsprüfung beim Web-Start (E4b, siehe Rückfrage 1).

**Out-of-Scope**
- Lesestellen und Lesesemantik bleiben unverändert: gleiche Keys, gleiche Defaults, `LagerbestellungAktiv` sperrt weiterhin nur bei `"false"`.
  Literal-Keys wie `"KommissionierTage"` werden **nicht** auf Konstanten umgestellt; der Drift-Guard deckt sie über eine feste Liste ab.
- `/StandortEinstellungen` wird nicht umgebaut. Geprüft wird nur, dass jedes Feld im Katalog steht und den gleichen Typ hat (AK 17). Die Maske
  hat kein Feld mit festem Wertebereich (`StandortSettingsCatalog.cs:41-102`).
- Der Master `ProduktionsauftragHierarchisch` bleibt read-only (ADR 0012). Geheimnisse bleiben in appsettings (ADR 0008).
- Keine Schema-Änderung. Die Metadaten stehen im Code, nicht in der DB.
- Die Spec-Vorlage `_templates/spec.md` hat den Abschnitt „Einstellungen“ bereits (Zeilen 40-45). Der Backlog-Punkt ist damit erledigt, hier ist
  nichts zu tun.

## Fachliche Anforderungen

**F1 Typ bestimmt das Bedienelement (ADR 0020 Punkt 1).** Die Typen sind:
- `Bool`: Schalter. Gezeigt wird der **wirksame** Zustand nach der Lesart des Keys (F6).
- `Int`: Zahlenfeld mit `Min`/`Max`, falls belegt, und Einheit.
- `String`: Textfeld; mehrzeilig, wenn `Multiline`.
- `Choice`: Dropdown mit festen Werten, je Wert ein Anzeigetext.
- `MultiChoice`: Checkbox-Gruppe mit festen Werten, gespeichert kommagetrennt in Katalog-Reihenfolge.
- `MasterData`: Dropdown aus den Stammdaten, gespeichert wird der Schlüssel. Bei großen Mengen (Lagerplätze) ist es ein Textfeld mit
  Vorschlagsliste (`<datalist>`) und Serverprüfung „existiert“.

**Grenzen nur, wo der Leser sie hat.** Eine Zahlgrenze kommt nur in den Katalog, wenn der Lesecode sie schon kennt (Clamp, Untergrenze,
Sonderwert). Erfundene Grenzen würden heute wirksame Werte abweisen.

**F2 Pflichtangaben je Key (ADR 0020 Punkt 3):**
- Anzeigename (deutsch, ohne Key-Syntax)
- Gruppe
- `Description` als „Bewirkt“, bei Auswahl inklusive der Wirkung jedes Werts
- `Verwendung` (Modul, Seite oder Dienst-Lauf)
- Default
- Standort (`AKE` | `IDEAL` | `beide`)
- Warnhinweis bei Außenwirkung oder Einweg-Charakter, sonst leer
- `AbhaengigVon` (Key), falls die Einstellung nur zusammen mit einem anderen Schalter wirkt

Der technische Key bleibt sichtbar, aber klein und gedämpft.

**F3 Übersicht (ADR 0020 Punkt 4):**
- Gruppen in Katalog-Reihenfolge.
- Ein Suchfeld oben filtert Zeilen clientseitig nach Anzeigename, Key und Beschreibung. Ein Schalter „Nur geänderte“ zeigt nur Werte, die vom
  Default abweichen.
- Abweichende Werte tragen ein Badge „weicht vom Standard ab (Standard: X)“, Text plus Farbe.
- Ungültige Bestandswerte tragen ein Badge „ungültig“ und den Rohwert. Oben steht ein Sammelbanner mit der Anzahl.
- Ein Warnhinweis erscheint als gelbe Hinweiszeile (Bootstrap-Alert, nicht nur Farbe).
- Abhängige Felder bleiben sichtbar und sind als „wirkt nur, wenn … an“ beschriftet. Das bestehende JS, das `BdeDefaultArbeitsgang` ausblendet
  (`Settings/Index.cshtml:257-265`), entfällt zugunsten dieser Beschriftung. Die Pflichtprüfung „bei NurFA kein leerer Default-Arbeitsgang“
  (:267-275) wandert in den Server (E4a, AK 13).

**F4 Speichern ohne Nebenwirkung.**
- Hat ein Key **keine DB-Zeile** und entspricht der gepostete Wert dem Katalog-Default, wird nichts geschrieben. So entstehen beim ersten
  Speichern keine neuen Zeilen.
- Unverändert gepostete Werte vorhandener Zeilen werden nicht neu geschrieben. Gilt für beide Seiten.
- Grund: Der Default im Katalog ist der Default des Lesers bei **fehlender** Zeile. Für manche Keys wirkt `""` anders als „Zeile fehlt“:
  - `BeschichtungAbholtage`: `null` ergibt „Dienstag,Donnerstag“ (`FaWorklistController.cs:201`), `""` ergibt keine Tage
    (`BusinessDayService.cs:88-89`).
  - `NegativeBuchungLagerplatz`: `null` ergibt „NAN“ (`StockMovementsController.cs:327`).
  - Vorbild für die Regel: [[fallstricke]], „Default-an-Schalter … brauchen eine geseedete Zeile“.

**F5 Serverprüfung (ADR 0020 Punkt 2, E4a).**
- Eine Funktion `SettingValidator.Validate(definition, rohwert)` liefert entweder den kanonischen Wert oder eine deutsche Fehlermeldung mit
  Anzeigename und den erlaubten Werten.
- Beide Controller prüfen **alle** geposteten Katalog-Keys, **bevor** sie schreiben. Bei einem Fehler wird nichts geschrieben. Die Seite kommt
  mit den eingegebenen Werten und einem Banner „Nicht gespeichert“ zurück, wie bei `StandortEinstellungenController.Save` (:105-108).
- `/Settings` verarbeitet nur Katalog-Keys (Allow-List wie `ServiceSettingsController.cs:47-48`). Ein unbekannter geposteter Key wird ignoriert.
- Kanonisierung:
  - Bool: `"true"`/`"false"`.
  - Int: `InvariantCulture`, getrimmt.
  - Choice: kanonische Schreibweise aus dem Katalog. Für `Sync:Quelle:*` muss der Wert exakt passen, weil der Dienst ordinal und ohne Trim
    liest (`SageQuelleSchalter.cs:33-40`). Das Dropdown sendet ohnehin den kanonischen Wert.
  - MultiChoice: Teile getrimmt, Katalog-Reihenfolge, unbekannter Teil = Fehler.
  - MasterData: Schlüssel muss existieren; leer ist erlaubt, wenn der Katalog `LeerErlaubt` setzt.

**F6 Bool-Lesart im Katalog.** Jeder Bool trägt seine Lesart, damit das Partial den wirksamen Zustand zeigt und Speichern nichts umkippt:
- `NurTrueAktiviert`: Muster `?.Equals("true") == true`. Gilt für die meisten AppSettings.
- `NurFalseSperrt`: `LagerbestellungAktiv` (`RequireLagerbestellungAktivAttribute.cs:33`), `VaultLinksAktiv`, `EnaioLinksAktiv` (Seed-Kommentar
  `Program.cs:375`, `AppSettingKeys.cs:107-110`).
- `LeerIstDefault`: alle ServiceSettings-Bools, Lesart `ServiceSettings.cs:34-35`.

Ein ungültiger Bool-Rohwert, etwa `"1"`, wird mit dem wirksamen Zustand und dem Badge „ungültig“ gezeigt. Speichern schreibt den kanonischen
Wert dieses wirksamen Zustands, das Verhalten bleibt also gleich.

**F7 Bestandsprüfung (E4b, nach Rückfrage 1).**
- Nach dem Seed in `Program.cs` prüft die Web-App einmal alle vorhandenen Zeilen beider Tabellen gegen den Katalog.
- Je ungültigem Wert entsteht eine Warnzeile in einem Lauf des neuen Protokoll-Dienstes `Einstellungspruefung`. Deutscher Anzeigename
  „Einstellungsprüfung“; Eintrag in `SyncLogServices.All` und `DisplayNames` (ADR 0010).
- Gibt es keine ungültigen Werte, wird kein Lauf geschrieben. Lesestellen bleiben unverändert.

## Technischer Lösungsentwurf

Leitern-Entscheidungen (ponytail): Der vorhandene Katalog-Record wird wiederverwendet, das vorhandene ViewModel verallgemeinert und das
vorhandene Dropdown-Muster für ungültige Werte (`Settings/Index.cshtml:87-90`) übernommen. Es gibt keine neue DB-Spalte, keine
UI-Bibliothek und keinen Umbau der Lesestellen.

1. **Record und Typ** (`ServiceSettingDefinition.cs`):
   - Umbenennen in `SettingDefinition` / `SettingType`. Das ist mechanisch: 9 Dateien, alle im Web- und Testprojekt; der Dienst referenziert die
     Namen nicht (Grep `ServiceSettingType|ServiceSettingDefinition\b`).
   - Grund: Ein AppSetting, das als „ServiceSettingDefinition“ beschrieben ist, wäre eine neue Falle.
   - Neue **optionale** Parameter, damit alle bestehenden Aufrufe kompilieren: `Anzeigename`, `Verwendung`, `Standort`, `Warnhinweis`,
     `AbhaengigVon`, `Werte` (`(string Wert, string Text)[]`), `Min`, `Max`, `Einheit`, `Stammdaten` (enum `Lagerplatz` | `Artikelkategorie` |
     `Bestellempfaengergruppe`), `LeerErlaubt`, `BoolLesart`, `Seed`.
   - `Description` bleibt Feldname und bedeutet „Bewirkt“. Sie wird weiter in `ServiceSettings.Description` geseedet (≤ 500 Zeichen,
     vorhandener Test :50-60).
2. **`AppSettingDefinitions.All`** (neu, E1): ein Eintrag je Zeile der Inventurtabelle.
   - `AppSettingKeys.FesteWerte` entfällt; seine zwei Einträge wandern als `Choice` in den Katalog. Die Konstanten in `AppSettingKeys` bleiben.
   - `Program.cs:289-488`: Die acht Seed-Blöcke werden **eine** Schleife über `AppSettingDefinitions.All.Where(d => d.Seed)`. Menge und Werte der
     geseedeten Keys bleiben exakt gleich (Spalte „Seed“, Test AK 2).
   - Keys, die heute nur `SQL/00_FreshInstall.sql` seedet, bekommen `Seed = false`. Sie bleiben weiter nur im FreshInstall.
3. **ViewModel**: `ServiceSettingItem`/`ServiceSettingGroup` werden zu `SettingItem`/`SettingGroup`.
   - `SettingItem` bekommt `Definition`, `RohWert`, `HatZeile`, `Wirksam`, `WeichtAb`, `Ungueltig`, `Fehler` und `Optionen` (für MasterData).
   - `SettingsViewModel` erhält `Groups` und `Orphans`; `Holidays` bleibt.
   - Der Aufbau ist eine gemeinsame Funktion für beide Kataloge (`BuildGroups(definitions, dbRows)`). Sie ist eine Verallgemeinerung von
     `ServiceSettingsController.cs:177-212`.
4. **Partial `_SettingField.cshtml`** (E2): rendert ein `SettingItem` nach F1/F2/F3.
   - Konventionen bleiben erhalten: Hidden-Feld trägt den `name`, Checkbox ohne `name` (Kommentar `ServiceSettings/Index.cshtml:235-236`),
     `name="settings[<Key>]"`. Davon hängen der Sage-Verbindungstest (:251-253) und die Controller-Bindung ab.
   - Master-Key und TLS-Warnung bleiben als Sonderfälle im ServiceSettings-View.
   - Die Sperrhinweise kommen weiter aus `SageQuelleSperre.Grund` (:64-78), nicht aus dem Katalog. So bleibt EIN Ort für die Regel.
5. **Validator** (E4a): `Services/Settings/SettingValidator.cs`, statisch und rein, ohne DB.
   - Die Existenz von MasterData prüft der Controller über vorhandene Repositories: `IStorageLocationRepository.GetByCodeAsync`,
     `IArticleCategoryRepository.GetByNameAsync`, `IOrderRecipientRepository.GetGroupByIdAsync`.
   - Die Dropdown-Listen kommen aus `GetAllOrderedAsync` / `GetAllGroupsAsync`.
6. **Bestandsprüfung** (E4b): in `Program.cs` nach Zeile 505; nutzt `SettingValidator` und `ISyncLogger`.

**frontend-design-Pflicht:** E2 und E3 ändern Views. Der Dev-Lauf ruft den Skill `frontend-design` **explizit** auf. Leitplanken:
- Konsistenz vor Eigenständigkeit: Bootstrap 5 (Cards je Gruppe wie heute `/ServiceSettings`, `form-switch`, `form-select`, `badge`, `alert`).
  Keine neue UI-, Icon- oder Chart-Bibliothek.
- Kontrast WCAG AA. Farbe trägt nie allein Bedeutung: Badges haben Text, Warnungen ein Symbol plus Text.
- An Fertigungsterminals bedienbar: Labels sind mit `for`/`id` verbunden, Hinweise hängen per `aria-describedby` am Feld.

## Migrations-/SQL-Auswirkungen

- **Keine Migration, kein SQL-Skript.** AppSettings bekommt keine neuen Spalten, die Metadaten stehen im Code. Der reservierte Block
  `SQL/110`–`113` bleibt frei; der Dev-Lauf gibt ihn in der Bündel-Übersicht zurück.
- `SQL/00_FreshInstall.sql` bleibt unverändert. `SQL/AgentJobs/` liest weder `AppSettings` noch `ServiceSettings` (Grep ohne Treffer), ist also
  nicht betroffen.
- Ungültige **Bestandswerte** bereinigt die Software nicht. Sie werden angezeigt (E2/E3) und protokolliert (E4b) und dann vom Menschen über die
  Seite korrigiert. Prüfabfrage für den Menschen, je Standort auf der WMS-DB (AKE und IDEAL). Die Kollation ist wichtig: Unter der üblichen
  CI-Kollation wäre `sdata` = `SData`, der Dienst liest aber ordinal.
  ```sql
  -- P-E1a: Quellenschalter mit nicht exakt erlaubtem Wert
  SELECT [Key], [Value] FROM [dbo].[ServiceSettings]
  WHERE [Key] LIKE N'Sync:Quelle:%'
    AND [Value] COLLATE Latin1_General_BIN NOT IN (N'View', N'SData', N'Vergleich', N'');
  -- P-E1b: AppSettings mit Bool-artigem Key, aber nicht kanonischem Wert
  SELECT [Key], [Value] FROM [dbo].[AppSettings]
  WHERE ([Key] LIKE N'%Aktiv' OR [Key] LIKE N'Bde%Buchung%' OR [Key] IN (N'NegativeBuchungErlaubt', N'QrMitFaNummer', N'KommissionierungMitZuweisung', N'LagerplatzEtikettRolle93x55'))
    AND [Value] COLLATE Latin1_General_BIN NOT IN (N'true', N'false');
  -- P-E1c: alle Werte zur Übernahme in die Inventur (Spalte "tatsächliche Werte")
  SELECT [Key], [Value] FROM [dbo].[AppSettings] ORDER BY [Key];
  ```
  Die Inventurspalte „Standort“ und die Aussagen „Wert X an Standort Y“ gelten bis zu diesem Lauf als **ungeprüft**. Kein Etappenschritt wartet
  darauf: E4b meldet dieselben Befunde zur Laufzeit.

## Einstellungen

Dieser Abschnitt ist der Kern der Spec. **Es kommt kein neuer Key hinzu und kein Default ändert sich.** Geändert werden Typ bzw. Bedienelement
und die Beschreibung. Die Spalte „Lesestelle“ ist die Inventur-Grundlage; E1 übernimmt sie in `Verwendung`. Den Text für „Bewirkt“ formuliert
E1 aus der Lesestelle, nicht aus dem Key-Namen.

### AppSettings (`/Settings`, Katalog neu in E1)

Abkürzungen: **NT** = Bool-Lesart nur `"true"` aktiviert, **NF** = nur `"false"` sperrt. Seed: **P** = `Program.cs`-Seed (Zeile), **F** = nur
FreshInstall (`00_FreshInstall.sql:1045-1078`), **–** = kein Seed.

| Key | Typ (neu) | Erlaubte Werte | Default (Leser) | Lesestelle(n) | Seed | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `KommissionierTage` | Int, Arbeitstage | ganze Zahl (Grenze nur laut Leser) | 4 | `FaWorklistController.cs:197`, `ProductionOrdersController.cs:139`, `PickingLeitstandController.cs:100`, `PickingController.cs:147` | F | beide | – |
| `VorkommissionierTage` | Int, Arbeitstage | dito | 1 | `FaWorklistController.cs:198`, `ProductionOrdersController.cs:140`, `PickingLeitstandController.cs:101` | F | beide | – |
| `BeschichtungTage` | Int, Arbeitstage | dito | 10 | `FaWorklistController.cs:200`, `ProductionOrdersController.cs:141`, `PickingLeitstandController.cs:102` | F | beide | – |
| `BeschichtungAbholtage` | MultiChoice | Montag … Sonntag (`BusinessDayService.cs:74-83`) | `Dienstag,Donnerstag` (nur bei fehlender Zeile) | `FaWorklistController.cs:201-202`, `ProductionOrdersController.cs:142`, `PickingLeitstandController.cs:103` | F | beide | – |
| `LackierteilKategorieName` | MasterData Artikelkategorie (Name), LeerErlaubt | Namen aus `ArticleCategories` | `""` = Lackierteil-Erkennung aus | `ProductionOrdersController.cs:145-146`, `FaWorklistController.cs:203-204`, `PickingLeitstandController.cs:106`; Dienst `CoatingDetectionService.cs:59`, `:156` | P:335 | beide | – |
| `WarningThresholdPercent` | Int, % | ganze Zahl | 150 | `StockOverviewController.cs:83` | F | beide | – |
| `CriticalThresholdPercent` | Int, % | ganze Zahl | 100 | `StockOverviewController.cs:84` | F | beide | – |
| `NegativeBuchungErlaubt` | Bool NT | true/false | false | `StockMovementsController.cs:311`, `:402`; `PickingTransferService.cs:233` | F | beide | „Erlaubt Buchungen ins Minus (Ausweich-Lagerplatz).“ |
| `NegativeBuchungLagerplatz` | MasterData Lagerplatz (Code, datalist + Existenzprüfung) | Codes aus `StorageLocations` | `NAN` (nur bei fehlender Zeile) | `StockMovementsController.cs:327-328`, `:416`; `PickingTransferService.cs:240` | F | beide | – ; AbhaengigVon `NegativeBuchungErlaubt` |
| `LagerplatzEtikettRolle93x55` | Bool NT | true/false | false | `Views/StorageLocations/PrintLabels.cshtml:9` | – | beide | – |
| `TeileverfolgungAktiv` | Bool NT | true/false | false | `HomeController.cs:28`, `_Layout.cshtml:56` | P:291 | AKE (OSEON) | – |
| `OseonRueckmeldungAktiv` | Bool NT | true/false | false | **kein Leser** (Rückfrage 3) | P:292 | AKE | „derzeit ohne Wirkung“ |
| `SageRueckmeldungAktiv` | Bool NT | true/false | false | **kein Leser** (Rückfrage 3) | P:293 | beide | „derzeit ohne Wirkung“ |
| `OseonAmpelGelbTage` | Int, Tage | ganze Zahl | 1 | `OseonTrafficLightService.cs:67` | P:470 | AKE | – |
| `OseonAmpelBlauTage` | Int, Tage | ganze Zahl | 2 | `OseonTrafficLightService.cs:68` | P:471 | AKE | – |
| `OseonReportingHorizonDays` | Int, Tage, Min 1 Max 60 | 1–60 (Leser klemmt, `OseonReportingController.cs:50`) | 10 | `OseonReportingController.cs:48-50` | P:294 | AKE | – |
| `OseonReportingOverdueLookbackDays` | Int, Tage, Min 1 Max 365 | 1–365 (Leser klemmt, :54) | 90 | `OseonReportingController.cs:52-55` | P:295 | AKE | – |
| `BestellungenAktiv` | Bool NT | true/false | false | `_Layout.cshtml:57`, `PickingController.cs:476`, `StockMovementsController.cs:98` | P:354 | beide | – |
| `LagerbestellungAktiv` | Bool **NF** | true/false | true | `RequireLagerbestellungAktivAttribute.cs:33`, `_Layout.cshtml:58`, `FaWorklistController.cs:369`, `PickingController.cs:485` | P:359 | beide | „Aus = Lager-/Glasbestellungen, Meine Fehlteile, Lager-Worklists gesperrt.“ |
| `DefaultLagerbestellempfaengerId` | MasterData Bestellempfängergruppe (Id), LeerErlaubt | Ids aus `OrderRecipientGroups` | `""` = Absenden blockt | `WarehouseRequisitionsController.cs:213-223` | P:355 | beide | – |
| `DefaultGlasbestellempfaengerId` | dito | dito | `""` = Absenden blockt | `WarehouseRequisitionsController.cs:213-223` | P:356 | beide | – |
| `GlasArtikelgruppen` | String (kommagetrennt) | frei (Artikelgruppe ist ein Feld am Artikel, keine Stammdatentabelle) | `""` | `ArticlesApiController.cs:30-31`, `WarehouseRequisitionsApiController.cs:85`, `:182` | P:357 | beide | – |
| `GemeinsameArtikelgruppen` | String (kommagetrennt) | frei | `EUZ` | `ArticlesApiController.cs:32-33`, `WarehouseRequisitionsApiController.cs:87` | P:358 | beide | – |
| `LeitstandAktiv` | Bool NT | true/false | false | `HomeController.cs:32`, `_Layout.cshtml:61`, `PickingController.cs:121-125`, `PickingLeitstandController.cs:178` | P:314 | beide | – |
| `KommissionierungMitZuweisung` | Bool NT | true/false | false | `PickingController.cs:127` (erst nach Leitstand-Prüfung :124), `PickingLeitstandController.cs:180`, `:372`, `:417` | P:315 | beide | – ; AbhaengigVon `LeitstandAktiv` |
| `FaCompletionAktiv` | Bool NT | true/false | false | `RequireFaCompletionAccessAttribute.cs:28`, `RequirePickingOrFaCompletionAccessAttribute.cs:34`, `FaWorklistController.cs:88`, `:347`, `_Layout.cshtml:67` | P:316 | beide | – |
| `WindowsAuthAktiv` | Bool NT | true/false | false | `AccountController.cs:48`, `WindowsAutoLoginMiddleware.cs:93` | P:400 | beide | „Ändert die Anmeldung aller Benutzer (Auto-Login).“ |
| `WindowsAuthBerechtigungsgruppe` | String | AD-Gruppe (SAM-Name) | `""` | `ActiveDirectoryService.cs:24` | P:401 | beide | – |
| `FaHierarchyKommissionierlistenAktiv` | Bool NT | true/false | false | `RequireFaHierarchyKommissionierlistenAktivAttribute.cs:45`, `_Layout.cshtml:72` | P:448 | IDEAL | – |
| `FaHierarchyBeschichtungAktiv` | Bool NT | true/false | false | `RequireFaHierarchyBeschichtungAktivAttribute.cs:43`, `_Layout.cshtml:81` | P:449 | IDEAL | – |
| `FaHierarchyVormontageAktiv` | Bool NT | true/false | false | `RequireFaHierarchyVormontageAktivAttribute.cs:43`, `_Layout.cshtml:87` | P:450 | IDEAL | – |
| `FaHierarchyMaxTiefe` | Int, Ebenen | ganze Zahl (Grenze laut `FaHierarchyTreeBuilder`, E1 prüft) | 500 | `FaHierarchyController.cs:80-81`, `:191` | P:451 | IDEAL | – |
| `SageSdataFaAnsichtAktiv` | Bool NT | true/false | false | `SageSdataFaController.cs:40`, `_Layout.cshtml:76` | – | beide | – (liest nur) |
| `QrMitFaNummer` | Bool NT | true/false | false | `StockMovementsController.cs:97`, `:118`, `:168`, `:258`, `:280`, `:299`, `:367`, `:384` | P:472 | beide | – |
| `QrErstesFeld` | Choice | `Artikelnummer` \| `Matchcode` | `Artikelnummer` | `_Layout.cshtml:28-29` | P:473 | beide | – |
| `QrTrennzeichen` | Choice | `;` \| `,` | `;` | `_Layout.cshtml:31` | P:474 | beide | – |
| `BdeAktiv` | Bool NT | true/false | false | `RequireBdeActiveAttribute.cs:24`, `HomeController.cs:36`, `_Layout.cshtml:63`, `FaHierarchyController.cs:119`, `_BdeMasterDataTabs.cshtml:8` | P:420 | beide | – |
| `BdeNurFaMeldung` | Bool NT | true/false | false | `BdeApiController.cs:90`, `:122`, `:213`; `BdeTerminalController.cs:61`, `:81`; `BdeBookingService.cs:277`, `:512`, `:533`; `ProductionWorkplacesController.cs:244` | P:421 | beide | – |
| `BdeDefaultArbeitsgang` | String + Vorschlagsliste Arbeitsgang-Stammdaten (Rückfrage 2) | frei | `""` = Fehler bei NurFA-Buchung ohne Werkbank-Default | `BdeDefaultWorkOperationService.cs:63-68`, `ProductionWorkplacesController.cs:242` | P:422 | beide | – ; AbhaengigVon `BdeNurFaMeldung` |
| `BdeMehrfachBuchungProOperator` | Bool NT | true/false | false | `BdeTerminalController.cs:152`, `BdeBookingService.cs:59`, `:204` | P:423 | beide | – |
| `BdeMehrfachBuchungProArbeitsgang` | Bool NT | true/false | false | `BdeTerminalController.cs:218`, `BdeBookingService.cs:58`, `:203`, `:301` | P:424 | beide | – |
| `BdeGleichzeitigerAbschlussBeiMehrfachStart` | Bool NT | true/false | false | `BdeTerminalController.cs:150-155` | P:425 | beide | – ; AbhaengigVon `BdeMehrfachBuchungProOperator` |
| `BdeSchichtkalenderAktiv` | Bool NT | true/false | false | `BdeShiftCalendarService.cs:21`; Dienst `BdeAutoPauseService.cs:39` | P:426 | beide | „Pausiert laufende Buchungen am Schichtende automatisch (Dienst).“ |
| `BdeTerminalMaxHauptFa` | Int, Anzahl | ganze Zahl | 5 | `BdeTerminalController.cs:69` | P:427 | IDEAL (Master an) | – |
| `Firmenname` | String | frei | `""` | `BriefkopfVorlageService.cs:75` | – | beide | – (auch in Standorteinstellungen) |
| `Firmenanschrift` | String, Multiline | frei | `""` | `BriefkopfVorlageService.cs:76` | – | beide | – (auch in Standorteinstellungen) |
| `BriefkopfVorlagePfad` | String | Pfad | `""` = Ersatz-Kopf | `BriefkopfVorlageService.cs:72` | – | beide | – |
| `HardwareScannerAktiv` | Bool NT | true/false | false | `_Layout.cshtml:17` | – | beide | – |
| `HardwareScannerPraefix` | String | frei | `""` = `~09` | `_Layout.cshtml:22`, `:24` | – | beide | – |
| `HardwareScannerMaxZeichenabstandMs` | Int, ms, Min 10 Max 500, LeerErlaubt | 10–500 (`StandortSettingsCatalog.cs:82-84`) | `""` = 50 | `_Layout.cshtml:23`, `:25` | – | beide | – |
| `VaultLinksAktiv` | Bool **NF** | true/false | true | `_VaultLink.cshtml:9` | P:378 | beide (IDEAL aus) | – |
| `VaultLinkVorlage` | String | URL mit `{Artikelnummer}` | AKE-Adresse (P:379) | `_VaultLink.cshtml:10` | P:379 | beide | – |
| `EnaioLinksAktiv` | Bool **NF** | true/false | true | `_EnaioDmsBadges.cshtml:19` | P:380 | beide | – |
| `EnaioLinkVorlage` | String | URL mit `{ObjektId}` | AKE-Adresse (P:381) | `_EnaioDmsBadges.cshtml:20` | P:381 | beide | – |

Ergänzende Belege:
- **Vollständigkeit:** Grep `GetValueAsync(|GetIntValueAsync(` über `IdealAkeWms/` sowie `AppSettings|AppSettingKeys` über
  `IDEALAKEWMSService/`, Stand 2026-11-07. Weitere Leser: die Views `PrintLabels`, `_BdeMasterDataTabs`, `_EnaioDmsBadges`, `_Layout` und
  `_VaultLink` über `@inject`; `CoatingDetectionService` per Roh-SQL. `wwwroot` liest keine Einstellungen. Ein Key, den E1 zusätzlich findet,
  kommt in den Katalog und wird als Zusatzfund gemeldet.
- **Standort-Spalte:** abgeleitet aus Code-Kommentaren und Modulzugehörigkeit (OSEON gibt es nur an AKE), bis P-E1c **ungeprüft**.

### ServiceSettings (`/ServiceSettings`, Katalog vorhanden, Metadaten in E3)

Typ-Änderungen (alles andere bleibt `Bool`/`Int`/`String`; E3 ergänzt für **alle** 75 Keys (`ServiceSettingDefinitions.cs:19-116`) Anzeigename, Verwendung, Standort, Warnhinweis; Text
„Bewirkt“ = bestehende `Description`, sprachlich geglättet, Inhalt unverändert):

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `Sync:Quelle:Lagerplaetze` | Choice | `View` (bisheriger SQL-Weg) \| `SData` (Sage-Schnittstelle) \| `Vergleich` (liest beide, schreibt aus View) | `View` | wie `ServiceSettingDefinitions.cs:68` | `LagerplatzQuelleSwitch` (Dienst, Lauf „Lagerplatz“) | beide | „SData schreibt Stammdaten aus der Sage-Schnittstelle.“ |
| `Sync:Quelle:Lagerbestand` | Choice | dito | `View` | `:69` | `BestandQuelleSwitch` | beide | dito |
| `Sync:Quelle:Artikel` | Choice | dito | `View` | `:70` | `ArtikelQuelleSwitch` | AKE frei; IDEAL gesperrt bis P-R1 | dito + bestehender Sperrhinweis (`SageQuelleSperre`) |
| `Sync:Quelle:FaStruktur` | Choice | dito | `View` | `:71` | `FaStrukturQuelleSwitch` | gesperrt bis P-F2 | dito + Sperrhinweis |
| `Sync:Quelle:Arbeitsplaetze` | Choice | dito | `View` | `:72` | `ArbeitsplatzQuelleSwitch` | beide | dito |
| `Sync:Quelle:LagerbuchungLookup` | Choice | dito | `View` | `:73` | `BuchungLookupQuelleSwitch` | beide | – (nur Lesen) |
| `Sync:SageLagerbuchungIntervalSeconds` | Int, s, Min 5 | ≥ 5 (Leser hebt < 5 auf 5, `SageBookingWorker.cs:62`) | 20 | unverändert | `SageBookingWorker` | beide | – |
| `Sync:BdeAutoPauseIntervalMinutes` | Int, min, Min 0 | 0 = aus (`SyncWorker.cs:535`) | 60 | unverändert | `SyncWorker` | beide | – |
| `Sync:LagerbestandIntervalMinutes` | Int, min, Min 0 | 0 = Worker-Standard (`SyncWorker.cs:557-559`) | 0 | unverändert | `SyncWorker` | beide | – |

Für weitere Int-Keys trägt E3 nur dann Grenzen ein, wenn der Leser sie belegt (F1).

**Pflicht-Warnhinweise** (Außenwirkung, Einweg oder Datenänderung):
- **Sage-Schreiben:** `SageLagerbuchungAktiv` und `SageLagerbuchungUmbuchungAktiv` („sendet Buchungen an Sage“).
- **Daten im WMS:** `Sync:ProductionOrderReconcileEnabled` („storniert FAs“), `Sync:FaZusatzinfoEnabled` und
  `Sync:FaZusatzinfoAutoErledigtEnabled` („setzt Komm-Erledigt“), `Sync:LagerbestandEnabled` („korrigiert/nullt Bestände“),
  `Sync:KreditorenEnabled` und `Sync:DebitorenEnabled` („überschreibt WMS-Werte“).
- **Mails:** `Sync:PartRequisitionEmailEnabled`, `Sync:WarehouseRequisitionEmailEnabled`, `ErrorNotification:Enabled`,
  `Notifications:MeldebestandEnabled` („versendet Mails“).
- **Sicherheit:** `SageLagerbuchungSslZertifikatPruefen` (bestehende TLS-Warnung bleibt).
- **Einweg:** `ProduktionsauftragHierarchisch` („Einwegtor“, read-only).

**Übernahme aus der SData-Spec:** Deren Tabelle (Nachtrag 2026-11-07) bleibt maßgeblich für Werte, Default und Wirkung der Quellenschalter.
Die dortige „bewusste Lücke“ schließt E3. Die Kreditoren-/Debitoren-Zeilen übernimmt E3 unverändert als Metadaten.

## Audit-Feld-Auswirkungen

Keine. `AppSettings` und `ServiceSettings` sind kein `AuditableEntity` (`AppSetting.cs:5-20`; [[fallstricke]], „`AppSettings` ist **kein**
`AuditableEntity`“). Es entstehen keine neuen Entitäten. Wer welche Einstellung geändert hat, wird wie bisher nicht protokolliert; das ist nicht
angefordert.

**Rollen und Zugriff unverändert:**
- `SettingsController` (:10) und `ServiceSettingsController` (:16) sind `[RequireAdminAccess]`.
- Im View `/Settings` steuert `HasMasterDataAccessAsync` die Bearbeitbarkeit (`Settings/Index.cshtml:5`); Admin hat die Wildcard.
- Kein neuer Filter, keine neue Rolle, daher kein Eintrag in `controller.md` oder `RoleOverview`.

**Listen-View-Pattern:** Beide Seiten sind Formulare, keine Listen-Views. Die bestehende Orphan-Tabelle in `/ServiceSettings`
(`Index.cshtml:143-173`) ist klein, ungepaginiert und vorgefiltert und fällt unter die Suche aus F3. Die neue Orphan-Liste auf `/Settings` wird
genauso behandelt. Pagination und Server-Spaltenfilter entfallen begründet (ADR 0005: Client-Mode für kleine, unpaginierte Ansichten).

## Akzeptanzkriterien

1. **E1:** `AppSettingDefinitions.All` enthält jeden Key der Inventurtabelle genau einmal, mit Typ, Default, Gruppe, Anzeigename, `Description`,
   `Verwendung` und `Standort`. `AppSettingKeys.FesteWerte` existiert nicht mehr; `QrErstesFeld` und `QrTrennzeichen` sind `Choice` mit denselben
   Werten.
2. **E1:** `Program.cs` seedet AppSettings in einer Schleife über den Katalog. Ein Test vergleicht die Menge (Key, Wert) der geseedeten Einträge mit
   der heutigen festen Liste (`Program.cs:289-488`, 38 Einträge) und findet keinen Unterschied.
3. **E1:** Nach E1 sehen `/Settings` und `/ServiceSettings` aus wie vorher. Build und alle Tests sind grün.
4. **E2:** `/Settings` zeigt jeden Katalog-Key, auch ohne DB-Zeile (dann mit dem Default). Je Key erscheinen Anzeigename als Label, der Key klein
   darunter, „Bewirkt“, „Verwendet in“, Default und Standort. Gruppen und Reihenfolge kommen aus dem Katalog. DB-Zeilen ohne Katalog-Eintrag
   stehen read-only unter „Nicht im Katalog“.
5. **E2:** Das Bedienelement folgt dem Typ (F1). Ein Bool zeigt den **wirksamen** Zustand nach Lesart (F6). Beispiel: `LagerbestellungAktiv` mit
   Wert `"1"` ist eingeschaltet und trägt das Badge „ungültig“.
6. **E2:** Ein Speichern ohne Änderung legt keine neue AppSettings-Zeile an und ändert keinen Wert (F4). Prüfbar am Beispiel
   `BeschichtungAbholtage`: Die Zeile wird gelöscht, die Seite gespeichert, und die Zeile existiert danach weiter nicht.
7. **E2:** Das Suchfeld filtert nach Anzeigename, Key und Beschreibung. „Nur geänderte“ zeigt nur Werte, die vom Default abweichen. Abweichung und
   Ungültigkeit sind als Text-Badge sichtbar, nicht nur als Farbe.
8. **E2:** `LackierteilKategorieName` und beide `Default…empfaengerId` sind Dropdowns aus den Stammdaten mit der Option „(leer = aus)“.
   `NegativeBuchungLagerplatz` ist ein Textfeld mit Vorschlagsliste der Lagerplatz-Codes.
9. **E3:** Die sechs `Sync:Quelle:*` sind Dropdowns `View`/`SData`/`Vergleich` mit Anzeigetexten. Ein ungültiger Bestandswert erscheint als
   eigene Option „„x“ (ungültig – bitte wählen)“. Der Sperrhinweis aus `SageQuelleSperre` erscheint unverändert: bei leerer Artikel-View für
   `FaStruktur`, bei gesetzter `Sync:ArticlesViewName` zusätzlich für `Artikel`.
10. **E3:** `/ServiceSettings` nutzt dasselbe Partial wie `/Settings`, mit gleichem Aufbau je Feld. Master-Key (read-only plus Link) und
    TLS-Warnung bleiben. Der Sage-Verbindungstest liefert mit denselben Werten dasselbe Ergebnis wie vorher.
11. **E3:** Ein Default-`true`-Bool mit leerem DB-Wert (z. B. `Sync:ProductionOrdersEnabled = ''`) wird als „Aktiviert“ gezeigt. Speichern
    schreibt `true`, nicht `false` (Zusatzbefund behoben).
12. **E4a:** Ein manipulierter POST mit `Sync:Quelle:Artikel = sdata` wird abgelehnt: Meldung nennt Anzeigename und erlaubte Werte, **kein** Key
    der Seite wird geschrieben. Dasselbe gilt für Int außerhalb Min/Max, unbekannte Choice, unbekannten MultiChoice-Teil und einen
    MasterData-Schlüssel, der nicht existiert.
13. **E4a:** `/Settings` mit `BdeNurFaMeldung = true` und leerem `BdeDefaultArbeitsgang` wird serverseitig abgelehnt (bisher nur JS,
    `Settings/Index.cshtml:267-275`).
14. **E4a:** `/Settings` ignoriert gepostete Keys, die nicht im Katalog stehen (kein neuer Datensatz).
15. **E4a:** Drift-Guard `AppSettingDefinitionsTests`:
    - keine doppelten Keys
    - Bool-Default ist `true`/`false`, Int-Default parsebar und innerhalb Min/Max
    - Choice/MultiChoice hat Werte, und der Default liegt darin
    - MasterData hat eine Quelle
    - Anzeigename, `Description`, `Verwendung`, `Standort` und Gruppe sind nicht leer
    - Felder passen in die Spalten (`Key` 100, `Value` 500)
    - jede `public const string` in `AppSettingKeys` steht im Katalog (Reflection)
    - eine feste Liste der Literal-Keys (`KommissionierTage`, `VorkommissionierTage`, `BeschichtungTage`, `WarningThresholdPercent`,
      `CriticalThresholdPercent`, `OseonAmpelGelbTage`, `OseonAmpelBlauTage`, `TeileverfolgungAktiv`, `LeitstandAktiv`, `BdeAktiv`,
      `BestellungenAktiv`, `OseonRueckmeldungAktiv`, `SageRueckmeldungAktiv`) steht im Katalog
16. **E4a:** `ServiceSettingDefinitionsTests` prüft zusätzlich: Anzeigename, `Verwendung` und `Standort` sind nicht leer; Choice hat Werte und
    der Default liegt darin; jeder `Sync:Quelle:*` ist `Choice` mit genau `View`, `SData`, `Vergleich` (gleich `SageQuelle`,
    `SageQuelleSchalter.cs:9`).
17. **E4a:** Jedes Feld von `StandortSettingsCatalog.Fields` steht im passenden Katalog (`Backend` App oder Service). Der Typ passt
    (String ↔ String/MasterData, Int ↔ Int, Bool ↔ Bool), und Min/Max stimmen überein, wo beide sie setzen.
18. **E4b:** Liegt beim Web-Start ein ungültiger Wert in AppSettings oder ServiceSettings, erscheint im Aktivitäts-Protokoll ein Lauf
    „Einstellungsprüfung“ mit je einer Warnzeile: Key, Rohwert, erlaubte Werte, wirksamer Wert. Ohne ungültige Werte entsteht kein Lauf.
    `SyncLogServices.All` und `DisplayNames` enthalten den Dienst.
19. **Alle Etappen:** Keine Lesestelle ändert ihr Verhalten. Die bestehenden Tests zu Lesestellen und Filtern laufen unverändert grün.
20. **Abschluss:**
    - Version 1.77.0 in beiden `AppVersion.cs`.
    - `Views/Help/Changelog.cshtml` ergänzt.
    - `Views/Help/Index.cshtml:124-125` nennt Anzeigenamen statt Keys.
    - `README.md` (AppSettings-Tabelle) verweist auf den Katalog als Quelle.
    - Testszenario in `docs/TESTSZENARIEN.md` und im Index.

## Test-Szenarien

Neues Kapitel **TS-1xx „Einstellungen: Auswahlfelder und Beschreibung (v1.77.0)“** mit der nächsten freien Nummer. Beim Schreiben ist TS-107
frei; die vorher laufenden Bug-Specs des Bündels können Nummern belegen, deshalb prüft der Dev-Lauf neu. Standort beide, je Unterszenario
Vorbedingung, Schritte, Erwartung und Negativfall:

- **TS-x.1 Darstellung /Settings** (E2): Gruppen, Anzeigenamen, „Bewirkt“/„Verwendet in“, Default, Standort, Warnhinweise
  (`NegativeBuchungErlaubt`, `WindowsAuthAktiv`), Key ohne Zeile sichtbar (`HardwareScannerAktiv`), Suche, „Nur geänderte“.
  Negativ: Nur-Lesen-Hinweis ohne Bearbeitungsrecht.
- **TS-x.2 Speichern ohne Nebenwirkung** (E2):
  - Vorher P-E1c-Auszug ziehen, unverändert speichern, P-E1c erneut ausführen: identisch, keine neuen Zeilen.
  - Negativ: Zeile `BeschichtungAbholtage` per SQL löschen, speichern, Zeile bleibt weg, FA-Liste zeigt weiter Abholtage Di/Do.
- **TS-x.3 Bool-Lesart** (E2/E3):
  - `LagerbestellungAktiv` per SQL auf `1`: Seite zeigt „Aktiviert, ungültig“, Menü Lagerbestellung sichtbar; nach Speichern steht `true`.
  - `Sync:ProductionOrdersEnabled` auf `''`: wird als aktiviert gezeigt, Speichern schreibt `true`.
- **TS-x.4 Quellenschalter** (E3):
  - Dropdown zeigt drei Werte; IDEAL mit gesetzter Artikel-View zeigt den Sperrhinweis bei Artikel und FaStruktur.
  - Per SQL `Sync:Quelle:Lagerplaetze = 'sdata'`: Option „ungültig“, Speichern ohne Korrektur wird abgelehnt.
  - Nach Korrektur auf `View` läuft der Lauf „Lagerplatz“ ohne Quellen-Warnung.
- **TS-x.5 Serverprüfung** (E4a): manipulierte POSTs (Browser-Entwicklerwerkzeuge oder ein Werkzeug wie curl) mit ungültigem Int, Choice,
  MultiChoice, MasterData und unbekanntem Key. Erwartung: Meldung, nichts geschrieben (P-E1c vorher/nachher identisch). `BdeNurFaMeldung` an
  plus leerer Default-Arbeitsgang wird abgelehnt.
- **TS-x.6 Bestandsprüfung** (E4b): ungültigen Wert per SQL setzen, App-Pool recyceln. Aktivitäts-Protokoll zeigt „Einstellungsprüfung“ mit
  Warnzeile. Nach Korrektur und erneutem Recycle: kein neuer Lauf.
- **TS-x.7 Regression:** Sage-Verbindungstest, Master-Anzeige mit Link, Standorteinstellungen speichern (unverändert), QR-Einstellungen wirken
  wie in TS-101.

Testindex `secondbrain/tests/testszenarien-index.md` nachziehen.

## Etappen (nur bei epic: true)

Ein Worktree (Bündel `feature/2026-08-07-ideal-teile-1-5`) und ein Merge am Ende. Jede Etappe baut und testet für sich grün und endet in einem
eigenen Commit. Keine Etappe wartet auf eine offene Rückfrage. Wo eine Antwort fehlt, baut der Lauf nach der Empfehlung und vermerkt es in der
Etappenzeile. Der Branch wird während der Arbeit mit `scripts/sync-worktree.ps1 -Slug 2026-08-07-ideal-teile-1-5` auf main-Stand gehalten.

**Bündel-Reihenfolge:** nach den drei Bug-Specs, **vor** der EAN-Spec. Die EAN-Spec legt ihre neuen Einstellungen dann direkt in
`AppSettingDefinitions` bzw. `ServiceSettingDefinitions` mit allen Pflichtangaben an. Versionsnummer: eine für das ganze Epic, v1.77.0, gesetzt
in der letzten Etappe.

| # | Etappe | Inhalt / Abschluss | Hängt an | Status | Commit |
|---|--------|--------------------|----------|--------|--------|
| E1 | Inventur + Katalog AppSettings | Record-Umbenennung + optionale Metadaten; `AppSettingDefinitions.All` laut Inventur (Lesestellen beim Schreiben erneut greppen); `FesteWerte` → Choice; Seed-Schleife; AK 1–3; Tests: Seed-Gleichheit, Katalog vollständig. Keine sichtbare Änderung. | – (Rückfrage 2/3: Empfehlung) | erledigt | `b220fcba` |
| E2 | Darstellung /Settings | `_SettingField.cshtml`, verallgemeinertes ViewModel/`BuildGroups`, Gruppen, Suche, „Nur geänderte“, Bool-Lesart, MasterData-Optionen, Schreibregel F4; frontend-design-Skill; AK 4–8; Controller-Tests (Darstellung, F4). | E1 | erledigt | `924e926e` |
| E3 | Darstellung /ServiceSettings | Metadaten aller Service-Keys, `Sync:Quelle:*` → Choice, Sperrhinweis/TLS/Master unverändert, Bool-Lesart `LeerIstDefault`; AK 9–11; Regression `ServiceSettingsControllerTests`. | E1, E2 (Partial) | erledigt | `8200dae5` |
| E4a | Serverprüfung + Drift-Guard | `SettingValidator`, beide Controller ganz oder gar nicht, Allow-List `/Settings`, NurFA-Pflicht serverseitig; AK 12–17; `SettingValidatorTests`, erweiterte Drift-Guards. | E1–E3 | erledigt | `cb23aa58` |
| E4b | Bestandsprüfung beim Start | `Program.cs` nach Seed, `SyncLogServices.Einstellungspruefung` + DisplayName; AK 18. Danach Abschluss AK 20 (Version, Changelog, Hilfe, README, TS, Brain). | E4a; **Rückfrage 1** (baut nach Empfehlung; bei „nein“ entfällt die Etappe und wird als Blocker vermerkt) | erledigt | `3e28eebd` |

## Deploy

**Vorläufig.** Der QA-Lauf bestätigt die Angaben gegen den echten Diff.

- **Web-App:** ja (Controller, Views, Katalog, `Program.cs`).
- **Service:** nein, funktional. Der Dienst referenziert das Web-Projekt und liefert dessen DLL mit; Katalog und `SageQuelleSperre` ändern dort
  kein Verhalten. Erfasst der Diff Dienst-Code, wird auf ja korrigiert.
- **Migration:** nein. Der reservierte SQL-Block 110–113 bleibt ungenutzt.
- **Ablauf:**
  1. Publish aus dem Worktree, auf DEV AKE und DEV IDEAL testen.
  2. Vorher P-E1a/b/c je DEV-DB ausführen. Ungültige Werte sind nach dem Deploy auf der Seite markiert und blockieren dort das Speichern, bis
     sie korrigiert sind.
  3. Danach Merge (Schranke 2).
- **Publish-Befehle:**
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  ```
- **Deploy-Risiko:** Ein ungültiger Bestandswert sperrt das Speichern der ganzen Seite, bis er korrigiert ist. Das ist gewollt (ADR 0020:
  ablehnen statt still Default). Darum P-E1 vor der Produktiv-Einspielung je Standort laufen lassen.

**Final (QA 2026-11-07, aus dem echten Diff `2fafd073..3e28eebd`):**
- Web: **ja** (`IdealAkeWms/`: Kataloge, Controller, Views, `_SettingField`, `settings-fields.js`, `Program.cs`, `SyncLogServices`).
- Service: **ja, nur Versionsstand** (`IDEALAKEWMSService/AppVersion.cs` 1.76.0 → 1.77.0; der Dienst liefert die geänderte Web-DLL mit, Katalog
  und `SyncLogServices` ändern dort kein Verhalten). Mitveröffentlichen, damit Web und Dienst denselben Stand zeigen.
- Migration: **nein** (keine Datei unter `*/Migrations/`, kein `SQL/`-Skript, kein Einmal-Skript).
- Reihenfolge: (1) P-E1a/b/c je Standort auf der WMS-DB ausführen und notieren, (2) Web publizieren, (3) Dienst publizieren und starten,
  (4) Aktivitäts-Protokoll auf einen Lauf „Einstellungsprüfung“ prüfen (erscheint nur bei ungültigen Werten).
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```
- Ablauf Mensch: Publish aus dem Worktree → Testsystem → Test → Merge. Nach dem Merge nur dann erneut aus `main` publizieren, wenn der Merge
  getestete Dateien mit parallelen `main`-Änderungen zusammengeführt hat.

## QA-Nachweis (qa-agent, 2026-11-07)

Geprüft im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, HEAD `3e28eebd`.
Review-Bereich `git diff 2fafd073 3e28eebd` (E1 `b220fcba`, E2 `924e926e`, E3 `8200dae5`, E4a `cb23aa58`, E4b `3e28eebd`), 35 Dateien, +1693/−667.

### 1. Build und Tests (ganzer Worktree)
- `dotnet build IdealAkeWms.slnx`: **0 Fehler**, 28 Warnungen (bestehende Nullable-/NuGet-Warnungen, keine aus diesem Diff).
- `dotnet test --no-build`:
  - `IDEALAKEWMSService.Tests`: **497 bestanden, 0 übersprungen, 0 fehlgeschlagen** (497).
  - `IdealAkeWms.Tests`: **2424 bestanden, 1 übersprungen, 0 fehlgeschlagen** (2425).
  - Die bekannten Flaky-Tests (`SyncWorker_IgnoresIConfigurationDisableSeed_ForBlockGates`, `NotificationWorker_PositiveLowStock_UsesOrangeColor`)
    sind in diesem Lauf nicht aufgetreten.

### 2. Code-Review (selbst, synchron)
Gelesen: `SettingDefinition.cs`, `AppSettingDefinitions.cs` (alle 54 Einträge), `ServiceSettingDefinitions.cs` (Meta für alle 75 Keys, `IntMeta`,
`Ergaenzen`), `SettingValidator.cs`, `SettingItem.cs`, `Einstellungspruefung.cs`, beide Controller vollständig, `_SettingField.cshtml`,
`Settings/Index.cshtml`, ServiceSettings-View-Diff inkl. Sage-Verbindungstest-JS, `settings-fields.js`, `Program.cs`-Diff, alle geänderten Tests.
Gegengeprüft: Lesestellen `RequireLagerbestellungAktivAttribute.cs:34`, `_Layout.cshtml:14-31`, `BusinessDayService.cs:74-95`,
`ServiceSettings.cs:31-36` (Dienst), `AppSettingRepository.cs`, `CachedSettingRepository.cs:10`. Seed-Gleichheit: die 38 (Key, Wert) aus
`git show 2fafd073:IdealAkeWms/Program.cs` extrahiert = `FruehererSeed` im Test = `SeedEintraege` (AK 2, AK 19).
Defektklassen: kein `@Html.Raw`/kein Server-Wert in `<script>`; kein `hidden`-Attribut (Filter über Klasse `d-none`); kein `?? 0`;
Bindung über `Dictionary<string,string>` nach Key (keine Parallel-Arrays); NurFA-Pflicht jetzt serverseitig (AK 13); Bool-Lesart an einer Stelle
(`SettingItem.BoolWirksam`) – siehe B3.

| # | Befund | Schwere | Behandlung |
|---|---|---|---|
| B1 | F5 verlangt, dass die Seite nach Ablehnung **mit den eingegebenen Werten** zurückkommt. `/Settings` macht `RedirectToAction(Index)` (`SettingsController.cs` SaveSettings, Fehlerzweig), `/ServiceSettings` gibt zwar die View zurück, baut das Modell aber aus der DB (`BuildViewModelAsync`). Auf DEV bestätigt: nach Ablehnung (AK 13) stand „nur FA-Meldung“ wieder auf aus. Daten sicher (nichts geschrieben), aber die Eingaben sind weg. | mittel (UX, Abweichung von F5) | offen – Mensch entscheidet: nachbessern oder als Abweichung akzeptieren |
| B2 | `NegativeBuchungLagerplatz` (MasterData, `LeerErlaubt=false`, Default `NAN`): fehlt die Zeile **und** gibt es keinen Lagerplatz `NAN`, zeigt die Seite kein „ungültig“ (Prüfung nur bei vorhandener Zeile, `SettingItem.Aus`), aber jedes Speichern scheitert an „ein vorhandener Stammdaten-Eintrag“. Auf DEV AKE/IDEAL nicht auslösbar (Zeile und Lagerplatz `NAN` vorhanden). | niedrig | offen; Prüfabfrage je Standort: `SELECT (SELECT COUNT(*) FROM dbo.AppSettings WHERE [Key]=N'NegativeBuchungLagerplatz') AS Zeile, (SELECT COUNT(*) FROM dbo.StorageLocations WHERE Code=N'NAN') AS NAN;` – beide 0 = Befund trifft zu |
| B3 | `BoolWirksam`/`SettingValidator` trimmen den Rohwert, die Leser nicht (z. B. `RequireLagerbestellungAktivAttribute.cs:34` vergleicht ohne Trim). Ein Bestandswert `" false"` würde als „aus“ gezeigt, wirkt aber „an“; Speichern schriebe `false` und änderte das Verhalten. Nur bei Leerzeichen im DB-Wert. | niedrig (theoretisch) | offen, Hinweis; P-E1b erfasst solche Werte nicht (Leerzeichen-Varianten sind `NOT IN ('true','false')` → werden dort gelistet) |
| B4 | Choice-Dropdown vergleicht exakt (`_SettingField.cshtml`, `w.Wert == Model.Wirksam`), der AppSettings-Validator ohne Groß/Klein. Ein Bestandswert `matchcode` erscheint als Option „(ungültig – bitte wählen)“, aber ohne Badge/Banner; Speichern kanonisiert auf `Matchcode` (Leser `_Layout.cshtml:28` ist ebenfalls case-insensitiv → kein Verhaltenswechsel). | kosmetisch | offen, Hinweis |
| B5 | Drift-Guard AK 16 (`SettingValidatorTests.DriftGuard_…`) prüft `Anzeigename` nicht leer – `ServiceSettingDefinitions.Ergaenzen` setzt bei fehlendem `Meta` aber den Key als Anzeigename und „Dienst“ als Verwendung. Ein künftiger Service-Key ohne Meta fällt dem Test daher nicht auf. Heute vollständig (75 Meta-Einträge = 75 Basis-Keys, per Skript abgeglichen). | niedrig | offen, Hinweis (Test um „Anzeigename != Key“ ergänzen) |
| B6 | Brain: `secondbrain/feature-map.md` hat noch keinen Eintrag für v1.77.0 (auch nicht v1.76.0). | Doku | offen – beim Bündel-Abschluss nachziehen |

Positiv bestätigt: Allow-List `/Settings` (unbekannte Keys ignoriert), „ganz oder gar nicht“ in beiden Controllern (Prüfung vor jedem Schreiben),
F4 (keine neue Zeile bei Default ohne Zeile, unveränderte Werte nicht neu geschrieben), Choice exakt auf ServiceSettings (`exakt: true`),
Default-true-Service-Bool mit leerem Wert bleibt an (AK 11), Master nur Anzeige (kein Formularfeld), TLS-Warnung nutzt den wirksamen Wert,
Sperrhinweis weiter aus `SageQuelleSperre.Grund`, Sage-Test-JS findet alle fünf `input[name="settings[...]"]` (Bool als Hidden-Input).
Nebeneffekt AK 11, beabsichtigt: steht `SageLagerbuchungSslZertifikatPruefen` leer in der DB, sendet der Sage-Verbindungstest jetzt
`verifyCertificate=true` (vorher `false`) – jetzt gleich dem Dienst (leer = geprüft).

### 3. Nachweisart je AK
| AK | Nachweis |
|---|---|
| 1 | automatisiert (`AppSettingDefinitionsTests.Katalog_…_AK1`, `QrFesteWerte_AlsChoice_AK1`) |
| 2 | automatisiert (`Seed_GleichDerFrueherenListe_AK2`) + QA-Abgleich gegen alten `Program.cs` |
| 3 | Build/Tests grün; Darstellung = Manual-UAT (DEV gesehen) |
| 4, 5, 7, 8 | Controller-Teil automatisiert (`SettingsControllerKatalogTests`); Darstellung, Suche, „Nur geänderte“, Badges = Manual-UAT (Browser, DEV OK) |
| 6 | automatisiert (`Save_OhneAenderung_…_AK6`) + DEV OK (BeschichtungAbholtage-Zeile blieb weg) |
| 9, 10 | Manual-UAT (DEV OK); AK 10 Sage-Test-Ergebnis selbst NICHT MOEGLICH (s. u.) |
| 11 | automatisiert (`SettingItem`/Controller) + DEV OK (an `SageLagerbuchungSslZertifikatPruefen`, s. u.) |
| 12 | automatisiert (`SettingValidatorTests`, `ServiceSettingsControllerTests`, `Save_UngueltigerWert_…_AK12`) + DEV OK (manipulierte POSTs) |
| 13, 14 | automatisiert + DEV OK |
| 15, 16, 17 | automatisiert (Drift-Guards) |
| 18 | Logik automatisiert (`EinstellungspruefungTests`); Start-Ablauf mit echter DB = Manual-UAT (DEV OK) |
| 19 | automatisiert (keine Lesestelle im Diff, Seed-Gleichheit, bestehende Lesestellen-Tests grün) |
| 20 | Durchsicht: beide `AppVersion.cs` 1.77.0, `Changelog.cshtml`, `Help/Index.cshtml`, `README.md`, TS-109, Index-Zeile 109 vorhanden |

### 4. Geänderte bestehende Tests
| Test | Einordnung |
|---|---|
| `OperationConfigsControllerTests` (Ctor-Aufruf) | nur Typ/Signatur (8 Ctor-Parameter) |
| `ServiceSettingDefinitionsTests`, `SageTlsPolicyTests` | nur Typ (`ServiceSettingType` → `SettingType`) |
| `ServiceSettingsControllerTests` Index-Test | Typ (`Value` → `Wirksam`, `Definition.*`) |
| `ServiceSettingsControllerTests` Save-Bool | Verhalten bewusst geändert: F4/AK 6 (Default ohne Zeile wird nicht geschrieben) |
| `ServiceSettingsControllerTests` Int-Fehler | Verhalten bewusst geändert: AK 12 (ganz oder gar nicht statt Teilspeicherung) |
| `SettingsControllerQrErstesFeldTests` | Ctor (Typ); Fixture mit bestehenden Zeilen wegen F4 (sonst schriebe ein Default-Wert nichts); Meldungstext nach AK 12 – bewusst |
| `HardwareScannerTests`, `VaultEnaioLinksTests` | Quelle der Seed-Prüfung von `Program.cs`-Text auf `AppSettingDefinitions.SeedEintraege` umgestellt (E1, AK 2) – gleiche Werte geprüft |
Keine Einordnung „sonstige“.

### 5. Checkliste / ADR 0020 (5a)
- Migration/SQL/FreshInstall/AgentJobs: nicht betroffen (keine Schema-Änderung, verifiziert im Diff). Audit-Felder: n/a (AppSettings/ServiceSettings
  kein `AuditableEntity`). Version beide 1.77.0, Anwender-Changelog + Brain-Changelog
  `[[2026-11-07-v1-77-0-einstellungen-auswahlfelder-und-beschreibung]]` vorhanden. TS-109.1–109.7 + Index-Zeile 109 vorhanden. Aufgabe
  `[[2026-11-07-appsettings-ohne-leser-aufraeumen]]` vorhanden. Offen: feature-map (B6).
- ADR 0020: feste Wertebereiche als Auswahl (Choice/MultiChoice/MasterData, `Sync:Quelle:*`), Serverprüfung beim Speichern beider Seiten,
  Anzeigename/Bewirkt/Verwendung/Default/Standort je Key (Drift-Guard), Warnhinweise an allen in der Spec geforderten Keys vorhanden. Keine Lücke
  außer B5 (Test-Schwäche).

### 6. DEV-Deploy und Testlauf
- Standort laut Frontmatter: **beide**. Elevated Shell: ja.
- `dev-deploy.ps1 -Standort AKE -Quelle <worktree>`: Exit 0, „HTTP 200, Fusszeile v1.77.0“, DEV-Absicherung angewendet, Dienst gestartet.
- `dev-deploy.ps1 -Standort IDEAL -Quelle <worktree>`: Exit 0, „HTTP 200, Fusszeile v1.77.0“, DEV-Absicherung angewendet, Dienst gestartet.
- Vorher-Auszug AppSettings+ServiceSettings je DEV-DB gezogen; am Ende beide DBs **identisch** zum Vorher-Stand (alle Testwerte zurückgesetzt;
  einzige Restabweichung: die per SQL gelöschte und wieder eingefügte Zeile `BeschichtungAbholtage` auf AKE hat `Description = ''`).
  Kein verbotener Schalter wurde eingeschaltet.

| Schritt | Ausgeführt | Ergebnis | Status |
|---|---|---|---|
| 109.1 AKE `/Settings` | Seite, Suche `scanner`, „Nur geänderte“ | Gruppen/Anzeigenamen/Key klein/Bewirkt/Verwendet in/Standard/Standort; Warnhinweise an Minus-Buchung, Windows-Anmeldung, Schichtkalender; „Hardware-Barcodescanner“ sichtbar; Suche filtert; „Nur geänderte“ zeigt nur Felder mit Badge „weicht vom Standard ab (Standard: …)“; Orphan `StammdatenADGruppe` unter „Nicht im Katalog“ (kein Leser im Code, nur alte SQL-Skripte – korrekt) | OK |
| 109.1 Negativ (ohne Stammdaten-Recht) | – | kein zweiter Benutzer auf DEV | NICHT MOEGLICH |
| 109.2 AKE | Zeile `BeschichtungAbholtage` per SQL gelöscht, unverändert gespeichert, Diff | „Einstellungen gespeichert.“, einzige Abweichung = gelöschte Zeile bleibt weg, keine neue Zeile | OK |
| 109.2 IDEAL | unverändert gespeichert, Diff | identisch | OK |
| 109.2 FA-Liste zeigt Abholtage Di/Do | – | nicht im Browser geprüft (Lesestelle unverändert, Default bei fehlender Zeile) | NICHT MOEGLICH (offen für Mensch) |
| 109.3 AppSettings | `LagerbestellungAktiv='1'`, Seite, Speichern | „Aktiviert“ + Badge „ungültig: „1““ + Fehlertext + Sammelbanner; nach Speichern `true` | OK |
| 109.3 ServiceSettings | statt `Sync:ProductionOrdersEnabled` (leer = wirksam AN = verbotener Sync-Schalter) an `SageLagerbuchungSslZertifikatPruefen=''` (Default true) | als „Aktiviert“ gezeigt; Speichern schrieb `true`; danach auf `false` zurückgesetzt | OK (Ersatz-Key) |
| 109.4 AKE | `Sync:Quelle:Lagerplaetze='sdata'`, Seite, Speichern ohne Korrektur, dann `View` gewählt und gespeichert | Option „„sdata“ (ungültig – bitte wählen)“, Badge, Banner; Ablehnung „Nicht gespeichert: „Quelle Lagerplatz-Abgleich“: „sdata“ … „View“, „SData“, „Vergleich““, DB unverändert; nach Korrektur nur die zwei erwarteten Werte geschrieben | OK |
| 109.4 IDEAL Sperrhinweis | `/ServiceSettings` IDEAL (Artikel-View gesetzt, `Quelle:Artikel=SData`) | Hinweis „gesperrt bis P-R1 … wirkt deshalb wie View“ an Artikel, „gesperrt bis P-F2“ an FA-Struktur | OK |
| 109.4 Lauf „Lagerplatz“ ohne Quellen-Warnung | – | braucht `Sync:LagerplaetzeEnabled` | NICHT MOEGLICH |
| 109.5 | manipulierter POST IDEAL (Horizont 99, Abholtag `Funtag`, Empfängergruppe 9999, `Erfunden=x`); POST nur `Erfunden`; ServiceSettings-POST `Quelle:Artikel=sdata` + `BomCacheWeeks=9` | drei Meldungen mit Anzeigename und erlaubten Werten, nichts geschrieben; `Erfunden` nie angelegt; ServiceSettings abgelehnt, `BomCacheWeeks` blieb 8 | OK |
| 109.5 NurFA ohne Default-Arbeitsgang | IDEAL: Schalter an, Default leer, speichern | „Nicht gespeichert: „BDE: Default-Arbeitsgang“ ist Pflicht …“, DB identisch (Eingabe danach zurückgesetzt, siehe B1) | OK |
| 109.6 | AKE: `LagerbestellungAktiv='1'`, App-Pool `AKE_WMS_DEV` recycelt; korrigiert, erneut recycelt | `SyncLogs` Service `Einstellungspruefung`: 1 Lauf, Warnzeile „Einstellungen (/Settings) LagerbestellungAktiv: „Lagerbestellungen“: „1“ ist nicht erlaubt – erlaubt: an oder aus (true/false). Wirksam: „true“.“, Abschluss `ungueltig=1`; nach Korrektur kein neuer Lauf | OK |
| 109.7 Master | IDEAL `/ServiceSettings` | Anzeige mit Link „Umschalten (eigene Seite)“, kein Formularfeld | OK |
| 109.7 Sage-Verbindungstest | DOM geprüft: fünf `input[name="settings[...]"]` vorhanden | Ergebnisvergleich gegen Sage nicht ausgeführt (kein Zugriff auf Sage-Testserver aus dem QA-Lauf) | NICHT MOEGLICH |
| 109.7 Standorteinstellungen | IDEAL unverändert gespeichert | „Standorteinstellungen gespeichert.“, DB identisch | OK |
| 109.7 QR wie TS-101 / TLS-Warnung bei `false` | – | Scan-Verhalten braucht Gerät/Kamera | NICHT MOEGLICH |

### Gesamturteil
Build und Tests grün, DEV beide Standorte auf v1.77.0, alle ausführbaren Szenarien OK. Offene Befunde B1–B6 sind nicht datengefährdend;
B1 ist eine Abweichung von F5 und wird dem Menschen zur Entscheidung vorgelegt. **Testbereit.**

### Manuelle Test-Checkliste (Mensch, Schranke 2)
1. B1 entscheiden: Eingaben nach Ablehnung behalten (nachbessern) oder Abweichung von F5 akzeptieren.
2. Je Standort auf dem Testsystem vor dem Deploy P-E1a/b/c und die Prüfabfrage aus B2 ausführen und notieren.
3. TS-109.1 Negativfall mit einem Benutzer ohne Stammdaten-Recht (Felder gesperrt, Nur-Lesen-Hinweis, kein Speichern-Knopf).
4. TS-109.2: nach gelöschter Zeile `BeschichtungAbholtage` zeigt die FA-Liste weiter Abholtage Di/Do.
5. TS-109.3 mit `Sync:ProductionOrdersEnabled=''` auf dem Testsystem (nur wo der Sync laufen darf).
6. TS-109.4: nach Korrektur auf `View` läuft „Lagerplatz“ ohne Quellen-Warnung (braucht `Sync:LagerplaetzeEnabled`).
7. TS-109.7: Sage-Verbindungstest mit denselben Werten vor/nach Deploy vergleichen; TLS-Warnung bei `false`; QR-Scans wie TS-101.
8. Kompletter TS-109-Durchlauf auf dem Testsystem (beide Standorte), danach Merge.
9. B6: `feature-map.md` beim Bündel-Abschluss nachziehen.

## QA-Wiederholung 2026-11-07 (Fix bd178dc4)

qa-agent, goal-Modus. Review-Umfang: `git diff 3e28eebd bd178dc4` (Fix B1–B5). Build/Tests/DEV über den ganzen Worktree, HEAD `8534da23`
(v1.78.0, enthält schon die EAN-Spec).

**1. Build und Tests:** `dotnet build IdealAkeWms.slnx` 0 Fehler (28 Warnungen, Altbestand). `dotnet test`: IdealAkeWms.Tests 2430 bestanden /
1 übersprungen / 0 Fehler (2431); IDEALAKEWMSService.Tests 497 / 0 / 0. Die bekannten Flaky-Tests traten in diesem Lauf nicht auf.

**2. Review des Fix-Diffs (selbst, synchron)** – geprüft: `SettingsController.SaveSettings`/`BuildViewModelAsync`, `ServiceSettingsController.SaveSettings`/
`BuildViewModelAsync`, `SettingItem.BoolWirksam`/`Aus`/`EntsprichtDefault`, `SettingValidator` (Bool), `Views/Settings/Index.cshtml` (Banner),
`_Layout.cshtml` (zeigt nur `TempData`, daher keine Doppelmeldung), die Leser `ServiceSettings.GetBoolAsync` (leer/Whitespace = Default, sonst
ordinal `true`) und `RequireLagerbestellungAktivAttribute` (ohne Trim).

| # | Fix | Ergebnis |
|---|---|---|
| B1 | beide Masken geben nach Ablehnung die View mit den geposteten Werten zurück (`eingaben` überschreibt den DB-Stand, nur Katalog-Keys) | behoben, DEV bestätigt (siehe 6) |
| B2 | unveränderter Rohwert (bzw. ohne Zeile = Default) wird weder geprüft noch geschrieben | behoben, DEV bestätigt |
| B3 | Bool ohne Trim wie die Leser; `LeerIstDefault` nimmt Whitespace als leer (= `GetBoolAsync`) | behoben, DEV bestätigt |
| B4 | Choice in abweichender Schreibweise → `Ungueltig` + Text „Schreibweise …“ (Badge + Banner) | behoben, DEV bestätigt |
| B5 | Test `ServiceKatalog_JederKeyHatEigeneMetadaten_QA_B5` (Anzeigename ≠ Key, Verwendung ≠ „Dienst“) | behoben, Test grün |
| B6 | feature-map | bleibt offen für den Bündelabschluss (wie beauftragt) |

Neue Befunde aus dem Fix-Diff:

| # | Befund | Schwere | Behandlung |
|---|---|---|---|
| W1 | `SettingValidator` Bool: ein reiner Leerzeichen-Wert (`" "`) wird beim Speichern als ungültig abgelehnt, der Dienst-Leser (`GetBoolAsync`) liest ihn als Default. Erreichbar nur über einen manipulierten POST (die Maske postet immer `true`/`false`), Bestandswerte fallen durch B2 nicht mehr in die Prüfung. | Hinweis | keine Änderung nötig |
| W2 | Testlücke: B1 und B2 für `/ServiceSettings` und B2 für `/Settings` haben keinen eigenen Unit-Test (B1 `/Settings` ist mit `Save_Abgelehnt_SeiteMitEingaben_QA_B1` abgedeckt). Auf DEV manuell nachgewiesen. | niedrig | Hinweis, kein Blocker |

**3. Nachweisart:** B1 `/Settings`, B3, B5 automatisiert (`SettingsControllerKatalogTests.Save_Abgelehnt_SeiteMitEingaben_QA_B1`,
`Index_BoolOhneTrim_WieDerLeser_QA_B3`, `SettingValidatorTests.ServiceKatalog_JederKeyHatEigeneMetadaten_QA_B5`); B1 `/ServiceSettings`, B2, B4 und
die Anzeige der Banner: Manual-UAT (DEV, unten).

**4. Geänderte bestehende Tests:** `SettingsControllerKatalogTests` (2 Tests: `TempData` → `ViewData`) und `SettingsControllerQrErstesFeldTests`
(2 Tests: `RedirectToActionResult` → `ViewResult`, `TempData` → `ViewData`) – Verhalten absichtlich geändert durch F5/B1. Keine Fixture-Anpassung.

**6. DEV (beide Standorte, Quelle Worktree, `dev-deploy.ps1` Exit 0, Fußzeile v1.78.0 an AKE und IDEAL):**

| Schritt | Ausführung | Ergebnis | Status |
|---|---|---|---|
| B1 `/Settings` | IDEAL: „BDE: nur FA-Meldung“ an, Default-Arbeitsgang leer, speichern | Banner „Nicht gespeichert: „BDE: Default-Arbeitsgang“ ist Pflicht … Ihre Eingaben stehen unten noch im Formular.“, Schalter steht weiter auf „Aktiviert“; DB: `BdeNurFaMeldung=false`, `BdeDefaultArbeitsgang=''` unverändert | OK |
| B1 `/ServiceSettings` | IDEAL: `Cleanup:AktivitaetsprotokollAufbewahrungTage` geleert, `WorkerSettings:SyncIntervalMinutes` 15 → 16, speichern | Fehler „„Aktivitäts-Protokoll: Aufbewahrung“: „“ ist nicht erlaubt – erlaubt: eine ganze Zahl.“, Formular zeigt leer und 16; DB 180 / 15 unverändert | OK |
| B2 | IDEAL: `NegativeBuchungLagerplatz` per SQL auf nicht vorhandenen Lagerplatz `XXQA`, `/Settings` unverändert speichern | Seite zeigt den Wert als ungültig; Speichern → „Einstellungen gespeichert.“, `CHECKSUM_AGG` über `AppSettings` vorher = nachher (nichts geschrieben); danach auf `NAN` zurückgesetzt | OK |
| B3 | IDEAL: `LagerbestellungAktiv = ' false'` per SQL | Schalter „Aktiviert“ + Badge „ungültig: „ false““ + Text „an oder aus (true/false)“ – passt zum Leser (ohne Trim = an); danach auf `true` zurückgesetzt | OK |
| B4 | IDEAL: `QrErstesFeld = 'matchcode'` per SQL | Badge „ungültig: „matchcode““ + Banner nennt „QR-Code: erstes Feld“; danach auf `Artikelnummer` gesetzt (Ursprungswert vorher nicht notiert; `Artikelnummer` ist Default und AKE-Wert) | OK |

**Gesamturteil:** B1–B5 behoben und auf DEV bestätigt, kein neuer blockierender Befund (W1, W2 sind Hinweise). Status bleibt **Testbereit**.
Checkliste oben bleibt gültig, außer Punkt 1 (B1 ist erledigt). Zusätzlich: 10. IDEAL-DEV `QrErstesFeld` prüfen, falls dort vor dem QA-Lauf
nicht `Artikelnummer` stand.

## Offene Rueckfragen

1. **Lese-Warnung, Konflikt ADR 0020 Punkt 2 mit der Backlog-Grenze „gleiche Lesesemantik“.** Die ADR verlangt beim **Lesen** eines ungültigen
   Werts Default plus Protokollwarnung. Das hieße rund 100 Lesestellen (Web) und die Dienst-Leser umbauen. Genügt stattdessen eine zentrale
   Prüfung beim Web-Start (E4b) plus Kennzeichnung auf beiden Seiten, bei unveränderten Lesestellen? **Empfehlung:** ja. Die Lesestellen nehmen
   schon heute ihren Default, es fehlt nur die Meldung.
2. **`BdeDefaultArbeitsgang` ist laut Code kein Stammdatenverweis.** Der Wert wird als Name eines neu angelegten Arbeitsgangs gesetzt
   (`BdeDefaultWorkOperationService.cs:34-38`, `:63-65`; Seed-Beispiel „PRODUKTION“, `Program.cs:422`). Freitext mit Vorschlagsliste aus den
   Arbeitsgang-Stammdaten statt Pflicht-Auswahl? **Empfehlung:** ja. Eine Pflicht-Auswahl würde heute gültige Werte abweisen.
3. **`OseonRueckmeldungAktiv` / `SageRueckmeldungAktiv` ohne Leser** (nur Seeds). Im Katalog mit dem Hinweis „derzeit ohne Wirkung“ führen und das
   Aufräumen als Aufgabe anlegen? **Empfehlung:** ja, in diesem Epic nichts löschen.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Lese-Warnung: zentrale Startprüfung statt Umbau der Lesestellen? → **ja (Gerald 2026-11-07)**
2. `BdeDefaultArbeitsgang` Freitext mit Vorschlagsliste statt Pflicht-Auswahl? → **ja (Gerald 2026-11-07)**
3. `OseonRueckmeldungAktiv`/`SageRueckmeldungAktiv` im Katalog als „ohne Wirkung“ führen, Aufräumen als Aufgabe? → **ja, Aufräumen als Aufgabe (Gerald 2026-11-07)**
