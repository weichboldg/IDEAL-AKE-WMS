---
type: spec
title: "Bugfix Update-Skript: zweiter Trockenlauf des Altbestand-Skripts liefert TREFFER 0"
slug: 2026-11-13-updater-zweiter-trockenlauf-altbestand-spec
status: InUmsetzung
created: 2026-11-13
updated: 2026-11-13
source_backlog: "[[2026-11-13-updater-zweiter-trockenlauf-altbestand]]"
bug: "[[2026-11-13-updater-zweiter-trockenlauf-altbestand-bug]]"
task: "[[2026-11-13-buendel-v1-99-16-goal-lauf]]"
worktree: ".claude/worktrees/2026-08-07-ideal-teile-1-5"
branch: "feature/2026-08-07-ideal-teile-1-5"
standort: beide
affected_code:
  - "SQL/Einmalig/2026-11-08_v1.88.0_Kommissionierauftraege_Altbestand.sql:47-51 (Wiederholungssperre: im Trockenlauf TREFFER 0 statt RAISERROR)"
  - "scripts/deploy/Update-IdealAkeWms.ps1:582-583 (zweiter Trockenlauf: fehlende Zeile TREFFER und Exit-Code ehrlich melden)"
  - "IdealAkeWms.Tests/Deploy/NachlaufManifestDriftTests.cs:120-137 (DatenFehler: neue Regel Sperre in Trockenlauf-Datei) + neue Rotprobe nach Muster :114-116"
  - "IdealAkeWms/AppVersion.cs, IDEALAKEWMSService/AppVersion.cs, IdealAkeWms/Views/Help/Changelog.cshtml (Vorschlag v1.99.16)"
  - "docs/TESTSZENARIEN.md (Vorschlag TS-133)"
  - "Bug-Record [[2026-11-13-updater-zweiter-trockenlauf-altbestand-bug]]"
open_questions:
  - "1. Nebenwirkung: Schlägt die Spur-Abfrage fehl (Get-SpurZahl :273 liefert dann $null), obwohl der Altbestand schon lief, läuft heute Fluss A in die Sperre. Am Haltepunkt bricht das Skript dann ab, Web und Dienst bleiben gestoppt. Nach dem Fix meldet der Trockenlauf TREFFER 0, das Skript bietet den Marker 'Entfaellt' an und läuft weiter. Ist das gewollt (Empfehlung: ja, der Altbestand ist erledigt, und die Sperre verhindert jeden Scharflauf weiter)?"
  - "2. Drift-Wächter: Die neue Regel erkennt nur Sperren der Form 'IF EXISTS (…) BEGIN … RAISERROR/THROW … END' (heute die einzige Form in den Trockenlauf-Dateien). Reicht diese Heuristik (Empfehlung), oder soll jede Sperre ein festes Kennzeichen tragen (z. B. Kommentar '-- SPERRE'), das der Test erzwingt?"
  - "3. Soll der Dev-Lauf die Regel 'auch die Wiederholungssperre gibt im Trockenlauf TREFFER: 0 aus' zusätzlich in .claude/agents/spec-agent.md und secondbrain/_templates/spec.md (Hauptcheckout, Abschnitt Manifest/ADR 0023) ergänzen (Empfehlung: ja, eine Zeile je Datei)?"
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

## Ziel / Nutzen (das Warum)

Am AKE Test-IDEAL (13.11.2026, Update-Skript v1.99.12) meldete das Update-Skript nach einem **erfolgreichen** Scharflauf des
Altbestand-Skripts: „zweiter Trockenlauf TREFFER:  (erwartet 0)“. Diese Warnung erscheint künftig bei jedem Scharflauf dieses Skripts, auf
jedem System, und sie führt in die Irre. Der Mensch am Zielserver kann nicht erkennen, ob der Lauf gelungen ist. Fluss A (ADR
[[0023-daten-und-einmal-skripte-fuehrt-das-update-skript-mit-bestaetigung-aus]]) soll nach dem Scharflauf mit „TREFFER: 0“ bestätigen.

Der Fehler hat keinen Datenschaden verursacht. Der Scharflauf war committet, und der Marker steht in derselben Transaktion
(`Update-IdealAkeWms.ps1:579`, `:382-387`).

## Umfang (In-Scope / Out-of-Scope)

