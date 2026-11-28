---
type: spec
title: Update-Skript Review-Befunde UP-1 bis UP-4 (Neustart nach Abbruch, COMMIT im Daten-Skript, Marker-Lokalzeit, Lieferverzug-Hinweis)
slug: 2026-11-28-update-skript-review-befunde-spec
status: Gemerged
created: 2026-11-28
updated: 2026-11-28
source_backlog: "[[2026-11-14-review-befunde-update-skript]]"
task: ""
jira: ""
worktree: ".claude/worktrees/2026-11-28-buendel-review-kommissionierung"
branch: "feature/2026-11-28-buendel-review-kommissionierung"
standort: beide
version: "1.99.93"
ts: "TS-213"
sql: "keine"
affected_code:
  - "scripts/deploy/Update-IdealAkeWms.ps1:677-693 (installierte Version: Fusszeile, sonst deploy-info.txt) - UP-1"
  - "scripts/deploy/Update-IdealAkeWms.ps1:844-845 (deploy-info.txt beim Kopieren) -> nach Schritt 7 (:899-900) - UP-1"
  - "scripts/deploy/Update-IdealAkeWms.ps1:268, :527 (Test-Neuer/Test-Faellig) - UP-1, nur falls Rueckfrage 1 = ja"
  - "scripts/deploy/Update-IdealAkeWms.ps1:315-321 (Show-Zustand, Weg-Texte) - UP-1"
  - "scripts/deploy/Update-IdealAkeWms.ps1:352-356 (Test-SqlFuerTransaktion) - UP-2"
  - "scripts/deploy/Update-IdealAkeWms.ps1:363-411 (Invoke-SqlTransaktion: Batch-Schleife, catch) - UP-2"
  - "scripts/deploy/Update-IdealAkeWms.ps1:585-587 (Fluss A, Text nach fehlgeschlagenem Scharflauf) - UP-2"
  - "scripts/deploy/Update-IdealAkeWms.ps1:334-340 (Set-MarkerBefehl, SYSDATETIME) - UP-3"
  - "scripts/deploy/Set-Startkennwort.ps1:87, :89 (GETDATE) - UP-3, nur falls Rueckfrage 2 = ja"
  - "scripts/deploy/nachlauf.psd1:532-538 (v1.99.19-LieferverzugSchwellen, Pruefung/LieferverzugSammellisteTage) - UP-4"
  - "IdealAkeWms.Tests/Deploy/NachlaufManifestDriftTests.cs:132-153 (DatenFehler) + neue Text-Waechter"
  - "IdealAkeWms/AppVersion.cs, IDEALAKEWMSService/AppVersion.cs, Views/Help/Changelog.cshtml (Versionssprung)"
  - "docs/betrieb/03-installation-update-rollback.md:168, :302; README.md:1052; docs/TESTSZENARIEN.md (TS-213)"
open_questions:
  - "1. UP-1: Soll zusaetzlich der Versionsfilter fuer Typ Daten entfallen (Daten wie Einmalig, Marker/Spur entscheiden), damit ein im Nachlauf mit 'n = spaeter' bzw. ohne SCHARF offen gelassener Daten-Eintrag beim naechsten Lauf wieder angeboten wird? Konflikt: kehrt den abgenommenen Stand TS-126.22 (SQL/108/SQL/110 'nicht faellig') um."
  - "2. UP-3: Marker-Zeit per @Jetzt = Get-Date (Lokalzeit des Rechners, auf dem das Skript laeuft) statt ADR-0025-Ausnahme - und Set-Startkennwort.ps1:87/89 (ModifiedAt = GETDATE()) gleich mit umstellen?"
  - "3. UP-4: Den alten Einstellungs-Eintrag v1.99.19-LieferverzugSchwellen (Firma beide, Text 'Sammelliste Tage nach Liefertermin = 1') auf 'AKE: ... = 1, IDEAL: Standard 3 lassen' umformulieren? Er erscheint auf jedem System unter v1.99.19 und widerspricht sonst dem Entscheid 'Default bleibt 3'."
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

## Ziel / Nutzen (das Warum)

Das Update-Skript ist der einzige Deploy-Weg (ADR [[0022-update-je-zielsystem-per-skript-mit-nachlauf-manifest]], ADR
[[0023-daten-und-einmal-skripte-fuehrt-das-update-skript-mit-bestaetigung-aus]]). Der Review vom 2026-11-14 hat vier offene Befunde
gefunden, die Sichtung 2026-11-28 gegen main v1.99.89 bestaetigt sie alle (Code unveraendert, Zeilennummern unten neu belegt):

- **UP-1:** Nach einem Abbruch ab Schritt 5 verschwinden faellige Nachlauf-Eintraege beim Neustart still. Das verletzt „melden statt still
  behandeln“ – z. B. der Backfill `SQL/110` fehlt danach in der Liste, der Lauf meldet Erfolg.
- **UP-2:** Ein unpaariges `COMMIT` (oder `USE`) in einem Daten-Skript beendet die aeussere Transaktion samt Marker. Das Skript meldet dann
  ggf. „zurueckgerollt, DB unveraendert“, obwohl Daten festgeschrieben sind.
- **UP-3:** Der Marker-Zeitstempel kommt von der SQL-Server-Uhr. Das bricht ADR [[0025-zeitstempel-in-lokalzeit-des-schreibenden-rechners]].
- **UP-4:** `Pruefung/LieferverzugSammellisteTage` meldet bei jedem Lauf eine Abweichung, solange der Wert nicht 1 ist. Gerald hat
  entschieden: Katalog-Default bleibt 3, AKE setzt 1 per Einstellung, eine Pruefung erzwingt keinen Wert.

## Umfang (In-Scope / Out-of-Scope)

**In-Scope:** UP-1, UP-2, UP-3, UP-4 in `scripts/deploy/`, die Drift-Waechter dazu, Betriebsdoku/README, TS-213, Versionssprung.

**Out-of-Scope:**
- Pester-Tests fuer die Skriptlogik. Es gibt keine (Backlog „Geprueft ohne Befund“), und diese Spec fuehrt keine ein. Nachweis per
  Text-Waechter im Drift-Test plus DEV-Lauf TS-213. Einfuehren, sobald eine zweite Skriptlogik-Regel ungetestet bricht.
- Install-Skript, SData-Kennwort (SD-4/SD-5 in [[2026-11-14-review-befunde-sdata]]).
- `SYSDATETIME()` in den Rollen-Reparaturen `nachlauf.psd1:199, :209, :288, :313` und `GETDATE()` in der Pruefung `:302`. Das sind
  SQL-Seeds bzw. ein Lesevergleich, und ADR 0025 laesst `GETDATE()` in SQL-Seeds ausdruecklich zu („bleibt – Lokalzeit des SQL-Servers“).
