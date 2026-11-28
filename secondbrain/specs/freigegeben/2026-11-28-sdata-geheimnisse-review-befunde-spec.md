---
type: spec
title: SData-Geheimnisse und Sage-Test – Review-Befunde SD-1, SD-2, SD-3, SD-4, SD-6
slug: 2026-11-28-sdata-geheimnisse-review-befunde-spec
status: Testbereit
created: 2026-11-28
updated: 2026-11-28
source_backlog: "[[2026-11-14-review-befunde-sdata]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-11-28-buendel-review-kommissionierung"
branch: "feature/2026-11-28-buendel-review-kommissionierung"
standort: beide
affected_code:
  - "[[2026-11-28-sdata-geheimnisse-review-befunde-bug]]"
  - "IdealAkeWms/Controllers/ServiceSettingsController.cs:176-242 (TestSageConnection: SD-1 Sperre gespeicherter Zugangsdaten, SD-2a Antwort per Stream kappen)"
  - "IdealAkeWms/Controllers/ServiceSettingsController.cs:46,277-323 (IstGeheim + Freitext-Weg Create/Edit/Delete: SD-3)"
  - "IdealAkeWms/Services/SageSdata/SdataZugangsdaten.cs:93-112 (WebStandAsync, neue reine Pruefung fuer SD-1)"
  - "IdealAkeWms/Controllers/SageTestController.cs:57-70 (Feldliste: orderBy/include ohne & ? #, SD-2b)"
  - "IdealAkeWms/Views/SageTest/Index.cshtml:87-90 (Hinweistext im Testfenster)"
  - "IDEALAKEWMSService/Workers/SageBookingWorker.cs:64-87 (zwei getrennte try-Bloecke, SD-6)"
  - "scripts/deploy/Update-IdealAkeWms.ps1:414-415 (Get-AppsettingsWert mit Ort Web, SD-4)"
  - "scripts/deploy/nachlauf.psd1:269-272 (+ neuer Eintrag Pruefung/SDataKennwortAppsettingsWeb, SD-4)"
  - "IdealAkeWms.Tests/Deploy/NachlaufManifestDriftTests.cs:183-191 (Feld Ort zulassen)"
  - "IdealAkeWms.Tests/Services/SageSdata/SageTestSdataTests.cs:202-213,351-366 (Tests erweitern)"
  - "docs/anwender/12-register-sage-test.md:134-136; IdealAkeWms/Views/Help/Index.cshtml:340-342; secondbrain/architektur/adr/0024-*.md (Abschnitt Nie im Klartext)"
open_questions:
  - "SD-3 Umfang: nur IstGeheim ohne Gross-/Kleinschreibung (Backlog-Vorschlag) oder allgemeine Regel im Freitext-Weg 'Key weicht nur in der Schreibweise von einem Katalog-Key ab -> ablehnen', die auch die gleiche Luecke am Einwegtor ProduktionsauftragHierarchisch / Sync:FaZusatzinfoAutoErledigtEnabled schliesst?"
  - "SD-1 http: Gespeicherte Zugangsdaten im Verbindungstest nie ueber http:// senden - auch dann nicht, wenn die gespeicherte SData:BaseUrl selbst mit http:// beginnt (Pruefabfrage im Abschnitt Pruefschritte)?"
epic: false
etappen: []
deploy:
  web: true
  service: true
  migration: false
freigabe_entscheidung: ""
freigabe_von: "Gerald Weichbold"
freigabe_am: 2026-11-28
---

> Fix-Spec zum Defekt-Datensatz [[2026-11-28-sdata-geheimnisse-review-befunde-bug]]. Bezug: [[0024-sdata-kennwort-verschluesselt-in-den-service-einstellungen]],
> [[2026-11-13-sdata-teil-2-userfelder-stueckliste-spec]] (dort Q-4 = SD-1 als Hinweis). Version **v1.99.93 (vorläufig)**, Testkapitel **TS-214
> (vorläufig)**, **keine SQL-Nummer** (keine Schemaänderung). Vergeben bis zu diesem Lauf: v1.99.90/91 (Kommissionierauftrag-Übersicht), v1.99.92,
> SQL/173, TS-213 (Buchungsrechte je Lagerort) – der Dev-Lauf prüft die Nummern neu.
>
> Alle Fundstellen neu belegt im Hauptcheckout `main` (= Bündel v1.99.89). Nicht im Umfang: SD-5 (erledigt), N-B4-4 (geklärt, BaseUrl bleibt).

## Ziel / Nutzen (das Warum)

ADR 0024 verspricht: das SData-Kennwort steht nirgends im Klartext und verlässt den Server nur in Richtung Sage. Drei Wege brechen das heute
(SD-1 Verbindungstest an beliebige Adresse, SD-3 Freitext-Weg mit anderer Schreibweise, SD-4 Klartext in der Web-Datei bleibt unbemerkt).
Zwei weitere Befunde sind Robustheit: SD-2 (Admin-Abfrage kann den Web-Prozess mit ~237.000 Zeilen fluten bzw. Sperren umgehen) und SD-6 (ein
Fehler in der Lagerbuchung hält den Sage-Wareneingang dauerhaft an). Alles kleine, lokale Änderungen ohne Funktionsumbau.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
- SD-1: Verbindungstest nutzt gespeicherte Zugangsdaten nur für die gespeicherte Adresse, nur über `https://`, nie mit schwächerer TLS-Prüfung.
- SD-2: (a) Antwort des Verbindungstests per Stream nach 50.000 Zeichen abbrechen; (b) Feldliste weist `&`, `?`, `#` in orderBy und include ab.
- SD-3: Freitext-Weg Create/Edit/Delete erkennt Geheim-Keys unabhängig von der Groß-/Kleinschreibung (Umfang der Regel: Rückfrage 1).
- SD-4: Update-Skript prüft `SageLagerbuchung:Password` auch in den Web-appsettings.
- SD-6: Lagerbuchung und Sage-Wareneingang im `SageBookingWorker` in getrennten `try`-Blöcken.
- Doku: Anwenderdoku Register Sage-Test (Stolperfalle „Behebung ist geplant“ ersetzen), Hilfe-Seite, ADR 0024 „Nie im Klartext“, Changelog,
  TESTSZENARIEN TS-214.

