---
type: spec
status: Gemerged
created: 2026-11-18
updated: 2026-11-28
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

## Befund Testsystem IDEAL :88 (Koordination 2026-11-18 13:25)
- Kreditoren-Abgleich meldet bei „Ungültiger Spaltenname USER_EKMindestbestellwert“ weiterhin nur den rohen SData-Fehler, **ohne Namen der Einstellung**
  `Sync:Userfeld:Kreditoren:USER_EKMindestbestellwert` (Anforderung Punkt 2). Nachbessern wie QA-Fix R4 (Artikel-Varianten), für alle Abgleiche mit Userfeldern.
- Manifest-Hinweis „IDEAL: USER_EKMindestbestellwert leer“ wurde beim Deploy nicht umgesetzt → am :88 von Hand leer gesetzt.

## Nachtrag v1.99.42 (Goal-Lauf [[2026-11-18-goal-lauf-nachbesserungen-v1-99-42]], Befund 13:25)

Ursache: Der Hinweis wurde **angehängt**, `SdataLeseException.Message` ist auf 600 Zeichen gekappt – bei langen SQL-Meldungen (Debitoren,
Artikel, Bestellungen …) fiel er weg, nur bei Kreditoren (kurz) blieb er stehen. Fix `3e02dde7`: Hinweis vorn
(„Sage kennt das Feld „X“ nicht – Einstellung „…“ (Sync:Userfeld:…) prüfen oder leer setzen. Sage: …“), erklärt aus der ungekappten
`Diagnose` (`SageUserfelder.Erklaeren(Exception)`), nie doppelt; alle Aufrufer umgestellt. Einmal je Lauf, weil jeder Abgleich beim ersten
Lesefehler abbricht. Fallstrick 53, TS-163.11. Status unverändert bis zum QA-Lauf.

## QA-Nachweis v1.99.42 (qa-agent, 2026-11-18)

Gemeinsamer Lauf über die Nachbesserungen v1.99.42 (Changelog [[2026-11-18-v1-99-42-nachbesserungen]]), Diff `9b6b3e0d..0af91b73` (7 Commits)
im Worktree `.claude/worktrees/2026-08-07-ideal-teile-1-5`, plus QA-Fix `b5463336`. Selbst in diesem Lauf geprüft, nicht delegiert.
Derselbe Block steht in allen vier betroffenen Specs (BF-CSV, Beschichtungsauftrag-Ausbau, Sage-Userfelder, Sage-AG-Anlage).

**1. Build und Tests** (Stand `b5463336`): `dotnet build IdealAkeWms.slnx` 0 Fehler, 30 Warnungen (neu: CS8604 `BfCsv.cs:124`, siehe H2).
`dotnet test`: IdealAkeWms.Tests **3291 bestanden / 1 übersprungen / 0 Fehler** (vor dem QA-Fix 3288/1/0); IDEALAKEWMSService.Tests **790/0/0**.

**2. Review** (Diff samt Umfeld gelesen: `BfCsv`, `BfAuftragWorkflow.FreigabeFehler/FreigebenAsync`, `BfAuftragRepository`, `UsersController.KuerzelPruefenAsync`,
`BeschichtungsauftragWorkflow.BearbeitenAsync`, `Print.cshtml`, `_PrintBriefkopfFuss.cshtml`, `SageUserfelder.Erklaeren` mit allen Aufrufern,
`SageFaArbeitsgangService` Kandidaten/Nachlesen, `SageFaArbeitsgangAnlageRepository.UnklarBestaetigenAsync`, View `SageFaArbeitsgaenge/Index`,
SQL/162, SQL/163, Einmal-Skript, FreshInstall, Manifest, beide Migrationen):