- Die Einstellung `Lieferverzug:TageNachTermin` selbst. Default 3 bleibt (`IDEALAKEWMSService/Services/LieferverzugMailService.cs:49`,
  `IdealAkeWms/Models/ServiceSettingDefinitions.cs:422`).

## Fachliche Anforderungen

1. **UP-1:** Ein Neustart nach Abbruch an beliebiger Stelle ab Schritt 3 bietet dieselben Nachlauf-Eintraege an wie der abgebrochene Lauf
   (Daten, Einmalig, Einstellung, Info, Leitstand). Daten-/Einmal-Eintraege, die schon gelaufen sind, verhindert weiter der Marker.
2. **UP-2:** Ein Daten-/Einmal-/Reparatur-Skript, das die Transaktion des Update-Skripts beenden oder die Datenbank wechseln kann, wird vor
   der Ausfuehrung abgelehnt. Beendet ein Skript sie trotzdem, sagt die Meldung die Wahrheit: ggf. festgeschrieben, kein Marker, von Hand
   pruefen. Ein falsches „DB unveraendert“ gibt es nicht mehr.
3. **UP-3:** `DeployScriptRuns.ExecutedAt` ist die Lokalzeit des Rechners, auf dem das Update-Skript laeuft (Web-Server). Altdaten bleiben
   unveraendert (ADR 0025).
4. **UP-4:** Kein Update-Lauf meldet eine Abweichung, weil `Lieferverzug:TageNachTermin` nicht 1 ist. Auf AKE-Systemen zeigt die
   Nachlauf-Liste einmal (beim Sprung auf die neue Version) den Hinweis „AKE: Vorgabe Einkauf 1 Tag - aktuellen Wert pruefen“ samt
   aktuellem Wert. IDEAL bekommt keinen Hinweis.

## Technischer Loesungsentwurf

### UP-1 – „installiert vorher“ ueberlebt den Abbruch

Befund, im Hauptcheckout nachgemessen:
- `Update-IdealAkeWms.ps1:680-681`: Die installierte Version kommt zuerst aus der Fusszeile, `:686-688` nur ohne Fusszeile aus
  `{Web}\deploy-info.txt`, `:689-691` sonst per Eingabe.
- `:844-845`: `deploy-info.txt` (= Paket-`publish-info.txt`, `:102`) wird **direkt nach dem robocopy** geschrieben.
- Fall a – Abbruch in 5, 5a (`:853`), 5b oder beim Web-Start (`:868-875`, App-Pool wird wieder gestoppt): Beim Neustart gibt es keine
  Fusszeile, `deploy-info.txt` liefert die Paketversion.
- Fall b – **zusaetzlich zum Backlog gefunden:** Abbruch nach erfolgreichem Web-Start, also Dienst laeuft nicht (`:891`), Dienst-DLL
  falsch (`:893`) oder Abbruch in Schritt 7. Beim Neustart zeigt die Fusszeile schon die Paketversion. Daran aendert die Position von
  `deploy-info.txt` allein nichts.
- Der Filter trifft **nicht nur Daten**: `Show-Nachlauf :907` filtert alle Typen ausser Schema/Ordner/Pruefung mit `Test-Faellig`, also
  `Test-Neuer` (`:268`, `:527`). Bei Fall a verschwinden deshalb auch Einstellung-, Info- und Leitstand-Eintraege. Die wurden nie gezeigt,
  weil Schritt 7 nicht erreicht war. 4a (`:821`), 5b (`:856`) und `Get-OffeneEintraege` (`:529`) filtern gleich.

Loesung, zwei kleine Aenderungen an der Stelle, durch die alle Filter laufen (`$installiert`):
1. **`deploy-info.txt` erst am Ende des Laufs schreiben:** Die beiden `Copy-Item $infoDatei …deploy-info.txt` aus `:844-845` werden zu
   Zeilen nach `Show-Nachlauf $e $installiert $true` (`:899`) und vor `return 0` (`:900`), je nach `$tauschWeb`/`$tauschDienst` wie
   bisher. robocopy schliesst `deploy-info.txt` weiter aus (`:638`). `-NurPruefen` (`:747-753`) und „Ende ohne DB-Kontakt“ (`:672`)
   schreiben weiter nichts.
2. **Die kleinere Version gewinnt:** In Schritt 1 wird `deploy-info.txt` auch gelesen, wenn die Fusszeile eine Version liefert. Ist sie
   **kleiner** als die Fusszeile, gilt sie (`$quelleVersion = 'deploy-info.txt (letzter vollstaendiger Lauf; Fusszeile vX)'`).
   Begruendung: Nach Fall b steht die Fusszeile schon auf der neuen Version, `deploy-info.txt` noch auf der alten. Eine zu kleine Version
   zeigt hoechstens Einstellung-/Info-Texte doppelt. Daten/Einmalig schuetzt der Marker, Schema die History. Ohne `deploy-info.txt`
   (Systeme vor dem Skript) bleibt es bei der Fusszeile.

Damit stimmt der Weg-Text fuer 5/5a/5b (`:318`) wieder („bietet offene Eintraege erneut an“). Die Texte fuer `6` (`:319`) und `default`
(`:320`) bekommen den Zusatz „Nachlauf-Eintraege ab v<installiert vorher>“. Der Text nennt die Version, die der naechste Lauf als
Untergrenze nimmt.

Bekannte Grenze, Entscheid offen in **Rueckfrage 1**: Laesst der Mensch in einem **vollstaendig** durchgelaufenen Lauf einen Daten-Eintrag
offen („n = spaeter“ `:604`, kein SCHARF `:579`), ist er beim naechsten Lauf nicht mehr faellig. Das ist so seit v1.99.1 (TS-126.22). Nur
falls Rueckfrage 1 = ja: `Test-Faellig` (`:527`) wird zu `($m.Typ -in 'Einmalig','Daten') -or (Test-Neuer …)`. Folge: `SQL/110` macht
je System einmal einen Trockenlauf, bis „entfaellt“ vermerkt ist. `SQL/108`/`SQL/147` erledigt die Spur (`nachlauf.psd1:351, :357`). Der
Vorpruefungs-Text „nicht faellig (eingefuehrt mit …)“ (`:712`) gilt dann nur noch fuer Typen ohne Marker. TS-126.22 bekommt den Vermerk
„ueberholt durch TS-213“.

### UP-2 – Transaktion des Skripts bleibt die einzige