**Out-of-Scope**
- SD-5, N-B4-4 (siehe oben).
- Entfernen des appsettings-Rückfalls (`SdataZugangsdaten.Aufloesen:82-85`) – Folgeaufgabe laut ADR 0024 „Übergangszustand“.
- where-Pflicht für die freie Ressource im Verbindungstest: das Feld ist bewusst frei (Vorschläge mit `?where=…&orderBy=…`,
  `Views/SageTest/Index.cshtml:132-136`); der Backlog verlangt dort nur den Stream-Abbruch. Sage rechnet die Abfrage serverseitig weiterhin ganz
  (begrenzt durch den 30-s-Timeout, `ServiceSettingsController.cs:198`); geschützt wird nur der Web-Prozess.
- Drosselung der Fehlermails im `SageBookingWorker` (heute je Takt eine Mail, `SyncErrorNotifier.cs:28-73` ohne Entprellung) – unverändert.
- Validierung von `SData:BaseUrl` beim Speichern (https-Pflicht) – nur falls Rückfrage 2 das verlangt, sonst nicht.

## Fachliche Anforderungen

**F1 (SD-1) – Gespeicherte Zugangsdaten nur für die gespeicherte Adresse.** Ist das Kennwortfeld im Testfenster leer **und** gibt es gespeicherte
Zugangsdaten (Quelle „Service-Einstellungen“ oder „appsettings“), sendet der Test nur, wenn alle Bedingungen gelten:
1. Die Fensteradresse ist eine gültige absolute URL ohne Benutzerteil (`@`).
2. Schema, Host und Port der Fensteradresse sind gleich denen von `SData:BaseUrl` (Host ohne Groß-/Kleinschreibung, Standard-Port ergänzt).
   Pfad, Application, ServiceContract, Dataset und Ressource dürfen abweichen (die Zugangsdaten gehen an den Host, nicht an den Pfad).
3. Das Schema ist `https` (Rückfrage 2: auch wenn `SData:BaseUrl` selbst `http://` ist).
4. Die TLS-Prüfung ist im Fenster nicht aus, wenn sie gespeichert an ist.

Sonst: **keine Anfrage**, Antwort `ok = false` mit einer Meldung wie „Gespeicherte Zugangsdaten nur für die gespeicherte Adresse
https://host:port – für eine andere Adresse Benutzer und Kennwort im Fenster eingeben.“ (bzw. „… nie über http://“, „… nicht mit
ausgeschalteter TLS-Prüfung“, „Adresse enthält Zugangsdaten (@)“, „SData:BaseUrl ist nicht gesetzt“). Die Meldung nennt nie Benutzer oder
Kennwort. Mit im Fenster eingegebenem Kennwort bleibt der Test frei wie heute (der Admin schickt dann seine eigene Eingabe). Gibt es keine
gespeicherten Zugangsdaten (Quelle „keine“), bleibt es beim Aufruf ohne Anmeldung wie heute (`ServiceSettingsController.cs:191,200`).

**F2 (SD-2a) – Antwort kappen beim Lesen.** Der Verbindungstest liest höchstens 50.000 Zeichen der Antwort und bricht dann ab (Anzeige mit
„… (gekuerzt)“ wie heute, `ServiceSettingsController.cs:222`). Kopfzeilen und Status unverändert.