| Nr | Schwere | Befund | Fundstelle | Behandlung |
|---|---|---|---|---|
| F1 | mittel | `IstUnklar` prüfte `StartsWith(UnklarText)`, und genau dieser Text wurde in v1.99.42 geändert. Unklar-Zeilen, die v1.99.38–1.99.41 mit dem alten Text gespeichert haben, wären nach dem Update **automatisch erneut gesendet** worden (Dublette in Sage) und hätten keinen Bestätigen-Knopf bekommen | `IdealAkeWms/Services/SageFaArbeitsgaenge.cs:37-41` (vorher), `Views/SageFaArbeitsgaenge/Index.cshtml:164` | **behoben** `b5463336`: gemeinsamer Anfang `UnklarAnfang = "Antwort unklar"` für Dienst-Regel, Repository und View; Test `SageFaArbeitsgaengeControllerTests.IstUnklar_ErkenntAltenUndNeuenText` (alter Text, neuer Text, echter Fehler). Auf DEV IDEAL nachgeprüft (163.12) |
| H1 | Hinweis | EF-Migration `20261118145602_AddBfPositionArbeitsschritte` legt nur die Spalte an; den Nachtrag der Arbeitsschritte macht allein `SQL/163` | Migration, `SQL/163_AddBfPositionArbeitsschritte.sql` | angenommen: Update-Skript führt SQL/163 in der Schema-Phase aus; das UPDATE läuft auch nach EF-Migrate (auf DEV belegt: 2 Positionen nachgetragen, getrimmt). Kein Fix |
| H2 | Hinweis | neue Warnung CS8604 `Z("", "ORDER_NUMBER", a.Nummer)` mit `string?` | `IdealAkeWms/Services/BfCsv.cs:124` | kosmetisch (freigegebene Aufträge haben immer eine Nummer). Offen |
| H3 | Hinweis | Test-CSV vor der Freigabe hat `ORDER_REMARK_4` leer: Kürzel wird erst bei der Freigabe festgeschrieben (wie Termine/Bemerkung) | `BfAuftragWorkflow` Freigabe, `BfAuftraegeController` Test-CSV | gewollt (Snapshot der Freigabe). Kein Fix |
| H4 | Hinweis | `[RegularExpression]` für das Kürzel läuft vor dem Trimmen: „ WAK“ mit Leerzeichen wird abgelehnt statt getrimmt | `UserEditViewModel` Kuerzel | harmlos (klare Meldung). Kein Fix |
| H5 | Hinweis | Einmal-Skript zeigt in der Zählung `NULL` statt `0`, wenn kein Altauftrag passt (DEV AKE) | `SQL/Einmalig/2026-11-18_v1.99.42_BeschichtungsauftragKreditorNachtragen.sql` Zählblock | kosmetisch; maßgeblich `TREFFER: 0` stimmt. Kein Fix |

Ohne Befund geprüft: keine Werte in `<script>`-Blöcken, kein `hidden` neben `d-*`, kein neues `?? 0`, keine Parallel-Arrays; Kürzel-Eindeutigkeit serverseitig
(`KuerzelPruefenAsync` + gefilterter UNIQUE-Index `UQ_Users_Kuerzel`); Kürzel-, Semikolon- und Ganzzahl-Sperre serverseitig in **einer** Funktion `FreigabeFehler`
(Anzeige und Freigabe); Kreditor-Nachtrag serverseitig (schon gesetzt / kein aktiver Beschichter → abgelehnt); Einmal-Skript-Regel = `KreditorVorbelegen`
(zwei Orte, gleiche Regel, im Skript-Kopf benannt); `Bestaetigen` nur Admin (Class-Level `RequireAdminAccess`), schreibt nichts an Sage.
Audit: Users (Edit), BF-Freigabe, Beschichtungsauftrag (`Geaendert`), Sage-AG-Bestätigen, Einmal-Skript – auf DEV in der DB gesehen.
Migrations-Disziplin: SQL/162 + SQL/163 mit `COL_LENGTH`/`sys.indexes`-Guards, DDL je Batch, History separat; FreshInstall an **beiden** Stellen (Spalten
`Users.Kuerzel`, `BfAuftraege.BestellerKuerzel`, `BfAuftragPositionen.Arbeitsschritte`, Index, beide `MigrationId`); AgentJobs nicht betroffen; nicht daten-destruktiv.
Manifest `nachlauf.psd1`: SQL/162, SQL/163, Einmal-Skript (Trockenlauf, `Braucht` SQL/161), BfCsv-Text nachgezogen; `NachlaufManifestDriftTests` grün.
Version 1.99.42 in beiden `AppVersion.cs`, Anwender-Changelog, TS-163 in `docs/TESTSZENARIEN.md` + Index-Zeile 163. ADR 0020: keine neue/geänderte Einstellung.

