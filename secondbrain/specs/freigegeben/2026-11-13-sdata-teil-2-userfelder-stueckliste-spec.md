---
type: spec
title: "SData Teil 2: Register Sage-Test (Feldliste, Stückliste live, FA-Struktur live), Lesetest Benutzerfelder, SData-Zugangsdaten verschlüsselt in den Service-Einstellungen"
slug: 2026-11-13-sdata-teil-2-userfelder-stueckliste-spec
status: Testbereit
created: 2026-11-13
updated: 2026-11-13
source_backlog: "[[2026-11-11-sdata-teil-2-userfelder-stueckliste]]"
task: "[[2026-11-13-buendel-v1-99-13-goal-lauf]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
affected_code:
  - "Code-Stand aller Fundstellen: Bündel-Worktree .claude/worktrees/2026-08-07-ideal-teile-1-5, v1.99.12 (IdealAkeWms/AppVersion.cs:5), am 2026-11-13 gelesen"
  - "FA-Struktur live (zieht um): IdealAkeWms/Controllers/SageSdataFaController.cs:20 (Filter RequirePickingOrTrackingOrLeitstandAccess), :40-45 (Gate SageSdataFaAnsichtAktiv), :48-53 (Endpunkt + Zugangsdaten aus appsettings); Views/SageSdataFa/Index.cshtml; Tests IdealAkeWms.Tests/Controllers/SageSdataFaControllerTests.cs:47-55, :87, :100-105"
  - "Menü: IdealAkeWms/Views/Shared/_Layout.cshtml:74-77 (Gate-Lesen), :151-158 (Menüpunkt FA-Struktur (Sage live)), :368-370 (Admin-Einträge Einstellungen/Service-Einstellungen/Standorteinstellungen); Reiter-Muster Views/Shared/_BdeMasterDataTabs.cshtml:12-39"
  - "Gate-Key: IdealAkeWms/Models/AppSettingKeys.cs:79, AppSettingDefinitions.cs:191, Models/Standort/StandortSettingsCatalog.cs:63, README.md:485 + Abschnitt :487, Views/Help/Index.cshtml:924, docs/TESTSZENARIEN.md TS-103 :10647 (103.2 :10660)"
  - "Verbindungstest: IdealAkeWms/Controllers/ServiceSettingsController.cs:17 (RequireAdminAccess), :107-181 (TestSageConnection, Basic-Auth nur aus der Anfrage :141-145); Views/ServiceSettings/Index.cshtml:52-57 (Knopf), :188-239 (Modal), :246-312 (Skript)"
  - "Zugangsdaten heute (appsettings SageLagerbuchung:Username/Password), drei Lesestellen: IDEALAKEWMSService/Services/Sdata/SageQuelleSchalter.cs:48-69 (SdataLeseZielLeser, 12 Aufrufer in Services/Sdata/*), IDEALAKEWMSService/Workers/SageBookingWorker.cs:145-151 (BuildEndpointAsync, Sage-Wareneingang :123) und :170-178 (Lagerbuchung), IdealAkeWms/Controllers/SageSdataFaController.cs:51-52"
  - "Verschlüsselung (wiederverwenden): IdealAkeWms/Program.cs:29-44 (AddDataProtection, Schlüssel unter DataProtection:KeysPath bzw. %ProgramData%\\IdealAkeWms\\DataProtection-Keys, ApplicationName IdealAkeWms, KEIN ProtectKeysWith*); Dienst ohne DataProtection (Suche AddDataProtection: nur Web), Worker-SDK mit ProjectReference auf das Web-Projekt IDEALAKEWMSService/IDEALAKEWMSService.csproj:1, :33-35"
  - "Einstellungen: IdealAkeWms/Models/SettingDefinition.cs:4-15 (SettingType ohne Geheim-Typ), :77-98; ServiceSettingDefinitions.cs:90-99 (Meta SData), :237-246 (Basis SData-Keys, Kategorie Sage-Lagerbuchung); ServiceSettingsController.cs:42-105 (SaveSettings, unverändert = überspringen :59), :205-254 (Freitext-Fallback Create/Edit/Delete zeigt/schreibt Klartext); Views/Shared/_SettingField.cshtml:95, :131 (Textfelder mit value=Wirksam); Views/ServiceSettings/Index.cshtml:162-167 (Orphan-Liste zeigt Value); Services/Settings/Einstellungspruefung.cs:16-38 (Befund enthält den wirksamen Wert :32); Models/ServiceSetting.cs:13-16 + ApplicationDbContext.cs:1017-1025 (Value nvarchar(500), keine Audit-Spalten)"
  - "SData-Lese-Client: IdealAkeWms/Services/SageSdata/SageSdataReadClient.cs:9-27 (Interface, nur GET), :127-128 (Verträge Resources/ResourcesSpecific), :155-162 (BuildQueryUrl ohne include), :164-206 (QueryAllAsync liest alle Seiten), :208-269 (LeseSeiteAsync privat); Web-HttpClient IdealAkeWms/Program.cs:163-169 (20 s); Dienst IDEALAKEWMSService/Services/Sdata/SdataRegistrierung.cs:13-19"
  - "Stücklisten heute: AKE IDEALAKEWMSService/Services/BomCacheSyncService.cs:291-296 (vw_AKE_Kommissionierung_StuecklistenDB je Artikelnummer); IDEAL aus der FA-Struktur (SQL/sage-views/vw_IDEAL-AKE_Kommissionierung_FAListe.sql)"
  - "AKE-Werkstattauftrag (Herkunft unbelegt, P-W1): IDEALAKEWMSService/Services/SageImportService.cs:55-67 (vw_AKE_Kommissionierung_WAListe, Spalte [WA Nummer]); SageZusatzinfoReader.cs:43-51 (vw_IDEAL_AKE_WMS_FAZusatzinformationen: Kaeltemittel, Ventil, Ausfuehrung E/Z, Maschine, Status); Viewtexte nicht im Repo"
  - "Deploy: scripts/deploy/IdealAkeWms.Deploy.psm1:139-156 (Test-OrdnerUndRechte; DataProtection nur Web :152; Platzhalter nur {Web}/{Dienst} :146-147); scripts/deploy/Update-IdealAkeWms.ps1:220 (KeysPath aus Web-appsettings); scripts/deploy/nachlauf.psd1:144-147 (Pruefung mit Appsettings=), :180-183 (Ordner-Muster), :204-205 (v1.71.0-AppsettingsProd), :244-245 (v1.71.0-IDEAL-SData)"
  - "NEU: IdealAkeWms/Controllers/SageTestController.cs + Views/SageTest/{Index,Feldliste,Stueckliste}.cshtml, Views/Shared/_EinstellungenTabs.cshtml, IdealAkeWms/Services/SageSdata/SdataZugangsdaten.cs (Auflösen + Schützen), Brain-Notiz codebase/sage-sdata/sage-sdata-benutzerfelder-lesetest.md"
open_questions:
  - "Rückfall auf appsettings: Sollen SageLagerbuchung:Username/Password in Dienst und Web als Rückfall weiter gelten, solange SData:Username in den Service-Einstellungen leer ist (mit Warnung im Protokoll), bis alle Systeme umgestellt sind? (Empfehlung: ja – sonst steht die Lagerbuchung an jedem System still, das nach dem Deploy noch nicht umgestellt ist; Entfernen des Rückfalls als Folgeaufgabe, sobald Pruefung/SDataKennwortAppsettings überall 0 zeigt.)"
  - "Gate SageSdataFaAnsichtAktiv: Entfällt der Schalter, weil die Ansicht nur noch Admins im Register Sage-Test sehen? (Empfehlung: ja – Key aus Katalog, Seed, Standort-Maske und Layout entfernen, die DB-Zeile per Daten-Skript in einer Transaktion löschen, Muster SQL/108. Alternative: Zeile stehen lassen, sie ist dann wirkungslos.)"
  - "Eigene Rolle für das Register Sage-Test? (Empfehlung: nein – RequireAdminAccess wie /ServiceSettings und /Settings; ein Testwerkzeug für eine Handvoll Admins rechtfertigt keine Rolle mit drei Pflegestellen.)"
  - "Stückliste live – Eingabe: nur Artikelnummer (Sage-Ressourcenliste mehrstufig, entspricht der AKE-Stückliste BomCacheSyncService.cs:291-296) oder zusätzlich FA-Nummer (FA-Positionen aus PpsFaBelegePositionenInfo, mehrstufig über BelIDNachfolger, entspricht der IDEAL-Quelle FAListe)? (Empfehlung: beide – Variante (b), der spätere Ersatz der SQL-Views, braucht an IDEAL die FA-Positionen, an AKE die Ressourcenliste.)"
  - "Schlüsselring im Klartext: Die DataProtection-Schlüssel liegen heute unverschlüsselt im Dateisystem (Program.cs:42-44, kein ProtectKeysWith*). Wer Lesezugriff auf den Ordner und die DB hat, kann das Kennwort entschlüsseln. Reicht das Schutzziel 'Kennwort nicht im Klartext in DB, Backups, DEV-Kopien, Protokollen und Oberfläche', oder sollen die Schlüssel zusätzlich mit DPAPI (Maschine) geschützt werden? (Empfehlung: reicht für diese Spec; DPAPI-Schutz als eigene Aufgabe, weil er den bestehenden Schlüsselring der Anmeldung/Antiforgery mitbetrifft.)"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: "2026-11-13"
---

# SData Teil 2: Register Sage-Test, Lesetest Benutzerfelder, Stückliste live, Zugangsdaten verschlüsselt