**F3 (SD-2b) – Feldliste ohne Parameter-Einschleusung.** `orderBy` und `include` mit `&`, `?` oder `#` werden ohne Anfrage an Sage abgewiesen
(„orderBy ohne &, ? und #.“ / „include ohne &, ? und #.“), wie heute schon die Ressource (`SageTestController.cs:63-64`).

**F4 (SD-3) – Freitext-Weg schreibweisenfest.** Create, Edit (GET/POST) und Delete lehnen `sdata:password` (jede Schreibweise von
`SData:Password`) mit derselben Meldung ab wie heute den exakten Key (`ServiceSettingsController.cs:48-52`). Umfang auf weitere Katalog-Keys:
Rückfrage 1 (Empfehlung: allgemeine Regel, siehe Lösungsentwurf).

**F5 (SD-4) – Web-Datei mitprüfen.** Das Update-Skript zeigt eine Abweichung, wenn `SageLagerbuchung:Password` in der Web-`appsettings.json`
oder `appsettings.<Umgebung>.json` gesetzt ist – eigene Zeile neben der bestehenden Dienst-Prüfung, Text nennt die Datei.

**F6 (SD-6) – Schritte unabhängig.** Ein Fehler im Lagerbuchungs-Schritt hält den Sage-Wareneingang und die Wareneingang-CSV im selben Takt nicht
auf und umgekehrt. Die Gates bleiben wie sie sind (Lagerbuchung nur bei `SageLagerbuchungAktiv`, Sage-WE nur bei `SageWareneingangBelegAktiv`
nicht-Safe gelesen, Q-2-Verhalten „Lesefehler ≠ aus“ bleibt).

## Technischer Loesungsentwurf

**Belegter Ist-Stand (main)**

| Befund | Beleg |
|---|---|
| SD-1: URL komplett aus dem Request | `ServiceSettingsController.cs:179-180` (`SdataUrlBuilder.BuildResourceUrl(req.BaseUrl, …)`) |
| SD-1: leeres Kennwort → gespeicherte Zugangsdaten, auch Benutzer überschrieben | `ServiceSettingsController.cs:182-192` |
| SD-1: TLS aus nach Formular, Basic-Auth an diese URL | `ServiceSettingsController.cs:194-204` |
| SD-1: `@`-Prüfung gibt es nur für die gespeicherte Adresse | `SdataZugangsdaten.cs:105` |
| SD-1: Adresse frei editierbar, Kennwortfeld mit Platzhalter „gespeichertes Kennwort verwenden“ | `Views/SageTest/Index.cshtml:93-94,119-121,164-173` |
| SD-1: einziger Aufrufer von `TestSageConnection` | `Views/SageTest/Index.cshtml:180` (Grep über das Repo, sonst nur Doku) |
| SD-2a: ganzer Body im Speicher, erst danach gekürzt | `ServiceSettingsController.cs:208-209,222` (`ResponseContentRead`, `ReadAsStringAsync`) |
| SD-2b: orderBy/include unkodiert in der URL, nur where kodiert | `SageSdataReadClient.cs:175,177,178` |
| SD-2b: Feldliste prüft Ressource auf `/ ? & #`, orderBy/include nicht | `SageTestController.cs:57-70` |
| SD-2b: Feldliste ist der einzige Weg mit Benutzereingabe in orderBy/include | `SageTestController.cs:105`; übrige Aufrufer `:163,:183,:194,:207,:220`, `SageSdataFaController.cs:73` mit Code-Konstanten |
| SD-3: `IstGeheim` über `TryGet`, ordinal | `ServiceSettingsController.cs:46`; `ServiceSettingDefinitions.cs:449-453` (`d.Key == key`) |
| SD-3: Freitext schreibt/löscht per `FindAsync(key)` | `ServiceSettingsController.cs:283,310,320`; `ServiceSettingRepository.cs:39,61` |
| SD-3: Spalte `ServiceSettings.Key` CI auf AKE-DEV und IDEAL-DEV (`Latin1_General_CI_AS`) | DEV-Nachmessung 2026-11-14 im Backlog; Prod **nicht belegt** (Prüfschritt P-1) – der Fix wirkt unabhängig von der Kollation |
| SD-3 Nebenbefund: gleiche Lücke am Einwegtor | `GuardedServiceSettingRepository.cs:32,40` → `HierarchischeStrukturKeys.cs:13-15` ordinal; beide Keys stehen im Katalog (`ServiceSettingDefinitions.cs:294,301`). `Create("produktionsauftraghierarchisch", "false")` umginge den Wächter und träfe bei CI-Kollation die echte Zeile (abgeleitet aus Code + DEV-Kollation, nicht ausgeführt) |
| SD-4: Prüfung liest nur den Dienst-Ordner | `Update-IdealAkeWms.ps1:415` (`Get-Einstellung $e.Dienst $e.UmgebungDienst …`); Web-Ordner/Umgebung stehen bereit (`:221` `UmgebungWeb`, `$e.Web`) |
| SD-4: Eintrag | `nachlauf.psd1:269-272` |
| SD-4: Web liest den Rückfall weiter | `SdataZugangsdaten.cs:82-85` (Web-`IConfiguration`) |
| SD-6: Lagerbuchung + Sage-WE in einem `try` | `SageBookingWorker.cs:64-87`; `sageWeAn` wird erst in `:73` gesetzt, ein Wurf in `:68` lässt damit auch die CSV (`:108`) aus |
| SD-6: `ProcessOnceAsync` wirft weiter | `SageBookingWorker.cs:512-524` (`throw;`) |

**F1 – eine reine Prüfung, ein Aufruf.** In `SdataZugangsdaten` (eine Stelle laut ADR 0024) eine statische, testbare Funktion, z. B.
`string? SperreGespeicherterZugang(string? fensterAdresse, bool fensterTlsPruefen, string? gespeicherteAdresse, bool gespeichertTlsPruefen)` →
`null` = erlaubt, sonst Meldungstext. Umsetzung mit `Uri.TryCreate(…, UriKind.Absolute)`, Vergleich `Scheme` (OrdinalIgnoreCase), `IdnHost`
(OrdinalIgnoreCase), `Port`; `UserInfo` nicht leer → gesperrt. In `TestSageConnection` im Zweig `:182-192` nach `WebStandAsync`: nur wenn
`z.Quelle != "keine"` die Prüfung mit `stand.BaseUrl` / `stand.ZertifikatPruefen` aufrufen; bei Sperre `Json(new { ok = false, …, error = sperre,
zugang = "gespeichert – nicht verwendet" })` ohne HTTP-Aufruf (Antwortform wie `:187-188`). Die Gesamtantwort `WebStandAsync` holt der Zweig
heute schon; nur `.Zugang` reicht nicht mehr, der Stand wird ganz behalten.

**F2 – Stream statt String.** `SendAsync(message, HttpCompletionOption.ResponseHeadersRead, ct)`, dann `ReadAsStreamAsync` + `StreamReader`
(UTF-8, BOM-Erkennung) und höchstens 50.001 Zeichen lesen (`ReadBlockAsync` in einen Puffer); mehr als 50.000 → kürzen + „… (gekuerzt)“, Antwort
verwerfen (Dispose bricht die Übertragung ab). Als kleine statische Hilfsfunktion (z. B. `LeseHoechstens(Stream, int, ct)` im Controller oder
`SdataZugangsdaten`-Nachbarklasse), damit ein Unit-Test mit `MemoryStream` sie belegt; `TestSageConnection` selbst baut seinen eigenen
`HttpClient` (`:194-198`) und ist nicht unit-testbar.

**F3 – Prüfung an der Vertrauensgrenze.** Zwei Zeilen in `SageTestController.Feldliste` neben `:63-64`. Kein Wächter in `BuildQueryUrl`:
alle anderen Aufrufer übergeben Konstanten, und ein `ArgumentException` dort käme in der Feldliste als HTTP 500 an (der Controller fängt nur
`SdataLeseException`, `:80-87,114`).

**F4 – Freitext-Weg (Rückfrage 1).**
- *Empfehlung (allgemein):* neue private Prüfung `KatalogKeyAndereSchreibweise(key)` = es gibt einen Katalog-Key, der `OrdinalIgnoreCase`
  gleich, aber ordinal ungleich ist → `GeheimAbgelehnt()`-artige Ablehnung mit Text „Key ‚x‘ weicht nur in der Groß-/Kleinschreibung von
  ‚Y‘ ab – Katalog-Einstellungen nur über die Maske pflegen.“ In Create (POST), Edit (POST) und Delete vor `IstGeheim`. Deckt Geheim- und
  Einwegtor-Keys in einer Regel ab, `IstGeheim` und `HierarchischeStrukturKeys` bleiben unverändert (der exakte Key läuft weiter über die
  bestehenden Wächter). Fallstrick 48 (`fallstricke.md:2093`): C#-Vergleich folgt der Spalten-Kollation.
- *Minimal (Backlog-Vorschlag):* `IstGeheim` mit `ServiceSettingDefinitions.All.Any(d => d.Type == SettingType.Geheim &&
  string.Equals(d.Key, key, StringComparison.OrdinalIgnoreCase))`; Einwegtor-Lücke bleibt offen.
- Edit (GET) braucht keine Änderung: `settings.FirstOrDefault(s => s.Key == id)` (`:292`) findet eine andere Schreibweise nicht → `NotFound`.

**F5 – optionales Manifest-Feld `Ort`.** `Get-AppsettingsWert` liest bei `$m['Ort'] -eq 'Web'` aus `$e.Web` / `$e.UmgebungWeb`, sonst wie heute
aus dem Dienst-Ordner. `Format-PruefText` und `Get-PruefStand` laufen bereits über diese eine Funktion (`:418,:422`) – keine weitere Stelle. Neuer
Eintrag im Manifest (siehe Deploy), bestehender Eintrag `Pruefung/SDataKennwortAppsettings` behält Id und Prüfung, nur der Klammerzusatz
„(auch Web: appsettings.Production.json)“ im Text entfällt (die Web-Datei hat jetzt eine eigene Zeile). Drift-Test: `Ort` optional, Werte
`Web|Dienst`, nur zusammen mit `Appsettings`.

**F6 – zwei `try`-Blöcke.** `SageBookingWorker.cs:65-87` aufteilen:
1. `try { if (GetBoolSafeAsync("SageLagerbuchungAktiv")) await ProcessOnceAsync(ct); } catch (OCE) when stopping { break; } catch (Exception ex)
   { LogError(…"Schritt Sage-Lagerbuchung"); NotifyErrorAsync("SageBookingWorker (Sage-Lagerbuchung)", …); }`
2. `try { sageWeAn = await GetBoolAsync(ToggleKey…); if (sageWeAn) … else await SageWareneingangAusAsync(ct); } catch … NotifyErrorAsync(
   "SageBookingWorker (Sage-Wareneingang)", …)` – `sageWeAn` bleibt bei einem Lesefehler `false` (CSV aus, kein `SageWareneingangAusAsync`,
   Q-2 unverändert).
Muster und Kommentar wie die übrigen Schritte (`:91-166`). Der Mail-Betreff ändert sich von „SageBookingWorker (unerwartet)“ auf die zwei
Schrittnamen (`SyncErrorNotifier.cs:45`). Ein ungültiges System-Kürzel bricht weiterhin **beide** Schritte ab, weil beide es lesen
(`:234`, `:366`) – gewollt (nie ohne Kürzel senden).

**Muster:** keine neuen Abstraktionen; eine reine Funktion je Regel (F1, F2) für Tests, sonst Inline-Prüfungen an der Vertrauensgrenze.

## Migrations-/SQL-Auswirkungen

Keine. Kein Schema, keine Migration, keine SQL-Datei, FreshInstall unberührt. Prüfschritte (lesend, Mensch, nicht Teil des Deploys):

- **P-1 (SD-3, Prod-Kollation):** `SELECT collation_name FROM sys.columns WHERE object_id = OBJECT_ID('dbo.ServiceSettings') AND name = 'Key';`
  auf AKE- und IDEAL-Prod. Erwartet `…_CI_…`; der Fix gilt unabhängig davon.
- **P-2 (SD-3, Altlast):** `SELECT [Key] FROM dbo.ServiceSettings WHERE [Key] = N'SData:Password' AND [Key] <> N'SData:Password' COLLATE Latin1_General_CS_AS;`
  Erwartet 0 Zeilen (keine Zeile mit abweichender Schreibweise). Treffer → Gerald, bevor der Fix live geht.
- **P-3 (SD-1, Rückfrage 2):** `SELECT [Value] FROM dbo.ServiceSettings WHERE [Key] = N'SData:BaseUrl';` auf AKE- und IDEAL-Prod. Beginnt ein Wert
  mit `http://`, ist Rückfrage 2 entscheidend.

## Einstellungen

keine. (Kein Key neu oder geändert; F1 liest die bestehenden `SData:BaseUrl` und `SageLagerbuchungSslZertifikatPruefen` nur.)

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

Keine. Keine Entität wird neu beschrieben. Der Freitext-Weg schreibt in den abgelehnten Fällen nichts mehr; der Schreibweg selbst setzt wie bisher
keine Audit-Felder (`ServiceSetting` hat keine, `ServiceSettingRepository.cs:37-57`).

## Akzeptanzkriterien

1. **SD-1:** Kennwort leer, gespeicherte Zugangsdaten vorhanden, Fensteradresse mit anderem Host oder Port als `SData:BaseUrl` → keine HTTP-Anfrage,
   Meldung „Gespeicherte Zugangsdaten nur für die gespeicherte Adresse …“; ein Listener auf der eingegebenen Adresse erhält **keine** Verbindung.
2. **SD-1:** wie 1, aber Adresse mit `http://` (sonst gleicher Host) → keine Anfrage, Meldung „… nie über http://“ (Geltung bei gespeicherter
   http-Adresse nach Rückfrage 2).