Befund: `Test-SqlFuerTransaktion` (`:352-356`) lehnt nur sqlcmd-Direktiven und `ROLLBACK` ab. **Der Backlog-Vorschlag „`\bCOMMIT\b`
ablehnen“ ist so nicht umsetzbar:** Alle Trockenlauf-/Transaktion-Skripte im Manifest enthalten ein **gepaartes**
`BEGIN TRANSACTION … COMMIT TRANSACTION`, nachgemessen per Grep:
- `SQL/110_BackfillStockMovementProductionOrderId.sql:76/87`
- `SQL/Einmalig/2026-11-08_v1.88.0_…Altbestand.sql:108/140`
- `…v1.99.32…:94/102`
- `…v1.99.42…:66/78`
- `…v1.99.72…:274/284`
- `…v1.47.4…:76/83`

Gepaart ist harmlos: `BEGIN TRAN` erhoeht `@@TRANCOUNT` auf 2, `COMMIT` senkt auf 1, die aeussere Transaktion bleibt. Gefaehrlich ist
nur **unpaarig**: `COMMIT` ohne `BEGIN` faellt auf 0, ein `BEGIN` ohne `COMMIT` bleibt auf 2. Im zweiten Fall schliesst `$tx.Commit()`
nur eine Ebene, und `$cn.Dispose()` rollt still zurueck, obwohl „uebernommen“ gemeldet wurde. `USE` kommt in keinem Daten-/Einmal-Skript
vor (Grep `^\s*USE\s` trifft nur `SQL/00-37`). Im Manifest (`nachlauf.psd1`) gibt es weder `COMMIT`, `BEGIN TRAN` noch `USE`.

Loesung:
1. **Statisch** in `Test-SqlFuerTransaktion` (vor jeder Ausfuehrung, gilt fuer Fluss A, Fluss B und Reparatur):
   - Zeile `^\s*USE\b` -> abgelehnt, Grund „USE (wechselt die Datenbank, Marker landete woanders)“.
   - Anzahl `\bCOMMIT\b` groesser als Anzahl `\bBEGIN\s+TRAN(SACTION)?\b` -> abgelehnt, Grund „COMMIT ohne BEGIN TRANSACTION (beendete die
     aeussere Transaktion samt Marker)“.
   - Ein Kommentar mit dem Wort COMMIT zaehlt mit. Das ist die sichere Seite: abgelehnt, nicht ausgefuehrt.
2. **Zur Laufzeit** in `Invoke-SqlTransaktion`: nach jedem Batch (`:374-386`, nach `$r.Dispose()`) in derselben Transaktion
   `SELECT @@TRANCOUNT, DB_NAME()`.
   - Erwartet: genau `1` und `$e.Db.Datenbank`.
   - Abweichung, oder die Abfrage selbst wirft -> Abbruch mit eigener Meldung `${name}: Transaktion nach Batch $nr nicht mehr die des
     Update-Skripts (@@TRANCOUNT x, DB y) -> …`. Danach `Rollback` versuchen.
   - Gelingt der Rollback (z. B. `USE`, `@@TRANCOUNT 2`), gilt die bisherige Meldung „zurueckgerollt, kein Marker, DB unveraendert“.
   - Wirft er (`@@TRANCOUNT 0`, Transaktion beendet), heisst die Meldung „Aenderungen bis Batch $nr ggf. festgeschrieben, kein Marker. Stand
     von Hand pruefen; Backup <Datei aus diesem Lauf>“.
   - Das Ergebnisobjekt bekommt dafuer `Fehler`. Den Text liefert genau eine Stelle, die bestehende `Write-Warnung` im catch
     (`:403-407`). Der stille `try { $tx.Rollback() } catch { }` (`:405`) entscheidet dann den Text.
   - **Unbelegt, im Test TS-213.4 messen:** wie SqlClient auf ein serverseitiges `COMMIT` reagiert (Transaktion „completed“, naechster
     Befehl mit `$tx` wirft). Das Skript behandelt deshalb beide Wege, Pruefabfrage liefert 0 und Pruefabfrage wirft, gleich.
   - `# ponytail: ein COMMIT mitten im Batch mit nachfolgendem Fehler im selben Batch meldet der Fehler-Zweig; Upgrade: Pruefung vor dem
     Rollback im catch`.
3. Fluss A: `& $nein 'Scharflauf fehlgeschlagen (zurueckgerollt).'` (`:587`) nennt nicht mehr pauschal „zurueckgerollt“, sondern verweist
   auf die Warnung davor: „Scharflauf nicht uebernommen (Meldung siehe Warnung davor).“ Das gleiche Muster steht schon bei der Reparatur
   `:461`.
4. Drift-Test `DatenFehler` (`NachlaufManifestDriftTests.cs:132-153`) bekommt dieselben zwei Regeln wie das Skript (Kopie wie
   `LeseWaechter :171`, Text-Waechter: Skript enthaelt die Regex woertlich). Rotprobe: synthetischer Text mit `COMMIT` ohne `BEGIN` bzw.
   `USE [x]` macht den Waechter rot, die echten Dateien bleiben gruen.

### UP-3 – Marker in Lokalzeit (Empfehlung, Rueckfrage 2)

`Set-MarkerBefehl` (`:334-340`) ist die einzige Stelle, die `DeployScriptRuns` schreibt. `Write-Marker :342-347` und der Marker in der
Transaktion `:387-391` laufen beide darueber. Aenderung: `SYSDATETIME()` -> `@Jetzt`, Wert `Get-Date`, als `SqlDbType.DateTime2`. Die
Spalte ist `datetime2 NOT NULL` ohne Default (`SQL/142_AddDeployScriptRuns.sql:24`, `SQL/00_FreshInstall.sql:3246`), also keine
Schemaaenderung. `ExecutedBy = SUSER_SNAME()` bleibt, das ist keine Zeit. Anzeige `Get-Marker :332` unveraendert.
Falls Rueckfrage 2 auch `Set-Startkennwort.ps1` umfasst: `:87, :89` `ModifiedAt = GETDATE()` -> `@Jetzt` (Parameter wie `@h`/`@w` dort).
Alternative laut Backlog: ADR-0025-Ausnahme dokumentieren statt umstellen. Nicht empfohlen, weil die Umstellung kuerzer ist als die
Ausnahme und keinen zweiten Konventionsfall schafft.

### UP-4 – Hinweis statt Pruefung

Die Pruefung `nachlauf.psd1:535-538` (Firma beide, Treffer bei Wert <> 1) **entfaellt**. Ein Eintrag Typ `Einstellung` ersetzt sie. Er
erscheint nur beim Versionssprung (`Show-Nachlauf :907`) und zeigt den aktuellen Wert ueber `Pruefabfrage` (`:920`, lesend ueber
`Invoke-Lesen` mit Schreibwort-Sperre):