**In-Scope**
1. Altbestand-Skript: Die Wiederholungssperre endet im Trockenlauf mit `TREFFER: 0` und Exit 0. Im Scharflauf bleibt sie unverändert ein
   Abbruch (`RAISERROR 16`).
2. Update-Skript `:582-583`: Meldet der zweite Trockenlauf keine Zeile `TREFFER` oder endet er mit Exit ≠ 0, nennt die Warnung genau das,
   samt Exit-Code und Hinweis „Scharflauf ist übernommen (Marker Scharf)“. Ein leerer Wert erscheint nicht mehr.
3. Drift-Wächter in `NachlaufManifestDriftTests`: Eine Sperre in einer Trockenlauf-Datei ohne `TREFFER: 0` macht den Test rot. Dazu
   eine Rotprobe.
4. TS-133 (Vorschlag), Version v1.99.16 (Vorschlag), Changelog, Brain-Nachträge.

**Out-of-Scope**
- Variante 2 der Backlog-Notiz („keine TREFFER-Zeile nach Scharflauf als Hinweis statt Warnung“): Sie behandelt nur das Symptom und
  verdeckte künftige echte Fehler im zweiten Trockenlauf. Teil 2 macht die Meldung nur genauer, er stuft sie nicht herab.
- `SQL/110`, `SQL/108`, `SQL/147`, v1.47.4 und die 00er-Bereinigung: Sie brauchen keine Änderung (Befund unten).
- Neue Manifest-Einträge und neue Felder.
- Nacharbeiten an Daten des AKE Test-IDEAL: Es gibt keinen Datenschaden. Der Prüfschritt steht unten.

## Fachliche Anforderungen

### Belegter Befund (Bündel-Worktree v1.99.15)

| Schritt | Beleg |
|---|---|
| Fluss A: nach dem Commit des Scharflaufs ein zweiter Trockenlauf | `scripts/deploy/Update-IdealAkeWms.ps1:579-582` |
| Der Trockenlauf läuft über `sqlcmd … -b` (Exit ≠ 0 bei Fehler mit Schwere > 10) | `Invoke-Trockenlauf` `:551-556`, `Invoke-SqlDatei` `:248-258`, `Get-SqlcmdArgs` `:246` |
| `TREFFER` wird per Regex `^\s*TREFFER: (\d+)\s*$` gelesen; ohne Zeile ist das Ergebnis `$null` | `Get-Treffer` `:355` |
| Die Auswertung prüft nur `$t2.Treffer -ne 0`; `$null -ne 0` ist wahr, `Exit` wird ignoriert → Warnung mit leerem Wert | `:583` |
| Die Sperre greift, sobald Aufträge mit `CreatedBy = N'Migration-KO-Altbestand'` da sind, also nach jedem Scharflauf. `RAISERROR … 16; RETURN;` gilt ohne Unterschied für Trockenlauf und Scharflauf | `SQL/Einmalig/2026-11-08_v1.88.0_Kommissionierauftraege_Altbestand.sql:47-51` (`@Wer` `:35`, Scharflauf schreibt `CreatedBy = @Wer` `:113`) |
| Die Zeile `TREFFER` steht erst im Trockenlauf-Zweig hinter der Sperre | `…Altbestand.sql:92-99` (`PRINT` `:96`) |
| Der Drift-Test prüft nur, ob irgendwo `TREFFER: ` vorkommt | `IdealAkeWms.Tests/Deploy/NachlaufManifestDriftTests.cs:132` |
| Die Abnahme TS-126.7 („zweiter Trockenlauf TREFFER: 0“) lief mit einem synthetischen Skript ohne Sperre | `docs/TESTSZENARIEN.md:11570-11574, 11584` |

Die Zeilennummern der Backlog-Notiz stimmen bis auf eine Stelle: Der Sperr-Block reicht von 47 bis 51 statt 46 bis 50. Die
Prüfabfrage der Notiz nennt die Spalte `ScriptId`. Diese Spalte gibt es nicht; richtig ist `ManifestId` (`SQL/142_AddDeployScriptRuns.sql:16`).

### Wo die Sperre im Trockenlauf überhaupt erreicht wird (alle Wege)

