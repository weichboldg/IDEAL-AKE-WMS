---
type: backlog
created: 2026-11-14
quelle: "[[2026-11-14-wochenend-review-test-anwenderdoku]]"
status: neu
modul: SData-Epic + SData Teil 2 (Lese-Clients, Register Sage-Test, Geheimnisse in ServiceSettings), Gate der Sage-Schreibclients
stand: "Bündel v1.99.22, 90bbbbd7, feature/2026-08-07-ideal-teile-1-5"
---
# Review-Befunde SData / Geheimnisse / Sage-Test (Teil A3, lesend)

Gegenstand: `git diff main...HEAD` im Bündel-Worktree. Nichts behoben (Schranke 1). Bezug: [[2026-11-13-sdata-teil-2-userfelder-stueckliste-spec]],
[[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]], [[0019-quellen-weiche-view-sdata-je-abgleich]].
Pfade relativ zum Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5/`.

## SD-1 – Verbindungstest schickt das gespeicherte Kennwort an eine frei eingegebene Adresse
- **Schwere:** mittel (Sicherheit; nur Admin)
- **Fundstelle:** `IdealAkeWms/Controllers/ServiceSettingsController.cs:176-204` (`TestSageConnection`), `IdealAkeWms/Views/SageTest/Index.cshtml:93-94,120-121,165`
- **Befund:** `BaseUrl`, `Application`, `Dataset`, `VerifyCertificate` und `Resource` kommen aus dem Request-Body (Formularfelder im Register
  Sage-Test, `sageTestBaseUrl` ist frei editierbar). Ist das Kennwortfeld leer, setzt Zeile 182-191 Benutzer **und** Kennwort aus den
  gespeicherten Zugangsdaten (`SdataZugangsdaten.WebStandAsync`) ein und sendet sie per Basic-Auth (Zeile 200-204) an die eingegebene URL – auch
  über `http://` und mit abgeschalteter Zertifikatsprüfung. Kein Abgleich mit dem gespeicherten `SData:BaseUrl`, keine `@`-Prüfung wie in
  `SdataZugangsdaten.WebStandAsync:105`.
- **Szenario:** Admin trägt `http://<eigener-rechner>:8080/` ein, lässt das Kennwort leer, klickt „Testen“ → der eigene Rechner erhält
  `Authorization: Basic <base64(Benutzer:Kennwort)>`. Damit ist das nach ADR 0024 „nie angezeigte“ SData-Kennwort im Klartext lesbar (auch
  versehentlich bei Tippfehler in der Adresse, über unverschlüsseltes HTTP im Netz).
- **Vorschlag:** Gespeicherte Zugangsdaten nur verwenden, wenn die Formular-Adresse (Schema+Host+Port) dem gespeicherten `SData:BaseUrl`
  entspricht; sonst Kennwort-Eingabe verlangen. Zusätzlich gespeicherte Zugangsdaten nie über `http://` senden. ADR 0024 „Nie im Klartext“ um
  diesen Weg ergänzen.

## SD-2 – Freie SData-Abfragen im Register umgehen die where-/orderBy-Sperre
- **Schwere:** niedrig (nur Admin, nur GET)
- **Fundstelle:** `IdealAkeWms/Controllers/ServiceSettingsController.cs:179-180,208-209`; `IdealAkeWms/Services/SageSdata/SageSdataReadClient.cs:170-172`
  (`orderBy`/`include` unkodiert); `IdealAkeWms/Controllers/SageTestController.cs:58-60`
- **Befund:** (a) Der Verbindungstest nimmt `Resource` frei (z. B. `RealTimeData`-Quelle ohne where) und liest den ganzen Body in den Speicher
  (`ReadAsStringAsync`, erst danach auf 50.000 Zeichen gekürzt). Die Code-Sperre „ohne where gesperrt“ (`SageSdataReadClient.cs:84-86`, 148ff.)
  greift hier nicht. (b) In der Feldliste werden `OrderBy` und `Include` ungeprüft in die URL gesetzt (nur `where` wird für `% # & +`
  kodiert); ein `&` in `orderBy` hängt beliebige Parameter an (z. B. ein zweites `where=`), womit auch die RealTimeData-where-Pflicht (Zeile 67)
  umgangen werden kann.
- **Szenario:** `Resource = dtsTeileverwendungsnachweis?format=json` → Sage liefert ~237.000 Zeilen, Web-Prozess hält den kompletten Body.
- **Vorschlag:** `orderBy`/`include` wie `where` kodieren bzw. `&`, `?`, `#` ablehnen; im Verbindungstest Antwort per Stream nach N Zeichen
  abbrechen. Kein Funktionsumbau nötig.

## SD-3 – Freitext-Weg schreibt Geheim-Key mit abweichender Groß-/Kleinschreibung im Klartext
- **Schwere:** niedrig (nur Admin, fail-safe)
- **Fundstelle:** `IdealAkeWms/Controllers/ServiceSettingsController.cs:46,277-283,315-322`; `IdealAkeWms/Models/ServiceSettingDefinitions.cs:342-346`
  (`TryGet` ordinal); `IdealAkeWms/Data/Repositories/ServiceSettingRepository.cs:39` (`FindAsync(key)`)
- **Befund:** `IstGeheim` prüft den Key **ordinal**. `Create` mit Key `sdata:password` gilt als nicht geheim; `UpsertAsync` sucht per
  `FindAsync` → SQL `WHERE [Key] = @p`. Bei einer CI-Kollation der Spalte `ServiceSettings.Key` trifft das die bestehende Zeile
  `SData:Password` und überschreibt den `dp1:`-Wert mit Klartext; `Delete("sdata:password")` löscht analog das Geheimnis.