**3. Nachweisart:** automatisch belegt: CSV-Aufbau/Kodierung (`BfCsvServiceTests`), Freigabe-Sperren (`BfAuftragWorkflowTests`), Kürzel-Prüfung (`UsersControllerTests`),
Kreditor-Nachtrag (`BeschichtungsauftragWorkflowTests`), Userfeld-Meldung (`SageUserfelderTests`), Unklar-Regel (`SageFaArbeitsgangServiceTests`, `SageFaArbeitsgaengeControllerTests`).
Manual-UAT (nicht als grün gezählt): UNIQUE-Index, SQL/162/163, Einmal-Skript, echtes Model-Binding der Formulare, Druck/PDF-Seitenzähler, WiCAM-Import, echtes SData.
Die Manual-UAT-Teile sind unten auf DEV abgearbeitet, soweit möglich.

**4. Geänderte bestehende Tests** – alle *Verhalten durch die Nachbesserung gewollt geändert*, kein „Fixture bis grün“:
`BfCsvServiceTests` (Vorlage → echte `BF00000002.csv`; Semikolon-Test → `Lagerplatz_KommaStattSemikolon`; Kodierungs-Test von BestellerName auf Arbeitsschritte,
weil der Name nicht mehr in die CSV geht) – P-1 (2); `BfAuftragWorkflowTests` (Fixture-Benutzer mit Kürzel, Materialnummer „M-1“ → „11“ wegen Ganzzahl-Sperre,
Semikolon-Test umbenannt/angepasst, Test für entfernte `Feld()` gelöscht) – P-1 (2) + Kürzel; `SageFaArbeitsgangServiceTests.Nachlesen_KeinFund_…` (Fehler → unklar) – N1;
`NachlaufManifestDriftTests` (SQL/162, SQL/163 ergänzt) – Liste.

**6. DEV-Deploy** (erhöhte Shell; Zielordner `D:\Dev\{AKE,IDEAL}\{Web,Service}` vorher geleert, erhalten `appsettings.DEV.json*`, `logs`, `App_Data`):
`dev-deploy.ps1 -Quelle <Worktree>` je Standort zweimal (Stand `0af91b73`, dann `b5463336`): **AKE** Exit 0 „HTTP 200, Fusszeile v1.99.42“,
**IDEAL** Exit 0 „HTTP 200, Fusszeile v1.99.42“; DEV-Absicherung lief je Standort. Beide Migrationen in beiden DEV-DBs; danach `SQL/162` und `SQL/163`
per `sqlcmd` je DB: Exit 0, idempotent (Spaltenbreiten 20/20/400 Byte, `UQ_Users_Kuerzel` gefiltert `[Kuerzel] IS NOT NULL`).

TS-163 auf DEV IDEAL (Testdaten Kennung `QA-163`; `BfCsvPfad = D:\Dev\wicam-pool\ideal-qa`, `BfCsvAktiv = true` nur für den Test):