- **Vorprüfung** (`:709-720`): Trockenlauf nur für `Get-OffeneEintraege`. Das sind Einträge **ohne Marker und ohne Spur > 0** (`:522-525`).
  Spur des Altbestands = genau die Sperrbedingung (`nachlauf.psd1:157`), also hier nicht erreichbar.
- **4a / `Invoke-Eintrag`** (`:531-550`): Marker → Ende (`:532-533`), Spur > 0 → Frage „Marker Spur“, Ende (`:536-541`). Fluss A
  startet nur bei Spur = 0 oder `$null`.
- **Zweiter Trockenlauf in Fluss A** (`:582`): **immer** nach einem erfolgreichen Scharflauf, denn der hat die Spur erzeugt. Das ist der Bug.
- **Grenzfall** Spur-Abfrage wirft (`Get-SpurZahl` `:273` liefert `$null`). Dann erreichen Vorprüfung und Fluss A die Sperre:
  - heute: Exit 1 → in der Vorprüfung „nicht bestimmbar, Backup Pflicht“ (`:718-719`), in 4a Abbruch am Haltepunkt (`:560`, `:564`);
  - nach dem Fix: `TREFFER: 0` → „entfällt“, Angebot Marker `Entfaellt` (`:565-568`, `:809-812`). Siehe Rückfrage 1.

### Andere Daten-/Einmal-Skripte im Manifest (alle `Datei`-Einträge in `nachlauf.psd1` durchgesehen)

Fluss A (Ausführung `Trockenlauf`, zweiter Trockenlauf `:582`) haben nur zwei Einträge:

| Manifest-Id | Ausführung | Sperre / Abbruch vor `TREFFER`? | Betroffen? |
|---|---|---|---|
| `Einmalig/2026-11-08_v1.88.0_Kommissionierauftraege_Altbestand` (`nachlauf.psd1:154-159`) | Trockenlauf, Haltepunkt | Schema-Guards `:37-46` (SQL/133, SQL/141), **Wiederholungssperre `:47-51`**, Standardbereich `:52-57` | **ja**, nur die Sperre `:47-51`. Die anderen drei können nach einem Scharflauf nicht greifen: Tabellen und Spalte sind da, und `Pruefung/Standardbereich` prüft den Standardbereich vorher. |
| `SQL/110` (`nachlauf.psd1:172-176`) | Trockenlauf, Nachher | nur Schema-Guard `SQL/110:19-23` (SQL/109); idempotent über `ProductionOrderId IS NULL` (`:30`, `:85`), keine Sperre | nein, der zweite Trockenlauf liefert `TREFFER` aus `:70-71` |
| `SQL/108` (`:160-165`) | Transaktion (Fluss B, kein zweiter Trockenlauf) | kein `RAISERROR`/`RETURN` (Grep) | nein |
| `SQL/147` (`:166-171`) | Transaktion | kein `RAISERROR`/`RETURN` (Grep) | nein |
| `Einmalig/2026-10-07_v1.47.4_…` (`:177-181`) | Transaktion | kein `RAISERROR`/`RETURN` (Grep) | nein |
| `Einmalig/2026-10-15_…Bereinigung00erArtikel` (`:182-187`) | anzeigen (Fluss C, das Skript führt nicht aus) | Sperre `:72-76` greift **nur bei `@Loeschen = 1`**, Schalter `@Loeschen` statt `@Trockenlauf` | nein. Die Sperre gilt dort schon richtig nur für den scharfen Modus und ist das Vorbild für den Fix. |
| Prüfungs-Reparaturen `133_…sql`, `86_…sql` (`:95-121`) | `sqlcmd`, kein Trockenlauf | – | nein |
| `Einmalig/2026-09-28_Hotfix-1.30.1_…` | bewusst nicht im Manifest (`NachlaufManifestDriftTests.cs:19`, `:84-85`) | – | nein |

Ergebnis: Der Fix betrifft genau eine SQL-Datei.

### Teil 1: Sperre im Trockenlauf (Altbestand-Skript)

Den Sperr-Block `:47-51` so umbauen (nach dem Muster der 00er-Bereinigung `:72-76`, die nur im scharfen Modus sperrt):

