---
type: spec
status: Testbereit
created: 2026-11-18
updated: 2026-11-18
standort: beide
source_backlog: "[[2026-11-18-sage-userfelder-als-einstellung]]"
freigabe_quelle: "/goal 2026-11-18 (Gerald 09:15: „bei den Userfeldern nicht hardcodiert, immer als Einstellung“)"
worktree: .claude/worktrees/2026-08-07-ideal-teile-1-5
branch: feature/2026-08-07-ideal-teile-1-5
version: 1.99.40
testszenario: TS-161
adr: "[[0033-sage-benutzerfelder-als-einstellung]]"
---
# Sage-Benutzerfelder (USER_*) als Einstellung – Spec

Freigabe: Der /goal-Auftrag vom 2026-11-18 ist die Freigabe (Schranke 1 durch den Auftrag genommen). Lauf-Notiz:
[[2026-11-18-goal-lauf-userfelder-einstellung]].

## Ziel
Jedes Sage-Benutzerfeld, das das WMS liest, kommt aus einer ServiceSetting. Der Default ist der heutige Feldname, AKE liest also
unverändert. Ein leerer Wert bedeutet, dass das Feld nicht abgefragt wird. Die abhängige Anzeige zeigt dann „nicht konfiguriert“, es
entsteht kein Fehler. Kennt Sage das Feld nicht, erscheint eine Meldung mit dem Einstellungsnamen, einmal je Lauf. Fehlt eine SData-Resource
(HTTP 404), wird das einmal verständlich gemeldet und nicht in jedem Lauf wiederholt.

## Vollständige Erhebung (Suche `USER_` im Worktree, Stand `2ae5d1da` v1.99.39)
| Fundstelle | Feld(er) | Leseweg | Einstellung (Key) |
|---|---|---|---|
| `KontokorrentSyncService.cs:49` | USER_EKMindestbestellwert | SData Kontokorrent (Kreditoren) | `Sync:Userfeld:Kreditoren:EKMindestbestellwert` |
| `KontokorrentSyncService.cs:57` | USER_KundenNameFA | SData Adressen (Debitoren) | `Sync:Userfeld:Debitoren:KundenNameFA` |
| `SdataArtikelReader.cs:59` | USER_LagerplatzID, USER_Mindestbestand | SData ArtikelVariantenExtended | `Sync:Userfeld:Artikel:LagerplatzID`, `…:Artikel:Mindestbestand` |
| `EinkaufBestellungenSyncService.cs:52-53` | KHKEKBelege_**USER_AB**, KHKEKBelege_**USER_Kom** (RealTimeData-Alias, Präfix bleibt Code) | RealTimeData Kopf | `Sync:Userfeld:Bestellungen:AB`, `…:Bestellungen:Kom` |
| `SageArbeitsplatzReader.cs:72` | USER_ArbeitsSchritt | SQL KHKPpsArbeitsplaetze | `Sync:Userfeld:Arbeitsplaetze:ArbeitsSchritt` |
| `BfArtikelFelder.cs:13-19` | 7 × USER_OS* (Blechfertigung) | SData Artikel (Nachlese) | `Sync:Userfeld:Blechfertigung:<Name>` |
| `SdataFaStrukturReader.cs:147-161` | 13 × USER_OS* (FA-Kopf mit Präfix KHKPpsFaBelege_, 8 davon auch am Artikel) | RealTimeData + SData Artikel | `Sync:Userfeld:FaStruktur:<Name>` |
| `ColumnDefinitions.cs:826`, `PurchaseProposals/Index.cshtml:179` | USER_Mindestbestand (Spaltentitel) | Anzeige | liest `…:Artikel:Mindestbestand` |

Nur Kommentar oder Präfix (bleibt): `ArtikelBenutzerfelderWriter.cs:121`, `SageTestAufbereitung.cs:65-69`, Modell-Kommentare, `SQL/105`, `SQL/156`.
Bereits Einstellung: `Sync:ArtikelBenutzerfelder`. Views auf Sage-Seite (vw_*) liegen außerhalb des WMS-Codes.