Grundlagen: [[2026-11-06-sage-sdata-stammdaten-sync-spec]] (G4, G13/E5, G2) · [[2026-11-06-sage-sdata-folgearbeit]] ·
[[sage-sdata-commonwawiservicesresources]] · [[sage-sdata-realtimedata]] · [[sage-sdata-uebersicht]] · FA live v1.71.0 [[2026-10-30-sage-sdata-fa-ansicht-spec]] ·
DPS-Fragen des PPS-Epics [[2026-11-02-sage-pps-fa-bde-abgleich-spec]] · ADRs [[0008-servicesettings-db-first-mit-typisiertem-katalog]],
[[0020-einstellungen-mit-festem-wertebereich-als-auswahl-und-mit-wirkungsbeschreibung]], [[0022-update-je-zielsystem-per-skript-mit-nachlauf-manifest]],
[[0023-daten-und-einmal-skripte-fuehrt-das-update-skript-mit-bestaetigung-aus]].

**Umsetzungsort:** Bündel-Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Zweig `feature/2026-08-07-ideal-teile-1-5` (v1.99.12). Alle
Datei:Zeile-Angaben beziehen sich auf diesen Stand.

**Verbindlich (Entscheidungen Gerald 2026-11-12, Backlog):** (1) Lesetest listet **alle** lesbaren Felder je Ressource inkl. USER_ mit Beispielwerten;
(2) Stückliste zuerst als **Live-Ansicht**, Ersatz der SQL-Views (b) später; Schreibbarkeit nur **prüfen** ($schema, Service-Liste), **nichts schreiben**;
(3) Lesetest am Sage-AKE-Test, nur GET, über DEV; (4) nicht lesbare Felder → Fragenliste an DPS (zusammen mit den DPS-Fragen des PPS-Epics);
(5) IDEAL-Endpunkt und Zugangsdaten **nur in den Service-Einstellungen**, Kennwort verschlüsselt; (6) Live-Ansichten nur für Admins im Register
**„Sage-Test“**, Menüpunkt „FA-Struktur (Sage live)“ für Anwender entfällt.

**Standort beide:** Der Code geht an beide Standorte. AKE: Register, Lesetest gegen den AKE-Test (sagetest01), Zugangsdaten-Umzug. IDEAL: bleibt
wirkungslos, bis `SData:BaseUrl`/`SData:Dataset` eingetragen sind (heute leer bzw. Lagerbuchung aus, Manifest `v1.71.0-IDEAL-SData`,
`nachlauf.psd1:244-245`). Unabhängig von `ProduktionsauftragHierarchisch` – die Live-Ansichten lesen nur Sage, nicht `FaHierarchy*`.

## Ziel / Nutzen (das Warum)

- **Benutzerfelder:** Für FA, AKE-Werkstattauftrag und Stückliste ist offen, welche USER_-Felder über SData überhaupt lesbar sind (bisheriger Befund:
  FA-USER-Felder über `CommonWawiServicesResources` nicht, [[sage-sdata-commonwawiservicesresources]] „Benutzerdefinierte Felder“). Ein
  wiederholbarer Lesetest mit vollständiger Feldliste entscheidet, was das WMS lesen kann und was DPS liefern muss – an AKE jetzt, an IDEAL sobald der
  Endpunkt da ist.
- **Stückliste:** Bevor ein Abgleich die SQL-Views ersetzt (b), muss sichtbar sein, was SData liefert. Die Live-Ansicht zeigt es ohne Persistenz.
- **Zugangsdaten:** Heute stehen Benutzer und Kennwort im Klartext an zwei Stellen in appsettings (Dienst + Web, [[sage-sdata-uebersicht]] „Zugang“).
  Gerald will IDEAL ohne Dateieingriff einrichten. Ein Ort (DB), Kennwort verschlüsselt, nie angezeigt.
- **Testwerkzeug, nicht Anwenderfunktion:** `RealTimeData` ist laut DPS für Endanwender nicht freigegeben ([[sage-sdata-uebersicht]] Warnung); die
  FA-live-Seite gehört daher nicht ins Anwendermenü.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. Register-Leiste in den Einstellungen (Einstellungen | Service-Einstellungen | Standorteinstellungen | **Sage-Test**), nur Admin.
2. Register Sage-Test mit: Sage-Verbindung testen (umgezogen), **Feldliste**, **Stückliste live**, **FA-Struktur live** (umgezogen, Funktion unverändert).
3. Lesetest am AKE-Test mit der Feldliste (Schritte L1–L10), Ergebnis als Brain-Notiz inkl. Schreibbarkeits-Befund und DPS-Fragenliste.
4. SData-Zugangsdaten als Service-Einstellungen `SData:Username` / `SData:Password` (Kennwort verschlüsselt mit der vorhandenen ASP.NET-Core-
   DataProtection), gelesen von Web und Dienst an den drei bestehenden Lesestellen.
5. Deploy: Ordnerrecht Dienst auf den Schlüsselring, Prüfung „Kennwort noch in appsettings“, Hinweise.

**Out-of-Scope**
- Jeder Schreibaufruf an Sage (auch kein Schreibversuch an der Stückliste – braucht eigene Freigabe, Entscheidung 2).
- Variante (b): Ersatz der SQL-Views/BOM-Cache durch SData (Folge-Spec nach dem Lesetest; G13 des Epics bleibt).
- Persistenz von Benutzerfeldern (welche gebraucht werden, ist nicht festgelegt).
- Umstellung der FA-live-Seite von `RealTimeData` auf `CommonWawiServicesResources` (Folgeaufgabe; hier nur Umzug).
- Weitere offene Epic-Punkte aus dem Backlog: G5 Übergabe Lagerplatz/BDE-Mitarbeiter, P-S1/P-R1/P-F2/P-A1/P-S2, Kommissionierwagen „Übergabe offen“,
  Monitoring der Sage-Queue bei Umbuchungen – bleiben in [[2026-11-06-sage-sdata-folgearbeit]].
- Andere Geheimnisse (ConnectionStrings, MailSettings) bleiben appsettings-only (ADR 0008).

## Fachliche Anforderungen

### F1 Register „Sage-Test“ und Einstellungs-Reiter
- Neue Partial `_EinstellungenTabs.cshtml` nach dem Muster `_BdeMasterDataTabs.cshtml:12-39` (Model = aktiver Reiter), eingebunden oben auf
  `/Settings`, `/ServiceSettings`, `/StandortEinstellungen` und `/SageTest`. Alle vier Controller sind `[RequireAdminAccess]`
  (`SettingsController.cs:11`, `ServiceSettingsController.cs:17`, `StandortEinstellungenController.cs:20`, neu `SageTestController`) – der Reiter
  braucht keine eigene Sichtbarkeitsregel. Die Admin-Menüeinträge (`_Layout.cshtml:368-370`) bleiben; ein Eintrag „Sage-Test“ kommt **nicht** dazu
  (Erreichbarkeit über den Reiter genügt).
- Register-Seite `/SageTest` zeigt oben den Konfigurationsstand: Adresse, Dataset, Benutzer (Name), Kennwort-Status („gesetzt“ | „nicht gesetzt“ |
  „nicht entschlüsselbar“ | „aus appsettings“), TLS-Prüfung an/aus. Fehlt Adresse oder Dataset: gelbe Meldung „Sage-SData nicht konfiguriert:
  …“ und **keine** Anfrage an Sage (IDEAL bis zum Endpunkt).
- Rolle: keine eigene (Rückfrage 3).

### F2 Sage-Verbindung testen (Umzug)
- Knopf und Modal ziehen von `/ServiceSettings` (`Index.cshtml:52-57`, `:188-239`, Skript `:246-312`) in das Register Sage-Test. Auf
  `/ServiceSettings` bleibt an derselben Stelle ein Link „Sage-Verbindung testen → Register Sage-Test“.
- Die Felder Adresse/Application/ServiceContract/Dataset/TLS sind mit den **gespeicherten** Werten vorbelegt und im Modal änderbar (ersetzt das
  bisherige Lesen ungespeicherter Formularwerte, `Index.cshtml:251-254`). Die Action `ServiceSettingsController.TestSageConnection`
  (`:127-181`) bleibt die einzige Testlogik (wiederverwendet, nicht kopiert).
- **Neu:** Benutzer und Kennwort im Modal leer = gespeicherte Zugangsdaten (Auflösung F6, nur serverseitig). Das Kennwort wird nie an den Browser
  gegeben; das Benutzerfeld ist mit dem gespeicherten Namen vorbelegt, das Kennwortfeld leer mit Platzhalter „gespeichertes Kennwort verwenden“.

### F3 Feldliste (Werkzeug des Lesetests)
- Eingaben: **Vertrag** (Auswahl: `CommonWawiServicesResources` | `CommonWawiServicesResourcesSpecific` | `RealTimeData`), **Ressource**
  (Freitext mit Vorschlagsliste der Lesetest-Ressourcen), **where** (bei `RealTimeData` Pflicht, sonst optional), **orderBy** (Pflicht, Sage antwortet
  sonst mit HTTP 500 – [[sage-sdata-commonwawiservicesresources]] „Aufrufregeln“), **include** (optional), **Anzahl Beispielzeilen** (1–5, Default 3).
  Server prüft alles (ADR 0020): Vertrag nur aus der Liste, Anzahl 1–5, Ressource ohne `/`, `?`, `&`.
- Ablauf (nur GET, nacheinander, Sage-Last [[sage-sdata-uebersicht]] „Serverlast“):
  1. `{Ressource}/$schema` – Feldnamen, Typ, `sme:maxLength`, Pflicht, je Ressource `sme:canGet/canPost/canPut/canDelete`. Antwortet die Ressource
     ohne eigenes `$schema` mit Fehler: Hinweis „Schema nicht lesbar“, Ablauf geht mit Schritt 2 weiter.
  2. Eine Seite Daten **ohne select** (`count` = Anzahl, `startIndex=0`), damit alle Felder kommen.
- Ausgabe: Tabelle **Feld | Typ/Länge (Schema) | im Schema | in den Daten | Beispielwerte (je Zeile, gekürzt auf 200 Zeichen)**. Listen aus
  `include` (z. B. `SpecificUserfields` mit `DatafieldName`/`DatafieldValue`) werden flach als Zeilen `SpecificUserfields.<DatafieldName>`
  dargestellt; andere Kind-Listen als `<Kind>[n].<Feld>`. Felder mit Präfix `USER_` bzw. aus `SpecificUserfields` sind gekennzeichnet (Text
  „Benutzerfeld“, nicht nur Farbe). Darüber: Schreibrechte der Ressource laut Schema („Lesen | Anlegen | Ändern | Löschen: ja/nein“), Anzahl
  Felder, `$totalResults`, Dauer, angeforderte URL (ohne Zugangsdaten).