```sql
IF EXISTS (SELECT 1 FROM dbo.PickingOrders WHERE CreatedBy = @Wer)
BEGIN
    IF @Trockenlauf = 1
    BEGIN
        -- v1.99.16: Trockenlauf nach erfolgtem Lauf (z. B. zweiter Trockenlauf des Update-Skripts) -> erledigt, nichts zu tun
        PRINT N'Altbestand wurde bereits ueberfuehrt (Auftraege mit CreatedBy = Migration-KO-Altbestand vorhanden). Nichts zu tun.';
        PRINT N'TREFFER: 0';
        RETURN;
    END
    RAISERROR(N'Altbestand wurde bereits ueberfuehrt (...). NICHT ERNEUT AUSFUEHREN. Abbruch.', 16, 1);
    RETURN;
END
```

Pflichten:
- **Kein zweites Literal `DECLARE @Trockenlauf BIT = 1;`.** Das Update-Skript ersetzt genau ein Vorkommen und bricht sonst ab
  (`:576-577`), der Drift-Test zählt ebenfalls (`NachlaufManifestDriftTests.cs:131`). Im Block nur `IF @Trockenlauf = 1`.
- `@Trockenlauf` ist vor der Sperre deklariert (`:33`), alles liegt in einem Batch (die Datei hat kein `GO`). Das `RETURN` im inneren Block
  beendet den Batch.
- Die Zeile genau als `TREFFER: 0` am Zeilenanfang, passend zu `Get-Treffer` (`:355`).
- Datei bleibt ASCII (`DeploySkripte_NurAscii…`, `NachlaufManifestDriftTests.cs:226`), kein `ROLLBACK`, keine `sqlcmd`-Direktive (`:133-134`).
- Warnkopf „NICHT ERNEUT AUSFUEHREN“ (`:2`) bleibt. Er gilt weiter für den Scharflauf (ADR 0015).
- Die Reihenfolge der Guards bleibt: Schema-Guards `:37-46` vor der Sperre. Fehlt SQL/133 oder SQL/141, ist das weiter ein Fehler und kein
  „erledigt“.

**Vorhandene Marker bleiben gültig.** `Get-Marker` liest nur `ManifestId` (`:327`), der gespeicherte `FileSha256` wird nie verglichen.
Ein geänderter Datei-Hash bietet den Eintrag also auf keinem System erneut an.

### Teil 2: Meldung des zweiten Trockenlaufs (`Update-IdealAkeWms.ps1:582-583`)

Drei Fälle, in dieser Reihenfolge:
1. `$null -eq $t2.Treffer` oder `$t2.Exit -ne 0` → `Write-Warnung "<Id>: zweiter Trockenlauf ohne Zeile TREFFER (Exit n). Scharflauf ist
   übernommen (Marker Scharf); Ausgabe im Log-Ordner prüfen."` Der Pfad der Ausgabedatei steht schon vorher in der Konsole (`:256`).
2. `$t2.Treffer -ne 0` → bisherige Warnung „TREFFER: n (erwartet 0)“.
3. sonst → bisheriges `Write-Ok`.

Nur Text und Bedingung ändern sich: kein Abbruch, kein neuer Ablauf, die Marker-Logik bleibt unverändert. Der Text bleibt ASCII (Umlaute
umschreiben wie im übrigen Skript) und ohne `"$Name?"` (`DeploySkripte_KeineVariableMitFragezeichenImText`, `:205`).

### Teil 3: Drift-Wächter (verhindert das Wiederauftreten)

**Heute verhindert keine Prüfung den Fehler:**
- `DatenFehler` verlangt für Trockenlauf-Dateien nur, dass irgendwo `TREFFER: ` steht (`:132`). Das erfüllt das fehlerhafte Skript durch
  `:96`.
- TS-126.7 prüft den zweiten Trockenlauf nur mit einem synthetischen Skript ohne Sperre.

**Neue Regel in `DatenFehler`** (nur `Ausfuehrung = Trockenlauf`):
- Jeder Block `IF EXISTS (…) BEGIN … END`, der `RAISERROR` oder `THROW` enthält, muss im selben Block `TREFFER: 0` ausgeben.
- Fehlermeldung: `"<Id>: Sperre (IF EXISTS … RAISERROR) ohne TREFFER: 0 im Trockenlauf"`.

Hinweise für den Dev-Lauf:
- Heutige Treffer der Form: nur `…Altbestand.sql:47`. Die anderen Guards sind `IF OBJECT_ID … IS NULL`, `IF COL_LENGTH … IS NULL` und
  `IF @Standard IS NULL` (`:37`, `:42`, `:53`; `SQL/110:19`) und fallen nicht unter die Regel. Sie sollen auch im Trockenlauf scheitern,
  denn dann fehlt eine Voraussetzung.