3. **SD-1:** wie 1, Adresse gleich, TLS-Haken im Fenster aus bei gespeichert „an“ → keine Anfrage, Meldung zur TLS-Prüfung.
4. **SD-1:** Adresse mit Benutzerteil (`https://x@host:port/`) und leerem Kennwort → keine Anfrage, Meldung „Adresse enthält Zugangsdaten (@)“.
5. **SD-1:** gespeicherte Adresse unverändert (auch andere Ressource, Dataset, Application), Kennwort leer → Test läuft wie heute, Zusatz
   „Zugangsdaten: gespeichert (…)“.
6. **SD-1:** Kennwort im Fenster eingegeben → Test geht an jede eingegebene Adresse wie heute, ohne gespeicherte Werte.
7. **SD-1:** Unit-Test der Prüffunktion: gleiche Adresse mit/ohne Standard-Port und mit anderer Groß-/Kleinschreibung des Hosts = erlaubt; anderer
   Host, anderer Port, http, `@`, ungültige URL, gespeicherte Adresse leer = gesperrt; keine Meldung enthält Benutzer oder Kennwort.
8. **SD-2a:** Unit-Test der Lesefunktion: Stream mit 60.000 Zeichen → Ergebnis 50.000 Zeichen + „… (gekuerzt)“; Stream mit 10 Zeichen →
   unverändert. Im Code ist kein `ReadAsStringAsync` mehr in `TestSageConnection`.