- Tabelle mit Client-Spaltenfilter (`filterable-table`, `data-col-key` je Spalte, ohne Server-Mode – kleine, unpaginierte Diagnose-Ausgabe,
  CLAUDE.md Listen-Views).
- Fehler (HTTP ≠ 2xx, nicht erreichbar, nicht lesbar) erscheinen als rote Meldung mit Status und `$diagnoses[0].message`
  (`SageSdataReadClient.DiagnoseText`, `:272-292`); nie leere Tabelle ohne Meldung.

### F4 Stückliste live
- Eingabe **Artikelnummer** (und, falls Rückfrage 4 = beide, alternativ **FA-Nummer/BelID**), Mandant (Zahl, vorbelegt mit der Zahl nach dem
  letzten `;` im Dataset – am AKE-Test belegt `ake_TEST2026;1` → 1, P-S2 im Epic; am IDEAL-Endpunkt zu prüfen).
- **Artikelnummer:** `CommonWawiServicesResourcesSpecific/PpsRessourcenListenInfo?where=Mandant eq {m} and Artikelnummer eq '{a}'
  &include=PpsRessourcenPositionenResolvedInfo,SpecificUserfields&orderBy=Artikelnummer` (Aufruf und Felder am AKE-Test belegt:
  86003871 → 11 Positionen Ebene 1 + 6 Ebene 2, [[sage-sdata-commonwawiservicesresources]] „…Specific“).
  Anzeige: Kopf (alle Felder + Kopf-Benutzerfelder), darunter die Positionen als eingerückter Baum über `Ebene` und `RowGuid`/`RowGuidParent`
  (bzw. `BelPosID`/`BelPosIdParent`). Feste Spalten: Ebene, Position, Ressourcetyp, Ressourcenummer, Bezeichnung/Matchcode, Menge, Arbeitsgang,
  ArtikelnummerUBG, Platz; je Position aufklappbar „alle Felder“ inkl. Positions-Benutzerfelder (USER_VKPreis, USER_EPDMVERSION, USER_AUSCAD,
  USER_Positionsnummer … – so viele, wie Sage liefert).
- **FA-Nummer (nur falls Rückfrage 4 = beide):** `PpsFaBelegeInfo where BelID eq {n}` → `StrukturID`; dann `PpsFaBelegeInfo where StrukturID eq {s}`
  und `PpsFaBelegePositionenInfo where BelID in (…)` in Blöcken (Muster `SageSdataQuellen.OrBloecke`, `SageSdataFaController.cs:144-145`); Baum über
  `BelIDNachfolger` (am AKE-Test belegt: Struktur 835, [[sage-sdata-commonwawiservicesresources]] „Lesetest Struktur 835“). Spalten wie oben plus
  BelPosID, Soll-/Istmenge.
- Tabelle mit Client-Spaltenfilter wie F3. Fehlerfälle sichtbar: nicht konfiguriert, Sage nicht erreichbar, Artikel/FA nicht gefunden
  („keine Ressourcenliste für Artikel …“), Antwort nicht lesbar.
- Nichts wird gespeichert, kein Bezug zu `CachedBom*`/`FaHierarchy*`.

### F5 FA-Struktur live (Umzug)
- `SageSdataFaController`: Filter `[RequirePickingOrTrackingOrLeitstandAccess]` (`:20`) → `[RequireAdminAccess]`; Gate-Prüfung `:40-45` entfällt
  (Rückfrage 2); Lesen der Zugangsdaten `:48-53` über F6. Seite bekommt den Reiter (aktiv „Sage-Test“) und einen Rücklink ins Register. Abläufe,
  Quellen (`RealTimeData`) und Darstellung bleiben unverändert.
- Menüpunkt `_Layout.cshtml:151-158` und das Lesen des Gates `:74-77` entfallen.

### F6 SData-Zugangsdaten in den Service-Einstellungen
- **Neue Keys** im Katalog (`ServiceSettingDefinitions`, Kategorie wie `SData:*` `:237-240`): `SData:Username` (String) und `SData:Password`
  (neuer `SettingType.Geheim`). Grund für den eigenen Typ statt String: Das generische Feld zeigt den Wert an (`_SettingField.cshtml:131`
  `value=@Model.Wirksam`), die Orphan-Liste zeigt `Value` (`Index.cshtml:166`), die Einstellungsprüfung protokolliert den wirksamen Wert
  (`Einstellungspruefung.cs:32`).
- **Geheim-Feld in `/ServiceSettings`:** Status-Badge („gesetzt“ | „nicht gesetzt“ | „nicht entschlüsselbar – neu eingeben“), Eingabe
  `type=password autocomplete=new-password` **ohne value**, Kontrollkästchen „Kennwort entfernen“. Speichern: leeres Feld = unverändert;
  Eingabe = verschlüsseln und speichern; „entfernen“ = leerer Wert. Bei einem Validierungsfehler einer anderen Einstellung wird die Kennwort-Eingabe
  **nicht** zurückgegeben (Feld leer, Hinweis „Kennwort nicht gespeichert, bitte erneut eingeben“).
- **Verschlüsselung = vorhandene DataProtection** (Web `Program.cs:42-44`, Schlüsselring `DataProtection:KeysPath` bzw.
  `%ProgramData%\IdealAkeWms\DataProtection-Keys`, ApplicationName `IdealAkeWms`). Purpose `IdealAkeWms.ServiceSettings.Geheim.v1`. Gespeichert wird
  `dp1:` + Schutztext. Länge: Schutztext eines Kennworts bis 128 Zeichen passt in `ServiceSettings.Value` nvarchar(500) (`ServiceSetting.cs:13-16`) –
  Server lehnt längere Kennwörter ab; ein Test belegt die Länge. **Keine Migration.**
- **Dienst:** bekommt denselben Schlüsselring nur lesend. Die Pfadermittlung aus `Program.cs:34-40` wandert in eine gemeinsame Methode im
  Web-Projekt (eine Stelle), die beide `Program.cs` nutzen; der Dienst setzt zusätzlich `DisableAutomaticKeyGeneration()` (nur das Web legt
  Schlüssel an). Dass die DataProtection-Typen im Worker-Projekt über die ProjectReference verfügbar sind (`IDEALAKEWMSService.csproj:33-35`), ist
  **nicht belegt** – der Build zeigt es; sonst `<FrameworkReference Include="Microsoft.AspNetCore.App" />` (keine neue Paket-Abhängigkeit).
- **Auflösung an einer Stelle** (`SdataZugangsdaten`, Web-Projekt, von Web und Dienst genutzt), aufgerufen an den drei Lesestellen
  `SdataLeseZielLeser.LesenAsync` (`SageQuelleSchalter.cs:50-57`, Signatur bleibt – die 12 Aufrufer ändern sich nicht),
  `SageBookingWorker.cs:145-151` und `:170-178` (die doppelte Endpunkt-Erzeugung wird dabei zu einer), `SageSdataFaController.cs:48-53`,
  sowie im Verbindungstest (F2) und im Register (F1, F3, F4):
  1. `SData:Username` gesetzt → DB gewinnt. Kennwort aus `SData:Password` entschlüsseln. Nicht entschlüsselbar (fremder Schlüsselring, z. B. DEV mit
     kopierter Prod-DB; Wert ohne `dp1:`, z. B. per SQL im Klartext eingetragen) → **Fehler** „SData-Kennwort nicht entschlüsselbar – in den
     Service-Einstellungen neu eingeben“, **kein** Aufruf an Sage, **kein** Rückfall auf appsettings (Dienst: wie fehlende Konfiguration –
     `SdataLeseZielLeser` wirft wie heute `:66-67`; Buchungs-Worker sendet nicht und protokolliert den Grund).
  2. `SData:Username` leer → (Rückfrage 1) Rückfall `SageLagerbuchung:Username/Password` aus appsettings mit Warnung im jeweiligen Lauf
     „Zugangsdaten aus appsettings – bitte in die Service-Einstellungen übernehmen“ (einmal je Lauf, ohne Werte).
- **Nie im Klartext:** Kennwort erscheint in keiner Seite, keiner JSON-Antwort, keinem Protokoll (`SyncLogs`, Serilog), keiner Einstellungsprüfung
  (Geheim-Keys werden übersprungen), keiner Orphan-Liste. Der Freitext-Fallback `Create`/`Edit`/`Delete` (`ServiceSettingsController.cs:205-254`)
  lehnt Katalog-Keys vom Typ Geheim ab (sonst landet Klartext in der DB bzw. der Schutztext in einem Formular). Änderung am Kennwort schreibt eine
  Serilog-Zeile „SData-Kennwort geändert/entfernt durch {Windows-Benutzer}“ ohne Wert.
- **ADR:** ADR 0008 nimmt Verbindungsgeheimnisse ausdrücklich aus der UI-Tabelle aus („Ausnahmen, die appsettings-only bleiben“). Entscheidung 5
  weicht davon für die SData-Zugangsdaten ab → **neuer ADR** „SData-Zugangsdaten DB-first, Kennwort mit DataProtection verschlüsselt“ (ADR 0008 wird
  nicht umgeschrieben; der neue ADR nennt die Abweichung und das Schutzziel aus Rückfrage 5).

### F7 Lesetest am AKE-Test (Ergebnis = Brain-Notiz)
Ausführung über das DEV-System (AKE) mit der Feldliste (F3) bzw. Stückliste live (F4), nur GET, Dataset `ake_TEST2026;1`. Beispielschlüssel aus
früheren Tests (FA/Struktur 835, Artikel 86003871); existieren sie nicht mehr, nimmt der Lauf je Ressource die erste Zeile nach `orderBy`.
Bis F3 auf DEV steht, lassen sich L1–L10 schon mit dem bestehenden Modal „Sage-Verbindung testen“ ausführen (ServiceContract im Formular
ungespeichert überschreiben, Ressource mit Abfrage).