- Die Regex darf nicht über ein `;` hinweg zum nächsten `BEGIN` springen (z. B. `(?is)\bIF\s+EXISTS\b[^;]*?\bBEGIN\b(.*?)\bEND\b`). Das
  verschachtelte `BEGIN … END` des Fixes ist unkritisch: Der kurze Treffer endet am inneren `END` und enthält `TREFFER: 0`.
- Code-Kommentar mit Obergrenze: `// ponytail: erkennt nur IF EXISTS-Sperren; Sperren als IF (SELECT COUNT…) > 0 rutschen durch – dann Kennzeichen-Konvention (Rückfrage 2)`.

**Rotprobe** nach dem Muster `Manifest_TrockenlaufDateiOhneTreffer_MachtWaechterRot` (`:114-116`):
`DatenFehler(ManifestText, d => SqlText(d).Replace("TREFFER: 0", "TREFFER: X"))` enthält die neue Meldung. Hinweis: `TREFFER: 0` kommt im
Altbestand-Skript nach dem Fix nur im Sperr-Block vor (`:96` lautet `CONCAT(N'TREFFER: ', …)`). Die bestehende Prüfung `:132` bleibt
dabei grün.

### Prüfschritt auf AKE Test-IDEAL (Mensch, kein Code)

Bestätigt, dass der Scharflauf vom 13.11. vollständig ist. Spaltennamen nach `SQL/142:14-30`:

```sql
SELECT ManifestId, Mode, Hits, PackageVersion, ExecutedAt, ExecutedBy
FROM dbo.DeployScriptRuns WHERE ManifestId LIKE N'Einmalig/2026-11-08%';
SELECT COUNT(*) AS Auftraege FROM dbo.PickingOrders WHERE CreatedBy = N'Migration-KO-Altbestand';
```

Erwartet: eine Zeile `Mode = Scharf`, `Hits` = Treffer des ersten Trockenlaufs, und Aufträge > 0. Bis Gerald das bestätigt hat, ist
„Scharflauf vollständig“ für dieses System unbelegt.

## Technischer Loesungsentwurf

- Teil 1: Änderung in einer Datei, eine T-SQL-Verzweigung. Vorbild ist das vorhandene Muster der 00er-Bereinigung (`:72`), also keine
  neue Konvention.
- Teil 2: eine `if/elseif/else`-Kette statt der einzeiligen Bedingung `:583`, keine neue Funktion.
- Teil 3: eine Bedingung mehr in der vorhandenen `DatenFehler`-Schleife (`:125-135`) und ein `[Fact]`. Kein neuer Testansatz.
- `publish/` im Worktree ist Build-Ausgabe (`publish/SQL/Einmalig/…`) und wird nicht von Hand bearbeitet. Das Paket baut der Publish neu.

## Migrations-/SQL-Auswirkungen

Keine Migration, kein neues `SQL/XX`, kein `00_FreshInstall.sql`, keine `SQL/AgentJobs`. Geändert wird nur der Inhalt des Einmal-Skripts
(ADR 0015). Für einen Neulauf auf einem frischen System ändert sich nichts: Ohne Spur greift die Sperre nicht.

## Einstellungen

keine

## Audit-Feld-Auswirkungen

Keine. Der neue Trockenlauf-Zweig schreibt nichts. Der Scharflauf-Zweig mit seinen Audit-Feldern (`…Altbestand.sql:104-131`) bleibt
unverändert.

## Akzeptanzkriterien

1. Altbestand-Skript im Trockenlauf (`@Trockenlauf = 1`) auf einer DB mit Aufträgen `CreatedBy = N'Migration-KO-Altbestand'`: Exit 0,
   Ausgabe enthält die Zeile `TREFFER: 0` und den Hinweis „bereits ueberfuehrt … Nichts zu tun“, keine Fehlermeldung, DB unverändert.