- **Szenario:** Folge ist fail-safe (Wert ohne `dp1:` → „nicht entschlüsselbar“, kein Sage-Aufruf), aber das Kennwort steht dann im Klartext in
  DB/Backups – genau was ADR 0024 ausschließen will.
- **Prüfabfrage (Mensch, DEV):** `SELECT collation_name FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ServiceSettings') AND name = 'Key';`
  (CI → Befund trifft zu).
- **Vorschlag:** `IstGeheim` mit `StringComparison.OrdinalIgnoreCase` gegen den Katalog prüfen (eine Zeile).

## SD-4 – Prüfung „SDataKennwortAppsettings“ sieht nur die Dienst-Datei
- **Schwere:** niedrig
- **Fundstelle:** `scripts/deploy/nachlauf.psd1:160-163`; `scripts/deploy/Update-IdealAkeWms.ps1:413,420` (`Get-AppsettingsWert` liest nur `$e.Dienst`)
- **Befund:** ADR 0024 nennt zwei Klartext-Orte (Dienst-`appsettings.json` **und** Web-`appsettings.Production.json`). Die Prüfung misst nur die
  Dienst-Datei; der Text verweist auf die Web-Datei, aber eine dort verbliebene Klartext-Zeile zeigt nie „Abweichung“. Das Web liest den
  Rückfall (`SdataZugangsdaten.Aufloesen:82-85`) weiter aus der Web-Datei.
- **Vorschlag:** Zweite Prüfung (oder `Appsettings` mit Ort `Web`) für `SageLagerbuchung:Password` in den Web-appsettings.

## SD-5 – Install-Skript schreibt das SData-Kennwort weiterhin im Klartext in beide appsettings
- **Schwere:** mittel (Geheimnis im Klartext bei jeder Neuinstallation)
- **Fundstelle:** `scripts/deploy/Install-IdealAkeWms.ps1:82,269-273,366-368`
- **Befund:** `-SageLagerbuchungCredential` schreibt `SageLagerbuchung:Username/Password` im Klartext in Web-`appsettings.Production.json` und
  Dienst-`appsettings.json`. Seit v1.99.14 (ADR 0024) ist der Zielort `/ServiceSettings` (verschlüsselt); das Update-Skript meldet genau diesen
  Zustand anschließend als Abweichung (`Pruefung/SDataKennwortAppsettings`).
- **Szenario:** Neuinstallation IDEAL mit Parameter → Kennwort im Klartext neben dem Programm, Prüfung im ersten Update rot.
- **Vorschlag:** Parameter entfernen bzw. nur noch Hinweis „Zugang nach dem Start in /ServiceSettings eintragen“; Install-Doku nachziehen.

## SD-6 – Fehler im Lagerbuchungs-Schritt blockiert den Sage-Wareneingang im selben Takt (Gate-Prüfung)
- **Schwere:** Hinweis
- **Fundstelle:** `IDEALAKEWMSService/Workers/SageBookingWorker.cs:64-87`
- **Befund:** `ProcessOnceAsync` (Lagerbuchung) und `ProcessSageWareneingangAsync` stehen im **selben** `try`. Wirft der Lagerbuchungs-Schritt
  (z. B. DB-Fehler in `EnqueueMissingAsync`, ungültiges System-Kürzel), läuft der Sage-WE-Schritt in diesem Takt nicht – dauerhaft, solange der
  Fehler besteht. Der Kommentar Zeile 89-90 („ein Fehler hier hält die Sage-Buchungen nicht auf und umgekehrt“) gilt nur für die CSV.
  Schalter-Gates selbst sind korrekt: POST nur in `SageLagerbuchungClient.cs:45`/`SageWareneingangClient.cs:47`, jeweils hinter
  `SageLagerbuchungAktiv` bzw. `SageWareneingangBelegAktiv` (Worker Zeile 67/73) und Enqueue hinter dem globalen Schalter
  (`SageBookingEnqueueingStockMovementRepository.cs:51-53`).
- **Vorschlag:** Zwei getrennte `try`-Blöcke wie beim CSV-Schritt.

## Geprüft ohne Befund
- `SdataZugangsdaten` (eine Stelle, `ToString` ohne Kennwort, kein Rückfall bei nicht entschlüsselbar, Dienst-Provider nur lesend mit
  `DisableAutomaticKeyGeneration`, gleicher `ApplicationName` in `Program.cs:37-39`).
- `/ServiceSettings`: Geheim-Feld ohne `value`, Status aus gespeichertem Wert, Validierung vor jedem Schreiben, Log ohne Wert,
  Einstellungsprüfung überspringt Geheim (`Einstellungspruefung.cs:27`), Antiforgery auf allen POSTs, `[RequireAdminAccess]` auf
  `ServiceSettingsController`, `SageTestController`, `SageSdataFaController`.
- SData-Lesewege im Dienst: alle Leser über `QueryAllAsync` (orderBy-Pflicht, `NurMitWhere`), Werte in `in (…)`/`eq '…'` mit `''`-Escaping
  (`SdataArtikelReader.cs:314`); Audit-Felder in den SData-Writern (`ArticleSageDetailsWriter`, `ArtikelBenutzerfelderWriter`,
  `KontokorrentSyncService`, `StillstandsgruendeSyncService`, `ArtikelLieferantSyncService`) gesetzt, `DateTime.Now`.
- Tests: gezielter Lauf `dotnet test IdealAkeWms.Tests` (Filter Zeitstempel/Manifest/Katalog/Spaltenfilter/Sage-Test/ServiceSettings):
  261 bestanden, 0 Fehler (2026-11-13).