## Regeln
- **R1 Lesart:** Zeile fehlt → Default (heutiger Name). Leer → nicht konfiguriert, das Feld wird nicht angefragt. Ungültig (nicht `USER_…`) → wie leer, mit Warnung im Lauf.
- **R2 Prüfung beim Speichern:** `Muster` `^(USER_[A-Za-z0-9_]{1,100})?$`. Der Präfix (`KHKEKBelege_`, `KHKPpsFaBelege_`) steht nie in der Einstellung.
- **R3 Merkmale:** Blechfertigung schreibt die Merkmale weiter unter dem logischen Namen (= Default). Das Web liest dieselben Merkmale. Eine Umbenennung des Sage-Felds lässt also keine verwaisten Merkmale zurück.
- **R4 Ungültiger Spaltenname:** Die Meldung „Sage kennt das Feld „X“ nicht – Einstellung „<Anzeigename>“ (<Key>) prüfen oder leer setzen“ steht einmal je Lauf im Aktivitäts-Protokoll.
- **R5 HTTP 404:** Die Meldung lautet „Resource „R“ am Sage-Server nicht vorhanden – Abgleich <Name> in den Service-Einstellungen ausschalten“. Der Schritt wird danach 24 h nicht mehr ausgeführt (eine Mail, kein Fehler je Lauf).
- **R6 Anzeige:** Bestellvorschläge: Spaltentitel „Mindestbestand (Sage)“, Zellen „nicht konfiguriert“, wenn die Einstellung leer ist. BF-Aufträge: Hinweis, welche Blechfertigungs-Felder nicht konfiguriert sind.

## Einstellungen (ADR 0020)
Je Abgleich eine eigene Kategorie „Sage-Benutzerfelder: <Abgleich>“. Anzeigename, Wirkung (Description), Default = heutiger Name.
Standort: Kreditoren und Artikel AKE, Blechfertigung und FA-Struktur IDEAL, Rest beide. Warnhinweis: „Falscher Name → Lauf meldet Fehler.“
Manifest IDEAL: `Sync:Userfeld:Kreditoren:USER_EKMindestbestellwert` leer setzen (Key = `Sync:Userfeld:<Abgleich>:<Standardname>`).

## Plan (kompakt)
1. `IdealAkeWms/Services/SageSdata/SageUserfelder.cs`: Katalog (Key, Default, Kategorie, Anzeigename, Wirkung, Standort), `AusWerten(Func<key,string?>)`, `Feld(key)`, `Erklaeren(meldung)`. In `ServiceSettingDefinitions.Basis()` und `Meta` eintragen. Test: Defaults = heutige Strings, leer → null, ungültig → null.
2. Dienst `ServiceSettings.GetByPrefixAsync` (eine Abfrage) und `SageUserfelderLeser.LesenAsync(config)`.
3. Kontokorrent, Artikel-Varianten, Bestellungen-Kopf, Arbeitsplatz-SQL, BF-Nachlese und FA-Struktur auf aufgelöste Namen umstellen. Tests: AKE-Select zeichengleich, leer → Feld fehlt im Select.
4. 404 in `SdataLeseException` plus 24-h-Sperre in `SyncWorker.RunResilientAsync`. „Ungültiger Spaltenname“ über `Erklaeren` an den Fangstellen.
5. Web: Spalte Bestellvorschläge und BF-Hinweis (frontend-design).
6. Version 1.99.40, Changelogs, Hilfe, README, TS-161, Manifest, ADR 0033, Fallstrick 51, Brain.

## Akzeptanz
- AKE ohne Einstellungszeilen liest zeichengleich wie v1.99.39 (Test `SageUserfelderTests.Default_selects_unveraendert`).
- Ist ein Feld leer, wird es nicht angefragt und die Anzeige zeigt „nicht konfiguriert“.
- HTTP 500 „Ungültiger Spaltenname“ ergibt die Meldung mit dem Einstellungsnamen, HTTP 404 die Meldung „Resource … nicht vorhanden“, und es gibt kein Wiederholen in jedem Lauf.