2. Dasselbe Skript mit `@Trockenlauf = 0` auf derselben DB: weiter `RAISERROR` (Schwere 16) „NICHT ERNEUT AUSFUEHREN“, nichts geändert.
3. Auf einer DB ohne solche Aufträge liefern Trockenlauf und Scharflauf dieselbe Ausgabe wie vor dem Fix: Zählung, Liste, `TREFFER: n`.
4. Fehlt SQL/141 (Spalte `AltbestandPruefen`), bricht der Trockenlauf wie bisher mit Fehler ab. Es gibt kein `TREFFER: 0`.
5. Die Datei enthält das Literal `DECLARE @Trockenlauf BIT = 1;` genau einmal, ist ASCII, ohne `ROLLBACK` und ohne `sqlcmd`-Direktive.
6. Fluss A nach `SCHARF`: Der zweite Trockenlauf meldet `OK … zweiter Trockenlauf TREFFER: 0`, keine Warnung.
7. Liefert der zweite Trockenlauf keine Zeile `TREFFER` oder Exit ≠ 0, nennt die Warnung „ohne Zeile TREFFER (Exit n)“ und „Scharflauf ist
   übernommen“. Nirgends steht mehr „TREFFER:  (erwartet 0)“ mit leerem Wert.
8. Liefert der zweite Trockenlauf `TREFFER: n > 0`, bleibt die bisherige Warnung „TREFFER: n (erwartet 0)“.
9. `NachlaufManifestDriftTests`: alle grün. Die neue Rotprobe (Altbestand ohne `TREFFER: 0` im Sperr-Block) ist rot mit der Meldung
   „Sperre … ohne TREFFER: 0“. `SQL/110` löst die neue Regel nicht aus.
10. Ein bestehender Marker `Scharf` oder `Entfaellt` des Altbestands wird nach dem Update auf v1.99.16 weiter als „erledigt am …“ gezeigt
    und nicht erneut angeboten, obwohl sich der Datei-Hash geändert hat.
11. `dotnet build IdealAkeWms.slnx` und `dotnet test` grün. Version in beiden `AppVersion.cs` und im Anwender-Changelog.

## Test-Szenarien

Vorschlag **TS-133 „Update-Skript: zweiter Trockenlauf nach Scharflauf mit Wiederholungssperre (v1.99.16)“** für `docs/TESTSZENARIEN.md`
(nach TS-132). Danach `secondbrain/tests/testszenarien-index.md` nachziehen. Die Nummer prüft der Dev-Lauf neu.

**Vorbedingungen (Agent, nur DEV, wie TS-126):**
- DB-Kopie der DEV und Paketkopie mit eigener `ziele.psd1`-Zeile.
- Echte Daten- und Einmal-Skripte **nur im Trockenlauf** (ADR 0023).
- Scharfpfad nur mit einem synthetischen Eintrag `Test/Sperre`:
  - Die Datei liegt nur in der Paketkopie und schreibt in `dbo.TS133_Probe`.
  - Sie hat dieselbe Sperr-Form wie der Fix: `IF EXISTS (SELECT 1 FROM dbo.TS133_Probe) BEGIN IF @Trockenlauf = 1 … TREFFER: 0 … END`.
- Spur für den echten Altbestand: eine von Hand eingefügte Zeile in `dbo.PickingOrders` der Kopie mit
  `CreatedBy = N'Migration-KO-Altbestand'` (synthetische Testdaten, nicht der Scharflauf).
- Danach alles entfernen.

| Nr | Szenario | Erwartet |
|---|---|---|
| 133.1 | Echtes Altbestand-Skript, Trockenlauf per `sqlcmd -b` auf der Kopie **mit** Spur-Zeile | Exit 0; Zeilen „bereits ueberfuehrt … Nichts zu tun“ und `TREFFER: 0`; keine Zählungstabellen; DB unverändert |
| 133.2 | Wie 133.1, Spur-Zeile entfernt | Ausgabe wie vor dem Fix: Zählung, Liste „Altbestand pruefen“, `TREFFER: n` |
| 133.3 | Update-Skript, synthetischer Eintrag `Test/Sperre`, Probe-Tabelle leer, Backup j, `SCHARF` | Probe-Zeilen geschrieben, Marker `Scharf`; **zweiter Trockenlauf `TREFFER: 0` als OK**, keine Warnung |
| 133.4 | Nächster Lauf des Update-Skripts nach 133.3 | Eintrag „erledigt am … (Scharf, Treffer n)“, kein Trockenlauf, kein Halt |
| 133.5 | Altbestand mit vorhandenem Marker (aus einem Lauf vor v1.99.16, also mit altem Hash), `-NurPruefen` | „erledigt am …“, wird nicht angeboten (AK 10) |
| 133.6 | Drift-Test `NachlaufManifestDriftTests` | grün; Rotprobe „Sperre ohne TREFFER: 0“ rot wie erwartet |