| # | Abfrage (relativ zu `{Basis}/sdata/ol`) | Festhalten |
|---|---|---|
| L1 | `CommonWawiServicesResources/{DS}/PpsFaBelegeInfo/$schema` und Daten `…/PpsFaBelegeInfo?where=BelID eq 835&orderBy=BelID&count=1&format=json` | alle Felder inkl. `BelegProperty1–4`, `USER_*` mit Typ und Beispielwert |
| L2 | wie L1 für `PpsFaBelegePositionenInfo` (`orderBy=BelPosID`, `count=5`) und `PpsFaBelegePositionenArbeitsgangInfo` (`orderBy=BelAGPosID`) | dito, `PositionProperty1–4` |
| L3 | `…/PpsFaBelegeInfo?where=BelID eq 835&include=SpecificUserfields&orderBy=BelID&count=1&format=json` in **beiden** Verträgen | HTTP-Status; bei 200 die gelieferten `DatafieldName` |
| L4 | `CommonWawiServicesResourcesSpecific/{DS}/$schema` (Ressourcenliste) und `…/SpecificUserfields?orderBy=…&count=5`, `…/CustomFields?orderBy=…&count=5` | welche Tabellen/Objekte die Ressourcen abdecken (Schlüsselfelder) |
| L5 | `CommonWawiServicesResourcesSpecific/{DS}/PpsRessourcenListenInfo?where=Mandant eq 1 and Artikelnummer eq '86003871'&include=PpsRessourcenPositionenResolvedInfo,SpecificUserfields&orderBy=Artikelnummer&count=1&format=json` | Kopf- und Positionsfelder, alle Benutzerfelder (Name, Typ, Länge, Wert) |
| L6 | `$schema` von `PpsRessourcenListenInfo`, `PpsRessourcenPositionenResolvedInfo`, `PpsRessourcenPositionenInfo`, `Artikel/ArtikelStueckliste` | `sme:canPost/canPut/canDelete` je Ressource (Schreibbarkeit, Entscheidung 2) |
| L7 | `CommonWawiServices/{DS}/$schema` | Service-Operationen, deren Name `Ressource`, `Stueck`, `Fertigungsauftrag` oder `Userfield` enthält, mit Request-Feldern |
| L8 | `RealTimeData/{DS}/dtsFaBelegsuche?where=BelID eq 835&format=json` (ohne select) und `…/dtsAbfLOGFertigungsauftraege?where=BelID eq 835&format=json` | USER-Feldnamen mit Tabellenpräfix → zu welcher Sage-Tabelle die AKE-FA-Felder gehören (nur Beleg; RealTimeData ist für den Betrieb nicht freigegeben) |
| L9 | Werkstattauftrag AKE: Ressource(n) zum Ergebnis von **P-W1** (unten) | alle Felder inkl. USER_ |
| L10 | Am IDEAL-Endpunkt (sobald eingetragen): L1–L7 erneut | Unterschiede AKE ↔ IDEAL (eigener Abschnitt in der Notiz) |

**Ergebnis-Notiz** `secondbrain/codebase/sage-sdata/sage-sdata-benutzerfelder-lesetest.md` (Hauptcheckout): je Ressource Tabelle Feld | Typ | Länge |
Beispiel | Benutzerfeld ja/nein; Abschnitt „Schreibbarkeit Stückliste“ (L6/L7, ohne Schreibversuch); Abschnitt **„Fragen an DPS“**: je nicht lesbarem,
fachlich vermutetem Feld eine Frage (Feld, Sage-Tabelle laut L8/P-W1, gewünschter Lesevertrag/Ressource), verlinkt aus
[[2026-11-02-sage-pps-fa-bde-abgleich-spec]] (DPS-Fragen des PPS-Epics, Entscheidung 4). [[sage-sdata-commonwawiservicesresources]] bekommt einen
Verweis darauf (additiv).

**P-W1 – Was ist der „Werkstattauftrag“ an AKE?** (Sage-DB AKE, z. B. `ake_TEST2026` auf AKESQL20; nur lesen). Aus dem Code belegt ist nur, dass
die AKE-FA-Quelle eine View mit Spalte `[WA Nummer]` ist (`SageImportService.cs:55-67`) und die FA-Zusatzinfos aus einer View kommen
(`SageZusatzinfoReader.cs:43-51`); welche Sage-Tabellen dahinterstehen, steht nicht im Repo.
```sql
SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.vw_AKE_Kommissionierung_WAListe'));
SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.vw_IDEAL_AKE_WMS_FAZusatzinformationen'));
-- danach je Tabelle <T> aus beiden Viewtexten:
SELECT OBJECT_NAME(object_id) AS Tabelle, name, TYPE_NAME(user_type_id) AS Typ, max_length
FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.<T>') AND name LIKE N'USER[_]%' ORDER BY name;
```
Festhalten: Viewtexte nach `SQL/sage-views/`, Tabellen und USER-Felder des Werkstattauftrags; danach L9. Bis dahin ist jede Aussage über den
Werkstattauftrag **unbelegt**.

## Technischer Lösungsentwurf

- **Lese-Client erweitern, nicht neu bauen** (`SageSdataReadClient.cs`): (a) optionaler Parameter `include` in `BuildQueryUrl`/`QueryAllAsync`
  (`:155-162`, `:164-165`; Default `null`, bestehende Aufrufer unverändert); (b) neue Methode für **genau eine Seite** (`count`, `startIndex=0`) auf
  Basis des privaten `LeseSeiteAsync` (`:208-269`, wirft wie dort) – `QueryAllAsync` liest sonst alle Seiten; (c) neue Methode `GetSchemaAsync`
  (GET `{Ressource}/$schema`, liefert den XML-Text, gleicher Header/TLS/Auth-Weg). Alle drei nur GET. Generischer Zeilentyp `JsonElement` für Feldliste
  und Stückliste (Felder sind nicht vorab bekannt). Für `RealTimeData` in der Feldliste: vorhandenes `GetAsync` (`:70-123`, where-Pflicht `:73-74`).
- **SageTestController** (`[RequireAdminAccess]`, MVC): `Index` (Stand + Verbindungstest), `Feldliste` (GET-Formular), `Stueckliste` (GET-Formular).
  Reine Aufbereitung (Schema-XML → Feldliste, JSON → flache Felder, Positionen → Baum) als statische, unit-testbare Klassen neben dem Controller
  (Muster `SageSdataFaTreeBuilder`). Keine Repository-/DB-Zugriffe außer ServiceSettings-Lesen.
- **Zugangsdaten:** `SdataZugangsdaten` (Web-Projekt `Services/SageSdata/`): `Schuetzen(string)`, `Aufloesen(dbUser, dbPasswort, IConfiguration,
  IDataProtector)` → `(Benutzer, Kennwort, Quelle, Fehler/Warnung)`. Web: Protector aus DI; Dienst: einmal erzeugter Protector aus demselben
  Schlüsselring (lesend). `SettingType.Geheim` in `SettingDefinition.cs:4-15`, `_SettingField.cshtml` (eigener Zweig), `SettingValidator`/
  `SettingItem` (Geheim nie als Wert), `ServiceSettingsController.SaveSettings` (Zweig Geheim vor `:59`), `Einstellungspruefung` (Geheim überspringen).
- **Reiter:** `_EinstellungenTabs.cshtml` (Bootstrap `nav-tabs`, `aria-current`), keine neue Bibliothek. Frontend-Arbeit → Skill `frontend-design`
  (CLAUDE.md), Kontrast WCAG AA, Benutzerfeld-Kennzeichnung als Text.
- **Umsetzungsreihenfolge (ein Lauf, je Schritt ein Commit):** (1) Zugangsdaten F6 inkl. Tests; (2) Reiter + Register + Umzug Verbindungstest/FA live
  F1/F2/F5; (3) Client-Erweiterung + Feldliste F3; (4) Stückliste live F4; (5) Doku, Manifest, Version. Lesetest F7 im DEV-Test nach Schritt 3.

## Migrations-/SQL-Auswirkungen

- **Keine EF-Migration, keine Schemaänderung, `00_FreshInstall.sql` unverändert:** neue Keys sind Zeilen in `ServiceSettings` (Value nvarchar(500)
  reicht, F6); `SettingType` ist nur Code.
- **Nur falls Rückfrage 2 = ja:** Daten-Skript `SQL/<nächste freie Nummer bei Umsetzung>_RemoveAppSettingSageSdataFaAnsichtAktiv.sql`
  (heute zuletzt SQL/146 belegt, `nachlauf.psd1:91-92`; die parallele Spec Wareneingang Teil 3 belegt evtl. 147 – Nummer beim Umsetzen prüfen):
  `DELETE FROM dbo.AppSettings WHERE [Key] = N'SageSdataFaAnsichtAktiv'`, idempotent, ohne ROLLBACK und ohne sqlcmd-Direktiven (ADR 0023,
  Ausführung Transaktion). Muster `SQL/108`. Nicht destruktiv für Fachdaten.
- `SQL/AgentJobs/*`: nicht betroffen (kein Pflichtfeld, kein MERGE).