9. **SD-2b:** Feldliste mit `orderBy = "BelID&where=1 eq 1"` bzw. `include = "X#y"` bzw. `orderBy` mit `?` → Fehlermeldung, kein Client-Aufruf
   (Strict-Mock, Test `Feldliste_UngueltigeEingabe_Meldung_KeineAnfrage` erweitert).
10. **SD-3:** Create, Edit (POST) und Delete mit `sdata:password` → Ablehnung mit Warnmeldung, kein `UpsertAsync`/`DeleteAsync` (Test
    `Freitext_CreateEditDelete_LehnenGeheimAb` um die Schreibweise erweitert).
11. **SD-3 (nur bei Antwort 1 = allgemein):** dasselbe für `produktionsauftraghierarchisch` und `sync:fazusatzinfoautoerledigtenabled`; ein
    Orphan-Key ohne Katalog-Gegenstück bleibt über den Freitext-Weg pflegbar.
12. **SD-4:** `Update-IdealAkeWms.ps1 -NurPruefen` mit gesetztem `SageLagerbuchung:Password` nur in der Web-Datei zeigt „Abweichung 1“ für
    `Pruefung/SDataKennwortAppsettingsWeb` mit dem Web-Dateipfad; ohne Eintrag „OK“. Die Dienst-Prüfung bleibt unverändert.
13. **SD-4:** Drift-Test grün; er lässt `Ort` nur mit `Appsettings` und nur mit `Web|Dienst` zu.
14. **SD-6:** Im Code stehen Lagerbuchung und Sage-WE in zwei getrennten `try`/`catch`-Blöcken mit eigenen Schrittnamen; ein Wurf in
    `ProcessOnceAsync` lässt `ProcessSageWareneingangAsync` und die CSV im selben Takt laufen (Code-Review; siehe TS-214.6).
15. **SD-6:** Lesefehler des Schalters `SageWareneingangBelegAktiv` setzt weiterhin nichts auf „Klären“ (kein Aufruf `SageWareneingangAusAsync`).
16. Anwenderdoku 12 ohne „Behebung ist geplant“-Stolperfalle, stattdessen das neue Verhalten; ADR 0024 „Nie im Klartext“ nennt Verbindungstest und
    Freitext-Schreibweise; Changelog-Eintrag v1.99.93 (vorläufig).
17. Build grün, `dotnet test` beider Testprojekte grün.

## Test-Szenarien

Neues Kapitel **TS-214 (vorläufig) — Geheimnisse und Sage-Test: Review-Befunde (v1.99.93, vorläufig)** in `docs/TESTSZENARIEN.md`, Bezug TS-131
(`docs/TESTSZENARIEN.md:11842`). Standort beide; auf AKE-DEV fehlen gespeicherte Zugangsdaten (Backlog-Nachmessung) – für 214.1 einmal Benutzer und
Kennwort des Sage-Test-Kontos eintragen (Gerald).

| Nr | Schritt | Erwartung |
|---|---|---|
| 214.1 | Register Sage-Test → Verbindung testen, Adresse auf `http://127.0.0.1:9123/` (lokaler Listener), Kennwort leer | Meldung „nur für die gespeicherte Adresse“ bzw. „nie über http://“, Listener sieht keine Verbindung |
| 214.2 | Wie 214.1 mit `https://<gespeicherter Host>:<anderer Port>/` | Meldung, keine Anfrage |
| 214.3 | Gespeicherte Adresse, TLS-Haken aus (gespeichert an), Kennwort leer | Meldung zur TLS-Prüfung |
| 214.4 | Gespeicherte Adresse unverändert, Ressource `$schema`, Kennwort leer | HTTP 200, „Zugangsdaten: gespeichert (Service-Einstellungen)“ |
| 214.5 | Verbindungstest Ressource `Artikel?orderBy=Artikelnummer&format=json` (große Antwort) | Antwort endet mit „… (gekuerzt)“, Web-Prozess ohne Speicherspitze (Task-Manager grob) |
| 214.6 | SD-6 Review: `SageBookingWorker.cs` zwei Blöcke; DEV-Normalbetrieb mit beiden Schaltern an | Beide Schritte laufen, Aktivitäts-Protokoll unverändert; Fehlerfall auf DEV nicht gezielt erzeugbar (DB-Fehler in `EnqueueMissingAsync`), daher Code-Review |
| 214.7 | Feldliste: orderBy `BelID&where=1 eq 1` | Meldung „orderBy ohne &, ? und #.“, keine Anfrage |
| 214.8 | `/ServiceSettings/Create` Key `sdata:password`, Wert `x` | Warnmeldung, P-2-Abfrage zeigt weiter genau eine Zeile `SData:Password` mit `dp1:`-Wert |
| 214.9 | (bei Antwort 1 = allgemein) Create Key `produktionsauftraghierarchisch` Wert `false` | Warnmeldung, Wert unverändert |
| 214.10 | DEV: Dummy-Wert `SageLagerbuchung:Password = "x"` in Web-`appsettings.<Umgebung>.json`, `-NurPruefen`, danach Zeile entfernen | Zeile `Pruefung/SDataKennwortAppsettingsWeb` „Abweichung 1“ mit Web-Datei; nach Entfernen „OK“ |

## Etappen (nur bei epic: true)

Nicht zutreffend (epic: false).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

*Final (QA 2026-11-28, aus dem echten Diff `a57973f7..7abbc46b`).*

- **Web-App:** ja (`IdealAkeWms/`: ServiceSettingsController, SageTestController, SdataZugangsdaten, Views SageTest/Help, AppVersion).
- **Service:** ja (`IDEALAKEWMSService/Workers/SageBookingWorker.cs`, AppVersion).
- **Migration:** nein (keine Datei unter `*/Migrations/`, kein SQL).
- **Update-Skript:** ja (`scripts/deploy/Update-IdealAkeWms.ps1` Funktion `Get-AppsettingsWert`, `nachlauf.psd1`) – geht im Paket unter `publish\deploy` mit.
- **Publish erledigt (QA):** `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-11-28-buendel-review-kommissionierung\publish` – v1.99.92, Commit
  7abbc46b, `IDEALAKEWMSWeb` + `IDEALAKEWMSWebService` + `SQL` + `deploy` + `publish-info.txt`. Beide Komponenten deployen (Reihenfolge ohne
  Migration: Update-Skript wie üblich; kein Backup-Sonderschritt nötig).