**Negativfälle**

| Nr | Szenario | Erwartet |
|---|---|---|
| 133.7 | Synthetisches Skript **ohne** Trockenlauf-Zweig in der Sperre (Stand vor dem Fix), sonst wie 133.3 | Warnung „zweiter Trockenlauf ohne Zeile TREFFER (Exit 1). Scharflauf ist übernommen …“, **kein** leerer Wert; Marker `Scharf` steht |
| 133.8 | Synthetisches Skript, das nach dem Scharflauf noch Treffer meldet (Trockenlauf zählt eine Zeile, die der Scharflauf nicht ändert) | Warnung „TREFFER: 1 (erwartet 0)“ wie bisher |
| 133.9 | **Nur Gerald:** echtes Altbestand-Skript, `@Trockenlauf = 0` von Hand in SSMS auf einer DEV-Kopie mit Spur-Zeile | `RAISERROR` „NICHT ERNEUT AUSFUEHREN“, nichts angelegt |
| 133.10 | Kopie ohne Spalte `PickingOrders.AltbestandPruefen` (Spalte in einer Wegwerf-Kopie entfernt) | Trockenlauf Exit ≠ 0 mit „SQL/141 fehlt“, kein `TREFFER: 0` |

Hinweis 133.9: Formal ist das ein Scharflauf eines echten Einmal-Skripts. Agenten führen ihn deshalb nicht aus (ADR 0023), auch wenn die
Sperre jeden Schreibweg abschneidet. Der QA-Lauf belegt AK 2 stattdessen durch Lesen des Diffs: Der `RAISERROR`-Zweig ist unverändert.

**Mensch (Gerald):** Prüfschritt AKE Test-IDEAL oben. Beim nächsten Update auf einem System, auf dem der Altbestand noch offen ist
(IDEAL-Prod bzw. AKE-Prod), muss der zweite Trockenlauf `TREFFER: 0` ohne Warnung zeigen.

## Etappen (nur bei epic: true)

| # | Etappe | Status | Commit |
|---|--------|--------|--------|
| – | entfällt (kein Epic) | – | – |

## Deploy

*Vorläufig, der QA-Lauf bestätigt gegen den echten Diff.*

- **Web-App:** ja. Nur wegen des Versionssprungs: Das Update-Skript vergleicht die Fußzeile mit der Paketversion (`Update-IdealAkeWms.ps1:860-861`).
- **Service:** ja. Nur wegen des Versionssprungs (`IDEALAKEWMSService/AppVersion.cs`).
- **Migration:** nein.
- **Manifest-Einträge:** keine neuen. Der Eintrag `Einmalig/2026-11-08_v1.88.0_Kommissionierauftraege_Altbestand` (`nachlauf.psd1:154-159`)
  bleibt unverändert, auch Id, Version und Text. Das geänderte Einmal-Skript und das geänderte Update-Skript kommen mit dem Paket.
- **Reihenfolge:** keine Besonderheit. Das neue Update-Skript wirkt ab dem nächsten Lauf aus dem neuen Paket.
- **Publish-Befehle** (im Worktree):
  `dotnet publish IdealAkeWms/IdealAkeWms.csproj -c Release -o .\publish\IDEALAKEWMSWeb`
  `dotnet publish IDEALAKEWMSService/IDEALAKEWMSService.csproj -c Release -o .\publish\IDEALAKEWMSWebService`
  Danach das Paket wie im Bündel üblich bauen (inkl. `SQL/` und `scripts/deploy/`).

**Brain-Nachträge (Hauptcheckout, nicht im Worktree):**
- `fallstricke.md` §30 neuer Punkt: „Eine Wiederholungssperre in einer Trockenlauf-Datei muss im Trockenlauf `TREFFER: 0` ausgeben.“
  **Warum wichtig:** Fluss A startet nach jedem Scharflauf einen zweiten Trockenlauf, und genau dann greift die Sperre. Ohne die Zeile
  meldet das Update-Skript eine Warnung mit leerem Wert, obwohl der Lauf gelungen ist.