## Umsetzung (2026-11-18, Commit `127f7e3f`)
- Katalog `SageUserfelder.Katalog` (27): Kreditoren 1, Debitoren 1, Artikel 2, Offene Bestellungen 2, Arbeitsplätze 1, Blechfertigung 7, FA-Struktur 13.
  `ServiceSettingDefinitions.All` = Basis + Userfelder (Metadaten aus dem Katalog). Der Dienst lädt alle mit einer Abfrage (`ServiceSettings.UserfelderSafeAsync`; DB nicht lesbar → Defaults), das Web mit `SageUserfelder.AusAsync`.
- Abweichender Sage-Name: Typisierte SData-Zeilen haben `[JsonExtensionData] Weitere`; `SageUserfelder.Text` nimmt die typisierte Eigenschaft nur beim Default-Namen. Arbeitsplatz-SQL: `TableSqlFuer(feld)`, leer → `CAST(NULL …) AS USER_ArbeitsSchritt` + `ArbeitsschrittGeliefert: false` (bestehende Warnung P-A1).
- „nicht konfiguriert“ ist sichtbar in: Kreditor-/Debitor-Details, Artikel-Sage-Karte (Lagerplatz-ID, Mindestbestand), Bestellvorschläge-Spalte, Hinweis-Kasten BF-Aufträge. AB/Kom und FA-Struktur: Info im Lauf, die Anzeige bleibt leer. CSV-Export Bestellvorschläge: leere Zelle.
- 404: Meldung in `SdataLeseException`, Sperre 24 h je Schritt in `SyncWorker.RunResilientAsync` (im Speicher, ein Dienst-Neustart setzt sie zurück; `ponytail:`-Kommentar).
- Build 0 Fehler. Tests bestanden: Dienst 778, Web 3254 (+1 übersprungen). Neu: `SageUserfelderTests` (9).