- **Nach dem Merge:** nur dann aus `main` neu publizieren, wenn der Merge getestete Dateien mit parallelen main-Änderungen zusammengeführt hat.
- **Manifest-Einträge** (`scripts/deploy/nachlauf.psd1`, ADR 0022/0023) – im Diff vorhanden:
  - **neu** `Id='Pruefung/SDataKennwortAppsettingsWeb'; Version='1.99.92'`; `Typ='Pruefung'; Firma='beide'; Phase='Nachher';
    Ausfuehrung='anzeigen'; Appsettings='SageLagerbuchung:Password'; Ort='Web'`; Text: „SData-Kennwort steht noch im Klartext in {Datei}
    (Web). Erst in den Service-Einstellungen eintragen, Register Sage-Test pruefen, dann aus der Datei entfernen.“; Quelle =
    `2026-11-28-v1-99-92-sdata-geheimnisse-review-befunde` (Brain-Changelog entsteht beim Laufabschluss). Hinweis: IDEAL-DEV meldet hier
    bereits echt „Abweichung 1“ (`D:\Dev\IDEAL\Web\appsettings.DEV.json` enthält das Kennwort) – auf Test/Prod ist mit derselben Zeile zu rechnen.
  - **geändert** `Pruefung/SDataKennwortAppsettings` (Id, Version, Prüfung unverändert): Klammerzusatz „(auch Web: appsettings.Production.json)“
    im Text entfällt.
  - Sonst keine (kein SQL, kein Einmal-Skript, keine Einstellung, kein Leitstand-Schritt, keine Anwender-Info, kein Ordner/Recht).
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb` und
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`; das Update-Skript (`scripts/deploy`)
  geht mit dem Paket mit.

## Offene Rueckfragen

Keine Fragen an den Fachbereich (alles technische Sicherheits-/Robustheitsfragen).

1. **SD-3 Umfang:** Nur `IstGeheim` ohne Groß-/Kleinschreibung (Backlog-Vorschlag, eine Zeile) oder die allgemeine Regel im Freitext-Weg „Key weicht
   nur in der Schreibweise von einem Katalog-Key ab → ablehnen“? Die allgemeine Regel schließt zusätzlich die gleiche Lücke am Einwegtor
   (`produktionsauftraghierarchisch` umginge heute `GuardedServiceSettingRepository`, Beleg im Lösungsentwurf). **Empfehlung: allgemeine Regel**
   (eine Prüfung statt zwei, kein Eingriff in die Wächter).
2. **SD-1 http:** Gespeicherte Zugangsdaten im Verbindungstest nie über `http://` senden – auch dann nicht, wenn `SData:BaseUrl` selbst mit
   `http://` beginnt (P-3 vorher laufen lassen)? Folge bei „ja“: auf einem Standort mit http-Adresse geht der Test nur mit im Fenster
   eingegebenem Kennwort, der Dienst sendet unverändert über http. **Empfehlung: ja** (der Test ist Diagnose, kein Grund, das Kennwort zusätzlich
   unverschlüsselt zu verschicken); https-Pflicht beim Speichern von `SData:BaseUrl` wäre eine eigene Aufgabe.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

Keine Fachbereichs-Fragen, kein Jira-Vorgang.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

1. SD-3 Umfang: nur IstGeheim oder allgemeine Schreibweisen-Regel (inkl. Einwegtor)? → **allgemeine Regel wie empfohlen** (Gerald 2026-11-28)
2. SD-1 http: gespeicherte Zugangsdaten nie über http://, auch bei http-SData:BaseUrl? → **Nein – „es muss funktionieren“ (Gerald 2026-11-28):** gespeicherte Zugangsdaten nur an die konfigurierte `SData:BaseUrl` (gleiches Schema/Host/Port); ist `SData:BaseUrl` selbst http, bleibt der Test mit gespeichertem Kennwort möglich. http an eine abweichende Adresse: nie. Kein Standort darf durch die Änderung den Verbindungstest verlieren.

## QA-Nachweis (qa-agent, 2026-11-28)

Worktree `.claude/worktrees/2026-11-28-buendel-review-kommissionierung`, Branch `feature/2026-11-28-buendel-review-kommissionierung`.
Geprüfter Bereich: Umsetzungs-Commit `c9c9f888` (Diff gegen `a57973f7`) + QA-Fix `7abbc46b`. Version 1.99.92, TS-212 (Spec nannte vorläufig
1.99.93/TS-214). Standort laut Frontmatter: beide.

### 1. Build und Tests
- `dotnet build IdealAkeWms.slnx`: 0 Fehler (37 Warnungen, vorbestehend).
- `dotnet test` auf c9c9f888: IdealAkeWms.Tests 3880 bestanden / 1 übersprungen / 0 Fehler; IDEALAKEWMSService.Tests 848 / 0 / 0.
- Nach QA-Fix 7abbc46b: IdealAkeWms.Tests **3882** / 1 / 0 (+2 neue Fälle); IDEALAKEWMSService.Tests **848** / 0 / 0.

### 2. Code-Review (selbst, in diesem Lauf)
Geprüft: `ServiceSettingsController` (TestSageConnection, Freitext Create/Edit GET+POST/Delete), `SdataZugangsdaten.SperreGespeicherterZugang`
und `LeseHoechstensAsync`, `WebStandAsync`/`Aufloesen` (Quelle von BaseUrl/TLS), `SageTestController.Feldliste`, `SageBookingWorker`-Schleife
(alle try-Blöcke, `sageWeAn`-Fluss zur CSV), `Update-IdealAkeWms.ps1` `Get-Einstellung`/`Get-AppsettingsWert`/Auswahl der Prüfungen (läuft
versionsunabhängig, Z. 704/928), `nachlauf.psd1`, Drift-Test, View-JS (Ausgabe per `textContent`), Doku/Hilfe/Changelog.