- Bug-Record auf `behoben` setzen.
- Changelog-Eintrag `secondbrain/changelog/`, `feature-map.md`.
- Rückfrage 3: `spec-agent.md` und `_templates/spec.md`.

## Offene Rueckfragen

1. Nebenwirkung: Schlägt die Spur-Abfrage fehl (`Get-SpurZahl` `:273` liefert dann `$null`), obwohl der Altbestand schon lief, läuft heute
   Fluss A in die Sperre. Am Haltepunkt bricht das Skript dann ab, Web und Dienst bleiben gestoppt. Nach dem Fix meldet der Trockenlauf
   `TREFFER: 0`, das Skript bietet den Marker `Entfaellt` an und läuft weiter. Ist das gewollt? Empfehlung: ja, der Altbestand ist erledigt,
   und die Sperre verhindert jeden Scharflauf weiter.
2. Drift-Wächter: Die neue Regel erkennt nur Sperren der Form `IF EXISTS (…) BEGIN … RAISERROR/THROW … END`. Das ist heute die einzige Form
   in den Trockenlauf-Dateien. Reicht diese Heuristik (Empfehlung), oder soll jede Sperre ein festes Kennzeichen tragen (z. B. Kommentar
   `-- SPERRE`), das der Test erzwingt?
3. Soll der Dev-Lauf die Regel „auch die Wiederholungssperre gibt im Trockenlauf `TREFFER: 0` aus“ zusätzlich in `.claude/agents/spec-agent.md`
   und `secondbrain/_templates/spec.md` ergänzen (Hauptcheckout, Abschnitt Manifest/ADR 0023)? Empfehlung: ja, eine Zeile je Datei.

## Freigabe-Nachtrag 2026-11-13 13:58 (Gerald, geht dem Rumpf vor)
- Gemeinsamer /goal-Lauf mit [[2026-11-13-updater-zweiter-trockenlauf-altbestand-spec]], [[2026-11-13-doppelte-kommissionier-prio-nach-ruecknahme-spec]]
  und [[2026-11-13-utc-schreiber-auf-lokalzeit-spec]] auf dem Bündel (v1.99.15). **Versions-/TS-/SQL-Nummern vergibt der Lauf in Lauf-Reihenfolge**
  (Versionen ab 1.99.16, TS ab 133, SQL ab 148); Nummern in den Specs sind Vorschläge.
- **Zusätzlich im Umfang (Zusatzfund /goal-Lauf 2026-11-13, [[2026-11-13-buendel-v1-99-13-goal-lauf]]):** (1) `-NurPruefen` und jede
  Ja/Nein-/Eingabe-Frage dürfen ohne Eingabe nicht endlos warten – leere Eingabe bzw. Ende der Eingabe führt zu einer definierten Antwort
  (bei `-NurPruefen` nur lesend weiter, sonst „nein“/Abbruch ohne Änderung); Ursache im Skript belegen. (2) DEV-Ziel(e) in `ziele.psd1`
  vorschlagen (AKE-DEV/IDEAL-DEV mit Pfaden und Url von DEV, Prod=$false) – Pfade aus der DEV-Installation belegen, nicht raten.

## Freigabe-Antworten (Mensch fuellt aus — Schranke 1)

<!--
  ANLEITUNG: Diese Spec ist erst startklar, wenn HIER jede offene Rueckfrage
  beantwortet ist UND die Datei nach specs/freigegeben/ verschoben wurde UND
  im Frontmatter status: Freigegeben steht. Der Dev-Lauf liest DIESEN Block
  als seinen Auftrag. Antworte je Frage in **fett** hinter dem Pfeil.
-->

1. Spur-Abfrage scheitert → künftig „entfällt“ statt Abbruch am Haltepunkt, gewollt? → **Ja, gewollt (Gerald 2026-11-13).**
2. Drift-Wächter: Heuristik `IF EXISTS`-Sperre reicht, oder Kennzeichen-Konvention? → **Heuristik reicht (Gerald 2026-11-13).**
3. Regel zusätzlich in `spec-agent.md` und `_templates/spec.md` ergänzen? → **Ja, eine Zeile je Datei (Gerald 2026-11-13).**