| TS | Durchgeführt | Ergebnis | Status |
|---|---|---|---|
| 163.1 | `BF99990163` freigegeben, Dienst schrieb `BF99990163.csv` (792 Byte, `CsvStatus` 2) | 15 Zeilen, nur CRLF, jede Zeile 12 `;`, keine `"`, nur ASCII; Zeilen 1–13 byte-gleich zur Vorlage `BF00000002.csv` bis auf die Auftragsnummer; Position ohne Materialnummer fehlt | OK (Byte-Aufbau); WiCAM-Import **NICHT MÖGLICH** (kein Pool, Manual-UAT) |
| 163.2 | Datenzeilen geprüft | `G-KT-I-BLENDE-SW-80-510-V01;3;2;EILT - 18.11 - 1;K-02,0,0,0;K-02;WAK;19.11.2026;2026-12-02;ST EG KA;;;` und `G-TK-I-BLENDE-SW-80-710-V01;1;5;18.11;H4-02,0,0,0;H4-02;WAK;19.11.2026;2026-12-02;LA KA;;;` (Arbeitsschritte aus SQL/163-Nachtrag, getrimmt) | OK |
| 163.3 | `BF99990164` geöffnet | Block nennt „Pos 1 Matchcode: Semikolon“, „Pos 2: Materialnummer „1,5“ ist nicht ganzzahlig“ und das fehlende Kürzel | OK |
| 163.4 | Testuser Kürzel `wak` gespeichert, Liste `?colf_kuerzel=wak` | DB `WAK`, Audit gesetzt; Spalte „Kürzel“, Filter liefert genau Testuser | OK |
| 163.5 | zweiter Benutzer `WAK`, dann `A B`, dann `zu-lang-12345` | „Kürzel WAK ist schon an Testuser vergeben.“; Formatmeldung „nur Buchstaben und Ziffern, höchstens 10 Zeichen“; `maxlength=10` kappt; nichts gespeichert | OK |
| 163.6 | Freigeben ohne Kürzel; Kürzel nachgetragen; erneut freigeben | erst „Freigabe von BF99990163 abgelehnt: Besteller Testuser hat kein Kürzel …“, Status bleibt, nichts geändert; dann freigegeben ohne Neuabschicken, Details „Testuser (WAK)“, `BestellerKuerzel` WAK | OK |
| 163.7 | `BA-2026-00006` (Übergeben) per SQL auf `CreditorId NULL`, Dienstleister `qa-lack1 `; Auswahl „Lackierer (Kreditor) nachtragen“ → QA-LACK1, Speichern | `CreditorId` 2289, Dienstleister „QA Lackiererei Voll GmbH“, RAL/Termine/Positionen unverändert, Audit gesetzt; Auswahl danach weg | OK |
| 163.8 | – | Direkt-POST ohne Entwicklertools nicht ausführbar; Leser ohne Schreibrecht nicht prüfbar (nur eine Admin-Identität) | NICHT MÖGLICH – Server-Regel automatisch belegt (`Kreditor_InUebergeben_NurNachtragen_SolangeLeer`) |
| 163.9 | Einmal-Skript **nur Trockenlauf** auf DEV IDEAL und AKE | IDEAL: `BA-2026-00006 → 2289 QA Lackiererei Voll GmbH`, `TREFFER: 1`, nichts geändert; stornierte Aufträge mit passendem Kurznamen nicht dabei. AKE: `TREFFER: 0`. Nach 163.7 zweiter Trockenlauf IDEAL `TREFFER: 0` | OK; Scharflauf bewusst nicht |
| 163.10 | `BA-2026-00007` mit 36 Positionen (32 temporär kopiert): PDF-Knopf `/Beschichtungsauftraege/Pdf/7` und Druck über Chrome-Druckmodul (`--print-to-pdf` der Druckansicht) | Barcode klein rechts neben dem Titel, nur waagrechte Linien, Tabellenkopf auf Seite 2 wiederholt, „Seite 1 von 2“ / „Seite 2 von 2“ in **Druck und PDF**; DEV ohne Briefkopf-Vorlage → Zählung auch ohne Fußbereich; einseitig „Seite 1 von 1“ | OK |
| 163.11 | – | braucht Abgleich gegen SData (`Sync:*Enabled` auf DEV verboten) | NICHT MÖGLICH – automatisch belegt (`SageUserfelderTests.Erklaeren_LangeSqlDiagnose_HinweisVorn_AusDiagnose_NieDoppelt`) |
| 163.12 | Warteschlange FA 1060414 per SQL: `PU/180` Fehler mit **altem** Unklar-Text, `IS/200` mit neuem, `VL/190` „HTTP 400“; Seite `?fa=1060414`; Knopf bei PU | Knopf an beiden Unklar-Zeilen, keiner bei HTTP 400; Klick → „FA 1060414: Arbeitsgang 180 als „in Sage vorhanden“ bestätigt“, DB Status 4 (InSageVorhanden), Audit gesetzt. Schalter blieb aus, nichts an Sage | OK (Umlaute der per sqlcmd eingefügten Testtexte verstümmelt – Testdaten-Artefakt, nicht App) |