| # | Befund | Schwere | Behandlung |
|---|---|---|---|
| Q-1 | **SD-3 unvollständig:** Die Spalten-Kollation (`Latin1_General_CI_AS`, DEV gemessen) behandelt nicht nur Groß-/Kleinschreibung gleich, sondern auch **Leerzeichen am Ende** (SQL-Padding) und **Vollbreite-Zeichen** (breiten-unabhängig). DEV-Messung: `WHERE [Key] = N'SData:BaseUrl  '` → 1 Treffer; `NCHAR(65331)+N'Data:BaseUrl'` → 1 Treffer. `Create("SData:Password ", "x")` lief an `KatalogKeyAndereSchreibweise` (OrdinalIgnoreCase) und `IstGeheim` (ordinal) vorbei; `FindAsync` hätte die echte Zeile gefunden und das Kennwort im Klartext überschrieben. | hoch (Sicherheit, ADR 0024) | **Behoben in 7abbc46b:** Vergleich per `CompareInfo.Compare(k, key.TrimEnd(), IgnoreCase/IgnoreWidth/IgnoreKanaType)`; Test `Freitext_AndereSchreibweiseKatalogKey_Abgelehnt` um `"SData:Password "` und `"\uFF33Data:Password"` erweitert (grün). DEV IDEAL: Create `SData:Password ` → Warnung, Zeile unverändert. |
| Q-2 | Meldung bei der Leerzeichen-Variante sagt „weicht nur in der Groß-/Kleinschreibung … ab“. | kosmetisch | Bewusst belassen (Text in TS-212.9 und Test verankert; die Ablehnung ist das Wesentliche). |
| Z-1 | Zusatzfund außerhalb des Diffs: `/ServiceSettings` zeigt `WarningMessage` doppelt (`_Layout.cshtml:510` + `Views/ServiceSettings/Index.cshtml:31`). | kosmetisch, vorbestehend | Nicht mitgebaut, nur gemeldet. |

Ohne Befund geprüft: SD-1 prüft die tatsächlich gesendete URL (gleicher `Uri`-Parse wie `HttpRequestMessage`), Standard-Port, Host über
`IdnHost` ohne Groß-/Kleinschreibung, `@` in Ziel und Gespeichertem, http nur wenn gespeichert http (Antwort 2), TLS nicht schwächer; Sperre
nur bei Quelle ≠ „keine“ (Benutzer ohne Kennwort zählt als gespeichert → gesperrt, korrekt). Redirects: .NET entfernt `Authorization` beim
Folgen und folgt nicht https→http. SD-2a: `ResponseHeadersRead` + `StreamReader`/`ReadBlockAsync` bis max+1, Dispose bricht ab; kein
`ReadAsStringAsync` mehr. SD-2b: Prüfung an der Vertrauensgrenze, `else if` vermeidet Doppelmeldung bei leerem orderBy. SD-6: zwei try-Blöcke
mit eigenem Betreff und je OCE-`break`; `sageWeAn` bleibt bei Lesefehler `false` → kein `SageWareneingangAusAsync`, CSV aus (Q-2 unverändert).
SD-4: `$m['Ort']` auf der Hashtable aus psd1 ist PS-5.1-fest; Skript und Manifest rein ASCII (Suche nach Nicht-ASCII ohne Treffer). Defektklassen:
kein `@Html.Raw` in Script, kein `hidden`+`d-*`, kein stiller `?? 0`, keine parallelen Arrays, Server erzwingt alle Regeln, die
Schreibweisen-Regel lebt an einer Stelle.

### 3. Nachweisart je Akzeptanzkriterium
| AK | Nachweis |
|---|---|
| 1–4 | Unit (`SperreGespeicherterZugang_NurGespeicherteAdresse`, `_HttpAnHttpsAdresse_NenntHttp`) + DEV IDEAL 212.1–212.4 (Listener ohne Verbindung) |
| 5 | DEV IDEAL: Sperre passiert, „Zugangsdaten: gespeichert (Service-Einstellungen)“; HTTP 200 **Manual-UAT** (idealsage2 von diesem PC nicht erreichbar, Kennwort auf DEV nicht gesetzt) |
| 6 | DEV IDEAL: Kennwort im Fenster → Listener erhält genau 1 Verbindung („Eingabe im Fenster“) |
| 7 | Unit (14 Fälle) |
| 8 | Unit (`LeseHoechstens_KapptNach50000`, `TestSageConnection_LiestNichtMehrGanzenBody`); echte große Antwort **Manual-UAT** |
| 9 | Unit (`Feldliste_UngueltigeEingabe_Meldung_KeineAnfrage`, Strict-Mock) + DEV IDEAL 212.8 |
| 10–11 | Unit (`Freitext_AndereSchreibweiseKatalogKey_Abgelehnt`, `Freitext_OrphanKey_BleibtPflegbar`) + DEV IDEAL 212.9; Prod-Kollation **Manual-UAT** (P-1) |
| 12 | DEV AKE + IDEAL mit dem Paket-Skript unter Windows PowerShell 5.1 (212.10) |
| 13 | Unit (`NachlaufManifestDriftTests`) |
| 14–15 | Code-Review (Abschnitt 2); Laufzeit mit beiden Schaltern an **Manual-UAT** (DEV-Sperre `SageLagerbuchungAktiv`) |
| 16 | Review Doku 12, Hilfe, Changelog v1.99.92; ADR-0024-Nachtrag im Hauptcheckout (laut Auftrag) |
| 17 | Abschnitt 1 |

### 4. Geänderte bestehende Tests
- `SageTestSdataTests.Feldliste_UngueltigeEingabe_Meldung_KeineAnfrage`: optionaler Parameter `include` + 3 InlineData – Verhalten bewusst
  erweitert (AK 9); bestehende Fälle unverändert.
- `NachlaufManifestDriftTests`: zwei zusätzliche Assertions für `Ort` (AK 13).
- Keine Fixture-Anpassung, kein „Sonstiges“.

### 5. Checkliste CLAUDE.md
Keine Migration/SQL/FreshInstall/AgentJobs. Keine Audit-Felder (Spec). Version 1.99.92 in beiden `AppVersion.cs`, Anwender-Changelog ergänzt;
Brain-Changelog/feature-map macht der Lauf am Ende. `docs/TESTSZENARIEN.md` TS-212 + Index Zeile 212 vorhanden. 5a ADR 0020: keine
neue/geänderte Einstellung – entfällt. Manifest-Eintrag für die neue Prüfung vorhanden; kein weiterer manueller Deploy-Schritt.