```
@{ Id='v1.99.93-LieferverzugSammellisteTage'; Version='1.99.93'; Typ='Einstellung'; Firma='AKE'; Phase='Nachher'; Ausfuehrung='anzeigen'
   Pruefabfrage='SELECT ISNULL((SELECT TOP 1 LTRIM(RTRIM([Value])) FROM dbo.ServiceSettings WHERE [Key] = N''Lieferverzug:TageNachTermin''), N''kein Eintrag (Standard 3)'') AS TageNachTermin'
   Text='AKE: Vorgabe Einkauf 1 Tag - aktuellen Wert pruefen (/ServiceSettings > Lieferverzug-Mail, Sammelliste Tage nach Liefertermin; Katalog-Standard bleibt 3).'
   Quelle='2026-11-28-update-skript-review-befunde' }
```

Nur ASCII (Waechter `DeploySkripte_NurAscii_OhneSage_RechteNurHinzufuegend`, Drift-Test `:305`), deshalb „-“ statt Gedankenstrich. Die
Version folgt der endgueltigen Paketversion des Dev-Laufs. Rueckfrage 3 betrifft den Alt-Eintrag `v1.99.19-LieferverzugSchwellen`
(`:532-534`). Kollisionshinweis: Die parallele Lieferverzug-Spec (v1.99.94) beruehrt evtl. dieselben Manifest-Zeilen. Beim Merge die
Reihenfolge pruefen.

### Muster

Kein neues Muster. Die eine Regel liegt an der Stelle, durch die alle Aufrufer laufen: `$installiert` fuer alle Filter,
`Test-SqlFuerTransaktion`/`Invoke-SqlTransaktion` fuer Fluss A, Fluss B und Reparatur, `Set-MarkerBefehl` fuer alle Marker. Betroffenes
Modul: Update-Skript ([[module]], [[feature-map]]).

## Migrations-/SQL-Auswirkungen

Keine Migration, kein SQL-Skript, keine SQL-Nummer (176 bleibt frei). `DeployScriptRuns.ExecutedAt` ist ohne Default (`SQL/142:24`). Damit
gibt es keine OBJECT_ID-Pruefung und keine Aenderung an `00_FreshInstall.sql`.

## Einstellungen

keine. `Lieferverzug:TageNachTermin` bleibt wie sie ist (Int 0–60, Default 3, `ServiceSettingDefinitions.cs:240, :422`); nur der
Manifest-Hinweis aendert sich.

| Key | Typ | Erlaubte Werte | Default | Wirkung | Verwendung | Standort | Warnhinweis |
|---|---|---|---|---|---|---|---|
| – | – | – | – | – | – | – | – |

## Audit-Feld-Auswirkungen

- `dbo.DeployScriptRuns.ExecutedAt`: ab v1.99.93 Lokalzeit des Rechners mit dem Update-Skript (`Get-Date`), vorher SQL-Server-Uhr.
  Altzeilen bleiben (ADR 0025). Auf AKE/IDEAL ist die Abweichung unbelegt, auf DEV ohne Wirkung (SQL-Server = DEV-PC).
- Nur falls Rueckfrage 2 = ja: `dbo.Users.ModifiedAt` aus `Set-Startkennwort.ps1` ebenso.
- Keine Rollen- oder Zugriffsfilter-Aenderung. Das Skript laeuft als Administrator am Zielserver (`:1`). Keine neue Tabellen-View, also
  keine Listen-View-Pflichten.

## Akzeptanzkriterien

1. `Copy-Item … deploy-info.txt` steht im Skript nur noch nach `Show-Nachlauf $e $installiert $true` und vor dem abschliessenden
   `return 0` von `Invoke-System`. Ein Text-Waechter im Drift-Test prueft die Reihenfolge per Index.
2. Schritt 1 nimmt bei Fusszeile **und** `deploy-info.txt` die kleinere Version und nennt beide in der Zeile „Installiert: …(Quelle: …)“.
3. AKE-DEV, Abbruch in Schritt 6 (TS-213.1), danach Neustart: Die Vorpruefung zeigt „Installiert“ = Version vor dem abgebrochenen Lauf.
   Plan und Nachlauf-Liste enthalten dieselben Eintraege wie der erste Lauf.
4. AKE-DEV, Abbruch nach erfolgreichem Web-Start (Dienst startet nicht, TS-213.2), danach Neustart: Die Fusszeile zeigt die neue Version,
   „Installiert“ zeigt die alte (Quelle `deploy-info.txt`). Die Nachlauf-Liste zeigt die Einstellung-/Info-Eintraege der uebersprungenen
   Versionen.
5. Ein Transaktion-Skript mit `COMMIT` ohne `BEGIN TRANSACTION` wird mit „abgelehnt (COMMIT ohne BEGIN TRANSACTION …) -> nichts
   ausgefuehrt“ abgewiesen. Mit `USE [master]` entsprechend „abgelehnt (USE …)“.
6. Alle Daten-/Einmal-Skripte im Manifest passieren die neue statische Regel (Drift-Test gruen). Die Rotprobe mit unpaarigem COMMIT bzw.
   `USE` ist rot.
7. Ein Skript, das die statische Regel umgeht (TS-213.4: `-- BEGIN TRAN` als Kommentar plus `COMMIT`), endet mit der Meldung
   „Transaktion nach Batch n nicht mehr die des Update-Skripts …“. Bei `@@TRANCOUNT 0` heisst es „ggf. festgeschrieben, kein Marker“, nie
   „DB unveraendert“. Es gibt keinen Marker.
8. `Update-IdealAkeWms.ps1` enthaelt kein `SYSDATETIME()` mehr (Text-Waechter). Der Marker-INSERT nutzt den Parameter `@Jetzt`.
9. Neuer Marker auf DEV: `ExecutedAt` liegt innerhalb +-1 min von `Get-Date` am Rechner des Skripts.
10. `nachlauf.psd1` enthaelt keinen Eintrag `Pruefung/LieferverzugSammellisteTage` mehr. `-NurPruefen` auf AKE-DEV und IDEAL-DEV zeigt
    im Block „Pruefungen“ keine Lieferverzug-Tage-Zeile.
11. AKE-DEV mit installiert < 1.99.93: Die Nachlauf-Liste zeigt `v1.99.93-LieferverzugSammellisteTage` mit „Pruefabfrage: <Wert>“ (bzw.
    „kein Eintrag (Standard 3)“). IDEAL-DEV zeigt den Eintrag nicht.