Nachweise 163.10 im Hauptcheckout `secondbrain/tests/2026-11-18-qa-v1-99-42-druck-pdf/`: `ts-163-10-druck-chrome-ba-2026-00007-2-seiten.pdf` (Druck),
`ts-163-10-pdf-wms-ba-2026-00007-2-seiten.pdf` (PDF-Knopf), `ts-163-10-pdf-wms-ba-2026-00007-1-seite.pdf`, `ts-163-10-druckansicht-bildschirm-ba-2026-00007.png`
(Bildschirmansicht). Bildschirmfotos aus dem Browser-Tab scheiterten am Renderer-Timeout der Erweiterung; Druck und PDF liegen daher als gerenderte PDF-Dateien vor.

**Aufgeräumt (DEV IDEAL, per Abfrage belegt):** BF-Aufträge/-Positionen 0, Merkmal-Definition/-Werte `QA-163` gelöscht, `BfCsvAktiv = false`, `BfCsvPfad` leer,
Ordner `ideal-qa` entfernt, Testuser-Kürzel NULL, `BA-2026-00006` zurück auf `CreditorId 2290` / „QA Lackierer ohne Anschrift“, temporäre Positionen an
`BA-2026-00007` gelöscht, `OperationConfigs` PU/IS/VL `SageArbeitsgangnummerManuell` NULL, Sage-AG-Warteschlange leer. `SageLagerbuchungAktiv`,
`SageFaArbeitsgaengeAnlegenAktiv`, alle `Sync:*Enabled` unverändert aus. SyncLogs bleiben. Nichts an Sage geschrieben, kein Test-/Produktivsystem berührt.

**7. Publish:** in diesem Lauf nicht erstellt – der Aufrufer macht Paket 7 selbst (`publish\` nicht angefasst).

**Deploy (aus dem echten Diff):** Web ja, Dienst ja (`SageFaArbeitsgangService`, Sdata-Abgleiche mit `Erklaeren(ex)`, `BfCsvService` über `BfCsv`),
Migration **ja** (SQL/162, SQL/163; additiv, SQL/163 mit Daten-Nachtrag). Reihenfolge: DB-Backup → SQL/162, SQL/163 (Update-Skript Schema-Phase) → Web → Dienst →
Einmal-Skript Kreditor im Trockenlauf, Scharflauf nur nach Freigabe der Liste + Backup. Vor `BfCsvAktiv`: Kürzel aller BF-Besteller pflegen.
Nach dem Merge nur neu publishen, wenn der Merge getestete Dateien mit parallelen main-Änderungen zusammenführt.

**Manuelle Checkliste (offen, für den Menschen):**
1. 163.1 WiCAM-Import einer vom Dienst geschriebenen Datei am echten Pool (Testsystem IDEAL), Vergleich mit `BF00000002.csv`.
2. 163.8 Direkt-POST mit zweitem Kreditor bzw. Nicht-Beschichter; Leser ohne Schreibrecht sieht keine Auswahl.
3. 163.9 Scharflauf des Einmal-Skripts auf dem Testsystem (nach Backup, über das Update-Skript), danach Trockenlauf `TREFFER: 0`.
4. 163.10 Druck in Edge am Drucker-Dialog sichtbar prüfen (hier über das Chrome-Druckmodul belegt).
5. 163.11 falsche Userfeld-Einstellung gegen echtes SData (Testsystem) je Abgleich, danach zurücksetzen.
6. 163.12 Sage-AG unklar im echten Ablauf; E1b (Schreibtest am IDEAL-Ziel) bleibt offen.
7. Kürzel für alle BF-Besteller vor Go-live pflegen.
8. Testsystem-Durchlauf TS-163 komplett mit dem Publish-Paket, danach Merge (Schranke 2).