### 6. DEV-Deploy und TS-212
Shell erhöht (Administrator = True). `dev-deploy.ps1 -Standort AKE|IDEAL -Quelle <worktree>` zweimal (auf c9c9f888, nach dem Fix auf
7abbc46b): jeweils Exit 0, „HTTP 200, Fusszeile v1.99.92“, „Fertig: AKE v1.99.92 auf http://localhost:8081/“ bzw. „IDEAL … :8082/“.
Dienst-Logs: SageBookingWorker „gestartet. Version 1.99.92“, 0 `[ERR]` seit Deploy (beide).

| Schritt | Durchgeführt | Ergebnis | Status |
|---|---|---|---|
| 212.1 | IDEAL, `http://127.0.0.1:9123/`, Kennwort leer, TCP-Listener aktiv | „Gespeicherte Zugangsdaten nur für die gespeicherte Adresse https://idealsage2.ideal.ideal-ake.at:5493 (nie über http://)“, Zugang „gespeichert – nicht verwendet“; Listener ohne Verbindung | OK |
| 212.2 | IDEAL, Port 5494 | Meldung „nur für die gespeicherte Adresse …“, keine Anfrage | OK |
| 212.3 | IDEAL: `SageLagerbuchungSslZertifikatPruefen` kurz `true` (SQL), Haken aus | „Gespeicherte Zugangsdaten nicht mit ausgeschalteter Zertifikatsprüfung …“; Wert danach zurück auf `false` (geprüft) | OK |
| 212.4 | IDEAL, `https://x@…:5493/` | „Adresse enthält Zugangsdaten (@)“ | OK |
| 212.5 | IDEAL, Host in Großbuchstaben, Kennwort leer | Sperre passiert, „Zugangsdaten: gespeichert (Service-Einstellungen)“; Sage-Server verweigert Verbindung von diesem PC | teilweise OK; HTTP 200 NICHT MOEGLICH |
| 212.6 | IDEAL, Dummy-Kennwort im Fenster, Listener | Listener: genau 1 Verbindung; „Eingabe im Fenster“ | OK |
| 212.7 | – | braucht gültige Sage-Anmeldung (AKE-DEV 401 ohne gespeicherte Zugangsdaten, IDEAL nicht erreichbar) | NICHT MOEGLICH |
| 212.8 | IDEAL Feldliste, orderBy `BelID&where=1 eq 1`, include `X#y` | „orderBy ohne &, ? und #.“ + „include ohne &, ? und #.“ | OK |
| 212.9 | IDEAL Create: `sdata:password`, `SData:Password ` (Leerzeichen), `produktionsauftraghierarchisch`=false; Orphan `Adhoc:QaTest` | drei Warnungen; SQL: `SData:Password` unverändert (CS-abweichend 0), `ProduktionsauftragHierarchisch` = true; Orphan gespeichert, danach per SQL entfernt | OK |
| 212.10 | Paket-Skript `publish\deploy\Update-IdealAkeWms.ps1 -System DEV -NurPruefen` (powershell.exe 5.1); AKE: Dummy-Kennwort in `D:\Dev\AKE\Web\appsettings.DEV.json`, danach Datei aus Sicherung (Hash gleich) | AKE ohne: `AppsettingsWeb` OK; mit Dummy: „Abweichung 1 … D:\Dev\AKE\Web\appsettings.DEV.json (Web)“; zurück: OK. IDEAL: echte Abweichung (Web-DEV-Datei enthält das Kennwort). Dienst-Prüfung unverändert. | OK |
| 212.11 | Code-Review + Dienst-Log | zwei Blöcke bestätigt; beide Schalter auf DEV aus (Sperre), Dienst ohne Fehler | Review OK; Laufzeit NICHT MOEGLICH |
| Negativ | AKE-DEV, Quelle „keine“, gespeicherte Adresse | Aufruf ohne Anmeldung wie bisher: HTTP 401, „keine gespeicherten Zugangsdaten“, Kopfzeilen per Stream gelesen | OK |

### 7. Publish
`C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-11-28-buendel-review-kommissionierung\publish` (Zielordner vorher geleert): `IDEALAKEWMSWeb`,
`IDEALAKEWMSWebService` (beide Exit 0), `SQL`, `deploy`, `publish-info.txt` (Version 1.99.92, Commit 7abbc46b02a6a71fc764fe69779749bf7bad4690).
Das Paket-Skript lief daraus fehlerfrei (212.10).

### Manuelle Test-Checkliste (Mensch, offen)
1. TS-212.5: System mit erreichbarem Sage und gespeicherten Zugangsdaten, unveränderte Adresse → HTTP 200, „Zugangsdaten: gespeichert
   (Service-Einstellungen)“; ist dort `SData:BaseUrl` http, muss der Test weiter gehen (Antwort 2).
2. TS-212.7: große Ressource (z. B. `Artikel?orderBy=Artikelnummer&format=json`) → endet mit „… (gekuerzt)“, Web-Prozess ohne Speicherspitze.
3. TS-212.11: Testsystem mit `SageLagerbuchungAktiv` und `SageWareneingangBelegAktiv` an → beide Schritte laufen, Aktivitäts-Protokoll normal.
4. Prüfabfragen P-1 (Kollation), P-2 (Altlast-Schreibweise), P-3 (BaseUrl http?) auf AKE- und IDEAL-Prod vor Go-live.
5. Update-Skript auf Test/Prod: Zeile `Pruefung/SDataKennwortAppsettingsWeb` beachten (IDEAL-DEV hat das Kennwort in der Web-Datei; erst
   Service-Einstellungen pflegen, dann aus der Datei entfernen).
6. Gesamtlauf TS-212 auf dem Testsystem (beide Standorte), danach Schranke 2 (Merge).

> **Bestätigung Gerald 2026-11-28 (nach Testbereit):** Deutung Freigabe-Antwort 2 passt – gespeicherte Zugangsdaten über http nur, wenn die gespeicherte `SData:BaseUrl` selbst http ist (umgesetzt in `SdataZugangsdaten.SperreGespeicherterZugang`).