## QA-Nachweis
qa-agent, 2026-11-18. Geprüfter Bereich `2ae5d1da..127f7e3f` plus QA-Fix `4190c887`. Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`.
`standort` fehlte im Frontmatter. Die Spec nennt AKE und IDEAL, deshalb jetzt `beide`.

**1. Build und Tests** (im Worktree, Stand `127f7e3f` und erneut nach dem Fix `4190c887`):
- `dotnet build IdealAkeWms.slnx`: 0 Fehler. Warnungen: 29 vor dem Fix, 24 danach (Nullable- und NuGet-Hinweise, alle schon vorher vorhanden).
- `dotnet test`:
  - IdealAkeWms.Tests: 3254 bestanden, 1 übersprungen, 0 Fehler.
  - IDEALAKEWMSService.Tests: vor dem Fix 778 bestanden, 0 Fehler. Nach dem Fix 779 bestanden (+1 neuer Test), 0 Fehler.

**2. Code-Review**. Geprüft wurden:
- alle 25 Dateien des Diffs;
- Aufrufer von `SageArbeitsplatzReader` (Werkbank-Anlage, Arbeitsgang-Abgleich) und deren Fangstellen;
- `ArtikelBenutzerfelderWriter`: ein fehlender Schlüssel bleibt unberührt, R3 ist erfüllt;
- `SettingValidator` und `ServiceSettingsController.SaveSettings`: leer wird als leere Zeile gespeichert, das Muster wird serverseitig geprüft;
- der Seed in `Program.cs`, der die 27 Zeilen mit dem Standardwert anlegt (Wirkung wie eine fehlende Zeile);
- `SyncWorker`: alle Schritte laufen nacheinander, das Dictionary ist also ohne Nebenläufigkeit;
- der 404-Wurf in `SageSdataReadClient.cs:255`.

AKE zeichengleich: Kreditoren- und Debitoren-Select, Varianten-Select, Arbeitsplatz-SQL (Lesen über die Position, `TableSqlFuer` mit dem Default ergibt genau `TableSql`), Kopf-Select der Bestellungen, FA-Belegsuche (Präfix + logische Reihenfolge = vorher) und BF-Nachlese (Filter `USER_OSIstLagerartikel ne 0`) sind alle unverändert. Der Test `Default_selects_unveraendert_wie_v1_99_39` belegt das.

Einsetzen in Abfragen nur nach `^USER_[A-Za-z0-9_]{1,100}$` (`SageUserfelder.cs:772`, Prüfung in `Aus`). Ein leerer Wert ergibt `null`: Das Feld fällt aus dem Select bzw. wird im SQL zu `CAST(NULL …)`, eine Exception entsteht nicht.

| Nr | Schwere | Fundstelle | Befund | Behandlung |
|---|---|---|---|---|
| F1 | mittel | `IDEALAKEWMSService/Services/Sdata/SdataArtikelReader.cs:234` | Der SData-Artikelweg (ArtikelVariantenExtended mit USER_LagerplatzID/USER_Mindestbestand) hatte keine Fangstelle mit `Erklaeren`. `SageImportService.cs:592` protokolliert `ex.Message` roh. R4 und die Akzeptanz („Meldung mit Einstellungsnamen“) galten für 2 der 27 Felder nicht. Der Nachlauf-Text für IDEAL verspricht diese Meldung aber. | **Behoben** in `4190c887`: Ein HTTP 500 des Varianten-Lesens wird mit erklärter Diagnose neu geworfen. Neuer Test `ArtikelSdataTests.LesenAsync_VariantenUngueltigerSpaltenname_NenntDieEinstellung`. |
| H1 | niedrig | `IDEALAKEWMSService/Workers/SyncWorker.cs:633-653` | Die 24-h-Sperre gilt je Schritt und für jede 404 irgendwo in der Kette. Eine vorübergehende 404 (Sage-IIS startet neu, falsche `SData:BaseUrl`) setzt einen ganzen Schritt wie „Artikel-Sync“ für 24 h aus. Die Sperre liegt im Speicher, ein Dienst-Neustart hebt sie auf. | So in R5 beschlossen und mit `ponytail:` markiert. Nur Hinweis für die Abnahme: Nach dem Beheben der Ursache den Dienst neu starten. |
| H2 | niedrig | `IdealAkeWms/Services/SageSdata/SageSdataQueryModels.cs:737` | Der 404-Text „Abgleich in den Service-Einstellungen ausschalten“ erscheint auch in Web-Kontexten ohne Abgleich (Register Sage-Test, FA-Struktur live). | Hinweis, kein Fix (nur der Wortlaut ist betroffen). |
| H3 | niedrig | `IdealAkeWms/AppVersion.cs:6`, `IDEALAKEWMSService/AppVersion.cs:6` | `Date` steht für 1.99.40 noch auf `2026-11-17`. | Hinweis an den Hauptlauf. |
| H4 | info | `SageUserfelder.cs:872` | Eine Zahl unter einem abweichenden Namen wird per `GetRawText` gelesen. Exponent-Schreibweise ergäbe `null`. Der Default-Name läuft über den typisierten Weg und ist nicht betroffen. | Hinweis. |
| H5 | info | `SdataFaStrukturReader.cs` (Belegsuche) | RealTimeData lässt unbekannte Felder laut L8 still weg (HTTP 200). Ein falscher Name in „FA-Struktur“ erzeugt dann keine „Ungültiger Spaltenname“-Meldung, die Felder bleiben nur leer. Das verhält sich wie vorher. | Hinweis, Teil der Manual-UAT 161.5 nur für SData-Ressourcen mit 500. |

Defektklassen:
- Werte in `<script>`: nur der statische Spalten-JSON-Block in PurchaseProposals, kein `Html.Raw`.
- `hidden` zusammen mit `d-*`: nicht vorhanden.
- Stille Fallbacks: „DB nicht lesbar → Defaults“ (`ServiceSettings.UserfelderSafeAsync`) ist bewusst und dokumentiert, wie die übrigen `*Safe`.
- Parallele Arrays: nicht vorhanden.
- Validierung nur im Browser: nein, der Server prüft. TS-161.6 hat das auf DEV belegt.
- Eine Regel an mehreren Orten: `Muster` und `Gueltig` stehen in derselben Datei. `SdataArtikelReader.GueltigesBenutzerfeld` ist eine dritte, ältere Kopie derselben Regel (Drift-Risiko gering).

**3. Beweisart je Akzeptanzkriterium**

| AK | Beweis |
|---|---|
| AKE ohne Einstellungszeilen liest zeichengleich | automatisiert: `SageUserfelderTests.Default_selects_unveraendert_wie_v1_99_39`. Echter Lauf gegen Sage-Test = Manual-UAT 161.2 |
| Leer → nicht angefragt, Anzeige „nicht konfiguriert“ | automatisiert (Select ohne Feld, `Leer_laesst_das_Feld_aus_der_Abfrage_weg`); Anzeige auf DEV belegt (161.3/161.4/161.8). Razor-Views = Manual-UAT, auf DEV erledigt |
| HTTP 500 „Ungültiger Spaltenname“ → Meldung mit Einstellung; HTTP 404 → „Resource … nicht vorhanden“, keine Wiederholung | Text und Kette automatisiert (`Erklaeren_…`, `Http404_…`, neuer Artikel-Test). Echte Sage-Antwort und 24-h-Sperre im laufenden Dienst = **Manual-UAT** (161.5, 161.9) |

**4. Geänderte bestehende Tests:** keine. Neu: `SageUserfelderTests` (9) und der QA-Test in `ArtikelSdataTests` (1, nur hinzugefügt).

**5. Checkliste und ADR 0020**
- Keine Migration und kein SQL. FreshInstall ist nicht betroffen: Fehlt die Zeile, gilt der Default; der Web-Seed legt die Zeilen mit dem Default an.
- Version 1.99.40 steht in beiden `AppVersion.cs`. Anwender-Changelog, Hilfe und README sind gepflegt.
- Brain-Changelog [[2026-11-18-v1-99-40-sage-userfelder-als-einstellung]] und ADR 0033 sind vorhanden.
- TS-161 steht in `docs/TESTSZENARIEN.md` und im Testindex (Zeile 161).
- ADR 0020: Jede der 27 Einstellungen hat Anzeigename, Wirkung (Verwendung), Default, Standort und Warnhinweis. Der Server prüft das Muster beim Speichern. Leer ist erlaubt.
- Freitext statt Auswahlliste ist richtig, weil die Namen frei sind.
- Manifest `scripts/deploy/nachlauf.psd1`: zwei Einträge v1.99.40 (Einstellung IDEAL, Info beide), reines ASCII (0 Nicht-ASCII-Bytes).

**6. DEV-Deploy und TS-161**
- Deploy AKE und IDEAL:
  - Zielordner `D:\Dev\{AKE,IDEAL}\{Web,Service}` vorher geleert. Erhalten blieben `appsettings.DEV.json*`, `logs` und `App_Data`.
  - Danach `dev-deploy.ps1 -Quelle <Worktree>`, Exit 0, Fußzeile **v1.99.40** (Stand `127f7e3f`).
  - Nach dem Fix das Ganze erneut mit Stand `4190c887`, Exit 0, v1.99.40.
  - Dienst-Logs 2026-11-18 ohne ERR/FTL.

| Schritt | Ausgeführt | Ergebnis | Status |
|---|---|---|---|
| 161.1 | AKE-DEV `/ServiceSettings` | 7 Kategorien „Sage-Benutzerfelder: Kreditoren / Debitoren / Artikel / Offene Bestellungen / Arbeitsplätze / Blechfertigung / FA-Struktur“, je Feld Warnhinweis „Name muss in Sage existieren …“, Werte = bisherige Namen (DB: 27 Zeilen, alle = Default) | OK |
| 161.2 | – | braucht echte Abgleich-Läufe gegen Sage | NICHT MÖGLICH (Syncs auf DEV aus, harte Grenze) |
| 161.3 | Kreditoren > Mindestbestellwert leer gespeichert (DB `[]`), Kreditor 253 geöffnet | „EK-Mindestbestellwert: nicht konfiguriert“. Lauf-Info im Protokoll nicht prüfbar (Sync aus) | OK (Anzeige) / Lauf NICHT MÖGLICH |
| 161.4 | Artikel > Mindestbestand leer gespeichert | Bestellvorschläge: Kopf „Mindestbestand (Sage)“, Zellen „nicht konfiguriert“; Artikel 64388 (Bearbeiten, Karte Sage-Zusatzfelder) zeigt „nicht konfiguriert“ bei Mindestbestand, Lagerplatz-ID weiter mit Wert | OK |
| 161.5 | – | braucht Sage-Lauf mit falschem Namen | NICHT MÖGLICH |
| 161.6 | AB-Nummer = `KHKEKBelege_USER_AB`, Kommission = `USER X`, Speichern | Server lehnt beides ab (Fehlerliste mit beiden Einstellungen), DB unverändert | OK |
| 161.7 | – | Kreditoren-Lauf IDEAL gegen Sage | NICHT MÖGLICH |
| 161.8 | IDEAL-DEV: Blechfertigung > Ist Lagerartikel leer (per SQL), `/BfAuftraege/Warenkorb` | Hinweis-Kasten „Sage-Benutzerfelder nicht konfiguriert: Blechfertigung: Ist …“. Nachlese-Lauf nicht prüfbar | OK (Anzeige) / Lauf NICHT MÖGLICH |
| 161.9 | – | Abgleich Artikel-Lieferanten einschalten = `Sync:*Enabled` | NICHT MÖGLICH (harte Grenze) |
| 161.10 | alle Änderungen zurückgesetzt | AKE und IDEAL: 0 `Sync:Userfeld:*`-Zeilen weichen vom Standard ab (sqlcmd-Prüfung) | OK |

**7. Publish:** gehört laut Auftrag dem Hauptlauf und ist hier nicht erstellt.

**Deploy (aus dem echten Diff):** `deploy.web: ja` (IdealAkeWms/), `deploy.service: ja` (IDEALAKEWMSService/), `deploy.migration: nein`.
- Reihenfolge: Web und Dienst gemeinsam.
- Danach IDEAL nach Manifest `v1.99.40-Userfeld-EKMindestbestellwert-IDEAL`: Kreditoren-Mindestbestellwert leer setzen und Artikel-Felder prüfen.
- AKE: nichts einzustellen.
- Nach dem Merge nur dann neu aus `main` publizieren, wenn der Merge getestete Dateien mit parallelen main-Änderungen zusammengeführt hat.

**Manuelle Prüfliste (offen, Mensch):**
1. AKE-Test: Abgleiche Kreditoren, Debitoren, Artikel (SData), Offene Bestellungen laufen lassen. Die Werte müssen wie v1.99.39 sein, ohne neue Warnung (161.2).
2. AKE-Test: Kreditoren-Mindestbestellwert leer setzen, Lauf starten. Im Protokoll muss die Info „nicht konfiguriert“ stehen, danach zurücksetzen (161.3, Laufteil).
3. AKE-Test: `USER_GibtEsNicht` eintragen, Lauf starten. Erwartet wird genau eine Meldung mit dem Einstellungsnamen (161.5). Zusätzlich dasselbe für Artikel > Mindestbestand mit Artikel-Quelle SData (prüft den QA-Fix F1).
4. IDEAL-Test: Kreditoren-Mindestbestellwert leer setzen, Kreditoren-Lauf ohne „Ungültiger Spaltenname“ (161.7).
5. IDEAL-Test: Ist Lagerartikel leer setzen. Die Nachlese meldet „nicht konfiguriert“ statt eines Fehlers (161.8, Laufteil).
6. IDEAL-Test: Artikel-Lieferanten einschalten. Erwartet werden eine Meldung „Resource „ArtikelLieferantInfo“ … nicht vorhanden“ und 24 h lang kein neuer Fehler (161.9). Danach wieder ausschalten.
7. Alle Testeinstellungen auf den Standard zurücksetzen (161.10).