## Einstellungen

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| `SData:Username` (neu, ServiceSettings) | String | frei (Sage-Benutzername), max. 100 Zeichen, Server trimmt | leer | Benutzer der SData-Anmeldung. Gesetzt = DB gewinnt; leer = Rückfall appsettings (Rückfrage 1) | Dienst: Sage-Buchungs-Worker (Lagerbuchung, Sage-Wareneingang), alle SData-Abgleiche; Web: Register Sage-Test, FA-Struktur live | beide (IDEAL erst mit Endpunkt) | „Gilt auch für die Schreibwege an Sage (Lagerbuchung, Wareneingang). Falscher Benutzer = Buchungen schlagen fehl.“ |
| `SData:Password` (neu, ServiceSettings) | Geheim (neuer Typ) | frei, 1–128 Zeichen; nie angezeigt; gespeichert verschlüsselt `dp1:…` | leer | Kennwort zu `SData:Username`; nicht entschlüsselbar = kein Sage-Aufruf, Meldung | wie `SData:Username` | beide | wie oben + „Nach dem Wechsel der Server-Schlüssel (DataProtection-Ordner gelöscht) neu eingeben.“ |
| `SData:BaseUrl`, `SData:Application`, `SData:Dataset`, `SageLagerbuchungSslZertifikatPruefen` (geändert: nur Verwendungstext) | wie bisher | wie bisher | wie bisher | unverändert | Meta `ServiceSettingDefinitions.cs:90-99`: „FA-Struktur live“ → „Register Sage-Test“; Beschreibung `:246` „FA-Ansicht Sage live“ → „Sage-Test“ | wie bisher | – |
| `SageSdataFaAnsichtAktiv` (AppSettings, **entfällt** falls Rückfrage 2 = ja) | Bool | – | – | – | – | – | – |
| appsettings `SageLagerbuchung:Username/Password` (Dienst + Web) | Datei | – | – | nur noch Rückfall (Rückfrage 1), Warnung im Lauf | wie heute | beide | Manifest-Prüfung meldet verbliebenes Klartext-Kennwort |

## Audit-Feld-Auswirkungen

Keine `AuditableEntity` betroffen. `ServiceSettings` hat keine Audit-Spalten (`ServiceSetting.cs:5-25`, `ApplicationDbContext.cs:1017-1025`) – wie
bei allen Service-Einstellungen. Nachvollziehbarkeit der Kennwort-Änderung über die Serilog-Zeile aus F6 (Wer, wann, geändert/entfernt; nie der
Wert). Lesetest und Live-Ansichten schreiben nichts.

## Akzeptanzkriterien

1. Als Admin zeigen `/Settings`, `/ServiceSettings`, `/StandortEinstellungen` und `/SageTest` dieselbe Reiterleiste mit vier Reitern, der aktuelle
   ist markiert (`aria-current`). Als Nicht-Admin führt `/SageTest` zur Zugriffsverweigerung.
2. Im Hauptmenü gibt es für niemanden „FA-Struktur (Sage live)“. `/SageSdataFa` als Benutzer mit Rolle picking (ohne Admin) → Zugriff verweigert;
   als Admin erreichbar über Register Sage-Test → Link, Ablauf wie TS-103 (Struktur 835 am AKE-Test).
3. `/ServiceSettings` hat keinen Verbindungstest-Modal mehr, sondern einen Link ins Register. Im Register ist das Modal mit den gespeicherten Werten
   vorbelegt; mit leerem Benutzer/Kennwort und gespeicherten Zugangsdaten liefert `$schema` HTTP 200. Weder HTML noch JSON-Antwort enthalten das
   Kennwort (Browser-Entwicklertools: Quelltext und Netzwerk).
4. `/ServiceSettings` zeigt für `SData:Password` nur den Status und ein leeres Kennwortfeld. Nach Eingabe und Speichern steht in
   `SELECT [Value] FROM dbo.ServiceSettings WHERE [Key] = N'SData:Password'` ein Wert mit Präfix `dp1:`, nicht das Kennwort. Erneutes Speichern
   mit leerem Feld lässt den Wert unverändert; „Kennwort entfernen“ leert ihn.
5. Mit gesetzten DB-Zugangsdaten und **entfernten** `SageLagerbuchung:*`-Einträgen in Dienst- und Web-appsettings laufen am AKE-Test:
   Sage-Lagerbuchung (eine Testbuchung), ein SData-Abgleich (z. B. Lagerplätze im Modus Vergleich), Feldliste, Stückliste live, FA-Struktur live.
6. (Rückfrage 1 = ja) Mit leerem `SData:Username` und gesetzten appsettings-Werten läuft alles wie vor dem Deploy; der Lauf „Sage-Lagerbuchung“
   und ein SData-Abgleichslauf zeigen je eine Warnung „Zugangsdaten aus appsettings …“.
7. Steht in `SData:Password` ein nicht entschlüsselbarer Wert (per SQL `UPDATE … SET [Value] = N'klartext'` oder DB von einem anderen System),
   sendet der Dienst nichts an Sage, das Protokoll nennt „SData-Kennwort nicht entschlüsselbar – in den Service-Einstellungen neu eingeben“, das
   Register zeigt denselben Status, und es gibt keinen Rückfall auf appsettings.
8. Das Kennwort (Klartext) steht nach AK 4–7 in keiner `SyncLogs`-Zeile, keiner Serilog-Datei von Web und Dienst und keinem Lauf
   „Einstellungsprüfung“ (Suche nach dem Testkennwort). `/ServiceSettings/Edit/SData:Password` und `Create` mit diesem Key werden abgelehnt.
9. Feldliste mit `CommonWawiServicesResources`, `PpsFaBelegeInfo`, `where=BelID eq 835`, `orderBy=BelID`, Anzahl 1 zeigt jedes Feld des Schemas mit
   Typ/Länge, die Spalten „im Schema“/„in den Daten“, Beispielwerte, die Kennzeichnung „Benutzerfeld“ und die Schreibrechte der Ressource.
   Ohne orderBy, mit Anzahl 0 oder 6, mit Vertrag außerhalb der Liste oder `RealTimeData` ohne where: Meldung, keine Anfrage an Sage.
10. Feldliste mit `include=SpecificUserfields` (L5-Ressource) zeigt die Benutzerfelder als eigene Zeilen `SpecificUserfields.<Name>`.
11. Stückliste live mit Artikel 86003871 (AKE-Test) zeigt den Kopf mit Benutzerfeldern und die Positionen eingerückt nach Ebene (Ebene 1 und 2),
    je Position aufklappbar alle Felder inkl. Benutzerfelder. Unbekannte Artikelnummer → „keine Ressourcenliste für Artikel …“, keine leere Tabelle.
    (Rückfrage 4 = beide:) FA 835 zeigt die Struktur mit Positionen und BelPosID.
12. Ein Unit-Test belegt, dass Feldliste und Stückliste über den Lese-Client ausschließlich GET senden (HttpMessageHandler-Fake prüft die Methode),
    und dass `include`/Ein-Seiten-Lesen die URL wie in F3/F4 bauen.
13. IDEAL-DEV ohne `SData:BaseUrl`: Register zeigt „Sage-SData nicht konfiguriert: SData:BaseUrl“, Feldliste und Stückliste senden nichts; FA-Liste,
    FA-Struktur, Kommissionierung und Dienstläufe wie vorher.
14. Die Notiz `sage-sdata-benutzerfelder-lesetest` enthält L1–L8 (L9 nach P-W1) mit allen Feldern je Ressource, den Schreibbarkeits-Befund
    (L6/L7) und den Abschnitt „Fragen an DPS“; [[2026-11-02-sage-pps-fa-bde-abgleich-spec]] verlinkt ihn.
15. Update-Skript Schritt 5a meldet den Eintrag „Dienst liest DataProtection-Schlüssel“ als OK bzw. stellt das Leserecht her; die Nachlauf-Liste
    zeigt die Prüfung „Kennwort noch in appsettings“ mit Datei, solange es dort steht.
16. Build und alle Tests grün; Drift-Guard (`ServiceSettingDefinitionsTests`) kennt `SData:Username`/`SData:Password`; Manifest-Drift-Test grün.

## Test-Szenarien

Neues Kapitel **TS-<nächste freie Nummer>** in `docs/TESTSZENARIEN.md` (heute zuletzt TS-129; parallele Specs prüfen) „Register Sage-Test und
SData-Zugangsdaten“; TS-103 (`:10647`) anpassen: 103.2 ist überholt (kein Menüpunkt, Zugriff nur Admin), Vorbedingung „appsettings.Production.json
mit SageLagerbuchung“ → Service-Einstellungen. Index [[testszenarien-index]] nachziehen.

Skizze:
- **Vorbedingungen:** AKE-DEV mit `SData:BaseUrl` = AKE-Test, `SData:Dataset` = `ake_TEST2026;1`, Zugangsdaten des SData-Servicebenutzers bekannt;
  Admin-Benutzer und Benutzer mit Rolle picking ohne Admin; IDEAL-DEV ohne `SData:BaseUrl`.
- **Schritte:** (1) Reiter auf allen vier Seiten (AK 1); (2) Zugriff picking (AK 2); (3) Kennwort in `/ServiceSettings` setzen, SQL-Prüfung `dp1:`
  (AK 4); (4) appsettings-Einträge entfernen, Dienst neu starten, Testbuchung + Abgleich + Register (AK 5); (5) Rückfall: `SData:Username` leeren,
  appsettings zurück (AK 6); (6) Klartext per SQL eintragen (AK 7), danach korrekt neu setzen; (7) Suche nach dem Testkennwort in Protokoll und
  Log-Dateien (AK 8); (8) Feldliste PpsFaBelegeInfo, mit include, Negativfälle (AK 9, 10); (9) Stückliste 86003871 und unbekannter Artikel (AK 11);
  (10) IDEAL-DEV (AK 13); (11) Update-Skript gegen DEV: Schritt 5a und Nachlauf (AK 15).
- **Negativfälle:** fremder Schlüsselring (DB-Kopie), leeres orderBy, RealTimeData ohne where, Sage nicht erreichbar (falsche Adresse),
  Kennwort > 128 Zeichen, Nicht-Admin per URL auf `/SageTest/Feldliste`.

## Etappen (nur bei epic: true)

Entfällt – kein Epic. Reihenfolge der Commits siehe „Technischer Lösungsentwurf“.

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt | – | – |

## Deploy

*Vorläufig – der QA-Lauf bestätigt gegen den echten Diff.*