12. Der Drift-Test `NachlaufManifestDriftTests` ist komplett gruen. Der Build hat keine neuen Warnungen. Alle Deploy-Dateien sind ASCII.
13. Nur falls Rueckfrage 1 = ja: Ein Daten-Eintrag, im Nachlauf mit „n = spaeter“ offen gelassen, erscheint beim naechsten Lauf wieder als
    „offen“. Die Vorpruefung zeigt fuer Daten kein „nicht faellig“ mehr.
14. Doku: `docs/betrieb/03-installation-update-rollback.md:168` (Versionsquelle mit Minimum-Regel) und `:302` (deploy-info.txt am
    Laufende, neue Zeilennummer) sind nachgezogen, ebenso `README.md:1052` (keine Pruefung mehr, Hinweis AKE). Anwender-Changelog hat den
    Eintrag v1.99.93.

## Test-Szenarien

Neues Kapitel **TS-213 (vorlaeufig) – Update-Skript: Neustart nach Abbruch, Transaktionsschutz, Marker-Zeit, Lieferverzug-Hinweis** in
`docs/TESTSZENARIEN.md`. Vorbedingungen wie TS-133 (`docs/TESTSZENARIEN.md:12071-12072`): DB-Kopie der DEV, Paketkopie mit eigener
`ziele.psd1`-Zeile auf die Kopie, nur DEV.

| Nr | Schritt | Erwartung |
|---|---|---|
| 213.1 | Lauf mit falscher `-SiteUrl` (Fusszeile HTTP-Fehler) bis Abbruch in Schritt 6, dann Neustart mit richtiger URL | „Installiert“ = Vorversion (Quelle deploy-info.txt bzw. Eingabe), Nachlauf-Liste wie im ersten Lauf, Daten-Eintraege offen |
| 213.2 | Lauf, nach Web-Start Dienst am Starten hindern (z. B. Dienstkonto-Kennwort falsch auf der Kopie), dann Neustart | Fusszeile neu, „Installiert“ alt (Quelle deploy-info.txt, Fusszeile genannt), Einstellung-/Info-Eintraege erscheinen |
| 213.3 | Synthetisches Transaktion-Skript mit `COMMIT` ohne `BEGIN` bzw. mit `USE [master]` im Manifest der Paketkopie | „abgelehnt (…) -> nichts ausgefuehrt“, kein Marker |
| 213.4 | Synthetisches Skript `-- BEGIN TRAN` + `UPDATE …` + `COMMIT` (umgeht die statische Regel) | Meldung „Transaktion nach Batch 1 nicht mehr die des Update-Skripts (@@TRANCOUNT 0 …) … ggf. festgeschrieben“, kein Marker; Verhalten von SqlClient protokollieren (unbelegt) |
| 213.5 | Fluss-A-Lauf auf der Kopie, danach `SELECT ExecutedAt FROM dbo.DeployScriptRuns` | Zeit = `Get-Date` am Skript-Rechner (+-1 min) |
| 213.6 | `-NurPruefen` AKE-DEV und IDEAL-DEV | keine Pruefung Lieferverzug-Tage; AKE: Hinweis v1.99.93 mit aktuellem Wert; IDEAL: kein Hinweis |
| 213.7 | Regression: TS-126 Kernfaelle (Marker, Spur, SCHARF, uebernehmen/verwerfen) auf der Kopie | unveraendert |

TS-126.22 bekommt nur falls Rueckfrage 1 = ja den Vermerk „ueberholt durch TS-213“.

## Etappen (nur bei epic: true)

entfaellt (kein Epic).

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | – | – | – |

## Deploy

**Final (QA 2026-11-28, aus dem echten Diff `ff681409` + QA-Fix `a83d8756`).**

- **Web-App:** ja – nur Versionssprung `IdealAkeWms/AppVersion.cs` (1.99.93) und Anwender-Changelog `Views/Help/Changelog.cshtml`. Kein
  Verhalten der Anwendung geaendert.