- **Web-App:** ja (Register, Controller, Views, Einstellungstyp, Zugangsdaten-Auflösung, Layout).
- **Service:** ja (Zugangsdaten-Auflösung, DataProtection lesend).
- **Migration:** nein. Daten-Skript nur bei Rückfrage 2 = ja.
- **Reihenfolge:** Web vor Dienst (Standard des Update-Skripts). Erst **nach** dem Deploy Benutzer/Kennwort in `/ServiceSettings` eintragen, Lauf
  „Sage-Lagerbuchung“ prüfen, dann die appsettings-Einträge entfernen. IDEAL: Zugangsdaten erst mit dem SData-Endpunkt.
- **Skript-Anpassung (Teil der Umsetzung):** `IdealAkeWms.Deploy.psm1:146-147` kennt neu den Platzhalter `{DataProtection}` (ermittelter
  Schlüsselpfad aus `:141` bzw. `Update-IdealAkeWms.ps1:220`).
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, Version = Version dieser Umsetzung `<v>`):
  - `<v>-DataProtectionDienst` – Typ **Ordner**, Firma beide, Phase Vorher, Ausführung automatisch; `Pfad='{DataProtection}'`, `Identitaet='Dienst'`,
    `Recht='Lesen'`, `Pflicht=$true`, `Herstellen=$true`; Text „Der Dienst entschlüsselt das SData-Kennwort mit dem Schlüsselring der Web-App und
    braucht Leserecht.“
  - `Pruefung/SDataKennwortAppsettings` – Typ **Pruefung**, Firma beide, Phase Nachher, Ausführung anzeigen; `Appsettings='SageLagerbuchung:Password'`
    (Muster `nachlauf.psd1:144-147`); Text „SData-Kennwort steht noch im Klartext in {Datei}. Erst in den Service-Einstellungen eintragen und den
    Lauf Sage-Lagerbuchung prüfen, dann aus der Datei entfernen (auch Web: appsettings.Production.json).“
  - `<v>-SDataZugang` – Typ **Einstellung**, Firma beide, Phase Nachher, anzeigen; Text „Service-Einstellungen → SData: Benutzer und Kennwort
    eintragen (Kennwort wird verschlüsselt gespeichert). IDEAL erst mit SData-Endpunkt. Ist im Web DataProtection:KeysPath gesetzt, braucht die
    Dienst-appsettings.json denselben Wert.“
  - `<v>-SageTestInfo` – Typ **Info**, Firma beide, Phase Nachher, anzeigen; Text „Menüpunkt FA-Struktur (Sage live) entfällt; Admins: Einstellungen
    → Reiter Sage-Test. Anwender informieren, falls sie die Seite genutzt haben.“
  - nur bei Rückfrage 2 = ja: `SQL/<n>` – Typ **Daten**, Firma beide, Phase VorStart, Ausführung **Transaktion**, `Datei='<n>_RemoveAppSettingSageSdataFaAnsichtAktiv.sql'`,
    `Spur='SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.AppSettings WHERE [Key] = N''SageSdataFaAnsichtAktiv'') THEN 0 ELSE 1 END'`, Text „Löscht den
    entfallenen Schalter (idempotent); Spur 1 = nichts zu tun.“
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
- **Brain/Doku-Pflichten der Umsetzung:** neuer ADR (F6), `fallstricke.md` (Schlüsselring-Verlust bzw. DB-Kopie → Kennwort neu eingeben, mit Warum),
  [[services]] (neue Keys), [[sage-sdata-uebersicht]] „Zugang“ (Zugangsdaten jetzt DB), README (AppSettings-Zeile `:485`, Abschnitt `:487`),
  Hilfeseite (`Help/Index.cshtml:924` + Register Sage-Test), Anwender- und Brain-Changelog, [[feature-map]], Version in beiden `AppVersion.cs`.

## Offene Rueckfragen

1. **Rückfall auf appsettings:** Sollen `SageLagerbuchung:Username/Password` als Rückfall gelten, solange `SData:Username` leer ist (mit Warnung),
   bis alle Systeme umgestellt sind? *Empfehlung:* ja; Entfernen des Rückfalls als Folgeaufgabe, sobald `Pruefung/SDataKennwortAppsettings`
   überall 0 zeigt.
2. **Gate `SageSdataFaAnsichtAktiv`:** Entfällt der Schalter? *Empfehlung:* ja, Key aus Katalog/Seed/Standort-Maske/Layout entfernen und die
   DB-Zeile per Daten-Skript (Transaktion, Muster SQL/108) löschen. Alternative: Zeile stehen lassen (wirkungslos).
3. **Eigene Rolle für Sage-Test?** *Empfehlung:* nein, `RequireAdminAccess` wie die übrigen Einstellungsseiten.
4. **Stückliste live – Eingabe:** nur Artikelnummer (Ressourcenliste, AKE-Quelle) oder zusätzlich FA-Nummer (FA-Positionen über `BelIDNachfolger`,
   IDEAL-Quelle)? *Empfehlung:* beide.
5. **Schlüsselring im Klartext:** Reicht das Schutzziel „nicht im Klartext in DB, Backups, DEV-Kopien, Protokollen, Oberfläche“, oder zusätzlich
   DPAPI-Schutz der Schlüsseldateien? *Empfehlung:* reicht hier; DPAPI als eigene Aufgabe (betrifft Anmeldung/Antiforgery mit).

## Freigabe-Nachtrag 2026-11-13 08:39 (Gerald, geht dem Rumpf vor)
- Gemeinsamer /goal-Lauf mit [[2026-11-12-befunde-ake-regression-dev-spec]], [[2026-11-11-wareneingang-teil-3-rahmenabrufe-spec]] und
  [[2026-11-13-sdata-teil-2-userfelder-stueckliste-spec]] auf dem Bündel (v1.99.12). **Versions- und TS-Nummern vergibt der Lauf in
  Lauf-Reihenfolge neu** (die Nummern in den Specs sind Vorschläge; WE Teil 3 und Bugfix schlagen beide TS-130 vor). SQL ab 147.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. Rückfall auf appsettings bis zur Umstellung aller Systeme → **Ja, mit Warnung; Entfernen als Folgeaufgabe (Gerald 2026-11-13).**
2. Gate SageSdataFaAnsichtAktiv entfernen + Zeile per Daten-Skript löschen → **Ja, wie empfohlen (Gerald 2026-11-13).**
3. Eigene Rolle für Sage-Test → **Nein, nur Admin (RequireAdminAccess) (Gerald 2026-11-13).**
4. Stückliste live: Artikelnummer und/oder FA-Nummer → **Beide (Gerald 2026-11-13).**
5. Schlüsselring: Schutzziel reicht / DPAPI zusätzlich → **Schutzziel reicht; DPAPI als eigene Aufgabe (Gerald 2026-11-13).**

## QA-Nachweis 2026-11-13 (qa-agent) – Ergebnis: NICHT Testbereit (1 Befund mit Fix-Bedarf)

Geprüfter Stand: Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, Branch `feature/2026-08-07-ideal-teile-1-5`, HEAD `0b1cc9a2`;
Commit-Bereich dieser Spec `a8dd9d2e..0b1cc9a2` (`7f3b1a66`, `cd38fbaa`, `0b1cc9a2`). Standort laut Frontmatter: beide.

### 1. Build und Tests
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 28 Warnungen (bestehend).
- `dotnet test`: IdealAkeWms.Tests **2779 bestanden / 1 übersprungen / 0 fehlgeschlagen** (2780); IDEALAKEWMSService.Tests **603 / 0 / 0**.

### 2. Code-Review (selbst, synchron, Diff `a8dd9d2e..0b1cc9a2`)
Gelesen: `SdataZugangsdaten`, `ServiceSettingsController` (SaveSettings-Zweig Geheim, TestSageConnection, Create/Edit/Delete), `SettingValidator`,
`SettingItem`, `Einstellungspruefung`, `_SettingField` (Geheim-Zweig), `SageTestController`, `SageTestAufbereitung`, `SageSdataReadClient`
(QueryPageAsync/GetSchemaAsync/include), `SageSdataFaController`, Views SageTest/*, `_EinstellungenTabs`, `_Layout`, Dienst `SageQuelleSchalter`,
`SageQuelleWeiche`, `SageBookingWorker`, `SageWareneingangService`, 5 SData-Sync-Services, `SQL/147`, `nachlauf.psd1`, `IdealAkeWms.Deploy.psm1`,
`Update-IdealAkeWms.ps1`, ADR 0024, Lesetest-Notiz, TS-131.

| # | Schwere | Befund | Behandlung |
|---|---|---|---|
| **Q-1** | mittel (AK 11 nicht erfüllt) | **Einrückung der Stückliste live hängt an der Server-Kultur.** `Views/SageTest/Stueckliste.cshtml:106` `style="padding-left: @(0.5 + z.Tiefe * 1.25)rem"` formatiert den double mit de-Kultur → `0,5rem` / `1,75rem` / `4,25rem` = ungültiges CSS, wird verworfen. Nur gerade Tiefen (ganzzahliges Ergebnis, `3rem`) rücken ein. Auf DEV belegt: Artikel 86003871 – Ebene-2-Zeilen **nicht** eingerückt; FA 836 – nur die Sub-FA-Zeilen (Tiefe 2) eingerückt, Positionen (Tiefe 1/3) nicht. Unit-Test `RessourcenBaum_86003871_…` prüft nur `Tiefe`, nicht das Markup. | **offen – Fix nötig** (z. B. `.ToString(CultureInfo.InvariantCulture)` oder feste Klassen je Tiefe). Danach TS-131.9.1/131.9.2 erneut. |
| Q-2 | leicht | Stiller Rückfall: `/SageTest/Feldliste?…&Anzahl=abc` → Binder scheitert, `ModelState.Clear()` (`SageTestController.cs:43`) verwirft den Fehler, gelesen wird mit Default 3 (auf DEV belegt). Gleiches für `FaNummer=abc` (wirkt wie „nicht eingegeben“). | offen, Empfehlung: Bindungsfehler vor `Clear()` als Meldung übernehmen. Kein Abnahmehindernis (Admin-Werkzeug, nur GET). |
| Q-3 | leicht | `SageBookingWorker.ProcessOnceAsync`: bei „nicht entschlüsselbar“ schreibt jeder Takt (Default 20 s) einen fehlgeschlagenen Lauf „Sage-Lagerbuchung“; der Wareneingang-Zweig entprellt (`_weZugangGemeldet`), die Lagerbuchung nicht. Beim appsettings-Rückfall je Takt eine Warnzeile (spec-konform „einmal je Lauf“, aber Lauf = Takt). | offen, Empfehlung: gleiche Entprellung wie beim Wareneingang. Kein Abnahmehindernis. |
| Q-4 | leicht / Hinweis | `TestSageConnection` mit leerem Kennwort sendet das **gespeicherte** Kennwort per Basic-Auth an die im Fenster **frei eingetragene** Adresse – ein Admin kann es so an einen eigenen Server schicken (gleiche Macht hat er über `SData:BaseUrl`; Schutzziel Antwort 5). Ein leeres Kennwort ersetzt außerdem einen eingegebenen Benutzer. | Hinweis an Gerald; Option: gespeicherte Zugangsdaten nur bei unveränderter Adresse. |
| Q-5 | leicht | `Pruefung/SDataKennwortAppsettings` prüft nur die **Dienst**-appsettings (`Update-IdealAkeWms.ps1:410`); Web-`appsettings.Production.json` nur über den Text. Kein automatischer Abgleich Dienst-`DataProtection:KeysPath` = Web-Wert (fehlt er: „nicht entschlüsselbar“, fail-safe, sichtbar). | offen, Folgeaufgabe; Hinweistext `v1.99.14-SDataZugang` deckt es manuell ab. |
| Q-6 | leicht (Brain) | F7 verlangt einen additiven Verweis von [[sage-sdata-commonwawiservicesresources]] auf [[sage-sdata-benutzerfelder-lesetest]] – fehlt (Grep 0 Treffer). PPS-Spec verlinkt ihn (AK 14 erfüllt). | offen, eine Zeile im Hauptcheckout. |

Gezielt geprüfte Fehlerklassen ohne Befund: Werte in `<script>` (nur `@Url.Action`), `hidden` + `d-*` (nur `d-none` per classList), parallele Arrays
(keine), Browser-only-Validierung (Feldliste/Stückliste/Kennwortlänge serverseitig), eine Regel an mehreren Stellen (Zugangsauflösung nur in
`SdataZugangsdaten`; alle Leser von `SageLagerbuchung:*` laufen darüber – Grep belegt), Klartext (`ToString`-Overrides an `SdataZugang`,
`SdataLeseZiel`, `SageBookingEndpoint`; Validator ohne Wert; Einstellungsprüfung überspringt Geheim).

### 3. Nachweisart je AK
| AK | Nachweis |
|---|---|
| 1 | Unit (`SageTest_NurAdmin`) + DEV (Reiter auf 4 Seiten, aktiver markiert); Nicht-Admin: Manual-UAT |
| 2 | Unit (`Zugriff_NurAdmin_KeinGateMehr`) + DEV (kein Menüpunkt, FA 835 über Register); picking-Benutzer: Manual-UAT |
| 3 | DEV: Link statt Modal, Modal vorbelegt, leeres Kennwort → gespeicherte Zugangsdaten (HTTP 401 mit Testkennwort, erwartbar). „$schema 200“ mit echtem Kennwort: **Manual-UAT** |
| 4 | Unit (`Speichern_*`) + DEV (`dp1:`, unverändert, entfernen) |
| 5 | **Manual-UAT** (Testbuchung/Abgleich auf DEV gesperrt) |
| 6 | Unit (`Weiche_ZugangAusAppsettings_*`, `Aufloesen_OhneDbBenutzer_*`) + DEV-Web („aus appsettings“, Hinweis); Dienstläufe: Manual-UAT |
| 7 | Unit + DEV-Web (Klartext; fremder Schlüsselring an IDEAL-DEV: Status, Register, Verbindungstest ohne Anfrage); Dienstlauf: Manual-UAT |
| 8 | DEV (SyncLogs 0, Serilog Web/Dienst 0, Edit abgelehnt) + Unit (`Freitext_CreateEditDelete_LehnenGeheimAb`) |
| 9, 10 | DEV gegen AKE-Test (Feldliste, include, Negativfälle) + Unit |
| 11 | DEV: Inhalt korrekt (17 Positionen 11+6, FA-Modus, Negativfälle) – **Einrückung FEHLER (Q-1)** |
| 12 | Unit (`Client_EineSeiteMitInclude_UndSchema_NurGet`) |
| 13 | DEV IDEAL (BaseUrl kurz geleert) + Unit |
| 14 | Notiz vorhanden (L1–L10, Schreibbarkeit, Fragen an DPS U1–U9), PPS-Spec verlinkt; Q-6 |
| 15 | **Manual-UAT** (Update-Skript nicht gegen DEV lauffähig, s. u.); Manifest-Struktur per Drift-Test |
| 16 | grün (s. 1) |

### 4. Geänderte bestehende Tests
- `BereinigungenV179Tests`, `ServiceSettingsControllerTests`: nur Konstruktor (Typänderung).
- `SageSdataFaControllerTests`: Gate-Test ersetzt durch Admin-Attribut-Test – gewollt (Antwort 2/3, AK 2); Meldung `SData:Username` statt `SageLagerbuchung:Username` – gewollt (F6).
- `AppSettingDefinitionsTests` 57 → 56 – gewollt (Gate entfällt). `ColumnFilterSyntaxTests`: `SageTest/Stueckliste` Client-Liste – gewollt (F4).
- `NachlaufManifestDriftTests`: Appsettings-Prüfung darf Phase `Nachher` – gewollt (Manifest). `SageQuelleTests`: nur ergänzt. „Sonstiges“: keine.

### 5. Checkliste / ADR 0020
Keine Migration, `00_FreshInstall` unverändert (richtig). `SQL/147` idempotent, ohne Transaktionssteuerung/sqlcmd-Direktiven; auf IDEAL_WMS_DEV
zweimal ausgeführt: 1 Zeile gelöscht, dann „nichts zu tun“. Manifest: `SQL/147`, `v1.99.14-DataProtectionDienst`, `Pruefung/SDataKennwortAppsettings`,
`v1.99.14-SDataZugang`, `v1.99.14-SageTestInfo` vorhanden. Version 1.99.14 in beiden `AppVersion.cs`, Anwender- und Brain-Changelog vorhanden.
TS-131 in `docs/TESTSZENARIEN.md`, TS-103 angepasst, Index-Zeile 131 vorhanden. ADR 0020: `SData:Username` (≤100 serverseitig, Anzeigename,
Wirkung, Verwendung, Standort, Warnung) und `SData:Password` (Geheim, 1–128 serverseitig, Warnung) vollständig. ADR 0024 gelesen: deckt
Entscheidung, Schutzgrenze und Deploy-Folgen.

### 6. DEV-Deploy und Testlauf
- `dev-deploy.ps1 -Standort AKE` und `-Standort IDEAL`, Quelle Worktree: beide Exit 0, Fußzeile **v1.99.14** = AppVersion.
- DEV-Konfig: `DataProtection:KeysPath` in `D:\Dev\AKE\Service\appsettings.DEV.json` (`D:\Dev\AKE\keys`) und `D:\Dev\IDEAL\Service\appsettings.DEV.json`
  (`D:\Dev\IDEAL\keys`) **ergänzt (bleibt)**. Leserecht Dienstkonto `svc_idealakewms@ake.at` über „Authentifizierte Benutzer: Ändern“ vorhanden.
- Für den Rückfall-Test kurzzeitig `SageLagerbuchung:*` aus der Dienst-DEV-Datei in `D:\Dev\AKE\Web\appsettings.DEV.json` kopiert (ohne Ausgabe),
  danach Originaldatei wiederhergestellt. IDEAL `SData:BaseUrl` kurz geleert und wiederhergestellt. `SData:Username/Password` an AKE- und IDEAL-DEV am
  Ende leer. Testkennwort frei erfunden (nicht das echte).

| Schritt | Ergebnis | Status |
|---|---|---|
| 131.1.1 Reiter 4 Seiten | überall gleich, aktiver markiert | OK |
| 131.1.2 Admin-Menü ohne „Sage-Test“ | ja | OK |
| 131.1.3 / 131.2.2 Nicht-Admin | kein zweiter Benutzer im Browser (Windows-Anmeldung) | NICHT MÖGLICH (Unit belegt Filter) |
| 131.2.1 kein Menüpunkt FA live | ja | OK |
| 131.2.3 FA live 835 über Register | Reiter, Rücklink, 7 FAs, 11 Positionen, 5 Aufrufe (appsettings-Rückfall) | OK |
| 131.2.4 Schalter weg | Standorteinstellungen ohne Schalter | OK |
| 131.2.5 SQL/147 | IDEAL-DEV gelöscht, 2. Lauf „nichts zu tun“; AKE-DEV ohne Zeile | OK |
| 131.3.1/131.3.2 Kennwort setzen | Badge „gesetzt“, DB `dp1:` (159 Zeichen), kein Klartext | OK |
| 131.3.3 leer speichern | Hash des DB-Werts unverändert | OK |
| 131.3.4 Fehler anderer Einstellung | Browser blockt Intervall < 5 clientseitig; Serverpfad über 131.3.5 belegt | OK (über 131.3.5) |
| 131.3.5 129 Zeichen | Meldung ohne Wert + „Kennwort nicht gespeichert …“ am Feld, DB unverändert | OK |
| 131.3.6 entfernen | DB leer | OK |
| 131.3.7 Edit per URL | Warnung, zurück auf Liste | OK (Create: Unit) |
| 131.3.8 Serilog | „SData: Kennwort geändert/entfernt durch AKENB\…“ ohne Wert | OK |
| 131.4.1–131.4.3 echtes Kennwort, Testbuchung, Abgleich | echtes Kennwort nicht verwendet; Buchung/Syncs auf DEV gesperrt | NICHT MÖGLICH |
| 131.4.4 Register liest (Rückfall) | Feldliste, Stückliste, FA live HTTP 200 | OK |
| 131.4.5 Rückfall-Warnung | Register „aus appsettings“ + Hinweis; Dienstläufe gesperrt | OK (Web) / NICHT MÖGLICH (Dienst) |
| 131.5.1 Klartext per SQL | „nicht entschlüsselbar“, Register ohne Anfrage, kein Rückfall trotz appsettings | OK |
| 131.5.2 Dienstlauf | SageLagerbuchungAktiv/Syncs aus | NICHT MÖGLICH |
| 131.5.3 Verbindungstest | „Fehler – SData-Kennwort nicht entschlüsselbar …“, keine Anfrage | OK |
| 131.5.4 fremder Schlüsselring | AKE-`dp1:` in IDEAL-DEV: „nicht entschlüsselbar“, kein Rückfall | OK |
| 131.6.1–131.6.3 Verbindungstest | Link, Vorbelegung, leeres Kennwort → „Zugangsdaten: gespeichert (Service-Einstellungen)“, HTTP 401 (Testkennwort) | OK (200 = Manual-UAT) |
| 131.6.4 Quelltext/Netzwerk | Code: kein value, JSON ohne Kennwort; Entwicklerwerkzeuge im Lauf nicht verfügbar | teilweise → Manual-UAT |
| 131.7.1/131.7.2 | SyncLogs 0, Serilog Web+Dienst AKE/IDEAL 0 Treffer | OK |
| 131.7.3 Einstellungsprüfung | Unit (`SettingItem_UndEinstellungspruefung_*`) | OK (Unit) |
| 131.8.1 Feldliste PpsFaBelegeInfo | Schema 401 → Vertrags-Schema, Rechte nur Lesen, 67 Felder, 3 Benutzerfelder, URL ohne Zugangsdaten | OK |
| 131.8.2 include SpecificUserfields | `…SpecificUserfields.USER_*` als Benutzerfeld mit Typ „(Daten)“ | OK |
| 131.8.4–131.8.8 Negativ | alle Meldungen, keine Anfrage | OK |
| 131.8.9 unbekannte Ressource | „HTTP 404: Die Resource 'Gibtsnicht' …“ | OK |
| 131.8.10 RealTimeData mit orderBy/count | 200, 90 Felder, `KHKPpsFaBelege_USER_*` als Benutzerfeld | OK |
| 131.9.1 Stückliste 86003871 | 17 Positionen (11+6), Kopf 32 Felder/1 Benutzerfeld, je Zeile 74 Felder/5 Benutzerfelder; **keine Einrückung** | **FEHLER (Q-1)** |
| 131.9.2/131.9.3 FA 836 | Titel „FA 836 gehört zur Struktur 835 …“, BelPosID/Soll; Einrückung nur bei Tiefe 2 | **FEHLER (Q-1)** |
| 131.9.4–131.9.6 Negativ | Meldungen wie erwartet | OK |
| 131.9.7 nichts gespeichert | Code ohne DB-Schreibweg | OK (Review) |
| 131.10.1 IDEAL ohne BaseUrl | Register/Feldliste/Stückliste „nicht konfiguriert: SData:BaseUrl“ | OK |
| 131.10.2 IDEAL-Regression | nicht durchgespielt | NICHT MÖGLICH → Manual-UAT |
| 131.11 Update-Skript | `ziele.psd1` kennt kein DEV-Ziel; `-NurPruefen` landet in der System-Auswahl und läuft bei geschlossener Eingabe in eine Endlosschleife („Ungueltig.“) – Zusatzfund, nicht diese Spec | NICHT MÖGLICH |
| TS-103.3–103.13 | 103.3 über 131.2.3 OK; übrige nicht einzeln | Manual-UAT |

### 7. Publish
`<Worktree>\publish\` geleert und neu: `IDEALAKEWMSWeb`, `IDEALAKEWMSWebService`, `SQL`, `deploy`, `publish-info.txt` (Version 1.99.14, Commit
`0b1cc9a2c0308a192713d5ca3f51b8e1fd786d66`). **Achtung:** enthält Befund Q-1 – nach dem Fix neu publishen.

### Deploy (aus dem echten Diff, gilt nach dem Fix)
- Web: ja (`IdealAkeWms/`), Dienst: ja (`IDEALAKEWMSService/`), Migration: nein; Daten-Skript `SQL/147` (Update-Skript, Transaktion).
- Reihenfolge: Backup → Web → `SQL/147` → Dienst (Schritt 5a Ordnerrecht `{DataProtection}`; Dienst-`DataProtection:KeysPath` = Web-Wert) → danach
  Zugangsdaten in `/ServiceSettings`, Lauf „Sage-Lagerbuchung“ prüfen, erst dann `SageLagerbuchung:*` aus beiden appsettings entfernen.
- Nach dem Merge nur neu publishen, wenn der Merge geprüfte Dateien mit parallelen main-Änderungen zusammenführt.

### Offen für den Menschen (nach dem Fix von Q-1)
1. TS-131.9.1/131.9.2: Einrückung Ebene 1/2 bzw. FA-Struktur sichtbar.
2. Echtes SData-Kennwort in `/ServiceSettings` (AKE-Test) eintragen; Register: Verbindungstest `$schema` = 200, Feldliste/Stückliste/FA live lesen (AK 3, 5).
3. Entwicklerwerkzeuge: Quelltext `/ServiceSettings`, `/SageTest` und Netzwerk `TestSageConnection` ohne Kennwort (131.6.4).
4. Testsystem: Testbuchung mit DB-Zugang, ein SData-Abgleich (AK 5); Rückfall mit genau einer Warnung je Lauf (AK 6, 131.4.5).
5. Testsystem: nicht entschlüsselbarer Wert → Lauf „Sage-Lagerbuchung“ fehlgeschlagen, nichts gesendet (131.5.2).
6. Nicht-Admin (picking) auf `/SageTest`, `/SageTest/Feldliste`, `/SageSdataFa` → verweigert (131.1.3, 131.2.2).
7. Update-Skript auf dem Testsystem: Schritt 5a `v1.99.14-DataProtectionDienst`, Prüfung `SDataKennwortAppsettings`, `SQL/147`, Nachlauf (131.11).
8. IDEAL-Regression 131.10.2 und TS-103.4–103.13 als Admin.

## QA-Nachprüfung 2026-11-13 (qa-agent, Reparaturversuch 1) – Ergebnis: **Testbereit**

Review des Diffs `0b1cc9a2..7048071a` (`SageBookingWorker.cs`, `SageTestController.cs`, `Stueckliste.cshtml`, `SageTestSdataTests.cs`):
- **Q-1 erledigt:** `padding-left` mit `CultureInfo.InvariantCulture` formatiert, Quelltext-Wächtertest dazu. DEV (Browser): Artikel 86003871 – Ebene-2-Zeilen
  eingerückt; FA 836 – Tiefen 0/1/2/3 gestaffelt (FA 835, Position, Sub-FA, Sub-FA-Position). TS-131.9.1–131.9.3 **OK**.
- **Q-2 erledigt:** `UngueltigeZahlen()` übernimmt Bindungsfehler vor `ModelState.Clear()` als Meldung und bricht ohne Anfrage an Sage ab. DEV:
  `Feldliste?…&Anzahl=abc` → „Anzahl: keine gültige ganze Zahl.“, keine Tabelle/URL; `Stueckliste?FaNummer=abc` → „FaNummer: keine gültige ganze
  Zahl.“ **OK**. (Meldung nennt den Parameternamen, nicht die Feldbeschriftung – kosmetisch, kein Befund.)
- **Q-3 erledigt:** Lagerbuchung protokolliert „nicht entschlüsselbar“ nur einmal je Zustandswechsel (`_lbZugangGemeldet`, gleiches Muster wie
  Wareneingang); der reguläre Lauf beginnt erst nach der Prüfung. Code-Review OK; Dienstlauf auf DEV gesperrt → Checkliste Punkt 5.
- **Q-6 erledigt:** Verweis in [[sage-sdata-commonwawiservicesresources]] Zeile 93.
- Q-4, Q-5: Hinweise, bleiben in der Checkliste (Punkte 9, 10).

Build 0 Fehler; Tests IdealAkeWms.Tests **2781 / 0 / 1 übersprungen**, IDEALAKEWMSService.Tests **603 / 0** (selbst gelaufen). DEV-Deploy
`dev-deploy.ps1` AKE und IDEAL aus dem Worktree: beide Exit 0, Fußzeile v1.99.14. Für den Browser-Test erneut kurz `SageLagerbuchung:*` in
`D:\Dev\AKE\Web\appsettings.DEV.json` (Rückfall), danach Original wiederhergestellt; `SData:Username/Password` an AKE- und IDEAL-DEV leer.
Publish: `<Worktree>\publish\` – DLLs 10:42 nach Commit `7048071a` (10:42:16), `publish-info.txt` Commit `7048071ab236322dd18dcaa4e6b5e3a7ce2cee0c`,
Version 1.99.14 (Zeile „Erstellt“ steht noch auf 10:36 – nur kosmetisch).

Deploy wie oben (Web + Dienst, keine Migration, `SQL/147` Daten, Dienst-`DataProtection:KeysPath` = Web-Wert, Ordnerrecht Schritt 5a).

**Ergänzung der Checkliste für den Menschen:**
9. Q-4 entscheiden: Soll der Verbindungstest die gespeicherten Zugangsdaten nur an die gespeicherte Adresse senden?
10. Q-5: Nach dem Deploy von Hand prüfen, dass `SageLagerbuchung:Password` auch aus dem Web-`appsettings.Production.json` entfernt ist und
    `DataProtection:KeysPath` in Dienst und Web gleich ist (das Update-Skript prüft nur die Dienst-Datei).
11. Q-3 auf dem Testsystem: nicht entschlüsselbarer Wert bei aktiver Lagerbuchung → genau **ein** fehlgeschlagener Lauf, kein weiterer je Takt;
    nach Neueingabe wieder normale Läufe.