- **Service:** ja – nur Versionssprung `IDEALAKEWMSService/AppVersion.cs` (1.99.93).
- **Migration:** nein (kein neuer Eintrag unter `*/Migrations/`, kein SQL-Skript, kein `00_FreshInstall.sql`).
- **Update-Skript selbst:** `scripts/deploy/Update-IdealAkeWms.ps1`, `Set-Startkennwort.ps1`, `nachlauf.psd1` reisen im Paket unter
  `publish\deploy\` mit. Wirksam ab dem Lauf **mit** diesem Paket (Minimum-Regel fuer den Neustart greift erst, wenn der abgebrochene Lauf
  schon dieses Paket war).
- **Manifest** (`nachlauf.psd1`): neu `v1.99.93-LieferverzugSammellisteTage` (Einstellung, Firma AKE, Nachher, anzeigen, Pruefabfrage
  aktueller Wert); entfernt `Pruefung/LieferverzugSammellisteTage`; Text `v1.99.19-LieferverzugSchwellen` auf „AKE = 1, IDEAL Standard 3“.
  Kein manueller Deploy-Schritt ausserhalb des Manifests noetig.
- **Folge beim ersten Lauf mit diesem Paket (Freigabe-Antwort 1):** Daten-Eintraege sind immer faellig. Auf Systemen, auf denen `SQL/108`
  bzw. `SQL/147` nie lief (Spur 0, so auf AKE-DEV fuer `SQL/108`), steht der Eintrag jetzt unter „vor dem Start“ – das stoppt Web **und**
  Dienst und fragt in 5b j/n/e; `SQL/110` macht je System einmal einen Trockenlauf bis „entfaellt“ vermerkt ist.
- **Paket (fertig):** `C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-11-28-buendel-review-kommissionierung\publish` – v1.99.93, Commit
  `a83d875671b7865c181ee62b660fc94605b18cff`, `IDEALAKEWMSWeb` + `IDEALAKEWMSWebService` + `SQL` + `deploy` + `publish-info.txt`. Beide
  Komponenten deployen (Reihenfolge ohne Besonderheit, kein Backup-Zwang aus dieser Spec; ein Haltepunkt mit Treffern verlangt es wie bisher).
  Ablauf Mensch: Paket auf das Testsystem -> `deploy\Update-IdealAkeWms.ps1 -NurPruefen`, dann ohne -> testen -> Merge.
- Nach dem Merge nur dann neu aus `main` publishen, wenn der Merge getestete Dateien mit parallelen main-Aenderungen zusammengefuehrt hat:
  ```
  dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb
  dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService
  ```

## Offene Rueckfragen

1. UP-1: Soll zusaetzlich der Versionsfilter fuer Typ `Daten` entfallen (Daten wie Einmalig, Marker und Spur entscheiden)? Dann wird ein
   Daten-Eintrag, der im Nachlauf mit „n = spaeter“ bzw. ohne SCHARF offen blieb, beim naechsten Lauf wieder angeboten. **Konflikt:** Das
   kehrt den abgenommenen Stand TS-126.22 um (`SQL/108`/`SQL/110` „nicht faellig“, Spec
   [[2026-11-11-update-skript-prueft-und-fuehrt-aus-mit-bestaetigung-spec]] Auftragspunkt 2). Kosten: je System einmal Trockenlauf
   `SQL/110` bis „entfaellt“. Empfehlung: ja. Nur so stimmt „offen (spaeter)“, und der Marker verhindert Doppellaeufe (ADR 0023).
2. UP-3: Marker-Zeit per `@Jetzt = Get-Date` statt einer ADR-0025-Ausnahme (Empfehlung: ja)? Soll `Set-Startkennwort.ps1:87/89`
   (`ModifiedAt = GETDATE()`) gleich mit umgestellt werden (Empfehlung: ja, zwei Zeilen, derselbe Konventionsbruch)?
3. UP-4: Den alten Einstellungs-Eintrag `v1.99.19-LieferverzugSchwellen` (`nachlauf.psd1:532-534`, Firma beide, Text „Sammelliste Tage nach
   Liefertermin = 1 (Einkauf, Till)“) umformulieren auf „AKE: = 1 (Einkauf, Till); IDEAL: Standard 3 lassen“? Er erscheint auf jedem
   System unter v1.99.19 und widerspricht sonst dem Entscheid „Default bleibt 3“. Empfehlung: ja, nur Text.

## ANTWORTEN auf Rueckfragen - Fachbereich (aus Jira)

<!--
  Von der Koordination eingetragen, wortgetreu, je Antwort:
  <Nr>. <Name>, <yyyy-mm-dd>, [<Jira-Schluessel> Kommentar](<Link>): "<Antwort>"
  Das ist fachlicher Input, keine Freigabe. Entscheidend bleibt der Freigabe-Block darunter;
  uebernimmt der Mensch eine Antwort, genuegt dort "wie Fachbereich".
-->

(keine Fachbereichs-Fragen; alle Rueckfragen gehen an Gerald)

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
  Bei Varianten-Specs zusaetzlich freigabe_entscheidung im Frontmatter setzen.
-->

1. UP-1 Versionsfilter fuer Daten entfallen lassen (TS-126.22 ueberholt)? → **ja, wie empfohlen** (Gerald 2026-11-28)
2. UP-3 `@Jetzt = Get-Date` statt ADR-Ausnahme; `Set-Startkennwort.ps1` mit umstellen? → **ja, beides wie empfohlen** (Gerald 2026-11-28)
3. UP-4 Text `v1.99.19-LieferverzugSchwellen` auf AKE = 1 / IDEAL Standard 3 umformulieren? → **ja, wie empfohlen** (Gerald 2026-11-28)

## QA-Nachweis (qa-agent, 2026-11-28)

Worktree `.claude/worktrees/2026-11-28-buendel-review-kommissionierung`, Branch `feature/2026-11-28-buendel-review-kommissionierung`.
Gepruefter Bereich: Umsetzungs-Commit `ff681409`, QA-Fix `a83d8756`. Korrektur durch QA: Version **1.99.93** statt 1.99.95, Szenario
**TS-213** statt TS-216, Manifest-Id `v1.99.93-LieferverzugSammellisteTage` (Frontmatter und Text angepasst). `standort: beide` war gesetzt.

### 1. Build und Tests (selbst ausgefuehrt)
- `dotnet build IdealAkeWms.slnx`: 0 Fehler, 37 Warnungen (alle vorbestehend: CS86xx in Tests, NU1902/NU1903 Paket-Hinweise).
- `dotnet test` auf `ff681409`: IdealAkeWms.Tests 3884 bestanden / 1 uebersprungen / 0 fehlgeschlagen; IDEALAKEWMSService.Tests 848 / 0 / 0.
- Nach dem QA-Fix `a83d8756` erneut: Build 0 Fehler; IdealAkeWms.Tests 3884 / 1 / 0; IDEALAKEWMSService.Tests 848 / 0 / 0.
- PS-5.1-Parser (`Parser::ParseFile`) fuer `Update-IdealAkeWms.ps1`: 0 Fehler. Alle `scripts/deploy/*.ps1|psm1|psd1` reines ASCII (grep auf
  Nicht-ASCII: 0 Treffer); Zeilenende CRLF wie vorher.

### 2. Code-Review (selbst, synchron)
Gelesen: kompletter Diff `ff681409`, dazu `Invoke-SqlTransaktion`, `Invoke-Pruefung`, `Invoke-FlussA/B`, `Invoke-Eintrag`,
`Get-OffeneEintraege`, `Show-Zustand`, `Show-Nachlauf`, `Invoke-System` (Schritte 1–7), Hauptlauf; die sechs Transaktion-/Trockenlauf-Skripte
(`SQL/110`, `Einmalig` v1.88.0/v1.99.32/v1.99.42/v1.99.72/v1.47.4) auf GO, `BEGIN TRAN`, `COMMIT`, `RETURN` zwischen beiden.

| # | Befund | Schwere | Behandlung |
|---|---|---|---|
| Q-1 | **SqlClient nach serverseitigem COMMIT (TS-213.4 gemessen):** Die Pruefabfrage liefert `@@TRANCOUNT 0` (wirft nicht), und `$tx.Rollback()` wirft **ebenfalls nicht**. Die bewusste Abweichung der Umsetzung („ggf. festgeschrieben“ nur bei verletzt **und** gescheitertem Rollback) meldete deshalb „zurueckgerollt, kein Marker, DB unveraendert“, obwohl die Aenderung festgeschrieben war (Probetabelle `v = 1` nach dem Lauf). AK 7 war verletzt – genau der Fehler, den UP-2 beseitigen soll. | hoch | **behoben** `a83d8756`: `$beendet = $tc -eq 0` beim Anschlag der Laufzeitpruefung; Meldung „ggf. festgeschrieben“ bei `$verletzt -and ($beendet -or -not $rbOk)`. XACT_ABORT-Fehler (nicht verletzt) bleibt „Fehler in Batch n -> zurueckgerollt“ (gemessen korrekt, `v = 0`). Text-Waechter im Drift-Test ergaenzt. |
| Q-2 | `Invoke-Pruefung`, Reparatur in Transaktion: `Stop-Mit "Reparatur zurueckgerollt (Meldung siehe Warnung davor)"` – nach UP-2 kann die Warnung davor „ggf. festgeschrieben“ lauten. | niedrig | **behoben** `a83d8756`: „Reparatur nicht uebernommen (Meldung siehe Warnung davor)“, wie Fluss A. |
| – | StrictMode 2: `$script:InstalliertVorher` wird in der ersten Zeile von `Invoke-System` gesetzt, `Show-Zustand` laeuft nur im catch danach; im Zweig `Ermittlung|1|2` wird die Variable nicht gelesen. `$tc`/`$db` in try und catch gesetzt; `GetString(1)` auf `DB_NAME()` der aktuellen DB ist nie NULL; `$beendet` initialisiert. | – | keine Befunde |
| – | Reparatur-Pfad `$marker = $null`: Laufzeitpruefung greift genauso, kein Marker-INSERT; Manifest-Reparaturen ohne `BEGIN TRAN`/`COMMIT`/`USE` (grep). Die RCSI-Reparatur laeuft `OhneTransaktion`, nicht ueber `Invoke-SqlTransaktion`. | – | keine Befunde |
| – | „Daten immer faellig“: `SQL/110` (Nachher, Trockenlauf, ohne Spur) macht je Lauf einen Trockenlauf bis zum Marker; `SQL/108`/`SQL/147` (VorStart, Transaktion) erledigt die Spur. Wo die Spur 0 ist (AKE-DEV: `SQL/108`, Schluessel `OseonRueckmeldungAktiv`/`SageRueckmeldungAktiv` noch da; kein Seed legt sie neu an – `AppSettingDefinitions.cs:245`, DEV-Absicherung grep leer), steht der Eintrag unter „vor dem Start“ und zwingt Web **und** Dienst zum Stopp (auch bei `-Komponente Dienst`). Gewollte Folge der Freigabe-Antwort 1, im Deploy-Abschnitt vermerkt. | Hinweis | dokumentiert |
| – | Haltepunkt-Abbruch vor dem Stoppen: Phase Haltepunkt hat nur Einmalig v1.88.0 (war schon immer faellig), keinen Daten-Eintrag; „Daten faellig“ fuegt keinen neuen Haltepunkt hinzu. | – | keine Befunde |
| – | `deploy-info.txt` bei `-NurPruefen` / „Ende ohne DB-Kontakt“ / ohne Tausch: richtig **nicht** geschrieben (beide `return` liegen vor Schritt 7; `$tauschWeb`/`$tauschDienst` steuern weiter). Schreiben in diesen Faellen wuerde die Minimum-Regel aushebeln. | – | keine Befunde |
| – | Gepaarte `BEGIN TRANSACTION … COMMIT TRANSACTION` in allen sechs Skripten im selben Batch (kein GO, kein RETURN dazwischen) – keine Falschablehnung durch die Laufzeitpruefung. `EXEC('USE master')` wechselt die DB nur im inneren Scope (gemessen: Lauf korrekt uebernommen). | – | keine Befunde |
| – | Defekt-Klassen der Vorlage (`<script>`-Einbettung, `hidden`+`d-*`, stille Fallbacks, Positionsbindung, nur-Browser-Validierung): nicht beruehrt (kein View-/JS-/Binder-Code). „Eine Regel an mehreren Stellen“: USE/COMMIT-Regel im Skript und Kopie im Drift-Test, der Text-Waechter prueft die Woertlichkeit. | – | keine Befunde |

### 3. Beweisart je Akzeptanzkriterium
| AK | Beweis |
|---|---|
| 1 | automatisiert (`Skript_DeployInfoAmLaufende_MarkerInLokalzeit`, Index-Reihenfolge) |
| 2 | DEV belegt (213.6: „Installiert: v1.99.92 (Quelle: deploy-info.txt (letzter vollstaendiger Lauf; Fusszeile v1.99.93))“) |
| 3, 4 | Manual-UAT (213.1/213.2, Abbruch-Neustart auf DB-Kopie) – offen |
| 5 | DEV-Messung mit den echten Skriptfunktionen (213.3), zusaetzlich Text-Waechter |
| 6 | automatisiert (`Manifest_CommitOhneBeginOderUse_MachtWaechterRot`, `Manifest_DatenUndEinmalSkripte_NurMitBestaetigung`) |
| 7 | DEV-Messung (213.4) nach QA-Fix gruen; vorher rot (Q-1) |
| 8 | automatisiert (Text-Waechter: kein `SYSDATETIME()`, `@Jetzt` vorhanden) |
| 9 | DEV-Messung (213.5, Marker-Zeit = Get-Date); Unterschied zur SQL-Server-Uhr auf DEV nicht sichtbar (gleicher Rechner) |
| 10, 11 | DEV belegt (213.6) + automatisiert (kein Manifest-Eintrag `Pruefung/LieferverzugSammellisteTage`) |
| 12 | automatisiert (Drift-Test gruen, ASCII-Waechter) |
| 13 | teilweise DEV (Vorpruefung ohne „nicht faellig“, 213.6); Wiederkehr nach „n = spaeter“ Manual-UAT (213.7) – offen |
| 14 | Review: `docs/betrieb/03` :168 und Tabelle deploy-info, `README.md` Lieferverzug-Zeile, Anwender-Changelog v1.99.93 vorhanden |

### 4. Geaenderte bestehende Tests
- `NachlaufManifestDriftTests.DatenFehler` (Helfer des bestehenden `Manifest_DatenUndEinmalSkripte_NurMitBestaetigung`): zwei Regeln
  (USE, unpaariges COMMIT) ergaenzt – Verhalten bewusst geaendert durch AK 5/6. Keine Fixture angepasst. Sonst nur neue Tests.

### 5. Checkliste CLAUDE.md
Keine Migration/SQL/FreshInstall/AgentJobs (keine Schemaaenderung). Audit: `Users.ModifiedAt` aus `Set-Startkennwort.ps1` jetzt Lokalzeit
(ADR 0025), `ModifiedBy`/`ModifiedByWindows` weiter gesetzt. Version in beiden `AppVersion.cs` 1.99.93, Anwender-Changelog und
Brain-Changelog `2026-11-28-v1-99-93-update-skript-review-befunde.md` vorhanden. `docs/TESTSZENARIEN.md` TS-213 + Vermerk an TS-126.22,
Testindex-Zeile 213 vorhanden. 5a ADR 0020: keine neue oder geaenderte Einstellung (nur Manifest-Hinweis) – entfaellt.
Der Deploy-Schritt der Spec (Hinweis AKE) steht in `nachlauf.psd1`.

### 6. DEV-Deploy und TS-213
Shell erhoeht (`net session` ok). `D:\Dev\scripts\dev-deploy.ps1 -Standort AKE|IDEAL -Quelle <worktree>` auf `ff681409`: beide Exit 0,
„HTTP 200, Fusszeile v1.99.93“, „Fertig: AKE v1.99.93 auf http://localhost:8081/“ bzw. „Fertig: IDEAL v1.99.93 auf http://localhost:8082/“.
Der QA-Fix `a83d8756` aendert nur `scripts/deploy/*` und einen Test – Web und Dienst auf DEV sind damit codegleich, kein Neu-Deploy noetig.

| Schritt | Durchfuehrung | Ergebnis | Status |
|---|---|---|---|
| 213.1 | – | Abbruch-Neustart braucht einen interaktiven Volllauf auf einer DB-Kopie mit eigener `ziele.psd1`-Zeile | NICHT MOEGLICH (Mensch) |
| 213.2 | – | dito, Dienst am Start hindern | NICHT MOEGLICH (Mensch) |
| 213.3 | Echte Funktionen `Test-SqlFuerTransaktion`/`Invoke-SqlTransaktion`/`Set-MarkerBefehl` per AST aus dem Skript geladen (PS 5.1), Wegwerf-DB `QA_TS213` auf `.` mit der `SQL/142`-Tabelle, danach geloescht | `COMMIT` ohne BEGIN: „abgelehnt (COMMIT ohne BEGIN TRANSACTION …) -> nichts ausgefuehrt“; `USE [master]`: „abgelehnt (USE …)“; jeweils kein Marker, Daten unveraendert | OK |
| 213.4 | wie 213.3, `-- BEGIN TRAN` + `UPDATE` + `COMMIT` | vor dem Fix: „… (@@TRANCOUNT 0, DB QA_TS213) -> zurueckgerollt, … DB unveraendert“, Daten **festgeschrieben** -> Q-1. Nach dem Fix: „Transaktion nach Batch 1 nicht mehr die des Update-Skripts (@@TRANCOUNT 0, DB QA_TS213) -> Aenderungen bis Batch 1 ggf. festgeschrieben, kein Marker. Stand von Hand pruefen; Backup keines aus diesem Lauf.“ SqlClient: Pruefabfrage wirft nicht (liefert 0), Rollback wirft nicht. Zusatz: `BEGIN TRAN` ohne COMMIT -> @@TRANCOUNT 2 -> zurueckgerollt (Daten 0); gepaart -> uebernommen + Marker; XACT_ABORT-Fehler -> „Fehler in Batch 1 … zurueckgerollt“ (Daten 0) | OK (nach Fix) |
| 213.5 | wie 213.3, normaler Lauf mit Marker | `ExecutedAt` 2026-11-28 12:11:56.360, `Get-Date` 12:11:56.364 (UTC 11:11) – Lokalzeit. `Set-Startkennwort.ps1` nicht ausgefuehrt (nur Text-Waechter) | OK / Startkennwort offen |
| 213.6 | Paket-Skript `publish\deploy\Update-IdealAkeWms.ps1 -Firma AKE bzw. IDEAL -System DEV -NurPruefen` (powershell.exe 5.1), vorher Probe-`deploy-info.txt` (Version 1.99.92) in `D:\Dev\<Standort>\Web`, danach entfernt | beide Exit 0, „-NurPruefen: nichts gestoppt, nichts geschrieben“; „Installiert: v1.99.92 (Quelle: deploy-info.txt (letzter vollstaendiger Lauf; Fusszeile v1.99.93))“; kein `Pruefung/LieferverzugSammellisteTage` im Block Pruefungen; AKE Nachlauf Nr. 6 `[Einstellung] v1.99.93-LieferverzugSammellisteTage` mit „Pruefabfrage: 3“; IDEAL ohne den Eintrag; kein „nicht faellig“; AKE-Plan „vor dem Start: SQL/108“ (Spur 0) | OK |
| 213.7 | – | „n = spaeter“ und Folgelauf brauchen einen interaktiven Volllauf | NICHT MOEGLICH (Mensch) |
| 213.8 | – | Regression TS-126-Kernfaelle auf DB-Kopie | NICHT MOEGLICH (Mensch) |

DEV-DBs: `DeployScriptRuns` auf AKE_WMS_DEV und IDEAL_WMS_DEV weiter 0 Zeilen (nichts geschrieben). Kein Sage-Zugriff.

### 7. Publish
`C:\Git\IDEAL-AKE-WMS\.claude\worktrees\2026-11-28-buendel-review-kommissionierung\publish` (vorher geleert): `IDEALAKEWMSWeb`,
`IDEALAKEWMSWebService` (beide `dotnet publish` Exit 0), `SQL`, `deploy`, `publish-info.txt` (Version 1.99.93, Commit
`a83d875671b7865c181ee62b660fc94605b18cff`, Branch, Spec, Erstellt 2026-11-28 12:13, Komponenten). Das Paket-Skript lief daraus (213.6).

**Ergebnis: Testbereit** (Build/Tests gruen, Review-Befunde Q-1/Q-2 behoben und nachgemessen, DEV v1.99.93 an beiden Standorten, Paket fertig).

### Manuelle Checkliste (Mensch, Schranke 2)
1. TS-213.1 auf einer DEV-DB-Kopie: Lauf mit falscher `-SiteUrl` bis Abbruch in 6, Neustart -> „Installiert“ = Vorversion, Nachlauf-Liste wie im ersten Lauf, „Weiter: … Nachlauf-Eintraege ab v…“.
2. TS-213.2 auf der Kopie: Dienst nach dem Web-Start am Starten hindern, Neustart -> Fusszeile neu, „Installiert“ alt (Quelle deploy-info.txt mit Fusszeile).
3. TS-213.7: Daten-Eintrag mit „n = spaeter“ offen lassen, Folgelauf -> wieder „offen“.
4. TS-213.8: TS-126-Kernfaelle (Marker, Spur, SCHARF, uebernehmen/verwerfen) auf der Kopie.
5. TS-213.5 Rest: `Set-Startkennwort.ps1` auf der Kopie, `Users.ModifiedAt` = Lokalzeit; auf dem Testsystem (SQL-Server ungleich Web-Server) die Marker-Zeit gegen die Uhr des Web-Servers pruefen.
6. Erster Lauf mit diesem Paket auf AKE-Test/IDEAL-Test: erst `-NurPruefen`. Zu erwarten: `SQL/108`/`SQL/147` mit Spur 0 stehen unter „vor dem Start“ (stoppt Web und Dienst), `SQL/110` macht einmal einen Trockenlauf -> „entfaellt“ vermerken.
7. AKE: Der Nachlauf-Hinweis `v1.99.93-LieferverzugSammellisteTage` zeigt den aktuellen Wert; im Einkauf auf 1 pruefen. IDEAL: kein Hinweis.
8. Danach Merge (Schranke 2).
